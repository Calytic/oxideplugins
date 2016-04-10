// Reference: RustBuild
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("Portgun", "Reneb", "2.0.1")]
    class Portgun : RustPlugin
    {
        private FieldInfo serverinput;
        private int collLayers;
        
        void Loaded()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            if (!permission.PermissionExists(permissionPortgun)) permission.RegisterPermission(permissionPortgun, this);
        }
        void OnServerInitialized()
        {
            collLayers = UnityEngine.LayerMask.GetMask(new string[] { "Construction", "Deployed", "Tree", "Terrain", "Resource", "World", "Water", "Default" });
        }

        private static string permissionPortgun = "canportgun";
        private static int authLevel = 2;
         
        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<int>("Configure - Admin Level", ref authLevel);
            CheckCfg<string>("Configure - Permission", ref permissionPortgun);
            SaveConfig();
        }

        bool hasAccess( BasePlayer player )
        {
            if (player.net.connection.authLevel >= authLevel) return true;
            return permission.UserHasPermission(player.userID.ToString(), permissionPortgun);
        }

        [ChatCommand("p")]
        void cmdChatPortgun(BasePlayer player, string command, string[] args)
        {
            if(!hasAccess(player))
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            RaycastHit hitInfo;
            if(!UnityEngine.Physics.Raycast(player.eyes.HeadRay(), out hitInfo, Mathf.Infinity, collLayers))
            {
                SendReply(player, "Couldn't find a destination");
                return;
            }
            Debug.Log(hitInfo.point.ToString());
            Debug.Log(hitInfo.distance.ToString());
            player.MovePosition(hitInfo.point);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", hitInfo.point, null, null, null, null);
        }
    }
}