namespace Oxide.Plugins
{
    [Info("DMDeployables", "ColonBlow", "1.1.5", ResourceId = 1240)]
    class DMDeployables : RustPlugin
    {


	public bool ProtectToolCupboard => Config.Get<bool>("ProtectToolCupboard");
	public bool ProtectSignage => Config.Get<bool>("ProtectSignage");
	public bool ProtectBaseOven => Config.Get<bool>("ProtectBaseOven");
        public bool ProtectMiningQuarry => Config.Get<bool>("ProtectMiningQuarry");
	public bool ProtectWaterCatcher => Config.Get<bool>("ProtectWaterCatcher");
	public bool ProtectResearchTable => Config.Get<bool>("ProtectResearchTable");
	public bool ProtectSleepingBag => Config.Get<bool>("ProtectSleepingBag");
        public bool ProtectRepairBench => Config.Get<bool>("ProtectRepairBench");
        public bool ProtectAutoTurret => Config.Get<bool>("ProtectAutoTurret");
	public bool ProtectBoxes => Config.Get<bool>("ProtectBoxes");
	public bool ProtectBarricade => Config.Get<bool>("ProtectBarricade");
	public bool ProtectSingleDoors => Config.Get<bool>("ProtectSingleDoors");
	public bool ProtectDoubleDoors => Config.Get<bool>("ProtectDoubleDoors");
	public bool ProtectWindows => Config.Get<bool>("ProtectWindows");
	public bool ProtectShutters => Config.Get<bool>("ProtectShutters");
	public bool ProtectShelves => Config.Get<bool>("ProtectShelves");
	public bool ProtectChainLink => Config.Get<bool>("ProtectChainLink");
	public bool ProtectPrisonCell => Config.Get<bool>("ProtectPrisonCell");
	public bool ProtectShopFront => Config.Get<bool>("ProtectShopFront");
	public bool ProtectExternalWalls => Config.Get<bool>("ProtectExternalWalls");
	public bool ProtectExternalGates => Config.Get<bool>("ProtectExternalGates");
	public bool ProtectFloorGrill => Config.Get<bool>("ProtectFloorGrill");
	public bool ProtectFloorLadder => Config.Get<bool>("ProtectFloorLadder");

        protected override void LoadDefaultConfig()
        	{
            	Config["ProtectToolCupboard"] = true;
	    	Config["ProtectSignage"] = true;
	    	Config["ProtectBaseOven"] = true;
	   	Config["ProtectMiningQuarry"] = true;
           	Config["ProtectWaterCatcher"] = true;
	   	Config["ProtectResearchTable"] = true;
	    	Config["ProtectSleepingBag"] = true;
	    	Config["ProtectRepairBench"] = true;
	    	Config["ProtectAutoTurret"] = true;
	    	Config["ProtectBoxes"] = true;
		Config["ProtectBarricade"] = true;
		Config["ProtectSingleDoors"] = true;
		Config["ProtectDoubleDoors"] = true;
		Config["ProtectWindows"] = true;
		Config["ProtectShutters"] = true;
		Config["ProtectShelves"] = true;
		Config["ProtectChainLink"] = true;
		Config["ProtectPrisonCell"] = true;
		Config["ProtectShopFront"] = true;
		Config["ProtectExternalGates"] = true;
		Config["ProtectExternalWalls"] = true;
		Config["ProtectFloorGrill"] = true;
		Config["ProtectFloorLadder"] = true;

            	SaveConfig();
        	}

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {

		if ((entity.name.Contains("cupboard.tool")) & (ProtectToolCupboard == true)) return false;
				
            	if (entity as Signage != null && hitInfo.damageTypes.GetMajorityDamageType().ToString().Contains(""))
				{
				if (ProtectSignage == true)
					{
                   			hitInfo.damageTypes = new Rust.DamageTypeList();
					}
				}

		// Lanterns, Furnaces, Campfires Protection
           	   if (entity as BaseOven != null && hitInfo.damageTypes.GetMajorityDamageType().ToString().Contains(""))
				{
				if (ProtectBaseOven == true)
					{
                   			hitInfo.damageTypes = new Rust.DamageTypeList();
					}
				}

           	   if (entity as SleepingBag != null && hitInfo.damageTypes.GetMajorityDamageType().ToString().Contains(""))
				{
				if (ProtectSleepingBag == true)
					{
                   			hitInfo.damageTypes = new Rust.DamageTypeList();
					}
				}

           	   if (entity as AutoTurret != null && hitInfo.damageTypes.GetMajorityDamageType().ToString().Contains(""))
				{
				if (ProtectAutoTurret == true)
					{
                   			hitInfo.damageTypes = new Rust.DamageTypeList();
					}
				}

		   if ((entity.name.Contains("repair.bench")) & (ProtectRepairBench == true)) return false;

		   if ((entity.name.Contains("mining.quarry")) & (ProtectMiningQuarry == true)) return false;

		   if ((entity.name.Contains("research.table")) & (ProtectResearchTable == true)) return false;

		   if ((entity.name.Contains("barricade")) & (ProtectBarricade == true)) return false;

                   if ((entity.name.Contains("water.catcher")) & (ProtectWaterCatcher == true)) return false;

		   if ((entity.name.Contains("gates.external")) & (ProtectExternalGates == true)) return false;

		   if ((entity.name.Contains("wall.external")) & (ProtectExternalWalls == true)) return false;

           	   if ((entity.name.Contains("door.hinged")) & (ProtectSingleDoors == true)) return false;

           	   if ((entity.name.Contains("door.double")) & (ProtectDoubleDoors == true)) return false;
	
		   if ((entity.name.Contains("box.wooden")) & (ProtectBoxes == true)) return false;
		   if ((entity.name.Contains("stash.small")) & (ProtectBoxes == true)) return false;
		
		   if ((entity.name.Contains("embrasure")) & (ProtectShutters == true)) return false;

		   if ((entity.name.Contains("window.bars")) & (ProtectWindows == true)) return false;

		   if ((entity.name.Contains("shelves")) & (ProtectWindows == true)) return false;

		   if ((entity.name.Contains("frame.fence")) & (ProtectChainLink == true)) return false;

		   if ((entity.name.Contains("frame.cell")) & (ProtectPrisonCell == true)) return false;

		   if ((entity.name.Contains("shopfront")) & (ProtectShopFront == true)) return false;

		   if ((entity.name.Contains("floor.grill")) & (ProtectFloorGrill == true)) return false;

		   if ((entity.name.Contains("floor.ladder")) & (ProtectFloorLadder == true)) return false;		

		return null;
        }
    }
}
