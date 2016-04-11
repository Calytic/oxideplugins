using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;


namespace Oxide.Plugins {
    [Info("NoNight", "BaK", "1.0.2", ResourceId = 1279)]
    class NoNight : RustPlugin {
        public int sunsetHour = 16; // latest time allowed
        public int sunriseHour = 8; // hour to set after time exceeds sunsetHour
        [HookMethod("OnTick")]
        private void OnTick() {
            try {
                    if (TOD_Sky.Instance.Cycle.Hour <= sunsetHour && TOD_Sky.Instance.Cycle.Hour >= sunriseHour) {
                        // it's already day do nothing
                    }
                    else {
						// change time
						TOD_Sky.Instance.Cycle.Hour = sunriseHour;
						Puts("NoNight has changed the server time.");
                    } 
            }
            catch (Exception ex) {
                PrintError("OnTick failed: {0}", ex.Message);
            }
        }
    }
}
