using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using Oxide.Core.Plugins;
using Oxide.Core;


namespace Oxide.Plugins
{
    [Info("Cornucopia", "Deicide666ra", "1.1.4", ResourceId = 1264)]
    class Cornucopia : RustPlugin
    {
        class CornuConfig
        {
            public CornuConfig()
            {
                // Animals
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/chicken.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/horse.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/boar.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/stag.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/wolf.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Animals.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/animals/bear.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });

                // Ore nodes
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });
                Ores.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab", Min = -1, Max = -1, IgnoreIrridiated = true });

                // Silver barrels
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_barrel_1.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Brown barrels
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_barrel_2.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Oil Drums
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/oil_barrel.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Trashcans
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/loot_trash.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Trash piles (food)
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/autospawn/resource/loot/trash-pile-1.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Weapon crates
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });

                // Box crates
                Loots.Add(new CornuConfigItem { Prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab", Min = -1, Max = -1, IgnoreIrridiated = true, DeleteEmtpy = false });
            }

            // Refresh Interval in minutes
            public int RefreshMinutes= 15;

            // Apply the loot fix to prevent stacked rad town loot crates
            public bool ApplyLootFix= true;

            // Run the cycle on start
            public bool RefreshOnStart= false;

            // If true, any item that has a maximum will be prevented from spawning outside of the Cornucopia spawn cycle
            //public bool MaxSpawnBlock = true;

            public List<CornuConfigItem> Animals= new List<CornuConfigItem>();
            public List<CornuConfigItem> Ores = new List<CornuConfigItem>();
            public List<CornuConfigItem> Loots = new List<CornuConfigItem>();
        }

        class CornuConfigItem
        {
            public string Prefab;
            public int Min;
            public int Max;
            public bool IgnoreIrridiated;
            public bool DeleteEmtpy;
        }

        private CornuConfig g_config;
        private Timer g_refreshTimer;
        //private bool g_spawnBlock = true;

        void Loaded() => LoadConfigValues();
        protected override void LoadDefaultConfig()
        {
            g_config = new CornuConfig();
            Config.WriteObject(g_config, true);
            Puts("New configuration file created.");
        }

        void LoadConfigValues()
        {
            try
            {
                g_config = Config.ReadObject<CornuConfig>();
            }
            catch
            {
                Puts("Could not read config, creating new default config.");
                LoadDefaultConfig();
            }

            //g_spawnBlock = g_config.MaxSpawnBlock;
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.Append("<color=yellow>Cornucopia 1.1.4.0</color> Â· Controls resource abundance\n");
            if (player.IsAdmin())
            {
                sb.Append("  Â· ").AppendLine("<color=lime>/cdump</color> (<color=orange>cornu.dump</color>) for RCON stats");
                sb.Append("  Â· ").AppendLine("<color=lime>/cspawn</color> (<color=orange>cornu.spawn</color>) adjusts resources");
                sb.Append("  Â· ").AppendLine("<color=lime>/cfixloot</color> (<color=orange>cornu.fixloot</color>) loot box stacking fix");
                sb.Append("  Â· ").Append("<color=lime>/cpurge</color> (<color=orange>cornu.purge</color>) deletes ALL resources");
            }
            player.ChatMessage(sb.ToString());
        }

        void OnServerInitialized()
        {
            g_refreshTimer = timer.Every(g_config.RefreshMinutes * 60, OnTimer);
        }

        void OnTimer()
        {
            if (g_config.ApplyLootFix) FixLoot(null);

            //g_spawnBlock = false;
            try
            {
                MainSpawnCycle();
            }
            finally
            {
                //g_spawnBlock = g_config.MaxSpawnBlock;
            }
        }

        void Unloaded()
        {
            if (g_refreshTimer != null)
            {
                g_refreshTimer.Destroy();
                g_refreshTimer = null;
            }
        }

        Dictionary<string, int> GetCollectibles()
        {
            // wood
            // stone-1
            // metalore-2
            // sulfurore-3
            // mushroom-cluster-1
            // mushroom-cluster-2
            // mushroom-cluster-3
            // mushroom-cluster-4
            // mushroom-cluster-5
            // mushroom-cluster-6
            // hemp
            return Resources.FindObjectsOfTypeAll<CollectibleEntity>()
                .Where(c => c.isActiveAndEnabled && !c.LookupShortPrefabName().Contains("hemp") && !c.LookupShortPrefabName().Contains("mushroom"))
                .GroupBy(c => c.LookupShortPrefabName()).ToDictionary(c => c.Key, c => c.Count());
        }

        Dictionary<string, IGrouping<string, BaseEntity>> GetOreNodes()
        {
            // stone-ore
            // metal-ore
            // sulfur-ore
            return Resources.FindObjectsOfTypeAll<BaseResource>()
                .Where(c => /*c.name.StartsWith("autospawn") &&*/ c.isActiveAndEnabled)
                .Cast<BaseEntity>()
                .GroupBy(c => c.LookupShortPrefabName()).ToDictionary(c => c.Key, c => c);
        }

        Dictionary<string, IGrouping<string, BaseEntity>> GetLootContainers()
        {
            // loot_trash
            // loot_barrel_1
            // loot_barrel_2
            // crate_normal
            // crate_normal_2
            return Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled)
                .Cast<BaseEntity>()
                .GroupBy(c => c.LookupShortPrefabName()).ToDictionary(c => c.Key, c => c);
        }

        Dictionary<string, IGrouping<string, BaseEntity>> GetAnimals()
        {
            // chicken
            // horse
            // boar
            // stag
            // wolf
            // bear
            return Resources.FindObjectsOfTypeAll<BaseNPC>()
                .Where(c => c.isActiveAndEnabled)
                .Cast<BaseEntity>()
                .GroupBy(c => c.LookupShortPrefabName()).ToDictionary(c => c.Key, c => c);
        }

        void DumpSpawns(Dictionary<string, int> entities)
        {
            foreach (var t in entities)
                Puts($"{t.Key.PadRight(50)} {t.Value}");
        }

        void DumpSpawns(Dictionary<string, IGrouping<string, BaseEntity>> entities)
        {
            foreach (var t in entities)
                Puts($"{t.Key.PadRight(50)} {t.Value.Count()}");
        }

        [ConsoleCommand("cornu.dump")]
        private void DumpCommand(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Player() != null && arg.Player().IsAdmin() == false) return;
            DumpEntities();
        }

        [ConsoleCommand("cornu.spawn")]
        private void SpawnCommand(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Player() != null && arg.Player().IsAdmin() == false) return;
            MainSpawnCycle();
        }

        [ConsoleCommand("cornu.fixloot")]
        private void FixLootCommand(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Player() != null && arg.Player().IsAdmin() == false) return;
            FixLoot(null);
        }

        [ConsoleCommand("cornu.purge")]
        private void PurgeCommand(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Player() != null && arg.Player().IsAdmin() == false) return;
            Purge();
        }

        void DumpEntities()
        {
            Puts($"= COLLECTIBLES ================");
            DumpSpawns(GetCollectibles());
            Puts($"= NODES =======================");
            DumpSpawns(GetOreNodes());
            Puts($"= CONTAINERS ==================");
            DumpSpawns(GetLootContainers());
            Puts($"= ANIMALS =====================");
            DumpSpawns(GetAnimals());
        }

        Vector2 GetBoxPos(LootContainer box)
        {
            return new Vector2(box.transform.position.x, box.transform.position.z);
        }

        [ChatCommand("cpurge")]
        void cmdPurge(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                player.ChatMessage("You need to be admin to run this command, sorry buddy!");
                return;
            }
            Purge();
        }

        [ChatCommand("cfixloot")]
        void cmdFixLoot(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                player.ChatMessage("You need to be admin to run this command, sorry buddy!");
                return;
            }
            FixLoot(player);
        }

        [ChatCommand("cdump")]
        void cmdDump(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                player.ChatMessage("You need to be admin to run this command, sorry buddy!");
                return;
            }
            DumpEntities();
        }

        [ChatCommand("cspawn")]
        void cmdSpawn(BasePlayer player, string cmd, string[] args)
        {
            if (!player.IsAdmin())
            {
                player.ChatMessage("You need to be admin to run this command, sorry buddy!");
                return;
            }
            player.ChatMessage("Respawning the lost ones!");
            MainSpawnCycle();
            player.ChatMessage("... and done!");
        }

        [ChatCommand("ctest")]
        void cmdTest(BasePlayer player, string cmd, string[] args)
        {

            var toto = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled && c.LookupShortPrefabName().Contains("trash-pile"));

            Puts($"{toto.First().LookupPrefabName()}");
        }

        void SubCycle(IEnumerable<BaseEntity> entities, IEnumerable<CornuConfigItem> limits, List<CollectibleEntity> collectibles, ref bool aborted)
        {
            foreach (var spawn in limits)
            {
                if (spawn.Min == -1 && spawn.Max == -1) continue;

                var matches = entities.Where(r => r.LookupPrefabName() == spawn.Prefab);

                if (matches.Count() < spawn.Min && spawn.Min != -1)
                {
                    if (!aborted) BatchSpawn(matches.Count(), spawn.Min, spawn.Prefab, collectibles, ref aborted);
                }
                else if (matches.Count() > spawn.Max && spawn.Max != -1)
                {
                    PopulationControl(matches, spawn.Max);
                }

                var deleted = 0;
                if (spawn.DeleteEmtpy)
                {
                    foreach (var match in matches.OfType<LootContainer>())
                    {
                        if (!match.inventory.itemList.Any())
                        {
                            match.Kill(BaseNetworkable.DestroyMode.None);
                            deleted++;
                        }
                    }
                }
                if (deleted > 0) Puts($"Deleted {deleted} empty {spawn.Prefab}");
            }
        }

        void MainSpawnCycle()
        {            
            var doAnimals = g_config.Animals.Any(a => a.Min != -1 || a.Max != -1);
            var doOres = g_config.Ores.Any(a => a.Min != -1 || a.Max != -1);
            var doLoots = g_config.Loots.Any(a => a.Min != -1 || a.Max != -1);

            if (!doAnimals && !doOres && !doLoots)
            {
                Puts("Nothing to process, skipping MainSpawnCycle()");
                return;
            }

            var tick = DateTime.Now;
            var collectibles = Resources.FindObjectsOfTypeAll<CollectibleEntity>().Where(c => c.isActiveAndEnabled).ToList();
            //Puts($"collectibles: {(DateTime.Now - tick).TotalMilliseconds} ms");

            var aborted = false;

            if (doAnimals)
            {
                tick = DateTime.Now;
                SubCycle(Resources.FindObjectsOfTypeAll<BaseNPC>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), g_config.Animals, collectibles, ref aborted);
                //Puts($"npc: {(DateTime.Now - tick).TotalMilliseconds} ms");
            }

            if (doOres)
            {
                tick = DateTime.Now;
                SubCycle(Resources.FindObjectsOfTypeAll<BaseResource>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), g_config.Ores, collectibles, ref aborted);
                //Puts($"res: {(DateTime.Now - tick).TotalMilliseconds} ms");
            }

            if (doLoots)
            {
                tick = DateTime.Now;
                SubCycle(Resources.FindObjectsOfTypeAll<LootContainer>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>(), g_config.Loots, collectibles, ref aborted);
                //Puts($"loot: {(DateTime.Now - tick).TotalMilliseconds} ms");
            }
        }

        void PopulationControl(IEnumerable<BaseEntity> matches, int cap)
        {
            if (cap < 0) return;
            if (matches.Count() < cap) return;
            if (matches.Count() == 0) return;
            var toDelete = matches.Count() - cap;

            var killed = 0;
            var shortPrefabName = matches.First().LookupShortPrefabName();

            while (killed != toDelete)
            {
                var idx = Random.Range(0, matches.Count() - 1);
                var match = matches.ElementAt(idx);
                if (!match.enabled) continue;
                match.enabled= false;
                match.Kill();
                killed++;
            }

            Puts($"Destroying {toDelete}X {shortPrefabName}!");
        }

        void BatchSpawn(int current, int wanted, string prefab, List<CollectibleEntity> collectibles, ref bool aborted)
        {
            if (aborted) return;

            int toSpawn = wanted - current;
            if (toSpawn <= 0) return;

            if (toSpawn > collectibles.Count())
            {
                Puts($"Could not find enough collectibles to complete the spawn cycle (this is normal after a server restart, it takes time!)");
                aborted = true;
                toSpawn = collectibles.Count();
            }

            Puts($"Spawning {toSpawn}X {prefab}!");
            for (int i = 0; i < toSpawn; i++)
                ReplaceCollectibleWithSomething(prefab, collectibles);

            return;
        }

        private void ReplaceCollectibleWithSomething(string prefabName, List<CollectibleEntity> collectibles)
        {
            // Pick a collectible that we did not replace yet and remove it from the list
            var pick = Random.Range(0, collectibles.Count() - 1);
            var spawnToReplace = collectibles.ElementAt(pick);
            collectibles.RemoveAt(pick);

            // save the position
            var position = spawnToReplace.transform.position;

            // delete the collectible (we are replacing it)
            spawnToReplace.Kill();

            BaseEntity entity = GameManager.server.CreateEntity(prefabName, position, new Quaternion(0, 0, 0, 0));
            if (entity == null)
            {
                Puts($"Tried to spawn {prefabName} but entity could not be spawned.");
                return;
            }

            entity.name = prefabName;
            entity.Spawn(true);
        }

        void FixLoot(BasePlayer player)
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled && c.LookupShortPrefabName().StartsWith("crate")).
                OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z)
                .ToList();

            var count = spawns.Count();
            var racelimit = count * count;

            var antirace = 0;
            var deleted = 0;

            for (var i = 0; i < count; i++)
            {
                var box = spawns[i];
                var pos = GetBoxPos(box);

                if (++antirace > racelimit)
                {
                    Puts("Race condition detected ?! report to author");
                    return;
                }

                var next = i + 1;
                while (next < count)
                {
                    var box2 = spawns[next];
                    var pos2 = GetBoxPos(box2);
                    var distance = Vector2.Distance(pos, pos2);

                    if (++antirace > racelimit)
                    {
                        Puts("Race condition detected ?! report to author");
                        return;
                    }

                    if (distance < 5)
                    {
                        spawns.RemoveAt(next);
                        count--;
                        box2.Kill();
                        deleted++;
                    }
                    else break;
                }
            }

            if (deleted > 0)
                Puts($"Deleted {deleted} stacked loot boxes (out of {count})");
            if (player != null)
                player.ChatMessage($"Deleted {deleted} stacked loot boxes (out of {count})");
        }

        private void Purge()
        {
            // Delete all spawnables
            var ores = GetOreNodes();
            foreach (var grp in ores)
                foreach (var ore in grp.Value) ore.Kill();

            var loots = GetLootContainers();
            foreach (var grp in loots)
                foreach (var loot in grp.Value) loot.Kill();

            var animals = GetAnimals();
            foreach (var grp in animals)
                foreach (var animal in grp.Value) animal.Kill();
        }

        //void OnEntitySpawned(BaseNetworkable entity)
        //{
        //    if (!g_spawnBlock) return;

        //    var controlled = g_config.Animals.Union(g_config.Ores).Union(g_config.Loots);
        //    var prefab = entity.LookupPrefabName();
        //    if (controlled.Any(c => c.Prefab == prefab && c.Max != -1))
        //    {
        //        entity.Kill();
        //        //Puts($"BLOCKED OnEntitySpawned {entity.LookupShortPrefabName()}");
        //    }
        //}
    }
}
