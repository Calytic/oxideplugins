using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ReportBot", "Spicy", "1.0.5")]
    [Description("Allows server reports to be sent over Steam to server owners.")]

    class ReportBot : CovalencePlugin
    {
        string reportPermission, ownerSteamID, requestPageURL;

        void Init()
        {
            SetupConfig();
            SetupLang();

            permission.RegisterPermission(reportPermission, this);

            ulong _ownerSteamID;

            if (!(ulong.TryParse(ownerSteamID, out _ownerSteamID)) || ownerSteamID.Length != 17)
            {
                Puts("Configuration error. The OwnerSteamID provided is invalid.");
                return;
            }

            Puts($"ReportBot initialised. Forwarding reports to {ownerSteamID}.");
        }

        protected override void LoadDefaultConfig()
        {
            Config["Settings"] = new Dictionary<string, string>
            {
                ["ReportPermission"] = "reportbot.use",
                ["OwnerSteamID"] = "76561198103592543",
                ["RequestPageURL"] = "http://steam.spicee.xyz/addreport.php"
            };
        }

        void SetupConfig()
        {
            reportPermission = Config.Get<string>("Settings", "ReportPermission");
            ownerSteamID = Config.Get<string>("Settings", "OwnerSteamID");
            requestPageURL = Config.Get<string>("Settings", "RequestPageURL");
        }

        void SetupLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["InvalidSyntax"] = "Invalid syntax. Syntax: /report [name] [message]",
                ["NoPlayersFound"] = "No players were found with that name.",
                ["MultiplePlayersFound"] = "Multiple players were found with that name.",
                ["WebRequestFailed"] = "Report failed to send. (WebRequest failed).",
                ["WebRequestSuccess"] = "Report sent successfully."
            }, this);
        }

        [Command("report", "reportbot.report")]
        void cmdReport(IPlayer player, string command, string[] args)
        {
            if (!(permission.UserHasPermission(player.Id, reportPermission)))
            {
                player.Reply(lang.GetMessage("NoPermission", this));
                return;
            }

            if (args.Length == 0 || args.Length < 2 || args == null)
            {
                player.Reply(lang.GetMessage("InvalidSyntax", this));
                return;
            }

            IEnumerable<IPlayer> targetList = players.FindPlayers(args[0]);

            if (targetList.Count() < 1)
            {
                player.Reply(lang.GetMessage("NoPlayersFound", this));
                return;
            }

            if (targetList.Count() > 1)
            {
                player.Reply(lang.GetMessage("MultiplePlayersFound", this));
                return;
            }

            IPlayer target = players.FindPlayer(args[0]);

            float playerX, playerY, playerZ;
            player.Position(out playerX, out playerY, out playerZ);

            float targetX, targetY, targetZ;
            target.Position(out targetX, out targetY, out targetZ);

            string playerPosition = $"({Math.Floor(playerX)}, {Math.Floor(playerY)}, {Math.Floor(playerZ)})";
            string targetPosition = $"({Math.Floor(targetX)}, {Math.Floor(targetY)}, {Math.Floor(targetZ)})";

            string reportMessage = "";

            for (int i = 1; i < args.Length; i++)
                reportMessage = reportMessage + " " + args[i];

            if (reportMessage.Contains("|"))
                reportMessage = reportMessage.Replace('|', '/');

            reportMessage = reportMessage.Trim();

            string reportRequestURL = string.Format("{0}?ownersteamid={1}&reportersteamid={2}&reporterposition={3}&reporteesteamid={4}&reporteeposition={5}&reportmessage={6}",
                requestPageURL, ownerSteamID, player.Id, playerPosition, target.Id, targetPosition, reportMessage);

            webrequest.EnqueueGet(reportRequestURL, (code, response) =>
            {
                if (response == null || code != 200)
                {
                    player.Reply(lang.GetMessage("WebRequestFailed", this));
                    Puts(lang.GetMessage("WebRequestFailed", this));
                    return;
                }

                player.Reply(lang.GetMessage("WebRequestSuccess", this));
                Puts(lang.GetMessage("WebRequestSuccess", this));
            }, this);
        }
    }
}