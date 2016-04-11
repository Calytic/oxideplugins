
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

/*
    This is my first plugin, and I'm not very good with C# - so this is probably horrible and I apologize now.  Thanks!
*/

namespace Oxide.Plugins
{
    [Info("PersonalBeacon", "Mordenak", "1.0.7", ResourceId = 1000)]
    class PersonalBeacon : RustPlugin
    {
        // To be moved into config file at some point...
        static int beaconHeight = 500;
        static int arrowSize = 10;
        static float beaconRefresh = 2f;

        static Core.Configuration.DynamicConfigFile BeaconData;
        static Dictionary<string, bool> userBeacons = new Dictionary<string, bool>();
        static Dictionary<string, Oxide.Plugins.Timer> userBeaconTimers = new Dictionary<string, Oxide.Plugins.Timer>();

        static Dictionary<string, bool> adminBeacons = new Dictionary<string, bool>();
        static Dictionary<string, Oxide.Plugins.Timer> adminTimers = new Dictionary<string, Oxide.Plugins.Timer>();

        static Dictionary<string, bool> adminBeaconIsOn = new Dictionary<string, bool>();
        static Dictionary<string, Oxide.Plugins.Timer> adminBeaconTimers = new Dictionary<string, Oxide.Plugins.Timer>();

        void Loaded()
        {
            LoadBeaconData();
        }

        void Unload()
        {
            SaveBeaconData();
            //CleanUpBeacons();
        }

        void OnServerSave()
        {
            SaveBeaconData();
        }

        private void SaveBeaconData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("PersonalBeacon_Data");
        }
        private void LoadBeaconData()
        {
            //Debug.Log("Loading data...");
            try
            {
                //BeaconData = Interface.GetMod().DataFileSystem.ReadObject<Oxide.Core.Configuration.DynamicConfigFile>("PersonalBeacon_Data");
                BeaconData = Interface.GetMod().DataFileSystem.GetDatafile("PersonalBeacon_Data");
            }
            catch
            {
                Debug.Log("Failed to load datafile.");
            }
            //Debug.Log("Data should be loaded.");
        }
  
        void DisplayBeacon(BasePlayer player)
        {
            var playerId = player.userID.ToString();
            // player has disconnected
            if (!player.IsConnected())
            {
                Debug.Log("Cleaning up disconnected player timer.");
                userBeacons[playerId] = false;
                userBeaconTimers[playerId].Destroy();
                return;
            }

            if (BeaconData == null)
            {
                Debug.Log("BeaconData wasn't loaded before use, forcing load.");
                LoadBeaconData();
            }
            if (BeaconData[playerId] == null)
            {
                Debug.Log(string.Format("Player [{0}] -- BeaconData is corrupt.", playerId) );
                userBeacons[playerId] = false;
                userBeaconTimers[playerId].Destroy();
                return;
                /*
                foreach (var playerbeacons in BeaconData)
                {
                    Debug.Log(playerbeacons.ToString());
                }
                */
            }

            var table = BeaconData[playerId] as Dictionary<string, object>;
            //var beaconGround = new Vector3((float)table["x"], (float)table["y"], (float)table["z"]);
            var beaconGround = new Vector3();
            // Necessary evil here
            beaconGround.x = float.Parse(table["x"].ToString());
            beaconGround.y = float.Parse(table["y"].ToString());
            beaconGround.z = float.Parse(table["z"].ToString());

            var beaconSky = beaconGround;
            beaconSky.y = beaconSky.y + beaconHeight;

            player.SendConsoleCommand("ddraw.arrow", beaconRefresh, UnityEngine.Color.red, beaconGround, beaconSky, arrowSize);
        }

        [ChatCommand("setwp")]
        void cmdSetBeacon(BasePlayer player, string command, string[] args)
        {
            Dictionary<string, object> coords = new Dictionary<string, object>();
            coords.Add("x", player.transform.position.x);
            coords.Add("y", player.transform.position.y);
            coords.Add("z", player.transform.position.z);

            if (BeaconData == null)
            {
                Debug.Log("BeaconData wasn't loaded before use, forcing load.");
                LoadBeaconData();
            }

            BeaconData[player.userID.ToString()] = coords;

            var newVals = BeaconData[player.userID.ToString()] as Dictionary<string, object>;

            SendReply(player, string.Format("Beacon set to: x: {0}, y: {1}, z: {2}", newVals["x"], newVals["y"], newVals["z"]) );
        }

        [ChatCommand("wp")]
        void cmdBeacon(BasePlayer player, string command, string[] args)
        {

            if (BeaconData == null)
            {
                Debug.Log("BeaconData wasn't loaded before use, forcing load.");
                LoadBeaconData();
            }

            var playerId = player.userID.ToString();

            if (BeaconData[playerId] == null)
            {
                SendReply(player, "You have not set a waypoint yet.  Please run /setwp to create a waypoint.");
                Debug.Log(string.Format("Player [{0}] -- BeaconData is corrupt or non-existent.", playerId) );
                return;
                /*
                foreach (var playerbeacons in BeaconData)
                {
                    Debug.Log(playerbeacons.ToString());
                }
                */
            }
            if (!userBeacons.ContainsKey(playerId)) userBeacons.Add(playerId, false);

            // maybe unecessary
            if (userBeacons[playerId] == null) userBeacons[playerId] = false;

            if (userBeacons[playerId] == false)
            {
                DisplayBeacon(player); // display immediately
                userBeaconTimers[playerId] = timer.Repeat(beaconRefresh, 0, delegate() { DisplayBeacon(player); } );
                SendReply(player, "Beacon on.");
                userBeacons[playerId] = true;
            }
            else
            {
                userBeaconTimers[playerId].Destroy();
                SendReply(player, "Beacon off.");
                userBeacons[playerId] = false;
            }
        }

        // Admin commands:

        [ChatCommand("wpadmin")]
        void cmdAdminWaypoint(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;

            // set a wp at the current location
            var currLocation = player.transform.position;

            var playerId = player.userID.ToString();

            if (!adminBeaconIsOn.ContainsKey(playerId)) adminBeaconIsOn.Add(playerId, false);
            if (adminBeaconIsOn[playerId] == null) adminBeaconIsOn[playerId] = false;

            //var repeatBeacon = new Dictionary<string, Oxide.Plugins.Timer>();

            if (adminBeaconIsOn[playerId] == false)
            {
                SendReply(player, "Sending Admin Waypoint to all players.");
                adminBeaconTimers[playerId] = timer.Repeat(beaconRefresh, 0, delegate() {
                    var beaconGround = currLocation;

                    var beaconSky = beaconGround;
                    beaconSky.y = beaconSky.y + beaconHeight;
                    ConsoleSystem.Broadcast("ddraw.arrow", beaconRefresh, UnityEngine.Color.green, beaconGround, beaconSky, arrowSize);
                } );
                adminBeaconIsOn[playerId] = true;
            }
            else
            {
                SendReply(player, "Removing the Admin Waypoint.");
                foreach (var adbeacontimers in adminBeaconTimers)
                {
                    adbeacontimers.Value.Destroy();
                }
                adminBeaconIsOn[playerId] = false;
            }
        }

        [ChatCommand("wpcount")]
        void cmdCountBeacons(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            int wpCount = 0;
            foreach (var playerbeacons in BeaconData)
            {
                //Debug.Log(playerbeacons.ToString());
                wpCount = wpCount + 1;
            }
            //Debug.Log(string.Format("Found {0} waypoints.", wpCount) );
            SendReply(player, string.Format("Tracking {0} waypoints.", wpCount) );
        }

        [ChatCommand("wpshowall")]
        void cmdShowAllBeacons(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 0) return;
            var playerId = player.userID.ToString();
            if (!adminBeacons.ContainsKey(playerId)) adminBeacons.Add(playerId, false);
            if (adminBeacons[playerId] == null) adminBeacons[playerId] = false;
            if (adminBeacons[playerId] == false)
            {
                foreach (var playerbeacons in BeaconData)
                {
                    //Debug.Log(string.Format("Looking for beacon for player: {0}", playerbeacons.Key) );

                    var targetId = playerbeacons.Key;

                    var table = BeaconData[targetId] as Dictionary<string, object>;
                    //var beaconGround = new Vector3((float)table["x"], (float)table["y"], (float)table["z"]);
                    var beaconGround = new Vector3();
                    // Necessary evil here
                    beaconGround.x = float.Parse(table["x"].ToString());
                    beaconGround.y = float.Parse(table["y"].ToString());
                    beaconGround.z = float.Parse(table["z"].ToString());

                    var beaconSky = beaconGround;
                    beaconSky.y = beaconSky.y + beaconHeight;
                    player.SendConsoleCommand("ddraw.arrow", beaconRefresh, UnityEngine.Color.red, beaconGround, beaconSky, arrowSize);
                    adminTimers[targetId] = timer.Repeat(beaconRefresh, 0, delegate() { player.SendConsoleCommand("ddraw.arrow", beaconRefresh, UnityEngine.Color.red, beaconGround, beaconSky, arrowSize);; } );

                    //player.SendConsoleCommand("ddraw.arrow", 10f, UnityEngine.Color.red, beaconGround, beaconSky, 10);
                }
                SendReply(player, "All beacons on.");
                adminBeacons[playerId] = true;
            }
            else
            {
                foreach (var playerdata in BeaconData)
                {
                    adminTimers[playerdata.Key].Destroy();
                }
                //adminTimers[targetId].Destroy();
                SendReply(player, "All beacons off.");
                adminBeacons[playerId] = false;
            }
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player) 
        {
            var helpString = "<color=#11FF22>PersonalBeacon</color>:\n/setwp - Sets the beacon to the current location.\n/wp - Toggles beacon on or off.";
            player.ChatMessage(helpString.TrimEnd());
        }
    }
}