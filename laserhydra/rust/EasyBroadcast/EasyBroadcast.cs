using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Easy Broadcast", "LaserHydra", "2.1.0", ResourceId = 863)]
    [Description("Broadcast a message to the server")]
    class EasyBroadcast : RustPlugin
    {
        ////////////////////////////////////////
        ///     On Plugin Loaded
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("broadcast.use", this);

            LoadConfig();
        }

        ////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Display", "<color=blue>{title}</color>: {message}");
            SetConfig("Title", "Server");

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
        }

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        [ChatCommand("bcast")]
        void cmdBroadcast(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "broadcast.use"))
            {
                SendChatMessage(player, "Broadcast", "You have no permission to use this command!");
                return;
            }
            if (args.Length < 1)
            {
                SendChatMessage(player, "Broadcast", "Syntax: /bcast <message>");
            }

            string message = Config["Display"] as string;
            message = message.Replace("{title}", Config["Title"] as string).Replace("{message}", ListToString(args.ToList(), 0, " "));

            BroadcastChat(message);
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);
    }
}
