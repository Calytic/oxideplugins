using System;

namespace Oxide.Plugins
{
    [Info("PillsHere", "Wulf/lukespragg", "3.0.0", ResourceId = 1723)]
    [Description("Recovers health, hunger, and thirst by set amounts from using rad pills")]

    class PillsHere : RustPlugin
    {
        const string permUse = "pillshere.use";
        float healAmount;
        float hungerAmount;
        float thirstAmount;

        protected override void LoadDefaultConfig()
        {
            Config["HealthAmount"] = healAmount = GetConfig("HealthAmount", 20f);
            Config["HungerAmount"] = hungerAmount = GetConfig("HealthAmount", 0f);
            Config["ThirstAmount"] = thirstAmount = GetConfig("HealthAmount", 0f);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permUse, this);
        }

        void OnConsumableUse(Item item)
        {
            var player = item.GetOwnerPlayer();
            if (item.info?.itemid != 1685058759 || player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, permUse)) return;

            player.Heal(healAmount);
            player.metabolism.calories.value += hungerAmount;
            player.metabolism.hydration.value += thirstAmount;
        }

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
    }
}
