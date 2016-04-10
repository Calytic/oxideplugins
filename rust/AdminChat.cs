using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Admin Chat", "LaserHydra", "1.3.0", ResourceId = 1123)]
    [Description("Chat with admins only")]
    class AdminChat : RustPlugin
    {	
		#region Loaded
		//	On Plugin-Load
        void Loaded()
        {
			if(!permission.PermissionExists("adminchat.use")) permission.RegisterPermission("adminchat.use", this);
			LoadDefaultConfig();
        }
		#endregion
		
		#region Config
		//	Load Config
        protected override void LoadDefaultConfig()
        {
			if(Config["Prefix"] == null) Config["Prefix"] = "<color=red>[</color><color=#2B2B2B>ADMINCHAT</color><color=red>]</color>";
			if(Config["Prefix"].ToString() != "<color=red>[</color><color=#2B2B2B>ADMINCHAT</color><color=red>]</color>") return;
			
			if(Config["NameColor"] == null) Config["NameColor"] = "red";
			if(Config["NameColor"].ToString() != "red") return;

            SaveConfig();
        }
		#endregion
		
		#region AdminChat Command
		//	AdminChat command
        [ChatCommand("a")]
        void cmdAdminChat(BasePlayer player, string cmd, string[] args)
        {
            string uid = Convert.ToString(player.userID);
			
            if (!permission.UserHasPermission(uid, "adminchat.use"))
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

			SendAdminMessage(player.displayName, allArgs);
        }
		#endregion
				
		#region Useful Methods
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

        string ListToString(List<string> list)
        {
			string output;
			if(list.Count != 0)
			{
				output = list[0];
				foreach (string current in list)
				{
					if (current == list[0])
					{
						continue;
					}

					output = output + " " + current;
				}
			}
			else
			{
				output = "";
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
		
		// 	Send a message into the AdminChat
		void SendAdminMessage(string name, string msg)
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
				if (permission.UserHasPermission(current.userID.ToString(), "adminchat.use"))
				{
					SendChatMessage(current, Config["Prefix"] + " <color=" + Config["NameColor"] + ">" + name + "</color>", msg);
				}
            }
		}

        //---------------------------------------------------------------------------//
		#endregion
    }
}
