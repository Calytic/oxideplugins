using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using System.Reflection;
using Oxide.Core;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins 
{ 
	[Info("Rules GUI", "PaiN", "1.4.8", ResourceId = 1247)]
	[Description("This plugin displays the rules on connect.")] 
	class RulesGUI : RustPlugin
	{ 
		private bool backroundimage;
		private bool Changed;
		private string text;
		private bool displayoneveryconnect;
		private string kickmsg;
		private string backroundimageurl;
		
		void Loaded()  
		{
			permission.RegisterPermission("rulesgui.usecmd", this);
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("RulesGUIdata");
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
			backroundimageurl = Convert.ToString(GetConfig("Backround", "ImageURL", "https://i.ytimg.com/vi/yaqe1qesQ8c/maxresdefault.jpg"));
			backroundimage = Convert.ToBoolean(GetConfig("Backround", "Enabled", false));
			displayoneveryconnect = Convert.ToBoolean(GetConfig("Settings", "DisplayOnEveryConnect", false));
			kickmsg = Convert.ToString(GetConfig("Messages", "KICK_MESSAGE", "You disagreed with the rules!"));
			text = Convert.ToString(GetConfig("Messages", "RULES_MESSAGE", new List<string>{
			"<color=cyan>Welcome!</color> <color=red>The following in-game activities are prohibited in the Game:</color>",
			"<color=yellow>1.</color> Use of bots, use of third-party software, bugs.",
			"<color=yellow>2.</color> Pretending to be a member of Administration.",
			"<color=yellow>3.</color> Fraud, other dishonest actions.",
			"<color=yellow>4.</color> Flooding, flaming, spam, printing in capital letters (CAPS LOCK).",
			"<color=yellow>5.</color> Creating obstructions for other users.",
			"<color=yellow>6.</color> Advertisement, political propaganda."
			}));
			
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


		

		class Data
		{
			public List<string> Players = new List<string>{};
		}


		Data data;

		void Unloaded()
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(current, "RulesGUI");
			}
		}
		
		
		void UseUI(BasePlayer player, string msg)
		{ 
			var elements = new CuiElementContainer();

			var mainName = elements.Add(new CuiPanel
			{
				Image =
				{
					Color = "0.1 0.1 0.1 1"
				},
				RectTransform =
				{
					AnchorMin = "0 0",
					AnchorMax = "1 1"
				},
				CursorEnabled = true
			}, "Overlay", "RulesGUI"); 
			if(backroundimage == true)
			{
				elements.Add(new CuiElement
				{  
					Parent = "RulesGUI",
					Components =
					{
						new CuiRawImageComponent
						{
							Url = backroundimageurl,
							Sprite = "assets/content/textures/generic/fulltransparent.tga"
						}, 
						new CuiRectTransformComponent
						{
							AnchorMin = "0 0",
							AnchorMax = "1 1"
						}
					}
				});
			}				 
			var Agree = new CuiButton
            {
                Button =
                {
                    Close = mainName,
                    Color = "0 255 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.2 0.16",
					AnchorMax = "0.45 0.2"
                },
                Text =
                {
                    Text = "I Agree",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
			var Disagree = new CuiButton
            {
				
				
                Button =
                {
					Command = "global.hardestcommandtoeverguess",
                    Close = mainName,
                    Color = "255 0 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.16",
					AnchorMax = "0.75 0.2"
                },
                Text =
                {
                    Text = "I Disagree",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = msg, 
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
			}, mainName);
			elements.Add(Agree, mainName);
			elements.Add(Disagree, mainName);
			CuiHelper.AddUi(player, elements);
		}
		
		[ConsoleCommand("hardestcommandtoeverguess")]
		void cmdHardestcmd(ConsoleSystem.Arg arg)
		{
			BasePlayer player = (BasePlayer)arg.connection.player;
			Network.Net.sv.Kick(player.net.connection, rust.QuoteSafe(kickmsg));
		}
		
		[ChatCommand("rulesto")]
		void cmdRulesTo(BasePlayer player, string cmd, string[] args)
		{
			if(!permission.UserHasPermission(player.userID.ToString(), "rulesgui.usecmd"))
			{
				SendReply(player, "You do not have permission to use this command!");
				return;
			}
			if(args.Length != 1)
			{
				SendReply(player, "Syntax: /rulesto \"target\" ");
				return;
			}
			BasePlayer target = BasePlayer.Find(args[0]);
			if(target == null)
			{
				SendReply(player, "Player not found!");
				return;
			}
			string msg = "";
			foreach(var rule in Config["Messages", "RULES_MESSAGE"] as List<object>)
			msg = msg + rule.ToString() + "\n \n";
			UseUI(target, msg.ToString());
			SendReply(player, "You have displayed the rules to <color=orange> " + target.displayName + "</color>");
			
		}		
			
		[ChatCommand("rule")]
		void cmdRule(BasePlayer player, string cmd, string[] args)
		{
			string msg = "";
			foreach(var rule in Config["Messages", "RULES_MESSAGE"] as List<object>)
			msg = msg + rule.ToString() + "\n \n";
			UseUI(player, msg.ToString());
		}

		void DisplayUI(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => DisplayUI(player));
            }
            else 
			{
				string steamId = Convert.ToString(player.userID);
				if(displayoneveryconnect == true)
				{
					string msg = "";
					foreach(var rule in Config["Messages", "RULES_MESSAGE"] as List<object>)
					msg = msg + rule.ToString() + "\n \n";
					UseUI(player, msg.ToString());
				}
				else 
				{			
					if(data.Players.Contains(steamId)) return;
					string msg = "";
					foreach(var rule in Config["Messages", "RULES_MESSAGE"] as List<object>)
					msg = msg + rule.ToString() + "\n \n";
					UseUI(player, msg.ToString());
					data.Players.Add(steamId);	
					Interface.GetMod().DataFileSystem.WriteObject("RulesGUIdata", data);
				}
            }
        }
		
		
		void OnPlayerInit(BasePlayer player)		
		{
			DisplayUI(player);		
		}
	}
}