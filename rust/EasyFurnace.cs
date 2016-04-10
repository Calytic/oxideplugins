using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("EasyFurnace", "Deicide666ra", "1.0.2", ResourceId = 1155)]
    class EasyFurnace: RustPlugin
    {
        Dictionary<string, ItemDefinition> g_itemDefinitions = new Dictionary<string, ItemDefinition>();

        void OnServerInitialized()
        {
            g_itemDefinitions.Clear();
            var gameObjectArray = FileSystem.LoadAll<GameObject>("Assets/Items/");
            g_itemDefinitions = (gameObjectArray.Select(x => x.GetComponent<ItemDefinition>()).Where(x => x != null)).ToDictionary(x => x.shortname);
        }

        int GetStackSize(string shortname)
        {
            ItemDefinition item= null;
            var ret= g_itemDefinitions.TryGetValue(shortname, out item);
            if (!ret) throw new Exception("failed to get stack size for " + shortname);
            return item.stackable;
        }

        Dictionary<BaseOven, BasePlayer> g_lootCache = new Dictionary<BaseOven, BasePlayer>();
        
        void OnPlayerLoot(PlayerLoot lootInventory, object lootable)
        {
            var looter = lootInventory.GetComponent("BasePlayer") as BasePlayer;
            if (looter == null) return;

            var furnace = lootable as BaseOven;
            if (furnace == null) return;

            g_lootCache[furnace]= looter;
        }

        int RemoveItemsFromInventory(BasePlayer player, string shortname, int amount)
        {
            ItemDefinition itemToRemove = null;
            var ret = g_itemDefinitions.TryGetValue(shortname, out itemToRemove);
            var foundItems = player.inventory.FindItemIDs(itemToRemove.itemid);
            var nbFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (nbFound < amount) amount = nbFound;

            var nbRemoved = player.inventory.Take(foundItems, itemToRemove.itemid, amount);
            return nbRemoved;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            try {
                // Check if we're moving ore
                if (item.info.shortname != "metal_ore" && item.info.shortname != "sulfur_ore") return;

                // ignore tiny stacks
                if (item.amount < 100) return;

                // Make sure furnace is empty (except for the ore we just moved)
                if (container.itemList.Count() > 1) return;

                // Check if we're moving it to a furnace
                BaseOven furnace = UnityEngine.Object.FindObjectsOfType<BaseOven>().FirstOrDefault(x => x.inventory == container);
                if (furnace == null) return;

                // Try to identify the looter based on stored info (ugly hack)
                BasePlayer player;
                if (g_lootCache.TryGetValue(furnace, out player) == false || player == null) return;

                // Remove required wood from inventory
                int woodToRetain = (int)Math.Ceiling(item.amount * 1.25);
                int woodMaxStack = GetStackSize("wood");
                if (woodToRetain > woodMaxStack)
                    woodToRetain = woodMaxStack;

                var retainedWood= RemoveItemsFromInventory(player, "wood", woodToRetain);
                if (retainedWood == 0)
                    return;

                // Remove required metal frags from inventory
                if (item.info.shortname == "metal_ore")
                {
                    var retainedMetal = RemoveItemsFromInventory(player, "metal_fragments", 1);
                    if (retainedMetal == 0)
                    {
                        // refund wood
                        ItemManager.Create(g_itemDefinitions["wood"], retainedWood).MoveToContainer(player.inventory.containerMain);
                        return;
                    }
                }

                // Empty furnace (the ore player just added)
                item.RemoveFromContainer();

                // Add wood we retained
                ItemManager.Create(g_itemDefinitions["wood"], retainedWood).MoveToContainer(container);

                // Add metal frag we retained
                if (item.info.shortname == "metal_ore")
                    ItemManager.Create(g_itemDefinitions["metal_fragments"], 1).MoveToContainer(container);

                // Split and add the ore back in
                if (item.info.shortname == "metal_ore")
                {
                    // split metal in 4 equal parts
                    var item1 = item.SplitItem(item.amount / 2);
                    var item2 = item;
                    var item3 = item1.SplitItem(item1.amount / 2);
                    var item4 = item2.SplitItem(item2.amount / 2);

                    item1.MoveToContainer(container, -1, false);
                    item2.MoveToContainer(container, -1, false);
                    item3.MoveToContainer(container, -1, false);
                    item4.MoveToContainer(container, -1, false);
                }
                else 
                {
                    // for sulfur, we just split it in two
                    var item1 = item.SplitItem(item.amount / 2);
                    var item2 = item;

                    item1.MoveToContainer(container, -1, false);
                    item2.MoveToContainer(container, -1, false);
                }

                // Start the furnace
                furnace.Invoke("StartCooking", 0);
            }
            catch (Exception ex)
            {
                Puts($"EasyFurnace encountered an exception: {ex.Message}\r\n{ex.StackTrace}");
            }
        }
    }
}