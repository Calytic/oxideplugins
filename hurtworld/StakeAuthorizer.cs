//Reference: UnityEngine.UI
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Stake Authorizer", "LaserHydra", "1.1.3", ResourceId = 1577)]
    [Description("Auth/deauth yourself for every stake")]
    class StakeAuthorizer : HurtworldPlugin
    {
        Dictionary<PlayerSession, List<OwnershipStakeServer>> authed = new Dictionary<PlayerSession, List<OwnershipStakeServer>>();

        ////////////////////////////////////////
        ///     Plugin Base Hooks and Methods
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("stakeauthorizer.use", this);
            LoadMessages();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Authorized", "You have been authorized at all stakes."},
                {"Authorized Radius", "You have been authorized at all stakes in the radius of {radius}m."},
                {"Deauthorized", "You have been deauthorized at all stakes."},
                {"Deauthorized Radius", "You have been deauthorized at all stakes in the radius of {radius}m."},
                {"Invalid Radius Argument", "Syntax Error! /{cmd} <radius> | The <radius> argument must be a valid number!"},
                {"Undone", "You have been deauthorized at all stakes you have authorized yourself when using /authall the last time."}
            }, this);
        }

        ////////////////////////////////////////
        ///     Commands
        ////////////////////////////////////////

        [ChatCommand("authall")]
        void cmdAuthorize(PlayerSession player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.SteamId.ToString(), "stakeauthorizer.use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId.ToString()));
                return;
            }

            if (args.Length == 1)
            {
                int radius;

                if (!TryConvert(args[0], out radius))
                {
                    SendChatMessage(player, GetMsg("Invalid Radius Argument", player.SteamId.ToString()).Replace("{cmd}", cmd));
                    return;
                }

                Authorize(player, radius);
                SendChatMessage(player, GetMsg("Authorized Radius").Replace("{radius}", radius.ToString()));
            }
            else
            {
                Authorize(player, 0);
                SendChatMessage(player, GetMsg("Authorized"));
            }
        }

        [ChatCommand("deauthall")]
        void cmdDeauthorize(PlayerSession player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.SteamId.ToString(), "stakeauthorizer.use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId.ToString()));
                return;
            }

            if (args.Length == 1)
            {
                int radius;

                if (!TryConvert(args[0], out radius))
                {
                    SendChatMessage(player, GetMsg("Invalid Radius Argument", player.SteamId.ToString()).Replace("{cmd}", cmd));
                    return;
                }

                Deauthorize(player, radius);
                SendChatMessage(player, GetMsg("Deauthorized Radius").Replace("{radius}", radius.ToString()));
            }
            else
            {
                Deauthorize(player, 0);
                SendChatMessage(player, GetMsg("Deauthorized"));
            }
        }

        [ChatCommand("undoauth")]
        void cmdUndoauthorization(PlayerSession player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.SteamId.ToString(), "stakeauthorizer.use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId.ToString()));
                return;
            }

            Undoauthorization(player);
            SendChatMessage(player, GetMsg("Undone"));
        }

        ////////////////////////////////////////
        ///     Stake Related
        ////////////////////////////////////////

        void Authorize(PlayerSession player, int radius)
        {
            List<OwnershipStakeServer> stakes = GetStakes(player.WorldPlayerEntity.transform.position, radius);
            authed[player] = stakes;

            foreach (OwnershipStakeServer stake in stakes)
                if (!stake.HasAuthority(player.Player))
                    stake.Authorize(player.Identity, player.WorldPlayerEntity);
        }

        void Deauthorize(PlayerSession player, int radius)
        {
            List<OwnershipStakeServer> stakes = GetStakes(player.WorldPlayerEntity.transform.position, radius);

            foreach (OwnershipStakeServer stake in stakes)
                if (stake.HasAuthority(player.Player))
                    stake.Deauthorize(player.Identity, player.WorldPlayerEntity);
        }

        void Undoauthorization(PlayerSession player)
        {
            if (authed.ContainsKey(player))
                foreach (OwnershipStakeServer stake in authed[player])
                    if (stake.HasAuthority(player.Player))
                        stake.Deauthorize(player.Identity, player.WorldPlayerEntity);
        }

        List<OwnershipStakeServer> GetStakes(Vector3 sourcePos, float radius) => (from collider in Physics.OverlapSphere(sourcePos, radius) where collider.GetComponent<OwnershipStakeServer>() != null select collider.GetComponent<OwnershipStakeServer>()).ToList();

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        bool TryConvert<S, C>(S source, out C converted)
        {
            try
            {
                converted = (C) Convert.ChangeType(source, typeof(C));
                return true;
            }
            catch (Exception)
            {
                converted = default(C);
                return false;
            }
        }

        ////////////////////////////////////////
        ///     Config and Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        string GetMsg(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }

        ////////////////////////////////////////
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
    }
}
