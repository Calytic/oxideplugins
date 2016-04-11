using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("Supply Signal Alerter", "Lederp", "1.0.0")]
    class SupplySignalAlert : RustPlugin
    {
        void OnWeaponThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity is SupplySignal)
            {
                timer.Once(2.5f, () =>
                    {
                        SupplySignal signal = entity as SupplySignal;
                        Vector3 location = signal.GetEstimatedWorldPosition();
                        ConsoleSystem.Broadcast("chat.add", 0, string.Format("<color=orange>{0}:</color> {1}", "SERVER (Supply Drop)", "Location: X: " + location.x + " Y: " + location.y + " Z: " + location.z));
                    });
            }
        }
    }
}
