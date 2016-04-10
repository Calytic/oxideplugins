using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using System.Linq;
using Rust;

namespace Oxide.Plugins
{
    [Info("Supply Drop Destroyer", "PaiN", 0.2, ResourceId = 1281)]
    [Description("This plugin destroys the supply drop container after x seconds.")]
    class SupplyDropDestroyer : RustPlugin
	{
		private bool Changed;
		private int destroyafter;
		
		object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        } 
		
		void Loaded()
		{
			LoadVariables();	
		}
		
		void LoadVariables()
		{
			destroyafter = Convert.ToInt32(GetConfig("Settings", "DestroyAfter", 300));
			
			if (Changed)
			{
				SaveConfig();
				Changed = false;
			
			}	
		} 
	
		protected override void LoadDefaultConfig()
		{
			Puts("Creating a new configuration file!");
			Config.Clear();
			LoadVariables();
		}
	
		void OnEntitySpawned(BaseNetworkable entity)
		{ 
			if(entity as SupplyDrop)
			{
				timer.Once(destroyafter, () =>	{ 
				entity.Kill();
				Puts("Successfully destroyed " + entity.name.ToString());
				});
			}
		}
	}
}