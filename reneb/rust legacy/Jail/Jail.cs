// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("Jail", "Reneb", "1.0.1", ResourceId = 954)]
    class Jail : RustLegacyPlugin
    {
        [PluginReference] Plugin ZoneManager;

        [PluginReference] Plugin Spawns;

        ////////////////////////////////////////////
        /// FIELDS
        ////////////////////////////////////////////
        StoredData storedData;
        static Hash<string, JailInmate> jailinmates = new Hash<string, JailInmate>();
        public DateTime epoch = new System.DateTime(1970, 1, 1);
        bool hasSpawns = false;
        private Hash<NetUser, Plugins.Timer> TimersList = new Hash<NetUser, Plugins.Timer>();
        public RustServerManagement management;

        /////////////////////////////////////////
        /// Cached Fields, used to make the plugin faster
        /////////////////////////////////////////
        public NetUser cachedPlayer;
        public int cachedTime;
        public int cachedCount;
        public JailInmate cachedJail;
        public int cachedInterval;

        /////////////////////////////////////////
        // Data Management
        /////////////////////////////////////////
        class StoredData
        {
            public HashSet<JailInmate> JailInmates = new HashSet<JailInmate>();
            public StoredData()
            {
            }
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Jail", storedData);
        }
        void LoadData()
        {
            jailinmates.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Jail");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var jaildef in storedData.JailInmates)
                jailinmates[jaildef.userid] = jaildef;
        }

        /////////////////////////////////////////
        // class JailInmate
        // Where all informations about a jail inmate is stored in the database
        /////////////////////////////////////////

        public class JailInmate
        {
            public string userid;
            public string x;
            public string y;
            public string z;
            public string jx;
            public string jy;
            public string jz;
            public string expireTime;
            Vector3 jail_position;
            Vector3 free_position;
            int expire_time;

            public JailInmate()
            {
            }

            public JailInmate(NetUser player, Vector3 position, int expiretime = -1)
            {
                userid = player.playerClient.userID.ToString();
                x = player.playerClient.lastKnownPosition.x.ToString();
                y = player.playerClient.lastKnownPosition.y.ToString();
                z = player.playerClient.lastKnownPosition.z.ToString();
                jx = position.x.ToString();
                jy = position.y.ToString();
                jz = position.z.ToString();
                expireTime = expiretime.ToString();
            }
            public void UpdateJail(Vector3 position, int expiretime = -1)
            {
                jx = position.x.ToString();
                jy = position.y.ToString();
                jz = position.z.ToString();
                expireTime = expiretime.ToString();
            }
            public Vector3 GetJailPosition()
            {
                if (jail_position == default(Vector3)) jail_position = new Vector3(float.Parse(jx),float.Parse(jy),float.Parse(jz));
                return jail_position;
            }
            public Vector3 GetFreePosition()
            {
                if (free_position == default(Vector3)) free_position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return free_position;
            }
            public int GetExpireTime()
            {
                if (expire_time == 0) expire_time = int.Parse(expireTime);
                return expire_time;
            }
        }

        /////////////////////////////////////////
        // Oxide Hooks
        /////////////////////////////////////////

        /////////////////////////////////////////
        // Loaded()
        // Called when the plugin is loaded
        /////////////////////////////////////////
        void Loaded()
        {
            LoadData();
            permission.RegisterPermission("canjail", this);
            management = RustServerManagement.Get();
        }

        /////////////////////////////////////////
        // LoadDefaultConfig()
        // Called first when the plugin loads to load the default config
        /////////////////////////////////////////
        void LoadDefaultConfig() { }

        /////////////////////////////////////////
        // Unload()
        // Called when the plugin is unloaded (via oxide.unload or oxide.reload or when the server shutsdown)
        /////////////////////////////////////////
        void Unload()
        {
            foreach (KeyValuePair<NetUser, Plugins.Timer> pair in TimersList)
            {
                pair.Value.Destroy();
            }
            TimersList.Clear();
        }

        void OnServerInitialized()
        {
            foreach(PlayerClient player in PlayerClient.All)
            {
                if (jailinmates[player.userID.ToString()] != null)
                {
                    NetUser netuser = player.netUser;
                    SendPlayerToJail(netuser);
                    CheckPlayerExpireTime(netuser);
                }
            }
        }

        /////////////////////////
        // OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        // Called when a player spawns (after connection or after death)
        /////////////////////////
        void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        {
            if (jailinmates[player.userID.ToString()] != null)
            {
                NetUser netuser = player.netUser;
                SendPlayerToJail(netuser);
                CheckPlayerExpireTime(netuser);
            }
        }

        /////////////////////////////////////////
        // Oxide Permission system
        /////////////////////////////////////////
        bool hasPermission(NetUser player) { if (player.CanAdmin()) return true; return permission.UserHasPermission(player.playerClient.userID.ToString(), "canjail"); }

        /////////////////////////////////////////
        // ZoneManager Hooks
        /////////////////////////////////////////

        /////////////////////////////////////////
        // bool isPlayerInZone(string ZoneID, BasePlayer player)
        // Called to see if a player is inside a zone or not
        /////////////////////////////////////////
        bool isInZone(NetUser player)
        {
            if (ZoneManager == null) return false;
            return (bool)ZoneManager.Call("isPlayerInZone", "Jail", player.playerClient);
        }

        /////////////////////////////////////////
        // OnEnterZone(string ZoneID, BasePlayer player)
        // Called when a player enters a Zone managed by ZoneManager
        /////////////////////////////////////////
        void OnEnterZone(string ZoneID, PlayerClient player)
        {
            if (ZoneID == "Jail")
            {
                NetUser netuser = player.netUser;
                if (hasPermission(netuser)) { SendReply(netuser, string.Format(WelcomeJail, netuser.displayName)); }
                else if (jailinmates[player.userID.ToString()] == null) { SendReply(netuser, KeepOut); }
            }
        }

        /////////////////////////////////////////
        // OnExitZone(string ZoneID, BasePlayer player)
        // Called when a player leaves a Zone managed by ZoneManager
        /////////////////////////////////////////
        void OnExitZone(string ZoneID, PlayerClient player)
        {
            if (ZoneID == "Jail")
            {
                if (jailinmates[player.userID.ToString()] != null) { SendReply(player.netUser, KeepIn); }
            }
        }

        /////////////////////////////////////////
        // Spawns Database Hooks
        /////////////////////////////////////////

        /////////////////////////////////////////
        // int GetSpawnsCount(string spawnfilename)
        // returns the number of spawns in the file
        //
        // Vector3 GetRandomSpawnVector3(string spawnfilename, int max)
        // returns a random spawn between index 1 and index MAX (here is the number of spawns in the file)
        /////////////////////////////////////////
        object FindCell(string userid)
        {
            if (Spawns == null) { Puts(NoSpawnDatabase); return null; }
            if (spawnfile == null) { Puts(NoSpawnFile); return null; }
            var count = Spawns.Call("GetSpawnsCount", spawnfile);
            if (count is bool) return null;
            if ((int)count == 0) { Puts(EmptySpawnFile); return null; }
            return Spawns.Call("GetRandomSpawn", spawnfile);
        }
         
        void LoadSpawnfile()
        {
            if (spawnfile == null) { Puts(NoSpawnFile); return; }
            var count = Spawns.Call("GetSpawnsCount", spawnfile);
            if (count is bool)
            {
                Puts("{0} is not a valid spawnfile", spawnfile.ToString());
                Config["spawnfile"] = null;
                spawnfile = null;
                SaveConfig();
                return;
            }
            Puts(JailsLoaded, count.ToString());
        }

        /////////////////////////////////////////
        // Random functions
        /////////////////////////////////////////
        void ForcePlayerPosition(PlayerClient player, Vector3 destination)
        {
            management.TeleportPlayerToWorld(player.netPlayer, destination);
        }

        int CurrentTime() { return System.Convert.ToInt32(System.DateTime.UtcNow.Subtract(epoch).TotalSeconds); }

        
        private object FindPlayer(string tofind)
        {
            var findplayer = rust.FindPlayer(tofind);
            if(findplayer == null)
            { 
                return noPlayersFound;
            }
            return findplayer;
        }

        /////////////////////////////////////////
        // Jail functions
        /////////////////////////////////////////

        /////////////////////////////////////////
        // AddPlayerToJail(BasePlayer player, int expiretime)
        // Adds a player to the jail, and saves him in the database
        /////////////////////////////////////////
        void AddPlayerToJail(NetUser player, int expiretime)
        {
            string userid = player.playerClient.userID.ToString();
            var tempPoint = FindCell(userid);
            
            if (tempPoint == null) { return; }
            JailInmate newjailmate;
            if (jailinmates[userid] != null) { newjailmate = jailinmates[userid]; newjailmate.UpdateJail((Vector3)tempPoint, expiretime); }
            else newjailmate = new JailInmate(player, (Vector3)tempPoint, expiretime);
            if (jailinmates[userid] != null) storedData.JailInmates.Remove(jailinmates[userid]);
            jailinmates[userid] = newjailmate;
            storedData.JailInmates.Add(jailinmates[userid]);
            SaveData();
        }

        /////////////////////////////////////////
        // SendPlayerToJail(BasePlayer player)
        // Sends a player to the jail
        /////////////////////////////////////////
        void SendPlayerToJail(NetUser player)
        {
            if (jailinmates[player.playerClient.userID.ToString()] == null) return;
            ZoneManager.Call("AddPlayerToZoneKeepinlist", "Jail", player.playerClient);
            ForcePlayerPosition(player.playerClient, jailinmates[player.playerClient.userID.ToString()].GetJailPosition());
            SendReply(player, YouAreInJail);
        }

        /////////////////////////////////////////
        // RemovePlayerFromJail(BasePlayer player)
        // Removes a player from the jail (need to be called after SendPlayerOutOfJail, because we need the return point)
        /////////////////////////////////////////
        void RemovePlayerFromJail(NetUser player)
        {
            if (jailinmates[player.playerClient.userID.ToString()] != null) storedData.JailInmates.Remove(jailinmates[player.playerClient.userID.ToString()]);
            jailinmates[player.playerClient.userID.ToString()] = null;
            SaveData();
        }

        /////////////////////////////////////////
        // SendPlayerOutOfJail(BasePlayer player)
        // Send player out of the jail
        /////////////////////////////////////////
        void SendPlayerOutOfJail(NetUser player)
        {
            if (jailinmates[player.playerClient.userID.ToString()] == null) return;
            cachedJail = jailinmates[player.playerClient.userID.ToString()];
            ZoneManager.Call("RemovePlayerFromZoneKeepinlist", "Jail", player.playerClient);
            ForcePlayerPosition(player.playerClient, cachedJail.GetFreePosition());
            SendReply(player, YouAreFree);
        }

        /////////////////////////////////////////
        // CheckPlayerExpireTime(BasePlayer player)
        // One function to take care of the timer, calls himself.
        /////////////////////////////////////////
        void CheckPlayerExpireTime(NetUser player)
        {
            if (TimersList[player] != null) { TimersList[player].Destroy(); TimersList[player] = null; }
            if (player.playerClient == null) return;
            if (player.playerClient.controllable == null) return;
            if (jailinmates[player.playerClient.userID.ToString()] == null) return;
            cachedJail = jailinmates[player.playerClient.userID.ToString()];
            if (cachedJail.GetExpireTime() == -1) return;
            cachedInterval = cachedJail.GetExpireTime() - CurrentTime();
            if (cachedInterval < 1)
            {
                SendPlayerOutOfJail(player);
                RemovePlayerFromJail(player); 
            }
            else
                TimersList[player] = timer.Once( (float)(cachedInterval + 1), () => CheckPlayerExpireTime(player));
        }

        /////////////////////////////////////////
        // Chat commands
        /////////////////////////////////////////
        [ChatCommand("jail_config")]
        void cmdChatJailConfig(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, NoPermission); return; }
            if(ZoneManager == null) { SendReply(player, NoZoneManager); return; }
            if (Spawns == null) { SendReply(player, NoSpawnDatabase); return; }
            if (args.Length < 2)
            {
                SendReply(player, "/jail_config spawnfile jailspawnfile => set the spawns where players will be jailed");
                SendReply(player, "/jail_config zone RADIUS");
                SendReply(player, "You must stand in the center of the radius zone of the jail.");
                return;
            }
            switch(args[0].ToLower())
            {
                case "zone":
                    string[] zoneargs = new string[] { "name", "Jail", "eject", "true", "radius", args[1], "pvpgod", "true", "pvegod", "true", "sleepgod", "true", "undestr", "true", "nobuild", "true", "notp", "true", "nokits", "true", "nodeploy", "true", "nosuicide", "true" };
                    ZoneManager.Call("CreateOrUpdateZone", "Jail", zoneargs, player.playerClient.lastKnownPosition);
                    SendReply(player, JailCreated);
                break;
                case "spawnfile":
                    var count = Interface.GetMod().CallHook("GetSpawnsCount", new object[] { args[1] });
                    if (count == null)
                    {
                        SendReply(player, "SpawnFile {0} is not a valid spawnfile", args[0].ToString());
                        Config["spawnfile"] = null;
                        spawnfile = null;
                    }
                    else
                    {
                        Config["spawnfile"] = args[1];
                        spawnfile = args[1];
                        SendReply(player, "New SpawnFile for Jaild Players: {0}", spawnfile);
                        LoadSpawnfile();
                    }
               break;
                default:
                    return;
                    break;

            }
            SaveConfig();
        }
        [ChatCommand("jail")]
        void cmdChatJail(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, NoPermission); return; }
            if (ZoneManager == null) { SendReply(player, NoZoneManager); return; }
            if(Spawns == null) { SendReply(player, NoSpawnDatabase); return; }
            if (args.Length  == 0) { SendReply(player, "/jail PLAYER option:Time(seconds)"); return; }

            var target = FindPlayer(args[0].ToString());
            if (target is string) { SendReply(player, target.ToString()); return; }
            cachedPlayer = (NetUser)target;

            cachedTime = -1;
            if (args.Length > 1) int.TryParse(args[1], out cachedTime);
            if (cachedTime != -1) cachedTime += CurrentTime();
            AddPlayerToJail(cachedPlayer, cachedTime);
            SendPlayerToJail(cachedPlayer);

            CheckPlayerExpireTime(cachedPlayer);

            SendReply(player, string.Format("{0} was sent to jail",cachedPlayer.displayName.ToString()));
        }
        [ChatCommand("jailhelp")]
        void cmdChatJailHelp(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, NoPermission); return; }
            if (ZoneManager == null) { SendReply(player, NoZoneManager); return; }
            if (Spawns == null) { SendReply(player, NoSpawnDatabase); return; }
            SendReply(player, "/jail PLAYER optional:XX => Send a player to jail (for optional X seconds)");
            SendReply(player, "/free PLAYER => Free player from jail");
            SendReply(player, "/jail_config spawnfile jailspawnfile => set the spawns where players will be jailed");
            SendReply(player, "/jail_config zone RADIUS");
            SendReply(player, "You must stand in the center of the radius zone of the jail.");
        }

        [ChatCommand("free")]
        void cmdChatFree(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, NoPermission); return; }
            if (ZoneManager == null) { SendReply(player, NoZoneManager); return; }
            if (Spawns == null) { SendReply(player, NoSpawnDatabase); return; }
            if (args.Length == 0) { SendReply(player, "/jail PLAYER option:Time(seconds)"); return; }

            var target = FindPlayer(args[0].ToString());
            if (target is string) { SendReply(player, target.ToString()); return; }
            cachedPlayer = (NetUser)target;

            SendPlayerOutOfJail(cachedPlayer);
            RemovePlayerFromJail(cachedPlayer);

            CheckPlayerExpireTime(cachedPlayer);

            SendReply(player, string.Format("{0} was freed from jail", cachedPlayer.displayName.ToString()));
        }


        /////////////////////////////////////////
        // Config handler
        // Thx to Bombardir and his code in Pets, stole his way! Much better and cleaner than my old one
        /////////////////////////////////////////
        private static string NoPermission = "You don't have the permission to use this command";
        private static string NoZoneManager = "You can't use the Jail plugin without ZoneManager";
        private static string JailCreated = "You successfully created/updated the jail zone, use /zone_list for more informations";
        private static string noPlayersFound = "No Online player with this name was found";
        private static string NoSpawnDatabase = "No spawns set or no spawns database found http://forum.rustoxide.com/resources/spawns-database.720";
        private static string multiplePlayersFound = "Multiple players found";
        private static string spawnfile = null;
        private static string NoSpawnFile = "No SpawnFile - You must configure your spawnfile first: /jail_config spawnfile FILENAME";
        private static string JailsLoaded = "Jail Plugin: {0} cell spawns were detected and loaded";
        private static string YouAreInJail = "You were arrested and sent to jail";
        private static string YouAreFree = "You were freed from jail";
        private static string KeepOut = "Keep out, no visitors allowed in the jail";
        private static string WelcomeJail = "Welcome to the jail {0}";
        private static string KeepIn = "You are not allowed to leave the Jail";
        private static string EmptySpawnFile = "The spawnfile is empty, can't find any spawn points. Make sure to create a valid Spawn Database first";

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Message: No Permission", ref NoPermission);
            CheckCfg<string>("Message: No ZoneManager", ref NoZoneManager);
            CheckCfg<string>("Message: Jail Created", ref JailCreated);
            CheckCfg<string>("Message: No Player Found", ref noPlayersFound);
            CheckCfg<string>("Message: No SpawnDatabase", ref NoSpawnDatabase);
            CheckCfg<string>("Message: No SpawnFile", ref NoSpawnFile);
            CheckCfg<string>("Message: Loaded Cells", ref JailsLoaded);
            CheckCfg<string>("Message: Sent In Jail", ref YouAreInJail);
            CheckCfg<string>("Message: Freed", ref YouAreFree);
            CheckCfg<string>("Message: KeepOut", ref KeepOut);
            CheckCfg<string>("Message: Welcome ADMIN", ref WelcomeJail);
            CheckCfg<string>("spawnfile", ref spawnfile);
            CheckCfg<string>("Message: KeepIn", ref KeepIn);
            CheckCfg<string>("Message: Empty Spawn file", ref EmptySpawnFile);
            SaveConfig();
        }

        void SendHelpText(NetUser netuser)
        {
            if (!hasPermission(netuser)) return;
            SendReply(netuser, "Jail Commands: /jailhelp");
        }
    }
}