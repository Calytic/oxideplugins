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
    [Info("VoteChecker", "Pho3niX90", "2.2.2", ResourceId = 1216)]
    class VoteChecker : RustPlugin
    {
        #region [CLASSES]
        static class Constants
        {
            public const string PLATFORM = "rust"; //Game-servers.top doesn't need a platform string, cause were awesome >8-)
            public const string SERVICE = "listforge"; //nope not this as well
            public const bool DEBUG = false;
        }
        #endregion

        #region [LISTS]
        int timesVoted;
        private Dictionary<ulong, DateTime> Cooldowns = new Dictionary<ulong, DateTime>();
        private Collection<RewardItem> Rewards = new Collection<RewardItem>();
        public Collection<LastVote> Users = new Collection<LastVote>();
        private Dictionary<string, string> shortnameDictionary;
        public class RewardItem
        {
            public RewardItem(string itemName, int votesRequired, int itemAmount)
            {
                this.itemName = itemName;
                this.votesRequired = votesRequired;
                this.itemAmount = itemAmount;
            }

            public string itemName { get; set; }
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
                new RewardItem("Pistol Bullet",           -1,      100),
                new RewardItem("Semi-automatic Pistol",   1,      1)
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
            SaveConfig();
        }
        #endregion

        #region [HOOKS]
        void OnServerInitialized()
        {
            InitializeTable();
        }

        private void InitializeTable()
        {
            shortnameDictionary.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                shortnameDictionary.Add(itemdef.displayName.english.ToString().ToLower().Trim(), itemdef.shortname.ToString());
            }
        }
        void Loaded()
        {
            LoadRewards();
            //LoadMessages();
            AnnouncePlugins();

            if (shortnameDictionary == null)
            {
                shortnameDictionary = new Dictionary<string, string>();
            }
            if (Rewards.Count < 1)
            {
                PrintWarning("No items was loaded, will now load defaults");
                LoadDefaultRewards();
                PrintWarning(Rewards.Count + " default rewards loaded");
            }
            LoadVotes();
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (player.displayName.ToLower() == "server") return;


            // set cooldown
            TimeSpan time = new TimeSpan(0, 0, 15, 0);
            DateTime combined = DateTime.Now.Add(time);
            if (!Cooldowns.ContainsKey(player.userID)) { Cooldowns.Add(player.userID, DateTime.Now); }
            //

            bool autoGive;
            if (!bool.TryParse(Config["autoGive"].ToString(), out autoGive)) { autoGive = true; }
            if (!autoGive) { return; }
            GetRewardDelayed(player);
            Cooldowns[player.userID] = combined;
        }
        #endregion

        #region [CHAT COMMANDS]
        [ChatCommand("addreward")]
        private void ChatCmd_AddReward(BasePlayer player, string cmd, string[] args)
        {


            if (player.net.connection.authLevel < 2)
            {
                PrintToChat(player, "Only admins are allowed to use this function");
                return;
            }
            var tmpItemName = "";
            int tmpItemAmnt = 0;
            int tmpItemVtNeed = 0;
            switch (args.Length)
            {

                case 0:
                    PrintToChat(player, "/addreward itemname rewardamount votecountneeded");
                    break;
                case 3:
                    tmpItemName = args[0];
                    if (!int.TryParse(args[1], out tmpItemAmnt)) { }
                    if (!int.TryParse(args[2], out tmpItemVtNeed)) { }
                    break;
                case 4:
                    tmpItemName = args[0] + " " + args[1];
                    if (!int.TryParse(args[2], out tmpItemAmnt)) { }
                    if (!int.TryParse(args[3], out tmpItemVtNeed)) { }
                    break;
                default:
                    PrintToChat(player, "Incorrect usage");
                    return;
            }

            string value;
            if (shortnameDictionary.TryGetValue(tmpItemName.ToLower().Trim(), out value))
            {
                Rewards.Add(new RewardItem(tmpItemName, tmpItemVtNeed, tmpItemAmnt));
                PrintToChat(player, "Item " + Capitalise(tmpItemName) + " was added with a quanitity of " + tmpItemAmnt + " and will be given at " + tmpItemVtNeed + " votes");
                SaveRewards();
            }
            else
            {
                PrintToChat(player, "No such item -" + tmpItemName + "- , please use /itemlist to see available items");
                Puts("No such item -" + tmpItemName + "- , please use /itemlist to see available items");
            }
        }
        [ChatCommand("getreward")]
        private void GetMyVoteReward(BasePlayer player, string cmd) { GetRewardDelayed(player); }
        private readonly WebRequests webRequestsAddress = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        private readonly WebRequests webRequestsApi = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
        [ChatCommand("rewardconf")]
        private void ChatCmd_Config(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 2) { PrintToChat(player, "Only admins are allowed to use this function"); return; }
            if (args == null || args.Length == 0)
            {
                PrintToChat(player, "TGS API: " + ((string)Config["tgsApi"] == "" ? "No api set, please set ASAP" : (string)Config["tgsApi"]));
                PrintToChat(player, "Listforge API: " + ((string)Config["serverApi"] == "" ? "No api set, please set ASAP" : (string)Config["serverApi"]));
                PrintToChat(player, "ServerID: " + Config["serverId"]);
                PrintToChat(player, "Reward Type: " + Config["trackingInterval"] + Config["trackingType"] + "");
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

                        if (player.displayName.ToLower() == "server") return;
                        var playerId = player.userID;
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
        private void ChatCmd_ClearRewards(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel < 2) { PrintToChat(player, "Only admins are allowed to use this function"); return; }
            Rewards.Clear();
            SaveRewards();
            PrintToChat(player, "Rewards file cleared. Please add some rewards with /addreward itemname itemamount votecount");
        }
        [ChatCommand("itemlist")]
        private void getItemList(BasePlayer player, string cmd)
        {

            foreach (var itemList in shortnameDictionary.Keys)
            {
                PrintToChat(player, Capitalise(itemList));
            }
        }
        [ChatCommand("voterewards")]
        private void ChatCmd_Rewards(BasePlayer player, string cmd)
        {
            PrintToChat(player, "Get rewards for daily votes");
            var RewardsSorted = Rewards.OrderBy(a => a.votesRequired);
            var tmplastVote = Users.Where(d => d.steamid == player.userID).FirstOrDefault();
            int thisvote = (tmplastVote.lastvote == null) ? tmplastVote.lastvote : 0;
            int nextVote = thisvote + 1;
            foreach (var voteReward in RewardsSorted) // Loop through List with foreach.
            {
                var resource = voteReward.itemName;
                var votesNeeded = voteReward.votesRequired; //int Votes
                int resourceAmount = voteReward.itemAmount; //int Amount
                if (thisvote == votesNeeded)
                { PrintToChat(player, "LAST Reward: " + resourceAmount + " * " + resource + " when you have reached " + votesNeeded + " votes"); }
                else
                if (nextVote == votesNeeded || votesNeeded == -1)
                {
                    if (votesNeeded == -1)
                    { PrintToChat(player, "NEXT Reward: " + resourceAmount + " * " + resource + " for every vote"); }
                    else
                    { PrintToChat(player, "NEXT Reward: " + resourceAmount + " * " + resource + " when you have reached " + votesNeeded + " votes"); }
                }
                else { PrintToChat(player, "Reward: " + resourceAmount + " * " + resource + " when you have reached " + votesNeeded + " votes"); }
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
        void GetRewardDelayed(BasePlayer player)
        {
            PrintWarning("0");
            if (!Cooldowns.ContainsKey(player.userID)) { Cooldowns.Add(player.userID, DateTime.Now); }
            DateTime Date;
            PrintWarning("1");
            Cooldowns.TryGetValue(player.userID, out Date);
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
            Cooldowns[player.userID] = combined; // we are setting the cooldown.
        }
        void GetRewardsForThisPlayer(BasePlayer player)
        {
            timesVoted = 0;
            if (player.displayName.ToLower() == "server") return;
            var playerId = player.userID.ToString();
            //Game-Servers.top
            if ((string)Config["tgsApi"] != "" && Config["tgsApi"] != null)
            {
                webRequests.EnqueueGet("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&interval=" + Config["trackingInterval"] + "&period=" + Config["trackingType"] + "&steamid=" + playerId + "&app", (code, response) => WebRequestCallbackApi(code, response, player), this);
                Debug(1, "TGS Passing: http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&interval=" + Config["trackingInterval"] + "&period=" + Config["trackingType"] + "&steamid=" + playerId + "&app");
            }
            //listforge
            if ((string)Config["serverApi"] != "" && Config["serverApi"] != null)
            {
                webRequests.EnqueueGet("http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM, (code, response) => WebRequestCallback(code, response, player), this);
                Debug(1, "LF Passing: http://api.cyberscene.co.za/listforge/votechecker.php?steamid=" + playerId + "&ver=" + this.Version + "&api=" + Config["serverApi"] + "&mode=" + Config["trackingType"] + "&interval=" + Config["trackingInterval"] + "&platform=" + Constants.PLATFORM);
            }
        }
        void WebRequestCallbackAddress(int code, string response, BasePlayer player)
        {
            if (response == null || code != 200) { Puts("error " + code + ": Couldn't get an answer from Game-Servers.top for " + player.displayName); return; }

            Config["tgsAddress"] = "http://game-servers.top/server/" + response;
            PrintToChat(player, "Game-Server address set to " + (string)Config["tgsAddress"]);
            PrintWarning("Your server address has been saved as " + response + ", setup complete for Game-Servers.top");
            SaveConfig();
        }
        void WebRequestCallbackApi(int code, string response, BasePlayer player)
        {
            int tmpVotes = 0;
            if (response == null || code != 200) { Puts("error" + code + ": Couldn't get an answer from Game-Servers.top for " + player.displayName); return; }

            if (!int.TryParse(response, out tmpVotes)) { PrintError("Game-Servers.top Error: '" + response + "' - " + player.displayName + " (" + player.userID + ") didn't received their reward."); }
            Debug(1, "Game-Servers votes is " + tmpVotes + " for player " + player.displayName);
            timesVoted += tmpVotes;
            if ((string)Config["serverApi"] == "" || Config["serverApi"] == null)
            {
                giveItems(player, timesVoted);
                SaveVotes();
            }
        }
        void WebRequestCallback(int code, string response, BasePlayer player)
        {
            int tmpVotes = 0;
            if (response == null || code != 200) { Puts("error" + code + ": Couldn't get an answer from Cyberscene for " + player.displayName); return; }

            if (!int.TryParse(response, out tmpVotes)) { PrintError("Cyberscene Error: '" + response + "' - " + player.displayName + " (" + player.userID + ") didn't received their reward."); }
            Debug(1, "Listforge votes is " + tmpVotes + " for player " + player.displayName);
            timesVoted += tmpVotes;

            timer.Once(1, () => giveItems(player, timesVoted));
            SaveVotes();
        }

        void giveItems(BasePlayer player, int voteCount)
        {
            Debug(1, "1");
            Debug(1, "Votecount passed to giveitems is " + voteCount);
            var playerName = player.displayName;
            Debug(1, "2");
            var playerId = player.userID;
            Debug(1, "3");



            var tmplastVote = Users.Where(d => d.steamid == playerId).FirstOrDefault();

            if (tmplastVote == null)
            {
                Debug(1, "12");
                Debug(1, "LastVote is null for user, recreate.");
                Users.Add(new LastVote((ulong)playerId, 0));
                SaveVotes();

                Debug(1, "13");
            }
            else
            {
                Debug(1, "14");
                if (tmplastVote.lastvote > voteCount) { tmplastVote.lastvote = 0; }; //this means to interval has reset
            }
            Debug(1, "The lastvotecount after checks is " + ((tmplastVote == null) ? "isnull" : tmplastVote.lastvote.ToString()));
            Debug(1, "14.5");
            tmplastVote = Users.Where(d => d.steamid == playerId).FirstOrDefault();

            Debug(1, "The lastvotecount after select is " + tmplastVote.lastvote);


            Debug(1, "15");
            int LastVote = tmplastVote.lastvote;

            if (voteCount == 0 || voteCount == LastVote)
            {
                Debug(1, "4");
                PrintToChat(player, "You have no new rewards, please vote for our server to receive rewards.");
                if ((string)Config["tgsApi"] != null || (string)Config["tgsApi"] != "")
                {
                    Debug(1, "5");
                    PrintToChat(player, (string)Config["tgsAddress"]);
                }
                if ((string)Config["serverId"] != null || (string)Config["serverId"] != "")
                {
                    Debug(1, "6");
                    PrintToChat(player, "http://rust-servers.net/server/" + Config["serverId"] + "/");
                }
                return;
            }

            Debug(1, "16");
            var RewardsLimited = from p in Rewards.ToList() where (p.votesRequired > LastVote && p.votesRequired <= voteCount) || p.votesRequired == -1 select p;
            Debug(1, "16.5");
            Debug(1, "RewardsLimited contains " + RewardsLimited.Count() + " rewards and isnull? " + (RewardsLimited == null));
            Debug(1, "16.7");
            foreach (var voteReward in RewardsLimited)
            {
                Debug(1, "17");
                var votesNeeded = voteReward.votesRequired;
                Debug(1, "18");
                int resourceAmount = voteReward.itemAmount;
                Debug(1, "19");
                var definition = ItemManager.FindItemDefinition(shortnameDictionary[voteReward.itemName.ToLower().Trim()]);
                Debug(1, "20");
                int amountToGive = resourceAmount;
                Debug(1, "21");
                Debug(1, "Should we give? amnt:" + amountToGive + " vtneed:" + votesNeeded + " vtcnt:" + voteCount + " lstvote: " + LastVote);
                if (amountToGive > 0 && LastVote < voteCount)
                {
                    Debug(1, "22");
                    int remainderToGive = amountToGive;

                    Debug(1, "23");
                    while (remainderToGive > 0)
                    {
                        Debug(1, "24");
                        var maxStackSize = 1000;
                        Debug(1, "25");
                        var stackToGive = maxStackSize;

                        if (remainderToGive < maxStackSize)
                        {
                            stackToGive = remainderToGive;
                        }

                        var isBP = false;

                        player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, stackToGive, isBP), (ItemContainer)player.inventory.containerMain);
                        remainderToGive = remainderToGive - maxStackSize;
                    }
                    PrintToChat(player, "Thanks for voting " + voteCount + " times, you have received " + amountToGive + " " + voteReward.itemName);
                }


            }

            if (tmplastVote != null) { tmplastVote.lastvote = voteCount; Debug(1, "Storing users last vote, "); }// else { Users.Add(new LastVote(playerId, voteCount)); }
            //possible fix v
            foreach (var e in Users.Where(a => a.steamid == (ulong)player.userID))
            {
                e.lastvote = voteCount;
            }
            //possible fix ^

            timesVoted = 0;
        }
        private string Capitalise(string word)
        {
            var finalText = "";
            finalText = Char.ToUpper(word[0]).ToString();
            var spaceFound = 0;
            for (var i = 1; i < word.Length; i++)
            {
                if (word[i] == ' ')
                {
                    spaceFound = i + 1;
                }
                if (i == spaceFound)
                {
                    finalText = finalText + Char.ToUpper(word[i]).ToString();
                }
                else finalText = finalText + word[i].ToString();
            }
            return finalText;
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
                wrAnnoucePlugin.EnqueueGet("http://game-servers.top/api/query.php?apikey=" + Config["tgsApi"] + "&annoucePlugins&pn=" + plugin.Name + "&pt=" + plugin.Title + "&prid=" + plugin.ResourceId + "&pv=" + plugin.Version
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
