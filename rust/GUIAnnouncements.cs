using System;
using System.Collections.Generic;

using UnityEngine;
using Rust;

using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("GUIAnnouncements", "JoeSheep", "1.0.2", ResourceId = 1222)]
    [Description("Creates announcements with custom messages by command across the top of every player's screen in a banner.")]
    
    public class GUIAnnouncements : RustPlugin
    {	
    	#region Configuration
    	
//    	  const string permAnnounce = "GUIAnnouncements.announce";
    	
    	System.Timers.Timer timer;
    	
		bool active = false;
    	
		int msgShowDuration = 10;
		int fontSize = 15;
		
		protected override void LoadDefaultConfig()
		{
			PrintWarning("Creating a new configuration file.");
            Config.Clear();
		    Config["MessageShowDuration"] = msgShowDuration;
		    Config["FontSize"] = fontSize;
		    SaveConfig();
		}
		
		private void LoadConfig()
		{
			msgShowDuration = GetConfig<int>("MessageShowDuration", msgShowDuration);
			if (msgShowDuration == 0)
			{
				PrintWarning("Config MessageShowDuration set to 0, resetting to default.");
				int msgShowDuration = 10;
				Config["MessageShowDuration"] = msgShowDuration;
				SaveConfig();
				LoadConfig();
			}
			fontSize = GetConfig<int>("FontSize", fontSize);
			if (fontSize > 33 | fontSize == 0)
			{
				PrintWarning("Config FontSize greater than 33 or 0, resetting to default.");
				int fontSize = 15;
				Config["FontSize"] = fontSize;
				SaveConfig();
				LoadConfig();
			}
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
			                      },this);
		}
		
		#endregion
		
		#region Initialization
		
		void OnServerInitialized()
		{
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif
            
            LoadConfig();
            LoadDefaultMessages();
//            permission.RegisterPermission(permAnnounce, this);
            createTimer();
            
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
        
		void LoadMsgGui(string Msg)
		{	
			char[] newMsgChar = {'\''};
			string newMsg = Msg.TrimEnd(newMsgChar).TrimStart(newMsgChar);
			
        	var elements = new CuiElementContainer();
        		elements.Add(new CuiElement
					{
        				Name = announcementGUI,
        				Components = 
        				{
							new CuiImageComponent {Color = "0.1 0.1 0.1 0.7", FadeIn = 1},
							new CuiRectTransformComponent {AnchorMin = "-0.027 0.91", AnchorMax = "1.026 0.99"}
        				},
        				FadeOut = 1
        			});
        		elements.Add(new CuiElement
        	        {
        	            Name = announcementText,
        	            Components =
        	            {
        	             	new CuiTextComponent {Text = newMsg, FontSize = fontSize, Align = TextAnchor.MiddleCenter, FadeIn = 1},
        	             	new CuiRectTransformComponent {AnchorMin = "-0.027 0.91", AnchorMax = "1.026 0.99"}
        	            },
        	            FadeOut = 1
        	        });
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.AddUi(player, elements);
				active = true;
			}
			timer.Start();
		}
		
		#endregion
		
		#region Functions
		
		void createTimer()
		{
			timer = new System.Timers.Timer(1000* msgShowDuration);
            timer.Interval = 1000 * msgShowDuration;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
		}
		
		void destroyUI()
		{
			timer.Stop();
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, announcementGUI);
				CuiHelper.DestroyUi(player, announcementText);
				active = false;
			}
		} 
		
		void Unload()
		{
			if(active)
			{
				destroyUI();
			}
		}
		
		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
			destroyUI();
		}
		
		#endregion
		
		#region Commands
		
        void cmdAnnounce(BasePlayer player, string cmd, string[] args)
		{
			if(player.net.connection.authLevel > 0)
			{
				if(args.Length >= 1)
				{
					if(active);
					{
						destroyUI();
        			}
					string Msg = "";
					for(int i = 0; i < args.Length; i++)				
						Msg = Msg + " " + args[i];
					LoadMsgGui(Msg);
				} else SendReply(player, Lang("ChatCommandAnnounceUsage", player.UserIDString));
			} else SendReply(player, Lang("NoPermission", player.UserIDString));
		}

        void ccmdAnnounce(ConsoleSystem.Arg arg)
        {
        	if(!arg.isAdmin)
        	{
        		SendReply(arg, Lang("NoPermission"));
        		return;
        	}
        	if (arg.Args == null || arg?.Args?.Length <= 0)
        	{
        		SendReply(arg, Lang("ConsoleCommandAnnounceUsage"));
        		return;
        	}
        	if(arg.Args.Length >=1)
        	{
        		if(active);
        		{
        			destroyUI();
        		}
        		string Msg = arg.Args[0];
        		LoadMsgGui(Msg);
        	}
        }
        
        void cmdDestroyAnnouncement(BasePlayer player, string cmd)
        {
        	if(player.net.connection.authLevel >0)
        	{
        		destroyUI();
        	} else SendReply(player, Lang("NoPermission", player.UserIDString));
        }
        
        void ccmdAnnounceDestroy(ConsoleSystem.Arg arg)
        {
        	if(arg.isAdmin)
        	{
        		destroyUI();
        	} else SendReply(arg, Lang("NoPermission"));
        }
        
        void cmdAnnounceHelp(BasePlayer player, string cmd)
        {
        	if(player.net.connection.authLevel >0)
        	{
        		SendReply(player, Lang("AnnounceHelp", player.UserIDString));
        	} else SendReply(player, Lang("NoPermission", player.UserIDString));
        }
        
        void ccmdAnnounceHelp(ConsoleSystem.Arg arg)
        {
        	if(arg.isAdmin)
        	{
        		SendReply(arg, Lang("AnnounceHelp"));
        	} SendReply(arg, Lang("NoPermission"));
        }
        
        #endregion
        
        string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);
    }
}