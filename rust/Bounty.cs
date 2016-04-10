using System.Collections.Generic;
using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
using UnityEngine;



namespace Oxide.Plugins
{
    [Info("Bounty", "k1lly0u", "0.1.71", ResourceId = 1649)]
    class Bounty : RustPlugin
    {

        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;
        [PluginReference]
        Plugin PopupNotifications;
        [PluginReference]
        Plugin Economics;

        private bool Changed;

        BountyDataStorage bountyData;
        private DynamicConfigFile BountyData;

        RewardDataStorage rewardData;
        private DynamicConfigFile RewardData;

        PlayerTimeStamp playerTimeData = new PlayerTimeStamp();

        private Dictionary<string, string> itemInfo;

        private Dictionary<ulong, OpenBox> rewardBoxs = new Dictionary<ulong, OpenBox>();
        string rBox = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";

        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("bounty.use", this);
            permission.RegisterPermission("bounty.admin", this);

            lang.RegisterMessages(messages, this);
            
            BountyData = Interface.Oxide.DataFileSystem.GetFile("bounty_players");
            RewardData = Interface.Oxide.DataFileSystem.GetFile("bounty_rewards");
            itemInfo = new Dictionary<string, string>();            

            LoadData();
            LoadVariables();           

        }
        void OnServerInitialized()
        {
            CheckDependencies();   
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (bountyData.players.ContainsKey(player.userID))
                {
                    if (bountyData.players[player.userID].Bountys.Count > 0)
                        initTimestamp(player);
                }
            }
            InitializeTable();
            SaveDataLoop();
            if (useReminders)
                RemindPlayers();
        }
        void CheckDependencies()
        {
            if (Friends == null)
            {
                if (useFriendsAPI)
                {
                    PrintWarning($"FriendsAPI could not be found! Disabling friends feature");
                    useFriendsAPI = false;
                }
            }

            if (Clans == null)
            {
                if (useClans)
                {
                    PrintWarning($"Clans could not be found! Disabling clans feature");
                    useClans = false;
                }
            }
            if (PopupNotifications == null)
            {
                if (usePopup)
                {
                    PrintWarning($"Popup Notifications could not be found! Disabling feature");
                    usePopup = false;
                }
            }
            if (Economics == null)
            {
                if (useEconomics)
                {
                    PrintWarning($"Economics could not be found! Disabling money feature");
                    useEconomics = false;
                }
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (bountyData.players.ContainsKey(player.userID))
            {
                int count = bountyData.players[player.userID].Bountys.Count;
                if (count > 0)
                {
                    initTimestamp(player);
                    if (usePopup && PopupNotifications)
                    {
                        timer.Once(15, ()=> SendPopup(player, string.Format(lang.GetMessage("numOutstand", this, player.UserIDString), count)));
                    }
                    else
                        timer.Once(15, () => SendMSG(player, string.Format(lang.GetMessage("numOutstand", this, player.UserIDString), count)));                    
                }
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            SaveData();
            playerTimeData.playerTime.Clear();
            rewardBoxs.Clear();
        }        
       
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info.Initiator == null) return;
                if (entity is BasePlayer && info.Initiator is BasePlayer)
                {
                    if ((BasePlayer)entity != (BasePlayer)info.Initiator)
                    {
                        BasePlayer victim = (BasePlayer)entity;
                        ulong VID = victim.userID;
                        BasePlayer attacker = (BasePlayer)info.Initiator;
                        ulong AID = attacker.userID;
                        if (bountyData.players.ContainsKey(VID))
                        {
                            if (bountyData.players[VID].Bountys.Count > 0)
                            {
                                if (useClans)
                                {
                                    if (IsClanmate(VID, AID))
                                    {
                                        if (usePopup && PopupNotifications)
                                        {
                                            SendPopup(attacker, lang.GetMessage("title", this, attacker.UserIDString) + lang.GetMessage("clanMate", this, attacker.UserIDString));
                                        }
                                        else
                                            SendMSG(attacker, lang.GetMessage("clanMate", this, attacker.UserIDString));
                                        return;
                                    }
                                }
                                if (useFriendsAPI)
                                {
                                    if (IsFriend(victim, attacker.userID))
                                    {
                                        if (usePopup && PopupNotifications)
                                        {
                                            SendPopup(attacker, lang.GetMessage("title", this, attacker.UserIDString) + lang.GetMessage("isFriend", this, attacker.UserIDString));
                                        }
                                        else
                                            SendMSG(attacker, lang.GetMessage("isFriend", this, attacker.UserIDString));
                                        return;
                                    }
                                }
                                recordEarnings(attacker, victim);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        void OnPlayerLootEnd(PlayerLoot inventory)
        {
            BasePlayer player = inventory.GetComponent<BasePlayer>();

            if (rewardBoxs.ContainsKey(player.userID))
                StoreRewardData(player);            
        }
        #endregion

        #region methods
        //////////////////////////////////////////////////////////////////////////////////////
        // Bounty Methods ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        
        void addBounty(BasePlayer player, BasePlayer target, string item, int amount)
        {

            if (checkExisting(player, target))
            {
                SendMSG(player, lang.GetMessage("existBounty", this, player.UserIDString));
                return;
            }
            if (rewardBoxs.ContainsKey(player.userID))
            {
                SendMSG(player, lang.GetMessage("existBox", this, player.UserIDString));
                return;
            }
            ulong TID = target.userID;
            if (item == "Money")
            {
                List<ItemStorage> items = new List<ItemStorage>();
                ItemStorage bItem = new ItemStorage();
                bItem.money = true;
                bItem.amount = amount;
                bItem.itemname = item;
                items.Add(bItem);
                bountyData.players[TID].Bountys.Add(player.userID, new BountyInfo() { InitiatorName = player.displayName, BountyItems = items });
                bountyData.players[TID].TotalBountys++;
                initTimestamp(target);
                SaveData();
                notifyBounty(player, target);
                return;
            }
            var pos = player.transform.position;

            Vector3 newPos = pos + player.eyes.BodyForward() + new Vector3(0, 1, 0);
            BaseEntity box = GameManager.server.CreateEntity(rBox, newPos);           
            box.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
            box.Spawn(true);
            ItemContainer loot = box.GetComponent<ItemContainer>();
            var ownerloot = player.inventory.loot;
            ownerloot.StartLootingEntity(box, true);
            ownerloot.AddContainer(loot);
            ownerloot.SendImmediate();
            SendMSG(player, lang.GetMessage("setReward", this, player.UserIDString));
            rewardBoxs[player.userID] = new OpenBox() { entity = box, target = target };
        }
       
        void StoreRewardData(BasePlayer player)
        {
            ulong ID = player.userID;
            if (rewardBoxs.ContainsKey(ID))
            {
                BasePlayer target = rewardBoxs[ID].target;
                ulong TID = target.userID;
                BaseEntity box = rewardBoxs[ID].entity.GetComponent<BaseEntity>();
                StorageContainer loot = box.GetComponent<StorageContainer>();
                List<ItemStorage> items = new List<ItemStorage>();

                foreach (Item item in loot.inventory.itemList)
                {
                    ItemStorage bItem = new ItemStorage();
                    bItem.amount = item.amount;
                    bItem.itemname = item.info.displayName.english;
                    items.Add(bItem);
                }
                if (items.Count == 0)
                {
                    SendMSG(player, lang.GetMessage("noItems", this, player.UserIDString));
                    loot.inventory.itemList.Clear();
                    rewardBoxs.Remove(ID);
                    loot.KillMessage();
                    loot.SendNetworkUpdateImmediate(false);
                    return;
                }

                bountyData.players[TID].Bountys.Add(ID, new BountyInfo() { InitiatorName = player.displayName, BountyItems = items });
                bountyData.players[TID].TotalBountys++;
                initTimestamp(target);
                SaveData();

                loot.inventory.itemList.Clear();
                rewardBoxs.Remove(ID);
                notifyBounty(player, target);
                
                loot.KillMessage();
                loot.SendNetworkUpdateImmediate(false);
            }
        }
        private void notifyBounty(BasePlayer player, BasePlayer target)
        {
            if (usePopup && PopupNotifications)
            {
                if (globalBroadcast)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        SendPopup(p, string.Format(lang.GetMessage("addBounty", this, player.UserIDString), target.displayName));
                    }
                }
                else { SendPopup(player, string.Format(lang.GetMessage("addBounty", this, player.UserIDString), target.displayName)); }
                
                SendPopup(target, string.Format(lang.GetMessage("bountyAdded", this, player.UserIDString), player.displayName));
            }
            else
            {
                if (globalBroadcast)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        SendMSG(p, string.Format(lang.GetMessage("addBounty", this, player.UserIDString), target.displayName));
                    }
                }
                else { SendMSG(player, string.Format(lang.GetMessage("addBounty", this, player.UserIDString), target.displayName)); }
                SendMSG(target, string.Format(lang.GetMessage("bountyAdded", this, player.UserIDString), player.displayName));
            }
        }
        private void SendMSG(BasePlayer player, string msg)
        {
            SendReply(player, mainColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + msgColor + msg + "</color>");
        }
        private bool checkExisting(BasePlayer player, BasePlayer target)
        {
            ulong TID = target.userID;
            if (!bountyData.players.ContainsKey(TID))
            {
                bountyData.players.Add(TID, new PlayerData()
                {
                    PlayerName = target.displayName,
                    PlayerID = TID,                    
                    Bountys = new Dictionary<ulong, BountyInfo>()
                });
                return false;
            }           
            if (bountyData.players[TID].Bountys.ContainsKey(player.userID))
                return true;
            return false;
        }
        private bool itemCosts(BasePlayer player, string item, int amount)
        {
            if (item == "Money")
            {
                return true;
            }
            string itemshortname = itemInfo[item];
            var definition = ItemManager.FindItemDefinition(itemshortname);

            int itemID = definition.itemid;
            int invAmount = player.inventory.GetAmount(itemID);
            Puts(invAmount.ToString());

            if (amount <= invAmount)
            {
                player.inventory.Take(null, itemID, amount);

                return true;
            }
            return false;
        }
        private void recordEarnings(BasePlayer player, BasePlayer victim)
        {
            ulong VID = victim.userID;
            ulong PID = player.userID;
            if (!rewardData.rewards.ContainsKey(PID))
            {
                rewardData.rewards.Add(PID, new RewardInfo()
                {
                    PlayerID = PID,
                    PlayerName = player.displayName,                    
                    Rewards = new Dictionary<int, UnclaimedData>()
                });
            }
            foreach (var bounty in bountyData.players[VID].Bountys)
            {
                int rewardCount = rewardData.rewards[PID].Rewards.Count;
                rewardData.rewards[PID].Rewards.Add((rewardCount + 1), new UnclaimedData()
                {
                    VictimID = VID,
                    VictimName = victim.displayName,
                    Rewards = bounty.Value.BountyItems
                });
                rewardData.rewards[PID].TotalCount++;
            }
            int bountyCount = bountyData.players[VID].Bountys.Count;
            calculateTimestamp(victim);
            bountyData.players[VID].Bountys.Clear();
            if (usePopup && PopupNotifications)
            {
                SendPopup(player, string.Format(lang.GetMessage("numEarnt", this, player.UserIDString), victim.displayName, bountyCount.ToString()));
            }
            else
                SendMSG(player, string.Format(lang.GetMessage("numEarnt", this, player.UserIDString), victim.displayName, bountyCount.ToString()));
        }
        private bool claimBounty(BasePlayer player, int ID)
        {           
            foreach (var entry in rewardData.rewards[player.userID].Rewards[ID].Rewards)
            {
                if (entry.money)
                {
                    if (RewardMoney(player, entry.amount))
                        return true;
                }
                else
                {
                    GiveItem(player, entry.itemname, entry.amount, player.inventory.containerMain);                    
                }
            }
            return true;
        }
        private void InitializeTable()
        {
            itemInfo.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions();
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                itemInfo.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToLower());
            }
        }
        private object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref)
        {
            itemname = itemname.ToLower();
            bool isBP = false;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (itemInfo.ContainsKey(itemname))
                itemname = itemInfo[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
                return string.Format("{0} {1}", "Item not found: ", itemname);
            player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, amount, isBP), pref);
            return true;
        }
        List<BasePlayer> FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                    if (player.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(player);
                        return foundPlayers;
                    }
                string lowername = player.displayName.ToLower();
                if (lowername.Contains(lowerarg))
                {
                    foundPlayers.Add(player);
                }
            }
            if (foundPlayers.Count == 0)
            {
                foreach (var player in BasePlayer.sleepingPlayerList)
                {
                    if (steamid != 0L)
                        if (player.userID == steamid)
                        {
                            foundPlayers.Clear();
                            foundPlayers.Add(player);
                            return foundPlayers;
                        }
                    string lowername = player.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                    {
                        foundPlayers.Add(player);
                    }
                }
            }
            return foundPlayers;
        }
        List<string> FindItem(string arg)
        {
            var foundItems = new List<string>();

            foreach (var item in itemInfo)
            {
                string lowername = arg.ToLower();

                if (lowername == item.Key)
                {
                    foundItems.Add(item.Key);
                    Puts(foundItems.Count.ToString());
                }
                else if (lowername == item.Value)
                {
                    foundItems.Add(item.Key);
                    Puts(foundItems.Count.ToString());
                }
            }
            return foundItems;
        }
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(BasePlayer player, ulong friendID)
        {
            bool isFriend = (bool)Friends?.Call("IsFriend", player.userID, friendID);
            return isFriend;
        }
        private void RemindPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (bountyData.players.ContainsKey(player.userID))
                {
                    int count = bountyData.players[player.userID].Bountys.Count;
                    if (count > 0)
                    {
                        if (usePopup && PopupNotifications)
                        {
                            SendPopup(player, string.Format(lang.GetMessage("numOutstand", this, player.UserIDString), count));
                        }
                        else
                            SendMSG(player, string.Format(lang.GetMessage("numOutstand", this, player.UserIDString), count));
                    }
                }
            }
            timer.Once(remindTime * 60, () => RemindPlayers());
        }
        private void SendPopup(BasePlayer player, string msg)
        {
            PopupNotifications?.Call("CreatePopupOnPlayer", lang.GetMessage("title", this, player.UserIDString) + msg, player, popupTime);
        }
        private bool CheckPlayerMoney(BasePlayer player, int amount)
        {
            if (useEconomics)
            {
                double money = (double)Economics?.CallHook("GetPlayerMoney", player.userID);
                if (money >= amount)
                {
                    money = money - amount;
                    Economics?.CallHook("Set", player.userID, money);
                    return true;
                }
                return false;
            }
            return false;
        }
        private bool RewardMoney(BasePlayer player, double amount)
        {
            if (useEconomics)
            {
                double money = (double)Economics?.CallHook("GetPlayerMoney", player.userID);
                if (amount >= 1)
                {
                    var setmoney = money + amount;
                    Economics?.CallHook("Set", player.userID, setmoney);
                    return true;
                }

                return false;
            }
            return false;
        }
       

        //////////////////////////////////////////////////////////////////////////////////////
        // Time //////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void initTimestamp(BasePlayer player)
        {
            long currentTimestamp = getCurrentTime();
            var state = new PlayerStateInfo(player);
            var ID = player.userID;

            if (!playerTimeData.playerTime.ContainsKey(ID))
            {
                playerTimeData.playerTime.Add(ID, state);
            }
            playerTimeData.playerTime[ID].InitTimeStamp = currentTimestamp;
        }
        private void calculateTimestamp(BasePlayer player)
        {
            var ID = player.userID;
            if (!bountyData.players.ContainsKey(ID)) return;
            if (bountyData.players[ID].Bountys.Count > 0)
            {
                long currentTimestamp = getCurrentTime();
                long initTimeStamp = playerTimeData.playerTime[ID].InitTimeStamp;
                long totalPlayed = currentTimestamp - initTimeStamp;

                bountyData.players[ID].TotalWantedSec += totalPlayed;
                TimeSpan ClockPlayTime = TimeSpan.FromSeconds(bountyData.players[ID].TotalWantedSec);
                bountyData.players[ID].TotalWantedClock = string.Format("{0:c}", ClockPlayTime);

                foreach (var bounty in bountyData.players[ID].Bountys)
                {
                    var e = bounty.Value;
                    e.WantedTime += totalPlayed;
                    e.WantedTimeClock = string.Format("{0:c}", ClockPlayTime);
                }

                playerTimeData.playerTime[ID].InitTimeStamp = currentTimestamp;

            }
        }
        private long getCurrentTime()
        {
            long timestamp = 0;
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000;
            timestamp = ticks;

            return timestamp;
        }        
        
        #endregion

        #region chat commands
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("bounty")]
        private void cmdBounty(BasePlayer player, string command, string[] args)
        {
            if (!canBountyPlayer(player)) return;
            
            if (args.Length == 0)
            {
                SendMSG(player, lang.GetMessage("commands", this, player.UserIDString));
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty add</color>" + msgColor + " - Adds a bounty");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty claim</color>" + msgColor + " - Lists your current rewards");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty claim #ID#</color>" + msgColor + " - Claim reward with ID number");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty close</color>" + msgColor + " - Close a open bounty box");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty check</color>" + msgColor + " - Check yourself for bounty(s)");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty check playername</color>" + msgColor + " - Check player for bounty(s)");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty top</color>" + msgColor + " - List the top bounty hunters / top wanted time");
                SendMSG(player, "</color>" + "<color=#8dc63f>/bounty wanted</color>" + msgColor + " - List the most wanted / most current wanted time");

                if (canBountyAdmin(player))
                {
                    SendMSG(player, "</color>" + "<color=#8dc63f>/bounty clear playername</color>" + msgColor + " - Clear player bounty(s)");
                    SendMSG(player, "</color>" + "<color=#8dc63f>/bounty wipe</color>" + msgColor + " - Wipe all bounty data");
                }
                return;
            }
            ulong ID = player.userID;
            var data = bountyData.players;
            var rdata = rewardData.rewards;
            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "add":
                        if (!useEconomyOnly)
                            SendMSG(player, lang.GetMessage("addFormat", this, player.UserIDString));
                        if (Economics && useEconomics)
                            SendMSG(player, lang.GetMessage("addFormatMoney", this, player.UserIDString));
                        return;
                    case "claim":
                        if (rdata.ContainsKey(ID))
                        {
                            if (rdata[ID].Rewards.Count > 0)
                            {
                                SendMSG(player, string.Format(lang.GetMessage("claimRewards", this, player.UserIDString), rdata[ID].Rewards.Count));
                                foreach (var reward in rdata[ID].Rewards)
                                {
                                    SendReply(player, string.Format(lang.GetMessage("rewardInfo", this, player.UserIDString), reward.Key.ToString()));
                                    foreach (var item in reward.Value.Rewards)
                                    {
                                        SendReply(player, mainColor + item.itemname + "</color>" + msgColor + " --- </color>" + mainColor + item.amount.ToString() + "</color>");
                                    }
                                }
                                return;
                            }
                        }
                        SendMSG(player, lang.GetMessage("noRewards", this, player.UserIDString));
                        return;

                    case "check":
                        if (data.ContainsKey(ID))
                        {
                            if (data[ID].Bountys.Count > 0)
                            {
                                SendMSG(player, string.Format(lang.GetMessage("numOutstand", this, player.UserIDString), data[ID].Bountys.Count));
                                foreach (var entry in data[ID].Bountys)
                                {
                                    SendReply(player, string.Format(lang.GetMessage("checkInfo", this, player.UserIDString), entry.Value.InitiatorName));
                                    foreach (var item in entry.Value.BountyItems)
                                    {
                                        SendReply(player, mainColor + item.itemname + "</color>" + msgColor + " --- </color>" + mainColor + item.amount.ToString() + "</color>");
                                    }
                                }
                                return;
                            }
                        }
                        SendMSG(player, lang.GetMessage("noOutstand", this, player.UserIDString));
                        return;

                    case "close":
                        if (rewardBoxs.ContainsKey(player.userID))
                            StoreRewardData(player);
                        return;

                    case "top":
                        if (rdata.Count > 0)
                        {
                            Dictionary<string, int> topHunters = new Dictionary<string, int>();
                            foreach (var entry in rdata)
                            {
                                topHunters.Add(entry.Value.PlayerName, entry.Value.TotalCount);
                            }
                            Dictionary<string, int> top5 = topHunters.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (top5.Count > 0)
                            {
                                SendMSG(player, lang.GetMessage("mostKills", this, player.UserIDString));
                                foreach (var name in top5)
                                {
                                    SendReply(player, string.Format(lang.GetMessage("topList", this, player.UserIDString), name.Key, name.Value));
                                }
                            }
                            /////////////////////////////////

                            Dictionary<string, long> topWanted = new Dictionary<string, long>();
                            foreach (var entry in data)
                            {
                                topWanted.Add(entry.Value.PlayerName, entry.Value.TotalWantedSec);
                            }
                            Dictionary<string, long> top5w = topWanted.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (top5w.Count > 0)
                            {
                                SendMSG(player, lang.GetMessage("mostOverallTime", this, player.UserIDString));
                                foreach (var name in top5w)
                                {
                                    TimeSpan ClockPlayTime = TimeSpan.FromSeconds(name.Value);
                                    string time = string.Format("{0:c}", ClockPlayTime);
                                    SendReply(player, string.Format(lang.GetMessage("wantedOverallTime", this, player.UserIDString), name.Key, time));
                                }
                            }
                            return;
                        }
                        SendMSG(player, lang.GetMessage("noTop", this, player.UserIDString));
                        return;
                    case "save":
                        if (rewardBoxs.ContainsKey(player.userID))
                            StoreRewardData(player);
                        return;
                    case "wanted":
                        if (data.Count > 0)
                        {
                            Dictionary<string, int> mostWanted = new Dictionary<string, int>();
                            foreach (var entry in data)
                            {
                                mostWanted.Add(entry.Value.PlayerName, entry.Value.Bountys.Count);
                            }
                            Dictionary<string, int> top5 = mostWanted.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (top5.Count > 0)
                            {
                                SendMSG(player, lang.GetMessage("mostWanted", this, player.UserIDString));
                                foreach (var name in top5)
                                {
                                    SendReply(player, string.Format(lang.GetMessage("wantedList", this, player.UserIDString), name.Key, name.Value));
                                }                                
                            }
                            Dictionary<string, long> longestWanted = new Dictionary<string, long>();
                            foreach (var entry in data)
                            {
                                List<long> times = new List<long>();
                                long best = 0;
                                foreach (var bounty in entry.Value.Bountys)
                                {
                                    long t = bounty.Value.WantedTime;
                                    if (t > best)
                                        best = t;
                                }

                                longestWanted.Add(entry.Value.PlayerName, best);
                            }
                            Dictionary<string, long> long5 = longestWanted.OrderByDescending(pair => pair.Value).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
                            if (long5.Count > 0)
                            {
                                SendMSG(player, lang.GetMessage("mostCurrentTime", this, player.UserIDString));
                                foreach (var name in long5)
                                {
                                    TimeSpan ClockPlayTime = TimeSpan.FromSeconds(name.Value);
                                    string time = string.Format("{0:c}", ClockPlayTime);
                                    SendReply(player, string.Format(lang.GetMessage("wantedCurrentTime", this, player.UserIDString), name.Key, time));
                                }
                            }
                            return;
                        }
                        SendMSG(player, lang.GetMessage("noWanted", this, player.UserIDString));
                        return;

                    case "wipe":
                        if (isAuth(player))
                        {
                            rdata.Clear();
                            data.Clear();
                            SaveData();
                            SendMSG(player, lang.GetMessage("wipedData", this, player.UserIDString));
                        }
                        return;
                }
            }
            if (args[0].ToLower() == "claim" && args.Length == 2)
            {
                if (rdata.ContainsKey(ID))
                {
                    int rewardNum;
                    if (!int.TryParse(args[1], out rewardNum))
                    {
                        SendMSG(player, lang.GetMessage("invRNum", this, player.UserIDString));
                        return;
                    }
                    if (!rdata[ID].Rewards.ContainsKey(rewardNum))
                    {
                        SendMSG(player, lang.GetMessage("invRNum", this, player.UserIDString));
                        return;
                    }

                    bool success = claimBounty(player, rewardNum);
                    if (success)
                    {
                        SendMSG(player, string.Format(lang.GetMessage("claimSuccess", this, player.UserIDString), rewardNum));
                        rdata[ID].Rewards.Remove(rewardNum);
                        return;
                    }
                }
                SendMSG(player, lang.GetMessage("claimFormat", this, player.UserIDString));
                return;
            }

            BasePlayer target = null;
            if (args.Length >= 2)
            {
                var players = FindPlayer(args[1]);
                if (players.Count == 0)
                {
                    SendMSG(player, lang.GetMessage("noPlayers", this, player.UserIDString));
                    return;
                }
                if (players.Count > 1)
                {
                    SendMSG(player, lang.GetMessage("multiplePlayers", this, player.UserIDString));
                    return;
                }
                target = players[0];
            }          
            if (target != null)
            {
                
                var TID = target.userID;
                switch (args[0].ToLower())
                {
                    case "add":
                        if (args.Length >= 2)
                        {
                            int amount = 1;
                            //if (target == player)
                            //{
                            //    SendMSG(player, lang.GetMessage("noSelf", this, player.UserIDString));
                            //    return;
                            //}
                            if (args.Length == 4)
                            {                                
                                if (args[2].ToLower() == "money" && useEconomics)
                                {
                                    if (!int.TryParse(args[3], out amount))
                                    {
                                        SendMSG(player, lang.GetMessage("invAmount", this, player.UserIDString));
                                        return;
                                    }
                                    if (CheckPlayerMoney(player, amount))
                                    {
                                        addBounty(player, target, "Money", amount);
                                        return;
                                    }
                                    SendMSG(player, lang.GetMessage("noMoney", this, player.UserIDString));
                                    return;
                                }                                
                            }
                            if (useEconomyOnly) SendMSG(player, lang.GetMessage("addFormatMoney", this, player.UserIDString));
                            else                                  
                                addBounty(player, target, "", 0);
                            return;
                        }
                        SendMSG(player, lang.GetMessage("addFormat", this, player.UserIDString));
                        if (Economics && useEconomics)
                            SendMSG(player, lang.GetMessage("addFormatMoney", this, player.UserIDString));
                        return;
                    case "check":
                        if (args.Length == 2)
                        {
                            if (data.ContainsKey(TID))
                            {
                                if (data[TID].Bountys.Count > 0)
                                {
                                    SendMSG(player, string.Format(lang.GetMessage("checkBounty", this, player.UserIDString), data[TID].Bountys.Count, data[TID].PlayerName));
                                    foreach (var entry in data[TID].Bountys)
                                    {
                                        SendReply(player, string.Format(msgColor + lang.GetMessage("checkInfo", this, player.UserIDString) + "</color>", entry.Value.InitiatorName));
                                        foreach (var item in entry.Value.BountyItems)
                                        {
                                            SendReply(player, mainColor + item.itemname + "</color>" + msgColor + " --- </color>" + mainColor + item.amount.ToString() + "</color>");
                                        }
                                    }
                                    return;
                                }                                
                            }
                            SendMSG(player, string.Format(lang.GetMessage("pnoOutstand", this, player.UserIDString), target.displayName));
                            return;
                        }
                        SendMSG(player, lang.GetMessage("checkFormat", this, player.UserIDString));
                        return;

                    case "clear":
                        if (isAuth(player))
                        {
                            if (args.Length == 2)
                            {
                                if (data.ContainsKey(TID))
                                {
                                    if (data[TID].Bountys.Count > 0)
                                    {
                                        int icount = data[TID].Bountys.Count;
                                        data[TID].Bountys.Clear();
                                        SendMSG(player, string.Format(lang.GetMessage("clearBounty", this, player.UserIDString), icount, target.displayName));
                                        return;
                                    }
                                }
                                SendMSG(player, string.Format(lang.GetMessage("pnoOutstand", this, player.UserIDString), target.displayName));
                            }
                        }
                        return;
                }
            }
        }
        [ConsoleCommand("bounty.wipe")]
        void ccmdbWipe(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            var data = bountyData.players;
            var rdata = rewardData.rewards;
            rdata.Clear();
            data.Clear();
            SaveData();
            Puts("Bounty data wiped!");
        }
        [ConsoleCommand("bounty.list")]
        void ccmdbList(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            foreach (var entry in bountyData.players)
            {
                if (entry.Value.Bountys.Count > 0)
                {
                    Puts("Name: " + entry.Value.PlayerName);
                    Puts("ID: " + entry.Value.PlayerID);
                    foreach (var reward in entry.Value.Bountys)
                    {
                        Puts("-- Initiator: " + reward.Value.InitiatorName);
                        Puts("-- Items;");
                        foreach (var ientry in reward.Value.BountyItems)
                        {
                            Puts("---- " + ientry.itemname + " X " + ientry.amount);
                        }
                    }
                }
            }
        }


        #endregion

        #region permissions/auth

        //////////////////////////////////////////////////////////////////////////////////////
        // Permissions/Auth //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool canBountyPlayer(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "bounty.use")) return true;
            else if (canBountyAdmin(player)) return true;            
            return false;
        }
        bool canBountyAdmin(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "bounty.admin")) return true;
            else if (isAuth(player)) return true;
            return false;
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection.authLevel >= auth) return true;
            return false;
        }
        bool isAuthCon(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, lang.GetMessage("noPerms", this));
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static int auth = 1;
        static bool useClans = true;
        static bool useFriendsAPI = true;
        static bool usePopup = true;
        static bool useReminders = true;
        static bool useEconomics = true;
        static bool globalBroadcast = true;
        static bool useEconomyOnly = false;

        static int saveLoop = 10;
        static int popupTime = 30;
        static int remindTime = 20;

        static string mainColor = "<color=orange>";
        static string msgColor = "<color=#939393>";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Authlevel to access admin commands", ref auth);
            CheckCfg("Use FriendsAPI", ref useFriendsAPI);
            CheckCfg("Use Clans", ref useClans);
            CheckCfg("Broadcast new bounty's to global", ref globalBroadcast);
            CheckCfg("Popups - Use Popup Notifications", ref usePopup);
            CheckCfg("Popups - Popup Notification time", ref popupTime);
            CheckCfg("Reminders - Use reminders", ref useReminders);
            CheckCfg("Reminders - Timer (mins)", ref remindTime);
            CheckCfg("Economics - Use money as bounty", ref useEconomics);
            CheckCfg("Economics - Only use money to set a bounty", ref useEconomyOnly);
            CheckCfg("Colors - Main color", ref mainColor);
            CheckCfg("Colors - Message color", ref msgColor);
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        #endregion        

        #region data
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
                
        void SaveData()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (bountyData.players.ContainsKey(player.userID))
                {
                    if (bountyData.players[player.userID].Bountys.Count > 0)
                        calculateTimestamp(player);
                }
            }
            BountyData.WriteObject(bountyData);
            RewardData.WriteObject(rewardData);
        }
        void SaveDataLoop()
        {
            SaveData();
            timer.Once(saveLoop, () => SaveDataLoop());
        }
        void LoadData()
        {
            try
            {
                bountyData = BountyData.ReadObject<BountyDataStorage>();
            }
            catch
            {
                Puts("Couldn't load Bounty player data, creating new datafile");
                bountyData = new BountyDataStorage();
            }
            try
            {
                rewardData = RewardData.ReadObject<RewardDataStorage>();
            }
            catch
            {
                Puts("Couldn't load Bounty reward data, creating new datafile");
                rewardData = new RewardDataStorage();
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Class ////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        class BountyDataStorage
        {
            public Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
            public BountyDataStorage() { }
        }
        class PlayerData
        {
            public ulong PlayerID;
            public string PlayerName;
            public int TotalBountys;
            public long TotalWantedSec;
            public string TotalWantedClock;            
            public Dictionary<ulong, BountyInfo> Bountys;

            public PlayerData() { }
            public PlayerData(string name, ulong id)
            {
                PlayerID = id;
                PlayerName = name;                                             
                TotalBountys = 0;
                TotalWantedSec = 0;
                TotalWantedClock = "00:00:00";
                Bountys = new Dictionary<ulong, BountyInfo>();
            }
        }
        class BountyInfo
        {
            public string InitiatorName;
            public List<ItemStorage> BountyItems = new List<ItemStorage>();
            public long WantedTime;
            public string WantedTimeClock;

            public BountyInfo() { }
            public BountyInfo(ulong playerid, string playername, List<ItemStorage> items)
            {
                InitiatorName = playername;
                BountyItems = items;
                WantedTime = 0;
                WantedTimeClock = "00:00:00";                
            }
        }
        class RewardDataStorage
        {
            public Dictionary<ulong, RewardInfo> rewards = new Dictionary<ulong, RewardInfo>();
            public RewardDataStorage() { }
        }
        class RewardInfo
        {            
            public ulong PlayerID;            
            public string PlayerName;
            public int TotalCount;
            public Dictionary<int, UnclaimedData> Rewards;

            public RewardInfo() { }
            public RewardInfo(ulong aid, string attackname)
            {
                PlayerID = aid;
                PlayerName = attackname;
                TotalCount = 0;
                Rewards = new Dictionary<int, UnclaimedData>();
            }
        }
        class UnclaimedData
        {
            public ulong VictimID;
            public string VictimName;
            public List<ItemStorage> Rewards = new List<ItemStorage>();
            public UnclaimedData() { }

        }
        class ItemStorage
        {
            public bool money;
            public int amount;
            public string itemname;
        }
        class OpenBox
        {
            public BasePlayer target;
            public BaseEntity entity;
        }       

        class PlayerTimeStamp
        {
            public Dictionary<ulong, PlayerStateInfo> playerTime = new Dictionary<ulong, PlayerStateInfo>();
            public PlayerTimeStamp() { }
        }
        class PlayerStateInfo
        {
            public long InitTimeStamp;

            public PlayerStateInfo() { }

            public PlayerStateInfo(BasePlayer player)
            {
                InitTimeStamp = 0;
            }
        }        
       
        #endregion

        #region messages
        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Bounty : " },
            {"noPerms", "You do not have permission to use this command" },
            {"neRes", "You do not have enough resources to place this bounty" },
            {"existBounty", "You already have a bounty placed on this player" },
            {"existBox", "You already have reward box open!" },
            {"setReward", "Place your reward items in the box infront of you!" },
            {"addBounty", "A bounty has been placed on {0}" },
            {"bountyAdded", "{0} has just placed a bounty on you!" },
            {"numOutstand", "You currently have {0} outstanding bounty(s)" },
            {"noOutstand", "You have no outstanding bounty's" },
            {"noSelf", "You cannot place a bounty on yourself" },
            {"noItems", "You didn't place any items in the box, no bounty has been placed" },
            {"noPlayers", "Could not find a player with that name" },
            {"multiplePlayers", "Multiple players found with that name" },
            {"noItem", "Could not find a item with that name" },
            {"noMoney", "You do not have enough money" },
            {"multipleItems", "Multiple items found with that name, try typing more of the name" },
            {"invAmount", "The amount needs to be a number" },
            {"checkBounty", "{1} has {0} outstanding bounty(s)" },
            {"pnoOutstand", "{0} has no outstanding bounty's" },
            {"clearBounty", "You have removed {0} bounty(s) from {1}" },
            {"clanMate", "You cannot claim a bounty on a clan mate" },
            {"isFriend", "You cannot claim a bounty on a friend" },
            {"numEarnt", "{0} had {1} bounty(s), you can claim these using /bounty claim" },
            {"checkInfo", "<color=#8dc63f>Initiator:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Rewards;</color>" },
            {"noRewards", "You don't have any rewards" },
            {"claimRewards", "You currently have {0} bounty reward(s) to claim, use /bounty claim #ID#" },
            {"rewardInfo", "<color=#8dc63f>RewardID:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Items;</color>" },
            {"invRNum", "You must type a valid reward number" },
            {"claimSuccess", "You have claimed reward ID {0}" },
            {"claimFormat", "/bounty claim ID#" },
            {"wipedData", "All data has been wiped!" },
            {"mostWanted", "--- Top wanted active bounty's" },
            {"mostCurrentTime", "--- Top current wanted time" },
            {"mostKills", "--- Top bounty killers" },
            {"mostOverallTime", "--- Top total wanted time" },
            {"wantedList", "<color=#8dc63f>Name:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Active Bountys:</color> {1}" },
            {"wantedCurrentTime", "<color=#8dc63f>Name:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Current wanted time:</color> {1}" },
            {"topList", "<color=#8dc63f>Name:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Bountys Collected:</color> {1}" },
            {"wantedOverallTime", "<color=#8dc63f>Name:</color> {0} <color=#31698a>---</color> <color=#8dc63f>Total wanted time:</color> {1}" },
            {"addFormat", "Format: /bounty add PlayerName" },
            {"addFormatMoney", "Economics Format: /bounty add PlayerName money amount" },
            {"checkFormat", "Format: /bounty check PlayerName"},
            {"commands", "Commands" },
            {"noTop", "There are currently no top hunters" },
            {"noWanted", "There are currently no wanted players" }
        };
        #endregion

    }
}
