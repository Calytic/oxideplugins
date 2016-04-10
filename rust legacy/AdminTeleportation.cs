using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("AdminTeleportation", "Reneb", "1.0.0", ResourceId = 940)]
    class AdminTeleportation : RustLegacyPlugin
    {
        /////////////////////////////
        // FIELDS
        /////////////////////////////
        NetUser cachedUser;
        string cachedSteamid;
        string cachedReason;
        string cachedName;
        Vector3 cachedPos;
        RaycastHit cachedRaycast;
        Vector3 vectorup = new Vector3(0f, 1f, 0f);
        int terrainLayer;
        public static Dictionary<NetUser, Vector3> teleportBack = new Dictionary<NetUser, Vector3>();

        private Core.Configuration.DynamicConfigFile Data;

        RustServerManagement management;

        /////////////////////////////
        // Data Management
        /////////////////////////////

        void LoadData()
        {
            Data = Interface.GetMod().DataFileSystem.GetDatafile("AdminTeleportation");
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("AdminTeleportation");
        }


        /////////////////////////////
        // Config Management
        /////////////////////////////

        static string notAllowed = "You are not allowed to use this command.";

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key]; 
            else
                Config[Key] = var;
        }

        void Init()
        {
            //CheckCfg<string>("Messages: Not Allowed", ref notAllowed);
            SaveConfig();
        }


        /////////////////////////////
        // Oxide Hooks
        /////////////////////////////

        void Loaded()
        {
            if (!permission.PermissionExists("canteleport")) permission.RegisterPermission("canteleport", this);
            terrainLayer = LayerMask.GetMask(new string[] { "Terrain" });
            LoadData();
        }
        void OnServerSave()
        {
            SaveData();
        }
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
        }


        /////////////////////////////
        // Teleportation Functions
        /////////////////////////////

        void DoTeleportToPlayer(NetUser source, NetUser target)
        {
            management.TeleportPlayerToPlayer(source.playerClient.netPlayer, target.playerClient.netPlayer);
            SendReply(source, string.Format("You teleported to {0}", target.playerClient.userName));
        }
        void TeleportToPos(NetUser source, float x, float y, float z)
        {
            if (Physics.Raycast(new Vector3(x, -1000f, z), vectorup, out cachedRaycast, Mathf.Infinity, terrainLayer))
            {
                if (cachedRaycast.point.y > y) y = cachedRaycast.point.y;
            }
            management.TeleportPlayerToWorld(source.playerClient.netPlayer, new Vector3(x, y, z));
            SendReply(source, string.Format("You teleported to {0} {1} {2}", x.ToString(), y.ToString(), z.ToString()));
        }

        /////////////////////////////
        // Random Functions
        /////////////////////////////
        bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
        
        [ChatCommand("tpsave")]
        void cmdChatTeleportSave(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            if (args.Length == 0) {
                SendReply(netuser, "Saved Teleportation Points:");
                foreach (KeyValuePair<string, object> pair in Data)
                {
                    if(pair.Value != null)
                        SendReply(netuser, string.Format("{0}", pair.Key));
                }
                return;
            }
            switch(args[0])
            {
                case "remove":
                case "delete":
                case "rem":
                case "del":
                    if (Data[args[1]] == null)
                    {
                        SendReply(netuser, "This teleportation location name doesn't exists");
                        return;
                    }
                    
                    Data[args[1]] = null;
                    SendReply(netuser, string.Format("Teleport location {0} was deleted", args[0]));
                    SaveData();
                    break;
                case "reset":
                    Data.Clear();
                    SaveData();
                    SendReply(netuser, "Teleport locations were reseted");
                break;
                default:
                    if(Data[args[0]] != null)
                    {
                        SendReply(netuser, "This teleportation location name already exists");
                        return;
                    }
                    var newsave = new Dictionary<string, object>();
                    newsave.Add("x", netuser.playerClient.lastKnownPosition.x.ToString());
                    newsave.Add("y", netuser.playerClient.lastKnownPosition.y.ToString());
                    newsave.Add("z", netuser.playerClient.lastKnownPosition.z.ToString());
                    Data[args[0]] = newsave;
                    SendReply(netuser, string.Format("Teleport location \"{0}\" was created on your position", args[0]));
                    SaveData();
                break;

            }
        }
        [ChatCommand("tpb")]
        void cmdChatTeleportBack(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            if(!teleportBack.ContainsKey(netuser))
            {
                SendReply(netuser, "You dont have any return point");
                return;
            }
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, teleportBack[netuser]);
            SendReply(netuser, "You teleported back.");
            teleportBack.Remove(netuser);
        }
        [ChatCommand("bring")]
        void cmdChatBring(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            if (args.Length == 0) { return; }
            else if (args.Length == 1)
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    DoTeleportToPlayer(targetuser, netuser);
                    SendReply(netuser, string.Format("You teleported {0} to you.",targetuser.playerClient.userName));
                    return;
                }
                SendReply(netuser, "The target player doesn't seem to exist");
                return;
            }
            return;
        }
        [ChatCommand("tp")]
        void cmdChatTeleport(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }

            // Nothing to do
            if (args.Length == 0) { return; }

            // Teleport self to a player
            else if (args.Length == 1)
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser != null)
                {
                    if (!teleportBack.ContainsKey(netuser)) teleportBack.Add(netuser, netuser.playerClient.lastKnownPosition);
                    DoTeleportToPlayer(netuser, targetuser);
                    return;
                }
                if(Data[args[0]] != null)
                {
                    if (!teleportBack.ContainsKey(netuser)) teleportBack.Add(netuser, netuser.playerClient.lastKnownPosition);
                    var targetpos = Data[args[0]] as Dictionary<string,object>;
                    TeleportToPos(netuser, Convert.ToSingle(targetpos["x"]), Convert.ToSingle(targetpos["y"]), Convert.ToSingle(targetpos["z"]));
                    return;
                }
                SendReply(netuser, "The target player or location doesn't seem to exist");
                return;
            }

            // Teleport to a player to another player, or teleport self to positions
            else if (args.Length == 2)
            {
                NetUser sourcePlayer = rust.FindPlayer(args[0]);
                if (sourcePlayer != null)
                {
                    NetUser targetPlayer = rust.FindPlayer(args[1]);
                    if (targetPlayer != null)
                    {
                        DoTeleportToPlayer(sourcePlayer, targetPlayer);
                        SendReply(netuser, string.Format("You successfully teleport {0} to {1}", sourcePlayer.playerClient.userName, targetPlayer.playerClient.userName));
                        return;
                    }
                    if (Data[args[1]] != null)
                    {
                        if (!teleportBack.ContainsKey(targetPlayer)) teleportBack.Add(targetPlayer, targetPlayer.playerClient.lastKnownPosition);
                        var targetpos = Data[args[1]] as Dictionary<string, object>;
                        TeleportToPos(targetPlayer, Convert.ToSingle(targetpos["x"]), Convert.ToSingle(targetpos["y"]), Convert.ToSingle(targetpos["z"]));
                        return;
                    }
                    SendReply(netuser, "Couldn't find the destination player");
                    return;
                }
                float x;
                float z;
                if (float.TryParse(args[0], out x) && float.TryParse(args[1], out z))
                {
                    if(!teleportBack.ContainsKey(netuser)) teleportBack.Add(netuser, netuser.playerClient.lastKnownPosition);
                    TeleportToPos(netuser, x, -1000f, z);
                    return;
                }
                SendReply(netuser, "Couldn't find the player to teleport");
                return;
            }

            // Teleport player to positions, or teleport self to positions
            else if (args.Length == 3)
            {
                float x;
                float z;
                NetUser sourcePlayer = rust.FindPlayer(args[0]);
                if (sourcePlayer != null)
                {
                    if (float.TryParse(args[1], out x) && float.TryParse(args[2], out z))
                    {
                        TeleportToPos(sourcePlayer, x, -1000f, z);
                        return;
                    }
                    SendReply(netuser, string.Format("Trying to teleport {0} to wrong coordinates: {1} {2}", sourcePlayer.playerClient.userName, args[1], args[2]));
                    return;
                }
                float y;
                if (float.TryParse(args[0], out x) && float.TryParse(args[1], out y) && float.TryParse(args[2], out z))
                {
                    if (!teleportBack.ContainsKey(netuser)) teleportBack.Add(netuser, netuser.playerClient.lastKnownPosition);
                    TeleportToPos(netuser, x, y, z);
                    return;
                }
                SendReply(netuser, string.Format("Couldn't teleport with there arguments: {0} {1} {2}", args[0], args[1], args[2]));
                return;
            }

            // Teleport player to positions
            else if (args.Length == 4)
            {
                float x;
                float y;
                float z;
                NetUser sourcePlayer = rust.FindPlayer(args[0]);
                if (sourcePlayer == null)
                {
                    SendReply(netuser, string.Format("{0} doesn't exist", args[0]));
                    return;
                }
                if (float.TryParse(args[1], out x) && float.TryParse(args[2], out y) && float.TryParse(args[3], out z))
                {
                    TeleportToPos(sourcePlayer, x, y, z);
                    return;
                }
                SendReply(netuser, string.Format("Wrong coordinates: {0} {1} {2}", args[1], args[2], args[3]));
                return;
            }
        }
        [ChatCommand("p")]
        void cmdChatPortalgun(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float heightAdjustment = 0f;
            if (args.Length > 0) float.TryParse(args[0], out heightAdjustment);
            
            if (Physics.Raycast(netuser.playerClient.controllable.GetComponent<Character>().eyesRay, out cachedRaycast))
            {
                cachedPos = cachedRaycast.point;
                cachedPos.y += heightAdjustment;
                management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
            }
        }
        [ChatCommand("up")]
        void cmdChatUp(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float heightAdjustment = 3f;
            if (args.Length > 0) float.TryParse(args[0], out heightAdjustment);
            cachedPos = netuser.playerClient.lastKnownPosition;
            cachedPos.y += heightAdjustment;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("down")]
        void cmdChatDown(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float heightAdjustment = 4f;
            if (args.Length > 0) float.TryParse(args[0], out heightAdjustment);
            cachedPos = netuser.playerClient.lastKnownPosition;
            cachedPos.y -= heightAdjustment;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("fw")]
        void cmdChatForward(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float dist = 4f;
            if (args.Length > 0) float.TryParse(args[0], out dist);
            cachedPos = netuser.playerClient.lastKnownPosition + (netuser.playerClient.controllable.GetComponent<Character>().eyesRotation * Vector3.forward)*dist;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("back")]
        void cmdChatBack(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float dist = 4f;
            if (args.Length > 0) float.TryParse(args[0], out dist);
            cachedPos = netuser.playerClient.lastKnownPosition - (netuser.playerClient.controllable.GetComponent<Character>().eyesRotation * Vector3.forward) * dist;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("right")]
        void cmdChatRight(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float dist = 4f;
            if (args.Length > 0) float.TryParse(args[0], out dist);
            cachedPos = netuser.playerClient.lastKnownPosition + (netuser.playerClient.controllable.GetComponent<Character>().eyesRotation * Vector3.right) * dist;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("left")]
        void cmdChatLeft(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            float dist = 4f;
            if (args.Length > 0) float.TryParse(args[0], out dist);
            cachedPos = netuser.playerClient.lastKnownPosition - (netuser.playerClient.controllable.GetComponent<Character>().eyesRotation * Vector3.right) * dist;
            management.TeleportPlayerToWorld(netuser.playerClient.netPlayer, cachedPos);
        }
        [ChatCommand("tphelp")]
        void cmdChatTPHelp(NetUser netuser, string command, string[] args)
        {
            if (!hasAccess(netuser, "canteleport")) { SendReply(netuser, notAllowed); return; }
            SendReply(netuser, "/tp PLAYERNAME => teleport to a player");
            SendReply(netuser, "/tp SAVEDLOCATION => teleport to a location that you've saved");
            SendReply(netuser, "/tp PLAYERNAME PLAYERNAME => teleport the first player to the second player");
            SendReply(netuser, "/tp PLAYERNAME SAVEDLOCATION => teleport a player to a saved location");
            SendReply(netuser, "/tp X Z => Teleport to the coordinates X & Z");
            SendReply(netuser, "/tp PLAYERNAME X Z => Teleport a source player to the coordinates X & Z");
            SendReply(netuser, "/tp X Y Z => Teleport yourself to the coordinates X Y Z ");
            SendReply(netuser, "/tp PLAYERNAME X Y Z => Teleport a source player to the coordinates X Y Z");
            SendReply(netuser, "/bring TARGETPLAYER => Teleport a player to you.");
            SendReply(netuser, "/tpsave => get the list of saved locations");
            SendReply(netuser, "/tpsave remove XX => remove a saved location");
            SendReply(netuser, "/tpsave XXX => Add a new saved location");
            SendReply(netuser, "/tpb => This only works for yourself, when you teleport to a player or to a position");
            SendReply(netuser, "/p => Portal gun, teleport to where you are looking at");
            SendReply(netuser, "/up XX => Go up Xm, 4 is default.");
            SendReply(netuser, "/down XX => Go down Xm, 4 is default.");
            SendReply(netuser, "/right XX => Go right Xm, 4 is default.");
            SendReply(netuser, "/left XX => Go left Xm, 4 is default.");
            SendReply(netuser, "/fw XX => Go forward Xm, 4 is default.");
            SendReply(netuser, "/back XX => Go back Xm, 4 is default.");
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser, "canteleport")) return;
            SendReply(netuser, "Teleport Commands: /tphelp");
        }
    }
}