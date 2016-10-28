using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Ext.MySql;
using Oxide.Ext.SQLite;
using Oxide.Core.Configuration;
using Oxide.Core.Database;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("EnhancedBanSystem", "Reneb", "5.0.8", ResourceId = 1951)]
    class EnhancedBanSystem : CovalencePlugin
    {
        [PluginReference]
        Plugin PlayerDatabase;

        ////////////////////////////////////////////////////////////
        // Static fields
        ////////////////////////////////////////////////////////////
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        char[] ipChrArray = new char[] { '.' };

        static BanSystem banSystem;

        static Hash<int, BanData> cachedBans = new Hash<int, BanData>();

        static List<int> wasBanned = new List<int>();

        ////////////////////////////////////////////////////////////
        // Config fields
        ////////////////////////////////////////////////////////////

        static string Platform = "Steam";
        static string Server = "1.1.1.1:28015";
        static string Game = "Rust";

        string PermissionBan = "enhancedbansystem.ban";
        string PermissionUnban = "enhancedbansystem.unban";
        string PermissionBanlist = "enhancedbansystem.banlist";
        string PermissionKick = "enhancedbansystem.kick";

        static bool SQLite_use = false;
        static string SQLite_DB = "banlist.db";

        static bool MySQL_use = false;
        static string MySQL_Host = "localhost";
        static int MySQL_Port = 3306;
        static string MySQL_DB = "banlist";
        static string MySQL_User = "root";
        static string MySQL_Pass = "toor";

        static bool PlayerDatabase_use = false;
        static string PlayerDatabase_IPFile = "EnhancedBanSystem_IPs.json";

        static bool Files_use = false;

        static bool WebAPI_use = false;
        static string WebAPI_Ban_Request = "http://webpage.com/api.php?action=ban&pass=mypassword&id={id}&steamid={steamid}&name={name}&ip={ip}&reason={reason}&source={source}&game={game}&platform={platform}&server={server}&tempban={expiration}";
        static string WebAPI_Unban_Request = "http://webpage.com/api.php?action=unban&pass=mypassword&steamid={steamid}&name={name}&ip={ip}&name={name}&source={source}";
        static string WebAPI_IsBanned_Request = "http://webpage.com/api.php?action=isbanned&pass=mypassword&id={id}&update={update}&steamid={steamid}&ip={ip}&time={time}&name={name}&game=Rust&server=rust.kortal.org:28015";
        static string WebAPI_Banlist_Request = "http://webpage.com/banlist.php?startid={startid}";

        static bool Native_use = false;

        static string BanDefaultReason = "Banned";
        static string BanEvadeReason = "Ban Evade";

        static bool Kick_Broadcast = true;
        static bool Kick_Log = true;
        static bool Kick_OnBan = true;

        static bool Ban_Broadcast = true;
        static bool Ban_Log = true;

        static bool Ban_Escape = true;

        static bool Log_Denied = true;

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
            CheckCfg<string>("Server Info - Platform", ref Platform);
            CheckCfg<string>("Server Info - Game", ref Game);
            CheckCfg<string>("Server Info - IP:PORT", ref Server);

            CheckCfg<string>("Permissions - Ban", ref PermissionBan);
            CheckCfg<string>("Permissions - Unban", ref PermissionUnban);
            CheckCfg<string>("Permissions - Banlist", ref PermissionBanlist);
            CheckCfg<string>("Permissions - Kick", ref PermissionKick);

            CheckCfg<bool>("DataType - SQLite - use", ref SQLite_use);
            CheckCfg<string>("DataType - SQLite - Database Filename", ref SQLite_DB);

            CheckCfg<bool>("DataType - MySQL - use", ref MySQL_use);
            CheckCfg<string>("DataType - MySQL - Host", ref MySQL_Host);
            CheckCfg<int>("DataType - MySQL - Port", ref MySQL_Port);
            CheckCfg<string>("DataType - MySQL - Database", ref MySQL_DB);
            CheckCfg<string>("DataType - MySQL - User", ref MySQL_User);
            CheckCfg<string>("DataType - MySQL - Pass", ref MySQL_Pass);

            CheckCfg<bool>("DataType - Files - use", ref Files_use);

            CheckCfg<bool>("DataType - PlayerDatabase - use", ref PlayerDatabase_use);
            CheckCfg<string>("DataType - PlayerDatabase - IP Filename", ref PlayerDatabase_IPFile);

            CheckCfg<bool>("DataType - WebAPI - use", ref WebAPI_use);
            CheckCfg<string>("DataType - WebAPI - Host", ref WebAPI_Ban_Request);
            CheckCfg<string>("DataType - WebAPI - Unban", ref WebAPI_Unban_Request);
            CheckCfg<string>("DataType - WebAPI - IsBanned", ref WebAPI_IsBanned_Request);
            CheckCfg<string>("DataType - WebAPI - Banlist", ref WebAPI_Banlist_Request);

            CheckCfg<bool>("DataType - Native - use", ref Native_use);

            CheckCfg<bool>("Ban - Evade", ref Ban_Escape);
            CheckCfg<string>("Ban - Default Reason", ref BanDefaultReason);
            CheckCfg<string>("Ban - Evade Reason", ref BanEvadeReason);
            CheckCfg<bool>("Ban - Broadcast", ref Ban_Broadcast);
            CheckCfg<bool>("Ban - Log", ref Ban_Log);

            CheckCfg<bool>("Kick - Broadcast", ref Kick_Broadcast);
            CheckCfg<bool>("Kick - Log", ref Kick_Log);
            CheckCfg<bool>("Kick - On Ban", ref Kick_OnBan);

            CheckCfg<bool>("Denied Connection - Log", ref Log_Denied);

            SaveConfig();

            if (SQLite_use) banSystem |= BanSystem.SQLite;
            if (MySQL_use) banSystem |= BanSystem.MySQL;
            if (Native_use) banSystem |= BanSystem.Native;
            if (PlayerDatabase_use) banSystem |= BanSystem.PlayerDatabase;
            if (Files_use) banSystem |= BanSystem.Files;
            if (WebAPI_use) banSystem |= BanSystem.WebAPI;

            InitializeLang();
        }

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No matching player was found.\n", "No matching player was found.\n" },
                { "You are temporarily banned from this server ({0}). {1} left", "You are temporarily banned from this server ({0}). {1} left" },
                { "You are banned from this server ({0}).", "You are banned from this server ({0})." },
                {"Loaded {0} bans\n\r","Loaded {0} bans\n\r" },
                {"This ban already exists ({0}).","This ban already exists ({0})." },
                {"Successfully added {0} to the banlist.","Successfully added {0} to the banlist." },
                {"Multiple Bans Found:\n\r","Multiple Bans Found:\n\r" },
                {"{0} matching bans were removed","{0} matching bans were removed" },
                {"{0} - {1} isn't banned.\n","{0} - {1} isn't banned.\n" },
                {"Loaded\n\r","Loaded\n\r" },
                {"You don't have the permission to use this command.","You don't have the permission to use this command." },
                {"Syntax: kick < Name | SteamID | IP | IP Range > < reason(optional) >","Syntax: kick < Name | SteamID | IP | IP Range > < reason(optional) >" },
                {"Syntax: unban < Name | SteamID | IP | IP Range >","Syntax: unban < Name | SteamID | IP | IP Range >" },
                {"Syntax: ban < Name | SteamID | IP | IP Range > < reason(optional) > < time in secondes(optional) > ","Syntax: ban < Name | SteamID | IP | IP Range > < reason(optional) > < time in secondes(optional) > " },
                {"Syntax: banlist <BanSystem> <startid>","Syntax: banlist <BanSystem> <startid>" },
                {"Avaible BanSystems:\n","Avaible BanSystems:\n" },
                {"Wrong usage of /banlist","Wrong usage of /banlist" },
                {"Index is out of range. Current bans recorded: {0}","Index is out of range. Current bans recorded: {0}" },
                {"Banlist - {0}-{1}/{2}\n","Banlist - {0}-{1}/{2}\n" }
            }, this);
        }

        ////////////////////////////////////////////////////////////
        // ID Save
        ////////////////////////////////////////////////////////////

        static DynamicConfigFile Ban_ID_File;
        static int Ban_ID = 0;

        void Load_ID()
        {
            try
            {
                Ban_ID_File = Interface.GetMod().DataFileSystem.GetDatafile("EnhancedBanSystem_ID");
                Ban_ID = (int)Ban_ID_File["id"];
            }
            catch
            {
                Ban_ID = 0;
                Ban_ID_File["id"] = Ban_ID;
                Save_ID();
            }
        }

        void Save_ID()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("EnhancedBanSystem_ID");
        }

        static int GetNewID()
        {
            Ban_ID++;
            Ban_ID_File["id"] = Ban_ID;
            return Ban_ID;
        }

        ////////////////////////////////////////////////////////////
        // Enum & Class
        ////////////////////////////////////////////////////////////

        enum BanSystem
        {
            Native = 1,
            MySQL = 2,
            SQLite = 4,
            WebAPI = 8,
            PlayerDatabase = 16,
            Files = 32,
        }

        class BanData
        {
            public int id;
            public string steamid;
            public string ip;
            public string name;
            public string game;
            public string server;
            public string source;
            public double date;
            public double expire;
            public string reason;
            public string platform;

            public BanData() { }

            public BanData(object source, string userID, string name, string ip, string reason, double duration)
            {
                this.id = GetNewID();
                this.source = source is IPlayer ? ((IPlayer)source).Name : source is string ? (string)source : "Console";
                this.steamid = userID;
                this.name = name;
                this.ip = ip;
                this.reason = reason;
                this.expire = duration != 0.0 ? LogTime() + duration : 0.0;
                this.date = LogTime();
                this.platform = Platform;
                this.game = Game;
                this.server = Server;
            }

            public BanData(int id, string source, string userID, string name, string ip, string reason, string duration)
            {
                this.id = id;
                this.source = source;
                this.steamid = userID;
                this.name = name;
                this.ip = ip;
                this.reason = reason;
                this.expire = double.Parse(duration);
                this.date = LogTime();
                this.platform = Platform;
                this.game = Game;
                this.server = Server;
            }

            public string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }

            public override string ToString()
            {
                return string.Format("{0} - {1} - {2} - {3} - {4}", steamid, name, ip, reason, expire == 0.0 ? "Permanent" : expire < LogTime() ? "Expired" : string.Format("Temporary: {0}s", (expire - LogTime()).ToString()));
            }
        }

        ////////////////////////////////////////////////////////////
        // General Methods
        ////////////////////////////////////////////////////////////

        static double LogTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        string GetMsg(string key, object steamid = null) { return lang.GetMessage(key, this, steamid is IPlayer ? ((IPlayer)steamid).Id : steamid == null ? null : steamid.ToString()); }
        //string GetMsg(string key, object steamid = null) { return key; }

        bool hasPermission(IPlayer player, string permissionName)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.Id.ToString(), permissionName);
        }

        bool isIPAddress(string arg)
        {
            int subIP;
            string[] strArray = arg.Split(ipChrArray);
            if (strArray.Length != 4)
            {
                return false;
            }
            foreach (string str in strArray)
            {
                if (str.Length == 0)
                {
                    return false;
                }
                if (!int.TryParse(str, out subIP) && str != "*")
                {
                    return false;
                }
                if (!(str == "*" || (subIP >= 0 && subIP <= 255)))
                {
                    return false;
                }
            }
            return true;
        }

        bool IPRange(string sourceIP, string targetIP)
        {
            string[] srcArray = sourceIP.Split(ipChrArray);
            string[] trgArray = targetIP.Split(ipChrArray);
            for (int i = 0; i < 4; i++)
            {
                if (srcArray[i] != trgArray[i] && srcArray[i] != "*")
                {
                    return false;
                }
            }
            return true;
        }

        bool RangeFromIP(string sourceIP, out string range1, out string range2, out string range3)
        {
            range1 = string.Empty;
            range2 = string.Empty;
            range3 = string.Empty;
            if (sourceIP == string.Empty) return false;

            string[] strArray = sourceIP.Split(ipChrArray);
            if (strArray.Length != 4)
            {
                return false;
            }

            range1 = string.Format("{0}.*.*.*", strArray[0]);
            range2 = string.Format("{0}.{1}.*.*", strArray[0], strArray[1]);
            range3 = string.Format("{0}.{1}.{2}.*", strArray[0], strArray[1], strArray[2]);

            return true;
        }

        List<IPlayer> FindPlayers(string userIDorNameorIP, object source, out string reason)
        {
            reason = string.Empty;
            var FoundPlayers = players.FindPlayers(userIDorNameorIP).ToList();
            if (FoundPlayers.Count == 0)
            {
                reason = GetMsg("No matching player was found.\n", source);
            }
            if (FoundPlayers.Count > 1)
            {
                foreach (var iplayer in FoundPlayers)
                {
                    reason += string.Format("{0} {1}\r\n", iplayer.Id, iplayer.Name);
                }
            }
            return FoundPlayers;
        }

        List<IPlayer> FindConnectedPlayers(string userIDorNameorIP, object source, out string reason)
        {
            reason = string.Empty;
            ulong steamid = 0L;
            var FoundPlayers = new List<IPlayer>();
            ulong.TryParse(userIDorNameorIP, out steamid);
            if (isIPAddress(userIDorNameorIP))
            {
                FoundPlayers = players.All.Where(x => x.IsConnected).Where(w => IPRange(userIDorNameorIP, w.Address)).ToList();
            }
            else if (steamid != 0L)
            {
                var p = players.FindPlayer(userIDorNameorIP);
                if (p != null && p.IsConnected)
                {
                    FoundPlayers.Add(p);
                }
            }
            else
            {
                FoundPlayers = players.FindPlayers(userIDorNameorIP).Where(x => x.IsConnected).ToList();
                if (FoundPlayers.Count > 1)
                {
                    foreach (var iplayer in FoundPlayers)
                    {
                        reason += string.Format("{0} {1}\r\n", iplayer.Id, iplayer.Name);
                    }
                }
            }
            if (FoundPlayers.Count == 0)
            {
                reason = GetMsg("No matching player was found.\n", source);
            }
            return FoundPlayers;
        }

        string GetPlayerIP(IPlayer iplayer)
        {
            if (iplayer.IsConnected) return iplayer.Address;
            return GetPlayerIP(iplayer.Id);
        }

        string GetPlayerIP(string userID)
        {
            if (PlayerDatabase != null)
            {
                return (string)PlayerDatabase.Call("GetPlayerData", userID, "ip") ?? string.Empty;
            }
            return string.Empty;
        }

        string GetPlayerName(string userID)
        {
            if (PlayerDatabase != null)
            {
                return (string)PlayerDatabase.Call("GetPlayerData", userID, "name") ?? string.Empty;
            }
            return string.Empty;
        }

        string FormatTimeLeft(double timeleft)
        {
            var d = 0.0;
            var h = 0.0;
            var m = 0.0;
            var s = 0.0;
            if (timeleft >= 86400.0) { d = Math.Floor(timeleft / 86400.0); timeleft -= d * 86400.0; }
            if (timeleft >= 3600) { h = Math.Floor(timeleft / 3600.0); timeleft -= h * 3600.0; }
            if (timeleft >= 60) { m = Math.Floor(timeleft / 60.0); timeleft -= m * 60.0; }
            if (timeleft >= 0) { s = Math.Floor(timeleft); }

            return string.Format("{0}{1}{2}{3}", d > 0 ? $"{d.ToString()}d " : string.Empty, h > 0 ? $"{h.ToString()}h " : string.Empty, m > 0 ? $"{m.ToString()}m " : string.Empty, s > 0 ? $"{s.ToString()}s " : string.Empty);
        }

        bool HasDelayedAnswer() => BanSystemHasFlag(banSystem, BanSystem.MySQL) || BanSystemHasFlag(banSystem, BanSystem.SQLite) || (BanSystemHasFlag(banSystem, BanSystem.WebAPI));

        bool BanSystemHasFlag(BanSystem b, BanSystem t) => (b & t) == t;

        string FormatReturn(BanSystem system, string msg, params object[] args) => string.Format("{0}: {1}", system.ToString(), string.Format(msg, args));

        void SendReply(object source, string msg)
        {
            if (source is IPlayer) ((IPlayer)source).Reply(msg);
            else if (source is string) return;
            else Interface.Oxide.LogInfo(msg);
        }

        public static string ToShortString(TimeSpan timeSpan)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
        }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////


        void OnServerInitialized()
        {
            Load_ID();
            string returnstring = string.Empty;

            if (BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
            {
                returnstring += PlayerDatabase_Load();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Files))
            {
                returnstring += Files_Load();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
            {
                returnstring += MySQL_Load();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
            {
                returnstring += SQLite_Load();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
            {
                returnstring += WebAPI_Load();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Native))
            {
                returnstring += Native_Load();
            }

            permission.RegisterPermission(PermissionBan, this);
            permission.RegisterPermission(PermissionBanlist, this);
            permission.RegisterPermission(PermissionKick, this);
            permission.RegisterPermission(PermissionUnban, this);

            Interface.Oxide.LogInfo(returnstring);
        }


        void OnServerSave()
        {
            Save_ID();
            if (BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
            {
                Save_PlayerDatabaseIP();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Files))
            {
                Save_Files();
            }
            if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
            {
                // Nothing to save
            }
            if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
            {
                // Nothing to save
            }
            if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
            {
                // Nothing to save
            }
        }

        object CanUserLogin(string name, string steamid, string ip)
        {
            BanData bd = null;
#if RUST
            using (TimeWarning.New("CanUserLogin", 0.01f))
            {
#endif
            if (isBanned_NonDelayed(name, steamid, ip, Ban_Escape, out bd))
            {
                if (bd != null && bd.expire != 0.0)
                {
                    return string.Format(GetMsg("You are temporarily banned from this server ({0}). {1} left", steamid), bd.reason, FormatTimeLeft(bd.expire - LogTime()));
                }
                return string.Format(GetMsg("You are banned from this server ({0}).", steamid), bd == null ? string.Empty : bd.reason);
            }
#if RUST
            }
#endif
            return null;
        }

        void OnUserConnected(IPlayer player)
        {
#if RUST
            using (TimeWarning.New("OnUserConnected", 0.01f))
            {
#endif
            if (player == null) return;
            string ip = player.Address;
            string name = player.Name;
            string steamid = player.Id;

            isBanned_Delayed(name, steamid, ip, Ban_Escape);
#if RUST
            }
#endif
        }

        ////////////////////////////////////////////////////////////
        // Files
        ////////////////////////////////////////////////////////////

        static StoredData storedData;

        class StoredData
        {
            public HashSet<string> Banlist = new HashSet<string>();

            public StoredData() { }
        }

        string Files_Load()
        {
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("EnhancedBanSystem");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var b in storedData.Banlist)
            {
                var bd = JsonConvert.DeserializeObject<BanData>(b);
                if (!cachedBans.ContainsKey(bd.id))
                    cachedBans.Add(bd.id, bd);
            }
            return FormatReturn(BanSystem.Files, GetMsg("Loaded {0} bans\n\r", null), storedData.Banlist.Count.ToString());
        }

        void Save_Files()
        {
            Interface.GetMod().DataFileSystem.WriteObject("EnhancedBanSystem", storedData);
        }

        string Files_UpdateBan(BanData bandata)
        {
            if (!cachedBans.ContainsKey(bandata.id)) return FormatReturn(BanSystem.Files, "No such ban id {0}", bandata.id);

            storedData.Banlist.Remove(cachedBans[bandata.id].ToJson());
            cachedBans.Remove(bandata.id);

            storedData.Banlist.Add(bandata.ToJson());

            if (!cachedBans.ContainsKey(bandata.id))
                cachedBans.Add(bandata.id, bandata);

            return FormatReturn(BanSystem.Files, GetMsg("Successfully updated {0} in the banlist."), bandata.ToString());
        }

        string Files_ExecuteBan(BanData bandata)
        {

            var f = cachedBans.Values.Where(x => x.ip == bandata.ip).Where(x => x.steamid == bandata.steamid).ToList();
            if (f.Count > 0)
            {
                return FormatReturn(BanSystem.Files, GetMsg("This ban already exists ({0})."), f[0].ToString());
            }
            storedData.Banlist.Add(bandata.ToJson());

            if (!cachedBans.ContainsKey(bandata.id))
                cachedBans.Add(bandata.id, bandata);

            return FormatReturn(BanSystem.Files, GetMsg("Successfully added {0} to the banlist."), bandata.ToString());
        }

        string Files_RawUnban(List<BanData> unbanList)
        {
            int i = 0;
            foreach (var u in unbanList)
            {
                var json = u.ToJson();
                if (storedData.Banlist.Contains(json))
                {
                    i++;
                    storedData.Banlist.Remove(json);
                }
            }
            return FormatReturn(BanSystem.Files, GetMsg("{0} matching bans were removed"), i.ToString());
        }

        string Files_ExecuteUnban(string steamid, string name, string ip, out List<BanData> unbanList)
        {
            unbanList = new List<BanData>();
            if (ip != string.Empty)
            {
                unbanList = cachedBans.Values.Where(x => x.ip == ip).ToList();
            }
            else if (steamid != string.Empty)
            {
                unbanList = cachedBans.Values.Where(x => x.steamid == steamid).ToList();
            }
            else
            {
                unbanList = cachedBans.Values.Where(x => x.name == name).ToList();
                if (unbanList.Count == 0)
                {
                    var lname = name.ToLower();
                    unbanList = cachedBans.Values.Where(x => x.name.ToLower().Contains(lname)).ToList();
                    if (unbanList.Count > 1)
                    {
                        var ret = FormatReturn(BanSystem.Files, GetMsg("Multiple Bans Found:\n\r"));
                        foreach (var b in unbanList)
                        {
                            ret += string.Format("{0} - {1} - {2}\n\r", b.steamid, b.name, b.reason);
                        }
                        return ret;
                    }
                }
            }
            return Files_RawUnban(unbanList);
        }

        bool Files_IsBanned(string steamid, string ip, out BanData bandata)
        {
            bandata = null;
            double cTime = LogTime();
            bool permanent = false;
            List<BanData> unbanList = new List<BanData>();
            List<BanData> list = new List<BanData>();
            {
                // check by ID then IP
                var b_steamid = cachedBans.Values.Where(x => x.steamid == steamid).ToList();
                var b_steamid_ip = b_steamid.Where(w => w.ip == ip).ToList();

                foreach (var b in b_steamid_ip)
                {
                    if (b.expire != 0.0 && cTime >= b.expire)
                    {
                        unbanList.Add(b);
                    }
                    else
                    {
                        if (b.expire == 0.0)
                        {
                            permanent = true;
                        }
                        if (b.ip == ip)
                        {
                            bandata = b;
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    Files_RawUnban(unbanList);
                    foreach (var u in unbanList)
                    {
                        b_steamid_ip.Remove(u);
                    }
                    unbanList.Clear();
                }
                foreach (var b in b_steamid)
                {
                    if (b.expire != 0.0 && cTime >= b.expire)
                    {
                        unbanList.Add(b);
                    }
                    else
                    {
                        if (b.expire == 0.0)
                        {
                            permanent = true;
                        }
                        if (b.ip == ip)
                        {
                            bandata = b;
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    Files_RawUnban(unbanList);
                    foreach (var u in unbanList)
                    {
                        b_steamid.Remove(u);
                    }
                    unbanList.Clear();
                }
                if (bandata == null)
                {
                    if (b_steamid_ip.Count > 0)
                    {
                        bandata = b_steamid_ip[0];
                    }
                    else if (b_steamid.Count > 0)
                    {
                        bandata = b_steamid[0];
                    }
                }
            }
            if (bandata == null && !permanent)
            {
                // check by IP & IP Range
                list = cachedBans.Values.Where(x => IPRange(x.ip, ip)).ToList();
                foreach (var b in list)
                {
                    if (b.expire != 0.0 && cTime >= b.expire)
                    {
                        unbanList.Add(b);
                    }
                    else
                    {
                        if (b.expire == 0.0)
                        {
                            permanent = true;
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    Files_RawUnban(unbanList);
                    foreach (var u in unbanList)
                    {
                        list.Remove(u);
                    }
                    unbanList.Clear();
                }
                if (list.Count > 0)
                {
                    bandata = list[0];
                }
            }
            if (bandata != null && bandata.expire != 0.0 && permanent)
            {
                bandata.expire = 0.0;
                Files_UpdateBan(bandata);
            }
            return bandata != null;
        }

        string Files_Banlist(object source, int startid)
        {
            if (startid > cachedBans.Count)
            {
                return FormatReturn(BanSystem.Files, "Index is out of range. Current bans recorded: {0}", cachedBans.Count.ToString());
            }

            int i = -1;
            int max = startid + 9;

            string returnstring = FormatReturn(BanSystem.Files, GetMsg("Banlist - {0}-{1}/{2}\n"), startid.ToString(), max.ToString(), cachedBans.Count.ToString());


            var bans = from pair in cachedBans orderby pair.Key descending select pair;

            foreach (KeyValuePair<int, BanData> b in bans)
            {
                i++;
                if (i < startid) continue;
                if (i > max) break;
                returnstring += b.ToString() + "\n";

            }

            return returnstring;
        }

        ////////////////////////////////////////////////////////////
        // PlayerDatabase
        ////////////////////////////////////////////////////////////

        static StoredIPData storedIPData;

        class StoredIPData
        {
            public HashSet<string> Banlist = new HashSet<string>();

            public StoredIPData() { }
        }

        string PlayerDatabase_Load()
        {
            if (PlayerDatabase == null) return FormatReturn(BanSystem.PlayerDatabase, "Missing plugin: oxidemod.org/threads/playerdatabase.18409/");
            try
            {
                storedIPData = Interface.GetMod().DataFileSystem.ReadObject<StoredIPData>(PlayerDatabase_IPFile);
            }
            catch
            {
                storedIPData = new StoredIPData();
            }
            foreach (var b in storedIPData.Banlist)
            {
                var bd = JsonConvert.DeserializeObject<BanData>(b);
                if (!cachedBans.ContainsKey(bd.id))
                    cachedBans.Add(bd.id, bd);
            }
            var getKnownPlayers = (List<string>)PlayerDatabase.Call("GetAllKnownPlayers");
            if (getKnownPlayers == null) return FormatReturn(BanSystem.PlayerDatabase, "Error P01");

            int i = 0;
            List<BanData> list = new List<BanData>();
            foreach (var steamid in getKnownPlayers)
            {
                var success = PlayerDatabase.Call("GetPlayerDataRaw", steamid, "Banned");
                if (!(success is string)) continue;
                list = JsonConvert.DeserializeObject<List<BanData>>((string)success);
                foreach (var b in list)
                {
                    i++;
                    if (!cachedBans.ContainsKey(b.id))
                        cachedBans.Add(b.id, b);
                }
            }
            return FormatReturn(BanSystem.PlayerDatabase, GetMsg("Loaded {0} bans\n\r"), i.ToString());
        }

        void Save_PlayerDatabaseIP()
        {
            Interface.GetMod().DataFileSystem.WriteObject(PlayerDatabase_IPFile, storedIPData);
        }


        string PlayerDatabase_ExecuteBan(BanData bandata)
        {
            if (bandata.steamid != string.Empty)
            {
                List<BanData> list = new List<BanData>();
                var success = PlayerDatabase.Call("GetPlayerDataRaw", bandata.steamid, "Banned");
                if (success is string)
                {
                    list = JsonConvert.DeserializeObject<List<BanData>>((string)success);
                }

                var f = list.Where(x => x.ip == bandata.ip).ToList();
                if (f.Count > 0)
                {
                    return FormatReturn(BanSystem.PlayerDatabase, GetMsg("This ban already exists ({0})."), f[0].ToString());
                }
                f.Add(bandata);
                PlayerDatabase.Call("SetPlayerData", bandata.steamid, "Banned", f);
            }
            else
            {
                var f2 = cachedBans.Values.Where(x => x.ip == bandata.ip).Where(x => x.steamid == string.Empty).ToList();
                if (f2.Count > 0)
                {
                    return FormatReturn(BanSystem.PlayerDatabase, GetMsg("This ban already exists ({0})."), f2[0].ToString());
                }
                storedIPData.Banlist.Add(bandata.ToJson());
            }
            if (!cachedBans.ContainsKey(bandata.id))
                cachedBans.Add(bandata.id, bandata);
            return FormatReturn(BanSystem.PlayerDatabase, GetMsg("Successfully added {0} to the banlist."), bandata.ToString());
        }

        string PlayerDatabase_UpdateBan(BanData bandata, double expire)
        {
            if (bandata.steamid == string.Empty)
            {
                if (cachedBans.ContainsKey(bandata.id))
                {
                    var json = bandata.ToJson();
                    if (storedIPData.Banlist.Contains(json))
                    {
                        cachedBans.Remove(bandata.id);
                        storedIPData.Banlist.Remove(json);
                        bandata.expire = expire;
                        storedIPData.Banlist.Add(bandata.ToJson());
                        cachedBans.Add(bandata.id, bandata);
                        return FormatReturn(BanSystem.Files, GetMsg("Successfully updated {0} in the banlist."), bandata.ToString());
                    }
                }
            }
            else
            {
                List<BanData> list = new List<BanData>();
                bandata.expire = expire;
                var b_steamid = PlayerDatabase.Call("GetPlayerDataRaw", bandata.steamid, "Banned");
                if (b_steamid is string)
                {
                    list = JsonConvert.DeserializeObject<List<BanData>>((string)b_steamid);
                }
                if (list.Count > 0)
                {
                    foreach (var b in list)
                    {
                        b.expire = expire;
                    }
                    PlayerDatabase.Call("SetPlayerData", bandata.steamid, "Banned", list);
                    return FormatReturn(BanSystem.Files, GetMsg("Successfully updated {0} in the banlist."), bandata.ToString());
                }
            }
            return string.Empty;
        }

        string PlayerDatabase_RawUnban(List<BanData> unbanList)
        {
            int i = 0;
            foreach (var u in unbanList)
            {
                if (u.steamid == string.Empty)
                {
                    var json = u.ToJson();
                    if (storedIPData.Banlist.Contains(json))
                    {
                        i++;
                        storedIPData.Banlist.Remove(json);
                    }
                }
                else
                {
                    i++;
                    PlayerDatabase.Call("SetPlayerData", u.steamid, "Banned", new List<BanData>());
                }
            }
            return FormatReturn(BanSystem.PlayerDatabase, GetMsg("{0} matching bans were removed"), i.ToString());
        }

        string PlayerDatabase_ExecuteUnban(string steamid, string name, string ip, out List<BanData> unbanList)
        {
            unbanList = new List<BanData>();
            if (ip != string.Empty)
            {
                unbanList = cachedBans.Values.Where(x => x.ip == ip).ToList();
            }
            else if (steamid != string.Empty)
            {
                unbanList = cachedBans.Values.Where(x => x.steamid == steamid).ToList();
            }
            else
            {
                unbanList = cachedBans.Values.Where(x => x.name == name).ToList();
                if (unbanList.Count == 0)
                {
                    var lname = name.ToLower();
                    unbanList = cachedBans.Values.Where(x => x.name.ToLower().Contains(lname)).ToList();
                    if (unbanList.Count > 1)
                    {
                        var ret = FormatReturn(BanSystem.PlayerDatabase, GetMsg("Multiple Bans Found:\n\r"));
                        foreach (var b in unbanList)
                        {
                            ret += string.Format("{0} - {1} - {2}\n\r", b.steamid, b.name, b.reason);
                        }
                        return ret;
                    }
                }
            }
            return PlayerDatabase_RawUnban(unbanList);
        }

        bool PlayerDatabase_IsBanned(string steamid, string ip, out BanData bandata)
        {
            bandata = null;
            BanData possibleData = null;
            double cTime = LogTime();
            bool permanent = false;
            List<BanData> unbanList = new List<BanData>();
            List<BanData> list = new List<BanData>();
            {
                // check by ID then IP
                var b_steamid = PlayerDatabase.Call("GetPlayerDataRaw", steamid, "Banned");
                if (b_steamid is string)
                {
                    list = JsonConvert.DeserializeObject<List<BanData>>((string)b_steamid);
                }
                foreach (var b in list)
                {
                    if (b.expire != 0.0 && cTime >= b.expire)
                    {
                        unbanList.Add(b);
                    }
                    else
                    {
                        if (b.expire == 0.0)
                        {
                            permanent = true;
                        }
                        if (b.ip == ip)
                        {
                            bandata = b;
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    PlayerDatabase_RawUnban(unbanList);
                    foreach (var b in unbanList)
                    {
                        list.Remove(b);
                    }
                    unbanList.Clear();
                }

                if (list.Count > 0 && bandata == null)
                {
                    possibleData = list[0];
                }
            }
            {
                // check by IP & IP Range
                list = cachedBans.Values.Where(x => IPRange(x.ip, ip)).ToList();
                foreach (var b in list)
                {
                    if (b.expire != 0.0 && cTime >= b.expire)
                    {
                        unbanList.Add(b);
                    }
                    else
                    {
                        if (b.expire == 0.0)
                        {
                            permanent = true;
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    PlayerDatabase_RawUnban(unbanList);
                    foreach (var b in unbanList)
                    {
                        list.Remove(b);
                    }
                    unbanList.Clear();
                }
                if (list.Count > 0 && bandata == null && possibleData == null)
                {
                    possibleData = list[0];
                }
            }
            if (bandata == null && possibleData != null)
            {
                bandata = possibleData;
            }
            if (bandata != null && bandata.expire != 0.0 && permanent)
            {
                PlayerDatabase_UpdateBan(bandata, 0.0);
            }
            return bandata != null;
        }

        string PlayerDatabase_Banlist(object source, int startid)
        {
            if (startid > cachedBans.Count)
            {
                return FormatReturn(BanSystem.PlayerDatabase, GetMsg("Index is out of range. Current bans recorded: {0}"), cachedBans.Count.ToString());
            }

            int i = -1;
            int max = startid + 9;

            string returnstring = FormatReturn(BanSystem.PlayerDatabase, GetMsg("Banlist - {0}-{1}/{2}\n"), startid.ToString(), max.ToString(), cachedBans.Count.ToString());


            var bans = from pair in cachedBans orderby pair.Key descending select pair;

            foreach (KeyValuePair<int, BanData> b in bans)
            {
                i++;
                if (i < startid) continue;
                if (i > max) break;
                returnstring += b.ToString() + "\n";

            }

            return returnstring;
        }

        ////////////////////////////////////////////////////////////
        // WebAPI
        ////////////////////////////////////////////////////////////

        string FormatOnlineBansystem(string line, Dictionary<string, string> args)
        {
            foreach (KeyValuePair<string, string> pair in args)
            {
                line = line.Replace(pair.Key, pair.Value);
            }
            return line;
        }

        string WebAPI_ExecuteBan(object source, BanData bandata)
        {
            webrequest.EnqueueGet(FormatOnlineBansystem(WebAPI_Ban_Request, new Dictionary<string, string> { { "{id}", bandata.id.ToString() }, { "{steamid}", bandata.steamid }, { "{name}", bandata.name }, { "{ip}", bandata.ip }, { "{reason}", bandata.reason }, { "{source}", bandata.source }, { "{expiration}", bandata.expire.ToString() }, { "{game}", bandata.game }, { "{platform}", bandata.platform }, { "{server}", bandata.server } }), (code, response) =>
            {
                if (response == null && code == 200)
                {
                    response = FormatReturn(BanSystem.WebAPI, "Couldn't contact the WebAPI");
                }
                if (source is IPlayer) ((IPlayer)source).Reply(response);
                else Interface.Oxide.LogInfo(response);
            }, this);

            return string.Empty;
        }

        string WebAPI_ExecuteUnban(object source, string steamid, string name, string ip)
        {
            webrequest.EnqueueGet(FormatOnlineBansystem(WebAPI_Unban_Request, new Dictionary<string, string> { { "{steamid}", steamid }, { "{name}", name }, { "{ip}", ip } }), (code, response) =>
            {
                if (response == null && code == 200)
                {
                    response = FormatReturn(BanSystem.WebAPI, "Couldn't contact the WebAPI");
                }
                if (source is IPlayer) ((IPlayer)source).Reply(response);
                else Interface.Oxide.LogInfo(response);
            }, this);

            return string.Empty;
        }

        string WebAPI_IsBanned(BanData bandata, bool update)
        {
            webrequest.EnqueueGet(FormatOnlineBansystem(WebAPI_IsBanned_Request, new Dictionary<string, string> { { "{id}", bandata.id.ToString() }, { "{steamid}", bandata.steamid }, { "{name}", bandata.name }, { "{ip}", bandata.ip }, { "{source}", "EBS" }, { "{update}", update.ToString() }, { "{time}", LogTime().ToString() } }), (code, response) =>
            {
                if (response != null || code != 200)
                {
                    if (response == "false" || response == "0")
                        return;
                    timer.Once(0.01f, () => Kick(null, bandata.steamid, response == "true" || response == "1" ? "Banned" : response, false));
                }
                else
                {
                    Interface.Oxide.LogWarning("WebAPI couldn't be contacted or is not valid");
                }
            }, this);

            return string.Empty;
        }

        string WebAPI_Banlist(object source, int startid)
        {
            webrequest.EnqueueGet(FormatOnlineBansystem(WebAPI_Banlist_Request, new Dictionary<string, string> { { "{startid}", startid.ToString() } }), (code, response) =>
            {
                if (response != null || code != 200)
                {
                    SendReply(source, response);
                }
            }, this);

            return string.Empty;
        }

        string WebAPI_Load()
        {
            return FormatReturn(BanSystem.WebAPI, GetMsg("Loaded\n\r"));
        }

        ////////////////////////////////////////////////////////////
        // SQLite
        ////////////////////////////////////////////////////////////

        Ext.SQLite.Libraries.SQLite Sqlite = Interface.GetMod().GetLibrary<Ext.SQLite.Libraries.SQLite>();
        Connection Sqlite_conn;

        string SQLite_Load()
        {
            var returnstring = string.Empty;
            try
            {
                Sqlite_conn = Sqlite.OpenDb(SQLite_DB, this);
                if (Sqlite_conn == null)
                {
                    returnstring = FormatReturn(BanSystem.SQLite, "Couldn't open the SQLite.");
                }
                else
                {
                    Sqlite.Insert(Core.Database.Sql.Builder.Append("CREATE TABLE IF NOT EXISTS EnhancedBanSystem ( id INTEGER NOT NULL PRIMARY KEY UNIQUE, steamid TEXT, name TEXT, ip TEXT, reason TEXT, source TEXT, game TEXT, platform TEXT, server TEXT, expire INTEGER );"), Sqlite_conn);
                    returnstring = FormatReturn(BanSystem.SQLite, GetMsg("Loaded\n\r"));
                }
            }
            catch (Exception e)
            {
                returnstring = e.Message;
            }
            return FormatReturn(BanSystem.SQLite, returnstring);
        }


        string SQLite_RawBan(BanData bandata)
        {
            try
            {
                Sqlite.Insert(Core.Database.Sql.Builder.Append("INSERT OR REPLACE INTO EnhancedBanSystem ( id, steamid, name, ip, reason, source, game, platform, server, expire ) VALUES ( @0, @1, @2, @3, @4, @5, @6, @7, @8, @9 )", bandata.id, bandata.steamid, bandata.name, bandata.ip, bandata.reason, bandata.source, bandata.game, bandata.platform, bandata.server, (int)bandata.expire), Sqlite_conn);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return FormatReturn(BanSystem.SQLite, GetMsg("Successfully added {0} to the banlist."), bandata.ToString());
        }

        string SQLite_ExecuteBan(object source, BanData bandata)
        {
            var sqlString = bandata.steamid == string.Empty ? Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `ip` == @0 ", bandata.ip) : Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `steamid` == @0 AND `ip` == @1 ", bandata.steamid, bandata.ip);

            Sqlite.Query(sqlString, Sqlite_conn, list =>
            {
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        var bd = new BanData(int.Parse(entry["id"].ToString()), (string)entry["source"], (string)entry["steamid"], (string)entry["name"], (string)entry["ip"], (string)entry["reason"], entry["expire"].ToString());
                        var response = FormatReturn(BanSystem.SQLite, GetMsg("This ban already exists ({0})."), bd.ToString());
                        SendReply(source, response);
                        return;
                    }
                }
                var reponse2 = SQLite_RawBan(bandata);
                if (source is IPlayer) ((IPlayer)source).Reply(reponse2);
                else Interface.Oxide.LogInfo(reponse2);
            });

            return string.Empty;
        }

        void SQLite_RawUnban(object source, List<long> unbanList)
        {
            foreach (var id in unbanList)
            {
                Sqlite.Insert(Core.Database.Sql.Builder.Append("DELETE from EnhancedBanSystem WHERE `id` = @0", id), Sqlite_conn);
            }
            var returnstring = FormatReturn(BanSystem.SQLite, GetMsg("{0} matching bans were removed"), unbanList.Count.ToString());
            SendReply(source, returnstring);
        }

        string SQLite_ExecuteUnban(object source, string steamid, string name, string ip)
        {
            List<long> unbanList = new List<long>();
            if (ip != string.Empty || steamid != string.Empty)
            {
                var sqlString = ip != string.Empty ? Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `ip` == @0 ", ip) : Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `steamid` == @0", steamid);
                Sqlite.Query(sqlString, Sqlite_conn, list =>
                {
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            unbanList.Add((long)entry["id"]);
                        }
                    }
                    SQLite_RawUnban(source, unbanList);
                });
            }
            else
            {
                Sqlite.Query(Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `name` LIKE @0", "%" + name + "%"), Sqlite_conn, list =>
                {
                    List<Dictionary<string, object>> f = new List<Dictionary<string, object>>();
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            f.Add(entry);
                            unbanList.Add((long)entry["id"]);
                        }
                    }
                    if (unbanList.Count > 1)
                    {
                        var ret = FormatReturn(BanSystem.SQLite, GetMsg("Multiple Bans Found:\n\r"));
                        foreach (var e in f)
                        {
                            ret += string.Format("{0} - {1} - {2}\n\r", (string)e["steamid"], (string)e["name"], (string)e["reason"]);
                        }
                        if (source is IPlayer) ((IPlayer)source).Reply(ret);
                        else Interface.Oxide.LogInfo(ret);
                        return;
                    }
                    SQLite_RawUnban(source, unbanList);
                });
            }
            return string.Empty;
        }

        void SQLite_UpdateBan(BanData bandata)
        {
            Sqlite.Insert(Core.Database.Sql.Builder.Append("UPDATE EnhancedBanSystem SET `steamid`= @1, `name`= @2, `ip`= @3,`reason`= @4,`source`=@5, `game`= @6, `platform`= @7,`server`= @8, `expire`= @9 WHERE `id` = @0", bandata.id, bandata.steamid, bandata.name, bandata.ip, bandata.reason, bandata.source, bandata.game, bandata.platform, bandata.server, (int)bandata.expire), Sqlite_conn);
        }

        void SQLite_IsBanned(BanData bandata, bool update)
        {
            bandata.reason = BanEvadeReason;

            double cTime = LogTime();
            List<long> unbanList = new List<long>();
            Dictionary<string, object> match = new Dictionary<string, object>();

            Sqlite.Query(Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `steamid` == @0", bandata.steamid), Sqlite_conn, list =>
            {

                var l = new List<Dictionary<string, object>>();
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        var expire = (long)entry["expire"];
                        if (expire != 0.0 && cTime >= expire)
                        {
                            unbanList.Add((long)entry["id"]);
                        }
                        else
                        {
                            if ((string)entry["ip"] == bandata.ip)
                            {
                                match = entry;
                            }
                            l.Add(entry);
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    SQLite_RawUnban("EBS", unbanList);
                    unbanList.Clear();
                }
                var l2 = l.Where(x => (long)x["expire"] == 0l).ToList();
                if (l2.Count == 0)
                {
                    string range1;
                    string range2;
                    string range3;
                    if (RangeFromIP(bandata.ip, out range1, out range2, out range3))
                    {
                        Sqlite.Query(Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem WHERE `ip` == @0 OR `ip` == @1 OR `ip` == @2 OR `ip` == @3", bandata.ip, range1, range2, range3), Sqlite_conn, list2 =>
                        {
                            if (list2 != null)
                            {
                                foreach (var entry in list2)
                                {
                                    var expire = (long)entry["expire"];
                                    if (expire != 0.0 && cTime >= expire)
                                    {
                                        unbanList.Add((long)entry["id"]);
                                    }
                                    else
                                    {
                                        if (!l.Contains(entry))
                                            l.Add(entry);
                                    }
                                }
                            }
                            if (l.Count > 0)
                            {
                                l2 = l.Where(x => (long)x["expire"] == 0l).ToList();
                                bandata.expire = l2.Count > 0 ? 0.0 : match.ContainsKey("expire") ? (long)(match["expire"]) : (long)(l[0]["expire"]);
                                if (match.ContainsKey("expire"))
                                {
                                    if ((long)(match["expire"]) != bandata.expire)
                                    {
                                        var bd = new BanData(int.Parse(match["id"].ToString()), (string)match["source"], (string)match["steamid"], (string)match["name"], (string)match["ip"], (string)match["reason"], match["expire"].ToString());
                                        bd.expire = bandata.expire;
                                        SQLite_UpdateBan(bd);
                                    }
                                }
                                else if (update) ExecuteBan("EBS", bandata, false);
                                timer.Once(0.1f, () => Kick(null, bandata.steamid, match.ContainsKey("reason") ? (string)match["reason"] : (string)l[0]["reason"], false));
                            }
                        });
                    }
                }
                if (l.Count > 0)
                {
                    bandata.expire = l2.Count > 0 ? 0.0 : match.ContainsKey("expire") ? (long)(match["expire"]) : (long)(l[0]["expire"]);
                    if (match.ContainsKey("expire"))
                    {
                        if ((long)(match["expire"]) != bandata.expire)
                        {
                            var bd = new BanData(int.Parse(match["id"].ToString()), (string)match["source"], (string)match["steamid"], (string)match["name"], (string)match["ip"], (string)match["reason"], match["expire"].ToString());
                            SQLite_UpdateBan(bd);
                        }
                    }
                    else if (update) ExecuteBan("EBS", bandata, false);
                    timer.Once(0.1f, () => Kick(null, bandata.steamid, match.ContainsKey("reason") ? (string)match["reason"] : (string)l[0]["reason"], false));
                }
            });
        }

        string SQLite_Banlist(object source, int startid)
        {
            Sqlite.Query(Core.Database.Sql.Builder.Append("SELECT * from EnhancedBanSystem ORDER BY id DESC"), Sqlite_conn, list =>
            {
                int i = -1;
                int max = startid + 9;
                string replystring = string.Empty;
                if (list != null)
                {
                    replystring += FormatReturn(BanSystem.SQLite, GetMsg("Banlist - {0}-{1}/{2}\n"), startid.ToString(), max.ToString(), list.Count.ToString());
                    foreach (var entry in list)
                    {
                        i++;
                        if (i < startid) continue;
                        if (i > max) break;
                        var bd = new BanData(int.Parse(entry["id"].ToString()), (string)entry["source"], (string)entry["steamid"], (string)entry["name"], (string)entry["ip"], (string)entry["reason"], entry["expire"].ToString());
                        replystring += bd.ToString() + "\n";
                    }
                    SendReply(source, replystring);
                }
            });
            return string.Empty;
        }

        ////////////////////////////////////////////////////////////
        // MySQL
        ////////////////////////////////////////////////////////////

        Ext.MySql.Libraries.MySql Sql = Interface.GetMod().GetLibrary<Ext.MySql.Libraries.MySql>();
        Connection Sql_conn;

        string MySQL_Load()
        {
            string returnstring = string.Empty;
            try
            {
                Sql_conn = Sql.OpenDb(MySQL_Host, MySQL_Port, MySQL_DB, MySQL_User, MySQL_Pass, this);
                if (Sql_conn == null || Sql_conn.Con == null)
                {
                    returnstring = FormatReturn(BanSystem.MySQL, "Couldn't open the MySQL PlayerDatabase: {0} ", Sql_conn.Con.State.ToString());
                }
                else
                {
                    Sql.Insert(Core.Database.Sql.Builder.Append("CREATE TABLE IF NOT EXISTS enhancedbansystem ( `id` int(11) NOT NULL, `steamid` VARCHAR(17),`name` VARCHAR(25),`ip` VARCHAR(15),`reason` VARCHAR(25),`source` VARCHAR(25), `game` VARCHAR(25) , `platform` VARCHAR(25), `server` VARCHAR(25), `expire` int(11) );"), Sql_conn);
                    returnstring = FormatReturn(BanSystem.MySQL, GetMsg("Loaded\n\r"));
                }
            }
            catch (Exception e)
            {
                returnstring = FormatReturn(BanSystem.MySQL, e.Message);
            }
            return returnstring;
        }

        string MySQL_RawBan(BanData bandata)
        {
            try
            {
                Sql.Insert(Core.Database.Sql.Builder.Append("INSERT IGNORE INTO enhancedbansystem ( `id`, `steamid`,`name`,`ip`,`reason`,`source`,`game`,`platform`, `server`, `expire` ) VALUES ( @0, @1, @2, @3, @4, @5, @6, @7, @8, @9 )", bandata.id, bandata.steamid, bandata.name, bandata.ip, bandata.reason, bandata.source, bandata.game, bandata.platform, bandata.server, (int)bandata.expire), Sql_conn);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return FormatReturn(BanSystem.MySQL, GetMsg("Successfully added {0} to the banlist."), bandata.ToString());
        }

        void MySQL_UpdateBan(BanData bandata)
        {
            Sqlite.Insert(Core.Database.Sql.Builder.Append("UPDATE EnhancedBanSystem SET `steamid`= @1, `name`= @2, `ip`= @3,`reason`= @4,`source`=@5, `game`= @6, `platform`= @7,`server`= @8, `expire`= @9 WHERE `id` = @0", bandata.id, bandata.steamid, bandata.name, bandata.ip, bandata.reason, bandata.source, bandata.game, bandata.platform, bandata.server, (int)bandata.expire), Sqlite_conn);
        }

        void MySQL_RawUnban(object source, List<int> unbanList)
        {
            foreach (var id in unbanList)
            {
                Sql.Insert(Core.Database.Sql.Builder.Append("DELETE from enhancedbansystem WHERE `id` = @0", id), Sql_conn);
            }
            var returnstring = FormatReturn(BanSystem.SQLite, GetMsg("{0} matching bans were removed"), unbanList.Count.ToString());
            SendReply(source, returnstring);
        }
        string MySQL_ExecuteBan(object source, BanData bandata)
        {
            var sqlString = bandata.steamid == string.Empty ? Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `ip` = @0 ", bandata.ip) : Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `steamid` = @0 AND `ip` = @1 ", bandata.steamid, bandata.ip);
            Sql.Query(sqlString, Sql_conn, list =>
            {
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        var bd = new BanData(int.Parse(entry["id"].ToString()), (string)entry["source"], (string)entry["steamid"], (string)entry["name"], (string)entry["ip"], (string)entry["reason"], entry["expire"].ToString());
                        var response = FormatReturn(BanSystem.MySQL, GetMsg("This ban already exists ({0})."), bd.ToString());
                        SendReply(source, response);
                        return;
                    }
                }
                var reponse2 = MySQL_RawBan(bandata);
                SendReply(source, reponse2);
            });

            return string.Empty;
        }

        string MySQL_ExecuteUnban(object source, string steamid, string name, string ip)
        {
            int i = 0;
            List<int> unbanList = new List<int>();
            if (ip != string.Empty || steamid != string.Empty)
            {
                var sqlString = ip != string.Empty ? Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `ip` = @0 ", ip) : Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `steamid` = @0", steamid);
                Sql.Query(sqlString, Sql_conn, list =>
                {
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            unbanList.Add((int)entry["id"]);
                            i++;
                        }
                    }
                    MySQL_RawUnban(source, unbanList);
                });
            }
            else
            {
                Sql.Query(Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `name` LIKE @0", "%" + name + "%"), Sql_conn, list =>
                {
                    List<Dictionary<string, object>> f = new List<Dictionary<string, object>>();
                    if (list != null)
                    {
                        foreach (var entry in list)
                        {
                            f.Add(entry);
                            unbanList.Add((int)entry["id"]);
                            i++;
                        }
                    }
                    if (unbanList.Count > 1)
                    {
                        string ret = FormatReturn(BanSystem.MySQL, GetMsg("Multiple Bans Found:\n\r"));
                        foreach (var e in f)
                        {
                            ret += string.Format("{0} - {1} - {2}\n\r", (string)e["steamid"], (string)e["name"], (string)e["reason"]);
                        }
                        if (source is IPlayer) ((IPlayer)source).Reply(ret);
                        else Interface.Oxide.LogInfo(ret);
                        return;
                    }
                    else
                    {
                        MySQL_RawUnban(source, unbanList);
                    }
                });
            }
            return string.Empty;
        }

        void MySQL_IsBanned(BanData bandata, bool update)
        {
            List<int> unbanList = new List<int>();
            double cTime = LogTime();
            Dictionary<string, object> match = new Dictionary<string, object>();

            Sql.Query(Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `steamid` = @0", bandata.steamid), Sql_conn, list =>
            {
                var l = new List<Dictionary<string, object>>();
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        var expire = (int)entry["expire"];
                        if (expire != 0 && cTime >= expire)
                        {
                            unbanList.Add((int)entry["id"]);
                        }
                        else
                        {
                            if ((string)entry["ip"] == bandata.ip)
                            {
                                match = entry;
                            }
                            l.Add(entry);
                        }
                    }
                }
                if (unbanList.Count > 0)
                {
                    MySQL_RawUnban("EBS", unbanList);
                    unbanList.Clear();
                }
                var l2 = l.Where(x => (int)x["expire"] == 0).ToList();
                if (l2.Count == 0)
                {
                    string range1;
                    string range2;
                    string range3;
                    if (RangeFromIP(bandata.ip, out range1, out range2, out range3))
                    {
                        Sql.Query(Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem WHERE `ip` = @0 OR `ip` = @1 OR `ip` = @2 OR `ip` = @3", bandata.ip, range1, range2, range3), Sql_conn, list2 =>
                        {
                            if (list2 != null)
                            {
                                foreach (var entry in list2)
                                {
                                    var expire = (int)entry["expire"];
                                    if (expire != 0 && cTime >= double.Parse(expire.ToString()))
                                    {
                                        unbanList.Add((int)entry["id"]);
                                    }
                                    else
                                    {
                                        if (!l.Contains(entry))
                                            l.Add(entry);
                                    }
                                }
                            }
                            if (l.Count > 0)
                            {
                                bandata.expire = l2.Count > 0 ? 0.0 : match.ContainsKey("expire") ? (int)(match["expire"]) : (int)(l[0]["expire"]);
                                if (match.ContainsKey("expire"))
                                {
                                    if (double.Parse(match["expire"].ToString()) != bandata.expire)
                                    {
                                        var bd = new BanData(int.Parse(match["id"].ToString()), (string)match["source"], (string)match["steamid"], (string)match["name"], (string)match["ip"], (string)match["reason"], match["expire"].ToString());
                                        MySQL_UpdateBan(bd);
                                    }
                                }
                                else if (update) ExecuteBan("EBS", bandata, false);
                                timer.Once(0.1f, () => Kick(null, bandata.steamid, match.ContainsKey("reason") ? (string)match["reason"] : (string)l[0]["reason"]));
                            }
                        });
                    }
                }
                if (l.Count > 0)
                {
                    bandata.expire = l2.Count > 0 ? 0.0 : match.ContainsKey("expire") ? double.Parse(match["expire"].ToString()) : double.Parse(l[0]["expire"].ToString());
                    if (match.ContainsKey("expire"))
                    {
                        if (double.Parse(match["expire"].ToString()) != bandata.expire)
                        {
                            var bd = new BanData(int.Parse(match["id"].ToString()), (string)match["source"], (string)match["steamid"], (string)match["name"], (string)match["ip"], (string)match["reason"], match["expire"].ToString());
                            MySQL_UpdateBan(bd);
                        }
                    }
                    else if (update) ExecuteBan("EBS", bandata, false);
                    timer.Once(0.1f, () => Kick(null, bandata.steamid, match.ContainsKey("reason") ? (string)match["reason"] : (string)l[0]["reason"], false));
                }
            });
        }

        string MySQL_Banlist(object source, int startid)
        {
            Sql.Query(Core.Database.Sql.Builder.Append("SELECT * from enhancedbansystem ORDER BY id DESC"), Sql_conn, list =>
            {
                int i = -1;
                int max = startid + 9;
                string replystring = string.Empty;
                if (list != null)
                {
                    replystring += FormatReturn(BanSystem.MySQL, GetMsg("Banlist - {0}-{1}/{2}\n"), startid.ToString(), max.ToString(), list.Count.ToString());
                    foreach (var entry in list)
                    {
                        i++;
                        if (i < startid) continue;
                        if (i > max) break;
                        var bd = new BanData(int.Parse(entry["id"].ToString()), (string)entry["source"], (string)entry["steamid"], (string)entry["name"], (string)entry["ip"], (string)entry["reason"], entry["expire"].ToString());
                        replystring += bd.ToString() + "\n";

                    }
                    SendReply(source, replystring);
                }
            });
            return string.Empty;
        }

        ////////////////////////////////////////////////////////////
        // Native
        ////////////////////////////////////////////////////////////

        string Native_Load()
        {
            return FormatReturn(BanSystem.Native, GetMsg("Loaded\n\r"));
        }
        string Native_ExecuteBan(BanData bandata)
        {
            if (bandata.steamid == string.Empty) return FormatReturn(BanSystem.Native, "Can't ban by IP.");

            var iplayer = players.FindPlayer(bandata.steamid);
            if (iplayer == null) return FormatReturn(BanSystem.Native, GetMsg("No matching player was found.\n"));

            if (iplayer.IsBanned) return FormatReturn(BanSystem.Native, GetMsg("This ban already exists ({0})."), iplayer.Id.ToString());

            TimeSpan duration = bandata.expire == 0.0 ? default(TimeSpan) : TimeSpan.Parse((bandata.expire - LogTime()).ToString());

            iplayer.Ban(bandata.reason, duration);
            return FormatReturn(BanSystem.Native, GetMsg("Successfully added {0} to the banlist."), bandata.steamid.ToString());
        }

        string Native_ExecuteUnban(string steamid, string name)
        {
            if (steamid == string.Empty)
            {
                if (name == string.Empty) return string.Empty;
                var f = players.FindPlayers(name).Where(x => x.IsBanned).ToList();
                if (f.Count == 0)
                {
                    return FormatReturn(BanSystem.Native, GetMsg("No matching player was found.\n"));
                }
                if (f.Count > 1)
                {
                    var ret = string.Empty;
                    foreach (var p in f)
                    {
                        ret += string.Format("{0} - {1}\n", p.Id, p.Name);
                    }
                    return ret;
                }
                steamid = f[0].Id;
            }
            var b = players.FindPlayer(steamid);
            if (b == null)
            {
                return FormatReturn(BanSystem.Native, GetMsg("No matching player was found.\n"));
            }
            if (!b.IsBanned)
            {
                return FormatReturn(BanSystem.Native, GetMsg("{0} - {1} isn't banned.\n"), b.Id, b.Name);
            }
            b.Unban();
            return FormatReturn(BanSystem.Native, "1 matching bans were removed");
        }

        bool Native_IsBanned(string steamid)
        {
            var b = players.FindPlayer(steamid);
            if (b == null)
            {
                return false;
            }
            return b.IsBanned;
        }

        string Native_Banlist(object source, int startid)
        {
            int i = -1;
            int max = startid + 9;

            var banlist = players.All.Where(x => x.IsBanned).ToList();

            string returnstring = FormatReturn(BanSystem.Native, GetMsg("Banlist - {0}-{1}/{2}\n"), startid.ToString(), max.ToString(), banlist.Count.ToString());

            foreach (IPlayer b in banlist)
            {
                i++;
                if (i < startid) continue;
                if (i > max) break;
                returnstring += string.Format("{0} - {1} - {2}", b.Id, b.Name, ToShortString(b.BanTimeRemaining));
            }

            return returnstring;
        }


        ////////////////////////////////////////////////////////////
        // Kick
        ////////////////////////////////////////////////////////////

        string Kick(object source, string target, string reason, bool shouldBroadcast = true)
        {
            string r = string.Empty;
            var foundplayers = FindConnectedPlayers(target, source, out r);
            if (r != string.Empty)
            {
                return r;
            }

            var returnkick = string.Empty;
            foreach (var iplayer in foundplayers)
            {
                returnkick += ExecuteKick(source, iplayer, reason, shouldBroadcast) + "\r\n";
            }

            return returnkick;
        }
        string TryKick(object source, string[] args)
        {
            string target = args[0];
            string reason = args.Length > 1 ? args[1] : "Kicked";
            return Kick(source, target, reason);
        }

        string ExecuteKick(object source, IPlayer player, string reason, bool shouldBroadcast = true)
        {
            if (shouldBroadcast && Kick_Broadcast)
                server.Broadcast(string.Format(GetMsg("{0} was kicked from the server ({1})", null), player.Name.ToString(), reason));

            if (Kick_Log)
                Interface.Oxide.LogWarning(string.Format(GetMsg("{0} was kicked from the server ({1})", null), player.Name.ToString(), reason));

            player.Kick(reason);

            return string.Format(GetMsg("{0} was kicked from the server ({1})", source), player.Name.ToString(), reason);
        }


        ////////////////////////////////////////////////////////////
        // IsBanned
        ////////////////////////////////////////////////////////////

        bool isBanned_NonDelayed(string name, string steamid, string ip, bool update, out BanData bandata)
        {
            bool denied = false;
            bandata = null;
            if (!denied && BanSystemHasFlag(banSystem, BanSystem.Native))
            {
                if (Native_IsBanned(steamid))
                {
                    if (Log_Denied)
                        Interface.Oxide.LogInfo(string.Format("Native: {0} - {1} - {2} was rejected from the server", steamid, name, ip));
                    denied = true;
                }
            }
            if (!denied && BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
            {
                if (PlayerDatabase_IsBanned(steamid, ip, out bandata))
                {
                    if (Log_Denied)
                        Interface.Oxide.LogInfo(string.Format("PlayerDatabase: {0} - {1} - {2} was rejected from the server", steamid, name, ip));
                    denied = true;
                }
            }
            if (!denied && BanSystemHasFlag(banSystem, BanSystem.Files))
            {
                if (Files_IsBanned(steamid, ip, out bandata))
                {
                    if (Log_Denied)
                        Interface.Oxide.LogInfo(string.Format("Files: {0} - {1} - {2} was rejected from the server", steamid, name, ip));
                    denied = true;
                }
            }
            if (update && denied)
            {
                if (bandata != null && (bandata.ip != ip || bandata.steamid != steamid))
                    PrepareBan("EBS", steamid, name, ip, BanEvadeReason, bandata.expire == 0.0 ? 0.0 : bandata.expire - LogTime(), false);
                else if (bandata == null)
                    PrepareBan("EBS", steamid, name, ip, BanEvadeReason, 0.0, false);
            }
            return bandata != null;
        }

        void isBanned_Delayed(string name, string steamid, string ip, bool update)
        {
            var partialBan = new BanData("EBS", steamid, name, ip, string.Empty, 0.0);
            if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
            {
                SQLite_IsBanned(partialBan, update);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
            {
                MySQL_IsBanned(partialBan, update);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
            {
                WebAPI_IsBanned(partialBan, update);
            }
        }

        ////////////////////////////////////////////////////////////
        // Banlist
        ////////////////////////////////////////////////////////////

        string TryBanlist(object source, string[] args)
        {
            int startID = 0;
            BanSystem bs;
            if (args != null && args.Length > 1)
            {
                int.TryParse(args[1], out startID);
            }
            switch (args[0].ToLower())
            {
                case "files":
                    bs = BanSystem.Files;
                    break;
                case "mysql":
                    bs = BanSystem.MySQL;
                    break;
                case "native":
                    bs = BanSystem.Native;
                    break;
                case "playerdatabase":
                    bs = BanSystem.PlayerDatabase;
                    break;
                case "sqlite":
                    bs = BanSystem.SQLite;
                    break;
                case "webapi":
                    bs = BanSystem.WebAPI;
                    break;
                default:
                    return GetMsg("Wrong usage of /banlist", source);
            }

            return Banlist(source, bs, startID);
        }

        string Banlist(object source, BanSystem bs, int startID)
        {
            switch (bs)
            {
                case BanSystem.Files:
                    return Files_Banlist(source, startID);
                case BanSystem.MySQL:
                    return MySQL_Banlist(source, startID);
                case BanSystem.Native:
                    return Native_Banlist(source, startID);
                case BanSystem.PlayerDatabase:
                    return PlayerDatabase_Banlist(source, startID);
                case BanSystem.SQLite:
                    return SQLite_Banlist(source, startID);
                case BanSystem.WebAPI:
                    return WebAPI_Banlist(source, startID);
                default:
                    return string.Empty;
            }
        }

        ////////////////////////////////////////////////////////////
        // Ban
        ////////////////////////////////////////////////////////////


        string TryBan(object source, string[] args)
        {
            string ipaddress = isIPAddress(args[0]) ? args[0] : string.Empty;
            string steamid = string.Empty;
            string name = string.Empty;
            string errorreason = string.Empty;
            ulong userID = 0L;

            string reason = args.Length > 1 ? args[1] : BanDefaultReason;
            double duration = 0.0;
            if (args.Length > 2) double.TryParse(args[2], out duration);


            if (ipaddress != string.Empty)
            {
                return BanIP(source, ipaddress, reason, duration);
            }
            else
            {
                var foundplayers = FindPlayers(args[0], source, out errorreason);
                if (errorreason != string.Empty)
                {
                    if (ulong.TryParse(args[0], out userID))
                    {
                        return BanID(source, args[0], reason, duration);
                    }
                    return errorreason;
                }
                return BanPlayer(source, foundplayers[0], reason, duration);
            }
        }

        string BanIP(object source, string ip, string reason, double duration)
        {
            return PrepareBan(source, string.Empty, string.Empty, ip, reason, duration, Kick_OnBan);
        }

        string BanID(object source, string steamid, string reason, double duration)
        {
            string name = GetPlayerName(steamid);
            string ipaddress = GetPlayerIP(steamid);

            return PrepareBan(source, steamid, name, ipaddress, reason, duration, Kick_OnBan);
        }

        string BanPlayer(object source, IPlayer player, string reason, double duration)
        {
            var address = GetPlayerIP(player);

            return PrepareBan(source, player.Id, player.Name, address, reason, duration, Kick_OnBan);
        }

        string PrepareBan(object source, string userID, string name, string ip, string reason, double duration, bool kick)
        {
            var bandata = new BanData(source, userID, name, ip, reason, duration);

            return ExecuteBan(source, bandata, kick);
        }
        string ExecuteBan(object source, BanData bandata, bool kick)
        {
            if (wasBanned.Contains(bandata.id)) return string.Empty;

            string returnstring = string.Empty;
            if (BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
            {
                returnstring += PlayerDatabase_ExecuteBan(bandata);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Files))
            {
                returnstring += Files_ExecuteBan(bandata);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
            {
                returnstring += MySQL_ExecuteBan(source, bandata);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
            {
                returnstring += SQLite_ExecuteBan(source, bandata);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
            {
                returnstring += WebAPI_ExecuteBan(source, bandata);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Native))
            {
                //For the time being temp bans wont be handled by native
                returnstring += Native_ExecuteBan(bandata);
            }

            if (Ban_Broadcast && bandata.name != string.Empty)
                server.Broadcast(string.Format(GetMsg("{0} was {2} banned from the server ({1})"), bandata.name, bandata.reason, bandata.expire == 0.0 ? GetMsg("permanently") : GetMsg("temporarily")));

            if (Ban_Log && (source is IPlayer) && ((IPlayer)source).Id != "server_console")
                Interface.Oxide.LogWarning(returnstring);

            if (kick)
                Kick(source, bandata.steamid != string.Empty ? bandata.steamid : bandata.ip, "Banned");

            wasBanned.Add(bandata.id);

            return returnstring;
        }

        ////////////////////////////////////////////////////////////
        // Unban
        ////////////////////////////////////////////////////////////

        string ExecuteUnban(object source, string steamid, string name, string ip)
        {
            string returnstring = string.Empty;
            List<BanData> unbanList = new List<BanData>();
            List<BanData> unbanList2 = new List<BanData>();

            if (BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
            {
                returnstring += PlayerDatabase_ExecuteUnban(steamid, name, ip, out unbanList);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Files))
            {
                returnstring += Files_ExecuteUnban(steamid, name, ip, out unbanList2);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
            {
                returnstring += MySQL_ExecuteUnban(source, steamid, name, ip);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
            {
                returnstring += SQLite_ExecuteUnban(source, steamid, name, ip);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
            {
                returnstring += WebAPI_ExecuteUnban(source, steamid, name, ip);
            }
            if (BanSystemHasFlag(banSystem, BanSystem.Native))
            {
                returnstring += Native_ExecuteUnban(steamid, name);
            }

            foreach (var b in unbanList)
            {
                if (cachedBans.ContainsKey(b.id))
                    cachedBans.Remove(b.id);
            }
            foreach (var b in unbanList2)
            {
                if (cachedBans.ContainsKey(b.id))
                    cachedBans.Remove(b.id);
            }

            return returnstring;
        }

        string TryUnBan(object source, string[] args)
        {
            string ipaddress = isIPAddress(args[0]) ? args[0] : string.Empty;
            string steamid = string.Empty;
            ulong userID = 0L;
            string name = string.Empty;
            string errorreason = string.Empty;

            if (ipaddress != string.Empty)
            {
                return ExecuteUnban(source, string.Empty, string.Empty, ipaddress);
            }
            else
            {
                ulong.TryParse(args[0], out userID);
                return ExecuteUnban(source, userID != 0L ? args[0] : string.Empty, userID == 0L ? args[0] : string.Empty, string.Empty);
            }
        }


        ////////////////////////////////////////////////////////////
        // Commands
        ////////////////////////////////////////////////////////////

        [Command("ban", "player.ban")]
        void cmdBan(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBan))
            {
                player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString()));
                return;
            }
            if (args == null || (args.Length < 1))
            {
                player.Reply(GetMsg("Syntax: ban < Name | SteamID | IP | IP Range > < reason(optional) > < time in secondes(optional) > ", player.Id.ToString()));
                return;
            }
            try
            {
                player.Reply(TryBan(player, args));
            }
            catch (Exception e)
            {
                player.Reply("ERROR:" + e.Message);
            }
        }

        [Command("banlist", "player.banlist")]
        void cmdBanlist(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionBanlist))
            {
                player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString()));
                return;
            }
            if (args == null || args.Length == 0)
            {
                string replystring = GetMsg("Syntax: banlist <BanSystem> <startid>", player.Id.ToString()) + "\n";
                replystring += GetMsg("Avaible BanSystems:\n", player.Id.ToString());
                if (BanSystemHasFlag(banSystem, BanSystem.Files))
                {
                    replystring += "Files\n";
                }
                if (BanSystemHasFlag(banSystem, BanSystem.MySQL))
                {
                    replystring += "MySQL\n";
                }
                if (BanSystemHasFlag(banSystem, BanSystem.Native))
                {
                    replystring += "Native\n";
                }
                if (BanSystemHasFlag(banSystem, BanSystem.PlayerDatabase))
                {
                    replystring += "PlayerDatabase\n";
                }
                if (BanSystemHasFlag(banSystem, BanSystem.SQLite))
                {
                    replystring += "SQLite\n";
                }
                if (BanSystemHasFlag(banSystem, BanSystem.WebAPI))
                {
                    replystring += "WebAPI\n";
                }
                player.Reply(replystring);
                return;
            }
            try
            {
                player.Reply(TryBanlist(player, args));
            }
            catch (Exception e)
            {
                player.Reply("ERROR:" + e.Message);
            }
        }

        [Command("kick", "player.kick")]
        void cmdKick(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionKick))
            {
                player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString()));
                return;
            }
            if (args == null || (args.Length < 1))
            {
                player.Reply(GetMsg("Syntax: kick < Name | SteamID | IP | IP Range > < reason(optional) >", player.Id.ToString()));
                return;
            }
            try
            {
                player.Reply(TryKick(player, args));
            }
            catch (Exception e)
            {
                player.Reply("ERROR:" + e.Message);
            }
        }

        [Command("unban", "player.unban")]
        void cmdUnban(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermissionUnban))
            {
                player.Reply(GetMsg("You don't have the permission to use this command.", player.Id.ToString()));
                return;
            }
            if (args == null || (args.Length < 1))
            {
                player.Reply(GetMsg("Syntax: unban < Name | SteamID | IP | IP Range >", player.Id.ToString()));
                return;
            }
            try
            {
                player.Reply(TryUnBan(player, args));
            }
            catch (Exception e)
            {
                player.Reply("ERROR:" + e.Message);
            }
        }
    }
}