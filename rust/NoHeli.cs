using Oxide.Core;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("NoHeli", "HoPollo", "1.0.1")]
    class NoHeli : RustPlugin
    {
		private readonly string heliPrefab = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
		
		
		private uint heliPrefabId;
		
        void OnEntitySpawned(BaseNetworkable entity)
        {
			
			heliPrefabId = StringPool.Get(heliPrefab);
        	
            if (entity == null) return;
            if (entity.prefabID == heliPrefabId)
            {
                entity.KillMessage();
                Puts("NoHeli : Patrol Stopped!");
				//PrintToConsole("Heli Stopped"); If you want to broadcast on console
				
				return;
            }
		}		
    }
}
