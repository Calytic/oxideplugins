
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("KickBan", "Reneb & Mughisi", "1.0.3", ResourceId = 946)]
    class KickBan : RustLegacyPlugin
    {
        NetUser cachedUser;
        string cachedSteamid;
        string cachedReason;
        string cachedName;

        public Type BanType;
        public FieldInfo steamid;
        public FieldInfo username;
        public FieldInfo reason;
        public FieldInfo bannedUsers;


        static bool broadcastKick = true;
        static bool broadcastBan = true;
        static bool broadcastUnban = true;
        static string notAllowed = "You are not allowed to use this command.";
        static string banMessage = "{0} - {1} was banned from the server - {2}";
        static string kickMessage = "{0} - {1} was kicked from the server - {2}";
        static string unbanMessage = "{0} - {1} was unbanned from the server";
        static string noplayerfound = "Couldn't find the target user";

        void Loaded()
        {
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
            if (!permission.PermissionExists("canban")) permission.RegisterPermission("canban", this);
            if (!permission.PermissionExists("cankick")) permission.RegisterPermission("cankick", this);
            if (!permission.PermissionExists("canunban")) permission.RegisterPermission("canunban", this);
            BanType = typeof(BanList).GetNestedType("Ban", BindingFlags.Instance | BindingFlags.NonPublic);
            steamid = BanType.GetField("steamid");
            username = BanType.GetField("username");
            reason = BanType.GetField("reason");

            bannedUsers = typeof(BanList).GetField("bannedUsers", (BindingFlags.Static | BindingFlags.NonPublic));
        }


        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Messages: Not Allowed", ref notAllowed);
            CheckCfg<string>("Messages: Ban ({0} is the userid, {1} the name, {2} the reason", ref banMessage);
            CheckCfg<string>("Messages: Kick ({0} is the userid, {1} the name, {2} the reason", ref kickMessage);
            CheckCfg<string>("Messages: Unban ({0} is the userid, {1} the name", ref unbanMessage);
            CheckCfg<string>("Messages: No player found", ref noplayerfound);
            CheckCfg<bool>("Settings: Broadcast Bans", ref broadcastBan);
            CheckCfg<bool>("Settings: Broadcast Kicks", ref broadcastKick);
            SaveConfig();
        }

        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        [ChatCommand("ban")]
        void cmdChatBan(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canban")) { SendReply(netuser, notAllowed); return; }
            if (args.Length == 0) { SendReply(netuser, "/ban STEAMID NAME REASON"); SendReply(netuser, "/ban PLAYERNAME REASON"); return; }
            cachedSteamid = string.Empty;
            cachedName = string.Empty;
            NetUser targetuser = rust.FindPlayer(args[0]);
            if (targetuser != null)
            {
                cachedSteamid = targetuser.playerClient.userID.ToString();
                cachedName = targetuser.playerClient.userName.ToString();
            }
            else
            {
                if (args[0].Length != 17) { SendReply(netuser, noplayerfound); return; }
                cachedSteamid = args[0];
            }
            cachedReason = string.Empty;
            if (args.Length > 1)
            {
                if (cachedName == string.Empty)
                {
                    cachedName = args[1];
                    if (args.Length > 2) cachedReason = args[2];
                }
                else
                    cachedReason = args[1];
            }
            cachedReason += "(" + netuser.displayName + ")";
            if (BanList.Contains(Convert.ToUInt64(cachedSteamid)))
            {
                SendReply(netuser, string.Format("{0} is already in the banlist", cachedSteamid));
                return;
            }
            if (!broadcastBan) ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format(banMessage, cachedSteamid, cachedName, cachedReason)));
            else Broadcast(string.Format(banMessage, cachedSteamid, cachedName, cachedReason));
            Interface.CallHook("cmdBan", cachedSteamid, cachedName, cachedReason);

        }
        [ChatCommand("kick")]
        void cmdChatKick(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "cankick")) { SendReply(netuser, notAllowed); return; }
            if (args.Length == 0) { SendReply(netuser, "/kick STEAMID NAME REASON"); SendReply(netuser, "/kick PLAYERNAME REASON"); return; }
            cachedSteamid = string.Empty;
            cachedName = string.Empty;
            NetUser targetuser = rust.FindPlayer(args[0]);
            if (targetuser != null)
            {
                cachedSteamid = targetuser.playerClient.userID.ToString();
                cachedName = targetuser.playerClient.userName.ToString();
            }
            else
            {
                if (args[0].Length != 17) { SendReply(netuser, noplayerfound); return; }
                cachedSteamid = args[0];
            }
            cachedReason = string.Empty;
            if (args.Length > 1)
            {
                if (cachedName == string.Empty)
                {
                    cachedName = args[1];
                    if (args.Length > 2) cachedReason = args[2];
                }
                else
                    cachedReason = args[1];
            }
            cachedReason += "(" + netuser.displayName + ")";
            if (!broadcastKick) ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format(kickMessage, cachedSteamid, cachedName, cachedReason)));
            else Broadcast(string.Format(kickMessage, cachedSteamid, cachedName, cachedReason));
            Interface.CallHook("cmdKick", cachedSteamid, cachedName, cachedReason);

        }
        [ChatCommand("unban")]
        void cmdChatUnban(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canunban")) { SendReply(netuser, notAllowed); return; }
            if (args.Length == 0) { SendReply(netuser, "/unban STEAMID|PLAYERNAME"); return; }

            var targetunban = args[0];
            var bannedusers = bannedUsers.GetValue(null);
            MethodInfo Enumerator = bannedusers.GetType().GetMethod("GetEnumerator");
            var myEnum = Enumerator.Invoke(bannedusers, new object[0]);
            MethodInfo MoveNext = myEnum.GetType().GetMethod("MoveNext");
            MethodInfo GetCurrent = myEnum.GetType().GetMethod("get_Current");
            string unbantarget = string.Empty;
            string unbanname = string.Empty;
            while ((bool)MoveNext.Invoke(myEnum, new object[0]))
            {
                var bannedUser = GetCurrent.Invoke(myEnum, new object[0]);
                if (targetunban == steamid.GetValue(bannedUser).ToString() || targetunban == username.GetValue(bannedUser).ToString())
                {
                    unbantarget = steamid.GetValue(bannedUser).ToString();
                    unbanname = username.GetValue(bannedUser).ToString();
                }
            }
            if (unbantarget == string.Empty)
            {
                SendReply(netuser, noplayerfound);
                return;
            }

            Interface.CallHook("cmdUnban", unbantarget);
            if (!broadcastUnban) ConsoleNetworker.SendClientCommand(netuser.playerClient.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(string.Format(unbanMessage, unbantarget.ToString(), unbantarget.ToString())));
            else Broadcast(string.Format(unbanMessage, unbantarget.ToString(), unbantarget.ToString()));
        }
        [ChatCommand("banlist")]
        void cmdChatBanlist(NetUser netuser, string command, string[] args)
        {
            if (!(hasAccess(netuser, "canunban") || hasAccess(netuser, "canban"))) { SendReply(netuser, notAllowed); return; }
            int bl = 1;
            if (args.Length > 0) int.TryParse(args[0], out bl);
            var bannedusers = bannedUsers.GetValue(null);
            MethodInfo Enumerator = bannedusers.GetType().GetMethod("GetEnumerator");
            var myEnum = Enumerator.Invoke(bannedusers, new object[0]);
            MethodInfo MoveNext = myEnum.GetType().GetMethod("MoveNext");
            MethodInfo GetCurrent = myEnum.GetType().GetMethod("get_Current");

            int current = 1;
            SendReply(netuser, "Starting banlist from index: " + current.ToString());
            while ((bool)MoveNext.Invoke(myEnum, new object[0]))
            {
                if (current >= bl)
                {
                    var bannedUser = GetCurrent.Invoke(myEnum, new object[0]);
                    SendReply(netuser, string.Format("{0} - {1} - {2}", steamid.GetValue(bannedUser).ToString(), username.GetValue(bannedUser).ToString(), reason.GetValue(bannedUser).ToString()));
                }
                current++;
                if (current > bl + 20) break;
            }
        }
        void Broadcast(string message)
        {
            ConsoleNetworker.Broadcast("chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(message));
        }
        void cmdUnban(string userid)
        {
            BanList.Remove(Convert.ToUInt64(userid));
        }
        bool cmdUnbanByNameOrID(string targetunban)
        {
            var bannedusers = bannedUsers.GetValue(null);
            MethodInfo Enumerator = bannedusers.GetType().GetMethod("GetEnumerator");
            var myEnum = Enumerator.Invoke(bannedusers, new object[0]);
            MethodInfo MoveNext = myEnum.GetType().GetMethod("MoveNext");
            MethodInfo GetCurrent = myEnum.GetType().GetMethod("get_Current");
            string unbantarget = string.Empty;
            string unbanname = string.Empty;
            while ((bool)MoveNext.Invoke(myEnum, new object[0]))
            {
                var bannedUser = GetCurrent.Invoke(myEnum, new object[0]);
                if (targetunban == steamid.GetValue(bannedUser).ToString() || targetunban == username.GetValue(bannedUser).ToString())
                {
                    unbantarget = steamid.GetValue(bannedUser).ToString();
                    unbanname = username.GetValue(bannedUser).ToString();
                }
            }
            if (unbantarget == string.Empty) return false;
            Interface.CallHook("cmdUnban", unbantarget);
            return true;
        }
        void cmdKick(string userid, string name, string reason)
        {
            cachedUser = rust.FindPlayer(userid);
            if (cachedUser != null)
            {
                cachedUser.Kick(NetError.Facepunch_Kick_RCON, true);
            }
        }
        void cmdBan(string userid, string name = "Unknown", string reason = "Unkown")
        {
            ulong playerid;
            if (!ulong.TryParse(userid, out playerid))
            {
                return;
            }
            if (!BanList.Contains(playerid))
            {
                BanList.Add(playerid, name, reason);
                BanList.Save();
            }
            cachedUser = rust.FindPlayer(userid);
            if (cachedUser != null)
            {
                cachedUser.Kick(NetError.ConnectionBanned, true);
            }
        }
        void SendHelpText(NetUser netuser)
        {
            if (hasAccess(netuser, "canunban")) SendReply(netuser, "Kick&Ban Unban: /unban PLAYERNAME/STEAMID");
            if (hasAccess(netuser, "canban")) SendReply(netuser, "Kick&Ban Ban: /ban PLAYERNAME/STEAMID REASON");
            if (hasAccess(netuser, "cankick")) SendReply(netuser, "Kick&Ban Kick: /kick PLAYERNAME/STEAMID REASON");
            if (hasAccess(netuser, "canban") || hasAccess(netuser, "canuban")) SendReply(netuser, "Kick&Ban Banlist: /banlist");
        }
    }
}