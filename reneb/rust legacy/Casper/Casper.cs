using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.RustLegacy;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("Casper", "Reneb", "1.0.0")]
    class Casper : RustLegacyPlugin
    {
        RustServerManagement manager;
        Vector3 VectorAway = new Vector3(0f, 500f, 0f);

        void Loaded() 
        {
            manager = RustServerManagement.Get();
            if (!permission.PermissionExists("cancasper")) permission.RegisterPermission("cancasper", this);
        }

        List<HumanController> caspers = new List<HumanController>();
        Dictionary<HumanController,Vector3> teleported = new Dictionary<HumanController, Vector3>();
        object OnGetClientMove(HumanController human, Vector3 origin, int encoded, ushort stateFlags, uLink.NetworkMessageInfo info)
        { 
            if(!caspers.Contains(human)) return null;
            if (teleported.ContainsKey(human)) return false;
            if (human.dead) return null;
            if (human.networkView.viewID == uLink.NetworkViewID.unassigned) return null;
            Angle2 ang = new Angle2 { encoded = encoded };
            teleported.Add(human, origin);
            human.idMain.origin = origin + VectorAway;
            human.idMain.eyesAngles = ang;
            human.idMain.stateFlags.flags = stateFlags;
            object[] args = new object[] { origin + VectorAway, ang.encoded, stateFlags, (float)(NetCull.time - info.timestamp) };
            human.networkView.RPC("ReadClientMove", uLink.RPCMode.Others, args);
            human.ServerFrame();
            return false;
        }

        void ClearAllInjuries(NetUser netuser)
        {
            if (netuser == null || netuser.playerClient == null) return;
            if (netuser.playerClient.rootControllable == null) return;
            if (!caspers.Contains(netuser.playerClient.rootControllable.GetComponent<HumanController>())) return;
            netuser.playerClient.rootControllable.GetComponent<FallDamage>().ClearInjury();
            timer.Once(1f, () => ClearAllInjuries(netuser));
        }

        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cancasper");
        }

        [ChatCommand("casper")]
        void cmdChatCasper(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser)) { SendReply(netuser, "You dont have access to this command."); return; }
            var humancontroller = netuser.playerClient.rootControllable.GetComponent<HumanController>();
            if(caspers.Contains(humancontroller))
            {
                SendReply(netuser, "Casper: OFF");
                caspers.Remove(humancontroller);
                if (teleported.ContainsKey(humancontroller))
                {
                    manager.TeleportPlayerToWorld(netuser.networkPlayer, teleported[humancontroller]);
                    teleported.Remove(humancontroller);
                }
                netuser.playerClient.rootControllable.GetComponent<TakeDamage>().SetGodMode(false);
                return;
            }
            else
            {
                caspers.Add(humancontroller);
                SendReply(netuser, "Casper: ON, when you get back to the ground, no one will see you nor hear you!");
                netuser.playerClient.rootControllable.GetComponent<TakeDamage>().SetGodMode(true);
                timer.Once(1f, () => ClearAllInjuries(netuser));
                return;
            }
        }
    }
}