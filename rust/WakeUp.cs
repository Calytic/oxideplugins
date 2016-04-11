using System;

namespace Oxide.Plugins
{
    [Info("WakeUp", "Virobeast", "0.0.3", ResourceId = 1487)]
    [Description("Removes Your sleeping screen after hitting respawn")]

    class WakeUp : RustPlugin
    {

        #region no sleeper

        void OnPlayerRespawned(BasePlayer player)
        {
            timer.Once(3, () =>
            {
                if (player.IsSleeping()) player.EndSleeping();
            });
        }
        #endregion
    }
}
