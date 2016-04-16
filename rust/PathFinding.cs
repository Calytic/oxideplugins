using System;
using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using static UnityEngine.Vector3;

namespace Oxide.Plugins
{
    [Info("PathFinding", "Reneb / Nogrod", "1.0.1")]
    public class PathFinding : RustPlugin
    {
        private static readonly Vector3 Up = up;
        public sealed class PathFinder
        {
            private static readonly sbyte[,] Direction = {{0, -1}, {1, 0}, {0, 1}, {-1, 0}, {1, -1}, {1, 1}, {-1, 1}, {-1, -1}};
            public static readonly Vector3 EyesPosition;
            public static readonly uint Size;
            public static PriorityQueue OpenList;
            public static PathFindNode[,] Grid;

            static PathFinder()
            {
                EyesPosition = new Vector3(0f, 1.6f, 0f);
                OpenList = new PriorityQueue(MaxDepth * Direction.GetLength(0)); // 8 directions for each node
                Size = World.Size;
                Grid = new PathFindNode[Size, Size];
                //Interface.Oxide.LogInfo("Queue: {0} Grid: {1} Size: {2}", OpenList.MaxSize, Grid.Length, Size);
            }

            public List<Vector3> FindPath(Vector3 sourcePos, Vector3 targetPos)
            {
                //Interface.Oxide.LogInfo("Queue: {0} Grid: {1}", OpenList.MaxSize, Grid.Length);
                var closedList = new HashSet<PathFindNode>();

                var targetNode = new PathFindNode(targetPos);
                if (targetNode.X < 0 || targetNode.X >= Size || targetNode.Z < 0 || targetNode.Z >= Size) return null;
                Grid[targetNode.X, targetNode.Z] = targetNode;

                var startNode = new PathFindNode(sourcePos);
                if (startNode.X < 0 || startNode.X >= Size || startNode.Z < 0 || startNode.Z >= Size) return null;
                Grid[startNode.X, startNode.Z] = startNode;
                OpenList.Enqueue(startNode);

                while (OpenList.Count > 0)
                {
                    var currentNode = OpenList.Dequeue();
                    if (currentNode == targetNode)
                    {
                        Clear();
                        return RetracePath(startNode, targetNode);
                    }
                    closedList.Add(currentNode);
                    for (var i = 0; i < 8; i++)
                    {
                        var dirX = Direction[i, 0];
                        var dirZ = Direction[i, 1];
                        var x = currentNode.X + dirX;
                        var z = currentNode.Z + dirZ;
                        if (x < 0 || x >= Size || z < 0 || z >= Size) continue;
                        var neighbour = FindPathNodeOrCreate(x, z, currentNode.Position.y);
                        //Interface.Oxide.LogInfo("Checking neighbour: {0} {1} {2} {3} {4}", x, z, neighbour.Position, closedList.Contains(neighbour), neighbour.Walkable);
                        if (!neighbour.Walkable) continue;
                        var newGScore = currentNode.G + GetDistance(currentNode, neighbour);
                        if (newGScore >= neighbour.G && closedList.Contains(neighbour)) continue;
                        if (newGScore < neighbour.G || !OpenList.Contains(neighbour))
                        {
                            //foreach (var player in BasePlayer.activePlayerList)
                            //    player.SendConsoleCommand("ddraw.sphere", 30f, Color.black, neighbour.Position, .25f);
                            neighbour.G = newGScore;
                            neighbour.H = GetDistance(neighbour, targetNode);
                            neighbour.F = newGScore + neighbour.H;
                            neighbour.Parent = currentNode;
                            if (!OpenList.Contains(neighbour))
                                OpenList.Enqueue(neighbour);
                            else
                                OpenList.Update(neighbour);
                        }
                    }
                    if (closedList.Count > MaxDepth)
                    {
                        Interface.Oxide.LogWarning("[PathFinding] Hit MaxDepth!");
                        break;
                    }
                }
                Clear();
                return null;
            }

            private static void Clear()
            {
                OpenList.Clear();
                Array.Clear(Grid, 0, Grid.Length);
            }

            private static List<Vector3> RetracePath(PathFindNode startNode, PathFindNode endNode)
            {
                var path = new List<Vector3>();
                while (endNode != startNode)
                {
                    path.Add(endNode.Position);
                    endNode = endNode.Parent;
                }
                path.Reverse();
                //path.RemoveAt(0);
                return path;
            }

            private static int GetDistance(PathFindNode nodeA, PathFindNode nodeB)
            {
                var dstX = Math.Abs(nodeA.X - nodeB.X);
                var dstZ = Math.Abs(nodeA.Z - nodeB.Z);
                var dstY = Math.Abs(nodeA.Position.y - nodeB.Position.y);

                if (dstX > dstZ)
                    return 14*dstZ + 10*(dstX - dstZ) + (int)(10*dstY);
                return 14*dstX + 10*(dstZ - dstX) + (int)(10*dstY);
            }

            private static PathFindNode FindPathNodeOrCreate(int x, int z, float y)
            {
                var node = Grid[x, z];
                if (node != null) return node;
                var halfGrid = Size/2f;
                var groundPos = new Vector3(x - halfGrid, y, z - halfGrid);
                groundPos.y = TerrainMeta.HeightMap.GetHeight(groundPos);
                FindRawGroundPosition(groundPos, out groundPos);
                Grid[x, z] = node = new PathFindNode(groundPos);
                return node;
            }
        }

        public sealed class PriorityQueue
        {
            private readonly PathFindNode[] nodes;
            private int numNodes;

            public PriorityQueue(int maxNodes)
            {
                numNodes = 0;
                nodes = new PathFindNode[maxNodes + 1];
            }

            public int Count => numNodes;

            public int MaxSize => nodes.Length - 1;

            public void Clear()
            {
                Array.Clear(nodes, 1, numNodes);
                numNodes = 0;
            }

            public bool Contains(PathFindNode node)
            {
                return nodes[node.QueueIndex] == node;
            }

            public void Update(PathFindNode node)
            {
                SortUp(node);
            }

            public void Enqueue(PathFindNode node)
            {
                nodes[++numNodes] = node;
                node.QueueIndex = numNodes;
                SortUp(node);
            }

            private void Swap(PathFindNode node1, PathFindNode node2)
            {
                nodes[node1.QueueIndex] = node2;
                nodes[node2.QueueIndex] = node1;

                var temp = node1.QueueIndex;
                node1.QueueIndex = node2.QueueIndex;
                node2.QueueIndex = temp;
            }

            private void SortUp(PathFindNode node)
            {
                var parent = node.QueueIndex/2;
                while (parent > 0)
                {
                    var parentNode = nodes[parent];
                    if (CompareTo(parentNode, node) >= 0)
                        break;

                    Swap(node, parentNode);

                    parent = node.QueueIndex/2;
                }
            }

            private void SortDown(PathFindNode node)
            {
                var finalQueueIndex = node.QueueIndex;
                while (true)
                {
                    var newParent = node;
                    var childLeftIndex = 2*finalQueueIndex;

                    if (childLeftIndex > numNodes)
                    {
                        node.QueueIndex = finalQueueIndex;
                        nodes[finalQueueIndex] = node;
                        break;
                    }

                    var childLeft = nodes[childLeftIndex];
                    if (CompareTo(childLeft, newParent) >= 0)
                    {
                        newParent = childLeft;
                    }

                    var childRightIndex = childLeftIndex + 1;
                    if (childRightIndex <= numNodes)
                    {
                        var childRight = nodes[childRightIndex];
                        if (CompareTo(childRight, newParent) >= 0)
                        {
                            newParent = childRight;
                        }
                    }

                    if (newParent != node)
                    {
                        nodes[finalQueueIndex] = newParent;

                        var temp = newParent.QueueIndex;
                        newParent.QueueIndex = finalQueueIndex;
                        finalQueueIndex = temp;
                    }
                    else
                    {
                        node.QueueIndex = finalQueueIndex;
                        nodes[finalQueueIndex] = node;
                        break;
                    }
                }
            }

            public PathFindNode Dequeue()
            {
                var node = nodes[1];
                Remove(node);
                return node;
            }

            public void Remove(PathFindNode node)
            {
                if (node.QueueIndex == numNodes)
                {
                    nodes[numNodes--] = null;
                    return;
                }

                var formerLastNode = nodes[numNodes];
                Swap(node, formerLastNode);
                nodes[numNodes--] = null;

                var parentIndex = formerLastNode.QueueIndex/2;
                var parentNode = nodes[parentIndex];

                if (parentIndex > 0 && CompareTo(formerLastNode, parentNode) >= 0)
                    SortUp(formerLastNode);
                else
                    SortDown(formerLastNode);
            }

            private static int CompareTo(PathFindNode node, PathFindNode other)
            {
                if (node.F == other.F)
                {
                    if (node.H == other.H)
                        return 0;
                    if (node.H > other.H)
                        return -1;
                    return 1;
                }
                if (node.F > other.F)
                    return -1;
                return 1;
            }
        }

        public sealed class PathFindNode
        {
            public readonly int X;
            public readonly int Z;
            public int QueueIndex;
            public float H;
            public float G;
            public float F;
            public PathFindNode Parent;
            public Vector3 Position;
            public bool Walkable;

            public PathFindNode(Vector3 position)
            {
                Position = position;
                X = (int) Math.Round(position.x + PathFinder.Size/2f);
                Z = (int) Math.Round(position.z + PathFinder.Size/2f);
                Walkable = !Physics.CheckSphere(position + PathFinder.EyesPosition, .801f, blockLayer);
            }

            public override int GetHashCode()
            {
                return X << 16 | Z;
            }
        }

        public static bool FindRawGroundPosition(Vector3 sourcePos, out Vector3 groundPos)
        {
            groundPos = sourcePos;
            RaycastHit hitinfo;
            if (Physics.Raycast(sourcePos + Up, down, out hitinfo, groundLayer))
            {
                groundPos.y = Math.Max(hitinfo.point.y, TerrainMeta.HeightMap.GetHeight(groundPos));
                return true;
            }
            if (Physics.Raycast(sourcePos - Up, Up, out hitinfo, groundLayer))
            {
                groundPos.y = Math.Max(hitinfo.point.y, TerrainMeta.HeightMap.GetHeight(groundPos));
                return true;
            }
            return false;
        }

        private class PathFollower : MonoBehaviour
        {
            private readonly FieldInfo viewangles = typeof (BasePlayer).GetField("viewAngles", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            public List<Vector3> Paths = new List<Vector3>();
            public float secondsTaken;
            public float secondsToTake;
            public float waypointDone;
            public float speed;
            public Vector3 StartPos;
            public Vector3 EndPos;
            public Vector3 nextPos;
            public BaseEntity entity;
            public BasePlayer player;

            private void Awake()
            {
                entity = GetComponent<BaseEntity>();
                player = GetComponent<BasePlayer>() ?? player;
                speed = 4f;
            }

            private void Move()
            {
                if (secondsTaken == 0f) FindNextWaypoint();
                Execute_Move();
                if (waypointDone >= 1f) secondsTaken = 0f;
            }

            private void Execute_Move()
            {
                if (StartPos == EndPos) return;
                secondsTaken += Time.deltaTime;
                waypointDone = Mathf.InverseLerp(0f, secondsToTake, secondsTaken);
                nextPos = Lerp(StartPos, EndPos, waypointDone);
                entity.transform.position = nextPos;
                player?.ClientRPCPlayer(null, player, "ForcePositionTo", nextPos);
                entity.TransformChanged();
            }

            private void FindNextWaypoint()
            {
                if (Paths.Count == 0)
                {
                    StartPos = EndPos = zero;
                    enabled = false;
                    return;
                }
                SetMovementPoint(Paths[0], 4f);
            }

            public void SetMovementPoint(Vector3 endpos, float s)
            {
                StartPos = entity.transform.position;
                if (endpos != StartPos)
                {
                    EndPos = endpos;
                    secondsToTake = Distance(EndPos, StartPos)/s;
                    entity.transform.rotation = Quaternion.LookRotation(EndPos - StartPos);
                    if (player != null) SetViewAngle(player, entity.transform.rotation);
                    secondsTaken = 0f;
                    waypointDone = 0f;
                }
                Paths.RemoveAt(0);
            }

            private void SetViewAngle(BasePlayer player, Quaternion ViewAngles)
            {
                viewangles.SetValue(player, ViewAngles);
                player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }

            private void FixedUpdate()
            {
                Move();
            }
        }

        public static Vector3 jumpPosition = new Vector3(0f, 1f, 0f);
        public static int groundLayer;
        public static int blockLayer;
        private static int MaxDepth = 1000;
        private readonly FieldInfo serverinput = typeof (BasePlayer).GetField("serverInput", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

        protected override void LoadDefaultConfig()
        {
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T) Config[Key];
            else
                Config[Key] = var;
        }

        private void Init()
        {
            CheckCfg("Max Depth", ref MaxDepth);
            SaveConfig();
        }


        /////////////////////////////////////////////
        /// OXIDE HOOKS
        /////////////////////////////////////////////
        private void OnServerInitialized()
        {
            groundLayer = LayerMask.GetMask("Terrain", "World", "Construction");
            blockLayer = LayerMask.GetMask("World", "Construction", "Tree", "Deployed");

            timer.Once(30f, ResetPathFollowers);
        }

        private void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<PathFollower>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            PathFinder.OpenList = null;
            PathFinder.Grid = null;
        }

        /////////////////////////////////////////////
        /// Outside Plugin Calls
        /////////////////////////////////////////////
        private bool FindAndFollowPath(BaseEntity entity, Vector3 sourcePosition, Vector3 targetPosition)
        {
            //var curtime = Time.realtimeSinceStartup;
            var bestPath = FindBestPath(sourcePosition, targetPosition);
            //Debug.Log((Time.realtimeSinceStartup - curtime).ToString());
            if (bestPath == null) return false;
            FollowPath(entity, bestPath);
            return true;
        }

        private void FollowPath(BaseEntity entity, List<Vector3> pathpoints)
        {
            var pathfollower = entity.GetComponent<PathFollower>() ?? entity.gameObject.AddComponent<PathFollower>();
            pathfollower.Paths = pathpoints;
            pathfollower.enabled = true;
        }

        [HookMethod("FindBestPath")]
        public List<Vector3> FindBestPath(Vector3 sourcePosition, Vector3 targetPosition)
        {
            return FindLinePath(sourcePosition, targetPosition) ?? FindPath(sourcePosition, targetPosition);
        }

        public List<Vector3> Go(Vector3 source, Vector3 target)
        {
            return FindLinePath(source, target) ?? FindPath(source, target);
        }

        private List<Vector3> FindPath(Vector3 sourcePosition, Vector3 targetPosition)
        {
            //Puts("FindPath: {0} {1}", sourcePosition, targetPosition);
            return new PathFinder().FindPath(sourcePosition, targetPosition);
        }

        private List<Vector3> FindLinePath(Vector3 sourcePosition, Vector3 targetPosition)
        {
            var distance = (int) Math.Ceiling(Distance(sourcePosition, targetPosition));
            if (distance <= 0) return null;
            var straightPath = new List<Vector3>(new Vector3[distance]) {[distance - 1] = targetPosition};
            var currentPos = Lerp(sourcePosition, targetPosition, 1f / distance);
            Vector3 groundPosition;
            if (!FindRawGroundPosition(currentPos, out groundPosition)) return null;
            if (Distance(groundPosition, sourcePosition) > 2) return null;
            if (Physics.Linecast(sourcePosition + jumpPosition, groundPosition + jumpPosition, blockLayer)) return null;
            straightPath[0] = groundPosition;
            for (var i = 1; i < distance - 1; i++)
            {
                currentPos = Lerp(sourcePosition, targetPosition, (i + 1f)/distance);
                if (!FindRawGroundPosition(currentPos, out groundPosition)) return null;
                if (Distance(groundPosition, straightPath[i - 1]) > 2) return null;
                if (Physics.Linecast(straightPath[i - 1] + jumpPosition, groundPosition + jumpPosition, blockLayer)) return null;
                straightPath[i] = groundPosition;
            }
            if (Physics.Linecast((distance == 1 ? sourcePosition : straightPath[distance - 2]) + jumpPosition, targetPosition + jumpPosition, blockLayer)) return null;
            return straightPath;
        }

        /////////////////////////////////////////////
        /// Reset part of the plugin
        /////////////////////////////////////////////
        private void ResetPathFollowers()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<PathFollower>();
            foreach (var gameObj in objects)
                if (gameObj.Paths.Count == 0)
                    UnityEngine.Object.Destroy(gameObj);
        }

        /////////////////////////////////////////////
        /// Debug Command
        /////////////////////////////////////////////
        [ChatCommand("path")]
        private void cmdChatPath(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < 1) return;
            Quaternion currentRot;
            if (!TryGetPlayerView(player, out currentRot)) return;
            object closestEnt;
            Vector3 closestHitpoint;
            if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint)) return;

            FindAndFollowPath(player, player.transform.position, closestHitpoint);
        }

        private bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input?.current == null) return false;
            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }

        private bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            var sourceEye = sourcePos + PathFinder.EyesPosition;
            var ray = new Ray(sourceEye, sourceDir*forward);

            var hits = Physics.RaycastAll(ray);
            var closestdist = 999999f;
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
    }
}
