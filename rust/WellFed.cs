using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("WellFed", "ColonBlow", "1.1.3", ResourceId = 1233)]
    class WellFed : RustPlugin
    {

	public float LoginWellFedHealth => Config.Get<float>("Login Health");
	public float SpawnWellFedHealth => Config.Get<float>("Spawn Health");
	public float LoginWellFedHunger => Config.Get<float>("Login Hunger");
	public float SpawnWellFedHunger => Config.Get<float>("Spawn Hunger");
	public float LoginWellFedThirst => Config.Get<float>("Login Thirst");
	public float SpawnWellFedThirst => Config.Get<float>("Spawn Thirst");
	public bool WellFedOnLogin => Config.Get<bool>("Enable WellFed On Login");
	public bool WellFedOnSpawn => Config.Get<bool>("Enable WellFed On Spawn");
	
        protected override void LoadDefaultConfig()
        {

            Config["Login Health"] = 100;
            Config["Spawn Health"] = 100;
	    Config["Login Hunger"] = 1000;
	    Config["Spawn Hunger"] = 1000;
	    Config["Login Thirst"] = 1000;
	    Config["Spawn Thirst"] = 1000;
            Config["Enable WellFed On Login"] = true;
	    Config["Enable WellFed On Spawn"] = true;

            SaveConfig();
        }

        void OnServerInitialized()
        {
           	permission.RegisterPermission("wellfed.onlogin", this);
		permission.RegisterPermission("wellfed.onspawn", this);
    	}

	void OnPlayerInit(BasePlayer player)

        {
	if (WellFedOnLogin == true)
		{
		if (CanBeFed(player, "wellfed.onlogin"))
			{
                	player.metabolism.hydration.value = LoginWellFedThirst;
                	player.metabolism.calories.value = LoginWellFedHunger;
                	player.InitializeHealth(LoginWellFedHealth, 100);
			}
		return;
		}
	return;
	}

	void OnPlayerRespawned(BasePlayer player)

        {
	if (WellFedOnSpawn == true)
		{
		if (CanBeFed(player, "wellfed.onspawn"))
			{
                	player.metabolism.hydration.value = SpawnWellFedThirst;
                	player.metabolism.calories.value = SpawnWellFedHunger;
                	player.InitializeHealth(SpawnWellFedHealth, 100);
			}
		return;
		}
	return;
	}

        bool CanBeFed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            return false;
        }

    }
}
