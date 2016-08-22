using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("ConsoleMessages", "Skrallex", "1.0.0")]
    [Description("Send messages to players with a console command")]
    class ConsoleMessage : RustPlugin {

        void Loaded() {
            LoadDefaultMessages();
        }

        void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"NoPermission", "You do not have permission to use this command."},
                {"SaySyntax", "Syntax: cm.say PlayerName Message"},
                {"ManyPlayersFound", "Multiple players found with that name, please be more specific."},
                {"NoPlayersFound", "No players found with that name, please try another."}
            }, this);
        }

        [ConsoleCommand("cm.say")]
        void CMSay(ConsoleSystem.Arg args) {
            if(args.Player() != null && !args.Player().IsAdmin()) {
                args.ReplyWith(Lang("NoPermission"));
                return;
            }
            if(!args.HasArgs(2)) {
                args.ReplyWith(Lang("SaySyntax"));
                return;
            }

            if(GetPlayersByName(args.GetString(0)).Count > 1) {
                args.ReplyWith(Lang("ManyPlayersFound"));
                return;
            }
            if(GetPlayersByName(args.GetString(0)).Count == 0) {
                args.ReplyWith(Lang("NoPlayersFound"));
                return;
            }
            BasePlayer target = GetPlayersByName(args.GetString(0))[0];

            SendReply(target, args.GetString(1));
        }

        List<BasePlayer> GetPlayersByName(string playerName) {
            List<BasePlayer> foundPlayers = new List<BasePlayer>();
            foreach(BasePlayer activePlayer in BasePlayer.activePlayerList) {
                if(activePlayer.displayName.ToLower().Contains(playerName.ToLower()))
                    foundPlayers.Add(activePlayer);
            }
            return foundPlayers;
        }

        string Lang(string key) {
            return lang.GetMessage(key, this, null);
        }
    }
}
