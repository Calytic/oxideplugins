using System;
using System.Collections.Generic;
using System.Text;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HelpText", "Domestos/Calytic", "2.0.1", ResourceId = 676)]
    class HelpText : CovalencePlugin
    {
        private bool UseCustomHelpText;
        private bool AllowHelpTextFromOtherPlugins;
        private List<object> CustomHelpText;

        private void Loaded()
        {
            this.UseCustomHelpText = GetConfig<bool>("Settings", "UseCustomHelpText", false);
            this.AllowHelpTextFromOtherPlugins = GetConfig<bool>("Settings", "AllowHelpTextFromOtherPlugins", true);
            this.CustomHelpText = GetConfig<List<object>>("CustomHelpText", new List<object>() {
                "custom helptext",
                "custom helptext"
            });
        }

        protected override void LoadDefaultConfig()
        {
            Config["UseCustomHelpText"] = false;
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
                        sb.Clear();
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
                var gameObject = player.Character.Object;
                if(gameObject is GameObject) {
				    plugins.CallHook("SendHelpText", (gameObject as GameObject).GetComponent<BasePlayer>());
                }
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