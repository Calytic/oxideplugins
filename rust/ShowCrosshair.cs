using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;

namespace Oxide.Plugins
{
    [Info("ShowCrosshair", "Marat", "1.0.5", ResourceId = 2057)]
	[Description("Shows a crosshair on the screen.")]

    class ShowCrosshair : RustPlugin
    { 
	    List<ulong> Toggle = new List<ulong>();
		bool HasEnabled(BasePlayer player) => Toggle.Contains(player.userID);

		#region Initialization
		
		private bool configChanged;
	    private const string permShowCrosshair = "showcrosshair.allowed";
		private string background = "http://i.imgur.com/mD8K49U.png";
		private string background2 = "http://i.imgur.com/mYV1bFs.png";

        void Loaded()
        {
			LoadConfiguration();
            LoadDefaultMessages();
            permission.RegisterPermission(permShowCrosshair, this);
            cmd.AddChatCommand(command, this, "cmdChatCrosshair");
			cmd.AddChatCommand(commandmenu, this, "cmdChatShowMenu");
			cmd.AddConsoleCommand($"global.{command}", this, "cmdConsoleCrosshair");
        }
		
		#endregion
		
		#region Configuration
		
		protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created.");
            Config.Clear();
		}
		
		private bool usePermissions = false;
		private bool ShowOnLogin = false;
		private bool EnableSound = true;
		private bool KeyBindSet = true;
		private string SoundOpen = "assets/bundled/prefabs/fx/build/promote_metal.prefab";
		private string SoundDisable = "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab";
		private string SoundSelect = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab";
		private string SoundToggle = "assets/prefabs/misc/xmas/presents/effects/unwrap.prefab";
		private string commandmenu = "showmenu";
		private string command = "crosshair";
		private string keybindClose = "";
		private string keybind = "f5";
		private string colorClose = "0 0 0 0.7";
		private string colorBackground = "0 0 0 0.7";
		private string colorToggle = "0 0 0 0.7";
		private string colorDisable = "0 0 0 0.7";
		private string image1 = "http://i.imgur.com/n1y3P5t.png";
	    private string image2 = "http://i.imgur.com/v6dqmPI.png";
	    private string image3 = "http://i.imgur.com/oTcb8fz.png";
		private string image4 = "http://i.imgur.com/FRpk2mJ.png";
		private string image5 = "http://i.imgur.com/8Jrca6t.png";
	    private string image6 = "http://i.imgur.com/K7yirTy.png";
	    private string image7 = "http://i.imgur.com/beHkRnR.png";
		private string image8 = "http://i.imgur.com/tB088dk.png";
		
		private void LoadConfiguration()
        {
			command = GetConfigValue("Options", "Command", command);
			commandmenu = GetConfigValue("Options", "CommandMenu", commandmenu);
			keybind = GetConfigValue("Options", "KeyBindMenu", keybind);
			keybindClose = GetConfigValue("Options", "KeyBindClose", keybindClose);
			KeyBindSet = GetConfigValue("Options", "KeyBindSet", KeyBindSet);
			ShowOnLogin = GetConfigValue("Options", "ShowOnLogin", ShowOnLogin);
			EnableSound = GetConfigValue("Options", "EnableSound", EnableSound);
			usePermissions = GetConfigValue("Options", "UsePermissions", usePermissions);
			
			SoundOpen = GetConfigValue("Sound", "SoundOpen", SoundOpen);
			SoundDisable = GetConfigValue("Sound", "SoundDisable", SoundDisable);
			SoundSelect = GetConfigValue("Sound", "SoundSelect", SoundSelect);
			SoundToggle = GetConfigValue("Sound", "SoundToggle", SoundToggle);
			
			colorClose = GetConfigValue("Color", "ColorButtonClose", colorClose);
			colorToggle = GetConfigValue("Color", "ColorButtonToggle", colorToggle);
			colorDisable = GetConfigValue("Color", "ColorButtonDisable", colorDisable);
			colorBackground = GetConfigValue("Color", "ColorBackground", colorBackground);
			
			image1 = GetConfigValue("Image", "Crosshair1", image1);
			image2 = GetConfigValue("Image", "Crosshair2", image2);
			image3 = GetConfigValue("Image", "Crosshair3", image3);
			image4 = GetConfigValue("Image", "Crosshair4", image4);
			image5 = GetConfigValue("Image", "Crosshair5", image5);
			image6 = GetConfigValue("Image", "Crosshair6", image6);
			image7 = GetConfigValue("Image", "Crosshair7", image7);
			image8 = GetConfigValue("Image", "Crosshair8", image8);
			
			if (!configChanged) return;
            PrintWarning("Configuration file updated.");
            SaveConfig();
        }
		
		private T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        private void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }
		
		#endregion
		
		#region Localization
		
		void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "You don't have permission to use this command.",
				["Enabled"] = "You have enabled the crosshair.",
                ["Disabled"] = "You have disabled the crosshair.",
				["crosshair1"] = "You set the crosshair â1.",
				["crosshair2"] = "You set the crosshair â2.",
				["crosshair3"] = "You set the crosshair â3.",
				["crosshair4"] = "You set the crosshair â4.",
				["crosshair5"] = "You set the crosshair â5.",
				["crosshair6"] = "You set the crosshair â6.",
				["crosshair7"] = "You set the crosshair â7.",
				["crosshair8"] = "You set the crosshair â8."
            }, this, "en");
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "Ð£ Ð²Ð°Ñ Ð½ÐµÑ ÑÐ°Ð·ÑÐµÑÐµÐ½Ð¸Ñ Ð½Ð° Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°Ð½Ð¸Ðµ ÑÑÐ¾Ð¹ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ.",
				["Enabled"] = "ÐÑ Ð²ÐºÐ»ÑÑÐ¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ.",
                ["Disabled"] = "ÐÑ Ð¾ÑÐºÐ»ÑÑÐ¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ.",
				["crosshair1"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â1.",
				["crosshair2"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â2.",
				["crosshair3"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â3.",
				["crosshair4"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â4.",
				["crosshair5"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â5.",
				["crosshair6"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â6.",
				["crosshair7"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â7.",
				["crosshair8"] = "ÐÑ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð¿ÐµÑÐµÐºÑÐµÑÑÐ¸Ðµ â8."
            }, this, "ru");
        }

        #endregion
		
		#region Commands
		
		/////Crosshair/////
		void cmdChatCrosshair(BasePlayer player)
		{
			if (usePermissions && !IsAllowed(player.UserIDString, permShowCrosshair))
            {
                Reply(player, Lang("NoPermission", player.UserIDString));
                return;
            }
			if (HasEnabled(player))
			{
                DisabledCrosshair(player);
			}
            else
			{
                EnabledCrosshair(player);
			}
        }
		void cmdConsoleCrosshair(ConsoleSystem.Arg arg)
	    {
		    var player = arg.Player();
		    cmdChatCrosshair(player);
	    }
		////ShowMenu////
		void cmdChatShowMenu(BasePlayer player)
		{
			if (usePermissions && !IsAllowed(player.UserIDString, permShowCrosshair))
            {
                Reply(player, Lang("NoPermission", player.UserIDString));
                return;
            }
			if (HasEnabled(player))
			{
                DisabledMenu(player);
			}
            else
			{
                EnabledMenu(player);
				if(EnableSound)Effect.server.Run(SoundOpen, player.transform.position, Vector3.zero, null, false);
			}
        }
		[ConsoleCommand("ShowMenu")]
        private void cmdConsoleShowMenu(ConsoleSystem.Arg arg)
	    {
			var player = arg.Player();
			cmdChatShowMenu(player);
	    }
		////CloseMenu////
		[ConsoleCommand("CloseMenu")]
        void cmdConsoleCloseMenu(ConsoleSystem.Arg arg)
	    {
		    var player = arg.Player();
		    DisabledMenu(player);
	    }
		////Commands////
		[ConsoleCommand("command1")]
        void cmdConsoleCommand1(ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair1(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair1", player.UserIDString));
        }
        [ConsoleCommand("command2")]
        void cmdConsoleCommand2(ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair2(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair2", player.UserIDString));
        }
        [ConsoleCommand("command3")]
        void cmdConsoleCommand3(ConsoleSystem.Arg arg)
        {
		    var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair3(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair3", player.UserIDString));
        }
        [ConsoleCommand("command4")]
        void cmdConsoleCommand4(ConsoleSystem.Arg arg)
		{
		    var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair4(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair4", player.UserIDString));
	    }
		[ConsoleCommand("command5")]
        void cmdConsoleCommand5(ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair5(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair5", player.UserIDString));
        }
        [ConsoleCommand("command6")]
        void cmdConsoleCommand6(ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair6(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair6", player.UserIDString));
        }
        [ConsoleCommand("command7")]
        void cmdConsoleCommand7(ConsoleSystem.Arg arg)
        {
		    var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair7(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair7", player.UserIDString));
        }
        [ConsoleCommand("command8")]
        void cmdConsoleCommand8(ConsoleSystem.Arg arg)
		{
		    var player = arg.Player();
			DestroyCrosshair(player);
		    ShowCrosshair8(player);
			if(EnableSound)Effect.server.Run(SoundSelect, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("crosshair8", player.UserIDString));
	    }
		[ConsoleCommand("commandNext")]
        void cmdConsoleCommandNext(ConsoleSystem.Arg arg)
		{
		    var player = arg.Player();
			DestroyGUImenu(player);
			NextMenu(player, null);
			if(EnableSound)Effect.server.Run(SoundToggle, player.transform.position, Vector3.zero, null, false);
	    }
		[ConsoleCommand("commandBack")]
        void cmdConsoleCommandBack(ConsoleSystem.Arg arg)
		{
		    var player = arg.Player();
			DestroyGUImenu(player);
			ShowMenu(player, null);
			if(EnableSound)Effect.server.Run(SoundToggle, player.transform.position, Vector3.zero, null, false);
	    }
		[ConsoleCommand("commandDisable")]
        void cmdConsoleCommandDisable(ConsoleSystem.Arg arg)
		{
		    var player = arg.Player();
			DestroyCrosshair(player);
			if(EnableSound)Effect.server.Run(SoundDisable, player.transform.position, Vector3.zero, null, false);
			Reply(player, Lang("Disabled", player.UserIDString));
	    }
		
		#endregion
		
		#region Hooks
		
        void OnPlayerInit(BasePlayer player)
        {
            if (usePermissions && !IsAllowed(player.UserIDString, permShowCrosshair))
            {
                return;
            }
			if (ShowOnLogin)
		    {
				player.SendConsoleCommand($"global.{command}");
		    }
			if (KeyBindSet)
            {
                player.Command("bind " + keybind + " \"ShowMenu\"");
				player.Command("bind " + keybindClose + " \"CloseMenu\"");
            }
	    }
		void OnPlayerDisconnected(BasePlayer player)
	    {
			if (Toggle.Contains(player.userID))
            {
			    if (KeyBindSet)
                {
			        player.SendConsoleCommand("bind " + keybind + " \"\"");
			        player.SendConsoleCommand("bind " + keybindClose + " \"\"");
			    }
                Toggle.Remove(player.userID);
			    DestroyAll(player);
			    return;
			}
	    }
		void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                Toggle.Remove(player.userID);
				DestroyAll(player);
				return;
            }
        }
		void DestroyAll(BasePlayer player)
	    {
			DestroyGUImenu(player);
		    DestroyCrosshair(player);
	    }
		void DestroyCrosshair(BasePlayer player)
	    {
		    CuiHelper.DestroyUi(player, "image1");
			CuiHelper.DestroyUi(player, "image2");
			CuiHelper.DestroyUi(player, "image3");
			CuiHelper.DestroyUi(player, "image4");
			CuiHelper.DestroyUi(player, "image5");
			CuiHelper.DestroyUi(player, "image6");
			CuiHelper.DestroyUi(player, "image7");
			CuiHelper.DestroyUi(player, "image8");
	    }
		void DestroyGUImenu(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "GUImenu");
			CuiHelper.DestroyUi(player, "GUImenu2");
		}
		void EnabledCrosshair(BasePlayer player)
        {
            if (!Toggle.Contains(player.userID))
            {
                Toggle.Add(player.userID);
				DestroyCrosshair(player);
				ShowCrosshair1(player);
                Reply(player, Lang("Enabled", player.UserIDString));
            }
        }
        void DisabledCrosshair(BasePlayer player)
        {
            if (Toggle.Contains(player.userID))
            {
                Toggle.Remove(player.userID);
			    DestroyCrosshair(player);
                Reply(player, Lang("Disabled", player.UserIDString));
            }
        }
		void EnabledMenu(BasePlayer player)
        {
            if (!Toggle.Contains(player.userID))
            {
                Toggle.Add(player.userID);
				DestroyGUImenu(player);
		        ShowMenu(player, null);
            }
        }
        void DisabledMenu(BasePlayer player)
        {
            if (Toggle.Contains(player.userID))
            {
                Toggle.Remove(player.userID);
			    DestroyGUImenu(player);
            }
        }
		
		#endregion
		
		#region Crosshair
		
	    void ShowCrosshair1(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "image1",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image1,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair2(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image2",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image2,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair3(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image3",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image3,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair4(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image4",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image4,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair5(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
            elements.Add(new CuiElement
            {
                Name = "image5",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image5,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair6(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image6",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image6,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair7(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image7",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image7,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		void ShowCrosshair8(BasePlayer player)
        {
		    var elements = new CuiElementContainer();
			elements.Add(new CuiElement
            {
                Name = "image8",
				Parent = "Hud",
                Components =
                {
                    new CuiRawImageComponent 
					{ 
						Color = "1 1 1 1", 
						Url = image8,
						Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					},
                    new CuiRectTransformComponent 
					{ 
						AnchorMin = "0.490 0.4812",
                        AnchorMax = "0.509 0.517"
					}
                }
            });
			CuiHelper.AddUi(player, elements);
		}
		
		#endregion
		
		#region GuiMenu
		
		/////////////////Menu1/////////////////////
		
		void ShowMenu(BasePlayer player, string text)
        {
			var elements = new CuiElementContainer();
            var menu = elements.Add(new CuiPanel
            {
                Image =
                {
					FadeIn = 0.6f,
                    Color = colorBackground
                },
                RectTransform =
                {
                    AnchorMin = "0.2395 0.18",
                    AnchorMax = "0.761 0.4525"
                },
                CursorEnabled = true
            }, "Hud", "GUImenu"); 
			var buttonClose = new CuiButton
            {
                Button =
                {
                    Command = "CloseMenu",
					FadeIn = 0.6f,
                    Color = colorClose
                },
                RectTransform =
                {
                    AnchorMin = "0.402 -0.225",
                    AnchorMax = "0.596 -0.058"
                },
                Text =
                {
                    Text = "<color=#ff0000>C</color><color=#ff1a1a>l</color><color=#ff3333>o</color><color=#ff1a1a>s</color><color=#ff0000>e</color>",
	   /////rus/////Text = "<color=#ff0000>Ð</color><color=#ff1a1a>Ð°</color><color=#ff3333>Ðº</color><color=#ff4d4d>Ñ</color><color=#ff3333>Ñ</color><color=#ff1a1a>Ñ</color><color=#ff0000>Ñ</color>",
                    FontSize = 18,
					FadeIn = 0.6f,
                    Align = TextAnchor.MiddleCenter
                }
            };
			
			/////////////button///////////////////
			
            elements.Add(buttonClose, menu);
            {
				//button1
                elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command1",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.0445 0.11",
                        AnchorMax = $"0.236 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",						
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button2
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command2",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.282 0.11",
                        AnchorMax = $"0.476 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",						
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button3
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command3",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.523 0.11",
                        AnchorMax = $"0.715 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button4
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command4",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.762 0.11",
                        AnchorMax = $"0.954 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//buttonDisable
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"commandDisable",
						FadeIn = 0.6f,
                        Color = colorDisable
                    },
                    RectTransform =
                    {
                        AnchorMin = "-0.003 -0.226",
                        AnchorMax = "0.192 -0.060"
                    },
                    Text =
                    {
                        Text = "<color=#fbff00>D</color><color=#fbff1a>i</color><color=#fcff33>s</color><color=#fcff4d>a</color><color=#fcff33>b</color><color=#fbff1a>l</color><color=#fbff00>e</color>",
		   /////rus/////Text = "<color=#e2e600>Ð</color><color=#fbff00>Ñ</color><color=#fbff1a>Ðº</color><color=#fcff33>Ð»</color><color=#fcff4d>Ñ</color><color=#fcff33>Ñ</color><color=#fbff1a>Ð¸</color><color=#fbff00>Ñ</color><color=#e2e600>Ñ</color>",
                        FontSize = 18,
						FadeIn = 0.6f,
                        Align = TextAnchor.MiddleCenter
                    }
                }, menu);
				//buttonNext
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"commandNext",
						FadeIn = 0.6f,
                        Color = colorToggle
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.805 -0.226",
                        AnchorMax = "1 -0.060"
                    },
                    Text =
                    {
                        Text = "<color=#0055ff>N</color><color=#1a66ff>e</color><color=#1a66ff>x</color><color=#0055ff>t</color>",
		   /////rus/////Text = "<color=#0055ff>Ð</color><color=#1a66ff>Ð°</color><color=#3377ff>Ð»</color><color=#1a66ff>Ðµ</color><color=#0055ff>Ðµ</color>",
                        FontSize = 18,
						FadeIn = 0.6f,
                        Align = TextAnchor.MiddleCenter
                    }
                }, menu);
				
				////////////////background///////////////
				
				//background1
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.2555 0.195",
                            AnchorMax = $"0.3705 0.44"
				        }
                    }
                });
				//background2
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.3805 0.195",
                            AnchorMax = $"0.4955 0.44"
				        }
                    }
                });
				//background3
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.5055 0.195",
                            AnchorMax = $"0.6205 0.44"
				        }
                    }
                });
				//background4
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.6305 0.195",
                            AnchorMax = $"0.7455 0.44"
				        }
                    }
                });
				
				////////////////image////////////////
				
				//image1
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image1,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.294 0.295",
                            AnchorMax = $"0.335 0.365"
				        }
                    }
                });
				//image2
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image2,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.42 0.295",
                            AnchorMax = $"0.46 0.365"
				        }
                    }
                });
				//image3
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image3,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.545 0.295",
                            AnchorMax = $"0.585 0.365"
				        }
                    }
                });
				//image4
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image4,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.666 0.295",
                            AnchorMax = $"0.710 0.365"
				        }
                    }
                });
				
				////////////MainBackground////////////////
				
				elements.Add(new CuiElement
                {
                    Name = menu,
					FadeOut = 0.3f,
				    Parent = "Hud.Under",
                    Components =
                    {
                        new CuiRawImageComponent 
					    { 
						    Color = "1 1 1 1", 
							FadeIn = 0.3f,
						    Url = background2,
						    Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					    },
                        new CuiRectTransformComponent 
					    { 
						    AnchorMin = "0.2365 0.110",
                            AnchorMax = "0.7635 0.468"
					    }
                    }
                });
            }
            CuiHelper.AddUi(player, elements);
        }
		
		/////////////////Menu2/////////////////////
		
		void NextMenu(BasePlayer player, string text)
        {
			var elements = new CuiElementContainer();
            var menu = elements.Add(new CuiPanel
            {
                Image =
                {
					FadeIn = 0.6f,
                    Color = colorBackground
                },
                RectTransform =
                {
                    AnchorMin = "0.2395 0.18",
                    AnchorMax = "0.761 0.4525"
                },
                CursorEnabled = true
            }, "Hud", "GUImenu2"); 
			var buttonClose = new CuiButton
            {
                Button =
                {
                    Command = "CloseMenu",
					FadeIn = 0.6f,
                    Color = colorClose
                },
                RectTransform =
                {
                    AnchorMin = "0.402 -0.225",
                    AnchorMax = "0.596 -0.058"
                },
                Text =
                {
                    Text = "<color=#ff0000>C</color><color=#ff1a1a>l</color><color=#ff3333>o</color><color=#ff1a1a>s</color><color=#ff0000>e</color>",
	   /////rus/////Text = "<color=#ff0000>Ð</color><color=#ff1a1a>Ð°</color><color=#ff3333>Ðº</color><color=#ff4d4d>Ñ</color><color=#ff3333>Ñ</color><color=#ff1a1a>Ñ</color><color=#ff0000>Ñ</color>",
                    FontSize = 18,
					FadeIn = 0.6f,
                    Align = TextAnchor.MiddleCenter
                }
            };
			
			/////////////button///////////////////
			
            elements.Add(buttonClose, menu);
            {
				//button5
                elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command5",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.0445 0.11",
                        AnchorMax = $"0.236 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",						
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button6
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command6",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.282 0.11",
                        AnchorMax = $"0.476 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",						
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button7
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command7",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.523 0.11",
                        AnchorMax = $"0.715 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//button8
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"command8",
						FadeIn = 0.6f,
                        Color = "0 0 0 0"
                    },
                    RectTransform =
                    {
                        AnchorMin = $"0.762 0.11",
                        AnchorMax = $"0.954 0.85"
                    },
                    Text =
                    {
                        Text = "<color=#46d100>S</color><color=#52f500>e</color><color=#66ff1a>l</color><color=#52f500>e</color><color=#46d100>c</color><color=#3aad00>t</color>",
		   /////rus/////Text = "<color=#3aad00>Ð</color><color=#46d100>Ñ</color><color=#52f500>Ð±</color><color=#66ff1a>Ñ</color><color=#52f500>Ð°</color><color=#46d100>Ñ</color><color=#3aad00>Ñ</color>",
                        FontSize = 20,
						FadeIn = 0.6f,
                        Align = TextAnchor.LowerCenter
                    }
                }, menu);
				//buttonDisable
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"commandDisable",
						FadeIn = 0.6f,
                        Color = colorDisable
                    },
                    RectTransform =
                    {
                        AnchorMin = "-0.003 -0.226",
                        AnchorMax = "0.192 -0.060"
                    },
                    Text =
                    {
                        Text = "<color=#fbff00>D</color><color=#fbff1a>i</color><color=#fcff33>s</color><color=#fcff4d>a</color><color=#fcff33>b</color><color=#fbff1a>l</color><color=#fbff00>e</color>",
		   /////rus/////Text = "<color=#e2e600>Ð</color><color=#fbff00>Ñ</color><color=#fbff1a>Ðº</color><color=#fcff33>Ð»</color><color=#fcff4d>Ñ</color><color=#fcff33>Ñ</color><color=#fbff1a>Ð¸</color><color=#fbff00>Ñ</color><color=#e2e600>Ñ</color>",
                        FontSize = 18,
						FadeIn = 0.6f,
                        Align = TextAnchor.MiddleCenter
                    }
                }, menu);
				//buttonBack
				elements.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"commandBack",
						FadeIn = 0.6f,
                        Color = colorToggle
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.805 -0.226",
                        AnchorMax = "1 -0.060"
                    },
                    Text =
                    {
                        Text = "<color=#0055ff>B</color><color=#1a66ff>a</color><color=#1a66ff>c</color><color=#0055ff>k</color>",
		   /////rus/////Text = "<color=#0055ff>Ð</color><color=#1a66ff>Ð°</color><color=#3377ff>Ð·</color><color=#1a66ff>Ð°</color><color=#0055ff>Ñ</color>",
                        FontSize = 18,
						FadeIn = 0.6f,
                        Align = TextAnchor.MiddleCenter
                    }
                }, menu);
				
				////////////////background///////////////
				
				//background1
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.2555 0.195",
                            AnchorMax = $"0.3705 0.44"
				        }
                    }
                });
				//background2
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.3805 0.195",
                            AnchorMax = $"0.4955 0.44"
				        }
                    }
                });
				//background3
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.5055 0.195",
                            AnchorMax = $"0.6205 0.44"
				        }
                    }
                });
				//background4
				elements.Add(new CuiElement
                {
                    Name = menu,
					Parent = "Hud.Under",
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1",
							FadeIn = 0.3f,
							Url = background,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.6305 0.195",
                            AnchorMax = $"0.7455 0.44"
				        }
                    }
                });
				
				////////////////image////////////////
				
				//image5
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image5,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.294 0.295",
                            AnchorMax = $"0.335 0.365"
				        }
                    }
                });
				//image6
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image6,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.42 0.295",
                            AnchorMax = $"0.46 0.365"
				        }
                    }
                });
				//image7
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image7,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.545 0.295",
                            AnchorMax = $"0.585 0.365"
				        }
                    }
                });
				//image8
				elements.Add(new CuiElement
                {
                    Name = menu,
			        Components =
                    {
                        new CuiRawImageComponent
				        { 
					        Color = "1 1 1 1", 
					        Url = image8,
					        Sprite = "assets/content/textures/generic/fulltransparent.tga" 
				        },
                        new CuiRectTransformComponent 
				        { 
					        AnchorMin = $"0.666 0.295",
                            AnchorMax = $"0.710 0.365"
				        }
                    }
                });
				
				////////////MainBackground////////////////
				
				elements.Add(new CuiElement
                {
                    Name = menu,
					FadeOut = 0.3f,
				    Parent = "Hud.Under",
                    Components =
                    {
                        new CuiRawImageComponent 
					    { 
						    Color = "1 1 1 1", 
							FadeIn = 0.3f,
						    Url = background2,
						    Sprite = "assets/content/textures/generic/fulltransparent.tga" 
					    },
                        new CuiRectTransformComponent 
					    { 
						    AnchorMin = "0.2365 0.110",
                            AnchorMax = "0.7635 0.468"
					    }
                    }
                });
            }
            CuiHelper.AddUi(player, elements);
        }
		
		#endregion
		 
		#region Helpers
		
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void Reply(BasePlayer player, string message, string args = null) => PrintToChat(player, $"{message}", args);
		
		bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);
		
        #endregion
    }
}