using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Quick Sort", "emu", "0.1.1", ResourceId = 1263)]
    [Description("Allows players to quickly sort their items into containers")]

    public class QuickSort : RustPlugin
    {
        private bool lootAllowed = false;
        private float lootTime = 0;

        private static string s_Deposit = "Quick Sort";
        private static string s_DepositExisting = "Existing";
        private static string s_DepositAll = "All";
        private static string s_DepositWeapon = "Weapons";
        private static string s_DepositAmmo = "Ammo";
        private static string s_DepositMed = "Medical";
        private static string s_DepositAttire = "Attire";
        private static string s_DepositResources = "Resources";
        private static string s_DepositConstruction = "Construction";
        private static string s_DepositItems = "Deployables";
        private static string s_DepositTool = "Tools";
        private static string s_DepositFood = "Food";
        private static string s_DepositTraps = "Traps";
        private static string s_LootAll = "Loot All";

        private void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            if (!target) return;

            player.gameObject.AddComponent<UIDestroyer>();
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(translatedJson, null, null, null, null));
        }

        [ConsoleCommand("autosort.dep")]
        private void AutoDeposit(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;

            BasePlayer player = arg.Player();

            List<Item> itemsSelected;
            List<Item> uselessItems;
            ItemContainer container = GetLootedInventory(player);
            ItemContainer playerMain = player.inventory.containerMain;
            ItemContainer playerWear = player.inventory.containerWear;
            ItemContainer playerBelt = player.inventory.containerBelt;

            if (container != null && playerMain != null)
            {
                if (arg.Args != null && arg.Args.Length == 1)
                {
                    if (arg.Args[0].Equals("ex"))
                    {
                        itemsSelected = GetExistingItems(playerMain, container);
                    }
                    else
                    {
                        ItemCategory category = StringToItemCategory(arg.Args[0]);
                        itemsSelected = GetItemsOfType(playerMain, category);
                    }
                }
                else
                {
                    itemsSelected = CloneItemList(playerMain.itemList);
                    itemsSelected.AddRange(CloneItemList(playerWear.itemList));
                    itemsSelected.AddRange(CloneItemList(playerBelt.itemList));
                }

                uselessItems = GetUselessItems(itemsSelected, container);
                foreach (Item item in uselessItems)
                {
                    itemsSelected.Remove(item);
                }

                itemsSelected.Sort((item1, item2) => item2.info.itemid.CompareTo(item1.info.itemid));
                MoveItems(itemsSelected, container);
            }
        }

        [ConsoleCommand("autosort.loot")]
        private void AutoLoot(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null || !lootAllowed)
                return;

            timer.Once(lootTime, () => DoAutoLoot(arg.Player()));
        }


        [ConsoleCommand("autosort.lootallowed")]
        private void SetLootAllowed(ConsoleSystem.Arg arg)
        {
            if (!arg.isAdmin || arg.Args.Length != 1)
                return;

            bool x;
            if (!bool.TryParse(arg.Args[0], out x))
                return;

            lootAllowed = x;
            Config["LootAllowed"] = lootAllowed;
            SaveConfig();
        }

        [ConsoleCommand("autosort.loottime")]
        private void SetLootTime(ConsoleSystem.Arg arg)
        {
            if (!arg.isAdmin || arg.Args.Length != 1)
                return;

            float x;
            if (!float.TryParse(arg.Args[0], out x))
                return;

            lootTime = x;
            Config["LootTime"] = lootTime;
            SaveConfig();
        }


        private void DoAutoLoot(BasePlayer player)
        {
            List<Item> itemsSelected;
            ItemContainer container = GetLootedInventory(player);
            ItemContainer playerMain = player.inventory.containerMain;

            if (container != null && playerMain != null)
            {
                itemsSelected = CloneItemList(container.itemList);

                itemsSelected.Sort((item1, item2) => item2.info.itemid.CompareTo(item1.info.itemid));
                MoveItems(itemsSelected, playerMain);
            }
        }

        private List<Item> GetUselessItems(List<Item> items, ItemContainer container)
        {
            ItemModCookable cookable;
            List<Item> uselessItems = new List<Item>();
            BaseOven furnace = container.entityOwner.GetComponent<BaseOven>();

            if (furnace != null)
            {
                foreach (Item item in items)
                {
                    cookable = item.info.GetComponent<ItemModCookable>();

                    if (item.info.GetComponent<ItemModBurnable>() == null && (cookable == null || cookable.lowTemp > CookingTemperature(furnace) || cookable.highTemp < CookingTemperature(furnace)))
                        uselessItems.Add(item);
                }
            }

            return uselessItems;
        }

          private float CookingTemperature(BaseOven oven)
          {
              switch (oven.temperature)
              {
                case BaseOven.TemperatureType.Warming:
                  return 50f;
                case BaseOven.TemperatureType.Cooking:
                  return 200f;
                case BaseOven.TemperatureType.Smelting:
                  return 1000f;
                case BaseOven.TemperatureType.Fractioning:
                  return 1500f;
                default:
                  return 15f;
              }
          }

        private List<Item> CloneItemList(List<Item> list)
        {
            List<Item> clone = new List<Item>();

            foreach (Item item in list)
            {
                clone.Add(item);
            }

            return clone;
        }

        private ItemContainer GetLootedInventory(BasePlayer player)
        {
            PlayerLoot playerLoot = player.gameObject.GetComponent<PlayerLoot>();

            if (playerLoot != null && playerLoot.IsLooting())
                return playerLoot.containers[0];
            else
                return null;
        }

        private void MoveItems(List<Item> items, ItemContainer to)
        {
            foreach (Item item in items)
            {
                item.MoveToContainer(to, -1, true);
            }
        }

        private List<Item> GetItemsOfType(ItemContainer container, ItemCategory category)
        {
            List<Item> items = new List<Item>();

            foreach (Item item in container.itemList)
            {
                if (item.info.category == category)
                    items.Add(item);
            }

            return items;
        }

        private ItemCategory StringToItemCategory(string categoryName)
        {
            string[] categoryNames = Enum.GetNames(typeof(ItemCategory));
            for (int i = 0; i < categoryNames.Length; i++)
            {
                if (categoryName.ToLower().Equals(categoryNames[i].ToLower()))
                {
                    return (ItemCategory)i;
                }
            }
            return (ItemCategory)categoryNames.Length;
        }

        private List<Item> GetExistingItems(ItemContainer primary, ItemContainer secondary)
        {
            List<Item> existingItems = new List<Item>();
            if (primary != null && secondary != null)
            {
                for (int i = 0; i < primary.itemList.Count; i++)
                {
                    for (int j = 0; j < secondary.itemList.Count; j++)
                    {
                        if (primary.itemList[i].info.itemid == secondary.itemList[j].info.itemid)
                        {
                            existingItems.Add(primary.itemList[i]);
                            break;
                        }
                    }
                }
            }
            return existingItems;
        }

        class UIDestroyer : MonoBehaviour
        {
            private void PlayerStoppedLooting(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("pnlSorting", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDeposit", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositExisting", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositExisting", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositAll", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositAll", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnLootAll", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblLootAll", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositWeapon", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositWeapon", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositAmmo", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositAmmo", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositMed", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositMed", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositAttire", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositAttire", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositResources", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositResources", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositConstruction", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositConstruction", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositItems", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositItems", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositTool", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositTool", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositFood", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositFood", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("btnDepositTraps", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("lblDepositTraps", null, null, null, null));
                Destroy(this);
            }
        }

        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                return defaultValue;
            }
        }

        private static string translatedJson = "";

        void InitTranslation()
        {
            translatedJson = json.Replace("{s_Deposit}", s_Deposit);
            translatedJson = translatedJson.Replace("{s_DepositExisting}", s_DepositExisting);
            translatedJson = translatedJson.Replace("{s_DepositAll}", s_DepositAll);
            translatedJson = translatedJson.Replace("{s_DepositWeapon}", s_DepositWeapon);
            translatedJson = translatedJson.Replace("{s_DepositAmmo}", s_DepositAmmo);
            translatedJson = translatedJson.Replace("{s_DepositMed}", s_DepositMed);
            translatedJson = translatedJson.Replace("{s_DepositAttire}", s_DepositAttire);
            translatedJson = translatedJson.Replace("{s_DepositResources}", s_DepositResources);
            translatedJson = translatedJson.Replace("{s_DepositConstruction}", s_DepositConstruction);
            translatedJson = translatedJson.Replace("{s_DepositItems}", s_DepositItems);
            translatedJson = translatedJson.Replace("{s_DepositTool}", s_DepositTool);
            translatedJson = translatedJson.Replace("{s_DepositFood}", s_DepositFood);
            translatedJson = translatedJson.Replace("{s_DepositTraps}", s_DepositTraps);
            translatedJson = translatedJson.Replace("{s_LootAll}", s_LootAll);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            Config["LootAllowed"] = lootAllowed;
            Config["LootTime"] = lootTime;

            SaveConfig();
        }

        private void LoadConfigValues()
        {
            lootAllowed = GetConfig<bool>("LootAllowed", lootAllowed);
            lootTime = GetConfig<float>("LootTime", lootTime);
        }

        void OnServerInitialized()
        {
            InitTranslation();
            LoadConfigValues();
        }

        #region UI
        const string json = @"[
                        {
                            ""parent"": ""HUD/Overlay"",
                            ""name"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Image"",
                                    ""color"": ""0.5 0.5 0.5 0.33""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.675 0.68"",
                                    ""anchormax"": ""0.96 0.86""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDeposit"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_Deposit}"",
                                    ""fontSize"":16,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.02 0.8"",
                                    ""anchormax"": ""0.3 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositExisting"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep ex"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.02 0.6"",
                                    ""anchormax"": ""0.3 0.8""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositExisting"",
                            ""parent"": ""btnDepositExisting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositExisting}"",
                                    ""fontSize"":16,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositAll"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.02 0.35"",
                                    ""anchormax"": ""0.3 0.55""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositAll"",
                            ""parent"": ""btnDepositAll"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositAll}"",
                                    ""fontSize"":16,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnLootAll"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.loot"",
                                    ""color"": ""0 0.7 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.02 0.05"",
                                    ""anchormax"": ""0.3 0.3""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblLootAll"",
                            ""parent"": ""btnLootAll"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_LootAll}"",
                                    ""fontSize"":16,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositWeapon"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep weapon"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.8"",
                                    ""anchormax"": ""0.63 0.99""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositWeapon"",
                            ""parent"": ""btnDepositWeapon"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositWeapon}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositAmmo"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep ammunition"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.59"",
                                    ""anchormax"": ""0.63 0.78""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositAmmo"",
                            ""parent"": ""btnDepositAmmo"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositAmmo}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositMed"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep medical"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.39"",
                                    ""anchormax"": ""0.63 0.58""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositMed"",
                            ""parent"": ""btnDepositMed"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositMed}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositAttire"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep attire"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.19"",
                                    ""anchormax"": ""0.63 0.38""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositAttire"",
                            ""parent"": ""btnDepositAttire"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositAttire}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositResources"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep resources"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.35 0.01"",
                                    ""anchormax"": ""0.63 0.18""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositResources"",
                            ""parent"": ""btnDepositResources"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositResources}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositConstruction"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep construction"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.67 0.8"",
                                    ""anchormax"": ""0.95 0.99""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositConstruction"",
                            ""parent"": ""btnDepositConstruction"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositConstruction}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositItems"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep items"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.67 0.59"",
                                    ""anchormax"": ""0.95 0.78""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositItems"",
                            ""parent"": ""btnDepositItems"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositItems}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositTool"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep tool"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.67 0.39"",
                                    ""anchormax"": ""0.95 0.58""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositTool"",
                            ""parent"": ""btnDepositTool"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositTool}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositFood"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep food"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.67 0.19"",
                                    ""anchormax"": ""0.95 0.38""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositFood"",
                            ""parent"": ""btnDepositFood"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositFood}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""name"": ""btnDepositTraps"",
                            ""parent"": ""pnlSorting"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""command"":""autosort.dep traps"",
                                    ""color"": ""1 0.5 0 0.5"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.67 0.01"",
                                    ""anchormax"": ""0.95 0.18""
                                }
                            ]
                        },
                        {
                            ""name"": ""lblDepositTraps"",
                            ""parent"": ""btnDepositTraps"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{s_DepositTraps}"",
                                    ""fontSize"":14,
                                    ""align"": ""MiddleCenter""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        }
                        ]";
        #endregion
    }
}
