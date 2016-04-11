using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
namespace Oxide.Plugins
{
	[Info("AdvMetabolism", "BDM", "1.2.1")]
    [Description("Easily control the metabolism of a player.")]
	public class AdvMetabolism : RustLegacyPlugin	
	{
        void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPermissionHelp", "You do not have permission to use '[color orange]/advmetahelp[color white]'"},
                {"PluginVersion", "AdvMetabolism Plugin v1.2.1 by: BDM"},
                {"HelpIntro", "For [COLOR ORANGE]detailed help [COLOR WHITE]use this syntax: [COLOR CYAN]/'command'help"},
                {"HelpExample", "Example: [COLOR CYAN]/feedhelp"},
                {"HelpAbilityIntro", "The following are the current abilities of the plugin:"},
                {"HelpFeedIntro", "'[COLOR CYAN]/feed[COLOR WHITE]'"},
                {"HelpStarveIntro", "'[COLOR CYAN]/starve[COLOR WHITE]'"},
                {"HelpHealIntro", "'[COLOR CYAN]/heal[COLOR WHITE]'"},
                {"HelpCureIntro", "'[COLOR CYAN]/cure[COLOR WHITE]'"},
                {"HelpPoisonIntro", "'[COLOR CYAN]/poison[COLOR WHITE]'"},
                {"HelpRadIntro", "'[COLOR CYAN]/rad[COLOR WHITE]'"},
                {"HelpKillIntro", "'[COLOR CYAN]/kill[COLOR WHITE]'"},
                {"HelpVitalsIntro", "'[COLOR CYAN]/vitals[COLOR WHITE]'"},
                {"CommandIntroFeed", "AdvMetabolism - Feed Help"},
                {"NoPermissionFeed", "You do not have permission to use '[color orange]/feed[color white]'"},
                {"CommandUsageFeed", "Syntax: [COLOR CYAN]/feed [COLOR WHITE]or [COLOR CYAN]/feed [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionFeed", "[COLOR ORANGE]This command will max out the targeted user's food/calories"},
                {"CommandIntroStarve", "AdvMetabolism - Starve Help"},
                {"NoPermissionStarve", "You do not have permission to use '[color orange]/starve[color white]'"},
                {"CommandUsageStarve", "Syntax: [COLOR CYAN]/starve [COLOR WHITE]or [COLOR CYAN]/starve [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionStarve", "[COLOR ORANGE]This command will clear the targeted user's food/calories"},
                {"CommandIntroHeal", "AdvMetabolism - Heal Help"},
                {"NoPermissionHeal", "You do not have permission to use '[color orange]/heal[color white]'"},
                {"CommandUsageHeal", "Syntax: [COLOR CYAN]/heal [COLOR WHITE]or [COLOR CYAN]/heal [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionHeal", "[COLOR ORANGE]This command will max out the targeted user's health"},
                {"CommandIntroCure", "AdvMetabolism - Cure Help"},
                {"NoPermissionCure", "You do not have permission to use '[color orange]/cure[color white]'"},
                {"CommandUsageCure", "Syntax: [COLOR CYAN]/cure [COLOR WHITE]or [COLOR CYAN]/cure [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionCure", "[COLOR ORANGE]This command will clear any form of injury"}, 
                {"CommandIntroPoison", "AdvMetabolism - Feed Help"},
                {"NoPermissionPoison", "You do not have permission to use '[color orange]/poison[color white]'"},
                {"CommandUsagePoison", "Syntax: [COLOR CYAN]/poison [COLOR WHITE]or [COLOR CYAN]/poison [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionPoison", "[COLOR ORANGE]This command will apply poison to the targeted user"},
                {"CommandIntroRad", "AdvMetabolism - Rad Help"},
                {"NoPermissionRad", "You do not have permission to use '[color orange]/rad[color white]'"},
                {"CommandUsageRad", "Syntax: [COLOR CYAN]/rad [COLOR WHITE]or [COLOR CYAN]/rad [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionRad", "[COLOR ORANGE]This command will make the targeted user gain 500 rads"},          
                {"CommandIntroKill", "AdvMetabolism - Kill Help"},
                {"NoPermissionKill", "You do not have permission to use '[color orange]/kill[color white]'"},
                {"CommandUsageKill", "Syntax: [COLOR CYAN]/kill [COLOR WHITE]or [COLOR CYAN]/kill [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionKill", "[COLOR ORANGE]This command will max out the targeted user's rads, forcing them to die"}, 
                {"CommandIntroVitals", "AdvMetabolism - Vitals Help"},
                {"CommandUsageVitals", "Syntax: [COLOR CYAN]/vitals *coming soon*[COLOR WHITE]or [COLOR CYAN]/vitals [COLOR WHITE]'[COLOR YELLOW]user[COLOR WHITE]'"},
                {"CommandDescriptionVitals", "[COLOR ORANGE]This command will display the users vitals"}, 
                {"FeedSelf", "You have been fed!"},           
                {"FeedTargetReply", "You have fed - [color yellow]"},           
                {"FeedTargetMessage", "You have been fed by - [color cyan]"},     
                {"HealSelf", "You have been healed!"},           
                {"HealTargetReply", "You have healed - [color yellow]"},           
                {"HealTargetMessage", "You have been healed by - [color cyan]"},           
                {"CureSelf", "You have been cured!"},           
                {"CureTargetReply", "You have cured - [color yellow]"},           
                {"CureTargetMessage", "You have been cured by - [color cyan]"},        
                {"StarveSelf", "You have been starved!"},           
                {"StarveTargetReply", "You have starved - [color yellow]"},           
                {"StarveTargetMessage", "You have been starved by - [color cyan]"},    
                {"PoisonSelf", "You have been poisoned!"},           
                {"PoisonTargetReply", "You have poisoned - [color yellow]"},           
                {"PoisonTargetMessage", "You have been poisoned by - [color cyan]"},    
                {"RadSelf", "You have been radiated!"},           
                {"RadTargetReply", "You have radiated - [color yellow]"},           
                {"RadTargetMessage", "You have been radiated by - [color cyan]"}, 
                {"KillSelf", "You have been killed!"},           
                {"KillTargetReply", "You have killed - [color yellow]"},           
                {"KillTargetMessage", "You have been killed by - [color cyan]"},
                {"NoPermissionVitals", "You dont have permission to use '[color orange]/vitals[color white]'"},    
                {"SelfVitals", "Self vitals will be coming soon!"},        
                {"TargetVitals", "Your targets vitals are:"}    
            };
            lang.RegisterMessages(messages, this);
        }
        void Loaded()
        {
            LoadDefaultMessages();
            permission.RegisterPermission("advmetabolism.allowed", this);
        }
                [ChatCommand("advmetahelp")]
		void cmdAdvMetaHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
                SendReply(netuser, GetMessage("NoPermissionHelp", netuser.userID.ToString()));
			}
			else { 
                rust.Notice(netuser, GetMessage("PluginVersion", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("HelpIntro", netuser.userID.ToString())));
                timer.Once(1, () => SendReply(netuser, GetMessage("HelpExample", netuser.userID.ToString())));
                timer.Once(3, () => SendReply(netuser, GetMessage("HelpAbilityIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpFeedIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpStarveIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpHealIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpCureIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpPoisonIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpRadIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpKillIntro", netuser.userID.ToString())));
                timer.Once(4, () => SendReply(netuser, GetMessage("HelpVitalsIntro", netuser.userID.ToString())));
                }
		} 
                        [ChatCommand("feedhelp")]
		void cmdFeedHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionFeed", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroFeed", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageFeed", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionFeed", netuser.userID.ToString())));
                }
		} 
                        [ChatCommand("starvehelp")]
		void cmdStarveHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionStarve", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroStarve", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageStarve", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionStarve", netuser.userID.ToString())));
                }
		} 
                        [ChatCommand("healhelp")]
		void cmdHealHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionHeal", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroHeal", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageHeal", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionHeal", netuser.userID.ToString())));
                }
		} 
                                [ChatCommand("curehelp")]
		void cmdCureHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionCure", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroCure", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageCure", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionCure", netuser.userID.ToString())));
                }
		} 
                                        [ChatCommand("poisonhelp")]
		void cmdPoisonHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionPoison", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroPoison", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsagePoison", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionPoison", netuser.userID.ToString())));
                }
		} 
                                [ChatCommand("radhelp")]
		void cmdRadHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionRad", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroRad", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageRad", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionRad", netuser.userID.ToString())));
                }
		} 
                                [ChatCommand("killhelp")]
		void cmdKillHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionKill", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroKill", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageKill", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionKill", netuser.userID.ToString())));
                }
		} 
                                [ChatCommand("vitalshelp")]
		void cmdVitalsHelp(NetUser netuser, string command, string[] args)
		{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
			{
				SendReply(netuser, GetMessage("NoPermissionVitals", netuser.userID.ToString())); return;
			}
			else { 
                rust.Notice(netuser, GetMessage("CommandIntroVitals", netuser.userID.ToString()));
                timer.Once(1, () => SendReply(netuser, GetMessage("CommandUsageVitals", netuser.userID.ToString())));
                timer.Once(2, () => SendReply(netuser, GetMessage("CommandDescriptionVitals", netuser.userID.ToString())));
                }
		} 
         [ChatCommand("feed")]
        void cmdFeedPlayer(NetUser netuser, string command, string[] args)
				{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionFeed", netuser.userID.ToString()));  
					}
					else if (args.Length != 1)
					{
					  var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddCalories(3000);
			         SendReply(netuser, GetMessage("FeedSelf", netuser.userID.ToString()));
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddCalories(3000);
			SendReply(netuser, GetMessage("FeedTargetReply", netuser.userID.ToString()) + targetuser.displayName);
            SendReply(targetuser, GetMessage("FeedTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                         [ChatCommand("starve")]
        void cmdStarvePlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
				    SendReply(netuser, GetMessage("NoPermissionStarve", netuser.userID.ToString()));
                    return;  
					}
					else if (args.Length != 1)
					{
					  var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.SubtractCalories(3000);
			         SendReply(netuser, GetMessage("NoPermissionStarve", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.SubtractCalories(3000);
			SendReply(netuser, GetMessage("StarveTargetReply", netuser.userID.ToString()) + targetuser.displayName);
            SendReply(targetuser, GetMessage("StarveTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                                         [ChatCommand("heal")]
        void cmdHealPlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionHeal", netuser.userID.ToString())); 
                        return; 
					}
					else if (args.Length != 1)
					{
                     var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     rootCharacter.takeDamage.health = 100;
			         SendReply(netuser, GetMessage("HealTargetMessage", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     rootCharacter.takeDamage.health = 100;
			SendReply(netuser, GetMessage("HealTargetReply", netuser.userID.ToString()) + targetuser.displayName);
            SendReply(targetuser, GetMessage("HealTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                                 [ChatCommand("cure")]
        void cmdCurePlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionCure", netuser.userID.ToString()));  
                        return;
					}
					else if (args.Length != 1)
					{
                     var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable) return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
			         Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
			         metabolism.AddCalories(3000);
			         float radLevel = metabolism.GetRadLevel();
                     metabolism.AddAntiRad(radLevel);
			         FallDamage fallDamage = rootControllable.GetComponent<FallDamage>();
			         fallDamage.ClearInjury();
			         HumanBodyTakeDamage humanBodyTakeDamage = rootControllable.GetComponent<HumanBodyTakeDamage>();
			         humanBodyTakeDamage.SetBleedingLevel(0);
			         SendReply(netuser, GetMessage("CureSelf", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable) return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
			         Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
			         metabolism.AddCalories(3000);
			         float radLevel = metabolism.GetRadLevel();
                     metabolism.AddAntiRad(radLevel);
			         FallDamage fallDamage = rootControllable.GetComponent<FallDamage>();
			         fallDamage.ClearInjury();
			         HumanBodyTakeDamage humanBodyTakeDamage = rootControllable.GetComponent<HumanBodyTakeDamage>();
			         humanBodyTakeDamage.SetBleedingLevel(0);
			         SendReply(netuser, GetMessage("CureTargetReply", netuser.userID.ToString()) + targetuser.displayName);
                     SendReply(targetuser, GetMessage("CureTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                 [ChatCommand("poison")]
        void cmdPoisonPlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionPoison", netuser.userID.ToString()));  
                        return;
					}
					else if (args.Length != 1)
					{
					  var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable) return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddPoison(10);
			         SendReply(netuser, GetMessage("PoisonSelf", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable) return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddPoison(10);
			         SendReply(netuser, GetMessage("PoisonTargetReply", netuser.userID.ToString()) + targetuser.displayName);
                     SendReply(targetuser, GetMessage("PoisonTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                                 [ChatCommand("rad")]
        void cmdRadPlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionRad", netuser.userID.ToString())); 
                        return; 
					}
					else if (args.Length != 1)
					{
					  var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddRads(500);
			         SendReply(netuser, GetMessage("RadSelf", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddRads(500);
			         SendReply(netuser, GetMessage("RadTargetReply", netuser.userID.ToString()) + targetuser.displayName);
                     SendReply(targetuser, GetMessage("RadTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }
                                         [ChatCommand("kill")]
        void cmdKillPlayer(NetUser netuser, string command, string[] args)
				{

            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionKill", netuser.userID.ToString()));  
                        return;
					}
					else if (args.Length != 1)
					{
					  var rootControllable = netuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddRads(99999999);
			       	netuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
			         SendReply(netuser, GetMessage("KillSelf", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
                     var rootControllable = targetuser.playerClient.rootControllable;
			         if (!rootControllable)return;
                     var rootCharacter = rootControllable.rootCharacter;
			         if (!rootCharacter) return;
                     Metabolism metabolism = rootControllable.GetComponent<Metabolism>();
                     metabolism.AddRads(99999999);
                     targetuser.playerClient.rootControllable.rootCharacter.takeDamage.SetGodMode(false);
			         SendReply(netuser, GetMessage("KillTargetReply", netuser.userID.ToString()) + targetuser.displayName);
                     SendReply(targetuser, GetMessage("KillTargetMessage", netuser.userID.ToString()) + netuser.displayName);
		}
			}
                }                
              [ChatCommand("vitals")]
        void cmdVitalCheck(NetUser netuser, string command, string[] args)
				{
            if (!permission.UserHasPermission(netuser.playerClient.userID.ToString(), "advmetabolism.allowed"))
					{ 
						SendReply(netuser, GetMessage("NoPermissionVitals", netuser.userID.ToString()));  
                        return;
					}
					else if (args.Length != 1)
					{
			         SendReply(netuser, GetMessage("CommandUsageVitals", netuser.userID.ToString()));
                     return;
					}
					else
					{
						NetUser targetuser = rust.FindPlayer(args[0]);
						if (targetuser != null)
						{
			         SendReply(netuser, GetMessage("TargetVitals", netuser.userID.ToString()));
                      VitalControl(netuser, targetuser);
		}
			}
                }
void VitalControl(NetUser netuser, NetUser targetuser)
{
	var player = targetuser.playerClient.controllable;
	var character = player.character;
	var metabolism = character.GetComponent<Metabolism>();
	var hp = character.health;
	var cal = metabolism.GetCalorieLevel();
	var rad = metabolism.GetRadLevel();
	var health = "Check health ranges";
	var calorie = "Check calorie ranges";
	var rads = "Check rad ranges";
if(rad < 250 && rad > -1  || rad == 250)
{
rads = ("[color green]" + rad);
}
if(rad < 500 && rad > 250 || rad == 500)
{
rads = ("[color orange]" + rad);
}
if(rad < 3000 && rad > 500 || rad == 3000)
{
rads = ("[color red]" + rad);
}
if(cal < 100 && cal > -1 || cal == 100)
{
calorie = ("[color red]" + cal);
}
if(cal < 750 && cal > 100 || cal == 750)
{
calorie = ("[color orange]" + cal);
}
if(cal < 1500 && cal > 750 || cal == 1500)
{
calorie = ("[color yellow]" + cal);
}
if(cal < 3000 && cal > 1500 || cal == 3000)
{
calorie = ("[color green]" + cal);
}
if(hp < 25 && hp > -1 || hp == 25)
{
health = ("[color red]" + hp);
}
if(hp < 40 && hp > 25 || hp == 40)
{
health = ("[color orange]" + hp);
}
if(hp < 75 && hp > 40 || hp == 75)
{
health = ("[color yellow]" + hp);
}
if(hp < 100 && hp > 75 || hp == 100)
{
health = ("[color green]" + hp);
}
SendReply(netuser, "[color yellow]" + targetuser.displayName + "[color white] - Health:" + health + "[color white] - Calories:" + calorie + "[color white] - Rads:" + rads);
return;
}
        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
    }
}