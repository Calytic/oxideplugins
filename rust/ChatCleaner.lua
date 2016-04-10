PLUGIN.Title = "Chat Cleaner"
PLUGIN.Version = V(0, 3, 1)
PLUGIN.Description = "Clears or resets player's chat when joining the server and on command."
PLUGIN.Author = "Wulf / Luke Spragg"
PLUGIN.Url = "http://oxidemod.org/plugins/1183/"
PLUGIN.ResourceId = 1183

--[[ Do NOT edit the config here, instead edit ChatCleaner.json in oxide/config ! ]]

local messages, settings
function PLUGIN:LoadDefaultConfig()
    self.Config.Messages = self.Config.Messages or {}
    messages = self.Config.Messages
    messages.Cleared = messages.Cleared or "<color=orange><size=18><b><i>Chat Cleared!</i></b></size></color>"
    messages.Welcome = messages.Welcome or "<color=orange><size=20><b>Welcome to {server}!</b></size></color>"

    self.Config.Settings = self.Config.Settings or {}
    settings = self.Config.Settings
    settings.ChatCommand = settings.ChatCommand or "clear"
    settings.ClearOnJoin = settings.ClearOnJoin or "true"
    settings.ClearedMessage = settings.ClearedMessage or "true"
    settings.RestoreChat = settings.RestoreChat or "false"
    settings.WelcomeMessage = settings.WelcomeMessage or "true"

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(settings.ChatCommand, self.Plugin, "ClearChat")
end

function PLUGIN:ClearChat(player, cmd)
    local i = 1; while i <= 14 do rust.SendChatMessage(player, messages.Cleared); i = i + 1 end

    local magic = messages.Cleared .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
        .. "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"
    rust.SendChatMessage(player, magic)

    if cmd and settings.ClearedMessage == "true" then rust.SendChatMessage(player, messages.Cleared) end
    if cmd and settings.RestoreChat == "true" then self:RestoreChat(player) end
end

function PLUGIN:RestoreChat(player)
    local chatHandler = plugins.Find("chathandler")
    if not chatHandler then
        print("[" .. self.Title .. "] History cannot be restored, please install http://oxidemod.org/plugins/707/")
        return
    end
    chatHandler:CallHook("cmdHistory", player)
end

function PLUGIN:OnPlayerInit(player)
    if settings.ClearOnJoin == "true" then self:ClearChat(player) end

    if settings.WelcomeMessage == "true" then
        local message = messages.Welcome:gsub("{server}", ConVar.Server.hostname)
        rust.SendChatMessage(player, message)
    end
end
