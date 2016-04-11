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

/* BROUGHT TO YOU BY        
,.   ,.         .  .        `.---     .              . 
`|  / ,-. ,-. . |  |  ,-.    |__  . , |- ,-. ,-. ,-. |-
 | /  ,-| | | | |  |  ,-|   ,|     X  |  |   ,-| |   | 
 `'   `-^ ' ' ' `' `' `-^   `^--- ' ` `' '   `-^ `-' `'
 ~PrincessRadPants and Swuave
*/
namespace Oxide.Plugins
{


    [Info("VENoRecoilTest", "PrincessRadPants and Swuave", "1.0.0")]
    public class VENoRecoilTest : RustLegacyPlugin
    {
        void Loaded()
        {
            //Add permissions
            if (!permission.PermissionExists("cannorecoil")) permission.RegisterPermission("cannorecoil", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
        }

        //Returns if player has access
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) { return true; }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cannorecoil"))
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

        void TestUser(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "config.save");
            rust.RunClientCommand(targetuser, "input.mousespeed 0");

            rust.RunClientCommand(targetuser, "input.bind Up F4 None");
            rust.RunClientCommand(targetuser, "input.bind Down F4 None");
            rust.RunClientCommand(targetuser, "input.bind Left F4 None");
            rust.RunClientCommand(targetuser, "input.bind Right F4 None");
            rust.RunClientCommand(targetuser, "input.bind Fire Mouse0 W");
            rust.RunClientCommand(targetuser, "input.bind AltFire F4 none");
            rust.RunClientCommand(targetuser, "input.bind Sprint F4 none");
            rust.RunClientCommand(targetuser, "input.bind Duck F4 None");
            rust.RunClientCommand(targetuser, "input.bind Jump F4 None");
            rust.RunClientCommand(targetuser, "input.bind Inventory 7 None");
        }
        void EndTest(NetUser targetuser)
        {
            rust.RunClientCommand(targetuser, "config.load");
        }

        //Chat Command
        [ChatCommand("recoiltest")]
        void cmdRecoilTest(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "Syntax: /recoiltest <playername>");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    TestUser(targetuser);
                }
            }
        }
        [ChatCommand("testend")]
        void cmdTestEnd(NetUser netuser, string command, string[] args)
        {
            //Make sure player has access to use command
            if (!hasAccess(netuser))
            {
                SendReply(netuser, "You do not have permission to use this command");
            }
            else if (args.Length != 1)
            {
                SendReply(netuser, "Syntax: /testend <playername>");
            }
            else
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    EndTest(targetuser);
                }
            }
        }
    }
}