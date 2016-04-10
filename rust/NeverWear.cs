using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NeverWear", "k1lly0u", "0.1.31", ResourceId = 1816)]
    class NeverWear : RustPlugin
    {
        void Loaded() => RegisterPermissions();
        void OnServerInitialized() => LoadVariables();
        private void RegisterPermissions()
        {
            permission.RegisterPermission("neverwear.use", this);
            permission.RegisterPermission("neverwear.attire", this);
            permission.RegisterPermission("neverwear.weapons", this);
            permission.RegisterPermission("neverwear.tools", this);
        }
        private bool HasPerm(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.UserIDString, perm)) return true;
            return false;
        }
        void OnLoseCondition(Item item, ref float amount)
        {
            BasePlayer player;
            if (item.GetOwnerPlayer() == null)
            {
                if (!item.info.shortname.Contains(".mod."))
                    return;
                if (item.parentItem.GetOwnerPlayer() == null)
                    return;
                player = item.parentItem.GetOwnerPlayer();
            }
            else player = item.GetOwnerPlayer();
            var def = ItemManager.FindItemDefinition(item.info.itemid);
            if ((configData.useWhiteList && configData.WhitelistedItems.Contains(def.shortname) && HasPerm(player, "neverwear.use"))
                || (def.category == ItemCategory.Weapon && configData.useWeapons && HasPerm(player, "neverwear.weapons"))
                || (def.category == ItemCategory.Attire && configData.useAttire && HasPerm(player, "neverwear.attire"))
                || (def.category == ItemCategory.Tool && configData.useTools && HasPerm(player, "neverwear.tools")))
                if (item.hasCondition)                
                    item.RepairCondition(amount);                
            return;
        }

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public bool useWeapons { get; set; }
            public bool useTools { get; set; }            
            public bool useAttire { get; set; }
            public bool useWhiteList { get; set; }
            public List<string> WhitelistedItems { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                useTools = true,
                useAttire = false,
                useWeapons = false,
                useWhiteList = false,
                WhitelistedItems = new List<string>
                {
                    "hatchet",
                    "pickaxe",
                    "rifle.bolt",
                    "rifle.ak"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
