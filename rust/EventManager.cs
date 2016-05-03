using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("Event Manager", "Reneb / k1lly0u", "2.0.1", ResourceId = 740)]
    class EventManager : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Spawns;

        [PluginReference]
        Plugin Kits;

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        Plugin ServerRewards;

        [PluginReference]
        Plugin Economics;

        private string EventSpawnFile;
        private string EventGameName;
        private string ZoneName;
        private string TokenType;

        private bool EventOpen;
        private bool EventStarted;
        private bool EventEnded;
        private bool EventPending;        
        private int EventMaxPlayers = 0;
        private int EventMinPlayers = 0;
        private int EventAutoNum = -1;

        public int PlayTimer;

        public float LastAnnounce;
        public bool AutoEventLaunched = false;
        public bool UseClassSelection;
        public GameMode EventMode;

        private List<string> EventGames;
        private List<EventPlayer> EventPlayers;
        public List<ulong> Godmode;
        public List<Timer> AutoArenaTimers;

        private ConfigData configData;

        ClassData classData;
        private DynamicConfigFile Class_Data;
        #endregion

        #region Classes        
        class EventPlayer : MonoBehaviour
        {
            public BasePlayer player;

            public float health;
            public float calories;
            public float hydration;

            public bool inEvent;
            public bool savedInventory;
            public bool savedHome;

            public string currentClass;

            public List<EventInvItem> InvItems = new List<EventInvItem>();
            public Vector3 Home;

            void Awake()
            {
                inEvent = true;
                savedInventory = false;
                savedHome = false;
                player = GetComponent<BasePlayer>();
            }
            public void SaveHealth()
            {
                health = player.health;
                calories = player.metabolism.calories.value;
                hydration = player.metabolism.hydration.value;
            }
            public void SaveHome()
            {
                if (!savedHome)
                    Home = player.transform.position;
                savedHome = true;
            }
            public void TeleportHome()
            {
                if (!savedHome)
                    return;
                TPPlayer(player, Home);
                savedHome = false;
            }
            public void SaveInventory()
            {
                if (savedInventory)
                    return;
                InvItems.Clear();
                InvItems.AddRange(GetItems(player.inventory.containerWear, "wear"));
                InvItems.AddRange(GetItems(player.inventory.containerMain, "main"));
                InvItems.AddRange(GetItems(player.inventory.containerBelt, "belt"));
                savedInventory = true;
            }
            private IEnumerable<EventInvItem> GetItems(ItemContainer container, string containerName)
            {
                return container.itemList.Select(item => new EventInvItem
                {
                    itemid = item.info.itemid,
                    bp = item.IsBlueprint(),
                    container = containerName,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    skin = item.skin,
                    condition = item.condition,
                    contents = item.contents?.itemList.Select(item1 => new EventInvItem
                    {
                        itemid = item1.info.itemid,
                        amount = item1.amount,
                        condition = item1.condition
                    }).ToArray()
                });
            }
            public void RestoreInventory()
            {
                foreach (var kitem in InvItems)
                {
                    var item = ItemManager.CreateByItemID(kitem.itemid, kitem.amount, kitem.bp, kitem.skin);
                    item.condition = kitem.condition;
                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null) weapon.primaryMagazine.contents = kitem.ammo;
                    player.inventory.GiveItem(item, kitem.container == "belt" ? player.inventory.containerBelt : kitem.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
                    if (kitem.contents == null) continue;
                    foreach (var ckitem in kitem.contents)
                    {
                        var item1 = ItemManager.CreateByItemID(ckitem.itemid, ckitem.amount);
                        if (item1 == null) continue;
                        item1.condition = ckitem.condition;
                        item1.MoveToContainer(item.contents);
                    }
                }
                savedInventory = false;
            }
        }
        class EventInvItem
        {
            public int itemid;
            public bool bp;
            public int skin;
            public string container;
            public int amount;
            public float condition;
            public int ammo;
            public EventInvItem[] contents;
        }
        class ConfigData
        {
            public string Default_Gamemode { get; set; }
            public string Default_Spawnfile { get; set; }
            public int Battlefield_Timer { get; set; }
            public bool KillDeserters { get; set; }
            public int Required_AuthLevel { get; set; }
            public string Messaging_MainColor { get; set; }
            public string Messaging_MsgColor { get; set; }
            public bool Announce_Event { get; set; }
            public bool AnnounceDuring_Event { get; set; }
            public int AnnounceEvent_Interval { get; set; }
            public bool UseEconomicsAsTokens { get; set; }
            public bool UseClassSelector_Default { get; set; }
            public AutoEvents z_AutoEvents { get; set; }
        }
        class AutoEvents
        {
            public int GameInterval { get; set; }
            public bool AutoCancel { get; set; }
            public int AutoCancel_Timer { get; set; }
            public List<AutoEventSetup> z_AutoEventSetup { get; set; }
        }
        class AutoEventSetup
        {
            public bool UseClassSelector { get; set; }
            public string GameType { get; set; }
            public GameMode EventMode { get; set; }
            public string Spawnfile { get; set; }
            public string Kit { get; set; }
            public bool CloseOnStart { get; set; }
            public int TimeToJoin { get; set; }
            public int MinimumPlayers { get; set; }
            public int MaximumPlayers { get; set; }
            public int TimeLimit { get; set; }
            public string ZoneID { get; set; }

        }
        class ClassData
        {
            public Dictionary<string, string> ClassKits = new Dictionary<string, string>();
        }
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent,
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
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
        }
        public enum GameMode
        {
            Normal,
            Battlefield
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            EventGames = new List<string>();
            EventMode = GameMode.Normal;
            EventPlayers = new List<EventPlayer>();
            AutoArenaTimers = new List<Timer>();
            Class_Data = Interface.Oxide.DataFileSystem.GetFile("EventManager_Classes");

        }
        void OnServerInitialized()
        {
            lang.RegisterMessages(Messages, this);
            LoadVariables();
            LoadData();
            EventOpen = false;
            EventStarted = false;
            EventEnded = true;
            EventPending = false;
            UseClassSelection = configData.UseClassSelector_Default;
            EventGameName = configData.Default_Gamemode;
            timer.Once(0.2f, InitializeGames);
        }
        void InitializeGames()
        {
            //Interface.Oxide.CallHook("RegisterGame");
            SelectSpawnfile(configData.Default_Spawnfile);
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);
            EndEvent();
            DestroyGame();
        }
        void OnPlayerRespawned(BasePlayer player)
        {
            if (!EventStarted) return;
            if (!player.GetComponent<EventPlayer>()) return;
            if (player.GetComponent<EventPlayer>().inEvent)
            {
                if (!EventStarted) return;
                TeleportPlayerToEvent(player);
            }
            else
            {
                RedeemInventory(player);
                TeleportPlayerHome(player);
                TryErasePlayer(player);
            }
        }
        void OnPlayerAttack(BasePlayer player, HitInfo hitinfo)
        {
            if (!EventStarted) return;
            if (player.GetComponent<EventPlayer>() == null || !(player.GetComponent<EventPlayer>().inEvent))
                return;
            if (hitinfo.HitEntity != null)
                Interface.Oxide.CallHook("OnEventPlayerAttack", player, hitinfo);
            return;
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (!EventStarted) return;
            if ((entity as BasePlayer)?.GetComponent<EventPlayer>() == null) return;
            Interface.Oxide.CallHook("OnEventPlayerDeath", ((BasePlayer)entity), hitinfo);
            return;
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (!EventStarted) return;
            if (player.GetComponent<EventPlayer>() != null)
                LeaveEvent(player);
        }
        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (!EventStarted) return;
            var player = entity as BasePlayer;
            if (Godmode == null || player == null) return;
            if (Godmode.Contains(player.userID))
            {
                info.damageTypes = new DamageTypeList();
                info.HitMaterial = 0;
                info.PointStart = Vector3.zero;
            }
        }
        #endregion

        #region Checks
        bool hasEventStarted()
        {
            return EventStarted;
        }
        bool isPlaying(BasePlayer player)
        {
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            return eplayer != null && eplayer.inEvent;
        }
        object canRedeemKit(BasePlayer player)
        {
            if (!EventStarted) return null;
            TryErasePlayer(player);
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            if (eplayer == null) return null;
            return false;
        }
        object canShop(BasePlayer player)
        {
            if (!EventStarted) return null;
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            if (eplayer == null) return null;
            return GetMessage("CanShop");
        }

        object CanTeleport(BasePlayer player)
        {
            if (!EventStarted) return null;
            EventPlayer eplayer = player.GetComponent<EventPlayer>();
            if (eplayer == null) return null;
            return GetMessage("CanTP");
        }
        #endregion

        #region Config
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
            var config = new ConfigData
            {
                AnnounceDuring_Event = true,
                AnnounceEvent_Interval = 120,
                Announce_Event = true,
                Battlefield_Timer = 1200,
                Default_Gamemode = "Deathmatch",
                Default_Spawnfile = "deathmatchspawns",
                KillDeserters = true,
                Required_AuthLevel = 1,
                Messaging_MainColor = "#FF8C00",
                Messaging_MsgColor = "#939393",
                UseEconomicsAsTokens = false,
                UseClassSelector_Default = true,
                z_AutoEvents = new AutoEvents
                {
                    AutoCancel = true,
                    AutoCancel_Timer = 300,
                    GameInterval = 1200,
                    z_AutoEventSetup = CreateDefaultAutoConfig()
                }
            };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        static List<AutoEventSetup> CreateDefaultAutoConfig()
        {
            var newautoconfiglist = new List<AutoEventSetup>
            {
                new AutoEventSetup
                {
                    GameType = "Deathmatch",
                    EventMode = GameMode.Battlefield,
                    Spawnfile = "deathmatchspawns",
                    Kit = "",
                    CloseOnStart = true,
                    TimeToJoin = 60,
                    TimeLimit = 1800,
                    MinimumPlayers = 2,
                    MaximumPlayers = 20,
                    UseClassSelector = false,
                    ZoneID = null
                },
                new AutoEventSetup
                {
                    GameType = "TeamDeathmatch",
                    EventMode = GameMode.Battlefield,
                    Spawnfile = "tdm_spawns_a",
                    Kit = "tdmkit",
                    CloseOnStart = false,
                    TimeToJoin = 60,
                    TimeLimit = 0,
                    MinimumPlayers = 2,
                    MaximumPlayers = 20,
                    UseClassSelector = false,
                    ZoneID = null
                },
                new AutoEventSetup
                {
                    GameType = "GunGame",
                    EventMode = GameMode.Battlefield,
                    Spawnfile = "ggspawns",
                    Kit = "ggkit",
                    CloseOnStart = false,
                    TimeToJoin = 60,
                    TimeLimit = 0,
                    MinimumPlayers = 2,
                    MaximumPlayers = 20,
                    UseClassSelector = false,
                    ZoneID = null
                },
                new AutoEventSetup
                {
                    GameType = "ChopperSurvival",
                    EventMode = GameMode.Battlefield,
                    Spawnfile = "csspawns",
                    Kit = "cskit",
                    CloseOnStart = true,
                    TimeToJoin = 60,
                    TimeLimit = 0,
                    MinimumPlayers = 1,
                    MaximumPlayers = 20,
                    UseClassSelector = false,
                    ZoneID = null
                }
            };
            return newautoconfiglist;
        }
        #endregion

        #region Messaging
        private void MSG(BasePlayer player, string langkey, bool title = true)
        {
            string message = $"<color={configData.Messaging_MsgColor}>{GetMessage(langkey)}</color>";
            if (title) message = $"<color={configData.Messaging_MainColor}>{GetMessage("Title")}</color>" + message;
            SendReply(player, message);
        }
        void BroadcastToChat(string msg)
        {
            Debug.Log(msg);
            ConsoleSystem.Broadcast("chat.add", 0, $"<color={configData.Messaging_MainColor}>{GetMessage("Title")}</color><color={configData.Messaging_MsgColor}>{GetMessage(msg)}</color>");
        }
        private string GetMessage(string key) => lang.GetMessage(key, this);

        [HookMethod("BroadcastEvent")]
        public void BroadcastEvent(string msg)
        {
            foreach (EventPlayer eventplayer in EventPlayers)
                MSG(eventplayer.player, msg.QuoteSafe());
        }

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            { "multipleNames", "Multiple players found"},
            { "noPlayerFound", "No players found"},
            { "MessagesEventMinPlayers", "The Event {0} has reached min players and will start in {1} seconds"},
            { "MessagesEventMaxPlayers", "The Event {0} has reached max players. You may not join for the moment"},
            { "MessagesEventStatusClosedStarted", "The Event {0} has already started, it's too late to join."},
            { "Title", "Event Manager: "},
            { "MessagesEventStatusClosedEnd", "There is currently no event"},
            { "MessagesEventStatusOpenStarted", "The Event {0} has started, but is still opened: /event join"},
            { "MessagesEventStatusOpen", "The Event {0} is currently opened for entries: /event, join"},
            { "MessagesEventCloseAndEnd", "The Event needs to be closed and ended before using this command."},
            { "MessagesEventNotAnEvent", "This Game {0} isn't registered, did you reload the game after loading Event - Core?"},
            { "MessagesEventNotInEvent", "You are not currently in the Event."},
            { "MessagesEventBegin", "Event: {0} is about to begin!"},
            { "MessagesEventLeft", "{0} has left the Event! (Total Players: {1})"},
            { "MessagesEventJoined", "{0} has joined the Event!  (Total Players: {1})"},
            { "MessagesEventAlreadyJoined", "You are already in the Event."},
            { "MessagesEventPreEnd", "Event: {0} is now over, waiting for players to respawn before sending home!"},
            { "MessagesEventEnd", "All players respawned, {0} has ended!"},
            { "MessagesEventNoGamePlaying", "An Event game is not underway."},
            { "MessagesEventCancel", "The Event was cancelled!"},
            { "MessagesEventClose", "The Event entrance is now closed!"},
            { "MessagesEventOpen", "The Event is now open for : {0} !  Type /event join to join!"},
            { "MessagesPermissionsNotAllowed", "You are not allowed to use this command"},
            { "MessagesEventNotSet", "An Event game must first be chosen."},
            { "MessagesErrorSpawnfileIsNull", "The spawnfile can't be set to null"},
            { "MessagesEventNoSpawnFile", "A spawn file must first be loaded."},
            { "MessagesEventAlreadyOpened", "The Event is already open."},
            { "MessagesEventAlreadyClosed", "The Event is already closed."},
            { "MessagesEventAlreadyStarted", "An Event game has already started."},
            { "ClassSelect", "Choose your class!" },
            { "ClassNotice", "You can reopen this menu at any time by typing /event class" },
            { "CanShop", "You are not allowed to shop while in an Event" },
            { "CanTP", "You are not allowed to teleport while in an Event" },
            { "NoPlayers", "Not enough players" },
            { "NoAuto", "No Automatic Events Configured" },
            { "NoAutoInit", "No Events were successfully initialized, check that your events are correctly configured" },
            { "TimeLimit", "Time limit reached" },
            { "EventCancelled", "Event {0} was cancelled because: {1}" },
            { "EventOpen", "Event {0} in now opened, you can join it by typing /event join" },
            { "StillOpen", "Event {0} is still open, you can join it by typing /event join" },
            { "EventClosed", "The Event is currently closed." },
            { "NotInEvent", "You are not currently in the Event." },
            { "NullKitname", "You can't have a null kitname" },
            { "NoKits", "Unable to find the Kits plugin" },
            { "KitNotExist", "The kit {0} doesn't exist" },
            { "CancelAuto", "Auto events have been cancelled" }
        };
        #endregion

        #region Class Selection
        private void SelectClass(BasePlayer player)
        {
            string panelName = "ClassSelector";
            CuiHelper.DestroyUi(player, panelName);
            if (player.IsSleeping() || player.IsReceivingSnapshot() || player.IsDead())
            {                
                timer.Once(3, () => SelectClass(player));
                return;
            }            

            var Class_Element = UI.CreateElementContainer(panelName, "0.1 0.1 0.1 0.98", "0.05 0.05", "0.95 0.95", true);

            UI.CreatePanel(ref Class_Element, panelName, "0.9 0.9 0.9 0.1", "0.04 0.05", "0.96 0.94");
            UI.CreateLabel(ref Class_Element, panelName, "0.9 0.9 0.9 1.0", $"<color={configData.Messaging_MainColor}>{EventGameName}</color>", 24, "0.05 0.85", "0.95 0.92");
            UI.CreateLabel(ref Class_Element, panelName, "0.9 0.9 0.9 1.0", $"<color={configData.Messaging_MainColor}>{GetMessage("ClassSelect")}</color>", 24, "0.05 0.75", "0.95 0.83");
            UI.CreateLabel(ref Class_Element, panelName, "0.9 0.9 0.9 1.0", $"<color={configData.Messaging_MainColor}>{GetMessage("ClassNotice")}</color>", 18, "0.05 0.05", "0.95 0.12");

            int i = 0;
            foreach (var entry in classData.ClassKits)
            {
                CreateClassButton(ref Class_Element, panelName, entry.Key, entry.Value, i);
                i++;
            }

            CuiHelper.AddUi(player, Class_Element);
        }
        private void CreateClassButton(ref CuiElementContainer container, string panelName, string name, string kit, int number)
        {
            Vector2 dimensions = new Vector2(0.25f, 0.07f);
            Vector2 origin = new Vector2(0.095f, 0.6f);
            float offsetY = 0;
            float offsetX = 0;
            switch (number)
            {
                case 0:
                case 1:
                case 2:
                    offsetX = (0.03f + dimensions.x) * number;
                    break;
                case 3:
                case 4:
                case 5:
                    {
                        offsetX = (0.03f + dimensions.x) * (number - 3);
                        offsetY = (0.07f + dimensions.y) * 1;
                    }
                    break;
                case 6:
                case 7:
                case 8:
                    {
                        offsetX = (0.03f + dimensions.x) * (number - 6);
                        offsetY = (0.07f + dimensions.y) * 2;
                    }
                    break;
            }
            Vector2 offset = new Vector2(offsetX, -offsetY);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            UI.CreateButton(ref container, panelName, "0.2 0.2 0.2 0.7", name, 18, posMin.x + " " + posMin.y, posMax.x + " " + posMax.y, $"Choose_Class {kit}");
        }

        [ConsoleCommand("Choose_Class")]
        void cmdChoose_Class(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, "ClassSelector");
            var className = arg.GetString(0).Replace("'", "");
            bool noGear = false;
            if (string.IsNullOrEmpty(player.GetComponent<EventPlayer>().currentClass)) noGear = true;
            player.GetComponent<EventPlayer>().currentClass = className;
            if (noGear) GivePlayerKit(player, null);
        }
        #endregion

        #region Game Timer UI
        private void StartTimer(int time)
        {
            AutoArenaTimers.Add(timer.Once(time, () => CancelEvent(GetMessage("TimeLimit"))));
            PlayTimer = time;
            foreach (var player in EventPlayers)
                TimerCountdown(player.player);            
        }
        private void TimerCountdown(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PlayTimer");
            if (EventStarted)
            {
                var timerElement = UI.CreateElementContainer("PlayTimer", "0.3 0.3 0.3 0.6", "0.45 0.91", "0.55 0.948", false);
                TimeSpan dateDifference = TimeSpan.FromSeconds(PlayTimer);
                string clock = string.Format("{0:D2}:{1:D2}", dateDifference.Minutes, dateDifference.Seconds);
                UI.CreateLabel(ref timerElement, "PlayTimer", "", clock, 20, "0 0", "1 1");
                CuiHelper.AddUi(player, timerElement);
                PlayTimer--;
                AutoArenaTimers.Add(timer.In(1, () => TimerCountdown(player)));
            }
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ClassSelector");
            CuiHelper.DestroyUi(player, "PlayTimer");
        }
        #endregion

        #region Global Functions
        bool hasAccess(ConsoleSystem.Arg arg)
        {
            if (arg.connection?.authLevel < 1)
            {
                SendReply(arg, GetMessage("MessagesPermissionsNotAllowed"));
                return false;
            }
            return true;
        }
        static void TPPlayer(BasePlayer player, Vector3 destination)
        {           
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.TransformChanged();
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }
        static void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");            
        }
        #endregion

        #region Player Management
        [HookMethod("TeleportAllPlayersToEvent")]
        public void TeleportAllPlayersToEvent()
        {
            foreach (EventPlayer eventplayer in EventPlayers.ToArray())
                TeleportPlayerToEvent(eventplayer.player);
                //Interface.Oxide.CallHook("OnEventPlayerSpawn", eventplayer.player);
        }
        //void OnEventPlayerSpawn(BasePlayer player) => TeleportPlayerToEvent(player);
        void TeleportPlayerToEvent(BasePlayer player)
        {
            var eventPlayer = player.GetComponent<EventPlayer>();
            if (eventPlayer == null || player.net?.connection == null) return;
            var targetpos = Spawns.Call("GetRandomSpawn", EventSpawnFile);
            if (targetpos is string)
                return;
            var newpos = Interface.Oxide.CallHook("EventChooseSpawn", player, targetpos);
            if (newpos is Vector3)
                targetpos = newpos;
            if (newpos is bool)
                if ((bool)newpos == false)
                {
                    timer.Once(3, () => TeleportPlayerToEvent(player));
                        return;
                }
            ZoneManager?.Call("AddPlayerToZoneKeepinlist", ZoneName, player);

            TPPlayer(player, (Vector3)targetpos);

            Interface.Oxide.CallHook("OnEventPlayerSpawn", player);
        }
        void SaveAllInventories()
        {
            foreach (EventPlayer player in EventPlayers)
                player?.SaveInventory();
        }
        void SaveAllPlayerStats()
        {
            foreach (EventPlayer player in EventPlayers)
                player?.SaveHealth();
        }
        void SaveAllHomeLocations()
        {
            foreach (EventPlayer player in EventPlayers)
                player?.SaveHome();
        }
        void SetAllEventPlayers()
        {
            foreach (EventPlayer player in EventPlayers)
                SetEventPlayer(player);
        }      
        void RedeemInventory(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (player.IsDead() || player.health < 1)
            {
                timer.Once(5, () => RedeemInventory(player));
                return;
            }
            eventplayer.player.inventory.Strip();
            if (eventplayer.savedInventory) 
                eventplayer.RestoreInventory();            
        }
        void TeleportPlayerHome(BasePlayer player)
        {            
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (player.IsDead() || player.health < 1)
                return;
            if (eventplayer.savedHome)
                eventplayer.TeleportHome();
        }
        void TryErasePlayer(BasePlayer player)
        {
            var eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer == null) return;
            if (!(eventplayer.inEvent) && !(eventplayer.savedHome) && !(eventplayer.savedInventory))
            {
                eventplayer.enabled = false;
                EventPlayers.Remove(eventplayer);
                UnityEngine.Object.Destroy(eventplayer);                
            }
        }
        [HookMethod("GivePlayerKit")]
        public void GivePlayerKit(BasePlayer player, string GiveKit)
        {
            player.inventory.Strip();
            if (!AutoEventLaunched)
            {
                if (!UseClassSelection)
                    Kits.Call("GiveKit", player, GiveKit);
                else
                {
                    if (string.IsNullOrEmpty(player.GetComponent<EventPlayer>().currentClass))
                        SelectClass(player);
                    else GiveClassKit(player);
                }
            }
            else
            {
                if (!configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum].UseClassSelector)
                    Kits.Call("GiveKit", player, configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum].Kit);
                else
                {
                    if (string.IsNullOrEmpty(player.GetComponent<EventPlayer>().currentClass))
                        SelectClass(player);
                    else GiveClassKit(player);
                }
            }
        }
        private void GiveClassKit(BasePlayer player)
        {
            Kits.Call("GiveKit", player, player.GetComponent<EventPlayer>().currentClass);
            Interface.Oxide.CallHook("OnPlayerSelectClass", player);
        }
        void EjectPlayer(BasePlayer player)
        {
            if (player.IsAlive())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                player.CancelInvoke("WoundingEnd");
                player.metabolism.bleeding.value = 0f;
            }
            if (!string.IsNullOrEmpty(ZoneName))
                ZoneManager?.Call("RemovePlayerFromZoneKeepinlist", ZoneName, player);

            player.GetComponent<EventPlayer>().inEvent = false;
            Interface.Oxide.CallHook("DisableBypass", player.userID);           
        }
        void EjectAllPlayers()
        {
            foreach (EventPlayer eventplayer in EventPlayers)            
                EjectPlayer(eventplayer.player); 
        }
        void SendPlayersHome()
        {
            foreach (EventPlayer eventplayer in EventPlayers)
            {
                TeleportPlayerHome(eventplayer.player);
                RestorePlayerHealth(eventplayer.player);
            }
        }
        void RestorePlayerHealth(BasePlayer player)
        {
            EventPlayer eventplayer = player.GetComponent<EventPlayer>();
            if (eventplayer)
            {
                player.health = eventplayer.health;
                player.metabolism.calories.value = eventplayer.calories;
                player.metabolism.hydration.value = eventplayer.hydration;
                player.metabolism.bleeding.value = 0;
                player.metabolism.SendChangesToClient();
            }
        }
        void RedeemPlayersInventory()
        {
            foreach (EventPlayer eventplayer in EventPlayers)            
                RedeemInventory(eventplayer.player);
        }
        void TryEraseAllPlayers()
        {
            for (int i = 0; i < EventPlayers.Count; i++)         
                TryErasePlayer(EventPlayers[i].player);            
        }
        #endregion

        #region Event Management
        [HookMethod("OpenEvent")]
        public object OpenEvent()
        {
            if (EventOpen)
                return $"{EventGameName} is already open";

            var success = Interface.Oxide.CallHook("CanEventOpen");
            if (success is string)            
                return (string)success;
            
            EventOpen = true;
            EventPlayers = new List<EventPlayer>();
            BroadcastToChat(string.Format(GetMessage("MessagesEventOpen"), EventGameName));
            Interface.Oxide.CallHook("OnEventOpenPost");
            return true;
        }
        void OnEventOpenPost() => OnEventOpenPostAutoEvent();        
        void OnEventOpenPostAutoEvent()
        {
            if (!AutoEventLaunched) return;

            DestroyTimers();
            var autocfg = configData.z_AutoEvents;
            if (autocfg.AutoCancel_Timer != 0)
                AutoArenaTimers.Add(timer.Once(autocfg.AutoCancel_Timer, () => CancelEvent(GetMessage("NoPlayers"))));
            AutoArenaTimers.Add(timer.Repeat(configData.AnnounceEvent_Interval, 0, AnnounceEvent));
        }
        object CanEventOpen()
        {
            if (EventGameName == null) return GetMessage("MessagesEventNotSet");
            else if (EventSpawnFile == null) return GetMessage("MessagesEventNoSpawnFile");
            else if (EventOpen) return GetMessage("MessagesEventAlreadyOpened");

            object success = Spawns.Call("GetSpawnsCount", EventSpawnFile);
            if (success is string)
                return (string)success;
            return null;
        }

        [HookMethod("CloseEvent")]
        public object CloseEvent()
        {
            if (!EventOpen) return GetMessage("MessagesEventAlreadyClosed");
            EventOpen = false;
            Interface.Oxide.CallHook("OnEventClosePost");
            if (EventStarted)
                BroadcastToChat(GetMessage("MessagesEventClose"));
            else
                BroadcastToChat(GetMessage("MessagesEventCancel"));
            return true;
        }
        object AutoEventNext()
        {
            if (configData.z_AutoEvents.z_AutoEventSetup.Count == 0)
            {
                AutoEventLaunched = false;
                return GetMessage("NoAuto");
            }
            bool successful = false;
            for (int i = 0; i < configData.z_AutoEvents.z_AutoEventSetup.Count; i++)
            {
                EventAutoNum++;
                if (EventAutoNum >= configData.z_AutoEvents.z_AutoEventSetup.Count) EventAutoNum = 0;

                var autocfg = configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum];

                object success = SelectEvent(autocfg.GameType);
                if (success is string) { continue; }

                success = SelectSpawnfile(autocfg.Spawnfile);
                if (success is string) { continue; }

                success = SelectMinplayers(autocfg.MinimumPlayers);
                if (success is string) { continue; }

                success = SelectMaxplayers(autocfg.MaximumPlayers);
                if (success is string) { continue; }

                success = Interface.Oxide.CallHook("CanEventOpen");
                if (success is string) { continue; }

                if (!string.IsNullOrEmpty(autocfg.ZoneID))
                    ZoneName = autocfg.ZoneID;

                successful = true;
                break;
            }
            if (!successful)            
                return GetMessage("NoAutoInit");

            AutoArenaTimers.Add(timer.Once(configData.z_AutoEvents.GameInterval, () => OpenEvent()));
            return null;
        }
        void OnEventStartPost()
        {
            DestroyTimers();
            if (AutoEventLaunched)
                OnEventStartPostAutoEvent();
            else if (EventMode == GameMode.Battlefield)
                StartTimer(configData.Battlefield_Timer);
            if (configData.AnnounceDuring_Event)
                AutoArenaTimers.Add(timer.Repeat(configData.AnnounceEvent_Interval, 0, () => AnnounceDuringEvent()));
        }
        void OnEventStartPostAutoEvent()
        {           
            if (configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum].TimeLimit != 0)
                StartTimer(configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum].TimeLimit);
        }
        void DestroyTimers()
        {
            foreach (Timer eventtimer in AutoArenaTimers)
                eventtimer.Destroy();            
            AutoArenaTimers.Clear();
        }
        void CancelEvent(string reason)
        {
            var message = GetMessage("EventCancelled");
            object success = Interface.Oxide.CallHook("OnEventCancel");
            if (success != null)
            {
                if (success is string)
                    message = (string)success;
                else
                    return;
            }
            BroadcastToChat(string.Format(message, EventGameName, reason));
            DestroyTimers(); 
            if (EventStarted)           
                EndEvent();
            else if (AutoEventLaunched)
                AutoEventNext();
        }
        void AnnounceEvent()
        {
            var message = GetMessage("EventOpen");
            object success = Interface.Oxide.CallHook("OnEventAnnounce");
            if (success is string)
            {
                message = (string)success;
            }
            BroadcastToChat(string.Format(message, EventGameName));
        }
        void AnnounceDuringEvent()
        {
            if (configData.AnnounceDuring_Event)
            {
                if (EventOpen && EventStarted)
                {
                    var message = GetMessage("StillOpen");
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        if (!player.GetComponent<EventPlayer>())
                            SendReply(player, string.Format(message, EventGameName));
                    }
                }
            }
        }
        object LaunchEvent()
        {            
            AutoEventLaunched = true;
            if (!EventStarted)
            {
                if (!EventOpen)
                {
                    object success = AutoEventNext();
                    if (success is string)                    
                        return (string)success;
                    
                    success = OpenEvent();
                    if (success is string)                    
                        return (string)success;                    
                }
                else OnEventOpenPostAutoEvent();                  
            }
            else OnEventStartPostAutoEvent();
            
            return null;
        }

        [HookMethod("EndEvent")]
        public object EndEvent()
        {
            if (EventEnded) return GetMessage("MessagesEventNoGamePlaying");
            foreach (var player in EventPlayers)            
                Interface.Oxide.CallHook("DestroyUI", player.player);                      

            BroadcastToChat(string.Format(GetMessage("MessagesEventPreEnd"), EventGameName));
            EventOpen = false;
            EventStarted = false;
            EventPending = false;
            EventEnded = true;
            EnableGod();
            timer.Once(5, ProcessPlayers);
            return true;
        }
        void ProcessPlayers()
        {
            if (CheckForDead())
            {
                BroadcastToChat(string.Format(GetMessage("MessagesEventEnd"), EventGameName));
                Interface.Oxide.CallHook("OnEventEndPre");                
                RedeemPlayersInventory();
                SendPlayersHome();
                EjectAllPlayers();
                TryEraseAllPlayers();
                DisableGod();                
                DestroyGame();
                Interface.Oxide.CallHook("OnEventEndPost");                
            }
            else timer.Once(5, ProcessPlayers);
        }        
        bool CheckForDead()
        {
            int i = 0;
            foreach (EventPlayer p in EventPlayers)
            {
                if (p.player.IsDead() || !p.player.IsAlive())
                {
                    var pos = Spawns.Call("GetRandomSpawn", EventSpawnFile);
                    if (pos is Vector3) p.player.RespawnAt((Vector3) pos, new Quaternion());
                    else p.player.Respawn();
                    i++;
                }
                else if (p.player.IsWounded() || p.player.health < 2)
                {
                    p.player.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                    RestorePlayerHealth(p.player);
                    i++;
                }
                else if (p.player.IsSleeping())
                {
                    p.player.EndSleeping();
                    i++;
                }
            }
            return i == 0;
        }
        void EnableGod()
        {
            Godmode = new List<ulong>();
            foreach (EventPlayer player in EventPlayers)
            {
                Godmode.Add(player.player.userID);
                player.player.metabolism.bleeding.value = 0;
                player.player.metabolism.SendChangesToClient();
            }
        }
        void DisableGod()
        {      
            Godmode.Clear();
        }
        void DestroyGame()
        {
            DestroyTimers();
            EventPlayers.Clear();
            ZoneName = "";
            var objects = UnityEngine.Object.FindObjectsOfType<EventPlayer>();
            EventPlayer empty = new EventPlayer();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
        }
        object CanEventStart()
        {
            if (EventGameName == null) return GetMessage("MessagesEventNotSet");
            if (EventSpawnFile == null) return GetMessage("MessagesEventNoSpawnFile");
            return EventStarted ? GetMessage("MessagesEventAlreadyStarted") : null;
        }

        [HookMethod("StartEvent")]
        public object StartEvent()
        {
            object success = Interface.Oxide.CallHook("CanEventStart");
            if (success is string)            
                return (string)success;            
            
            Interface.Oxide.CallHook("OnEventStartPre");
            if (!AutoEventLaunched)
                ZoneName = (string)Interface.Oxide.CallHook("OnRequestZoneName");
            BroadcastToChat(string.Format(GetMessage("MessagesEventBegin"), EventGameName));
            EventStarted = true;
            EventEnded = false;
            DestroyTimers();
            SaveAllInventories();
            SaveAllHomeLocations();
            SaveAllPlayerStats();
            SetAllEventPlayers();
            TeleportAllPlayersToEvent();
            Interface.Oxide.CallHook("OnEventStartPost");
            return true;
        }        
       
        void SetEventPlayer(EventPlayer player)
        {
            Interface.Oxide.CallHook("EnableBypass", player.player.userID);
            player.inEvent = true;
            player.enabled = true;
            player.SaveHome();
            player.SaveInventory();
            player.SaveHealth();
        }
        object JoinEvent(BasePlayer player)
        {
            if (player.GetComponent<EventPlayer>())            
                if (EventPlayers.Contains(player.GetComponent<EventPlayer>()))
                    return GetMessage("MessagesEventAlreadyJoined");            

            object success = Interface.Oxide.CallHook("CanEventJoin", player);
            if (success is string)            
                return (string)success;
            var eventPlayer = player.GetComponent<EventPlayer>() ?? player.gameObject.AddComponent<EventPlayer>();
            EventPlayers.Add(eventPlayer);
            if (EventStarted)
            {
                if (EventMode == GameMode.Battlefield || (AutoEventLaunched && configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum].TimeLimit != 0))
                    TimerCountdown(player);
                SetEventPlayer(eventPlayer);
                BroadcastToChat(string.Format(GetMessage("MessagesEventJoined"), player.displayName, EventPlayers.Count));
                Interface.Oxide.CallHook("OnEventJoinPost", player);
                TeleportPlayerToEvent(player);
                return true;
            }            

            BroadcastToChat(string.Format(GetMessage("MessagesEventJoined"), player.displayName, EventPlayers.Count));
            Interface.Oxide.CallHook("OnEventJoinPost", player);
            return true;
        }
        object CanEventJoin(BasePlayer player)
        {
            if (!EventOpen)
                return GetMessage("EventClosed");

            if (EventMaxPlayers != 0 && EventPlayers.Count >= EventMaxPlayers)            
                return string.Format(GetMessage("MessagesEventMaxPlayers"), EventGameName); 
                       
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (!AutoEventLaunched) return null;
            var autocfg = configData.z_AutoEvents.z_AutoEventSetup[EventAutoNum];
            if (EventPlayers.Count >= autocfg.MinimumPlayers && !EventStarted && EventEnded && !EventPending)
            {                
                float timerStart = autocfg.TimeToJoin;
                BroadcastToChat(string.Format(GetMessage("MessagesEventMinPlayers"), EventGameName, timerStart));

                EventPending = true;
                DestroyTimers();
                AutoArenaTimers.Add(timer.Once(timerStart, () => StartEvent()));
            }
            return null;
        }
        void OnEventEndPost()
        {
            if (AutoEventLaunched)
                AutoEventNext();
        }
        [HookMethod("LeaveEvent")]
        public object LeaveEvent(BasePlayer player)
        {
            var eventPlayer = player.GetComponent<EventPlayer>();
            if (eventPlayer == null && !EventPlayers.Contains(eventPlayer))            
                return GetMessage("NotInEvent");  

            Interface.Oxide.CallHook("OnEventLeavePre");
            Interface.Oxide.CallHook("DisableBypass", player.userID);
            eventPlayer.inEvent = false;

            if (!EventEnded || !EventStarted)            
                BroadcastToChat(string.Format(GetMessage("MessagesEventLeft"), player.displayName, (EventPlayers.Count - 1)));
            
            if (!string.IsNullOrEmpty(ZoneName))
                ZoneManager?.Call("RemovePlayerFromZoneKeepinlist", ZoneName, player);

            if (EventStarted)
            {
                //EventPlayers.Remove(eventPlayer);
                player.inventory.Strip();                
                RedeemInventory(player);
                TeleportPlayerHome(player);
                RestorePlayerHealth(player);
                EjectPlayer(player);
                TryErasePlayer(player);
                Interface.Oxide.CallHook("OnEventLeavePost", player);
            }
            else
            {
                EventPlayers.Remove(eventPlayer);
                UnityEngine.Object.Destroy(eventPlayer);
            }
            return true;
        }
        [HookMethod("SelectEvent")]
        public object SelectEvent(string name)
        {
            if (!(EventGames.Contains(name))) return string.Format(GetMessage("MessagesEventNotAnEvent"), name);
            if (EventStarted || EventOpen) return GetMessage("MessagesEventCloseAndEnd");
            EventGameName = name;
            Interface.Oxide.CallHook("OnSelectEventGamePost", name);
            return true;
        }

        [HookMethod("SelectSpawnfile")]
        public object SelectSpawnfile(string name)
        {
            if (name == null) return GetMessage("MessagesErrorSpawnfileIsNull");

            var eventset = CheckEventSet();
            if (eventset is string)
                return (string)eventset;

            object success = Interface.Oxide.CallHook("OnSelectSpawnFile", name);
            if (success == null)            
                return string.Format(GetMessage("MessagesEventNotAnEvent"), EventGameName);            

            EventSpawnFile = name;
            success = Spawns.Call("GetSpawnsCount", EventSpawnFile);

            if (success is string)
            {
                EventSpawnFile = null;
                return (string)success;
            }

            return true;
        }
        object SelectKit(string kitname)
        {
            if (kitname == null) return GetMessage("NullKitname");
            var eventset = CheckEventSet();
            if (eventset is string)
                return (string)eventset;

            object success = Kits.Call("isKit", kitname);
            if (!(success is bool))            
                return GetMessage("NoKits");
            if (!(bool)success)            
                return string.Format(GetMessage("KitNotExist"), kitname); 
            success = Interface.Oxide.CallHook("OnSelectKit", kitname);
            if (success == null)            
                return $"{EventGameName} doesn't let you choose a kit";            
            return true;
        }       
        object SelectMaxplayers(int num)
        {
            var eventset = CheckEventSet();
            if (eventset is string)
                return (string)eventset;

            Interface.Oxide.CallHook("OnPostSelectMaxPlayers", num);
            return true;
        }
        object SelectMinplayers(int num)
        {
            var eventset = CheckEventSet();
            if (eventset is string)
                return (string)eventset;

            Interface.Oxide.CallHook("OnPostSelectMinPlayers", num);
            return true;
        }
        object SelectNewZone(MonoBehaviour monoplayer, string radius)
        {
            var eventset = CheckEventSet();
            if (eventset is string)
                return (string)eventset;

            if (EventStarted || EventOpen) return GetMessage("MessagesEventCloseAndEnd");
            Interface.Oxide.CallHook("OnSelectEventZone", monoplayer, radius);            
            return true;
        }
        private object CheckEventSet()
        {
            if (string.IsNullOrEmpty(EventGameName)) return GetMessage("MessagesEventNotSet");
            if (!(EventGames.Contains(EventGameName))) return string.Format(GetMessage("MessagesEventNotAnEvent"), EventGameName);
            return null;
        }

        [HookMethod("RegisterEventGame")]
        public object RegisterEventGame(string name)
        {
            if (!(EventGames.Contains(name)))
                EventGames.Add(name);
            Puts(string.Format("Registered event game: {0}", name));
            Interface.Oxide.CallHook("OnSelectEventGamePost", EventGameName);

            if (EventGameName == name)
            {
                object success = SelectEvent(EventGameName);
                if (success is string)                
                    Puts((string)success);
            }            
            return true;
        }
        void OnExitZone(string zoneId, BasePlayer player)
        {
            if (EventStarted)
                if(player.GetComponent<EventPlayer>())
                    if (zoneId.Equals(ZoneName))
                        if (configData.KillDeserters)
                            player.Die();
        }
        #endregion

        #region Commands
        [ChatCommand("event")]
        void cmdEvent(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                string message = string.Empty;
                if (!EventOpen && !EventStarted) message = GetMessage("MessagesEventStatusClosedEnd");
                else if (EventOpen && !EventStarted) message = GetMessage("MessagesEventStatusOpen");
                else if (EventOpen && EventStarted) message = GetMessage("MessagesEventStatusOpenStarted");
                else message = GetMessage("MessagesEventStatusClosedStarted");
                MSG(player, string.Format(message, EventGameName));

                if (EventOpen)
                {
                    SendReply(player, "/event join - Join a event");
                    SendReply(player, "/event leave - Leave a event");
                    if (UseClassSelection)
                        SendReply(player, "/event class - Opens the class selector");
                }
                if (player.IsAdmin())
                {
                    SendReply(player, "/event open - Open a event");
                    SendReply(player, "/event cancel - Cancel a event");
                    SendReply(player, "/event cs - Activate/de-activate class selection");
                    SendReply(player, "/event cs add <classname> <kitname> - Add a new kit to class selection");
                    SendReply(player, "/event cs remove <classname> - Remove a kit from class selection");
                    SendReply(player, "/event start - Start a event");
                    SendReply(player, "/event close - Close a event to new entries");
                    SendReply(player, "/event end - End a event");
                    SendReply(player, "/event launch - Launch auto events");
                    SendReply(player, "/event game \"Game Name\" - Change event game");
                    SendReply(player, "/event gamemode <normal/battlefield> - Switch game modes");
                    SendReply(player, "/event minplayers XX - Set minimum required players (auto event)");
                    SendReply(player, "/event maxplayers XX - Set maximum players (auto event)");
                    SendReply(player, "/event spawnfile \"filename\" - Change the event spawnfile");
                    SendReply(player, "/event kit \"kitname\" - Change the event kit");
                }
                return;
            }
            switch (args[0].ToLower())
            {
                case "join":
                    object join = JoinEvent(player);
                    if (join is string)
                    {
                        SendReply(player, (string)join);
                        return;
                    }
                    return;
                case "leave":
                    object leave = LeaveEvent(player);
                    if (leave is string)
                    {
                        SendReply(player, (string)leave);
                        return;
                    }
                    return;
                case "class":
                    if (UseClassSelection)                    
                        if (EventStarted)
                            if (player.GetComponent<EventPlayer>())
                                SelectClass(player);                    
                    return;
            }
            if (!player.IsAdmin()) return;
            switch (args[0].ToLower())
            {
                case "cancel":
                    AutoEventLaunched = false;
                    if (EventOpen) CancelEvent(GetMessage("CancelAuto"));
                    DestroyTimers();
                    SendReply(player, GetMessage("CancelAuto"));
                    return;
                case "open":
                    object open = OpenEvent();
                    if (open is string)
                    {
                        SendReply(player, (string)open);
                        return;
                    }
                    SendReply(player, string.Format("Event \"{0}\" is now opened.", EventGameName));
                    return;
                case "start":
                    object start = StartEvent();
                    if (start is string)
                    {
                        SendReply(player, (string)start);
                        return;
                    }
                    SendReply(player, string.Format("Event \"{0}\" is now started.", EventGameName));
                    return;
                case "close":
                    object close = CloseEvent();
                    if (close is string)
                    {
                        SendReply(player, (string)close);
                        return;
                    }
                    SendReply(player, string.Format("Event \"{0}\" is now closed for entries.", EventGameName));
                    return;
                case "cs":
                    if (args.Length >= 2)
                    {
                        switch (args[1].ToLower())
                        {
                            case "add":
                                if (classData.ClassKits.Count >= 9)
                                {
                                    SendReply(player, "You have already set the maximum number of classes");
                                    return;
                                }
                                if (args.Length == 4)
                                {
                                    object isKit = Kits.Call("isKit", args[3]);
                                    if (!(isKit is bool))
                                    {
                                        SendReply(player, "Unable to find the kits plugin");
                                        return;
                                    }
                                    if (!(bool)isKit)
                                    {
                                        SendReply(player, string.Format("The kit {0} doesn't exist", args[3]));
                                        return;
                                    }
                                    classData.ClassKits.Add(args[2], args[3]);
                                    SaveData();
                                    SendReply(player, $"You have successfully added a new class kit {args[2]}, using kit {args[3]}");
                                }
                                return;
                            case "remove":
                                if (args.Length == 3)
                                {
                                    if (classData.ClassKits.ContainsKey(args[2]))
                                    {
                                        classData.ClassKits.Remove(args[2]);
                                        SaveData();
                                        SendReply(player, $"You have successfully removed the class {args[2]}");
                                        return;
                                    }
                                    SendReply(player, string.Format("The class {0} doesn't exist", args[2]));
                                }
                                return;
                        }
                    }
                    if (UseClassSelection)
                    {
                        UseClassSelection = false;
                        SendReply(player, "You have de-activated class selection");
                        return;
                    }
                    if (classData.ClassKits.Count < 1)
                    {
                        SendReply(player, "You must set classes before activating the class selector");
                        return;
                    }
                    UseClassSelection = true;
                    SendReply(player, "You have activated class selection");
                    return;
                case "end":
                    object end = EndEvent();
                    if (end is string)
                    {
                        SendReply(player, (string)end);
                        return;
                    }
                    SendReply(player, string.Format("Event \"{0}\" has ended.", EventGameName));
                    return;
                case "game":
                    if (args.Length > 1)
                    {
                        object game = SelectEvent(args[1]);
                        if (game is string)
                        {
                            SendReply(player, (string)game);
                            return;
                        }
                        configData.Default_Gamemode = EventGameName;
                        SaveConfig();
                        SendReply(player, string.Format("{0} is now the next Event game.", args[1]));
                    }
                    return;
                case "gamemode":
                    if (args.Length > 1)
                    {
                        switch (args[1].ToLower())
                        {
                            case "normal":
                                EventMode = GameMode.Normal;
                                break;
                            case "battlefield":
                                EventMode = GameMode.Battlefield;
                                break;
                            default:
                                break;                      
                        }
                        SendReply(player, string.Format("Event game mode is now set to {0}", EventMode.ToString()));
                    }
                    return;
                case "minplayers":
                    if (args.Length > 1)
                    {
                        int min;
                        if (!int.TryParse(args[1], out min))
                        {
                            MSG(player, "You must enter a number", false);
                            return;
                        }
                        object minplayers = SelectMinplayers(min);
                        if (minplayers is string)
                        {
                            MSG(player, (string)minplayers);
                            return;
                        }
                        SendReply(player, string.Format("Minimum Players for {0} is now {1} (this is only useful for auto events).", args[1], EventSpawnFile));
                    }
                    return;
                case "maxplayers":
                    if (args.Length > 1)
                    {
                        int max;
                        if (!int.TryParse(args[1], out max))
                        {
                            MSG(player, "You must enter a number", false);
                            return;
                        }
                        object maxplayers = SelectMaxplayers(max);
                        if (maxplayers is string)
                        {
                            SendReply(player, (string)maxplayers);
                            return;
                        }
                        SendReply(player, string.Format("Maximum Players for {0} is now {1}.", args[1], EventSpawnFile));
                    }
                    return;
                case "spawnfile":
                    if (args.Length > 1)
                    {
                        object spawnfile = SelectSpawnfile(args[1]);
                        if (spawnfile is string)
                        {
                            SendReply(player, (string)spawnfile);
                            return;
                        }
                        configData.Default_Spawnfile = args[1];
                        SaveConfig();
                        SendReply(player, string.Format("Spawnfile for {0} is now {1} .", EventGameName, EventSpawnFile));
                    }
                        return;
                case "kit":
                    if (args.Length > 1)
                    {
                        object success = SelectKit(args[1]);
                        if (success is string)
                        {
                            SendReply(player, (string)success);
                            return;
                        }
                        SendReply(player, string.Format("The new Kit for {0} is now {1}", EventGameName, args[1]));
                    }
                    return;
                case "launch":
                    object launch = LaunchEvent();
                    if (launch is string)
                    {
                        SendReply(player, (string)launch);
                        return;
                    }
                    SendReply(player, string.Format("Event \"{0}\" is now launched.", EventGameName));
                    return;
            }
        }

        [ConsoleCommand("event")]
        void ccmdEvent(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "event open - Open a event");
                SendReply(arg, "event cancel - Cancel a event");
                SendReply(arg, "event start - Start a event");
                SendReply(arg, "event close - Close a event to new entries");
                SendReply(arg, "event end - End a event");
                SendReply(arg, "event launch - Launch auto events");
                SendReply(arg, "event game \"Game Name\" - Change event game");
                SendReply(arg, "event minplayers XX - Set minimum required players (auto event)");
                SendReply(arg, "event maxplayers XX - Set maximum players (auto event)");
                SendReply(arg, "event spawnfile \"filename\" - Change the event spawnfile");
                SendReply(arg, "event kit \"kitname\" - Change the event kit");
                SendReply(arg, "event cs - Activate/de-activate class selection");
                SendReply(arg, "event cs add <classname> <kitname> - Add a new kit to class selection");
                SendReply(arg, "event cs remove <classname> - Remove a kit from class selection");
                return;
            }
            switch (arg.Args[0].ToLower())
            {
                case "cancel":
                    AutoEventLaunched = false;
                    if (EventOpen) CancelEvent("Auto events have been cancelled");
                    DestroyTimers();
                    SendReply(arg, string.Format("Auto events have been cancelled", EventGameName));
                    return;
                case "open":
                    object open = OpenEvent();
                    if (open is string)
                    {
                        SendReply(arg, (string)open);
                        return;
                    }
                    SendReply(arg, string.Format("Event \"{0}\" is now opened.", EventGameName));
                    return;
                case "start":
                    object start = StartEvent();
                    if (start is string)
                    {
                        SendReply(arg, (string)start);
                        return;
                    }
                    SendReply(arg, string.Format("Event \"{0}\" is now started.", EventGameName));
                    return;
                case "close":
                    object close = CloseEvent();
                    if (close is string)
                    {
                        SendReply(arg, (string)close);
                        return;
                    }
                    SendReply(arg, string.Format("Event \"{0}\" is now closed for entries.", EventGameName));
                    return;
                case "cs":
                    if (arg.Args.Length > 1)
                    {
                        switch (arg.Args[1].ToLower())
                        {
                            case "add":
                                if (classData.ClassKits.Count >= 9)
                                {
                                    SendReply(arg, "You have already set the maximum number of classes");
                                    return;
                                }
                                if (arg.Args.Length == 4)
                                {
                                    object isKit = Kits.Call("isKit", arg.Args[3]);
                                    if (!(isKit is bool))
                                    {
                                        SendReply(arg, "Unable to find the kits plugin");
                                        return;
                                    }
                                    if (!(bool)isKit)
                                    {
                                        SendReply(arg, string.Format("The kit {0} doesn't exist", arg.Args[3]));
                                        return;
                                    }
                                    classData.ClassKits.Add(arg.Args[2], arg.Args[3]);
                                    SaveData();
                                    SendReply(arg, $"You have successfully added a new class kit {arg.Args[2]}, using kit {arg.Args[3]}");
                                }
                                return;
                            case "remove":
                                if (arg.Args.Length == 3)
                                {
                                    if (classData.ClassKits.ContainsKey(arg.Args[2]))
                                    {
                                        classData.ClassKits.Remove(arg.Args[2]);
                                        SaveData();
                                        SendReply(arg, $"You have successfully removed the class {arg.Args[2]}");
                                        return;
                                    }
                                    SendReply(arg, string.Format("The class {0} doesn't exist", arg.Args[2]));
                                }
                                return;
                        }
                    }
                    if (UseClassSelection)
                    {
                        UseClassSelection = false;
                        SendReply(arg, "You have de-activated class selection");
                        return;
                    }
                    if (classData.ClassKits.Count < 1)
                    {
                        SendReply(arg, "You must set classes before activating the class selector");
                        return;
                    }
                    UseClassSelection = true;
                    SendReply(arg, "You have activated class selection");
                    return;
                case "end":
                    object end = EndEvent();
                    if (end is string)
                    {
                        SendReply(arg, (string)end);
                        return;
                    }
                    SendReply(arg, string.Format("Event \"{0}\" has ended.", EventGameName));
                    return;
                case "game":
                    object game = SelectEvent(arg.Args[1]);
                    if (game is string)
                    {
                        SendReply(arg, (string)game);
                        return;
                    }
                    configData.Default_Gamemode = EventGameName;
                    SaveConfig();
                    SendReply(arg, string.Format("{0} is now the next Event game.", arg.Args[1]));
                    return;
                case "gamemode":
                    if (arg.Args.Length > 1)
                    {
                        switch (arg.Args[1].ToLower())
                        {
                            case "normal":
                                EventMode = GameMode.Normal;
                                break;
                            case "battlefield":
                                EventMode = GameMode.Battlefield;
                                break;
                            default:
                                break;
                        }
                        SendReply(arg, string.Format("Event game mode is now set to {0}", EventMode.ToString()));
                    }
                    return;
                case "minplayers":
                    int min;
                    if (!int.TryParse(arg.Args[1], out min))
                    {
                        SendReply(arg, "You must enter a number");
                        return;
                    }
                    object minplayers = SelectMinplayers(min);
                    if (minplayers is string)
                    {
                        SendReply(arg, (string)minplayers);
                        return;
                    }
                    SendReply(arg, string.Format("Minimum Players for {0} is now {1} (this is only useful for auto events).", arg.Args[1], EventSpawnFile));
                    return;
                case "maxplayers":
                    int max;
                    if (!int.TryParse(arg.Args[1], out max))
                    {
                        SendReply(arg, "You must enter a number");
                        return;
                    }
                    object maxplayers = SelectMaxplayers(max);
                    if (maxplayers is string)
                    {
                        SendReply(arg, (string)maxplayers);
                        return;
                    }
                    SendReply(arg, string.Format("Maximum Players for {0} is now {1}.", arg.Args[1], EventSpawnFile));
                    return;
                case "spawnfile":
                    object spawnfile = SelectSpawnfile(arg.Args[1]);
                    if (spawnfile is string)
                    {
                        SendReply(arg, (string)spawnfile);
                        return;
                    }
                    configData.Default_Spawnfile = arg.Args[1];
                    SaveConfig();
                    SendReply(arg, string.Format("Spawnfile for {0} is now {1} .", EventGameName, EventSpawnFile));
                    return;
                case "kit":
                    object success = SelectKit(arg.Args[1]);
                    if (success is string)
                    {
                        SendReply(arg, (string)success);
                        return;
                    }
                    SendReply(arg, string.Format("The new Kit for {0} is now {1}", EventGameName, arg.Args[1]));
                    return;  
                case "launch":
                    object launch = LaunchEvent();
                    if (launch is string)
                    {
                        SendReply(arg, (string)launch);
                        return;
                    }
                    SendReply(arg, string.Format("Event \"{0}\" is now launched.", EventGameName));
                    return;
            }
        }
        #endregion

        #region Tokens
        [HookMethod("AddTokens")]
        public void AddTokens(string userid, int amount)
        {            
            if (configData.UseEconomicsAsTokens)
                if (Economics)
                    Economics?.Call("Deposit", userid, amount);
            else ServerRewards?.Call("AddPoints", userid, amount);
        }    
       
        #endregion

        #region Data

        void SaveData()
        {
            Class_Data.WriteObject(classData);
            Puts("Saved class data");
        }        
        void LoadData()
        {
            try
            {
                classData = Class_Data.ReadObject<ClassData>();
            }
            catch
            {
                Puts("Couldn't load class data, creating new datafile");
                classData = new ClassData();
            }            
        }
        #endregion

        //[ConsoleCommand("event.openauto")]
       // void ccmdEventOpenAuto(ConsoleSystem.Arg arg)
        //{
           // if (!hasAccess(arg)) return;
           // object success = OpenEvent();
           // if (success is string)
           // {
           //     SendReply(arg, (string)success);
           //     return;
           // }
           // OpenAutoEventLaunched = true;
           // EventAutoNum = 0;
           // DestroyTimers();
           // var evencfg = EventAutoConfig[EventAutoNum.ToString()] as Dictionary<string, object>;
           // if (evencfg["timelimit"] != null && evencfg["timelimit"].ToString() != "0")
            //    AutoArenaTimers.Add(timer.Once(Convert.ToSingle(evencfg["timelimit"]), () => CancelEvent("Not enough players")));
            //SelectMinplayers((string)evencfg["minplayers"]);
           // SendReply(arg, string.Format("Event \"{0}\" is now opened.", EventGameName));
        //}
    }
}
