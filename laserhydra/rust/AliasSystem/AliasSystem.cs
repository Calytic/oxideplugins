using System.Collections.Generic;
using Oxide.Game.Rust.Libraries;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Alias System", "LaserHydra", "2.0.0", ResourceId = 1307)]
    [Description("Setup alias for chat and console commands")]
    class AliasSystem : RustPlugin
    {
		class Alias
		{
			public string original;
			public string alias;
			public string originaltype;
			public string aliastype;
			public string permission = "commandalias.use";
			
			public Alias(string aliasname, string Original)
			{
				original = Original;
				alias = aliasname;
				
				if(aliasname.StartsWith("/")) aliastype = "chat";
				else if(!aliasname.StartsWith("/")) aliastype = "console";
				
				if(Original.StartsWith("/")) originaltype = "chat";
				else if(!Original.StartsWith("/")) originaltype = "console";
			}
			
			public Alias()
			{
			}
		}
		
		class Data
		{
			public List<Alias> alias = new List<Alias>();
			
			public Data()
			{
			}
		}
		
		Data data;
		
		Data LoadData() 
		{
			return Interface.GetMod().DataFileSystem.ReadObject<Data>("AliasSystem_Data");
		}
		
		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("AliasSystem_Data", data);
		}
		
		void Loaded()
		{	
			data = LoadData();
			LoadConfig();
			
			if(!permission.PermissionExists("commandalias.admin")) permission.RegisterPermission("commandalias.admin", this);
			if(!permission.PermissionExists("commandalias.use")) permission.RegisterPermission("commandalias.use", this);

            foreach(Alias current in data.alias)
            {
				if(current.aliastype == "chat")
				{
					cmd.AddChatCommand(current.alias.Substring(1, current.alias.Length - 1), this, "ChatAlias");
				}
				else if(current.aliastype == "console")
				{
					cmd.AddConsoleCommand("global." + current.alias, this, "ConsoleAlias");
				}
            }
		}
		
		void LoadConfig()
		{
		}
		
		void LoadDefaultConfig()
		{
			Puts("Generating new config file...");
			LoadConfig();
		}
		
		void ChatAlias(BasePlayer player, string command, string[] args)
		{
			if(GetAliasByName("/" + command) == null) return;
			
			Alias alias = GetAliasByName("/" + command);
			
			if(!permission.UserHasPermission(player.userID.ToString(), alias.permission)) 
			{
				SendChatMessage(player, "Command Alias", "You have no permission to use this command.");
				return;
			}
			
			if(alias.originaltype == "chat")
				RunChatCommand(player, command, ListToString(args.ToList(), 0, " "));
			else if(alias.originaltype == "console")
				RunConsoleCommand(player, command, ListToString(args.ToList(), 0, " "));
		}
		
		void ConsoleAlias(ConsoleSystem.Arg arg)
		{
			BasePlayer player = (BasePlayer)arg?.connection?.player ?? null;
			if(player == null) return;
			
			string command = arg?.cmd?.namefull?.Replace("global.", "") ?? "";
			
			if(GetAliasByName(command) == null) return;
			
			Alias alias = GetAliasByName(command);
			
			if(!permission.UserHasPermission(player.userID.ToString(), alias.permission)) 
			{
				SendChatMessage(player, "Command Alias", "You have no permission to use this command.");
				return;
			}
			
			if(alias.originaltype == "chat")
				RunChatCommand(player, command, ArgToString(arg, 0, " "));
			else if(alias.originaltype == "console")
				RunConsoleCommand(player, command, ArgToString(arg, 0, " "));
		}
		
		[ChatCommand("alias")]
		void cmdAlias(BasePlayer player, string command, string[] args)
		{
			if(!permission.UserHasPermission(player.userID.ToString(), "commandalias.admin")) 
			{
				SendChatMessage(player, "Command Alias", "You have no permission to use this command.");
				return;
			}
			
			if(args.Length < 1)
			{
				SendChatMessage(player, "Command Alias", "\n/alias add <alias> <command>\n/alias remove <alias>");
				return;
			}
			
			switch(args[0])
			{
				case "add":
					
					if(args.Length < 3)
					{
						SendChatMessage(player, "Command Alias", "Syntax: /alias add <alias> <original>");
						return;
					}
					
					data.alias.Add(new Alias(args[1], args[2]));
					SaveData();
					
					if(args[1].StartsWith("/")) cmd.AddChatCommand(args[1].Substring(1, args[1].Length - 1), this, "ChatAlias");
					else cmd.AddConsoleCommand("global." + args[1], this, "ConsoleAlias");
					
					SendChatMessage(player, "Command Alias", $"Alias {args[1]} successfuly set for command {args[2]}");
					
					break;
					
				case "remove":
				
					if(args.Length < 2)
					{
						SendChatMessage(player, "Command Alias", "Syntax: /alias remove <alias>");
						return;
					}
					
					Alias alias = GetAliasByName(args[1]);
					
					if(data.alias.Contains(alias))
					{
						data.alias.Remove(alias);
						SaveData();
					
						SendChatMessage(player, "Command Alias", $"Alias {args[1]} successfuly removed.");	
					}
					else SendChatMessage(player, "Command Alias", $"Alias {args[1]} does not exist.");
					
					break;
					
				default:
					break;
			}
		}
		
		Alias GetAliasByName(string aliasname)
		{
			foreach(Alias current in data.alias)
            {
				if(current.alias == aliasname) return current;
            }
			
			return null;
		}
		
		void RunChatCommand(BasePlayer player, string command, string args)
		{
			Alias alias = GetAliasByName(command);
			if(alias == null) alias = GetAliasByName("/" + command);
			
			player.SendConsoleCommand("chat.say", "/" + $"{alias.original.Substring(1, alias.original.Length - 1).Replace("'", "\"")} {args.Replace("'", "\"")}");
		}
		
		void RunConsoleCommand(BasePlayer player, string command, string args)
		{
			Alias alias = GetAliasByName(command);
			if(alias == null) alias = GetAliasByName("/" + command);
			
			player.SendConsoleCommand(alias.original.Replace("'", "\""), args.Replace("'", "\""));
		}
		
        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

		BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				if(player.displayName.ToLower().Contains(searchedLower)) foundPlayers.Add(player.displayName);
			}
			
			switch(foundPlayers.Count)
			{
				case 0:
					SendChatMessage(executer, prefix, "The Player can not be found.");
					break;
					
				case 1:
					targetPlayer = BasePlayer.Find(foundPlayers[0]);
					break;
				
				default:
					string players = ListToString(foundPlayers, 0, ", ");
					SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
					break;
			}
			
            return targetPlayer;
        }

        //---------------------------->   Converting   <----------------------------//

        string ListToString(List<string> list, int first, string seperator)
		{
			return String.Join(seperator, list.Skip(first).ToArray());
		}
		
		string ArgToString(ConsoleSystem.Arg arg, int first, string seperator)
		{
			if(arg.Args == null || arg.Args.Count() < 1) return string.Empty;
			return ListToString(arg.Args.ToList(), first, seperator);
		}

        //------------------------------>   Config   <------------------------------//

        void SetConfig(string Arg1, object Arg2, object Arg3 = null, object Arg4 = null)
		{
			if(Arg4 == null)
			{
				Config[Arg1, Arg2.ToString()] = Config[Arg1, Arg2.ToString()] ?? Arg3;
			}
			else if(Arg3 == null)
			{
				Config[Arg1] = Config[Arg1] ?? Arg2;
			}
			else
			{
				Config[Arg1, Arg2.ToString(), Arg3.ToString()] = Config[Arg1, Arg2.ToString(), Arg3.ToString()] ?? Arg4;
			}
		}

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        //---------------------------------------------------------------------------//
        #endregion
    }
}
