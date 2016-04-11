using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("WhoTalks", "PreFiX", "0.1.0")]
    class WhoTalks : RustLegacyPlugin
    {
		
		static Hash<NetUser, int> lastTalked = new Hash<NetUser, int>();
		
		void OnPlayerVoice(NetUser netuser, List<uLink.NetworkPlayer> players)
		{
			int time = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			
			if (time >= lastTalked[netuser]) {
			
				foreach (uLink.NetworkPlayer player in players)
				{
					NetUser zaidejas = NetUser.Find(player);
					if (zaidejas != null) {
						rust.InventoryNotice(zaidejas, "â¤ " + netuser.displayName);
					}
				}
				
				lastTalked[netuser] = time + 3;
				
			}
		}

	}
}