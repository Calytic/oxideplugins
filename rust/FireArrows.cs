using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("FireArrows", "Colon Blow", "1.1.5")]
    class FireArrows : RustPlugin
    {
	bool Changed;
	string SetArrowIcon;
	
	Dictionary<ulong, FireArrowData> FireArrowOn = new Dictionary<ulong, FireArrowData>();
	Dictionary<ulong, string> GuiInfoFA = new Dictionary<ulong, string>();

        class FireArrowData
        {
             	public BasePlayer player;
	     	public ulong arrowtype;
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
	static bool ShowFireOnDraw = true;
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
		CheckCfg("Effects - Show Fire on Arrow Draw", ref ShowFireOnDraw);
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

////////Player attack trigger////////////////////////////////////////////////////////////////////////

	void OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
	{
		if (FireArrowOn.ContainsKey(player.userID))
		{
            		if (usingCorrectWeapon(player))
			{
				ArrowFX(player, hitInfo);
				return;
			}
		}
	}

////////Sets Arrow types, messages and damages//////////////////////////////////////////////////////////

	void setInitialArrow(BasePlayer player, ulong arrowtype)
	{
		FireArrowOn.Remove(player.userID);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		arrowtype = 1
		});
		SendReply(player, lang.GetMessage("defaultarrowtxt", this));
		DestroyCui(player);
		return;
	}

	void setFireArrow(BasePlayer player, ulong arrowtype)
	{
		FireArrowOn.Remove(player.userID);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		arrowtype = 2
		});
		SendReply(player, lang.GetMessage("firearrowtxt", this));
		DestroyCui(player);
		SetArrowIcon = IconFireArrow;
		ArrowGui(player);
		return;
	}

	bool gotFireArrowReg(BasePlayer player)
	{
              	int cloth_amount = player.inventory.GetAmount(94756378);
		int fuel_amount = player.inventory.GetAmount(28178745);
		if (cloth == null) return false;
               	if (cloth_amount >= cloth && fuel_amount >= fuel)
               		{
			player.inventory.Take(null, 28178745, fuel);
			player.inventory.Take(null, 94756378, cloth);
                  	return true;
                  	}
		return false;
	}

	void setDamageFireArrow(BasePlayer player, HitInfo hitInfo)
	{
		Effect.server.Run("assets/bundled/prefabs/fx/impacts/additive/fire.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", hitInfo.HitPositionWorld);
		FireArrow?.Spawn(true);
		timer.Once(DurationFireArrow, () => FireArrow.Kill());
		return;
	}

	void setFireBallArrow(BasePlayer player, ulong arrowtype)
	{
		FireArrowOn.Remove(player.userID);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		arrowtype = 3
		});
		SendReply(player, lang.GetMessage("fireballarrowtxt", this));
		DestroyCui(player);
		SetArrowIcon = IconFireBall;
		ArrowGui(player);
		return;
	}

	void setDamageFireBall(BasePlayer player, HitInfo hitInfo)
	{
		
		Effect.server.Run("assets/bundled/prefabs/fx/survey_explosion.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireBallArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/napalm.prefab", hitInfo.HitPositionWorld);
		FireBallArrow?.Spawn(true);
		timer.Once(DurationFireBallArrow, () => FireBallArrow.Kill());
		return;
	}

	bool gotFireBallReg(BasePlayer player)
	{
              	int cloth_amount = player.inventory.GetAmount(94756378);
		int fuel_amount = player.inventory.GetAmount(28178745);
		int oil_amount = player.inventory.GetAmount(1983936587);
		if (cloth == null) return false;
               	if (cloth_amount >= cloth && fuel_amount >= fuel && oil_amount >= oil)
               		{
			player.inventory.Take(null, 28178745, fuel);
			player.inventory.Take(null, 94756378, cloth);
			player.inventory.Take(null, 1983936587, oil);
                  	return true;
                  	}
		return false;
	}

	void setFireBombArrow(BasePlayer player, ulong arrowtype)
	{
		FireArrowOn.Remove(player.userID);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		arrowtype = 4
		});
		SendReply(player, lang.GetMessage("firebombarrowtxt", this));
		DestroyCui(player);
		SetArrowIcon = IconFireBomb;
		ArrowGui(player);
		return;
	}

	void setDamageFireBomb(BasePlayer player, HitInfo hitInfo)
	{
		Effect.server.Run("assets/bundled/prefabs/fx/weapons/landmine/landmine_explosion.prefab", hitInfo.HitPositionWorld);
		BaseEntity FireBombArrow = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", hitInfo.HitPositionWorld);
		FireBombArrow?.Spawn(true);
		timer.Once(DurationFireBombArrow, () => FireBombArrow.Kill());
		return;
	}

	bool gotFireBombReg(BasePlayer player)
	{
        int cloth_amount = player.inventory.GetAmount(94756378);
	int fuel_amount = player.inventory.GetAmount(28178745);
	int oil_amount = player.inventory.GetAmount(1983936587);
	int explosives_amount = player.inventory.GetAmount(1755466030);

		if (cloth == null) return false;
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

	void setStandardArrow(BasePlayer player, ulong arrowtype)
	{
		FireArrowOn.Remove(player.userID);
		FireArrowOn.Add(player.userID, new FireArrowData
		{
		player = player,
		arrowtype = 1
		});
		SendReply(player, lang.GetMessage("defaultarrowtxt", this));
		DestroyCui(player);
		return;
	}
	
	void tellNotGrantedArrow(BasePlayer player)
	{
	SendReply(player, lang.GetMessage("deniedarrowtxt", this));
	}

	void tellDoesNotHaveMaterials(BasePlayer player)
	{
	SendReply(player, lang.GetMessage("doesnothavemattxt", this));
	}



	void OnUpdate(BasePlayer player, InputState input, ulong arrowtype)
	{
		if(Input.GetKey("space"))
		{
		SendReply(player, lang.GetMessage("doesnothavemattxt", this));
		}
	}

	void arrowTogglecheck(BasePlayer player, ulong arrowtype)
       	{
	if (!usingCorrectWeapon(player)) return;

	if (!(FireArrowOn.ContainsKey(player.userID)))
		{
			setInitialArrow(player, arrowtype);
			return;
		}
		
		if (FireArrowOn.ContainsKey(player.userID))
		{
		foreach (var FireArrowData in FireArrowOn.Values)
			{
			if (FireArrowOn[player.userID].arrowtype == 1)
				{
					if (!IsAllowed(player, "firearrows.allowed")) 	
						{ 
						setFireArrow(player, arrowtype);
						tellNotGrantedArrow(player);
						break;		
						}
					setFireArrow(player, arrowtype);
					return;	
				}
			if (FireArrowOn[player.userID].arrowtype == 2)
				{
					if (!IsAllowed(player, "firearrows.ball.allowed"))
						{ 
						setFireBallArrow(player, arrowtype);
						tellNotGrantedArrow(player);
						break;		
						}
					setFireBallArrow(player, arrowtype);
					return;
				}
				if (FireArrowOn[player.userID].arrowtype == 3)
				{
					if (!IsAllowed(player, "firearrows.bomb.allowed"))
						{ 
						
						setFireBombArrow(player, arrowtype);
						tellNotGrantedArrow(player);
						break;		
						}
					
					setFireBombArrow(player, arrowtype);
					return;
				}
				if (FireArrowOn[player.userID].arrowtype == 4)
				{
					setStandardArrow(player, arrowtype);
					return;
				}
			}
		return;
		}
        }

////////Changed Arrow Type with mousewheel click or chat command//////////////////////////////////////////////////

	void OnPlayerInput(BasePlayer player, InputState input, ulong arrowtype)
        {
        	if (input.WasJustPressed(BUTTON.FIRE_THIRD))
		{
		arrowTogglecheck(player, arrowtype);
		}
        	if (input.IsDown(BUTTON.FIRE_SECONDARY))
		{
		preFireFX(player);
		}
	}

	[ChatCommand("firearrow")]
        void cmdChatfirearrow(BasePlayer player, string command, string[] args, ulong arrowtype)
	{
	arrowTogglecheck(player, arrowtype);
	}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
		if (p is BuildingPrivlidge) return;
		p.Hurt(damageamount, damagetype, player, true);
                }
	}

////////Displays arrow effects and applies damages////////////////////////////////////////////////////////////

	void preFireFXeffect(BasePlayer player)
	{
	timer.Once(0.5f, () => Effect.server.Run("assets/bundled/prefabs/fx/impacts/additive/fire.prefab", player.eyes.position + player.eyes.BodyForward()));
	}

	void preFireFX(BasePlayer player)
	{
		if (!usingCorrectWeapon(player)) return;
		if (!ShowFireOnDraw) return;
		if (!(FireArrowOn.ContainsKey(player.userID)))
		{
			return;
		}

		if (FireArrowOn.ContainsKey(player.userID))
			foreach (var FireArrowData in FireArrowOn.Values)
				{

					if (FireArrowOn[player.userID].arrowtype == 1)
					{
					return;
					}
					if (FireArrowOn[player.userID].arrowtype == 2 && IsAllowed(player, "firearrows.allowed"))
					{
						preFireFXeffect(player);
					}
					if (FireArrowOn[player.userID].arrowtype == 3 && IsAllowed(player, "firearrows.ball.allowed"))
					{
						preFireFXeffect(player);
					}
					if (FireArrowOn[player.userID].arrowtype == 4 && IsAllowed(player, "firearrows.bomb.allowed"))
					{
						preFireFXeffect(player);
					}
				}
	}


	void ArrowFX(BasePlayer player, HitInfo hitInfo)
	{
			if (!(FireArrowOn.ContainsKey(player.userID)))
			{
			return;
			}

			if (FireArrowOn.ContainsKey(player.userID))
			{
				foreach (var FireArrowData in FireArrowOn.Values)
				{

					if (FireArrowOn[player.userID].arrowtype == 1)
					{
					return;
					}
					if (FireArrowOn[player.userID].arrowtype == 2 && IsAllowed(player, "firearrows.allowed"))
					{
						if (!gotFireArrowReg(player)) { tellDoesNotHaveMaterials(player); return; }
							applyBlastDamage(player, DamageFireArrow, Rust.DamageType.Heat, hitInfo);
							setDamageFireArrow(player, hitInfo);
							return;
					}
					if (FireArrowOn[player.userID].arrowtype == 3 && IsAllowed(player, "firearrows.ball.allowed"))
					{
						if (!gotFireBallReg(player)) { tellDoesNotHaveMaterials(player); return; }
							applyBlastDamage(player, DamageFireBall, Rust.DamageType.Heat, hitInfo);
							setDamageFireBall(player, hitInfo);
							return;
						
					}
					if (FireArrowOn[player.userID].arrowtype == 4 && IsAllowed(player, "firearrows.bomb.allowed"))
					{
						if (!gotFireBombReg(player)) { tellDoesNotHaveMaterials(player); return; }
						applyBlastDamage(player, DamageFireBomb, Rust.DamageType.Explosion, hitInfo);	
						setDamageFireBomb(player, hitInfo);
						return;
					}
				}
			}

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
        	elements.Add(new CuiElement
                {
                    Name = GuiInfoFA[player.userID],
                    Components =
                    {
                        new CuiRawImageComponent { Color = "1 1 1 1", Url = SetArrowIcon },
                        new CuiRectTransformComponent { AnchorMin = "0.100 0.04",  AnchorMax = "0.15 0.12"}
                    }
                });
            }

            CuiHelper.AddUi(player, elements);
	   //timer.Once(3f, () => DestroyCui(player));
        }

//////////////////////////////////////////////////////////////////////////////////////////////////////////

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

	void OnPlayerRespawned(BasePlayer player)
	{
                DestroyCui(player);
		FireArrowOn.Remove(player.userID);
	}

	void OnPlayerDisconnected(BasePlayer player, string reason)
	{
                DestroyCui(player);
		FireArrowOn.Remove(player.userID);
	}

    }


}