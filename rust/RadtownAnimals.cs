using System;
using System.Collections.Generic;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("RadtownAnimals", "k1lly0u", "0.1.2", ResourceId = 1561)]
    class RadtownAnimals : RustPlugin
    {
        private bool Changed;
        private List<GameObject> monuments = new List<GameObject>();
        private Dictionary<BaseEntity, GameObject> animals = new Dictionary<BaseEntity, GameObject>();

        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {                       
            lang.RegisterMessages(messages, this);
            LoadVariables();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void OnServerInitialized()
        {
            findAllMonuments();
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (animals.ContainsKey(entity))
            {
                var monument = animals[entity];
                var type = getEntityName(entity);     
                animals.Remove(entity);
                timer.Once(animalRespawn * 60, () => spawnAnimal(monument, type));
            }
        }
        void Unload()
        {
            killAllAnimals();
            animals.Clear();
            monuments.Clear();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static bool spawnLighthouse = false;
        static bool spawnAirfield = false;
        static bool spawnPowerplant = false;
        static bool spawnTrainyard = false;
        static bool spawnWaterplant = false;
        static bool spawnWarehouse = false;
        static bool spawnSatellite = false;
        static bool spawnDome = false;
        static bool spawnRadtown = true;

        static bool spawnBears = true;
        static bool spawnBoars = false;
        static bool spawnChickens = false;
        static bool spawnHorses = false;
        static bool spawnStags = false;
        static bool spawnWolfs = true;

        static int animalCount = 10;
        static int maximumAWOL = 100;
        static int maximumAnimals = 50;
        
        static float animalSpread = 20f;
        static float animalRespawn = 10f;
        static float distanceCheck = 5f;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Options - Spawns - Lighthouses", ref spawnLighthouse);
            CheckCfg("Options - Spawns - Airfield", ref spawnAirfield);
            CheckCfg("Options - Spawns - Powerplant", ref spawnPowerplant);
            CheckCfg("Options - Spawns - Trainyard", ref spawnTrainyard);
            CheckCfg("Options - Spawns - Water Treatment Plant", ref spawnWaterplant);
            CheckCfg("Options - Spawns - Warehouses", ref spawnWarehouse);
            CheckCfg("Options - Spawns - Satellite", ref spawnSatellite);
            CheckCfg("Options - Spawns - Sphere Tank", ref spawnDome);
            CheckCfg("Options - Spawns - Rad-towns", ref spawnRadtown);

            CheckCfg("Options - Animals - Spawn Bears", ref spawnBears);
            CheckCfg("Options - Animals - Spawn Boars", ref spawnBoars);
            CheckCfg("Options - Animals - Spawn Chickens", ref spawnChickens);
            CheckCfg("Options - Animals - Spawn Horses", ref spawnHorses);
            CheckCfg("Options - Animals - Spawn Stags", ref spawnStags);
            CheckCfg("Options - Animals - Spawn Wolfs", ref spawnWolfs);
            CheckCfg("Options - Animals - Maximum Amount (total)", ref maximumAnimals);
            CheckCfg("Options - Animals - Maximum Amount (per monument)", ref animalCount);
            CheckCfg("Options - Animals - Maximum distance from monument", ref maximumAWOL);
            CheckCfgFloat("Options - Spawnpoints - Spread", ref animalSpread);

            CheckCfgFloat("Options - Timers - Respawn (minutes)", ref animalRespawn);
            CheckCfgFloat("Options - Timers - Distance check timer (minutes)", ref distanceCheck);
        }
        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"nullList", "Error getting a list of monuments" },
            {"title", "<color=orange>RadtownAnimals</color> : " },
            {"noPerms", "You have insufficient permission" },
            {"killedAll", "Killed all animals" }
        };
       
        //////////////////////////////////////////////////////////////////////////////////////
        // RadtownAnimals /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private void findAllMonuments()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (spawnLighthouse)
                {
                    if (gobject.name.ToLower().Contains("lighthouse/lighthouse"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnPowerplant)
                {
                    if (gobject.name.ToLower().Contains("powerplant_1"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnAirfield)
                {
                    if (gobject.name.ToLower().Contains("airfield_1"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnTrainyard)
                {
                    if (gobject.name.ToLower().Contains("large/trainyard_1"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnWaterplant)
                {
                    if (gobject.name.ToLower().Contains("large/water_treatment_plant_1"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnWarehouse)
                {
                    if (gobject.name.ToLower().Contains("mining/warehouse"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnSatellite)
                {
                    if (gobject.name.ToLower().Contains("production/satellite_dish"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnDome)
                {
                    if (gobject.name.ToLower().Contains("production/sphere_tank"))
                    {
                        monuments.Add(gobject);
                    }
                }
                if (spawnRadtown)
                {
                    if (gobject.name.ToLower().Contains("small/radtown_small_3"))
                    {
                        monuments.Add(gobject);
                    }
                }            
            }
            if (monuments == null)
            {
                Puts(lang.GetMessage("nullList", this));
                return;
            }
            startSpawn();                         
        }
        private void startSpawn()
        {
            foreach (var location in monuments)
            {
                timer.Once(0.1f, () => getAnimals(location));
            }
        }
        private Vector3 getPos(Vector3 pos)
        {
            Vector3 randomPos = Quaternion.Euler(UnityEngine.Random.Range((float)(-animalSpread * 0.2), animalSpread * 0.2f), UnityEngine.Random.Range((float)(-animalSpread * 0.2), animalSpread * 0.2f), UnityEngine.Random.Range((float)(-animalSpread * 0.2), animalSpread * 0.2f)) * pos;
            Vector3 correctPos = getGroundPosition(randomPos);
            return correctPos;            
        }
        private void getAnimals(GameObject monument)
        {
            List<string> setAnimals = new List<string>();

            if (spawnBears) setAnimals.Add("bear");
            if (spawnBoars) setAnimals.Add("boar");
            if (spawnChickens) setAnimals.Add("chicken");
            if (spawnHorses) setAnimals.Add("horse");
            if (spawnStags) setAnimals.Add("stag");
            if (spawnWolfs) setAnimals.Add("wolf");

            var aCount = setAnimals.Count;
            int eachAnimal = animalCount / aCount;

            foreach (var animal in setAnimals)
            {                
                var type = animal;
                timer.Repeat(0.1f, eachAnimal, () => spawnAnimal(monument, type));                
            }
        }
        private void spawnAnimal(GameObject monument, string animalType)
        {
            if (animals.Count >= maximumAnimals) return;

            var newPos = getPos(monument.transform.position);
            var animal = GameManager.server.CreateEntity("assets/bundled/prefabs/autospawn/animals/" + animalType + ".prefab", newPos, new Quaternion(), true);
            BaseEntity entity = animal?.GetComponent<BaseEntity>();
            if (entity != null)
            {
                entity?.Spawn(true);
                animals.Add(entity, monument);
                checkDistance(entity, monument);
            }
        }       
        static Vector3 getGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        private void checkDistance(BaseEntity animal, GameObject monument)
        {
            if (animal != null)
            {
                var currentPos = animal.transform.position;
                var monPos = monument.transform.position;
                if (Vector3.Distance(currentPos, monPos) > (maximumAWOL))
                {
                    killRespawn(animal, monument);
                    return;
                }
                timer.Once(distanceCheck * 60, () => checkDistance(animal, monument));
            }
        }      
        private void killRespawn(BaseEntity animal, GameObject monument)
        {                   
            var name = getEntityName(animal);
            animal.KillMessage();
            animals.Remove(animal);
            spawnAnimal(monument, name);
        }
        private string getEntityName(BaseEntity entity)
        {
            string name = "";
            if (entity.name.Contains("bear")) name = "bear";
            if (entity.name.Contains("boar")) name = "boar";
            if (entity.name.Contains("chicken")) name = "chicken";
            if (entity.name.Contains("horse")) name = "horse";
            if (entity.name.Contains("stag")) name = "stag";
            if (entity.name.Contains("wolf")) name = "wolf";
            return name;
        }
        private void killAllAnimals()
        {
            foreach (var animal in animals.Keys)
            {
                animal.KillMessage();
            }
            animals.Clear();           
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Permission/Auth Check /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
               
        bool isAdmin(BasePlayer player)
        {
            if (player.net.connection != null)
            {
                if (player.net.connection.authLevel <= 1)
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
                    return false;
                }
            }
            return true;
        }
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, lang.GetMessage("noPerms", this));
                    return false;
                }
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat/Console Commands /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("ra_killall")]
        private void chatKillAnimals(BasePlayer player, string command, string[] args)
        {
            if (!isAdmin(player)) return;
            killAllAnimals();
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("killedAll", this, player.UserIDString));
        }

        [ConsoleCommand("ra_killall")]
        private void ccmdKillAnimals(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            killAllAnimals();
            SendReply(arg, lang.GetMessage("killedAll", this));
        }

    }
}
