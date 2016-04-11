using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Rust;

namespace Oxide.Plugins
{
    [Info("FortWars", "Naleen", "0.3.3", ResourceId = 1618)]
    class FortWars : RustPlugin
    {

        // FW Values
        private bool BuildPhase;
        private bool StartedGame;
        private bool FWEnabled;
        private int TimeBuild = 1200;
        private int TimeFight = 2400;
        private int TimeHeli = 600;
        private int TimeDropBuild = 300;
        private int TimeDropFight = 300;
        private int CraftBuild = 10;
        private int CraftFight = 600;
        private int HeliSpeed = 110;
        private int HeliHP = 200;
        private int HeliHPRudder = 30;
        private int BuildGatherMulti = 900;
        private int FightGatherMulti = 12;
        private int DropBuild = 0;
        private int DropFight = 1;


        public string PhaseStr { get; private set; }


        ////////////////////////////////////////////////////////////
        // Messages ////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        Dictionary<string, string> LangMessages = new Dictionary<string, string>()
        {
            {"NotEnabled", "Fort Wars is disabled." },
            {"NoConfig", "Creating a new config file." },
            {"Title", "<color=orange>Fort Wars</color> : "},
            {"NoPerms", "You are not authorized to use this command."},
            {"BuildPhase", "Build Phase."},
            {"BuildPhaseTime", "{0} minutes of Build Phase remaining."},
            {"FightPhase", "Fight Phase."},
            {"FightPhaseTime", "{0} minutes of Fight Phase remaining."},
            {"HeliBuild", "It's Build phase, give them a chance."},
            {"HeliSpawn", "Spawning {0} helicopters."},
            {"LowBuildRate", "Build Rates are Lowered."},
            {"MoreHelicopters", "Helicopter spawns increased."},
            {"LowGatherRate", "Gathering rate lowered."},
            {"HiGatherRate", "Gathering rate Increased."},
            {"DropSpawn", "Spawning {0} Cargo Planes."},
            {"MoreCargoDrop", "Cargo Plane spawns increased."}
        };



        //Loot
        private static readonly Dictionary<string, object> DefaultGatherResourceModifiers = new Dictionary<string, object>();
        public Dictionary<string, float> GatherResourceModifiers { get; private set; }

        //Crafting
        public float CraftingRate { get; private set; }

        List<ItemBlueprint> blueprintDefinitions = new List<ItemBlueprint>();

        private static readonly Dictionary<string, object> DefaultIndividualRates = new Dictionary<string, object>();

        public Dictionary<string, float> Blueprints { get; } = new Dictionary<string, float>();
        public Dictionary<string, float> IndividualRates { get; private set; }

        List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();

        public List<string> Items { get; } = new List<string>();

        //Helicopter

        //Timers
        public List<Oxide.Plugins.Timer> AutoTimers = new List<Oxide.Plugins.Timer>();
        DateTime PhaseStart;

        //Resource Gather
        public int GatherRate { get; private set; }
        public float GatherPC = 100;

        //Cargo
        public int MinX { get; set; }
        public int MaxX { get; set; }

        public int MinY { get; set; }
        public int MaxY { get; set; }

        public int MinZ { get; set; }
        public int MaxZ { get; set; }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks /////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        void Loaded()
        {
            LoadDefaultConfig();
            lang.RegisterMessages(LangMessages, this);
            LoadPermissions();
        }
        private void LoadDefaultConfig() {
            //Config.Clear();
            LoadConfigVariables();
            SaveConfig();
        }
        void OnServerInitialized()
        {
            int iWorldHalfSize = Convert.ToInt32(World.Size / 2);
            MinX = -iWorldHalfSize + 300;
            MaxX = iWorldHalfSize - 300;
            MinZ = -iWorldHalfSize + 300;
            MaxZ = iWorldHalfSize - 300;
            MinY = 250;
            MaxY = 400;
            //Puts(" X:" + MinX + " " + MaxX + " Y:" + MinY + " " + MaxY + " Z:" + MinZ + " " + MaxZ);
            blueprintDefinitions = ItemManager.bpList;
            foreach (var bp in blueprintDefinitions)
                Blueprints.Add(bp.targetItem.shortname, bp.time);

            itemDefinitions = ItemManager.itemList;
            Puts(itemDefinitions.Count.ToString());
            foreach (var itemdef in itemDefinitions)
                Items.Add(itemdef.displayName.english);

            CraftingRate = 100;
            GatherRate = 100;
            FWEnabled = true;
            UpdateCraftingRate();
            LoadConfigVariables();
            SaveConfig();
            StartBuildPhase();
        }
        void LoadPermissions()
        {
            permission.RegisterPermission("FortWars.UseAll", this);
            permission.RegisterPermission("FortWars.UseHeli", this);
            permission.RegisterPermission("FortWars.UseFight", this);
            permission.RegisterPermission("FortWars.UseBuild", this);
            permission.RegisterPermission("FortWars.UseEnable", this); 
            permission.RegisterPermission("FortWars.UseDrop", this);
        }
        void Unload()
        {
            DestroyTimers();
            foreach (var bp in blueprintDefinitions)
                bp.time = Blueprints[bp.targetItem.shortname];
            CraftingRate = 100f;
            GatherRate = 100;
            UpdateCraftingRate();
        }

        private void StartBuildPhase()
        {
            DestroyTimers();
            BuildPhase = true;

            BroadcastToChat(lang.GetMessage("Title", this) +
                        lang.GetMessage("BuildPhase", this));
            
            BroadcastToChat(string.Format(lang.GetMessage("Title", this) + 
                lang.GetMessage("BuildPhaseTime", this), 
                (TimeBuild / 60).ToString()));

            //Build Rate
            CraftingRate = CraftBuild;

            //Gather Rate
            BroadcastToChat(string.Format(lang.GetMessage("Title", this) +
                lang.GetMessage("HiGatherRate", this),
                (TimeBuild / 60).ToString()));
            GatherRate = BuildGatherMulti;


            //Update
            UpdateGatherRate();
            UpdateCraftingRate();

            //Timers
            PhaseStart = DateTime.Now.AddMinutes(TimeBuild/60);
            AutoTimers.Add(timer.Once(TimeBuild, () => StartFightPhase()));
        }
        private void StartFightPhase()
        {
            DestroyTimers();
            BuildPhase = false;

            BroadcastToChat(lang.GetMessage("Title", this) +
                        lang.GetMessage("FightPhase", this));

            BroadcastToChat(string.Format(lang.GetMessage("Title", this) +
                lang.GetMessage("FightPhaseTime", this),
                (TimeFight / 60).ToString()));

            //Heli Wave
            StartHeliWaves();
            
            // Low Build
            BroadcastToChat(lang.GetMessage("Title", this) +
                        lang.GetMessage("LowBuildRate", this));
            CraftingRate = CraftFight;

            //Low Gather
            BroadcastToChat(lang.GetMessage("Title", this) +
                        lang.GetMessage("LowGatherRate", this));
            GatherRate = FightGatherMulti;

            //Updates
            UpdateGatherRate();
            UpdateCraftingRate();

            //Timers
            PhaseStart = DateTime.Now.AddMinutes(TimeBuild / 60);
            AutoTimers.Add(timer.Once(TimeFight, () => StartBuildPhase()));

        }

        
        private void StartHeliWaves()
        {
            BroadcastToChat(lang.GetMessage("Title", this) +
                        lang.GetMessage("MoreHelicopters", this));
            callHeli(1);
            AutoTimers.Add(timer.Once(TimeHeli, () => StartHeliWaves()));

        }
        private void StartDropWaves()
        {
            if (DropBuild != 0 || DropFight != 0) { 
                BroadcastToChat(lang.GetMessage("Title", this) +
                            lang.GetMessage("MoreCargoDrop", this));
                callDrop(1);
                if (DropBuild >= 1 && BuildPhase)
                    AutoTimers.Add(timer.Once(TimeDropBuild, () => StartDropWaves()));
                else if (DropFight >= 1 && !BuildPhase)
                    AutoTimers.Add(timer.Once(TimeDropFight, () => StartDropWaves()));
            }

        }


        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;

            var gatherType = dispenser.gatherType.ToString("G");
            var amount = item.amount;


            item.amount = (int)(item.amount * GatherPC);

            dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount = (int)(amount * 1.5);

            if (dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount < 0)
                item.amount += (int)dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount;
        }

        ////////////////////////////////////////////////////////////
        // HeliCopter Spawn ////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        #region Helicopter
        void OnEntitySpawned(BaseNetworkable entity)
        {

            if (entity == null) return;

            //994850627 is the prefabID of a heli.
            if (entity.prefabID == 994850627)
            {
                BaseHelicopter heli = (BaseHelicopter)entity;
                heli.maxCratesToSpawn = 2;
                heli.bulletDamage = 10f;
                typeof(PatrolHelicopterAI).GetField("maxRockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(entity.GetComponent<PatrolHelicopterAI>(), 20);
            }
        }
        private void callHeli(int num = 1)
        {
            int i = 0;
            while (i < num)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (!(bool)((UnityEngine.Object)entity))
                    return;
                PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
                heliAI.maxSpeed = (float)HeliSpeed;     //helicopter speed
                                                        //Change the health & weakpoint(s) heath
                ((BaseCombatEntity)entity).startHealth = HeliHP;
                var weakspots = ((BaseHelicopter)entity).weakspots;
                weakspots[0].maxHealth = HeliHP / 2;
                weakspots[0].health = HeliHP / 2;
                weakspots[1].maxHealth = HeliHPRudder;
                weakspots[1].health = HeliHPRudder;
                entity.Spawn(true);
                i++;
            }
        }
        #endregion
        private void callDrop(int num = 1)
        {
            int i = 0;
            while (i < num)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", new Vector3(), new Quaternion(), true);
                
                if (!(bool)((UnityEngine.Object)entity))
                    return;
                CargoPlane cargoI = entity.GetComponent<CargoPlane>();
                cargoI.InitDropPosition(GetRandomWorldPos());
                entity.Spawn(true);
                i++;
            }
        }
        ////////////////////////////////////////////////////////////
        // Config //////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////   
        private void LoadConfigVariables()
        {
            
            Puts("Configuration file started.");

            CheckCfg<int>("Time - Build", ref TimeBuild);
            CheckCfg<int>("Time - Fight", ref TimeFight);
            CheckCfg<int>("Time - Heli", ref TimeHeli);
            CheckCfg<int>("Time - Drop Build", ref TimeDropBuild);
            CheckCfg<int>("Time - Drop Fight", ref TimeDropFight);
            CheckCfg<int>("Craft - Build", ref CraftBuild);
            CheckCfg<int>("Craft - Fight", ref CraftFight);
            CheckCfg<int>("Drop - Build", ref DropBuild);
            CheckCfg<int>("Drop - Fight", ref DropFight);
            CheckCfg<int>("Heli - Speed", ref HeliSpeed);
            CheckCfg<int>("Heli - HP", ref HeliHP);
            CheckCfg<int>("Heli - HPRudder", ref HeliHPRudder);
            CheckCfg<int>("Gather - Build", ref BuildGatherMulti);
            CheckCfg<int>("Gather - Fight", ref FightGatherMulti);

            Puts("Configuration file updated.");
        }

        ////////////////////////////////////////////////////////////
        // Console Commands ////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        #region console commands
        [ChatCommand("phase")]
        private void chatcmdPhase(BasePlayer player, string command, string[] arg)
        {
            PhaseStr = "Not Enabled";

            TimeSpan timeRemaining = new TimeSpan();
            if (FWEnabled)
            {

                timeRemaining = PhaseStart.Subtract(DateTime.Now);
                if (BuildPhase)
                {
                    PhaseStr = lang.GetMessage("BuildPhaseTime", this, player.UserIDString);
                }
                else
                    PhaseStr = lang.GetMessage("FightPhaseTime", this, player.UserIDString);


            }
            SendReply(player, PhaseStr, (int)timeRemaining.TotalMinutes + 1);
        }

        [ChatCommand("hell")]
        private void chatcmdHell(BasePlayer player, string command, string[] arg)
        {
            if (!IsAllowed(player, "FortWars.UseAll", false))
                if (!IsAllowed(player, "FortWars.UseHeli", true)) return;

            int num = 1;
            PhaseStr = lang.GetMessage("NotEnabled", this, player.UserIDString);
            if (FWEnabled)
            {
                PhaseStr = lang.GetMessage("HeliBuild", this, player.UserIDString);
                if (!BuildPhase)
                {
                    
                    bool result = Int32.TryParse(arg[0], out num);
                    if (!result)
                        num = 1;
                    callHeli(num);
                    PhaseStr =
                        lang.GetMessage("Title", this) + 
                        lang.GetMessage("HeliSpawn", this, player.UserIDString);

                }
            }
            SendReply(player, PhaseStr, num.ToString());
        }

        [ChatCommand("drop")]
        private void chatcmdDrop(BasePlayer player, string command, string[] arg)
        {
            if (!IsAllowed(player, "FortWars.UseAll", false))
                if (!IsAllowed(player, "FortWars.UseDrop", true)) return;

            int num = 1;
            PhaseStr = lang.GetMessage("NotEnabled", this, player.UserIDString);
            if (FWEnabled)
            {
                bool result = Int32.TryParse(arg[0], out num);
                if (!result)
                    num = 1;
                callDrop(num);
                PhaseStr =
                    lang.GetMessage("Title", this) +
                    lang.GetMessage("DropSpawn", this, player.UserIDString);

            }
            SendReply(player, PhaseStr, num.ToString());
        }

        [ConsoleCommand("fw.fight")]
        void ccmdFight(ConsoleSystem.Arg arg)
        {
            if (!IsAllowed(arg, "FortWars.UseAll", false))
                if (!IsAllowed(arg, "FortWars.UseFight", true)) return;

            StartFightPhase();
            return;

        }

        [ConsoleCommand("fw.build")]
        void ccmdBuild(ConsoleSystem.Arg arg)
        {
            if(!IsAllowed(arg, "FortWars.UseAll", false))
                if(!IsAllowed(arg, "FortWars.UseBuild", true)) return;

            StartBuildPhase();
            return;
        }

        [ConsoleCommand("fw.enable")]
        void ccmdEnable(ConsoleSystem.Arg arg)
        {
            if (!IsAllowed(arg, "FortWars.UseAll", false))
                if (!IsAllowed(arg, "FortWars.UseEnable", true)) return;

            var rate = arg.GetInt(0);
            if (rate == 1)
            {
                FWEnabled = true;
                StartBuildPhase();
                return;
            }
            if (rate == 0)
            {
                FWEnabled = false;
                DestroyTimers();
                return;
            }

        }
        #endregion
        ////////////////////////////////////////////////////////////
        // Utilities ///////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        #region Utilities 
        void UpdateGatherRate()
        {
            GatherPC = GatherRate / 100;
            if (GatherPC < 1) GatherPC = 1;
        }
        void DestroyTimers()
        {
            foreach (Oxide.Plugins.Timer eventimer in AutoTimers)
            {
                eventimer.Destroy();
            }
            
            AutoTimers.Clear();
        }
        bool IsAllowed(BasePlayer player, string perm, bool bmsg = true)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            if (bmsg)
                SendReply(player, lang.GetMessage("NoPerms", this, player.UserIDString));
            return false;
        }
        bool IsAllowed(ConsoleSystem.Arg arg, string perm, bool bmsg = true)
        {
            if (permission.UserHasPermission(arg.Player().userID.ToString(), perm)) return true;
            if(bmsg)
                SendReply(arg, lang.GetMessage("NoPerms", this, arg.Player().UserIDString));
            return false;
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
            }
            SaveConfig();
        }
        
        ////////////////////////////////////////////////////////////
        // Auth Check //////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, lang.GetMessage("NoPerms", this, arg.Player().UserIDString));
                    return false;
                }
            }
            return true;
        }


        ////////////////////////////////////////////////////////////
        // Update Crafting /////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        private void UpdateCraftingRate()
        {
            foreach (var bp in blueprintDefinitions)
            {
                bp.time = Blueprints[bp.targetItem.shortname] * CraftingRate / 100;
            }
        }

        ////////////////////////////////////////////////////////////
        // Chat Broadcast //////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        void BroadcastToChat(string msg)
        {
            Debug.Log(msg);
            ConsoleSystem.Broadcast("chat.add", new object[] { 0, msg });
        }
        private void SendChatMessage(BasePlayer player, string message)
        {
            player?.SendConsoleCommand("chat.add", -1, message);
        }

        ////////////////////////////////////////////////////////////
        // Random //////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////
        public Vector3 GetRandomWorldPos()
        {
            var x = Oxide.Core.Random.Range(MinX, MaxX + 1) + 1;
            var y = Oxide.Core.Random.Range(MinY, MaxY + 1);
            var z = Oxide.Core.Random.Range(MinZ, MaxZ + 1) + 1;

            return new Vector3(x, y, z);
        }


        #endregion



    }
}

