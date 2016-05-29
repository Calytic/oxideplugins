using UnityEngine;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Airdrop Randomizer", "k1lly0u", "0.1.1", ResourceId = 1898)]
    class AirdropRandomizer : RustPlugin
    {
        #region Fields
        private FieldInfo dropPosition;
        #endregion

        #region Oxide Hooks        
        void OnServerInitialized()
        {
            LoadVariables();
            dropPosition = typeof(CargoPlane).GetField("dropPosition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }       
        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity is CargoPlane)
                if (entity.GetComponent<CargoPlane>() != null)
                {
                    var plane = entity.GetComponent<CargoPlane>();                    
                    var location = (Vector3)dropPosition.GetValue(plane);
                    if (location != null)
                    {
                        var x = UnityEngine.Random.Range(location.x - configData.MaxDistance, location.x + configData.MaxDistance);
                        var z = UnityEngine.Random.Range(location.z - configData.MaxDistance, location.z + configData.MaxDistance);
                        plane.UpdateDropPosition(new Vector3(x, location.y, z));
                    } 
                }
        }       
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public float MaxDistance { get; set; }
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
                MaxDistance = 300f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}

