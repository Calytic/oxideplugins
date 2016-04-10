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
    [Info("Location", "Reneb", "1.0.1", ResourceId = 937)]
    class Location : RustLegacyPlugin
    {
        [PluginReference]
        Plugin Share;

        public float nearest;
        public Vector2 nearestVector;
        public float cachedDistance;
        public Vector3 cachedVector3;
        public NetUser cachedUser;
        public object cachedReturn;

        public static string notAllowed = "You are not allowed to use this command.";
        public static string notAllowedPlayer = "You are not allowed to get the target's location";
        public static string locationMessage = "{0} is located @ {1} {2} {3} - near {4}";
        public static string helpMessage = "Show your current location: /location";
        public static bool allowLocations = true;
        public static bool allowTargetLocations = true;
        public static List<object[]> locationsList = GetLocations();

        public static Dictionary<Vector2, string> locList;

        void Loaded()
        {
            if (!permission.PermissionExists("admin")) permission.RegisterPermission("admin", this);
            locList = GetLocList();
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
            CheckCfg<string>("Messages: Help", ref helpMessage);
            CheckCfg<string>("Messages: Not Allowed To Target This Player", ref notAllowedPlayer);
            CheckCfg<string>("Messages: Location Message, 0 is the playername, 1 2 3 are x, y, z coordinates, and 4 is the name of the location", ref locationMessage);
            CheckCfg<bool>("Settings: Allow Chat Command For Players", ref allowLocations);
            CheckCfg<bool>("Settings: Allow players to target other players (Share Plugin needed)", ref allowTargetLocations);
            CheckCfg<List<object[]>>("Locations: List", ref locationsList);
            SaveConfig();
        }
        
        bool canTarget(NetUser netuser, NetUser targetuser)
        {
            if (netuser.CanAdmin())
                return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "admin")) return true;
            if (!allowTargetLocations)
                return false;
            cachedReturn = Interface.CallHook("isSharing", targetuser.playerClient.userID.ToString(), netuser.playerClient.userID.ToString());
            if (cachedReturn is bool) return (bool)cachedReturn;
            return false;
        }
        bool hasAccess(NetUser netuser)
        {
            if (allowLocations)
                return true;
            if (netuser.CanAdmin())
                return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "admin");
        }
        [ChatCommand("location")]
        void cmdChatLocation(NetUser netuser, string command, string[] args)
        {
            if(!hasAccess(netuser)) { SendReply(netuser, notAllowed); return; }
            cachedUser = netuser;
            if (args.Length > 0)
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if(targetuser != null)
                {
                    if(!canTarget(netuser,targetuser)) { SendReply(netuser, notAllowedPlayer); return; }
                    cachedUser = targetuser;
                }
            }
            cachedVector3 = cachedUser.playerClient.lastKnownPosition;
            SendReply(netuser, string.Format(locationMessage, cachedUser.playerClient.userName.ToString(), Mathf.Ceil(cachedVector3.x).ToString(), Mathf.Ceil(cachedVector3.y).ToString(), Mathf.Ceil(cachedVector3.z).ToString(), FindLocationName(cachedVector3)));
        }
        
        string FindLocationName(Vector3 position)
        {
            nearest = 999999999f;
            Vector2 currentPos = new Vector2(position.x, position.z);
            foreach(KeyValuePair<Vector2, string> pair in locList)
            {
                cachedDistance = Vector2.Distance(currentPos, pair.Key);
                if(cachedDistance < nearest)
                {
                    nearestVector = pair.Key;
                    nearest = cachedDistance;
                }
            }
            return locList[nearestVector];
        }
        static Dictionary<Vector2, string> GetLocList()
        {
            var newlist = new Dictionary<Vector2, string>();
            object[] cachedArr;
            foreach (object arr in locationsList)
            {
                cachedArr = arr as object[];
                newlist.Add(new Vector3(Convert.ToSingle(cachedArr[1]), Convert.ToSingle(cachedArr[2])), cachedArr[0].ToString());
            }
            return newlist;
        }
        static List<object[]> GetLocations()
        {
            var locationslist = new List<object[]>();
            locationslist.Add(new object[] { "Hacker Valley South", 5907, -1848 });
            locationslist.Add(new object[] { "Hacker Mountain South", 5268, -1961 });
            locationslist.Add(new object[] { "Hacker Valley Middle", 5268, -2700 });
            locationslist.Add(new object[] { "Hacker Mountain North", 4529, -2274 });
            locationslist.Add(new object[] { "Hacker Valley North", 4416, -2813 });
            locationslist.Add(new object[] { "Wasteland North", 3208, -4191 });
            locationslist.Add(new object[] { "Wasteland South", 6433, -2374 });
            locationslist.Add(new object[] { "Wasteland East", 4942, -2061 });
            locationslist.Add(new object[] { "Wasteland West", 3827, -5682 });
            locationslist.Add(new object[] { "Sweden", 3677, -4617 });
            locationslist.Add(new object[] { "Everust Mountain", 5005, -3226 });
            locationslist.Add(new object[] { "North Everust Mountain", 4316, -3439 });
            locationslist.Add(new object[] { "South Everust Mountain", 5907, -2700 });
            locationslist.Add(new object[] { "Metal Valley", 6825, -3038 });
            locationslist.Add(new object[] { "Metal Mountain", 7185, -3339 });
            locationslist.Add(new object[] { "Metal Hill", 5055, -5256 });
            locationslist.Add(new object[] { "Resource Mountain", 5268, -3665 });
            locationslist.Add(new object[] { "Resource Valley", 5531, -3552 });
            locationslist.Add(new object[] { "Resource Hole", 6942, -3502 });
            locationslist.Add(new object[] { "Resource Road", 6659, -3527 });
            locationslist.Add(new object[] { "Beach", 5494, -5770 });
            locationslist.Add(new object[] { "Beach Mountain", 5108, -5875 });
            locationslist.Add(new object[] { "Coast Valley", 5501, -5286 });
            locationslist.Add(new object[] { "Coast Mountain", 5750, -4677 });
            locationslist.Add(new object[] { "Coast Resource", 6120, -4930 });
            locationslist.Add(new object[] { "Secret Mountain", 6709, -4730 });
            locationslist.Add(new object[] { "Secret Valley", 7085, -4617 });
            locationslist.Add(new object[] { "Factory Radtown", 6446, -4667 });
            locationslist.Add(new object[] { "Small Radtown", 6120, -3452 });
            locationslist.Add(new object[] { "Big Radtown", 5218, -4800 });
            locationslist.Add(new object[] { "Hangar", 6809, -4304 });
            locationslist.Add(new object[] { "Tanks", 6859, -3865 });
            locationslist.Add(new object[] { "Civilian Forest", 6659, -4028 });
            locationslist.Add(new object[] { "Civilian Mountain", 6346, -4028 });
            locationslist.Add(new object[] { "Civilian Road", 6120, -4404 });
            locationslist.Add(new object[] { "Ballzack Mountain", 4316, -5682 });
            locationslist.Add(new object[] { "Ballzack Valley", 4720, -5660 });
            locationslist.Add(new object[] { "Spain Valley", 4742, -5143 });
            locationslist.Add(new object[] { "Portugal Mountain", 4203, -4570 });
            locationslist.Add(new object[] { "Portugal", 4579, -4637 });
            locationslist.Add(new object[] { "Lone Tree Mountain", 4842, -4354 });
            locationslist.Add(new object[] { "Forest", 5368, -4434 });
            locationslist.Add(new object[] { "Rad-Town Valley", 5907, -3400 });
            locationslist.Add(new object[] { "Next Valley", 4955, -3900 });
            locationslist.Add(new object[] { "Silk Valley", 5674, -4048 });
            locationslist.Add(new object[] { "French Valley", 5995, -3978 });
            locationslist.Add(new object[] { "Ecko Valley", 7085, -3815 });
            locationslist.Add(new object[] { "Ecko Mountain", 7348, -4100 });
            locationslist.Add(new object[] { "Zombie Hill", 6396, -3428 });
            return locationslist;
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser)) return;
            SendReply(netuser, helpMessage);
        }
    }
}