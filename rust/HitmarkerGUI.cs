using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Hitmarker GUI", "PaiN", "1.3.2", ResourceId = 1241)]
    [Description("This plugin informs the attacker/player if he hit someone..")]
    class HitmarkerGUI : RustPlugin
    {
        private bool Changed;
        private bool enablesound;
        private string soundeffect;
		private string headshotsoundeffect;
		private string HeadshotImageURL;
        private bool useimage;
        private string ImageURL;
        private bool usetext;
        private string TextWord;

		List<BasePlayer> hitmarkeron = new List<BasePlayer>();
		
        void Loaded()
        {
            LoadVariables();
			foreach(BasePlayer current in BasePlayer.activePlayerList)
			{
				hitmarkeron.Add(current);
			
			}
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        void LoadVariables()
        {
            enablesound = Convert.ToBoolean(GetConfig("Sound", "EnableSoundEffect", true));
            soundeffect = Convert.ToString(GetConfig("Sound", "Sound Effect", "assets/bundled/prefabs/fx/takedamage_hit.prefab"));
            useimage = Convert.ToBoolean(GetConfig("Image", "Activated", true));
            ImageURL = Convert.ToString(GetConfig("Image", "ImageURL", "http://oxidemod.org/attachments/fuzidev_2-png.10926/"));
            usetext = Convert.ToBoolean(GetConfig("Text", "Activated", false));
            TextWord = Convert.ToString(GetConfig("Text", "TextWord", "HIT"));
			headshotsoundeffect = Convert.ToString(GetConfig("Sound", "HeadshotSoundEffect", "assets/bundled/prefabs/fx/headshot.prefab"));
			HeadshotImageURL = Convert.ToString(GetConfig("Image", "HeadshotImageURL", "http://i.imgur.com/dopEPnQ.png"));

            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }

        string image = @"[
                       {
                            ""name"": ""HitMarkerImage"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                               {
								   ""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
                                    ""type"":""UnityEngine.UI.RawImage"",
                                    ""imagetype"": ""Tiled"",
                                    ""color"": ""1.0 1.0 1.0 1.0"",
                                    ""url"": ""{url}"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.49 0.48"",
                                    ""anchormax"": ""0.51 0.52""
                                }
                            ]
                        }
                    ]
                    ";
        string text = @"[
                       {
                            ""name"": ""HitMarker"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
									""sprite"": ""assets/content/textures/generic/fulltransparent.tga"",
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.0 0.0 0.0 0.0"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.50 0.49"",
                                    ""anchormax"": ""0.60 0.51""
                                }
                            ]
                        },
                        {
                            ""parent"": ""HitMarker"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{text}"",
                                    ""fontSize"":20,
                                    ""color"":""1 0.0 0.0 2"",
                                    ""align"": ""MiddleCenter"",
                                    ""anchormin"": ""0.50 0.50"",
                                    ""anchormax"": ""0.50 0.50""
                                }
                            ]
                        },
                    ]
                    ";
					
		[ChatCommand("hitmarker")]
		void cmdHitMarker(BasePlayer player, string cmd, string[] args)
		{
			if(!hitmarkeron.Contains(player))
			{
				hitmarkeron.Add(player);
				SendReply(player, "<color=cyan>HitMarker</color>:" + " " + "<color=orange>You have enabled your hitmarker.</color>");
			}
			else
			{
				hitmarkeron.Remove(player);
				SendReply(player, "<color=cyan>HitMarker</color>:" + " " + "<color=orange>You have disabled your hitmarker.</color>");
			}
		
		
		}
		
		void OnPlayerInit(BasePlayer player)
		{
			hitmarkeron.Add(player);
		}
		void OnPlayerDisconnected(BasePlayer player)
		{
			hitmarkeron.Remove(player);
		}
		
		
        void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            var gettingdmg = hitinfo.HitEntity as BasePlayer;
            if (gettingdmg && hitmarkeron.Contains(attacker))
            {
				if(hitinfo.isHeadshot)
				{
					if(useimage == true)
					{
						if (enablesound == true)
						{
							Effect.server.Run(headshotsoundeffect, attacker.transform.position, Vector3.zero, attacker.net.connection);
						}
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "AddUI", new Facepunch.ObjectList(image.Replace("{url}", HeadshotImageURL), null, null, null, null));
						timer.Once(0.5f, () => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HitMarkerImage", null, null, null, null)));
					}
				}
				else
				{
					if(useimage == true)
					{
						if (enablesound == true)
						{
							Effect.server.Run(soundeffect, attacker.transform.position, Vector3.zero, attacker.net.connection);
						}
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "AddUI", new Facepunch.ObjectList(image.Replace("{url}", ImageURL), null, null, null, null));
						timer.Once(0.5f, () => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HitMarkerImage", null, null, null, null)));
					}
					else if (usetext == true)
					{
						if (enablesound == true)
						{
							Effect.server.Run(soundeffect, attacker.transform.position, Vector3.zero, attacker.net.connection);
						}
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "AddUI", new Facepunch.ObjectList(text.Replace("{text}", TextWord), null, null, null, null));
						timer.Once(0.5f, () => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = attacker.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HitMarker", null, null, null, null)));

					}
				}
            }
        }
    }
}