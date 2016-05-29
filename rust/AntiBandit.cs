using Oxide.Core.Plugins;
using Rust;
using System;                      //DateTime
using System.Collections.Generic;  //Required for Whilelist
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("AntiBandit", "Alphawar", "0.9.1", ResourceId = 1879)]
    [Description("Plugin designed to assist servers with RDM (designed for RPG servers)")]
    class AntiBandit : RustPlugin
    {
        [PluginReference]
        Plugin RustIOFriendListAPI;


        private List<Timer> tTimers = new List<Timer>();
        private Hash<ulong, double> PlayerCooldownList = new Hash<ulong, double>();
        private List<Vector3> ExpiredPvPZonesList = new List<Vector3>();
        private List<Vector3> ExpiredRaidZonesList = new List<Vector3>();
        private Hash<Vector3, PVPData> PvPList = new Hash<Vector3, PVPData>();
        private Hash<Vector3, RaidDetails> raidZoneList = new Hash<Vector3, RaidDetails>();

        void OnPlayerInit(BasePlayer _player)
        {
            if (_player == null) return;
            if (ReceivingSnapshotcheck(_player) == true) return;
            createPlayerCooldown(_player);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (purgeMode == true) return;
            if (entity == null) return;
            if (((entity is BuildingBlock) || (entity is Door)) && (hitinfo.Initiator is BasePlayer))
            {
                BasePlayer attacker = (BasePlayer)hitinfo.Initiator;
                bool ownBuildingDamage = checkBuildingOwner(attacker, entity);
                if (ownBuildingDamage == true) return;
                DebugMessage(2, "Entity Taken Damage, Entity is building");
                if (raidZoneList.Count > 0)
                {
                    DebugMessage(2, "There are active raid zones");
                    foreach (var entry in raidZoneList)
                    {
                        bool testVar1 = zoneCooldownCheck("raid", entry.Key);
                        bool testVar2 = checkEntityInZone(entity, entry.Key, RaidZone2);
                        bool testVar3 = checkPlayerInZone(attacker, entry.Key, RaidZone2);
                        if ((testVar1 == true) && (testVar2 == true) && (testVar3 == true))
                        {
                            DebugMessage(2, "Cooldown Passed, Entity & Player In Zone");
                            return;
                        }
                        else
                        {
                            DebugMessage(2, "Not all in zone");
                            DebugMessage(2, string.Format("Entity: {0}. Player: {1}", testVar2, testVar3));
                        }
                    }
                    DebugMessage(1, "Nullifying damage");
                    NullifyDamage(hitinfo);
                }
                else
                {
                    DebugMessage(2, "No raid zones are active");
                    DebugMessage(1, "Nullifying damage");
                    NullifyDamage(hitinfo);
                }
            }
            else if (entity is BasePlayer && hitinfo.Initiator is BasePlayer) // Checks that hitinfo is a player if so continues
            {
                if (purgeMode == true) return;
                DebugMessage(2, "Entity Taken Damage, Entity is player");
                BasePlayer victim = (BasePlayer)entity;
                BasePlayer attacker = (BasePlayer)hitinfo.Initiator;
                if (attacker == victim) return;
                DebugMessage(2, "saving Baseplayers");
                if (raidZoneList.Count > 0)
                {
                    DebugMessage(2, "There are active raid zones");
                    foreach (var entry in raidZoneList)
                    {
                        bool testVar1 = zoneCooldownCheck("raid", entry.Key);
                        bool testVar2 = checkPlayerInZone(attacker, entry.Key, RaidZone2);
                        bool testVar3 = checkPlayerInZone(victim, entry.Key, RaidZone2);
                        if ((testVar1 == true) && (testVar2 == true) && (testVar3 == true))
                        {
                            DebugMessage(2, "Cooldown Passed, Players In Zone");
                            return;
                        }
                        else
                        {
                            DebugMessage(2, "Not all test variables where met for raid zone");
                            DebugMessage(2, string.Format("testVar1: {0}, testVar1: {1}, testVar1: {2}", testVar1, testVar2, testVar3));
                        }
                    }
                    DebugMessage(1, "Players did not pass the Raid Zone Check");
                }
                else DebugMessage(2, "No Active Raid zones");
                if (PvPList.Count > 0)
                {
                    DebugMessage(2, "There are active PvP zones");
                    foreach (var entry in PvPList)
                    {
                        bool testVar1 = zoneCooldownCheck("pvp", entry.Key);
                        bool testVar2 = checkPlayerInZone(attacker, entry.Key, PVPZone3);
                        bool testVar3 = checkPlayerInZone(victim, entry.Key, PVPZone3);
                        bool testVar4 = checkPlayerRegistered(attacker, entry.Key);
                        bool testVar5 = checkPlayerRegistered(victim, entry.Key);
                        if ((testVar1 == true) && (testVar2 == true) && (testVar3 == true) && (testVar4 == true) && (testVar5 == true))
                        {
                            DebugMessage(2, "Cooldown Passed, Players In Zone, Players Registered");
                            return;
                        }
                        else
                        {
                            DebugMessage(2, "Not all test variables where met for pvp zone");
                            DebugMessage(2, string.Format("testVar1: {0}, testVar1: {1}, testVar1: {2}, testVar1: {3}, testVar1: {4}", testVar1, testVar2, testVar3, testVar4, testVar5));
                        }
                    }
                }
                else DebugMessage(2, "No Active PvP zones");
                DebugMessage(2, "Raid and PvP Zone conditions where not meet");
                DebugMessage(1, "Nullifying damage");
                NullifyDamage(hitinfo);
            }
            return;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if ((entity is BuildingBlock) || (entity is Door))
            {
                DebugMessage(2, "On Entity Death, Entity is building");
                if (raidZoneList.Count > 0)
                {
                    DebugMessage(2, "There are active raid zones");
                    foreach (var entry in raidZoneList)
                    {
                        DebugMessage(2, "This zone is active, Checking if InZone");
                        bool testVar1 = checkEntityInZone(entity, entry.Key, RaidZone2);
                        if (testVar1 == true)
                        {
                            DebugMessage(2, "Resetting timer for a zone");
                            raidZoneList[entry.Key].RaidEndTimer = (GetTimeStamp() + RaidTimeLimit);
                        }
                        else
                        {
                            DebugMessage(2, "Not Resetting timer");
                        }
                    }
                }
            }
            else if (entity is BasePlayer)
            {
                BasePlayer victim = (BasePlayer)entity;
                DebugMessage(2, "On Entity Death, Entity is Player");
                if (raidZoneList.Count > 0)
                {
                    DebugMessage(2, "There are active raid zones");
                    foreach (var entry in raidZoneList)
                    {
                        DebugMessage(2, "This zone is active, Checking if InZone");
                        bool testVar1 = checkPlayerInZone(victim, entry.Key, RaidZone2);
                        if (testVar1 == true)
                        {
                            DebugMessage(2, "Resetting timer for a zone");
                            raidZoneList[entry.Key].RaidEndTimer = (GetTimeStamp() + RaidTimeLimit);
                        }
                        else
                        {
                            DebugMessage(2, "Not Resetting timer");
                        }
                    }
                }
                if (PvPList.Count > 0)
                {
                    DebugMessage(2, "There are active PvP zones");
                    foreach (var entry in raidZoneList)
                    {
                        DebugMessage(2, "This zone is active, Checking if InZone");
                        bool testVar1 = checkPlayerInZone(victim, entry.Key, RaidZone2);
                        if (testVar1 == true)
                        {
                            DebugMessage(2, "Resetting timer for a zone");
                            PvPList[entry.Key].PvPEndTimer = (GetTimeStamp() + PvPTimeLimit);
                        }
                        else
                        {
                            DebugMessage(2, "Not Resetting timer");
                        }
                    }
                }
            }
            else
            {
            }
        }



        [ChatCommand("antibandit")]
        void chatSettings(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAllowed(player, "antibandit.admin")){
                ChatMessageHandler(player, lang.GetMessage("MissingPermission", this, player.UserIDString), "admin");
                return;}
            if (args == null || args.Length == 0)
            {
                ChatMessageHandler(player, lang.GetMessage("MissingAdminCmD", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "debug":
                    if (!IsAllowed(player, "antibandit.debug")){
                        ChatMessageHandler(player, lang.GetMessage("MissingPermission", this, player.UserIDString), "debug");
                        return;}
                    if (args.Length == 1)
                    {
                        ChatMessageHandler(player, lang.GetMessage("DebugIncorrectCmD", this, player.UserIDString));
                        return;
                    }
                    if (args[1] == "0")
                    {
                        DebugLevel = 0;
                        ChatMessageHandler(player, lang.GetMessage("DebugMode", this, player.UserIDString), "0");
                        return;
                    }
                    else if (args[1] == "1")
                    {
                        DebugLevel = 1;
                        ChatMessageHandler(player, lang.GetMessage("DebugMode", this, player.UserIDString), "1");
                        return;
                    }
                    else if (args[1] == "2")
                    {
                        DebugLevel = 2;
                        ChatMessageHandler(player, lang.GetMessage("DebugMode", this, player.UserIDString), "2");
                        return;
                    }
                    else if (args[1] == "3")
                    {
                        DebugLevel = 3;
                        ChatMessageHandler(player, lang.GetMessage("DebugMode", this, player.UserIDString), "3");
                        return;
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("DebugIncorrectCmD", this, player.UserIDString));
                        return;
                    }
                case "purge":
                    if (args.Length == 1)
                    {
                        ChatMessageHandler(player, lang.GetMessage("PurgeInccorectCmd", this, player.UserIDString));
                        return;
                    }
                    if (args[1] == "true")
                    {
                        ChatMessageHandler(player, lang.GetMessage("PurgeModeOn", this, player.UserIDString));
                        purgeMode = true;
                        return;
                    }
                    else if (args[1] == "false")
                    {
                        ChatMessageHandler(player, lang.GetMessage("PurgeModeOff", this, player.UserIDString));
                        purgeMode = false;
                        return;
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("PurgeInccorectCmd", this, player.UserIDString));
                        return;
                    }
                default:
                    ChatMessageHandler(player, lang.GetMessage("InvalidCmD", this, player.UserIDString));
                    return;
            }
        }

        [ChatCommand("raid")]
        void raidChatHandle(BasePlayer _player)
        {
            double RaidInitData = (GetTimeStamp() + RaidDelay);
            double RaidEndData = (GetTimeStamp() + RaidTimeLimit + RaidDelay);
            if (!IsAllowed(_player, "antibandit.player")){
                ChatMessageHandler(_player, lang.GetMessage("MissingPermission", this, _player.UserIDString), "player");
                return;}
            createPlayerCooldown(_player);
            bool _testVar1 = checkServerPvPCooldown();
            bool _testVar2 = checkPlayerCooldown(_player);
            if ((_testVar1 == true) && (_testVar2 == true))
            {
                Vector3 PosHashValue = (_player.transform.position);
                bool _testVar3 = GetNearbyTargetWall(PosHashValue, _player);
                if (_testVar3 == true)
                {
                    BroadcastMessageHandler(lang.GetMessage("XhasCreatedZone", this, _player.UserIDString), _player.displayName);
                    timer.Once(PvPDelay, () =>
                    {
                        BroadcastMessageHandler(lang.GetMessage("RaidZoneActive", this, _player.UserIDString));
                });
                    raidZoneList.Add(PosHashValue, new RaidDetails { RaidInitCooldown = RaidInitData, RaidEndTimer = RaidEndData });
                    serverRaidCooldownTimeStamp = GetTimeStamp() + serverRaidCooldown;
                    PlayerCooldownList[_player.userID] = GetTimeStamp() + playerCooldown;
                    DebugMessage(1, "Zone Created");
                }
            }
        }

        [ChatCommand("pvp")]
        void pvpChatHandle(BasePlayer _player, string cmd, string[] args)
        {
            if (!IsAllowed(_player, "antibandit.player")){
                ChatMessageHandler(_player, lang.GetMessage("MissingPermission", this, _player.UserIDString), "player");
                return;}
            createPlayerCooldown(_player);
            bool _testVar1 = checkServerPvPCooldown();
            bool _testVar2 = checkPlayerCooldown(_player);
            if ((_testVar1 == true) && (_testVar2 == true))
            {
                Vector3 _PosHashValue = (_player.transform.position);
                bool _testVar3 = checkNearPlayerNotFriend(_player, _PosHashValue);
                if (_testVar3 == true)
                {
                    serverPvPCooldownTimeStamp = GetTimeStamp() + serverPvPCooldown;
                    PlayerCooldownList[_player.userID] = GetTimeStamp() + playerCooldown;
                    double PvPInitData = (GetTimeStamp() + PvPDelay);
                    double PvPEndData = (GetTimeStamp() + PvPTimeLimit + PvPDelay);
                    PvPList.Add(_PosHashValue, new PVPData { PvPInitCooldown = PvPInitData, PvPEndTimer = PvPEndData });
                    registerNearPlayers(_PosHashValue);
                }
            }
        }

        [ConsoleCommand("AntiBandit.Purge")]
        void purgeToggle(ConsoleSystem.Arg arg)
        {
            if (!arg.isAdmin) return;
            if (arg.Args[0].ToLower() == "true") purgeMode = true;
            else if (arg.Args[0].ToLower() == "false") purgeMode = false;
            else Puts(lang.GetMessage("PurgeInccorectConsole", this));
        }

        bool checkPlayerCooldown(BasePlayer _player)
        {
            if ((PlayerCooldownList[_player.userID] < GetTimeStamp()) || (IsAllowed(_player, "antibandit.nocooldown")))
            {
                return true;
            }
            return false;
        }

        bool checkServerPvPCooldown()
        {
            if (serverPvPCooldownTimeStamp < GetTimeStamp())
            {
                return true;
            }
            return false;
        }
        bool checkServerRaidCooldown()
        {
            if (serverRaidCooldownTimeStamp < GetTimeStamp())
            {
                return true;
            }
            return false;
        }

        bool GetNearbyTargetWall(Vector3 hashPos, BasePlayer _player)
        {
            double RaidInitData = (GetTimeStamp() + RaidDelay);
            double RaidEndData = (GetTimeStamp() + RaidTimeLimit + RaidDelay);

            List<BaseEntity> entities = new List<BaseEntity>();
            Vis.Entities(hashPos, RaidZone1, entities, wallCol);
            if (entities.Count > 0)
            {
                foreach (BaseEntity _entry in entities)
                {
                    ulong _ownerID = _entry.OwnerID;
                    if (_ownerID != 0)
                    {
                        //bool _testVar1 = false; //is player friend
                        bool _testVar2 = false; //is player
                        ulong target = _entry.OwnerID;

                        var _test = RustIOFriendListAPI?.Call("ORFriends", _player.userID, _ownerID);
                        bool _result = Convert.ToBoolean(_test);
                        if (_ownerID == _player.userID) _testVar2 = true;
                        if ((_result == false) && (_testVar2 == false))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        class PVPData
        {
            public List<ulong> playerList = new List<ulong>();
            public double PvPInitCooldown;
            public double PvPEndTimer;
        }

        class RaidDetails
        {
            public double RaidInitCooldown;
            public double RaidEndTimer;
        }

        float CalculateDistance(Vector3 playerPos, Vector3 zonePos)
        {
            var distance = (Vector3.Distance(playerPos, zonePos));
            return distance;
        }

        void loadPermissions()
        {
            string[] Permissionarray = { "player", "debug", "admin", "nocooldown" };
            foreach (string i in Permissionarray)
            {
                string regPerm = Title.ToLower() + "." + i;
                permission.RegisterPermission(regPerm, this);
            }
        }


        private static int wallCol;
        private static int playerCol;

        private void Loaded()
        {
            loadPermissions();
            LoadVariables();
            lang.RegisterMessages(messages, this);
            playerCol = LayerMask.GetMask(new string[] { "Player (Server)" });
            wallCol = LayerMask.GetMask(new string[] { "Construction" });

            tTimers.Add(timer.Repeat(60f, 0, () =>
            {
                DebugMessage(1, "PvP timer is launching");
                DebugMessage(2, "Clearing existing items in list");
                ExpiredPvPZonesList.Clear();
                ExpiredRaidZonesList.Clear();
                foreach (var entry in PvPList)
                {
                    DebugMessage(2, "Checking a zone timestamp");
                    if (PvPList[entry.Key].PvPEndTimer < GetTimeStamp())
                    {
                        DebugMessage(2, "Adding a zone for deletion");
                        ExpiredPvPZonesList.Add(entry.Key);
                    }
                }
                foreach (var entry in raidZoneList)
                {
                    DebugMessage(2, "Checking a zone timestamp");
                    if (raidZoneList[entry.Key].RaidEndTimer < GetTimeStamp())
                    {
                        DebugMessage(2, "Adding a raid zone for deletion");
                        ExpiredRaidZonesList.Add(entry.Key);
                    }
                }

                DebugMessage(2, "Checking if anything needs to be deleted");
                if (ExpiredPvPZonesList.Count > 0)
                {
                    foreach (Vector3 zone in ExpiredPvPZonesList)
                    {
                        DebugMessage(2, "PvP Zones are needed for deletion");
                        PvPList.Remove(zone);
                    }
                }
                if (ExpiredRaidZonesList.Count > 0)
                {
                    foreach (Vector3 zone in ExpiredRaidZonesList)
                    {
                        DebugMessage(2, "Raids Zones are needed for deletion");
                        raidZoneList.Remove(zone);
                    }
                }
                ExpiredPvPZonesList.Clear();
                ExpiredRaidZonesList.Clear();
            }));
        }
        private void Unloaded()
        {
            DestroyTimers();
            PvPList.Clear();
            raidZoneList.Clear();
            ExpiredPvPZonesList.Clear();
            ExpiredRaidZonesList.Clear();
            tTimers.Clear();
        }

        void DestroyTimers()
        {
            foreach (Timer sTimer in tTimers)
            {
                DebugMessage(2, "Destroying a Timer now");
                sTimer.Destroy();
                DebugMessage(2, "Destroyed a Timer");
            }
            tTimers.Clear();
            DebugMessage(1, "Removed all items from the list");
        }
        bool IsAllowed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.UserIDString, perm)) return true;
            return false;
        }
        double GetTimeStamp()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        static void NullifyDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
            hitinfo.HitMaterial = 0;
            hitinfo.PointStart = Vector3.zero;
        }
        public bool checkEntityInZone(BaseCombatEntity _entity, Vector3 _zoneKey, int _maxDistance)
        {
            if (_entity.Distance(_zoneKey) < _maxDistance)
            {
                DebugMessage(2, string.Format("entity Distance: {0} from tested zone", _entity.Distance(_zoneKey)));
                DebugMessage(1, "Entity In zone, Returning True");
                return true;
            }
            else
            {
                DebugMessage(2, string.Format("entity Distance: {0} from tested zone", _entity.Distance(_zoneKey)));
                DebugMessage(1, "Entity out of zone, Returning False");
                return false;
            }
        }
        public bool checkPlayerInZone(BasePlayer _player, Vector3 _zoneKey, int _maxDistance)
        {
            if (_player.Distance(_zoneKey) < _maxDistance)
            {
                DebugMessage(2, string.Format("Player Distance: {0} from tested zone", _player.Distance(_zoneKey)));
                DebugMessage(1, "Entity In zone, Returning True");
                return true;
            }
            else
            {
                DebugMessage(2, string.Format("Player Distance: {0} from tested zone", _player.Distance(_zoneKey)));
                DebugMessage(1, "Entity out of zone, Returning False");
                return false;
            }
        }
        public bool checkPlayerRegistered(BasePlayer _player, Vector3 _zoneKey)
        {
            if (PvPList[_zoneKey].playerList.Contains(_player.userID))
            {
                DebugMessage(2, string.Format("{0} is registered to the zone", _player.displayName));
                DebugMessage(1, "Returning True");
                return true;
            }
            else
            {
                DebugMessage(2, string.Format("{0} is not registered to the zone", _player.displayName));
                DebugMessage(1, "Returning false");
                return false;
            }
        }
        bool checkBuildingOwner(BasePlayer _player, BaseCombatEntity _entity)
        {
            BaseEntity _testEntity = _entity;
            ulong _testResult = FindOwner(_testEntity);
            if (_testResult == _player.userID) return true;
            else return false;
        }
        bool checkNearPlayerNotFriend (BasePlayer _player, Vector3 _hashPos)
        {
            if (BasePlayer.activePlayerList.Count < 2) return false;
            foreach (BasePlayer _target in BasePlayer.activePlayerList)
            {
                if (!(_target == _player))
                {
                    float distance_between = Vector3.Distance(_hashPos, _target.transform.position);
                    if (distance_between <= PVPZone1)
                    {
                        Puts(_target.UserIDString);
                        var _test = RustIOFriendListAPI?.Call("ORFriends", _player.userID, _target.userID);
                        bool _result = Convert.ToBoolean(_test);
                        if (_result == false) return true;
                    }
                }
            }
            return false;
        }
        void registerNearPlayers(Vector3 _hashPos)
        {
            List<BasePlayer> _NearPlayers = new List<BasePlayer>();
            Vis.Entities(_hashPos, PVPZone2, _NearPlayers, playerCol);
            foreach (BasePlayer _entry in _NearPlayers)
            {
                PvPList[_hashPos].playerList.Add(_entry.userID);// Add to the list with this
                timer.Once(PvPDelay, () =>
                {
                    ChatMessageHandler(_entry, "PvP Enabled");
                });
                ChatMessageHandler(_entry, "Warning - A PVP zone has been created");
                ChatMessageHandler(_entry, "You are part of this event.");
                ChatMessageHandler(_entry, string.Format("It will start in {0} seconds.", PvPDelay));
            }
        }
        bool zoneCooldownCheck(string _zoneType, Vector3 _zonekey)
        {
            DebugMessage(1, "Checking zone cooldown");
            if (_zoneType == "raid")
            {
                if (raidZoneList[_zonekey].RaidInitCooldown < GetTimeStamp()) return true;
                else return false;
            }
            else if (_zoneType == "pvp")
            {
                if (PvPList[_zonekey].PvPInitCooldown < GetTimeStamp()) return true;
                else return false;
            }
            else
            {
                DebugMessage(1, "Warning zoneCooldownCheck has failed all if statements.");
                return false;
            }
        }
        bool zoneTimerCheck(string _zoneType, Vector3 _zonekey)
        {
            DebugMessage(1, "Checking zone end time");
            if (_zoneType == "raid")
            {
                if (raidZoneList[_zonekey].RaidEndTimer > GetTimeStamp()) return true;
                else return false;
            }
            else if (_zoneType == "pvp")
            {
                if (PvPList[_zonekey].PvPEndTimer > GetTimeStamp()) return true;
                else return false;
            }
            else
            {
                DebugMessage(1, "Warning zoneCooldownCheck has failed all if statements.");
                return false;
            }
        }
        ulong FindOwner(BaseEntity entity)
        {
            ulong ownerid = entity.OwnerID;
            return ownerid;
        }
        void createPlayerCooldown(BasePlayer _player)
        {
            if (!PlayerCooldownList.Keys.Contains(_player.userID))
            {
                PlayerCooldownList.Add(_player.userID, GetTimeStamp() - playerCooldown);
            }
        }
        bool ReceivingSnapshotcheck(BasePlayer _player)
        {
            if (_player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(_player));
                return true;
            }
            return false;
        }
        void DebugMessage(int _minDebuglvl, string _msg){
            if (DebugLevel >= _minDebuglvl){
                Puts(_msg);
                if (DebugLevel == 3 && _minDebuglvl == 1){
                    PrintToChat(_msg);}}}

        //////////////////////////////////////////////////////////////////////////////////////
        // MessageHandles ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void BroadcastMessageHandler(string message, params object[] args)
        {
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void ChatMessageHandler(BasePlayer player, string message, params object[] args)
        {
            PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Config ////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private string ChatPrefixColor;
        private string ChatPrefix;
        private string ChatMessageColor;
        private bool purgeMode = false;
        private float RaidZone1;
        private int DebugLevel;
        private int PVPZone1;
        private float PVPZone2;
        private int PVPZone3;
        private int PvPDelay;
        private int PvPTimeLimit;
        private int RaidZone2;
        private int RaidDelay;
        private int RaidTimeLimit;
        private double playerCooldown;
        private double serverPvPCooldown;
        private double serverRaidCooldown;
        private double serverPvPCooldownTimeStamp;
        private double serverRaidCooldownTimeStamp;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
        void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
        {
            //Booleans
            //PickEnabled = Convert.ToBoolean(GetConfig("Settings", "PickEnabled", false));
            //Double
            playerCooldown = Convert.ToDouble(GetConfig("Values", "playerCooldown", 90));
            serverPvPCooldown = Convert.ToDouble(GetConfig("Values", "serverPvPCooldown", 10));
            serverRaidCooldown = Convert.ToDouble(GetConfig("Values", "serverRaidCooldown", 60));
            //Floats
            PVPZone2 = Convert.ToSingle(GetConfig("Values", "PlayerAddRadius", 50.0f));
            RaidZone1 = Convert.ToSingle(GetConfig("Values", "RaidZone1", 5f));
            //Ints
            PVPZone1 = Convert.ToInt32(GetConfig("Values", "PVPZone1", 5));
            PVPZone3 = Convert.ToInt32(GetConfig("Values", "PVPZone3", 50));
            PvPDelay = Convert.ToInt32(GetConfig("Values", "PvPDelay", 30));
            PvPTimeLimit = Convert.ToInt32(GetConfig("Values", "PvPTimeLimit", 60));
            RaidZone2 = Convert.ToInt32(GetConfig("Values", "RaidZone2", 300));
            RaidDelay = Convert.ToInt32(GetConfig("Values", "RaidDelay", 30));
            RaidTimeLimit = Convert.ToInt32(GetConfig("Values", "RaidTimeLimit", 180));
            DebugLevel = Convert.ToInt32(GetConfig("Values", "DebugLevel", 0));
            //Strings
            //Targated = Convert.ToString(GetConfig("Messages", "NotAffected", "You are being targeted"));
            ChatPrefix = Convert.ToString(GetConfig("ChatSettings", "ChatPrefix", "AntiBandit"));
            ChatPrefixColor = Convert.ToString(GetConfig("ChatSettings", "ChatPrefixColor", "008800"));
            ChatMessageColor = Convert.ToString(GetConfig("ChatSettings", "ChatMessageColor", "yellow"));
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

        //////////////////////////////////////////////////////////////////////////////////////
        // Lang //////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"MissingPermission", "You do not have the required permission: {0}" },
            {"MissingAdminCmD", "Missing Command please use /antibandit (Debug / Purge)" },
            {"DebugIncorrectCmD", "Invalid command, please use: /antibandit Debug 0/1/2/3" },
            {"DebugMode", "Debug Mode set to {0}" },
            {"PurgeInccorectCmd", "Invalid command, please use: /antibandit purge true/false" },
            {"PurgeModeOn", "Purge Mode Has Started" },
            {"PurgeModeOff", "Purge Mode Has Ended" },
            {"InvalidCmD", "Invalid Command" },
            {"XhasCreatedZone", "{0} has created a zone." },
            {"RaidZoneActive", "A Raid zone has become active" },   //Incorrect format: true/false
            {"PurgeInccorectConsole", "Incorrect format: true/false" }
        };
    }
}