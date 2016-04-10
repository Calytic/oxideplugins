using Oxide.Core;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Flamethrower", "Colon Blow", "1.2.0", ResourceId = 1498)]
    class Flamethrower : RustPlugin
    {
        private float damageplayer => Config.Get<float>("BlastDamagetoPlayer");
        private float damagebuilding => Config.Get<float>("BlastDamagetoBuilding");
        private float damageNPC => Config.Get<float>("BlastDamagetoNPC");
        private float damageradius => Config.Get<float>("BlastDamageRadius");
	private bool prot => Config.Get<bool>("BlastDamageEffectedByProtectionValue");
	private float ReloadTime => Config.Get<float>("FlamethrowerReloadTime");
	private float gfminduration => Config.Get<float>("GroundFlameMinimumDuration");
	private float gfmaxduration => Config.Get<float>("GroundFlameMaximumDuration");
	private float gfspreadtime => Config.Get<float>("GroundFlameTimetoSpread");
	private float gfdps => Config.Get<float>("GroundFlameDamagePerSecond");
	private float gfdamageradius => Config.Get<float>("GroundFlameDamageRadius");
	private int fuel => Config.Get<int>("AmountRequired-LowGradeFuel");
	private int oil => Config.Get<int>("AmountRequired-CrudeOil");
	private bool weaponcanfail => Config.Get<bool>("EnableChanceOfWeaponFailure");
	private float flametimer;
	private float flamestart;

	private bool addtoflamestart = true;

        private readonly Dictionary<ulong, PlayerData> _flamethrower = new Dictionary<ulong, PlayerData>();

	string entprefab = "assets/bundled/prefabs/fireball.prefab";
	string fxprefab = "assets/bundled/prefabs/fx/impacts/additive/fire.prefab";
	string prefabweaponfailure = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
			
        class PlayerData
        {
        public BasePlayer player;
        }

        void OnServerInitialized()
        {

	flametimer = 0.01f;
	flamestart = 2.5f;
        }

        protected override void LoadDefaultConfig()
        {
		PrintWarning("Creating a new configuration file.");
           	Config.Clear();
        	Config["BlastDamagetoPlayer"] = 10.0f;
        	Config["BlastDamagetoBuilding"] = 10.0f;
        	Config["BlastDamagetoNPC"] = 10.0f;
		Config["BlastDamageRadius"] = 1.0f;
		Config["BlastDamageEffectedByProtectionValue"] = true;
		Config["FlamethrowerReloadTime"] = 4.0f;
		Config["GroundFlameMinimumDuration"] = 10.0f;
		Config["GroundFlameMaximumDuration"] = 10.0f;
		Config["GroundFlameDamagePerSecond"] = 2.0f;
		Config["GroundFlameTimetoSpread"] = 8.0f;
		Config["GroundFlameDamageRadius"] = 1.0f;
		Config["AmountRequired-LowGradeFuel"] = 5;
		Config["AmountRequired-CrudeOil"] = 5;
		Config["EnableChanceOfWeaponFailure"] = true;
        	SaveConfig();
        }

	void Loaded()
	{
	permission.RegisterPermission("flamethrower.allowed", this);
	}

        void LoadPermissions()
        {
        if (!permission.PermissionExists("flamethrower.allowed")) permission.RegisterPermission("flamethrower.allowed", this);
        }

        bool IsAllowed(BasePlayer player, string perm)
        {
        if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
        return false;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
		Item activeItem = player.GetActiveItem();
            	if (activeItem != null && activeItem.info.shortname == "smg.thompson" && input.WasJustPressed(BUTTON.USE))
            	{
			if (_flamethrower.ContainsKey(player.userID))
				{
				SendReply(player, "Flamethrower Reloading...");
				return;
				}

			if (!IsAllowed(player, "flamethrower.allowed")) return;
			if (IsAllowed(player, "flamethrower.allowed"))
			{
                   		int oil_amount = player.inventory.GetAmount(1983936587);
                    		if (oil_amount < oil)
                    			{
                        		SendReply(player, "You need more CRUDE OIL use Flamethrower");
                    			}
				
                    		int fuel_amount = player.inventory.GetAmount(28178745);
                    		if (fuel_amount < fuel)
                    			{
                        		SendReply(player, "Need more LOW GRADE FUEL to use Flamethrower");
                        		return;
                    			}

				player.inventory.Take(null, 28178745, fuel);
				player.inventory.Take(null, 1983936587, oil);

				flamethrowerFX(player);
				_flamethrower.Add(player.userID, new PlayerData{player = player});
				timer.Once(ReloadTime, () => _flamethrower.Remove(player.userID));
			}
	    	}
	}

	void flameFX(BasePlayer player)
	{

	        addtoflamestart = true;

		Effect.server.Run(fxprefab, player.eyes.position + player.eyes.BodyForward()*flamestart);
		applyBlastDamage(player);

		BaseEntity entity1 = GameManager.server.CreateEntity(entprefab, player.eyes.position + player.eyes.BodyForward()*flamestart);
		FireBall fball = entity1.GetComponent<FireBall>();
		fball.damagePerSecond = gfdps;
		fball.radius = gfdamageradius;
		fball.lifeTimeMin = gfminduration;
		fball.lifeTimeMax = gfmaxduration;
		fball.generation = gfspreadtime;

		entity1?.Spawn(true);

            	var raycastHits = Physics.RaycastAll(player.transform.position + Vector3.up, player.eyes.BodyForward(), flamestart).GetEnumerator();
            	var nearestDistance = flamestart;
            	Vector3? nearestPoint = null;
            	while (raycastHits.MoveNext())
            	{
                	if (raycastHits.Current == null) continue;
                	var raycastHit = (RaycastHit)raycastHits.Current;
                	if (raycastHit.distance < nearestDistance)
                	{
				nearestDistance = raycastHit.distance;
                    		nearestPoint = raycastHit.point;
		    		addtoflamestart = false;
                	}
		        if (raycastHit.distance > nearestDistance)
                	{
				nearestDistance = raycastHit.distance;
                    		nearestPoint = raycastHit.point;
		    		addtoflamestart = true;
                	}
			if (raycastHit.distance < 1.0f)
                	{
				nearestDistance = raycastHit.distance;
                    		nearestPoint = raycastHit.point;
		    		flamestart = nearestDistance;
                	}
            	}
		if(addtoflamestart)
		{
		flamestart = flamestart+0.30f;
		}
	}

	void flamethrowerFX(BasePlayer player)
	{
	chancetoFail(player);
	flameFXrepeater(player);
	}

	void flameFXrepeater(BasePlayer player)
	{
	flamestart = 2.5f;
	timer.Repeat(flametimer, 25,() => flameFX(player));
	}

	void applyBlastDamage(BasePlayer player)
	{
	playerBlastDamage(player);
	buildingBlastDamage(player);
	npcBlastDamage(player);
	}

	void playerBlastDamage(BasePlayer player)
	{
	
        List<BasePlayer> playerlist = new List<BasePlayer>();
        Vis.Entities<BasePlayer>(player.eyes.position + player.eyes.BodyForward()*flamestart, damageradius, playerlist);

        	foreach (BasePlayer p in playerlist)
                {
		p.Hurt(damageplayer, global::Rust.DamageType.Heat, player, prot);
                }
	}

	void buildingBlastDamage(BasePlayer player)
	{
	
        List<BuildingBlock> blocklist = new List<BuildingBlock>();
        Vis.Entities<BuildingBlock>(player.eyes.position + player.eyes.BodyForward()*flamestart, damageradius, blocklist);

        	foreach (BuildingBlock b in blocklist)
                {
		b.Hurt(damagebuilding, global::Rust.DamageType.Heat, player, prot);
                }
	}

	void npcBlastDamage(BasePlayer player)
	{
	
        List<BaseNPC> NPClist = new List<BaseNPC>();
        Vis.Entities<BaseNPC>(player.eyes.position + player.eyes.BodyForward()*flamestart, damageradius, NPClist);

        	foreach (BaseNPC n in NPClist)
                {
		n.Hurt(damageNPC, global::Rust.DamageType.Heat, player, prot);
                }
	}

	void chancetoFail(BasePlayer player)
	{
		if (weaponcanfail)
		{
			global::Rust.DamageType WeaponFailure = global::Rust.DamageType.Explosion;
			var roll = UnityEngine.Random.Range(0, 666);
			var badnumber = UnityEngine.Random.Range(0, 666);

			if (roll == badnumber)
			{
		    		List<BaseCombatEntity> entities666 = new List<BaseCombatEntity>();
                    		Vis.Entities<BaseCombatEntity>(player.transform.position, 5, entities666);

				SendReply(player, "Your Weapon Exploded in your face...");
			 	Effect.server.Run(prefabweaponfailure, player.transform.position);

                    		foreach (BaseCombatEntity w in entities666)
                   		{
					w.Hurt(500, global::Rust.DamageType.Explosion, null, false);
                    		}
			}
		}
	}
    }
}
