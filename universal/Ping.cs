using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Ping", "Wulf/lukespragg", "1.7.0", ResourceId = 1921)]
    [Description("Ping command and automatic kicking of players with high pings")]

    class Ping : CovalencePlugin
    {
        #region Initialization

        const string permBypass = "ping.bypass";

        bool highPingKick;
        bool kickNotices;
        bool repeatChecking;
        bool warnBeforeKick;

        int highPingLimit;
        int kickGracePeriod;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["High Ping Kick (true/false)"] = highPingKick = GetConfig("High Ping Kick (true/false)", true);
            Config["Kick Notice Messages (true/false)"] = kickNotices = GetConfig("Kick Notice Messages (true/false)", true);
            Config["Repeat Checking (true/false)"] = repeatChecking = GetConfig("Repeat Checking (true/false)", true);
            Config["Warn Before Kicking (true/false)"] = warnBeforeKick = GetConfig("Warn Before Kicking (true/false)", true);

            // Settings
            Config["High Ping Limit (Milliseconds)"] = highPingLimit = GetConfig("High Ping Limit (Milliseconds)", 200);
            Config["Kick Grace Period (Seconds)"] = kickGracePeriod = GetConfig("Kick Grace Period (Seconds)", 30);

            // Cleanup
            Config.Remove("AdminExcluded");
            Config.Remove("HighPingKick");
            Config.Remove("KickNotices");
            Config.Remove("PingLimit");
            Config.Remove("RepeatCheck");
            Config.Remove("WarnBeforeKick");

            SaveConfig();
        }

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kicked"] = "{0} kicked for high ping ({1}ms)",
                ["KickWarning"] = "You will be kicked in {0} seconds if your ping is not lowered",
                ["Ping"] = "You have a ping of {0}ms",
                ["PingTooHigh"] = "Ping is too high: {0}ms"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kicked"] = "{0} expulsÃ© pour ping Ã©levÃ© ({1} ms)",
                ["KickWarning"] = "Vous sera lancÃ© dans {0} secondes si votre ping nâest pas abaissÃ©",
                ["Ping"] = "Vous avez un ping de {0} ms",
                ["PingTooHigh"] = "Ping est trop Ã©levÃ©eÂ : {0} ms"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kicked"] = "{0} gekickt fÃ¼r hohen Ping ({1} ms)",
                ["KickWarning"] = "Sie werden in {0} Sekunden gekickt wenn Ihr Ping nicht gesenkt wird",
                ["Ping"] = "Sie haben einen Ping von {0} ms",
                ["PingTooHigh"] = "Ping ist zu hoch: {0} ms"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kicked"] = "{0} Ð½Ð¾Ð³Ð°Ð¼Ð¸ Ð²ÑÑÐ¾ÐºÐ¸Ð¹ Ð¿Ð¸Ð½Ð³ ({1} ms)",
                ["KickWarning"] = "ÐÐ°Ð¼ Ð±ÑÐ´ÐµÑ Ð½Ð¾Ð³Ð°Ð¼Ð¸ Ð² {0} ÑÐµÐºÑÐ½Ð´ ÐµÑÐ»Ð¸ Ð¿Ð¸Ð½Ð³ Ð½Ðµ Ð¾Ð¿ÑÑÑÐ¸Ð»",
                ["Ping"] = "Ð£ Ð²Ð°Ñ Ð¿Ð¸Ð½Ð³ {0} ms",
                ["PingTooHigh"] = "ÐÐ¸Ð½Ð³ ÑÐ»Ð¸ÑÐºÐ¾Ð¼ Ð²ÑÑÐ¾ÐºÐ°: {0} ms"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Kicked"] = "{0} expulsado por ping alto ({1} ms)",
                ["KickWarning"] = "Usted va ser pateado en {0} segundos si el ping no baja",
                ["Ping"] = "Tienes un ping de {0} ms",
                ["PingTooHigh"] = "Ping es demasiado alto: {0} ms"
            }, this, "es");
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permBypass, this);
        }

        #endregion

        #region Game Hooks

        void OnServerInitialized()
        {
            // Loop through all players and run ping check
            foreach (var player in players.Connected) timer.Once(5f, () => PingCheck(player));
        }

        void OnServerSave()
        {
            // Check if repeating checking is enabled
            if (!repeatChecking) return;

            // Loop through all player sand run ping check
            foreach (var player in players.Connected) timer.Once(5f, () => PingCheck(player));
        }

        void OnUserConnected(IPlayer player) => timer.Once(10f, () => PingCheck(player));

        #endregion

        #region Ping Checking

        void PingCheck(IPlayer player, bool warned = false)
        {
            // Check if player is connected or has permission to bypass
            if (!player.IsConnected || player.HasPermission(permBypass)) return;

            var ping = player.Ping;
            if (ping < highPingLimit || !highPingKick) return;

            // Check if warning should be given
            if (warnBeforeKick && !warned)
            {
                // Warn player with grace period
                player.Message(Lang("KickWarning", player.Id, kickGracePeriod));
                timer.Once(kickGracePeriod, () => PingCheck(player, true));
            }
            else
                // Kick player
                PingKick(player, ping.ToString());
        }

        void PingKick(IPlayer player, string ping)
        {
            // Kick player and show reason
            player.Kick(Lang("PingTooHigh", player.Id, ping));

            // Check if kick notices are enabled
            if (!kickNotices) return;

            // Show kick notice in console/log and chat
            Puts(Lang("Kicked", null, player.Name, ping));
            server.Broadcast(Lang("Kicked", null, player.Name, ping));
        }

        [Command("ping", "pong")]
        void PingCommand(IPlayer player, string command, string[] args) => player.Reply(Lang("Ping", player.Id, player.Ping));

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
