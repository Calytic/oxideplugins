using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CustomSpawnPoints", "Reneb", "1.0.3", ResourceId = 1076)]
    public class CustomSpawnPoints : RustPlugin
    {

        [PluginReference]
        Plugin Spawns;

        bool activated = false;



        /////////////////////////////////////////
        // Oxide Hooks
        /////////////////////////////////////////
        BasePlayer.SpawnPoint OnFindSpawnPoint()
        {
            if (!activated) return null;
            var targetpos = Spawns.Call("GetRandomSpawn", new object[] { spawnsname });
            if (targetpos is string)
                return null;
            BasePlayer.SpawnPoint point = new BasePlayer.SpawnPoint();
            point.pos = (Vector3)targetpos;
            point.rot = new Quaternion(1f,0f,0f,0f);
            RaycastHit hit;
            if (checkDown != 0f)
            {
                if (Physics.Raycast(new Ray(point.pos + vectorUp, Vector3.down), out hit, checkDown, -1063190271))
                {
                    point.pos = hit.point;
                }
            }
            return point;
        }

        /////////////////////////////////////////
        // Config Manager
        /////////////////////////////////////////
        private static string spawnsname = "spawnfile";
        static string MessagesPermissionsNotAllowed = "You are not allowed to use this command";
        static string CheckUp = "1.0";
        static string CheckDown = "1.0";
        Vector3 vectorUp = new Vector3(0f, 1f, 0f);
        float checkDown = 2f;

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
            CheckCfg<string>("Settings - Spawn Database Name", ref spawnsname);
            CheckCfg<string>("Messages - Permissions - Not Allowed", ref MessagesPermissionsNotAllowed);
            CheckCfg<string>("Spawn Fix - Check from Xm up", ref CheckUp);
            CheckCfg<string>("Spawn Fix - Check to Xm down", ref CheckDown);
            vectorUp = new Vector3(0f, Convert.ToSingle(CheckUp), 0f);
            checkDown = Convert.ToSingle(CheckUp) + Convert.ToSingle(CheckDown);
            SaveConfig();
        }

        void OnServerInitialized()
        {
            LoadSpawns();
        }

        void LoadSpawns()
        {
            activated = false;
            object success = Spawns.Call("GetSpawnsCount", new object[] { spawnsname });
            if (success is string)
            {
                Debug.Log("Custom Spawn Points - ERROR:" + (string)success);
                return;
            }
            int count = 0;
            if (!int.TryParse(success.ToString(), out count))
            {
                Debug.Log(string.Format("Custom Spawn Points - ERROR: {0} is not a valid spawnfile",spawnsname));
                return;
            }
            if (count < 1)
            {
                Debug.Log("Custom Spawn Points - ERROR: You must have at least 1 spawn in your spawnfile");
                return;
            }
            Debug.Log(string.Format("Custom Spawn Points: {0} spawn points loaded", count.ToString()));
            activated = true;
        }

        bool hasAccess(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 2)
                {
                    SendReply(arg, MessagesPermissionsNotAllowed);
                    return false;
                }
            }
            return true;
        }
        [ConsoleCommand("spawns.config")]
        void ccmdSpawnFile(ConsoleSystem.Arg arg)
        {
            if (!hasAccess(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "spawns.config SPAWNFILENAME");
                return;
            }
            object success = Spawns.Call("GetSpawnsCount", new object[] { arg.Args[0] });
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            int count = 0;
            if(!int.TryParse(success.ToString(), out count))
            {
                SendReply(arg, "This is not a valid spawnfile");
                return;
            }
            if (count < 1)
            {
                SendReply(arg, "You must have at least 1 spawn in your spawnfile");
                return;
            }
            SendReply(arg,string.Format("{0} spawns loaded", count.ToString()));
            spawnsname = arg.Args[0];
            SaveConfig();
            LoadSpawns();
        }
    }
}
