using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Logger", "Wulf/lukespragg", "1.2.0", ResourceId = 670)]
    [Description("Configurable logging of chat, commands, and more")]

    class Logger : RustPlugin
    {
        #region Initialization

        List<object> exclusions;
        bool logChat;
        bool logCommands;
        bool logConnections;
        bool logRespawns;
        bool logToConsole;

        protected override void LoadDefaultConfig()
        {
            Config["Exclusions"] = exclusions = GetConfig("Exclusions", new List<object>
            {
                "/help", "/version", "chat.say", "craft.add", "craft.canceltask", "global.kill", "global.respawn",
                "global.respawn_sleepingbag", "global.status", "global.wakeup", "inventory.endloot", "inventory.unlockblueprint"
            });
            Config["LogChat"] = logChat = GetConfig("LogChat", false);
            Config["LogCommands"] = logCommands = GetConfig("LogCommands", true);
            Config["LogConnections"] = logConnections = GetConfig("LogConnections", true);
            Config["LogRespawns"] = logRespawns = GetConfig("LogRespawns", false);
            Config["LogToConsole"] = logToConsole = GetConfig("LogToConsole", true);
            SaveConfig();
        }

        void Init()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            LoadDefaultMessages();

            if (!logChat) Unsubscribe("OnPlayerChat");
            if (!logCommands) Unsubscribe("OnServerCommand");
            if (!logConnections) Unsubscribe("OnPlayerInit");
            if (!logRespawns) Unsubscribe("OnPlayerRespawned");
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) ran chat command: {2}",
                ["Connected"] = "{0} ({1}) connected from {2}",
                ["ConsoleCommand"] = "{0} ({1}) ran console command: {2} {3}",
                ["Disconnected"] = "{0} ({1}) disconnected",
                ["Respawned"] = "{0} ({1}) respawned at {2}"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) a couru chat commandeÂ : {2}",
                ["Connected"] = "{0} ({1}) reliant {2}",
                ["ConsoleCommand"] = "{0} ({1}) a couru la console de commandeÂ : {3} {2}",
                ["Disconnected"] = "{0} ({1}) dÃ©connectÃ©",
                ["Respawned"] = "{0} ({1}) rÃ©apparaÃ®tre Ã  {2}"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) lief Chat-Befehl: {2}",
                ["Connected"] = "{0} ({1}) {2} verbunden",
                ["ConsoleCommand"] = "{0} ({1}) lief Konsole Befehl: {2} {3}",
                ["Disconnected"] = "{0} ({1}) nicht getrennt",
                ["Respawned"] = "{0} ({1}) bereits am {2}"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) Ð¿Ð¾Ð±ÐµÐ¶Ð°Ð» ÑÐ°Ñ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ: {2}",
                ["Connected"] = "{0} ({1}) Ð¸Ð· {2}",
                ["ConsoleCommand"] = "{0} ({1}) Ð¿Ð¾Ð±ÐµÐ¶Ð°Ð» ÐºÐ¾Ð¼Ð°Ð½Ð´Ð° ÐºÐ¾Ð½ÑÐ¾Ð»Ð¸: {2} {3}",
                ["Disconnected"] = "{0} ({1}) Ð¾ÑÐºÐ»ÑÑÐµÐ½",
                ["Respawned"] = "{0} ({1}) Ð²Ð¾Ð·ÑÐ¾Ð´Ð¸ÑÑÑ Ð² {2}"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) funcionÃ³ el comando chat: {2}",
                ["Connected"] = "{0} ({1}) conectado de {2}",
                ["ConsoleCommand"] = "{0} ({1}) funcionÃ³ el comando de consola: {2} {3}",
                ["Disconnected"] = "{0} ({1}) desconectado",
                ["Respawned"] = "{0} ({1}) hizo en {2}"
            }, this, "es");
        }

        #endregion

        #region Logging

        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (!logChat || player == null) return;

            var args = arg.GetString(0, "text");
            Log("chat", $"{player.displayName} ({player.userID}): {args}");
        }

        void OnPlayerConnected(Network.Message packet)
        {
            if (!logConnections) return;
            var con = packet.connection;
            Log("connections", Lang("Connected", null, con.username, con.userid, IpAddress(con.ipaddress)));
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (!logConnections) return;
            Log("connections", Lang("Disconnected", null, player.displayName, player.UserIDString));
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (!logRespawns) return;
            Log("respawns", Lang("Respawned", null, player.displayName, player.userID, player.transform.position));
        }

        void OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (!logCommands || arg.connection == null) return;

            var command = arg.cmd.namefull;
            var args = arg.GetString(0);

            if (args.StartsWith("/") && !exclusions.Contains(args))
                Log("commands", Lang("ChatCommand", null, arg.connection.username, arg.connection.userid, args));
            if (command != "chat.say" && !exclusions.Contains(command))
                Log("commands", Lang("ConsoleCommand", null, arg.connection.username, arg.connection.userid, command, arg.ArgsStr));
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        static string IpAddress(string ip) => Regex.Replace(ip, @":{1}[0-9]{1}\d*", "");

        void Log(string fileName, string text)
        {
            var dateTime = DateTime.Now.ToString("yyyy-MM-dd");
            ConVar.Server.Log($"oxide/logs/{Title.ToLower()}-{fileName}_{dateTime}.txt", text);
            if (logToConsole) Puts(text);
        }

        #endregion
    }
}
