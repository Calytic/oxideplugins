using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
/* BROUGHT TO YOU BY        
,.   ,.         .  .        `.---     .              . 
`|  / ,-. ,-. . |  |  ,-.    |__  . , |- ,-. ,-. ,-. |-
 | /  ,-| | | | |  |  ,-|   ,|     X  |  |   ,-| |   | 
 `'   `-^ ' ' ' `' `' `-^   `^--- ' ` `' '   `-^ `-' `'
 ~PrincessRadPants and Swuave
*/
namespace Oxide.Plugins
{
	
	
	[Info("VEGod", "PrincessRadPants and Swuave", "1.1.0")]
	public class VEGod : RustLegacyPlugin
	{
		private List<string> Gods = new List<string>();
		
		void Loaded()
		{
			if (!permission.PermissionExists("cangod")) permission.RegisterPermission("cangod", this);
			if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
		} // Loaded End
		
		bool hasAccess(NetUser netuser)
		{
			if (netuser.CanAdmin()) { return true; }
			else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cangod"))
			{
				return true;
			}
			else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all"))
			{
				return true;
			}
			else
			{
				return false;
			}
		} // hasAccess end
		
		[ChatCommand("god")]
		void cmdGod(NetUser netuser, string command, string[] args)
		{
			if (!hasAccess(netuser))
			{
				SendReply(netuser, "You do not have permission to use this command"); return;
			}
			else { ToggleGodMode(netuser); }
			
			
		} // cmdGod end
		
		void ToggleGodMode(NetUser netuser)
		{
			string userid = netuser.playerClient.userID.ToString();
			
			if (!Gods.Contains(userid))
			{
				netuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
				Gods.Add(userid);
				Rust.Notice.Popup(netuser.networkPlayer, "â", "God mode activated");
			}
			else if (Gods.Contains(userid))
			{
				netuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
				Gods.Remove(userid);
				Rust.Notice.Popup(netuser.networkPlayer, "â ", "God mode deactivated");
			}
		} // ToggleGodMode end
		
		void OnPlayerDisconnected(uLink.NetworkPlayer netplayer)
		{
			PlayerClient player = ((NetUser)netplayer.GetLocalData()).playerClient;
			string userid = player.userID.ToString();
			if (Gods.Contains(userid))
			{
				Gods.Remove(userid);
			}
		} // OnPLayerDisconnect end 
		
	} // VEGod end
	
	
}
