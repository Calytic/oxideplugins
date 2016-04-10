// Reference: Oxide.Ext.Rust

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Demolish Limiter", "Mughisi", "1.0.1")]
    class DemolishLimiter : RustPlugin
    {

        #region Configuration Data

        private bool configChanged = false;

        // Plugin settings
        string defaultChatPrefix = "Demolisher";
        string defaultChatPrefixColor = "#008000ff";

        string chatPrefix;
        string chatPrefixColor;

        // Plugin options
        bool defaultAdminCanDemolish = true;
        bool defaultModeratorCanDemolish = false;
        bool defaultLogDemolishToConsole = true;

        bool adminCanDemolish;
        bool moderatorCanDemolish;
        bool logToConsole;
        
        // Plugin messages
        string defaultNotAllowed = "You are not allowed to demolish anything.";
        
        string notAllowed;

        #endregion

        void Loaded()
        {
            LoadConfigValues();
        }

        protected override void LoadDefaultConfig()
        {
            Log("New configuration file created.");
        }

        void LoadConfigValues()
        {
            // Plugin settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));

            // Plugin options
            adminCanDemolish = Convert.ToBoolean(GetConfigValue("Options", "AdminsCanDemolish", defaultAdminCanDemolish));
            moderatorCanDemolish = Convert.ToBoolean(GetConfigValue("Options", "ModeratorsCanDemolish", defaultModeratorCanDemolish));
            logToConsole = Convert.ToBoolean(GetConfigValue("Options", "LogToConsole", defaultLogDemolishToConsole));

            // Plugin messages
            notAllowed = Convert.ToString(GetConfigValue("Messages", "NotAllowed", defaultNotAllowed));

            if (configChanged)
            {
                Log("Configuration file updated.");
                SaveConfig();
            }
        }
        
        object OnBuildingBlockDemolish(BuildingBlock block, BasePlayer player)
        {
            int demolisherAuthLevel = player.net.connection.authLevel;

            if ((demolisherAuthLevel == 1 && !moderatorCanDemolish) || (demolisherAuthLevel == 2 && !adminCanDemolish))
            {
                SendChatMessage(player, notAllowed);
                return true;
            }

            if (logToConsole)
                Log($"{player.displayName} has demolished a {block.blockDefinition.hierachyName} at location {block.transform.position.ToString()}.");

            return null;
        }

        #region Helper methods
        void Log(string message)
        {
            Puts("{0} : {1}", Title, message);
        }

        void SendChatMessage(BasePlayer player, string message, string arguments = null)
        {
            string chatMessage = $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}";
            player?.SendConsoleCommand("chat.add", -1, string.Format(chatMessage, arguments), 1.0);
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

        void SetConfigValue(string category, string setting, object newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }
        #endregion
    }

}
