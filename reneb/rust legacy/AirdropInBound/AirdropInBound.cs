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
    [Info("AirdropInBound", "Reneb based on FeuerSturm91's Airdrop In Bound", "1.0.0")]
    class AirdropInBound : RustLegacyPlugin
    {

        [PluginReference]
        Plugin Location;

        static string wrongarguments = "Wrong arguments, or target player doesn't exist";
        static bool useLocation = true;
        static bool showDistance = true;
        static bool showDirection = true;

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
            CheckCfg<string>("Messages: Wrong Arguments", ref wrongarguments);
            CheckCfg<bool>("Settings: Use Location Plugin http://oxidemod.org/plugins/location.937/ ", ref useLocation);
            CheckCfg<bool>("Settings: Show distance to airdrop", ref showDistance);
            CheckCfg<bool>("Settings: Show direction to airdrop", ref showDirection);
            SaveConfig();
        }

        void OnAirdrop(Vector3 targetposition)
        {
            string message = "Airdrop inbound";
            string location = string.Empty;
            string distance = string.Empty;
            string direction = string.Empty;
            if(useLocation && Location != null)
            {
                location = Location.Call("FindLocationName", targetposition) as string;
                if(location != null)
                {
                    location = " near " + location;
                }
            }
            message += location + "!";
            foreach(PlayerClient player in PlayerClient.All)
            {
                var currentmessage = message.ToString();
                if (showDistance)
                {
                    distance = "approx. " + Math.Ceiling(Math.Abs(Vector3.Distance(targetposition, player.lastKnownPosition))).ToString() + "m";
                }
                if (showDirection)
                {
                    direction = GetDirection(targetposition, player.lastKnownPosition);
                }
                if(showDistance || showDirection)
                {
                    currentmessage = string.Format("{0} ({1} {2} of your position)", currentmessage, distance, direction);
                }
                ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add AntiCheat " + Facepunch.Utility.String.QuoteSafe(currentmessage));
            }

        }
        string GetDirection(Vector3 targetposition, Vector3 playerposition)
        {
            if (Vector3.Distance(targetposition, playerposition) < 10) return string.Empty;
            string northsouth;
            string westeast;
            string direction = string.Empty;
            if (playerposition.x < targetposition.x)
                northsouth = "South";
            else
                northsouth = "North";
            if (playerposition.z < targetposition.z)
                westeast = "East";
            else
                westeast = "West";
            var diffx = Math.Abs(playerposition.x - targetposition.x);
            var diffz = Math.Abs(playerposition.z - targetposition.z);
            if (diffx / diffz <= 0.5) direction = westeast;
            if (diffx / diffz > 0.5 && diffx / diffz < 1.5) direction = northsouth + "-" + westeast;
            if (diffx / diffz >= 1.5) direction = northsouth;
            return direction;
        }
    }
}