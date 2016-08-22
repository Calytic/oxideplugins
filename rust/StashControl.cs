using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("StashControl", "Piarb", "1.0.1", ResourceId = 1950)]
    [Description("Manage small stashes")]
    public class StashControl : RustPlugin
    {
	    //mode
		public bool PerpetuumStashes;
		public bool ConstDecayStashes;
		//settings
		public bool Debug;
		public int StashControlTick;
		public int StashLifeTime;
		public bool RepairOnLoot;
		public bool HiddenDecayOnly;
		
	    private bool ConfigChanged;
		private float hurt_amount;
		
		protected override void LoadDefaultConfig() => Puts("New configuration file created.");		
        
        void LoadConfig()
        {
            ConstDecayStashes = Convert.ToBoolean(GetConfigValue("Mode", "ConstDecayStashes", false));
			PerpetuumStashes = Convert.ToBoolean(GetConfigValue("Mode", "PerpetuumStashes", true));
			Debug = Convert.ToBoolean(GetConfigValue("Settings", "Debug", false));
			StashControlTick = Convert.ToInt32(GetConfigValue("Settings", "StashControlTick", "3600"));
			StashLifeTime = Convert.ToInt32(GetConfigValue("Settings", "StashLifeTime", "86400"));
			RepairOnLoot = Convert.ToBoolean(GetConfigValue("Settings", "RepairOnLoot", true));
			HiddenDecayOnly = Convert.ToBoolean(GetConfigValue("Settings", "HiddenDecayOnly", true));
            if (ConfigChanged)
            {
                Puts("Configuration file updated.");
                SaveConfig();
            }
        }

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                ConfigChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return value;
            value = defaultValue;
            data[setting] = value;
            ConfigChanged = true;
            return value;
        }
				
        void Loaded()
        {
            //Puts("Stash controller activated");
			LoadConfig();
            //mode check
			if (ConstDecayStashes && PerpetuumStashes)
				{
					Puts("You must set only one Mode = true for stashes! Reset to default.");
					PerpetuumStashes = true;
					ConstDecayStashes = false;
				}
			if (StashLifeTime/StashControlTick < 3f)
				{
					Puts("StashControlTick must be several times smaller than StashLifeTime.");
					StashLifeTime = StashControlTick * 3;
					Puts("Set StashLifeTime = StashControlTick * 3 = "+StashLifeTime);
				}
						
			if (PerpetuumStashes) NoDecayStashes();
			if (ConstDecayStashes) HurtStahes();
        }
		
        private void NoDecayStashes()
        {
			if (Debug) Puts("Timer shot at "+Time.realtimeSinceStartup);
            var stashes = UnityEngine.Object.FindObjectsOfType<StashContainer>();
            if (stashes.Length == 0)
            {
                if (Debug) Puts("There is no stashes in game.");
            }
			
			var i = 1;
            foreach (StashContainer stash in stashes)
            {
                if (Debug) Puts("Found stash #"+Convert.ToString(i++)+" at "+(int)stash.transform.position.x + " " + (int)stash.transform.position.y + " " + (int)stash.transform.position.z+" INFO MaxH:"+Convert.ToString(stash.MaxHealth())+" Health:"+Convert.ToString(stash.Health()));
				
				stash.CancelInvoke("Decay");
				stash.Invoke("Decay", 259200f);
            }
            timer.In(StashControlTick, () => NoDecayStashes());
		}
		
		private void HurtStahes()
		{
			if (Debug) Puts("Timer shot at "+Time.realtimeSinceStartup);
            var stashes = UnityEngine.Object.FindObjectsOfType<StashContainer>();
            if (stashes.Length == 0)
            {
                if (Debug) Puts("There is no stashes in game.");
            }
				else
					hurt_amount = stashes[0].MaxHealth() / (StashLifeTime / StashControlTick);
				
			var i = 1;
            foreach (StashContainer stash in stashes)
            {
                if (Debug) Puts("Found stash #"+Convert.ToString(i++)+" at "+(int)stash.transform.position.x + " " + (int)stash.transform.position.y + " " + (int)stash.transform.position.z+" INFO MaxH:"+Convert.ToString(stash.MaxHealth())+" Health:"+Convert.ToString(stash.Health()));
				stash.CancelInvoke("Decay");
				stash.Invoke("Decay", 259200f);
				
				if (HiddenDecayOnly)
				{
					if (stash.IsHidden())
						stash.Hurt(hurt_amount*10);
				}
				else
					stash.Hurt(hurt_amount*10);
				
				if (Debug) Puts("hurt_amount: "+hurt_amount+" After Hurt: "+Convert.ToString(stash.Health()));
            }
            timer.In(StashControlTick, () => HurtStahes());
		}
		
		void OnLootEntity(BasePlayer player, BaseCombatEntity entity)
		{
			if (RepairOnLoot)
			{
				if (entity.LookupPrefab().name == "small_stash_deployed.prefab")
				{
					entity.health = entity.MaxHealth();
					if (Debug) Puts("Stash health restored");
				}				
			}

		}

    }
}