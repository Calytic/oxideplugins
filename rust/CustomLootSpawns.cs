using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;



namespace Oxide.Plugins
{
    [Info("CustomLootSpawns", "k1lly0u", "0.1.7", ResourceId = 1655)]
    class CustomLootSpawns : RustPlugin
    {

        bool changed;

        CustomLootData lootData;
        private DynamicConfigFile LootSpawnData;

        private FieldInfo serverinput;

        private List<BaseEntity> currentBoxes = new List<BaseEntity>();
        private Dictionary<int, BoxTypes> boxTypes = new Dictionary<int, BoxTypes>();

        private Dictionary<ulong, OpenBox> openBoxes = new Dictionary<ulong, OpenBox>();
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("customlootspawns.admin", this);
            lang.RegisterMessages(messages, this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            LootSpawnData = Interface.Oxide.DataFileSystem.GetFile("LootSpawn_Data");
            LootSpawnData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };
                
            LoadData();
            LoadVariables();
        }
        void OnServerInitialized()
        {            
            FindBoxTypes();
            refreshBoxes();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            SaveData();
            ClearData(false);
        }
        void ClearData(bool refresh)
        {
            foreach (var box in currentBoxes)
            {
                KillBox(box);
            }
            currentBoxes.Clear();
            boxTypes.Clear();
            openBoxes.Clear();
            if (refresh)
            {
                FindBoxTypes();
            }
        }
        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            BasePlayer player = inventory.GetComponent<BasePlayer>();
            if (openBoxes.ContainsKey(player.userID))
            {
                StoreBoxData(player);
                openBoxes.Remove(player.userID);
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (currentBoxes.Contains(entity))
            {
                currentBoxes.Remove(entity);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // LootSpawn methods /////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private void CreateBox(BasePlayer player, string name, int type, int skin = 0)
        {
            var boxtype = boxTypes[type];

            if (!lootData.customLootBoxes.ContainsKey(name))
                lootData.customLootBoxes.Add(name, new BoxTypeData() { boxtype = boxtype.Type, skin = boxtype.skin, name = name });

            var pos = player.transform.position;
           
            BaseEntity box = GameManager.server.CreateEntity(boxtype.Type, new Vector3(pos.x, pos.y - 1, pos.z));
            if (boxtype.skin != 0)
                box.skinID = boxtype.skin;
            box.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
            box.Spawn(true);
            StorageContainer loot = box.GetComponent<StorageContainer>();
            loot.inventory.itemList.Clear();
            loot.PlayerOpenLoot(player);
            openBoxes[player.userID] = new OpenBox() { box = box, name = name };
        }
        private void StoreBoxData(BasePlayer player)
        {
            ulong ID = player.userID;
            if (openBoxes.ContainsKey(ID))
            {
                string name = openBoxes[ID].name;
                BaseEntity box = openBoxes[ID].box as BaseEntity;
                StorageContainer loot = box.GetComponent<StorageContainer>();
                List<ItemStorage> items = new List<ItemStorage>();

                foreach (Item item in loot.inventory.itemList)
                {
                    ItemStorage bItem = new ItemStorage();
                    bItem.amount = item.amount;
                    bItem.itemname = item.info.displayName.english;
                    bItem.BP = item.IsBlueprint();
                    bItem.itemid = item.info.itemid;
                    bItem.skinid = item.skin;
                    bItem.slot = item.position;
                    items.Add(bItem);
                }
                if (items.Count == 0)
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noItems", this, player.UserIDString));
                    loot.inventory.itemList.Clear();
                    box.Kill();
                    openBoxes.Remove(ID);
                    lootData.customLootBoxes.Remove(name);
                    return;
                }
                lootData.customLootBoxes[name].loot = items;
                boxTypes.Add(boxTypes.Count + 1, new BoxTypes { Type = name, skin = box.skinID });                
                SaveData();
                SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("boxCreated", this, player.UserIDString), boxTypes.Count, name));
                loot.inventory.itemList.Clear();
                box.Kill();
                openBoxes.Remove(ID);
            }
        }
        
        private void AddSpawn(BasePlayer player, int type)
        {
            string boxname = boxTypes[type].Type;
            string boxent = boxname;
            if (lootData.customLootBoxes.ContainsKey(boxname))
            {
                boxent = lootData.customLootBoxes[boxname].boxtype;
            }
            var pos = GetSpawnPos(player);
            BaseEntity entity = CreateLoot(boxent, pos, boxTypes[type].skin);
            StorageContainer loot = entity.GetComponent<StorageContainer>();
            if (lootData.customLootBoxes.ContainsKey(boxname) && lootData.customLootBoxes[boxname].loot != null)
            {
                loot.inventory.itemList.Clear();
                foreach (var boxItem in lootData.customLootBoxes[boxname].loot)
                {
                    Item item = ItemManager.CreateByItemID(boxItem.itemid, boxItem.amount, boxItem.BP, boxItem.skinid);
                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.primaryMagazine.capacity;
                    }
                    item.MoveToContainer(loot.inventory, boxItem.slot);
                }
            }
            int num = GenerateID();
            lootData.Boxes.Add(num, new CLootBox() { ID = entity.net.ID, skin = boxTypes[type].skin, Position = entity.transform.position, Type = boxent, Name = boxname });
            currentBoxes.Add(entity);
            SaveData();
        }
        private int GenerateID()
        {
            int num = GetRandomNumber();
            if (lootData.Boxes.ContainsKey(num))
            {
                num = GetRandomNumber();                
            }
            return num;
        }
        private int GetRandomNumber()
        {
            int num = UnityEngine.Random.Range(1, 9999999);
            return num;
        }
        private BaseEntity CreateLoot(string type, Vector3 pos, int skin = 0)
        {
            BaseEntity entity = GameManager.server.CreateEntity(type, pos, new Quaternion(), true);
            entity.skinID = skin;
            entity.Spawn(true);
            return entity;
        }
        private bool KillBox(BaseEntity entity)
        {
            if (currentBoxes.Contains(entity))
            {
                entity.Kill();
                return true;
            }
            return false;                
        }
        private bool RemoveBoxData(BaseEntity entity)
        {
            foreach (var box in lootData.Boxes)
            {
                if (box.Value.ID == entity.net.ID)
                {
                    lootData.Boxes.Remove(box.Key);
                    entity.Kill();
                    SaveData();
                    return true;
                }
            }
            return false;
        }
        private void refreshBoxes()
        {
            foreach (BaseEntity box in currentBoxes)
            {
                box.Kill();                
            }
            currentBoxes.Clear();
            
            foreach (var newBox in lootData.Boxes)
            {             
                var entity = CreateLoot(newBox.Value.Type, newBox.Value.Position, newBox.Value.skin);
                currentBoxes.Add(entity);
                StorageContainer loot = entity.GetComponent<StorageContainer>();
                newBox.Value.ID = entity.net.ID;
                var boxname = newBox.Value.Name;
                if (lootData.customLootBoxes.ContainsKey(boxname) && lootData.customLootBoxes[boxname].loot != null)
                {
                    loot.inventory.itemList.Clear();
                    foreach (var boxItem in lootData.customLootBoxes[boxname].loot)
                    {
                        Item item = ItemManager.CreateByItemID(boxItem.itemid, boxItem.amount, boxItem.BP, boxItem.skinid);
                        var weapon = item.GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.primaryMagazine.capacity;
                        }
                        item.MoveToContainer(loot.inventory, boxItem.slot);
                    }
                }
            }
            timer.Once(respawnTimer * 60, () => refreshBoxes());
        }
        private object rayBox(Vector3 Pos, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(Pos, Aim);
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
        private StorageContainer findBox(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            Vector3 eyesAdjust = new Vector3(0f, 1.5f, 0f);

            var rayResult = rayBox(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is StorageContainer)
            {
                var box = rayResult as StorageContainer;                
                return box;
            }
            return null;
        }        
        private void ShowBoxList(BasePlayer player)
        {
            foreach (var entry in boxTypes)
            {
                SendEchoConsole(player.net.connection, string.Format("{0} - {1} {2}", entry.Key, entry.Value.Type, entry.Value.skinname));                   
            }
        }
        private void ShowCurrentBoxes(BasePlayer player)
        {
            foreach (var box in lootData.Boxes)
            {
                SendEchoConsole(player.net.connection, string.Format("{0} - {1} - {2}", box.Key, box.Value.Position, box.Value.Type));
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
        } // credit Build
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
                                boxTypes.Add(i, new BoxTypes { Type = str, skin = 0 });
                                i++;
                            }
                        }
                    }
                }
            }
            boxTypes.Add(i, new BoxTypes { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", skin = 0 });
            i++;
            boxTypes.Add(i, new BoxTypes { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", skin = 10124, skinname = "Ammo" });
            i++;
            boxTypes.Add(i, new BoxTypes { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", skin = 10123, skinname = "FirstAid" });
            i++;
            boxTypes.Add(i, new BoxTypes { Type = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", skin = 10141, skinname = "Guns" });
            i++;
            foreach (var box in lootData.customLootBoxes)
            {
                boxTypes.Add(i, new BoxTypes { Type = box.Key, skin = box.Value.skin });
                i++;
            }
        } // credit Build
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
        private List<BaseEntity> FindInRadius(Vector3 pos, float rad)
        {
            var foundBoxes = new List<BaseEntity>();
            foreach (var item in currentBoxes)
            {                
                var itemPos = item.transform.position;
                if (GetDistance(pos, itemPos.x, itemPos.y, itemPos.z) < rad)
                {
                    foundBoxes.Add(item);                    
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

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("cls")]
        private void chatLootspawn(BasePlayer player, string command, string[] args)
        {
            if (!canSpawnLoot(player)) return;
            if (args.Length == 0)
            {
                SendReply(player, lang.GetMessage("synAdd", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synRem", this, player.UserIDString));
                SendReply(player, lang.GetMessage("createSyn", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synList", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synBoxes", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synWipe", this, player.UserIDString));
                return;
            }
            if (args.Length >= 1)
            {
                if (args[0].ToLower() == "add")
                {
                    int type;
                    if (!int.TryParse(args[1], out type))
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notNum", this, player.UserIDString));
                        return;
                    }
                    if (boxTypes.ContainsKey(type))
                    {
                        AddSpawn(player, type);
                        return;
                    }
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notType", this, player.UserIDString));
                    return;
                }
                if (args[0].ToLower() == "create")
                {
                    if (!(args.Length == 3))
                    {
                        SendReply(player, lang.GetMessage("createSyn", this, player.UserIDString));
                        return;
                    }
                    if (!(args[1] == "") || (args[1] == null))
                    {
                        if (lootData.customLootBoxes.ContainsKey(args[1]))
                        {
                            SendReply(player, lang.GetMessage("nameExists", this, player.UserIDString));
                            return;
                        }
                        int type;
                        if (!int.TryParse(args[2], out type))
                        {
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notNum", this, player.UserIDString));
                            return;
                        }
                        if (boxTypes.ContainsKey(type))
                        {                            
                            CreateBox(player, args[1], type, boxTypes[type].skin);
                            return;
                        }
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notType", this, player.UserIDString));
                        return;
                    }
                    SendReply(player, lang.GetMessage("createSyn", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "remove")
                {
                    var box = findBox(player);
                    if (box != null)
                    {
                        if (RemoveBoxData(box))
                        {
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("removedBox", this, player.UserIDString));
                            return;
                        }
                        else
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notReg", this, player.UserIDString));
                        return;
                    }
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notBox", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "list")
                {
                    ShowCurrentBoxes(player);
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "boxes")
                {
                    ShowBoxList(player);
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "near")
                {
                    float rad = 3f;
                    if (args.Length == 2) float.TryParse(args[1], out rad);

                    var boxes = FindInRadius(player.transform.position, rad);
                    if (boxes != null)
                    {
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("foundBoxes", this, player.UserIDString), boxes.Count.ToString()));
                        foreach (var box in boxes)
                        {
                            player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, box.transform.position, 1f);
                        }
                    }
                    else
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noFind", this, player.UserIDString), rad.ToString()));
                }
                else if (args[0].ToLower() == "wipe")
                {
                    int count = lootData.Boxes.Count;
                    
                    ClearData(true);
                    lootData.Boxes.Clear();
                    SaveData();
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("wipedAll", this, player.UserIDString), count.ToString()));
                    return;
                }
                else if (args[0].ToLower() == "wipeall")
                {
                    
                    int customcount = lootData.customLootBoxes.Count;
                    lootData.customLootBoxes.Clear();
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("wipedData", this, player.UserIDString), customcount.ToString()));

                    int count = lootData.Boxes.Count;
                    ClearData(true);
                    lootData.Boxes.Clear();
                    SaveData();
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("wipedAll", this, player.UserIDString), count.ToString()));
                    return;
                }
            }        
        }
        bool canSpawnLoot(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "customlootspawns.admin")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
            {
                if (player.net.connection.authLevel < authLevel)
                {                    
                    return false;
                }
            }
            return true;
        }

        #region DataManagement
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////        
        void SaveData()
        {
            LootSpawnData.WriteObject(lootData);
        }
        void LoadData()
        {
            try
            {
                lootData = LootSpawnData.ReadObject<CustomLootData>();
            }
            catch
            {
                Puts("Couldn't load LootSpawn data, creating new datafile");
                lootData = new CustomLootData();
            }
            
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Custom Loot data classes //////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        class CustomLootData
        {
            public Dictionary<int, CLootBox> Boxes = new Dictionary<int, CLootBox>();
            public Dictionary<string, BoxTypeData> customLootBoxes = new Dictionary<string, BoxTypeData>();
        }
        class CLootBox
        {
            public uint ID;
            public string Type;
            public int skin = 0;
            public string Name;
            public Vector3 Position;
        }  
        class BoxTypes
        {
            public string skinname = "";
            public int skin;
            public string Type;
        }
        class BoxTypeData
        {            
            public string name;
            public int skin = 0;
            public string boxtype;
            public List<ItemStorage> loot = new List<ItemStorage>();
        }  
        class OpenBox
        {
            public string name;            
            public BaseEntity box;
        }        
        class ItemStorage
        {
            public bool BP;
            public int slot;
            public string itemname;
            public int itemid;
            public int skinid;
            public int amount;            
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

        #region Config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        int respawnTimer = 15;
        int authLevel = 1;
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Options - Box refresh timer (mins)", ref respawnTimer);
            CheckCfg("Options - Authlevel required", ref authLevel);
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


        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#31698a>LootSpawns</color> : "},
            {"checkConsole", "Check your console for a list of boxes" },
            {"noPerms", "You do not have permission to use this command" },
            {"notType", "The number you have entered is not on the list" },
            {"notNum", "You must enter a box number" },
            {"notBox", "You are not looking at a box" },
            {"notReg", "This is not a custom placed box" },
            {"removedBox", "Box deleted" },
            {"synAdd", "/cls add id - Adds a new box" },
            {"createSyn", "/cls create yourboxname ## - Builds a custom loot box with boxID: ## and Name: yourboxname" },
            {"nameExists", "You already have a box with that name" },
            {"synRem", "/cls remove - Remove the box you are looking at" },
            {"synBoxes", "/cls boxes - List available box types and their ID" },
            {"synWipe", "/cls wipe - Wipes all custom placed boxes" },
            {"synList", "/cls list - Puts all custom box details to console" },
            {"synNear", "/cls near XX - Shows custom loot boxes in radius XX" },
            {"wipedAll", "Wiped {0} custom box spawns" },
            {"wipedData", "Wiped {0} loot kits" },
            {"foundBoxes", "Found {0} loot spawns near you"},
            {"noFind", "Couldn't find any boxes in radius: {0}M" },
            {"noItems", "You didnt place any items in the box" },
            {"boxCreated", "You have created a new loot box. ID: {0}, Name: {1}" }
        };       
    }
}
