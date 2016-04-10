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
    [Info("AdminAirdrop", "Reneb", "1.0.2", ResourceId = 938)]
    class AdminAirdrop : RustLegacyPlugin
    {
        NetUser cachedUser;
        string cachedSteamid;
        string cachedReason;
        string cachedName;
        Vector3 cachedPos;

        void Loaded()
        {
            if (!permission.PermissionExists("canairdrop")) permission.RegisterPermission("canairdrop", this);
        }

        static string notAllowed = "You are not allowed to use this command.";
        static string calledAirdrop = "Called airdrop...";
        static string cancelledAirdrop = "{0} Airdrops cancelled...";
        static string destroyedCrates = "{0} Supply crates destroyed...";
        static string massAirdrop = "Calling {0} airdrops ...";
        static string airdropPos = "Airdrop called @ {0}";
        static string airdropPlayer = "Airdrop called @ {0} - {1}'s position";
        static string wrongarguments = "Wrong arguments, or target player doesn't exist";
        static string help1 = "/airdrop => to call an airdrop";
        static string help2 = "/airdrop cancel => to cancel all airdrop planes";
        static string help3 = "/airdrop destroy => to destroy all crates";
        static string help4 = "/airdrop PLAYER => to send an airdrop on the player";
        static string help5 = "/airdrop X Z => to send an airdrop on this location";
        static string help6 = "/airdrop XX => to send XX random airdrops";

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
            CheckCfg<string>("Messages: Called 1 airdrop", ref calledAirdrop);
            CheckCfg<string>("Messages: Cancelled the airdrop calls", ref cancelledAirdrop);
            CheckCfg<string>("Messages: Destroyed all crates", ref destroyedCrates);
            CheckCfg<string>("Messages: Mass airdrop call", ref massAirdrop);
            CheckCfg<string>("Messages: Airdrop on position", ref airdropPos);
            CheckCfg<string>("Messages: Airdrop on player position", ref airdropPlayer);
            CheckCfg<string>("Messages: Wrong Arguments", ref wrongarguments);
            CheckCfg<string>("Messages: Help 1", ref help1);
            CheckCfg<string>("Messages: Help 2", ref help2);
            CheckCfg<string>("Messages: Help 3", ref help3);
            CheckCfg<string>("Messages: Help 5", ref help5);
            CheckCfg<string>("Messages: Help 4", ref help4);
            CheckCfg<string>("Messages: Help 6", ref help6);
            SaveConfig();
        }

        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        [ChatCommand("airdrop")]
        void cmdChatBan(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canairdrop")) { SendReply(netuser, notAllowed); return; }
            
            // CALL 1 AIRDROP
            if(args.Length == 0) { SendReply(netuser, calledAirdrop); SupplyDropZone.CallAirDrop(); return; }

            // CANCEL ALL AIRDROPS
            if (args.Length == 1 && args[0].ToString() == "help")
            {
                SendReply(netuser, help1);
                SendReply(netuser, help2);
                SendReply(netuser, help3);
                SendReply(netuser, help4);
                SendReply(netuser, help5);
                SendReply(netuser, help6);
                return;
            }

            // CANCEL ALL AIRDROPS
            if (args.Length == 1 && args[0].ToString() == "cancel")
            {
                int planenumber = 0;
                foreach (SupplyDropPlane plane in UnityEngine.Resources.FindObjectsOfTypeAll<SupplyDropPlane>())
                {
                    if (plane.gameObject.name == "C130") continue;
                    planenumber++;
                    plane.NetDestroy();
                }
                SendReply(netuser, string.Format(cancelledAirdrop, planenumber.ToString()));
                return;
            }

            // DESTROY ALL SUPPLY CRATES
            if (args.Length == 1 && args[0].ToString() == "destroy")
            {
                int cratenumber = 0; 
                foreach (SupplyCrate crate in UnityEngine.Resources.FindObjectsOfTypeAll<SupplyCrate>())
                {
                    if (crate.gameObject.name == "SupplyCrate") continue;
                    cratenumber++;
                    NetCull.Destroy(crate.gameObject);
                }
                SendReply(netuser, string.Format(destroyedCrates, cratenumber.ToString()));
                return;
            }

            // CALL MASS AIRDROP
            int number;
            if(args.Length == 1 && int.TryParse(args[0], out number))
            {
                SendReply(netuser,string.Format(massAirdrop, number.ToString() ));
                for ( int i = 0; i < number; i ++)
                {
                    SupplyDropZone.CallAirDrop();
                }
                return;
            }

            // CALL AN AIRDROP ON POSITION
            float x;
            float z;
            if(args.Length > 1 && float.TryParse(args[0],out x))
            {
                if(args.Length == 2) float.TryParse(args[1], out z);
                else float.TryParse(args[2], out z);
                
                if(z != default(float))
                {
                    cachedPos = new Vector3(x, 0f, z);
                    SupplyDropZone.CallAirDropAt(cachedPos);
                    SendReply(netuser, string.Format(airdropPos, cachedPos.ToString()));
                }
                return;
            }

            // CALL AN AIRDROP TO A PLAYER POSITION
            NetUser targetuser = rust.FindPlayer(args[0]);
            if (targetuser != null)
            {
                cachedPos = targetuser.playerClient.lastKnownPosition;
                if(cachedPos != default(Vector3))
                {
                    SupplyDropZone.CallAirDropAt(cachedPos);
                    SendReply(netuser, string.Format(airdropPlayer, cachedPos.ToString(), targetuser.playerClient.userName.ToString()));
                }
                return;
            }


            SendReply(netuser, wrongarguments);
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser, "canairdrop")) return;
            SendReply(netuser, "Airdrop Commands: /airdrop help");
        }
    }
}