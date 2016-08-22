using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Rust;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MonumentRadiation", "k1lly0u", "0.2.04", ResourceId = 1562)]
    class MonumentRadiation : RustPlugin
    {
        private bool RadsOn;
        private int OffTimer;
        private int OnTimer;

        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);

        private List<RZ> RadiationZones = new List<RZ>();
        private ConfigData configData;

        #region Oxide Hooks       
        void Loaded()
        {
            lang.RegisterMessages(messages, this);            
            LoadVariables();
        }        
        void OnServerInitialized()
        {
            if (!ConVar.Server.radiation)
            {
                RadsOn = false;
                ConVar.Server.radiation = true;
            }
            else RadsOn = true;
            FindMonuments();           
        }
        void Unload()
        {
            for (int i = 0; i < RadiationZones.Count; i++)            
                UnityEngine.Object.Destroy(RadiationZones[i]);            
            RadiationZones.Clear();
            if (!RadsOn) ConVar.Server.radiation = false;
        }
        #endregion
      
        #region Functions
        private void FindMonuments()
        {
            if (configData.Options.Using_HapisIsland) { CreateHapis(); return; }

            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("autospawn/monument"))
                {
                    var pos = gobject.transform.position;

                    if (gobject.name.Contains("lighthouse"))
                    {
                        if (configData.Zones.Lighthouse.Activate)
                            CreateZone(configData.Zones.Lighthouse, pos);
                        continue;
                    }

                    if (gobject.name.Contains("powerplant_1"))
                    {
                        if (configData.Zones.Powerplant.Activate)
                            CreateZone(configData.Zones.Powerplant, pos);
                        continue;
                    }

                    if (gobject.name.Contains("military_tunnel_1"))
                    {
                        if (configData.Zones.Tunnels.Activate)
                            CreateZone(configData.Zones.Tunnels, pos);
                        continue;
                    }

                    if (gobject.name.Contains("airfield_1"))
                    {
                        if (configData.Zones.Airfield.Activate)
                            CreateZone(configData.Zones.Airfield, pos);
                        continue;
                    }

                    if (gobject.name.Contains("trainyard_1"))
                    {
                        if (configData.Zones.Trainyard.Activate)
                            CreateZone(configData.Zones.Trainyard, pos);
                        continue;
                    }

                    if (gobject.name.Contains("water_treatment_plant_1"))
                    {
                        if (configData.Zones.WaterTreatment.Activate)
                            CreateZone(configData.Zones.WaterTreatment, pos);
                        continue;
                    }

                    if (gobject.name.Contains("warehouse"))
                    {
                        if (configData.Zones.Warehouse.Activate)
                            CreateZone(configData.Zones.Warehouse, pos);
                        continue;
                    }

                    if (gobject.name.Contains("satellite_dish"))
                    {

                        if (configData.Zones.Satellite.Activate)
                            CreateZone(configData.Zones.Satellite, pos);
                        continue;
                    }

                    if (gobject.name.Contains("sphere_tank"))
                    {
                        if (configData.Zones.Dome.Activate)
                            CreateZone(configData.Zones.Dome, pos);
                        continue;
                    }

                    if (gobject.name.Contains("radtown_small_3"))
                    {
                        if (configData.Zones.Radtown.Activate)
                            CreateZone(configData.Zones.Radtown, pos);
                        continue;
                    }
                }                
            }
            ConfirmCreation();
        }
        private void CreateHapis()
        {
            if (configData.Zones.Lighthouse.Activate)
            {
                CreateZone(new MonumentSettings() { Name = "Lighthouse", Radiation = configData.Zones.Lighthouse.Radiation, Radius = HIMon["lighthouse_1"].Radius }, HIMon["lighthouse_1"].Position);
                CreateZone(new MonumentSettings() { Name = "Lighthouse", Radiation = configData.Zones.Lighthouse.Radiation, Radius = HIMon["lighthouse_2"].Radius }, HIMon["lighthouse_2"].Position);
            }
            if (configData.Zones.WaterTreatment.Activate) CreateZone(new MonumentSettings() { Name = "WaterTreatment", Radiation = configData.Zones.WaterTreatment.Radiation, Radius = HIMon["water"].Radius }, HIMon["water"].Position);
            if (configData.Zones.Tunnels.Activate) CreateZone(new MonumentSettings() { Name = "Tunnels", Radiation = configData.Zones.Tunnels.Radiation, Radius = HIMon["tunnels"].Radius }, HIMon["tunnels"].Position);
            if (configData.Zones.Satellite.Activate) CreateZone(new MonumentSettings() { Name = "Satellite", Radiation = configData.Zones.Satellite.Radiation, Radius = HIMon["satellite"].Radius }, HIMon["satellite"].Position);
            ConfirmCreation();
        }
        private void ConfirmCreation()
        {
            if (RadiationZones.Count > 0)
            {
                if (configData.Options.Use_Timers) StartRadTimers();
                Puts("Created " + RadiationZones.Count + " monument radiation zones");
                if (!ConVar.Server.radiation)
                {
                    RadsOn = false;
                    ConVar.Server.radiation = true;
                }
            }
        }
        private void CreateZone(MonumentSettings zone, Vector3 pos)
        {
            var newZone = new GameObject().AddComponent<RZ>();
            newZone.Activate($"{zone.Name}_{GetRandom()}", pos, zone.Radius, zone.Radiation);
            RadiationZones.Add(newZone);
        }                       
        private void StartRadTimers()
        {
            int ontime = configData.Timers.Static_On;
            int offtime = configData.Timers.Static_Off;
            if (configData.Options.Use_RandomTimers)
            {
                ontime = GetRandom(configData.Timers.Random_OnMin, configData.Timers.Random_OnMax);
                offtime = GetRandom(configData.Timers.Random_OffMin, configData.Timers.Random_OffMax);
            }
            OnTimer = ontime * 60;
            timer.Repeat(1, OnTimer, () =>
            {
                OnTimer--;
                if (OnTimer == 0)
                {                    
                    foreach (var zone in RadiationZones)
                        zone.Deactivate();
                    if (configData.Options.Using_InfoPanel) timer.Once(5, ()=> ConVar.Server.radiation = false);

                    if (configData.Options.Broadcast_Timers)                    
                        MessageAllPlayers(lang.GetMessage("RadsOffMsg", this), offtime);
                    
                    OffTimer = offtime * 60;
                    timer.Repeat(1, OffTimer, () =>
                    {
                        OffTimer--;
                        if (OffTimer == 0)
                        {
                            foreach (var zone in RadiationZones)
                                zone.Reactivate();
                            if (configData.Options.Using_InfoPanel) ConVar.Server.radiation = true;
                            if (configData.Options.Broadcast_Timers)                            
                                MessageAllPlayers(lang.GetMessage("RadsOnMsg", this), ontime);
                            
                            StartRadTimers();
                        }
                    });
                }
            });
        }
        private int GetRandom() => UnityEngine.Random.Range(1, 1000);
        private int GetRandom(int min, int max) => UnityEngine.Random.Range(min, max);        
        private void MessageAllPlayers(string msg, int time) => PrintToChat(string.Format(msg, time));           
        
        void EnterRadiation(BasePlayer player)
        {
            if (configData.Messaging.Display_EnterMessage)
            {
                if (ConVar.Server.radiation == false) return;                              
                SendReply(player, lang.GetMessage("enterMessage", this, player.UserIDString));
            }
        }
        void LeaveRadiation(BasePlayer player)
        {
            if (configData.Messaging.Display_LeaveMessage)
            {
                if (ConVar.Server.radiation == false) return;
                SendReply(player, lang.GetMessage("leaveMessage", this, player.UserIDString));
            }
        }
        #endregion

        #region Commands   
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
                

        [ConsoleCommand("mr_list")]
        void ccmdRadZoneList(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            Puts(lang.GetMessage("monList", this));
            if (RadiationZones.Count == 0) Puts("none");
            foreach (var zone in RadiationZones)
                Puts(zone.name + " ------ " + zone.Position);
        }

        [ChatCommand("mr_list")]
        void chatRadZoneList(BasePlayer player, string command, string[] args)
        {
            if (!isAdmin(player)) return;
            Puts(lang.GetMessage("title", this) + lang.GetMessage("monList", this));
            if (RadiationZones.Count == 0) Puts("none");
            foreach (var zone in RadiationZones)
                Puts(zone.name + "====" + zone.Position);
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

        #region Classes        
        public class RZ : MonoBehaviour
        {
            public Vector3 Position;
            public float ZoneRadius;
            public float RadiationAmount;

            private List<BasePlayer> InZone;

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "RadZone";

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                InZone = new List<BasePlayer>();
            }
            public void Activate(string type, Vector3 pos, float radius, float amount)
            {
                Position = pos;
                ZoneRadius = radius;
                RadiationAmount = amount;

                gameObject.name = type;
                transform.position = Position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;

                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads = Rads ?? gameObject.AddComponent<TriggerRadiation>();
                Rads.RadiationAmount = RadiationAmount;
                Rads.radiationSize = ZoneRadius;
                Rads.interestLayers = playerLayer;
                Rads.enabled = true;

                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");
                InvokeRepeating("UpdateTrigger", 3f, 3f);
            }
            public void Deactivate()
            {
                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads.enabled = false;
                gameObject.SetActive(false);
                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");                
            }
            public void Reactivate()
            {
                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads.enabled = true;
                gameObject.SetActive(true);
                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");
                InvokeRepeating("UpdateTrigger", 3f, 3f);
            }
            private void OnDestroy()
            {
                CancelInvoke("UpdateTrigger");
                Destroy(gameObject);
            }
            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = ZoneRadius;
                }
            }
            private void UpdateTrigger()
            {
                var OutZone = InZone;
                InZone = new List<BasePlayer>();
                int entities = Physics.OverlapSphereNonAlloc(Position, ZoneRadius, colBuffer, playerLayer);
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    if (player != null)
                    {
                        InZone.Add(player);
                        if (!OutZone.Contains(player))
                            Interface.Oxide.CallHook("EnterRadiation", player);
                    }                    
                }
                foreach (var player in OutZone)
                {
                    if (!InZone.Contains(player))
                        Interface.Oxide.CallHook("LeaveRadiation", player);
                }
            }
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

        #region Config
        class MonumentSettings
        {
            public bool Activate;
            public string Name;
            public float Radius;
            public float Radiation;
        }
        class Messaging
        {
            public bool Display_EnterMessage { get; set; }
            public bool Display_LeaveMessage { get; set; }
        }
        class RadiationTimers
        {
            public int Random_OnMin { get; set; }
            public int Random_OnMax { get; set; }
            public int Random_OffMin { get; set; }
            public int Random_OffMax { get; set; }
            public int Static_Off { get; set; }
            public int Static_On { get; set; }
        }
        class RadZones
        {
            public MonumentSettings Airfield { get; set; }
            public MonumentSettings Dome { get; set; }
            public MonumentSettings Lighthouse { get; set; }
            public MonumentSettings Powerplant { get; set; }
            public MonumentSettings Radtown { get; set; }
            public MonumentSettings Satellite { get; set; }
            public MonumentSettings Trainyard { get; set; }
            public MonumentSettings Tunnels { get; set; }
            public MonumentSettings Warehouse { get; set; }
            public MonumentSettings WaterTreatment { get; set; }
        }
        class Options
        {
            public bool Broadcast_Timers { get; set; }
            public bool Using_HapisIsland { get; set; }
            public bool Using_InfoPanel { get; set; }
            public bool Use_Timers { get; set; }
            public bool Use_RandomTimers { get; set; }
        }
        class ConfigData
        {
            public Messaging Messaging { get; set; }
            public RadiationTimers Timers { get; set; }
            public Options Options { get; set; }
            public RadZones Zones { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            var config = new ConfigData
            {
               Messaging = new Messaging
               {
                   Display_EnterMessage = true,
                   Display_LeaveMessage = false
               },
               Timers = new RadiationTimers
               {
                   Random_OffMax = 60,
                   Random_OffMin = 25,
                   Random_OnMax = 30,
                   Random_OnMin = 5,
                   Static_Off = 15,
                   Static_On = 45
               },
               Options = new Options
               {
                   Broadcast_Timers = true,
                   Use_RandomTimers = false,
                   Use_Timers = true,
                   Using_HapisIsland = false,
                   Using_InfoPanel = false
               },
               Zones = new RadZones
               {
                   Airfield = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Airfield",
                       Radiation = 100,
                       Radius = 85
                   },
                   Dome = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Dome",
                       Radiation = 100,
                       Radius = 50
                   },
                   Lighthouse = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Lighthouse",
                       Radiation = 100,
                       Radius = 15
                   },
                   Powerplant = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Powerplant",
                       Radiation = 100,
                       Radius = 120
                   },
                   Radtown = new MonumentSettings
                   {
                       Activate = true,
                       Name = "Radtown",
                       Radiation = 100,
                       Radius = 85
                   },
                   Satellite = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Satellite",
                       Radiation = 100,
                       Radius = 60
                   },
                   Trainyard = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Trainyard",
                       Radiation = 100,
                       Radius = 100
                   },
                   Tunnels = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Tunnels",
                       Radiation = 100,
                       Radius = 90
                   },
                   Warehouse = new MonumentSettings
                   {
                       Activate = false,
                       Name = "Warehouse",
                       Radiation = 100,
                       Radius = 15
                   },
                   WaterTreatment = new MonumentSettings
                   {
                       Activate = false,
                       Name = "WaterTreatment",
                       Radiation = 100,
                       Radius = 120
                   }
               }
        };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
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
