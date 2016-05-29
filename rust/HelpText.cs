using System;
using System.Collections.Generic;
using System.Text;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("HelpText", "Domestos/Calytic", "2.0.0", ResourceId = 676)]
	class HelpText : CovalencePlugin
    {
        private bool UseCustomHelpText;
        private bool AllowHelpTextFromOtherPlugins;
        private List<object> CustomHelpText;

        private void Loaded()
        {
            this.UseCustomHelpText = GetConfig<bool>("Settings","UseCustomHelpText", false);
            this.AllowHelpTextFromOtherPlugins = GetConfig<bool>("Settings","AllowHelpTextFromOtherPlugins", true);
            this.CustomHelpText = GetConfig<List<object>>("CustomHelpText", new List<object>() {
                "custom helptext",
                "custom helptext"
            });
        }

        protected override void LoadDefaultConfig ()
		{
			Config["UseCustomHelpText"] = false;
            Config["Settings","AllowHelpTextFromOtherPlugins"] = true;
            Config["Settings","CustomHelpText"] = CustomHelpText = new List<object>() {
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
                foreach (var text in CustomHelpText)
                {
                	player.Reply(text.ToString());
                }
            }

            if (AllowHelpTextFromOtherPlugins)
            {
				#if RUST
				plugins.CallHook("SendHelpText", player.ConnectedPlayer.Character.Object);
	            #endif
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
