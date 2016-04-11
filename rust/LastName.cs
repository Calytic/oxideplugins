using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
	[Info("LastName", "deer_SWAG", "0.1.14", ResourceId = 1227)]
	[Description("Stores all usernames")]
	public class LastName : RustPlugin
	{
		const string databaseName = "LastName";

		private class StoredData
		{
			public HashSet<Player> Players = new HashSet<Player>();

			public StoredData() {}

			public void Add(Player player) {Players.Add(player);}
		}

		private class Player
		{
			public ulong userID;
			public HashSet<string> Names = new HashSet<string>();

			public Player() {}
			public Player(ulong userID) {this.userID = userID;}

			public void Add(string name) {Names.Add(name);}
		}

		private StoredData 		  _data;
		private DynamicConfigFile _nameChangeData;
		private StringBuilder	  _stringBuilder;

		protected override void LoadDefaultConfig()
		{
			Config.Clear();

			CheckConfig();

			Puts("Default config was saved and loaded");
		}

		private void OnPluginLoaded()
		{
			CheckConfig();

			_data = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(databaseName);

			if(_data == null)
			{
				PrintError("Unable to load data file");
				ConsoleSystem.Run.Server.Normal("oxide.unload LastName");
			}

			if(IsPluginExists("NameChange"))
				_nameChangeData = Interface.GetMod().DataFileSystem.GetDatafile("NameChange");

			_stringBuilder = new StringBuilder();
		}

		private void OnPlayerConnected(Network.Message packet)
		{
			if((bool)Config["ReplaceWithFirstName"])
				if(_data.Players.Count > 0)
					if(_nameChangeData != null)
					{
						foreach(KeyValuePair<string, object> a in _nameChangeData)
						{
							if(Convert.ToUInt64(a.Key) != packet.connection.userid)
								foreach(Player p in _data.Players)
									if(packet.connection.userid == p.userID)
									{
										packet.connection.username = p.Names.First();
										goto end;
									}
						} end:;
					}
					else
					{
						foreach(Player p in _data.Players)
						{
							if(packet.connection.userid == p.userID)
							{
								packet.connection.username = p.Names.First();
								break;
							}
						}
					}
		}

		private void OnPlayerInit(BasePlayer player)
		{
			if(_data.Players.Count > 0)
			{
				bool found = false;
				bool newName = false;

				foreach(Player p in _data.Players)
				{
					if(p.userID == player.userID)
					{
						found = true;

						foreach(string s in p.Names)
						{
							if(s == player.displayName)
								break;
							else
								newName = true;
						}

						if(newName)
							p.Add(player.displayName);

						break;
					}
				}

				if(!found)
				{
					Player p = new Player(player.userID);
					p.Add(player.displayName);

					_data.Add(p);
				}
			}
			else
			{
				Player p = new Player(player.userID);
				p.Add(player.displayName);

				_data.Add(p);
			}

			SaveData();
		}

		[ChatCommand("lastname")]
		private void cmdChat(BasePlayer player, string command, string[] args)
		{
			if ((int)Config["CommandAuthLevel"] <= player.net.connection.authLevel)
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
			string message = "";
			string name = "";

			message = ((string)Config["Message", "PlayerWasFound"]);

			try
			{
				ulong id = Convert.ToUInt64(args[0]);

				foreach(Player p in _data.Players)
				{
					if(p.userID == id)
					{
						name = p.Names.First();

						foreach(string s in p.Names)
							message += s + ", ";

						break;
					}
				}
			}
			catch {}
			finally
			{
				if(name.Length > 0)
				{
					message = message.Substring(0, message.Length - 2).Replace("%name%", name).Replace("%id%", args[0]);
				}
				else
				{
					Player found = null;

					for(int i = 0; i < args.Length; i++)
						name += args[i] + " ";

					name = name.TrimEnd();

					foreach(Player p in _data.Players)
					{
						foreach(string s in p.Names)
							if(s.Equals(name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = p;
								goto done;
							}
							else if(s.StartsWith(name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = p;
								goto done;
							}
							else if(StringContains(s, name, StringComparison.CurrentCultureIgnoreCase))
							{
								found = p;
								goto done;
							}
					} done:;

					if(found != null)
					{
						foreach(string s in found.Names)
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
			if((int)Config["CommandAuthLevel"] <= player.net.connection.authLevel)
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
			Interface.GetMod().DataFileSystem.WriteObject(databaseName, _data);
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
			return Interface.GetMod().GetLibrary<Oxide.Core.Libraries.Plugins>("Plugins").Exists(name);
		}

		private bool StringContains(string source, string value, StringComparison comparison)
		{
			return source.IndexOf(value, comparison) >= 0;
		}
	}
}