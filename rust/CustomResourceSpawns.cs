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
    [Info("CustomResourceSpawns", "k1lly0u", "0.1.1", ResourceId = 1783)]
    class CustomResourceSpawns : RustPlugin
    {
        bool changed;

        CRSData resourceData;
        private DynamicConfigFile ResourceData;

        private FieldInfo serverinput;

        private List<BaseEntity> currentResources = new List<BaseEntity>();
        private Dictionary<ulong, int> addingResources = new Dictionary<ulong, int>();
        private Dictionary<int, string> resourceTypes = new Dictionary<int, string>();
        
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("customresourcespawns.admin", this);
            lang.RegisterMessages(messages, this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            ResourceData = Interface.Oxide.DataFileSystem.GetFile("CRS_Data");
            ResourceData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };

            LoadData();
            LoadVariables();
        }
        void OnServerInitialized()
        {
            FindTypes();
            RefreshResources();
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
            DestroyResource();
        }
        void DestroyResource()
        {
            foreach (var entry in currentResources)
                KillResource(entry);
            currentResources.Clear();            
        }  
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (addingResources.ContainsKey(player.userID))
                addingResources.Remove(player.userID);
        }     
        
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (currentResources.Contains(entity))                
                if (dispenser.fractionRemaining <= 0.05)
                    currentResources.Remove(entity);
        }        

        //////////////////////////////////////////////////////////////////////////////////////
        // ResourceSpawn methods /////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        
        private void AddSpawn(BasePlayer player, int type)
        {
            string resource = resourceTypes[type]; 
            var pos = GetSpawnPos(player);
            BaseEntity entity = CreateResource(resource, pos);
            resourceData.Resources.Add(new ResData() { ID = entity.net.ID, Key = type, Position = entity.transform.position, Type = resource });
            currentResources.Add(entity);
            SaveData();
        }
        private BaseEntity CreateResource(string type, Vector3 pos)
        {
            BaseEntity entity = GameManager.server.CreateEntity(type, pos, new Quaternion(), true);
            entity.Spawn(true);
            return entity;
        }
        private bool KillResource(BaseEntity entity)
        {
            if (currentResources.Contains(entity))
            {
                entity.Kill();
                return true;
            }
            return false;
        }
        private bool RemoveResourceData(BaseEntity entity)
        {
            if (currentResources.Contains(entity))
            {                
                foreach (var resource in resourceData.Resources)
                {
                    if (resource.ID == entity.net.ID)
                    {
                        currentResources.Remove(entity);
                        entity.Kill();
                        resourceData.Resources.Remove(resource);
                        SaveData();
                        return true;
                    }
                }                
            }
            return false;
        }
        private void RefreshResources()
        {
            foreach (BaseEntity entry in currentResources)            
                entry.Kill();            
            currentResources.Clear();

            foreach (var newR in resourceData.Resources)
            {
                var entity = CreateResource(newR.Type, newR.Position);
                currentResources.Add(entity);
                newR.ID = entity.net.ID;                
            }
            timer.Once(respawnTimer * 60, () => RefreshResources());
        }        
        private BaseEntity FindResource(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(input.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!UnityEngine.Physics.Raycast(ray, out hit, 10f))
                return null;
            return hit.GetEntity();
        }
        private void ShowResourceList(BasePlayer player)
        {
            foreach (var entry in resourceTypes)
                SendEchoConsole(player.net.connection, string.Format("{0} - {1}", entry.Key, entry.Value));            
        }
        private void ShowCurrentResources(BasePlayer player)
        {
            foreach (var resource in resourceData.Resources)
                SendEchoConsole(player.net.connection, string.Format("{0} - {1} - {2}", resource.Key, resource.Position, resource.Type));            
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
        private void FindTypes()
        {
            var filesField = typeof(FileSystem_AssetBundles).GetField("files", BindingFlags.Instance | BindingFlags.NonPublic);
            var files = (Dictionary<string, AssetBundle>)filesField.GetValue(FileSystem.iface);
            int i = 1;
            foreach (var str in files.Keys)
                if (str.StartsWith("assets/bundled/prefabs/autospawn/resource"))
                    if (!str.Contains("loot"))// || str.StartsWith("assets/bundled/prefabs/autospawn/collectable"))
                    {
                        var gmobj = GameManager.server.FindPrefab(str);
                        if (gmobj?.GetComponent<BaseEntity>() != null)
                        {
                            resourceTypes.Add(i, str);
                            i++;
                        }
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
                if (hit.collider.GetComponentInParent<TriggerBase>() == null)                
                    if (hit.distance < closestdist)
                    {
                        closestdist = hit.distance;
                        closestHitpoint = hit.point;
                    }
            return closestHitpoint;
        }
        private List<Vector3> FindInRadius(Vector3 pos, float rad)
        {
            var foundResources = new List<Vector3>();
            foreach (var item in currentResources)
            {
                var itemPos = item.transform.position;
                if (GetDistance(pos, itemPos.x, itemPos.y, itemPos.z) < rad)                
                    foundResources.Add(item.transform.position);
            }
            return foundResources;
        }
        private float GetDistance(Vector3 v3, float x, float y, float z)
        {
            float distance = 1000f;

            distance = (float)Math.Pow(Math.Pow(v3.x - x, 2) + Math.Pow(v3.y - y, 2), 0.5);
            distance = (float)Math.Pow(Math.Pow(distance, 2) + Math.Pow(v3.z - z, 2), 0.5);

            return distance;
        }
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (addingResources.ContainsKey(player.userID))
                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    int type = addingResources[player.userID];
                    AddSpawn(player, type);
                }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("crs")]
        private void chatLootspawn(BasePlayer player, string command, string[] args)
        {
            if (!canSpawn(player)) return;
            if (addingResources.ContainsKey(player.userID))
            {
                addingResources.Remove(player.userID);
                SendReply(player, lang.GetMessage("endAdd", this, player.UserIDString));
                return;
            }
            if (args.Length == 0)
            {
                SendReply(player, lang.GetMessage("synAdd", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synRem", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synList", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synResource", this, player.UserIDString));
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
                    if (resourceTypes.ContainsKey(type))
                    {
                        addingResources.Add(player.userID, type);
                        SendReply(player, lang.GetMessage("adding", this, player.UserIDString));
                        return;
                    }
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notType", this, player.UserIDString));
                    return;
                }                
                else if (args[0].ToLower() == "remove")
                {
                    var resource = FindResource(player);
                    if (resource != null)
                    {
                        if (RemoveResourceData(resource))
                        {
                            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("removedResource", this, player.UserIDString));
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
                    ShowCurrentResources(player);
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "resources")
                {
                    ShowResourceList(player);
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
                    return;
                }
                else if (args[0].ToLower() == "near")
                {
                    float rad = 10f;
                    if (args.Length == 2) float.TryParse(args[1], out rad);

                    var resources = FindInRadius(player.transform.position, rad);
                    if (resources != null)
                    {
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("foundResources", this, player.UserIDString), resources.Count.ToString()));
                        foreach (var resource in resources)
                            player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, resource, 1f);                        
                    }
                    else
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noFind", this, player.UserIDString), rad.ToString()));
                }
                else if (args[0].ToLower() == "wipe")
                {
                    int count = resourceData.Resources.Count;
                    DestroyResource();
                    resourceData.Resources.Clear();
                    SaveData();
                    SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("wipedAll", this, player.UserIDString), count.ToString()));
                    return;
                }                
            }
        }
        bool canSpawn(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "customresourcespawns.admin")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)            
                if (player.net.connection.authLevel < authLevel)                
                    return false;
            return true;
        }

        #region DataManagement
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////        
        void SaveData() => ResourceData.WriteObject(resourceData);        
        void LoadData()
        {
            try
            {
                resourceData = ResourceData.ReadObject<CRSData>();
            }
            catch
            {
                Puts("Couldn't load ResourceSpawn data, creating new datafile");
                resourceData = new CRSData();
            }

        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Custom Loot data classes //////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        class CRSData
        {
            public List<ResData> Resources = new List<ResData>();
        }
        class ResData
        {
            public uint ID;
            public int Key;
            public string Type;
            public Vector3 Position;
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
            CheckCfg("Options - Resource refresh timer (mins)", ref respawnTimer);
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
            {"title", "<color=orange>ResourceSpawns</color> : "},
            {"checkConsole", "Check your console for a list of resources" },
            {"noPerms", "You do not have permission to use this command" },
            {"notType", "The number you have entered is not on the list" },
            {"notNum", "You must enter a resource number" },
            {"notBox", "You are not looking at a resource" },
            {"notReg", "This is not a custom placed resource" },
            {"removedResource", "Box deleted" },
            {"synAdd", "/crs add id - Adds a new resource" },
            {"nameExists", "You already have a resource with that name" },
            {"synRem", "/crs remove - Remove the resource you are looking at" },
            {"synResource", "/crs resources - List available resource types and their ID" },
            {"synWipe", "/crs wipe - Wipes all custom placed resources" },
            {"synList", "/crs list - Puts all custom resource details to console" },
            {"synNear", "/crs near XX - Shows custom resources in radius XX" },
            {"wipedAll", "Wiped {0} custom resource spawns" },
            {"foundResources", "Found {0} resource spawns near you"},
            {"noFind", "Couldn't find any resources in radius: {0}M" },
            {"adding", "You have activated the resouce tool. Look where you want to place and press shoot. Type /crs to end" },
            {"endAdd", "You have de-activated the resouce tool" }
        };
    }
}
