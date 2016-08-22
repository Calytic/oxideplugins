using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Admin Inventory Cleaner", "TheDoc - Uprising Servers", "1.2.0", ResourceId = 973)]
    class InventoryCleaner : RustPlugin
    {
	void SendChatMessage(BasePlayer player, string message, string args = null) => PrintToChat(player, $"<color=lime>InvCleaner</color> : {message}", args);

	void Init() => PluginSetup();

        [ChatCommand("cleaninv")]
        void cmdChatCleanInv(BasePlayer player, string command, string[] args)
        {
            if (IsAllowed(player, "CanUseInvClean") && player != null) {
	        player.inventory.Strip();
		SendChatMessage(player, "Your Inventory is now clean!");
	    }	
        }

        void PluginSetup()
        {
            LoadPermissions();
        }
		
        void LoadPermissions()
        {
            if (!permission.PermissionExists("CanUseInvClean")) permission.RegisterPermission("CanUseInvClean", this);
        }

        bool IsAllowed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            SendChatMessage(player, "You are <color=red>Not Allowed</color> To Use this command!");
            return false;
        }
    }
}
