using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;
using Rust;
using System.Reflection;
namespace Oxide.Plugins
{
    [Info("MagicTeleportation", "Norn", 0.9, ResourceId = 1404)]
    [Description("Teleportation system.")]
    public class MagicTeleportation : RustPlugin
    {
        [PluginReference]
        Plugin PopupNotifications;

        [PluginReference]
        Plugin BuildingOwners;

        [PluginReference]
        Plugin DeadPlayersList;

        class StoredData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
            public Dictionary<string, HomeEntities> Entities = new Dictionary<string, HomeEntities>();
            public Dictionary<int, TeleportInfo> Teleports = new Dictionary<int, TeleportInfo>();
            public StoredData() { }
        }
        public class TeleportInfo
        {
            public int iID;
            public string tTitle;
            public string tDescription;
            public float fX;
            public float fY;
            public float fZ;
            public bool uEnabled;
            public int iAuthLevel;
            public bool uSleepGod;
            public int iCount;
            public TeleportInfo() { }
        }
        class HomesLocs
        {
            public float fX;
            public float fY;
            public float fZ;
            public float fHealth;
            public int iEID;
            public string tBagName;
            public HomesLocs() { }
        }
        class PlayerData
        {
            public ulong uUserID;
            public bool uCooldownEnabled;
            public Dictionary<int, HomesLocs> HomesLocs = new Dictionary<int, HomesLocs>();
            public PlayerData() { }
        }
        public class HomeEntities
        {
            public string tPrefab_name;
            public string tShortname;
            public bool bEnabled;

            public HomeEntities() { }
        }
        Dictionary<string, string> DEFAULT_HomeEntities = new Dictionary<string, string>() {
            {
                "assets/prefabs/deployable/bed/bed_deployed.prefab", "Bed"
            },
        };
        StoredData MTData;
        private void Loaded()
        {
            MTData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
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
        private int CreateTeleport(string title, string description, float X, float Y, float Z, bool sleepgod = true, int authlevel = 0, bool enabled = true)
        {
            TeleportInfo TPInfo = new TeleportInfo();
            TPInfo.iID = GetRandomNumber(0, 25);
            TPInfo.tTitle = title;
            TPInfo.tDescription = description;
            TPInfo.fX = X;
            TPInfo.fY = Y;
            TPInfo.fZ = Z;
            TPInfo.uSleepGod = sleepgod;
            TPInfo.iAuthLevel = authlevel;
            TPInfo.uEnabled = enabled;
            MTData.Teleports.Add(TPInfo.iID, TPInfo);
            return TPInfo.iID;
        }
        private bool PlayerExists(BasePlayer player)
        {
            PlayerData item = null;
            if (MTData.PlayerData.TryGetValue(player.userID, out item))
            {
                return true;
            }
            return false;
        }
        private int ResetCooldownForAll()
        {
            int count = 0;
            foreach (var player in MTData.PlayerData.Values)
            {
                player.uCooldownEnabled = false;
                count++;
            }
            return count;
        }
        private int PlayerHomeCount(BasePlayer player)
        {
            PlayerData item = null;
            int count = 0;
            if (MTData.PlayerData.TryGetValue(player.userID, out item))
            {
                foreach (var entry in item.HomesLocs.Values)
                {
                    count++;
                }
            }
            return count;
        }
        private bool InitPlayer(BasePlayer player)
        {
            if (!PlayerExists(player))
            {
                PlayerData z = new PlayerData();
                z.uUserID = player.userID;
                z.uCooldownEnabled = false;
                MTData.PlayerData.Add(z.uUserID, z);
                return true;
            }
            return false;
        }
        private bool EntityPlayerCheck(uint netid, BasePlayer attacker = null)
        {
            PlayerData item = null;
            foreach (var entry in MTData.PlayerData.Values)
            {
                if (MTData.PlayerData.TryGetValue(entry.uUserID, out item))
                {
                    if (item.HomesLocs.Count == 0)
                    {
                        return false;
                    }
                    List<int> remove_list = new List<int>();
                    foreach (var home in item.HomesLocs.Values)
                    {
                        if (home.iEID == netid)
                        {
                            remove_list.Add(home.iEID);
                        }
                    }
                    if (remove_list.Count >= 1)
                    {
                        foreach (var z in remove_list)
                        {
                            if (attacker != null)
                            {
                                if (attacker.userID == entry.uUserID)
                                {
                                    string parsed_config = Config["GeneralMessages", "HomeDestroyed"].ToString();
                                    parsed_config = parsed_config.Replace("{home}", item.HomesLocs[z].tBagName);
                                    if (parsed_config.Length >= 1) PrintToChatEx(attacker, parsed_config);
                                }
                                Puts("[" + item.uUserID.ToString() + "] " + item.HomesLocs[z].tBagName + " has been destroyed by " + attacker.displayName.ToString() + " / " + attacker.userID.ToString() + ".");
                            }
                            else
                            {
                                Puts("[" + item.uUserID.ToString() + "] " + item.HomesLocs[z].tBagName + " has been destroyed.");
                            }
                            item.HomesLocs.Remove(z);
                        }
                    }
                }
                return true;
            }
            return false;
        }
        private bool EntityExists(BaseCombatEntity entity)
        {
            if (entity.isActiveAndEnabled)
            {
                return true;
            }
            return false;
        }
        private void OnHomeEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info.Initiator is BasePlayer)
            {
                var attacker = info.Initiator as BasePlayer;
                EntityPlayerCheck(entity.net.ID, attacker);
            }
            else
            {
                EntityPlayerCheck(entity.net.ID);
            }
        }
        List<ulong> SLEEPING_TELEPORTERS = new List<ulong>();
        Dictionary<ulong, Timer> TELEPORT_QUEUE = new Dictionary<ulong, Timer>();
        private HitInfo OnEntityTakeDamage(BaseCombatEntity vic, HitInfo hitInfo)
        {
            if (vic == null || hitInfo == null || vic.ToPlayer() == null) return null;
            BasePlayer player = vic as BasePlayer;
            if (!player.IsSleeping() && SLEEPING_TELEPORTERS.Contains(player.userID))
            {
                SLEEPING_TELEPORTERS.Remove(player.userID);
            }
            if (Convert.ToBoolean(Config["Settings", "TPSleepGod"]))
            {
                if (player.IsSleeping())
                {
                    if (SLEEPING_TELEPORTERS.Contains(player.userID))
                    {
                        if (hitInfo.Initiator is BasePlayer)
                        {
                            var attacker = hitInfo.Initiator as BasePlayer;
                            if (attacker.userID == player.userID)
                            {
                                return null;
                            }
                            else
                            {
                                if (Config["GeneralMessages", "PlayerNoAwake"] != null) PrintToChatEx(attacker, Config["GeneralMessages", "PlayerNoAwake"].ToString());
                                hitInfo.damageTypes.ScaleAll(0f);
                                return hitInfo;
                            }
                        }
                    }
                }
            }
            if (TELEPORT_QUEUE.ContainsKey(player.userID))
            {
                try
                {
                    TELEPORT_QUEUE[player.userID].Destroy();
                    TELEPORT_QUEUE.Remove(player.userID);
                    if (Config["GeneralMessages", "TeleportInterrupted"] != null) PrintToChatEx(player, Config["GeneralMessages", "TeleportInterrupted"].ToString());
                    UnfreezePlayer(player.userID);
                }
                catch
                {
                    //Puts("DEBUG: OnEntityTakeDamage(): Failed to kill " + player.displayName + "'s teleport timer.");
                }
            }
            return null;
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.transform == null)
            {
                return;
            }
            if (MTData.Entities != null)
            {
                foreach (var entry in MTData.Entities)
                {
                    if (entry.Value.bEnabled)
                    {
                        if (entry.Value.tPrefab_name == entity.name)
                        {
                            OnHomeEntityDeath(entity, info);
                            return;
                        }
                    }
                }
            }
        }
        private static Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();
        private static Dictionary<string, int> deployedToItem = new Dictionary<string, int>();
        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            deployedToItem.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
                if (itemdef.GetComponent<ItemModDeployable>() != null) deployedToItem.Add(itemdef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath, itemdef.itemid);
            }


        }
        private void RefundHomeEntity(BasePlayer player, BaseEntity entity, int amount)
        {
            if (entity.GetComponentInParent<Deployable>() != null)
            {
                Deployable refund_item = entity.GetComponentInParent<Deployable>();
                if (refund_item != null)
                {
                    if (deployedToItem.ContainsKey(refund_item.gameObject.name)) player.inventory.GiveItem(deployedToItem[refund_item.gameObject.name], 1, true);
                }
            }
        }
        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BaseEntity e = gameObject.ToBaseEntity();
            BasePlayer player = planner.ownerPlayer;
            if (!(e is BaseEntity) || player == null)
            {
                return;
            }
            if (MTData.Entities != null)
            {
                foreach (var entry in MTData.Entities)
                {
                    if (entry.Value.bEnabled)
                    {
                        if (entry.Value.tPrefab_name == gameObject.name) // Fire Up
                        {
                            int max_per_build = Convert.ToInt32(Config["Settings", "MaxEntitiesPerBuilding"]);
                            int max_homes = Convert.ToInt32(Config["HomeSettings", "MaxHomes"]);
                            if (PlayerHomeCount(player) >= max_homes)
                            {
                                string parsed_config = Config["GeneralMessages", "MaxHomes"].ToString();
                                parsed_config = parsed_config.Replace("{max_homes}", max_homes.ToString());
                                if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);

                                if (Convert.ToBoolean(Config["Settings", "RefundEntity"])) RefundHomeEntity(player, e, 1);
                                e.Kill();
                                return;
                            }
                            Vector3 position = e.transform.position;
                            if (!PlayerExists(player))
                            {
                                if (InitPlayer(player))
                                {
                                    Puts("Set up user data for " + player.displayName + " (" + player.userID + ").");
                                }
                            }
                            PlayerData d = null;
                            if (MTData.PlayerData.TryGetValue(player.userID, out d))
                            {
                                HomesLocs z = new HomesLocs();
                                z.iEID = (int)e.net.ID;
                                z.fX = position.x;
                                z.fY = position.y;
                                z.fZ = position.z;
                                z.fHealth = e.MaxHealth();
                                z.tBagName = "Bed";
                                d.HomesLocs.Add(z.iEID, z);
                                Puts("Set up new home for " + player.displayName + " (" + player.userID + ") (" + z.iEID.ToString() + ")");

                                if (Config["GeneralMessages", "SetupHome"] != null)
                                {
                                    string parsed_config = Config["GeneralMessages", "SetupHome"].ToString();
                                    parsed_config = parsed_config.Replace("{command}", Config["GeneralMessages", "SetupHome"].ToString());
                                    PrintToChatEx(player, parsed_config);
                                }
                                SaveData();
                            }
                        }
                    }
                }
            }
            else
            {
                Puts("Failed to load entity data, setting default entities...");
                LoadDefaultConfig();
            }

        }
        Dictionary<string, object> GetSleepingBagData(SleepingBag bag)
        {
            var bagdata = new Dictionary<string,
                object>();

            bagdata.Add("name", bag.niceName);
            bagdata.Add("pos", bag.transform.position);

            return bagdata;
        }
        List<Dictionary<string, object>> FindSleepingBags(ulong userid)
        {
            var bags = new List<Dictionary<string,
                object>>();
            foreach (SleepingBag bag in SleepingBag.FindForPlayer(userid, true))
            {
                bags.Add(GetSleepingBagData(bag));
            }

            return bags;
        }
        private bool UnfreezePlayer(ulong steamid)
        {
            PlayerData item = null;
            if (MTData.PlayerData.TryGetValue(steamid, out item))
            {
                if (item.uCooldownEnabled)
                {
                    item.uCooldownEnabled = false;
                }
                return true;
            }
            return false;
        }
        private void CommandMessage(BasePlayer player)
        {
            if (player != null)
            {
                if (Config["GeneralMessages", "Usage"] != null) PrintToChatEx(player, Config["GeneralMessages", "Usage"].ToString());
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]) && Config["GeneralMessages", "AdminCmd"] != null)
                {
                    string parsed_config = Config["GeneralMessages", "AdminCmd"].ToString();
                    parsed_config = parsed_config.Replace("{command}", Config["Commands", "Main"].ToString());
                    parsed_config = parsed_config.Replace("{createtp}", Config["Commands", "CreateTeleport"].ToString());
                    parsed_config = parsed_config.Replace("{remove}", Config["Commands", "RemoveTeleport"].ToString());
                    PrintToChatEx(player, parsed_config);
                }
            }
        }
        [ChatCommand("t")]
        void cmdHome(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0 || args.Length > 6)
            {
                CommandMessage(player);
            }
            else if (args[0] == "clean")
            {
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                {
                    if (args.Length == 1)
                    {
                        int count = 0;
                        foreach (SleepingBag bag in SleepingBag.FindForPlayer(player.userID, true))
                        {
                            bag.Kill();
                            count++;
                        }
                        MTData.PlayerData.Remove(player.userID);
                        PrintToChatEx(player, "Removed " + count.ToString() + " homes. (" + player.displayName.ToString() + ").");
                    }
                }
            }
            else if (args[0] == Config["Commands", "Entities"].ToString())
            {
                
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                {
                    if (args.Length < 2)
                    {
                        int count = 0;
                        foreach (var entity in MTData.Entities.Values)
                        {
                            PrintToChatEx(player, "[" + count.ToString() + "] Entity: <color=yellow>" + entity.tShortname + "</color>\n[" + count.ToString() + "] Prefab: <color=yellow>" + entity.tPrefab_name + "</color>\n[" + count.ToString() + "] Enabled: <color=yellow>" + entity.bEnabled.ToString() + "</color>.");
                            count++;
                        }
                        if (count != 0) PrintToChatEx(player, "Found " + count.ToString() + " entities."); else PrintToChatEx(player, "No entities currently exist.");
                        return;
                    }
                    else
                    {
                        foreach (ItemDefinition item in ItemManager.itemList)
                        {
                            if (item.category == ItemCategory.Items)
                            {
                                if (item.displayName.english == args[1])
                                {
                                    foreach (var itemdef in deployedToItem)
                                    {
                                        if (item.itemid == itemdef.Value)
                                        {
                                            Puts(itemdef.Key + " - " + itemdef.Value);
                                            HomeEntities z = new HomeEntities();
                                            z.bEnabled = true;
                                            z.tPrefab_name = itemdef.Key;
                                            z.tShortname = item.displayName.english;
                                            MTData.Entities.Add(z.tPrefab_name, z);
                                            PrintToChatEx(player, "You have <color=green>successfully</color> added <color=yellow>" + z.tShortname + "</color> [ " + z.tPrefab_name + " ] to the entities list. Enabled: " + z.bEnabled.ToString() + ".");
                                            SaveData();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Config["GeneralMessages", "NoAuthLevel"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoAuthLevel"].ToString());
                }
            }
            else if (args[0] == Config["Commands", "RemoveTeleport"].ToString())
            {
                if (args.Length < 2)
                {
                    string parsed_config = Config["GeneralMessages", "RemoveTeleport"].ToString();
                    parsed_config = parsed_config.Replace("{command}", Config["Commands", "Main"].ToString());
                    parsed_config = parsed_config.Replace("{subcommand}", Config["Commands", "RemoveTeleport"].ToString());
                    if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                    return;
                }
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                {
                    int teleport_id = Convert.ToInt32(args[1]);
                    if (MTData.Teleports.ContainsKey(teleport_id))
                    {
                        string parsed_config = Config["GeneralMessages", "TPRemoveSuccess"].ToString();
                        parsed_config = parsed_config.Replace("{id}", args[1].ToString());
                        PrintToChatEx(player, parsed_config);
                        MTData.Teleports.Remove(teleport_id);
                    }
                    else
                    {
                        if (Config["GeneralMessages", "TPNoExist"] != null) PrintToChatEx(player, Config["GeneralMessages", "TPNoExist"].ToString());
                    }
                }
                else
                {
                    if (Config["GeneralMessages", "NoAuthLevel"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoAuthLevel"].ToString());
                }
            }
            else if (args[0] == Config["Commands", "CreateTeleport"].ToString())
            {
                int return_id = -1;
                if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                {
                    if (args.Length == 3)
                    {
                        string title = args[1];
                        string description = args[2];
                        return_id = CreateTeleport(args[1], args[2], player.transform.position.x, player.transform.position.y, player.transform.position.z);
                    }
                    else if (args.Length == 4)
                    {
                        string title = args[1];
                        string description = args[2];
                        bool sleepgod = Convert.ToBoolean(args[3]);
                        return_id = CreateTeleport(args[1], args[2], player.transform.position.x, player.transform.position.y, player.transform.position.z, sleepgod);
                    }
                    else if (args.Length == 5)
                    {
                        string title = args[1];
                        string description = args[2];
                        bool sleepgod = Convert.ToBoolean(args[3]);
                        int authlevel = Convert.ToInt32(args[4]);
                        return_id = CreateTeleport(args[1], args[2], player.transform.position.x, player.transform.position.y, player.transform.position.z, sleepgod, authlevel);
                    }
                    else if (args.Length == 6)
                    {
                        string title = args[1];
                        string description = args[2];
                        bool sleepgod = Convert.ToBoolean(args[3]);
                        int authlevel = Convert.ToInt32(args[4]);
                        bool enabled = Convert.ToBoolean(args[5]);
                        return_id = CreateTeleport(args[1], args[2], player.transform.position.x, player.transform.position.y, player.transform.position.z, sleepgod, authlevel, enabled);
                    }
                    else
                    {
                        if (Config["GeneralMessages", "CreateTeleport"] != null)
                        {
                            string parsed_config = Config["GeneralMessages", "CreateTeleport"].ToString();
                            parsed_config = parsed_config.Replace("{command}", Config["Commands", "Main"].ToString());
                            parsed_config = parsed_config.Replace("{subcommand}", Config["Commands", "CreateTeleport"].ToString());
                            if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                        }

                    }
                    if (return_id != -1 && MTData.Teleports.ContainsKey(return_id))
                    {
                        string parsed_config = Config["GeneralMessages", "TeleportCreated"].ToString();
                        parsed_config = parsed_config.Replace("{id}", return_id.ToString());
                        parsed_config = parsed_config.Replace("{title}", MTData.Teleports[return_id].tTitle);
                        parsed_config = parsed_config.Replace("{description}", MTData.Teleports[return_id].tDescription);
                        parsed_config = parsed_config.Replace("{sleepgod}", MTData.Teleports[return_id].uSleepGod.ToString());
                        parsed_config = parsed_config.Replace("{authlevel}", MTData.Teleports[return_id].iAuthLevel.ToString());
                        parsed_config = parsed_config.Replace("{enabled}", MTData.Teleports[return_id].uEnabled.ToString());
                        PrintToChatEx(player, parsed_config);
                    }
                    else
                    {
                        if (args.Length != 0 && args.Length >= 3)
                        {
                            Puts(player.displayName + " tried to create a teleport. [FAILED]");
                            if (Config["GeneralMessages", "TPCreationFailed"] != null) PrintToChatEx(player, Config["GeneralMessages", "TPCreationFailed"].ToString());
                        }
                    }
                }
                else
                {
                    if (Config["GeneralMessages", "NoAuthLevel"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoAuthLevel"].ToString());
                }
            }
            else if (args[0] == Config["Commands", "Public"].ToString()) // Public Teleports
            {
                if (args.Length == 1)
                {
                    int count = 0;
                    foreach (var item in MTData.Teleports.Values)
                    {
                        if (item.fX != 0 && item.fZ != 0)
                        {
                            if (player.net.connection.authLevel >= item.iAuthLevel)
                            {
                                count++;
                                if (!item.uEnabled && player.net.connection.authLevel < Convert.ToInt32(Config["General", "AuthLevel"])) break;
                                string parsed_config = Config["GeneralMessages", "TeleportInfo"].ToString();
                                parsed_config = parsed_config.Replace("{id}", count.ToString());
                                parsed_config = parsed_config.Replace("{title}", item.tTitle);
                                parsed_config = parsed_config.Replace("{description}", item.tDescription);
                                parsed_config = parsed_config.Replace("{tpcount}", item.iCount.ToString());

                                if (parsed_config.Length >= 1)
                                {
                                    if (player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                                    {
                                        string enabled_color = "white";
                                        if (item.uEnabled)
                                        {
                                            enabled_color = "green";
                                        }
                                        else
                                        {
                                            enabled_color = "red";
                                        }
                                        PrintToChatEx(player, parsed_config + "\n[ <color=#33CCFF>" + count.ToString() + "</color> ] <color=red>ID:</color> " + item.iID.ToString() + " : <color=red>Authlevel:</color> " + item.iAuthLevel.ToString() + " : " + "<color=red>Enabled:</color> <color=" + enabled_color.ToString() + ">" + item.uEnabled.ToString() + "</color> : <color=red>Sleep God:</color> " + item.uSleepGod.ToString() + ".");
                                    }
                                    else
                                    {
                                        PrintToChatEx(player, parsed_config);
                                    }
                                }
                            }
                        }
                    }
                    if (count == 0)
                    {
                        if (Config["GeneralMessages", "NoTeleports"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoTeleports"].ToString());
                    }
                    else
                    {
                        string parsed_config = Config["GeneralMessages", "PublicTP"].ToString();
                        parsed_config = parsed_config.Replace("{command}", Config["Commands", "Main"].ToString());
                        parsed_config = parsed_config.Replace("{subcommand}", Config["Commands", "Public"].ToString());
                        PrintToChatEx(player, parsed_config);
                    }
                }
                else if (args.Length == 2)
                {
                    int count = 0;
                    int foundcount = 0;
                    PlayerData d = null;
                    if (MTData.PlayerData.TryGetValue(player.userID, out d))
                    {
                        foreach (var item in MTData.Teleports.Values)
                        {
                            if (item.fX != 0 && item.fZ != 0)
                            {
                                count++;
                                if (args[1].ToString() == count.ToString())
                                {
                                    if (!TELEPORT_QUEUE.ContainsKey(player.userID))
                                    {
                                        if (item.iAuthLevel == 0 || player.net.connection.authLevel >= item.iAuthLevel)
                                        {
                                            if (!d.uCooldownEnabled)
                                            {
                                                if (Convert.ToBoolean(Config["TPSettings", "SanityCheck"]))
                                                {
                                                    string reason = IsTeleportationCapable(player);
                                                    if (reason != "continue")
                                                    {
                                                        if (reason.Length >= 1) PrintToChatEx(player, reason);
                                                        return;
                                                    }

                                                }
                                                foundcount++;
                                                item.iCount++;
                                                InitTeleport(player, Convert.ToSingle(item.fX), Convert.ToSingle(item.fY), Convert.ToSingle(item.fZ), false, true, item.tTitle, item.tDescription, count, Convert.ToInt32(Config["TPSettings", "Cooldown"]));

                                            }
                                            else
                                            {

                                                string parsed_config = Config["TPSettings", "TPCooldown"].ToString();
                                                parsed_config = parsed_config.Replace("{cooldown}", Convert.ToInt32(Config["TPSettings", "Cooldown"]).ToString());
                                                if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                                            }
                                        }
                                        else
                                        {
                                            string parsed_config = Config["GeneralMessages", "TPNoExist"].ToString();
                                            parsed_config = parsed_config.Replace("{id}", args[1].ToString());
                                            if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                                        }
                                    }
                                    else
                                    {
                                        if (Config["GeneralMessages", "TeleportPending"] != null) PrintToChatEx(player, Config["GeneralMessages", "TeleportPending"].ToString());
                                    }
                                    return;
                                }
                            }
                        }
                    }
                    if (foundcount == 0)
                    {
                        string parsed_config = Config["GeneralMessages", "TPNoExist"].ToString();
                        parsed_config = parsed_config.Replace("{id}", args[1].ToString());
                        if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                    }
                }
            }
            else if (args[0] == Config["Commands", "Home"].ToString()) // Home Teleport
            {
                if (args.Length == 1)
                {
                    int count = 0;
                    PlayerData d = null;
                    if (MTData.PlayerData.TryGetValue(player.userID, out d))
                    {
                        if (d.HomesLocs.Count == 0)
                        {
                            PrintToChatEx(player, Config["GeneralMessages", "NoHomes"].ToString());
                            return;
                        }
                        SyncHomesEx(player);
                        foreach (var item in d.HomesLocs)
                        {
                            if (item.Value.fX != 0 && item.Value.fZ != 0)
                            {
                                count++;
                                string parsed_config = Config["GeneralMessages", "HomeInfo"].ToString();
                                parsed_config = parsed_config.Replace("{id}", count.ToString());
                                parsed_config = parsed_config.Replace("{title}", item.Value.tBagName);
                                parsed_config = parsed_config.Replace("{hp}", item.Value.fHealth.ToString());
                                if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);

                            }
                        }
                        if (count == 0)
                        {
                            if (Config["GeneralMessages", "NoHomes"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoHomes"].ToString());
                        }
                        else
                        {
                            string parsed_config = Config["GeneralMessages", "HomeTP"].ToString();
                            parsed_config = parsed_config.Replace("{command}", Config["Commands", "Main"].ToString());
                            parsed_config = parsed_config.Replace("{subcommand}", Config["Commands", "Home"].ToString());
                            PrintToChatEx(player, parsed_config);
                        }
                    }
                    else
                    {
                        if (Config["GeneralMessages", "NoHomes"] != null) PrintToChatEx(player, Config["GeneralMessages", "NoHomes"].ToString());
                    }
                }
                else if (args.Length == 2)
                {
                    int count = 0;
                    int foundcount = 0;
                    PlayerData d = null;
                    if (MTData.PlayerData.TryGetValue(player.userID, out d))
                    {
                        foreach (var item in d.HomesLocs)
                        {
                            if (item.Value.fX != 0 && item.Value.fZ != 0)
                            {
                                count++;
                                if (args[1].ToString() == count.ToString())
                                {
                                    foundcount++;
                                    if (!TELEPORT_QUEUE.ContainsKey(player.userID))
                                    {
                                        if (!d.uCooldownEnabled)
                                        {
                                            if (Convert.ToBoolean(Config["HomeSettings", "SanityCheck"]))
                                            {
                                                string reason = IsTeleportationCapable(player);
                                                if (reason != "continue")
                                                {
                                                    if (reason.Length >= 1) PrintToChatEx(player, reason);
                                                    return;
                                                }
                                            }
                                            InitTeleport(player, Convert.ToSingle(item.Value.fX), Convert.ToSingle(item.Value.fY), Convert.ToSingle(item.Value.fZ), true, true, item.Value.tBagName.ToString());
                                        }
                                        else
                                        {
                                            string parsed_config = Config["GeneralMessages", "TPCooldown"].ToString();
                                            parsed_config = parsed_config.Replace("{cooldown}", Convert.ToInt32(Config["HomeSettings", "Cooldown"]).ToString());
                                            if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                                        }
                                    }
                                    else
                                    {
                                        if (Config["GeneralMessages", "TeleportPending"] != null) PrintToChatEx(player, Config["GeneralMessages", "TeleportPending"].ToString());
                                    }
                                    return;
                                }
                            }
                        }
                    }
                    if (count == 0 || foundcount == 0)
                    {
                        string parsed_config = Config["GeneralMessages", "HomeNoExist"].ToString();
                        parsed_config = parsed_config.Replace("{id}", args[1].ToString());
                        if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                    }
                }
            }
            else
            {
                CommandMessage(player);
            }
        }
        private bool InitTeleport(BasePlayer player, float init_x, float init_y, float init_z, bool type = true, bool printtoplayer = true, string title = "", string description = "", int count = -1, int seconds = 0)
        {
            if(!PlayerExists(player)) InitPlayer(player);
            if (SLEEPING_TELEPORTERS.Contains(player.userID)) { if (!player.IsSleeping()) { SLEEPING_TELEPORTERS.Remove(player.userID); } }
            if (TELEPORT_QUEUE.ContainsKey(player.userID)) { return false; }
            PlayerData d = null;
            if (MTData.PlayerData.TryGetValue(player.userID, out d)) { d.uCooldownEnabled = true; }
            float x = Convert.ToSingle(init_x);
            float y = Convert.ToSingle(init_y);
            float z = Convert.ToSingle(init_z);
            if (seconds == 0)
            {
                seconds = Convert.ToInt32(Config["HomeSettings", "TPWait"]);
            }
            if (type)
            {
                if (title != "")
                {
                    string parsed_config = Config["GeneralMessages", "TPHome"].ToString();
                    parsed_config = parsed_config.Replace("{seconds}", seconds.ToString());
                    parsed_config = parsed_config.Replace("{title}", title);
                    if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                }
                TELEPORT_QUEUE.Add(player.userID, timer.Once(seconds, () => TeleportPlayerPosition(player, new Vector3(x, y + Convert.ToInt32(Config["Settings", "EntityHeight"]), z))));
            }
            else
            {
                if (title != "")
                {
                    string parsed_config = Config["GeneralMessages", "TPGeneral"].ToString();
                    parsed_config = parsed_config.Replace("{title}", title.ToString());
                    parsed_config = parsed_config.Replace("{seconds}", seconds.ToString());
                    parsed_config = parsed_config.Replace("{tpcount}", count.ToString());
                    if (parsed_config.Length >= 1) PrintToChatEx(player, parsed_config);
                }
                TELEPORT_QUEUE.Add(player.userID, timer.Once(seconds, () => TeleportPlayerPosition(player, new Vector3(x, y, z))));
            }
            return true;
        }
        //--------------------------->   Position forcing   <---------------------------//

        private void TeleportPlayerPosition(BasePlayer player, Vector3 pos)
        {
			if(player != null)
			{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
				if(!BasePlayer.sleepingPlayerList.Contains(player))	BasePlayer.sleepingPlayerList.Add(player);
				SLEEPING_TELEPORTERS.Add(player.userID);
				player.CancelInvoke("InventoryUpdate");
				player.inventory.crafting.CancelAll(true);
				player.MovePosition(pos);
				player.ClientRPCPlayer(null, player, "ForcePositionTo", pos, null, null, null, null);
				player.TransformChanged();
				player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
				player.UpdateNetworkGroup();
				player.SendNetworkUpdateImmediate(false);
				player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
				player.SendFullSnapshot();
				timer.Once(Convert.ToInt32(Config["HomeSettings", "Cooldown"]), () => UnfreezePlayer(player.userID));
				TELEPORT_QUEUE[player.userID].Destroy();
				TELEPORT_QUEUE.Remove(player.userID);
			}
        }
        void Unload()
        {
            SaveData();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerExists(player)) InitPlayer(player);
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (SLEEPING_TELEPORTERS.Contains(player.userID))
            {
                SLEEPING_TELEPORTERS.Remove(player.userID);
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (SLEEPING_TELEPORTERS.Contains(player.userID))
            {
                SLEEPING_TELEPORTERS.Remove(player.userID);
            }
            if (TELEPORT_QUEUE.ContainsKey(player.userID))
            {
                TELEPORT_QUEUE.Remove(player.userID);
            }
        }
        Timer SyncHomeData;
        void OnServerInitialized()
        {
            InitializeTable();
            InitializeConfig();
        }
        void InitializeConfig()
        {
            if(Convert.ToBoolean(Config["General", "PopulateDefaults"])) { Puts("Generating default entities...");  Config["General", "PopulateDefaults"] = false; SaveConfig(); PopulateEntityData(); }
            if (Config["HomeMessages", "ExternalReason"] == null)
            {
                Puts("Updating configuration file (out of date)...");
                Config["HomeMessages", "ExternalReason"] = "You <color=#FF0000>can't</color> teleport from here.";
                SaveConfig();
            }
            Puts("Populated " + MTData.Entities.Count.ToString() + " entities.");
            int seconds = Convert.ToInt32(Config["Settings", "UpdateTimerInt"]);
            SyncHomeData = timer.Repeat(seconds, 0, () => SyncHomes());
            int player_count = ResetCooldownForAll();
            if (player_count >= 1)
            {
                Puts(player_count.ToString() + " players had their cooldown reset to default.");
            }
            if (!BuildingOwners && Convert.ToBoolean(Config["Dependencies", "BuildingOwners"]))
            {
                Puts("[BuildingOwners][682]: Plugin has not been found! [ BuildingOwners : false ]");
                Config["Dependencies", "BuildingOwners"] = false;
                SaveConfig();
            }
            if (!DeadPlayersList && Convert.ToBoolean(Config["Dependencies", "DeadPlayersList"]))
            {
                Puts("[DeadPlayersList][696]: Plugin has not been found! [ DeadPlayersList : false ]");
                Config["Dependencies", "DeadPlayersList"] = false;
                SaveConfig();
            }
            Puts("[Building Owners][682]: [Enabled: " + Config["Dependencies", "BuildingOwners"].ToString() + "] | [Dead Players List][696]: [Enabled: " + Config["Dependencies", "DeadPlayersList"] + "]");
        }
        void SyncHomesEx(BasePlayer player)
        {
            if (player != null)
            {
                if (MTData.PlayerData.ContainsKey(player.userID))
                {
                    if (SleepingBag.FindForPlayer(player.userID, true).Length == 0)
                    {
                        if (MTData.PlayerData[player.userID].HomesLocs.Count >= 1)
                        {
                            MTData.PlayerData[player.userID].HomesLocs.Clear();
                            Puts("Resetting: " + player.displayName + "'s [" + player.userID.ToString() + "] homes list.");
                            return;
                        }
                    }
                    List<uint> ids = new List<uint>();
                    List<uint> remove_list = new List<uint>();
                    foreach (SleepingBag bag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        foreach (var entry in MTData.Entities)
                        {
                            if (entry.Value.bEnabled)
                            {
                                if (entry.Value.tPrefab_name == bag.LookupPrefabName())
                                {
                                    int bag_id = (int)bag.net.ID;
                                    if (MTData.PlayerData[player.userID].HomesLocs.ContainsKey(bag_id))
                                    {
                                        if (MTData.PlayerData[player.userID].HomesLocs[bag_id].tBagName != bag.niceName)
                                        {
                                            MTData.PlayerData[player.userID].HomesLocs[bag_id].tBagName = bag.niceName;
                                        }
                                        if (MTData.PlayerData[player.userID].HomesLocs[bag_id].fHealth != bag.health)
                                        {
                                            MTData.PlayerData[player.userID].HomesLocs[bag_id].fHealth = bag.health;
                                        }
                                        ids.Add(bag.net.ID);
                                    }
                                }
                            }
                        }
                    }
                    foreach (var home in MTData.PlayerData[player.userID].HomesLocs)
                    {
                        if (!ids.Contains((uint)home.Key)) remove_list.Add((uint)home.Key);
                    }
                    new List<uint>(remove_list).ForEach(u => {
                        MTData.PlayerData[player.userID].HomesLocs.Remove((int)u);
                    });
                }
            }
        }
        void SyncHomes()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SyncHomesEx(player);
            }
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, MTData);
        }
        private string IsTeleportationCapable(BasePlayer player)
        {
            string result = "";
            string tp_correct = "continue";
            if (player != null && player.IsConnected())
            {
                if (Convert.ToBoolean(Config["Settings", "BypassAdmin"]) && player.net.connection.authLevel >= Convert.ToInt32(Config["General", "AuthLevel"]))
                {
                    return tp_correct;
                }
                float min_hp = (float)Convert.ToDouble(Config["HomeSettings", "MinimumHealthCheck"]);
                float health = player.health;
                float temperature = player.currentTemperature;
                float comfort = player.currentComfort;
                var cantp = Interface.Call("canTeleport", player);
                if (health <= min_hp)
                {
                    result = Config["HomeMessages", "MinHP"].ToString();
                    result = result.Replace("{minhp}", min_hp.ToString());
                }
                else if (player.IsWounded()) { result = Config["HomeMessages", "Wounded"].ToString(); }
                else if (player.IsOnFire()) { result = Config["HomeMessages", "Fire"].ToString(); }
                else if (player.IsSwimming()) { result = Config["HomeMessages", "Swimming"].ToString(); }
                else if (!player.IsAlive()) { result = Config["HomeMessages", "Alive"].ToString(); }
                else if (!player.CanBuild() && Convert.ToBoolean(Config["HomeSettings", "TPInBlockedArea"])) { result = Config["HomeMessages", "BuildingBlocked"].ToString(); }
                else if (temperature < 0 && comfort <= 0 && !Convert.ToBoolean(Config["HomeSettings", "BypassCold"]))
                {
                    result = Config["HomeMessages", "TooCold"].ToString();
                    result = result.Replace("{temperature}", temperature.ToString());
                }
                else if (cantp != null) { if (cantp is string) { result = Convert.ToString(cantp); } else { result = Config["HomeMessages", "ExternalReason"].ToString(); } }
                else { result = tp_correct; } if (result.Length == 0) { result = Config["HomeMessages", "Failed"].ToString(); }
            }
            return result;
        }
        private string FindPlayerName(ulong userId)
        {
            BasePlayer player = BasePlayer.FindByID(userId);
            if (player) return player.displayName + " (Online)";

            player = BasePlayer.FindSleeping(userId);
            if (player) return player.displayName + " (Sleeping)";
            if (DeadPlayersList)
            {
                string name = DeadPlayersList?.Call("GetPlayerName", userId) as string;
                if (name != null) return name + " (Dead)";
            }
            return "Unknown";
        }
        private void GetDeployedItemOwner(BasePlayer player, SleepingBag ditem)
        {
            SendReply(player, string.Format("Sleeping Bag '{0}': {1} - {2}", ditem.niceName.ToString(), FindPlayerName(ditem.deployerUserID), ditem.deployerUserID.ToString()));
        }
        private object FindOwnerBlock(BuildingBlock block)
        {
            if (BuildingOwners)
            {
                object returnhook = BuildingOwners?.Call("FindBlockData", block);

                if (returnhook != null)
                {
                    if (!(returnhook is bool))
                    {
                        ulong ownerid = Convert.ToUInt64(returnhook);
                        return ownerid;
                    }
                }
            }
            else
            {
                Puts("To be able to obtain the owner of a building you need to install the BuildingOwner plugin.");
            }
            return false;
        }
        protected override void LoadDefaultConfig()
        {
            // -- [ RESET ] ---

            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL ] ---

            Config["General", "PopulateDefaults"] = true;
            Config["General", "ShowPluginName"] = false;
            Config["General", "Protocol"] = Protocol.network;
            Config["General", "AuthLevel"] = 2;

            // --- [ DEPENDENCIES ] ---

            Config["Dependencies", "BuildingOwners"] = true;
            Config["Dependencies", "DeadPlayersList"] = true;
            Config["Dependencies", "PopupNotifications"] = false;

            // --- [ SETTINGS ] ---

            Config["Settings", "TPSleepGod"] = true;
            Config["Settings", "UpdateTimerInt"] = 10;
            Config["Settings", "MaxEntitiesPerBuilding"] = 2;
            Config["Settings", "BypassAdmin"] = false;
            Config["Settings", "EntityHeight"] = 2;
            Config["Settings", "RefundEntity"] = true;

            // --- [ TELEPORT SETTINGS ] ---

            Config["TPSettings", "Cooldown"] = 30;
            Config["TPSettings", "TPWait"] = 3;
            Config["TPSettings", "TPInBlockedArea"] = true;
            Config["TPSettings", "BypassCold"] = false;
            Config["TPSettings", "MinimumHealthCheck"] = 51.00;
            Config["TPSettings", "SanityCheck"] = true;

            // --- [ HOME SETTINGS ] ---

            Config["HomeSettings", "MaxHomes"] = 4;
            Config["HomeSettings", "Cooldown"] = 5;
            Config["HomeSettings", "TPWait"] = 8;
            Config["HomeSettings", "MinimumHealthCheck"] = 30.00;
            Config["HomeSettings", "SanityCheck"] = true;
            Config["HomeSettings", "TPInBlockedArea"] = true;
            Config["HomeSettings", "BypassCold"] = false;

            // --- [ COMMANDS ] ---

            Config["Commands", "Main"] = "t"; // Main Command
            Config["Commands", "Home"] = "home"; // Home Teleport
            Config["Commands", "Public"] = "list"; // Public Teleports
            Config["Commands", "CreateTeleport"] = "create"; // Create Public Teleports - sub command
            Config["Commands", "RemoveTeleport"] = "remove";
            Config["Commands", "Entities"] = "entity";

            // --- [ MESSAGES ] ---

            Config["GeneralMessages", "Usage"] = "<color=#33CCFF>USAGE:</color> /" + Config["Commands", "Main"].ToString() + " <" + Config["Commands", "Home"].ToString() + " | " + Config["Commands", "Public"].ToString() + ">";
            Config["GeneralMessages", "DBCleared"] = "You have <color=#FF3300>cleared</color> the Magic Homes database.";
            Config["GeneralMessages", "NoAuthLevel"] = "You <color=#FF3300>do not</color> have access to this command.";
            Config["GeneralMessages", "HomeDestroyed"] = "You have <color=#FF0000>destroyed</color> your home (<color=#FFFF00>{home}</color>).";
            Config["GeneralMessages", "MaxHomes"] = "You have reached your maximum allowed homes. ({max_homes})";
            Config["GeneralMessages", "SetupHome"] = "You have setup a new home! (Use /" + Config["Commands", "Main"].ToString() + " <" + Config["Commands", "Home"].ToString() + "> at any time).";
            Config["GeneralMessages", "NoHomes"] = "You have <color=red>no</color> homes.";
            Config["GeneralMessages", "NoTeleports"] = "There is currently <color=red>nowhere</color> to teleport to.";
            Config["GeneralMessages", "AdminCmd"] = "<color=yellow>ADMIN:</color> /{command} <{createtp} | {remove} | entity | clean>";
            Config["GeneralMessages", "TeleportInterrupted"] = "Your teleport has been <color=red>interrupted</color>...";
            Config["GeneralMessages", "HomeInfo"] = "[ <color=#33CCFF>{id}</color> ] <color=#FFFF00>{title}</color>, HP: <color=#FF0000>{hp}</color>.";
            Config["GeneralMessages", "TeleportInfo"] = "[ <color=#33CCFF>{id}</color> ] <color=yellow>Name:</color> {title}, <color=yellow>Description:</color> {description}. (<color=#33CCFF>{tpcount}</color>)";
            Config["GeneralMessages", "TPCooldown"] = "You are not currently allowed to teleport. (<color=#FF0000>{cooldown} second cooldown</color>).";
            Config["GeneralMessages", "TPHome"] = "You will be teleported to your home in <color=#FFFF00>{seconds}</color> seconds (<color=#FFFF00>{title}</color>).";
            Config["GeneralMessages", "TPGeneral"] = "You will be teleported to {title} in <color=#FFFF00>{seconds}</color> seconds (<color=#FFFF00>{tpcount}</color>).";
            Config["GeneralMessages", "HomeNoExist"] = "That home does <color=red>not</color> exist. [<color=#FFFF00>{id}</color>]";
            Config["GeneralMessages", "TPNoExist"] = "That teleport does <color=red>not</color> exist. [<color=#FFFF00>{id}</color>]";
            Config["GeneralMessages", "PlayerNoAwake"] = "You <color=red>cannot</color> attack someone who has not woken up from teleporting.";
            Config["GeneralMessages", "TeleportCreated"] = "Created teleport <color=#FF0000>{id}</color> at your current location!\nTitle: {title} : <color=yellow>Description:</color> {description},\n<color=yellow>Sleep God:</color> {sleepgod} | <color=yellow>Auth Level:</color> {authlevel} | Enabled: <color=yellow>{enabled}</color>.";
            Config["GeneralMessages", "CreateTeleport"] = "<color=yellow>USAGE:</color> /{command} {subcommand}\n<color=red>title</color> | <color=red>description</color> | <color=yellow>sleepgod</color> (<color=green>true</color>/<color=red>false</color>) | <color=yellow>authlevel</color> | <color=yellow>enabled</color> (<color=green>true</color>/<color=red>false</color>).";
            Config["GeneralMessages", "RemoveTeleport"] = "<color=yellow>USAGE:</color> /{command} {subcommand} <id>";
            Config["GeneralMessages", "TPCreationFailed"] = "<color=yellow>ERROR:</color> Failed to create teleport.";
            Config["GeneralMessages", "HomeTP"] = "<color=yellow>USAGE:</color> /{command} {subcommand} <id>.";
            Config["GeneralMessages", "PublicTP"] = "<color=yellow>USAGE:</color> /{command} {subcommand} <id>.";
            Config["GeneralMessages", "TeleportPending"] = "You already have a teleport <color=red>pending</color>.";
            Config["GeneralMessages", "TPRemoveSuccess"] = "<color=yellow>INFO:</color> You have removed the teleport: {id}!";

            // --- [ SUB MESSAGES ] ---

            Config["HomeMessages", "MinHP"] = "Your health <color=#FF0000>needs</color> to be above <color=#FF0000>{minhp}</color> to teleport home.";
            Config["HomeMessages", "Wounded"] = "You <color=#FF0000>can't</color> teleport home when you're <color=#FF0000>wounded</color>.";
            Config["HomeMessages", "Fire"] = "You <color=#FF0000>can't</color> teleport home when you're on <color=#FF0000>fire</color>.";
            Config["HomeMessages", "Swimming"] = "You <color=#FF0000>can't</color> teleport home when you're <color=#FF0000>swimming</color>.";
            Config["HomeMessages", "Alive"] = "You <color=#FF0000>can't</color> teleport home when you're not even <color=#FF0000>alive</color>.";
            Config["HomeMessages", "BuildingBlocked"] = "You <color=#FF0000>can't</color> teleport home when you're in a <color=#FF0000>building blocked</color> area.";
            Config["HomeMessages", "TooCold"] = "It's too <color=#FF0000>cold</color> to teleport. (<color=#00E1FF>{temperature}</color>)";
            Config["HomeMessages", "Failed"] = "<color=#FF0000>Failed</color> to teleport, contact an administrator.";
            Config["HomeMessages", "ExternalReason"] = "You <color=#FF0000>can't</color> teleport from here.";

            // --- [ OTHER ] ----

            SaveConfig();
        }
        int PopulateEntityData()
        {
            int r = 0;
            MTData.Entities.Clear();
            foreach (var entity in DEFAULT_HomeEntities)
            {
                HomeEntities z = new HomeEntities();
                z.bEnabled = true;
                z.tPrefab_name = entity.Key.ToString();
                z.tShortname = entity.Value.ToString();
                MTData.Entities.Add(z.tPrefab_name, z);
                SaveData();
                r++;
            }
            return r;
        }
        private void PrintToChatEx(BasePlayer player, string result, string tcolour = "#66FF66")
        {
            if (!Convert.ToBoolean(Config["Dependencies", "PopupNotifications"]))
            {
                if (Convert.ToBoolean(Config["General", "ShowPluginName"]))
                {
                    PrintToChat(player, "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result);
                }
                else
                {
                    PrintToChat(player, result);
                }
            }
            else
            {
                if (PopupNotifications)
                {
                    if (Convert.ToBoolean(Config["General", "ShowPluginName"]))
                    {
                        PopupNotifications?.Call("CreatePopupNotification", "<color=" + tcolour + ">" + this.Title.ToString() + "</color>\n" + result, player);
                    }
                    else
                    {
                        PopupNotifications?.Call("CreatePopupNotification", result, player);
                    }
                }
            }
        }
    }
}