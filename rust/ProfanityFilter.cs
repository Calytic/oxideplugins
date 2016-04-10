using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins {
    [Info("ProfanityFilter", "OwnProx", "1.0.1")]
    class ProfanityFilter : RustPlugin 
	{
		/*
		* Configurable Options
		*/
		string[] IllegalWords = {"fuck", "shit", "cunt", "whore", "nigger", "negro", "barstard", "arsehole", "dickhead", "bollocks", "bitch",
		"slag", "wanker", "bellend"};
		bool TempBanOnBadWords = true, BanOnBadWord = false, KickOnBadWord = true; //set both to false to just ignore the message in chat
		int TempBanTime = 1; //1 = 30 mins, 2 = 60, 3 = 90  and so on
		
		/* Do Not Edit */
		private Dictionary<ulong, float> Bans = new Dictionary<ulong, float>();
		private System.Timers.Timer timer;
		private DateTime NowTimePlease = new DateTime(2016, 2, 2, 0, 0, 0);
		private List<ulong> IdsToRemove = new List<ulong>();
		
		private object OnPlayerChat(ConsoleSystem.Arg arg)
		{
			if(arg != null)
			{
				string playerChat = arg.GetString(0, "text").ToLower();
				BasePlayer player = arg.connection.player as BasePlayer;
				if (player != null && playerChat != null)
				{
					for(int i = 0; i < IllegalWords.Length; i++) 
					if(playerChat.Contains(IllegalWords[i]))
					{
						if(TempBanOnBadWords)
						{
							Bans.Add(player.userID, (float)(GetTimestamp() + (300000f * TempBanTime)));
							Network.Net.sv.Kick(player.net.connection, "Stop swearing you've been banned for " + TempBanTime * 30 + " Minutes!");							
						}
						else
						{					
							if(BanOnBadWord) ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", player.UserIDString, player.displayName, "Banned for swearing!"),true);
							if(BanOnBadWord || KickOnBadWord) Network.Net.sv.Kick(player.net.connection, "Banned for swearing!");
						}
						return "handled";
					}
				}
			}
			return null;
		}
		
		private void OnPlayerInit(BasePlayer player)
		{
			float t = 0f;
			if(Bans.TryGetValue(player.userID, out t) && t > GetTimestamp()) Network.Net.sv.Kick(player.net.connection, "You are still banned for " + (int) Math.Round(((t-GetTimestamp()) / 60000), 0) + " minutes.");
		}
		
		private void Loaded()
        {
			timer = new System.Timers.Timer();
            timer.Interval = 1200000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
			timer.Start();	
        }
		
		private void Unload()
		{
			Bans.Clear();
			timer.Stop();
			timer.Dispose();
		}
		
		private double GetTimestamp()
		{
			return System.DateTime.UtcNow.Subtract(NowTimePlease).TotalMilliseconds;
		}
	
		private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
			double time = GetTimestamp();
			foreach(KeyValuePair<ulong, float> p in Bans) if(time > p.Value) IdsToRemove.Add(p.Key);
			foreach(ulong i in IdsToRemove) Bans.Remove(i);
			IdsToRemove.Clear();
		}
    }
}