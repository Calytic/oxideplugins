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
using RustProto;

namespace Oxide.Plugins
{
    [Info("AdminDoor", "Reneb", "1.0.1", ResourceId = 932)]
    class AdminDoor : RustLegacyPlugin
    {
        public static int doorLayer;
        private static MethodInfo togglestateserver;
        private static FieldInfo doorstate;



        public static string messageActivated = "Admin Door: Activated";
        public static string messageDeactivated = "Admin Door: Deactivated";
        public static float checkTimer = 0.2f;
        public static float checkRadius = 3f;

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
            CheckCfg<string>("Messages: Activated", ref messageActivated);
            CheckCfg<string>("Messages: Deactivated", ref messageDeactivated);
            CheckCfg<float>("Settings: Check for Doors every X seconds", ref checkTimer);
            CheckCfg<float>("Settings: Check for Doors in a X meter radius", ref checkRadius);

            SaveConfig();
        }

        public class AdminDoorHandler : MonoBehaviour
        {
            public PlayerClient playerclient;
            public float lastTick = Time.realtimeSinceStartup;
            public List<BasicDoor> doors = new List<BasicDoor>();
            public List<BasicDoor> toremovedoors = new List<BasicDoor>();
            public BasicDoor cachedDoor;

            void Awake()
            {
                playerclient = GetComponent<PlayerClient>();
            }

            void FixedUpdate()
            {
                if (Time.realtimeSinceStartup - lastTick > 0.2f)
                {
                    toremovedoors.Clear();
                    foreach (BasicDoor door in doors)
                        toremovedoors.Add(door);
                    foreach (Collider collider in Physics.OverlapSphere(playerclient.lastKnownPosition, 3f, doorLayer))
                    {
                        if (!collider.GetComponent<BasicDoor>()) continue;
                        cachedDoor = collider.GetComponent<BasicDoor>();
                        if (toremovedoors.Contains(cachedDoor)) toremovedoors.Remove(cachedDoor);
                        if (!doors.Contains(cachedDoor)) OnDoorEntry(cachedDoor);
                    }
                    foreach (BasicDoor door in toremovedoors)
                        OnDoorLeave(door);
                    lastTick = Time.realtimeSinceStartup;
                }
            }
            void OnDoorLeave(BasicDoor door)
            {
                doors.Remove(door);
                if(doorstate.GetValue(door).ToString() == "Opened" || doorstate.GetValue(door).ToString() == "Opening")
                    togglestateserver.Invoke(door, new object[] { playerclient.lastKnownPosition, NetCull.timeInMillis, null });
            }
            void OnDoorEntry(BasicDoor door)
            {
                doors.Add(door);
                if (doorstate.GetValue(door).ToString() == "Closed" || doorstate.GetValue(door).ToString() == "Closing")
                    togglestateserver.Invoke(door, new object[] { playerclient.lastKnownPosition, NetCull.timeInMillis, null });
            } 
            void OnDestroy()
            {
                foreach(BasicDoor door in doors)
                {
                    if (doorstate.GetValue(door).ToString() == "Opened" || doorstate.GetValue(door).ToString() == "Opening")
                        togglestateserver.Invoke(door, new object[] { playerclient.lastKnownPosition, NetCull.timeInMillis, null });
                }
            }
        }

        void Loaded() {
            foreach(MethodInfo methodinfo in typeof(BasicDoor).GetMethods((BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)))
            {
                if(methodinfo.Name == "ToggleStateServer")
                {
                    if(methodinfo.GetParameters().Length == 3)
                    {
                        togglestateserver = methodinfo;
                    }
                }
            }
            doorstate = typeof(BasicDoor).GetField("state", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            if (!permission.PermissionExists("candoor")) permission.RegisterPermission("candoor", this);
            doorLayer = LayerMask.GetMask(new string[] { "Mechanical" });
        }

        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(AdminDoorHandler));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin())
                return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "candoor");
        }


        [ChatCommand("admindoor")]
        void cmdChatAdminDoor(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser)) { SendReply(netuser, "You don't have access to this command"); return; }

            if (netuser.playerClient.GetComponent<AdminDoorHandler>())
            {
                SendReply(netuser, "Admin door deactivated.");
                GameObject.Destroy(netuser.playerClient.GetComponent<AdminDoorHandler>());
            }
            else
            {
                SendReply(netuser, "Admin door activated.");
                netuser.playerClient.gameObject.AddComponent<AdminDoorHandler>();
            }
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser)) return;
            SendReply(netuser, "Admin Auto Door: /admindoor");
        }
    }
}
