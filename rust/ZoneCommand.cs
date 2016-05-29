using System;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;

/* ----------- TODO:
	/zcmd a 12345678 pp 10 on "sayto Oh, you, $player.name!" - ten times per every player
	/zcmd a 12345678 10 "say HAHAAHA" - ten times
*/

namespace Oxide.Plugins
{
	[Info("ZoneCommand", "deer_SWAG", "0.0.35", ResourceId = 1254)]
	[Description("Executes the commands when a player is entering a zone")]
	public class ZoneCommand : RustPlugin
	{
		enum Methods { Always, PerPlayer, PerDay, PerGameDay };
		enum Modes	 { OnEnter, OnExit }

		class StoredData
		{
			public HashSet<Zone> Zones = new HashSet<Zone>();

			public StoredData() { }

			public void Add(Zone zone) => Zones.Add(zone);
			public void Remove(Zone zone) => Zones.Remove(zone);
		}

		class Zone
		{
			public string  Id;
			public Methods Method;
			public Modes   Mode;
			public int	   Amount = -1;
			public List<string> Commands = new List<string>();
			public HashSet<ZonePlayer> Players = new HashSet<ZonePlayer>();

			public Zone() { }
			public Zone(string Id) { this.Id = Id; }

			public void Add(string command) => Commands.Add(command);
			public void Add(ZonePlayer player) => Players.Add(player);
		}

		class ZonePlayer
		{
			public ulong UserId;
			public int Count;

			public ZonePlayer() { }
		}

		[PluginReference]
		Plugin ZoneManager;

		StoredData data;

		/*protected override void LoadDefaultConfig()
		{
			CheckConfig();

			Puts("Default config was saved and loaded");
		}*/

		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>()
			{
				{ "HelpText", @"ZoneCommand:\n
								/zcmd add <zoneID> [once/perplayer] <command>\n
								/zcmd add <zoneID> [once/perplayer] [command1, command2, ...]\n
								/zcmd remove <zoneID>\n
								/zcmd list" },
				{ "AvailableVars",  "Available variables: $player.id, $player.name, $player.xyz, $player.x, $player.y, $player.z" },
				{ "UsableCommands", "Available commands: add, remove, list, clear, vars" },
				{ "ErrorEnterID",       "You must enter ID of zone that you want to remove/add" },
				{ "ErrorEnterCommands", "You must enter at least one command" },
				{ "ErrorNotFound",      "Zone was not found" },
				{ "Added",   "Zone with commands was successfully added!" },
				{ "Removed", "Commands for zone has been removed!" },
				{ "Clear",   "All commands for zones were deleted" }
			}, this);
		}

		void OnServerInitialized()
		{
			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);

			if(data == null)
			{
				RaiseError("Unable to load data file");
				ConsoleSystem.Run.Server.Normal("oxide.unload ZoneCommand");
			}

			if(ZoneManager == null)
				PrintError("You have to install ZoneManager for use this plugin");
		}

		void Loaded()
		{
			LoadDefaultMessages();
			//CheckConfig();
		}

		void Unload()
		{
			SaveData();
		}

		void OnEnterZone(string zoneID, BasePlayer player)
		{
			if (data.Zones.Count > 0)
			{
				foreach (Zone zone in data.Zones)
				{
					if (zone.Id == zoneID && zone.Mode == Modes.OnEnter)
					{
						ExecuteZone(zone, player);
					}
				}
			}
		}

		void OnExitZone(string zoneID, BasePlayer player)
		{
			if (data.Zones.Count > 0)
			{
				foreach (Zone zone in data.Zones)
				{
					if (zone.Id == zoneID && zone.Mode == Modes.OnExit)
					{
						ExecuteZone(zone, player);
					}
				}
			}
		}

		void ExecuteZone(Zone zone, BasePlayer player)
		{
			bool addPlayer = false;
			bool addOne    = false;

			switch (zone.Method)
			{
				case Methods.Always:
					if(zone.Amount != -1)
					{
						if(zone.Amount != zone.Players.Count)
						{
							addPlayer = true;
						}
						else
						{
							return;
						}
					}
					break;
				case Methods.PerPlayer:
					{
						if(zone.Amount != -1)
						{
							if (zone.Players.Count > 0)
							{
								bool found = false;

								foreach (ZonePlayer zp in zone.Players)
								{
									if(zp.UserId == player.userID)
									{
										found = true;

										if(zp.Count == zone.Amount)
										{
											return;
										}
										else
										{
											zp.Count++;
										}

										break;
									}
								}

								if(!found)
								{
									addPlayer = true;
									addOne = true;
								}
							}
							else
							{
								addPlayer = true;
								addOne = true;
							}
						}
						else
						{
							if (zone.Players.Count > 0)
							{
								foreach (ZonePlayer zp in zone.Players)
								{
									if (zp.UserId == player.userID)
									{
										return;
									}
								}

								addPlayer = true;
							}
							else
							{
								addPlayer = true;
							}
						}
					}
					break;
				case Methods.PerDay:
					// TODO: per day
					break;
				case Methods.PerGameDay:
					// TODO: per game day
					break;
			}

			if (addPlayer)
				zone.Add(new ZonePlayer { UserId = player.userID, Count = addOne ? 1 : 0 });

			foreach (string s in zone.Commands)
			{
				string command = s.Replace("$player.id", player.userID.ToString())
									.Replace("$player.name", player.displayName)
									.Replace("$player.xyz", player.transform.position.x + " " + player.transform.position.y + " " + player.transform.position.z)
									.Replace("$player.x", player.transform.position.x.ToString())
									.Replace("$player.y", player.transform.position.y.ToString())
									.Replace("$player.z", player.transform.position.z.ToString());

				if (command.StartsWith("sayto", StringComparison.CurrentCultureIgnoreCase))
				{
					PrintToChat(player, command.Substring(6));
				}

				ConsoleSystem.Run.Server.Normal(command);
			}
		}

		/*
			/zcmd add 3453453 sayto something
			/zcmd add 4564565 always say shit
			/zcmd add 4395342 40 say hiooooouse
			/zcmd add 7563223 always 1 boom $player.x $player.y $player.z
			/zcmd add 1243556 perplayer sayto You win!
			/zcmd add 3534134 perday 30 give giftbox
			/zcmd add 2347534 perplayer 6 say mom, please
		*/

		[ChatCommand("zcmd")]
		void cmdChat(BasePlayer player, string command, string[] args)
		{
			if (player.IsAdmin() && args.Length > 0)
			{
				switch(args[0])
				{
					case "add":
						{
							if (args.Length > 1)
							{
								if (args.Length > 2)
								{
									if (args[2] == "perplayer" || args[2] == "pp") // Per player
										AddZone(player, args, Methods.PerPlayer);
									else if (args[2] == "perday" || args[2] == "pd") // Per day
										AddZone(player, args, Methods.PerDay);
									else if (args[2] == "pergameday" || args[2] == "pgd") // Per game day
										AddZone(player, args, Methods.PerGameDay);
									else if (args[2] == "always")					// Always
										AddZone(player, args, Methods.Always);
									else if (IsDigitsOnly(args[2]))					// Always with amount
										AddZone(player, args, Methods.Always, true, false);
									else
										AddZone(player, args, Methods.Always, false, false); // Always
								}
								else
								{
									PrintToChat(player, Lang("ErrorEnterCommands"));
								}
							}
							else
							{
								PrintToChat(player, Lang("ErrorEnterID"));
							}
						}
						break;
					case "remove":
						{
							if (args.Length > 1)
							{
								RemoveZone(player, args);
							}
							else
							{
								PrintToChat(player, Lang("ErrorEnterID"));
							}
						}
						break;
					case "copy":
						// TODO: copy commands to another zone
						break;
					case "list":
						{
							PrintZoneList(player);
						}
						break;
					case "clear":
						{
							data.Zones.Clear();
							SaveData();
							PrintToChat(player, Lang("Clear"));
						}
						break;
					case "vars":
						PrintToChat(player, Lang("AvailableVars"));
						break;
					default:
						PrintToChat(player, Lang("UsableCommands"));
						break;
				}
			}
			else
			{
				PrintToChat(player, Lang("HelpText"));
			}
		}

		[ConsoleCommand("zone.command")] // TODO: console command
		void cmdConsole(ConsoleSystem.Arg arg)
		{
			//Puts("currently only from chat");
		}

		void AddZone(BasePlayer player, string[] args, Methods method, bool onlyAmount = false, bool hasMethod = true)
		{
			Zone zone = new Zone(args[1]);
			zone.Method = method;

			// zcmd add 43345345 always 8 say

			int offset = 2;

			if (onlyAmount)
			{
				zone.Amount = int.Parse(args[2]);
				offset++;
			}
			else
			{
				if (hasMethod)
					offset++;

				if (IsDigitsOnly(args[3]))
				{
					zone.Amount = int.Parse(args[3]);
					offset++;
				}
			}
			

			string cmd = "";
			for (int i = offset; i < args.Length; i++)
				cmd += args[i] + " ";

			zone.Add(cmd.Substring(0, cmd.Length - 1));

			data.Add(zone);

			/*
			

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
			}*/

			SaveData();

			PrintToChat(player, Lang("Added"));
		}

		void RemoveZone(BasePlayer player, string[] args)
		{
			Zone removed = null;

			foreach (Zone z in data.Zones)
				if (args[1] == z.Id && removed == null)
					removed = z;

			if (removed != null)
			{
				data.Remove(removed);
				SaveData();
				PrintToChat(player, Lang("Removed"));
			}
			else
			{
				PrintToChat(player, Lang("ErrorNotFound"));
			}
		}

		void PrintZoneList(BasePlayer player)
		{
			string message = "Zones with commands:\n";

			if (data.Zones.Count > 0)
			{
				foreach (Zone z in data.Zones)
				{
					message = z.Id;

					/*if (z.Method == Methods.Once)
					{
						message += " (once";

						if (z.Executed)
							message += ", executed):\n";
						else
							message += "):\n";
					}
					else if (z.Method == Methods.PerPlayer)
					{
						message += " (ones per player)";
					}*/

					message += ":\n";

					foreach (string s in z.Commands)
						message += s + "\n";
				}
				message = message.Substring(0, message.Length - 1);
			}

			PrintToChat(player, message);
		}

		void SendHelpText(BasePlayer player)
		{
			if(player.IsAdmin())
				PrintToChat(player, Lang("HelpText"));
		}

		/*private void CheckConfig()
		{
			// ...

			SaveConfig();
		}*/

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(Title, data);
		}

		// -------------------- PLAYER FIND FUNCTIONS --------------------
		// ---------------------------------------------------------------

		BasePlayer FindPlayer(string nameOrId)
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

		BasePlayer FindPlayerByName(string name)
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

		void ConfigItem(string name, object defaultValue)
		{
			Config[name] = Config[name] ?? defaultValue;
		}

		string Lang(string key)
		{
			return lang.GetMessage(key, this);
		}

		bool IsDigitsOnly(string str)
		{
			foreach (char c in str)
				if (c < '0' || c > '9')
					return false;
			return true;
		}

		string[] HaveBrackets(string[] arr, int offset) // TODO: this weird shit
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