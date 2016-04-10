PLUGIN.Title        = "Player location"
PLUGIN.Description  = "Allows users to see their current location"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 2, 0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceID   = 663

function PLUGIN:Init()
    self:LoadDefaultConfig()
    for _, cmd in pairs(self.Config.Settings.ChatCommands) do
        command.AddChatCommand(cmd, self.Object, "cmdLocation")
    end
end

function PLUGIN:LoadDefaultConfig()
    -- settings
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.ChatCommands   = self.Config.Settings.ChatCommands or {"loc", "location" }
    self.Config.Settings.Precision      = self.Config.Settings.Precision    or "0"
    -- messages
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.Location = self.Config.Messages.Location or "Current location x: {x} y: {y} z: {z}"
    self.Config.Messages.HelpText = self.Config.Messages.HelpText or "use /location or /loc to see your current location"
    -- save the config to file
    self:SaveConfig()
end

function PLUGIN:cmdLocation(player, cmd, args)
    if not player then return end
    local format = "%."..self.Config.Settings.Precision.."f"
    local x = string.format(format, player.transform.position.x)
    local y = string.format(format, player.transform.position.y)
    local z = string.format(format, player.transform.position.z)
    local output = string.gsub(self.Config.Messages.Location, "{x}", x)
    output = string.gsub(output, "{y}", y)
    output = string.gsub(output, "{z}", z)
    rust.SendChatMessage(player, output)
end

function PLUGIN:SendHelpText(player)
    rust.SendChatMessage(player, self.Config.Messages.HelpText)
end