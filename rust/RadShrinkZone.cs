using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Text.RegularExpressions;
namespace Oxide.Plugins
{
    [Info("RadShrinkZone", "vaalberith", "1.0.2", ResourceId = 1828)]
	class RadShrinkZone : RustPlugin
    {
		
		//DEFAULT VALUES
		
		Vector3 target = new Vector3(0,0,0);
		float saferadius = 10;
		float saferadiusmin = 5;
		float eventradius = 30;
		float radpower = 50;
		float step = 1;
		float period = 20;
		float drawtime = 5;
		string permissionrad="RadShrinkZone.can";
		bool drawrad=true;
		bool drawsafe=true;
		bool breaking = false;
		bool ok = true;
		
		//INITIALISATION\DECLARATION
		
		List<Vector3> position = new List<Vector3>();
		
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
				{"Helplong", "/rad saferadius saferadiusmin eventradius radpower step period (6 arguments to config) OR \n/rad start|stop|clear (1 argument to launch|stop|clear zones) OR \n/rad x y z (3 arguments to set position) OR\n/rad draw 0|1|2 (2 arguments to draw safezone|radzone|both) OR\n/rad drawtime (seconds) (2 arguments to set draw time)"},
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
				{"Helplong", "/rad saferadius saferadiusmin eventradius radpower step period (6 arguments to config) OR \n/rad start|stop|clear (1 argument to launch|stop|clear zones) OR \n/rad x y z (3 arguments to set position) OR\n/rad draw 0|1|2 (2 arguments to draw safezone|radzone|both) OR\n/rad drawtime (seconds) (2 arguments to set draw time)"},
				{"Erased", "ÐÑÐµ Ð¸Ð²ÐµÐ½Ñ-Ð·Ð¾Ð½Ñ Ð¾ÑÐ¸ÑÐµÐ½Ñ"}
			};
            lang.RegisterMessages(messagesRu, this, "ru");
        } 
        #endregion
		
		//CONFIG
		
		//PLUGIN REFERENCE AND HOOKS
		
		[PluginReference]
        Plugin ZoneManager;
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
			if (!ZoneID.Contains("radshrink_pos_")) return;
			if (drawrad)
			{
				Regex regex = new Regex(@"\d+");
				Match match = regex.Match(ZoneID);
				if (match.Success)
				{
					float sphereradius = (eventradius-saferadius)/2;
					player.SendConsoleCommand("ddraw.sphere", drawtime, Color.red, position[Convert.ToInt32(match.Value, 16)], sphereradius);
				}
			}
			if (drawsafe) player.SendConsoleCommand("ddraw.sphere", drawtime, Color.green, target, saferadius);
		}
		
		
		
		void OnServerInitialized()
        {
			LoadDefaultMessages();
            if (plugins.Exists("ZoneManager")) ok = true;
            else {
				PrintWarning("Install ZoneManager!");
				ok=false;
			}
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
        void rad(BasePlayer player, string cmd, string[] args)
        {
			if (!ok) return;
			if (!IsAllowed(player, permissionrad))
			{	
				PrintToChat(player, GetMessage("NoPerm", player.UserIDString));
				return;
			}
			if (args.Length == 6)
			{				
				saferadius = Convert.ToSingle(args[0]);
				saferadiusmin = Convert.ToSingle(args[1]);
				eventradius = Convert.ToSingle(args[2]);
				radpower = Convert.ToSingle(args[3]);
				step = Convert.ToSingle(args[4]);
				period = Convert.ToSingle(args[5]);
				return;
			}
			if (args.Length == 1)
			{
				if (args[0]=="start")
				{
					started();
					return;
				}
				else if (args[0]=="stop")
				{
					stop();
					return;
				}
				else if (args[0]=="clear")
				{
					DelPos();
					return;
				}
				else PrintToChat(player, GetMessage("Help", player.UserIDString));
				return;
			}
			if (args.Length == 3)
			{
				target.x = Convert.ToSingle(args[0]);
				target.y = Convert.ToSingle(args[1]);
				target.z = Convert.ToSingle(args[2]);
				return;
			}
			if (args.Length == 2)
			{
				if (args[0]=="draw")
				{
					if (args[1] == "0")
					{
						drawsafe=false;
						drawrad=false;
					}
					else if (args[1] == "1")
					{
						drawsafe = true;
						drawrad = false;
					}
					else if (args[1] == "2") 
					{
						drawrad = true;
						drawsafe = false;
					}
					else if (args[1] == "3") 
					{
						drawsafe = true;
						drawrad = true;
					}
					else PrintToChat(player, GetMessage("Help", player.UserIDString));
					return;
				}
				if (args[0]=="drawtime")
				{
					drawtime=Convert.ToSingle(args[1]);
					return;
				}
				PrintToChat(player, GetMessage("Help", player.UserIDString));
				return;
			}
			PrintToChat(player, GetMessage("Helplong", player.UserIDString));
			return;
		}
	}
}