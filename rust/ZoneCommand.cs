using System;
using System.Collections.Generic;

using Oxide.Core;
using Rust;
using Network;

/* TODO:
// --- /zcmd edit

!!!	the executes a command when all players leave a zone would be nice too

// Hmmmm...

	always - a, once o, perplayer - pp, perday - pd
	onexit - ox, onenter - on

	/zcmd a 12345678 pp 10 on "sayto Oh, you, $player.name!" - ten times per every player
	/zcmd a 12345678 10 "say HAHAAHA" - ten times
*/

namespace Oxide.Plugins
{
	[Info("Zone Command", "deer_SWAG", "0.0.25", ResourceId = 1254)]
	[Description("Executes commands when player is entering a zone")]
	public class ZoneCommand : RustPlugin
	{
		public const string databaseName = "ZoneCommand";

		private enum Methods { Always, Once, PerPlayer, PerDay };
		private enum Modes	 { OnEnter, OnExit }

		private class StoredData
		{
			public HashSet<Zone> Zones = new HashSet<Zone>();

			public StoredData() { }

			public void Add(Zone zone) => Zones.Add(zone);
			public void Remove(Zone zone) => Zones.Remove(zone);
		}

		private class Zone
		{
			public string  Id;
			public Methods Method;
			public Modes   Mode;
			public bool    Executed;
			public int	   Amount;
			public HashSet<string> Commands = new HashSet<string>();
			public HashSet<ulong>  Players  = new HashSet<ulong>();

			public Zone() { }
			public Zone(string Id) { this.Id = Id; }

			public void Add(string command) => Commands.Add(command);
			public void Add(ulong player) => Players.Add(player);
		}

		private StoredData _data;

		private void OnServerInitialized()
		{
			_data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(databaseName);

			if(_data == null)
			{
				PrintError("Unable to load data file");
				ConsoleSystem.Run.Server.Normal("oxide.unload ZoneCommand");
			}

			if(!IsPluginExists("ZoneManager"))
				PrintWarning("You have to install ZoneManager for use this plugin");
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();

			CheckConfig();

			Puts("Default config was saved and loaded");
		}

		private void OnPluginLoaded()
		{
			CheckConfig();
		}

		private void Unload()
		{
			SaveData();
		}

		void OnEnterZone(string zoneID, BasePlayer player)
		{
			if (_data.Zones.Count > 0)
			{
				bool execute = true;
				bool add = false;

				foreach (Zone z in _data.Zones)
				{
					if (z.Id == zoneID && z.Mode == Modes.OnEnter && !z.Executed)
					{
						switch(z.Method)
						{
							case Methods.Once:
								z.Executed = true;
								break;
							case Methods.PerPlayer:
								{
									if (z.Players.Count > 0)
										foreach (ulong id in z.Players)
											if (id == player.userID)
												execute = false;
											else
												add = true;
									else
										add = true;

								}
								break;
							case Methods.PerDay:
								// TODO: per day
								break;
							case Methods.Always:
								break;
						}

						if(add)
							z.Add(player.userID);

						if(execute)
						{
							foreach(string s in z.Commands)
							{
								string command = s.Replace("$player.id", player.userID.ToString())
												  .Replace("$player.name", player.displayName)
												  .Replace("$player.x", player.transform.position.x.ToString())
												  .Replace("$player.y", player.transform.position.y.ToString())
												  .Replace("$player.z", player.transform.position.z.ToString());

								if(command.StartsWith("sayto", StringComparison.CurrentCultureIgnoreCase))
								{
									PrintToChat(player, command.Substring(6));
								}

								ConsoleSystem.Run.Server.Normal(command);
							}
						}
					}
				} // foreach zones
			}
		}

		void OnExitZone(string zoneID, BasePlayer player)
		{
			
		}

		private void ExecuteCommands()
		{

		}

		[ChatCommand("zcmd")]
		private void cmdChat(BasePlayer player, string command, string[] args)
		{
			if (player.IsAdmin())
			if (args.Length > 0)
			{
				string arg0 = args[0];
				if(arg0 == "add" || arg0 == "a")
				{
					if(args.Length > 1)
						if(args.Length > 2)
							if(args[2] == "once" || args[2] == "o")
								AddZone(player, args, Methods.Once);
							else if(args[2] == "perplayer" || args[2] == "pp")
								AddZone(player, args, Methods.PerPlayer);
							else
								AddZone(player, args);
						else
							PrintToChat(player, (string)Config["Messages", "EnterCommands"]);
					else
						PrintToChat(player, (string)Config["Messages", "EnterID"]);
				}
				else if(arg0 == "remove" || arg0 == "r" || arg0 == "delete" || arg0 == "d")
				{
					if(args.Length > 1)
					{
						Zone removed = null;

						foreach(Zone z in _data.Zones)
							if(args[1] == z.Id && removed == null)
								removed = z;

						if(removed != null)
						{
							_data.Remove(removed);
							SaveData();
							PrintToChat(player, (string)Config["Messages", "Removed"]);
						}
						else
						{
							PrintToChat(player, (string)Config["Messages", "NotFound"]);
						}
					}
					else
					{
						PrintToChat(player, (string)Config["Messages", "EnterID"]);
					}
				}
				else if(arg0 == "list" || arg0 == "l")
				{
					string message = "Zones with commands:\n";

					if(_data.Zones.Count > 0)
					{
						foreach(Zone z in _data.Zones)
						{
							message = z.Id;

							if(z.Method == Methods.Once)
							{
								message += " (once";

								if(z.Executed)
									message += ", executed):\n";
								else
									message += "):\n";
							}
							else if(z.Method == Methods.PerPlayer)
							{
								message += " (ones per player)";
							}

							message += ":\n";

							foreach(string s in z.Commands)
								message += s + "\n";
						}
						message = message.Substring(0, message.Length - 1);
					}

					PrintToChat(player, message);
				}
				else if(arg0 == "edit" || arg0 == "e")
				{

				}
				else if(arg0 == "clear" || arg0 == "c")
				{
					_data.Zones.Clear();
					_data.Zones = new HashSet<Zone>();
					SaveData();
					PrintToChat(player, (string)Config["Messages", "Clear"]);
				}
				else if(arg0 == "vars" || arg0 == "v")
				{
					PrintToChat(player, (string)Config["Messages", "AvailableVars"]);
				}
				else
				{
					PrintToChat(player, (string)Config["Messages", "UsableCommands"]);
				}
			}
			else
			{
				PrintToChat(player, (string)Config["Messages", "HelpText"]);
			}
		}

		[ConsoleCommand("zcmd")]
		private void cmdConsole(ConsoleSystem.Arg arg)
		{
			Puts("currently only from chat");
		}

		private void AddZone(BasePlayer player, string[] args, Methods method = Methods.Always)
		{
			Zone zone = new Zone(args[1]);
			zone.Method = method;

			int offset = 2;

			if(method > 0)
				offset++;

			object withBrackets = HaveBrackets(args, offset);

			if(withBrackets != null)
			{
				string[] s = withBrackets as string[];

				for(int i = 0; i < s.Length; i++)
				{
					if(s[i][0] == ' ')
						s[i] = s[i].Substring(1);
					if(s[i][s[i].Length - 1] == ' ')
						s[i] = s[i].Substring(0, s[i].Length - 1);

					zone.Commands.Add(s[i]);
				}
			}
			else
			{
				string cmd = "";
				for(int i = offset; i < args.Length; i++)
					cmd += args[i] + " ";

				zone.Add(cmd.Substring(0, cmd.Length - 1));
			}

			_data.Add(zone);

			SaveData();

			PrintToChat(player, (string)Config["Messages", "Added"]);
		}

		private void PrintList()
		{
			
		}

		void SendHelpText(BasePlayer player)
		{
			if(player.IsAdmin())
				PrintToChat(player, (string)Config["Messages", "HelpText"]);
		}

		private void CheckConfig()
		{
			ConfigItem("Messages", "HelpText", "ZoneCommand (once/perplayer is optional):\n/zcmd add <zoneID> [once/perplayer] <command>\n" +
											   "/zcmd add <zoneID> [once/perplayer] [command1, command2, ...]\n/zcmd remove <zoneID>\n/zcmd list");
			ConfigItem("Messages", "AvailableVars", "Available variables: $player.id, $player.name, $player.x, $player.y, $player.z");
			ConfigItem("Messages", "UsableCommands", "Available commands: add, remove, list, clear, vars");
			ConfigItem("Messages", "EnterID", "You must enter ID of zone that you want to remove/add");
			ConfigItem("Messages", "EnterCommands", "You must enter at least one command");
			ConfigItem("Messages", "Removed", "Zone with commands has been removed!");
			ConfigItem("Messages", "NotFound", "Zone was not found");
			ConfigItem("Messages", "Added", "Zone with commands was successfully added");
			ConfigItem("Messages", "Clear", "All zones with commands were deleted");

			SaveConfig();
		}

		private void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(databaseName, _data);
		}

		// -------------------- PLAYER FIND FUNCTIONS --------------------
		// ---------------------------------------------------------------

		private BasePlayer FindPlayer(string nameOrId)
		{
			if (IsDigitsOnly(nameOrId))
			{
				ulong id = Convert.ToUInt64(nameOrId);
				BasePlayer player = BasePlayer.FindByID(id);

				if (player == null)
					return FindPlayerByName(nameOrId);
				else
					return player;
			}
			else
			{
				return FindPlayerByName(nameOrId);
			}
		}

		private BasePlayer FindPlayerByName(string name)
		{
			BasePlayer player = null;
			int length = BasePlayer.activePlayerList.Count;

			if (length != 0)
			for (int i = 0; i < length; i++)
			if (BasePlayer.activePlayerList[i].displayName.Equals(name, StringComparison.CurrentCulture))
			{
				player = BasePlayer.activePlayerList[i];
				break;
			}

			return player;
		}

		// ----------------------------- UTILS -----------------------------
		// -----------------------------------------------------------------

		private void ConfigItem(string name1, string name2, object defaultValue)
		{
			Config[name1, name2] = Config[name1, name2] ?? defaultValue;
		}

		private bool IsDigitsOnly(string str)
		{
			foreach (char c in str)
				if (c < '0' || c > '9')
					return false;
			return true;
		}

		private bool IsPluginExists(string name)
		{
			return Interface.GetMod().GetLibrary<Oxide.Core.Libraries.Plugins>("Plugins").Exists(name);
		}

		private string[] HaveBrackets(string[] arr, int offset)
		{
			string str = "";

			for (int i = offset; i < arr.Length; i++)
				str += arr[i] + " ";

			str = str.Substring(0, str.Length - 1);

			int start = str.IndexOf("[");
			int end = str.LastIndexOf("]");

			if (start == -1 && end == -1)
				return null;

			str = str.Substring(start + 1, str.Length - 2);

			return str.Split(',');
		}
	}
}