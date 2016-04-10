using System;

namespace Oxide.Plugins
{
    [Info("NoDurability", "Wulf/lukespragg", "2.0.0", ResourceId = 1061)]
    public class NoDurability : CovalencePlugin
    {
        #region Initialization

        void Loaded()
        {
            #if !RUST
            throw new NotSupportedException($"This plugin does not support {(covalence.Game ?? "this game")}");
            #endif

            permission.RegisterPermission("nodurability.allowed", this);
        }

        #endregion

        #region Durability Control

        #if RUST
        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item?.GetOwnerPlayer();
            if (player == null) return;

            if (HasPermission(player.UserIDString, "nodurability.allowed")) item.condition = item.maxCondition;

            //Puts($"{item.info.shortname} was damaged by: {amount} | Condition is: {item.condition}/{item.maxCondition}");
        }
        #endif

        #endregion

        #region Helper Methods

        bool HasPermission(string steamId, string perm) => permission.UserHasPermission(steamId, perm);

        #endregion
    }
}
