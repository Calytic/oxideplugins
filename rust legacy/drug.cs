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


    [Info("Drug", "Allows admins to drug and cure players.", "0.0.1")]
    public class Drug : RustLegacyPlugin
    {
        void Loaded()
        {
            //Add permissions
            if (!permission.PermissionExists("CanDrug")) permission.RegisterPermission("CanDrug", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
        }

        //Returns if player has access
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) { return true; }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "CanDrug"))
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
        [ChatCommand("Drug")]
        void cmdDrug(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "/drug PLAYERNAME");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    rust.RunClientCommand(targetuser, "render.fov 120");
                    rust.RunClientCommand(targetuser, "notice.inventory \"You Have Been Drugged\"");
                    SendReply(netuser, "You Drugged that player");
     

                }
            }
        }
        [ChatCommand("Cure")]
        void cmdCure(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "/cure PLAYERNAME");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    rust.RunClientCommand(targetuser, "render.fov 60");
                    rust.RunClientCommand(targetuser, "notice.inventory \"You Have Been Cured\"");
                    SendReply(netuser, "You Cured that player");
                }
            }
        }
    }
}