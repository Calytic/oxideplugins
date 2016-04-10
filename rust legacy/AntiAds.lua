PLUGIN.Title = "Anti-Advertising"
PLUGIN.Version = V(2, 0, 0)
PLUGIN.Description = "Kicks or bans players who try to advertise there servers."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://oxidemod.org/plugins/476/"
PLUGIN.ResourceId = 476

local game = "legacy"

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.Kick = self.Config.Settings.Kick or "true"
    self.Config.Settings.Ban = self.Config.Settings.Ban or "false"
    self.Config.Settings.AllowedServers = self.Config.Settings.AllowedServers or { "84.200.193.120:28030", "84.200.193.120:28080"}

    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.NoAdvertising = self.Config.Messages.NoAdvertising or "Advertising is now allowed on this server"
    self.Config.Messages.PlayerBanned = self.Config.Messages.PlayerBanned or "{player} banned for advertising"
    self.Config.Messages.PlayerKicked = self.Config.Messages.PlayerKicked or "{player} kicked for advertising"

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    permission.RegisterPermission("antiads.bypass", self.Plugin)
end

local function HasPermission(steamId)
    if permission.UserHasPermission(steamId, "antiads.bypass") then return true end
    return false
end

local function Print(self, message) print(self.Title .. " > " .. message) end

local function ParseMessage(message, values)
    for key, value in pairs(values) do message = message:gsub("{" .. key .. "}", value) end
    return message
end

local function AllowedServer(self, address)
    for i = 1, #self.Config.Settings.AllowedServers do
        if address == self.Config.Settings.AllowedServers[i] then return true end
    end
end

local function MatchAddress(self, text)
    if text:match("(%d+.%d+.%d+.%d+:%d+)") then return true end
end

local function SendMessage(self, game, player)
    local message = self.Config.Messages.NoAdvertising

    if game == "rust" then
        rust.SendChatMessage(player, message)
        return
    end
    if game == "legacy" then
        rust.Notice(player, message, "", 30)
        return
    end
    if game == "rok" then
        SendChatMessage(player, player.Name, message)
        return
    end
    if game == "7dtd" then
        sevendays.SendChatMessage(player, message)
        return
    end
end

local function Ban(self, game, player)
    local reason = self.Config.Messages.NoAdvertising
    local banned = self.Config.Messages.PlayerBanned

    if game == "rust" then
        rust.RunServerCommand("ban", player.displayName, reason)
        local message = ParseMessage(banned, { player = player.displayName })
        rust.BroadcastChat(message)
        return
    end
    if game == "legacy" then
        player:Ban()
        local message = ParseMessage(banned, { player = player.displayName })
        rust.BroadcastChat(message)
        return
    end
    if game == "rok" then
        local message = util.TableToArray({ player, reason })
        CodeHatch.Engine.Networking.Server.Ban.methodarray[1]:Invoke(nil, message)
        local message = ParseMessage(banned, { player = player.Name })
        rok.BroadcastChat(message)
        return
    end
    if game == "7dtd" then
        --global.AdminTools.AddBan(steamId, ownerId, datetime, reason, true)
        local message = ParseMessage(banned, { player = player })
        sevendays.BroadcastChat(message)
        return
    end
end

local function Kick(self, game, player)
    local reason = self.Config.Messages.NoAdvertising
    local kicked = self.Config.Messages.PlayerKicked

    if game == "rust" then
        Network.Net.sv:Kick(player.net.connection, reason)
        local message = ParseMessage(kicked, { player = player.displayName })
        rust.BroadcastChat(message)
        return
    end
    if game == "legacy" then
        player:Kick(global.NetError.Facepunch_Kick_RCON, true)
        local message = ParseMessage(kicked, { player = player.displayName })
        rust.BroadcastChat(message)
        return
    end
    if game == "rok" then
        local message = util.TableToArray({ player, reason })
        CodeHatch.Engine.Networking.Server.Kick.methodarray[2]:Invoke(nil, message)
        local message = ParseMessage(kicked, { player = player.Name })
        rok.BroadcastChat(message)
        return
    end
    if game == "7dtd" then
        --global.StaticDirectories.KickPlayerForClientInfo(clientinfo, reason, monobehaviour)
        local message = ParseMessage(kicked, { player = player })
        sevendays.BroadcastChat(message)
        return
    end
end

local function Punish(self, game, player)
    if self.Config.Settings.Ban == "true" then
        Ban(self, game, player)
    elseif self.Config.Settings.Kick == "true" then
        Kick(self, game, player)
    end
end

-- Rust Experimental
if game == "rust" then
    function PLUGIN:OnPlayerChat(arg)
        if not arg then return end
        if not arg.connection then return end
        if not arg.connection.player then return end

        local player = arg.connection.player
        local message = arg:GetString(0, "text")
        local name = arg.connection.player.displayName
        local steamId = rust.UserIDFromPlayer(player)

        if not message or message == "" or message:sub(1, 1) == "/" then return end

        if not MatchAddress(self, message) then return end
        if not AllowedServer(self, address) then
            if HasPermission(steamId) then return end
            SendMessage(self, game, player)
            Punish(self, game, player)
            return false
        end
    end
    return
end

-- Rust Legacy
if game == "legacy" then
    function PLUGIN:OnPlayerChat(player, message)
        if not player then return end
        if not message or message == "" or message:sub(1, 1) == "/" then return end

        local name = player.displayName
        local steamId = rust.UserIDFromPlayer(player)

        if not MatchAddress(self, message) then return end
        if not AllowedServer(self, address) then
            if HasPermission(steamId) then return end
            SendMessage(self, game, player)
            Punish(self, game, player)
            return false
        end
    end
    return
end

-- Reign of Kings
if game == "rok" then
    function PLUGIN:OnPlayerChat(event)
        if not event then return end
        if event.SenderId == "9999999999" then return end

        local player = event.Player
        local message = event.Message
        local name = event.Name
        local steamId = rok.GetEventSenderId(event)

        if not message or message == "" or message:sub(1, 1) == "/" then return end

        if not MatchAddress(self, message) then return end
        if not AllowedServer(self, address) then
            if HasPermission(steamId) then return end
            Punish(self, game, player)
            event:Cancel()
        end
    end
    return
end

-- 7 Days to Die
if game == "7dtd" then
    function PLUGIN:OnPlayerChat(message, name)
        if not message or message == "" or message:sub(1, 1) == "/" then return end
        if not name then return end

        --local steamId = sevendays.IdFromPlayer(name)

        if not MatchAddress(self, message) then return end
        if not AllowedServer(self, address) then
            --if HasPermission(steamId) then return end
            SendMessage(self, game, name)
            Punish(self, game, name)
            return false
        end
    end
    return
end
