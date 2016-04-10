// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers
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
    [Info("Prod", "Reneb", "1.0.2", ResourceId = 928)]
    class Prod : RustLegacyPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        private static FieldInfo accessUsers;

        RaycastHit cachedRaycast;
        Character cachedCharacter;
        bool cachedBoolean;
        Collider cachedCollider;
        StructureComponent cachedStructure;
        DeployableObject cachedDeployable;
        StructureMaster cachedMaster;
        Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;

        void Loaded() {
            if (!permission.PermissionExists("prod")) permission.RegisterPermission("prod", this);
            accessUsers = typeof(PasswordLockableObject).GetField("_validUsers", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        }
        void OnServerInitialized()
        { 
        }
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin())
                return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "prod");
        }
        [ChatCommand("prod")]
        void cmdChatProd(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser)) { SendReply(netuser, "You don't have access to this command"); return; }
            cachedCharacter = netuser.playerClient.rootControllable.idMain.GetComponent<Character>();
            if (!MeshBatchPhysics.Raycast(cachedCharacter.eyesRay, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { SendReply(netuser, "Are you looking at the sky?"); return; }
            //Debug.Log(cachedRaycast.collider.ToString());
            //Debug.Log(LayerMask.LayerToName(cachedRaycast.collider.gameObject.layer));
            //Debug.Log(cachedRaycast.collider.transform.position.y.ToString());
            //Debug.Log(cachedCharacter.origin.y.ToString());

            //GameObject.Destroy(cachedRaycast.collider.GetComponent<UnityEngine.MeshCollider>());
            /*
            
            var components = cachedRaycast.collider.GetComponents<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }
            Debug.Log("============= COMPONENTS IN PARENT =============");
            components = cachedRaycast.collider.GetComponentsInParent<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }
            Debug.Log("============= COMPONENTS IN CHILDREN =============");
            components = cachedRaycast.collider.GetComponentsInChildren<UnityEngine.Component>();
            foreach (var comp in components)
            {
                Debug.Log(comp.ToString());
            }*/
            if (cachedhitInstance != null)
            {
                cachedCollider = cachedhitInstance.physicalColliderReferenceOnly;
                if (cachedCollider == null) { SendReply(netuser, "Can't prod what you are looking at"); return; }
                cachedStructure = cachedCollider.GetComponent<StructureComponent>();
                if (cachedStructure != null && cachedStructure._master != null)
                {
                    cachedMaster = cachedStructure._master;
                    var name = PlayerDatabase?.Call("GetPlayerData", cachedMaster.ownerID.ToString(), "name");
                    SendReply(netuser, string.Format("{0} - {1} - {2}", cachedStructure.gameObject.name, cachedMaster.ownerID.ToString(), name == null ? "UnknownPlayer" : name.ToString()));
                    return;
                }
            }
            else 
            {

                cachedDeployable = cachedRaycast.collider.GetComponent<DeployableObject>();
                if (cachedDeployable != null)
                {
                    var name = PlayerDatabase?.Call("GetPlayerData", cachedDeployable.ownerID.ToString(), "name");
                    SendReply(netuser, string.Format("{0} - {1} - {2}", cachedDeployable.gameObject.name, cachedDeployable.ownerID.ToString(), name == null ? cachedDeployable.ownerName.ToString() : name.ToString()));
                    if(cachedDeployable.GetComponent<PasswordLockableObject>())
                    {
                        SendReply(netuser, "Players with access to this door:");
                        int count = 0;
                        foreach(ulong userid in (HashSet<ulong>)accessUsers.GetValue(cachedDeployable.GetComponent<PasswordLockableObject>()))
                        {
                            count++;
                            name = PlayerDatabase?.Call("GetPlayerData", userid.ToString(), "name");
                            SendReply(netuser, string.Format("{0} - {1}", userid.ToString(), name == null ? "Unknown" : name.ToString()));
                        }
                        if(count == 0) SendReply(netuser, "No one exept the owner.");
                    }
                    return;
                }
            }
            SendReply(netuser, string.Format("Can't prod what you are looking at: {0}",cachedRaycast.collider.gameObject.name));
        }
        void SendHelpText(NetUser netuser)
        {
            if (hasAccess(netuser)) SendReply(netuser, "Prod Command: /prod => know who owns a structure or deployable");
        }
    }
}