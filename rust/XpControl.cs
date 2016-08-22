/*
*
* Chat commands:
* /setlvl <lvl> <nickname / steamid>
* /addlvl <lvl> <nickname / steamid>
* /resetlvl <nickname / steamid>
* /showone <nickname / steamid>
* /showall <page:optional>
* /addxp <points> <nickname / steamid>
*
* Console commands:
* setlvl <lvl> <nickname / steamid>
* addlvl <lvl> <nickname / steamid>
* resetlvl <nickname / steamid>
* showone <nickname / steamid>
* showall <page:optional>
* addxp <points> <nickname / steamid>
*
* API:
* object XPCSetLVL(ulong userID, int level) // Returns true if successful, otherwise returns null
* object XPCAddLVL(ulong userID, int level) // Returns true if successful, otherwise returns null
* void   XPCResetLVL(ulong userID)
* void   XPCAddXP(ulong userID, int xp)
*
*/

using Oxide.Core.Plugins;
using Rust.Xp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XP Control", "miRror", "5.0.0", ResourceId = 2013)]
	
    class XpControl : RustPlugin
    {	
		private int maxLevel = 99; // fix for current game version
		
        private void Init()
		{	
			lang.RegisterMessages(new Dictionary<string, string> {
				["formataddlvl"] 		= "/addlvl <lvl> <nickname / steamid>\n con.addlvl <lvl> <nickname / steamid>",
				["formatsetlvl"] 		= "/setlvl <lvl> <nickname / steamid>\n con.setlvl <lvl> <nickname / steamid>",
				["formatresetlvl"] 		= "/resetlvl <nickname / steamid>\n con.resetlvl <nickname / steamid>",
				["formatshowone"] 		= "/showone <nickname / steamid>\n con.showone <nickname / steamid>",
				["formataddxp"] 		= "/addxp <points> <nickname/ steamid>\n con.addxp <points> <nickname / steamid>",				
				["StringFormat"] 		= "String format:\n <color=orange>{0}</color>",
				["NoPlayers"] 			= "No players found!",
				["LevelValidNum"] 		= "Level needs to be positive number!",
				["PageValidNum"]		= "Page needs to be positive number!",
				["LevelLimit"] 			= "<color=orange>Error!</color> Player can reach maximum level with that number. Current players level: <color=orange>{0}</color>",
				["AddLevelSuccess"] 	= "New level of player <color=orange>{0}</color>: <color=orange>{1}</color>",
				["PointsValidNum"]  	= "The number of points must be positive!",
				["AddXpSuccess"] 		= "Player <color=orange>{0}</color> got <color=orange>{1} XP</color>",
				["ResetLevelSuccess"] 	= "Player <color=orange>{0}</color> successfully were reseted!",
				["FormatValidLevel"] 	= "This level can't be set to player. Valid values: <color=orange>1 < x < {0}</color>",
				["SetLevelSuccess"] 	= "Player <color=orange>{0}</color> got lvl: <color=orange>{1}</color>",
				["ShowOneSuccess"] 		= "Player <color=orange>{0}</color> has: <color=#24C63D>{1}</color> LVL and <color=#3E82D0>{2}</color> XP",
				["ListPlayers"] 		= "List of online players",
				["ChangePage"] 			= "Change page: showall <page>",
				["Page"] 				= "Page",
				["CheckConsole"] 		= "<color=orange>Check your console!</color>",
			}, this);
		}
		
        private void Loaded()
        {
			List<string> commands = new List<string>() {"setlvl", "addlvl", "resetlvl", "showone", "addxp", "showall"};
			
			foreach(string command in commands)
			{
				permission.RegisterPermission("xpcontrol." + command, this);
				cmd.AddChatCommand(command, this, "cmdChatHandler");
				cmd.AddConsoleCommand("global." + command, this, "cmdConsoleHandler");
			}
        }

		//Other methods
		
		private void Action(BasePlayer player, ConsoleSystem.Arg arg, string cmd, string[] args)
		{	
			if(arg != null)
				player = arg.Player();
			
			Agent 		agent;
			BasePlayer 	target;
			bool 		isServer = player == null;
			int 		level, xp;
			string 		str, userID = player?.UserIDString;
			
			switch(cmd)
			{
				case "setlvl":
					target = GetPlayer(player, arg, cmd, args, 2, isServer, userID);
					
					if(target == null)
						return;
					
					if(!Int32.TryParse(args[0], out level) || level < 1 || level > maxLevel)
					{
						Reply(player, arg, string.Format(Lang("FormatValidLevel", userID), maxLevel), isServer);
						
						return;							
					}
					
					agent = BasePlayer.FindXpAgent(target.userID);
					
					agent.Reset();
					agent.Add(Definitions.Cheat, Rust.Xp.Config.LevelToXp(level));	

					Reply(player, arg, string.Format(Lang("SetLevelSuccess", userID), target.displayName, level), isServer);
					
					break;
				case "addlvl":
					target = GetPlayer(player, arg, cmd, args, 2, isServer, userID);
					
					if(target == null)
						return;
					
					if(!Int32.TryParse(args[0], out level) || level < 1)
					{
						Reply(player, arg, string.Format(Lang("LevelValidNum", userID)), isServer);
						
						return;							
					}
					
					agent = BasePlayer.FindXpAgent(target.userID);
					
					int newLevel = (int)agent.CurrentLevel + level;
					
					if(newLevel > maxLevel)
					{
						Reply(player, arg, string.Format(Lang("LevelLimit", userID), (int)agent.CurrentLevel), isServer);
						
						return;
					}
					
					agent.Add(Definitions.Cheat, Rust.Xp.Config.LevelToXp(newLevel) - agent.EarnedXp);

					Reply(player, arg, string.Format(Lang("AddLevelSuccess", userID), target.displayName, newLevel), isServer);
					
					break;
				case "resetlvl":
					target = GetPlayer(player, arg, cmd, args, 1, isServer, userID);
					
					if(target == null)
						return;

					BasePlayer.FindXpAgent(target.userID).Reset();

					Reply(player, arg, string.Format(Lang("ResetLevelSuccess", userID), target.displayName), isServer);
					
					break;
				case "showone":
					target = GetPlayer(player, arg, cmd, args, 1, isServer, userID);
					
					if(target == null)
						return;

					agent = BasePlayer.FindXpAgent(target.userID);
					
					Reply(player, arg, string.Format(Lang("ShowOneSuccess", userID), target.displayName, (int)agent.CurrentLevel, (int)agent.UnspentXp), isServer);
					
					break;
				case "showall":
					if(!isServer && !player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, "xpcontrol." + cmd))
						return;
					
					int cntPlayers 	  = BasePlayer.activePlayerList.Count;
					int currentPage   = 1;
					int playersOnPage = 50;
					int allPages 	  = (int)Math.Ceiling(1.0f * cntPlayers / playersOnPage);
	
					if(cntPlayers == 0)
					{
						Reply(player, arg, string.Format(Lang("NoPlayers", userID)), isServer);
						
						return;						
					}
					
					if(args.Length > 0 && !Int32.TryParse(args[0], out currentPage) || currentPage < 1 || currentPage > allPages)
					{
						Reply(player, arg, string.Format(Lang("PageValidNum", userID)), isServer);
						
						return;							
					}

					if(!isServer)
						player.SendConsoleCommand("clear"); // fix for bad parsing tags

					str = "<color=#C65624>" + new String('-', 70) + "\n" + Lang("ListPlayers", userID).PadRight(26) + ("<color=#24C63D>LVL</color>").PadRight(50) + ("<color=#3E82D0>XP</color>").PadRight(28) + "| " + Lang("Page", userID) + $" {currentPage}/{allPages} |\n" + new String('-', 70) + "</color>\n";
					
					List<BasePlayer> players = BasePlayer.activePlayerList.GetRange((currentPage - 1) * playersOnPage, currentPage == 1 ? Math.Min(cntPlayers, playersOnPage) : (cntPlayers - ((currentPage - 1) * playersOnPage)));

					foreach(BasePlayer t in players)
						str += $"<color=orange>{t.displayName, -25}</color> <color=#24C63D>{t.xp.CurrentLevel, -25:#;minus #}</color> <color=#3E82D0>{t.xp.UnspentXp:#;minus #}</color>\n";	
					
					str += "<color=#C65624>" + new String('-', 70) + "\n" + Lang("ChangePage", userID) + "\n" + new String('-', 70) + "</color>\n";
					
					if(isServer) 
						Reply(player, arg, "\n" + str, isServer);
					else {
						if(arg == null)
							SendReply(player, Lang("CheckConsole", userID));
						
						PrintToConsole(player, str);
					}
					
					break;
				case "addxp":
					target = GetPlayer(player, arg, cmd, args, 2, isServer, userID);
					
					if(target == null)
						return;
					
					if(!Int32.TryParse(args[0], out xp) || xp < 1)
					{
						Reply(player, arg, string.Format(Lang("PointsValidNum", userID)), isServer);
						
						return;							
					}

					BasePlayer.FindXpAgent(target.userID).Add(Definitions.Cheat, xp);
		
					Reply(player, arg, string.Format(Lang("AddXpSuccess", userID), target.displayName, xp), isServer);
					
					break;
			}
		}

		private BasePlayer GetPlayer(BasePlayer player, ConsoleSystem.Arg arg, string cmd, string[] args, int needArgs, bool isServer, string userID)
		{
			if(!isServer && !player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, "xpcontrol." + cmd))
				return null;
			
			if(args.Length < needArgs)
			{
				Reply(player, arg, string.Format(Lang("StringFormat", userID), Lang("format" + cmd, userID)), isServer);
				
				return null;
			}
			
			string strNameOrIDOrIP = string.Join(" ", needArgs > 1 ? args.Skip(1).ToArray() : args);
			
			BasePlayer target = rust.FindPlayer(strNameOrIDOrIP);		
			
			if(target == null)
			{
				Reply(player, arg, Lang("NoPlayers", userID), isServer);
				
				return null;				
			}
			
			return target;	
		}
		
		private string Lang(string key, string userID) => lang.GetMessage(key, this, userID);
		
		private void Reply(BasePlayer player, ConsoleSystem.Arg arg, string message, bool isServer)
		{ 	
			if(isServer)
				message = Regex.Replace(message, "<.*?>", string.Empty);
			
			if(arg == null)	SendReply(player, message);
			else 			SendReply(arg, 	  message);	
		}
		
		//Chat commands
		
		private void cmdChatHandler(BasePlayer player, string cmd, string[] args) => Action(player, null, cmd, args);	
		
		//Console commands
		
		private void cmdConsoleHandler(ConsoleSystem.Arg arg) => Action(null, arg, arg.cmd.name, arg.HasArgs() ? arg.Args : new string[0]);		
		
		//API

        [HookMethod("XPCSetLVL")]
        public object XPCSetLVL(ulong userID, int level)
        {
			if(level < 1 || level > maxLevel)
				return null;
			
			Agent agent = BasePlayer.FindXpAgent(userID);
			
			agent.Reset();
			agent.Add(Definitions.Cheat, Rust.Xp.Config.LevelToXp(level));	
			
			return true;
		}

        [HookMethod("XPCAddLVL")]
        public object XPCAddLVL(ulong userID, int level)
        {
			Agent agent = BasePlayer.FindXpAgent(userID);

			int newLevel = (int)agent.CurrentLevel + level;
			
			if(newLevel > maxLevel)
				return null;
			
			agent.Add(Definitions.Cheat, Rust.Xp.Config.LevelToXp(newLevel) - agent.EarnedXp);

			return true;
		}

        [HookMethod("XPCResetLVL")]
        public void XPCResetLVL(ulong userID) => BasePlayer.FindXpAgent(userID).Reset();
		
        [HookMethod("XPCAddXP")]
        public void XPCAddXP(ulong userID, int xp) => BasePlayer.FindXpAgent(userID).Add(Definitions.Cheat, xp);
	}
}