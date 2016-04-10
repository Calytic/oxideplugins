using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Oxide.Plugins
{
    /*
    [B]Changelog 2.2.2[/B]
    [LIST]
    [*] Added the ability to annouce plugins to Game-Servers.top. This eliminates the need for a server owners to keep listing their mods on their servers.
    [/LIST]
    */
    [Info("VoteChecker", "Pho3niX90", "2.2.2", ResourceId = 1189)]
    class VoteChecker : HurtworldPlugin
    {
        [PluginReference("EconomyBanks")]
        Plugin EconomyBanks;
        string MoneySym;
        public bool EconomyBanksEnabled()
        {
            EconomyBanks = (Plugin)plugins.Find("EconomyBanks");
            if (EconomyBanks == null)
            {
                return false;
            }
            return true;
        }
#region [CLASSES]
        static class Constants
        {
            public const string PLATFORM = "hurtworld";
            public const string SERVICE = "listforge";
            public const bool DEBUG = false;
        }
#endregion

#region [LISTS]
        int timesVoted;
        private Dictionary<ulong, DateTime> Cooldowns = new Dictionary<ulong, DateTime>();
        private Collection<RewardItem> Rewards = new Collection<RewardItem>();
        public Collection<LastVote> Users = new Collection<LastVote>();
        public class RewardItem
        {
            public RewardItem(string itemId, int votesRequired, int itemAmount)
            {
                this.itemId = itemId;
                this.votesRequired = votesRequired;
                this.itemAmount = itemAmount;
            }

            public string itemId { get; set; }
            public int votesRequired { get; set; }
            public int itemAmount { get; set; }
        }
        public class LastVote
        {
            public LastVote(ulong steamid, int lastvote)
            {
                this.steamid = steamid;
                this.lastvote = lastvote;
            }

            public ulong steamid { get; set; }
            public int lastvote { get; set; }
        }
        private Collection<RewardItem> LoadDefaultRewards()
        {

            Rewards = new Collection<RewardItem>
            {
                new RewardItem( "52",          1,      100),
                new RewardItem( "7",           1,      1),
                new RewardItem( "money",        1,      500)
            };

            SaveRewards();
            return Rewards;
        }
#endregion

#region [CONFIGS]
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file for rewards.");
            Config.Clear();
            Config["serverApi"] = "";
            Config["serverId"] = ""; //serverID 1017
            Config["trackingInterval"] = "1";
            Config["trackingType"] = "month";
            Config["autoGive"] = "true";

            Config["tgsApi"] = "";
            Config["tgsAddress"] = "";
            Config["tgsAnnounce"] = "false";
            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"syntax_addreward", "Syntax: /addreward <itemname> <quantity> <votesrequired>"},
                {"syntax_error_votereqmustbepositive","Syntax Error: Vote required needs to be a number"},
                {"syntax_error_itemamountneedstobeanumber","Syntax Error: Item amount needs to be a number" },
                {"msg_getreward","Get rewards for daily votes"},
                {"msg_adminsonly", "Only admins are allowed to use this function"},
                {"msg_nonewrewards", "You have no new rewards, please vote for our server to receive rewards."},
                {"msg_VotingThanks", "Thanks for voting {voteCount} times, you have received {stackToGive} {itemName}"},
                {"msg_VotingThanksM", "Thanks for voting {voteCount} times, you have received {moneySymbol}{stackToGive}" },
                {"msg_reward_last", "LAST Reward: {resourceAmount} * {resource} when you have reached {votesNeeded} votes" },
                {"msg_reward_next_1", "NEXT Reward: {resourceAmount} * {resource} for every vote"},
                {"msg_reward_next_0", "NEXT Reward: {resourceAmount} * {resource} when you have reached {votesNeeded} votes"},
                {"msg_reward", "Reward: {resourceAmount} * {resource} when you have reached {votesNeeded} votes"}
            }, this);
        }
#endregion

#region [HOOKS]
        void Loaded()
        {
            if (EconomyBanksEnabled())
            {
                MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");
            }
            LoadRewards();
            LoadMessages();
            AnnouncePlugins();
            if (Rewards.Count < 1)
            {
                PrintWarning("No items was loaded, will now load defaults");
                LoadDefaultRewards();
                PrintWarning(Rewards.Count + " default rewards loaded");
            }
            LoadVotes();
        }
        void OnPlayerInit(PlayerSession player)

        {
            if (player.Name.ToLower() == "server") return;


            // set cooldown
            TimeSpan time = new TimeSpan(0, 0, 15, 0);
            DateTime combined = DateTime.Now.Add(time);
            var playerid = (ulong)player.SteamId;
            if (!Cooldowns.ContainsKey(playerid)) { Cooldowns.Add(playerid, DateTime.Now); }
            //

            bool autoGive;
            if (!bool.TryParse(Config["autoGive"].ToString(), out autoGive)) { autoGive = true; }
            if (!autoGive) { return; }
            GetRewardDelayed(player);
            Cooldowns[playerid] = combined;
        }
#endregion

#region [CHAT COMMANDS]
        [ChatCommand("addreward")]
        private void ChatCmd_AddReward(PlayerSession player, string cmd, string[] args)
        {
            int voteNeeded;
            int itemAmount;
            if (args.Length < 3) { PrintToChat(player, GetMsg("syntax_addreward", player)); return; };
            if (!int.TryParse(args[args.Length - 1], out voteNeeded)) { PrintToChat(player, GetMsg("syntax_error_votereqmustbepositive", player)); return; }
            if (!int.TryParse(args[args.Length - 2], out itemAmount)) { PrintToChat(player, GetMsg("syntax_error_itemamountneedstobeanumber", player)); return; }

            Array.Resize<string>(ref args, args.Length - 2);
            string itemName = string.Join(" ", args);

            Rewards.Add(new RewardItem(itemName, voteNeeded, itemAmount));
            SaveRewards();
        }
        [ChatCommand("getreward")]
        private void GetMyVoteReward(PlayerSession player, string cmd)
        { GetRewardDelayed(player); }
        private readonly WebRequests webRequestsAddress = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        private readonly WebRequests webRequestsApi = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        [ChatCommand("rewardconf")]
        private void ChatCmd_Config(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrintToChat(player, "Only admins are allowed to use this function"); return; }
            if (args == null || args.Length == 0)
            {
                PrintToChat(player, "TGS API: " + ((string)Config["tgsApi"] == "" ? "No api set, please set ASAP" : (string)Config["tgsApi"]));
                PrintToChat(player, "Listforge API: " + ((string)Config["serverApi"] == "" ? "No api set, please set ASAP" : (string)Config["serverApi"]));
                PrintToChat(player, "ServerID: " + Config["serverId"]);
                PrintToChat(player, "Tracking: " + Config["trackingInterval"] + " " + Config["trackingType"] + "");
                return;
            }
            if (args[0].Length < 1 || args[1].Length < 1) { PrintToChat(player, "/rewardconf [api/serverid/tracking/autogive] configValue"); return; }

            switch (args[0])
            {
                case "api":
                    if (args[1].Equals("listforge"))
                    {
                        Config["serverApi"] = args[2];
                        PrintToChat(player, "ServerApi set to " + args[2]);
                    }
                    else if (args[1].Equals("tgs"))
                    {
                        Config["tgsApi"] = args[2];

                        PrintToChat(player, "Game-Server ServerApi set to " + args[2]);

                        //NOW LETS FETCH SOME DETAILS FOR THE SERVER

                        if (player.Name.ToLower() == "server") return;
                        var playerId = player.SteamId;
                        webRequestsAddress.EnqueueGet("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&getAddress", (code, response) => WebRequestCallbackAddress(code, response, player), this);
                        /////////////////////////////////////////////

                    }
                    break;
                case "serverid":
                    Config["serverId"] = args[1];
                    PrintToChat(player, "ServerID set to " + args[1]);
                    break;
                case "tracking":
                    int interval;
                    if (int.TryParse(args[1], out interval))
                    {
                        Config["trackingInterval"] = args[1];
                        PrintToChat(player, "Interval set to " + args[1]);
                    }
                    else
                    {
                        PrintToChat(player, "Syntax Error: Interval must be an Integer (Number)");
                    }

                    if (args[2].Equals("month", StringComparison.CurrentCultureIgnoreCase)
                        || args[2].Equals("week", StringComparison.CurrentCultureIgnoreCase)
                        || args[2].Equals("day", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Config["trackingType"] = args[2];
                        PrintToChat(player, "Tracking set to " + args[2]);
                    }
                    else
                    {
                        PrintToChat(player, "Syntax Error: Tracking must be either month/day/week");
                    }
                    break;
                case "autoreward":
                    bool val;
                    if (!bool.TryParse(args[1], out val)) { PrintToChat(player, "Please enter either true or false."); };
                    Config["autoGive"] = val;
                    PrintToChat(player, "AutoGive set to " + val);
                    break;
            }

            SaveConfig();
        }
        [ChatCommand("clearrewards")]
        private void ChatCmd_ClearRewards(PlayerSession player, string cmd)
        {
            if (!player.IsAdmin) { PrintToChat(player, GetMsg("msg_adminsonly", player)); return; }
            Rewards.Clear();
            SaveRewards();
            PrintToChat(player, "Rewards file cleared. Please add some rewards with /addreward itemname itemamount votecount");
        }
        [ChatCommand("rewards")]
        private void ChatCmd_Rewards(PlayerSession player, string cmd)
        {
            PrintToChat(player, GetMsg("msg_getreward", player));
            var RewardsSorted = Rewards.OrderBy(a => a.votesRequired);
            var tmplastVote = Users.Where(d => d.steamid == (ulong)player.SteamId).FirstOrDefault();
            int thisvote = (tmplastVote.lastvote == null) ? tmplastVote.lastvote : 0;
            int nextVote = thisvote + 1;
            foreach (var voteReward in RewardsSorted) // Loop through List with foreach.
            {
                string resource = getItemName(voteReward.itemId);
                var votesNeeded = voteReward.votesRequired; //int Votes
                int resourceAmount = voteReward.itemAmount; //int Amount
                if (thisvote == votesNeeded)
                {
                    PrintToChat(player, GetMsg("msg_reward_last", player)
                      .Replace("{resourceAmount}", resourceAmount.ToString())
                      .Replace("{resource}", resource)
                      .Replace("votesNeeded", votesNeeded.ToString()));
                }
                else
                if (nextVote == votesNeeded || votesNeeded == -1)
                {
                    if (votesNeeded == -1)
                    {
                        PrintToChat(player, GetMsg("msg_reward_next_1", player)
                          .Replace("{resourceAmount}", resourceAmount.ToString())
                          .Replace("{resource}", resource));
                    }
                    else
                    {
                        PrintToChat(player, GetMsg("msg_reward_next_0", player)
                          .Replace("{resourceAmount}", resourceAmount.ToString())
                          .Replace("{resource}", resource)
                          .Replace("{votesNeeded}", votesNeeded.ToString()));
                    }
                }
                else {
                    PrintToChat(player, GetMsg("msg_reward", player)
                 .Replace("{resourceAmount}", resourceAmount.ToString())
                 .Replace("{resource}", resource)
                 .Replace("{votesNeeded}", votesNeeded.ToString()));
                }
            }
        }
#endregion

#region [SAVE REWARDS]
        private void SaveRewards() { Interface.GetMod().DataFileSystem.WriteObject("VoteCheckerRewards", Rewards); }
        private void LoadRewards()
        {
            try
            {
                Rewards = Interface.GetMod().DataFileSystem.ReadObject<Collection<RewardItem>>("VoteCheckerRewards");
            }
            catch (Exception e)
            {

                PrintWarning("You are using the old rewards format, we will now try and convert it.");

                var _StoreStock_OLD = Interface.GetMod().DataFileSystem.ReadObject<Collection<string[]>>("VoteCheckerRewards");
                foreach (var item in _StoreStock_OLD)
                {
                    string itemid = item[0];
                    int votecount = int.Parse(item[1]);
                    int amount = int.Parse(item[2]);
                    Rewards.Add(new RewardItem(itemid, votecount, amount));
                }
                PrintWarning("Conversion complete, we converted " + Rewards.Count + " items");
                SaveRewards();
                _StoreStock_OLD.Clear();

            }
        }
        private void SaveVotes() { Interface.GetMod().DataFileSystem.WriteObject("VoteCheckerUserVotes", Users); }
        private void LoadVotes() { Users = Interface.GetMod().DataFileSystem.ReadObject<Collection<LastVote>>("VoteCheckerUserVotes"); }
#endregion

#region [HELPERS]
        private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        void GetRewardDelayed(PlayerSession player)
        {
            PrintWarning("0");
            var playerid = (ulong)player.SteamId;
            if (!Cooldowns.ContainsKey(playerid)) { Cooldowns.Add(playerid, DateTime.Now); }
            DateTime Date;
            PrintWarning("1");
            Cooldowns.TryGetValue(playerid, out Date);
            PrintWarning("2");
            PrintWarning(Date + " " + DateTime.Now);
            PrintWarning("3");
            if (Date > DateTime.Now)
            {
                PrintWarning("4");
                PrintToChat(player, "You can only use this command in " + Date.Subtract(DateTime.Now).Minutes + " minutes");
                return;
            }

            GetRewardsForThisPlayer(player);

            TimeSpan time = new TimeSpan(0, 0, 15, 0);
            DateTime combined = Date.Add(time);
            Cooldowns[playerid] = combined; // we are setting the cooldown.
        }
        void GetRewardsForThisPlayer(PlayerSession player)
        {
            if (player.Name.ToLower() == "server") return;
            var playerId = player.SteamId;
            timesVoted = 0;
            //Game-Servers.top
            //Puts("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&interval=" + Config["trackingInterval"] + "&period=" + Config["trackingType"] + "&steamid=" + playerId + "&app");
            if ((string)Config["tgsApi"] != "" && Config["tgsApi"] != null)
            {
                webRequests.EnqueueGet("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&interval=" + Config["trackingInterval"] + "&period=" + Config["trackingType"] + "&steamid=" + playerId + "&app", (code, response) => WebRequestCallbackApi(code, response, player), this);
                Debug(1, "TGS Passing: http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&interval=" + Config["trackingInterval"] + "&period=" + Config["trackingType"] + "&steamid=" + playerId + "&app");
            }
            //listforge
            if ((string)Config["serverApi"] != "" && Config["serverApi"] != null)
            {
                Puts("http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM);
                webRequests.EnqueueGet("http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM, (code, response) => WebRequestCallback(code, response, player), this);
                Debug(1, "LF Passing: http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM);
            }
            //Puts("http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM);


        }
        void WebRequestCallbackAddress(int code, string response, PlayerSession player)
        {
            if (response == null || code != 200) { Puts("error " + code + ": Couldn't get an answer from Game-Servers.top for " + player.Name); return; }

            Config["tgsAddress"] = "http://game-servers.top/server/" + response;
            PrintToChat(player, "Game-Server address set to " + (string)Config["tgsAddress"]);
            PrintWarning("Your server address has been saved as " + response + ", setup complete for Game-Servers.top");
            SaveConfig();
        }
        void WebRequestCallbackApi(int code, string response, PlayerSession player)
        {
            int tmpVotes = 0;
            if (response == null || code != 200) { Puts("error" + code + ": Couldn't get an answer from Game-Servers.top for " + player.Name); return; }

            if (!int.TryParse(response, out tmpVotes)) { PrintError("Game-Servers.top Error: '" + response + "' - " + player.Name + " didn't received their reward."); }
            Debug(1, "Game-Servers votes is " + tmpVotes + " for player " + player.Name);
            timesVoted += tmpVotes;
            if ((string)Config["serverApi"] == "" || Config["serverApi"] == null)
            {
                giveItems(player, timesVoted);
                SaveVotes();
            }
        }
        void WebRequestCallback(int code, string response, PlayerSession player)
        {
            int tmpVotes = 0;
            if (response == null || code != 200) { Puts("error" + code + ": Couldn't get an answer from Cyberscene for " + player.Name); return; }

            if (!int.TryParse(response, out tmpVotes)) { PrintError("Cyberscene Error: '" + response + "' - " + player.Name + " didn't received their reward."); }
            Debug(1, "Listforge votes is " + tmpVotes + " for player " + player.Name);
            timesVoted += tmpVotes;

            timer.Once(1, () => giveItems(player, timesVoted));
            SaveVotes();
        }

        void giveItems(PlayerSession player, int voteCount)
        {
            Debug(1, "Votecount passed to giveitems is " + voteCount);
            var playerName = player.Name;
            var playerId = (ulong)player.SteamId;


            var tmplastVote = Users.Where(d => d.steamid == playerId).FirstOrDefault();
            //Debug(1, "The lastvotecount before checks is " + tmplastVote.lastvote);
            if (tmplastVote == null)
            {
                Debug(1, "LastVote is null for user, recreate.");
                Users.Add(new LastVote((ulong)playerId, 0));
                SaveVotes();
            }
            else
            {
                if (tmplastVote.lastvote > voteCount) { tmplastVote.lastvote = 0; }; //this means to interval has reset
            }



            Debug(1, "The lastvotecount after checks is " + ((tmplastVote == null) ? "isnull" : tmplastVote.lastvote.ToString()));

            tmplastVote = Users.Where(d => d.steamid == playerId).FirstOrDefault();

            Debug(1, "The lastvotecount after select is " + tmplastVote.lastvote);


            int LastVote = tmplastVote.lastvote;


            if (voteCount == 0 || voteCount == LastVote)
            {
                PrintToChat(player, GetMsg("msg_nonewrewards", player));
                if ((string)Config["tgsApi"] != null && (string)Config["tgsApi"] != "")
                {
                    PrintToChat(player, (string)Config["tgsAddress"]);
                }
                if ((string)Config["serverId"] != null && (string)Config["serverId"] != "")
                {
                    PrintToChat(player, "http://hurtworld-servers.net/server/" + Config["serverId"] + "/");
                }
                return;
            }


            PrintWarning("LastVote: " + LastVote + ", Current vote: " + voteCount);
            var RewardsLimited = from p in Rewards.ToList() where (p.votesRequired > LastVote && p.votesRequired <= voteCount) || p.votesRequired == -1 select p;

            foreach (var voteReward in RewardsLimited)
            {
                var votesNeeded = voteReward.votesRequired;
                int resourceAmount = voteReward.itemAmount;
                int amountToGive = resourceAmount;
                Debug(1, "Should we give? amnt:" + amountToGive + " vtneed:" + votesNeeded + " vtcnt:" + voteCount + " lstvote: " + LastVote);
                int remainderToGive = amountToGive;

                if (voteReward.itemId.Equals("money", StringComparison.OrdinalIgnoreCase) && LastVote < voteCount)
                {
                    if (EconomyBanksEnabled())
                    {
                        EconomyBanks.Call("AddCash", player, (double)voteReward.itemAmount);
                        //hurt.SendChatMessage(player, "You have received " + amountToGive + " for voting");
                        hurt.SendChatMessage(player, GetMsg("msg_VotingThanksM", player.SteamId)
                            .Replace("{voteCount}", voteCount.ToString())
                            .Replace("{moneySymbol}", MoneySym)
                            .Replace("{stackToGive}", voteReward.itemAmount.ToString())
                            .Replace("{itemName}", getItemName(voteReward.itemId))
                            .Replace("{moneySymbol}", MoneySym));
                    }
                    else
                    {
                        PrintError("Money needed to be given, but economy plugin wasn't loaded");
                    }

                }
                else if (!voteReward.itemId.Equals("money", StringComparison.OrdinalIgnoreCase) && LastVote < voteCount)
                {
                    while (remainderToGive > 0)
                    {
                        var maxStackSize = 1000;
                        var stackToGive = maxStackSize;

                        if (remainderToGive < maxStackSize)
                        {
                            stackToGive = remainderToGive;
                        }

                        int itemId;
                        if (!Int32.TryParse(voteReward.itemId, out itemId))
                        {
                            PrintError("It seems your rewards file has been manually modified and is now corrupted. Please delete the rewards .json file in the data folder and reload the plugin.");
                        }
                        var ItemMgr = GlobalItemManager.Instance;
                        ItemMgr.GiveItem(player.Player, ItemMgr.GetItem(itemId), stackToGive);
                        remainderToGive = remainderToGive - maxStackSize;
                        var itemName = getItemName(itemId);

                        hurt.SendChatMessage(player, GetMsg("msg_VotingThanks", player.SteamId)
                            .Replace("{voteCount}", voteCount.ToString())
                            .Replace("{stackToGive}", stackToGive.ToString())
                            .Replace("{itemName}", itemName));
                    }
                }
            }

            if (tmplastVote != null) { tmplastVote.lastvote = voteCount; Debug(1, "Storing users last vote, "); } //else { Users.Add(new LastVote(playerId, voteCount)); }
            //possible fix v
            foreach (var e in Users.Where(a => a.steamid == (ulong)player.SteamId))
            {
                e.lastvote = voteCount;
            }
            //possible fix ^

            timesVoted = 0;
        }
        private void PrintToChat(PlayerSession player, string Message)
        {
            hurt.SendChatMessage(player, Message);
        }
        string GetMsg(string key, object userID = null)
        {
            return (userID == null) ? lang.GetMessage(key, this) : lang.GetMessage(key, this, userID.ToString());
        }
        string getItemName(int itemId)
        {
            return GlobalItemManager.Instance.GetItem(itemId).GetNameKey().Split('/').Last();
        }
        string getItemName(string tmpitemId)
        {
            int itemId;
            if (!int.TryParse(tmpitemId, out itemId) && tmpitemId.Equals("money", StringComparison.OrdinalIgnoreCase))
            {
                return "Money";
            }
            else
            {
                return GlobalItemManager.Instance.GetItem(itemId).GetNameKey().Split('/').Last();
            }
        }
        private void Debug(int level, string msg)
        {
            if (Constants.DEBUG)
            {
                switch (level)
                {
                    case 0:
                        Puts(msg);
                        break;
                    case 1:
                        PrintWarning(msg);
                        break;
                    case 2:
                        PrintError(msg);
                        break;
                }
            }

        }
        /// <summary>
        /// This is to annouce plugins to game-servers.top, this eliminates the need for server owners to list it on their own. Users will also be able to search for servers based on plugins. 
        /// </summary>
        private readonly WebRequests wrAnnoucePlugin = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        private void AnnouncePlugins()
        {
            if ((string)Config["tgsApi"] == "" || Config["tgsApi"] == null) return;
            
                foreach (Plugin plugin in plugins.GetAll())
            {
                wrAnnoucePlugin.EnqueueGet("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&annoucePlugins&pn="+plugin.Name+ "&pt=" +plugin.Title+"&prid="+ plugin.ResourceId + "&pv="+ plugin.Version 
                    , (code, response) => AnnoucePluginCallback(code, response, plugin.Title), this);
                 
            }

        }
        void AnnoucePluginCallback(int code, string response, string title)
        {
            if (response == null || code != 200) { PrintError("Error-" + code + ": There was an error when announcing " + title + " to Game-Servers.top"); return; }
            PrintWarning(title + " has been annouced successfully to Game-Servers.top");
        }
#endregion
    }
}
