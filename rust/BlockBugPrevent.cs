using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BlockBugPrevent", "sami37", "1.0.0", ResourceId = 2166)]
    [Description("Prevent foundation block build on another foundation.")]
    public class BlockBugPrevent : RustPlugin
    {
        void Loaded()
        {
			lang.RegisterMessages(new Dictionary<string,string>{
				["NotAllowed"] = "<color='#DD0000'>Your are not allowed to build foundation here.</color>"
			}, this);
        }
        static int colisionentity = LayerMask.GetMask("Construction");
        void OnEntityBuilt(Planner planner, UnityEngine.GameObject gameObject)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null) return;
			BuildingBlock block = gameObject.GetComponent<BuildingBlock>();
			if (block == null) return;
			Vector3 sourcepos = block.transform.position;
			RaycastHit initial_hit;
			if (Physics.Raycast(sourcepos, Vector3.down, out initial_hit, colisionentity))
			{
				var entity = initial_hit.collider.GetComponentInParent<BuildingBlock>();
				if (entity != null)
				{
					if (block.LookupPrefab().name.Contains("foundation"))
					{
						block.KillMessage();
						SendReply(player, lang.GetMessage("NotAllowed", this));
					}
				}
			}
        }
    }
}