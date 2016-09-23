using System;
using System.Collections.Generic;
using System.Text;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("HelpText", "Domestos/Calytic", "2.0.41", ResourceId = 676)]
    class HelpText : CovalencePlugin
    {
        private bool UseCustomHelpText;
        private bool AllowHelpTextFromOtherPlugins;
        private List<object> CustomHelpText;

        private void Loaded()
        {
            UseCustomHelpText = GetConfig<bool>("Settings", "UseCustomHelpText", false);
            AllowHelpTextFromOtherPlugins = GetConfig<bool>("Settings", "AllowHelpTextFromOtherPlugins", true);
            CustomHelpText = GetConfig<List<object>>("Settings","CustomHelpText", new List<object>() {
                "custom helptext",
                "custom helptext"
            });
        }

        protected override void LoadDefaultConfig()
        {
            Config["Settings", "UseCustomHelpText"] = false;
            Config["Settings", "AllowHelpTextFromOtherPlugins"] = true;
            Config["Settings", "CustomHelpText"] = CustomHelpText = new List<object>() {
                "custom helptext",
                "custom helptext"
            };

            SaveConfig();
        }

        [Command("help")]
        void cmdHelp(IPlayer player, string command, string[] args)
        {
            if (player == null) return;

            if (UseCustomHelpText)
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                foreach (var text in CustomHelpText)
                {
                    sb.AppendLine(text.ToString());
                    i++;

                    if (i % 10 == 0)
                    {
                        player.Reply(sb.ToString());
                        sb.Length = 0;
                        i = 0;
                    }
                }

                if (i > 0)
                {
                    player.Reply(sb.ToString());
                }
            }

            if (AllowHelpTextFromOtherPlugins)
            {
                plugins.CallHook("SendHelpText", player.Object);
            }
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private T GetConfig<T>(string name, string name2, T defaultValue)
        {
            if (Config[name, name2] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name, name2], typeof(T));
        }
    }
}