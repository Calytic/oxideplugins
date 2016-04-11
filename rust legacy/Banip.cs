using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("banip", "copper", "3.9")]
    class Banip : RustLegacyPlugin
    {
		NetUser cachedUser;
        string cachedSteamid;
        string cachedReason;
        string cachedName;
		
	    public Type BanType;
        public FieldInfo steamid;
        public FieldInfo username;
        public FieldInfo reason;
        public FieldInfo bannedUsers;


        private Core.Configuration.DynamicConfigFile Data;
		private Core.Configuration.DynamicConfigFile Info;
		private Core.Configuration.DynamicConfigFile Wl;
        void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(ip)"); Info = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(ip.pl)"); Wl = Interface.GetMod().DataFileSystem.GetDatafile("Blacklist(ip.wl)"); }
        void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("Blacklist(ip)"); Interface.GetMod().DataFileSystem.SaveDatafile("Blacklist(ip.pl)"); Interface.GetMod().DataFileSystem.SaveDatafile("Blacklist(ip.wl)"); }
        void Unload() { SaveData(); }
		
		void OnServerSave()
		{
			if(shouldsaveonserversave)
			{
				SaveData();
			}
		}

        void Loaded()
        {			
            LoadData();
			if (!permission.PermissionExists("canunbanip")) permission.RegisterPermission("canunbanip", this);
			if (!permission.PermissionExists("canbanslpip")) permission.RegisterPermission("canbanslpip", this);
			if (!permission.PermissionExists("canbanip")) permission.RegisterPermission("canbanip", this);
        }


        public static string notinlist = "there is no players found in our banlist";
		public static string banMessage = "has been banned from the server";
		public static string cachedreason = "Banip(ip in banlist : ";
		public static string systemname = "Derpteam";
        public static string banlist = "banlist:";
        public static string couldntFindPlayer = "Couldn't find the target player.";
        public static string removedban = "was removed from the  banlist.";
        public static string addedban = "was added to the banlist";
		public static string addedwl = "was added to the whitelist";
		public static string tryconnectMessage = "tried to connect but is banned";
        public static string notinbanlist = "the player you requested is not found in our database: ";
		public static string isrconadminmsg = "the player you attempted to ban is an admin or has the immunity flag ";
		public static string banselfmsg = "you attempted to ban your self but your server still needs you !!! ";
        public static string alreadybanned = "is already banned";
		public static bool shouldbanid = false;
		public static bool shouldpreventbanonself = true;
		public static bool shouldkick = false;
		public static bool shouldsaveonserversave = false;
		public static bool shouldbanidifbanned = true;
		public static bool shouldcrashclientafterban = true;
		public static bool shouldcrashclient = true;
		public static bool shouldallowwhitelist = false;
		public static bool shouldbroadcast = false;
		public static bool shouldbroadcasttryconnect = false;
		public static bool shouldcapframes = false;
		public static bool shouldlogall = false;
		public static bool shouldcapframesafterban = false;
		public static bool shouldrebindknownhackkey = false;
		public static bool shouldpreventbanonrconadmin = true;
		public static bool shouldpreventmultipleidentityunbancollision = true;
		static int capedamount = 1;

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
			
			CheckCfg<bool>("Messages: should cap fps of player after ban", ref shouldcapframesafterban);
			CheckCfg<bool>("Messages: should prevent multiple unbans", ref shouldpreventmultipleidentityunbancollision);
			CheckCfg<bool>("Messages: should prevent ban on rconadmin/ipbanimmuneflaged players)", ref shouldpreventbanonrconadmin);
			CheckCfg<bool>("Messages: should prevent ban on self", ref shouldpreventbanonself);
			CheckCfg<bool>("Messages: should log all player ip's", ref shouldlogall);
			CheckCfg<bool>("Messages: should cap fps of banned player", ref shouldcapframes);
			CheckCfg<bool>("Messages: should crash client after ban", ref shouldcrashclientafterban);
			CheckCfg<bool>("Messages: should rebing known aimbot keys to gamekeys(will not stop aimbot but might help prevent)", ref shouldrebindknownhackkey);
			CheckCfg<int>("Messages: caped banned client fps amount", ref capedamount);
			CheckCfg<bool>("Messages: should log banned players and allow whitelist", ref shouldallowwhitelist);
            CheckCfg<string>("Messages: not in lists", ref notinlist);
			CheckCfg<string>("Messages: attempted to ban admin msg", ref isrconadminmsg);
			CheckCfg<string>("Messages: attempted to ban self msg", ref banselfmsg);
			CheckCfg<string>("Messages: try connectmessage", ref tryconnectMessage);
			CheckCfg<string>("Messages: systemname", ref systemname);
			CheckCfg<string>("Messages: banmesssage chat", ref banMessage);
            CheckCfg<string>("Messages: reason for autoban", ref cachedreason);
			CheckCfg<string>("Messages: systemname", ref systemname);
            CheckCfg<string>("Messages: No Player Found", ref couldntFindPlayer);
            CheckCfg<string>("Messages: Removed from banlst", ref removedban);
            CheckCfg<string>("Messages: Added to banlst", ref addedban);
            CheckCfg<string>("Messages: already banned", ref alreadybanned);
            CheckCfg<string>("Messages: is not found in our banlist", ref notinbanlist);
			CheckCfg<bool>("Messages: should kick after ban", ref shouldkick);
			CheckCfg<bool>("Messages: shouldbanid", ref shouldbanid);
			CheckCfg<bool>("Messages: should broadcast try to connect", ref shouldbroadcasttryconnect);
			CheckCfg<bool>("Messages: should crash client if player connects and is banned", ref shouldcrashclient);
			CheckCfg<bool>("Messages: should banid if player connect and is banned", ref shouldbanidifbanned);
			CheckCfg<string>("Messages: Added to banlst", ref addedwl);
			CheckCfg<bool>("Messages: should broadcast ban", ref shouldbroadcast);
			CheckCfg<bool>("Messages: should save data on server save", ref shouldsaveonserversave);
            SaveConfig();
        }
		bool hasAccessunbanip(NetUser netuser, string permissionname)
		{
			if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canunbanip")) return true;
			return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
		}
		
		bool hasAccessbanipslp(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canbanslpip")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
		bool hasimmunity(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "ipbanimunity")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
		bool hasAccess(NetUser netuser, string permissionname)
        {
            if (netuser.CanAdmin()) return true;
			if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "canbanip")) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), permissionname);
        }
		void SendHelpText(NetUser netuser)
        {
            if (hasAccess(netuser, "canbanip")) SendReply(netuser, "Banip : Type /baniphelp to get help about the banip plugin");
        }
		[ChatCommand("baniphelp")]
		void cmdChatBaniphelp(NetUser netuser, string command, string[] args)
		{
			if (hasAccess(netuser, "canbanip")) rust.SendChatMessage(netuser, systemname, "Banip : Type /banip playername to ban an ip");
			if (hasAccess(netuser, "canbanip")) rust.SendChatMessage(netuser, systemname, "Banip : Type /edp playername to find all players in the banlist with that name pages are optional");
			if (hasAccess(netuser, "canbanip")) rust.SendChatMessage(netuser, systemname, "Banip : A player can be added to the banipimmunity group by giving them the banipimmunity flag");
			if (hasAccess(netuser, "canbanip")) rust.SendChatMessage(netuser, systemname, "Banip : Type /ipbanlist to view ip banlist in pages");
			if (hasAccessunbanip(netuser, "canunbanip")) rust.SendChatMessage(netuser, systemname, "Banip Unban: /unbanip ip");
            if (hasAccessunbanip(netuser, "canunbanip")) rust.SendChatMessage(netuser, systemname, "Banip whitelistid: /ipwl 'PLAYERNAME < must be precise");
			if (hasAccessbanipslp(netuser, "canbanslpip")) rust.SendChatMessage(netuser, systemname, "Banip offline: /banslp 'PLAYERNAME < must be precise");
		}

		Dictionary<string, object> GetPlayerWl(string userid)
        {
            if (Wl[userid] == null)
                Wl[userid] = new Dictionary<string, object>();
            return Wl[userid] as Dictionary<string, object>;
        }
		Dictionary<string, object> GetPlayerinfo(string userid)
        {
            if (Info[userid] == null)
                Info[userid] = new Dictionary<string, object>();
            return Info[userid] as Dictionary<string, object>;
        }

        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }
		void dounbanip(NetUser netuser, string targetid, string targetname)
		{
			var playerdata = GetPlayerdata("Blacklist(ip)");
			if (playerdata.ContainsKey(targetid))
            {
                playerdata.Remove(targetid);
				rust.SendChatMessage(netuser, systemname, targetname + removedban);
                return;
            }
            rust.SendChatMessage(netuser, systemname, notinbanlist + " " +  targetname);
		}
        [ChatCommand("unbanip")]
        void cmdChatUnbanip(NetUser netuser, string command, string[] args)
        {
			if (!hasAccessunbanip(netuser, "canunbanip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
            var playerdata = GetPlayerdata("Blacklist(ip)");
			if (playerdata.Count == 0)
            {
				rust.SendChatMessage(netuser, systemname, notinlist);
                return;
            }
            if (args.Length == 0)
            {
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /unbanip 'playername or ip");
				return;
            }
            string targetname = string.Empty;
            string targetid = string.Empty;
			int count = 0;
			int count2 = 0;
			foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if (pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0] || pair.Value.ToString().ToLower() == args[0] || pair.Key.ToString().ToLower() == args[0] || pair.Value.ToString().ToLower().Contains(args[0]) || pair.Key.ToString().ToLower().Contains(args[0]))
                {
					count2++;	
                }
            }
			if(shouldpreventmultipleidentityunbancollision)
			if(count2 > 1)
			{
				rust.SendChatMessage(netuser, systemname, "Warning there is a total of " + count2.ToString() + " players found with that name plz be more specific");
                return;				
			}
			foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if (pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0] || pair.Value.ToString().ToLower() == args[0] || pair.Key.ToString().ToLower() == args[0] || pair.Value.ToString().ToLower().Contains(args[0]) || pair.Key.ToString().ToLower().Contains(args[0]))
                {
                    var targetname2 = pair.Value.ToString();
                    var targetid2 = pair.Key.ToString();
					count++;	
					if(count > 1)
					{
						rust.SendChatMessage(netuser, systemname, "Warning multiple players found with that name please be more specific");
						break;
						return;
						continue;
					}
					dounbanip(netuser, targetid2, targetname2);
					return;
                }
            }
			rust.SendChatMessage(netuser, systemname, "There was no players found with the name " + args[0].ToString());
        }
		[ChatCommand("ebp")]
		void cmdChatFindbannedipplayer(NetUser netuser, string command, string[] args)
		{
			var playerdata = GetPlayerdata("Blacklist(ip)");
			if (!hasAccess(netuser, "canbanip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return;}
			int bl2 = 1;
			int newbl = 0;
			if (args.Length > 1) int.TryParse(args[1], out bl2);
			var newcount = (bl2 + 20);
			if (args.Length > 1){
			rust.SendChatMessage(netuser, systemname, "Now searching for "  + args[0].ToString() + " in a total of " + playerdata.Count.ToString() + " players. starting from index " + bl2);
			foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if (pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0] || pair.Value.ToString().ToLower() == args[0] || pair.Key.ToString().ToLower() == args[0] || pair.Value.ToString().ToLower().Contains(args[0]) || pair.Key.ToString().ToLower().Contains(args[0]))
                {
                    var targetname = pair.Value.ToString();
                    var targetid = pair.Key.ToString();
					newbl++;
					if(newbl < bl2)
						continue;
					if(newbl >= newcount)
						break;
					if(newbl >= newcount)
						return;
					if(newbl >= newcount)
						continue;
					rust.SendChatMessage(netuser, systemname, newbl + ")" + " " + targetname + " " + targetid);
                }
            }
			return;
			}
			
			if (args.Length == 0)
			{
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /ebp 'playername < must be precise page(optional)");
				return;
			}
			int count = 0;
			if (playerdata.Count == 0)
            {
				rust.SendChatMessage(netuser, systemname, "there is no players found in the banlist");
                return;
            }
			rust.SendChatMessage(netuser, systemname, "Now searching for "  + args[0].ToString() + " in a total of " + playerdata.Count.ToString() + " players.");
			foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if (pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0] || pair.Value.ToString().ToLower() == args[0] || pair.Key.ToString().ToLower() == args[0] || pair.Value.ToString().ToLower().Contains(args[0]) || pair.Key.ToString().ToLower().Contains(args[0]))
                {
                    var targetname = pair.Value.ToString();
                    var targetid = pair.Key.ToString();
					count++;
					if(count >= 20)
						break;
					if(count >= 20)
						return;
					if(count > 20)
						continue;
					rust.SendChatMessage(netuser, systemname, count + ")" + " " + targetid + " " + targetname);
                }
            }
		}
		[ChatCommand("ipbanlist")]
        void cmdChatIpbanlist(NetUser netuser, string command, string[] args)
        {
			if (!hasAccess(netuser, "canbanip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
            var playerdata = GetPlayerdata("Blacklist(ip)");
			int bl = 1;
			int current = 1;
			if (args.Length > 0) int.TryParse(args[0], out bl);
			MethodInfo Enumerator = playerdata.GetType().GetMethod("GetEnumerator");
			var myEnum = Enumerator.Invoke(playerdata, new object[0]);
			MethodInfo MoveNext = myEnum.GetType().GetMethod("MoveNext");
			MethodInfo GetCurrent = myEnum.GetType().GetMethod("get_Current");
			rust.SendChatMessage(netuser, systemname, banlist);
			while ((bool)MoveNext.Invoke(myEnum, new object[0]))
            {
                if (current >= bl)
                {
					var bannedUser = GetCurrent.Invoke(myEnum, new object[0]);
                    rust.SendChatMessage(netuser, systemname, string.Format("{0}", bannedUser));
                }
                current++;
                if (current > bl + 20) break;
            }
			return;
		}
		[ChatCommand("ipwl")]
		void cmdChatAddipwl(NetUser netuser, string command, string[] args)
		{
			if (!hasAccessunbanip(netuser, "canunbanip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
			var whitelist = GetPlayerWl("Blacklist(ip.wl)");
			if (args.Length == 0)
			{
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /ipwl 'playername < must be precise");
				return;
			}
			if(!shouldlogall)
			{
				if(!shouldallowwhitelist)
				{
					rust.SendChatMessage(netuser, systemname, "should log banned players is disabled plz enable it to use this command");
					return;
				}
			}
			var playerdata = GetPlayerinfo(args[0]);
			if (playerdata.ContainsKey("name"))
			{
				var gg = playerdata["id"].ToString();
				var q = playerdata["name"].ToString();
				if (whitelist.ContainsKey(gg))
				{
					rust.SendChatMessage(netuser, systemname, args[0] + " is already whitelisted");
					return;
				}
				whitelist.Add(gg, q);
				rust.SendChatMessage(netuser, systemname, args[0] + " " + addedwl);
				return;
				
			}
			rust.SendChatMessage(netuser, systemname, notinbanlist + " " + args[0]);
		}
		[ChatCommand("banslp")]
		void cmdChatAddofflinebanip(NetUser netuser, string command, string[] args)
		{
			if (!hasAccessbanipslp(netuser, "canbanslpip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
			var whitelist = GetPlayerdata("Blacklist(ip)");
			if (args.Length == 0)
			{
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /banslp 'playername < must be precise");
				return;
			}
			if(!shouldlogall)
			{
				if(!shouldallowwhitelist)
				{
					rust.SendChatMessage(netuser, systemname, "should log banned players is disabled plz enable it to use this command");
					return;
				}
			}
			var playerdata = GetPlayerinfo(args[0]);
			if(playerdata.ContainsKey("name"))
			{
				var gg = playerdata["ip"].ToString();
				var q = playerdata["name"].ToString();
				if (whitelist.ContainsKey(gg))
				{
					rust.SendChatMessage(netuser, systemname, args[0] + " is already banned");
					return;
				}
				var staffname = netuser.displayName;
				whitelist.Add(gg, q + " (Offlineban) " + " by: " + staffname);
				rust.SendChatMessage(netuser, systemname, args[0] + " " + addedban);
				return;
				
			}
			rust.SendChatMessage(netuser, systemname, notinbanlist + " " + args[0]);
			
		}
		void Broadcast(string message)
        {
            ConsoleNetworker.Broadcast("chat.add " + systemname + " " + Facepunch.Utility.String.QuoteSafe(message));
        }

        [ChatCommand("banip")]
        void cmdChatAddbanip(NetUser netuser, string command, string[] args)
        {
			if (!hasAccess(netuser, "canbanip")) { rust.SendChatMessage(netuser, systemname, "you are not allowed to use this command"); return; }
            var playerdata = GetPlayerdata("Blacklist(ip)");
            if (args.Length == 0)
            {
				rust.SendChatMessage(netuser, systemname, "wrong syntax: /banip playername");
				return;
            }
			NetUser targetuser = rust.FindPlayer(args[0]);
            if (targetuser == null)
            {
				rust.SendChatMessage(netuser, systemname, couldntFindPlayer);
                return;
            }
			if(shouldpreventbanonself)
			{
				if (targetuser == netuser)
				{
					rust.SendChatMessage(netuser, systemname, banselfmsg);
					return;
				}
			}
			if(shouldpreventbanonrconadmin)
			{
				if (hasimmunity(targetuser, "ipbanimunity")) { rust.SendChatMessage(netuser, systemname, isrconadminmsg); return; }
			}
			cachedSteamid = targetuser.playerClient.userID.ToString();
            cachedName = targetuser.playerClient.userName.ToString();
			var targetname = targetuser.displayName;
            var targetid = targetuser.networkPlayer.externalIP;
			cachedName = string.Empty;
			cachedReason = string.Empty;
            if (args.Length > 1)
            {
                if (cachedName == string.Empty)
                {
                    cachedName = args[1];
                    if (args.Length > 2)
					{
						cachedReason = args[2];
					}
					else
					{
						cachedReason = args[1];
					}
                }
            }
			cachedReason += "(" + netuser.displayName + ")";

            if (playerdata.ContainsKey(targetid))
            {
                rust.SendChatMessage(netuser, systemname, targetname + " " + alreadybanned);
                return;
            }
			if(!playerdata.ContainsKey(targetid))
			{
				var targetuid = targetuser.playerClient.userID.ToString();
				var staffname = netuser.displayName;
				var staffid = netuser.playerClient.userID.ToString();
				rust.SendChatMessage(netuser, systemname, targetname + " " + addedban + " " + cachedReason);
				playerdata.Add(targetid, targetname + " "+ "(id:" + targetuid + " )"  + "(by: " + netuser.displayName +  ")" + " (staffid: " + staffid + " )" + "Reason: " + cachedReason);
				if(shouldcrashclientafterban)
				{
					rust.RunClientCommand(targetuser, "quit");
				}
				if(shouldkick)
				{
					targetuser.Kick(NetError.Facepunch_Kick_RCON, true);
				}
				if (shouldbanid)
			    {
					var targetuserid = targetuser.playerClient.userID.ToString();
					Interface.CallHook("cmdBan", targetuserid, targetname, cachedReason);
			    }
				if(shouldcapframesafterban)
				{
					rust.RunClientCommand(targetuser, "render.frames " + capedamount);
					rust.RunClientCommand(targetuser, "config.save");
				}
				if(shouldbroadcast)
				{
					Broadcast(string.Format(targetname + " " + banMessage + " " + cachedReason));
				}
				return;
			}
            rust.SendChatMessage(netuser, systemname, notinbanlist + " " + targetname.ToString());
			
        }

		PlayerClient FindPlayer(string name)
		{
			foreach (PlayerClient player in PlayerClient.All)
			{
				if (player.userName == name || player.userID.ToString() == name) return player;
			}
			return null;
		}

		[ConsoleCommand("banip")]
		void cmdConsoleReset(ConsoleSystem.Arg arg)
		{
			var playerdata = GetPlayerdata("Blacklist(ip)");
			var targetuser = rust.FindPlayer(arg.ArgsStr);
			if (arg.Args.Length == 1)
			{
				var targetname = targetuser.displayName;
				var targetid = targetuser.networkPlayer.externalIP;
				playerdata.Add(targetid, targetname);
			}
		}
		void OnPlayerConnected(NetUser netuser)
		{
			var playerdata = GetPlayerdata("Blacklist(ip)");
			var Whitelist = GetPlayerWl("Blacklist(ip.wl)");
			var targetid = netuser.networkPlayer.externalIP;
			var targetuserid = netuser.playerClient.userID.ToString();
			var targetname = netuser.displayName;
			if(shouldrebindknownhackkey)
			{
				rust.RunClientCommand(netuser, "input.bind Chat Return q");
				rust.RunClientCommand(netuser, "input.bind Inventory Tab LeftAlt");
			}
			if(shouldlogall)
			{
				var playerlog = GetPlayerinfo(targetname);
				if(playerlog.ContainsKey("id"))
				{
					playerlog.Remove("id");
					playerlog.Remove("name");
					playerlog.Remove("ip");
				}
				playerlog.Add("id", targetuserid);
				playerlog.Add("name", targetname);
				playerlog.Add("ip", targetid);
			}
			if(Whitelist.ContainsKey(targetuserid)) return;
			if(playerdata.ContainsKey(targetid))
			{
				if(shouldcapframes)
				{
					rust.RunClientCommand(netuser, "render.frames " + capedamount);
					rust.RunClientCommand(netuser, "config.save");
				}
				if(shouldcrashclient)
				{
					rust.RunClientCommand(netuser, "quit");
				}
				if (shouldbanidifbanned)
				{
					Interface.CallHook("cmdBan", targetuserid.ToString(), targetname, cachedreason + " " + targetid + " )");
				}
			
				if(shouldbroadcasttryconnect)
			    {
					Broadcast(string.Format(targetname + " " + tryconnectMessage));
			    }
			netuser.Kick(NetError.Facepunch_Kick_RCON, true);
				if(shouldlogall) return;
				if(shouldallowwhitelist)
				{
					var playerlog = GetPlayerinfo(targetname);
					if(playerlog.ContainsKey("id"))
					{
						playerlog.Remove("id");
						playerlog.Remove("name");
						playerlog.Remove("ip");
					}
					playerlog.Add("id", targetuserid);
					playerlog.Add("name", targetname);
					playerlog.Add("ip", targetid);
				}
			}
		}
    }
}