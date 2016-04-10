PLUGIN.Title        = "Helptext"
PLUGIN.Description  = "Hooks into plugins to send helptext"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 0, 1)
PLUGIN.ResourceID   = 962

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

function PLUGIN:cmdHelp(netuser)
    if not netuser then return end
    if self.Config.Settings.UseCustomHelpText == "true" then
        for _, helptext in pairs(self.Config.CustomHelpText) do
            rust.SendChatMessage(netuser, helptext)
        end
    end
    if self.Config.Settings.AllowHelpTextFromOtherPlugins == "true" then
        plugins.CallHook("SendHelpText", util.TableToArray({netuser}))
    end
end