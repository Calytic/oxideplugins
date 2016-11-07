using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Vote For Money", "Frenk92", "0.5.0", ResourceId = 2086)]
    class VoteForMoney : RustPlugin
    {
        [PluginReference]
        Plugin Economics;
        [PluginReference]
        Plugin ServerRewards;
        [PluginReference]
        Plugin Kits;

        const string permAdmin = "voteformoney.admin";
        const string site1 = "Rust-Servers";
        const string site2 = "TopRustServers";
        const string site3 = "BeancanIO";
        const string link1 = "http://rust-servers.net/server/";
        const string link2 = "http://toprustservers.com/server/";
        const string link3 = "http://beancan.io/server/";
        public bool edit = false;

        #region Config
        string rustServersKey = "";
        string rustServersID = "";
        string topRustKey = "";
        string topRustID = "";
        string beancanKey = "";
        string beancanID = "";
        string voteType = "day";
        int voteInterval = 1;
        bool useRP = false;
        bool useEconomics = true;
        bool useKits = false;
        string prefix = "<color=#808000ff><b>VoteForMoney:</b></color>";
        Dictionary<string, string> kits = new Dictionary<string, string> { { "default", "" } };
        Dictionary<string, string> money = new Dictionary<string, string> { { "default", "250" } };
        Dictionary<string, string> rp = new Dictionary<string, string> { { "default", "30" } };

        string configVersion = "0.2.0";

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file.");
            Config.Clear();
        }

        void LoadConfigData()
        {
            var version = (string)Config["Version"];
            //Load config
            rustServersKey = (string)ReadConfig("Rust-Servers - Api Key", rustServersKey);
            rustServersID = (string)ReadConfig("Rust-Servers - Server ID", rustServersID);
            topRustKey = (string)ReadConfig("TopRustServers - Api Key", topRustKey);
            topRustID = (string)ReadConfig("TopRustServers - Server ID", topRustID);
            beancanKey = (string)ReadConfig("BeancanIO - Api Key", beancanKey);
            beancanID = (string)ReadConfig("BeancanIO - Server ID", beancanID);
            voteType = (string)ReadConfig("Vote Type", voteType);
            voteInterval = Convert.ToInt16(ReadConfig("Vote Interval", voteInterval));
            useRP = Convert.ToBoolean(ReadConfig("Use RP", useRP));
            useEconomics = Convert.ToBoolean(ReadConfig("Use Economics", useEconomics));
            useKits = Convert.ToBoolean(ReadConfig("Use Kits", useKits));
            prefix = (string)ReadConfig("Prefix", prefix);

            var oldMoney = "";
            var oldRP = "";
            var oldKit = "";
            if(version == null || version == "0.1.0")
            {
                oldMoney = (string)ReadConfig("Money", oldMoney);
                oldRP = (string)ReadConfig("RP", oldRP);
                oldKit = (string)ReadConfig("Kit", oldKit);
            }
            else
            {
                money = ConvertToDictionary(ReadConfig("Money", money), money);
                rp = ConvertToDictionary(ReadConfig("RP", rp), rp);
                kits = ConvertToDictionary(ReadConfig("Kits", kits), kits);
            }

            if(version == null || version != configVersion)
            {
                PrintWarning("Configuration is outdate. Update in progress...");
                Config.Clear();    

                SetConfig("Rust-Servers - Api Key", rustServersKey);
                SetConfig("Rust-Servers - Server ID", rustServersID);
                SetConfig("TopRustServers - Api Key", topRustKey);
                SetConfig("TopRustServers - Server ID", topRustID);
                SetConfig("BeancanIO - Api Key", beancanKey);
                SetConfig("BeancanIO - Server ID", beancanID);
                SetConfig("Vote Type", voteType);
                SetConfig("Vote Interval", voteInterval);
                SetConfig("Use Economics", useEconomics);
                SetConfig("Use RP", useRP);
                SetConfig("Use Kits", useKits);
                SetConfig("Prefix", prefix);

                if(version == null || version == "0.1.0")
                {
                    money = new Dictionary<string, string> { { "default", oldMoney } };
                    SetConfig("Money", money);
                    rp = new Dictionary<string, string> { { "default", oldRP } };
                    SetConfig("RP", rp);
                    kits = new Dictionary<string, string> { { "default", oldKit } };
                    SetConfig("Kits", kits);
                }
                else
                {
                    SetConfig("Money", money);
                    SetConfig("RP", rp);
                    SetConfig("Kits", kits);
                }

                SetConfig("Version", configVersion);
            }
        }

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>            {
                ["NoAnswer"] = "No answer from {0}. Try later.",
                ["AlreadyVoted"] = "You have already voted on {0}.",
                ["NextVote"] = "Next Vote: {0}",
                ["NotVoted"] = "You have not voted yet on {0}.\nLink to vote: {1}{2}",
                ["Thanks"] = "Thanks for voted on {0}",
                ["RewardCoins"] = "Coins reward: {0}",
                ["RewardRP"] = "RP reward: {0}",
                ["RewardXP"] = "XP reward: {0}",
                ["RewardLVL"] = "Level reward: {0}",
                ["RewardKit"] = "Kit rewarded.",
                ["EditID"] = "{0} - Server ID edited.",
                ["EditKey"] = "{0} - Api Key edited.",
                ["SiteDisabled"] = "{0} disabled",
                ["AddMoney"] = "Money reward \"{0}\" was added.",
                ["RemoveMoney"] = "Money reward \"{0}\" was removed.",
                ["AddRP"] = "RP reward \"{0}\" was added.",
                ["RemoveRP"] = "RP reward \"{0}\" was removed.",
                ["AddKit"] = "Kit reward \"{0}\" was added.",
                ["RemoveKit"] = "Kit reward \"{0}\" was removed.",
                ["NotExist"] = "\"{0}\" doesn't exist in config.",
                ["NotExistGroup"] = "Group \"{0}\" doesn't exist.",
                ["NotExistKit"] = "Kit \"{0}\" doesn't exist.",
                ["ErrorNumbers"] = "Error. Insert only numbers.",
                ["EditType"] = "Vote type edited in: {0}",
                ["ErrorType"] = "Error. Only 'day' or 'hour'.",
                ["EditInterval"] = "Vote interval edited in: {0}",
                ["EditUseEco"] = "Use Economics edited in: {0}",
                ["EditUseRP"] = "Use RP edited in: {0}",
                ["EditUseKits"] = "Use Kits edited in: {0}",
                ["ErrorBool"] = "Error. Only 'true' or 'false'.",
                ["Help"] = "\n============== VOTE HELP =============\n/vote <money|rp|kit> true/false - to use <Economics|RP|Kits> or not.\n/vote <money|rp|kit> add \"GROUP\" \"AMOUNT\" - to add a group for a different reward.\n/vote <money|rp|kit> remove \"GROUP\" - to remove a group.\n/vote type day/hour - edit vote type.\n/vote interval AMOUNT - edit vote interval.\n/vote <rservers|toprust|beancan> <id \"SERVERID\"|key \"APIKEY\"> - edit <Rust-Servers|TopRustServers|BeancanIO> <ID|ApiKey>.\n/vote <rservers|toprust|beancan> false - disable <Rust-Servers|TopRustServers|BeancanIO>.\n============== VOTE HELP =============",
            }, this);
        }
        #endregion

        #region Data
        public Collection<PlayerVote> Users = new Collection<PlayerVote>();
        public class PlayerVote
        {
            public ulong UserId { get; set; }
            public string Name { get; set; }
            public Dictionary<string, SitesVote> Sites;

            public PlayerVote(ulong UserId, string Name)
            {
                this.UserId = UserId;
                this.Name = Name;
                Sites = new Dictionary<string, SitesVote>();
            }
        }

        public class SitesVote
        {
            public int Votes { get; set; }
            public string ExpDate { get; set; }
            public string Claimed { get; set; }

            public SitesVote(int Votes, string ExpDate, string Claimed)
            {
                this.Votes = Votes;
                this.ExpDate = ExpDate;
                this.Claimed = Claimed;
            }
        }

        private void LoadData() { Users = Interface.GetMod().DataFileSystem.ReadObject<Collection<PlayerVote>>("VoteForMoney"); }
        private void SaveData() { Interface.Oxide.DataFileSystem.WriteObject("VoteForMoney", Users); }
        #endregion

        #region Hooks
        void Init()
        {
            LoadDefaultMessages();

            permission.RegisterPermission(permAdmin, this);
        }

        void Loaded()
        {
            LoadData();
            LoadConfigData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            GetRequest(player);
        }
        #endregion

        #region Commands
        [ChatCommand("vote")]
        private void cmdVote(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0 || args == null)
            {
                GetRequest(player);
                return;
            }

            if(HasPermission(player.UserIDString, permAdmin))
            {
                edit = true;

                try
                {
                    switch (args[0])
                    {
                        case "help":
                            {
                                MessageChat(player, Lang("Help", player.UserIDString));
                                break;
                            }
                        case "money":
                            {
                                bool flag;
                                if (Boolean.TryParse(args[1], out flag))
                                {
                                    useEconomics = flag;
                                    SetConfig("Use Economics", useEconomics);
                                    MessageChat(player, Lang("EditUseEco", player.UserIDString, flag));
                                    break;
                                }

                                int n;
                                bool isNumber = int.TryParse(args[3], out n);
                                if (!isNumber)
                                {
                                    MessageChat(player, Lang("ErrorNumbers", player.UserIDString));
                                    break;
                                }
                                if (args[1] == "add")
                                {
                                    if (permission.GroupExists(args[2]))
                                    {
                                        money.Add(args[2], args[3]);
                                        MessageChat(player, Lang("AddMoney", player.UserIDString, args[2]));
                                    }
                                    else
                                    {
                                        MessageChat(player, Lang("NotExistGroup", player.UserIDString, args[2]));
                                        break;
                                    }
                                }
                                else if (args[1] == "remove")
                                {
                                    if (money.ContainsKey(args[2]))
                                    {
                                        money.Remove(args[2]);
                                        MessageChat(player, Lang("RemoveMoney", player.UserIDString, args[2]));
                                    }
                                    else
                                        MessageChat(player, Lang("NotExist", player.UserIDString, args[2]));
                                }
                                SetConfig("Money", money);
                                break;
                            }
                        case "rp":
                            {
                                bool flag;
                                if (Boolean.TryParse(args[1], out flag))
                                {
                                    useRP = flag;
                                    SetConfig("Use RP", useRP);
                                    MessageChat(player, Lang("EditUseRP", player.UserIDString, flag));
                                    break;
                                }

                                int n;
                                bool isNumber = int.TryParse(args[3], out n);
                                if (!isNumber)
                                {
                                    MessageChat(player, Lang("ErrorNumbers", player.UserIDString));
                                    break;
                                }
                                if (args[1] == "add")
                                {
                                    if (permission.GroupExists(args[2]))
                                    {
                                        rp.Add(args[2], args[3]);
                                        MessageChat(player, Lang("AddRP", player.UserIDString, args[2]));
                                    }
                                    else
                                    {
                                        MessageChat(player, Lang("NotExistGroup", player.UserIDString, args[2]));
                                        break;
                                    }
                                }
                                else if (args[1] == "remove")
                                {
                                    if (rp.ContainsKey(args[2]))
                                    {
                                        rp.Remove(args[2]);
                                        MessageChat(player, Lang("RemoveRP", player.UserIDString, args[2]));
                                    }
                                    else
                                        MessageChat(player, Lang("NotExist", player.UserIDString, args[2]));
                                }
                                SetConfig("RP", rp);
                                break;
                            }
                        case "kit":
                            {
                                bool flag;
                                if (Boolean.TryParse(args[1], out flag))
                                {
                                    useKits = flag;
                                    SetConfig("Use Kits", useKits);
                                    MessageChat(player, Lang("EditUseKits", player.UserIDString, flag));
                                    break;
                                }

                                if (args[1] == "add")
                                {
                                    if (permission.GroupExists(args[2]))
                                    {
                                        if (!Convert.ToBoolean(Kits?.Call("isKit", args[3])))
                                        {
                                            MessageChat(player, Lang("NotExistKit", player.UserIDString, args[3]));
                                            break;
                                        }
                                        kits.Add(args[2], args[3]);
                                        MessageChat(player, Lang("AddKit", player.UserIDString, args[2]));
                                    }
                                    else
                                    {
                                        MessageChat(player, Lang("NotExistGroup", player.UserIDString, args[2]));
                                        break;
                                    }
                                }
                                else if (args[1] == "remove")
                                {
                                    if (kits.ContainsKey(args[2]))
                                    {
                                        kits.Remove(args[2]);
                                        MessageChat(player, Lang("RemoveKit", player.UserIDString, args[2]));
                                    }
                                    else
                                        MessageChat(player, Lang("NotExist", player.UserIDString, args[2]));
                                }
                                SetConfig("Kits", kits);
                                break;
                            }
                        case "type":
                            {
                                if (args[1] != "day" && args[1] != "hour")
                                {
                                    MessageChat(player, Lang("ErrorType", player.UserIDString));
                                    break;
                                }
                                voteType = args[1];
                                SetConfig("Vote Type", voteType);
                                MessageChat(player, Lang("EditType", player.UserIDString, voteType));
                                break;
                            }
                        case "interval":
                            {
                                int n;
                                bool isNumber = int.TryParse(args[1], out n);
                                if (!isNumber)
                                {
                                    MessageChat(player, Lang("ErrorNumbers", player.UserIDString));
                                    break;
                                }
                                voteInterval = n;
                                SetConfig("Vote Interval", voteInterval);
                                MessageChat(player, Lang("EditInterval", player.UserIDString, voteInterval));
                                break;
                            }
                        case "rservers":
                            {
                                switch(args[1])
                                {
                                    case "id":
                                        {
                                            rustServersID = args[2];
                                            SetConfig("Rust-Servers - Server ID", rustServersID);
                                            MessageChat(player, Lang("EditID", player.UserIDString, site1));
                                            break;
                                        }
                                    case "key":
                                        {
                                            rustServersKey = args[2];
                                            SetConfig("Rust-Servers - Api Key", rustServersKey);
                                            MessageChat(player, Lang("EditKey", player.UserIDString, site1));
                                            break;
                                        }
                                    case "false":
                                        {
                                            rustServersID = "";
                                            SetConfig("Rust-Servers - Server ID", rustServersID);
                                            rustServersKey = "";
                                            SetConfig("Rust-Servers - Api Key", rustServersKey);
                                            MessageChat(player, Lang("SiteDisabled", player.UserIDString, site1));
                                            break;
                                        }
                                }

                                break;
                            }
                        case "toprust":
                            {
                                switch (args[1])
                                {
                                    case "id":
                                        {
                                            topRustID = args[2];
                                            SetConfig("TopRustServers - Server ID", topRustID);
                                            MessageChat(player, Lang("EditID", player.UserIDString, site2));
                                            break;
                                        }
                                    case "key":
                                        {
                                            topRustKey = args[2];
                                            SetConfig("TopRustServers - Api Key", topRustKey);
                                            MessageChat(player, Lang("EditKey", player.UserIDString, site2));
                                            break;
                                        }
                                    case "false":
                                        {
                                            topRustID = "";
                                            SetConfig("TopRustServers - Server ID", topRustID);
                                            topRustKey = "";
                                            SetConfig("TopRustServers - Api Key", topRustKey);
                                            MessageChat(player, Lang("SiteDisabled", player.UserIDString, site2));
                                            break;
                                        }
                                }

                                break;
                            }
                        case "beancan":
                            {
                                switch (args[1])
                                {
                                    case "id":
                                        {
                                            beancanID = args[2];
                                            SetConfig("BeancanIO - Server ID", beancanID);
                                            MessageChat(player, Lang("EditID", player.UserIDString, site3));
                                            break;
                                        }
                                    case "key":
                                        {
                                            beancanKey = args[2];
                                            SetConfig("BeancanIO - Api Key", beancanKey);
                                            MessageChat(player, Lang("EditKey", player.UserIDString, site3));
                                            break;
                                        }
                                    case "false":
                                        {
                                            beancanID = "";
                                            SetConfig("BeancanIO - Server ID", beancanID);
                                            beancanKey = "";
                                            SetConfig("BeancanIO - Api Key", beancanKey);
                                            MessageChat(player, Lang("SiteDisabled", player.UserIDString, site3));
                                            break;
                                        }
                                }

                                break;
                            }
                    }
                } catch { }

                edit = false;
            }
        }
        #endregion

        #region Methods
        //Create New Player Data If Not Exist
        void NewPlayer(BasePlayer player)
        {
            var playerId = player.userID;
            var playerName = player.displayName;
            var tmp = Users.Where(d => d.UserId == playerId).FirstOrDefault();
            if (tmp == null)
            {
                Users.Add(new PlayerVote(playerId, playerName));
                Puts($"Created new player data for {playerName}");
                SaveData();

                string time = DateTime.Now.ToString();
                tmp = Users.Where(d => d.UserId == playerId).FirstOrDefault();
                tmp.Sites.Add(site1, new SitesVote(0, time, "0"));
                tmp.Sites.Add(site2, new SitesVote(0, time, "0"));
                tmp.Sites.Add(site3, new SitesVote(0, time, "0"));
                SaveData();
            }
            else if (!tmp.Sites.ContainsKey(site3))
            {
                tmp.Sites.Add(site3, new SitesVote(0, DateTime.Now.ToString(), "0"));
                SaveData();
            }
        }

        void GetRequest(BasePlayer player)
        {
            NewPlayer(player);

            var steamid = player.userID.ToString();

            if (rustServersKey != "" && rustServersKey != null)
            {
                webrequest.EnqueueGet("http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key=" + rustServersKey + "&steamid=" + steamid, (code, response) => GetCallback(code, response, player, site1), this);
            }

            if (topRustKey != "" && topRustKey != null)
            {
                webrequest.EnqueueGet("http://api.toprustservers.com/api/get?plugin=voter&key=" + topRustKey + "&uid=" + steamid, (code, response) => GetCallback(code, response, player, site2), this);
            }

            if (beancanKey != "" && beancanKey != null)
            {
                webrequest.EnqueueGet("http://beancan.io/vote/get/" + beancanKey + "/" + steamid, (code, response) => GetCallback(code, response, player, site3), this);
            }
        }

        void GetCallback(int code, string response, BasePlayer player, string site)
        {
            if (response == null || code != 200)
            {
                Puts($"Error: {code} - Couldn't get an answer from {site}");
                MessageChat(player, Lang("NoAnswer", player.UserIDString, site));
                return;
            }

            int tmpRes;
            int.TryParse(response, out tmpRes);
            CheckVote(player, tmpRes, site);
        }

        private void CheckVote(BasePlayer player, int response, string site)
        {
            var time = DateTime.Now;
            var tmp = Users.Where(d => d.UserId == player.userID).FirstOrDefault();

            if (tmp.Sites[site].Claimed == null)
            {
                tmp.Sites[site].Claimed = "0";
                SaveData();
            }

            var expdate = Convert.ToDateTime(tmp.Sites[site].ExpDate);
            if (time < expdate)
            {
                if (response == 1) //if voted before expire date
                {
                    tmp.Sites[site].Claimed = "-1";
                    SaveData();
                }
                MessageChat(player, Lang("AlreadyVoted", player.UserIDString, site));
                MessageChat(player, Lang("NextVote", player.UserIDString, expdate));
                return;
            }
            else
            {
                if(response == 1)
                {
                    tmp.Sites[site].Claimed = "0";
                    SaveData();
                }
            }

            if (response == 0)
            {
                if (site == site1)
                {
                    MessageChat(player, Lang("NotVoted", player.UserIDString, site, link1, rustServersID));
                    return;
                }

                if(site == site2)
                {
                    MessageChat(player, Lang("NotVoted", player.UserIDString, site, link2, topRustID));
                    return;
                }

                if (site == site3)
                {
                    MessageChat(player, Lang("NotVoted", player.UserIDString, site, link3, beancanID));
                    return;
                }
            }

            if (response == 2 && (tmp.Sites[site].Claimed == "0" || tmp.Sites[site].Claimed == "1"))
            {
                MessageChat(player, Lang("AlreadyVoted", player.UserIDString, site));
                return;
            }

            if (response == 1 || tmp.Sites[site].Claimed == "-1")
            {
                tmp.Sites[site].Votes++;
                tmp.Sites[site].Claimed = "1";
                SaveData();
                PrintWarning($"New vote on {site}: {player.displayName}");
                GetMoney(player, site);
            }
        }

        private void GetMoney(BasePlayer player, string site)
        {
            MessageChat(player, Lang("Thanks", player.UserIDString, site));

            if (useEconomics)
            {
                var i = 0;
                var total = 0;
                foreach(var m in money)
                {
                    if (m.Key != "default" && permission.GetUserGroups(player.UserIDString).Contains(m.Key))
                    {
                        Economics?.Call("Deposit", player.userID, m.Value);
                        total += Convert.ToInt32(m.Value);
                        i++;
                    }
                }
                if (i == 0)
                {
                    Economics?.Call("Deposit", player.userID, money["default"]);
                    total = Convert.ToInt32(money["default"]);
                }
                MessageChat(player, Lang("RewardCoins", player.UserIDString, total));
            }
            
            if(useRP)
            {
                var i = 0;
                var total = 0;
                foreach (var r in rp)
                {
                    if (r.Key != "default" && permission.GetUserGroups(player.UserIDString).Contains(r.Key))
                    {
                        ServerRewards?.Call("AddPoints", new object[] { player.userID, r.Value });
                        total += Convert.ToInt32(r.Value);
                        i++;
                    }
                }
                if (i == 0)
                {
                    ServerRewards?.Call("AddPoints", new object[] { player.userID, rp["default"] });
                    total = Convert.ToInt32(rp["default"]);
                }
                MessageChat(player, Lang("RewardRP", player.UserIDString, total));
            }

            if(useKits)
            {
                var i = 0;
                foreach(var kit in kits)
                {
                    if(kit.Key != "default" && permission.GetUserGroups(player.UserIDString).Contains(kit.Key))
                    {
                        Kits?.Call("GiveKit", player, kit.Value);
                        i++;
                    }
                }
                if(i == 0)
                    Kits?.Call("GiveKit", player, kits["default"]);
                MessageChat(player, Lang("RewardKit", player.UserIDString));
            }

            var tmp = Users.Where(d => d.UserId == player.userID).FirstOrDefault();
            switch (voteType)
            {
                case "hour":
                    {
                        int hours = voteInterval;
                        var newdate = DateTime.Now + new TimeSpan(0, hours, 0, 0);
                        tmp.Sites[site].ExpDate = newdate.ToString();
                        SaveData();
                        break;
                    }
                case "day":
                    {
                        int days = voteInterval;
                        var newdate = DateTime.Now + new TimeSpan(days, 0, 0, 0);
                        tmp.Sites[site].ExpDate = newdate.ToString();
                        SaveData();
                        break;
                    }
            }
        }
        #endregion

        #region Utility
        //save config
        void SetConfig(string name, object data)
        {
            if (Config[name] == null || edit)
            {
                Config[name] = data;
                SaveConfig();
                return;
            }
        }

        //read config
        object ReadConfig(string name, object data)
        {
            if (Config[name] != null)
            {
                return Config[name];
            }

            return data;
        }

        private Dictionary<string, string> ConvertToDictionary(object obj, Dictionary<string, string> dict)
        {
            if (typeof(IDictionary).IsAssignableFrom(obj.GetType()))
            {
                IDictionary idict = (IDictionary)obj;

                Dictionary<string, string> newDict = new Dictionary<string, string>();
                foreach (object key in idict.Keys)
                {
                    newDict.Add(key.ToString(), idict[key].ToString());
                }

                return newDict;
            }
            else
            {
                PrintWarning($"Invalid dictionary in config. Restored to default value");
            }

            return dict;
        }

        //control if player have permission
        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        void MessageChat(BasePlayer player, string message, string args = null) => PrintToChat(player, $"{prefix} {message}", args);
        #endregion
    }
}
