using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using Oxide.Core;
using CodeHatch.Engine.Networking;
using CodeHatch.Common;
using CodeHatch.Networking.Events.Players;

namespace Oxide.Plugins
{ 
    [Info("Guild-Tag to Chat", "PaiN", "1.0.0", ResourceId = 0)]
    [Description("Adds the guild tag to chat")]
    class GuildTagToChat : ReignOfKingsPlugin
	{ 
	 
		public void Loaded()  
		{  
			LoadDefaultConfig();	
		}  
		
		protected override void LoadDefaultConfig()
			{
				Puts("Creating new configuration file!");
				Config.Clear();
				
				Config["GuildTagColor"] = "[FF0000]";
				SaveConfig();	
			}		 
		
		void OnPlayerChat(PlayerEvent e)
		{
			Player player = e.Player;
			string colortag = Convert.ToString(Config["GuildTagColor"]);
            player.DisplayNameFormat = "(" + colortag + player.GetGuild().Name + "[FFFFFF]" + ") %name%";
		}
	}
}