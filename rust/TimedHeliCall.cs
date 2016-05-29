using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TimedHeliCall", "Troubled", 0.1)]
    class TimedHeliCall : RustPlugin
    {
        private int _heliInterval;
        private int _x;
        private int _z;
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["HeliInterval"] = 60;
            Config["X"] = 0;
            Config["Z"] = 0;
            SaveConfig();
            PrintWarning("Configuration file made");
        }

        void Init()
        {
            if (Convert.ToInt32(Config["X"]) == 0 || Convert.ToInt32(Config["Z"]) == 0)
            {
                PrintError("Change your configuration file.");
            }

            _heliInterval = GetIntConfig("HeliInterval");
            _x = GetIntConfig("X");
            _z = GetIntConfig("Z");
            Puts($"X: {_x} Z:{_z} interval: {_heliInterval}");
            if (_heliInterval > 0)
            {
                timer.Every(_heliInterval * 60, CallHeli);
            }
        }

        private int GetIntConfig(string configkey)
        {
            return Convert.ToInt32(Config[configkey]);
        }

        private void CallHeli()
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion());
            if (!entity) return;
            Puts($"Helicopter called to X:{_x} Z:{_z}");
            PatrolHelicopterAI helicopter = entity.GetComponent<PatrolHelicopterAI>();
            helicopter.SetInitialDestination(new Vector3(_x, 20f, _z));
            entity.Spawn();
        }
    }
}
