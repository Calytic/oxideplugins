using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using ServerInfo;
using ServerInfo.Extensions;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("ServerInfo", "baton", "0.4.0", ResourceId = 1317)]
	[Description("UI customizable server info with multiple tabs.")]
	public sealed class ServerInfo : RustPlugin
	{
		private static Settings _settings;
		private static readonly Dictionary<ulong, PlayerInfoState> PlayerActiveTabs = new Dictionary<ulong, PlayerInfoState>();
		private static readonly Permission Permission = Interface.GetMod().GetLibrary<Permission>();

		protected override void LoadDefaultConfig()
		{
			Config.Set("settings", Settings.CreateDefault());
		}

		private void OnServerInitialized()
		{
			LoadConfig();
			var configFileName = Manager.ConfigPath + "/server_info_text.json";

			_settings = null;
			try
			{
				var settingsDict = Config.Get("settings") as Dictionary<string, object>;
				_settings = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(settingsDict));
			}
			catch (Exception)
			{
				Puts("ServerInfo: Failed to load config");
				return;
			}

			_settings = _settings ?? Settings.CreateDefault();

			if (!_settings.UpgradedConfig && Config.Exists(configFileName))
			{
				try
				{
					Puts("ServerInfo: Upgrading settings from server_info_text.json");
					_settings = Config.ReadObject<Settings>(configFileName);
					_settings.UpgradedConfig = true;
					Puts("ServerInfo: Successfully upgraded config");
				}
				catch (Exception)
				{
					Puts("ServerInfo: Failed to upgrade config. Manual editing is required.");
					Puts("ServerInfo: Copy your settings by parts to new config in ServerInfo.json");
				}
			}

			foreach (var player in BasePlayer.activePlayerList)
				AddHelpButton(player);

			Config.Set("settings", _settings);
			SaveConfig();
		}

		void Unload()
		{
			foreach (var playerActiveTab in PlayerActiveTabs)
			{
				var player = BasePlayer.activePlayerList.FirstOrDefault(f => f.userID == playerActiveTab.Key);
				if (player == null)
					continue;

				CuiHelper.DestroyUi(player, playerActiveTab.Value.MainPanelName);
				CuiHelper.DestroyUi(player, playerActiveTab.Value.ChatHelpButtonName);
			}

			PlayerActiveTabs.Clear();
		}

		[ConsoleCommand("changetab")]
		private void ChangeTab(ConsoleSystem.Arg arg)
		{
			if (arg.connection == null || arg.connection.player == null || !arg.HasArgs(4) || _settings == null)
				return;

			var player = arg.connection.player as BasePlayer;
			if (player == null)
				return;

			if (!PlayerActiveTabs.ContainsKey(player.userID))
				return;

			var previousTabIndex = PlayerActiveTabs[player.userID].ActiveTabIndex;
			var tabToChangeTo = arg.GetInt(0, 65535);

			if (previousTabIndex == tabToChangeTo)
				return;

			var tabToSelectIndex = arg.GetInt(0);
			var activeButtonName = arg.GetString(1);
			var tabToSelectButtonName = arg.GetString(2);
			var mainPanelName = arg.GetString(3);

			CuiHelper.DestroyUi(player, PlayerActiveTabs[player.userID].ActiveTabContentPanelName);
			CuiHelper.DestroyUi(player, activeButtonName);
			CuiHelper.DestroyUi(player, tabToSelectButtonName);

			var allowedTabs = _settings.Tabs
				.Where((tab, tabIndex) => string.IsNullOrEmpty(tab.OxideGroup) ||
					tab.OxideGroup.Split(',')
						.Any(group => Permission.UserHasGroup(player.userID.ToString(CultureInfo.InvariantCulture), group)))
				.ToList();
			var tabToSelect = allowedTabs[tabToSelectIndex];
			PlayerActiveTabs[player.userID].ActiveTabIndex = tabToSelectIndex;
			PlayerActiveTabs[player.userID].PageIndex = 0;

			var container = new CuiElementContainer();
			var tabContentPanelName = CreateTabContent(tabToSelect, container, mainPanelName);
			var newActiveButtonName = AddActiveButton(tabToSelectIndex, tabToSelect, container, mainPanelName);
			AddNonActiveButton(previousTabIndex, container, _settings.Tabs[previousTabIndex], mainPanelName, newActiveButtonName);

			PlayerActiveTabs[player.userID].ActiveTabContentPanelName = tabContentPanelName;

			SendUI(player, container);
		}

		[ConsoleCommand("changepage")]
		private void ChangePage(ConsoleSystem.Arg arg)
		{
			if (arg.connection == null || arg.connection.player == null || !arg.HasArgs(2) || _settings == null)
				return;

			var player = arg.connection.player as BasePlayer;
			if (player == null)
				return;

			if (!PlayerActiveTabs.ContainsKey(player.userID))
				return;

			var playerInfoState = PlayerActiveTabs[player.userID];
			var currentTab = _settings.Tabs[playerInfoState.ActiveTabIndex];
			var currentPageIndex = playerInfoState.PageIndex;

			var pageToChangeTo = arg.GetInt(0, 65535);
			var currentTabContentPanelName = playerInfoState.ActiveTabContentPanelName;
			var mainPanelName = arg.GetString(1);

			if (pageToChangeTo == currentPageIndex)
				return;

			CuiHelper.DestroyUi(player, currentTabContentPanelName);

			playerInfoState.PageIndex = pageToChangeTo;

			var container = new CuiElementContainer();
			var tabContentPanelName = CreateTabContent(currentTab, container, mainPanelName, pageToChangeTo);
			PlayerActiveTabs[player.userID].ActiveTabContentPanelName = tabContentPanelName;

			SendUI(player, container);
		}

		[ConsoleCommand("infoclose")]
		private void CloseInfo(ConsoleSystem.Arg arg)
		{
			if (arg.connection == null || arg.connection.player == null || !arg.HasArgs() || _settings == null)
				return;

			var player = arg.connection.player as BasePlayer;
			if (player == null)
				return;

			if (!PlayerActiveTabs.ContainsKey(player.userID))
				return;

			const string defaultName = "defaultString";
			var mainPanelName = arg.GetString(0, defaultName);

			if (mainPanelName.Equals(defaultName, StringComparison.OrdinalIgnoreCase))
				return;

			PlayerInfoState state;
			PlayerActiveTabs.TryGetValue(player.userID, out state);
			if (state == null)
				return;

			CuiHelper.DestroyUi(player, mainPanelName);

			state.ActiveTabIndex = _settings.TabToOpenByDefault;
			state.MainPanelName = string.Empty;
			state.PageIndex = 0;
		}

		private void OnPlayerInit(BasePlayer player)
		{
			if (player == null || _settings == null)
				return;

			if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
			{
				timer.Once(2, () => OnPlayerInit(player));
				return;
			}

			PlayerInfoState state;
			PlayerActiveTabs.TryGetValue(player.userID, out state);

			if (state == null)
			{
				state = new PlayerInfoState(_settings);
				PlayerActiveTabs.Add(player.userID, state);
			}

			AddHelpButton(player);

			if (!state.InfoShownOnLogin)
				return;

			ShowInfo(player, string.Empty, new string[0]);
		}

		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (player == null || _settings == null)
				return;

			PlayerInfoState state;
			PlayerActiveTabs.TryGetValue(player.userID, out state);

			if (state == null)
				return;

			CuiHelper.DestroyUi(player, state.MainPanelName);

			state.ActiveTabIndex = _settings.TabToOpenByDefault;
			state.MainPanelName = string.Empty;
			state.PageIndex = 0;
		}

		private static void AddHelpButton(BasePlayer player)
		{
			if (!_settings.HelpButton.IsEnabled || _settings == null)
				return;

			var container = new CuiElementContainer();
			var helpChatButton = CreateHelpButton();
			var helpButtonName = container.Add(helpChatButton);
			if (!PlayerActiveTabs.ContainsKey(player.userID))
				PlayerActiveTabs[player.userID] = new PlayerInfoState(_settings);

			PlayerActiveTabs[player.userID].ChatHelpButtonName = helpButtonName;
			CuiHelper.AddUi(player, container);
		}

		[ConsoleCommand("info")]
		private void ShowConsoleInfo(ConsoleSystem.Arg arg)
		{
			if (arg == null || arg.connection == null || arg.connection.player == null || _settings == null)
				return;

			var player = arg.connection.player as BasePlayer;
			if (player == null)
				return;
			if (string.IsNullOrEmpty(PlayerActiveTabs[player.userID].MainPanelName))
				ShowInfo(player, string.Empty, null);
		}

		[ChatCommand("info")]
		private void ShowInfo(BasePlayer player, string command, string[] args)
		{
			if (player == null || _settings == null)
				return;

			if (!PlayerActiveTabs.ContainsKey(player.userID))
				PlayerActiveTabs.Add(player.userID, new PlayerInfoState(_settings));

			var container = new CuiElementContainer();
			var mainPanelName = AddMainPanel(container);
			PlayerActiveTabs[player.userID].MainPanelName = mainPanelName;

			var tabToSelectIndex = _settings.TabToOpenByDefault;
			var allowedTabs = _settings.Tabs
				.Where((tab, tabIndex) => string.IsNullOrEmpty(tab.OxideGroup) ||
					tab.OxideGroup.Split(',')
						.Any(group => Permission.UserHasGroup(player.userID.ToString(CultureInfo.InvariantCulture), group)))
				.ToList();
			if (allowedTabs.Count <= 0)
			{
				SendReply(player, "[GUI Help] You don't have permissions to see info.");
				return;
			}

			var activeAllowedTab = allowedTabs[tabToSelectIndex];
			var tabContentPanelName = CreateTabContent(activeAllowedTab, container, mainPanelName);
			var activeTabButtonName = AddActiveButton(tabToSelectIndex, activeAllowedTab, container, mainPanelName);

			for (int tabIndex = 0; tabIndex < allowedTabs.Count; tabIndex++)
			{
				if (tabIndex == tabToSelectIndex)
					continue;

				AddNonActiveButton(tabIndex, container, allowedTabs[tabIndex], mainPanelName, activeTabButtonName);
			}
			PlayerActiveTabs[player.userID].ActiveTabContentPanelName = tabContentPanelName;
			SendUI(player, container);
		}

		private static void SendUI(BasePlayer player, CuiElementContainer container)
		{
			var json = JsonConvert.SerializeObject(container, Formatting.None, new JsonSerializerSettings
			{
				StringEscapeHandling = StringEscapeHandling.Default,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				Formatting = Formatting.Indented
			});
			json = json.Replace(@"\t", "\t");
			json = json.Replace(@"\n", "\n");

			//CuiHelper.AddUi(player, container);
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json, null, null, null, null));
		}

		private static string AddMainPanel(CuiElementContainer container)
		{
			Color backgroundColor;
			ColorExtensions.TryParseHexString(_settings.BackgroundColor, out backgroundColor);
			var mainPanel = new CuiPanel
			{
				Image =
				{
					Color = backgroundColor.ToRustFormatString()
				},
				CursorEnabled = true,
				RectTransform =
				{
					AnchorMin = _settings.Position.GetRectTransformAnchorMin(),
					AnchorMax = _settings.Position.GetRectTransformAnchorMax()
				}
			};

			var tabContentPanelName = container.Add(mainPanel);
			if (!_settings.BackgroundImage.Enabled)
				return tabContentPanelName;

			var backgroundImage = CreateImage(tabContentPanelName, _settings.BackgroundImage);

			container.Add(backgroundImage);

			return tabContentPanelName;
		}

		private static CuiElement CreateImage(string panelName, ImageSettings settings)
		{
			var element = new CuiElement();
			var image = new CuiRawImageComponent
			{
				Url = settings.Url,
				Color = string.Format("1 1 1 {0:F1}", (settings.TransparencyInPercent / 100.0f))
			};

			var position = settings.Position;
			var rectTransform = new CuiRectTransformComponent
			{
				AnchorMin = position.GetRectTransformAnchorMin(),
				AnchorMax = position.GetRectTransformAnchorMax()
			};
			element.Components.Add(image);
			element.Components.Add(rectTransform);
			element.Name = CuiHelper.GetGuid();
			element.Parent = panelName;

			return element;
		}


		private string CreateTabContent(HelpTab helpTab, CuiElementContainer container, string mainPanelName, int pageIndex = 0)
		{
			var tabPanelName = CreateTab(helpTab, container, mainPanelName, pageIndex);
			var closeButton = CreateCloseButton(mainPanelName, _settings.CloseButtonColor);
			container.Add(closeButton, tabPanelName);
			return tabPanelName;
		}

		private static string CreateTab(HelpTab helpTab, CuiElementContainer container, string mainPanelName, int pageIndex)
		{
			var tabPanelName = CreateTabPanel(container, mainPanelName, "#00000000");

			var currentPage = helpTab.Pages.ElementAtOrDefault(pageIndex);
			if (currentPage == null)
				return tabPanelName;

			foreach (var imageSettings in currentPage.ImageSettings)
			{
				var imageObject = CreateImage(tabPanelName, imageSettings);
				container.Add(imageObject);
			}

			var cuiLabel = CreateHeaderLabel(helpTab);
			container.Add(cuiLabel, tabPanelName);

			const float firstLineMargin = 0.91f;
			const float textLineHeight = 0.04f;

			for (var textRow = 0; textRow < currentPage.TextLines.Count; textRow++)
			{
				var textLine = currentPage.TextLines[textRow];
				var textLineLabel = CreateTextLineLabel(helpTab, firstLineMargin, textLineHeight, textRow, textLine);
				container.Add(textLineLabel, tabPanelName);
			}

			if (pageIndex > 0)
			{
				var prevPageButton = CreatePrevPageButton(mainPanelName, pageIndex, _settings.PrevPageButtonColor);
				container.Add(prevPageButton, tabPanelName);
			}

			if (helpTab.Pages.Count - 1 == pageIndex)
				return tabPanelName;

			var nextPageButton = CreateNextPageButton(mainPanelName, pageIndex, _settings.NextPageButtonColor);
			container.Add(nextPageButton, tabPanelName);

			return tabPanelName;
		}

		private static string CreateTabPanel(CuiElementContainer container, string mainPanelName, string hexColor)
		{
			Color backgroundColor;
			ColorExtensions.TryParseHexString(hexColor, out backgroundColor);

			return container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image =
				{
					Color = backgroundColor.ToRustFormatString(),
				},
				RectTransform =
				{
					AnchorMin = "0.22 0.01",
					AnchorMax = "0.99 0.98"
				}
			}, mainPanelName);
		}

		private static string CreateTabContentPanel(CuiElementContainer container, string mainPanelName, string hexColor)
		{
			Color backgroundColor;
			ColorExtensions.TryParseHexString(hexColor, out backgroundColor);

			return container.Add(new CuiPanel
			{
				CursorEnabled = false,
				Image =
				{
					Color = backgroundColor.ToRustFormatString(),
				},
				RectTransform =
				{
					AnchorMin = "0 0",
					AnchorMax = "0.86 1"
				}
			}, mainPanelName);
		}

		private static CuiLabel CreateHeaderLabel(HelpTab helpTab)
		{
			return new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0.01 0.85",
					AnchorMax = "1.0 0.98"
				},
				Text =
				{
					Align = helpTab.HeaderAnchor,
					FontSize = helpTab.HeaderFontSize,
					Text = helpTab.HeaderText
				}
			};
		}

		private static CuiButton CreateCloseButton(string mainPanelName, string hexColor)
		{
			Color color;
			ColorExtensions.TryParseHexString(hexColor, out color);
			return new CuiButton
			{
				Button =
				{
					Command = string.Format("infoclose {0}", mainPanelName),
					Close = mainPanelName,
					Color = color.ToRustFormatString()
				},
				RectTransform =
				{
					AnchorMin = "0.86 0.93",
					AnchorMax = "0.97 0.99"
				},
				Text =
				{
					Text = "Close",
					FontSize = 18,
					Align = TextAnchor.MiddleCenter
				}
			};
		}

		private static CuiButton CreateHelpButton()
		{
			Color color;
			ColorExtensions.TryParseHexString(_settings.HelpButton.Color, out color);
			return new CuiButton
			{
				Button =
				{
					Command = "info",
					Color = color.ToRustFormatString()
				},
				RectTransform =
				{
					AnchorMin = _settings.HelpButton.Position.GetRectTransformAnchorMin(),
					AnchorMax = _settings.HelpButton.Position.GetRectTransformAnchorMax()
				},
				Text =
				{
					Text = _settings.HelpButton.Text,
					FontSize = _settings.HelpButton.FontSize,
					Align = TextAnchor.MiddleCenter
				}
			};
		}

		private static CuiLabel CreateTextLineLabel(HelpTab helpTab, float firstLineMargin, float textLineHeight, int textRow,
			string textLine)
		{
			var textLineLabel = new CuiLabel
			{
				RectTransform =
				{
					AnchorMin = "0.01 " + (firstLineMargin - textLineHeight * (textRow + 1)),
					AnchorMax = "0.85 " + (firstLineMargin - textLineHeight * textRow)
				},
				Text =
				{
					Align = helpTab.TextAnchor,
					FontSize = helpTab.TextFontSize,
					Text = textLine
				}
			};
			return textLineLabel;
		}

		private static CuiButton CreatePrevPageButton(string mainPanelName, int pageIndex, string hexColor)
		{
			Color color;
			ColorExtensions.TryParseHexString(hexColor, out color);
			return new CuiButton
			{
				Button =
				{
					Command = string.Format("changepage {0} {1}", pageIndex - 1, mainPanelName),
					Color = color.ToRustFormatString()
				},
				RectTransform =
				{
					AnchorMin = "0.86 0.01",
					AnchorMax = "0.97 0.07"
				},
				Text =
				{
					Text = "Prev Page",
					FontSize = 18,
					Align = TextAnchor.MiddleCenter
				}
			};
		}

		private static CuiButton CreateNextPageButton(string mainPanelName, int pageIndex, string hexColor)
		{
			Color color;
			ColorExtensions.TryParseHexString(hexColor, out color);
			return new CuiButton
			{
				Button =
				{
					Command = string.Format("changepage {0} {1}", pageIndex + 1, mainPanelName),
					Color = color.ToRustFormatString()
				},
				RectTransform =
				{
					AnchorMin = "0.86 0.08",
					AnchorMax = "0.97 0.15"
				},
				Text =
				{
					Text = "Next Page",
					FontSize = 18,
					Align = TextAnchor.MiddleCenter
				}
			};
		}

		private static void AddNonActiveButton(
			int tabIndex,
			CuiElementContainer container,
			HelpTab helpTab,
			string mainPanelName,
			string activeTabButtonName)
		{
			Color nonActiveButtonColor;
			ColorExtensions.TryParseHexString(_settings.InactiveButtonColor, out nonActiveButtonColor);

			CuiButton helpTabButton = CreateTabButton(tabIndex, helpTab, nonActiveButtonColor);
			string helpTabButtonName = container.Add(helpTabButton, mainPanelName);

			CuiElement helpTabButtonCuiElement =
				container.First(i => i.Name.Equals(helpTabButtonName, StringComparison.OrdinalIgnoreCase));
			CuiButtonComponent generatedHelpTabButton = helpTabButtonCuiElement.Components.OfType<CuiButtonComponent>().First();

			string command = string.Format("changeTab {0} {1} {2} {3}", tabIndex, activeTabButtonName, helpTabButtonName, mainPanelName);
			generatedHelpTabButton.Command = command;
		}

		private static string AddActiveButton(
			int activeTabIndex,
			HelpTab activeTab,
			CuiElementContainer container,
			string mainPanelName)
		{
			Color activeButtonColor;
			ColorExtensions.TryParseHexString(_settings.ActiveButtonColor, out activeButtonColor);

			var activeHelpTabButton = CreateTabButton(activeTabIndex, activeTab, activeButtonColor);
			var activeTabButtonName = container.Add(activeHelpTabButton, mainPanelName);

			var activeTabButtonCuiElement =
				container.First(i => i.Name.Equals(activeTabButtonName, StringComparison.OrdinalIgnoreCase));
			var activeTabButton = activeTabButtonCuiElement.Components.OfType<CuiButtonComponent>().First();

			var command = string.Format("changeTab {0}", activeTabIndex);
			activeTabButton.Command = command;

			return activeTabButtonName;
		}

		private static CuiButton CreateTabButton(int tabIndex, HelpTab helpTab, Color color)
		{
			const float verticalMargin = 0.03f;
			const float buttonHeight = 0.06f;

			return new CuiButton
			{
				Button =
				{
					Color = color.ToRustFormatString()
				},
				RectTransform =
				{
					AnchorMin = string.Format("0.01 {0}", 1 - ((verticalMargin + buttonHeight) * (tabIndex + 1))),
					AnchorMax = string.Format("0.20 {0}", 1 - ((verticalMargin * (tabIndex + 1)) + (tabIndex * buttonHeight)))
				},
				Text =
				{
					Text = helpTab.ButtonText,
					FontSize = helpTab.TabButtonFontSize,
					Align = helpTab.TabButtonAnchor
				}
			};
		}
	}
}

namespace ServerInfo
{
	[JsonObject]
	public sealed class Settings
	{
		public Settings()
		{
			Tabs = new List<HelpTab>();
			ShowInfoOnPlayerInit = true;
			TabToOpenByDefault = 0;
			Position = new Position();

			ActiveButtonColor = "#" + Color.cyan.ToHexStringRGBA();
			InactiveButtonColor = "#" + Color.gray.ToHexStringRGBA();
			CloseButtonColor = "#" + Color.gray.ToHexStringRGBA();
			PrevPageButtonColor = "#" + Color.gray.ToHexStringRGBA();
			NextPageButtonColor = "#" + Color.gray.ToHexStringRGBA();
			BackgroundColor = "#" + new Color(0f, 0f, 0f, 1.0f).ToHexStringRGBA();
			HelpButton = new HelpButtonSettings();

			BackgroundImage = new BackgroundImageSettings();
		}

		public List<HelpTab> Tabs { get; set; }
		public bool ShowInfoOnPlayerInit { get; set; }
		public int TabToOpenByDefault { get; set; }

		public Position Position { get; set; }
		public BackgroundImageSettings BackgroundImage { get; set; }

		public string ActiveButtonColor { get; set; }
		public string InactiveButtonColor { get; set; }
		public string CloseButtonColor { get; set; }
		public string NextPageButtonColor { get; set; }
		public string PrevPageButtonColor { get; set; }
		public string BackgroundColor { get; set; }

		public HelpButtonSettings HelpButton { get; set; }

		public bool UpgradedConfig { get; set; }

		public static Settings CreateDefault()
		{
			var settings = new Settings();
			var firstTab = new HelpTab
			{
				ButtonText = "First Tab",
				HeaderText = "First Tab",
				Pages =
                {
                    new HelpTabPage
                    {
                        TextLines =
                        {
                            "This is first tab, first page.",
                            "Add some text here by adding more lines.",
                            "You should replace all default text lines with whatever you feel up to",
                            "type <color=red> /info </color> to open this window",
                            "Press next page to check second page.",
                            "You may add more pages in config file."
                        },
                        ImageSettings =
                        {
                            new ImageSettings
                            {
                                Position = new Position
                                {
                                    MinX = 0,
                                    MaxX = 0.5f,
                                    MinY = 0,
                                    MaxY = 0.5f
                                },
                                Url = "http://th04.deviantart.net/fs70/PRE/f/2012/223/4/4/rust_logo_by_furrypigdog-d5aqi3r.png"
                            },
                            new ImageSettings
                            {
                                Position = new Position
                                {
                                    MinX = 0.5f,
                                    MaxX = 1f,
                                    MinY = 0,
                                    MaxY = 0.5f
                                },
                                Url = "http://files.enjin.com/176331/IMGS/LOGO_RUST1.fw.png"
                            },
                            new ImageSettings
                            {
                                Position = new Position
                                {
                                    MinX = 0,
                                    MaxX = 0.5f,
                                    MinY = 0.5f,
                                    MaxY = 1f
                                },
                                Url = "http://files.enjin.com/176331/IMGS/LOGO_RUST1.fw.png"
                            },
                            new ImageSettings
                            {
                                Position = new Position
                                {
                                    MinX = 0.5f,
                                    MaxX = 1f,
                                    MinY = 0.5f,
                                    MaxY = 1f
                                },
                                Url = "http://th04.deviantart.net/fs70/PRE/f/2012/223/4/4/rust_logo_by_furrypigdog-d5aqi3r.png"
                            },
                        }
                    },
                    new HelpTabPage
                    {
                        TextLines =
                        {
                            "This is first tab, second page",
                            "Add some text here by adding more lines.",
                            "You should replace all default text lines with whatever you feel up to",
                            "type <color=red> /info </color> to open this window",
                            "Press next page to check third page.",
                            "Press prev page to go back to first page.",
                            "You may add more pages in config file."
                        }
                    }
                    ,
                    new HelpTabPage
                    {
                        TextLines =
                        {
                            "This is first tab, third page",
                            "Add some text here by adding more lines.",
                            "You should replace all default text lines with whatever you feel up to",
                            "type <color=red> /info </color> to open this window",
                            "Press prev page to go back to second page.",
                        }
                    }
                }
			};
			var secondTab = new HelpTab
			{
				ButtonText = "Second Tab",
				HeaderText = "Second Tab",
				Pages =
                {
                    new HelpTabPage
                    {
                        TextLines =
                        {
                            "This is second tab, first page.",
                            "Add some text here by adding more lines.",
                            "You should replace all default text lines with whatever you feel up to",
                            "type <color=red> /info </color> to open this window",
                            "You may add more pages in config file."
                        }
                    }
                }
			};
			var thirdTab = new HelpTab
			{
				ButtonText = "Third Tab",
				HeaderText = "Third Tab",
				Pages =
                {
                    new HelpTabPage
                    {
                        TextLines =
                        {
                            "This is third tab, first page.",
                            "Add some text here by adding more lines.",
                            "You should replace all default text lines with whatever you feel up to",
                            "type <color=red> /info </color> to open this window",
                            "You may add more pages in config file."
                        }
                    }
                }
			};

			settings.Tabs.Add(firstTab);
			settings.Tabs.Add(secondTab);
			settings.Tabs.Add(thirdTab);

			return settings;
		}
	}

	public sealed class Position
	{
		public Position()
		{
			MinX = 0.15f;
			MaxX = 0.9f;
			MinY = 0.2f;
			MaxY = 0.9f;
		}

		public float MinX { get; set; }
		public float MaxX { get; set; }
		public float MinY { get; set; }
		public float MaxY { get; set; }

		public string GetRectTransformAnchorMin()
		{
			return string.Format("{0} {1}", MinX, MinY);
		}

		public string GetRectTransformAnchorMax()
		{
			return string.Format("{0} {1}", MaxX, MaxY);
		}
	}

	public sealed class HelpTab
	{
		private string _headerText;
		private string _buttonText;

		public HelpTab()
		{
			ButtonText = "Default ServerInfo Help Tab";
			HeaderText = "Default ServerInfo Help";
			Pages = new List<HelpTabPage>();
			TextFontSize = 16;
			HeaderFontSize = 32;
			TabButtonFontSize = 16;
			TextAnchor = TextAnchor.MiddleLeft;
			HeaderAnchor = TextAnchor.UpperLeft;
			TabButtonAnchor = TextAnchor.MiddleCenter;
			OxideGroup = string.Empty;
		}

		public string ButtonText
		{
			get { return string.IsNullOrEmpty(_buttonText) ? _headerText : _buttonText; }
			set { _buttonText = value; }
		}

		public string HeaderText
		{
			get
			{
				return string.IsNullOrEmpty(_headerText) ? _buttonText : _headerText;
			}
			set { _headerText = value; }
		}

		public List<HelpTabPage> Pages { get; set; }

		public TextAnchor TabButtonAnchor { get; set; }
		public int TabButtonFontSize { get; set; }

		public TextAnchor HeaderAnchor { get; set; }
		public int HeaderFontSize { get; set; }

		public int TextFontSize { get; set; }
		public TextAnchor TextAnchor { get; set; }

		public string OxideGroup { get; set; }
	}

	public sealed class HelpTabPage
	{
		public List<string> TextLines { get; set; }
		public List<ImageSettings> ImageSettings { get; set; }

		public HelpTabPage()
		{
			TextLines = new List<string>();
			ImageSettings = new List<ImageSettings>();
		}
	}

	public class ImageSettings
	{
		public Position Position { get; set; }
		public string Url { get; set; }
		public int TransparencyInPercent { get; set; }

		public ImageSettings()
		{
			Position = new Position
			{
				MaxX = 1.0f,
				MaxY = 1.0f,
				MinY = 0.0f,
				MinX = 0.0f
			};
			Url = "http://7-themes.com/data_images/out/35/6889756-black-backgrounds.jpg";
			TransparencyInPercent = 100;
		}
	}

	public sealed class BackgroundImageSettings : ImageSettings
	{
		public bool Enabled { get; set; }

		public BackgroundImageSettings()
		{
			Enabled = false;
		}
	}

	public sealed class HelpButtonSettings
	{
		public bool IsEnabled { get; set; }
		public string Text { get; set; }
		public Position Position { get; set; }
		public string Color { get; set; }
		public int FontSize { get; set; }

		public HelpButtonSettings()
		{
			IsEnabled = false;
			Text = "Help";
			Color = "#" + UnityEngine.Color.gray.ToHexStringRGBA();
			FontSize = 18;

			Position = new Position { MinX = 0.00f, MaxX = 0.05f, MinY = 0.10f, MaxY = 0.14f };
		}
	}

	public sealed class PlayerInfoState
	{
		public PlayerInfoState(Settings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");

			ActiveTabIndex = settings.TabToOpenByDefault;
			PageIndex = 0;
			InfoShownOnLogin = settings.ShowInfoOnPlayerInit;
			ActiveTabContentPanelName = string.Empty;
			ChatHelpButtonName = string.Empty;
			MainPanelName = string.Empty;
		}

		public int ActiveTabIndex { get; set; }
		public int PageIndex { get; set; }
		public bool InfoShownOnLogin { get; set; }
		public string ActiveTabContentPanelName { get; set; }
		public string ChatHelpButtonName { get; set; }
		public string MainPanelName { get; set; }
	}
}

namespace ServerInfo.Extensions
{
	public static class ColorExtensions
	{
		public static string ToRustFormatString(this Color color)
		{
			return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
		}

		//
		// UnityEngine 5.1 Color extensions which were removed in 5.2
		//

		public static string ToHexStringRGB(this Color col)
		{
			Color32 color = col;
			return string.Format("{0}{1}{2}", color.r, color.g, color.b);
		}

		public static string ToHexStringRGBA(this Color col)
		{
			Color32 color = col;
			return string.Format("{0}{1}{2}{3}", color.r, color.g, color.b, color.a);
		}

		public static bool TryParseHexString(string hexString, out Color color)
		{
			try
			{
				color = FromHexString(hexString);
				return true;
			}
			catch
			{
				color = Color.white;
				return false;
			}
		}

		private static Color FromHexString(string hexString)
		{
			if (string.IsNullOrEmpty(hexString))
			{
				throw new InvalidOperationException("Cannot convert an empty/null string.");
			}
			var trimChars = new[] { '#' };
			var str = hexString.Trim(trimChars);
			switch (str.Length)
			{
				case 3:
					{
						var chArray2 = new[] { str[0], str[0], str[1], str[1], str[2], str[2], 'F', 'F' };
						str = new string(chArray2);
						break;
					}
				case 4:
					{
						var chArray3 = new[] { str[0], str[0], str[1], str[1], str[2], str[2], str[3], str[3] };
						str = new string(chArray3);
						break;
					}
				default:
					if (str.Length < 6)
					{
						str = str.PadRight(6, '0');
					}
					if (str.Length < 8)
					{
						str = str.PadRight(8, 'F');
					}
					break;
			}
			var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
			var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
			var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
			var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

			return new Color32(r, g, b, a);
		}
	}
}