// Reference: Newtonsoft.Json

using Oxide.Core;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Oxide.Plugins
{

    [Info("Family Share Blocker", "Mughisi", 1.0)]
    class FamilyShareBlocker : RustLegacyPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'FamilyShareBlocker.json' in your server's config folder.
        // <drive>:\...\server\<server identity>\oxide\config\

        private bool configChanged = false;

        // Plugin settings
        string defaultChatPrefix = "Steam Profiler";
        string defaultChatPrefixColor = "008000ff";
        string defaultAPIKey = "STEAM_API_KEY";

        string chatPrefix;
        string chatPrefixColor;
        string APIKey;

        // Plugin options
        bool defaultLogToConsole = true;
        List<object> defaultWhitelist = new List<object>();

        bool logToConsole;
        List<string> whitelist = new List<string>();

        #endregion

        public class WebResponse
        {

            [JsonProperty("response")]
            public Content Response { get; set; }

            public class Content
            {

                [JsonProperty("lender_steamid")]
                public string LenderSteamid { get; set; }
            }
        }

        void Loaded() => LoadConfigValues();

        void Init()
        {
            if (!permission.PermissionExists("CanUseGodmode")) permission.RegisterPermission("CanBeGod", this);
        }
        protected override void LoadDefaultConfig() => Log("New configuration file created.");

        void LoadConfigValues()
        {
            APIKey = Convert.ToString(GetConfigValue("Settings", "SteamAPIKey", defaultAPIKey));
            logToConsole = Convert.ToBoolean(GetConfigValue("Options", "LogToConsole", defaultLogToConsole));
            var list = GetConfigValue("Options", "Whitelist", defaultWhitelist);

            whitelist.Clear();
            foreach (object steamID in list as List<object>)
                whitelist.Add(steamID.ToString());

            if (configChanged)
            {
                SaveConfig();
                Log("Configuration file updated.");
            }
        }

        void OnPlayerConnected(NetUser netuser)
        {
            if (APIKey == defaultAPIKey || APIKey == "")
            {
                Log("Error! No Steam API key found.");
                Log("You need to set your API key in the configuration file for this plugin to work!");
                Log("To obtain an API key browse to http://steamcommunity.com/dev/apikey");
                return;
            }

            string playerName = netuser.displayName;
            string steamID = Convert.ToString(netuser.userID);

            if (whitelist.Contains(steamID))
            {
                if (logToConsole)
                    Log($"{playerName} ({steamID}) is whitelisted, allowing the player to join.");
                return;
            }

            string url = $"http://api.steampowered.com/IPlayerService/IsPlayingSharedGame/v0001/?key={APIKey}&steamid={steamID}&appid_playing=252490";
            Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (code, response) => IsFamilySharing(code, response, netuser), this);
        }

        void IsFamilySharing(int code, string response, NetUser netuser)
        {
            string playerName = netuser.displayName;
            string steamID = Convert.ToString(netuser.userID);

            switch (code)
            {
                case 200:
                    var json = JsonConvert.DeserializeObject<WebResponse>(response);
                    if (json.Response.LenderSteamid != "0")
                    {
                        if (logToConsole)
                            Log($"{playerName} ({steamID}) is using a family shared account, kicking player...");

                        netuser.Kick(NetError.Facepunch_Whitelist_Failure, true);
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

        void Log(string message) => Puts("{0} : {1}", Title, message);

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;

            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }

            if (!data.TryGetValue(setting, out value))
            {
                value = defaultValue;
                data[setting] = value;
                configChanged = true;
            }

            return value;
        }

        #endregion
    }

}
