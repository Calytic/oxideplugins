using System;

namespace Oxide.Plugins
{
    [Info("ToukDurability", "Touk", "1.0.0")]
    [Description("Customize durability")]
    public class ToukDurability : RustPlugin
    {
        float durabilityRatio = 1f;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();
            Config["DurabilityRatio"] = durabilityRatio;
            SaveConfig();
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            if (item?.GetOwnerPlayer() == null) return;
            durabilityRatio = GetConfig<float>("DurabilityRatio", durabilityRatio);
            item.condition += amount - (amount * durabilityRatio);
            //Puts($"{item?.GetOwnerPlayer()} {item.info.shortname} was damaged by: {amount}*{durabilityRatio} | Condition is: {item.condition}/{item.maxCondition}");
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
    }
}
