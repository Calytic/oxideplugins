using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Rust;
using UnityEngine;

using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("EasyVote", "Exel80", "1.1.4", ResourceId = 2102)]
    [Description("Making voting super easy and smooth!")]
    class EasyVote : RustPlugin
    {
        // Special thanks to MJSU, for all hes efforts what he have done so far!
        // http://oxidemod.org/members/mjsu.99205/

        #region Initializing
        public bool DEBUG = false;
        public StringBuilder RList = new StringBuilder();
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        // {"Claim reward URL", "Get vote status URL, "Server link to chat URL"}
        string[] RustServers = { "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key={0}&steamid={1}", "https://rust-servers.net/api/?object=votes&element=claim&key={0}&steamid={1}", "http://rust-servers.net/server/{0}" };
        string[] TopRustServers = { "http://api.toprustservers.com/api/put?plugin=voter&key={0}&uid={1}", "http://api.toprustservers.com/api/get?plugin=voter&key={0}&uid={1}", "http://toprustservers.com/server/{0}" };
        string[] BeancanIO = { "http://beancan.io/vote/put/{0}/{1}", "http://beancan.io/vote/get/{0}/{1}", "http://beancan.io/server/{0}" };

        private void Loaded()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("EasyVote");
            LoadConfigValues();
            BuildRewardList();

            //If it's a new month wipe the saved votes
            if (storedData.month != DateTime.Now.Month)
            {
                PrintWarning("New month detected. Wiping user votes");
                Interface.GetMod().DataFileSystem.WriteObject("EasyVote.bac", storedData); //Save backup
                storedData = new StoredData();
                Interface.GetMod().DataFileSystem.WriteObject("EasyVote", storedData); // Write wiped data
            }

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ClaimError"] = "Something went wrong! We got <color=red>{0} error</color> from <color=yellow>{1}</color>. Please try again later!",
                ["ClaimReward"] = "You just received your vote reward(s). Enjoy!",
                ["EarnReward"] = "When you are voted. Type <color=cyan>/reward</color> to earn your reward(s)!",
                ["RewardList"] = "<color=cyan>Player reward, when voted</color> <color=orange>{0}</color> <color=cyan>time(s).</color>",
                ["Received"] = "You have received {0}x {1}",
                ["ThankYou"] = "Thank you for voting {0} time(s)",
                ["NoRewards"] = " You do not have any new rewards avaliable \n Please type <color=yellow> /vote </color> and go to the website to vote and receive your reward",
                ["RemeberClaim"] = "You haven't yet claimed your reward from voting server! Use <color=cyan>/reward</color> to claim your reward! \n You have to claim your reward in <color=yellow>24h</color>! Otherwise it will be gone!",
                ["GlobalAnnouncment"] = "<color=yellow>{0}</color><color=cyan> has voted </color><color=yellow>{1}</color><color=cyan> time(s) and just received their rewards. Find out where to vote by typing</color><color=yellow> /vote</color>\n<color=cyan>To see a list of avaliable rewards type</color><color=yellow> /reward list</color>",
                ["money"] = "{0} has been desposited into your account",
                ["rp"] = "You have gained {0} reward points",
                ["addlvl"] = "You have gained {0} level(s)",
                ["addgroup"] = "You have been added to group {0} {1}",
                ["grantperm"] = "You have been given permission {0} {1}",
                ["zlvl-wc"] = "You have gained {0} woodcrafting level(s)",
                ["zlvl-mg"] = "You have gained {0} mining level(s)",
                ["zlvl-s"] = "You have gained {0} skinning level(s)",
                ["zlvl-c"] = "You have gained {0} crafting level(s)"
            }, this);

            //Puts(config.Settings["Annoucment"]);
        }
        #endregion

        #region Annoucment
        void OnPlayerSleepEnded(BasePlayer player)
        {
            // if Annoucment is true, check player status when his SleepEnded.
            if (config.Settings["Annoucment"].ToLower() == "true")
            {
                if (IsEmpty(config.Settings["RustServersID"].ToString())
                    && IsEmpty(config.Settings["RustServersKEY"].ToString()))
                {
                    debug(player, $"Check {player.displayName} vote status from RustServers");

                    string _RustBroadcastServer = String.Format(RustServers[1], config.Settings["RustServersKEY"], player.userID);
                    webrequest.EnqueueGet(_RustBroadcastServer, (code, response) => CheckStatus(code, response, player), this);
                }
                if (IsEmpty(config.Settings["TopRustServersID"].ToString())
                    && IsEmpty(config.Settings["TopRustServersKEY"].ToString()))
                {
                    debug(player, $"Check {player.displayName} vote status from TopRustServers");

                    string _TopRustBroadcastServers = String.Format(TopRustServers[1], config.Settings["TopRustServersKEY"], player.userID);
                    webrequest.EnqueueGet(_TopRustBroadcastServers, (code, response) => CheckStatus(code, response, player), this);
                }
                if (IsEmpty(config.Settings["BeancanID"].ToString())
                    && IsEmpty(config.Settings["BeancanKEY"].ToString()))
                {
                    debug(player, $"Check {player.displayName} vote status from BeancanIO");

                    string _BeancanBroadcastServers = String.Format(BeancanIO[1], config.Settings["BeancanKEY"], player.userID);
                    webrequest.EnqueueGet(_BeancanBroadcastServers, (code, response) => CheckStatus(code, response, player), this);
                }
            }
        }
        #endregion

        #region Commands
        [ChatCommand("vote")]
        void cmdVote(BasePlayer player, string command, string[] args)
        {
            // Making sure that ID or KEY isn't Empty
            if (IsEmpty(config.Settings["RustServersID"].ToString())
                && IsEmpty(config.Settings["RustServersKEY"].ToString()))
                Chat(player, String.Format("<color=silver>" + RustServers[2] + "</color>", config.Settings["RustServersID"]));

            if (IsEmpty(config.Settings["TopRustServersID"].ToString())
                && IsEmpty(config.Settings["TopRustServersKEY"].ToString()))
                Chat(player, String.Format("<color=silver>" + TopRustServers[2] + "</color>", config.Settings["TopRustServersID"]));

            if (IsEmpty(config.Settings["BeancanID"].ToString())
                && IsEmpty(config.Settings["BeancanKEY"].ToString()))
                Chat(player, String.Format("<color=silver>" + BeancanIO[2] + "</color>", config.Settings["BeancanID"]));

            Chat(player, Lang("EarnReward", player.UserIDString));
        }
        [ChatCommand("reward")]
        void cmdReward(BasePlayer player, string command, string[] args)
        {
            checkPlayer(player);
            string _rewardCmd;

            if (args.Length < 1)
                _rewardCmd = "";
            else
                _rewardCmd = args[0];

            switch (_rewardCmd)
            {
                case "list":
                        SendReply(player, RList.ToString());
                    break;
                default:
                    {
                        // Timeout (in milliseconds)
                        var timeout = 5500f;

                        if (IsEmpty(config.Settings["RustServersKEY"].ToString()))
                        {
                            string _RustServer = String.Format(RustServers[0], config.Settings["RustServersKEY"], player.userID);
                            webrequest.EnqueueGet(_RustServer, (code, response) => ClaimReward(code, response, player, "RustServers"), this, null, timeout);
                            debug(player, _RustServer);
                        }
                        if (IsEmpty(config.Settings["TopRustServersKEY"].ToString()))
                        {
                            string _TopRustServers = String.Format(TopRustServers[0], config.Settings["TopRustServersKEY"], player.userID);
                            webrequest.EnqueueGet(_TopRustServers, (code, response) => ClaimReward(code, response, player, "TopRustServers"), this, null, timeout);
                            debug(player, _TopRustServers);
                        }
                        if (IsEmpty(config.Settings["BeancanKEY"].ToString()))
                        {
                            string _Beancan = String.Format(BeancanIO[0], config.Settings["BeancanKEY"], player.userID);
                            webrequest.EnqueueGet(_Beancan, (code, response) => ClaimReward(code, response, player, "BeancanIO"), this, null, timeout);
                            debug(player, _Beancan);
                        }
                    }
                    break;
            }
        }
        #endregion

        #region Reward Handler
        private void RewardHandler(BasePlayer player)
        {
            var info = new PlayerData(player);
            List<int> numberMax = new List<int>();

            // Double-Check that player is in database.
            if (!storedData.Players.ContainsKey(info.id))
                checkPlayer(player);

            // Storing how many time player has voted.
            addVote(player, info);

            // Get how many time player has voted.
            int voted = storedData.Players[info.id].voted;

            // Add rewardNumber to one List
            foreach (KeyValuePair<string, List<string>> kvp in config.Reward)
            {
                int rewardNumber;
                // Remove alphabetic and leave only number.
                if (!int.TryParse(kvp.Key.Replace("vote", ""), out rewardNumber))
                {
                    Puts($"Invalid vote config format \"{kvp.Key}\"");
                    continue;
                }
                numberMax.Add(rewardNumber);
            }

            // Take closest number from rewardNumbers
            int closest = numberMax.Aggregate((x, y) => Math.Abs(x - voted) <= Math.Abs(y - voted) ? x : y);

            // and here the magic happens.
            foreach (KeyValuePair<string, List<string>> kvp in config.Reward)
            {
                if (closest != 0)
                {
                    debug(player, $"Reward Number: {closest} Voted: {voted}");

                    // Loop for all rewards.
                    if (kvp.Key.ToString() == $"vote{closest}")
                    {
                        Chat(player, $"{Lang("ThankYou", player.UserIDString, voted)}");
                        foreach (string reward in kvp.Value)
                        {
                            // Split reward to variable and value.
                            string[] valueSplit = reward.Split(':');
                            string variable = valueSplit[0];
                            string value = valueSplit[1].Replace(" ", "");

                            // Checking variables and run console command.
                            // If variable not found, then try give item.
                            if (config.Variables.ContainsKey(variable))
                            {
                                rust.RunServerCommand(getCmdLine(player, variable, value));
                                Chat(player, $"{Lang(variable, player.UserIDString, value)}");
                                debug(player, $"Ran command {String.Format(variable, value)}");
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    Item itemToReceive = ItemManager.CreateByName(variable, Convert.ToInt32(value));
                                    debug(player, $"Received item {itemToReceive.info.displayName.translated} {value}");
                                    //If the item does not end up in the inventory
                                    //Drop it on the ground for them
                                    if (!player.inventory.GiveItem(itemToReceive, player.inventory.containerMain))
                                        itemToReceive.Drop(player.GetDropPosition(), player.GetDropVelocity());

                                    Chat(player, $"{Lang("Received", player.UserIDString, value, itemToReceive.info.displayName.translated)}");
                                }
                                catch (Exception e) { PrintWarning($"{e}"); }
                            }
                        }
                    }
                }
            }
            if(config.Settings["GlobalAnnouncment"].ToString().ToLower() == "true")
                PrintToChat($"{Lang("GlobalAnnouncment", player.UserIDString, player.displayName, voted)}");
        }
        #endregion

        #region Configuration Defaults
        PluginConfig DefaultConfig()
        {
            var defaultConfig = new PluginConfig
            {
                Settings = new Dictionary<string, string>
                {
                    { PluginSettings.Prefix, "<color=cyan>[EasyVote]</color>" },
                    { PluginSettings.Annoucment, "true" },
                    { PluginSettings.GlobalAnnouncment, "true" },
                    { PluginSettings.RustServersID, "" },
                    { PluginSettings.RustServersKEY, "" },
                    { PluginSettings.TopRustServersID, "" },
                    { PluginSettings.TopRustServersKEY, "" },
                    { PluginSettings.BeancanID, "" },
                    { PluginSettings.BeancanKEY, "" }
                },
                Reward = new Dictionary<string, List<string>>
                {
                    { "vote1", new List<string>() { "supply.signal: 1" } },
                    { "vote3", new List<string>() { "supply.signal: 1", "money: 250" } },
                    { "vote6", new List<string>() { "supply.signal: 1", "money: 500", "addlvl: 1" } }
                },
                Variables = new Dictionary<string, string>
                {
                    ["money"] = "eco.c deposit {playerid} {value}",
                    ["rp"] = "sr add {playername} {value}",
                    ["addlvl"] = "xp addlvl {playername} {value}",
                    ["addgroup"] = "addgroup {playerid} {value} {value2}",
                    ["grantperm"] = "grantperm {playerid} {value} {value2}",
                    ["zlvl-wc"] = "zlvl {playername} WC +{value}",
                    ["zlvl-mg"] = "zlvl {playername} MG +{value}",
                    ["zlvl-s"] = "zlvl {playername} S +{value}"
                }
            };
            return defaultConfig;
        }
        #endregion

        #region Configuration Setup
        private bool configChanged;
        private PluginConfig config;

        class PluginSettings
        {
            public const string Prefix = "Prefix";
            public const string Annoucment = "Annoucment";
            public const string GlobalAnnouncment = "GlobalAnnouncment";
            public const string RustServersID = "RustServersID";
            public const string RustServersKEY = "RustServersKEY";
            public const string TopRustServersID = "TopRustServersID";
            public const string TopRustServersKEY = "TopRustServersKEY";
            public const string BeancanID = "BeancanID";
            public const string BeancanKEY = "BeancanKEY";
        }

        class PluginConfig
        {
            public Dictionary<string, string> Settings { get; set; }
            public Dictionary<string, List<string>> Reward { get; set; }
            public Dictionary<string, string> Variables { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
        }

        void LoadConfigValues()
        {
            config = Config.ReadObject<PluginConfig>();
            var defaultConfig = DefaultConfig();
            Merge(config.Settings, defaultConfig.Settings);
            Merge(config.Reward, defaultConfig.Reward, true);
            Merge(config.Variables, defaultConfig.Variables);

            if (!configChanged) return;
            PrintWarning("Configuration file updated.");
            Config.WriteObject(config);
        }

        void Merge<T1, T2>(IDictionary<T1, T2> current, IDictionary<T1, T2> defaultDict, bool rewardFilter = false)
        {
            foreach (var pair in defaultDict)
            {
                if (rewardFilter) continue;
                if (current.ContainsKey(pair.Key)) continue;
                current[pair.Key] = pair.Value;
                configChanged = true;
            }
            var oldPairs = defaultDict.Keys.Except(current.Keys).ToList();
            foreach (var oldPair in oldPairs)
            {
                if (rewardFilter) continue;
                configChanged = true;
            }
        }
        #endregion

        #region Webrequests
        void ClaimReward(int code, string response, BasePlayer player, string url)
        {
            debug(player, $"Code: {code}, Response: {response}");

            if (response == null || code != 200)
            {
                PrintWarning("Error: {0} - Couldn't get an answer for {1} ({2})", code, player.displayName, url);
                Chat(player, $"{Lang("ClaimError", player.UserIDString, code, url)}");
                return;
            }

            if (response == "1")
                RewardHandler(player);
            else
                Chat(player, $"{Lang("NoRewards", player.UserIDString)}");
        }
        void CheckStatus(int code, string response, BasePlayer player)
        {
            debug(player, $"Code: {code}, Response: {response}");

            if (response == null || code != 200)
                return;

            if (response == "1")
                Chat(player, Lang("RemeberClaim", player.UserIDString));
        }
        #endregion

        #region Storing
        class StoredData
        {
            public Dictionary<string, PlayerData> Players = new Dictionary<string, PlayerData>();
            public int month = DateTime.Now.Month;
            public StoredData() { }

        }
        class PlayerData
        {
            public string id;
            public int voted; // Claimed votes.

            public PlayerData() { }

            public PlayerData(BasePlayer player)
            {
                id = player.UserIDString;
                voted = 0;
            }
            public void AddVote(int numbr)
            {
                voted = numbr;
            }
        }
        StoredData storedData;
        #endregion

        #region Helper
        public void BuildRewardList()
        {
            var txt = new Dictionary<string, List<string>>();

            // Load & Save config Reward "Vote" to one txt list.
            foreach (KeyValuePair<string, List<string>> kvp in config.Reward)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (!txt.ContainsKey(kvp.Key))
                        txt.Add(kvp.Key, new List<string>());

                    txt[kvp.Key].Add(kvp.Value[i]);
                }
            }

            // Create "StringBuilder"
            foreach (var NKey in txt.Keys)
            {
                int voteNumber;
                if (!int.TryParse(NKey.Replace("vote", ""), out voteNumber))
                {
                    PrintWarning($"Invalid vote config format \"{NKey}\"");
                    continue;
                }
                RList.Append(Lang("RewardList", null, voteNumber)).AppendLine();
                for (int i = 0; i < txt[NKey].Count(); i++)
                {
                    RList.Append(" - " + txt[NKey][i]).AppendLine();
                }
            }
        }
        public void Chat(BasePlayer player, string str)
        {
            SendReply(player, $"{config.Settings["Prefix"]} " + str);
        }
        public void debug(BasePlayer player, string msg)
        {
            if (DEBUG)
                Puts($"[Debug] {player.displayName} - {msg}");
        }
        public bool IsEmpty(string s)
        {
            if (s != String.Empty) return true;
            return false;
        }
        void checkPlayer(BasePlayer player)
        {
            var info = new PlayerData(player);
            if (!storedData.Players.ContainsKey(info.id))
            {
                storedData.Players.Add(info.id, info);
                Interface.GetMod().DataFileSystem.WriteObject("EasyVote", storedData);
            }
        }
        void addVote(BasePlayer player, PlayerData info)
        {
            if (storedData.Players.ContainsKey(info.id))
            {
                int voted = storedData.Players[info.id].voted;
                storedData.Players[info.id].AddVote(voted + 1);
                Interface.GetMod().DataFileSystem.WriteObject("EasyVote", storedData);
            }
        }
        private string getCmdLine(BasePlayer player, string str, string value)
        {
            var output = String.Empty;
            string playerid = player.UserIDString;
            string playername = player.displayName;

            // Checking if value contains => -
            if (!value.Contains('-'))
                output = config.Variables[str].ToString()
                    .Replace("{playerid}", playerid)
                    .Replace("{playername}", '"' + playername + '"')
                    .Replace("{value}", value);
            else
            {
                string[] splitValue = value.Split('-');
                output = config.Variables[str].ToString()
                    .Replace("{playerid}", playerid)
                    .Replace("{playername}", '"' + playername + '"')
                    .Replace("{value}", splitValue[0])
                    .Replace("{value2}", splitValue[1]);
            }
            return $"{output}";
        }
        #endregion
    }
}
