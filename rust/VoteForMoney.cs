using System;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Vote For Money", "Frenk92", "0.4.1", ResourceId = 2086)]
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
        const string link1 = "http://rust-servers.net/server/";
        const string link2 = "http://toprustservers.com/server/";
        public bool edit = false;

        #region Config
        string rustServersKey = "";
        string rustServersID = "";
        string topRustKey = "";
        string topRustID = "";
        string voteType = "day";
        int voteInterval = 1;
        string money = "250";
        string rp = "30";
        string kit = "";
        bool useRP = false;
        bool useEconomics = true;
        string prefix = "<color=#808000ff><b>VoteForMoney:</b></color>";

        string configVersion = "0.1.0";

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file.");
            Config.Clear();
        }

        void LoadConfigData()
        {
            //Load config
            rustServersKey = (string)ReadConfig("Rust-Servers - Api Key");
            rustServersID = (string)ReadConfig("Rust-Servers - Server ID");
            topRustKey = (string)ReadConfig("TopRustServers - Api Key");
            topRustID = (string)ReadConfig("TopRustServers - Server ID");
            voteType = (string)ReadConfig("Vote Type");
            voteInterval = Convert.ToInt16(ReadConfig("Vote Interval"));
            money = (string)ReadConfig("Money");
            rp = (string)ReadConfig("RP");
            kit = (string)ReadConfig("Kit");
            useRP = Convert.ToBoolean(ReadConfig("Use RP"));
            useEconomics = Convert.ToBoolean(ReadConfig("Use Economics"));
            prefix = (string)ReadConfig("Prefix");

            var version = (string)ReadConfig("Version");
            if(version == null || version != configVersion)
            {
                PrintWarning("Configuration is outdate. Update in progress...");
                Config.Clear();    
                //Default config
                SetConfig("Rust-Servers - Api Key", rustServersKey);
                SetConfig("Rust-Servers - Server ID", rustServersID);
                SetConfig("TopRustServers - Api Key", topRustKey);
                SetConfig("TopRustServers - Server ID", topRustID);
                SetConfig("Vote Type", voteType);
                SetConfig("Vote Interval", voteInterval);
                SetConfig("Money", money);
                SetConfig("RP", rp);
                SetConfig("Kit", kit);
                SetConfig("Use Economics", useEconomics);
                SetConfig("Use RP", useRP);
                SetConfig("Prefix", prefix);
                SetConfig("Version", configVersion);

                //Load config
                rustServersKey = (string)ReadConfig("Rust-Servers - Api Key");
                rustServersID = (string)ReadConfig("Rust-Servers - Server ID");
                topRustKey = (string)ReadConfig("TopRustServers - Api Key");
                topRustID = (string)ReadConfig("TopRustServers - Server ID");
                voteType = (string)ReadConfig("Vote Type");
                voteInterval = Convert.ToInt16(ReadConfig("Vote Interval"));
                money = (string)ReadConfig("Money");
                rp = (string)ReadConfig("RP");
                kit = (string)ReadConfig("Kit");
                useRP = Convert.ToBoolean(ReadConfig("Use RP"));
                useEconomics = Convert.ToBoolean(ReadConfig("Use Economics"));
                prefix = (string)ReadConfig("Prefix");
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
                ["RewardKit"] = "Kit rewarded",
                ["EditID"] = "{0} - Server ID edited.",
                ["EditKey"] = "{0} - Api Key edited.",
                ["SiteDisabled"] = "{0} disabled",
                ["EditMoney"] = "Money reward edited in: {0}",
                ["EditRP"] = "RP reward edited in: {0}",
                ["EditXP"] = "XP reward edited in: {0}",
                ["EditLVL"] = "Level reward edited in: {0}",
                ["EditKit"] = "Kit reward edited in: {0}",
                ["KitDisabled"] = "Kit reward disabled",
                ["ErrorNumbers"] = "Error. Insert only numbers.",
                ["EditType"] = "Vote type edited in: {0}",
                ["ErrorType"] = "Error. Only 'day' or 'hour'.",
                ["EditInterval"] = "Vote interval edited {0}",
                ["EditUseEco"] = "Use Economics edited in: {0}",
                ["EditUseRP"] = "Use RP edited in: {0}",
                ["ErrorBool"] = "Error. Only 'true' or 'false'.",
                ["Help"] = "============== VOTE HELP =============",
                ["HelpNext"] = "====> /vote help 2 - next page <====",
                ["Help1"] = "=============== PAGE 1 ===============",
                ["Help2"] = "=============== PAGE 2 ===============",
                ["HelpRSID"] = "/vote rservers id SERVERID - edit Rust-Servers id.",
                ["HelpRSKey"] = "/vote rsevers key APIKEY - edit Rust-Servers api key.",
                ["HelpRSDisable"] = "/vote rsevers false - disable Rust-Servers.",
                ["HelpTRSID"] = "/vote toprust id SERVERID - edit TopRustServers id.",
                ["HelpTRSKey"] = "/vote toprust key APIKEY - edit TopRustServers api key.",
                ["HelpTRSDisable"] = "/vote toprust false - disable TopRustServers.",
                ["HelpMoney"] = "/vote money AMOUNT - edit Money reward.",
                ["HelpRP"] = "/vote rp AMOUNT - edit RP reward.",
                ["HelpKit"] = "/vote kit KITNAME - edit Kit reward.",
                ["HelpKitDisable"] = "/vote kit false - disable Kit reward.",
                ["HelpType"] = "/vote type day/hour - edit vote type.",
                ["HelpInterval"] = "/vote interval AMOUNT - edit vote interval.",
                ["HelpUseEco"] = "/vote economy true/false - to use Economics or not.",
                ["HelpUseRP"] = "/vote userp true/false - to use RP or not.",
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
                                try
                                {
                                    if (args[1] == "2")
                                    {
                                        MessageChat(player, Lang("Help", player.UserIDString));
                                        MessageChat(player, Lang("HelpUseEco", player.UserIDString));
                                        MessageChat(player, Lang("HelpUseRP", player.UserIDString));
                                        MessageChat(player, Lang("HelpRSID", player.UserIDString));
                                        MessageChat(player, Lang("HelpRSKey", player.UserIDString));
                                        MessageChat(player, Lang("HelpRSDisable", player.UserIDString));
                                        MessageChat(player, Lang("HelpTRSID", player.UserIDString));
                                        MessageChat(player, Lang("HelpTRSKey", player.UserIDString));
                                        MessageChat(player, Lang("HelpTRSDisable", player.UserIDString));
                                        MessageChat(player, Lang("Help2", player.UserIDString));
                                        break;
                                    }
                                }
                                catch
                                {
                                    MessageChat(player, Lang("Help", player.UserIDString));
                                    MessageChat(player, Lang("HelpMoney", player.UserIDString));
                                    MessageChat(player, Lang("HelpRP", player.UserIDString));
                                    MessageChat(player, Lang("HelpKit", player.UserIDString));
                                    MessageChat(player, Lang("HelpKitDisable", player.UserIDString));
                                    MessageChat(player, Lang("HelpType", player.UserIDString));
                                    MessageChat(player, Lang("HelpInterval", player.UserIDString));
                                    MessageChat(player, Lang("HelpNext", player.UserIDString));
                                    MessageChat(player, Lang("Help1", player.UserIDString));
                                }
                                break;
                            }
                        case "money":
                            {
                                int n;
                                bool isNumber = int.TryParse(args[1], out n);
                                if (!isNumber)
                                {
                                    MessageChat(player, Lang("ErrorNumbers", player.UserIDString));
                                    break;
                                }
                                money = args[1];
                                SetConfig("Money", money);
                                MessageChat(player, Lang("EditMoney", player.UserIDString, money));
                                break;
                            }
                        case "rp":
                            {
                                int n;
                                bool isNumber = int.TryParse(args[1], out n);
                                if (!isNumber)
                                {
                                    MessageChat(player, Lang("ErrorNumbers", player.UserIDString));
                                    break;
                                }
                                rp = args[1];
                                SetConfig("RP", rp);
                                MessageChat(player, Lang("EditRP", player.UserIDString, rp));
                                break;
                            }
                        case "kit":
                            {
                                if (args[1] == "false")
                                {
                                    kit = "";
                                    MessageChat(player, Lang("KitDisabled", player.UserIDString, kit));
                                }
                                else
                                {
                                    kit = args[1];
                                    MessageChat(player, Lang("EditKit", player.UserIDString, kit));
                                }
                                SetConfig("Kit", kit);
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
                        case "economy":
                            {
                                bool flag;
                                if (Boolean.TryParse(args[1], out flag))
                                {
                                    useEconomics = flag;
                                    SetConfig("Use Economics", useEconomics);
                                    MessageChat(player, Lang("EditUseEco", player.UserIDString, flag));
                                }
                                else
                                {
                                    MessageChat(player, Lang("ErrorBool", player.UserIDString));
                                }
                                break;
                            }
                        case "userp":
                            {
                                bool flag;
                                if (Boolean.TryParse(args[1], out flag))
                                {
                                    useRP = flag;
                                    SetConfig("Use RP", useRP);
                                    MessageChat(player, Lang("EditUseRP", player.UserIDString, flag));
                                }
                                else
                                {
                                    MessageChat(player, Lang("ErrorBool", player.UserIDString));
                                }
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
                Economics?.Call("Deposit", player.userID, money);
                MessageChat(player, Lang("RewardCoins", player.UserIDString, money));
            }
            
            if(useRP)
            {
                ServerRewards?.Call("AddPoints", new object[] { player.userID, rp });
                MessageChat(player, Lang("RewardRP", player.UserIDString, rp));
            }

            if(kit != "") //if kit == "", kit reward is disable
            {
                Kits?.Call("GiveKit", player, kit);
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
        object ReadConfig(string name)
        {
            if (Config[name] != null)
            {
                return Config[name];
            }

            return null;
        }

        //control if player have permission
        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        void MessageChat(BasePlayer player, string message, string args = null) => PrintToChat(player, $"{prefix} {message}", args);
        #endregion
    }
}
