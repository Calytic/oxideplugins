using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AdminRadar", "Reneb", "1.0.1", ResourceId = 978)]
    class AdminRadar : RustPlugin
    {
        private static int authlevel = 2;

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
            CheckCfg<int>("Settings: AuthLevel", ref authlevel);
            SaveConfig();
        }



        static Vector3 textheight = new Vector3(0f, 3f, 0f);
        static Vector3 bodyheight = new Vector3(0f, 0f, 0f);
        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(WallhackRadar));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }
        public class WallhackRadar : MonoBehaviour
        {
            BasePlayer player;
            public float distance;
            public float boxheight;
            public float invoketime;
            public UnityEngine.Color boxcolor;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                boxcolor = UnityEngine.Color.blue;
            }
            void DoRadar()
            {
                if (!player.IsConnected())
                {
                    GameObject.Destroy(this);
                    return;
                }
                foreach(BasePlayer targetplayer in BasePlayer.activePlayerList)
                {
                    if (targetplayer == player) continue;
                    if (Vector3.Distance(targetplayer.transform.position, player.transform.position) < distance)
                    {
                        player.SendConsoleCommand("ddraw.box", invoketime, boxcolor, targetplayer.transform.position + bodyheight, boxheight);
                    }
                }
            }
        }
        [ChatCommand("radar")]
        void cmdChatRadar(BasePlayer player, string command, string[] args)
        {
            if(player.net.connection.authLevel < authlevel)
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            if(player.GetComponent<WallhackRadar>() && args.Length == 0)
            {
                GameObject.Destroy(player.GetComponent<WallhackRadar>());
                SendReply(player, "Admin Radar: Deactivated");
                return;
            }
            WallhackRadar whrd = player.GetComponent<WallhackRadar>();
            if (whrd == null) whrd = player.gameObject.AddComponent<WallhackRadar>();
            float defaulttime = 5f;
            float defaultdistance = 8000f;
            float boxheight = 4f;
            if (args.Length > 0) float.TryParse(args[0], out defaulttime);
            if (args.Length > 1) float.TryParse(args[1], out defaultdistance);
            if (args.Length > 2) float.TryParse(args[2], out boxheight);
            whrd.CancelInvoke();
            whrd.InvokeRepeating("DoRadar", 0f, defaulttime);
            whrd.invoketime = defaulttime;
            whrd.distance = defaultdistance;
            whrd.boxheight = boxheight;
            SendReply(player, string.Format("Admin Radar: Activated - {0}s refresh - {1}m distance - {2}m box-height", defaulttime.ToString(), defaultdistance.ToString(), boxheight.ToString()));
        }
    }
} 