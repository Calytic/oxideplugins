using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;
using Rust;

namespace Oxide.Plugins
{
    [Info("Portals", "LaserHydra", "2.0.1", ResourceId = 1234)]
    [Description("Create portals and feel like in Star Trek")]
    class Portals : RustPlugin
    {
        #region Global Declaration

        private List<PortalInfo> portals = new List<PortalInfo>();
        public static Portals Instance = null;

        #endregion

        #region MonoBehaviour Classes

        private class PortalPlayerHandler : MonoBehaviour
        {
            public Timer timer;
            public BasePlayer player => gameObject.GetComponent<BasePlayer>();

            public void Teleport(PortalEntity portal)
            {
                if (portal.info.CanUse(player))
                {
                    PortalPoint otherPoint = portal.point.PointType == PortalPointType.Entrance ? portal.info.Exit : portal.info.Entrance;

                    Instance.Teleport(player, otherPoint.Location.Vector3);
                    Interface.CallHook("OnPortalUsed", player, JObject.FromObject(portal.info), JObject.FromObject(portal.point));
                }
            }
        }

        private class PortalEntity : MonoBehaviour
        {
            public PortalInfo info = new PortalInfo();
            public PortalPoint point = new PortalPoint();

            public static void Create(PortalInfo info, PortalPoint p)
            {
                p.GameObject = new GameObject();

                PortalEntity portal = p.GameObject.AddComponent<PortalEntity>();
                
                p.Sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", p.Location.Vector3, new Quaternion(), true).GetComponent<SphereEntity>();
                p.Sphere.currentRadius = 2;
                p.Sphere.lerpSpeed = 0f;
                p.Sphere.Spawn();

                p.GameObject.transform.position = p.Location.Vector3;

                portal.info = info;
                portal.point = p;
            }

            public void OnTriggerExit(Collider coll)
            {
                GameObject go = coll.gameObject;

                if (go.GetComponent<BasePlayer>())
                {
                    PortalPlayerHandler handler = go.GetComponent<PortalPlayerHandler>();

                    if (handler && handler.timer != null && !handler.timer.Destroyed)
                    {
                        handler.timer.Destroy();
                        Instance.PrintToChat(handler.player, Instance.GetMsg("Teleportation Cancelled"));
                    }
                }
            }

            public void OnTriggerEnter(Collider coll)
            {
                GameObject go = coll.gameObject;

                if (go.GetComponent<BasePlayer>())
                {
                    PortalPlayerHandler handler = go.GetComponent<PortalPlayerHandler>();

                    if (handler)
                    {
                        if (point.PointType == PortalPointType.Exit && info.OneWay)
                            return;

                        if (handler.player.IsSleeping())
                            return;

                        if (!info.CanUse(handler.player))
                        {
                            Instance.PrintToChat(handler.player, Instance.GetMsg("No Permission Portal"));
                            return;
                        }

                        Instance.PrintToChat(handler.player, Instance.GetMsg("Teleporting").Replace("{time}", info.TeleportationTime.ToString()));
                        handler.timer = Instance.timer.Once(info.TeleportationTime, () => handler.Teleport(this));
                    }
                }
            }

            public void UpdateCollider()
            {
                BoxCollider coll;

                if (gameObject.GetComponent<BoxCollider>())
                    coll = gameObject.GetComponent<BoxCollider>();
                else
                    coll = gameObject.AddComponent<BoxCollider>();

                coll.size = new Vector3(1, 2, 1);
                coll.isTrigger = true;
                coll.enabled = true;
            }

            public void Awake()
            {
                gameObject.name = "Portal";
                gameObject.layer = (int)Layer.Reserved1;

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                UpdateCollider();
            }
        }

        private enum PortalPointType
        {
            Entrance,
            Exit
        }

        #endregion

        #region Portal Classes

        private class PortalInfo
        {
            public string ID;
            public readonly PortalPoint Entrance = new PortalPoint { PointType = PortalPointType.Entrance };
            public readonly PortalPoint Exit = new PortalPoint { PointType = PortalPointType.Exit };
            public bool OneWay = true;
            public float TeleportationTime = 5f;
            public string RequiredPermission = "portals.use";

            private bool _created;

            private void Update()
            {
                Entrance.PointType = PortalPointType.Entrance;
                Exit.PointType = PortalPointType.Exit;
            }

            public void ReCreate()
            {
                Remove();
                Create();
            }

            public void Create()
            {
                Update();

                PortalEntity.Create(this, Entrance);
                PortalEntity.Create(this, Exit);

                _created = true;
            }

            public void Remove()
            {
                if (!_created)
                    return;

                Entrance.Sphere.Kill();
                Exit.Sphere.Kill();

                GameObject.Destroy(Entrance.GameObject);
                GameObject.Destroy(Exit.GameObject);
            }

            public bool CanUse(BasePlayer player) => Instance.permission.UserHasPermission(player.UserIDString, RequiredPermission);

            public static PortalInfo Find(string ID) => Instance.portals.Find((p) => p.ID == ID);

            public override int GetHashCode() => ID.GetHashCode();

            public PortalInfo(string ID)
            {
                this.ID = ID;
            }

            public PortalInfo()
            {
            }
        }

        private class PortalPoint
        {
            public readonly Location Location = new Location();
            internal PortalPointType PointType;
            internal GameObject GameObject;
            internal SphereEntity Sphere;
        }

        private class Location
        {
            public string _location = "0 0 0";

            internal Vector3 Vector3
            {
                get
                {
                    float[] vars = (from var in _location.Split(' ') select Convert.ToSingle(var)).ToArray();

                    return new Vector3(vars[0], vars[1], vars[2]);
                }
                set { _location = $"{value.x} {value.y} {value.z}"; }
            }
        }

        #endregion

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            Instance = this;

            RegisterPerm("admin");
            RegisterPerm("use");

            LoadData(out portals);
            LoadMessages();

            foreach (PortalInfo portal in portals)
            {
                permission.RegisterPermission(portal.RequiredPermission, this);
                portal.Create();
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        private void Unloaded()
        {
            foreach (var portal in portals)
                portal.Remove();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerDisconnected(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!player.gameObject.GetComponent<PortalPlayerHandler>())
                player.gameObject.AddComponent<PortalPlayerHandler>();
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (player.gameObject.GetComponent<PortalPlayerHandler>())
                Component.Destroy(player.gameObject.GetComponent<PortalPlayerHandler>());
        }

        #endregion

        #region Loading

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"No Permission Portal", "You don't have permission to use this portal."},
                {"Invalid ID", "{ID} is no valid ID. The ID must be a valid number!"},
                {"Portal Does Not Exist", "Portal {ID} does not exist."},
                {"Portal Entrance Set", "Entrance for Portal {ID} was set at your current location."},
                {"Portal Exit Set", "Exit for Portal {ID} was set at your current location."},
                {"Portal Removed", "Portal {ID} was removed."},
                {"Teleporting", "You entered a portal. You will be teleported in {time} seconds."},
                {"Teleportation Cancelled", "Teleportation cancelled as you left the portal before the teleportation process finished."},
                {"Portal List Empty", "There are no portals." },
                {"Portal List", "Portals: {portals}" }
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("portal")]
        private void cmdPortal(BasePlayer player, string cmd, string[] args)
        {
            if (!HasPerm(player.userID, "admin"))
            {
                SendReply(player, GetMsg("No Permission"));
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "Syntax: /portal <entrance|exit|remove|list> <ID>");
                return;
            }

            string ID;
            PortalInfo portal;

            switch (args[0])
            {
                case "entrance":

                    if (args.Length != 2)
                    {
                        SendReply(player, "Syntax: /portal entrance <ID>");
                        return;
                    }

                    ID = args[1];

                    portal = PortalInfo.Find(ID);

                    if (portal == null)
                    {
                        portal = new PortalInfo(ID);
                        portals.Add(portal);
                    }

                    portal.Entrance.Location.Vector3 = player.transform.position;
                    portal.ReCreate();

                    SaveData(portals);

                    SendReply(player, GetMsg("Portal Entrance Set").Replace("{ID}", args[1]));

                    break;

                case "exit":

                    if (args.Length != 2)
                    {
                        SendReply(player, "Syntax: /portal exit <ID>");
                        return;
                    }

                    ID = args[1];

                    portal = PortalInfo.Find(ID);

                    if (portal == null)
                    {
                        portal = new PortalInfo(ID);
                        portals.Add(portal);
                    }

                    portal.Exit.Location.Vector3 = player.transform.position;
                    portal.ReCreate();

                    SaveData(portals);

                    SendReply(player, GetMsg("Portal Exit Set").Replace("{ID}", args[1]));

                    break;

                case "remove":

                    if (args.Length != 2)
                    {
                        SendReply(player, "Syntax: /portal remove <ID>");
                        return;
                    }

                    ID = args[1];

                    portal = PortalInfo.Find(ID);

                    if (portal == null)
                    {
                        SendReply(player, GetMsg("Portal Does Not Exist").Replace("{ID}", args[1]));
                        return;
                    }

                    portal.Remove();
                    portals.Remove(portal);

                    SaveData(portals);

                    SendReply(player, GetMsg("Portal Removed").Replace("{ID}", args[1]));

                    break;

                case "list":

                    string portalList = portals.Count == 0
                        ? GetMsg("Portal List Empty")
                        : GetMsg("Portal List").Replace("{portals}",
                                string.Join("<color=#333> â </color>", portals.Select(p => $"<color=#C4FF00>{p.ID}</color>").ToArray()) );

                    SendReply(player, portalList);

                    break;

                default:
                    
                    SendReply(player, "Syntax: /portal <entrance|exit|remove> <ID>");

                    break;
            }
        }

        #endregion

        #region Helper

        #region Teleportation Helper

        private void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            
            player.StartSleeping();
            player.MovePosition(position);

            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);

            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);

            if (player.net?.connection == null)
                return;

            try
            {
                player.ClearEntityQueue(null);
            }
            catch
            {}

            player.SendFullSnapshot();
        }

        #endregion

        #region Finding Helper

        private BasePlayer GetPlayer(string searchedPlayer, BasePlayer player)
        {
            foreach (BasePlayer current in BasePlayer.activePlayerList)
                if (current.displayName.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<BasePlayer> foundPlayers =
                (from current in BasePlayer.activePlayerList
                 where current.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendReply(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.displayName).ToList();
                    string players = string.Join(", ", playerNames.ToArray());
                    SendReply(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        #endregion

        #region Data Helper

        private string DataFileName => Title.Replace(" ", string.Empty);

        private void LoadData<T>(out T data, string filename = null) => data = Interface.Oxide.DataFileSystem.ReadObject<T>(filename == null ? DataFileName : $"{DataFileName}/{filename}");

        private void SaveData<T>(T data, string filename = null) => Interface.Oxide.DataFileSystem.WriteObject(filename == null ? DataFileName : $"{DataFileName}/{filename}", data, true);

        #endregion

        #region Message Helper

        private string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        #endregion

        #region Permission Helper

        private void RegisterPerm(params string[] permArray)
        {
            string perm = string.Join(".", permArray);

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        private bool HasPerm(object uid, params string[] permArray)
        {
            string perm = string.Join(".", permArray);

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        private string PermissionPrefix => this.Title.Replace(" ", "").ToLower();

        #endregion

        #endregion
    }
}