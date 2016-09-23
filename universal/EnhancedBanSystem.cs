using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Enhanced Ban System", "Reneb, initially by Domestos", "4.0.10")]
    class EnhancedBanSystem : CovalencePlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        ////////////////////////////////////////////////////////////
        // Static fields
        ////////////////////////////////////////////////////////////
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        static string ipPattern = @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
        static System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(ipPattern);

        ////////////////////////////////////////////////////////////
        // Static Methods
        ////////////////////////////////////////////////////////////

        static double LogTime() { return DateTime.UtcNow.Subtract(epoch).TotalSeconds; }

        string GetMsg(string key, object steamid = null) { return lang.GetMessage(key, this, steamid == null ? null : steamid.ToString()); }

        string NormalizeIP(string ip) { if (!ip.Contains(":")) return ip; return ip.Substring(0, ip.LastIndexOf(":")); }

        bool hasPermission(IPlayer player, string permissionName) { 
        	if (player.IsAdmin) return true; 
        	return permission.UserHasPermission(player.Id.ToString(), permissionName); 
        }

        string FormatOnlineBansystem(string line, Dictionary<string, string> args)
        {
            foreach (KeyValuePair<string, string> pair in args)
            {
                line = line.Replace(pair.Key, pair.Value);
            }
            return line;
        }

        ////////////////////////////////////////////////////////////
        // Configs
        ////////////////////////////////////////////////////////////

        string PermissionBan = "enhancedbansystem.ban";
        string PermissionUnban = "enhancedbansystem.unban";
        string PermissionBanlist = "enhancedbansystem.banlist";
        string PermissionKick = "enhancedbansystem.kick";
        string OnlineWebRequestBanLine = "http://webpage.com/api.php?action=ban&pass=mypassword&steamid={steamid}&name={name}&ip={ip}&reason={reason}&source={source}&tempban={expiration}";
        string OnlineWebRequestUnbanLine = "http://webpage.com/api.php?action=unban&pass=mypassword&steamid={steamid}&name={name}&ip={ip}&name={name}&source={source}";
        string OnlineWebRequestIsbanned = "http://webpage.com/api.php?action=isbanned&pass=mypassword&steamid={steamid}&ip={ip}&time={time}&name={name}";
        string OnlineWebRequestBanlist = "http://webpage.com/banlist.php";
        string OnlineWebRequestIsbannedYes = "1";
        string OnlineWebRequestIsbannedNo = "0";
        string BanDefaultReason = "Banned";
        bool BroadcastKick = true;
        bool BroadcastBan = true;
        string BroadcastBanMessage = "{0} was banned from the server ({1})";
        string BroadcastKickMessage = "{0} was kicked from the server ({1})";
        int Bansystem = 1;

        protected override void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T) 
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        { 
            CheckCfg<int>("BanSystem - 0: Both System, 1: LocalData, 2: OnlineWebRequest", ref Bansystem);
            CheckCfg<string>("Bans - Permission", ref PermissionBan);
            CheckCfg<bool>("Bans - Broadcast", ref BroadcastBan);
            CheckCfg<string>("Bans - Broadcast Message", ref BroadcastBanMessage);
            CheckCfg<string>("Bans - Default Reason", ref BanDefaultReason);
            CheckCfg<string>("Unban - Permission", ref PermissionUnban);
            CheckCfg<string>("Kick - Permission", ref PermissionKick);
            CheckCfg<bool>("Kick - Broadcast", ref BroadcastKick);
            CheckCfg<string>("Kick - Broadcast Message", ref BroadcastKickMessage);
            CheckCfg<string>("Banlist - Permission", ref PermissionBanlist);
            CheckCfg<string>("Online Banlist - Ban line request", ref OnlineWebRequestBanLine);
            CheckCfg<string>("Online Banlist - Unban line request", ref OnlineWebRequestUnbanLine);
            CheckCfg<string>("Online Banlist - Check if banned request", ref OnlineWebRequestIsbanned);
            CheckCfg<string>("Online Banlist - Check if banned request - answer yes", ref OnlineWebRequestIsbannedYes);
            CheckCfg<string>("Online Banlist - Check if banned request - answer no", ref OnlineWebRequestIsbannedNo);
            CheckCfg<string>("Online Banlist - Banlist page", ref OnlineWebRequestBanlist);

            SaveConfig();
        }

        ////////////////////////////////////////////////////////////
        // Log Management
        ////////////////////////////////////////////////////////////

        static StoredData storedData;
        static Hash<string, BanData> banLogs = new Hash<string, BanData>();
        static Hash<string, List<BanData>> bannedIPs = new Hash<string, List<BanData>>();

        void UpdateBannedIPs()
        {
            bannedIPs.Clear();
            foreach (KeyValuePair<string, BanData> pair in banLogs)
            {
                foreach (string ip in pair.Value.IPs)
                {
                    if (!bannedIPs.ContainsKey(ip))
                        bannedIPs.Add(ip, new List<BanData>());
                    if (!bannedIPs[ip].Contains(pair.Value))
                        bannedIPs[ip].Add(pair.Value);
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
            public string date;
            public List<string> IPs;
            public int expiration;
            public string source;

            public BanData() { }

            public BanData(string steamID, string reason, string name, List<string> IP, int duration, string source)
            {
                this.steamID = steamID;
                this.reason = reason;
                this.name = name;
                this.IPs = IP;
                this.expiration = duration < 1 ? 0 : Convert.ToInt32(LogTime()) + duration;
                this.date = LogTime().ToString();
            }
        }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission(PermissionBan, this);
            permission.RegisterPermission(PermissionUnban, this);
            permission.RegisterPermission(PermissionBanlist, this);
            permission.RegisterPermission(PermissionKick, this);
            LoadData();
            UpdateBannedIPs();

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "You don't have the permission to use this command.","You don't have the permission to use this command." },
                { "{0} - {1} is already banned.","{0} - {1} is already banned." },
                { "This IP is already banned.","This IP is already banned." },
                {  "No bans matching this ip.", "No bans matching this ip." },
                {  "No bans matching this steamid.", "No bans matching this steamid." },
                { "No bans matching this name.","No bans matching this name." },
                {"Multiple bans matching this name:\r\n","Multiple bans matching this name:\r\n" },
                {  "{0} {1} was successfully unbanned.", "{0} {1} was successfully unbanned." },
                { "Syntax: ban <Name|SteamID|IP> <reason (optional)> <time in secondes (optional>", "Syntax: ban <Name|SteamID|IP> <reason (optional)> <time in secondes (optional>" },
                { "Syntax: player.ban <Name|SteamID|IP> <reason (optional)> <time in secondes (optional>", "Syntax: player.ban <Name|SteamID|IP> <reason (optional)> <time in secondes (optional>" },
                { "{0} {1} was successfully banned from the server ({2})", "{0} {1} was successfully banned from the server ({2})" },
                { "{0} {1} was successfully tempbanned from the server ({2}) for {3} secs","{0} {1} was successfully tempbanned from the server ({2}) for {3} secs" },
                {"You are banned from this server ({0})", "You are banned from this server ({0})" },
                { "You are tempbanned from this server ({0})", "You are tempbanned from this server ({0})" },
                { "Syntax: unban <Name|SteamID|IP>","Syntax: unban <Name|SteamID|IP>" },
                 { "Syntax: player.unban <Name|SteamID|IP>","Syntax: player.unban <Name|SteamID|IP>" },
                {"Syntax: kick <Name|SteamID|IP> <reason(optional)>","Syntax: kick <Name|SteamID|IP> <reason(optional)>" },
                {"Syntax: player.kick <Name|SteamID|IP> <reason(optional)>","Syntax: player.kick <Name|SteamID|IP> <reason(optional)>" },
                {"No player matching this name was found.","No player matching this name was found." },
                {"Command Sent, waiting for an answer","Command Sent, waiting for an answer" },
                {"You are banned from this server.","You are banned from this server." }
            }, this);
        }

        void OnUserConnected(IPlayer player)
        {
            string reason = string.Empty;
            string steamid = player.Id.ToString();
            string name = player.Name.ToString();
            string ip = NormalizeIP(player.Address);

            /*
            if (Bansystem == 0 || Bansystem == 1)
            {
                if (isBanned(steamid, name, ip, out reason, out expiration))
                {
                    timer.Once(0.01f, () => player.ConnectedPlayer.Kick(expiration == 0 ? string.Format(GetMsg("You are banned from this server ({0})", steamid), reason) : string.Format(GetMsg("You are tempbanned from this server ({0})", steamid), reason)));
                }
            }*/

            if (Bansystem == 0 || Bansystem == 2)
            {
                webrequest.EnqueueGet(FormatOnlineBansystem(OnlineWebRequestIsbanned, new Dictionary<string, string> { { "{steamid}", steamid }, { "{name}", name }, { "{ip}", ip }, { "{source}", steamid }, { "{time}", LogTime().ToString() } }), (code, response) =>
                {
                    if (response != null || code != 200)
                    {
                        if (response == OnlineWebRequestIsbannedYes)
                            timer.Once(0.01f, () => player.Kick(GetMsg("You are banned from this server.", steamid)));
                    }
                }, this);
            }
        }

        object CanUserLogin(string name, string steamid, string ip)
        {
            if (Bansystem == 0 || Bansystem == 1)
            {
                string reason = string.Empty;
                int expiration = 0;
                if (isBanned(steamid, name, ip, out reason, out expiration))
                {
                    return expiration == 0 ? string.Format(GetMsg("You are banned from this server ({0})", steamid), reason) : string.Format(GetMsg("You are tempbanned from this server ({0})", steamid), reason);
                }
            }
            return null;
        }

        ////////////////////////////////////////////////////////////
        // Players Management
        ////////////////////////////////////////////////////////////

        List<string> FindOnlinePlayers(string arg)
        {
            var name = arg.ToLower();
            var liststeamids = new List<string>();
            ulong steamid = 0L;
            if (arg.Length == 17 && ulong.TryParse(arg, out steamid))
            {
                liststeamids.Add(steamid.ToString());
            }
            else if (regex.IsMatch(arg))
            {
                foreach (var player in players.GetAllPlayers().Where(x => x.IsConnected)) { if (NormalizeIP(player.Address) == arg && !liststeamids.Contains(player.Id.ToString())) liststeamids.Add(player.Id.ToString()); }
            }
            else
            {
                foreach (var player in players.GetAllPlayers().Where(x => x.IsConnected)) { if (player.Name.ToString().ToLower().Contains(name) && !liststeamids.Contains(player.Id.ToString())) liststeamids.Add(player.Id.ToString()); }
            }
            return liststeamids;
        }

        List<string> FindPlayers(string arg)
        {
            ulong steamid = 0L;
            if (arg.Length == 17 && ulong.TryParse(arg, out steamid)) return new List<string> { steamid.ToString() };

            List<string> results = new List<string>();
            string searchip = string.Empty;
            string searchname = arg.ToLower();

            if (regex.IsMatch(arg))
                searchip = arg;

            var liststeamids = new List<string>();

            foreach (var player in players.GetAllPlayers()) { if (!liststeamids.Contains(player.Id.ToString())) liststeamids.Add(player.Id.ToString()); }
            if (PlayerDatabase) { foreach (var userid in (PlayerDatabase.Call("GetAllKnownPlayers") as HashSet<string>)) { if (!liststeamids.Contains(userid)) liststeamids.Add(userid); } }

            foreach (var userid in liststeamids)
            {
                if (searchip != string.Empty)
                {
                    if (PlayerDatabase)
                    {
                        var success = PlayerDatabase.Call("GetPlayerDataRaw", userid, "IPs");
                        if (success is string)
                        {
                            var ips = JsonConvert.DeserializeObject<List<string>>((string)success);
                            if (ips != null && ips.Contains(searchip) && !results.Contains(userid)) results.Add(userid);
                        }
                    }
                    else
                    {
                        var gplayer = players.GetPlayer(userid);
                        if (gplayer != null && gplayer.IsConnected && NormalizeIP(gplayer.Address) == searchip && !results.Contains(userid)) results.Add(userid);
                    }
                }
                else
                {

                    var g2player = players.GetPlayer(userid);
                    if (g2player != null && g2player.Name.ToString().ToLower().Contains(searchname) && !results.Contains(userid)) results.Add(userid);
                    if (PlayerDatabase && g2player == null)
                    {
                        var name = (string)PlayerDatabase.Call("GetPlayerData", userid, "name") ?? string.Empty;
                        if (name != string.Empty && name.ToLower().Contains(searchname) && !results.Contains(userid)) results.Add(userid);
                    }
                }
            }

            return results;
        }

        string UserIDToName(string userid)
        {
            var name = string.Empty;
            if (PlayerDatabase)
            {
                name = (string)PlayerDatabase.Call("GetPlayerData", userid, "name") ?? string.Empty;
            }
            if (name == string.Empty)
            {
                var gplayer = players.GetPlayer(userid);
                if (gplayer != null) name = gplayer.Name.ToString();
            }
            if (name == string.Empty) name = "Unknown Error 1";
            return name;
        }

        Dictionary<string, object> GetPlayerData(string steamid)
        {
            var playerinfo = new Dictionary<string, object> { { "steamid", steamid }, { "IPs", new List<string>() }, { "name", string.Empty } };

            if (PlayerDatabase)
            {
                var name = (string)PlayerDatabase.Call("GetPlayerData", steamid, "name") ?? string.Empty;
                playerinfo["name"] = name;

                var playerIPs = new List<string>();
                var successs = PlayerDatabase.Call("GetPlayerDataRaw", steamid, "IPs");
                if (successs is string)
                {
                    playerIPs = JsonConvert.DeserializeObject<List<string>>((string)successs);
                }
                foreach (var ip in playerIPs) { ((List<string>)playerinfo["IPs"]).Add(ip); }
            }
            var iplayer = players.GetPlayer(steamid);
            if (iplayer != null)
            {
                if ((string)playerinfo["name"] == string.Empty) playerinfo["name"] = iplayer.Name.ToString();
                if (((List<string>)playerinfo["IPs"]).Count == 0 && iplayer.IsConnected) ((List<string>)playerinfo["IPs"]).Add(NormalizeIP(iplayer.Address.ToString()));
            }
            return playerinfo;
        }

        ////////////////////////////////////////////////////////////
        // Kick
        ////////////////////////////////////////////////////////////

        string Kick(object source, string target, string reason, bool isBan)
        {
            var foundplayers = FindOnlinePlayers(target);
            if (foundplayers.Count == 0) { return GetMsg("No player matching this name was found.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null); }
            if (!regex.IsMatch(target) && foundplayers.Count > 1) { var returnmsg = string.Empty; foreach (var userid in foundplayers) { returnmsg += string.Format("{0} - {1}\r\n", userid, players.GetPlayer(userid).Name.ToString()); } return returnmsg; }

            var returnkick = string.Empty;
            foreach (string steamid in foundplayers)
            {
                returnkick += ExecuteKick(source, foundplayers[0], reason, isBan) + "\r\n";
            }
            return returnkick;
        }
        string Kick(object source, string[] args)
        {
            string target = args[0];
            string reason = args.Length > 1 ? args[1] : "Kicked";
            return Kick(source, target, reason, false);
        }

        string ExecuteKick(object source, string steamid, string reason, bool isBan)
        {
            var player = players.GetConnectedPlayer(steamid);
            if (player == null) { return GetMsg("Error, this player doesn't seem to be connected", source is IPlayer ? ((IPlayer)source).Id.ToString() : null); }

            if (isBan) { if (BroadcastBan) server.Broadcast(string.Format(BroadcastBanMessage, player.Name.ToString(), reason)); }
            else if (BroadcastKick) server.Broadcast(string.Format(BroadcastKickMessage, player.Name.ToString(), reason));

            player.Kick(reason);
            return string.Format(GetMsg("{0} was kicked from the server ({1})", source is IPlayer ? ((IPlayer)source).Id.ToString() : null), player.Name.ToString(), reason);
        }

        ////////////////////////////////////////////////////////////
        // Bans
        ////////////////////////////////////////////////////////////

        string MultipleBans(object source, List<string> steamids, string reason, int duration)
        {
            var answer = string.Empty;
            foreach (string userid in steamids) { answer += Ban(source, userid, reason, duration) + "\r\n"; }
            return answer;
        }
        string Ban(object source, string steamid, string reason, int duration)
        {
            var playerdatacompiled = GetPlayerData(steamid);
            string answermsg = string.Empty;

            string name = playerdatacompiled["name"] as string;

            List<string> ips = playerdatacompiled["IPs"] as List<string>;

            if (Bansystem == 0 || Bansystem == 2)
            {

                webrequest.EnqueueGet(FormatOnlineBansystem(OnlineWebRequestBanLine, new Dictionary<string, string> { { "{steamid}", steamid }, { "{name}", name }, { "{ip}", ips.Count > 0 ? ips[ips.Count - 1] : string.Empty }, { "{reason}", reason }, { "{source}", source is IPlayer ? ((IPlayer)source).Name.ToString() : "Console" }, { "{expiration}", (duration + Convert.ToInt32(LogTime())).ToString() } }), (code, response) =>
                {
                    if (response != null || code != 200)
                    {
                        if (source is IPlayer) ((IPlayer)source).Reply(response);
                        else Interface.Oxide.LogInfo(response);
                    }
                }, this);
            }

            if (Bansystem == 0 || Bansystem == 1)
            {
                if (banLogs[steamid] != null && duration == 0)
                {
                    answermsg = string.Format(GetMsg("{0} - {1} is already banned.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null), steamid, name);
                }
                else
                {
                    foreach (string ip in ips)
                    {
                        if (ip != string.Empty && ip.Length > 5)
                        {
                            if (banLogs[ip] != null)
                            {
                                Unban(null, ip);
                            }
                        }
                    }
                    answermsg = ExecuteBan(source, steamid, steamid, name, ips, reason, duration);
                }
            }
            Kick(source, ips.Count != 0 ? ips[ips.Count - 1] : steamid, reason, true);
            return answermsg;
        }

        string ExecuteBan(object source, string key, string steamid, string name, List<string> ips, string reason, int duration)
        {
            banLogs[key] = new BanData(steamid, reason, name, ips, duration, source is IPlayer ? ((IPlayer)source).Name.ToString() : "Console");
            storedData.BanLogs.Add(banLogs[key]);
            UpdateBannedIPs();

            return duration == 0 ? string.Format(GetMsg("{0} {1} was successfully banned from the server ({2})", source is IPlayer ? ((IPlayer)source).Id.ToString() : null), steamid == string.Empty ? ips[0] : steamid, steamid == string.Empty ? string.Empty : name, reason) : string.Format(GetMsg("{0} {1} was successfully tempbanned from the server ({2}) for {3} secs", source is IPlayer ? ((IPlayer)source).Id.ToString() : null), steamid == string.Empty ? ips[0] : steamid, steamid == string.Empty ? string.Empty : name, reason, duration.ToString());
        }

        string BanIP(object source, string ip, string reason, int duration)
        {
            string answermsg = string.Empty;

            if (Bansystem == 0 || Bansystem == 2)
            {
                webrequest.EnqueueGet(FormatOnlineBansystem(OnlineWebRequestBanLine, new Dictionary<string, string> { { "{steamid}", string.Empty }, { "{name}", string.Empty }, { "{ip}", ip }, { "{reason}", reason }, { "{source}", source is IPlayer ? ((IPlayer)source).Name.ToString() : "Console" }, { "{expiration}", (duration + Convert.ToInt32(LogTime())).ToString() } }), (code, response) =>
                {
                    if (response != null || code != 200)
                    {
                        if (source is IPlayer) ((IPlayer)source).Reply(response);
                        else Interface.Oxide.LogInfo(response);
                    }
                }, this);
            }
            if (Bansystem == 0 || Bansystem == 1)
            {
                if (bannedIPs.ContainsKey(ip))
                {
                    answermsg = GetMsg("This IP is already banned.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null);
                }
                else
                {
                    answermsg = ExecuteBan(source, ip, string.Empty, string.Empty, new List<string> { ip }, reason, duration);
                }
            }
            Kick(source, ip, reason, true);
            return answermsg;
        }

        bool isBanned(string steamid, string name, string ip, out string reason, out int expiration)
        {
            reason = string.Empty;
            expiration = -1;
            if (steamid != string.Empty && steamid != "0" && banLogs[steamid] != null)
            {
                reason = banLogs[steamid].reason;
                expiration = banLogs[steamid].expiration;
                if (LogTime() >= expiration && expiration != 0)
                {
                    Interface.Oxide.LogInfo(string.Format("[Enhanced Ban System] {0} ban expired", steamid));
                    Unban(null, steamid);
                    return false;
                }
                if (ip != string.Empty && !banLogs[steamid].IPs.Contains(ip))
                {
                    storedData.BanLogs.Remove(banLogs[steamid]);
                    banLogs[steamid].IPs.Add(ip);
                    storedData.BanLogs.Add(banLogs[steamid]);
                    Interface.Oxide.LogInfo(string.Format("[Enhanced Ban System] {0} was added to the ban of {1}", ip, steamid));
                }
                return true;
            }

            if (ip != string.Empty && bannedIPs.ContainsKey(ip))
            {
                foreach (var bandata in bannedIPs[ip])
                {
                    if (bandata.reason != string.Empty)
                        reason = bandata.reason;
                    if (bandata.expiration == 0)
                        expiration = bandata.expiration;
                    else if (bandata.expiration != 0 && expiration != 0)
                        expiration = bandata.expiration;
                }
                if (expiration == -1) expiration = 0;
                int timeleft = expiration - Convert.ToInt32(LogTime());
                Unban(null, ip);
                if (expiration != 0 && timeleft <= 0) { Interface.Oxide.LogInfo(string.Format("[Enhanced Ban System] {0} - {1} ban expired", ip, steamid)); return false; }
                ExecuteBan(null, steamid, steamid, name, new List<string> { ip }, reason, expiration == 0 ? 0 : timeleft);
                Interface.Oxide.LogInfo(string.Format("[Enhanced Ban System] {0} ban was updated to match {1} steamid", ip, steamid));
                return true;
            }
            return false;
        }

        string TryBan(object source, string[] args)
        {
            string ipaddress = regex.IsMatch(args[0]) ? args[0] : string.Empty;
            string steamid = string.Empty;
            string name = string.Empty;

            string reason = args.Length > 1 ? args[1] : BanDefaultReason;
            int duration = 0;
            if (args.Length > 2) int.TryParse(args[2], out duration);

            var foundplayers = FindPlayers(args[0]);
            if (ipaddress != string.Empty)
            {
                if (foundplayers.Count == 0) { return BanIP(source, ipaddress, reason, duration); }
                else { return MultipleBans(source, foundplayers, reason, duration); }
            }
            else
            {
                if (foundplayers.Count == 0) return GetMsg("No player matching this name was found.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null);
                if (foundplayers.Count > 1) { var returnstring = string.Empty; foreach (string userid in foundplayers) { returnstring += string.Format("{0} {1}\r\n", userid, UserIDToName(userid)); } return returnstring; }
                return Ban(source, foundplayers[0], reason, duration);
            }
        }


        ////////////////////////////////////////////////////////////
        // Unbans
        ////////////////////////////////////////////////////////////

        string Unban(object source, string arg)
        {
            var returnstring = string.Empty;
            ulong steamid = 0L;
            var searchkey = arg;
            string ip = regex.IsMatch(arg) ? arg : string.Empty;
            if (arg.Length == 17) ulong.TryParse(arg, out steamid);

            if (Bansystem == 0 || Bansystem == 1)
            {
                bool canBan = false;
                if (ip != string.Empty) { if (!bannedIPs.ContainsKey(arg)) { returnstring = GetMsg("No bans matching this ip.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null); } else { canBan = true; } }
                else if (steamid != 0L) { if (banLogs[arg] == null) { returnstring = GetMsg("No bans matching this steamid.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null); } else canBan = true; }
                else
                {
                    var lowername = arg.ToLower();
                    var foundPlayers = new List<string>();

                    foreach (KeyValuePair<string, BanData> bandata in banLogs) { if (bandata.Value.name.ToLower().Contains(lowername)) foundPlayers.Add(bandata.Value.steamID); }

                    if (foundPlayers.Count == 0) returnstring = GetMsg("No bans matching this name.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null);
                    else if (foundPlayers.Count > 1)
                    {
                        returnstring += GetMsg("Multiple bans matching this name:\r\n", source is IPlayer ? ((IPlayer)source).Id.ToString() : null);
                        foreach (var userid in foundPlayers) { returnstring += string.Format("{0} - {1} - {2}\r\n", userid, banLogs[userid].name, banLogs[userid].reason); }
                    }
                    else
                    {
                        canBan = true;
                        searchkey = foundPlayers[0];
                    }
                }
                if (canBan)
                {
                    var unbanList = new List<string>();
                    unbanList.Add(searchkey);
                    for (int i = 0; ; i++)
                    {
                        if (i >= unbanList.Count) break;
                        foreach (var match in FindMultipleBans(unbanList[i]))
                        {
                            if (!unbanList.Contains(match))
                                unbanList.Add(match);
                        }
                    }

                    foreach (var key in unbanList) { ExecuteUnban(key); }
                    UpdateBannedIPs();
                    returnstring = string.Format(GetMsg("{0} {1} was successfully unbanned.", source is IPlayer ? ((IPlayer)source).Id.ToString() : null), searchkey, string.Empty);
                }
            }
            if (Bansystem == 0 || Bansystem == 2)
            {
                webrequest.EnqueueGet(FormatOnlineBansystem(OnlineWebRequestUnbanLine, new Dictionary<string, string> { { "{steamid}", steamid != 0L ? steamid.ToString() : string.Empty }, { "{name}", (steamid == 0L && ip == string.Empty) ? arg : string.Empty }, { "{ip}", ip }, { "{source}", source is IPlayer ? ((IPlayer)source).Id.ToString() : "0" } }), (code, response) =>
                {
                    if (response != null || code != 200)
                    {
                        if (source is IPlayer) ((IPlayer)source).Reply(response);
                        else Interface.Oxide.LogInfo(response);
                    }
                }, this);
            }
            
            return returnstring;
        }

        void ExecuteUnban(string key)
        {
            if (banLogs[key] != null)
            {
                storedData.BanLogs.Remove(banLogs[key]);
                banLogs.Remove(key);
            }
        }

        List<string> FindMultipleBans(string arg)
        {
            var returnlist = new List<string>();
            if (banLogs[arg] != null)
            {
                foreach (var ip in banLogs[arg].IPs)
                {
                    if (!returnlist.Contains(ip))
                        returnlist.Add(ip);
                }
            }
            if (bannedIPs.ContainsKey(arg))
            {
                foreach (var key in bannedIPs[arg])
                {
                    if (!returnlist.Contains(key.steamID))
                        returnlist.Add(key.steamID);
                }
            }
            return returnlist;
        }

        ////////////////////////////////////////////////////////////
        // Banlist
        ////////////////////////////////////////////////////////////

        string Banlist(int startid)
        {
            string returnmsg = string.Empty;
            if (Bansystem == 0 || Bansystem == 2)
            {
                returnmsg += OnlineWebRequestBanlist;
            }
            if (Bansystem == 0 || Bansystem == 1)
            {
                returnmsg += string.Format("Banlist - {0}/{1}\r\n", startid.ToString(), banLogs.Count.ToString());
                int i = 0;
                foreach (KeyValuePair<string, BanData> pair in banLogs)
                {
                    if (i >= startid)
                    {
                        returnmsg += string.Format("{0} - {1} {2} banned for {3} - expires {4}\r\n", i.ToString(), pair.Key, pair.Value.name, pair.Value.reason, pair.Value.expiration.ToString());
                    }
                    if (i > startid + 10) break;
                    i++;
                }
            }
            return returnmsg;
        }

        ////////////////////////////////////////////////////////////
        // Chat commands
        ////////////////////////////////////////////////////////////

        [Command("ban")]
        void cmdBan(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: ban < Name | SteamID | IP > < reason(optional) > < time in secondes(optional > ", player.Id.ToString())); return; }
            try { player.Reply(TryBan(player, args)); } catch { }
        }
        [Command("banlist")]
        void cmdBanlist(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBanlist)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            int startid = 0;
            if (args != null && (args.Length > 0)) { int.TryParse(args[0], out startid); }
            try { player.Reply(Banlist(startid)); } catch { }
        }
        [Command("kick")]
        void cmdKick(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionKick)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: kick <Name|SteamID|IP> <reason(optional)>", player.Id.ToString())); return; }
            try { player.Reply(Kick(player, args)); } catch { }
        }
        [Command("unban")]
        void cmdUnban(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionUnban)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: unban <Name|SteamID|IP>", player.Id.ToString())); return; }
            try { player.Reply(Unban(player, args[0])); } catch { }
        }

        ////////////////////////////////////////////////////////////
        // Console commands
        ////////////////////////////////////////////////////////////

        [Command("player.ban")]
        void ccmdBan(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: player.ban < Name | SteamID | IP > < reason(optional) > < time in secondes(optional > ", player.Id.ToString())); return; }
            try { player.Reply(TryBan(player, args)); } catch { }
        }
        [Command("player.banlist")]
        void ccmdBanlist(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBanlist)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            int startid = 0;
            if (args != null && (args.Length > 0)) { int.TryParse(args[0], out startid); }
            try { player.Reply(Banlist(startid)); } catch { }
        }
        [Command("player.kick")]
        void ccmdKick(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionKick)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: kick <Name|SteamID|IP> <reason(optional)>", player.Id.ToString())); return; }
            try { player.Reply(Kick(player, args)); } catch { }
        }
        [Command("player.unban")]
        void ccmdUnban(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionUnban)) { player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString())); return; }
            if (args == null || (args.Length < 1)) { player.Reply(GetMsg("Syntax: player.unban <Name|SteamID|IP>", player.Id.ToString())); return; }
            try { player.Reply(Unban(player, args[0])); } catch { }
        }
    }
}
