using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
     	[Info("FauxAdmin", "Colon Blow", "1.0.3", ResourceId = 1933)]
    	class FauxAdmin : RustPlugin
     	{

	public bool DisableFlyHackProtection => Config.Get<bool>("DisableFlyHackProtection");
	public bool DisableNoclipProtection => Config.Get<bool>("DisableNoclipProtection");
	public bool DisableFauxAdminDemolish => Config.Get<bool>("DisableFauxAdminDemolish");
	public bool DisableFauxAdminRotate => Config.Get<bool>("DisableFauxAdminRotate");
	public bool DisableFauxAdminUpgrade => Config.Get<bool>("DisableFauxAdminUpgrade");
	public bool AllowGodModeToggle => Config.Get<bool>("AllowGodModeToggle");
	public bool DisableNoclipOnNoBuild => Config.Get<bool>("DisableNoclipOnNoBuild");
	public bool UseLevelActivation => Config.Get<bool>("UseLevelActivation");
	public float ActivateFauxAdminOnLevel => Config.Get<float>("ActivateFauxAdminOnLevel");

	Dictionary<ulong, RestrictedData> _restricted = new Dictionary<ulong, RestrictedData>();

        class RestrictedData
        {
             public BasePlayer player;
        }

        protected override void LoadDefaultConfig()
        	{
			Config["DisableFlyHackProtection"] = true;
			Config["DisableNoclipProtection"] = true;
			Config["DisableFauxAdminDemolish"] = true;
			Config["DisableFauxAdminRotate"] = true;
			Config["DisableFauxAdminUpgrade"] = true;
			Config["AllowGodModeToggle"] = false;
			Config["DisableNoclipOnNoBuild"] = true;
			Config["UseLevelActivation"] = false;
			Config["ActivateFauxAdminOnLevel"] = 50f;
            		SaveConfig();
        	}

        void Loaded()
        	{
		if (DisableFlyHackProtection) ConVar.AntiHack.flyhack_protection = 0;
		if (DisableNoclipProtection) ConVar.AntiHack.noclip_protection = 0;
		lang.RegisterMessages(messages, this);
        	permission.RegisterPermission("fauxadmin.allowed", this);
		permission.RegisterPermission("fauxadmin.bypass", this);
		permission.RegisterPermission("fauxadmin.blocked", this);
		permission.RegisterPermission("fauxadmin.god", this);
		}

	////////////////////////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        	{
			{"restricted", "You are not allowed to noclip here." },
			{"notallowed", "You are not worthy yet!" }
        	};

	////////////////////////////////////////////////////////////////////////////////////////////////////////

	void OnPlayerTick(BasePlayer player)
		{
			if (player.net?.connection?.authLevel > 0) return;

			if (!AllowGodModeToggle)
			{
				if (isAllowed(player, "fauxadmin.god")) return;
				if (player.net.connection.info.GetBool("global.god", true) || player.IsImmortal())
				{
				rust.RunClientCommand(player, "global.god false");
				}
			}
			if (!DisableNoclipOnNoBuild) return;
			if (DisableNoclipOnNoBuild)
			{
				if (_restricted.ContainsKey(player.userID)) return;

				if (player.CanBuild()) return;
				if (!player.CanBuild())
				{
					if (!player.IsFlying()) return;
					if (isAllowed(player, "fauxadmin.bypass")) return;
			   		if ((player.IsFlying()) && ((isAllowed(player, "fauxadmin.allowed")) || MeetsLevelReq(player)))
			   		{
						player.violationLevel = 0;
						
						var newPos = player.transform.position;
						DeactivateNoClip(player, newPos);
						return;
					}
				}
			}
		return;
		}

	private void DeactivateNoClip(BasePlayer player, Vector3 newPos)
		{
			if (player == null) return;
			if (_restricted.ContainsKey(player.userID)) return;
			timer.Repeat(0.1f, 10,() => ForcePlayerPosition(player, newPos));

			_restricted.Add(player.userID, new RestrictedData
			{
			player = player
			});
			SendReply(player, lang.GetMessage("restricted", this));
			rust.RunClientCommand(player, "noclip");
			timer.Once(1, () => _restricted.Remove(player.userID));
			return;
		}

	////////////////////////////////////////////////////////////////////////////////////////////////////////

        object OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            	if (block.OwnerID == 0 || player.userID == 0) return null;
		if (block.OwnerID == player.userID) return null;
            	if (block.OwnerID != player.userID && DisableFauxAdminDemolish)
            	{
                	return true;
            	}
            	return null;
        }

        object OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            	if (block.OwnerID == 0 || player.userID == 0) return null;
		if (block.OwnerID == player.userID) return null;
            	if (block.OwnerID != player.userID && DisableFauxAdminRotate)
            	{
                	return true;
            	}
        
            	return null;
        }

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player)
        {
            	if (block.OwnerID == 0 || player.userID == 0) return null;
		if (block.OwnerID == player.userID) return null;
            	if (block.OwnerID != player.userID && DisableFauxAdminUpgrade)
            	{
                	return true;
            	}
        
            	return null;
        }

	////////////////////////////////////////////////////////////////////////////////////////////////////////

	void OnPlayerSleepEnded(BasePlayer player)
		{

			if (isAllowed(player, "fauxadmin.blocked"))
            		{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                		return;
            		}

			if (player.net?.connection?.authLevel > 0) return;

			if (UseLevelActivation)
			{
				if (MeetsLevelReq(player))
				{
					player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
					return;
				}
			}

			if (!isAllowed(player, "fauxadmin.allowed"))
            		{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                		return;
            		}
			if (isAllowed(player, "fauxadmin.allowed"))
        		{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
				return;
			}
		return;
		}

	////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool MeetsLevelReq(BasePlayer player)
	{
		var playerlevel = player.ExperienceLevel;
		if (playerlevel >= ActivateFauxAdminOnLevel)
		{
			return true;
		}
		return false;
	}

	bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

	}
}