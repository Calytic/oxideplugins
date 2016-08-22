using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Game.Rust.Libraries;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Notifier", "SkinN", "3.1.3", ResourceId = 797)]
    [Description("Server administration tool with chat based notifications")]

    class Notifier : RustPlugin
    {
        #region Plugin Variables

        // Developer Variables
        private static bool Dev = false;
        private static string Seperator = string.Join("-", new string[50 + 1]);
        private static string DataFile = "Notifier_PlayersData_v2";

        // Global Variables
        private Dictionary<string, PlayerData> PlayersData;
        private System.Random AdvertsRandom = new System.Random();
        private int LastAdvert = 0;
        private List<string> WM_Queue = new List<string>();
        private Dictionary<string, object> Rules;
        private Dictionary<string, string> Countries = new Dictionary<string, string>
        {
            { "EN", "English" },
            { "PT", "Portuguese" },
            { "ES", "Spanish" },
            { "FR", "French" },
            { "DE", "German" },
            { "TR", "Turk" },
            { "IT", "Italian" },
            { "DK", "Danish" },
            { "RU", "Russian" },
            { "UA", "Ukrainian" },
            { "NL", "Dutch" },
            { "RO", "Romanian" },
            { "HU", "Hungarian" },
            { "JP", "Japanese" },
            { "CZ", "Czech" }            
        };
        private List<string> HTMLColors = new List<string> {
            "white",
            "silver",
            "gray",
            "black",
            "red",
            "maroon",
            "yellow",
            "orange",
            "olive",
            "lime",
            "green",
            "aqua",
            "teal",
            "blue",
            "navy",
            "fuchsia",
            "purple",
            "cyan",
            "grey",
            "lightblue",
            "dimgrey",
            "lightgreen",
            "pink",
            "magenta"
        };
        private class PlayerData
        {
            public bool IsAdmin = false;
            public string SteamID = "STEAMID_UNKNOWN";
            public string IpAddress = "Unknown";
            public string Country = "Unknown";
            public string Name = "Unknown";
            public string Code = "Unknown";

            public PlayerData() { }

            internal PlayerData(BasePlayer player)
            {
                SteamID = player.UserIDString;
                this.Update(player);
            }

            internal void Update(BasePlayer player, bool joined = true)
            {
                Name = player.displayName;
                IpAddress = player.net.connection.ipaddress.Split(':')[0];
                IsAdmin = player.IsAdmin();
            }
        }

        // Supported Plugins
        Plugin BetterChat;

        #endregion Plugin Variables

        #region Configuration

        private bool BroadcastToConsole;
        private bool EnablePluginPrefix;
        private bool EnableLineSperators;
        private bool EnablePluginIcon;
        private bool EnableJoinMessage;
        private bool EnableLeaveMessage;
        private bool EnableAdvertMessages;
        private bool EnableScheduledMessages;
        private bool EnableWelcomeMessage;
        private bool EnableBetterChatSupport;
        private bool EnableHelpTextSupport;
        private bool HideAdmins;
        private bool NotifyHelicopter;
        private bool NotifyAirdrop;
        private string PluginPrefix;
        private string TimeFormat;
        private string DateFormat;
        private string RulesLanguage;
        private string IconProfile;
        private int AdvertsInterval;

        private T GetConfig<T>(params object[] args)
        {
            string[] stringArgs = GetConfigPath(args);
            if (Config.Get(stringArgs) == null)
                Config.Set(args);
            return (T)Convert.ChangeType(Config.Get(stringArgs), typeof(T));
        }

        private void SetConfig(params object[] args)
        {
            string[] stringArgs = GetConfigPath(args);
            if (Config.Get(stringArgs) == null)
                Config.Set(args);
        }

        private List<string> ConvertList(object value)
        {
            if (value is List<object>)
            {
                List<object> list = (List<object>) value;
                List<string> strings = list.Select(s => (string) s).ToList();
                return strings;
            }
            else { return (List<string>) value; }
        }

        private string[] GetConfigPath(params object[] args)
        {
            string[] stringArgs = new string[args.Length - 1];
            for (var i = 0; i < args.Length - 1; i++)
                stringArgs[i] = args[i].ToString();
            return stringArgs;
        }

        private string GetMsg(string key) { return lang.GetMessage(key, this, null); }

        protected override void LoadDefaultConfig() { }

        private void LoadVariables()
        {
            // Reset configuration on developer mode
            if (Dev)
                Config.Clear();

            // General Settings
            string GS = "General Settings";

            AdvertsInterval = GetConfig<int>(GS, "Adverts Interval (In Minutes)", 15);
            PluginPrefix = GetConfig<string>(GS, "Plugin Prefix", "<white>[ <cyan>NOTIFIER<end> ]<end>");
            IconProfile = GetConfig<string>(GS, "Icon Profile", "76561198235146288");
            RulesLanguage = GetConfig<string>(GS, "Rules Default Language", "auto").ToUpper();
            TimeFormat = GetConfig<string>(GS, "Time Format", "{hour}:{minute}:{second}");
            DateFormat = GetConfig<string>(GS, "Date Format", "{day}/{month}/{year}");
            EnablePluginPrefix = GetConfig<bool>(GS, "Enable Plugin Prefix", true);
            BroadcastToConsole = GetConfig<bool>(GS, "Broadcast To Console", true);
            EnablePluginIcon = GetConfig<bool>(GS, "Enable Plugin Icon", false);
            EnableJoinMessage = GetConfig<bool>(GS, "Enable Join Message", true);
            EnableLeaveMessage = GetConfig<bool>(GS, "Enable Leave Message", true);
            EnableAdvertMessages = GetConfig<bool>(GS, "Enable Advert Messages", true);
            EnableScheduledMessages = GetConfig<bool>(GS, "Enable Scheduled Messages", false);
            EnableWelcomeMessage = GetConfig<bool>(GS, "Enable Welcome Message", true);
            EnableLineSperators = GetConfig<bool>(GS, "Enable Line Separators", true);
            NotifyHelicopter = GetConfig<bool>(GS, "Notify Incoming Helicopter", true);
            NotifyAirdrop = GetConfig<bool>(GS, "Notify Incoming Airdrop", false);
            HideAdmins = GetConfig<bool>(GS, "Hide Admins", false);
            EnableBetterChatSupport = GetConfig<bool>(GS, "Enable BetterChat Support", false);
            EnableHelpTextSupport = GetConfig<bool>(GS, "Enable HelpText Support", true);

            // Welcome Message
            SetConfig("Welcome Messages", new List<object>
                {
                    "<size=20>Welcome <lightblue>{player.name}<end></size> (Level: {player.level})",
                    "<orange><size=20>â¢</size><end> Type <orange>help<end> or <orange>/notifier help<end> for all available commands",
                    "<orange><size=20>â¢</size><end> Please respect the server <orange>/rules<end>",
                    "<orange><size=20>â¢</size><end> Check our live map at <lime>http://{server.ip}:{server.port}<end>"
                }
            );

            // Advert Messages
            SetConfig("Advert Messages", new List<object>
                {
                    "Please be respectful with all the other players",
                    "Cheating or abusing of game exploits may result in a <red>permanent<end> ban.",
                    "You are playing on: <orange>{server.hostname}<end>",
                    "There are <orange>{players.active}<silver>/<end>{server.maxplayers} <silver>players playing in the server, and<end> {players.sleepers}<end> sleepers.",
                    "Check <cyan>Notifier's<end> commands with <orange>/notifier help<end> command."
                }
            );

            // Scheduled Messages
            SetConfig("Schedule Messages", new Dictionary<string, object>
                {
                    {
                        "00:46", new List<object> {
                            "It is now <orange>{localtime.now}<end> (Server local time)"
                        }
                    },
                    {
                        "00:47", new List<object> {
                            "It is now <orange>{localtime.date} {localtime.now}<end> (Server local time)"
                        }
                    }
                }
            );

            // Server Rules
            SetConfig("Rules", "EN", new List<object> {
                    "Cheating is strictly prohibited",
                    "Respect all players",
                    "Don't spam the chat",
                    "Do not abuse of bugs or exploits"
                }
            );
            SetConfig("Rules", "PT", new List<object> {
                    "Usar cheats Ã© estritamente proibido",
                    "Respeita todos os jogadores",
                    "NÃ£o spames o chat",
                    "NÃ£o abuses de bugs ou exploits"
                }
            );
            SetConfig("Rules", "FR", new List<object> {
                    "Tricher est strictement interdit.",
                    "Respectez tous les joueurs.",
                    "Ãvitez le spam dans le chat.",
                    "Jouer juste et ne pas abuser des bugs / exploits."
                }
            );
            SetConfig("Rules", "ES", new List<object> {
                    "Los trucos estÃ¡n terminantemente prohibidos.",
                    "Respeta a todos los jugadores.",
                    "Evita el Spam en el chat.",
                    "Juega limpio y no abuses de bugs/exploits."
                }
            );
            SetConfig("Rules", "DE", new List<object> {
                    "Cheaten ist verboten!",
                    "Respektiere alle Spieler",
                    "Spam im Chat zu vermeiden.",
                    "Spiel fair und missbrauche keine Bugs oder Exploits."
                }
            );
            SetConfig("Rules", "TR", new List<object> {
                    "Hile kesinlikle yasaktÄ±r.",
                    "TÃ¼m oyuncular SaygÄ±.",
                    "Sohbet Spam kaÃ§Ä±nÄ±n.",
                    "Adil oynayÄ±n ve bÃ¶cek / aÃ§Ä±klarÄ± kÃ¶tÃ¼ye yok."
                }
            );
            SetConfig("Rules", "IT", new List<object> {
                    "Cheating Ã¨ severamente proibito.",
                    "Rispettare tutti i giocatori.",
                    "Evitare lo spam in chat.",
                    "Fair Play e non abusare di bug / exploit."
                }
            );
            SetConfig("Rules", "DK", new List<object> {
                    "Snyd er strengt forbudt.",
                    "Respekter alle spillere.",
                    "UndgÃ¥ spam i chatten.",
                    "Spil fair og misbrug ikke bugs / exploits."
                }
            );
            SetConfig("Rules", "RU", new List<object>{
                    "ÐÐ°Ð¿ÑÐµÑÐµÐ½Ð¾ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°ÑÑ ÑÐ¸ÑÑ.",
                    "ÐÐ°Ð¿ÑÐµÑÐµÐ½Ð¾ ÑÐ¿Ð°Ð¼Ð¸ÑÑ Ð¸ Ð¼Ð°ÑÐµÑÐ¸ÑÑÑÑ.",
                    "Ð£Ð²Ð°Ð¶Ð°Ð¹ÑÐµ Ð´ÑÑÐ³Ð¸Ñ Ð¸Ð³ÑÐ¾ÐºÐ¾Ð².",
                    "ÐÐ³ÑÐ°Ð¹ÑÐµ ÑÐµÑÑÐ½Ð¾ Ð¸ Ð½Ðµ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·ÑÐ¹ÑÐµ Ð±Ð°Ð³Ð¸ Ð¸ Ð»Ð°Ð·ÐµÐ¹ÐºÐ¸."
                }
            );
            SetConfig("Rules", "UA", new List<object> {
                    "ÐÐ±Ð¼Ð°Ð½ ÑÑÐ²Ð¾ÑÐ¾ Ð·Ð°Ð±Ð¾ÑÐ¾Ð½ÐµÐ½Ð¾.",
                    "ÐÐ¾Ð²Ð°Ð¶Ð°Ð¹ÑÐµ Ð²ÑÑÑ Ð³ÑÐ°Ð²ÑÑÐ²",
                    "Ð©Ð¾Ð± ÑÐ½Ð¸ÐºÐ½ÑÑÐ¸ ÑÐ¿Ð°Ð¼Ñ Ð² ÑÐ°ÑÑ.",
                    "ÐÑÐ°ÑÐ¸ ÑÐµÑÐ½Ð¾ Ñ Ð½Ðµ Ð·Ð»Ð¾Ð²Ð¶Ð¸Ð²Ð°ÑÐ¸ Ð¿Ð¾Ð¼Ð¸Ð»ÐºÐ¸ / Ð¿Ð¾Ð´Ð²Ð¸Ð³Ð¸."
                }
            );
            SetConfig("Rules", "NL", new List<object> {
                    "Vals spelen is ten strengste verboden.",
                    "Respecteer alle spelers",
                    "Vermijd spam in de chat.",
                    "Speel eerlijk en maak geen misbruik van bugs / exploits."
                }
            );
            SetConfig("Rules", "RO", new List<object> {
                    "Cheaturile sunt strict interzise!",
                    "RespectaÈi toÈi jucÄtorii!",
                    "EvitaÈi spamul Ã®n chat!",
                    "JucaÈi corect Èi nu abuzaÈi de bug-uri/exploituri!"
                }
            );
            SetConfig("Rules", "HU", new List<object> {
                    "CsalÃ¡s szigorÃºan tilos.",
                    "Tiszteld minden jÃ¡tÃ©kostÃ¡rsad.",
                    "KerÃ¼ld a spammolÃ¡st a chaten.",
                    "JÃ¡tssz tisztessÃ©gesen Ã©s nem Ã©lj vissza a hibÃ¡kkal."
                }
            );
            SetConfig("Rules", "JP", new List<object> {
                    "ãã¼ãè¡çºã¯åºãç¦ãã¦ããã¾ãã",
                    "å¨ã¦ã®ãã¬ã¤ã¤ã¼ã«æ¬æãæã£ã¦ä¸ããã",
                    "ãã£ããã§ã¹ãã è¡çºã¯ããªãã§ä¸ããã",
                    "ãã°ã®æªç¨è¡çºãå¬å¹³ãªãã¬ã¤ã¯ããªãã§ä¸ãã"
                }
            );
            SetConfig("Rules", "CZ", new List<object> {
                    "CheatovÃ¡nÃ­ je pÅÃ­snÄ zakÃ¡zÃ¡no.",
                    "Respektuj a neurÃ¡Å¾ej ostatnÃ­ hrÃ¡Äe.",
                    "Nespamuj chat zbyteÄnÄ.",
                    "Hraj fair play a nezneuÅ¾Ã­vej bugy/exploity."
                }
            );

            Rules = (Dictionary<string, object>) Config.Get("Rules");

            // Commands Section
            string cmds = "Commands";

            SetConfig(cmds, "Triggers", "Players List", new List<string> { "/players" });
            SetConfig(cmds, "Triggers", "Plugins List", new List<string> { "/plugins" });
            SetConfig(cmds, "Triggers", "Admins List", new List<string> { "/admins" });
            SetConfig(cmds, "Triggers", "Players Count", new List<string> { "/online" });
            SetConfig(cmds, "Triggers", "Server Map", new List<string> { "/map" });
            SetConfig(cmds, "Triggers", "Server Rules", new List<string> { "/rules" });

            SetConfig(cmds, "Settings", "Players List", true);
            SetConfig(cmds, "Settings", "Plugins List", false);
            SetConfig(cmds, "Settings", "Admins List", false);
            SetConfig(cmds, "Settings", "Server Rules", true);
            SetConfig(cmds, "Settings", "Server Map", false);
            SetConfig(cmds, "Settings", "Players Count", false);

            // Save file
            SaveConfig();
        }

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Join Notification", "<lightblue>{player.name}<end> joined from {player.country}" },
                { "Leave Notification", "<lightblue>{player.name}<end> disconnected. (Reason: {reason})" },
                { "Players List Description", "Shows a list of all online players" },
                { "Plugins List Description", "Shows a list of all the server plugins" },
                { "Admins List Description", "Shows a list of all online Admins" },
                { "Server Rules Description", "Shows server rules" },
                { "Server Map Description", "Shows the link of this server live map" },
                { "Players Count Description", "Shows the a count of all active players, sleepers and Admins" },
                { "Server Map Message", "Check our live map at: <yellow>{server.ip}:{server.port}<end>" },
                { "No Admins Online", "There are no <cyan>Admins<end> online" },
                { "Admins List Title", "Admins Online" },
                { "Plugins List Title", "Plugins List" },
                { "Players List Title", "Players List" },
                { "Server Rules Title", "Server Rules" },
                { "Rules Languages List Title", "Available Rules Languages" },
                { "Incoming Airdrop", "<yellow>Airdrop <silver>incoming, drop coordinates are:<end> {x}, {y}, {z}<end>."},
                { "Incoming Helicopter", "<yellow>Patrol Helicopter<end> incoming!" },
                { "Players Count Message", "There are <orange>{players.active}<silver>/<end>{server.maxplayers}<end> <silver>players online, and <orange>{players.sleepers}<end> sleepers<end>" }
            }, this);
        }

        #endregion Configuration

        #region Chat System

        private void Con(string msg) { if (BroadcastToConsole || Dev) { Puts(SimpleColorFormat(msg, true)); } }

        private void Debug(string text) { if (Dev) { Con(text); } }

        private void Say(string msg, string profile = "0", bool usePrefix = true)
        {
            Con(msg);
            if (!String.IsNullOrEmpty(PluginPrefix) && EnablePluginPrefix && usePrefix)
                msg = PluginPrefix + " " + msg;
            if (profile == "0" && EnablePluginIcon)
                profile = IconProfile;
            rust.BroadcastChat(SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        private void Tell(BasePlayer player, string msg, string profile = "0", bool usePrefix = true)
        {
            if (!String.IsNullOrEmpty(PluginPrefix) && EnablePluginPrefix && usePrefix)
                msg = PluginPrefix + " " + msg;
            if (profile == "0" && EnablePluginIcon)
                profile = IconProfile;
            rust.SendChatMessage(player, SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        public string SimpleColorFormat(string text, bool removeTags = false)
        {
            /*  Simple Color Format ( v3.0 ) */

            Regex end = new Regex(@"\<(end?)\>");
            Regex hex = new Regex(@"\<(#\w+?)\>");
            Regex names = new Regex(@"\<(\w+?)\>");
            Regex size = new Regex(@"\<size=(\w+?)\>");
            Regex closeSize = new Regex(@"\<(/size?)\>");

            if (removeTags)
            {   
                text = end.Replace(text, "");
                text = names.Replace(text, "");
                text = hex.Replace(text, "");
                text = size.Replace(text, "");
                text = closeSize.Replace(text, "");
            }
            else
            {   
                foreach (Match i in names.Matches(text))
                {
                    string x = i.ToString().Replace("<", "").Replace(">", "");
                    if (HTMLColors.Contains(x.ToLower()))
                        text = text.Replace("<" + x + ">", "<color=" + x.ToLower() + ">");
                }
                text = end.Replace(text, "</color>");
                text = hex.Replace(text, "<color=$1>");
            }
            return text;
        }

        #endregion Chat System

        #region Plugin Hooks

        void Loaded()
        {
            LoadVariables();
            LoadMessages();

            try
            {
                PlayersData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>(DataFile);
            }
            catch { PlayersData = new Dictionary<string, PlayerData>(); }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                InitializePlayer(player, false);

            if (EnableAdvertMessages)
                timer.Repeat(AdvertsInterval * 60, 0, () => AdvertsLoop());

            if (EnableScheduledMessages)
                timer.Once(60 - DateTime.Now.Second, () => CheckScheduledMessages());
                timer.Once(60 - DateTime.Now.Second, () => timer.Repeat(60, 0, () => CheckScheduledMessages()));

            // Plugin Commands
            var command = Oxide.Core.Interface.Oxide.GetLibrary<Command>();
            Dictionary<string, object> Settings = (Dictionary<string, object>) Config.Get("Commands", "Settings");
            foreach (var cmd in Settings)
            {
                if ((bool) cmd.Value)
                {
                    List<string> triggers = ConvertList(Config.Get("Commands", "Triggers", cmd.Key));
                    foreach (string item in triggers)
                        command.AddChatCommand(item.Replace("/", String.Empty), this, cmd.Key.Replace(" ", string.Empty) + "_Command");
                }
            }
        }

        void Unload() { Interface.Oxide.DataFileSystem.WriteObject(DataFile, PlayersData); }

        void SendHelpText(BasePlayer player) { if (EnableHelpTextSupport) { Plugin_Command(player, "/notifier", new string[] {"help"}); } }

        #endregion Plugin Hooks

        #region Plugin Methods

        private void InitializePlayer(BasePlayer player, bool joined = true)
        {
            string uid = player.UserIDString;
            if (!PlayersData.ContainsKey(uid))
                PlayersData.Add(uid, new PlayerData(player));
            else
                PlayersData[uid].Update(player, joined);
            PlayerData ply = PlayersData[uid];
            if (ply.Country == "Unknown" || ply.Code == "Unknown")
                webrequest.EnqueueGet("http://ip-api.com/json/" + ply.IpAddress + "?fields=3", (code, response) => WebRequestFilter(code, response, player, joined), this);
            else if (joined)
                JoinMessages(player);
        }

        private void WebRequestFilter(int code, string response, BasePlayer player, bool joined)
        {
            string uid = player.UserIDString;
            if (PlayersData.ContainsKey(uid))
            {
                if (response != null || code == 200)
                {
                        var json = JObject.Parse(response);
                        string _country = json["country"].ToString();
                        string _code = json["countryCode"].ToString();
                        if (!String.IsNullOrEmpty(_country))
                            PlayersData[uid].Country = _country;
                        if (!String.IsNullOrEmpty(_code))
                            PlayersData[uid].Code = _code;
                }
                if (joined)
                    JoinMessages(player);
            }
        }

        private void JoinMessages(BasePlayer player)
        {
            if (EnableJoinMessage && NotHide(player))
            {
                Say(ReplaceNameFormats(GetMsg("Join Notification"), player));
                Tell(player, string.Join("\n", new string[60 + 1]), usePrefix: false);
            }
            if (EnableWelcomeMessage)
            {
                string uid = player.UserIDString;
                if (!WM_Queue.Contains(uid))
                    WM_Queue.Add(uid);
                if (Dev)
                    OnPlayerSleepEnded(player);
            }
        }

        private void AdvertsLoop()
        {
            List<string> Adverts = ConvertList(Config.Get("Advert Messages"));
            int index = LastAdvert;
            if (Adverts.Count > 1)
            {
                while (index == LastAdvert)
                    index = AdvertsRandom.Next(Adverts.Count);
            }
            LastAdvert = index;
            Say(ReplaceNameFormats((string) Adverts[index]));
        }

        private void CheckScheduledMessages()
        {
            DateTime Now = DateTime.Now;
            string cur = Pads(Now.Hour.ToString()) + ":" + Pads(Now.Minute.ToString());
            Dictionary<string, object> ScheduleMessages = (Dictionary<string, object>) Config.Get("Schedule Messages");
            if (ScheduleMessages.ContainsKey(cur))
            {
                foreach (string line in ConvertList(ScheduleMessages[cur]))
                    Say(ReplaceNameFormats(line));
            }
        }

        private string ReplaceNameFormats(string text, BasePlayer player = null)
        {
            int Active = BasePlayer.activePlayerList.Count;
            int Sleepers = BasePlayer.sleepingPlayerList.Count;
            DateTime Now = DateTime.Now;
            string time = TimeFormat.Replace("{hour}", Pads(Now.Hour.ToString())).Replace("{minute}", Pads(Now.Minute.ToString())).Replace("{second}", Pads(Now.Second.ToString()));
            string date = DateFormat.Replace("{year}", Now.Year.ToString()).Replace("{month}", Pads(Now.Month.ToString())).Replace("{day}", Pads(Now.Day.ToString()));

            Dictionary<string, object> Dict = new Dictionary<string, object> {
                { "{server.ip}", (String.IsNullOrEmpty(ConVar.Server.ip)) ? "localhost" : ConVar.Server.ip },
                { "{server.port}", ConVar.Server.port },
                { "{server.hostname}", ConVar.Server.hostname },
                { "{server.description}", ConVar.Server.description},
                { "{server.maxplayers}", ConVar.Server.maxplayers },
                { "{server.worldsize}", ConVar.Server.worldsize },
                { "{server.seed}", ConVar.Server.seed },
                { "{server.level}", ConVar.Server.level },
                { "{localtime.now}", time},
                { "{localtime.date}", date},
                { "{players.active}", Active },
                { "{players.sleepers}", Sleepers },
                { "{players.total}", Active + Sleepers }
            };
            if (player != null)
            {
                string uid = player.UserIDString;
                if (PlayersData.ContainsKey(uid))
                {
                    PlayerData ply = PlayersData[uid];

                    Dict.Add("{player.name}", ply.Name);
                    Dict.Add("{player.country}", ply.Country);
                    Dict.Add("{player.code}", ply.Code);
                    Dict.Add("{player.ipaddress}", ply.IpAddress);
                    Dict.Add("{player.steamid}", ply.SteamID);

                    // Player Experience
                    int level = (int) player.xp.CurrentLevel;
                    //float currentXp = (float) player.xp.EarnedXp;
                    //float levelToXp = Rust.Xp.Config.LevelToXp(level + 1);
                    //float xpRequired = levelToXp - Rust.Xp.Config.LevelToXp(level);
                    //float levelXp = currentXp - Rust.Xp.Config.LevelToXp(level);
                    //float percentage = (levelXp / xpRequired) * 100;

                    Dict.Add("{player.level}", level);
                    //Dict.Add("{player.xp}", currentXp.ToString("0.00"));
                    //Dict.Add("{player.progress}", percentage.ToString("0.00"));

                    // BetterChat Support
                    BetterChat = (Plugin) plugins.Find("BetterChat");
                    if (EnableBetterChatSupport && BetterChat != null)
                    {
                        Dictionary<string, object> Group = (Dictionary<string, object>) BetterChat.Call("API_FindPlayerPrimaryGroup", uid);
                        if (Group != null)
                            Dict.Add("{player.title}", Group["TitleText"]);
                            Dict.Add("{player.titlecolor}", Group["TitleColor"]);
                            Dict.Add("{player.namecolor}", Group["PlayerNameColor"]);
                    }
                }
            }
            foreach (var item in Dict)
                text = text.Replace(item.Key, item.Value.ToString());
            return text;
        }

        private void PlaceSeparator(BasePlayer player = null, bool noPrefix = false)
        {
            if (EnableLineSperators)
            {
                if (player)
                    Tell(player, Seperator, usePrefix: noPrefix);
                else
                    Say(Seperator, usePrefix: noPrefix);
            }
        }

        private static List<List<string>> SplitList(List<string> names, int nSize = 3)
        {
            var list = new List<List<string>>();
            for (int i = 0; i < names.Count; i += nSize)
                list.Add(names.GetRange(i, Math.Min(nSize, names.Count - i)));
            return list;
        }

        private string GetLanguage(BasePlayer player = null)
        {
            string lang = "EN";

            if (player != null && RulesLanguage == "AUTO")
            {
                string PlyLang = PlayersData[player.UserIDString].Code;
                if (Rules.ContainsKey(PlyLang))
                    lang = PlyLang;
            }
            else if (Rules.ContainsKey(RulesLanguage))
                lang = RulesLanguage;

            return lang;
        }

        private List<string> GetActivePlayersList() { return (from ply in BasePlayer.activePlayerList where (PlayersData.ContainsKey(ply.UserIDString)) select ply.UserIDString).ToList(); }

        private bool NotHide(BasePlayer player) { return (!(HideAdmins && player.IsAdmin())); }

        private string Pads(string target, int number = 2) { return target.PadLeft(number, '0'); }

        #endregion Plugin Methods

        #region Player Hooks

        void OnPlayerInit(BasePlayer player) { InitializePlayer(player); }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            string uid = player.UserIDString;
            if (PlayersData.ContainsKey(uid))
            {   
                string LeaveMessage = GetMsg("Leave Notification");
                if (reason.StartsWith("Kicked: "))
                    reason = "Kicked: " + reason.Replace(reason.Split()[0], "").Trim();
                if (EnableLeaveMessage && NotHide(player))
                    Say(ReplaceNameFormats(LeaveMessage.Replace("{reason}", reason), player));
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            string uid = player.UserIDString;
            if (WM_Queue.Contains(uid))
            {
                WM_Queue.Remove(uid);
                List<string> lines = ConvertList(Config.Get("Welcome Messages"));
                foreach (string line in lines)
                    Tell(player, ReplaceNameFormats(line, player), usePrefix: false);
            }
        }

        #endregion Player Hooks

        #region Entity Hooks

        void OnAirdrop(CargoPlane plane, Vector3 location)
        {
            if (NotifyAirdrop)
            {
                string x = location.x.ToString();
                string y = location.y.ToString();
                string z = location.z.ToString();
                string loc = x + ", " + y + ", " + z;
                Say(GetMsg("Incoming Airdrop").Replace("{location}", loc).Replace("{x}", x).Replace("{y}", y).Replace("{z}", z));
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.ShortPrefabName == "patrolhelicopter" && NotifyHelicopter)
                Say(GetMsg("Incoming Helicopter"));
        }

        #endregion

        #region Plugin Commands

        [ChatCommand("notifier")]
        private void Plugin_Command(BasePlayer player, string command, string[] args)
        {
            string arg = string.Join(" ", args);
            if (arg == "help")
            {
                Dictionary<string, object> Settings = (Dictionary<string, object>) Config.Get("Commands", "Settings");
                foreach (var cmd in Settings)
                {
                    if ((bool)cmd.Value)
                    {
                        List<string> triggers = ConvertList(Config.Get("Commands", "Triggers", cmd.Key));
                        string row = string.Join("<silver>,<end> /", triggers.ToArray());
                        Tell(player, "<orange>" + row + "<end> - <lightblue>" + GetMsg(cmd.Key + " Description") + "<end>", usePrefix: false);
                    }
                }
            }
            else
            {
                Tell(player, "<cyan><size=18>NOTIFIER</size><end> <grey>v" + this.Version + "<end>", "76561198235146288", false);
                Tell(player, this.Description, usePrefix: false);
                Tell(player, "Powered by <orange>Oxide 2<end>, developed by <#9810FF>SkinN<end>", "76561197999302614", false);
            }
        }

        private void PlayersList_Command(BasePlayer player, string command, string[] args)
        {
            List<string> Active = GetActivePlayersList();
            List<string> PlayerNames = (from ply in Active where (!(PlayersData[ply].IsAdmin && HideAdmins) || player.IsAdmin()) select "<lightblue>" + PlayersData[ply].Name + "<end>").ToList();
            List<List<string>> Chuncks = SplitList(PlayerNames);
            Tell(player, "<white>" + GetMsg("Players List Title") + "<end>");

            PlaceSeparator(player, false);

            foreach (List<string> lis in Chuncks)
                Tell(player, String.Join(", ", lis.ToArray()), usePrefix: false);
        }

        private void ServerRules_Command(BasePlayer player, string command, string[] args)
        {
            string arg = string.Join(" ", args).ToUpper();
            string lang = GetLanguage(player);
            string name = "Unknown";

            if (Rules.Count == 0)
                return;

            if (!String.IsNullOrEmpty(arg))
            {
                bool found = false;
                foreach (var c in Countries)
                {
                    if (c.Key == arg)
                        lang = c.Key;
                        found = true;
                    string countryName = c.Value.ToUpper();
                    if (!found && countryName.Contains(arg))
                        lang = c.Key;
                        found = true;
                }
                if (!found && Rules.ContainsKey(arg))
                    lang = arg;
            }
            else if (arg == "LIST")
            {
                Tell(player, "<white>" + GetMsg("Rules Languages List Title") + "<end>");

                if (EnableLineSperators)
                    Tell(player, Seperator, usePrefix: false);

                List<string> langs = new List<string>();

                foreach (var item in Rules)
                {
                    if (Countries.ContainsKey(item.Key))
                        name = Countries[item.Key];
                    else
                        name = item.Key;
                    langs.Add("<orange>" + name + "<end> (" + item.Key + ")");
                }

                List<List<string>> Chuncks = SplitList(langs);

                foreach (List<string> lis in Chuncks)
                    Tell(player, String.Join(", ", lis.ToArray()), usePrefix: false);

                return;
            }

            if (Countries.ContainsKey(lang))
                name = Countries[lang];
            else
                name = lang.ToUpper();

            Tell(player, "<white>" + GetMsg("Server Rules Title") + "<end>");

            PlaceSeparator(player, false);

            List<string> ItemList = ConvertList(Rules[lang]);

            foreach (string line in ItemList)
                Tell(player, "<orange>" + line + "<end>", usePrefix: false);

            PlaceSeparator(player, false);

            Tell(player, "<white>Displaying <yellow>" + name + "<end> rules<end>", usePrefix: false);
        }

        private void PluginsList_Command(BasePlayer player, string command, string[] args)
        {
            Tell(player, "<white>" + GetMsg("Plugins List Title") + "<end>");
            PlaceSeparator(player, false);
            foreach (var p in plugins.GetAll())
            {
                if (p.Author != "Oxide Team")
                    Tell(player, String.Format("<orange>{0}<end> <grey>v{1}<end> <silver>{2}<end>", p.Title, p.Version, p.Author), usePrefix: false);
            }
        }

        private void AdminsList_Command(BasePlayer player, string command, string[] args)
        {
            List<string> Active = GetActivePlayersList();
            List<string> PlayerNames = (from ply in Active where PlayersData[ply].IsAdmin select "<cyan>" + PlayersData[ply].Name + "<end>").ToList();
            if (HideAdmins && !(player.IsAdmin()))
                Tell(player, GetMsg("No Admins Online"));
            else
            {
                List<List<string>> Chuncks = SplitList(PlayerNames);
                Tell(player, "<white>" + GetMsg("Admins List Title") + "<end>");
                PlaceSeparator(player, false);
                foreach (List<string> lis in Chuncks)
                    Tell(player, String.Join(", ", lis.ToArray()), usePrefix: false);
            }
        }

        private void PlayersCount_Command(BasePlayer player, string command, string[] args) { Tell(player, ReplaceNameFormats(GetMsg("Players Count Message"))); }

        private void ServerMap_Command(BasePlayer player, string command, string[] args) { Tell(player, ReplaceNameFormats(GetMsg("Server Map Message"))); }

        #endregion Plugin Commands
    }
}