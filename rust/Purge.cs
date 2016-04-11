using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins {
    [Info("Purge", "innominata", "0.0.1")] //concept by /u/leftysix on reddit.com/r/playrust
    class Purge : RustPlugin {
        public int sunset = 18; // hour to start purge
        public int sunrise = 8; // hour to end purge
        private int tickCount = 0;
        private bool night;
        
        void startPurge() {
            Announce("Night has fallen. PVP Enabled. Buildings Take Damage.");
        }
        void endPurge() {
            Announce("Night has ended. PVP Disabled. Buildings Invulnerable.");
        }

        private bool isNight(){
        	return !(isDay());
        }
        private bool isDay(){
        	return (TOD_Sky.Instance.Cycle.Hour <= sunset && TOD_Sky.Instance.Cycle.Hour >= sunrise);
        }

        private void checkTime(){
        	if (isNight() && night) return;
        	if (isDay() && !night)  return;
        	if (isDay() && night) { 
        		night = false;
        		endPurge();
        	 	return; }
        	if (isNight() && !night) { 
        		night = true;
        		startPurge();
        		return; 
        	}
        }

        [HookMethod("OnTick")]
        private void OnTick() {
            try {
                if (tickCount < 60) {
                	tickCount++;
                }
                else {
                	checkTime();
                	tickCount = 0;
                }
            }
            catch (Exception ex) {
                PrintError("{0}: {1}", Title,"OnTick failed: " + ex.Message); //Sure, lets keep this in, just in case :)
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
        	if (night) return; //Free for all
			if (entity is BasePlayer && info.Initiator == null) return; //World damage (Drowning, falling, radiation...)
			if (!(entity is BasePlayer)) {
	        	string name = entity.name.ToString();
	        	if (name.Contains("loot")) return; //We want loot barrels to be breakable during day

	            var block = entity as BuildingBlock;
	            if (block) {
	            	if (block.grade.ToString() == "Twigs") return; //We want to be able to use twigs as building templates
	            }
	        }
            info.damageTypes = new DamageTypeList(); //Otherwise set all damages to 0
        }

        private void Announce(string msg) {
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                SendReply(player, msg);
            }
        }
    }

}

