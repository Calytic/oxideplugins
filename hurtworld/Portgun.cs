using System.Collections.Generic;
using Oxide.Core;
namespace Oxide.Plugins
{
    [Info("Portgun", "Reneb", "1.0.1", ResourceId = 1572)]
    class Portgun : HurtworldPlugin
    {
        UnityEngine.RaycastHit hit;
        int portgunLayer;
        void Loaded()
        {
            permission.RegisterPermission("portgun.use", this);
            var messages = new Dictionary<string, string>
            {
                {"NotAllowed", "You are not allowed to use this command"},
                {"NotFound", "Couldn't find something in front of you"}
            };
            lang.RegisterMessages(messages, this);

            portgunLayer = UnityEngine.LayerMask.GetMask("Terrain", "Constructions");
        }
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        [ChatCommand("p")]
        void cmdPortgun(PlayerSession session, string command, string[] args)
        {
            if(!permission.UserHasPermission(session.SteamId.ToString(), "portgun.use") && !session.IsAdmin)
            {
                hurt.SendChatMessage(session, GetMessage("NotAllowed", session.SteamId.ToString()));
                return;
            } 
            var net = GameManager.GetPlayerEntity(session.Player) as UnityEngine.GameObject;
            var thirdper = net.GetComponentInChildren<Assets.Scripts.Core.CamPosition>().transform;
            var currentRot = thirdper.rotation * UnityEngine.Vector3.forward;

            if(!UnityEngine.Physics.Raycast(net.transform.position + new UnityEngine.Vector3(0f, 1.5f, 0f), currentRot, out hit, float.MaxValue, portgunLayer))
            {
                hurt.SendChatMessage(session, GetMessage("NotFound", session.SteamId.ToString()));
                return;
            }
            net.transform.position = hit.point;
        }

        [ChatCommand("up")]
        void cmdUp(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "portgun.use") && !session.IsAdmin) { hurt.SendChatMessage(session, GetMessage("NotAllowed", session.SteamId.ToString())); return; }
            var net = GameManager.GetPlayerEntity(session.Player) as UnityEngine.GameObject;

            float distance = 3f;
            if (args.Length > 0)
                if (!float.TryParse(args[0], out distance))
                    distance = 3f;

            net.transform.position = net.transform.position + new UnityEngine.Vector3(0f,distance,0f);
        }

        [ChatCommand("forward")]
        void cmdForward(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "portgun.use") && !session.IsAdmin) { hurt.SendChatMessage(session, GetMessage("NotAllowed", session.SteamId.ToString())); return; }
            var net = GameManager.GetPlayerEntity(session.Player) as UnityEngine.GameObject;
            var thirdper = net.GetComponentInChildren<Assets.Scripts.Core.CamPosition>().transform;
            var currentRot = thirdper.rotation * UnityEngine.Vector3.forward;

            float distance = 3f;
            if (args.Length > 0)
                if (!float.TryParse(args[0], out distance))
                    distance = 3f;

            net.transform.position = net.transform.position + (distance * currentRot);
        }

        [ChatCommand("right")]
        void cmdRight(PlayerSession session, string command, string[] args)
        {
            if (!permission.UserHasPermission(session.SteamId.ToString(), "portgun.use") && !session.IsAdmin) { hurt.SendChatMessage(session, GetMessage("NotAllowed", session.SteamId.ToString())); return; }
            var net = GameManager.GetPlayerEntity(session.Player) as UnityEngine.GameObject;
            var thirdper = net.GetComponentInChildren<Assets.Scripts.Core.CamPosition>().transform;
            var currentRot = thirdper.rotation * UnityEngine.Vector3.right;

            float distance = 3f;
            if (args.Length > 0)
                if (!float.TryParse(args[0], out distance))
                    distance = 3f;

            net.transform.position = net.transform.position + (distance * currentRot);
        }
    }
}