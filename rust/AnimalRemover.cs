using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AnimalRemover", "Ankawi", "1.0.2")]
    [Description("Allows you to disable specific animals from spawning on the server")]
    public class AnimalRemover : RustPlugin
    {
        const string BearPrefab = "assets/bundled/prefabs/autospawn/animals/bear.prefab";
        const string BoarPrefab = "assets/bundled/prefabs/autospawn/animals/boar.prefab";
        const string ChickenPrefab = "assets/bundled/prefabs/autospawn/animals/chicken.prefab";
        const string HorsePrefab = "assets/bundled/prefabs/autospawn/animals/horse.prefab";
        const string StagPrefab = "assets/bundled/prefabs/autospawn/animals/stag.prefab";
        const string WolfPrefab = "assets/bundled/prefabs/autospawn/animals/wolf.prefab";

        new void LoadConfig()
        {
            SetConfig("Disable Bear Spawning", false);
            SetConfig("Disable Boar Spawning", false);
            SetConfig("Disable Chicken Spawning", false);
            SetConfig("Disable Horse Spawning", false);
            SetConfig("Disable Stag Spawning", false);
            SetConfig("Disable Wolf Spawning", false);

            SaveConfig();
        }
        protected override void LoadDefaultConfig() => PrintWarning("Creating a new configuration file...");

        void OnServerInitialized()
        {
            LoadConfig();
            KillAnimals();
        }

        Dictionary<string, IGrouping<string, BaseEntity>> GetAnimals()
        {
            return Resources.FindObjectsOfTypeAll<BaseNPC>()
                .Where(c => c.isActiveAndEnabled)
                .Cast<BaseEntity>()
                .GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c);
        }

        void KillAnimals()
        {
            var animalList = GetAnimals();
            foreach (var g in animalList)
            {
                foreach (var animal in g.Value)
                {
                    if ((bool)Config["Disable Bear Spawning"])
                    {
                        if (animal.PrefabName.Contains(BearPrefab))
                        {
                            animal.Kill();
                        }
                    }
                    if ((bool)Config["Disable Boar Spawning"])
                    {
                        if (animal.PrefabName.Contains(BoarPrefab))
                        {
                            animal.Kill();
                        }
                    }
                    if ((bool)Config["Disable Chicken Spawning"])
                    {
                        if (animal.PrefabName.Contains(ChickenPrefab))
                        {
                            animal.Kill();
                        }
                    }
                    if ((bool)Config["Disable Horse Spawning"])
                    {
                        if (animal.PrefabName.Contains(HorsePrefab))
                        {
                            animal.Kill();
                        }
                    }
                    if ((bool)Config["Disable Stag Spawning"])
                    {
                        if (animal.PrefabName.Contains(StagPrefab))
                        {
                            animal.Kill();
                        }
                    }
                    if ((bool)Config["Disable Wolf Spawning"])
                    {
                        if (animal.PrefabName.Contains(WolfPrefab))
                        {
                            animal.Kill();
                        }
                    }
                }
            }
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            KillAnimals();
        }
        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }
    }
}