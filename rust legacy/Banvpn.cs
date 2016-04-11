// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("vpnban", "copper", "2.4.0")]
    class Banvpn : RustLegacyPlugin
    {
		JsonSerializerSettings jsonsettings;
        private Core.Configuration.DynamicConfigFile Data;
		private Core.Configuration.DynamicConfigFile Info;
		private Core.Configuration.DynamicConfigFile Wl;
        void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(vpn)"); Info = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(vpn.pl)"); 	Wl = Interface.GetMod().DataFileSystem.GetDatafile("whitelist(vpn)");}
        void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("Blacklist(vpn)"); Interface.GetMod().DataFileSystem.SaveDatafile("Blacklist(vpn.pl)"); Interface.GetMod().DataFileSystem.SaveDatafile("whitelist(vpn)"); }
        void Unload() { SaveData(); }

        void Loaded()
        {			
            LoadData();
			if (!permission.PermissionExists("canvpn")) permission.RegisterPermission("canvpn", this);
			if (!permission.PermissionExists("canvpn.isp")) permission.RegisterPermission("canvpn.isp", this);
			if(shoudblockisp)
			{
				var blacklistisp = GetPlayerdata("Blacklist(vpn.isp)");
				if(!blacklistisp.ContainsKey(turkytelecom))
				{
					blacklistisp.Add(turkytelecom, "Reason : virtual nest (vpn.isp)");
					SaveData();
				}
				if(!blacklistisp.ContainsKey(virtualnest))
				{
					blacklistisp.Add(virtualnest, "Reason : virtual nest (vpn.isp)");
					SaveData();
				}
			}
        }

        public static string notindatabase = "there is no players found in our database with that name";
		public static string virtualnest = "AS8551 Bezeq International-Ltd";
        public static string banlist = "banlist:";
        public static string couldntFindPlayer = "Couldn't find the target player.";
        public static string removedban = "{0} was removed from the  banlist.";
        public static string addedban = "{0} was added to the banlist.";
        public static string notFriendWith = "the player you requested is not found in our banlist: {0}";
        public static string alreadybanned = "the player is already banned: {0}";
		public static string teamspeak = "ts.derpteamgames.com";
		public static string isbannedmsg = "you are blacklisted  as a result of (virtual nest) plz contact an admin on our teamspeak at";
		public static string colorisbanned = "[color red]";
		public static string colorhelp = "[color cyan]";
		public static string colorteamspeak = "[color cyan]";
		public static string colorwebsite = "[color cyan]";
		public static string website = "or make a ban apeal on our website at http://7dtd.enjin.com/";
		public static string colorisbannedmsg = "[color cyan]";
		public static string isbanned = "YOU ARE BLACKLISTED";
		public static string systemname = "DTG(derpteamgames)";
		public static string shouldkick = "1";
		public static bool shouldbanid = true;
		public static bool shouldbanidifbanned = false;
		public static bool shouldsavedataonserversave = false;
		public static string addtowl = "{0} have been added to the whitelist";
		public static string alreadywl = "the player has already been added to the whitelist";
		public static string colorisbannedd = "[color cyan]";
		public static string isbannedd = "is trying to connect but is banned";
		public static string removedwhitelist = "{0} was removed from the whitelist";
		public static string turkytelecom = "AS9121 Turk Telekomunikasyon Anonim Sirketi";
		public static bool shoudblockisp = true;

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
			CheckCfg<string>("Messages: should kick after ban", ref shouldkick);
			CheckCfg<string>("Messages: virtualnest(vpn.isp)", ref virtualnest);
			CheckCfg<string>("Messages: blacklist telecom(vpn.isp)", ref turkytelecom);
            CheckCfg<string>("Messages: no players found in database", ref notindatabase);
			CheckCfg<string>("Messages: chat msg", ref isbannedd);
			CheckCfg<string>("Messages: chat msg color", ref colorisbannedd);
            CheckCfg<string>("Messages: banlist", ref banlist);
            CheckCfg<string>("Messages: No Players Found", ref couldntFindPlayer);
            CheckCfg<string>("Messages: Removed from banlist", ref removedban);
            CheckCfg<string>("Messages: Added to banlst", ref addedban);
            CheckCfg<string>("Messages: u have already banned", ref alreadybanned);
            CheckCfg<string>("Messages: is not found in our banlist", ref notFriendWith);
			CheckCfg<string>("Messages: Teamspeak", ref teamspeak);
			CheckCfg<string>("Messages: isbannedmsg", ref isbannedmsg);
			CheckCfg<string>("Messages: color is blacklisted", ref colorisbanned);
			CheckCfg<string>("Messages: color help", ref colorhelp);
			CheckCfg<string>("Messages: color teamspeak", ref colorteamspeak);
			CheckCfg<string>("Messages: website", ref website);
			CheckCfg<string>("Messages: colorwebsite", ref colorwebsite);
			CheckCfg<string>("Messages: color is banned msg", ref colorisbannedmsg);
			CheckCfg<string>("Messages: is banned msg bold", ref isbanned);
			CheckCfg<string>("Messages: bansystemname", ref systemname);
			CheckCfg<bool>("Messages: shouldbanid", ref shouldbanid);
			CheckCfg<bool>("Messages: should banid if player connect and is using vpn", ref shouldbanidifbanned);
			CheckCfg<bool>("Messages: shouldblock isp(vpn.isp telecom)", ref shoudblockisp);
			CheckCfg<bool>("Messages: should save data on server save(not needed)", ref shouldsavedataonserversave);
			CheckCfg<string>("Messages: Remove from whitelist", ref removedwhitelist);
			CheckCfg<string>("Messages: added to whitelist", ref addtowl);
			CheckCfg<string>("Messages: whitelisted players", ref alreadywl);
			SaveConfig();
        }
		
		bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
            if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canvpn")) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canvpn.isp")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }

		Dictionary<string, object> GetPlayer(string userid)
        {
            if (Info[userid] == null)
                Info[userid] = new Dictionary<string, object>();
            return Info[userid] as Dictionary<string, object>;
        }

		Dictionary<string, object> GetwlPlayer(string userid)
        {
            if (Wl[userid] == null)
                Wl[userid] = new Dictionary<string, object>();
            return Wl[userid] as Dictionary<string, object>;
        }

        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }
		[ChatCommand("blisp")]
		void cmdChatblacklistisp(NetUser netuser, string command, string[] args)
		{
			if(!hasAccess(netuser, "canvpn.isp")) { SendReply(netuser, "you are not allowed to use this command"); return; }
			{
				if (args.Length != 1)
				{
					rust.SendChatMessage(netuser, systemname, "wrong syntax /blisp playername");
					return;
				}
			}
			NetUser targetuser = rust.FindPlayer(args[0]);
			if(targetuser == null)
			{
				rust.SendChatMessage(netuser, systemname, "there was no player found with that name: " + args[0]);
				return;
			}
			if (targetuser == netuser)
			{
				rust.SendChatMessage(netuser, systemname, "You can't ban your self, your server still needs you");
				return;
			}
			var targetip = targetuser.networkPlayer.externalIP;
			var targetname = targetuser.displayName;
			var targetid = targetuser.playerClient.userID.ToString();
			var staffid = netuser.playerClient.userID.ToString();
			var blacklistisp = GetPlayerdata("Blacklist(vpn.isp)");
			var url = string.Format("http://iphub.info/api.php?ip=" + targetuser.networkPlayer.externalIP + "&showtype=4");
			Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (code, response) =>
			{
				var jsonresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
				var targetinfo = (jsonresponse["asn"].ToString());
				if(blacklistisp.ContainsKey(targetinfo))
				{
					rust.SendChatMessage(netuser, systemname, "this player's isp is already blacklisted");
					return;
				}
				blacklistisp.Add(targetinfo, targetname + " by: " + netuser.displayName + " staffid: " + staffid);
				rust.SendChatMessage(netuser, systemname, targetname + "(" + targetinfo + ")" + " has been added to the banlist");
				
			}
			, this);	
		}
		
		[ChatCommand("vuwl")]
		void cmdChatunWhitelstvpn(NetUser netuser, string command, string[] args)
		{
			if(!hasAccess(netuser, "canvpn")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
			var Whitelist = GetwlPlayer("Whitelist(vpn)");
			NetUser targetuser = rust.FindPlayer(args[0]);
			var targetid = targetuser.playerClient.userID.ToString();
			if (!Whitelist.ContainsKey(targetid))
			{
				rust.SendChatMessage(netuser, systemname, string.Format(couldntFindPlayer, args[0].ToString()));
				return;
			}
			rust.SendChatMessage(netuser, systemname, string.Format(removedwhitelist, args[0].ToString()));
			Whitelist.Remove(targetid);
			return;
		}

		[ChatCommand("vwl")]
        void cmdChatWhitelstvpn(NetUser netuser, string command, string[] args)
        {
			var Whitelist = GetwlPlayer("Whitelist(vpn)");
			var playerdata = GetPlayer(args[0]);
			if (!hasAccess(netuser, "canvpn")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
            if (playerdata.ContainsKey("name"))
            {
				var gg = playerdata["id"].ToString();
				var q = playerdata["name"].ToString();
				if (Whitelist.ContainsKey(gg))
				{
					rust.SendChatMessage(netuser, systemname, alreadywl);
					return;
				}
				Whitelist.Add(gg, q);
				rust.SendChatMessage(netuser, systemname, string.Format(addtowl, q.ToString()));
                return;
            }
            rust.SendChatMessage(netuser, systemname, string.Format(couldntFindPlayer, args[0].ToString()));
		}
		void OnServerSave()
		{
			if(shouldsavedataonserversave)
			{
				SaveData();
			}
		}
		void Broadcast(string message)
        {
            ConsoleNetworker.Broadcast("chat.add " + systemname + " "+ Facepunch.Utility.String.QuoteSafe(message));
        }
		void OnPlayerConnected(NetUser netuser)
		{
			var url = string.Format("http://iphub.info/api.php?ip=" + netuser.networkPlayer.externalIP + "&showtype=4");
			Interface.GetMod().GetLibrary<WebRequests>("WebRequests").EnqueueGet(url, (code, response) =>
			{
				var targetuserid = netuser.playerClient.userID.ToString();
				var Whitelist = GetwlPlayer("Whitelist(vpn)");
				if(Whitelist.ContainsKey(targetuserid)) return;
				var jsonresponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
				var playervpn = (jsonresponse["proxy"].ToString());
				var targetip = netuser.networkPlayer.externalIP;
				var targetname = netuser.displayName;
				var playerdata = GetPlayerdata("Blacklist(vpn)");
				var targetid = playervpn;
				if(shoudblockisp)
				{
					var blacklistisp = GetPlayerdata("Blacklist(vpn.isp)");
					var playerispvpn = (jsonresponse["asn"].ToString());
					if(blacklistisp.ContainsKey(playerispvpn))
					{
						rust.SendChatMessage(netuser, systemname,  colorisbanned + isbanned);
					    rust.SendChatMessage(netuser, systemname,  colorisbannedmsg + isbannedmsg + " " + colorteamspeak + teamspeak);
					    rust.SendChatMessage(netuser, systemname,  colorwebsite + " " + website);
						netuser.Kick(NetError.Facepunch_Kick_RCON, true);
						return;
					}
					
					
				}
				if(playervpn == shouldkick)
				{
					var players = GetPlayer(targetname);
					rust.SendChatMessage(netuser, systemname,  colorisbanned + isbanned);
					rust.SendChatMessage(netuser, systemname,  colorisbannedmsg + isbannedmsg + " " + colorteamspeak + teamspeak);
					rust.SendChatMessage(netuser, systemname,  colorwebsite + " " + website);
					if(!players.ContainsKey("name"))
					{
						players.Add("id", targetuserid);
						players.Add("name", targetname);
						players.Add("ip", targetip);
					}
				    if (shouldbanidifbanned)
				    {
				        Interface.CallHook("cmdBan", targetuserid.ToString(), targetname);
				    }
				netuser.Kick(NetError.Facepunch_Kick_RCON, true);
				Broadcast(string.Format(targetname + " " + isbannedd));
				}
				
			}, this);
		}
    }
}