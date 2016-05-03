using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

using System.Text.RegularExpressions;
namespace Oxide.Plugins
{
    [Info("RadShrinkZone", "vaalberith", "1.0.3", ResourceId = 1828)]
	class RadShrinkZone : RustPlugin
    {
		
		//DEFAULT VALUES
		
		Vector3 target = new Vector3(0,0,0);
		float saferadius = 10;
		float saferadiusmin = 5;
		float eventradius = 40;
		float radpower = 50;
		float step = 1;
		float period = 20;
		float drawtime = 5;
		string drawmode = "safe";
		
		string permissionrad="RadShrinkZone.can";
		bool breaking = true;
		bool ok = true;
		
		//INITIALISATION\DECLARATION
		List<Vector3> position = new List<Vector3>();
		readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RadShrinkZoneDefault");
		Dictionary<string, List<string>> radzonedata = new Dictionary<string, List<string>>();
		
		//LOCALIZATION
		
		#region Localization
		 
		string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
		
        void LoadDefaultMessages()
        {
            var messagesEn = new Dictionary<string, string> 
            {
                {"EventStart", "Radiation is coming! Run to the centre to safe your life! ( {0} : {1} )"},
                {"RadiusDecreased", "Radius of safezone decreased!"},
                {"SafezoneRadiusReachedMin", "Radius of safezone reached its minimum!"},
                {"EventStopped", "Event stopped!"},
                {"NoPerm", "No permission!"},
				{"Help", "Type /rad for usage help"},
				{"Helplong", "<color=red>Config:</color>\n/rad drawmod none|safe|rad|both\n/rad drawtime (seconds)\n/rad saferad (m)\n /rad saferadmin (m)\n/rad eventrad (m)\n/rad radpower (%)\n/rad step (num)\n/rad period (seconds)\n/rad x y z\n/rad me (uses your position as target)\n<color=red>Manager:</color>\n/rad start\n/rad stop (stops decrease)\n/rad clear (close event and remove zones)"},
				{"Erased", "Erased all event rad zones"}
            };
            lang.RegisterMessages(messagesEn, this);
			 
			var messagesRu = new Dictionary<string, string>
            {
				{"EventStart", "Ð Ð°Ð´Ð¸Ð°ÑÐ¸Ñ Ð¿Ð¾ÑÐ²Ð»ÑÐµÑÑÑ! ÐÐµÐ³Ð¸ Ð² ÑÐµÐ½ÑÑ, ÑÑÐ¾Ð±Ñ Ð²ÑÐ¶Ð¸ÑÑ! ( {0} : {1} )"},
				{"RadiusDecreased", "Ð Ð°Ð´Ð¸ÑÑ Ð±ÐµÐ·Ð¾Ð¿Ð°ÑÐ½Ð¾Ð¹ Ð·Ð¾Ð½Ñ ÑÐ¼ÐµÐ½ÑÑÐ¸Ð»ÑÑ!"},
				{"SafezoneRadiusReachedMin", "Ð Ð°Ð´Ð¸ÑÑ Ð±ÐµÐ·Ð¾Ð¿Ð°ÑÐ½Ð¾Ð¹ Ð·Ð¾Ð½Ñ Ð´Ð¾ÑÑÐ¸Ð³ Ð¼Ð¸Ð½Ð¸Ð¼ÑÐ¼Ð°!"},
				{"EventStopped", "ÐÐ²ÐµÐ½Ñ Ð·Ð°ÐºÐ¾Ð½ÑÐ¸Ð»ÑÑ!"},
				{"NoPerm", "ÐÐµÑ Ð¿ÑÐ°Ð²!"},
				{"Help", "ÐÐ°Ð¿Ð¸ÑÐ¸ /rad Ð´Ð»Ñ Ð¿Ð¾Ð´ÑÐºÐ°Ð·ÐºÐ¸"},
				{"Helplong", "<color=red>Config:</color>\n/rad drawmod none|safe|rad|both\n/rad drawtime (seconds)\n/rad saferad (m)\n /rad saferadmin (m)\n/rad eventrad (m)\n/rad radpower (%)\n/rad step (num)\n/rad period (seconds)\n/rad x y z\n/rad me (uses your position as target)\n<color=red>Manager:</color>\n/rad start\n/rad stop (stops decrease)\n/rad clear (close event and remove zones)"},
				{"Erased", "ÐÑÐµ Ð¸Ð²ÐµÐ½Ñ-Ð·Ð¾Ð½Ñ Ð¾ÑÐ¸ÑÐµÐ½Ñ"}
			};
            lang.RegisterMessages(messagesRu, this, "ru");
        } 
        #endregion
		
		//CONFIG
		
		//PLUGIN REFERENCE AND HOOKS
		
		[PluginReference]
        Plugin ZoneManager;
		
		void OnServerInitialized()
        {
			LoadDefaultMessages();
            if (plugins.Exists("ZoneManager")) ok = true;
            else 
			{
				PrintWarning("Install ZoneManager!");
				ok=false;
			}
			
			radzonedata = dataFile.ReadObject<Dictionary<string, List<string>>>();
			List<string> pos;
			if (!radzonedata.TryGetValue("Position", out pos))
			{
				//first datafile creating ("position" does not exist)
				safecfg();
				Puts("BattleRoyale RadZone created for first time.");
			}
			else Puts("BattleRoyale RadZone datafile loaded.");
			execcfg();
        }
		
		void execcfg()
		{
			radzonedata = dataFile.ReadObject<Dictionary<string, List<string>>>();
			List<string> value;
			if (radzonedata.TryGetValue("Position", out value))
				target.x=Convert.ToSingle(value[0]);target.y=Convert.ToSingle(value[1]);target.z=Convert.ToSingle(value[2]);
			if (radzonedata.TryGetValue("SafeRadius", out value))
				saferadius=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("SafeRadiusMinimum", out value))
				saferadiusmin=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("EventRadius", out value))
				eventradius=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("RadiationPower", out value))
				radpower=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("DecreaseStep", out value))
				step=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("DecreasePeriod", out value))
				period=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("DrawTime", out value))
				drawtime=Convert.ToSingle(value[0]);
			if (radzonedata.TryGetValue("DrawMode", out value))
				drawmode=value[0];
		}
		
		void safecfg()
		{
			radzonedata["Position"] = new List<string>(){target.x.ToString(),target.y.ToString(),target.z.ToString()};
			radzonedata["SafeRadius"] = new List<string>(){saferadius.ToString()};
			radzonedata["SafeRadiusMinimum"] = new List<string>(){saferadiusmin.ToString()};
			radzonedata["EventRadius"] = new List<string>(){eventradius.ToString()};
			radzonedata["RadiationPower"] = new List<string>(){radpower.ToString()};
			radzonedata["DecreaseStep"] = new List<string>(){step.ToString()};
			radzonedata["DecreasePeriod"] = new List<string>(){period.ToString()};
			radzonedata["DrawTime"] = new List<string>(){drawtime.ToString()};
			radzonedata["DrawMode"] = new List<string>(){drawmode};
			
			dataFile.WriteObject(radzonedata);
		}
		
		void Unload() 
		{
			DelPos();
		}
		
		void Loaded()
		{
			permission.RegisterPermission(permissionrad, this);
		}
		
		void OnEnterZone(string ZoneID, BasePlayer player)
		{
			if (breaking) return;
			if (!ZoneID.Contains("radshrink_pos_")) return;
			if (drawmode=="rad" || drawmode=="both")
			{
				Regex regex = new Regex(@"\d+");
				Match match = regex.Match(ZoneID);
				if (match.Success)
				{
					float sphereradius = (eventradius-saferadius)/2;
					player.SendConsoleCommand("ddraw.sphere", drawtime, Color.red, position[Convert.ToInt32(match.Value, 16)], sphereradius);
				}
			}
			if (drawmode=="safe"|| drawmode=="both") player.SendConsoleCommand("ddraw.sphere", drawtime, Color.green, target, saferadius);
		}
		
		
		//MAIN FUNCTIONS
		
		private void createZone(string zoneID, Vector3 pos, float radius, float rads)
        {
            List<string> build = new List<string>();
            build.Add("radius");
            build.Add(radius.ToString());
            build.Add("radiation");
            build.Add(rads.ToString());
            string[] zoneArgs = build.ToArray();
            ZoneManager?.Call("CreateOrUpdateZone", zoneID, zoneArgs, pos);
        }  
		
        private void eraseZone(string zoneID)
        {
            ZoneManager.Call("EraseZone", zoneID);
        }

		private void CalcPos (Vector3 pos)
		{
			if (breaking) return;
			position.Clear();
			float centerline = (eventradius+saferadius)/2;
			float sphereradius = (eventradius-saferadius)/2;
			float corn = centerline *0.71f; 
			Vector3 SW = new Vector3(pos.x-corn, pos.y, pos.z-corn); position.Add(SW);
			Vector3 W = new Vector3(pos.x-centerline, pos.y, pos.z); position.Add(W);
			Vector3 NW = new Vector3(pos.x-corn, pos.y, pos.z+corn); position.Add(NW);
			Vector3 N = new Vector3(pos.x, pos.y, pos.z+centerline); position.Add(N);
			Vector3 NE = new Vector3(pos.x+corn, pos.y, pos.z+corn); position.Add(NE);
			Vector3 E = new Vector3(pos.x+centerline, pos.y, pos.z); position.Add(E);
			Vector3 SE = new Vector3(pos.x+corn, pos.y, pos.z-corn); position.Add(SE);
			Vector3 S = new Vector3(pos.x, pos.y, pos.z-centerline); position.Add(S);
			int i=0;
			foreach(Vector3 elem in position)
			{
				createZone("radshrink_pos_"+i,elem,sphereradius,radpower);
				i++;
			}
		}
		
		private void DelPos()
		{
			stop();
			int i=0;
			foreach(Vector3 elem in position)
			{
				eraseZone("radshrink_pos_"+i);
				i++;
			}
			position.Clear();
			if (i>0) Puts(GetMessage("Erased"));
		}
		 
		private void started()
		{
			breaking = false;
			execcfg();
			PrintToChat(GetMessage("EventStart"),target.x,target.z);
			Puts (GetMessage("EventStart"),target.x,target.z);
			StartZoneShrink();
		}
		
		private void stop()
		{
			breaking=true;
			PrintToChat(GetMessage("EventStopped"));
			Puts(GetMessage("EventStopped"));
		}
		
		private void StartZoneShrink()
        {
			if (breaking) return;
            timer.In(period, () => Shrink());
        }
		
		private void Shrink()
		{
			if (breaking) return;
			if (saferadius <= saferadiusmin) 
			{
				PrintToChat(GetMessage("SafezoneRadiusReachedMin"));
				Puts(GetMessage("SafezoneRadiusReachedMin"));
				return;
			}
			CalcPos(target);
			saferadius=saferadius - step;
			Puts(GetMessage("RadiusDecreased"));
			PrintToChat(GetMessage("RadiusDecreased"));
			StartZoneShrink();
		}
		
		bool IsAllowed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            return false;
        }
		
		// INTERFACE, COMMANDS
		
		[ChatCommand("rad")]
        void radchat(BasePlayer player, string cmd, string[] args)
        {
			if (!ok) return;
			if (player !=null)
			{
				if (!IsAllowed(player, permissionrad))
				{	
					SendReply(player, GetMessage("NoPerm", player.UserIDString));
					return;
				}
			}
			
			if (args.Length == 1)
			{
				if (args[0]=="start") 
				{
					started();
				}
				else if (args[0]=="stop")
				{
					stop();
				}
				else if (args[0]=="clear")
				{
					DelPos();
				}
				else if (args[0]=="me")
				{
					if (player == null) return;
					target=player.transform.position;
					safecfg();
				}
				
				else if (player !=null) SendReply(player, GetMessage("Help", player.UserIDString));
				
				return;
			}
			
			if (args.Length == 2)
			{
				if (args[0]=="drawmod")
				{
					if (args[1] == "safe" || args[1] == "rad" || args[1] == "both" || args[1] == "none")
					{
						drawmode=args[1];
					}
					else  if (player !=null) SendReply(player, GetMessage("Help", player.UserIDString));
				}
				else if (args[0]=="drawtime")
				{
					drawtime=Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="saferad")
				{				
					saferadius = Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="saferadmin")
				{				
					saferadiusmin = Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="eventrad")
				{				
					eventradius = Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="radpower")
				{				
					radpower = Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="step")
				{				
					step = Convert.ToSingle(args[1]);
				}
				
				else if (args[0]=="period")
				{				
					period = Convert.ToSingle(args[1]);
				}
				
				else if (player !=null) SendReply(player, GetMessage("Help", player.UserIDString));
				
				safecfg();
				return;
			}
			
			if (args.Length == 3)
			{
				target.x = Convert.ToSingle(args[0]);
				target.y = Convert.ToSingle(args[1]);
				target.z = Convert.ToSingle(args[2]);
				safecfg();
			}
			
			else if (player !=null) SendReply(player, GetMessage("Helplong", player.UserIDString));
			return;
		}
		
		[ConsoleCommand("rad")] 
		void radconsole(ConsoleSystem.Arg arg) 
		{
			if (arg.Args == null) return;
			string [] args = arg.Args;
			
			if (arg.connection != null)
            {
                BasePlayer player = arg.connection.player as BasePlayer;
				if (!IsAllowed(player, permissionrad))
				{	
					PrintToChat(player, GetMessage("NoPerm", player.UserIDString));
					return;
				}
				radchat(player, "rad", args);
			}
			
			radchat(null, "rad", args);
			return;
		}
	}
}