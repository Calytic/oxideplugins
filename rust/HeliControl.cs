using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("HeliControl", "Shady", "1.0.39", ResourceId = 1348)]
    [Description("Tweak various settings of helicopters. Plugin originally developed by koenrad.")]
    class HeliControl : RustPlugin
    {
        #region Config/Init
        private Dictionary<string, string> englishnameToShortname = new Dictionary<string, string>();       //for finding shortnames
        StoredData storedData = new StoredData();
        StoredData2 storedData2 = new StoredData2();
        StoredData3 storedData3 = new StoredData3();
        private float boundary;
        PatrolHelicopterAI HeliInstance = null;
        private List<BaseHelicopter> BaseHelicopters = new List<BaseHelicopter>();
        private HashSet<HelicopterDebris> Gibs = new HashSet<HelicopterDebris>();
        private HashSet<FireBall> FireBalls = new HashSet<FireBall>();
        FieldInfo timeBetweenRockets = typeof(PatrolHelicopterAI).GetField("timeBetweenRockets", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo maxRockets = typeof(PatrolHelicopterAI).GetField("maxRockets", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo numRocketsLeft = typeof(PatrolHelicopterAI).GetField("numRocketsLeft", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo tooHotUntil = typeof(HelicopterDebris).GetField("tooHotUntil", (BindingFlags.Instance | BindingFlags.NonPublic));
        bool configWasChanged = false;
        bool init = false;
        private const BaseNetworkable.DestroyMode NoDestroy = BaseNetworkable.DestroyMode.None;
        private System.Random rng = new System.Random(); //used for loot crates, better alternative -- should ensure it always returns a new random value, not re-using
        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        static string heliPrefab = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";

        bool DisableHeli => GetConfig("DisableHeli", false);
        bool DisableDefaultHeliSpawns => GetConfig("DisableDefaultHeliSpawns", false);
        bool ModifyDamageToHeli => GetConfig("ModifyDamageToHeli", false);
        bool UseGlobalDamageModifier => GetConfig("UseGlobalDamageModifier", false);
        bool UseCustomLoot => GetConfig("UseCustomLoot", false);
        bool DisableGibs => GetConfig("DisableGibs", false);
        bool DisableNapalm => GetConfig("DisableNapalm", false);
        bool LockedCrates => GetConfig("LockedCrates", true);
        bool AutoCallIfExists => GetConfig("AutoCallHeliIfAlreadyExists", false);
        bool CooldownPlayersCallMultiple => GetConfig("CooldownPlayersCanCallMultiple", false);
        bool CooldownPlayersCanTarget => GetConfig("CooldownPlayersCanCallToTarget", true);
        bool HelicopterCanShootWhileDying => GetConfig("HelicopterCanShootWhileDying", true);

        float GlobalDamageMultiplier => GetConfig("GlobalDamageMultiplier", 1.0f);
        float HeliBulletDamageAmount => GetConfig("HeliBulletDamageAmount", 20.0f);
        float MainRotorHealth => GetConfig("MainRotorHealth", 750.0f);
        float TailRotorHealth => GetConfig("TailRotorHealth", 375.0f);
        float BaseHealth => GetConfig("BaseHealth", 10000.0f);
        float HeliSpeed => GetConfig("HeliSpeed", 25.0f);
        float HeliAccuracy => GetConfig("HeliAccuracy", 2.0f);
        float TimeBeforeUnlocking => GetConfig("TimeBeforeUnlockingCrates", 0.0f);
        float TurretFireRate => GetConfig("TurretFireRate", 0.125f);
        float TurretburstLength => GetConfig("TurretBurstLength", 3f);
        float TurretTimeBetweenBursts => GetConfig("TurretTimeBetweenBursts", 3f);
        float TurretMaxRange => GetConfig("TurretMaxRange", 300f);
        float GibsTooHotLength => GetConfig("GibsTooHotLength", 480f);
        float GibsHealth => GetConfig("GibsHealth", 500f);
        float TimeBetweenRockets => GetConfig("TimeBetweenRockets", 0.2f);
        float CallHeliEvery => GetConfig("CallHeliEverySeconds", 0f);
        float RocketDamageBlunt => GetConfig("RocketDamageBlunt", 175);
        float RocketDamageExplosion => GetConfig("RocketDamageExplosion", 100);
        float RocketExplosionRadius => GetConfig("RocketExplosionRadius", 6);


        int MaxLootCrates => GetConfig("MaxLootCratesToDrop", 4);
        int MaxHeliRockets => GetConfig("MaxHeliRockets", 12);
        int BulletSpeed => GetConfig("BulletSpeed", 250);
        int LifeTimeMinutes => GetConfig("LifeTimeMinutes", 15);
        int WaterRequired => GetConfig("WaterRequiredToExtinguishNapalm", 10000);


        Dictionary<string, object> cds => GetConfig("Cooldowns", new Dictionary<string, object>());
        Dictionary<string, object> limits => GetConfig("Limits", new Dictionary<string, object>());

        [PluginReference]
        Plugin NTeleportation;


        
        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/

        protected override void LoadDefaultConfig()
        {
            //PrintWarning("Creating a new configuration file.");
           // Config.Clear();
            Config["DisableHeli"] = DisableHeli;
            Config["DisableDefaultHeliSpawns"] = DisableDefaultHeliSpawns;
            Config["ModifyDamageToHeli"] = ModifyDamageToHeli;
            Config["UseGlobalDamageModifier"] = UseGlobalDamageModifier;
            Config["UseCustomLoot"] = UseCustomLoot;
            Config["GlobalDamageMultiplier"] = GlobalDamageMultiplier;
            Config["HeliBulletDamageAmount"] = HeliBulletDamageAmount;
            Config["HelicopterCanShootWhileDying"] = HelicopterCanShootWhileDying;
            Config["MainRotorHealth"] = MainRotorHealth;
            Config["TailRotorHealth"] = TailRotorHealth;
            Config["BaseHealth"] = BaseHealth;
            Config["MaxLootCratesToDrop"] = MaxLootCrates;
            Config["HeliSpeed"] = HeliSpeed;
            Config["HeliAccuracy"] = HeliAccuracy;
            Config["MaxHeliRockets"] = MaxHeliRockets;
            Config["DisableGibs"] = DisableGibs;
            Config["DisableNapalm"] = DisableNapalm;
            Config["BulletSpeed"] = BulletSpeed;
            Config["LockedCrates"] = LockedCrates;
            Config["TimeBeforeUnlockingCrates"] = TimeBeforeUnlocking;
            Config["LifeTimeMinutes"] = LifeTimeMinutes;
            Config["TimeBetweenRockets"] = TimeBetweenRockets;
            Config["TurretFireRate"] = TurretFireRate;
            Config["TurretBurstLength"] = TurretburstLength;
            Config["TurretTimeBetweenBursts"] = TurretTimeBetweenBursts;
            Config["TurretMaxTargetRange"] = TurretMaxRange;
            Config["RocketDamageBlunt"] = RocketDamageBlunt;
            Config["RocketDamageExplosion"] = RocketDamageExplosion;
            Config["RocketExplosionRadius"] = RocketExplosionRadius;
            Config["GibsTooHotLength"] = GibsTooHotLength;
            Config["GibsHealth"] = GibsHealth;
            Config["CallHeliEverySeconds"] = CallHeliEvery;
            Config["AutoCallHeliIfAlreadyExists"] = AutoCallIfExists;
            Config["CooldownPlayersCanCallMultiple"] = CooldownPlayersCallMultiple;
            Config["WaterRequiredToExtinguishNapalm"] = WaterRequired;
            Config["CooldownPlayersCanCallToTarget"] = CooldownPlayersCanTarget;
            var i = 0;
            for (i = 1; i <= 10; i++) if (!cds.ContainsKey("Cooldown." + i)) cds.Add("Cooldown." + i, 86400f);
            for (i = 1; i <= 10; i++) if (!limits.ContainsKey("Limit." + i)) limits.Add("Limit." + i, 5);
            Config["Cooldowns"] = cds;
            Config["Limits"] = limits;
            SaveConfig();
        }
        /*--------------------------------------------------------------//
		//						Initial Setup							//
		//--------------------------------------------------------------*/
        void Init()
        {
            LoadDefaultConfig();
            var i = 0;
            if (limits.Keys.Count <= 0)
            {
                for (i = 1; i <= 10; i++)
                {
                    if (!limits.ContainsKey("Limit." + i))
                    {
                        limits.Add("Limit." + i, 5);
                        if (!configWasChanged) configWasChanged = true;
                    }
                }
            }
            
            if (cds.Keys.Count <= 0)
            {
                for (i = 1; i <= 10; i++)
                {
                    if (!cds.ContainsKey("Cooldown." + i))
                    {
                        cds.Add("Cooldown." + i, 86400f);
                        if (!configWasChanged) configWasChanged = true;
                    }
                }
            }
            
            if (configWasChanged)
            {
                Config["Cooldowns"] = cds; // unsure if needed
                Config["Limits"] = limits;
                SaveConfig();
            }

            var perms = new List<String>();
            perms.Add("callheli");
            perms.Add("killheli");
            perms.Add("shortname");
            perms.Add("strafe");
            perms.Add("update");
            perms.Add("destination");
            perms.Add("killnapalm");
            perms.Add("killgibs");
            perms.Add("unlockcrates");
            perms.Add("admin");
            perms.Add("ignorecooldown");
            perms.Add("ignorelimits");
            perms.Add("tpheli");
          foreach(var perm in cds.Keys) perms.Add(perm);

            foreach (var limit in limits.Keys) perms.Add(limit);

            for (int j = 0; j < perms.Count; j++) permission.RegisterPermission("helicontrol." + perms[j], this);
                
            LoadDefaultMessages();
        }



        /*--------------------------------------------------------------//
		//			Localization Stuff			                        //
		//--------------------------------------------------------------*/

        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                //DO NOT EDIT LANGUAGE FILES HERE! Navigate to oxide\lang\HeliControl.en.json
                {"noPerms", "You do not have permission to use this command!"},
                {"invalidSyntax", "Invalid Syntax, usage example: {0} {1}"},
                {"invalidSyntaxMultiple", "Invalid Syntax, usage example: {0} {1} or {2} {3}"},
                {"heliCalled", "Helicopter Inbound!"},
                {"helisCalledPlayer", "{0} Helicopter(s) called on: {1}"},
                {"entityDestroyed", "{0} {1}(s) were annihilated!"},
                {"helisForceDestroyed", "{0} Helicopter(s) were forcefully destroyed!"},
                {"heliAutoDestroyed", "Helicopter auto-destroyed because config has it disabled!" },
                {"playerNotFound", "Could not find player: {0}"},
                {"noHelisFound", "No active helicopters were found!"},
                {"cannotBeCalled", "This can only be called on a single Helicopter, there are: {0} active."},
                {"strafingYourPosition", "Helicopter is now strafing your position."},
                {"strafingOtherPosition", "Helicopter is now strafing {0}'s position."},
                {"destinationYourPosition", "Helicopter's destination has been set to your position."},
                {"destinationOtherPosition", "Helicopter's destination has been set to {0}'s position."},
                {"IDnotFound", "Could not find player by ID: {0}" },
                {"updatedHelis", "{0} helicopters were updated successfully!" },
                {"callheliCooldown", "You must wait before using this again! You've waited: {0}/{1}" },
                {"invalidCoordinate", "Incorrect argument supplied for {0} coordinate!" },
                {"coordinatesOutOfBoundaries", "Coordinates are out of map boundaries!" },
                {"callheliLimit", "You've used your daily limit of {0} heli calls!" },
                {"unlockedAllCrates", "Unlocked all Helicopter crates!" },
                {"teleportedToHeli", "You've been teleported to the ground below the active Helicopter!" },
                {"itemNotFound", "Item not found!" },
            };
            lang.RegisterMessages(messages, this);
        }

        private string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        /*----------------------------------------------------------------------------------------------------------------------------//
        //													HOOKS																	  //
        //----------------------------------------------------------------------------------------------------------------------------*/

        /*--------------------------------------------------------------//
		//					OnServerInitialized Hook					//
		//--------------------------------------------------------------*/
        void OnServerInitialized()
        {
            //Initialize the list of english to shortnames
            var startTime = UnityEngine.Time.realtimeSinceStartup;
            englishnameToShortname = new Dictionary<string, string>();
            var ItemsDefinition = ItemManager.GetItemDefinitions();
            for(int i = 0; i < ItemsDefinition.Count(); i++)
            {
                var itemdef = ItemsDefinition[i];
                if (itemdef == null) continue;
                englishnameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
            timer.Every(5f, () => CheckHelicopter());


            var allHelicopters = GameObject.FindObjectsOfType<BaseHelicopter>();
            var allDebris = GameObject.FindObjectsOfType<HelicopterDebris>();
            var allFireballs = GameObject.FindObjectsOfType<FireBall>();
            var events = GameObject.FindObjectsOfType<TriggeredEventPrefab>();
         
            var countH = 0;
            for(int i = 0; i < allHelicopters.Count(); i++)
            {
                var baseHeli = allHelicopters[i];
                if (!baseHeli.IsAlive() || baseHeli == null) continue;
                var patrolAI = baseHeli?.GetComponent<PatrolHelicopterAI>() ?? null;
                if (patrolAI == null) continue;
                BaseHelicopters.Add(baseHeli);
                HeliInstance = patrolAI;
                UpdateHelis(baseHeli, false);
                countH++;
            }
            for(int i = 0; i < allDebris.Count(); i++)
            {
                var gib = allDebris[i];
                if (gib == null) continue;
                Gibs.Add(gib);
            }
            for(int i = 0; i < allFireballs.Count(); i++)
            {
                var fb = allFireballs[i];
                if (fb == null) continue;
                var name = fb?.ShortPrefabName ?? string.Empty;
                if (!name.Contains("napalm") && !name.Contains("oil")) continue;
                FireBalls.Add(fb);
            }
            if(DisableDefaultHeliSpawns)
            {
                for (int i = 0; i < events.Count(); i++)
                {
                    var prefab = events[i];
                    var name = prefab?.targetPrefab?.resourcePath ?? "Unknown";
                    if (name.Contains("heli"))
                    {
                        GameObject.Destroy(prefab);
                        Puts("Disabled default Helicopter spawning.");
                        break;
                    }
                }
            }
            ConVar.PatrolHelicopter.bulletAccuracy = HeliAccuracy;
            ConVar.PatrolHelicopter.lifetimeMinutes = LifeTimeMinutes;
            //Get the saved drop list
            if (UseCustomLoot) LoadSavedData();
            LoadWeaponData();
            storedData3 = Interface.GetMod()?.DataFileSystem?.ReadObject<StoredData3>("HeliControlCooldowns") ?? null;

            boundary = TerrainMeta.Size.x / 2;
                init = true;
            if (CallHeliEvery <= 0f) return;
            timer.Every(CallHeliEvery, () =>
            {
                if (countAllHeli() >= 1 && !AutoCallIfExists) return;
                call(1);
            });
         
        }
        #endregion
        #region Hooks

        void Unload() => SaveData3();

        void OnServerSave() => timer.Once(3.5f, () => SaveData3()); //delay saving to avoid potential lag spikes



        /*--------------------------------------------------------------//
		//					OnEntitySpawned Hook						//
		//--------------------------------------------------------------*/
        void OnEntitySpawned(BaseNetworkable entity)
        {

            if (entity == null || !init) return;
            var heliname = entity?.ShortPrefabName ?? string.Empty;
            if (string.IsNullOrEmpty(heliname)) return;
            if (heliname.Contains("napalm") || heliname.Contains("oilfireball") && !heliname.Contains("rocket"))
            {
                var fireball = entity?.GetComponent<FireBall>() ?? null;
                if (fireball == null) return;
                if (DisableNapalm) entity.Kill(NoDestroy);
                if (!entity.isDestroyed)
                {
                    fireball.waterToExtinguish = WaterRequired;
                    fireball.SendNetworkUpdate(BasePlayer.NetworkQueue.Update); //may not be needed?
                    if (!FireBalls.Contains(fireball)) FireBalls.Add(fireball);
                }
            }

            if (heliname == "rocket_heli")
            {
                var explosion = entity?.GetComponent<TimedExplosive>() ?? null;
                var dmgTypes = explosion?.damageTypes ?? null;
                if (explosion == null || dmgTypes == null) return;
                explosion.explosionRadius = RocketExplosionRadius;
                for(int i = 0; i < dmgTypes.Count; i++)
                {
                    var dmg = dmgTypes[i];
                    if (dmg.type == Rust.DamageType.Blunt) dmg.amount = RocketDamageBlunt;
                    if (dmg.type == Rust.DamageType.Explosion) dmg.amount = RocketDamageExplosion;
                }
            }

                if (heliname == "heli_crate")
            {
                //check for config setting, and makes sure there is loot data before changing heli loot
                if (UseCustomLoot && storedData.HeliInventoryLists != null && storedData.HeliInventoryLists.Count > 0)
                {
                    var heli_crate = entity?.GetComponent<LootContainer>() ?? null;
                    if (heli_crate == null) return;
                    if (heli_crate.inventory == null) return; //possible that the inventory is somehow null? not sure
                    int index;
                    index = rng.Next(storedData.HeliInventoryLists.Count);
                    BoxInventory inv = storedData.HeliInventoryLists[index];
                    for(int i = 0; i < heli_crate.inventory.itemList.Count; i++) //perhaps this is a better method than .Clear()?
                    {
                        var item = heli_crate.inventory.itemList[i];
                        RemoveFromWorld(item);
                    }
                    for(int i = 0; i < inv.lootBoxContents.Count; i++)
                    {
                        var itemDef = inv.lootBoxContents[i];
                        if (itemDef == null) continue;
                        var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(itemDef.name).itemid, itemDef.amount, itemDef.skinID);
                        if (item == null) continue;
                        if (!item.MoveToContainer(heli_crate.inventory)) RemoveFromWorld(item); //ensure the item is completely removed if we can't move it, so we're not causing issues
                    }
                    heli_crate.inventory.MarkDirty();
                }

                if (TimeBeforeUnlocking != 0f)
                {
                    timer.Once(TimeBeforeUnlocking, () =>
                    {
                        if (entity == null || entity.isDestroyed) return;
                        var crate2 = entity?.GetComponent<LockedByEntCrate>() ?? null;
                        if (crate2 != null)
                        {
                            crate2.CancelInvoke("Think");
                            crate2.SetLocked(false);
                            crate2.lockingEnt = null;
                        }
                    });
                }

            }

            if (heliname.Contains("servergibs_patrolhelicopter"))
            {
                var debris = entity?.GetComponent<HelicopterDebris>() ?? null;
                if (debris == null) return;
                if (DisableGibs)
                {
                    if (!entity.isDestroyed) entity.Kill(NoDestroy);
                    return;
                }
                if (GibsHealth != 500f)
                {
                    debris.InitializeHealth(GibsHealth, GibsHealth);
                    debris.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
                Gibs.Add(debris);
                if (GibsTooHotLength != 480f) tooHotUntil.SetValue(debris, Time.realtimeSinceStartup + GibsTooHotLength);
            }


            if (heliname.Contains("patrolhelicopter") && !heliname.Contains("gibs"))
            {
                // Disable Helicopters
                if (DisableHeli)
                {
                    if (!entity.isDestroyed) entity.Kill(NoDestroy);
                    Puts(GetMessage("heliAutoDestroyed"));
                    return;
                }
                var AIHeli = entity?.GetComponent<PatrolHelicopterAI>() ?? null;
                var BaseHeli = entity?.GetComponent<BaseHelicopter>() ?? null;
                if (AIHeli == null || BaseHeli == null) return;
                HeliInstance = AIHeli;

                BaseHelicopters.Add(BaseHeli);
                UpdateHelis(BaseHeli, true);
            }
        }

        object CanBeTargeted(BaseCombatEntity entity, MonoBehaviour monoTurret)
        {
            if (!init || HelicopterCanShootWhileDying) return null;
            var turret = monoTurret?.GetComponent<HelicopterTurret>() ?? null;
            var aiHeli = turret?._heliAI ?? null;
            if (aiHeli == null) return null;

            var heliState = aiHeli?._currentState ?? PatrolHelicopterAI.aiState.IDLE;
            if (heliState == PatrolHelicopterAI.aiState.DEATH) return false;

            return null;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!init) return;
            var name = entity?.ShortPrefabName ?? string.Empty;
            if (name.Contains("patrolhelicopter") && !name.Contains("gib"))
            {
                var baseHeli = entity?.GetComponent<BaseHelicopter>() ?? null;
                if (baseHeli == null) return;
                if (BaseHelicopters.Contains(baseHeli)) BaseHelicopters.Remove(baseHeli);
            }
            if (name.Contains("oilfireball") || name.Contains("napalm"))
            {
                var fireball = entity?.GetComponent<FireBall>() ?? null;
                if (fireball == null) return;
                if (FireBalls.Contains(fireball)) FireBalls.Remove(fireball);
            }
            if (name.Contains("servergibs_patrolhelicopter.prefab"))
            {
                var debris = entity?.GetComponent<HelicopterDebris>() ?? null;
                if (debris == null) return;
                if (Gibs.Contains(debris)) Gibs.Remove(debris);
            }
        }

        /*--------------------------------------------------------------//
       //						OnPlayerAttack Hook						//
       //--------------------------------------------------------------*/
        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {

            if (attacker == null || hitInfo == null || hitInfo.HitEntity == null) return;
            var name = hitInfo?.HitEntity?.ShortPrefabName ?? string.Empty;

            if (name == "patrolhelicopter")         //We hit a helicopter
            {
                if (!ModifyDamageToHeli) return; //Check if damage modification is on
                var dmgMod = 0f;
                float.TryParse(GlobalDamageMultiplier.ToString(), out dmgMod);
                if (UseGlobalDamageModifier && dmgMod != 0f)        //Check for global modifier
                {
                    hitInfo?.damageTypes?.ScaleAll(dmgMod);
                    return;
                }

                var shortName = hitInfo?.Weapon?.GetItem()?.info?.shortname ?? null;    //weapon's shortname
                var displayName = hitInfo?.Weapon?.GetItem()?.info?.displayName?.english ?? null;
                float weaponConfig = 0.0f;
                if (shortName == null) return;
                storedData2.WeaponList.TryGetValue(shortName, out weaponConfig);
                if (weaponConfig == 0.0f) storedData2.WeaponList.TryGetValue(displayName, out weaponConfig);
                if (weaponConfig != 0.0f && weaponConfig != 1.0f) hitInfo.damageTypes.ScaleAll(weaponConfig);
                else if (dmgMod != 1.0 && UseGlobalDamageModifier) hitInfo.damageTypes.ScaleAll(dmgMod);
            }
        }
        #endregion
        #region Main
        /*----------------------------------------------------------------------------------------------------------------------------//
        //													CORE FUNCTIONS															  //
        //----------------------------------------------------------------------------------------------------------------------------*/
        private void UpdateHelis(BaseHelicopter heli, bool justCreated = false)
        {
            if (heli == null) return;
            heli.startHealth = BaseHealth;
            if (justCreated) heli.InitializeHealth(BaseHealth, BaseHealth);
            heli.maxCratesToSpawn = MaxLootCrates;
            heli.bulletDamage = HeliBulletDamageAmount;
            heli.bulletSpeed = BulletSpeed;
            var weakspots = heli.weakspots;
            if (justCreated)
            {
                weakspots[0].health = MainRotorHealth;
                weakspots[1].health = TailRotorHealth;
            }
            weakspots[0].maxHealth = MainRotorHealth;
            weakspots[1].maxHealth = TailRotorHealth;
            var heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
            if (heliAI == null) return;
            heliAI.maxSpeed = HeliSpeed;
            timeBetweenRockets.SetValue(heliAI, TimeBetweenRockets);
            maxRockets.SetValue(heliAI, MaxHeliRockets);
            numRocketsLeft.SetValue(heliAI, MaxHeliRockets);
            updateTurrets(heliAI);
            heli.SendNetworkUpdateImmediate(justCreated);
        }

            //"callheliCmd" is essentially pointless I have no idea what I was thinking when I made this
            void callheliCmd(int amountToCall, BasePlayer target = null)
        {
            if (amountToCall <= 1 && target == null)
            {
                call();
                return;
            }
            if (target != null) callOther(target.transform.position, amountToCall);
        }
        /*--------------------------------------------------------------//
		//			callOther - call heli on other person				//
		//--------------------------------------------------------------*/
        private void callOther(Vector3 coordinates, int num)
        {
            int i = 0;
            while (i < num)
            {
                var entity = GameManager.server.CreateEntity(heliPrefab, new Vector3(), new Quaternion(), true);
                if (!entity || entity == null) return;
                var heliAI = entity?.GetComponent<PatrolHelicopterAI>() ?? null;
                if (heliAI == null) continue;
                heliAI.SetInitialDestination(coordinates + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                entity.Spawn();
                i++;
            }
        }

        BaseHelicopter callCoordinates(Vector3 coordinates) //potentially useful for external plugin calls
        {
                var heli = (BaseHelicopter)GameManager.server.CreateEntity(heliPrefab, new Vector3(), new Quaternion(), true);
                if (!heli) return null;
                var heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
            if (heliAI == null) return null;
                heliAI.SetInitialDestination(coordinates + new Vector3(0f, 10f, 0f), 0.25f);
                heli.Spawn();
            return heli;
        }

        /*--------------------------------------------------------------//
		//					call - call heli in general					//
		//--------------------------------------------------------------*/
        private void call(int num = 1)
        {
            int i = 0;
            while (i < num)
            {
                var entity = GameManager.server.CreateEntity(heliPrefab, new Vector3(), new Quaternion(), true);
                if (!entity)
                    return;
                entity.Spawn();
                i++;
            }
        }
        private void updateTurrets(PatrolHelicopterAI helicopter)
        {
            if (helicopter == null) return;
            var guns = new List<HelicopterTurret>();
            guns.Add(helicopter.leftGun);
            guns.Add(helicopter.rightGun);
            for(int i = 0; i < guns.Count; i++)
            {
                var turret = guns[i];
                turret.fireRate = TurretFireRate;
                turret.timeBetweenBursts = TurretTimeBetweenBursts;
                turret.burstLength = TurretburstLength;
                turret.maxTargetRange = TurretMaxRange;
            }
        }
        /*--------------------------------------------------------------//
       //				killAll - produces no loot drops		        //
       //--------------------------------------------------------------*/
        private int killAll(bool isForced = false)
        {
            CheckHelicopter();
            int count = 0;
            var amount = BaseHelicopters.Count;
            if (BaseHelicopters.Count <= 0) return count;
            for(int i = 0; i < BaseHelicopters.Count; i++)
            {
                var helicopter = BaseHelicopters[i];
                if (helicopter == null) continue;
                helicopter.maxCratesToSpawn = 0;        //comment this line if you want loot drops with killheli
                if (isForced == true) helicopter.Kill(NoDestroy);
                else helicopter.DieInstantly();
                count++;
            }
            CheckHelicopter();
            return count;
        }
        #endregion
        #region Commands
        /*----------------------------------------------------------------------------------------------------------------------------//
       //												CHAT COMMANDS																  //
       //----------------------------------------------------------------------------------------------------------------------------*/
        [ChatCommand("unlockcrates")]
        private void cmdUnlockCrates(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "unlockcrates")) return;
            var crates = GameObject.FindObjectsOfType<LockedByEntCrate>();
            var count = 0;
            for(int i = 0; i < crates.Count(); i++)
            {
                var crate = crates[i];
                if (crate == null) continue;
                crate.CancelInvoke("Think");
                crate.SetLocked(false);
                crate.lockingEnt = (GameObject)null;
                count++;
            }
            SendReply(player, GetMessage("unlockedAllCrates"));
        }

        [ChatCommand("tpheli")]
        private void cmdTeleportHeli(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "tpheli")) return;
            if (HeliInstance == null || !HeliInstance.IsAlive())
            {
                SendReply(player, GetMessage("noHelisFound"));
                return;
            }
            if (countAllHeli() >= 2)
            {
                SendReply(player, string.Format(GetMessage("cannotBeCalled"), countAllHeli().ToString()));
                return;
            }
            if (NTeleportation == null)
            {
                SendReply(player, "NTeleportation must be installed for this to work!");
                return;
            }
            var ground = GetGround(HeliInstance.transform.position);
            //NTeleportation.Call("Teleport", player, ground); // not working currently?
            rust.RunServerCommand("teleport.topos \"" + player.UserIDString + "\" " + ground.x + " " + ground.y + " " + ground.z); // temp workaround
            SendReply(player, GetMessage("teleportedToHeli"));
        }

        [ChatCommand("callheli")]
        private void cmdCallToPlayer(BasePlayer player, string command, string[] args)
        {
            var cooldownTime = GetLowestCooldown(player);
            var limit = 0;
            var today = DateTime.Now.ToString("d");
            limit = GetHighestLimit(player);
            if (limit <= 0 && !ignoreLimits(player) && !canExecute(player, "callheli")) return;
            if (!canExecute(player, "callheli", false))
            {
                if (storedData3.timesCalled.ContainsKey(player.userID) && storedData3.lastCalled.ContainsKey(player.userID) && !ignoreLimits(player) && limit >= 1)
                {
                    var timesCalled = storedData3.timesCalled[player.userID];

                    var lastDate = storedData3.lastCalled[player.userID];
                    if (timesCalled + 1 > limit && today == lastDate)
                    {
                        SendReply(player, string.Format(GetMessage("callheliLimit"), limit));
                        return;
                    }
                    else storedData3.timesCalled[player.userID] = 0;
                }
                if (!ignoreCooldown(player) && cooldownTime != 0f)
                {
                    if (countAllHeli() >= 1 && !CooldownPlayersCallMultiple)
                    {
                        SendReply(player, string.Format(GetMessage("cannotBeCalled"), countAllHeli().ToString()));
                        return;
                    }
                    cooldownTime = GetLowestCooldown(player);


                    var now = UnityEngine.Time.realtimeSinceStartup;
                    if (!storedData3.cooldowns.ContainsKey(player.userID)) storedData3.cooldowns.Add(player.userID, now - cooldownTime);

                    var cooldown = now - storedData3.cooldowns[player.userID];

                    var diff = TimeSpan.FromSeconds(cooldown);
                    var cooldownDiff = TimeSpan.FromSeconds(cooldownTime);
                    var waitedString = "";
                    var timeToWait = "";
                    if (diff.TotalSeconds >= 0.01) waitedString = Math.Floor(diff.TotalSeconds) + "s";
                    if (diff.TotalMinutes >= 1) waitedString = Math.Floor(diff.TotalMinutes) + "m";
                    if (diff.TotalHours >= 1) waitedString = Math.Floor(diff.TotalHours) + "h";
                    if (cooldownDiff.TotalSeconds >= 0.01) timeToWait = Math.Floor(cooldownDiff.TotalSeconds) + "s";
                    if (cooldownDiff.TotalMinutes > 1) timeToWait = Math.Floor(cooldownDiff.TotalMinutes) + "m";
                    if (cooldownDiff.TotalHours > 1) timeToWait = Math.Floor(cooldownDiff.TotalHours) + "h";
                    if (cooldown < cooldownTime)
                    {
                        SendReply(player, string.Format(GetMessage("callheliCooldown"), waitedString, timeToWait));
                        return;
                    }
                }
            }
            if (!canExecute(player, "callheli") && cooldownTime == 0f)
            {
                return; //if cooldown time is not 0f, then it means this player is permitted because of cooldown permission
            }

            if (args.Length == 0 || !canExecute(player, "callheli", false) && !CooldownPlayersCanTarget)
            {
                callheliCmd(1);
                SendReply(player, GetMessage("heliCalled"));
                if (storedData3.cooldowns.ContainsKey(player.userID) && cooldownTime != 0f) storedData3.cooldowns[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                if (!storedData3.timesCalled.ContainsKey(player.userID)) storedData3.timesCalled.Add(player.userID, 1);
                else storedData3.timesCalled[player.userID]++;
                if (!storedData3.lastCalled.ContainsKey(player.userID)) storedData3.lastCalled.Add(player.userID, today);
                else storedData3.lastCalled[player.userID] = today;
                return;
            }
            BasePlayer target = null;
            ulong ID = 0;
         
            if (args.Length >= 1)
            {
                target = FindPlayerByPartialName(args[0]);
                if (ulong.TryParse(args[0], out ID)) target = FindPlayerByID(ID);
                if (target == null)
                {
                    SendReply(player, string.Format(GetMessage("playerNotFound"), args[0]));
                    return;
                }
            }

            int num = 1;
            if (args.Length == 2 && canExecute(player, "callheli"))
            {
                var result = int.TryParse(args[1], out num);
                if (!result) num = 1;
            }

            callheliCmd(num, target);
            SendReply(player, string.Format(GetMessage("helisCalledPlayer"), num, target.displayName));
            if (storedData3.cooldowns.ContainsKey(player.userID) && cooldownTime != 0f) storedData3.cooldowns[player.userID] = UnityEngine.Time.realtimeSinceStartup;
            if (!storedData3.timesCalled.ContainsKey(player.userID)) storedData3.timesCalled.Add(player.userID, 1);
            else storedData3.timesCalled[player.userID]++;
            if (!storedData3.lastCalled.ContainsKey(player.userID)) storedData3.lastCalled.Add(player.userID, today);
            else storedData3.lastCalled[player.userID] = today;
        }

        /*--------------------------------------------------------------//
		//					Chat Command for killheli					//
		//--------------------------------------------------------------*/
        [ChatCommand("killheli")]
        private void cmdKillHeli(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killheli")) return;
            int numKilled = 0;
            if (args.Length == 0) numKilled = killAll();
 
            if (args.Length >= 1)
            {
                if (args[0] == "forced")
                {
                    numKilled = killAll(true);
                    SendReply(player, string.Format(GetMessage("helisForceDestroyed"), numKilled.ToString(), new object[0]));
                    return;
                }
                else
                {
                    SendReply(player, string.Format(GetMessage("invalidSyntaxMultiple"), "/killheli", "", "/killheli", "forced"));
                    return;
                }
            }
            SendReply(player, string.Format(GetMessage("entityDestroyed"), numKilled.ToString(), "helicopter"));
        }

        [ChatCommand("updatehelis")]
        private void cmdUpdateHelicopters(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "update")) return;
            CheckHelicopter();
            if (countAllHeli() <= 0)
            {
                SendReply(player, GetMessage("noHelisFound"));
                return;
            }
            var count = 0;
            foreach(var helicopter in BaseHelicopters)
            {
                if (helicopter == null) continue;
                UpdateHelis(helicopter, false);
                count++;
            }
            SendReply(player, string.Format(GetMessage("updatedHelis"), count));
        }

        [ChatCommand("strafe")]
        private void cmdStrafeHeli(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "strafe")) return;
            if (HeliInstance == null || !HeliInstance.IsAlive())
            {
                SendReply(player, GetMessage("noHelisFound"));
                return;
            }
            if (countAllHeli() >= 2)
            {
                SendReply(player, string.Format(GetMessage("cannotBeCalled"), countAllHeli().ToString()));
                return;
            }
            if (args.Length <= 0)
            {
                HeliInstance.State_Strafe_Enter(player.transform.position, HeliInstance.CanUseNapalm());
                SendReply(player, GetMessage("strafingYourPosition"));
                return;
            }
            if (args.Length >= 1)
            {
                BasePlayer target = null;
                
                ulong ID = 0;

                if (args.Length >= 1)
                {
                    target = FindPlayerByPartialName(args[0]);
                    if (ulong.TryParse(args[0], out ID)) target = FindPlayerByID(ID);
                    if (target == null)
                    {
                        SendReply(player, string.Format(GetMessage("playerNotFound"), args[0]));
                        return;
                    }
                }
                HeliInstance.State_Strafe_Enter(target.transform.position, HeliInstance.CanUseNapalm());
                SendReply(player, string.Format(GetMessage("strafingOtherPosition"), target.displayName));
            }
        }


        [ChatCommand("helidest")]
        private void cmdDestChangeHeli(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "destination")) return;
            if (HeliInstance == null || !HeliInstance.IsAlive())
            {
                SendReply(player, GetMessage("noHelisFound"));
                return;
            }
            if (countAllHeli() >= 2)
            {
                SendReply(player, string.Format(GetMessage("cannotBeCalled"), countAllHeli().ToString()));
                return;
            }
            if (args.Length <= 0)
            {
                HeliInstance.SetTargetDestination(player.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                SendReply(player, GetMessage("destinationYourPosition"));
                return;
            }
            if (args.Length >= 1)
            {
                BasePlayer target = null;
                ulong ID = 0;

                if (args.Length >= 1)
                {
                    target = FindPlayerByPartialName(args[0]);
                    if (ulong.TryParse(args[0], out ID)) target = FindPlayerByID(ID);
                    if (target == null)
                    {
                        SendReply(player, string.Format(GetMessage("playerNotFound"), args[0]));
                        return;
                    }
                }
                HeliInstance.SetTargetDestination(target.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                SendReply(player, string.Format(GetMessage("destinationOtherPosition"), target.displayName));
            }
        }


        /*--------------------------------------------------------------//
        //					Chat Command for killfireballs				//
        //--------------------------------------------------------------*/

        [ChatCommand("killnapalm")]
        private void cmdKillFB(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killnapalm")) return;
            SendReply(player, string.Format(GetMessage("entityDestroyed"), killAllFB().ToString(), "fireball"));
        }



        /*--------------------------------------------------------------//
		//					Chat Command for killgibs					//
		//--------------------------------------------------------------*/
        [ChatCommand("killgibs")]
        private void cmdKillGibs(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killgibs")) return;
            SendReply(player, string.Format(GetMessage("entityDestroyed"), killAllGibs().ToString(), "helicopter gib"));
        }


        /*--------------------------------------------------------------//
		//				Chat Command for getshortname					//
		//--------------------------------------------------------------*/
        [ChatCommand("getshortname")]
        private void cmdGetShortName(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "shortname")) return;

            if (args == null || args.Length == 0)
            {
                SendReply(player, string.Format(GetMessage("invalidSyntax"), "/getshortname", "<item name>"));
                return;
            }
            string engName = "";// = args[0];
            if (args.Length > 1)
            {
                foreach (string arg in args) engName = engName + arg + " ";
                engName = engName.Substring(0, engName.Length - 1);
            }
            else engName = args[0];

            engName = engName.ToLower();

            if (englishnameToShortname.ContainsKey(engName)) SendReply(player, engName +  " is " + "\"" + englishnameToShortname[engName] + "\"");
            else SendReply(player, GetMessage("itemNotFound"));
        }



        /*----------------------------------------------------------------------------------------------------------------------------//
        //													CONSOLE COMMANDS														  //
        //----------------------------------------------------------------------------------------------------------------------------*/

        /*--------------------------------------------------------------//
		//				Console Command for callheli					//
		//--------------------------------------------------------------*/
        [ConsoleCommand("callheli")]
        private void consoleCallHeli(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player() ?? null;
            if (player != null && !canExecute(player, "callheli")) return;

            if (arg.Args == null || arg?.Args?.Length <= 0)
            {
                callheliCmd(1, null);
                SendReply(arg, GetMessage("heliCalled"));
                return;
            }
            if (arg.Args[0].ToLower() == "pos" && arg.Args.Length <= 3)
            {
                SendReply(arg, "You must supply 3 args for coordinates!");
                return;
            }

            if (arg.Args[0].ToLower() == "pos")
            {
                var coords = default(Vector3);
                var callNum = 1;
                if (!float.TryParse(arg.Args[1], out coords.x))
                {
                    SendReply(arg, string.Format(GetMessage("invalidCoordinate"), "X"));
                    return;
                }
                if (!float.TryParse(arg.Args[2], out coords.y))
                {
                    SendReply(arg, string.Format(GetMessage("invalidCoordinate"), "Y"));
                    return;
                }
                if (!float.TryParse(arg.Args[3], out coords.z))
                {
                    SendReply(arg, string.Format(GetMessage("invalidCoordinate"), "Z"));
                    return;
                }
                if (!CheckBoundaries(coords.x, coords.y, coords.z))
                {
                    SendReply(arg, GetMessage("coordinatesOutOfBoundaries"));
                    return;
                }
                if (arg.Args.Length >= 5)
                {
                   
                    if (int.TryParse(arg.Args[4], out callNum)) callOther(coords, callNum);
                    else
                    {
                        callOther(coords, callNum);
                        SendReply(arg, string.Format(GetMessage("helisCalledPlayer"), callNum, coords));
                    }
                }
                else callOther(coords, callNum);
                SendReply(arg, string.Format(GetMessage("helisCalledPlayer"), callNum, coords));
                return;
            }

            BasePlayer target = null;
            target = FindPlayerByPartialName(arg.Args[0]);
            ulong ID = 0;

                if (ulong.TryParse(arg.Args[0], out ID)) target = FindPlayerByID(ID);
                if (target == null)
                {
                    SendReply(arg, string.Format(GetMessage("playerNotFound"), arg.Args[0]));
                    return;
                }

            int num = 1;
            if (arg.Args.Length == 2)
            {
                var result = int.TryParse(arg.Args[1], out num);
                if (!result) num = 1;
            }

            callheliCmd(num, target);
            SendReply(arg, string.Format(GetMessage("helisCalledPlayer"), num, target.displayName));
        }






        /*--------------------------------------------------------------//
		//				Console Command for getshortname				//
		//--------------------------------------------------------------*/
        [ConsoleCommand("getshortname")]
        private void consoleGetShortName(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player() ?? null;
            if (player != null && !canExecute(player, "shortname")) return;
            if (arg.Args == null || arg?.Args?.Length <= 0)
            {
                SendReply(arg, string.Format(GetMessage("invalidSyntax"), "getshortname", "<item name>"));
                return;
            }

            string engName = "";// = args[0];
            if (arg.Args.Length > 1)
            {
                foreach (string str in arg.Args) engName = engName + str + " ";
                engName = engName.Substring(0, engName.Length - 1);
            }
            else engName = arg.Args[0];
            engName = engName.ToLower();

            if (englishnameToShortname.ContainsKey(engName)) SendReply(arg, "\"" + engName + "\" is \"" + englishnameToShortname[engName] + "\"");
            else SendReply(arg, GetMessage("itemNotFound"));
        }


     

        /*--------------------------------------------------------------//
		//				Console Command for killheli					//
		//--------------------------------------------------------------*/
        [ConsoleCommand("killheli")]
        private void consoleKillHeli(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player() ?? null;
            if (player != null && !canExecute(player, "killheli")) return;

            int numKilled = killAll();
            SendReply(arg, string.Format(GetMessage("entityDestroyed"), numKilled.ToString(), "helicopter"));
        }
        #endregion
        #region Util
        public void CheckHelicopter()
        {
            BaseHelicopters.RemoveAll(p => !p);
            Gibs.RemoveWhere(p => !p);
            Gibs.RemoveWhere(p => p == null);
            FireBalls.RemoveWhere(p => !p);
            FireBalls.RemoveWhere(p => p == null);
        }

        private int countAllHeli()
        {
            CheckHelicopter();
            return BaseHelicopters.Count;
        }

        //**Borrowed from Nogrod's NTeleportation, with permission**//
        private Vector3 GetGround(Vector3 sourcePos)
        {
            var oldPos = sourcePos;
            sourcePos.y = TerrainMeta.HeightMap.GetHeight(sourcePos);
            RaycastHit hitinfo;
            if (Physics.SphereCast(oldPos, .1f, Vector3.down, out hitinfo, groundLayer))
                sourcePos.y = hitinfo.point.y;
            return sourcePos;
        }


        private int killAllFB()
        {
            CheckHelicopter();
            int countfb = 0;
            if (FireBalls.Count <= 0) return countfb;
            foreach (var fb in FireBalls)
            {
                if (fb == null || fb.isDestroyed)
                {
                    FireBalls.Remove(fb);
                    continue;
                }
                var fbn = (BaseNetworkable)fb;
                fbn.Kill(NoDestroy);
                countfb++;
            }
            CheckHelicopter();
           
            return countfb;
        }

        private int killAllGibs()
        {
            CheckHelicopter();
            int countgib = 0;
            if (Gibs.Count <= 0) return countgib;
            foreach (var Gib in Gibs)
            {
                if (Gib == null) continue;
                var GibNetworkable = (BaseNetworkable)Gib;
                GibNetworkable.Kill(NoDestroy);
                countgib++;
            }
            CheckHelicopter();
            return countgib;
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        /*--------------------------------------------------------------//
		//		canExecute - check if the player has permission			//
		//--------------------------------------------------------------*/
        private bool canExecute(BasePlayer player, string perm, bool sendMsg = true)
        {
            var permprefix = "helicontrol." + perm;
            if (permission.UserHasPermission(player.UserIDString, "helicontrol.admin"))
            {
                return true;
            }
            if (!permission.UserHasPermission(player.UserIDString, permprefix))
            {
                if (sendMsg) SendReply(player, GetMessage("noPerms"));
                return false;
            }
            return true;
        }

        /*--------------------------------------------------------------//
		//			  Find a player by name/partial name				//
		//				Thank You Whoever Wrote This					//
		//--------------------------------------------------------------*/

        private BasePlayer FindPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var allPlayers = BasePlayer.activePlayerList.ToArray();
            // Try to find an exact match first
            for(int i = 0; i < allPlayers.Count(); i++)
            {
                var p = allPlayers[i];
                if (p.displayName == name)
                {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }

            }
            if (player != null)
                return player;
            // Otherwise try to find a partial match
            for(int i = 0; i < allPlayers.Count(); i++)
            {
                var p = allPlayers[i];
                if (p.displayName.ToLower().IndexOf(name) >= 0)
                {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            return player;
        }

        private BasePlayer FindPlayerByID(ulong playerid)
        {
            BasePlayer player = null;
            if (BasePlayer.FindByID(playerid)) return BasePlayer.FindByID(playerid);
            if (BasePlayer.FindSleeping(playerid)) return BasePlayer.FindSleeping(playerid);
            var allPlayers = BasePlayer.activePlayerList;
            // Try to find an exact match first
            foreach (var p in allPlayers) if (p.userID == playerid) player = p;
            return player;
        }

        private BasePlayer FindPlayerByID(string playerid)
        {
            if (string.IsNullOrEmpty(playerid)) return null;
            ulong uID = 0;
            if (!ulong.TryParse(playerid, out uID)) return null;
            return FindPlayerByID(uID);
        }


        void RemoveFromWorld(Item item)
        {
            if (item == null) return;
            if (item.parent != null) item.Drop(default(Vector3), default(Vector3));
            BaseEntity worldEntity = item.GetWorldEntity();
            if (worldEntity == null)
                return;
            item.SetWorldEntity((BaseEntity)null);
            item.OnRemovedFromWorld();
            if (item.contents != null)
                item.contents.OnRemovedFromWorld();
            if (!BaseEntityEx.IsValid(worldEntity))
                return;
            worldEntity.Kill(NoDestroy);
        }

        //CheckBoundaries taken from Nogrod's NTeleportation, with permission
        private bool CheckBoundaries(float x, float y, float z)
        {
            return x <= boundary && x >= -boundary && y < 2000 && y >= -100 && z <= boundary && z >= -boundary;
        }

        private float GetLowestCooldown(BasePlayer player)
        {
            var perms = new List<String>();
            var time = 0f;
            var cont = false;
            foreach (var perm in permission.GetUserPermissions(player.UserIDString))
            {
                if (perm.Contains("helicontrol.cooldown"))
                {
                    perms.Add(perm.Replace("helicontrol.", "").Replace("cooldown", "Cooldown")); //temp workaround
                    cont = true;
                }
            }
            if (!cont) return time;
            var nums = new HashSet<float>();
            foreach (var perm in perms)
            {
                var tempTime = 0f;
                if (!cds.ContainsKey(perm))
                {
                    PrintWarning("Cooldowns dictionary does not contain: " + perm);
                    continue;
                }
                if (!float.TryParse(cds[perm].ToString(), out tempTime))
                {
                    PrintWarning("Failed to parse cooldown time! -- report this on plugin thread");
                    continue;
                }
                nums.Add(tempTime);
            }
            if (nums.Count >= 1) time = nums.Min();
            return time;
        }

        private int GetHighestLimit(BasePlayer player)
        {
            var perms = new List<String>();
            var limit = 0;
            var cont = false;
            foreach (var perm in permission.GetUserPermissions(player.UserIDString))
            {
                if (perm.Contains("helicontrol.limit"))
                {
                    perms.Add(perm.Replace("helicontrol.", "").Replace("limit", "Limit")); //temp workaround
                    cont = true;
                }
            }
            if (!cont) return limit;
            var nums = new HashSet<int>();
            foreach (var perm in perms)
            {
                var tempTime = 0;
                if (limits.ContainsKey(perm))
                {
                    if (!int.TryParse(limits[perm].ToString(), out tempTime))
                    {
                        PrintWarning("Failed to parse limits! -- report this on plugin thread");
                        continue;
                    }
                }
                nums.Add(tempTime);
            }
            if (nums.Count >= 1) limit = nums.Max();
            return limit;
        }

        private bool ignoreCooldown(BasePlayer player)
        {
            return (permission.UserHasPermission(player.UserIDString, "helicontrol.ignorecooldown"));
        }
        
        private bool ignoreLimits(BasePlayer player)
        {
            return (permission.UserHasPermission(player.UserIDString, "helicontrol.ignorelimits"));
        }

        #endregion
        #region Classes
        /*----------------------------------------------------------------------------------------------------------------------------//
        //												STORED DATA CLASSES															  //
        //----------------------------------------------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------//
		//	StoredData class - holds a list of BoxInventories			//
		//--------------------------------------------------------------*/
        class StoredData
        {
            public List<BoxInventory> HeliInventoryLists = new List<BoxInventory>();

            public StoredData()
            {
            }
        }

        class StoredData2
        {
            public Dictionary<string, float> WeaponList = new Dictionary<string, float>();

            public StoredData2()
            {
            }
        }

        class StoredData3
        {
            public Dictionary<ulong, float> cooldowns = new Dictionary<ulong, float>();
            public Dictionary<ulong, int> timesCalled = new Dictionary<ulong, int>();
            public Dictionary<ulong, string> lastCalled = new Dictionary<ulong, string>();
            public StoredData3()
            {
            }
        }


        /*--------------------------------------------------------------//
		//	BoxInventory class - represents heli_crate inventory		//
		//--------------------------------------------------------------*/
        class BoxInventory
        {
            public List<ItemDef> lootBoxContents = new List<ItemDef>();

            public BoxInventory() { }

            public BoxInventory(List<ItemDef> list)
            {
                lootBoxContents = list;
            }

            public BoxInventory(List<Item> list)
            {
                for(int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item == null) continue;
                    lootBoxContents.Add(new ItemDef(item.info.shortname, item.amount, item.skin));
                }
            }

            public BoxInventory(string name, int amount, int skinID = 0)
            {
                lootBoxContents.Add(new ItemDef(name, amount, skinID));
            }

            public int InventorySize()
            {
                return lootBoxContents.Count;
            }

            public List<ItemDef> GetlootBoxContents()
            {
                return lootBoxContents;
            }
        }
        /*--------------------------------------------------------------//
		//			ItemDef class - represents an item					//
		//--------------------------------------------------------------*/
        class ItemDef
        {
            public string name;
            public int amount;
            public int skinID;

            public ItemDef() { }

            public ItemDef(string name, int amount, int skinID = 0)
            {
                this.name = name;
                this.amount = amount;
                this.skinID = skinID;
            }
        }
        #endregion
        #region Data
        /*--------------------------------------------------------------//
		//			LoadSaveData - loads up the loot data				//
		//--------------------------------------------------------------*/
        void LoadSavedData()
        {
            storedData = Interface.GetMod()?.DataFileSystem?.ReadObject<StoredData>("HeliControlData") ?? null;
            var count = storedData?.HeliInventoryLists?.Count ?? 0;
            //Create a default data file if there was none:
            if (storedData == null || storedData.HeliInventoryLists == null || count <= 0)
            {
                Puts("No Lootdrop Data found,  creating new file...");
                storedData = new StoredData();
                BoxInventory inv;
                inv = new BoxInventory("rifle.ak", 1);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("rifle.bolt", 1);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("explosive.timed", 3);
                inv.lootBoxContents.Add(new ItemDef("ammo.rocket.hv", 3));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("lmg.m249", 1);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle", 100));
                storedData.HeliInventoryLists.Add(inv);

                SaveData();
            }


        }

        void LoadWeaponData()
        {
            storedData2 = Interface.GetMod()?.DataFileSystem?.ReadObject<StoredData2>("HeliControlWeapons") ?? null;
            var count = storedData2?.WeaponList?.Count ?? 0;
            if (storedData2 == null || storedData2.WeaponList == null || count <= 0)
            {
                Puts("No weapons data found, creating new file...");
                storedData2 = new StoredData2();
                List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions();
                for(int i = 0; i < ItemsDefinition.Count(); i++)
                {
                    var itemdef = ItemsDefinition[i];
                    if (itemdef == null) continue;
                    var weapon = ItemManager.CreateByItemID(itemdef.itemid, 1)?.GetHeldEntity()?.GetComponent<BaseProjectile>() ?? null;
                    if (weapon == null) continue;
                    var category = itemdef?.category.ToString() ?? string.Empty;
                    var primaryMag = weapon?.primaryMagazine ?? null;
                    var shortname = itemdef?.shortname ?? string.Empty;
                    var englishName = itemdef?.displayName?.english ?? string.Empty;
                    if (primaryMag == null || string.IsNullOrEmpty(shortname) || string.IsNullOrEmpty(englishName)) continue;
                    if (primaryMag.capacity < 1) continue;
                    if (category == "Weapon" && shortname != "rocket.launcher") storedData2.WeaponList.Add(englishName, 1f);
                }
                SaveData2();
            }
        }


        /*--------------------------------------------------------------//
        //			  SaveData - used for loot and weapons			    //
        //--------------------------------------------------------------*/
        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlData", storedData);
        void SaveData2() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlWeapons", storedData2);
        void SaveData3() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlCooldowns", storedData3);
    }
    #endregion
}