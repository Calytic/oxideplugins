using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Oxide.Core;

using Rust;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnConfig", "Nogrod", "1.0.8")]
    internal class SpawnConfig : RustPlugin
    {
        private const bool Debug = false;
        private const int VersionConfig = 4;
        private readonly FieldInfo PrefabsField = typeof (SpawnPopulation).GetField("Prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo toStringField = typeof(StringPool).GetField("toString", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly FieldInfo toNumberField = typeof(StringPool).GetField("toNumber", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly FieldInfo guidToPathField = typeof(GameManifest).GetField("guidToPath", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly FieldInfo guidToObjectField = typeof(GameManifest).GetField("guidToObject", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly FieldInfo SpawnDistributionsField = typeof(SpawnHandler).GetField("SpawnDistributions", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo SpawnGroupsField = typeof (SpawnHandler).GetField("SpawnGroups", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo SpawnPointsField = typeof (SpawnGroup).GetField("spawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Include,
            Converters = new List<JsonConverter> { new UnityEnumConverter() }
        };

        private ConfigData _config;
        private bool _loaded;
        private bool startup = true;

        private new bool LoadConfig()
        {
            try
            {
                Config.Settings = Settings;
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

        private void Loaded()
        {
            if (!Debug) return;
            SpawnDump();
        }

        [ConsoleCommand("spawn.dump")]
        private void cmdSpawnDump(ConsoleSystem.Arg arg)
        {
            SpawnDump();
        }

        private void OnTerrainInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateSpawns();
            SpawnHandler.Instance.MaxSpawnsPerTick = 200;
            if (startup) SpawnHandler.Instance.UpdateDistributions();
            _loaded = true;
        }

        private void OnServerInitialized()
        {
            startup = false;
            if (!_loaded) OnTerrainInitialized();
            SpawnHandler.Instance.FillPopulations();
            SpawnHandler.Instance.FillGroups();
            SpawnHandler.Instance.EnforceLimits();
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            _config = new ConfigData
            {
                Version = Protocol.network,
                VersionConfig = VersionConfig,
                WorldSize = World.Size,
                WorldSeed = World.Seed
            };
            foreach (var population in SpawnHandler.Instance.SpawnPopulations)
            {
                var data = ToJsonString(population);
                //if ((int)population.Filter.BiomeType < 0)
                //    Puts("Bits: {0} Inv: {1}", Convert.ToString((int)population.Filter.BiomeType, 2), Convert.ToString((int)ToObject<TerrainBiomeEnum>("\"EVERYTHING, "+ToJsonString((TerrainBiomeEnum)~(int)population.Filter.BiomeType).Substring(1)), 2));
                var populationData = ToObject<SpawnPopulationData>(data);
                var prefabs = (Prefab<Spawnable>[])PrefabsField.GetValue(population);
                if (prefabs == null || prefabs.Length == 0)
                {
                    if (!string.IsNullOrEmpty(population.ResourceFolder))
                        PrefabsField.SetValue(population, Prefab.Load<Spawnable>(string.Concat("assets/bundled/prefabs/autospawn/", population.ResourceFolder), GameManager.server, PrefabAttribute.server));
                    if (population.ResourceList != null && population.ResourceList.Length > 0)
                        PrefabsField.SetValue(population, Prefab.Load<Spawnable>(population.ResourceList.Select(x => x.resourcePath).ToArray(), GameManager.server, PrefabAttribute.server));
                }
                //if (population.ResourceList != null) populationData.ResourceList.AddRange(population.ResourceList.Select(r => r.resourcePath));
                prefabs = (Prefab<Spawnable>[])PrefabsField.GetValue(population);
                var counts = prefabs.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.Count());
                foreach (var prefab in counts)
                    populationData.Prefabs.Add(new PrefabData { Prefab = prefab.Key, Weight = prefab.Value });
                if (_config.SpawnPopulations.ContainsKey(population.name)) Puts("SpawnPopulation key already exists: {0}", population.name);
                _config.SpawnPopulations[population.name] = populationData;
            }

            var monuments = Resources.FindObjectsOfTypeAll<MonumentInfo>();
            var spawnGroups = (List<SpawnGroup>)SpawnGroupsField.GetValue(SpawnHandler.Instance);
            var indexes = GetSpawnGroupIndexes(spawnGroups, monuments);
            foreach (var spawnGroup in spawnGroups)
            {
                var data = ToJsonString(spawnGroup);
                var spawnGroupData = ToObject<SpawnGroupData>(data);
                foreach (var spawnEntry in spawnGroup.prefabs)
                    spawnGroupData.Prefabs.Add(new SpawnEntryData {Prefab = spawnEntry.prefab.Get().GetComponent<LootContainer>().PrefabName, Weight = spawnEntry.weight, Mobile = spawnEntry.mobile});
                var spawnPoints = (BaseSpawnPoint[])SpawnPointsField.GetValue(spawnGroup);
                foreach (var spawnPoint in spawnPoints)
                {
                    var spawnPointData = new SpawnPointData { Position = $"{spawnPoint.transform.position.x} {spawnPoint.transform.position.y} {spawnPoint.transform.position.z}" };
                    if (spawnPoint is GenericSpawnPoint)
                    {
                        spawnPointData.DropToGround = ((GenericSpawnPoint)spawnPoint).dropToGround;
                        spawnPointData.RandomRot = ((GenericSpawnPoint)spawnPoint).randomRot;
                    }
                    else if (spawnPoint is RadialSpawnPoint)
                        spawnPointData.Radius = ((RadialSpawnPoint)spawnPoint).radius;
                    spawnGroupData.SpawnPoints[spawnPoint.name] = spawnPointData;
                }
                var key = GetSpawnGroupKey(spawnGroup, monuments, indexes);
                if (_config.SpawnGroups.ContainsKey(key)) Puts("SpawnGroup key already exists: {0}", key);
                _config.SpawnGroups[key] = spawnGroupData;
            }

            try
            {
                Config.WriteObject(_config);
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
            if (_config.Version == Protocol.network && _config.VersionConfig == VersionConfig && _config.WorldSize == World.Size && _config.WorldSeed == World.Seed) return;
            Puts("Incorrect config version({0}/{1}[{2}, {3}])", _config.Version, _config.VersionConfig, _config.WorldSize, _config.WorldSeed);
            if (_config.Version > 0) Config.WriteObject(_config, false, $"{Config.Filename}.old");
            CreateDefaultConfig();
        }

        private void SpawnDump()
        {
            if (SpawnHandler.Instance == null) return;
            var stringBuilder = new StringBuilder();
            var SpawnDistributions = (SpawnDistribution[])SpawnDistributionsField.GetValue(SpawnHandler.Instance);
            if (SpawnHandler.Instance.SpawnPopulations == null)
                stringBuilder.AppendLine("Spawn population array is null.");
            if (SpawnDistributions == null)
                stringBuilder.AppendLine("Spawn distribution array is null.");
            var densityField = typeof(SpawnDistribution).GetField("Density", BindingFlags.NonPublic | BindingFlags.Instance);
            var spawnGroups = (List<SpawnGroup>)SpawnGroupsField.GetValue(SpawnHandler.Instance);
            var containers = new HashSet<LootContainer>();
            foreach (var spawnGroup in spawnGroups)
            {
                stringBuilder.AppendLine("SpawnGroup: " + spawnGroup.name + " - " + spawnGroup.GetInstanceID());
                foreach (var entry in spawnGroup.prefabs)
                {
                    var loot = entry.prefab.Get()?.GetComponent<LootContainer>();
                    containers.Add(loot);
                    if (GameManager.server.FindPrefab(loot.PrefabName) == entry.prefab.Get())
                        stringBuilder.AppendLine("Identical!!!");
                    stringBuilder.AppendLine("\tPrefab: " + loot.PrefabName + " Name: " + entry.prefab.Get()?.name + " CName: " + loot.name + " Weight: " + entry.weight);
                }
            }
            stringBuilder.AppendLine("Containers: " + containers.Count);
            stringBuilder.AppendLine("Containers(global): " + Resources.FindObjectsOfTypeAll<LootContainer>().Length);
            if (SpawnHandler.Instance.SpawnPopulations != null && SpawnDistributions != null)
            {
                for (var i = 0; i < SpawnHandler.Instance.SpawnPopulations.Length; i++)
                {
                    var spawnPopulations = SpawnHandler.Instance.SpawnPopulations[i];
                    var spawnDistributions = SpawnDistributions[i];
                    if (spawnPopulations == null)
                        stringBuilder.AppendLine($"Population #{i} is not set.");
                    else
                    {
                        stringBuilder.AppendLine(string.IsNullOrEmpty(spawnPopulations.ResourceFolder) ? spawnPopulations.name : $"{spawnPopulations.name} (autospawn/{spawnPopulations.ResourceFolder})");
                        stringBuilder.AppendLine("\tPrefabs:");
                        var prefabs = (Prefab<Spawnable>[])PrefabsField.GetValue(spawnPopulations);
                        if (prefabs == null || prefabs.Length == 0)
                        {
                            if (!string.IsNullOrEmpty(spawnPopulations.ResourceFolder))
                                PrefabsField.SetValue(spawnPopulations, Prefab.Load<Spawnable>(string.Concat("assets/bundled/prefabs/autospawn/", spawnPopulations.ResourceFolder), GameManager.server, PrefabAttribute.server));
                            if (spawnPopulations.ResourceList != null && spawnPopulations.ResourceList.Length > 0)
                                PrefabsField.SetValue(spawnPopulations, Prefab.Load<Spawnable>(spawnPopulations.ResourceList.Select(x => x.resourcePath).ToArray(), GameManager.server, PrefabAttribute.server));
                        }
                        if (prefabs == null)
                            stringBuilder.AppendLine("\t\tN/A");
                        else
                        {
                            foreach (var prefab in prefabs)
                                stringBuilder.AppendLine($"\t\t{prefab.Name} - {prefab.Object}");
                        }
                        if (spawnDistributions == null)
                            stringBuilder.AppendLine($"\tDistribution #{i} is not set.");
                        else
                        {
                            var currentCount = SpawnHandler.Instance.GetCurrentCount(spawnPopulations);
                            var targetCount = SpawnHandler.Instance.GetTargetCount(spawnPopulations, spawnDistributions);
                            stringBuilder.AppendLine($"\tPopulation: {currentCount}/{targetCount} Scale: {spawnPopulations.ScaleWithServerPopulation}");
                            stringBuilder.AppendLine($"TerrainMeta X: {TerrainMeta.Size.x} Y: {TerrainMeta.Size.z} Density: {densityField.GetValue(spawnDistributions)} CurrentSpawnDensity: {spawnPopulations.GetCurrentSpawnDensity()}");
                        }
                    }
                    stringBuilder.AppendLine();
                }
            }
            //Puts(stringBuilder.ToString());
            ConVar.Server.Log("oxide/logs/spawns.txt", stringBuilder.ToString());
            //SpawnHandler.Instance.DumpReport("spawn.info.txt");
        }

        private void UpdateSpawns()
        {
            //Puts("Found {0} SpawnPopulations in config.", _config.SpawnPopulations.Count);
            foreach (var spawnPopulation in SpawnHandler.Instance.SpawnPopulations)
            {
                SpawnPopulationData spawnPopulationData;
                if (!_config.SpawnPopulations.TryGetValue(spawnPopulation.name, out spawnPopulationData))
                {
                    Puts("spawnpopulation data '{0}' not found, skipped.", spawnPopulation.name);
                    continue;
                }
                //var spawnPopulation2 = ScriptableObject.CreateInstance<SpawnPopulation>();
                //spawnPopulation2.name = spawnPopuplationData.Key;
                var prefabs = new List<Prefab<Spawnable>>();
                foreach (var prefab in spawnPopulationData.Prefabs)
                {
                    var gameObject = GameManager.server.FindPrefab(prefab.Prefab);
                    var component = gameObject?.GetComponent<Spawnable>();
                    if (component == null)
                    {
                        Puts("Prefab '{0}' not known, skipped.", prefab);
                        continue;
                    }
                    for (var i = 0; i < prefab.Weight; i++)
                        prefabs.Add(new Prefab<Spawnable>(prefab.Prefab, gameObject, component, GameManager.server, PrefabAttribute.server));
                }
                spawnPopulation.AlignToNormal = spawnPopulationData.AlignToNormal;
                spawnPopulation.ClusterDithering = spawnPopulationData.ClusterDithering;
                spawnPopulation.ClusterSizeMax = spawnPopulationData.ClusterSizeMax;
                spawnPopulation.ClusterSizeMin = spawnPopulationData.ClusterSizeMin;
                spawnPopulation.EnforcePopulationLimits = spawnPopulationData.EnforcePopulationLimits;
                spawnPopulation.ScaleWithServerPopulation = spawnPopulationData.ScaleWithServerPopulation;
                spawnPopulation.SpawnRate = spawnPopulationData.SpawnRate;
                spawnPopulation.TargetDensity = spawnPopulationData.TargetDensity;
                spawnPopulation.Filter = spawnPopulationData.Filter.ToSpawnFilter();
                //Puts(spawnPopulation.name + ToJsonString(spawnPopulationData.Filter, false));
                //Puts(spawnPopulation.name + ToJsonString(SpawnFilterData.FromSpawnFilter(spawnPopulation.Filter), false));
                //Puts("Bits: {0} - {1}", Convert.ToString((int)spawnPopulation.Filter.SplatType, 2), Convert.ToString((int)spawnPopulationData.Filter.SplatType, 2));
                PrefabsField.SetValue(spawnPopulation, prefabs.ToArray());
            }
            var monuments = Resources.FindObjectsOfTypeAll<MonumentInfo>();
            var guidToPath = (Dictionary<string, string>)guidToPathField.GetValue(null);
            var guidToObject = (Dictionary<string, UnityEngine.Object>)guidToObjectField.GetValue(null);
            var toNumber = (Dictionary<string, uint>)toNumberField.GetValue(null);
            var toString = (Dictionary<uint, string>)toStringField.GetValue(null);
            var largestKey = toString.Keys.Max();
            var spawnGroups = (List<SpawnGroup>) SpawnGroupsField.GetValue(SpawnHandler.Instance);
            foreach (var spawnGroupData in _config.SpawnGroups)
            {
                foreach (var spawnEntryData in spawnGroupData.Value.Prefabs)
                {
                    var newPrefabPath = $"{GetAssetPath(spawnEntryData.Prefab)}{spawnGroupData.Key.ToLower()}_{Utility.GetFileNameWithoutExtension(spawnEntryData.Prefab).ToLower()}.prefab";
                    UnityEngine.Object prefab;
                    if (!FileSystem.cache.TryGetValue(newPrefabPath, out prefab)) continue;
                    guidToPath.Remove(prefab.name);
                    guidToObject.Remove(prefab.name);
                    FileSystem.cache.Remove(newPrefabPath);
                    UnityEngine.Object.Destroy(prefab);
                }
            }
            //var spawnGroupPrefabsOld = new List<GameObject>();
            var indexes = GetSpawnGroupIndexes(spawnGroups, monuments);
            foreach (var spawnGroup in spawnGroups)
            {
                var key = GetSpawnGroupKey(spawnGroup, monuments, indexes);
                SpawnGroupData spawnGroupData;
                if (!_config.SpawnGroups.TryGetValue(key, out spawnGroupData))
                {
                    Puts("No spawngroup data found: {0}", key);
                    continue;
                }
                spawnGroup.maxPopulation = spawnGroupData.MaxPopulation;
                spawnGroup.numToSpawnPerTickMin = spawnGroupData.NumToSpawnPerTickMin;
                spawnGroup.numToSpawnPerTickMax = spawnGroupData.NumToSpawnPerTickMax;
                spawnGroup.respawnDelayMin = spawnGroupData.RespawnDelayMin;
                spawnGroup.respawnDelayMax = spawnGroupData.RespawnDelayMax;
                //spawnGroupPrefabsOld.AddRange(spawnGroup.prefabs.Where(entry => GameManager.server.FindPrefab(entry.prefab.Get().GetComponent<BaseEntity>().PrefabName) != entry.prefab.Get()).Select(entry => entry.prefab.Get()));
                /*foreach (var entry in spawnGroup.prefabs)
                {
                    var entity = GameManager.server.FindPrefab(entry.prefab.Get().GetComponent<BaseEntity>().PrefabName);
                    var inst = entry.prefab.Get();
                    Puts("{0} - {1} - {2}", entity.GetInstanceID(), inst.GetInstanceID(), GameManager.server.FindPrefab(entry.prefab.Get().GetComponent<BaseEntity>().PrefabName) == entry.prefab.Get());
                }*/
                spawnGroup.prefabs.Clear();
                foreach (var spawnEntryData in spawnGroupData.Prefabs)
                {
                    //var prefab = (GameObject)UnityEngine.Object.Instantiate(GameManager.server.FindPrefab(spawnEntryData.Prefab), default(Vector3), default(Quaternion));
                    var prefab = GameManager.server.CreatePrefab(spawnEntryData.Prefab, default(Vector3), default(Quaternion), false);
                    prefab.name = $"{key}_{Utility.GetFileNameWithoutExtension(spawnEntryData.Prefab)}".ToLower();
                    prefab.GetComponent<BaseEntity>().name = prefab.name;
                    var newPrefabPath = $"{GetAssetPath(spawnEntryData.Prefab)}{prefab.name}.prefab";
                    if (!toNumber.ContainsKey(newPrefabPath))
                    {
                        var newKey = largestKey++;
                        toNumber[newPrefabPath] = newKey;
                        toString[newKey] = newPrefabPath;
                    }
                    var newPrefabRef = new GameObjectRef {guid = prefab.name};
                    guidToPath[prefab.name] = newPrefabPath;
                    guidToObject[prefab.name] = prefab;
                    FileSystem.cache[newPrefabPath] = prefab;
                    spawnGroup.prefabs.Add(new SpawnGroup.SpawnEntry {prefab = newPrefabRef, weight = spawnEntryData.Weight, mobile = spawnEntryData.Mobile});
                }
                var spawnPoints = (BaseSpawnPoint[]) SpawnPointsField.GetValue(spawnGroup);
                foreach (var spawnPoint in spawnPoints)
                {
                    SpawnPointData spawnPointData;
                    if (!spawnGroupData.SpawnPoints.TryGetValue(spawnPoint.name, out spawnPointData))
                    {
                        Puts("spawnpoint data not found: {0} {1}", key, spawnPoint.name);
                        continue;
                    }
                    var position = GetVector3FromString(spawnPointData.Position);
                    if (position == Vector3.zero)
                    {
                        Puts("invalid spawnpoint position: {0} {1} {2}", key, spawnPoint.name, spawnPointData.Position);
                        continue;
                    }
                    spawnPoint.transform.position = position;
                    if (spawnPoint is GenericSpawnPoint)
                    {
                        ((GenericSpawnPoint) spawnPoint).dropToGround = spawnPointData.DropToGround;
                        ((GenericSpawnPoint) spawnPoint).randomRot = spawnPointData.RandomRot;
                    }
                    else if (spawnPoint is RadialSpawnPoint)
                        ((RadialSpawnPoint) spawnPoint).radius = spawnPointData.Radius;
                }
            }
            Puts("Loaded {0} SpawnPopulations and {1} SpawnGroups.", _config.SpawnPopulations.Count, _config.SpawnGroups.Count);
        }

        private static string ToJsonString(object obj, bool limit = true)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = limit ? new DynamicContractResolver() : null,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new UnityEnumConverter() }
            });
        }

        private T ToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        private Vector3 FindCenter(SpawnGroup spawnGroup)
        {
            var spawnPoints = (BaseSpawnPoint[])SpawnPointsField.GetValue(spawnGroup);
            var centroid = new Vector3(0, 0, 0);
            centroid = spawnPoints.Aggregate(centroid, (current, spawnPoint) => current + spawnPoint.transform.position);
            centroid /= spawnPoints.Length;
            return centroid;
        }

        private string GetSpawnGroupKey(SpawnGroup spawnGroup, MonumentInfo[] monuments, Dictionary<SpawnGroup, int> indexes)
        {
            var index = indexes[spawnGroup];
            return $"{GetSpawnGroupId(spawnGroup, monuments)}{(index > 0 ? $"_{index}" : string.Empty)}";
        }

        private string GetSpawnGroupId(SpawnGroup spawnGroup, MonumentInfo[] monuments)
        {
            var centroid = FindCenter(spawnGroup);
            var monument = FindClosest(centroid, monuments);
            return (monument == null ? $"{spawnGroup.name.Replace(" ", "_")}_{Id(centroid)}" : $"{Utility.GetFileNameWithoutExtension(monument.name)}_{spawnGroup.name.Replace(" ", "_")}_{Id(monument)}").ToLower();
        }

        private Dictionary<SpawnGroup, int> GetSpawnGroupIndexes(List<SpawnGroup> spawnGroups, MonumentInfo[] monuments)
        {
            var monumentGroups = new Dictionary<string, List<SpawnGroup>>();
            foreach (var spawnGroup in spawnGroups)
            {
                var monument = GetSpawnGroupId(spawnGroup, monuments);
                List<SpawnGroup> groups;
                if (!monumentGroups.TryGetValue(monument, out groups))
                    monumentGroups[monument] = groups = new List<SpawnGroup>();
                groups.Add(spawnGroup);
            }
            var indexes = new Dictionary<SpawnGroup, int>();
            foreach (var monumentGroup in monumentGroups)
            {
                monumentGroup.Value.Sort((a, b) =>
                {
                    var centerA = FindCenter(a);
                    var centerB = FindCenter(b);
                    if (centerA.y < centerB.y)
                        return -1;
                    if (centerA.y > centerB.y)
                        return 1;
                    return 0;
                });
                for (var i = 0; i < monumentGroup.Value.Count; i++)
                    indexes[monumentGroup.Value[i]] = i;
            }
            return indexes;
        }

        private static string Id(MonoBehaviour entity)
        {
            if (entity == null) return "XYZ";
            return Id(entity.transform.position);
        }

        private static string Id(Vector3 position)
        {
            return $"X{Math.Ceiling(position.x)}Y{Math.Ceiling(position.y)}Z{Math.Ceiling(position.z)}";
        }

        private static MonumentInfo FindClosest(Vector3 point, MonumentInfo[] monumentInfos)
        {
            MonumentInfo monument = null;
            var distance = 9999f;
            foreach (var monumentInfo in monumentInfos)
            {
                if (!monumentInfo.gameObject.activeInHierarchy) continue;
                var curDistance = Vector3.Distance(point, monumentInfo.transform.position);
                if (!(curDistance < distance)) continue;
                distance = curDistance;
                monument = monumentInfo;
            }
            return monument;
        }

        public static string GetAssetPath(string name)
        {
            try
            {
                return name.Substring(0, name.LastIndexOf('/') + 1);
            }
            catch
            {
                return null;
            }
        }

        private static Vector3 GetVector3FromString(string vectorString)
        {
            var vector = Vector3.zero;
            var position = vectorString.Split(' ', ',');
            if (position.Length != 3) return vector;

            float x;
            var result = float.TryParse(position[0], out x);
            float y;
            result |= float.TryParse(position[1], out y);
            float z;
            result |= float.TryParse(position[2], out z);

            if (!result) return vector;
            vector.x = x;
            vector.y = y;
            vector.z = z;
            return vector;
        }

        #region Nested type: ConfigData

        private class ConfigData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public uint WorldSize { get; set; }
            public uint WorldSeed { get; set; }
            public Dictionary<string, SpawnPopulationData> SpawnPopulations { get; } = new Dictionary<string, SpawnPopulationData>();
            public Dictionary<string, SpawnGroupData> SpawnGroups { get; } = new Dictionary<string, SpawnGroupData>();
        }

        #endregion

        #region Nested type: DynamicContractResolver

        private class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive || property.PropertyType == typeof (string)
                       || property.PropertyType == typeof (SpawnFilter)
                       || property.PropertyType == typeof (TerrainBiome.Enum)
                       || property.PropertyType == typeof (TerrainSplat.Enum)
                       || property.PropertyType == typeof (TerrainTopology.Enum)
                       || property.PropertyType == typeof (Vector3);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => p.DeclaringType == type && IsAllowed(p)).ToList();
            }
        }

        #endregion

        #region Nested type: PrefabData

        public class PrefabData
        {
            public string Prefab { get; set; } = string.Empty;
            public int Weight { get; set; } = 1;
        }

        #endregion

        #region Nested type: SpawnEntryData

        public class SpawnEntryData
        {
            public string Prefab { get; set; } = string.Empty;
            public int Weight { get; set; } = 1;
            public bool Mobile { get; set; }
        }

        #endregion

        #region TerrainBiomeEnum enum

        [Flags]
        public enum TerrainBiomeEnum
        {
            Everything = -1,
            Nothing = 0,
            Arid = 1,
            Temperate = 2,
            Tundra = 4,
            Arctic = 8
        }

        #endregion

        #region TerrainSplatEnum enum

        [Flags]
        public enum TerrainSplatEnum
        {
            Everything = -1,
            Nothing = 0,
            Dirt = 1,
            Snow = 2,
            Sand = 4,
            Rock = 8,
            Grass = 16,
            Forest = 32,
            Stones = 64,
            Gravel = 128
        }

        #endregion

        #region TerrainTopologyEnum enum

        [Flags]
        public enum TerrainTopologyEnum
        {
            Everything = -1,
            Nothing = 0,
            Field = 1,
            Cliff = 2,
            Summit = 4,
            Beachside = 8,
            Beach = 16,
            Forest = 32,
            Forestside = 64,
            Ocean = 128,
            Oceanside = 256,
            Decor = 512,
            Monument = 1024,
            Road = 2048,
            Roadside = 4096,
            Bridge = 8192,
            River = 16384,
            Riverside = 32768,
            Lake = 65536,
            Lakeside = 131072,
            Offshore = 262144,
            Powerline = 524288,
            Runway = 1048576,
            Building = 2097152,
            Cliffside = 4194304,
            Mountain = 8388608,
            Clutter = 16777216
            //WATER = 82048,
            //WATERSIDE = 164096,
            //SAND = 197016
        }

        #endregion

        #region Nested type: SpawnFilterData

        public class SpawnFilterData
        {
            public TerrainBiomeEnum BiomeType;
            public TerrainSplatEnum SplatType;
            public TerrainTopologyEnum TopologyAll;
            public TerrainTopologyEnum TopologyAny;
            public TerrainTopologyEnum TopologyNot;

            public SpawnFilter ToSpawnFilter()
            {
                return new SpawnFilter
                {
                    BiomeType = (TerrainBiome.Enum) BiomeType,
                    SplatType = (TerrainSplat.Enum) SplatType,
                    TopologyAll = (TerrainTopology.Enum) TopologyAll,
                    TopologyAny = (TerrainTopology.Enum) TopologyAny,
                    TopologyNot = (TerrainTopology.Enum) TopologyNot
                };
            }

            public static SpawnFilterData FromSpawnFilter(SpawnFilter filter)
            {
                return new SpawnFilterData
                {
                    BiomeType = (TerrainBiomeEnum) filter.BiomeType,
                    SplatType = (TerrainSplatEnum) filter.SplatType,
                    TopologyAll = (TerrainTopologyEnum) filter.TopologyAll,
                    TopologyAny = (TerrainTopologyEnum) filter.TopologyAny,
                    TopologyNot = (TerrainTopologyEnum) filter.TopologyNot
                };
            }
        }

        #endregion

        #region Nested type: SpawnGroupData

        public class SpawnGroupData
        {
            public int MaxPopulation { get; set; } = 5;
            public int NumToSpawnPerTickMin { get; set; } = 1;
            public int NumToSpawnPerTickMax { get; set; } = 2;
            public float RespawnDelayMin { get; set; } = 10f;
            public float RespawnDelayMax { get; set; } = 20f;
            public List<SpawnEntryData> Prefabs { get; set; } = new List<SpawnEntryData>();
            public Dictionary<string, SpawnPointData> SpawnPoints { get; set; } = new Dictionary<string, SpawnPointData>();
        }

        #endregion

        #region Nested type: SpawnPointData

        public class SpawnPointData
        {
            [DefaultValue(10f)]
            public float Radius { get; set; } = 10f;

            [DefaultValue(true)]
            public bool DropToGround { get; set; } = true;

            [DefaultValue(false)]
            public bool RandomRot { get; set; }

            [DefaultValue("0.0 0.0 0.0")]
            public string Position { get; set; }
        }

        #endregion

        #region Nested type: SpawnPopulationData

        public class SpawnPopulationData
        {
            //public string ResourceFolder { get; set; } = string.Empty;
            public float TargetDensity { get; set; } = 1f;
            public float SpawnRate { get; set; } = 1f;
            public int ClusterSizeMin { get; set; } = 1;
            public int ClusterSizeMax { get; set; } = 1;
            public int ClusterDithering { get; set; } = 1;
            public bool EnforcePopulationLimits { get; set; } = true;
            public bool ScaleWithServerPopulation { get; set; }
            public bool AlignToNormal { get; set; }
            public List<PrefabData> Prefabs { get; set; } = new List<PrefabData>();
            //public List<string> ResourceList { get; set; } = new List<string>();
            public SpawnFilterData Filter { get; set; } = new SpawnFilterData();
        }

        #endregion

        #region Nested type: UnityEnumConverter

        private class UnityEnumConverter : StringEnumConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null || ((Enum) value).ToString("G")[0] != '-')
                {
                    base.WriteJson(writer, value, serializer);
                    return;
                }
                var objectType = value.GetType();
                var isNullable = (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof (Nullable<>));
                var t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
                if (!Enum.IsDefined(t, -1))
                {
                    base.WriteJson(writer, value, serializer);
                    return;
                }
                var everything = Enum.GetName(t, -1);
                var tmp = new JTokenWriter();
                base.WriteJson(tmp, Enum.ToObject(t, ~(int) value), serializer);
                var result = tmp.Token.Value<string>();
                var values = new List<string> {everything};
                if (result.IndexOf(',') != -1)
                {
                    var names = result.Split(',');
                    for (var i = 0; i < names.Length; i++)
                        names[i] = names[i].Trim();
                    values.AddRange(names);
                }
                else
                    values.Add(result);
                writer.WriteValue(string.Join(", ", values.ToArray()));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String || reader.Value.ToString().IndexOf(',') == -1) return base.ReadJson(reader, objectType, existingValue, serializer);
                var enumText = reader.Value.ToString();
                var isNullable = (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof (Nullable<>));
                var t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
                if (!Enum.IsDefined(t, -1))
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                var everything = Enum.GetName(t, -1);
                var inverted = false;
                var names = enumText.Split(',');
                for (var i = 0; i < names.Length; i++)
                {
                    names[i] = names[i].Trim();
                    if (!names[i].Equals(everything, StringComparison.OrdinalIgnoreCase)) continue;
                    names[i] = null;
                    inverted = true;
                }
                names = names.Where(n => n != null).ToArray();

                enumText = string.Join(", ", names);
                reader = new JTokenReader(new JValue(enumText));
                reader.Read();
                var result = base.ReadJson(reader, objectType, existingValue, serializer);
                return inverted ? ~(int) result : result;
            }
        }

        #endregion
    }
}
