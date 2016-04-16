using System;
using Rust;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
     [Info("FauxClip", "Colon Blow", "1.3.5", ResourceId = 1299)]
     class FauxClip : RustPlugin
     {
	public float GracefulLandingTime => Config.Get<float>("GracefulLandingTime");
	public float BaseNoClipSpeed => Config.Get<float>("BaseNoClipSpeed");
	public float SprintNoClipSpeed => Config.Get<float>("SprintNoClipSpeed");
        public float TurboNoClipSpeed => Config.Get<float>("TurboNoClipSpeed");
	public bool UseFauxClipGodMode => Config.Get<bool>("UseFauxClipGodMode");

        protected override void LoadDefaultConfig()
        {
            	Config["GracefulLandingTime"] = 3;
	    	Config["BaseNoClipSpeed"] = .12;
	    	Config["SprintNoClipSpeed"] = .24;
	    	Config["TurboNoClipSpeed"] = 1;
		Config["UseFauxClipGodMode"] = true;
		
            SaveConfig();
        }

        class PlayerData
        {
             public BasePlayer player;
             public float speed;
             public Vector3 oldPos;
             public InputState input;
        }

        class LandingData
        {
             public BasePlayer player;
        }

        private readonly Dictionary<ulong, PlayerData> _noclip = new Dictionary<ulong, PlayerData>();
	private readonly Dictionary<ulong, LandingData> _landing = new Dictionary<ulong, LandingData>();
        private static readonly FieldInfo ServerInput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        public static FieldInfo lastPositionValue;

        void Loaded()
        {
        permission.RegisterPermission("fauxclip.allowed", this);
	permission.RegisterPermission("fauxclip.norestriction", this);
	permission.RegisterPermission("fauxclip.canuseturbo", this);
        lastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }

         void OnFrame()
         {

             if (_noclip.Count <= 0) return;
             foreach (var playerData in _noclip.Values)
             {
                var player = playerData.player;
		player.violationLevel = 0;
		if (noBuild(player) & (noAdmin(player) & (!isAllowed(player, "fauxclip.norestriction"))))
		{
                        Restrictedairspace(player);
            		return;
            	}

                if (player.net == null)
            	{
                        Deactivatenoclip(player);
            		return;
            	}
                else
                {          
                     var input = playerData.input;
                     var newPos = playerData.oldPos;
			var currentRot = Quaternion.Euler(input.current.aimAngles);
                     var speedMult = playerData.speed;

                     if (input.IsDown(BUTTON.SPRINT))
                        speedMult = SprintNoClipSpeed;

                     if (input.IsDown(BUTTON.USE) & (isAllowed(player, "fauxclip.canuseturbo")))
                        speedMult = TurboNoClipSpeed;
  
             	     else if (input.IsDown(BUTTON.RELOAD))
             	     {
                        Deactivatenoclip(player);
            		return;
             	     }  
                     else if (input.IsDown(BUTTON.JUMP))
                     {
                         newPos += (currentRot * Vector3.up * speedMult);
                     }   
                     else if (input.IsDown(BUTTON.FORWARD))
                     {
                         newPos += (currentRot * Vector3.forward * speedMult);
                     }
                     else if (input.IsDown(BUTTON.RIGHT))
                     {
                         newPos += (currentRot * Vector3.right * speedMult);
                     }
                     else if (input.IsDown(BUTTON.LEFT))
                     {
                         newPos += (currentRot * Vector3.left * speedMult);
                     }
                     else if (input.IsDown(BUTTON.BACKWARD))
                     {
                         newPos += (currentRot * Vector3.back * speedMult);
                     }  
                     else if (!input.IsDown(BUTTON.FORWARD))
             	     {
             		ForcePlayerPosition(player, newPos);
                     } 
                     else
                      newPos = player.transform.position;
                      lastPositionValue.SetValue(player, player.transform.position);
                      if (newPos == playerData.oldPos) continue;
                      ForcePlayerPosition(player, newPos);
                      playerData.oldPos = newPos;
                      player.TransformChanged();
                 }
             }
         }

	bool noAdmin(BasePlayer player)
		{
		if (player.IsAdmin()) return false;
		return true;
		}
	bool noBuild(BasePlayer player)
		{
		if (player.CanBuild()) return false;
		return true;
		}
	void DamageOn(BasePlayer player)
        	{
		player.metabolism.heartrate.min = 0;
                player.metabolism.heartrate.max = 1;
                player.metabolism.temperature.min = -100;
                player.metabolism.temperature.max = 100;
                player.metabolism.radiation_level.min = 0;
                player.metabolism.radiation_level.max = 100;
                player.metabolism.radiation_poison.min = 0;
                player.metabolism.radiation_poison.max = 500;
                player.metabolism.wetness.min = 0;
                player.metabolism.wetness.max = 1;
               	player.metabolism.dirtyness.min = 0;
                player.metabolism.dirtyness.max = 100;
                player.metabolism.oxygen.min = 0;
                player.metabolism.oxygen.max = 1;
		player.metabolism.bleeding.min = 0;
                player.metabolism.bleeding.max = 1;
                player.metabolism.comfort.min = 0;
               	player.metabolism.comfort.max = 1;
		return;
		}
	void DamageOff(BasePlayer player)
		{
			if (!UseFauxClipGodMode) return;

			foreach (var playerData in _noclip.Values)
			{
                	player.metabolism.heartrate.min = 0.5f;
                	player.metabolism.heartrate.max = 0.5f;
                	player.metabolism.temperature.min = 32;
                	player.metabolism.temperature.max = 32;
                	player.metabolism.radiation_level.min = 0;
                	player.metabolism.radiation_level.max = 0;
                	player.metabolism.radiation_poison.max = 0;
                	player.metabolism.wetness.min = 0;
                	player.metabolism.wetness.max = 0;
                	player.metabolism.dirtyness.min = 0;
                	player.metabolism.dirtyness.max = 0;
                	player.metabolism.oxygen.min = 1;
                	player.metabolism.oxygen.max = 1;
                	player.metabolism.bleeding.min = 0;
                	player.metabolism.bleeding.max = 0;
                	player.metabolism.comfort.min = 0;
			player.metabolism.comfort.max = 0;
			return;
			}
		}

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        	{
			if (!UseFauxClipGodMode) return null;

			if (UseFauxClipGodMode)
			{
				if (entity is BasePlayer)
				{
					var player = (BasePlayer)entity;
					if (_noclip.ContainsKey(player.userID))
					{
                   			return false;
					}
					if (_landing.ContainsKey(player.userID))
					{
                   			return false;
					}
				}
			}
		return null;
        	}

         void Deactivatenoclip(BasePlayer player)
         	{
		_noclip.Remove(player.userID);
		SendReply(player, "NoClip Deactivated");
		_landing.Add(player.userID, new LandingData
		{
		player = player
		});
		LandingCycleDone(player);
         	return;
         	}

         void Restrictedairspace(BasePlayer player)
         	{
     		_noclip.Remove(player.userID);
		SendReply(player, "You cannot noclip while under 'Building Blocked' flag !");
		_landing.Add(player.userID, new LandingData
		{
		player = player
		});
		LandingCycleDone(player);
         	return;
         	}

         void Activatenoclip(BasePlayer player, float speed)
         	{
     		_noclip.Add(player.userID, new PlayerData
        	{
                player = player,
                speed = speed,
                 input = (InputState) ServerInput.GetValue(player),
                 oldPos = player.transform.position
        	});
                SendReply(player, "NoClip Activated, press any key to start");
		DamageOff(player);
		return;
         	}

         void Togglenoclip(BasePlayer player, float speed)
         	{
             	if (_noclip.ContainsKey(player.userID))
                 	Deactivatenoclip(player);
             	if (_landing.ContainsKey(player.userID))
                 	SendReply(player, "Please Wait...");
             	else
                 	Activatenoclip(player, speed);
			return;
         	}

	void LandingCycleDone(BasePlayer player)
		{
		foreach (var playerData in _landing.Values)
			{
			timer.Once(GracefulLandingTime, () => _landing.Remove(player.userID));
			timer.Once(GracefulLandingTime, () => DamageOn(player));
			}
		}

         [ChatCommand("noclip")]
         void cmdChatnolcip(BasePlayer player, string command, string[] args)
         {
        	if (!isAllowed(player, "fauxclip.allowed"))
            		{
                	SendReply(player, "You are not worthy yet!");
                	return;
            		}
		if (isAllowed(player, "fauxclip.allowed"))
        		{
                	var speed = BaseNoClipSpeed;
                	if (args.Length > 0)
                	speed = Convert.ToSingle(args[0]);
                	Togglenoclip(player, speed);
        		}
		else return;
         }

	void OnPlayerRespawned(BasePlayer player)
	{
             	if (_noclip.ContainsKey(player.userID))
                 	_noclip.Remove(player.userID);
             	if (_landing.ContainsKey(player.userID))
                 	_landing.Remove(player.userID);
		else
		return;
	}
	void OnPlayerDisconnected(BasePlayer player, string reason)
	{
             	if (_noclip.ContainsKey(player.userID))
                 	_noclip.Remove(player.userID);
             	if (_landing.ContainsKey(player.userID))
                 	_landing.Remove(player.userID);
		else
		return;
	}

	bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
     }
}