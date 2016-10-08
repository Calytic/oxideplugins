using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using System.Collections;
using System.IO;

namespace Oxide.Plugins
{
    [Info("CustomCommands", "Absolut", "1.0.1", ResourceId = 2158)]

    class CustomCommands : RustPlugin
    {
        static GameObject webObject;

        CustomCommandData ccData;
        private DynamicConfigFile CCData;

        static CCImages ccImage;

        string TitleColor = "<color=orange>";
        string MsgColor = "<color=#A9A9A9>";

        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private Dictionary<ulong, CommandCreation> cmdCreation = new Dictionary<ulong, CommandCreation>();
        private List<ulong> UIOpen = new List<ulong>();
        private List<ulong> Mouse = new List<ulong>();


        #region Server Hooks

        void Loaded()
        {
            CCData = Interface.Oxide.DataFileSystem.GetFile("CustomCommands_Data");
            lang.RegisterMessages(messages, this);
        }

        void Unload()
        {
            foreach (var entry in timers)
                entry.Value.Destroy();
            timers.Clear();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                DestroyPlayer(p);
            }
            SaveData();
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyPlayer(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                GetSendMSG(player, "CCInfo", configData.OptionsKeyBinding);
                InitializePlayer(player);
            }
        }

        private void InitializePlayer(BasePlayer player)
        {
            if (!ccData.PlayerCommands.ContainsKey(player.userID))
                ccData.PlayerCommands.Add(player.userID, new List<Command>());
            player.Command($"bind {configData.OptionsKeyBinding} \"UI_Mouse\"");
            player.Command("bind tab \"inventory.toggle;UI_DestroyMouse\"");
            player.Command("bind mouse1 \"+attack2;UI_DestroyMouse\"");

        }

        private void DestroyPlayer(BasePlayer player)
        {
            player.Command($"bind {configData.OptionsKeyBinding} \"\"");
            player.Command("bind tab \"inventory.toggle\"");
            player.Command("bind mouse1 \"+attack2\"");
            if (UIOpen.Contains(player.userID))
                UIOpen.Remove(player.userID);
            if (Mouse.Contains(player.userID))
                MousePanel(player);
            DestroyCreationPanel(player);
            DestroyCCPanel(player);
        }

        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            ccImage = webObject.AddComponent<CCImages>();
            ccImage.SetDataDir(this);
            LoadVariables();
            LoadData();
            timers.Add("info", timer.Once(900, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            SaveData();
            if (ccData.SavedImages == null || ccData.SavedImages.Count == 0)
                Getimages();
            else Refreshimages();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerInit(p);
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;
            bool isCreating = false;
            if (cmdCreation.ContainsKey(player.userID))
            {
                isCreating = true;
            }
            if (isCreating)
            {
                if (arg.Args[0] == "quit")
                {
                    DestroyCreationPanel(player);
                    GetSendMSG(player, "CanceledCmdCreation");
                    return true;
                }
                if (cmdCreation[player.userID].step == 1)
                {
                    cmdCreation[player.userID].cmd.cmd = string.Join("!@!", arg.Args);
                    CreateCommand(player, 99);
                }
            }
            return null;
        }

        #endregion

        #region Functions

        public void DestroyCCPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelCC);
        }

        public void DestroyCreationPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelCreation);
        }
        
        private string GetLang(string msg)
        {
            if (messages.ContainsKey(msg))
                return lang.GetMessage(msg, this);
            else return msg;
        }

        private void GetSendMSG(BasePlayer player, string message, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3, arg4);
            SendReply(player, TitleColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + MsgColor + msg + "</color>");
        }

        private string GetMSG(string message, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3, arg4);
            return msg;
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }

        #endregion

        #region UI Creation

        private string PanelCC = "PanelCC";
        private string PanelCreation = "PanelCreation";
        private string PanelMouse = "PanelMouse";

        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    new CuiElement().Parent,
                    panel
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer element, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                element.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer element, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            static public void CreateButton(ref CuiElementContainer element, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            static public void LoadImage(ref CuiElementContainer element, string panel, string png, string aMin, string aMax)
            {
                element.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer element, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                //if (configdata.DisableUI_FadeIn)
                //    fadein = 0;
                element.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"black", "0 0 0 1.0" },
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "1 1 1 0.3" },
            {"light", ".564 .564 .564 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"limegreen", "0.42 1.0 0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"white", "1 1 1 1" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"CSorange", "1.0 0.64 0.10 1.0" }
        };

        private Dictionary<string, string> TextColors = new Dictionary<string, string>
        {
            {"limegreen", "<color=#6fff00>" }
        };

        #endregion

        #region UI Panels

        void CCPanel(BasePlayer player, string mode = "norm")
        {
            CuiHelper.DestroyUi(player, PanelCC);
            FreeMouse(player);
            var i = 0;
            var command = "";
            var element = UI.CreateElementContainer(PanelCC, "0 0 0 0", "0.95 0.3", "1.0 0.9");
            foreach (var entry in ccData.PlayerCommands[player.userID])
            {
                var pos = CmdButtonPos(i);
                if (mode == "norm")
                    command = $"any {i}";
                else if (mode == "edit")
                    command = $"UI_RemoveCommand {i}";
                UI.LoadImage(ref element, PanelCC, ccData.SavedImages["PurpleLongButton"].ToString(), $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateButton(ref element, PanelCC, "0 0 0 0", entry.cmd, 14, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", command, TextAnchor.MiddleCenter);
                i++;
            }
            if (ccData.PlayerCommands[player.userID].Count() < 17 && mode == "norm")
            {
                UI.LoadImage(ref element, PanelCC, ccData.SavedImages["GreenSquareButton"].ToString(), "0.01 0.11", "0.99 0.16");
                UI.CreateButton(ref element, PanelCC, "0 0 0 0", GetLang("Add"), 14, "0.01 0.11", "0.99 0.16", $"UI_CreateCommand", TextAnchor.MiddleCenter);
            }
            if (ccData.PlayerCommands[player.userID].Count() > 0 && mode == "norm")
            {
                UI.LoadImage(ref element, PanelCC, ccData.SavedImages["RedSquareButton"].ToString(), "0.01 0.05", "0.99 0.1");
                UI.CreateButton(ref element, PanelCC, "0 0 0 0", GetLang("Remove"), 14, "0.01 0.05", "0.99 0.1", $"UI_CCPanel yes", TextAnchor.MiddleCenter);
            }
            if (mode == "edit")
            {
                UI.LoadImage(ref element, PanelCC, ccData.SavedImages["RedSquareButton"].ToString(), "0.01 0.05", "0.99 0.1");
                UI.CreateButton(ref element, PanelCC, "0 0 0 0", GetLang("ExitEraseMode"), 14, "0.01 0.05", "0.99 0.1", $"UI_CCPanel no", TextAnchor.MiddleCenter);
            }
            UI.CreateButton(ref element, PanelCC, UIColors["red"], GetLang("Close"), 10, "0.01 0.01", "0.99 0.04", $"UI_DestroyCC", TextAnchor.MiddleCenter);
            //if (isAuth(player))
            //{
            //    UI.LoadImage(ref element, PanelPlayer, factionData.UIElements["PurpleLongButton"].ToString(), "0.52 0.5", "1.0 1.0");
            //    UI.CreateButton(ref element, PanelPlayer, "0 0 0 0", GetLang("AdminOptions"), 14, "0.52 0.5", "1.0 1.0", $"UI_TryPanel admin", TextAnchor.MiddleCenter);
            //}
            CuiHelper.AddUi(player, element);
        }

        private void CreateCommand(BasePlayer player, int step = 0)
        {
            CuiHelper.DestroyUi(player, PanelCreation);
            CuiHelper.DestroyUi(player, PanelMouse);
            if (Mouse.Contains(player.userID))
                Mouse.Remove(player.userID);
            var element = UI.CreateElementContainer(PanelCreation, "0 0 0 0", "0.3 0.3", "0.7 0.9");
            switch (step)
            {
                case 0:
                    if (cmdCreation.ContainsKey(player.userID))
                        cmdCreation.Remove(player.userID);
                    cmdCreation.Add(player.userID, new CommandCreation());
                    cmdCreation[player.userID].cmd = new Command();
                    UI.CreatePanel(ref element, PanelCreation, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);

                    UI.LoadImage(ref element, PanelCreation, ccData.SavedImages["OrangeSquareButton"].ToString(), "0.15 0.4", "0.45 0.6");
                    UI.CreateButton(ref element, PanelCreation, "0 0 0 0", GetLang("CONSOLE"), 16, "0.15 0.4", "0.45 0.6", $"UI_SetType console");

                UI.LoadImage(ref element, PanelCreation, ccData.SavedImages["BlueSquareButton"].ToString(), "0.55 0.4", "0.85 0.6");
                UI.CreateButton(ref element, PanelCreation, "0 0 0 0", GetLang("CHAT"), 16, "0.55 0.4", "0.85 0.6", $"UI_SetType chat");

                    break;
                case 1:
                    if (cmdCreation[player.userID].cmd.type == "chat")
                        UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], GetMSG("ProvideAChatCommand"), 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    else if (cmdCreation[player.userID].cmd.type == "console")
                        UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], GetMSG("ProvideAConsoleCommand"), 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                default:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    element = UI.CreateElementContainer(PanelCreation, "0 0 0 0", "0.3 0.3", "0.7 0.5");
                    UI.CreatePanel(ref element, PanelCreation, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], GetMSG("NewCMDInfo", cmdCreation[player.userID].cmd.type.ToUpper(), cmdCreation[player.userID].cmd.cmd), 20, "0.05 .5", ".95 1.0");
                    UI.CreateButton(ref element, PanelCreation, UIColors["buttonbg"], GetLang("SaveCommand"), 18, "0.2 0.05", "0.4 0.4", $"UI_SaveCommand", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelCreation, UIColors["buttonred"], GetLang("CancelCommand"), 18, "0.6 0.05", "0.8 0.4", $"UI_CancelCommand");
                    break;
            }
            CuiHelper.AddUi(player, element);
        }

        void FreeMouse(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelMouse);
            if (!Mouse.Contains(player.userID))
            {
                Mouse.Add(player.userID);
            }
            var element = UI.CreateElementContainer(PanelMouse, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
            CuiHelper.AddUi(player, element);
        }



        #endregion

        #region UI Calculations

        private float[] CmdButtonPos(int number)
        {
            Vector2 position = new Vector2(0.01f, 0.94f);
            Vector2 dimensions = new Vector2(0.98f, 0.05f);
            float offsetY = 0;
            float offsetX = 0;
            offsetY = (-0.001f - dimensions.y) * number;
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #endregion

        #region UI Commands

        [ConsoleCommand("UI_Mouse")]
        private void cmdOpenFactions(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            MousePanel(player);
        }

        private void MousePanel(BasePlayer player)
        {
            if (Mouse.Contains(player.userID))
            {
                CuiHelper.DestroyUi(player, PanelCreation);
                CuiHelper.DestroyUi(player, PanelMouse);
                Mouse.Remove(player.userID);
            }
            else
            {              
                FreeMouse(player);
            }

        }

        [ConsoleCommand("UI_DestroyMouse")]
        private void cmdUI_DestroyMouse(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (Mouse.Contains(player.userID))
                Mouse.Remove(player.userID);
            CuiHelper.DestroyUi(player, PanelMouse);
        }

        [ConsoleCommand("UI_DestroyCC")]
        private void cmdUI_DestroyCC(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (UIOpen.Contains(player.userID))
                UIOpen.Remove(player.userID);
            DestroyCCPanel(player);
            if (Mouse.Contains(player.userID))
                Mouse.Remove(player.userID);
            CuiHelper.DestroyUi(player, PanelMouse);
        }


        [ChatCommand("cc")]
        private void cmdcc(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                if (UIOpen.Contains(player.userID))
                {
                    UIOpen.Remove(player.userID);
                    DestroyCCPanel(player);
                }
                else
                {
                    UIOpen.Add(player.userID);
                    CCPanel(player);
                }
                return;
            }
        }

        [ConsoleCommand("any")]
        private void cmdchat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var i = 0;
            int num = 0;
            if (!int.TryParse(arg.Args[0], out num)) return;
            foreach (var entry in ccData.PlayerCommands[player.userID])
            {
                if (i == num)
                {
                    var cmd = entry.cmd.Replace("!@!"," ");
                    if (entry.type == "chat")
                    {
                        rust.RunClientCommand(player, $"chat.say",$"/{cmd}");
                    }
                    else if (entry.type == "console")
                    {
                        rust.RunClientCommand(player, $"{cmd}");
                    }
                }
                else
                {
                    i++;
                        continue;
                }
            }
        }

        [ConsoleCommand("UI_CCPanel")]
        private void cmdUI_CCPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args[0] == "yes")
                CCPanel(player, "edit");
            else if (arg.Args[0] == "no")
                CCPanel(player);
        }

        [ConsoleCommand("UI_RemoveCommand")]
        private void cmdUI_RemoveCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int index = Convert.ToInt16(arg.Args[0]);
            var i = 0;
            foreach (var entry in ccData.PlayerCommands[player.userID])
                if (i == index)
                {
                    ccData.PlayerCommands[player.userID].Remove(entry);
                    break;
                }
                else i++;
            CCPanel(player, "edit");
        }
       
        [ConsoleCommand("UI_CreateCommand")]
        private void cmdUI_CreateCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CreateCommand(player);
        }

        [ConsoleCommand("UI_SetType")]
        private void cmdUI_SetType(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            cmdCreation[player.userID].cmd.type = arg.Args[0];
            cmdCreation[player.userID].step = 1;
            CreateCommand(player, 1);
        }

        [ConsoleCommand("UI_SaveCommand")]
        private void cmdUI_SaveCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ccData.PlayerCommands[player.userID].Add(cmdCreation[player.userID].cmd);
            cmdCreation.Remove(player.userID);
            DestroyCreationPanel(player);
            GetSendMSG(player, "NewCommand");
            CCPanel(player);
        }

        [ConsoleCommand("UI_CancelCommand")]
        private void cmdUI_CancelCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (cmdCreation.ContainsKey(player.userID))
                cmdCreation.Remove(player.userID);
            DestroyCreationPanel(player);
            GetSendMSG(player, "CanceledCmdCreation");
        }

        #endregion

        #region Timers

        private void SaveLoop()
        {
            if (timers.ContainsKey("save"))
            {
                timers["save"].Destroy();
                timers.Remove("save");
            }
            SaveData();
            timers.Add("save", timer.Once(600, () => SaveLoop()));
        }

        private void InfoLoop()
        {
            if (timers.ContainsKey("info"))
            {
                timers["info"].Destroy();
                timers.Remove("info");
            }
            if (configData.InfoInterval == 0) return;
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                GetSendMSG(p, "CCInfo", configData.OptionsKeyBinding);
            }
            timers.Add("info", timer.Once(configData.InfoInterval * 60, () => InfoLoop()));
        }

        private void SetBoxFullNotification(string ID)
        {
            timers.Add(ID, timer.Once(5 * 60, () => timers.Remove(ID)));
        }

        #endregion

        #region Classes
        class CustomCommandData
        {
            public Dictionary<string, uint> SavedImages = new Dictionary<string, uint>();
            public Dictionary<ulong, List<Command>> PlayerCommands = new Dictionary<ulong, List<Command>>();

        }

        class Command
        {
            public string cmd;
            //public object[] Params;
            public string type;
        }

        class CommandCreation
        {
            public int step = 0;
            public Command cmd;
        }

        #endregion

        #region Unity WWW
        class QueueImage
        {
            public string url;
            public string name;
            public QueueImage(string st, string ur)
            {
                name = st;
                url = ur;
            }
        }

        class CCImages : MonoBehaviour
        {
            CustomCommands filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueImage> QueueList = new List<QueueImage>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(CustomCommands cc) => filehandler = cc;
            public void Add(string name, string url)
            {
                QueueList.Add(new QueueImage(name, url));
                if (activeLoads < MaxActiveLoads) Next();
            }

            void Next()
            {
                activeLoads++;
                var qi = QueueList[0];
                QueueList.RemoveAt(0);
                var www = new WWW(qi.url);
                StartCoroutine(WaitForRequest(www, qi));
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(WWW www, QueueImage info)
            {
                yield return www;

                if (www.error == null)
                {
                    if (!filehandler.ccData.SavedImages.ContainsKey(info.name))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.ccData.SavedImages.Add(info.name, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }

        [ConsoleCommand("getUIimages")]
        private void cmdgetimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Getimages();
            }
        }

        private void Getimages()
        {
            ccData.SavedImages.Clear();
                    foreach (var item in urls)
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            ccImage.Add(item.Key, item.Value);
                        }
                    }
            Puts(GetLang("ImgReload"));
        }

        [ConsoleCommand("checkUIimages")]
        private void cmdrefreshimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Refreshimages();
            }
        }

        private void Refreshimages()
        {

            foreach (var item in urls)
                if (!ccData.SavedImages.ContainsKey(item.Key))
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        ccImage.Add(item.Key, item.Value);
                    }
            Puts(GetLang("ImgRefresh"));
        }
        #endregion

        #region Custom Commands Data Management

        private Dictionary<string, string> urls = new Dictionary<string, string>
        {
            { "FIRST", "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/simple-black-square-icons-arrows/126517-simple-black-square-icon-arrows-double-arrowhead-left.png" },
            { "BACK", "https://image.freepik.com/free-icon/back-left-arrow-in-square-button_318-76403.png" },
            { "NEXT", "https://image.freepik.com/free-icon/right-arrow-square-button-outline_318-76302.png" },
            { "LAST", "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/matte-white-square-icons-arrows/124577-matte-white-square-icon-arrows-double-arrowhead-right.png" },
            { "BlueLongButton", "https://pixabay.com/static/uploads/photo/2016/01/23/11/41/button-1157299_960_720.png" },
            { "RedLongButton", "https://pixabay.com/static/uploads/photo/2016/01/23/11/42/button-1157301_960_720.png" },
            { "BlackLongButton", "https://pixabay.com/static/uploads/photo/2016/01/23/11/26/button-1157269_960_720.png" },
            { "GreenLongButton", "https://pixabay.com/static/uploads/photo/2015/07/25/08/03/the-button-859349_960_720.png" },
            { "PurpleLongButton", "https://pixabay.com/static/uploads/photo/2015/07/25/07/55/the-button-859343_960_720.png" },
            { "GreenSquareButton", "http://www.pd4pic.com/images/libya-flag-country-nationality-square-button.png" },
            { "RedSquareButton", "https://openclipart.org/image/2400px/svg_to_png/78601/Red-button.png" },
            { "BlueSquareButton", "http://downloadicons.net/sites/default/files/yellow-blue-crystal-icon-style-rectangular-button-32172.png" },
            { "OrangeSquareButton", "http://downloadicons.net/sites/default/files/orange-button,-square-icons-32177.png" },
            
            
        };


        void SaveData()
        {
            CCData.WriteObject(ccData);
        }

        void LoadData()
        {
            try
            {
                ccData = CCData.ReadObject<CustomCommandData>();
            }
            catch
            {
                Puts("Couldn't load the CustomCommands Data, creating a new datafile");
                ccData = new CustomCommandData();
            }
        }

        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int InfoInterval { get; set; }
            public string OptionsKeyBinding { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                InfoInterval = 15,
                OptionsKeyBinding = "h",
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "CustomCommands: " },
            {"CCInfo", "This server is running CustomCommands. Type /cc to open your personal CC Menu! Pressing '{0}' enables a player to enter 'Mouse Look Mode' to click buttons."},
            {"Next", "Next" },
            {"Back", "Back" },
            {"First", "First" },
            {"Last", "Last" },
            {"Close", "Close"},
            {"Quit", "Quit"},
            {"ImgReload", "Images have been wiped and reloaded!" },
            {"ImgRefresh", "Images have been refreshed !" },
            {"Delete", "Delete" },
            {"Remove", "Remove" },
            {"SelectaCMDType", "Please Select a Command Type" },
            {"CHAT" , "CHAT Command" },
            {"CONSOLE" , "CONSOLE Command" },
            {"NewCMDInfo", "New Command Info:\nCommand Type: {0}\nCommand: {1}" },
            {"SaveCommand", "Save Command?" },
            {"CancelCommand", "Cancel Command?" },
            {"ProvideAChatCommand", "Please Provide a Chat Command but leave the '/' out. For example, to create a Chat Command for this plugin type cc to create a button that opens /cc" },
            {"ProvideAConsoleCommand", "Please Provide a Console Command" },
            {"CanceledCmdCreation", "You have successfully cancelled Command Creation " },
            {"NewCommand", "You have successfully created a new command!" },
            {"ExitEraseMode", "Exit Erase Mode" }
        };
        #endregion
    }
}
