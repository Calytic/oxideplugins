PLUGIN.Title = "EventKick"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Description = "Kicks players that connect to server when event is running"
PLUGIN.Author = "SCALEXO"
PLUGIN.Url = ""
PLUGIN.ResourceId = 714

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.Command = self.Config.Settings.Command or self.Config.Settings.ChatCommand or "eventkick"
    self.Config.Settings.BroadcastChat = self.Config.Settings.BroadcastChat or "true"
	self.Config.Settings.EventKick = self.Config.Settings.EventKick or "false"

    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.PlayerKicked = self.Config.Messages.PlayerKicked or "{player} was kicked for joining when event is running!"
    self.Config.Messages.YouKicked = self.Config.Messages.YouKicked or "You were kicked for joining an event that is already running! Please try again later."
	self.Config.Messages.EventKickOff = self.Config.Messages.EventKickOff or "Event Kick is now off!"
    self.Config.Messages.EventKickOn = self.Config.Messages.EventKickOn or "Event Kick is now on!"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
	command.AddChatCommand(self.Config.Settings.Command, self.Plugin, "ChatCommand")
    command.AddConsoleCommand("global." .. self.Config.Settings.Command, self.Plugin, "ConsoleCommand")
    permission.RegisterPermission("eventkick.bypass", self.Plugin)
	permission.RegisterPermission("eventkick.status", self.Plugin)
end

local function Print(self, message) print(self.Title .. " > " .. message) end

local function HasPermission(steamId)
    if permission.UserHasPermission(steamId, "eventkick.bypass") then return true end
    return false
end

local function Eventkick(status)
    if permission.UserHasPermission(status, "eventkick.status") then return true end
    return false
end

function PLUGIN:ChatCommand(player)
    local steamId = rust.UserIDFromPlayer(player)
    if not HasPermission(steamId, "eventkick.status") then
        rust.SendChatMessage(player, self.Config.Messages.NoPermission)
        return
    end

    if self.Config.Settings.EventKick == "false" then
        self.Config.Settings.EventKick = "true"
        rust.SendChatMessage(player, self.Config.Messages.EventKickOn)
    else
        self.Config.Settings.EventKick = "false"
        rust.SendChatMessage(player, self.Config.Messages.EventKickOff)
    end

    self:SaveConfig()
end

function PLUGIN:EventKick(player)
    local steamId = rust.UserIDFromPlayer(player)
        if self.Config.Settings.EventKick == "true" then
            local message = self.Config.Messages.YouKicked
            Network.Net.sv:Kick(player.net.connection, message)
            local message = self.Config.Messages.PlayerKicked:gsub("{player}", player.displayName)
            Print(self, message)
            if self.Config.Settings.Broadcast == "true" then rust.BroadcastChat(message) end
        end
end

function PLUGIN:OnPlayerInit(player)
	if not player then return end
	local steamId = rust.UserIDFromPlayer(player)
	if HasPermission(steamId) then return end
		self:EventKick(player)
end

