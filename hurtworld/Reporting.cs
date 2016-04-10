//Reference: UnityEngine.UI
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Reporting", "Noviets", "1.0.5")]
    [Description("Submit reports and chat logging")]
    class Reporting : HurtworldPlugin
    {
		void Loaded()
		{
			unreadreports = Interface.GetMod().DataFileSystem.ReadObject<List<string>>("Reporting/UnreadReports");
			try{unreadbasereport = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<Vector3, string>>("Reporting/UnreadBaseReport");}catch{}
			reports = Interface.GetMod().DataFileSystem.ReadObject<List<string>>("Reporting/Reports");
			chatlog = Interface.GetMod().DataFileSystem.ReadObject<List<string>>("Reporting/ChatLog");
			permission.RegisterPermission("reporting.admin", this);
			LoadDefaultConfig();
			LoadMessages();
		}
		protected override void LoadDefaultConfig()
        {
			if(Config["LogKeywords"] == null) Config.Set("LogKeywords", false);
			if(Config["LogAllChat"] == null) Config.Set("LogAllChat", false);
			if(Config["Keywords"] == null) Config.Set("Keywords", new string[] {"hack", "admin", "exploit", "glitch", "aimbot"});
            SaveConfig();
        }
		void LoadMessages()
        {
            var msgs = new Dictionary<string, string>
            {
				{"NoUnread", "<color=orange>There are no unread reports.</color>"},
				{"Reported", "<color=yellow>You have reported {offender} for: {for}</color>"},
				{"BaseReported", "<color=yellow>You have reported the base at: {Location}</color>"},
				{"Error", "Wrong usage! Usage: /report Player Offense here"},
				{"BaseError", "Wrong usage! Usage: /basereport"},
				{"NoPermission", "You do not have permission."},
				{"BaseTP", "<color=orange>You have been Teleported to: {Location} reported by: {Player}</color>"},
				{"NoPlayer", "<color=orange>That player doesnt exist or is not online.</color>"},
				{"UnreadWelcome", "<color=orange>There are [<color=yellow>{Count}</color>] unread reports. Use /unread</color>"},
				{"BaseUnreadWelcome", "<color=orange>There are [<color=yellow>{Count}</color>] unread base reports. Use /unread</color>"}
			};
			lang.RegisterMessages(msgs, this);
        }

		List<string> unreadreports = new List<string>();
		Dictionary<Vector3, string> unreadbasereport = new Dictionary<Vector3, string>();
		List<string> reports = new List<string>();
		List<string> chatlog = new List<string>();
		string message(string message, string SteamId = null) => lang.GetMessage(message, this, SteamId);

        void SaveAll()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Reporting/UnreadReports", unreadreports);
			Interface.GetMod().DataFileSystem.WriteObject("Reporting/UnreadBaseReport", unreadbasereport);
			Interface.GetMod().DataFileSystem.WriteObject("Reporting/Reports", reports);
			Interface.GetMod().DataFileSystem.WriteObject("Reporting/ChatLog", chatlog);
        }
		
		void SaveUnread()
        {
            Interface.GetMod().DataFileSystem.WriteObject("Reporting/UnreadReports", unreadreports);
			Interface.GetMod().DataFileSystem.WriteObject("Reporting/UnreadBaseReport", unreadbasereport);
        }
		
		void SaveChatLog()
        {
			Interface.GetMod().DataFileSystem.WriteObject("Reporting/ChatLog", chatlog);
        }

		void AddReport(PlayerSession player, PlayerSession offender, string report)
        {

			reports.Add("["+System.DateTime.Now+"] "+player.Name+" ("+player.SteamId.ToString()+") reported "+offender.Name+" ("+offender.SteamId.ToString()+") for: "+report);
			unreadreports.Add("["+System.DateTime.Now+"] "+player.Name+" ("+player.SteamId.ToString()+") reported "+offender.Name+" ("+offender.SteamId.ToString()+") for: "+report);
            SaveAll();
			Puts(player.Name+" reported "+offender.Name+" for: " +report);
        }
		
		void AddBaseReport(Vector3 loc, PlayerSession player)
        {

			reports.Add("["+System.DateTime.Now+"] "+player.Name+" ("+player.SteamId.ToString()+") reported a Base at: "+loc.ToString());
			unreadbasereport.Add(loc, player.Name);
            SaveAll();
        }
		
		void LogTheChat(PlayerSession player, string message)
        {

			chatlog.Add("["+System.DateTime.Now+"] "+player.Name +" : "+ message);
            SaveChatLog();
        }
		
		[ChatCommand("report")]
        void reportCommand(PlayerSession session, string command, string[] args)
        {
			if(args.Length <2)
			{
				hurt.SendChatMessage(session, message("Error", session.SteamId.ToString()));
				return;
			}
			PlayerSession offender = GetSession(args[0]);
			if(offender == null)
			{
				hurt.SendChatMessage(session, message("NoPlayer", session.SteamId.ToString()));
				return;
			}
			else
			{
				string reportedfor = string.Join(" ", args, 1, args.Length-1);
				AddReport(session, offender, reportedfor);
				hurt.SendChatMessage(session, message("Reported", session.SteamId.ToString()).Replace("{offender}",offender.Name).Replace("{for}",reportedfor));
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> p in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					if (permission.UserHasPermission(p.Value.SteamId.ToString(), "reporting.admin"))
					{
						hurt.SendChatMessage(p.Value, "<color=orange>"+session.Name+" reported "+offender.Name+" for "+reportedfor+"</color>");
					}
				}
				return;
			}
		}
		
		[ChatCommand("reportbase")]
        void baseCommand(PlayerSession session, string command, string[] args)
        {
			if(args.Length >= 1)
			{
				hurt.SendChatMessage(session, message("BaseError", session.SteamId.ToString()));
				return;
			}
			Vector3 loc = session.WorldPlayerEntity.transform.position;
			AddBaseReport(loc, session);
			hurt.SendChatMessage(session, message("BaseReported", session.SteamId.ToString()).Replace("{Location}",loc.ToString()));
			return;
		}
		
		[ChatCommand("unread")]
        void viewreportCommand(PlayerSession session)
        {
			if(!permission.UserHasPermission(session.SteamId.ToString(), "reporting.admin"))
			{
				hurt.SendChatMessage(session, message("NoPermission", session.SteamId.ToString()));
				return;
			}
			else
			{
				if(unreadreports.Count > 0)
				{
					hurt.SendChatMessage(session, "<color=orange>"+unreadreports[0]+"</color>");
					unreadreports.Remove(unreadreports[0]);
					SaveUnread();
					return;
				}
				else if(unreadbasereport.Count > 0) 
				{ 
					var rbr = unreadbasereport.First();
					session.WorldPlayerEntity.transform.position = rbr.Key;
					hurt.SendChatMessage(session, message("BaseTP", session.SteamId.ToString()).Replace("{Location}",rbr.Key.ToString()).Replace("{Player}",rbr.Value));
					unreadbasereport.Remove(rbr.Key);
					SaveUnread();
					return;
				}
				else
				{
					hurt.SendChatMessage(session, message("NoUnread", session.SteamId.ToString()));
					return;
				}
			}
		}
		
		void OnPlayerChat(PlayerSession session, string message)
		{
			if((bool)Config["LogKeywords"])
			{
				var words = Config.Get<List<string>>("Keywords");
				foreach(string x in words){
					if(message.ToLower().Contains(x.ToLower())){
						LogTheChat(session, message);
						break;
					}
				}
			}
			if((bool)Config["LogAllChat"])
			{
				LogTheChat(session, message);
			}
		}
		
		private PlayerSession GetSession(string source) 
		{
			var IPCheck = Regex.Match(source, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
			foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> p in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
			{
				PlayerSession r = p.Value;
				if (source.ToLower() == r.Name.ToLower())
				{
					if(r.IsLoaded) return r;
				}
			}
			foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
			{
				PlayerSession player = pair.Value;
				if (IPCheck.Success)
				{
					if (source == player.Player.ipAddress)
					{
						if	(player.IsLoaded) return player;
					}
				}
				else if(source == player.SteamId.ToString())
				{
					if (player.IsLoaded) return player;
				}
				else if (player.Name.ToLower().Contains(source.ToLower()))
				{
					if(player.IsLoaded) return player;
				}
			}
			return null;
		}
		void OnPlayerInit(PlayerSession session)
        {
			if (permission.UserHasPermission(session.SteamId.ToString(), "reporting.admin"))
			{
				if(unreadreports.Count > 0)
				{
					timer.Once(10f, ()  => 
					{
						hurt.SendChatMessage(session, message("UnreadWelcome", session.SteamId.ToString()).Replace("{Count}",unreadreports.Count.ToString()));
					});
				}
				if(unreadbasereport.Count > 0)
				{
					timer.Once(10f, ()  => 
					{
						hurt.SendChatMessage(session, message("BaseUnreadWelcome", session.SteamId.ToString()).Replace("{Count}",unreadbasereport.Count.ToString()));
					});
				}
			}
        }
	}
}