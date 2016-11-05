using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Rust;
using Oxide.Core;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("RadPockets", "k1lly0u", "2.0.1", ResourceId = 1492)]
    class RadPockets : RustPlugin
    {
        #region Fields  
        StoredData storedData;
        private DynamicConfigFile data;

        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);

        private List<RZ> RadiationZones;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("radpockets_data");
            data.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };
            RadiationZones = new List<RZ>();
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("radpockets.use", this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            ConVar.Server.radiation = true;
            if (storedData.radData.Count == 0)
                CreateNewZones();
            else
            {
                foreach (var zone in storedData.radData)
                    CreateZone(zone);
                Puts($"Re-initalized {storedData.radData.Count} RadPockets");
            }
        }
        void Unload()
        {
            DestroyAllZones();
            DestroyAllComponents();
        }        
        #endregion

        #region Functions
        void DestroyAllZones()
        {
            for (int i = 0; i < RadiationZones.Count; i++)
                UnityEngine.Object.Destroy(RadiationZones[i]);
            RadiationZones.Clear();
        }
        void DestroyAllComponents()
        {
            var components = UnityEngine.Object.FindObjectsOfType<RZ>();
            if (components != null)
                foreach (var comp in components)
                    UnityEngine.Object.Destroy(comp);
        }
        void CreateNewZones()
        {
            int amountToCreate = GetRandom(configData.Count_Min, configData.Count_Max);
            for (int i = 0; i < amountToCreate; i++)
            {
                CreateZone(new PocketData
                {
                    amount = GetRandom(configData.Radiation_Min, configData.Radiation_Max),
                    position = GetRandomPos(),
                    radius = GetRandom(configData.Radius_Min, configData.Radius_Max)
                }, true);
            }
            SaveData();
            Puts($"Successfully created {amountToCreate} radiation pockets");
        }
        void CreateZone(PocketData zone, bool isNew = false, bool save = false)
        {
            var newZone = new GameObject().AddComponent<RZ>();
            newZone.Activate(zone);
            RadiationZones.Add(newZone);
            if (isNew) storedData.radData.Add(zone);
            if (save) SaveData();            
        }
        private Vector3 GetRandomPos()
        {
            int mapSize = Convert.ToInt32((TerrainMeta.Size.x / 2) - 600);

            int X = GetRandom(-mapSize, mapSize);
            int Y = GetRandom(-mapSize, mapSize);

            return new Vector3(X, TerrainMeta.HeightMap.GetHeight(new Vector3(X, 0, Y)), Y);            
        }        
        #endregion

        #region Helpers
        static int GetRandom() => UnityEngine.Random.Range(1, 10000);
        static int GetRandom(int min, int max) => UnityEngine.Random.Range(min, max);
        #endregion

        #region Chat Commands
        [ChatCommand("rp")]
        void cmdRP(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin() || permission.UserHasPermission(player.UserIDString, "radpockets.use"))
            {
                if (args == null || args.Length == 0)
                {
                    SendReply(player, $"<color=#00CC00>{Title}  </color><color=#939393>v </color><color=#00CC00>{Version}</color>");
                    SendReply(player, $"<color=#00CC00>/rp showall</color> - {Msg("showallsyn", player.UserIDString, true)}");
                    SendReply(player, $"<color=#00CC00>/rp shownear <opt:radius></color> - {Msg("shownearsyn", player.UserIDString, true)}");
                    SendReply(player, $"<color=#00CC00>/rp removeall</color> - {Msg("removeallsyn", player.UserIDString, true)}");
                    SendReply(player, $"<color=#00CC00>/rp removenear <opt:radius></color> - {Msg("removenearsyn", player.UserIDString, true)}");
                    SendReply(player, $"<color=#00CC00>/rp tpnear</color> - {Msg("tpnearsyn", player.UserIDString, true)}");
                    SendReply(player, $"<color=#00CC00>/rp create <radius> <radiation></color> - {Msg("createsyn", player.UserIDString, true)}");
                    return;
                }
                switch (args[0].ToLower())
                {
                    case "showall":
                        foreach (var zone in RadiationZones)
                            player.SendConsoleCommand("ddraw.box", 10f, Color.green, zone.data.position, 1f);
                        return;
                    case "shownear":
                        {
                            float distance = 0;
                            if (args.Length >= 2)
                                if (!float.TryParse(args[1], out distance))
                                    distance = 10f;
                            foreach (var zone in RadiationZones)
                            {
                                if (Vector3.Distance(zone.data.position, player.transform.position) <= distance)
                                    player.SendConsoleCommand("ddraw.box", 10f, Color.green, zone.data.position, 1f);
                            }
                        }
                        return;
                    case "removeall":
                        DestroyAllZones();
                        DestroyAllComponents();
                        storedData.radData.Clear();
                        SaveData();
                        SendReply(player, Msg("removedall", player.UserIDString));
                        return;
                    case "removenear":
                        {
                            float distance = 0;
                            if (args.Length >= 2)
                                if (!float.TryParse(args[1], out distance))
                                    distance = 10f;
                            int destCount = 0;
                            for (int i = 0; i < RadiationZones.Count; i++)
                            {
                                if (Vector3.Distance(RadiationZones[i].data.position, player.transform.position) <= distance)
                                {
                                    foreach (var entry in storedData.radData)
                                    {
                                        if (entry.position == RadiationZones[i].data.position)
                                        {
                                            storedData.radData.Remove(entry);
                                            SaveData();
                                            break;
                                        }
                                    }
                                    UnityEngine.Object.Destroy(RadiationZones[i]);
                                    RadiationZones.Remove(RadiationZones[i]);
                                    destCount++;

                                }
                            }
                            SendReply(player, Msg("zonesdestroyed", player.UserIDString, true).Replace("{count}", $"</color><color=#00CC00>{destCount}</color><color=#939393>"));
                        }
                        return;
                    case "tpnear":
                        object closestPosition = null;
                        float closestDistance = 4000;
                        foreach (var zone in RadiationZones)
                        {
                            var distance = Vector3.Distance(zone.data.position, player.transform.position);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestPosition = zone.data.position;
                            }
                        }
                        if (closestPosition is Vector3)
                            player.MovePosition((Vector3)closestPosition);
                        return;
                    case "create":
                        if (args.Length >= 3)
                        {
                            float distance = 0;
                            float radAmount = 0;
                            if (!float.TryParse(args[1], out distance))
                            {
                                SendReply(player, string.Format(Msg("notanumber", player.UserIDString, true), "distance"));
                                return;
                            }
                            if (!float.TryParse(args[2], out radAmount))
                            {
                                SendReply(player, string.Format(Msg("notanumber", player.UserIDString, true), "radiation amount"));
                                return;
                            }
                            CreateZone(new PocketData
                            {
                                amount = radAmount,
                                position = player.transform.position,
                                radius = distance
                            }, true, true);
                            SendReply(player, Msg("createsuccess", player.UserIDString, true)
                                .Replace("{radius}", $"</color><color=#00CC00>{distance}</color><color=#939393>")
                                .Replace("{radamount}", $"</color><color=#00CC00>{radAmount}</color><color=#939393>")
                                .Replace("{pos}", $"</color><color=#00CC00>{player.transform.position}</color>"));
                            return;
                        }
                        else SendReply(player, $"<color=#00CC00>/rp create <radius> <radiation></color> - {Msg("createsyn", player.UserIDString, true)}");
                        return;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Radiation Control
        class PocketData
        {
            public Vector3 position;
            public float radius;
            public float amount;
        }
        class RZ : MonoBehaviour
        {
            public PocketData data;  
            private List<BasePlayer> InZone;
            void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = $"radpocket_{GetRandom()}";

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                InZone = new List<BasePlayer>();
            }
            public void Activate(PocketData data)
            {
                this.data = data;
                
                transform.position = data.position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;

                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads = Rads ?? gameObject.AddComponent<TriggerRadiation>();
                Rads.RadiationAmountOverride = data.amount;
                Rads.radiationSize = data.radius;
                Rads.interestLayers = playerLayer;
                Rads.enabled = true;

                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");
                InvokeRepeating("UpdateTrigger", 3f, 3f);
            }            
            void OnDestroy()
            {
                CancelInvoke("UpdateTrigger");
                Destroy(gameObject);
            }
            internal void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = data.radius;
                }
            }
            internal void UpdateTrigger()
            {
                InZone = new List<BasePlayer>();
                int entities = Physics.OverlapSphereNonAlloc(data.position, data.radius, colBuffer, playerLayer);
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    if (player != null)                    
                        InZone.Add(player); 
                }                
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int Radius_Min { get; set; }
            public int Radius_Max { get; set; }
            public int Count_Min { get; set; }
            public int Count_Max { get; set; }
            public int Radiation_Min { get; set; }
            public int Radiation_Max { get; set; }
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
                Count_Max = 30,
                Count_Min = 15,
                Radiation_Max = 25,
                Radiation_Min = 2,
                Radius_Max = 60,
                Radius_Min = 15
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }
        class StoredData
        {
            public List<PocketData> radData = new List<PocketData>();
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
        }
        #endregion

        #region Localization
        string Msg(string key, string playerid = null, bool color = false)
        {
            if (color) return $"<color=#939393>{lang.GetMessage(key, this, playerid)}</color>";
            else return lang.GetMessage(key, this, playerid);
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"showallsyn", "Shows all RadPockets" },
            {"shownearsyn", "Shows nearby RadPockets within optional radius (default 10)" },
            {"removeallsyn", "Removes all RadPockets" },
            {"removenearsyn", "Removes RadPockets within optional radius (default 10)" },
            {"tpnearsyn", "Teleport to the closest RadPocket" },
            {"createsyn", "Create a new RadPocket on your location, requires a radius and radiation amount" },
            {"zonesdestroyed", "Destroyed {count} pockets" },
            {"notanumber", "You must enter a number value for {0}" },
            {"createsuccess", "You have successfully created a new RadPocket with a radius of {radius}, radiation amount of {radamount}, and position of {pos}" },
            {"removedall", "Successfully removed all Radiation Pockets" }
        };
        #endregion
    }
}
