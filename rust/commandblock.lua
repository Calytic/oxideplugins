PLUGIN.Title = "Command Block"
PLUGIN.Version = V(0, 2, 3)
PLUGIN.Description = "Blocks configured commands sent by a client to the server."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/resources/647/"
PLUGIN.ResourceId = 647
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Add chat and console commands to add/remove from the block list

function PLUGIN:Init()
    self:LoadDefaultConfig()
end

function PLUGIN:OnRunCommand(arg)
    if not arg then return end
    if not arg.connection then return end
    if not arg.connection.player then return end
    if not arg.cmd then return end
    if not arg.cmd.name then return end
    local player = arg.connection.player
    local command = arg.cmd.namefull
    local blocked = false
    for key, setting in pairs(self.Config.Settings.Commands) do
        if setting:match("^(" .. command .. ")$") then blocked = true end
        if blocked then
            if player then
                rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.Blocked)
                player:SendConsoleCommand("echo " .. self.Config.Messages.Blocked)
            end
            if debug then print("[" .. self.Title .. "] Player tried to run console command \"" .. arg.cmd.name .. "\" but was blocked by configuration") end
            return false 
        end
    end
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
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "SERVER"
    self.Config.Settings.Commands = self.Config.Settings.Commands or { "kill", "server.seed" }
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.Blocked = self.Config.Messages.Blocked or "Sorry, that command is blocked!"
    self.Config.Messages.CommandBlocked = nil -- Removed n 0.2.3
    self.Config.Messages.ChatCommand = nil -- Removed n 0.2.3, temporarily
    self:SaveConfig()
end
