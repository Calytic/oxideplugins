using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PlayersOnlineGui", "Steven", "1.0.4", ResourceId = 8908)]
    class PlayersOnlineGui : RustPlugin
    {
		static string ServerName = "Rival Rust";		
		System.Collections.Generic.List<ulong> DisabledFor = new System.Collections.Generic.List<ulong>();
        #region JSON
        string json = @"[  
		                { 
							""name"": ""PlayersOnline"",
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
                                    ""anchormax"": ""0.140 0.99""
                                }
                            ]
                        },
						{
                            ""parent"": ""PlayersOnline"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":"""+ServerName+@""",
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
                            ""parent"": ""PlayersOnline"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Players Online: {players}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.2"",
                                    ""anchormax"": ""1 0.8""
                                }
                            ]
                        },
						{
                            ""parent"": ""PlayersOnline"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""Sleepers Online: {sleepers}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.0000000001"",
                                    ""anchormax"": ""1 0.6""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
		void LoadGui(bool Logged, BasePlayer p)
		{			
			string send = "";
			if(Logged) send = json.Replace("{players}", ""+(BasePlayer.activePlayerList.Count-1)).Replace("{sleepers}", ""+(BasePlayer.sleepingPlayerList.Count+1));
			else send = json.Replace("{players}", ""+BasePlayer.activePlayerList.Count).Replace("{sleepers}", ""+BasePlayer.sleepingPlayerList.Count);
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				if(DisabledFor.Contains(player.userID) || Logged && p == player) continue;
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "PlayersOnline");
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", send);
			}
		}
		
		[ChatCommand("toggle")]
        void DisableCmd(BasePlayer player, string command, string[] args)
        {
			if(DisabledFor.Contains(player.userID)) 
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", json.Replace("{players}", ""+BasePlayer.activePlayerList.Count).Replace("{sleepers}", ""+BasePlayer.sleepingPlayerList.Count));
				DisabledFor.Remove(player.userID);
			}
			else 
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", "PlayersOnline");
				DisabledFor.Add(player.userID);
			}				
		}
		
		[HookMethod("OnPlayerInit")]
		void OnPlayerInit(BasePlayer player)
		{
			LoadGui(false, player);
		}
		
		[HookMethod("OnPlayerDisconnected")]
		void OnPlayerDisconnected(BasePlayer player)
		{
			if(DisabledFor.Contains(player.userID)) DisabledFor.Remove(player.userID);
			LoadGui(true, player);
		}
    }
}