using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using UnityEngine;

using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("GUIAnnouncements", "JoeSheep", "1.17.48", ResourceId = 1222)]
    [Description("Creates announcements with custom messages by command across the top of every player's screen in a banner.")]

    public class GUIAnnouncements : RustPlugin
    {
        #region Configuration

        #region Permissions
        const string PermAnnounce = "GUIAnnouncements.announce";
        const string PermAnnounceToggle = "GUIAnnouncements.toggle";
        #endregion
        #region Global Declerations
        private string AnnouncementGUI = String.Empty;
        private string AnnouncementText = String.Empty;
        private Dictionary<ulong, string> Exclusions = new Dictionary<ulong, string>();
        private List<ulong> JustJoined = new List<ulong>();
        private List<ulong> GlobalTimerList = new List<ulong>();
        private Dictionary<BasePlayer, Timer> PrivateTimers = new Dictionary<BasePlayer, Timer>();
        private Dictionary<BasePlayer, Timer> NewPlayerPrivateTimers = new Dictionary<BasePlayer, Timer>();
        private Dictionary<BasePlayer, Timer> PlayerRespawnedTimers = new Dictionary<BasePlayer, Timer>();
        private Timer PlayerTimer;
        private Timer GlobalTimer;
        private Timer NewPlayerTimer;
        private Timer PlayerRespawnedTimer;
        private Timer RealTimeTimer;
        private Timer SixtySecondsTimer;
        private Timer AutomaticAnnouncementsTimer;
        private string LastHitPlayer = String.Empty;
        private bool ConfigUpdated;
        private List<DateTime> RestartTimes;
        private Dictionary<DateTime, TimeSpan> CalcNextRestartDict = new Dictionary<DateTime, TimeSpan>();
        private DateTime NextRestart;
        private int LastHour;
        private int LastMinute;
        private bool RestartCountdown;
        private IEnumerator<string> ATALEnum;
        private bool RestartJustScheduled = false;
        private bool RestartScheduled = false;
        private List<string> RestartAnnouncementsWhenStrings;
        private DateTime ScheduledRestart;
        private TimeSpan AutomaticTimedAnnouncementsRepeat;
        private bool RestartSuspended = false;

        string BannerTintGrey = "0.1 0.1 0.1 0.7";
        string BannerTintRed = "0.5 0.1 0.1 0.7";
        string BannerTintGreen = "0.1 0.4 0.1 0.5";
        string TextYellow = "0.7 0.7 0.1";
        string TextOrange = "0.8 0.5 0.1";
        string TextWhite = "1 1 1";
        string BannerAnchorMaxX()
        {
            if (doNotOverlayLustyMap == true)
                if (lustyMapPosition.ToLower() == "right")
                    return "0.868 ";
            return "1.026 ";
        }
        string BannerAnchorMaxY = "0.9643";
        string BannerAnchorMinX()
        {
            if (doNotOverlayLustyMap == true)
                if (lustyMapPosition.ToLower() == "left")
                    return "0.131 ";
            return "-0.027 ";
        }
        string BannerAnchorMinY = "0.92";
        string TextAnchorMaxX = "0.868 ";
        string TextAnchorMaxY = "0.9643";
        string TextAnchorMinX = "0.131 ";
        string TextAnchorMinY = "0.92";
        #endregion
        //============================================================================================================
        #region Config Option Declerations
        #region Formatting
        public float announcementDuration { get; private set; } = 10f;
        public float welcomeAnnouncementDuration { get; private set; } = 20f;
        public int fontSize { get; private set; } = 18;
        public float fadeOutTime { get; private set; } = 0.5f;
        public float fadeInTime { get; private set; } = 0.5f;
        #endregion
        //============================================================================================================
        #region Automatic Announcements
        public bool automaticTimedAnnouncements { get; private set; } = false;
        public static List<object> automaticTimedAnnouncementsList { get; private set; } = new List<object>
        {
            "Automatic Timed Announcement 1",
            "Automatic Timed Announcement 2",
            "Automatic Timed Announcement 3"
        };
        public string automaticTimedAnnouncementsRepeat { get; private set; } = "00:30:00";
        public bool helicopterAnnouncement { get; private set; } = true;
        public bool helicopterDeathAnnouncement { get; private set; } = true;
        public bool helicopterDeathAnnouncementWithKiller { get; private set; } = true;
        public bool airdropAnnouncement { get; private set; } = true;
        public bool airdropAnnouncementLocation { get; private set; } = true;
        public bool welcomeAnnouncement { get; private set; } = true;
        public bool welcomeBackAnnouncement { get; private set; } = true;
        public bool newPlayerAnnouncements { get; private set; } = true;
        public int newPlayerAnnouncementsShowTimes { get; private set; } = 4;
        public List<object> newPlayerAnnouncementsList { get; private set; } = new List<object>
        {
                    "New player announcement 1.",
                    "New player announcement 2.",
                    "New player announcement 3."
        };
        public bool respawnAnnouncements { get; private set; } = false;
        public List<object> respawnAnnouncementsList { get; private set; } = new List<object>
        {
                    "Respawn announcement 1.",
                    "Respawn announcement 2.",
                    "Respawn announcement 3."
        };
        public bool restartAnnouncements { get; private set; } = false;
        public List<object> restartTimes { get; private set; } = new List<object>
        {
            "08:00:00",
            "20:00:00"
        };
        public List<object> restartAnnouncementsWhen { get; private set; } = new List<object>
        {
            "12:00:00",
            "11:00:00",
            "10:00:00",
            "09:00:00",
            "08:00:00",
            "07:00:00",
            "06:00:00",
            "05:00:00",
            "04:00:00",
            "03:00:00",
            "02:00:00",
            "01:00:00",
            "00:45:00",
            "00:30:00",
            "00:15:00",
            "00:05:00"
        };
        public bool restartServer { get; private set; } = false;
        #endregion
        //============================================================================================================
        #region Third Party Plugin Support
        public bool doNotOverlayLustyMap { get; private set; } = false;
        public string lustyMapPosition { get; private set; } = "Left";
        #endregion
        #endregion

        private void LoadGUIAnnouncementsConfig()
        {
            announcementDuration = GetConfig("Formatting", "AnnouncementShowDuration", 10f);
            if (announcementDuration == 0)
            {
                PrintWarning("Config AnnouncementShowDuration set to 0, resetting to 10f.");
                Config["Formatting", "AnnouncementShowDuration"] = 10f;
                ConfigUpdated = true;
            }

            welcomeAnnouncementDuration = GetConfig("Formatting", "WelcomeAnnouncementDuration", 20f);
            if (welcomeAnnouncementDuration == 0)
            {
                PrintWarning("Config WelcomeAnnouncementDuration set to 0, resetting to 20f.");
                Config["Formatting", "WelcomeAnnouncementDuration"] = 20f;
                ConfigUpdated = true;
            }

            fontSize = GetConfig("Formatting", "FontSize", 18);
            if (fontSize > 33 | fontSize == 0)
            {
                PrintWarning("Config FontSize greater than 28 or 0, resetting to 18.");
                Config["Formatting", "FontSize"] = 18;
                ConfigUpdated = true;
            }

            fadeInTime = GetConfig("Formatting", "FadeInTime", 0.5f);
            if (fadeInTime > announcementDuration / 2)
            {
                PrintWarning("Config FadeInTime is greater than half of AnnouncementShowDuration, resetting to half of AnnouncementShowDuration.");
                Config["Formatting", "FadeInTime"] = announcementDuration / 2;
                ConfigUpdated = true;
            }

            fadeOutTime = GetConfig("Formatting", "FadeOutTime", 0.5f);
            if (fadeOutTime > announcementDuration / 2)
            {
                PrintWarning("Config FadeOutTime is greater than half of AnnouncementShowDuration, resetting to half of AnnouncementShowDuration.");
                Config["Formatting", "FadeOutTime"] = announcementDuration / 2;
                ConfigUpdated = true;
            }

            automaticTimedAnnouncements = GetConfig("Automatic Announcements", "AutomaticTimedAnnouncements", false);
            automaticTimedAnnouncementsList = GetConfig("Automatic Announcements", "AutomaticTimedAnnouncementsList", automaticTimedAnnouncementsList);
            automaticTimedAnnouncementsRepeat = GetConfig("Automatic Announcements", "AutomaticTimedAnnouncementsRepeat", automaticTimedAnnouncementsRepeat);
            try
            {
                AutomaticTimedAnnouncementsRepeat = TimeSpan.Parse(automaticTimedAnnouncementsRepeat);
            }
            catch (FormatException) { PrintWarning("Config AutomaticTimedAnnouncementsRepeat is not of the correct format ie. HH:MM:SS. Resetting to default"); }
            catch (OverflowException) { PrintWarning("Config AutomaticTimedAnnouncementsRepeat has numbers out of range and should not be higher than: 23:59:59. Resetting to default"); }
            helicopterAnnouncement = GetConfig("Automatic Announcements", "HelicopterAnnouncement", true);
            helicopterDeathAnnouncement = GetConfig("Automatic Announcements", "HelicopterDeathAnnouncement", true);
            helicopterDeathAnnouncementWithKiller = GetConfig("Automatic Announcements", "HelicopterDeathAnnouncementWithKiller", true);
            airdropAnnouncement = GetConfig("Automatic Announcements", "AirdropAnnouncement", true);
            airdropAnnouncementLocation = GetConfig("Automatic Announcements", "AirdropAnnouncementLocation", false);
            welcomeAnnouncement = GetConfig("Automatic Announcements", "WelcomeAnnouncement", true);
            welcomeBackAnnouncement = GetConfig("Automatic Announcements", "WelcomeBackAnnouncement", true);
            newPlayerAnnouncements = GetConfig("Automatic Announcements", "NewPlayerAnnouncements", false);
            newPlayerAnnouncementsShowTimes = GetConfig("Automatic Announcements", "NewPlayerAnnouncementsShowTimes", 4);
            newPlayerAnnouncementsList = GetConfig("Automatic Announcements", "NewPlayerAnnouncementsList", newPlayerAnnouncementsList);
            respawnAnnouncements = GetConfig("Automatic Announcements", "RespawnAnnouncements", false);
            respawnAnnouncementsList = GetConfig("Automatic Announcements", "RespawnAnnouncementsList", respawnAnnouncementsList);
            restartAnnouncements = GetConfig("Automatic Announcements", "RestartAnnouncements", restartAnnouncements);
            restartTimes = GetConfig("Automatic Announcements", "RestartTimes", restartTimes);
            restartAnnouncementsWhen = GetConfig("Automatic Announcements", "RestartAnnouncementsWhen", restartAnnouncementsWhen);
            restartServer = GetConfig("Automatic Announcements", "RestartServer", restartServer);
            doNotOverlayLustyMap = GetConfig("Third Party Plugin Support", "DoNotOverlayLustyMap", false);

            lustyMapPosition = GetConfig("Third Party Plugin Support", "LustyMapPosition", "Left");
            if (lustyMapPosition.ToLower() != "left" && lustyMapPosition.ToLower() != "right" || lustyMapPosition == string.Empty || lustyMapPosition == null)
            {
                PrintWarning("Config LustyMapPosition is not left or right, resetting to left.");
                Config["Third Party Plugin Support", "LustyMapPosition"] = "Left";
                ConfigUpdated = true;
            }

            if (!ConfigUpdated) return;
            Puts("Configuration file has been updated.");
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("A new configuration file has been created.");

        private T GetConfig<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                ConfigUpdated = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            ConfigUpdated = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        private List<string> ConvertList(object value)
        {
            if (value is List<object>)
            {
                List<object> list = (List<object>)value;
                List<string> strings = list.Select(s => (string)s).ToList();
                return strings;
            }
            else { return (List<string>)value; }
        }

        #endregion
        //============================================================================================================
        #region PlayerData

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("GUIAnnouncementsPlayerData", storedData);

        void LoadSavedData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("GUIAnnouncementsPlayerData");
            if (storedData == null)
            {
                PrintWarning("GUIAnnouncement's datafile is null. Recreating data file...");
                storedData = new StoredData();
                SaveData();
                timer.Once(5, () =>
                {
                    PrintWarning("Reloading...");
                    ConsoleSystem.Run.Server.Normal("reload GUIAnnouncements");
                });
            }
        }

        class StoredData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
            public StoredData()
            {
            }
        }

        class PlayerData
        {
            public string Name;
            public string UserID;
            public int TimesJoined;
            public bool Dead;
            public PlayerData()
            {
            }
        }

        void CreatePlayerData(BasePlayer player)
        {
            var Data = new PlayerData();
            Data.Name = player.displayName;
            Data.UserID = player.userID.ToString();
            Data.TimesJoined = 0;
            storedData.PlayerData.Add(player.userID, Data);
            SaveData();
        }

        StoredData storedData;
        void OnServerSave() => SaveData();

        #endregion
        //============================================================================================================
        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
                {
                    {"ChatCommandAnnounce", "announce"},
                    {"ChatCommandAnnounceTo", "announceto"},
                    {"ChatCommandAnnounceTest", "announcetest"},
                    {"ChatCommandDestroyAnnouncement", "destroyannouncement"},
                    {"ChatCommandAnnouncementsToggle", "announcementstoggle" },
                    {"ChatCommandScheduleRestart", "announceschedulerestart" },
                    {"ChatCommandSuspendRestart", "announcesuspendrestart" },
                    {"ChatCommandResumeRestart", "announceresumerestart" },
                    {"ChatCommandGetNextRestart", "announcegetnextrestart" },
                    {"ChatCommandCancelScheduledRestart", "announcecancelscheduledrestart" },
                    {"ChatCommandCancelRestart", "announcecancelrestart" },
                    {"ChatCommandHelp", "announcehelp"},
                    {"ConsoleCommandAnnounce", "announce.announce"},
                    {"ConsoleCommandAnnounceTo", "announce.announceto"},
                    {"ConsoleCommandDestroyAnnouncement", "announce.destroy"},
                    {"ConsoleCommandAnnouncementsToggle", "announce.toggle"},
                    {"ConsoleCommandScheduleRestart", "announce.schedulerestart" },
                    {"ConsoleCommandSuspendRestart", "announce.suspendrestart" },
                    {"ConsoleCommandResumeRestart", "announce.resumerestart" },
                    {"ConsoleCommandGetNextRestart", "announce.getnextrestart" },
                    {"ConsoleCommandCancelScheduledRestart", "announce.cancelscheduledrestart" },
                    {"ConsoleCommandCancelRestart", "announce.cancelrestart" },
                    {"ConsoleCommandHelp", "announce.help"},
                    {"PlayerNotFound", "Player not found, check the name and if they are online."},
                    {"NoPermission", "You do not possess the required permissions."},
                    {"ChatCommandAnnounceUsage", "Usage: /announce <message>."},
                    {"ChatCommandAnnounceToUsage", "Usage: /announceto <player> <message>."},
                    {"ChatCommandAnnouncementsToggleUsage", "Usage: /announcementstoggle [player]."},
                    {"ChatCommandScheduleRestartUsage", "Usage: /announceschedulerestart <hh:mm:ss>." },
                    {"ChatCommandCancelScheduledRestartUsage", "Usage: /announcecancelscheduledrestart" },
                    {"ConsoleCommandAnnounceUsage", "Usage: announce.announce <message>."},
                    {"ConsoleCommandAnnounceToUsage", "Usage: announce.announceto <player> <message>."},
                    {"ConsoleCommandAnnouncementsToggleUsage", "Usage: announce.toggle <player>."},
                    {"ConsoleCommandScheduleRestartUsage", "Usage: announce.schedulerestart <hh:mm:ss>." },
                    {"ConsoleCommandCancelScheduledRestartUsage", "Usage: announce.cancelscheduledrestart." },
                    {"RestartAlreadyScheduled", "A restart has already been scheduled for {time}, please cancel that restart first with /announcecancelscheduledrestart or announce.cancelscheduledrestart" },
                    {"LaterThanNextRestart", "Your time will be scheduled later than the next restart at {time}, please make sure you schedule a restart before the aforementioned time." },
                    {"RestartNotScheduled", "A restart has not been scheduled for you to cancel." },
                    {"ScheduledRestartCancelled", "A manually scheduled restart for {time} has been cancelled." },
                    {"Excluded", "{playername} has been excluded from announcements."},
                    {"ExcludedTo", "You have been excluded from announcements."},
                    {"Included", "{playername} is being included in announcements."},
                    {"IncludedTo", "You are being included in announcements."},
                    {"IsExcluded", "{playername} is currently excluded from announcements."},
                    {"YouAreExcluded", "You are excluded from announcements and cannot see that test announcement"},
                    {"PlayerHelp", "Chat commands: /announcementstoggle"},
                    {"AnnounceHelp", "Chat commands: /announce <message>, /announceto <player> <message>, /announcementstoggle [player], /destroyannouncement, /announcecancelrestart | Console commands: announce.announce <message>, announce.announceto <player> <message>, announce.toggle <player>, announce.destroy, announce.cancelrestart"},
                    {"HelicopterAnnouncement", "Patrol helicopter inbound!"},
                    {"HelicopterDeathAnnouncement", "The patrol helicopter has been taken down!"},
                    {"HelicopterDeathAnnouncementWithPlayer", "{playername} got the last shot on the helicopter taking it down!"},
                    {"AirdropAnnouncement", "Airdrop en route!"},
                    {"AirdropAnnouncementWithLocation", "Airdrop en route to x{x}, z{z}!"},
                    {"WelcomeAnnouncement", "Welcome {playername}!"},
                    {"WelcomeBackAnnouncement", "Welcome back {playername}!"},
                    {"RestartAnnouncementsFormat", "Restarting in {time}."},
                    {"GetNextRestart", "Next restart is in {time1} at {time2}" },
                    {"RestartSuspendedChat", "The next restart at {time} has been suspended. Type /announceresumerestart to resume that restart." },
                    {"RestartSuspendedConsole", "The next restart at {time} has been suspended. Type announce.resumerestart to resume that restart." },
                    {"RestartResumed", "The previously suspended restart at {time} has been resumed." },
                    {"SuspendedRestartPassed", "The previously suspended restart at {time} has passed." },
            }, this);
        }

        #endregion
        //============================================================================================================
        #region Initialization

        void OnServerInitialized()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game.");
            #endif

            LoadGUIAnnouncementsConfig();
            LoadSavedData();
            LoadDefaultMessages();
            permission.RegisterPermission(PermAnnounce, this);
            permission.RegisterPermission(PermAnnounceToggle, this);

            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (!storedData.PlayerData.ContainsKey(activePlayer.userID))
                {
                    CreatePlayerData(activePlayer);
                    storedData.PlayerData[activePlayer.userID].TimesJoined = storedData.PlayerData[activePlayer.userID].TimesJoined + 1;
                    SaveData();
                }
            }
            foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (!storedData.PlayerData.ContainsKey(sleepingPlayer.userID))
                {
                    CreatePlayerData(sleepingPlayer);
                    storedData.PlayerData[sleepingPlayer.userID].TimesJoined = storedData.PlayerData[sleepingPlayer.userID].TimesJoined + 1;
                    SaveData();
                }
            }

            if (automaticTimedAnnouncements)
            {
                List<string> automaticTimedAnnouncementsList = ConvertList(Config.Get("Automatic Announcements", "AutomaticTimedAnnouncementsList"));
                ATALEnum = automaticTimedAnnouncementsList.GetEnumerator();
                AutomaticAnnouncementsTimer = timer.Repeat((float)AutomaticTimedAnnouncementsRepeat.TotalSeconds, 0, () =>
                {
                    AutomaticTimedAnnouncements();
                });
            }

            if (restartAnnouncements)
				RestartAnnouncementsStart();

            cmd.AddChatCommand(Lang("ChatCommandAnnounce"), this, "cmdAnnounce");
            cmd.AddChatCommand(Lang("ChatCommandAnnounceTo"), this, "cmdAnnounceTo");
            cmd.AddChatCommand(Lang("ChatCommandAnnounceTest"), this, "cmdAnnounceTest");
            cmd.AddChatCommand(Lang("ChatCommandDestroyAnnouncement"), this, "cmdDestroyAnnouncement");
            cmd.AddChatCommand(Lang("ChatCommandAnnouncementsToggle"), this, "cmdAnnouncementsToggle");
            cmd.AddChatCommand(Lang("ChatCommandScheduleRestart"), this, "cmdScheduleRestart");
            cmd.AddChatCommand(Lang("ChatCommandSuspendRestart"), this, "cmdSuspendRestart");
            cmd.AddChatCommand(Lang("ChatCommandResumeRestart"), this, "cmdResumeRestart");
            cmd.AddChatCommand(Lang("ChatCommandGetNextRestart"), this, "cmdGetNextRestart");
            cmd.AddChatCommand(Lang("ChatCommandCancelScheduledRestart"), this, "cmdCancelScheduledRestart");
            cmd.AddChatCommand(Lang("ChatCommandCancelRestart"), this, "cmdCancelRestart");
            cmd.AddChatCommand(Lang("ChatCommandHelp"), this, "cmdAnnounceHelp");
            cmd.AddConsoleCommand(Lang("ConsoleCommandAnnounce"), this, "ccmdAnnounce");
            cmd.AddConsoleCommand(Lang("ConsoleCommandAnnounceTo"), this, "ccmdAnnounceTo");
            cmd.AddConsoleCommand(Lang("ConsoleCommandDestroyAnnouncement"), this, "ccmdAnnounceDestroy");
            cmd.AddConsoleCommand(Lang("ConsoleCommandAnnouncementsToggle"), this, "ccmdAnnouncementsToggle");
            cmd.AddConsoleCommand(Lang("ConsoleCommandScheduleRestart"), this, "ccmdScheduleRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandSuspendRestart"), this, "ccmdSuspendRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandResumeRestart"), this, "ccmdResumeRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandGetNextRestart"), this, "ccmdGetNextRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandCancelScheduledRestart"), this, "ccmdCancelScheduledRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandCancelRestart"), this, "ccmdCancelRestart");
            cmd.AddConsoleCommand(Lang("ConsoleCommandHelp"), this, "ccmdAnnounceHelp");
        }
        #endregion
        //============================================================================================================
        #region GUI

        public void CreateMsgGUI(string Msg, string bannerTintColor, string textColor, BasePlayer player = null, bool isWelcomeAnnouncement = false, bool isRestartAnnouncement = false)
        {
            var GUI = new CuiElementContainer();
            GUI.Add(new CuiElement
            {
                Name = AnnouncementGUI,
                Components =
                        {
                            new CuiImageComponent {Color = bannerTintColor, FadeIn = fadeInTime},
                            new CuiRectTransformComponent {AnchorMin = BannerAnchorMinX() + BannerAnchorMinY, AnchorMax = BannerAnchorMaxX() + BannerAnchorMaxY}
                        },
                FadeOut = fadeOutTime
            });
            GUI.Add(new CuiElement
            {
                Name = AnnouncementText,
                Components =
                        {
                             new CuiTextComponent {Text = Msg, FontSize = fontSize, Align = TextAnchor.MiddleCenter, FadeIn = fadeInTime, Color = textColor},
                             new CuiRectTransformComponent {AnchorMin = TextAnchorMinX + TextAnchorMinY, AnchorMax = TextAnchorMaxX + TextAnchorMaxY}
                        },
                FadeOut = fadeOutTime
            });
            if (player == null)
            {
                destroyAllGUI();
                var e = BasePlayer.activePlayerList.GetEnumerator();
                for (var i = 0; e.MoveNext(); i++)
                {
                    if (!Exclusions.ContainsKey(e.Current.userID))
                    {
                        GlobalTimerList.Add(e.Current.userID);
                        CuiHelper.AddUi(e.Current, GUI);
                    }
                    else if (isRestartAnnouncement)
                    {
                        SendReply(e.Current, Msg, e.Current.userID);
                    }
                }
                GlobalTimer = timer.Once(announcementDuration, () => destroyGlobalGUI());
                return;
            }
            if (player != null)
            {
                destroyPrivateGUI(player);
                CuiHelper.AddUi(player, GUI);
                if (JustJoined.Contains(player.userID) && welcomeAnnouncement && isWelcomeAnnouncement)
                {
                    JustJoined.Remove(player.userID);
                    PrivateTimers[player] = timer.Once(welcomeAnnouncementDuration, () => destroyPrivateGUI(player));
                    return;
                }
                PrivateTimers[player] = timer.Once(announcementDuration, () => destroyPrivateGUI(player));
            }
        }

        #endregion
        //============================================================================================================
        #region Functions

        void OnPlayerInit(BasePlayer player)
        {
            if (welcomeAnnouncement || newPlayerAnnouncements || respawnAnnouncements)
            {
                JustJoined.Add(player.userID);
            }
            if (!storedData.PlayerData.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
            }
            if (storedData.PlayerData.ContainsKey(player.userID))
            {
                storedData.PlayerData[player.userID].TimesJoined = storedData.PlayerData[player.userID].TimesJoined + 1;
                SaveData();
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (JustJoined.Contains(player.userID))
            {
                JustJoined.Remove(player.userID);
            }
            NewPlayerPrivateTimers.TryGetValue(player, out NewPlayerTimer);
            if (NewPlayerTimer != null && !NewPlayerTimer.Destroyed)
            {
                NewPlayerTimer.Destroy();
            }
            PlayerRespawnedTimers.TryGetValue(player, out PlayerRespawnedTimer);
            if (PlayerRespawnedTimer != null && !PlayerRespawnedTimer.Destroyed)
            {
                PlayerRespawnedTimer.Destroy();
            }
			if (GlobalTimerList.Contains(player.userID))
			{
				GlobalTimerList.Remove(player.userID);
			}
            destroyPrivateGUI(player);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!storedData.PlayerData.ContainsKey(player.userID))
            {
                CreatePlayerData(player);
                storedData.PlayerData[player.userID].TimesJoined = storedData.PlayerData[player.userID].TimesJoined + 1;
                SaveData();
            }
            if (JustJoined.Contains(player.userID))
            {
                if (welcomeAnnouncement)
                {
                    WelcomeAnnouncement(player);
                    if (!newPlayerAnnouncements && storedData.PlayerData[player.userID].Dead == true && respawnAnnouncements)
                    {
                        storedData.PlayerData[player.userID].Dead = false;
                        timer.Once(welcomeAnnouncementDuration, () => RespawnedAnnouncements(player));
                    }
                }
                if (newPlayerAnnouncements)
                {
                    if (storedData.PlayerData[player.userID].TimesJoined <= newPlayerAnnouncementsShowTimes)
                    {
                        if (welcomeAnnouncement)
                        {
                            timer.Once(welcomeAnnouncementDuration, () => NewPlayerAnnouncements(player));
                        }
                        else
                        {
                            NewPlayerAnnouncements(player);
                        }
                    }
                    else
                    if (storedData.PlayerData[player.userID].Dead == true && respawnAnnouncements)
                    {
                        RespawnedAnnouncements(player);
                        storedData.PlayerData[player.userID].Dead = false;
                    }
                }
                if (!newPlayerAnnouncements && !welcomeAnnouncement && storedData.PlayerData[player.userID].Dead == true && respawnAnnouncements)
                {
                    RespawnedAnnouncements(player);
                    storedData.PlayerData[player.userID].Dead = false;
                }
            }
            else
            if (!JustJoined.Contains(player.userID) && storedData.PlayerData[player.userID].Dead == true && respawnAnnouncements)
            {
                RespawnedAnnouncements(player);
                storedData.PlayerData[player.userID].Dead = false;
            }
            if (!JustJoined.Contains(player.userID) && storedData.PlayerData[player.userID].Dead == true && !welcomeAnnouncement && !newPlayerAnnouncements && respawnAnnouncements)
            {
                RespawnedAnnouncements(player);
                storedData.PlayerData[player.userID].Dead = false;
            }
            if (storedData.PlayerData[player.userID].Dead == true && !respawnAnnouncements)
            {
                storedData.PlayerData[player.userID].Dead = false;
            }
        }

        void destroyAllGUI()
        {
            if (GlobalTimer != null && !GlobalTimer.Destroyed)
            {
                GlobalTimer.Destroy();
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (GlobalTimerList.Contains(player.userID))
                {
                    GlobalTimerList.Remove(player.userID);
                }
                PrivateTimers.TryGetValue(player, out PlayerTimer);
                if (PlayerTimer != null && !PlayerTimer.Destroyed)
                {
                    PlayerTimer.Destroy();
                }
				CuiHelper.DestroyUi(player, AnnouncementGUI);
                CuiHelper.DestroyUi(player, AnnouncementText);
            }
        }

        void destroyGlobalGUI()
        {
			if (GlobalTimer != null && !GlobalTimer.Destroyed)
            {
                GlobalTimer.Destroy();
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (GlobalTimerList.Contains(player.userID))
                {
                    GlobalTimerList.Remove(player.userID);
					CuiHelper.DestroyUi(player, AnnouncementGUI);
                    CuiHelper.DestroyUi(player, AnnouncementText);
                }
            }
        }

        void destroyPrivateGUI(BasePlayer player)
        {
            if (GlobalTimerList.Contains(player.userID))
            {
                GlobalTimerList.Remove(player.userID);
            }
            PrivateTimers.TryGetValue(player, out PlayerTimer);
            if (PlayerTimer != null && !PlayerTimer.Destroyed)
            {
                PlayerTimer.Destroy();
            }
			CuiHelper.DestroyUi(player, AnnouncementGUI);
            CuiHelper.DestroyUi(player, AnnouncementText);
        }

        void Unload()
        {
			destroyAllGUI();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                NewPlayerPrivateTimers.TryGetValue(player, out NewPlayerTimer);
                if (NewPlayerTimer != null && !NewPlayerTimer.Destroyed)
                    NewPlayerTimer.Destroy();
                PlayerRespawnedTimers.TryGetValue(player, out PlayerRespawnedTimer);
                if (PlayerRespawnedTimer != null && !PlayerRespawnedTimer.Destroyed)
                    PlayerRespawnedTimer.Destroy();
            }
            if (SixtySecondsTimer != null && !SixtySecondsTimer.Destroyed)
                SixtySecondsTimer.Destroy();
            if (AutomaticAnnouncementsTimer != null && !AutomaticAnnouncementsTimer.Destroyed)
                AutomaticAnnouncementsTimer.Destroy();
            if (RealTimeTimer != null && !RealTimeTimer.Destroyed)
                RealTimeTimer.Destroy();
            SaveData();
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (helicopterDeathAnnouncementWithKiller && entity is BaseHelicopter && info.Initiator is BasePlayer)
            {
                LastHitPlayer = info.Initiator.ToPlayer().displayName;
            }
        }

        private static BasePlayer FindPlayer(string IDName)
        {
            foreach (BasePlayer targetPlayer in BasePlayer.activePlayerList)
            {
                if (targetPlayer.UserIDString == IDName)
                    return targetPlayer;
                if (targetPlayer.displayName.Contains(IDName, CompareOptions.OrdinalIgnoreCase))
                    return targetPlayer;
            }
            return null;
        }

        private bool hasPermission(BasePlayer player, string perm)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), perm))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return false;
            }
            return true;
        }
		
		void RestartAnnouncementsStart()
		{
            if (RealTimeTimer != null && !RealTimeTimer.Destroyed)
                RealTimeTimer.Destroy();
            List<string> restartTimes = ConvertList(Config.Get("Automatic Announcements", "RestartTimes"));
            RestartTimes = restartTimes.Select(date => DateTime.Parse(date)).ToList();
            RestartAnnouncementsWhenStrings = ConvertList(Config.Get("Automatic Announcements", "RestartAnnouncementsWhen"));
            List<TimeSpan> RestartAnnouncementsWhen = RestartAnnouncementsWhenStrings.Select(date => TimeSpan.Parse(date)).ToList();
            GetNextRestart(RestartTimes);
            RealTimeTimer = timer.Repeat(0.5f, 0, () => RestartAnnouncements(RestartAnnouncementsWhen));
        }
		

        void GetNextRestart(List<DateTime> DateTimes)
        {
            var e = DateTimes.GetEnumerator();
            for (var i = 0; e.MoveNext(); i++)
            {
                if (DateTime.Compare(DateTime.Now, e.Current) < 0)
                {
                    CalcNextRestartDict.Add(e.Current, e.Current.Subtract(DateTime.Now));
                }
                if (DateTime.Compare(DateTime.Now, e.Current) > 0)
                {
                    CalcNextRestartDict.Add(e.Current.AddDays(1), e.Current.AddDays(1).Subtract(DateTime.Now));
                }
            }
            NextRestart = CalcNextRestartDict.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            CalcNextRestartDict.Clear();
            Puts("Next restart is at " + NextRestart.ToLongTimeString());
            Puts("Time until next restart is " + NextRestart.Subtract(DateTime.Now).ToShortString());
        }
		
		string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);

        #endregion
        //============================================================================================================
        #region Auto Announcements

        void RestartAnnouncements(List<TimeSpan> RestartAnnouncementsWhen)
        {
            var currentTime = DateTime.Now;
            if (NextRestart <= currentTime)
            {
                if (RestartSuspended)
                {
                    Puts(Lang("SuspendedRestartPassed").Replace("{time}", NextRestart.ToLongTimeString()));
                    RestartSuspended = false;
                }
                RestartAnnouncementsStart();
                return;
            }
            if (!RestartSuspended)
            {
                TimeSpan timeLeft = NextRestart.Subtract(currentTime);
                string secondsString = String.Empty;
                int hoursLeft = timeLeft.Hours;
                int minutesLeft = timeLeft.Minutes;
                int secondsLeft = timeLeft.Seconds;
                if ((!RestartCountdown && RestartAnnouncementsWhenStrings.Contains(timeLeft.ToShortString()) && ((LastHour != currentTime.Hour) || (LastMinute != currentTime.Minute))) || RestartJustScheduled)
                {
                    string timeLeftString = String.Empty;
                    if (RestartJustScheduled)
                        RestartJustScheduled = false;
                    if (hoursLeft > 0)
                    {
                        timeLeftString = timeLeftString + hoursLeft + " hours";
                        LastHour = currentTime.Hour;
                    }
                    if (hoursLeft == 1)
                    {
                        timeLeftString = timeLeftString + hoursLeft + " hour";
                        LastHour = currentTime.Hour;
                    }
                    if (minutesLeft > 0)
                    {
                        timeLeftString = timeLeftString + minutesLeft + " minutes";
                        LastMinute = currentTime.Minute;
                    }
                    Puts(Lang("RestartAnnouncementsFormat").Replace("{time}", timeLeftString));
                    CreateMsgGUI(Lang("RestartAnnouncementsFormat").Replace("{time}", timeLeftString), BannerTintGrey, TextWhite, null, false, true);
                }
                if (timeLeft <= new TimeSpan(00, 01, 00) && !RestartCountdown)
                {
                    int countDown = timeLeft.Seconds;
                    RestartCountdown = true;
                    CreateMsgGUI(Lang("RestartAnnouncementsFormat").Replace("{time}", countDown.ToString() + " seconds"), BannerTintGrey, TextWhite);
                    SixtySecondsTimer = timer.Repeat(1, countDown + 1, () =>
                        {
                            if (countDown == 1)
                                secondsString = " second";
                            else
                                secondsString = " seconds";
                            CreateMsgGUI(Lang("RestartAnnouncementsFormat").Replace("{time}", countDown.ToString() + secondsString), BannerTintGrey, TextWhite);
                            countDown = countDown - 1;
                            if (countDown == 0 && restartServer)
                            {
                                rust.RunServerCommand("saveall");
                                timer.Once(3, () => rust.RunServerCommand("restart 0"));
                            }
                        });
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (helicopterAnnouncement && entity is BaseHelicopter)
            {
                CreateMsgGUI(Lang("HelicopterAnnouncement"), BannerTintRed, TextOrange);
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (helicopterDeathAnnouncement && entity is BaseHelicopter)
            {
                if (helicopterDeathAnnouncementWithKiller)
                {
                    CreateMsgGUI(Lang("HelicopterDeathAnnouncementWithPlayer").Replace("{playername}", LastHitPlayer), BannerTintRed, TextWhite);
                    LastHitPlayer = String.Empty;
                }
                else
                {
                    CreateMsgGUI(Lang("HelicopterDeathAnnouncement"), BannerTintRed, TextWhite);
                }
            }
            if (entity is BasePlayer)
            {
                if (storedData.PlayerData.ContainsKey(entity.ToPlayer().userID))
                {
                    storedData.PlayerData[entity.ToPlayer().userID].Dead = true;
                    SaveData();
                }
            }
        }

        void OnAirdrop(CargoPlane plane, Vector3 location)
        {
            if (airdropAnnouncement)
            {
                if (airdropAnnouncementLocation)
                {
                    string x = location.x.ToString(), z = location.z.ToString();
                    CreateMsgGUI(Lang("AirdropAnnouncementWithLocation").Replace("{x}", x).Replace("{z}", z), BannerTintGreen, TextYellow);
                }
                else CreateMsgGUI(Lang("AirdropAnnouncement"), BannerTintGreen, TextYellow);
            }
        }

        void WelcomeAnnouncement(BasePlayer player)
        {
            if (welcomeAnnouncement)
            {
                if (welcomeBackAnnouncement && storedData.PlayerData[player.userID].TimesJoined > 1)
                {
                    CreateMsgGUI(Lang("WelcomeBackAnnouncement").Replace("{playername}", player.displayName), BannerTintGrey, TextWhite, player, true);
                }
                else
                {
                    CreateMsgGUI(Lang("WelcomeAnnouncement").Replace("{playername}", player.displayName), BannerTintGrey, TextWhite, player, true);
                }
            }
        }

        void NewPlayerAnnouncements(BasePlayer player)
        {
			if (JustJoined.Contains(player.userID))
            {
                JustJoined.Remove(player.userID);
            }
			List<string> newPlayerAnnouncementsList = ConvertList(Config.Get("Automatic Announcements", "NewPlayerAnnouncementsList"));
			List<string>.Enumerator e = newPlayerAnnouncementsList.GetEnumerator();
			if (storedData.PlayerData[player.userID].Dead == true && respawnAnnouncements)
            {
                PlayerRespawnedTimers[player] = timer.Once(announcementDuration * newPlayerAnnouncementsList.Count, () => RespawnedAnnouncements(player));
                storedData.PlayerData[player.userID].Dead = false;
                SaveData();
            }
            e.MoveNext();
            CreateMsgGUI(e.Current, BannerTintGrey, TextWhite, player);
            NewPlayerPrivateTimers[player] = timer.Repeat(announcementDuration, newPlayerAnnouncementsList.Count - 1, () =>
            {
                e.MoveNext();
                CreateMsgGUI(e.Current, BannerTintGrey, TextWhite, player);
            });
        }

        void RespawnedAnnouncements(BasePlayer player)
        {
            if(JustJoined.Contains(player.userID))
            {
                JustJoined.Remove(player.userID);
            }
            List<string> respawnAnnouncementsList = ConvertList(Config.Get("Automatic Announcements", "RespawnAnnouncementsList"));
            List<string>.Enumerator e = respawnAnnouncementsList.GetEnumerator();
            e.MoveNext();
            CreateMsgGUI(e.Current, BannerTintGrey, TextWhite, player);
            PlayerRespawnedTimers[player] = timer.Repeat(announcementDuration, respawnAnnouncementsList.Count - 1, () =>
            {
                e.MoveNext();
                CreateMsgGUI(e.Current, BannerTintGrey, TextWhite, player);
            });
        }

        void AutomaticTimedAnnouncements()
        {
            if (ATALEnum.MoveNext() == false)
            {
                ATALEnum.Reset();
                ATALEnum.MoveNext();
            }
            CreateMsgGUI(ATALEnum.Current, BannerTintGrey, TextWhite);
        }

        #endregion
        //============================================================================================================
        #region Commands

        void cmdAnnounce(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (args.Length >= 1)
                {
                    string Msg = "";
                    for (int i = 0; i < args.Length; i++)
                        Msg = Msg + " " + args[i];
                    CreateMsgGUI(Msg, BannerTintGrey, TextWhite);
                }
                else SendReply(player, Lang("ChatCommandAnnounceUsage", player.UserIDString));
            }
        }

        void ccmdAnnounce(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                if (arg.Args == null || arg?.Args?.Length <= 0)
                {
                    SendReply(arg, Lang("ConsoleCommandAnnounceUsage"));
                    return;
                }
                if (arg.Args.Length >= 1)
                {
                    string Msg = "";
                    for (int i = 0; i < arg.Args.Length; i++)
                        Msg = Msg + " " + arg.Args[i];
                    CreateMsgGUI(Msg, BannerTintGrey, TextWhite);
                }
            }
        }

        void cmdAnnounceTo(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (args.Length >= 2)
                {
                    string targetPlayer = args[0].ToLower(), Msg = "";
                    for (int i = 1; i < args.Length; i++)
                        Msg = Msg + " " + args[i];
                    BasePlayer targetedPlayer = FindPlayer(targetPlayer);
                    if (targetedPlayer != null)
                    {
                        if (!Exclusions.ContainsKey(targetedPlayer.userID))
                        {
                            CreateMsgGUI(Msg, BannerTintGrey, TextWhite, targetedPlayer);
                        }
                        else SendReply(player, Lang("IsExcluded", player.UserIDString).Replace("{playername}", targetedPlayer.displayName));
                    }
                    else SendReply(player, Lang("PlayerNotFound", player.UserIDString));
                }
                else SendReply(player, Lang("ChatCommandAnnounceToUsage", player.UserIDString));
            }
        }

        void ccmdAnnounceTo(ConsoleSystem.Arg arg, string[] args)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                if (arg.Args == null || arg?.Args?.Length <= 1)
                {
                    SendReply(arg, Lang("ConsoleCommandAnnounceToUsage"));
                    return;
                }
                string targetPlayer = arg.Args[0].ToLower(), Msg = "";
                for (int i = 1; i < arg.Args.Length; i++)
                    Msg = Msg + " " + arg.Args[i];
                BasePlayer targetedPlayer = FindPlayer(targetPlayer);
                if (targetedPlayer != null)
                {
                    if (!Exclusions.ContainsKey(targetedPlayer.userID))
                    {
                        CreateMsgGUI(Msg, BannerTintGrey, TextWhite, targetedPlayer);
                    }
                    else SendReply(arg, Lang("IsExcluded").Replace("{playername}", targetedPlayer.displayName));
                }
                else SendReply(arg, Lang("PlayerNotFound"));
            }
        }

        void cmdAnnounceTest(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (!Exclusions.ContainsKey(player.userID))
                {
                    string Msg = "GUIAnnouncements Test Announcement";
                    CreateMsgGUI(Msg, BannerTintGrey, TextWhite, player);
                }
                else SendReply(player, Lang("YouAreExcluded"), player.displayName);
            }
        }

        void cmdDestroyAnnouncement(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                destroyAllGUI();
            }
        }

        void ccmdAnnounceDestroy(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                destroyAllGUI();
            }
        }

        void cmdAnnouncementsToggle(BasePlayer player, string cmd, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                if (Exclusions.ContainsKey(player.userID))
                {
                    Exclusions.Remove(player.userID);
                    SendReply(player, Lang("IncludedTo", player.UserIDString));
                    return;
                }
                else
                {
                    if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounceToggle) || hasPermission(player, PermAnnounce))
                    {
                        Exclusions.Add(player.userID, player.displayName);
                        SendReply(player, Lang("ExcludedTo", player.UserIDString));
                    }
                }
            }
            if (args.Length > 0)
            {
                if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
                {
                    string targetPlayer = args[0].ToLower();
                    ulong targetPlayerUID64; ulong.TryParse(targetPlayer, out targetPlayerUID64);
                    BasePlayer targetedPlayer = FindPlayer(targetPlayer);
                    var GetKey = Exclusions.FirstOrDefault(x => x.Value.Contains(targetPlayer, CompareOptions.OrdinalIgnoreCase)).Key;
                    if (Exclusions.ContainsKey(GetKey) || Exclusions.ContainsKey(targetPlayerUID64))
                    {
                        string PlayerName = Exclusions[GetKey];
                        Exclusions.Remove(GetKey); Exclusions.Remove(targetPlayerUID64);
                        SendReply(player, Lang("Included", player.UserIDString).Replace("{playername}", PlayerName));
                        if (targetedPlayer != null)
                        {
                            SendReply(targetedPlayer, Lang("IncludedTo", targetedPlayer.UserIDString));
                        }
                    }
                    else
                    if (targetedPlayer != null)
                    {
                        Exclusions.Add(targetedPlayer.userID, targetedPlayer.displayName);
                        SendReply(player, Lang("Excluded", player.UserIDString).Replace("{playername}", targetedPlayer.displayName));
                        SendReply(targetedPlayer, Lang("ExcludedTo", targetedPlayer.UserIDString));
                    }
                    else SendReply(player, Lang("PlayerNotFound", player.UserIDString));
                }
            }
        }

        void ccmdAnnouncementsToggle(ConsoleSystem.Arg arg, string[] args)
        {
            if (arg?.Args?.Length > 0)
            {
                if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
                {
                    string targetPlayer = arg.Args[0].ToLower();
                    ulong targetPlayerUID64; ulong.TryParse(targetPlayer, out targetPlayerUID64);
                    BasePlayer targetedPlayer = FindPlayer(targetPlayer);
                    var GetKey = Exclusions.FirstOrDefault(x => x.Value.Contains(targetPlayer, CompareOptions.OrdinalIgnoreCase)).Key;
                    if (Exclusions.ContainsKey(GetKey) || Exclusions.ContainsKey(targetPlayerUID64))
                    {
                        string PlayerName = Exclusions[GetKey];
                        Exclusions.Remove(GetKey); Exclusions.Remove(targetPlayerUID64);
                        SendReply(arg, Lang("Included").Replace("{playername}", PlayerName));
                        if (targetedPlayer != null)
                        {
                            SendReply(targetedPlayer, Lang("IncludedTo", targetedPlayer.UserIDString));
                        }
                    }
                    else
                        if (targetedPlayer != null)
                    {
                        Exclusions.Add(targetedPlayer.userID, targetedPlayer.displayName);
                        SendReply(arg, Lang("Excluded").Replace("{playername}", targetedPlayer.displayName));
                        SendReply(targetedPlayer, Lang("ExcludedTo", targetedPlayer.UserIDString));
                    }
                    else SendReply(arg, Lang("PlayerNotFound"));
                }
            }
            else SendReply(arg, Lang("ConsoleCommandAnnouncementsToggleUsage"));
        }

        void cmdScheduleRestart(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (args.Length == 1)
                {
                    if (!RestartCountdown)
                    {
                        if (!RestartScheduled)
                        {
                            var currentTime = DateTime.Now;
                            TimeSpan scheduleRestart;
                            if (TimeSpan.TryParse(args[0], out scheduleRestart))
                            {
                                if (restartAnnouncements && currentTime.Add(scheduleRestart) < NextRestart)
                                {
                                    Puts("Restart scheduled in " + scheduleRestart.ToShortString());
                                    RestartTimes.Add(currentTime.Add(scheduleRestart + new TimeSpan(00, 00, 01)));
                                    ScheduledRestart = currentTime.Add(scheduleRestart + new TimeSpan(00, 00, 01));
                                    RestartScheduled = true;
                                    RestartJustScheduled = true;
                                    GetNextRestart(RestartTimes);
                                }
                                else SendReply(player, Lang("LaterThanNextRestart", player.UserIDString).Replace("{time}", NextRestart.ToShortTimeString()));
                            }
                            else SendReply(player, Lang("ChatCommandScheduleRestartUsage", player.UserIDString));
                        }
                        else SendReply(player, Lang("RestartAlreadyScheduled").Replace("{time}", NextRestart.ToShortTimeString()));
                    }
                }
                else SendReply(player, Lang("ChatCommandScheduleRestartUsage", player.UserIDString));
            }
        }

        void ccmdScheduleRestart(ConsoleSystem.Arg arg, string[] args)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                if (arg?.Args?.Length == 1)
                {
                    if (!RestartCountdown)
                    {
                        if (!RestartScheduled)
                        {
                            var currentTime = DateTime.Now;
                            TimeSpan scheduleRestart; TimeSpan.TryParse(arg.Args[0], out scheduleRestart);
                            if (restartAnnouncements && currentTime.Add(scheduleRestart) < NextRestart)
                            {
                                Puts("Restart scheduled in" + scheduleRestart.ToShortString());
                                RestartTimes.Add(currentTime.Add(scheduleRestart + new TimeSpan(00, 00, 01)));
                                ScheduledRestart = currentTime.Add(scheduleRestart + new TimeSpan(00, 00, 01));
                                RestartScheduled = true;
                                RestartJustScheduled = true;
                                GetNextRestart(RestartTimes);
                            }
                            else SendReply(arg, Lang("LaterThanNextRestart").Replace("{time}", NextRestart.ToShortTimeString()));
                        }
                        else SendReply(arg, Lang("RestartAlreadyScheduled").Replace("{time}", NextRestart.ToShortTimeString()));
                    }
                }
                else SendReply(arg, Lang("ChatCommandScheduleRestartUsage"));
            }
        }

        void cmdCancelScheduledRestart(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (RestartScheduled)
                {
                    RestartTimes.Remove(ScheduledRestart);
                    GetNextRestart(RestartTimes);
                    Puts(Lang("ScheduledRestartCancelled").Replace("{time}", ScheduledRestart.ToShortTimeString()));
                    SendReply(player, (Lang("ScheduledRestartCancelled", player.UserIDString).Replace("{time}", ScheduledRestart.ToShortTimeString())));
                }
                else SendReply(player, Lang("RestartNotScheduled", player.UserIDString));
            }
        }

        void ccmdCancelScheduledRestart(ConsoleSystem.Arg arg, string cmd)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                if (RestartScheduled)
                {
                    RestartTimes.Remove(ScheduledRestart);
                    GetNextRestart(RestartTimes);
                    SendReply(arg, (Lang("ScheduledRestartCancelled").Replace("{time}", ScheduledRestart.ToShortTimeString())));
                }
                else SendReply(arg, Lang("RestartNotScheduled"));
            }
        }

        void cmdSuspendRestart(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                RestartSuspended = true;
                SendReply(player, Lang("RestartSuspendedChat", player.UserIDString).Replace("{time}", NextRestart.ToLongTimeString()));
            }
        }

        void ccmdSuspendRestart(ConsoleSystem.Arg arg, string cmd)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                RestartSuspended = true;
                SendReply(arg, Lang("RestartSuspendedConsole").Replace("{time}", NextRestart.ToLongTimeString()));
            }
        }

        void cmdResumeRestart(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                RestartSuspended = false;
                SendReply(player, Lang("RestartResumed", player.UserIDString).Replace("{time}", NextRestart.ToLongTimeString()));
            }
        }

        void ccmdResumeRestart(ConsoleSystem.Arg arg, string cmd)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                RestartSuspended = false;
                SendReply(arg, Lang("RestartResumed").Replace("{time}", NextRestart.ToLongTimeString()));
            }
        }

        void cmdGetNextRestart(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                var timeLeft = NextRestart.Subtract(DateTime.Now);
                SendReply(player, Lang("GetNextRestart", player.UserIDString).Replace("{time1}", timeLeft.ToShortString()).Replace("{time2}", NextRestart.ToLongTimeString()));
            }
        }

        void ccmdGetNextRestart(ConsoleSystem.Arg arg, string cmd)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                var timeLeft = NextRestart.Subtract(DateTime.Now);
                SendReply(arg, Lang("GetNextRestart").Replace("{time1}", timeLeft.ToShortString()).Replace("{time2}", NextRestart.ToLongTimeString()));
            }
        }

        void cmdCancelRestart(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                if (SixtySecondsTimer != null && !SixtySecondsTimer.Destroyed)
                {
                    SixtySecondsTimer.Destroy();
                    SendReply(player, Lang("RestartTimerCanceled", player.UserIDString));
                    PrintWarning(Lang("RestartTimeCanceled"));
					timer.Once(60, () => RestartCountdown = false);
                }
            }
        }

        void ccmdCancelRestart(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                if (SixtySecondsTimer != null && !SixtySecondsTimer.Destroyed)
                {
                    SixtySecondsTimer.Destroy();
                    SendReply(arg, Lang("RestartTimerCanceled"));
                    PrintWarning(Lang("RestartTimeCanceled"));
					timer.Once(60, () => RestartCountdown = false);
                }
            }
        }

        void cmdAnnounceHelp(BasePlayer player, string cmd)
        {
            if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounce))
            {
                SendReply(player, Lang("AnnounceHelp", player.UserIDString));
            }
            else
                if (player.net.connection.authLevel > 0 || hasPermission(player, PermAnnounceToggle))
            {
                SendReply(player, Lang("PlayerHelp", player.UserIDString));
            }
        }

        void ccmdAnnounceHelp(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounce))
            {
                SendReply(arg, Lang("AnnounceHelp"));
            }
            else
                if (arg.isAdmin || hasPermission(arg.connection.player as BasePlayer, PermAnnounceToggle))
            {
                SendReply(arg, Lang("PlayerHelp"));
            }
        }
        #endregion
    }
}