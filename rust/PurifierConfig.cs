using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Purifier Config", "Shady", "1.0.3", ResourceId = 1911)]
    [Description("Tweak settings for water purifiers.")]
    class PurifierConfig : RustPlugin
    {
        bool configWasChanged = false;
        FieldInfo warmUpTime = typeof(WaterPurifier).GetField("warmupTime", (BindingFlags.Instance | BindingFlags.NonPublic));
        bool init = false;
        #region Config
        int WPM => GetConfig("WaterToProcessPerMinute", 120);
        int WaterRatio => GetConfig("FreshWaterRatio", 4);
        float Warmup => GetConfig("WarmupTime", 10f);

        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
        protected override void LoadDefaultConfig()
        {
           // Config.Clear();
            Config["WaterToProcessPerMinute"] = WPM;
            Config["FreshWaterRatio"] = WaterRatio;
            Config["WarmupTime"] = Warmup;
            SaveConfig();
        }
        #endregion
        #region Hooks
        void OnServerInitialized()
        {
            var purifiers = GameObject.FindObjectsOfType<WaterPurifier>();
            for(int i = 0; i < purifiers.Length; i++)
            {
                var pure = purifiers[i];
                ConfigurePurifier(pure);
            }
            Puts("Configured " + purifiers.Length + " water purifiers successfully!");
            init = true;
        }

        void Init() => LoadDefaultConfig();

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null || !init) return;
            var purifier = entity?.GetComponent<WaterPurifier>() ?? null;
            if (purifier == null) return;
            ConfigurePurifier(purifier);
        }
        #endregion
        #region ConfigurePurifiers
        void ConfigurePurifier(WaterPurifier purifier)
        {
            if (WPM != 120f) purifier.waterToProcessPerMinute = WPM;
            if (WaterRatio != 4) purifier.freshWaterRatio = WaterRatio;
            if (Warmup != 10f) warmUpTime.SetValue(purifier, Warmup);
            purifier.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }
        #endregion
        #region Util
        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
        #endregion
    }
}