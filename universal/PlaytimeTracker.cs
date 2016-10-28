using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Playtime Tracker", "k1lly0u", "0.1.31", ResourceId = 2125)]
    class PlaytimeTracker : CovalencePlugin
    {
        #region Fields
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;
        [PluginReference] Plugin Economy;
        [PluginReference] Plugin UEconomics;

        PlayData playData;
        private DynamicConfigFile TimeData;

        PermData permData;
        private DynamicConfigFile PermissionData;

        RefData referData;
        private DynamicConfigFile ReferralData;

        private Dictionary<string, TimeInfo> timeCache;
        private Dictionary<string, Timer> updateTimers;
        private Dictionary<string, double> timeStamps;        
        private Dictionary<string, GenericPosition> posCache;

        private Timer saveTimer;
        private bool canIssueRewards;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            TimeData = Interface.Oxide.DataFileSystem.GetFile("PTTracker/playtime_data");
            PermissionData = Interface.Oxide.DataFileSystem.GetFile("PTTracker/permission_data");
            ReferralData = Interface.Oxide.DataFileSystem.GetFile("PTTracker/referral_data");

            lang.RegisterMessages(Messages, this);

            timeCache = new Dictionary<string, TimeInfo>();
            timeStamps = new Dictionary<string, double>();
            updateTimers = new Dictionary<string, Timer>();
            posCache = new Dictionary<string, GenericPosition>();
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            HasValidRewardSystem();
            SaveLoop();

            foreach (var perm in permData.permissions)
                permission.RegisterPermission(perm.Key, this);

            foreach (var player in players.Connected)
                OnUserConnected(player);
        }
        void OnUserConnected(IPlayer player)
        {
            if (player == null) return;
                InitPlayerData(player);
        }
        void OnUserDisconnected(IPlayer player)
        {
            if (player == null) return;

            if (updateTimers.ContainsKey(player.Id))
            {
                if (updateTimers[player.Id] != null)
                    updateTimers[player.Id].Destroy();
                updateTimers.Remove(player.Id);
            }
            
            AddTime(player);

            if (posCache.ContainsKey(player.Id))
                posCache.Remove(player.Id);

            if (timeStamps.ContainsKey(player.Id))
                timeStamps.Remove(player.Id);
        }
        void Unload()
        {
            if (saveTimer != null)
                saveTimer.Destroy();
            foreach (var player in players.Connected)
                OnUserDisconnected(player);

            SaveData();
        }
        void HasValidRewardSystem()
        {
            bool success = false;
            if (configData.RewardSystem.Enabled)
            {
#if RUST
                if (configData.RewardSystem.RewardPlugins.Rust.ServerRewards && ServerRewards)
                    success = true;
                else if (configData.RewardSystem.RewardPlugins.Rust.Economics && Economics)
                    success = true;
#endif
#if HURTWORLD
                if (configData.RewardSystem.RewardPlugins.Hurtworld.Economy && Economy)
                    success = true;
#endif
                if (configData.RewardSystem.RewardPlugins.Universal.UEconomics && UEconomics)
                    success = true;
                if (!success)
                    PrintWarning("Unable to initialize any reward plugins. Rewards will not be issued!");
            }
           
            canIssueRewards = success;
        }
        #endregion

        #region Time Tracking
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        void InitPlayerData(IPlayer player)
        {
            var ID = player.Id;
            var time = GrabCurrentTime();
            CheckForData(player);
            if (!timeStamps.ContainsKey(ID))
                timeStamps.Add(ID, time);
            else timeStamps[ID] = time;
            ResetTimer(player, true);
        }
        void ResetTimer(IPlayer player, bool isNew = false)
        {            
            AddTime(player);
            if (isNew)
            {                
                updateTimers.Add(player.Id, timer.Once(60, () => ResetTimer(player)));
            }
            else updateTimers[player.Id] = timer.Once(60, () => ResetTimer(player));            
        }
        void CheckForData(IPlayer player)
        {
            if (!timeCache.ContainsKey(player.Id))
                timeCache.Add(player.Id, new TimeInfo
                {
                    afkTime = 0,
                    lastReward = 0,
                    referrals = 0,
                    playTime = 0
                });
        }
        void AddTime(IPlayer player)
        {
            var ID = player.Id;
            bool isAFK = false;
            var time = GrabCurrentTime() - timeStamps[ID];
            timeStamps[ID] = GrabCurrentTime();            
            if (configData.Options.TrackAFKTime)
            {
                isAFK = CheckPosition(player);
                AddPosition(player);
            }
            if (isAFK)            
                timeCache[ID].afkTime += time;            
            else            
                timeCache[ID].playTime += time;
            
            if (canIssueRewards)
                CheckForReward(player);
        }
        
        void AddPosition(IPlayer player)
        {
            if (player?.Position() == null)
                return;
            if (!posCache.ContainsKey(player.Id))
                posCache.Add(player.Id, player.Position());
            else posCache[player.Id] = player.Position();
        }
        
        bool CheckPosition(IPlayer player)
        {
            if (player?.Position() == null)
                return false;
            if (posCache.ContainsKey(player.Id))
            {
                if (posCache[player.Id] == player.Position())
                    return true;
            }
            return false;
        }        
        #endregion

        #region Reward Management
        void CheckForReward(IPlayer player)
        {
            if (timeCache[player.Id].playTime >= (timeCache[player.Id].lastReward + configData.RewardSystem.Points.Playtime_PointTimer))
            {
                AddPoints(player, configData.RewardSystem.Points.Playtime_Points);
                timeCache[player.Id].lastReward += configData.RewardSystem.Points.Playtime_PointTimer;
            }
        }
        void AddPoints(IPlayer player, int amount)
        {
            object multiplier = GetRewardMultiplier(player);
            if (multiplier == null) multiplier = 1f;
            amount = Convert.ToInt32(Math.Floor(amount * (float)multiplier));
            #if RUST
            if (ServerRewards && configData.RewardSystem.RewardPlugins.Rust.ServerRewards)
                ServerRewards?.Call("AddPoints", ulong.Parse(player.Id), amount);

            if (Economics && configData.RewardSystem.RewardPlugins.Rust.Economics)
                Economics?.Call("Deposit", ulong.Parse(player.Id), (double)amount);
            #endif

            #if HURTWORLD
            if (Economy && configData.RewardSystem.RewardPlugins.Hurtworld.Economy)
                Economy?.Call("AddMoney", player.Object as PlayerSession, (double)amount);
            #endif
            if (UEconomics && configData.RewardSystem.RewardPlugins.Universal.UEconomics)            
                UEconomics?.Call("Deposit", player.Id, amount);            
        }
        object GetRewardMultiplier(IPlayer player)
        {
            foreach (var perm in permData.permissions)
                if (permission.UserHasPermission(player.Id, perm.Key))
                    return perm.Value;
            return null;
        }
        private string GetPlaytimeClock(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }
        #endregion

        #region API
        object GetPlayTime(string playerid)
        {
            if (timeCache.ContainsKey(playerid))
            {
                if (timeStamps.ContainsKey(playerid))
                {
                    var additional = GrabCurrentTime() - timeStamps[playerid];
                    return timeCache[playerid].playTime + additional;
                }                
                else return timeCache[playerid].playTime;
            }
            else return null;
        }
        object GetAFKTime(string playerid)
        {
            if (timeCache.ContainsKey(playerid))
                return timeCache[playerid].afkTime;
            else return null;
        }
        object GetReferrals(string playerid)
        {
            if (timeCache.ContainsKey(playerid))
                return timeCache[playerid].referrals;
            else return null;
        }
        #endregion

        #region Commands
        [Command("playtime")]
        void cmdPlaytime(IPlayer player, string command, string[] args)
        {
            if(args == null || args.Length == 0)
            {
                var time = GetPlayTime(player.Id);
                if (time != null)
                {
                    player.Reply(GetPlaytimeClock((double)time));
                    return;
                }
                else player.Reply(Msg("notime", player.Id));
            }
            if (player.IsAdmin)
            {
                if (args.Length >= 1)
                {
                    var target = players.FindPlayer(args[0]);
                    if (target != null)
                    {
                        var time = GetPlayTime(target.Id);
                        if (time != null)
                        {
                            player.Reply($"{target.Name} {GetPlaytimeClock((double)time)}");
                            return;
                        }
                        else player.Reply(Msg("notimetarget", player.Id));
                    }
                    else player.Reply(Msg("notarget", player.Id));
                }
            }
        }
        [Command("ptt")]
        void cmdPTT(IPlayer player, string command, string[] args)
        {            
            if (player.IsAdmin)
            {
                if (args == null || args.Length == 0)
                {
                    player.Reply(Msg("ptt",player.Id));
                    player.Reply(Msg("addsyn",player.Id));
                    player.Reply(Msg("remsyn",player.Id));
                    player.Reply(Msg("listsyn",player.Id));
                    return;
                }
                switch (args[0].ToLower())
                {
                    case "add":
                        if (args.Length == 3)
                        {
                            float multiplier;
                            if (float.TryParse(args[2], out multiplier))
                            {
                                var perm = args[1].ToLower();
                                if (!perm.StartsWith("playtimetracker."))
                                    perm = "playtimetracker." + perm;
                                if (!permData.permissions.ContainsKey(perm))
                                {
                                    permData.permissions.Add(perm, multiplier);
                                    permission.RegisterPermission(perm, this);
                                    SavePermission();
                                    player.Reply(string.Format(Msg("createsucc",player.Id), perm, multiplier));
                                    return;
                                }
                                else player.Reply(string.Format(Msg("permexist",player.Id), perm));
                            }
                            else player.Reply(Msg("nomulti",player.Id));
                        }
                        else player.Reply("/ptt add <permissionname> <multiplier>");
                        return;
                    case "remove":
                        if (args.Length == 2)
                        {
                            if (permData.permissions.ContainsKey(args[1]))
                            {
                                permData.permissions.Remove(args[1]);
                                player.Reply(string.Format(Msg("permrem",player.Id), args[1]));
                            }
                            else player.Reply(string.Format(Msg("noperm",player.Id), args[1]));
                        }
                        else player.Reply("/ptt remove <permissionname>");
                        return;
                    case "list":
                        player.Reply(Msg("permmulti",player.Id));
                        foreach (var perm in permData.permissions)
                            player.Reply($"{perm.Key} | {perm.Value}");
                        return;
                    default:
                        break;
                }
            }
        }

        [Command("refer")]
        void cmdRefer(IPlayer player, string command, string[] args)
        {
            if (configData.ReferralSystem.UseReferralSystem)
            {
                if (args == null || args.Length == 0)
                {
                    player.Reply(Msg("refsyn",player.Id));
                    return;
                }
                if (referData.referrals.Contains(player.Id))
                {
                    player.Reply(Msg("alreadyref",player.Id));
                    return;
                }
                if (args.Length >= 1)
                {
                    IPlayer referee = players.FindPlayer(args[0]);
                    if (referee != null)
                    {
                        if (referee.Id == player.Id)
                        {
                            player.Reply(Msg("noself",player.Id));
                            return;
                        }
                        if (timeCache.ContainsKey(referee.Id))                        
                            timeCache[referee.Id].referrals++;                                                       
                        
                        referData.referrals.Add(player.Id);
                        if (canIssueRewards && configData.ReferralSystem.IssueRewardForReferral)
                        {
                            AddPoints(player, configData.RewardSystem.Points.Referral_JoinPoints);
                            AddPoints(referee, configData.RewardSystem.Points.Referral_InvitePoints);

                            if (referee.IsConnected)
                                referee.Reply(string.Format(Msg("referacceptref", referee.Id), configData.RewardSystem.Points.Referral_InvitePoints));
                            player.Reply(string.Format(Msg("referacceptplayer", player.Id), configData.RewardSystem.Points.Referral_JoinPoints));
                        }
                        else
                        {
                            if (referee.IsConnected)
                                referee.Reply(string.Format(Msg("referaccept1ref",referee.Id),player.Name));
                            player.Reply(Msg("referaccept1player",player.Id));
                        }                        
                    }
                    else player.Reply(Msg("noplayer", player.Id));
                }
            }
        }
        #endregion

        #region Config      
        class Rewards
        {
            public bool Enabled { get; set; }            
            public RPlugins RewardPlugins { get; set; }            
            public Points Points { get; set; }
        }  
        class RPlugins
        {
            public RRust Rust { get; set; }
            public RHurtworld Hurtworld { get; set; }
            public RUniversal Universal { get; set; }
        }
        class RRust
        {
            public bool ServerRewards { get; set; }
            public bool Economics { get; set; }
        }
        class RHurtworld
        {
            public bool Economy { get; set; }
        }
        class RUniversal
        {
            public bool UEconomics { get; set; }
        }
        class Referrals
        {
            public bool UseReferralSystem { get; set; }
            public bool IssueRewardForReferral { get; set; }            
        }
        class Points
        {
            public int Playtime_PointTimer { get; set; }
            public int Playtime_Points { get; set; }
            public int Referral_JoinPoints { get; set; }
            public int Referral_InvitePoints { get; set; }
        }
        class Options
        {
            public bool TrackAFKTime { get; set; }
            public int SaveTimer { get; set; }
        }        
        private ConfigData configData;
        class ConfigData
        {
            public Rewards RewardSystem { get; set; }
            public Referrals ReferralSystem { get; set; }
            public Options Options { get; set; }            
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Options = new Options
                {
                    SaveTimer = 15,
                    TrackAFKTime = true
                },
                RewardSystem = new Rewards
                {
                    Enabled = true,
                    RewardPlugins = new RPlugins
                    {
                        Hurtworld = new RHurtworld
                        {
                            Economy = false
                        },
                        Rust = new RRust
                        {
                            Economics = false,
                            ServerRewards = true
                        },
                        Universal = new RUniversal
                        {
                            UEconomics = false
                        }
                    },
                    Points = new Points
                    {
                        Playtime_Points = 5,
                        Playtime_PointTimer = 3600,
                        Referral_InvitePoints = 5,
                        Referral_JoinPoints = 3
                    }                    
                },
                ReferralSystem = new Referrals
                {
                    IssueRewardForReferral = true,
                    UseReferralSystem = true
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
#endregion

#region Data Management
        void SaveLoop() => saveTimer = timer.Once(configData.Options.SaveTimer * 60, () => { SaveData(); SaveLoop(); });
        void SaveData()
        {
            playData.timeData = timeCache;
            TimeData.WriteObject(playData);
            ReferralData.WriteObject(referData);
        }
        void SavePermission()
        {
            PermissionData.WriteObject(permData);
        }
        void LoadData()
        {
            try
            {
                playData = TimeData.ReadObject<PlayData>();
                timeCache = playData.timeData;
            }
            catch
            {
                playData = new PlayData();
            }
            try
            {
                referData = ReferralData.ReadObject<RefData>();
            }
            catch
            {
                referData = new RefData();
            }
            try
            {
                permData = PermissionData.ReadObject<PermData>();
            }
            catch
            {
                permData = new PermData();
            }
        }
        class PlayData
        {
            public Dictionary<string, TimeInfo> timeData = new Dictionary<string, TimeInfo>();
        }
        class TimeInfo
        {
            public double playTime;
            public double afkTime;
            public double lastReward;
            public int referrals;
        }
        class PermData
        {
            public Dictionary<string, float> permissions = new Dictionary<string, float>();
        }
        class RefData
        {
            public List<string> referrals = new List<string>();
        }
        #endregion

        #region Localization
        string Msg(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"noplayer", "Unable to find a player with that name."},
            {"referaccept1player", "Your referral has been accepted"},
            {"referaccept1ref", "{0} has acknowledged a referral from you"},
            {"referacceptplayer", "Your referral has been accepted and you have received {0} points"},
            {"referacceptref", "{0} has acknowledged a referral from you and you have received {1} points"},
            {"noself", "You can not refer yourself..."},
            {"alreadyref", "You have already acknowledged a referral"},
            {"refsyn", "/refer <playername> - Acknowledge a referral from <playername>"},
            {"permmulti", "permission | multiplier"},
            {"noperm", "The permission {0} does not exist"},
            {"permrem", "You have successfully removed the permission {0}"},
            {"nomulti", "You must enter a multiplier"},
            {"permexist", "The permission '{0}' already exists"},
            {"createsucc", "You have successfully created a new reward multiplier. Permission: {0}, Multiplier {1}"},
            {"ptt", "Create custom permissions with reward multipliers for VIP players"},
            {"addsyn", "/ptt add <permissionname> <multiplier> - Add a new reward multiplier"},
            {"remsyn", "/ptt remove <permissionname> - Remove reward multiplier"},
            {"listsyn", "/ptt list - List available permission and their multipliers"},
            {"notime", "Unable to get your playtime" },
            {"notimetarget", "Unable to get that players playtime" },
            {"notarget", "Unable to find the specified player" },
        };
        #endregion
    }
}
