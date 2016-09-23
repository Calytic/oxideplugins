using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("KDRGui", "Ankawi/LaserHydra", "1.0.4")]
    [Description("GUI that portrays kills, deaths, player name, and K/D Ratio")]
    class KDRGui : RustPlugin
    {
        Dictionary<ulong, HitInfo> LastWounded = new Dictionary<ulong, HitInfo>();

        static HashSet<PlayerData> LoadedPlayerData = new HashSet<PlayerData>();
        List<UIObject> UsedUI = new List<UIObject>();

        #region UI Classes

        // UI Classes - Created by LaserHydra
        class UIColor
        {
            double red;
            double green;
            double blue;
            double alpha;

            public UIColor(double red, double green, double blue, double alpha)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                this.alpha = alpha;
            }

            public override string ToString()
            {
                return $"{red.ToString()} {green.ToString()} {blue.ToString()} {alpha.ToString()}";
            }
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject()
            {
            }

            public string RandomString()
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                foreach (string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }

            public string AddPanel(string name, double left, double top, double width, double height, UIColor color, bool mouse = false, string parent = "Overlay")
            {
                name = name + RandomString();

                string type = "";
                if (mouse) type = "NeedsCursor";

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Image"},
                                {"color", color.ToString()}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            },
                            new Dictionary<string, string> {
                                {"type", type}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "Overlay", int alignmode = 0)
            {
                name = name + RandomString(); text = text.Replace("\n", "{NEWLINE}"); string align = "";

                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.ToString()},
                                {"align", align}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddButton(string name, double left, double top, double width, double height, UIColor color, string command = "", string parent = "Overlay", string closeUi = "")
            {
                name = name + RandomString();

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"close", closeUi},
                                {"command", command},
                                {"color", color.ToString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddImage(string name, double left, double top, double width, double height, UIColor color, string url = "http://oxidemod.org/data/avatars/l/53/53411.jpg?1427487325", string parent = "Overlay")
            {
                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"sprite", "assets/content/textures/generic/fulltransparent.tga"},
                                {"url", url},
                                {"color", color.ToString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString().Replace(",", ".")} {((1 - top) - height).ToString().Replace(",", ".")}"},
                                {"anchormax", $"{(left + width).ToString().Replace(",", ".")} {(1 - top).ToString().Replace(",", ".")}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }
        #endregion

        #region Data
        class PlayerData
        {
            public ulong id;
            public string name;
            public int kills;
            public int deaths;
            internal float KDR => deaths == 0 ? kills : (float)Math.Round(((float)kills) / deaths, 1);

            internal static void TryLoad(BasePlayer player)
            {
                if (Find(player) != null)
                    return;

                PlayerData data = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>($"KDRGui/{player.userID}");

                if (data == null || data.id == 0)
                {
                    data = new PlayerData
                    {
                        id = player.userID,
                        name = player.displayName
                    };
                }
                else
                    data.Update(player);

                data.Save();
                LoadedPlayerData.Add(data);
            }

            internal void Update(BasePlayer player)
            {
                name = player.displayName;
                Save();
            }

            internal void Save() => Interface.Oxide.DataFileSystem.WriteObject($"KDRGui/{id}", this, true);
            internal static PlayerData Find(BasePlayer player)
            {

                PlayerData data = LoadedPlayerData.ToList().Find((p) => p.id == player.userID);

                return data;
            }
        }
        #endregion

        #region Hooks
        void OnPlayerInit(BasePlayer player)
        {
            PlayerData.TryLoad(player);
        }
        //void LoadSleeperData()
        //{
        //    var sleepers = BasePlayer.sleepingPlayerList;
        //    foreach(var sleeper in sleepers)
        //    {
        //        PlayerData.TryLoad(sleeper);
        //        Puts("Loaded sleeper data");
        //    }
        //}
        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerData.TryLoad(player);
        }

        void Unloaded()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                foreach (var ui in UsedUI)
                    ui.Destroy(player);
            }
        }

        void Loaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerData.TryLoad(player);
            }

        }
        HitInfo TryGetLastWounded(ulong id, HitInfo info)
        {
            if (LastWounded.ContainsKey(id))
            {
                HitInfo output = LastWounded[id];
                LastWounded.Remove(id);
                return output;
            }

            return info;
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity?.ToPlayer() != null && info?.Initiator?.ToPlayer() != null)
            {
                NextTick(() =>
                {
                    if (entity.ToPlayer().IsWounded())
                        LastWounded[entity.ToPlayer().userID] = info;
                });
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == info.Initiator) return;
                if (entity == null || info.Initiator == null) return;

                if (info?.Initiator?.ToPlayer() == null && (entity?.name?.Contains("autospawn") ?? false))
                    return;
                if (entity.ToPlayer() != null)
                {
                    if (entity.ToPlayer().IsWounded())
                    {
                        info = TryGetLastWounded(entity.ToPlayer().userID, info);
                    }
                }
                if (entity != null && entity is BasePlayer && info?.Initiator != null && info.Initiator is BasePlayer)
                {
                    PlayerData victimData = PlayerData.Find((BasePlayer)entity);
                    PlayerData attackerData = PlayerData.Find((BasePlayer)info.Initiator);

                    victimData.deaths++;
                    attackerData.kills++;

                    victimData.Save();
                    attackerData.Save();
                }
            }
            catch (Exception ex)
            {              
            }
        }
        #endregion

        #region UI Handling
        void DrawKDRWindow(BasePlayer player)
        {
            UIObject ui = new UIObject();
            string panel = ui.AddPanel("panel1", 0.0132382892057026, 0.0285714285714286, 0.958248472505092, 0.874285714285714, new UIColor(0.501960784313725, 0.501960784313725, 0.501960784313725, 1), true, "Overlay");
            ui.AddText("list", 0.0626992561105207, 0.250544662309368, 0.83740701381509, 0.697167755991285, new UIColor(0, 1, 1, 1), GetTopList(), 20, panel, 7);
            ui.AddText("label4", 0.390148777895855, 0.163398692810458, 0.18384697130712, 0.0610021786492375, new UIColor(0, 0, 0, 1), "K/D Ratio", 24, panel, 7);
            ui.AddText("label3", 0.223358129649309, 0.163398692810458, 0.188097768331562, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Deaths", 24, panel, 7);
            ui.AddText("label2", 0.0541976620616366, 0.163398692810458, 0.16365568544102, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Kills", 24, panel, 7);
            ui.AddText("label1", 0.552444208289054, 0.163398692810458, 0.197662061636557, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Player Name", 24, panel, 7);
            string close = ui.AddButton("button1", 0.872476089266737, 0.0479302832244009, 0.0924548352816153, 0.0915032679738562, new UIColor(1, 0, 0, 1), "", panel, panel);
            ui.AddText("button1_Text", 0, 0, 1, 1, new UIColor(0.93, 23, 23, 0.9), "Close", 18, close, 3);

            //old alignment //ignore this
            //string panel = ui.AddPanel("panel1", 0.0132382892057026, 0.0285714285714286, 0.958248472505092, 0.874285714285714, new UIColor(0.501960784313725, 0.501960784313725, 0.501960784313725, 1), true, "Overlay");
            //ui.AddText("list", 0.0626992561105207, 0.250544662309368, 0.83740701381509, 0.697167755991285, new UIColor(0, 1, 1, 1), GetTopList(), 20, panel, 7);
            //ui.AddText("label4", 0.390148777895855, 0.163398692810458, 0.0924548352816153, 0.0610021786492375, new UIColor(0, 0, 0, 1), "K/D Ratio", 24, panel, 7);
            //ui.AddText("label3", 0.223358129649309, 0.163398692810458, 0.0786397449521785, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Deaths", 24, panel, 7);
            //ui.AddText("label2", 0.0541976620616366, 0.163398692810458, 0.0478214665249734, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Kills", 24, panel, 7);
            //ui.AddText("label1", 0.552444208289054, 0.163398692810458, 0.126461211477152, 0.0610021786492375, new UIColor(0, 0, 0, 1), "Player Name", 24, panel, 7);
            //string close = ui.AddButton("button1", 0.872476089266737, 0.0479302832244009, 0.0924548352816153, 0.0915032679738562, new UIColor(1, 0, 0, 1), "", panel, panel);
            //ui.AddText("button1_Text", 0, 0, 1, 1, new UIColor(0.93, 23, 23, 0.9), "Close", 18, close, 3);

            ui.Draw(player);

            UsedUI.Add(ui);
        }

        string GetTopList()
        {
            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                PlayerData.TryLoad(player);

            return string.Join("\n", LoadedPlayerData.OrderByDescending((d) => d.kills).Select((d) => $"{d.kills}                                                {d.deaths}                                                {d.KDR.ToString("0.00")}                                              {d.name}").Take(15).ToArray());
        }
        #endregion

        #region Commands
        [ChatCommand("top")]
        void cmdTop(BasePlayer player, string command, string[] args)
        {
            DrawKDRWindow(player);
        }

        [ChatCommand("kdr")]
        void cmdKdr(BasePlayer player, string command, string[] args)
        {
            GetCurrentStats(player);
        }
        void GetCurrentStats(BasePlayer player)
        {
            PlayerData data = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>($"KDRGui/{player.userID}");
            int kills = data.kills;
            int deaths = data.deaths;
            string playerName = data.name;
            float kdr = data.KDR;

            //rust.SendChatMessage(player, "<color=lime> Player Name : </color>" + $"{playerName}");
            //rust.SendChatMessage(player, "<color=red> Kills : </color>" + $"{kills}");
            //rust.SendChatMessage(player, "<color=red> Deaths : </color>" + $"{deaths}");
            //rust.SendChatMessage(player, "<color=red> K/D Ratio : </color>" + $"{kdr}");

            rust.SendChatMessage(player, "<color=red> Player Name : </color>" + $"{playerName}"
                                        + "\n" + "<color=lime> Kills : </color>" + $"{kills}"
                                        + "\n" + "<color=lime> Deaths : </color>" + $"{deaths}"
                                        + "\n" + "<color=lime> K/D Ratio : </color>" + $"{kdr}");
        }
        #endregion
    }
}