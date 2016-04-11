using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Commandblock", "Pilatus9", 1.1)]
    [Description("Block some console commands")]

    class Commandblock : RustLegacyPlugin
    {
        public static string prefix = "Oxide";
        public static List<object> block = new List<object>() { "suicide", "status", "unbanall" };


                void LoadDefaultMessages()
                {
                    var messages = new Dictionary<string, string>
                    {
                        {"CommandIsBlocked", "[color red]This command is blocked."}
                    };
                    lang.RegisterMessages(messages, this);
                }

                void LoadDefaultConfig() { }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Loaded()
        {
            LoadDefaultMessages();   
        }


        void Init()
        {
            CheckCfg<string>("Chat Prefix", ref prefix);
            CheckCfg<List<object>>("Global blocked commands (needs to be lowercase)", ref block);
            SaveConfig();
        }
        


        object OnRunCommand(ConsoleSystem.Arg arg, bool shouldAnswer)
        {
            if (arg == null) return null;
            if (arg.argUser == null) return null;
            string command;
            if (arg.Class != "global")
            {
                command = arg.Class + "." + arg.Function;
                if (!block.Contains(command)) return null;
            }
            else
            {
                if (!block.Contains(arg.Function)) return null;
            }
            NetUser netuser = arg.argUser.connection.netUser;
            rust.SendChatMessage(netuser, prefix, GetMessage("CommandIsBlocked", netuser.userID.ToString()));
            return false;
        }
  

        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}