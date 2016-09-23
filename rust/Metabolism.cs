using System;using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Metabolism", "Wulf/lukespragg", "2.3.1", ResourceId = 680)]
    [Description("Modifies player metabolism stats and rates")]

    class Metabolism : RustPlugin
    {
        #region Configuration

        const string permAllow = "metabolism.allow";
        bool usePermissions;
        float caloriesLossRate;
        float caloriesSpawnValue;
        float healthGainRate;
        float healthSpawnValue;
        float hydrationLossRate;
        float hydrationSpawnValue;

        protected override void LoadDefaultConfig()
        {
            Config["CaloriesLossRate"] = caloriesLossRate = GetConfig("CaloriesLossRate", 0.03f);
            Config["CaloriesSpawnValue"] = caloriesSpawnValue = GetConfig("CaloriesSpawnValue", 500f);
            Config["HealthGainRate"] = healthGainRate = GetConfig("HealthGainRate", 0.03f);
            Config["HealthSpawnValue"] = healthSpawnValue = GetConfig("HealthSpawnValue", 100f);
            Config["HydrationLossRate"] = hydrationLossRate = GetConfig("HydrationLossRate", 0.03f);
            Config["HydrationSpawnValue"] = hydrationSpawnValue = GetConfig("HydrationSpawnValue", 250f);
            Config["UsePermissions"] = usePermissions = GetConfig("UsePermissions", false);
            SaveConfig();
        }

        void Init()        {            LoadDefaultConfig();
            permission.RegisterPermission(permAllow, this);        }        #endregion

        #region Modify Metabolism

        void Metabolize(BasePlayer player)
        {
            player.health = healthSpawnValue;
            player.metabolism.calories.value = caloriesSpawnValue;
            player.metabolism.hydration.value = hydrationSpawnValue;
        }

        void OnPlayerRespawned(BasePlayer player) => Metabolize(player);        void OnRunPlayerMetabolism(PlayerMetabolism m, BaseCombatEntity entity)
        {
            var player = entity.ToPlayer();
            if (player == null) return;
            if (usePermissions && !permission.UserHasPermission(player.UserIDString, permAllow))

            player.health = Mathf.Clamp(player.health + healthGainRate, 0f, 100f);
            m.calories.value = Mathf.Clamp(m.calories.value - caloriesLossRate, m.calories.min, m.calories.max);
            m.hydration.value = Mathf.Clamp(m.hydration.value - hydrationLossRate, m.hydration.min, m.hydration.max);
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
    }
}
