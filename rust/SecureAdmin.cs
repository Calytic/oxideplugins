using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("SecureAdmin", "OwnProx", 0.2, ResourceId = 13661)]
    [Description("Secure Admins.")]
    public class SecureAdmin : RustPlugin
    {	
	#region VARIBLES
		private Dictionary<ulong, float> Bans = new Dictionary<ulong, float>();
		private System.Timers.Timer timer;
		private DateTime NowTimePlease = new DateTime(2016, 2, 2, 0, 0, 0);
		private List<ulong> IdsToRemove = new List<ulong>();
	#endregion
	#region COMMANDS
		[ChatCommand("spectate")]
		private void SpectateChatCmd(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "CanSpecTate"))
            {
				if (!player.IsSpectating())
				{
					var target = (args.Length > 0 ? BasePlayer.Find(args[0])?.displayName : string.Empty);
					if (string.IsNullOrEmpty(target) || target == player.displayName)
					{
						PrintToChat(player, "No target has been set");
						return;
					}
					player.Die();
					player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
					player.gameObject.SetLayerRecursive(10);
					player.CancelInvoke("MetabolismUpdate");
					player.CancelInvoke("InventoryUpdate");
					PrintToChat(player, "Started Spectating");
					player.UpdateSpectateTarget(target);
					player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
				}
				else
				{
					player.SetParent(null, 0);
					player.metabolism.Reset();
					player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
					player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
					player.gameObject.SetLayerRecursive(17);
					PrintToChat(player, "Stopped Spectating");
					player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
				}
            } else SendReply(player, "You don't have spectating permissions!");
        }
		
		[ChatCommand("tempban")]
        private void TempBanPlayer(BasePlayer player, string command, string[] args)
		{
			if (permission.UserHasPermission(player.UserIDString, "CanTempBanPlayer"))
            {
				if(args.Length < 2) SendReply(player, "Syntax Error: /tempban <user> <hours>");
				else
				{
					int hour = 0;
					if(int.TryParse(args[1], out hour))
					{
						if(hour > 0 && hour < 13)
						{
							BasePlayer b = BasePlayer.Find(args[0]);							                               
							if(b!=null)
							{
								if(b.net.connection.authLevel > 0 || permission.UserHasPermission(b.UserIDString, "CanBanPlayer") || permission.UserHasPermission(b.UserIDString, "CanTempBanPlayer"))
									SendReply(player, "Cannot ban a member of the Staff!");
								else
								{
									Bans.Add(b.userID, (float)(GetTimestamp() + (3600000f * hour)));
									SendReply(player, b.displayName + " has been banned for " + hour + " hours!");
									Network.Net.sv.Kick(b.net.connection, "You have been banned for " + hour + " hours!");
								}
							}
							else SendReply(player, "Player not found!");
						} else SendReply(player, "You can only ban 1-12 hours");
					} else SendReply(player, "Failed to parse Hour!");
				}
			} else SendReply(player, "You don't have temp ban permissions!");
		}
		
		[ChatCommand("ban")]
        private void BanPlayer(BasePlayer player, string command, string[] args)
		{
			if (permission.UserHasPermission(player.UserIDString, "CanBanPlayer"))
            {
				if(args.Length < 2) SendReply(player, "Syntax Error: /ban <user> <reason>");
				else
				{
					BasePlayer b = BasePlayer.Find(args[0]);							                               
					if(b!=null)
					{
						if(b.net.connection.authLevel > 0 || permission.UserHasPermission(b.UserIDString, "CanBanPlayer"))
							SendReply(player, "Cannot ban a member of the Staff!");
						else
						{
							string Reason = "";
							for(int i = 1; i < args.Length; i++) if(i == args.Length-1) Reason += args[i]; else Reason += args[i] + " ";
							ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", b.UserIDString, player.displayName, Reason),true);
							Network.Net.sv.Kick(b.net.connection, Reason);
						}
					}
					else SendReply(player, "Player not found!");
				}
			} else SendReply(player, "You don't have ban permissions!");
		}
		
		[ChatCommand("kick")]
        private void KickPlayer(BasePlayer player, string command, string[] args)
		{
			if (permission.UserHasPermission(player.UserIDString, "CanKickPlayer"))
            {
				if(args.Length < 2) SendReply(player, "Syntax Error: /kick <user> <reason>");
				else
				{					
					BasePlayer b = BasePlayer.Find(args[0]);
					if(b!=null)
					{
						if(b.net.connection.authLevel > 0 || permission.UserHasPermission(b.UserIDString, "CanBanPlayer") || permission.UserHasPermission(b.UserIDString, "CanTempBanPlayer") || permission.UserHasPermission(b.UserIDString, "CanKickPlayer"))
							SendReply(player, "Cannot kick a member of the Staff!");
						else
						{
							string Reason = "";
							for(int i = 1; i < args.Length; i++) if(i == args.Length-1) Reason += args[i]; else Reason += args[i] + " ";
							SendReply(player, b.displayName + " has been kicked!");
							Network.Net.sv.Kick(b.net.connection, Reason);
						}
					}
					else SendReply(player, "Player not found!");
				}
			} else SendReply(player, "You don't have kick permissions!");
		}
		
		[ChatCommand("say")]
		private void SayPlayer(BasePlayer player, string command, string[] args)
		{
			if (permission.UserHasPermission(player.UserIDString, "CanSayPlayer"))
            {
				if(args.Length < 1) SendReply(player, "Syntax Error: /say <msg>");
				else 
				{
					string Msg = "";
					for(int i = 1; i < args.Length; i++) if(i == args.Length-1) Msg += args[i]; else Msg += args[i] + " ";
					ConsoleSystem.Run.Server.Quiet("say " + Msg,true);
				}
			} else SendReply(player, "You don't have say permissions!");
		}
		
		[ChatCommand("permission")]
        private void EditPlayerPermission(BasePlayer player, string command, string[] args)
		{
			if (player.net.connection.authLevel == 2)
            {
				if(args.Length < 2) SendReply(player, "Syntax Error: /auth <user> <permission>");
				else
				{					
					BasePlayer b = BasePlayer.Find(args[0]);
					if(b!=null)
					{
						switch(args[1])
						{
							case "kick":
								if(HandlePermission(b.displayName, b.UserIDString, "CanKickPlayer")) SendReply(player, b.displayName + " Kick permission added!");
								else SendReply(player, b.displayName + " Kick permission removed!");
							break;
							case "tempban":
								if(HandlePermission(b.displayName, b.UserIDString, "CanTempBanPlayer")) SendReply(player, b.displayName + " Temp ban permission added!");
								else SendReply(player, b.displayName + " Temp ban permission removed!");
							break;
							case "ban":
								if(HandlePermission(b.displayName, b.UserIDString, "CanBanPlayer")) SendReply(player, b.displayName + " Ban permission added!");
								else SendReply(player, b.displayName + " Ban permission removed!");
							break;
							case "spectate":
								if(HandlePermission(b.displayName, b.UserIDString, "CanSpecTate")) SendReply(player, b.displayName + " Spectate permission added!");
								else SendReply(player, b.displayName + " Spectate permission removed!");
							break;
							case "say":
								if(HandlePermission(b.displayName, b.UserIDString, "CanSayPlayer")) SendReply(player, b.displayName + " Say permission added!");
								else SendReply(player, b.displayName + " Say permission removed!");
							break;
						}				
						ConsoleSystem.Run.Server.Quiet("server.writecfg",true);
					}	else SendReply(player, "Player not found!");
				}
			} else SendReply(player, "You are not a admin!");
		}
	#endregion
	#region Hooks
		[HookMethod("OnPlayerInit")]
		private void OnPlayerInit(BasePlayer player)
		{
			float t = 0f;
			if(Bans.TryGetValue(player.userID, out t) && t > GetTimestamp()) Network.Net.sv.Kick(player.net.connection, "You are still banned for " + (int) Math.Round(((t-GetTimestamp()) / 60000), 0) + " minutes.");
		}
		
		[HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
			if(arg?.connection?.authLevel < 2) return null;
            if (arg?.cmd?.namefull != "global.spectate" || arg.connection == null) return null;
            SpectateChatCmd(arg.connection.player as BasePlayer, null, new[] { arg.GetString(0) });
            return true;
        }
		
		[HookMethod("Loaded")
        private void Loaded()
        {
            permission.RegisterPermission("CanTempBanPlayer", this);
            permission.RegisterPermission("CanBanPlayer", this);
            permission.RegisterPermission("CanKickPlayer", this);
			permission.RegisterPermission("CanSpecTate", this);
			permission.RegisterPermission("CanSayPlayer", this);
			timer = new System.Timers.Timer();
            timer.Interval = 36000000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(EveryTenHours);
			timer.Start();	
        }
		
		[HookMethod("Unload")]
		private void Unload()
		{
			Bans.Clear();
			timer.Stop();
			timer.Dispose();
		}			
	#endregion
	#region FUNCTIONS
		private double GetTimestamp()
		{
			return System.DateTime.UtcNow.Subtract(NowTimePlease).TotalMilliseconds;
		}
	
		private void EveryTenHours(object sender, System.Timers.ElapsedEventArgs e) //used to clean bans just incase someone never loggs back in
        {
			double time = GetTimestamp();
			foreach(KeyValuePair<ulong, float> p in Bans) if(time > p.Value) IdsToRemove.Add(p.Key);
			foreach(ulong i in IdsToRemove) Bans.Remove(i);
			IdsToRemove.Clear();
		}
	
		private bool HandlePermission(string name, string userID, string Permission)
		{
			if(permission.UserHasPermission(userID, Permission)) 
			{
				ConsoleSystem.Run.Server.Quiet(string.Format("oxide.revoke user {0} {1}", name, Permission),true);
				return false;
			}
			else ConsoleSystem.Run.Server.Quiet(string.Format("oxide.grant user {0} {1}", name, Permission),true);
			return true;
		}
	#endregion		
    }
}