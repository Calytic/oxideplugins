using System;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("QuickSmelt", "Wulf/lukespragg", "1.3.0", ResourceId = 1067)]
    [Description("Increases the speed of the furnace smelting")]

    class QuickSmelt : RustPlugin
    {
        #region Initialization

        const string permAllow = "quicksmelt.allow";

        float byproductModifier;
        int byproductPercent;
        float cookedModifier;
        int cookedPercent;
        int fuelUsageModifier;
        bool overcookMeat;
        bool usePermissions;

        protected override void LoadDefaultConfig()
        {
            // Default is *roughly* x2 production rate
            Config["ByproductModifier"] = byproductModifier = GetConfig("ByproductModifier", 1f);
            Config["ByproductPercent"] = byproductPercent = GetConfig("ByproductPercent", 50);
            Config["FuelUsageModifier"] = fuelUsageModifier = GetConfig("FuelUsageModifier", 1);
            Config["CookedModifier"] = cookedModifier = GetConfig("CookedModifier", 1f);
            Config["CookedPercent"] = cookedPercent = GetConfig("CookedPercent", 100);
            Config["OvercookMeat"] = overcookMeat = GetConfig("OvercookMeat", false);
            Config["UsePermissions"] = usePermissions = GetConfig("UsePermissions", false);

            // Remove old config entries
            Config.Remove("ChancePerConsumption");
            Config.Remove("CharcoalChance");
            Config.Remove("CharcoalChanceModifier");
            Config.Remove("CharcoalProductionModifier");
            Config.Remove("DontOvercookMeat");
            Config.Remove("ProductionModifier");

            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permAllow, this);
        }

        void OnServerInitialized()
        {
            // Reset fuel consumption and byproduct amount - fix for previous versions
            var wood = ItemManager.FindItemDefinition("wood");
            var burnable = wood?.GetComponent<ItemModBurnable>();
            if (burnable != null)
            {
                burnable.byproductAmount = 1;
                burnable.byproductChance = 0.5f;
            }

            // Check if meat should be overcooked
            if (overcookMeat) return;

            // Loop through item definitions
            var itemDefinitions = ItemManager.itemList;
            foreach (var item in itemDefinitions)
            {
                // Skip any item definitions other than cooked meat
                if (!item.shortname.Contains(".cooked")) continue;

                // Lower high temperature on item definition to prevent burning
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null) cookable.highTemp = 150;
            }
        }

        void Unload()
        {
            // Loop through item definitions
            var itemDefinitions = ItemManager.itemList;
            foreach (var item in itemDefinitions)
            {
                // Skip any item definitions other than cooked meat
                if (!item.shortname.Contains(".cooked")) continue;

                // Lower high temperature on item definition to prevent burning
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null) cookable.highTemp = 250;
            }
        }

        #endregion

        #region Smelting Magic

        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            // Check if furnance is usable
            if (oven == null) return;

            // Check if permissions are enabled and player has permission
            if (usePermissions && !permission.UserHasPermission(oven.OwnerID.ToString(), permAllow)) return;

            // Modify the amount of fuel to use
            fuel.amount -= fuelUsageModifier - 1;

            // Modify the amount of byproduct to produce
            burnable.byproductAmount = 1 * (int)byproductModifier;
            burnable.byproductChance = (100 - byproductPercent) / 100f;

            // Loop through furance inventory slots
            for (var i = 0; i < oven.inventorySlots; i++)
            {
                try
                {
                    // Check for and ignore invalid items
                    var slotItem = oven.inventory.GetSlot(i);
                    if (slotItem == null || !slotItem.IsValid()) continue;

                    // Check for and ignore non-cookables
                    var cookable = slotItem.info.GetComponent<ItemModCookable>();
                    if (cookable == null) continue;

                    // Skip already cooked food items
                    if (slotItem.info.shortname.EndsWith(".cooked")) continue;

                    // The chance of consumption is going to result in a 1 or 0
                    var consumptionAmount = (int)Math.Ceiling(cookedModifier * (Random.Range(0f, 1f) <= cookedPercent ? 1 : 0));

                    // Check how many are actually in the furnace, before we try removing too many
                    var inFurnaceAmount = slotItem.amount;
                    if (inFurnaceAmount < consumptionAmount) consumptionAmount = inFurnaceAmount;

                    // Set consumption to however many we can pull from this actual stack
                    consumptionAmount = TakeFromInventorySlot(oven.inventory, slotItem.info.itemid, consumptionAmount, i);

                    // If we took nothing, then... we can't create any
                    if (consumptionAmount <= 0) continue;

                    // Create the item(s) that are now cooked
                    var cookedItem = ItemManager.Create(cookable.becomeOnCooked, cookable.amountOfBecome * consumptionAmount);
                    if (!cookedItem.MoveToContainer(oven.inventory)) cookedItem.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
                }
                catch (InvalidOperationException) { }
            }
        }

        int TakeFromInventorySlot(ItemContainer container, int itemId, int amount, int slot)
        {
            var item = container.GetSlot(slot);
            if (item.info.itemid != itemId) return 0;

            if (item.amount > amount)
            {
                item.MarkDirty();
                item.amount -= amount;
                return amount;
            }

            amount = item.amount;
            item.RemoveFromContainer();
            return amount;
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
    }
}
