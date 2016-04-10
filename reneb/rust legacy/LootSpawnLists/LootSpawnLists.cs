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
    [Info("LootSpawnLists", "Reneb from thomasfn", "2.0.2")]
    class LootSpawnLists : RustLegacyPlugin
    {

        public static Dictionary<string, object> SpawnLists;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void OnServerInitialized()
        {
            SpawnLists = LoadDefaultSpawnlists();
            CheckCfg<Dictionary<string, object>>("Spawnlists", ref SpawnLists);
            SaveConfig();
            PatchNewSpawnlists();
        }
        static Dictionary<string, object> LoadDefaultSpawnlists()
        {
            var tblspawnlists = new Dictionary<string, object>();
            var spawnlists = DatablockDictionary._lootSpawnLists as Dictionary<string,LootSpawnList>;
            foreach (KeyValuePair<string, LootSpawnList> pair in spawnlists)
            {
                var spawnlist = new Dictionary<string, object>();
                spawnlist.Add("min", pair.Value.minPackagesToSpawn);
                spawnlist.Add("max", pair.Value.maxPackagesToSpawn);
                spawnlist.Add("nodupes", pair.Value.noDuplicates);
                spawnlist.Add("oneofeach", pair.Value.spawnOneOfEach);
                var packages = new List<object>();
				foreach(LootSpawnList.LootWeightedEntry entry in pair.Value.LootPackages)
                {
					var tblentry = new Dictionary<string, object>();
                    if (!entry.obj)
                        continue;
                    else
                        tblentry.Add("object", entry.obj.name);
                    tblentry.Add("weight", entry.weight);
                    tblentry.Add("min", entry.amountMin);
                    tblentry.Add("max", entry.amountMax);
                    packages.Add(tblentry);
                }
                spawnlist.Add("packages", packages);
                tblspawnlists.Add(pair.Key, spawnlist);
            }
            return tblspawnlists;
        }
        void PatchNewSpawnlists()
        {
            int cnt = 0;
            var spawnlistobjects = new Dictionary<string, LootSpawnList>();
            foreach (KeyValuePair<string, object> pair in SpawnLists)
            {
                var currentspawnlist = pair.Value as Dictionary<string, object>;
                var obj = UnityEngine.ScriptableObject.CreateInstance<LootSpawnList>();
                obj.minPackagesToSpawn = Convert.ToInt32(currentspawnlist["min"]);
                obj.maxPackagesToSpawn = Convert.ToInt32(currentspawnlist["max"]);
                obj.noDuplicates = Convert.ToBoolean(currentspawnlist["nodupes"]);
                obj.spawnOneOfEach = Convert.ToBoolean(currentspawnlist["oneofeach"]);
                obj.name = pair.Key;
                spawnlistobjects.Add(pair.Key, obj);
                cnt++;
            }
            foreach (KeyValuePair<string, object> pair in SpawnLists)
            {
                var entrylist = new List<LootSpawnList.LootWeightedEntry>();
                var currentspawnlist = pair.Value as Dictionary<string, object>;
                var packages = currentspawnlist["packages"] as List<object>;
                foreach (Dictionary<string, object> entry in packages)
                {
                    var entryobj = new LootSpawnList.LootWeightedEntry();
                    entryobj.amountMin = Convert.ToInt32(entry["min"]);
                    entryobj.amountMax = Convert.ToInt32(entry["max"]);
                    entryobj.weight = Convert.ToSingle(entry["weight"]);
                    if (spawnlistobjects.ContainsKey(entry["object"].ToString()))
                    {
                        entryobj.obj = spawnlistobjects[entry["object"].ToString()];
                    }
                    else
                    {
                        entryobj.obj = DatablockDictionary.GetByName(entry["object"].ToString());
                    }
                    entrylist.Add(entryobj);
                }
                spawnlistobjects[pair.Key].LootPackages = entrylist.ToArray();
            }
            var spawnlists = DatablockDictionary._lootSpawnLists;
            spawnlists.Clear();
            foreach (KeyValuePair<string, object> pair in SpawnLists)
            {
                spawnlists.Add(pair.Key, spawnlistobjects[pair.Key]);
            }
            Puts(string.Format("{0} custom loot tables were loaded!", cnt.ToString()));
        }

    }
}