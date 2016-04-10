
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Player corpse duration modifier", "Mughisi", "2.0.2", ResourceId = 778)]
    [Description("Allows the server owner to set the time that it takes for corpses to disappear.")]
    public class CorpseDuration : RustPlugin
    {

        #region Configuration Data

        // Do not modify these values, to configure this plugin edit 'CorpseDuration.json' in your
        // server's config folder (<drive>:\...\server\<server identity>\oxide\config\) or use the
        // in-game commands provided by the plugin.

        private bool configChanged;

        #region Plugin settings

        public string ChatPrefix { get; private set; } = "Info";
        public string ChatPrefixColor { get; private set; } = "red";
        public bool UseChatPrefix { get; private set; } = true;

        #endregion

        #region Plugin options

        public float Duration { get; private set; } = 300f;

        #endregion

        #endregion

        #region Oxide Hooks

        private void Loaded()
        {
            LoadConfiguration();
            LoadMessages();
            permission.RegisterPermission("corpseduration.modify", this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");

        private void OnEntitySpawned(BaseEntity entity) => ResetTime(entity);

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo info) => ResetTime(entity);

        private void OnLootEntityEnd(BasePlayer looter, BaseEntity entity) => ResetTime(entity);

        private void SendHelpText(BasePlayer player) => SendChatMessage(player, "Setting", Math.Round(Duration / 60, 1));

        #endregion

        #region Chat commands

        [ChatCommand("corpsetime")]
        private void ModifyChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, "corpseduration.modify") || args.Length == 0)
            {
                SendChatMessage(player, "Setting", Math.Round(Duration / 60, 1));
                return;
            }

            float duration;
            if (!float.TryParse(args[0], out duration))
            {
                SendChatMessage(player, "SyntaxError", "/corpsetime");
                return;
            }

            if (duration < 0)
            {
                SendChatMessage(player, "SyntaxError", "/corpsetime");
                return;
            }

            Duration = duration * 60;
            SetConfigValue("Options", "Duration", Duration);

            SendChatMessage(player, "Modify", Math.Round(Duration / 60, 1));
        }

        #endregion

        #region Console commands

        [ConsoleCommand("corpse.time")]
        private void ModifyConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (!HasPermission(arg, "corpseduration.modify") || !arg.HasArgs())
            {
                arg.ReplyWith(string.Format(GetMessage("Setting", arg?.connection?.userid.ToString()), Math.Round(Duration / 60, 1)));
                return;
            }

            var duration = arg.GetFloat(0, -1);
            if (duration < 0)
            {
                arg.ReplyWith(GetMessage("SyntaxError", arg?.connection?.userid.ToString()));
                return;
            }

            Duration = duration * 60;
            SetConfigValue("Options", "Duration", Duration);

            arg.ReplyWith(string.Format(GetMessage("Modify", arg?.connection?.userid.ToString()), Math.Round(Duration / 60, 1)));
        }

        #endregion

        #region Helper methods

        private void ResetTime(BaseEntity entity)
        {
            var corpse = entity as BaseCorpse;
            if (!corpse) return;
            if (!(corpse is PlayerCorpse) && !corpse?.parentEnt?.ToPlayer()) return;

            timer.Once(1, () =>
                {
                    if (!corpse.isDestroyed)
                        corpse.ResetRemovalTime(Duration);
                });
        }

        private void LoadConfiguration()
        {
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", ChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", ChatPrefixColor);
            UseChatPrefix = GetConfigValue("Settings", "ChatPrefixEnabled", UseChatPrefix);
            Duration = GetConfigValue("Options", "Duration", Duration);

            if (!configChanged) return;
            Puts("Configuration file updated.");
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

        private void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }

        private void LoadMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"Modify", "You have set the lifespan of corpses to {0} minutes."},
                {"Setting", "Corpses of players are set to disappear after {0} minutes."},
                {"SyntaxError", "Syntax error! Please make sure you are using the syntax {0} <value> where value is a positive number in minutes." }
            };

            lang.RegisterMessages(messages, this);
        }

        private void SendChatMessage(BasePlayer player, string message, params object[] args)
        {
            var prefix = $"<color={ChatPrefixColor}>{ChatPrefix}</color>";
            var msg = string.Format(GetMessage(message, player.userID.ToString()), args);
            if (!UseChatPrefix)
            {
                prefix = msg;
                msg = null;
            }

            rust.SendChatMessage(player, prefix, msg);
        }

        private string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.userID.ToString(), perm);

        private bool HasPermission(ConsoleSystem.Arg arg, string perm) => (arg.connection == null) || permission.UserHasPermission(arg.connection.userid.ToString(), perm);

        #endregion

    }
}
