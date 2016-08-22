using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Better Say", "LaserHydra", "2.0.2", ResourceId = 998)]
    [Description("Customize the say console command output as you want")]
    class BetterSay : RustPlugin
    {
		void Loaded()
		{
            permission.RegisterPermission("bettersay.use", this);
			LoadConfig();
		}
		
		void LoadConfig()
		{
			SetConfig("Settings", "Formatting", "{Title}: {Message}");
			SetConfig("Settings", "Title", "Server");
			SetConfig("Settings", "Title Color", "cyan");
			SetConfig("Settings", "Message Color", "white");
		}
		
		void LoadDefaultConfig()
		{
			Puts("Generating new config file...");
			LoadConfig();
		}
		
		string RemoveFormatting(string old)
		{
			string _new = old;
			
			var matches = new Regex(@"(<color=.+?>)", RegexOptions.IgnoreCase).Matches(_new);
			foreach(Match match in matches)
			{
				if(match.Success) _new = _new.Replace(match.Groups[1].ToString(), "");
			}
			
			_new = _new.Replace("</color>", "");
			
			return _new;
		}
		
		object OnServerCommand(ConsoleSystem.Arg arg)
		{
			if(arg?.cmd?.namefull != null && arg?.cmd?.namefull == "global.say")
			{
				if(arg.connection != null && arg.connection.player != null)
				{
					BasePlayer player = arg.connection.player as BasePlayer;

					if (!permission.UserHasPermission(player.UserIDString, "bettersay.use"))
						return true;
				}
			
                string[] args = new string[0];
				string output = Config["Settings", "Formatting"] as string;

                if (arg.HasArgs()) args = arg.Args;
                string message = ListToString(args.ToList(), 0, " ");
				
				output = output.Replace("{Title}", $"<color={Config["Settings", "Title Color"].ToString()}>{Config["Settings", "Title"].ToString()}</color>").Replace("{Message}", $"<color={Config["Settings", "Message Color"].ToString()}>{message}</color>");
				BroadcastChat(output);
				Puts(RemoveFormatting(output));
				return true;
			}
			else return null;
		}

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            List<string> foundPlayers =
                (from player in BasePlayer.activePlayerList
                 where player.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select player.displayName).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(executer, prefix, "The Player can not be found.");
                    break;

                case 1:
                    return BasePlayer.Find(foundPlayers[0]);

                default:
                    string players = ListToString(foundPlayers, 0, ", ");
                    SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);
    }
}
