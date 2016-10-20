using System;

namespace Oxide.Plugins
{
    [Info("StructureRefund", "Wulf/lukespragg", "1.2.0", ResourceId = 1692)]
    [Description("Refunds previous build materials when demolishing and/or upgrading")]

    class StructureRefund : CovalencePlugin
    {
        #region Initialization

        const string permDemolish = "structurerefund.demolish";
        const string permUpgrade = "structurerefund.upgrade";
        bool demolishRefunds;
        bool upgradeRefunds;

        protected override void LoadDefaultConfig()
        {
            // Loop through building grade names
            foreach (var grade in Enum.GetNames(typeof(BuildingGrade.Enum)))
            {
                // Skip the invalid grade names
                if (grade.Equals("None") || grade.Equals("Count")) continue;

                // Add configuration setting for grade
                Config["Refund" + grade] = GetConfig("Refund" + grade, true);
            }

            Config["DemolishRefunds"] = demolishRefunds = GetConfig("DemolishRefunds", true);
            Config["UpgradeRefunds"] = upgradeRefunds = GetConfig("UpgradeRefunds", true);
            SaveConfig();
        }

        void Init()
        {
            LoadDefaultConfig();

            // Register user/group permissions
            permission.RegisterPermission(permDemolish, this);
            permission.RegisterPermission(permUpgrade, this);

            // Only allow hooks to be called if features are enabled
            if (!demolishRefunds) Unsubscribe("OnStructureDemolish");
            if (!upgradeRefunds) Unsubscribe("OnStructureUpgrade");
        }

        #endregion

        #region Refunding

        void RefundMaterials(BuildingBlock block, BasePlayer player)
        {
            // Check if player's inventory is full, don't refund if full
            if (player.inventory.containerMain.IsFull()) return;

            // Loop through resources used to build structure
            foreach (var item in block.blockDefinition.grades[(int)block.grade].costToBuild)
                // Give player resources used
                player.GiveItem(ItemManager.CreateByItemID(item.itemid, (int)item.amount));
        }

        void OnStructureDemolish(BuildingBlock block, BasePlayer player)
        {
            // Check if player has 'structurerefund.demolish' permission
            if (permission.UserHasPermission(player.UserIDString, permDemolish)) RefundMaterials(block, player);
        }

        void OnStructureUpgrade(BuildingBlock block, BasePlayer player)
        {
            // Check if player has 'structurerefund.upgrade' permission
            if (permission.UserHasPermission(player.UserIDString, permUpgrade)) RefundMaterials(block, player);
        }

        #endregion

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));    }
}
