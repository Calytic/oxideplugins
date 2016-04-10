
using System;
using System.Collections.Generic;
using System.Linq;
using CodeHatch.Build;
using CodeHatch.Engine.Networking;

namespace Oxide.Plugins
{
    [Info("Announcer", "Mughisi", "1.1.1", ResourceId = 1003)]
    public class Announcer : ReignOfKingsPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'Announcer.json' in your server's config folder.
        // <drive>:\...\save\oxide\config\

        bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Server";
        private const string DefaultChatPrefixColor = "950415";

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; private set; }

        // Join/Leave watcher settings
        private const bool DefaultWatcherEnabled = true;
        private const bool DefaultJoinEnabled = true;
        private const bool DefaultLeaveEnabled = true;
        private const bool DefaultShowChatPrefixW = true;
        private const bool DefaultLog = true;
        private static readonly List<object> DefaultHideList = new List<object>();

        public bool WatcherEnabled { get; private set; }
        public bool JoinEnabled { get; private set; }
        public bool LeaveEnabled { get; private set; }
        public bool ShowChatPrefixW { get; private set; }
        public bool LogToConsole { get; private set; }
        public List<string> HideList { get; private set; }

        // Broadcaster settings
        private const bool DefaultBroadcasterEnabled = true;
        private static readonly List<object> DefaultBroadcasts = new List<object> { "You can visit our forums at www.oxidemod.org!", "Don't forget to bring your friends!", "Type /rules to see our server's rules!" };
        private const int DefaultBroadcastInterval = 120;
        private const bool DefaultShowChatPrefixB = true;
        private const bool DefaultShowRandom = false;

        public bool BroadcasterEnabled { get; private set; }
        public List<string> Broadcasts { get; private set; }
        public int BroadcastInterval { get; private set; }
        public bool ShowChatPrefixB { get; private set; }

        public bool ShowRandom { get; private set; }


        // Rules settings
        private const bool DefaultRulesEnabled = true;
        private static readonly List<object> DefaultRules = new List<object> { "1. Do not cheat on our server.", "2. Speak English in chat at all times.", "3. When Mughisi says jump, you ask how high.", "Not complying with these rules may result in a ban at any given time." };
        private const bool DefaultShowChatPrefixR = true;

        public bool RulesEnabled { get; private set; }
        public List<string> Rules { get; private set; }
        public bool ShowChatPrefixR { get; private set; }

        // Custom commands
        private const bool DefaultShowChatPrefixC = true;
        private static readonly Dictionary<string, object> DefaultCustomCommands = new Dictionary<string, object>();

        public bool ShowChatPrefixC { get; private set; }
        public Dictionary<string, CustomCommand> CustomCommands { get; private set; }

        // Plugin messages
        private const string DefaultJoined = "{0} has joined the server!";
        private const string DefaultLeft = "{0} has left the server";

        public string Joined { get; private set; }
        public string Left { get; private set; }

        #endregion

        public class CustomCommand
        {
            public string Command;
            public List<string> Messages;

            public CustomCommand()
            {
            }

            public CustomCommand(string command, List<string> messages)
            {
                Command = command;
                Messages = messages;
            }
        }

        readonly Random random = new Random();

        int previouslyBroadcastedMessage = -1;

        void Loaded()
        {
            LoadConfigData();
            
            if (BroadcasterEnabled)
                timer.Repeat(BroadcastInterval, 0, BroadcastMessage);

            if (RulesEnabled)
                cmd.AddChatCommand("rules", this, "ShowRules");
        }

        protected override void LoadDefaultConfig() => Warning("New configuration file created.");

        private void CustomChatCommand(Player player, string command, string[] args)
        {
            foreach (var msg in CustomCommands[command.ToLower()].Messages)
            {
                var message = msg;
                if (ShowChatPrefixC)
                    message = $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}";

                PrintToChat(player, message);
            }
        }

        private void OnPlayerConnected(Player player)
        {
            if (player.Name == "Server" && player.Id == 9999999999) return;

            if (!WatcherEnabled) return;
            var message = string.Format(Joined, player.Name);

            if (LogToConsole)
                Log(message);

            if (!JoinEnabled) return;

            if (HideList.Contains(player.Id.ToString())) return;

            if (ShowChatPrefixW)
                message = $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}";

            PrintToChat(message);
        }

        private void OnPlayerDisconnected(Player player)
        {
            if (!WatcherEnabled) return;
            var message = string.Format(Left, player.Name);

            if (LogToConsole)
                Log(message);

            if (!LeaveEnabled) return;

            if (HideList.Contains(player.Id.ToString())) return;

            if (ShowChatPrefixW)
                message = $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}";

            PrintToChat(message);
        }
        
        void BroadcastMessage()
        {
            string message;
            if (ShowRandom && Broadcasts.Count > 2)
            {
                var randomMessage = random.Next(Broadcasts.Count);
                while (randomMessage == previouslyBroadcastedMessage)
                    randomMessage = random.Next(Broadcasts.Count);

                previouslyBroadcastedMessage = randomMessage;

                message = Broadcasts[randomMessage];
            }
            else
            {
                previouslyBroadcastedMessage++;
                if (previouslyBroadcastedMessage >= Broadcasts.Count) previouslyBroadcastedMessage = 0;
                message = Broadcasts[previouslyBroadcastedMessage];
            }
            if (ShowChatPrefixB)
                message = $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}";

            PrintToChat(message);
        }

        void ShowRules(Player player, string cmd, string[] args)
        {
            foreach (var rule in Rules)
            {
                var message = rule;

                if (ShowChatPrefixR)
                    message = $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}";

                PrintToChat(player, message);
            }
        }

        [ChatCommand("version")]
        private void VersionCommand(Player player, string command, string[] args)
        {
            PrintToChat(player, "Oxide version: {0}, Reign of Kings version: {1} ", Core.OxideMod.Version.ToString(), GameInfo.VersionName);
        }

        void Log(string msg) => Puts($"{Title} : {msg}");

        void Warning(string msg) => PrintWarning($"{Title} : {msg}");

        void LoadConfigData()
        {
            // Plugin settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);

            // Join/Leave watcher settings
            WatcherEnabled = GetConfigValue("ConnectionSettings", "Enabled", DefaultWatcherEnabled);
            JoinEnabled = GetConfigValue("ConnectionSettings", "ShowJoinMessages", DefaultJoinEnabled);
            LeaveEnabled = GetConfigValue("ConnectionSettings", "ShowLeaveMessages", DefaultLeaveEnabled);
            ShowChatPrefixW = GetConfigValue("ConnectionSettings", "ShowChatPrefix", DefaultShowChatPrefixW);
            LogToConsole = GetConfigValue("ConnectionSettings", "Log", DefaultLog);
            var tempHideList = GetConfigValue("ConnectionSettings", "ExcludePlayers", DefaultHideList);

            // Broadcaster settings
            BroadcasterEnabled = GetConfigValue("BroadcasterSettings", "Enabled", DefaultBroadcasterEnabled);
            var tempBroadcasts = GetConfigValue("BroadcasterSettings", "Messages", DefaultBroadcasts);
            BroadcastInterval = GetConfigValue("BroadcasterSettings", "Interval", DefaultBroadcastInterval);
            ShowChatPrefixB = GetConfigValue("BroadcasterSettings", "ShowChatPrefix", DefaultShowChatPrefixB);
            ShowRandom = GetConfigValue("BroadcasterSettings", "ShowInRandomOrder", DefaultShowRandom);

            // Rules settings
            RulesEnabled = GetConfigValue("RulesSettings", "Enabled", DefaultRulesEnabled);
            var tempRules = GetConfigValue("RulesSettings", "Rules", DefaultRules);
            ShowChatPrefixR = GetConfigValue("RulesSettings", "ShowChatPrefix", DefaultShowChatPrefixR);

            // Custom commands
            ShowChatPrefixC = GetConfigValue("CustomCommands", "ShowChatPrefix", DefaultShowChatPrefixC);
            var tempCustomCommands = GetConfigValue("CustomCommands", "Commands", DefaultCustomCommands);
            // Plugin messages
            Joined = GetConfigValue("Messages", "PlayerJoined", DefaultJoined);
            Left = GetConfigValue("Messages", "PlayerLeft", DefaultLeft);

            // Handle all config lists.
            HideList = new List<string>();
            if (tempHideList != null)
                foreach (var str in tempHideList)
                    HideList.Add(str.ToString());

            Broadcasts = new List<string>();
            if (tempBroadcasts != null)
                foreach (var str in tempBroadcasts)
                    Broadcasts.Add(str.ToString());

            Rules = new List<string>();
            if (tempRules != null)
                foreach (var str in tempRules)
                    Rules.Add(str.ToString());

            CustomCommands = new Dictionary<string, CustomCommand>();
            if (tempCustomCommands != null)
            {
                foreach (var obj in tempCustomCommands)
                {
                    var command = obj.Key;

                    var content = obj.Value as List<object>;
                    if (content == null) continue;
                    var messages = content.Select(msg => msg.ToString()).ToList();
                    CustomCommands.Add(command.ToLower(), new CustomCommand(command, messages));
                }
            }

            foreach(var command in CustomCommands)
                cmd.AddChatCommand(command.Key, this, "CustomChatCommand");

            if (!configChanged) return;
            Warning("The configuration file was updated!");
            SaveConfig();
        }

        private T GetConfigValue<T>(string category, string setting, T defaultValue)
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
    }
}
