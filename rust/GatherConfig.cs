using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using Rust;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GatherConfig", "Nogrod", "1.0.1")]
    class GatherConfig : RustPlugin
    {
        private const int VersionConfig = 1;
        private ConfigData _config;
        private Dictionary<string, ItemDefinition> _itemsDict;
        private readonly Regex _findLoot = new Regex(@"(autospawn\/resource\/(beachside|forest|field|roadside|ores).*|\/(npc|player)\/.*_corpse)\.prefab", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private new void LoadDefaultConfig()
        {
        }

        private new bool LoadConfig()
        {
            try
            {
                Config.Settings = new JsonSerializerSettings();
                if (!Config.Exists())
                    return CreateDefaultConfig();
                _config = Config.ReadObject<ConfigData>();
            }
            catch (Exception e)
            {
                Puts("Config load failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            return true;
        }

        private bool CreateDefaultConfig()
        {
            var allPrefabs = GameManifest.Get().pooledStrings.ToList().ConvertAll(p => p.str);
            var prefabNames = allPrefabs.Where(p => _findLoot.IsMatch(p)).ToArray();
            Array.Sort(prefabNames);
            var prefabData = new Dictionary<string, ExportPrefabData>();
            foreach (var source in prefabNames)
            {
                var dispenser = GameManager.server.FindPrefab(source)?.GetComponent<ResourceDispenser>();
                if (dispenser == null) continue;
                prefabData[source] = new ExportPrefabData
                {
                    Items = dispenser.containedItems
                };
            }
            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new ItemAmountConverter()
                    }
                };
                Config.WriteObject(new ExportData
                {
                    Version = Protocol.network,
                    VersionConfig = VersionConfig,
                    Prefabs = prefabData
                });
            }
            catch (Exception e)
            {
                Puts("Config save failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            Puts("Created new config");
            return LoadConfig();
        }

        private void CheckConfig()
        {
            if (_config.Version == Protocol.network && _config.VersionConfig == VersionConfig) return;
            Puts("Incorrect config version({0}/{1})", _config.Version, _config.VersionConfig);
            if (_config.Version > 0) Config.WriteObject(_config, false, $"{Config.Filename}.old");
            CreateDefaultConfig();
        }

        private void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            var allPrefabs = GameManifest.Get().pooledStrings.ToList().ConvertAll(p => p.str);
            var prefabNames = allPrefabs.Where(p => _findLoot.IsMatch(p)).ToArray();
            //Puts(string.Join(Environment.NewLine, prefabNames));
            //Puts("Count: " + prefabNames.Length + " Config: " + _config.Prefabs.Count);
            foreach (var source in prefabNames)
                GameManager.server.FindPrefab(source);
            CheckConfig();
            NextTick(UpdateGather);
        }

        private void UpdateGather()
        {
            _itemsDict = ItemManager.itemList.ToDictionary(i => i.shortname);
            var dispensers = Resources.FindObjectsOfTypeAll<ResourceDispenser>();
            foreach (var dispenser in dispensers)
            {
                var entity = dispenser.GetComponent<BaseEntity>();
                if (entity == null) continue;
                PrefabData prefabData;
                if (!_config.Prefabs.TryGetValue(entity.PrefabName, out prefabData)) continue;
                dispenser.containedItems.Clear();
                foreach (var itemAmount in prefabData.Items)
                {
                    var def = GetItem(itemAmount.Shortname);
                    if (def == null)
                    {
                        Puts("Item does not exist: {0} for: {1}", itemAmount.Shortname, entity.PrefabName);
                        continue;
                    }
                    dispenser.containedItems.Add(new ItemAmount(def, itemAmount.Amount));
                }
                dispenser.Initialize();
            }
            _itemsDict = null;
        }

        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || _itemsDict == null) return null;
            ItemDefinition item;
            return _itemsDict.TryGetValue(shortname, out item) ? item : null;
        }

        [ConsoleCommand("gather.reload")]
        private void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateGather();
            Puts("Gather config reloaded.");
        }

        public class ConfigData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, PrefabData> Prefabs { get; set; } = new Dictionary<string, PrefabData>();
        }

        public class ExportData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, ExportPrefabData> Prefabs { get; set; } = new Dictionary<string, ExportPrefabData>();
        }

        public class ExportPrefabData
        {
            public List<ItemAmount> Items { get; set; }
        }

        public class PrefabData
        {
            public ItemAmountData[] Items { get; set; } = new ItemAmountData[0];
        }

        public class ItemAmountData
        {
            public string Shortname { get; set; }
            public float Amount { get; set; }
        }

        private class ItemAmountConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var itemAmount = (ItemAmount)value;
                writer.WriteStartObject();
                writer.WritePropertyName("Shortname");
                writer.WriteValue(itemAmount.itemDef.shortname);
                writer.WritePropertyName("Amount");
                writer.WriteValue(itemAmount.amount);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(ItemAmount).IsAssignableFrom(objectType);
            }
        }
    }
}
