// Reference: Oxide.Ext.Rust

using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("BuildingPrivileges", "Reneb", 1.0)]
    class BuildingPrivileges : RustPlugin
    {
        private FieldInfo localbuildingPrivileges;
        void Loaded()
        {
            localbuildingPrivileges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        [ChatCommand("bdp")]
        void cmdChatBuildingPrivileges(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            var bldprivs = localbuildingPrivileges.GetValue(player) as List<BuildingPrivlidge>;
            foreach (BuildingPrivlidge bldpriv in bldprivs)
            {
                SendReply(player, string.Format("Found a Tool Cupboard @ {0} {1} {2}, allowed users:", Math.Round(bldpriv.transform.position.x).ToString(), Math.Round(bldpriv.transform.position.y).ToString(), Math.Round(bldpriv.transform.position.z).ToString()));
                var locAllowed = bldpriv.authorizedPlayers;
                foreach (ProtoBuf.PlayerNameID ply in locAllowed)
                {
                    SendReply(player, ply.username);
                }
            }
        }
    }
}