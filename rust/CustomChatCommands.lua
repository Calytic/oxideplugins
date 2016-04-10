PLUGIN.Title        = "Custom Chat Commands"
PLUGIN.Description  = "Set completely custom chat commands"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(2, 3, 1)
PLUGIN.ResourceID   = 649

-- --------------------------------
-- init
-- --------------------------------
function PLUGIN:Init()
    for cmd, _ in pairs(self.Config.ChatCommands) do
        command.AddChatCommand(cmd, self.Object, "cmdChatCmd")
    end
    self:LoadDefaultConfig()
    self:RegisterPermissions()
end
-- --------------------------------
-- permission check
-- --------------------------------
local function HasPermission(player, perm)
    local steamID = rust.UserIDFromPlayer(player)
    if player:GetComponent("BaseNetworkable").net.connection.authLevel > 0 then
        return true
    end
    if permission.UserHasPermission(steamID, perm) then
        return true
    end
    return false
end
-- --------------------------------
-- load config
-- --------------------------------
function PLUGIN:LoadDefaultConfig()
    -- general settings
    self.Config.Settings                = self.Config.Settings or {}
    self.Config.Settings.ChatName       = self.Config.Settings.ChatName or "SERVER"
    -- messages
    self.Config.Messages                = self.Config.Messages or {}
    self.Config.Messages.NoPermission   = self.Config.Messages.NoPermission or "You dont have permission to use this command!"
    -- chat commands
    self.Config.ChatCommands            = self.Config.ChatCommands or {
        ["command1"] = {
            ["text"] = {"This is an example text"},
            ["helptext"] = "This is the helptext for this command",
            ["permission"] = false
        },
        ["command2"] = {
            ["text"] = {"This is an example text for admins only", "You can also use multiline messages"},
            ["helptext"] = "This is the helptext for this command, also admin only",
            ["permission"] = "admin"
        }
    }
    -- update old admin entries to new permission entries added in v2.3.0
    for key, _ in pairs(self.Config.ChatCommands) do
        if self.Config.ChatCommands[key].admin then
            self.Config.ChatCommands[key].admin = nil
            self.Config.ChatCommands[key].permission = "admin"
        elseif self.Config.ChatCommands[key].admin ~= nil then
            self.Config.ChatCommands[key].admin = nil
            self.Config.ChatCommands[key].permission = false
        end
    end
    self:SaveConfig()
end
-- --------------------------------
-- register permissions
-- --------------------------------
function PLUGIN:RegisterPermissions()
    for key, _ in pairs(self.Config.ChatCommands) do
        local perm = self.Config.ChatCommands[key].permission or false
        if perm then
            if not permission.PermissionExists(perm) then
                permission.RegisterPermission(perm, self.Object)
            end
        end
    end
end
-- --------------------------------
-- handles the chat commands
-- --------------------------------
function PLUGIN:cmdChatCmd(player, chatcmd)
    local chatName = self.Config.Settings.ChatName
    for key, _ in pairs(self.Config.ChatCommands) do
        if chatcmd == key then
            local cmd = self.Config.ChatCommands[key]
            if cmd.permission then
                if HasPermission(player, cmd.permission) then
                    for k, _ in pairs(cmd.text) do
                        rust.SendChatMessage(player, chatName, cmd.text[k])
                    end
                else
                    rust.SendChatMessage(player, chatName, self.Config.Messages.NoPermission)
                end
            else
                for k, _ in pairs(cmd.text) do
                    rust.SendChatMessage(player, chatName, cmd.text[k])
                end
            end
        end
    end
end
-- --------------------------------
-- send helptext
-- --------------------------------
function PLUGIN:SendHelpText(player)
    local chatName = self.Config.Settings.ChatName
    for key, _ in pairs(self.Config.ChatCommands) do
        local cmd = self.Config.ChatCommands[key]
        if cmd.helptext and cmd ~= "" then
            if cmd.permission then
                if HasPermission(player, cmd.permission) then
                    rust.SendChatMessage(player, chatName, cmd.helptext)
                end
            else
                rust.SendChatMessage(player, chatName, cmd.helptext)
            end
        end
    end
end