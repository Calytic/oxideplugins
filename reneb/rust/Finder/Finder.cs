using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Finder", "Reneb", "2.0.6", ResourceId = 692)]
    class Finder : RustPlugin
    {
        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Plugin References
        //////////////////////////////////////////////////////////////////////////////////////////
        [PluginReference]
        Plugin DeadPlayersList;
        //////////////////////////////////////////////////////////////////////////////////////////
        ///// cached Fields
        //////////////////////////////////////////////////////////////////////////////////////////

        Dictionary<BasePlayer, Dictionary<string, Dictionary<string, object>>> cachedFind = new Dictionary<BasePlayer, Dictionary<string, Dictionary<string, object>>>();

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Fields
        //////////////////////////////////////////////////////////////////////////////////////////
        FieldInfo lastPositionValue;

        //////////////////////////////////////////////////////////////////////////////////////////
        ///// Configuration
        //////////////////////////////////////////////////////////////////////////////////////////

        static string noAccess = "You are not allowed to use this command";

        private bool Changed;

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Messages: noAccess", ref noAccess);

            SaveConfig();

        }

        void LoadDefaultConfig() { }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Oxide Hooks
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            lastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void OnServerInitialized()
        {
            InitializeTable();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Player Finder
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        object FindPlayer(string arg)
        {
            var findplayers = FindPlayers(arg);
            if (findplayers.Count == 0)
            {
                return "No players found";
            }
            if (findplayers.Count == 1)
            {
                foreach (KeyValuePair<string, Dictionary<string, object>> pair in findplayers)
                {
                    return pair.Value;
                }
            }
            return findplayers;
        }

        Dictionary<string, Dictionary<string, object>> FindPlayers(string arg)
        {
            var listPlayers = new Dictionary<string, Dictionary<string, object>>();

            ulong steamid = 0L;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();
            foreach (BasePlayer player in Resources.FindObjectsOfTypeAll<BasePlayer>())
            {
                if (steamid != 0L)
                    if (player.userID == steamid)
                    {
                        listPlayers.Clear();
                        listPlayers.Add(steamid.ToString(), GetFinderDataFromPlayer(player));
                        return listPlayers;
                    }
                if (player.displayName == null) continue;
                string lowername = player.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    listPlayers.Add(player.userID.ToString(), GetFinderDataFromPlayer(player));
                }
            }
            if (DeadPlayersList != null)
            {
                var deadplayers = DeadPlayersList.Call("GetPlayerList") as Dictionary<string, string>;
                foreach (KeyValuePair<string, string> pair in deadplayers)
                {
                    if (steamid != 0L)
                        if (pair.Key == arg)
                        {
                            listPlayers.Clear();
                            listPlayers.Add(pair.Key.ToString(), GetFinderDataFromDeadPlayers(pair.Key));
                            return listPlayers;
                        }
                    string lowername = pair.Value.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        listPlayers.Add(pair.Key.ToString(), GetFinderDataFromDeadPlayers(pair.Key));
                    }
                }
            }

            return listPlayers;
        }
        Dictionary<string, object> GetFinderDataFromDeadPlayers(string userid)
        {
            var playerData = new Dictionary<string, object>();

            playerData.Add("userid", userid);
            playerData.Add("name", (string)DeadPlayersList.Call("GetPlayerName", userid));
            playerData.Add("pos", (Vector3)DeadPlayersList.Call("GetPlayerDeathPosition", userid));
            playerData.Add("state", "Dead");
            playerData.Add("status", "Disconnected");

            return playerData;
        }
        Dictionary<string, object> GetFinderDataFromPlayer(BasePlayer player)
        {
            var playerData = new Dictionary<string, object>();

            playerData.Add("userid", player.userID.ToString());
            playerData.Add("name", player.displayName);
            playerData.Add("pos", player.transform.position);
            playerData.Add("state", player.IsDead() ? "Dead" : player.IsSleeping() ? "Sleeping" : player.IsSpectating() ? "Spectating" : "Alive");
            playerData.Add("status", player.IsConnected() ? "Connected" : "Disconnected");

            return playerData;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Teleport Functions & Data
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void ResetFind(BasePlayer player)
        {
            if (cachedFind.ContainsKey(player))
                cachedFind.Remove(player);
            cachedFind.Add(player, new Dictionary<string, Dictionary<string, object>>());
        }
        void AddFind(BasePlayer player, int count, Dictionary<string, object> data)
        {
            cachedFind[player].Add(count.ToString(), data);
        }
        object GetFind(BasePlayer player, string count)
        {
            if (!cachedFind.ContainsKey(player)) return "You didn't search for something yet";
            if (cachedFind[player].Count == 0) return "You didn't find anything";
            if (!cachedFind[player].ContainsKey(count)) return "This FindID is not valid";
            return cachedFind[player][count];
        }
        void ForcePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.MovePosition(destination);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Building Privilege Search
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Dictionary<string, object> GetBuildingPrivilegeData(BuildingPrivlidge bp)
        {
            var bpdata = new Dictionary<string, object>();

            bpdata.Add("pos", bp.transform.position);

            return bpdata;
        }
        List<Dictionary<string, object>> FindPrivileges(string userid)
        {
            var privileges = new List<Dictionary<string, object>>();
            ulong ulongid = ulong.Parse(userid);
            foreach (BuildingPrivlidge bp in Resources.FindObjectsOfTypeAll<BuildingPrivlidge>())
            {
                foreach (ProtoBuf.PlayerNameID pni in bp.authorizedPlayers)
                {
                    if (pni.userid == ulongid)
                    {
                        privileges.Add(GetBuildingPrivilegeData(bp));
                    }
                }
            }

            return privileges;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Sleeping Bag Search
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Dictionary<string, object> GetSleepingBagData(SleepingBag bag)
        {
            var bagdata = new Dictionary<string, object>();

            bagdata.Add("name", bag.niceName);
            bagdata.Add("pos", bag.transform.position);

            return bagdata;
        }
        List<Dictionary<string, object>> FindSleepingBags(string userid)
        {
            var bags = new List<Dictionary<string, object>>();
            ulong ulongid = ulong.Parse(userid);
            foreach (SleepingBag bag in SleepingBag.FindForPlayer(ulongid, true))
            {
                bags.Add(GetSleepingBagData(bag));
            }

            return bags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Item Search
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();

        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
        }

        List<Dictionary<string, object>> FindItems(ItemDefinition itemdef, int minamount)
        {
            var containers = new List<Dictionary<string, object>>();
            foreach (StorageContainer sc in Resources.FindObjectsOfTypeAll<StorageContainer>())
            {
                ItemContainer inventory = sc.inventory;
                if (inventory == null) continue;
                int amount = inventory.GetAmount(itemdef.itemid, false);
                if (amount < minamount) continue;
                Dictionary<string, object> scdata = new Dictionary<string, object>();
                scdata.Add("in", "Storage Box");
                scdata.Add("pos", sc.transform.position);
                scdata.Add("amount", amount.ToString());
                containers.Add(scdata);
            }

            foreach (BasePlayer player in Resources.FindObjectsOfTypeAll<BasePlayer>())
            {
                PlayerInventory inventory = player.inventory;
                if (inventory == null) continue;
                int amount = inventory.GetAmount(itemdef.itemid);
                if (amount < minamount) continue;
                Dictionary<string, object> scdata = new Dictionary<string, object>();
                scdata.Add("in", string.Format("{0} {1}", player.userID.ToString(), player.displayName));
                scdata.Add("pos", player.transform.position);
                scdata.Add("amount", amount.ToString());
                containers.Add(scdata);
            }
            return containers;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Chat Command
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("find")]
        void cmdChatFind(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, noAccess);
                return;
            }
            if (args.Length < 2 || args == null)
            {
                SendReply(player, "======== FINDER =======");
                SendReply(player, "/find player NAME/SteamID");
                SendReply(player, "/find bag NAME/SteamID");
                SendReply(player, "/find privilege NAME/SteamID");
                SendReply(player, "/find tp FINDID");
                return;
            }
            int count = 0;
            switch (args[0].ToLower())
            {
                case "players":
                case "player":
                    ResetFind(player);
                    Dictionary<string, Dictionary<string, object>> success = FindPlayers(args[1]);
                    if (success.Count == 0)
                    {
                        SendReply(player, string.Format("No players found with: {0}", args[1]));
                        return;
                    }

                    foreach (KeyValuePair<string, Dictionary<string, object>> pair in success)
                    {
                        AddFind(player, count, pair.Value);
                        SendReply(player, string.Format("{0} - {1} - {2} - {3} - {4}", count.ToString(), pair.Key, (string)pair.Value["name"], (string)pair.Value["state"], (string)pair.Value["status"]));
                        count++;
                    }
                    break;
                case "info":
                    object finddatarr = GetFind(player, args[1]);
                    if (finddatarr is string)
                    {
                        SendReply(player, (string)finddatarr);
                        return;
                    }
                    var findDatar = finddatarr as Dictionary<string, object>;
                    foreach (KeyValuePair<string, object> pair in findDatar)
                    {
                        SendReply(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
                    }
                    break;
                case "tp":
                    object finddatar = GetFind(player, args[1]);
                    if (finddatar is string)
                    {
                        SendReply(player, (string)finddatar);
                        return;
                    }
                    var findData = finddatar as Dictionary<string, object>;
                    if (!findData.ContainsKey("pos"))
                    {
                        SendReply(player, "Couldn't find the position for this data");
                        return;
                    }
                    ForcePlayerPosition(player, (Vector3)findData["pos"]);
                    foreach (KeyValuePair<string, object> pair in findData)
                    {
                        SendReply(player, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
                    }
                    break;
                case "item":
                    if (args.Length < 3)
                    {
                        SendReply(player, "/find item ITEMNAME MINNUMBER");
                        return;
                    }
                    int minnum = 0;
                    if (!int.TryParse(args[2], out minnum))
                    {
                        SendReply(player, "/find item ITEMNAME MINNUMBER");
                        return;
                    }
                    ResetFind(player);
                    string itemname = args[1].ToLower();
                    if (displaynameToShortname.ContainsKey(itemname))
                        itemname = displaynameToShortname[itemname];
                    ItemDefinition definition = ItemManager.FindItemDefinition(itemname);
                    if (definition == null)
                    {
                        SendReply(player, "This item doesn't exist");
                        return;
                    }
                    List<Dictionary<string, object>> containers = FindItems(definition, minnum);
                    foreach (Dictionary<string, object> container in containers)
                    {
                        AddFind(player, count, container);
                        SendReply(player, string.Format("{0} - {1} - Amount: {2} - In: {3}", count.ToString(), container["pos"].ToString(), container["amount"], container["in"]));
                        count++;
                    }
                    break;
                default:
                    object successs = FindPlayer(args[1]);
                    if (successs is string)
                    {
                        SendReply(player, (string)successs);
                        return;
                    }
                    if (successs is Dictionary<string, Dictionary<string, object>>)
                    {
                        SendReply(player, "Multiple players found, use the SteamID or use a fuller name");
                        foreach (KeyValuePair<string, Dictionary<string, object>> pair in (Dictionary<string, Dictionary<string, object>>)successs)
                        {
                            SendReply(player, string.Format("{0} - {1} - {2} - {3}", pair.Key, (string)pair.Value["name"], (string)pair.Value["state"], (string)pair.Value["status"]));
                        }
                        return;
                    }
                    ResetFind(player);
                    switch (args[0].ToLower())
                    {
                        case "sleepingbag":
                        case "bag":
                            List<Dictionary<string, object>> bags = FindSleepingBags((string)((Dictionary<string, object>)successs)["userid"]);
                            if (bags.Count == 0)
                            {
                                SendReply(player, "No sleeping bags found for this player");
                                return;
                            }
                            foreach (Dictionary<string, object> bag in bags)
                            {
                                AddFind(player, count, bag);
                                SendReply(player, string.Format("{0} - {1} - {2}", count.ToString(), bag["name"].ToString(), bag["pos"].ToString()));
                                count++;
                            }
                            break;
                        case "privilege":
                        case "cupboard":
                        case "toolcupboard":

                            List<Dictionary<string, object>> privileges = FindPrivileges((string)((Dictionary<string, object>)successs)["userid"]);
                            if (privileges.Count == 0)
                            {
                                SendReply(player, "No tool cupboard privileges found for this player");
                                return;
                            }
                            foreach (Dictionary<string, object> priv in privileges)
                            {
                                AddFind(player, count, priv);
                                SendReply(player, string.Format("{0} - {1}", count.ToString(), priv["pos"].ToString()));
                                count++;
                            }
                            break;

                        default:


                            break;


                    }

                    break;
            }
        }
    }
}
