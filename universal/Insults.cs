using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Insults", "Spicy", "1.0.2")]
    [Description("Send insults to yourself or other players.")]

    class Insults : CovalencePlugin
    {
        void Init()
        {
            SetupLanguage();
            permission.RegisterPermission("insults.use", this);
        }

        void SetupLanguage()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["InvalidSyntax"] = "Invalid syntax. Syntax: /insult [me|playername].",
                ["NoPlayersFound"] = "No players were found with that name.",
                ["MultiplePlayersFound"] = "Multiple players were found with that name.",
                ["WebRequestFailed"] = "WebRequest to {0} failed!",
                ["YouInsultedYourself"] = "You insulted yourself with insult:\n{0}",
                ["YouInsultedPlayer"] = "You insulted {0} with insult:\n{1}",
                ["PlayerInsultedYou"] = "{0} insulted you with insult:\n{1}"
            }, this, "en");
        }

        [Command("insult")]
        void cmdInsult(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission("insults.use"))
            {
                player.Reply(lang.GetMessage("NoPermission", this, player.Id));
                return;
            }

            if (args == null || args.Length == 0)
            {
                player.Reply(lang.GetMessage("InvalidSyntax", this, player.Id));
                return;
            }

            IPlayer targetPlayer;

            if (args[0] == "me")
                targetPlayer = player;
            else
            {
                IEnumerable<IPlayer> targetList = players.FindConnectedPlayers(args[0]);

                if (targetList.Count() < 1)
                {
                    player.Reply(lang.GetMessage("NoPlayersFound", this, player.Id));
                    return;
                }

                if (targetList.Count() > 1)
                {
                    player.Reply(lang.GetMessage("MultiplePlayersFound", this, player.Id));
                    return;
                }

                targetPlayer = players.FindConnectedPlayer(args[0]);
            }

            string insultURL = "http://insultgenerator.org";

            webrequest.EnqueueGet(insultURL, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    player.Reply(string.Format(lang.GetMessage("WebRequestFailed", this, player.Id), insultURL));
                    return;
                }

                string insult = GetInsult(response);

                if (targetPlayer == player)
                {
                    player.Reply(string.Format(lang.GetMessage("YouInsultedYourself", this, player.Id), insult));
                    return;
                }

                player.Reply(string.Format(lang.GetMessage("YouInsultedPlayer", this, player.Id), targetPlayer.Name, insult));
                targetPlayer.Reply(string.Format(lang.GetMessage("PlayerInsultedYou", this, targetPlayer.Id), player.Name, insult));
            }, this);
        }

        string GetInsult(string pageResponse)
        {
            int startIndex = pageResponse.IndexOf("<br><br>");
            int endIndex = pageResponse.IndexOf("</div>\n<center>");

            string insult = pageResponse.Substring(startIndex + 8, endIndex - startIndex - 8);
            string[] badStrings = { "&#44;", "&nbsp;" };

            foreach (string badString in badStrings)
                if (insult.Contains(badString))
                    insult = insult.Replace(badString, "");

            return insult;
        }
    }
}