using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("MonumentRadiation", "k1lly0u", "0.1.8", ResourceId = 1562)]
    class MonumentRadiation : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        private bool Changed;
        private bool RadsOn;
        int OffTimer;
        int OnTimer;

        MonumentZones zoneData;
        private DynamicConfigFile ZoneData;

        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");
           
        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            lang.RegisterMessages(messages, this);

            ZoneData = Interface.Oxide.DataFileSystem.GetFile("MR_data");
            ZoneData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };

            LoadData();
            LoadVariables();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void OnServerInitialized()
        {
            if (plugins.Exists("ZoneManager")) CheckForExisting();
            else Puts(lang.GetMessage("noZoneM", this));
        }

        void Unload()
        {
            if (!RadsOn) ConVar.Server.radiation = false;
            EraseAll();
        }
        #endregion
      
        #region functions
        //////////////////////////////////////////////////////////////////////////////////////
        // MonumentRadiation ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void CheckForExisting()
        {
            if (zoneData.MRZones != null && zoneData.MRZones.Count > 0) EraseAll();
            findAllMonuments();
        }
        private void EraseAll()
        {
            foreach (var zone in zoneData.MRZones)
                eraseZone(zone.Value);
            zoneData.MRZones.Clear();
            SaveData();
        }
        private int getRandomNum()
        {
            int randomNum = UnityEngine.Random.Range(1, 1000);
            return randomNum;
        }
        private void findAllMonuments()
        {
            if (useHapis) { CreateHapis(); return; }

            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                var pos = gobject.transform.position;

                if (radsLighthouse)                
                    if (gobject.name.ToLower().Contains("lighthouse/lighthouse"))                    
                        createZone("lighthouse_" + getRandomNum(), pos, sizeLighthouse, radsAmountLighthouse);                   
                
                if (radsPowerplant)                
                    if (gobject.name.ToLower().Contains("powerplant_1"))                    
                        createZone("powerplant_" + getRandomNum(), pos, sizePowerplant, radsAmountPowerplant);                    
                
                if (radsTunnels)                
                    if (gobject.name.ToLower().Contains("military_tunnel_1"))                    
                        createZone("tunnels_" + getRandomNum(), pos, sizeTunnels, radsAmountTunnels);                    
                
                if (radsAirfield)                
                    if (gobject.name.ToLower().Contains("airfield_1"))                    
                        createZone("airfield_" + getRandomNum(), pos, sizeAirfield, radsAmountAirfield);                    
                
                if (radsTrainyard)                
                    if (gobject.name.ToLower().Contains("large/trainyard_1"))                    
                        createZone("trainyard_" + getRandomNum(), pos, sizeTrainyard, radsAmountTrainyard);                    
                
                if (radsWaterplant)                
                    if (gobject.name.ToLower().Contains("large/water_treatment_plant_1"))                    
                        createZone("waterplant_" + getRandomNum(), pos, sizeWaterplant, radsAmountWaterplant);                    
                
                if (radsWarehouse)                
                    if (gobject.name.ToLower().Contains("mining/warehouse"))                    
                        createZone("warehouse_" + getRandomNum(), pos, sizeWarehouse, radsAmountWarehouse);                    
                
                if (radsSatellite)                
                    if (gobject.name.ToLower().Contains("production/satellite_dish"))                    
                        createZone("satellite_" + getRandomNum(), pos, sizeSatellite, radsAmountSatellite);                    
                
                if (radsDome)                
                    if (gobject.name.ToLower().Contains("production/sphere_tank"))                    
                        createZone("spheretank_" + getRandomNum(), pos, sizeDome, radsAmountDome);                    
                
                if (radsRadtown)                
                    if (gobject.name.ToLower().Contains("small/radtown_small_3"))                    
                        createZone("radtown_" + getRandomNum(), pos, sizeRadtown, radsAmountRadtown);
            }
            ConfirmCreation();
        }
        private void CreateHapis()
        {
            if (radsLighthouse)
            {
                createZone("lighthouse_1", HIMon["lighthouse_1"].Position, HIMon["lighthouse_1"].Radius.ToString(), radsAmountLighthouse);
                createZone("lighthouse_2", HIMon["lighthouse_2"].Position, HIMon["lighthouse_2"].Radius.ToString(), radsAmountLighthouse);
            }
            if (radsWaterplant) createZone("water_1", HIMon["water"].Position, HIMon["water"].Radius.ToString(), radsAmountWaterplant);
            if (radsTunnels) createZone("tunnels_1", HIMon["tunnels"].Position, HIMon["tunnels"].Radius.ToString(), radsAmountTunnels);
            if (radsSatellite) createZone("satellite_1", HIMon["satellite"].Position, HIMon["satellite"].Radius.ToString(), radsAmountSatellite);
            ConfirmCreation();
        }
        private void ConfirmCreation()
        {
            if (zoneData.MRZones.Count > 0)
            {
                if (useRadTimer) startRadTimers();
                Puts("Created " + zoneData.MRZones.Count + " monument radiation zones");
                if (!ConVar.Server.radiation)
                {
                    RadsOn = false;
                    ConVar.Server.radiation = true;
                }
                SaveData();
            }
        }
        private void createZone(string zoneID, Vector3 pos, string radius, float rads)
        {
            List<string> build = new List<string>();
            build.Add("radius");
            build.Add(radius);
            build.Add("radiation");
            build.Add(rads.ToString());

            string[] zoneArgs = build.ToArray();

            if (pos == null) return;
            if (zoneData.MRZones.ContainsKey(pos)) return;           

            ZoneManager?.Call("CreateOrUpdateZone", zoneID, zoneArgs, pos);
            zoneData.MRZones.Add(pos, zoneID);
        }               
        private void eraseZone(string zoneID)
        {
            ZoneManager.Call("EraseZone", zoneID);
            Puts("Zone " + zoneID + " removed.");
        }
        private void startRadTimers()
        {
            int ontime = timerOn;
            int offtime = timerOff;
            if (randomTimer)
            {
                ontime = GetRandom(randOnMin, randOnMax);
                offtime = GetRandom(randOffMin, randOffMax);
            }
            OnTimer = ontime * 60;
            timer.Repeat(1, OnTimer, () =>
            {
                OnTimer--;
                if (OnTimer == 0)
                {
                    ConVar.Server.radiation = false;
                    if (broadcastTimer)                    
                        MessageAllPlayers(lang.GetMessage("RadsOffMsg", this), offtime);
                    
                    OffTimer = offtime * 60;
                    timer.Repeat(1, OffTimer, () =>
                    {
                        OffTimer--;
                        if (OffTimer == 0)
                        {
                            ConVar.Server.radiation = true;
                            if (broadcastTimer)                            
                                MessageAllPlayers(lang.GetMessage("RadsOnMsg", this), ontime);
                            
                            startRadTimers();
                        }
                    });
                }
            });
        }
        private int GetRandom(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }
        private void MessageAllPlayers(string msg, int time) => ConsoleSystem.Broadcast("chat.add", new object[] { 0, string.Format(msg, time)});           
        
        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            if (useEnterMessage)
            {
                if (ConVar.Server.radiation == false) return;
                if (zoneData.MRZones.ContainsValue(ZoneID))                
                    SendReply(player, lang.GetMessage("enterMessage", this, player.UserIDString));
            }
        }
        void OnExitZone(string ZoneID, BasePlayer player)
        {
            if (useLeaveMessage)
            {
                if (ConVar.Server.radiation == false) return;
                if (zoneData.MRZones.ContainsValue(ZoneID))                
                    SendReply(player, lang.GetMessage("leaveMessage", this, player.UserIDString));
            }
        }
        #endregion

        #region perms/commands
        //////////////////////////////////////////////////////////////////////////////////////
        // Permission/Auth Check /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool isAdmin(BasePlayer player)
        {
            if (player.net.connection != null)            
                if (player.net.connection.authLevel <= 1)
                {
                    SendReply(player, lang.GetMessage("title", this) + lang.GetMessage("noPerms", this, player.UserIDString));
                    return false;
                }
            return true;
        }
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)            
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, lang.GetMessage("noPerms", this));
                    return false;
                }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ConsoleCommand("mr_clearall")]
        private void ccmdClearAll(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            EraseAll();
            SendReply(arg, lang.GetMessage("clearAll", this));
        }

        [ChatCommand("mr_clearall")]
        void chatClearAll(BasePlayer player, string command, string[] args)
        {
            if (!isAdmin(player)) return;
            EraseAll();
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("clearAll", this, player.UserIDString));
        }

        [ConsoleCommand("mr_list")]
        void ccmdRadZoneList(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            Puts(lang.GetMessage("monList", this));
            if (zoneData.MRZones.Count == 0) Puts("none");
            foreach (var zone in zoneData.MRZones)
                Puts(zone.Key + " ------ " + zone.Value.ToString());
        }

        [ChatCommand("mr_list")]
        void chatRadZoneList(BasePlayer player, string command, string[] args)
        {
            if (!isAdmin(player)) return;
            Puts(lang.GetMessage("title", this) + lang.GetMessage("monList", this));
            if (zoneData.MRZones.Count == 0) Puts("none");
            foreach (var zone in zoneData.MRZones)
                Puts(zone.Key + "====" + zone.Value.ToString());
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
        }

        [ChatCommand("mr")]
        void chatCheckTimers(BasePlayer player, string command, string[] args)
        {
            if (OnTimer != 0)
            {
                float timeOn = OnTimer / 60;
                string min = "minutes";
                if (timeOn < 1) { timeOn = OnTimer; min = "seconds"; }
                SendReply(player, string.Format(lang.GetMessage("RadsDownMsg", this), timeOn.ToString(), min));
            }
            else if (OffTimer != 0)
            {
                int timeOff = OffTimer / 60;
                string min = "minutes";
                if (timeOff < 1) { timeOff = OffTimer; min = "seconds"; }
                SendReply(player, string.Format(lang.GetMessage("RadsUpMsg", this), timeOff.ToString(), min));
            }
        }
        #endregion

        #region data management and classes
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management and Classes ///////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////        
        void SaveData() => ZoneData.WriteObject(zoneData);
        
        void LoadData()
        {
            try
            {
                zoneData = ZoneData.ReadObject<MonumentZones>();
            }
            catch
            {
                Puts("Couldn't load Monument Radiation data, creating new datafile");
                zoneData = new MonumentZones();
            }
        }
        class MonumentZones
        {
            public Dictionary<Vector3, string> MRZones = new Dictionary<Vector3, string>();
            public MonumentZones() { }
        }
        class HapisIslandMonuments
        {
            public Vector3 Position;
            public float Radius;
        }
        Dictionary<string, HapisIslandMonuments> HIMon = new Dictionary<string, HapisIslandMonuments>
        {
            {"lighthouse_1", new HapisIslandMonuments {Position = new Vector3(1562.30981f, 45.05141f, 1140.29382f), Radius = 15 } },
            {"lighthouse_2", new HapisIslandMonuments {Position = new Vector3(-1526.65112f, 45.3333473f, -280.0514f), Radius = 15 } },
            {"water", new HapisIslandMonuments {Position = new Vector3(-1065.191f, 125.3655f, 439.2279f), Radius = 100 } },
            {"tunnels", new HapisIslandMonuments {Position = new Vector3(-854.7694f, 72.34925f, -241.692f), Radius = 100 } },
            {"satellite", new HapisIslandMonuments {Position = new Vector3(205.2501f, 247.8247f, 252.5204f), Radius = 80 } }
        };
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
        }

        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static bool radsLighthouse = false;
        static bool radsAirfield = false;
        static bool radsPowerplant = false;
        static bool radsTrainyard = false;
        static bool radsWaterplant = false;
        static bool radsWarehouse = false;
        static bool radsSatellite = false;
        static bool radsDome = false;
        static bool radsRadtown = true;
        static bool radsTunnels = false;
        static bool useEnterMessage = true;
        static bool useLeaveMessage = false;
        static bool useRadTimer = true;
        static bool broadcastTimer = true;
        static bool randomTimer = false;
        static bool useHapis = false;

        static int randOnMin = 5;
        static int randOnMax = 30;
        static int randOffMin = 25;
        static int randOffMax = 60;
        static int timerOff = 15;
        static int timerOn = 45;

        static float radsAmountLighthouse = 100;
        static float radsAmountAirfield = 100;
        static float radsAmountPowerplant = 100;
        static float radsAmountTrainyard = 100;
        static float radsAmountWaterplant = 100;
        static float radsAmountWarehouse = 100;
        static float radsAmountSatellite = 100;
        static float radsAmountDome = 100;
        static float radsAmountRadtown = 100;
        static float radsAmountTunnels = 100;

        static string sizeLighthouse = "15";
        static string sizeAirfield = "85";
        static string sizePowerplant = "120";
        static string sizeTrainyard = "100";
        static string sizeWaterplant = "120";
        static string sizeWarehouse = "15";
        static string sizeSatellite = "60";
        static string sizeDome = "50";
        static string sizeRadtown = "60";
        static string sizeTunnels = "90";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Options - Using Hapis Island", ref useHapis);
            CheckCfg("Options - Radiation Zones - Lighthouses", ref radsLighthouse);
            CheckCfg("Options - Radiation Zones - Airfield", ref radsAirfield);
            CheckCfg("Options - Radiation Zones - Powerplant", ref radsPowerplant);
            CheckCfg("Options - Radiation Zones - Trainyard", ref radsTrainyard);
            CheckCfg("Options - Radiation Zones - Water Treatment Plant", ref radsWaterplant);
            CheckCfg("Options - Radiation Zones - Warehouses", ref radsWarehouse);
            CheckCfg("Options - Radiation Zones - Satellite", ref radsSatellite);
            CheckCfg("Options - Radiation Zones - Sphere Tank", ref radsDome);
            CheckCfg("Options - Radiation Zones - Rad-towns", ref radsRadtown);
            CheckCfg("Options - Radiation Zones - Military Tunnels", ref radsTunnels);
            CheckCfg("Options - Message - Use enter message", ref useEnterMessage);
            CheckCfg("Options - Message - Use leave message", ref useLeaveMessage);
            CheckCfg("Options - Timers - Use radiation activation/deactivation timers", ref useRadTimer);
            CheckCfg("Options - Timers - Broadcast radiation status", ref broadcastTimer);
            CheckCfg("Options - Timers - Amount of time deactivated (mins)", ref timerOff);
            CheckCfg("Options - Timers - Amount of time activated (mins)", ref timerOn);
            CheckCfg("Options - Timers - Use random timers", ref randomTimer);
            CheckCfg("Options - Timers - Random - Off minimum (mins)", ref randOffMin);
            CheckCfg("Options - Timers - Random - Off maximum (mins)", ref randOffMax);
            CheckCfg("Options - Timers - Random - On minimum (mins)", ref randOnMin);
            CheckCfg("Options - Timers - Random - On maximum (mins)", ref randOnMax);

            CheckCfg("Options - Zone Size - Rad-towns", ref sizeRadtown);
            CheckCfg("Options - Zone Size - Lighthouses", ref sizeLighthouse);
            CheckCfg("Options - Zone Size - Airfield", ref sizeAirfield);
            CheckCfg("Options - Zone Size - Powerplant", ref sizePowerplant);
            CheckCfg("Options - Zone Size - Trainyard", ref sizeTrainyard);
            CheckCfg("Options - Zone Size - Water Treatment Plant", ref sizeWaterplant);
            CheckCfg("Options - Zone Size - Warehouses", ref sizeWarehouse);
            CheckCfg("Options - Zone Size - Satellite", ref sizeSatellite);
            CheckCfg("Options - Zone Size - Sphere Tank", ref sizeDome);
            CheckCfg("Options - Zone Size - Military Tunnels", ref sizeTunnels);
            CheckCfgFloat("Options - Radiation amount - Lighthouses", ref radsAmountLighthouse);
            CheckCfgFloat("Options - Radiation amount - Airfield", ref radsAmountAirfield);
            CheckCfgFloat("Options - Radiation amount - Powerplant", ref radsAmountPowerplant);
            CheckCfgFloat("Options - Radiation amount - Trainyard", ref radsAmountTrainyard);
            CheckCfgFloat("Options - Radiation amount - Water Treatment Plant", ref radsAmountWaterplant);
            CheckCfgFloat("Options - Radiation amount - Warehouses", ref radsAmountWarehouse);
            CheckCfgFloat("Options - Radiation amount - Satellite", ref radsAmountSatellite);
            CheckCfgFloat("Options - Radiation amount - Sphere Tank", ref radsAmountDome);
            CheckCfgFloat("Options - Radiation amount - Rad-towns", ref radsAmountRadtown);
            CheckCfgFloat("Options - Radiation amount - Military Tunnels", ref radsAmountTunnels);

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
        #endregion

        #region messages
        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"nullList", "Error getting a list of monuments" },
            {"noPerms", "You have insufficient permission" },
            {"noZoneM", "ZoneManager is not installed, can not proceed" },
            {"title", "<color=orange>MonumentRadiation</color> : "},
            {"monList", "------ Monument Radiation List ------"},
            {"clearAll", "All monument radiation removed!"},
            {"enterMessage", "<color=#B30000>WARNING: </color><color=#B6B6B6>You are entering a irradiated area! </color>" },
            {"leaveMessage", "<color=#B30000>CAUTION: </color><color=#B6B6B6>You are leaving a irradiated area! </color>" },
            {"RadsOnMsg", "<color=#B6B6B6>Monument radiation levels are back up for </color><color=#00FF00>{0} minutes</color><color=grey>!</color>" },
            {"RadsOffMsg", "<color=#B6B6B6>Monument radiation levels are down for </color><color=#00FF00>{0} minutes</color><color=grey>!</color>"},
            {"RadsUpMsg", "<color=#B6B6B6>Monument radiation levels will be back up in </color><color=#00FF00>{0} {1}</color><color=#B6B6B6>!</color>"},
            {"RadsDownMsg", "<color=#B6B6B6>Monument radiation levels will be down in </color><color=#00FF00>{0} {1}</color><color=#B6B6B6>!</color>"}

    };
        #endregion


    }
}
