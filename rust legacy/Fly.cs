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
    [Info("Fly", "Reneb", "1.0.2", ResourceId = 934)]
    class Fly : RustLegacyPlugin
    {
        public static Vector3 heightAdjusment = new Vector3(0f, -0.75f, 0f);
        public static UnityEngine.Quaternion rotationDefault = new UnityEngine.Quaternion(0f, 0f, 0f,0f);
        public class FlyPlayer : MonoBehaviour
        {
            public PlayerClient playerClient;
            public DeployableObject lastObject;
            public Vector3 nextForward;
            public Character character;
            public RustServerManagement management;
            public StructureMaster lastMaster;
            public StructureComponent lastComponent;
            public float speed;
            void Awake()
            {
                playerClient = GetComponent<PlayerClient>();
                character = playerClient.controllable.GetComponent<Character>();
                management = RustServerManagement.Get();
                nextForward = character.origin;
                character.takeDamage.SetGodMode(true);
                NewObject();
            }
            void NewObject()
            {
                lastObject = NetCull.InstantiateStatic(";deploy_wood_box", nextForward + heightAdjusment, character.rotation).GetComponent<DeployableObject>();
                lastObject.SetupCreator(character.controllable);
            }
            void FixedUpdate()
            {
                if (character == null) GameObject.Destroy(this);
                if (speed == 0f) return;
                nextForward = nextForward + (character.eyesRotation*Vector3.forward)* speed;
                if(lastObject != null) NetCull.Destroy(lastObject.gameObject);
                NewObject();
                management.TeleportPlayerToWorld(playerClient.netPlayer, nextForward);
            } 
            public void Refresh()
            { 
                nextForward = character.origin;
            }
            void OnDestroy()
            {
                NetCull.Destroy(lastObject.gameObject);
                if (character != null) character.takeDamage.SetGodMode(false);
            }
        } 
        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(FlyPlayer));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }
        void Loaded()
        {
            if (!permission.PermissionExists("canfly")) permission.RegisterPermission("canfly", this);
        }
        bool hasAccess(NetUser netuser)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canfly");
        }
        [ChatCommand("fly")]
        void cmdChatFly(NetUser netuser, string command, string[] args)
        {
            if(!hasAccess(netuser)) { SendReply(netuser, "You dont have access to this command."); return; }
            if (args.Length == 0 && netuser.playerClient.GetComponent<FlyPlayer>())
            {
                GameObject.Destroy(netuser.playerClient.GetComponent<FlyPlayer>());
                return;
            }
            float speed = 1f;
            if (args.Length > 0) float.TryParse(args[0], out speed);
            FlyPlayer newfly = netuser.playerClient.GetComponent<FlyPlayer>();
            if(newfly == null) newfly = netuser.playerClient.gameObject.AddComponent<FlyPlayer>();
            newfly.Refresh();
            newfly.speed = speed;
        }
        void SendHelpText(NetUser netuser)
        {
            if (!hasAccess(netuser)) return;
            SendReply(netuser, "Fly Command: /fly SPEED => activate/deactivate fly, 0 will make you stand still");
        }
    } 
}