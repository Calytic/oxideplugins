using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
	[Info("LastName", "deer_SWAG", "0.1.15", ResourceId = 1227)]
	[Description("Stores all usernames")]
	public class LastName : RustPlugin
	{
		const string databaseName = "LastName";

		class StoredData
		{
			public HashSet<Player> Players = new HashSet<Player>();

			public StoredData() { }

			public void Add(Player player) => Players.Add(player);
		}

		class Player
		{
			public ulong userID;
			public HashSet<string> Names = new HashSet<string>();

			public Player() { }
			public Player(ulong userID) { this.userID = userID; }

			public void Add(string name) => Names.Add(name);
		}

		StoredData 		  data;
		DynamicConfigFile nameChangeData;

		protected override void LoadDefaultConfig()
		{
			Config.Clear();

			CheckConfig();

			Puts("Default config was saved and loaded");
		}

		private void OnPluginLoaded()
		{
			CheckConfig();

			data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(databaseName);

			if (data == null)
			{
				RaiseError("Unable to load data file");
				ConsoleSystem.Run.Server.Normal("oxide.unload LastName");
			}

			if (IsPluginExists("NameChange"))
				nameChangeData = Interface.GetMod().DataFileSystem.GetDatafile("NameChange");
		}

		private void OnPlayerConnected(Network.Message packet)
		{
			if ((bool)Config["ReplaceWithFirstName"] && data.Players.Count > 0)
			{
				if (nameChangeData != null)
				{
					foreach (KeyValuePair<string, object> item in nameChangeData)
					{
						if (Convert.ToUInt64(item.Key) != packet.connection.userid)
						{
							foreach (Player dataPlayer in data.Players)
							{
								if (packet.connection.userid == dataPlayer.userID)
								{
									packet.connection.username = dataPlayer.Names.First();
									goto end;
								}
							}
						}
					}
					end:;
				}
				else
				{
					foreach (Player dataPlayer in data.Players)
					{
						if (packet.connection.userid == dataPlayer.userID)
						{
							packet.connection.username = dataPlayer.Names.First();
							break;
						}
					}
				}
			}
		}

		private void OnPlayerInit(BasePlayer player)
		{
			if (data.Players.Count > 0)
			{
				bool found = false;
				bool newName = false;

				foreach (Player dataPlayer in data.Players)
				{
					if (dataPlayer.userID == player.userID)
					{
						found = true;

						foreach (string name in dataPlayer.Names)
						{
							if (name == player.displayName)
								break;
							else
								newName = true;
						}

						if (newName)
							dataPlayer.Add(player.displayName);

						break;
					}
				}

				if (!found)
				{
					Player p = new Player(player.userID);
					p.Add(player.displayName);

					data.Add(p);
				}
			}
			else
			{
				Player p = new Player(player.userID);
				p.Add(player.displayName);

				data.Add(p);
			}

			SaveData();
		}

		[ChatCommand("lastname")]
		private void cmdChat(BasePlayer player, string command, string[] args)
		{
			if (player.net.connection.authLevel >= (int)Config["CommandAuthLevel"])
				if (args.Length > 0)
					PrintToChat(player, GetNames(args));
				else
					PrintToChat(player, (string)Config["Message", "WrongQuery"]);
			else
				PrintToChat(player, (string)Config["Message", "NoAccess"]);
		}

		[ConsoleCommand("player.lastname")]
		private void cmdConsole(ConsoleSystem.Arg arg)
		{
			if (arg.HasArgs())
				Puts(GetNames(arg.Args));
			else
				Puts((string)Config["Message", "WrongQuery"]);
		}

		private string GetNames(string[] args)
		{
			string message = (string)Config["Message", "PlayerWasFound"];
			string name = string.Empty;

			try
			{
				ulong id = Convert.ToUInt64(args[0]);

				foreach (Player dataPlayer in data.Players)
				{
					if (dataPlayer.userID == id)
					{
						name = dataPlayer.Names.First();

						foreach (string n in dataPlayer.Names)
							message += n + ", ";

						break;
					}
				}
			}
			catch { }
			finally
			{
				if (name.Length > 0)
				{
					message = message.Substring(0, message.Length - 2).Replace("%name%", name).Replace("%id%", args[0]);
				}
				else
				{
					Player found = null;

					for (int i = 0; i < args.Length; i++)
						name += args[i] + " ";

					name = name.TrimEnd();

					foreach (Player dataPlayer in data.Players)
					{
						foreach (string s in dataPlayer.Names)
						{
							if (s.Equals(name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = dataPlayer;
								goto end;
							}
							else if (s.StartsWith(name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = dataPlayer;
								goto end;
							}
							else if (StringContains(s, name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = dataPlayer;
								goto end;
							}
						}
					} end:;

					if (found != null)
					{
						foreach (string s in found.Names)
							message += s + ", ";

						message = message.Substring(0, message.Length - 2).Replace("%name%", name).Replace("%id%", found.userID.ToString());
					}
					else
					{
						message = (string)Config["Message", "NoPlayerFound"];
					}
				}
			}

			return message;
		}

		void SendHelpText(BasePlayer player)
		{
			if (player.net.connection.authLevel >= (int)Config["CommandAuthLevel"])
				PrintToChat(player, (string)Config["Message", "WrongQuery"]);
		}

		private void CheckConfig()
		{
			ConfigItem("ReplaceWithFirstName", false);
			ConfigItem("CommandAuthLevel", 0);
			ConfigItem("Message", "NoAccess", "You are don't have access for this command");
			ConfigItem("Message", "WrongQuery", "/lastname <name/steamID>");
			ConfigItem("Message", "NoPlayerFound", "No players found with that name/steamID");
			ConfigItem("Message", "PlayerWasFound", "%name%(%id%) was also known as: ");

			SaveConfig();
		}

		private void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(databaseName, data);
		}

		// ----------------------------- UTILS -----------------------------
		// -----------------------------------------------------------------

		private void ConfigItem(string name, object defaultValue)
		{
			Config[name] = Config[name] ?? defaultValue;
		}

		private void ConfigItem(string name1, string name2, object defaultValue)
		{
			Config[name1, name2] = Config[name1, name2] ?? defaultValue;
		}

		private bool IsPluginExists(string name)
		{
			return Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins").Exists(name);
		}

		private bool StringContains(string source, string value, StringComparison comparison)
		{
			return source.IndexOf(value, comparison) >= 0;
		}
	}
}