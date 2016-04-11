using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
  [Info("NoBarrels", "Kyrah Abattoir", "0.1", ResourceId = 1872)]
  [Description("This plugin removes all non-oil barrels on the server.")]
  class NoBarrels : RustPlugin
  {
    private bool server_ready = false;
    private List<string> barrels = new List<string>() { "loot-barrel-1.prefab", "loot-barrel-2.prefab" };

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
        Puts($"Deleted {count} barrels on server start.");
        server_ready = true;
    }

    void OnEntitySpawned(BaseNetworkable entity)
    {
        if (!server_ready)
            return;
        if(ProcessBarrel(entity))
            Puts($"Deleted {entity.LookupShortPrefabName()}");
    }
  }
}
