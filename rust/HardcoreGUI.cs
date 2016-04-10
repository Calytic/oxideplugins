using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Hardcore GUI", "LaserHydra", "1.0.2", ResourceId = 1237)]
    [Description("Hide players Health, Hydration & Saturation")]
    public class HardcoreGUI : RustPlugin
    {     
        #region JSON
        string json = @"[
                       {
                            ""name"": ""HardcoreOverlay"",
                            ""parent"": ""HUD/Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 1"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.82 0.0"",
                                    ""anchormax"": ""1 0.16""
                                }
                            ]
                        },
                        {
                            ""parent"": ""HardcoreOverlay"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{ACTIVATED}"",
                                    ""fontSize"":20,
									""color"":""1.0 0.0 0.0 0.5"",
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0.01 0.01"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                    ]
                    ";
        #endregion
     
        void Loaded()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HardcoreOverlay"));
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{ACTIVATED}", "Hardcore Mode is activated!")));
                }
            }
        } 

        void OnPlayerInit(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{ACTIVATED}", "Hardcore Mode is activated!")));
        }
		
		void Unloaded(BasePlayer player)
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = current.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HardcoreOverlay"));
			}
		}

		void OnPlayerDisconnected(BasePlayer player)
		{
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("HardcoreOverlay"));
		}
    }
}