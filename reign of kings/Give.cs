using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using Oxide.Core;
using Oxide.Core.Plugins;
using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Inventory.Blueprints;
using CodeHatch.ItemContainer;
using System.Linq;
using System.Text;
using UnityEngine;
 
namespace Oxide.Plugins
{
    [Info("Give", "PaiN", 0.1, ResourceId = 0)] 
    [Description("A simple give plugin.")]
    class Give : ReignOfKingsPlugin 
    {
		void Loaded()
		{
			if (!permission.PermissionExists("cangive")) permission.RegisterPermission("cangive", this);
		}
		[ChatCommand("pgive")]
		void cmdPGive(Player player, string cmd, string[] args)
		{
			if(!permission.UserHasPermission(player.Id.ToString(), "cangive"))
			{
				SendReply(player, "You do not have permission to use this command!");
				return;
			}
			switch(args[0])
			{
				default:
				if(args.Length == 2)
				{
					var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(args[0].ToString(), true, true);
					var inventory = player.GetInventory().Contents;
					if(blueprintForName == null)
					{
						SendReply(player, "Could not find blueprint for {0}.", args[0].ToString());
						return;
					}
					if(inventory.FreeSlotCount == 0)
					{
						SendReply(player, "You dont have enough space in your inventory!");
						return;
					}
					int amount = Convert.ToInt32(args[1]);
					var invGameItemStack = new InvGameItemStack(blueprintForName, amount, null);
					ItemCollection.AutoMergeAdd(inventory, invGameItemStack);
					SendReply(player, "You have received {0} {1}.", blueprintForName.Name, amount.ToString());
				}
				else
				{
					SendReply(player, "/pgive <ItemName> <ItemStack>");
					return;
				}
				break;
				
				case "to":
				if(args.Length == 4)
				{
					Player target = Server.GetPlayerByName(args[1]);
					var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(args[2].ToString(), true, true);
					var inventory = target.GetInventory().Contents;
					if(blueprintForName == null)
					{
						SendReply(player, "Could not find blueprint for {0}.", args[2].ToString());
						return;
					}
					if(inventory.FreeSlotCount == 0)
					{
						SendReply(player, "You dont have enough space in your inventory!");
						return;
					}
					if(target == null)
					{
						SendReply(player, "Player not found!");
						return;
					}
					int amount = Convert.ToInt32(args[3]);
					var invGameItemStack = new InvGameItemStack(blueprintForName, amount, null);
					ItemCollection.AutoMergeAdd(inventory, invGameItemStack);
					SendReply(target, "You have received {1} {0}.", blueprintForName.Name, amount.ToString());
					SendReply(player, "You gave {0} {2} {1}.", target.DisplayName, blueprintForName.Name, amount.ToString());
				}
				else
				{
					SendReply(player, "/pgive to <TargetPlayer> <ItemName> <ItemStack> ");
					return;
				}
				break;
				case "help":
				SendReply(player, "/pgive to <PlayerName> <ItemName> <ItemStack> => Gives the item and the amount to the target.");
				SendReply(player, "/pgive <ItemName> <ItemStack> => Gives the item and the amount to the player who used the command.");
				break;
			}
		}
	}
}