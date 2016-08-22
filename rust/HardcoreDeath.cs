using System;
using System.Collections.Generic;
using System.Linq;
namespace Oxide.Plugins
{
    [Info("HardcoreDeath", "Reynostrum", "1.0.1")]
    [Description("Unlearn a random blueprint on death.")]
    class HardcoreDeath : RustPlugin
    {
        #region Init/config
        bool ProtectAdmin => GetConfig("ProtectAdmin", false);
        string Prefix => GetConfig("Prefix", "<color=#fdde23>[HARDCORE DEATH]</color>");
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Plugin is loading default configuration.");
            Config["ProtectAdmin"] = ProtectAdmin;
            Config["Prefix"] = Prefix;
            SaveConfig();
        }
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            LoadDefaultConfig();
        }
        #endregion

        #region Oxide hooks
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || entity as BasePlayer == null) return;
            BasePlayer player = entity.ToPlayer();
            if (!player.IsConnected()) return;
            if (ProtectAdmin && player.IsAdmin()) return;
            var playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(player.userID);
            var blueprints = playerInfo.unlockedItems;
            if (blueprints.Count == 0) return;
            var blueprint = blueprints.ToList()[UnityEngine.Random.Range(0, blueprints.Count)];
            blueprints.Remove(blueprint);
            ServerMgr.Instance.persistance.SetPlayerInfo(player.userID, playerInfo);
            player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            ItemDefinition bp = ItemManager.FindItemDefinition(blueprint);
            timer.Once(3f, () =>
            {
                PrintToChat(player, Prefix + " " + Lang("BlueprintUnlearned", player.UserIDString, bp.displayName.translated));
            });
        }
        #endregion

        #region Helpers
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"BlueprintUnlearned", "You have unlearned {0} blueprint." },
        };
        #endregion
    }
}
