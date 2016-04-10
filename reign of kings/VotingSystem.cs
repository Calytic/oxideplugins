using System;
using System.Collections.Generic;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.WorldEvents;
using CodeHatch.Networking.Events.WorldEvents.TimeEvents;
using CodeHatch.Networking.Events;
using CodeHatch.Common;

namespace Oxide.Plugins
{
    [Info("Voting System", "D-Kay", "1.3.2")]
    class VotingSystem : ReignOfKingsPlugin
    {
        private bool UseYNCommands => GetConfig("UseYNCommands", true);
        private int VoteDuration => GetConfig("VoteDuration", 30);
        private int TimeVoteCooldown => GetConfig("TimeVoteCooldown", 600);
        private int WeatherVoteCooldown => GetConfig("WeatherVoteCooldown", 180);

        private bool CanCommenceVoteTime = true;
        private bool CanCommenceVoteWeather = true;
        private int type = 0;

        List<Vote> CurrentVote = new List<Vote>();
        public class Vote
        {
            private Player _voter = null;
            private bool _choice = true;
            public Vote(Player player, bool choice)
            {
                _voter = player; _choice = choice;
            }
            public Player Voter { get { return _voter; } set { _voter = value; } }
            public bool Choice { get { return _choice; } set { _choice = value; } }
            public void Clear()
            {
                _voter = null; _choice = true;

            }
        }

        protected override void LoadDefaultConfig()
        {
            Config["UseYNCommands"] = UseYNCommands;
            Config["VoteDuration"] = VoteDuration;
            Config["TimeVoteCooldown"] = TimeVoteCooldown;
            Config["WeatherVoteCooldown"] = WeatherVoteCooldown;
            SaveConfig();
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "voteDayStart", "{0} wants to change the [4444FF]time[FFFFFF] to [4444FF]day[FFFFFF]." },
                { "voteNightStart", "{0} wants to change the [4444FF]time[FFFFFF] to [4444FF]night[FFFFFF]." },
                { "voteClearStart", "{0} wants to [4444FF]clear[FFFFFF] the [4444FF]weather[FFFFFF]." },
                { "voteStormStart", "{0} wants to make it [4444FF]storm[FFFFFF]." },
                { "voteDayPassed", "The vote to set the time to day has passed. ({0}% of the votes were yes)" },
                { "voteNightPassed", "The vote to set the time to night has passed. ({0}% of the votes were yes)" },
                { "voteClearPassed", "The vote to set the weather to clear has passed. ({0}% of the votes were yes)" },
                { "voteStormPassed", "The vote to make it storm has passed. ({0}% of the votes were yes)" },
                { "voteTimeFailed", "The vote to set the time to day has passed. ({0}% of the votes were yes)" },
                { "voteNightFailed", "The vote to set the time to night has passed. ({0}% of the votes were yes)" },
                { "voteClearFailed", "The vote to set the weather to clear has passed. ({0}% of the votes were yes)" },
                { "voteStormFailed", "The vote to make it storm has passed. ({0}% of the votes were yes)" },
                { "timeVoteReset", "The vote timer has reset and a new time vote can be started." },
                { "weatherVoteReset", "The vote timer has reset and a new weather vote can be started." },
                { "voteCommands", "[FFFFFF]Type [33CC33](/y)es[FFFFFF] or [FF0000](/n)o[FFFFFF] to participate in the vote." },
                { "voteDuration", "[FFFFFF]The vote will end in {0} seconds." },
                { "noOngoingVote", "There isn't an ongoing vote right now." },
                { "ongoingVote", "There is already an ongoing vote." },
                { "alreadyVoted", "You have already casted your vote." },
                { "voteYes", "{0} has voted [33CC33]yes[FFFFFF] to the current vote." },
                { "voteNo", "{0} has voted [33CC33]no[FFFFFF] to the current vote." },
                { "timeVoteCooldown", "There was a vote recently. There must be a {0} minutes delay between each time vote." },
                { "weatherVoteCooldown", "There was a vote recently. There must be a {0} minutes delay between each weather vote." }
            }, this);
        }

        private void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            if (UseYNCommands)
            {
                cmd.AddChatCommand("y", this, "YesCommand");
                cmd.AddChatCommand("n", this, "NoCommand");
            }
        }

        private void VoteFinish()
        {
            int yes = 0; int no = 0;
            foreach (var vote in CurrentVote)
            {
                if (vote.Choice == true) yes++;
                else no++;
            }
            float YesPercent = ((float)yes / (yes + no)) * 100;
            if (YesPercent >= 50f)
            {
                string Percent = ((int)YesPercent).ToString();
                switch (type)
                {
                    case 1:
                        PrintToChat(string.Format(GetMessage("voteDayPassed"), Percent));
                        EventManager.CallEvent(new TimeSetEvent(GameClock.Instance.HourOfSunriseStart, GameClock.Instance.DaySpeed));
                        break;
                    case 2:
                        PrintToChat(string.Format(GetMessage("voteNightPassed"), Percent));
                        EventManager.CallEvent(new TimeSetEvent(GameClock.Instance.HourOfSunsetStart, GameClock.Instance.DaySpeed));
                        break;
                    case 3:
                        PrintToChat(string.Format(GetMessage("voteClearPassed"), Percent));
                        EventManager.CallEvent(new WeatherSetEvent(Weather.WeatherType.Clear));
                        break;
                    case 4:
                        PrintToChat(string.Format(GetMessage("voteStormPassed"), Percent));
                        EventManager.CallEvent(new WeatherSetEvent(Weather.WeatherType.PrecipitateHeavy));
                        break;
                }
            }
            else
            {
                string Percent = ((int)YesPercent - 100).ToString();
                switch (type)
                {
                    case 1:
                        PrintToChat(string.Format(GetMessage("voteDayFailed"), Percent));
                        break;
                    case 2:
                        PrintToChat(string.Format(GetMessage("voteNightFailed"), Percent));
                        break;
                    case 3:
                        PrintToChat(string.Format(GetMessage("voteClearFailed"), Percent));
                        break;
                    case 4:
                        PrintToChat(string.Format(GetMessage("voteStormFailed"), Percent));
                        break;
                }
            }
            CurrentVote.Clear();
            if (type == 1 || type == 2)
            {
                timer.In(TimeVoteCooldown, VoteTimerResetTime);
            }
            else if (type == 3 || type == 4)
            {
                timer.In(WeatherVoteCooldown, VoteTimerResetWeather);
            }
        }

        private void VoteTimerResetTime()
        {
            CanCommenceVoteTime = true;
            PrintToChat(GetMessage("timeVoteReset"));
        }

        private void VoteTimerResetWeather()
        {
            CanCommenceVoteWeather = true;
            PrintToChat(GetMessage("weatherVoteReset"));
        }

        private void VoteStart(Player player)
        {
            if (type == 1 || type == 2)
            {
                if (CanCommenceVoteTime == false)
                {
                    SendReply(player, GetMessage("timeVoteCooldown", player.Id.ToString()), (TimeVoteCooldown / 60).ToString());
                    return;
                }
            }
            else if (type == 3 || type == 4)
            {
                if (CanCommenceVoteWeather == false)
                {
                    SendReply(player, GetMessage("weatherVoteCooldown", player.Id.ToString()), (WeatherVoteCooldown / 60).ToString());
                    return;
                }
            }
            CurrentVote.Add(new Vote(player, true));
            switch (type)
            {
                case 1:
                    PrintToChat(string.Format(GetMessage("voteDayStart"), player.DisplayName));
                    break;
                case 2:
                    PrintToChat(string.Format(GetMessage("voteNightStart"), player.DisplayName));
                    break;
                case 3:
                    PrintToChat(string.Format(GetMessage("voteClearStart"), player.DisplayName));
                    break;
                case 4:
                    PrintToChat(string.Format(GetMessage("voteStormStart"), player.DisplayName));
                    break;
            }
            PrintToChat(GetMessage("voteCommands"));
            PrintToChat(string.Format(GetMessage("voteDuration"), VoteDuration.ToString()));
            if (type == 1 || type == 2)
            {
                CanCommenceVoteTime = false;
            }
            else if (type == 3 || type == 4)
            {
                CanCommenceVoteWeather = false;
            }
            timer.In(VoteDuration, VoteFinish);
        }

        private void addVote(Player player, bool voted)
        {
            if (CurrentVote.Count == 0)
            {
                SendReply(player, GetMessage("noOngoingVote", player.Id.ToString()));
                return;
            }
            foreach (var vote in CurrentVote)
            {
                if (vote.Voter == player)
                {
                    SendReply(player, GetMessage("alreadyVoted", player.Id.ToString()));
                    return;
                }
            }
            if (voted)
            {
                CurrentVote.Add(new Vote(player, true));
                PrintToChat(string.Format(GetMessage("voteYes"), player.DisplayName));
            }
            else
            {
                CurrentVote.Add(new Vote(player, false));
                PrintToChat(string.Format(GetMessage("voteNo"), player.DisplayName));
            }
        }

        [ChatCommand("voteday")]
        private void VoteDayCommand(Player player)
        {
            if (CurrentVote.Count > 0)
            {
                SendReply(player, GetMessage("ongoingVote", player.Id.ToString()));
                return;
            }
            type = 1;
            VoteStart(player);
        }

        [ChatCommand("votenight")]
        private void VoteNightCommand(Player player)
        {
            if (CurrentVote.Count > 0)
            {
                SendReply(player, GetMessage("ongoingVote", player.Id.ToString()));
                return;
            }
            type = 2;
            VoteStart(player);
        }

        [ChatCommand("votewclear")]
        private void VoteWClearCommand(Player player)
        {
            if (CurrentVote.Count > 0)
            {
                SendReply(player, GetMessage("ongoingVote", player.Id.ToString()));
                return;
            }
            type = 3;
            VoteStart(player);
        }

        [ChatCommand("votewheavy")]
        private void VoteWHeavyCommand(Player player)
        {
            if (CurrentVote.Count > 0)
            {
                SendReply(player, GetMessage("ongoingVote", player.Id.ToString()));
                return;
            }
            type = 4;
            VoteStart(player);
        }

        [ChatCommand("no")]
        private void NoCommand(Player player)
        {
            addVote(player, false);
        }
        
        [ChatCommand("yes")]
        private void YesCommand (Player player)
        {
            addVote(player, true);
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
}
