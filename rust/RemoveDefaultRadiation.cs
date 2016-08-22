using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("RemoveDefaultRadiation", "k1lly0u", "0.1.0", ResourceId = 0)]
    class RemoveDefaultRadiation : RustPlugin
    {        
        #region Oxide Hooks        
        void OnServerInitialized()
        {
            LoadVariables();
            var allobjects = UnityEngine.Object.FindObjectsOfType<TriggerRadiation>();
            for (int i = 0; i < allobjects.Length; i++)
            {
                UnityEngine.Object.Destroy(allobjects[i]);
            }
            if (configData.PluginList.Count > 0)
            {
                PrintWarning("All radiation elements destroyed, reloading plugins that use radiation");
                foreach (var plugin in configData.PluginList)
                {
                    if (plugins.Exists(plugin))
                    {
                        rust.RunServerCommand($"oxide.reload {plugin}");
                    }
                }
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public List<string> PluginList { get; set; }
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
                PluginList = new List<string>
                {
                    "MonumentRadiation",
                    "RadPockets",
                    "ZoneManager"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion       
    }
}
