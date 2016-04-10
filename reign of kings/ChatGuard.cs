using System.Collections.Generic;
using System.Reflection;
using System;
using System.Text.RegularExpressions;
using System.Data;
using UnityEngine;
using Oxide.Core;
using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;

namespace Oxide.Plugins
{
    [Info("Chat Guard", "LaserHydra", "1.1.0", ResourceId = 1158)]
    [Description("Censor unwanted words/symbols from the chat")]
    class ChatGuard : ReignOfKingsPlugin
    {
        void Loaded()
        {
            LoadDefaultConfig();
        }

        protected override void LoadDefaultConfig()
        {
			SetConfig("WordFilter", "FilterList", new List<string>{"fuck", "bitch", "faggot"});
            
            SaveConfig();
        }
		
		string GetFilteredMesssage(string msg)
		{
			foreach(var word in Config["WordFilter", "FilterList"] as List<object>)
			{
				MatchCollection matches = new Regex(@"((?i)(?:\S+)?" + word + @"?\S+)").Matches(msg);
				
				foreach(Match match in matches)
				{
					
					if(match.Success)
					{
						string found = match.Groups[1].ToString();
						string replaced = "";
						
						for(int i = 0; i < found.Length; i++) replaced = replaced + "*";
						
						msg = msg.Replace(found, replaced);
					}
					else
					{
						break;
					}
				}
			}
			
			return msg;
		}
		
        void OnPlayerChat(PlayerEvent e)
        {
            List<string> ForbiddenWordsList = Config["ForbiddenWords"] as List<string>;
            Player player = e.Player;
            string message = e.ToString();
            string censored = "";
            bool isCensored = false;
            if (message.StartsWith("/"))
            {
                return;
            }
            
			string filteredMessage = GetFilteredMesssage(message);
			
            if (message != filteredMessage)
            {
                BroadcastChat(player.DisplayName, filteredMessage);
                e.Cancel();
            }
        }

        #region UsefulMethods
		//------------------------------->   Config   <-------------------------------//
		
		void SetConfig(string GroupName, string DataName, object Data)
        {
			Config[GroupName, DataName] = Config[GroupName, DataName] ?? Data;
        }
		
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
        #endregion
    }
}
