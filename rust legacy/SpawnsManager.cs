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
    [Info("Spawns Manager", "Reneb", "1.0.2")]
    class SpawnsManager : RustLegacyPlugin
    {
        [PluginReference]
        Plugin Spawns;

        public RustServerManagement management;

        bool validData = false;
        object cachedSpawn;
        /////////////////////////////
        // Config Management
        /////////////////////////////

        public static List<object> SpawnLists;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var) 
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Loaded()
        {
            management = RustServerManagement.Get();
        }
        void OnServerInitialized()
        {
            SpawnLists = LoadDefaultSpawnlists();
            CheckCfg<List<object>>("Spawns List", ref SpawnLists);
            SaveConfig();
            PatchNewSpawns();
        }
        static List<object> LoadDefaultSpawnlists()
        {
            var tblspawnlists = new List<object>();
            var spawnlists = SpawnManager._spawnPoints as SpawnManager.SpawnData[];
            foreach (SpawnManager.SpawnData spawnpoint in spawnlists)
            {
                var spawnlist = new Dictionary<string, object>();
                spawnlist.Add("pos_x", spawnpoint.pos.x.ToString());
                spawnlist.Add("pos_y", spawnpoint.pos.y.ToString());
                spawnlist.Add("pos_z", spawnpoint.pos.z.ToString());
                spawnlist.Add("rot_x", spawnpoint.rot.x.ToString());
                spawnlist.Add("rot_y", spawnpoint.rot.y.ToString());
                spawnlist.Add("rot_z", spawnpoint.rot.z.ToString());
                spawnlist.Add("rot_w", spawnpoint.rot.w.ToString());
                tblspawnlists.Add(spawnlist);
            }
            return tblspawnlists;
        }

        void PatchNewSpawns()
        {
            int cnt = 0;
            var newspawnlist = new SpawnManager.SpawnData[SpawnLists.Count];
            for (int i = 0; i < SpawnLists.Count; i++)
            {
                var spawnpoint = SpawnLists[i] as Dictionary<string, object>;
                newspawnlist[i].pos = new Vector3(Convert.ToSingle(spawnpoint["pos_x"]), Convert.ToSingle(spawnpoint["pos_y"]), Convert.ToSingle(spawnpoint["pos_z"]));
                newspawnlist[i].rot = new UnityEngine.Quaternion(Convert.ToSingle(spawnpoint["rot_x"]), Convert.ToSingle(spawnpoint["rot_y"]), Convert.ToSingle(spawnpoint["rot_z"]), Convert.ToSingle(spawnpoint["rot_w"]));
            }
            SpawnManager._spawnPoints = newspawnlist;

            Puts(string.Format("{0} custom spawn points were loaded!", SpawnLists.Count.ToString()));
        }
        [ChatCommand("spawnsmanager")]
        void cmdChatSpawnsManager(NetUser netuser, string command, string[] args)
        {
            if(args.Length == 0)
            {
                SendReply(netuser, string.Format("You currently have {0} spawn points loaded", ((SpawnManager.SpawnData[])SpawnManager._spawnPoints).Length));
                return;
            }
            if(Spawns == null)
            {
                SendReply(netuser, "You don't have the Spawns Database plugin to load spawns from it");
                return;
            }
            var loadFile = Interface.GetMod().DataFileSystem.GetDatafile(args[0]);
            if (loadFile["1"] == null)
            {
                SendReply(netuser, "This file doesn't exist");
                return;
            }
            var newspawns = new List<object>();
            foreach (KeyValuePair<string, object> pair in loadFile)
            {
                var currentvalue = pair.Value as Dictionary<string, object>;
                var spawnlist = new Dictionary<string, object>();
                spawnlist.Add("pos_x", currentvalue["x"].ToString());
                spawnlist.Add("pos_y", currentvalue["y"].ToString());
                spawnlist.Add("pos_z", currentvalue["z"].ToString());
                spawnlist.Add("rot_x", "0");
                spawnlist.Add("rot_y", "0");
                spawnlist.Add("rot_z", "0");
                spawnlist.Add("rot_w", "1");
                newspawns.Add(spawnlist);
            }
            SpawnLists = newspawns;
            PatchNewSpawns();
            SaveConfig();
            SendReply(netuser, string.Format("You now have {0} spawn points loaded", ((SpawnManager.SpawnData[])SpawnManager._spawnPoints).Length));
        }
    }
}
