using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Oxide.Core;
using UnityEngine;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ChatHead", "LeoCurtss", 0.3)]
    [Description("Displays chat messages above player")]

    class ChatHead : RustPlugin
    {
        //Dictionary - PlayerID - LastMesage
		//On every chat, add Player and message if not present.  Update message if exsists in dictionary
		Dictionary<string, string> lastChatMessage = new Dictionary<string, string>();
		
		void OnPlayerChat(ConsoleSystem.Arg arg)
		{
			BasePlayer player = (BasePlayer) arg.connection.player;
			string userID = player.UserIDString;
			string message = arg.GetString(0,"");
			
			if (lastChatMessage.ContainsKey(userID))
			{
				lastChatMessage[userID] = message;
			}
			else
			{
				lastChatMessage.Add(userID,message);
			}
			
			var Online = BasePlayer.activePlayerList as List<BasePlayer>;
			foreach(BasePlayer onlinePlayer in Online)
			{
				DrawChatMessage(onlinePlayer,player);
			}
		}
		
		void DrawChatMessage (BasePlayer onlinePlayer, BasePlayer chatPlayer)
		{
			float distanceBetween = Vector3.Distance(chatPlayer.transform.position,onlinePlayer.transform.position);
			
			if (distanceBetween <= 20)
			{
				
				string lastMessage = lastChatMessage[chatPlayer.UserIDString];
				Color messageColor = new Color(1,1,1,1);
				
				onlinePlayer.SendConsoleCommand("ddraw.text", 0.1f, messageColor, chatPlayer.transform.position + new Vector3(0, 1.9f, 0),"<size=25>" + lastMessage + "</size>");
				timer.Repeat(0.1f, 80, () =>
				{
					lastMessage = lastChatMessage[chatPlayer.UserIDString];
					onlinePlayer.SendConsoleCommand("ddraw.text", 0.1f, messageColor, chatPlayer.transform.position + new Vector3(0, 1.9f, 0),"<size=25>" + lastMessage + "</size>");
				});
			}
		}

    }
}