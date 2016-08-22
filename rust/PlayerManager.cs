using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using System.Linq;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("PlayerManager", "Reneb", "1.0.9", ResourceId= 1535)]
    class PlayerManager : RustPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        [PluginReference]
        Plugin EnhancedBanSystem;

        [PluginReference]
        Plugin chatmute;

        static string permissionPM = "playermanager.gui";
        static string permissionKICK = "playermanager.kick";
        static string permissionBAN = "playermanager.ban";
        static string permissionTP = "playermanager.tp";
        static string permissionIPs = "playermanager.ips";
        static List<object> ExternalCommandsList = DefaultExternalCommands();
        void Loaded()
        {
            permission.RegisterPermission(permissionPM, this);
            permission.RegisterPermission(permissionKICK, this);
            permission.RegisterPermission(permissionBAN, this);
            permission.RegisterPermission(permissionTP, this);
            permission.RegisterPermission(permissionIPs, this);
            foreach (object ecmdsRAW in ExternalCommandsList)
            {
                var ecmds = ecmdsRAW as Dictionary<string,object>;
                permission.RegisterPermission(ecmds["permission"].ToString(), this);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyGUI(player, "PlayerManagerOverlay");
                DestroyGUI(player, "DialogOverlay");
            }
        }
         
        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        void Init() 
        {
            CheckCfg<string>("Permission - GUI", ref permissionPM);
            CheckCfg<string>("Permission - GUI - Kick", ref permissionKICK);
            CheckCfg<string>("Permission - GUI - Ban", ref permissionBAN);
            CheckCfg<string>("Permission - GUI - TP", ref permissionTP);
            CheckCfg<string>("Permission - GUI - IPs", ref permissionIPs);
            CheckCfg<List<object>>("External Commands", ref ExternalCommandsList);
            CheckCfg<Dictionary<string, object>>("Options", ref dialogActions);
            var messages = new Dictionary<string, string>
            {
                {"PlayerKicked","<color=orange>SERVER:</color> {0} was kicked from the server ({1})" },
                { "YouKicked","You were kicked from the server ({0})" },
                { "PlayerBanned","<color=orange>SERVER:</color> {0} - {1} was banned from the server ({2})" },
                { "YouBanned","You were banned from the server ({0})" }
            };
            lang.RegisterMessages(messages, this);

            SaveConfig();
        }

         
        static List<object> DefaultExternalCommands()
        {
            return new List<object>
            {
                new Dictionary<string,object> {
                    { "permission" , "playermanager.canmute" },
                    { "commands" , new List<object> {
                            new Dictionary<string,object> {
                                { "color", "1 0 0 0.4" },
                                { "text", "mute" },
                                { "cmd" , "player.mute {steamid}" }
                            },
                            new Dictionary<string,object> {
                                { "color", "0 1 0 0.4" },
                                { "text", "unmute" },
                                { "cmd" , "player.unmute {steamid}" }
                            }
                        } },
                    { "name" , "Mute" }
                },
                 new Dictionary<string,object> {
                    { "permission" , "playermanager.canjail" },
                    { "commands" , new List<object> {
                            new Dictionary<string,object> {
                                { "color", "1 0 0 0.4" },
                                { "text", "Jail" },
                                { "cmd" , "player.jail {steamid}" }
                            },
                            new Dictionary<string,object> {
                                { "color", "0 1 0 0.4" },
                                { "text", "Free" },
                                { "cmd" , "player.free {steamid}" }
                            }
                        } },
                    { "name" , "Jail" }
                }
            };
        }

        Hash<ulong, PlayerMGUI> playerGUI = new Hash<ulong, PlayerMGUI>();

        class PlayerMGUI
        {
            public string section;
            public string subsection;
            public int page;
            public string select;
            public string search;
        }

        class DialogOptions
        {
            public Dictionary<string,string> option = new Dictionary<string, string>();
            public string action;
        }

        class ExternalCommands
        {
            public string permission;
            public List<ExternalCommand> commands;
            public string name;
        }
        class ExternalCommand
        {
            public string color;
            public string text;
            public string cmd;
        }

        void RefreshFullUI(BasePlayer player)
        {
            DestroyGUI(player, "PlayerManagerOverlay");
            AddUI(player, parentoverlay);
            playerGUI[player.userID].section = "players";
            playerGUI[player.userID].subsection = playerGUI[player.userID].search != string.Empty ? "search" : "online";
            playerGUI[player.userID].page = 0;
            playerGUI[player.userID].select = string.Empty;

            string TextPlayer = GenerateText("PlayerManagerOverlay", "Players", "20", "0.05", "0.15", "0.94", "0.99");
            string ButtonPlayer = GenerateButton("PlayerManagerOverlay", "playermanager.show players", "0.1 0.1 0.1 0.5", "0.05", "0.15", "0.94", "0.99");
            AddUI(player, TextPlayer);
            AddUI(player, ButtonPlayer);
            RefreshGUI(player, "section");
        }
        void RefreshGUI(BasePlayer player, string ttype)
        {
            switch(ttype)
            {
                case "section":
                    RefreshSection(player);
                    break;
            }
        }

        void RefreshSection(BasePlayer player)
        {
            if(playerGUI[player.userID] == null) { DestroyGUI(player,"PlayerManagerOverlay"); return; }
            switch(playerGUI[player.userID].section)
            {
                case "players":
                    DestroyGUI(player, "PlayerListSection");
                    CreateSectionPlayers(player);
                    RefreshSubSection(player);
                    break;
            }
        }
        void RefreshSubSection(BasePlayer player)
        {
            if (playerGUI[player.userID] == null) { DestroyGUI(player, "PlayerManagerOverlay"); return; }
            switch (playerGUI[player.userID].section)
            {
                case "players":
                    UpdateSubSectionPlayers(player);
                    break;
            }
        }

        void AddUI(BasePlayer player, string json)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json, null, null, null, null));
        }
        void DestroyGUI(BasePlayer player, string GUIName) { Game.Rust.Cui.CuiHelper.DestroyUi(player, GUIName); }

        void CreateSectionPlayers(BasePlayer player)
        {
            AddUI(player, playerlistsectionoverlay);
        }
        string playerlistsectionoverlay = @"[
			{
				""name"": ""PlayerListSection"",
				""parent"": ""PlayerManagerOverlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.8"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.01 0.06"",
						""anchormax"": ""0.4 0.90""
					},
					{
						""type"":""NeedsCursor"",
					}
				]
			},
            {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""All"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.95"",
                                    ""anchormax"": ""0.20 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.subsection all"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.95"",
                                    ""anchormax"": ""0.20 1""
                                }
                            ]
                        }, 
                    {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Online"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.20 0.95"",
                                    ""anchormax"": ""0.40 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.subsection online"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.20 0.95"",
                                    ""anchormax"": ""0.40 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Sleepers"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.40 0.95"",
                                    ""anchormax"": ""0.60 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.subsection sleepers"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.40 0.95"",
                                    ""anchormax"": ""0.60 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Search"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.60 0.95"",
                                    ""anchormax"": ""0.80 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.subsection search"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.60 0.95"",
                                    ""anchormax"": ""0.80 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Banned"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.80 0.95"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.subsection banned"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.8 0.95"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<<"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""0.5 0.05""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.page previous"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""0.5 0.05""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":"">>"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.5 0"",
                                    ""anchormax"": ""1 0.05""
                                }
                            ]
                        },
                        {
                            ""parent"": ""PlayerListSection"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Button"",
                                    ""color"": ""0.5 0.5 0.5 0.2"",
                                    ""command"":""playermanager.page next"",
                                    ""imagetype"": ""Tiled""
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.5 0"",
                                    ""anchormax"": ""1 0.05""
                                }
                            ]
                        }
		]
		";
        string selectoverlay = @"[
			{
            ""name"": ""PlayerManagerSelectOverlay"",
				""parent"": ""PlayerManagerOverlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.8"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.41 0.11"",
						""anchormax"": ""0.99 0.89""
					},
					{
						""type"":""NeedsCursor"",
					}
				]
			}
		]
		";

        string playerlistsubsectionoverlay = @"[
			{
				""name"": ""PlayerListSubSection"",
				""parent"": ""PlayerListSection"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.8"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0.06"",
						""anchormax"": ""1 0.94""
					},
					{
						""type"":""NeedsCursor"",
					}
				]
			}
		]
		";
        void UpdateSelect(BasePlayer player)
        {
            DestroyGUI(player, "PlayerManagerSelectOverlay");
            AddUI(player, selectoverlay);
            switch (playerGUI[player.userID].section)
            {
                case "players":
                    if (playerGUI[player.userID].select == string.Empty) return;
                    string steamid = GenerateText("PlayerManagerSelectOverlay", playerGUI[player.userID].select, "20", "0", "0.40", "0.90", "0.95");
                    AddUI(player, steamid);

                    var playerdata = (PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "default") as Dictionary<string, object>);
                    BasePlayer targetplayer = FindBasePlayerPlayer(playerGUI[player.userID].select);

                    string name = "Unknown Player";
                    if (playerdata != null)
                    {
                        if (playerdata["name"] != null)
                        {
                            name = playerdata["name"] as string;
                            string nametext = GenerateText("PlayerManagerSelectOverlay", name, "20", "0.40", "0.80", "0.90", "0.95");
                            AddUI(player, nametext);
                        }
                    }
                    else
                    {
                        string text = GenerateText("PlayerManagerSelectOverlay", targetplayer != null ? targetplayer.displayName : "Unknown Player Data", "20", "0.40", "0.80", "0.90", "0.95");
                        AddUI(player, text);
                    }
                    if (player.net.connection.authLevel > 1 || permission.UserHasPermission(player.userID.ToString(), permissionKICK))
                    {
                        string kicktext = GenerateText("PlayerManagerSelectOverlay", "KICK", "20", "0.40", "0.60", "0.85", "0.90");
                        string kickbutton = GenerateButton("PlayerManagerSelectOverlay", "playermanager.dialog kick", "1 0.4 0 0.8", "0.40", "0.60", "0.85", "0.90");
                        AddUI(player, kicktext);
                        AddUI(player, kickbutton);
                    }
                    if (player.net.connection.authLevel > 1 || permission.UserHasPermission(player.userID.ToString(), permissionBAN))
                    {
                        string bantext = GenerateText("PlayerManagerSelectOverlay", "BAN", "20", "0.60", "0.80", "0.85", "0.90");
                        string banbutton = GenerateButton("PlayerManagerSelectOverlay", "playermanager.dialog ban", "1 0 0 0.8", "0.60", "0.80", "0.85", "0.90");
                        AddUI(player, bantext);
                        AddUI(player, banbutton);
                    }

                    string tptext = string.Empty;
                    string tpbutton = string.Empty;
                    if (player.net.connection.authLevel > 1 || permission.UserHasPermission(player.userID.ToString(), permissionTP))
                    {
                        if (targetplayer != null)
                        {
                            tptext = GenerateText("PlayerManagerSelectOverlay", string.Format("Current Position {0} {1} {2}", targetplayer.transform.position.x.ToString(), targetplayer.transform.position.y.ToString(), targetplayer.transform.position.z.ToString()), "12", "0.10", "0.60", "0.75", "0.85");
                            tpbutton = GenerateButton("PlayerManagerSelectOverlay", string.Format("playermanager.teleport {0} {1} {2}", targetplayer.transform.position.x.ToString(), targetplayer.transform.position.y.ToString(), targetplayer.transform.position.z.ToString()), "0 1 0 0.2", "0.60", "1", "0.78", "0.82");
                        }
                        if (targetplayer == null || tptext == string.Empty)
                        {
                            var playertpdata = (PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "Last Position") as Dictionary<string, object>);
                            if (playertpdata != null)
                            {
                                tptext = GenerateText("PlayerManagerSelectOverlay", string.Format("Last Position {0} {1} {2}", playertpdata["x"].ToString(), playertpdata["y"].ToString(), playertpdata["z"].ToString()), "12", "0.10", "0.60", "0.75", "0.85");
                                tpbutton = GenerateButton("PlayerManagerSelectOverlay", string.Format("playermanager.teleport {0} {1} {2}", playertpdata["x"].ToString(), playertpdata["y"].ToString(), playertpdata["z"].ToString()), "0 1 0 0.2", "0.60", "1", "0.78", "0.82");
                            }
                            else
                            {
                                tptext = GenerateText("PlayerManagerSelectOverlay", "Last Position - Unknown", "12", "0.10", "0.60", "0.75", "0.85");
                                tpbutton = GenerateButton("PlayerManagerSelectOverlay", "playermanager.teleport", "1 0 0 0.2", "0.60", "1", "0.78", "0.82");
                            }
                        }
                        string tpbuttontext = GenerateText("PlayerManagerSelectOverlay", "Teleport", "10", "0.60", "1", "0.78", "0.82");
                        AddUI(player, tptext);
                        AddUI(player, tpbuttontext);
                        AddUI(player, tpbutton);
                    }

                    var fc = PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "First Connection") as Dictionary<string, object>;
                    if (fc != null)
                    {
                        string fctext = GenerateText("PlayerManagerSelectOverlay", string.Format("First Connection: {0}", TimeMinToString(fc["0"].ToString())), "10", "0.01", "0.30", "0.70", "0.75");
                        AddUI(player, fctext);
                    }

                    if (targetplayer != null && targetplayer.IsConnected())
                    {
                        string lstext = GenerateText("PlayerManagerSelectOverlay", "Last Seen: Player is Connected", "10", "0.31", "0.60", "0.70", "0.75");
                        AddUI(player, lstext);
                    }
                    else
                    {
                        var ls = PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "Last Seen") as Dictionary<string, object>;
                        if (ls != null)
                        {
                            string lstext = GenerateText("PlayerManagerSelectOverlay", string.Format("Last Seen: {0}", TimeMinToString(ls["0"].ToString())), "10", "0.31", "0.60", "0.70", "0.75");
                            AddUI(player, lstext);
                        }
                    }

                    var tp = PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "Time Played") as Dictionary<string, object>;
                    if (tp != null)
                    {
                        string playedtext = GenerateText("PlayerManagerSelectOverlay", string.Format("Time Played: {0}", SecondsToString(tp["0"].ToString())), "10", "0.61", "1", "0.70", "0.75");
                        AddUI(player, playedtext);
                    }
                    string banreason = string.Empty;
                    if (isBanned(ulong.Parse(playerGUI[player.userID].select), out banreason))
                    {
                        AddUI(player, GenerateText("PlayerManagerSelectOverlay", string.Format("BANNED: {0}", banreason), "15", "0.01", "0.6", "0.65", "0.70", "MiddleLeft"));
                    }
                    var lnames = PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "Names") as Dictionary<string, object>;
                    if (lnames != null)
                    {
                        AddUI(player, GenerateText("PlayerManagerSelectOverlay", "Used Names", "10", "0.01", "0.4", "0.60", "0.65", "MiddleLeft"));
                        decimal currentnum = 0m;
                        foreach (KeyValuePair<string, object> pair in lnames)
                        {
                            decimal currenty = 0.57m - currentnum * 0.03m;
                            string nametext = GenerateText("PlayerManagerSelectOverlay", pair.Value.ToString(), "10", "0.04", "0.4", currenty.ToString(), (currenty + 0.03m).ToString(), "MiddleLeft");
                            AddUI(player, nametext);
                            currentnum++;
                        }
                    }
                    if (player.net.connection.authLevel > 1 || permission.UserHasPermission(player.userID.ToString(), permissionIPs))
                    {
                        var lips = PlayerDatabase?.Call("GetPlayerData", playerGUI[player.userID].select, "IPs") as Dictionary<string, object>;
                        if (lips != null)
                        {
                            AddUI(player, GenerateText("PlayerManagerSelectOverlay", "Used IPs", "10", "0.6", "1", "0.60", "0.65", "MiddleLeft"));
                            decimal currentnum = 0m;
                            foreach (KeyValuePair<string, object> pair in lips)
                            {
                                decimal currenty = 0.57m - currentnum * 0.03m;
                                string iptext = GenerateText("PlayerManagerSelectOverlay", pair.Value.ToString(), "10", "0.63", "1", currenty.ToString(), (currenty + 0.03m).ToString(), "MiddleLeft");
                                AddUI(player, iptext);
                                currentnum++;
                            }
                        }
                    }

                    AddUI(player, GenerateText("PlayerManagerSelectOverlay", "External Plugins", "15", "0.1", "1", "0.30", "0.35", "MiddleLeft"));
                    decimal epy = 0m;
                    foreach(var ecmdsRAW in ExternalCommandsList)
                    {
                        var ecmds = ecmdsRAW as Dictionary<string, object>;
                        if (player.net.connection.authLevel > 1 || permission.UserHasPermission(player.userID.ToString(), ecmds["permission"].ToString()))
                        {
                            decimal yeppos = 0.29m - epy * 0.04m;
                            AddUI(player, GenerateText("PlayerManagerSelectOverlay", ecmds["name"].ToString(), "10", "0.1", "0.5", (yeppos - 0.04m).ToString(), yeppos.ToString(), "MiddleLeft"));
                            decimal epx = 0m;
                            foreach(var ecmdRAW in (ecmds["commands"] as List<object>))
                            {
                                var ecmd = ecmdRAW as Dictionary<string, object>;
                                decimal xeppos = 0.6m + epx * 0.05m;
                                AddUI(player, GenerateText("PlayerManagerSelectOverlay", ecmd["text"].ToString(), "10", xeppos.ToString(), (xeppos + 0.05m).ToString(), (yeppos - 0.04m).ToString(), yeppos.ToString()));
                                AddUI(player, GenerateButton("PlayerManagerSelectOverlay", ecmd["cmd"].ToString().Replace("{steamid}", playerGUI[player.userID].select).Replace("{name}", name), ecmd["color"].ToString(), xeppos.ToString(), (xeppos + 0.05m).ToString(), (yeppos - 0.04m).ToString(), yeppos.ToString()));
                                epx++;
                            }
                            epy++;
                        }
                    }
                    break;
            }
        }
        string SecondsToString(string time) { return SecondsToString(decimal.Parse(time)); }
        string SecondsToString(decimal time)
        {
            decimal days = Math.Floor(time / 86400);
            time -= days * 86400;
            decimal hours = Math.Floor(time / 3600);
            time -= hours * 3600;
            decimal minutes = Math.Floor(time / 60);
            time -= minutes * 60;
            return string.Format("{0}d {1}h {2}m {3}s", days.ToString(), hours.ToString(), minutes.ToString(), Math.Floor(time).ToString());
        }
        string TimeMinToString(string time) { return TimeMinToString(double.Parse(time)); }
        string TimeMinToString(double time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0) + timespan;
            return string.Format("{0}:{1} {2}/{3}/{4}", date.Hour.ToString(), date.Minute.ToString(), date.Month.ToString(), date.Day.ToString(), date.Year.ToString());
        }
        private BasePlayer FindBasePlayerPlayer(string steamid)
        {
            return FindBasePlayerPlayer(ulong.Parse(steamid));
        }
        private BasePlayer FindBasePlayerPlayer(ulong steamid)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.userID == steamid)
                    return player;
            }
            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
            {
                if (player.userID == steamid)
                    return player;
            }
            return null;
        }

        void UpdateSubSectionPlayers(BasePlayer player)
        {
            DestroyGUI(player, "PlayerListSubSection");
            AddUI(player, playerlistsubsectionoverlay);
            Dictionary<string,string> playerList = new Dictionary<string,string>(); ;
            switch(playerGUI[player.userID].subsection)
            {
                case "all":
                    if (PlayerDatabase != null)
                    {
                        var playerLists = (PlayerDatabase.Call("GetAllKnownPlayers") as HashSet<string>).ToList();
                        foreach (string playerl in playerLists)
                        {
                            var playerdata = (PlayerDatabase.Call("GetPlayerData", playerl, "default") as Dictionary<string, object>);
                            string name = "Unknown Player";
                            if (playerdata != null)
                            {
                                if (playerdata["name"] != null)
                                {
                                    name = playerdata["name"] as string;
                                }
                            }
                            playerList.Add(playerl, name);
                        }
                    }
                    else
                    {
                        foreach (BasePlayer tplayer in BasePlayer.activePlayerList)
                        {
                            if (!playerList.ContainsKey(tplayer.userID.ToString()))
                                playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                        }
                        foreach (BasePlayer tplayer in BasePlayer.sleepingPlayerList)
                        {
                            if (!playerList.ContainsKey(tplayer.userID.ToString()))
                                playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                        }
                    }
                    break;
                case "online":
                    foreach(BasePlayer tplayer in BasePlayer.activePlayerList)
                    {
                        if(!playerList.ContainsKey(tplayer.userID.ToString()))
                            playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                    }
                    break;
                case "search":
                    if (playerGUI[player.userID].search == string.Empty)
                    {
                        string searchtext = GenerateText("PlayerListSubSection", "You need to use /playermanager PARTIALNAME/STEAMID", "10", "0.01", "1.0", "0.90", "0.93", "MiddleLeft");
                        AddUI(player, searchtext);
                    }
                    else
                    {
                        if (PlayerDatabase != null)
                        {
                            var playerLists2 = (PlayerDatabase.Call("GetAllKnownPlayers") as HashSet<string>).ToList();
                            foreach (string playerl in playerLists2)
                            {
                                var playerdata = (PlayerDatabase.Call("GetPlayerData", playerl, "default") as Dictionary<string, object>);
                                string name = "Unknown Player";
                                if (playerdata != null)
                                {
                                    if (playerdata["name"] != null)
                                    {
                                        name = playerdata["name"] as string;
                                    }
                                }
                                if (name.ToLower().Contains(playerGUI[player.userID].search))
                                    playerList.Add(playerl, name);
                            }
                        }
                        else
                        {
                            foreach (BasePlayer tplayer in BasePlayer.activePlayerList)
                            {
                                if (!playerList.ContainsKey(tplayer.userID.ToString()) && tplayer.displayName.ToLower().Contains(playerGUI[player.userID].search))
                                    playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                            }
                            foreach (BasePlayer tplayer in BasePlayer.sleepingPlayerList)
                            {
                                if (!playerList.ContainsKey(tplayer.userID.ToString()) && tplayer.displayName.ToLower().Contains(playerGUI[player.userID].search))
                                    playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                            }
                        }
                    }
                    break;
                case "sleepers":
                    foreach (BasePlayer tplayer in BasePlayer.sleepingPlayerList)
                    {
                        if (!playerList.ContainsKey(tplayer.userID.ToString()))
                            playerList.Add(tplayer.userID.ToString(), tplayer.displayName);
                    }
                    break;
                case "banned":
                    foreach (ServerUsers.User user in ServerUsers.GetAll(ServerUsers.UserGroup.Banned))
                    {
                        var playerdata = (PlayerDatabase?.Call("GetPlayerData", user.steamid.ToString(), "default") as Dictionary<string, object>);
                        string name = user.username;
                        if (playerdata != null)
                        {
                            if (playerdata["name"] != null)
                            {
                                name = playerdata["name"] as string;
                            }
                        }
                        playerList.Add(user.steamid.ToString(), name);
                    }
                    if(EnhancedBanSystem != null)
                    {
                        var banlist = EnhancedBanSystem.Call("BannedPlayers") as List<string>;
                        if(banlist != null)
                        {
                            foreach(string userid in banlist)
                            {
                                if (playerList.ContainsKey(userid)) continue;
                                var name = "Unknown Player";
                                var bandata = EnhancedBanSystem.Call("GetBanData", userid) as Dictionary<string,object>;
                                if(bandata != null)
                                {
                                    if (bandata.ContainsKey("name"))
                                        name = bandata["name"].ToString();
                                    if (name == "Unknown Player")
                                    {
                                        var playerdata = (PlayerDatabase?.Call("GetPlayerData", userid, "default") as Dictionary<string, object>);
                                        if (playerdata != null)
                                        {
                                            if (playerdata["name"] != null)
                                            {
                                                name = playerdata["name"] as string;
                                            }
                                        }
                                    }
                                }
                                playerList.Add(userid, name);
                            }
                        }
                    }
                    break;
                default:
                     
                    break;
            }
            var items = from pair in playerList
                        orderby pair.Value ascending
                        select pair;
            decimal p = 0.0m;
            int page = (playerGUI[player.userID]).page;
            foreach(KeyValuePair<string,string> pair in items)
            {
                if (p >= page)
                {
                    decimal currentheight = (0.94m - 0.03m * (p- page));
                    string playertext = GenerateText("PlayerListSubSection", string.Format("{0} - {1}", pair.Key, pair.Value), "10", "0.01", "1.0", (currentheight - 0.03m).ToString(), (currentheight).ToString(), "MiddleLeft");
                    string playerbutton = GenerateButton("PlayerListSubSection", string.Format("playermanager.select {0}", pair.Key), "0 0 0 0", "0.01", "1.0", (currentheight - 0.03m).ToString(), (currentheight).ToString());
                    AddUI(player, playertext);
                    AddUI(player, playerbutton);
                }
                if (p > page + 25m) break;
                p++;
            }
        }
        bool isBanned(ulong steamid, out string reason)
        {
            reason = string.Empty;
            ServerUsers.User user = ServerUsers.Get(steamid);
            if(user != null)
            {
                if(user.group == ServerUsers.UserGroup.Banned)
                {
                    reason = user.notes;
                    return true;
                }
            }
            if(EnhancedBanSystem != null)
            {
                var bandata = EnhancedBanSystem.Call("GetBanData", steamid) as Dictionary<string, object>;
                if (bandata != null)
                {
                    if (bandata.ContainsKey("reason"))
                    {
                        reason = bandata["reason"].ToString();
                        return true;
                    }
                }
            }
            /*
            DynamicConfigFile ebslist = Interface.Oxide.DataFileSystem.GetFile("ebsbanlist");
            if(ebslist[steamid.ToString()] != null)
            {
                var bandata = ebslist[steamid.ToString()] as Dictionary<string, object>;
                if(bandata.ContainsKey("steamID"))
                {
                    reason = bandata.ContainsKey("reason") ? bandata["reason"].ToString() : "Unknown";
                    return true;
                }
            }*/
            return false;
        }
        
        string parentoverlay = @"[
			{
				""name"": ""PlayerManagerOverlay"",
				""parent"": ""Overlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.8"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0"",
						""anchormax"": ""1 1""
					},
					{
						""type"":""NeedsCursor"",
					}
				]
			},
			{
				""parent"": ""PlayerManagerOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Close"",
						""fontSize"":20,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0.01"",
						""anchormax"": ""1 0.05""
					},
				]
			},
			{
				""parent"": ""PlayerManagerOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Button"",
						""command"":""playermanager.close"",
						""color"": ""0.5 0.5 0.5 0.2"",
						""imagetype"": ""Tiled""
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0.01"",
						""anchormax"": ""1 0.05""
					}
				]
			}
		]
		";
        static Dictionary<string, object> dialogActions = new Dictionary<string, object> {
            {
                "ban" , new List<object>
                {
                    {"Wallhack"},
                     {"Speedhacker"},
                      {"Hacker"},
                    {"Aimbot"},
                    {"Spawnhack"},
                    {"Glitch"},
                    {"Disrespectfull"},
                    {"Other"}
                }
            },
            {
                "kick" , new List<object>
                {
                    {"Disrespectfull"},
                    {"Watch you language"},
                    {"Change Name"},
                    {"Other"}
                }
            },
            {
                "timelimit" , new List<object>
                {
                    {"0"},
                    {"1"},
                    {"60"},
                    {"300"},
                    {"600"},
                    {"3600"},
                    {"86400"},
                    {"172800"},
                    {"259200"},
                    {"604800"}
                }
            }
         };
        
        void RefreshDialog(BasePlayer player)
        {
            if (playerDialog[player.userID] == null) return;
            if (playerGUI[player.userID] == null) return;
            DestroyGUI(player, "DialogOverlay");
            if (!dialogActions.ContainsKey("timelimit"))
            {
                Debug.Log("ERROR timelimit WASNT PROPERLY SET IN THE CONFIGS! RESET YOUR CONFIGS AND TRY AGAIN");
                return;
            }
            AddUI(player, dialogoverlay);
            switch (playerDialog[player.userID].action)
            {
                case "ban":
                    if(!dialogActions.ContainsKey("ban"))
                    {
                        Debug.Log("ERROR ban WASNT PROPERLY SET IN THE CONFIGS! RESET YOUR CONFIGS AND TRY AGAIN");
                        DestroyGUI(player, "DialogOverlay");
                        return;
                    }
                    string reasontext = GenerateText("DialogOverlay", "Reason", "20", "0.4", "0.5", "0.80", "0.85", "MiddleLeft");
                    AddUI(player,reasontext);
                    var actions = dialogActions["ban"] as List<object>;
                    for (int i = 0; i < actions.Count; i++ )
                    {
                        decimal currenty = 0.75m - Convert.ToDecimal(i) * 0.04m;
                        string reasonstext = GenerateText("DialogOverlay", actions[i].ToString(), "12", "0.4", "0.5", currenty.ToString(),( currenty+0.04m).ToString(), "MiddleLeft");
                        string reasonsbutton = GenerateButton("DialogOverlay", string.Format("playermanager.dialogselect ban {0}", actions[i].ToString()), "0 0 0 0.4", "0.4", "0.5", currenty.ToString(), (currenty + 0.04m).ToString());
                        AddUI(player, reasonstext);
                        AddUI(player, reasonsbutton);
                    }

                    AddUI(player, GenerateText("DialogOverlay", "Time (s)", "20", "0.5", "0.6", "0.80", "0.85", "MiddleLeft"));
                    actions = dialogActions["timelimit"] as List<object>;
                    for (int i = 0; i < actions.Count; i++)
                    {
                        decimal currenty = 0.75m - Convert.ToDecimal(i) * 0.04m;
                        string reasonstext = GenerateText("DialogOverlay", actions[i].ToString(), "12", "0.5", "0.6", currenty.ToString(), (currenty + 0.04m).ToString(), "MiddleLeft");
                        string reasonsbutton = GenerateButton("DialogOverlay", string.Format("playermanager.dialogselect timelimit {0}", actions[i].ToString()), "0 0 0 0.4", "0.5", "0.6", currenty.ToString(), (currenty + 0.04m).ToString());
                        AddUI(player, reasonstext);
                        AddUI(player, reasonsbutton);
                    }

                    break;
                case "kick":
                    if (!dialogActions.ContainsKey("kick"))
                    {
                        Debug.Log("ERROR ban WASNT PROPERLY SET IN THE CONFIGS! RESET YOUR CONFIGS AND TRY AGAIN");
                        DestroyGUI(player, "DialogOverlay");
                        return;
                    }
                    AddUI(player, GenerateText("DialogOverlay", "Reason", "20", "0.4", "0.5", "0.80", "0.85", "MiddleLeft"));
                    var kickactions = dialogActions["kick"] as List<object>;
                    for (int i = 0; i < kickactions.Count; i++)
                    {
                        decimal currenty = 0.75m - Convert.ToDecimal(i) * 0.04m;
                        string reasonstext = GenerateText("DialogOverlay", kickactions[i].ToString(), "12", "0.4", "0.5", currenty.ToString(), (currenty + 0.04m).ToString(), "MiddleLeft");
                        string reasonsbutton = GenerateButton("DialogOverlay", string.Format("playermanager.dialogselect kick {0}", kickactions[i].ToString()), "0 0 0 0.4", "0.4", "0.5", currenty.ToString(), (currenty + 0.04m).ToString());
                        AddUI(player, reasonstext);
                        AddUI(player, reasonsbutton);
                    }
                    break;
            }
        }
        string dialogoverlay = @"[
			{
				""name"": ""DialogOverlay"",
				""parent"": ""Overlay"",
				""components"":
				[
					{
						 ""type"":""UnityEngine.UI.Image"",
						 ""color"":""0.1 0.1 0.1 0.8"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0 0"",
						""anchormax"": ""1 1""
					},
					{
						""type"":""NeedsCursor"",
					}
				]
			},
			{
				""parent"": ""DialogOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Close"",
						""fontSize"":20,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.4 0.25"",
						""anchormax"": ""0.55 0.30""
					},
				]
			},
			{
				""parent"": ""DialogOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Button"",
						""command"":""playermanager.dialogclose"",
						""color"": ""0.5 0.5 0.5 0.2"",
						""imagetype"": ""Tiled""
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.4 0.25"",
						""anchormax"": ""0.55 0.30""
					}
				]
			},
        {
				""parent"": ""DialogOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""Ok"",
						""fontSize"":20,
						""align"": ""MiddleCenter"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.55 0.25"",
						""anchormax"": ""0.60 0.30""
					},
				]
			},
			{
				""parent"": ""DialogOverlay"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Button"",
						""command"":""playermanager.execute"",
						""color"": ""0 1 0 0.2"",
						""imagetype"": ""Tiled""
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.55 0.25"",
						""anchormax"": ""0.60 0.30""
					}
				]
			}
		]
		";


        string jsonbutton = @"[
			{
				""parent"": ""{0}"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Button"",
						""command"":""{1}"",
						""color"": ""{2}"",
						""imagetype"": ""Tiled""
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""{3} {5}"",
						""anchormax"": ""{4} {6}""
					}
				]
			}
		]
		";
        string jsontext = @"[
			{
				""parent"": ""{0}"",
				""components"":
				[
					{
						""type"":""UnityEngine.UI.Text"",
						""text"":""{1}"",
						""fontSize"":{2},
						""align"": ""{7}"",
					},
					{
						""type"":""RectTransform"",
						""anchormin"": ""{3} {5}"",
						""anchormax"": ""{4} {6}""
					}
				]
			}
		]
		";

        string GenerateButton(string overlay, string command, string color, string xmin, string xmax, string ymin, string ymax)
        {
            return jsonbutton.Replace("{0}", overlay).Replace("{1}", command).Replace("{2}", color).Replace("{3}", xmin).Replace("{4}", xmax).Replace("{5}", ymin).Replace("{6}", ymax);
        }
        string GenerateText(string overlay, string text, string textsize, string xmin, string xmax, string ymin, string ymax, string pos = "MiddleCenter")
        {
            return jsontext.Replace("{0}", overlay).Replace("{1}", text).Replace("{2}", textsize).Replace("{3}", xmin).Replace("{4}", xmax).Replace("{5}", ymin).Replace("{6}", ymax).Replace("{7}", pos).Replace("'","");
        }
        [ChatCommand("playermanager")]
        void cmdChatPlayermanage(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 2 && !permission.UserHasPermission(player.userID.ToString(), permissionPM)) return;
            playerGUI[player.userID] = new PlayerMGUI();
            playerGUI[player.userID].search = args.Length > 0 ? args[0].ToLower() : string.Empty;
            RefreshFullUI(player);
        }
        [ConsoleCommand("playermanager.close")]
        void cmdConsolePlayermanagerClose(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            DestroyGUI(arg.Player(),"PlayerManagerOverlay");
        }
        [ConsoleCommand("playermanager.subsection")]
        void cmdConsolePlayermanagerSubsection(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length == 0) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            playerGUI[arg.Player().userID].subsection = arg.Args[0];
            RefreshSection(arg.Player());
        }
        [ConsoleCommand("playermanager.select")]
        void cmdConsolePlayermanagerSelect(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length == 0) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            playerGUI[arg.Player().userID].select = arg.Args[0];
            UpdateSelect(arg.Player());
        }
        Hash<ulong, DialogOptions> playerDialog = new Hash<ulong, DialogOptions>();
        [ConsoleCommand("playermanager.dialog")]
        void cmdConsolePlayermanagerDialog(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length == 0) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            if (playerDialog[arg.Player().userID] == null)
                playerDialog[arg.Player().userID] = new DialogOptions();
            playerDialog[arg.Player().userID].action = arg.Args[0];
            RefreshDialog(arg.Player());
        }
        [ConsoleCommand("playermanager.execute")]
        void cmdConsolePlayermanagerExecute(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            if (playerDialog[arg.Player().userID] == null) return;
            switch(playerDialog[arg.Player().userID].action)
            {
                case "ban":
                    if (arg.Player().net.connection.authLevel < 2 && !permission.UserHasPermission(arg.Player().userID.ToString(), permissionBAN)) return;
                    if (!playerDialog[arg.Player().userID].option.ContainsKey("ban")) return;
                    if (!playerDialog[arg.Player().userID].option.ContainsKey("timelimit")) return;
                    PlayerManagerBan(arg.Player(), playerGUI[arg.Player().userID].select);
                    break;
                case "kick":
                    if (arg.Player().net.connection.authLevel < 2 && !permission.UserHasPermission(arg.Player().userID.ToString(), permissionKICK)) return;
                    if (!playerDialog[arg.Player().userID].option.ContainsKey("kick")) return;
                    PlayerManagerKick(arg.Player(), playerGUI[arg.Player().userID].select);
                    break;
            }
            DestroyGUI(arg.Player(), "DialogOverlay");
        }

        [ConsoleCommand("playermanager.dialogselect")]
        void cmdConsolePlayermanagerDialogselect(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length < 2) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            if (playerDialog[arg.Player().userID] == null) return;
            if (playerDialog[arg.Player().userID].option.ContainsKey(arg.Args[0]))
                playerDialog[arg.Player().userID].option.Remove(arg.Args[0]);
            playerDialog[arg.Player().userID].option.Add(arg.Args[0], arg.Args[1]);
        }
        [ConsoleCommand("playermanager.dialogclose")]
        void cmdConsolePlayermanagerDialoCloseg(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            DestroyGUI(arg.Player(), "DialogOverlay");
        }
        void PlayerManagerKick(BasePlayer player, string steamidstring)
        {
            if (playerDialog[player.userID] == null) return;
            BasePlayer targetplayer = BasePlayer.Find(steamidstring);
            if(targetplayer == null)
            {
                SendReply(player, string.Format("The player {0} isnt online", steamidstring));
                return;
            }
            ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { 0, string.Format(lang.GetMessage("PlayerKicked", this), targetplayer.displayName, playerDialog[player.userID].option.ContainsKey("kick") ? playerDialog[player.userID].option["kick"] : "Unknown") });
            targetplayer.Kick(string.Format(lang.GetMessage("YouKicked", this), playerDialog[player.userID].option.ContainsKey("kick") ? playerDialog[player.userID].option["kick"] : "Unknown"));
        }
        [ConsoleCommand("playermanager.teleport")]
        void cmdConsolePlayermanagerTeleport(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length < 3) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            if (arg.Player().net.connection.authLevel < 2 && !permission.UserHasPermission(arg.Player().userID.ToString(), permissionTP)) return;
            Vector3 destination = new Vector3(float.Parse(arg.Args[0]), float.Parse(arg.Args[1]), float.Parse(arg.Args[2]));
            arg.Player().MovePosition(destination);
            arg.Player().ClientRPCPlayer(null, arg.Player(), "ForcePositionTo", destination);
        }
        void PlayerManagerBan(BasePlayer player, string steamidstring)
        {
            if (playerDialog[player.userID] == null) return;
            ulong steamid = ulong.Parse(steamidstring);
            if(steamid < 0xf8b0a10e470000L)
            {
                SendReply(player, "This doesnt seem to be a steamid");
                return;
            }

            var playerdata = (PlayerDatabase.Call("GetPlayerData", steamidstring, "default") as Dictionary<string, object>);
            string name = "Unknown Player";
            if (playerdata != null)
            {
                if (playerdata["name"] != null)
                {
                    name = playerdata["name"] as string;
                }
            }
            ServerUsers.User user = ServerUsers.Get(steamid);
            if (user != null && (user.group == ServerUsers.UserGroup.Banned))
            {
                SendReply(player, string.Format("The user {0} is already in the banlist", steamid.ToString()));
            }
            else
            {
                ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { 0, string.Format(lang.GetMessage("PlayerBanned", this), steamid.ToString(), name, playerDialog[player.userID].option.ContainsKey("ban") ? playerDialog[player.userID].option["ban"] : "Unknown") });
                if (EnhancedBanSystem != null)
                {
                    EnhancedBanSystem.Call("BanID", null, steamid, playerDialog[player.userID].option.ContainsKey("ban") ? playerDialog[player.userID].option["ban"] : "Unknown", playerDialog[player.userID].option.ContainsKey("timelimit") ? int.Parse(playerDialog[player.userID].option["timelimit"]) : 0);
                }
                else
                {
                    ServerUsers.Set(steamid, ServerUsers.UserGroup.Banned, name, playerDialog[player.userID].option.ContainsKey("ban") ? playerDialog[player.userID].option["ban"] : "Unknown");
                }
            }
                       
            BasePlayer targetplayer = BasePlayer.Find(steamidstring);
            if (targetplayer != null)
            {
                targetplayer.Kick(string.Format(lang.GetMessage("YouBanned", this), playerDialog[player.userID].option.ContainsKey("ban") ? playerDialog[player.userID].option["ban"] : "Unknown"));
                return;
            }
        }
        [ConsoleCommand("playermanager.page")]
        void cmdConsolePlayermanagerPage(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null) return;
            if (arg.Args.Length == 0) return;
            if (arg.Player() == null) return;
            if (playerGUI[arg.Player().userID] == null) return;
            switch(playerGUI[arg.Player().userID].section)
            {
                case "players":
                    if(arg.Args[0] == "next")
                        playerGUI[arg.Player().userID].page += 25;
                    else
                        playerGUI[arg.Player().userID].page -= 25;
                    if(playerGUI[arg.Player().userID].page < 0)
                        playerGUI[arg.Player().userID].page = 0;
                   
                    break;
            }
            RefreshSection(arg.Player());
        }
    }
}