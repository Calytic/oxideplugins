
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("Elevators", "Reneb", "1.0.1")]
    class Elevators : RustPlugin
    {
        [PluginReference]
        Plugin Waypoints;

        private static FieldInfo serverinput;
        StoredData storedData;
        static Hash<string, ElevatorInfo> elevators = new Hash<string, ElevatorInfo>();
        static Dictionary<string, string> nameToBlockPrefab = new Dictionary<string, string>();
        static List<Elevator> spawnedElevators = new List<Elevator>();
        static List<BaseCombatEntity> protectedBlock = new List<BaseCombatEntity>();
        public DamageTypeList emptyDamageType;
        // CACHED
        object closestEnt;
        Vector3 closestHitpoint;
        Quaternion currentRot;

        class StoredData
        {
            public HashSet<ElevatorInfo> Elevators = new HashSet<ElevatorInfo>();

            public StoredData()
            {
            }
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Elevators", storedData);
        }
        void LoadData()
        {
            elevators.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Elevators");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var elevator in storedData.Elevators)
                elevators[elevator.Name] = elevator;
        }
        public class ElevatorInfo
        {
            public string Name;
            public string PrefabName;
            public string WaypointsName;
            public string rx;
            public string ry;
            public string rz;
            public string rw;
            public string Grade;

            public ElevatorInfo()
            {

            }
            public ElevatorInfo(BuildingBlock block, string name, string waypoints)
            {
                rx = block.transform.rotation.x.ToString();
                ry = block.transform.rotation.y.ToString();
                rz = block.transform.rotation.z.ToString();
                rw = block.transform.rotation.w.ToString();
                Grade = GradeToNum(block.grade).ToString();
                this.Name = name;
                this.WaypointsName = waypoints;
                this.PrefabName = block.blockDefinition.fullName;
            }
        }
        ////////////////////////////////////////////////////// 
        ///  class WaypointInfo
        ///  Waypoint information, position & speed
        ///  public => will be saved in the data file
        ///  non public => won't be saved in the data file
        //////////////////////////////////////////////////////
        public class WaypointInfo
        {
            public string x;
            public string y;
            public string z;
            public string s;
            Vector3 position;
            float speed;

            public WaypointInfo(Vector3 position, float speed)
            {
                x = position.x.ToString();
                y = position.y.ToString();
                z = position.z.ToString();
                s = speed.ToString();

                this.position = position;
                this.speed = speed;
            }

            public Vector3 GetPosition()
            {
                if (position == Vector3.zero)
                    position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return position;
            }
            public float GetSpeed()
            {
                speed = Convert.ToSingle(s);
                return speed;
            }
        }
        /////////////////////////////////////////////////////
        ///  GetGrade(int lvl)
        ///  Convert grade number written by the players into the BuildingGrade.Enum used by rust
        /////////////////////////////////////////////////////
        static BuildingGrade.Enum NumToGrade(int lvl)
        {
            if (lvl == 0)
                return BuildingGrade.Enum.Twigs;
            else if (lvl == 1)
                return BuildingGrade.Enum.Wood;
            else if (lvl == 2)
                return BuildingGrade.Enum.Stone;
            else if (lvl == 3)
                return BuildingGrade.Enum.Metal;
            return BuildingGrade.Enum.TopTier;
        }
        static int GradeToNum(BuildingGrade.Enum lvl)
        {
            if (lvl == BuildingGrade.Enum.Twigs)
                return 0;
            else if (lvl == BuildingGrade.Enum.Wood)
                return 1;
            else if (lvl == BuildingGrade.Enum.Stone)
                return 2;
            else if (lvl == BuildingGrade.Enum.Metal)
                return 3;
            return 4;
        }

        public class Elevator : MonoBehaviour
        {
            public ElevatorInfo info;
            public List<WaypointInfo> waypoints;
            public TriggerBase trigger;
            public BuildingBlock block;
            public UnityEngine.Quaternion rotation;


            public Vector3 StartPos = new Vector3(0f, 0f, 0f);
            public Vector3 EndPos = new Vector3(0f, 0f, 0f);
            public Vector3 nextPos = new Vector3(0f, 0f, 0f);
            public float waypointDone = 0f;
            public float secondsTaken = 0f;
            public float secondsToTake = 0f;
            public float speed = 4f;
            public int currentWaypoint = 0;

            public List<BaseEntity> collidingPlayers = new List<BaseEntity>();

            public Elevator()
            {

            }

            public void SetInfo(ElevatorInfo info)
            {
               
                this.info = info;
                var cwaypoints = Interface.CallHook("GetWaypointsList", this.info.WaypointsName);
                if (cwaypoints == null)
                {
                    Debug.Log(string.Format("{0} was destroyed, informations are invalid. Did you set waypoints? or a PrefabName?", info.Name));
                    GameObject.Destroy(this);
                    return;
                }
                this.waypoints = new List<WaypointInfo>();
                foreach (var cwaypoint in (List<object>)cwaypoints)
                {
                    foreach (KeyValuePair<Vector3, float> pair in (Dictionary<Vector3,float>)cwaypoint)
                    {
                        this.waypoints.Add(new WaypointInfo(pair.Key, pair.Value));
                    }
                }
                if (this.waypoints.Count < 2)
                {
                    Debug.Log(string.Format("{0} waypoints were detected for {1}. Needs at least 2 waypoints. Destroying.", this.waypoints.Count.ToString(), info.Name));
                    GameObject.Destroy(this);
                    return;
                }
                this.rotation = new UnityEngine.Quaternion(Convert.ToSingle(info.rx), Convert.ToSingle(info.ry), Convert.ToSingle(info.rz), Convert.ToSingle(info.rw));
                this.block = CreateBuildingBlock(this.info.PrefabName, this.waypoints[0].GetPosition(), this.rotation, Convert.ToInt32(this.info.Grade));
                if(this.block == null)
                {
                    Debug.Log(string.Format("Something went wrong, couldn't create the BuildingBlock for {0}", info.Name));
                    GameObject.Destroy(this);
                    return;
                }
                protectedBlock.Add(this.block.GetComponent<BaseCombatEntity>());
                trigger = block.GetComponentInChildren<MeshCollider>().gameObject.AddComponent<TriggerBase>();
                trigger.gameObject.name = "Elevator";
                var newlayermask = new UnityEngine.LayerMask();
                newlayermask.value = 133120;   
                trigger.interestLayers = newlayermask;
                trigger.gameObject.layer = UnityEngine.LayerMask.NameToLayer("Trigger");
                spawnedElevators.Add(this);
            }
            void Awake()
            {
                enabled = false;
            } 
            void OnDestroy()
            {
                if (this.block != null)
                {
                    protectedBlock.Remove(this.block.GetComponent<BaseCombatEntity>());
                    this.block.KillMessage();
                }
                if (this.trigger != null) GameObject.Destroy(this.trigger);
            } 
            void FixedUpdate()
            {
                if (secondsTaken == 0f) GetNextPath();
                if (!enabled) return;
                if (StartPos != EndPos) Execute_Move();
                if (waypointDone >= 1f) secondsTaken = 0f;
            }
            void Execute_Move()
            {
                secondsTaken += Time.deltaTime;
                waypointDone = Mathf.InverseLerp(0f, secondsToTake, secondsTaken);
                nextPos = Vector3.Lerp(StartPos, EndPos, waypointDone);
                block.transform.position = nextPos;
                block.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
            }
            void GetNextPath()
            {
                if (currentWaypoint + 1 >= waypoints.Count)
                    currentWaypoint = -1;
                currentWaypoint++;
                if(currentWaypoint == 1 && collidingPlayers.Count==0)
                {
                    currentWaypoint = 0;
                    enabled = false;
                    return;
                }
                SetMovementPoint(block.transform.position, waypoints[currentWaypoint].GetPosition(), waypoints[currentWaypoint].GetSpeed());
                if (block.transform.position == waypoints[currentWaypoint].GetPosition()) { DeactivateMovement(); Invoke("ActivateMovement", waypoints[currentWaypoint].GetSpeed()); return; }
            }
            public void SetMovementPoint(Vector3 startpos, Vector3 endpos, float s)
            { 
                StartPos = startpos;
                EndPos = endpos;
                if (StartPos != EndPos)
                    secondsToTake = Vector3.Distance(EndPos, StartPos) / s;
                secondsTaken = 0f;
                waypointDone = 0f;
            }  
            public void ActivateMovement()
            {
                enabled = true;
            }
            public void DeactivateMovement()
            {
               enabled = false;
            }
        }
        void OnEntityEnter(TriggerBase triggerbase, BaseEntity entity)
        {
            if (triggerbase == null || entity == null) return;
            if (triggerbase.gameObject.name != "Elevator") return;
            foreach (Elevator ele in spawnedElevators)
            {
                if (ele.trigger == triggerbase)
                {
                    if (!ele.collidingPlayers.Contains(entity))
                        ele.collidingPlayers.Add(entity);
                    ele.ActivateMovement();
                } 
            }
        }
        void OnEntityLeave(TriggerBase triggerbase, BaseEntity entity)
        {
            if (triggerbase == null || entity == null) return;
            if (triggerbase.gameObject.name != "Elevator") return;
            foreach (Elevator ele in spawnedElevators)
            {
                if (ele.trigger == triggerbase)
                {
                    if (ele.collidingPlayers.Contains(entity))
                        ele.collidingPlayers.Remove(entity);
                }
            }
        }
        void NewElevator(ElevatorInfo info)
        {
            var objects = GameObject.FindObjectsOfType(typeof(Elevator));
            if (objects != null)
                foreach (Elevator gameObj in objects)
                {
                    if(gameObj.info.Name == info.Name)
                    {
                        GameObject.Destroy(gameObj);
                    }
                }
            GameObject gameobject = new UnityEngine.GameObject();
            gameobject.AddComponent<Elevator>();
            gameobject.GetComponent<Elevator>().SetInfo(info);
        }
        void Loaded()
        {
            emptyDamageType = new DamageTypeList();
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            LoadData();
        } 
        void Unload() 
        {
            var objects = GameObject.FindObjectsOfType(typeof(Elevator));
            if (objects != null)
                foreach (Elevator gameObj in objects)
                {
                    GameObject.Destroy(gameObj);
                } 
            foreach (Elevator ele in spawnedElevators)
            {
                if(ele != null) 
                    GameObject.Destroy(ele.gameObject);
            }                    
        }
        void OnServerInitialized()
        {
            foreach (KeyValuePair<string, ElevatorInfo> pair in elevators)
            {
                NewElevator(pair.Value);
            }
        }
        static BuildingBlock CreateBuildingBlock(string prefabname, Vector3 position, Quaternion rotation, int grade)
        {
            UnityEngine.GameObject prefab = GameManager.server.FindPrefab(prefabname);
            if (prefab == null) return null;
            UnityEngine.GameObject build = UnityEngine.Object.Instantiate(prefab);
            if (build == null) return null;
            BuildingBlock block = build.GetComponent<BuildingBlock>();
            if (block == null) return null;
            block.transform.position = position;
            block.transform.rotation = rotation;
            block.gameObject.SetActive(true);
            block.blockDefinition = PrefabAttribute.server.Find<Construction>(block.prefabID);
            block.Spawn(true); 
            block.SetGrade(NumToGrade(grade));
            block.health = block.MaxHealth();
            block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            return block;
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if(protectedBlock.Contains(entity))
            {
                CancelDamage(info);
            }
        }
        void CancelDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = emptyDamageType;
            hitinfo.DoHitEffects = false;
            hitinfo.HitMaterial = 0;
        }
        bool hasPermission(BasePlayer player)
        {
            if (player.net.connection.authLevel > 1) return true;
            return false;
        }
        static bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input == null)
                return false;
            if (input.current == null)
                return false;

            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }
        /////////////////////////////////////////////////////
        ///  TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        ///  Get the closest entity that the player is looking at
        /////////////////////////////////////////////////////
        bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            Vector3 sourceEye = sourcePos + new Vector3(0f, 1.5f, 0f);
            UnityEngine.Ray ray = new UnityEngine.Ray(sourceEye, sourceDir * Vector3.forward);

            var hits = UnityEngine.Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<TriggerBase>() == null)
                {
                    if (hit.distance < closestdist)
                    {
                        closestdist = hit.distance;
                        closestEnt = hit.collider;
                        closestHitpoint = hit.point;
                    }
                }
            }
            if (closestEnt is bool)
                return false;
            return true;
        }
        [ChatCommand("elevator_add")]
        void cmdChatElevatorAdd(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, "You don't have access to this command"); return; }
            if (args.Length < 2)
            { 
                SendReply(player, "/elevator_add NAME WAYPOINTS");
                return;
            } 
            var waypoints = Waypoints.Call("GetWaypointsList", args[1]);
            if(waypoints == null)
            {
                SendReply(player, "No waypoints with this name exist");
                return;
            }
            if( ((List<object>)waypoints).Count < 2)
            {
                SendReply(player, "To create an elevator you need at least 2 waypoints");
                return;
            }
            if (!TryGetPlayerView(player, out currentRot)) return;
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;
            Debug.Log(closestEnt.ToString());
            BuildingBlock block = ((Collider)closestEnt).GetComponentInParent<BuildingBlock>();
            if (block == null) 
            {
                SendReply(player, "What you are looking at is not a building block");
                return;
            }
            var newelevatorinfo = new ElevatorInfo(block, args[0], args[1]);
            if (elevators[newelevatorinfo.Name] != null) storedData.Elevators.Remove(elevators[newelevatorinfo.Name]);
            elevators[newelevatorinfo.Name] = newelevatorinfo;
            storedData.Elevators.Add(elevators[newelevatorinfo.Name]);
            SaveData(); 
            SendReply(player, "You've successfully created a new elevator");
            NewElevator(elevators[newelevatorinfo.Name]);
        }

        [ChatCommand("elevator_reset")]
        void cmdChatElevatorReset(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, "You don't have access to this command"); return; }
            foreach (KeyValuePair<string, ElevatorInfo> pair in elevators)
            {
                storedData.Elevators.Remove(elevators[pair.Key]);
            }
            elevators.Clear();
            SaveData();
            spawnedElevators.Clear();
            SendReply(player, "All elevators were removed");
            Unload();
        }

        [ChatCommand("elevator_remove")]
        void cmdChatElevatorRemove(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, "You don't have access to this command"); return; }
            if(args.Length == 0)
            {
                SendReply(player, "/elevator_remove NAME"); return;
            }
            if(elevators[args[0]] == null)
            {
                SendReply(player, "This elevator doesn't exist"); return;
            }
            foreach (Elevator ele in spawnedElevators)
            {
                if (ele.info.Name == elevators[args[0]].Name)
                    GameObject.Destroy(ele.gameObject);
            }
            storedData.Elevators.Remove(elevators[args[0]]);
            elevators[args[0]] = null;
            SendReply(player, string.Format("Elevator named {0} was deleted",args[0]));
            SaveData();
        }

        [ChatCommand("elevator_list")]
        void cmdChatElevatorList(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player)) { SendReply(player, "You don't have access to this command"); return; }
            if(elevators.Count == 0)
            {
                SendReply(player, "You don't have any elevators");
                return;
            }
            foreach (KeyValuePair<string, ElevatorInfo> pair in elevators)
            {
                SendReply(player, string.Format("{0} - {1}", pair.Key, pair.Value.PrefabName));
            }

        }
    }
}
