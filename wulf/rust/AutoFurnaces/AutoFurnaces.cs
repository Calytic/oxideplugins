namespace Oxide.Plugins
{
    [Info("Auto Furnaces", "Waizujin", 1.1)]
    [Description("Automatically starts all furnaces after a server restart.")]
    public class AutoFurnaces : RustPlugin
    {
        void OnServerInitialized()
        {
            int furnaceCount = 0;
            int furnaceEmptyCount = 0;
            BaseOven[] furnaces = UnityEngine.Object.FindObjectsOfType<BaseOven>() as BaseOven[];
            foreach (BaseOven furnace in furnaces) {
                bool hasCookable = false;

                foreach (Item item in furnace.inventory.itemList)
                {
                    if (
                        item.info.shortname == "bearmeat" ||
                        item.info.shortname == "metal_ore" ||
                        item.info.shortname == "chicken_raw" ||
                        item.info.shortname == "humanmeat_raw" ||
                        item.info.shortname == "wolfmeat_raw" ||
                        item.info.shortname == "sulfur_ore"
                    )
                    {
                        hasCookable = true;
                    }
                }

                if (furnace.temperature == BaseOven.TemperatureType.Smelting && hasCookable == true) {
                    furnace.inventory.temperature = 1000f;
                    furnace.CancelInvoke("Cook");
                    furnace.InvokeRepeating("Cook", 0.5f, 0.5f);
                    furnace.SetFlag(BaseEntity.Flags.On, true);

                    furnaceCount++;
                }

                if (hasCookable == false)
                {
                    furnaceEmptyCount++;
                }
            }

            Puts(furnaceCount + " Furnaces were automatically turned on.");
            Puts(furnaceEmptyCount + " Furnaces were ignored as they had nothing cookable in them.");
        }
    }
}
