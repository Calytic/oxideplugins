using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Purifier Config", "Shady", "1.0.1", ResourceId = 1911)]
    [Description("Tweak settings for water purifiers.")]
    class PurifierConfig : RustPlugin
    {
        bool configWasChanged = false;
        private List<WaterPurifier> waterPurifiers = new List<WaterPurifier>();
        FieldInfo warmUpTime = typeof(WaterPurifier).GetField("warmupTime", (BindingFlags.Instance | BindingFlags.NonPublic));


        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["WaterToProcessPerMinute"] = 120;
            Config["FreshWaterRatio"] = 4;
            Config["WarmupTime"] = 10f;
            SaveConfig();
        }

        void CheckConfigEntry<T>(string key, T value)
        {
            if (Config[key] == null)
            {
                Config[key] = value;
                configWasChanged = true;
            }
        }

        private void Init()
        {
            CheckConfigEntry("WaterToProcessPerMinute", 120);
            CheckConfigEntry("FreshWaterRatio", 4);
            CheckConfigEntry("WarmupTime", 10f);
            if (configWasChanged) SaveConfig();
        }


        void OnServerInitialized()
        {
            var purifiers = GameObject.FindObjectsOfType<WaterPurifier>();
            var count = 0;
            foreach(var pure in purifiers)
            { //check to make sure it isn't already contained so we're not modifying ones that were already spawned after a server restart (OnEntitySpawned)
                if (!waterPurifiers.Contains(pure))
                {
                    waterPurifiers.Add(pure);
                    ConfigurePurifier(pure);
                    count++;
                } 
            }
            Puts("Configured " + count + " water purifiers!");
        }


        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null) return;
            var getType = entity?.GetType()?.ToString() ?? string.Empty;
            if (!getType.ToLower().Contains("water")) return;
            var block = entity?.GetComponent<WaterPurifier>() ?? null;
            if (block == null) return;
            if (waterPurifiers.Contains(block)) waterPurifiers.Remove(block);
        }

        void ConfigurePurifier(WaterPurifier purifier)
        {
            var WPM = 120;
            var ratio = 4;
            var warmTime = 10f;
            TryParseInt(Config["WaterToProcessPerMinute"].ToString(), ref WPM); TryParseInt(Config["FreshWaterRatio"].ToString(), ref ratio); TryParseFloat(Config["WarmupTime"].ToString(), ref warmTime);
            if (WPM != 120f) purifier.waterToProcessPerMinute = WPM;
            if (ratio != 4) purifier.freshWaterRatio = ratio;
            if (warmTime != 10f) warmUpTime.SetValue(purifier, warmTime);
            purifier.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

            void OnEntitySpawned(BaseNetworkable entity)
        {
            var purifier = entity?.GetComponent<WaterPurifier>() ?? null;
            if (purifier == null) return;
            if (!waterPurifiers.Contains(purifier)) waterPurifiers.Add(purifier);
            ConfigurePurifier(purifier); 
        }

        public bool TryParseFloat(string text, ref float value)
        {
            float tmp;
            if (float.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }

        public bool TryParseInt(string text, ref int value)
        {
            int tmp;
            if (int.TryParse(text, out tmp))
            {
                value = tmp;
                return true;
            }
            else return false;
        }


    }
}