// Reference: RustBuild
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("RadtownAnimals", "k1lly0u", "0.2.11", ResourceId = 1561)]
    class RadtownAnimals : RustPlugin
    {
        #region Fields
        private Dictionary<BaseEntity, Vector3> animalList = new Dictionary<BaseEntity, Vector3>();
        private List<Timer> refreshTimers = new List<Timer>();
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            InitializeAnimalSpawns();
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity.GetComponent<BaseNPC>() != null)
                {
                    if (animalList.ContainsKey(entity as BaseEntity))
                    {
                        UnityEngine.Object.Destroy(entity.GetComponent<RAController>());
                        InitiateRefresh(entity as BaseEntity);
                    }
                }
            }
            catch { }
        }
        void Unload()
        {
            foreach (var time in refreshTimers)
                time.Destroy();

            foreach (var animal in animalList)
            {
                if (animal.Key != null)
                {
                    UnityEngine.Object.Destroy(animal.Key.GetComponent<RAController>());
                    animal.Key.KillMessage();
                }
            }
            var objects = UnityEngine.Object.FindObjectsOfType<RAController>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            animalList.Clear();
        }
        #endregion

        #region Initial Spawning
        private void InitializeAnimalSpawns()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("autospawn/monument"))
                {
                    var position = gobject.transform.position;
                    if (gobject.name.ToLower().Contains("lighthouse"))
                    {
                        if (configData.Lighthouses.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Lighthouses.AnimalCounts));
                            continue;
                        }
                    }
                    if (gobject.name.Contains("powerplant_1"))
                    {
                        if (configData.Powerplant.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Powerplant.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("military_tunnel_1"))
                    {
                        if (configData.MilitaryTunnels.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.MilitaryTunnels.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("airfield_1"))
                    {
                        if (configData.Airfield.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Airfield.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("trainyard_1"))
                    {
                        if (configData.Trainyard.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Trainyard.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("water_treatment_plant_1"))
                    {
                        if (configData.WaterTreatmentPlant.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.WaterTreatmentPlant.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("warehouse"))
                    {
                        if (configData.Warehouses.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Warehouses.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("satellite_dish"))
                    {
                        if (configData.Satellite.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Satellite.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("sphere_tank"))
                    {
                        if (configData.SphereTank.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.SphereTank.AnimalCounts));
                            continue;
                        }
                    }

                    if (gobject.name.Contains("radtown_small_3"))
                    {
                        if (configData.Radtowns.Enabled)
                        {
                            SpawnAnimals(position, GetSpawnList(configData.Radtowns.AnimalCounts));
                            continue;
                        }
                    }
                }
            }            
        }
        private Dictionary<string, int> GetSpawnList(AnimalCounts counts)
        {
            var spawnList = new Dictionary<string, int>
            {
                {"bear", counts.Bears},
                {"boar", counts.Boars },
                {"chicken", counts.Chickens },
                {"horse", counts.Horses },
                {"stag", counts.Stags },
                {"wolf", counts.Wolfs }
            };
            return spawnList;
        }
        private void SpawnAnimals(Vector3 position, Dictionary<string,int> spawnList)
        {
            if (animalList.Count >= configData.a_Options.TotalMaximumAmount)
            {
                PrintError(lang.GetMessage("spawnLimit", this));
                return;
            }
            foreach (var type in spawnList)
            {
                
                for (int i = 0; i < type.Value; i++)
                {
                    var entity = SpawnAnimalEntity(type.Key, position);
                    animalList.Add(entity, position);
                }
            }
        }
        #endregion

        #region Spawn Control
        private void InitiateRefresh(BaseEntity animal)
        {
            var position = animal.transform.position;
            var type = animal.ShortPrefabName.Replace(".prefab", "");
            refreshTimers.Add(timer.Once(configData.a_Options.RespawnTimer * 60, () =>
            {
                InitializeNewSpawn(type, position);
            }));
            animalList.Remove(animal);
        }
        private void InitializeNewSpawn(string type, Vector3 position)
        {
            var newAnimal = SpawnAnimalEntity(type, position);
            animalList.Add(newAnimal, position);
        }
        private BaseEntity SpawnAnimalEntity(string type, Vector3 pos)
        {
            var newPos = AdjustPosition(pos);
            BaseEntity entity = GameManager.server.CreateEntity($"assets/bundled/prefabs/autospawn/animals/{type}.prefab", newPos, new Quaternion(), true);
            entity.Spawn();
            var npc = entity.gameObject.AddComponent<RAController>();
            npc.SetHome(pos);
            return entity;
        }
        private Vector3 AdjustPosition(Vector3 pos)
        {
            Vector3 randomPos = Quaternion.Euler(UnityEngine.Random.Range((float)(-configData.a_Options.SpawnSpread * 0.2), configData.a_Options.SpawnSpread * 0.2f), UnityEngine.Random.Range((float)(-configData.a_Options.SpawnSpread * 0.2), configData.a_Options.SpawnSpread * 0.2f), UnityEngine.Random.Range((float)(-configData.a_Options.SpawnSpread * 0.2), configData.a_Options.SpawnSpread * 0.2f)) * pos;
            Vector3 correctPos = GetGroundPosition(randomPos);
            return correctPos;
        }
        #endregion

        #region Helper Methods
        static Vector3 GetGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))            
                sourcePos.y = hitInfo.point.y;            
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        #endregion

        #region NPCController
        class RAController : MonoBehaviour
        {
            private readonly MethodInfo SetDeltaTimeMethod = typeof(NPCAI).GetProperty("deltaTime", (BindingFlags.Public | BindingFlags.Instance)).GetSetMethod(true);

            internal static double targetAttackRange = 70;

            internal Vector3 Home;
            internal Vector3 NextPos;
            internal BaseCombatEntity Target;

            internal bool isAttacking;

            public BaseNPC NPC;
            public NPCAI AI;
            public NPCMetabolism Metabolism;

            void Awake()
            {
                AI = GetComponent<NPCAI>();
                NPC = GetComponent<BaseNPC>();
                Metabolism = GetComponent<NPCMetabolism>();
                isAttacking = false;
                Target = null;
                NPC.state = BaseNPC.State.Normal;
                NPC.enableSaving = false;
                BaseEntity.saveList.Remove(NPC);
            }
            void FixedUpdate()
            {
                if (AI.deltaTime < ConVar.Server.TickDelta()) return;
                if (NPC.IsStunned()) return;
                NPC.Tick();
                if (NPC.attack.IsActive())
                {
                    NPC.attack.gameObject.SetActive(false);
                    Move(NextPos);
                    return;
                }
                if (Vector3.Distance(transform.position, Home) > 140)
                {
                    Move(Home);
                    return;
                }
                if (isAttacking && Target != null)
                {
                    var distance = Vector3.Distance(transform.position, Target.transform.position);
                    if (distance >= 70)
                    {
                        isAttacking = false;
                        Target = null;
                        return;
                    }
                    else if (distance < targetAttackRange)
                    {
                        var normalized = (Target.transform.position - transform.position).XZ3D().normalized;
                        if (NPC.diet.Eat(Target))
                        {
                            NPC.Heal(NPC.MaxHealth() / 10);
                            Metabolism.calories.Add(Metabolism.calories.max / 10);
                            Metabolism.hydration.Add(Metabolism.hydration.max / 10);
                        }
                        else if (NPC.attack.Hit(Target, 1, false))
                            transform.rotation = Quaternion.LookRotation(normalized);
                        NPC.steering.Face(normalized);
                    }
                    else Move(Target.transform.position);
                }
                else if (Vector3.Distance(transform.position, NextPos) < 20)
                {
                    CalculateNextPos();

                    if (Metabolism.calories.value < 20f)
                        NPC.diet.Forage();
                    else if (Metabolism.sleep.value < 20f)
                        Sleep();
                }
                else Move(NextPos);
            }
            public void SetHome(Vector3 pos)
            {
                Home = pos;
                NextPos = pos;
            }
            void CalculateNextPos()
            {
                RaycastHit hitInfo;

                NextPos = Home;
                NextPos.x += UnityEngine.Random.Range(-100, 100);

                if (Physics.Raycast(NextPos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))
                    NextPos.y = hitInfo.point.y;
                NextPos.y = Mathf.Max(NextPos.y, TerrainMeta.HeightMap.GetHeight(NextPos));

                NextPos.z += UnityEngine.Random.Range(-100, 100);
            }
            void Move(Vector3 pos)
            {
                NPC.state = BaseNPC.State.Normal;
                AI.sense.Think();
                NPC.steering.Move((pos - transform.position).XZ3D().normalized, pos, (int)NPCSpeed.Trot);
            }
            void Sleep()
            {
                NPC.state = BaseNPC.State.Sleeping;
                NPC.sleep.Recover(20f);
                Metabolism.stamina.Run(20f);
                NPC.StartCooldown(20f, true);
            }
            internal void OnAttacked(HitInfo info)
            {
                if (info.Initiator)
                    Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }
            internal void Attack(BaseCombatEntity ent)
            {
                Target = ent;
                isAttacking = true;
                targetAttackRange = Math.Pow(NPC._collider.bounds.XZ3D().extents.Max() + NPC.attack.range + ent._collider.bounds.XZ3D().extents.Max(), 2);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("ra_killall")]
        private void chatKillAnimals(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin()) return;
            foreach(var animal in animalList)
            {
                UnityEngine.Object.Destroy(animal.Key.GetComponent<RAController>());
                animal.Key.KillMessage();
            }
            animalList.Clear();
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("killedAll", this, player.UserIDString));
        }

        [ConsoleCommand("ra_killall")]
        private void ccmdKillAnimals(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                foreach (var animal in animalList)
                {
                    UnityEngine.Object.Destroy(animal.Key.GetComponent<RAController>());
                    animal.Key.KillMessage();
                }
                animalList.Clear();
                SendReply(arg, lang.GetMessage("killedAll", this));
            }
        }
        #endregion

        #region Config 
        #region Options       
        class AnimalCounts
        {
            public int Bears;
            public int Boars;
            public int Chickens;
            public int Horses;
            public int Stags;
            public int Wolfs;
        }
        class LightHouses
        {
            public AnimalCounts AnimalCounts { get; set; }  
            public bool Enabled { get; set; }          
        }
        class Airfield
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class Powerplant
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class Trainyard
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class WaterTreatmentPlant
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class Warehouses
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class Satellite
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class SphereTank
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }

        class Radtowns
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }
        class MilitaryTunnels
        {
            public AnimalCounts AnimalCounts { get; set; }
            public bool Enabled { get; set; }
        }
        class Options
        {
            public int RespawnTimer;
            public float SpawnSpread;
            public int TotalMaximumAmount;           
        }
        #endregion

        private ConfigData configData;
        class ConfigData
        {
            public LightHouses Lighthouses { get; set; }
            public Airfield Airfield { get; set; }
            public Powerplant Powerplant { get; set; }
            public Trainyard Trainyard { get; set; }
            public WaterTreatmentPlant WaterTreatmentPlant { get; set; }
            public Warehouses Warehouses { get; set; }
            public Satellite Satellite { get; set; }
            public SphereTank SphereTank { get; set; }
            public Radtowns Radtowns { get; set; }
            public MilitaryTunnels MilitaryTunnels { get; set; }
            public Options a_Options { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Airfield = new Airfield
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Lighthouses = new LightHouses
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                MilitaryTunnels = new MilitaryTunnels
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Powerplant = new Powerplant
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Radtowns = new Radtowns
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Satellite = new Satellite
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                SphereTank = new SphereTank
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Trainyard = new Trainyard
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                Warehouses = new Warehouses
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                WaterTreatmentPlant = new WaterTreatmentPlant
                {
                    AnimalCounts = new AnimalCounts
                    {
                        Bears = 0,
                        Boars = 0,
                        Chickens = 0,
                        Horses = 0,
                        Stags = 0,
                        Wolfs = 0,
                    },
                    Enabled = false
                },
                a_Options = new Options
                {
                    TotalMaximumAmount = 40,
                    RespawnTimer = 15,
                    SpawnSpread = 100
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion      

        #region Messaging
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"nullList", "<color=#939393>Error getting a list of monuments</color>" },
            {"title", "<color=orange>Radtown Animals:</color> " },
            {"killedAll", "<color=#939393>Killed all animals</color>" },
            {"spawnLimit", "<color=#939393>The animal spawn limit has been hit.</color>" }
        };
        #endregion
    }
}
