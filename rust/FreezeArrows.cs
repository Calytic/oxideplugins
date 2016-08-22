using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
     	[Info("FreezeArrows", "Colon Blow", "1.0.7", ResourceId = 1601)]
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
			public int arrows;
			public bool arrowenabled;
        	}

		void Loaded()
        	{
			foreach (var player in BasePlayer.activePlayerList) 
			{
				if (!loadArrow.ContainsKey(player.userID))
				{
					loadArrow.Add(player.userID, new ShotArrowData
					{
					player = player,
					arrows = StartingArrowCount,
					arrowenabled = false
					});
				}
			}     
			LoadVariables();            
        		lang.RegisterMessages(messages, this);
			permission.RegisterPermission("freezearrows.allowed", this);
			permission.RegisterPermission("freezearrows.unlimited", this);
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
		static int ReFreezeCooldown = 120;
		static int FreezeRadius = 5;
		static float FreezeOverlayTime = 10f;
		static int StartingArrowCount = 1;
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
			CheckCfg("Arrows - Number of arrows on startup per player", ref StartingArrowCount);
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
            		{"nofreezearrows", "You have no freeze arrows left" },
            		{"unlimitedfreezearrows", "You have unlimited freeze arrows.. have fun :)" },
            		{"notoggle", "You have not toggled Freeze Arrows yet" },
			{"unfrozetxt", "You are now unfrozen...." }
        	};

////////////////////////////////////////////////////////////////////////////////////////////////////////

		void OnPlayerAttack(BasePlayer player, HitInfo hitInfo, Vector3 newPos, int arrows, bool arrowenabled)
		{
			if (!loadArrow.ContainsKey(player.userID)) return;
			if (!loadArrow[player.userID].arrowenabled) return;
			if (!HasPermission(player, "freezearrows.allowed")) return;
			if (usingCorrectWeapon(player))
			{
				var CurrentArrows = loadArrow[player.userID].arrows;
				findTarget(player, hitInfo, newPos);
				loadArrow[player.userID].arrowenabled = !loadArrow[player.userID].arrowenabled;
				if (showHitExplosionFX)
					{
					Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", hitInfo.HitPositionWorld);
					}
				if (HasPermission(player, "freezearrows.unlimited")) return;
				CurrentArrows = CurrentArrows - 1;
				loadArrow[player.userID].arrows = CurrentArrows;
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

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo, int arrows)
        {
		if (hitInfo == null) return;

            	if (!(hitInfo.Initiator is BasePlayer)) return;
		if (entity is BaseNPC || entity is BasePlayer)
		{
			var player = (BasePlayer)hitInfo.Initiator;
			if (HasPermission(player, "freezearrows.unlimited")) return;
			if (!usingCorrectWeapon(player)) return;
			if (usingCorrectWeapon(player))
			{
	    			loadArrow[player.userID].arrows = loadArrow[player.userID].arrows + 1;
           	 		PrintToChat(player, "You have added a freeze arrow to your quiver");
	    			PrintToChat(player, "Arrows Available: " + (loadArrow[player.userID].arrows));
				return;
			}
		return;
		}
	return;
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
		    Parent = "Overlay",
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
        void cmdChatfreezearrow(BasePlayer player, string command, string[] args, int arrows, bool arrowenabled)
	{	
		if (!HasPermission(player, "freezearrows.allowed")) return;
		if (!loadArrow.ContainsKey(player.userID))
			{
				loadArrow.Add(player.userID, new ShotArrowData
				{
				player = player,
				arrows = StartingArrowCount,
				arrowenabled = true
				});
				SendReply(player, lang.GetMessage("onnextshottxt", this));
				if (HasPermission(player, "freezearrows.unlimited")) return;

				SendReply(player, "Arrows Left: " + (loadArrow[player.userID].arrows));
			return;
			}
		if (HasPermission(player, "freezearrows.unlimited"))
			{
			loadArrow[player.userID].arrowenabled = true;
			SendReply(player, lang.GetMessage("onnextshottxt", this));
			return;
			}
		if (loadArrow[player.userID].arrows <= 0)
			{
			SendReply(player, lang.GetMessage("nofreezearrows", this)); 
			return;	
			}
		if (loadArrow[player.userID].arrows >= 1)
			{
			loadArrow[player.userID].arrowenabled = true;
			SendReply(player, lang.GetMessage("onnextshottxt", this));
			if (HasPermission(player, "freezearrows.unlimited")) return;

			SendReply(player, "Arrows Left: " + (loadArrow[player.userID].arrows));
			return;
			}
		return;
	}

	[ChatCommand("freezecount")]
        void cmdChatfreezecount(BasePlayer player, string command, string[] args, int arrows)
	{	
		if (!HasPermission(player, "freezearrows.allowed")) return;
		if (HasPermission(player, "freezearrows.unlimited"))
			{
			SendReply(player, lang.GetMessage("unlimitedfreezearrows", this));  
			return;
			}
		if (!loadArrow.ContainsKey(player.userID))
			{
			SendReply(player, lang.GetMessage("notoggle", this)); 
			return;
			}
		if (loadArrow[player.userID].arrows <= 0)
			{
			SendReply(player, lang.GetMessage("nofreezearrows", this)); 
			return;	
			}
		if (loadArrow[player.userID].arrows >= 1)
			{
			SendReply(player, "Arrows Left: " + (loadArrow[player.userID].arrows));
			return;
			}
		return;
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
			
	void OnPlayerSleepEnded(BasePlayer player)
	{
		if (!loadArrow.ContainsKey(player.userID))
			{
				loadArrow.Add(player.userID, new ShotArrowData
				{
				player = player,
				arrows = StartingArrowCount,
				arrowenabled = false
				});
			}
		DestroyCui(player);
		isFrozen.Remove(player.userID);
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