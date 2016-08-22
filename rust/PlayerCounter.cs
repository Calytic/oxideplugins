using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("PlayerCounter", "Bamabo", "1.5.1")]
    [Description("Adds a discrete player counter to the HUD")]

    class PlayerCounter : RustPlugin
    {
        public class PlayerPreferences
        {
            public string position { get; set; }
            public bool toggle { get; set; } = true;
            public CuiElementContainer elements { get; set; }
            public string container { get; set; }
            public CuiLabel playerCounter { get; set; }

            public PlayerPreferences() { }
            public PlayerPreferences(string position, bool toggle, int fontSize, string color)
            {
                elements = new CuiElementContainer();
                playerCounter = new CuiLabel
                {
                    Text =
                {
                    Text = "",
                    FontSize = fontSize,
                    Color = color,
                    Align = TextAnchor.UpperRight
                },
                    RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
                };
                container = elements.Add(new CuiPanel
                {
                    Image =
                        {
                            Color = "0 0 0 0"
                        },
                    RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                });
                elements.Add(playerCounter, container);
                this.position = position;
                this.toggle = toggle;
            }
        }

        private Dictionary<ulong, PlayerPreferences> preferences;

        private int fontSize { get; set; }
        private string iconID { get; set; }
        private string color { get; set; }
        private bool usePerms { get; set; }
        private string defaultPos { get; set; }
        private bool showSleepers { get; set; }

        void Init()
        {
            RegisterMessages();
            permission.RegisterPermission("playercounter.toggle", this);
            permission.RegisterPermission("playercounter.display", this);
            preferences = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerPreferences>>("PlayerCounter");

            iconID = GetConfigEntry<string>("serverIconID", "00000000000000000");
            fontSize = GetConfigEntry<int>("fontsize", 18);
            color = GetConfigEntry<string>("color", "1 1 1 0.5");
            defaultPos = GetConfigEntry<string>("defaultPosition", "left");
            showSleepers = GetConfigEntry<bool>("showSleepers", false);
            usePerms = GetConfigEntry<bool>("usePermissions", false);

            foreach (var player in BasePlayer.activePlayerList)
            {
                if(!preferences.ContainsKey(player.userID))
                    preferences.Add(player.userID, new PlayerPreferences(defaultPos, true, fontSize, color));

                if (preferences[player.userID].toggle && (permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms || player.IsAdmin()) || !usePerms)
                    CuiHelper.AddUi(player, preferences[player.userID].elements);
            }
        }
        void Loaded()
        {
            UpdateCounter();
        }
        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PlayerCounter", preferences);

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, preferences[player.userID].container);
        }
    
        void OnPlayerSleepEnded(BasePlayer player)
        {
            NextFrame(() =>
            {
                UpdateCounter();
            });
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (!preferences.ContainsKey(player.userID) && ((permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms) || player.IsAdmin() || !usePerms))
            {
                preferences.Add(player.userID, new PlayerPreferences(defaultPos, true, fontSize, color));
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            NextFrame(() =>
            {
                UpdateCounter();
            });
        }

        [ChatCommand("playercounter")]
        void cmdPlayerCounter(BasePlayer sender, string command, String[] args)
        {
            if ((permission.UserHasPermission(sender.UserIDString, "playercounter.toggle") && usePerms) || sender.IsAdmin() || !usePerms)
            {
                if (preferences.ContainsKey(sender.userID))
                {
                    if (args.Length == 0)
                    {
                        if (preferences[sender.userID].toggle)
                        {
                            preferences[sender.userID].toggle = false;
                            CuiHelper.DestroyUi(sender, preferences[sender.userID].container);
                            rust.SendChatMessage(sender, String.Format(lang.GetMessage("toggledOff", this, sender.UserIDString)), null, iconID);
                        }
                        else
                        {
                            preferences[sender.userID].toggle = true;
                            if ((preferences.ContainsKey(sender.userID) && preferences[sender.userID].toggle))
                                CuiHelper.AddUi(sender, preferences[sender.userID].elements);
                            rust.SendChatMessage(sender, String.Format(lang.GetMessage("toggledOn", this, sender.UserIDString)), null, iconID);
                        }
                    }
                    else if (args.Length == 1)
                    {
                        switch (args[0].ToLower())
                        {
                            case "left":
                                preferences[sender.userID].position = "left";
                                UpdateCounterForPlayer(sender);
                                break;
                            case "middle":
                                preferences[sender.userID].position = "middle";
                                UpdateCounterForPlayer(sender);
                                break;
                            case "right":
                                preferences[sender.userID].position = "right";
                                UpdateCounterForPlayer(sender);
                                break;
                            default:
                                rust.SendChatMessage(sender, String.Format(lang.GetMessage("noPosition", this, sender.UserIDString)), null, iconID);
                                break;
                        }
                    }
                    else
                        rust.SendChatMessage(sender, String.Format(lang.GetMessage("wrongNumberOfArguments", this, sender.UserIDString)), null, iconID);
                }
            }
            else
            {
                rust.SendChatMessage(sender, String.Format(lang.GetMessage("accessDenied", this, sender.UserIDString)), null, iconID);
            }
        }

        void UpdateCounterForPlayer(BasePlayer player)
        {
            if (preferences.ContainsKey(player.userID))
                CuiHelper.DestroyUi(player, preferences[player.userID].container);

            if ((!preferences.ContainsKey(player.userID)) && (((permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms) || player.IsAdmin()) || !usePerms))
                preferences.Add(player.userID, new PlayerPreferences("right", true, fontSize, color));

            preferences[player.userID].elements = new CuiElementContainer();

            if (preferences[player.userID].position == "left")
            {
                AlignLeft(player.userID);
            }
            else if (preferences[player.userID].position == "middle")
            {
                AlignMiddle(player.userID);
            }
            else
            {
                AlignRight(player.userID);
            }
            if (preferences[player.userID].toggle && (((permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms) || player.IsAdmin()) || !usePerms))
                CuiHelper.AddUi(player, preferences[player.userID].elements);
        }

        void UpdateCounter()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if(preferences.ContainsKey(player.userID))
                    CuiHelper.DestroyUi(player, preferences[player.userID].container);

                if ((!preferences.ContainsKey(player.userID)) && (((permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms) || player.IsAdmin()) || !usePerms))
                    preferences.Add(player.userID, new PlayerPreferences(defaultPos, true, fontSize, color));
                preferences[player.userID].elements = new CuiElementContainer(); 

                if (preferences[player.userID].position == "left")
                {
                    AlignLeft(player.userID);
                }
                else if (preferences[player.userID].position == "middle")
                {
                    AlignMiddle(player.userID);
                }
                else
                {
                    AlignRight(player.userID);
                }
                if (preferences[player.userID].toggle && (((permission.UserHasPermission(player.UserIDString, "playercounter.display") && usePerms) || player.IsAdmin()) || !usePerms))
                    CuiHelper.AddUi(player, preferences[player.userID].elements);
            }
        }

        void AlignLeft(ulong userID)
        {
            preferences[userID].container = preferences[userID].elements.Add(new CuiPanel
            {
                Image =
                        {
                            Color = "0 0 0 0"
                        },
                RectTransform =
                        {
                            AnchorMin = "0.003 0.75",
                            AnchorMax = "1 0.998"
                        }
            });


            preferences[userID].playerCounter = new CuiLabel
            {
                Text =
                {
                    Text = GetUpdatedCounterText(userID),
                    FontSize = fontSize,
                    Color = color,
                    Align = TextAnchor.UpperLeft
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            };
            preferences[userID].elements.Add(preferences[userID].playerCounter, preferences[userID].container);
        }
        void AlignMiddle(ulong userID)
        {
            preferences[userID].container = preferences[userID].elements.Add(new CuiPanel
            {
                Image =
                        {
                            Color = "0 0 0 0"
                        },
                RectTransform =
                        {
                            AnchorMin = "0 0.75",
                            AnchorMax = "1 0.998"
                        }
            });


            preferences[userID].playerCounter = new CuiLabel
            {
                Text =
                {
                    Text = GetUpdatedCounterText(userID),
                    FontSize = fontSize,
                    Color = color,
                    Align = TextAnchor.UpperCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            };
            preferences[userID].elements.Add(preferences[userID].playerCounter, preferences[userID].container);
        }
        void AlignRight(ulong userID)
        {
            preferences[userID].container = preferences[userID].elements.Add(new CuiPanel
            {
                Image =
                        {
                            Color = "0 0 0 0"
                        },
                RectTransform =
                        {
                            AnchorMin = "0 0.75",
                            AnchorMax = "0.996 0.998"
                        }
            });


            preferences[userID].playerCounter = new CuiLabel
            {
                Text =
                {
                    Text = GetUpdatedCounterText(userID),
                    FontSize = fontSize,
                    Color = color,
                    Align = TextAnchor.UpperRight
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            };
            preferences[userID].elements.Add(preferences[userID].playerCounter, preferences[userID].container);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file for PlayerCounter");
            Config.Clear();
            Config["color"] = "1 1 1 0.5";
            Config["defaultPosition"] = "left";
            Config["fontsize"] = 18;
            Config["serverIconID"] = "00000000000000000";
            Config["showSleepers"] = false;
            Config["usePermissions"] = false;
            SaveConfig();
        }
        
        T GetConfigEntry<T>(string configEntry, T defaultValue)
        {
            if (Config[configEntry] == null)
            {
                Config[configEntry] = defaultValue;
                SaveConfig();
            }
            return (T)Config[configEntry];
        }

        string GetUpdatedCounterText(ulong userID)
        {
            if (showSleepers)
                return String.Format(lang.GetMessage("counterWithSleepers", this, userID.ToString()), BasePlayer.activePlayerList.Count.ToString(), ConVar.Server.maxplayers.ToString(), BasePlayer.sleepingPlayerList.Count.ToString());
            else
                return String.Format(lang.GetMessage("counter", this, userID.ToString()), BasePlayer.activePlayerList.Count.ToString(), ConVar.Server.maxplayers);
        }

        void RegisterMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["accessDenied"] = "<color=red>You do not have access to that command.</color>",
                ["noPosition"] = "<color=red>There's no position by that name</color>",
                ["wrongNumberOfArguments"] = "<color=red>No PlayerCounter command takes that amount of arguments.</color>",
                ["toggledOn"] = "Toggled PlayerCounter on",
                ["toggledOff"] = "Toggled PlayerCounter off",
                ["counter"] = "{0}/{1}",
                ["counterWithSleepers"] = "{0}({2})/{1}"
            }, this);
        }
    }
} 