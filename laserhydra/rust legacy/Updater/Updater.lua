PLUGIN.Title = "Updater"
PLUGIN.Version = V(0, 4, 6)
PLUGIN.Description = "Automatic update checking and notifications for plugins."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/380/"
PLUGIN.ResourceId = 380

local debug = false
local game = "legacy"

--[[ Do NOT edit the config here, instead edit Updater.json in oxide/config ! ]]

local messages, settings
function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.ChatHelp = messages.ChatHelp or "Use '/update' to check for plugin updates"
    messages.ConsoleHelp = messages.ConsoleHelp or "Use 'update' to check for plugin updates"
    messages.CheckFailed = messages.CheckFailed or "{plugin} update check failed!"
    messages.CheckFinished = messages.CheckFinished or "Update check finished!"
    messages.CheckStarted = messages.CheckStarted or "Update check started!"
    messages.NoPermission = messages.NoPermission or "You do not have permission to use this command!"
    messages.NoUpdates = messages.NoUpdates or "Using the latest version of all plugins!"
    messages.NotifySubject = messages.NotifySubject or "{count} update(s) available on {hostname}"
    messages.Outdated = messages.Outdated or "{plugin} is outdated! Installed: {current}, Latest: {latest}"
    messages.Supported = messages.Supported or "Supported plugin count: {count}"
    messages.UpToDate = messages.UpToDate or "{plugin} is up-to-date, currently using version: {version}"

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.Command = settings.Command or "update"
    settings.CheckInterval = tonumber(settings.CheckInterval) or 3600
    settings.EmailNotifications = settings.EmailNotifications or "false"
    settings.PushNotifications = settings.PushNotifications or "false"
    settings.ShowUpToDate = settings.ShowUpToDate or "false"

    self:SaveConfig()
end

local function Print(self, message) print("[" .. self.Title .. "] " .. message) end

local function ParseString(message, values)
    for key, value in pairs(values) do
        value = tostring(value):gsub("[%-?*+%[%]%(%)%%]", "%%%%%0")
        message = message:gsub("{" .. key .. "}", value)
    end
    return message
end

local function HasPermission(steamId, perm)
    if permission.UserHasPermission(steamId, perm) then return true end
    return false
end

local server = covalence.Server

local function SendChatMessage(player, message)
    if game == "rust" then player.ConnectedPlayer:SendChatMessage(message) return end
    if game == "legacy" then rust.SendChatMessage(player, message) return end
    if game == "rok" then rok.SendChatMessage(player, message) return end
    --if game == "7dtd" then sdtd.SendChatMessage(player, message) return end
end

local function GetSteamId(player)
    if game == "rust" then return player.UniqueID end
    if game == "legacy" then return rust.UserIDFromPlayer(player) end
    if game == "rok" then return rok.IdFromPlayer(player) end
    --if game == "7dtd" then return sdtd.IdFromPlayer(player) end
    return false
end

local function GetHostname()
    if game == "rust" then return server.Name end
    if game == "legacy" then return global.server.hostname end
    if game == "rok" then return global.DedicatedServerBypass.get_Settings().ServerName end
    --if game == "7dtd" then return global.GamePrefs.GetString(EnumGamePrefs.ServerName) end
    return false
end

local function EmailMessage(subject, message)
    local emailApi = plugins.Find("EmailAPI") or plugins.Find("email")
    if emailApi then
        emailApi:CallHook("EmailMessage", subject, message)
    else
        if game == "rust" then print("Email API is not loaded! http://oxidemod.org/plugins/712/") return end
        if game == "legacy" then print("Email API is not loaded! http://oxidemod.org/plugins/1170/") return end
        if game == "rok" then print("Email API is not loaded! http://oxidemod.org/plugins/1172/") return end
        if game == "7dtd" then print("Email API is not loaded! http://oxidemod.org/plugins/0/") return end
    end
end

local function PushMessage(subject, message)
    local pushApi = plugins.Find("PushAPI") or plugins.Find("push")
    if pushApi then
        pushApi:CallHook("PushMessage", subject, message)
    else
        if game == "rust" then print("Push API is not loaded! http://oxidemod.org/plugins/705/") return end
        if game == "legacy" then print("Push API is not loaded! http://oxidemod.org/plugins/1166/") return end
        if game == "rok" then print("Push API is not loaded! http://oxidemod.org/plugins/1164/") return end
        if game == "7dtd" then print("Push API is not loaded! http://oxidemod.org/plugins/0/") return end
    end
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(settings.Command, self.Plugin, "ChatCommand")
    command.AddConsoleCommand("global." .. settings.Command, self.Plugin, "ConsoleCommand")
    permission.RegisterPermission("update.check", self.Plugin)
end

function PLUGIN:OnServerInitialized()
    self:UpdateCheck()

    local interval = settings.CheckInterval
    if interval > 0 then
        timer.Repeat(interval, 0, function() self:UpdateCheck() end, self.Plugin)
    end
end

--Command{ "update" }
function PLUGIN:ChatCommand(player, cmd)
    if player and not HasPermission(GetSteamId(player), "update.check") then
        SendChatMessage(player, messages.NoPermission)
        return
    end

    self:UpdateCheck(player)
end

--Command{ "global.update" }
function PLUGIN:ConsoleCommand(args)
    local player
    if args.connection then player = args.connection.player end

    if player and not HasPermission(GetSteamId(player), "update.check") then
        args:ReplyWith(messages.NoPermission)
        return
    end

    self:UpdateCheck(player)
end

function PLUGIN:UpdateCheck(player)
    Print(self, "### " .. messages.CheckStarted)
    if player then SendChatMessage(player, "### " .. messages.CheckStarted) end

    local outdatedTable, outdated, supported = nil, 0, 0
    local emailMessage, pushMessage = "", ""
    local pluginList = plugins.GetAll()

    for i = 0, pluginList.Length - 1 do
        local title = pluginList[i].Title
        local version = pluginList[i].Version:ToString()
        local resourceId = tostring(pluginList[i].ResourceId)
        if resourceId == "0" then resourceId = tostring(pluginList[i].Object.ResourceId) end

        if resourceId and resourceId ~= "" and resourceId ~= "0" and resourceId:match("%d") then
            supported = supported + 1

            local url = "https://dev.wulf.im/oxide/" .. resourceId
            webrequests.EnqueueGet(url, function(code, response)
                supported = supported - 1

                if code == 200 and response ~= "" then
                    if version < response then
                        local message = ParseString(messages.Outdated, { plugin = title, current = version, latest = response })
                        Print(self, message)
                        Print(self, "- http://oxidemod.org/plugins/" .. resourceId .. "/")
                        if player then
                            SendChatMessage(player, message)
                            SendChatMessage(player, "- http://oxidemod.org/plugins/" .. resourceId .. "/")
                        end

                        local message = ParseString(messages.Outdated, { plugin = "<b>" .. title .."</b>", current = version, latest = response })
                        emailMessage = emailMessage .. "<br/>" .. message
                        pushMessage = pushMessage .. "\n" .. message

                        outdated = outdated + 1

                    elseif settings.ShowUpToDate == "true" then
                        local message = ParseString(messages.UpToDate, { plugin = title, version = version })
                        Print(self, message)
                        if player then SendChatMessage(player, message) end
                    end
                else
                    Print(self, ParseString(messages.CheckFailed, { plugin = title }))
                    if player then SendChatMessage(player, message) end
                end

                if supported == 0 then
                    if outdated == 0 then
                        Print(self, messages.NoUpdates)
                        if player then SendChatMessage(player, messages.NoUpdates) end
                    end

                    Print(self, "### " .. messages.CheckFinished)
                    if player then SendChatMessage(player, "### " .. messages.CheckFinished) end

                    local subject = ParseString(messages.NotifySubject, { count = outdated, hostname = GetHostname() })
                    if settings.EmailNotifications == "true" and outdated > 0 then EmailMessage(subject, emailMessage) end
                    if settings.PushNotifications == "true" and outdated > 0 then PushMessage(subject, pushMessage) end
                end

                if debug then Print(self, "Webrequest for " .. title .. " gave response: " .. response) end
            end, self.Plugin)
        end
    end

    Print(self, ParseString(messages.Supported, { count = supported }))
end

function PLUGIN:SendHelpText(player)
    if HasPermission(GetSteamId(player), "update.check") then
        SendChatMessage(player, messages.ChatHelp)
    end
end
