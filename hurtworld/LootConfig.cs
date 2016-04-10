//#define DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{
    [Info("LootConfig", "Nogrod", "1.0.2", ResourceId = 1550)]
    internal class LootConfig : HurtworldPlugin
    {
        private const int VersionConfig = 3;
        private ConfigData _config;

        private readonly FieldInfo lootConfigsField = typeof(LootCalculator).GetField("_lootConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo childrenField = typeof(LootTreeNode).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance);

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
            CheckConfig();
            NextTick(UpdateLoot);
        }

        [ConsoleCommand("loot.stats")]
        private void cmdLootStats(string commandString)
        {
            var lootConfigs = (Dictionary<ELootConfig, LootTreeNode>)lootConfigsField.GetValue(LootCalculator.Instance);
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var loot in lootConfigs)
            {
                sb.AppendLine(loot.Key.ToString());
                PrintLootNode(loot.Value, 1, sb, 1);
            }
            Puts(sb.ToString());
        }

        [ConsoleCommand("loot.reload")]
        private void cmdConsoleReload(string commandString)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateLoot();
            Puts("Loot config reloaded.");
        }

        [ConsoleCommand("loot.reset")]
        private void cmdConsoleReset(string commandString)
        {
            lootConfigsField.SetValue(LootCalculator.Instance, new Dictionary<ELootConfig, LootTreeNode>());
            LootCalculator.Instance.Start();
            if (!CreateDefaultConfig())
                return;
            CheckConfig();
            UpdateLoot();
            Puts("Loot config reset.");
        }

        private void PrintLootNode(LootTreeNode lootNode, double parentChance, StringBuilder sb, int depth = 0)
        {
            var children = (List<KeyValuePair<double, LootTreeNode>>)childrenField.GetValue(lootNode);
            if (children != null && children.Count > 0)
            {
                sb.Append('\t', depth);
                sb.AppendLine($"{parentChance:P1}");
                depth++;
                var sum = children.Sum(l => l.Key);
                var cur = 0d;
                foreach (var entry in children)
                {
                    cur += entry.Key;
                    PrintLootNode(entry.Value, parentChance * (cur / sum), sb, depth);
                }
                return;
            }
            var itemGeneratorStatic = lootNode.LootResult as ItemGeneratorStatic;
            if (itemGeneratorStatic != null)
            {
                sb.Append('\t', depth);
                sb.AppendLine($"{parentChance:P1} {itemGeneratorStatic.StackSize}x {itemGeneratorStatic.ItemId} ({itemGeneratorStatic.RandomVariance})");
            }
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();

            var lootConfigs = (Dictionary<ELootConfig, LootTreeNode>)lootConfigsField.GetValue(LootCalculator.Instance);

            try
            {
                Config.Settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter(),
                        new LootTreeNodeConverter(),
                        new ItemGeneratorStaticConverter()
                    }
                };
                Config.WriteObject(new ExportData
                {
                    Version = GameManager.Instance.GetProtocolVersion(),
                    VersionConfig = VersionConfig,
                    LootConfigs = lootConfigs
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

        private void UpdateLoot()
        {
            var lootConfigs = new Dictionary<ELootConfig, LootTreeNode>();
            foreach (var nodeData in _config.LootConfigs)
                lootConfigs.Add(nodeData.Key, GetLootTreeNode(nodeData.Value));
            lootConfigsField.SetValue(LootCalculator.Instance, lootConfigs);
        }

        private LootTreeNode GetLootTreeNode(LootTreeNodeData lootTreeNodeData)
        {
            var lootTreeNode = new LootTreeNode
            {
                RollCount = lootTreeNodeData.RollCount,
                RollWithoutReplacement = lootTreeNodeData.RollWithoutReplacement
            };
            if (lootTreeNodeData.LootResult != null)
            {
                var itemGenerator = new ItemGeneratorStatic
                {
                    ItemId = lootTreeNodeData.LootResult.ItemId,
                    RandomVariance = lootTreeNodeData.LootResult.RandomVariance,
                    StackSize = (int) Math.Ceiling(lootTreeNodeData.LootResult.StackSize * _config.GlobalStackSizeMultiplier)
                };
                lootTreeNode.LootResult = itemGenerator;
            }
            foreach (var child in lootTreeNodeData.Children)
                lootTreeNode.AddChild(child.Key, GetLootTreeNode(child.Value));
            return lootTreeNode;
        }

        #region Nested type: ConfigData

        public class ConfigData
        {
            public float GlobalStackSizeMultiplier { get; set; } = 1;
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<ELootConfig, LootTreeNodeData> LootConfigs { get; set; } = new Dictionary<ELootConfig, LootTreeNodeData>();
        }

        #endregion

        #region Nested type: ExportData

        public class ExportData
        {
            public float GlobalStackSizeMultiplier { get; set; } = 1;
            public int Version { get; set; }
            public int VersionConfig { get; set; }
            public Dictionary<ELootConfig, LootTreeNode> LootConfigs { get; set; } = new Dictionary<ELootConfig, LootTreeNode>();
        }

        #endregion

        #region Nested type: LootTreeNodeConverter

        private class LootTreeNodeConverter : JsonConverter
        {
            private readonly FieldInfo childrenField = typeof(LootTreeNode).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance);

            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var node = (LootTreeNode) value;
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(node.RollCount));
                writer.WriteValue(node.RollCount);
                writer.WritePropertyName(nameof(node.RollWithoutReplacement));
                writer.WriteValue(node.RollWithoutReplacement);
                writer.WritePropertyName(nameof(node.LootResult));
                serializer.Serialize(writer, node.LootResult);
                writer.WritePropertyName("Children");
                serializer.Serialize(writer, childrenField.GetValue(node));
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (LootTreeNode).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: LootTreeNodeData

        public class LootTreeNodeData
        {
            public int RollCount { get; set; } = 1;
            public bool RollWithoutReplacement { get; set; } = true;
            public ItemGeneratorData LootResult { get; set; }
            public List<KeyValuePair<double, LootTreeNodeData>> Children { get; set; } = new List<KeyValuePair<double, LootTreeNodeData>>();
        }

        #endregion

        #region Nested type: ItemGeneratorStaticConverter

        private class ItemGeneratorStaticConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var itemGeneratorStatic = (ItemGeneratorStatic)value;
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(itemGeneratorStatic.ItemId));
                writer.WriteValue(itemGeneratorStatic.ItemId.ToString());
                writer.WritePropertyName(nameof(itemGeneratorStatic.StackSize));
                writer.WriteValue(itemGeneratorStatic.StackSize);
                writer.WritePropertyName(nameof(itemGeneratorStatic.RandomVariance));
                writer.WriteValue(itemGeneratorStatic.RandomVariance);
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(ItemGeneratorStatic).IsAssignableFrom(objectType);
            }
        }

        #endregion

        #region Nested type: ItemGeneratorData

        public class ItemGeneratorData
        {
            public EItemCode ItemId { get; set; }
            public int StackSize { get; set; } = 1;
            public int RandomVariance { get; set; }
        }

        #endregion
    }
}
