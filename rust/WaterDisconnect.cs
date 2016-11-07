using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("WaterDisconnect", "Wulf/lukespragg", "2.2.0", ResourceId = 2122)]
    [Description("Hurts or kills players that log out underwater")]

    class WaterDisconnect : CovalencePlugin
    {
        #region Initialization

        const string permExclude = "waterdisconnect.exclude";

        bool hurtOnLogout;
        bool hurtOverTime;
        bool killOnLogout;

        int damageAmount;
        int damageEvery;
        int waterPercent;

        protected override void LoadDefaultConfig()
        {
            // Options
            Config["Hurt On Logout (true/false)"] = hurtOnLogout = GetConfig("Hurt On Logout (true/false)", true);
            Config["Hurt Over Time (true/false)"] = hurtOverTime = GetConfig("Hurt Over Time (true/false)", true);
            Config["Kill On Logout (true/false)"] = killOnLogout = GetConfig("Kill On Logout (true/false)", false);

            // Settings
            Config["Damage Amount (1 - 500)"] = damageAmount = GetConfig("Damage Amount (1 - 500)", 10);
            Config["Damage Every (Seconds)"] = damageEvery = GetConfig("Damage Every (Seconds)", 10);
            Config["Underwater Percent (1 - 100)"] = waterPercent = GetConfig("Underwater Percent (1 - 100)", 75);

            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permExclude, this);
        }

        #endregion

        #region Disconnect Handling

        readonly FieldInfo modelStateInfo = typeof(BasePlayer).GetField("modelState", BindingFlags.Instance | BindingFlags.NonPublic);
        readonly Dictionary<ulong, Timer> timers = new Dictionary<ulong, Timer>();

        void HandleDisconnect(BasePlayer player)
        {
            var modelState = modelStateInfo.GetValue(player) as ModelState;
            if (!(modelState != null && modelState.waterLevel > (waterPercent / 100f))) return;
            if (permission.UserHasPermission(player.UserIDString, permExclude)) return;

            if (hurtOnLogout)
            {
                if (hurtOverTime)
                {
                    timers[player.userID] = timer.Every(damageEvery, () =>
                    {
                        if (player.IsDead() && timers.ContainsKey(player.userID))
                            timers[player.userID].Destroy();
                        else
                            player.Hurt(damageAmount);
                    });
                    return;
                }

                player.Hurt(damageAmount);
            }
            else if (killOnLogout) player.Kill();
        }

        #endregion

        #region Game Hooks

        void OnServerInitialized()
        {
            foreach (var sleeper in BasePlayer.sleepingPlayerList) HandleDisconnect(sleeper);
        }

        void OnPlayerDisconnected(BasePlayer player) => HandleDisconnect(player);

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
