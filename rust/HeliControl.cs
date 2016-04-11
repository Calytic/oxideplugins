using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeliControl", "Shady", "1.0.20", ResourceId = 1348)]
    [Description("Tweak various settings of helicopters. Plugin originally developed by koenrad.")]
    class HeliControl : RustPlugin
    {
        private Dictionary<string, string> englishnameToShortname = new Dictionary<string, string>();       //for finding shortnames
        StoredData storedData = new StoredData();
        StoredData2 storedData2 = new StoredData2();
        private int last;
        PatrolHelicopterAI HeliInstance = null;
        private List<BaseHelicopter> BaseHelicopters = new List<BaseHelicopter>();
        private List<PatrolHelicopterAI> AIHelis = new List<PatrolHelicopterAI>();
        private List<HelicopterTurret> heliTurrets = new List<HelicopterTurret>();
        private List<HelicopterDebris> Gibs = new List<HelicopterDebris>();
        private List<FireBall> FireBalls = new List<FireBall>();
        bool configWasChanged = false;


        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["DisableHeli"] = false;
            Config["ModifyDamageToHeli"] = false;
            Config["UseGlobalDamageModifier"] = false;
            Config["UseCustomLoot"] = false;
            Config["GlobalDamageMultiplier"] = 1.0;
            Config["HeliBulletDamageAmount"] = 20.0;
            Config["MainRotorHealth"] = 750.0;
            Config["TailRotorHealth"] = 375.0;
            Config["BaseHealth"] = 10000.0;
            Config["MaxLootCratesToDrop"] = 4;
            Config["HeliSpeed"] = 25.0;
            Config["HeliAccuracy"] = 2.0;
            Config["MaxHeliRockets"] = 12;
            Config["DisableGibs"] = false;
            Config["DisableNapalm"] = false;
            Config["BulletSpeed"] = 250.0;
            Config["LockedCrates"] = true;
            Config["TimeBeforeUnlockingCrates"] = 0.0f;
            Config["LifeTimeMinutes"] = 15;
            Config["TurretFireRate"] = 0.125f;
            Config["TurretBurstLength"] = 3f;
            Config["TurretTimeBetweenBursts"] = 3f;
            Config["TurretMaxTargetRange"] = 300f;
            Config["GibsTooHotLength"] = 480f;
            SaveConfig();
        }
        /*--------------------------------------------------------------//
		//						Initial Setup							//
		//--------------------------------------------------------------*/
        void CheckConfigEntry<T>(string key, T value)
        {
            if (Config[key] == null)
            {
                Config[key] = value;
                configWasChanged = true;
            }
        }
        void Init()
        {
            //Backwards compatibility for config files
            //Allows checking each config value on startup.
                  CheckConfigEntry("DisableHeli", false);
                  CheckConfigEntry("ModifyDamageToHeli", false);
                  CheckConfigEntry("UseGlobalDamageModifier", false);
                  CheckConfigEntry("UseCustomLoot", false);
                  CheckConfigEntry("GlobalDamageMultiplier", 1.0f);
                  CheckConfigEntry("HeliBulletDamageAmount", 20.0f);
                  CheckConfigEntry("MainRotorHealth", 750.0f);
                  CheckConfigEntry("TailRotorHealth", 375.0f);
                  CheckConfigEntry("BaseHealth", 10000.0f);
                  CheckConfigEntry("HeliSpeed", 25.0f);
                  CheckConfigEntry("HeliAccuracy", 2.0f);
                  CheckConfigEntry("MaxLootCratesToDrop", 4);
                  CheckConfigEntry("MaxHeliRockets", 12);
                  CheckConfigEntry("TimeBetweenRockets", 0.2f);
                  CheckConfigEntry("DisableGibs", false);
                  CheckConfigEntry("DisableNapalm", false);
                  CheckConfigEntry("BulletSpeed", 250.0f);
                  CheckConfigEntry("LockedCrates", true);
                  CheckConfigEntry("LifeTimeMinutes", 15);
                  CheckConfigEntry("TimeBeforeUnlockingCrates", 0.0f);
                  CheckConfigEntry("TurretFireRate", 0.125f);
                  CheckConfigEntry("TurretBurstLength", 3f);
                  CheckConfigEntry("TurretTimeBetweenBursts", 3f);
                  CheckConfigEntry("TurretMaxTargetRange", 300f);
                  CheckConfigEntry("GibsTooHotLength", 480f);
            if(configWasChanged) SaveConfig();

            permission.RegisterPermission("helicontrol.callheli", this);
            permission.RegisterPermission("helicontrol.killheli", this);
            permission.RegisterPermission("helicontrol.shortname", this);
            permission.RegisterPermission("helicontrol.strafe", this);
            permission.RegisterPermission("helicontrol.update", this);
            permission.RegisterPermission("helicontrol.destination", this);
            permission.RegisterPermission("helicontrol.killfireballs", this);
            permission.RegisterPermission("helicontrol.killgibs", this);
            permission.RegisterPermission("helicontrol.admin", this);


            //Set heli accuracy & adjust life time
            var bulletAccuracy = ConVar.PatrolHelicopter.bulletAccuracy;
            var lifetime = ConVar.PatrolHelicopter.lifetimeMinutes;
            TryParseFloat(Config["HeliAccuracy"].ToString(), ref bulletAccuracy);
            TryParseFloat(Config["LifeTimeMinutes"].ToString(), ref lifetime);
            ConVar.PatrolHelicopter.bulletAccuracy = bulletAccuracy;
            ConVar.PatrolHelicopter.lifetimeMinutes = lifetime;
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
            englishnameToShortname = new Dictionary<string, string>();
            var ItemsDefinition = ItemManager.GetItemDefinitions();
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                englishnameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
            timer.Every(5f, () => CheckHelicopter());


            LoadDefaultMessages();
            //Get the saved drop list
            if ((bool)Config["UseCustomLoot"]) LoadSavedData();
            LoadWeaponData();
            var allHelicopters = GameObject.FindObjectsOfType<BaseHelicopter>();

            foreach(BaseHelicopter baseHeli in allHelicopters)
            {
                //   if (!baseHeli.IsActive()) return;
                if (!baseHeli.IsAlive()) return;
                var patrolAI = baseHeli.GetComponent<PatrolHelicopterAI>();
                var turretLeft = patrolAI.leftGun;
                var turretRight = patrolAI.rightGun;
                BaseHelicopters.Add(baseHeli);
                AIHelis.Add(patrolAI);
                heliTurrets.Add(turretLeft);
                heliTurrets.Add(turretRight);
                HeliInstance = patrolAI;
            }
        }



        /*--------------------------------------------------------------//
		//					OnEntitySpawned Hook						//
		//--------------------------------------------------------------*/


    


        void OnEntitySpawned(BaseNetworkable entity)
        {

            if (entity == null) return;
            string heliname = entity.LookupShortPrefabName();
            if (heliname.Contains("napalm"))
                {
                if (entity.GetType().ToString() != "FireBall") return;
                if (((bool)Config["DisableNapalm"])) entity.KillMessage();
                if (!entity.isDestroyed) FireBalls.Add((FireBall)entity);
            }


            if (heliname.Contains("oilfireball"))
            {
                if ((bool)Config["LockedCrates"] == false && float.Parse(Config["TimeBeforeUnlockingCrates"].ToString()) <= 0) entity.KillMessage();
                if ((bool)Config["LockedCrates"] == false && float.Parse(Config["TimeBeforeUnlockingCrates"].ToString()) >= 1) timer.Once(float.Parse(Config["TimeBeforeUnlockingCrates"].ToString()), () => DoTimerThings(entity));
                if (!entity.isDestroyed) FireBalls.Add((FireBall)entity);
            }


                if (heliname == "heli_crate.prefab")
            {
                //check for config setting, and makes sure there is loot data before changing heli loot

                if ((bool)Config["UseCustomLoot"] && storedData.HeliInventoryLists != null && storedData.HeliInventoryLists.Count > 0)
                {
                    LootContainer heli_crate = (LootContainer)entity;
                    int index;
                    System.Random random = new System.Random();
                    do
                    {
                        index = random.Next(storedData.HeliInventoryLists.Count);
                    } while (index == last && storedData.HeliInventoryLists.Count > 1);
                    last = index;
                    BoxInventory inv = storedData.HeliInventoryLists[index];
                    heli_crate.inventory.itemList.Clear();
                    foreach (ItemDef itemDef in inv.lootBoxContents)
                    {
                        Item item = null;
                        if (!itemDef.isBP)
                            item = ItemManager.CreateByName(itemDef.name, itemDef.amount);
                        else
                            item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(itemDef.name).itemid, 1, true, 0);
                        item.MoveToContainer(heli_crate.inventory, -1, false);
                    }
                    heli_crate.inventory.MarkDirty();
                }

            }

            if (heliname.Contains("servergibs_patrolhelicopter.prefab"))
            {
                var disableGibs = (bool)Config["DisableGibs"];
                if (disableGibs)
                {
                    entity.KillMessage();
                    return;
                }
                Gibs.Add((HelicopterDebris)entity);
                var GibsTooHotLength = 480f;
                TryParseFloat(Config["GibsTooHotLength"].ToString(), ref GibsTooHotLength);
                HelicopterDebris debris = (HelicopterDebris)entity;
                FieldInfo tooHotUntil = typeof(HelicopterDebris).GetField("tooHotUntil", (BindingFlags.Instance | BindingFlags.NonPublic));
                tooHotUntil.SetValue(debris, Time.realtimeSinceStartup + GibsTooHotLength);
            }

            if (heliname == "patrolhelicopter.prefab")
            {
                // Disable Helicopters
                if ((bool)Config["DisableHeli"])
                {
                    entity.KillMessage();
                    Puts(GetMessage("heliAutoDestroyed"));
                    return;
                }
                HeliInstance = entity.GetComponent<PatrolHelicopterAI>();
                var AIHeli = entity.GetComponent<PatrolHelicopterAI>();
                BaseHelicopters.Add((BaseHelicopter)entity);
                AIHelis.Add(AIHeli);
                heliTurrets.Add(AIHeli.leftGun);
                heliTurrets.Add(AIHeli.rightGun);
                UpdateHelis(entity, true);
                getAllTurrets();
            }
           



        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return;
            var name = entity.LookupShortPrefabName();
            var networkable = (BaseNetworkable)entity;
            if (name == "patrolhelicopter.prefab")
            {
                var AIHeli = entity.GetComponent<PatrolHelicopterAI>();
                BaseHelicopters.Remove((BaseHelicopter)entity);
                AIHelis.Remove(AIHeli);
                heliTurrets.Remove(AIHeli.leftGun);
                heliTurrets.Remove(AIHeli.rightGun);
            }
            if (name.Contains("oilfireball") || name.Contains("napalm"))
            {
               FireBalls.Remove((FireBall)networkable);
            }
            if(name.Contains("servergibs_patrolhelicopter.prefab"))
            {
                Gibs.Remove((HelicopterDebris)networkable);
            }
        }



        private void UpdateHelis(BaseNetworkable entity, bool justCreated)
        {
            
            var heli = (BaseHelicopter)entity;
            if (heli == null) return;
            var mainhealth = 10000f; var heliBulletDmg = 20f; var HeliSpeed = 25f; var BulletSpeed = 250f; var MainRotorHealth = 750f; var TailRotorHealth = 350f; var TimeBetweenRockets = 0.2f; var MaxHeliRockets = 12;
            TryParseFloat(Config["BaseHealth"].ToString(), ref mainhealth); TryParseFloat(Config["HeliBulletDamageAmount"].ToString(), ref heliBulletDmg); TryParseFloat(Config["HeliSpeed"].ToString(), ref HeliSpeed); TryParseFloat(Config["BulletSpeed"].ToString(), ref BulletSpeed);
            TryParseFloat(Config["MainRotorHealth"].ToString(), ref MainRotorHealth); TryParseFloat(Config["TailRotorHealth"].ToString(), ref TailRotorHealth); TryParseFloat(Config["TimeBetweenRockets"].ToString(), ref TimeBetweenRockets); TryParseInt(Config["MaxHeliRockets"].ToString(), ref MaxHeliRockets);
            heli.startHealth = mainhealth;
          if(justCreated)  heli.InitializeHealth(mainhealth, mainhealth);
            heli.maxCratesToSpawn = (int)Config["MaxLootCratesToDrop"];
            heli.bulletDamage = heliBulletDmg;
            var heliAI = entity.GetComponent<PatrolHelicopterAI>();
            heliAI.maxSpeed = HeliSpeed;
            heli.bulletSpeed = BulletSpeed;
            var weakspots = heli.weakspots;
            if (justCreated)
            {
                weakspots[0].health = MainRotorHealth;
                weakspots[1].health = TailRotorHealth;
            }
            weakspots[0].maxHealth = MainRotorHealth;
            weakspots[1].maxHealth = TailRotorHealth;
            typeof(PatrolHelicopterAI).GetField("timeBetweenRockets", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity.GetComponent<PatrolHelicopterAI>(), TimeBetweenRockets);
            typeof(PatrolHelicopterAI).GetField("maxRockets", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity.GetComponent<PatrolHelicopterAI>(), MaxHeliRockets);
            typeof(PatrolHelicopterAI).GetField("numRocketsLeft", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity.GetComponent<PatrolHelicopterAI>(), MaxHeliRockets);
            heli.SendNetworkUpdateImmediate(justCreated);
        }

        public bool TryParseFloat(string text, ref float value)
        {
            float tmp;
            if (float.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }

        public bool TryParseInt(string text, ref int value)
        {
            int tmp;
            if (int.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }

        private void DoTimerThings(BaseNetworkable entity)
        {
            if (entity.isDestroyed) return;
                //Entity is already destroyed, calling kill on it when it is gone will cause warnings in console, so lets not do that
                
            entity.KillMessage();
        }

        /*--------------------------------------------------------------//
		//						OnPlayerAttack Hook						//
		//--------------------------------------------------------------*/
        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {

            if (attacker == null || hitInfo == null || hitInfo.HitEntity == null) return;

            if (hitInfo.HitEntity.LookupShortPrefabName() == "patrolhelicopter.prefab")         //We hit a helicopter
            {
                if (!(bool)Config["ModifyDamageToHeli"]) return;    //Check if damage modification is on
                var dmgMod = 0f;
                    float.TryParse(Config["GlobalDamageMultiplier"].ToString(), out dmgMod);
                if ((bool)Config["UseGlobalDamageModifier"] && dmgMod != 0f)        //Check for global modifier
                {
                    hitInfo.damageTypes.ScaleAll(dmgMod);
                    return;
                }

                var shortName = hitInfo.Weapon?.GetItem()?.info?.shortname ?? null;    //weapon's shortname
                var displayName = hitInfo.Weapon?.GetItem()?.info?.displayName?.english ?? null;
               float weaponConfig = 0.0f;
                if (shortName == null) return;
                storedData2.WeaponList.TryGetValue(shortName, out weaponConfig);
                if (weaponConfig == 0.0f) storedData2.WeaponList.TryGetValue(displayName, out weaponConfig);
                if (weaponConfig != 0.0f && weaponConfig != 1.0f)
                {
                    hitInfo.damageTypes.ScaleAll(weaponConfig);
                }
                else
                {
                    if (float.Parse(Config["GlobalDamageMultiplier"].ToString()) != 1.0 && (bool)Config["UseGlobalDamageModifier"]) hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageMultiplier"].ToString()));
                }
            }
        }

        /*----------------------------------------------------------------------------------------------------------------------------//
        //												CHAT COMMANDS																  //
        //----------------------------------------------------------------------------------------------------------------------------*/
        /*--------------------------------------------------------------//
		//					Chat Command for callheli					//
		//--------------------------------------------------------------*/

            //"callheliCmd" is essentially pointless I have no idea what I was thinking when I made this
            void callheliCmd(int amountToCall, BasePlayer target = null)
        {
            if (amountToCall <= 1 && target == null)
            {
                call();
                return;
            }
            if (target != null) callOther(target, amountToCall);
        }

        [ChatCommand("callheli")]
        private void cmdCallToPlayer(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "callheli")) return;
            if (args.Length == 0)
            {
                callheliCmd(1, null);
                SendReply(player, GetMessage("heliCalled"));
                return;
            }
            BasePlayer target = null;
            
            ulong ID = 0;
         
            if (args.Length >= 1)
            {
                target = FindPlayerByPartialName(args[0]);
                if (ulong.TryParse(args[0], out ID))
                {
                    target = FindPlayerByID(ID);
                }
                if (target == null)
                {
                    SendReply(player, string.Format(GetMessage("playerNotFound"), args[0]));
                    return;
                }
            }

            int num = 1;
            if (args.Length == 2)
            {
                bool result = Int32.TryParse(args[1], out num);
                if (!result) num = 1;

            }

            callheliCmd(num, target);
            SendReply(player, string.Format(GetMessage("helisCalledPlayer"), num, target.displayName));
        }

        /*--------------------------------------------------------------//
		//					Chat Command for killheli					//
		//--------------------------------------------------------------*/
        [ChatCommand("killheli")]
        private void cmdKillHeli(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killheli")) return;
            int numKilled = 0;
            if (args.Length == 0)
            {
                numKilled = killAll(false);
            }
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
            foreach(BaseHelicopter helicopter in BaseHelicopters)
            {
                UpdateHelis(helicopter, false);
                count += 1;
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
                    if (ulong.TryParse(args[0], out ID))
                    {
                        target = FindPlayerByID(ID);
                    }
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
                    if (ulong.TryParse(args[0], out ID))
                    {
                        target = FindPlayerByID(ID);
                    }
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

        [ChatCommand("killfireballs")]
        private void cmdKillFB(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killfireballs")) return;
            int numKilledFB = killAllFB();
            SendReply(player, string.Format(GetMessage("entityDestroyed"), numKilledFB.ToString(), "fireball"));
        }



        /*--------------------------------------------------------------//
		//					Chat Command for killgibs					//
		//--------------------------------------------------------------*/
        [ChatCommand("killgibs")]
        private void cmdKillGibs(BasePlayer player, string command, string[] args)
        {
            if (!canExecute(player, "killgibs")) return;
            int numKilledGibs = killAllGibs();
            SendReply(player, string.Format(GetMessage("entityDestroyed"), numKilledGibs.ToString(), "helicopter gib"));
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

            if (englishnameToShortname.ContainsKey(engName)) SendReply(player, engName +  "is" + "\"" + englishnameToShortname[engName] + "\"");
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

            if (arg.connection != null)
            {
                if (!canExecute(arg.connection.player as BasePlayer, "callheli")) return;
            }

            if (arg.Args == null || arg?.Args?.Length <= 0)
            {
                callheliCmd(1, null);
                if (arg.connection != null) SendReply(arg, GetMessage("heliCalled"));
                Puts(GetMessage("heliCalled"));
                return;
            }

            BasePlayer target = null;
            target = FindPlayerByPartialName(arg.Args[0]);
            ulong ID = 0;

                if (ulong.TryParse(arg.Args[0], out ID))
                {
                    target = FindPlayerByID(ID);
                }
                if (target == null)
                {
                    SendReply(arg, string.Format(GetMessage("playerNotFound"), arg.Args[0]));
                    return;
                }

            int num = 1;
            if (arg.Args.Length == 2)
            {
                bool result = Int32.TryParse(arg.Args[1], out num);
                if (!result) num = 1;
            }

            callheliCmd(num, target);
            if (arg.connection != null) SendReply(arg, string.Format(GetMessage("helisCalledPlayer"), num, target.displayName));
            Puts(string.Format(GetMessage("helisCalledPlayer"), num, target.displayName));
        }






        /*--------------------------------------------------------------//
		//				Console Command for getshortname				//
		//--------------------------------------------------------------*/
        [ConsoleCommand("getshortname")]
        private void consoleGetShortName(ConsoleSystem.Arg arg)
        {

            if (arg?.connection != null && arg.connection?.player != null)
            {
                if (!canExecute(arg.connection.player as BasePlayer, "shortname")) return;
            }
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

            if (englishnameToShortname.ContainsKey(engName))
            {
                if (arg.connection != null) SendReply(arg, "\"" + engName + "\" is \"" + englishnameToShortname[engName] + "\"");
                Puts("\"" + engName + "\"" + " is " + "\"" + englishnameToShortname[engName] + "\"");
            }
            else
            {
                if (arg.connection != null) SendReply(arg, GetMessage("itemNotFound"));
                Puts(GetMessage("itemNotFound"));
            }
        }


        public void CheckHelicopter()
        {
            BaseHelicopters.RemoveAll(p => !p);
            AIHelis.RemoveAll(p => !p);
            heliTurrets.RemoveAll(p => !p);
            Gibs.RemoveAll(p => !p);
            FireBalls.RemoveAll(p => !p);
        }

        /*--------------------------------------------------------------//
		//				Console Command for killheli					//
		//--------------------------------------------------------------*/
        [ConsoleCommand("killheli")]
        private void consoleKillHeli(ConsoleSystem.Arg arg)
        {

            if (arg.connection != null)
            {
                if (!canExecute(arg.connection.player as BasePlayer, "killheli")) return;
            }

            int numKilled = killAll(false);
            if (arg.connection != null) SendReply(arg, string.Format(GetMessage("entityDestroyed"), numKilled.ToString(), "helicopter"));

            Puts(string.Format(GetMessage("entityDestroyed"), numKilled.ToString(), "helicopter"));
        }



        /*----------------------------------------------------------------------------------------------------------------------------//
        //													CORE FUNCTIONS															  //
        //----------------------------------------------------------------------------------------------------------------------------*/

        /*--------------------------------------------------------------//
		//				killAll - produces no loot drops					//
		//--------------------------------------------------------------*/
        private int killAll(bool isForced)
        {
            CheckHelicopter();
            int count = 0;
            var amount = BaseHelicopters.Count;
            if (BaseHelicopters.Count <= 0) return count;
            var helicopters = new List<BaseHelicopter>(BaseHelicopters);
            foreach (BaseHelicopter helicopter in helicopters)
            {
                helicopter.maxCratesToSpawn = 0;        //comment this line if you want loot drops with killheli
                if (isForced == true) helicopter.KillMessage();
                else helicopter.DieInstantly();

                BaseHelicopters.Remove(helicopter);
                AIHelis.Remove(helicopter.GetComponent<PatrolHelicopterAI>());
                count++;
            }
            return count;
        }

        private void getAllTurrets()
        {
            CheckHelicopter();
            var turretFireRate = 0.125f;
            var timeBetweenBursts = 3f;
            var burstLength = 3f;
            var maxTargetRange = 300f;
            TryParseFloat(Config["TurretFireRate"].ToString(), ref turretFireRate);
            TryParseFloat(Config["TurretTimeBetweenBursts"].ToString(), ref timeBetweenBursts);
            TryParseFloat(Config["TurretBurstLength"].ToString(), ref burstLength);
            TryParseFloat(Config["TurretMaxTargetRange"].ToString(), ref maxTargetRange);
            if (heliTurrets.Count <= 0) return;
            foreach (HelicopterTurret heliturret in heliTurrets)
            {
                heliturret.fireRate = turretFireRate;
                heliturret.timeBetweenBursts = timeBetweenBursts;
                heliturret.burstLength = burstLength;
                heliturret.maxTargetRange = maxTargetRange;
            }
        }

        private int countAllHeli()
        {
            CheckHelicopter();
            int count = 0;
            count = BaseHelicopters.Count;
            return count;
        }


        private int killAllFB()
        {
            CheckHelicopter();
            int countfb = 0;
            if (FireBalls.Count <= 0) return countfb;
            var fbs = new List<FireBall>(FireBalls);
            foreach (FireBall fb in fbs)
            {
                var fbn = (BaseNetworkable)fb;
                fbn.KillMessage();
                FireBalls.Remove(fb);
                countfb++;
            }
            return countfb;
        }

        private int killAllGibs()
        {
            CheckHelicopter();
            int countgib = 0;
            if (Gibs.Count <= 0) return countgib;
            var debris = new List<HelicopterDebris>(Gibs);
            foreach (HelicopterDebris Gib in debris)
            {
                var GibNetworkable = (BaseNetworkable)Gib;
                GibNetworkable.KillMessage();
                Gibs.Remove(Gib);
                countgib++;
            }
            return countgib;
        }


        /*--------------------------------------------------------------//
		//			callOther - call heli on other person				//
		//--------------------------------------------------------------*/
        private void callOther(BasePlayer target, int num)
        {
            int i = 0;
            while (i < num)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (!entity) return;
                PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
                entity.GetComponent<PatrolHelicopterAI>().SetInitialDestination(target.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                entity.Spawn(true);
                i++;
            }
        }

        /*--------------------------------------------------------------//
		//					call - call heli in general					//
		//--------------------------------------------------------------*/
        private void call(int num = 1)
        {
            int i = 0;
            while (i < num)
            {
                var entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (!entity)
                    return;
                entity.Spawn(true);
                i++;
            }
        }

        /*--------------------------------------------------------------//
		//		canExecute - check if the player has permission			//
		//--------------------------------------------------------------*/
        private bool canExecute(BasePlayer player, string perm)
        {
            var permprefix = "helicontrol." + perm;         
            if (permission.UserHasPermission(player.userID.ToString(), "helicontrol.admin")) return true;
            if (!permission.UserHasPermission(player.userID.ToString(), permprefix))
            {
                SendReply(player, GetMessage("noPerms"));
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
            foreach (var p in allPlayers)
            {
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
            foreach (var p in allPlayers)
            {
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
            if (string.IsNullOrEmpty(playerid.ToString())) return null;
            BasePlayer player = null;
            if (BasePlayer.FindByID(playerid))
            {
                player = BasePlayer.FindByID(playerid);
                return player;
            }
            var allPlayers = BasePlayer.activePlayerList.ToArray();
            // Try to find an exact match first
            foreach (var p in allPlayers)
            {
                if (p.userID == playerid)
                {
                    player = p;
                }
            }
            return player;
        }

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
                foreach (var item in list)
                {
                    lootBoxContents.Add(new ItemDef(item.info.shortname, item.amount, item.IsBlueprint()));
                }
            }

            public BoxInventory(string name, int amount, bool isBP)
            {
                lootBoxContents.Add(new ItemDef(name, amount, isBP));
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
            public bool isBP;

            public ItemDef() { }

            public ItemDef(string name, int amount, bool isBP)
            {
                this.name = name;
                this.amount = amount;
                this.isBP = isBP;
            }
        }

        /*--------------------------------------------------------------//
		//			LoadSaveData - loads up the loot data				//
		//--------------------------------------------------------------*/
        void LoadSavedData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("HeliControlData");
            //Create a default data file if there was none:
            if (storedData.HeliInventoryLists.Count == 0)
            {
                Puts("No Lootdrop Data found,  creating new file...");
                storedData = new StoredData();
                BoxInventory inv;
                inv = new BoxInventory("rifle.ak", 1, false);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128, false));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("rifle.bolt", 1, false);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle.hv", 128, false));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("explosive.timed", 3, false);
                inv.lootBoxContents.Add(new ItemDef("ammo.rocket.hv", 3, false));
                storedData.HeliInventoryLists.Add(inv);

                inv = new BoxInventory("lmg.m249", 1, false);
                inv.lootBoxContents.Add(new ItemDef("ammo.rifle", 100, false));
                storedData.HeliInventoryLists.Add(inv);

                SaveData();
            }


        }

        void LoadWeaponData()
        {
            storedData2 = Interface.GetMod().DataFileSystem.ReadObject<StoredData2>("HeliControlWeapons");
            if (storedData2.WeaponList.Count == 0)
            {
                Puts("No weapons data found, creating new file...");
                storedData2 = new StoredData2();
                List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions();
                foreach (ItemDefinition itemdef in ItemsDefinition)
                {
                    var item = ItemManager.CreateByItemID(itemdef.itemid, 1, false);
                    if (item == null) continue;
                    BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon == null) continue;
                    if (itemdef.category.ToString() == "Weapon" && weapon != null && weapon.primaryMagazine != null && weapon.primaryMagazine.capacity >= 1 && itemdef.shortname != "rocket.launcher")
                    {
                        storedData2.WeaponList.Add(itemdef.displayName.english, 1f);
                    }
                }
                SaveData2();
            }
        }
        /*--------------------------------------------------------------//
        //			  SaveData - used for loot and weapons			    //
        //--------------------------------------------------------------*/
        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlData", storedData);
        void SaveData2() => Interface.GetMod().DataFileSystem.WriteObject("HeliControlWeapons", storedData2);
    }
}