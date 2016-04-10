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
    [Info("DoorShare", "Reneb", "1.0.3")]
    class DoorShare : RustLegacyPlugin
    {
        object cachedValue;

        [PluginReference]
        Plugin Share;

        MethodInfo toggledoor;

        void Loaded()
        {
            foreach (MethodInfo methinfo in typeof(BasicDoor).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                if (methinfo.Name == "ToggleStateServer" && methinfo.GetParameters().Length == 3)
                    toggledoor = methinfo;
            }
        }

        object OnDoorToggle(BasicDoor door, ulong timestamp, Controllable controllable)
        {
            if (controllable == null) return null;
            if (controllable.playerClient == null) return null;
            if (door.GetComponent<DeployableObject>() == null) return null;
            cachedValue = Interface.CallHook("isSharing", door.GetComponent<DeployableObject>().ownerID.ToString(), controllable.playerClient.userID.ToString());
            if (cachedValue is bool && (bool)cachedValue) return toggledoor.Invoke(door, new object[] { controllable.playerClient.lastKnownPosition, timestamp, null });
            return null;
        } 
    }
}