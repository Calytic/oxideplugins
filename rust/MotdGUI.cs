using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Motd GUI", "PaiN", 0.2, ResourceId = 10321)]
    [Description("Simple Motd on the screen.")]
    public class MotdGUI : RustPlugin
    {     
	
		System.Collections.Generic.List<ulong> guioff = new System.Collections.Generic.List<ulong>();	
		
        void Loaded()
        {
			LoadDefaultConfig();
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
			string text = Config["Motd", "Message"].ToString();
			string title = Config["Motd", "Title"].ToString();
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{text}", text).Replace("{title}", title), null, null, null, null));
			}
        }
		
		protected override void LoadDefaultConfig()
		{
		
		if(Config["Motd", "Message"] == null) Config["Motd", "Message"] = "<color=yellow>Today</color> is a beautiful day!";
		if(Config["Motd", "Message"].ToString() != "<color=yellow>Today</color> is a beautiful day!") return;

		if(Config["Motd", "Title"] == null) Config["Motd", "Title"] = "<color=red>Motd</color>";
		if(Config["Motd", "Title"].ToString() != "<color=red>Motd</color>") return;
		
		
		SaveConfig();
		}
		 
        #region JSON
        string json = @"[
                       { 
                            ""name"": ""Motd"",
                            ""parent"": ""Overlay"",
                            ""components"":
                            [ 
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.7"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.001 0.65"",
                                    ""anchormax"": ""0.25 0.85""
                                }
                            ]
                        },
                        {
                            ""parent"": ""Motd"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{title}"",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.7"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""Motd"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{text}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.1"",
                                    ""anchormax"": ""1 1.2""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
      
        [ChatCommand("motd")]
		void cmdMotdShow(BasePlayer player, string cmd, string[] args)
		{
			if(guioff.Contains(player.userID)) 
			{				
			string text = Config["Motd", "Message"].ToString();
			string title = Config["Motd", "Title"].ToString();
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{text}", text).Replace("{title}", title), null, null, null, null));
				guioff.Remove(player.userID);
			}
			else 
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Motd", null, null, null, null));
				guioff.Add(player.userID);
			}	
		
		
		}
         
        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player)
        {
			string text = Config["Motd", "Message"].ToString();
			string title = Config["Motd", "Title"].ToString();
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{text}", text).Replace("{title}", title), null, null, null, null));
        }
		
		void Unloaded(BasePlayer player)
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = current.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Motd", null, null, null, null));
			
			}
		
		}
		void OnPlayerDisconnected(BasePlayer player)
		{
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("Motd", null, null, null, null));
		}
    }
}