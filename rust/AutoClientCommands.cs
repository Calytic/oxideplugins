using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Automatic Client Commands", "k1lly0u", "0.1.0", ResourceId = 0)]
    class AutoClientCommands : RustPlugin
    {
        #region Oxide Hooks
        void OnServerInitialized()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"addSuccess", "You have successfully added a new automatic client command: {0}" },
                {"remSuccess", "You have successfully remove the automatic client command: {0}" },
                {"noFind", "Unable to find the command: {0}" }
            }, this);

            LoadVariables();

            foreach (var player in BasePlayer.activePlayerList)
                RunCommands(player);
        }
        void OnPlayerInit(BasePlayer player) => RunCommands(player);
        #endregion

        #region Functions
        private void RunCommands(BasePlayer player)
        {
            foreach(var entry in configData.Commands)
                player.SendConsoleCommand(entry);
        }
        [ConsoleCommand("addcommand")]
        private void ccmdAddCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                if (arg.Args != null && arg.Args.Length > 0)
                {
                    configData.Commands.Add(arg.Args[0]);
                    SendReply(arg, string.Format(lang.GetMessage("addSuccess", this), arg.Args[0]));
                    SaveConfig(configData);
                }
            }
        }
        [ConsoleCommand("removecommand")]
        private void ccmdRemoveCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                if (arg.Args != null && arg.Args.Length > 0)
                {
                    if (configData.Commands.Contains(arg.Args[0]))
                    {
                        configData.Commands.Remove(arg.Args[0]);
                        SendReply(arg, string.Format(lang.GetMessage("remSuccess", this), arg.Args[0]));
                        SaveConfig(configData);
                        return;
                    }
                    SendReply(arg, string.Format(lang.GetMessage("noFind", this), arg.Args[0]));
                }
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public List<string> Commands { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Commands = new List<string>
                {
                    "graphics.branding false"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}


