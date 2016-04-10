using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using System.Text;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

namespace Oxide.Plugins 
{
    [Info("PlayersOnline", "Steven", "1.0.0", ResourceId = 8907)]	
    class PlayersOnline : RustPlugin 
	{
		int PlayersToList = 4;
		
		[ChatCommand("who")]
        void cmdWho(BasePlayer player, string command, string[] args)
        {
            cmdPlayers(player, command, args);
        }
		
		[ChatCommand("players")]
        void cmdPlayers(BasePlayer player, string command, string[] args)
		{
			var sb = new StringBuilder();
			int line = 0;
			string TextL = "";
			var PlayersOnline = BasePlayer.activePlayerList as List<BasePlayer>;
            sb.AppendLine("There is currently " + PlayersOnline.Count + " players online.");
			foreach(var b in PlayersOnline)
			{
				if(line++ == PlayersToList-1)
				{
					TextL = TextL + b.displayName;
					sb.AppendLine(TextL);
					TextL = "";
					line = 0;
				} else TextL = TextL + b.displayName + ", ";
			}
			if(line != 0)
			{
				sb.AppendLine(TextL.Remove(TextL.Length - 2));
				TextL = "";
				line = 0;
			}
			SendReply(player, sb.ToString());
		}
    }
}