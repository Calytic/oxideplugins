using System;
using System.Collections.Generic;

using Oxide.Core;
using Rust;
using Network;

// TODO:
// --- /zcmd edit
// ??? add sayto console command
// --- optimize?

namespace Oxide.Plugins
{
	[Info("Zone Command", "deer_SWAG", "0.0.22", ResourceId = 1254)]
	[Description("Executes commands when player is entering a zone")]
	public class ZoneCommand : RustPlugin
	{
		private readonly string dbName = "ZoneCommand";

		class StoredData
		{
			public HashSet<Zone> Zones = new HashSet<Zone>();

			public StoredData() {}
		}

		class Zone
		{
			public string ID;
			public int 	  Method; // 0 - none, 1 - once, 2 - once per player
			public bool   Executed;
			public HashSet<string> Commands = new HashSet<string>();
			public HashSet<ulong>  Players  = new HashSet<ulong>();

			public Zone() {}
			public Zone(string id) {ID = id;}
		}

		private StoredData data;

		private void OnServerInitialized()
		{
			LoadData();

			var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
			if(!plugins.Exists("ZoneManager"))
				PrintWarning("You have to install ZoneManager to make this plugin works");
		}

		protected override void LoadDefaultConfig()
		{
			Config.Clear();

			CreateCheckConfig();

			Puts("Default config was saved and loaded");
		}

		void Loaded()
		{
			CreateCheckConfig();
		}

		void Unload()
		{
			SaveData();
		}
		

		void OnEnterZone(string ZoneID, BasePlayer player)
		{
			bool execute = true;
			bool add = false;

			if(data.Zones.Count > 0)
			{
				foreach(Zone z in data.Zones)
					if(z.ID == ZoneID && !z.Executed)
					{
						if(z.Method == 1)
							z.Executed = true;
						else if(z.Method == 2)
							if(z.Players.Count > 0)
								foreach(ulong id in z.Players)
									if(id == player.userID)
										execute = false;
									else
										add = true;
							else
								add = true;

						if(add)
							z.Players.Add(player.userID);

						if(execute)
							foreach(string s in z.Commands)
								ConsoleSystem.Run.Server.Normal(s.Replace("$player.id", player.userID.ToString())
														   		.Replace("$player.name", player.displayName)
														   		.Replace("$player.x", player.transform.position.x.ToString())
														   		.Replace("$player.y", player.transform.position.y.ToString())
														   		.Replace("$player.z", player.transform.position.z.ToString()));
					}
			}
		}

		void SendHelpText(BasePlayer player)
		{
			if(player.IsAdmin())
				PrintToChat(player, (string)Config["Messages", "HelpText"]);
		}

		[ChatCommand("zcmd")]
		private void cmdChat(BasePlayer player, string command, string[] args)
		{
			if(player.IsAdmin())
			if(args.Length > 0)
			{
				string arg0 = args[0];
				if(arg0 == "add" || arg0 == "a")
				{
					if(args.Length > 1)
						if(args.Length > 2)
							if(args[2] == "once" || args[2] == "o")
								AddZone(player, args, 1);
							else if(args[2] == "perplayer" || args[2] == "pp")
								AddZone(player, args, 2);
							else
								AddZone(player, args);
						else
							PrintToChat(player, (string)Config["Messages", "EnterCommands"]);
					else
						PrintToChat(player, (string)Config["Messages", "EnterID"]);
				}
				else if(arg0 == "remove" || arg0 == "r")
				{
					if(args.Length > 1)
					{
						Zone removed = null;

						foreach(Zone z in data.Zones)
							if(args[1] == z.ID && removed == null)
								removed = z;

						if(removed != null)
						{
							data.Zones.Remove(removed);
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

					if(data.Zones.Count > 0)
					{
						foreach(Zone z in data.Zones)
						{
							message = z.ID;

							if(z.Method == 1)
							{
								message += " (once";

								if(z.Executed)
									message += ", executed):\n";
								else
									message += "):\n";
							}
							else if(z.Method == 2)
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
					data.Zones.Clear();
					data.Zones = new HashSet<Zone>();
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

		private void AddZone(BasePlayer player, string[] args, int method = 0)
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

				zone.Commands.Add(cmd.Substring(0, cmd.Length - 1));
			}

			data.Zones.Add(zone);

			SaveData();

			PrintToChat(player, (string)Config["Messages", "Added"]);
		}

		private void CreateCheckConfig()
		{
			if(Config["Messages", "HelpText"] == null)
				Config["Messages", "HelpText"] = "ZoneCommand (once/perplayer is optional):\n" +
												 "/zcmd add <zoneID> [once/perplayer] <command>" +
												 "/zcmd add <zoneID> [once/perplayer] [command1, command2, ...]\n" +
												 //"/zcmd edit <zoneID>" +
												 "/zcmd remove <zoneID>\n" +
												 "/zcmd list";

			if(Config["Messages", "AvailableVars"] == null)
				Config["Messages", "AvailableVars"] = "Available variables: $player.id, $player.name, $player.x, $player.y, $player.z";

			if(Config["Messages", "UsableCommands"] == null)
				Config["Messages", "UsableCommands"] = "Available commands: add, remove, list, clear, vars";

			if(Config["Messages", "EnterID"] == null)
				Config["Messages", "EnterID"] = "You must enter ID of zone that you want to remove/add";

			if(Config["Messages", "EnterCommands"] == null)
				Config["Messages", "EnterCommands"] = "You must enter at least one command";

			if(Config["Messages", "Removed"] == null)
				Config["Messages", "Removed"] = "Zone with commands has been removed!";

			if(Config["Messages", "NotFound"] == null)
				Config["Messages", "NotFound"] = "Zone was not found";

			if(Config["Messages", "Added"] == null)
				Config["Messages", "Added"]	= "Zone with commands was successfully added";

			if(Config["Messages", "Clear"] == null)
				Config["Messages", "Clear"] = "All zones with commands were deleted";

			SaveConfig();
		}

		private void LoadData()
		{
			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(dbName);
		}

		private void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(dbName, data);
		}

		public string[] HaveBrackets(string[] arr, int offset)
		{
			string str = "";

			for(int i = offset; i < arr.Length; i++)
				str += arr[i] + " ";

			str = str.Substring(0, str.Length - 1);

			int start = str.IndexOf("[");
			int end = str.LastIndexOf("]");

			if(start == -1 && end == -1)
				return null;

			str = str.Substring(start + 1, str.Length - 2);

			return str.Split(',');
		}
	}
}