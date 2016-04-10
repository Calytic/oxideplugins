
using System;
using System.Collections.Generic;

using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities.Players;
using CodeHatch.Networking.Events.Players;
using CodeHatch.Networking.Events.Social;

namespace Oxide.Plugins
{
    [Info("Command Logger", "Mughisi", "1.0.2", ResourceId = 1103)]
    public class CommandLogger : ReignOfKingsPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'CommandLogger.json' in your server's config folder.
        // <drive>:\...\save\oxide\config\

        private bool configChanged;

        // Plugin settings
        private const bool DefaultChatLogEnabled = true;
        private const bool DefaultGuildChatLogEnabled = true;
        private const bool DefaultCommandLogEnabled = true;

        public bool ChatLogEnabled { get; private set; }
        public bool GuildChatLogEnabled { get; private set; }
        public bool CommandLogEnabled { get; private set; }

        #endregion

        private void Loaded() => LoadConfigData();

        protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");

        private void OnPlayerCommand(Player player, string command, string[] args)
        {
            if (!CommandLogEnabled) return;
            Log("commands", $"{player.Name} ran the command /{command} {args.JoinToString(" ")}");
        }

        private void OnPlayerChat(PlayerEvent e)
        {
            var chatEvent = e as PlayerChatEvent;
            if (chatEvent != null)
            {
                if (ChatLogEnabled)
                    Log("chat", chatEvent.Player.Name + " : " + chatEvent.Message);
            }

            var guildchatEvent = e as GuildMessageEvent;
            if (guildchatEvent != null)
            {
                var guild = e.Player.GetGuild();
                if (GuildChatLogEnabled)
                    Log("guild", "[" + guild.Name + "] " + guildchatEvent.Player.Name + " : " + guildchatEvent.Message);
            }
        }
        private void LoadConfigData()
        {
            // Plugin settings
            ChatLogEnabled = GetConfigValue("Settings", "ChatLogEnabled", DefaultChatLogEnabled);
            GuildChatLogEnabled = GetConfigValue("Settings", "GuildChatLogEnabled", DefaultGuildChatLogEnabled);
            CommandLogEnabled = GetConfigValue("Settings", "CommandLogEnabled", DefaultCommandLogEnabled);

            if (!configChanged) return;
            PrintWarning("The configuration file was updated!");
            SaveConfig();
        }

        private static void Log(string type, string msg) => LogFileUtil.LogTextToFile($"..\\Saves\\oxide\\logs\\{type}_{DateTime.Now.ToString("dd-MM-yyyy")}.txt", $"[{DateTime.Now.ToString("h:mm:ss tt")}] {msg}\r\n");

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
