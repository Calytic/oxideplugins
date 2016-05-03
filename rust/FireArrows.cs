using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("FireArrows", "Colon Blow", "1.1.7")]
    class FireArrows : RustPlugin
    {
	bool Changed;
	
	Dictionary<ulong, FireArrowData> FireArrowOn = new Dictionary<ulong, FireArrowData>();
	Dictionary<ulong, FireBallData> FireBallOn = new Dictionary<ulong, FireBallData>();
	Dictionary<ulong, FireBombData> FireBombOn = new Dictionary<ulong, FireBombData>();
	Dictionary<ulong, string> GuiInfoFA = new Dictionary<ulong, string>();

        class FireArrowData
        {
             	public BasePlayer player;
        }

        class FireBombData
        {
             	public BasePlayer player;
        }

        class FireBallData
        {
             	public BasePlayer player;
        }

        void Loaded()
        {         
		LoadVariables();
            	lang.RegisterMessages(messagesFA, this);
		permission.RegisterPermission("firearrows.allowed", this);
		permission.RegisterPermission("firearrows.ball.allowed", this);
		permission.RegisterPermission("firearrows.bomb.allowed", this);
        }

        void LoadDefaultConfig()
        {
            	Puts("Creating a new config file");
            	Config.Clear();
            	LoadVariables();
        }
	

////////Configuration Stuff////////////////////////////////////////////////////////////////////////////

	static bool ShowArrowTypeIcon = true;
	static float DamageFireArrow = 50f;
	static float DamageFireBall = 200f;
	static float DamageFireBomb = 500f;
	static float DamageRadius = 1f;
	static float DurationFireArrow = 10f;
	static float DurationFireBallArrow = 10f;
	static float DurationFireBombArrow = 10f;

	static int cloth = 5;
	static int fuel = 5;
	static int oil = 5;
	static int explosives = 5;

	private string IconFireArrow = "http://i.imgur.com/3e8FWvt.png";
	private string IconFireBall = "http://i.imgur.com/USdpXGT.png";
	private string IconFireBomb = "http://i.imgur.com/0DpAHMn.png";

        private void LoadVariables()
        {
            	LoadConfigVariables();
            	SaveConfig();
        }

        private void LoadConfigVariables()
        {
        	CheckCfg("Icon - Show Arrow Type", ref ShowArrowTypeIcon);
        	CheckCfgFloat("Damage - Fire Arrow", ref DamageFireArrow);
        	CheckCfgFloat("Damage - Fire Ball Arrow", ref DamageFireBall);
        	CheckCfgFloat("Damage - Fire Bomb Arrow", ref DamageFireBomb);
		CheckCfgFloat("Damage - Radius", ref DamageRadius);
		CheckCfgFloat("Duration - Fire Arrow", ref DurationFireArrow);
		CheckCfgFloat("Duration - Fire Ball Arrow", ref DurationFireBallArrow);
		CheckCfgFloat("Duration - Fire Bomb Arrow", ref DurationFireBombArrow);
		CheckCfg("Required - All Arrows - Cloth Amount", ref cloth);
		CheckCfg("Required - All Arrows- Low Grade Fuel Amount", ref fuel);
		CheckCfg("Required - FireBall & FireBomb Arrows - Crude Oil", ref oil);
		CheckCfg("Required - FireBomb Arrows - Explosives", ref explosives);
        	CheckCfg("Icon - Fire Arrow", ref IconFireArrow);
        	CheckCfg("Icon - Fire Ball Arrow", ref IconFireBall);
        	CheckCfg("Icon - Fire Bomb Arrow", ref IconFireBomb);
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

////////Language Settings////////////////////////////////////////////////////////////////////////////

       	Dictionary<string, string> messagesFA = new Dictionary<string, string>()
            	{
                	{"firearrowtxt", "Your Arrows are set for Fire."},
			{"fireballarrowtxt", "Your Arrows are set for FireBall."},
			{"firebombarrowtxt", "Your Arrows are set for FireBomb."},
                	{"doesnothavemattxt", "You don't have required materials..."},
               	 	{"defaultarrowtxt", "Your Arrows are set for Normal."},
			{"deniedarrowtxt", "No Access to This Arrow Tier."}
            	};

////////Arrow Damage and FX control////////////////////////////////////////////////////////////////////

	void OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
	{
           	if (usingCorrectWeapon(player))
		{
			ArrowFX(player, hitInfo);
			return;
		}
	}

	void ArrowFX(BasePlayer player, HitInfo hitInfo)
	{
		if (FireArrowOn.ContainsKey(player.userID))
			{
			FireArrowFX(player, hitInfo);
			return;
			}
		if (FireBallOn.ContainsKey(player.userID))
			{
			FireBallFX(player, hitInfo);
			return;			
			}
		if (FireBombOn.ContainsKey(player.userID))
			{
			FireBombFX(player, hitInfo);
			return;
			}
		else
		return;
	}

	void FireArrowFX(BasePlayer player, HitInfo hitInfo)
	{
		if (!hasResources(player)) { tellDoesNotHaveMaterials(player); return; }
		applyBlastDamage(player, DamageFireArrow, Rust.DamageType.Heat, hitInfo);

		Effect.server.Run("assets/bundled/prefabs/fx/impacts/additive/fire.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", hitInfo.HitPositionWorld);
		FireArrow?.Spawn(true);
		timer.Once(DurationFireArrow, () => FireArrow.Kill());
		return;
	}

	void FireBallFX(BasePlayer player, HitInfo hitInfo)
	{
		if (!hasResources(player)) { tellDoesNotHaveMaterials(player); return; }
		applyBlastDamage(player, DamageFireBall, Rust.DamageType.Heat, hitInfo);
		timer.Once(1, () => applyBlastDamage(player, DamageFireBall, Rust.DamageType.Heat, hitInfo));
		timer.Once(2, () => applyBlastDamage(player, DamageFireBall, Rust.DamageType.Heat, hitInfo));
		timer.Once(3, () => applyBlastDamage(player, DamageFireBall, Rust.DamageType.Heat, hitInfo));

		Effect.server.Run("assets/bundled/prefabs/fx/survey_explosion.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireBallArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/napalm.prefab", hitInfo.HitPositionWorld);
		FireBallArrow?.Spawn(true);
		timer.Once(DurationFireBallArrow, () => FireBallArrow.Kill());
		return;
	}

	void FireBombFX(BasePlayer player, HitInfo hitInfo)
	{
		if (!hasResources(player)) { tellDoesNotHaveMaterials(player); return; }
		applyBlastDamage(player, DamageFireBomb, Rust.DamageType.Explosion, hitInfo);

		Effect.server.Run("assets/bundled/prefabs/fx/weapons/landmine/landmine_explosion.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireBombArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", hitInfo.HitPositionWorld);
		FireBombArrow?.Spawn(true);
		timer.Once(DurationFireBombArrow, () => FireBombArrow.Kill());
		return;
	}

	void applyBlastDamage(BasePlayer player, float damageamount, Rust.DamageType damagetype, HitInfo hitInfo)
	{
	playerBlastDamage(player, damageamount, damagetype, hitInfo);
	}

	void playerBlastDamage(BasePlayer player, float damageamount, Rust.DamageType damagetype, HitInfo hitInfo)
	{
	
        List<BaseCombatEntity> playerlist = new List<BaseCombatEntity>();
        Vis.Entities<BaseCombatEntity>(hitInfo.HitPositionWorld, DamageRadius, playerlist);

		foreach (BaseCombatEntity p in playerlist)
                {
		if (!(p is BuildingPrivlidge))
			{
			p.Hurt(damageamount, damagetype, player, true);
			}
                }
	}

////////Arrow Toggle Control ////////////////////////////////////////////////////////////////////////////

	void OnPlayerInput(BasePlayer player, InputState input)
        {
        	if (input.WasJustPressed(BUTTON.FIRE_THIRD))
		{
			ToggleArrowType(player);
		}
	}

	[ChatCommand("firearrow")]
        void cmdChatfirearrow(BasePlayer player, string command, string[] args, ulong arrowtype)
	{
		ToggleArrowType(player);
	}

	void ToggleArrowType(BasePlayer player)
       	{
		if (!usingCorrectWeapon(player)) return;

		if (FireArrowOn.ContainsKey(player.userID))
		{
			FireBallToggle(player);
			return;	
		}
		if (FireBallOn.ContainsKey(player.userID))
		{
			FireBombToggle(player);
			return;
		}
		if (FireBombOn.ContainsKey(player.userID))
		{
			NormalArrowToggle(player);
			return;
		}
		if ((!FireArrowOn.ContainsKey(player.userID)) || (!FireBallOn.ContainsKey(player.userID)) || (!FireBombOn.ContainsKey(player.userID)))
		{
			FireArrowToggle(player);
			return;
		}
		else
		NormalArrowToggle(player);
		return;
        }

	void NormalArrowToggle(BasePlayer player)
	{
		DestroyArrowData(player);
		SendReply(player, lang.GetMessage("defaultarrowtxt", this));
		DestroyCui(player);
		return;
	}

	void FireArrowToggle(BasePlayer player)
	{
		if (!IsAllowed(player, "firearrows.allowed"))
			{
			FireBallToggle(player);
			return;
			}
		DestroyArrowData(player);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		});
		SendReply(player, lang.GetMessage("firearrowtxt", this));
		DestroyCui(player);
		ArrowGui(player);
		return;
	}

	void FireBallToggle(BasePlayer player)
	{
		if (!IsAllowed(player, "firearrows.ball.allowed"))
			{
			FireBombToggle(player);
			return;
			}
		DestroyArrowData(player);
		FireBallOn.Add(player.userID, new FireBallData
		{
		player = player,
		});
		SendReply(player, lang.GetMessage("fireballarrowtxt", this));
		DestroyCui(player);
		ArrowGui(player);
		return;
	}

	void FireBombToggle(BasePlayer player)
	{
		if (!IsAllowed(player, "firearrows.bomb.allowed"))
			{
			NormalArrowToggle(player);
			return;
			}
		DestroyArrowData(player);
		FireBombOn.Add(player.userID, new FireBombData
		{
		player = player,
		});
		SendReply(player, lang.GetMessage("firebombarrowtxt", this));
		DestroyCui(player);
		ArrowGui(player);
		return;
	}

///////////Checks to see if player has resources for Arrow///////////////////////////////////////

	bool hasResources(BasePlayer player)
	{
		int cloth_amount = player.inventory.GetAmount(94756378);
		int fuel_amount = player.inventory.GetAmount(28178745);
		int oil_amount = player.inventory.GetAmount(1983936587);
		int explosives_amount = player.inventory.GetAmount(1755466030);

		if (FireArrowOn.ContainsKey(player.userID))
			{
			if (cloth_amount >= cloth && fuel_amount >= fuel)
				{
				player.inventory.Take(null, 28178745, fuel);
				player.inventory.Take(null, 94756378, cloth);
				return true;
				}
			return false;
			}
		if (FireBallOn.ContainsKey(player.userID))
			{
			if (cloth_amount >= cloth && fuel_amount >= fuel && oil_amount >= oil)
				{
				player.inventory.Take(null, 28178745, fuel);
				player.inventory.Take(null, 94756378, cloth);
				player.inventory.Take(null, 1983936587, oil);
				return true;
				}
			return false;
			}
		if (FireBombOn.ContainsKey(player.userID))
			{
			if (cloth_amount >= cloth && fuel_amount >= fuel && oil_amount >= oil && explosives_amount >= explosives)
				{
				player.inventory.Take(null, 28178745, fuel);
				player.inventory.Take(null, 94756378, cloth);
				player.inventory.Take(null, 1983936587, oil);
				player.inventory.Take(null, 1755466030, explosives);
				return true;
				}
			return false;
			}

	return false;
	}

////////Shows Arrow type icons on player screen////////////////////////////////////////////////////////////////

	void ArrowCui(BasePlayer player)
	{
	if (ShowArrowTypeIcon) ArrowGui(player);
	}

        void ArrowGui(BasePlayer player)
        {
	DestroyCui(player);

        var elements = new CuiElementContainer();
        GuiInfoFA[player.userID] = CuiHelper.GetGuid();

        if (ShowArrowTypeIcon)
	{
		if (FireArrowOn.ContainsKey(player.userID))
        	{
        	elements.Add(new CuiElement
                	{
                    	Name = GuiInfoFA[player.userID],
                    	Components =
                    		{
                        	new CuiRawImageComponent { Color = "1 1 1 1", Url = IconFireArrow, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        	new CuiRectTransformComponent { AnchorMin = "0.100 0.04",  AnchorMax = "0.15 0.12"}
                    		}
                	});
         	}
        	if (FireBallOn.ContainsKey(player.userID))
        	{
        	elements.Add(new CuiElement
                	{
                    	Name = GuiInfoFA[player.userID],
                    	Components =
                    		{
                        	new CuiRawImageComponent { Color = "1 1 1 1", Url = IconFireBall, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        	new CuiRectTransformComponent { AnchorMin = "0.100 0.04",  AnchorMax = "0.15 0.12"}
                    		}
                	});
         	}
		if (FireBombOn.ContainsKey(player.userID))
        	{
        	elements.Add(new CuiElement
                	{
                    	Name = GuiInfoFA[player.userID],
                    	Components =
                    		{
                        	new CuiRawImageComponent { Color = "1 1 1 1", Url = IconFireBomb, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        	new CuiRectTransformComponent { AnchorMin = "0.100 0.04",  AnchorMax = "0.15 0.12"}
                    		}
                	});
         	}

	}
         CuiHelper.AddUi(player, elements);
        }

////////Helpers////////////////////////////////////////////////////////////////////////////////
	
	void tellNotGrantedArrow(BasePlayer player)
	{
	SendReply(player, lang.GetMessage("deniedarrowtxt", this));
	}

	void tellDoesNotHaveMaterials(BasePlayer player)
	{
	SendReply(player, lang.GetMessage("doesnothavemattxt", this));
	}
        

	bool IsAllowed(BasePlayer player, string perm)
        {
        	if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
        	return false;
        }

	bool usingCorrectWeapon(BasePlayer player)
	{
	Item activeItem = player.GetActiveItem();
        if (activeItem != null && activeItem.info.shortname == "crossbow") return true;
	if (activeItem != null && activeItem.info.shortname == "bow.hunting") return true;
	return false;
	}

	void DestroyCui(BasePlayer player)
	{
		string guiInfo;
		if (GuiInfoFA.TryGetValue(player.userID, out guiInfo)) CuiHelper.DestroyUi(player, guiInfo);
	}

	void DestroyArrowData(BasePlayer player)
		{
		if (FireArrowOn.ContainsKey(player.userID))
			{
			FireArrowOn.Remove(player.userID);
			}
		if (FireBallOn.ContainsKey(player.userID))
			{
			FireBallOn.Remove(player.userID);
			}
		if (FireBombOn.ContainsKey(player.userID))
			{
			FireBombOn.Remove(player.userID);
			}
		else
		return;
		}

	void OnPlayerRespawned(BasePlayer player)
	{
                DestroyCui(player);
		DestroyArrowData(player);
	}

	void OnPlayerDisconnected(BasePlayer player, string reason)
	{
                DestroyCui(player);
		DestroyArrowData(player);
	}

    }


}