// Reference: Newtonsoft.Json

using Rust;
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{

    [Info("DamageController", "Wolfs_Darker", "1.0.0")]
    class DamageController : RustPlugin
    {
        /**
         *  The Damage controller's configuration.
         */ 
        public DamageList config = new DamageList();

        /**
         * Damage list configuration.
         */ 
        public class DamageList {

            /**
             * Damage receiver's list.
             */ 
            public List<DamageReceiver> receivers;

            public DamageList() { 
                receivers = new List<DamageReceiver>(); 
            }

            /**
             * Finds a damage receiver's data from its name.
             */ 
            public DamageReceiver forName(String name)
            {
                foreach (DamageReceiver r in receivers)
                {
                    if (r.name.Equals(name))
                    {
                        return r;
                    }
                }
                return null;
            }
        }

        /**
         * Damage Receiver configuration.
         */ 
        public class DamageReceiver {

            /**
             * The receiver's name.
             */ 
            public String name;

            /**
             * The damage multipliers list.
             */ 
            public Dictionary<DamageType, float> list;

            public DamageReceiver(String name) {
                this.name = name;
                list = new Dictionary<DamageType, float>(); 
            }
        }

        protected override void LoadDefaultConfig()
        {
            if (Config["damage_list"] == null)
            {
                config.receivers.Add(new DamageReceiver("building"));
                config.receivers.Add(new DamageReceiver("player"));
                config.receivers.Add(new DamageReceiver("animal"));
                config.receivers.Add(new DamageReceiver("barricade"));

                foreach (DamageReceiver r in config.receivers)
                {
                    foreach (DamageType dmg in Enum.GetValues(typeof(DamageType)))
                    {
                        if (dmg != DamageType.LAST)
                            r.list[dmg] = 1f;
                    }
                }
            }
            Config["damage_list"] = config;
            SaveConfig();
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                config = JsonConvert.DeserializeObject<DamageList>(JsonConvert.SerializeObject(Config["damage_list"]).ToString());
            }
            catch (Exception ex)
            {
                Puts("OnServerInitialized failed: " + ex.Message);
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null)
                return;

            String target = entity is BasePlayer ? "player" : entity is BuildingBlock ? "building" : entity is BaseNPC ? "animal" : entity is Barricade ? "barricade" : "none";

            DamageReceiver receiver = config.forName(target);

            if (receiver == null)
                return;

            DamageType type = hitInfo.damageTypes.GetMajorityDamageType();

            if (type == null)
                return;


            float modifier;
            if (receiver.list.TryGetValue(type, out modifier))
            {
                hitInfo.damageTypes.Scale(type, modifier);
            }
        }

    }
}