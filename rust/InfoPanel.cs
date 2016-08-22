using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("InfoPanel", "Ghosst / Nogrod", "0.9.5", ResourceId = 1356)]
    [Description("A little panel with useful informations.")]
    public class InfoPanel : RustPlugin
    {
        #region DefaultConfigs
        private static string DefaultFontColor = "1 1 1 1";
        #endregion

        private Timer TestTimer;

        private Dictionary<string, Dictionary<string, IPanel>> PlayerPanels = new Dictionary<string, Dictionary<string, IPanel>>();
        private Dictionary<string, Dictionary<string, IPanel>> PlayerDockPanels = new Dictionary<string, Dictionary<string, IPanel>>();

        private Dictionary<string, List<string>> LoadedPluginPanels = new Dictionary<string, List<string>>();

        #region DefaultConfig

        private static PluginConfig Settings;

        private readonly List<string> TimeFormats = new List<string>
        {
            "H:mm",
            "HH:mm",
            "h:mm",
            "h:mm tt",
        };

        PluginConfig DefaultConfig()
        {
            var DefaultConfig = new PluginConfig
            {
                ThirdPartyPanels = new Dictionary<string, Dictionary<string, PanelConfig>>(),

                Messages = Messages,
                TimeFormats = TimeFormats,
                CompassDirections = new Dictionary<string, string>
                {
                    {"n","North"},
                    {"ne","Northeast"},
                    {"e","East"},
                    {"se","Southeast"},
                    {"s","South"},
                    {"sw","Southwest"},
                    {"w","West"},
                    {"nw","Northwest"},
                },
                Docks = new Dictionary<string, DockConfig>
                {
                    { "BottomLeftDock", new DockConfig
                        {
                            Available = true,
                            Width = 0.18f,
                            Height = 0.03f,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0.005 0.162 0.005 0.005",
                            BackgroundColor = "0 0 0 0.4",
                        }
                    },
                    { "BottomRightDock", new DockConfig
                        {
                            Available = true,
                            Width = 0.19f,
                            Height = 0.03f,
                            AnchorX = "Right",
                            AnchorY = "Bottom",
                            Margin = "0.005 0.005 0.005 0.165",
                            BackgroundColor = "0 0 0 0.4",
                        }
                    },
                    { "TopLeftDock", new DockConfig
                        {
                            Available = true,
                            Width = 0.175f,
                            Height = 0.03f,
                            AnchorX = "Left",
                            AnchorY = "Top",
                            Margin = "0.005 0.175 0.005 0.005",
                            BackgroundColor = "0 0 0 0.4",
                        }
                    },
                    { "TopRightDock", new DockConfig
                        {
                            Available = true,
                            Width = 0.39f,
                            Height = 0.03f,
                            AnchorX = "Right",
                            AnchorY = "Top",
                            Margin = "0.005 0.005 0.005 0.005",
                            BackgroundColor = "0 0 0 0.4",
                        }
                    }
                },

                Panels = new Dictionary<string, PanelConfig>
                {
                    {"Clock", new PanelConfig
                        {
                            Available = true,
                            Dock = "BottomLeftDock",
                            Order = 1,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.2f,
                            Height = 0.95f,
                            BackgroundColor = "0.1 0.1 0.1 0",
                            Text = new PanelTextConfig
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 14,
                                Margin = "0 0.01 0 0.01",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "ClockUpdateFrequency (seconds)" , ClockUpdateFrequency },
                                { "TimeFormat", "HH:mm" }
                            }
                        }
                    },
                    { "MessageBox", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopRightDock",
                            Order = 7,
                            AnchorX = "Right",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.005",
                            Width = 1f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4",
                            Text = new PanelTextConfig
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 14,
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "MessageUpdateFrequency (seconds)", MessageUpdateFrequency },
                                { "MsgOrder","normal" }
                            }
                        }
                    },
                    { "Balance", new PanelConfig
                        {
                            Available = true,
                            Dock = "BottomLeftDock",
                            Order = 7,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.8f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4" ,
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.1f,
                                Height = 0.8f,
                                Margin = "0 0.01 0.1 0.01",
                                Url = "http://i.imgur.com/HhL5TvU.png",
                            },
                            Text = new PanelTextConfig
                            {
                                Order =  2,
                                Width = 0.848f,
                                Height = 1f,
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 12,
                                Margin = "0 0.02 0 0",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "RefreshRate(s)", "5" },
                            }
                        }
                    },
                    { "Coordinates", new PanelConfig
                        {
                            Available = true,
                            Dock = "BottomRightDock",
                            Order = 7,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.5f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4" ,
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.13f,
                                Height = 0.8f,
                                Margin = "0 0.01 0.1 0.01",
                                Url = "http://i.imgur.com/Kr1pQ5b.png",
                            },
                            Text = new PanelTextConfig
                            {
                                Order =  2,
                                Width = 0.848f,
                                Height = 1f,
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 12,
                                Margin = "0 0.02 0 0",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "RefreshRate(s)", "3" },
                            }
                        }
                    },
                    { "Compass", new PanelConfig
                        {
                            Available = true,
                            Dock = "BottomRightDock",
                            Order = 8,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.5f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4" ,
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.188f,
                                Height = 0.8f,
                                Margin = "0 0.01 0.1 0.03",
                                Url = "http://i.imgur.com/dG5nOOJ.png",
                            },
                            Text = new PanelTextConfig
                            {
                                Order =  2,
                                Width = 0.76f,
                                Height = 1f,
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 12,
                                Margin = "0 0.02 0 0",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "RefreshRate(s)", "1" },
                                { "TextOrAngle", "text" }
                            }
                        }
                    },
                    { "OPlayers", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopLeftDock",
                            Order = 2,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.31f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4" ,
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.35f,
                                Height = 0.8f,
                                Margin = "0 0.05 0.1 0.05",
                                Url = "http://i.imgur.com/n9EYIWi.png",
                            },
                            Text = new PanelTextConfig
                            {
                                Order =  2,
                                Width = 0.68f,
                                Height = 1f,
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 14,
                            }
                        }
                    },
                    { "Sleepers", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopLeftDock",
                            Order = 3,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.17f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4",
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.5f,
                                Height = 0.8f,
                                Margin = "0 0.05 0.1 0.05",
                                Url = "http://i.imgur.com/XIIZkqD.png",
                            },
                            Text = new PanelTextConfig
                            {
                                Order =  2,
                                Width = 0.63f,
                                Height = 1f,
                                Align = TextAnchor.MiddleCenter,
                                FontColor = DefaultFontColor,
                                FontSize = 14,
                            }
                        }
                    },
                    { "AirdropEvent", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopLeftDock",
                            Order =  4,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.1f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4",
                            Image = new PanelImageConfig
                                {
                                    Order =  1,
                                    Width = 0.8f,
                                    Height = 0.8f,
                                    Margin = "0 0.1 0.1 0.1",
                                    Url = "http://i.imgur.com/dble6vf.png",
                                },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "InactiveColor", "1 1 1 0.1" },
                                { "ActiveColor", "0 1 0 1" },
                            }
                        }
                    },
                    { "HelicopterEvent", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopLeftDock",
                            Order = 5,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.1f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4",
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.75f,
                                Height = 0.8f,
                                Margin = "0 0.15 0.1 0.1",
                                Url = "http://i.imgur.com/hTTyTTx.png",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "InactiveColor", "1 1 1 0.1" },
                                { "ActiveColor", "0.7 0.2 0.2 1" },
                            }

                        }
                    },
                    { "Radiation", new PanelConfig
                        {
                            Available = true,
                            Dock = "TopLeftDock",
                            Order = 6,
                            AnchorX = "Left",
                            AnchorY = "Bottom",
                            Margin = "0 0 0 0.01",
                            Width = 0.1f,
                            Height = 0.95f,
                            BackgroundColor = "0 0 0 0.4",
                            Image = new PanelImageConfig
                            {
                                Order =  1,
                                Width = 0.75f,
                                Height = 0.8f,
                                Margin = "0 0.15 0.1 0.1",
                                Url = "http://i.imgur.com/owVdFsK.png",
                            },
                            PanelSettings = new Dictionary<string,object>
                            {
                                { "InactiveColor", "1 1 1 0.1" },
                                { "ActiveColor", "1 1 0 1" },
                                { "RefreshRate(s)", "3"}
                            }

                        }
                    }
                }
            };

            return DefaultConfig;
        }

        class PluginConfig
        {
            //public Dictionary<string, string> Settings { get; set; }

            public Dictionary<string, DockConfig> Docks { get; set; }
            public Dictionary<string, PanelConfig> Panels { get; set; }

            public Dictionary<string, Dictionary<string, PanelConfig>> ThirdPartyPanels { get; set; }

            public List<string> Messages { get; set; }
            public List<string> TimeFormats { get; set; }
            public Dictionary<string, string> CompassDirections { get; set; }

            public T GetPanelSettingsValue<T>(string Panel, string Setting, T defaultValue)
            {
                PanelConfig panelConfig;
                if (!Panels.TryGetValue(Panel, out panelConfig))
                    return defaultValue;

                if (panelConfig.PanelSettings == null)
                    return defaultValue;

                object value;
                if (!panelConfig.PanelSettings.TryGetValue(Setting, out value))
                    return defaultValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }

            public bool CheckPanelAvailability(string Panel)
            {
                PanelConfig panelConfig;
                if (!Panels.TryGetValue(Panel, out panelConfig))
                    return false;

                if (!panelConfig.Available)
                    return false;

                DockConfig dockConfig;
                return Docks.TryGetValue(panelConfig.Dock, out dockConfig) && dockConfig.Available;
            }

        }

        class DockConfig
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool Available { get; set; } = true;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AnchorX { get; set; } = "Left";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AnchorY { get; set; } = "Bottom";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float Width { get; set; } = 0.05f;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float Height { get; set; } = 0.95f;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string BackgroundColor { get; set; } = "0 0 0 0.4";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Margin { get; set; } = "0 0 0 0.005";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public PanelImageConfig Image { get; set; }
        }

        class BasePanelConfig
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool Available { get; set; } = true;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AnchorX { get; set; } = "Left";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AnchorY { get; set; } = "Bottom";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float Width { get; set; } = 0.05f;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float Height { get; set; } = 0.95f;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int Order { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string BackgroundColor { get; set; } = "0 0 0 0.4";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Margin { get; set; } = "0 0 0 0.005";
        }

        class PanelConfig : BasePanelConfig
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool Autoload { get; set; } = true;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Dock { get; set; } = "BottomLeftDock";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, object> PanelSettings { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public PanelImageConfig Image { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public PanelTextConfig Text { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float FadeOut { get; set; }
        }

        class PanelTextConfig : BasePanelConfig
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public new float Width { get; set; } = 1f;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TextAnchor Align { get; set; } = TextAnchor.MiddleCenter;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string FontColor { get; set; } = "1 1 1 1";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int FontSize { get; set; } = 14;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Content { get; set; } = "No Content";

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float FadeIn { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public float FadeOut { get; set; }
        }

        class PanelImageConfig : BasePanelConfig
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public new float Width { get; set; } = 1f;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Url { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Color { get; set; } = null;
        }

        protected void LoadConfigValues()
        {
            Settings = Config.ReadObject<PluginConfig>();

            var UnOrderPanels = Settings.Panels.Where(p => p.Value.Order == 0).ToDictionary(s => s.Key, s => s.Value);

            if (UnOrderPanels.Count == 0)
                return;

            PrintWarning("Reordering Panels.");

            foreach (var PanelCfg in UnOrderPanels)
            {
                //int HighestSiblingOrder = Settings.Panels.Where(p => p.Value.Dock == Settings.Panels[PanelName].Dock && p.Value.AnchorX == Settings.Panels[PanelName].AnchorX).Max(m => m.Value.Order);
                Settings.Panels[PanelCfg.Key].Order = PanelReOrder(PanelCfg.Value.Dock, PanelCfg.Value.AnchorX);
            }

            Config.WriteObject(Settings, true);
            PrintWarning("Config Saved.");
        }

        int PanelReOrder(string DockName, string AnchorX)
        {
            var SiblingPanels = Settings.Panels.Where(p => p.Value.Dock == DockName && p.Value.AnchorX == AnchorX);

            var Max = 0;
            if (SiblingPanels.Any())
                Max = SiblingPanels.Max(m => m.Value.Order);

            foreach (var pPanelCfg in Settings.ThirdPartyPanels)
            {
                if (pPanelCfg.Value.Count == 0) { continue; }

                var SiblingPluginPAnels = pPanelCfg.Value.Where(p => p.Value.Dock == DockName && p.Value.AnchorX == AnchorX);

                if (SiblingPluginPAnels.Any())
                {
                    var PluginMax = pPanelCfg.Value.Where(p => p.Value.Dock == DockName && p.Value.AnchorX == AnchorX).Max(m => m.Value.Order);
                    if (PluginMax > Max)
                        Max = PluginMax;
                }
            }
            return Max + 1;
        }

        #endregion

        #region Hooks

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(DefaultConfig(), true);
            PrintWarning("Default configuration file created.");
        }

        void Init()
        {
            LoadConfigValues();
            LoadData();
        }

        void OnServerInitialized()
        {
            Clock = new Watch
            (
                Settings.GetPanelSettingsValue("Clock", "ClockUpdateFrequency (seconds)", ClockUpdateFrequency),
                Settings.CheckPanelAvailability("Clock")
            );

            MessageBox = new Messenger
            (
                Settings.Messages,
                Settings.GetPanelSettingsValue("MessageBox", "MessageUpdateFrequency (seconds)", MessageUpdateFrequency),
                Settings.GetPanelSettingsValue("MessageBox", "MsgOrder", "normal")
            );

            Airplane = new AirplaneEvent();
            Helicopter = new HelicopterEvent();

            CompassObj = new Compass
            (
                Settings.GetPanelSettingsValue("Compass", "RefreshRate(s)", 1)
            );

            Rad = new Radiation
            (
                Settings.GetPanelSettingsValue("Radiation", "RefreshRate(s)", 3)
            );

            Bala = new Balance
            (
                Settings.GetPanelSettingsValue("Balance", "RefreshRate(s)", 3)
            );

            Coord = new Coordinates
            (
                Settings.GetPanelSettingsValue("Coordinates", "RefreshRate(s)", 3)
            );

            foreach (var player in BasePlayer.activePlayerList)
            {
                LoadPanels(player);
                InitializeGUI(player);
            }

            if (Settings.CheckPanelAvailability("Radiation"))
            {
                RadiationUpdater = timer.Repeat(Rad.RefreshRate, 0, () => Rad.Refresh(storedData, PlayerPanels));
            }

            if (Settings.CheckPanelAvailability("Balance"))
            {
                BalanceUpdater = timer.Repeat(Bala.RefreshRate, 0, () => Bala.Refresh(storedData, PlayerPanels));
            }

            if (Settings.CheckPanelAvailability("Coordinates"))
            {
                CoordUpdater = timer.Repeat(Coord.RefreshRate, 0, () => Coord.Refresh(storedData, PlayerPanels));
            }

            if (Settings.CheckPanelAvailability("MessageBox"))
            {
                MsgUpdater = timer.Repeat(MessageBox.RefreshRate, 0, () => MessageBox.Refresh(storedData, PlayerPanels));
            }

            if (Settings.CheckPanelAvailability("Clock"))
            {
                TimeUpdater = timer.Repeat(Clock.RefresRate, 0, () => Clock.Refresh(storedData, PlayerPanels));
            }

            if (Settings.CheckPanelAvailability("Compass"))
            {
                CompassUpdater = timer.Repeat(CompassObj.RefreshRate, 0, () => CompassObj.Refresh(storedData, PlayerPanels));
            }

            //TestTimer = timer.Repeat(5, 0, () => TestSH());

            ActivePlanes = UnityEngine.Object.FindObjectsOfType<CargoPlane>().ToList();

            if (ActivePlanes.Count > 0)
            {
                CheckAirplane();
            }
            else
            {
                Airplane.Refresh(storedData, PlayerPanels);
            }

            ActiveHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().ToList();

            if (ActiveHelicopters.Count > 0)
            {
                CheckHelicopter();
            }
            else
            {
                Helicopter.Refresh(storedData, PlayerPanels);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }
            if (PlayerPanels.ContainsKey(player.UserIDString))
            {
                PlayerPanels.Remove(player.UserIDString);
            }

            if (PlayerDockPanels.ContainsKey(player.UserIDString))
            {
                PlayerDockPanels.Remove(player.UserIDString);
            }

            timer.In(1, () => GUITimerInit(player));
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerPanels.Remove(player.UserIDString);
            PlayerDockPanels.Remove(player.UserIDString);

            timer.Once(2, RefreshOnlinePlayers);
            timer.Once(2, RefreshSleepers);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            timer.Once(2, RefreshSleepers);
        }

        private void OnEntitySpawned(BaseEntity Entity)
        {
            if (Entity == null) return;
            if (Entity is BaseHelicopter && Settings.Panels["HelicopterEvent"].Available)
            {
                ActiveHelicopters.Add((BaseHelicopter) Entity);

                if (HelicopterTimer == false)
                {
                    CheckHelicopter();
                }
            }


            if (Entity is CargoPlane && Settings.Panels["AirdropEvent"].Available)
            {
                ActivePlanes.Add((CargoPlane) Entity);

                if (AirplaneTimer == false)
                {
                    CheckAirplane();
                }
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyGUI(player);

            SaveData();

            PlayerPanels.Clear();
            PlayerDockPanels.Clear();

            Err.Clear();
            ErrD.Clear();
            ErrB.Clear();
            ErrA.Clear();

            storedData = null;
            Settings = null;
        }

        void OnPluginUnloaded(Plugin plugin)
        {
            if (!Settings.ThirdPartyPanels.ContainsKey(plugin.Title)) return;
            var PluginPanels = LoadedPluginPanels[plugin.Title];

            foreach(var PanelName in PluginPanels)
            {
                foreach (var pair in PlayerPanels)
                {
                    pair.Value[PanelName].DestroyPanel();
                    pair.Value[PanelName].Remover();
                }
            }

            LoadedPluginPanels.Remove(plugin.Title);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnServerShutdown()
        {
            SaveData();
        }

        #endregion

        #region PanelLoad
        /// <summary>
        /// </summary>
        /// <param name="Player"></param>
        private void LoadPanels(BasePlayer Player)
        {
            foreach(var Docks in Settings.Docks)
            {
                if (!Settings.Docks[Docks.Key].Available)
                    continue;
                LoadDockPanel(Docks.Key, Player);
            }

            var playerDockPanel = PlayerDockPanels[Player.UserIDString];
            foreach (var grouppedByDock in Settings.Panels.GroupBy(g => g.Value.Dock).ToDictionary(gd => gd.Key, gd => gd.Select(p => p).ToDictionary(gk => gk.Key, gk => gk.Value)))
            {
                if (!Settings.Docks[grouppedByDock.Key].Available)
                    continue;

                foreach (var panelCfg in grouppedByDock.Value)
                {
                    if (!Settings.CheckPanelAvailability(panelCfg.Key))
                        continue;

                    LoadPanel(playerDockPanel[grouppedByDock.Key], panelCfg.Key, panelCfg.Value);
                }
            }

            foreach(var loadedPluginPanel in LoadedPluginPanels)
            {
                foreach(var panelName in loadedPluginPanel.Value)
                {
                    Dictionary<string, PanelConfig> panelConfigs;
                    PanelConfig panelConfig;
                    if (!Settings.ThirdPartyPanels.TryGetValue(loadedPluginPanel.Key, out panelConfigs)
                        || !panelConfigs.TryGetValue(panelName, out panelConfig)
                        || !panelConfig.Available)
                        continue;

                    LoadPanel(playerDockPanel[panelConfig.Dock], panelName, panelConfig);
                }
            }
        }

        private IPanel LoadDockPanel(string DockName, BasePlayer Player)
        {
            var dockConfig = Settings.Docks[DockName];
            var DockPanel = new IPanel(DockName, Player, PlayerPanels, PlayerDockPanels)
            {
                Width = dockConfig.Width,
                Height = dockConfig.Height,
                AnchorX = dockConfig.AnchorX,
                AnchorY = dockConfig.AnchorY,
                Margin = Vector4Parser(dockConfig.Margin),
                BackgroundColor = ColorEx.Parse(dockConfig.BackgroundColor),
                IsDock = true
            };

            //LoadedDocks.Add(DockName, DockPanel);

            Dictionary<string, IPanel> panels;
            if(!PlayerDockPanels.TryGetValue(Player.UserIDString, out panels))
                PlayerDockPanels.Add(Player.UserIDString, panels = new Dictionary<string, IPanel>());
            panels.Add(DockName, DockPanel);

            return DockPanel;
        }

        private void LoadPanel(IPanel Dock, string PanelName, PanelConfig PCfg)
        {
            var Panel = Dock.AddPanel(PanelName);
            Panel.Width = PCfg.Width;
            Panel.Height = PCfg.Height;
            Panel.AnchorX = PCfg.AnchorX;
            Panel.AnchorY = PCfg.AnchorY;
            Panel.Margin = Vector4Parser(PCfg.Margin);
            Panel.BackgroundColor = ColorEx.Parse(PCfg.BackgroundColor);
            Panel.Order = PCfg.Order;
            Panel.Autoload = PCfg.Autoload;
            Panel.IsPanel = true;
            Panel.DockName = Dock.Name;
            Panel.FadeOut = Dock.FadeOut;

            if (PCfg.Text != null)
            {
                var Text = Panel.AddText(PanelName + "Text");
                Text.Width = PCfg.Text.Width;
                Text.Height = PCfg.Text.Height;
                Text.Margin = Vector4Parser(PCfg.Text.Margin);
                Text.Content = PCfg.Text.Content;
                Text.FontColor = ColorEx.Parse(PCfg.Text.FontColor);
                Text.FontSize = PCfg.Text.FontSize;
                Text.Align = PCfg.Text.Align;
                Text.Order = PCfg.Text.Order;
                Text.FadeOut = PCfg.Text.FadeOut;
                Text.TextComponent.FadeIn = PCfg.Text.FadeIn;
            }

            if (PCfg.Image != null)
            {
                var Image = Panel.AddImage(PanelName + "Image");
                Image.Width = PCfg.Image.Width;
                Image.Height = PCfg.Image.Height;
                Image.Margin = Vector4Parser(PCfg.Image.Margin);
                Image.Url = PCfg.Image.Url;
                Image.Order = PCfg.Image.Order;
                if (PCfg.Image.Color != null)
                    Image.Color = ColorEx.Parse(PCfg.Image.Color);
            }
        }

        #endregion

        #region Clock

        private Watch Clock;
        private int ClockUpdateFrequency = 4;
        private Timer TimeUpdater;

        public class Watch
        {
            string ClockFormat = "HH:mm";
            public int RefresRate = 4;
            public bool Available = true;

            TOD_Sky Sky = TOD_Sky.Instance;

            public Watch(int RefreshRate, bool Available)
            {
                RefresRate = RefreshRate;
                this.Available = Available;
            }

            public string GetServerTime(string PlayerID, StoredData storedData)
            {
                return DateTime.Now.AddHours(storedData.GetPlayerPanelSettings(PlayerID, "Clock", "Offset", 0)).ToString(storedData.GetPlayerPanelSettings(PlayerID, "Clock", "TimeFormat", ClockFormat), CultureInfo.InvariantCulture);
            }

            public string GetSkyTime(string PlayerID, StoredData storedData)
            {
                return Sky.Cycle.DateTime.ToString(storedData.GetPlayerPanelSettings(PlayerID, "Clock", "TimeFormat", ClockFormat), CultureInfo.InvariantCulture);
            }

            public string ShowTime(string PlayerID, StoredData storedData)
            {
                if (storedData.GetPlayerPanelSettings(PlayerID, "Clock", "Type", "Game") == "Server")
                    return GetServerTime(PlayerID, storedData);

                return GetSkyTime(PlayerID, storedData);
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("Clock"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("ClockText", out iPanel)) continue;
                    var showTime = ShowTime(panel.Key, storedData);
                    var panelText = (IPanelText)iPanel;
                    if (!showTime.Equals(panelText.Content))
                    {
                        panelText.Content = showTime;
                        panelText.Refresh();
                    }
                }
            }
        }

        #endregion

        #region MessageBox

        private Messenger MessageBox;
        private Timer MsgUpdater;
        private int MessageUpdateFrequency = 20;
        private List<string> Messages = new List<string> { "Welcome!", "Beware! You Are Not Alone!", "Leeeeeeeeeeeroy Jenkins" };
        private bool MessageBoxAvailable = true;


        public class Messenger
        {
            List<string> Messages;
            public int RefreshRate = 20;
            private int Counter = 0;
            private string MsgOrder = "normal";

            public Messenger(List<string> msgs, int RefreshRate,string MsgOrder)
            {
                Messages = msgs;
                this.RefreshRate = RefreshRate;
                this.MsgOrder = MsgOrder;

                if (MsgOrder == "random")
                {
                    Counter = Core.Random.Range(0, Messages.Count - 1);
                }

            }

            public string GetMessage()
            {
                return Messages[Counter];
            }

            private void RefreshCounter()
            {
                if (MsgOrder == "random")
                {
                    var OldCounter = Counter;
                    var NewCounter = Core.Random.Range(0, Messages.Count - 1);

                    if(OldCounter == NewCounter)
                    {
                        if(NewCounter+1 <= Messages.Count-1)
                        {
                            Counter = NewCounter + 1;
                            return;
                        }
                        else if(NewCounter - 1 >= 0)
                        {
                            Counter = NewCounter - 1;
                            return;
                        }
                    }

                    Counter = NewCounter;
                    return;
                }

                Counter++;
                if (Counter >= Messages.Count)
                    Counter = 0;
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("MessageBox"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("MessageBoxText", out iPanel)) continue;
                    var message = GetMessage();
                    var panelText = (IPanelText)iPanel;
                    if (!message.Equals(panelText.Content))
                    {
                        panelText.Content = message;
                        panelText.Refresh();
                    }
                }

                RefreshCounter();
            }

        }
        #endregion

        #region Events
        private Timer HeliAttack;
        private Timer RadiationUpdater;

        private AirplaneEvent Airplane;
        private List<CargoPlane> ActivePlanes;
        private bool AirplaneTimer = false;

        private HelicopterEvent Helicopter;
        private List<BaseHelicopter> ActiveHelicopters;
        private bool HelicopterTimer = false;

        private Radiation Rad;

        private BaseHelicopter ActiveHelicopter;

        public class AirplaneEvent
        {
            public bool isActive = false;
            public Color ImageColor;

            public AirplaneEvent()
            {
                ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("AirdropEvent", "InactiveColor", "1 1 1 0.1"));
            }

            public void SetActivity(bool active)
            {
                isActive = active;

                if (isActive)
                {
                    ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("AirdropEvent", "ActiveColor", "0 1 0 1"));
                    return;
                }
                ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("AirdropEvent", "InactiveColor", "1 1 1 0.1"));
                return;
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("AirdropEvent"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("AirdropEventImage", out iPanel)) continue;
                    var panelRawImage = (IPanelRawImage)iPanel;
                    if (panelRawImage.Color != ImageColor)
                    {
                        panelRawImage.Color = ImageColor;
                        panelRawImage.Refresh();
                    }
                }
            }
        }

        public class HelicopterEvent
        {
            public bool isActive = false;
            public Color ImageColor;

            public HelicopterEvent()
            {
                ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("HelicopterEvent", "InactiveColor", "1 1 1 0.1"));
            }

            public void SetActivity(bool active)
            {
                isActive = active;

                if (isActive)
                {
                    ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("HelicopterEvent", "ActiveColor", "1 0 0 1"));
                    return;
                }

                ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("HelicopterEvent", "InactiveColor", "1 1 1 0.1"));
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("HelicopterEvent"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("HelicopterEventImage", out iPanel)) continue;
                    var panelRawImage = (IPanelRawImage)iPanel;
                    if (panelRawImage.Color != ImageColor)
                    {
                        panelRawImage.Color = ImageColor;
                        panelRawImage.Refresh();
                    }
                }
            }
        }

        public class Radiation
        {
            bool isActive = false;
            public Color ImageColor;
            public int RefreshRate = 3;

            public Radiation(int RefreshRate)
            {
                isActive = ConVar.Server.radiation;
                this.RefreshRate = RefreshRate;
                if (isActive)
                {
                    ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("Radiation", "ActiveColor", "1 1 0 1"));
                }
                else
                {
                    ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("Radiation", "InactiveColor", "1 1 1 0.1"));
                }
            }

            public void SetActivity(bool active)
            {
                isActive = active;

                if (isActive)
                {
                    ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("Radiation", "ActiveColor", "1 0 0 1"));
                    return;
                }

                ImageColor = ColorEx.Parse(Settings.GetPanelSettingsValue("Radiation", "InactiveColor", "1 1 1 0.1"));
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (isActive == ConVar.Server.radiation)
                    return;

                SetActivity(ConVar.Server.radiation);

                if (!Settings.CheckPanelAvailability("Radiation"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("RadiationImage", out iPanel)) continue;
                    var panelRawImage = (IPanelRawImage)iPanel;
                    if (panelRawImage.Color != ImageColor)
                    {
                        panelRawImage.Color = ImageColor;
                        panelRawImage.Refresh();
                    }
                }
            }
        }

        public void CheckAirplane()
        {
            ActivePlanes.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);
            if (ActivePlanes.Count > 0)
            {
                if(Airplane.isActive == false)
                {
                    Airplane.SetActivity(true);
                    Airplane.Refresh(storedData, PlayerPanels);
                }

                AirplaneTimer = true;
                timer.In(10, CheckAirplane);
                return;
            }

            Airplane.SetActivity(false);
            Airplane.Refresh(storedData, PlayerPanels);
            AirplaneTimer = false;
        }

        public void CheckHelicopter()
        {
            ActiveHelicopters.RemoveAll(p => !p.IsValid() || !p.gameObject.activeInHierarchy);

            if (ActiveHelicopters.Count > 0)
            {

                if (Helicopter.isActive == false)
                {
                    Helicopter.SetActivity(true);
                    Helicopter.Refresh(storedData, PlayerPanels);
                }

                HelicopterTimer = true;
                timer.In(5, CheckHelicopter);
                return;
            }

            Helicopter.SetActivity(false);
            Helicopter.Refresh(storedData, PlayerPanels);
            HelicopterTimer = false;
        }

        #endregion

        #region Balance

        private Balance Bala;
        private Timer BalanceUpdater;
        public class Balance
        {
            public int RefreshRate = 3;

            public Balance(int RefreshRate)
            {
                this.RefreshRate = RefreshRate;
            }

            public double GetBalance(string PlayerID)
            {
                var player = RustCore.FindPlayerByIdString(PlayerID);
                if (player == null) return 0;
                return (double)(Interface.Oxide.CallHook("GetPlayerMoney", player.userID) ?? 0.0);
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("Balance"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("BalanceText", out iPanel)) continue;
                    var balance = $"{GetBalance(panel.Key):N}";
                    var panelText = (IPanelText)iPanel;
                    if (!balance.Equals(panelText.Content))
                    {
                        panelText.Content = balance;
                        panelText.Refresh();
                    }
                }
            }
        }
        #endregion

        #region Coordinates

        private Coordinates Coord;

        private Timer CoordUpdater;

        public class Coordinates
        {
            public int RefreshRate = 3;

            public Coordinates(int RefreshRate)
            {
                this.RefreshRate = RefreshRate;
            }

            public string GetCoord(string PlayerID)
            {
                var player = RustCore.FindPlayerByIdString(PlayerID);
                if (player == null) return string.Empty;
                return $"X: {player.transform.position.x.ToString("0")} | Z: {player.transform.position.z.ToString("0")}";
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("Coordinates"))
                    return;

                foreach (var panel in panels)
                {
                    IPanel iPanel;
                    if (!panel.Value.TryGetValue("CoordinatesText", out iPanel)) continue;
                    var coord = GetCoord(panel.Key);
                    var panelText = (IPanelText)iPanel;
                    if (!coord.Equals(panelText.Content))
                    {
                        panelText.Content = coord;
                        panelText.Refresh();
                    }
                }
            }
        }
        #endregion

        #region Compass

        private Compass CompassObj;

        private Timer CompassUpdater;

        public class Compass
        {
            public int RefreshRate = 3;

            public Compass(int RefreshRate)
            {
                this.RefreshRate = RefreshRate;
            }

            public string GetDirection(string PlayerID)
            {
                var player = RustCore.FindPlayerByIdString(PlayerID);

                if (player == null) return string.Empty;

                var PCurrent = player.eyes.rotation.eulerAngles;

                string str = $"{PCurrent.y.ToString("0")}\u00B0";

                if (Settings.GetPanelSettingsValue("Compass", "TextOrAngle", "text") == "text")
                {
                    if (PCurrent.y > 337.5 || PCurrent.y < 22.5)
                        str = Settings.CompassDirections["n"];
                    else if (PCurrent.y > 22.5 && PCurrent.y < 67.5)
                        str = Settings.CompassDirections["ne"];
                    else if (PCurrent.y > 67.5 && PCurrent.y < 112.5)
                        str = Settings.CompassDirections["e"];
                    else if (PCurrent.y > 112.5 && PCurrent.y < 157.5)
                        str = Settings.CompassDirections["se"];
                    else if (PCurrent.y > 157.5 && PCurrent.y < 202.5)
                        str = Settings.CompassDirections["s"];
                    else if (PCurrent.y > 202.5 && PCurrent.y < 247.5)
                        str = Settings.CompassDirections["sw"];
                    else if (PCurrent.y > 247.5 && PCurrent.y < 292.5)
                        str = Settings.CompassDirections["w"];
                    else if (PCurrent.y > 292.5 && PCurrent.y < 337.5)
                        str = Settings.CompassDirections["nw"];
                }

                return str;
            }

            public void Refresh(StoredData storedData, Dictionary<string, Dictionary<string, IPanel>> panels)
            {
                if (!Settings.CheckPanelAvailability("Compass"))
                {
                    return;
                }

                foreach (var panel in panels)
                {
                    if (panel.Value.ContainsKey("CompassText"))
                    {
                        var direction = GetDirection(panel.Key);
                        var panelText = (IPanelText)panel.Value["CompassText"];
                        if (!direction.Equals(panelText.Content))
                        {
                            panelText.Content = direction;
                            panelText.Refresh();
                        }
                    }
                }
            }
        }

        #endregion

        #region Commands

        [ChatCommand("ipanel")]
        private void IPanelCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                var Str = "InfoPanel Available Commands:\n";
                Str += "<b><color=#ffa500ff>/ipanel</color></b> - Chat Command list \n";
                Str += "<b><color=#ffa500ff>/ipanel <hide|show></color></b>- To hide or show the panel. \n";
                Str += "<b><color=#ffa500ff>/ipanel clock game</color></b> - Change to game time. \n";
                Str += "<b><color=#ffa500ff>/ipanel clock server <offset></color></b> - Change to server time.\n Offset: Add hours to the clock. (-23 - 23) \n";
                Str += "<b><color=#ffa500ff>/ipanel timeformat</color></b> - To change time format. \n";

                PrintToChat(player, Str);

                return;
            }

            switch (args[0])
            {
                case "hide":
                    if (!storedData.GetPlayerSettings(player.UserIDString, "enable", true))
                    {
                        break;
                    }

                    ChangePlayerSettings(player, "enable", "false");
                    DestroyGUI(player);
                    break;
                case "show":
                    if (storedData.GetPlayerSettings(player.UserIDString, "enable", true))
                    {
                        break;
                    }

                    ChangePlayerSettings(player, "enable", "true");
                    RevealGUI(player);
                    break;

                case "clock":
                    if (args[1] == "server")
                    {
                        ChangePlayerPanelSettings(player, "Clock", "Type", "Server");

                        if (args.Length == 3)
                        {
                            var offset = 0;

                            if (int.TryParse(args[2], out offset) && offset > -23 && offset < 23)
                            {
                                ChangePlayerPanelSettings(player, "Clock", "Offset", offset.ToString());
                            }
                        }

                    }
                    else if (args[1] == "game")
                    {
                        ChangePlayerPanelSettings(player, "Clock", "Type", "Game");
                    }
                    break;
                case "timeformat":
                    if (args.Length == 1)
                    {
                        var Str = "Available Time Formats:\n";

                        for (var index = 0; index < Settings.TimeFormats.Count; index++)
                        {
                            Str += $"[<color=#ffa500ff>{index}</color>] - {DateTime.Now.ToString(Settings.TimeFormats[index])}\n";
                        }

                        PrintToChat(player, Str+"Usage: /ipanel timeformat <color=#ffa500ff> NUMBER </color>");
                    }
                    else if(args.Length == 2)
                    {
                        var TimeFormat = 0;
                        if (int.TryParse(args[1], out TimeFormat) && TimeFormat >= 0 && TimeFormat < Settings.TimeFormats.Count)
                        {
                            ChangePlayerPanelSettings(player, "Clock", "TimeFormat", TimeFormats[TimeFormat]);
                        }
                    }
                    break;
                default:
                    PrintToChat(player, "Wrong Command!");
                    break;
            };

        }

        [ChatCommand("iptest")]
        private void IPaCommand(BasePlayer player, string command, string[] args)
        {

        }

        [ChatCommand("iperr")]
        private void IPCommand(BasePlayer player, string command, string[] args)
        {
            /*
            foreach (string item in Err)
            {
                Puts(item);
            }*/

            /*foreach (KeyValuePair<string,Dictionary<string,IPanel>> item in PlayerDockPanels)
            {
                foreach (KeyValuePair<string, IPanel> itemm in item.Value)
                {
                    Puts(itemm.Key);
                }
            }*/
            /*
            foreach (KeyValuePair<string, int> item in ErrB.OrderBy(k => k.Key))
            {
                Puts(item.Key + " - " + item.Value);
            }*/
           /*
            foreach (KeyValuePair<string, List<string>> item in ErrA)
            {
                Puts(item.Key + " -> ");

                foreach (string itemm in item.Value)
                {
                    Puts(itemm);
                }

                Puts("--------");
            }*/

            Err.Clear();
            ErrA.Clear();
            ErrB.Clear();
        }

        #endregion

        #region StoredData

        public static StoredData storedData;

        public class StoredData
        {
            public Dictionary<string, PlayerSettings> Players;

            public StoredData()
            {
                Players = new Dictionary<string, PlayerSettings>();
            }

            public bool CheckPlayerData(BasePlayer Player)
            {
                return Players.ContainsKey(Player.UserIDString);
            }

            public T GetPlayerSettings<T>(string PlayerID, string Key, T DefaultValue)
            {
                PlayerSettings playerSettings;
                if (Players.TryGetValue(PlayerID, out playerSettings))
                    return playerSettings.GetSetting(Key, DefaultValue);
                return DefaultValue;
            }

            public T GetPlayerPanelSettings<T>(BasePlayer Player, string Panel, string Key, T DefaultValue)
            {
                PlayerSettings playerSettings;
                if (Players.TryGetValue(Player.UserIDString, out playerSettings))
                    return playerSettings.GetPanelSetting(Panel, Key, DefaultValue);
                return DefaultValue;
            }

            public T GetPlayerPanelSettings<T>(string PlayerID, string Panel, string Key, T DefaultValue)
            {
                PlayerSettings playerSettings;
                if (Players.TryGetValue(PlayerID, out playerSettings))
                    return playerSettings.GetPanelSetting(Panel, Key, DefaultValue);
                return DefaultValue;
            }

        }

        public class PlayerSettings
        {
            public string UserId;
            public Dictionary<string, string> Settings;
            public Dictionary<string, Dictionary<string, string>> PanelSettings;

            public PlayerSettings()
            {
                Settings = new Dictionary<string, string>();
                PanelSettings = new Dictionary<string, Dictionary<string, string>>();
            }

            public PlayerSettings(BasePlayer player)
            {
                UserId = player.UserIDString;
                Settings = new Dictionary<string, string>();
                PanelSettings = new Dictionary<string, Dictionary<string, string>>();
            }

            public void SetSetting(string Key, string Value)
            {
                Settings[Key] = Value;
            }

            public void SetPanelSetting(string Panel, string Key, string Value)
            {
                Dictionary<string, string> settings;
                if (!PanelSettings.TryGetValue(Panel, out settings))
                    PanelSettings.Add(Panel, settings = new Dictionary<string, string>());

                settings[Key] = Value;
            }

            public T GetPanelSetting<T>(string Panel, string Key, T DefaultValue)
            {
                Dictionary<string, string> PanelConfig;
                if (!PanelSettings.TryGetValue(Panel, out PanelConfig))
                    return DefaultValue;

                string value;
                if (!PanelConfig.TryGetValue(Key, out value))
                    return DefaultValue;

                if (value == null)
                    return DefaultValue;
                return (T)Convert.ChangeType(value, typeof(T));
            }


            public T GetSetting<T>(string Key, T DefaultValue)
            {

                string value;
                if (!Settings.TryGetValue(Key, out value))
                    return DefaultValue;

                if (value == null)
                    return DefaultValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }

        }

        public void LoadData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("InfoPanel_db");
            if (storedData == null)
            {
                storedData = new StoredData();
                SaveData();
            }
        }

        public void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("InfoPanel_db", storedData);
        }

        public void ChangePlayerSettings(BasePlayer player, string Key, string Value)
        {
            PlayerSettings playerSettings;
            if (!storedData.Players.TryGetValue(player.UserIDString, out playerSettings))
                storedData.Players[player.UserIDString] = playerSettings = new PlayerSettings(player);
            playerSettings.SetSetting(Key, Value);
        }

        public void ChangePlayerPanelSettings(BasePlayer player, string Panel, string Key, string Value)
        {
            PlayerSettings playerSettings;
            if (!storedData.Players.TryGetValue(player.UserIDString, out playerSettings))
                storedData.Players[player.UserIDString] = playerSettings = new PlayerSettings(player);
            playerSettings.SetPanelSetting(Panel, Key, Value);
        }


        #endregion

        public List<string> Err = new List<string>();
        public Dictionary<string, List<string>> ErrA = new Dictionary<string,List<string>>();
        public Dictionary<string, int> ErrB = new Dictionary<string, int>();
        public Dictionary<string,int> ErrD = new Dictionary<string,int>();

        #region IPanelClass
        [JsonObject(MemberSerialization.OptIn)]
        public class IPanel
        {
            #region Class Variables

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("parent")]
            public string ParentName { get; set; } = "Overlay";

            [JsonProperty("components")]
            public List<ICuiComponent> Components = new List<ICuiComponent>();

            [JsonProperty("fadeOut")]
            public float FadeOut { get; set; }

            //Left-Right
            public Vector2 HorizontalPosition { get; set; } = new Vector2(0f, 1f);

            //Bottom-Top
            public Vector2 VerticalPosition { get; set; } = new Vector2(0f, 1f);

            public string AnchorX { get; set; } = "Left";

            public string AnchorY { get; set; } = "Bottom";

            public Vector4 Padding = Vector4.zero;
            public Vector4 Margin { get; set; } = Vector4.zero;

            public float Width { get; set; } = 1f;

            public float Height { get; set; } = 1f;

            public Color _BGColor = Color.black;
            public Color BackgroundColor
            {
                get
                {
                    return _BGColor;
                }
                set
                {
                    _BGColor = value;

                    if (ImageComponent == null)
                    {
                        ImageComponent = new CuiImageComponent();
                        Components.Insert(0, ImageComponent);
                    }

                    ImageComponent.Color = $"{value.r} {value.g} {value.b} {value.a}";
                }
            }

            public int Order = 0;

            public float _VerticalOffset = 0f;
            public float VerticalOffset
            {
                get
                {
                    return _VerticalOffset;
                }

                set
                {
                    _VerticalOffset = value;
                    SetVerticalPosition();
                }
            }

            //public Dictionary<string, IPanel> Childs = new Dictionary<string, IPanel>();
            public List<string> Childs = new List<string>();

            //Components
            public CuiRectTransformComponent RecTransform;
            public CuiImageComponent ImageComponent;

            //public bool ChildsChanged = false;

            BasePlayer Owner = null;

            public string DockName = null;

            public bool IsActive = false;
            public bool IsHidden = false;

            public bool IsPanel = false;
            public bool IsDock = false;

            public bool Autoload = true;
            private Dictionary<string, Dictionary<string, IPanel>> playerPanels;
            private Dictionary<string, Dictionary<string, IPanel>> playerDockPanels;
            #endregion

            public IPanel(string name, BasePlayer Player, Dictionary<string, Dictionary<string, IPanel>> playerPanels, Dictionary<string, Dictionary<string, IPanel>> playerDockPanels)
            {

                Name = name;
                Owner = Player;
                this.playerPanels = playerPanels;
                this.playerDockPanels = playerDockPanels;

                //LoadedPanels.Add(this._Name, this);

                Dictionary<string, IPanel> playerPanel;
                if (!playerPanels.TryGetValue(Player.UserIDString, out playerPanel))
                    playerPanels.Add(Player.UserIDString, playerPanel = new Dictionary<string, IPanel>());
                playerPanel.Add(name, this);

                RecTransform = new CuiRectTransformComponent();
                Components.Add(RecTransform);
            }

            public void SetAnchorXY(string Horizontal, string Vertical)
            {
                AnchorX = Horizontal;
                AnchorY = Vertical;
            }

            #region Positioning

            //x,y,z,w
            public void SetHorizontalPosition()
            {
                float Left;
                float Right;
                var Offset = GetOffset();

                if (AnchorX == "Right")
                {
                    Right = 1f - Margin.w;
                    Left = Right - Width;

                    HorizontalPosition = new Vector2(Left, Right) - new Vector2(Offset , Offset);
                }
                else
                {
                    Left = 0f + Margin.y;
                    Right = Left + Width;

                    HorizontalPosition = new Vector2(Left, Right) + new Vector2(Offset, Offset);
                }

                RecTransform.AnchorMin = $"{HorizontalPosition.x} {VerticalPosition.x}";
                RecTransform.AnchorMax = $"{HorizontalPosition.y} {VerticalPosition.y}";
            }

            public void SetVerticalPosition()
            {
                float Top;
                float Bottom;

                if (AnchorY == "Top")
                {
                    Top = 1f - Margin.x;
                    Bottom = Top - Height;
                    VerticalPosition = new Vector2(Bottom, Top) + new Vector2(_VerticalOffset, _VerticalOffset);
                }
                else
                {
                    Bottom = 0f + Margin.z;
                    Top = Bottom + Height;

                    VerticalPosition = new Vector2(Bottom, Top) + new Vector2(_VerticalOffset, _VerticalOffset);
                }

                RecTransform.AnchorMin = $"{HorizontalPosition.x} {VerticalPosition.x}";
                RecTransform.AnchorMax = $"{HorizontalPosition.y} {VerticalPosition.y}";
            }

            float FullWidth()
            {
                return Width + Margin.y + Margin.w;
            }

            float GetSiblingsFullWidth()
            {
                return 1f;
            }
            #endregion

            #region Json
            public string ToJson()
            {
                SetHorizontalPosition();
                SetVerticalPosition();

                return JsonConvert.SerializeObject(
                    this,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }
                );
            }


            public float GetOffset()
            {
                var Offset = 0f;

                var Parent = GetPanel(ParentName);

                if (Parent == null)
                    return Offset;

                var Siblings = Parent.GetChilds().Where(c => c.Value.AnchorX == AnchorX && c.Value.Order <= Order && c.Value.IsActive && c.Value.Name != Name).Select(c => c.Value).OrderBy(s => s.Order);

                foreach (var Sibling in Siblings)
                    Offset += Sibling.Width + Sibling.Margin.y + Sibling.Margin.w;

                return Offset;
            }

            public string GetJson(bool Brackets = true)
            {
                var Panel = ToJson();
                return Brackets ? $"[{Panel}]" : Panel;
            }

            #endregion

            #region Childs

            public int GetLastChild()
            {
                if (Childs.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return GetChilds().Max(p => p.Value.Order);
                }
            }

            public IPanelText AddText(string Name)
            {
                var Text = new IPanelText(Name, Owner, playerPanels, playerDockPanels) {ParentName = this.Name};
                Childs.Add(Name);
                return Text;
            }

            public IPanelRawImage AddImage(string Name)
            {
                var Image = new IPanelRawImage(Name, Owner, playerPanels, playerDockPanels) {ParentName = this.Name};
                Childs.Add(Name);
                return Image;
            }

            public IPanel AddPanel(string Name)
            {
                var Panel = new IPanel(Name, Owner, playerPanels, playerDockPanels) {ParentName = this.Name};
                Childs.Add(Name);
                return Panel;
            }

            #endregion

            #region Selectors

            List<string> GetActiveAfterThis()
            {
                var Panels = playerPanels[Owner.UserIDString]
                    .Where(p => p.Value.IsActive && p.Value.Order > Order && p.Value.ParentName == ParentName && p.Value.AnchorX == AnchorX)
                    .OrderBy(s => s.Value.Order)
                    .Select(k => k.Key)
                    .ToList();

                return Panels;
            }

            public Dictionary<string, IPanel> GetChilds()
            {
                return playerPanels[Owner.UserIDString].Where(x => Childs.Contains(x.Key)).ToDictionary(se => se.Key, se => se.Value);
            }

            public IPanel GetParent()
            {
                return GetPanel(ParentName);
            }

            public List<IPanel> GetSiblings()
            {
                return GetPanel(ParentName)?.GetChilds().Where(c => c.Value.AnchorX == AnchorX && c.Value.Name != Name).Select(c => c.Value).OrderBy(s => s.Order).ToList() ?? new List<IPanel>();
            }

            public IPanel GetPanel(string PName)
            {
                Dictionary<string, IPanel> panels;
                IPanel panel;
                if (playerPanels.TryGetValue(Owner.UserIDString, out panels) && panels.TryGetValue(PName, out panel))
                    return panel;
                return null;
            }

            public IPanel GetDock()
            {
                if (DockName == null) return null;
                Dictionary<string, IPanel> panels;
                IPanel panel;
                if (playerDockPanels.TryGetValue(Owner.UserIDString, out panels) && panels.TryGetValue(DockName, out panel))
                    return panel;
                return null;
            }

            #endregion

            #region GUI


            public void Hide()
            {
                foreach (var Panel in GetChilds().Where(p => p.Value.IsActive))
                    Panel.Value.Hide();

                CuiHelper.DestroyUi(Owner, Name);
                //CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(this.Owner.net.connection), null, "DestroyUI", new Facepunch.ObjectList(this._Name));
            }

            public void Reveal()
            {

                //Interface.Oxide.LogInfo(GetJson()); //TODO
                CuiHelper.AddUi(Owner, GetJson());
                //CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(this.Owner.net.connection), null, "AddUI", new Facepunch.ObjectList(GetJson()));

                IsActive = true;
                IsHidden = false;

                foreach (var Child in GetChilds().Where(p => p.Value.Autoload || p.Value.IsActive).OrderBy(s => s.Value.Order))
                    Child.Value.Reveal();
            }

            void ReDrawPanels(List<string> PanelsName)
            {
                foreach (var PanelName in PanelsName)
                    GetPanel(PanelName)?.DestroyPanel(false);

                foreach (var PanelName in PanelsName)
                    GetPanel(PanelName)?.ShowPanel();
            }

            public void ShowPanel(bool Childs = true)
            {
                if (storedData.GetPlayerSettings(Owner.UserIDString, "enable", true))
                {
                    var Dock = GetDock();
                    if (Dock != null && Dock.IsActive == false)
                        Dock.ShowPanel(false);

                    var ActivePanelsAfterThis = GetActiveAfterThis();

                    foreach (var PanelName in ActivePanelsAfterThis)
                        GetPanel(PanelName)?.DestroyPanel(false);

                    //ErrB.Add(this.Name + ErrB.Count,ActivePanelsAfterThis.Count);

                    if (storedData.GetPlayerSettings(Owner.UserIDString, "enable", true))
                    {
                        //Interface.Oxide.LogInfo(GetJson()); //TODO
                        CuiHelper.AddUi(Owner, GetJson());
                        //CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(this.Owner.net.connection), null, "AddUI", new Facepunch.ObjectList(GetJson()));
                    }

                    IsActive = true;
                    IsHidden = false;

                    if(Childs)
                    {
                        foreach (var Child in GetChilds().Where(p => p.Value.Autoload || p.Value.IsActive).OrderBy(s => s.Value.Order))
                            Child.Value.ShowPanel();
                    }

                    foreach (var PanelName in ActivePanelsAfterThis)
                        GetPanel(PanelName)?.ShowPanel();
                }
                else
                {
                    ShowPanelIfHidden();
                }
            }

            void ShowPanelIfHidden(bool Childs = true)
            {
                IsActive = true;
                IsHidden = true;
                if (Childs)
                {
                    foreach (var Child in GetChilds().Where(p => p.Value.Autoload || p.Value.IsActive).OrderBy(s => s.Value.Order))
                        Child.Value.ShowPanel();
                }
            }


            public void DestroyPanel( bool Redraw = true)
            {
                foreach (var Panel in GetChilds().Where(p => p.Value.IsActive))
                    Panel.Value.DestroyPanel(false);

                CuiHelper.DestroyUi(Owner, Name);
                //CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo(this.Owner.net.connection), null, "DestroyUI", new Facepunch.ObjectList(this._Name));

                IsActive = false;

                if (Redraw)
                    ReDrawPanels(GetActiveAfterThis());

                var Dock = GetDock();
                if(Dock?.GetChilds().Count(p => p.Value.IsActive) == 0) Dock.DestroyPanel();
            }


            public virtual void Refresh()
            {
                DestroyPanel();
                ShowPanel();
            }

            #endregion

            #region Util

            public void Remover()
            {
                foreach (var Child in GetChilds())
                    Child.Value.Remover();

                GetPanel(ParentName).Childs.Remove(Name);
                playerPanels[Owner.UserIDString].Remove(Name);
            }

            protected string ColorToString(Color color)
            {
                return $"{color.r} {color.g} {color.b} {color.a}";
            }

            #endregion
        }

        public class IPanelText : IPanel
        {
            public string Content
            {
                get
                {
                    return TextComponent.Text;
                }
                set
                {
                    TextComponent.Text = value;
                }
            }
            public TextAnchor Align
            {
                get
                {
                    return TextComponent.Align;
                }

                set
                {
                    TextComponent.Align = value;
                }
            }
            public int FontSize {
                get
                {
                    return TextComponent.FontSize;
                }
                set
                {
                    TextComponent.FontSize = value;
                }
            }
            public Color _FontColor = Color.white;
            public Color FontColor
            {
                get
                {
                    return _FontColor;
                }
                set
                {
                    _FontColor = value;
                    TextComponent.Color = $"{value.r} {value.g} {value.b} {value.a}";
                }
            }

            public CuiTextComponent TextComponent;

            public IPanelText(string Name, BasePlayer Player, Dictionary<string, Dictionary<string, IPanel>> playerPanels, Dictionary<string, Dictionary<string, IPanel>> playerDockPanels) : base(Name, Player, playerPanels, playerDockPanels)
            {
                TextComponent = new CuiTextComponent();
                Components.Insert(0, TextComponent);
            }

            public void RefreshText(BasePlayer player, string text)
            {
                DestroyPanel();
                Content = text;
                ShowPanel();
            }
        }

        public class IPanelRawImage : IPanel
        {
            public string Url
            {
                get
                {
                    return RawImageComponent.Url;
                }
                set
                {
                    RawImageComponent.Url = value;
                }
            }

            public Color _Color;
            public Color Color
            {
                get
                {
                    return _Color;
                }
                set
                {
                    _Color = value;
                    RawImageComponent.Color = ColorToString(value);
                }
            }

            public CuiRawImageComponent RawImageComponent;

            public IPanelRawImage(string Name, BasePlayer Player, Dictionary<string, Dictionary<string, IPanel>> playerPanels, Dictionary<string, Dictionary<string, IPanel>> playerDockPanels) : base(Name, Player, playerPanels, playerDockPanels)
            {
                RawImageComponent = new CuiRawImageComponent();
                Components.Insert(0, RawImageComponent);
            }
        }

        #endregion

        #region GUI

        private void DestroyGUI(BasePlayer player)
        {
            foreach (var Dock in PlayerDockPanels[player.UserIDString])
            {
                Dock.Value.DestroyPanel(false);
            }
        }

        void GUITimerInit(BasePlayer player)
        {
            if (player == null) return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(2, () => GUITimerInit(player));
            }
            else if (!PlayerDockPanels.ContainsKey(player.UserIDString))
            {
                LoadPanels(player);
                InitializeGUI(player);

                RefreshOnlinePlayers();
            }
        }

        private void InitializeGUI(BasePlayer player)
        {
            if (!storedData.GetPlayerSettings(player.UserIDString, "enable", true))
                return;

            foreach (var Panel in PlayerPanels[player.UserIDString])
            {
                switch (Panel.Key)
                {
                    case "ClockText":
                        ((IPanelText) Panel.Value).Content = Clock.ShowTime(player.UserIDString, storedData);
                        break;
                    case "OPlayersText":
                        ((IPanelText) Panel.Value).Content = BasePlayer.activePlayerList.Count + "/" + ConVar.Server.maxplayers;
                        break;
                    case "SleepersText":
                        ((IPanelText) Panel.Value).Content = BasePlayer.sleepingPlayerList.Count.ToString();
                        break;
                    case "MessageBoxText":
                        ((IPanelText) Panel.Value).Content = MessageBox.GetMessage();
                        break;
                    case "CoordinatesText":
                        ((IPanelText) Panel.Value).Content = Coord.GetCoord(player.UserIDString);
                        break;
                    case "BalanceText":
                        ((IPanelText) Panel.Value).Content = $"{Bala.GetBalance(player.UserIDString):N}";
                        break;
                    case "RadiationImage":
                        ((IPanelRawImage) Panel.Value).Color = Rad.ImageColor;
                        break;
                    case "AirdropEventImage":
                        ((IPanelRawImage) Panel.Value).Color = Airplane.ImageColor;
                        break;
                    case "HelicopterEventImage":
                        ((IPanelRawImage) Panel.Value).Color = Helicopter.ImageColor;
                        break;
                    case "CompassText":
                        ((IPanelText) Panel.Value).Content = CompassObj.GetDirection(player.UserIDString);
                        break;
                }
            }

            foreach (var Dock in PlayerDockPanels[player.UserIDString])
            {
                if (Dock.Value.Childs.Count != 0)
                    Dock.Value.ShowPanel();
            }

        }

        private void RevealGUI(BasePlayer player)
        {
            foreach (var Dock in PlayerDockPanels[player.UserIDString])
            {
                if (Dock.Value.Childs.Count != 0)
                    Dock.Value.ShowPanel();
            }
        }

        private void RefreshOnlinePlayers()
        {
            foreach (var panel in PlayerPanels)
            {
                if (Settings.GetPanelSettingsValue("OPlayers", "Available", true) && panel.Value.ContainsKey("OPlayersText"))
                {
                    var panelText = (IPanelText)panel.Value["OPlayersText"];
                    panelText.Content = $"{BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}";
                    panelText.Refresh();
                }
            }
        }

        private void RefreshSleepers()
        {
            foreach (var panel in PlayerPanels)
            {
                if (Settings.GetPanelSettingsValue("Sleepers", "Available", true) && panel.Value.ContainsKey("SleepersText"))
                {
                    var panelText = (IPanelText)panel.Value["SleepersText"];
                    panelText.Content = BasePlayer.sleepingPlayerList.Count.ToString();
                    panelText.Refresh();
                }
            }
        }

        #endregion

        #region API

        private bool PanelRegister(string PluginName,string PanelName, string json)
        {
            List<string> loadedPlugin;
            if (LoadedPluginPanels.TryGetValue(PluginName, out loadedPlugin) && loadedPlugin.Contains(PanelName))
                return true;

            var Cfg = JsonConvert.DeserializeObject<PanelConfig>(json);

            Dictionary<string, PanelConfig> thirdPartyPanel;
            if (!Settings.ThirdPartyPanels.TryGetValue(PluginName, out thirdPartyPanel))
                Settings.ThirdPartyPanels.Add(PluginName, thirdPartyPanel = new Dictionary<string, PanelConfig>());

            if (!thirdPartyPanel.ContainsKey(PanelName))
            {
                Cfg.Order = PanelReOrder(Cfg.Dock, Cfg.AnchorX);
                thirdPartyPanel.Add(PanelName, Cfg);

                Config.WriteObject(Settings, true);
                PrintWarning($"New panel added to the config file: {PanelName}");
            }

            foreach (var Docks in PlayerDockPanels)
            {
                if (Docks.Value.ContainsKey(Cfg.Dock))
                    LoadPanel(Docks.Value[Cfg.Dock], PanelName, Cfg);
            }

            if (!LoadedPluginPanels.TryGetValue(PluginName, out loadedPlugin))
                LoadedPluginPanels.Add(PluginName, loadedPlugin = new List<string>());
            loadedPlugin.Add(PanelName);

            return true;
        }

        private bool ShowPanel(string PluginName,string PanelName, string PlayerId = null)
        {
            if (!Settings.ThirdPartyPanels[PluginName][PanelName].Available)
                return false;

            if (PlayerId != null && PlayerPanels.ContainsKey(PlayerId))
            {
                PlayerPanels[PlayerId][PanelName].ShowPanel();
                return true;
            }

            foreach (var PlayerID in PlayerPanels.Keys)
                PlayerPanels[PlayerID][PanelName].ShowPanel();

            return true;
        }

        private bool HidePanel(string PluginName,string PanelName, string PlayerId = null)
        {
            if (!Settings.ThirdPartyPanels[PluginName][PanelName].Available)
                return false;

            if (PlayerId != null && PlayerPanels.ContainsKey(PlayerId))
            {
                PlayerPanels[PlayerId][PanelName].DestroyPanel();
                return true;
            }

            foreach (var PlayerID in PlayerPanels.Keys)
                PlayerPanels[PlayerID][PanelName].DestroyPanel();

            return true;
        }

        private bool RefreshPanel(string PluginName,string PanelName, string PlayerId = null)
        {
            if (!Settings.ThirdPartyPanels[PluginName][PanelName].Available)
                return false;

            if (PlayerId != null && PlayerPanels.ContainsKey(PlayerId))
            {
                PlayerPanels[PlayerId][PanelName].DestroyPanel();
                PlayerPanels[PlayerId][PanelName].ShowPanel();
                return true;
            }

            foreach (var PlayerID in PlayerPanels.Keys)
            {
                PlayerPanels[PlayerID][PanelName].DestroyPanel();
                PlayerPanels[PlayerID][PanelName].ShowPanel();
            }

            return true;
        }

        private void SetPanelAttribute(string PluginName,string PanelName, string Attribute, string Value, string PlayerId = null )
        {
            if (PlayerId != null && PlayerPanels.ContainsKey(PlayerId))
            {
                var Panel = PlayerPanels[PlayerId][PanelName];
                var PropInfo = Panel.GetType().GetProperty(Attribute);

                if (PropInfo == null)
                {
                    PrintWarning("Wrong Attribute name: " + Attribute);
                    return;
                }

                if (Attribute == "FontColor" || Attribute == "BackgroundColor")
                {
                    PropInfo.SetValue(Panel, ColorEx.Parse(Value), null);
                }
                else if (Attribute == "Margin")
                {
                    PropInfo.SetValue(Panel, Vector4Parser(Value), null);
                }
                else
                {
                    var ConvertedValue = Convert.ChangeType(Value, PropInfo.PropertyType);

                    PropInfo.SetValue(Panel, ConvertedValue, null);
                }

                return;
            }

            foreach (var playerID in PlayerPanels.Keys)
            {
                var Panel = PlayerPanels[playerID][PanelName];
                var PropInfo = Panel.GetType().GetProperty(Attribute);

                if (PropInfo == null)
                {
                    PrintWarning("Wrong Attribute name: " + Attribute);
                    return;
                }

                if (Attribute == "FontColor" || Attribute == "BackgroundColor")
                {
                    PropInfo.SetValue(Panel, ColorEx.Parse(Value), null);
                }
                else if (Attribute == "Margin")
                {
                    PropInfo.SetValue(Panel, Vector4Parser(Value), null);
                }
                else
                {
                    var ConvertedValue = Convert.ChangeType(Value, PropInfo.PropertyType);

                    PropInfo.SetValue(Panel, ConvertedValue, null);
                }
            }
        }

        private bool SendPanelInfo(string PluginName, List<string> Panels)
        {
            Dictionary<string, PanelConfig> panelConfig;
            if(!Settings.ThirdPartyPanels.TryGetValue(PluginName, out panelConfig))
            {
                return false;
            }

            var Removable =panelConfig.Keys.Except(Panels).ToList();

            foreach(var item in Removable)
            {
                panelConfig.Remove(item);
            }

            if(Removable.Count > 0)
            {
                Config.WriteObject(Settings, true);
                PrintWarning($"Config File refreshed! {Removable.Count} panel removed!");
            }

            return true;
        }

        private bool IsPlayerGUILoaded(string PlayerId)
        {
            return PlayerPanels.ContainsKey(PlayerId);
        }

        #endregion

        #region Utility
        internal static Vector4 Vector4Parser(string p)
        {
            var strArrays = p.Split(' ');
            if (strArrays.Length != 4)
                return Vector4.zero;
            return new Vector4(float.Parse(strArrays[0]), float.Parse(strArrays[1]), float.Parse(strArrays[2]), float.Parse(strArrays[3]));
        }
        #endregion
    }
}
