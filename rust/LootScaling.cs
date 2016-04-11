using UnityEngine;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Loot Scaling", "Kyrah Abattoir", "0.1", ResourceId = 1874)]
    [Description("Scale loot spawn rate/density by player count.")]
    class LootScaling : RustPlugin
    {
        //Existing spawn categories as of 08/26/2015
        //------------------------------------
        //animals loot
        //ores
        //roadside
        //collectable-food-mushroom
        //collectable-resource-stone
        //collectable-resource-hemp
        //field-tundra
        //forest-temperate
        //forest-tundra
        //forest-arctic 
        //forest-arid
        //forest-tundra-commons
        //forest-tundra-rares
        //beachside-deadtrees
        //beachside-palms
        //plant-pumpkin
        //plant-corn beachside-trees

        //you can put here which of the spawn categories you wish to enable player count scaling on.
        //Rust will then adjust item density based on the percentage of players online from 10% rate/density to 100% rate/density
        List<string> cfgPopulationScaling = new List<string>(new string[] {
            "loot",
            "roadside"
        });

        //NOTE the minimum spawn rate/density are two arbitrary values in the engine from 0.1 to 1.0 (10%/100%)
        //you can override them by passing new values to the server with:
        //
        //spawn.min_rate
        //spawn.max_rate
        //spawn.min_density
        //spawn.max_density
        //
        //I have NOT tested changing these, but setting min_rate/density to 0 is probably NOT a good idea so don't do it!

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            foreach (SpawnPopulation s in SingletonComponent<SpawnHandler>.Instance.SpawnPopulations)
            {
                if (cfgPopulationScaling.Contains(s.name))
                {
                    //Well since FacePunch already implemented it all for us, we should probably use it.
                    s.ScaleWithServerPopulation = true;
                    Puts($"Enabled loot scaling for: {s.name}");
                }
            }
        }
    }
}