using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("RadPockets", "k1lly0u", "0.1.4", ResourceId = 1492)]
    class RadPockets : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        private bool Changed;
        private bool RadsOn;

        private DynamicConfigFile RadPocketData;
        StoredData storedData;
        private Dictionary<string, ZoneInfo> RadPZones = new Dictionary<string, ZoneInfo>();
        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            RadPocketData = Interface.Oxide.DataFileSystem.GetFile("RadPockets");
            RadPocketData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };
            lang.RegisterMessages(messages, this);
            LoadData();
            LoadVariables();
            if (ConVar.Server.radiation == false)
            {
                RadsOn = false;
                ConVar.Server.radiation = true;
            }
        }
        void OnServerInitialized()
        {
            if (ZoneManager == null)
            {
                Puts("ZoneManager is not installed");
                return;
            }

            if (storedData.RadPZones.Count == 0)
            {
                int amountRZones = UnityEngine.Random.Range(minZones, maxZones);
                timer.Repeat(0.2f, amountRZones, () => getRadZoneInfo());
                Puts("No zones found, creating new zones");
                timer.Once((amountRZones * 0.2f) + 2, () =>
                {
                    Puts("Created " + RadPZones.Count + " new zones");
                });
            }
            else
            {
                foreach (var zone in storedData.RadPZones)
                    createZone(zone.ID, zone.Position);
            }
        }       
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (!RadsOn) ConVar.Server.radiation = false;
 
            foreach (var zone in storedData.RadPZones)
                eraseZone(zone.ID);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static int minRadius = 15;
        static int maxRadius = 60;
        static int minZones = 15;
        static int maxZones = 30;
        static int minRads = 100;
        static int maxRads = 1000;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {            
            CheckCfg("Options - Minimum zone radius", ref minRadius);
            CheckCfg("Options - Maximum zone radius", ref maxRadius);
            CheckCfg("Options - Minimum zones to create", ref minZones);
            CheckCfg("Options - Maximum zones to create", ref maxZones);
            CheckCfg("Options - Minimum radiation level", ref minRads);
            CheckCfg("Options - Maximum radiation level", ref maxRads);

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
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"loadedPockets", "Loaded {0} Radiation Pockets" },
            {"loadFail", "Couldn't load RadPockets data, creating new datafile" },
            {"title", "<color=orange>RadPockets</color> : "},
			{"noClosest", "Unable to find the closest zone"},
            {"cantFindZone", "Unable to find the zone"},
            {"sucessTP", "You have teleported to {0}"},
            {"zoneRemoved", "You have removed zone {0} at co-ordinates {1}"},
            {"zoneCreated", "Created zone {0} at co-ordinates {1}"},
            {"withRadius", ", with a radius of {2}"},
            {"withRadiation", ", with a radiation value of {2}"},
            {"incorrectFormat", "Incorrect format"},
            {"chatRadi", "arguments are radius XX and radiation XX"},
            {"chatRemoveSyn", "/radpockets_remove RadP_XX"},
            {"invalidID", "Invalid zone ID"},
            {"adminRemoved", "Zone {0} removed"},
			{"clearAll", "All radiation pockets removed!"},
			{"checkConsole", "Check your console for the list"},
			{"pocketList", "------ RadPocket List ------"},
			{"noPerms", "You dont have permission to use this command"},
            {"valueNum", "The value needs to be a number" }
        };

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        class StoredData
        {
            public readonly HashSet<ZoneInfo> RadPZones = new HashSet<ZoneInfo>();
        }
        void SaveData()
        {
            RadPocketData.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                RadPocketData.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = RadPocketData.ReadObject<StoredData>();
                Puts(lang.GetMessage("loadedPockets", this), storedData.RadPZones.Count);
            }
            catch
            {
                Puts(lang.GetMessage("loadFail", this));
                storedData = new StoredData();
            }
            RadPocketData.Settings.NullValueHandling = NullValueHandling.Include;

            foreach (var radzone in storedData.RadPZones)
                RadPZones[radzone.ID] = radzone;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Classes ///////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        class ZoneInfo
        {
            public string ID;
            public Vector3 Position;

            public ZoneInfo()
            {

            }
            public ZoneInfo(string zoneID, Vector3 zoneLoc)
            {
                ID = zoneID;
                Position = zoneLoc;
            }
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

        //////////////////////////////////////////////////////////////////////////////////////
        // Random Generating Functions ///////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
                
        private void getRadZoneInfo()
        {
            string zoneID = checkID();
            var pos = checkPos();
            if (zoneID != null && !(pos is string))
                createZone(zoneID, (Vector3)pos);
        }
        private object checkPos()
        {
            Vector3 position = getRandomPos();
            if (position.y <= 0)
                return position.ToString();
            return position;
        }
        private string checkID()
        {
            string zoneID = getRandomID();
            if (RadPZones.ContainsKey(zoneID))
                return null;
            return zoneID;
        }
        private string getRandomID()
        {
            int randomNum = UnityEngine.Random.Range(1, 1000);
            string randID = ("rad_" + randomNum);            
            return randID;
        }
        private string getRandomRads()
        {
            int randomNum = UnityEngine.Random.Range(minRads, maxRads);            
            return randomNum.ToString();
        }
        private float getRandomRadius()
        {
            float randomSize = UnityEngine.Random.Range(minRadius, maxRadius);          
            return randomSize;
        }
        private Vector3 getRandomPos()
        {
            float mapSize = (TerrainMeta.Size.x / 2) - 600f;

            float randomX = UnityEngine.Random.Range(-mapSize, mapSize);
            float randomY = UnityEngine.Random.Range(-mapSize, mapSize);

            Vector3 pos = new Vector3(randomX, 0, randomY);
            Vector3 targetPos = getGroundPosition(pos);
            return targetPos;
        }      
        static Vector3 getGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }         

        //////////////////////////////////////////////////////////////////////////////////////
        // Main Functions ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void createZone (string zoneID, Vector3 pos)
        {                  
            string[] zoneArgs = sendArgs();
            ZoneInfo zoneinfo;

            if (!RadPZones.TryGetValue(zoneID, out zoneinfo))
                zoneinfo = new ZoneInfo { ID = zoneID };

            if (pos != null)
            zoneinfo.Position = pos;

            ZoneManager?.Call("CreateOrUpdateZone", zoneID, zoneArgs, pos);

            RadPZones[zoneID] = zoneinfo;
            storedData.RadPZones.Add(zoneinfo);
            SaveData();
        }
        void createCustomZone(string zoneID, string[] info, Vector3 pos)
        {            
            ZoneInfo zoneinfo;

            if (!RadPZones.TryGetValue(zoneID, out zoneinfo))
                zoneinfo = new ZoneInfo { ID = zoneID };

            if (pos != null)
                zoneinfo.Position = pos;

            if (info != null)
            {
                var newArgs = new List<string>();
                foreach (var arg in info)
                {
                    newArgs.Add(arg);
                }                
                if (!info.Contains("radius"))
                {
                    newArgs.Add("radius");
                    newArgs.Add("10");
                }
                if (!info.Contains("radiation"))
                {
                    newArgs.Add("radiation");
                    newArgs.Add("1");
                }
                info = newArgs.ToArray();
            }
            else info = sendArgs();

            ZoneManager?.Call("CreateOrUpdateZone", zoneID, info, pos);

            RadPZones[zoneID] = zoneinfo;
            storedData.RadPZones.Add(zoneinfo);
            SaveData();
        }
        private void eraseZone(string zoneID)
        {
            ZoneManager.Call("EraseZone", zoneID);          
            Puts("Zone " + zoneID + " removed.");
        }
        string[] sendArgs()
        {
            float radius = getRandomRadius();
            var zoneRadius = Convert.ToString(radius);

            string[] zoneArgs = new string[4];
            zoneArgs[0] = "radius";
            zoneArgs[1] = zoneRadius;
            zoneArgs[2] = "radiation";
            zoneArgs[3] = getRandomRads();
            return zoneArgs;
        }
        private object getClosestZonePos(BasePlayer player)
        {
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPos = player.transform.position;

            List<Vector3> zonePosi = new List<Vector3>();
            foreach (var zone in storedData.RadPZones)
            {
                var pos = zone.Position;
                zonePosi.Add(pos);
            }

            foreach (Vector3 zonePos in zonePosi)
            {
                Vector3 directionToTarget = zonePos - currentPos;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    var closestPos = zonePos;
                    if (closestPos != null)
                        return closestPos;
                }
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noClosest", this, player.UserIDString));
            return null;
        }

        private ZoneInfo getClosestZone(Vector3 position)
        {
            foreach (var zone in storedData.RadPZones)
            {
                if (zone.Position == position)
                {                    
                    return zone;
                }
            }
            return null;            
        }
        private void tpPlayer(BasePlayer player, Vector3 pos)
        {
            player.MovePosition(pos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            player.metabolism.Reset();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("rad_tpnear")]
        private void chatRadTP(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            var closestZone = getClosestZonePos(player);
            if (closestZone == null)
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("cantFindZone", this, player.UserIDString));

            tpPlayer(player, (Vector3)closestZone);
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("sucessTP", this, player.UserIDString), closestZone));
        }

        [ChatCommand("rad_removenear")]
        private void chatRemoveNear(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;

            var closestZonePos = getClosestZonePos(player);
            ZoneInfo closestZone = getClosestZone((Vector3)closestZonePos);
           
            if (!storedData.RadPZones.Contains(closestZone))
            {
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("cantFindZone", this, player.UserIDString));
                return;
            }

            eraseZone(closestZone.ID);
            storedData.RadPZones.Remove(closestZone);
            SaveData();
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("zoneRemoved", this, player.UserIDString), closestZone.ID, closestZone.Position));
        }

        [ChatCommand("rad_create")]
        private void chatRadCreate(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            var ID = getRandomID();
            Vector3 Pos = player.transform.position;

            if (args.Length == 0 || args == null)
            {
                createZone(ID, Pos);
                SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("zoneCreated", this, player.UserIDString), ID, Pos));
                return;
            }
            else if (args.Length == 2 || args.Length == 4)
            {
                for (var i = 0; i < args.Length; i = i + 2)
                {
                    switch (args[i].ToLower())
                    {
                        case "radiation":
                            float rads;
                            if (!float.TryParse(args[i + 1], out rads))
                            {
                                SendReply(player, lang.GetMessage("valueNum", this, player.UserIDString));
                                return;
                            }
                            break;
                        case "radius":
                            float radius;
                            if (!float.TryParse(args[i + 1], out radius))
                            {
                                SendReply(player, lang.GetMessage("valueNum", this, player.UserIDString));
                                return;
                            }
                            break;
                        default:                            
                            break;
                    }
                    if (args.Length >= 2)
                    {
                        createCustomZone(ID, args, Pos);
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("zoneCreated", this, player.UserIDString), ID, Pos));
                        return;
                    }
                }                
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("incorrectFormat", this, player.UserIDString));
            SendReply(player, lang.GetMessage("chatRadi", this, player.UserIDString));
            return;
        }

        [ChatCommand("rad_remove")]
        void chatRadZoneRemove(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;

            if (args.Length == 0)
            {
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("chatRemoveSyn", this, player.UserIDString));
                return;
            }

            ZoneInfo zoneinfo;
            if (!RadPZones.TryGetValue(args[0].ToLower(), out zoneinfo))
            {
                SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("invalidID", this, player.UserIDString)));
                return;
            }

            eraseZone(zoneinfo.ID);
            storedData.RadPZones.Remove(zoneinfo);
            SaveData();
            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("adminRemoved", this, player.UserIDString), args[0]));            
        }

        [ChatCommand("rad_clearall")]
        void chatRadZoneClear(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            
            foreach (var zone in storedData.RadPZones)
            {
                eraseZone(zone.ID);                
            }
            storedData.RadPZones.Clear();
            SaveData();
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("clearAll", this, player.UserIDString));
        }

        [ChatCommand("rad_list")]
        void chatRadZoneList(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            Puts(lang.GetMessage("title", this) + lang.GetMessage("pocketList", this));
            if (RadPZones.Count == 0) Puts("none");
            foreach (var zone in storedData.RadPZones)
                Puts(zone.ID + "====" + zone.Position);
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Auth/Permissions //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 1) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }       
    }
}
