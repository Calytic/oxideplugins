using System;
using Rust;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
     [Info("SafeHomes", "Vypr/Phoenix", "1.0.0")]
     class SafeHomes : RustPlugin
     {
      	

         static DamageTypeList emptyDamage = new DamageTypeList();
      	//public bool playerBuild = player.CanBuild();
             /*public BasePlayer player;
 			public bool BuildingPrivilegeBool = player.CanBuild();*/
        //public player = it.Current:GetComponent("BasePlayer");
 


     	bool BuildingPrivilege(BasePlayer player)
		{
			if (player.CanBuild()) return false;
			return true;
		}

	 void CancelDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = emptyDamage;
            hitinfo.HitEntity = null;
        }
		

     	void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		  {
			      if(entity is BuildingBlock && info.Initiator is BasePlayer) {
              BasePlayer player = (BasePlayer) entity.ToPlayer();
              BasePlayer victim = (BasePlayer) info.Initiator;
               if(BuildingPrivilege(victim)){
                  CancelDamage(info);  
                  SendReply(victim, "ERROR: You do not have access to damage this.");
               }




            }

            
                      // CancelDamage(info);    
            
            
		  }


  }
        
}