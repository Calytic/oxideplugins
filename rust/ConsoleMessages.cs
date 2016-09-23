using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("ConsoleMessages", "Skrallex", "1.1.1", ResourceId = 2093)]
    [Description("Send messages to players with a console command")]
    class ConsoleMessages : RustPlugin {
        bool UsePermissionsOnly = false;

        const string adminPerm = "consolemessages.admin";
        const string sayPerm = "consolemessages.say";
        const string sayAllPerm = "consolemessages.sayall";
        const string replyPerm = "consolemessages.reply";

        void Loaded() {
            permission.RegisterPermission(adminPerm, this);
            permission.RegisterPermission(sayPerm, this);
            permission.RegisterPermission(sayAllPerm, this);
            permission.RegisterPermission(replyPerm, this);
            LoadDefaultMessages();
            LoadConfig();
        }

        protected override void LoadDefaultConfig() {
            Puts("Generating default config file");
            Config.Clear();
            Config["UsePermissionsOnly"] = false;
            SaveConfig();
        }

        void LoadConfig() {
            this.UsePermissionsOnly = (bool)Config["UsePermissionsOnly"];
        }

        void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"Prefix", "<color=orange>ConsoleMessages</color>:"},
                {"NoPermission", "You do not have permission to use this command."},
                {"SaySyntax", "Syntax: cm.say PlayerName This is a message"},
                {"SayNPSyntax", "Syntax: cm.saynp PlayerName This is a message (No Prefix)"},
                {"SayAllSyntax", "Syntax: cm.sayall This is a message"},
                {"SayAllNPSyntax", "Syntax: cm.sayallnp This is a message (No Prefix)"},
                {"ReplySyntax", "Syntax: /cm_reply Message"},
                {"SayMessageSent", "Sent message to {0}: \"{1}\""},
                {"SayAllMessageSent", "Sent message to all online players: \"{0}\""},
                {"MessageSent", "Your message \"{0}\" was sent to the console."},
                {"ManyPlayersFound", "Multiple players found with that name, please be more specific."},
                {"NoPlayersFound", "No players found with that name, please try another."}
            }, this);
        }

        //Prefix attached to message.
        [ConsoleCommand("cm.say")]
        void ConsoleCmdCMSay(ConsoleSystem.Arg args) {
            if(args.Args.Length < 2) {
                ReplyConsole(args, "SaySyntax");
                return;
            }
            CMSay(args);
        }

        //No prefix attached to message.
        [ConsoleCommand("cm.saynp")]
        void ConsoleCmdCMSayNP(ConsoleSystem.Arg args) {
            if(args.Args.Length < 2) {
                ReplyConsole(args, "SayNPSyntax", false);
                return;
            }
            CMSay(args, false);
        }

        [ConsoleCommand("cm.sayall")]
        void ConsoleCmdCMSayAll(ConsoleSystem.Arg args) {
            if(args.Args.Length < 1) {
                ReplyConsole(args, "SayAllSyntax");
                return;
            }
            CMSayAll(args);
        }

        [ConsoleCommand("cm.sayallnp")]
        void ConsoleCmdCMSayAllNP(ConsoleSystem.Arg args) {
            if(args.Args.Length < 1) {
                ReplyConsole(args, "SayAllNPSyntax");
                return;
            }
            CMSayAll(args, false);
        }

        void CMSay(ConsoleSystem.Arg args, bool usePrefix = true) {
            string msg = "";
            string[] words = args.Args.Skip(1).ToArray();
            if(args.Player() != null && !IsAllowed(args.Player(), sayPerm)) {
                ReplyConsole(args, "NoPermission", usePrefix);
                return;
            }
            if(GetPlayersByName(args.GetString(0)).Count > 1) {
                ReplyConsole(args, "ManyPlayersFound", usePrefix);
                return;
            }
            if(GetPlayersByName(args.GetString(0)).Count == 0) {
                ReplyConsole(args, "NoPlayersFound", usePrefix);
                return;
            }
            foreach(string word in words) {
                msg += word + " ";
            }

            BasePlayer target = GetPlayersByName(args.GetString(0))[0];
            ReplyPlayer(target, msg, usePrefix);
            ReplyConsoleFormatted(args, String.Format(Lang("SayMessageSent"), target.displayName, msg), usePrefix);
        }

        void CMSayAll(ConsoleSystem.Arg args, bool usePrefix = true) {
            string msg = "";
            if(args.Player() != null && !IsAllowed(args.Player(), sayAllPerm)) {
                ReplyConsole(args, "NoPermission", usePrefix);
                return;
            }
            foreach(string word in args.Args) {
                msg += word + " ";
            }
            foreach(BasePlayer activePlayer in BasePlayer.activePlayerList) {
                ReplyPlayer(activePlayer, msg, usePrefix);
            }
            ReplyConsoleFormatted(args, String.Format(Lang("SayAllMessageSent"), msg), usePrefix);
        }

        [ChatCommand("cm_reply")]
        void ChatCmdCMReply(BasePlayer player, string cmd, string[] args) {
            string msg = "";
            if(!IsAllowed(player, replyPerm)) {
                ReplyPlayer(player, "NoPermission");
                return;
            }
            if(args.Length < 1) {
                ReplyPlayer(player, "ReplySyntax");
                return;
            }

            foreach(string arg in args) {
                msg += arg + " ";
            }
            Puts(player.displayName + ": " + msg);
            ReplyFormatted(player, String.Format(Lang("MessageSent"), msg));
        }

        List<BasePlayer> GetPlayersByName(string playerName) {
            List<BasePlayer> foundPlayers = new List<BasePlayer>();
            foreach(BasePlayer activePlayer in BasePlayer.activePlayerList) {
                if(activePlayer.displayName.ToLower().Contains(playerName.ToLower()) || activePlayer.UserIDString.Equals(playerName))
                    foundPlayers.Add(activePlayer);
            }
            return foundPlayers;
        }

        void ReplyPlayer(BasePlayer player, string langKey, bool usePrefix = true) {
            if(!usePrefix) {
                SendReply(player, Lang(langKey));
                return;
            }
            SendReply(player, Lang("Prefix") + " " + Lang(langKey));
        }

        void ReplyFormatted(BasePlayer player, string msg, bool usePrefix = true) {
            if(!usePrefix) {
                SendReply(player, msg);
                return;
            }
            SendReply(player, Lang("Prefix") + " " + msg);
        }

        void ReplyConsole(ConsoleSystem.Arg args, string langKey, bool usePrefix = true) {
            if(!usePrefix || args.Player() == null) {
                args.ReplyWith(Lang(langKey));
                return;
            }
            args.ReplyWith(Lang("Prefix") + " " + Lang(langKey));
        }

        void ReplyConsoleFormatted(ConsoleSystem.Arg args, string msg, bool usePrefix = true) {
            if(!usePrefix || args.Player() == null) {
                args.ReplyWith(msg);
                return;
            }
            args.ReplyWith(Lang("Prefix") + " " + msg);
        }

        bool IsAllowed(BasePlayer player, string perm) {
            if(player.IsAdmin() && !UsePermissionsOnly) return true;
            if(permission.UserHasPermission(player.UserIDString, adminPerm)) return true;
            if(permission.UserHasPermission(player.UserIDString, perm)) return true;
            return false;
        }

        string Lang(string key) {
            return lang.GetMessage(key, this, null);
        }
    }
}
