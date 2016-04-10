// Reference: Oxide.Ext.Rust

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("BlockBlockElevators", "Reneb", "1.0.1")]
    class BlockBlockElevators : RustPlugin
    {

        BuildingBlock cachedBlock;
        TriggerBase cachedTrigger;
        BasePlayer cachedPlayer;
        int playerMask;
        int blockLayer;
        int triggerLayer;

        bool hasStarted = false;

        void OnServerInitialized()
        {
            hasStarted = true;
            playerMask = LayerMask.GetMask(new string[] { "Player (Server)" });
            triggerLayer = UnityEngine.LayerMask.NameToLayer("Trigger");
            blockLayer = UnityEngine.LayerMask.NameToLayer("Construction");
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            cachedBlock = gameObject.GetComponent<BuildingBlock>();
            if (cachedBlock == null) return;
            if (cachedBlock.blockDefinition == null) return;
            if (cachedBlock.blockDefinition.fullName != "build/block.halfheight") return;
            cachedBlock.GetComponentInChildren<MeshCollider>().gameObject.AddComponent<TriggerBase>();
            cachedBlock.GetComponentInChildren<TriggerBase>().gameObject.layer = triggerLayer;
            cachedBlock.GetComponentInChildren<TriggerBase>().interestLayers = playerMask;
            timer.Once(0.1f, () => ResetBlock(cachedBlock) );

        }
        void ResetBlock(BuildingBlock block)
        {
            if (block == null) return;
            block.constructionCollision.UpdateLayer(false);
            GameObject.Destroy(block.GetComponentInChildren<TriggerBase>());
        }
        void OnEntityEnter(TriggerBase triggerbase, BaseEntity entity)
        {
            if (!hasStarted) return;
            cachedBlock = triggerbase.GetComponentInParent<BuildingBlock>();
            if (cachedBlock == null) return;
            if (cachedBlock.blockDefinition.fullName != "build/block.halfheight") return;
            cachedPlayer = entity.GetComponent<BasePlayer>();
            if (cachedPlayer == null) return;
            cachedBlock.Kill(BaseNetworkable.DestroyMode.Gib);
            cachedPlayer.SendConsoleCommand("chat.add", new object[] { "0", string.Format("<color=orange>{0}:</color> {1}", "Warning", "You are not allowed to build blocks over you"), 1.0 });
        }
    }
}
