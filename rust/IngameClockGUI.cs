using System;
using System.Collections.Generic;

using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Ingame Clock GUI", "deer_SWAG", "0.0.5", ResourceId = 1245)]
	[Description("Displays ingame and server time")]
	public class IngameClockGUI : RustPlugin
	{
		const string databaseName = "IngameClockGUI";
		const int isClockEnabled = 1 << 0;
		const int isServerTime = 1 << 1;
		const string defaultInfoSize = "0.3";

		class Data
		{
			public HashSet<Player> Players = new HashSet<Player>();

			public Data() {}
		}

		class Player
		{
			public ulong userID;
			public short options;

			public Player() {}
			public Player(ulong id, short o)
			{
				userID = id;
				options = o;
			}
		}

		private class TimedInfo
		{
			public DateTime startTime;
			public DateTime endTime;
			public string text;
			public bool serverTime;
			public string size;

			public TimedInfo(DateTime st, DateTime et, string txt, bool server, string s)
			{
				startTime = st;
				endTime = et;
				text = txt;
				serverTime = server;
				size = s;
			}
		}

		private string clockJson = @"
		[{
			""name"":   ""Clock"",
			""parent"": ""Overlay"",
			""components"":
			[
				{
					""type"":	   ""UnityEngine.UI.Button"",
					""color"":	   ""%background%"",
					""imagetype"": ""Tiled""
				},
				{
					""type"":	   ""RectTransform"",
					""anchormin"": ""%left% %bottom%"",
					""anchormax"": ""%right% %top%""
				}
			]
		},
		{
			""parent"": ""Clock"",
			""components"":
			[
				{
					""type"":	  ""UnityEngine.UI.Text"",
					""text"":	  ""%prefix%%time%%postfix%"",
					""fontSize"": %size%,
					""color"":    ""%color%"",
					""align"":    ""MiddleCenter""
				},
				{
					""type"":	   ""RectTransform"",
					""anchormin"": ""0 0"",
					""anchormax"": ""1 0.9""
				}
			]
		}]";

		private string infoJson = @"
		[{
			""name"":   ""ClockInfo"",
			""parent"": ""Overlay"",
			""components"":
			[
				{
					""type"":	   ""UnityEngine.UI.Button"",
					""color"":	   ""%background%"",
					""imagetype"": ""Tiled""
				},
				{
					""type"":	   ""RectTransform"",
					""anchormin"": ""%info_left% %bottom%"",
					""anchormax"": ""%info_right% %top%""
				}
			]
		},
		{
			""parent"": ""ClockInfo"",
			""components"":
			[
				{
					""type"":	  ""UnityEngine.UI.Text"",
					""text"":	  ""%info%"",
					""fontSize"": %size%,
					""color"":    ""%color%"",
					""align"":    ""MiddleCenter""
				},
				{
					""type"":	   ""RectTransform"",
					""anchormin"": ""0.01 0"",
					""anchormax"": ""0.99 1""
				}
			]
		}]";

		// -------------------- MAIN --------------------

		Data 		data;
		Timer 		updateTimer;
		TOD_Sky 	sky;
		DateTime 	dt;

		bool isLoaded = false,
			 isInit   = false;

		string   time = "";
		DateTime gameTime;
		DateTime serverTime;

		private TimedInfo 		currentTI;
		private List<TimedInfo> tiList;

		protected override void LoadDefaultConfig()
		{
			Config.Clear();

			CheckCreateConfig();

			SaveConfig();
			Puts("Default config was saved and loaded!");
		}

		void OnPluginLoaded()
		{
			isLoaded = true;
			if(isInit) Load();
		}

		void OnServerInitialized()
		{
			isInit = true;
			if(isLoaded) Load();
		}

		void Load()
		{
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>(databaseName);
			tiList = new List<TimedInfo>();
			currentTI = null;
			sky  = TOD_Sky.Instance;

			CheckCreateConfig();

			double left   = (double)Config["Position", "Left"];
			double right  = (double)Config["Position", "Left"] + (double)Config["Size", "Width"];
			double bottom = (double)Config["Position", "Bottom"];
			double top    = (double)Config["Position", "Bottom"] + (double)Config["Size", "Height"];

			clockJson = clockJson.Replace("%background%", (string)Config["BackgroundColor"])
								 .Replace("%color%", (string)Config["TextColor"])
								 .Replace("%size%", Config["FontSize"].ToString())
								 .Replace("%left%", left.ToString())
								 .Replace("%right%", right.ToString())
								 .Replace("%bottom%", bottom.ToString())
								 .Replace("%top%", top.ToString())
								 .Replace("%prefix%", (string)Config["Prefix"])
								 .Replace("%postfix%", (string)Config["Postfix"]);

			// --- for timed notifications

			List<object> ti = (List<object>)Config["TimedInfo"];
			int size = ti.Count;

			for(int i = 0; i < size; i++)
			{
				string infoString = (string)ti[i];

				if(infoString.Length > 0)
					tiList.Add(GetTimedInfo(infoString));
			}

			double info_left = right + 0.002;

			infoJson = infoJson.Replace("%background%", (string)Config["BackgroundColor"])
							   .Replace("%color%", (string)Config["TextColor"])
							   .Replace("%size%", Config["FontSize"].ToString())
							   .Replace("%bottom%", bottom.ToString())
							   .Replace("%top%", top.ToString())
							   .Replace("%info_left%", info_left.ToString())
							   .Replace("%info_right%", defaultInfoSize);
			// ---

			UpdateTime();

			updateTimer = timer.Repeat((int)Config["UpdateTimeInSeconds"], 0, () => UpdateTime());
		}

		void Unload()
		{
			SaveData();
			DestroyGUI();
			DestroyInfo();
		}

		[ChatCommand("clock")]
		void cmdChat(BasePlayer player, string command, string[] args)
		{
			if(args.Length == 1)
			{
				if(args[0] == "server" || args[0] == "s")
				{
					if((bool)Config["PreventChangingTime"])
						PrintToChat(player, (string)Config["Messages", "PreventChangeEnabled"]);
					else
						if(data.Players.Count > 0)
						{
							foreach(Player p in data.Players)
							{
								if(p.userID == player.userID)
								{
									if(GetOption(p.options, isServerTime))
									{
										p.options &= ~isServerTime;
										PrintToChat(player, (string)Config["Messages", "STDisabled"]);
									}
									else
									{
										p.options += isServerTime;
										PrintToChat(player, (string)Config["Messages", "STEnabled"]);
									}

									break;
								}
							}
						}
						else
						{
							data.Players.Add(new Player(player.userID, isClockEnabled | isServerTime));
							PrintToChat(player, (string)Config["Messages", "STEnabled"]);
						}
				}
				else
				{
					PrintToChat(player, (string)Config["Messages", "Help"]);
				}
			}
			else
			{
				bool found = false;

				if(data.Players.Count > 0)
				{
					foreach(Player p in data.Players)
					{
						if(p.userID == player.userID)
						{
							found = true;

							if(GetOption(p.options, isClockEnabled))
							{
								p.options &= ~isClockEnabled;
								DestroyGUI();
								PrintToChat(player, (string)Config["Messages", "Disabled"]);
							}
							else
							{
								p.options += isClockEnabled;
								AddGUI();
								PrintToChat(player, (string)Config["Messages", "Enabled"]);
							}

							break;
						}
						else
						{
							found = false;
						}
					}

					if(!found)
					{
						data.Players.Add(new Player(player.userID, 0));
						DestroyGUI();
						PrintToChat(player, (string)Config["Messages", "Disabled"]);
					}
				}
				else
				{
					data.Players.Add(new Player(player.userID, 0));
					PrintToChat(player, (string)Config["Messages", "Disabled"]);
				}
			}
		}

		void AddGUI()
		{
			if(data.Players.Count > 0)
			{
				int size = BasePlayer.activePlayerList.Count;
				for(int i = 0; i < size; i++)
				{
					BasePlayer bp = BasePlayer.activePlayerList[i];
					bool found = false;

					foreach(Player p in data.Players)
					{
						if(p.userID == bp.userID)
						{
							found = true;

							if(GetOption(p.options, isClockEnabled))
							{
								if(!((bool)Config["PreventChangingTime"]))
									if(GetOption(p.options, isServerTime))
										dt = serverTime;
									else
										dt = gameTime;

								ShowTime();

								CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(bp.net.connection),	null, "AddUI", new Facepunch.ObjectList(clockJson.Replace("%time%", time)));
							}

							break;
						}
						else
						{
							found = false;
						}
					}

					if(!found)
					{
						if(!((bool)Config["PreventChangingTime"]))
							dt = gameTime;
						ShowTime();
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(bp.net.connection),	null, "AddUI", new Facepunch.ObjectList(clockJson.Replace("%time%", time)));
					}
				}
			}
			else
			{
				int size = BasePlayer.activePlayerList.Count;
				for(int i = 0; i < size; i++)
				{
					BasePlayer bp = BasePlayer.activePlayerList[i];
					if(!((bool)Config["PreventChangingTime"]))
						dt = gameTime;
					ShowTime();
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(bp.net.connection),	null, "AddUI", new Facepunch.ObjectList(clockJson.Replace("%time%", time)));
				}
			}
		}

		private void UpdateTime()
		{
			gameTime = sky.Cycle.DateTime;
			serverTime = DateTime.Now;

			if((bool)Config["PreventChangingTime"])
				if((bool)Config["ServerTime"])
					dt = serverTime;
				else
					dt = gameTime;

			DestroyGUI();
			AddGUI();
			UpdateInfo();
		}

		private void DestroyGUI()
		{
			int size = BasePlayer.activePlayerList.Count;
			for(int i = 0; i < size; i++)
			{
				BasePlayer bp = BasePlayer.activePlayerList[i];
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(bp.net.connection), null, "DestroyUI", new Facepunch.ObjectList("Clock"));
			}
		}

		private void ShowTime()
		{
			if((int)Config["TimeFormat"] == 24)
				if((bool)Config["ShowSeconds"])
					time = dt.ToString("HH:mm:ss");
				else
					time = dt.ToString("HH:mm");
			else
				if((bool)Config["ShowSeconds"])
					time = dt.ToString("h:mm:ss tt");
				else
					time = dt.ToString("h:mm tt");
		}

		void ShowInfo(string text, string iSize)
		{
			int size = BasePlayer.activePlayerList.Count;
			for(int i = 0; i < size; i++)
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(BasePlayer.activePlayerList[i].net.connection),	null,  "AddUI", new Facepunch.ObjectList(infoJson.Replace("%info%", text).Replace("%info_right%", iSize)));
		}

		void UpdateInfo()
		{
			if(tiList.Count > 0)
			{
				DateTime g = DateTime.Parse(gameTime.ToString("HH:mm"));
				DateTime s = DateTime.Parse(serverTime.ToString("HH:mm"));

				if(currentTI == null)
				{
					for(int i = 0; i < tiList.Count; i++)
					{
						if(!tiList[i].serverTime)
						{
							if(g.Ticks > tiList[i].startTime.Ticks && g.Ticks < tiList[i].endTime.Ticks)
							{
								currentTI = tiList[i];
								ShowInfo(tiList[i].text, tiList[i].size);
							}
						}
						else
						{
							if(s.Ticks > tiList[i].startTime.Ticks && s.Ticks < tiList[i].endTime.Ticks)
							{
								currentTI = tiList[i];
								ShowInfo(tiList[i].text, tiList[i].size);
							}
						}
					}
				}
				else
				{
					if(!currentTI.serverTime)
					{
						if(g.Ticks > currentTI.endTime.Ticks)
						{
							currentTI = null;
							DestroyInfo();
						}
					}
					else
					{
						if(s.Ticks > currentTI.endTime.Ticks)
						{
							currentTI = null;
							DestroyInfo();
						}
					}
				}
			}
		}

		void DestroyInfo()
		{
			int size = BasePlayer.activePlayerList.Count;
			for(int i = 0; i < size; i++)
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(BasePlayer.activePlayerList[i].net.connection), null, "DestroyUI", new Facepunch.ObjectList("ClockInfo"));
		}

		// -------------------- UTILS --------------------

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject(databaseName, data);
		}

		bool GetOption(int options, int option)
		{
			if((options & option) != 0)
				return true;
			else
				return false;
		}

		void SendHelpText(BasePlayer player)
		{
			PrintToChat(player, (string)Config["Messages", "Help"]);
		}

		private enum TIStates { Init, StartBracket, StartTime, FirstColon, SecondColon, Hyphen, EndTime, AfterBracket, Text, Size, SizeAfterDot };

		private TimedInfo GetTimedInfo(string source)
		{
			source = source.TrimStart();
			source = source.TrimEnd();

			TIStates currentState = TIStates.Init;

			string startTime = "", endTime = "", text = "", nSize = "";
			bool st = false; // Server time

			int size = source.Length;

			for(int i = 0; i < size; i++)
			{				
				switch(currentState)
				{
					case TIStates.Init:
						{
							if (source[i] == '[')
							{
								currentState = TIStates.StartTime;
							}
							else if(source[i] == 's' || source[i] == 'S')
							{
								st = true;
								i++;
								currentState = TIStates.StartTime;
							}
							break;
						}
					case TIStates.StartTime:
						{
							if (Char.IsDigit(source[i]))
							{
								startTime += source[i];
							}
							else if (source[i] == ':')
							{
								startTime += source[i];
								currentState = TIStates.FirstColon;
							}
							break;
						}
					case TIStates.FirstColon:
						{
							if (Char.IsDigit(source[i]))
							{
								startTime += source[i];
							}
							else if(source[i] == '-')
							{
								i--;
								currentState = TIStates.Hyphen;
							}
							break;
						}
					case TIStates.Hyphen:
						{
							if (Char.IsDigit(source[i]))
							{
								i--;
								currentState = TIStates.EndTime;
							}
							break;
						}
					case TIStates.EndTime:
						{
							if (Char.IsDigit(source[i]))
							{
								endTime += source[i];
							}
							else if (source[i] == ':')
							{
								endTime += source[i];
								currentState = TIStates.SecondColon;
							}
							break;
						}
					case TIStates.SecondColon:
						{
							if (Char.IsDigit(source[i]))
								endTime += source[i];
							else if(source[i] == ']')
								currentState = TIStates.AfterBracket;
							else if(source[i] == '-')
								currentState = TIStates.Size;
							break;
						}
					case TIStates.Size:
						{
							if(Char.IsDigit(source[i]))
							{
								nSize += source[i];
							}
							else if(source[i] == '.')
							{
								nSize += '.';
								currentState = TIStates.SizeAfterDot;
							}
							break;
						}
					case TIStates.SizeAfterDot:
						{
							if(Char.IsDigit(source[i]))
								nSize += source[i];
							else if(source[i] == ']')
								currentState = TIStates.AfterBracket;
							break;
						}
					case TIStates.AfterBracket:
						{
							if(source[i] != ' ')
								text += source[i];
							currentState = TIStates.Text;
							break;
						}
					case TIStates.Text:
						{
							text += source[i];
							break;
						}
				}
			}

			if(nSize.Length == 0)
				nSize = defaultInfoSize;

			return new TimedInfo(DateTime.Parse(startTime), DateTime.Parse(endTime), text, st, nSize);
		}

		// -------------------- DEFAULT CONFIG --------------------

		void CheckCreateConfig()
		{
			if(Config["UpdateTimeInSeconds"] == null)
				Config["UpdateTimeInSeconds"] = 2;

			if(Config["ShowSeconds"] == null)
				Config["ShowSeconds"] = false;

			if(Config["BackgroundColor"] == null)
				Config["BackgroundColor"] = "0.1 0.1 0.1 0.3";

			if(Config["TextColor"] == null)
				Config["TextColor"] = "1 1 1 0.3";

			if(Config["FontSize"] == null)
				Config["FontSize"] = 14;

			if(Config["Position", "Left"] == null)
				Config["Position", "Left"] = 0.01;

			if(Config["Position", "Bottom"] == null)
				Config["Position", "Bottom"] = 0.015;

			if(Config["Size", "Width"] == null)
				Config["Size", "Width"] = 0.05;

			if(Config["Size", "Height"] == null)
				Config["Size", "Height"] = 0.03;

			if(Config["ServerTime"] == null)
				Config["ServerTime"] = false;

			if(Config["PreventChangingTime"] == null)
				Config["PreventChangingTime"] = false;

			if(Config["TimeFormat"] == null)
				Config["TimeFormat"] = 24;

			if(Config["Prefix"] == null)
				Config["Prefix"] = "";

			if(Config["Postfix"] == null)
				Config["Postfix"] = "";

			if(Config["TimedInfo"] == null)
				Config["TimedInfo"] = new string[1] {""};

			if(Config["Messages", "Enabled"] == null)
				Config["Messages", "Enabled"] = "You have enabled clock";

			if(Config["Messages", "Disabled"] == null)
				Config["Messages", "Disabled"] = "You have disabled clock";

			if(Config["Messages", "STEnabled"] == null)
				Config["Messages", "STEnabled"] = "Now your clock shows server time";

			if(Config["Messages", "STDisabled"] == null)
				Config["Messages", "STDisabled"] = "Now your clock shows ingame time";

			if(Config["Messages", "Help"] == null)
				Config["Messages", "Help"] = "Clock:\n/clock - toggle clock\n/clock server - toggle server/ingame time";

			if(Config["Messages", "PreventChangeEnabled"] == null)
				Config["Messages", "PreventChangeEnabled"] = "You can't choose between server or ingame time";

			SaveConfig();
		}
	}
}