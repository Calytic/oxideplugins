using System;
using Rust;

namespace Oxide.Plugins
{
    [Info("NoSleepers", "Wulf/lukespragg", "0.4.1", ResourceId = 1452)]
    [Description("Prevents players from sleeping and optionally removes player corpses")]

    class NoSleepers : CovalencePlugin
    {
        #region Initialization

        const string permExclude = "nosleepers.exclude";
        bool killExisting;
        bool removeCorpses;

        protected override void LoadDefaultConfig()
        {
            Config["KillExisting"] = killExisting = GetConfig("KillExisting", false);
            Config["RemoveCorpses"] = removeCorpses = GetConfig("RemoveCorpses", true);
            SaveConfig();
        }

        void OnServerInitialized()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            permission.RegisterPermission(permExclude, this);
            if (!killExisting) return;

            var killCount = 0;
            var sleepers = BasePlayer.sleepingPlayerList;
            foreach (var sleeper in sleepers.ToArray())
            {
                if (sleeper.IsDead()) sleepers.Remove(sleeper);
                sleeper.KillMessage();
                killCount++;
            }
            if (killCount > 0) Puts($"Killed {killCount} {(killCount == 1 ? "sleeper" : "sleepers")}");
        }

        #endregion

        #region Sleeper/Corpse Removal

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsDead() && !permission.UserHasPermission(player.UserIDString, permExclude)) player.Respawn();
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if (player.IsSleeping() && !permission.UserHasPermission(player.UserIDString, permExclude)) player.EndSleeping();
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.IsDead() && !permission.UserHasPermission(player.UserIDString, permExclude)) player.Hurt(1000f, DamageType.Suicide, player, false);
        }

        //object OnPlayerSleep(BasePlayer player) => true; // TODO: Hook might be causing local player duplication

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (removeCorpses && entity.ShortPrefabName.Equals("player_corpse")) entity.KillMessage();
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
    }
}
