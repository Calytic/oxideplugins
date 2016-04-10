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
    [Info("HouseLocations", "Reneb", "1.0.2", ResourceId = 960)]
    class HouseLocations : RustLegacyPlugin
    {
        string cachedSteamID;
        string cachedName;
        ulong cachedsteamid;
        Vector3 cachedVector3;
        Hash<ulong, List<StructureMaster>> userIDToStructure = new Hash<ulong, List<StructureMaster>>();
        Hash<NetUser, List<Vector3>> userTeleports = new Hash<NetUser, List<Vector3>>();

        Vector3 VectorUp = new Vector3(0f, 4f, 0f);

        RustServerManagement management;

        [PluginReference]
        Plugin PlayerDatabase;

        FieldInfo structurecomplist;

        void Loaded() {
            if (!permission.PermissionExists("canlocate")) permission.RegisterPermission("canlocate", this);
            if (!permission.PermissionExists("all")) permission.RegisterPermission("all", this);
            structurecomplist = typeof(StructureMaster).GetField("_structureComponents", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
        }
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin())
                return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "all")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canlocate");
        }
        Vector3 FindFirstComponent(StructureMaster master)
        {
            foreach( StructureComponent comp in (HashSet<StructureComponent>)structurecomplist.GetValue(master))
                return comp.transform.position;
            return Vector3.zero;
        }
        [ChatCommand("housetp")]
        void cmdChatHouseTP(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You don't have access to this command"); return; }
            if (args.Length == 0) { SendReply(player, "/housetp HOUSETPID"); return; }
            if(userTeleports[player] == null) { SendReply(player, "You must use /house PLAYERNAME/ID first"); return; }
            int houseid = 0;
            if(!int.TryParse(args[0], out houseid)) { SendReply(player, "/housetp HOUSETPID"); return; }
            if(houseid < 0 || houseid > userTeleports[player].Count) { SendReply(player, "This ID is out of range"); return; }

            management.TeleportPlayerToWorld(player.networkPlayer, userTeleports[player][houseid] + VectorUp);

        }
        [ChatCommand("house")]
        void cmdChatHouse(NetUser player, string command, string[] args)
        {
            if (!hasAccess(player)) { SendReply(player, "You don't have access to this command"); return; }
            if (args.Length == 0) { SendReply(player, "/house STEAMID/name"); return; }
            string[] steamids;
            ulong teststeam;
            if(args.Length == 1 && args[0].Length == 17 && ulong.TryParse(args[0],out teststeam))
            {
                steamids = new string[] { teststeam.ToString() };
            }
            else
            {
                var tempsteamids = PlayerDatabase?.Call("FindAllPlayers", args[0]);
                if(tempsteamids == null)
                {
                    SendReply(player, "You must have the Player Database plugin to use this plugin.");
                    return;
                }
                steamids = (string[])tempsteamids;
                if(steamids.Length == 0)
                {
                    SendReply(player, "No Players found.");
                    return;
                }
            }
            userIDToStructure.Clear();
            foreach (StructureMaster master in (List<StructureMaster>)StructureMaster.AllStructures)
            {
                if (userIDToStructure[master.ownerID] == null)
                    userIDToStructure[master.ownerID] = new List<StructureMaster>();
                userIDToStructure[master.ownerID].Add(master);
            }
            if (userTeleports[player] == null)
                userTeleports[player] = new List<Vector3>();
            userTeleports[player].Clear();
            int currentid = 0;
            SendReply(player, "/housetp XX => to teleport to the house by houseid.");
            for (int i = 0; i < steamids.Length; i++)
            {
                cachedSteamID = steamids[i];
                cachedsteamid = Convert.ToUInt64(cachedSteamID);
                cachedName = "Unknown";
                var tempname = PlayerDatabase?.Call("GetPlayerData", cachedSteamID, "name");
                if (tempname != null)
                    cachedName = tempname.ToString();
                if(userIDToStructure[cachedsteamid] == null)
                {
                    SendReply(player, string.Format("{0} - {1}: No Structures Found.",cachedSteamID,cachedName));
                    continue;
                }
                foreach(StructureMaster master in userIDToStructure[cachedsteamid])
                {
                    cachedVector3 = FindFirstComponent(master);
                    userTeleports[player].Add(cachedVector3);
                    SendReply(player, string.Format("{3} - {0} - {1}: {2}", cachedSteamID, cachedName, cachedVector3.ToString(), currentid.ToString()));
                    currentid++;
                }
            }
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser)) return;
            SendReply(netuser, "House Locator Commands: /house PLAYER/STEAMID => Find houses owned by this/those players");
        }
    }
}