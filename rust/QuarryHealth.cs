using System;

namespace Oxide.Plugins
{
    [Info("Quarry Health", "Waizujin", 1.0)]
    [Description("Changes the health value of quarries.")]
    public class QuarryHealth : RustPlugin
    {
        public float quarryHealth { get { return Config.Get<float>("quarry_health"); } }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            Config["quarry_health"] = 2500f;

            SaveConfig();
        }

        void OnServerInitialized()
        {
            updateQuarries();
        }

        void OnEntityBuilt(Planner planner, UnityEngine.GameObject component)
        {
            ItemDefinition item = planner.GetOwnerItemDefinition();

            if (item.shortname == "mining.quarry")
            {
                updateQuarries();
            }
        }

        public void updateQuarries()
        {
            var quarries = UnityEngine.Object.FindObjectsOfType<MiningQuarry>();

            foreach (MiningQuarry quarry in quarries)
            {
                quarry.health = quarryHealth;
            }
        }
    }
}
