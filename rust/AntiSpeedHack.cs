// Reference: Newtonsoft.Json
// Reference: Oxide.Ext.Rust
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AntiSpeedHack", "Steven", 0.2)]
    class AntiSpeedHack : RustPlugin
    {		
		/*
		 * Start Of Configurable options
		 */
		private static int BlockAfterDetectionAmount = 3;
		private static bool AutoBan = true;
		/*
		 * End Of Configurable options
		 */
		
        System.Timers.Timer timer;
		
        private static long GetTimestamp()
        {
            return long.Parse(System.DateTime.Now.ToString("yyyyMMddHHmmssffff"));
        }

		static void SendMsgAdmin(string msg)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<BaseNetworkable>().net.connection.authLevel > 0)
                {
                    player.SendConsoleCommand("chat.add \"AntiCheat\" " + msg.QuoteSafe() + " 1.0");
                }
            }
        }
		
        class LastPosData
        {
            public int Warnings;
			public BasePlayer Player;
			public Vector3 Pos;
            public LastPosData(BasePlayer p)
            {
				Pos = p.transform.position;
				Warnings = 0;
				Player = p;
            }
			
            public bool Check()
            {
                if (Vector3.Distance(Pos, Player.transform.position) >= 35)
                {
                    Warnings++;                   
                } 
				else if(Warnings >= 1)
				{
					Warnings--;
				}
				Pos = Player.transform.position;
                if (Warnings >= BlockAfterDetectionAmount)
                {
                    return true;
                }
                return false;
            }
        };
        private Dictionary<string, LastPosData> AntiSpeedHacking;
		private List<string> Checking;
		int Checks = 4, IndexChecker = 0;
		
		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
			int Count = AntiSpeedHacking.Count;
			if(Count >= 1)
			{
				if(Checks >= 4)
				{
					if(IndexChecker>=Count)
					{
						IndexChecker = 0;
						Checks = 4;
					}
					Checking.Clear();
					int i=1;
					foreach (string key in AntiSpeedHacking.Keys)
					{
						if(i<=IndexChecker) continue;
						if(Checking.Count < 5)
						{
							Checking.Add(key);
							IndexChecker = i;
						} else break;
						i++;
					}
					Checks = 0;
				}
				foreach(string SteamIDCurrentCheck in Checking)
				{
					BasePlayer player = AntiSpeedHacking[SteamIDCurrentCheck].Player;
					if(player)
					{
						if(AntiSpeedHacking[SteamIDCurrentCheck].Check() == true)
						{
							Puts(player.displayName + " was speed hacking");
							SendMsgAdmin(player.displayName + " was detected speed hacking and was banned");
							if(AutoBan)
							{
								ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", player.userID.ToString(), player.displayName, "Banned by Auto Speed.").ToString(),true);
								ConsoleSystem.Run.Server.Quiet("server.writecfg",true);
							}
							Network.Net.sv.Kick(player.net.connection, "Kicked from the server");
						}
					} 
				}
				Checks++;
			}
        }
		void Unload()
		{
			timer.Stop();
		}
        void Loaded()
        {
            AntiSpeedHacking = new Dictionary<string, LastPosData>();
			Checking = new List<string>();
		    timer = new System.Timers.Timer(4000);
            timer.Interval = 4000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
			timer.Start();
        }

        void OnServerInitialized()
        {
        }
		
		void OnPlayerInit(BasePlayer ply)
        {
			if(ply != null)
			{           
				if (AntiSpeedHacking.ContainsKey(ply.userID.ToString()) == false)
				{
					AntiSpeedHacking.Add(ply.userID.ToString(), new LastPosData(ply));
				}
			}
		}
		
		void OnPlayerDisconnected(BasePlayer ply, object connection)
		{
			if(ply != null)
			{
				AntiSpeedHacking.Remove(ply.userID.ToString());
			}
		}
    }
}