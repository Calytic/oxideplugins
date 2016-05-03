using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("WellFed", "ColonBlow", "1.1.5", ResourceId = 1233)]
    class WellFed : RustPlugin
    {

	public float LoginWellFedHealth => Config.Get<float>("Login Health");
	public float SpawnWellFedHealth => Config.Get<float>("Spawn Health");
	public float LoginWellFedHunger => Config.Get<float>("Login Hunger");
	public float SpawnWellFedHunger => Config.Get<float>("Spawn Hunger");
	public float LoginWellFedThirst => Config.Get<float>("Login Thirst");
	public float SpawnWellFedThirst => Config.Get<float>("Spawn Thirst");
	
        protected override void LoadDefaultConfig()
        {

            Config["Login Health"] = 100f;
            Config["Spawn Health"] = 100f;
	    Config["Login Hunger"] = 500f;
	    Config["Spawn Hunger"] = 500f;
	    Config["Login Thirst"] = 500f;
	    Config["Spawn Thirst"] = 500f;

            SaveConfig();
        }

        void OnServerInitialized()
        {
           	permission.RegisterPermission("wellfed.onlogin", this);
		permission.RegisterPermission("wellfed.onspawn", this);
    	}

	void OnPlayerInit(BasePlayer player)

        {
		if (CanBeFed(player, "wellfed.onlogin"))
			{
                	player.metabolism.hydration.value = LoginWellFedThirst;
                	player.metabolism.calories.value = LoginWellFedHunger;
                	player.health = LoginWellFedHealth;
			}
	return;
	}

	void OnPlayerRespawned(BasePlayer player)

        {
		if (CanBeFed(player, "wellfed.onspawn"))
			{
                	player.metabolism.hydration.value = SpawnWellFedThirst;
                	player.metabolism.calories.value = SpawnWellFedHunger;
                	player.health = SpawnWellFedHealth;
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
