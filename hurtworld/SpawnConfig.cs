using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnConfig", "Nogrod", "1.0.2")]
    internal class SpawnConfig : HurtworldPlugin
    {
        private const int VersionConfig = 1;
        private ConfigData _config;

        private new bool LoadConfig()
        {
            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    Converters =
                    {
                        new UnityVector3Converter(),
                        new UnityGameObjectConverter(),
                        new StringEnumConverter()
                    }
                };
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

        private new void LoadDefaultConfig()
        {
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();

            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            var resourceSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<ResourceSpawner>();
            var lootSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<LootSpawner>();
            var probabilityBasedResourceSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<ProbabilityBasedResourceSpawner>();
            var creatureSpawnDirectors = UnityEngine.Resources.FindObjectsOfTypeAll<CreatureSpawnDirector>();

            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter(),
                        new UnityVector3Converter(),
                        new UnityGameObjectConverter()
                    },
                    ContractResolver = new DynamicContractResolver()
                };
                Config.WriteObject(new ExportData
                {
                    Version = GameManager.Instance.GetProtocolVersion(),
                    VersionConfig = VersionConfig,
                    MultiSpawners = multiSpawners.ToDictionary(m => m.name),
                    ResourceSpawners = resourceSpawners.ToDictionary(r => r.name),
                    LootSpawners = lootSpawners.ToDictionary(l => l.name),
                    PropabilitySpawners = probabilityBasedResourceSpawners.ToDictionary(p => p.name),
                    CreatureSpawnDirectors = creatureSpawnDirectors.ToDictionary(c => c.name)
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
            if (_config.Version == GameManager.Instance.GetProtocolVersion() && _config.VersionConfig == VersionConfig) return;
            Puts("Incorrect config version({0}/{1})", _config.Version, _config.VersionConfig);
            if (_config.Version > 0) Config.WriteObject(_config, false, $"{Config.Filename}.old");
            CreateDefaultConfig();
        }

        private void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateSpawns();
        }

        private void UpdateSpawns()
        {
            var multiSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<MultiSpawner>();
            var lootSpawners = UnityEngine.Resources.FindObjectsOfTypeAll<LootSpawner>();
            var creatureSpawnDirectors = UnityEngine.Resources.FindObjectsOfTypeAll<CreatureSpawnDirector>();
            foreach (var multiSpawner in multiSpawners)
            {
                MultiSpawnerData multiSpawnerData;
                if (!_config.MultiSpawners.TryGetValue(multiSpawner.name, out multiSpawnerData))
                {
                    Puts("MultiSpawnerData '{0}' not found, skipped.", multiSpawner.name);
                    continue;
                }
                multiSpawner.ChildToSelf = multiSpawnerData.ChildToSelf;
                multiSpawner.InitialWaitTime = multiSpawnerData.InitialWaitTime;
                multiSpawner.MinimumSpawnCount = multiSpawnerData.MinimumSpawnCount;
                multiSpawner.Offset = multiSpawnerData.Offset;
                multiSpawner.SecondsPerSpawn = multiSpawnerData.SecondsPerSpawn;
                multiSpawner.SpawnedLimit = multiSpawnerData.SpawnedLimit;
                multiSpawner.Spawns = multiSpawnerData.Spawns;
            }
            foreach (var lootSpawner in lootSpawners)
            {
                LootSpawnerData lootSpawnerData;
                if (!_config.LootSpawners.TryGetValue(lootSpawner.name, out lootSpawnerData))
                {
                    Puts("LootSpawnerData '{0}' not found, skipped.", lootSpawner.name);
                    continue;
                }
                lootSpawner.ChildToSelf = lootSpawnerData.ChildToSelf;
                lootSpawner.InitialWaitTime = lootSpawnerData.InitialWaitTime;
                lootSpawner.MinimumSpawnCount = lootSpawnerData.MinimumSpawnCount;
                lootSpawner.Offset = lootSpawnerData.Offset;
                lootSpawner.SecondsPerSpawn = lootSpawnerData.SecondsPerSpawn;
                lootSpawner.SpawnedLimit = lootSpawnerData.SpawnedLimit;
                lootSpawner.Spawns = lootSpawnerData.Spawns;
            }
            foreach (var creatureSpawnDirector in creatureSpawnDirectors)
            {
                CreatureSpawnDirectorData creatureSpawnDirectorData;
                if (!_config.CreatureSpawnDirectors.TryGetValue(creatureSpawnDirector.name, out creatureSpawnDirectorData))
                {
                    Puts("CreatureSpawnDirectorData '{0}' not found, skipped.", creatureSpawnDirector.name);
                    continue;
                }
                creatureSpawnDirector.InitializeRandomData = creatureSpawnDirectorData.InitializeRandomData;
                creatureSpawnDirector.SpawnConfigs = creatureSpawnDirectorData.SpawnConfigs.Select(config => new SpawnConfiguration
                {
                    AlignToSurface = config.AlignToSurface,
                    FastestSpawnRateSeconds = config.FastestSpawnRateSeconds,
                    IsDebug = config.IsDebug,
                    Object = config.Object,
                    CellSpawnRates = config.CellSpawnRates
                }).ToArray();
            }
        }

        [ConsoleCommand("spawn.reload")]
        private void cmdConsoleReload(string commandString)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateSpawns();
            Puts("Spawn config reloaded.");
        }

        #region Nested type: ConfigData

        public class ConfigData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, MultiSpawnerData> MultiSpawners { get; set; }
            //public Dictionary<string, ResourceSpawnerData> ResourceSpawners { get; set; }
            public Dictionary<string, LootSpawnerData> LootSpawners { get; set; }
            //public Dictionary<string, ProbabilityBasedResourceSpawnerData> PropabilitySpawners { get; set; }
            public Dictionary<string, CreatureSpawnDirectorData> CreatureSpawnDirectors { get; set; }
        }

        #endregion

        #region Nested type: ExportData
        public class ExportData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<string, MultiSpawner> MultiSpawners { get; set; }
            public Dictionary<string, ResourceSpawner> ResourceSpawners { get; set; }
            public Dictionary<string, LootSpawner> LootSpawners { get; set; }
            public Dictionary<string, ProbabilityBasedResourceSpawner> PropabilitySpawners { get; set; }
            public Dictionary<string, CreatureSpawnDirector> CreatureSpawnDirectors { get; set; }
        }
        #endregion

        #region Nested type: MultiSpawnerData
        public class MultiSpawnerData
        {
            public bool ChildToSelf { get; set; }
            public float SecondsPerSpawn { get; set; }
            public int SpawnedLimit { get; set; }
            public List<Spawn> Spawns { get; set; }
            public Vector3 Offset { get; set; }
            public float InitialWaitTime { get; set; }
            public int MinimumSpawnCount { get; set; }
        }
        #endregion

        #region Nested type: LootSpawnerData
        public class LootSpawnerData
        {
            public bool ChildToSelf { get; set; }
            public float SecondsPerSpawn { get; set; }
            public float SpawnedLimit { get; set; }
            public Vector3 Offset { get; set; }
            public float InitialWaitTime { get; set; }
            public int MinimumSpawnCount { get; set; }
            public List<LootSpawner.LootProbablityPair> Spawns { get; set; }
        }
        #endregion

        #region Nested type: CreatureSpawnDirectorData
        public class CreatureSpawnDirectorData
        {
            public List<SpawnConfigData> SpawnConfigs { get; set; }
            public bool InitializeRandomData { get; set; }
        }
        #endregion

        #region Nested type: SpawnConfigData
        public class SpawnConfigData
        {
            public GameObject Object { get; set; }
            public bool AlignToSurface { get; set; }
            public float FastestSpawnRateSeconds { get; set; }
            public Dictionary<int, int> CellSpawnRates { get; set; }
            public bool IsDebug { get; set; }
        }
        #endregion

        #region Nested type: DynamicContractResolver
        private class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                if (property.PropertyType == typeof(SpawnConfiguration[])) property.Ignored = false;
                if (property.DeclaringType == typeof(SpawnConfiguration) && property.PropertyName.Equals("CellData")) property.Ignored = true;
                return property.PropertyType.IsPrimitive || property.PropertyType == typeof(string)
                       || property.PropertyType == typeof(MultiSpawner)
                       || property.PropertyType == typeof(LootSpawner)
                       || property.PropertyType == typeof(ResourceSpawner)
                       || property.PropertyType == typeof(ProbabilityBasedResourceSpawner)
                       || property.PropertyType == typeof(CreatureSpawnDirector)
                       || property.PropertyType == typeof(SpawnConfiguration[])
                       || property.PropertyType == typeof(Spawn)
                       || property.PropertyType == typeof(ELootConfig)
                       || property.PropertyType == typeof(GameObject)
                       || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                       || property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                       || property.PropertyType == typeof(Vector3);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => (p.DeclaringType == type || p.DeclaringType == typeof(MultiSpawner)) && IsAllowed(p)).ToList();
            }
        }

        #endregion

        #region Nested type: UnityVector3Converter
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

        #region Nested type: UnityGameObjectConverter
        private class UnityGameObjectConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((GameObject)value).name);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var prefabName = reader.Value.ToString();
                var config = NetworkObjectPool.Instance.GetPrefabConfig(prefabName);
                if (config == null)
                {
                    Interface.Oxide.LogInfo("[{0}] Prefab config not found: {1}", nameof(SpawnConfig), prefabName);
                    var prefab = (GameObject) Resources.Load(prefabName);
                    if (prefab == null)
                        Interface.Oxide.LogInfo("[{0}] Prefab failed to load: {1}", nameof(SpawnConfig), prefabName);
                    return prefab;
                }
                return config.Server.Prefab.gameObject;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GameObject);
            }
        }
        #endregion
    }
}
