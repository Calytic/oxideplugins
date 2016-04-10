using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
namespace Oxide.Plugins
{
    [Info("MagicArea", "Norn", 0.1, ResourceId = 1551)]
    [Description("Areas to practice building/pvp.")]
    public class MagicArea : RustPlugin
    {
        [PluginReference] Plugin Kits;
        [PluginReference] Plugin MagicTeleportation;

        // -------------- [ SAVING VARIABLES ] -------------- 

        class MA
        {
            public Dictionary<int, AreaInfo> Areas = new Dictionary<int, AreaInfo>();
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
            public Dictionary<uint, AreaEntities> Entities = new Dictionary<uint, AreaEntities>();
            public MA() { }
        }
        public class AreaEntities
        {
            public uint iID;
            public int iAreaID;
            public ulong uCreatorID;
            public int iCreated;
            public int iExpire;
            public AreaEntities() { }
        }
        class PlayerData
        {
            public ulong uUserID;
            public int iInArea;
            public Int32 iInitStamp;
            public PlayerData() { }
        }
        class SpawnInfo
        {
            public float fX;
            public float fY;
            public float fZ;
            public int iEID;
            public SpawnInfo() { }
        }
        class AreaInfo
        {
            public int iID;
            public string tTitle;
            public string tDescription;
            public float fMinX;
            public float fMinY;
            public float fMinZ;
            public float fRadius;
            public bool uEnabled;
            public bool bGod;
            public bool bResetInv;
            public int iCount;
            public string tKit;
            public bool bCanResearch;
            public bool bRemoveEntities;
            public int iEntityExpire;
            public Dictionary<int, SpawnInfo> Spawns = new Dictionary<int, SpawnInfo>();
            public AreaInfo() { }
        }
        MA MAData;

        #region Localization

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"KitReceived", "[<color=green>INFO:</color>] You have been given kit: <color=yellow>{kit_name}</color> to use inside of <color=yellow>{area_title}</color>."},
                {"InventoryReset", "[<color=green>INFO:</color>] Your inventory has been <color=red>reset</color> because you left an area."},
                {"ResearchBlocked", "[<color=yellow>ERROR</color>] Researching is <color=red>blocked</color> in this area."},
                {"AreaCreated", "[<color=green>INFO:</color>] You have successfully created area id: {area_id}."},
                {"TeleportedBack", "[<color=green>INFO</color>] You have been teleported back to <color=yellow>{area_title}</color>." },
                {"EntityExpiry", "[<color=yellow>INFO</color>] This object will expire at: <color=yellow>{expire_time}</color>!\n(Current Time: <color=yellow>{current_time}</color>)" }
            };
            lang.RegisterMessages(messages, this);
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        #endregion
        // -----------------------------------------------

        Timer AreaSync;
        void OnServerInitialized()
        {
            Puts("Loaded " + MAData.Areas.Count.ToString() + " area(s).");

            // --- [ TIMERS ] ---
            int seconds = Convert.ToInt32(Config["Settings", "TimerInterval"]);
            AreaSync = timer.Repeat(seconds, 0, () => AreaTimer());
            LoadDefaultMessages();
        }

        private void AreaTimer()
        {
            // ==================================== [ ENTITY PORTION ] ==========================================
            new List<uint>(MAData.Entities.Keys).ForEach(u =>
            {
                BaseNetworkable ent = BaseNetworkable.serverEntities.Find(u);
                if (ent != null && UnixTimeStampUTC() >= MAData.Entities[u].iExpire)
                {
                    int area_id = MAData.Entities[u].iAreaID;
                    if (area_id != -1 && MagicAreaExists(area_id))
                    {
                        if (MAData.Areas[area_id].bRemoveEntities && MAData.Areas[area_id].iEntityExpire != 0)
                        {
                            MAData.Entities.Remove(u);
                            ent.Kill();
                            if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts("Removing entity ID: " + u.ToString() + " [EXPIRED]"); }
                        }
                    }
                }
            });
            // ================================== [ PLAYER ] ====================================================
            foreach (BasePlayer connected_player in BasePlayer.activePlayerList)
            {
                bool found = false;
                if(connected_player != null && connected_player.IsConnected())
                {
                    if(!PlayerExists(connected_player)) { InitPlayer(connected_player); }
                    foreach(var area in MAData.Areas.Values)
                    {
                        if(PlayerToPoint(connected_player, area.fRadius, area.fMinX, area.fMinY, area.fMinZ))
                        {
                            if (MAData.PlayerData[connected_player.userID].iInArea != area.iID)
                            {
                                MAData.PlayerData[connected_player.userID].iInArea = area.iID;
                                Interface.Oxide.CallHook("OnPlayerEnterMagicArea", connected_player, MAData.PlayerData[connected_player.userID].iInArea);
                                if (area.tKit.Length >= 1 && area.tKit != null)
                                {
                                    object iskit = Kits?.Call("isKit", area.tKit);
                                    if (iskit is bool && (bool)iskit)
                                    {
                                        connected_player.inventory.Strip();
                                        object successkit = Kits.Call("GiveKit", connected_player, area.tKit);
                                        if (successkit is bool && (bool)successkit)
                                        {
                                            string parsed_config = GetMessage("KitReceived", connected_player.UserIDString);
                                            parsed_config = parsed_config.Replace("{kit_name}", area.tKit);
                                            parsed_config = parsed_config.Replace("{area_title}", area.tTitle);
                                            PrintToChat(connected_player, parsed_config);
                                        }
                                    }
                                }
                            }
                            found = true;
                        }
                    }
                    if(!found && MAData.PlayerData[connected_player.userID].iInArea != -1)
                    {
                        if(MAData.Areas[MAData.PlayerData[connected_player.userID].iInArea].bResetInv)
                        {
                            PrintToChat(connected_player, GetMessage("InventoryReset", connected_player.UserIDString));
                            connected_player.inventory.Strip();
                        }
                        Interface.Oxide.CallHook("OnPlayerExitMagicArea", connected_player, MAData.PlayerData[connected_player.userID].iInArea);
                        MAData.PlayerData[connected_player.userID].iInArea = -1;
                    }
                }
            }
        }

        private void OnPlayerExitMagicArea(BasePlayer player, int area)
        {
            if (MagicAreaExists(area)) {
                if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts(player.displayName + " has left: " + MAData.Areas[area].tTitle + " [ " + area.ToString() + " ]"); PrintToChat(player, player.displayName + " has left: " + MAData.Areas[area].tTitle + " [ " + area.ToString() + " ]"); } }
        }

        private void OnPlayerEnterMagicArea(BasePlayer player, int area)
        {
            if (MagicAreaExists(area)) { if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts(player.displayName + " has entered: " + MAData.Areas[area].tTitle + " [ " + area.ToString() + " ]"); PrintToChat(player, player.displayName + " has entered: " + MAData.Areas[area].tTitle + " [ " + area.ToString() + " ]"); } }
        }

        private int CreateMagicArea(Vector3 position, float radius, string title = "-1", string description = "-1", bool enabled = true)
        {
            int id = -1;
            if (position != Vector3.zero)
            {
                AreaInfo Area = new AreaInfo();
                Area.iID = GetRandomNumber(0, 25);
                if(title == "-1") { title = "Area" + Area.iID.ToString(); }
                Area.tTitle = title;
                Area.tDescription = description;
                Area.uEnabled = enabled;
                Area.iCount = 0;
                Area.fMinX = position.x;
                Area.fMinY = position.y;
                Area.fMinZ = position.z;
                Area.fRadius = radius;
                Area.bGod = false;
                Area.tKit = "";
                Area.bCanResearch = false;
                Area.bResetInv = true;
                Area.bRemoveEntities = true;
                Area.iEntityExpire = Convert.ToInt32(Config["Settings", "DefaultExpire"]);
                id = Area.iID;
                MAData.Areas.Add(Area.iID, Area);
                SaveData();
            }
            return id;
        }

        private bool MagicAreaExists(int id)
        {
            AreaInfo item = null;
            if(MAData.Areas.TryGetValue(id, out item)) { return true; }
            return false;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BaseEntity e = gameObject.ToBaseEntity();
            BasePlayer player = planner.ownerPlayer;
            if (!(e is BaseEntity) || player == null)
            {
                return;
            }
            int id = MAData.PlayerData[player.userID].iInArea;
            if (PlayerExists(player) && id != -1 && MagicAreaExists(id))
            {
                if (MAData.Areas[id].bRemoveEntities && MAData.Areas[id].iEntityExpire != 0)
                {
                    AreaEntities Area = new AreaEntities();
                    Area.iAreaID = id;
                    Area.iCreated = UnixTimeStampUTC();
                    Area.iExpire = Area.iCreated + MAData.Areas[id].iEntityExpire;
                    Area.iID = (uint)e.net.ID;
                    Area.uCreatorID = player.userID;
                    MAData.Entities.Add(Area.iID, Area);
                    if (Convert.ToBoolean(Config["Settings", "Debug"]))
                    {
                        string parsed_config = GetMessage("EntityExpiry", player.UserIDString);
                        parsed_config = parsed_config.Replace("{expire_time}", UnixTimeStampToDateTime(Area.iExpire).ToLongTimeString());
                        parsed_config = parsed_config.Replace("{current_time}", UnixTimeStampToDateTime(UnixTimeStampUTC()).ToLongTimeString());
                        PrintToChat(player, parsed_config);
                    }
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
            {
                var player = entity as BasePlayer;
                if (PlayerExists(player))
                {
                    int id = MAData.PlayerData[player.userID].iInArea;
                    if (id != -1 && MagicAreaExists(id)) { MAData.PlayerData[player.userID].iInArea = -1; }
                }
            }
            else
            {
                uint id = entity.net.ID;
                if (MAData.Entities.ContainsKey(id))
                {
                    if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts("Destroying entity: " + id.ToString() + ". [DEATH]"); }
                    MAData.Entities.Remove(id);
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            uint id = entity.net.ID;
            if (MAData.Entities.ContainsKey(id))
            {
                if (UnixTimeStampUTC() >= MAData.Entities[id].iExpire)
                {
                    if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts("Destroying entity: " + id.ToString() + ". [EXPIRED]"); }
                    entity.Kill();
                    MAData.Entities.Remove(id);
                }
            }
        }

        private object OnItemResearch(Item item, BasePlayer player)
        {
            if(PlayerExists(player))
            {
                int id = MAData.PlayerData[player.userID].iInArea;
                if (id != -1 && MagicAreaExists(id)) { if(!MAData.Areas[id].bCanResearch) { PrintToChat(player, GetMessage("ResearchBlocked", player.UserIDString)); return false; } }
            }
            return null;
        }

        // --- [ CONSOLE ] ---

        [ConsoleCommand("area.create")]
        private void ccmdCreateArea(ConsoleSystem.Arg arg)
        {
            if (arg.connection.authLevel >= Convert.ToInt32(Config["Admin", "MaxLevel"]))
            {
                var player = arg.connection?.player as BasePlayer;
                if (player == null) return;
                int id = CreateMagicArea(player.transform.position, 50);
                Puts(GetMessage("AreaCreated", player.UserIDString).Replace("{area_id}", id.ToString()));
                PrintToChat(player, GetMessage("AreaCreated", player.UserIDString).Replace("{area_id}", id.ToString()));
            }
        }

        // ------------------

        private object OnPlayerRespawned(BasePlayer player)
        {
            int id = MAData.PlayerData[player.userID].iInArea; bool success = false;
            if (PlayerExists(player) && id != -1 && MagicAreaExists(id))
            {
                if (MAData.Areas[id].Spawns.Count == 0)
                {
                    success = Convert.ToBoolean(MagicTeleportation.CallHook("InitTeleport", player, MAData.Areas[id].fMinX, MAData.Areas[id].fMinY, MAData.Areas[id].fMinZ, false, true, MAData.Areas[id].tTitle, null, 1, 3));
                }
                if (success)
                {
                    string parsed_config = GetMessage("TeleportedBack", player.UserIDString);
                    parsed_config = parsed_config.Replace("{area_title}", MAData.Areas[id].tTitle);
                    PrintToChat(player, parsed_config);
                }
                MAData.PlayerData[player.userID].iInArea = -1;
                return false;
            }
            return null;
        }

        private bool PlayerToPoint(BasePlayer player, float radi, float x, float y, float z)
        {
            float oldposx = 0.0f, oldposy = 0.0f, oldposz = 0.0f, tempposx = 0.0f, tempposy = 0.0f, tempposz = 0.0f;
            oldposx = player.transform.position.x;
            oldposy = player.transform.position.y;
            oldposz = player.transform.position.z;
            tempposx = (oldposx - x);
            tempposy = (oldposy - y);
            tempposz = (oldposz - z);
            if (((tempposx < radi) && (tempposx > -radi)) && ((tempposy < radi) && (tempposy > -radi)) && ((tempposz < radi) && (tempposz > -radi)))
            {
                return true;
            }
            return false;
        }

        void Loaded()
        {
            MAData = Interface.GetMod().DataFileSystem.ReadObject<MA>(this.Title);
        }

        void Unload()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, MAData);
        }

        // ======================== [ PLAYER ] ===========================

        private bool PlayerExists(BasePlayer player)
        {
            PlayerData item = null;
            if (MAData.PlayerData.TryGetValue(player.userID, out item))
            {
                return true;
            }
            return false;
        }

        private bool InitPlayer(BasePlayer player)
        {
            if (!PlayerExists(player))
            {
                PlayerData z = new PlayerData();
                z.uUserID = player.userID;
                z.iInArea = -1;
                z.iInitStamp = UnixTimeStampUTC();
                MAData.PlayerData.Add(z.uUserID, z);
                if (Convert.ToBoolean(Config["Settings", "Debug"])) { Puts("Registering " + player.displayName + " [ " + player.userID.ToString() + " ]."); }
                return true;
            }
            return false;
        }

        // ===============================================================

        private bool IsPlayerInArea(BasePlayer player, float MinX, float MinY, float MaxX, float MaxY)
        {
            if (player != null && player.isConnected)
            { float X = player.transform.position.x; float Y = player.transform.position.y; float Z = player.transform.position.z; if (X >= MinX && X <= MaxX && Y >= MinY && Y <= MaxY) { return true; } }
            return false;
            
        }

        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating..."); Config.Clear();
            Config["Settings", "TimerInterval"] = 1;
            Config["Settings", "Debug"] = false;
            Config["Settings", "DefaultExpire"] = 10800; // 3 hours
            Config["Admin", "MaxLevel"] = 2;
            
            
        }

        private HitInfo OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BasePlayer)
            {
                var player = entity as BasePlayer;
                if (PlayerExists(player))
                {
                    int id = MAData.PlayerData[player.userID].iInArea;
                    if (id != -1 && MagicAreaExists(id))
                    {
                        if (MAData.Areas[id].bGod)
                        {
                            hitInfo.damageTypes.ScaleAll(0f);
                            return hitInfo;
                        }
                    }
                }
            }
            return null;
        }

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            return null;
        }

        public static Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }

        public static int GetRandomNumber(int min, int max)
        {
            System.Random r = new System.Random();
            int n = r.Next();
            return n;
        }
    }
}