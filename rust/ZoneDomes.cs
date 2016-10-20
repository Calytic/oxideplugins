using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZoneDomes", "k1lly0u", "0.1.32", ResourceId = 1945)]
    class ZoneDomes : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin ZoneManager;

        private List<BaseEntity> Spheres = new List<BaseEntity>();

        private bool Active;
        private const string SphereEnt = "assets/prefabs/visualization/sphere.prefab";

        ZDData data;
        private DynamicConfigFile Data;
        #endregion

        #region Data
        class ZDData
        {
            public Dictionary<string, ZoneEntry> Zones = new Dictionary<string, ZoneEntry>();
        }
        class ZoneEntry
        {
            public Vector3 Position;
            public float Radius;
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
        void SaveData()
        {
            Data.WriteObject(data);
        }
        void LoadData()
        {
            try
            {
                data = Data.ReadObject<ZDData>();
            }
            catch
            {
                data = new ZDData();
            }
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            Data = Interface.Oxide.DataFileSystem.GetFile("zonedomes_data");
            Data.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            VerifyDependency();
            LoadData();
            RemoveExisting();
            InitializeDomes();
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity.PrefabName == SphereEnt)
                Spheres.Add(entity);
        }
        void Unload() => DestroyAllSpheres();
        #endregion

        #region Functions
        private void VerifyDependency()
        {
            if (ZoneManager)
                Active = true;
            else
            {
                PrintWarning(GetMsg("noZM"));
                Active = false;
            }
        }
        private void RemoveExisting()
        {
            for (int i = 0; i < Spheres.Count; i++)
            {
                foreach (var entry in data.Zones)
                {
                    if (Spheres[i] != null)
                    {
                        if (Spheres[i].transform.position == entry.Value.Position)
                        {
                            DestroySphere(Spheres[i]);
                        }
                    }
                }
            }
            Spheres.Clear();
        }        
        private void InitializeDomes()
        {
            var removeList = new List<string>();
            foreach(var entry in data.Zones)
            {
                var exists = VerifyZoneID(entry.Key);
                if (exists is string && !string.IsNullOrEmpty((string)exists))
                {
                    CreateSphere(entry.Value.Position, entry.Value.Radius);
                }
                else removeList.Add(entry.Key);
            }
            if (removeList.Count > 0)
            {
                PrintWarning(string.Format(GetMsg("invZone"), removeList.Count));
                foreach (var entry in removeList)
                    data.Zones.Remove(entry);
            }
        }
        private void CreateSphere(Vector3 position, float radius)
        {
            BaseEntity sphere = GameManager.server.CreateEntity(SphereEnt, position, new Quaternion(), true);
            SphereEntity ent = sphere.GetComponent<SphereEntity>();
            ent.currentRadius = radius * 2;
            ent.lerpSpeed = 0f;
            sphere?.Spawn();
            Spheres.Add(sphere);
        }
        private void DestroySphere(BaseEntity sphere)
        {
            if (sphere != null)
            {
                Spheres.Remove(sphere);
                sphere.KillMessage();                
            }
        }
        private void DestroyAllSpheres()
        {
            foreach (var sphere in Spheres)
                if (sphere != null)
                    sphere.KillMessage();
            Spheres.Clear();
        }
        #endregion

        #region ZoneManager Hooks
        private object VerifyZoneID(string zoneid) => ZoneManager?.Call("CheckZoneID", zoneid);
        private object GetZoneLocation(string zoneid) => ZoneManager?.Call("GetZoneLocation", zoneid);
        private object GetZoneRadius(string zoneid) => ZoneManager?.Call("GetZoneRadius", zoneid);
        #endregion

        #region Zone Creation
        [HookMethod("AddNewDome")]
        public bool AddNewDome(BasePlayer player, string ID)
        {
            var zoneid = VerifyZoneID(ID);
            if (zoneid is string && !string.IsNullOrEmpty((string)zoneid))
            {
                if (data.Zones.ContainsKey(ID))
                {
                    SendMsg(player, "", GetMsg("alreadyExists"));
                    return false;
                }
                var pos = GetZoneLocation(ID);
                if (pos != null && pos is Vector3)
                {
                    var radius = GetZoneRadius(ID);
                    if (radius != null && radius is float)
                    {
                        CreateSphere((Vector3)pos, (float)radius);
                        data.Zones.Add(ID, new ZoneEntry { Position = (Vector3)pos, Radius = (float)radius });
                        SaveData();
                        SendMsg(player, ID, GetMsg("newSuccess"));
                        return true;
                    }
                    else
                    {
                        SendMsg(player, ID, GetMsg("noRad"));
                        return false;
                    }
                }
                else
                {
                    SendMsg(player, ID, GetMsg("noLoc"));
                    return false;
                }
            }
            else
            {
                SendMsg(player, ID, GetMsg("noVerify"));
                return false;
            }
        }
        [HookMethod("RemoveExistingDome")]
        public bool RemoveExistingDome(BasePlayer player, string ID)
        {
            if (data.Zones.ContainsKey(ID))
            {
                for (int i = 0; i < Spheres.Count; i++)
                {
                    if (Spheres[i] != null)
                    {
                        if (Spheres[i].transform.position == data.Zones[ID].Position)
                        {
                            DestroySphere(Spheres[i]);
                            data.Zones.Remove(ID);
                            SaveData();
                            SendMsg(player, ID, GetMsg("remSuccess"));
                            return true;
                        }
                    }
                }
                SendMsg(player, ID, GetMsg("noEntity"));
                SendMsg(player, ID, GetMsg("remInvalid"));
                return false;
            }
            else
            {
                SendMsg(player, ID, GetMsg("noInfo"));
                return false;
            }
        }
        #endregion

        #region Chat Commands

        [ChatCommand("zd")]
        private void cmdZoneDome(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin())
            {
                if (args == null || args.Length == 0)
                {
                    SendMsg(player, GetMsg("addSyn"), "/zd add <zoneid>");
                    SendMsg(player, GetMsg("remSyn"), "/zd remove <zoneid>");
                    SendMsg(player, GetMsg("listSyn"), "/zd list");
                    return;
                }
                if (args.Length >= 1)
                {
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 2)
                            {
                                AddNewDome(player, args[1]);
                            }
                            return;
                        case "remove":
                            if (args.Length == 2)
                            {
                                RemoveExistingDome(player, args[1]);
                            }                           
                            return;
                        case "list":
                            Puts(GetMsg("listForm"));
                            foreach (var entry in data.Zones)                               
                                Puts($"{entry.Key} -- {entry.Value.Radius} -- {entry.Value.Position}");
                            return;
                    }
                }
            }
        }
        #endregion

        #region Messaging
        private void SendMsg(BasePlayer player, string message, string keyword) => SendReply(player, $"<color=orange>{keyword}</color><color=#939393>{message}</color>");
        private string GetMsg(string key) => lang.GetMessage(key, this);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"noInfo", "Unable to find information for: " },
            {"noEntity", "Unable to find the sphere entity with for zone ID: " },
            {"remInvalid", "Removing invalid zone data" },
            {"remSuccess", "You have successfully removed the sphere from zone: " },
            {"noVerify", "Unable to verify with ZoneManager the ID: " },
            {"noLoc", "Unable to retrieve location data from ZoneManager for: " },
            {"noRad", "Unable to retrieve radius data from ZoneManager for: " },
            {"newSuccess", "You have successfully created a sphere for the zone: " },
            {"noZM", "Unable to find ZoneManager, unable to proceeed" },
            {"invZone", "Found {0} invalid zones. Removing them from data" },
            {"listForm", "--- Sphere List --- \n <ID> -- <Radius> -- <Position>" },
            {"addSyn", " - Adds a sphere to the zone <zoneid>" },
            {"remSyn", " - Removes a sphere from the zone <zoneid>" },
            {"listSyn", " - Lists all current active spheres and their position" },
            {"alreadyExists", "This zone already has a dome" }
        };

        #endregion
    }
}
