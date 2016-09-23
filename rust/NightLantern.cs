using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("NightLantern", "k1lly0u", "2.0.1", ResourceId = 1182)]
    class NightLantern : RustPlugin
    {
        #region Fields
        private Timer timeCheck;
        private List<BaseOven> lights;
        private bool isActivated;
        private bool isEnabled;
        private bool lightsOn;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lights = new List<BaseOven>();
            permission.RegisterPermission("nightlantern.use", this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"You have disabled auto lights","You have disabled auto lights" },
                {"You have enabled auto lights","You have enabled auto lights" }
            }, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            isActivated = true;
            isEnabled = true;
            lightsOn = false;
            FindLights();            
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (isActivated)
            {
                if (entity == null) return;
                if (entity is BaseOven)
                    CheckType(entity as BaseOven);
            }
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity == null) return;
            if (entity is BaseOven && lights.Contains(entity as BaseOven))
                lights.Remove(entity as BaseOven);
        }
        void Unload()
        {
            if (timeCheck != null)
                timeCheck.Destroy();
        }
        #endregion

        #region Functions
        void FindLights()
        {
            var ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>().ToList();
            foreach (var oven in ovens)            
                CheckType(oven);            
            TimeLoop();
        }
        void CheckType(BaseOven oven)
        {
            if ((oven.ShortPrefabName == "lantern.deployed" && configData.LightTypes.Lanterns)
                   || (oven.ShortPrefabName == "jackolantern.happy" && configData.LightTypes.JackOLanterns)
                   || (oven.ShortPrefabName == "jackolantern.angry" && configData.LightTypes.JackOLanterns)
                   || (oven.ShortPrefabName == "campfire" && configData.LightTypes.Campfires)
                   || (oven.ShortPrefabName == "ceilinglight.deployed" && configData.LightTypes.CeilingLights)
                   || (oven.ShortPrefabName == "furnace" && configData.LightTypes.Furnaces))
                lights.Add(oven);
        }
        void TimeLoop()
        {
            timeCheck = timer.Once(20, () =>
            {
                if (isEnabled)
                    CheckTime();
                TimeLoop();
            });
        }
        void CheckTime()
        {
            var time = TOD_Sky.Instance.Cycle.Hour;
            if (time >= configData.SunsetHour)
            {
                if (!lightsOn)
                {
                    ToggleLanterns(true);
                }

            }
            else if (time >= configData.SunriseHour && time < configData.SunsetHour)
            {
                if (lightsOn)
                {
                    ToggleLanterns(false);
                }
            }
        }        
        void ToggleLanterns(bool status)
        {
            lightsOn = status;
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].IsOn() == status) continue;
                lights[i].SetFlag(BaseEntity.Flags.On, status);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("lantern")]
        void cmdLantern(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "nightlantern.use")) return;
            if (isEnabled)
            {
                isEnabled = false;
                if (lightsOn)
                    ToggleLanterns(false);
                SendReply(player, lang.GetMessage("You have disabled auto lights", this, player.UserIDString));
                return;
            }
            else
            {
                isEnabled = true;
                if (!lightsOn)
                    CheckTime();
                SendReply(player, lang.GetMessage("You have enabled auto lights", this, player.UserIDString));
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class LightTypes
        {
            public bool Campfires { get; set; }
            public bool Lanterns { get; set; }
            public bool CeilingLights { get; set; }
            public bool Furnaces { get; set; }
            public bool JackOLanterns { get; set; }

        }
        class ConfigData
        {
            public LightTypes LightTypes { get; set; }
            public float SunriseHour { get; set; }
            public float SunsetHour { get; set; }            
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
                LightTypes = new LightTypes
                {
                    Campfires = true,
                    CeilingLights = true,
                    Furnaces = false,
                    JackOLanterns = true,
                    Lanterns = true
                },
                SunriseHour = 7.5f,
                SunsetHour = 18.5f
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
