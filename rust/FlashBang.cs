using UnityEngine;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using System.Reflection;
using Oxide.Core;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("FlashBang", "PaiN", "0.5", ResourceId = 1283)]
    [Description("Replaces grenade with a flashbang grenade.")]
    class FlashBang : RustPlugin
    {
		
		class Data
		{
			public List<FlashInfo> FlashInfo = new List<FlashInfo>{};
		} 
		 
		Data data;
		
		class FlashInfo
		{
			public int ID;
			public int Duration;
			public string Power;
			public int Radius;
	
			public FlashInfo(int num, int dur, string pow, int rad)
			{
				ID = num;
				Duration = dur;
				Power = pow;
				Radius = rad;
			}
				
			public FlashInfo()
			{ 
			}
		}
		
		void Loaded()
		{
			permission.RegisterPermission("flashbang.admin", this);
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("FlashBang_Settings"); 
			foreach(FlashInfo info in data.FlashInfo)
			{Puts($"{info.ID}");}
			if(data.FlashInfo == null)
			Puts("There are not FlashBang settings! Type /fhelp to check the commands!");	
			

		}
		
        #region GUI
		void UseUI(BasePlayer player, string strength)
		{
			var elements = new CuiElementContainer();
				elements.Add(new CuiElement
				{  
					Name = "FlashBang",
					Parent = "HUD/Overlay",
					FadeOut = 0.5f,
					Components =
					{
						new CuiImageComponent
						{
							Color = $"1.0 1.0 1.0 {strength}",
							FadeIn = 0.5f
						}, 
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0",
							AnchorMax = "1 1"
						}
					}
				});
			CuiHelper.AddUi(player, elements);
		}

        #endregion
		
		int GetNewId()
		{		
			int id = 0;
			foreach(FlashInfo info in data.FlashInfo)
			{
				id = Math.Max(0, info.ID);
			}
			return id + 1;
		}

        void OnWeaponThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity.name.Contains("grenade.f1"))
            {
				if(data.FlashInfo == null)
				{
					Puts(player.displayName + " tried to throw a flashbang but there aren't any settings. || /fb help");
					return;
				}
                PrintToChat("<color=orange>FlashBang System</color> : <color=cyan>" + player.displayName + " </color>has thrown a flashbang!");
                timer.Once(3, () => Flash(player, entity));
            }
        }
		
		[ChatCommand("fb")]
		void cmdFB(BasePlayer player, string cmd, string[] args)
		{
			if(args.Length == 0)
			{ 
				player.SendConsoleCommand("chat.say \"/fb help\" ");
				return;
			}
			ulong steamId = player.userID;
			switch(args[0])
			{
				case "add":			
					if(!permission.UserHasPermission(steamId.ToString(), "flashbang.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					if(args.Length != 4)
					{
						SendReply(player, "/fb add <Duration> <Radius> <Power>");
						return;
					}   
					int fnum;
					int dur = Convert.ToInt32(args[1]); 
					int rad = Convert.ToInt32(args[2]);
					string pow = Convert.ToString(args[3]);
					if(data.FlashInfo == null)
					{
						fnum = 1;
					}
					else
					{
						fnum = GetNewId();
					}
					var info = new FlashInfo(fnum,dur,pow,rad);
					data.FlashInfo.Add(info);
					SendReply(player, "You have added a new FlashBang setting!");
					Interface.GetMod().DataFileSystem.WriteObject("FlashBang_Settings", data);
					break;
				case "help":	
					if(!permission.UserHasPermission(steamId.ToString(), "flashbang.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					SendReply(player, "/fb add <Duration> <Radius> <Power>");
					SendReply(player, "/fb remove <ID> ");
					SendReply(player, "/fb list");
				break;
				case "remove":
					if(!permission.UserHasPermission(steamId.ToString(), "flashbang.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					if(args.Length != 2) 
					{
						SendReply(player, "/fb remove <ID> || /fb list");
						return;
					}
					foreach(FlashInfo infos in data.FlashInfo)
					{
						if(infos.ID.ToString() == args[1].ToString())
						{
							data.FlashInfo.Remove(infos);
							SendReply(player, "You have removed the flashbang setting ID: " + infos.ID.ToString());
							Interface.GetMod().DataFileSystem.WriteObject("FlashBang_Settings", data);
							break;
						}
					}
				break;
				case "list":
					if(!permission.UserHasPermission(steamId.ToString(), "flashbang.admin"))
					{
						SendReply(player, "You do not have permission to use this command!");
						return;
					}
					SendReply(player, "<color=#91FFB5>Current Flashbang Settings</color>");
					foreach(FlashInfo infom in data.FlashInfo)
					{
						SendReply(player, "ID: <color=#91FFB5>{2}</color>\nDuration: <color=cyan>{0}</color> \nPower:<color=orange> {1} </color> \nRadius: <color=lime>{3}</color>", infom.Duration, infom.Power, infom.ID, infom.Radius);
						SendReply(player, "<color=#91FFB5>*************</color>");										
					}
					SendReply(player, "<color=#91FFB5>*************</color>");
				break;
			}
			
		}

        void Flash(BasePlayer player, BaseEntity entity)
        {
            Vector3 flashPos = entity.GetEstimatedWorldPosition();
            entity.Kill();
            foreach(BasePlayer current in BasePlayer.activePlayerList)
            {
				foreach(FlashInfo info in data.FlashInfo)
				{
					if (Vector3.Distance(current.transform.position, flashPos) <= info.Radius)
					{
						UseUI(current, info.Power);
						timer.Once(info.Duration, () => CuiHelper.DestroyUi(current, "FlashBang"));
						return;
					}
				}
            }

        }

        void Unloaded()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
			CuiHelper.DestroyUi(player, "FlashBang");
        }
    }
}
