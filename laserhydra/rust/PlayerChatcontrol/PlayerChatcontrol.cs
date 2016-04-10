using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Player Chatcontrol", "LaserHydra", "2.0.0", ResourceId = 866)]
    [Description("Write as another player")]
    class PlayerChatcontrol : RustPlugin
    {
        [ChatCommand("talk")]
        void cmdTalk(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel != 2)
            {
                SendChatMessage(player, "CHATCONTROL", "You have no permission to use this command");
                return;
            }

            if (args.Length < 2)
            {
                SendChatMessage(player, "CHATCONTROL", "Syntax: /talk [player] [message]");
                return;
            }

            string msg = "";
            foreach (string arg in args)
            {
                if (arg == args[0])
                {
                    continue;
                }

                if (msg == "")
                {
                    msg = msg + arg;
                    continue;
                }

                msg = msg + " " + arg;
            }

            string[] target;
            target = GetPlayer(args[0]);
            if (target.Length == 0)
            {
                SendChatMessage(player, "CHATCONTROL", "No matching players found!");
            }

            if (target.Length > 1)
            {
                SendChatMessage(player, "CHATCONTROL", "Multiple players found:");
                string multipleUsers = "";
                foreach (string matchingplayer in target)
                {
                    if (multipleUsers == "")
                    {
                        multipleUsers = "<color=yellow>" + matchingplayer + "</color>";
                        continue;
                    }

                    if (multipleUsers != "")
                    {
                        multipleUsers = multipleUsers + ", " + "<color=yellow>" + matchingplayer + "</color>";
                    }

                }

                SendChatMessage(player, "CHATCONTROL", multipleUsers);
            }

            if (target.Length == 1)
            {
                BasePlayer targetPlayer = BasePlayer.Find(target[0]);
                ForcePlayerChat(targetPlayer, msg);
            }
        }

        void ForcePlayerChat(BasePlayer target, string message)
        {
            target.SendConsoleCommand("chat.say " + "\"" + message + "\"");
        }


        //--------------------------->   Player finding   <---------------------------//

        string[] GetPlayer(string searchedPlayer)
        {
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string display = player.displayName;
                string displayLower = display.ToLower();

                if (!displayLower.Contains(searchedLower))
                {
                    continue;
                }
                if (displayLower.Contains(searchedLower))
                {
                    foundPlayers.Add(display);
                }
            }
            var matchingPlayers = foundPlayers.ToArray();
            return matchingPlayers;
        }

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg)
        {
            PrintToChat("<color=orange>" + prefix + "</color>: " + msg);
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg)
        {
            SendReply(player, "<color=orange>" + prefix + "</color>: " + msg);
        }

        //---------------------------------------------------------------------------//
    }
}