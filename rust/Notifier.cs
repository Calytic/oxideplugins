using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Game.Rust.Libraries;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;


namespace Oxide.Plugins
{
    [Info("Notifier", "SkinN", "3.0.8", ResourceId = 797)]
    [Description("Server administration tool with chat based notifications")]

    class Notifier : RustPlugin
    {
        #region Plugin Resources

        // Developer Variables
        private readonly bool Dev = false;
        private readonly string Seperator = string.Join("-", new string[50 + 1]);
        private readonly string DatabaseFile = "Notifier_PlayersData";

        // Plugin Variables
        private Dictionary<string, PlayerCache> Players;
        private System.Random AdvertsLoop = new System.Random();
        private int LastAdvert = 0;
        private List<string> WM_Queue = new List<string>();

        // Avalailable Rules Countries Dictionary
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

        // HTML default colors
        private List<string> HTMLColors = new List<string> {
            "white", "silver", "gray", "black", "red", "maroon",
            "yellow", "orange", "olive", "lime", "green", "aqua",
            "teal", "blue", "navy", "fuchsia", "purple", "cyan",
            "grey", "lightblue", "dimgrey", "lightgreen", "pink", "magenta"
        };

        // Player Database Class
        public class PlayerCache
        {
            public bool isadmin = false;
            public string steamid = "STEAM_ID_UNKOWN";
            public string ipaddress = "Unknown";
            public string country = "Unknown";
            public string username = "Unknown";
            public string countrycode = "Unknown";

            public PlayerCache()
            {
            }

            internal PlayerCache(BasePlayer player)
            {
                steamid = player.UserIDString;
                this.Update(player);
            }

            internal void Update(BasePlayer player)
            {
                username = player.displayName;
                ipaddress = player.net.connection.ipaddress.Split(':')[0];
                isadmin = player.IsAdmin();
            }
        };

        // Configuration Variables
        private string Motd;
        private string Prefix;
        private string IconProfile;
        private string RulesDefaultLanguage;
        private string TimeFormat;
        private string DateFormat;
        private int AdvertsInterval;
        private bool EnableIconProfile;
        private bool EnableJoinMessage;
        private bool EnableLeaveMessage;
        private bool EnableScheduledMessages;
        private bool BroadcastToConsole;
        private bool EnablePluginPrefix;
        private bool EnableAdvertMessages;
        private bool EnableWelcomeMessage;
        private bool NotifyHelicopter;
        private bool NotifyAirdrop;
        private bool HideAdmins;
        private bool EnableChatSeparators;

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig()
        {
        }

        private void LoadVariables()
        {
            // Force Clear Configuration On Developer Mode
            //Config.Clear();

            #region General Settings

            Prefix = GetConfig<string>("General Settings", "Prefix", "<white>[ <cyan>NOTIFIER<end> ]<end>");
            EnablePluginPrefix = GetConfig<bool>("General Settings", "Enable Plugin Prefix", true);
            EnableIconProfile = GetConfig<bool>("General Settings", "Enable Icon Profile", false);
            IconProfile = GetConfig<string>("General Settings", "Icon Profile", "76561198235146288");
            TimeFormat = GetConfig<string>("General Settings", "Time Format", "{hour}:{minute}:{second}");
            DateFormat = GetConfig<string>("General Settings", "Date Format", "{day}/{month}/{year}");
            RulesDefaultLanguage = GetConfig<string>("General Settings", "Rules Default Language", "auto");
            BroadcastToConsole = GetConfig<bool>("General Settings", "Broadcast To Console", true);
            EnableScheduledMessages = GetConfig<bool>("General Settings", "Enable Scheduled Messages", false);
            EnableJoinMessage = GetConfig<bool>("General Settings", "Enable Join Message", true);
            EnableLeaveMessage = GetConfig<bool>("General Settings", "Enable Leave Message", true);
            EnableAdvertMessages = GetConfig<bool>("General Settings", "Enable Advert Messages", true);
            EnableWelcomeMessage = GetConfig<bool>("General Settings", "Enable Welcome Message", true);
            AdvertsInterval = GetConfig<int>("General Settings", "Adverts Interval (In Minutes)", 12);
            NotifyHelicopter = GetConfig<bool>("General Settings", "Notify Incoming Helicopter", false);
            NotifyAirdrop = GetConfig<bool>("General Settings", "Notify Incoming Airdrop", false);
            HideAdmins = GetConfig<bool>("General Settings", "Hide Admins", false);
            EnableChatSeparators = GetConfig<bool>("General Settings", "Enable Chat Separators", true);

            #endregion

            // MOTD
            Motd = GetConfig<string>("Message Of The Day", "We are using <cyan>Notifier<end> <grey>v3.0<end>, type <orange>/notifier help<end> for all its available commands.");

            #region Advert Messages

            SetConfig("Advert Messages", new List<object>
                {
                    "<orange>Need help?<end> Try calling for the <cyan>Admins<end> in the chat.",
                    "Please avoid any insults and be respectful!",
                    "Cheating or abusing of game exploits will result in a <red>permanent<end> ban.",
                    "You are playing on: <orange>{server.hostname}<end>",
                    "There are <orange>{players.active}<silver>/<end>{server.maxplayers} <silver>players playing in the server, and<end> {players.sleepers}<end> sleepers.",
                    "Check <cyan>Notifier's<end> with <orange>/notifier help<end> command."
                }
            );

            #endregion

            #region Welcome Messages

            SetConfig("Welcome Messages", new List<object>
                {
                    "<size=18>Welcome <lightblue>{player.name}<end></size>",
                    "<orange><size=20>â¢</size><end> Type <orange>/notifier help<end> for all available commands",
                    "<orange><size=20>â¢</size><end> Please respect our server <orange>/rules<end>",
                    "<orange><size=20>â¢</size><end> Check our live map at <lime>http://{server.ip}:{server.port}<end>"
                }
            );

            #endregion

            #region Schedule Messages

            SetConfig("Schedule Messages", new Dictionary<string, object>
                {
                    {
                        "22:50", new List<object> {
                            "It is now <orange>{localtime.now}<end> (Server local time)"
                        }
                    },
                    {
                        "22:00", new List<object> {
                            "It is now <orange>{localtime.date} {localtime.now}<end> (Server local time)"
                        }
                    }
                }
            );

            #endregion

            #region Rules

            SetConfig("Rules", "EN", new List<object> {
                    "Cheating is strictly prohibited.",
                    "Respect all players",
                    "Avoid spam in chat.",
                    "Play fair and don\'t abuse of bugs/exploits."
                }
            );
            SetConfig("Rules", "PT", new List<object> {
                    "Usar cheats e totalmente proibido.",
                    "Respeita todos os jogadores.",
                    "Evita spam no chat.",
                    "Nao abuses de bugs ou exploits."
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

            #endregion

            #region Commands

            // Command Triggers
            SetConfig("Commands", "Triggers", "Players List", new List<string> { "/players" });
            SetConfig("Commands", "Triggers", "Plugins List", new List<string> { "/plugins" });
            SetConfig("Commands", "Triggers", "Admins List", new List<string> { "/admins" });
            SetConfig("Commands", "Triggers", "Players Count", new List<string> { "/online" });
            SetConfig("Commands", "Triggers", "Server MOTD", new List<string> { "/motd" });
            SetConfig("Commands", "Triggers", "Server Map", new List<string> { "/map" });
            SetConfig("Commands", "Triggers", "Server Rules", new List<string> { "/rules" });

            // Command Settings
            SetConfig("Commands", "Settings", "Players List", true);
            SetConfig("Commands", "Settings", "Plugins List", false);
            SetConfig("Commands", "Settings", "Admins List", false);
            SetConfig("Commands", "Settings", "Server Rules", true);
            SetConfig("Commands", "Settings", "Server Map", false);
            SetConfig("Commands", "Settings", "Server MOTD", false);
            SetConfig("Commands", "Settings", "Players Count", true);

            #endregion

            SaveConfig();
        }

        #region Configuration Methods

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

        #endregion

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "Join Message", "<lightblue>{player.name} <silver>joined from<end> {player.country}<end>" },
                { "Leave Message", "<lightblue>{player.name}<end> left the server (Reason: {reason})" },
                { "Incoming Airdrop", "<yellow>Airdrop <silver>incoming, drop coordinates are:<end> {x}, {y}, {z}<end>."},
                { "Incoming Helicopter", "<yellow>Patrol Helicopter<end> incoming!" },
                { "No Admins Online", "There are no <cyan>Admins<end> currently online" },
                { "Players List Description", "List of active players" },
                { "Plugins List Description", "List of plugins running in the server" },
                { "Admins List Description", "List of active Admins" },
                { "Server Rules Description", "Displays server rules (In the player Steam language if set to automatic)" },
                { "Server Map Description", "Shows the URL to the server live map (Rust:IO)" },
                { "Server MOTD Description", "Shows the Message Of The Day" },
                { "Players Count Description", "Counts active players, sleepers and admins of the server" },
                { "Players List Title", "Players List" },
                { "Plugins List Title", "Plugins List" },
                { "Admins List Title", "Admins Online" },
                { "Server Rules Title", "Server Rules" },
                { "Rules Languages List Title", "Available Rules Languages" },
                { "Server MOTD Title", "Message Of The Day" },
                { "Server Map Message", "Check our live map at: <yellow>{server.ip}:{server.port}<end>" },
                { "Players Count Message", "There are <orange>{players.active} <silver>of<end> {server.maxplayers}<end> <silver>players in the server, and <orange>{players.sleepers}<end> sleepers<end>" },
                { "Help Text Message", "For all the <cyan>Notifier<end>'s commands type <orange>/notifier help<end>" }
            }, this);
        }

        private string GetMsg(string key) { return lang.GetMessage(key, this, null); }

        #endregion

        #region Messages System

        private void Con(string msg)
        {
            if (BroadcastToConsole)
                Puts(SimpleColorFormat(msg, true));
        }

        private void Say(string msg, string profile = "0", bool prefix = true)
        {
            Con(msg);
            if (!String.IsNullOrEmpty(Prefix) && EnablePluginPrefix && prefix)
                msg = Prefix + " " + msg;

            if (profile == "0" && EnableIconProfile)
                profile = IconProfile;

            rust.BroadcastChat(SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        private void Tell(BasePlayer player, string msg, string profile = "0", bool prefix = true)
        {
            if (!String.IsNullOrEmpty(Prefix) && EnablePluginPrefix && prefix)
                msg = Prefix + " " + msg;

            if (profile == "0" && EnableIconProfile)
                profile = IconProfile;

            rust.SendChatMessage(player, SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        public string SimpleColorFormat(string text, bool removeTags = false)
        {
            /*  Simple Color Format ( v3.0 ) */

            Regex end = new Regex(@"\<(end?)\>"); // End Tags
            Regex hex = new Regex(@"\<(#\w+?)\>"); // Hex Codes
            Regex names = new Regex(@"\<(\w+?)\>"); // Color Names

            if (removeTags)
            {   
                // Remove tags
                text = end.Replace(text, "");
                text = names.Replace(text, "");
                text = hex.Replace(text, "");
            }
            else
            {   
                foreach (Match i in names.Matches(text))
                {
                    string x = i.ToString().Replace("<", "").Replace(">", "");
                    if (HTMLColors.Contains(x.ToLower()))
                        text = text.Replace("<" + x + ">", "<color=" + x.ToLower() + ">");
                }

                // Replace tags
                text = end.Replace(text, "</color>");
                text = hex.Replace(text, "<color=$1>");
            }

            return text;
        }

        #endregion

        #region Plugin Hooks

        private void Init()
        {
            // Load variables and plugin messages
            LoadVariables();
            LoadMessages();

            // Get Players database
            try
            {
                Players = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerCache>>(DatabaseFile);
            }
            catch { Players = new Dictionary<string, PlayerCache>(); }

            // Cache all active players
            foreach (BasePlayer ply in BasePlayer.activePlayerList)
                OnPlayerInit(ply, false);

            // Plugin Timers
            if (EnableAdvertMessages)
            {
                timer.Repeat(AdvertsInterval * 60, 0, () => SendAdvert());
                Puts("Started Advert Messages timer, set to " + AdvertsInterval + " minute/s");
            }

            if (EnableScheduledMessages)
            {
                timer.Repeat(60, 0, () => CheckScheduledMessages());
                Puts("Started Schedule Messages timer");
            }

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

        void Unload()
        {
            // Write database file
            Interface.Oxide.DataFileSystem.WriteObject(DatabaseFile, Players);
        }

        #endregion

        #region Player Hooks

        void OnPlayerInit(BasePlayer player, bool sendJoinMessages = true)
        {
            string uid = player.UserIDString;

            if (!Players.ContainsKey(uid))
                Players.Add(uid, new PlayerCache(player));
            else
                Players[uid].Update(player);

            string country = Players[uid].country;
            string cCode = Players[uid].countrycode;
            if ((country == "Unknown" || country == null) || (cCode == "Unknown" || cCode == null))
                webrequest.EnqueueGet("http://ip-api.com/json/" + Players[uid].ipaddress + "?fields=3",
                    (code, response) => WebrequestFilter(code, response, player, sendJoinMessages), this);
            else if (sendJoinMessages)
                JoinMessages(player);

        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            string uid = player.UserIDString;

            if (Players.ContainsKey(uid))
            {   
                string LeaveMessage = GetMsg("Leave Message");

                if (reason.StartsWith("Kicked: "))
                    reason = "Kicked: " + reason.Replace(reason.Split()[0], "").Trim();

                if (EnableLeaveMessage && NotHide(player))
                    Say(GetNameFormats(LeaveMessage.Replace("{reason}", reason), player));
            }
        }

        #endregion

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
            if (NotifyHelicopter && entity.ToString().Contains("/patrolhelicopter.prefab"))
                Say(GetMsg("Incoming Helicopter"));
        }

        #endregion

        #region Plugin Commands

        void PlayersList_Command(BasePlayer player, string command, string[] args)
        {
            List<string> Active = GetActivePlayersList();
            List<string> PlayerNames = (from ply in Active where (!(Players[ply].isadmin && HideAdmins) || player.IsAdmin()) select "<lightblue>" + Players[ply].username + "<end>").ToList();
            List<List<string>> Chuncks = SplitList(PlayerNames);

            Tell(player, "<white>" + GetMsg("Players List Title") + "<end>");

            if (EnableChatSeparators)
                Tell(player, Seperator, prefix: false);

            foreach (List<string> lis in Chuncks)
                Tell(player, String.Join(", ", lis.ToArray()), prefix: false);
        }

        void AdminsList_Command(BasePlayer player, string command, string[] args)
        {
            List<string> Active = GetActivePlayersList();
            List<string> PlayerNames = (from ply in Active where Players[ply].isadmin select "<cyan>" + Players[ply].username + "<end>").ToList();

            if (HideAdmins && !(player.IsAdmin()))
                Tell(player, GetMsg("No Admins Online"));
            else
            {
                List<List<string>> Chuncks = SplitList(PlayerNames);

                Tell(player, "<white>" + GetMsg("Admins List Title") + "<end>");

                if (EnableChatSeparators)
                    Tell(player, Seperator, prefix: false);

                foreach (List<string> lis in Chuncks)
                    Tell(player, String.Join(", ", lis.ToArray()), prefix: false);
            }
        }

        void ServerRules_Command(BasePlayer player, string command, string[] args)
        {
            string arg = string.Join(" ", args).ToUpper();
            string lang = GetLanguage(player);
            string name = "Unknown";
            Dictionary<string, object> Rules = (Dictionary<string, object>) Config.Get("Rules");

            if (Rules.Count == 0)
                return;

            if (!String.IsNullOrEmpty(arg) && Rules.ContainsKey(arg))
            {
                lang = arg;
            }
            else if (arg == "LIST")
            {
                Tell(player, "<white>" + GetMsg("Rules Languages List Title") + "<end>");

                if (EnableChatSeparators)
                    Tell(player, Seperator, prefix: false);

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
                    Tell(player, String.Join(", ", lis.ToArray()), prefix: false);

                return;
            }

            if (Countries.ContainsKey(lang))
                    name = Countries[lang];
                else
                    name = lang.ToUpper();

            Tell(player, "<white>" + GetMsg("Server Rules Title") + "<end>");

            if (EnableChatSeparators)
                Tell(player, Seperator, prefix: false);

            List<string> ItemList = ConvertList(Rules[lang]);

            foreach (string line in ItemList)
                Tell(player, "<orange>" + line + "<end>", prefix: false);

            if (EnableChatSeparators)
                Tell(player, Seperator, prefix: false);

            Tell(player, "<white>Displaying <yellow>" + name + "<end> rules<end>", prefix: false);
        }

        void PluginsList_Command(BasePlayer player, string command, string[] args)
        {
            Tell(player, "<white>" + GetMsg("Plugins List Title") + "<end>");

            if (EnableChatSeparators)
                Tell(player, Seperator, prefix: false);

            foreach (var p in plugins.GetAll())
            {
                if (p.Author != "Oxide Team")
                    Tell(player, String.Format("<orange>{0}<end> <grey>v{1}<end> <silver>{2}<end>", p.Title, p.Version, p.Author), prefix: false);
            }
        }

        void ServerMOTD_Command(BasePlayer player, string command, string[] args)
        {
            string arg = string.Join(" ", args);

            if (!String.IsNullOrEmpty(arg))
            {
                Motd = arg;
                SetConfig("Message Of The Day", Motd);
                SaveConfig();
                Tell(player, "MOTD changed to: <white>" + Motd + "<end>");
                return;
            }

            Tell(player, "<white>" + GetMsg("Server MOTD Title") + "<end>");

            if (EnableChatSeparators)
                Tell(player, Seperator, prefix: false);

            Tell(player, GetNameFormats("<white>" + Motd + "<end>"), prefix: false);
        }

        void PlayersCount_Command(BasePlayer player, string command, string[] args) { Tell(player, GetNameFormats(GetMsg("Players Count Message"))); }

        void ServerMap_Command(BasePlayer player, string command, string[] args) { Tell(player, GetNameFormats(GetMsg("Server Map Message"))); }

        [ChatCommand("notifier")]
        void Plugin_Command(BasePlayer player, string command, string[] args)
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
                        Tell(player, "<orange>" + row + "<end> - <lightblue>" + GetMsg(cmd.Key + " Description") + "<end>", prefix: false);
                    }
                }
            }
            else
            {
                Tell(player, "<cyan><size=18>NOTIFIER</size><end> <grey>v" + this.Version + "<end>", "76561198235146288", false);
                Tell(player, this.Description, prefix: false);
                Tell(player, "Powered by <orange>Oxide 2<end> and developed by <#9810FF>SkinN<end>", "76561197999302614", false);
            }
        }

        #endregion

        #region Plugin Methods

        private void WebrequestFilter(int code, string response, BasePlayer player, bool sendJoinMessages)
        {
            try
            {
                string uid = player.UserIDString;

                if (!(response == null || code != 200))
                {
                    if (Players.ContainsKey(uid))
                    {
                        var json = JObject.Parse(response);
                        string country = (string) json["country"];
                        string cCode = (string) json["countryCode"];
                        if (country != null)
                            Players[uid].country = country;
                        if (cCode != null)
                            Players[uid].countrycode = cCode;
                    }
                }
            } catch {}

            if (sendJoinMessages)
                JoinMessages(player);
        }

        private void JoinMessages(BasePlayer player)
        {
            try
            {
                if (EnableJoinMessage && NotHide(player))
                {
                    Say(GetNameFormats(GetMsg("Join Message"), player));
                    string sep = string.Join("\n", new string[50 + 1]);
                    Tell(player, sep, prefix: false);
                }
                if (EnableWelcomeMessage)
                {
                    List<string> WelcomeMessage = ConvertList(Config.Get("Welcome Messages"));
                    foreach (string line in WelcomeMessage)
                        Tell(player, GetNameFormats(line, player), prefix: false);
                }
            } catch {}
        }

        private void SendAdvert()
        {
            List<string> Adverts = ConvertList(Config.Get("Advert Messages"));
            int index = LastAdvert;

            // Is there more than one Advert?
            if (Adverts.Count > 1)
            {
                // Loop untill it gets a different advert
                while (index == LastAdvert)
                    index = AdvertsLoop.Next(Adverts.Count);
            }

            LastAdvert = index;

            Say(GetNameFormats((string) Adverts[index]));
        }

        private void CheckScheduledMessages()
        {
            Dictionary<string, object> ScheduleMessages = (Dictionary<string, object>) Config.Get("Schedule Messages");
            DateTime Now = DateTime.Now;
            string cur = Pads(Now.Hour.ToString()) + ":" + Pads(Now.Second.ToString());

            if (ScheduleMessages.ContainsKey(cur))
            {
                List<string> ItemList = ConvertList(ScheduleMessages[cur]);
                foreach (string line in ItemList)
                    Say(GetNameFormats(line));
            }
        }

        private string GetLanguage(BasePlayer player = null)
        {
            string lang = "EN";
            string DefaultLang = RulesDefaultLanguage.ToUpper();
            Dictionary<string, object> Rules = (Dictionary<string, object>) Config.Get("Rules");

            if (player != null && DefaultLang == "AUTO")
            {
                string PlyLang = Players[player.UserIDString].countrycode;
                if (Rules.ContainsKey(PlyLang))
                    lang = PlyLang;
            }
            else if (Rules.ContainsKey(DefaultLang))
                lang = DefaultLang;

            return lang;
        }

        private string GetNameFormats(string text, BasePlayer player = null)
        {

            int Active = BasePlayer.activePlayerList.Count;
            int Sleepers = BasePlayer.sleepingPlayerList.Count;
            DateTime Now = DateTime.Now;
            string time = TimeFormat.Replace("{hour}", Pads(Now.Hour.ToString())).Replace("{minute}", Pads(Now.Minute.ToString())).Replace("{second}", Pads(Now.Second.ToString()));
            string date = DateFormat.Replace("{year}", Now.Year.ToString()).Replace("{month}", Pads(Now.Month.ToString())).Replace("{day}", Pads(Now.Day.ToString()));

            #region Server Side
            Dictionary<string, object> Dict = new Dictionary<string, object> {
                { "{server.ip}", ConVar.Server.ip },
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
            #endregion

            #region Player Side

            if (player != null)
            {
                string uid = player.UserIDString;
                if (Players.ContainsKey(uid))
                { 
                    Dict.Add("{player.name}", Players[uid].username);
                    Dict.Add("{player.country}", Players[uid].country);
                    Dict.Add("{player.countrycode}", Players[uid].countrycode);
                    Dict.Add("{player.ip}", Players[uid].ipaddress);
                    Dict.Add("{player.uid}", Players[uid].steamid);
                }
            }

            #endregion

            foreach (var kvp in Dict)
                text = text.Replace(kvp.Key, kvp.Value.ToString());

            return text;
        }

        private static List<List<string>> SplitList(List<string> names, int nSize = 3)
        {
            var list = new List<List<string>>();
            for (int i = 0; i < names.Count; i += nSize)
                list.Add(names.GetRange(i, Math.Min(nSize, names.Count - i)));
            return list;
        }

        private bool NotHide(BasePlayer player) { return (!(HideAdmins && player.IsAdmin())); }

        private List<string> GetActivePlayersList() { return (from ply in BasePlayer.activePlayerList where (Players.ContainsKey(ply.UserIDString)) select ply.UserIDString).ToList(); }

        private string Pads(string target, int number = 2) { return target.PadLeft(number, '0'); }

        void SendHelpText(BasePlayer player) { Tell(player, GetMsg("Help Text Message")); }

        #endregion
    }
}