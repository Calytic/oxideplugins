using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
     	[Info("FreezeArrows", "Colon Blow", "1.0.4", ResourceId = 1601)]
     	class FreezeArrows : RustPlugin
	{

		bool Changed;

 		Dictionary<ulong, string> GuiInfo = new Dictionary<ulong, string>();
		Dictionary<ulong, FreezeArrowData> isFrozen = new Dictionary<ulong, FreezeArrowData>();
		Dictionary<ulong, ShotArrowData> loadArrow = new Dictionary<ulong, ShotArrowData>();

       		class FreezeArrowData
        	{
             		public BasePlayer player;
			public Vector3 oldPos;
        	}

       		class ShotArrowData
        	{
             		public BasePlayer player;
        	}

		void Loaded()
        	{           
			LoadVariables();            
        		lang.RegisterMessages(messages, this);
			permission.RegisterPermission("freezearrows.allowed", this);
		}

        	void LoadDefaultConfig()
        	{
            		Puts("Creating a new config file");
            		Config.Clear();
            		LoadVariables();
        	}

       		bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

////////Configuration Stuff////////////////////////////////////////////////////////////////////////////

		static int FreezeTime = 10;
		static int ReFreezeCooldown = 10;
		static int FreezeRadius = 5;
		static float FreezeOverlayTime = 10f;
		static bool useFreezeOverlay = true;
		static bool showHitExplosionFX = true;
		static bool freezePlayers = true;
		static bool freezeNPCs = true;

        	private void LoadVariables()
        	{
            		LoadConfigVariables();
            		SaveConfig();
        	}

        	private void LoadConfigVariables()
        	{
        		CheckCfg("Time - How long player is frozen when hit", ref FreezeTime);
			CheckCfg("Time - Cooldown for freezing same player again", ref ReFreezeCooldown);
			CheckCfg("Radius - The distance from impact players are effeted", ref FreezeRadius);
			CheckCfgFloat("Overlay - How long frozen overlay is shown when player is frozen", ref FreezeOverlayTime);
			CheckCfg("Overlay - Show freeze overlay when player is frozen", ref useFreezeOverlay);
			CheckCfg("Effects - Show hit explosion effect", ref showHitExplosionFX);
			CheckCfg("Targets - Arrows will freeze players", ref freezePlayers);
			CheckCfg("Targets - Arrows will freeze NPCs", ref freezeNPCs);
        	}

        	private void CheckCfg<T>(string Key, ref T var)
        	{
            	if (Config[Key] is T)
                	var = (T)Config[Key];
            	else
                	Config[Key] = var;
        	}

        	private void CheckCfgFloat(string Key, ref float var)
        	{

            	if (Config[Key] != null)
                	var = Convert.ToSingle(Config[Key]);
            	else
                	Config[Key] = var;
       	 	}

        	object GetConfig(string menu, string datavalue, object defaultValue)
        	{
            	var data = Config[menu] as Dictionary<string, object>;
            	if (data == null)
            		{
                		data = new Dictionary<string, object>();
                		Config[menu] = data;
                		Changed = true;
            		}
            		object value;
            	if (!data.TryGetValue(datavalue, out value))
            		{
                		value = defaultValue;
                		data[datavalue] = value;
                		Changed = true;
            		}
            		return value;
        	}
		
////////////////////////////////////////////////////////////////////////////////////////////////////////

        	Dictionary<string, string> messages = new Dictionary<string, string>()
        	{
			{"onnextshottxt", "Your next shot will be a Freeze Arrow" },
			{"offnextshottxt", "Your next shot will a Normal Arrow" },
            		{"yourfrozetxt", "You are frozen in place...." },
			{"unfrozetxt", "You are now unfrozen...." }
        	};

////////////////////////////////////////////////////////////////////////////////////////////////////////

		void OnPlayerAttack(BasePlayer player, HitInfo hitInfo, Vector3 newPos)
		{
			if (!HasPermission(player, "freezearrows.allowed")) return;
			if (!loadArrow.ContainsKey(player.userID)) return;
			if (usingCorrectWeapon(player))
			{
				findTarget(player, hitInfo, newPos);
				loadArrow.Remove(player.userID);
				if (showHitExplosionFX)
				{
				Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", hitInfo.HitPositionWorld);
				}
				return;
			}
		return;
		}

		bool usingCorrectWeapon(BasePlayer player)
		{
			Item activeItem = player.GetActiveItem();
        		if (activeItem != null && activeItem.info.shortname == "crossbow") return true;
			if (activeItem != null && activeItem.info.shortname == "bow.hunting") return true;
			return false;
		}

////////////////////////////////////////////////////////////////////////////////////////////////////////
	
		void findTarget(BasePlayer player, HitInfo hitInfo, Vector3 newPos)
		{
        		List<BasePlayer> plist = new List<BasePlayer>();
			List<BaseNPC> nlist = new List<BaseNPC>();

        		Vis.Entities<BasePlayer>(hitInfo.HitPositionWorld, FreezeRadius, plist);
			Vis.Entities<BaseNPC>(hitInfo.HitPositionWorld, FreezeRadius, nlist);

			if (freezePlayers)
			{
        			foreach (BasePlayer p in plist)
                		{
					if (isFrozen.ContainsKey(p.userID)) return;
				
					isFrozen.Add(p.userID, new FreezeArrowData
					{
					player = p,
					oldPos = p.transform.position
					});
					SendReply(p, lang.GetMessage("yourfrozetxt", this));
					repeater(p, hitInfo, newPos);
                		}
			}
			if (freezeNPCs)
			{
				foreach (BaseNPC n in nlist)
				{
                			n.state = BaseNPC.State.Sleeping;
                			n.sleep.Recover(FreezeTime);
                			n.StartCooldown(FreezeTime, true);
				}
			}
		}

		void repeater(BasePlayer p, HitInfo hitInfo, Vector3 newPos)
		{
			if (useFreezeOverlay) FrozenGui(p);
			timer.Repeat(0.1f, FreezeTime*10,() => freezeposition(p, hitInfo, newPos));
			timer.Once(FreezeOverlayTime, () => DestroyCui(p)); 
			timer.Once(FreezeOverlayTime, () => SendReply(p, lang.GetMessage("unfrozetxt", this)));
			timer.Once(ReFreezeCooldown, () => isFrozen.Remove(p.userID));
		
		}

		void freezeposition(BasePlayer p, HitInfo hitInfo, Vector3 newPos)
		{
                      	newPos = p.transform.position;
                      	ForcePlayerPosition(p, newPos);
                      	p.TransformChanged();
		}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void FrozenGui(BasePlayer player)
        {
            string guiInfo;
            if (GuiInfo.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);

            var elements = new CuiElementContainer();
            GuiInfo[player.userID] = CuiHelper.GetGuid();

            elements.Add(new CuiElement
                {
                    Name = GuiInfo[player.userID],
                    Components =
                    {
                        new CuiRawImageComponent { Sprite = "assets/content/ui/overlay_freezing.png" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

            CuiHelper.AddUi(player, elements);
        }

///////////////////////////////////////////////////////////////////////////////////////////////////////

	[ChatCommand("freezearrow")]
        void cmdChatfreezearrow(BasePlayer player, string command, string[] args)
	{	
	if (!HasPermission(player, "freezearrows.allowed")) return;
	if (loadArrow.ContainsKey(player.userID)) { loadArrow.Remove(player.userID); SendReply(player, lang.GetMessage("offnextshottxt", this)); return; }

		SendReply(player, lang.GetMessage("onnextshottxt", this));
		loadArrow.Add(player.userID, new ShotArrowData
		{
		player = player
		});
	}


///////////////////////////////////////////////////////////////////////////////////////////////////////

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
		isFrozen.Remove(player.userID);
                string guiInfo;
                if (GuiInfo.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);
            }
        }
	void DestroyCui(BasePlayer player)
	{
                string guiInfo;
                if (GuiInfo.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);
	}

	void OnPlayerRespawned(BasePlayer player)
	{
                DestroyCui(player);
		isFrozen.Remove(player.userID);
	}

	void OnPlayerDisconnected(BasePlayer player)
	{
                DestroyCui(player);
		isFrozen.Remove(player.userID);
	}

    }
	
}