using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ClockGui", "Steven", "1.0.0", ResourceId = 8913)]
    class ClockGui : RustPlugin
    {
		static string ServerTimePrefix = "[GMT+1] ";
		System.Collections.Generic.List<ulong> DisabledFor = new System.Collections.Generic.List<ulong>();		
		System.Timers.Timer timer;
		int LastMin = -1;
        #region JSON
        string json = @"[  
		                { 
							""name"": ""Clock"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.7"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.145 0.93"",
                                    ""anchormax"": ""0.215 0.99""
                                }
                            ]
                        },
						{
                            ""parent"": ""Clock"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":"""+ServerTimePrefix+@"{time}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.2"",
                                    ""anchormax"": ""1 0.8""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
		string GetTime()
		{
			int Hour = System.DateTime.Now.Hour, Min = System.DateTime.Now.Minute;
			if(Hour < 10 && Min < 10)
				return string.Format("0{0}:0{1}", Hour, Min);
			else if (Hour < 10)
				return string.Format("0{0}:{1}", Hour, Min);
			else if(Min < 10)
				return string.Format("{0}:0{1}", Hour, Min);
			return string.Format("{0}:{1}", Hour, Min);
		}
				
		void Loaded()
        {
		    timer = new System.Timers.Timer(500);
            timer.Interval = 500;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
			LastMin = System.DateTime.Now.Minute;
			timer.Start();
        }
		
		void Unload()
		{
			timer.Stop();
		}
		
		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
			timer.Stop();
			int NowMin = System.DateTime.Now.Minute;
			if(LastMin == NowMin)
				 timer.Interval = 500;
			 else
			 {
				string time = GetTime();
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					if(player != null)
					{
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Clock");
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json.Replace("{time}", time));
					}
				}
				LastMin = NowMin;
				timer.Interval = 60000;
			 }
			 timer.Start();
		}
		
		[ChatCommand("clock")]
        void DisableCmd(BasePlayer player, string command, string[] args)
        {
			if(DisabledFor.Contains(player.userID)) 
			{				
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json.Replace("{time}", GetTime()));
				DisabledFor.Remove(player.userID);
			}
			else 
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "Clock");
				DisabledFor.Add(player.userID);
			}				
		}
		
		[HookMethod("OnPlayerInit")]
		void OnPlayerInit(BasePlayer player)
		{
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json.Replace("{time}", GetTime()));
		}
		
		[HookMethod("OnPlayerDisconnected")]
		void OnPlayerDisconnected(BasePlayer player)
		{
			if(DisabledFor.Contains(player.userID)) DisabledFor.Remove(player.userID);
		}
    }
}