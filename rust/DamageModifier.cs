using System.Collections.Generic;
using System;

using Rust;

namespace Oxide.Plugins
{
     [Info("DamageModifier", "ColonBlow", "1.2.1")]
     internal class DamageModifier : RustPlugin
     {
         private const int DamageTypeMax = (int) DamageType.LAST;
         private readonly float[] _modifiers = new float[DamageTypeMax];
         private bool _didConfigChange;

         private void Loaded() => LoadConfigValues();
         protected override void LoadDefaultConfig() => Puts("New configuration file created.");

         private void LoadConfigValues()
         {
             foreach (DamageType val in Enum.GetValues(typeof(DamageType)))
             {
                 if (val == DamageType.LAST) continue;
                 _modifiers[(int) val] = Convert.ToSingle(GetConfigValue("Global_Mutipliers", val.ToString(), 1.0));
             }

             if (!_didConfigChange) return;
             Puts("Configuration file updated.");
             SaveConfig();
         }

         private object GetConfigValue(string category, string setting, object defaultValue)
         {
             var data = Config[category] as Dictionary<string, object>;
             object value;
             if (data == null)
             {
                 data = new Dictionary<string, object>();
                 Config[category] = data;
                 _didConfigChange = true;
             }

             if (data.TryGetValue(setting, out value)) return value;
             value = defaultValue;
             data[setting] = value;
             _didConfigChange = true;
             return value;
         }

         private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
         {
             if (entity == null || hitInfo == null) return;
             for (var i = 0; i < DamageTypeMax; i++)
             {
                 hitInfo.damageTypes.Scale((DamageType) i, _modifiers[i]);
             }
         }
     }
}