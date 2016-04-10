using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using System.Linq;
using Rust;

namespace Oxide.Plugins
{
    [Info("SleepingSystemGUI", "PaiN", 0.2, ResourceId = 0)]
    [Description("GUI Based Sleeping system.")]
    class SleepingSystemGUI : RustPlugin
    {
		private Timer _timer;
		///Next Update///
		/*private bool Changed;
		private string youneedtosleep;
		private int bar5timer;
		private int bar4timer;
		private int bar3timer;
		private int bar2timer;
		
		
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
			youneedtosleep = Convert.ToString(GetConfig("Messages", "YouNeedToSleep", "You are tired, your screen will start flashing || /sleep"));
			bar5timer = Convert.ToInt32(GetConfig("Timers", "Bar5Timer", 300));
			bar4timer = Convert.ToInt32(GetConfig("Timers", "Bar4Timer", 600));
			bar3timer = Convert.ToInt32(GetConfig("Timers", "Bar3Timer", 900));
			bar2timer = Convert.ToInt32(GetConfig("Timers", "Bar2Timer", 1200));
			
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
		}*/
	
        string json = @"[
                       {
                            ""name"": ""SleepingSystem"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.75"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.8355 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                    ]
                    ";
string text = @"[
                        {
                            ""name"": ""Text"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<size=25>Z</size><size=20>z</size>z"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.719 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                   ]
                   ";
string bar1 = @"[
                        {
                            ""name"": ""Bar1"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<color=red>||</color>"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.76 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                   ]
                   ";
string bar2 = @"[
                        {
                            ""name"": ""Bar2"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<color=olive>||</color>"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.79 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                   ]
                   ";
string bar3 = @"[
                        {
                            ""name"": ""Bar3"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<color=yellow>||</color>"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.82 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                   ]
                   ";
string bar4 = @"[
                        {
                            ""name"": ""Bar4"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<color=green>||</color>"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.85 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                   ]
                   ";
string bar5 = @"[
                 {
                            ""name"": ""Bar5"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""<color=lime>||</color>"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"", 
                                    ""anchormin"": ""0.88 0"",
                                    ""anchormax"": ""0.9755 0.044""
                                }
                            ]
                        }
                    ]
                    ";
string image = @"[
                       {
                            ""name"": ""Image"",
                            ""parent"": ""Overlay"",
							""fadeOut"": ""0.5"",
                            ""components"":
                            [
                               {
                                     ""type"":""UnityEngine.UI.Image"",
									 ""fadeIn"": ""0.5"",
                                     ""color"":""0 0 0 0.9"",
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
string sleeptext = @"[
                       {
							""name"": ""SleepText"",
							""parent"": ""Overlay"",
							""components"":
							[
								{
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 1"",
                                }, 
								{    
									""type"":""RectTransform"",
									""anchormin"": ""0 0"",
									""anchormax"": ""1 1""
								},
								{  
									""type"":""NeedsCursor""
								}
							]
						},
						{
							""parent"": ""SleepText"",
							""components"":
							[
								{
									""type"":""UnityEngine.UI.Text"",
									""text"":""<size=50>Z</size><size=35>z</size><size=26>z</size> <color=yellow>You are resting...</color>"",
									""fontSize"":35,
									""align"": ""MiddleCenter"",
								},
								{ 
									""type"":""RectTransform"",
									""anchormin"": ""0 0.20"",
									""anchormax"": ""1 0.9""
								}
							]
						}
                    ]
                    ";
					

        void Loaded()  
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json);
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", text);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar1);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar2);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar3);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar4);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar5);
				timer.Once(450f, () => player.SendConsoleCommand("bars.four") );
				timer.Once(900f, () => player.SendConsoleCommand("bars.three") );
				timer.Once(1350f, () => player.SendConsoleCommand("bars.two") );
				timer.Once(1800f, () => player.SendConsoleCommand("bars.one") );
            }
        }
		
		void OnPlayerInit(BasePlayer player)
		{
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json);
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", text);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar1);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar2);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar3);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar4);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar5);
				timer.Once(450f, () => player.SendConsoleCommand("bars.four") );
				timer.Once(900f, () => player.SendConsoleCommand("bars.three") );
				timer.Once(1350f, () => player.SendConsoleCommand("bars.two") );
				timer.Once(1800f, () => player.SendConsoleCommand("bars.one") );
            
		}
		
        void Unloaded()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "SleepingSystem");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Text");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "SleepText");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar1");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar2");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar3");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar4");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar5");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Image");


            }


        } 
		
		void ResetGUI(BasePlayer player) 
		{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "SleepingSystem");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Text");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar1");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar2");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar3");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar4");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar5");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Image");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json);
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", text);
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar1);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar2);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar3);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar4);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", bar5);
				timer.Once(450f, () => player.SendConsoleCommand("bars.four") );
				timer.Once(900f, () => player.SendConsoleCommand("bars.three") );
				timer.Once(1350f, () => player.SendConsoleCommand("bars.two") );
				timer.Once(1800f, () => player.SendConsoleCommand("bars.one") );
		
		
		}
		
		
		void OnPlayerDisconnected(BasePlayer player)
		{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "SleepingSystem");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar1");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Text");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar2");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar3");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar4");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar5");
		}
		
		void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
		{
			if(hitInfo == null) return;
			if(entity.ToPlayer())
			{
				ResetGUI(player);
			}
		}
		
		[ConsoleCommand("bars.four")]
		void cmdBars4(ConsoleSystem.Arg arg)
		{
				BasePlayer player = (BasePlayer) arg.connection.player;
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar5");

		}
		[ConsoleCommand("bars.three")]
		void cmdBars3(ConsoleSystem.Arg arg)
		{
				BasePlayer player = (BasePlayer) arg.connection.player;
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar4");
		}
		[ConsoleCommand("bars.two")]
		void cmdBars2(ConsoleSystem.Arg arg)
		{
				BasePlayer player = (BasePlayer) arg.connection.player;
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar3");
		}
		[ConsoleCommand("bars.one")]
		void cmdBar1(ConsoleSystem.Arg arg)
		{
				_timer.Destroy();
				BasePlayer player = (BasePlayer) arg.connection.player;
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Bar2");
				SendReply(player, "<color=cyan>Sleeping System</color>: " + "You need to get a rest, otherwise you screen will start flashing. || /sleep");
				_timer = timer.Repeat(25,0, () => {
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", image);
				
				timer.Once(2, () => {
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Image");
				
					}); 
				});
			
		}
		[ChatCommand("sleep")]
		void cmdSleep(BasePlayer player, string cmd, string[] args)
		{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", sleeptext);
				timer.Once(2, () =>{
				player.StartSleeping();
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "SleepText");
				ResetGUI(player);
				});
		} 
	} 
}