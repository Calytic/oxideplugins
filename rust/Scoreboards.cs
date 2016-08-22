using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Scoreboards", "LaserHydra", "1.0.0", ResourceId = 0)]
    [Description("Allows you to create Scoreboards and plugins to insert data")]
    class Scoreboards : RustPlugin
    {
        static Scoreboards Instance;
        static ScoreboardData Data;

        #region Config Variables

        static string AnchorMin;
        static string AnchorMax;

        static string BackgroundColor;

        static string HeaderColor;
        static string TitleColor;
        static string ContentColor;

        #endregion

        #region Classes

        public class ScoreboardData
        {
            public string ActiveScoreboard = string.Empty;
            public List<Scoreboard> All = new List<Scoreboard>();
        }

        public class Scoreboard
        {
            public static Scoreboard Find(string Title) => Data.All.Find((s) => s.Title == Title);

            public string Title;
            public string Description;
            internal bool Active => Data.ActiveScoreboard == Title;

            internal KeyValuePair<string, string>[] Entries = new KeyValuePair<string, string>[0];

            public override int GetHashCode() => Title.GetHashCode();

            public void Remove() => Data.All.Remove(this);

            public void SetActive() => SetActiveScoreboard(this);

            public void SetEntries(KeyValuePair<string, string>[] Entries)
            {
                this.Entries = Entries;
                Instance.ScoreboardUpdated(this);
            }

            public static void SetActiveScoreboard(Scoreboard scoreboard)
            {
                Data.ActiveScoreboard = scoreboard.Title;
                Instance.ActiveScoreboardChanged(scoreboard);
            }

            public static Scoreboard GetActiveScoreboard() => Data.All.Find((s) => s.Active);

            public static void AddScoreboard(Scoreboard scoreboard)
            {
                if (!Data.All.Contains(scoreboard))
                {
                    Data.All.Add(scoreboard);
                    Instance.WriteData(Data);
                }
            }
        }

        #endregion

        #region API

        void UpdateScoreboard(string ScoreboardTitle, KeyValuePair<string, string>[] Entries)
        {
            var scoreboard = Scoreboard.Find(ScoreboardTitle);

            if (scoreboard == null)
                return;

            scoreboard.SetEntries(Entries);
        }

        void CreateScoreboard(string ScoreboardTitle, string ScoreboardDescription, KeyValuePair<string, string>[] Entries)
        {
            if (Scoreboard.Find(ScoreboardTitle) != null)
                return;

            Scoreboard.AddScoreboard(new Scoreboard { Title = ScoreboardTitle, Description = ScoreboardDescription, Entries = Entries.Take(10).ToArray() });
        }

        void RemoveScoreboard(string ScoreboardTitle)
        {
            var scoreboard = Scoreboard.Find(ScoreboardTitle);

            if (scoreboard == null)
                return;

            scoreboard.Remove();
        }

        #endregion

        #region Scoreboards

        void ScoreboardUpdated(Scoreboard scoreboard)
        {
            WriteData(Data);

            if (scoreboard.Active)
                UpdateScoreboardUI();
        }

        void ActiveScoreboardChanged(Scoreboard scoreboard)
        {
            UpdateScoreboardUI();
            WriteData(Data);
        }

        void UpdateScoreboardUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DrawScoreboardUI(player);
        }

        void DrawScoreboardUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Scoreboard");

            var cui = GetCUI();
            
            if (cui != null)
                CuiHelper.AddUi(player, cui);
        }

        static CuiElementContainer GetCUI()
        {
            var activeScoreboard = Scoreboard.GetActiveScoreboard();

            if (activeScoreboard == null)
                return null;

            CuiElementContainer elements = new CuiElementContainer();

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = BackgroundColor
                },
                RectTransform =
                {
                    AnchorMin = AnchorMin,
                    AnchorMax = AnchorMax
                }
            }, "Hud.Under", "Scoreboard");

            elements.Add(new CuiElement
            {
                Name = "Header",
                Parent = "Scoreboard",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = "Scoreboard",
                        Align = TextAnchor.LowerCenter,
                        FontSize = 28,
                        Color = HeaderColor
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.06372549 0.8822171",
                        AnchorMax = "0.9411765 0.9676675"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = "Title",
                Parent = "Scoreboard",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = activeScoreboard.Title,
                        Align = TextAnchor.UpperCenter,
                        FontSize = 18,
                        Color = TitleColor
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.06372549 0.8337182",
                        AnchorMax = "0.9411765 0.8960739"
                    }
                }
            });

            elements.Add(new CuiElement
            {
                Name = "Contents",
                Parent = "Scoreboard",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = string.Join("\n", activeScoreboard.Entries.Select((kvp) => $"{kvp.Key}: {kvp.Value}").ToArray()),
                        Align = TextAnchor.UpperCenter,
                        FontSize = 18,
                        Color = ContentColor
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.06372549 0.0277136",
                        AnchorMax = "0.9411765 0.8337182"
                    }
                }
            });

            return elements;
        }

        #endregion

        #region Commands

        [ChatCommand("scoreboard")]
        void cmdScoreboard(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "scoreboards.admin"))
            {
                SendReply(player, LangMsg("No Permission", player.userID));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "/scoreboard <select|disable|list>");
                return;
            }

            switch (args[0].ToLower())
            {
                case "select":
                    if (args.Length != 2)
                    {
                        SendReply(player, "Syntax: /scoreboard select <scoreboard>");
                        return;
                    }

                    var scoreboard = Scoreboard.Find(args[1]);

                    if (scoreboard == null)
                    {
                        SendReply(player, LangMsg("Scoreboard Does Not Exist", player.userID, args[1]));
                        return;
                    }

                    scoreboard.SetActive();
                    SendReply(player, LangMsg("Scoreboard Selected", player.userID, scoreboard.Title));

                    break;

                case "disable":

                    Scoreboard.SetActiveScoreboard(new Scoreboard { Title = string.Empty });
                    SendReply(player, LangMsg("Scoreboard Disabled", player.userID));

                    break;

                case "list":

                    SendReply(player, $"Scoreboards:{Environment.NewLine}{string.Join(Environment.NewLine, Data.All.Select((s) => $"{s.Title} - {s.Description}").ToArray())}");

                    break;

                default:
                    SendReply(player, "/scoreboard <select|disable|list>");
                    break;
            }
        }

        #endregion

        #region Hooks

        void Loaded()
        {
            Instance = this;

            permission.RegisterPermission("scoreboards.admin", this);
            ReadData(out Data);
            LoadMessages();
            LoadConfig();

            CreateScoreboard("Bullets Fired", "Shows the amount of fired bullets", new KeyValuePair<string, string>[0]);

            UpdateScoreboardUI();
        }

        void Unloaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, "Scoreboard");
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            DrawScoreboardUI(player);
        }

        #endregion
        
        #region Loading

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No Permission", "You do not have permission to do that." }
            }, this);
        }

        new void LoadConfig()
        {
            AnchorMin = UpdateConfig<string>("Anchor Min", "0.83 0.21");
            AnchorMax = UpdateConfig<string>("Anchor Max", "0.98 0.91");

            BackgroundColor = UpdateConfig<string>("Background Color", "0.3 0.3 0.3 0.7");

            HeaderColor = UpdateConfig<string>("Header Color", "0.66 1 0 1");
            TitleColor = UpdateConfig<string>("Title Color", "0 0 0 1");
            ContentColor = UpdateConfig<string>("Content Color", "0 0 0 1");

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new configuration file...");

        #endregion

        #region Helpers

        string LangMsg(string key, object id = null, params string[] replacements) => string.Format(lang.GetMessage(key, this, id == null ? null : id.ToString()), replacements);

        DataValue ReadData<DataValue>(out DataValue data, string filename = null) => data = Core.Interface.Oxide.DataFileSystem.ReadObject<DataValue>(filename == null ? GetType().Name : $"{GetType().Name}/{filename}");
        void WriteData<DataValue>(DataValue data, string filename = null) => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == null ? GetType().Name : $"{GetType().Name}/{filename}", data);

        ConfigValue UpdateConfig<ConfigValue>(params object[] fullPath)
        {
            List<string> pathL = fullPath.Select((v) => (string)v).ToList();
            pathL.RemoveAt(pathL.Count - 1);
            string[] path = pathL.ToArray();

            if (Config.Get(path) == null)
            {
                PrintWarning("Generating config value: {0}", string.Join("/", path));
                Config.Set(fullPath);
            }

            return (ConfigValue)Convert.ChangeType(Config.Get(path), typeof(ConfigValue));
        }

        #endregion
    }
}