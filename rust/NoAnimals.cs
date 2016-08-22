using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
  [Info("NoAnimals", "Phraxxer", "0.0.1", ResourceId = 1337)]
  [Description("This plugin removes all animals from your server.")]
  class NoAnimals : RustPlugin
  {
    private bool serverInit = false;
    private List<string> animal_list = new List<string>() { "bear", "stag", "chicken", "boar", "horse", "wolf" };
	
    private bool checkEnt(BaseNetworkable entity)
    {
        if (entity.isActiveAndEnabled && animal_list.Contains(entity.LookupPrefab().name))
        {
            entity.Kill();
            return true;
        }
        return false;
    }
		
    void OnServerInitialized()
    {
		var animals = Resources.FindObjectsOfTypeAll<BaseNPC>();
		int count_animal = 0;
		
        foreach (var b in animals)
        {
            if (checkEnt(b))
                count_animal++;
        }
		
		Puts($"Deleted {count_animal} animals on server start.");
        serverInit = true;
    }

    void OnEntitySpawned(BaseNetworkable entity)
    {
        if (!serverInit)
            return;
        if(checkEnt(entity))
            return;
    }
  }
}
