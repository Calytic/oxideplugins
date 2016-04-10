// Reference: Newtonsoft.Json

using Oxide.Core;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using CodeHatch.Engine.Core.Networking;
using Newtonsoft.Json;
using CodeHatch.Engine.Networking;

namespace Oxide.Plugins
{

    [Info("Steam Profiler", "Mughisi", 1.0)]
    class SteamProfiler : ReignOfKingsPlugin
    {

        #region Configuration Data

        // Do not modify these values, to configure this plugin edit
        // 'SteamProfiler.json' in your server's config folder.
        // <drive>:\...\Save\oxide\config\

        private bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Server";
        private const string DefaultChatPrefixColor = "950415";
        private const string DefaultApiKey = "STEAM_API_KEY";
        private const bool DefaultLogToConsole = true;
        private const bool DefaultAnnounceToServer = false;
        private static readonly List<object> DefaultWhitelist = new List<object>();

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; private set; }
        public string ApiKey { get; private set; }
        public bool LogToConsole { get; private set; }
        public bool AnnounceToServer { get; private set; }
        public List<string> Whitelist { get; private set; }

        // VAC Ban options
        private const bool DefaultVacEnabled = true;
        private const int DefaultAllowedVacBans = 0;
        private const int DefaultVacBanMinimumAge = 365;

        public bool VacEnabled { get; private set; }
        public int AllowedVacBans { get; private set; }
        public int MinimumBanAge { get; private set; }

        // Family Share options
        private const bool DefaultShareEnabled = true;

        public bool ShareEnabled { get; private set; }

        // Private profile blocker options
        private const bool DefaultPrivateEnabled = true;

        public bool PrivateEnabled { get; private set; }

        // Messages
        private const string DefaultVacBanKickMessage = "VAC banned accounts are not allowed.";
        private const string DefaultVacBanKickAnnounce = "{0} was not allowed on the server because of one or more VAC bans.";
        private const string DefaultFamilyShareKickMessage = "Family Shared accounts are not allowed.";
        private const string DefaultFamilyShareKickAnnounce = "{0} was not allowed on the server because of a shared game.";
        private const string DefaultPrivateKickMessage = "Accounts with private profiles are not allowed.";
        private const string DefaultPrivateKickAnnounce = "{0} was not allowed on the server because of a private profile.";

        public string VacBanKickMessage { get; private set; }
        public string VacBanKickAnnounce { get; private set; }
        public string FamilyShareKickMessage { get; private set; }
        public string FamilyShareKickAnnounce { get; private set; }
        public string PrivateKickMessage { get; private set; }
        public string PrivateKickAnnounce { get; private set; }

        #endregion

        #region WebResponses

        internal class VacBanWebResponse
        {
            [JsonProperty("Players")]
            public Player[] Players { get; set; }

            internal class Player
            {
                [JsonProperty("SteamId")]
                public string SteamId { get; set; }

                [JsonProperty("CommunityBanned")]
                public bool CommunityBanned { get; set; }

                [JsonProperty("VACBanned")]
                public bool VACBanned { get; set; }

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

        internal class FamilyShareWebResponse
        {

            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {
                [JsonProperty("lender_steamid")]
                public string LenderSteamid { get; set; }
            }
        }

        internal class ProfileWebResponse
        {
            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {
                [JsonProperty("players")]
                public Player[] Players { get; set; }
            }

            public class Player
            {
                [JsonProperty("steamid")]
                public string Steamid;

                [JsonProperty("communityvisibilitystate")]
                public int Communityvisibilitystate;

                [JsonProperty("profilestate")]
                public int Profilestate;

                [JsonProperty("personaname")]
                public string Personaname;

                [JsonProperty("lastlogoff")]
                public int Lastlogoff;

                [JsonProperty("profileurl")]
                public string Profileurl;

                [JsonProperty("avatar")]
                public string Avatar;

                [JsonProperty("avatarmedium")]
                public string Avatarmedium;

                [JsonProperty("avatarfull")]
                public string Avatarfull;

                [JsonProperty("personastate")]
                public int Personastate;

                [JsonProperty("realname")]
                public string Realname;

                [JsonProperty("primaryclanid")]
                public string Primaryclanid;

                [JsonProperty("timecreated")]
                public int Timecreated;

                [JsonProperty("personastateflags")]
                public int Personastateflags;

                [JsonProperty("loccountrycode")]
                public string Loccountrycode;

                [JsonProperty("locstatecode")]
                public string Locstatecode;

                [JsonProperty("loccityid")]
                public int Loccityid;
            }
        }

        #endregion

        readonly Dictionary<ulong, string> blockedPlayersList = new Dictionary<ulong, string>();

        private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");

        void Loaded() => LoadConfigValues();

        protected override void LoadDefaultConfig() => Log("New configuration file created.");

        void LoadConfigValues()
        {
            // Settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);
            ApiKey = GetConfigValue("Settings", "SteamAPIKey", DefaultApiKey);
            LogToConsole = GetConfigValue("Settings", "LogToConsole", DefaultLogToConsole);
            AnnounceToServer = GetConfigValue("Settings", "AnnounceToServer", DefaultAnnounceToServer);

            // Options
            Whitelist = new List<string>();
            var tempWhitelist = GetConfigValue("Options", "Whitelist", DefaultWhitelist);

            // VAC Ban Blocker
            VacEnabled = GetConfigValue("VACBanBlocker", "Enabled", DefaultVacEnabled);
            AllowedVacBans = GetConfigValue("VACBanBlocker", "NumberOfAllowedVACBans", DefaultAllowedVacBans);
            MinimumBanAge = GetConfigValue("VACBanBlocker", "MinimumDaysSinceLastBan", DefaultVacBanMinimumAge);

            // Family Share Blocker
            ShareEnabled = GetConfigValue("FamilyShareBlocker", "Enabled", DefaultShareEnabled);

            // Private Profile Blocker
            PrivateEnabled = GetConfigValue("PrivateProfileBlocker", "Enabled", DefaultPrivateEnabled);

            // Messages
            VacBanKickMessage = GetConfigValue("Messages", "VACBanKickMessage", DefaultVacBanKickMessage);
            VacBanKickAnnounce = GetConfigValue("Messages", "VACBanKickServerAnnouncement", DefaultVacBanKickAnnounce);
            FamilyShareKickMessage = GetConfigValue("Messages", "FamilyShareKickMessage", DefaultFamilyShareKickMessage);
            FamilyShareKickAnnounce = GetConfigValue("Messages", "FamilyShareServerAnnouncement", DefaultFamilyShareKickAnnounce);
            PrivateKickMessage = GetConfigValue("Messages", "PrivateProfileKickMessage", DefaultPrivateKickMessage);
            PrivateKickAnnounce = GetConfigValue("Messages", "PrivateProfileKickServerAnnouncement", DefaultPrivateKickAnnounce);

            // Load the whitelist
            if (tempWhitelist != null)
                foreach (var entry in tempWhitelist)
                    Whitelist.Add(entry.ToString());

            if (!configChanged) return;
            SaveConfig();
            Log("Configuration file updated.");
        }

        ConnectionError OnUserApprove(ConnectionLoginData data)
        {
            if (!blockedPlayersList.ContainsKey(data.PlayerId) || Whitelist.Contains(data.PlayerId.ToString())) return ConnectionError.NoError;

            if (LogToConsole)
                Warning(string.Format(blockedPlayersList[data.PlayerId], data.PlayerName + " (" + data.PlayerId + ")"));

            return ConnectionError.ApprovalDenied;
        }

        void OnPlayerConnected(Player player)
        {
            if (ApiKey == DefaultApiKey || ApiKey == "")
            {
                Log("Error! No Steam API key found.");
                Log("You need to set your API key in the configuration file for this plugin to work!");
                Log("To obtain an API key browse to http://steamcommunity.com/dev/apikey");
                return;
            }

            var playerName = player.DisplayName;
            var steamId = player.Id.ToString();

            if (playerName == "Server" && steamId == "9999999999") return;

            if (Whitelist != null && Whitelist.Contains(steamId))
            {
                if (LogToConsole)
                    Log($"{playerName} with Steam Id {steamId} is whitelisted, allowing the player to join.");
                return;
            }

            string vacbanurl = $"http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={ApiKey}&steamids={steamId}";
            string familyshareurl = $"http://api.steampowered.com/IPlayerService/IsPlayingSharedGame/v0001/?key={ApiKey}&steamid={steamId}&appid_playing=344760";
            string profileurl = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={ApiKey}&steamids={steamId}";

            if (VacEnabled)
                webRequests.EnqueueGet(vacbanurl, (code, response) => IsVacBanned(code, response, player), this);
            if (ShareEnabled)
                webRequests.EnqueueGet(familyshareurl, (code, response) => IsFamilySharing(code, response, player), this);
            if (PrivateEnabled)
                webRequests.EnqueueGet(profileurl, (code, response) => HasPrivateProfile(code, response, player), this);
        }

        void IsVacBanned(int code, string response, Player player)
        {
            var playerName = player.DisplayName;
            var steamId = player.Id.ToString();

            switch (code)
            {
                case 200:
                    var json = JsonConvert.DeserializeObject<VacBanWebResponse>(response);
                    if (json.Players[0].VACBanned)
                    {
                        if (blockedPlayersList.ContainsKey(player.Id)) return;
                        var denyAccess = json.Players[0].NumberOfVACBans > AllowedVacBans || json.Players[0].DaysSinceLastBan < MinimumBanAge;

                        if (!denyAccess) return;

                        if (LogToConsole)
                            Warning(string.Format(VacBanKickAnnounce, playerName + " (" + steamId + ")"));

                        if (AnnounceToServer)
                            BroadcastMessage(VacBanKickAnnounce, player.DisplayName);

                        blockedPlayersList.Add(player.Id, VacBanKickMessage);
                        Server.Kick(player, VacBanKickMessage, false);
                    }
                    break;
                case 401:
                    Log("Webrequest failed, invalid Steam API key.");
                    break;
                case 404:
                case 503:
                    Log("Webrequest failed. Steam API unavailable.");
                    break;
                default:
                    Log($"Webrequest failed. Error code {code}.");
                    break;
            }
        }

        void IsFamilySharing(int code, string response, Player player)
        {
            var playerName = player.DisplayName;
            var steamId = player.Id.ToString();

            switch (code)
            {
                case 200:
                    var json = JsonConvert.DeserializeObject<FamilyShareWebResponse>(response);
                    if (json.Response.LenderSteamid != "0")
                    {
                        if (blockedPlayersList.ContainsKey(player.Id)) return;

                        if (LogToConsole)
                            Warning(string.Format(FamilyShareKickAnnounce, playerName + " (" + steamId + ")"));

                        if (AnnounceToServer)
                            BroadcastMessage(FamilyShareKickAnnounce, player.DisplayName);

                        blockedPlayersList.Add(player.Id, FamilyShareKickMessage);
                        Server.Kick(player, FamilyShareKickMessage, false);
                    }
                    break;
                case 401:
                    Log("Webrequest failed, invalid Steam API key.");
                    break;
                case 404:
                case 503:
                    Log("Webrequest failed. Steam API unavailable.");
                    break;
                default:
                    Log($"Webrequest failed. Error code {code}.");
                    break;
            }
        }

        void HasPrivateProfile(int code, string response, Player player)
        {
            var playerName = player.DisplayName;
            var steamId = player.Id.ToString();

            switch (code)
            {
                case 200:
                    var json = JsonConvert.DeserializeObject<ProfileWebResponse>(response);
                    if (json.Response.Players[0].Communityvisibilitystate != 3)
                    {
                        if (blockedPlayersList.ContainsKey(player.Id)) return;

                        if (LogToConsole)
                            Warning(string.Format(PrivateKickAnnounce, playerName + " (" + steamId + ")"));

                        if (AnnounceToServer)
                            BroadcastMessage(PrivateKickAnnounce, player.DisplayName);

                        blockedPlayersList.Add(player.Id, PrivateKickMessage);
                        Server.Kick(player, PrivateKickMessage, false);
                    }
                    break;
                case 401:
                    Log("Webrequest failed, invalid Steam API key.");
                    break;
                case 404:
                case 503:
                    Log("Webrequest failed. Steam API unavailable.");
                    break;
                default:
                    Log($"Webrequest failed. Error code {code}.");
                    break;
            }
        }

        #region Helper methods

        void Log(string msg) => Puts($"{Title} : {msg}");

        void Warning(string msg) => PrintWarning($"{Title} : {msg}");

        void BroadcastMessage(string message, params object[] args) => Server.BroadcastMessage($"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}", args);

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion
    }

}
