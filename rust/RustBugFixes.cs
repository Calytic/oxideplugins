using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rust Bug Fixes", "Mughisi", 1.0)]
    class RustBugFixes : RustPlugin
    {

        /* Includes 'fixes' for the following bugs:
         *  - Quarry inventories' capacity is limited to 6 and should be 18.
         */

        void OnServerInitialized()
        {
            var quarries = UnityEngine.Object.FindObjectsOfType<MiningQuarry>();
            foreach (var quarry in quarries)
            {
                quarry.hopperPrefab.instance.GetComponent<StorageContainer>().inventory.capacity = 18;
                quarry.fuelStoragePrefab.instance.GetComponent<StorageContainer>().inventory.capacity = 18;
            }
        }

        void OnEntityBuilt(Planner planner, GameObject component)
        {
            var quarry = component.ToBaseEntity() as MiningQuarry;
            if (!quarry) return;
            quarry.hopperPrefab.instance.GetComponent<StorageContainer>().inventory.capacity = 18;
            quarry.fuelStoragePrefab.instance.GetComponent<StorageContainer>().inventory.capacity = 18;
        }
    }
}
