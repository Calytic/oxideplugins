using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Salvager", "Deicide666ra", "1.0.2")]
    class Salvager : RustPlugin
    {
        // Configuration
        Dictionary<string, string> c_salvagers= new Dictionary<string, string>(); // uniqueId, steamId
        string c_price; // default: "1000 wood + 5000 metal.fragments"
        double c_refundRatio; // default: 0.5

        // Global variables
        Dictionary<ulong, string> g_playerCommands = new Dictionary<ulong, string>(); // steamid, activeCommand
        Dictionary<string, ItemDefinition> g_itemDefinitions = new Dictionary<string, ItemDefinition>();
        Dictionary<string, ItemBlueprint> g_blueprintDefinitions = new Dictionary<string, ItemBlueprint>();
        Dictionary<string, BasePlayer> g_lootCache = new Dictionary<string, BasePlayer>();
        Dictionary<string, int> g_priceDict = new Dictionary<string, int>();
        bool g_configChanged;

        void Loaded() => LoadConfigValues();

        protected override void LoadDefaultConfig()
        {
            Puts("New configuration file created.");
        }

        void LoadConfigValues()
        {
            // Refund ratio
            c_refundRatio= Convert.ToDouble(GetConfigValue("Salvager", "Refund Ratio", 0.5));

            // Price
            c_price = Convert.ToString(GetConfigValue("Salvager", "Price", "1000 wood + 5000 metal.fragments"));
            g_priceDict.Clear();
            var pricePieces = c_price.Split('+');
            foreach (var piece in pricePieces)
            {
                var parts = piece.Trim(' ').Split(' ');
                g_priceDict.Add(parts.Last(), Convert.ToInt32(parts.First()));
            }
            //foreach (var element in g_priceDict)
            //{
            //    Puts($"{element.Key} X{element.Value}");
            //}

            // Salvagers vs owners
            var mess= GetConfigValue("Salvager", "Salvagers", c_salvagers);
            foreach (var entry in (IEnumerable)mess)
            {
                var parts = entry.ToString().Trim(new[] { '[', ']' }).Split(',');
                c_salvagers.Add(parts.First(), parts.Last());
            }
            //foreach (var entry in c_salvagers)
            //{
            //    Puts($"key: {entry.Key} value: {entry.Value}");
            //}

            if (g_configChanged)
            {
                Puts("Configuration file updated.");
                SaveConfig();
            }
        }

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                g_configChanged = true;
            }

            if (data.TryGetValue(setting, out value)) return value;
            value = defaultValue;
            data[setting] = value;
            g_configChanged = true;
            return value;
        }

        void SetConfigValue(string category, string setting, object newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                g_configChanged = true;
            }
            SaveConfig();
        }

        void OnServerInitialized()
        {
            g_itemDefinitions= ItemManager.itemList.ToDictionary(c => c.shortname, c => c);
            g_blueprintDefinitions = ItemManager.bpList.ToDictionary(c => c.targetItem.shortname, c => c);
        }

        [ChatCommand("salvager")]
        void cmdSalvager(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length != 1)
            {
                var sb = new StringBuilder();
                sb.Append("<color=lime>Salvager</color> version <color=yellow>1.0.2</color> by <color=red>Deicide666ra</color>\n");
                sb.Append("Original idea by <color=red>[HBros]Moe</color>\n");
                sb.Append("=======================================\n");
                sb.Append("\tAvailable commands:\n");
                sb.Append("\t\t<color=yellow>/salvager buy</color>\t\t\tenables buy mode\n");
                sb.Append("\t\t<color=yellow>/salvager remove</color>\tstarts remove mode\n");
                sb.Append(GetPriceString());
                player.ChatMessage(sb.ToString());
                return;
            }

            var arg = args[0].ToLower();

            if (arg == "buy")
            {
                if (!HasPrice(player))
                {
                    player.ChatMessage(FormatError(GetPriceString()));
                    return;
                }

                LinkContainer(player);
                return;
            }

            if (arg == "remove")
            {
                UnlinkContainer(player);
                return;
            }
        }

        void LinkContainer(BasePlayer player)
        {
            var sb = new StringBuilder();

            var currentCommand = GetActiveCommand(player);
            if (currentCommand != null)
            {
                ClearActiveCommand(player);
                sb.Append($"<color=yellow>{currentCommand}</color> mode is now <color=red>OFF</color>\n");
            }

            SetActiveCommand(player, "buy");

            sb.Append($"<color=yellow>buy</color> mode is now <color=lime>ON</color>\nOpen the <color=yellow>Repair Bench</color> you wish to convert to a <color=yellow>Salvaging Bench</color>");
            player.ChatMessage(sb.ToString());
        }

        void UnlinkContainer(BasePlayer player)
        {
            var sb = new StringBuilder();

            var currentCommand = GetActiveCommand(player);
            if (currentCommand != null)
            {
                ClearActiveCommand(player);
                sb.Append($"<color=yellow>{currentCommand}</color> mode is now <color=red>OFF</color>\n");
            }

            SetActiveCommand(player, "remove");

            sb.Append($"<color=yellow>remove</color> mode is now <color=lime>ON</color>\nOpen the <color=yellow>Salvaging Bench</color> you wish to convert back to a <color=yellow>Repair Bench</color>");
            player.ChatMessage(sb.ToString());
        }

        void OnPlayerLoot(PlayerLoot lootInventory, object lootable)
        {
            var looter = lootInventory.GetComponent("BasePlayer") as BasePlayer;
            if (looter == null) return;

            BaseEntity container = lootable as BaseEntity;
            if (container == null) return;
            if (container.LookupShortPrefabName() != "repairbench_deployed.prefab") return;

            string command = GetActiveCommand(looter);
            if (command == null)
            {
                ulong owner = GetSalvagerOwner(container);
                if (owner != 0)
                {
                    looter.ChatMessage("You found a <color=yellow>Salvaging Bench!</color>!");
                }
                g_lootCache[UniqueId(container)] = looter;
                return;
            }

            var sb = new StringBuilder();
            ClearActiveCommand(looter);
            sb.Append($"<color=yellow>{command}</color> mode is now <color=red>OFF</color>");
            looter.ChatMessage(sb.ToString());

            RunCommand(looter, command, container);
        }

        bool CheckPlayerInventoryForItems(BasePlayer player, string shortname, int amount)
        {
            ItemDefinition itemToCheck = null;
            var ret = g_itemDefinitions.TryGetValue(shortname, out itemToCheck);
            if (ret == false) return false;
            var foundItems = player.inventory.FindItemIDs(itemToCheck.itemid);
            if (foundItems == null || !foundItems.Any()) return false;
            var nbFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (nbFound < amount) return false;
            return true;
        }

        int RemoveItemsFromInventory(BasePlayer player, string shortname, int amount)
        {
            ItemDefinition itemToRemove = null;
            var ret = g_itemDefinitions.TryGetValue(shortname, out itemToRemove);
            if (ret == false) return 0;
            var foundItems = player.inventory.FindItemIDs(itemToRemove.itemid);
            if (foundItems == null || !foundItems.Any()) return 0;
            var nbFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (nbFound < amount) amount = nbFound;
            var nbRemoved = player.inventory.Take(foundItems, itemToRemove.itemid, amount);
            return nbRemoved;
        }

        string GetPriceString()
        {
            var sb = new StringBuilder();
            sb.Append("The price to setup a <color=yellow>Salvaging Bench is</color>:\n");
            foreach (var price in g_priceDict)
            {
                sb.Append($"\t\t{price.Key} X {price.Value}\n");
            }

            return sb.ToString();
        }

        bool HasPrice(BasePlayer player )
        {
            foreach (var price in g_priceDict)
            {
                if (!CheckPlayerInventoryForItems(player, price.Key, price.Value))
                    return false;
            }
            return true;
        }

        void RunCommand(BasePlayer player, string command, BaseEntity container)
        {
            var owner = GetSalvagerOwner(container);

            //*********************************************
            // LINKBOX
            //*********************************************
            if (command.StartsWith("buy"))
            {
                // Make sure the bench is not already associated with a list
                if (owner != 0)
                {
                    player.ChatMessage(FormatError("This <color=yellow>Repair Bench</color> is already a <color=yellow>Salvaging Bench</color>, use <color=yellow>/salvager remove</color> to cancel (must be owner)"));
                    return;
                }

                // Check if we have the price and if so, remove
                if (!HasPrice(player)) 
                {
                    player.ChatMessage(FormatError(GetPriceString()));
                    return;
                }
                foreach (var price in g_priceDict)
                {
                    var removed = RemoveItemsFromInventory(player, price.Key, price.Value);
                    if (removed != price.Value)
                    {
                        Puts($"Oh snap! Tried to remove {price.Value} X {price.Key} but only {removed} were found --- this should not happen, contact the author");
                    }
                }
                    

                // Link the box
                c_salvagers.Add(UniqueId(container), player.userID.ToString());

                // Save the config
                SetConfigValue("Salvager", "Salvagers", c_salvagers);

                // Report success
                player.ChatMessage($"<color=yellow>Repair Bench</color> is now a <color=yellow>Salvaging Bench</color>!");
                return;
            }

            //*********************************************
            // UNLINKBOX
            //*********************************************
            if (command.StartsWith("remove"))
            {
                // Abort if no list is currently associated with the box
                if (owner == 0)
                {
                    player.ChatMessage(FormatError("This <color=yellow>Repair Bench</color> is not a <color=yellow>Salvaging Bench!</color>"));
                    return;
                }

                // Unlink the box
                c_salvagers.Remove(UniqueId(container));

                // Save the config
                SetConfigValue("Salvager", "Salvagers", c_salvagers);

                // Report success
                player.ChatMessage($"<color=yellow>Salvaging Bench</color> successfully reverted to a <color=yellow>Repair Bench</color>");
                return;
            }
        }

        private string GetActiveCommand(BasePlayer player)
        {
            string command = null;
            g_playerCommands.TryGetValue(player.userID, out command);
            return command;
        }

        private void SetActiveCommand(BasePlayer player, string command)
        {
            string oldCommand = GetActiveCommand(player);
            if (oldCommand != null) ClearActiveCommand(player);
            g_playerCommands.Add(player.userID, command);
        }

        private void ClearActiveCommand(BasePlayer player)
        {
            if (GetActiveCommand(player) != null)
                g_playerCommands.Remove(player.userID);
        }

        private ulong GetSalvagerOwner(BaseEntity container)
        {
            string steamId = "0";
            c_salvagers.TryGetValue(UniqueId(container), out steamId);
            return Convert.ToUInt64(steamId);
        }

        private bool CreateSalvager(BaseEntity container, ulong steamIdOwner)
        {
            var currentOwner = GetSalvagerOwner(container);
            if (currentOwner != 0)
            {
                // Error already salvager
                return false;
            }
            
            c_salvagers.Add(UniqueId(container), steamIdOwner.ToString());
            return true;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            StorageContainer entity = container.entityOwner as StorageContainer;
            if (entity == null) return;

            if (entity.LookupShortPrefabName() != "repairbench_deployed.prefab") return;

            if (GetSalvagerOwner(entity) == 0) return;
            
            // Try to identify the looter based on stored info (ugly hack)
            BasePlayer player;
            if (g_lootCache.TryGetValue(UniqueId(entity), out player) == false || player == null)
            {
                //Puts($"Looter cannot be identified");
                return;
            }

            if (item.hasCondition && item.condition == 0)
            {
                player.ChatMessage(FormatError("This item is totally broken, nothing would be salvageable, try repairing it first!"));
                return;
            }

            ItemBlueprint bp = null;
            if (g_blueprintDefinitions.TryGetValue(item.info.shortname, out bp) == false)
            {
                player.ChatMessage(FormatError($"This item is not recyclable: {item.info.displayName.english}"));
                player.GiveItem(item);
                return;
            }

            item.RemoveFromContainer();
            SalvageItem(player, item);

            player.ChatMessage($"Successfully recycled <color=yellow>{item.info.displayName.english}</color> to base materials!");
        }

        void SalvageItem(BasePlayer player, Item item)
        {
            ItemBlueprint bp = null;
            if (g_blueprintDefinitions.TryGetValue(item.info.shortname, out bp) == false)
                return;

            var ratio = item.hasCondition ? (item.condition / item.maxCondition) : 1;

            foreach (var ingredient in bp.ingredients)
            {
                var refundAmount = (double)ingredient.amount / bp.amountToCreate;
                refundAmount *= item.amount;
                refundAmount *= ratio;
                refundAmount *= c_refundRatio;
                refundAmount = Math.Ceiling(refundAmount);
                if (refundAmount < 1) refundAmount = 1;
                
                ItemBlueprint ingredientBp = null;
                if (g_blueprintDefinitions.TryGetValue(ingredient.itemDef.shortname, out ingredientBp) == true)
                {
                    // Cascade down other salvageable items
                    var subItem= ItemManager.CreateByName(ingredient.itemDef.shortname, (int)refundAmount);
                    if (subItem.hasCondition && item.hasCondition) subItem.condition = item.condition;
                    SalvageItem(player, subItem);
                }
                else
                {
                    var newItem = ItemManager.Create(g_itemDefinitions[ingredient.itemDef.shortname], (int)refundAmount);
                    player.GiveItem(newItem);
                }
            }
        }

        string UniqueId(BaseEntity entity)
        {
            return $"({entity.transform.localPosition.x};{entity.transform.localPosition.y};{entity.transform.localPosition.z})";
        }

        string FormatError(string message)
        {
            return $"<color=red>ERROR</color>: {message} - <color=orange>aborting</color>.";
        }
    }
}