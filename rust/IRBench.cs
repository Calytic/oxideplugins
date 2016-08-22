using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IRBench", "Werkrat", "0.1.0")]
    [Description("Repair without losing durability!")]
    class IRBench : RustPlugin
    {
		protected override void LoadDefaultConfig() {
			//Default is 0.2 - so leave it default by default xD
            Config["ConditionLostOnRepair"] = 0.2;
        }
		
		private float conditionLost;
		
		[HookMethod("OnServerInitialized")]
        private void onServerInitialized() 
		{
			LoadConfig();
			conditionLost = Config.Get<float>("ConditionLostOnRepair");
			
			if (conditionLost >= 1 || conditionLost < 0)
			{
				PrintError("ConditionLostOnRepair setting invalid, resetting to default");
				LoadConfig();
				conditionLost = (float)0.2;
				SaveConfig();
			}
				
			var benches = UnityEngine.Object.FindObjectsOfType<RepairBench>();
            var updated = 0;
			var count = 0;
            foreach (var rbs in benches)
			{
				++count;
				if (rbs.maxConditionLostOnRepair != conditionLost)
				{
					rbs.maxConditionLostOnRepair = conditionLost;
					++updated;
				}
			}
			Puts("Updated [" + updated.ToString() + "/" + count.ToString() + "] repair benches. New condition lost on repair setting: " + conditionLost.ToString());
		}
		[HookMethod("OnEntitySpawned")]
		private void onEntitySpawned(BaseNetworkable ent) 
		{
            if (ent is RepairBench)   
			{
				//Update newly placed repair benches...
				RepairBench entity = ent as RepairBench;
				entity.maxConditionLostOnRepair = conditionLost;
			}
		}
	}
}