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
	[Info("AdvZap", "BDM", "1.0.0")]
    [Description("Makes a player unable to move.")]
	public class AdvZap : RustLegacyPlugin
	{
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPermission", "You do not have permission to use '[color orange]/zap[color white]'"},
                {"CommandUsage", "Use this syntax: [color orange]/zap [color white]'[color yellow]username[color white]'"},
                {"ZapReply", "You have zapped -[COLOR YELLOW] {0}"},
                {"ZapMessage", "You have been zapped by -[COLOR CYAN]"},
                {"UnzapReply", "You have unzapped -[COLOR YELLOW] {0}"},
                {"UnzapMessage", "You have been unzapped by -[COLOR CYAN]"}
            };
            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("advzap.allowed", this);
        }
		[ChatCommand("zap")]
		void cmdZap(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advzap.allowed"))
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
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     var hbtd = targetuser.playerClient.rootControllable.GetComponent<HumanBodyTakeDamage>();
                     hbtd.HealOverTime(100f);
                     rootCharacter.takeDamage.health = -100;
                     rootCharacter.takeDamage.maxHealth = 100;
                     targetuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
                     SendReply(netuser, string.Format(GetMessage("ZapReply", netuser.userID.ToString()), targetuser.displayName));
                     SendReply(targetuser, GetMessage("ZapMessage", netuser.userID.ToString()) + netuser.displayName);
			}
            }
		}
		[ChatCommand("unzap")]
		void cmdUnZap(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advzap.allowed"))
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
                     int hp = 99;
                     int mhp = 100;
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     rootCharacter.takeDamage.health = hp;
                     rootCharacter.takeDamage.maxHealth = mhp;
                     targetuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
                     SendReply(netuser, string.Format(GetMessage("UnzapReply", netuser.userID.ToString()), targetuser.displayName));
                     SendReply(targetuser, GetMessage("UnzapMessage", netuser.userID.ToString()) + netuser.displayName);
			}
            }
		}
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);     
        } 

	} 