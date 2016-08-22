using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BlockStashPlacement", "Flaymar", "1.0.2", ResourceId = 2060)]
    [Description("Blocks small stash placement!")]
    public class BlockStashPlacement : RustPlugin
    {
        void Loaded(){
            lang.RegisterMessages(messages, this);
        }

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"MayNotPlace", "Deploying stashes is not enabled on this server!"},
        };
        void OnEntitySpawned(BaseEntity entity, GameObject gameObject)
        {
            if (entity is StashContainer) 
            {
                var stash = entity.GetComponent<StashContainer>(); 
                var player = BasePlayer.FindByID(stash.OwnerID);
                if (player != null)
                {
                    SendReply(player, lang.GetMessage("MayNotPlace", this, player.UserIDString));
                    player.inventory.GiveItem(ItemManager.CreateByItemID(1051155022));
                }
                stash.DieInstantly();                             
            }
        }
    }
}