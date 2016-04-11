using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using Rust;
using Network;
using System.Reflection;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Time", "Tsunderella", "1.2.0")]
    [Description("Adds Time Command.")]
    class TimeSet : RustPlugin
    {
		// bug when you type '/time freeze' with no number.
		bool readyToCheck = false;
		private float toff;
		private float number;
		private float secondnumber;
		private bool cooldown;
		private string meridiem;
		private string Timeofday;
		private string arguments;
		private bool TimeStopped;
		private float TimeStoppedAt; 
		HashSet<ulong> users = new HashSet<ulong>();
		
		
		
       
/*         protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["TimeStopped"] = false;
            Config["TimeStoppedAt"] = 0f;
			Config.Save();
		} */
		

        void Init()
        {

			//TimeStopped=(bool) Config["TimeStopped"];
			//TimeStoppedAt=(float) Config["TimeStopped"];
			TimeStopped=false;
            permission.RegisterPermission("TimeSet.use", this);
			readyToCheck=true;
			cooldown=false;
        }
		void Unloaded()
		{
/* 			Config.Clear();
            Config["TimeStopped"] = TimeStopped;
            Config["TimeStoppedAt"] = TimeStoppedAt;
			Config.Save(); */
		}
		[ChatCommand("time")]
        void cmdtTime(BasePlayer player, string cmd, string[] args)
		{
			 
			if (args.Length <= 0){
				player.SendConsoleCommand("chat.add", "76561198295970834",string.Format(GetMessage("time_get", player.UserIDString),Timeofday), 1.0);
			}else if(args[0]=="help"){player.SendConsoleCommand("chat.add", "76561198295970834",GetMessage("time_help", player.UserIDString),1.0);
			}else{ 
				arguments = args[0];
				if (!pPerm(player, "TimeSet.use")) return;
				float.TryParse(arguments, out number);

				if((0<number)&&(number<=24)){
					TimeStopped=false;
					TOD_Sky.Instance.Cycle.Hour=number;
					timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_set", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));  
				}else if(arguments=="day"){
					TimeStopped=false;
					TOD_Sky.Instance.Cycle.Hour=6.00f;timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_set", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));   
				}else if(arguments=="night"){
					TimeStopped=false;
					TOD_Sky.Instance.Cycle.Hour=19.5f;timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_set", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));   
				}else if(arguments=="unfreeze"){
					TimeStopped=false;
					cooldown=false;
/* 					Config.Clear();
					Config["TimeStopped"] = TimeStopped;
					Config["TimeStoppedAt"] = 0f;
					Config.Save(); */
					timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_unfreeze", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834")); 
				}else if(arguments=="freeze"){
					if (args.Length ==2){
						float.TryParse(args[1], out secondnumber);
						
					}else{
						TimeStoppedAt=TOD_Sky.Instance.Cycle.Hour;
						TimeStopped=true;
						cooldown=false;
						timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_freeze", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));	
						return;
					}
					if((0<=secondnumber)&&(secondnumber<=23.74)){
						TimeStoppedAt=secondnumber;
						TimeStopped=true;
						cooldown=false;
/* 						Config.Clear();
						Config["TimeStopped"] = TimeStopped;
						Config["TimeStoppedAt"] = TimeStoppedAt;
						Config.Save(); */
						timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_freeze", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));
					}else{
						player.SendConsoleCommand("chat.add", "76561198295970834",string.Format(GetMessage("time_freezeerror", player.UserIDString),arguments),1.0); 	
					}
				}else{
					player.SendConsoleCommand("chat.add", "76561198295970834",string.Format(GetMessage("time_error", player.UserIDString),arguments),1.0); 	
				}
			}
		} 
		[ChatCommand("day")]
        void cmdtDay(BasePlayer player){
			string id = rust.UserIDFromPlayer(player);
			if (!pPerm(player, "TimeSet.use")) return;
			TOD_Sky.Instance.Cycle.Hour=6.501f;
            timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_set", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));  		
		}
		[ChatCommand("night")] 
        void cmdtNight(BasePlayer player){
			string id = rust.UserIDFromPlayer(player); 
			if (!pPerm(player, "TimeSet.use")) return;  
			TOD_Sky.Instance.Cycle.Hour=18.501f;	
            timer.Once(1f, () => rust.BroadcastChat(string.Format(GetMessage("time_set", player.UserIDString),Timeofday,player.displayName),null,"76561198295970834"));  		
		}


        [HookMethod("OnTick")]
        private void OnTick() {
            try {
				
                if (readyToCheck){
					
					float Time=TOD_Sky.Instance.Cycle.Hour;
					DateTime dt = TOD_Sky.Instance.Cycle.DateTime;
                    if(Time>=13f){toff=-12f;}else if(Time<=1f){toff=12f;}else{toff=0f;}
					if(Time<12f){meridiem="am";}else{meridiem="pm";}
					string[] splitArray = (Time.ToString()).Split('.');
					Timeofday = (float.Parse(splitArray[0])+toff)+":"+(dt.ToString("mm"))+meridiem;
					
					if(TimeStopped&&!cooldown){
						
						TOD_Sky.Instance.Cycle.Hour=TimeStoppedAt;
						cooldown=true;
						timer.Once(10f, () => cooldown=false);
					}
					
				
				
				}
			}
            catch (Exception error) {
                PrintError("{0}: {1}", Title,"OnTick failed: " + error.Message);
            } 
        }
        [HookMethod("whatisthetime")]
        private string whatisthetime()
        {
             return Timeofday;
        }
        void Loaded()
        {
            Interface.Oxide.DataFileSystem.WriteObject("TimeSet", users);
            lang.RegisterMessages(new Dictionary<string, string>
            { 
                ["time_get"] = "The current time is {0}.",
                ["time_noperm"] = "You do not have the TimeSet.use permission!",
				["time_help"] = "/time - Tells what time it is.\n/time help - Bring up this menu.\n/time <0-24/day/night/freeze/unfreeze> - Sets the time or freezes time if you have the permission.\n/day - Sets the time to 6am if you have permission.\n/night - Sets the time to 7:30pm if you have permission.",
                ["time_error"] = "{0} is not a valid parameter!",
				["time_freeze"] = "{1} has frooze the time to {0}.",
				["time_freezeerror"] = "Please enter a valid time to freeze.",
				["time_unfreeze"] = "{1} has unfrooze the time.",
                ["time_set"] = "{1} has set the time to {0}."
            }, this); 
        }
        string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }

		bool pPerm(BasePlayer player, string perm){
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
			player.SendConsoleCommand("chat.add", "76561198295970834", GetMessage("time_noperm", player.UserIDString),1.0);
            return false;
        }

	}
}