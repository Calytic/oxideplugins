using System;
using System.Collections.Generic;

using Rust;

namespace Oxide.Plugins
{

    [Info("Indestructable Buildings", "Mughisi", "1.0.1", ResourceId=966)]
    class IndestructableBuildings : RustPlugin
    {

        #region Configuration Data

        bool configChanged;

        // Plugin settings
        string defaultChatPrefix = "Protector";
        string defaultChatPrefixColor = "#008000ff";

        string chatPrefix;
        string chatPrefixColor;

        // Plugin options
        bool defaultProtectFoundations = true;
        bool defaultProtectAllBuildingBlocks = true;
        bool defaultInformPlayer = true;
        float defaultInformInterval = 30;

        bool protectFoundations;
        bool protectAllBuildingBlocks;
        bool informPlayer;
        float informInterval;

        // Messages
        string defaultHelpText = "Damage to {0} has been disabled.";
        string defaultInformMessage = "You cannot deal damage to {0}!";

        string helpText;
        string informMessage;

        #endregion

        class OnlinePlayer
        {
            public BasePlayer Player;
            public float LastInformTime;

            public OnlinePlayer(BasePlayer player)
            {
            }
        }

        [OnlinePlayers] Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        protected override void LoadDefaultConfig()
        {
            Log("Created a new default configuration file.");
            Config.Clear();
            LoadVariables();
        }

        void Loaded()
        {
            LoadVariables();

            // Save config changes when required
            if (configChanged)
            {
                Log("The configuration file was updated.");
                SaveConfig();
            }
        }

        void LoadVariables()
        {
            // Settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));

            // Options
            protectFoundations = bool.Parse(Convert.ToString(GetConfigValue("Options", "ProtectFoundations", defaultProtectFoundations)));
            protectAllBuildingBlocks = bool.Parse(Convert.ToString(GetConfigValue("Options", "ProtectAllBuildingBlocks", defaultProtectAllBuildingBlocks)));
            informPlayer = bool.Parse(Convert.ToString(GetConfigValue("Options", "StickyGrenades", defaultInformPlayer)));
            informInterval = float.Parse(Convert.ToString(GetConfigValue("Options", "InformInterval", defaultInformInterval)));

            // Messages
            helpText = Convert.ToString(GetConfigValue("Messages", "HelpText", defaultHelpText));
            informMessage = Convert.ToString(GetConfigValue("Messages", "InformMessage", defaultInformMessage));
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var block = entity as BuildingBlock;
            if (!block) return;
            if (((block.blockDefinition.hierachyName == "foundation" || block.blockDefinition.hierachyName == "foundation.triangle") && protectFoundations) || protectAllBuildingBlocks)
                info.damageTypes = new DamageTypeList();

            if (info.damageTypes.Total() != 0f) return;

            var player = info.Initiator as BasePlayer;
            if (player && informPlayer && onlinePlayers[player].LastInformTime + informInterval < GetTimestamp())
            {
                onlinePlayers[player].LastInformTime = GetTimestamp();
                SendChatMessage(player, informMessage, (protectAllBuildingBlocks ? "buildings" : "foundations"));
            }
        }

        void OnPlayerInit(BasePlayer player) 
            => onlinePlayers[player].LastInformTime = 0f;

        void SendHelpText(BasePlayer player)
        {
            if (!protectFoundations && !protectAllBuildingBlocks) return;
            SendChatMessage(player, helpText, (protectAllBuildingBlocks ? "buildings" : "foundations"));
        }

        #region Helper Methods

        void Log(string message) 
            => Puts("{0} : {1}", Title, message);

        void SendChatMessage(BasePlayer player, string message, params object[] arguments) 
            => PrintToChat(player, $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}", arguments);
        
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
        private long GetTimestamp()
            => Convert.ToInt64((System.DateTime.UtcNow.Subtract(epoch)).TotalSeconds);

        #endregion

    }

}
