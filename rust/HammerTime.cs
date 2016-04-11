using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hammer Time", "Shady", "1.0.3", ResourceId = 1711)]
    [Description("Tweak settings for building blocks like demolish time, and rotate time.")]
    class HammerTime : RustPlugin
    {

        /*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["DemolishTime"] = 600f;
            Config["RotateTime"] = 600f;
            SaveConfig();
        }

            void DoInvokes(BuildingBlock block, bool demo, bool rotate, bool justCreated)
        {
            var demoTime = 600f;
            var rotateTime = 600f;
            float.TryParse(Config["DemolishTime"].ToString(), out demoTime);
            float.TryParse(Config["RotateTime"].ToString(), out rotateTime);
            if (demo)
            {
                if (demoTime < 0) block.CancelInvoke("StopBeingDemolishable");
                if (demoTime == 0) block.Invoke("StopBeingDemolishable", 0.01f);
                if (demoTime >= 1 && demoTime != 600) //if time is = to 600, then it's default, and there's no point in changing anything
                {
                    block.CancelInvoke("StopBeingDemolishable");
                    block.Invoke("StopBeingDemolishable", demoTime);
                    block.SendNetworkUpdateImmediate(justCreated);
                }
            }
            if (rotate)
            {
                if (rotateTime < 0) block.CancelInvoke("StopBeingRotatable");
                if (rotateTime == 0) block.Invoke("StopBeingRotatable", 0.01f);
                if (rotateTime >= 1 && rotateTime != 600) //if time is = to 600, then it's default, and there's no point in changing anything
                {
                    block.CancelInvoke("StopBeingRotatable");
                    block.Invoke("StopBeingRotatable", rotateTime);
                    block.SendNetworkUpdateImmediate(justCreated);
                }
            }
        }

        private void OnEntityBuilt(Planner plan, GameObject objectBlock)
        {
            var GetTypeString = objectBlock?.ToBaseEntity()?.GetType()?.ToString();
            var isBuildingBlock = GetTypeString == "BuildingBlock";
            if (!isBuildingBlock) return;
            var block = (BuildingBlock)objectBlock.ToBaseEntity();
            if (block == null) return;
            DoInvokes(block, true, true, true);
           
        }
        private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (block == null) return;
            DoInvokes(block, false, true, false);
        }
    }
}