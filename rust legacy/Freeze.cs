 using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
namespace Oxide.Plugins
{


    [Info("Freeze", "Freeze a player so they can't move", "0.0.1")]
    public class Freeze : RustLegacyPlugin
    {
        void Loaded()
        {
            //Add permissions
            if (!permission.PermissionExists("CanFreeze")) permission.RegisterPermission("CanFreeze", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
        }

        //Returns if player has access
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) { return true; }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "CanFreeze"))
            {
                return true;
            }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Chat Command
        [ChatCommand("Freeze")]
        void cmdGetRekt(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "/freeze PLAYERNAME");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    rust.RunClientCommand(targetuser, "input.bind Duck 1 None");
                    rust.RunClientCommand(targetuser, "input.bind Jump 3 None");
                    rust.RunClientCommand(targetuser, "input.bind Fire 3 None");
                    rust.RunClientCommand(targetuser, "input.bind AltFire 7 None");
                    rust.RunClientCommand(targetuser, "input.bind Up 6 RightArrow");
                    rust.RunClientCommand(targetuser, "input.bind Down 6 LeftArrow");
                    rust.RunClientCommand(targetuser, "input.bind Left 6 UpArrow");
                    rust.RunClientCommand(targetuser, "input.bind Right 7 DownArrow");
                    rust.RunClientCommand(targetuser, "input.bind Flashlight 8 Insert");
                    rust.RunClientCommand(targetuser, "deathscreen.reason \"[color cyan]âYou Have Been Frozen!â\"");
                    rust.RunClientCommand(targetuser, "deathscreen.show");
                    SendReply(netuser, "You Froze that player");
     

                }
            }
        }
        [ChatCommand("Unfreeze")]
        void cmdGetFixed(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "/unfreeze PLAYERNAME");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    rust.RunClientCommand(targetuser, "input.bind Up W None");
                    rust.RunClientCommand(targetuser, "input.bind Down S None");
                    rust.RunClientCommand(targetuser, "input.bind Left A None");
                    rust.RunClientCommand(targetuser, "input.bind Right D None");
                    rust.RunClientCommand(targetuser, "input.bind Fire Mouse0 None");
                    rust.RunClientCommand(targetuser, "input.bind AltFire Mouse1 none");
                    rust.RunClientCommand(targetuser, "input.bind Sprint LeftShift none");
                    rust.RunClientCommand(targetuser, "input.bind Duck LeftControl None");
                    rust.RunClientCommand(targetuser, "input.bind Jump Space None");
                    rust.RunClientCommand(targetuser, "input.bind Inventory Tab None");
				    rust.RunClientCommand(targetuser, "config.load");
                    rust.RunClientCommand(targetuser, "config.save");
                    rust.RunClientCommand(targetuser, "deathscreen.reason \"[color #0BFF55]âYou Have Been Unfrozen!â\"");
                    rust.RunClientCommand(targetuser, "deathscreen.show");
                    SendReply(netuser, "You Unfroze that player");
                }
            }
        }
    }
}