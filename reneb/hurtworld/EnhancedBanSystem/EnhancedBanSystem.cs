using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
namespace Oxide.Plugins
{
    [Info("Enhanced Ban System", "Domestos & Reneb", "1.0.2")]
    class EnhancedBanSystem : HurtworldPlugin
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
            permission.RegisterPermission(PermissionBan, this);
            permission.RegisterPermission(PermissionBanCheck, this);
            permission.RegisterPermission(PermissionKick, this);
        }
        void Unload()
        {
            SaveData();
        }
        
        object CanClientLogin(PlayerSession session)
        {
            string ipaddress = session.Player.ipAddress;
            string userid = session.Identity.SteamId.ToString();
            string reason = string.Empty;
            if (banLogs[userid] != null)
                if(!CanConnect(banLogs[userid], session, out reason))
                {
                    return reason;
                }
            if(bannedIPs[ipaddress] != null)
                if(!CanConnect(bannedIPs[ipaddress], session, out reason))
                {
                    return reason;
                }
            return null;
        }

        bool CanConnect(BanData bandata, PlayerSession session, out string reason)
        {
            reason = string.Empty;
            string ipaddress = session.Player.ipAddress;
            string userid = session.Identity.SteamId.ToString();

            if (bandata.steamID == ipaddress)
            {
                string reason2 = bandata.reason;
                int duration = bandata.expiration == 0 ? 0 : Convert.ToInt32(Time.time) - bandata.expiration;
                UnbanIP(null, ipaddress);
                BanID(null, session.Identity.SteamId.m_SteamID, reason2, duration);
                bandata = banLogs[userid];
            }
            if (bandata.expiration != 0 && Convert.ToInt32(LogTime()) >= bandata.expiration)
            {
                Unban(null, userid);
            } 
            else
            {
                if (!bandata.IPs.Contains(ipaddress)) { AddIPToUserBan(userid, ipaddress); }
                if (banLogs[userid] == null) { BanID(null, session.Identity.SteamId.m_SteamID, bandata.reason, bandata.expiration == 0 ? 0 : Convert.ToInt32(Time.time) - bandata.expiration); }
                reason = bandata.expiration == 0 ? GetMessage("MessageDenyConnection", userid) : GetMessage("MessageDenyConnectionTemp", userid);
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

        static string DefaultBanReason = "Hacking";

        string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);

        

        
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
            CheckCfg<string>("Ban - Default Ban Reason", ref DefaultBanReason);

            CheckCfg<bool>("Kick - Broadcast Chat", ref BroadcastKicks);
            CheckCfg<string>("Kick - permission", ref PermissionKick);

            CheckCfg<bool>("Unban - Broadcast Chat", ref BroadcastUnbans);
            CheckCfg<string>("Setting - Chat Name", ref ChatName);


            var messages = new Dictionary<string, string>
        {
            {"ChatName", "You are not allowed to use this command"},
            {"MessageKick", "An admin kicked you for {0}"},
            {"MessageKickBroadcast", "{0} was kicked from the server for {1}" },
            {"MessageBan", "An admin banned you for {0}"},
            {"MessageBanBroadcast", "An admin banned {0} from the server for {1}"},
            {"MessageDenyConnection", "You are banned on this server"},
            {"MessageDenyConnectionTemp", "You are temp-banned on this server"},
            {"MessageBanCheck", "Use /bancheck to check if and for how long someone is banned"},
            {"MessageNoPlayerFound",  "No player found"},
            {"MessageMultiplePlayersFound",  "Multiple players found:"}
        };

            lang.RegisterMessages(messages, this);

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
        bool hasPermission(PlayerSession player, string permissionName)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.SteamId.ToString(), permissionName);
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
                List<PlayerSession> foundPlayers = new List<PlayerSession>();
                string argLower = arg.ToLower();
                foreach (var pair in GameManager.Instance.GetSessions())
                {
                    var tplayer = pair.Value;
                    if (tplayer.Name.ToLower().Contains(argLower))
                        foundPlayers.Add(tplayer);
                }
                if (foundPlayers.Count == 1)
                    return foundPlayers[0].SteamId.m_SteamID;
                else if (foundPlayers.Count == 0)
                    return GetMessage("MessageNoPlayerFound", null);
                else
                {
                    string msg = GetMessage("MessageMultiplePlayersFound", null)+"\n";
                    foreach (PlayerSession player in foundPlayers) { msg += string.Format("{0} {1}\n", player.SteamId.ToString(), player.Name); }
                    return msg;
                }
            }
        }
        private object FindOnlinePlayer(string arg)
        {
            ulong steamidParsed = 0L;
            if(arg.Length == 17)
                ulong.TryParse(arg, out steamidParsed);

            string argLower = arg.ToLower();
            List<PlayerSession> foundPlayers = new List<PlayerSession>();
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var tplayer = pair.Value;
                if (tplayer == null) continue;
                if (tplayer.Name.ToLower().Contains(argLower))
                    foundPlayers.Add(tplayer);
                else if(steamidParsed != 0L && tplayer.SteamId.ToString() == arg)
                    return tplayer;
            }
            if (foundPlayers.Count == 1)
                return foundPlayers[0];
            else if (foundPlayers.Count == 0)
                return GetMessage("MessageNoPlayerFound", null);
            else
            {
                string msg = GetMessage("MessageMultiplePlayersFound", null) + "\n";
                foreach (PlayerSession player in foundPlayers) { msg += string.Format("{0} {1}\n", player.SteamId.ToString(), player.Name); }
                return msg;
            }
        }
        
        void SendMessage(object source, string msg)
        {
            if (source is PlayerSession)
                hurt.SendChatMessage((PlayerSession)source, msg);
            /*else if (source is ConsoleSystem.Arg)
                SendReply((ConsoleSystem.Arg)source, msg);*/
            else
                Interface.Oxide.LogInfo(msg);
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
            if (!(findplayer is PlayerSession)) { SendMessage(source, findplayer is string ? (string)findplayer : GetMessage("MessageNoPlayerFound", null)); return; }
            ExecuteKick(source, (PlayerSession)findplayer, reason);
        }
        void ExecuteKick(object source, PlayerSession target, string reason)
        {
            SendMessage(source, string.Format("You've kicked {0}", target.Name));
            if(BroadcastKicks)
                hurt.BroadcastChat(ChatName, string.Format(GetMessage("MessageKickBroadcast", null), target.Name, reason));
            if(target.Player != null)
                GameManager.Instance.StartCoroutine(GameManager.Instance.DisconnectPlayerSync(target.Player, string.Format(GetMessage("MessageKick", target.SteamId.ToString()), reason)));
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
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var player = pair.Value;
                if (player.Player == null) continue;
                if (player.Player.ipAddress == ipaddress)
                {
                    string steamid = player.SteamId.ToString();
                    string name = player.Name;
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
            List<PlayerSession> targetkick = new List<PlayerSession>();
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var player = pair.Value;
                if (player.Player == null) continue;
                if (ipaddress == player.Player.ipAddress)
                    targetkick.Add(player);
            }
            for (int i = 0; i < targetkick.Count; i++)
            {
                GameManager.Instance.StartCoroutine(GameManager.Instance.DisconnectPlayerSync(targetkick[i].Player, string.Format(GetMessage("MessageBan", targetkick[i].SteamId.ToString()), reason)));
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
            string targetid = targetID.ToString();
            string ipaddress = string.Empty;
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var player = pair.Value;
                if (player.SteamId.ToString() == targetid)
                {
                    targetName = player.Name;
                    targetIP = player.Player == null ? "0" : player.Player.ipAddress;
                    break;
                }
            }

            if (targetName == string.Empty || targetIP == string.Empty)
            {
                if (PlayerDatabase != null)
                {
                    var playerdata = (PlayerDatabase.Call("GetPlayerData", targetid, "default") as Dictionary<string, object>);
                    if (playerdata != null)
                    {
                        if (playerdata.ContainsKey("name"))
                        {
                            targetName = playerdata["name"] as string;
                        }
                    }
                    var lips = PlayerDatabase.Call("GetPlayerData", targetid, "IPs") as Dictionary<string, object>;
                    if (lips != null)
                    {
                        if (lips.ContainsKey("0"))
                        {
                            targetIP = lips["0"] as string;
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
        void Ban(object sourcePlayer, PlayerSession player, string reason, object duration)
        {
            BanID(sourcePlayer, player.SteamId.m_SteamID, reason, duration is int ? (int)duration : 0);
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
                hurt.BroadcastChat(ChatName, string.Format(GetMessage("MessageBanBroadcast", null), targetName, reason));

            List<PlayerSession> targetkick = new List<PlayerSession>();

            foreach (var pair in GameManager.Instance.GetSessions())
            {
                var player = pair.Value;
                if (player.Player == null) continue;
                if (player.SteamId.ToString() == targetID)
                    targetkick.Add(player);
            }
            for (int i = 0; i < targetkick.Count; i++)
            {
                GameManager.Instance.StartCoroutine(GameManager.Instance.DisconnectPlayerSync(targetkick[i].Player, string.Format(GetMessage("MessageBan", targetkick[i].SteamId.ToString()), reason)));
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
                    hurt.BroadcastChat(ChatName, string.Format("{0} - {1} was unbanned from the server.", targetID, name));
            }
        }
        void DeleteBan(string targetID)
        {
            storedData.BanLogs.Remove(banLogs[targetID]);
            banLogs.Remove(targetID);
            banLogs[targetID] = null;
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
        void cmdChatBan(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan))
            {
                SendMessage(player, "You dont have access to this command");
                return;
            }
            TryBan(player, args);
        }
        [ChatCommand("kick")]
        void cmdChatKick(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionKick))
            {
                SendMessage(player, "You dont have access to this command");
                return;
            }
            TryKick(player, args);
        }
        [ChatCommand("unban")]
        void cmdChatUnban(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan))
            {
                SendMessage(player, "You dont have access to this command");
                return;
            }
            TryUnban(player, args);
        }
        
        [ChatCommand("checkban")]
        void cmdChatCheckBan(PlayerSession player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBanCheck))
            {
                SendMessage(player, "You dont have access to this command");
                return;
            }
            CheckBan(player, args);
        }
        ////////////////////////////////////////////////////////////
        // Console Commands
        ////////////////////////////////////////////////////////////
        /* 
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
        }*/
    }
}
