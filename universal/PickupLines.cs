using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PickupLines", "Spicy", "1.0.1")]
    [Description("Send lovely pickup lines to yourself or other players.")]

    class PickupLines : CovalencePlugin
    {
        void Init()
        {
            SetupLanguage();
            permission.RegisterPermission("pickuplines.use", this);
        }

        void SetupLanguage()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["InvalidSyntax"] = "Invalid syntax. Syntax: /pickupline [me|playername].",
                ["NoPlayersFound"] = "No players were found with that name.",
                ["MultiplePlayersFound"] = "Multiple players were found with that name.",
                ["WebRequestFailed"] = "WebRequest to {0} failed!",
                ["YouPickupLinedYourself"] = "You sent yourself a pickup line:\n{0}",
                ["YouPickupLinedPlayer"] = "You sent {0} a pickup line:\n{1}",
                ["PlayerPickupLinedYou"] = "{0} sent you a pickup line:\n{1}"
            }, this, "en");
        }

        [Command("pickupline")]
        void cmdPickupLine(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission("pickuplines.use"))
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

            string pickupLineURL = "http://www.pickuplinegen.com";

            webrequest.EnqueueGet(pickupLineURL, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    player.Reply(string.Format(lang.GetMessage("WebRequestFailed", this, player.Id), pickupLineURL));
                    return;
                }

                string pickupLine = GetPickupLine(response);

                if (targetPlayer == player)
                {
                    player.Reply(string.Format(lang.GetMessage("YouPickupLinedYourself", this, player.Id), pickupLine));
                    return;
                }

                player.Reply(string.Format(lang.GetMessage("YouPickupLinedPlayer", this, player.Id), targetPlayer.Name, pickupLine));
                targetPlayer.Reply(string.Format(lang.GetMessage("PlayerPickupLinedYou", this, targetPlayer.Id), player.Name, pickupLine));
            }, this);
        }

        string GetPickupLine(string pageResponse)
        {
            int startIndex = pageResponse.IndexOf("<h2>Pickup lines used at your own risk</h2>");
            int endIndex = pageResponse.IndexOf("<div id=\"generate\">");

            string pickupLine = pageResponse.Substring(startIndex + 80, endIndex - startIndex - 93);

            string[] badStrings = { };

            foreach (string badString in badStrings)
                if (pickupLine.Contains(badString))
                    pickupLine = pickupLine.Replace(badString, "");

            return pickupLine;
        }
    }
}
