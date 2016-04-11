//Requested by CCTV and RHAKOON on the Oxide Rust requests forum
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Craft Spam Blocker", "LeoCurtss", 0.2)]
    [Description("Prevents items from being crafted if the player's inventory is full.")]

    class CraftSpamBlocker : RustPlugin
    {
		void Loaded()
        {
            //Lang API dictionary
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CPB_CantCraft"] = "Item not crafted!  Your inventory is full."
            }, this);
        }
		
		void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
			BasePlayer player = task.owner;
			
			if (player.inventory.containerMain.itemList.Count > 23 && player.inventory.containerBelt.itemList.Count > 5)
			{
				task.cancelled = true;
				SendReply(player, GetMessage("CPB_CantCraft", player.UserIDString));
                Puts(player.displayName + " tried to craft an item, but their inventory was full!");
			}

			
        }
		
		private string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }
	
    }
}