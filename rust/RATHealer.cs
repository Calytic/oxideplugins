
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("RATHealer", "@lonestarcanuck", 0.1, ResourceId = 1963)]
    [Description("The Rust Admin Tool (RAT) provides ingame and console heal features")]
    public class RATHealer : RustPlugin
    {
		public BasePlayer cachedPlayer;
		
        // Code goes here...
		void Init()
		{	
			Puts("Init of RAT Healer works!");
		}
		void Unload()
		{
			Puts("Unload of RAT Healer works!");
		}
		
		private object GetPlayer(string tofind)
        {
            List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
			
            object targetplayer = null;
			
            foreach (BasePlayer player in onlineplayers.ToArray())
            {

                if (player.displayName.ToString() == tofind)
                    return player;
            }
			
			return null;
        }
		
		[ConsoleCommand("RATHealer.heal")]
        private void HealCommand(ConsoleSystem.Arg arg)
        {
            
			Puts(string.Format("Attempting to heal Player: {0}...",arg.Args[0]));
						
			if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0))
            {
                Puts("RATHealer.heal \"player\" \"heal amount\"");
				Puts("Example: RATHealer.heal Quicken 1000");
                return;
            }
			
			object target = false;
			
            target = GetPlayer(arg.Args[0]);
			
			if (target == null) 
			{
               Puts("Couldn't find player: " + arg.Args [0].ToString ());
               return;
			}
			
        	//Heal, feed, cure the player...
			var targetPlayer = target as BasePlayer;
			
			targetPlayer.metabolism.hydration.value = 1000;
            targetPlayer.metabolism.calories.value = 1000;
            targetPlayer.InitializeHealth(100, 100);
			targetPlayer.metabolism.poison.value = 0;
			// targetPlayer.metabolism.radiation.value = 0;
			targetPlayer.metabolism.oxygen.value = 1;
			targetPlayer.metabolism.bleeding.value = 0;
			targetPlayer.metabolism.wetness.value = 0;
			// targetPlayer.metabolism.dirtyness.value = 0;

			Puts(targetPlayer.displayName + " was healed, hydrated, fed, etc.");

			Puts("Done.");
        }

		[ConsoleCommand("RATHealer.all")]
        private void HealAllCommand(ConsoleSystem.Arg arg)
        {
        	Puts("Attempting to heal all Players...");
			
			List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
			
            foreach (BasePlayer player in onlineplayers.ToArray())
            {
				Puts(string.Format("Healing Player: {0}...", player.displayName.ToString()));
				player.metabolism.hydration.value = 1000;
				player.metabolism.calories.value = 1000;
				player.InitializeHealth(100, 100);
				player.metabolism.bleeding.value = 0;
            }
			
			Puts("Done.");
        }
		
		[ConsoleCommand("RATHealer.list")]
        private void ListCommand(ConsoleSystem.Arg arg)
        {
        	Puts("Listing All Players...");
			
			List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
			
            foreach (BasePlayer player in onlineplayers.ToArray())
            {
				Puts(string.Format("Player: {0}", player.displayName.ToString()));
            }
			
			Puts("Done.");
        }
    }
}