using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("CapsNoCaps", "PsychoTea", "1")]
	[Description("Turns all uppercase chat into lowercase")]
	
    public sealed class CapsNoCaps : RustPlugin
	{
        bool OnPlayerChat(ConsoleSystem.Arg arg, string player)
        {
            BasePlayer uidPlayer = (BasePlayer)arg.connection.player;
            string message = arg.GetString(0, "text");
            string uid = uidPlayer.userID.ToString();

            if (!(message[0].Equals("/")))
            {
                string lowerMessage = message.ToLower();
                ConsoleSystem.Broadcast("chat.add", uid, string.Format(uidPlayer.displayName + ": " + lowerMessage), 1.0);
                return false;
            }

            return false;
        }
	}
}