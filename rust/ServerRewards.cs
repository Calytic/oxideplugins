using System;
using Oxide.Core;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.IO;


namespace Oxide.Plugins
{
    [Info("ServerRewards", "k1lly0u", "0.2.71", ResourceId = 1751)]
    public class ServerRewards : RustPlugin
    {
        #region fields
        [PluginReference] Plugin Kits;
        [PluginReference] Plugin Economics;
        [PluginReference] Plugin HumanNPC;
        [PluginReference] Plugin LustyMap;

        PlayerDataStorage playerData;
        private DynamicConfigFile PlayerData;

        RewardDataStorage rewardData;
        private DynamicConfigFile RewardData;

        ReferData referData;
        private DynamicConfigFile ReferralData;

        PermData permData;
        private DynamicConfigFile PermissionData;

        NPCDealers npcDealers;
        private DynamicConfigFile NPC_Dealers;

        static GameObject webObject;
        static UnityWeb uWeb;
        static MethodInfo getFileData = typeof(FileStorage).GetMethod("StorageGet", (BindingFlags.Instance | BindingFlags.NonPublic));

        TimeData timeData = new TimeData();
        ConfigData configData;

        private List<ulong> OpenUI = new List<ulong>();
        private Dictionary<ulong, Timer> RPTimers = new Dictionary<ulong, Timer>();        
        private Dictionary<string, string> ItemNames = new Dictionary<string, string>();


        string mainPanel = "RewardStore";
        string selectPanel = "SelectionBar";
        string rpPanelName = "PlayerRP";

        private static FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        #endregion

        #region Classes
        // Player Info
        class PlayerDataStorage
        {
            public Dictionary<ulong, SRInfo> Players = new Dictionary<ulong, SRInfo>();
        }
        class SRInfo
        {            
            public double PlayTime = 0;
            public int LastReward = 0;            
            public int RewardPoints = 0;
        }
        class ReferData
        {
            public List<ulong> ReferredPlayers = new List<ulong>();
        }
        //Reward Info
        class RewardDataStorage
        {
            public Dictionary<string, KitInfo> RewardKits = new Dictionary<string, KitInfo>();
            public Dictionary<int, ItemInfo> RewardItems = new Dictionary<int, ItemInfo>();
            public Dictionary<string, CommandInfo> RewardCommands = new Dictionary<string, CommandInfo>();
            public Dictionary<string, Dictionary<int, uint>> storedImages = new Dictionary<string, Dictionary<int, uint>>();
        }        
        class KitInfo
        {
            public string KitName;
            public string Description = "";
            public string URL = "";
            public int Cost;
        }
        class ItemInfo
        {
            public string DisplayName;
            public string URL;
            public int ID;
            public int Amount;            
            public int Skin;
            public int Cost;
        }
        class CommandInfo
        {
            public List<string> Command;
            public string Description;
            public int Cost;
        }        
        // Time Data
        class TimeData
        {
            public Dictionary<ulong, double> Players = new Dictionary<ulong, double>();
        }       
        // Permissions
        class PermData
        {
            public Dictionary<string, int> Permissions = new Dictionary<string, int>();
        }
        class NPCDealers
        {
            public Dictionary<string, NPCInfos> NPCIDs = new Dictionary<string, NPCInfos>();
        }
        class NPCInfos
        {
            public float X;
            public float Z;
            public float ID;
        }
        class KitItemEntry
        {
            public int ItemAmount;
            public List<string> ItemMods;
        }

        //UI
        class SR_UI
        {
            public static ConfigData configdata;
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax)
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overlay",
                    panelName
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {               
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },                        
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer container, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }
        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"light", "0.9 0.9 0.9 0.1" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"grey8", "0.8 0.8 0.8 1.0" }
        };
        #endregion

        #region UI Creation
        private void CategoryMenu(BasePlayer player)
        {            
            var Selector = SR_UI.CreateElementContainer(selectPanel, UIColors["dark"], "0 0.92", "1 1");
            SR_UI.CreatePanel(ref Selector, selectPanel, UIColors["light"], "0.01 0.05", "0.99 0.95", true);
            SR_UI.CreateLabel(ref Selector, selectPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("storeTitle", this, player.UserIDString)}</color>", 30, "0.05 0", "0.2 1");

            int number = 0;           
            if (!configData.Disable_Kits) { CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeKits", this, player.UserIDString), "UI_ChangeElement kits", number); number++; }
            if (!configData.Disable_Items) { CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeItems", this, player.UserIDString), "UI_ChangeElement items", number); number++; }
            if (!configData.Disable_Commands) { CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeCommands", this, player.UserIDString), "UI_ChangeElement commands", number); number++; }            
            if (Economics) if(!configData.Disable_CurrencyExchange) { CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeExchange", this, player.UserIDString), "UI_ChangeElement exchange", number); number++; }
            if (!configData.Disable_CurrencyTransfer) { CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeTransfer", this, player.UserIDString), "UI_ChangeElement transfer", number); number++; }
            CreateMenuButton(ref Selector, selectPanel, lang.GetMessage("storeClose", this, player.UserIDString), "UI_DestroyAll", number);
            //SR_UI.CreateButton(ref Selector, selectPanel, UIColors["buttonbg"], lang.GetMessage("storeClose", this, player.UserIDString), 18, "0.81 0.1", "0.93 0.9", "UI_DestroyAll");
            CuiHelper.AddUi(player, Selector);
            DisplayPoints(player);           
        }
        private void PopupMessage(BasePlayer player, string msg)
        {
            CuiHelper.DestroyUi(player, "PopupMsg");
            var element = SR_UI.CreateElementContainer("PopupMsg", UIColors["dark"], "0.33 0.45", "0.67 0.6");
            SR_UI.CreatePanel(ref element, "PopupMsg", UIColors["grey1"], "0.01 0.04", "0.99 0.96");
            SR_UI.CreateLabel(ref element, "PopupMsg", "", $"{configData.MSG_MainColor}{msg}</color>", 22, "0 0", "1 1");
            CuiHelper.AddUi(player, element);
            timer.Once(3.5f, () => CuiHelper.DestroyUi(player, "PopupMsg"));
        }
        private void DisplayPoints(BasePlayer player)
        {
            if (player == null) return;
            CuiHelper.DestroyUi(player, rpPanelName);
            if (!OpenUI.Contains(player.userID)) return;
            int playerPoints = 0;
            if (playerData.Players.ContainsKey(player.userID))
                playerPoints = playerData.Players[player.userID].RewardPoints;
            var element = SR_UI.CreateElementContainer(rpPanelName, "0 0 0 0", "0.3 0", "0.7 0.1");
            string message = $"{configData.MSG_MainColor}{lang.GetMessage("storeRP", this, player.UserIDString)}: {playerPoints}</color>";
            if (Economics)
            {
                var amount = Economics?.Call("GetPlayerMoney", player.userID);
                message = message + $" || {configData.MSG_MainColor}Economics: {amount}</color>";
            }
            if (configData.Use_Playtime)
                if (playerData.Players.ContainsKey(player.userID))
                {                    
                    message = $"{configData.MSG_MainColor}{lang.GetMessage("storePlaytime", this, player.UserIDString)}: {GetPlaytimeClock(player)}</color> || " + message;
                }
                        
            SR_UI.CreateLabel(ref element, rpPanelName, "0 0 0 0", message, 20, "0 0", "1 1", TextAnchor.MiddleCenter, 0f);
            CuiHelper.AddUi(player, element);
            timer.Once(1, () => DisplayPoints(player));
        }       
        private void KitsElement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var Main = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardKits;
            if (rew.Count > 10)
            {
                var maxpages = (rew.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeNext", this, player.UserIDString), 18, "0.84 0.05", "0.97 0.1", $"UI_ChangePageK {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeBack", this, player.UserIDString), 18, "0.03 0.05", "0.16 0.1", $"UI_ChangePageK {page - 1}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int rewardcount = 10 * page;
            List<string> kitNames = new List<string>();
            foreach (var entry in rewardData.RewardKits)
                kitNames.Add(entry.Key);

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                var contents = GetKitContents(rew[kitNames[n]].KitName);
                if (string.IsNullOrEmpty(contents) || !configData.ShowKitContents)
                    contents = rew[kitNames[n]].Description;
                CreateKitCommandEntry(ref Main, mainPanel, kitNames[n], contents, rew[kitNames[n]].Cost, i, true);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("noKits", this, player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            CuiHelper.AddUi(player, Main);
            //DisplayPoints(player);
        }
        private void ItemElement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var Main = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardItems;   
            if (rew.Count > 21)
            {
                var maxpages = (rew.Count - 1) / 21 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeNext", this, player.UserIDString), 18, "0.84 0.05", "0.97 0.1", $"UI_ChangePageI {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeBack", this, player.UserIDString), 18, "0.03 0.05", "0.16 0.1", $"UI_ChangePageI {page - 1}");
            }
            int maxentries = (21 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int i = 0;
            int rewardcount = 21 * page;
            for (int n = rewardcount; n < maxentries; n++ )            
            {
                CreateItemEntry(ref Main, mainPanel, rew[n].DisplayName, n, rew[n].Amount, i);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("noItems", this, player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            CuiHelper.AddUi(player, Main);
            //DisplayPoints(player);

        }
        private void CommandElement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var Main = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardCommands;
            if (rew.Count > 10)
            {
                var maxpages = (rew.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeNext", this, player.UserIDString), 18, "0.84 0.05", "0.97 0.1", $"UI_ChangePageC {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeBack", this, player.UserIDString), 18, "0.03 0.05", "0.16 0.1", $"UI_ChangePageC {page - 1}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int rewardcount = 10 * page;
            List<string> commNames = new List<string>();
            foreach (var entry in rewardData.RewardCommands)
                commNames.Add(entry.Key);

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                CreateKitCommandEntry(ref Main, mainPanel, commNames[n], rew[commNames[n]].Description, rew[commNames[n]].Cost, i, false);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("noCommands", this, player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");

            CuiHelper.AddUi(player, Main);
            //DisplayPoints(player);
        }
        private void ExchangeElement(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var Main = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("exchange1", this, player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_Color}{lang.GetMessage("exchange2", this, player.UserIDString)}</color>{configData.MSG_MainColor}{configData.RP_ExchangeRate} {lang.GetMessage("storeRP", this, player.UserIDString)}</color> -> {configData.MSG_MainColor}{configData.Econ_ExchangeRate} {lang.GetMessage("storeCoins", this, player.UserIDString)}</color>", 20, "0 0.6", "1 0.7");
            SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("storeRP", this, player.UserIDString)} => {lang.GetMessage("storeEcon", this, player.UserIDString)}</color>", 20, "0.25 0.4", "0.4 0.55");
            SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("storeEcon", this, player.UserIDString)} => {lang.GetMessage("storeRP", this, player.UserIDString)}</color>", 20, "0.6 0.4", "0.75 0.55");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeExchange", this, player.UserIDString), 20, "0.25 0.3", "0.4 0.38", "UI_Exchange 1");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeExchange", this, player.UserIDString), 20, "0.6 0.3", "0.75 0.38", "UI_Exchange 2");
            CuiHelper.AddUi(player, Main);
        }
        #region Transfer System
        private void CreateTransferElement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var HelpMain = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref HelpMain, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            SR_UI.CreateLabel(ref HelpMain, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("transfer1", this, player.UserIDString)}</color>", 22, "0 0.9", "1 1");

            var playerCount = BasePlayer.activePlayerList.Count;
            if (playerCount > 96)
            {
                var maxpages = (playerCount - 1) / 96 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref HelpMain, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeNext", this, player.UserIDString), 18, "0.87 0.92", "0.97 0.97", $"SRUI_Transfer {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref HelpMain, mainPanel, UIColors["buttonbg"], lang.GetMessage("storeBack", this,  player.UserIDString), 18, "0.03 0.92", "0.13 0.97", $"SRUI_Transfer {page - 1}");
            }
            int maxentries = (96 * (page + 1));
            if (maxentries > playerCount)
                maxentries = playerCount;
            int rewardcount = 96 * page;

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                if (BasePlayer.activePlayerList[n] == null) continue;
                CreatePlayerNameEntry(ref HelpMain, mainPanel, BasePlayer.activePlayerList[n].displayName, BasePlayer.activePlayerList[n].UserIDString, i);
                i++;
            }            
            CuiHelper.AddUi(player, HelpMain);
        }
        private void TransferElement(BasePlayer player, string name, string id)
        {
            CuiHelper.DestroyUi(player, mainPanel);
            var Main = SR_UI.CreateElementContainer(mainPanel, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, mainPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);

            SR_UI.CreateLabel(ref Main, mainPanel, "", $"{configData.MSG_MainColor}{lang.GetMessage("transfer2", this, player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], "1", 20, "0.27 0.3", "0.37 0.38", $"SRUI_TransferID {id} {name} 1");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], "10", 20, "0.39 0.3", "0.49 0.38", $"SRUI_TransferID {id} {name} 10");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], "100", 20, "0.51 0.3", "0.61 0.38", $"SRUI_TransferID {id} {name} 100");
            SR_UI.CreateButton(ref Main, mainPanel, UIColors["buttonbg"], "1000", 20, "0.63 0.3", "0.73 0.38", $"SRUI_TransferID {id} {name} 1000");

            CuiHelper.AddUi(player, Main);
            
        }
        private void CreatePlayerNameEntry(ref CuiElementContainer container, string panelName, string name, string id, int number)
        {
            var pos = CalcPlayerNamePos(number);
            SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], name, 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"SRUI_TransferNext {id} {name}");
        }
        private float[] CalcPlayerNamePos(int number)
        {
            Vector2 position = new Vector2(0.014f, 0.82f);
            Vector2 dimensions = new Vector2(0.12f, 0.055f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 8)
            {
                offsetX = (0.002f + dimensions.x) * number;
            }
            if (number > 7 && number < 16)
            {
                offsetX = (0.002f + dimensions.x) * (number - 8);
                offsetY = (-0.0055f - dimensions.y) * 1;
            }
            if (number > 15 && number < 24)
            {
                offsetX = (0.002f + dimensions.x) * (number - 16);
                offsetY = (-0.0055f - dimensions.y) * 2;
            }
            if (number > 23 && number < 32)
            {
                offsetX = (0.002f + dimensions.x) * (number - 24);
                offsetY = (-0.0055f - dimensions.y) * 3;
            }
            if (number > 31 && number < 40)
            {
                offsetX = (0.002f + dimensions.x) * (number - 32);
                offsetY = (-0.0055f - dimensions.y) * 4;
            }
            if (number > 39 && number < 48)
            {
                offsetX = (0.002f + dimensions.x) * (number - 40);
                offsetY = (-0.0055f - dimensions.y) * 5;
            }
            if (number > 47 && number < 56)
            {
                offsetX = (0.002f + dimensions.x) * (number - 48);
                offsetY = (-0.0055f - dimensions.y) * 6;
            }
            if (number > 55 && number < 64)
            {
                offsetX = (0.002f + dimensions.x) * (number - 56);
                offsetY = (-0.0055f - dimensions.y) * 7;
            }
            if (number > 63 && number < 72)
            {
                offsetX = (0.002f + dimensions.x) * (number - 64);
                offsetY = (-0.0055f - dimensions.y) * 8;
            }
            if (number > 71 && number < 80)
            {
                offsetX = (0.002f + dimensions.x) * (number - 72);
                offsetY = (-0.0055f - dimensions.y) * 9;
            }
            if (number > 79 && number < 88)
            {
                offsetX = (0.002f + dimensions.x) * (number - 80);
                offsetY = (-0.0055f - dimensions.y) * 10;
            }
            if (number > 87 && number < 96)
            {
                offsetX = (0.002f + dimensions.x) * (number - 88);
                offsetY = (-0.0055f - dimensions.y) * 11;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }       
       
        #endregion

        #region Item Entries
        private void CreateMenuButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {            
            Vector2 dimensions = new Vector2(0.1f, 0.6f);
            Vector2 origin = new Vector2(0.25f, 0.2f);
            Vector2 offset = new Vector2((0.01f + dimensions.x) * number, 0);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 16, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }
        private void CreateKitCommandEntry(ref CuiElementContainer container, string panelName, string name, string description, int cost, int number, bool kit)
        {
            Vector2 dimensions = new Vector2(0.8f, 0.079f);
            Vector2 origin = new Vector2(0.03f, 0.86f);
            float offsetY = (0.004f + dimensions.y) * number; 
            Vector2 offset = new Vector2(0, offsetY);
            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;
            string command;
            if (kit)
            {
                command = $"UI_BuyKit {name}";
                if (!string.IsNullOrEmpty(rewardData.RewardKits[name].URL))
                {                    
                    string fileLocation = rewardData.storedImages[999999999.ToString()][0].ToString();
                    if (rewardData.storedImages.ContainsKey(rewardData.RewardKits[name].KitName))
                        fileLocation = rewardData.storedImages[rewardData.RewardKits[name].KitName][0].ToString();

                    SR_UI.LoadImage(ref container, panelName, fileLocation, $"{posMin.x} {posMin.y}", $"{posMin.x + 0.05} {posMax.y}");                    
                }
                posMin.x = 0.09f;
            }
            else command = $"UI_BuyCommand {name}";
            SR_UI.CreateLabel(ref container, panelName, "", $"{configData.MSG_MainColor}{name}</color> -- {configData.MSG_Color}{description}</color>", 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", TextAnchor.UpperLeft);
            SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], $"{lang.GetMessage("storeCost", this)}: {cost}", 18, $"0.84 {posMin.y + 0.015}", $"0.97 {posMax.y - 0.015f}", command);
        }       
        private void CreateItemEntry(ref CuiElementContainer container, string panelName, string itemname, int itemnumber, int amount, int number)
        {
            if (rewardData.RewardItems.ContainsKey(itemnumber))
            {                
                var item = rewardData.RewardItems[itemnumber];
                Vector2 dimensions = new Vector2(0.13f, 0.24f);
                Vector2 origin = new Vector2(0.03f, 0.7f);
                float offsetY = 0;
                float offsetX = 0;                
                switch (number)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        offsetX = (0.005f + dimensions.x) * number;
                        break;
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        {
                            offsetX = (0.005f + dimensions.x) * (number - 7);
                            offsetY = (0.02f + dimensions.y) * 1;
                        }
                        break;
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                        {
                            offsetX = (0.005f + dimensions.x) * (number - 14);
                            offsetY = (0.02f + dimensions.y) * 2;
                        }
                        break;
                }
                Vector2 offset = new Vector2(offsetX, -offsetY);
                Vector2 posMin = origin + offset;
                Vector2 posMax = posMin + dimensions;

                string fileLocation = rewardData.storedImages[999999999.ToString()][0].ToString();
                if (rewardData.storedImages.ContainsKey(item.ID.ToString()))
                    fileLocation = rewardData.storedImages[item.ID.ToString()][item.Skin].ToString();

                SR_UI.LoadImage(ref container, panelName, fileLocation, $"{posMin.x + 0.02} {posMin.y + 0.08}", $"{posMax.x - 0.02} {posMax.y}");
                if (amount > 1)
                    SR_UI.CreateTextOverlay(ref container, panelName, $"{configData.MSG_MainColor}<size=18>X</size><size=20>{amount}</size></color>", "", 20, $"{posMin.x + 0.02} {posMin.y + 0.1}", $"{posMax.x - 0.02} {posMax.y - 0.1}", TextAnchor.MiddleCenter);
                SR_UI.CreateLabel(ref container, panelName, "", itemname, 16, $"{posMin.x} {posMin.y + 0.05}", $"{posMax.x} {posMin.y + 0.08}");
                SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], $"{lang.GetMessage("storeCost", this)}: {item.Cost}", 16, $"{posMin.x + 0.015} {posMin.y}", $"{posMax.x - 0.015} {posMin.y + 0.05}", $"UI_BuyItem {itemnumber}");
            }
        }
        
        #endregion

        #region UI Commands
        [ConsoleCommand("UI_BuyKit")]
        private void cmdBuyKit(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var kitName = arg.ArgsStr;
            if (rewardData.RewardKits.ContainsKey(kitName))
            {
                var kit = rewardData.RewardKits[kitName];
                if (playerData.Players.ContainsKey(player.userID))
                {
                    var pd = playerData.Players[player.userID];
                    if (pd.RewardPoints >= kit.Cost)
                    {
                        if (TakePoints(player.userID, kit.Cost, "Kit " + kit.KitName) != null)
                        {
                            Kits?.Call("GiveKit", new object[] { player, kit.KitName });
                            PopupMessage(player, string.Format(lang.GetMessage("buyKit", this, player.UserIDString), kitName));
                            return;
                        }
                    }                    
                }
                PopupMessage(player, lang.GetMessage("notEnoughPoints", this, player.UserIDString));
                return;
            }
            PopupMessage(player, lang.GetMessage("errorKit", this, player.UserIDString));
            return;
        }
        [ConsoleCommand("UI_BuyCommand")]
        private void cmdBuyCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var commandname = arg.ArgsStr;
            if (rewardData.RewardCommands.ContainsKey(commandname))
            {
                var command = rewardData.RewardCommands[commandname];
                if (playerData.Players.ContainsKey(player.userID))
                {
                    var pd = playerData.Players[player.userID];
                    if (pd.RewardPoints >= command.Cost)
                    {
                        if (TakePoints(player.userID, command.Cost, "Command") != null)
                        {
                            foreach (var cmd in command.Command)
                                ConsoleSystem.Run.Server.Normal(cmd.Replace("$player.id", player.UserIDString).Replace("$player.name", player.displayName).Replace("$player.x", player.transform.position.x.ToString()).Replace("$player.y", player.transform.position.y.ToString()).Replace("$player.z", player.transform.position.z.ToString()));

                            PopupMessage(player, string.Format(lang.GetMessage("buyCommand", this, player.UserIDString), commandname));
                            return;
                        }
                    }
                }
                PopupMessage(player, lang.GetMessage("notEnoughPoints", this, player.UserIDString));
                return;
            }
            PopupMessage(player, lang.GetMessage("errorCommand", this, player.UserIDString));
            return;
        }
        [ConsoleCommand("UI_BuyItem")]
        private void cmdBuyItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var itemname = int.Parse(arg.GetString(0).Replace("'", ""));
            if (rewardData.RewardItems.ContainsKey(itemname))
            {
                var item = rewardData.RewardItems[itemname];
                if (playerData.Players.ContainsKey(player.userID))
                {
                    var pd = playerData.Players[player.userID];
                    if (player.inventory.containerMain.itemList.Count == 24)
                    {
                        PopupMessage(player, lang.GetMessage("fullInv", this, player.UserIDString));
                        return;
                    }
                    if (pd.RewardPoints >= item.Cost)
                    {
                        if (TakePoints(player.userID, item.Cost, item.DisplayName) != null)
                        {
                            GiveItem(player, itemname);
                            PopupMessage(player, string.Format(lang.GetMessage("buyItem", this, player.UserIDString), item.Amount, item.DisplayName));
                            return;
                        }
                    }
                }
                PopupMessage(player, lang.GetMessage("notEnoughPoints", this, player.UserIDString));
                return;
            }
            PopupMessage(player, lang.GetMessage("errorItem", this, player.UserIDString));
            return;
        }        
        [ConsoleCommand("UI_ChangeElement")]
        private void cmdChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var panelName = arg.GetString(0).Replace("'", "");
            switch (panelName)
            {
                case "kits":
                    KitsElement(player);
                    return;
                case "commands":
                    CommandElement(player);
                    return;
                case "items":
                    ItemElement(player);
                    return;
                case "exchange":
                    ExchangeElement(player);
                    return;
                case "transfer":
                    CreateTransferElement(player);
                    return;              
            }
        }
        [ConsoleCommand("UI_ChangePageI")]
        private void cmdChangeItemPage(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, mainPanel);            
            var pageNumber = arg.GetString(0).Replace("'", "");
            ItemElement(player, int.Parse(pageNumber));
        }
        [ConsoleCommand("UI_ChangePageK")]
        private void cmdChangeKitPage(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, mainPanel);
            var pageNumber = arg.GetString(0).Replace("'", "");
            KitsElement(player, int.Parse(pageNumber));
        }
        [ConsoleCommand("UI_ChangePageC")]
        private void cmdChangeCommandPage(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, mainPanel);
            var pageNumber = arg.GetString(0).Replace("'", "");
            CommandElement(player, int.Parse(pageNumber));
        }
        [ConsoleCommand("UI_Exchange")]
        private void cmdExchange(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;            
            var type = int.Parse(arg.GetString(0).Replace("'", ""));
            if (!playerData.Players.ContainsKey(player.userID))
            {
                PopupMessage(player, lang.GetMessage("noRP", this, player.UserIDString));
                return;
            }
            if (type == 1)
            {
                if (playerData.Players[player.userID].RewardPoints < configData.RP_ExchangeRate)
                {
                    PopupMessage(player, lang.GetMessage("notEnoughPoints", this, player.UserIDString));
                    return;
                }
                if (TakePoints(player.userID, configData.RP_ExchangeRate, "RP Exchange") != null)
                {
                    Economics.Call("Deposit", player.userID, (double)configData.Econ_ExchangeRate);
                    PopupMessage(player, $"{lang.GetMessage("exchange", this, player.UserIDString)}{configData.RP_ExchangeRate} {lang.GetMessage("storeRP", this, player.UserIDString)} for {configData.Econ_ExchangeRate} {lang.GetMessage("storeCoins", this, player.UserIDString)}");
                }
            }
            else
            {
                var amount = Convert.ToSingle(Economics?.Call("GetPlayerMoney", player.userID));
                if (amount < configData.Econ_ExchangeRate)
                {
                    PopupMessage(player, lang.GetMessage("notEnoughCoins", this, player.UserIDString));
                    return;
                }
                Economics?.Call("Withdraw", player.userID, (double)configData.Econ_ExchangeRate);
                AddPoints(player.userID, configData.RP_ExchangeRate);
                PopupMessage(player, $"{lang.GetMessage("exchange", this, player.UserIDString)}{configData.Econ_ExchangeRate} {lang.GetMessage("storeCoins", this, player.UserIDString)} for {configData.RP_ExchangeRate} {lang.GetMessage("storeRP", this, player.UserIDString)}");
            }
        }
        [ConsoleCommand("SRUI_Transfer")]
        private void ccmdTransfer(ConsoleSystem.Arg args)
        {
            var player = args.connection.player as BasePlayer;
            if (player == null)
                return;
            var type = int.Parse(args.GetString(0));
            CreateTransferElement(player, type);
        }
        [ConsoleCommand("SRUI_TransferNext")]
        private void ccmdTransferNext(ConsoleSystem.Arg args)
        {
            var player = args.connection.player as BasePlayer;
            if (player == null)
                return;
            var ID = args.GetString(0);
            var name = args.GetString(1);
            TransferElement(player, name, ID);            
        }
        [ConsoleCommand("SRUI_TransferID")]
        private void ccmdTransferID(ConsoleSystem.Arg args)
        {
            var player = args.connection.player as BasePlayer;
            if (player == null)
                return;
            var ID = ulong.Parse(args.GetString(0));
            var name = args.GetString(1);
            var amount = int.Parse(args.GetString(2));
            if ((int)CheckPoints(player.userID) >= amount)
            {
                if (TakePoints(player.userID, amount) != null)
                {
                    AddPoints(ID, amount);
                    PopupMessage(player, string.Format(lang.GetMessage("transfer3", this), amount, lang.GetMessage("storeRP", this), name));
                }
            }
            else
            {
                PopupMessage(player, lang.GetMessage("notEnoughPoints", this));
            }
        }
        [ConsoleCommand("UI_DestroyAll")]
        private void cmdDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;            
            DestroyUI(player);
        }
        #endregion
        private void DestroyUI(BasePlayer player)
        {
            if (OpenUI.Contains(player.userID))
            {                
                OpenUI.Remove(player.userID);
            }
            CuiHelper.DestroyUi(player, selectPanel);
            CuiHelper.DestroyUi(player, mainPanel);
            CuiHelper.DestroyUi(player, rpPanelName);
            OpenMap(player);
        }
        #endregion

        #region Oxide Hooks  
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_players");
            RewardData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_rewards");
            ReferralData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_referrals");
            PermissionData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_permissions");
            NPC_Dealers = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_npcids");
        }
        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            uWeb = webObject.AddComponent<UnityWeb>();
            uWeb.Add("http://i.imgur.com/zq9zuKw.jpg", "999999999", 0);

            if (!Kits) PrintWarning($"Kits could not be found! Unable to issue kit rewards"); 
                      
            LoadData();
            LoadVariables();
            SR_UI.configdata = configData;

            foreach (var item in ItemManager.itemList)
            {
                if (!ItemNames.ContainsKey(item.itemid.ToString()))
                    ItemNames.Add(item.itemid.ToString(), item.displayName.translated);
            }
            foreach (var perm in permData.Permissions)
                permission.RegisterPermission(perm.Key, this);
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);

            timer.Once(configData.Save_Interval * 60, () => SaveLoop());            
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                DestroyUI(player);
                var ID = player.userID;                
                if (configData.Use_Playtime)
                    InitPlayerData(player);
                if (playerData.Players.ContainsKey(ID))
                    if (playerData.Players[ID].RewardPoints > 0)
                        msgOutstanding(player);
            }           
        }
        void OnPlayerDisconnected(BasePlayer player) => DestroyPlayer(player);
        
        void DestroyPlayer(BasePlayer player)
        {
            if (player == null) return;
            if (RPTimers.ContainsKey(player.userID))
                RPTimers[player.userID].Destroy();            
            DestroyUI(player);
            SavePlayersData(player);
            if (timeData.Players.ContainsKey(player.userID))
                timeData.Players.Remove(player.userID);
        }
        void Unload()
        {            
            foreach (var player in BasePlayer.activePlayerList)
                DestroyPlayer(player);
            SaveData(true);
        }
        #endregion

        #region Functions  
        private void InitPlayerData(BasePlayer player)
        {
            var ID = player.userID;
            if (!timeData.Players.ContainsKey(ID))            
                timeData.Players.Add(ID, GrabCurrentTime());                        
            ResetTimer(player, true);
        }
        private void SavePlayersData(BasePlayer player)
        {
            var ID = player.userID;            

            if (!playerData.Players.ContainsKey(ID))
                playerData.Players.Add(ID, new SRInfo());
            if (configData.Use_Playtime)
            {
                var time = GrabCurrentTime();
                playerData.Players[ID].PlayTime += (time - timeData.Players[player.userID]);
                timeData.Players[player.userID] = time;
            }
        }
        private void SaveActivePlayer(BasePlayer player)
        {
            if (configData.Use_Playtime)
            {
                SavePlayersData(player);
                if (player != null)
                {
                    CheckForReward(player);
                }
            }
        }       
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMinutes;        
        private void CheckForReward(BasePlayer player)
        {
            var ID = player.userID;           
            if ((playerData.Players[ID].PlayTime - playerData.Players[player.userID].LastReward) >= configData.PT_Point_Interval)
            {
                var points = configData.Points_Time;
                var hasPerm = HasPermission(player);
                if (hasPerm > 0) points = hasPerm;
                playerData.Players[player.userID].LastReward = (int)playerData.Players[ID].PlayTime;                
                AddPoints(player.userID, points);           
            }
            ResetTimer(player);
            if (playerData.Players[ID].RewardPoints > 0)
                if (configData.MSG_OutstandingPoints)
                    if (IsDivisble(playerData.Players[ID].RewardPoints))
                        msgOutstanding(player);            
        }   
        private void ResetTimer(BasePlayer player, bool logIn = false)
        {
            float Time = configData.PT_Point_Interval * 60;
            if (logIn)
                if (playerData.Players.ContainsKey(player.userID))
                {
                    var p = playerData.Players[player.userID];
                    var time = (p.PlayTime - p.LastReward) * 60;                            
                    Time = Time - (float)time;
                }
            if (!RPTimers.ContainsKey(player.userID))            
                RPTimers.Add(player.userID, timer.Once(Time, () => SaveActivePlayer(player)));
            
            else RPTimers[player.userID] = timer.Once(Time, () => SaveActivePlayer(player));
        }
        private void msgOutstanding(BasePlayer player)
        {
            var outstanding = playerData.Players[player.userID].RewardPoints;
            SendMSG(player, string.Format(lang.GetMessage("msgOutRewards1", this, player.UserIDString), outstanding));
        }
        private string GetPlaytimeClock(BasePlayer player)
        {
            if (player == null) return null;
            TimeSpan dateDifference = TimeSpan.FromMinutes(playerData.Players[player.userID].PlayTime + (GrabCurrentTime() - timeData.Players[player.userID]));
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)            
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid)
                            return p;
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))                    
                        foundPlayers.Add(p);                    
                }            
            if (foundPlayers.Count == 0)            
                foreach (var sleeper in BasePlayer.sleepingPlayerList)                
                    if (sleeper != null)
                    {
                        if (steamid != 0L)
                            if (sleeper.userID == steamid)
                                return sleeper;
                        string lowername = sleeper.displayName.ToLower();
                        if (lowername.Contains(lowerarg))                        
                            foundPlayers.Add(sleeper);
                    }            
            if (foundPlayers.Count == 0)
            {
                if (player != null)
                    SendMSG(player, lang.GetMessage("noPlayers", this, player.UserIDString));
                return null;
            }
            if (foundPlayers.Count > 1)
            {
                if (player != null)
                    SendMSG(player, lang.GetMessage("multiPlayers", this, player.UserIDString));
                return null;
            }

            return foundPlayers[0];
        }
        private void SendMSG(BasePlayer player, string msg, string keyword = "title")
        {
            if (keyword == "title") keyword = lang.GetMessage("title", this, player.UserIDString);
            SendReply(player, configData.MSG_MainColor + keyword + "</color>" + configData.MSG_Color + msg + "</color>");
        }
        [HookMethod("AddPoints")]
        public object AddPoints(ulong ID, int amount)
        {            
            if (!playerData.Players.ContainsKey(ID))
                playerData.Players.Add(ID, new SRInfo());            
            playerData.Players[ID].RewardPoints += amount;

            if (configData.LogRPTransactions)
            {
                BasePlayer player = BasePlayer.FindByID(ID);
                var message = $"ADD - (offline){ID} has been issued {amount}x RP";

                if (player != null)
                    message = $"ADD - {ID} - {player.displayName} has been issued {amount}x RP";

                var dateTime = DateTime.Now.ToString("yyyy-MM-dd");
                ConVar.Server.Log($"oxide/logs/ServerRewards - EarntRP_{dateTime}.txt", message);
            }            
            return true;
        }
        [HookMethod("TakePoints")]
        public object TakePoints(ulong ID, int amount, string item = "")
        {
            
            if (!playerData.Players.ContainsKey(ID)) return null;
            playerData.Players[ID].RewardPoints -= amount;
            if (configData.LogRPTransactions)
            {
                BasePlayer player = BasePlayer.FindByID(ID);
                var message = $"TAKE - (offline){ID} has used {amount}x RP";

                if (player != null)
                    message = $"TAKE - {ID} - {player.displayName} has used {amount}x RP";
                if (!string.IsNullOrEmpty(item))
                    message = message + $" on: {item}";
                if (player != null)
                    message = message + $"\nInventory Count's:\n Belt: {player.inventory.containerBelt.itemList.Count}\n Main: {player.inventory.containerMain.itemList.Count}\n Wear: {player.inventory.containerWear.itemList.Count} ";

                var dateTime = DateTime.Now.ToString("yyyy-MM-dd");
                ConVar.Server.Log($"oxide/logs/ServerRewards-SpentRP_{dateTime}.txt", message);
            }
            return true;
        }
        [HookMethod("CheckPoints")]
        public object CheckPoints(ulong ID)
        {
            if (!playerData.Players.ContainsKey(ID)) return null;
            return playerData.Players[ID].RewardPoints;
        }
        object ClearPlayerData(BasePlayer target, ulong ID = 0U)
        {
            ulong userID;
            string name;
            if (target != null)
            {
                userID = target.userID;
                name = target.displayName;
            }
            else
            {
                userID = ID;
                name = $"{ID}";
            }
            if (RemovePlayer(userID)) return string.Format(lang.GetMessage("clearPlayer", this), name);
            else return lang.GetMessage("noPlayers", this);
        }
        private bool RemovePlayer(ulong ID)
        {
            if (playerData.Players.ContainsKey(ID))
            {
                playerData.Players.Remove(ID);
                return true;
            }
            return false;           
        }
        private bool IsDivisble(int x) => (x % configData.MSG_DisplayEvery) == 0;
        private int HasPermission(BasePlayer player)
        {
            foreach(var entry in permData.Permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, entry.Key))
                    return entry.Value;
            }
            return -1;
        }
        private void GiveItem(BasePlayer player, int itemkey)
        {
            if (rewardData.RewardItems.ContainsKey(itemkey))
            {
                var entry = rewardData.RewardItems[itemkey];
                Item item = ItemManager.CreateByItemID(entry.ID, entry.Amount, entry.Skin);
                item.MoveToContainer(player.inventory.containerMain);
            }
        }
        void SendEchoConsole(Network.Connection cn, string msg)
        {
            if (Network.Net.sv.IsConnected())
            {
                Network.Net.sv.write.Start();
                Network.Net.sv.write.PacketID(Network.Message.Type.ConsoleMessage);
                Network.Net.sv.write.String(msg);
                Network.Net.sv.write.Send(new Network.SendInfo(cn));
            }
        }
        private BasePlayer FindEntity(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            var rayResult = Ray(player, currentRot);
            if (rayResult is BasePlayer)
            {
                var ent = rayResult as BasePlayer;
                return ent;
            }
            return null;
        }
        private object Ray(BasePlayer player, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(player.transform.position + new Vector3(0f, 1.5f, 0f), Aim);
            float distance = 50f;
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BaseEntity>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BaseEntity>();
                    }
                }
            }
            return target;
        }
        private string isNPCRegistered(string ID)
        {
            if (npcDealers.NPCIDs.ContainsKey(ID)) return lang.GetMessage("npcExist", this);
            
            return null;
        }
        private void OpenStore(BasePlayer player, bool isNPC = false)
        {            
            SavePlayersData(player);
            OpenUI.Add(player.userID);
            CloseMap(player);
            CategoryMenu(player);
            if (!configData.Disable_Kits)
                KitsElement(player);
            else if (!configData.Disable_Items)
                ItemElement(player);
            else if (!configData.Disable_Commands)
                CommandElement(player);
            else
            {
                OpenUI.Remove(player.userID);
                timer.Once(3.5f, () => { DestroyUI(player); OpenMap(player); });
                PopupMessage(player, "    All reward options are currently disabled. Closing the store.");
            }
        }        
        private string GetKitContents(string kitname)
        {
            var contents = Kits?.Call("GetKitContents", kitname);
            if (contents != null)
            {
                var itemString = "";
                var itemList = new SortedDictionary<string, KitItemEntry>();

                foreach (var item in (string[])contents)
                {
                    var entry = item.Split('_');
                    var name = ItemNames[entry[0]];
                    var amount = int.Parse(entry[1]);
                    var mods = new List<string>();

                    if (entry.Length > 2)
                        for (int i = 2; i < entry.Length; i++)
                        {     
                            mods.Add(ItemNames[entry[i]]);
                        }

                    if (itemList.ContainsKey(name))
                        itemList[name].ItemAmount += amount;
                    else itemList.Add(name, new KitItemEntry { ItemAmount = amount, ItemMods = mods });                    
                }
                int eCount = 0;
                foreach (var entry in itemList)
                {
                    itemString = itemString + $"{entry.Value.ItemAmount}x {entry.Key}";
                    if (entry.Value.ItemMods.Count > 0)
                    {
                        itemString = itemString + " (";
                        int i = 0;
                        foreach (var mod in entry.Value.ItemMods)
                        {
                            itemString = itemString + $"{mod}";
                            if (i < entry.Value.ItemMods.Count - 1)
                                itemString = itemString + ", ";
                            i++;
                        }
                        itemString = itemString + "), ";
                    }
                    else if (eCount < itemList.Count - 1)
                        itemString = itemString + ", ";
                    eCount++;
                }
                return itemString;
            }
            return null;
        }
        #endregion

        #region External Calls
        private void CloseMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("DisableMaps", player);
            }
        }
        private void OpenMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("EnableMaps", player);
            }
        }
        private void AddMapMarker(float x, float z, string name, string icon = "rewarddealer")
        {
            if (LustyMap)
            {
                LustyMap.Call("AddMarker", x, z, name, icon);
                LustyMap.Call("addCustom", icon);
                LustyMap.Call("cacheImages");
            }
        }
        private void RemoveMapMarker(string name)
        {
            if (LustyMap)
                LustyMap.Call("RemoveMarker", name);
        }
        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (player == null || npc == null) return;
            var npcID = npc.UserIDString;
            if (npcDealers.NPCIDs.ContainsKey(npcID))
            {
                OpenStore(player, true);
            }
        }
        #endregion        

        #region Chat Commands 
        [ChatCommand("s")]
        private void cmdStore(BasePlayer player, string command, string[] args)
        {
            if ((configData.NPCDealers_Only && isAuth(player)) || !configData.NPCDealers_Only)
            {
                OpenStore(player);
            }            
        }
        [ChatCommand("rewards")]
        private void cmdRewards(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "V " + Version, lang.GetMessage("title", this, player.UserIDString));                
                SendMSG(player, lang.GetMessage("chatCheck1", this, player.UserIDString), lang.GetMessage("chatCheck", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("storeSyn2", this, player.UserIDString), lang.GetMessage("storeSyn21", this, player.UserIDString));
                if (isAuth(player))
                {
                    SendMSG(player, lang.GetMessage("chatAddKit", this, player.UserIDString), lang.GetMessage("addSynKit", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatAddItem", this, player.UserIDString), lang.GetMessage("addSynItem", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatAddCommand", this, player.UserIDString), lang.GetMessage("addSynCommand", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRemove", this, player.UserIDString), lang.GetMessage("remSynKit", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRemove", this, player.UserIDString), lang.GetMessage("remSynItem", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRemove", this, player.UserIDString), lang.GetMessage("remSynCommand", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatList1", this, player.UserIDString), lang.GetMessage("chatList", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("permAdd2", this, player.UserIDString), lang.GetMessage("permAdd1", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("permRem2", this, player.UserIDString), lang.GetMessage("permRem1", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("permList2", this, player.UserIDString), lang.GetMessage("permList1", this, player.UserIDString));
                }
                return;
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "check":
                        if (!playerData.Players.ContainsKey(player.userID))
                        {
                            SendMSG(player, lang.GetMessage("errorProfile", this, player.UserIDString));
                            Puts(lang.GetMessage("errorPCon", this, player.UserIDString), player.displayName);
                            return;
                        }
                        SavePlayersData(player);
                        int points = playerData.Players[player.userID].RewardPoints;
                        if (configData.Use_Playtime)
                            SendMSG(player, string.Format(lang.GetMessage("pointsAvail", this, player.UserIDString), GetPlaytimeClock(player), points));
                        else SendMSG(player, string.Format(lang.GetMessage("tpointsAvail", this, player.UserIDString), points));
                        return;
                    case "list":
                        if (isAuth(player))
                            foreach (var entry in rewardData.RewardItems)
                            {
                                SendEchoConsole(player.net.connection, string.Format("ID: {0} - Type: {1} - Amount: {2} - Cost: {3}", entry.Key, entry.Value.DisplayName, entry.Value.Amount, entry.Value.Cost));
                            }
                        return;                    
                    case "add":
                        if (args.Length >= 2)
                            if (isAuth(player))
                            {
                                switch (args[1].ToLower())
                                {
                                    case "kit":
                                        if (args.Length == 5)
                                        {
                                            int i = -1;
                                            int.TryParse(args[4], out i);
                                            if (i <= 0) { SendMSG(player, lang.GetMessage("noCost", this, player.UserIDString)); return; }

                                            object isKit = Kits?.Call("isKit", new object[] { args[3] });
                                            if (isKit is bool)
                                                if ((bool)isKit)
                                                {
                                                    if (!rewardData.RewardKits.ContainsKey(args[2]))
                                                        rewardData.RewardKits.Add(args[2], new KitInfo() { KitName = args[3], Cost = i, Description = "" });
                                                    else
                                                    {
                                                        SendMSG(player, string.Format(lang.GetMessage("rewardExisting", this, player.UserIDString), args[2]));
                                                        return;
                                                    }
                                                    SendMSG(player, string.Format(lang.GetMessage("addSuccess", this, player.UserIDString), "kit", args[2], i));
                                                    SaveRewards();
                                                    return;
                                                }
                                            SendMSG(player, lang.GetMessage("noKit", this, player.UserIDString), "");
                                            return;
                                        }
                                        SendMSG(player, "", lang.GetMessage("addSynKit", this, player.UserIDString));
                                        return;
                                    case "item":
                                        if (args.Length >= 3)
                                        {
                                            int i = -1;
                                            int.TryParse(args[2], out i);
                                            if (i <= 0) { SendMSG(player, lang.GetMessage("noCost", this, player.UserIDString)); return; }
                                            if (player.GetActiveItem() != null)
                                            {
                                                Item item = player.GetActiveItem();
                                                if (item == null)
                                                {
                                                    SendMSG(player, "", "Unable to get the required item information");
                                                    return;
                                                }
                                                ItemInfo newItem = new ItemInfo
                                                {
                                                    Amount = item.amount,                                                    
                                                    Cost = i,
                                                    DisplayName = item.info.displayName.english,
                                                    ID = item.info.itemid,
                                                    Skin = item.skin,
                                                    URL = ""
                                                };                                                
                                                rewardData.RewardItems.Add(rewardData.RewardItems.Count, newItem);
                                                SendMSG(player, string.Format(lang.GetMessage("addSuccess", this, player.UserIDString), "item", newItem.DisplayName, i));
                                                SaveRewards();
                                                return;
                                            }
                                            SendMSG(player, "", lang.GetMessage("itemInHand", this, player.UserIDString));
                                            return;
                                        }
                                        SendMSG(player, "", lang.GetMessage("addSynItem", this, player.UserIDString));
                                        return;
                                    case "command":
                                        
                                        if (args.Length == 5)
                                        {
                                            int i = -1;
                                            int.TryParse(args[4], out i);
                                            if (i <= 0) { SendMSG(player, lang.GetMessage("noCost", this, player.UserIDString)); return; }
                                            rewardData.RewardCommands.Add(args[2], new CommandInfo { Command = new List<string> { args[3] }, Cost = i, Description = "" });
                                            SendMSG(player, string.Format(lang.GetMessage("addSuccess", this, player.UserIDString), "command", args[2], i));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, "", lang.GetMessage("addSynCommand", this, player.UserIDString));
                                        return;
                                }
                            }
                        return;

                    case "remove":
                        if (isAuth(player))
                            if (args.Length == 3)
                            {
                                switch (args[1].ToLower())
                                {
                                    case "kit":
                                        if (rewardData.RewardKits.ContainsKey(args[2]))
                                        {
                                            rewardData.RewardKits.Remove(args[2]);
                                            SendMSG(player, "", string.Format(lang.GetMessage("remSuccess", this, player.UserIDString), args[2]));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, lang.GetMessage("noKitRem", this, player.UserIDString), "");
                                        return;
                                    case "item":
                                        int i;
                                        if (!int.TryParse(args[2], out i))
                                        {
                                            SendMSG(player, "", lang.GetMessage("itemIDHelp", this, player.UserIDString));
                                            return;
                                        }
                                        if (rewardData.RewardItems.ContainsKey(i))
                                        {
                                            SendMSG(player, "", string.Format(lang.GetMessage("remSuccess", this, player.UserIDString), rewardData.RewardItems[i].DisplayName));
                                            rewardData.RewardItems.Remove(i);
                                            Dictionary<int, ItemInfo> newList = new Dictionary<int, ItemInfo>();
                                            int n = 0;
                                            foreach(var entry in rewardData.RewardItems)
                                            {
                                                newList.Add(n, entry.Value);
                                                n++;
                                            }
                                            rewardData.RewardItems = newList;
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, lang.GetMessage("noItemRem", this, player.UserIDString), "");
                                        return;
                                    case "command":
                                        if (rewardData.RewardCommands.ContainsKey(args[2]))
                                        {
                                            rewardData.RewardKits.Remove(args[2]);
                                            SendMSG(player, "", string.Format(lang.GetMessage("remSuccess", this, player.UserIDString), args[2]));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, lang.GetMessage("noCommandRem", this, player.UserIDString), "");
                                        return;
                                }                               
                            }
                
                        return;
                    case "permission":
                        if (args.Length >= 2)
                            if (isAuth(player))
                            {
                                switch (args[1].ToLower())
                                {
                                    case "add":
                                        if (args.Length == 4)
                                        {
                                            int amount;
                                            if (int.TryParse(args[3], out amount))
                                            {
                                                var perm = args[2].ToLower();
                                                if (!perm.StartsWith("serverrewards."))
                                                    perm = "serverrewards." + perm;
                                                permData.Permissions.Add(perm, amount);
                                                permission.RegisterPermission(perm, this);
                                                SaveRewards();
                                                SendMSG(player, "", string.Format(lang.GetMessage("permCreated", this, player.UserIDString), perm, amount));
                                                return;
                                            }
                                        }
                                        SendMSG(player, lang.GetMessage("permAdd2", this, player.UserIDString), lang.GetMessage("permAdd1", this, player.UserIDString));
                                        return;
                                    case "remove":
                                        if (args.Length == 3)
                                            if (permData.Permissions.ContainsKey(args[2].ToLower()))
                                            {
                                                permData.Permissions.Remove(args[2].ToLower());
                                                SaveRewards();
                                                SendMSG(player, "", string.Format(lang.GetMessage("permRemoved", this, player.UserIDString), args[2].ToLower()));
                                                return;
                                            }
                                        SendMSG(player, lang.GetMessage("permRem2", this, player.UserIDString), lang.GetMessage("permRem1", this, player.UserIDString));
                                        return;
                                    case "list":
                                        SendMSG(player, "", "Permissions;");
                                        foreach (var entry in permData.Permissions)
                                            SendMSG(player, string.Format(lang.GetMessage("permListSyn", this, player.UserIDString), entry.Key, entry.Value));
                                        return;                                  
                                }

                            }
                        return;
                }                
            }
        }       
        [ChatCommand("refer")]
        private void cmdRefer(BasePlayer player, string command, string[] args)
        {
            if (configData.Use_Referrals)
            {
                if (args == null || args.Length == 0)
                {
                    SendMSG(player, "V " + Version, lang.GetMessage("title", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("chatRefer", this, player.UserIDString), lang.GetMessage("refSyn", this, player.UserIDString));
                    return;
                }
                if (referData.ReferredPlayers.Contains(player.userID))
                {
                    SendMSG(player, lang.GetMessage("alreadyRefer1", this, player.UserIDString));
                    return;
                }
                if (args.Length >= 1)
                {
                    BasePlayer referee = FindPlayer(player, args[0]);
                    if (referee != null)
                    {
                        if (referee.userID == player.userID)
                        {
                            SendMSG(player, lang.GetMessage("notSelf", this, player.UserIDString));
                            return;
                        }
                        if (!playerData.Players.ContainsKey(player.userID) || !playerData.Players.ContainsKey(referee.userID))
                        {
                            SendMSG(player, lang.GetMessage("errorProfile", this, player.UserIDString));
                            Puts(lang.GetMessage("errorPCon", this, player.UserIDString), player.displayName);
                            return;
                        }
                        referData.ReferredPlayers.Add(player.userID);
                        AddPoints(player.userID, configData.Points_Referrals);
                        AddPoints(referee.userID, configData.Points_Invites);
                        SendMSG(player, string.Format(lang.GetMessage("rInvitee", this, player.UserIDString), configData.Points_Referrals));
                        if (!referee.IsSleeping())
                            SendMSG(referee, string.Format(lang.GetMessage("rInviter", this, player.UserIDString), configData.Points_Invites, player.displayName));
                        return;
                    }
                    SendMSG(player, "", string.Format(lang.GetMessage("noFind", this, player.UserIDString), args[0]));
                }
            }
        }
        [ChatCommand("srnpc")]
        void cmdQuestNPC(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "/srnpc add");
                SendMSG(player, "/srnpc remove");
                return;
            }
            var NPC = FindEntity(player);
            if (NPC != null)
            {
                var isRegistered = isNPCRegistered(NPC.UserIDString);

                switch (args[0].ToLower())
                {
                    case "add":
                        {
                            if (!string.IsNullOrEmpty(isRegistered))
                            {
                                SendMSG(player, isRegistered);
                                return;
                            }
                            int key = npcDealers.NPCIDs.Count + 1;
                            npcDealers.NPCIDs.Add(NPC.UserIDString, new NPCInfos { ID = key, X = NPC.transform.position.x, Z = NPC.transform.position.z });
                            AddMapMarker(NPC.transform.position.x, NPC.transform.position.z, $"{lang.GetMessage("Reward Dealer", this)} {key}");
                            SendMSG(player, lang.GetMessage("npcNew", this));
                            SaveNPC();
                        }
                        return;
                    case "remove":
                        {
                            if (!string.IsNullOrEmpty(isRegistered))
                            {
                                npcDealers.NPCIDs.Remove(NPC.UserIDString);
                                int i = 1;
                                var data = new Dictionary<string, NPCInfos>();

                                foreach (var npc in npcDealers.NPCIDs)
                                {
                                    RemoveMapMarker($"{lang.GetMessage("Reward Dealer", this)} {npc.Value}");
                                    data.Add(npc.Key, new NPCInfos { ID = i, X = NPC.transform.position.x, Z = NPC.transform.position.z });
                                    i++;
                                }
                                foreach (var npc in data)
                                {
                                    AddMapMarker(npc.Value.X, npc.Value.Z, $"{lang.GetMessage("Reward Dealer", this)} {npc.Value.ID}");
                                }
                                npcDealers.NPCIDs = data;

                                SendMSG(player, lang.GetMessage("npcRem", this));
                                SaveNPC();
                            }
                            else SendMSG(player, lang.GetMessage("npcNotAdded", this));
                        }
                        return;
                    default:
                        break;
                }
            }
            else SendMSG(player, lang.GetMessage("noNPC", this, player.UserIDString));                
        }

        [ChatCommand("sr")]
        private void cmdSR(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, lang.GetMessage("srAdd2", this, player.UserIDString), lang.GetMessage("srAdd1", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("srTake2", this, player.UserIDString), lang.GetMessage("srTake1", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("srClear2", this, player.UserIDString), lang.GetMessage("srClear1", this, player.UserIDString));
                return;
            }
            if (args.Length >= 2)
            {
                BasePlayer target = FindPlayer(player, args[1]);
                if (target != null) 
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(args[2], out i);
                                if (i != 0)                                
                                    if (AddPoints(target.userID, i) != null) 
                                        SendMSG(player, string.Format(lang.GetMessage("addPoints", this, player.UserIDString), target.displayName, i));
                            }
                            return;
                        case "take":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null) 
                                        SendMSG(player, string.Format(lang.GetMessage("removePoints", this, player.UserIDString), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendMSG(player, string.Format(lang.GetMessage("clearPlayer", this, player.UserIDString), target.displayName));
                            return;
                        case "check":
                            if (args.Length == 2)
                            {
                                if (playerData.Players.ContainsKey(target.userID))
                                {
                                    var points = playerData.Players[target.userID].RewardPoints;
                                    SendMSG(player, string.Format("{0} - {2}: {1}", target.displayName, points, lang.GetMessage("storeRP", this)));
                                    return;
                                }
                                SendMSG(player, string.Format(lang.GetMessage("noProfile", this, player.UserIDString), target.displayName));
                            }
                            return;            
                    }
            }
        }

        [ConsoleCommand("sr")]
        private void ccmdSR(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "sr add <playername> <amount>" + lang.GetMessage("srAdd2", this));
                SendReply(arg, "sr take <playername> <amount>" + lang.GetMessage("srTake2", this));
                SendReply(arg, "sr clear <playername>" + lang.GetMessage("srClear2", this));
                return;
            }
            if (arg.Args.Length >= 2)
            {
                BasePlayer target = FindPlayer(null, arg.Args[1]);
                if (target != null)
                    switch (arg.Args[0].ToLower())
                    {
                        case "add":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(arg.Args[2], out i);
                                if (i != 0)
                                    if (AddPoints(target.userID, i) != null)
                                        SendReply(arg, string.Format(lang.GetMessage("addPoints", this), target.displayName, i));
                            }
                            return;
                        case "take":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(arg.Args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null)
                                        SendReply(arg, string.Format(lang.GetMessage("removePoints", this), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendReply(arg, string.Format(lang.GetMessage("clearPlayer", this), target.displayName));
                            return;
                        case "check":
                            if (arg.Args.Length == 2)
                            {
                                if (playerData.Players.ContainsKey(target.userID))
                                {
                                    var points = playerData.Players[target.userID].RewardPoints;
                                    SendReply(arg, string.Format("{0} - {2}: {1}", target.displayName, points, lang.GetMessage("storeRP", this)));
                                    return;
                                }
                                SendReply(arg, string.Format(lang.GetMessage("noProfile", this), target.displayName));
                            }
                            return;
                    }
            }
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }
        bool isAuthCon(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You dont not have permission to use this command.");
                    return false;
                }
            }
            return true;
        }        
        #endregion

        #region Data
        void SaveData(bool unload = false)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null) continue;
                if (timeData.Players.ContainsKey(player.userID))
                    SavePlayersData(player);
                if (!unload)
                    if (!RPTimers.ContainsKey(player.userID))
                        ResetTimer(player);
            }
            PlayerData.WriteObject(playerData);
            ReferralData.WriteObject(referData);
            Puts("Saved player data");
            
        }
        void SaveRewards()
        {
            PermissionData.WriteObject(permData);
            RewardData.WriteObject(rewardData);
            Puts("Saved reward/permission data");
        }
        void SaveNPC()
        {
            NPC_Dealers.WriteObject(npcDealers);
            Puts("Saved NPC data");
        }
        private void SaveLoop()
        {
            SaveData();            
            timer.Once(configData.Save_Interval * 60, () => SaveLoop());
        }       
        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<PlayerDataStorage>();
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                playerData = new PlayerDataStorage();
            }
            try
            {
                rewardData = RewardData.ReadObject<RewardDataStorage>();
            }
            catch
            {
                Puts("Couldn't load reward data, creating new datafile");
                rewardData = new RewardDataStorage();
            }
            try
            {
                referData = ReferralData.ReadObject<ReferData>();
            }
            catch
            {
                Puts("Couldn't load referral data, creating new datafile");
                referData = new ReferData();
            }
            try
            {
                permData = PermissionData.ReadObject<PermData>();
            }
            catch
            {
                Puts("Couldn't load permission data, creating new datafile");
                permData = new PermData();
            }
            try
            {
                npcDealers = NPC_Dealers.ReadObject<NPCDealers>();
            }
            catch
            {
                Puts("Couldn't load NPC data, creating new datafile");
                npcDealers = new NPCDealers();
            }
        }
        #endregion

        #region Config

        class ConfigData
        {
            public int Save_Interval { get; set; }
            public int PT_Point_Interval { get; set; }
            public int Points_Referrals { get; set; }
            public int Points_Invites { get; set; }
            public int Points_Time { get; set; }
            public int Econ_ExchangeRate { get; set; }
            public int RP_ExchangeRate { get; set; }
            public int MSG_DisplayEvery { get; set; }
            public bool Use_Playtime { get; set; }
            public bool Use_Referrals { get; set; }
            public bool Disable_Kits { get; set; }
            public bool Disable_Items { get; set; }
            public bool Disable_Commands { get; set; }
            public bool DisableUI_FadeIn { get; set; }
            public bool Disable_CurrencyExchange { get; set; }
            public bool Disable_CurrencyTransfer { get; set; }
            public bool MSG_OutstandingPoints { get; set; }
            public bool ShowKitContents { get; set; }
            public bool LogRPTransactions { get; set; }
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
            public bool NPCDealers_Only { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            ConfigData config = new ConfigData
            {
                Disable_Commands = false,
                Disable_Items = false,
                Disable_Kits = false,
                Disable_CurrencyExchange = false,
                Disable_CurrencyTransfer = false,
                DisableUI_FadeIn = false,
                LogRPTransactions = true,
                Econ_ExchangeRate = 250,
                RP_ExchangeRate = 1,
                Save_Interval = 10,
                PT_Point_Interval = 60,
                Points_Referrals = 2,
                Points_Invites = 3,
                Points_Time = 1,
                MSG_DisplayEvery = 1,
                Use_Playtime = true,
                MSG_OutstandingPoints = true,
                Use_Referrals = true,
                MSG_MainColor = "<color=orange>",
                MSG_Color = "<color=#939393>",
                NPCDealers_Only = false,
                ShowKitContents = true
            };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region Unity WWW
        class QueueItem
        {
            public string url;
            public string itemid;
            public int skinid;            
            public QueueItem(string ur, string na, int sk)
            {
                url = ur;
                itemid = na;
                skinid = sk;               
            }
        }
        class UnityWeb : MonoBehaviour
        {
            ServerRewards filehandler;
            const int MaxActiveLoads = 3;
            private Queue<QueueItem> QueueList = new Queue<QueueItem>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            private void Awake()
            {
                filehandler = (ServerRewards)Interface.Oxide.RootPluginManager.GetPlugin(nameof(ServerRewards));
            }
            private void OnDestroy()
            {
                QueueList.Clear();
                filehandler = null;
            }
            public void Add(string url, string itemid, int skinid)
            {
                QueueList.Enqueue(new QueueItem(url, itemid, skinid));
                if (activeLoads < MaxActiveLoads) Next();
            }

            void Next()
            {
                if (QueueList.Count <= 0) return;
                activeLoads++;
                StartCoroutine(WaitForRequest(QueueList.Dequeue()));
            }
            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(QueueItem info)
            {
                using (var www = new WWW(info.url))
                {
                    yield return www;
                    if (filehandler == null) yield break;
                    if (www.error != null)
                    {
                        print(string.Format("Image loading fail! Error: {0}", www.error));
                    }
                    else
                    {
                        if (!filehandler.rewardData.storedImages.ContainsKey(info.itemid.ToString()))
                            filehandler.rewardData.storedImages.Add(info.itemid.ToString(), new Dictionary<int, uint>());
                        if (!filehandler.rewardData.storedImages[info.itemid.ToString()].ContainsKey(info.skinid))
                        {
                            ClearStream();
                            stream.Write(www.bytes, 0, www.bytes.Length);
                            uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                            ClearStream();
                            filehandler.rewardData.storedImages[info.itemid.ToString()].Add(info.skinid, textureID);
                        }
                    }
                    activeLoads--;
                    if (QueueList.Count > 0) Next();
                    else if (QueueList.Count <= 0) filehandler.SaveRewards();
                }
            }
        }
        [ConsoleCommand("loadimages")]
        private void cmdLoadImages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                LoadImages(false);
            }
        }
        [ConsoleCommand("loadlocalimages")]
        private void cmdLoadLocalImages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                LoadImages(true);
            }
        }
        private void LoadImages(bool isLocal)
        {
            string dir = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "ServerRewards" + Path.DirectorySeparatorChar + "Icons" + Path.DirectorySeparatorChar;

            foreach(var entry in rewardData.storedImages)
            {                
                foreach(var image in entry.Value)
                {
                    FileStorage.server.Remove(image.Value, FileStorage.Type.png, uint.MaxValue);
                }
            }
            rewardData.storedImages.Clear();
            uWeb.Add("http://i.imgur.com/zq9zuKw.jpg", "999999999", 0);
            foreach (var entry in rewardData.RewardItems)
            {               
                if (!string.IsNullOrEmpty(entry.Value.URL))
                {
                    var url = entry.Value.URL;
                    if (isLocal)
                        url = dir + url;
                    uWeb.Add(url, entry.Value.ID.ToString(), entry.Value.Skin);
                }
            }

            foreach (var entry in rewardData.RewardKits)
            {               
                if (!string.IsNullOrEmpty(entry.Value.URL))
                {
                    var url = entry.Value.URL;
                    if (isLocal)
                        url = dir + url;
                    uWeb.Add(url, entry.Value.KitName, 0);
                }
            }
        }
        #endregion



        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "ServerRewards: " },
            { "msgOutRewards1", "You currently have {0} unspent reward tokens! Spend them in the reward store using /s" },
            {"msgNoPoints", "You dont have enough reward points" },
            {"errorProfile", "Error getting your profile from the database"},
            {"errorPCon", "There was a error pulling {0}'s profile from the database" },
            {"errorItemPlayer", "There was an error whilst retrieving your reward, please contact an administrator" },
            {"noFind", "Unable to find {0}" },
            {"rInviter", "You have recieved {0} reward points for inviting {1}" },
            {"rInvitee", "You have recieved {0} reward points" },
            {"refSyn", "/refer <playername>" },
            {"remSynKit", "/rewards remove kit <name>" },
            {"remSynItem", "/rewards remove item <number>" },
            {"remSynCommand", "/rewards remove command <name>" },
            {"noKit", "Kit's could not confirm that the kit exists. Check Kit's and your kit data" },
            {"noKitRem", "Unable to find a reward kit with that name" },
            {"noItemRem", "Unable to find a reward item with that number" },
            {"noCommandRem", "Unable to find a reward command with that name" },
            {"remSuccess", "You have successfully removed {0} from the rewards list" },
            {"addSynKit", "/rewards add kit <Name> <kitname> <cost>" },
            {"addSynItem", "/rewards add item <cost>" },
            {"addSynCommand", "/rewards add command <Name> <command> <cost>" },
            {"storeSyn21", "/s" },
            {"storeSyn2", " - Opens the reward store" },
            {"addSuccess", "You have added the {0} {1}, available for {2} tokens" },
            {"rewardExisting", "You already have a reward kit named {0}" },
            {"noCost", "You must enter a reward cost" },
            {"reward", "Reward: " },
            {"desc1", ", Description: " },
            {"cost", ", Cost: " },
            {"claimSyn", "/claim <rewardname>" },
            {"noReward", "This reward doesnt exist!" },
            {"claimSuccess", "You have claimed {0}" },
            {"multiPlayers", "Multiple players found with that name" },
            {"noPlayers", "No players found" },
            {"pointsAvail", "You have played for {0}, and have {1} point(s) to spend" },
            {"tpointsAvail", "You have {0}  reward point(s) to spend" },
            {"rewardAvail", "Available Rewards;" },
            {"chatClaim", " - Claim the reward"},
            {"chatCheck", "/rewards check" },
            {"chatCheck1", " - Displays you current time played and current reward points"},
            {"chatList", "/rewards list"},
            {"chatList1", " - Dumps item rewards and their ID numbers to console"},
            {"chatAddKit", " - Add a new reward kit"},
            {"chatAddItem", " - Add a new reward item"},
            {"chatAddCommand", " - Add a new reward command"},
            {"chatRemove", " - Removes a reward"},
            {"chatRefer", " - Acknowledge your referral from <playername>"},
            {"alreadyRefer1", "You have already been referred" },
            {"addPoints", "You have given {0} {1} points" },
            {"removePoints", "You have taken {0} points from {1}"},
            {"clearPlayer", "You have removed {0}'s reward profile" },
            {"srAdd1", "/sr add <playername> <amount>" },
            {"srAdd2", " - Adds <amount> of reward points to <playername>" },
            {"srTake1", "/sr take <playername> <amount>" },
            {"srTake2", " - Takes <amount> of reward points from <playername>" },
            {"srClear1", "/sr clear <playername>" },
            {"srClear2", " - Clears <playername>'s reward profile" },
            {"notSelf", "You cannot refer yourself. But nice try!" },
            {"noCommands", "There are currently no commands set up" },
            {"noItems", "There are currently no items set up" },
            {"noKits", "There are currently no kits set up" },
            {"exchange1", "Here you can exchange economics money (Coins) for reward points (RP) and vice-versa" },
            {"exchange2", "The current exchange rate is " },
            {"buyKit", "You have purchased a {0} kit" },
            {"notEnoughPoints", "You don't have enough points" },
            {"errorKit", "There was a error purchasing this kit. Contact a administrator" },
            {"buyCommand", "You have purchased the {0} command" },
            {"errorCommand", "There was a error purchasing this command. Contact a administrator" },
            {"buyItem", "You have purchased {0}x {1}" },
            {"errorItem", "There was a error purchasing this item. Contact a administrator" },
            {"notEnoughCoins", "You do not have enough coins to exchange" },            
            {"exchange", "You have exchanged " },
            {"itemInHand", "You must place the item you wish to add in your hands" },
            {"itemIDHelp", "You must enter the items number. Type /rewards list to see available entries" },
            {"noProfile", "{0} does not have any saved data" },
            {"permAdd1", "/rewards permission add <permname> <amount>" },
            {"permAdd2", " - Add a new permission to give a different amount of playtime points" },
            {"permRem1", "/rewards permission remove <permname>" },
            {"permRem2", " - Remove a custom permission" },
            {"permCreated", "You have created a new permission {0} with a point value of {1}" },
            {"permRemoved", "You have successfully removed the permission {0}" },
            {"permList1", "/rewards permission list" },
            {"permList2", " - Lists all custom permissions and their point value" },
            {"permListSyn", "Permission: {0}, Value: {1}" },
            {"storeTitle", "Reward Store" },
            {"storeKits", "Kits" },
            {"storeCommands", "Commands" },
            {"storeItems", "Items" },
            {"storeExchange", "Exchange" },
            {"storeTransfer", "Transfer" },
            {"storeClose", "Close" },
            {"storeNext", "Next" },
            {"storeBack", "Back" },
            {"storePlaytime", "Playtime" },
            {"storeCost", "Cost" },
            {"storeRP", "RP" },
            {"storeEcon", "Economics" },
            {"storeCoins", "Coins" },
            {"npcExist", "This NPC is already a Reward Dealer" },
            {"npcNew", "You have successfully added a new Reward Dealer" },
            {"npcRem", "You have successfully removed a Reward Dealer" },
            {"npcNotAdded", "This NPC is not a Reward Dealer" },
            {"noNPC", "Could not find a NPC to register" },
            {"Reward Dealer", "Reward Dealer" },
            {"fullInv", "Your inventory is full" },
            {"transfer1", "Select a user to transfer money to" },
            {"transfer2", "Select a amount to send" },
            {"transfer3", "You have transferred {0} {1} to {2}" }
        };
    }   
}
