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
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ServerRewards", "k1lly0u", "0.3.21", ResourceId = 1751)]
    public class ServerRewards : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Kits;
        [PluginReference] Plugin Economics;
        [PluginReference] Plugin HumanNPC;
        [PluginReference] Plugin LustyMap;
        [PluginReference] Plugin PlaytimeTracker;

        #region Datafiles
        PointData playerData;
        private DynamicConfigFile PlayerData;

        RewardDataStorage rewardData;
        private DynamicConfigFile RewardData;        
        
        NPCDealers npcDealers;
        private DynamicConfigFile NPC_Dealers;
        #endregion

        static GameObject webObject;
        static UnityWeb uWeb;
        static MethodInfo getFileData = typeof(FileStorage).GetMethod("StorageGet", (BindingFlags.Instance | BindingFlags.NonPublic));

        static ServerRewards instance;
        ConfigData configData;

        private Timer saveTimer;

        private Dictionary<ulong, OUIData> OpenUI;
        private Dictionary<string, string> ItemNames;
        private Dictionary<ulong, int> PointCache;
        private Dictionary<ulong, NPCInfos> NPCCreator;
        #endregion

        #region Classes
        #region Player data
        class PointData
        {
            public Dictionary<ulong, int> Players = new Dictionary<ulong, int>();
        }
        
        #endregion
        
        #region Reward data
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
            public int TargetID;
        }
        class CommandInfo
        {
            public List<string> Command;
            public string Description;
            public int Cost;
        }

        #endregion

        #region Other data
        class NPCDealers
        {
            public Dictionary<string, NPCInfos> NPCIDs = new Dictionary<string, NPCInfos>();
        }
        class NPCInfos
        {
            public float X;
            public float Z;
            public float ID;
            public string NPCID;
            public bool isCustom;
            public List<int> itemList = new List<int>();
            public List<string> kitList = new List<string>();
            public List<string> commandList = new List<string>();
        }
        class KitItemEntry
        {
            public int ItemAmount;
            public List<string> ItemMods;
        }
        public class OUIData
        {
            public ElementType type;
            public int page;
            public string npcid;
        }
        #endregion

        #region UI
        static string UIMain = "SR_Store";
        static string UISelect = "SR_Select";
        static string UIRP = "SR_RPPanel";
        static string UIPopup = "SR_Popup";

        class SR_UI
        {
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
                panel, CuiHelper.GetGuid());
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (instance.configData.UI_Options.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (instance.configData.UI_Options.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel, CuiHelper.GetGuid());
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {               
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
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
                if (instance.configData.UI_Options.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
        }
        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"light", "0.9 0.9 0.9 0.1" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttoncom", "0 0.5 0.1 0.9" },
            {"grey8", "0.8 0.8 0.8 1.0" }
        };
        #endregion
        #endregion

        #region Player UI
        private void DisplayPoints(BasePlayer player)
        {
            if (player == null) return;
            CuiHelper.DestroyUi(player, UIRP);
            if (!OpenUI.ContainsKey(player.userID)) return;
            int playerPoints = 0;
            if (PointCache.ContainsKey(player.userID))
                playerPoints = PointCache[player.userID];
            var element = SR_UI.CreateElementContainer(UIRP, "0 0 0 0", "0.3 0", "0.7 0.1");
            string message = $"{configData.Messaging.MSG_MainColor}{msg("storeRP", player.UserIDString)}: {playerPoints}</color>";
            if (Economics)
            {
                var amount = Economics?.Call("GetPlayerMoney", player.userID);
                message = message + $" || {configData.Messaging.MSG_MainColor}Economics: {amount}</color>";
            }
            if (configData.Options.Use_PTT)
            {
                var time = PlaytimeTracker?.Call("GetPlayTime", player.UserIDString);
                if (time is double)
                {
                    var playTime = GetPlaytimeClock((double)time);
                    if (!string.IsNullOrEmpty(playTime))
                        message = $"{configData.Messaging.MSG_MainColor}{msg("storePlaytime", player.UserIDString)}: {playTime}</color> || " + message;
                }
            }              

            SR_UI.CreateLabel(ref element, UIRP, "0 0 0 0", message, 20, "0 0", "1 1", TextAnchor.MiddleCenter, 0f);
            CuiHelper.AddUi(player, element);
            timer.Once(1, () => DisplayPoints(player));
        }
        private void PopupMessage(BasePlayer player, string msg)
        {
            CuiHelper.DestroyUi(player, UIPopup);
            var element = SR_UI.CreateElementContainer(UIPopup, UIColors["dark"], "0.33 0.45", "0.67 0.6");
            SR_UI.CreatePanel(ref element, UIPopup, UIColors["grey1"], "0.01 0.04", "0.99 0.96");
            SR_UI.CreateLabel(ref element, UIPopup, "", $"{configData.Messaging.MSG_MainColor}{msg}</color>", 22, "0 0", "1 1");
            CuiHelper.AddUi(player, element);
            timer.Once(3.5f, () => CuiHelper.DestroyUi(player, UIPopup));
        }

        private void OpenNavMenu(BasePlayer player, string npcid = null)
        {
            CuiElementContainer element = GetElement(ElementType.Navigation, 0, npcid);            
            CuiHelper.AddUi(player, element);
            DisplayPoints(player);
        }        
        private void SwitchElement(BasePlayer player, ElementType type, int page = 0, string npcid = null)
        {           
            if (type == ElementType.Transfer)
            {
                UIElements.DestroyUI(player, OpenUI[player.userID]);
                CreateTransferElement(player, page);
                OpenUI[player.userID] = new OUIData { type = ElementType.None };                
            }
            else
            {
                CuiElementContainer element = GetElement(type, page, npcid);

                UIElements.DestroyUI(player, OpenUI[player.userID]);
                CuiHelper.DestroyUi(player, UIMain);
                OpenUI[player.userID] = new OUIData { npcid = npcid, page = page, type = type };

                CuiHelper.AddUi(player, element);
            }            
        }        
        private CuiElementContainer GetElement(ElementType type, int page, string npcid = null)
        {            
            if (!string.IsNullOrEmpty(npcid))
            {
                if (UIElements.npcElements.ContainsKey(npcid))
                    return UIElements.npcElements[npcid][type][page];
            }
            return UIElements.standardElements[type][page];
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UISelect);
            CuiHelper.DestroyUi(player, UIRP);
            if (OpenUI.ContainsKey(player.userID))
            {
                UIElements.DestroyUI(player, OpenUI[player.userID]);
                UIElements.DestroyNav(player, OpenUI[player.userID]);
                OpenUI.Remove(player.userID);
            }            
            OpenMap(player);
        }

        public enum ElementType
        {
            None,
            Navigation,
            Kits,
            Items,
            Commands,
            Exchange,
            Transfer
        }
        #endregion

        #region UI Creation
        #region Static UI cache
        public static class UIElements
        {            
            public static Dictionary<ElementType, CuiElementContainer[]> standardElements;
            public static Dictionary<string, Dictionary<ElementType, CuiElementContainer[]>> npcElements;
            
            public static void RenameComponents()
            {
                foreach(var element in standardElements[ElementType.Navigation][0].ToArray())
                {
                    if (element.Name == "AddUI CreatedPanel")
                        element.Name = CuiHelper.GetGuid();
                }
                foreach(var npc in npcElements)
                {
                    foreach(var element in npc.Value[ElementType.Navigation][0])
                    {
                        if (element.Name == "AddUI CreatedPanel")
                            element.Name = CuiHelper.GetGuid();
                    }
                }
            }
            public static void DestroyUI(BasePlayer player, OUIData data)
            {
                if (data.type == ElementType.None) return;

                CuiElementContainer element = null;

                if (!string.IsNullOrEmpty(data.npcid) && npcElements.ContainsKey(data.npcid))                
                    element = npcElements[data.npcid][data.type][data.page];                
                else element = standardElements[data.type][data.page];

                for (int i = 0; i < element.ToArray().Length; i++)                
                    CuiHelper.DestroyUi(player, element.ToArray()[i].Name);                                
            } 
            public static void DestroyNav(BasePlayer player, OUIData data)
            {
                CuiElementContainer element = null;

                if (!string.IsNullOrEmpty(data.npcid))
                    element = npcElements[data.npcid][ElementType.Navigation][0];
                else element = standardElements[ElementType.Navigation][0];

                for (int i = 0; i < element.ToArray().Length; i++)
                    CuiHelper.DestroyUi(player, element.ToArray()[i].Name);
            }           
        }
        void InitializeAllElements()
        {
            PrintWarning("Creating and storing all UI elements to cache");
            CreateNavUI();
            CreateKitsUI();
            CreateItemsUI();
            CreateCommandsUI();
            CreateExchangeUI();
            CreateAllNPCs();
            UIElements.RenameComponents();
            PrintWarning("All UI elements created successfully!");
        }

        #region Standard Elements
        private void CreateNavUI(string npcid = "")
        {
            var Selector = SR_UI.CreateElementContainer(UISelect, UIColors["dark"], "0 0.92", "1 1");
            SR_UI.CreatePanel(ref Selector, UISelect, UIColors["light"], "0.01 0.05", "0.99 0.95", true);
            SR_UI.CreateLabel(ref Selector, UISelect, "", $"{configData.Messaging.MSG_MainColor}{msg("storeTitle")}</color>", 30, "0.05 0", "0.2 1");
            
            int number = 0;
            if (!configData.Categories.Disable_Kits) { CreateMenuButton(ref Selector, UISelect, msg("storeKits"), $"SRUI_ChangeElement Kits 0 {npcid}", number); number++; }
            if (!configData.Categories.Disable_Items) { CreateMenuButton(ref Selector, UISelect, msg("storeItems"), $"SRUI_ChangeElement Items 0 {npcid}", number); number++; }
            if (!configData.Categories.Disable_Commands) { CreateMenuButton(ref Selector, UISelect, msg("storeCommands"), $"SRUI_ChangeElement Commands 0 {npcid}", number); number++; }
            if (Economics) if (!configData.Categories.Disable_CurrencyExchange) { CreateMenuButton(ref Selector, UISelect, msg("storeExchange"), "SRUI_ChangeElement Exchange 0", number); number++; }
            if (!configData.Categories.Disable_CurrencyTransfer) { CreateMenuButton(ref Selector, UISelect, msg("storeTransfer"), "SRUI_ChangeElement Transfer 0", number); number++; }
            CreateMenuButton(ref Selector, UISelect, msg("storeClose"), "SRUI_DestroyAll", number);

            if (!string.IsNullOrEmpty(npcid))
                UIElements.npcElements.Add(npcid, new Dictionary<ElementType, CuiElementContainer[]> { { ElementType.Navigation, new CuiElementContainer[] { Selector } } });
            else UIElements.standardElements.Add(ElementType.Navigation, new CuiElementContainer[] { Selector });
        }
        private void CreateKitsUI()
        {
            int maxPages = 0;
            var count = rewardData.RewardKits;
            if (count.Count > 10)            
                maxPages = (count.Count - 1) / 10 + 1;
            List<CuiElementContainer> kitList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)            
                kitList.Add(CreateKitsElement(i));
            UIElements.standardElements.Add(ElementType.Kits, kitList.ToArray());            
        }
        private void CreateItemsUI()
        {
            int maxPages = 0;
            var count = rewardData.RewardItems;
            if (count.Count > 21)
                maxPages = (count.Count - 1) / 21 + 1;
            List<CuiElementContainer> itemList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)
                itemList.Add(CreateItemsElement(i));
            UIElements.standardElements.Add(ElementType.Items, itemList.ToArray());
        }
        private void CreateCommandsUI()
        {
            int maxPages = 0;
            var count = rewardData.RewardCommands;
            if (count.Count > 10)
                maxPages = (count.Count - 1) / 10 + 1;
            List<CuiElementContainer> commandList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)
                commandList.Add(CreateCommandsElement(i));
            UIElements.standardElements.Add(ElementType.Commands, commandList.ToArray());
        }
        private void CreateExchangeUI()
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("exchange1")}</color>", 24, "0 0.82", "1 0.9");
            SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_Color}{msg("exchange2")}</color>{configData.Messaging.MSG_MainColor}{configData.CurrencyExchange.RP_ExchangeRate} {msg("storeRP")}</color> -> {configData.Messaging.MSG_MainColor}{configData.CurrencyExchange.Econ_ExchangeRate} {msg("storeCoins")}</color>", 20, "0 0.6", "1 0.7");
            SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("storeRP")} => {msg("storeEcon")}</color>", 20, "0.25 0.4", "0.4 0.55");
            SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("storeEcon")} => {msg("storeRP")}</color>", 20, "0.6 0.4", "0.75 0.55");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeExchange"), 20, "0.25 0.3", "0.4 0.38", "SRUI_Exchange 1");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeExchange"), 20, "0.6 0.3", "0.75 0.38", "SRUI_Exchange 2");
            UIElements.standardElements.Add(ElementType.Exchange, new CuiElementContainer[] { Main });
        }
        private CuiElementContainer CreateKitsElement(int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardKits;
            if (rew.Count > 10)
            {
                var maxpages = (rew.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Kits {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Kits {page - 1}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int rewardcount = 10 * page;

            List<string> kitNames = rewardData.RewardKits.Keys.ToList();

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                var contents = GetKitContents(rew[kitNames[n]].KitName);
                if (string.IsNullOrEmpty(contents) || !configData.UI_Options.ShowKitContents)
                    contents = rew[kitNames[n]].Description;
                CreateKitCommandEntry(ref Main, UIMain, kitNames[n], contents, rew[kitNames[n]].Cost, i, true);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noKits")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        private CuiElementContainer CreateItemsElement(int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardItems;
            if (rew.Count > 21)
            {
                var maxpages = (rew.Count - 1) / 21 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Items {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Items {page - 1}");
            }
            int maxentries = (21 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int i = 0;
            int rewardcount = 21 * page;
            for (int n = rewardcount; n < maxentries; n++)
            {
                CreateItemEntry(ref Main, UIMain, n, i);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noItems")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        private CuiElementContainer CreateCommandsElement(int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            var rew = rewardData.RewardCommands;
            if (rew.Count > 10)
            {
                var maxpages = (rew.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Commands {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Commands {page - 1}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int rewardcount = 10 * page;

            List<string> commNames = rewardData.RewardCommands.Keys.ToList();            

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                CreateKitCommandEntry(ref Main, UIMain, commNames[n], rew[commNames[n]].Description, rew[commNames[n]].Cost, i, false);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noCommands")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        #endregion
                
        #region NPC Elements
        private void CreateAllNPCs()
        {
            foreach(var npc in npcDealers.NPCIDs)
            {
                if (npc.Value.isCustom)
                {
                    CreateNPCMenu(npc.Key);                    
                }
            }
        }
        private void CreateNPCMenu(string npcid)
        {
            if (UIElements.npcElements.ContainsKey(npcid))
                UIElements.npcElements.Remove(npcid);
            CreateNavUI(npcid);
            CreateNPCCommandsUI(npcid);
            CreateNPCItemsUI(npcid);
            CreateNPCKitsUI(npcid);
            UIElements.RenameComponents();
        }
        private void CreateNPCKitsUI(string npcid)
        {
            int maxPages = 0;
            var count = npcDealers.NPCIDs[npcid].kitList;
            if (count.Count > 10)
                maxPages = (count.Count - 1) / 10 + 1;
            List<CuiElementContainer> kitList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)
                kitList.Add(CreateNPCKitsElement(npcid, i));
            UIElements.npcElements[npcid].Add(ElementType.Kits, kitList.ToArray());
        }
        private void CreateNPCItemsUI(string npcid)
        {
            int maxPages = 0;
            var count = npcDealers.NPCIDs[npcid].itemList;
            if (count.Count > 21)
                maxPages = (count.Count - 1) / 21 + 1;
            List<CuiElementContainer> itemList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)
                itemList.Add(CreateNPCItemsElement(npcid, i));
            UIElements.npcElements[npcid].Add(ElementType.Items, itemList.ToArray());
        }
        private void CreateNPCCommandsUI(string npcid)
        {
            int maxPages = 0;
            var count = npcDealers.NPCIDs[npcid].commandList;
            if (count.Count > 10)
                maxPages = (count.Count - 1) / 10 + 1;
            List<CuiElementContainer> commandList = new List<CuiElementContainer>();
            for (int i = 0; i <= maxPages; i++)
                commandList.Add(CreateNPCCommandsElement(npcid, i));
            UIElements.npcElements[npcid].Add(ElementType.Commands, commandList.ToArray());
        }
        private CuiElementContainer CreateNPCKitsElement(string npcid, int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);

            List<string> kitNames = npcDealers.NPCIDs[npcid].kitList;
            if (kitNames.Count > 10)
            {
                var maxpages = (kitNames.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Kits {page + 1} {npcid}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Kits {page - 1} {npcid}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > kitNames.Count)
                maxentries = kitNames.Count;
            int rewardcount = 10 * page;
                        
            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                var contents = GetKitContents(rewardData.RewardKits[kitNames[n]].KitName);
                if (string.IsNullOrEmpty(contents) || !configData.UI_Options.ShowKitContents)
                    contents = rewardData.RewardKits[kitNames[n]].Description;
                CreateKitCommandEntry(ref Main, UIMain, kitNames[n], contents, rewardData.RewardKits[kitNames[n]].Cost, i, true);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noKits")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        private CuiElementContainer CreateNPCItemsElement(string npcid, int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);

            var rew = npcDealers.NPCIDs[npcid].itemList;
            if (rew.Count > 21)
            {
                var maxpages = (rew.Count - 1) / 21 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Items {page + 1} {npcid}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Items {page - 1} {npcid}");
            }
            int maxentries = (21 * (page + 1));
            if (maxentries > rew.Count)
                maxentries = rew.Count;
            int i = 0;
            int rewardcount = 21 * page;

            for (int n = rewardcount; n < maxentries; n++)
            {                
                CreateItemEntry(ref Main, UIMain, rew[n], i);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noItems")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        private CuiElementContainer CreateNPCCommandsElement(string npcid, int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);

            List<string> commNames = npcDealers.NPCIDs[npcid].commandList;
            if (commNames.Count > 10)
            {
                var maxpages = (commNames.Count - 1) / 10 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_ChangeElement Commands {page + 1} {npcid}");
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_ChangeElement Commands {page - 1} {npcid}");
            }
            int maxentries = (10 * (page + 1));
            if (maxentries > commNames.Count)
                maxentries = commNames.Count;
            int rewardcount = 10 * page;
            
            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                CreateKitCommandEntry(ref Main, UIMain, commNames[n], rewardData.RewardCommands[commNames[n]].Description, rewardData.RewardCommands[commNames[n]].Cost, i, false);
                i++;
            }
            if (i == 0)
                SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("noCommands")}</color>", 24, "0 0.82", "1 0.9");
            return Main;
        }
        #endregion
        #endregion

        #region Transfer System
        private void CreateTransferElement(BasePlayer player, int page = 0)
        {            
            var HelpMain = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref HelpMain, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            SR_UI.CreateLabel(ref HelpMain, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("transfer1", player.UserIDString)}</color>", 22, "0 0.9", "1 1");

            var playerCount = BasePlayer.activePlayerList.Count;
            if (playerCount > 96)
            {
                var maxpages = (playerCount - 1) / 96 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref HelpMain, UIMain, UIColors["buttonbg"], msg("storeNext", player.UserIDString), 18, "0.87 0.92", "0.97 0.97", $"SRUI_Transfer {page + 1}");
                if (page > 0)
                    SR_UI.CreateButton(ref HelpMain, UIMain, UIColors["buttonbg"], msg("storeBack",  player.UserIDString), 18, "0.03 0.92", "0.13 0.97", $"SRUI_Transfer {page - 1}");
            }
            int maxentries = (96 * (page + 1));
            if (maxentries > playerCount)
                maxentries = playerCount;
            int rewardcount = 96 * page;

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                if (BasePlayer.activePlayerList[n] == null) continue;
                CreatePlayerNameEntry(ref HelpMain, UIMain, BasePlayer.activePlayerList[n].displayName, BasePlayer.activePlayerList[n].UserIDString, i);
                i++;
            }
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, HelpMain);
        }
        private void TransferElement(BasePlayer player, string name, string id)
        {
            CuiHelper.DestroyUi(player, UIMain);
            var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 0.92");
            SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);

            SR_UI.CreateLabel(ref Main, UIMain, "", $"{configData.Messaging.MSG_MainColor}{msg("transfer2", player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], "1", 20, "0.27 0.3", "0.37 0.38", $"SRUI_TransferID {id} {name} 1");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], "10", 20, "0.39 0.3", "0.49 0.38", $"SRUI_TransferID {id} {name} 10");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], "100", 20, "0.51 0.3", "0.61 0.38", $"SRUI_TransferID {id} {name} 100");
            SR_UI.CreateButton(ref Main, UIMain, UIColors["buttonbg"], "1000", 20, "0.63 0.3", "0.73 0.38", $"SRUI_TransferID {id} {name} 1000");

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
                command = $"SRUI_BuyKit {name}";
                if (!string.IsNullOrEmpty(rewardData.RewardKits[name].URL))
                {                    
                    string fileLocation = rewardData.storedImages[999999999.ToString()][0].ToString();
                    if (rewardData.storedImages.ContainsKey(rewardData.RewardKits[name].KitName))
                        fileLocation = rewardData.storedImages[rewardData.RewardKits[name].KitName][0].ToString();

                    SR_UI.LoadImage(ref container, panelName, fileLocation, $"{posMin.x} {posMin.y}", $"{posMin.x + 0.05} {posMax.y}");                    
                }
                posMin.x = 0.09f;
            }
            else command = $"SRUI_BuyCommand {name}";
            SR_UI.CreateLabel(ref container, panelName, "", $"{configData.Messaging.MSG_MainColor}{name}</color> -- {configData.Messaging.MSG_Color}{description}</color>", 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", TextAnchor.UpperLeft);
            SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], $"{msg("storeCost")}: {cost}", 18, $"0.84 {posMin.y + 0.015}", $"0.97 {posMax.y - 0.015f}", command);
        }       
        private void CreateItemEntry(ref CuiElementContainer container, string panelName, int itemnumber, int number)
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
                if (item.Amount > 1)
                    SR_UI.CreateTextOverlay(ref container, panelName, $"{configData.Messaging.MSG_MainColor}<size=18>X</size><size=20>{item.Amount}</size></color>", "", 20, $"{posMin.x + 0.02} {posMin.y + 0.1}", $"{posMax.x - 0.02} {posMax.y - 0.1}", TextAnchor.MiddleCenter);
                SR_UI.CreateLabel(ref container, panelName, "", item.DisplayName, 16, $"{posMin.x} {posMin.y + 0.05}", $"{posMax.x} {posMin.y + 0.08}");
                SR_UI.CreateButton(ref container, panelName, UIColors["buttonbg"], $"{msg("storeCost")}: {item.Cost}", 16, $"{posMin.x + 0.015} {posMin.y}", $"{posMax.x - 0.015} {posMin.y + 0.05}", $"SRUI_BuyItem {itemnumber}");
            }
        }
        #endregion

        #region Kit Contents
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

        #region UI Commands
        [ConsoleCommand("SRUI_BuyKit")]
        private void cmdBuyKit(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var kitName = arg.ArgsStr;
            if (rewardData.RewardKits.ContainsKey(kitName))
            {
                var kit = rewardData.RewardKits[kitName];
                if (PointCache.ContainsKey(player.userID))
                {
                    var pd = PointCache[player.userID];
                    if (pd >= kit.Cost)
                    {
                        if (TakePoints(player.userID, kit.Cost, "Kit " + kit.KitName) != null)
                        {
                            Kits?.Call("GiveKit", new object[] { player, kit.KitName });
                            PopupMessage(player, string.Format(msg("buyKit", player.UserIDString), kitName));
                            return;
                        }
                    }                    
                }
                PopupMessage(player, msg("notEnoughPoints", player.UserIDString));
                return;
            }
            PopupMessage(player, msg("errorKit", player.UserIDString));
            return;
        }

        [ConsoleCommand("SRUI_BuyCommand")]
        private void cmdBuyCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var commandname = arg.ArgsStr;
            if (rewardData.RewardCommands.ContainsKey(commandname))
            {
                var command = rewardData.RewardCommands[commandname];
                if (PointCache.ContainsKey(player.userID))
                {
                    var pd = PointCache[player.userID];
                    if (pd >= command.Cost)
                    {
                        if (TakePoints(player.userID, command.Cost, "Command") != null)
                        {
                            foreach (var cmd in command.Command)
                                ConsoleSystem.Run.Server.Normal(cmd.Replace("$player.id", player.UserIDString).Replace("$player.name", player.displayName).Replace("$player.x", player.transform.position.x.ToString()).Replace("$player.y", player.transform.position.y.ToString()).Replace("$player.z", player.transform.position.z.ToString()));

                            PopupMessage(player, string.Format(msg("buyCommand", player.UserIDString), commandname));
                            return;
                        }
                    }
                }
                PopupMessage(player, msg("notEnoughPoints", player.UserIDString));
                return;
            }
            PopupMessage(player, msg("errorCommand", player.UserIDString));
            return;
        }

        [ConsoleCommand("SRUI_BuyItem")]
        private void cmdBuyItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var itemname = int.Parse(arg.GetString(0).Replace("'", ""));
            if (rewardData.RewardItems.ContainsKey(itemname))
            {
                var item = rewardData.RewardItems[itemname];
                if (PointCache.ContainsKey(player.userID))
                {
                    var pd = PointCache[player.userID];
                    if (player.inventory.containerMain.itemList.Count == 24)
                    {
                        PopupMessage(player, msg("fullInv", player.UserIDString));
                        return;
                    }
                    if (pd >= item.Cost)
                    {
                        if (TakePoints(player.userID, item.Cost, item.DisplayName) != null)
                        {
                            GiveItem(player, itemname);
                            PopupMessage(player, string.Format(msg("buyItem", player.UserIDString), item.Amount, item.DisplayName));
                            return;
                        }
                    }
                }
                PopupMessage(player, msg("notEnoughPoints", player.UserIDString));
                return;
            }
            PopupMessage(player, msg("errorItem", player.UserIDString));
            return;
        }        

        [ConsoleCommand("SRUI_ChangeElement")]
        private void cmdChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;

            string type = arg.GetString(0);
            int page = 0;
            string npcid = null;

            if (arg.Args.Length >= 2)            
                page = arg.GetInt(1);

            if (arg.Args.Length >= 3)
                npcid = arg.GetString(2);
            
            switch (type)
            {
                case "Kits":
                    SwitchElement(player, ElementType.Kits, page, npcid);
                    return;
                case "Commands":
                    SwitchElement(player, ElementType.Commands, page, npcid);
                    return;
                case "Items":
                    SwitchElement(player, ElementType.Items, page, npcid);
                    return;
                case "Exchange":
                    SwitchElement(player, ElementType.Exchange);
                    return;
                case "Transfer":
                    SwitchElement(player, ElementType.Transfer, page);
                    return;              
            }
        }
        
        [ConsoleCommand("SRUI_Exchange")]
        private void cmdExchange(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;            
            var type = int.Parse(arg.GetString(0).Replace("'", ""));           
            if (type == 1)
            {
                if (!PointCache.ContainsKey(player.userID) || PointCache[player.userID] < configData.CurrencyExchange.RP_ExchangeRate)
                {
                    PopupMessage(player, msg("notEnoughPoints", player.UserIDString));
                    return;
                }
                if (TakePoints(player.userID, configData.CurrencyExchange.RP_ExchangeRate, "RP Exchange") != null)
                {
                    Economics.Call("Deposit", player.userID, (double)configData.CurrencyExchange.Econ_ExchangeRate);
                    PopupMessage(player, $"{msg("exchange", player.UserIDString)}{configData.CurrencyExchange.RP_ExchangeRate} {msg("storeRP", player.UserIDString)} for {configData.CurrencyExchange.Econ_ExchangeRate} {msg("storeCoins", player.UserIDString)}");
                }
            }
            else
            {
                var amount = Convert.ToSingle(Economics?.Call("GetPlayerMoney", player.userID));
                if (amount < configData.CurrencyExchange.Econ_ExchangeRate)
                {
                    PopupMessage(player, msg("notEnoughCoins", player.UserIDString));
                    return;
                }
                Economics?.Call("Withdraw", player.userID, (double)configData.CurrencyExchange.Econ_ExchangeRate);
                AddPoints(player.userID, configData.CurrencyExchange.RP_ExchangeRate);
                PopupMessage(player, $"{msg("exchange", player.UserIDString)}{configData.CurrencyExchange.Econ_ExchangeRate} {msg("storeCoins", player.UserIDString)} for {configData.CurrencyExchange.RP_ExchangeRate} {msg("storeRP", player.UserIDString)}");
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
                    PopupMessage(player, string.Format(msg("transfer3"), amount, msg("storeRP"), name));
                }
            }
            else
            {
                PopupMessage(player, msg("notEnoughPoints"));
            }
        }
       
        [ConsoleCommand("SRUI_DestroyAll")]
        private void cmdDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;            
            DestroyUI(player);
        }
        #endregion
        
        #endregion

        #region Oxide Hooks  
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_players");
            RewardData = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_rewards");
            NPC_Dealers = Interface.Oxide.DataFileSystem.GetFile("ServerRewards/data/serverrewards_npcids");
            ItemNames = new Dictionary<string, string>();
            OpenUI = new Dictionary<ulong, OUIData>();
            NPCCreator = new Dictionary<ulong, NPCInfos>();
            UIElements.npcElements = new Dictionary<string, Dictionary<ElementType, CuiElementContainer[]>>();
            UIElements.standardElements = new Dictionary<ElementType, CuiElementContainer[]>();
        }
        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            uWeb = webObject.AddComponent<UnityWeb>();
            uWeb.Add("http://i.imgur.com/zq9zuKw.jpg", "999999999", 0);
                                              
            LoadData();
            LoadVariables();

            instance = this;

            if (!Kits) PrintWarning($"Kits could not be found! Unable to issue kit rewards");
            if (configData.Options.Use_PTT && !PlaytimeTracker) PrintWarning("Playtime Tracker could not be found! Unable to monitor user playtime");
               
            foreach (var item in ItemManager.itemList)
            {
                if (!ItemNames.ContainsKey(item.itemid.ToString()))
                    ItemNames.Add(item.itemid.ToString(), item.displayName.translated);
            }            

            InitializeAllElements();

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);

            SaveLoop();            
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                DestroyUI(player);
                var ID = player.userID; 
                if (PointCache.ContainsKey(ID))
                    if (PointCache[ID] > 0)
                        InformPoints(player);
            }           
        }
        void OnPlayerDisconnected(BasePlayer player) => DestroyUI(player);
        void Unload()
        {
            if (saveTimer != null)
                saveTimer.Destroy();    
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
            SaveData();
        }
        #endregion

        #region Functions        
        private void SendMSG(BasePlayer player, string msg, string keyword = "title")
        {
            if (keyword == "title") keyword = lang.GetMessage("title", this, player.UserIDString);
            SendReply(player, configData.Messaging.MSG_MainColor + keyword + "</color>" + configData.Messaging.MSG_Color + msg + "</color>");
        }
        private void InformPoints(BasePlayer player)
        {
            var outstanding = PointCache[player.userID];
            SendMSG(player, string.Format(msg("msgOutRewards1", player.UserIDString), outstanding));
        }
        private void OpenStore(BasePlayer player, string npcid = null)
        {
            OpenUI.Add(player.userID, new OUIData { npcid = npcid });
            CloseMap(player);
            OpenNavMenu(player, npcid);
            if (!configData.Categories.Disable_Kits)            
                SwitchElement(player, ElementType.Kits, 0, npcid);
            else if (!configData.Categories.Disable_Items)
                SwitchElement(player, ElementType.Items, 0, npcid);
            else if (!configData.Categories.Disable_Commands)
                SwitchElement(player, ElementType.Commands, 0, npcid);
            else
            {
                OpenUI.Remove(player.userID);
                timer.Once(3.5f, () => { DestroyUI(player); OpenMap(player); });
                PopupMessage(player, "All reward options are currently disabled. Closing the store.");
            }
        }
        private string GetPlaytimeClock(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }
        private void GiveItem(BasePlayer player, int itemkey)
        {
            if (rewardData.RewardItems.ContainsKey(itemkey))
            {
                var entry = rewardData.RewardItems[itemkey];
                Item item = ItemManager.CreateByItemID(entry.ID, entry.Amount, entry.Skin);
                if (entry.TargetID != 0) item.blueprintTarget = entry.TargetID;
                item.MoveToContainer(player.inventory.containerMain);
            }
        }
        private BasePlayer FindPlayer(BasePlayer player, string arg)
        {
            ulong targetID;
            if (ulong.TryParse(arg, out targetID))
            {
                var target = covalence.Players.GetPlayer(arg);
                if (target != null && target.Object is BasePlayer)
                    return target as BasePlayer;
            }

            var targets = covalence.Players.FindPlayers(arg);            

            if (targets.ToArray().Length == 0)
            {
                if (player != null)
                    SendMSG(player, msg("noPlayers", player.UserIDString));
                return null;
            }
            if (targets.ToArray().Length > 1)
            {
                if (player != null)
                    SendMSG(player, msg("multiPlayers", player.UserIDString));
                return null;
            }
            if ((targets.ToArray()[0].Object as BasePlayer) != null)
                return targets.ToArray()[0].Object as BasePlayer;
            else SendMSG(player, msg("noPlayers", player.UserIDString));
            return null;
        }        
        private bool RemovePlayer(ulong ID)
        {
            if (PointCache.ContainsKey(ID))
            {
                PointCache.Remove(ID);
                return true;
            }
            return false;           
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
        #endregion

        #region API
        [HookMethod("AddPoints")]
        public object AddPoints(ulong ID, int amount)
        {
            if (!PointCache.ContainsKey(ID))
                PointCache.Add(ID, amount);
            else PointCache[ID] += amount;
            
            if (configData.Options.LogRPTransactions)
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

            if (!PointCache.ContainsKey(ID)) return null;
            PointCache[ID] -= amount;
            
            if (configData.Options.LogRPTransactions)
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
            if (!PointCache.ContainsKey(ID)) return null;
            return PointCache[ID];
        }
        #endregion

        #region External API Calls        
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
                OpenStore(player, npcID);
            }
        }
        #endregion

        #region NPC Registration
        private static FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

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
            if (npcDealers.NPCIDs.ContainsKey(ID)) return msg("npcExist");
            return null;
        }
        [ChatCommand("srnpc")]
        void cmdSRNPC(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "/srnpc add - Add a new NPC vendor");
                SendMSG(player, "/srnpc remove - Remove a NPC vendor");
                SendMSG(player, "/srnpc loot - Create a custom loot table for the specified NPC vendor");
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
                            AddMapMarker(NPC.transform.position.x, NPC.transform.position.z, $"{msg("Reward Dealer")} {key}");
                            SendMSG(player, msg("npcNew"));
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
                                    RemoveMapMarker($"{msg("Reward Dealer")} {npc.Value}");
                                    data.Add(npc.Key, new NPCInfos { ID = i, X = NPC.transform.position.x, Z = NPC.transform.position.z });
                                    i++;
                                }
                                foreach (var npc in data)
                                {
                                    AddMapMarker(npc.Value.X, npc.Value.Z, $"{msg("Reward Dealer")} {npc.Value.ID}");
                                }
                                npcDealers.NPCIDs = data;

                                SendMSG(player, msg("npcRem"));
                                SaveNPC();
                            }
                            else SendMSG(player, msg("npcNotAdded"));
                        }
                        return;
                    case "loot":
                        {
                            if (!string.IsNullOrEmpty(isRegistered))
                            {
                                if (!NPCCreator.ContainsKey(player.userID))
                                    NPCCreator.Add(player.userID, new NPCInfos { NPCID = NPC.UserIDString });
                                else NPCCreator[player.userID] = new NPCInfos { NPCID = NPC.UserIDString };
                                var Main = SR_UI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 1");
                                SR_UI.CreatePanel(ref Main, UIMain, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                                SR_UI.CreateLabel(ref Main, UIMain, "", msg("cldesc", player.UserIDString), 18, "0.25 0.88", "0.75 0.98");
                                SR_UI.CreateLabel(ref Main, UIMain, "", msg("storeKits", player.UserIDString), 18, "0 0.8", "0.33 0.88");
                                SR_UI.CreateLabel(ref Main, UIMain, "", msg("storeItems", player.UserIDString), 18, "0.33 0.8", "0.66 0.88");
                                SR_UI.CreateLabel(ref Main, UIMain, "", msg("storeCommands", player.UserIDString), 18, "0.66 0.8", "1 0.88");
                                CuiHelper.AddUi(player, Main);
                                NPCLootMenu(player);
                            }
                            else SendMSG(player, msg("npcNotAdded"));
                        }
                        return;
                    default:
                        break;
                }
            }
            else SendMSG(player, msg("noNPC", player.UserIDString));
        }
        private void NPCLootMenu(BasePlayer player, int page = 0)
        {
            var Main = SR_UI.CreateElementContainer(UISelect, "0 0 0 0", "0 0", "1 1");
            SR_UI.CreateButton(ref Main, UISelect, UIColors["buttonbg"], msg("save", player.UserIDString), 16, "0.85 0.91", "0.95 0.96", "SRUI_NPCSave", TextAnchor.MiddleCenter, 0f);
            SR_UI.CreateButton(ref Main, UISelect, UIColors["buttonbg"], msg("storeClose",player.UserIDString), 16, "0.05 0.91", "0.15 0.96", "SRUI_NPCCancel", TextAnchor.MiddleCenter, 0f);
            
            int[] itemNames = rewardData.RewardItems.Keys.ToArray();
            string[] kitNames = rewardData.RewardKits.Keys.ToArray();
            string[] commNames = rewardData.RewardCommands.Keys.ToArray();

            int maxCount = itemNames.Length;
            if (kitNames.Length > maxCount) maxCount = kitNames.Length;
            if (commNames.Length > maxCount) maxCount = commNames.Length;

            if (maxCount > 34)
            {
                var maxpages = (maxCount - 1) / 34 + 1;
                if (page < maxpages - 1)
                    SR_UI.CreateButton(ref Main, UISelect, UIColors["buttonbg"], msg("storeNext"), 18, "0.84 0.05", "0.97 0.1", $"SRUI_NPCPage {page + 1}", TextAnchor.MiddleCenter, 0f);
                if (page > 0)
                    SR_UI.CreateButton(ref Main, UISelect, UIColors["buttonbg"], msg("storeBack"), 18, "0.03 0.05", "0.16 0.1", $"SRUI_NPCPage {page - 1}", TextAnchor.MiddleCenter, 0f);
            }
            
            int maxComm = (34 * (page + 1));
            if (maxComm > commNames.Length)
                maxComm = commNames.Length;
            int commcount = 34 * page;

            int comm = 0;
            for (int n = commcount; n < maxComm; n++)
            {
                string color1 = UIColors["buttonbg"];
                string text1 = commNames[n];
                string command1 = $"SRUI_CustomList Commands {text1} true {page}";
                string color2 = "0 0 0 0";
                string text2 = "";
                string command2 = "";

                if (NPCCreator[player.userID].commandList.Contains(commNames[n]))
                {
                    color1 = UIColors["buttoncom"];
                    command1 = $"SRUI_CustomList Commands {text1} false {page}";
                }
                if (n + 1 < commNames.Length)
                {                    
                    color2 = UIColors["buttonbg"];
                    text2 = commNames[n + 1];
                    command2 = $"SRUI_CustomList Commands {text2} true {page}";
                    if (NPCCreator[player.userID].commandList.Contains(commNames[n + 1]))
                    {
                        color2 = UIColors["buttoncom"];
                        command2 = $"SRUI_CustomList Commands {text2} false {page}";
                    }
                    ++n;
                }

                CreateItemButton(ref Main, UISelect, color1, text1, command1, color2, text2, command2, comm, 0.66f);
                comm++;
            }

            int maxKit = (34 * (page + 1));
            if (maxKit > kitNames.Length)
                maxKit = kitNames.Length;
            int kitcount = 34 * page;

            int kits = 0;
            for (int n = kitcount; n < maxKit; n++)
            {
                string color1 = UIColors["buttonbg"];
                string text1 = kitNames[n];
                string command1 = $"SRUI_CustomList Kits {text1} true {page}";
                string color2 = "0 0 0 0";
                string text2 = "";
                string command2 = "";

                if (NPCCreator[player.userID].kitList.Contains(kitNames[n]))
                {
                    color1 = UIColors["buttoncom"];
                    command1 = $"SRUI_CustomList Kits {text1} false {page}";
                }
                if (n + 1 < kitNames.Length)
                {                    
                    color2 = UIColors["buttonbg"];
                    text2 = kitNames[n+1];
                    command2 = $"SRUI_CustomList Kits {text2} true {page}";
                    if (NPCCreator[player.userID].kitList.Contains(kitNames[n+1]))
                    {
                        color2 = UIColors["buttoncom"];
                        command2 = $"SRUI_CustomList Kits {text2} false {page}";
                    }
                    ++n;
                }

                CreateItemButton(ref Main, UISelect, color1, text1, command1, color2, text2, command2, kits, 0f);
                kits++;
            }

            int maxItem = (34 * (page + 1));
            if (maxItem > itemNames.Length)
                maxItem = itemNames.Length;
            int itemcount = 34 * page;

            int items = 0;
            for (int n = itemcount; n < maxItem; n++)
            {
                string color1 = UIColors["buttonbg"];
                string text1 = rewardData.RewardItems[n].DisplayName;
                string command1 = $"SRUI_CustomList Items {n} true {page}";
                string color2 = "0 0 0 0";
                string text2 = "";
                string command2 = "";

                if (NPCCreator[player.userID].itemList.Contains(n))
                {
                    color1 = UIColors["buttoncom"];
                    command1 = $"SRUI_CustomList Items {n} false {page}";
                }
                if (n + 1 < rewardData.RewardItems.Count)
                {                   
                    color2 = UIColors["buttonbg"];
                    text2 = rewardData.RewardItems[n + 1].DisplayName;
                    command2 = $"SRUI_CustomList Items {n + 1} true {page}";
                    if (NPCCreator[player.userID].itemList.Contains(n + 1))
                    {
                        color2 = UIColors["buttoncom"];
                        command2 = $"SRUI_CustomList Items {n + 1} false {page}";
                    }
                    ++n;
                }

                CreateItemButton(ref Main, UISelect, color1, text1, command1, color2, text2, command2, items, 0.33f);
                items++;
            }
            CuiHelper.DestroyUi(player, UISelect);
            CuiHelper.AddUi(player, Main);
        }
        void CreateItemButton(ref CuiElementContainer Main, string panel, string b1color, string b1text, string b1command, string b2color, string b2text, string b2command, int number, float xPos)
        {
            float offsetX = 0.01f;
            float offsetY = 0.0047f;
            Vector2 dimensions = new Vector2(0.15f, 0.04f);
            Vector2 origin = new Vector2(xPos + offsetX, 0.76f);

            Vector2 offset = new Vector2(0, (offsetY + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;

            SR_UI.CreateButton(ref Main, panel, b1color, b1text, 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", b1command, TextAnchor.MiddleCenter, 0f);
            SR_UI.CreateButton(ref Main, panel, b2color, b2text, 14, $"{posMin.x + offsetX + dimensions.x} {posMin.y}", $"{posMax.x + offsetX + dimensions.x} {posMax.y}", b2command, TextAnchor.MiddleCenter, 0f);
        }  
              
        [ConsoleCommand("SRUI_CustomList")]
        private void cmdCustomList(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;

            string type = arg.GetString(0);
            string key = arg.GetString(1);
            bool isAdding = arg.GetBool(2);
            int page = arg.GetInt(3);
            
            switch (type)
            {
                case "Kits":
                    if (isAdding)
                        NPCCreator[player.userID].kitList.Add(key);
                    else NPCCreator[player.userID].kitList.Remove(key);
                    break;
                case "Commands":
                    if (isAdding)
                        NPCCreator[player.userID].commandList.Add(key);
                    else NPCCreator[player.userID].commandList.Remove(key);
                    break;
                case "Items":
                    var id = int.Parse(key);
                    if (isAdding)
                        NPCCreator[player.userID].itemList.Add(id);
                    else NPCCreator[player.userID].itemList.Remove(id);
                    break;                                 
            }
            NPCLootMenu(player, page);
        }
        [ConsoleCommand("SRUI_NPCPage")]
        private void cmdNPCPage(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;

            int page = arg.GetInt(0);           
            NPCLootMenu(player, page);
        }
        [ConsoleCommand("SRUI_NPCCancel")]
        private void cmdNPCCancel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            NPCCreator.Remove(player.userID);
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UISelect);
            SendReply(player, msg("clcanc", player.UserIDString));
        }
        [ConsoleCommand("SRUI_NPCSave")]
        private void cmdNPCSave(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UISelect);

            var info = NPCCreator[player.userID];
            npcDealers.NPCIDs[info.NPCID].isCustom = true;
            npcDealers.NPCIDs[info.NPCID].itemList = info.itemList;
            npcDealers.NPCIDs[info.NPCID].commandList = info.commandList;
            npcDealers.NPCIDs[info.NPCID].kitList = info.kitList;
            SaveNPC();
            CreateNPCMenu(info.NPCID);
            NPCCreator.Remove(player.userID);
            SendReply(player, msg("clootsucc", player.UserIDString));
        }
        #endregion     

        #region Chat Commands 
        [ChatCommand("s")]
        private void cmdStore(BasePlayer player, string command, string[] args)
        {
            if ((configData.Options.NPCDealers_Only && isAuth(player)) || !configData.Options.NPCDealers_Only)
            {
                OpenStore(player);
            }            
        }

        [ChatCommand("rewards")]
        private void cmdRewards(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "V " + Version, msg("title", player.UserIDString));                
                SendMSG(player, msg("chatCheck1", player.UserIDString), msg("chatCheck", player.UserIDString));
                SendMSG(player, msg("storeSyn2", player.UserIDString), msg("storeSyn21", player.UserIDString));
                if (isAuth(player))
                {
                    SendMSG(player, msg("chatAddKit", player.UserIDString), msg("addSynKit", player.UserIDString));
                    SendMSG(player, msg("chatAddItem", player.UserIDString), msg("addSynItem", player.UserIDString));
                    SendMSG(player, msg("chatAddCommand", player.UserIDString), msg("addSynCommand", player.UserIDString));
                    SendMSG(player, msg("chatRemove", player.UserIDString), msg("remSynKit", player.UserIDString));
                    SendMSG(player, msg("chatRemove", player.UserIDString), msg("remSynItem", player.UserIDString));
                    SendMSG(player, msg("chatRemove", player.UserIDString), msg("remSynCommand", player.UserIDString));
                    SendMSG(player, msg("chatList1", player.UserIDString), msg("chatList", player.UserIDString));                    
                }
                return;
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "check":
                        if (!PointCache.ContainsKey(player.userID))
                        {
                            SendMSG(player, msg("errorProfile", player.UserIDString));
                            Puts(msg("errorPCon", player.UserIDString), player.displayName);
                            return;
                        }
                        int points = PointCache[player.userID];
                        SendMSG(player, string.Format(msg("tpointsAvail", player.UserIDString), points));
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
                                            if (i <= 0) { SendMSG(player, msg("noCost", player.UserIDString)); return; }

                                            object isKit = Kits?.Call("isKit", new object[] { args[3] });
                                            if (isKit is bool)
                                                if ((bool)isKit)
                                                {
                                                    if (!rewardData.RewardKits.ContainsKey(args[2]))
                                                        rewardData.RewardKits.Add(args[2], new KitInfo() { KitName = args[3], Cost = i, Description = "" });
                                                    else
                                                    {
                                                        SendMSG(player, string.Format(msg("rewardExisting", player.UserIDString), args[2]));
                                                        return;
                                                    }
                                                    SendMSG(player, string.Format(msg("addSuccess", player.UserIDString), "kit", args[2], i));
                                                    SaveRewards();
                                                    return;
                                                }
                                            SendMSG(player, msg("noKit", player.UserIDString), "");
                                            return;
                                        }
                                        SendMSG(player, "", msg("addSynKit", player.UserIDString));
                                        return;
                                    case "item":
                                        if (args.Length >= 3)
                                        {
                                            int i = -1;
                                            int.TryParse(args[2], out i);
                                            if (i <= 0) { SendMSG(player, msg("noCost", player.UserIDString)); return; }
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
                                                    URL = "",
                                                    TargetID = !item.IsBlueprint() ? 0 : item.blueprintTarget
                                                };                                                
                                                rewardData.RewardItems.Add(rewardData.RewardItems.Count, newItem);
                                                SendMSG(player, string.Format(msg("addSuccess", player.UserIDString), "item", newItem.DisplayName, i));
                                                SaveRewards();
                                                return;
                                            }
                                            SendMSG(player, "", msg("itemInHand", player.UserIDString));
                                            return;
                                        }
                                        SendMSG(player, "", msg("addSynItem", player.UserIDString));
                                        return;
                                    case "command":
                                        
                                        if (args.Length == 5)
                                        {
                                            int i = -1;
                                            int.TryParse(args[4], out i);
                                            if (i <= 0) { SendMSG(player, msg("noCost", player.UserIDString)); return; }
                                            rewardData.RewardCommands.Add(args[2], new CommandInfo { Command = new List<string> { args[3] }, Cost = i, Description = "" });
                                            SendMSG(player, string.Format(msg("addSuccess", player.UserIDString), "command", args[2], i));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, "", msg("addSynCommand", player.UserIDString));
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
                                            SendMSG(player, "", string.Format(msg("remSuccess", player.UserIDString), args[2]));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, msg("noKitRem", player.UserIDString), "");
                                        return;
                                    case "item":
                                        int i;
                                        if (!int.TryParse(args[2], out i))
                                        {
                                            SendMSG(player, "", msg("itemIDHelp", player.UserIDString));
                                            return;
                                        }
                                        if (rewardData.RewardItems.ContainsKey(i))
                                        {
                                            SendMSG(player, "", string.Format(msg("remSuccess", player.UserIDString), rewardData.RewardItems[i].DisplayName));
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
                                        SendMSG(player, msg("noItemRem", player.UserIDString), "");
                                        return;
                                    case "command":
                                        if (rewardData.RewardCommands.ContainsKey(args[2]))
                                        {
                                            rewardData.RewardKits.Remove(args[2]);
                                            SendMSG(player, "", string.Format(msg("remSuccess", player.UserIDString), args[2]));
                                            SaveRewards();
                                            return;
                                        }
                                        SendMSG(player, msg("noCommandRem", player.UserIDString), "");
                                        return;
                                }                               
                            }
                
                        return;                    
                }                
            }
        }               

        

        [ChatCommand("sr")]
        private void cmdSR(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, msg("srAdd2", player.UserIDString), "/sr add <playername> <amount>");
                SendMSG(player, msg("srTake2", player.UserIDString), "/sr take <playername> <amount>");
                SendMSG(player, msg("srClear2", player.UserIDString), "/sr clear <playername>");
                SendMSG(player, msg("srCheck", player.UserIDString), "/sr check <playername>");
                SendMSG(player, msg("srAdd3", player.UserIDString), "/sr add all <amount>");
                SendMSG(player, msg("srTake3", player.UserIDString), "/sr take all <amount>");
                SendMSG(player, msg("srClear3", player.UserIDString), "/sr clear all");                
                return;
            }
            if (args.Length >= 2)
            {
                if(args[1].ToLower() == "all")
                {
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                if (int.TryParse(args[2], out i))
                                {
                                    var pList = PointCache.Keys.ToArray();
                                    foreach (var entry in pList)
                                        AddPoints(entry, i);
                                    SendMSG(player, string.Format(msg("addPointsAll", player.UserIDString), i));
                                }
                            }
                            return;

                        case "take":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                if (int.TryParse(args[2], out i))
                                {
                                    var pList = PointCache.Keys.ToArray();
                                    foreach (var entry in pList)
                                    {
                                        var amount = CheckPoints(entry);
                                        if (amount is int)
                                        {
                                            if ((int)amount >= i)
                                                TakePoints(entry, i);
                                            else TakePoints(entry, (int)amount);
                                        }
                                    }

                                    SendMSG(player, string.Format(msg("remPointsAll", player.UserIDString), i));
                                }                                
                            }
                            return;
                        case "clear":
                            PointCache.Clear();
                            SendMSG(player, msg("clearAll", player.UserIDString));
                            return;
                    }
                }
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
                                        SendMSG(player, string.Format(msg("addPoints", player.UserIDString), target.displayName, i));
                            }
                            return;

                        case "take":
                            if (args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null) 
                                        SendMSG(player, string.Format(msg("removePoints", player.UserIDString), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendMSG(player, string.Format(msg("clearPlayer", player.UserIDString), target.displayName));
                            return;
                        case "check":
                            if (args.Length == 2)
                            {
                                if (PointCache.ContainsKey(target.userID))
                                {
                                    var points = PointCache[target.userID];
                                    SendMSG(player, string.Format("{0} - {2}: {1}", target.displayName, points, msg("storeRP")));
                                    return;
                                }
                                SendMSG(player, string.Format(msg("noProfile", player.UserIDString), target.displayName));
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
                SendReply(arg, "sr add <playername> <amount>" + msg("srAdd2"));
                SendReply(arg, "sr take <playername> <amount>" + msg("srTake2"));
                SendReply(arg, "sr clear <playername>" + msg("srClear2"));
                SendReply(arg, "sr check <playername>" + msg("srCheck"));
                SendReply(arg, "sr add all <amount>" + msg("srAdd3"));
                SendReply(arg, "sr take all <amount>" + msg("srTake3"));
                SendReply(arg, "sr clear all" + msg("srClear3"));
                return;
            }
            if (arg.Args.Length >= 2)
            {
                if (arg.Args[1].ToLower() == "all")
                {
                    switch (arg.Args[0].ToLower())
                    {
                        case "add":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                if (int.TryParse(arg.Args[2], out i))
                                {
                                    var pList = PointCache.Keys.ToArray();
                                    foreach (var entry in pList)
                                        AddPoints(entry, i);
                                    SendReply(arg, string.Format(msg("addPointsAll"), i));
                                }
                            }
                            return;

                        case "take":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                if (int.TryParse(arg.Args[2], out i))
                                {
                                    var pList = PointCache.Keys.ToArray();
                                    foreach (var entry in pList)
                                    {
                                        var amount = CheckPoints(entry);
                                        if (amount is int)
                                        {
                                            if ((int)amount >= i)
                                                TakePoints(entry, i);
                                            else TakePoints(entry, (int)amount);
                                        }                                        
                                    }

                                    SendReply(arg, string.Format(msg("remPointsAll"), i));
                                }
                            }
                            return;
                        case "clear":
                            PointCache.Clear();
                            SendReply(arg, msg("clearAll"));
                            return;
                    }
                }
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
                                        SendReply(arg, string.Format(msg("addPoints"), target.displayName, i));
                            }
                            return;
                        case "take":
                            if (arg.Args.Length == 3)
                            {
                                int i = 0;
                                int.TryParse(arg.Args[2], out i);
                                if (i != 0)
                                    if (TakePoints(target.userID, i) != null)
                                        SendReply(arg, string.Format(msg("removePoints"), i, target.displayName));
                            }
                            return;
                        case "clear":
                            RemovePlayer(target.userID);
                            SendReply(arg, string.Format(msg("clearPlayer"), target.displayName));
                            return;
                        case "check":
                            if (arg.Args.Length == 2)
                            {
                                if (PointCache.ContainsKey(target.userID))
                                {
                                    var points = PointCache[target.userID];
                                    SendReply(arg, string.Format("{0} - {2}: {1}", target.displayName, points, msg("storeRP")));
                                    return;
                                }
                                SendReply(arg, string.Format(msg("noProfile"), target.displayName));
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
        void SaveData()
        {
            playerData.Players = PointCache;
            PlayerData.WriteObject(playerData);
            Puts("Saved player data");            
        }
        void SaveRewards()
        {
            RewardData.WriteObject(rewardData);
            Puts("Saved reward data");
        }
        void SaveNPC()
        {
            NPC_Dealers.WriteObject(npcDealers);
            Puts("Saved NPC data");
        }
        private void SaveLoop() => saveTimer = timer.Once(configData.Options.Save_Interval * 60, () => { SaveData(); SaveLoop(); });
            
        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<PointData>();
                PointCache = playerData.Players;
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                playerData = new PointData();
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
        class Tabs
        {
            public bool Disable_Kits { get; set; }
            public bool Disable_Items { get; set; }
            public bool Disable_Commands { get; set; }
            public bool Disable_CurrencyExchange { get; set; }
            public bool Disable_CurrencyTransfer { get; set; }
        }
        class Messaging
        {
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
        }
        class Exchange
        {
            public int Econ_ExchangeRate { get; set; }
            public int RP_ExchangeRate { get; set; }
        }
        class Options
        {
            public bool LogRPTransactions { get; set; }
            public int Save_Interval { get; set; }
            public bool NPCDealers_Only { get; set; }
            public bool Use_PTT { get; set; }
        }
        class UIOptions
        {
            public bool DisableUI_FadeIn { get; set; }
            public bool ShowKitContents { get; set; }
        }
        class ConfigData
        {
            public Tabs Categories { get; set; }
            public Exchange CurrencyExchange { get; set; }
            public Messaging Messaging { get; set; }
            public Options Options { get; set; }
            public UIOptions UI_Options { get; set; }           
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
                Categories = new Tabs
                {
                    Disable_Commands = false,
                    Disable_Items = false,
                    Disable_Kits = false,
                    Disable_CurrencyExchange = false,
                    Disable_CurrencyTransfer = false                    
                },
                CurrencyExchange = new Exchange
                {
                    Econ_ExchangeRate = 250,
                    RP_ExchangeRate = 1
                },
                Messaging = new Messaging
                {
                    MSG_MainColor = "<color=orange>",
                    MSG_Color = "<color=#939393>"
                },
                Options = new Options
                {
                    LogRPTransactions = true,
                    NPCDealers_Only = false,
                    Save_Interval = 10,
                    Use_PTT = true
                },
                UI_Options = new UIOptions
                {
                    DisableUI_FadeIn = false,
                    ShowKitContents = true
                }                
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

        #region Localization
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
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
            {"tpointsAvail", "You have {0} reward point(s) to spend" },
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
            {"addPointsAll", "You have given everyone {0} points" },
            {"remPointsAll", "You have taken {0} points from everyone"},
            {"clearAll", "You have removed all reward profiles" },
            {"srAdd2", " - Adds <amount> of reward points to <playername>" },
            {"srAdd3", " - Adds <amount> of reward points to all players" },
            {"srTake2", " - Takes <amount> of reward points from <playername>" },
            {"srTake3", " - Takes <amount> of reward points from all players" },
            {"srClear2", " - Clears <playername>'s reward profile" },
            {"srClear3", " - Clears all reward profiles" },
            {"srCheck", " - Check a players point count" },
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
            {"transfer3", "You have transferred {0} {1} to {2}" },
            {"clootsucc", "You have successfully created a new loot list for this NPC" },
            {"save", "Save"},
            {"cldesc", "Select items, kits and commands to add to this NPC's custom store list" },
            {"clcanc", "You have cancelled custom loot creation"}
        };
        #endregion
    }
}
