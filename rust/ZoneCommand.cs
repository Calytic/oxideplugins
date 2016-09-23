using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Oxide.Core;

using UnityEngine;

/* --- Do not edit anything here if you don't know what are you doing --- */

namespace Oxide.Plugins
{
	[Info("ZoneCommand", "deer_SWAG", "0.1.0", ResourceId = 1254)]
	[Description("Executes the commands when a player is entering a zone")]
	class ZoneCommand : RustPlugin
	{
		enum Methods { Once, Always, PerPlayer, PerDay, PerGameDay, PerGame }
		enum Modes	 { OnEnter, OnExit }

		const string PermissionName = "zonecommand.use";

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
			public string  UserGroup;
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

		StoredData data;

		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>()
			{
				{ "HelpText", "ZoneCommand:\n" +
								"/zcmd add <zoneID> {command} {command} ...\n" +
								"/zcmd remove <zoneID>\n" +
								"/zcmd list" },
				{ "AvailableVars",  "Available variables: $player.id, $player.name, $player.xyz, $player.x, $player.y, $player.z" },
				{ "UsableCommands", "Available commands: add, remove, list, clear, vars" },
				{ "ErrorEnterID",       "You must enter ID of zone" },
				{ "ErrorEnterCommands", "You must enter at least one command" },
				{ "ErrorNotFound",      "Zone was not found" },
				{ "Added",   "Zone with commands was successfully added!" },
				{ "Removed", "Commands for zone has been removed!" },
				{ "Clear",   "All commands for zones were deleted" },
				{ "List",    "Zones with commands:\n" },
				{ "Unpermitted", "You do not have a permission to use this command" }
			}, this);
		}

		void OnServerInitialized()
		{
			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);

			if(data == null)
			{
				RaiseError("Unable to load data file");
				rust.RunServerCommand("oxide.unload " + Title);
			}

			if(!IsPluginExists("ZoneManager") || !IsPluginExists("RectZones"))
				RaiseError("You need to install ZoneManager or RectZones to use this plugin");
		}

		void Loaded()
		{
			LoadDefaultMessages();

			permission.RegisterPermission(PermissionName, this);
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
						ExecuteZone(zone, player);
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
						ExecuteZone(zone, player);
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
							addPlayer = true;
						else
							return;
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
											return;
										else
											zp.Count++;

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
										return;
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
					PrintToChat(player, command.Substring(6));

				rust.RunServerCommand(command);
			}
		}
	
		// /zcmd add 81195143 {say hello there} {say okay then}

		[ChatCommand("zcmd")]
		void cmdChat(BasePlayer player, string command, string[] args)
		{
			if (!IsPlayerPermitted(player, PermissionName))
				return;

			if (args.Length > 0)
			{
				string cmdWithArgs = ArrayToString(args);

				QueryLanguage.Lexer lexer = new QueryLanguage.Lexer();

				lexer.Parse(cmdWithArgs);

				QueryLanguage.Parser parser = new QueryLanguage.Parser(lexer.Lexems as List<QueryLanguage.Lexem>);

				QueryLanguage.LexemType type = parser.ParseCommand();

				string id = parser.ParseId();

				switch (type)
				{
					case QueryLanguage.LexemType.AddCmd:
						{
							if (string.IsNullOrEmpty(id))
							{
								PrintToChat(player, Lang("ErrorEnterID", player));
								return;
							}

							AddCommand(parser, id, player);
						}
						break;
					case QueryLanguage.LexemType.RemoveCmd:
						{
							if (string.IsNullOrEmpty(id))
							{
								PrintToChat(player, Lang("ErrorEnterID", player));
								return;
							}

							RemoveCommand(parser, id, player);
						}
						break;
					case QueryLanguage.LexemType.ListCmd:
						ListCommand(parser, player);
						break;
					default:
						PrintToChat(player, Lang("HelpText", player));
						break;
				}
			}
			else
			{
				PrintToChat(player, Lang("HelpText", player));
			}
		}

		[ConsoleCommand("zone.command")] // TODO: console command
		void cmdConsole(ConsoleSystem.Arg arg)
		{
			Puts("currently only from chat");
		}

		void AddCommand(QueryLanguage.Parser parser, string id, BasePlayer player)
		{
			PrintToChat("AddCommand");

			List<string> cmds = parser.ParseCommands();

			if(cmds.Count == 0)
			{
				PrintToChat(player, Lang("ErrorEnterCommands", player));
				return;
			}

			QueryLanguage.Parser.ExecutionAndCount executionAndCount = parser.ParseExecutionAndCount();

			Methods method = Methods.Always;

			switch(executionAndCount.Execution1)
			{
				case QueryLanguage.LexemType.Always: method = Methods.Always; break;
				case QueryLanguage.LexemType.Once: method = Methods.Once; break;
			}

			switch(executionAndCount.Execution2)
			{
				case QueryLanguage.LexemType.Player: method = Methods.PerPlayer; break;
				case QueryLanguage.LexemType.Day: method = Methods.PerDay; break;
				case QueryLanguage.LexemType.Game: method = Methods.PerGame; break;
			}

			if(executionAndCount.Execution2 == QueryLanguage.LexemType.Game && executionAndCount.Execution3 == QueryLanguage.LexemType.Day)
			{
				method = Methods.PerGameDay;
			}

			Zone zone = new Zone(id);
			zone.Commands = new List<string>(cmds);
			zone.Mode = parser.ParseRule() == QueryLanguage.LexemType.Exit ? Modes.OnExit : Modes.OnEnter;
			zone.Amount = executionAndCount.Count;
			zone.Method = method;
			zone.UserGroup = parser.ParseUserGroup();

			data.Add(zone);

			SaveData();
			PrintToChat(player, Lang("Added", player));
		}

		void RemoveCommand(QueryLanguage.Parser parser, string id, BasePlayer player)
		{
			if(data.Zones.RemoveWhere(x => x.Id == id) > 0)
			{
				player.ChatMessage(Lang("Removed", player));
			}
			else
			{
				player.ChatMessage(Lang("ErrorNotFound", player));
			}
		}

		void ListCommand(QueryLanguage.Parser parser, BasePlayer player)
		{
			string result = string.Empty;

			foreach (Zone zone in data.Zones)
			{
				result += zone.Id + " (" + (zone.Mode == Modes.OnEnter ? "on enter" : "on exit") + ", " + zone.Method.ToString().ToLower() + "):\n\t";
				
				foreach(string command in zone.Commands)
				{
					result += command + ", ";
				}

				result = result.Substring(0, result.Length - 2);
			}

			if(string.IsNullOrEmpty(result))
			{
				player.ChatMessage(Lang("ErrorNotFound", player));
				return;
			}

			player.ChatMessage(result);
		}

		void AddZone(BasePlayer player, string[] args, Methods method, bool onlyAmount = false, bool hasMethod = true)
		{
			Zone zone = new Zone(args[1]);
			zone.Method = method;

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

			SaveData();
			PrintToChat(player, Lang("Added"));
		}

		void RemoveZone(BasePlayer player, string[] args)
		{
			int removed = data.Zones.RemoveWhere(x => x.Id == args[1]);

			if (removed > 0)
			{
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
			string message = Lang("List");

			if (data.Zones.Count > 0)
			{
				foreach (Zone z in data.Zones)
				{
					message += z.Id;

					switch(z.Mode)
					{
						case Modes.OnEnter:
							message += " (on enter) ";
							break;
						case Modes.OnExit:
							message += " ( on exit) ";
							break;
					}

					switch(z.Method)
					{
						case Methods.Always:
							message += "(always)";
							break;
						case Methods.PerPlayer:
							message += "(per player)";
							break;
						case Methods.PerDay:
							message += "(per day)";
							break;
						case Methods.PerGameDay:
							message += "(per game day)";
							break;
					}

					message += (z.Amount > 0 ? (" (" + z.Amount + ")") : "") + ":\n";

					foreach (string s in z.Commands)
						message += s + "; ";

					message = message.Substring(0, message.Length - 2) + "\n";
				}

				message = message.Substring(0, message.Length - 1);
			}
			else
			{
				message += Lang("ErrorNotFound");
			}

			PrintToChat(player, message);
		}

		void SendHelpText(BasePlayer player)
		{
			if(IsPlayerPermitted(player, PermissionName))
				PrintToChat(player, Lang("HelpText"));
		}

		// ----------------------------- UTILS -----------------------------
		// -----------------------------------------------------------------

		bool IsPluginExists(string name)
		{
			return Interface.GetMod().GetLibrary<Core.Libraries.Plugins>().Exists(name);
		}

		string Lang(string key, BasePlayer player = null)
		{
			return lang.GetMessage(key, this, player?.UserIDString);
		}

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(Title, data);
		}

		bool IsDigitsOnly(string str)
		{
			foreach (char c in str)
				if (c < '0' || c > '9')
					return false;
			return true;
		}

		string ArrayToString(string[] array)
		{
			string result = string.Empty;

			foreach (string s in array)
			{
				result += s + " ";
			}

			return result;
		}

		bool IsPlayerPermitted(BasePlayer player, string permissionName)
		{
			return player.IsAdmin() || permission.UserHasPermission(player.UserIDString, permissionName);
		}

		// ---------------------------- PARSER -----------------------------
		// -----------------------------------------------------------------

		class QueryLanguage
		{
			/*
				add           				--Ë¥
				remove        				--Ë§------ required
				list          				--Ë©

				on                          --------- required for next one

				enter						--Ë¥------ not required
				exit						--Ë©

				123456780     				--------- required

				execute       				--------- required for next two

				always						--Ë¥
				once						--Ë§
				per day						--Ë§
				per game â login/logout		--Ë§------ not required
				per game day				--Ë§
				per player					--Ë©

				x times						--------- not required

				only for x 					--------- user group (admin, player, etc.) (number or string) (not required)

				from x:xx to y:yy			--------- not required

				{command}					--------- required (commands in braces)
			*/

			public enum LexemType
			{
				AddCmd, RemoveCmd, ListCmd,
				Text, StartBrace, EndBrace,
				On, Enter, Exit,
				Execute, Always, Once, Per, Day, Game, Player, Times,
				Only, For,
				From, To,
				Unknown
			}

			public class Lexem
			{
				public LexemType Type;
				public string Value;
				public int Offset;
			}

			class LexemDefenition<T>
			{
				public LexemType Type;
				public T Representation;

				public LexemDefenition(T representation, LexemType type)
				{
					Representation = representation;
					Type = type;
				}
			}

			class DynamicLexemDefenition : LexemDefenition<Regex>
			{
				public DynamicLexemDefenition(string representation, LexemType type) : base(new Regex(representation, RegexOptions.Compiled), type) { }
			}

			class StaticLexemDefenition : LexemDefenition<string>
			{
				public StaticLexemDefenition(string representation, LexemType type) : base(representation, type) { }
			}

			static class LexemDefenitions
			{
				public static StaticLexemDefenition[] Static = new[]
				{
					new StaticLexemDefenition("add", LexemType.AddCmd),
					new StaticLexemDefenition("remove", LexemType.RemoveCmd),
					new StaticLexemDefenition("list", LexemType.ListCmd),

					new StaticLexemDefenition("on", LexemType.On),
					new StaticLexemDefenition("enter", LexemType.Enter),
					new StaticLexemDefenition("exit", LexemType.Exit),

					new StaticLexemDefenition("execute", LexemType.Execute),
					new StaticLexemDefenition("per", LexemType.Per),
					new StaticLexemDefenition("only", LexemType.Only),
					new StaticLexemDefenition("for", LexemType.For),
					new StaticLexemDefenition("times", LexemType.Times),

					new StaticLexemDefenition("always", LexemType.Always),
					new StaticLexemDefenition("once", LexemType.Once),

					new StaticLexemDefenition("day", LexemType.Day),
					new StaticLexemDefenition("game", LexemType.Game),
					new StaticLexemDefenition("player", LexemType.Player),

					new StaticLexemDefenition("from", LexemType.From),
					new StaticLexemDefenition("to", LexemType.To),

					new StaticLexemDefenition("{", LexemType.StartBrace),
					new StaticLexemDefenition("}", LexemType.EndBrace)
				};

				public static DynamicLexemDefenition[] Dynamic = new[]
				{
					new DynamicLexemDefenition(@"[\s\S]", LexemType.Text)
				};
			}

			public class Lexer
			{
				public IEnumerable<Lexem> Lexems { get; private set; }

				string source;
				int offset;

				public void Parse(string src)
				{
					source = src;

					var prepLexems = new List<Lexem>();

					while (InBounds())
					{
						Lexem lexem = ProcessStatic() ?? ProcessDynamic();

						if (lexem != null)
							prepLexems.Add(lexem);
					}

					var lexems = new List<Lexem>();
					Lexem firstTextLexem = null;

					foreach (Lexem lexem in prepLexems) // Fix for text. Idk how to do it properly
					{
						if (lexem.Type == LexemType.Text)
						{
							if (firstTextLexem == null)
								firstTextLexem = lexem;
							else
								firstTextLexem.Value += lexem.Value;
						}
						else
						{
							if (firstTextLexem != null)
							{
								lexems.Add(firstTextLexem);
								firstTextLexem = null;
							}

							lexems.Add(lexem);
						}
					}

					Lexems = lexems;
				}

				Lexem ProcessStatic()
				{
					foreach (var defenition in LexemDefenitions.Static)
					{
						var representation = defenition.Representation;
						var length = representation.Length;

						if (offset + length > source.Length || !source.Substring(offset, length).Equals(representation, StringComparison.CurrentCultureIgnoreCase))
							continue;

						offset += length;

						return new Lexem { Type = defenition.Type, Offset = offset, Value = representation };
					}

					return null;
				}

				Lexem ProcessDynamic()
				{
					foreach (var defenition in LexemDefenitions.Dynamic)
					{
						var match = defenition.Representation.Match(source, offset);

						if (!match.Success)
							continue;

						offset += match.Length;

						return new Lexem { Type = defenition.Type, Offset = offset, Value = match.Value };
					}

					return null;
				}

				bool InBounds()
				{
					return offset < source.Length;
				}
			}

			public class Parser
			{
				List<Lexem> lexems;

				public class Time
				{
					public TimeSpan? From;
					public TimeSpan? To;

					public Time(TimeSpan? from, TimeSpan? to) { From = from; To = to; }
				}

				public class ExecutionAndCount
				{
					public LexemType Execution1 = LexemType.Unknown;
					public LexemType Execution2 = LexemType.Unknown;
					public LexemType Execution3 = LexemType.Unknown;
					public int Count;

					public ExecutionAndCount(LexemType ex1 = LexemType.Unknown, LexemType ex2 = LexemType.Unknown, LexemType ex3 = LexemType.Unknown, int count = 0)
					{
						Execution1 = ex1;
						Execution2 = ex2;
						Execution3 = ex3;
						Count = count;
					}
				}

				public Parser(List<Lexem> lexems)
				{
					this.lexems = lexems;
				}

				/// <summary>Unknown if not command</summary>
				public LexemType ParseCommand()
				{
					Lexem lexem = lexems[0];

					LexemType type = lexem.Type;

					if (type == LexemType.AddCmd || type == LexemType.ListCmd || type == LexemType.RemoveCmd)
						return type;

					return LexemType.Unknown;
				}

				public LexemType ParseRule()
				{
					if (lexems[2].Type == LexemType.On)
					{
						if (lexems[4].Type == LexemType.Enter || lexems[4].Type == LexemType.Exit)
							return lexems[4].Type;
					}

					return LexemType.Unknown;
				}

				/// <summary>Empty if no id</summary>
				public string ParseId()
				{
					Lexem lexem = lexems[1];

					string id = lexem.Value.Trim();

					if (lexem.Type == LexemType.Text)
						return id;

					return string.Empty;
				}

				public ExecutionAndCount ParseExecutionAndCount()
				{
					for (int i = 2; i < lexems.Count; i++)
					{
						if (lexems[i].Type == LexemType.Execute)
						{
							LexemType executionType1 = lexems[i + 2].Type;

							if (executionType1 == LexemType.Always || executionType1 == LexemType.Once)
							{
								int count = ParseCount(i + 2);
								return new ExecutionAndCount(executionType1, LexemType.Unknown, LexemType.Unknown, count);
							}
							else if (executionType1 == LexemType.Per)
							{
								LexemType executionType2 = lexems[i + 4].Type;

								if (executionType2 == LexemType.Day || executionType2 == LexemType.Player)
								{
									int count = ParseCount(i + 4);
									return new ExecutionAndCount(LexemType.Per, executionType2, LexemType.Unknown, count);
								}
								else if (executionType2 == LexemType.Game)
								{
									if (lexems[i + 6].Type == LexemType.Day)
									{
										int count2 = ParseCount(i + 6);
										return new ExecutionAndCount(LexemType.Per, LexemType.Game, LexemType.Day, count2);
									}

									int count = ParseCount(i + 4);
									return new ExecutionAndCount(LexemType.Per, LexemType.Game, LexemType.Unknown, count);
								}
							}
							else
							{
								int count = ParseCount(i);
								return new ExecutionAndCount(LexemType.Unknown, LexemType.Unknown, LexemType.Unknown, count);
							}
						}
						else if (lexems[i].Type == LexemType.StartBrace)
						{
							break;
						}
					}

					return new ExecutionAndCount();
				}

				int ParseCount(int position)
				{
					if (lexems[position + 1].Type == LexemType.Text)
					{
						if (lexems[position + 2].Type == LexemType.Times)
						{
							int number;

							int.TryParse(lexems[position + 1].Value.Trim(), out number);

							return number;
						}
					}

					return -1;
				}

				public List<string> ParseCommands()
				{
					List<string> cmds = new List<string>(1);

					for (int i = 2; i < lexems.Count; i++)
					{
						Lexem lexemStart = lexems[i]; // Start brace

						if (lexemStart.Type == LexemType.StartBrace)
						{
							string cmd = string.Empty;

							for (int ii = i + 1; ii < lexems.Count; ii++)
							{
								Lexem lexemCmd = lexems[ii];

								if (lexemCmd.Type != LexemType.EndBrace)
								{
									cmd += lexemCmd.Value;
								}
								else
								{
									i = ii;
									cmds.Add(cmd.TrimStart().TrimEnd());

									break;
								}
							}
						}
					}

					return cmds;
				}

				public string ParseUserGroup()
				{
					for (int i = 2; i < lexems.Count; i++)
					{
						if (lexems[i].Type == LexemType.Only && lexems[i + 2].Type == LexemType.For)
						{
							if (lexems[i + 3].Type == LexemType.Text)
							{
								return lexems[i + 3].Value.TrimStart().TrimEnd();
							}
						}
						else if (lexems[i].Type == LexemType.StartBrace)
						{
							break;
						}
					}

					return string.Empty;
				}

				public Time ParseTime()
				{
					for (int i = 2; i < lexems.Count; i++)
					{
						if (lexems[i].Type == LexemType.From)
						{
							string from = string.Empty;

							if (lexems[i + 1].Type == LexemType.Text)
							{
								from = lexems[i + 1].Value.Trim();

								if (lexems[i + 2].Type == LexemType.To)
								{
									if (lexems[i + 3].Type == LexemType.Text)
									{
										TimeSpan timeFrom;
										TimeSpan timeTo;

										bool fromSuccess = TimeSpan.TryParse(from, out timeFrom);

										if (!fromSuccess)
											return new Time(null, null);

										bool toSuccess = TimeSpan.TryParse(lexems[i + 3].Value.Trim(), out timeTo);

										if (!toSuccess)
											return new Time(null, null);

										return new Time(timeFrom, timeTo);
									}
								}
							}
						}
					}

					return new Time(null, null);
				}
			}
		}

	}
}
