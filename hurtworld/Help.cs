using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Help", "Waizujin", 1.2, ResourceId = 0)]
    [Description("Provides chat commands for a user to get help.")]

    class Help : HurtworldPlugin
    {
        protected override void LoadDefaultConfig()
        {
			PrintWarning("Creating a new configuration file.");
            Config["HelpSettings", "HelpTitle"] = "Help";
            Config["HelpSettings", "HelpTitleColor"] = "#ff8000";
            Config["HelpCommands", "rules"] = "Don't cheat, hack, or exploit bugs. Obey admins, and have fun.";
            SaveConfig();
		}

        [ChatCommand("help")]
        void HelpCommand(PlayerSession session, string command, string[] args)
        {
            Dictionary<string, object> commands = Config["HelpCommands"] as Dictionary<string, object>;
            string helpColor = Config["HelpSettings", "HelpTitleColor"].ToString();
            string helpTitle = Config["HelpSettings", "HelpTitle"].ToString();
            string prefix = "<color=" + helpColor + ">" + helpTitle + "</color>";

            if(args.Length == 0)
            {
                string commandList = "";

                foreach(KeyValuePair<string, object> commandSingle in commands)
                {
                    commandList = commandList + commandSingle.Key + ", ";
                }

                commandList = commandList.Substring(0, commandList.Length - 2);

                hurt.SendChatMessage(session, prefix, "All available commands: " + commandList);

                return;
            }

            object answer;
            if(commands.TryGetValue(args[0], out answer))
            {
                string answerString = answer.ToString();

                hurt.SendChatMessage(session, prefix, answerString);
            } else {
                hurt.SendChatMessage(session, prefix, "That command does not exist.");
            }
        }

        [ChatCommand("helpadd")]
        void HelpAddCommand(PlayerSession session, string command, string[] args)
        {
            if(!session.IsAdmin)
            {
                return;
            }

            string helpColor = Config["HelpSettings", "HelpTitleColor"].ToString();
            string helpTitle = Config["HelpSettings", "HelpTitle"].ToString();
            string prefix = "<color=" + helpColor + ">" + helpTitle + "</color>";

            if(args.Length == 0 || args.Length == 1)
            {
                hurt.SendChatMessage(session, prefix, "This command requires 2 arguments. Example: /helpadd helpname \"Help Answer\" ");
                return;
            }

            Config["HelpCommands", args[0]] = args[1];
            SaveConfig();

            hurt.SendChatMessage(session, prefix, "Help command added successfully. Type /help to see your new command on the list.");
        }

        [ChatCommand("helpremove")]
        void HelpRemoveCommand(PlayerSession session, string command, string[] args)
        {
            if(!session.IsAdmin)
            {
                return;
            }

            string helpColor = Config["HelpSettings", "HelpTitleColor"].ToString();
            string helpTitle = Config["HelpSettings", "HelpTitle"].ToString();
            string prefix = "<color=" + helpColor + ">" + helpTitle + "</color>";

            if(args.Length == 0)
            {
                hurt.SendChatMessage(session, prefix, "This command requires at least 1 argument, the name of the help command you wish to delete.");
                return;
            }

            Dictionary<string, object> commands = Config["HelpCommands"] as Dictionary<string, object>;
            if(commands.ContainsKey(args[0]))
            {
                commands.Remove(args[0]);
                SaveConfig();
            } else {
                hurt.SendChatMessage(session, prefix, "That help command doesn't exist.");
                return;
            }

            hurt.SendChatMessage(session, prefix, "Help command deleted successfully.");
        }
    }
}
