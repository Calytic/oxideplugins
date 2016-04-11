using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;


namespace Oxide.Plugins {
    [Info("SkipNightVote", "Mordenak", "1.1.0", ResourceId = 1014)]
    class SkipNightVote : RustPlugin {

        public class TimePoll : RustPlugin {
            Dictionary<string, bool> votesReceived = new Dictionary<string, bool>();
            float votesRequired;

            public TimePoll(float votes) {
                votesRequired = votes;
            }

            bool checkVote(string playerId) {
                if ( votesReceived.ContainsKey(playerId) ) 
                    return false;
                return true;
            }

            public bool voteDay(BasePlayer player) {
                var playerId = player.userID.ToString();

                if (!checkVote(playerId))
                    return false;
                votesReceived.Add(playerId, true);
                return true;
            }

            public int tallyVotes() {
                int yesVotes = 0;
                foreach (var votes in votesReceived) {
                    if (votes.Value) 
                        yesVotes = yesVotes + 1;
                }
                return yesVotes;
            }

            public bool wasVoteSuccessful() {
                float result = (float)tallyVotes() / BasePlayer.activePlayerList.Count;
                if (result >= votesRequired)
                    return true;
                else
                    return false;
            }

        }

        public float requiredVotesPercentage = 0.5f; // % of votes needed to change time
        public float pollRetryTime = 5; // in minutes
        public float pollTimer = 1.0f; // in minutes
        public int sunsetHour = 18; // hour to start a vote
        public int sunriseHour = 8; // hour to set if vote is successful
        public bool displayVoteProgress = false; // determine whether to display a message for vote progress

        bool readyToCheck = false;
        
        public TimePoll votePoll = null;
        float lastPoll = 0f;

        // Messages!
        string noPollOpen = "No poll is open at this time.";
        string alreadyVoted = "You have already voted once.";
        string voteProgress = "Vote progress: {0} / {1} ({2}%/{3}%)";
        string voteOpenTime = "Night time skip vote is now open for {0} minute(s).";
        string voteSuccessful = "Vote was successful, it will be daytime soon.";
        string voteFailed = "Vote failed, not enough players voted to skip night time.";
        string voteReOpenTime = "Vote will re-open in {0} minute(s).";
        string voteNow = "Type <color=#FF2211>/voteday</color> now to skip night time.";
        

        [ChatCommand("voteday")]
        void cmdVoteTime(BasePlayer player, string command, string[] args) {
            if (votePoll == null) {
                SendReply(player, noPollOpen);
                return;
            }

            var checkVote = votePoll.voteDay(player);
            if (!checkVote) {
                SendReply(player, alreadyVoted);
                return; // don't go further if the player has voted
            }
            checkVotes();
            if (displayVoteProgress) {
                if (votePoll != null) {
                    int totalPlayers = BasePlayer.activePlayerList.Count;
                    int votes = votePoll.tallyVotes();
                    float percent = (float)votes / totalPlayers;
                    MessageAllPlayers( string.Format(voteProgress, votes, totalPlayers, (int)(percent*100), (int)(requiredVotesPercentage*100)) );
                }
            }
        }

        
        void openVote()
        {
            if (votePoll != null) 
                return;
            votePoll = new TimePoll(requiredVotesPercentage);
            MessageAllPlayers(string.Format(voteOpenTime, pollTimer) );
            MessageAllPlayers(voteNow);
            lastPoll = Time.realtimeSinceStartup;
        }

        void closeVote() {
            votePoll = null;
        }

        void checkVotes() {
            if (votePoll.wasVoteSuccessful()) {
                MessageAllPlayers(voteSuccessful);
                // change time
                TOD_Sky.Instance.Cycle.Hour = sunriseHour;
                Puts("{0}: {1}", Title, "has changed the server time.");
                // clean up votePoll
                closeVote();
            }
        }

        [HookMethod("OnTick")]
        private void OnTick() {
            try {
                if (readyToCheck) {
                    //Debug.Log("Plugin passed ready check...");
                    if (votePoll != null) { // timeout
                        if (Time.realtimeSinceStartup >= (lastPoll + (pollTimer * 60))) {
                            MessageAllPlayers( voteFailed );
                            MessageAllPlayers(string.Format(voteReOpenTime, pollRetryTime) );
                            closeVote();
                        }
                    }
                    if (TOD_Sky.Instance.Cycle.Hour <= sunsetHour && TOD_Sky.Instance.Cycle.Hour >= sunriseHour) {
                        // it's already day do nothing
                    }
                    else {
                        // check when last vote was...
                        if (Time.realtimeSinceStartup >= (lastPoll + (pollRetryTime * 60)) ) {
                            if (votePoll == null)
                                openVote();
                            else
                                checkVotes();
                        }
                    }
                }
            }
            catch (Exception ex) {
                PrintError("{0}: {1}", Title,"OnTick failed: " + ex.Message);
            }
        }

        private void MessageAllPlayers(string message) {
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                SendReply(player, message);
            }
        }

        void PopulateConfig() {

            Config["Version"] = Version.ToString();

            var settings = new Dictionary<string, object>();
            settings.Add("requiredVotesPercentage", requiredVotesPercentage);
            settings.Add("pollRetryTime", pollRetryTime);
            settings.Add("pollTimer", pollTimer);
            settings.Add("sunsetHour", sunsetHour);
            settings.Add("sunriseHour", sunriseHour);
            settings.Add("displayVoteProgress", displayVoteProgress);

            Config["Settings"] = settings;

            // messages
            var messages = new Dictionary<string, string>();
            messages.Add("noPollOpen", "No poll is open at this time.");
            messages.Add("alreadyVoted", "You have already voted once.");
            messages.Add("voteProgress", "Vote progress: {0} / {1} ({2}%/{3}%)");
            messages.Add("voteOpenTime", "Night time skip vote is now open for {0} minute(s).");
            messages.Add("voteSuccessful", "Vote was successful, it will be daytime soon.");
            messages.Add("voteFailed", "Vote failed, not enough players voted to skip night time.");
            messages.Add("voteReOpenTime", "Vote will re-open in {0} minute(s).");
            messages.Add("voteNow", "Type <color=#FF2211>/voteday</color> now to skip night time.");

            Config["Messages"] = messages;

            SaveConfig();
        }

        void Loaded() {
            LoadConfig();

            Debug.Log( string.Format("version is {0}", Version) );

            if (Config["Version"] != null) {
                var cfgVersion = Config["Version"] as string;
                if (cfgVersion != Version.ToString()) {
                    PrintError("{0}: {1}", Title, "Config out of date!  Forcing update.");
                    Config.Clear();
                    PopulateConfig();
                    readyToCheck = true;
                }
                else {
                    if (Config["Settings"] != null) {
                        var settings = Config["Settings"] as Dictionary<string, object>;

                        requiredVotesPercentage = (float)Convert.ChangeType(settings["requiredVotesPercentage"], typeof(float));
                        pollRetryTime = (float)Convert.ChangeType(settings["pollRetryTime"], typeof(float));
                        pollTimer = (float)Convert.ChangeType(settings["pollTimer"], typeof(float));
                        sunsetHour = (int)settings["sunsetHour"];
                        sunriseHour = (int)settings["sunriseHour"];
                        displayVoteProgress = (bool)settings["displayVoteProgress"];
                    }
                    else {
                        PrintError("{0}: {1}", Title, "Loading Config[\"Settings\"] failed.");
                    }

                    if (Config["Messages"] != null) {
                        var messages = Config["Messages"] as Dictionary<string, object>;

                        
                        noPollOpen = (string)messages["noPollOpen"];
                        alreadyVoted = (string)messages["alreadyVoted"];
                        voteProgress = (string)messages["voteProgress"];
                        voteOpenTime = (string)messages["voteOpenTime"];
                        voteSuccessful = (string)messages["voteSuccessful"];
                        voteFailed = (string)messages["voteFailed"];
                        voteReOpenTime = (string)messages["voteReOpenTime"];
                        voteNow = (string)messages["voteNow"];
                    }
                    else {
                        PrintError("{0}: {1}", Title, "Loading Config[\"Messages\"] failed.");
                    }
                    
                    readyToCheck = true;
                    // it appears we don't want to get this too early...
                    PopulateConfig();
                }
            }
            else {
                PrintError("{0}: {1}", Title, "Config out of date!  Forcing update.");
                Config.Clear();
                PopulateConfig();
                readyToCheck = true;
            }
        }



    }

}
