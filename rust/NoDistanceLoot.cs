using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;


namespace Oxide.Plugins
{
    [Info("NoDistanceLoot", "4seti [Lunatiq] for Rust Planet", "0.1.1", ResourceId = 000)]
    public class NoDistanceLoot : RustPlugin
    {

        #region Utility Methods

        private void Log(string message)
        {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message)
        {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message)
        {
            PrintError("{0}: {1}", Title, message);
        }

        #endregion


        void Loaded()
        {
            Log("Loaded");
        }

        Dictionary<BasePlayer, string> looters = new Dictionary<BasePlayer, string>();

        [HookMethod("OnPlayerLoot")]
        void OnPlayerLoot(PlayerLoot lootInventory, UnityEngine.Object entry)
        {
            BasePlayer looter = lootInventory.GetComponent("BasePlayer") as BasePlayer;
            if (looters.ContainsKey(looter))            
                looters.Remove(looter);
            
            if (entry is BasePlayer)
            {
                BasePlayer target = entry as BasePlayer;              
                if (target.IsAlive() && !target.IsSleeping())
                {
                    looter.ChatMessage("Finish him before loot!");
                    looter.SendConsoleCommand("inventory.endloot");
                    looter.UpdateNetworkGroup();
                    looter.SendFullSnapshot();
                }
                else if (target.IsSleeping())
                {
                    looters.Add(looter, target.userID.ToString());
                }
            }

        }
        [HookMethod("OnPlayerSleepEnded")]
        void OnPlayerSleepEnded(BasePlayer player)
        {
			if (player != null)
			{
				if (looters.ContainsValue(player.userID.ToString()))
				{
					var looter = looters.FirstOrDefault(x => x.Value == player.userID.ToString()).Key;		
					if(looter.IsConnected()){					
						looter.SendConsoleCommand("inventory.endloot");
						looter.UpdateNetworkGroup();
						looter.SendFullSnapshot();
					}					
						looters.Remove(looter);					
				}
			}
        }
    }
}