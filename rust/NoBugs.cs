using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("NoBugs", "azalea`", "1.3", ResourceId = 1778)]
    class NoBugs : RustPlugin
    {
        void OnServerInitialized()
        {
            foreach (var stash in UnityEngine.Object.FindObjectsOfType<StashContainer>())
            {
                SetupObject(stash.gameObject);
            }

            foreach (var stocking in UnityEngine.Object.FindObjectsOfType<Stocking>())
            {
                SetupObject(stocking.gameObject);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsDead())
                player.RemoveFromTriggers();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.IsAlive() && !player.IsOnGround() && !player.IsAdmin())
                player.Die();
        }

        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            if (obj.GetComponent<StashContainer>() != null || obj.GetComponent<Stocking>() != null)
            {
                SetupObject(obj);

                return;
            }

            if (obj.GetComponentInParent<BuildingBlock>() != null)
            {
                BuildingBlock Block = obj.GetComponentInParent<BuildingBlock>();
                
                if (!Block.blockDefinition.hierachyName.StartsWith("foundation")) return;

                RaycastHit hitInfo;

                if (Physics.Raycast(new Ray(obj.transform.position, Vector3.down), out hitInfo, float.PositiveInfinity))
                {
                    if (System.Math.Round(hitInfo.distance, 2) == 0.00d)
                        Block.KillMessage();
                }
                else    Block.KillMessage();
            }
        }

        void SetupObject(GameObject Object)
        {
            Object.layer = (int)Layer.Prevent_Building;
            //Object.transform.localScale = new Vector3(1f, 3f, 1f);
        }
    }
}