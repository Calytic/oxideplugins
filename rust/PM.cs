using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using System.Text;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins 
{
    [Info("PM", "Steven", "1.0.2", ResourceId = 8906)]	
    class PM : RustPlugin 
	{
		bool FindByHoleName = false;
		
		Dictionary<ulong, ulong> PmHistory = new Dictionary<ulong, ulong>();		
		[HookMethod("OnPlayerDisconnected")]
		void OnPlayerDisconnected(BasePlayer player)
		{
			if(PmHistory.ContainsKey(player.userID)) PmHistory.Remove(player.userID);
		}		
		[ChatCommand("pm")]
        void cmdPm(BasePlayer player, string command, string[] args)
		{
			if(args.Length > 1)
			{
				string Player = args[0].ToLower(), Msg = "";
				for(int i = 1; i < args.Length; i++)				
					Msg = Msg + " " + args[i];
				BasePlayer p;
				if(FindByHoleName && (p = BasePlayer.activePlayerList.Find(x => x.displayName.ToLower().EndsWith(Player))) != null || (p = BasePlayer.activePlayerList.Find(x => x.displayName.ToLower().Contains(Player))) != null ) //used ends with due to clan tags
				{
					if(PmHistory.ContainsKey(player.userID)) PmHistory[player.userID] = p.userID; else PmHistory.Add(player.userID, p.userID);
					if(PmHistory.ContainsKey(p.userID)) PmHistory[p.userID] = player.userID; else PmHistory.Add(p.userID, player.userID);
					SendReply(player, "<color=#00FFFF>PM to " + p.displayName + "</color>: "+ Msg);
					SendReply(p, "<color=#00FFFF>PM from " + player.displayName + "</color>: "+ Msg);
				}
				else SendReply(player, Player+" is not online.");
			} else  SendReply(player, "Incorrect Syntax use: /pm <name> <msg>");
		}
		
		[ChatCommand("r")]
        void cmdPmReply(BasePlayer player, string command, string[] args)
		{
			if(args.Length > 0)
			{
				string Msg = "";
				for(int i = 0; i < args.Length; i++)				
					Msg = Msg + " " + args[i];
				ulong steamid;
				if(PmHistory.TryGetValue(player.userID, out steamid))
				{
					BasePlayer p;
					if((p = BasePlayer.activePlayerList.Find(x => x.userID == steamid)) != null)
					{
						SendReply(player, "<color=#00FFFF>PM to " + p.displayName + "</color>: "+ Msg);
						SendReply(p, "<color=#00FFFF>PM from " + player.displayName + "</color>: "+ Msg);
					}
					else SendReply(player, "the last person you was talking to is not online anymore.");
				} else SendReply(player, "You haven't messaged anyone or they haven't messaged you.");
			} else  SendReply(player, "Incorrect Syntax use: /r <msg>");
		}
    }
}