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
    [Info("AbsolutGifts", "Absolut", "1.0.0", ResourceId = 999999)]

    class AbsolutGifts : RustPlugin
    {
        static GameObject webObject;

        GiftData agdata;
        private DynamicConfigFile AGData;

        static UnityImages uImage;

        int GlobalTime = 0;

        string TitleColor = "<color=orange>";
        string MsgColor = "<color=#A9A9A9>";

        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private Dictionary<ulong, GiftCreation> giftprep = new Dictionary<ulong, GiftCreation>();
        private Dictionary<ulong, int> TimeSinceLastGift = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> NextGift = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> CurrentGift = new Dictionary<ulong, int>();
        private Dictionary<ulong, List<Gift>> Objective = new Dictionary<ulong, List<Gift>>();
        private Dictionary<ulong, Vector3> AFK = new Dictionary<ulong, Vector3>();

        #region Server Hooks

        void Loaded()
        {
            AGData = Interface.Oxide.DataFileSystem.GetFile("AbsolutGifts_Data");
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

        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            uImage = webObject.AddComponent<UnityImages>();
            uImage.SetDataDir(this);
            LoadVariables();
            LoadData();
            timers.Add("info", timer.Once(900, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            SaveData();
            if (agdata.SavedImages == null || agdata.SavedImages.Count == 0)
                Getimages();
            else Refreshimages();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerInit(p);
            timer.Once(60, () => ChangeGlobalTime());
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyPlayer(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                GetSendMSG(player, "AGInfo");
            }
            InitializePlayer(player);
        }

        private void InitializePlayer(BasePlayer player)
        {
            if (!agdata.Players.ContainsKey(player.userID))
                agdata.Players.Add(player.userID, new playerdata { PlayerTime = 0, ReceivedGifts = new List<int>() });
            if (agdata.Players.ContainsKey(player.userID))
            {
                //Puts("Contains Player");
                if (GrabCurrentTime() > agdata.Players[player.userID].Lastconnection + 86400)
                {
                    //Puts("Time is Greater");
                    agdata.Players[player.userID].ReceivedGifts.Clear();
                    agdata.Players[player.userID].PlayerTime = 0;
                    SaveData();
                }
                else
                {
                    //Puts("Time is Less");
                    double timeremaining = (agdata.Players[player.userID].Lastconnection + 86400) - GrabCurrentTime();
                    float time = (float)timeremaining;
                    timers.Add(player.userID.ToString(), timer.Once(time, () => ResetGifts(player)));
                    //Puts(time.ToString());
                }
            }
            if (!TimeSinceLastGift.ContainsKey(player.userID))
                TimeSinceLastGift.Add(player.userID, 0);
            TimeSinceLastGift[player.userID] = agdata.Players[player.userID].PlayerTime;
            //Puts($"Time since last gift... imported for {player.displayName} : {TimeSinceLastGift[player.userID]}");
            InitializeGiftObjective(player);
        }

        private void DestroyPlayer(BasePlayer player)
        {
            if (!agdata.Players.ContainsKey(player.userID))
                agdata.Players.Add(player.userID, new playerdata { PlayerTime = 0, ReceivedGifts = new List<int>()});
            agdata.Players[player.userID].PlayerTime += TimeSinceLastGift[player.userID];
            agdata.Players[player.userID].Lastconnection = GrabCurrentTime();
            if (timers.ContainsKey(player.userID.ToString()))
                timers.Remove(player.userID.ToString());
            TimeSinceLastGift.Remove(player.userID);
            NextGift.Remove(player.userID);
            Objective.Remove(player.userID);
            DestroyGiftPanel(player);
        }

        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        #endregion

        #region Player Hooks

        #endregion

        #region Functions

        private void CancelGiftCreation(BasePlayer player)
        {
            DestroyGiftPanel(player);
            if (giftprep.ContainsKey(player.userID))
                giftprep.Remove(player.userID);
            GetSendMSG(player, "GiftCreationCanceled");
            AdminPanel(player);
        }

        public void DestroyGiftPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelGift);
        }

        void ChangeGlobalTime()
        {
            GlobalTime++;
            //Puts(GlobalTime.ToString());
            timer.Once(1 * 60, () => ChangeGlobalTime());
            CheckPlayers();
        }

        private void CheckPlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (NextGift.ContainsKey(player.userID))
                {
                    if (configData.NoAFK)
                        if (CheckAFK(player)) continue;
                    NextGift[player.userID]--;
                    TimeSinceLastGift[player.userID]++;
                    //Puts($"Gift Requirement time remaining for {player.displayName} - {NextGift[player.userID].ToString()}");
                    if (NextGift[player.userID] < 1)
                    {
                        GiveGift(player);
                    }
                }
                else InitializeGiftObjective(player);
        }

        bool CheckAFK(BasePlayer player)
        {
            if (player?.transform.position == null)
                return false;
            if (AFK.ContainsKey(player.userID))
            {
                if (AFK[player.userID] == player.transform.position)
                    return true;
            }
            return false;
        }

        private void InitializeGiftObjective(BasePlayer player)
        {
            if (!agdata.Players.ContainsKey(player.userID))
                agdata.Players.Add(player.userID, new playerdata { PlayerTime = 0, ReceivedGifts = new List<int>() });
            foreach (var entry in agdata.Gifts.Where(gift => !agdata.Players[player.userID].ReceivedGifts.Contains(gift.Key)).OrderBy(kvp => kvp.Key))
            {
                //Puts("New Objective {entry.Key.ToString()}");
                if (Objective.ContainsKey(player.userID))
                    Objective.Remove(player.userID);
                Objective.Add(player.userID, entry.Value);
                if (NextGift.ContainsKey(player.userID))
                    NextGift.Remove(player.userID);
                NextGift.Add(player.userID, (entry.Key - TimeSinceLastGift[player.userID]));
                if (CurrentGift.ContainsKey(player.userID))
                    CurrentGift.Remove(player.userID);
                CurrentGift.Add(player.userID, entry.Key);
                break;
            }
        }

        private void ResetGifts(BasePlayer player)
        {

            TimeSinceLastGift[player.userID] = 0;
            agdata.Players[player.userID].ReceivedGifts.Clear();
            Objective.Remove(player.userID);
            NextGift.Remove(player.userID);
            CurrentGift.Remove(player.userID);
            InitializeGiftObjective(player);
        }

        private void GiveGift(BasePlayer player)
        {
            foreach (var entry in Objective[player.userID])
            {
                Item item = ItemManager.CreateByItemID(entry.ID, entry.amount);
                if (item != null)
                {
                    item.MoveToContainer(player.inventory.containerMain);
                    GetSendMSG(player, "NewGiftGiven", item.amount.ToString(), item.info.shortname);
                    //Puts($"Gave Gift: {item.info.shortname}");
                    TimeSinceLastGift[player.userID] = 0;
                    agdata.Players[player.userID].ReceivedGifts.Add(CurrentGift[player.userID]);
                }
            }
            Objective.Remove(player.userID);
            NextGift.Remove(player.userID);
            CurrentGift.Remove(player.userID);
            InitializeGiftObjective(player);
        }

        private void CreateNumberPadButton(ref CuiElementContainer container, string panelName, int i, int number, string command)
        {
            var pos = CalcNumButtonPos(i);
            UI.CreateButton(ref container, panelName, UIColors["buttonbg"], number.ToString(), 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"{command} {number}");
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

        private string PanelGift = "PanelGift";

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

        void GiftPanel(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelGift);
            var element = UI.CreateElementContainer(PanelGift, "0 0 0 0", "0.2 0.15", "0.8 0.85", true);
            UI.CreatePanel(ref element, PanelGift, UIColors["dark"], "0. 0", "1 1");
            var i = 0;
            double count = agdata.Gifts.Count();   
            int entriesallowed = 4;
            double remainingentries = count - (page * (entriesallowed - 1));
            double totalpages = (Math.Floor(count / (entriesallowed - 1)));

            if (isAuth(player))
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("AdminPanel"), 12, "0.03 0.02", "0.13 0.075", $"UI_AdminsPanel");
            }

            if (page < totalpages - 1)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Last"), 18, "0.8 0.02", "0.85 0.075", $"UI_GiftMenu {totalpages}");
            }
            if (remainingentries > entriesallowed)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Next"), 18, "0.74 0.02", "0.79 0.075", $"UI_GiftMenu {page + 1}");
            }
            if (page > 0)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Back"), 18, "0.68 0.02", "0.73 0.075", $"UI_GiftMenu {page - 1}");
            }
            if (page > 1)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("First"), 18, "0.62 0.02", "0.67 0.075", $"UI_GiftMenu {0}");
            }
            int n = 0;
            int shownentries = page * entriesallowed;
            List<int> completed = new List<int>();
            if (agdata.Players.ContainsKey(player.userID))
                foreach (var entry in agdata.Players[player.userID].ReceivedGifts)
                    completed.Add(entry);
            foreach (var entry in agdata.Gifts.OrderBy(kvp => kvp.Key))
            {
                i++;
                if (i < shownentries + 1) continue;
                else if (i <= shownentries + entriesallowed)
                {
                    CreateGiftMenuEntry(ref element, PanelGift, entry.Key, n, completed, player);
                    n++;
                }
            }

            UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Close"), 16, "0.87 0.02", "0.97 0.075", $"UI_DestroyGiftPanel");
            CuiHelper.AddUi(player, element);
        }

        private void CreateGifts(BasePlayer player, int step = 0, int page = 0)
        {
            var i = 0;
            string image = "";
            var element = UI.CreateElementContainer(PanelGift, "0 0 0 0", "0.3 0.3", "0.7 0.9");
            switch (step)
            {
                case 0:
                    CuiHelper.DestroyUi(player, PanelGift);
                    if (giftprep.ContainsKey(player.userID))
                        giftprep.Remove(player.userID);
                    giftprep.Add(player.userID, new GiftCreation());
                    //UI.CreateLabel(ref element, PanelGift, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("SelectGiftTimer")}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    NumberPad(player, "UI_SelectTime", "SelectTime");
                    return;
                case 1:
                    CuiHelper.DestroyUi(player, PanelGift);
                    double count = 0;
                    foreach (var item in ItemManager.itemList)
                        count++;
                    UI.CreatePanel(ref element, PanelGift, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GetMSG("SelectGift", giftprep[player.userID].time.ToString()), 20, "0.05 .95", ".95 1", TextAnchor.MiddleCenter);
                    double entriesallowed = 30;
                    double remainingentries = count - (page * (entriesallowed - 1));
                    double totalpages = (Math.Floor(count / (entriesallowed - 1)));
                    {
                        if (page <= totalpages - 1)
                        {
                            UI.LoadImage(ref element, PanelGift, agdata.SavedImages["LAST"].ToString(), "0.8 0.02", "0.85 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.8 0.02", "0.85 0.075", $"UI_CreateGifts {1} {totalpages}");
                        }
                        if (remainingentries > entriesallowed)
                        {
                            UI.LoadImage(ref element, PanelGift, agdata.SavedImages["NEXT"].ToString(), "0.74 0.02", "0.79 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.74 0.02", "0.79 0.075", $"UI_CreateGifts {1} {page + 1}");
                        }
                        if (page > 0)
                        {
                            UI.LoadImage(ref element, PanelGift, agdata.SavedImages["BACK"].ToString(), "0.68 0.02", "0.73 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.68 0.02", "0.73 0.075", $"UI_CreateGifts {1} {page - 1}");
                        }
                        if (page > 1)
                        {
                            UI.LoadImage(ref element, PanelGift, agdata.SavedImages["FIRST"].ToString(), "0.62 0.02", "0.67 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.62 0.02", "0.67 0.075", $"UI_CreateGifts {1} {0}");
                        }
                    }
                    int n = 0;
                    var pos = CalcButtonPos(n);
                    double shownentries = page * entriesallowed;
                    //if (page == 0)
                    //{
                    //    if (configData.ServerRewards == true && ServerRewards)
                    //    {
                    //        UI.LoadImage(ref element, PanelGift, imgData.SavedImages[Category.Money]["SR"][0].ToString(), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                    //        UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectpriceItemshortname SR");
                    //        n++;
                    //        i++;
                    //    }
                    //}
                    foreach (var item in ItemManager.itemList)
                    {
                        i++;
                        image = agdata.SavedImages["MISSINGIMG"].ToString();
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            pos = CalcButtonPos(n);
                            if (agdata.SavedImages.ContainsKey(item.shortname))
                                image = agdata.SavedImages[item.shortname].ToString();
                            UI.LoadImage(ref element, PanelGift, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            if (image == agdata.SavedImages["MISSINGIMG"].ToString()) UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], item.shortname, 14, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectGift {item.itemid}");
                            n++;
                        }
                    }
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Quit"), 16, "0.03 0.02", "0.13 0.075", $"UI_AdminsPanel");
                    break;
                default:
                    CuiHelper.DestroyUi(player, PanelGift);
                    UI.CreatePanel(ref element, PanelGift, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GetMSG("NewGiftInfo", giftprep[player.userID].time.ToString()), 20, "0.05 .8", ".95 .95");
                    string GiftDetails = "";
                    foreach (var entry in giftprep[player.userID].gifts)
                        GiftDetails += $"{GetMSG("GiftDetails", ItemManager.FindItemDefinition(entry.ID).shortname, entry.amount.ToString())}\n";
                    UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GiftDetails, 20, "0.1 0.16", "0.9 0.75", TextAnchor.MiddleLeft);
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonbg"], GetLang("FinalizeGift"), 18, "0.2 0.05", "0.4 0.15", $"UI_FinalizeGift", TextAnchor.MiddleCenter);
                    if (giftprep[player.userID].gifts.Count() < 11)
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonbg"], GetLang("AddToGift"), 18, "0.401 0.05", "0.599 0.15", $"UI_CreateGifts {1} {0}", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Cancel"), 18, "0.6 0.05", "0.8 0.15", $"UI_CancelGiftCreation");
                    break;
            }
            CuiHelper.AddUi(player, element);
        }

        void ManageGifts(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelGift);
            var element = UI.CreateElementContainer(PanelGift, "0 0 0 0", "0.2 0.15", "0.8 0.85", true);
            UI.CreatePanel(ref element, PanelGift, UIColors["dark"], "0. 0", "1 1");
            var i = 0;
            double count = agdata.Gifts.Count();
            int entriesallowed = 4;
            double remainingentries = count - (page * (entriesallowed - 1));
            double totalpages = (Math.Floor(count / (entriesallowed - 1)));

            if (page < totalpages - 1)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Last"), 12, "0.8 0.02", "0.85 0.075", $"UI_ManageGifts {totalpages}");
            }
            if (remainingentries > entriesallowed)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Next"), 12, "0.74 0.02", "0.79 0.075", $"UI_ManageGifts {page + 1}");
            }
            if (page > 0)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Back"), 12, "0.68 0.02", "0.73 0.075", $"UI_ManageGifts {page - 1}");
            }
            if (page > 1)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("First"), 12, "0.62 0.02", "0.67 0.075", $"UI_ManageGifts {0}");
            }
            int shownentries = page * entriesallowed;
            int n = 0;
            foreach (var entry in agdata.Gifts.OrderBy(kvp => kvp.Key))
            {
                i++;
                if (i < shownentries + 1) continue;
                else if (i <= shownentries + entriesallowed)
                {
                    CreateGiftMenuEntry(ref element, PanelGift, entry.Key, n, null);
                    n++;
                }
            }
            UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Back"), 16, "0.87 0.02", "0.97 0.075", $"UI_AdminsPanel");
            CuiHelper.AddUi(player, element);
        }

        private void CreateGiftMenuEntry(ref CuiElementContainer container, string panelName, int ID, int num, List<int> completed, BasePlayer player = null)
        {
            var pos = GiftPos(num);
            var i = 0;
            var currentID = 0;
            if (player != null)
            {
                if (CurrentGift.ContainsKey(player.userID))
                    currentID = CurrentGift[player.userID];
            }
            if (currentID == ID)
            {
                UI.CreatePanel(ref container, panelName, UIColors["yellow"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("GiftTitleInProgress", ID.ToString()), 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
            }
            else if (completed != null)
            {
                if (completed.Contains(ID))
                {
                    UI.CreatePanel(ref container, panelName, UIColors["green"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                    UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("GiftTitleCompleted", ID.ToString()), 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
                }
                else
                {
                    UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                    UI.CreateLabel(ref container, panelName, UIColors["limegreen"], GetMSG("GiftTitle", ID.ToString()), 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
                }
            }
            else
            {
                UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateLabel(ref container, panelName, UIColors["limegreen"], GetMSG("GiftTitle", ID.ToString()), 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
            }

                var xmin = pos[0] + 0.001f;
            var xmax = pos[0] + 0.1f;
            var ymin = pos[3] - 0.135f;
            var ymax = pos[3] - 0.05f;
            foreach (var entry in agdata.Gifts[ID])
            {
                var item = ItemManager.CreateByItemID(entry.ID);
                if (item != null)
                {
                    if (i > 0 && i < 4)
                    {
                        xmin += 0.12f;
                        xmax += 0.12f;
                    }
                    if (i == 4)
                    {
                        xmin = pos[0] + 0.001f;
                        xmax = pos[0] + 0.1f;
                        ymin -= .13f;
                        ymax -= .13f;
                    }
                    if (i > 4 && i < 8)
                    {
                        xmin += 0.12f;
                        xmax += 0.12f;
                    }
                    if (i == 8)
                    {
                        xmin = pos[0] + 0.001f;
                        xmax = pos[0] + 0.1f;
                        ymin -= .13f;
                        ymax -= .13f;
                    }
                    if (i > 8 && i < 10)
                    {
                        xmin += 0.12f;
                        xmax += 0.12f;
                    }

                    var picture = agdata.SavedImages["MISSINGIMG"].ToString();
                    if (agdata.SavedImages.ContainsKey(item.info.shortname))
                        picture = agdata.SavedImages[item.info.shortname].ToString();
                    UI.LoadImage(ref container, PanelGift, picture, $"{xmin} {ymin}", $"{xmax} {ymax}");
                    if (picture == agdata.SavedImages["MISSINGIMG"].ToString()) UI.CreateLabel(ref container, PanelGift, UIColors["limegreen"], item.info.shortname, 12, $"{xmin} {ymin + 0.05f}", $"{xmax} {ymax + 0.05f}", TextAnchor.MiddleCenter);
                    UI.CreateLabel(ref container, PanelGift, UIColors["limegreen"], entry.amount.ToString(), 16, $"{xmin} {ymin - 0.025f}", $"{xmax} {ymax - 0.025f}", TextAnchor.MiddleCenter);
                    i++;
                }
            }
            if (player == null)
                UI.CreateButton(ref container, panelName, UIColors["buttonred"], GetLang("Delete"), 16, $"{pos[2] - .125f} {pos[1] + .01f}", $"{pos[2] - .01f} {pos[1] + .125f}", $"UI_RemoveGift {ID}");
        }

        private void AdminPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelGift);
            var i = 0;
            var element = UI.CreateElementContainer(PanelGift, UIColors["dark"], "0.3 0.3", "0.7 0.6", true);
            UI.CreatePanel(ref element, PanelGift, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelGift, MsgColor, GetLang("AdminPanel"), 75, "0.05 0", "0.95 1");
            var loc = CalcButtonPos(i);
            UI.CreateButton(ref element, PanelGift, UIColors["CSorange"], GetLang("CreateGift"), 10, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_CreateGifts {0} {0}"); i++;
            loc = CalcButtonPos(i);
            UI.CreateButton(ref element, PanelGift, UIColors["CSorange"], GetLang("ManageGifts"), 10, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_ManageGifts {0} {0}"); i++;
            //loc = CalcButtonPos(i);
            //UI.CreateButton(ref element, PanelGift, UIColors["CSorange"], GetLang("BlackListingREMOVE"), 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_BlackList {0} remove"); i++;
            UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Back"), 10, "0.03 0.02", "0.13 0.075", $"UI_GiftMenu {0}");
            CuiHelper.AddUi(player, element);
        }

        private void NumberPad(BasePlayer player, string cmd, string title)
        {
            CuiHelper.DestroyUi(player, PanelGift);
            var element = UI.CreateElementContainer(PanelGift, UIColors["dark"], "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelGift, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GetLang(title), 16, "0.1 0.85", "0.9 .98", TextAnchor.UpperCenter);
            var n = 1;
            var i = 0;
            if (title == "SelectTime")
            {
                while (n < 20)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n++;
                }
                while (n >= 20 && n < 60)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 5;
                }
                while (n >= 60 && n < 240)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 30;
                }
                while (n >= 240 && n <= 1470)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 60;
                }
            }
            else if (title == "SelectAmount")
            {
                while (n < 10)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n++;
                }
                while (n >= 10 && n < 25)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 5;
                }
                while (n >= 25 && n < 200)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 25;
                }
                while (n >= 200 && n <= 950)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 50;
                }
                while (n >= 1000 && n <= 10000)
                {
                    CreateNumberPadButton(ref element, PanelGift, i, n, cmd); i++; n += 500;
                }
            }
            UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("Quit"), 10, "0.03 0.02", "0.13 0.075", $"UI_AdminsPanel");
            CuiHelper.AddUi(player, element);
        }

        #endregion

        #region UI Calculations

        private float[] GiftPos(int number)
        {
            Vector2 position = new Vector2(0.03f, 0.525f);
            Vector2 dimensions = new Vector2(0.46f, 0.425f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 2)
            {
                offsetY = (-0.01f - dimensions.y) * number;
            }
            if (number > 1 && number < 4)
            {
                offsetX = dimensions.x + 0.005f;
                offsetY = (-0.01f - dimensions.y) * (number - 2);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] FilterButton(int number)
        {
            Vector2 position = new Vector2(0.01f, 0.0f);
            Vector2 dimensions = new Vector2(0.08f, 0.04f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 2)
            {
                offsetY = (0.005f + dimensions.y) * number;
            }
            if (number > 1 && number < 4)
            {
                offsetX = (0.01f + dimensions.x) * 1;
                offsetY = (0.005f + dimensions.y) * (number - 2);
            }
            if (number > 3 && number < 6)
            {
                offsetX = (0.01f + dimensions.x) * 2;
                offsetY = (0.005f + dimensions.y) * (number - 4);
            }
            if (number > 5 && number < 8)
            {
                offsetX = (0.01f + dimensions.x) * 3;
                offsetY = (0.005f + dimensions.y) * (number - 6);
            }
            if (number > 7 && number < 10)
            {
                offsetX = (0.01f + dimensions.x) * 4;
                offsetY = (0.005f + dimensions.y) * (number - 8);
            }
            if (number > 9 && number < 12)
            {
                offsetX = (0.01f + dimensions.x) * 5;
                offsetY = (0.005f + dimensions.y) * (number - 10);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] BackgroundButton(int number)
        {
            Vector2 position = new Vector2(0.3f, 0.97f);
            Vector2 dimensions = new Vector2(0.035f, 0.03f);
            float offsetY = 0;
            float offsetX = 0;
            offsetX = (0.005f + dimensions.x) * number;
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] CalcButtonPos(int number)
        {
            Vector2 position = new Vector2(0.02f, 0.78f);
            Vector2 dimensions = new Vector2(0.15f, 0.15f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.01f + dimensions.x) * (number - 6);
                offsetY = (-0.025f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.01f + dimensions.x) * (number - 12);
                offsetY = (-0.025f - dimensions.y) * 2;
            }
            if (number > 17 && number < 24)
            {
                offsetX = (0.01f + dimensions.x) * (number - 18);
                offsetY = (-0.025f - dimensions.y) * 3;
            }
            if (number > 23 && number < 30)
            {
                offsetX = (0.01f + dimensions.x) * (number - 24);
                offsetY = (-0.025f - dimensions.y) * 4;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] CalcNumButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.75f);
            Vector2 dimensions = new Vector2(0.09f, 0.10f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 9)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 8 && number < 18)
            {
                offsetX = (0.01f + dimensions.x) * (number - 9);
                offsetY = (-0.02f - dimensions.y) * 1;
            }
            if (number > 17 && number < 27)
            {
                offsetX = (0.01f + dimensions.x) * (number - 18);
                offsetY = (-0.02f - dimensions.y) * 2;
            }
            if (number > 26 && number < 36)
            {
                offsetX = (0.01f + dimensions.x) * (number - 27);
                offsetY = (-0.02f - dimensions.y) * 3;
            }
            if (number > 35 && number < 45)
            {
                offsetX = (0.01f + dimensions.x) * (number - 36);
                offsetY = (-0.02f - dimensions.y) * 4;
            }
            if (number > 44 && number < 54)
            {
                offsetX = (0.01f + dimensions.x) * (number - 45);
                offsetY = (-0.02f - dimensions.y) * 5;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #endregion

        #region UI Commands

        [ChatCommand("gift")]
        private void cmdgift(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                GiftPanel(player);
                return;
            }
        }

        [ConsoleCommand("UI_GiftMenu")]
        private void cmdUI_GiftMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            GiftPanel(player, page);
        }     

        [ConsoleCommand("UI_SelectTime")]
        private void cmdUI_SelectTime(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            //Puts("TRYING");
            int time = Convert.ToInt32(arg.Args[0]);
            if (agdata.Gifts.ContainsKey(time))
                GetSendMSG(player, "TimeAlreadyExists", time.ToString());
            giftprep[player.userID].time = time;
            CreateGifts(player, 1);
        }

        

        [ConsoleCommand("UI_ManageGifts")]
        private void cmdUI_ManageGifts(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            ManageGifts(player, page);
        }

        [ConsoleCommand("UI_CreateGifts")]
        private void cmdUI_CreateGifts(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int step;
            if (!int.TryParse(arg.Args[0], out step)) return;
            int page;
            if (!int.TryParse(arg.Args[1], out page)) return;
            CreateGifts(player, step, page);
        }


        [ConsoleCommand("UI_RemoveGift")]
        private void cmdUI_RemoveGift(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int ID;
            if (!int.TryParse(arg.Args[0], out ID)) return;
            if (agdata.Gifts.ContainsKey(ID))
                agdata.Gifts.Remove(ID);
            foreach (var entry in CurrentGift.Where(e => e.Value == ID))
            {
                InitializeGiftObjective(BasePlayer.FindByID(entry.Key));
            }
            GetSendMSG(player, "GiftRemoved", ID.ToString());
            ManageGifts(player);
            SaveData();
        }

        [ConsoleCommand("UI_FinalizeGift")]
        private void cmdUI_FinalizeGift(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (agdata.Gifts.ContainsKey(giftprep[player.userID].time))
                agdata.Gifts.Remove(giftprep[player.userID].time);
            agdata.Gifts.Add(giftprep[player.userID].time, giftprep[player.userID].gifts);
            GetSendMSG(player, "NewGift", giftprep[player.userID].time.ToString());
            AdminPanel(player);
            SaveData();
        }



        [ConsoleCommand("UI_DestroyGiftPanel")]
        private void cmdUI_DestroyGiftPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyGiftPanel(player);
        }

        [ConsoleCommand("UI_CancelGiftCreation")]
        private void cmdUI_CancelListing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CancelGiftCreation(player);
        }


        [ConsoleCommand("UI_SelectGift")]
        private void cmdUI_SelectGift(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int item = Convert.ToInt32(arg.Args[0]);
            giftprep[player.userID].currentgift = new Gift {ID = item};
            DestroyGiftPanel(player);
            NumberPad(player, "UI_SelectAmount", "SelectAmount");
        }

        [ConsoleCommand("UI_SelectAmount")]
        private void cmdUI_SelectPriceAmount(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int amount = Convert.ToInt32(arg.Args[0]);
            giftprep[player.userID].currentgift.amount = amount;
            giftprep[player.userID].gifts.Add(giftprep[player.userID].currentgift);
            giftprep[player.userID].currentgift = null;
            CreateGifts(player, 99);
        }

        [ConsoleCommand("UI_AdminsPanel")]
        private void cmdUI_AdminMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AdminPanel(player);
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
                GetSendMSG(p, "AGInfo");
            }
            timers.Add("info", timer.Once(configData.InfoInterval * 60, () => InfoLoop()));
        }

        private void SetBoxFullNotification(string ID)
        {
            timers.Add(ID, timer.Once(5 * 60, () => timers.Remove(ID)));
        }

        #endregion

        #region Classes
        class GiftData
        {
            public Dictionary<string, uint> SavedImages = new Dictionary<string, uint>();
            public Dictionary<int, List<Gift>> Gifts = new Dictionary<int, List<Gift>>();
            public Dictionary<ulong, playerdata> Players = new Dictionary<ulong, playerdata>();
        }

        class Gift
        {
            public int ID;
            public int amount;
        }

        class playerdata
        {
            public int PlayerTime;
            public List<int> ReceivedGifts;
            public double Lastconnection;
        }

        class GiftCreation
        {
            public int time;
            public List<Gift> gifts = new List<Gift>();
            public Gift currentgift;
        }

        enum Category
        {
            Weapons,
            Armor,
            Attire,
            Ammunition,
            Medical,
            Tools,
            Building,
            Resources,
            Other,
            None,
            All,
            Extra,
            Food,
            Money
        }

        #endregion

        #region Unity WWW
        class QueueImage
        {
            public string url;
            public string shortname;
            public QueueImage(string st, string ur)
            {
                shortname = st;
                url = ur;
            }
        }

        class UnityImages : MonoBehaviour
        {
            AbsolutGifts filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueImage> QueueList = new List<QueueImage>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(AbsolutGifts am) => filehandler = am;
            public void Add(string shortname, string url)
            {
                QueueList.Add(new QueueImage(shortname, url));
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
                    if (!filehandler.agdata.SavedImages.ContainsKey(info.shortname))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.agdata.SavedImages.Add(info.shortname, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }

        [ConsoleCommand("images")]
        private void cmdgetimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Getimages();
            }
        }

        private void Getimages()
        {
            agdata.SavedImages.Clear();
            foreach (var category in urls)
                foreach (var entry in category.Value)
                    foreach (var item in entry.Value.Where(kvp => kvp.Key == 0))
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            uImage.Add(entry.Key, item.Value);
                        }
                    }
            Puts(GetLang("ImgReload"));
        }

        [ConsoleCommand("checkimages")]
        private void cmdrefreshimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Refreshimages();
            }
        }

        private void Refreshimages()
        {
            foreach (var category in urls)
                foreach (var entry in category.Value)
                    if (!agdata.SavedImages.ContainsKey(entry.Key))
                        foreach (var item in entry.Value.Where(kvp => kvp.Key == 0))
                        {
                            if (!string.IsNullOrEmpty(item.Value))
                            {
                                uImage.Add(entry.Key, item.Value);
                            }
                        }
            Puts(GetLang("ImgRefresh"));
        }
        #endregion

        #region Absolut Gifts Data Management

        private Dictionary<Category, Dictionary<string, Dictionary<int, string>>> urls = new Dictionary<Category, Dictionary<string, Dictionary<int, string>>>
        {
            {Category.Money, new Dictionary<string, Dictionary<int, string>>
            {
                {"SR", new Dictionary<int, string>
                {
                {0, "http://oxidemod.org/data/resource_icons/1/1751.jpg?1456924271" },
                }
                },
            }
            },

            {Category.None, new Dictionary<string, Dictionary<int, string>>
            {
                {"MISSINGIMG", new Dictionary<int, string>
                {
                {0, "http://www.hngu.net/Images/College_Logo/28/b894b451_c203_4c08_922c_ebc95077c157.png" },
                }
                },
                {"ARROW", new Dictionary<int, string>
                {
                {0, "http://www.freeiconspng.com/uploads/red-arrow-curved-5.png" },
                }
                },
                {"FIRST", new Dictionary<int, string>
                {
                {0, "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/simple-black-square-icons-arrows/126517-simple-black-square-icon-arrows-double-arrowhead-left.png" },
                }
                },
                {"BACK", new Dictionary<int, string>
                {
                {0, "https://image.freepik.com/free-icon/back-left-arrow-in-square-button_318-76403.png" },
                }
                },
                {"NEXT", new Dictionary<int, string>
                {
                {0, "https://image.freepik.com/free-icon/right-arrow-square-button-outline_318-76302.png"},
                }
                },
                {"LAST", new Dictionary<int, string>
                {
                {0, "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/matte-white-square-icons-arrows/124577-matte-white-square-icon-arrows-double-arrowhead-right.png" },
                }
                },
                {"OFILTER", new Dictionary<int, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/41/button-1157299_960_720.png" },
                }
                },
                {"UFILTER", new Dictionary<int, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/42/button-1157301_960_720.png" },
                }
                },
                {"ADMIN", new Dictionary<int, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/26/button-1157269_960_720.png" },
                }
                },
                {"MISC", new Dictionary<int, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2015/07/25/07/55/the-button-859343_960_720.png" },
                }
                },
                {"SELL", new Dictionary<int, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2015/07/25/08/03/the-button-859350_960_720.png" },
                }
                },

                }
            },
            {Category.Attire, new Dictionary<string, Dictionary<int, string>>
            {
                { "tshirt", new Dictionary<int, string>
                {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/62/T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200547" },
                {10130, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c6/Argyle_Scavenger_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204436"},
                {10033, "http://vignette2.wikia.nocookie.net/play-rust/images/4/44/Baseball_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053725" },
                {10003, "http://vignette1.wikia.nocookie.net/play-rust/images/b/bb/Black_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054123"},
                {14177, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1e/Blue_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053931" },
                {10056, "http://vignette3.wikia.nocookie.net/play-rust/images/9/98/Facepunch_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053603"},
                {14181, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a2/Forest_Camo_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053948" },
                {10024, "http://vignette3.wikia.nocookie.net/play-rust/images/1/19/German_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200140"},
                {10035, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c0/Hacker_Valley_Veteran_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053826" },
                {10046, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4d/Missing_Textures_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200051"},
                {10038, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4c/Murderer_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195600" },
                {101, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f3/Red_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053820" },
                {10025, "http://vignette1.wikia.nocookie.net/play-rust/images/b/bd/Russia_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195447" },
                {10002, "http://vignette4.wikia.nocookie.net/play-rust/images/5/59/Sandbox_Game_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054255"},
                {10134, "http://vignette1.wikia.nocookie.net/play-rust/images/6/61/Ser_Winter_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234909" },
                {10131, "http://vignette2.wikia.nocookie.net/play-rust/images/7/70/Shadowfrax_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234921"},
                {10041, "http://vignette1.wikia.nocookie.net/play-rust/images/4/43/Skull_%26_Bones_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195506" },
                {10053, "http://vignette4.wikia.nocookie.net/play-rust/images/5/5b/Smile_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195652"},
                {10039, "http://vignette1.wikia.nocookie.net/play-rust/images/6/6d/Target_Practice_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200031" },
                {584379, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1f/Urban_Camo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054016"},
                {10043, "http://vignette2.wikia.nocookie.net/play-rust/images/d/d8/Vyshyvanka_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053755" },
                }
            },
            {"pants", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3f/Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20150821195647" },
                {10001, "http://vignette2.wikia.nocookie.net/play-rust/images/1/1d/Blue_Jeans_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053716"},
                {10049, "http://vignette3.wikia.nocookie.net/play-rust/images/d/de/Blue_Track_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200133" },
                {10019, "http://vignette2.wikia.nocookie.net/play-rust/images/1/17/Forest_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195731"},
                {10078, "http://vignette2.wikia.nocookie.net/play-rust/images/3/30/Old_Prisoner_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200717" },
                {10048, "http://vignette1.wikia.nocookie.net/play-rust/images/4/4d/Punk_Rock_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053631"},
                {10021, "http://vignette2.wikia.nocookie.net/play-rust/images/d/de/Snow_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195500" },
                {10020, "http://vignette4.wikia.nocookie.net/play-rust/images/5/54/Urban_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195400" },
            }
            },
            {"shoes.boots", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b3/Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200326" },
                {10080, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f1/Army_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200800"},
                {10023, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c5/Black_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195749" },
                {10088, "http://vignette4.wikia.nocookie.net/play-rust/images/a/af/Bloody_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200836"},
                {10034, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a9/Punk_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195937" },
                {10044, "http://vignette2.wikia.nocookie.net/play-rust/images/5/5b/Scavenged_Sneaker_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195530"},
                {10022, "http://vignette1.wikia.nocookie.net/play-rust/images/c/cf/Tan_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195755" },
            }
            },
             {"tshirt.long", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/57/Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200605" },
                {10047, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e1/Aztec_Long_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195354"},
                {10004, "http://vignette3.wikia.nocookie.net/play-rust/images/e/e2/Black_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195617" },
                {10089, "http://vignette2.wikia.nocookie.net/play-rust/images/8/83/Christmas_Jumper_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200701"},
                {10106, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Creepy_Jack_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201059" },
                {10050, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c2/Frankensteins_Sweater_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195302"},
                {10032, "http://vignette1.wikia.nocookie.net/play-rust/images/0/04/Green_Checkered_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195518" },
                {10005, "http://vignette4.wikia.nocookie.net/play-rust/images/5/53/Grey_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195329"},
                {10125, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2c/Lawman_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201508" },
                {10118, "http://vignette4.wikia.nocookie.net/play-rust/images/9/96/Merry_Reindeer_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201444"},
                {10051, "http://vignette1.wikia.nocookie.net/play-rust/images/4/4b/Nightmare_Sweater_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195419" },
                {10006, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f6/Orange_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195658"},
                {10036, "http://vignette4.wikia.nocookie.net/play-rust/images/8/8a/Sign_Painter_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054215" },
                {10042, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1d/Varsity_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195536" },
                {10007, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ad/Yellow_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195949"},
            }
            },
             {"mask.bandana", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Bandana_Mask_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200526" },
                {10061, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bf/Black_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200233"},
                {10060, "http://vignette2.wikia.nocookie.net/play-rust/images/1/1d/Blue_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200057" },
                {10067, "http://vignette1.wikia.nocookie.net/play-rust/images/1/13/Checkered_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195703"},
                {10104, "http://vignette1.wikia.nocookie.net/play-rust/images/6/64/Creepy_Clown_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201043" },
                {10066, "http://vignette2.wikia.nocookie.net/play-rust/images/e/ee/Desert_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200208"},
                {10063, "http://vignette2.wikia.nocookie.net/play-rust/images/3/3f/Forest_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195310" },
                {10059, "http://vignette4.wikia.nocookie.net/play-rust/images/5/53/Green_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195442"},
                {10065, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9c/Red_Skull_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195511" },
                {10064, "http://vignette4.wikia.nocookie.net/play-rust/images/a/af/Skull_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195334"},
                {10062, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a4/Snow_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195425" },
                {10079, "http://vignette1.wikia.nocookie.net/play-rust/images/4/49/Wizard_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200841"},
            }
            },
             {"mask.balaclava", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/52/Improvised_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200410" },
                {10105, "http://vignette2.wikia.nocookie.net/play-rust/images/0/01/Burlap_Brains_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201020"},
                {10069, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e8/Desert_Camo_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200044"},
                {10071, "http://vignette3.wikia.nocookie.net/play-rust/images/7/70/Double_Yellow_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195931" },
                {10068, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a6/Forest_Camo_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200221"},
                {10057, "http://vignette4.wikia.nocookie.net/play-rust/images/2/2c/Murica_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053748" },
                {10075, "http://vignette2.wikia.nocookie.net/play-rust/images/2/2b/Nightmare_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062050"},
                {10070, "http://vignette2.wikia.nocookie.net/play-rust/images/8/86/Red_Check_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195709" },
                {10054, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f1/Rorschach_Skull_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053839"},
                {10090, "http://vignette2.wikia.nocookie.net/play-rust/images/0/09/Skin_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200829" },
                {10110, "http://vignette1.wikia.nocookie.net/play-rust/images/c/ce/Stitched_Skin_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201026"},
                {10084, "http://vignette2.wikia.nocookie.net/play-rust/images/2/27/The_Rust_Knight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200811" },
                {10139, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e7/Valentine_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204423"},
                {10111, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9c/Zipper_Face_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201014" },
            }
            },
             {"jacket.snow", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/04/Snow_Jacket_-_Red_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200536" },
                {10082, "http://vignette3.wikia.nocookie.net/play-rust/images/7/75/60%27s_Army_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200741"},
                {10113, "http://vignette2.wikia.nocookie.net/play-rust/images/e/ed/Black_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201110" },
                {10083, "http://vignette4.wikia.nocookie.net/play-rust/images/8/89/Salvaged_Shirt%2C_Coat_and_Tie_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200642"},
                {10112, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c9/Woodland_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201105" },
            }
            },
             {"jacket", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/8b/Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200520" },
                {10011, "http://vignette1.wikia.nocookie.net/play-rust/images/6/65/Blue_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200113"},
                {10012, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f0/Desert_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195737" },
                {10009, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4c/Green_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200156"},
                {10015, "http://vignette3.wikia.nocookie.net/play-rust/images/f/fd/Hunting_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195956" },
                {10013, "http://vignette3.wikia.nocookie.net/play-rust/images/d/db/Multicam_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200127"},
                {10072, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b5/Provocateur_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200817" },
                {10010, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Red_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195323" },
                {10008, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fc/Snowcamo_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200105"},
                {10014, "http://vignette2.wikia.nocookie.net/play-rust/images/9/94/Urban_Camo_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200215" },
            }
            },
            {"hoodie", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b5/Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205713" },
                {10142, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/BCHILLZ%21_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160313225348"},
                {14179, "http://vignette3.wikia.nocookie.net/play-rust/images/9/97/Black_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205706" },
                {10052, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Bloody_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205645"},
                {14178, "http://vignette3.wikia.nocookie.net/play-rust/images/5/5a/Blue_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205700" },
                {10133, "http://vignette2.wikia.nocookie.net/play-rust/images/2/27/Cuda87_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205750"},
                {14072, "http://vignette2.wikia.nocookie.net/play-rust/images/2/21/Green_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205654" },
                {10132, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4c/Rhinocrunch_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205744"},
                {10129, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0c/Safety_Crew_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205756" },
                {10086, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c2/Skeleton_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205737"},
            }
            },
            {"hat.cap", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/77/Baseball_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200434" },
                {10029, "http://vignette4.wikia.nocookie.net/play-rust/images/5/56/Blue_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195645"},
                {10027, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Forest_Camo_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200024" },
                {10055, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4e/Friendly_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195606"},
                {10030, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4a/Green_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195925" },
                {10026, "http://vignette3.wikia.nocookie.net/play-rust/images/7/70/Grey_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195714"},
                {10028, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Red_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195742" },
                {10045, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2d/Rescue_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053620" },
            }
            },
            {"hat.beenie", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c1/Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200552" },
                {14180, "http://vignette2.wikia.nocookie.net/play-rust/images/5/58/Black_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053801"},
                {10018, "http://vignette1.wikia.nocookie.net/play-rust/images/4/43/Blue_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200238" },
                {10017, "http://vignette4.wikia.nocookie.net/play-rust/images/b/b8/Green_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195919"},
                {10040, "http://vignette1.wikia.nocookie.net/play-rust/images/7/71/Rasta_Beenie_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195430" },
                {10016, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4e/Red_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195554"},
                {10085, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e3/Winter_Deers_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200706" },
            }
            },
            {"burlap.gloves", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a1/Leather_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200306" },
                {10128, "http://vignette4.wikia.nocookie.net/play-rust/images/b/b5/Boxer%27s_Bandages_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201502"},
            }
            },
            {"burlap.shirt", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/d7/Burlap_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200455" },
                {10136, "http://vignette1.wikia.nocookie.net/play-rust/images/7/77/Pirate_Vest_%26_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204350"},
            }
            },
            {"hat.boonie", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/88/Boonie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200347" },
                {10058, "http://vignette4.wikia.nocookie.net/play-rust/images/1/12/Farmer_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195725"},
            }
            },
            {"santahat", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4f/Santa_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151217230743" },
            }
            },
            {"hazmat.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6a/Hazmat_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060831" },
            }
            },
            {"hazmat.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Hazmat_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054359" },
            }
            },
            {"hazmat.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Hazmat_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061437" },
            }
            },
            {"hazmat.gloves", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/aa/Hazmat_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061629" },
            }
            },
            {"hazmat.boots", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/8a/Hazmat_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060906" },
            }
            },
            {"hat.miner", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1b/Miners_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060851" },
            }
            },
            {"hat.candle", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/ad/Candle_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061731" },
            }
            },

            {"burlap.trousers", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e5/Burlap_Trousers_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054430" },
            }
            },
            {"burlap.shoes", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/10/Burlap_Shoes_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061222" },
            }
            },
            {"burlap.headwrap", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Burlap_Headwrap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061044" },
            }
            },
            }
            },
            {Category.Armor, new Dictionary<string, Dictionary<int, string>>
            {
            {"bucket.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a5/Bucket_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200541" },
                {10127, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1c/Medic_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201521"},
                {10126, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Wooden_Bucket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201438" },
            }
            },
            {"wood.armor.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/68/Wood_Armor_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061716" },
            }
            },
            {"wood.armor.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4f/Wood_Chestplate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060921" },
            }
            },
            {"roadsign.kilt", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/31/Road_Sign_Kilt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200530" },
            }
            },
            {"roadsign.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/84/Road_Sign_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054408" },
            }
            },
            {"riot.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4e/Riot_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060949" },
            }
            },
            {"metal.plate.torso", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9d/Metal_Chest_Plate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061201" },
            }
            },
            {"metal.facemask", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1f/Metal_Facemask_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061432" },
            }
            },

            {"coffeecan.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/44/Coffee_Can_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061305" },
            }
            },
            {"bone.armor.suit", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/14/Bone_Armor_icon.png/revision/latest/scale-to-width-down/100?cb=20160901064349" },
            }
            },
            {"attire.hide.vest", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c0/Hide_Vest_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061337" },
            }
            },
            {"attire.hide.skirt", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/91/Hide_Skirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065030" },
            }
            },
            {"attire.hide.poncho", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/7/7f/Hide_Poncho_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061141" },
            }
            },
            {"attire.hide.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e4/Hide_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061352" },
            }
            },
            {"attire.hide.helterneck", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hide_Halterneck_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065021" },
            }
            },
            {"attire.hide.boots", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/57/Hide_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060954" },
            }
            },
            {"deer.skull.mask", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/22/Deer_Skull_icon.png/revision/latest/scale-to-width-down/100?cb=20150405141500" },
            }
            },
            }
            },
            {Category.Weapons, new Dictionary<string, Dictionary<int, string>>
            {
            {"pistol.revolver", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/58/Revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092929" },
                {10114, "http://vignette1.wikia.nocookie.net/play-rust/images/5/51/Outback_revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092935"},
            }
            },
            {"pistol.semiauto", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6b/Semi-Automatic_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200319" },
                {10087, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Contamination_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200655"},
                {10108, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c3/Halloween_Bat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201053" },
                {10081, "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Reaper_Note_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200711"},
                {10073, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Red_Shine_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200630" },
            }
            },
            {"rifle.ak", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200609" },
                {10135, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9e/Digital_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225138"},
                {10137, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9f/Military_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225144" },
                {10138, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a1/Tempered_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204335"},
            }
            },
            {"rifle.bolt", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200415" },
                {10117, "http://vignette2.wikia.nocookie.net/play-rust/images/2/22/Dreamcatcher_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234844"},
                {10115, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9e/Ghost_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234902" },
                {10116, "http://vignette1.wikia.nocookie.net/play-rust/images/c/cf/Tundra_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234858"},
            }
            },
            {"shotgun.pump", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/60/Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205718" },
                {10074, "http://vignette4.wikia.nocookie.net/play-rust/images/9/94/Chieftain_Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062100"},
                {10140, "http://vignette4.wikia.nocookie.net/play-rust/images/4/42/The_Swampmaster_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205830" },
            }
            },
            {"shotgun.waterpipe", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/1b/Waterpipe_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205730" },
                {10143, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4a/The_Peace_Pipe_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205804"},
            }
            },
            {"rifle.lr300", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/d/d9/LR-300_Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160825132402"},
            }
            },
            {"crossbow", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Crossbow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061004" },
            }
            },
            {"smg.thompson", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4e/Thompson_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092921" },
                {10120, "http://vignette3.wikia.nocookie.net/play-rust/images/8/84/Santa%27s_Little_Helper_icon.png/revision/latest/scale-to-width-down/100?cb=20160225141743"},
            }
            },
            {"weapon.mod.small.scope", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9c/4x_Zoom_Scope_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201610" },
            }
            },
            {"weapon.mod.silencer", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Silencer_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200615" },
            }
            },
            {"weapon.mod.muzzlebrake", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/3/38/Muzzle_Brake_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121719" },
            }
            },
            {"weapon.mod.muzzleboost", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7d/Muzzle_Boost_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121705" },
            }
            },
            {"weapon.mod.lasersight", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/8e/Weapon_Lasersight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201545" },
            }
            },
            {"weapon.mod.holosight", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/45/Holosight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200620" },
            }
            },
            {"weapon.mod.flashlight", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/0d/Weapon_Flashlight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201539" },
            }
            },
            {"spear.wooden", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f2/Wooden_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060930" },
            }
            },
            {"spear.stone", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0a/Stone_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061726" },
            }
            },
            {"smg.2", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Custom_SMG_icon.png/revision/latest/scale-to-width-down/100?cb=20151108000740" },
            }
            },
            {"shotgun.double", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/3f/Double_Barrel_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160816061211" },
            }
            },
            {"salvaged.sword", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/77/Salvaged_Sword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061458" },
            }
            },
            {"salvaged.cleaver", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7e/Salvaged_Cleaver_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054417" },
            }
            },
            {"rocket.launcher", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/06/Rocket_Launcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061852" },
            }
            },
            {"rifle.semiauto", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/8d/Semi-Automatic_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160128160721" },
            }
            },
            {"pistol.eoka", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b5/Eoka_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061104" },
            }
            },
            {"machete", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/34/Machete_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060741" },
            }
            },
            {"mace", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4d/Mace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061207" },
            }
            },
            {"longsword", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/34/Longsword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061240" },
            }
            },
            {"lmg.m249", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c6/M249_icon.png/revision/latest/scale-to-width-down/100?cb=20151112221315" },
            }
            },
            {"knife.bone", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/Bone_Knife_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061357" },
            }
            },
            {"flamethrower", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/55/Flame_Thrower_icon.png/revision/latest/scale-to-width-down/100?cb=20160415084104" },
            }
            },
            {"bow.hunting", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hunting_Bow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060745" },
            }
            },
            {"bone.club", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/19/Bone_Club_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060940" },
            }
            },
            {"grenade.f1", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/52/F1_Grenade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054509" },
            }
            },
            {"grenade.beancan", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/be/Beancan_Grenade_icon.png/revision/latest/scale-to-width-down/50?cb=20151106060959" },
            }
            },
            }
            },


            {Category.Ammunition, new Dictionary<string, Dictionary<int, string>>
            {
            {"ammo.handmade.shell", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0d/Handmade_Shell_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061522" },
            }
            },
            {"ammo.pistol", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9b/Pistol_Bullet_icon.png/revision/latest/scale-to-width-down/43?cb=20151106061928" },
            }
            },
             {"ammo.pistol.fire", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/31/Incendiary_Pistol_Bullet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054326" },
            }
            },
            {"ammo.pistol.hv", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e5/HV_Pistol_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061857" },
            }
            },
            {"ammo.rifle", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/49/5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103333" },
            }
            },
            {"ammo.rifle.explosive", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/31/Explosive_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061449" },
            }
            },
            {"ammo.rifle.hv", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/df/HV_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20150612151932" },
            }
            },
            {"ammo.rifle.incendiary", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e1/Incendiary_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200254" },
            }
            },
            {"ammo.rocket.basic", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061039" },
            }
            },
            {"ammo.rocket.fire", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f9/Incendiary_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061939" },
            }
            },
            {"ammo.rocket.hv", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f4/High_Velocity_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054436" },
            }
            },
            {"ammo.rocket.smoke", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/80/Smoke_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20150531134255" },
            }
            },
            {"ammo.shotgun", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2f/12_Gauge_Buckshot_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061114" },
            }
            },
            {"ammo.shotgun.slug", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/1a/12_Gauge_Slug_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061838" },
            }
            },
            {"arrow.hv", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/e/e5/High_Velocity_Arrow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054350" },
            }
            },
            {"arrow.wooden", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/3d/Wooden_Arrow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061615" },
            }
            },
            }
            },

            {Category.Medical, new Dictionary<string, Dictionary<int, string>>
            {
            {"bandage", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f8/Bandage_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061541" },
            }
            },
            {"syringe.medical", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/99/Medical_Syringe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061059" },
            }
            },
            { "largemedkit", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/99/Large_Medkit_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054425" },
            }
            },
            { "antiradpills", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0e/Anti-Radiation_Pills_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060926" },
            }
            },
            }
            },


            {Category.Building, new Dictionary<string, Dictionary<int, string>>
            {
            {"bed", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/fe/Bed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061212" },
            }
            },
            {"box.wooden", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/ff/Wood_Storage_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054500" },
            }
            },
            {"box.wooden.large", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b2/Large_Wood_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200336" },
            }
            },
            {"ceilinglight", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/43/Ceiling_Light_icon.png/revision/latest/scale-to-width-down/100?cb=20160331070008" },
            }
            },
            {"door.double.hinged.metal", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/14/Sheet_Metal_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201657" },
            }
            },
            {"door.double.hinged.toptier", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c1/Armored_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201616" },
            }
            },
            {"door.double.hinged.wood", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/41/Wood_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201605" },
            }
            },
            {"door.hinged.metal", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Sheet_Metal_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201232" },
            }
            },
            {"door.hinged.toptier", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/bc/Armored_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201148" },
            }
            },
            {"door.hinged.wood", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7e/Wooden_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201125" },
            }
            },
            {"floor.grill", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/48/Floor_Grill_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201701" },
            }
            },
            {"floor.ladder.hatch", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7c/Ladder_Hatch_icon.png/revision/latest/scale-to-width-down/100?cb=20160203005615" },
            }
            },
            {"gates.external.high.stone", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/85/High_External_Stone_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201341" },
            }
            },
            {"gates.external.high.wood", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/High_External_Wooden_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200625" },
            }
            },
            {"shelves", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a5/Salvaged_Shelves_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201358" },
            }
            },
            {"shutter.metal.embrasure.a", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/88/Metal_Vertical_embrasure_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201307" },
            }
            },
            {"shutter.metal.embrasure.b", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/5d/Metal_horizontal_embrasure_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201154" },
            }
            },
            {"shutter.wood.a", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/2b/Wood_Shutters_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201159" },
            }
            },
            {"sign.hanging", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/d/df/Two_Sided_Hanging_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200913" },
            }
            },
            {"sign.hanging.banner.large", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/29/Large_Banner_Hanging_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200937" },
            }
            },
            {"sign.hanging.ornate", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4f/Two_Sided_Ornate_Hanging_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200909" },
            }
            },
            {"sign.pictureframe.landscape", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/87/Landscape_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200943" },
            }
            },
            {"sign.pictureframe.portrait", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/50/Portrait_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200949" },
            }
            },
            {"sign.pictureframe.tall", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/65/Tall_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201003" },
            }
            },
            {"sign.pictureframe.xl", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/bf/XL_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200847" },
            }
            },
            {"sign.pictureframe.xxl", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/95/XXL_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200933" },
            }
            },
            {"sign.pole.banner.large", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/1/16/Large_Banner_on_pole_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200958" },
            }
            },
            {"sign.post.double", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/5e/Double_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200918" },
            }
            },
            {"sign.post.single", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/11/Single_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200928" },
            }
            },
            {"sign.post.town", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/62/One_Sided_Town_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200953" },
            }
            },
            {"sign.post.town.roof", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/fa/Two_Sided_Town_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200904" },
            }
            },
            {"sign.wooden.huge", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6e/Huge_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054354" },
            }
            },
            {"sign.wooden.large", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bc/Large_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061909" },
            }
            },
            {"sign.wooden.medium", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c3/Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061634" },
            }
            },
            {"sign.wooden.small", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Small_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061315" },
            }
            },
            {"jackolantern.angry", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/9/96/Jack_O_Lantern_Angry_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062158" },
            }
            },
            {"jackolantern.happy", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/92/Jack_O_Lantern_Happy_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062154" },
            }
            },
            {"ladder.wooden.wall", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c8/Wooden_Ladder_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200358" },
            }
            },
            {"lantern", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/46/Lantern_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060856" },
            }
            },
            {"lock.code", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0c/Code_Lock_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061407" },
            }
            },
            {"mining.quarry", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054421" },
            }
            },
            {"wall.external.high", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/96/High_External_Wooden_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061300" },
            }
            },
            {"wall.external.high.stone", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b6/High_External_Stone_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060841" },
            }
            },
            {"wall.frame.cell", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f6/Prison_Cell_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201626" },
            }
            },
            {"wall.frame.cell.gate", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/30/Prison_Cell_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201621" },
            }
            },
            {"wall.frame.fence", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/2a/Chainlink_Fence_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201645" },
            }
            },
            {"wall.frame.fence.gate", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/7a/Chainlink_Fence_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201556" },
            }
            },
            {"wall.frame.shopfront", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/c/c1/Shop_Front_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201551" },
            }
            },
            {"wall.window.bars.metal", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fe/Metal_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201255" },
            }
            },
            {"wall.window.bars.toptier", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/e/eb/Reinforced_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201133" },
            }
            },
            {"wall.window.bars.wood", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/27/Wooden_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201138" },
            }
            },
            {"lock.key", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9e/Lock_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061620" },
            }
            },
            { "barricade.concrete", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b3/Concrete_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061124" },
            }
            },
            {"barricade.metal", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/b/bb/Metal_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061108" },
            }
            },
            { "barricade.sandbags", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a7/Sandbag_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061417" },
            }
            },
            { "barricade.wood", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e5/Wooden_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061024" },
            }
            },
            { "barricade.woodwire", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7b/Barbed_Wooden_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061508" },
            }
            },
            { "barricade.stone", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/cc/Stone_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061226" },
            }
            },
            }
            },

            {Category.Resources, new Dictionary<string, Dictionary<int, string>>
            {
            {"charcoal", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ad/Charcoal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061556" },
            }
            },
            {"cloth", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f7/Cloth_icon.png/revision/latest/scale-to-width-down/100?cb=20151106071629" },
            }
            },
            {"crude.oil", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3c/Crude_Oil_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054451" },
            }
            },
            {"fat.animal", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/d/d5/Animal_Fat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060911" },
            }
            },
            {"hq.metal.ore", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/80/High_Quality_Metal_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061625" },
            }
            },
            {"lowgradefuel", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/26/Low_Grade_Fuel_icon.png/revision/latest/scale-to-width-down/100?cb=20151110002210" },
            }
            },
            {"metal.fragments", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/7/74/Metal_Fragments_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061759" },
            }
            },
            {"metal.ore", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0a/Metal_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060814" },
            }
            },
            {"leather", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9a/Leather_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061923" },
            }
            },
            {"metal.refined", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a1/High_Quality_Metal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061156" },
            }
            },
            {"wood", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f2/Wood_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061551" },
            }
            },
            {"seed.corn", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Corn_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054446" },
            }
            },
            {"seed.hemp", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1c/Hemp_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20160708084856" },
            }
            },
            {"seed.pumpkin", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/66/Pumpkin_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054519" },
            }
            },
            {"stones", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/85/Stones_icon.png/revision/latest/scale-to-width-down/100?cb=20150405123145" },
            }
            },
            {"sulfur", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/32/Sulfur_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061605" },
            }
            },
            {"sulfur.ore", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/22/Sulfur_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061904" },
            }
            },
            {"gunpowder", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/17/Gun_Powder_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060731" },
            }
            },
            {"researchpaper", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/ac/Research_Paper_icon.png/revision/latest/scale-to-width-down/100?cb=20160819103106" },
            }
            },
            {"explosives", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/47/Explosives_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054330" },
            }
            },
            }
            },





            {Category.Tools, new Dictionary<string, Dictionary<int, string>>
            {
            {"botabag", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f5/Bota_Bag_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061015" },
            }
            },
            {"box.repair.bench", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3b/Repair_Bench_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214020" },
            }
            },
            {"bucket.water", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bc/Water_Bucket_icon.png/revision/latest/scale-to-width-down/100?cb=20160413085322" },
            }
            },
            {"explosive.satchel", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0b/Satchel_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20160813023035" },
            }
            },
            {"explosive.timed", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/6c/Timed_Explosive_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061610" },
            }
            },
            {"flare", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Flare_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061129" },
            }
            },
            {"fun.guitar", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bb/Acoustic_Guitar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060809" },
            }
            },
            {"furnace", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e3/Furnace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054341" },
            }
            },
            {"furnace.large", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/e/ee/Large_Furnace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054456" },
            }
            },
            {"hatchet", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/06/Hatchet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061743" },
            }
            },
            {"icepick.salvaged", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e1/Salvaged_Icepick_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061453" },
            }
            },
            {"axe.salvaged", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c9/Salvaged_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060750" },
            }
            },
            {"pickaxe", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/86/Pick_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061323" },
            }
            },
            {"research.table", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/21/Research_Table_icon.png/revision/latest/scale-to-width-down/100?cb=20160129014240" },
            }
            },
            {"small.oil.refinery", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ac/Small_Oil_Refinery_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214041" },
            }
            },
            {"stone.pickaxe", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/77/Stone_Pick_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20150405134645" },
            }
            },
            {"stonehatchet", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9b/Stone_Hatchet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061843" },
            }
            },
            {"supply.signal", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/24/Supply_Signal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106071621" },
            }
            },
            {"surveycharge", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9a/Survey_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061517" },
            }
            },
            {"target.reactive", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/60/Reactive_Target_icon.png/revision/latest/scale-to-width-down/100?cb=20160331070018" },
            }
            },
            {"tool.camera", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/0e/Camera_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060755" },
            }
            },
            {"water.barrel", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e2/Water_Barrel_icon.png/revision/latest/scale-to-width-down/100?cb=20160504013134" },
            }
            },
            {"water.catcher.large", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/3/35/Large_Water_Catcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061049" },
            }
            },
            {"water.catcher.small", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/04/Small_Water_Catcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061919" },
            }
            },
            {"water.purifier", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6e/Water_Purifier_icon.png/revision/latest/scale-to-width-down/100?cb=20160512082941" },
            }
            },
            {"torch", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/48/Torch_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061512" },
            }
            },
            {"stash.small", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Small_Stash_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062004" },
            }
            },
            {"sleepingbag", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/be/Sleeping_Bag_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200428" },
            }
            },
            {"hammer.salvaged", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f8/Salvaged_Hammer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060804" },
            }
            },
            {"hammer", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Hammer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061319" },
            }
            },
            {"blueprintbase", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Blueprint_icon.png/revision/latest/scale-to-width-down/100?cb=20160819063752" },
            }
            },
            {"fishtrap.small", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9d/Survival_Fish_Trap_icon.png/revision/latest/scale-to-width-down/100?cb=20160506135224" },
            }
            },
            {"building.planner", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/ba/Building_Plan_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061847" },
            }
            },
            }
            },

            {Category.Other, new Dictionary<string, Dictionary<int, string>>
            {
            { "cctv.camera", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/24/CCTV_Camera_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062215" },
            }
            },
            {"pookie.bear", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/61/Pookie_Bear_icon.png/revision/latest/scale-to-width-down/100?cb=20151217230015" },
            }
            },
            {"targeting.computer", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/07/Targeting_Computer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062210" },
            }
            },
            {"trap.bear", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b0/Snap_Trap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061711" },
            }
            },
            {"trap.landmine", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Land_Mine_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200450" },
            }
            },
            {"autoturret", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f9/Auto_Turret_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062203" },
            }
            },
            {"spikes.floor", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f7/Wooden_Floor_Spikes_icon.png/revision/latest/scale-to-width-down/100?cb=20150517235346" },
            }
            },
            {"note", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/d/d5/Note_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060819" },
            }
            },
            {"paper", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/96/Paper_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054403" },
            }
            },
            {"map", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/c/c8/Paper_Map_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061639" },
            }
            },
            {"campfire", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/35/Camp_Fire_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060846" },
            }
            },
            }
            },

            {Category.Food, new Dictionary<string, Dictionary<int, string>>
            {
            { "wolfmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/16/Cooked_Wolf_Meat_icon.png/revision/latest/scale-to-width-down/100?cb=20160131235320" },
            }
            },
            {"waterjug", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f2/Water_Jug_icon.png/revision/latest/scale-to-width-down/100?cb=20160422072821" },
            }
            },
            {"water.salt", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/ce/Salt_Water_icon.png/revision/latest/scale-to-width-down/100?cb=20160708084848" },
            }
            },
            {"water", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/7f/Water_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061403" },
            }
            },
            {"smallwaterbottle", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fc/Small_Water_Bottle_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061933" },
            }
            },
            {"pumpkin", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4c/Pumpkin_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061814" },
            }
            },
            {"mushroom", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/a8/Mushroom_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060836" },
            }
            },
            {"meat.pork.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Cooked_Pork_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201237" },
            }
            },
            {"humanmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/d/d2/Cooked_Human_Meat_icon.png/revision/latest/scale-to-width-down/100?cb=20150405113229" },
            }
            },
            {"granolabar", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6c/Granola_Bar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060916" },
            }
            },
            {"fish.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/8b/Cooked_Fish_icon.png/revision/latest/scale-to-width-down/100?cb=20160506135233" },
            }
            },
            {"chocholate", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/45/Chocolate_Bar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061914" },
            }
            },
            {"chicken.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6f/Cooked_Chicken_icon.png/revision/latest/scale-to-width-down/100?cb=20151108000759" },
            }
            },
            {"candycane", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2c/Candy_Cane_icon.png/revision/latest/scale-to-width-down/100?cb=20151217224745" },
            }
            },
            {"can.tuna", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/2d/Can_of_Tuna_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061943" },
            }
            },
            {"can.beans", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e5/Can_of_Beans_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060935" },
            }
            },
            {"blueberries", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Blueberries_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061231" },
            }
            },
            {"black.raspberries", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/6/6f/Black_Raspberries_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214047" },
            }
            },
            {"bearmeat.cooked", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/17/Bear_Meat_Cooked_icon.png/revision/latest/scale-to-width-down/100?cb=20160109015147" },
            }
            },
            {"apple", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061034" },
            }
            },
            }
            }
        };

        private Dictionary<string, string> defaultBackgrounds = new Dictionary<string, string>
        {
            { "NEVERDELETE", "http://www.intrawallpaper.com/static/images/r4RtXBr.png" },
            { "default2", "http://www.intrawallpaper.com/static/images/background-wallpapers-32_NLplhCS.jpg" },
            { "default3", "http://www.intrawallpaper.com/static/images/Light-Wood-Background-Wallpaper_JHG6qot.jpg" },
            { "default4", "http://www.intrawallpaper.com/static/images/White-Background-BD1.png" },
            { "default5", "http://www.intrawallpaper.com/static/images/Red_Background_05.jpg" },
            { "default6", "http://www.intrawallpaper.com/static/images/White-Background-BD1.png" },
            { "default7", "http://www.intrawallpaper.com/static/images/abstract-hd-wallpapers-1080p_gDn0G81.jpg" },
            { "default8", "http://www.intrawallpaper.com/static/images/Background-HD-High-Quality-C23.jpg" },
            { "default10", "http://www.intrawallpaper.com/static/images/wood_background_hd_picture_3_169844.jpg" },
            { "default11", "http://www.intrawallpaper.com/static/images/518079-background-hd.jpg" },
            { "default12", "http://www.intrawallpaper.com/static/images/special_flashy_stars_background_03_hd_pictures_170805.jpg" },
            { "default13", "http://www.intrawallpaper.com/static/images/maxresdefault_jKFJl8g.jpg" },
            { "default14", "http://www.intrawallpaper.com/static/images/maxresdefault15.jpg" },
        };



        void SaveData()
        {
            AGData.WriteObject(agdata);
        }

        void LoadData()
        {
            try
            {
                agdata = AGData.ReadObject<GiftData>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Gift Data, creating a new datafile");
                agdata = new GiftData();
            }
        }

        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int InfoInterval { get; set; }
            public bool NoAFK { get; set; }

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
                NoAFK = true,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Absolut Gifts: " },
            {"AGInfo", "This server is running Absolut Gifts. Type /gift to access the Gift Menu!"},
            {"Next", "Next" },
            {"Back", "Back" },
            {"First", "First" },
            {"Last", "Last" },
            {"Close", "Close"},
            {"Quit", "Quit"},
            {"ImgReload", "Images have been wiped and reloaded!" },
            {"ImgRefresh", "Images have been refreshed !" },
            {"NewGiftInfo", "New Gift Information for Playing {0} Minutes." },
            {"GiftDetails", "{1} - {0}" },
            {"TimeAlreadyExists", "The select time {0} already exists as a Gift, if you continue the old entry will be removed" },
            {"GiftCreationCanceled", "You have successfully cancelled Gift Creation." },
            {"AdminPanel", "Admin Menu" },
            {"Delete", "Delete" },
            {"GiftTitle", "Gift Requirement: {0} minutes" },
            {"GiftTitleInProgress", "IN PROGRESS: {0}" },
            {"GiftTitleCompleted", "COMPLETED: {0}" },
            {"NewGift", "You have successfully created a new gift for {0} minutes!" },
            {"NewGiftGiven", "You have been given {0} {1} for your PlayTime! Thanks for playing on the server today!" },
            {"SelectTime", "Select Minute Requirement for this Gift..." },
            {"SelectAmount", "Select the amount of the chosen item for this Gift." },
            {"CreateGift", "Create a Gift" },
            {"ManageGifts", "Manage Gifts" },
            {"SelectGift", "Select a Gift Item" },
            {"FinalizeGift", "Save Gift" },
            {"AddToGift", "Add More..." },
            {"Cancel", "Cancel" }

        };
        #endregion
    }
}
