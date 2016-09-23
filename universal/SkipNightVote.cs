using System;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SkipNightVote", "k1lly0u", "0.1.1", ResourceId = 2058)]
    class SkipNightVote : CovalencePlugin
    {
        #region Fields
        private List<string> ReceivedVotes;

        private bool VoteOpen;
        private int TimeRemaining;
        private int RequiredVotes;
        private string TimeRemMSG;
        private Timer VotingTimer;
        private Timer TimeCheck;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("skipnightvote.admin", this);
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized() {
            LoadVariables();
            ReceivedVotes = new List<string>();
            RequiredVotes = 0;
            VoteOpen = false;
            TimeRemaining = 0;
            TimeRemMSG = GetMSG("timeRem").Replace("{secCol}", configData.Messaging.MSGColor).Replace("{mainCol}", configData.Messaging.MainColor);
            CheckTime();
        }
        void Unload()
        {
            if (VotingTimer != null)
                VotingTimer.Destroy();
            if (TimeCheck != null)
                TimeCheck.Destroy();            
        }
        #endregion

        #region Functions
        private void OpenVote()
        {
            var rVotes = server.Players * configData.Options.RequiredVotePercentage;
            if (rVotes < 1) rVotes = 1;
            RequiredVotes = Convert.ToInt32(rVotes);
            VoteOpen = true;
            var msg = GetMSG("voteMSG").Replace("{secCol}", configData.Messaging.MSGColor).Replace("{mainCol}", configData.Messaging.MainColor).Replace("{reqVote}", (configData.Options.RequiredVotePercentage * 100).ToString());
            server.Broadcast(msg);
            VoteTimer();
        }
        private void VoteTimer()
        {
            TimeRemaining = configData.VoteTimers.VoteOpenTimer * 60;
            VotingTimer = timer.Repeat(1, TimeRemaining, () =>
            {
                TimeRemaining--;
                if (TimeRemaining == 0)
                {
                    TallyVotes();
                    return;
                }
                if (TimeRemaining == 180)
                {
                    server.Broadcast(TimeRemMSG.Replace("{time}", "3").Replace("{type}", GetMSG("Minutes")));
                }
                if (TimeRemaining == 120)
                {
                    server.Broadcast(TimeRemMSG.Replace("{time}", "2").Replace("{type}", GetMSG("Minutes")));
                }
                if (TimeRemaining == 60)
                {
                    server.Broadcast(TimeRemMSG.Replace("{time}", "1").Replace("{type}", GetMSG("Minute")));
                }
                if (TimeRemaining == 30)
                {
                    server.Broadcast(TimeRemMSG.Replace("{time}", "30").Replace("{type}", GetMSG("Seconds")));
                }
                if (TimeRemaining == 10)
                {
                    server.Broadcast(TimeRemMSG.Replace("{time}", "10").Replace("{type}", GetMSG("Seconds")));
                }
            });
        }        
        private void CheckTime()
        {
            if (!VoteOpen)
            {
                if ((server.Time.TimeOfDay >= TimeSpan.Parse(configData.Options.TimeToOpen) && server.Time.TimeOfDay < TimeSpan.Parse("23:59:59")) || (server.Time.TimeOfDay >= TimeSpan.Parse("00:00:00") && server.Time.TimeOfDay < TimeSpan.Parse(configData.Options.TimeToSet)))
                {
                    OpenVote();
                }
                else
                {
                    TimeCheck = timer.Once(20, () => CheckTime());
                }
            }
            else
            {
                if (server.Time.TimeOfDay >= TimeSpan.Parse(configData.Options.TimeToSet) && server.Time.TimeOfDay < TimeSpan.Parse(configData.Options.TimeToOpen))
                {
                    VoteEnd(false);
                }
            }
        }
        private void TallyVotes()
        {
            if (ReceivedVotes.Count >= RequiredVotes)
                VoteEnd(true);
            else VoteEnd(false);
        }
        private void VoteEnd(bool success)
        {            
            VoteOpen = false;
            RequiredVotes = 0;
            VotingTimer.Destroy();
            ReceivedVotes.Clear();
            TimeRemaining = 0;

            if (success)
            {
                server.Time = server.Time.Date + TimeSpan.Parse(configData.Options.TimeToSet);                
                server.Broadcast($"{configData.Messaging.MainColor}{GetMSG("Voting was successful, skipping night.")}</color>");
            }
            else
            {
                server.Broadcast($"{configData.Messaging.MainColor}{GetMSG("Voting was unsuccessful.")}</color>");
            }
            TimeCheck = timer.Once(configData.VoteTimers.TimeBetweenVotes * 60, () => CheckTime());
        }
        #endregion

        #region Helpers
        private bool AlreadyVoted(string player) => ReceivedVotes.Contains(player);
        #endregion

        #region ChatCommands
        [Command("voteday")]
        private void cmdVoteDay(IPlayer player, string command, string[] args)
        {
            if (VoteOpen)
            {
                if (!AlreadyVoted(player.Id))
                {
                    ReceivedVotes.Add(player.Id);
                    player.Reply(GetMSG("You have voted to skip night", player.Id));
                    server.Broadcast($"{configData.Messaging.MainColor}{ReceivedVotes.Count} / {RequiredVotes}</color> {configData.Messaging.MSGColor}{GetMSG("have voted to skip night", player.Id)}</color>");
                    if (ReceivedVotes.Count >= RequiredVotes)
                        VoteEnd(true);
                    return;
                }
            }
            else player.Reply($"{configData.Messaging.MainColor}{GetMSG("There is not a vote currently open", player.Id)}</color>");
        }
        [Command("nightvote"), Permission("skipnightvote.admin")]
        private void cmdAdminVote(IPlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                player.Reply($"{configData.Messaging.MainColor}/nightvote open</color> {configData.Messaging.MSGColor}- {GetMSG("Force open a new vote", player.Id)}</color>");
                player.Reply($"{configData.Messaging.MainColor}/nightvote close</color> {configData.Messaging.MSGColor}- {GetMSG("Cancel the current vote", player.Id)}</color>");
                return;
            }
            switch (args[0].ToLower())
            {
                case "open":
                    {
                        OpenVote();
                    }
                    return;
                case "close":
                    {
                        VoteEnd(false);
                    }
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class Messaging
        {
            public string MainColor { get; set; }
            public string MSGColor { get; set; }
        }        
        class Timers
        {
            public int VoteOpenTimer { get; set; }
            public int TimeBetweenVotes { get; set; }
        }
        class Options
        {
            public float RequiredVotePercentage { get; set; }
            public string TimeToOpen { get; set; }
            public string TimeToSet { get; set; }
        }
        class ConfigData
        {

            public Messaging Messaging { get; set; }
            public Timers VoteTimers { get; set; }
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
                Messaging = new Messaging
                {
                    MainColor = "<color=orange>",
                    MSGColor = "<color=#939393>"
                },
                Options = new Options
                {
                    RequiredVotePercentage = 0.4f,
                    TimeToOpen = "18:00:00",
                    TimeToSet = "07:00:00"
                },
                VoteTimers = new Timers
                {
                    VoteOpenTimer = 4,
                    TimeBetweenVotes = 5
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messaging
        private string GetMSG(string key, string userid = null) => lang.GetMessage(key, this, userid);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"Force open a new vote", "Force open a new vote" },
            {"Cancel the current vote", "Cancel the current vote" },
            {"There is not a vote currently open", "There is not a vote currently open" },
            {"You have voted to skip night", "You have voted to skip night" },
            {"have voted to skip night", "players have voted to skip night" },
            {"Voting was successful, skipping night.", "Voting was successful, skipping night." },
            {"Voting was unsuccessful.", "Voting was unsuccessful." },
            {"Minutes", "Minutes" },
            {"Minute", "Minute" },
            {"Seconds", "Seconds" },
            {"voteMSG", "{secCol}Type</color> {mainCol}/voteday</color> {secCol}now if you want to skip night. If </color>{mainCol}{reqVote}%</color> {secCol}of players vote night will be skipped</color>" },
            {"timeRem", "{secCol}Voting ends in</color> {mainCol}{time} {type}</color>{secCol}, use </color>{mainCol}/voteday</color>{secCol} to cast your vote</color>" }
        };
        #endregion
    }
}
