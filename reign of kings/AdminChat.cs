using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using Oxide.Core;
using CodeHatch.Engine.Networking;
using CodeHatch.Common;

namespace Oxide.Plugins
{
    [Info("Admin Chat", "LaserHydra", "1.1.0", ResourceId = 1152)]
    [Description("Chat with admins only")]
    class AdminChat : ReignOfKingsPlugin
    {
        public void Loaded()
        {
            //if (!permission.PermissionExists("canAdminChat")) permission.RegisterPermission("canAdminChat", this);
            //List<string> adminChat = new List<string>();
            LoadDefaultConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            Config["Prefix"] = "[E70000][[2B2B2B]ADMINCHAT[E70000]]";
            Config["NameColor"] = "E70000";
            SaveConfig();
        }

        [ChatCommand("a")]
        void cmdToggleAdminChat(Player player, string cmd, string[] args)
        {
            string uid = Convert.ToString(player.Id);
            //if (!permission.UserHasPermission(uid, "canAdminChat") || player.IsAdmin())
            if (!CodeHatch.Common.PlayerExtensions.HasPermission(player, "canAdminChat"))
            {
                SendChatMessage(player, "ADMINCHAT", "You have no permission to use this command!");
                return;
            }

            if (args == null || args.Length < 1)
            {
                SendChatMessage(player, "ADMINCHAT", "Syntax: /a [message]");
            }

            string allArgs, message;
            allArgs = Convert.ToString(args[0]);

            foreach (string arg in args)
            {
                if (arg == Convert.ToString(args[0]))
                {
                    continue;
                }

                allArgs = allArgs + " " + arg;
            }

            foreach (Player current in CodeHatch.Engine.Networking.Server.ClientPlayers)
            {
                string currId = Convert.ToString(current.Id);
                //if (!permission.UserHasPermission(currId, "canAdminChat") || player.IsAdmin())
                if (CodeHatch.Common.PlayerExtensions.HasPermission(current, "canAdminChat"))
                {
                    SendChatMessage(current, Config["Prefix"] + " " + "[" + Config["NameColor"] + "]" + player.Name, allArgs);
                }
            }

            /*if (adminChat.Exists(uid))
            {
                adminChat.Remove(uid);
                SendChatMessage(player, "ADMINCHAT", "Only admins can see your messages now!");
            }
            else
            {
                adminChat.Add(uid);
                SendChatMessage(player, "ADMINCHAT", "All players can see your messages again!");
            }*/
        }

        /*bool OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.connection.player;
            string uid = Convert.ToString(player.userID);
            string msg = arg.GetString(0, "text");

            if (adminChat.Exists(uid))
            {
                foreach (BasePlayer current in BasePlayer.activePlayerList)
                {
                    string currId = Convert.ToString(current.userID);
                    if (!permission.UserHasPermission(currId, "canAdminChat") || player.IsAdmin())
                    {
                        SendChatMessage(player, "<color=red>[<color=2B2B2B>ADMINCHAT</color>] " + player.displayName + "</color>", msg);
                    }
                }
                return false;
            }

            if (msg.StartsWith == "@")
            {
                foreach (BasePlayer current in BasePlayer.activePlayerList)
                {
                    string currId = Convert.ToString(current.userID);
                    if (!permission.UserHasPermission(currId, "canAdminChat") || player.IsAdmin())
                    {
                        SendChatMessage(player, "<color=red>[<color=2B2B2B>ADMINCHAT</color>] " + player.displayName + "</color>", msg);
                    }
                }
                return false;
            }
        }*/
        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("[FF9A00]" + prefix + "[FFFFFF]: " + msg);
        }

        void SendChatMessage(Player player, string prefix, string msg)
        {
            SendReply(player, "[FF9A00]" + prefix + "[FFFFFF]: " + msg);
        }

        //---------------------------------------------------------------------------//
    }
}
