using System;
using System.Text;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Where's My Corpse", "LeoCurtss", 0.5)]
    [Description("Points a player to their corpse when they type a command.")]

    class WheresMyCorpse : RustPlugin
    {
		
        void Loaded()
        {
            LoadData();
			
			permission.RegisterPermission("wheresmycorpse.canuse", this);
			
			//Lang API dictionary
			lang.RegisterMessages(new Dictionary<string,string>{
				["WMC_NoData"] = "No data was found on your last death.  The WheresMyCorpse plugin may have been reloaded or you have not died yet.",
				["WMC_LastSeen"] = "Your corpse was last seen {0} meters from here.",
				["WMC_NoPermission"] = "You do not have permission to use that command."
			}, this);
        }
		
		private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		}
		
		Dictionary<string, string> deathInfo = new Dictionary<string, string>();

		void LoadData()
		{
			deathInfo = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, string>>("WheresMyCorpse");
		}

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("WheresMyCorpse", deathInfo);
		}

		void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			
			if (entity.name.Contains("player.prefab"))
			{
				var player = entity as BasePlayer;
				
				string UserID = player.UserIDString;
				string DeathPosition = entity.GetEstimatedWorldPosition().ToString();
				
				Puts("Player death info: " + UserID + " at " + DeathPosition);
				
				LoadData();
				
				string value;
				
				if (deathInfo.TryGetValue(UserID, out value))
				{
					deathInfo[UserID] = DeathPosition;
					SaveData();
				}
				else
				{
					deathInfo.Add(UserID,DeathPosition);
					SaveData();
				}
			}
		}
		
		void OnPlayerRespawned(BasePlayer player)
		{
            if (permission.UserHasPermission(player.userID.ToString(), "wheresmycorpse.canuse"))
			{
				if (deathInfo.ContainsKey(player.UserIDString))
				{
					Vector3 lastDeathPosition = getVector3(deathInfo[player.UserIDString]);
					Vector3 currentPosition = player.transform.position;
					
					float distanceToCorpse = Vector3.Distance(lastDeathPosition,currentPosition);
					
					SendReply(player,string.Format(GetMessage("WMC_LastSeen",player.UserIDString),distanceToCorpse.ToString("0")));
					drawArrow(player,60.0f);
				}
				else
				{
					SendReply(player,GetMessage("WMC_NoData",player.UserIDString));
				}
			}
		}
		
		[ChatCommand("where")]
        void TestCommand(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "wheresmycorpse.canuse"))
			{
				if (deathInfo.ContainsKey(player.UserIDString))
				{
					Vector3 lastDeathPosition = getVector3(deathInfo[player.UserIDString]);
					Vector3 currentPosition = player.transform.position;
					
					float distanceToCorpse = Vector3.Distance(lastDeathPosition,currentPosition);
					
					SendReply(player,string.Format(GetMessage("WMC_LastSeen",player.UserIDString),distanceToCorpse.ToString("0")));
					drawArrow(player,30.0f);
				}
				else
				{
					SendReply(player,GetMessage("WMC_NoData",player.UserIDString));
				}
			}
			else
			{
				SendReply(player,GetMessage("WMC_NoPermission",player.UserIDString));
			}
        }
		
		void drawArrow(BasePlayer player, float duration)
		{
			Vector3 lastDeathPosition = getVector3(deathInfo[player.UserIDString]);
			Vector3 currentPosition = player.transform.position;
			
			float distanceToCorpse = Vector3.Distance(lastDeathPosition,currentPosition);
			
			Vector3 arrowBasePosition = LerpByDistance(currentPosition + new Vector3(0, 1, 0),lastDeathPosition + new Vector3(0, 1, 0),3);
			Vector3 arrowPointPosition = LerpByDistance(currentPosition + new Vector3(0, 1, 0),lastDeathPosition + new Vector3(0, 1, 0),6);
			
			Vector3 beaconBasePosition = lastDeathPosition;
			Vector3 beaconPointPosition = lastDeathPosition + new Vector3(0, 1000, 0);
			
			Color arrowColor = new Color(1, 0, 0, 1);
			Color textColor = new Color(1,0,0,1);
			Color beaconColor = new Color(1,0,0,1);
			player.SendConsoleCommand("ddraw.arrow", duration, arrowColor, arrowBasePosition, arrowPointPosition, 0.5f);
			player.SendConsoleCommand("ddraw.text", duration, textColor, arrowBasePosition, "Distance: " + distanceToCorpse.ToString("0") + " meters");
			player.SendConsoleCommand("ddraw.arrow", duration, arrowColor, beaconBasePosition, beaconPointPosition, 1.0f);
		}
		
		public Vector3 getVector3(string rString){
			string[] temp = rString.Substring(1,rString.Length-2).Split(',');
			float x = float.Parse(temp[0]);
			float y = float.Parse(temp[1]);
			float z = float.Parse(temp[2]);
			Vector3 rValue = new Vector3(x,y,z);
			return rValue;
		}
		
		public Vector3 LerpByDistance(Vector3 A, Vector3 B, float x)
		{
			Vector3 P = x * Vector3.Normalize(B - A) + A;
			return P;
		}
    }
}