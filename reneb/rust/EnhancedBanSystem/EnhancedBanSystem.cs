using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using System.Linq;
using UnityEngine;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("Enhanced Ban System", "Domestos & Reneb", "3.0.8", ResourceId = 693)]
    class EnhancedBanSystem : RustPlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double LogTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////

        void Loaded()
        {
            LoadData();
            UpdateBannedIPs();
            if (!permission.PermissionExists(PermissionBan)) permission.RegisterPermission(PermissionBan, this);
            if (!permission.PermissionExists(PermissionBanCheck)) permission.RegisterPermission(PermissionBanCheck, this);
            if (!permission.PermissionExists(PermissionKick)) permission.RegisterPermission(PermissionKick, this);
        }
        void Unload()
        {
            SaveData();
        }
        object CanClientLogin(Network.Connection connection)
        {
            string ipaddress = connection.ipaddress.Substring(0, connection.ipaddress.IndexOf(":"));
            string userid = connection.userid.ToString();
            string reason = string.Empty;
            if (banLogs[userid] != null)
                if(!CanConnect(banLogs[userid], connection, out reason))
                {
                    return reason;
                }
            if(bannedIPs[ipaddress] != null)
                if(!CanConnect(bannedIPs[ipaddress], connection, out reason))
                {
                    return reason;
                }
            return null;
        }

        bool CanConnect(BanData bandata, Network.Connection connection, out string reason)
        {
            reason = string.Empty;
            string ipaddress = connection.ipaddress.Substring(0, connection.ipaddress.IndexOf(":"));
            string userid = connection.userid.ToString();

            if (bandata.steamID == ipaddress)
            {
                string reason2 = bandata.reason;
                int duration = bandata.expiration == 0 ? 0 : Convert.ToInt32(Time.time) - bandata.expiration;
                UnbanIP(null, ipaddress);
                BanID(null, connection.userid, reason2, duration);
                bandata = banLogs[userid];
            }
            if (bandata.expiration != 0 && Convert.ToInt32(LogTime()) >= bandata.expiration)
            {
                Unban(null, userid);
            }
            else
            {
                if (!bandata.IPs.Contains(ipaddress)) { AddIPToUserBan(userid, ipaddress); }
                if (banLogs[userid] == null) { BanID(null, connection.userid, bandata.reason, bandata.expiration == 0 ? 0 : Convert.ToInt32(Time.time) - bandata.expiration); }
                reason = bandata.expiration == 0 ? MessageDenyConnection : MessageDenyConnectionTemp;
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////
        // Log Management
        ////////////////////////////////////////////////////////////

        static StoredData storedData;
        static Hash<string, BanData> banLogs = new Hash<string, BanData>();
        static Hash<string, BanData> bannedIPs = new Hash<string, BanData>();

        void UpdateBannedIPs()
        {
            bannedIPs.Clear();
            foreach(KeyValuePair<string, BanData> pair in banLogs)
            {
                foreach(string ip in pair.Value.IPs)
                {
                    if (bannedIPs[ip] == null)
                        bannedIPs.Add(ip, pair.Value);
                }
            }
        }

        class StoredData
        {
            public HashSet<BanData> BanLogs = new HashSet<BanData>();
            public StoredData()
            {
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("EnhancedBanSystem", storedData);
        }

        void LoadData()
        {
            banLogs.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("EnhancedBanSystem");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var thelog in storedData.BanLogs)
            {
                banLogs[thelog.steamID] = thelog;
            }
        }

        public class BanData
        {
            public string steamID;
            public string reason;
            public string name;
            public List<string> IPs;
            public int expiration;

            public BanData() { }

            public BanData(string steamID, string reason, string name, string IP, int duration)
            {
                this.steamID = steamID;
                this.reason = reason;
                this.name = name;
                this.IPs = new List<string> { IP };
                if (duration < 1) this.expiration = 0;
                else
                {
                    this.expiration = Convert.ToInt32(LogTime()) + duration;
                }
            }
        }

        ////////////////////////////////////////////////////////////
        // Config Fields
        ////////////////////////////////////////////////////////////

        static bool BroadcastBans = true;
        static bool BroadcastUnbans = true;
        static bool BroadcastKicks = true;
        static bool LogToConsole = true;
        static string ChatName = "<color=orange>SERVER</color>";
        static string PermissionBan = "enhancedbansystem.ban";
        static string PermissionKick = "enhancedbansystem.kick";
        static string PermissionBanCheck = "enhancedbansystem.bancheck";
        static string MessageKick = "An admin kicked you for {0}";
        static string MessageKickBroadcast = "{0} was kicked from the server for {1}";
        static string MessageBan = "An admin banned you for {0}";
        static string MessageBanBroadcast = "An admin banned {0} from the server for {1}";
        static string MessageDenyConnection = "You are banned on this server";
        static string MessageDenyConnectionTemp = "You are temp-banned on this server";
        static string MessageBanCheck = "Use /bancheck to check if and for how long someone is banned";
        static string DefaultBanReason = "Hacking";
        static string MessageNoPlayerFound = "No player found";
        ////////////////////////////////////////////////////////////
        // Config Management
        ////////////////////////////////////////////////////////////

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }
        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<bool>("Ban - Broadcast Chat", ref BroadcastBans);
            CheckCfg<string>("Ban - permission", ref PermissionBan);
            CheckCfg<string>("Ban - Message - Player", ref MessageBan);
            CheckCfg<string>("Ban - Message - Broadcast", ref MessageBanBroadcast);
            CheckCfg<string>("Ban - Message - Deny Connection - Permanent", ref MessageDenyConnection);
            CheckCfg<string>("Ban - Message - Deny Connection - Temp", ref MessageDenyConnectionTemp);
            CheckCfg<string>("Ban - Default Ban Reason", ref DefaultBanReason);

            CheckCfg<bool>("Kick - Broadcast Chat", ref BroadcastKicks);
            CheckCfg<string>("Kick - permission", ref PermissionKick);
            CheckCfg<string>("Kick - Message - Player", ref MessageKick);
            CheckCfg<string>("Kick - Message - Broadcast", ref MessageKickBroadcast);

            CheckCfg<bool>("Unban - Broadcast Chat", ref BroadcastUnbans);
            CheckCfg<string>("Setting - Chat Name", ref ChatName);
            CheckCfg<string>("Setting - Message - No Player Found", ref MessageNoPlayerFound);
            SaveConfig();
        }

        ////////////////////////////////////////////////////////////
        // External Hooks Functions
        ////////////////////////////////////////////////////////////
        List<string> BannedPlayers()
        {
            var banlist = new List<string>();
            foreach (KeyValuePair<string, BanData> pair in banLogs) { banlist.Add(pair.Key); }
            return banlist;
        }
        Dictionary<string, object> GetBanData(ulong userid)
        {
            return GetBanData(userid.ToString());
        }
        Dictionary<string, object> GetBanData(string target)
        {
            var bandataa = new Dictionary<string, object>();
            if (banLogs[target] != null)
            {
                bandataa.Add("name", banLogs[target].name);
                bandataa.Add("steamID", banLogs[target].steamID);
                bandataa.Add("reason", banLogs[target].reason);
                bandataa.Add("IPs", banLogs[target].IPs);
                bandataa.Add("expiration", banLogs[target].expiration);
            }
            else
            {
                if(bannedIPs[target] != null)
                {
                    BanData bdata = bannedIPs[target];
                    bandataa.Add("name", bdata.name);
                    bandataa.Add("steamID", bdata.steamID);
                    bandataa.Add("reason", bdata.reason);
                    bandataa.Add("IPs", bdata.IPs);
                    bandataa.Add("expiration", bdata.expiration);
                }
            }
            return bandataa;
        }

        ////////////////////////////////////////////////////////////
        // Random Functions
        ////////////////////////////////////////////////////////////
        bool hasPermission(BasePlayer player, string permissionName)
        {
            if (player.net.connection.authLevel > 1) return true;
            return permission.UserHasPermission(player.userID.ToString(), permissionName);
        }

        private object FindPlayer(string arg)
        {
            ulong steamidParsed;
            if (arg.Length == 17 && ulong.TryParse(arg, out steamidParsed))
                return steamidParsed;

            if (PlayerDatabase != null)
            {
                string success = PlayerDatabase.Call("FindPlayer", arg) as string;
                if (success.Length == 17)
                    return ulong.Parse(success);
                else
                    return success;
            }
            else
            {
                List<BasePlayer> foundPlayers = new List<BasePlayer>();
                string argLower = arg.ToLower();
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player.displayName.ToLower().Contains(argLower))
                        foundPlayers.Add(player);
                }
                foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                {
                    if (player.displayName.ToLower().Contains(argLower))
                        foundPlayers.Add(player);
                }
                if (foundPlayers.Count == 1)
                    return foundPlayers[0].userID;
                else if (foundPlayers.Count == 0)
                    return "Couldn't find a player that matches";
                else
                {
                    string msg = "Multiple players found:\n";
                    foreach(BasePlayer player in foundPlayers) { msg += string.Format("{0} {1}\n", player.userID.ToString(), player.displayName); }
                    return msg;
                }
            }
        }
        private object FindOnlinePlayer(string arg)
        {
            ulong steamidParsed = 0L;
            if (arg.Length == 17 && ulong.TryParse(arg, out steamidParsed)) {
                BasePlayer foundplayer = (BasePlayer)BasePlayer.Find(arg);
                if (foundplayer == null) return MessageNoPlayerFound;
                return foundplayer;
            }

            string argLower = arg.ToLower();
            List<BasePlayer> foundPlayers = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(argLower))
                    foundPlayers.Add(player);
            }
            if (foundPlayers.Count == 1)
                return foundPlayers[0];
            else if (foundPlayers.Count == 0)
                return MessageNoPlayerFound;
            else
            {
                string msg = "Multiple players found:\n";
                foreach (BasePlayer player in foundPlayers) { msg += string.Format("{0} {1}\n", player.userID.ToString(), player.displayName); }
                return msg;
            }
        }
        
        void SendMessage(object source, string msg)
        {
            if (source is BasePlayer)
                SendReply((BasePlayer)source, msg);
            else if (source is ConsoleSystem.Arg)
                SendReply((ConsoleSystem.Arg)source, msg);
            else
                Debug.LogWarning(msg);
        }

        
        ////////////////////////////////////////////////////////////
        // show bans functions
        ////////////////////////////////////////////////////////////
        void ShowBans(object source, string[] args)
        {
            int startNum = 0;
            if (args != null && args.Length > 0)
                int.TryParse(args[0], out startNum);

            int current = 0;
            foreach(KeyValuePair<string, BanData> pair in banLogs)
            {
                if (current >= startNum)
                    SendMessage(source, string.Format("{0} - {1} - {2} - {3} {4}", current.ToString(), pair.Key, pair.Value.name, pair.Value.reason, pair.Value.expiration == 0 ? string.Empty : Convert.ToInt32(LogTime()) - pair.Value.expiration > 0 ? "- Expired" : "- "+ (pair.Value.expiration - Convert.ToInt32(LogTime())).ToString() + "s left"));
                current++;
            }
        }

        ////////////////////////////////////////////////////////////
        // Kick functions
        ////////////////////////////////////////////////////////////
        void TryKick(object source, string[] args)
        {
            if (args == null || args.Length < 1) { SendMessage(source, "Syntax: player.kick <name|steamID> <reason (optional)>"); return; }
            Kick(source, args[0], args.Length > 1 ? args[1] : DefaultBanReason);
        }
        void Kick(object source, string target, string reason)
        {
            var findplayer = FindOnlinePlayer(target);
            if (!(findplayer is BasePlayer)) { SendMessage(source, findplayer is string ? (string)findplayer : MessageNoPlayerFound); return; }
            ExecuteKick(source, (BasePlayer)findplayer, reason);
        }
        void ExecuteKick(object source, BasePlayer target, string reason)
        {
            SendMessage(source, string.Format("You've kicked {0}", target.displayName));
            if(BroadcastKicks)
                ConsoleSystem.Broadcast("chat.add", new object[] { 0, ChatName + " " + string.Format(MessageKickBroadcast, target.displayName, reason) });
            Network.Net.sv.Kick(target.net.connection, string.Format(MessageKick, reason));
        }
        ////////////////////////////////////////////////////////////
        // Ban IP functions
        ////////////////////////////////////////////////////////////

        static string ipPattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
        static System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(ipPattern);
        void TryBanIP(object source, string[] args)
        {
            string target = args[0];
            string reason = args.Length > 1 ? args[1] : DefaultBanReason;
            int duration = 0;
            if (args.Length > 2)
                int.TryParse(args[2], out duration);
            BanIP(source, target, reason, duration);
        }
        Dictionary<string, string> FindPlayersByIP(string ipaddress)
        {
            var listplayers = new Dictionary<string, string>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.net.connection.ipaddress.Substring(0, player.net.connection.ipaddress.IndexOf(":")) == ipaddress)
                {
                    string steamid = player.userID.ToString();
                    string name = player.displayName;
                    if(!listplayers.ContainsKey(steamid))
                        listplayers.Add(steamid, name);
                }
            }
            if (PlayerDatabase != null)
            {
                var playerLists = (PlayerDatabase.Call("GetAllKnownPlayers") as HashSet<string>).ToList();
                foreach (string playerid in playerLists)
                {
                    var lips = PlayerDatabase.Call("GetPlayerData", playerid, "IPs") as Dictionary<string, object>;
                    if (lips != null)
                    {
                        foreach (KeyValuePair<string, object> pair in lips)
                        {
                            if (pair.Value.ToString() == ipaddress)
                            {
                                string steamid = playerid;
                                string name = "Unknown";
                                var playerdata = (PlayerDatabase.Call("GetPlayerData", playerid, "default") as Dictionary<string, object>);
                                if (playerdata != null)
                                    if (playerdata["name"] != null)
                                        name = playerdata["name"] as string;
                                if (!listplayers.ContainsKey(steamid))
                                    listplayers.Add(steamid, name);
                            }
                        }
                    }
                }
            }
            return listplayers;
        }
        void RawBanIP(object source, string ipaddress, string reason, int duration)
        {

            BanData bandata = new BanData(ipaddress, reason, "Unknown", ipaddress, duration);
            if (banLogs[ipaddress] != null)
                storedData.BanLogs.Remove(banLogs[ipaddress]);
            banLogs[ipaddress] = bandata;
            storedData.BanLogs.Add(banLogs[ipaddress]);
            SendMessage(source, string.Format("{0} was banned from the server for {1}", ipaddress.ToString(), reason));
            UpdateBannedIPs();
            SaveData();
        }
        void AddIpToBan(object source, string targetID, string ipaddress)
        {
            if (banLogs[targetID] == null) return;

            foreach (string ip in banLogs[targetID].IPs)
            {
                if (ip == ipaddress)
                {
                    SendMessage(source, string.Format("This ip adress is already banned in {0} - {1} for {2}", targetID, banLogs[targetID].name, banLogs[targetID].reason));
                    return;
                }
            }
            storedData.BanLogs.Remove(banLogs[targetID]);
            banLogs[targetID].IPs.Add(ipaddress);
            SendMessage(source, string.Format("This ip adress was added to the ban of {0} - {1} for {2}", targetID, banLogs[targetID].name, banLogs[targetID].reason));
            storedData.BanLogs.Add(banLogs[targetID]);
            SaveData();
        }
        void BanIP(object source, string ipaddress, string reason, int duration)
        {
            string targetName = string.Empty;
            string targetID = string.Empty;
            var players = FindPlayersByIP(ipaddress);
            if(players.Count == 0)
            {
                RawBanIP(source, ipaddress, reason, duration);
                return;
            }
            foreach(KeyValuePair<string,string> pair in players)
            {
                targetID = pair.Key;
                targetName = pair.Value;
                if (banLogs[targetID] == null)
                    ExecuteBan(source, targetID, targetName, ipaddress, reason, duration);
                else
                    AddIpToBan(source, targetID, ipaddress);
            }
            List<BasePlayer> targetkick = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (ipaddress == player.net.connection.ipaddress.Substring(0, player.net.connection.ipaddress.IndexOf(":")))
                    targetkick.Add(player);
            }
            for (int i = 0; i < targetkick.Count; i++)
            {
                Network.Net.sv.Kick(targetkick[i].net.connection, string.Format(MessageBan, reason));
            }
            SaveData();
        }

        bool IsBannedIP(string arg)
        {
            if (bannedIPs[arg] != null) return true;
            return false;
        }
        bool IsBannedUser(ulong userid)
        {
            if (banLogs[userid.ToString()] != null)
                return true;
            return false;
        }
        void AddIPToUserBan(string userid, string ipaddress)
        {
            storedData.BanLogs.Remove(banLogs[userid]);
            banLogs[userid].IPs.Add(ipaddress);
            storedData.BanLogs.Add(banLogs[userid]);
            SaveData();
            UpdateBannedIPs();
        }
        ////////////////////////////////////////////////////////////
        // Unban IP functions
        ////////////////////////////////////////////////////////////

        void UnbanIP(object source, string ipaddress)
        {
            List<string> targetNames = new List<string>();
            List<string> targetIDs = new List<string>();
            bool unbanned = false;

            List<string> unbanip = new List<string>();
            foreach(BanData bdata in banLogs.Values)
            {
                if (bdata.IPs.Contains(ipaddress))
                    unbanip.Add(bdata.steamID);
            }
            foreach(string steamid in unbanip)
            {
                unbanned = true;
                Unban(source, steamid);
            }
            if (banLogs[ipaddress] != null)
            {
                unbanned = true;
                Unban(source, ipaddress);
            }
            if(!unbanned)
                SendMessage(source, "No matchs were found");
        }
        ////////////////////////////////////////////////////////////
        // Ban functions
        ////////////////////////////////////////////////////////////
        void TryBan(object source, string[] args)
        {
            if (args == null || args.Length < 1) { SendMessage(source, "Syntax: player.ban <name|steamID|IP> <reason (optional)> <time in seconds (optional)>"); return; }

            string target = args[0];
            if (regex.IsMatch(target)) { TryBanIP(source, args); return; }

            string reason = args.Length > 1 ? args[1] : DefaultBanReason;

            int duration = 0;
            if (args.Length > 2) int.TryParse(args[2], out duration);

            var findplayer = FindPlayer(args[0]);
            if (!(findplayer is ulong))
            {
                SendMessage(source, findplayer is string ? (string)findplayer : "Couldn't find a player that matches.");
                return;
            }
            if (banLogs[findplayer.ToString()] != null)
            {
                SendMessage(source, "This player is already banned");
                return;
            }
            BanID(source, (ulong)findplayer, reason, duration);
        }

        void BanID(object sourcePlayer, ulong targetID, string reason, int duration)
        {
            string targetName = string.Empty;
            string targetIP = string.Empty;
            string ipaddress = string.Empty;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.userID == targetID)
                {
                    targetName = player.displayName;
                    targetIP = player.net.connection.ipaddress.Substring(0, player.net.connection.ipaddress.IndexOf(":"));
                    break;
                }
            }

            if (targetName == string.Empty || targetIP == string.Empty)
            {
                if (PlayerDatabase != null)
                {
                    var playerdata = (PlayerDatabase.Call("GetPlayerData", targetID.ToString(), "default") as Dictionary<string, object>);
                    if (playerdata != null)
                    {
                        if (playerdata.ContainsKey("name"))
                        {
                            targetName = playerdata["name"] as string;
                        }
                    }
                    var lips = PlayerDatabase.Call("GetPlayerData", targetID.ToString(), "IPs") as Dictionary<string, object>;
                    if (lips != null)
                    {
                        if (lips.ContainsKey("0"))
                        {
                            targetIP = lips["0"] as string;
                        }
                    }
                }
                else
                {
                    foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                    {
                        if (player.userID == targetID)
                        {
                            targetName = player.displayName;
                            targetIP = "0";
                            break;
                        }
                    }
                }
            }
            if (targetName == string.Empty)
            {
                targetName = "Unknown";
                targetIP = "0";
            }
            if(banLogs[targetIP] != null)
                UnbanIP(sourcePlayer, targetIP);
            ExecuteBan(sourcePlayer, targetID.ToString(), targetName, targetIP, reason, duration);
        }
        void Ban(object sourcePlayer, BasePlayer player, string reason, object duration)
        {
            BanID(sourcePlayer, player.userID, reason, duration is int ? (int)duration : 0);
        }
        void ExecuteBan(object sourcePlayer, string targetID, string targetName, string targetIP, string reason, int duration)
        {
            BanData bandata = new BanData(targetID, reason, targetName, targetIP, duration);
            if (banLogs[targetID] != null)
                storedData.BanLogs.Remove(banLogs[targetID]);
            banLogs[targetID] = bandata;
            storedData.BanLogs.Add(banLogs[targetID]);
            SendMessage(sourcePlayer, string.Format("{0} - {1} was banned from the server for {2}", targetID, targetName, reason));
            if(BroadcastBans)
            {
                ConsoleSystem.Broadcast("chat.add", new object[] { 0, ChatName + " " + string.Format(MessageBanBroadcast, targetName, reason) });
            }
            List<BasePlayer> targetkick = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.userID.ToString() == targetID)
                    targetkick.Add(player);
            }
            for (int i = 0; i < targetkick.Count; i++)
            {
                Network.Net.sv.Kick(targetkick[i].net.connection, string.Format(MessageBan, reason));
            }
            UpdateBannedIPs();
            SaveData();
        }
        void CheckBan(object sourcePlayer, string[] args)
        {
            if (args == null || args.Length < 1)
            {
                SendMessage(sourcePlayer, "Syntax: player.checkban <name|steamID|ip>");
                return;
            }
            string target = args[0].ToLower();
            Dictionary<string, object> bdata = new Dictionary<string, object>();

            var tempdata = GetBanData(target);
            if(tempdata.Count != 0)
                bdata = tempdata;

            if(bdata.Count == 0)
            {
                object findplayer = FindPlayer(target);
                if (findplayer is ulong)
                {
                    var tdata = GetBanData((ulong)findplayer);
                    if (tdata.Count != 0)
                        bdata = tempdata;
                }
            }

            if(bdata.Count == 0)
            {
                List<BanData> foundBanData = new List<BanData>();
                foreach (KeyValuePair<string, BanData> pair in banLogs)
                {
                    if (pair.Value.name.ToLower().Contains(target) || pair.Value.IPs.Contains(target))
                        foundBanData.Add(pair.Value);
                }
                if(foundBanData.Count > 1)
                {
                    string msg = "Multiple bans found:\n";
                    foreach (BanData bbdata in foundBanData) { msg += string.Format("{0} {1}\n", bbdata.steamID, bbdata.name); }
                    SendMessage(sourcePlayer, msg);
                    return;
                }
                if(foundBanData.Count == 1)
                {
                    bdata.Add("name", foundBanData[0].name);
                    bdata.Add("steamID", foundBanData[0].steamID);
                    bdata.Add("reason", foundBanData[0].reason);
                    bdata.Add("IPs", foundBanData[0].IPs);
                    bdata.Add("expiration", foundBanData[0].expiration);
                }
            }

            if(bdata.Count == 0)
            {
                SendMessage(sourcePlayer, string.Format("No players found matching {0}", args[0]));
                return;
            }

            if ((int)bdata["expiration"] == 0)
            {
                SendMessage(sourcePlayer, string.Format("{1} {0} is permanently banned for {2}", (string)bdata["name"], (string)bdata["steamID"], (string)bdata["reason"]));
            }
            else if (Convert.ToInt32(LogTime()) >= (int)bdata["expiration"])
            {
                SendMessage(sourcePlayer, string.Format("{1} {0} ban expired ({2})", (string)bdata["name"], (string)bdata["steamID"], (string)bdata["reason"]));
            }
            else
            {
                SendMessage(sourcePlayer, string.Format("{1} {0} is temporarly banned for {2}", (string)bdata["name"], (string)bdata["steamID"], (string)bdata["reason"]));
            }
        }

        ////////////////////////////////////////////////////////////
        // Unban functions
        ////////////////////////////////////////////////////////////
        void TryUnban(object source, string[] args)
        {
            if (args == null || args.Length < 1)
            { 
                return;
            }
            string target = args[0];
            if (regex.IsMatch(target))
            {
                UnbanIP(source, target);
                return;
            }
            object findban = FindBanLog(target);
            if (!(findban is ulong))
            {
                SendMessage(source, findban is string ? (string)findban : "Couldn't find a ban that matches.");
                return;
            }
            Unban(source, (ulong)findban);
        }
        object FindBanLog(string arg)
        {
            string argLower = arg.ToLower();
            if (banLogs[arg] != null)
                return ulong.Parse(arg);

            string foundNames = string.Empty;
            ulong foundBandata = 0L;
            bool multipleBans = false;
            foreach (KeyValuePair<string, BanData> pair in banLogs)
            {
                if (pair.Value.name.ToLower().Contains(argLower))
                {
                    if (foundBandata == 0L)
                    {
                        foundBandata = ulong.Parse(pair.Key);
                        foundNames += string.Format("{0} {1}\n", pair.Key, pair.Value.name);
                    }
                    else
                    {
                        multipleBans = true;
                        foundNames += string.Format("- {0} {1}\n", pair.Key, pair.Value.name);
                    }
                }
            }
            if (multipleBans)
                return foundNames;
            if (foundBandata == 0L)
                return "No ban found";
            return foundBandata;
        }
        void Unban(object source, string targetID)
        {
            if (banLogs[targetID] == null)
                SendMessage(source, string.Format("{0} isn't banned", targetID));
            else
            {
                string name = banLogs[targetID].name;
                DeleteBan(targetID);
                SendMessage(source, string.Format("{0} - {1} was unbanned from the server.", targetID, name));

                if(BroadcastUnbans)
                    ConsoleSystem.Broadcast("chat.add", new object[] { 0, ChatName + " " + string.Format("{0} - {1} was unbanned from the server.", targetID, name) });
            }
        }
        void DeleteBan(string targetID)
        {
            storedData.BanLogs.Remove(banLogs[targetID]);
            banLogs[targetID] = null;
            banLogs.Remove(targetID);
            SaveData();
            UpdateBannedIPs();
        }
        void Unban(object source, ulong targetID)
        {
            Unban(source, targetID.ToString());
        }
        ////////////////////////////////////////////////////////////
        // Chat commands
        ////////////////////////////////////////////////////////////
        [ChatCommand("ban")]
        void cmdChatBan(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan))
            {
                SendReply(player, "You dont have access to this command");
                return;
            }
            TryBan(player, args);
        }
        [ChatCommand("kick")]
        void cmdChatKick(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionKick))
            {
                SendReply(player, "You dont have access to this command");
                return;
            }
            TryKick(player, args);
        }
        [ChatCommand("unban")]
        void cmdChatUnban(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan))
            {
                SendReply(player, "You dont have access to this command");
                return;
            }
            TryUnban(player, args);
        }
        
        [ChatCommand("checkban")]
        void cmdChatCheckBan(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBanCheck))
            {
                SendReply(player, "You dont have access to this command");
                return;
            }
            CheckBan(player, args);
        }
        ////////////////////////////////////////////////////////////
        // Console Commands
        ////////////////////////////////////////////////////////////
        [ConsoleCommand("player.ban")]
        void cmdConsolePlayerBan(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), PermissionBan))
                {
                    SendReply(arg, "You dont have access to this command");
                    return;
                }
            }
            TryBan(arg, arg.Args);
        }
        
        [ConsoleCommand("player.checkban")]
        void cmdConsolePlayerCheckBan(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), PermissionBanCheck))
                {
                    SendReply(arg, "You dont have access to this command");
                    return;
                }
            }
            CheckBan(arg, arg.Args);
        }
        [ConsoleCommand("player.kick")]
        void cmdConsolePlayerKick(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), PermissionKick))
                {
                    SendReply(arg, "You dont have access to this command");
                    return;
                }
            }
            TryKick(arg, arg.Args);
        }
        [ConsoleCommand("player.unban")]
        void cmdConsolePlayerUnban(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), PermissionBan))
                {
                    SendReply(arg, "You dont have access to this command");
                    return;
                }
            }
            TryUnban(arg, arg.Args);
        }
        [ConsoleCommand("player.banlist")]
        void ccmdPlayerBanlist(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (!hasPermission(arg.Player(), PermissionBanCheck))
                {
                    SendReply(arg, "You dont have access to this command");
                    return;
                }
            }
            ShowBans(arg, arg.Args);
        }
        [ConsoleCommand("ebs.import")]
        void ccmdEBSImport(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                SendReply(arg, "You can only use this command from the server console");
                return;
            }
            var ebslist = Interface.Oxide.DataFileSystem.GetFile("ebsbanlist") as Oxide.Core.Configuration.DynamicConfigFile;
            foreach (KeyValuePair<string, object> pair in ebslist)
            {
                if (pair.Key == null) continue;
                if (pair.Value == null) continue;
                if (banLogs[pair.Key] != null) continue;
                var playerbanlistdata = pair.Value as Dictionary<string, object>;
                if (playerbanlistdata == null) continue;
                ExecuteBan(null, playerbanlistdata["steamID"].ToString(), playerbanlistdata["name"].ToString(), playerbanlistdata["IP"].ToString(), playerbanlistdata["reason"].ToString(), 0);
            }
            SaveData();
        }
    }
}
