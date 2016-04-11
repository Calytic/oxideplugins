using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RewardingCraft", "AvalonZone", "0.0.3", ResourceId = 1739)]
    [Description("The more you craft, the more you learn !")]
    public class RewardingCraft : RustPlugin
    {
        private StoredData storedData;

        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NewItemRewarded", "New item reward progression : {0} / {1}"},
                {"RewardProgress", "{0} crafting gained 1 point !"},
                {"NewItemLearned", "You've learned how to craft a new item : {0}"},
                {"RarityLevelChanged", "Current item rarity level changed to {0}"},
                {"EverythingDiscovered", "You know everything about this type {0}"}
            };
            lang.RegisterMessages(messages, this);
        }

        class StoredData
        {
            public Dictionary<ulong, PlayerInfo> Players = new Dictionary<ulong, PlayerInfo>();

            public StoredData()
            {
            }
        }

        class PlayerInfo
        {
            public string UserIDString;
            public ulong UserID;
            public string Name;
            public Dictionary<RCCAT, Skill> SkillTable;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer player)
            {
                UserID = player.userID;
                UserIDString = player.UserIDString;
                Name = player.displayName;
                SkillTable = RewardingCraftTablesGenerator.GenerateSkillTable();
            }

        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!storedData.Players.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
            }
            // RenderUI(player);
        }

        private void RenderUI(BasePlayer player)
        {
            Dictionary<ItemCategory, string> skillColors = new Dictionary<ItemCategory, string>();

            skillColors.Add(ItemCategory.Ammunition, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Attire, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Construction, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Tool, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Traps, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Weapon, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Medical, "0.8 0.4 0 1");
            skillColors.Add(ItemCategory.Items, "0.8 0.4 0 1");

            CuiHelper.DestroyUi(player, "RewardingCraftUI");
            var elements = new CuiElementContainer();

            var mainPanel = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.1 0.1 0.1 0.7"
                },
                RectTransform =
                {
                    //AnchorMin = "0.67 0.04525",
                    AnchorMin = "0.67 0.06525",
                    AnchorMax = "0.819 0.1625"
                }
            }, "HUD/Overlay", "RewardingCraftUI");


            //Add Elements here is the first sub container
            var MainUIElementContainer = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = mainPanel,
                Components =
                        {
                            new CuiImageComponent { Color = "0.4 0.4 0.4 1"},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 0.3" }
                        }
            };
            elements.Add(MainUIElementContainer);

            var TitleTextUIElement = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = MainUIElementContainer.Name,
                Components =
                        {
                            new CuiTextComponent { Color = "0.9 0.9 0.9 1", Text = "My Text",  FontSize = 8, Align = TextAnchor.UpperLeft},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0.5 1" }
                        }
            };
            elements.Add(TitleTextUIElement);

            var ProgressBarUIElementContainer = new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = MainUIElementContainer.Name,
                Components =
                        {
                            new CuiImageComponent { Color = "0.0 0.9 0.0 1"},
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 0.5" }
                        }
            };
            elements.Add(ProgressBarUIElementContainer);

            CuiHelper.AddUi(player, elements);

        }

        void Loaded()
        {
            LoadDefaultMessages();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(RCK.DataFileName);
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(RCK.DataFileName, storedData);
        }

        void CreatePlayerData(BasePlayer player)
        {
            PlayerInfo info = new PlayerInfo(player);

            if (!storedData.Players.ContainsKey(info.UserID))
            {
                storedData.Players.Add(info.UserID, info);
                Interface.Oxide.DataFileSystem.WriteObject(RCK.DataFileName, storedData);
            }
        }

        void OnServerSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(RCK.DataFileName, storedData);
        }

        List<ItemDefinition> GetAvailableItems(BasePlayer player, ItemDefinition ItemDef, RCCAT Category)
        {
            List<ItemDefinition> selectedItemPool = new List<ItemDefinition>();
            PlayerInfo pInfo = storedData.Players[player.userID];

            foreach (ItemBlueprint itemBlueprint in ItemManager.bpList)
            {
                if (itemBlueprint.isResearchable)
                {
                    ItemDefinition item = itemBlueprint.targetItem;
                    if (ItemDef.category == item.category)
                    {
                        if (!player.blueprints.CanCraft(item.itemid, 0))
                        {
                            if (item.rarity <= pInfo.SkillTable[Category].CurrentRarityLevel)
                            {
                                selectedItemPool.Add(item);
                            }
                        }
                    }
                }
            }
            return selectedItemPool;
        }

        void AddSkillPoint(BasePlayer player, RCCAT Category, ItemDefinition ItemDef)
        {
            storedData.Players[player.userID].SkillTable[Category].Level++;

            PlayerInfo pInfo = storedData.Players[player.userID];
            ulong CurrentPoint = pInfo.SkillTable[Category].Level % RCK.LevelStep;

            //Get filtered list of Items
            List<ItemDefinition> selectedItemPool = GetAvailableItems(player, ItemDef, Category);

            // If all items are already discovered, it's time to move to the next rarity level... if possible ;-)
            if (selectedItemPool.Count > 0)
            {
                PrintToChat(player, string.Format(lang.GetMessage("NewItemRewarded", this), new object[] { CurrentPoint, RCK.LevelStep }));
                PrintToChat(player, string.Format(lang.GetMessage("RewardProgress", this), new object[] { ItemDef.category }));

                if (storedData.Players[player.userID].SkillTable[Category].Level % RCK.LevelStep == 0)
                {
                    ItemDefinition selectedItemDefinition = selectedItemPool[UnityEngine.Random.Range(0, selectedItemPool.Count - 1)];
                    player.blueprints.Learn(selectedItemDefinition);
                    PrintToChat(player, string.Format(lang.GetMessage("NewItemLearned", this), new object[] { selectedItemDefinition.displayName.english }));
                }
            }
            else
            {
                if (pInfo.SkillTable[Category].CurrentRarityLevel < Rust.Rarity.VeryRare)
                {
                    storedData.Players[player.userID].SkillTable[Category].CurrentRarityLevel++;
                    PrintToChat(player, string.Format(lang.GetMessage("RarityLevelChanged", this), new object[] { storedData.Players[player.userID].SkillTable[Category].CurrentRarityLevel }));
                }
                else
                {
                    PrintToChat(player, string.Format(lang.GetMessage("EverythingDiscovered", this), new object[] { ItemDef.category.ToString() }));
                }
            }
            Interface.Oxide.DataFileSystem.WriteObject(RCK.DataFileName, storedData);
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            BasePlayer player = task.owner;
            CreatePlayerData(player);
            ItemDefinition currentItemInfo = item.info;

            switch (currentItemInfo.category)
            {
                case ItemCategory.Ammunition:
                    AddSkillPoint(player, RCCAT.AmmunitionCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Attire:
                    AddSkillPoint(player, RCCAT.AttireCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Construction:
                    AddSkillPoint(player, RCCAT.ConstructionCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Tool:
                    AddSkillPoint(player, RCCAT.ToolCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Traps:
                    AddSkillPoint(player, RCCAT.TrapsCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Weapon:
                    AddSkillPoint(player, RCCAT.WeaponCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Medical:
                    AddSkillPoint(player, RCCAT.MedicalCraftingLevel, currentItemInfo);
                    break;
                case ItemCategory.Items:
                    AddSkillPoint(player, RCCAT.ItemsCraftingLevel, currentItemInfo);
                    break;
                default:
                    //Add 1 point to all skills ???
                    break;
            }
            //RenderUI(player);
        }

        public static class RewardingCraftTablesGenerator
        {
            public static Dictionary<RCCAT, Skill> GenerateSkillTable()
            {
                var skillTable = new Dictionary<RCCAT, Skill>();
                var defaultSkill = new Skill(0, Rust.Rarity.Common);

                skillTable.Add(RCCAT.AmmunitionCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.AttireCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.ConstructionCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.GeneralCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.ItemsCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.MedicalCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.ToolCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.TrapsCraftingLevel, defaultSkill);
                skillTable.Add(RCCAT.WeaponCraftingLevel, defaultSkill);

                return skillTable;
            }
        }

        public class Skill
        {
            public ulong Level;
            public Rust.Rarity CurrentRarityLevel;

            public Skill(ulong level, Rust.Rarity rarityLevel)
            {
                Level = level;
                CurrentRarityLevel = rarityLevel;
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum RCCAT
        {
            ToolCraftingLevel,
            WeaponCraftingLevel,
            ItemsCraftingLevel,
            AmmunitionCraftingLevel,
            AttireCraftingLevel,
            ConstructionCraftingLevel,
            MedicalCraftingLevel,
            TrapsCraftingLevel,
            GeneralCraftingLevel
        }

        static class RCK
        {
            public const string DataFileName = "RewardingCraft_Data";
            public const string PluginName = "RewardingCraft";
            public const int LevelStep = 100;
        }
    }
}
