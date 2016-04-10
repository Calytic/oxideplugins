using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Notice", "LaserHydra", "1.0.1", ResourceId = 1193)]
    [Description("Notice players anonymously")]
    class Notice : RustPlugin
    {
        void Loaded()
        {
            LoadDefaultConfig();
            if (!permission.PermissionExists("canNotice")) permission.RegisterPermission("canNotice", this);
        }

        protected override void LoadDefaultConfig()
        {
			if(Config["Prefix"] == null) Config["Prefix"] = "<color=red>ADMIN</color>";
			if(Config["Prefix"].ToString() != "<color=red>ADMIN</color>") return;
            SaveConfig();
        }
		
        [ChatCommand("notice")]
        void cmdNotice(BasePlayer player, string cmd, string[] args)
        {
            string uid = player.userID.ToString();
            if (!permission.UserHasPermission(uid, "canNotice"))
            {
                SendChatMessage(player, "NOTICE", "You have no permission to use this command!");
                return;
            }

            if (args.Length < 2)
            {
                SendChatMessage(player, "NOTICE", "Syntax: /notice <player> <message>");
                return;
            }

            string msg = ArrayToString(args, 1);
            string prefix = Config["Prefix"].ToString();
            BasePlayer targetPlayer = GetPlayer(args[0], player, "NOTICE");
            if (targetPlayer != null)
            {
                SendChatMessage(targetPlayer, prefix, msg);
                SendChatMessage(player, "NOTICE", "Message sent!");
            }
        }

        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
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

            if (matchingPlayers.Length == 0)
            {
                SendChatMessage(executer, prefix, "No matching players found!");
            }

            if (matchingPlayers.Length > 1)
            {
                SendChatMessage(executer, prefix, "Multiple players found:");
                string multipleUsers = "";
                foreach (string matchingplayer in matchingPlayers)
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
                SendChatMessage(executer, prefix, multipleUsers);
            }

            if (matchingPlayers.Length == 1)
            {
                targetPlayer = BasePlayer.Find(matchingPlayers[0]);
            }
            return targetPlayer;
        }

        //---------------------------->   Converting   <----------------------------//

        string ArrayToString(string[] array, int first)
        {
            int count = 0;
            string output = array[first];
            foreach (string current in array)
            {
                if (count <= first)
                {
                    count++;
                    continue;
                }

                output = output + " " + current;
                count++;
            }
            return output;
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
        #endregion
    }
}
