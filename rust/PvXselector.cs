using Oxide.Core;
//using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;                      //DateTime
using System.Collections.Generic;  //Required for Whilelist
//using System.Data;
//using System.Globalization;
using System.Linq;
//using System.Reflection;
using UnityEngine;
//using System.Collections;
//using ConVar;
//using Facepunch;
//using Network;
//using ProtoBuf;
//using System.Runtime.CompilerServices;
//using Oxide.Core.Libraries.Covalence;
//using System.Text.RegularExpressions;
//using Oxide.Plugins;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("PvXSelector", "Alphawar", "0.9.5", ResourceId = 1817)]
    [Description("Player vs X Selector: Beta version 14")]
    class PvXselector : RustPlugin
    {

        #region Data/PlayerJoin/ServerInit
        //Loaded goes first
        PlayerDataStorage playerData;
        private DynamicConfigFile PlayerData;
        TicketDataStorage ticketData;
        private DynamicConfigFile TicketData;
        TicketLogStorage ticketLog;
        private DynamicConfigFile TicketLog;

        private List<ulong> selectGuiOpen = new List<ulong>();
        private Hash<ulong, PlayerInfo> InfoCache = new Hash<ulong, PlayerInfo>();
        private List<ulong> SleeperCache = new List<ulong>();
        private List<ulong> UnknownUserCache = new List<ulong>();
        private List<BasePlayer> AdminPlayerMode = new List<BasePlayer>();
        private List<BasePlayer> activeAdmins = new List<BasePlayer>();
        private List<ulong> antiChatSpam = new List<ulong>();
        private Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();

        class PlayerDataStorage
        {
            public Hash<ulong, PlayerInfo> Info = new Hash<ulong, PlayerInfo>();
            public List<ulong> sleepers = new List<ulong>();
            public List<ulong> UnknownUser = new List<ulong>();
        }
        class TicketDataStorage
        {
            public Dictionary<int, ulong> Link = new Dictionary<int, ulong>();
            public Dictionary<ulong, Ticket> Info = new Dictionary<ulong, Ticket>();
            public Dictionary<ulong, string> Notification = new Dictionary<ulong, string>();
        }
        class TicketLogStorage
        {
            public Dictionary<int, LogData> Log = new Dictionary<int, LogData>();
        }

        class PlayerInfo
        {
            public string username;
            public string FirstConnection;
            public string LatestConnection;
            public string mode;
            public bool ticket;
        }
        class Ticket
        {
            public int TicketNumber;
            public string Username;
            public string UserId;
            public string requested;
            public string reason;
            public string CreatedTimeStamp;
        }
        class LogData
        {
            public string UserId;
            public string AdminId;
            public string Username;
            public string AdminName;
            public string requested;
            public string reason;
            public bool Accepted;
            public string CreatedTimeStamp;
            public string ClosedTimeStamp;
        }

        void OnServerInitialized()
        {
            LoadData();
            InfoCache = playerData.Info;
            checkPlayersRegistered();
            RegisterGroups();
            foreach (BasePlayer _player in BasePlayer.activePlayerList)
            {
                if (!isNPC(_player))
                {
                    createPvXIndicator(_player);
                    if (isplayerNA(_player))createPvXSelector(_player);
                    if (hasPerm(_player, "admin"))
                    {
                        activeAdmins.Add(_player);
                        createAdminIndicator(_player);
                    }
                    updatePlayerChatTag(_player);
                }
            }
            foreach (ulong _key in playerData.Info.Keys)
            {
                if (playerData.Info[_key].FirstConnection == null) playerData.Info[_key].FirstConnection = DateTimeStamp();
                if (playerData.Info[_key].LatestConnection == null) playerData.Info[_key].LatestConnection = DateTimeStamp();
                if (playerData.Info[_key].FirstConnection == "null") playerData.Info[_key].FirstConnection = DateTimeStamp();
                if (playerData.Info[_key].LatestConnection == "null") playerData.Info[_key].LatestConnection = DateTimeStamp();
            }
        }
        void Loaded()
        {
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("PvX/PlayerData");
            TicketData = Interface.Oxide.DataFileSystem.GetFile("PvX/TicketData");
            TicketLog = Interface.Oxide.DataFileSystem.GetFile("PvX/TicketLog");
            lang.RegisterMessages(messages, this);
            permissionHandle();
            LoadVariables();
        }
        void Unloaded()
        {
            foreach (var _player in BasePlayer.activePlayerList)
            {
                DestroyAllPvXUI(_player);
            }
            SaveAll();
        }

        void saveCacheData()
        {
            playerData.Info = InfoCache;
            playerData.sleepers = SleeperCache;
            playerData.UnknownUser = UnknownUserCache;
            PlayerData.WriteObject(playerData);
        }
        void saveTicketData()
        {
            TicketData.WriteObject(ticketData);
        }
        void saveTicketLog()
        {
            TicketLog.WriteObject(TicketLog);
        }
        void SaveAll()
        {
            playerData.Info = InfoCache;
            playerData.sleepers = SleeperCache;
            playerData.UnknownUser = UnknownUserCache;
            PlayerData.WriteObject(playerData);
            TicketData.WriteObject(ticketData);
            TicketLog.WriteObject(ticketLog);
        }

        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<PlayerDataStorage>();
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                playerData = new PlayerDataStorage();
                InfoCache = playerData.Info;
            }
            try
            {
                ticketData = TicketData.ReadObject<TicketDataStorage>();
            }
            catch
            {
                Puts("Couldn't load Ticket data, creating new datafile");
                ticketData = new TicketDataStorage();
            }
            try
            {
                ticketLog = TicketLog.ReadObject<TicketLogStorage>();
            }
            catch
            {
                Puts("Couldn't load Ticket Log, creating new datafile");
                ticketLog = new TicketLogStorage();
            }
        }
        void OnPlayerInit(BasePlayer _player)
        {
            if (_player == null) return;
            if (_player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(3, () => OnPlayerInit(_player));
                return;
            }
            else PlayerLoaded(_player);
        }
        void PlayerLoaded(BasePlayer _player)
        {
            if (hasPerm(_player, "admin"))
            {
                activeAdmins.Add(_player);
                createAdminIndicator(_player);
            }
            if (playerData.UnknownUser.Contains(_player.userID))
            {
                updatePvXPlayerData(_player);
                playerData.UnknownUser.Remove(_player.userID);
            }
            if (isplayerNA(_player))
            {
                updatePlayerChatTag(_player);
                saveCacheData();
                createPvXIndicator(_player);
                createPvXSelector(_player);
                return;
            }
            InfoCache[_player.userID].LatestConnection = DateTimeStamp();
            updatePlayerChatTag(_player);
            if (SleeperCache.Contains(_player.userID))
            {
                updatePvXPlayerData(_player);
                SleeperCache.Remove(_player.userID);
                saveCacheData();
            }
            if (UnknownUserCache.Contains(_player.userID))
            {
                updatePvXPlayerData(_player);
                saveCacheData();
            }
            SaveAll();
            createPvXIndicator(_player);
            if (InfoCache[_player.userID].mode != "NA") return;
            else if (ticketData.Notification.ContainsKey(_player.userID)) LangMSG(_player, "TickClosLogin", ticketData.Notification[_player.userID]);
            else createPvXSelector(_player);
        }
        void OnPlayerDisconnected(BasePlayer _player)
        {
            selectGuiOpen.Remove(_player.userID);
            if (hasPerm(_player, "admin"))
            {
                activeAdmins.Remove(_player);
            }
        }
        void OnPlayerRespawned(BasePlayer _player)
        {
        }


        void addTicketLog(BasePlayer _admin, int _ticketID, bool _)
        {
            ulong _UserID = ticketData.Link[_ticketID];
            int _logID = NewLogID();
            ticketLog.Log.Add(_logID, new LogData
            {
                Accepted = true,
                ClosedTimeStamp = DateTimeStamp(),
                AdminId = _admin.UserIDString,
                CreatedTimeStamp = ticketData.Info[_UserID].CreatedTimeStamp,
                reason = ticketData.Info[_UserID].reason,
                requested = ticketData.Info[_UserID].requested,
                UserId = ticketData.Info[_UserID].UserId,
                AdminName = _admin.displayName,
                Username = ticketData.Info[_UserID].Username,
            });
        }

        void checkPlayersRegistered()
        {
            foreach (BasePlayer _player in BasePlayer.activePlayerList)
                if (!(InfoCache.ContainsKey(_player.userID)))
                    addPlayer(_player);

            foreach (BasePlayer _player in BasePlayer.sleepingPlayerList)
                if (!(InfoCache.ContainsKey(_player.userID)))
                    addSleeper(_player);
        }
        void addPlayer(BasePlayer _player)
        {
            if (isNPC(_player.userID)) return;
            InfoCache.Add(_player.userID, new PlayerInfo
            {
                username = _player.displayName,
                mode = "NA",
                ticket = false,
                FirstConnection = DateTimeStamp(),
                LatestConnection = DateTimeStamp(),
            });
            createPvXSelector(_player);
            saveCacheData();
        }
        void addSleeper(BasePlayer _player)
        {
            if (isNPC(_player.userID)) return;
            InfoCache.Add(_player.userID, new PlayerInfo
            {
                username = _player.displayName,
                mode = "NA",
                ticket = false,
                FirstConnection = DateTimeStamp(),
                LatestConnection = "Sleeper",
            });
            SleeperCache.Add(_player.userID);
            saveCacheData();
        }
        void addOffline(ulong _userID)
        {
            if (isNPC(_userID)) return;
            InfoCache.Add(_userID, new PlayerInfo
            {
                username = "UNKNOWN",
                mode = "NA",
                ticket = false,
                FirstConnection = DateTimeStamp(),
                LatestConnection = "UNKNOWN",
            });
            UnknownUserCache.Add(_userID);
            saveCacheData();
        }

        static string pvxPlayerSelectorUI = "createPvXModeSelector";
        static string pvxPlayerUI = "pvxPlayerModeUI";
        static string pvxAdminUI = "pvxAdminTicketCountUI";
        string[] GuiList = new string[] { pvxPlayerSelectorUI, pvxPlayerUI, pvxAdminUI};

        #endregion

        #region Config/Permision/Plugin Ref
        //Players
        private bool PvEAttackPvE;
        private bool PvEAttackPvP;
        private bool PvPAttackPvE;
        private bool PvPAttackPvP;
        private bool PvELootPvE;
        private bool PvELootPvP;
        private bool PvPLootPvE;
        private bool PvPLootPvP;
        private bool PvEUsePvPDoor;
        private bool PvPUsePvEDoor;
        private float PvEDamagePvE;
        private float PvEDamagePvP;
        private float PvPDamagePvE;
        private float PvPDamagePvP;
        private float PvEDamagePvEStruct;
        private float PvEDamagePvPStruct;
        private float PvPDamagePvEStruct;
        private float PvPDamagePvPStruct;
        //Metabolism
        private float PvEFoodLossRate;
        private float PvEWaterLossRate;
        private float PvEHealthGainRate;
        private float PvEFoodSpawn;
        private float PvEWaterSpawn;
        private float PvEHealthSpawn;
        private float PvPFoodLossRate;
        private float PvPWaterLossRate;
        private float PvPHealthGainRate;
        private float PvPFoodSpawn;
        private float PvPWaterSpawn;
        private float PvPHealthSpawn;
        //NPC
        private bool NPCAttackPvE;
        private bool NPCAttackPvP;
        private bool PvEAttackNPC;
        private bool PvPAttackNPC;
        private float NPCDamagePvE;
        private float NPCDamagePvP;
        private float PvEDamageNPC;
        private float PvPDamageNPC;
        private bool PvELootNPC;
        private bool PvPLootNPC;
        //Animal
        private float PvEDamageAnimals;
        private float PvPDamageAnimals;
        private float NPCDamageAnimals;
        private float AnimalsDamagePvE;
        private float AnimalsDamagePvP;
        private float AnimalsDamageNPC;
        //Turret
        private bool TurretPvETargetPvE;
        private bool TurretPvETargetPvP;
        private bool TurretPvPTargetPvE;
        private bool TurretPvPTargetPvP;
        private bool TurretPvETargetNPC;
        private bool TurretPvPTargetNPC;
        private bool TurretPvETargetAnimal;
        private bool TurretPvPTargetAnimal;
        private float TurretPvEDamagePvEAmnt;
        private float TurretPvEDamagePvPAmnt;
        private float TurretPvPDamagePvEAmnt;
        private float TurretPvPDamagePvPAmnt;
        private float TurretPvEDamageNPCAmnt;
        private float TurretPvPDamageNPCAmnt;
        private float TurretPvEDamageAnimalAmnt;
        private float TurretPvPDamageAnimalAmnt;
        //Helicopter
        private bool HeliTargetPvE;
        private bool HeliTargetPvP;
        private bool HeliTargetNPC;
        private float HeliDamagePvE;
        private float HeliDamagePvP;
        private float HeliDamageNPC;
        private float HeliDamagePvEStruct;
        private float HeliDamagePvPStruct;
        private float HeliDamageAnimal;
        private float HeliDamageByPvE;
        private float HeliDamageByPvP;
        //Fire
        private float FireDamagePvE;
        private float FireDamagePvP;
        private float FireDamageNPC;
        private float FireDamagePvEStruc;
        private float FireDamagePvPStruc;
        //Others
        public static bool DisableUI_FadeIn;
        private bool DebugMode;
        private bool NamesIncludeSleepers;
        private string ChatPrefixColor;
        private string ChatPrefix;
        private string ChatMessageColor;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
        void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
        {
            //Players
            PvEAttackPvE = Convert.ToBoolean(GetConfig("2: Player", "01: PvE v PvE", false));
            PvEAttackPvP = Convert.ToBoolean(GetConfig("2: Player", "02:PvE v PvP", false));
            PvPAttackPvE = Convert.ToBoolean(GetConfig("2: Player", "03:PvP v PvE", false));
            PvPAttackPvP = Convert.ToBoolean(GetConfig("2: Player", "04:PvP v PvP", true));
            PvELootPvE = Convert.ToBoolean(GetConfig("2: Player", "05:PvE Loot PvE", true));
            PvELootPvP = Convert.ToBoolean(GetConfig("2: Player", "06:PvE Loot PvP", false));
            PvPLootPvE = Convert.ToBoolean(GetConfig("2: Player", "07:PvP Loot PvE", false));
            PvPLootPvP = Convert.ToBoolean(GetConfig("2: Player", "08:PvP Loot PvP", true));
            PvEUsePvPDoor = Convert.ToBoolean(GetConfig("2: Player", "09:PvE Use PvPDoor", false));
            PvPUsePvEDoor = Convert.ToBoolean(GetConfig("2: Player", "10:PvP Use PvEDoor", false));
            PvEDamagePvE = Convert.ToSingle(GetConfig("2: Player", "11:PvE Damage PvE", 0.0));
            PvEDamagePvP = Convert.ToSingle(GetConfig("2: Player", "12:PvE Damage PvP", 0.0));
            PvPDamagePvE = Convert.ToSingle(GetConfig("2: Player", "13:PvP Damage PvE", 0.0));
            PvPDamagePvP = Convert.ToSingle(GetConfig("2: Player", "14:PvP Damage PvP", 1.0));
            PvEDamagePvEStruct = Convert.ToSingle(GetConfig("2: Player", "15: PvEDamagePvEStruct", 0.0));
            PvEDamagePvPStruct = Convert.ToSingle(GetConfig("2: Player", "16: PvEDamagePvPStruct", 0.0));
            PvPDamagePvEStruct = Convert.ToSingle(GetConfig("2: Player", "17: PvPDamagePvEStruct", 0.0));
            PvPDamagePvPStruct = Convert.ToSingle(GetConfig("2: Player", "18: PvPDamagePvPStruct", 1.0));
            //Metabolism
            PvEFoodLossRate = Convert.ToSingle(GetConfig("3: Metabolism", "01: PvEFoodLossRate", 0.03));
            PvEWaterLossRate = Convert.ToSingle(GetConfig("3: Metabolism", "02: PvEWaterLossRate", 0.03));
            PvEHealthGainRate = Convert.ToSingle(GetConfig("3: Metabolism", "03: PvEHealthGainRate", 0.03));
            PvEFoodSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "04: PvEFoodSpawn", 100.0));
            PvEWaterSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "05: PvEWaterSpawn", 250.00));
            PvEHealthSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "06: PvEHealthSpawn", 500.00));
            PvPFoodLossRate = Convert.ToSingle(GetConfig("3: Metabolism", "07: PvPFoodLossRate", 0.03));
            PvPWaterLossRate = Convert.ToSingle(GetConfig("3: Metabolism", "08: PvPWaterLossRate", 0.03));
            PvPHealthGainRate = Convert.ToSingle(GetConfig("3: Metabolism", "09: PvPHealthGainRate", 0.03));
            PvPFoodSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "10: PvPFoodSpawn", 100.0));
            PvPWaterSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "11: PvPWaterSpawn", 250.0));
            PvPHealthSpawn = Convert.ToSingle(GetConfig("3: Metabolism", "12: PvPHealthSpawn", 500.0));
            //NPC
            NPCAttackPvE = Convert.ToBoolean(GetConfig("4: NPC", "01: NPC Attack PvE", true));
            NPCAttackPvP = Convert.ToBoolean(GetConfig("4: NPC", "02: NPC Attack PvP", true));
            PvEAttackNPC = Convert.ToBoolean(GetConfig("4: NPC", "03: PvE Attack NPC", true));
            PvPAttackNPC = Convert.ToBoolean(GetConfig("4: NPC", "04: PvP Attack NPC", true));
            NPCDamagePvE = Convert.ToSingle(GetConfig("4: NPC", "05: NPC Damage PvE", 1.0));
            NPCDamagePvP = Convert.ToSingle(GetConfig("4: NPC", "06: NPC Damage PvP", 1.0));
            PvEDamageNPC = Convert.ToSingle(GetConfig("4: NPC", "07: PvE Damage NPC", 1.0));
            PvPDamageNPC = Convert.ToSingle(GetConfig("4: NPC", "08: PvP Damage NPC", 1.0));
            PvELootNPC = Convert.ToBoolean(GetConfig("4: NPC", "09: PvE Loot NPC", true));
            PvPLootNPC = Convert.ToBoolean(GetConfig("4: NPC", "10: PvP Loot NPC", true));
            //Animal
            PvEDamageAnimals = Convert.ToSingle(GetConfig("5: Animals", "1: PvE Damage Animals", 1.0f));
            PvPDamageAnimals = Convert.ToSingle(GetConfig("5: Animals", "2: PvP Damage Animals", 1.0f));
            NPCDamageAnimals = Convert.ToSingle(GetConfig("5: Animals", "3: NPC Damage Animals", 1.0f));
            AnimalsDamagePvE = Convert.ToSingle(GetConfig("5: Animals", "4: Animals Damage PvE", 1.0f));
            AnimalsDamagePvP = Convert.ToSingle(GetConfig("5: Animals", "5: Animals Damage PvP", 1.0f));
            AnimalsDamageNPC = Convert.ToSingle(GetConfig("5: Animals", "6: Animals Damage NPC", 1.0f));
            //Turret
            TurretPvETargetPvE = Convert.ToBoolean(GetConfig("6: Turret", "01: TurretPvETargetPvE", true));
            TurretPvETargetPvP = Convert.ToBoolean(GetConfig("6: Turret", "02: TurretPvETargetPvP", false));
            TurretPvPTargetPvE = Convert.ToBoolean(GetConfig("6: Turret", "03: TurretPvPTargetPvE", false));
            TurretPvPTargetPvP = Convert.ToBoolean(GetConfig("6: Turret", "04: TurretPvPTargetPvP", true));
            TurretPvETargetNPC = Convert.ToBoolean(GetConfig("6: Turret", "05: TurretPvETargetNPC", true));
            TurretPvPTargetNPC = Convert.ToBoolean(GetConfig("6: Turret", "06: TurretPvPTargetNPC", true));
            TurretPvETargetAnimal = Convert.ToBoolean(GetConfig("6: Turret", "07: TurretPvETargetAnimal", true));
            TurretPvPTargetAnimal = Convert.ToBoolean(GetConfig("6: Turret", "08: TurretPvPTargetAnimal", true));
            TurretPvEDamagePvEAmnt = Convert.ToSingle(GetConfig("6: Turret", "09: TurretPvEDamagePvEAmnt", 1.0f));
            TurretPvEDamagePvPAmnt = Convert.ToSingle(GetConfig("6: Turret", "10: TurretPvEDamagePvPAmnt", 0.0f));
            TurretPvPDamagePvEAmnt = Convert.ToSingle(GetConfig("6: Turret", "11: TurretPvPDamagePvEAmnt", 0.0f));
            TurretPvPDamagePvPAmnt = Convert.ToSingle(GetConfig("6: Turret", "12: TurretPvPDamagePvPAmnt", 1.0f));
            TurretPvEDamageNPCAmnt = Convert.ToSingle(GetConfig("6: Turret", "13: TurretPvEDamageNPCAmnt", 1.0f));
            TurretPvPDamageNPCAmnt = Convert.ToSingle(GetConfig("6: Turret", "14: TurretPvPDamageNPCAmnt", 1.0f));
            TurretPvEDamageAnimalAmnt = Convert.ToSingle(GetConfig("6: Turret", "15: TurretPvEDamageAnimal", 1.0f));
            TurretPvPDamageAnimalAmnt = Convert.ToSingle(GetConfig("6: Turret", "16: TurretPvPDamageAnimal", 1.0f));
            //Helicopter
            HeliTargetPvE = Convert.ToBoolean(GetConfig("7: Heli", "01: HeliTargetPvE", false));
            HeliTargetPvP = Convert.ToBoolean(GetConfig("7: Heli", "02: HeliTargetPvP", true));
            HeliTargetNPC = Convert.ToBoolean(GetConfig("7: Heli", "03: HeliTargetNPC", false));
            HeliDamagePvE = Convert.ToSingle(GetConfig("7: Heli", "04: HeliDamagePvE", 0.0));
            HeliDamagePvP = Convert.ToSingle(GetConfig("7: Heli", "05: HeliDamagePvP", 1.0));
            HeliDamageNPC = Convert.ToSingle(GetConfig("7: Heli", "06: HeliDamageNPC", 0.0));
            HeliDamagePvEStruct = Convert.ToSingle(GetConfig("7: Heli", "07: HeliDamagePvEStruct", 0.0));
            HeliDamagePvPStruct = Convert.ToSingle(GetConfig("7: Heli", "08: HeliDamagePvPStruct", 1.0));
            HeliDamageAnimal = Convert.ToSingle(GetConfig("7: Heli", "09: HeliDamageAnimal", 1.0));
            HeliDamageByPvE = Convert.ToSingle(GetConfig("7: Heli", "10: HeliDamageByPvE", 0.0));
            HeliDamageByPvP = Convert.ToSingle(GetConfig("7: Heli", "11: HeliDamageByPvp", 1.0));
            //fire
            FireDamagePvE = Convert.ToSingle(GetConfig("8: Fire", "1: FireDamagePvE", 0.1));
            FireDamagePvP = Convert.ToSingle(GetConfig("8: Fire", "2: FireDamagePvP", 1.0));
            FireDamageNPC = Convert.ToSingle(GetConfig("8: Fire", "3: FireDamageNPC", 1.0));
            FireDamagePvEStruc = Convert.ToSingle(GetConfig("8: Fire", "4: FireDamagePvEStruc", 0.0));
            FireDamagePvPStruc = Convert.ToSingle(GetConfig("8: Fire", "5: FireDamagePvPStruc", 1.0));
            //others
            DisableUI_FadeIn = Convert.ToBoolean(GetConfig("9-1:Settings", "DisableUI Fadein", false));
            DebugMode = Convert.ToBoolean(GetConfig("9-1:Settings", "DebugMode", false));
            ChatPrefix = Convert.ToString(GetConfig("9-1:Settings", "ChatPrefix", "PvX"));
            ChatPrefixColor = Convert.ToString(GetConfig("9-1:Settings", "ChatPrefixColor", "008800"));
            ChatMessageColor = Convert.ToString(GetConfig("9-1:Settings", "ChatMessageColor", "yellow"));
        }

        object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
            }
            return value;
        }
        T GetConfig<T>(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null)
            {
                Config.Set(args);
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        bool hasPerm(BasePlayer _player, string perm, string reason = null)
        {
            string regPerm = Title.ToLower() + "." + perm; //pvxselector.admin
            if (permission.UserHasPermission(_player.UserIDString, regPerm)) return true;
            if (reason != "null")
                SendReply(_player, reason);
            return false;
        }

        void permissionHandle()
        {
            string[] Permissionarray = { "admin", "wipe" };
            foreach (string i in Permissionarray)
            {
                string regPerm = Title.ToLower() + "." + i;
                Puts("Checking if " + regPerm + " is registered.");
                if (!permission.PermissionExists(regPerm))
                {
                    permission.RegisterPermission(regPerm, this);
                    Puts(regPerm + " is registered.");
                }
                else
                {
                    Puts(regPerm + " is already registered.");
                }
            }
        }
        #endregion

        #region UI Creation
        class QUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    new CuiElement().Parent = "Hud",
                    panelName
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer container, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }

        private void DestroyAllPvXUI(BasePlayer player)
        {
            foreach(string _v in GuiList)
            {
                CuiHelper.DestroyUi(player, _v);
            }
            //DestroyEntries(player);
        }
        private void DestroyPvXUI(BasePlayer player, string _ui)
        {
            CuiHelper.DestroyUi(player, _ui);
        }
        //private void DestroyEntries(BasePlayer player)
        //{
        //    CuiHelper.DestroyUi(player, UIPanel);
        //    if (OpenUI.ContainsKey(player.userID))
        //    {
        //        foreach (var entry in OpenUI[player.userID])
        //            CuiHelper.DestroyUi(player, entry);
        //        OpenUI.Remove(player.userID);
        //    }
        //}
        private void AddUIString(BasePlayer player, string name)
        {
            if (!OpenUI.ContainsKey(player.userID))
                OpenUI.Add(player.userID, new List<string>());
            OpenUI[player.userID].Add(name);
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"Black-100", "0.0 0.0 0.0 1.0" },  //Black
            {"Black-50", "0.0 0.0 0.0 0.50" },
            {"Black-15", "0.0 0.0 0.0 0.15" },
            {"Grey2-100", "0.2 0.2 0.2 1.0" },  //Grey 2
            {"Grey2-50", "0.2 0.2 0.2 0.50" },
            {"Grey2-15", "0.2 0.2 0.2 0.15" },
            {"Grey5-100", "0.5 0.5 0.5 1.0" },  //Grey 5
            {"Grey5-50", "0.5 0.5 0.5 0.50" },
            {"Grey5-15", "0.5 0.5 0.5 0.15" },
            {"Grey8-100", "0.8 0.8 0.8 1.0" },  //Grey 8
            {"Grey8-50", "0.8 0.8 0.8 0.50" },
            {"Grey8-15", "0.8 0.8 0.8 0.15" },
            {"White-100", "1.0 1.0 1.0 1.0" },  //White
            {"White-50", "1.0 1.0 1.0 0.50" },
            {"White-15", "1.0 1.0 1.0 0.15" },
            {"Red-100", "0.7 0.2 0.2 1.0" },    //Red
            {"Red-50", "0.7 0.2 0.2 0.50" },
            {"Red-15", "0.7 0.2 0.2 0.15" },
            {"Green-100", "0.2 0.7 0.2 1.0" },  //Green
            {"Green-50", "0.2 0.7 0.2 0.50" },
            {"Green-15", "0.2 0.7 0.2 0.15" },
            {"Blue-100", "0.2 0.2 0.7 1.0" },  //Blue
            {"Blue-50", "0.2 0.2 0.7 0.50" },
            {"Blue-15", "0.2 0.2 0.7 0.15" },
            {"Yellow-100", "0.9 0.9 0.2 1.0" },  //Yellow
            {"Yellow-50", "0.9 0.9 0.2 0.50" },
            {"Yellow-15", "0.9 0.9 0.2 0.15" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttonopen", "0.2 0.8 0.2 0.9" },
            {"buttoncompleted", "0 0.5 0.1 0.9" },
            {"buttonred", "0.85 0 0.35 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
        };
        #endregion

        #region GUIs

        private void createPvXSelector(BasePlayer _player)
        {
            selectGuiOpen.Add(_player.userID);
            timer.Once(5, () => updatePvXSelector(_player));
            var PvXselectorContainer = QUI.CreateElementContainer(
                pvxPlayerSelectorUI,
                UIColors["Black-50"],
                "0.17 0.15",
                "0.33 0.25",
                true
                );
            QUI.CreateButton(
                ref PvXselectorContainer,
                pvxPlayerSelectorUI,
                UIColors["Red-100"],
                "PvP",
                22,
                "0.1 0.2",
                "0.45 0.8",
                "PvXGuiCMD pvp"
                );
            QUI.CreateButton(
                ref PvXselectorContainer,
                pvxPlayerSelectorUI,
                UIColors["Green-100"],
                "PvE",
                22,
                "0.55 0.2",
                "0.9 0.8",
                "PvXGuiCMD pve"
                );
            CuiHelper.AddUi(_player, PvXselectorContainer);
        }
        private void updatePvXSelector(BasePlayer _player)
        {
            CuiHelper.DestroyUi(_player, pvxPlayerSelectorUI);
            if (isplayerNA(_player))createPvXSelector(_player);
        }

        private void createPvXIndicator(BasePlayer _player)
        {
            var indicatorContainer = QUI.CreateElementContainer(
                pvxPlayerUI,
                UIColors["Black-15"],
                "0.48 0.11",
                "0.52 0.14");
            if (InfoCache[_player.userID].mode == "NA")
                indicatorContainer = QUI.CreateElementContainer(
                    pvxPlayerUI,
                    UIColors["Red-100"],
                    "0.48 0.11",
                    "0.52 0.14");
            else if (ticketData.Info.ContainsKey(_player.userID))
                indicatorContainer = QUI.CreateElementContainer(
                    pvxPlayerUI,
                    UIColors["Yellow-15"],
                    "0.48 0.11",
                    "0.52 0.14");
            if (AdminPlayerMode.Contains(_player))
            {
                QUI.CreateLabel(
                    ref indicatorContainer,
                    pvxPlayerUI,
                    UIColors["Green-100"],
                    InfoCache[_player.userID].mode,
                    15,
                    "0.1 0.1",
                    "0.90 0.99");
            }
            else
            {
                QUI.CreateLabel(ref indicatorContainer,
                    pvxPlayerUI,
                    UIColors["White-100"],
                    InfoCache[_player.userID].mode,
                    15,
                    "0.1 0.1",
                    "0.90 0.99");
            }
            CuiHelper.AddUi(_player, indicatorContainer);
        }
        private void updatePvXIndicator(BasePlayer _player)
        {
            CuiHelper.DestroyUi(_player, pvxPlayerUI);
            createPvXIndicator(_player);
        }

        private void createAdminIndicator(BasePlayer _player)
        {
            if (!hasPerm(_player, "admin")) return;
            var adminCountContainer = QUI.CreateElementContainer(
                pvxAdminUI,
                UIColors["Black-100"],
                "0.166 0.055",
                "0.34 0.0955");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                UIColors["White-100"],
                "PvX Tickets",
                10,
                "0.0 0.1",
                "0.3 0.90");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                UIColors["White-100"],
                string.Format("Open: {0}", ticketData.Info.Count.ToString()),
                10,
                "0.301 0.1",
                "0.65 0.90");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                UIColors["White-100"],
                string.Format("Closed: {0}", ticketLog.Log.Count.ToString()),
                10,
                "0.651 0.1",
                "1 0.90");
            CuiHelper.AddUi(_player, adminCountContainer);
        }
        private void UpdateAdminIndicator()
        {
            if (activeAdmins == null) return;
            else if (activeAdmins.Count < 1) return;
            foreach (BasePlayer _player in activeAdmins)
            {
                CuiHelper.DestroyUi(_player, pvxAdminUI);
                createAdminIndicator(_player);
            }
        }

        private void createButton(BasePlayer _player)
        { }
        #endregion

        #region Lang/Chat
        void LangMSG(BasePlayer _player, string langMsg, params object[] args)
        {
            string message = lang.GetMessage(langMsg, this, _player.UserIDString);
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void LangMSGBroadcast(string langMsg, params object[] args)
        {
            string message = lang.GetMessage(langMsg, this);
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void BroadcastMessageHandle(string message, params object[] args)
        {
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void ChatMessageHandle(BasePlayer _player, string message, params object[] args)
        {
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void PutsLang(string langMsg, params object[] args)
        {
            string message = lang.GetMessage(langMsg, this);
            Puts(string.Format(message, args));
        }
        void PutsPlayerHandle(BasePlayer _player, string msg, params object[] args)
        {
            Puts(msg, args);
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{msg}</color>", args);
        }
        void PutsPlayerHandleLang(BasePlayer _player, string langMsg, params object[] args)
        {
            string msg = lang.GetMessage(langMsg, this, _player.UserIDString);
            Puts(msg, args);
            SendReply(_player, msg, args);
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"xx", "xxx" },
            {"notAllwPickup", "You can't pick this item as owner is: {0}" },
            {"AdmModeRem", "You have deactivated Admin Mode" },
            {"AdmModeAdd", "You are now in Admin mode" },
            {"lvlRedxpSav", "Your Level was reduced, Lost xp has been saved." },
            {"lvlIncrxpRes", "Your Level has been increased, Lost xp Restored." },
            {"numbonly", "Incorrect format: Included letter in ticketID" },
            {"TickCrea", "You have created a ticket" },
            {"TickCanc", "You have Canceled your ticket" },
            {"TickAcep", "Your Ticket has been accepted" },
            {"TickDecl", "Your Ticket has been declined" },
            {"TickAcepAdm", "Your Ticket accepted the ticket" },
            {"TickDeclAdm", "Your Ticket decline the ticket" },
            {"TickClosLogin", "Welcome back, Your ticket was {0}" },
            {"TickList", "Ticket#: {0}, User: {1}" },
            {"TickDet", "Ticket Details:" },
            {"TickID", "Ticket ID: {0}" },
            {"TickName", "Username: {0}" },
            {"TickStmID", "SteamID: {0}" },
            {"TickSelc", "Selected: {0}" },
            {"TickRsn", "Reason: {0}" },
            {"TickDate", "Ticket Created: {0}" },
            {"TickCnt", "Open Tickets: {0}" },
            {"TickNotAvail", "Ticket#:{0} Does not exist" },
            {"ComndList", "PvX Command List:" },
            {"CompTickCnt", "Closed Tickets: {0}" },
            {"IncoForm", "Incorrect format" },
            {"IncoFormPleaUse", "Incorrect format Please Use:" },
            {"TicketDefaultReason", "Change Requested Via Chat" },
            {"NoTicket", "There are no tickets to display" },
            {"AlreadySubmitted", "You have already requested to change, Please conctact your admin" },
            {"PvETarget", "You are attacking a PvE player" },
            {"NoActTick", "You do not have an active ticket" },
            {"RSNChan", "You have changed your tickets reason." },
            {"PvEStructure", "That structure belongs to a PvE player" },
            {"NoXPModeNA", "You will not earn XP until you have selected PvE/PvP" },
            {"NoSaveLvLNA", "Your levl is not saved unless you select PvE or PvP" },
            {"TargisGod", "You are attacking a god" },
            {"YouisGod", "You are attacking a god" },
            {"MissPerm", "You do not have the required permision" }
        };
        #endregion

        #region Ticket Functions
        void createTicket(BasePlayer _player, string selection)
        {
            int _TicketNumber = GetNewID();
            string _username = _player.displayName;
            string _requested = selection;
            string _reason = lang.GetMessage("TicketDefaultReason", this, _player.UserIDString);
            ticketData.Link.Add(_TicketNumber, _player.userID);
            ticketData.Info.Add(_player.userID, new Ticket
            {
                CreatedTimeStamp = DateTimeStamp(),
                reason = lang.GetMessage("TicketDefaultReason", this, _player.UserIDString),
                requested = selection,
                TicketNumber = _TicketNumber,
                UserId = _player.UserIDString,
                Username = _player.displayName
            });
            LangMSG(_player, "TickCrea");
            InfoCache[_player.userID].ticket = true;
            SaveAll();
            UpdateAdminIndicator();
            updatePvXIndicator(_player);
        }
        void cancelTicket(BasePlayer _player)
        {
            if (playerData.Info[_player.userID].ticket == false) return;
            int _ticketNumber = ticketData.Info[_player.userID].TicketNumber;
            ticketData.Link.Remove(_ticketNumber);
            ticketData.Info.Remove(_player.userID);
            InfoCache[_player.userID].ticket = false;
            SaveAll();
            LangMSG(_player, "TickCanc");
            updatePvXIndicator(_player);
            UpdateAdminIndicator();
            return;
        }
        void ticketAccept(BasePlayer _admin, int _ticketID)//Update required to fix Baseplayer NRE
        {
            ulong _UserID = ticketData.Link[_ticketID];
            addTicketLog(_admin, _ticketID, true);
            LangMSG(_admin, "TickAcepAdm");
            playerData.Info[_UserID].ticket = false;
            InfoCache[_UserID].mode = ticketData.Info[_UserID].requested;
            SaveAll();
            BasePlayer _player = basePlayerByID(_UserID);
            if (_player != null && _player.isConnected)
            {
                LangMSG(_player, "TickAcep");
                updatePvXIndicator(_player);
                updatePlayerChatTag(_player);
            }
            else if (_player != null && !_player.isConnected)
            {
                ticketData.Notification.Add(_player.userID, "Accepted");
            }
            else
            {
                ticketData.Notification.Add(_player.userID, "Accepted");
            }
            ticketData.Info.Remove(_UserID);
            ticketData.Link.Remove(_ticketID);
            SaveAll();
            UpdateAdminIndicator();
            if (_player != null && _player.isConnected) updatePvXIndicator(_player);
        }
        void ticketDecline(BasePlayer _admin, int _ticketID)//updated: Fixed Baseplayer NRE
        {
            ulong _UserID = ticketData.Link[_ticketID];
            addTicketLog(_admin, _ticketID, false);
            LangMSG(_admin, "TickAcepAdm");
            playerData.Info[_UserID].ticket = false;
            ticketData.Info.Remove(_UserID);
            ticketData.Link.Remove(_ticketID);
            SaveAll();
            UpdateAdminIndicator();
            BasePlayer _player = basePlayerByID(_UserID);
            if (_player != null && _player.isConnected)
            {
                LangMSG(_player, "TickDecl");
                updatePvXIndicator(_player);
            }
            else ticketData.Notification.Add(_player.userID, "Declined");
        }
        void ticketCount(BasePlayer _player)
        {
            LangMSG(_player, "TickCnt", ticketData.Link.Count);
            LangMSG(_player, "CompTickCnt", ticketLog.Log.Count);
        }
        void listTickets(BasePlayer _player)
        {
            if (ticketData.Link.Count > 0)
            {
                foreach (var ticket in ticketData.Info)
                {
                    ulong _key = ticket.Key;
                    PutsPlayerHandleLang(_player, "TickList", ticketData.Info[_key].TicketNumber, ticketData.Info[_key].Username);
                }
            }
        }
        void displayTicket(BasePlayer _player, int _ticketID)
        {
            if (ticketData.Link.ContainsKey(_ticketID))
            {
                ulong _key = ticketData.Link[_ticketID];
                //DateTime _date = DateTime.FromOADate(ticketData.Info[_key].timeStamp);
                LangMSG(_player, "TickDet");
                LangMSG(_player, "TickID", _ticketID);
                LangMSG(_player, "TickName", ticketData.Info[_key].Username);
                LangMSG(_player, "TickStmID", _key);
                LangMSG(_player, "TickSelc", ticketData.Info[_key].requested);
                LangMSG(_player, "TickRsn", ticketData.Info[_key].reason);
                LangMSG(_player, "TickDate", ticketData.Info[_key].CreatedTimeStamp);
            }
            else LangMSG(_player, "TickNotAvail", _ticketID);
        }
        bool playerHasTicket(BasePlayer _player)
        {
            if (InfoCache[_player.userID].ticket == true) return true;
            else return false;
        }
        bool playerHasTicket(ulong _userID)
        {
            if (InfoCache[_userID].ticket == true) return true;
            else return false;
        }

        int GetNewID()
        {
            for (int _i = 1; _i <= 500; _i++)
            {
                if (ticketData.Link.ContainsKey(_i)) { }//Place Debug code in future
                else
                {
                    //Puts("Key {0} doesnt exist, Returning ticket number", _i); //debug
                    return _i;
                }
            }
            return 0;
        }
        int NewLogID()
        {
            for (int _i = 1; _i <= 500; _i++)
            {
                if (ticketLog.Log.ContainsKey(_i)) { }
                else
                {
                    //Puts("Key {0} doesnt exist, Returning ticket number", _i); //debug
                    return _i;
                }
            }
            return 0;
        }

        void consolListTickets()
        {
            foreach (ulong _ticket in ticketData.Info.Keys)
            {
                Puts("    ");
                Puts("    ");
                PutsLang("TickDet");
                PutsLang("TickID", ticketData.Info[_ticket].TicketNumber);
                PutsLang("TickName", ticketData.Info[_ticket].Username);
                PutsLang("TickStmID", _ticket);
                PutsLang("TickSelc", ticketData.Info[_ticket].requested);
                PutsLang("TickRsn", ticketData.Info[_ticket].reason);
                PutsLang("TickDate", ticketData.Info[_ticket].CreatedTimeStamp);
            }
        }
        void consolListLog()
        {
            foreach (int _ticket in ticketLog.Log.Keys)
            {
                Puts("    ");
                Puts("    ");
                Puts("Log Ticket");
                Puts("Accepted: {0}", ticketLog.Log[_ticket].Accepted);
                Puts("CreatedTimeStamp: {0}", ticketLog.Log[_ticket].CreatedTimeStamp);
                Puts("ClosedTimeStamp: {0}", ticketLog.Log[_ticket].ClosedTimeStamp);
                Puts("Username: {0}", ticketLog.Log[_ticket].Username);
                Puts("UserId: {0}", ticketLog.Log[_ticket].UserId);
                Puts("AdminName: {0}", ticketLog.Log[_ticket].AdminName);
                Puts("AdminId: {0}", ticketLog.Log[_ticket].AdminId);
                Puts("Requested: {0}", ticketLog.Log[_ticket].requested);
                Puts("Reason: {0}", ticketLog.Log[_ticket].reason);
            }
        }

        #endregion

        #region Looting Functions
        ItemContainer.CanAcceptResult CanAcceptItem(ItemContainer container, Item item)
        {
            if (container.playerOwner != null)
            {
                BasePlayer _player = container.playerOwner;
                List<Item.OwnerFraction> _itemOwners = item.owners;
                if (_itemOwners == null) return ItemContainer.CanAcceptResult.CanAccept;
                if (_itemOwners.Count < 1) return ItemContainer.CanAcceptResult.CanAccept;
                ulong _ownerID = _itemOwners[0].userid;
                if (_ownerID == 0) return ItemContainer.CanAcceptResult.CanAccept;
                if (isNPC(_ownerID)) return ItemContainer.CanAcceptResult.CanAccept;
                if (SameOnlyCheck(container.playerOwner.userID, _ownerID)) return ItemContainer.CanAcceptResult.CanAccept;
                else
                {
                    LangMSG(container.playerOwner, "notAllwPickup", InfoCache[_ownerID].mode);
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            else return ItemContainer.CanAcceptResult.CanAccept;
        }
        private object CanLootPlayer(BasePlayer _target, BasePlayer _looter)
        {
            if (isNPC(_target)) return canLootNPC(_looter);
            if (isGod(_target)) return null;
            if (areInEvent(_looter, _target)) return null;
            return PvPOnlyCheck(_looter, _target) ? null : (object)false;
        }
        private void OnLootPlayer(BasePlayer _looter, BasePlayer _target)
        {
            if (isNPC(_target)) { npcLootHandle(_looter); return; };
            if (areInEvent(_looter, _target)) return;
            if (PvPOnlyCheck(_looter, _target)) return;
            else NextTick(_looter.EndLooting);
        }
        private void OnLootEntity(BasePlayer _looter, BaseEntity _target)
        {
            if (_target is BaseCorpse)
            {
                var corpse = _target?.GetComponent<PlayerCorpse>() ?? null;
                if (corpse != null)
                {
                    if (isNPC(corpse)) { npcLootHandle(_looter); return; }
                    ulong _corpseID = corpse.playerSteamID;
                    if (_corpseID == _looter.userID) return;
                    BasePlayer _corpseBP = basePlayerByID(_corpseID);
                    if (_corpseBP != null)
                        if (areInEvent(_looter, _corpseBP)) return;
                        else if (PvPOnlyCheck(_looter.userID, _corpseID)) return;
                        else NextTick(_looter.EndLooting);
                }
            }
            else if (_target is StorageContainer)
            {
                StorageContainer _container = (StorageContainer)_target;
                if (_container.OwnerID == 0) return;
                BasePlayer _containerBP = basePlayerByID(_container.OwnerID);
                if (_container.OwnerID == _looter.userID) return;
                if (_containerBP != null)
                    if (areInEvent(_looter, _containerBP)) return;
                if (SameOnlyCheck(_looter.userID, _container.OwnerID)) return;
                else NextTick(_looter.EndLooting);
            }
            else return;
        }
        #endregion

        #region Building Functions
        private List<object> BuildEntityList = new List<object>() {
            typeof(AutoTurret),typeof(Barricade),typeof(BaseCombatEntity),
            typeof(BaseOven),typeof(BearTrap),typeof(BuildingBlock),
            typeof(BuildingPrivlidge),typeof(CeilingLight),typeof(Door),
            typeof(Landmine),typeof(LiquidContainer),typeof(ReactiveTarget),
            typeof(RepairBench),typeof(ResearchTable),typeof(Signage),
            typeof(SimpleBuildingBlock),typeof(SleepingBag),typeof(StabilityEntity),
            typeof(StorageContainer),typeof(SurvivalFishTrap),typeof(WaterCatcher),
            typeof(WaterPurifier)};
        private List<object> BasePartEntityList = new List<object>() {
            typeof(BaseOven),typeof(BuildingBlock),typeof(BuildingPrivlidge),
            typeof(CeilingLight),typeof(Door),typeof(LiquidContainer),
            typeof(RepairBench),typeof(ResearchTable),typeof(Signage),
            typeof(SimpleBuildingBlock),typeof(SleepingBag),typeof(StabilityEntity),
            typeof(StorageContainer),typeof(SurvivalFishTrap),typeof(WaterCatcher),
            typeof(WaterPurifier)};
        private List<object> CombatPartEntityList = new List<object>() {
            typeof(AutoTurret),typeof(Barricade),typeof(BearTrap),typeof(Landmine),
            typeof(ReactiveTarget),typeof(BaseCombatEntity)};



        void OnEntitySpawned(BaseNetworkable _entity)
        {
            if (_entity is BaseEntity)
            {
                BaseEntity _base = (BaseEntity)_entity;
                if (_base.OwnerID == 0) return;
                else if (AdminPlayerMode.Contains(basePlayerByID(_base.OwnerID)))
                    _base.OwnerID = 0;
            }
        }

        #endregion

        #region Compatibility Functions

        [PluginReference]
        Plugin Vanish;
        [PluginReference]
        Plugin Skills;

        bool checkInvis(BasePlayer _player)
        {
            var isInvisible = Vanish?.Call("IsInvisible", _player);
            var isStealthed = Skills?.Call("isStealthed", _player);
            if (isInvisible != null && (bool)isInvisible)
            {
                return true;
            }
            else if (isStealthed != null && (bool)isStealthed)
            {
                return true;
            }
            else return false;
        }

        [PluginReference]
        private Plugin BetterChat;
        void RegisterGroups()
        {
            if (!BetterChat) return;
            if (!GroupExists("PvP"))
            {
                NewGroup("PvP");
                SetGroupTitle("PvP");
                SetGroupColor("PvP");
            }
            if (!GroupExists("PvE"))
            {
                NewGroup("PvE");
                SetGroupTitle("PvE");
                SetGroupColor("PvE");
            }
        }

        void updatePlayerChatTag(BasePlayer _player)
        {
            if (!BetterChat) return;
            ulong _userID = _player.userID;
            string _userIDs = _player.UserIDString;
            string _mode = InfoCache[_userID].mode;
            if (_mode == "pvp")
            {
                if (!(UserInGroup(_userIDs, "PvP"))) AddToGroup(_userIDs, "PvP");
                if (UserInGroup(_userIDs, "PvE")) RemoveFromGroup(_userIDs, "PvE");
            }
            else if (_mode == "pve")
            {
                if (!(UserInGroup(_userIDs, "PvE"))) AddToGroup(_userIDs, "PvE");
                if (UserInGroup(_userIDs, "PvP")) RemoveFromGroup(_userIDs, "PvP");
            }
            else if (_mode == "NA")
            {
                if (UserInGroup(_userIDs, "PvE")) RemoveFromGroup(_userIDs, "PvE");
                if (UserInGroup(_userIDs, "PvP")) RemoveFromGroup(_userIDs, "PvP");
            }
        }

        private bool GroupExists(string name) => (bool)BetterChat?.Call("API_GroupExists", (name.ToLower()));
        private bool NewGroup(string name) => (bool)BetterChat?.Call("API_AddGroup", (name.ToLower()));
        private bool UserInGroup(string ID, string name) => (bool)BetterChat?.Call("API_IsUserInGroup", ID, (name.ToLower()));
        private bool AddToGroup(string ID, string name) => (bool)BetterChat?.Call("API_AddUserToGroup", ID, (name.ToLower()));
        private bool RemoveFromGroup(string ID, string name) => (bool)BetterChat?.Call("API_RemoveUserFromGroup", ID, (name.ToLower()));
        private object SetGroupTitle(string name) => BetterChat?.Call("API_SetGroupSetting", (name.ToLower()), "title", $"[{name}]");
        private object SetGroupColor(string name) => BetterChat?.Call("API_SetGroupSetting", (name.ToLower()), "titlecolor", "orange");

        [PluginReference]
        private Plugin HumanNPC;
        bool isNPC(ulong _test)
        {
            if (HumanNPC == null) return false;
            else if (_test < 76560000000000000L) return true;
            else return false;
        }
        bool isNPC(BasePlayer _test)
        {
            if (HumanNPC == null) return false;
            else if (_test.userID < 76560000000000000L) return true;
            else return false;
        }
        bool isNPC(BaseCombatEntity _player)
        {
            BasePlayer _test = (BasePlayer)_player;
            if (HumanNPC == null) return false;
            else if (_test.userID < 76560000000000000L) return true;
            else return false;
        }
        bool isNPC(PlayerCorpse _test)
        {
            if (HumanNPC == null) return false;
            else if (_test.playerSteamID < 76560000000000000L) return true;
            else return false;
        }

        void npcDamageHandle(BasePlayer _NPC, HitInfo _hitInfo)
        {
            BasePlayer _attacker = (BasePlayer)_hitInfo.Initiator;
            if (isNPC(_attacker)) return;
            if ((InfoCache[_attacker.userID].mode == "pvp") && (PvPAttackNPC == true)) return;
            if ((InfoCache[_attacker.userID].mode == "pve") && (PvEAttackNPC == true)) return;
            else ModifyDamage(_hitInfo, 0);
        }
        void npcAttackHandle(BasePlayer _target, HitInfo _hitInfo)
        {
            if (isNPC(_target)) return;
            if ((InfoCache[_target.userID].mode == "pvp") && (NPCAttackPvP == true)) return;
            if ((InfoCache[_target.userID].mode == "pve") && (NPCAttackPvE == true)) return;
            else ModifyDamage(_hitInfo, 0);
        }

        bool canLootNPC(BasePlayer _player)
        {
            if ((PvELootNPC == true) && (PvPLootNPC == true)) return true;
            else if ((InfoCache[_player.userID].mode == "pvp") && (PvPLootNPC == true)) return true;
            else if ((InfoCache[_player.userID].mode == "pve") && (PvELootNPC == true)) return true;
            else return false;
        }
        void npcLootHandle(BasePlayer _player)
        {
            if ((PvELootNPC == true) && (PvPLootNPC == true)) return;
            if ((InfoCache[_player.userID].mode == "pvp") && (PvPLootNPC == true)) return;
            if ((InfoCache[_player.userID].mode == "pve") && (PvELootNPC == true)) return;
            BroadcastMessageHandle("Not allowed to loot");
            NextTick(_player.EndLooting);
        }

        [PluginReference]
        private Plugin EventManager;
        bool isInEvent(BasePlayer _player1)
        {
            if (EventManager == null) return false;
            bool _var = (bool)EventManager?.Call("isPlaying", _player1);
            if (_var == true) return true;
            return false;
        }

        bool areInEvent(BasePlayer _player1, BasePlayer _player2)
        {
            if (EventManager == null) return false;
            bool _var1 = (bool)EventManager?.Call("isPlaying", _player1);
            bool _var2 = (bool)EventManager?.Call("isPlaying", _player2);
            if (_var1 == true && _var1 == _var2) return true;
            return false;
        }


        [PluginReference]
        private Plugin Godmode;
        private bool checkIsGod(string _player) => (bool)Godmode?.Call("IsGod", _player);

        private bool isGod(ulong _player)
        {
            if (Godmode == null) return false;
            return checkIsGod(_player.ToString());
        }
        private bool isGod(BasePlayer _player)
        {
            if (Godmode == null) return false;
            if (_player == null) return false;
            return checkIsGod(_player.UserIDString);
        }


        #endregion

        #region Door Functions
        void OnDoorOpened(Door _door, BasePlayer _player)
        {
            if (_door == null) return;
            if (_door.OwnerID == 0) return;
            if (!(SameOnlyCheck(_player.userID, _door.OwnerID)))
            {
                _door.SetFlag(BaseEntity.Flags.Open, false);
                _door.SendNetworkUpdateImmediate();
            }
        }
        #endregion

        #region PvX Check/Find Functions
        private bool PvPOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if ((InfoCache[_player1.userID].mode == "pvp") && (InfoCache[_player2.userID].mode == "pvp"))
                return true;
            return false;
        }
        private bool PvPOnlyCheck(ulong _player1, ulong _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if ((InfoCache[_player1].mode == "pvp") && (InfoCache[_player2].mode == "pvp")) return true;
            return false;
        }
        private bool PvEOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if ((InfoCache[_player1.userID].mode == "pve") && (InfoCache[_player2.userID].mode == "pve"))
                return true;
            return false;
        }
        private bool PvEOnlyCheck(ulong _player1, ulong _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if ((InfoCache[_player1].mode == "pve") && (InfoCache[_player2].mode == "pve")) return true;
            return false;
        }
        private bool SameOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if (InfoCache[_player1.userID].mode == InfoCache[_player2.userID].mode) return true;
            return false;
        }
        private bool SameOnlyCheck(ulong _player1, ulong _player2)
        {
            if (isplayerNA(_player1)) return false;
            if (isplayerNA(_player2)) return false;
            if (InfoCache[_player1].mode == InfoCache[_player2].mode) return true;
            return false;
        }

        bool isplayerNA(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (!InfoCache.ContainsKey(_playerID))
            {
                if (_player == null)
                {
                    addOffline(_playerID);
                    BroadcastMessageHandle("Adding offline");
                    SaveAll();
                    return true;
                }
                addPlayer(_player);
                return true;
            }
            if (InfoCache[_playerID].mode == "NA") return true;
            else return false;
        }
        bool isplayerNA(ulong _playerID)
        {
            if (!InfoCache.ContainsKey(_playerID))
            {
                BasePlayer _player = basePlayerByID(_playerID);
                if (_player == null)
                {
                    addOffline(_playerID);
                    BroadcastMessageHandle("Adding offline");
                    SaveAll();
                    return true;
                }
                addPlayer(_player);
                return true;
            }
            if (InfoCache[_playerID].mode == "NA") return true;
            else return false;
        }
        bool isPvP(ulong _playerID)
        {
            if (_playerID == 0 || isNPC(_playerID)) return false;
            BasePlayer _player = basePlayerByID(_playerID);
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pvp") return true;
            else return false;
        }
        bool isPvP(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pvp") return true;
            else return false;
        }
        bool isPvP(BaseCombatEntity _BaseCombat)
        {
            BasePlayer _player = (BasePlayer)_BaseCombat;
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pvp") return true;
            else return false;
        }
        bool isPvE(ulong _playerID)
        {
            if (_playerID == 0 || isNPC(_playerID)) return false;
            BasePlayer _player = basePlayerByID(_playerID);
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }
        bool isPvE(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }
        bool isPvE(BaseCombatEntity _BaseCombat)
        {
            BasePlayer _player = (BasePlayer)_BaseCombat;
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }

        bool BaseplayerCheck(BasePlayer _attacker, BasePlayer _victim)
        {
            if (_attacker == _victim) return true;
            if (isGod(_victim)) return true;
            if (isGod(_attacker)) return true;
            if (areInEvent(_attacker, _victim)) return true;
            return false;
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsDigit(c))
                {
                    //Puts("Character Detected Returning false");
                    return false;
                }
            }
            //Puts("Detected no Characters Returning true");
            return true;
        }
        BasePlayer basePlayerByID(ulong _ID)
        {
            BasePlayer _player = BasePlayer.FindByID(_ID);
            if (_player == null) _player = BasePlayer.FindSleeping(_ID);
            return _player;
        }
        #endregion

        #region Hooks
        public void updatePvXPlayerData(BasePlayer _player)
        {
            playerData.Info[_player.userID].username = _player.displayName;
            playerData.Info[_player.userID].LatestConnection = DateTimeStamp();
        }
        bool isPvEUlong(ulong _playerID)
        {
            if (_playerID == 0 || isNPC(_playerID)) return false;
            BasePlayer _player = basePlayerByID(_playerID);
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }
        bool isPvEBaseplayer(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }
        #endregion

        #region Chat/Console Handles
        [ChatCommand("pvx")]
        void PvXChatCmd(BasePlayer _player, string cmd, string[] args)
        {
            if ((args == null) || (args.Length == 0))
            {
                LangMSG(_player, "ComndList");
                ChatMessageHandle(_player, "/pvx select, /pvx change, /pvx ticket /pvx gui");
                if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx select, /pvx admin");
                return;
            }
            switch (args[0].ToLower())
            {
                case "admin": //meed to transfer accept/dec;ome/list function
                    adminFunction(_player, args);
                    return;
                case "change": //Completed
                    changeFunction(_player);
                    return;
                case "debug":
                    debugFunction();
                    return;
                case "developer":
                    developerFunction();
                    return;
                case "help":
                    helpFunction(_player);
                    return;
                case "select":
                    selectFunction(_player, args);
                    return;
                case "ticket":
                    ticketFunction(_player, args);
                    return;
                case "gui":
                    guiFunction(_player, args);
                    return;
                default:
                    LangMSG(_player, "ComndList");
                    ChatMessageHandle(_player, "/pvx select, /pvx change, /pvx ticket /pvx gui");
                    if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx select, /pvx admin");
                    return;
            }
        }

        [ConsoleCommand("pvx.cmd")]
        void PvXConsoleCmd(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) return;
            if (arg.Args == null || arg.Args.Length == 0) Puts("Hello");
            consolListTickets();
            consolListLog();
        }

        [ConsoleCommand("PvXGuiCMD")]
        void PvXGuiCMD(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 1)return;
            if (arg.Args[0] != "pvp" && arg.Args[0] != "pve")return;
            BasePlayer _player = (BasePlayer)arg.connection.player;
            if (_player == null) return;
            string cmdValue = arg.Args[0];
            if (isplayerNA(_player))
            {
                InfoCache[_player.userID].mode = cmdValue;
                saveCacheData();
                updatePvXIndicator(_player);
                updatePlayerChatTag(_player);
                ChatMessageHandle(_player, "Selected: {0}", cmdValue);
            }
            DestroyPvXUI(_player, pvxPlayerSelectorUI);
        }

        [ChatCommand("pvxhide")]
        void test1(BasePlayer _player, string cmd, string[] args)
        {
            DestroyAllPvXUI(_player);
        }
        [ChatCommand("pvxshow")]
        void test(BasePlayer _player, string cmd, string[] args)
        {
            createPvXIndicator(_player);
            createAdminIndicator(_player);
        }
        #endregion

        #region Chat Functions
        //chat
        void adminFunction(BasePlayer _player, string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx admin [list/accept/decline/display]");
                return;
            }
            string _cmd = args[1].ToLower(); // admin, accept, 1
            if (!(hasPerm(_player, "admin", "MissPerm"))) return;
            if (_cmd == "count") ticketCount(_player);
            if (_cmd == "list") listTickets(_player);
            if (_cmd == "mode") adminMode(_player);
            if ((_cmd == "display") && (args.Length == 3))
            {
                if (IsDigitsOnly(args[2]))
                    displayTicket(_player, Convert.ToInt32(args[2]));
            }
            if ((_cmd == "accept") && (args.Length == 3))
            {
                if ((IsDigitsOnly(args[2])) && (ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    ticketAccept(_player, Convert.ToInt32(args[2]));
                else if (!(ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    LangMSG(_player, "TickNotAvail", args[2]);
                else
                {
                    LangMSG(_player, "IncoFormPleaUse");
                    ChatMessageHandle(_player, "/pvx admin accept #");
                }
            }
            if ((_cmd == "decline") && (args.Length == 3))
            {
                if ((IsDigitsOnly(args[2])) && (ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    ticketDecline(_player, Convert.ToInt32(args[2]));
                else if (!(ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    LangMSG(_player, "TickNotAvail", args[2]);
                else
                {
                    LangMSG(_player, "IncoFormPleaUse");
                    ChatMessageHandle(_player, "/pvx admin decline #");
                }
            }
        }
        void changeFunction(BasePlayer _player)
        {
            if (playerHasTicket(_player) == true)
            {
                ChatMessageHandle(_player, "AlreadySubmitted"); return;
            }
            else if (isplayerNA(_player))
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx select [pvp/pve]");
                return;
            }
            else if (isPvP(_player)) createTicket(_player, "pve");
            else if (isPvE(_player)) createTicket(_player, "pvp");
            else PutsPlayerHandle(_player, "Error: 27Q1 - Please inform Dev");
            return;
        }
        void selectFunction(BasePlayer _player, string[] args)
        {
            if ((args.Length != 2) && (args[1] != "pve") && (args[1] != "pvp"))
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx select [pvp/pve]");
            }
            else if (InfoCache[_player.userID].mode == "NA")
            {
                InfoCache[_player.userID].mode = args[1].ToLower();
                saveCacheData();
                updatePvXIndicator(_player);
            }
        }
        void ticketFunction(BasePlayer _player, string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx ticket cancel");
                ChatMessageHandle(_player, "/pvx ticket reason ''reason on ticket''");
                return;
            }
            string _cmd = args[1].ToLower();
            if (InfoCache[_player.userID].ticket == false)
            {
                LangMSG(_player, "NoActTick");
                return;
            }
            if (_cmd == "cancel")
            {
                cancelTicket(_player);
            }
            if ((_cmd == "reason") && (args.Length == 3))
            {
                ticketData.Info[_player.userID].reason = args[2];
                LangMSG(_player, "RSNChan");
                ChatMessageHandle(_player, args[2]);
                SaveAll();
                return;
            }
        }
        void guiFunction(BasePlayer _player, string[] args)
        {
            if (args.Length == 1)
            {
                LangMSG(_player, "ComndList");
                ChatMessageHandle(_player, "/pvx gui pvx on/off");
                if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx gui admin on/off");
                return;
            }
            if (!(args.Length == 3)) return;
            if ((args[1].ToLower() == "admin") && (hasPerm(_player, "admin")))
            {
                if (args[2].ToLower() == "on") createAdminIndicator(_player);
                else if (args[2].ToLower() == "off") DestroyPvXUI(_player, pvxAdminUI);
                return;
            }
            else if (args[1].ToLower() == "pvx")
            {
                if (args[2].ToLower() == "on") createPvXIndicator(_player);
                else if (args[2].ToLower() == "off") DestroyPvXUI(_player, pvxPlayerUI);
                return;
            }
            return;
        }
        void debugFunction()
        { }
        void developerFunction()
        { }
        void helpFunction(BasePlayer _player)
        {
            ChatMessageHandle(_player, "Plugin: PvX");
            ChatMessageHandle(_player, "Description: {0}", Description); 
            ChatMessageHandle(_player, "Version {0}", Version);
            ChatMessageHandle(_player, "Mod Developer: Alphawar");
        }

        //console

        #endregion


        #region OnEntityTakeDamage
        void OnEntityTakeDamage(BaseCombatEntity _target, HitInfo hitinfo)
        {
            BaseEntity _attacker = hitinfo.Initiator;
            object _n = _target.GetType();

            /*
            if (_target is BasePlayer && 1 == 1){
                BasePlayer _test = (BasePlayer)_target;
                if (_test.userID == 76561198006265515) testvar(_target, hitinfo);}
            else if (BuildEntityList.Contains(_n) && 1 == 1){
                if (_target.OwnerID == 76561198006265515) testvar(_target, hitinfo);}
            */

            if (_attacker is BasePlayer && _target is BasePlayer) PlayerVPlayer((BasePlayer)_target, (BasePlayer)_attacker, hitinfo);                               //Player V Player
            else if (_attacker is BasePlayer && BuildEntityList.Contains(_n) && !(_n is AutoTurret)) PlayerVBuilding(_target, (BasePlayer)_attacker, hitinfo);      //Player V Building

            else if (_attacker is BasePlayer && _target is BaseHelicopter) PlayerVHeli((BasePlayer)_attacker, hitinfo);                                             //Player V Heli
            else if ((_attacker is BaseHelicopter||(_attacker is FireBall && _attacker.ShortPrefabName == "napalm")) && _target is BasePlayer) HeliVPlayer((BasePlayer)_target, hitinfo);
            else if ((_attacker is BaseHelicopter || (_attacker is FireBall && _attacker.ShortPrefabName == "napalm")) && BuildEntityList.Contains(_n)) HeliVBuilding(_target, hitinfo);
            else if ((_attacker is BaseHelicopter || (_attacker is FireBall && _attacker.ShortPrefabName == "napalm")) && _target is BaseNPC) HeliVAnimal((BaseNPC)_target, hitinfo);
            

            else if (_attacker is BasePlayer && _target is AutoTurret) PlayerVTurret((AutoTurret)_target, (BasePlayer)_attacker, hitinfo);                          //Player V Turret
            else if (_attacker is AutoTurret && _target is BasePlayer) TurretVPlayer((BasePlayer)_target, (AutoTurret)_attacker, hitinfo);                          //Turret V Player
            else if (_attacker is AutoTurret && _target is AutoTurret) TurretVTurret((AutoTurret)_target, (AutoTurret)_attacker, hitinfo);                          //Turret V Turret
            else if (_attacker is AutoTurret && _target is BaseNPC) TurretVAnimal((BaseNPC)_target, (AutoTurret)_attacker, hitinfo);                                //Turret V Animal

            else if (_attacker is BasePlayer && _target is BaseNPC) PlayerVAnimal((BasePlayer)_attacker, hitinfo);                                                  //Player V Animal
            else if (_attacker is BaseNPC && _target is BasePlayer) AnimalVPlayer((BasePlayer)_target, hitinfo);
            else if (_attacker is FireBall)
            {
                FireBall _fire = (FireBall)_attacker;
                if (_target is BasePlayer) FireVPlayer((BasePlayer)_target, hitinfo);
                else if (BuildEntityList.Contains(_n)) FireVBuilding(_target, hitinfo);
            }

            
            //if (hitinfo.Initiator is BaseTrap)
            //if (hitinfo.Initiator is Barricade)
            //if (hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli" ||
            //hitinfo.WeaponPrefab.ShortPrefabName == "rocket_heli_napalm")
            //if (hitinfo.Initiator != null && hitinfo.Initiator.ShortPrefabName == "napalm")
        }

        void testvar(BaseCombatEntity _target, HitInfo hitinfo)
        {
            //Type typeInformation = hitinfo.Initiator.GetType();
            //BaseHelicopter
            //_attacker is FireBall && _attacker.ShortPrefabName = fireball_small
        }
        void PlayerVPlayer(BasePlayer _victim, BasePlayer _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling PvP");
            if (BaseplayerCheck(_attacker, _victim)) return;
            if (isNPC(_attacker))
            {
                if (isNPC(_victim)) return;
                else if (isPvE(_victim) && NPCAttackPvE) ModifyDamage(_hitinfo, NPCDamagePvE);
                else if (isPvP(_victim) && NPCAttackPvP) ModifyDamage(_hitinfo, NPCDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            else if (isPvE(_attacker))
            {
                if (isNPC(_victim)) if (PvEAttackNPC) ModifyDamage(_hitinfo, PvEDamageNPC); else ModifyDamage(_hitinfo, 0);
                else if (isPvE(_victim) && PvEAttackPvE) ModifyDamage(_hitinfo, PvEDamagePvE);
                else if (isPvP(_victim) && PvEAttackPvP) ModifyDamage(_hitinfo, PvEDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            else if (isPvP(_attacker))
            {
                if (isNPC(_victim)) if (PvPAttackNPC) ModifyDamage(_hitinfo, PvPDamageNPC); else ModifyDamage(_hitinfo, 0);
                else if (isPvE(_victim) && PvPAttackPvE) ModifyDamage(_hitinfo, PvPDamagePvE);
                else if (isPvP(_victim) && PvPAttackPvP) ModifyDamage(_hitinfo, PvPDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            if (InfoCache[_victim.userID].mode == "pve")
            {
                if (!antiChatSpam.Contains(_attacker.userID))
                {
                    antiChatSpam.Add(_attacker.userID);
                    timer.Once(2f, () => antiChatSpam.Remove(_attacker.userID));
                    LangMSG(_attacker, lang.GetMessage("PvETarget", this, _attacker.UserIDString));
                }
                _victim.EndLooting();
            }
            //if (_victim.userID == 76561198006265515)
            //{
            //    Puts("AttackerBP: {0}", _attacker);
            //    Puts("VARE: {0}", hitinfo.Initiator);
            //    Puts("VARE: {0}", hitinfo.InitiatorPlayer);
            //}
            return;
        }
        void PlayerVBuilding(BaseEntity _target, BasePlayer _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling PvB");
            ulong _victim = _target.OwnerID;
            if (_target.OwnerID == 0) return;
            if (isInEvent(_attacker)) return;
            if (_target.OwnerID == _attacker.userID) return;
            if (isGod(_target.OwnerID)) return;
            if (isGod(_attacker)) return;
            if (isNPC(_attacker))
            {
                if (isNPC(_victim)) return;
                else if (isPvE(_victim) && NPCAttackPvE) ModifyDamage(_hitinfo, NPCDamagePvE);
                else if (isPvP(_victim) && NPCAttackPvP) ModifyDamage(_hitinfo, NPCDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            else if (isPvE(_attacker))
            {
                if (isNPC(_victim)) if (PvEAttackNPC) ModifyDamage(_hitinfo, PvEDamageNPC); else ModifyDamage(_hitinfo, 0);
                else if (areInEvent(_attacker, _attacker)) return;
                else if (isPvE(_victim) && PvEAttackPvE) ModifyDamage(_hitinfo, PvEDamagePvE);
                else if (isPvP(_victim) && PvEAttackPvP) ModifyDamage(_hitinfo, PvEDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            else if (isPvP(_attacker))
            {
                if (isNPC(_victim)) if (PvPAttackNPC) ModifyDamage(_hitinfo, PvPDamageNPC); else ModifyDamage(_hitinfo, 0);
                else if (areInEvent(_attacker, _attacker)) return;
                else if (isPvE(_victim) && PvPAttackPvE) ModifyDamage(_hitinfo, PvPDamagePvE);
                else if (isPvP(_victim) && PvPAttackPvP) ModifyDamage(_hitinfo, PvPDamagePvP);
                else ModifyDamage(_hitinfo, 0);
            }
            if (InfoCache[_victim].mode == "pve")
            {
                if (!antiChatSpam.Contains(_attacker.userID))
                {
                    antiChatSpam.Add(_attacker.userID);
                    timer.Once(2f, () => antiChatSpam.Remove(_attacker.userID));
                    LangMSG(_attacker, lang.GetMessage("PvETarget", this, _attacker.UserIDString));
                }
            }
        }

        void PlayerVHeli(BasePlayer _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling PvH");
            if (isNPC(_attacker)) return;
            else if (isGod(_attacker)) return;
            else if (isInEvent(_attacker)) return;
            else if (isPvE(_attacker) && HeliTargetPvE) ModifyDamage(_hitinfo, HeliDamageByPvE);
            else if (isPvP(_attacker) && HeliTargetPvP) ModifyDamage(_hitinfo, HeliDamageByPvP);
            else ModifyDamage(_hitinfo, 0);
        }
        void HeliVPlayer(BasePlayer _victim, HitInfo _hitinfo)
        {
            Puts("Calling HvP");
            if (isNPC(_victim)) return;
            else if (isGod(_victim)) return;
            else if (isInEvent(_victim)) return;
            else if (isPvE(_victim) && HeliTargetPvE) ModifyDamage(_hitinfo, HeliDamagePvE);
            else if (isPvP(_victim) && HeliTargetPvP) ModifyDamage(_hitinfo, HeliDamagePvP);
            else ModifyDamage(_hitinfo, 0);
        }
        void HeliVBuilding(BaseEntity _target, HitInfo _hitinfo)
        {
            //Puts("Calling HvB");
            ulong _ownerID = _target.OwnerID;
            if (isNPC(_ownerID)) return;
            else if (isGod(_ownerID)) return;
            else if (isPvE(_ownerID) && HeliTargetPvE) ModifyDamage(_hitinfo, HeliDamagePvEStruct);
            else if (isPvP(_ownerID) && HeliTargetPvP) ModifyDamage(_hitinfo, HeliDamagePvPStruct);
            else ModifyDamage(_hitinfo, 0);
        }
        void HeliVAnimal(BaseNPC _target, HitInfo _hitinfo)
        {
            //Puts("Calling HvA");
            ModifyDamage(_hitinfo, HeliDamageAnimal);
        }

        void PlayerVTurret(AutoTurret _target, BasePlayer _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling PvT");
            ulong _ownerID = _target.OwnerID;
            if (isGod(_attacker)) return;
            else if (isInEvent(_attacker)) return;
            else if (isNPC(_attacker) && isPvE(_ownerID)) ModifyDamage(_hitinfo, TurretPvEDamageNPCAmnt);
            else if (isNPC(_attacker) && isPvP(_ownerID)) ModifyDamage(_hitinfo, TurretPvPDamageNPCAmnt);
            else if (isPvE(_attacker) && isPvE(_ownerID)) ModifyDamage(_hitinfo, TurretPvEDamagePvEAmnt);
            else if (isPvE(_attacker) && isPvP(_ownerID)) ModifyDamage(_hitinfo, TurretPvEDamagePvPAmnt);
            else if (isPvP(_attacker) && isPvE(_ownerID)) ModifyDamage(_hitinfo, TurretPvPDamagePvEAmnt);
            else if (isPvP(_attacker) && isPvP(_ownerID)) ModifyDamage(_hitinfo, TurretPvPDamagePvPAmnt);
            else ModifyDamage(_hitinfo, 0);
        }
        void TurretVPlayer(BasePlayer _target, AutoTurret _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling TvP");
            ulong _attackerID = _attacker.OwnerID;
            if (isGod(_target)) return;
            else if (isInEvent(_target)) return;
            else if (isPvE(_attackerID) && isNPC(_target)) ModifyDamage(_hitinfo, TurretPvEDamageNPCAmnt);
            else if (isPvP(_attackerID) && isNPC(_target)) ModifyDamage(_hitinfo, TurretPvPDamageNPCAmnt);
            else if (isPvE(_attackerID) && isPvE(_target)) ModifyDamage(_hitinfo, TurretPvEDamagePvEAmnt);
            else if (isPvE(_attackerID) && isPvP(_target)) ModifyDamage(_hitinfo, TurretPvEDamagePvPAmnt);
            else if (isPvP(_attackerID) && isPvE(_target)) ModifyDamage(_hitinfo, TurretPvPDamagePvEAmnt);
            else if (isPvP(_attackerID) && isPvP(_target)) ModifyDamage(_hitinfo, TurretPvPDamagePvPAmnt);
            else ModifyDamage(_hitinfo, 0);
        }
        void TurretVTurret(AutoTurret _target, AutoTurret _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling TvT");
            ulong _targetID = _target.OwnerID;
            ulong _attackerID = _target.OwnerID;
            if (isPvE(_attackerID) && isPvE(_targetID)) ModifyDamage(_hitinfo, TurretPvEDamagePvEAmnt);
            else if (isPvE(_attackerID) && isPvP(_targetID)) ModifyDamage(_hitinfo, TurretPvEDamagePvPAmnt);
            else if (isPvP(_attackerID) && isPvE(_targetID)) ModifyDamage(_hitinfo, TurretPvPDamagePvEAmnt);
            else if (isPvP(_attackerID) && isPvP(_targetID)) ModifyDamage(_hitinfo, TurretPvPDamagePvPAmnt);
            else ModifyDamage(_hitinfo, 0);
        }
        void TurretVAnimal(BaseNPC _target, AutoTurret _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling TvA");
            ulong _turretOwner = _attacker.OwnerID;
            if (isPvE(_turretOwner) && TurretPvETargetAnimal) ModifyDamage(_hitinfo, TurretPvEDamageAnimalAmnt);
            else if (isPvP(_turretOwner) && TurretPvPTargetAnimal) ModifyDamage(_hitinfo, TurretPvPDamageAnimalAmnt);
            else ModifyDamage(_hitinfo, 0);
        }

        void PlayerVAnimal(BasePlayer _attacker, HitInfo _hitinfo)
        {
            //Puts("Calling PvA");
            if (isGod(_attacker)) return;
            else if (isInEvent(_attacker)) return;
            else if (isNPC(_attacker)) ModifyDamage(_hitinfo, NPCDamageAnimals);
            else if (isPvE(_attacker)) ModifyDamage(_hitinfo, PvEDamageAnimals);
            else if (isPvP(_attacker)) ModifyDamage(_hitinfo, PvPDamageAnimals);
            else ModifyDamage(_hitinfo, 0);
        }
        void AnimalVPlayer(BasePlayer _target, HitInfo _hitinfo)
        {
            //Puts("Calling AvP");
            if (isGod(_target)) return;
            else if (isInEvent(_target)) return;
            else if (isNPC(_target)) ModifyDamage(_hitinfo, AnimalsDamageNPC);
            else if (isPvE(_target)) ModifyDamage(_hitinfo, AnimalsDamagePvE);
            else if (isPvP(_target)) ModifyDamage(_hitinfo, AnimalsDamagePvP);
            else if (isplayerNA(_target)) ModifyDamage(_hitinfo, 1);
            else ModifyDamage(_hitinfo, 0);
        }

        void FireVPlayer(BasePlayer _target, HitInfo _hitinfo)
        {
            if (isNPC(_target)) return;
            else if (isGod(_target)) return;
            else if (isInEvent(_target)) return;
            else if (isPvE(_target)) ModifyDamage(_hitinfo, FireDamagePvE);
            else if (isPvP(_target)) ModifyDamage(_hitinfo, FireDamagePvP);
            else ModifyDamage(_hitinfo, 0);
        }
        void FireVBuilding(BaseEntity _target, HitInfo _hitinfo)
        {
            Puts("Calling FvB");
            if (isPvE(_target.OwnerID)) ModifyDamage(_hitinfo, FireDamagePvEStruc);
            else if (isPvP(_target.OwnerID)) ModifyDamage(_hitinfo, FireDamagePvPStruc);
            else ModifyDamage(_hitinfo, 0);
        }
        #endregion

        #region CanBeTargeted
        private object CanBeTargeted(BaseCombatEntity _target, MonoBehaviour turret)
        {
            if (turret is HelicopterTurret && _target is BasePlayer && HeliTargetPlayer((BasePlayer)_target)) return null;
            else if (turret is AutoTurret && _target is BasePlayer && TurretTargetPlayer((BasePlayer)_target, (AutoTurret)turret)) return null;
            else if (turret is AutoTurret && _target is BaseNPC && TurretTargetAnimals((BaseNPC)_target, (AutoTurret)turret)) return null;
            else return false;
        }

        bool HeliTargetPlayer(BasePlayer _target)
        {
            if (isNPC(_target) && HeliTargetNPC) return true;
            else if (checkInvis(_target)) return true;
            else if (isPvE(_target) && HeliTargetPvE) return true;
            else if (isPvP(_target) && HeliTargetPvP) return true;
            return false;
        }
        bool TurretTargetPlayer(BasePlayer _target, AutoTurret _attacker)
        {
            ulong _OwnerID = _attacker.OwnerID;
            if (!isNPC(_target) && checkInvis(_target)) return true;
            else if (isPvE(_OwnerID) && isNPC(_target) && TurretPvETargetNPC) return true;
            else if (isPvE(_OwnerID) && isPvE(_target) && TurretPvETargetPvE) return true;
            else if (isPvE(_OwnerID) && isPvP(_target) && TurretPvETargetPvP) return true;
            else if (isPvP(_OwnerID) && isNPC(_target) && TurretPvPTargetNPC) return true;
            else if (isPvP(_OwnerID) && isPvE(_target) && TurretPvPTargetPvE) return true;
            else if (isPvP(_OwnerID) && isPvP(_target) && TurretPvPTargetPvP) return true;
            return false;
        }
        bool TurretTargetAnimals(BaseNPC _target, AutoTurret _attacker)
        {
            ulong _OwnerID = _attacker.OwnerID;
            if (isPvE(_OwnerID) && TurretPvETargetAnimal) return true;
            if (isPvP(_OwnerID) && TurretPvPTargetAnimal) return true;
            return false;
        }
        #endregion

        //void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        //{
        //    if(trigger is BuildPrivilegeTrigger)
        //        trigger.
        //    Puts("OnEntityEnter works!");
        //}
        //void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        //{
        //    Puts("OnEntityLeave works!");
        //}

        void adminMode(BasePlayer _player)
        {
            if (AdminPlayerMode.Contains(_player))
            {
                LangMSG(_player, "AdmModeRem");
                AdminPlayerMode.Remove(_player);
                updatePvXIndicator(_player);
                return;
            }
            else AdminPlayerMode.Add(_player);
            LangMSG(_player, "AdmModeAdd");
            updatePvXIndicator(_player);
        }
        bool isInAdminMode(BasePlayer _player)
        {
            if (AdminPlayerMode.Contains(_player)) return true;
            return false;
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            //Puts("Container is type {0}", container.GetType());
            //Puts("Container is type {0}", container.entityOwner);
            //Puts("Container is type {0}", container.playerOwner);
            if (container.entityOwner != null) return;
            if (container.playerOwner != null)
            {
                BasePlayer _player = container.playerOwner;
                if (isInAdminMode(_player)) item.ClearOwners();
            }
        }


        



        void ModifyDamage(HitInfo hitinfo, float scale)
        {
            if (scale == 0f)
            {
                hitinfo.damageTypes = new DamageTypeList();
                hitinfo.DoHitEffects = false;
                hitinfo.HitMaterial = 0;
                hitinfo.PointStart = Vector3.zero;
                hitinfo.PointEnd = Vector3.zero;
            }
            else if (scale == 1) return;
            else
            {
                //Puts("Modify Damabe by: {0}", scale);
                hitinfo.damageTypes.ScaleAll(scale);
            }
        }

        string DateTimeStamp()
        {
            return DateTime.Now.ToString("HH:mm dd-MM-yyyy");
        }
        double GetTimeStamp()
        {
            return (DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Debug /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        int DebugLevel = 0;
        void DebugMessage(int _minDebuglvl, string _msg)
        {
            if (DebugLevel >= _minDebuglvl)
            {
                Puts(_msg);
                if (DebugLevel == 3 && _minDebuglvl == 1)
                {
                    PrintToChat(_msg);
                }
            }
        }
        void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            //if (metabolism.bleeding.GetType)
            //if (metabolism.heartrate) return;
            //if (metabolism.hydration) return;
            //if (metabolism.calories) return;
        }
    }
}

//Ticket accepted should be fixed for offline/dead players, now add update mechanism on playerinit
// config color + opacity
// Fix up/Shorten hooks eg: 

