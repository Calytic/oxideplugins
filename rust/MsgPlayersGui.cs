using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("MsgPlayersGui", "Steven", "1.0.1", ResourceId = 8909)]
    class MsgPlayersGui : RustPlugin
    {	
		int MsgDisappearTime = 15; //15 Seconds		
        System.Timers.Timer timer;
		bool IsAll = false, canRun = true;
        #region JSON
        string json = @"[  
		                { 
							""name"": ""AnnouncementMsg"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.7"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.020 0.91"",
                                    ""anchormax"": ""0.980 0.99""
                                }
                            ]
                        },
						{
                            ""parent"": ""AnnouncementMsg"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""From: {from}"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.6"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
						{
                            ""parent"": ""AnnouncementMsg"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{msg}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.1"",
                                    ""anchormax"": ""1 0.8""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
		void LoadMsgGui(string Name, string Msg)
		{			
			string send = json.Replace("{from}", Name).Replace("{msg}", Msg);
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", send);
			}
			timer.Start();
		}
		
        void Loaded()
        {
		    timer = new System.Timers.Timer(1000 * MsgDisappearTime);
            timer.Interval = 1000 * MsgDisappearTime;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
        }
		
		void Unload()
		{
			timer.Stop();
		}
		BasePlayer TempPlayer;
		
		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
			timer.Stop();
			if(IsAll)
			{
				foreach (BasePlayer player in BasePlayer.activePlayerList)
				{
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "AnnouncementMsg");
				}
			} 
			else if(TempPlayer!=null) 
			{				
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = TempPlayer.net.connection }, null, "DestroyUI", "AnnouncementMsg");
				TempPlayer = null;
			}
			canRun = true;
		}
		
		[ChatCommand("announce")]
        void cmdAnnounce(BasePlayer player, string command, string[] args)
		{
			if(!canRun) 
			{
				SendReply(player, "A announcement has recently been done please wait " + MsgDisappearTime + " seconds.");	
				return;		
			}
			if(player.net.connection.authLevel < 1) return;
			if(args.Length >= 1)
			{
				IsAll = true;
				string Msg = "";
				for(int i = 0; i < args.Length; i++)				
					Msg = Msg + " " + args[i];
				canRun = false;
				LoadMsgGui(player.displayName, Msg);
			} else SendReply(player, "Incorrect Syntax use /announce <msg>");				
		}
		
		[ChatCommand("announceto")]
        void cmdAnnounceTo(BasePlayer player, string command, string[] args)
		{
			if(!canRun) 
			{
				SendReply(player, "A announcement has recently been done please wait " + MsgDisappearTime + " seconds.");	
				return;		
			}
			if(player.net.connection.authLevel < 1) return;
			if(args.Length >= 2)
			{
				IsAll = false;
				string Player = args[0].ToLower(), Msg = "";
				for(int i = 1; i < args.Length; i++)				
					Msg = Msg + " " + args[i];
				BasePlayer p;
				if((p = BasePlayer.activePlayerList.Find(x => x.displayName.ToLower().EndsWith(Player))) != null) //used ends with due to clan tags
				{
					string send = json.Replace("{from}", player.displayName).Replace("{msg}", Msg);
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = p.net.connection }, null, "AddUI", send);
					canRun = false;
					timer.Start();
					TempPlayer = p;
				} else SendReply(player, Player+" is not online.");
			} else SendReply(player, "Incorrect Syntax use /announce <name> <msg>");
		}
    }
}