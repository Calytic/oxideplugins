using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Plugins;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PrivateMessage", "Nogrod", "2.0.2", ResourceId = 659)]
    class PrivateMessage : RustPlugin
    {
        private readonly Dictionary<ulong, ulong> pmHistory = new Dictionary<ulong, ulong>();

        [PluginReference]
        private Plugin Ignore;

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"PMTo", "<color=#00FFFF>PM to {0}</color>: {1}"},
                {"PMFrom", "<color=#00FFFF>PM from {0}</color>: {1}"},
                {"PlayerNotOnline", "{0} is not online."},
                {"NotOnlineAnymore", "The last person you was talking to is not online anymore."},
                {"NotMessaged", "You haven't messaged anyone or they haven't messaged you."},
                {"IgnoreYou", "<color=red>{0} is ignoring you and cant recieve your PMs</color>"},
                {"SelfPM", "You can not send messages to yourself."},
                {"SyntaxR", "Incorrect Syntax use: /r <msg>"},
                {"SyntaxPM", "Incorrect Syntax use: /pm <name> <msg>"}
            }, this);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (pmHistory.ContainsKey(player.userID)) pmHistory.Remove(player.userID);
        }

        [ChatCommand("pm")]
        void cmdPm(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 1)
            {
                var name = args[0];
                var p = FindPlayer(name);
                if (p == player)
                {
                    PrintMessage(player, "SelfPM");
                    return;
                }
                if (p != null)
                {
                    if (!(bool) (Interface.Oxide.CallHook("CanChat", player) ?? true))
                    {
                        SendReply(player, "You are not allowed to chat here");
                        return;
                    }
                    var hasIgnore = Ignore?.CallHook("HasIgnored", p.userID, player.userID);
                    if (hasIgnore != null && (bool) hasIgnore)
                    {
                        PrintMessage(player, "IgnoreYou", p.displayName);
                        return;
                    }
                    var msg = string.Empty;
                    for (var i = 1; i < args.Length; i++)
                        msg = $"{msg} {args[i]}";
                    pmHistory[player.userID] = p.userID;
                    pmHistory[p.userID] = player.userID;
                    PrintMessage(player, "PMTo", p.displayName, msg);
                    PrintMessage(p, "PMFrom", player.displayName, msg);
                    Puts("[PM]{0}->{1}:{2}", player.displayName, p.displayName, msg);
                }
                else
                    PrintMessage(player, "PlayerNotOnline", name);
            }
            else
                PrintMessage(player, "SyntaxPM");
        }

        [ChatCommand("r")]
        void cmdPmReply(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                ulong steamid;
                if (pmHistory.TryGetValue(player.userID, out steamid))
                {
                    var p = FindPlayer(steamid);
                    if (p != null)
                    {
                        if (!(bool) (Interface.Oxide.CallHook("CanChat", player) ?? true))
                        {
                            SendReply(player, "You are not allowed to chat here");
                            return;
                        }
                        var hasIgnore = Ignore?.CallHook("HasIgnored", p.userID, player.userID);
                        if (hasIgnore != null && (bool)hasIgnore)
                        {
                            PrintMessage(player, "IgnoreYou", p.displayName);
                            return;
                        }
                        var msg = string.Empty;
                        for (var i = 0; i < args.Length; i++)
                            msg = $"{msg} {args[i]}";
                        PrintMessage(player, "PMTo", p.displayName, msg);
                        PrintMessage(p, "PMFrom", player.displayName, msg);
                        Puts("[PM]{0}->{1}:{2}", player.displayName, p.displayName, msg);
                    }
                    else
                        PrintMessage(player, "NotOnlineAnymore");
                }
                else
                    PrintMessage(player, "NotMessaged");
            }
            else
                PrintMessage(player, "SyntaxR");
        }

        private void PrintMessage(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        }

        private static BasePlayer FindPlayer(ulong id)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == id)
                    return activePlayer;
            }
            return null;
        }
    }
}
