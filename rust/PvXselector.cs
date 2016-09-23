using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;                      //DateTime
using System.Collections.Generic;  //Required for Whilelist
//using System.Data;
//using System.Globalization;
using System.Linq;
//using System.Reflection;
using UnityEngine;
using Oxide.Core.Configuration;
//using ConVar;
//using Facepunch;
//using Network;
//using ProtoBuf;
//using System.Runtime.CompilerServices;
//using Oxide.Core.Libraries.Covalence;
//using System.Text.RegularExpressions;
//using Oxide.Plugins;

namespace Oxide.Plugins
{
    [Info("PvXSelector", "Alphawar", "0.9.4", ResourceId = 1817)]
    [Description("Player vs X Selector")]
    class PvXselector : RustPlugin
    {

        #region Data/PlayerJoin/ServerInit
        //Loaded goes first
        PlayerDataStorage playerData;
        private DynamicConfigFile PlayerData;
        TicketDataStorage ticketData;
        private DynamicConfigFile TicketData;
        TicketLogStorage ticketLog;
        private DynamicConfigFile TicketLog;

        private Hash<ulong, PlayerInfo> InfoCache = new Hash<ulong, PlayerInfo>();
        private List<ulong> SleeperCache = new List<ulong>();
        private List<BasePlayer> AdminPlayerMode = new List<BasePlayer>();
        private List<BasePlayer> activeAdmins = new List<BasePlayer>();
        private List<ulong> antiChatSpam = new List<ulong>();
        public List<ulong> IgnoreXPFunction = new List<ulong>();
        private Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();

        class PlayerDataStorage
        {
            public Hash<ulong, PlayerInfo> Info = new Hash<ulong, PlayerInfo>();
            public List<ulong> sleepers = new List<ulong>();
        }
        class TicketDataStorage
        {
            public Dictionary<int, ulong> Link = new Dictionary<int, ulong>();
            public Dictionary<ulong, Ticket> Info = new Dictionary<ulong, Ticket>();
            public Dictionary<ulong, string> Notification = new Dictionary<ulong, string>();
        }
        class TicketLogStorage
        {
            public Dictionary<int, LogData> Log = new Dictionary<int, LogData>();
        }

        class PlayerInfo
        {
            public string username;
            public double timeStamp;
            public string mode;
            public bool ticket;
            public int pveLevel;
            public int pvpLevel;
            public float xpSpent;
            public float xpUnSpent;
        }
        class Ticket
        {
            public int TicketNumber;
            public string username;
            public string requested;
            public string reason;
            public string timeStamp;
        }
        class LogData
        {
            public ulong UserId;
            public ulong AdminId;
            public string requested;
            public string reason;
            public bool Accepted;
            public string createdTimeStamp;
            public string ClosedTimeStamp;
        }

        void OnServerInitialized()
        {
            LoadData();
            InfoCache = playerData.Info;
            checkPlayersRegistered();
            RegisterGroups();
            foreach (BasePlayer _player in BasePlayer.activePlayerList)
            {
                if (!isNPC(_player))
                {
                    createPvXIndicator(_player);
                    storePlayerLevel(_player);
                    if (hasPerm(_player, "admin"))
                    {
                        activeAdmins.Add(_player);
                        createAdminIndicator(_player);
                    }
                    updatePlayerChatTag(_player);
                }
            }
        }
        void Loaded()
        {
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("PvX/PlayerData");
            TicketData = Interface.Oxide.DataFileSystem.GetFile("PvX/TicketData");
            TicketLog = Interface.Oxide.DataFileSystem.GetFile("PvX/TicketLog");
            lang.RegisterMessages(messages, this);
            permissionHandle();
            LoadVariables();
            foreach (var pm in XPMultiplier)
                XPpermHandle(pm.Key);
        }
        void Unloaded()
        {
            foreach (var _player in BasePlayer.activePlayerList)
            {
                storePlayerLevel(_player);
                DestroyAllUI(_player);
            }
            SaveAll();
        }

        void saveCacheData()
        {
            playerData.Info = InfoCache;
            playerData.sleepers = SleeperCache;
            PlayerData.WriteObject(playerData);
        }
        void saveTicketData()
        {
            TicketData.WriteObject(ticketData);
        }
        void saveTicketLog()
        {
            TicketLog.WriteObject(TicketLog);
        }
        void SaveAll()
        {
            playerData.Info = InfoCache;
            PlayerData.WriteObject(playerData);
            TicketData.WriteObject(ticketData);
            TicketLog.WriteObject(ticketLog);
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
                InfoCache = playerData.Info;
            }
            try
            {
                ticketData = TicketData.ReadObject<TicketDataStorage>();
            }
            catch
            {
                Puts("Couldn't load Ticket data, creating new datafile");
                ticketData = new TicketDataStorage();
            }
            try
            {
                ticketLog = TicketLog.ReadObject<TicketLogStorage>();
            }
            catch
            {
                Puts("Couldn't load Ticket Log, creating new datafile");
                ticketLog = new TicketLogStorage();
            }
        }
        void OnPlayerInit(BasePlayer _player)
        {
            if (_player == null) return;
            if (_player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(_player));
                return;
            }
            if (hasPerm(_player, "admin"))
            {
                activeAdmins.Add(_player);
                createAdminIndicator(_player);
            }
            if (!InfoCache.ContainsKey(_player.userID))
            {
                addPlayer(_player);
                updatePlayerChatTag(_player);
                storePlayerLevel(_player);
                saveCacheData();
                createPvXIndicator(_player);
                SelectorOverlay(_player);
                return;
            }
            updatePlayerChatTag(_player);
            storePlayerLevel(_player);
            if (SleeperCache.Contains(_player.userID))
            {
                pvxUpdateXPDataFile(_player);
                SleeperCache.Remove(_player.userID);
                saveCacheData();
            }
            createPvXIndicator(_player);
            if (InfoCache[_player.userID].mode != "NA") return;
            else if (ticketData.Notification.ContainsKey(_player.userID)) LangMSG(_player, "TickClosLogin", ticketData.Notification[_player.userID]);
            else SelectorOverlay(_player);
        }
        void OnPlayerDisconnected(BasePlayer _player)
        {
            if (hasPerm(_player, "admin"))
            {
                activeAdmins.Remove(_player);
            }
        }

        static string pvxPlayerUI = "pvxPlayerModeUI";
        static string pvxAdminUI = "pvxAdminTicketCountUI";
        static string UIPanel = "UIPanel";
        static string UIEntry = "UIEntry";
        #endregion

        #region Config/Permision/Plugin Ref
        public static bool DisableUI_FadeIn;
        private bool DebugMode;
        private bool NamesIncludeSleepers;
        private bool EnablePvECap;
        private bool EnablePvPCap;
        private bool PvELootNPC;
        private bool PvPLootNPC;
        private bool PvEDamageNPC;
        private bool PvPDamageNPC;
        private bool NPCDamagePvE;
        private bool NPCDamagePvP;
        private int PvECap;
        private int PvPCap;
        private string ChatPrefixColor;
        private string ChatPrefix;
        private string ChatMessageColor;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
        void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
        {
            //settings
            DisableUI_FadeIn = Convert.ToBoolean(GetConfig("Setting", "DisableUI Fadein", false));
            DebugMode = Convert.ToBoolean(GetConfig("Settings", "DebugMode", false));
            PvELootNPC = Convert.ToBoolean(GetConfig("npc-Settings", "PvELoot", true));
            PvPLootNPC = Convert.ToBoolean(GetConfig("npc-Settings", "PvPLoot", true));
            PvEDamageNPC = Convert.ToBoolean(GetConfig("npc-Settings", "PvEDamage", true));
            PvPDamageNPC = Convert.ToBoolean(GetConfig("npc-Settings", "PvPDamage", true));
            NPCDamagePvE = Convert.ToBoolean(GetConfig("npc-Settings", "NPCDamagetoPVE", true));
            NPCDamagePvP = Convert.ToBoolean(GetConfig("npc-Settings", "PvPDamagetoPVP", true));
            EnablePvECap = Convert.ToBoolean(GetConfig("Cap-Settings", "PvE-Enabled", false));
            EnablePvPCap = Convert.ToBoolean(GetConfig("Cap-Settings", "PvP-Enabled", false));
            PvECap = Convert.ToInt16(GetConfig("Cap-Settings", "PvE-Max-Level", 99));
            PvPCap = Convert.ToInt16(GetConfig("Cap-Settings", "PvP-Max-Level", 99));
            //chat
            ChatPrefix = Convert.ToString(GetConfig("ChatSettings", "ChatPrefix", "PvX"));
            ChatPrefixColor = Convert.ToString(GetConfig("ChatSettings", "ChatPrefixColor", "008800"));
            ChatMessageColor = Convert.ToString(GetConfig("ChatSettings", "ChatMessageColor", "yellow"));
            XPMultiplier = GetConfig<Dictionary<string, object>>("Permission Multipliers", new Dictionary<string, object> { { "adminxp", 2f }, { "vipxp", 1.5f } });
        }

        object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
            }
            return value;
        }
        T GetConfig<T>(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null)
            {
                Config.Set(args);
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        bool hasPerm(BasePlayer _player, string perm, string reason = null)
        {
            string regPerm = Title.ToLower() + "." + perm; //pvxselector.admin
            if (permission.UserHasPermission(_player.UserIDString, regPerm)) return true;
            if (reason != "null")
                SendReply(_player, reason);
            return false;
        }

        void permissionHandle()
        {
            string[] Permissionarray = { "admin", "wipe" };
            foreach (string i in Permissionarray)
            {
                string regPerm = Title.ToLower() + "." + i;
                Puts("Checking if " + regPerm + " is registered.");
                if (!permission.PermissionExists(regPerm))
                {
                    permission.RegisterPermission(regPerm, this);
                    Puts(regPerm + " is registered.");
                }
                else
                {
                    Puts(regPerm + " is already registered.");
                }
            }
        }
        #endregion

        #region UI Creation
        class QUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
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
                    new CuiElement().Parent = "Hud",
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
                if (DisableUI_FadeIn)
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
                if (DisableUI_FadeIn)
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
                if (DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }

        private void DestroyAllUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, pvxPlayerUI);
            CuiHelper.DestroyUi(player, pvxAdminUI);
            //DestroyEntries(player);
        }
        private void DestroyUI(BasePlayer player, string _ui)
        {
            CuiHelper.DestroyUi(player, _ui);
        }
        //private void DestroyEntries(BasePlayer player)
        //{
        //    CuiHelper.DestroyUi(player, UIPanel);
        //    if (OpenUI.ContainsKey(player.userID))
        //    {
        //        foreach (var entry in OpenUI[player.userID])
        //            CuiHelper.DestroyUi(player, entry);
        //        OpenUI.Remove(player.userID);
        //    }
        //}
        private void AddUIString(BasePlayer player, string name)
        {
            if (!OpenUI.ContainsKey(player.userID))
                OpenUI.Add(player.userID, new List<string>());
            OpenUI[player.userID].Add(name);
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"darkSoft", "0.1 0.1 0.1 0.20" },
            {"light", "0.7 0.7 0.7 0.3" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"Red", "0.90 0.2 0.2 0.8" },
            {"Green", "0.2 0.7 0.2 1.0" },
            {"Yellow", "0.90 0.90 0.2 0.8" },
            {"YellowSoft", "0.90 0.90 0.2 0.2" },
            {"Black", "0.0 0.0 0.0 1.00" },
            {"White", "1.0 1.0 1.0 1.00" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttonopen", "0.2 0.8 0.2 0.9" },
            {"buttoncompleted", "0 0.5 0.1 0.9" },
            {"buttonred", "0.85 0 0.35 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"grey8", "0.8 0.8 0.8 1.0" }
        };
        #endregion

        #region GUIs
        private void SelectorOverlay(BasePlayer player)
        {
            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image ={
                    Color = "0.1 0.1 0.1 1"},
                RectTransform ={
                    AnchorMin = "0.1 0.15",
                    AnchorMax = "0.4 0.25"},
                CursorEnabled = true
            }, "Hud", "SelOverLay");
            var PVP = new CuiButton
            {
                Button ={
                    Command = "PvXSelection pvp",
                    Close = mainName,
                    Color = "0.8 0.2 0.2 1"},
                RectTransform ={
                    AnchorMin = "0.2 0.16",
                    AnchorMax = "0.45 0.8"},
                Text ={
                    Text = "PvP",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter}
            };
            var PVE = new CuiButton
            {
                Button ={
                    Command = "PvXSelection pve",
                    Close = mainName,
                    Color = "0.2 0.8 0.2 1"},
                RectTransform ={
                    AnchorMin = "0.55 0.2",
                    AnchorMax = "0.8 0.8"},
                Text ={
                    Text = "PvE",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter}
            };
            elements.Add(PVP, mainName);
            elements.Add(PVE, mainName);
            CuiHelper.AddUi(player, elements);
        }

        private void createPvXIndicator(BasePlayer _player)
        {
            var indicatorContainer = QUI.CreateElementContainer(pvxPlayerUI,
                UIColors["darkSoft"],
                "0.48 0.11",
                "0.52 0.14");
            if (InfoCache[_player.userID].mode == "NA")
                indicatorContainer = QUI.CreateElementContainer(pvxPlayerUI,
                    UIColors["Red"],
                    "0.48 0.11",
                    "0.52 0.14");
            else if (ticketData.Info.ContainsKey(_player.userID))
                indicatorContainer = QUI.CreateElementContainer(pvxPlayerUI,
                    UIColors["YellowSoft"],
                    "0.48 0.11",
                    "0.52 0.14");
            if (AdminPlayerMode.Contains(_player))
            {
                QUI.CreateLabel(ref indicatorContainer,
                    pvxPlayerUI,
                    UIColors["Green"],
                    InfoCache[_player.userID].mode,
                    15,
                    "0.1 0.1",
                    "0.90 0.99");
            }
            else
            {
                QUI.CreateLabel(ref indicatorContainer,
                    pvxPlayerUI,
                    "White",
                    InfoCache[_player.userID].mode,
                    15,
                    "0.1 0.1",
                    "0.90 0.99");
            }
            CuiHelper.AddUi(_player, indicatorContainer);
        }
        private void updatePvXIndicator(BasePlayer _player)
        {
            CuiHelper.DestroyUi(_player, pvxPlayerUI);
            createPvXIndicator(_player);
        }

        private void createAdminIndicator(BasePlayer _player)
        {
            if (!hasPerm(_player, "admin")) return;
            var adminCountContainer = QUI.CreateElementContainer(
                pvxAdminUI,
                UIColors["dark"],
                "0.166 0.055",
                "0.34 0.0955");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                "White",
                "PvX Tickets",
                10,
                "0.0 0.1",
                "0.3 0.90");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                "White",
                string.Format("Open: {0}", ticketData.Info.Count.ToString()),
                10,
                "0.301 0.1",
                "0.65 0.90");
            QUI.CreateLabel(ref adminCountContainer,
                pvxAdminUI,
                "White",
                string.Format("Closed: {0}", ticketLog.Log.Count.ToString()),
                10,
                "0.651 0.1",
                "1 0.90");
            CuiHelper.AddUi(_player, adminCountContainer);
        }
        private void UpdateAdminIndicator()
        {
            if (activeAdmins == null) return;
            else if (activeAdmins.Count < 1) return;
            foreach (BasePlayer _player in activeAdmins)
            {
                CuiHelper.DestroyUi(_player, pvxAdminUI);
                createAdminIndicator(_player);
            }
        }

        private void createButton(BasePlayer _player)
        { }
        #endregion

        #region Lang/Chat
        void LangMSG(BasePlayer _player, string langMsg, params object[] args)
        {
            string message = lang.GetMessage(langMsg, this, _player.UserIDString);
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void LangMSGBroadcast(string langMsg, params object[] args)
        {
            string message = lang.GetMessage(langMsg, this);
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void BroadcastMessageHandle(string message, params object[] args)
        {
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void ChatMessageHandle(BasePlayer _player, string message, params object[] args)
        {
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }
        void PutsPlayerHandle(BasePlayer _player, string msg, params object[] args)
        {
            Puts(msg, args);
            PrintToChat(_player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{msg}</color>", args);
        }
        void PutsPlayerHandleLang(BasePlayer _player, string langMsg, params object[] args)
        {
            string msg = lang.GetMessage(langMsg, this, _player.UserIDString);
            Puts(msg, args);
            SendReply(_player, msg, args);
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"xx", "xxx" },
            {"notAllwPickup", "You can't pick this item as owner is: {0}" },
            {"AdmModeRem", "You have deactivated Admin Mode" },
            {"AdmModeAdd", "You are now in Admin mode" },
            {"lvlRedxpSav", "Your Level was reduced, Lost xp has been saved." },
            {"lvlIncrxpRes", "Your Level has been increased, Lost xp Restored." },
            {"numbonly", "Incorrect format: Included letter in ticketID" },
            {"TickCrea", "You have created a ticket" },
            {"TickCanc", "You have Canceled your ticket" },
            {"TickAcep", "Your Ticket has been accepted" },
            {"TickDecl", "Your Ticket has been declined" },
            {"TickAcepAdm", "Your Ticket accepted the ticket" },
            {"TickDeclAdm", "Your Ticket decline the ticket" },
            {"TickClosLogin", "Welcome back, Your ticket was {0}" },
            {"TickList", "Ticket#: {0}, User: {1}" },
            {"TickDet", "Ticket Details:" },
            {"TickID", "Ticket ID: {0}" },
            {"TickName", "Username: {0}" },
            {"TickStmID", "SteamID: {0}" },
            {"TickSelc", "Selected: {0}" },
            {"TickRsn", "Reason: {0}" },
            {"TickDate", "Ticket Created: {0}" },
            {"TickCnt", "Open Tickets: {0}" },
            {"TickNotAvail", "Ticket#:{0} Does not exist" },
            {"ComndList", "PvX Command List:" },
            {"CompTickCnt", "Closed Tickets: {0}" },
            {"IncoForm", "Incorrect format" },
            {"IncoFormPleaUse", "Incorrect format Please Use:" },
            {"TicketDefaultReason", "Change Requested Via Chat" },
            {"NoTicket", "There are no tickets to display" },
            {"AlreadySubmitted", "You have already requested to change, Please conctact your admin" },
            {"PvETarget", "You are attacking a PvE player" },
            {"NoActTick", "You do not have an active ticket" },
            {"RSNChan", "You have changed your tickets reason." },
            {"PvEStructure", "That structure belongs to a PvE player" },
            {"NoXPModeNA", "You will not earn XP until you have selected PvE/PvP" },
            {"NoSaveLvLNA", "Your levl is not saved unless you select PvE or PvP" },
            {"TargisGod", "You are attacking a god" },
            {"YouisGod", "You are attacking a god" },
            {"MissPerm", "You do not have the required permision" }
        };
        #endregion

        #region Ticket Functions
        void createTicket(BasePlayer _player, string selection)
        {
            int _TicketNumber = GetNewID();
            string _username = _player.displayName;
            string _requested = selection;
            string _reason = lang.GetMessage("TicketDefaultReason", this, _player.UserIDString);
            ticketData.Link.Add(_TicketNumber, _player.userID);
            ticketData.Info.Add(_player.userID, new Ticket
            {
                username = _username,
                TicketNumber = _TicketNumber,
                reason = _reason,
                requested = _requested,
                timeStamp = DateTimeStamp()
            });
            LangMSG(_player, "TickCrea");
            InfoCache[_player.userID].ticket = true;
            SaveAll();
            UpdateAdminIndicator();
            updatePvXIndicator(_player);
        }
        void cancelTicket(BasePlayer _player)
        {
            if (playerData.Info[_player.userID].ticket == false) return;
            int _ticketNumber = ticketData.Info[_player.userID].TicketNumber;
            ticketData.Link.Remove(_ticketNumber);
            ticketData.Info.Remove(_player.userID);
            InfoCache[_player.userID].ticket = false;
            SaveAll();
            LangMSG(_player, "TickCanc");
            updatePvXIndicator(_player);
            UpdateAdminIndicator();
            return;
        }
        void ticketAccept(BasePlayer _admin, int _ticketID)
        {
            BasePlayer _ticketOwner = basePlayerByID(ticketData.Link[_ticketID]);
            int _unspentxp = _ticketOwner.UnspentXp;
            ulong _UserID = _ticketOwner.userID;
            int _logID = NewLogID();
            IgnoreXPFunction.Add(_UserID);
            LangMSG(_admin, "TickAcepAdm");
            InfoCache[_UserID].mode = ticketData.Info[_UserID].requested;
            ticketLog.Log.Add(_logID, new LogData
            {
                Accepted = true,
                ClosedTimeStamp = DateTimeStamp(),
                AdminId = _admin.userID,
                createdTimeStamp = ticketData.Info[_UserID].timeStamp,
                reason = ticketData.Info[_UserID].reason,
                requested = ticketData.Info[_UserID].requested,
                UserId = _UserID
            });
            playerData.Info[_UserID].ticket = false;
            if (ticketData.Info[_UserID].requested == "pve")
                PvELevelHandle(_ticketOwner);
            else if (ticketData.Info[_UserID].requested == "pvp")
                PvPLevelHandle(_ticketOwner, _unspentxp);
            ticketData.Info.Remove(_UserID);
            ticketData.Link.Remove(_ticketID);
            updatePlayerChatTag(_ticketOwner);
            UpdateAdminIndicator();
            SaveAll();
            if (_ticketOwner.IsConnected())
            {
                LangMSG(_ticketOwner, "TickAcep");
                updatePvXIndicator(_ticketOwner);
            }
            else ticketData.Notification.Add(_ticketOwner.userID, "Accepted");
            IgnoreXPFunction.Remove(_UserID);
        }
        void ticketDecline(BasePlayer _admin, int _ticketID)
        {
            BasePlayer _ticketOwner = basePlayerByID(ticketData.Link[_ticketID]);
            ulong _UserID = _ticketOwner.userID;
            int _logID = NewLogID();
            LangMSG(_admin, "TickDeclAdm");
            ticketLog.Log.Add(_logID, new LogData
            {
                Accepted = false,
                ClosedTimeStamp = DateTimeStamp(),
                AdminId = _admin.userID,
                createdTimeStamp = ticketData.Info[_UserID].timeStamp,
                reason = ticketData.Info[_UserID].reason,
                requested = ticketData.Info[_UserID].requested,
                UserId = _UserID
            });
            playerData.Info[_UserID].ticket = false;
            ticketData.Info.Remove(_UserID);
            ticketData.Link.Remove(_ticketID);
            UpdateAdminIndicator();
            SaveAll();
            if (_ticketOwner.IsConnected())
            {
                LangMSG(_ticketOwner, "TickDecl");
                updatePvXIndicator(_ticketOwner);
            }
            else ticketData.Notification.Add(_ticketOwner.userID, "Declined");
        }
        void ticketCount(BasePlayer _player)
        {
            LangMSG(_player, "TickCnt", ticketData.Link.Count);
            LangMSG(_player, "CompTickCnt", ticketLog.Log.Count);
        }
        void listTickets(BasePlayer _player)
        {
            if (ticketData.Link.Count > 0)
            {
                foreach (var ticket in ticketData.Info)
                {
                    ulong _key = ticket.Key;
                    PutsPlayerHandleLang(_player, "TickList", ticketData.Info[_key].TicketNumber, ticketData.Info[_key].username);
                }
            }
        }
        void displayTicket(BasePlayer _player, int _ticketID)
        {
            if (ticketData.Link.ContainsKey(_ticketID))
            {
                ulong _key = ticketData.Link[_ticketID];
                //DateTime _date = DateTime.FromOADate(ticketData.Info[_key].timeStamp);
                LangMSG(_player, "TickDet");
                LangMSG(_player, "TickID", _ticketID);
                LangMSG(_player, "TickName", ticketData.Info[_key].username);
                LangMSG(_player, "TickStmID", _key);
                LangMSG(_player, "TickSelc", ticketData.Info[_key].requested);
                LangMSG(_player, "TickRsn", ticketData.Info[_key].reason);
                LangMSG(_player, "TickDate", ticketData.Info[_key].timeStamp);
            }
            else LangMSG(_player, "TickNotAvail", _ticketID);
        }
        bool playerHasTicket(BasePlayer _player)
        {
            if (InfoCache[_player.userID].ticket == true) return true;
            else return false;
        }
        int GetNewID()
        {
            for (int _i = 1; _i <= 500; _i++)
            {
                if (ticketData.Link.ContainsKey(_i)) { }//Place Debug code in future
                else
                {
                    //Puts("Key {0} doesnt exist, Returning ticket number", _i); //debug
                    return _i;
                }
            }
            return 0;
        }
        int NewLogID()
        {
            for (int _i = 1; _i <= 500; _i++)
            {
                if (ticketLog.Log.ContainsKey(_i)) { }
                else
                {
                    //Puts("Key {0} doesnt exist, Returning ticket number", _i); //debug
                    return _i;
                }
            }
            return 0;
        }
        #endregion

        #region XP Functions
        Dictionary<string, object> XPMultiplier;

        void XPpermHandle(string perm)
        {
            string regPerm = Title.ToLower() + "." + perm;
            Puts("Checking if " + regPerm + " is registered.");
            if (!permission.PermissionExists(regPerm))
            {
                permission.RegisterPermission(regPerm, this);
                Puts(regPerm + " is registered.");
            }
            else
            {
                Puts(regPerm + " is already registered.");
            }
        }
        float XPMultPerm(BasePlayer _player)
        {
            float multiplier = 1f;
            foreach (var m in XPMultiplier)
                if (hasPerm(_player, m.Key) && Convert.ToSingle(m.Value) > multiplier)
                    multiplier = Convert.ToSingle(m.Value);
            return multiplier;
        }

        private object GetPlayerLevel(ulong _ID)
        {
            var agent = BasePlayer.FindXpAgent(_ID);
            if (agent != null)
            {
                return Math.Floor(agent.CurrentLevel);
            }
            return null;
        }

        object OnXpEarn(ulong _userID, float _xpValue, string source)
        {
            if (_userID == 0) return _xpValue;
            if (isNPC(_userID)) return _xpValue;
            if (isGod(_userID)) return null;
            if (isplayerNA(_userID)) return 0f;
            BasePlayer _player = basePlayerByID(_userID);
            if (_player == null) return _xpValue;
            var agent = GetPlayerLevel(_userID);
            if (agent != null)
            {
                int _lvl = Convert.ToInt16(agent);
                if (((isPvP(_userID)) && (_lvl < PvPCap)) || ((isPvE(_userID)) && (_lvl < PvECap)))
                {
                    return _xpValue * XPMultPerm(_player);
                }
                else
                {
                    return 0f;
                }
            }
            else
            {
                return 0f;
            }
        }
        void OnXpLevelUp(ulong _userID, int _level)
        {
            if (IgnoreXPFunction.Contains(_userID)) return;
            BasePlayer _player = basePlayerByID(_userID);
            if (isplayerNA(_player)) { LangMSG(_player, "NoSaveLvLNA"); return; }
            int _currentLevel = Convert.ToInt32(_player.xp.CurrentLevel);
            if ((_level <= PvECap) && (InfoCache[_userID].pveLevel < _level))
            {
                InfoCache[_userID].pveLevel = _currentLevel;
            }
            if ((_level <= PvPCap) && (InfoCache[_userID].pvpLevel < _level))
            {
                InfoCache[_userID].pvpLevel = _currentLevel;
            }
            saveCacheData();
        }
        void OnXpSpent(ulong _userID, int _amount, string item)
        {
            if (IgnoreXPFunction.Contains(_userID)) return;
            InfoCache[_userID].xpUnSpent = InfoCache[_userID].xpUnSpent - _amount;
            InfoCache[_userID].xpSpent = InfoCache[_userID].xpSpent + _amount;
            saveCacheData();
        }
        void OnXpEarned(ulong _userID, float _amount, string source)
        {
            if (IgnoreXPFunction.Contains(_userID)) return;
            //if (_userID== 76561198006265515)
            //{
            //    source=="cheat"
            //}
            if (_amount == 0f) return;
            InfoCache[_userID].xpUnSpent = InfoCache[_userID].xpUnSpent + _amount;
            saveCacheData();
        }

        void setPlayerLevel(ulong _userID, int _level)
        {//need to change, issue is xp added when dropping levels
            BasePlayer _player = basePlayerByID(_userID);
            float Unspentxp = InfoCache[_userID].xpUnSpent;
            disablePvXLogger(_userID);
            _player.xp.Reset();
            _player.xp.Add(Rust.Xp.Definitions.Cheat, Rust.Xp.Config.LevelToXp(_level));
            if (_player.xp.UnspentXp > Unspentxp)
            {
                _player.xp.SpendXp((Convert.ToInt32(_player.xp.UnspentXp - Unspentxp)), string.Empty);
            }
            else if (_player.xp.UnspentXp < Unspentxp)
            {
                float addXp = Unspentxp - _player.xp.UnspentXp;
                _player.xp.Add(Rust.Xp.Definitions.Cheat, addXp);
            }
            enablePvXLogger(_userID);
            saveCacheData();
        }
        void storePlayerLevel(BasePlayer _player)
        {
            int _currentLevel = Convert.ToInt32(_player.xp.CurrentLevel);
            if ((_currentLevel <= PvECap) && (InfoCache[_player.userID].pveLevel < _currentLevel))
            {
                InfoCache[_player.userID].pveLevel = _currentLevel;
            }
            else InfoCache[_player.userID].pveLevel = InfoCache[_player.userID].pveLevel;
            if ((_currentLevel <= PvPCap) && (InfoCache[_player.userID].pvpLevel < _currentLevel))
            {
                InfoCache[_player.userID].pvpLevel = _currentLevel;
            }
            else InfoCache[_player.userID].pvpLevel = InfoCache[_player.userID].pvpLevel;
        }
        void PvELevelHandle(BasePlayer _player)
        {
            if (_player.xp.CurrentLevel > PvECap)
            {
                setPlayerLevel(_player.userID, PvECap);
                LangMSG(_player, "lvlRedxpSav");
            }
            else if (InfoCache[_player.userID].pveLevel > _player.xp.CurrentLevel)
            {
                setPlayerLevel(_player.userID, Convert.ToInt16(InfoCache[_player.userID].pveLevel));
                LangMSG(_player, "lvlIncrxpRes");
            }
        }//Completed
        void PvPLevelHandle(BasePlayer _player, int _unspentxp)
        {
            if (_player.xp.CurrentLevel > PvPCap)
            {
                setPlayerLevel(_player.userID, PvPCap);
                LangMSG(_player, "lvlRedxpSav");
            }
            else if (InfoCache[_player.userID].pvpLevel > _player.xp.CurrentLevel)
            {
                setPlayerLevel(_player.userID, InfoCache[_player.userID].pvpLevel);
                LangMSG(_player, "lvlIncrxpRes");
            }
        }//completed
        #endregion

        #region Looting Functions
        ItemContainer.CanAcceptResult CanAcceptItem(ItemContainer container, Item item)
        {
            if (container.playerOwner != null)
            {
                BasePlayer _player = container.playerOwner;
                List<Item.OwnerFraction> _itemOwners = item.owners;
                if (_itemOwners == null) return ItemContainer.CanAcceptResult.CanAccept;
                if (_itemOwners.Count < 1) return ItemContainer.CanAcceptResult.CanAccept;
                ulong _ownerID = _itemOwners[0].userid;
                if (_ownerID == 0) return ItemContainer.CanAcceptResult.CanAccept;
                if (isNPC(_ownerID)) return ItemContainer.CanAcceptResult.CanAccept;
                if (SameOnlyCheck(container.playerOwner.userID, _ownerID)) return ItemContainer.CanAcceptResult.CanAccept;
                else
                {
                    LangMSG(container.playerOwner, "notAllwPickup", InfoCache[_ownerID].mode);
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            else return ItemContainer.CanAcceptResult.CanAccept;
        }
        private object CanLootPlayer(BasePlayer _target, BasePlayer _looter)
        {
            if (isNPC(_target)) return canLootNPC(_looter);
            if (isGod(_target)) return null;
            if (areInEvent(_looter, _target)) return null;
            return PvPOnlyCheck(_looter, _target) ? null : (object)false;
        }
        private void OnLootPlayer(BasePlayer _looter, BasePlayer _target)
        {
            if (isNPC(_target)) { npcLootHandle(_looter); return; };
            if (areInEvent(_looter, _target)) return;
            if (PvPOnlyCheck(_looter, _target)) return;
            else NextTick(_looter.EndLooting);
        }
        private void OnLootEntity(BasePlayer _looter, BaseEntity _target)
        {
            if (_target is BaseCorpse)
            {
                var corpse = _target?.GetComponent<PlayerCorpse>() ?? null;
                if (corpse != null)
                {
                    if (isNPC(corpse)) { npcLootHandle(_looter); return; }
                    ulong _corpseID = corpse.playerSteamID;
                    if (_corpseID == _looter.userID) return;
                    BasePlayer _corpseBP = basePlayerByID(_corpseID);
                    if (_corpseBP != null)
                        if (areInEvent(_looter, _corpseBP)) return;
                        else if (PvPOnlyCheck(_looter.userID, _corpseID)) return;
                        else NextTick(_looter.EndLooting);
                }
            }
            else if (_target is StorageContainer)
            {
                StorageContainer _container = (StorageContainer)_target;
                if (_container.OwnerID == 0) return;
                BasePlayer _containerBP = basePlayerByID(_container.OwnerID);
                if (_container.OwnerID == _looter.userID) return;
                if (_containerBP != null)
                    if (areInEvent(_looter, _containerBP)) return;
                if (SameOnlyCheck(_looter.userID, _container.OwnerID)) return;
                else NextTick(_looter.EndLooting);
            }
            else return;
        }
        #endregion

        #region Building Functions
        private List<object> BuildEntityList = new List<object>() {
            typeof(AutoTurret),typeof(Barricade),typeof(BaseCombatEntity),
            typeof(BaseOven),typeof(BearTrap),typeof(BuildingBlock),
            typeof(BuildingPrivlidge),typeof(CeilingLight),typeof(Door),
            typeof(Landmine),typeof(LiquidContainer),typeof(ReactiveTarget),
            typeof(RepairBench),typeof(ResearchTable),typeof(Signage),
            typeof(SimpleBuildingBlock),typeof(SleepingBag),typeof(StabilityEntity),
            typeof(StorageContainer),typeof(SurvivalFishTrap),typeof(WaterCatcher),
            typeof(WaterPurifier)};
        private List<object> BaseEntityList = new List<object>() {
            typeof(BaseOven),typeof(BuildingBlock),typeof(BuildingPrivlidge),
            typeof(CeilingLight),typeof(Door),typeof(LiquidContainer),
            typeof(RepairBench),typeof(ResearchTable),typeof(Signage),
            typeof(SimpleBuildingBlock),typeof(SleepingBag),typeof(StabilityEntity),
            typeof(StorageContainer),typeof(SurvivalFishTrap),typeof(WaterCatcher),
            typeof(WaterPurifier)};
        private List<object> CombatEntityList = new List<object>() {
            typeof(AutoTurret),typeof(Barricade),typeof(BearTrap),typeof(Landmine),
            typeof(ReactiveTarget),typeof(BaseCombatEntity)};



        void OnEntitySpawned(BaseNetworkable _entity)
        {
            if (_entity is BaseEntity)
            {
                BaseEntity _base = (BaseEntity)_entity;
                if (_base.OwnerID == 0) return;
                else if (AdminPlayerMode.Contains(basePlayerByID(_base.OwnerID)))
                    _base.OwnerID = 0;
            }
        }

        #endregion

        #region Compatibility Functions

        [PluginReference]
        private Plugin BetterChat;
        void RegisterGroups()
        {
            if (!BetterChat) return;
            if (!GroupExists("PvP"))
            {
                NewGroup("PvP");
                SetGroupTitle("PvP");
                SetGroupColor("PvP");
            }
            if (!GroupExists("PvE"))
            {
                NewGroup("PvE");
                SetGroupTitle("PvE");
                SetGroupColor("PvE");
            }
        }

        void updatePlayerChatTag(BasePlayer _player)
        {
            if (!BetterChat) return;
            ulong _userID = _player.userID;
            string _userIDs = _player.UserIDString;
            string _mode = InfoCache[_userID].mode;
            if (_mode == "pvp")
            {
                if (!(UserInGroup(_userIDs, "PvP"))) AddToGroup(_userIDs, "PvP");
                if (UserInGroup(_userIDs, "PvE")) RemoveFromGroup(_userIDs, "PvE");
            }
            else if (_mode == "pve")
            {
                if (!(UserInGroup(_userIDs, "PvE"))) AddToGroup(_userIDs, "PvE");
                if (UserInGroup(_userIDs, "PvP")) RemoveFromGroup(_userIDs, "PvP");
            }
            else if (_mode == "NA")
            {
                if (UserInGroup(_userIDs, "PvE")) RemoveFromGroup(_userIDs, "PvE");
                if (UserInGroup(_userIDs, "PvP")) RemoveFromGroup(_userIDs, "PvP");
            }
        }

        private bool GroupExists(string name) => (bool)BetterChat?.Call("API_GroupExists", (name.ToLower()));
        private bool NewGroup(string name) => (bool)BetterChat?.Call("API_AddGroup", (name.ToLower()));
        private bool UserInGroup(string ID, string name) => (bool)BetterChat?.Call("API_IsUserInGroup", ID, (name.ToLower()));
        private bool AddToGroup(string ID, string name) => (bool)BetterChat?.Call("API_AddUserToGroup", ID, (name.ToLower()));
        private bool RemoveFromGroup(string ID, string name) => (bool)BetterChat?.Call("API_RemoveUserFromGroup", ID, (name.ToLower()));
        private object SetGroupTitle(string name) => BetterChat?.Call("API_SetGroupSetting", (name.ToLower()), "title", $"[{name}]");
        private object SetGroupColor(string name) => BetterChat?.Call("API_SetGroupSetting", (name.ToLower()), "titlecolor", "orange");

        [PluginReference]
        private Plugin HumanNPC;
        bool isNPC(ulong _test)
        {
            if (HumanNPC == null) return false;
            else if (_test < 76560000000000000L) return true;
            else return false;
        }
        bool isNPC(BasePlayer _test)
        {
            if (HumanNPC == null) return false;
            else if (_test.userID < 76560000000000000L) return true;
            else return false;
        }
        bool isNPC(PlayerCorpse _test)
        {
            if (HumanNPC == null) return false;
            else if (_test.playerSteamID < 76560000000000000L) return true;
            else return false;
        }

        void npcDamageHandle(BasePlayer _NPC, HitInfo _hitInfo)
        {
            BasePlayer _attacker = (BasePlayer)_hitInfo.Initiator;
            if (isNPC(_attacker)) return;
            if ((InfoCache[_attacker.userID].mode == "pvp") && (PvPDamageNPC == true)) return;
            if ((InfoCache[_attacker.userID].mode == "pve") && (PvEDamageNPC == true)) return;
            else NullifyDamage(_hitInfo);
        }
        void npcAttackHandle(BasePlayer _target, HitInfo _hitInfo)
        {
            if (isNPC(_target)) return;
            if ((InfoCache[_target.userID].mode == "pvp") && (NPCDamagePvP == true)) return;
            if ((InfoCache[_target.userID].mode == "pve") && (NPCDamagePvE == true)) return;
            else NullifyDamage(_hitInfo);
        }

        bool canLootNPC(BasePlayer _player)
        {
            if ((PvELootNPC == true) && (PvPLootNPC == true)) return true;
            else if ((InfoCache[_player.userID].mode == "pvp") && (PvPLootNPC == true)) return true;
            else if ((InfoCache[_player.userID].mode == "pve") && (PvELootNPC == true)) return true;
            else return false;
        }
        void npcLootHandle(BasePlayer _player)
        {
            if ((PvELootNPC == true) && (PvPLootNPC == true)) return;
            if ((InfoCache[_player.userID].mode == "pvp") && (PvPLootNPC == true)) return;
            if ((InfoCache[_player.userID].mode == "pve") && (PvELootNPC == true)) return;
            BroadcastMessageHandle("Not allowed to loot");
            NextTick(_player.EndLooting);
        }

        [PluginReference]
        private Plugin EventManager;
        bool isInEvent(BasePlayer _player1)
        {
            if (EventManager == null) return false;
            bool _var = (bool)EventManager?.Call("isPlaying", _player1);
            if (_var == true) return true;
            return false;
        }

        bool areInEvent(BasePlayer _player1, BasePlayer _player2)
        {
            if (EventManager == null) return false;
            bool _var1 = (bool)EventManager?.Call("isPlaying", _player1);
            bool _var2 = (bool)EventManager?.Call("isPlaying", _player2);
            if (_var1 == true && _var1 == _var2) return true;
            return false;
        }


        [PluginReference]
        private Plugin Godmode;
        private bool checkIsGod(string _player) => (bool)Godmode?.Call("IsGod", _player);

        private bool isGod(ulong _player)
        {
            if (Godmode == null) return false;
            return checkIsGod(_player.ToString());
        }
        private bool isGod(BasePlayer _player)
        {
            if (Godmode == null) return false;
            if (_player == null) return false;
            return checkIsGod(_player.UserIDString);
        }


        #endregion

        #region Door Functions
        void OnDoorOpened(Door _door, BasePlayer _player)
        {
            if (_door == null) return;
            if (_door.OwnerID == 0) return;
            if (!(SameOnlyCheck(_player.userID, _door.OwnerID)))
            {
                _door.SetFlag(BaseEntity.Flags.Open, false);
                _door.SendNetworkUpdateImmediate();
            }
        }
        #endregion

        #region PvX Check/Find Functions
        private bool PvPOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (InfoCache[_player1.userID].mode == "NA") return false;
            if (InfoCache[_player2.userID].mode == "NA") return false;
            if ((InfoCache[_player1.userID].mode == "pvp") && (InfoCache[_player2.userID].mode == "pvp"))
                return true;
            return false;
        }
        private bool PvPOnlyCheck(ulong _player1, ulong _player2)
        {
            if (!InfoCache.ContainsKey(_player1)) return false;
            if (!InfoCache.ContainsKey(_player2)) return false;
            if (InfoCache[_player1].mode == "NA") return false;
            if (InfoCache[_player2].mode == "NA") return false;
            if ((InfoCache[_player1].mode == "pvp") && (InfoCache[_player2].mode == "pvp")) return true;
            return false;
        }
        private bool PvEOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (InfoCache[_player1.userID].mode == "NA") return false;
            if (InfoCache[_player2.userID].mode == "NA") return false;
            if ((InfoCache[_player1.userID].mode == "pve") && (InfoCache[_player2.userID].mode == "pve"))
                return true;
            return false;
        }
        private bool PvEOnlyCheck(ulong _player1, ulong _player2)
        {
            if (!InfoCache.ContainsKey(_player1)) return false;
            if (!InfoCache.ContainsKey(_player2)) return false;
            if (InfoCache[_player1].mode == "NA") return false;
            if (InfoCache[_player2].mode == "NA") return false;
            if ((InfoCache[_player1].mode == "pve") && (InfoCache[_player2].mode == "pve")) return true;
            return false;
        }
        private bool SameOnlyCheck(BasePlayer _player1, BasePlayer _player2)
        {
            if (InfoCache[_player1.userID].mode == "NA") return false;
            if (InfoCache[_player2.userID].mode == "NA") return false;
            if (InfoCache[_player1.userID].mode == InfoCache[_player2.userID].mode) return true;
            return false;
        }
        private bool SameOnlyCheck(ulong _player1, ulong _player2)
        {
            if (!InfoCache.ContainsKey(_player1)) return false;
            if (!InfoCache.ContainsKey(_player2)) return false;
            if (InfoCache[_player1].mode == "NA") return false;
            if (InfoCache[_player2].mode == "NA") return false;
            if (InfoCache[_player1].mode == InfoCache[_player2].mode) return true;
            return false;
        }

        bool isplayerNA(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (!InfoCache.ContainsKey(_playerID))
            {
                if (_player == null) return true;
                addPlayer(_player);
                return true;
            }
            if (InfoCache[_playerID].mode == "NA") return true;
            else return false;
        }
        bool isplayerNA(ulong _playerID)
        {
            if (!InfoCache.ContainsKey(_playerID))
            {
                BasePlayer _player = basePlayerByID(_playerID);
                if (_player == null) return true;
                addPlayer(_player);
                return true;
            }
            if (InfoCache[_playerID].mode == "NA") return true;
            else return false;
        }
        bool isPvP(ulong _playerID)
        {
            if (_playerID == 0 || isNPC(_playerID)) return false;
            BasePlayer _player = basePlayerByID(_playerID);
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pvp") return true;
            else return false;
        }
        bool isPvP(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pvp") return true;
            else return false;
        }
        bool isPvE(ulong _playerID)
        {
            if (_playerID == 0 || isNPC(_playerID)) return false;
            BasePlayer _player = basePlayerByID(_playerID);
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }
        bool isPvE(BasePlayer _player)
        {
            ulong _playerID = _player.userID;
            if (_playerID == 0 || isNPC(_playerID)) return false;
            if (_player == null) return false;
            if (!InfoCache.ContainsKey(_playerID))
            {
                addPlayer(_player);
                return false;
            }
            if (InfoCache[_playerID].mode == "pve") return true;
            else return false;
        }

        void checkPlayersRegistered()
        {
            foreach (BasePlayer _player in BasePlayer.activePlayerList)
                if (!(InfoCache.ContainsKey(_player.userID)))
                    addPlayer(_player);

            foreach (BasePlayer _player in BasePlayer.sleepingPlayerList)
                if (!(InfoCache.ContainsKey(_player.userID)))
                    addSleeper(_player);
        }
        void addPlayer(BasePlayer _player)
        {
            if (isNPC(_player.userID)) return;
            InfoCache.Add(_player.userID, new PlayerInfo
            {
                username = _player.displayName,
                mode = "NA",
                ticket = false,
                timeStamp = GetTimeStamp(),
                pvpLevel = 0,
                pveLevel = 0,
                xpSpent = _player.xp.SpentXp,
                xpUnSpent = _player.xp.UnspentXp
            });
            SelectorOverlay(_player);
            storePlayerLevel(_player);
            saveCacheData();
        }
        void addSleeper(BasePlayer _player)
        {
            if (isNPC(_player.userID)) return;
            InfoCache.Add(_player.userID, new PlayerInfo
            {
                username = _player.displayName,
                mode = "NA",
                ticket = false,
                timeStamp = GetTimeStamp(),
                pvpLevel = 0,
                pveLevel = 0,
                xpSpent = 0,
                xpUnSpent = 0
            });
            SleeperCache.Add(_player.userID);
            saveCacheData();
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsDigit(c))
                {
                    Puts("Character Detected Returning false");
                    return false;
                }
            }
            Puts("Detected no Characters Returning true");
            return true;
        }
        BasePlayer basePlayerByID(ulong _ID)
        {
            BasePlayer _player = BasePlayer.FindByID(_ID);
            if (_player == null) _player = BasePlayer.FindSleeping(_ID);
            return _player;
        }
        #endregion

        #region Hooks
        public void pvxUpdateXPDataFile(BasePlayer _player)
        {
            InfoCache[_player.userID].xpUnSpent = _player.xp.UnspentXp;
            InfoCache[_player.userID].xpSpent = _player.xp.SpentXp;
            storePlayerLevel(_player);
        }
        public void disablePvXLogger(ulong _userID)
        {
            IgnoreXPFunction.Add(_userID);
        }
        public void enablePvXLogger(ulong _userID)
        {
            IgnoreXPFunction.Remove(_userID);
        }
        public void enablePvXLoggerAndResetUserData(ulong _userID)
        {
            IgnoreXPFunction.Remove(_userID);
            BasePlayer _player = basePlayerByID(_userID);
            InfoCache[_userID].xpUnSpent = _player.xp.UnspentXp;
            InfoCache[_userID].xpSpent = _player.xp.SpentXp;
            storePlayerLevel(_player);
        }

        #endregion

        #region Chat/Console Handles
        [ChatCommand("pvx")]
        void PvXCmd(BasePlayer _player, string cmd, string[] args)
        {
            if ((args == null) || (args.Length == 0))
            {
                LangMSG(_player, "ComndList");
                ChatMessageHandle(_player, "/pvx select, /pvx change, /pvx ticket /pvx gui");
                if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx select, /pvx admin");
            }
            switch (args[0].ToLower())
            {
                case "admin": //meed to transfer accept/dec;ome/list function
                    adminFunction(_player, args);
                    return;
                case "change": //Completed
                    changeFunction(_player);
                    return;
                case "debug":
                    debugFunction();
                    return;
                case "developer":
                    developerFunction();
                    return;
                case "help":
                    helpFunction(_player);
                    return;
                case "select":
                    selectFunction(_player, args);
                    return;
                case "ticket":
                    ticketFunction(_player, args);
                    return;
                case "gui":
                    guiFunction(_player, args);
                    return;
                default:
                    LangMSG(_player, "ComndList");
                    ChatMessageHandle(_player, "/pvx select, /pvx change, /pvx ticket /pvx gui");
                    if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx select, /pvx admin");
                    return;
            }
        }

        [ConsoleCommand("PvXSelection")]
        void PvXSelection(ConsoleSystem.Arg arg)
        {
            if ((arg.Args.Length == 0) || (arg.Args.Length == 2)) return;
            BasePlayer _player = (BasePlayer)arg.connection.player;
            string cmdValue = arg.Args[0];
            if (playerData.Info[_player.userID].mode == "NA")
            {
                InfoCache[_player.userID].mode = cmdValue;
                saveCacheData();
                updatePvXIndicator(_player);
                ChatMessageHandle(_player, "Selected: {0}", cmdValue);
            }
            else if (playerData.Info[_player.userID].mode != cmdValue.ToLower())
            {
                createTicket(_player, cmdValue.ToLower());
                SaveAll();
                updatePvXIndicator(_player);
                ChatMessageHandle(_player, "Ticket Created");
            }
        }

        [ChatCommand("pvxhide")]
        void test1(BasePlayer _player, string cmd, string[] args)
        {
            DestroyAllUI(_player);
        }
        [ChatCommand("pvxshow")]
        void test(BasePlayer _player, string cmd, string[] args)
        {
            createPvXIndicator(_player);
            createAdminIndicator(_player);
        }
        #endregion

        #region Chat Functions
        void adminFunction(BasePlayer _player, string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx admin [list/accept/decline/display]");
                return;
            }
            string _cmd = args[1].ToLower(); // admin, accept, 1
            if (!(hasPerm(_player, "admin", "MissPerm"))) return;
            if (_cmd == "count") ticketCount(_player);
            if (_cmd == "list") listTickets(_player);
            if (_cmd == "mode") adminMode(_player);
            if ((_cmd == "display") && (args.Length == 3))
            {
                if (IsDigitsOnly(args[2]))
                    displayTicket(_player, Convert.ToInt32(args[2]));
            }
            if ((_cmd == "accept") && (args.Length == 3))
            {
                if ((IsDigitsOnly(args[2])) && (ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    ticketAccept(_player, Convert.ToInt32(args[2]));
                else if (!(ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    LangMSG(_player, "TickNotAvail", args[2]);
                else
                {
                    LangMSG(_player, "IncoFormPleaUse");
                    ChatMessageHandle(_player, "/pvx admin accept #");
                }
            }
            if ((_cmd == "decline") && (args.Length == 3))
            {
                if ((IsDigitsOnly(args[2])) && (ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    ticketDecline(_player, Convert.ToInt32(args[2]));
                else if (!(ticketData.Link.ContainsKey(Convert.ToInt32(args[2]))))
                    LangMSG(_player, "TickNotAvail", args[2]);
                else
                {
                    LangMSG(_player, "IncoFormPleaUse");
                    ChatMessageHandle(_player, "/pvx admin decline #");
                }
            }
        }

        void changeFunction(BasePlayer _player)
        {
            storePlayerLevel(_player);
            if (playerHasTicket(_player) == true)
            {
                ChatMessageHandle(_player, "AlreadySubmitted"); return;
            }
            else if (isplayerNA(_player))
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx select [pvp/pve]");
                return;
            }
            else if (isPvP(_player)) createTicket(_player, "pve");
            else if (isPvE(_player)) createTicket(_player, "pvp");
            else PutsPlayerHandle(_player, "Error: 27Q1 - Please inform Dev");
            return;
        }

        void selectFunction(BasePlayer _player, string[] args)
        {
            if ((args.Length != 2) && (args[1] != "pve") && (args[1] != "pvp"))
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx select [pvp/pve]");
            }
            else if (InfoCache[_player.userID].mode == "NA")
            {
                InfoCache[_player.userID].mode = args[1].ToLower();
                saveCacheData();
                updatePvXIndicator(_player);
            }
        }

        void ticketFunction(BasePlayer _player, string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                LangMSG(_player, "IncoFormPleaUse");
                ChatMessageHandle(_player, "/pvx ticket cancel");
                ChatMessageHandle(_player, "/pvx ticket reason ''reason on ticket''");
                return;
            }
            string _cmd = args[1].ToLower();
            if (InfoCache[_player.userID].ticket == false)
            {
                LangMSG(_player, "NoActTick");
                return;
            }
            if (_cmd == "cancel")
            {
                cancelTicket(_player);
            }
            if ((_cmd == "reason") && (args.Length == 3))
            {
                ticketData.Info[_player.userID].reason = args[2];
                LangMSG(_player, "RSNChan");
                ChatMessageHandle(_player, args[2]);
                SaveAll();
                return;
            }
        }

        void guiFunction(BasePlayer _player, string[] args)
        {
            if (args.Length == 1)
            {
                LangMSG(_player, "ComndList");
                ChatMessageHandle(_player, "/pvx gui pvx on/off");
                if (hasPerm(_player, "admin")) ChatMessageHandle(_player, "/pvx gui admin on/off");
                return;
            }
            if (!(args.Length == 3)) return;
            if ((args[1].ToLower() == "admin") && (hasPerm(_player, "admin")))
            {
                if (args[2].ToLower() == "on") createAdminIndicator(_player);
                else if (args[2].ToLower() == "off") DestroyUI(_player, pvxAdminUI);
                return;
            }
            else if (args[1].ToLower() == "pvx")
            {
                if (args[2].ToLower() == "on") createPvXIndicator(_player);
                else if (args[2].ToLower() == "off") DestroyUI(_player, pvxPlayerUI);
                return;
            }
            return;
        }

        void debugFunction()
        { }
        void developerFunction()
        { }
        void helpFunction(BasePlayer _player)
        {
            ChatMessageHandle(_player, "Plugin: PvX");
            ChatMessageHandle(_player, "Version {0}", Version);
            ChatMessageHandle(_player, "Mod Developer: Alphawar");
        }
        #endregion

        //void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        //{
        //    if(trigger is BuildPrivilegeTrigger)
        //        trigger.
        //    Puts("OnEntityEnter works!");
        //}
        //void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        //{
        //    Puts("OnEntityLeave works!");
        //}

        void adminMode(BasePlayer _player)
        {
            if (AdminPlayerMode.Contains(_player))
            {
                LangMSG(_player, "AdmModeRem");
                AdminPlayerMode.Remove(_player);
                updatePvXIndicator(_player);
                return;
            }
            else AdminPlayerMode.Add(_player);
            LangMSG(_player, "AdmModeAdd");
            updatePvXIndicator(_player);
        }
        bool isInAdminMode(BasePlayer _player)
        {
            if (AdminPlayerMode.Contains(_player)) return true;
            return false;
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            //Puts("Container is type {0}", container.GetType());
            //Puts("Container is type {0}", container.entityOwner);
            //Puts("Container is type {0}", container.playerOwner);
            if (container.entityOwner != null) return;
            if (container.playerOwner != null)
            {
                BasePlayer _player = container.playerOwner;
                if (isInAdminMode(_player)) item.ClearOwners();
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            object _n = entity.GetType();
            if (entity is BasePlayer && hitinfo.Initiator is BasePlayer)
            {
                BasePlayer _victim = (BasePlayer)entity;
                BasePlayer _attacker = (BasePlayer)hitinfo.Initiator;
                if (isNPC(_victim)) { npcDamageHandle(_victim, hitinfo); return; }
                if (isNPC(_attacker)) { npcAttackHandle(_victim, hitinfo); return; }
                if (_attacker == _victim) return;
                if (isGod(_victim)) return;
                if (isGod(_attacker)) return;
                if (areInEvent(_attacker, _victim)) return;
                if (PvPOnlyCheck(_victim.userID, _attacker.userID)) return;
                if (InfoCache[_victim.userID].mode == "pve")
                {
                    if (!antiChatSpam.Contains(_attacker.userID))
                    {
                        antiChatSpam.Add(_attacker.userID);
                        timer.Once(2f, () => antiChatSpam.Remove(_attacker.userID));
                        LangMSG(_attacker, lang.GetMessage("PvETarget", this, _attacker.UserIDString));
                    }
                    _victim.EndLooting();
                }
                NullifyDamage(hitinfo);
            }
            else if ((BaseEntityList.Contains(_n)) && (hitinfo.Initiator is BasePlayer))
            {
                BasePlayer _attacker = (BasePlayer)hitinfo.Initiator;
                BaseEntity _target = entity;
                if (entity.OwnerID == 0) return;
                if (isNPC(_attacker)) { NullifyDamage(hitinfo); return; }
                if (isInEvent(_attacker)) return;
                if (_target.OwnerID == _attacker.userID) return;
                if (isGod(_target.OwnerID))
                {
                    NullifyDamage(hitinfo);
                    LangMSG(_attacker, "TargisGod");
                    return;
                }
                if (isGod(_attacker))
                {
                    NullifyDamage(hitinfo);
                    LangMSG(_attacker, "YouisGod");
                    return;
                }
                if (PvPOnlyCheck(_target.OwnerID, _attacker.userID)) return;
                if (!antiChatSpam.Contains(_attacker.userID))
                {
                    antiChatSpam.Add(_attacker.userID);
                    timer.Once(2f, () => antiChatSpam.Remove(_attacker.userID));
                    LangMSG(_attacker, lang.GetMessage("PvETarget", this, _attacker.UserIDString));
                }
                NullifyDamage(hitinfo);
            }
            else if ((CombatEntityList.Contains(_n)) && (hitinfo.Initiator is BasePlayer))
            {
                BasePlayer _attacker = (BasePlayer)hitinfo.Initiator;
                BaseEntity _target = entity;
                if (entity.OwnerID == 0) return;
                if (isNPC(_attacker)) { NullifyDamage(hitinfo); return; }
                if (isInEvent(_attacker)) return;
                if (_target.OwnerID == _attacker.userID) return;
                if (isGod(_target.OwnerID)) return;
                if (isGod(_attacker)) return;
                if (PvPOnlyCheck(_target.OwnerID, _attacker.userID)) return;
                if (!antiChatSpam.Contains(_attacker.userID))
                {
                    antiChatSpam.Add(_attacker.userID);
                    timer.Once(2f, () => antiChatSpam.Remove(_attacker.userID));
                    LangMSG(_attacker, lang.GetMessage("PvETarget", this, _attacker.UserIDString));
                }
                NullifyDamage(hitinfo);
            }
            else return;
        }

        void PvXFunction(BasePlayer _player, string[] args)
        {

        }
        static void NullifyDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
            hitinfo.HitMaterial = 0;
            hitinfo.PointStart = Vector3.zero;
        }
        void ModifyDamage(HitInfo hitinfo)
        { }

        string DateTimeStamp()
        {
            return DateTime.Now.ToString("HH:mm dd-MM-yyyy");
        }
        double GetTimeStamp()
        {
            return (DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }


        //////////////////////////////////////////////////////////////////////////////////////
        // Debug /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        int DebugLevel = 0;
        void DebugMessage(int _minDebuglvl, string _msg)
        {
            if (DebugLevel >= _minDebuglvl)
            {
                Puts(_msg);
                if (DebugLevel == 3 && _minDebuglvl == 1)
                {
                    PrintToChat(_msg);
                }
            }
        }
    }
}

// config color + opacity
