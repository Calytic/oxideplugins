using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Facepunch;
using Oxide.Core.Plugins;
using Rust;
using System.Globalization;
using System.Linq;

using UnityEngine;
namespace Oxide.Plugins
{
    [Info("AntiDecay", "copper- Cleaned by Wulf", "1.0.0")]
    class AntiDecay : RustLegacyPlugin
    {
		// Clean Build by Wulf
		
		private Core.Configuration.DynamicConfigFile Data;
		void Loaded()
        {
			Debug.Log("Anti Decay Started here are settings - Enabled Means AntiDecay is Enabled on that object :D ");
		    foreach (KeyValuePair<string, object> pair in defaultobjects)
			{
				Debug.Log(pair.Key.ToString() + " " + pair.Value.ToString());
			}
        }
		void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
			foreach (KeyValuePair<string, object> pair in Config)
			{
				if(pair.Key.ToString() == Key)
				{
					if (Config[pair.Key.ToString()] is T)
						var = (T)Config[pair.Key.ToString()];
					return;
				}
			}
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
		public static bool shoulddisabledecay = false;
		public static Dictionary<string,object> defaultobjects = Defaultobject();

        void Init()
        {
			CheckCfg<Dictionary<string,object>>("Default Objects", ref defaultobjects);
			CheckCfg<bool>("Messages: Should Disable Decay on all objects?", ref shoulddisabledecay);
			SaveConfig();
        }
		object ModifyDamage(TakeDamage takedamage, ref DamageEvent damage, object tags)
		{
		    if(!damage.attacker.id.GetComponent<EnvDecay>())
				return null;
			if(shoulddisabledecay)
			{
				return CancelDamage(damage);;
			}
			foreach (KeyValuePair<string, object> pair in defaultobjects)
			{
				if(takedamage.gameObject.name.ToString().ToLower().Contains(pair.Key.ToString().ToLower()))
				{
					if((bool)defaultobjects[pair.Key.ToString()] == true)
					{
						return CancelDamage(damage);
						break;
						continue;
					}
					
				} 
			}
			return null;
			
		}
		object CancelDamage(DamageEvent damage)
        {
            damage.amount = 0f;
            damage.status = LifeStatus.IsAlive;
            return damage;
        }
	    static Dictionary<string,object> Defaultobject()
        {
            var newdict = new Dictionary<string, object>();
            newdict.Add("Shelter",false);
            newdict.Add("Ceiling", true);
            newdict.Add("Foundation",true);
            newdict.Add("Wall",true);
            newdict.Add("Pillar",true);
            newdict.Add("Ramp",true);
            newdict.Add("Window", true);
			newdict.Add("Spike", true);
			newdict.Add("Box", true);
			newdict.Add("Furnace", true);
			newdict.Add("Baricade", true);
			newdict.Add("Campfire", false);
			newdict.Add("Bench", true);
			newdict.Add("Door", true);
			newdict.Add("Stair", true);
			newdict.Add("Gate", true);
            return newdict;
        }
	}
}

















			