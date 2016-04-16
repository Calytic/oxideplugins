// Reference: RustBuild
// Reference: Newtonsoft.Json
// Requires: PathFinding

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

using Rust;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HumanNPC", "Reneb/Nogrod/Calytic", "0.3.2", ResourceId = 856)]
    public class HumanNPC : RustPlugin
    {
        //////////////////////////////////////////////////////
        ///  Fields
        //////////////////////////////////////////////////////
        private int playerLayer;
        private static int targetLayer;
        private static Vector3 Vector3Down;
        private static Vector3 jumpPosition;
        private static int groundLayer;
        private static int blockshootLayer;

        private static readonly FieldInfo modelStateField = typeof(BasePlayer).GetField("modelState", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        private static readonly FieldInfo viewangles = typeof(BasePlayer).GetField("viewAngles", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        private static readonly FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic));
        private static readonly Collider[] colBuffer = (Collider[]) typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic)).GetValue(null);

        Hash<ulong, HumanNPCInfo> humannpcs = new Hash<ulong, HumanNPCInfo>();

        private bool save;
        private StoredData storedData;
        private DynamicConfigFile data;
        private Vector3 eyesPosition;
        private string chat = "<color=#FA58AC>{0}:</color> ";

        [PluginReference]
        Plugin Kits;
        [PluginReference]
        Plugin Waypoints;

        private static PathFinding PathFinding;

        class StoredData
        {
            public HashSet<HumanNPCInfo> HumanNPCs = new HashSet<HumanNPCInfo>();
        }

        public class WaypointInfo
        {
            public float Speed;
            public Vector3 Position;

            public WaypointInfo(Vector3 position, float speed)
            {
                Speed = speed;
                Position = position;
            }
        }

        //////////////////////////////////////////////////////
        ///  class SpawnInfo
        ///  Spawn information, position & rotation
        ///  public => will be saved in the data file
        ///  non public => won't be saved in the data file
        //////////////////////////////////////////////////////
        public class SpawnInfo
        {
            public Vector3 position;
            public Quaternion rotation;

            public SpawnInfo(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }

            public string String()
            {
                return $"Pos{position} - Rot{rotation}";
            }
            public string ShortString()
            {
                return $"Pos({Math.Ceiling(position.x)},{Math.Ceiling(position.y)},{Math.Ceiling(position.z)})";
            }
        }

        //////////////////////////////////////////////////////
        ///  class HumanTrigger
        /// MonoBehaviour: managed by UnityEngine
        ///  This takes care of all collisions and area management of humanNPCs
        //////////////////////////////////////////////////////
        public class HumanTrigger : MonoBehaviour
        {
            HumanPlayer npc;

            private readonly HashSet<BasePlayer> triggerPlayers = new HashSet<BasePlayer>();

            public float collisionRadius;

            void Awake()
            {
                npc = GetComponent<HumanPlayer>();
                collisionRadius = npc.info.collisionRadius;
                InvokeRepeating("UpdateTriggerArea", 2f, 1.5f);
            }
            void OnDestroy()
            {
                //Interface.Oxide.LogInfo("Destroy child: {0}", child?.name);
                CancelInvoke("UpdateTriggerArea");
            }
            void UpdateTriggerArea()
            {
                var count = Physics.OverlapSphereNonAlloc(npc.player.transform.position, collisionRadius, colBuffer, targetLayer);
                var collidePlayers = new HashSet<BasePlayer>();
                for (int i = 0; i < count; i++)
                {
                    var collider = colBuffer[i];
                    colBuffer[i] = null;
                    var player = collider.GetComponentInParent<BasePlayer>();
                    if (player != null)
                    {
                        if (player == npc.player) continue;
                        collidePlayers.Add(player);
                        if (triggerPlayers.Add(player)) OnEnterCollision(player);
                        continue;
                    }
                    var ai = collider.GetComponentInParent<NPCAI>();
                    if (ai != null && ai.decider.hatesHumans)
                        npc.StartAttackingEntity(collider.GetComponentInParent<BaseNPC>());
                }

                var removePlayers = new HashSet<BasePlayer>();
                foreach (BasePlayer player in triggerPlayers)
                    if (!collidePlayers.Contains(player)) removePlayers.Add(player);
                foreach (BasePlayer player in removePlayers)
                {
                    triggerPlayers.Remove(player);
                    OnLeaveCollision(player);
                }
            }
            void OnEnterCollision(BasePlayer player)
            {
                Interface.Oxide.CallHook("OnEnterNPC", npc.player, player);
            }
            void OnLeaveCollision(BasePlayer player)
            {
                Interface.Oxide.CallHook("OnLeaveNPC", npc.player, player);
            }
        }

        //////////////////////////////////////////////////////
        ///  class HumanLocomotion
        /// MonoBehaviour: managed by UnityEngine
        ///  This takes care of all movements and attacks of HumanNPCs
        //////////////////////////////////////////////////////
        public class HumanLocomotion : MonoBehaviour
        {
            private HumanPlayer npc;
            public Vector3 StartPos = new Vector3(0f, 0f, 0f);
            public Vector3 EndPos = new Vector3(0f, 0f, 0f);
            public Vector3 LastPos = new Vector3(0f, 0f, 0f);
            private Vector3 nextPos = new Vector3(0f, 0f, 0f);
            private float waypointDone = 0f;
            private float secondsTaken = 0f;
            private float secondsToTake = 0f;

            public List<WaypointInfo> cachedWaypoints;
            private int currentWaypoint = -1;

            public float followDistance = 3.5f;
            private float lastHit = 0f;

            public int noPath = 0;
            public bool shouldMove = true;

            private float lastReload = 0f;
            private float startedReload = 0f;
            private bool reloading = false;

            private bool returning = false;

            public BaseCombatEntity attackEntity = null;
            public BaseEntity followEntity = null;
            public Vector3 targetPosition = default(Vector3);

            public List<Vector3> pathFinding;

            private ModelState model = null;
            private HeldEntity firstWeapon = null;

            public void Awake()
            {
                npc = GetComponent<HumanPlayer>();
                UpdateWaypoints();
                lastReload = Time.realtimeSinceStartup;

                model = GetModel();
                model.onground = true;
            }
            public void UpdateWaypoints()
            {
                if (string.IsNullOrEmpty(npc.info.waypoint)) return;
                var cwaypoints = Interface.Oxide.CallHook("GetWaypointsList", npc.info.waypoint);
                if (cwaypoints == null)
                    cachedWaypoints = null;
                else
                {
                    cachedWaypoints = new List<WaypointInfo>();
                    var lastPos = npc.info.spawnInfo.position;
                    var speed = GetSpeed();
                    foreach (var cwaypoint in (List<object>)cwaypoints)
                    {
                        foreach (var pair in (Dictionary<Vector3, float>)cwaypoint)
                        {
                            if (HumanNPC.PathFinding == null)
                            {
                                cachedWaypoints.Add(new WaypointInfo(pair.Key, pair.Value));
                                continue;
                            }
                            var temppathFinding = HumanNPC.PathFinding.Go(lastPos, pair.Key);
                            speed = pair.Value;
                            if (temppathFinding != null)
                            {
                                lastPos = pair.Key;
                                foreach (var vector3 in temppathFinding)
                                    cachedWaypoints.Add(new WaypointInfo(vector3, speed));
                            }
                            else
                            {
                                Interface.Oxide.LogInfo("Blocked waypoint? {0} for {1}", pair.Key, npc.player.displayName);
                                //cachedWaypoints.Add(new WaypointInfo(pair.Key, speed));
                            }
                        }
                    }
                    if (HumanNPC.PathFinding != null && lastPos != npc.info.spawnInfo.position)
                    {
                        var temppathFinding = HumanNPC.PathFinding.Go(lastPos, npc.info.spawnInfo.position);
                        if (temppathFinding != null)
                        {
                            foreach (var vector3 in temppathFinding)
                                cachedWaypoints.Add(new WaypointInfo(vector3, speed));
                        }
                        else
                        {
                            Interface.Oxide.LogInfo("Blocked waypoint to spawn? {0} for {1}", lastPos, npc.player.displayName);
                        }
                    }
                    //Interface.Oxide.LogInfo("Waypoints: {0} for {1}", cachedWaypoints.Count, npc.player.displayName);
                }
            }
            void FixedUpdate()
            {
                TryToMove();
            }
            public void TryToMove()
            {
                if (npc.player.IsDead() || npc.player.IsWounded()) return;
                if (targetPosition != default(Vector3)) ProcessFollow(targetPosition);
                else if (attackEntity is BaseCombatEntity) ProcessAttack(attackEntity);
                else if (followEntity is BaseEntity) ProcessFollow(followEntity.transform.position);
                else if (secondsTaken == 0f) GetNextPath();

                if (StartPos != EndPos) Execute_Move();
                if (waypointDone >= 1f) secondsTaken = 0f;
            }
            void Execute_Move()
            {
                if (!shouldMove) return;
                secondsTaken += Time.deltaTime;
                waypointDone = Mathf.InverseLerp(0f, secondsToTake, secondsTaken);
                nextPos = Vector3.Lerp(StartPos, EndPos, waypointDone);
                nextPos.y = GetMoveY(nextPos);
                npc.player.MovePosition(nextPos);
                npc.player.eyes.position = nextPos + new Vector3(0, 1.6f, 0);
                npc.player.TransformChanged();
                npc.player.UpdatePlayerCollider(true, false);

                model.onground = !IsSwimming();
            }

            public bool IsSwimming() {
                return WaterLevel.Test(npc.player.transform.position + new Vector3(0,0.65f,0));
            }

            public ModelState GetModel() {
                return (ModelState)modelStateField.GetValue(npc.player);
            }

            float GetSpeed(float speed = -1)
            {
                if (returning)
                    speed = 7;
                else if (speed == -1)
                    speed = npc.info.speed;

                if (IsSwimming())
                    speed = speed/2f;

                return speed;
            }
            void GetNextPath()
            {
                if (npc == null) npc = GetComponent<HumanPlayer>();
                LastPos = Vector3.zero;
                shouldMove = true;
                if (cachedWaypoints == null) {
                    shouldMove = false;
                    return;
                }
                Interface.Oxide.CallHook("OnNPCPosition", npc.player, npc.player.transform.position);
                if (currentWaypoint + 1 >= cachedWaypoints.Count)
                {
                    UpdateWaypoints();
                    currentWaypoint = -1;
                }
                currentWaypoint++;

                var wp = cachedWaypoints[currentWaypoint];
                SetMovementPoint(npc.player.transform.position, wp.Position, GetSpeed(wp.Speed));
                if (npc.player.transform.position == wp.Position)
                {
                    npc.DisableMove();
                    npc.Invoke("AllowMove", GetSpeed(wp.Speed));
                    return;
                }
            }

            public void SetMovementPoint(Vector3 startpos, Vector3 endpos, float s)
            {
                StartPos = startpos;

                if (endpos != Vector3.zero)
                {
                    EndPos = endpos;
                    EndPos.y = Math.Max(EndPos.y, TerrainMeta.HeightMap.GetHeight(EndPos));
                    if (StartPos != EndPos)
                        secondsToTake = Vector3.Distance(EndPos, StartPos) / s;
                    npc.LookTowards(EndPos);
                }
                else
                {
                    if (IsInvoking("PathFinding")) { CancelInvoke("PathFinding"); }
                }

                secondsTaken = 0f;
                waypointDone = 0f;
            }

            private bool HitChance(float chance = -1f)
            {
                if (chance < 0)
                    chance = npc.info.hitchance;
                return UnityEngine.Random.Range(1, 100) < (int)(chance * 100);
            }

            void Move(Vector3 position, float speed = -1) {
                if (speed == -1) {
                    speed = npc.info.speed;
                }

                if (waypointDone >= 1f)
                {
                    if (pathFinding != null && pathFinding.Count > 0) pathFinding.RemoveAt(pathFinding.Count - 1);
                    waypointDone = 0f;
                }
                if (pathFinding == null || pathFinding.Count < 1) return;
                shouldMove = true;

                if (waypointDone == 0f) SetMovementPoint(position, pathFinding[pathFinding.Count - 1], GetSpeed(speed));
            }

            void ProcessAttack(BaseCombatEntity entity)
            {
                if (entity != null && entity.IsAlive())
                {
                    var c_attackDistance = Vector3.Distance(entity.transform.position, npc.player.transform.position);
                    shouldMove = false;

                    bool validAttack = Vector3.Distance(LastPos, npc.player.transform.position) < npc.info.maxDistance && noPath < 5;

                    //Interface.Oxide.LogInfo("Entity: {0} {1} {2}", entity.GetType().FullName, entity.IsAlive(), validAttack);
                    if (validAttack)
                    {
                        if (c_attackDistance < npc.info.damageDistance && CanSee(npc.player, entity))
                        {
                            AttemptAttack(entity);
                            return;
                        }

                        Move(npc.player.transform.position);
                    }
                    else
                        npc.EndAttackingEntity();
                }
                else
                    npc.EndAttackingEntity();
            }

            public void ProcessFollow(Vector3 target)
            {
                var c_followDistance = Vector3.Distance(target, npc.player.transform.position);
                shouldMove = false;
                if (c_followDistance > followDistance && Vector3.Distance(LastPos, npc.player.transform.position) < npc.info.maxDistance && noPath < 5)
                {
                    Move(npc.player.transform.position);
                }
                else
                {
                    if (followEntity is BaseEntity)
                    {
                        npc.EndFollowingEntity(noPath < 5);
                    }
                    else if(targetPosition != default(Vector3))
                    {
                        npc.EndGo(noPath < 5);
                    }
                }
            }

            public void PathFinding()
            {
                Vector3 target = default(Vector3);

                if (attackEntity != null)
                {
                    //Vector3 diff = new Vector3(Core.Random.Range(-npc.info.attackDistance, npc.info.attackDistance), 0, Core.Random.Range(-npc.info.attackDistance, npc.info.attackDistance));
                    target = attackEntity.transform.position;// + diff;
                }
                else if (followEntity != null)
                {
                    target = followEntity.transform.position;
                }
                else if (targetPosition != default(Vector3))
                {
                    if (targetPosition == npc.player.transform.position)
                    {
                        returning = false;
                        targetPosition = default(Vector3);
                    }
                    target = targetPosition;
                }

                if (target != default(Vector3))
                {
                    PathFinding(new Vector3(target.x, GetMoveY(target), target.z));
                }
            }

            public void PathFinding(Vector3 targetPos)
            {
                if (gameObject == null) return;
                if (IsInvoking("PathFinding")) { CancelInvoke("PathFinding"); }
                if (GetSpeed() <= 0) return;

                var temppathFinding = HumanNPC.PathFinding?.Go(npc.player.transform.position, targetPos);

                if (temppathFinding == null)
                {
                    if (pathFinding == null || pathFinding.Count == 0)
                        noPath++;
                    else noPath = 0;
                    if (noPath < 5) Invoke("PathFinding", 2);
                }
                else
                {
                    noPath = 0;

                    pathFinding = temppathFinding;
                    pathFinding.Reverse();
                    waypointDone = 0f;
                    Invoke("PathFinding", pathFinding.Count / GetSpeed(npc.info.speed));
                }
            }

            public void GetBackToLastPos()
            {
                if (npc.player.transform.position == LastPos) return;
                targetPosition = LastPos;
                pathFinding = null;
                Invoke("PathFinding", 0);
                //SetMovementPoint(npc.player.transform.position, LastPos, 7f);
                //secondsTaken = 0.01f;
            }

            public void Enable()
            {
                if (GetSpeed() <= 0) return;
                enabled = true;
            }
            public void Disable() { enabled = false; }

            public float GetMoveY(Vector3 position)
            {
                if(IsSwimming()) {
                    float point = TerrainMeta.WaterMap.GetHeight(position) -0.65f;
                    float groundY = GetGroundY(position);
                    if(groundY > point) {
                        return groundY;
                    }

                    return point-0.65f;
                }

                return GetGroundY(position);
            }

            public float GetGroundY(Vector3 position) {
                position = position + Vector3.up;
                RaycastHit hitinfo;
                if (Physics.Raycast(position, Vector3Down, out hitinfo, 100f, groundLayer))
                {
                    return hitinfo.point.y;
                }
                return position.y - .5f;
            }

            public void CreateProjectileEffect(BaseCombatEntity target, Item attackitem, float dmg, bool miss = false) {
                var baseProjectile = attackitem.GetHeldEntity() as BaseProjectile;
                if (baseProjectile == null) return;

                if (!npc.info.needsAmmo || npc.player.inventory.Take(null, baseProjectile.primaryMagazine.ammoType.itemid, 1) > 0) {
                    var component = baseProjectile.primaryMagazine.ammoType.GetComponent<ItemModProjectile>();
                    if (component == null) return;
                    npc.ForceSignalAttack();
                    npc.LookTowards(target.transform.position);

                    var source = npc.player.transform.position + npc.player.GetOffset() + (Quaternion.LookRotation(target.transform.position - npc.player.transform.position) * baseProjectile.ikHold_max.position);
                    var dir = (target.transform.position + npc.player.GetOffset() - source).normalized;
                    var vector32 = ((dir * (component.projectileVelocity + UnityEngine.Random.Range(-component.projectileVelocityRandom, component.projectileVelocityRandom))));

                    RaycastHit raycastHit;
                    if (!Physics.SphereCast(source - dir, 1f, vector32.normalized, out raycastHit, npc.info.attackDistance + 5f, targetLayer))
                    {
                        return;
                    }

                    if(miss) {
                        float aimConeMin = 0.8f;
                        float aimConeMax = 0.8f;
                        var aimOffset = Quaternion.Euler(UnityEngine.Random.Range((float)(-aimConeMin * 0.5), aimConeMax * 0.5f), UnityEngine.Random.Range((float)(-aimConeMax * 0.5), aimConeMax * 0.5f), UnityEngine.Random.Range((float)(-aimConeMax * 0.5), aimConeMax * 0.5f)) * npc.player.eyes.HeadForward();
                        vector32 = aimOffset + vector32;
                    }

                    Effect.server.Run(baseProjectile.attackFX.resourcePath, baseProjectile, StringPool.Get(baseProjectile.handBone), Vector3.zero, Vector3.forward, null, false);
                    var effect = new Effect();
                    effect.Init(Effect.Type.Projectile, source, vector32.normalized);
                    effect.scale = vector32.magnitude;
                    effect.pooledString = component.projectileObject.resourcePath;
                    effect.number = UnityEngine.Random.Range(0, 2147483647);
                    EffectNetwork.Send(effect);

                    Vector3 dest = default(Vector3);

                    if(miss) {
                        dmg = 0;
                        dest = raycastHit.point;
                    } else {
                        dest = target.transform.position;
                    }
                    var hitInfo = new HitInfo(npc.player, DamageType.Bullet, dmg, dest)
                    {
                        DidHit = !miss,
                        HitEntity = target,
                        PointStart = source,
                        PointEnd = raycastHit.point,
                        HitPositionWorld = dest,
                        HitNormalWorld = -dir,
                        WeaponPrefab = GameManager.server.FindPrefab(StringPool.Get(baseProjectile.prefabID)).GetComponent<AttackEntity>(),
                        Weapon = (AttackEntity)firstWeapon,
                        HitMaterial = StringPool.Get("Flesh")
                    };
                    target.OnAttacked(hitInfo);
                    Effect.server.ImpactEffect(hitInfo);
                }
            }

            public void AttemptAttack(BaseCombatEntity entity) {
                if (npc.info.needsAmmo && !reloading && Time.realtimeSinceStartup > lastReload + npc.info.fireDuration)
                {
                    reloading = true;
                    entity.SignalBroadcast(BaseEntity.Signal.Reload, string.Empty, null);
                    startedReload = Time.realtimeSinceStartup;
                    //PlayReload(npc);
                    return;
                }
                if (reloading && Time.realtimeSinceStartup > startedReload + npc.info.reloadDuration)
                {
                    reloading = false;
                    lastReload = Time.realtimeSinceStartup;
                    return;
                }
                if (!reloading && Time.realtimeSinceStartup > lastHit + npc.info.damageInterval)
                {
                    lastHit = Time.realtimeSinceStartup;
                    DoAttack(entity, !HitChance());
                }
            }


            public void DoAttack(BaseCombatEntity target, bool miss = false)
            {
                if (npc == null) return;
                if(firstWeapon == null || (firstWeapon != null && (firstWeapon.isDestroyed || !npc.HasAmmo(firstWeapon.GetItem())))) {
                    firstWeapon = npc.EquipFirstWeapon();
                    npc.SetActive(0);
                }

                if (firstWeapon != null)
                {
                    Item attackitem = firstWeapon.GetItem();
                    if (attackitem == null) return;
                    if (attackitem.uid != npc.player.svActiveItemID)
                        npc.SetActive(attackitem.uid);

                    float dmg = npc.info.damageAmount * UnityEngine.Random.Range(0.8f, 1.2f);
                    if (target is BaseNPC)
                        dmg *= 1.5f;
                    else if (target is AutoTurret)
                        dmg *= 3f;

                    if(weaponProjectile.ContainsKey(attackitem.info.shortname)) {
                        npc.ForceSignalGesture();
                        CreateProjectileEffect(target, attackitem, dmg, miss);
                    }
                    else
                    {
                        var hitInfo = new HitInfo(npc.player, DamageType.Stab, dmg, target.transform.position)
                        {
                            PointStart = npc.player.transform.position,
                            PointEnd = target.transform.position
                        };
                        target.SendMessage("OnAttacked", hitInfo, SendMessageOptions.DontRequireReceiver);
                        npc.ForceSignalAttack();
                    }
                }
            }
        }

        //////////////////////////////////////////////////////
        ///  class HumanPlayer : MonoBehaviour
        ///  MonoBehaviour: managed by UnityEngine
        /// Takes care of all the sub categories of the HumanNPCs
        //////////////////////////////////////////////////////
        public class HumanPlayer : MonoBehaviour
        {
            public HumanNPCInfo info;
            public HumanLocomotion locomotion;
            public HumanTrigger trigger;
            public ProtectionProperties protection;

            public BasePlayer player;

            public float lastMessage;

            public List<TuneNote> tunetoplay = new List<TuneNote>();
            public int currentnote = 0;
            Effect effectP = new Effect("assets/prefabs/instruments/guitar/effects/guitarpluck.prefab", new Vector3(0, 0, 0), Vector3.forward);
            Effect effectS = new Effect("assets/prefabs/instruments/guitar/effects/guitarpluck.prefab", new Vector3(0, 0, 0), Vector3.forward);

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                protection = ScriptableObject.CreateInstance<ProtectionProperties>();
            }

            public void SetInfo(HumanNPCInfo info, bool update = false)
            {
                this.info = info;
                if (info == null) return;
                displayName.SetValue(player, info.displayName);
                SetViewAngle(info.spawnInfo.rotation);
                player.syncPosition = true;
                if (!update)
                {
                    player.userID = info.userid;
                    player.UserIDString = player.userID.ToString();
                    player.MovePosition(info.spawnInfo.position);
                    player.eyes = player.eyes ?? player.GetComponent<PlayerEyes>();
                    player.eyes.position = info.spawnInfo.position + new Vector3(0, 1.6f, 0);
                    player.EndSleeping();
                    if (info.minstrel != null) PlayTune();
                    protection.Clear();
                    foreach (var pro in info.protections)
                        protection.Add(pro.Key, pro.Value);
                }
                if (locomotion != null) Destroy(locomotion);
                locomotion = player.gameObject.AddComponent<HumanLocomotion>();
                if (trigger != null) Destroy(trigger);
                trigger = player.gameObject.AddComponent<HumanTrigger>();
                lastMessage = Time.realtimeSinceStartup;
                DisableMove();
                AllowMove();
            }

            public void UpdateHealth(HumanNPCInfo info)
            {
                player.InitializeHealth(info.health, info.health);
                player.health = info.health;
            }

            public void AllowMove() { locomotion?.Enable(); }
            public void DisableMove() { locomotion?.Disable(); }
            public void TemporaryDisableMove(float thetime = -1f)
            {
                if (thetime == -1f) thetime = info.stopandtalkSeconds;
                DisableMove();
                if (gameObject == null) return;
                if (IsInvoking("AllowMove")) CancelInvoke("AllowMove");
                Invoke("AllowMove", thetime);
            }
            public void EndAttackingEntity(bool trigger = true)
            {
                if (locomotion.gameObject != null && locomotion.IsInvoking("PathFinding")) locomotion.CancelInvoke("PathFinding");
                locomotion.noPath = 0;
                locomotion.shouldMove = true;
                if (trigger)
                {
                    Interface.Oxide.CallHook("OnNPCStopTarget", player, locomotion.attackEntity);
                }
                locomotion.attackEntity = null;
                player.health = info.health;
                locomotion.GetBackToLastPos();
                SetActive(0);
            }
            public void EndFollowingEntity(bool trigger = true)
            {
                if (locomotion.IsInvoking("PathFinding")) locomotion.CancelInvoke("PathFinding");

                locomotion.noPath = 0;
                locomotion.shouldMove = true;
                if (trigger)
                {
                    Interface.Oxide.CallHook("OnNPCStopTarget", player, locomotion.followEntity);
                }
                locomotion.followEntity = null;
            }

            public void EndGo(bool trigger = true)
            {
                if (locomotion.IsInvoking("PathFinding")) locomotion.CancelInvoke("PathFinding");

                locomotion.noPath = 0;
                locomotion.shouldMove = true;

                if (trigger)
                {
                    Interface.Oxide.CallHook("OnNPCStopGo", player, locomotion.targetPosition);
                }
                locomotion.targetPosition = default(Vector3);
            }
            public void PlayTune()
            {
                if (info.minstrel == null || gameObject == null) return;
                if (tunetoplay.Count == 0) GetTune(this);
                if (tunetoplay.Count == 0) return;
                Invoke("PlayNote", 1);
            }
            public void PlayNote()
            {
                if (tunetoplay[currentnote].Pluck)
                {
                    effectP.worldPos = player.transform.position;
                    effectP.origin = player.transform.position;
                    effectP.scale = tunetoplay[currentnote].NoteScale;
                    EffectNetwork.Send(effectP);
                }
                else
                {
                    effectS.worldPos = player.transform.position;
                    effectS.origin = player.transform.position;
                    effectS.scale = tunetoplay[currentnote].NoteScale;
                    EffectNetwork.Send(effectS);
                }
                currentnote++;
                if (currentnote >= tunetoplay.Count)
                    currentnote = 0;
                Invoke("PlayNote", tunetoplay[currentnote].Delay);
            }
            public void StartAttackingEntity(BaseCombatEntity entity)
            {
                if (locomotion.attackEntity != null) return;
                if (Interface.Oxide.CallHook("OnNPCStartTarget", player, entity) == null)
                {
                    var item = GetFirstWeaponItem();
                    if (item != null)
                        SetActive(item.uid);
                    locomotion.attackEntity = entity;
                    locomotion.pathFinding = null;

                    if (locomotion.LastPos == Vector3.zero) locomotion.LastPos = player.transform.position;
                    if (gameObject != null && IsInvoking("AllowMove"))
                    {
                        CancelInvoke("AllowMove");
                        AllowMove();
                    }
                    locomotion.Invoke("PathFinding", 0);
                }
            }

            public void StartFollowingEntity(BaseEntity entity)
            {
                if (locomotion.targetPosition != default(Vector3))
                {
                    EndGo(false);
                }
                player.SendNetworkUpdate();
                locomotion.followEntity = entity;
                locomotion.pathFinding = null;

                if (locomotion.LastPos == Vector3.zero) locomotion.LastPos = player.transform.position;
                if (IsInvoking("AllowMove")) { CancelInvoke("AllowMove"); AllowMove(); }
                locomotion.Invoke("PathFinding", 0);
            }

            public void StartGo(Vector3 position)
            {
                if (locomotion.followEntity != null)
                {
                    EndFollowingEntity(false);
                }
                player.SendNetworkUpdate();
                locomotion.targetPosition = position;
                locomotion.pathFinding = null;

                if (locomotion.LastPos == Vector3.zero) locomotion.LastPos = player.transform.position;
                if (IsInvoking("AllowMove")) { CancelInvoke("AllowMove"); AllowMove(); }
                locomotion.Invoke("PathFinding", 0);
            }

            public HeldEntity GetCurrentWeapon()
            {
                foreach (Item item in player.inventory.containerBelt.itemList)
                {
                    BaseEntity heldEntity = item.GetHeldEntity();
                    if (heldEntity is HeldEntity && !heldEntity.HasFlag(BaseEntity.Flags.Disabled))
                        return (HeldEntity) heldEntity;
                }
                return null;
            }

            public Item GetFirstWeaponItem() {
                return GetFirstWeapon()?.GetItem();
            }

            public HeldEntity GetFirstWeapon()
            {
                foreach (Item item in player.inventory.containerBelt.itemList)
                {
                    if (item.CanBeHeld() && HasAmmo(item) && (item.info.category == ItemCategory.Weapon))
                        return item.GetHeldEntity() as HeldEntity;
                }
                return null;
            }

            public HeldEntity GetFirstTool()
            {
                foreach (Item item in player.inventory.containerBelt.itemList)
                {
                    if (item.CanBeHeld() && item.info.category == ItemCategory.Tool)
                        return item.GetHeldEntity() as HeldEntity;
                }
                return null;
            }

            public List<Item> GetAmmo(Item item)
            {
                List<Item> ammos = new List<Item>();
                AmmoTypes ammoType;
                if (!ammoTypes.TryGetValue(item.info.shortname, out ammoType))
                    return ammos;
                player.inventory.FindAmmo(ammos, ammoType);
                return ammos;
            }

            public bool HasAmmo(Item item)
            {
                if (!info.needsAmmo) return true;
                AmmoTypes ammoType;
                if (!ammoTypes.TryGetValue(item.info.shortname, out ammoType))
                    return true;

                List<Item> ammos = new List<Item>();
                player.inventory.FindAmmo(ammos, ammoType);
                return ammos.Count > 0;
            }

            public void UnequipAll()
            {
                if (player.inventory?.containerBelt == null) return;
                foreach (Item item in player.inventory.containerBelt.itemList)
                {
                    if (item.CanBeHeld())
                        (item.GetHeldEntity() as HeldEntity)?.SetHeld(false);
                }
            }

            public HeldEntity EquipFirstWeapon()
            {
                HeldEntity weapon = GetFirstWeapon();
                if (weapon != null)
                {
                    UnequipAll();
                    weapon.SetHeld(true);
                }
                return weapon;
            }

            public HeldEntity EquipFirstTool()
            {
                HeldEntity weapon = GetFirstTool();
                if (weapon != null)
                {
                    UnequipAll();
                    weapon.SetHeld(true);
                }
                return weapon;
            }

            public void SetActive(uint id)
            {
                player.svActiveItemID = id;
                player.SendNetworkUpdate();
            }

            void OnDestroy()
            {
                Destroy(locomotion);
                Destroy(trigger);
                Destroy(protection);
            }

            public void LookTowards(Vector3 pos)
            {
                if (pos != player.transform.position)
                    SetViewAngle(Quaternion.LookRotation(pos - player.transform.position));
            }


            public void ForceSignalGesture()
            {
                player.SignalBroadcast(BaseEntity.Signal.Gesture, "pickup_item", null);
            }

            public void ForceSignalAttack()
            {
                player.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty, null);
            }

            public void SetViewAngle(Quaternion ViewAngles)
            {
                viewangles.SetValue(player, ViewAngles.eulerAngles);
                player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        //////////////////////////////////////////////////////
        ///  class HumanNPCInfo
        ///  NPC information that will be saved inside the datafile
        ///  public => will be saved in the data file
        ///  non public => won't be saved in the data file
        //////////////////////////////////////////////////////
        public class HumanNPCInfo
        {
            public ulong userid;
            public string displayName;
            public bool invulnerability;
            public float health;
            public bool respawn;
            public float respawnSeconds;
            public SpawnInfo spawnInfo;
            public string waypoint;
            public float collisionRadius;
            public string spawnkit;
            public float damageAmount;
            public float damageDistance;
            public float damageInterval;
            public float attackDistance;
            public float maxDistance;
            public string minstrel;
            public bool hostile;
            public float speed;
            public bool stopandtalk;
            public float stopandtalkSeconds;
            public bool enable;
            public bool lootable;
            public float hitchance;
            public float fireDuration;
            public float reloadDuration;
            public bool needsAmmo;
            public bool defend;
            public List<string> message_hello;
            public List<string> message_bye;
            public List<string> message_use;
            public List<string> message_hurt;
            public List<string> message_kill;
            public Dictionary<DamageType, float> protections = new Dictionary<DamageType, float>();

            public HumanNPCInfo(ulong userid, Vector3 position, Quaternion rotation)
            {
                this.userid = userid;
                displayName = "NPC";
                invulnerability = true;
                health = 50;
                hostile = false;
                needsAmmo = true;
                respawn = true;
                respawnSeconds = 60;
                spawnInfo = new SpawnInfo(position, rotation);
                collisionRadius = 10;
                damageDistance = 3;
                damageAmount = 10;
                attackDistance = 100;
                maxDistance = 200;
                hitchance = 0.75f;
                fireDuration = 4f;
                reloadDuration = 3f;
                speed = 3;
                stopandtalk = true;
                stopandtalkSeconds = 3;
                enable = true;
                lootable = true;
                damageInterval = 2;
                for (var i = 0; i < (int)DamageType.LAST; i++)
                    protections[(DamageType)i] = 0f;
            }

            public HumanNPCInfo Clone(ulong userid)
            {
                return new HumanNPCInfo(userid, spawnInfo.position, spawnInfo.rotation)
                {
                    displayName = displayName,
                    invulnerability = invulnerability,
                    health = health,
                    respawn = respawn,
                    respawnSeconds = respawnSeconds,
                    waypoint = waypoint,
                    collisionRadius = collisionRadius,
                    spawnkit = spawnkit,
                    damageAmount = damageAmount,
                    damageDistance = damageDistance,
                    attackDistance = attackDistance,
                    maxDistance = maxDistance,
                    hostile = hostile,
                    speed = speed,
                    stopandtalk = stopandtalk,
                    stopandtalkSeconds = stopandtalkSeconds,
                    lootable = lootable,
                    defend = defend,
                    damageInterval = damageInterval,
                    minstrel = minstrel,
                    message_hello = message_hello?.ToList(),
                    message_bye = message_bye?.ToList(),
                    message_use = message_use?.ToList(),
                    message_hurt = message_hurt?.ToList(),
                    message_kill = message_kill?.ToList(),
                    needsAmmo = needsAmmo,
                    hitchance = hitchance,
                    fireDuration = fireDuration,
                    reloadDuration = fireDuration,
                    protections = protections?.ToDictionary(p => p.Key, p => p.Value)
                };
            }
        }

        class NPCEditor : MonoBehaviour
        {
            public BasePlayer player;
            public HumanPlayer targetNPC;
            void Awake()
            {
                player = GetComponent<BasePlayer>();
            }
        }

        public static Dictionary<string, AmmoTypes> ammoTypes = new Dictionary<string, AmmoTypes>();
        //{
        //    {"bow.hunting", AmmoTypes.BOW_ARROW},
        //    {"crossbow", AmmoTypes.BOW_ARROW},
        //    {"pistol.eoka", AmmoTypes.HANDMADE_SHELL},
        //    {"pistol.semiauto", AmmoTypes.PISTOL_9MM},
        //    {"pistol.revolver", AmmoTypes.PISTOL_9MM},
        //    {"rifle.ak", AmmoTypes.RIFLE_556MM},
        //    {"rifle.bolt", AmmoTypes.RIFLE_556MM},
        //    {"shotgun.pump", AmmoTypes.SHOTGUN_12GUAGE},
        //    {"shotgun.waterpipe", AmmoTypes.HANDMADE_SHELL},
        //    {"smg.2", AmmoTypes.PISTOL_9MM},
        //    {"smg.thompson", AmmoTypes.PISTOL_9MM}
        //};

        private static Dictionary<string, BaseProjectile> weaponProjectile = new Dictionary<string,BaseProjectile>();

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            ammoTypes = new Dictionary<string, AmmoTypes>();
            weaponProjectile = new Dictionary<string,BaseProjectile>();
            CheckCfg("Chat", ref chat);
            SaveConfig();
        }

        private static bool GetBoolValue(string value)
        {
            if (value == null) return false;
            value = value.Trim().ToLower();
            switch (value)
            {
                case "t":
                case "true":
                case "1":
                case "yes":
                case "y":
                case "on":
                    return true;
                default:
                    return false;
            }
        }

        void Loaded()
        {
            LoadData();
        }



        void Unload()
        {
            foreach (BasePlayer player in Resources.FindObjectsOfTypeAll<BasePlayer>())
            {
                if (player.userID >= 76560000000000000L || player.userID <= 0L || player.isDestroyed) continue;
                player.KillMessage();
            }
            var npcEditors = UnityEngine.Object.FindObjectsOfType<NPCEditor>();
            foreach (var gameObj in npcEditors)
                UnityEngine.Object.Destroy(gameObj);
            SaveData();
        }

        void SaveData()
        {
            if (storedData != null && save)
            {
                data.WriteObject(storedData);
                save = false;
            }
        }

        void LoadData()
        {
            data = Interface.Oxide.DataFileSystem.GetFile(nameof(HumanNPC));
            data.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            data.Settings.Converters = new JsonConverter[] {new SpawnInfoConverter(), new UnityQuaternionConverter(), new UnityVector3Converter()};

            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            data.Clear();
            foreach (var thenpc in storedData.HumanNPCs)
                humannpcs[thenpc.userid] = thenpc;
        }

        public class TuneNote
        {
            public float NoteScale, Delay;
            public bool Pluck;
            public TuneNote()
            {
            }
        }

        static void GetTune(HumanPlayer hp)
        {
            var tune = Interface.Oxide.CallHook("getTune", hp.info.minstrel);
            if (tune == null)
            {
                hp.CancelInvoke("PlayTune");
                return;
            }
            var newtune = new List<TuneNote>();
            foreach (var note in (List<object>)tune)
            {
                var newnote = new TuneNote();
                foreach (var pair in (Dictionary<string, object>)note)
                {
                    if (pair.Key == "NoteScale") newnote.NoteScale = Convert.ToSingle(pair.Value);
                    if (pair.Key == "Delay") newnote.Delay = Convert.ToSingle(pair.Value);
                    if (pair.Key == "Pluck") newnote.Pluck = Convert.ToBoolean(pair.Value);
                }
                newtune.Add(newnote);
            }
            hp.tunetoplay = newtune;
        }

        //////////////////////////////////////////////////////
        ///  Oxide Hooks
        //////////////////////////////////////////////////////

        //////////////////////////////////////////////////////
        ///  OnServerInitialized()
        ///  called when the server is done being initialized
        //////////////////////////////////////////////////////
        void OnServerInitialized()
        {
            eyesPosition = new Vector3(0f, 0.5f, 0f);
            jumpPosition = new Vector3(0f, 2f, 0f);
            Vector3Down = new Vector3(0f, -1f, 0f);
            PathFinding = (PathFinding)plugins.Find(nameof(PathFinding));
            playerLayer = LayerMask.GetMask("Player (Server)");
            targetLayer = LayerMask.GetMask("Player (Server)", "Trigger", "AI", "Deployed");
            groundLayer = LayerMask.GetMask("Construction", "Terrain", "World");
            blockshootLayer = LayerMask.GetMask("Construction", "Terrain", "World", "Deployed", "Tree", "Resource", "World", "Debris");

            foreach (var info in ItemManager.itemList)
            {
                var baseProjectile = info.GetComponent<ItemModEntity>()?.entityPrefab.Get().GetComponent<BaseProjectile>();
                if (baseProjectile == null) continue;
                weaponProjectile.Add(info.shortname, baseProjectile);

                var projectile = baseProjectile.primaryMagazine.ammoType.GetComponent<ItemModProjectile>();
                if (projectile != null && !ammoTypes.ContainsKey(info.shortname))
                {
                    ammoTypes.Add(info.shortname, projectile.ammoType);
                }
            }

            RefreshAllNPC();
        }

        //////////////////////////////////////////////////////
        ///  OnServerSave()
        ///  called when a server performs a save
        //////////////////////////////////////////////////////
        void OnServerSave()
        {
            SaveData();
        }

        void OnServerShutdown()
        {
            SaveData();
        }

        //////////////////////////////////////////////////////
        /// OnPlayerInput(BasePlayer player, InputState input)
        /// Called when a plugin presses a button
        //////////////////////////////////////////////////////
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!input.WasJustPressed(BUTTON.USE)) return;
            //Interface.Oxide.LogInfo("Use pressed: {0}", player.displayName);
            Quaternion currentRot;
            TryGetPlayerView(player, out currentRot);
            var hitpoints = Physics.RaycastAll(new Ray(player.transform.position + eyesPosition, currentRot * Vector3.forward), 5f, playerLayer);
            Array.Sort(hitpoints, (a, b) => a.distance == b.distance ? 0 : a.distance > b.distance ? 1 : -1);
            for (var i = 0; i < hitpoints.Length; i++)
            {
                //Interface.Oxide.LogInfo("Raycast: {0}", hitinfo.collider.name);
                var humanPlayer = hitpoints[i].collider.GetComponentInParent<HumanPlayer>();
                if (humanPlayer != null)
                {
                    if (humanPlayer.info.stopandtalk && humanPlayer.locomotion.attackEntity == null)
                    {
                        humanPlayer.LookTowards(player.transform.position);
                        humanPlayer.TemporaryDisableMove();
                    }
                    if (humanPlayer.info.message_use != null && humanPlayer.info.message_use.Count != 0)
                        SendMessage(humanPlayer, player, GetRandomMessage(humanPlayer.info.message_use));
                    Interface.Oxide.CallHook("OnUseNPC", humanPlayer.player, player);
                    break;
                }
            }
        }

        //////////////////////////////////////////////////////
        /// OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        /// Called when an entity gets attacked (can be anything, building, animal, player ..)
        //////////////////////////////////////////////////////
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            var humanPlayer = entity.GetComponent<HumanPlayer>();
            if (humanPlayer != null)
            {
                if (hitinfo.Initiator is BaseCombatEntity && !(hitinfo.Initiator is Barricade) && humanPlayer.info.defend) humanPlayer.StartAttackingEntity((BaseCombatEntity)hitinfo.Initiator);
                if (humanPlayer.info.message_hurt != null && humanPlayer.info.message_hurt.Count != 0)
                {
                    if (hitinfo.Initiator?.ToPlayer() != null)
                        SendMessage(humanPlayer, hitinfo.Initiator.ToPlayer(), GetRandomMessage(humanPlayer.info.message_hurt));
                }
                Interface.Oxide.CallHook("OnHitNPC", entity.GetComponent<BaseCombatEntity>(), hitinfo);
                if (humanPlayer.info.invulnerability)
                {
                    hitinfo.damageTypes = new DamageTypeList();
                    hitinfo.DoHitEffects = false;
                    hitinfo.HitMaterial = 0;
                }
                else
                    humanPlayer.protection.Scale(hitinfo.damageTypes);
            }
        }

        //////////////////////////////////////////////////////
        /// OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        /// Called when an entity gets killed (can be anything, building, animal, player ..)
        //////////////////////////////////////////////////////
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            var humanPlayer = entity.GetComponent<HumanPlayer>();
            if (humanPlayer != null)
            {
                if (!humanPlayer.info.lootable)
                    humanPlayer.player.inventory.Strip();
                if (humanPlayer.info.message_kill != null && humanPlayer.info.message_kill.Count > 0)
                {
                    if (hitinfo.Initiator?.ToPlayer() != null)
                        SendMessage(humanPlayer, hitinfo.Initiator.ToPlayer(), GetRandomMessage(humanPlayer.info.message_kill));
                }
                Interface.Oxide.CallHook("OnKillNPC", entity.GetComponent<BasePlayer>(), hitinfo);
                if (humanPlayer.info.respawn)
                    timer.Once(humanPlayer.info.respawnSeconds, () => SpawnOrRefresh(humanPlayer.info.userid));
            }
        }

        object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            var humanPlayer = target.GetComponent<HumanPlayer>();
            if (humanPlayer != null && !humanPlayer.info.lootable)
            {
                NextTick(looter.EndLooting);
                return false;
            }
            return null;
        }

        void OnLootPlayer(BasePlayer looter, BasePlayer target)
        {
            if (humannpcs[target.userID] != null)
                Interface.Oxide.CallHook("OnLootNPC", looter.inventory.loot, target, target.userID);
        }

        void OnLootEntity(BasePlayer looter, BaseEntity entity)
        {
            if (looter == null || !(entity is PlayerCorpse)) return;
            var userId = ((PlayerCorpse) entity).playerSteamID;
            if (humannpcs[userId] != null)
                Interface.Oxide.CallHook("OnLootNPC", looter.inventory.loot, entity, userId);
        }

        //////////////////////////////////////////////////////
        /// End of Oxide Hooks
        //////////////////////////////////////////////////////

        private Dictionary<ulong, HumanPlayer> cache = new Dictionary<ulong, HumanPlayer>();

        public HumanPlayer FindHumanPlayerByID(ulong userid)
        {
            HumanPlayer humanPlayer;
            if (cache.TryGetValue(userid, out humanPlayer))
                return humanPlayer;
            var allBasePlayer = Resources.FindObjectsOfTypeAll<HumanPlayer>();
            foreach (var humanplayer in allBasePlayer)
            {
                if (humanplayer.player.userID != userid) continue;
                cache[userid] = humanplayer;
                return humanplayer;
            }
            return null;
        }

        public HumanPlayer FindHumanPlayer(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;
            var allBasePlayer = Resources.FindObjectsOfTypeAll<HumanPlayer>();
            foreach (var humanplayer in allBasePlayer)
            {
                if (!nameOrId.Equals(humanplayer.player.UserIDString) && !humanplayer.player.displayName.Contains(nameOrId, CompareOptions.OrdinalIgnoreCase)) continue;
                return humanplayer;
            }
            return null;
        }

        BasePlayer FindPlayerByID(ulong userid)
        {
            var allBasePlayer = Resources.FindObjectsOfTypeAll<BasePlayer>();
            foreach (BasePlayer player in allBasePlayer)
            {
                if (player.userID == userid) return player;
            }
            return null;
        }

        void RefreshAllNPC()
        {
            List<ulong> npcspawned = new List<ulong>();
            foreach (KeyValuePair<ulong, HumanNPCInfo> pair in humannpcs)
            {
                if (!pair.Value.enable) continue;
                npcspawned.Add(pair.Key);
                SpawnOrRefresh(pair.Key);
            }
            foreach (BasePlayer player in Resources.FindObjectsOfTypeAll<BasePlayer>())
            {
                if (player.userID >= 76560000000000000L || player.userID <= 0L || npcspawned.Contains(player.userID) || player.isDestroyed) continue;
                player.KillMessage();
                PrintWarning($"Detected a HumanNPC with no data or disabled, deleting him: {player.userID} {player.displayName}");
            }
        }

        void SpawnOrRefresh(ulong userid)
        {
            BasePlayer findplayer = FindPlayerByID(userid);

            if (findplayer == null || findplayer.isDestroyed)
            {
                cache.Remove(userid);
                SpawnNPC(userid, false);
            }
            else RefreshNPC(findplayer, false);
        }

        void SpawnNPC(ulong userid, bool isediting)
        {
            HumanNPCInfo info;
            if (!humannpcs.TryGetValue(userid, out info)) return;
            if (!isediting && !info.enable) return;
            var newPlayer = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", info.spawnInfo.position, info.spawnInfo.rotation).ToPlayer();
            var humanPlayer = newPlayer.gameObject.AddComponent<HumanPlayer>();
            humanPlayer.SetInfo(info);
            newPlayer.Spawn(true);

            humanPlayer.UpdateHealth(info);
            cache[userid] = humanPlayer;
            UpdateInventory(humanPlayer);
            Interface.Oxide.CallHook("OnNPCRespawn", newPlayer);
            Puts("Spawned NPC: " + userid);
        }

        private void UpdateInventory(HumanPlayer humanPlayer)
        {
            humanPlayer.player.inventory.DoDestroy();
            humanPlayer.player.inventory.ServerInit(humanPlayer.player);
            if (!string.IsNullOrEmpty(humanPlayer.info.spawnkit))
            {
                //player.inventory.Strip();
                Kits.Call("GiveKit", humanPlayer.player, humanPlayer.info.spawnkit);
                humanPlayer.EquipFirstWeapon();
            }
            /*player.SV_ClothingChanged();
            if (humanPlayer.info.protections != null)
            {
                player.baseProtection.Clear();
                foreach (var protection in info.protections)
                    player.baseProtection.Add(protection.Key, protection.Value);
            }*/
            humanPlayer.player.inventory.ServerUpdate(0f);
        }

        void KillNpc(BasePlayer player)
        {
            if (player.userID >= 76560000000000000L || player.userID <= 0L || player.isDestroyed) return;
            cache.Remove(player.userID);
            player.KillMessage();
        }

        public void RefreshNPC(BasePlayer player, bool isediting)
        {
            HumanNPCInfo info;
            if (!humannpcs.TryGetValue(player.userID, out info)) return;
            KillNpc(player);
            if (!info.enable && !isediting)
            {
                Puts($"NPC was killed because he is disabled: {player.userID}");
                return;
            }
            SpawnOrRefresh(player.userID);
        }

        public void UpdateNPC(BasePlayer player, bool isediting)
        {
            HumanNPCInfo info;
            if (!humannpcs.TryGetValue(player.userID, out info)) return;
            if (!info.enable && !isediting)
            {
                KillNpc(player);
                Puts($"NPC was killed because he is disabled: {player.userID}");
                return;
            }
            if (player.GetComponent<HumanPlayer>() != null)
                UnityEngine.Object.Destroy(player.GetComponent<HumanPlayer>());
            var humanplayer = player.gameObject.AddComponent<HumanPlayer>();
            humanplayer.SetInfo(info, true);
            cache[player.userID] = humanplayer;
            Puts("Refreshed NPC: " + player.userID);
        }

        public HumanPlayer CreateNPC(Vector3 position, Quaternion currentRot, string name = "NPC", ulong clone = 0)
        {
            HumanNPCInfo npcInfo = null;
            var userId = (ulong) UnityEngine.Random.Range(0, 2147483647);
            if (clone != 0)
            {
                HumanNPCInfo tempInfo;
                if (humannpcs.TryGetValue(clone, out tempInfo))
                {
                    npcInfo = tempInfo.Clone(userId);
                    npcInfo.spawnInfo = new SpawnInfo(position, currentRot);
                }
            }
            if (npcInfo == null) npcInfo = new HumanNPCInfo(userId, position, currentRot);
            npcInfo.displayName = name;
            RemoveNPC(userId);

            humannpcs[userId] = npcInfo;
            storedData.HumanNPCs.Add(npcInfo);
            save = true;

            SpawnNPC(userId, true);

            return FindHumanPlayerByID(userId);
        }

        public void RemoveNPC(ulong npcid)
        {
            if (humannpcs.ContainsKey(npcid))
            {
                storedData.HumanNPCs.Remove(humannpcs[npcid]);
                humannpcs[npcid] = null;
            }
            cache.Remove(npcid);
            var npc = FindHumanPlayerByID(npcid);
            if (npc?.player != null && !npc.player.isDestroyed)
                npc.player.KillMessage();
        }

        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, "You don't have access to this command");
                return false;
            }
            return true;
        }

        bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input?.current == null) return false;
            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }

        bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            Vector3 sourceEye = sourcePos + new Vector3(0f, 1.5f, 0f);
            Ray ray = new Ray(sourceEye, sourceDir * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider.GetComponentInParent<TriggerBase>() == null && hit.distance < closestdist)
                {
                    closestdist = hit.distance;
                    closestEnt = hit.collider;
                    closestHitpoint = hit.point;
                }
            }

            if (closestEnt is bool) return false;
            return true;
        }

        private static bool CanSee(BasePlayer source, BaseEntity target)
        {
            if (Physics.Linecast(source.transform.position + jumpPosition, target.transform.position + jumpPosition, blockshootLayer))
                return false;
            return true;
        }

        private static string GetRandomMessage(List<string> messagelist) => messagelist[GetRandom(0, messagelist.Count)];
        private static int GetRandom(int min, int max) => UnityEngine.Random.Range(min, max);

        List<string> ListFromArgs(string[] args, int from)
        {
            var newlist = new List<string>();
            for (var i = from; i < args.Length; i++)
                newlist.Add(args[i]);
            return newlist;
        }

        //////////////////////////////////////////////////////////////////////////////
        /// Chat Commands
        //////////////////////////////////////////////////////////////////////////////
        [ChatCommand("npc_add")]
        void cmdChatNPCAdd(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (player.GetComponent<NPCEditor>() != null)
            {
                SendReply(player, "NPC Editor: Already editing an NPC, say /npc_end first");
                return;
            }
            Quaternion currentRot;
            if (!TryGetPlayerView(player, out currentRot))
            {
                SendReply(player, "Couldn't get player rotation");
                return;
            }

            HumanPlayer humanPlayer;
            if (args.Length > 0)
            {
                ulong targetId;
                if (!ulong.TryParse(args[0], out targetId))
                {
                    SendReply(player, "/npc_add [TARGETID]");
                    return;
                }
                HumanNPCInfo tempInfo;
                if (!humannpcs.TryGetValue(targetId, out tempInfo))
                {
                    SendReply(player, "Couldn't find the NPC");
                    return;
                }
                humanPlayer = CreateNPC(player.transform.position, currentRot, "NPC", targetId);
            }
            else
                humanPlayer = CreateNPC(player.transform.position, currentRot);
            if (humanPlayer == null)
            {
                SendReply(player, "Couldn't spawn the NPC");
                return;
            }
            var npcEditor = player.gameObject.AddComponent<NPCEditor>();
            npcEditor.targetNPC = humanPlayer;
        }

        [ChatCommand("npc_way")]
        void cmdChatNPCWay(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;

            HumanPlayer humanPlayer;
            if (args.Length == 0)
            {
                Quaternion currentRot;
                if (!TryGetPlayerView(player, out currentRot)) return;
                object closestEnt;
                Vector3 closestHitpoint;
                if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;
                humanPlayer = ((Collider)closestEnt).GetComponentInParent<HumanPlayer>();
                if (humanPlayer == null)
                {
                    SendReply(player, "This is not an NPC");
                    return;
                }
            }
            else if (args.Length > 0)
            {
                humanPlayer = FindHumanPlayer(args[0]);
                if (humanPlayer == null)
                {
                    ulong userid;
                    if (!ulong.TryParse(args[0], out userid))
                    {
                        SendReply(player, "/npc_way TargetId/Name");
                        return;
                    }
                    SpawnNPC(userid, true);
                    humanPlayer = FindHumanPlayerByID(userid);
                }
                if (humanPlayer == null)
                {
                    SendReply(player, "Couldn't Spawn the NPC");
                    return;
                }
            }
            else
            {
                SendReply(player, "You are not looking at an NPC or this userid doesn't exist");
                return;
            }
            if (humanPlayer.locomotion.cachedWaypoints == null)
            {
                SendReply(player, "The NPC has no waypoints");
                return;
            }
            var eyes = new Vector3(0, 1.6f, 0);
            var lastPos = humanPlayer.info.spawnInfo.position + eyes;
            for (var i = 0; i < humanPlayer.locomotion.cachedWaypoints.Count; i++)
            {
                var pos = humanPlayer.locomotion.cachedWaypoints[i].Position + eyes;
                //player.SendConsoleCommand("ddraw.sphere", 30f, Color.black, lastPos, .5f);
                player.SendConsoleCommand("ddraw.line", 30f, i % 2 == 0 ? Color.blue : Color.red, lastPos, pos);
                lastPos = pos;
            }
        }

        [ChatCommand("npc_edit")]
        void cmdChatNPCEdit(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (player.GetComponent<NPCEditor>() != null)
            {
                SendReply(player, "NPC Editor: Already editing an NPC, say /npc_end first");
                return;
            }

            HumanPlayer humanPlayer;
            if (args.Length == 0)
            {
                Quaternion currentRot;
                if (!TryGetPlayerView(player, out currentRot)) return;
                object closestEnt;
                Vector3 closestHitpoint;
                if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;
                humanPlayer = ((Collider)closestEnt).GetComponentInParent<HumanPlayer>();
                if (humanPlayer == null)
                {
                    SendReply(player, "This is not an NPC");
                    return;
                }
            }
            else if (args.Length > 0)
            {
                humanPlayer = FindHumanPlayer(args[0]);
                if (humanPlayer == null)
                {
                    ulong userid;
                    if (!ulong.TryParse(args[0], out userid))
                    {
                        SendReply(player, "/npc_edit TargetId/Name");
                        return;
                    }
                    SpawnNPC(userid, true);
                    humanPlayer = FindHumanPlayerByID(userid);
                }
                if (humanPlayer == null)
                {
                    SendReply(player, "Couldn't Spawn the NPC");
                    return;
                }
            }
            else
            {
                SendReply(player, "You are not looking at an NPC or this userid doesn't exist");
                return;
            }

            var npceditor = player.gameObject.AddComponent<NPCEditor>();
            npceditor.targetNPC = humanPlayer;
            SendReply(player, $"NPC Editor: Start Editing {npceditor.targetNPC.player.displayName} - {npceditor.targetNPC.player.userID}");
        }

        [ChatCommand("npc_list")]
        void cmdChatNPCList(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (humannpcs.Count == 0)
            {
                SendReply(player, "No NPC created yet");
                return;
            }

            SendReply(player, "==== NPCs ====");
            foreach (var pair in humannpcs) SendReply(player, $"{pair.Key} - {pair.Value.displayName} - {pair.Value.spawnInfo.ShortString()} {(pair.Value.enable ? "" : "- Disabled")}");
        }

        [ChatCommand("npc")]
        void cmdChatNPC(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            var npcEditor = player.GetComponent<NPCEditor>();
            if (npcEditor == null)
            {
                SendReply(player, "NPC Editor: You need to be editing an NPC, say /npc_add or /npc_edit");
                return;
            }
            if (args.Length == 0)
            {
                SendReply(player, "<color=#81F781>/npc attackdistance</color><color=#F2F5A9> XXX </color>=> <color=#D8D8D8>Distance between him and the target needed for the NPC to ignore the target and go back to spawn</color>");
                SendReply(player, "<color=#81F781>/npc bye</color> reset/<color=#F2F5A9>\"TEXT\" \"TEXT2\" \"TEXT3\" </color>=><color=#D8D8D8> Dont forgot the \", this is what NPC with say when a player gets away, multiple texts are possible</color>");
                SendReply(player, "<color=#81F781>/npc damageamount</color> <color=#F2F5A9>XXX </color>=> <color=#D8D8D8>Damage done by that NPC when he hits a player</color>");
                SendReply(player, "<color=#81F781>/npc damagedistance</color> <color=#F2F5A9>XXX </color>=> <color=#D8D8D8>Min distance for the NPC to hit a player (3 is default, maybe 20-30 needed for snipers?)</color>");
                SendReply(player, "<color=#81F781>/npc damageinterval</color> <color=#F2F5A9>XXX </color>=> <color=#D8D8D8>Time to wait before attacking again (2 seconds is default)</color>");
                SendReply(player, "<color=#81F781>/npc enable</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false</color><color=#D8D8D8>Enable/Disable the NPC, maybe save it for later?</color>");
                SendReply(player, "<color=#81F781>/npc health</color> <color=#F2F5A9>XXX </color>=> <color=#D8D8D8>To set the Health of the NPC</color>");
                SendReply(player, "<color=#81F781>/npc hello</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"TEXT\" \"TEXT2\" \"TEXT3\" </color>=> <color=#D8D8D8>Dont forgot the \", this what will be said when the player gets close to the NPC</color>");
                SendReply(player, "<color=#81F781>/npc hostile</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false</color> <color=#F2F5A9>XX </color>=> <color=#D8D8D8>To set it if the NPC is Hostile</color>");
                SendReply(player, "<color=#81F781>/npc hurt</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"TEXT\" \"TEXT2\" \"TEXT3\"</color> => <color=#D8D8D8>Dont forgot the \", set a message to tell the player when he hurts the NPC</color>");
                SendReply(player, "<color=#81F781>/npc invulnerable</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false </color>=> <color=#D8D8D8>To set the NPC invulnerable or not</color>");
                SendReply(player, "<color=#81F781>/npc kill</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"TEXT\" \"TEXT2\" \"TEXT3\" </color>=> <color=#D8D8D8>Dont forgot the \", set a message to tell the player when he kills the NPC</color>");
                SendReply(player, "<color=#81F781>/npc kit</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"KitName\" </color>=> <color=#D8D8D8>To set the kit of this NPC, requires the Kit plugin</color>");
                SendReply(player, "<color=#81F781>/npc lootable</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false</color> <color=#F2F5A9>XX </color>=> <color=#D8D8D8>To set it if the NPC corpse is lootable or not</color>");
                SendReply(player, "<color=#81F781>/npc maxdistance</color> <color=#F2F5A9>XXX </color>=><color=#D8D8D8> Max distance from the spawn point that the NPC can run from (while attacking a player)</color>");
                SendReply(player, "<color=#81F781>/npc minstrel</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"TuneName\" </color>=> <color=#D8D8D8>To set tunes to play by the NPC.</color>");
                SendReply(player, "<color=#81F781>/npc name</color> <color=#F2F5A9>\"THE NAME\"</color> =><color=#D8D8D8> To set a name to the NPC</color>");
                SendReply(player, "<color=#81F781>/npc radius</color> <color=#F2F5A9>XXX</color> =><color=#D8D8D8> Radius of which the NPC will detect the player</color>");
                SendReply(player, "<color=#81F781>/npc respawn</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false</color> <color=#F2F5A9>XX </color>=> <color=#D8D8D8>To set it to respawn on death after XX seconds, default is instant respawn</color>");
                SendReply(player, "<color=#81F781>/npc spawn</color> <color=#F2F5A9>\"new\" </color>=> <color=#D8D8D8>To set the new spawn location</color>");
                SendReply(player, "<color=#81F781>/npc speed</color><color=#F2F5A9> XXX </color>=> <color=#D8D8D8>To set the NPC running speed (while chasing a player)</color>");
                SendReply(player, "<color=#81F781>/npc stopandtalk</color> <color=#F2F5A9>true</color>/<color=#F6CECE>false</color> XX <color=#F2F5A9>XX </color>=> <color=#D8D8D8>To choose if the NPC should stop & look at the player that is talking to him</color>");
                SendReply(player, "<color=#81F781>/npc use</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"TEXT\" \"TEXT2\" \"TEXT3\"</color> => <color=#D8D8D8>Dont forgot the \", this what will be said when the player presses USE on the NPC</color>");
                SendReply(player, "<color=#81F781>/npc waypoints</color> <color=#F6CECE>reset</color>/<color=#F2F5A9>\"Waypoint list Name\" </color>=> <color=#D8D8D8>To set waypoints of an NPC, /npc_help for more informations</color>");
                return;
            }
            var param = args[0].ToLower();
            if (args.Length == 1)
            {
                string message;
                switch (param)
                {
                    case "name":
                        message = $"This NPC name is: {npcEditor.targetNPC.info.displayName}";
                        break;
                    case "enable":
                    case "enabled":
                        message = $"This NPC enabled: {npcEditor.targetNPC.info.enable}";
                        break;
                    case "invulnerable":
                    case "invulnerability":
                        message = $"This NPC invulnerability is set to: {npcEditor.targetNPC.info.invulnerability}";
                        break;
                    case "lootable":
                        message = $"This NPC lootable is set to: {npcEditor.targetNPC.info.lootable}";
                        break;
                    case "hostile":
                        message = $"This NPC hostility is set to: {npcEditor.targetNPC.info.hostile}";
                        break;
                    case "defend":
                        message = $"This NPC defend is set to: {npcEditor.targetNPC.info.defend}";
                        break;
                    case "needsammo":
                        message = $"This NPC needsAmmo is set to: {npcEditor.targetNPC.info.needsAmmo}";
                        break;
                    case "health":
                        message = $"This NPC Initial health is set to: {npcEditor.targetNPC.info.health}";
                        break;
                    case "attackdistance":
                        message = $"This Max Attack Distance is: {npcEditor.targetNPC.info.attackDistance}";
                        break;
                    case "damageamount":
                        message = $"This Damage amount is: {npcEditor.targetNPC.info.damageAmount}";
                        break;
                    case "damageinterval":
                        message = $"This Damage interval is: {npcEditor.targetNPC.info.damageInterval} seconds";
                        break;
                    case "maxdistance":
                        message = $"The Max Distance from spawn is: {npcEditor.targetNPC.info.maxDistance}";
                        break;
                    case "damagedistance":
                        message = $"This Damage distance is: {npcEditor.targetNPC.info.damageDistance}";
                        break;
                    case "radius":
                        message = $"This NPC Collision radius is set to: {npcEditor.targetNPC.info.collisionRadius}";
                        break;
                    case "respawn":
                        message = $"This NPC Respawn is set to: {npcEditor.targetNPC.info.respawn} after {npcEditor.targetNPC.info.respawnSeconds} seconds";
                        break;
                    case "spawn":
                        message = $"This NPC Spawn is set to: {npcEditor.targetNPC.info.spawnInfo.String()}";
                        break;
                    case "speed":
                        message = $"This NPC Chasing speed is: {npcEditor.targetNPC.info.speed}";
                        break;
                    case "stopandtalk":
                        message = $"This NPC stop to talk is set to: {npcEditor.targetNPC.info.stopandtalk} for {npcEditor.targetNPC.info.stopandtalkSeconds} seconds";
                        break;
                    case "waypoints":
                    case "waypoint":
                        message = string.IsNullOrEmpty(npcEditor.targetNPC.info.waypoint) ? "No waypoints set for this NPC yet" : $"This NPC waypoints are: {npcEditor.targetNPC.info.waypoint}";
                        break;
                    case "minstrel":
                        message = string.IsNullOrEmpty(npcEditor.targetNPC.info.minstrel) ? "No tune set for this NPC yet" : $"This NPC Tune is: {npcEditor.targetNPC.info.minstrel}";
                        break;
                    case "kit":
                    case "kits":
                        message = string.IsNullOrEmpty(npcEditor.targetNPC.info.spawnkit) ? "No spawn kits set for this NPC yet" : $"This NPC spawn kit is: {npcEditor.targetNPC.info.spawnkit}";
                        break;
                    case "hello":
                        if (npcEditor.targetNPC.info.message_hello == null || (npcEditor.targetNPC.info.message_hello.Count == 0))
                            message = "No hello message set yet";
                        else
                            message = $"This NPC will say hi: {npcEditor.targetNPC.info.message_hello.Count} different messages";
                        break;
                    case "bye":
                        if (npcEditor.targetNPC.info.message_bye == null || npcEditor.targetNPC.info.message_bye.Count == 0)
                            message = "No bye message set yet";
                        else
                            message = $"This NPC will say bye: {npcEditor.targetNPC.info.message_bye.Count} difference messages ";
                        break;
                    case "use":
                        if (npcEditor.targetNPC.info.message_use == null || npcEditor.targetNPC.info.message_use.Count == 0)
                            message = "No bye message set yet";
                        else
                            message = $"This NPC will say bye: {npcEditor.targetNPC.info.message_use.Count} different messages";
                        break;
                    case "hurt":
                        if (npcEditor.targetNPC.info.message_hurt == null || npcEditor.targetNPC.info.message_hurt.Count == 0)
                            message = "No hurt message set yet";
                        else
                            message = $"This NPC will say ouch: {npcEditor.targetNPC.info.message_hurt.Count} different messages";
                        break;
                    case "kill":
                        if (npcEditor.targetNPC.info.message_kill == null || npcEditor.targetNPC.info.message_kill.Count == 0)
                            message = "No kill message set yet";
                        else
                            message = $"This NPC will say a death message: {npcEditor.targetNPC.info.message_kill.Count} different messages";
                        break;
                    case "hitchance":
                        message = $"This NPC hit chance is: {npcEditor.targetNPC.info.hitchance}";
                        break;
                    case "fireduration":
                        message = $"This NPC fire duration is: {npcEditor.targetNPC.info.fireDuration}";
                        break;
                    case "reloadduration":
                        message = $"This NPC reload duration is: {npcEditor.targetNPC.info.reloadDuration}";
                        break;
                    default:
                        message = "Wrong Argument, /Npc for more informations";
                        break;
                }
                SendReply(player, message);
                return;
            }
            switch (param)
            {
                case "name":
                    npcEditor.targetNPC.info.displayName = args[1];
                    break;
                case "enable":
                case "enabled":
                    npcEditor.targetNPC.info.enable = GetBoolValue(args[1]);
                    break;
                case "invulnerable":
                case "invulnerability":
                    npcEditor.targetNPC.info.invulnerability = GetBoolValue(args[1]);
                    break;
                case "lootable":
                    npcEditor.targetNPC.info.lootable = GetBoolValue(args[1]);
                    break;
                case "hostile":
                    npcEditor.targetNPC.info.hostile = GetBoolValue(args[1]);
                    break;
                case "defend":
                    npcEditor.targetNPC.info.defend = GetBoolValue(args[1]);
                    break;
                case "needsammo":
                    npcEditor.targetNPC.info.needsAmmo = GetBoolValue(args[1]);
                    break;
                case "health":
                    npcEditor.targetNPC.info.health = Convert.ToSingle(args[1]);
                    break;
                case "attackdistance":
                    npcEditor.targetNPC.info.attackDistance = Convert.ToSingle(args[1]);
                    break;
                case "damageamount":
                    npcEditor.targetNPC.info.damageAmount = Convert.ToSingle(args[1]);
                    break;
                case "damageinterval":
                    npcEditor.targetNPC.info.damageInterval = Convert.ToSingle(args[1]);
                    break;
                case "maxdistance":
                    npcEditor.targetNPC.info.maxDistance = Convert.ToSingle(args[1]);
                    break;
                case "damagedistance":
                    npcEditor.targetNPC.info.damageDistance = Convert.ToSingle(args[1]);
                    break;
                case "radius":
                    npcEditor.targetNPC.info.collisionRadius = Convert.ToSingle(args[1]);
                    break;
                case "respawn":
                    npcEditor.targetNPC.info.respawn = GetBoolValue(args[1]);
                    npcEditor.targetNPC.info.respawnSeconds = 60;
                    if (args.Length > 2)
                        npcEditor.targetNPC.info.respawnSeconds = Convert.ToSingle(args[2]);
                    break;
                case "spawn":
                    Quaternion currentRot;
                    TryGetPlayerView(player, out currentRot);
                    var newSpawn = new SpawnInfo(player.transform.position, currentRot);
                    npcEditor.targetNPC.info.spawnInfo = newSpawn;
                    SendReply(player, $"This NPC Spawn now is set to: {newSpawn.String()}");
                    break;
                case "speed":
                    npcEditor.targetNPC.info.speed = Convert.ToSingle(args[1]);
                    break;
                case "stopandtalk":
                    npcEditor.targetNPC.info.stopandtalk = GetBoolValue(args[1]);
                    npcEditor.targetNPC.info.stopandtalkSeconds = 3;
                    if (args.Length > 2)
                        npcEditor.targetNPC.info.stopandtalkSeconds = Convert.ToSingle(args[2]);
                    break;
                case "waypoints":
                case "waypoint":
                    var name = args[1].ToLower();
                    if (name == "reset")
                        npcEditor.targetNPC.info.waypoint = null;
                    else if (Interface.Oxide.CallHook("GetWaypointsList", name) == null)
                    {
                        SendReply(player, "This waypoint doesn't exist");
                        return;
                    }
                    else npcEditor.targetNPC.info.waypoint = name;
                    break;
                case "minstrel":
                    npcEditor.targetNPC.info.minstrel = args[1];
                    break;
                case "kit":
                case "kits":
                    npcEditor.targetNPC.info.spawnkit = args[1].ToLower();
                    break;
                case "hello":
                    npcEditor.targetNPC.info.message_hello = args[1] == "reset" ? new List<string>() : ListFromArgs(args, 1);
                    break;
                case "bye":
                    npcEditor.targetNPC.info.message_bye = args[1] == "reset" ? new List<string>() : ListFromArgs(args, 1);
                    break;
                case "use":
                    npcEditor.targetNPC.info.message_use = args[1] == "reset" ? new List<string>() : ListFromArgs(args, 1);
                    break;
                case "hurt":
                    npcEditor.targetNPC.info.message_hurt = args[1] == "reset" ? new List<string>() : ListFromArgs(args, 1);
                    break;
                case "kill":
                    npcEditor.targetNPC.info.message_kill = args[1] == "reset" ? new List<string>() : ListFromArgs(args, 1);
                    break;
                case "hitchance":
                    npcEditor.targetNPC.info.hitchance = Convert.ToSingle(args[1]);
                    break;
                case "fireduration":
                    npcEditor.targetNPC.info.fireDuration = Convert.ToSingle(args[1]);
                    break;
                case "reloadduration":
                    npcEditor.targetNPC.info.reloadDuration = Convert.ToSingle(args[1]);
                    break;
                default:
                    SendReply(player, "Wrong Argument, /npc for more informations");
                    return;
            }
            SendReply(player, $"NPC Editor: Set {args[0]} to {args[1]}");
            save = true;
            RefreshNPC(npcEditor.targetNPC.player, true);
        }

        [ChatCommand("npc_end")]
        void cmdChatNPCEnd(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            var npcEditor = player.GetComponent<NPCEditor>();
            if (npcEditor == null)
            {
                SendReply(player, "NPC Editor: You are not editing any NPC");
                return;
            }
            if (!npcEditor.targetNPC.info.enable)
            {
                npcEditor.targetNPC.player.KillMessage();
                SendReply(player, "NPC Editor: The NPC you edited is disabled, killing him");
            }
            UnityEngine.Object.Destroy(npcEditor);
            SendReply(player, "NPC Editor: Ended");
        }

        [ChatCommand("npc_pathtest")]
        void cmdChatNPCPathTest(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            var npcEditor = player.GetComponent<NPCEditor>();
            if (npcEditor == null)
            {
                SendReply(player, "NPC Editor: You are not editing any NPC");
                return;
            }
            Quaternion currentRot;
            if (!TryGetPlayerView(player, out currentRot)) return;
            object closestEnt;
            Vector3 closestHitpoint;
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;
            Interface.Oxide.CallHook("FindAndFollowPath", npcEditor.targetNPC.player, npcEditor.targetNPC.player.transform.position, closestHitpoint);
        }

        [ChatCommand("npc_remove")]
        void cmdChatNPCRemove(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;

            HumanPlayer humanPlayer;
            if (args.Length == 0)
            {
                Quaternion currentRot;
                if (!TryGetPlayerView(player, out currentRot)) return;
                object closestEnt;
                Vector3 closestHitpoint;
                if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;
                humanPlayer = ((Collider)closestEnt).GetComponentInParent<HumanPlayer>();
                if (humanPlayer == null)
                {
                    SendReply(player, "This is not an NPC");
                    return;
                }
            }
            else if (args.Length > 0)
            {
                ulong userid;
                if (!ulong.TryParse(args[0], out userid))
                {
                    SendReply(player, "/npc_remove TARGETID");
                    return;
                }
                humanPlayer = FindHumanPlayerByID(userid);
                if (humanPlayer == null)
                {
                    SendReply(player, "This NPC doesn't exist");
                    return;
                }
            }
            else
            {
                SendReply(player, "You are not looking at an NPC or this userid doesn't exist");
                return;
            }

            var targetid = humanPlayer.player.userID;
            RemoveNPC(targetid);
            SendReply(player, $"NPC {targetid} Removed");
        }

        [ChatCommand("npc_reset")]
        void cmdChatNPCReset(BasePlayer player, string command, string[] args)
        {
            if (!hasAccess(player)) return;
            if (player.GetComponent<NPCEditor>() != null) UnityEngine.Object.Destroy(player.GetComponent<NPCEditor>());
            cache.Clear();
            humannpcs.Clear();
            storedData.HumanNPCs.Clear();
            save = true;
            SendReply(player, "All NPCs were removed");
            OnServerInitialized();
        }

        void SendMessage(HumanPlayer npc, BasePlayer target, string message)
        {
            if (Time.realtimeSinceStartup > npc.lastMessage + 0.1f)
            {
                SendReply(target, $"{chat}{message}", npc.player.displayName);
                npc.lastMessage = Time.realtimeSinceStartup;
            }
        }

        //////////////////////////////////////////////////////
        // NPC HOOKS:
        // will call ALL plugins
        //////////////////////////////////////////////////////

        //////////////////////////////////////////////////////
        /// OnHitNPC(BasePlayer npc, HitInfo hinfo)
        /// called when an NPC gets hit
        //////////////////////////////////////////////////////
        /*void OnHitNPC(BasePlayer npc, HitInfo hinfo)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnUseNPC(BasePlayer npc, BasePlayer player)
        ///  called when a player press USE while looking at the NPC (5m max)
        //////////////////////////////////////////////////////
        /*void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnEnterNPC(BasePlayer npc, BasePlayer player)
        ///  called when a player gets close to an NPC (default is in 10m radius)
        //////////////////////////////////////////////////////
        void OnEnterNPC(BasePlayer npc, BasePlayer player)
        {
            if (player.userID < 76560000000000000L) return;
            var humanPlayer = npc.GetComponent<HumanPlayer>();
            if (humanPlayer.info.message_hello != null && humanPlayer.info.message_hello.Count > 0)
                SendMessage(humanPlayer, player, GetRandomMessage(humanPlayer.info.message_hello));
            if (humanPlayer.info.hostile)
                humanPlayer.StartAttackingEntity(player);
        }

        //////////////////////////////////////////////////////
        ///  OnLeaveNPC(BasePlayer npc, BasePlayer player)
        ///  called when a player gets away from an NPC
        //////////////////////////////////////////////////////
        void OnLeaveNPC(BasePlayer npc, BasePlayer player)
        {
            if (player.userID < 76560000000000000L) return;
            var humanPlayer = npc.GetComponent<HumanPlayer>();
            if (humanPlayer.info.message_bye != null && humanPlayer.info.message_bye.Count > 0)
                SendMessage(humanPlayer, player, GetRandomMessage(humanPlayer.info.message_bye));
        }

        //////////////////////////////////////////////////////
        ///  OnKillNPC(BasePlayer npc, HitInfo hinfo)
        ///  called when an NPC gets killed
        //////////////////////////////////////////////////////
        /*void OnKillNPC(BasePlayer npc, HitInfo hinfo)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnNPCPosition(BasePlayer npc, Vector3 pos)
        ///  Called when an npc reachs a position
        //////////////////////////////////////////////////////
        /*void OnNPCPosition(BasePlayer npc, Vector3 pos)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnNPCRespawn(BasePlayer npc)
        ///  Called when an NPC respawns
        ///  here it will give an NPC a kit and set the first tool in the belt as the active weapon
        //////////////////////////////////////////////////////
        /*void OnNPCRespawn(BasePlayer npc)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnNPCStartAttacking(BasePlayer npc, BaseEntity target)
        ///  Called when an NPC start to target someone to attack
        ///  return anything will block the attack
        //////////////////////////////////////////////////////
        /*object OnNPCStartTarget(BasePlayer npc, BaseEntity target)
        {
            return null;
        }*/
        //////////////////////////////////////////////////////
        ///  OnNPCStopTarget(BasePlayer npc, BaseEntity target)
        ///  Called when an NPC stops targetting
        ///  no return;
        //////////////////////////////////////////////////////
        /*void OnNPCStopTarget(BasePlayer npc, BaseEntity target)
        {
        }*/

        //////////////////////////////////////////////////////
        ///  OnLootNPC(PlayerLoot loot, BaseEntity target, string npcuserID)
        ///  Called when an NPC gets looted
        ///  no return;
        //////////////////////////////////////////////////////
        /*void OnLootNPC(PlayerLoot loot, BaseEntity target, ulong npcuserID)
        {
        }*/

        private class UnityQuaternionConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var quaternion = (Quaternion)value;
                writer.WriteValue($"{quaternion.x} {quaternion.y} {quaternion.z} {quaternion.w}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Quaternion(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]), Convert.ToSingle(values[3]));
                }
                var o = JObject.Load(reader);
                return new Quaternion(Convert.ToSingle(o["rx"]), Convert.ToSingle(o["ry"]), Convert.ToSingle(o["rz"]), Convert.ToSingle(o["rw"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Quaternion);
            }
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
        }

        private class SpawnInfoConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {

            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var o = JObject.Load(reader);
                Vector3 position;
                Quaternion rotation;
                if (o["position"] != null)
                {
                    var values = Convert.ToString(o["position"]).Trim().Split(' ');
                    position = new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                    values = Convert.ToString(o["rotation"]).Trim().Split(' ');
                    rotation = new Quaternion(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]), Convert.ToSingle(values[3]));
                }
                else
                {
                    position = new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
                    rotation = new Quaternion(Convert.ToSingle(o["rx"]), Convert.ToSingle(o["ry"]), Convert.ToSingle(o["rz"]), Convert.ToSingle(o["rw"]));
                }
                return new SpawnInfo(position, rotation);
            }

            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(SpawnInfo);
            }
        }
    }
}
