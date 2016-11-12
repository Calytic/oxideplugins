using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Inventory Cleaner", "PaiN", "1.1.3", ResourceId = 0)]
    [Description("This plugin allows players with permission to clean all/their/target's inventory.")]
    class InventoryCleaner : RustPlugin
    {
		void Loaded() => permission.RegisterPermission(this.Name.ToLower()+".use", this);

      
		[ChatCommand("invcleanall")] 
		void cmdInvCleanAll(BasePlayer player, string cmd, string[] args)
		{
			
			string steamId = Convert.ToString(player.userID);
			if (permission.UserHasPermission(steamId, "inventorycleaner.use"))
			{
				if (args.Length == 1)
				{
					SendReply(player, "Commands: \n/invcleanme => Cleans your inventory.\n/invclean \"player\" => Cleans the target's inventory\n/invcleanall => Cleans everyones invetory.");
					return;
				}
				
					var players = BasePlayer.activePlayerList as List<BasePlayer>;
					foreach (BasePlayer current in BasePlayer.activePlayerList)
					{
						current.inventory.Strip();
						Puts(player.displayName + " has cleaned all the inventories!");
						PrintToChat("<color=orange>[Inventory Cleaner]</color> " + player.displayName + " has cleaned all the inventories (" + players.Count + ") !");
						SendReply(player, "<color=orange>[Inventory Cleaner]</color> " + "You have cleaned " + players.Count + " inventories!");
					
					}
					
				}
				else
				{
					SendReply(player, "You do not have permission to use this command!");
					return;		
				}
			}
		
		
		[ChatCommand("invclean")]
		void cmdInvClean(BasePlayer player, string cmd, string[] args)
		{
			string steamId = Convert.ToString(player.userID);
			if (permission.UserHasPermission(steamId, "inventorycleaner.use"))
			{
				if (args.Length == 0)
				{
					SendReply(player, "Commands: \n/invcleanme => Cleans your inventory.\n/invclean \"player\" => Cleans the target's inventory\n/invcleanall => Cleans everyones invetory.");
					return;
				}
				if (args.Length == 1)
				{
				var target = BasePlayer.Find(args[0]);
				target.inventory.Strip();
				SendReply(player, "<color=orange>[Inventory Cleaner]</color> " + "You have successfully cleaned <color=cyan>" + target.displayName + "</color>'s inventory!");
				}
			}
			else
			{
			SendReply(player, "You do not have permission to use this command!");
			return;
			}
		
		}
		
		[ChatCommand("invcleanme")]
		void cmdInvCleanMe(BasePlayer player, string cmd, string[] args)
		{
			if (args.Length == 0)
			{
				player.inventory.Strip();
				SendReply(player, "<color=orange>[Inventory Cleaner]</color> " + "You have cleaned your inventory!");
			}

		}
		
	}
}
