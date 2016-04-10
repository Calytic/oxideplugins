/******************************************************************************
* Version 2.0 Changelog
*** Rewrote and cleaned up the plugin.*
*** Changed the method of logging time to log time every x minutes.*
*** Cleaned up the playtime command output.*
*** Cleaned up the lastseen command output.*
*** Cleaned up the mostonline command output.*
*** Added a message on login that says how long since the player last logged in.*
*** Fixed KeyNotFoundException when running lastseen on a user that doesn't exist.*
*** Added a prefix to all chat commands. "Play Time"*
*** Added BroadcastLastSeenOnConnect to config, so the broadcast can be disabled if need be.
*** Config will update with new values automatically in future updates.
******************************************************************************/

using System.Collections.Generic;
using System;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PlayTime", "Waizujin", 2.0)]
    [Description("Logs players play time and allows you to view the players play time with a command.")]
    public class PlayTime : RustPlugin
    {
		public string Prefix = "<color=red>Play Time:</color> ";
		public int SaveInterval { get { return Config.Get<int>("Save Interval"); } }
		public bool BroadcastLastSeenOnConnect { get { return Config.Get<bool>("Broadcast Last Seen on Connect"); } }

		protected override void LoadDefaultConfig()
		{
			PrintWarning("Creating a new configuration file.");

			Config["Save Interval"] = 300;
			Config["Broadcast Last Seen on Connect"] = true;
		}

		class PlayTimeData
        {
            public Dictionary<string, PlayTimeInfo> Players = new Dictionary<string, PlayTimeInfo>();

            public PlayTimeData() {  }
        }

        class PlayTimeInfo
        {
            public string SteamID;
            public string Name;
            public long LastPlayTimeIncrement;
            public long LastLogoutTime;
            public long PlayTime;

            public PlayTimeInfo() {  }

            public PlayTimeInfo(BasePlayer player)
            {
                long currentTimestamp = GrabCurrentTimestamp();
                SteamID = player.userID.ToString();
                Name = player.displayName;
                LastPlayTimeIncrement = currentTimestamp;
                LastLogoutTime = 0;
                PlayTime = 0;
            }
        }

        PlayTimeData playTimeData;

        private void OnServerInitialized()
        {
			var dirty = false;

			if (Config["Save Interval"] == null)
			{
				Config["Save Interval"] = 300;
				dirty = true;
			}

			if (Config["Broadcast Last Seen on Connect"] == null)
			{
				Config["Broadcast Last Seen on Connect"] = true;
				dirty = true;
			}

			if (dirty)
			{
				PrintWarning("Updating configuration file with new values.");
				SaveConfig();
			}

			playTimeData = Interface.GetMod().DataFileSystem.ReadObject<PlayTimeData>("PlayTime");

            permission.RegisterPermission("canUsePlayTime", this);
            permission.RegisterPermission("canUseLastSeen", this);
            permission.RegisterPermission("canUseMostOnline", this);

            timer.Repeat(SaveInterval, 0, () => updatePlayTime());
        }

        void OnPlayerInit(BasePlayer player)
        {
			long currentTimestamp = GrabCurrentTimestamp();
			var info = new PlayTimeInfo(player);
			
			if (BroadcastLastSeenOnConnect)
			{
				long lastLogoutTime = playTimeData.Players[info.SteamID].LastLogoutTime;
				long lastSeenDays = 0;
				long lastSeenHours = 0;
				long lastSeenMinutes = 0;
				long lastSeenSeconds = currentTimestamp - lastLogoutTime;

				if (lastSeenSeconds > 60)
				{
					lastSeenMinutes = lastSeenSeconds / 60;
					lastSeenSeconds = lastSeenSeconds - (lastSeenMinutes * 60);
				}

				if (lastSeenMinutes > 60)
				{
					lastSeenHours = lastSeenMinutes / 60;
					lastSeenMinutes = lastSeenMinutes - (lastSeenHours * 60);
				}

				if (lastSeenHours > 24)
				{
					lastSeenDays = lastSeenHours / 24;
					lastSeenHours = lastSeenHours - (lastSeenDays * 24);
				}

				if (lastSeenDays > 0)
				{
					PrintToChat(Prefix + player.displayName + " was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
					Puts(player.displayName + " was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
				}
				else if (lastSeenHours > 0)
				{
					PrintToChat(Prefix + player.displayName + " was last seen " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
					Puts(player.displayName + " was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
				}
				else if (lastSeenMinutes > 0)
				{
					PrintToChat(Prefix + player.displayName + " was last seen " + lastSeenMinutes + " minutes ago.");
					Puts(player.displayName + " was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
				}
				else
				{
					PrintToChat(Prefix + player.displayName + " was last seen " + lastSeenSeconds + " seconds ago.");
					Puts(player.displayName + " was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
				}
			}

			if (playTimeData.Players.ContainsKey(info.SteamID))
			{
				Puts("Player already has a PlayTime log.");

				playTimeData.Players[info.SteamID].LastPlayTimeIncrement = currentTimestamp;
				playTimeData.Players[info.SteamID].Name = player.displayName;
			}
			else
			{
				Puts("Saving new player to PlayTime log.");
				playTimeData.Players.Add(info.SteamID, info);
			}

			Interface.GetMod().DataFileSystem.WriteObject("PlayTime", playTimeData);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            long currentTimestamp = GrabCurrentTimestamp();
            var info = new PlayTimeInfo(player);

            long lastIncrement = playTimeData.Players[info.SteamID].LastPlayTimeIncrement;
            long totalPlayed = currentTimestamp - lastIncrement;

            playTimeData.Players[info.SteamID].PlayTime += totalPlayed;
            playTimeData.Players[info.SteamID].LastPlayTimeIncrement = currentTimestamp;
            playTimeData.Players[info.SteamID].LastLogoutTime = currentTimestamp;
            Interface.GetMod().DataFileSystem.WriteObject("PlayTime", playTimeData);
        }

        public void updatePlayTime()
        {
			Puts("Saving playtime data for all online users.");
            foreach (BasePlayer onlinePlayer in BasePlayer.activePlayerList)
            {
                long currentTimestamp = GrabCurrentTimestamp();
                long playerSteamID = FindPlayer(onlinePlayer.userID.ToString());

                if (playerSteamID == 0)
                {
					// No info in file, so lets initiate him.
					var info = new PlayTimeInfo(onlinePlayer);

					if (playTimeData.Players.ContainsKey(info.SteamID))
					{
						Puts("Player already has a PlayTime log.");

						playTimeData.Players[info.SteamID].LastPlayTimeIncrement = GrabCurrentTimestamp();
						playTimeData.Players[info.SteamID].Name = onlinePlayer.displayName;
					}
					else
					{
						Puts("Saving new player to PlayTime log.");
						playTimeData.Players.Add(info.SteamID, info);
					}

					Interface.GetMod().DataFileSystem.WriteObject("PlayTime", playTimeData);

					return;
                }

                if (onlinePlayer.userID.ToString() == playerSteamID.ToString())
                {
					string playerName = playTimeData.Players[playerSteamID.ToString()].Name;
					long playedTime = currentTimestamp - playTimeData.Players[playerSteamID.ToString()].LastPlayTimeIncrement;

                    playTimeData.Players[playerSteamID.ToString()].PlayTime += playedTime;
                    playTimeData.Players[playerSteamID.ToString()].LastPlayTimeIncrement = currentTimestamp;

                    Interface.GetMod().DataFileSystem.WriteObject("PlayTime", playTimeData);
                }
            }
        }

        [ChatCommand("playtime")]
        private void PlayTimeCommand(BasePlayer player, string command, string[] args)
        {
			if (!hasPermission(player, "canUsePlayTime") && args.Length > 0)
			{
				SendReply(player, Prefix + "You don't have permission to use this command.");

				return;
			}

			var queriedPlayer = "";

			if (args.Length == 0)
			{
				queriedPlayer = player.userID.ToString();
			}
			else
			{
				queriedPlayer = args[0];
			}

			long daysPlayed = 0;
			long hoursPlayed = 0;
			long minutesPlayed = 0;
			long secondsPlayed = 0;

			long playerSteamID = 0;

			playerSteamID = FindPlayer(queriedPlayer);

			if (playerSteamID == 0)
			{
				SendReply(player, Prefix + "The player '" + queriedPlayer + "' does not exist in the system.");

				return;
			}

			string playerName = playTimeData.Players[playerSteamID.ToString()].Name;

			secondsPlayed = playTimeData.Players[playerSteamID.ToString()].PlayTime;

			if (secondsPlayed > 60)
			{
				minutesPlayed = secondsPlayed / 60;
				secondsPlayed = secondsPlayed - (minutesPlayed * 60);
			}

			if (minutesPlayed > 60)
			{
				hoursPlayed = minutesPlayed / 60;
				minutesPlayed = minutesPlayed - (hoursPlayed * 60);
			}

			if (hoursPlayed > 24)
			{
				daysPlayed = hoursPlayed / 24;
				hoursPlayed = hoursPlayed - (daysPlayed * 24);
			}

			if (daysPlayed > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " (" + playerSteamID + ") has played for " + daysPlayed + " days " + hoursPlayed + " hours and " + minutesPlayed + " minutes.");
			}
			else if (hoursPlayed > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " (" + playerSteamID + ") has played for " + hoursPlayed + " hours and " + minutesPlayed + " minutes.");
			}
			else if (minutesPlayed > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " (" + playerSteamID + ") has played for " + minutesPlayed + " minutes.");
			}
			else
			{
				SendReply(player, Prefix + "The player " + playerName + " (" + playerSteamID + ") has played for " + secondsPlayed + " seconds.");
			}
		}

		[ChatCommand("lastseen")]
		private void LastSeenCommand(BasePlayer player, string command, string[] args)
		{
			if (!hasPermission(player, "canUseLastSeen"))
			{
				SendReply(player, Prefix + "You don't have permission to use this command.");

				return;
			}

			if (args.Length == 0)
			{
				SendReply(player, Prefix + "Please enter a players name or steam 64 id.");

				return;
			}

			long currentTimestamp = GrabCurrentTimestamp();
			var queriedPlayer = args[0];
			long playerSteamID = 0;
            try { playerSteamID = FindPlayer(queriedPlayer); } catch (KeyNotFoundException e ) { return; }

			if (playerSteamID == 0)
			{
				SendReply(player, Prefix + "The player '" + queriedPlayer + "' does not exist in the system.");

				return;
			}

			string playerName = playTimeData.Players[playerSteamID.ToString()].Name;

			foreach (BasePlayer onlinePlayer in BasePlayer.activePlayerList)
			{
				if (onlinePlayer.userID.ToString() == playerSteamID.ToString())
				{
					SendReply(player, Prefix + "The player " + playerName + " ( " + playerSteamID + ") is online right now!");

					return;
				}
			}

			long lastLogoutTime = playTimeData.Players[playerSteamID.ToString()].LastLogoutTime;
			long lastSeenDays = 0;
			long lastSeenHours = 0;
			long lastSeenMinutes = 0;
			long lastSeenSeconds = currentTimestamp - lastLogoutTime;

			if (lastSeenSeconds > 60)
			{
				lastSeenMinutes = lastSeenSeconds / 60;
				lastSeenSeconds = lastSeenSeconds - (lastSeenMinutes * 60);
			}

			if (lastSeenMinutes > 60)
			{
				lastSeenHours = lastSeenMinutes / 60;
				lastSeenMinutes = lastSeenMinutes - (lastSeenHours * 60);
			}

			if (lastSeenHours > 24)
			{
				lastSeenDays = lastSeenHours / 24;
				lastSeenHours = lastSeenHours - (lastSeenDays * 24);
			}

			if (lastSeenDays > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " ( " + playerSteamID + ") was last seen " + lastSeenDays + " days " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
			}
			else if (lastSeenHours > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " ( " + playerSteamID + ") was last seen " + lastSeenHours + " hours and " + lastSeenMinutes + " minutes ago.");
			}
			else if (lastSeenMinutes > 0)
			{
				SendReply(player, Prefix + "The player " + playerName + " ( " + playerSteamID + ") was last seen " + lastSeenMinutes + " minutes ago.");
			}
			else
			{
				SendReply(player, Prefix + "The player " + playerName + " ( " + playerSteamID + ") was last seen " + lastSeenSeconds + " seconds ago.");
			}
		}

		[ChatCommand("mostonline")]
		private void MostOnlineCommand(BasePlayer player, string command, string[] args)
		{
			if (!hasPermission(player, "canUseMostOnline"))
			{
				SendReply(player, Prefix + "You don't have permission to use this command.");

				return;
			}

			Dictionary<string, long> mostOnline = new Dictionary<string, long>();

			foreach (string playerID in playTimeData.Players.Keys)
			{
				mostOnline.Add(playTimeData.Players[playerID].Name, playTimeData.Players[playerID].PlayTime);
			}

			List<KeyValuePair<string, long>> sorted = (from kv in mostOnline orderby kv.Value descending select kv).Take(10).ToList();

			string highscore = "<color=red>Top 10 Most Online</color> \n" +
			"------------------------------ \n" +
			"Rank - Online Time (Days:Hours:Minutes) - <color=red>Player Name</color> \n" +
			"------------------------------ \n";
			int count = 0;
			foreach (KeyValuePair<string, long> kv in sorted)
			{
				count++;
				long daysPlayed = 0;
				long hoursPlayed = 0;
				long minutesPlayed = 0;
				long secondsPlayed = kv.Value;

				string daysPlayedString = "";
				string hoursPlayedString = "";
				string minutesPlayedString = "";

				if (secondsPlayed > 60)
				{
					minutesPlayed = secondsPlayed / 60;
					secondsPlayed = secondsPlayed - (minutesPlayed * 60);
				}

				if (minutesPlayed > 60)
				{
					hoursPlayed = minutesPlayed / 60;
					minutesPlayed = minutesPlayed - (hoursPlayed * 60);
				}

				if (hoursPlayed > 24)
				{
					daysPlayed = hoursPlayed / 24;
					hoursPlayed = hoursPlayed - (daysPlayed * 24);
				}

				if (daysPlayed < 10) { daysPlayedString = "0" + daysPlayed.ToString(); } else { daysPlayedString = daysPlayed.ToString(); }
				if (hoursPlayed < 10) { hoursPlayedString = "0" + hoursPlayed.ToString(); } else { hoursPlayedString = hoursPlayed.ToString(); }
				if (minutesPlayed < 10) { minutesPlayedString = "0" + minutesPlayed.ToString(); } else { minutesPlayedString = minutesPlayed.ToString(); }

				if (count < 10)
				{
					highscore += "  ";
				}

				highscore += count + ". " + daysPlayedString + ":" + hoursPlayedString + ":" + minutesPlayedString + " - <color=red>" + kv.Key + "</color>\n";
			}

			SendReply(player, Prefix + highscore);
		}

		private long FindPlayer(string queriedPlayer)
        {
            long playerSteamID = 0;

            foreach (string playerID in playTimeData.Players.Keys)
            {
                if (playerID == queriedPlayer)
                {
                    playerSteamID = Convert.ToInt64(playerID);
                }
                else if (playTimeData.Players[playerID].Name.Contains(queriedPlayer))
                {
                    playerSteamID = Convert.ToInt64(playerID);
                }
            }

            return playerSteamID;
        }

        private static long GrabCurrentTimestamp()
        {
            long timestamp = 0;
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000;
            timestamp = ticks;

            return timestamp;
        }

        bool hasPermission(BasePlayer player, string perm)
        {
            if (player.net.connection.authLevel > 1)
            {
                return true;
            }

            return permission.UserHasPermission(player.userID.ToString(), perm);
        }
    }
}
