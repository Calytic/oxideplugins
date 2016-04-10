using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PlayerRadar", "Reneb", "1.0.2", ResourceId = 1326)]
    class PlayerRadar : RustPlugin
    {
        private static int authlevel = 2;
        private static FieldInfo serverinput;
        static int playerCol;
        private static string xmin = "0.5";
        private static string xmax = "0.9";
        private static string ymin = "0.5";
        private static string ymax = "0.9";
        private static string refreshSpeed = "0.1";
        private static bool showSleepers = false;
        private static string permName = "canradar";
        private static string playerColor = "0 0.8 0 1";
        private static string npcColor = "1 0.5 0 1";
        private static string sleeperColor = "0.8 0 0 1";
        private static string radarUrl = "http://s28.postimg.org/r8ebzj1yl/radar_1.png";
        private static float refreshspeed = 0.1f;

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
            CheckCfg<int>("Admin - AuthLevel", ref authlevel);
            CheckCfg<string>("Admin - Permissions Name", ref permName);
            CheckCfg<string>("GUI - X Min", ref xmin);
            CheckCfg<string>("GUI - X Max", ref xmax);
            CheckCfg<string>("GUI - Y Min", ref ymin);
            CheckCfg<string>("GUI - Y Max", ref ymax);
            CheckCfg<bool>("Radar - Show Sleepers", ref showSleepers);
            CheckCfg<string>("Radar - Refresh Speed", ref refreshSpeed);
            CheckCfg<string>("Radar - Image URL", ref radarUrl);
            CheckCfg<string>("Radar - Color - Sleepers", ref sleeperColor);
            CheckCfg<string>("Radar - Color - Players", ref playerColor);
            CheckCfg<string>("Radar - Color - HumanNPC", ref npcColor);
            SaveConfig();

            refreshspeed = Convert.ToSingle(refreshSpeed);
        }

        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(RadarClass));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }


        void Loaded()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            playerCol = LayerMask.GetMask(new string[] { "Player (Server)" });
            radaroverlay = radaroverlay.Replace("{xmin}", xmin).Replace("{xmax}", xmax).Replace("{ymin}", ymin).Replace("{ymax}", ymax).Replace("{radarurl}", radarUrl);
            if (!permission.PermissionExists(permName)) permission.RegisterPermission(permName, this);
        }
         
        public class RadarClass : MonoBehaviour
        {
            public BasePlayer player;
            public float distance;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                distance = 100f;
                InvokeRepeating("Radar", 0f, refreshspeed);
            }
            
            void Radar()
            {
                if (player == null) GameObject.Destroy(this);
                if (!player.IsConnected()) GameObject.Destroy(this);
                ShowGUI(player, distance);
            }
            void OnDestroy()
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "RadarUnderlay");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "RadarOverlay");
            }
        }


        public static string radaroverlay = @"[  
		                { 
							""name"": ""RadarOverlay"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.RawImage"",
                                    ""imagetype"":""Tiled"",
                                     ""url"":""{radarurl}"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{xmin} {ymin}"",
                                    ""anchormax"": ""{xmax} {ymax}""
                                }
                            ]
                        },
                    ]
                    ";
        public static string radarunderlay = @"[
                        { 
							""name"": ""RadarUnderlay"",
                            ""parent"": ""RadarOverlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        }
                    ]
                    ";
        public static string playerjson = @"[  
		                { 
							""parent"": ""RadarUnderlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""{color}"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{xmin} {ymin}"",
                                    ""anchormax"": ""{xmax} {ymax}""
                                }
                            ]
                        }
                    ]
                    ";
        static void ShowGUI(BasePlayer player, float distance)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "RadarUnderlay");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", radarunderlay);
             
            var input = serverinput.GetValue(player) as InputState;
            var angle = input.current.aimAngles.y;
            var player_x = player.transform.position.x;
            var player_y = player.transform.position.z;
            string color = string.Empty;
            Collider playerCollider = player.GetComponentInChildren<Collider>();
            foreach (Collider col in Physics.OverlapSphere(player.transform.position, distance, playerCol))
            {
                if (playerCollider == col) continue;
                BasePlayer targetplayer = col.GetComponentInParent<BasePlayer>();
                if (targetplayer == null) continue;

                color = targetplayer.IsConnected() ? playerColor : npcColor;
                float x = col.transform.position.x;
                float y = col.transform.position.z;
                int x2 = (int)((x - player_x) * Math.Cos(Math.PI * angle / 180) - (y - player_y) * Math.Sin(Math.PI * angle / 180)) * 50 / (int)distance + 50;
                int y2 = (int)((x - player_x) * Math.Sin(Math.PI * angle / 180) + (y - player_y) * Math.Cos(Math.PI * angle / 180)) * 50 / (int)distance + 50;
                var playerpos = playerjson.Replace("{xmin}", ((Convert.ToDecimal(x2) - 0.5m) / 100).ToString()).Replace("{color}", color).Replace("{xmax}", ((Convert.ToDecimal(x2) + 0.5m) / 100).ToString()).Replace("{ymin}", ((Convert.ToDecimal(y2) - 0.50m) / 100).ToString()).Replace("{ymax}", ((Convert.ToDecimal(y2) + 0.5m) / 100).ToString());
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", playerpos);
            } 

            if (!showSleepers) return;
            color = sleeperColor;
            foreach (BasePlayer targetplayer in BasePlayer.sleepingPlayerList)
            {
                if (Vector3.Distance(targetplayer.transform.position, player.transform.position) > distance) continue;
                float x = targetplayer.transform.position.x;
                float y = targetplayer.transform.position.z;
                int x2 = (int)((x - player_x) * Math.Cos(Math.PI * angle / 180) - (y - player_y) * Math.Sin(Math.PI * angle / 180)) * 50 / (int)distance + 50;
                int y2 = (int)((x - player_x) * Math.Sin(Math.PI * angle / 180) + (y - player_y) * Math.Cos(Math.PI * angle / 180)) * 50 / (int)distance + 50;
                var playerpos = playerjson.Replace("{xmin}", ((Convert.ToDecimal(x2) - 0.5m) / 100).ToString()).Replace("{color}", color).Replace("{xmax}", ((Convert.ToDecimal(x2) + 0.5m) / 100).ToString()).Replace("{ymin}", ((Convert.ToDecimal(y2) - 0.50m) / 100).ToString()).Replace("{ymax}", ((Convert.ToDecimal(y2) + 0.5m) / 100).ToString());
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", playerpos);
            }
        }

        [ChatCommand("radar")]
        void cmdChatRadar(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < authlevel && !permission.UserHasPermission(player.userID.ToString(), permName))
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            RadarClass rplayer = player.GetComponent<RadarClass>();
            if (rplayer != null)
            {
                GameObject.Destroy(rplayer);
                if (args.Length == 0)
                    return;
            }
            timer.Once(0.1f, ()  =>
            {
                rplayer = player.gameObject.AddComponent<RadarClass>();
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", radaroverlay);
                 
                float dist = 500f;
                if (args.Length > 0)
                    float.TryParse(args[0], out dist);
                rplayer.distance = dist;
                rplayer.enabled = true;
            }
            );
        }
    }
} 