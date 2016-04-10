PLUGIN.Title = "Notifico"
PLUGIN.Version = V(0, 2, 4)
PLUGIN.Description = "Sends messages and alerts to configured IRC channels via Notifico - http://n.tkte.ch/"
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/704/"
PLUGIN.ResourceId = 704
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Additional user information such as IP, country, SteamID, etc

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

function PLUGIN:OnPlayerConnected(packet)
    if not packet then return end
    if not packet.connection then return end
    local player = packet.connection
    if self.Config.Settings.ShowConnects ~= "false" then
        local payload = self.Config.Messages.Connected:gsub("{player}", player.username)
        self:SendPayload(player, payload)
    end
end

function PLUGIN:OnPlayerDisconnected(player)
    if not player then return end
    if self.Config.Settings.ShowDisconnects ~= "false" then
        local payload = self.Config.Messages.Disconnected:gsub("{player}", player.displayName)
        self:SendPayload(player, payload)
    end
end

function PLUGIN:OnRunCommand(arg)
    if not arg then return end; if not arg.connection then return end; if not arg.connection.player then return end
    if not arg.cmd then return end; if not arg.cmd.namefull then return end
    local player = arg.connection.player
    if not self:PermissionsCheck(player) then return end
    local chat = arg:GetString(0, "text"); local console = arg.cmd.namefull; local excluded
    for key, value in pairs(self.Config.Settings.Exclusions) do if value == console then excluded = true; break end end
    if not excluded then
        if self.Config.Settings.ShowConsoleCommands ~= "false" then
            self:SendPayload(player, player.displayName .. " ran console command: " .. console)
        end
    elseif chat:sub(1, 1) == "/" then
        if self.Config.Settings.ShowChatCommands ~= "false" then
            self:SendPayload(player, player.displayName .. " ran chat command: " .. chat)
        end
    end
end

function PLUGIN:OnPlayerChat(arg)
    if not arg then return end; if not arg.connection then return end; if not arg.connection.player then return end
    local player = arg.connection.player
    local chat = arg:GetString(0, "text")
    if not chat or chat == "" or chat:sub(1, 1) == "/" then return end
    if self.Config.Settings.ShowChat ~= "false" then
        self:SendPayload(player, player.displayName .. ": " .. chat)
    end
end

function PLUGIN:SendPayload(netuser, payload)
    if self.Config.Settings.HookUrl == "" then print(self.Title .. ": You need to set your Notifico hook URL!"); return end
    local url = self.Config.Settings.HookUrl .. "?payload=" .. payload
    webrequests.EnqueueGet(url, function(code, response)
        if code ~= 200 then print(self.Title .. ": Failed to send message to Notifico!"); return end
    end, self.Plugin)
end

function PLUGIN:PermissionsCheck(player)
    local authLevel
    if player then authLevel = player.net.connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(player.displayName .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.Exclusions = self.Config.Settings.Exclusions or {
        "chat.say", "craft.add", "craft.cancel", "global.kill", "global.respawn", "global.respawn_sleepingbag", "global.status",
        "global.wakeup", "inventory.endloot"
    }
    self.Config.Settings.HookUrl = self.Config.Settings.HookUrl or ""
    self.Config.Settings.ShowChat = self.Config.Settings.ShowChat or "true"
    self.Config.Settings.ShowChatCommands = self.Config.Settings.ShowChatCommands or "true"
    self.Config.Settings.ShowConnects = self.Config.Settings.ShowConnects or "true"
    self.Config.Settings.ShowConsoleCommands = self.Config.Settings.ShowConsoleCommands or "true"
    self.Config.Settings.ShowDisconnects = self.Config.Settings.ShowDisconnects or "true"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.Connected = self.Config.Messages.Connected or "{player} has connected to the server"
    self.Config.Messages.Disconnected = self.Config.Messages.Disconnected or "{player} has disconnected from the server"
    self:SaveConfig()
end
