using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Authentication", "Jos\u00E9 Paulo (FaD)", 0.2)]
    [Description("Players must enter a password after they wake up or else they'll be kicked.")]
    public class Authentication : RustPlugin
    {
		/*----------------*/
		/*Plugin Variables*/
		/*----------------*/
		
		static Dictionary<string, Timer> authing = new Dictionary<string, Timer>();
		static Dictionary<string, BasePlayer> auths = new Dictionary<string, BasePlayer>();
		
		/*----------------*/
		/*Plugin Functions*/
		/*----------------*/
		
		/*Message Functions*/
		private void Write(string message)
		{
			PrintToChat("<color=lightblue>[AUTH]</color> {0}", message);
		}
		
		private void Write(BasePlayer player, string message)
		{
			PrintToChat(player, "<color=lightblue>[AUTH]</color> {0}", message);
		}
		
		/*Auth Functions*/
		private void Auth(BasePlayer player, bool authed = true)
		{
			//Remove player from the dictionary. This process is done to both authed and non-authed players;
			Timer auth;
			string steamID = player.userID.ToString();
			authing.TryGetValue(steamID, out auth);
			auth.Destroy();
			authing.Remove(steamID);
			
			//Add player to the auths dictionary to prevent him from using the /auth [password] command again;
			if(authed)
			{
				auths.Add(steamID, player);
			}
			
			//Kick the player after timer ended and no password was entered;
			if(!authed)
				player.Kick(Config["AUTHENTICATION_TIMED_OUT"].ToString());
		}
		
		private void RequestAuth(BasePlayer player)
		{
			string steamID = player.userID.ToString();
			//Find and replace "{TIMEOUT}" to the timeout set in the config file;
			string request = Config["PASSWORD_REQUEST"].ToString().Replace("{TIMEOUT}",Config["TIMEOUT"].ToString());
			
			authing.Add( steamID, timer.Once(Convert.ToSingle(Config["TIMEOUT"]), () => Auth(player,false)) );
			
			Write(player, request);
		}
		
		/*Chat Commands*/
		[ChatCommand("auth")]
		private void cmdAuth(BasePlayer player, string cmd, string[] args)
		{
			string steamID = player.userID.ToString();
			//Limit avaliable commands if player is not authed yet.
			if(!auths.ContainsKey(steamID))
			{
				switch(args.Length)
				{
					case 0:
						Write(player, Config["SYNTAX_ERROR"].ToString());
						break;
					case 1:
						if(args[0] == Config["PASSWORD"].ToString())
						{
							Auth(player);
							Write(player, Config["AUTHENTICATION_SUCCESSFUL"].ToString());
						}
						else
						{
							Write(player, Config["INCORRECT_PASSWORD"].ToString());
						}
						break;
					default:
						Write(player, Config["INVALID_COMMAND"].ToString());
						break;
				}
			}
			else //Player was already authed;
			{
				if(permission.UserHasPermission(steamID, "auth.edit")) // Auth moderator;
				{
					switch(args.Length)
					{
						case 1:
							if(args[0] == Config["PASSWORD"].ToString())
							{
								Write(player, Config["ALREADY_AUTHED"].ToString());
							}
							else if(args[0] == "help")// /auth help;
							{
								Write(player, 
								"Available commands:\n"
								+ "<color=silver>/auth [password]</color> - Authenticates players;\n"
								+ "<color=silver>/auth password</color> - Shows current password;\n"
								+ "<color=silver>/auth password [new password]</color> - Sets new password;");
							}
							else if(args[0] == "password")// /auth password;
							{
								Write(player, "Current password: " + Config["PASSWORD"].ToString());
							}
							else
							{
								Write(player, Config["HELP"].ToString());
							}
							break;
						case 2:
							if(args[0] == "password")// /auth password [new password];
							{
								if(args[1] != "password" && args[1] != "help")
								{
									Config["PASSWORD"] = args[1];
									SaveConfig();
									Write(player, "Current password set to: " + Config["PASSWORD"].ToString());
								}
								else
								{
									Write(player, Config["INCORRECT_PASSWORD"].ToString());
								}
							}
							else
							{
								Write(player, Config["HELP"].ToString());
							}
							break;
						default:
							Write(player, Config["HELP"].ToString());
							break;
					}
				}
				else //Not an Auth Moderator;
				{
					switch(args.Length)
					{
						case 1:
							Write(player, Config["ALREADY_AUTHED"].ToString());
							break;
					}
				}
			}
		}
		
		/*------------*/
		/*Plugin Hooks*/
		/*------------*/
		
		void Init()
		{
			permission.RegisterPermission("auth.edit", this);
			LoadDefaultConfig();
		}
		
		protected override void LoadDefaultConfig()
		{
			bool outdated = false;
			bool deprecated = false;
			
			if(Config["TIMEOUT"] == null)
			{
				Config["TIMEOUT"] = 30;
				outdated = true;
			}
			if(Config["PASSWORD"] == null)
			{
				Config["PASSWORD"] = "changeme";
				outdated = true;
			}
			if(Config["PASSWORD_REQUEST"] == null)
			{
				Config["PASSWORD_REQUEST"] = "Type /auth [password] in the following {TIMEOUT} seconds to authenticate or you'll be kicked.";
				outdated = true;
			}
			if(Config["SYNTAX_ERROR"] == null)
			{
				Config["SYNTAX_ERROR"] = "Correct syntax: /auth [password]";
				outdated = true;
			}
			if(Config["INCORRECT_PASSWORD"] == null)
			{
				Config["INCORRECT_PASSWORD"] = "Incorrect password. Please try again.";
				outdated = true;
			}
			if(Config["AUTHENTICATION_TIMED_OUT"] == null)
			{
				Config["AUTHENTICATION_TIMED_OUT"] = "You took too long to authenticate";
				outdated = true;
			}
			if(Config["AUTHENTICATION_SUCCESSFUL"] == null)
			{
				Config["AUTHENTICATION_SUCCESSFUL"] = "Authentication sucessful.";
				outdated = true;
			}
			if(Config["INVALID_COMMAND"] == null)
			{
				Config["INVALID_COMMAND"] = "Invalid command or you must be authed to do this.";
				outdated = true;
			}
			if(Config["HELP"] == null)
			{
				Config["HELP"] = "Type /help for all available commands.";
				outdated = true;
			}
			if(Config["ALREADY_AUTHED"] == null)
			{
				Config["ALREADY_AUTHED"] = "You're already authed.";
				outdated = true;
			}
				
			/*--------------------*/
			/*Deprecated Variables*/
			/*--------------------*/
			if(Config["AUTHENTICATION_SUCCESSFULL"] != null)
			{
				Config["AUTHENTICATION_SUCCESSFULL"] = "DEPRECATED_VARIABLE_VERSION_0.1";
				deprecated = true;
			}
			
			/*-------------*/
			/*Print Warning*/
			/*-------------*/
			if(outdated)
			{
				PrintWarning("New variable(s) added to Config file! Reconfiguration may be required.");
			}
			
			if(deprecated)
			{
				PrintWarning("Deprecated variable(s) found and replaced in Config file! Reconfiguration may be required.");
			}
				
			SaveConfig();
			
		}
		
		void OnPlayerSleepEnded(BasePlayer player)
		{
			string steamID = player.userID.ToString();
			//Prevents request from being executed when player respawn;
			if(!auths.ContainsKey(steamID))
			{
				timer.Once(1, () => RequestAuth(player));
			}
		}
		
		void OnPlayerDisconnected(BasePlayer player)
		{
			string steamID = player.userID.ToString();
			auths.Remove(steamID);
		}
		
    } //End of Plugin;
} //End of namespace;