// Reference: RustBuild
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("CustomAnimalSpawns", "k1lly0u", "0.1.21", ResourceId = 2015)]
    class CustomAnimalSpawns : RustPlugin
    {
        #region Fields
        CASData casData;
        private DynamicConfigFile casdata;

        private FieldInfo serverinput;

        private List<BaseEntity> animalCache = new List<BaseEntity>();
        private Dictionary<int, string> animalTypes = new Dictionary<int, string>();
        private List<Timer> refreshTimers = new List<Timer>();

        private Dictionary<ulong, int> animalCreators = new Dictionary<ulong, int>();

        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("customanimalspawns.admin", this);
            lang.RegisterMessages(messages, this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            casdata = Interface.Oxide.DataFileSystem.GetFile("CustomSpawns/cas_data");
            casdata.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter() };
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            FindAnimalTypes();
            InitializeAnimalSpawns();
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseNPC)
                entity.GetComponent<NPCController>()?.OnAttacked(hitInfo);
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity.GetComponent<BaseNPC>() != null)
                {
                    if (animalCache.Contains(entity as BaseEntity))
                    {
                        UnityEngine.Object.Destroy(entity.GetComponent<NPCController>());
                        InitiateRefresh(entity as BaseEntity);
                    }
                }
            }
            catch { }
        }
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (animalCreators.ContainsKey(player.userID))
                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    int type = animalCreators[player.userID];
                    AddSpawn(player, type);
                }
        }
        void Unload()
        {
            foreach (var time in refreshTimers)
                time.Destroy();

            foreach (var animal in animalCache)
            {
                if (animal != null)
                {
                    UnityEngine.Object.Destroy(animal.GetComponent<NPCController>());
                    animal.KillMessage();
                }
            }
            var objects = UnityEngine.Object.FindObjectsOfType<NPCController>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            animalCache.Clear();
        }
        #endregion

        #region Resource Control
        private void InitializeAnimalSpawns()
        {
            foreach (var resource in casData.animals)
            {
                InitializeNewSpawn(resource.Type, resource.Position);
            }
        }
        private void InitiateRefresh(BaseEntity resource)
        {
            var position = resource.transform.position;
            var type = resource.PrefabName;
            refreshTimers.Add(timer.Once(configData.RespawnTimer * 60, () =>
            {
                InitializeNewSpawn(type, position);
            }));
            animalCache.Remove(resource);
        }
        private void InitializeNewSpawn(string type, Vector3 position)
        {
            var newAnimal = SpawnAnimalEntity(type, position);

            animalCache.Add(newAnimal);
        }
        private BaseEntity SpawnAnimalEntity(string type, Vector3 pos)
        {
            BaseEntity entity = GameManager.server.CreateEntity(type, pos, new Quaternion(), true);            
            entity.Spawn();
            var npc = entity.gameObject.AddComponent<NPCController>();
            npc.SetHome(pos);
            return entity;
        }

        class NPCController : MonoBehaviour
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
                if (Vector3.Distance(transform.position, Home) > 100)
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
                NextPos.x += UnityEngine.Random.Range(-70, 70);

                if (Physics.Raycast(NextPos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))                
                    NextPos.y = hitInfo.point.y;                
                NextPos.y = Mathf.Max(NextPos.y, TerrainMeta.HeightMap.GetHeight(NextPos));

                NextPos.z += UnityEngine.Random.Range(-70, 70);   
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

        #region Animal Spawning
        private void AddSpawn(BasePlayer player, int type)
        {
            string animal = animalTypes[type];
            var pos = GetSpawnPos(player);
            BaseEntity entity = SpawnAnimalEntity(animal, pos);
            casData.animals.Add(new CLAnimal { Position = entity.transform.position, Type = animal });
            animalCache.Add(entity);
            SaveData();
        }        
        #endregion

        #region Helper Methods       
        private void FindAnimalTypes()
        {
            var filesField = typeof(FileSystem_AssetBundles).GetField("files", BindingFlags.Instance | BindingFlags.NonPublic);
            var files = (Dictionary<string, AssetBundle>)filesField.GetValue(FileSystem.iface);
            int i = 1;
            foreach (var str in files.Keys)
                if (str.StartsWith("assets/bundled/prefabs/autospawn/animals/"))
                {
                    var gmobj = GameManager.server.FindPrefab(str);
                    if (gmobj?.GetComponent<BaseEntity>() != null)
                    {
                        animalTypes.Add(i, str);
                        i++;
                    }
                }
        }
        private Vector3 GetSpawnPos(BasePlayer player)
        {
            Vector3 closestHitpoint;
            Vector3 sourceEye = player.transform.position + new Vector3(0f, 1.5f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            Quaternion currentRot = Quaternion.Euler(input.current.aimAngles);
            Ray ray = new Ray(sourceEye, currentRot * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = player.transform.position;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<TriggerBase>() == null)
                {
                    if (hit.distance < closestdist)
                    {
                        closestdist = hit.distance;
                        closestHitpoint = hit.point;
                    }
                }
            }
            return closestHitpoint;
        }        
        private List<Vector3> FindInRadius(Vector3 pos, float rad)
        {
            var foundResources = new List<Vector3>();
            foreach (var item in casData.animals)
            {
                var itemPos = item.Position;
                if (GetDistance(pos, itemPos) < rad)
                {
                    foundResources.Add(itemPos);
                }
            }
            return foundResources;
        }       
        private bool RemoveFromData(Vector3 pos)
        {
            foreach (var resource in casData.animals)
            {
                if (GetDistance(pos, resource.Position) < 1)
                {
                    casData.animals.Remove(resource);
                    return true;
                }
            }
            return false;
        }
        private float GetDistance(Vector3 v3, Vector3 v32) => Vector3.Distance(v3, v32);

        private void ShowAnimalList(BasePlayer player)
        {
            foreach (var entry in animalTypes)
                SendEchoConsole(player.net.connection, string.Format("{0} - {1}", entry.Key, entry.Value));
        }
        private void ShowCurrentAnimals(BasePlayer player)
        {
            foreach (var resource in casData.animals)
                SendEchoConsole(player.net.connection, string.Format("{0} - {1}", resource.Position, resource.Type));
        }
        void SendEchoConsole(Network.Connection cn, string msg)
        {
            if (Network.Net.sv.IsConnected())
            {
                Network.Net.sv.write.Start();
                Network.Net.sv.write.PacketID(Network.Message.Type.ConsoleMessage);
                Network.Net.sv.write.String(msg);
                Network.Net.sv.write.Send(new Network.SendInfo(cn));
            }
        }
        #endregion

        #region Chat Commands
        [ChatCommand("cas")]
        private void chatAnimalSpawn(BasePlayer player, string command, string[] args)
        {
            if (!canSpawnAnimals(player)) return;
            if (animalCreators.ContainsKey(player.userID))
            {
                animalCreators.Remove(player.userID);
                SendMSG(player, lang.GetMessage("endAdd", this, player.UserIDString));
                return;
            }
            if (args.Length == 0)
            {
                SendReply(player, lang.GetMessage("synAdd", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synNear", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synRemNear", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synList", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synAnimal", this, player.UserIDString));
                SendReply(player, lang.GetMessage("synWipe", this, player.UserIDString));
                return;
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "add":
                        {
                            int type;
                            if (!int.TryParse(args[1], out type))
                            {
                                SendMSG(player, MSG("notNum", player.UserIDString));
                                return;
                            }
                            if (animalTypes.ContainsKey(type))
                            {
                                animalCreators.Add(player.userID, type);
                                SendMSG(player, MSG("adding", player.UserIDString));
                                return;
                            }
                            SendMSG(player, MSG("notType", player.UserIDString));
                        }
                        return;
                    case "remove":
                        {
                            if (args.Length >= 2 && args[1].ToLower() == "near")
                            {
                                float rad = 10f;
                                if (args.Length == 3) float.TryParse(args[2], out rad);

                                var animals = FindInRadius(player.transform.position, rad);
                                if (animals != null)
                                {
                                    int i = 0;
                                    for (int n = 0; n < animals.Count; n++)
                                    {
                                        RemoveFromData(animals[i]);
                                        i++;
                                    }
                                    SaveData();
                                    SendMSG(player, string.Format(MSG("removedNear", player.UserIDString), i, rad));
                                    return;
                                }
                                else
                                    SendMSG(player, string.Format(MSG("noFind", player.UserIDString), rad.ToString()));
                                return;
                            }
                        }
                        return;
                    case "list":
                        ShowCurrentAnimals(player);
                        SendMSG(player, MSG("checkConsole", player.UserIDString));
                        return;
                    case "animals":
                        ShowAnimalList(player);
                        SendMSG(player, MSG("checkConsole", player.UserIDString));
                        return;
                    case "near":
                        {
                            float rad = 10f;
                            if (args.Length == 2) float.TryParse(args[1], out rad);

                            var animals = FindInRadius(player.transform.position, rad);
                            if (animals != null)
                            {
                                SendMSG(player, string.Format(MSG("foundResources", player.UserIDString), animals.Count.ToString()));
                                foreach (var resource in animals)
                                    player.SendConsoleCommand("ddraw.box", 10f, Color.magenta, resource, 1f);
                            }
                            else
                                SendMSG(player, string.Format(MSG("noFind", player.UserIDString), rad.ToString()));
                        }
                        return;
                    case "wipe":
                        {
                            var count = casData.animals.Count;
                            foreach (var resource in animalCache)
                            {
                                resource.KillMessage();
                            }
                            casData.animals.Clear();
                            SaveData();
                            SendMSG(player, string.Format(MSG("wipedAll1", player.UserIDString), count));
                        }
                        return;
                    default:
                        break;
                }
            }
        }
        bool canSpawnAnimals(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "customanimalspawns.admin")) return true;
            SendMSG(player, MSG("noPerms", player.UserIDString));
            return false;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int RespawnTimer { get; set; }
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
                RespawnTimer = 20
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => casdata.WriteObject(casData);
        void LoadData()
        {
            try
            {
                casData = casdata.ReadObject<CASData>();
            }
            catch
            {
                casData = new CASData();
            }
        }
        class CASData
        {
            public List<CLAnimal> animals = new List<CLAnimal>();
        }
        #endregion

        #region Classes
        class CLAnimal
        {
            public string Type;
            public Vector3 Position;
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        } // borrowed from ZoneManager
        #endregion

        #region Messaging
        private void SendMSG(BasePlayer player, string message) => SendReply(player, $"<color=orange>{Title}:</color> <color=#939393>{message}</color>");
        private string MSG(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"checkConsole", "Check your console for a list of animals" },
            {"noPerms", "You do not have permission to use this command" },
            {"notType", "The number you have entered is not on the list" },
            {"notNum", "You must enter a animal number" },
            {"notBox", "You are not looking at a animal" },
            {"notReg", "This is not a custom placed animal" },
            {"RemovedAnimal", "Resource deleted" },
            {"synAdd", "<color=orange>/cas add id </color><color=#939393>- Adds a new animal spawn</color>" },
            {"synRem", "<color=orange>/cas remove </color><color=#939393>- Remove the animal you are looking at</color>" },
            {"synRemNear", "<color=orange>/cas remove near <radius> </color><color=#939393>- Removes the animal spawns within <radius> (default 10M)</color>" },
            {"synAnimal", "<color=orange>/cas animals </color><color=#939393>- List available animal types and their ID</color>" },
            {"synWipe", "<color=orange>/cas wipe </color><color=#939393>- Wipes all custom placed animal spawns</color>" },
            {"synList", "<color=orange>/cas list </color><color=#939393>- Puts all animal spawn details to console</color>" },
            {"synNear", "<color=orange>/cas near XX </color><color=#939393>- Shows custom animal spawns in radius XX</color>" },
            {"wipedAll1", "Wiped {0} custom animal spawns" },
            {"foundResources", "Found {0} animal spawns near you"},
            {"noFind", "Couldn't find any animal spawns in radius: {0}M" },
            {"adding", "You have activated the animal tool. Look where you want to place and press shoot. Type /crs to end" },
            {"endAdd", "You have de-activated the animal tool" },
            {"removedNear", "Removed {0} animal spawns within a {1}M radius of your position" }
        };

        #endregion
    }
}