using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{
    [Info("CustomLootSpawns", "k1lly0u", "0.2.2", ResourceId = 1655)]
    class CustomLootSpawns : RustPlugin
    {
        #region Fields
        CLSData clsData;
        private DynamicConfigFile clsdata;

        private FieldInfo serverinput;

        private Dictionary<BaseEntity, int> boxCache = new Dictionary<BaseEntity, int>();
        private Dictionary<int, CustomBoxData> boxTypes = new Dictionary<int, CustomBoxData>();
        private List<Timer> refreshTimers = new List<Timer>();
        private List<BaseEntity> wipeList = new List<BaseEntity>();

        private Dictionary<ulong, BoxCreator> boxCreators = new Dictionary<ulong, BoxCreator>();

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("customlootspawns.admin", this);
            lang.RegisterMessages(messages, this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            clsdata = Interface.Oxide.DataFileSystem.GetFile("CustomSpawns/cls_data");
            clsdata.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            FindBoxTypes();
            InitializeBoxSpawns();
        }
        private void OnEntityKill(BaseNetworkable entity)
        {
            var baseEnt = entity as BaseEntity;
            if (baseEnt == null) return;
            if (wipeList.Contains(baseEnt)) return;
            if (entity.GetComponent<LootContainer>())
            {
                if (boxCache.ContainsKey(baseEnt))
                {
                    InitiateRefresh(baseEnt, boxCache[baseEnt]);
                }
            }
            else if (entity.GetComponent<StorageContainer>())
            {
                if (boxCache.ContainsKey(baseEnt))
                {
                    InitiateRefresh(baseEnt, boxCache[baseEnt]);
                }
            }
        }
        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            BasePlayer player = inventory.GetComponent<BasePlayer>();
            if (boxCreators.ContainsKey(player.userID))
            {
                StoreBoxData(player);
                boxCreators.Remove(player.userID);
            }
            if (inventory.entitySource != null)
            {
                var box = inventory.entitySource;
                if (boxCache.ContainsKey(box))
                {
                    if (box is LootContainer) return;
                    if (box is StorageContainer)
                    {
                        if ((box as StorageContainer).inventory.itemList.Count == 0)
                            box.KillMessage();
                    }
                }
            }
        }
        void Unload()
        {
            foreach (var time in refreshTimers)
                time.Destroy();

            foreach(var box in boxCache)
            {
                if (box.Key == null) continue;

                ClearContainer(box.Key);
                box.Key.KillMessage();
            }
            boxCache.Clear();
        }
        #endregion

        #region Box Control
        private void InitializeBoxSpawns()
        {
            foreach (var box in clsData.lootBoxes)
            {
                InitializeNewBox(box.Key);
            }
        }
        private void InitiateRefresh(BaseEntity box, int ID)
        {            
            refreshTimers.Add(timer.Once(configData.RespawnTimer * 60, () =>
            {
                InitializeNewBox(ID);                
            }));
            boxCache.Remove(box);
        }
        private void InitializeNewBox(int ID)
        {
            if (!clsData.lootBoxes.ContainsKey(ID)) return;
            var boxData = clsData.lootBoxes[ID];
            var newBox = SpawnBoxEntity(boxData.boxType.Type, boxData.Position, boxData.yRotation, boxData.boxType.SkinID);
            if (!string.IsNullOrEmpty(boxData.customLoot) && clsData.customBoxes.ContainsKey(boxData.customLoot))
            {
                var customLoot = clsData.customBoxes[boxData.customLoot];
                if (customLoot.itemList.Count > 0)
                {
                    ClearContainer(newBox);
                    for (int i = 0; i < customLoot.itemList.Count; i++)
                    {
                        var itemInfo = customLoot.itemList[i];
                        var item = CreateItem(itemInfo.ID, itemInfo.Amount, itemInfo.SkinID);
                        if (newBox is LootContainer)
                            item.MoveToContainer((newBox as LootContainer).inventory);
                        else item.MoveToContainer((newBox as StorageContainer).inventory);
                    }
                }
            }
            boxCache.Add(newBox, ID);           
        }
        private BaseEntity SpawnBoxEntity(string type, Vector3 pos, float rot, ulong skin = 0)
        {
            BaseEntity entity = GameManager.server.CreateEntity(type, pos, Quaternion.Euler(0, rot, 0), true);
            entity.skinID = skin;
            entity.Spawn();
            return entity;
        }
        
        private void ClearContainer(BaseEntity container)
        {
            if (container is LootContainer)
            {
                while ((container as LootContainer).inventory.itemList.Count > 0)
                {
                    var item = (container as LootContainer).inventory.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
            }
            else
            {
                while ((container as StorageContainer).inventory.itemList.Count > 0)
                {
                    var item = (container as StorageContainer).inventory.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
            }
        }

        #endregion

        #region Custom Loot Creation
        private void AddSpawn(BasePlayer player, int type)
        {
            var boxData = boxTypes[type];            
            var pos = GetSpawnPos(player);
            var ID = GenerateRandomID();  
            clsData.lootBoxes.Add(ID, new CLBox { Position = pos, yRotation = player.GetNetworkRotation().y, boxType = boxData.boxType, customLoot = boxData.Name });            
            SaveData();
            InitializeNewBox(ID);
        }
        private void CreateNewCLB(BasePlayer player, string name, int type, ulong skin = 0)
        {
            if (boxCreators.ContainsKey(player.userID))
            {
                if (boxCreators[player.userID].entity != null)
                {
                    ClearContainer(boxCreators[player.userID].entity);
                    boxCreators[player.userID].entity.KillMessage();
                }
                boxCreators.Remove(player.userID);
            }
            var boxData = boxTypes[type];
            var pos = GetGroundPosition(player.transform.position + (player.eyes.BodyForward() * 2));

            BaseEntity box = GameManager.server.CreateEntity(boxData.boxType.Type, pos);
            if (boxData.boxType.SkinID != 0)
                box.skinID = boxData.boxType.SkinID;

            box.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
            box.Spawn();

            ClearContainer(box);            

            boxCreators.Add(player.userID, new BoxCreator { entity = box, boxData = new CustomBoxData { Name = name, boxType = boxData.boxType } });
        }
        private void StoreBoxData(BasePlayer player)
        {
            ulong ID = player.userID;
            var boxData = boxCreators[ID];

            var itemList = new List<Item>();
            if (boxData.entity is LootContainer) itemList = (boxData.entity as LootContainer).inventory.itemList;
            else itemList = (boxData.entity as StorageContainer).inventory.itemList;

            var storedList = new List<ItemStorage>();
            for (int i = 0; i < itemList.Count; i++)
            {
                storedList.Add(new ItemStorage { ID = itemList[i].info.itemid, Amount = itemList[i].amount, Shortname = itemList[i].info.shortname, SkinID = itemList[i].skin });
            }
            
            if (storedList.Count == 0)
            {
                SendMSG(player, MSG("noItems", player.UserIDString));
                boxData.entity.KillMessage();
                boxCreators.Remove(player.userID);
                return;
            }
            var data = new CustomBoxData { boxType = boxData.boxData.boxType, Name = boxData.boxData.Name, itemList = storedList };
            clsData.customBoxes.Add(boxData.boxData.Name, data);
            boxTypes.Add(boxTypes.Count + 1, data);               
            SaveData();
            SendMSG(player, string.Format(MSG("boxCreated", player.UserIDString), boxTypes.Count, boxData.boxData.Name));
            ClearContainer(boxData.entity);
            boxData.entity.KillMessage();
            boxCreators.Remove(player.userID);
        }
        #endregion

        #region Helper Methods
        private Item CreateItem(int itemID, int itemAmount, ulong itemSkin) => ItemManager.CreateByItemID(itemID, itemAmount, itemSkin);
        private int GenerateRandomID() => UnityEngine.Random.Range(0, 999999999);
        static Vector3 GetGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        private void FindBoxTypes()
        {
            var filesField = typeof(FileSystem_AssetBundles).GetField("files", BindingFlags.Instance | BindingFlags.NonPublic);
            var files = (Dictionary<string, AssetBundle>)filesField.GetValue(FileSystem.iface);
            int i = 1;
            foreach (var str in files.Keys)
            {
                if ((str.StartsWith("assets/content/") || str.StartsWith("assets/bundled/") || str.StartsWith("assets/prefabs/")) && str.EndsWith(".prefab"))
                {
                    if (str.Contains("resource/loot") || str.Contains("radtown/crate") || str.Contains("radtown/loot") || str.Contains("loot") || str.Contains("radtown/oil"))
                    {
                        if (!str.Contains("ot/dm tier1 lootb"))
                        {
                            var gmobj = GameManager.server.FindPrefab(str);

                            if (gmobj?.GetComponent<BaseEntity>() != null)
                            {
                                boxTypes.Add(i, new CustomBoxData { boxType = new BoxType { Type = str, SkinID = 0 } });
                                i++;
                            }
                        }
                    }
                }
            }
            boxTypes.Add(i, new CustomBoxData { boxType = new BoxType { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", SkinID = 0 } });
            i++;
            boxTypes.Add(i, new CustomBoxData { boxType = new BoxType { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", SkinID = 10124, SkinName = "Ammo" } });
            i++;
            boxTypes.Add(i, new CustomBoxData { boxType = new BoxType { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", SkinID = 10123, SkinName = "FirstAid" } });
            i++;
            boxTypes.Add(i, new CustomBoxData { boxType = new BoxType { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", SkinID = 10141, SkinName = "Guns" } });
            i++;
            foreach (var box in clsData.customBoxes)
            {                
                boxTypes.Add(i, box.Value);
                i++;
            }
        }
        private Vector3 GetSpawnPos(BasePlayer player)
        {
            Vector3 closestHitpoint;
            Vector3 sourceEye = player.transform.position + new Vector3(0f, 1.5f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            Quaternion currentRot = Quaternion.Euler(input.current.aimAngles);
            Ray ray = new Ray(sourceEye, currentRot * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = player.transform.position;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<TriggerBase>() == null)
                {
                    if (hit.distance < closestdist)
                    {
                        closestdist = hit.distance;
                        closestHitpoint = hit.point;
                    }
                }
            }
            return closestHitpoint;
        }
        private BaseEntity FindContainer(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            Vector3 eyesAdjust = new Vector3(0f, 1.5f, 0f);

            var rayResult = CastRay(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is BaseEntity)
            {
                var box = rayResult as BaseEntity;
                return box;
            }
            return null;
        }
        private object CastRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(Pos, Aim);
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.distance < 100)
                {
                    if (hit.collider.GetComponentInParent<StorageContainer>() != null)
                        target = hit.collider.GetComponentInParent<StorageContainer>();

                    else if (hit.collider.GetComponentInParent<LootContainer>() != null)
                        target = hit.collider.GetComponentInParent<LootContainer>();
                }                
            }
            return target;
        }
        private List<BaseEntity> FindInRadius(Vector3 pos, float rad)
        {
            var foundBoxes = new List<BaseEntity>();
            foreach (var item in boxCache)
            {
                var itemPos = item.Key.transform.position;
                if (GetDistance(pos, itemPos.x, itemPos.y, itemPos.z) < rad)
                {
                    foundBoxes.Add(item.Key);
                }
            }
            return foundBoxes;
        }
        private float GetDistance(Vector3 v3, float x, float y, float z)
        {
            float distance = 1000f;

            distance = (float)Math.Pow(Math.Pow(v3.x - x, 2) + Math.Pow(v3.y - y, 2), 0.5);
            distance = (float)Math.Pow(Math.Pow(distance, 2) + Math.Pow(v3.z - z, 2), 0.5);

            return distance;
        }
        private bool IsUncreateable(string name)
        {
            foreach(var entry in unCreateable)
            {
                if (name.Contains(entry))
                    return true;
            }
            return false;
        }
        private void ShowBoxList(BasePlayer player)
        {
            foreach (var entry in boxTypes)
            {
                SendEchoConsole(player.net.connection, string.Format("{0} - {1} {2}", entry.Key, entry.Value.boxType.Type, entry.Value.boxType.SkinName));
            }
        }
        private void ShowCurrentBoxes(BasePlayer player)
        {
            foreach (var box in clsData.lootBoxes)
            {
                SendEchoConsole(player.net.connection, string.Format("{0} - {1}", box.Value.Position, box.Value.boxType.Type));
            }
        }
        void SendEchoConsole(Network.Connection cn, string msg)
        {
            if (Network.Net.sv.IsConnected())
            {
                Network.Net.sv.write.Start();
                Network.Net.sv.write.PacketID(Network.Message.Type.ConsoleMessage);
                Network.Net.sv.write.String(msg);
                Network.Net.sv.write.Send(new Network.SendInfo(cn));
            }
        }
        #endregion

        #region Chat Commands
        [ChatCommand("cls")]
        private void chatLootspawn(BasePlayer player, string command, string[] args)
        {
            if (!canSpawnLoot(player)) return;
            if (args.Length == 0)
            {
                SendReply(player, MSG("synAdd", player.UserIDString));
                SendReply(player, MSG("synRem", player.UserIDString));
                SendReply(player, MSG("createSyn", player.UserIDString));
                SendReply(player, MSG("synList", player.UserIDString));
                SendReply(player, MSG("synBoxes", player.UserIDString));
                SendReply(player, MSG("synWipe", player.UserIDString));
                return;
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "add":
                        {
                            int type;
                            if (!int.TryParse(args[1], out type))
                            {
                                SendMSG(player, MSG("notNum", player.UserIDString));
                                return;
                            }
                            if (boxTypes.ContainsKey(type))
                            {
                                AddSpawn(player, type);
                                return;
                            }
                            SendMSG(player, MSG("notType", player.UserIDString));
                        }
                        return;
                    case "create":
                        {
                            if (!(args.Length == 3))
                            {
                                SendMSG(player, MSG("createSyn", player.UserIDString));
                                return;
                            }
                            if (!(args[1] == "") || (args[1] == null))
                            {
                                if (clsData.customBoxes.ContainsKey(args[1]))
                                {
                                    SendMSG(player, MSG("nameExists", player.UserIDString));
                                    return;
                                }
                                int type;
                                if (!int.TryParse(args[2], out type))
                                {
                                    SendMSG(player, MSG("notNum", player.UserIDString));
                                    return;
                                }
                                if (boxTypes.ContainsKey(type))
                                {
                                    if (IsUncreateable(boxTypes[type].boxType.Type))
                                    {
                                        SendMSG(player, MSG("unCreateable", player.UserIDString));
                                        return;
                                    }
                                    CreateNewCLB(player, args[1], type, boxTypes[type].boxType.SkinID);
                                    return;
                                }
                                SendMSG(player, MSG("notType", player.UserIDString));
                                return;
                            }
                            SendReply(player, MSG("createSyn", player.UserIDString));
                        }
                        return;
                    case "remove":
                        {
                            var box = FindContainer(player);
                            if (box != null)
                            {
                                if (boxCache.ContainsKey(box))
                                {
                                    if (clsData.lootBoxes.ContainsKey(boxCache[box]))
                                    {
                                        clsData.lootBoxes.Remove(boxCache[box]);
                                        SaveData();
                                    }
                                    ClearContainer(box);
                                    box.KillMessage();
                                    SendMSG(player, MSG("removedBox", player.UserIDString));
                                    return;
                                }
                                else
                                    SendMSG(player, MSG("notReg", player.UserIDString));
                                return;
                            }
                            SendMSG(player, MSG("notBox", player.UserIDString));
                        }
                        return;
                    case "list":
                        ShowCurrentBoxes(player);
                        SendMSG(player, MSG("checkConsole", player.UserIDString));
                        return;
                    case "boxes":
                        ShowBoxList(player);
                        SendMSG(player, MSG("checkConsole", player.UserIDString));
                        return;
                    case "near":
                        {
                            float rad = 3f;
                            if (args.Length == 2) float.TryParse(args[1], out rad);

                            var boxes = FindInRadius(player.transform.position, rad);
                            if (boxes != null)
                            {
                                SendMSG(player, string.Format(MSG("foundBoxes", player.UserIDString), boxes.Count));
                                foreach (var box in boxes)
                                {
                                    player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, box.transform.position, 1f);
                                }
                            }
                            else
                                SendMSG(player, string.Format(MSG("noFind", player.UserIDString), rad));
                        }
                        return;
                    case "wipe":
                        {
                            var count = clsData.lootBoxes.Count;
                            
                            foreach(var box in boxCache)
                            {
                                wipeList.Add(box.Key);
                                ClearContainer(box.Key);
                                box.Key.KillMessage();
                            }
                            clsData.lootBoxes.Clear();
                            wipeList.Clear();
                            SaveData();
                            SendMSG(player, string.Format(MSG("wipedAll1", player.UserIDString), count));
                        }
                        return;
                    case "wipeall":
                        {
                            var count = clsData.lootBoxes.Count;
                            var count2 = clsData.customBoxes.Count;
                            foreach (var box in boxCache)
                            {
                                wipeList.Add(box.Key);
                                ClearContainer(box.Key);
                                box.Key.KillMessage();
                            }
                            clsData.lootBoxes.Clear();
                            clsData.customBoxes.Clear();
                            wipeList.Clear();
                            SaveData();
                            SendMSG(player, string.Format(MSG("wipedData1", player.UserIDString), count, count2));
                        }
                        return;
                    default:
                        break;
                }               
            }
        }
        bool canSpawnLoot(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "customlootspawns.admin")) return true;
            SendMSG(player, MSG("noPerms", player.UserIDString));
            return false;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int RespawnTimer { get; set; }
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
                RespawnTimer = 20
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => clsdata.WriteObject(clsData);
        void LoadData()
        {
            try
            {
                clsData = clsdata.ReadObject<CLSData>();
            }
            catch
            {
                clsData = new CLSData();
            }
        }
        class CLSData
        {
            public Dictionary<int, CLBox> lootBoxes = new Dictionary<int, CLBox>();
            public Dictionary<string, CustomBoxData> customBoxes = new Dictionary<string, CustomBoxData>();
        }
        #endregion

        #region Classes
        class CLBox
        {
            public float yRotation;
            public Vector3 Position;
            public BoxType boxType;
            public string customLoot;            
        }
        class BoxCreator
        {
            public BaseEntity entity;
            public CustomBoxData boxData;
        }
        class CustomBoxData
        {
            public string Name = null;
            public BoxType boxType = new BoxType();
            public List<ItemStorage> itemList = new List<ItemStorage>();
        }
        class BoxType
        {
            public string SkinName = null;
            public ulong SkinID;
            public string Type;
        }
        class ItemStorage
        {
            public string Shortname;
            public int ID;
            public ulong SkinID;
            public int Amount;
        }
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        } // borrowed from ZoneManager
        #endregion

        #region Messaging
        private void SendMSG(BasePlayer player, string message) => SendReply(player, $"<color=orange>{Title}:</color> <color=#939393>{message}</color>");
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"checkConsole", "Check your console for a list of boxes" },
            {"noPerms", "You do not have permission to use this command" },
            {"notType", "The number you have entered is not on the list" },
            {"notNum", "You must enter a box number" },
            {"notBox", "You are not looking at a box" },
            {"notReg", "This is not a custom placed box" },
            {"removedBox", "Box deleted" },
            {"synAdd", "<color=orange>/cls add id </color><color=#939393>- Adds a new box</color>" },
            {"createSyn", "<color=orange>/cls create yourboxname ## </color><color=#939393>- Builds a custom loot box with boxID: ## and Name: yourboxname</color>" },
            {"nameExists", "You already have a box with that name" },
            {"synRem", "<color=orange>/cls remove </color><color=#939393>- Remove the box you are looking at</color>" },
            {"synBoxes", "<color=orange>/cls boxes </color><color=#939393>- List available box types and their ID</color>" },
            {"synWipe", "<color=orange>/cls wipe </color><color=#939393>- Wipes all custom placed boxes</color>" },
            {"synList", "<color=orange>/cls list </color><color=#939393>- Puts all custom box details to console</color>" },
            {"synNear", "<color=orange>/cls near XX </color><color=#939393>- Shows custom loot boxes in radius XX</color>" },
            {"wipedAll1", "Wiped {0} custom loot spawns" },
            {"wipedData1", "Wiped {0} custom loot spawns and {1} custom loot kits" },
            {"foundBoxes", "Found {0} loot spawns near you"},
            {"noFind", "Couldn't find any boxes in radius: {0}M" },
            {"noItems", "You didnt place any items in the box" },
            {"boxCreated", "You have created a new loot box. ID: {0}, Name: {1}" },
            {"unCreateable", "You can not create custom loot for this type of box" }
        };

        #endregion

        List<string> unCreateable = new List<string> { "barrel", "trash", "giftbox" };
    }
}
