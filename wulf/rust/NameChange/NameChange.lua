PLUGIN.Title = "Name Change"
PLUGIN.Version = V(0, 1, 5)
PLUGIN.Description = "Instantly rename any player and keep it that way."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1184/"
PLUGIN.ResourceId = 1184

local debug = false

--[[ Do NOT edit the config here, instead edit NameChange.json in oxide/config ! ]]

local messages, settings
function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.ChatHelp = messages.ChatHelp or "Use '/rename player name' to rename a player"
    messages.ConsoleHelp = messages.ConsoleHelp or "Use 'rename player name' to rename a player"
    messages.InvalidTarget = messages.InvalidTarget or "Invalid player! Please try again"
    messages.NoPermission = messages.NoPermission or "You do not have permission to use this command!"
    messages.PlayerRenamed = messages.PlayerRenamed or "{player} renamed to {name}!"
    messages.PlayerReset = messages.PlayerReset or "{player}'s name reset to {name}!"
    messages.YouRenamed = messages.YouRenamed or "You were renamed to {name} by {player}!"
    messages.YouReset = messages.YouReset or "Your name has been reset to {name}!"

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.Command = settings.Command or "rename"
    settings.NotifyPlayer = settings.NotifyPlayer or "true"

    self:SaveConfig()
end

local names
local displayName = global.BasePlayer._type:GetField("_displayName", rust.PrivateBindingFlag()) 

function PLUGIN:Init()
    self:LoadDefaultConfig()

    names = datafile.GetDataTable("NameChange") or {}

    command.AddChatCommand(settings.Command, self.Plugin, "ChatCommand")
    command.AddConsoleCommand("global." .. settings.Command, self.Plugin, "ConsoleCommand")

    permission.RegisterPermission("rename.players", self.Plugin)
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

local function FindPlayer(self, player, target)
    local target = global.BasePlayer.Find(target)
    if not target then
        if player then
            rust.SendChatMessage(player, messages.InvalidTarget)
        else
            Print(self, messages.InvalidTarget)
        end
        return
    end
    return target
end

function PLUGIN:OnUserApprove(connection)
    local steamId = rust.UserIDFromConnection(connection)

    if debug then Print(self, connection.username .. " (" .. steamId .. ")") end

    for key, _ in pairs(names) do
        if steamId == key then
            connection.username = names[steamId].Rename
        end
    end

    if debug then Print(self, connection.username .. " (" .. steamId .. ")") end
end

function PLUGIN:Rename(player, target, arg)
    if player and not HasPermission(rust.UserIDFromPlayer(player), "rename.players") then
        if player then
            rust.SendChatMessage(player, messages.NoPermission)
        else
            Print(self, messages.NoPermission)
        end
        return
    end

    local target = FindPlayer(self, player, target)
    if target then
        local source
        if not player then source = "Admin" else source = player.displayName end

        local oldName = target.displayName
        local steamId = rust.UserIDFromPlayer(target)

        if debug then Print(self, oldName .. " (" .. steamId .. ")") end

        if names[steamId] and arg == "reset" then
            displayName:SetValue(target, names[steamId].Original)
            names[steamId] = nil
            datafile.SaveDataTable("NameChange")

            local message = ParseString(messages.PlayerReset, { player = oldName, name = target.displayName })
            if player then
                rust.SendChatMessage(player, message)
            else
                Print(self, message)
            end
        else
            names[steamId] = names[steamId] or {}
            names[steamId].Original = target.displayName
            names[steamId].Rename = arg
            datafile.SaveDataTable("NameChange")
            displayName:SetValue(target, arg)

            if settings.NotifyPlayer == "true" then
                local message = ParseString(messages.YouRenamed, { player = source, name = arg })
                if player then
                    rust.SendChatMessage(target, message)
                end
            end

            local message = ParseString(messages.PlayerRenamed, { player = oldName, name = arg })
            if player and player ~= target then
                rust.SendChatMessage(player, message)
            else
                Print(self, message)
            end
        end

        if debug then Print(self, name .. " (" .. steamId .. ")") end
    end
end

function PLUGIN:ChatCommand(player, cmd, args)
    if args.Length ~= 2 then
        rust.SendChatMessage(player, messages.ChatHelp)
        return
    end

    self:Rename(player, args[0], args[1])
end

function PLUGIN:ConsoleCommand(args)
    local player
    if args.connection then player = args.connection.player end

    if not args:HasArgs(2) then
        if not player then
            Print(self, messages.ConsoleHelp)
        else
            args:ReplyWith(messages.ConsoleHelp)
        end
        return
    end

    self:Rename(player, args:GetString(0), args:GetString(1))
end

function PLUGIN:SendHelpText(player)
    if HasPermission(rust.UserIDFromPlayer(player), "rename.players") then
        rust.SendChatMessage(player, messages.ChatHelp)
    end
end
