using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BoxLooters", "4seti [Lunatiq] for Rust Planet", "0.2.92", ResourceId = 989)]
    public class BoxLooters : RustPlugin
    {
        Vector3 eyesAdjust;
        private FieldInfo serverinput;

        Core.Libraries.Time time = new Core.Libraries.Time();

        Dictionary<uint, BoxData> boxData;
        Dictionary<ulong, PlayerData> playerData;       
        bool changed;

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("boxlooters.checkbox", this);
            lang.RegisterMessages(messages, this);

            eyesAdjust = new Vector3(0f, 1.5f, 0f);            
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)); 

            timer.Once(SaveTimer, () => SaveDataLoop());
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload() => SaveData();
        void OnLootEntity(BasePlayer looter, BaseEntity entry)
        {
            if (looter == null || entry == null) return;
            var type = entry.GetType();
            if (entry is StorageContainer)
            {
                StorageContainer box = entry as StorageContainer;
                if (!(box.panelName == "largewoodbox" || box.panelName == "smallwoodbox"
                      || box.panelName == "fuelstorage" || box.panelName == "smallstash"
                      || box.panelName == "furnace" || box.panelName == "smallrefinery"
                      || box.panelName == "largefurnace" || box.panelName == "watercatcher"
                      || box.name.Contains("quarry/hopperoutput.prefab")
                      || box.prefabID == 349880778))
                    return;
                if (box == null || looter == null) return;
                uint boxID = box.net.ID;
                uint timeStamp = time.GetUnixTimestamp();
                if (boxData == null) boxData = new Dictionary<uint, BoxData>();
                if (boxData.ContainsKey(boxID))
                {
                    if (boxData[boxID].Looters.ContainsKey(looter.userID))
                    {
                        boxData[boxID].Looters[looter.userID].LastLoot = DateTime.Now.ToString("d/M/yyyy HH:mm:ss");
                        boxData[boxID].Looters[looter.userID].LastInit = timeStamp;
                    }
                    else boxData[boxID].Looters.Add(looter.userID, new LootEntry(looter.displayName, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), timeStamp));

                    boxData[boxID].lastInit = timeStamp;
                }
                else
                {
                    boxData.Add(boxID, new BoxData(timeStamp, box.transform.position));
                    boxData[boxID].Looters.Add(looter.userID, new LootEntry(looter.displayName, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), timeStamp));
                }
            }
            if (entry is BasePlayer)
            {
                BasePlayer player = entry.GetComponent<BasePlayer>();
                ulong playerID = player.userID;
                uint timeStamp = time.GetUnixTimestamp();
                if (playerData == null) playerData = new Dictionary<ulong, PlayerData>();
                if (playerData.ContainsKey(playerID))
                {
                    if (playerData[playerID].Looters.ContainsKey(looter.userID))
                    {
                        playerData[playerID].Looters[looter.userID].LastLoot = DateTime.Now.ToString("d/M/yyyy HH:mm:ss");
                        playerData[playerID].Looters[looter.userID].LastInit = timeStamp;
                    }
                    else
                        playerData[playerID].Looters.Add(looter.userID, new LootEntry(looter.displayName, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), timeStamp));

                    playerData[playerID].lastInit = timeStamp;
                }
                else
                {
                    playerData.Add(playerID, new PlayerData(timeStamp));
                    playerData[playerID].PlayerName = player.displayName;
                    playerData[playerID].Looters.Add(looter.userID, new LootEntry(looter.displayName, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), timeStamp));
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null) return;
            if (entity is StorageContainer)
            {
                var box = entity as StorageContainer;
                if (!(box.panelName == "largewoodbox" || box.panelName == "smallwoodbox"
                      || box.panelName == "fuelstorage" || box.panelName == "smallstash"
                      || box.panelName == "furnace" || box.panelName == "smallrefinery"
                      || box.panelName == "largefurnace" || box.panelName == "watercatcher"
                      || box.name.Contains("quarry/hopperoutput.prefab")
                      || box.prefabID == 349880778))
                    return;
                if (hitInfo.Initiator is BasePlayer)
                {
                    var player = hitInfo.Initiator as BasePlayer;
                    if (boxData.ContainsKey(box.net.ID))
                    {
                        boxData[box.net.ID].destrID = player.userID;
                        boxData[box.net.ID].destrName = player.displayName;
                    }
                    else
                    {
                        boxData.Add(box.net.ID, new BoxData(time.GetUnixTimestamp(), box.transform.position));
                        boxData[box.net.ID].destrID = player.userID;
                        boxData[box.net.ID].destrName = player.displayName;
                    }
                }
                AddToLootLog(box);
            }
        }
        #endregion

        #region Methods
        private void AddToLootLog(StorageContainer box)
        {
            if (boxData.ContainsKey(box.net.ID))
            {
                boxData[box.net.ID].lastInit = time.GetUnixTimestamp();
                var lootData = getLooters(box.transform.position);
                foreach (var looter in lootData)
                {
					if (!boxData[box.net.ID].Looters.ContainsKey(looter.userID))
						boxData[box.net.ID].Looters.Add(looter.userID, new LootEntry(looter.Name, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), time.GetUnixTimestamp()));
                    else
                        boxData[box.net.ID].Looters[looter.userID].LastLoot = DateTime.Now.ToString("d/M/yyyy HH:mm:ss");
                }
            }
            else
            {
                boxData.Add(box.net.ID, new BoxData(time.GetUnixTimestamp(), box.transform.position));
                var lootData = getLooters(box.transform.position);
                foreach (var looter in lootData)
                {
                    if (!boxData[box.net.ID].Looters.ContainsKey(looter.userID))
                        boxData[box.net.ID].Looters.Add(looter.userID, new LootEntry(looter.Name, DateTime.Now.ToString("d/M/yyyy HH:mm:ss"), time.GetUnixTimestamp()));
                    else
                        boxData[box.net.ID].Looters[looter.userID].LastLoot = DateTime.Now.ToString("d/M/yyyy HH:mm:ss");
                }
            }
        }
        private List<Looter> getLooters(Vector3 v3)
        {
            List<Looter> looters = new List<Looter>();
            int playerMask = UnityEngine.LayerMask.GetMask("Player (Server)");
            var colliders = Physics.OverlapSphere(v3, detectRadius, playerMask);
            foreach (var collider in colliders)
            {
                var player = collider.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    looters.Add(new Looter(player.userID, player.displayName));
                }
            }
            return looters;
        }  
        private List<BasePlayer> FindPlayerByName(string playerName = "")
        {            
            if (playerName == "") return null;
            playerName = playerName.ToLower();
            List<BasePlayer> matches = new List<BasePlayer>();
            foreach (var player in BasePlayer.activePlayerList)
            {
                string displayName = player.displayName.ToLower();
                if (displayName.Contains(playerName))
                {
                    matches.Add(player);
                }
            }
            return matches;
        }
        private Tuple<uint, BoxData> FindBoxFromRad(Vector3 pos, float rad)
        {
            Tuple<uint, BoxData> result = null;
            foreach (var item in boxData)
            {
                if (GetDistance(pos, item.Value.x, item.Value.y, item.Value.z) < rad)
                {
                    result = new Tuple<uint, BoxData>(item.Key, item.Value);
                    break;
                }
            }
            return result;
        }
        private float GetDistance(Vector3 v3, float x, float y, float z)
        {
            float distance = 1000f;

            distance = (float)Math.Pow(Math.Pow(v3.x - x, 2) + Math.Pow(v3.y - y, 2), 0.5);
            distance = (float)Math.Pow(Math.Pow(distance, 2) + Math.Pow(v3.z - z, 2), 0.5);

            return distance;
        }
        void ReplyChat(BasePlayer player, string msg)
        {
            player.ChatMessage(string.Format("<color=#81D600>{0}</color>: {1}", Title, msg));
        }
        object FindBoxFromRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = UnityEngine.Physics.RaycastAll(Pos, Aim);
            float distance = 1000f;
            object target = null;
            
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<StorageContainer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<StorageContainer>();
                    }
                }
                else if (hit.collider.GetComponentInParent<BasePlayer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BasePlayer>();
                    }
                }
            }
            return target;
        }
        #endregion

        #region ChatCommands
        [ChatCommand("boxsave")]
        void cmdSave(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            SaveData();
            ReplyChat(player, "Data Saved!");
        }
        [ChatCommand("boxclear")]
        void cmdClear(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            int oldDataCount = boxData.Count + playerData.Count;
            int oldEntriesCount = boxData.Sum(v => v.Value.Looters.Count) + playerData.Sum(v => v.Value.Looters.Count);
            float remH = -1;
            if (args.Length > 0) float.TryParse(args[0], out remH);
            TryClear(remH);
            int newEntriesCount = boxData.Sum(v => v.Value.Looters.Count) + playerData.Sum(v => v.Value.Looters.Count);
            ReplyChat(player, string.Format(lang.GetMessage("RemovedRows", this, player.UserIDString), (oldDataCount - playerData.Count - boxData.Count), (remH >= 0 ? remH : RemoveHours), (boxData.Count + playerData.Count)));
            ReplyChat(player, string.Format(lang.GetMessage("RemovedEntries", this, player.UserIDString), (oldEntriesCount - newEntriesCount), (remH >= 0 ? remH : RemoveHours), (newEntriesCount)));
        }
        [ChatCommand("box")]
        void cmdBox(BasePlayer player, string cmd, string[] args)
        {
            if (!HavePerm(player)) return;

            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;

            var rayResult = FindBoxFromRay(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is StorageContainer)
            {
                var box = rayResult as StorageContainer;
                if (box != null)
                {
                    if (boxData.ContainsKey(box.net.ID))
                    {
                        ReplyChat(player, string.Format(lang.GetMessage("BoxData", this, player.UserIDString), box.net.ID));
                        int i = 1;
                        foreach (var data in boxData[box.net.ID].Looters)
                        {
                            ReplyChat(player, string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot));
                            i++;
                        }
                    }
                    else
                        ReplyChat(player, string.Format(lang.GetMessage("NoLooters", this, player.UserIDString), box.net.ID));
                }
                else
                    ReplyChat(player, lang.GetMessage("NoBox", this, player.UserIDString));
            }
            else if (rayResult is BasePlayer)
            {
                var target = rayResult as BasePlayer;
                if (target != null)
                {
                    if (playerData.ContainsKey(target.userID))
                    {
                        ReplyChat(player, string.Format(lang.GetMessage("BoxData", this, player.UserIDString), target.displayName));
                        int i = 1;
                        foreach (var data in playerData[target.userID].Looters)
                        {
                            ReplyChat(player, string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot));
                            i++;
                        }
                    }
                    else
                        ReplyChat(player, string.Format(lang.GetMessage("NoLootersPlayer", this, player.UserIDString), target.userID));
                }
                else
                    ReplyChat(player, lang.GetMessage("NoPlayer", this, player.UserIDString));
            }
            else
                ReplyChat(player, lang.GetMessage("Nothing", this, player.UserIDString));
        }
        [ChatCommand("boxrad")]
        void cmdBoxRad(BasePlayer player, string cmd, string[] args)
        {
            if (!HavePerm(player)) return;

            float rad = 3f;
            if (args.Length > 0) float.TryParse(args[0], out rad);

            var boxRad = FindBoxFromRad(player.transform.position, rad);
            if (boxRad != null)
            {
                ReplyChat(player, string.Format(lang.GetMessage("BoxData", this, player.UserIDString), boxRad.Item1));

                player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, boxRad.Item2.BoxV3(), 1f);

                if (boxRad.Item2.destrID != 0)
                    ReplyChat(player, string.Format(lang.GetMessage("DetectDestr", this, player.UserIDString), boxRad.Item2.destrName, boxRad.Item2.destrID));

                int i = 1;
                foreach (var data in boxRad.Item2.Looters)
                {
                    ReplyChat(player, string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot));
                    i++;
                }
            }
            else
                ReplyChat(player, lang.GetMessage("NoBox", this, player.UserIDString));
        }
        [ChatCommand("boxpname")]
        void cmdBoxPlayer(BasePlayer player, string cmd, string[] args)
        {
            if (!HavePerm(player)) return;

            if (args.Length == 0) return;

            var pList = FindPlayerByName(args[0]);
            BasePlayer target;
            if (pList.Count > 1)
            {
                ReplyChat(player, lang.GetMessage("MatchOverflow", this, player.UserIDString));
            }
            else if (pList.Count == 0)
            {
                ReplyChat(player, lang.GetMessage("MatchNoone", this, player.UserIDString));
            }
            else
            {
                target = pList.First();
                if (playerData.ContainsKey(target.userID))
                {
                    ReplyChat(player, string.Format(lang.GetMessage("BoxData", this, player.UserIDString), target.displayName));
                    int i = 1;
                    foreach (var data in playerData[target.userID].Looters)
                    {
                        ReplyChat(player, string.Format(lang.GetMessage("DetectedLooter", this, player.UserIDString), i, data.Value.Name, data.Key, data.Value.FirstLoot, data.Value.LastLoot));
                        i++;
                    }
                }
                else
                    ReplyChat(player, string.Format(lang.GetMessage("NoLootersPlayer", this, player.UserIDString), target.userID));
            }
        }
        private bool HavePerm(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "boxlooters.checkbox") || player.net.connection.authLevel >
                0)
                return true;
            return false;
        }
        #endregion

        #region Config & Variables

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"BoxData", "List of looters for this Box[<color=#F5D400>{0}</color>]:"},
            {"PlayerData", "List of looters for this Player[<color=#F5D400>{0}</color>]:"},
            {"RemovedRows", "Removed {0} rows older than {1} hours, {2} rows total"},
            {"RemovedEntries", "Removed {0} loot entries older than {1} hours, {2} entries total"},
            {"DetectedLooter", "<color=#F5D400>[{0}]</color><color=#4F9BFF>{1}</color>({2}) F:<color=#F80>{3}</color> L:<color=#F80>{4}</color>"},
            {"DetectDestr", "Destoyed by: <color=#4F9BFF>{0}</color> ID:{1}"},
            {"DetectName", "<color=#4F9BFF>{0}</color> ID:{1}"},
            {"NoBox", "<color=#4F9BFF>No Box is found</color>"},
            {"NoLooters", "<color=#4F9BFF>This Box[{0}] is clear!</color>"},
            {"NoLootersPlayer", "<color=#4F9BFF>This Player[{0}] is clear!</color>"},
            {"NoPlayer", "<color=#4F9BFF>No Box is found</color>"},
            {"Nothing", "<color=#4F9BFF>Nothing is found</color>"},
            {"MatchOverflow",  "More than one match!"},
            {"MatchNoone",  "No players with that name found!"}
        };
        
        float detectRadius = 15f;
        int SaveTimer = 600;
        int RemoveHours = 48;        

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfgFloat("Options - /boxrad detect radius", ref detectRadius);            
            CheckCfg("Options - Data save timer (seconds)", ref SaveTimer);
            CheckCfg("Options - Amount of hours before removing an entry", ref RemoveHours);
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                changed = true;
            }
            return value;
        }
        #endregion

        #region Data Management
        private void LoadData()
        {
            try
            {
                boxData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<uint, BoxData>>("box-data");
                Log("Old Box data loaded!");
            }
            catch
            {
                boxData = new Dictionary<uint, BoxData>();
                Warn("New Box Data file initiated!");
                SaveData();
            }
            try
            {
                playerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("box-data-player");
                Log("Old Players data loaded!");
            }
            catch
            {
                playerData = new Dictionary<ulong, PlayerData>();
                Warn("New Player Data file initiated!");
                SaveData();
            }
            TryClear();
        }
        void TryClear(float remH = -1)
        {
            List<uint> removeList = new List<uint>();
            uint timeStamp = time.GetUnixTimestamp();
            List<ulong> entryRemoveList = new List<ulong>();
            long removedEntries = 0;
            foreach (var item in boxData)
            {
                if (((timeStamp - item.Value.lastInit) / 3600) >= (remH >= 0 ? remH : RemoveHours))
                {
                    removeList.Add(item.Key);
                }
                else
                {
                    entryRemoveList = new List<ulong>();
                    foreach (var lootEntry in item.Value.Looters)
                    {
                        if (lootEntry.Value.LastInit > 0)
                            if (((timeStamp - lootEntry.Value.LastInit) / 3600) >= (remH >= 0 ? remH : RemoveHours))
                            {
                                entryRemoveList.Add(lootEntry.Key);
                            }
                    }
                    foreach (var remItem in entryRemoveList)
                    {
                        item.Value.Looters.Remove(remItem);
                        removedEntries++;
                    }
                }
            }
            if (removeList.Count > 0 || removedEntries > 0)
            {
                foreach (var item in removeList)
                {
                    boxData.Remove(item);
                }
                Warn(string.Format("Removed {0} old records from BoxData, {1} old LootEntries removed", removeList.Count, removedEntries));
            }
            List<ulong> pRemoveList = new List<ulong>();
            removedEntries = 0;
            foreach (var item in playerData)
            {
                if (((timeStamp - item.Value.lastInit) / 3600) >= (remH >= 0 ? remH : RemoveHours))
                {
                    pRemoveList.Add(item.Key);
                }
                else
                {
                    entryRemoveList = new List<ulong>();
                    foreach (var lootEntry in item.Value.Looters)
                    {
                        if (lootEntry.Value.LastInit > 0)
                            if (((timeStamp - lootEntry.Value.LastInit) / 3600) >= (remH >= 0 ? remH : RemoveHours))
                            {
                                entryRemoveList.Add(lootEntry.Key);
                            }
                    }
                    foreach (var remItem in entryRemoveList)
                    {
                        item.Value.Looters.Remove(remItem);
                        removedEntries++;
                    }
                }
            }
            if (removeList.Count > 0)
            {
                foreach (var item in pRemoveList)
                {
                    playerData.Remove(item);
                }
                Warn(string.Format("Removed {0} old records from PlayerData, {1} old LootEntries removed", pRemoveList.Count, removedEntries));
            }
        }
        void SaveDataLoop()
        {
            SaveData();
            timer.Once(SaveTimer, () => SaveDataLoop());
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("box-data", boxData);
            Interface.GetMod().DataFileSystem.WriteObject("box-data-player", playerData);
            Log("Data saved!");
        }
        #endregion

        #region Utility Methods

        private void Log(string message) => Puts("{0}: {1}", Title, message);
        private void Warn(string message) => PrintWarning("{0}: {1}", Title, message);
        private void Error(string message) => PrintError("{0}: {1}", Title, message);
        #endregion

        #region Class        
        public class Looter
        {
            public readonly ulong userID;
            public readonly string Name;
            public Looter(ulong userid, string name)
            {
                userID = userid;
                Name = name;
            }
        }       

        public class BoxData
        {
            public uint lastInit;
            public float x = 0, y = 0, z = 0;
            public ulong destrID = 0;
            public string destrName;
            public Dictionary<ulong, LootEntry> Looters;

            public BoxData()
            {
                lastInit = 0;
                destrName = string.Empty;
                Looters = new Dictionary<ulong, LootEntry>();
            }
            public BoxData(uint time, Vector3 pos)
            {
                lastInit = time;
                destrName = string.Empty;
                x = pos.x;
                y = pos.y;
                z = pos.z;
                Looters = new Dictionary<ulong, LootEntry>();
            }
			public Vector3 BoxV3()
			{
				return new Vector3(x, y + 0.5f, z);
			}
        }

        public class PlayerData
        {
            public uint lastInit;
            public string PlayerName;
            public Dictionary<ulong, LootEntry> Looters;

            public PlayerData()
            {
                lastInit = 0;
                Looters = new Dictionary<ulong, LootEntry>();
            }
            public PlayerData(uint time)
            {
                lastInit = time;
                Looters = new Dictionary<ulong, LootEntry>();
            }
        }
        public class LootEntry
        {
            public string Name;
			public uint LastInit = 0;
			public string FirstLoot;
            public string LastLoot;
            public LootEntry()
            {
                Name = string.Empty;
                FirstLoot = string.Empty;
                LastLoot = string.Empty;
            }
            public LootEntry(string name, string firstLoot, uint lastInit)
            {
                Name = name;
                FirstLoot = firstLoot;
                LastLoot = FirstLoot;
				LastInit = lastInit;
            }
        }
        public class Tuple<T, U>
        {
            public T Item1 { get; set; }
            public U Item2 { get; set; }

            public Tuple(T item1, U item2)
            {
                Item1 = item1;
                Item2 = item2;
            }
        }
        #endregion
    }
}
