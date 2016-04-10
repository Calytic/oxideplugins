using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.Data;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Compass GUI", "PaiN", 1.1, ResourceId = 1231)]
    [Description("This plugin shows which direction is the player facing in a GUI.")]
    public class CompassGUI : RustPlugin
    {     
		List<BasePlayer> gui = new List<BasePlayer>();

		private Timer _timer;
		private bool Changed;
		private bool displaycoords;
		private string xmin;
		private string xmax; 
		private string ymin;
		private string ymax; 
		private bool enableonconnect;
		
        void Loaded() 
        {  
            _timer = timer.Every(1, Test);
			foreach(BasePlayer current in BasePlayer.activePlayerList)
			{
				gui.Add(current); 
			}
			LoadVariables();

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
			xmin = Convert.ToString(GetConfig("GUI", "X min", "0.45"));
			xmax = Convert.ToString(GetConfig("GUI", "X max", "0.56"));
			ymin = Convert.ToString(GetConfig("GUI", "Y min", "0.91"));
			ymax = Convert.ToString(GetConfig("GUI", "Y max", "0.99"));
			enableonconnect = Convert.ToBoolean(GetConfig("Settings", "EnableOnConnect", true));
			displaycoords = Convert.ToBoolean(GetConfig("Settings", "DisplayCoordinates", true));
			
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
		 

        static string Title = "<color=yellow>Compass</color>"; 
        #region JSON
        string json = @"[
                       {
                            ""name"": ""EyesPosition"",
                            ""parent"": ""HUD/Overlay"",
                            ""components"":
                            [
                                {
                                     ""type"":""UnityEngine.UI.Image"",
                                     ""color"":""0.1 0.1 0.1 0.7"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""{xmin} {ymin}"",
                                    ""anchormax"": ""{xmax} {ymax}""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EyesPosition"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":"""+Title+@""",
                                    ""fontSize"":20,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.40"",
                                    ""anchormax"": ""1 1""
                                }
                            ]
                        },
                        {
                            ""parent"": ""EyesPosition"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""{eyeposition}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.20"",
                                    ""anchormax"": ""1 0.65""
                                } 
                            ]
                        },
						{
                            ""parent"": ""EyesPosition"",
                            ""components"":
                            [
                                {
                                    ""type"":""UnityEngine.UI.Text"",
                                    ""text"":""X: {positionx},  Z: {positionz}"",
                                    ""fontSize"":15,
                                    ""align"": ""MiddleCenter"",
                                },
                                {
                                    ""type"":""RectTransform"",
                                    ""anchormin"": ""0 0.0000001"",
                                    ""anchormax"": ""1 0.4""
                                }
                            ]
                        }
                    ]
                    ";
        #endregion
     //Credits to Mughishi for the Compass plugin s
        string GetEyesPosition(BasePlayer player)
        {
            double lookRotation = player.eyes.rotation.eulerAngles.y;
         
            if (lookRotation > 337.5 || lookRotation < 22.5)
              return "<color=cyan>North</color>";
            else if (lookRotation > 22.5 && lookRotation < 67.5)
                return "<color=cyan>North-East</color>";
            else if (lookRotation > 67.5 && lookRotation < 112.5)
                return "<color=cyan>East</color>";
            else if (lookRotation > 112.5 && lookRotation < 157.5)
                return "<color=cyan>South-East</color>";
            else if (lookRotation > 157.5 && lookRotation < 202.5)
                return "<color=cyan>South</color>";
            else if (lookRotation > 202.5 && lookRotation < 247.5)
                return "<color=cyan>South-West</color>";
            else if (lookRotation > 247.5 && lookRotation < 292.5)
                return "<color=cyan>West</color>";
            else if (lookRotation > 292.5 && lookRotation < 337.5)
                return "<color=cyan>North-West</color>";
            return "None";
        }
		
		
		
        void Test()
        {
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				if (gui.Contains(player))		
				{
		
					//Debug PrintToChat("Contains " + player.displayName);
					int posx = Convert.ToInt32(player.transform.position.x);
					int posy = Convert.ToInt32(player.transform.position.y);
					int posz = Convert.ToInt32(player.transform.position.z);
					if(displaycoords == false)
					{
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("EyesPosition", null, null, null, null));
						CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{eyeposition}", GetEyesPosition(player)).Replace("{xmin}", xmin).Replace("{xmax}", xmax).Replace("{ymin}", ymin).Replace("{ymax}", ymax), null, null, null, null));
					}
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("EyesPosition", null, null, null, null));
					CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(json.Replace("{eyeposition}", GetEyesPosition(player)).Replace("{positionx}", posx.ToString()).Replace("{positionz}", posz.ToString()).Replace("{xmin}", xmin).Replace("{xmax}", xmax).Replace("{ymin}", ymin).Replace("{ymax}", ymax), null, null, null, null));
				}
			}

     
        }
         
		 
		/*NextUpdate[ChatCommand("compassbuy")]
		void cmdBuyCompass(BasePlayer player, string cmd, string[] args)
		{	
			if(!plugins.Exists("00-Economics"))
			{
				Puts("Economics plugin is not installed! If you dont wanna use the buy system then do not download it! Get it at http://oxidemod.org/plugins/717/ . ");
				SendReply(player, "<color=orange>CompassGUI</color>" + "You can't buy a compass since the server owner has disabled this feature!");
				return;
			}

			string checkconfig = Config["Compass", "EnableEconomics"].ToString();
			if(checkconfig == "true")
			{
				string steamId = player.userID.ToString();
				if (storedData.Players.Any(p => p.UserId == steamId))
				{
					SendReply(player, "<color=orange>CompassGUI</color>" + " You already have a compass.");	
					return;
				}  
				 var playerMoney = API.GetUserDataFromPlayer(player);
				 int price = Convert.ToInt32(Config["Compass", "Price"]);
				 
				 if(playerMoney[1] >= price)
				 {
					var info = new PlayerInfo(player);
					storedData.Players.Add(info);
					playerMoney.Withdraw(price);
					SendReply(player, "<color=orange>CompassGUI</color>" + " You have successfully bought a compass for(" + price + "). Use /compass to enable and disable your compass.");	

				}
			}
		}*/
		
		[ChatCommand("showpos")]
		void cmdCopyPos(BasePlayer player, string cmd, string[] args)
		{
			SendReply(player, "<color=orange>CompassGUI</color>" + " To copy your position open the console \"<color=blue>F1</color>\" ");
			player.SendConsoleCommand("echo <color=aqua>CompassGUI</color>");
			player.SendConsoleCommand("echo <color=yellow>Your position is</color>: X: " + player.transform.position.x + " Y: " + player.transform.position.y + " Z: " + player.transform.position.z);
		
		
		}
		
		[ChatCommand("compass")]
		void cmdCompass(BasePlayer player, string cmd, string[] args)
		{
			if(gui.Contains(player))
			{
				CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("EyesPosition", null, null, null, null));
				SendReply(player, "<color=orange>CompassGUI</color>" + " You have disabled your compass!");
				gui.Remove(player);
			}
			else
			{ 
				gui.Add(player);
				SendReply(player, "<color=orange>CompassGUI</color>" + " You have enabled your compass!");		
			}
		
		
		}
		
         
		 
        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player)
        {
			if(enableonconnect == true)
			{
				gui.Add(player);	
			}
		}
		
		void Unloaded()
		{ 
			foreach(BasePlayer current in BasePlayer.activePlayerList)
			{
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = current.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("EyesPosition", null, null, null, null));

			
			}
			gui.Clear();
			_timer.Destroy();
		} 
		void OnPlayerDisconnected(BasePlayer player)
		{
			gui.Remove(player);
			CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("EyesPosition", null, null, null, null));

		}
    }
}