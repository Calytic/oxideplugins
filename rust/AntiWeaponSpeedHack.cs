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
    [Info("AntiWeaponSpeedHack", "Steven", 0.5)]
    class AntiWeaponSpeedHack : RustPlugin
    {
		/*
		 * Start Of Configurable options
		 */
		private static int BlockAfterDetectionAmount = 10;
		private static bool AutoBan = true;
		/*
		 * End Of Configurable options
		 */
		
		
        private static int[] Limits = new int[10] { 8000, 4000, 18000, 10000, 21000, 13000, 13500, 700, 0, 0 };

        private static long GetTimestamp()
        {
            return long.Parse(System.DateTime.Now.ToString("yyyyMMddHHmmssffff"));
        }

        private struct LastWepData
        {
            public long[] time;
			public long[] LastTimeDiff;
            public int[] Warnings;
            public LastWepData(int k)
            {
                time = new long[10];
                LastTimeDiff = new long[10];
                Warnings = new int[10];
                for (int i = 0; i < 10; i++)
                {
					LastTimeDiff[i] = 0;
                    time[i] = 0;
                    Warnings[i] = 0;
                }
            }
            public bool Check(int ID)
            {
                long nowTime = GetTimestamp();
				LastTimeDiff[ID] = (nowTime - time[ID]);
                if (LastTimeDiff[ID] <= Limits[ID])
                {
                    Warnings[ID]++;                   
                } 
				else if(Warnings[ID] >= 1)
				{
					Warnings[ID] = 0;
				}
				time[ID] = nowTime;
                if (Warnings[ID] >= BlockAfterDetectionAmount)
                    return true;
                return false;
            }
        };
        private Dictionary<BaseEntity, LastWepData> AntiWeaponSpeedHacking;

        void Loaded()
        {
            AntiWeaponSpeedHacking = new Dictionary<BaseEntity, LastWepData>();
        }

        void OnServerInitialized()
        {
		}
		
        void SendMsgAdmin(string msg)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.GetComponent<BaseNetworkable>().net.connection.authLevel > 0)
                {
                    player.SendConsoleCommand("chat.add \"AntiCheat\" " + msg.QuoteSafe() + " 1.0");
                }
            }
        }
		
        void OnPlayerAttack(BasePlayer player, HitInfo info)
        {
            if (player != null && player.svActiveItem != null && info != null && info.HitEntity != null)
            {
                int Type = -1;
                switch (player.svActiveItem.info.shortname.ToString())
                {
					case "torch":
					case "hammer_salvaged":
                    case "rock":
                        Type = 0;
                        break;
					case "knife_bone":
					case "stonehatchet":
                    case "hatchet":
                        Type = 1;
                        break;
                    case "pickaxe":
                        Type = 2;
                        break;
					case "spear_stone":
					case "spear_wooden":
					case "ice_salvaged":
					case "axe_salvaged":
					case "pistol_revolver":
						Type = 3;
						break;
					case "bow_hunting":
						Type = 4;
						break;
					case "pistol_eoka":
						Type = 5;
						break;
					case "rifle_bolt":
						Type = 6;
						break;
					case "smg_thompson":
					case "rifle_ak":
						Type = 7;
						break;
                }

                if (Type != -1)
                {
                    if (AntiWeaponSpeedHacking.ContainsKey(player) == false)
                    { 
                        AntiWeaponSpeedHacking.Add(player, new LastWepData(0)); 
                    }
					
                    if(AntiWeaponSpeedHacking[player].Check(Type) == true)
                    {
                        Puts(player.displayName + " was weapon speed hacking");
                        SendMsgAdmin(player.displayName + " was detected weapon speed hacking and was banned");
                        if(AutoBan)
						{
							ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", player.userID.ToString(), player.displayName, "Auto Weapon Speed (" + player.svActiveItem.info.shortname.ToString() + ") hit in " + AntiWeaponSpeedHacking[player].LastTimeDiff[Type] + "/" +Limits[Type]+ " " + BlockAfterDetectionAmount +" Times.").ToString(),true);
							ConsoleSystem.Run.Server.Quiet("server.writecfg",true);
						}
						Network.Net.sv.Kick(player.net.connection, "Kicked from the server");
                    }
                }
            }
        }
		
		void OnPlayerDisconnected(BasePlayer ply, object connection)
		{
			if(ply != null)
			{
				AntiWeaponSpeedHacking.Remove(ply);
			}
		}
    }
}