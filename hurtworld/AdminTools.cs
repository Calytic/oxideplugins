// Reference: UnityEngine.UI
using Oxide.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using uLink;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("AdminTools", "Noviets", "1.2.5", ResourceId = 1584)]
    [Description("Provides chat commands")]

    class AdminTools : HurtworldPlugin
    {
		
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","AdminTools: You dont have Permission to do this!"},
                {"playernotfound","AdminTools: That player does not exist, or is not online."},
				{"noreason","AdminTools: You must provide a reason to kick."},
				{"banfail","AdminTools: Incorrect Usage: /Ban (Player|IP|SteamID)"},
				{"tempbanfail","AdminTools: Incorrect Usage: /TempBan (Player|IP|SteamID) (Duration in minutes)"},
				{"godfail","AdminTools: Incorrect Usage: /Godmode (on|off)"},
				{"unbanfail","AdminTools: Invalid SteamID. Please try again: /unban SteamID"},
				{"kicked","<color=#ff0000>{Player}</color> has been <color=#ff0000>KICKED</color> for: <color=#ffa500>{Reason}</color>"},
				{"banned","<color=#ff0000>{Player}</color> has been <color=#ff0000>BANNED</color>."},
				{"tempbanned","<color=#ff0000>{Player}</color> has been <color=#ff0000>BANNED</color> for <color=orange>{Duration}</color> minutes"},
				{"tempcheckfail","AdminTools: Invalid Syntax - /CheckTempBan (IP|SteamID|PlayerName)"},
				{"istempbanned","AdminTools: {Name} is TempBanned for {Duration}"},
				{"isnttempbanned","AdminTools: {Name} is not TempBanned"},
				{"isnolongertempbanned","AdminTools: {Name} is no longer TempBanned"},
				{"tempremovefail","AdminTools: Invalid Syntax - /RemoveTempBan (IP|SteamID|PlayerName)"},
				{"unbanned","AdminTools: You have Unbanned SteamID: {ID}"},
				{"banmsg","You have been Banned."},
				{"healfail","AdminTools: Incorrect Usage. /Heal -or- /Heal (Player)"},
				{"tempbanmsg","You have been TempBanned for {Duration} minutes."},
				{"godkill","Killing in Godmode."},
				{"notgod","AdminTools: You are not in Godmode."},
				{"alreadygod","AdminTools: You are already in Godmode."},
				{"muted","<color=#ff0000>{Player}</color> has been <color=#ff0000>Muted</color> for {time} seconds.{reason}"},
				{"mutewarning","<color=#ff0000>Please be aware; your mute duration will increase on each attempt to speak</color>"},
				{"unmuted","<color=#ff0000>{Player}</color> is no longer <color=#ff0000>Muted</color>."},
				{"mutefail","AdminTools: Incorrect Usage: /mute (Player|IP|SteamID) (Duration)"},
				{"frozenmsg","AdminTools: You have Frozen {Player}."},
				{"unfrozenmsg","AdminTools: {Player} is no longer Frozen.<color>"},
				{"frozen","<color=#ff0000>You have been Frozen.</color>"},
				{"unfrozen","<color=#ff0000>You are no longer Frozen.</color>"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		
		public class TBA
		{
			public string ID;
			public string Name;
			public string IP;
			public DateTime TillExpire;

			public TBA(string iID, string iName, string iIP, DateTime TE)
			{
				ID = iID;
				Name = iName;
				IP = iIP;
				TillExpire = TE;
			}
		}

		
		protected override void LoadDefaultConfig()
        {
			if(Config["ShowConsoleMsg"] == null) Config.Set("ShowConsoleMsg", false);
			if(Config["KillingInGodBan"] == null) Config.Set("KillingInGodBan", false);
			if(Config["KillingInGodKick"] == null) Config.Set("KillingInGodKick", false);
            SaveConfig();
        }
		
		string GetNameOfObject(UnityEngine.GameObject obj){
			var ManagerInstance = GameManager.Instance;
			return ManagerInstance.GetDescriptionKey(obj);
		}

		List<ulong> Godlist = new List<ulong>();
		List<TBA> TempBans = new List<TBA>();
		DateTime TimeLeft;
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		
		void Loaded()
        {
			try{
				TempBans = Interface.GetMod().DataFileSystem.ReadObject<List<TBA>>("AdminTools/TempBans");
			}catch{SaveTempBans();}
            permission.RegisterPermission("admintools.kick", this);
            permission.RegisterPermission("admintools.ban", this);
			permission.RegisterPermission("admintools.tempban", this);
			permission.RegisterPermission("admintools.godmode", this);
			permission.RegisterPermission("admintools.mute", this);
			permission.RegisterPermission("admintools.freeze", this);
			permission.RegisterPermission("admintools.all", this);
			LoadDefaultMessages();
			LoadDefaultConfig();
		}
		
		void SaveGodlist() => Interface.GetMod().DataFileSystem.WriteObject("AdminTools/Godlist", Godlist);

		void SaveTempBans() => Interface.GetMod().DataFileSystem.WriteObject("AdminTools/TempBans", TempBans);

		private void OnPlayerDisconnect(PlayerSession session)
		{ 
			if (Godlist.Contains((ulong)session.SteamId))
			{
				Godlist.Remove((ulong)session.SteamId);
				SaveGodlist();
			}
		}

		private PlayerSession GetSession(String source) 
		{
			var IPCheck = Regex.Match(source, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
			try
			{
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> p in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(p.Value != null)
					{
						if(p.Value.IsLoaded) {
							if (source.ToLower() == p.Value.Name.ToLower())
								return p.Value;
						}
					}
				}
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if(pair.Value != null)
					{
						if(pair.Value.IsLoaded)
						{
							if (IPCheck.Success)
							{
								if (source == pair.Value.Player.ipAddress)
									return pair.Value;
							}
							else if(source == pair.Value.SteamId.ToString())
								return pair.Value;
							else if (pair.Value.Name.ToLower().Contains(source.ToLower()))
								return pair.Value;
						}
					}
				}
			}
			catch {
				Puts("An error occurred fetching session");
				return null; 
			}
			return null;
		}

		object OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				string KillName = GetNameOfObject(source.EntitySource);
				if(KillName != "")
				{
					string Killer = KillName.Replace("(P)","");
					if ((bool) Config["KillingInGodBan"] || (bool) Config["KillingInGodKick"] )
					{
						PlayerSession person = GetSession(Killer);
						if(person !=null)
						{
							ulong ID = (ulong)person.SteamId;
							if (Godlist.Contains(ID))
							{
								if ((bool) Config["KillingInGodKick"])
									Singleton<GameManager>.Instance.KickPlayer(ID.ToString(), Msg("godkill"));
								if(!person.IsAdmin)
								{
									if ((bool) Config["KillingInGodBan"])
									{
										ConsoleManager.Instance?.ExecuteCommand("ban " + ID);
										Singleton<GameManager>.Instance.KickPlayer(ID.ToString(), Msg("godkill"));
									}
								}
							}
						}
					}
				}
				if(Godlist.Contains((ulong)session.SteamId))
				{
					EntityStats stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
					timer.Once(0.1f,()=>
					{
						stats.GetFluidEffect(EEntityFluidEffectType.Health).SetValue(100f);
					});
					return false;
				}
			}
			return null;
		}

		void CanClientLogin(PlayerSession session)
        {
			var Joiner = CheckBanJoin(session.Player.ipAddress, session.Name, session.SteamId.ToString()) as TBA;
			if (Joiner != null)
			{
				System.DateTime CurrentTime = System.DateTime.UtcNow;
				if(Joiner.TillExpire < CurrentTime)
				{
					TempBans.Remove(Joiner);
					return;
				}
					string[] TidyTime = Joiner.TillExpire.Subtract(CurrentTime).ToString().Split('.');
					Singleton<GameManager>.Instance.KickPlayer(session.SteamId.ToString(), Msg("tempbanmsg",session.SteamId.ToString().Replace("{Duration} minutes.", TidyTime[0])));
				}
        }

		[ChatCommand("kick")]
		void KickCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.kick") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length < 2)
				{
					hurt.SendChatMessage(session, Msg("noreason",session.SteamId.ToString()));
					return;
				}
				PlayerSession person = GetSession(args[0]);
				if(person == null)
				{
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()));
					return;
				}
				else
				{
					string ID = person.SteamId.ToString();
					string reason = string.Join(" ", args, 1, args.Length-1);
					hurt.BroadcastChat((Msg("kicked",session.SteamId.ToString())).Replace("{Player}", person.Name).Replace("{Reason}", reason));
					Singleton<GameManager>.Instance.KickPlayer(ID,  reason);
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " kicked " + person.Name);
					return;
				}
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission"));
		}

		[ChatCommand("ban")]
		void BanCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.ban") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length != 1)
				{
					hurt.SendChatMessage(session, Msg("banfail",session.SteamId.ToString()));
					return;
				}
				if (args[0].Length == 17)
				{
					string ID = args[0];
					hurt.BroadcastChat((Msg("banned",session.SteamId.ToString())).Replace("{Player}", "SteamID: "+args[0]));
					ConsoleManager.Instance?.ExecuteCommand("ban " + ID);
					Singleton<GameManager>.Instance.KickPlayer(ID, Msg("banmsg",session.SteamId.ToString()));
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " banned SteamID: " + ID);
					return;
				}
				PlayerSession person = GetSession(args[0]);
				if(person == null)
				{
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()));
					return;
				}
				else
				{
					string ID = person.SteamId.ToString();
					hurt.BroadcastChat((Msg("banned",session.SteamId.ToString())).Replace("{Player}", person.Name));
					ConsoleManager.Instance?.ExecuteCommand("ban " + ID);
					Singleton<GameManager>.Instance.KickPlayer(ID, Msg("banmsg",session.SteamId.ToString()));
					
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " banned " + person.Name);
					return;
				}
			}
			else
			hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}

		[ChatCommand("tempban")]
		void TempBanCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length != 2)
				{
					hurt.SendChatMessage(session, Msg("tempbanfail",session.SteamId.ToString()));
					return;
				}
				var PlayerBanned = CheckBan(args[0]) as TBA;
				if(PlayerBanned != null)
				{
					TempBans.Remove(PlayerBanned);
				}
				System.DateTime CurrentTime = System.DateTime.UtcNow;
				PlayerSession person = GetSession(args[0]);
				if(person == null)
				{
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()));
					return;
				}
				double Duration;
				try {
					Duration = Convert.ToDouble(args[1]);
				} catch {
					hurt.SendChatMessage(session, Msg("tempbanfail",session.SteamId.ToString()));
					return;
				}
				hurt.BroadcastChat(Msg("tempbanned",session.SteamId.ToString()).Replace("{Player}", person.Name).Replace("{Duration}",Duration.ToString()));
				TempBans.Add(new TBA(person.SteamId.ToString(), person.Name, person.Player.ipAddress, CurrentTime.AddMinutes(Duration)));
				SaveTempBans();
				Singleton<GameManager>.Instance.KickPlayer(person.SteamId.ToString(), Msg("tempbanmsg",session.SteamId.ToString()).Replace("{Duration}",Duration.ToString()));
				if ((bool) Config["ShowConsoleMsg"])
					Puts(session.Name + " TempBanned "+person.Name+" for "+Duration.ToString());
				return;
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("removetempban")]
		void RemoveTempBanCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length == 1)
				{
					System.DateTime CurrentTime = System.DateTime.UtcNow;
					var PlayerBanned = CheckBan(args[0]) as TBA;
					if(PlayerBanned != null)
					{
						hurt.SendChatMessage(session, Msg("isnolongertempbanned",session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
						TempBans.Remove(PlayerBanned);
						if ((bool) Config["ShowConsoleMsg"])
							Puts(session.Name+" removed the TempBan on "+PlayerBanned.Name);
						return;
					}
					else
						hurt.SendChatMessage(session, Msg("isnttempbanned",session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
				}
				else
					hurt.SendChatMessage(session, Msg("tempremovefail",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("checktempban")]
		void CheckTempBanCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.tempban") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length == 1)
				{
					System.DateTime CurrentTime = System.DateTime.UtcNow;
					var PlayerBanned = CheckBan(args[0]) as TBA;
					if(PlayerBanned != null)
					{
						string[] TidyTime = PlayerBanned.TillExpire.Subtract(CurrentTime).ToString().Split('.');
						if(PlayerBanned.TillExpire < CurrentTime)
						{
							hurt.SendChatMessage(session, Msg("isnolongertempbanned",session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
							TempBans.Remove(PlayerBanned);
							SaveTempBans();
						}
						else
							hurt.SendChatMessage(session, Msg("istempbanned",session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name).Replace("{Duration}",TidyTime[0]));
					}
					else
						hurt.SendChatMessage(session, Msg("isnttempbanned",session.SteamId.ToString()).Replace("{Name}", PlayerBanned.Name));
				}
				else
					hurt.SendChatMessage(session, Msg("tempcheckfail",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("unban")]
		void UnbanCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.ban") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length != 1)
				{
					hurt.SendChatMessage(session, Msg("unbanfail",session.SteamId.ToString()));
					return;
				}
				if (args[0].Length == 17)
				{
					string ID = args[0];
					ConsoleManager.Instance?.ExecuteCommand("unban " + ID);
					hurt.SendChatMessage(session, Msg("unbanned",session.SteamId.ToString()).Replace("{ID}",ID));
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " unbanned: " + ID);
					return;
				}
				else
					hurt.SendChatMessage(session, Msg("unbanfail",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("mute")]
		void MuteCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.mute") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				string reason = "";
				if(args.Length < 2)
				{
					hurt.SendChatMessage(session, Msg("mutefail",session.SteamId.ToString()));
					return;
				}
				PlayerSession person = GetSession(args[0]);
				float Duration = float.Parse(args[1]);
				if (args.Length >= 3)
				{
					reason = " Reason: " + string.Join(" ", args, 2, args.Length-2);
				}
					
				if(person == null)
				{
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()));
					return;
				}
				else
				{
					var ChatMgr = Singleton<ChatManagerServer>.Instance;
					hurt.BroadcastChat((Msg("muted",session.SteamId.ToString())).Replace("{Player}", person.Name).Replace("{time}", args[1]).Replace("{reason}", reason));
					hurt.SendChatMessage(person, Msg("mutewarning",session.SteamId.ToString()));
					ChatMgr.Mute((ulong)person.SteamId, Duration);
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " muted " + person.Name);
					return;
				}
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("unmute")]
		void UnmuteCommand(PlayerSession session, string command, string[] args)
		{
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.mute") || permission.UserHasPermission(session.SteamId.ToString(),"admintools.all"))
			{
				if(args.Length != 1)
				{
					hurt.SendChatMessage(session, Msg("mutefail",session.SteamId.ToString()));
					return;
				}
				PlayerSession person = GetSession(args[0]);
				if(person == null)
				{
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()).Replace("{Player}",args[0]));
					return;
				}
				else
				{
					var ChatMgr = Singleton<ChatManagerServer>.Instance;
					ulong ID = (ulong)person.SteamId;
					hurt.BroadcastChat((Msg("unmuted",session.SteamId.ToString())).Replace("{Player}", person.Name));
					ChatMgr.Unmute(ID);
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name + " unmuted " + person.Name);
					return;
				}
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("freeze")]
		void FreezeCommand(PlayerSession session, string command, string[] args)
		{
			if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.freeze") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all"))
			{
				PlayerSession target = GetSession(args[0]);
				if(target != null)
				{
					CharacterMotorSimple motor = target.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
					motor.CanMove = false;
					hurt.SendChatMessage(session, Msg("frozenmsg",session.SteamId.ToString()).Replace("{Player}",target.Name));
					hurt.SendChatMessage(target, Msg("frozen",session.SteamId.ToString()));
					return;
				}
				else
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()).Replace("{Player}",args[0]));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		[ChatCommand("unfreeze")]
		void UnFreezeCommand(PlayerSession session, string command, string[] args)
		{
			if (permission.UserHasPermission(session.SteamId.ToString(), "admintools.freeze") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all"))
			{
				PlayerSession target = GetSession(args[0]);
				if(target != null)
				{
					CharacterMotorSimple motor = target.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
					motor.CanMove = true;
					hurt.SendChatMessage(session, Msg("unfrozenmsg",session.SteamId.ToString()).Replace("{Player}",target.Name));
					hurt.SendChatMessage(target, Msg("unfrozen",session.SteamId.ToString()));
					return;
				}
				else
					hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()).Replace("{Player}",args[0]));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}

		[ChatCommand("godmode")]
        void GodmodeCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all"))
			{
				if(args.Length != 1) 
				{
					hurt.SendChatMessage(session, Msg("godfail",session.SteamId.ToString()));
					return;
				}
				if (args[0] == "on") {
					if(Godlist.Contains((ulong)session.SteamId))
					{
						hurt.SendChatMessage(session, Msg("alreadygod",session.SteamId.ToString()));
						return;
					}
					Heal(session);
					AlertManager.Instance.GenericTextNotificationServer("Godmode Enabled",session.Player);
					Godlist.Add((ulong)session.SteamId);
					SaveGodlist();
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name+" turned GODMODE on");
					return;
				}
				else if (args[0] == "off")
				{
					if(!Godlist.Contains((ulong)session.SteamId))
					{
						hurt.SendChatMessage(session, Msg("notgod",session.SteamId.ToString()));
						return;
					}
					Heal(session);
					AlertManager.Instance.GenericTextNotificationServer("Godmode Disabled",session.Player);
					Godlist.Remove((ulong)session.SteamId);
					SaveGodlist();
					if ((bool) Config["ShowConsoleMsg"])
						Puts(session.Name+" turned GODMODE off");
					return;
				}
				else
					hurt.SendChatMessage(session, Msg("godfail",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		void Heal(PlayerSession player)
		{
			EntityStats stats = player.WorldPlayerEntity.GetComponent<EntityStats>();
			stats.GetFluidEffect(EEntityFluidEffectType.ColdBar).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.Radiation).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.HeatBar).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.Dampness).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.Hungerbar).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.Nutrition).SetValue(100f);	
			stats.GetFluidEffect(EEntityFluidEffectType.BodyTemperature).Reset(true);
			stats.GetFluidEffect(EEntityFluidEffectType.Toxin).SetValue(0f);
			stats.GetFluidEffect(EEntityFluidEffectType.Health).SetValue(100f);
		}
		
		[ChatCommand("heal")]
        void HealCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"admintools.godmode") || permission.UserHasPermission(session.SteamId.ToString(), "admintools.all"))
			{
				if(args.Length > 1)
				{
					hurt.SendChatMessage(session, Msg("healfail",session.SteamId.ToString()));
					hurt.SendChatMessage(session, args.Length.ToString());
					return;
				}
				if(args.Length == 0)
				{
					Heal(session);
					hurt.SendChatMessage(session, "AdminTools: Healed");
				}
				if(args.Length == 1)
				{
					PlayerSession target = GetSession(args[0]);
					if(target != null)
					{
						Heal(target);
						hurt.SendChatMessage(session, "AdminTools: Healed "+target.Name);
					}
					else
						hurt.SendChatMessage(session, Msg("playernotfound",session.SteamId.ToString()));
				}
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		
		object CheckBan(string checkstring)
		{
			foreach (TBA TempBannedPlayer in TempBans)
			{
				if (TempBannedPlayer.Name == checkstring || TempBannedPlayer.IP == checkstring || TempBannedPlayer.ID == checkstring)
				{
					return TempBannedPlayer;
				}			
			}
			return null;
		}
		
		object CheckBanJoin(string IP, string Name, string ID)
		{
			foreach (TBA TempBannedPlayer in TempBans)
			{
				if (TempBannedPlayer.Name == Name || TempBannedPlayer.IP == IP || TempBannedPlayer.ID == ID)
				{
					return TempBannedPlayer;
				}			
			}
			return null;
		}
	}
}