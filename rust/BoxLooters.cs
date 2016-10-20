using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("BoxLooters", "4seti / k1lly0u", "0.3.1", ResourceId = 989)]
    class BoxLooters : RustPlugin
    {
        #region Fields
        BoxDS boxData;
        PlayerDS playerData;
        private DynamicConfigFile bdata;
        private DynamicConfigFile pdata;

        private Vector3 eyesAdjust;
        private FieldInfo serverinput;

        private bool eraseData = false;

        private Dictionary<uint, BoxData> boxCache;
        private Dictionary<ulong, PlayerData> playerCache;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            bdata = Interface.Oxide.DataFileSystem.GetFile("Boxlooters/box_data");
            pdata = Interface.Oxide.DataFileSystem.GetFile("Boxlooters/player_data");

            eyesAdjust = new Vector3(0f, 1.5f, 0f);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            boxCache = new Dictionary<uint, BoxData>();
            playerCache = new Dictionary<ulong, PlayerData>();

            lang.RegisterMessages(messages, this);
            permission.RegisterPermission("boxlooters.checkbox", this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            if (eraseData)
                ClearAllData();
            else RemoveOldData();
        }
        void OnNewSave(string filename) => eraseData = true;        
        void OnServerSave() => SaveData();
        void Unload() => SaveData();

        void OnLootEntity(BasePlayer looter, BaseEntity entity)
        {
            if (looter == null || entity == null || !IsValidType(entity)) return;

            var time = GrabCurrentTime();
            var date = DateTime.Now.ToString("d/M HH:mm:ss");
            var lootEntry = new LootEntry
            {
                FirstLoot = date,
                LastInit = time,
                LastLoot = date,
                Name = looter.displayName
            };
            
            if (entity is BasePlayer)
            {
                var looted = entity.ToPlayer();
                if (!playerCache.ContainsKey(looted.userID))
                    playerCache.Add(looted.userID, new PlayerData(time, looter));
                else
                {
                    playerCache[looted.userID].lastInit = time;
                    playerCache[looted.userID].AddLoot(looter, time, date);
                }
            }
            else
            {
                if (entity?.net?.ID == null) return;
                var boxId = entity.net.ID;
                if (!boxCache.ContainsKey(boxId))
                    boxCache.Add(boxId, new BoxData(time, looter, entity.transform.position));
                else
                {
                    boxCache[boxId].lastInit = time;
                    boxCache[boxId].AddLoot(looter, time, date);               
                }
            }

        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (entity == null || !IsValidType(entity) || entity is BasePlayer) return;
                if (hitInfo?.Initiator is BasePlayer)
                {
                    if (entity?.net?.ID == null) return;
                    var boxId = entity.net.ID;
                    if (!boxCache.ContainsKey(boxId)) return;
                    boxCache[boxId].SetKiller(hitInfo.InitiatorPlayer.userID, hitInfo.InitiatorPlayer.displayName);
                }
            }
            catch { }
        }
        #endregion

        #region Data Cleanup
        void ClearAllData()
        {
            PrintWarning("Detected map wipe, resetting loot data!");
            boxCache.Clear();
            playerCache.Clear();
        }
        void RemoveOldData()
        {
            PrintWarning("Attempting to remove old log entries");
            int boxCount = 0;
            int playerCount = 0;
            var time = GrabCurrentTime() - (configData.RemoveHours * 3600);
            for (int j = 0; j < boxCache.Count; j++)
            {
                var ekey = boxCache.Keys.ToList()[j];
                var entry = boxCache[ekey];
                if (entry.lastInit < time)
                {
                    boxCache.Remove(ekey);
                    boxCount++;
                    continue;
                }
                for (int i = 0; i < entry.Looters.Count; i++)
                {
                    var key = entry.Looters.Keys.ToList()[i];
                    var looter = entry.Looters[key];
                    if (looter.LastInit < time)
                    {
                        entry.Looters.Remove(key);
                        boxCount++;
                    }
                }
            }
            PrintWarning($"Removed {boxCount} old records from BoxData");
            for (int j = 0; j < playerCache.Count; j++)
            {
                var ekey = playerCache.Keys.ToList()[j];
                var entry = playerCache[ekey];
                if (entry.lastInit < time)
                {
                    playerCache.Remove(ekey);
                    playerCount++;
                    continue;
                }
                for (int i = 0; i < entry.Looters.Count; i++)
                {
                    var key = entry.Looters.Keys.ToList()[i];
                    var looter = entry.Looters[key];
                    if (looter.LastInit < time)
                    {
                        entry.Looters.Remove(key);
                        playerCount++;
                    }
                }
            }
            PrintWarning($"Removed {playerCount} old records from PlayerData");
        }
        #endregion

        #region Functions
        object FindBoxFromRay(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(input.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 20))
                return null;

            var hitEnt = hit.collider.GetComponentInParent<BaseEntity>();
            if (hitEnt != null)
            {
                if (IsValidType(hitEnt))
                    return hitEnt;
            }
            return null;            
        }
        void ReplyInfo(BasePlayer player, string Id, int replies = 10, bool isPlayer = false, string additional = "")
        {
            var entId = Id;
            if (!string.IsNullOrEmpty(additional))
                entId = $"{additional} - {Id}";

            if (!isPlayer)
            {                
                if (boxCache.ContainsKey(uint.Parse(Id)))
                {
                    var box = boxCache[uint.Parse(Id)];
                    SendReply(player, string.Format(lang.GetMessage("BoxData", this, player.UserIDString), entId));

                    if (!string.IsNullOrEmpty(box.destroyName))
                        SendReply(player, string.Format(lang.GetMessage("DetectDestr", this, player.UserIDString), box.destroyName, box.destroyId));

                    int i = 1;
                    string response = "";
                    foreach (var data in box.Looters.OrderByDescending(x => x.Value.LastInit))
                    {
                        if (i > replies) return;
                        response += string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot);
                        i++;
                        if (i > replies)
                            response += "/n";
                    }
                    SendReply(player, response);
                }
                else SendReply(player, string.Format(lang.GetMessage("NoLooters", this, player.UserIDString), entId));
            }
            else
            {
                if (playerCache.ContainsKey(ulong.Parse(Id)))
                {
                    SendReply(player, string.Format(lang.GetMessage("PlayerData", this, player.UserIDString), entId));
                    int i = 1;
                    string response = "";
                    foreach (var data in playerCache[ulong.Parse(Id)].Looters.OrderByDescending(x => x.Value.LastInit))
                    {
                        if (i > replies) return;
                        response += string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot);
                        i++;
                        if (i > replies)
                            response += "/n";
                    }
                }
                else SendReply(player, string.Format(lang.GetMessage("NoLootersPlayer", this, player.UserIDString), entId));
            }
        }
        #endregion

        #region Helpers
        double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        bool HasPermission(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "boxlooters.checkbox") || player.net.connection.authLevel > 0;
        float GetDistance(Vector3 init, Vector3 target) => Vector3.Distance(init, target);
        bool IsValidType(BaseEntity entity) => !entity.GetComponent<LootContainer>() && (entity is StorageContainer || entity is MiningQuarry || entity is ResourceExtractorFuelStorage || entity is BasePlayer);
        #endregion

        #region Commands
        [ChatCommand("box")]
        void cmdBox(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player)) return;
            if (args == null || args.Length == 0)
            {
                var success = FindBoxFromRay(player);
                if (success is MiningQuarry)
                {
                    var children = (success as MiningQuarry).children;
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            if (child.GetComponent<StorageContainer>())
                            {
                                ReplyInfo(player, child.net.ID.ToString(), 5, false, child.ShortPrefabName);
                            }
                        }
                    }
                    else SendReply(player, lang.GetMessage("Nothing", this, player.UserIDString));
                }
                else if (success is BaseEntity)
                    ReplyInfo(player, (success as BaseEntity).net.ID.ToString());

                else SendReply(player, lang.GetMessage("Nothing", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "help":
                    {
                        SendReply(player, $"<color=#4F9BFF>{Title}  v{Version}</color>");
                        SendReply(player, "<color=#4F9BFF>/box help</color> - Display the help menu");
                        SendReply(player, "<color=#4F9BFF>/box</color> - Retrieve information on the box you are looking at");                        
                        SendReply(player, "<color=#4F9BFF>/box id <number></color> - Retrieve information on the specified box");
                        SendReply(player, "<color=#4F9BFF>/box near <opt:radius></color> - Show nearby boxes (current and destroyed) and their ID numbers");
                        SendReply(player, "<color=#4F9BFF>/box player <partialname/id></color> - Retrieve loot information about a player");
                        SendReply(player, "<color=#4F9BFF>/box clear</color> - Clears all saved data");
                        SendReply(player, "<color=#4F9BFF>/box save</color> - Saves box data");
                    }
                    return;
                case "id":
                    if (args.Length >= 2)
                    {
                        uint id;
                        if (uint.TryParse(args[1], out id))                        
                            ReplyInfo(player, id.ToString());                        
                        else SendReply(player, lang.GetMessage("NoID", this, player.UserIDString));
                        return;
                    }
                    break;
                case "near":
                    {
                        float radius = 20f;
                        if (args.Length >= 2)
                        {
                            if (!float.TryParse(args[1], out radius))
                                radius = 20f;
                        }
                        foreach(var box in boxCache)
                        {
                            if (GetDistance(player.transform.position, box.Value.GetPosition()) <= radius)
                            {
                                player.SendConsoleCommand("ddraw.text", 20f, Color.green, box.Value.GetPosition() + new Vector3(0, 1.5f, 0), $"<size=40>{box.Key}</size>");
                                player.SendConsoleCommand("ddraw.box", 20f, Color.green, box.Value.GetPosition(), 1f);
                            }
                        }
                    }
                    return;
                case "player":
                    if (args.Length >= 2)
                    {
                        var target = covalence.Players.FindPlayer(args[1]);
                        if (target != null)                        
                            ReplyInfo(player, target.Id, 10, true);
                        else SendReply(player, lang.GetMessage("NoPlayer", this, player.UserIDString));
                        return;
                    }
                    break;
                case "clear":
                    boxCache.Clear();
                    playerCache.Clear();
                    SendReply(player, lang.GetMessage("ClearData", this, player.UserIDString));
                    return;
                case "save":
                    SaveData();
                    SendReply(player, lang.GetMessage("SavedData", this, player.UserIDString));
                    return;
                default:
                    break;
            }
            SendReply(player, lang.GetMessage("SynError", this, player.UserIDString));
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int RemoveHours { get; set; }            
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                RemoveHours = 48
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management        
        class BoxData
        {
            public double lastInit;
            public float x, y, z;
            public ulong destroyId;
            public string destroyName;
            public Dictionary<ulong, LootEntry> Looters;
           
            public BoxData() { }
            public BoxData(double time, BasePlayer player, Vector3 pos)
            {
                lastInit = time;
                x = pos.x;
                y = pos.y;
                z = pos.z;
                Looters = new Dictionary<ulong, LootEntry>
                {
                    { player.userID, new LootEntry
                    {
                        FirstLoot = DateTime.Now.ToString("d/M HH:mm:ss"),
                        LastInit = time,
                        LastLoot = DateTime.Now.ToString("d/M HH:mm:ss"),
                        Name = player.displayName
                    }}
                };
            }
            public void AddLoot(BasePlayer looter, double time, string date)
            {
                if (Looters.ContainsKey(looter.userID))
                {
                    Looters[looter.userID].LastInit = time;
                    Looters[looter.userID].LastLoot = date;
                }
                else Looters.Add(looter.userID, new LootEntry
                {
                    FirstLoot = date,
                    LastInit = time,
                    LastLoot = date,
                    Name = looter.displayName
                });
            }
            public void SetKiller(ulong Id, string name)
            {
                destroyId = Id;
                destroyName = name;
            }
            public Vector3 GetPosition() => new Vector3(x, y, z);            
        }
        class PlayerData
        {
            public double lastInit;
            public Dictionary<ulong, LootEntry> Looters;
            public PlayerData() { }
            public PlayerData(double time, BasePlayer player)
            {
                lastInit = time;
                Looters = new Dictionary<ulong, LootEntry>
                {
                    { player.userID, new LootEntry
                    {
                        FirstLoot = DateTime.Now.ToString("d/M HH:mm:ss"),
                        LastInit = time,
                        LastLoot = DateTime.Now.ToString("d/M HH:mm:ss"),
                        Name = player.displayName
                    }}
                };
            }
            public void AddLoot(BasePlayer looter, double time, string date)
            {
                if (Looters.ContainsKey(looter.userID))
                {
                    Looters[looter.userID].LastInit = time;
                    Looters[looter.userID].LastLoot = date;
                }
                else
                    Looters.Add(looter.userID, new LootEntry
                    {
                        FirstLoot = date,
                        LastInit = time,
                        LastLoot = date,
                        Name = looter.displayName
                    });
            }
        }
        public class LootEntry
        {
            public string Name;
            public double LastInit = 0;
            public string FirstLoot;
            public string LastLoot;
            public LootEntry()
            {
                Name = string.Empty;
                FirstLoot = string.Empty;
                LastLoot = string.Empty;
            }
            public LootEntry(string name, string firstLoot, double lastInit)
            {
                Name = name;
                FirstLoot = firstLoot;
                LastLoot = FirstLoot;
                LastInit = lastInit;
            }
        }
        void SaveData()
        {
            boxData.boxes = boxCache;
            playerData.players = playerCache;
            bdata.WriteObject(boxData);
            pdata.WriteObject(playerData);
            PrintWarning("Saved Boxlooters data");
        }
        void LoadData()
        {            
            try
            {
                boxData = bdata.ReadObject<BoxDS>();
                boxCache = boxData.boxes;
            }
            catch
            {
                boxData = new BoxDS();
            }
            try
            {
                playerData = pdata.ReadObject<PlayerDS>();
                playerCache = playerData.players;
            }
            catch
            {
                playerData = new PlayerDS();                
            }
        }
        class BoxDS
        {
            public Dictionary<uint, BoxData> boxes = new Dictionary<uint, BoxData>();
        }
        class PlayerDS
        {
            public Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
        }       
        #endregion

        #region Localization
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"BoxData", "List of looters for this Box[<color=#F5D400>{0}</color>]:"},
            {"PlayerData", "List of looters for this Player [<color=#F5D400>{0}</color>]:"},            
            {"DetectedLooter", "<color=#F5D400>[{0}]</color><color=#4F9BFF>{1}</color>({2}) F:<color=#F80>{3}</color> L:<color=#F80>{4}</color>"},
            {"DetectDestr", "Destoyed by: <color=#4F9BFF>{0}</color> ID:{1}"},
            {"NoLooters", "<color=#4F9BFF>The box [{0}] is clear!</color>"},
            {"NoLootersPlayer", "<color=#4F9BFF>The player [{0}] is clear!</color>"},
            {"Nothing", "<color=#4F9BFF>Unable to find a valid entity</color>"},
            {"NoID", "<color=#4F9BFF>You must enter a valid entity ID</color>"},
            {"NoPlayer",  "No players with that name/ID found!"},
            {"SynError", "<color=#F5D400>Syntax Error: Type '/box' to view available options</color>" },
            {"SavedData", "You have successfully saved loot data" },
            {"ClearData", "You have successfully cleared all loot data" }
        };
        #endregion
    }
}
