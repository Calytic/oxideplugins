using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Converters;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("StashSniffer", "k1lly0u", "0.1.1", ResourceId = 2062)]
    class StashSniffer : RustPlugin
    {
        #region Fields
        StoredData storedData;
        private DynamicConfigFile data;

        Dictionary<uint, Vector3> stashCache;
        bool isInit = false;
        bool resetData = false;
        #endregion

        #region Oxide Hooks

        void OnNewSave(string filename) => resetData = true;
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("stashsniffer_data");
            data.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };
            stashCache = new Dictionary<uint, Vector3>();
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"There are {0} small stashes on the map", "There are {0} small stashes on the map" },
                {"nearSyn", "/stash near <radius> <opt:time> - Show all small stash locations within a radius" },
                {"allSyn", "/stash all <opt:time> - Show all small stash locations around the map" },
                {"countSyn", "/stash count - Get current stash count" }
            }, this);
        }
        void OnServerInitialized()
        {
            if (!resetData) LoadData();
            isInit = true;
            if (stashCache.Count < 1)
                FindAllStashes();
        }        
        
        void OnServerSave() => SaveData();
        void OnEntitySpawned(BaseEntity entity, GameObject gameObject)
        {
            if (entity is StashContainer)
            {
                if (!stashCache.ContainsKey(entity.net.ID))
                    stashCache.Add(entity.net.ID, entity.transform.position);
                else stashCache[entity.net.ID] = entity.transform.position;
            }
        }
        private void OnEntityKill(BaseNetworkable entity)
        {
            if (isInit)
            {
                if (stashCache.ContainsKey(entity.net.ID))
                {
                    stashCache.Remove(entity.net.ID);                    
                }
            }
        }
        #endregion

        #region Functions
        void FindAllStashes()
        {
            var containers = UnityEngine.Object.FindObjectsOfType<StashContainer>();
            foreach (var stash in containers)
            {
                if (!stashCache.ContainsKey(stash.net.ID))
                    stashCache.Add(stash.net.ID, stash.transform.position);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("stash")]
        void cmdSniff(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, lang.GetMessage("nearSyn",this,player.UserIDString));
                SendReply(player, lang.GetMessage("allSyn", this, player.UserIDString));
                SendReply(player, lang.GetMessage("countSyn", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "all":
                    {
                        int time = 10;
                        if (args.Length == 2)
                            int.TryParse(args[1], out time);
                        foreach (var stash in stashCache)
                            player.SendConsoleCommand("ddraw.box", time, Color.green, stash.Value, 1f);
                    }
                    return;
                case "near":
                    if (args.Length > 1)
                    {
                        int time = 10;
                        float radius = 20f;
                        float.TryParse(args[1], out radius);
                        if (args.Length > 2)
                            int.TryParse(args[2], out time);
                        foreach (var stash in stashCache)
                        {
                            if (Vector3.Distance(player.transform.position, stash.Value) <= radius)
                                player.SendConsoleCommand("ddraw.box", time, Color.green, stash.Value, 1f);
                        } 
                    }
                    return;
                case "count":
                    SendReply(player, string.Format(lang.GetMessage("There are {0} small stashes on the map", this, player.UserIDString), stashCache.Count));
                    return;
                default:
                    break;
            }
        }

        #endregion

        #region Data Management
        void SaveData()
        {
            storedData.StashIDs = stashCache;
            data.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
                stashCache = storedData.StashIDs;
            }
            catch
            {
                storedData = new StoredData();
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
        }
        class StoredData
        {
            public Dictionary<uint, Vector3> StashIDs = new Dictionary<uint, Vector3>();
        }
        #endregion
    }
}
