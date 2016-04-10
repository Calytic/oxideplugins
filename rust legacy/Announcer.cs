
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Server Information Announcer", "Mughisi", "1.0.0")]
    public class Announcer : RustLegacyPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'ServerInfo.json' in your server's config folder.
        // <drive>:\...\oxide\config\

        bool configChanged;
        bool configCreated;

        // Plugin settings
        string defaultChatPrefix = "Server";

        string chatPrefix;

        // Join/Leave watcher settings
        bool defaultWatcherEnabled = true;
        bool defaultShowChatPrefixW = true;
        bool defaultLog = true;

        bool watcherEnabled;
        bool showChatPrefixW;
        bool log;

        // Broadcaster settings
        bool defaultBroadcasterEnabled = true;
        List<object> defaultBroadcasts = new List<object> { "You can visit our forums at www.oxidemod.org!", "Don't forget to bring your friends!", "Type /rules to see our server's rules!" };
        int defaultBroadcastInterval = 30;
        bool defaultShowChatPrefixB = true;

        bool broadcasterEnabled;
        List<string> broadcasts = new List<string>();
        int broadcastInterval;
        bool showChatPrefixB;

        // Rules settings
        bool defaultRulesEnabled = true;
        List<object> defaultRules = new List<object> { "1. Do not cheat on our server.", "2. Speak English in chat at all times.", "3. When Mughisi says jump, you ask how high.", "Not complying with these rules may result in a ban at any given time." };
        bool defaultShowChatPrefixR = true;

        bool rulesEnabled;
        List<string> rules = new List<string>();
        bool showChatPrefixR;

        // Plugin messages
        string defaultJoined = "{0} has joined the server!";
        string defaultLeft = "{0} has left the server";

        string joined;
        string left;

        #endregion

        Random random = new Random();

        int previouslyBroadcastedMessage = -1;

        void Loaded()
        {
            LoadConfigData();

            if (broadcasterEnabled)
                timer.Repeat(broadcastInterval, 0, () => BroadcastMessage());

            if (rulesEnabled)
                cmd.AddChatCommand("rules", this, "ShowRules");
        }

        protected override void LoadDefaultConfig()
        {
            configCreated = true;
            Warning("New configuration file created.");
        }

        private void OnPlayerConnected(NetUser player)
        {
            if (!watcherEnabled) return;
            var message = string.Format(joined, player.displayName);

            if (log)
                Log(message);

            if (showChatPrefixW)
                BroadcastMessage(chatPrefix, message);
            else
                BroadcastMessage(message);
        }

        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            if (!watcherEnabled) return;
            var netUser = player.GetLocalData<NetUser>();
            var message = string.Format(left, netUser.displayName);

            if (log)
                Log(message);

            if (showChatPrefixW)
                BroadcastMessage(chatPrefix, message);
            else
                BroadcastMessage(message);
        }

        void BroadcastMessage()
        {
            int randomMessage = random.Next(broadcasts.Count);
            while (randomMessage == previouslyBroadcastedMessage)
                randomMessage = random.Next(broadcasts.Count);

            previouslyBroadcastedMessage = randomMessage;
            var message = broadcasts[randomMessage];

            if (showChatPrefixB)
                 BroadcastMessage(chatPrefix, message);
            else
                BroadcastMessage(message);
        }

        void ShowRules(NetUser player, string cmd, string[] args)
        {
            foreach (var rule in rules)
            {
                var message = rule;

                if (showChatPrefixR)
                    SendMessage(player, chatPrefix, message);
                else
                    SendMessage(player, message);
            }
        }

        void Log(string msg) => Puts($"{Title} : {msg}");

        void Warning(string msg) => PrintWarning($"{Title} : {msg}");

        string QuoteSafe(string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd(new char[] { '\\' }) + "\"";

        void SendMessage(NetUser netUser, string name, string message = null)
        {
            if (message == null)
            {
                message = name;
                name = "Server";
            }

            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"chat.add {QuoteSafe(name)} {QuoteSafe(message)}");
        }

        void BroadcastMessage(string name, string message = null)
        {
            if (message == null)
            {
                message = name;
                name = "Server";
            }
            ConsoleNetworker.Broadcast($"chat.add {QuoteSafe(name)} {QuoteSafe(message)}");
        }

        void LoadConfigData()
        {
            // Plugin settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));

            // Join/Leave watcher settings
            watcherEnabled = Convert.ToBoolean(GetConfigValue("ConnectionSettings", "Enabled", defaultWatcherEnabled));
            showChatPrefixW = Convert.ToBoolean(GetConfigValue("ConnectionSettings", "ShowChatPrefix", defaultShowChatPrefixW));
            log = Convert.ToBoolean(GetConfigValue("ConnectionSettings", "Log", defaultLog));

            // Broadcaster settings
            broadcasterEnabled = Convert.ToBoolean(GetConfigValue("BroadcasterSettings", "Enabled", defaultBroadcasterEnabled));
            var tempbroadcasts = GetConfigValue("BroadCasterSettings", "BroadcastMessages", defaultBroadcasts) as List<object>;
            broadcastInterval = Convert.ToInt16(GetConfigValue("BroadcasterSettings", "Interval", defaultBroadcastInterval));
            showChatPrefixB = Convert.ToBoolean(GetConfigValue("BroadcasterSettings", "ShowChatPrefix", defaultShowChatPrefixB));

            // Rules settings
            rulesEnabled = Convert.ToBoolean(GetConfigValue("RulesSettings", "Enabled", defaultRulesEnabled));
            var temprules = GetConfigValue("RulesSettings", "Rules", defaultRules);
            showChatPrefixR = Convert.ToBoolean(GetConfigValue("RulesSettings", "ShowChatPrefix", defaultShowChatPrefixR));

            // Plugin messages
            joined = Convert.ToString(GetConfigValue("Messages", "PlayerJoined", defaultJoined));
            left = Convert.ToString(GetConfigValue("Messages", "PlayerLeft", defaultLeft));

            // Handle broadcaster & rules lists
            broadcasts.Clear();
            foreach (var str in tempbroadcasts)
                broadcasts.Add(str.ToString());

            rules.Clear();
            foreach (var str in temprules as List<object>)
                rules.Add(str.ToString());

            if (configChanged)
            {
                Warning("The configuration file was updated!");
                SaveConfig();
            }
        }

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
    }
}
