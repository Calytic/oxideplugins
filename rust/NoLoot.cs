using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
  [Info("NoLoot", "Virobeast", "0.0.2", ResourceId = 1488)]
  [Description("This plugin removes all loot from your server.")]
  class NoLoot : RustPlugin
  {
    private bool server_ready = false;
    private List<string> barrels = new List<string>() { "loot-barrel-1.prefab", "loot-barrel-2.prefab", "crate_normal.prefab", "crate_normal_2.prefab", "crate_normal_2_food.prefab", "crate_normal_2_medical.prefab", "loot_trash.prefab", "cargo_plane.prefab", "oil_barrel.prefab", "trash-pile-1.prefab", "loot_barrel_1.prefab", "patrolhelicopter.prefab", "loot_barrel_2.prefab" };

    private bool ProcessBarrel(BaseNetworkable entity)
    {
        if (entity.isActiveAndEnabled && barrels.Contains(entity.LookupShortPrefabName()))
        {
            entity.Kill();
            return true;
        }
        return false;
    }

    void OnServerInitialized()
    {
        LootContainer[] loot = Resources.FindObjectsOfTypeAll<LootContainer>();
        int count = 0;
        foreach (var entity in loot)
        {
            if (ProcessBarrel(entity))
                count++;
        }
        Puts($"Deleted {count} loot entities on server start.");
        server_ready = true;
    }

    void OnEntitySpawned(BaseNetworkable entity)
    {
        if (!server_ready)
            return;
        if(ProcessBarrel(entity))
            return;
    }
  }
}
