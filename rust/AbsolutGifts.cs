// Requires: ImageLibrary
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections;
using System.IO;

namespace Oxide.Plugins
{
    [Info("AbsolutGifts", "Absolut", "1.1.1", ResourceId = 2159)]

    class AbsolutGifts : RustPlugin
    {
        [PluginReference]
        ImageLibrary ImageLibrary;

        [PluginReference]
        Plugin ServerRewards;

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin AbsolutCombat;

        GiftData agdata;
        private DynamicConfigFile AGData;

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
            LoadVariables();
            LoadData();
            AddImage("http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/simple-black-square-icons-arrows/126517-simple-black-square-icon-arrows-double-arrowhead-left.png", "FIRST");
            AddImage("https://image.freepik.com/free-icon/back-left-arrow-in-square-button_318-76403.png", "BACK");
            AddImage("https://image.freepik.com/free-icon/right-arrow-square-button-outline_318-76302.png", "NEXT");
            AddImage("http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/matte-white-square-icons-arrows/124577-matte-white-square-icon-arrows-double-arrowhead-right.png", "LAST");
            AddImage("http://oxidemod.org/data/resource_icons/1/1751.jpg?1456924271", "SR");
            AddImage("http://oxidemod.org/data/resource_icons/0/717.jpg?1465675504", "ECO");
            AddImage("http://oxidemod.org/data/resource_icons/2/2103.jpg?1472590458", "AC");
            if (!permission.PermissionExists("AbsolutGifts.vip"))
                permission.RegisterPermission("AbsolutGifts.vip", this);
            if (!permission.PermissionExists("AbsolutGifts.admin"))
                permission.RegisterPermission("AbsolutGifts.admin", this);
            timers.Add("info", timer.Once(900, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            SaveData();
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
                InitializePlayer(player);
            }
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
                agdata.Players.Add(player.userID, new playerdata { PlayerTime = 0, ReceivedGifts = new List<int>() });
            agdata.Players[player.userID].PlayerTime = TimeSinceLastGift[player.userID];
            agdata.Players[player.userID].Lastconnection = GrabCurrentTime();
            if (!timers.ContainsKey(player.userID.ToString()))
                agdata.Players[player.userID].Lastconnection = GrabCurrentTime();
            else timers.Remove(player.userID.ToString());
            //Puts(TimeSinceLastGift[player.userID].ToString());
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

        public string GetImage(string shortname, ulong skin = 0)
        {
            var img = ImageLibrary.GetImage(shortname, skin);
            return img;
        }

        public bool AddImage(string url, string shortname, ulong skin = 0)
        {
            var img = ImageLibrary.AddImage(url, shortname, skin);
            return img;
        }




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
            if (player == null)
                return false;
            if (!AFK.ContainsKey(player.userID))
            {
                AFK.Add(player.userID, player.transform.position);
                //Puts("Not in Dictionary");
                return false;
            }
            if (AFK[player.userID] == player.transform.position)
            {
                //Puts("Same Position - AFK");
                return true;
            }
            else if (AFK[player.userID] != player.transform.position)
            {
                AFK[player.userID] = player.transform.position;
                //Puts("New Position - Not AFK");
            }
            return false;
        }

        private void InitializeGiftObjective(BasePlayer player)
        {
            if (!agdata.Players.ContainsKey(player.userID))
                agdata.Players.Add(player.userID, new playerdata { PlayerTime = 0, ReceivedGifts = new List<int>() });
            foreach (var entry in agdata.Gifts.Where(gift => !agdata.Players[player.userID].ReceivedGifts.Contains(gift.Key)).OrderBy(kvp => kvp.Key))
            {
                if (entry.Value.vip)
                    if (!permission.UserHasPermission(player.UserIDString, "AbsolutGifts.vip"))
                        continue;
                //Puts("New Objective {entry.Key.ToString()}");
                if (Objective.ContainsKey(player.userID))
                    Objective.Remove(player.userID);
                Objective.Add(player.userID, entry.Value.gifts);
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
            agdata.Players[player.userID].PlayerTime = 0;
            agdata.Players[player.userID].ReceivedGifts.Clear();
            Objective.Remove(player.userID);
            NextGift.Remove(player.userID);
            CurrentGift.Remove(player.userID);
            InitializeGiftObjective(player);
            SaveData();
        }

        private void GiveGift(BasePlayer player)
        {
            foreach (var entry in Objective[player.userID])
            {
                if (entry.SR)
                {
                    ServerRewards?.Call("AddPoints", player.userID.ToString(), entry.amount);
                    GetSendMSG(player, "NewGiftGiven", entry.amount.ToString(), "ServerRewards Points");
                }
                else if (entry.Eco)
                {
                    Economics.Call("DepositS", player.userID.ToString(), entry.amount);
                    GetSendMSG(player, "NewGiftGiven", entry.amount.ToString(), "Economics");
                }
                else if (entry.AC)
                {
                    AbsolutCombat.Call("AddMoney", player.userID.ToString(), entry.amount, false);
                    GetSendMSG(player, "NewGiftGiven", entry.amount.ToString(), "AbsolutCombat Money");
                }
                else
                {
                    Item item = ItemManager.CreateByItemID(entry.ID, entry.amount);
                    if (item != null)
                    {
                        item.MoveToContainer(player.inventory.containerMain);
                        GetSendMSG(player, "NewGiftGiven", item.amount.ToString(), item.info.shortname);
                    }
                }
                //Puts($"Gave Gift: {item.info.shortname}");
            }
            TimeSinceLastGift[player.userID] = 0;
            agdata.Players[player.userID].ReceivedGifts.Add(CurrentGift[player.userID]);
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
            static public void CreateTextOutline(ref CuiElementContainer element, string panel, string colorText, string colorOutline, string text, int size, string DistanceA, string DistanceB, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiTextComponent{Color = colorText, FontSize = size, Align = align, Text = text },
                        new CuiOutlineComponent {Distance = DistanceA + " " + DistanceB, Color = colorOutline},
                        new CuiRectTransformComponent {AnchorMax = aMax, AnchorMin = aMin }
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
            //Puts(count.ToString());
            int entriesallowed = 4;
            double remainingentries = count - (page * (entriesallowed));
            double totalpages = (Math.Floor(count / (entriesallowed)));
            //Puts(totalpages.ToString());

            if (permission.UserHasPermission(player.UserIDString, "AbsolutGifts.admin") || isAuth(player))
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("AdminPanel"), 12, "0.03 0.02", "0.13 0.075", $"UI_AdminsPanel");
            }

            if (page < totalpages - 2)
            {
                UI.CreateButton(ref element, PanelGift, UIColors["header"], GetLang("Last"), 18, "0.8 0.02", "0.85 0.075", $"UI_GiftMenu {totalpages - 1}");
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
            CuiElementContainer element = UI.CreateElementContainer(PanelGift, "0 0 0 0", "0.3 0.3", "0.7 0.9");
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
                    element = UI.CreateElementContainer(PanelGift, "0 0 0 0", "0.4 0.3", "0.6 0.6", true);
                    UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GetMSG("VIPGift", giftprep[player.userID].time.ToString()), 20, "0.05 .4", ".95 .95");
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonbg"], GetLang("Yes"), 18, "0.2 0.05", "0.4 0.25", $"UI_VIP true", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonred"], GetLang("No"), 18, "0.6 0.05", "0.8 0.25", $"UI_VIP false");
                    break;
                case 2:
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
                            UI.LoadImage(ref element, PanelGift, GetImage("LAST"), "0.8 0.02", "0.85 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.8 0.02", "0.85 0.075", $"UI_CreateGifts {2} {totalpages}");
                        }
                        if (remainingentries > entriesallowed)
                        {
                            UI.LoadImage(ref element, PanelGift, GetImage("NEXT"), "0.74 0.02", "0.79 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.74 0.02", "0.79 0.075", $"UI_CreateGifts {2} {page + 1}");
                        }
                        if (page > 0)
                        {
                            UI.LoadImage(ref element, PanelGift, GetImage("BACK"), "0.68 0.02", "0.73 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.68 0.02", "0.73 0.075", $"UI_CreateGifts {2} {page - 1}");
                        }
                        if (page > 1)
                        {
                            UI.LoadImage(ref element, PanelGift, GetImage("FIRST"), "0.62 0.02", "0.67 0.075");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 16, "0.62 0.02", "0.67 0.075", $"UI_CreateGifts {2} {0}");
                        }
                    }
                    int n = 0;
                    var pos = CalcButtonPos(n);
                    double shownentries = page * entriesallowed;
                    if (page == 0)
                    {
                        if (ServerRewards)
                        {
                            UI.LoadImage(ref element, PanelGift, GetImage("SR"), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectGift SR");
                            n++;
                            i++;
                        }
                        if (Economics)
                        {
                            pos = CalcButtonPos(n);
                            UI.LoadImage(ref element, PanelGift, GetImage("ECO"), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectGift ECO");
                            n++;
                            i++;
                        }
                        if (AbsolutCombat)
                        {
                            pos = CalcButtonPos(n);
                            UI.LoadImage(ref element, PanelGift, GetImage("AC"), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelGift, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectGift AC");
                            n++;
                            i++;
                        }
                    }
                    foreach (var item in ItemManager.itemList)
                    {
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            pos = CalcButtonPos(n);
                            UI.LoadImage(ref element, PanelGift, GetImage(item.shortname), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            //UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], item.shortname, 14, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);
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
                    var alt = "";
                    foreach (var entry in giftprep[player.userID].gifts)
                        if (entry.ID != 0)
                            GiftDetails += $"{GetMSG("GiftDetails", ItemManager.FindItemDefinition(entry.ID).shortname, entry.amount.ToString())}\n";
                        else
                        {
                            if (entry.AC) alt = "AbsolutCombat Money";
                            if (entry.Eco) alt = "Economics";
                            if (entry.SR) alt = "ServerRewards Points";
                            GiftDetails += $"{GetMSG("GiftDetails", alt, entry.amount.ToString())}\n";
                        }
                    UI.CreateLabel(ref element, PanelGift, UIColors["limegreen"], GiftDetails, 20, "0.1 0.16", "0.9 0.75", TextAnchor.MiddleLeft);
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonbg"], GetLang("FinalizeGift"), 18, "0.2 0.05", "0.4 0.15", $"UI_FinalizeGift", TextAnchor.MiddleCenter);
                    if (giftprep[player.userID].gifts.Count() < 11)
                    UI.CreateButton(ref element, PanelGift, UIColors["buttonbg"], GetLang("AddToGift"), 18, "0.401 0.05", "0.599 0.15", $"UI_CreateGifts {2} {0}", TextAnchor.MiddleCenter);
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
                UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateTextOutline(ref container, panelName, UIColors["yellow"], UIColors["black"], GetMSG("GiftTitleInProgress", ID.ToString(), NextGift[player.userID].ToString()), 16, "1", "1", $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
            }
            else if (completed != null && completed.Contains(ID))
            {
                UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateTextOutline(ref container, panelName, UIColors["green"], UIColors["black"], GetMSG("GiftTitleCompleted", ID.ToString()), 16, "1", "1", $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
            }
            else
            {
                UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");
                UI.CreateTextOutline(ref container, panelName, UIColors["white"], UIColors["black"], GetMSG("GiftTitle", ID.ToString()), 16, "1", "1", $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
            }
            if (agdata.Gifts[ID].vip) UI.CreateLabel(ref container, PanelGift, UIColors["red"], "VIP", 30, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);

            var xmin = pos[0] + 0.001f;
            var xmax = pos[0] + 0.1f;
            var ymin = pos[3] - 0.135f;
            var ymax = pos[3] - 0.05f;
            foreach (var entry in agdata.Gifts[ID].gifts)
            {
                var item = ItemManager.CreateByItemID(entry.ID);
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
                    if(item != null)
                    UI.LoadImage(ref container, PanelGift, GetImage(item.info.shortname), $"{xmin} {ymin}", $"{xmax} {ymax}");
                    else if (entry.Eco)
                        UI.LoadImage(ref container, PanelGift, GetImage("ECO"), $"{xmin} {ymin}", $"{xmax} {ymax}");
                    else if (entry.SR)
                        UI.LoadImage(ref container, PanelGift, GetImage("SR"), $"{xmin} {ymin}", $"{xmax} {ymax}");
                    else if (entry.AC)
                        UI.LoadImage(ref container, PanelGift, GetImage("AC"), $"{xmin} {ymin}", $"{xmax} {ymax}");
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
            int time = Convert.ToInt32(arg.Args[0]);
            if (agdata.Gifts.ContainsKey(time))
                GetSendMSG(player, "TimeAlreadyExists", time.ToString());
            giftprep[player.userID].time = time;
            CreateGifts(player, 1);
        }

        [ConsoleCommand("UI_VIP")]
        private void cmdUI_VIP(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var answer = arg.Args[0];
            if (answer == "true")
                giftprep[player.userID].vip = true;
            else giftprep[player.userID].vip = false;
            CreateGifts(player, 2);
        }
   
        [ConsoleCommand("UI_ManageGifts")]
        private void cmdUI_ManageGifts(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!permission.UserHasPermission(player.UserIDString, "AbsolutGifts.admin") && !isAuth(player)) return;
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
            if (!permission.UserHasPermission(player.UserIDString, "AbsolutGifts.admin") && !isAuth(player)) return;
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
            agdata.Gifts.Add(giftprep[player.userID].time, new GiftCollection { gifts = giftprep[player.userID].gifts, vip = giftprep[player.userID].vip });
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
            int item;
            if (int.TryParse(arg.Args[0], out item)) giftprep[player.userID].currentgift = new Gift { ID = item };
            else
            {
                if (arg.Args[0] == "SR")
                    giftprep[player.userID].currentgift = new Gift { SR = true };
                else if (arg.Args[0] == "ECO")
                    giftprep[player.userID].currentgift = new Gift { Eco = true };
                else if (arg.Args[0] == "AC")
                    giftprep[player.userID].currentgift = new Gift { AC = true };
            }
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
            //if (giftprep[player.userID].gifts.Count() == 0 || giftprep[player.userID].gifts == null)
            //    giftprep[player.userID].gifts = new List<Gift>();
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
            if (!permission.UserHasPermission(player.UserIDString, "AbsolutGifts.admin") && !isAuth(player))
            {
                GetSendMSG(player, "NotAuth");
                return;
            }
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
            public Dictionary<int, GiftCollection> Gifts = new Dictionary<int, GiftCollection>();
            public Dictionary<ulong, playerdata> Players = new Dictionary<ulong, playerdata>();
        }
        class GiftCollection
        {
            public bool vip;
            public List<Gift> gifts = new List<Gift>();
        }

        class Gift
        {
            public int ID;
            public int amount;
            public bool SR;
            public bool Eco;
            public bool AC;
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
            public bool vip;
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

        #region Data Management

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
            {"NewGiftInfo", "New Gift Information for Playing {0} Minute(s)." },
            {"GiftDetails", "{1} - {0}" },
            {"TimeAlreadyExists", "The select time {0} already exists as a Gift, if you continue the old entry will be removed" },
            {"GiftCreationCanceled", "You have successfully cancelled Gift Creation." },
            {"AdminPanel", "Admin Menu" },
            {"Delete", "Delete" },
            {"GiftTitle", "Gift Requirement: {0} Minute(s)" },
            {"GiftTitleInProgress", "IN PROGRESS: {0} Minute(s)\nYou have {1} Minute(s) Remaining!" },
            {"GiftTitleCompleted", "COMPLETED: {0} Minute(s)" },
            {"NewGift", "You have successfully created a new gift for {0} Minute(s)!" },
            {"GiftRemoved", "You have deleted the gift for {0} Minute(s)!" },
            {"NewGiftGiven", "You have been given {0} {1} for your PlayTime! Thanks for playing on the server today!" },
            {"SelectTime", "Select Minute Requirement for this Gift..." },
            {"SelectAmount", "Select the amount of the chosen item for this Gift." },
            {"CreateGift", "Create a Gift" },
            {"ManageGifts", "Manage Gifts" },
            {"SelectGift", "Select a Gift Item" },
            {"FinalizeGift", "Save Gift" },
            {"AddToGift", "Add More..." },
            {"Cancel", "Cancel" },
            {"NotAuth", "You are not an admin." },
            {"VIPGift", "Make this a VIP only gift?" },
            {"Yes", "Yes" },
            {"No", "No" }

        };
        #endregion
    }
}
