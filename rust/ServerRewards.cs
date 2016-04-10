using System;
using Oxide.Core;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;


namespace Oxide.Plugins
{
    [Info("ServerRewards", "k1lly0u", "0.1.73", ResourceId = 1751)]
    public class ServerRewards : RustPlugin
    {
        #region fields
        [PluginReference] Plugin Kits;

        bool Changed;        

        PlayerDataStorage playerData;
        private DynamicConfigFile PlayerData;

        RewardDataStorage rewardData;
        private DynamicConfigFile RewardData;

        ReferData referData;
        private DynamicConfigFile ReferralData;

        TimeData timeData = new TimeData();

        Dictionary<string, int> Rewards = new Dictionary<string, int>();
        #endregion

        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("serverrewards_players");
            RewardData = Interface.Oxide.DataFileSystem.GetFile("serverrewards_rewards");
            ReferralData = Interface.Oxide.DataFileSystem.GetFile("serverrewards_referrals");
        }

        void OnServerInitialized()
        {
            if (!Kits) PrintWarning($"Kits could not be found! Unable to issue rewards");            
            LoadData();
            LoadVariables();
            if (useTime)
            {
                foreach (var player in BasePlayer.activePlayerList) OnPlayerInit(player);
                timer.Once(saveInterval * 60, () => SaveLoop());
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                var ID = player.userID;
                if (!playerData.Players.ContainsKey(ID))
                    playerData.Players.Add(ID, new SRInfo());
                if (useTime) InitPlayerData(player);
                if (playerData.Players[ID].RewardPoints > 0)
                    msgOutstanding(player);
            }           
        }

        void OnPlayerDisconnected(BasePlayer player) => SavePlayerData(player);
        
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload() => SaveData();
        #endregion

        #region functions
        //////////////////////////////////////////////////////////////////////////////////////
        // Server Rewards ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private void InitPlayerData(BasePlayer player)
        {
            var ID = player.userID;
            if (!timeData.Players.ContainsKey(ID))
                timeData.Players.Add(ID, new PlayerInfo() { SteamID = player.UserIDString });            
            timeData.Players[ID].InitTimeStamp = GrabCurrentTime();
        }
        private void SavePlayerData(BasePlayer player)
        {
            if (useTime)
            {
                var ID = player.userID;
                if (playerData.Players.ContainsKey(ID))
                {
                    playerData.Players[ID].PlayTime += (GrabCurrentTime() - timeData.Players[player.userID].InitTimeStamp);
                    timeData.Players[player.userID].InitTimeStamp = GrabCurrentTime();

                    TimeSpan timespan = TimeSpan.FromMinutes(playerData.Players[ID].PlayTime);
                    TimeSpan ClockPlayTime = new TimeSpan(timespan.Ticks - (timespan.Ticks % 600000000));
                    playerData.Players[ID].Clock = string.Format("{0:c}", ClockPlayTime);
                    
                    checkForReward(player);                    
                }
                else OnPlayerInit(player);
            }
        }
       
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMinutes;
        
        private void checkForReward(BasePlayer player)
        {
            var ID = player.userID;
            if (playerData.Players[ID].PlayTime >= (rewardTime * playerData.Players[ID].RewardLevel))
            {
                playerData.Players[ID].RewardLevel++;
                playerData.Players[ID].RewardPoints++;                
            }
            if (playerData.Players[ID].RewardPoints > 0)
                if (useMessages)
                    if (IsDivisble(playerData.Players[ID].RewardPoints))
                        msgOutstanding(player);            
        }   
        private void msgOutstanding(BasePlayer player)
        {
            var outstanding = playerData.Players[player.userID].RewardPoints;
            SendMSG(player, string.Format(lang.GetMessage("msgOutRewards", this, player.UserIDString), outstanding));
        }
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)            
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid)
                            return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))                    
                        foundPlayers.Add(p);                    
                }            
            if (foundPlayers.Count == 0)            
                foreach (var sleeper in BasePlayer.sleepingPlayerList)                
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                                return sleeper;
                        string lowername = sleeper.displayName.ToLower();
                        if (lowername.Contains(lowerarg))                        
                            foundPlayers.Add(sleeper);
                    }            
            if (foundPlayers.Count == 0)
            {
                if (player != null)
                    SendMSG(player, lang.GetMessage("noPlayers", this, player.UserIDString));
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendMSG(player, lang.GetMessage("multiPlayers", this, player.UserIDString));
                return null;
            }

            return foundPlayers[0];
        }

        private void SendMSG(BasePlayer player, string msg, string keyword = "title")
        {
            if (keyword == "title") keyword = lang.GetMessage("title", this, player.UserIDString);
            SendReply(player, fontColor1 + keyword + "</color>" + fontColor2 + msg + "</color>");
        }
        object AddPoints(ulong ID, int amount)
        {
            if (!playerData.Players.ContainsKey(ID))
                playerData.Players.Add(ID, new SRInfo());
            playerData.Players[ID].RewardPoints += amount;
            return true;
        }
        object TakePoints(ulong ID, int amount)
        {
            if (!playerData.Players.ContainsKey(ID)) return null;
            playerData.Players[ID].RewardPoints -= amount;
            return true;
        }
        private void RemovePlayer(ulong ID)
        {
            if (playerData.Players.ContainsKey(ID))
                playerData.Players.Remove(ID);            
        }
        private bool IsDivisble(int x) => (x % displayMsgAt) == 0;
        
        #endregion

        #region chat commands
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("rewards")]
        private void cmdRewards(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "V " + Version, lang.GetMessage("title", this, player.UserIDString));                
                SendMSG(player, lang.GetMessage("chatCheck1", this, player.UserIDString), lang.GetMessage("chatCheck", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("chatList1", this, player.UserIDString), lang.GetMessage("chatList", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("chatClaim", this, player.UserIDString), lang.GetMessage("claimSyn", this, player.UserIDString));
                if (isAuth(player))
                {
                    SendMSG(player, lang.GetMessage("chatAdd", this, player.UserIDString), lang.GetMessage("addSyn", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRemove", this, player.UserIDString), lang.GetMessage("remSyn", this, player.UserIDString));
                }
                return;
            }
            if (args.Length >= 1)
            {                
                switch (args[0].ToLower())
                {  
                    case "check":
                        if (!playerData.Players.ContainsKey(player.userID))
                        {
                            SendMSG(player, lang.GetMessage("errorProfile", this, player.UserIDString));
                            Puts(lang.GetMessage("errorPCon", this, player.UserIDString), player.displayName);
                            return;
                        }
                        SavePlayerData(player);
                        string time = playerData.Players[player.userID].Clock;
                        int points = playerData.Players[player.userID].RewardPoints;
                        if (useTime)
                            SendMSG(player, string.Format(lang.GetMessage("pointsAvail", this, player.UserIDString), time, points));
                        else SendMSG(player, string.Format(lang.GetMessage("tpointsAvail", this, player.UserIDString), points));
                        return;

                    case "list":
                        SendMSG(player, "", lang.GetMessage("rewardAvail", this, player.UserIDString));
                        foreach (var entry in rewardData.Rewards)
                            SendMSG(player, lang.GetMessage("reward", this, player.UserIDString) + entry.Key + lang.GetMessage("desc1", this, player.UserIDString) + entry.Value.Description + lang.GetMessage("cost", this, player.UserIDString) + entry.Value.PointsNeeded, "");                        
                        return;

                    case "add":
                        if (isAuth(player))
                        {
                            if (args.Length >= 4)
                            {
                                int i = -1;
                                string desc = "";
                                int.TryParse(args[3], out i);
                                if (i <= 0) { SendMSG(player, lang.GetMessage("noCost", this, player.UserIDString)); return; }
                                if (args.Length == 5) desc = args[4];
                                object isKit = Kits?.Call("isKit", new object[] { args[2] });
                                if (isKit is bool)
                                    if ((bool)isKit)
                                    {
                                        if (!rewardData.Rewards.ContainsKey(args[1]))
                                            rewardData.Rewards.Add(args[1], new RewardInfo() { KitName = args[2], PointsNeeded = i , Description = desc});
                                        else
                                        {
                                            SendMSG(player, string.Format(lang.GetMessage("rewardExisting", this, player.UserIDString), args[1]));
                                            return;
                                        }
                                        SendMSG(player, string.Format(lang.GetMessage("addSuccess", this, player.UserIDString), args[1], i));
                                        SaveRewards();
                                        return;
                                    }
                                SendMSG(player, lang.GetMessage("noKit", this, player.UserIDString), "");
                                return;
                            }
                            SendMSG(player, "", lang.GetMessage("addSyn", this, player.UserIDString));
                        }
                        return;

                    case "remove":
                        if (isAuth(player))
                        {
                            if (args.Length == 2)
                            {
                                if (rewardData.Rewards.ContainsKey(args[1]))
                                {
                                    rewardData.Rewards.Remove(args[1]);
                                    SendMSG(player, "", string.Format(lang.GetMessage("remSuccess", this, player.UserIDString), args[1]));
                                    SaveRewards();
                                    return;
                                }
                                SendMSG(player, lang.GetMessage("noKitRem", this, player.UserIDString), "");
                                return;
                            }
                            SendMSG(player, "", lang.GetMessage("remSyn", this, player.UserIDString));
                        }
                        return;
                }                
            }
        }
        [ChatCommand("claim")]
        private void cmdClaim(BasePlayer player, string command, string[] args)
        {      
            if (args == null || args.Length == 0)
            {
                SendMSG(player, lang.GetMessage("chatClaim", this, player.UserIDString), lang.GetMessage("claimSyn", this, player.UserIDString));
                return;
            }      
            if (!playerData.Players.ContainsKey(player.userID))
            {
                SendMSG(player, lang.GetMessage("errorProfile", this, player.UserIDString));
                Puts(lang.GetMessage("errorPCon", this, player.UserIDString), player.displayName);
                return;
            }
            if (args.Length == 1)
            {
                if (!rewardData.Rewards.ContainsKey(args[0]))
                {
                    SendMSG(player, lang.GetMessage("noReward", this, player.UserIDString));
                    return;
                }
                int point = playerData.Players[player.userID].RewardPoints;
                int take = rewardData.Rewards[args[0]].PointsNeeded;
                if (point >= take)
                {
                    string kitname = rewardData.Rewards[args[0]].KitName;
                    object success = Kits?.Call("GiveKit", new object[] { player, kitname });
                    if (success is bool)
                        if ((bool)success)
                        {
                            SendMSG(player, "", string.Format(lang.GetMessage("claimSuccess", this, player.UserIDString), args[0]));
                            TakePoints(player.userID, take);
                            return;
                        }
                    SendMSG(player, lang.GetMessage("errorItemPlayer", this, player.UserIDString));
                    return;
                }
                SendMSG(player, lang.GetMessage("msgNoPoints", this, player.UserIDString));
                return;
            }
            SendMSG(player, "", lang.GetMessage("claimSyn", this, player.UserIDString));
            return;
        }

        [ChatCommand("refer")]
        private void cmdRefer(BasePlayer player, string command, string[] args)
        {
            if (useReferrals)
            {
                if (args == null || args.Length == 0)
                {
                    SendMSG(player, "V " + Version, lang.GetMessage("title", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRefer", this, player.UserIDString), lang.GetMessage("refSyn", this, player.UserIDString));
                    return;
                }
                if (referData.ReferredPlayers.Contains(player.userID))
                {
                    SendMSG(player, lang.GetMessage("alreadyRefer1", this, player.UserIDString));
                    return;
                }
                if (args.Length >= 1)
                {
                    BasePlayer referee = FindPlayer(player, args[0]);
                    if (referee != null)
                    {
                        if (referee.userID == player.userID)
                        {
                            SendMSG(player, lang.GetMessage("notSelf", this, player.UserIDString));
                            return;
                        }
                        if (!playerData.Players.ContainsKey(player.userID) || !playerData.Players.ContainsKey(referee.userID))
                        {
                            SendMSG(player, lang.GetMessage("errorProfile", this, player.UserIDString));
                            Puts(lang.GetMessage("errorPCon", this, player.UserIDString), player.displayName);
                            return;
                        }
                        referData.ReferredPlayers.Add(player.userID);
                        AddPoints(player.userID, referralPoints);
                        AddPoints(referee.userID, invitePoints);
                        SendMSG(player, string.Format(lang.GetMessage("rInvitee", this, player.UserIDString), referralPoints));
                        if (!referee.IsSleeping())
                            SendMSG(referee, string.Format(lang.GetMessage("rInviter", this, player.UserIDString), invitePoints, player.displayName));
                        return;
                    }
                    SendMSG(player, "", string.Format(lang.GetMessage("noFind", this, player.UserIDString), args[0]));
                }
            }
        }

        [ChatCommand("sr")]
        private void cmdSR(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, lang.GetMessage("srAdd2", this, player.UserIDString), lang.GetMessage("srAdd1", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("srTake2", this, player.UserIDString), lang.GetMessage("srTake1", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("srClear2", this, player.UserIDString), lang.GetMessage("srClear1", this, player.UserIDString));
                return;
            }
            if (args.Length >= 2)
            {
                BasePlayer target = FindPlayer(player, args[1]);
                if (target != null) 
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(args[2], out i);
                                if (i != 0)                                
                                    if (AddPoints(target.userID, i) != null) 
                                        SendMSG(player, string.Format(lang.GetMessage("addPoints", this, player.UserIDString), target.displayName, i));
                            }
                            return;
                        case "take":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null) 
                                        SendMSG(player, string.Format(lang.GetMessage("removePoints", this, player.UserIDString), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendMSG(player, string.Format(lang.GetMessage("clearPlayer", this, player.UserIDString), target.displayName));
                            return;                        
                    }
            }
        }

        [ConsoleCommand("sr")]
        private void ccmdSR(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "sr add <playername> <amount>" + lang.GetMessage("srAdd2", this));
                SendReply(arg, "sr take <playername> <amount>" + lang.GetMessage("srTake2", this));
                SendReply(arg, "sr clear <playername>" + lang.GetMessage("srClear2", this));
                return;
            }
            if (arg.Args.Length >= 2)
            {
                BasePlayer target = FindPlayer(null, arg.Args[1]);
                if (target != null)
                    switch (arg.Args[0].ToLower())
                    {
                        case "add":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(arg.Args[2], out i);
                                if (i != 0)
                                    if (AddPoints(target.userID, i) != null)
                                        SendReply(arg, string.Format(lang.GetMessage("addPoints", this), target.displayName, i));
                            }
                            return;
                        case "take":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(arg.Args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null)
                                        SendReply(arg, string.Format(lang.GetMessage("removePoints", this), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendReply(arg, string.Format(lang.GetMessage("clearPlayer", this), target.displayName));
                            return;
                    }
            }
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }
        bool isAuthCon(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You dont not have permission to use this command.");
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region data and classes
        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        class PlayerDataStorage
        {
            public Dictionary<ulong, SRInfo> Players = new Dictionary<ulong, SRInfo>();
            public PlayerDataStorage() { }
        }
        class ReferData
        {
            public List<ulong> ReferredPlayers = new List<ulong>();
            public ReferData() { }
        }
        class RewardDataStorage
        {
            public Dictionary<string, RewardInfo> Rewards = new Dictionary<string, RewardInfo>();
            public RewardDataStorage() { }
        }
        class RewardInfo
        {
            public string KitName;
            public string Description = "";
            public int PointsNeeded;            
        }
        class SRInfo
        {
            public double PlayTime = 0;
            public string Clock = "00:00";
            public int RewardLevel = 1;
            public int RewardPoints = 0;
        }
        class TimeData
        {
            public Dictionary<ulong, PlayerInfo> Players = new Dictionary<ulong, PlayerInfo>();
            public TimeData() { }
        }
        class PlayerInfo
        {
            public string SteamID;
            public double InitTimeStamp = 0;            
        }
        void SaveData()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (timeData.Players.ContainsKey(player.userID))
                    SavePlayerData(player);
            }
            PlayerData.WriteObject(playerData);
            ReferralData.WriteObject(referData);
            Puts("Saved player data");
            
        }
        void SaveRewards()
        {
            RewardData.WriteObject(rewardData);
            Puts("Saved reward data");
        }
        private void SaveLoop()
        {
            SaveData();            
            timer.Once(saveInterval * 60, () => SaveLoop());
        }       
        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<PlayerDataStorage>();
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                playerData = new PlayerDataStorage();
            }
            try
            {
                rewardData = RewardData.ReadObject<RewardDataStorage>();
            }
            catch
            {
                Puts("Couldn't load reward data, creating new datafile");
                rewardData = new RewardDataStorage();
            }
            try
            {
                referData = ReferralData.ReadObject<ReferData>();
            }
            catch
            {
                Puts("Couldn't load referral data, creating new datafile");
                referData = new ReferData();
            }
        }
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////        

        static int saveInterval = 10;
        static int rewardTime = 60;
        static int referralPoints = 2;
        static int invitePoints = 3;
        static int pointAmount = 1;
        static int displayMsgAt = 1;
        static bool useTime = true;
        static bool useReferrals = true;
        static bool useMessages = true;

        static string fontColor1 = "<color=orange>";
        static string fontColor2 = "<color=#939393>";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Options - Data save interval (minutes)", ref saveInterval);
            CheckCfg("Options - Use time played", ref useTime);
            CheckCfg("Options - Use player referrals", ref useReferrals);
            CheckCfg("Options - Time played per reward point(minutes)", ref rewardTime);
            CheckCfg("Messages - Display message when given reward points", ref useMessages);
            CheckCfg("Messages - Display messages every X amount of points", ref displayMsgAt);
            CheckCfg("Options - Amount of reward points to give", ref pointAmount);
            CheckCfg("Referrals - Points for the inviting player", ref invitePoints);
            CheckCfg("Referrals - Points for the invited player", ref referralPoints);
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

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "ServerRewards: " },
            { "msgOutRewards", "You currently have {0} unspent reward tokens! Claim them with /rewards" },
            {"msgNoPoints", "You dont have enough reward points" },
            {"errorProfile", "Error getting your profile from the database"},
            {"errorPCon", "There was a error pulling {0}'s profile from the database" },
            {"errorItem", "Error: {0} does not exist, please check your data file" },
            {"errorItemPlayer", "There was an error whilst retrieving your reward, please contact an administrator" },
            {"noFind", "Unable to find {0}" },
            {"rInviter", "You have recieved {0} reward points for inviting {1}" },
            {"rInvitee", "You have recieved {0} reward points" },
            {"refSyn", "/refer <playername>" },
            {"remSyn", "/rewards remove <rewardname>" },
            {"noKit", "Kit's could not confirm that the kit exists. Check Kit's and your kit data" },
            {"noKitRem", "Unable to find a reward kit with that name" },
            {"remSuccess", "You have successfully removed {0} from the rewards list" },
            {"addSyn", "/rewards add <rewardname> <kitname> <cost> <description>" },
            {"addSuccess", "You have added the kit {0}, available for {1} tokens" },
            {"rewardExisting", "You already have a reward kit named {0}" },
            {"noCost", "You must enter a reward cost" },
            {"reward", "Reward: " },
            {"desc1", ", Description: " },
            {"cost", ", Cost: " },
            {"claimSyn", "/claim <rewardname>" },
            {"noReward", "This reward doesnt exist!" },
            {"claimSuccess", "You have claimed {0}" },
            {"multiPlayers", "Multiple players found with that name" },
            {"noPlayers", "No players found" },
            {"pointsAvail", "You have played for {0}, and have {1} point(s) to spend" },
            {"tpointsAvail", "You have {0}  reward point(s) to spend" },
            {"rewardAvail", "Available Rewards;" },
            {"chatClaim", " - Claim the reward"},
            {"chatCheck", "/rewards check" },
            {"chatCheck1", " - Displays you current time played and current reward points"},
            {"chatList", "/rewards list"},
            {"chatList1", " - Displays current rewards and their cost"},
            {"chatAdd", " - Add a new reward kit"},
            {"chatRemove", " - Removes a reward kit"},
            {"chatRefer", " - Acknowledge your referral from <playername>"},
            {"alreadyRefer1", "You have already been referred" },
            {"addPoints", "You have given {0} {1} points" },
            {"removePoints", "You have taken {0} points from {1}"},
            {"clearPlayer", "You have removed {0}'s reward profile" },
            {"srAdd1", "/sr add <playername> <amount>" },
            {"srAdd2", " - Adds <amount> of reward points to <playername>" },
            {"srTake1", "/sr take <playername> <amount>" },
            {"srTake2", " - Takes <amount> of reward points from <playername>" },
            {"srClear1", "/sr clear <playername>" },
            {"srClear2", " - Clears <playername>'s reward profile" },
            {"notSelf", "You cannot refer yourself. But nice try!" }
        };
    }   
}
