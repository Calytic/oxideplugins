using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using MoegicBoxExtensions;

/*
    FUTURE EXPANSION:
        - Admin pays nothing for recycling box
        - Ownership tracking of boxes to protect from unwarranted unlinks
        - Have some kind of access list to tradeboxes so an NPC can manage quest-user associations (API?)
        - Support Hunt XP rewards or other types of rewards
        - Put items in crate and have them offered for sale..drag in your invent and pay up?
        - Support NPC association?
        - Have some basic logging or saved contents of the trade?
        - Let users create their own trade chests

    KNOWN ISSUES:
        - Rounding with a 100% refund on recycle can create situations where you get a tiny bit more than you should
          There is no way around this other than rounding down instead of up which creates bigger issues.
          Solution: Don't use 100% refund
          Example of this issue: Recycling a timed explosive charge down to base mats yields 75 low grade fuel
          Each craft of low grade yields 7 LGF and 75 cannot be divided by 7.
          When the player recycles the 75 low grade, the "round up" causes him to be able to craft 77 with the returned mats
          We could lower it to give only stuff for 70, but then users would complain the refund is not 100%
          We could force recycling on stacks to use the output amount and just return whatever didn't form a whole craft
          (in this case stuff for 70 LFG and return the 5 as is)
        - Cannot have price over 1 stack (very limiting!)
*/

namespace MoegicBoxExtensions
{
    static class MoegicExtensions
    {
        public static string MoegicId(this BaseEntity entity)
        {
            return $"({entity.transform.localPosition.x};{entity.transform.localPosition.y};{entity.transform.localPosition.z})";
        }
    }
}

namespace Oxide.Plugins
{
    [Info("MoegicBox", "Deicide666ra", "1.0.3")]
    class MoegicBox : RustPlugin
    {
        // config
        public class MoegicConfig
        {
            // general
            public Dictionary<string, TradeList> TradeLists = new Dictionary<string, TradeList>(); // listname, TradeList
            public Dictionary<string, string> BoxLinks = new Dictionary<string, string>(); // entityid, listname

            // recycler
            public float RefundRatio= 0.5f; // default ratio
            public Dictionary<string, int> RecyclerPrice = new Dictionary<string, int>() {  };

            public void Save()
            {
                Interface.GetMod().DataFileSystem.WriteObject("MoegicBox", this);
            }
        }
        MoegicConfig g_config= null;

        // global variables
        private Dictionary<ulong, string> g_playerCommands = new Dictionary<ulong, string>(); // steamid, activeCommand
        Dictionary<string, ItemDefinition> g_itemDefinitions = new Dictionary<string, ItemDefinition>();
        Dictionary<string, ItemBlueprint> g_blueprintDefinitions = new Dictionary<string, ItemBlueprint>();

        void CreateExampleSalesLists()
        {
            var exampleList = new TradeList("list1");
            exampleList.Offers.Add(new TradeOffer(new Product("Wood", 1000), new[] { new Product("Salvaged Axe", 1) }));
            exampleList.Offers.Add(new TradeOffer(new Product("Metal Ore", 1000), new[] { new Product("Pick Axe", 1) }));
            g_config.TradeLists.Add(exampleList.Listname, exampleList);

            exampleList = new TradeList("list2");
            exampleList.Offers.Add(new TradeOffer(new Product("Cloth", 500), new[] { new Product("Animal Fat", 250) }));
            exampleList.Offers.Add(new TradeOffer(new Product("Animal Fat", 250), new[] { new Product("Cloth", 500) }));
            g_config.TradeLists.Add(exampleList.Listname, exampleList);

            exampleList = new TradeList("list3");
            exampleList.Offers.Add(new TradeOffer(new Product("Metal Fragments", 2500), new[] { new Product("Assault Rifle", 1), new Product("5.56 Rifle Ammo", 100) }));
            g_config.TradeLists.Add(exampleList.Listname, exampleList);

        }

        void Loaded()
        {
            g_config = Interface.GetMod().DataFileSystem.ReadObject<MoegicConfig>("MoegicBox");
            if (g_config.TradeLists.Count == 0)
            {
                Puts("New configuration file created.");
                CreateExampleSalesLists();
                g_config.Save();
            }
        }

        public class TradeOffer
        {
            public TradeOffer() { } // for serialization purposes

            public TradeOffer(Product price, Product [] reward)
            {
                Price = price;
                Reward = reward;
            }

            public Product Price { get; set; }
            public Product [] Reward { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append($"Selling {Reward[0]}");

                foreach (var reward in Reward.Except(new[] { Reward[0] }))
                {
                    sb.Append(" + ");
                    sb.Append(reward);
                }

                sb.Append($" (cost: {Price})");
                return sb.ToString();
            }
        }

        public class Product
        {
            public Product() { } // for serialization purposes

            public Product(string displayName, int amount)
            {
                DisplayName = displayName;
                Amount = amount;
            }

            public string DisplayName { get; set; }
            public int Amount { get; set; }

            public override string ToString()
            {
                return $"<color=yellow>{DisplayName}</color>x<color=yellow>{Amount}</color>";
            }
        }

        public class TradeList
        {
            public TradeList() { } // for serialization purposes

            public TradeList(string name)
            {
                Listname = name;
                Offers = new List<TradeOffer>();
            }

            public List<TradeOffer> Offers { get; set; }

            public string Listname { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var offer in Offers)
                    sb.Append($"\n\t{offer.ToString()}");
                return sb.ToString();
            }
        }

        public class TradeBox
        {
            public TradeBox() { } // for serialization purposes

            int BoxEntityId { get; set; }
            ulong OwnerId { get; set; }
            string TradeListName { get; set; }
        }
        
        bool IsMoethorized(BasePlayer player)
        {
            return player.IsAdmin();
        }

        void OnServerInitialized()
        {
            g_itemDefinitions = ItemManager.itemList.ToDictionary(c => c.displayName.english.Trim(new[] { ' ', '\n', '\r' }), c => c);
            g_blueprintDefinitions = ItemManager.bpList.ToDictionary(c => c.targetItem.shortname, c => c);
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            cmdMoe(player, "help", null);
        }

        [ChatCommand("moe")]
        void cmdMoe(BasePlayer player, string cmd, string[] args)
        {
            var sb = new StringBuilder();
            sb.Append("<color=lime>MoegixBox</color> version <color=yellow>1.0.3</color> by <color=red>Deicide666ra & [HBros]Moe</color>\n");
            sb.Append("=======================================\n");
            sb.Append("\tAvailable commands:\n");
            sb.Append("<color=yellow>\t\t/mrec</color>\t\t\t\t\tstarts recycle box mode\n");
            if (IsMoethorized(player))
            {
                sb.Append("<color=yellow>\t\t/mlists</color>\t\t\t\tshows available tradelists\n");
                sb.Append("<color=yellow>\t\t/mshow xxxx</color>\tshows the contents of list named xxxx\n");
                sb.Append("<color=yellow>\t\t/mlink xxxx</color>\t\tstarts link mode for list named xxxx\n");
            }
            sb.Append("<color=yellow>\t\t/mulink</color>\t\t\t\tstarts unlink mode");

            if (cmd != "help")
            {
                sb.AppendLine();
                sb.Append(GetPriceString());
            }

            player.ChatMessage(sb.ToString());
        }

        [ChatCommand("mlists")]
        void cmdTradelists(BasePlayer player, string cmd, string[] args)
        {
            if (!IsMoethorized(player))
            {
                player.ChatMessage("Unknown command 'tradelists'!");
                return;
            }

            var msg = "<color=lime>Available trade lists:</color> <color=yellow>";
            foreach (var list in g_config.TradeLists)
                msg+= $"\n\t{list.Value.Listname}";
            msg += "</color>";
            player.ChatMessage(msg);
        }

        string FormatError(string message)
        {
            return $"<color=red>ERROR</color>: {message} - <color=orange>aborting</color>.";
        }

        [ChatCommand("mshow")]
        void cmdShowlist(BasePlayer player, string cmd, string[] args)
        {
            if (!IsMoethorized(player))
            {
                player.ChatMessage("Unknown command 'showlist'!");
                return;
            }

            // Make sure we have a listname as param otherwise show usage
            if (args.Length != 1)
            {
                player.ChatMessage(FormatError("Usage: <color=yellow>/mlist</color> <color=lime>listname</color>"));
                return;
            }

            // Get the list and show msg if it does not exist
            TradeList list= GetList(args[0]);
            if (list == null)
            {
                player.ChatMessage(FormatError($"Trade list <color=yellow>" + args[0] + "</color> not found (it's case sensitive!)"));
                return;
            }

            // Generate and show the list contents

            var sb = new StringBuilder();
            sb.Append($"Displaying trade list <color=lime>\"{list.Listname}\"</color>:");
            sb.Append(list);
            player.ChatMessage(sb.ToString());
        }
                
        [ChatCommand("mrec")]
        void cmdRecycleBox(BasePlayer player, string cmd, string[] args)
        {
            var sb = new StringBuilder();

            var currentCommand = GetActiveCommand(player);
            if (currentCommand != null)
            {
                ClearActiveCommand(player);
                sb.Append($"<color=yellow>{currentCommand}</color> mode is now <color=red>OFF</color>\n");
            }

            if (!HasPrice(player))
            {
                player.ChatMessage(FormatError(GetPriceString()));
                return;
            }

            SetActiveCommand(player, $"recyclebox");

            sb.Append($"<color=yellow>recyclebox</color> mode is now <color=lime>ON</color>\nOpen the container you wish to toggle recycling box mode for");
            player.ChatMessage(sb.ToString());
        }


        [ChatCommand("mlink")]
        void cmdLinkBox(BasePlayer player, string cmd, string[] args)
        {
            if (!IsMoethorized(player))
            {
                player.ChatMessage("Unknown command 'linkbox'!");
                return;
            }

            var sb = new StringBuilder();

            var currentCommand = GetActiveCommand(player);
            if (currentCommand != null)
            {
                ClearActiveCommand(player);
                sb.Append($"<color=yellow>{currentCommand}</color> mode is now <color=red>OFF</color>\n");
            }

            if (args.Length != 1)
            {
                sb.Append(FormatError("Usage: <color=yellow>/mlink</color> <color=lime>listname</color>"));
                player.ChatMessage(sb.ToString());
                return;
            }

            TradeList list= GetList(args[0]);
            if (list == null)
            {
                sb.Append(FormatError("Trade list <color=yellow>" + args[0] + "</color> not found (check case)"));
                player.ChatMessage(sb.ToString());
                return;
            }

            SetActiveCommand(player, $"linkbox {args[0]}");

            sb.Append($"<color=yellow>linkbox</color> mode is now <color=lime>ON</color>\nOpen the container you wish to associate with <color=yellow>{args[0]}</color>");
            player.ChatMessage(sb.ToString());
        }

        [ChatCommand("mulink")]
        void cmdUnlinkBox(BasePlayer player, string cmd, string[] args)
        {
            var sb = new StringBuilder();

            var currentCommand = GetActiveCommand(player);
            if (currentCommand != null)
            {
                ClearActiveCommand(player);
                sb.Append($"<color=yellow>{currentCommand}</color> mode is now <color=red>OFF</color>\n");
            }

            SetActiveCommand(player, $"unlinkbox");

            sb.Append($"<color=yellow>unlinkbox</color> mode is now <color=lime>ON</color>\nOpen the container you wish to unlink");
            player.ChatMessage(sb.ToString());
        }

        void OnPlayerLoot(PlayerLoot lootInventory, object lootable)
        {
            var looter = lootInventory.GetComponent<BasePlayer>();
            if (looter == null) return;

            BaseEntity container = lootable as BaseEntity;
            if (container == null) return;
            if (container.LookupShortPrefabName() != "box.wooden.large.prefab")
                return;

            string command = GetActiveCommand(looter);
            if (command == null)
            {
                var currentTradeList = GetActiveList(container);

                if (currentTradeList == "recycling")
                {
                    looter.ChatMessage("You found a <color=yellow>Moegic Recycling Box!</color>!");
                }
                else if (currentTradeList != null)
                {
                    var list = GetList(currentTradeList);
                    looter.ChatMessage("You found a <color=yellow>Moegic Tradebox</color>!" + list.ToString());

                }
                return;
            }
            
            var sb = new StringBuilder();
            ClearActiveCommand(looter);
            sb.Append($"<color=yellow>{command}</color> mode is now <color=red>OFF</color>");
            looter.ChatMessage(sb.ToString());

            RunCommand(looter, command, container);
        }

        void RunCommand(BasePlayer player, string command, BaseEntity box)
        {
            var currentTradeList = GetActiveList(box);

            //*********************************************
            // RECYCLEBOX
            //*********************************************
            if (command.StartsWith("recyclebox"))
            {
                // Make sure the box is not already associated with a list
                if (currentTradeList != null)
                {
                    player.ChatMessage($"This box is already associated with <color=yellow>{currentTradeList}</color>!");
                    return;
                }
                else
                {
                    // Check if we have the price and if so, remove
                    if (!HasPrice(player))
                    {
                        player.ChatMessage(FormatError(GetPriceString()));
                        return;
                    }
                    foreach (var price in g_config.RecyclerPrice)
                    {
                        var removed = RemoveItemsFromInventory(player, price.Key, price.Value);
                        if (removed != price.Value)
                        {
                            Puts($"Oh snap! Tried to remove {price.Value} X {price.Key} but only {removed} were found --- this should not happen, contact the author");
                        }
                    }

                    // Link the box
                    g_config.BoxLinks.Add(box.MoegicId(), "recycling");
                    g_config.Save();

                    // Report success
                    player.ChatMessage($"Box is now a <color=yellow>Moegic Recycling Box</color>!");
                    return;
                }
            }


            //*********************************************
            // LINKBOX
            //*********************************************
            if (command.StartsWith("linkbox"))
            {
                // Make sure the box is not already associated with a list
                if (currentTradeList != null)
                {
                    player.ChatMessage(FormatError("This box is already associated with <color=lime>" + currentTradeList + "</color>, use <color=yellow>/unlinkbox</color> first"));
                    return;
                }

                // Make sure the new list we're trying to assign actually exists
                var listName = command.Split(new[] { ' ' }).Last();
                TradeList list = GetList(listName);
                if (list == null)
                {
                    player.ChatMessage(FormatError("Trade list <color=yellow>" + listName + "</color> not found (check case)"));
                    return;
                }

                // Link the box
                g_config.BoxLinks.Add(box.MoegicId(), listName);
                g_config.Save();

                // Report success
                player.ChatMessage($"Box associated with <color=yellow>{listName}</color> successfully!");
                return;
            }

            //*********************************************
            // UNLINKBOX
            //*********************************************
            if (command.StartsWith("unlinkbox"))
            {
                // Abort if no list is currently associated with the box
                if (currentTradeList == null)
                {
                    player.ChatMessage(FormatError("This box is not linked with any list!"));
                    return;
                }

                if (currentTradeList != "recycling" && !IsMoethorized(player))
                {
                    player.ChatMessage(FormatError("You do not have the required access to unlink this box"));
                    return;
                }

                // Unlink the box
                g_config.BoxLinks.Remove(box.MoegicId());
                g_config.Save();

                // Report success
                player.ChatMessage($"Box successfully unlinked from <color=yellow>{currentTradeList}</color>");
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

        private TradeList GetList(string listName)
        {
            TradeList list;
            if (!g_config.TradeLists.TryGetValue(listName, out list)) return null;
            return list;
        }

        private string GetActiveList(BaseEntity box)
        {
            string listName = null;
            g_config.BoxLinks.TryGetValue(box.MoegicId(), out listName);
            return listName;
        }

        private bool SetActiveList(BaseEntity box, string list)
        {
            string existingList = GetActiveList(box);
            if (existingList != null) return false;
            g_config.BoxLinks.Add(box.MoegicId(), list);
            return true;
        }

        private static BasePlayer GetPlayerFromContainer(ItemContainer container, Item item) => item.GetOwnerPlayer() ?? 
                            BasePlayer.activePlayerList.FirstOrDefault(p => p.inventory.loot.IsLooting() && p.inventory.loot.entitySource == container.entityOwner);


        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            BaseEntity box = container.entityOwner;
            if (box == null) return;

            if (box.LookupShortPrefabName() != "box.wooden.large.prefab") return;

            string listname = GetActiveList(box);
            if (listname == null) return;
            
            // Try to identify the looter based on stored info (ugly hack)
            BasePlayer player= GetPlayerFromContainer(container, item);
            if (player == null)
            {
                Puts($"Looter cannot be identified");
                return;
            }

            if (listname == "recycling")
            {
                if (item.hasCondition && item.condition == 0)
                {
                    player.ChatMessage(FormatError("This item is totally broken, nothing would be recycled out of it, try repairing it first!"));
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
            else // normal tradelist
            {
                var list = GetList(listname);
                var matchingOffers = list.Offers.Where(o => o.Price.DisplayName == item.info.displayName.english && o.Price.Amount <= item.amount).OrderBy(o => o.Price.Amount);
                if (!matchingOffers.Any())
                {
                    player.ChatMessage($"This Moegic Tradebox does not have anything for sale that is worth {item.amount}X {item.info.displayName.translated}");
                    player.GiveItem(item);
                    return;
                }

                var pricePaid = item.amount;
                var price = matchingOffers.First().Price.Amount;

                // give player his reward(s)
                while (pricePaid >= price)
                {
                    pricePaid -= price;
                    foreach (var r in matchingOffers.First().Reward)
                    {
                        var reward = ItemManager.Create(g_itemDefinitions[r.DisplayName], r.Amount);
                        player.GiveItem(reward);
                    }
                }

                if (pricePaid > 0)
                {
                    var refund = ItemManager.Create(g_itemDefinitions[item.info.displayName.english], pricePaid);
                    player.GiveItem(refund);
                }

                item.RemoveFromContainer();
            }
        }

        //void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        //{
        //    if (g_config.BoxLinks.Any(bl => bl.Key == entity.MoegicId() && bl.Value != "recycling"))
        //    {
        //        hitInfo.damageTypes.ScaleAll(0);

        //        var player = hitInfo?.Initiator?.ToPlayer();
        //        if (player != null)
        //        {
        //            player.ChatMessage(FormatError("This is a <color=yellow>Moegic Tradebox</color> and it is immune to all forms of damage."));
        //        }
        //    }
        //}

        bool CheckPlayerInventoryForItems(BasePlayer player, string displayName, int amount)
        {
            ItemDefinition itemToCheck = null;
            var ret = g_itemDefinitions.TryGetValue(displayName, out itemToCheck);
            if (ret == false) return false;
            var foundItems = player.inventory.FindItemIDs(itemToCheck.itemid);
            if (foundItems == null || !foundItems.Any()) return false;
            var nbFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (nbFound < amount) return false;
            return true;
        }

        int RemoveItemsFromInventory(BasePlayer player, string displayName, int amount)
        {
            ItemDefinition itemToRemove = null;
            var ret = g_itemDefinitions.TryGetValue(displayName, out itemToRemove);
            var foundItems = player.inventory.FindItemIDs(itemToRemove.itemid);
            var nbFound = foundItems == null ? 0 : foundItems.Sum(item => item.amount);
            if (nbFound < amount) amount = nbFound;
            var nbRemoved = player.inventory.Take(foundItems, itemToRemove.itemid, amount);
            return nbRemoved;
        }

        void SalvageItem(BasePlayer player, Item item)
        {
            var sb = new StringBuilder();

            ItemBlueprint bp = null;
            if (g_blueprintDefinitions.TryGetValue(item.info.shortname, out bp) == false)
                return;

            var ratio = item.hasCondition ? (item.condition / item.maxCondition) : 1;

            sb.Append($"Recycling <color=lime>{item.info.displayName.english}</color> to {(int)(g_config.RefundRatio * 100.0)}% base materials:");

            // WARNING: This opens up a LOT of exploiting, use with caution
            // Example: Players using guitars or bullets to get quasi-unlimited bp frags
            //if (item.IsBlueprint())
            //{
            //    var framents = 100;
            //    switch (item.info.rarity)
            //    {
            //        case Rust.Rarity.Common:
            //            framents = (int)Math.Floor(100.0 * g_config.RefundRatio);
            //            break;
            //        case Rust.Rarity.Uncommon:
            //            framents = (int)Math.Floor(250.0 * g_config.RefundRatio);
            //            break;
            //        case Rust.Rarity.Rare:
            //            framents = (int)Math.Floor(500.0 * g_config.RefundRatio);
            //            break;
            //        case Rust.Rarity.VeryRare:
            //            framents = (int)Math.Floor(1000.0 * g_config.RefundRatio);
            //            break;
            //        default:
            //            return;
            //    }

            //    var newItem = ItemManager.Create(g_itemDefinitions["Blueprint Fragment"], framents);
            //    player.GiveItem(newItem);
            //    sb.AppendLine();
            //    sb.Append($"    <color=lime>{newItem.info.displayName.english}</color> X<color=yellow>{framents}</color>");
            //}
            //else
            {
                foreach (var ingredient in bp.ingredients)
                {
                    var refundAmount = (double)ingredient.amount / bp.amountToCreate;
                    refundAmount *= item.amount;
                    refundAmount *= ratio;
                    refundAmount *= g_config.RefundRatio;
                    refundAmount = Math.Ceiling(refundAmount);
                    if (refundAmount < 1) refundAmount = 1;

                    var newItem = ItemManager.Create(g_itemDefinitions[ingredient.itemDef.displayName.english], (int)refundAmount);

                    ItemBlueprint ingredientBp = null;
                    if (g_blueprintDefinitions.TryGetValue(ingredient.itemDef.shortname, out ingredientBp) == true)
                        if (item.hasCondition) newItem.condition = (float)Math.Ceiling(newItem.maxCondition * ratio);

                    player.GiveItem(newItem);
                    sb.AppendLine();
                    sb.Append($"    <color=lime>{newItem.info.displayName.english}</color> X<color=yellow>{newItem.amount}</color>");
                }
            }

            player.ChatMessage(sb.ToString());
        }

        string GetPriceString()
        {
            var sb = new StringBuilder();
            sb.Append("\tThe price to setup a <color=yellow>Recycling Moegic Box</color> is:");
            foreach (var price in g_config.RecyclerPrice)
            {
                sb.Append($"\n\t\t{price.Value} {price.Key}");
            }

            return sb.ToString();
        }

        bool HasPrice(BasePlayer player)
        {
            foreach (var price in g_config.RecyclerPrice)
            {
                if (!CheckPlayerInventoryForItems(player, price.Key, price.Value))
                    return false;
            }
            return true;
        }
    }
}