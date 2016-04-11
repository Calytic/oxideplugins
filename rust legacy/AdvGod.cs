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

/* IN MADE EDITION
.----           ---,---  .----       
| __    |    |     |     |___   
|   |   |    |     |     |       
`^--'   `----^     |     `^---  
 ~GutePG - POINTGAME
*/

namespace Oxide.Plugins
{
	[Info("AdvGod", "Gute & PointGame - credits xBDMx", "1.0.0")]
    [Description("You can give god mode for a player.")]
	public class AdvGod : RustLegacyPlugin
	{
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPermission", "You are not allowed to use '[color orange]/god[color white]'"},
                {"CommandUsage", "Use syntax: [color orange]/god [color white]'[color yellow]username[color white]'"},
                {"GodReply", "You gave god mode for -[COLOR YELLOW] {0}"},
                {"GodMessage", "You got godmode by -[COLOR CYAN]"},
                {"UngodReply", "You shot of godmode -[COLOR YELLOW] {0}"},
                {"UngodMessage", "He was removed given by godmode -[COLOR CYAN]"}
            };
            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("advgod.allowed", this);
        }
		[ChatCommand("god")]
		void cmdGod(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advgod.allowed"))
			{
                SendReply(netuser, GetMessage("NoPermission", netuser.userID.ToString()));
                return;
			}
			else if (args.Length != 1)
					{
                SendReply(netuser, GetMessage("CommandUsage", netuser.userID.ToString()));
                return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     targetuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(true);
                     SendReply(netuser, string.Format(GetMessage("GodReply", netuser.userID.ToString()), targetuser.displayName));
                     SendReply(targetuser, GetMessage("GodMessage", netuser.userID.ToString()) + netuser.displayName);
			}
            }
		}
		[ChatCommand("ungod")]
		void cmdUngod(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advgod.allowed"))
			{
                SendReply(netuser, GetMessage("NoPermission", netuser.userID.ToString()));
                return;
			}
			else if (args.Length != 1)
					{
                SendReply(netuser, GetMessage("CommandUsage", netuser.userID.ToString()));
                return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     targetuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
                     SendReply(netuser, string.Format(GetMessage("UngodReply", netuser.userID.ToString()), targetuser.displayName));
                     SendReply(targetuser, GetMessage("UngodMessage", netuser.userID.ToString()) + netuser.displayName);
			}
            }
		}
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);     
        } 

	} 