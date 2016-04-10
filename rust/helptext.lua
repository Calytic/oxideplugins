PLUGIN.Title        = "Helptext"
PLUGIN.Description  = "Hooks into plugins to send helptext"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 4, 0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceID   = 676

function PLUGIN:Init()
    command.AddChatCommand("help", self.Object, "cmdHelp")
    self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.UseCustomHelpText = self.Config.Settings.UseCustomHelpText or "false"
    self.Config.Settings.AllowHelpTextFromOtherPlugins = self.Config.Settings.AllowHelpTextFromOtherPlugins or "true"
    self.Config.CustomHelpText = self.Config.CustomHelpText or {
       "custom helptext",
       "custom helptext"
    }
    self:SaveConfig()
end

function PLUGIN:cmdHelp(player)
    if not player then return end
    if self.Config.Settings.UseCustomHelpText == "true" then
        for _, helptext in pairs(self.Config.CustomHelpText) do
            rust.SendChatMessage(player, helptext)
        end
    end
    if self.Config.Settings.AllowHelpTextFromOtherPlugins == "true" then
        plugins.CallHook("SendHelpText", util.TableToArray({player}))
    end
end