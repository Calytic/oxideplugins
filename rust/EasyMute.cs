using Oxide.Core;
//using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
//using System.Reflection;
//using System.Linq;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
	[Info("EasyMute", "4seti [Lunatiq] for Rust Planet", "0.2.0", ResourceId = 730)]
	class EasyMute : RustPlugin
	{
		#region Utility Methods
		private void Log(string message) => Puts("{0}: {1}", Title, message);
		private void Warn(string message) => PrintWarning("{0}: {1}", Title, message);
		private void Error(string message) => PrintError("{0}: {1}", Title, message);		
		void ReplyChat(BasePlayer player, string msg) => player.ChatMessage(string.Format("<color=#81D600>{0}</color>: {1}", ReplyName, msg));
		void Loaded() => Log("Loaded");
		void Unload() => SaveData();
		private T GetConfig<T>(string name, T defaultValue)
		{
			if (Config[name] == null)
				return defaultValue;
			return (T)Convert.ChangeType(Config[name], typeof(T));
		}
		#endregion

		#region Vars
		IDictionary<ulong, Muted> MuteList = new Dictionary<ulong, Muted>();
		Oxide.Core.Libraries.Time time = new Oxide.Core.Libraries.Time();
		string ReplyName = "EasyMute";
		string DefaultReason = "Bad Language";

        private Dictionary<string, string> Messages = new Dictionary<string, string>();
        private Dictionary<string, string> defMsg = new Dictionary<string, string>()
		{
			["mutedReply"] = "You are muted for: <color=#F23F3F>{0}</color> time left: <color=#F23F3F>{1}</color>",
			["playerMuted"] = " <color=#81F23F>{0}</color> was muted for <color=#81F23F>{1}</color> in case of <color=#81F23F>{2}</color>",
			["playerUnMuted"] = "<color=#81F23F>{0}</color> was unmuted",
			["playerNotMuted"] = "Player <color=#81F23F>{0}</color> wasn't muted at all",
			["noOneFound"] = "No players with that name found",
			["tooMuchFound"] = "Too much matches for that name:",
			["alreadyMuted"] = "Player <color=#F23F3F>{0}</color> muted already!",
			["usage"] = "USAGE: <color=#81F23F>/mute <Name> <time:#D#H#M> <Reason:Optional></color>",
			["usageUn"] = "USAGE: <color=#81F23F>/unmute <Name></color>",
			["listEntry"] = "<color=#F5D400>{0}[{1}]</color> for <color=#F23F3F>{2}</color>  time left: <color=#81F23F>{3}</color> ({4})"
		};
		#endregion


		void OnServerInitialized()
		{
			LoadConfig();
			var version = GetConfig<Dictionary<string, object>>("version", null);
			VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
			permission.RegisterPermission("canmute", this);

			Messages = new Dictionary<string, string>();
			var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);
			if (cfgMessages != null)
				foreach (var pair in cfgMessages)
					Messages[pair.Key] = Convert.ToString(pair.Value);

			if (verNum < Version || defMsg.Count > Messages.Count)
			{
				//placeholder for future version updates
				foreach (var pair in defMsg)
					if (!Messages.ContainsKey(pair.Key))
						Messages[pair.Key] = pair.Value;

				Config["messages"] = Messages;
				Config["version"] = Version;
				Warn("Config version updated to: " + Version.ToString() + " please check it");
				SaveConfig();
			}

			ReplyName = GetConfig<string>("ReplyName", ReplyName);
			DefaultReason = GetConfig<string>("DefaultReason", DefaultReason);
			LoadData();
        }
		protected override void LoadDefaultConfig()
		{
			Log("Creating a new config file");
			Config.Clear();
			LoadVariables();
		}
		void LoadVariables()
		{
			Config["messages"] = defMsg;
			Config["version"] = Version;
			Config["ReplyName"] = ReplyName;		
			Config["DefaultReason"] = DefaultReason;
		}
		object OnPlayerChat(ConsoleSystem.Arg arg)
		{
			BasePlayer player = null;
			string msg = "";
			try
			{
				if (arg == null) return null;
				if (arg.connection.player == null) return null;

				if (arg.connection.player is BasePlayer)
				{
					player = arg.connection.player as BasePlayer;
					if (player.net.connection.authLevel > 0) return null;
				}
				else return null;

				msg = arg.GetString(0, "text").ToLower();

				if (msg == null) return null;
				else if (msg == "") return null;
				else if (msg.Substring(0, 1).Equals("/") || msg.Substring(0, 1).Equals("!")) return null;

				if (player == null) return null;
			}
			catch
			{
				return null;
			}
			uint stamp = time.GetUnixTimestamp();
            if (MuteList.ContainsKey(player.userID))
			{
				if (MuteList[player.userID].EndTime > stamp)
				{
					ReplyChat(player, string.Format(Messages["mutedReply"], MuteList[player.userID].Reason, getUnixTimeToString(MuteList[player.userID].EndTime - stamp)));
					return false;
				}
				else
					MuteList.Remove(player.userID);
			}
			return null;
		}

		private string getUnixTimeToString(uint timeDif)
		{
			string strTime = string.Empty;
			if (timeDif >= 86400)
			{
				strTime += ((timeDif - timeDif % 86400) / 86400) + "D";
				timeDif = timeDif % 86400;
            }
			if (timeDif >= 3600)
			{
				strTime += ((timeDif - timeDif % 3600) / 3600) + "H";
				timeDif = timeDif % 3600;
			}
			if (timeDif >= 60)
			{
				strTime += ((timeDif - timeDif % 60) / 60) + "M";
				timeDif = timeDif % 60;
			}
			if (timeDif > 0)
			{
				strTime += timeDif + "S";
			}

			return strTime;
		}

		private bool stringTimeToUnixTimeStapm(string timeString, ref uint stamp)
		{
			string patern = @"(\d*)[dhm]";
			Regex regex = new Regex(patern, RegexOptions.IgnoreCase);
			Match match = regex.Match(timeString);
			stamp = time.GetUnixTimestamp();
            if (match.Success)
			{
				while (match.Success)
				{
					if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "d")
					{
						stamp += uint.Parse(match.Groups[1].Value) * 24 * 60 * 60;
                    }
					else if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "h")
					{
						stamp += uint.Parse(match.Groups[1].Value) * 60 * 60;
					}
					else if (match.ToString().ToLower().Replace(match.Groups[1].Value, string.Empty) == "m")
					{
						stamp += uint.Parse(match.Groups[1].Value) * 60;
					}
					match = match.NextMatch();
                }
                return true;
			}
			return false;
		}

		private List<BasePlayer> FindPlayerByName(string playerName = "")
		{
			// Check if a player name was supplied.
			if (playerName == "") return null;

			// Set the player name to lowercase to be able to search case insensitive.
			//playerName = playerName.ToLower();

			// Setup some variables to save the matching BasePlayers with that partial
			// name.

			List<BasePlayer> entityArray = BaseEntity.Util.FindTargets(playerName, true).Cast<BasePlayer>().ToList();

			// Iterate through the online player list and check for a match.
			//foreach (var player in entityArray)
			//{
			//	// Get the player his/her display name and set it to lowercase.
			//	string displayName = player.displayName.ToLower();

			//	// Look for a match.
			//	if (displayName.Contains(playerName))  //&& player.net.connection.authLevel == 0
   //             {
			//		matches.Add(player);
			//	}
			//}



			// Return all the matching players.
			return entityArray;
		}

		[ChatCommand("mute")]
		void cmdMute(BasePlayer player, string cmd, string[] args)
		{
			if (!HasPerm(player, "canmute")) return;
			if (args.Length < 2)
			{
				ReplyChat(player, Messages["usage"]);
                return;
			}
			
			string reason = args.Length > 2 ? args[2] : DefaultReason;

			DoMute(player, args[0], args[1], reason);
		}

		[ConsoleCommand("mute")]
		void cmdConsoleMute(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				if (arg.connection.authLevel < 1)
				{
					SendReply(arg, "You dont have access to this command");
					return;
				}
			}
			if (arg.Args.Length < 2)
			{
				SendReply(arg, Messages["usage"]);
				return;
			}

			string reason = arg.Args.Length > 2 ? arg.Args[2] : DefaultReason;

			DoMute(null, arg.Args[0], arg.Args[1], reason, false, arg);
		}

		void DoMute(BasePlayer player, string name, string time, string reasonGiven, bool chat = true, ConsoleSystem.Arg console = null)
		{
			BasePlayer target;
			var pList = FindPlayerByName(name);
			if (pList.Count == 0)
			{
				if (chat)
					ReplyChat(player, Messages["noOneFound"]);
				else
					SendReply(console, Messages["noOneFound"]);
				return;
			}
			else if (pList.Count > 1)
			{
				if (chat)
					ReplyChat(player, Messages["tooMuchFound"]);
				else
					SendReply(console, Messages["tooMuchFound"]);

				int i = 0;
				foreach (var p in pList)
				{
					if (chat)
						ReplyChat(player, string.Format("<color=#F5D400>[{0}]</color> - {1}", i, p.displayName));
					else
						SendReply(console, string.Format("<color=#F5D400>[{0}]</color> - {1}", i, p.displayName));
					i++;
				}
				return;
			}
			else
			{
				target = pList.First();
			}
			uint muteTime = 0;
			if (MuteList.ContainsKey(target.userID))
			{
				if (chat)
					ReplyChat(player, string.Format(Messages["alreadyMuted"], target.displayName));
				else
					SendReply(console, string.Format(Messages["alreadyMuted"], target.displayName));
				return;
			}
			else
			{
				if (stringTimeToUnixTimeStapm(time, ref muteTime))
				{
					MuteList.Add(target.userID, new Muted(target.displayName, muteTime, reasonGiven, player == null ? "Console" : player.displayName));
					if (chat)
						ReplyChat(player, string.Format(Messages["playerMuted"], target.displayName, time, reasonGiven));
					else
						SendReply(console, string.Format(Messages["playerMuted"], target.displayName, time, reasonGiven));
				}
			}
		}
		
		[ChatCommand("unmute")]
		void cmdUnMute(BasePlayer player, string cmd, string[] args)
		{
			if (!HasPerm(player, "canmute")) return;
			if (args.Length < 1)
			{
				ReplyChat(player, Messages["usageUn"]);
				return;
			}
			DoUnmute(player, args[0]);
		}

		[ConsoleCommand("unmute")]
		void cmdConsoleUnMute(ConsoleSystem.Arg arg)
		{
			if (arg.connection != null)
			{
				if (arg.connection.authLevel < 1)
				{
					SendReply(arg, "You dont have access to this command");
					return;
				}
			}
			if (arg.Args.Length == 0)
			{
				SendReply(arg, Messages["usageUn"]);
				return;
			}

			DoUnmute(null, arg.Args[0], false, arg);
		}

		void DoUnmute(BasePlayer player, string name, bool chat = true, ConsoleSystem.Arg console = null)
		{
			KeyValuePair<ulong, string> target;
			var pList = MuteList.Where(x => x.Value.Name.Contains(name, System.Globalization.CompareOptions.IgnoreCase)).ToDictionary(k => k.Key, k => k.Value.Name);
			if (pList.Count == 0)
			{
				if (chat)
					ReplyChat(player, Messages["noOneFound"]);
				else
					SendReply(console, Messages["noOneFound"]);
				return;
			}
			else if (pList.Count > 1)
			{
				if (chat)
					ReplyChat(player, Messages["tooMuchFound"]);
				else
					SendReply(console, Messages["tooMuchFound"]);
				int i = 0;
				foreach (var p in pList)
				{
					if (chat)
						ReplyChat(player, string.Format("<color=#F5D400>[{0}]</color> - {1}", i, p.Value));
					else
						SendReply(console, string.Format("<color=#F5D400>[{0}]</color> - {1}", i, p.Value));
					i++;
				}
				return;
			}
			else
			{
				target = pList.First();
			}
			if (MuteList.ContainsKey(target.Key))
			{
				MuteList.Remove(target.Key);
				if (chat)
					ReplyChat(player, string.Format(Messages["playerUnMuted"], target.Value));
				else
					SendReply(console, string.Format(Messages["playerUnMuted"], target.Value));
				return;
			}
			else
			{
				if (chat)
					ReplyChat(player, string.Format(Messages["playerNotMuted"], target.Value));
				else
					SendReply(console, string.Format(Messages["playerNotMuted"], target.Value));
				return;
			}
		}
		bool HasPerm(BasePlayer p, string pe)
		{
			if (p.net.connection.authLevel > 0) return true;
			else return permission.UserHasPermission(p.userID.ToString(), pe);

		}

		[ChatCommand("mutesave")]
		void cmdSave(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			SaveData();
			ReplyChat(player, "Data Saved!");
		}

		[ChatCommand("mutelist")]
		void cmdList(BasePlayer player, string cmd, string[] args)
		{
			if (!HasPerm(player, "canmute")) return;
			List<ulong> cleaningList = new List<ulong>();
			if (MuteList.Count > 0)
			{
				uint stamp = time.GetUnixTimestamp();
				foreach (var entry in MuteList)
				{
					if (stamp < entry.Value.EndTime)
						ReplyChat(player, string.Format(Messages["listEntry"], entry.Value.Name, entry.Key, entry.Value.Reason, getUnixTimeToString(entry.Value.EndTime - stamp), entry.Value.By));
					else
						cleaningList.Add(entry.Key);
                }
				foreach (var key in cleaningList)
				{
					MuteList.Remove(key);
                }
			}
			else
				ReplyChat(player, "NoEntry");
        }

		private void LoadData()
		{
			try
			{
				MuteList = Interface.GetMod().DataFileSystem.ReadObject<IDictionary<ulong, Muted>>("mute-data");
				Log("Old EasyMute data loaded!");
				List<ulong> cleaningList = new List<ulong>();
				if (MuteList.Count > 0)
				{
					uint stamp = time.GetUnixTimestamp();
					foreach (var entry in MuteList)
					{
						if (stamp > entry.Value.EndTime)
							cleaningList.Add(entry.Key);
					}
					if (cleaningList.Count > 0)
					{
						foreach (var key in cleaningList)
						{
							MuteList.Remove(key);
						}
						SaveData();
					}
				}
			}
			catch
			{
				MuteList = new Dictionary<ulong, Muted>();
				Warn("New EasyMute Data file initiated!");
				SaveData();
			}			
		}

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject<IDictionary<ulong, Muted>>("mute-data", MuteList);
			Log("Data saved!");
		}


		private class Muted
		{
			public uint EndTime = 0;
			public string Reason = string.Empty;
			public string Name = string.Empty;
			public string By = string.Empty;
			public Muted(string name, uint endTime, string reason, string by)
			{
				Name = name;
				EndTime = endTime;
				Reason = reason;
				By = by;
			}
		}

	}
}