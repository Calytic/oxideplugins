using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ShowHealth", "Troubled", "0.2.1")]
    [Description("Check any player's health")]

    class ShowHealth : RustPlugin
    {
        void Init()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("showhealth.use", this);
        }

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"PlayerHealth", "{0} has {1} health"},
                {"PlayerNotFound", "Player not found"},
                {"WrongSyntax", "Check your syntax. Use /hp playername"},
                {"NotAllowed", "You are not allowed to use this command" }
            }, this);
        }

        [ChatCommand("hp")]
        void Health(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player, "showhealth.use")) return;

            if (args.Length < 1 || args.Length > 1)
            {
                PrintToChat(player, Lang("WrongSyntax"));
                return;
            }

            var target = rust.FindPlayer(args[0]);
            PrintToChat(player, target == null
                ? Lang("PlayerNotFound")
                : string.Format(Lang("PlayerHealth"), target.displayName, Math.Round(target.health)));
        }

        string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);

        bool IsAllowed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            PrintToChat(player, Lang("NotAllowed"));
            return false;
        }
    }
}
