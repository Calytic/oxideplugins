using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoStashBug", "azalea`", "1.1")]
    class NoStashBug : RustPlugin
    {
        static int PreventBuilding = LayerMask.NameToLayer("Prevent Building");

        void OnServerInitialized()
        {
            var Stashes = UnityEngine.Object.FindObjectsOfType<StashContainer>();

            foreach (var stash in Stashes)
            {
                stash.gameObject.layer = PreventBuilding;
                stash.gameObject.transform.localScale = new Vector3(1f, 3f, 1f);
            }
        }

        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            if (obj.GetComponent<StashContainer>() != null)
            {           
                obj.layer = PreventBuilding;
                obj.transform.localScale = new Vector3(1f, 3f, 1f);
            }
        }

    }
}