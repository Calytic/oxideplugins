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
    [Info("Explosion Tracker", "PaiN", 0.7, ResourceId = 1282)]
    [Description("This plugin tracks every explosion that happens in the server.")]
    class ExplosionTracker : RustPlugin
    {
		private bool Changed;
		private bool logtofile;
		private bool logtorcon;
		
		void Loaded()
        {
            LoadVariables();
        }

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

        void LoadVariables()
        {

            logtofile = Convert.ToBoolean(GetConfig("Settings", "LogToFile", true));
            logtorcon = Convert.ToBoolean(GetConfig("Settings", "LogToRcon", true));

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
		
		void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            NextTick(() => {
                if (logtorcon == true)
                {
                    Puts("**" + player.displayName + "**" + "(" + player.userID.ToString() + ")" + " threw " + entity.name.ToString() +
                    " at position " +
                    "( X: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().x).ToString() +
                    " Y: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().y).ToString() +
                    " Z: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().z).ToString() + " )");
                }
                if (logtofile == true)
                {
                    ConVar.Server.Log("Oxide/Logs/ExplosionTrackerLog.txt", "**" + player.displayName + "**" + "(" + player.userID.ToString() + ")" + " threw " + entity.name.ToString() +
                    " at position " +
                    "( X: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().x).ToString() +
                    " Y: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().y).ToString() +
                    " Z: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().z).ToString() + " )");
                }
            });
        }
			
			void OnRocketLaunched(BasePlayer player, BaseEntity entity)
			{
				NextTick(() => {
				if(logtorcon == true)
				{
					Puts("**"+player.displayName+"**" +"(" + player.userID.ToString() + ")" + " launched a rocket at " +
					"( X: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().x).ToString() + 
					" Y: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().y).ToString() + 
					" Z: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().z).ToString() + " )");
				}
				if(logtofile == true) 
				{
					ConVar.Server.Log("Oxide/Logs/ExplosionTrackerLog.txt", "**"+player.displayName+"**" +"(" + player.userID.ToString() + ")" + " launched a rocket at " +
					"( X: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().x).ToString() + 
					" Y: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().y).ToString() + 
					" Z: " + Convert.ToInt32(entity.GetEstimatedWorldPosition().z).ToString() + " )");
				}
				});
			}
	}
}
