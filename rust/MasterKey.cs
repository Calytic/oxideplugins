using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("MasterKey", "Wulf/lukespragg", "0.4.10", ResourceId = 1151)]
    [Description("Gain access to any locked object with permissions")]

    class MasterKey : CovalencePlugin
    {
        #region Initialization

        readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("MasterKey");
        readonly string[] lockableTypes = { "boxes", "cells", "doors", "gates", "shops", "floors" };
        Dictionary<string, bool> playerPrefs = new Dictionary<string, bool>();
        const string permCupboards = "masterkey.cupboards";

        bool logUsage;
        bool showMessages;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["LogUsage"] = logUsage = GetConfig("LogUsage", true);
            Config["ShowMessages"] = showMessages = GetConfig("ShowMessages", true);

            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            playerPrefs = dataFile.ReadObject<Dictionary<string, bool>>();

            foreach (var type in lockableTypes) permission.RegisterPermission($"{Title.ToLower()}.{type}", this);
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Disabled"] = "Master key access is now disabled",
                ["Enabled"] = "Master key access is now enabled",
                ["MasterKeyUsed"] = "{0} ({1}) used master key at {2}",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["UnlockedWith"] = "Unlocked {0} with master key!"
            }, this);
        }

        #endregion

        #region Chat Command

        [Command("masterkey", "mkey", "mk")]
        void ChatCommand(IPlayer player, string command, string[] args)
        {
            foreach (var type in lockableTypes)
            {
                if (player.HasPermission($"{Title.ToLower()}.{type}")) continue;
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            if (!playerPrefs.ContainsKey(player.Id)) playerPrefs.Add(player.Id, true);
            playerPrefs[player.Id] = !playerPrefs[player.Id];
            dataFile.WriteObject(playerPrefs);

            player.Reply(playerPrefs[player.Id] ? Lang("Enabled", player.Id) : Lang("Disabled"));
        }

        #endregion

        #region Lock Access

        object CanUseLock(BasePlayer player, BaseLock @lock)
        {
            var prefab = @lock.parentEntity.Get(true).ShortPrefabName;

            if (!@lock.IsLocked()) return true;
            if (playerPrefs.ContainsKey(player.UserIDString) && !playerPrefs[player.UserIDString]) return null;

            foreach (var type in lockableTypes)
            {
                if (!type.Contains(prefab)) continue;

                if (!permission.UserHasPermission(player.UserIDString, $"masterkey.{type}")) return null;
                if (showMessages) player.ChatMessage(Lang("UnlockedWith", player.UserIDString, type));
                if (logUsage) LogToFile(Lang("MasterKeyUsed", null, player.displayName, player.UserIDString, player.transform.position));
                return true;
            }

            return null;
        }

        #endregion

        #region Cupboard Access

        void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            var player = entity as BasePlayer;
            if (player == null || !(trigger is BuildPrivilegeTrigger)) return;

            if (playerPrefs.ContainsKey(player.UserIDString) && !playerPrefs[player.UserIDString]) return;
            if (!permission.UserHasPermission(player.UserIDString, permCupboards)) return;

            timer.Once(0.1f, () => player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true));
            if (showMessages) player.ChatMessage(Lang("UnlockedWith", player.UserIDString, "cupboard"));
            if (logUsage) LogToFile(Lang("MasterKeyUsed", null, player.displayName, player.UserIDString, player.transform.position));
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void LogToFile(string text) => ConVar.Server.Log($"oxide/logs/{Title.ToLower()}_{DateTime.Now.ToString("yyyy-MM-dd")}.txt", text);

        #endregion
    }
}
