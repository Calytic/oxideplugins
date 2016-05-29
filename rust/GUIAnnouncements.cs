using System;
using System.Collections.Generic;

using UnityEngine;
using Rust;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{
    [Info("GUIAnnouncements", "JoeSheep", "1.3.8", ResourceId = 1222)]
    [Description("Creates announcements with custom messages by command across the top of every player's screen in a banner.")]
    
    public class GUIAnnouncements : RustPlugin
    {	
    	#region Configuration
    	
    	const string permAnnounce = "GUIAnnouncements.announce";
    	
    	private List<ulong> JustJoined = new List<ulong>();
    	
    	Timer GlobalTimer;
		
		static string bannerTintGrey = "0.1 0.1 0.1 0.7";
		static string bannerTintRed = "0.5 0.1 0.1 0.7";
		static string bannerTintGreen = "0.1 0.4 0.1 0.5";
		static string textYellow = "0.7 0.7 0.1";
		static string textOrange = "0.8 0.5 0.1";
		static string textWhite = "1 1 1";
		
		float msgShowDuration = 10f;
		float welcomeAnnouncementDuration = 30f;
		int fontSize = 20;
		float fadeOutTime = 0.5f;
		float fadeInTime = 0.5f;
		bool helicopterAnnouncement = true;
		bool airdropAnnouncement = true;
		bool airdropAnnouncementLocation = false;
		bool welcomeAnnouncement = true;
		
		protected override void LoadDefaultConfig()
		{
			PrintWarning("Creating a new configuration file.");
            Config.Clear();
		    Config["MessageShowDuration"] = msgShowDuration;
		    Config["WelcomeAnnouncementDuration"] = welcomeAnnouncementDuration;
		    Config["FontSize"] = fontSize;
		    Config["FadeInTime"] = fadeInTime;
		    Config["FadeOutTime"] = fadeOutTime;
		    Config["HelicopterAnnouncement"] = helicopterAnnouncement;
		    Config["AirdropAnnouncement"] = airdropAnnouncement;
		    Config["AirdropAnnouncementLocation"] = airdropAnnouncementLocation;
		    Config["WelcomeAnnouncement"] = welcomeAnnouncement;
		    SaveConfig();
		}
		
		private void LoadConfig()
		{
			msgShowDuration = GetConfig<float>("MessageShowDuration", msgShowDuration);
			if (msgShowDuration == 0)
			{
				PrintWarning("Config MessageShowDuration set to 0, resetting to default.");
				float msgShowDuration = 10f;
				Config["MessageShowDuration"] = msgShowDuration;
				SaveConfig();
				LoadConfig();
			}
			float halfMsgShowDuration = msgShowDuration / 2;
			
			welcomeAnnouncementDuration = GetConfig<float>("WelcomeAnnouncementDuration", welcomeAnnouncementDuration);
			if (welcomeAnnouncementDuration == 0)
			{
				PrintWarning("Config WelcomeAnnouncementDuration set to 0, resetting to default.");
				float welcomeAnnouncementDuration = 30f;
				Config["WelcomeAnnouncementDuration"] = welcomeAnnouncementDuration;
				SaveConfig();
				LoadConfig();
			}
			
			fontSize = GetConfig<int>("FontSize", fontSize);
			if (fontSize > 33 | fontSize == 0)
			{
				PrintWarning("Config FontSize greater than 33 or 0, resetting to default.");
				int fontSize = 20;
				Config["FontSize"] = fontSize;
				SaveConfig();
				LoadConfig();
			}
			
			fadeOutTime = GetConfig<float>("FadeOutTime", fadeOutTime);
			if (fadeOutTime > halfMsgShowDuration)
			{
				PrintWarning("Config FadeOutTime is greater than half of MessageShowDuration, resetting to half of MessageShowDuration.");
				float fadeOutTime = halfMsgShowDuration;
				Config["FadeOutTime"] = fadeOutTime;
				SaveConfig();
				LoadConfig();
			}
			
			fadeInTime = GetConfig<float>("FadeInTime", fadeInTime);
			if (fadeInTime > halfMsgShowDuration)
			{
				PrintWarning("Config FadeInTime is greater than half of MessageShowDuration, resetting to half of MessageShowDuration.");
				float fadeInTime = halfMsgShowDuration;
				Config["FadeInTime"] = fadeInTime;
				SaveConfig();
				LoadConfig();
			}
			
			helicopterAnnouncement = GetConfig<bool>("HelicopterAnnouncement", helicopterAnnouncement);
			
			airdropAnnouncement = GetConfig<bool>("AirdropAnnouncement", airdropAnnouncement);
			
			airdropAnnouncementLocation = GetConfig<bool>("AirdropAnnouncementLocation", airdropAnnouncementLocation);
			
			welcomeAnnouncement = GetConfig<bool>("WelcomeAnnouncement", welcomeAnnouncement);
		}
		
		T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
		
		#endregion
		
		#region Localization
		
		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
			                      	{"ChatCommandAnnounce", "announce"},
			                      	{"ChatCommandDestroyAnnouncement", "destroyannouncement"},
			                      	{"ChatCommandHelp", "announcehelp"},
			                      	{"ConsoleCommandAnnounce", "announce.announce"},
			                      	{"ConsoleCommandDestroyAnnouncement", "announce.destroy"},
			                      	{"ConsoleCommandHelp", "announce.help"},
			                      	{"NoPermission", "You do not possess the required permissions."},
			                      	{"ChatCommandAnnounceUsage", "Usage: /announce <message>."},
			                      	{"ConsoleCommandAnnounceUsage", "Usage: announce.announce <message>."},
			                      	{"AnnounceHelp", "Chat commands: /announce <message>, /destroyannouncement Console commands: announce.announce <message>, announce.destroy"},
			                      	{"HelicopterAnnouncement", "Patrol helicopter inbound!"},
			                      	{"AirdropAnnouncement", "Airdrop on route!"},
			                      	{"AirdropAnnouncementWithLocation", "Airdrop on route! Dropping at {x}, {z}."},
			                      	{"WelcomeAnnouncement", "Welcome {playername}!"},
			                      	
			                      },this);
		}
		
		#endregion
		
		#region Initialization
		
		void OnServerInitialized()
		{
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game.");
            #endif
            
            LoadConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(permAnnounce, this);
            
            var timerInstance = new Oxide.Core.Libraries.Timer.TimerInstance(1000, 1000, null, this);
            GlobalTimer = new Timer(timerInstance);
            
            
            
            cmd.AddChatCommand(Lang("ChatCommandAnnounce"), this, "cmdAnnounce");
            cmd.AddChatCommand(Lang("ChatCommandDestroyAnnouncement"), this, "cmdDestroyAnnouncement");
            cmd.AddChatCommand(Lang("ChatCommandHelp"), this, "cmdAnnounceHelp");
            cmd.AddConsoleCommand(Lang("ConsoleCommandAnnounce"), this, "ccmdAnnounce");
            cmd.AddConsoleCommand(Lang("ConsoleCommandDestroyAnnouncement"), this, "ccmdAnnounceDestroy");
            cmd.AddConsoleCommand(Lang("ConsoleCommandHelp"), this, "ccmdAnnounceHelp");
		}
		#endregion
        
        #region GUI
        
        string announcementGUI = "AnnouncementGUI";
        string announcementText = "AnnouncementText";
        
		void CreateGlobalMsgGUI(string Msg, string bannerTintColor, string textColor)
		{
        	var elements = new CuiElementContainer();
        		elements.Add(new CuiElement
					{
        				Name = announcementGUI,
        				Components = 
        				{
							new CuiImageComponent {Color = bannerTintColor, FadeIn = fadeInTime},
							new CuiRectTransformComponent {AnchorMin = "-0.027 0.92", AnchorMax = "1.026 0.99"}
        				},
        				FadeOut = fadeOutTime
        			});
        		elements.Add(new CuiElement
        	        {
        	            Name = announcementText,
        	            Components =
        	            {
        	             	new CuiTextComponent {Text = Msg, FontSize = fontSize, Align = TextAnchor.MiddleCenter, FadeIn = fadeInTime, Color = textColor},
        	             	new CuiRectTransformComponent {AnchorMin = "-0.027 0.92", AnchorMax = "1.026 0.99"}
        	            },
        	            FadeOut = fadeOutTime
        	        });
        	foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.AddUi(player, elements);
			}
        	GlobalTimer = timer.Once(msgShowDuration, () => destroyGlobalGUI());
		}
		
		void CreatePrivateMsgGUI(string Msg, string bannerTintColor, string textColor, BasePlayer player)
		{
        	var elements = new CuiElementContainer();
        		elements.Add(new CuiElement
					{
        				Name = announcementGUI,
        				Components = 
        				{
							new CuiImageComponent {Color = bannerTintColor, FadeIn = fadeInTime},
							new CuiRectTransformComponent {AnchorMin = "-0.027 0.92", AnchorMax = "1.026 0.99"}
        				},
        				FadeOut = fadeOutTime
        			});
        		elements.Add(new CuiElement
        	        {
        	            Name = announcementText,
        	            Components =
        	            {
        	             	new CuiTextComponent {Text = Msg, FontSize = fontSize, Align = TextAnchor.MiddleCenter, FadeIn = fadeInTime, Color = textColor},
        	             	new CuiRectTransformComponent {AnchorMin = "-0.027 0.92", AnchorMax = "1.026 0.99"}
        	            },
        	            FadeOut = fadeOutTime
        	        });
        	CuiHelper.AddUi(player, elements);
        	if(JustJoined.Contains(player.userID))
        	{
        		JustJoined.Remove(player.userID);
        		timer.Once(welcomeAnnouncementDuration, () => destroyPrivateGUI(player));
        	} else timer.Once(msgShowDuration, () => destroyPrivateGUI(player));
		}
		
		#endregion
		
		#region Functions
		
		void OnPlayerInit(BasePlayer player)
		{
			if(welcomeAnnouncement)
			{
				JustJoined.Add(player.userID);
			}
		}
		
		void OnPlayerDisconnect(BasePlayer player)
		{
			if(JustJoined.Contains(player.userID))
			{
				JustJoined.Remove(player.userID);
				destroyPrivateGUI(player);
			}
		}
		
		void OnPlayerSleepEnded(BasePlayer player)
		{
			if(welcomeAnnouncement)
			{
				if(JustJoined.Contains(player.userID))
				{
					WelcomeAnnouncement(player);
				}
			}
		}
		
		void destroyGlobalGUI()
		{
			if(!GlobalTimer.Destroyed)
			{
				GlobalTimer.Destroy();
			}
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, announcementGUI);
				CuiHelper.DestroyUi(player, announcementText);
			}
		}
			
		void destroyPrivateGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, announcementGUI);
			CuiHelper.DestroyUi(player, announcementText);
		}
		
		void Unload()
		{
			destroyGlobalGUI();
		}
		
		private bool hasPermission(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "GUIAnnouncements.announce"))
            {
                SendReply(player, Lang("NoPermission", player.UserIDString));
                return false;
            }
            return true;
        }
		
		#endregion
		
		#region Auto Announcements
		
		void OnEntitySpawned(BaseNetworkable entity)
		{
			if(helicopterAnnouncement && entity.ToString().Contains("/patrolhelicopter.prefab"))
			{
				destroyGlobalGUI();
				string textColor = textOrange;
				string bannerTintColor = bannerTintRed;
				CreateGlobalMsgGUI(Lang("HelicopterAnnouncement"), bannerTintColor, textColor);
			}
		}
		
		void OnAirdrop(CargoPlane plane, Vector3 location)
		{
			if(airdropAnnouncement)
			{
				destroyGlobalGUI();
				string textColor = textYellow;
				string bannerTintColor = bannerTintGreen;
				if(airdropAnnouncementLocation)
				{
					string x = location.x.ToString();
					string z = location.z.ToString();
					CreateGlobalMsgGUI(Lang("AirdropAnnouncementWithLocation").Replace("{x}", x).Replace("{z}", z),bannerTintColor, textColor);
				} else CreateGlobalMsgGUI(Lang("AirdropAnnouncement"),bannerTintColor, textColor);
			}
		}
		
		void WelcomeAnnouncement(BasePlayer player)
		{
			if(welcomeAnnouncement)
			{
				destroyPrivateGUI(player);
				string bannerTintColor = bannerTintGrey;
        		string textColor = textWhite;
        		CreatePrivateMsgGUI(Lang("WelcomeAnnouncement").Replace("{playername}", player.displayName), bannerTintColor, textColor, player);
			}
		}
		
		#endregion
		
		#region Commands
		
		[HookMethod("cmdAnnounce")]
        void cmdAnnounce(BasePlayer player, string cmd, string[] args)
		{
			if(player.net.connection.authLevel > 0 || hasPermission(player))
			{
				if(args.Length >= 1)
				{
					destroyGlobalGUI();
					string bannerTintColor = bannerTintGrey;
        			string textColor = textWhite;
					string Msg = "";
					for(int i = 0; i < args.Length; i++)				
						Msg = Msg + " " + args[i];
					CreateGlobalMsgGUI(Msg, bannerTintColor, textColor);
				} else SendReply(player, Lang("ChatCommandAnnounceUsage", player.UserIDString));
			} return;
		}

        [HookMethod("ccmdAnnounce")]
        void ccmdAnnounce(ConsoleSystem.Arg arg)
        {
        	if(arg.isAdmin || hasPermission(arg.connection.player as BasePlayer))
        	{
        		if (arg.Args == null || arg?.Args?.Length <= 0)
        		{
        			SendReply(arg, Lang("ConsoleCommandAnnounceUsage"));
        			return;
        		}
        		if(arg.Args.Length >=1)
        		{
        			destroyGlobalGUI();
        			string bannerTintColor = bannerTintGrey;
        			string textColor = textWhite;
        			string Msg = "";
        			for(int i = 0; i < arg.Args.Length; i++)				
						Msg = Msg + " " + arg.Args[i];
        			CreateGlobalMsgGUI(Msg, bannerTintColor, textColor);
        		}
        	} return;
        } 
        
        void cmdDestroyAnnouncement(BasePlayer player, string cmd)
        {
        	if(player.net.connection.authLevel > 0 || hasPermission(player))
        	{
        		destroyGlobalGUI();
        	} return;
        }
        
        void ccmdAnnounceDestroy(ConsoleSystem.Arg arg)
        {
        	if(arg.isAdmin || hasPermission(arg.connection.player as BasePlayer))
        	{
        		destroyGlobalGUI();
        	} return;
        }
        
        void cmdAnnounceHelp(BasePlayer player, string cmd)
        {
        	if(player.net.connection.authLevel > 0 || hasPermission(player))
        	{
        		SendReply(player, Lang("AnnounceHelp", player.UserIDString));
        	} return;
        }
        
        void ccmdAnnounceHelp(ConsoleSystem.Arg arg)
        {
        	if(arg.isAdmin || hasPermission(arg.connection.player as BasePlayer))
        	{
        		SendReply(arg, Lang("AnnounceHelp"));
        	} return;
        }
        
        #endregion
   
        string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
} 