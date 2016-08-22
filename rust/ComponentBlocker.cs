using System;
using System.Text;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ComponentBlocker", "Calytic", "0.1.0", ResourceId = 1382)]
    class ComponentBlocker : RustPlugin
    {
        List<string> blockList = new List<string>();
        List<string> blockCache = new List<string>();

        bool enabled = false;

        private Dictionary<string, string> messages = new Dictionary<string, string>();

        private List<string> texts = new List<string>() {
            "You are not allowed to use this command",
            "You may not research this (restricted), blueprints refunded!",
            "You may not research this (restricted)",
            "You may not craft this (restricted)",
            "You may not deploy this (restricted)",
            "You may not pick that up (restricted)"
        };

        private bool sendMessages = true;

        void OnServerInitialized()
        {
            blockList = GetConfig<List<string>>("blockList", new List<string>());
            sendMessages = GetConfig<bool>("sendMessages", true);
            Config["blockList"] = blockList;

            Dictionary<string, object> customMessages = GetConfig<Dictionary<string, object>>("messages", null);
            if (customMessages != null)
            {
                foreach (KeyValuePair<string, object> kvp in customMessages)
                {
                    messages[kvp.Key] = kvp.Value.ToString();
                }
            }

            LoadData();

            timer.Once(1f, delegate()
            {
                enabled = true;
            });
        }

        void LoadData()
        {
            if (this.Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (this.GetConfig<string>("VERSION", this.Version.ToString()) != this.Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        void LoadDefaultConfig() 
        {
            Dictionary<string, object> messages = new Dictionary<string, object>();

            foreach (string text in texts)
            {
                if (messages.ContainsKey(text))
                {
                    PrintWarning("Duplicate translation string: " + text);
                }
                else
                {
                    messages.Add(text, text);
                }
            }

            Config["messages"] = messages;
            Config["sendMessages"] = true;
            Config["blockList"] = new List<string>();
            Config["VERSION"] = this.Version.ToString();
        }

        protected void ReloadConfig()
        {
            Dictionary<string, object> messages = new Dictionary<string, object>();

            foreach (string text in texts)
            {
                if (!messages.ContainsKey(text))
                {
                    messages.Add(text, text);
                }
            }

            Config["messages"] = messages;
            Config["VERSION"] = this.Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE

            // END NEW CONFIGURATION OPTIONS

            PrintWarning("Upgrading Configuration File");
            SaveConfig();
        }

        private void SendHelpText(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                return;
            }

            var sb = new StringBuilder()
               .Append("ComponentBlocker by <color=#ce422b>http://cyclone.network</color>\n")
               .Append("  ").Append("<color=\"#ffd479\">/blocker \"name\"</color> - Adds or removes item/entity to/from blocklist").Append("\n");
            player.ChatMessage(sb.ToString());
        }

        [ChatCommand("listinv")]
        private void cmdListInv(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                return;
            }

            List<string> prefabs = new List<string>();

            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item.info.itemMods.Length > 0)
                {
                    foreach (ItemMod itemMod in item.info.itemMods)
                    {
                        prefabs.Add(itemMod.name);
                    }
                }
            }

            player.ConsoleMessage("Inventory item prefabs:");
            player.ConsoleMessage(string.Join("\n", prefabs.ToArray()));
            SendReply(player, "Press F1 and open console");
        }

        [ChatCommand("clearblocklist")]
        private void cmdClearBlockList(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                return;
            }

            blockList.Clear();
            blockCache.Clear();
            SaveConfig();
            SendReply(player, "Blocklist cleared");
        }

        [ChatCommand("blocklist")]
        private void cmdBlockList(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                return;
            }

            string[] list = blockList.ToArray();
            player.ConsoleMessage("CURRENT BLOCKLIST:");
            player.ConsoleMessage(string.Join(", ", list));
            SendReply(player, "Press F1 and open console");
        }

        [ChatCommand("blocker")]
        private void cmdBlock(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                return;
            }

            if (args.Length == 1)
            {
                string name = args[0];
                if (blockList.Contains(name))
                {
                    blockList.Remove(name);
                    blockCache.Remove(name);
                    SaveConfig();
                    player.ChatMessage(name + " removed from block list");
                }
                else
                {
                    blockList.Add(name);
                    SaveConfig();
                    player.ChatMessage(name + " added to block list");
                }
            }
            else
            {
                player.ChatMessage("Invalid Syntax.  /blocker \"name\"");
            }
        }

        [ConsoleCommand("blocker")]
        void ccBlock(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null && arg.connection.authLevel < 1)
            {
                return;
            }

            if (arg.Args.Length == 1)
            {
                string name = arg.Args[0];
                if (blockList.Contains(name))
                {
                    blockList.Remove(name);
                    SaveConfig();
                    SendReply(arg, name + " removed from block list");
                }
                else
                {
                    blockList.Add(name);
                    SaveConfig();
                    SendReply(arg, name + " added to block list");
                }
            }
            else
            {
                SendReply(arg, "Invalid Syntax.  blocker \"name\"");
            }
        }

        void AddBlock(string name)
        {
            if (!blockList.Contains(name))
            {
                blockList.Add(name);
            }
        }

        void RemoveBlock(string name)
        {
            if (blockList.Contains(name))
            {
                blockList.Remove(name);
            }
        }

        bool IsBlocking(string name)
        {
            return blockList.Contains(name);
        }

        T GetConfig<T>(string key, T defaultValue)
        {
            try
            {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>)
                {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String))
                    {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    }
                    else if (t == typeof(int))
                    {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                }
                else if (val is Dictionary<string, object>)
                {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int))
                    {
                        var cval = new Dictionary<string, int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                    else if (t == typeof(List<object>) || t == typeof(List<string>))
                    {
                        var cval = new Dictionary<string, List<string>>();
                        foreach(var v in val as Dictionary<string, object>) {
                            if (v.Value is List<object>)
                            {
                                var clist = new List<string>();
                                foreach (object str in (List<object>)v.Value)
                                {
                                    clist.Add(str.ToString());
                                }
                                cval.Add(v.Key.ToString(), clist);
                            }
                        }
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!enabled) return;
            //this.CheckBlueprints(player);
        }

        object OnItemCraft(ItemCraftTask task)
        {
            if (!enabled) return null;
            ItemDefinition def = task.blueprint.targetItem;

            if (isBlocked(def.displayName.english, def.shortname))
            {
                task.cancelled = true;
                RefundIngredients(task.blueprint, task.owner, task.amount);
                if(sendMessages)
                    SendReply(task.owner, messages["You may not craft this (restricted)"]);

                return false;
            }

            return null;
        }

        private void RefundIngredients(ItemBlueprint bp, BasePlayer player, int amount = 1)
        {
            using (List<ItemAmount>.Enumerator enumerator = bp.ingredients.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ItemAmount current = enumerator.Current;

                    Item i = ItemManager.CreateByItemID(current.itemid, Convert.ToInt32(current.amount) * amount);
                    if (!i.MoveToContainer(player.inventory.containerMain))
                    {
                        i.Drop(player.eyes.position, player.eyes.BodyForward() * 2f);
                    }
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable networkable)
        {
            if (!enabled) return;
            if (!CheckNetworkable(networkable))
            {
                var container = networkable as LootContainer;
                if (container == null)
                    return;
                if (container.inventory == null || container.inventory.itemList == null)
                {
                    return;
                }

                foreach (Item item in container.inventory.itemList)
                {
                    CheckItem(item);
                }
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!enabled) return;
            CheckItem(item);
        }

        void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            if (!enabled) return;
            if (CheckNetworkable(entity))
            {
                if(sendMessages)
                    SendReply(deployer.GetOwnerPlayer(), messages["You may not deploy this (restricted)"]);
            }
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (!enabled) return;
            if (CheckItem(item))
            {
                if(sendMessages)
                    SendReply(player, messages["You may not pick that up (restricted)"]);
            }
        }

        //void OnBlueprintReveal(Item item, Item revealed, BasePlayer player)
        //{
            
        //    if (!enabled) return;
        //    ItemMod[] mods = item.info.itemMods;
        //    //if (CheckItem(revealed))
        //    //{
        //        // REFUND BLUEPRINT FRAGMENT/PAGE/BOOK/LIBRARY
        //        timer.Once(0.1f, delegate()
        //        {
        //            if (item.info.itemid == 1351589500)
        //            {
        //                player.inventory.GiveItem(ItemManager.Create(item.info.itemid, 20));
        //            }
        //            else
        //            {
        //                player.inventory.GiveItem(ItemManager.Create(item.info.itemid, 1));
        //            }
        //        });
                
        //        if(sendMessages)
        //            SendReply(player, messages["You may not research this (restricted), blueprints refunded!"]);
        //    //}
        //}

        void OnPlantGather(PlantEntity plant, Item item, BasePlayer player)
        {
            if (!enabled) return;
            if (CheckItem(item))
            {
                if(sendMessages)
                    SendReply(player, messages["You may not pick that up (restricted)"]);
            }
        }

        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (!enabled) return;
            CheckItem(item);
        }

        //private void CheckBlueprints(BasePlayer player)
        //{
        //    if (player.net == null)
        //    {
        //        return;
        //    }

        //    if (player.net.connection == null)
        //    {
        //        return;
        //    }

        //    if (SingletonComponent<ServerMgr>.Instance == null)
        //    {
        //        return;
        //    }

        //    if (SingletonComponent<ServerMgr>.Instance.persistance == null)
        //    {
        //        return;
        //    }

        //    bool removed = false;
        //    ProtoBuf.PersistantPlayer persistentPlayer = SingletonComponent<ServerMgr>.Instance.persistance.GetPlayerInfo(player.userID);

        //    if (persistentPlayer is ProtoBuf.PersistantPlayer)
        //    {
        //        foreach (string blocked in this.blockList)
        //        {
        //            ItemDefinition item = ItemManager.FindItemDefinition(blocked);
        //            if (item is ItemDefinition && persistentPlayer.blueprints is ProtoBuf.BlueprintList && persistentPlayer.blueprints.complete is List<int> && persistentPlayer.blueprints.complete.Contains(item.itemid))
        //            {
        //                persistentPlayer.blueprints.complete.Remove(item.itemid);
        //                removed = true;
        //            }
        //        }

        //        if (removed)
        //        {
        //            PlayerBlueprints.InitializePersistance(persistentPlayer);
        //            SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerInfo(player.userID, persistentPlayer);
        //            player.SendFullSnapshot();
        //        }
        //    }
        //}

        private bool CheckItem(Item item)
        {
            List<string> properties = new List<string>();

            if (item.info != null)
            {
                if (item.info.shortname != null)
                {
                    properties.Add(item.info.shortname);
                }

                if (item.info.displayName is Translate.Phrase && item.info.displayName.english != null)
                {
                    properties.Add(item.info.displayName.english);
                }

                properties.Add(item.info.itemid.ToString());

                if (item.info.itemMods.Length > 0)
                {
                    foreach (ItemMod itemMod in item.info.itemMods)
                    {
                        if (!properties.Contains(itemMod.name))
                        {
                            properties.Add(itemMod.name);
                            break;
                        }
                    }
                }
            }

            if (properties.Count > 0 && isBlocked(properties.ToArray()))
            {
                item.Remove(0f);
                item.RemoveFromContainer();
                return true;
            }

            return false;
        }

        private bool CheckNetworkable(BaseNetworkable networkable)
        {
            if (isBlocked(networkable.name, networkable.PrefabName, networkable.ShortPrefabName))
            {
                networkable.Kill();
                return true;
            }

            return false;
        }

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (grade == BuildingGrade.Enum.Wood && isBlocked("structure.wood"))
            {
                if (sendMessages)
                    SendReply(player, messages["You may not deploy this (restricted)"]);

                return true;
            }
            else if (grade == BuildingGrade.Enum.Stone && isBlocked("structure.stone"))
            {
                if (sendMessages)
                    SendReply(player, messages["You may not deploy this (restricted)"]);

                return true;
            }
            else if (grade == BuildingGrade.Enum.Metal && isBlocked("structure.metal"))
            {
                if (sendMessages)
                    SendReply(player, messages["You may not deploy this (restricted)"]);

                return true;
            }
            else if (grade == BuildingGrade.Enum.TopTier && isBlocked("structure.armored"))
            {
                if (sendMessages)
                    SendReply(player, messages["You may not deploy this (restricted)"]);

                return true;
            }

            return null;
        }

        private bool isBlocked(params string[] names) 
        {
            foreach (string name in names)
            {
                if (blockCache.Contains(name))
                {
                    return true;
                }
            }

            foreach (string blocked in blockList)
            {
                foreach (string name in names)
                {
                    if (name.Contains(blocked))
                    {
                        blockCache.Add(name);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
