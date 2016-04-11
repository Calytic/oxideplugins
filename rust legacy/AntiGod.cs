using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("AntiGod", "xBDMx", "1.1.0")]
    [Description("Forcefully disable Godmode on anyone.")]

    public class AntiGod : RustLegacyPlugin
    {
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"CommandUsage", "Usage: [color orange]/ungod [color white]'[color yellow]username[color white]'"},
                {"NoPermission", "You do not have permission to use '[color orange]/ungod[color white]'"},
                {"RemovedGodmode", "Godmode removed from '[color yellow]{0}[color white]'"}
            };
            lang.RegisterMessages(messages, this);
        }

        void Loaded()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("antigod.allowed", this);
        }

        [ChatCommand("ungod")]
        void CmdUngod(NetUser netuser, string command, string[] args)
        {
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "antigod.allowed"))
            {
                SendReply(netuser, GetMessage("NoPermission", netuser.userID.ToString()));
                return;
            }

            if (args.Length != 1)
            {
                SendReply(netuser, GetMessage("CommandUsage", netuser.userID.ToString()));
                return;
            }

            var target = rust.FindPlayer(args[0]);
            if (target != null)
            {
                target.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
                SendReply(netuser, string.Format(GetMessage("RemovedGodmode", netuser.userID.ToString()), target.displayName));
            }
        }

        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}
