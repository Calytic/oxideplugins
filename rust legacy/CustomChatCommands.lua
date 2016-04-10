PLUGIN.Title        = "Custom Chat Commands"
PLUGIN.Description  = "Set completely custom chat commands"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 0, 0)
PLUGIN.ResourceID   = _

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
local function HasPermission(netuser, perm)
    local steamID = rust.UserIDFromPlayer(netuser)
    if netuser:CanAdmin() then
        return true
    end
    if permission.UserHasPermission(steamID, perm) then
        return true
    end
    return false
end
-- --------------------------------
-- temporary until rust.SendChatMessage() supports chat names
-- --------------------------------
function PLUGIN:SendChatMessage(netuser, message)
    local chatName = self.Config.Settings.ChatName
    global.ConsoleNetworker.SendClientCommand(netuser.networkPlayer, "chat.add "..chatName.." "..rust.QuoteSafe(message).."")
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
function PLUGIN:cmdChatCmd(netuser, chatcmd)
    for key, _ in pairs(self.Config.ChatCommands) do
        if chatcmd == key then
            local cmd = self.Config.ChatCommands[key]
            if cmd.permission then
                if HasPermission(netuser, cmd.permission) then
                    for k, _ in pairs(cmd.text) do
                        self:SendChatMessage(netuser, cmd.text[k])
                    end
                else
                    self:SendChatMessage(netuser, self.Config.Messages.NoPermission)
                end
            else
                for k, _ in pairs(cmd.text) do
                    self:SendChatMessage(netuser, cmd.text[k])
                end
            end
        end
    end
end
-- --------------------------------
-- send helptext
-- --------------------------------
function PLUGIN:SendHelpText(netuser)
    for key, _ in pairs(self.Config.ChatCommands) do
        local cmd = self.Config.ChatCommands[key]
        if cmd.helptext and cmd ~= "" then
            if cmd.permission then
                if HasPermission(netuser, cmd.permission) then
                    rust.SendChatMessage(netuser, cmd.helptext)
                end
            else
                rust.SendChatMessage(netuser, cmd.helptext)
            end
        end
    end
end