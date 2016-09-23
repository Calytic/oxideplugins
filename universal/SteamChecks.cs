using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("SteamChecks", "Spicy", "2.4.14")]
    [Description("Check Steam servers to grab information to act on.")]

    class SteamChecks : CovalencePlugin
    {
        #region Globals

        uint steamAppId;
        Regex limitedAccountRegex;

        #endregion

        #region Functions

        string LangMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        bool Empty(string str) => string.IsNullOrEmpty(str);

        bool WhiteListed(IPlayer player) => whiteList.Contains(player.Id);

        bool ValidRequest(int code)
        {
            switch (code)
            {
                case 200:
                    return true;
                case 401:
                    Puts(LangMessage("SteamAPIKeyInvalid"));
                    return false;
                case 404:
                case 503:
                    Puts(LangMessage("SteamAPIUnavailable"));
                    return false;
                default:
                    Puts(string.Format(LangMessage("WebRequestFailed"), code));
                    return false;
            }
        }

        #endregion

        #region Configuration

        string steamAPIKey;

        bool communityBanKick;
        bool communityBanBroadcast;

        bool vacBanKick;
        int vacBanThreshold;
        bool vacBanBroadcast;

        bool daysSinceLastBanKick;
        int daysSinceLastBanThreshold;
        bool daysSinceLastBanBroadcast;

        bool gameBanKick;
        int gameBanThreshold;
        bool gameBanBroadcast;

        bool tradeBanKick;
        bool tradeBanBroadcast;

        bool privateProfileKick;
        bool privateProfileBroadcast;

        bool limitedAccountKick;
        bool limitedAccountBroadcast;

        bool noProfileKick;
        bool noProfileBroadcast;

        bool sharingGameKick;
        bool sharingGameBroadcast;

        bool hoursPlayedKick;
        int hoursPlayedThreshold;
        bool hoursPlayedBroadcast;

        List<string> whiteList;

        protected override void LoadDefaultConfig()
        {
            Config["Settings"] = new Dictionary<string, object>
            {
                ["SteamAPIKey"] = "",

                ["CommunityBanKick"] = false,
                ["CommunityBanBroadcast"] = true,

                ["VACBanKick"] = false,
                ["VACBanThreshold"] = 2,
                ["VACBanBroadcast"] = true,

                ["DaysSinceLastBanKick"] = false,
                ["DaysSinceLastBanThreshold"] = 0,
                ["DaysSinceLastBanBroadcast"] = true,

                ["GameBanKick"] = false,
                ["GameBanThreshold"] = 2,
                ["GameBanBroadcast"] = true,

                ["TradeBanKick"] = false,
                ["TradeBanBroadcast"] = true,

                ["PrivateProfileKick"] = false,
                ["PrivateProfileBroadcast"] = true,

                ["LimitedAccountKick"] = false,
                ["LimitedAccountBroadcast"] = true,

                ["NoProfileKick"] = false,
                ["NoProfileBroadcast"] = true,

                ["SharingGameKick"] = false,
                ["SharingGameBroadcast"] = true,

                ["HoursPlayedKick"] = false,
                ["HoursPlayedThreshold"] = 0,
                ["HoursPlayedBroadcast"] = true,

                ["WhiteList"] = new List<string>
                {
                    "76561198103592543"
                }
            };
        }

        void SetupConfiguration()
        {
            steamAPIKey = Config.Get<string>("Settings", "SteamAPIKey");

            communityBanKick = Config.Get<bool>("Settings", "CommunityBanKick");
            communityBanBroadcast = Config.Get<bool>("Settings", "CommunityBanBroadcast");

            vacBanKick = Config.Get<bool>("Settings", "VACBanKick");
            vacBanThreshold = Config.Get<int>("Settings", "VACBanThreshold");
            vacBanBroadcast = Config.Get<bool>("Settings", "VACBanBroadcast");

            daysSinceLastBanKick = Config.Get<bool>("Settings", "DaysSinceLastBanKick");
            daysSinceLastBanThreshold = Config.Get<int>("Settings", "DaysSinceLastBanThreshold");
            daysSinceLastBanBroadcast = Config.Get<bool>("Settings", "DaysSinceLastBanBroadcast");

            gameBanKick = Config.Get<bool>("Settings", "GameBanKick");
            gameBanThreshold = Config.Get<int>("Settings", "GameBanThreshold");
            gameBanBroadcast = Config.Get<bool>("Settings", "GameBanBroadcast");

            tradeBanKick = Config.Get<bool>("Settings", "TradeBanKick");
            tradeBanBroadcast = Config.Get<bool>("Settings", "TradeBanBroadcast");

            privateProfileKick = Config.Get<bool>("Settings", "PrivateProfileKick");
            privateProfileBroadcast = Config.Get<bool>("Settings", "PrivateProfileBroadcast");

            limitedAccountKick = Config.Get<bool>("Settings", "LimitedAccountKick");
            limitedAccountBroadcast = Config.Get<bool>("Settings", "LimitedAccountBroadcast");

            noProfileKick = Config.Get<bool>("Settings", "NoProfileKick");
            noProfileBroadcast = Config.Get<bool>("Settings", "NoProfileBroadcast");

            sharingGameKick = Config.Get<bool>("Settings", "SharingGameKick");
            sharingGameBroadcast = Config.Get<bool>("Settings", "SharingGameBroadcast");

            hoursPlayedKick = Config.Get<bool>("Settings", "HoursPlayedKick");
            hoursPlayedThreshold = Config.Get<int>("Settings", "HoursPlayedThreshold");
            hoursPlayedBroadcast = Config.Get<bool>("Settings", "HoursPlayedBroadcast");

            whiteList = Config.Get<List<string>>("Settings", "WhiteList");
        }

        #endregion

        #region Language

        void SetupLanguage()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["WhiteListedPlayer"] = "{0} [{1}] is a whitelisted player, therefore they bypass SteamChecks.",

                ["SteamAPIKeyInvalid"] = "The Steam API Key you entered is invalid.",
                ["SteamAPIUnavailable"] = "The Steam API is unavailable at this time.",
                ["WebRequestFailed"] = "WebRequest to SteamAPI failed. Error {0}.",

                ["CommunityBanKick"] = "You were kicked. (Steam Community ban).",
                ["CommunityBanBroadcast"] = "{0} attempted to join but they have a Steam Community ban.",
                ["CommunityBanConsole"] = "{0} [{1}] was kicked. (Steam Community ban).",

                ["VACBanKick"] = "You were kicked. (Too many VAC bans).",
                ["VACBanBroadcast"] = "{0} attempted to join but they have too many VAC bans.",
                ["VACBanConsole"] = "{0} [{1}] was kicked. (Too many VAC bans).",

                ["DaysSinceLastBanKick"] = "You were kicked. (Ban is too recent, please wait {0} days).",
                ["DaysSinceLastBanBroadcast"] = "{0} attempted to join but their ban is too recent.",
                ["DaysSinceLastBanConsole"] = "{0} [{1}] was kicked. (Ban is too recent, they need to wait {2} days).",

                ["GameBanKick"] = "You were kicked. (Too many Game bans).",
                ["GameBanBroadcast"] = "{0} attempted to join but they have too many Game bans.",
                ["GameBanConsole"] = "{0} [{1}] was kicked. (Too many Game bans).",

                ["TradeBanKick"] = "You were kicked. (Trade banned).",
                ["TradeBanBroadcast"] = "{0} attempted to join but they are trade banned.",
                ["TradeBanConsole"] = "{0} [{1}] was kicked. (Trade banned).",

                ["PrivateProfileKick"] = "You were kicked. (Private profile).",
                ["PrivateProfileBroadcast"] = "{0} attempted to join but their profile is private.",
                ["PrivateProfileConsole"] = "{0} [{1}] was kicked. (Private profile).",

                ["LimitedAccountKick"] = "You were kicked. (Limited account).",
                ["LimitedAccountBroadcast"] = "{0} attempted to join but their account is limited.",
                ["LimitedAccountConsole"] = "{0} [{1}] was kicked. (Limited account).",
                ["LimitedAccountCheckFailed"] = "Couldn't check if {0} [{1}] has a limited account because they haven't setup their profile.",

                ["NoProfileKick"] = "You were kicked. (No Steam Community profile).",
                ["NoProfileBroadcast"] = "{0} attempted to join but they haven't setup their profile.",
                ["NoProfileConsole"] = "{0} [{1}] was kicked. (No Steam Community profile).",

                ["SharingGameKick"] = "You were kicked. (Family sharing).",
                ["SharingGameBroadcast"] = "{0} attempted to join but they are family sharing the game.",
                ["SharingGameConsole"] = "{0} [{1}] was kicked. (Family sharing).",

                ["HoursPlayedKick"] = "You were kicked. (Not enough playtime. You lack: {0} hours).",
                ["HoursPlayedBroadcast"] = "{0} attempted to join but they don't have enough playtime.",
                ["HoursPlayedConsole"] = "{0} [{1}] was kicked. (Not enough playtime. They lack: {2} hours).",
                ["HoursPlayedCheckFailed"] = "Couldn't check hours played from {0} [{1}] because they have a private profile or are family sharing."
            }, this);
        }

        #endregion

        #region JSON

        public class GetPlayerBans
        {
            [JsonProperty("players")]
            public Player[] Players { get; set; }

            public class Player
            {
                [JsonProperty("CommunityBanned")]
                public string CommunityBanned { get; set; }

                [JsonProperty("VACBanned")]
                public string VACBanned { get; set; }

                [JsonProperty("NumberOfVACBans")]
                public int NumberOfVACBans { get; set; }

                [JsonProperty("DaysSinceLastBan")]
                public int DaysSinceLastBan { get; set; }

                [JsonProperty("NumberOfGameBans")]
                public int NumberOfGameBans { get; set; }

                [JsonProperty("EconomyBan")]
                public string EconomyBan { get; set; }
            }
        }

        public class GetPlayerSummaries
        {
            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {
                [JsonProperty("players")]
                public Player[] Players { get; set; }

                public class Player
                {
                    [JsonProperty("communityvisibilitystate")]
                    public int CommunityVisibilityState { get; set; }
                }
            }
        }

        public class IsPlayingSharedGame
        {
            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {
                [JsonProperty("lender_steamid")]
                public ulong LenderSteamId { get; set; }
            }
        }

        public class GetOwnedGames
        {
            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {
                [JsonProperty("games")]
                public Game[] Games { get; set; }

                public class Game
                {
                    [JsonProperty("appid")]
                    public int AppId { get; set; }

                    [JsonProperty("playtime_forever")]
                    public int PlaytimeForever { get; set; }
                }
            }
        }

        #endregion

        #region Server Hooks

        void Init()
        {
            steamAppId = covalence.ClientAppId;
            limitedAccountRegex = new Regex("<isLimitedAccount>(.*)</isLimitedAccount>");

            SetupConfiguration();
            SetupLanguage();

            if (Empty(steamAPIKey))
                Puts("Incorrect configuration! Please provide your Steam API Key. Get one at \"http://steamcommunity.com/dev/apikey\".");
        }

        void OnUserConnected(IPlayer player)
        {
            if (WhiteListed(player))
            {
                Puts(string.Format(LangMessage("WhiteListedPlayer"), player.Name, player.Id));
                return;
            }

            RunSteamChecks(player);
        }

        #endregion

        #region Steam Checking

        void RunSteamChecks(IPlayer player)
        {
            if (Empty(steamAPIKey))
            {
                Puts("Incorrect configuration! Please provide your Steam API Key. Get one at \"http://steamcommunity.com/dev/apikey\".");
                return;
            }

            string requestBans = $"http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={steamAPIKey}&steamids={player.Id}";

            webrequest.EnqueueGet(requestBans, (codeBans, responseBans) =>
            {
                if (!ValidRequest(codeBans))
                    return;

                GetPlayerBans jsonBans = JsonConvert.DeserializeObject<GetPlayerBans>(responseBans);

                if (jsonBans.Players[0].CommunityBanned == "True" && communityBanKick)
                {
                    player.Kick(LangMessage("CommunityBanKick", player.Id));
                    Puts(string.Format(LangMessage("CommunityBanConsole"), player.Name, player.Id));

                    if (communityBanBroadcast)
                        server.Broadcast(string.Format(LangMessage("CommunityBanBroadcast"), player.Name));

                    return;
                }

                if (jsonBans.Players[0].NumberOfVACBans > vacBanThreshold && vacBanKick)
                {
                    player.Kick(LangMessage("VACBanKick", player.Id));
                    Puts(string.Format(LangMessage("VACBanConsole"), player.Name, player.Id));

                    if (vacBanBroadcast)
                        server.Broadcast(string.Format(LangMessage("VACBanBroadcast"), player.Name));

                    return;
                }

                if (jsonBans.Players[0].NumberOfGameBans > gameBanThreshold && gameBanKick)
                {
                    player.Kick(LangMessage("GameBanKick", player.Id));
                    Puts(string.Format(LangMessage("GameBanConsole"), player.Name, player.Id));

                    if (gameBanBroadcast)
                        server.Broadcast(string.Format(LangMessage("GameBanBroadcast"), player.Name));

                    return;
                }

                if (jsonBans.Players[0].EconomyBan == "banned" && tradeBanKick)
                {
                    player.Kick(LangMessage("TradeBanKick", player.Id));
                    Puts(string.Format(LangMessage("TradeBanConsole"), player.Name, player.Id));

                    if (tradeBanBroadcast)
                        server.Broadcast(string.Format(LangMessage("TradeBanBroadcast"), player.Name));

                    return;
                }

                int daysSinceLastBan = jsonBans.Players[0].DaysSinceLastBan;

                if (daysSinceLastBanKick && jsonBans.Players[0].VACBanned == "True" && daysSinceLastBan < daysSinceLastBanThreshold)
                {
                    int daysNeeded = daysSinceLastBanThreshold - daysSinceLastBan;

                    player.Kick(string.Format(LangMessage("DaysSinceLastBanKick", player.Id), daysNeeded));
                    Puts(string.Format(LangMessage("DaysSinceLastBanConsole"), player.Name, player.Id, daysNeeded));

                    if (daysSinceLastBanBroadcast)
                        server.Broadcast(string.Format(LangMessage("DaysSinceLastBanBroadcast"), player.Name));

                    return;
                }

                string requestPrivateProfile = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamAPIKey}&steamids={player.Id}";

                webrequest.EnqueueGet(requestPrivateProfile, (codePrivateProfile, responsePrivateProfile) =>
                {
                    if (!ValidRequest(codePrivateProfile))
                        return;

                    GetPlayerSummaries jsonPrivateProfile = JsonConvert.DeserializeObject<GetPlayerSummaries>(responsePrivateProfile);
                    int privateProfile = jsonPrivateProfile.Response.Players[0].CommunityVisibilityState;

                    if (privateProfile < 3 && privateProfileKick)
                    {
                        player.Kick(LangMessage("PrivateProfileKick", player.Id));
                        Puts(string.Format(LangMessage("PrivateProfileConsole"), player.Name, player.Id));

                        if (privateProfileBroadcast)
                            server.Broadcast(string.Format(LangMessage("PrivateProfileBroadcast"), player.Name));

                        return;
                    }

                    string requestXML = $"http://steamcommunity.com/profiles/{player.Id}/?xml=1";

                    webrequest.EnqueueGet(requestXML, (codeXML, responseXML) =>
                    {
                        if (!ValidRequest(codeXML))
                            return;

                        bool noProfile = responseXML.Contains("This user has not yet set up their Steam Community profile.");

                        if (noProfile && noProfileKick)
                        {
                            player.Kick(LangMessage("NoProfileKick", player.Id));
                            Puts(string.Format(LangMessage("NoProfileConsole"), player.Name, player.Id));

                            if (noProfileBroadcast)
                                server.Broadcast(string.Format(LangMessage("NoProfileBroadcast"), player.Name));

                            return;
                        }

                        if (!noProfile)
                        {
                            if (System.Convert.ToInt32(limitedAccountRegex.Match(responseXML).Groups[1].ToString()) == 1 && limitedAccountKick)
                            {
                                player.Kick(LangMessage("LimitedAccountKick", player.Id));
                                Puts(string.Format(LangMessage("LimitedAccountConsole"), player.Name, player.Id));

                                if (limitedAccountBroadcast)
                                    server.Broadcast(string.Format(LangMessage("LimitedAccountBroadcast"), player.Name));

                                return;
                            }
                        }
                        else
                        {
                            Puts(string.Format(LangMessage("LimitedAccountCheckFailed"), player.Name, player.Id));
                        }


                        string requestSharingGame = $"http://api.steampowered.com/IPlayerService/IsPlayingSharedGame/v0001/?key={steamAPIKey}&steamid={player.Id}&appid_playing={steamAppId}";

                        webrequest.EnqueueGet(requestSharingGame, (codeSharingGame, responseSharingGame) =>
                        {
                            if (!ValidRequest(codeSharingGame))
                                return;

                            IsPlayingSharedGame jsonSharingGame = JsonConvert.DeserializeObject<IsPlayingSharedGame>(responseSharingGame);

                            if (jsonSharingGame.Response.LenderSteamId > 0 && sharingGameKick)
                            {
                                player.Kick(LangMessage("SharingGameKick", player.Id));
                                Puts(string.Format(LangMessage("SharingGameConsole"), player.Name, player.Id));

                                if (sharingGameBroadcast)
                                    server.Broadcast(string.Format(LangMessage("SharingGameBroadcast"), player.Name));

                                return;
                            }

                            if (!(privateProfile < 3) && !(jsonSharingGame.Response.LenderSteamId > 0))
                            {
                                string requestHoursPlayed = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={steamAPIKey}&steamid={player.Id}";

                                webrequest.EnqueueGet(requestHoursPlayed, (codeHoursPlayed, responseHoursPlayed) =>
                                {
                                    if (!ValidRequest(codeHoursPlayed))
                                        return;

                                    GetOwnedGames jsonHoursPlayed = JsonConvert.DeserializeObject<GetOwnedGames>(responseHoursPlayed);
                                    int minutesPlayed = jsonHoursPlayed.Response.Games.Single(x => x.AppId == steamAppId).PlaytimeForever;
                                    float hoursPlayed = minutesPlayed / 60;
                                    float hoursNeeded = hoursPlayedThreshold - hoursPlayed;

                                    if (hoursPlayedThreshold > hoursPlayed && hoursPlayedKick)
                                    {
                                        player.Kick(string.Format(LangMessage("HoursPlayedKick", player.Id), hoursNeeded));
                                        Puts(string.Format(LangMessage("HoursPlayedConsole"), player.Name, player.Id, hoursNeeded));

                                        if (hoursPlayedBroadcast)
                                            server.Broadcast(string.Format(LangMessage("HoursPlayedBroadcast"), player.Name));

                                        return;
                                    }
                                }, this);
                            }
                            else
                                Puts(string.Format(LangMessage("HoursPlayedCheckFailed"), player.Name, player.Id));
                        }, this);
                    }, this);
                }, this);
            }, this);
        }

        #endregion
    }
}