//#define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Oxide.Core;

using Rust;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LootConfig", "Nogrod", "1.0.14")]
    internal class LootConfig : RustPlugin
    {
        private const int VersionConfig = 7;
        private readonly FieldInfo ParentSpawnGroupField = typeof (SpawnPointInstance).GetField("parentSpawnGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo SpawnGroupsField = typeof (SpawnHandler).GetField("SpawnGroups", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo SpawnPointsField = typeof(SpawnGroup).GetField("spawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Regex _findLoot = new Regex(@"(crate[\-_]normal[\-_\d\w]*|loot[\-_]barrel[\-_\d\w]*|loot[\-_]trash[\-_\d\w]*|heli[\-_]crate[\-_\d\w]*|oil[\-_]barrel[\-_\d\w]*|supply[\-_]drop[\-_\d\w]*|trash[\-_]pile[\-_\d\w]*|/dmloot/.*|giftbox[\-_]loot|stocking[\-_](small|large)[\-_]deployed)\.prefab", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private ConfigData _config;
        private Dictionary<string, ItemDefinition> _itemsDict;

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

        private void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            var allPrefabs = GameManifest.Get().pooledStrings.ToList().ConvertAll(p => p.str);
            var prefabs = allPrefabs.Where(p => _findLoot.IsMatch(p)).ToArray();
#if DEBUG
            Puts(string.Join(Environment.NewLine, allPrefabs.ToArray()));
            Puts("Count: " + prefabs.Length);
#endif
            foreach (var source in prefabs)
            {
#if DEBUG
                Puts(source);
#endif
                GameManager.server.FindPrefab(source);
            }
            CheckConfig();
            NextTick(UpdateLoot);
        }

        [ConsoleCommand("loot.reload")]
        private void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateLoot();
            Puts("Loot config reloaded.");
        }

        [ConsoleCommand("loot.dump")]
        private void cmdLootDump(ConsoleSystem.Arg arg)
        {
            LootDump();
        }

        [ConsoleCommand("loot.stats")]
        private void cmdLootStats(ConsoleSystem.Arg arg)
        {
            var lootContainers = Resources.FindObjectsOfTypeAll<LootContainer>();
            var itemModReveal = Resources.FindObjectsOfTypeAll<ItemModReveal>();
            var itemModUnwrap = Resources.FindObjectsOfTypeAll<ItemModUnwrap>();
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var lootContainer in lootContainers)
            {
                sb.AppendLine(lootContainer.name);
                PrintLootSpawn(lootContainer.lootDefinition, 1, sb, 1);
            }
            foreach (var reveal in itemModReveal)
            {
                sb.AppendLine(reveal.name);
                PrintLootSpawn(reveal.revealList, 1, sb, 1);
            }
            foreach (var unwrap in itemModUnwrap)
            {
                sb.AppendLine(unwrap.name);
                PrintLootSpawn(unwrap.revealList, 1, sb, 1);
            }
            var logname = $"oxide/logs/Loot_{DateTime.Now.ToString("yyMMdd_HHmmss")}.txt";
            ConVar.Server.Log(logname, sb.ToString());
            Puts("Stats written to '{0}'", logname);
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            var loot = Resources.FindObjectsOfTypeAll<LootSpawn>();
            var itemModReveal = Resources.FindObjectsOfTypeAll<ItemModReveal>();
            var itemModUnwrap = Resources.FindObjectsOfTypeAll<ItemModUnwrap>();
#if DEBUG
            var sb = new StringBuilder();
            foreach (var reveal in itemModReveal)
            {
                var items = new List<ItemAmount>();
                var stack = new Stack<LootSpawn>();
                stack.Push(reveal.revealList);
                while (stack.Count > 0)
                {
                    var lootSpawn = stack.Pop();
                    if (lootSpawn.subSpawn != null && lootSpawn.subSpawn.Length > 0)
                    {
                        foreach (var entry in lootSpawn.subSpawn)
                        {
                            stack.Push(entry.category);
                        }
                        continue;
                    }
                    if (lootSpawn.items != null) items.AddRange(lootSpawn.items);
                }
                sb.Clear();
                sb.AppendLine(reveal.name);
                sb.AppendLine("Items:");
                foreach (var item in items)
                    sb.AppendLine($"\t{item.itemDef.shortname}: {item.amount}");
                Puts(sb.ToString());
            }
            Puts("LootContainer: {0} LootSpawn: {1} ItemModReveal: {2}", Resources.FindObjectsOfTypeAll<LootContainer>().Length, loot.Length, itemModReveal.Length);
#endif
            var caseInsensitiveComparer = new CaseInsensitiveComparer();
            Array.Sort(loot, (a, b) => caseInsensitiveComparer.Compare(a.name, b.name));
            Array.Sort(itemModReveal, (a, b) => caseInsensitiveComparer.Compare(a.name, b.name));
            Array.Sort(itemModUnwrap, (a, b) => caseInsensitiveComparer.Compare(a.name, b.name));
            var spawnGroupsData = new Dictionary<string, Dictionary<string, LootContainer>>();
            var spawnGroups = (List<SpawnGroup>)SpawnGroupsField.GetValue(SpawnHandler.Instance);
            var monuments = Resources.FindObjectsOfTypeAll<MonumentInfo>();
            var indexes = GetSpawnGroupIndexes(spawnGroups, monuments);
            foreach (var spawnGroup in spawnGroups)
            {
                Dictionary<string, LootContainer> spawnGroupData;
                var spawnGroupKey = GetSpawnGroupKey(spawnGroup, monuments, indexes);
                if (spawnGroup.prefabs == null) continue;
                if (!spawnGroupsData.TryGetValue(spawnGroupKey, out spawnGroupData))
                    spawnGroupsData[spawnGroupKey] = spawnGroupData = new Dictionary<string, LootContainer>();
                foreach (var entry in spawnGroup.prefabs)
                {
                    var lootContainer = entry.prefab?.Get()?.GetComponent<LootContainer>();
                    if (lootContainer == null) continue;
                    spawnGroupData[lootContainer.PrefabName] = lootContainer;
                }
            }
            var containerData = new Dictionary<string, LootContainer>();
            var allPrefabs = GameManifest.Get().pooledStrings.ToList().ConvertAll(p => p.str).Where(p => _findLoot.IsMatch(p)).ToArray();
            Array.Sort(allPrefabs, (a, b) => caseInsensitiveComparer.Compare(a, b));
            foreach (var strPrefab in allPrefabs)
            {
                var prefab = GameManager.server.FindPrefab(strPrefab)?.GetComponent<LootContainer>();
                if (prefab == null) continue;
                containerData[strPrefab] = prefab;
            }
            /*foreach (var container in containers)
            {
                if (container.gameObject.activeInHierarchy || container.GetComponent<SpawnPointInstance>() != null) continue; //skip spawned & spawn groups
                containerData[container.PrefabName] = container;
            }*/
            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new ItemAmountConverter(),
                        new LootSpawnEntryConverter(),
                        new LootContainerConverter(),
                        new LootSpawnConverter(),
                        new ItemModRevealConverter(),
                        new ItemModUnwrapConverter()
                    }
                };
                Config.WriteObject(new ExportData
                {
                    Version = Protocol.network,
                    VersionConfig = VersionConfig,
                    WorldSize = World.Size,
                    WorldSeed = World.Seed,
                    LootContainers = containerData,
                    SpawnGroups = spawnGroupsData.OrderBy(l => l.Key).ToDictionary(l => l.Key, l => l.Value),
                    ItemModReveals = itemModReveal.ToDictionary(l => l.name),
                    ItemModUnwraps = itemModUnwrap.ToDictionary(l => l.name),
                    Categories = loot.ToDictionary(l => l.name)
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
            if (_config.Version == Protocol.network && _config.VersionConfig == VersionConfig && _config.WorldSize == World.Size && _config.WorldSeed == World.Seed) return;
            Puts("Incorrect config version({0}/{1}[{2}, {3}])", _config.Version, _config.VersionConfig, _config.WorldSize, _config.WorldSeed);
            if (_config.Version > 0) Config.WriteObject(_config, false, $"{Config.Filename}.old");
            CreateDefaultConfig();
        }

        private void LootDump()
        {
            var containers = Resources.FindObjectsOfTypeAll<LootContainer>();
            Puts("Containers: {0}", containers.Length);
            foreach (var container in containers)
            {
                Puts("Container: {0} {1} {2}", container.name, container.PrefabName, container.GetInstanceID());
                Puts("Loot: {0} {1}", container.lootDefinition.name, container.lootDefinition.GetInstanceID());
            }
        }

        private static void PrintLootSpawn(LootSpawn lootSpawn, float parentChance, StringBuilder sb, int depth = 0)
        {
            if (lootSpawn.subSpawn != null && lootSpawn.subSpawn.Length > 0)
            {
                sb.Append('\t', depth);
                sb.AppendLine($"{lootSpawn.name} {parentChance:P1}");
                depth++;
                var sum = lootSpawn.subSpawn.Sum(l => l.weight);
                var cur = 0;
                foreach (var entry in lootSpawn.subSpawn)
                {
                    cur += entry.weight;
                    PrintLootSpawn(entry.category, parentChance*(cur/(float) sum), sb, depth);
                }
                return;
            }
            if (lootSpawn.items != null && lootSpawn.items.Length > 0)
            {
                foreach (var amount in lootSpawn.items)
                {
                    sb.Append('\t', depth);
                    sb.AppendLine($"{parentChance:P1} {amount.amount}x {amount.itemDef.shortname} ({lootSpawn.name})");
                }
            }
        }

        private void UpdateLoot()
        {
            _itemsDict = ItemManager.itemList.ToDictionary(i => i.shortname);
            var lootContainers = Resources.FindObjectsOfTypeAll<LootContainer>();
            var lootSpawnsOld = Resources.FindObjectsOfTypeAll<LootSpawn>();
            var itemModReveals = Resources.FindObjectsOfTypeAll<ItemModReveal>();
            var itemModUnwraps = Resources.FindObjectsOfTypeAll<ItemModUnwrap>();
            var monuments = Resources.FindObjectsOfTypeAll<MonumentInfo>();
#if DEBUG
            Puts("LootContainer: {0} LootSpawn: {1} ItemModReveal: {2}", lootContainers.Length, lootSpawnsOld.Length, itemModReveals.Length);
#endif
            var spawnGroups = (List<SpawnGroup>)SpawnGroupsField.GetValue(SpawnHandler.Instance);
            var spawnGroupsEnabled = !spawnGroups.Any(spawnGroup => spawnGroup.prefabs.Any(entry => GameManager.server.FindPrefab(entry.prefab.Get().GetComponent<BaseNetworkable>().PrefabName) == entry.prefab.Get()));
            var lootSpawns = new Dictionary<string, LootSpawn>();
            var indexes = GetSpawnGroupIndexes(spawnGroups, monuments);
            foreach (var lootContainer in lootContainers)
            {
#if DEBUG
                Puts("Update LootContainer: {0} {1} {2}", lootContainer.name, lootContainer.PrefabName, lootContainer.GetInstanceID());
#endif
                if (GameManager.server.FindPrefab(lootContainer.PrefabName) != lootContainer.gameObject)
                {
                    if (lootContainer.GetComponent<SpawnPointInstance>() != null)
                    {
                        if (!spawnGroupsEnabled)
                        {
                            UpdateLootContainer(_config.LootContainers, lootContainer, lootSpawns);
                            continue;
                        }
                        var spawnPointInstance = lootContainer.GetComponent<SpawnPointInstance>();
                        var parentSpawnGroup = (SpawnGroup)ParentSpawnGroupField.GetValue(spawnPointInstance);
                        Dictionary<string, LootContainerData> spawnGroupData;
                        if (!_config.SpawnGroups.TryGetValue(GetSpawnGroupKey(parentSpawnGroup, monuments, indexes), out spawnGroupData))
                        {
                            Puts("No spawngroup data found: {0}", GetSpawnGroupKey(parentSpawnGroup, monuments, indexes));
                            continue;
                        }
                        UpdateLootContainer(spawnGroupData, lootContainer, lootSpawns);
                        continue;
                    }
                    if (lootContainer.GetComponent<Spawnable>() == null)
                    {
                        if (lootContainer.name.Equals(lootContainer.PrefabName) || lootContainer.name.Equals(Core.Utility.GetFileNameWithoutExtension(lootContainer.PrefabName)))
                        {
#if DEBUG
                            var components = lootContainer.GetComponents<MonoBehaviour>();
                            Puts("Name: {0} Identical: {1}\tActiveP: {2}\tActiveC: {3}", lootContainer.name, GameManager.server.FindPrefab(lootContainer.PrefabName) == lootContainer.gameObject, GameManager.server.FindPrefab(lootContainer.PrefabName).activeInHierarchy, lootContainer.gameObject.activeInHierarchy);
                            Puts("Components: {0}", string.Join(",", components.Select(c => c.GetType().FullName).ToArray()));
                            Puts("Position: {0}", lootContainer.transform.position);
#endif
                            UpdateLootContainer(_config.LootContainers, lootContainer, lootSpawns);
                        }
                        continue;
                    }
                }
                UpdateLootContainer(_config.LootContainers, lootContainer, lootSpawns);
            }
            if (spawnGroupsEnabled)
            {
                foreach (var spawnGroup in spawnGroups)
                {
                    Dictionary<string, LootContainerData> spawnGroupData;
                    if (!_config.SpawnGroups.TryGetValue(GetSpawnGroupKey(spawnGroup, monuments, indexes), out spawnGroupData))
                    {
                        Puts("No spawngroup data found: {0}", GetSpawnGroupKey(spawnGroup, monuments, indexes));
                        continue;
                    }
                    foreach (var entry in spawnGroup.prefabs)
                        UpdateLootContainer(spawnGroupData, entry.prefab.Get().GetComponent<LootContainer>(), lootSpawns);
                }
            }
            else
            {
                Puts("No SpawnConfig loaded, skipping SpawnGroups...");
            }
            foreach (var reveal in itemModReveals)
            {
#if DEBUG
                Puts("Update ItemModReveal: {0}", reveal.name);
#endif
                ItemModRevealData revealConfig;
                if (!_config.ItemModReveals.TryGetValue(reveal.name.Replace("(Clone)", ""), out revealConfig))
                {
                    Puts("No reveal data found: {0}", reveal.name.Replace("(Clone)", ""));
                    continue;
                }
                var lootSpawn = GetLootSpawn(revealConfig.RevealList, lootSpawns);
                if (lootSpawn == null)
                {
                    Puts("RevealList category '{0}' for '{1}' not found, skipping", revealConfig.RevealList, reveal.name.Replace("(Clone)", ""));
                    continue;
                }
                reveal.numForReveal = revealConfig.NumForReveal;
                reveal.revealedItemAmount = revealConfig.RevealedItemAmount;
                reveal.revealedItemOverride = GetItem(revealConfig.RevealedItemOverride);
                reveal.revealList = lootSpawn;
            }
            foreach (var unwrap in itemModUnwraps)
            {
#if DEBUG
                Puts("Update ItemModUnwrap: {0}", unwrap.name);
#endif
                ItemModUnwrapData unwrapConfig;
                if (!_config.ItemModUnwraps.TryGetValue(unwrap.name.Replace("(Clone)", ""), out unwrapConfig))
                {
                    Puts("No reveal data found: {0}", unwrap.name.Replace("(Clone)", ""));
                    continue;
                }
                var lootSpawn = GetLootSpawn(unwrapConfig.RevealList, lootSpawns);
                if (lootSpawn == null)
                {
                    Puts("RevealList category '{0}' for '{1}' not found, skipping", unwrapConfig.RevealList, unwrap.name.Replace("(Clone)", ""));
                    continue;
                }
                unwrap.revealList = lootSpawn;
            }
            _itemsDict = null;
            foreach (var lootSpawn in lootSpawnsOld)
                UnityEngine.Object.Destroy(lootSpawn);
        }

        private void UpdateLootContainer(Dictionary<string, LootContainerData> containerData, LootContainer container, Dictionary<string, LootSpawn> lootSpawns)
        {
            if (container == null) return;
            LootContainerData containerConfig;
            if (containerData == null || !containerData.TryGetValue(container.PrefabName, out containerConfig))
            {
                Puts("No container data found: {0}", container.PrefabName);
                return;
            }
            container.maxDefinitionsToSpawn = containerConfig.MaxDefinitionsToSpawn;
            container.minSecondsBetweenRefresh = containerConfig.MinSecondsBetweenRefresh;
            container.maxSecondsBetweenRefresh = containerConfig.MaxSecondsBetweenRefresh;
            container.destroyOnEmpty = containerConfig.DestroyOnEmpty;
            container.distributeFragments = containerConfig.DistributeFragments;
            container.lootDefinition = GetLootSpawn(containerConfig.LootDefinition, lootSpawns);
            container.inventorySlots = containerConfig.InventorySlots;
            container.SpawnType = containerConfig.SpawnType;
            if (!container.gameObject.activeInHierarchy || container.inventory == null) return;
            container.inventory.capacity = containerConfig.InventorySlots;
            container.CancelInvoke("SpawnLoot");
            container.SpawnLoot();
        }

        private LootSpawn GetLootSpawn(string lootSpawnName, Dictionary<string, LootSpawn> lootSpawns)
        {
            LootSpawn lootSpawn;
            if (lootSpawns.TryGetValue(lootSpawnName, out lootSpawn)) return lootSpawn;
            LootSpawnData lootSpawnData;
            if (!_config.Categories.TryGetValue(lootSpawnName, out lootSpawnData))
            {
                Puts("Loot category config not found: {0}", lootSpawnName);
                return null;
            }
            lootSpawns[lootSpawnName] = lootSpawn = ScriptableObject.CreateInstance<LootSpawn>();
            lootSpawn.name = lootSpawnName;
            lootSpawn.items = new ItemAmount[lootSpawnData.Items.Length];
            lootSpawn.subSpawn = new LootSpawn.Entry[lootSpawnData.SubSpawn.Length];
            FillItemAmount(lootSpawn.items, lootSpawnData.Items, lootSpawnName);
            for (var i = 0; i < lootSpawnData.SubSpawn.Length; i++)
            {
                var subSpawn = lootSpawnData.SubSpawn[i];
                var category = GetLootSpawn(subSpawn.Category, lootSpawns);
                lootSpawn.subSpawn[i] = new LootSpawn.Entry { category = category, weight = subSpawn.Weight };
            }
            return lootSpawn;
        }

        private void FillItemAmount(ItemAmount[] amounts, ItemAmountData[] amountDatas, string parent)
        {
            for (var i = 0; i < amountDatas.Length; i++)
            {
                var itemAmountData = amountDatas[i];
                var def = GetItem(itemAmountData.Shortname);
                if (def == null)
                {
                    Puts("Item does not exist: {0} for: {1}", itemAmountData.Shortname, parent);
                    continue;
                }
                if (itemAmountData.Amount <= 0)
                {
                    Puts("Item amount too low: {0} for: {1}", itemAmountData.Shortname, parent);
                    continue;
                }
                amounts[i] = new ItemAmount(def, itemAmountData.Amount);
            }
        }

        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || _itemsDict == null) return null;
            ItemDefinition item;
            return _itemsDict.TryGetValue(shortname, out item) ? item : null;
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

        #region Nested type: ConfigData

        public class ConfigData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public uint WorldSize { get; set; }
            public uint WorldSeed { get; set; }
            public Dictionary<string, LootContainerData> LootContainers { get; set; } = new Dictionary<string, LootContainerData>();
            public Dictionary<string, Dictionary<string, LootContainerData>> SpawnGroups { get; set; } = new Dictionary<string, Dictionary<string, LootContainerData>>();
            public Dictionary<string, ItemModRevealData> ItemModReveals { get; set; } = new Dictionary<string, ItemModRevealData>();
            public Dictionary<string, ItemModUnwrapData> ItemModUnwraps { get; set; } = new Dictionary<string, ItemModUnwrapData>();
            public Dictionary<string, LootSpawnData> Categories { get; set; } = new Dictionary<string, LootSpawnData>();
        }

        #endregion

        #region Nested type: ExportData

        public class ExportData
        {
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public uint WorldSize { get; set; }
            public uint WorldSeed { get; set; }
            public Dictionary<string, LootContainer> LootContainers { get; set; } = new Dictionary<string, LootContainer>();
            public Dictionary<string, Dictionary<string, LootContainer>> SpawnGroups { get; set; } = new Dictionary<string, Dictionary<string, LootContainer>>();
            public Dictionary<string, ItemModReveal> ItemModReveals { get; set; } = new Dictionary<string, ItemModReveal>();
            public Dictionary<string, ItemModUnwrap> ItemModUnwraps { get; set; } = new Dictionary<string, ItemModUnwrap>();
            public Dictionary<string, LootSpawn> Categories { get; set; } = new Dictionary<string, LootSpawn>();
        }

        #endregion

        #region Nested type: ItemAmountConverter

        private class ItemAmountConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var itemAmount = (ItemAmount) value;
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
                return typeof (ItemAmount).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: ItemAmountData

        public class ItemAmountData
        {
            public string Shortname { get; set; }
            public float Amount { get; set; }
        }

        #endregion

        #region Nested type: ItemModRevealConverter

        private class ItemModRevealConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entry = (ItemModReveal) value;
                writer.WriteStartObject();
                writer.WritePropertyName("NumForReveal");
                writer.WriteValue(entry.numForReveal);
                writer.WritePropertyName("RevealedItemOverride");
                writer.WriteValue(entry.revealedItemOverride?.shortname);
                writer.WritePropertyName("RevealedItemAmount");
                writer.WriteValue(entry.revealedItemAmount);
                writer.WritePropertyName("RevealList");
                writer.WriteValue(entry.revealList.name);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (ItemModReveal).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: ItemModRevealData

        public class ItemModRevealData
        {
            public int NumForReveal { get; set; } = 10;
            public string RevealedItemOverride { get; set; }
            public int RevealedItemAmount { get; set; } = 1;
            public string RevealList { get; set; }
        }

        #endregion

        #region Nested type: ItemModUnwrapConverter

        private class ItemModUnwrapConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entry = (ItemModUnwrap)value;
                writer.WriteStartObject();
                writer.WritePropertyName("RevealList");
                writer.WriteValue(entry.revealList.name);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(ItemModUnwrap).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: ItemModUnwrapData

        public class ItemModUnwrapData
        {
            public string RevealList { get; set; }
        }

        #endregion

        #region Nested type: LootContainerConverter

        private class LootContainerConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var container = (LootContainer) value;
                writer.WriteStartObject();
                writer.WritePropertyName("DestroyOnEmpty");
                writer.WriteValue(container.destroyOnEmpty);
                writer.WritePropertyName("LootDefinition");
                writer.WriteValue(container.lootDefinition.name);
                writer.WritePropertyName("MaxDefinitionsToSpawn");
                writer.WriteValue(container.maxDefinitionsToSpawn);
                writer.WritePropertyName("MinSecondsBetweenRefresh");
                writer.WriteValue(container.minSecondsBetweenRefresh);
                writer.WritePropertyName("MaxSecondsBetweenRefresh");
                writer.WriteValue(container.maxSecondsBetweenRefresh);
                writer.WritePropertyName("DistributeFragments");
                writer.WriteValue(container.distributeFragments);
                writer.WritePropertyName("InitialLootSpawn");
                writer.WriteValue(container.initialLootSpawn);
                writer.WritePropertyName("SpawnType");
                writer.WriteValue(container.SpawnType.ToString());
                writer.WritePropertyName("InventorySlots");
                writer.WriteValue(container.inventorySlots);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (LootContainer).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: LootContainerData

        public class LootContainerData
        {
            public bool DestroyOnEmpty { get; set; } = true;
            public string LootDefinition { get; set; }
            public int MaxDefinitionsToSpawn { get; set; }
            public float MinSecondsBetweenRefresh { get; set; } = 3600f;
            public float MaxSecondsBetweenRefresh { get; set; } = 7200f;
            public bool DistributeFragments { get; set; } = true;
            public LootContainer.spawnType SpawnType { get; set; }
            public int InventorySlots { get; set; }
        }

        #endregion

        #region Nested type: LootSpawnConverter

        private class LootSpawnConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var lootSpawn = (LootSpawn) value;
                writer.WriteStartObject();
                writer.WritePropertyName("Items");
                serializer.Serialize(writer, lootSpawn.items);
                writer.WritePropertyName("SubSpawn");
                serializer.Serialize(writer, lootSpawn.subSpawn);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (LootSpawn).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: LootSpawnData

        public class LootSpawnData
        {
            public ItemAmountData[] Items { get; set; } = new ItemAmountData[0];
            public LootSpawnEntryData[] SubSpawn { get; set; } = new LootSpawnEntryData[0];
        }

        #endregion

        #region Nested type: LootSpawnEntryConverter

        private class LootSpawnEntryConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entry = (LootSpawn.Entry) value;
                writer.WriteStartObject();
                writer.WritePropertyName("Category");
                writer.WriteValue(entry.category.name);
                writer.WritePropertyName("Weight");
                writer.WriteValue(entry.weight);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (LootSpawn.Entry).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: LootSpawnEntryData

        public class LootSpawnEntryData
        {
            public string Category { get; set; }
            public int Weight { get; set; }
        }

        #endregion
    }
}
