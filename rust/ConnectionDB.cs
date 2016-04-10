using System;
using System.Collections.Generic;
using Oxide.Core;
namespace Oxide.Plugins
{
    [Info("ConnectionDB", "Norn", 0.2, ResourceId = 1459)]
    [Description("Connection database for devs.")]
    public class ConnectionDB : RustPlugin
    {
        class StoredData
        {
            public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
            public StoredData(){}
        }
        class PlayerInfo
        {
            public ulong uUserID;
            public string tFirstName;
            public string tLastName;
            public int iInitTimestamp;
            public int iLastSeen;
            public string tInitIP;
            public string tLastIP;
            public int iSecondsPlayed;
            public int iConnections;
            public string tReason;
            public bool bAlive;
            public PlayerInfo(){}
        }
        StoredData DB_Connection;
        void Unload()
        {
            SaveData();
            if (SecondsCount != null) SecondsCount.Destroy();
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, DB_Connection);
        }
        private bool InitPlayer(BasePlayer player)
        {
            if (player == null || !player.isConnected) return false;
            PlayerInfo p = null;
            if (!DB_Connection.PlayerInfo.TryGetValue(player.userID, out p))
            {
                var info = new PlayerInfo(); int firstseen = UnixTimeStampUTC(); string name = player.displayName; string ip = player.net.connection.ipaddress;
                info.uUserID = player.userID;
                info.iInitTimestamp = firstseen;
                info.iLastSeen = firstseen;
                info.iSecondsPlayed = 0;
                info.tFirstName = name;
                info.tLastName = name;
                info.tInitIP = ip;
                info.tLastIP = ip;
                info.iConnections = 1;
                info.bAlive = player.IsAlive();
                info.tReason = "null";
                DB_Connection.PlayerInfo.Add(info.uUserID, info);
                Interface.GetMod().DataFileSystem.WriteObject(this.Title, DB_Connection);
                int current_connections = Convert.ToInt32(Config["DB", "UniqueConnections"]); current_connections++; Config["DB", "UniqueConnections"] = current_connections; SaveConfig();
                if(Convert.ToBoolean(Config["General", "Debug"])) Puts("Registering " + info.tFirstName + " [ " + info.uUserID + " ].");
                return true;
            }
            else
            {
                p.iLastSeen = UnixTimeStampUTC();
                p.tLastName = player.displayName;
                p.tLastIP = player.net.connection.ipaddress;
                p.bAlive = player.IsAlive();
                p.iConnections++;
                if (Convert.ToBoolean(Config["General", "Debug"])) Puts("Updating " + p.tFirstName + " [ " + p.uUserID + " ].");
            }
            return false;
        }
        private void SyncAlive(BasePlayer player)
        {
            if (player != null && player.IsConnected())
            {
                if (ConnectionDataExists(player))
                {
                    DB_Connection.PlayerInfo[player.userID].bAlive = player.IsAlive();
                }
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            InitPlayer(player);
        }
        private bool SaveConnectionDataFromID(ulong steamid, string reason = "")
        {
            if (ConnectionDataExistsFromID(steamid))
            {
                BasePlayer player = BasePlayer.FindByID(steamid);
                if(player != null && player.IsConnected())
                {
                    DB_Connection.PlayerInfo[steamid].tLastName = player.displayName;
                    DB_Connection.PlayerInfo[steamid].tLastIP = player.net.connection.ipaddress;
                    DB_Connection.PlayerInfo[steamid].bAlive = player.IsAlive();
                }
                DB_Connection.PlayerInfo[steamid].iLastSeen = UnixTimeStampUTC();
                if (reason != "") { DB_Connection.PlayerInfo[player.userID].tReason = reason; }
                return true;
            }
            return false;
        }
        private void SaveConnectionData(BasePlayer player, string reason = "")
        {
            if (ConnectionDataExists(player))
            {
                DB_Connection.PlayerInfo[player.userID].tLastName = player.displayName;
                DB_Connection.PlayerInfo[player.userID].tLastIP = player.net.connection.ipaddress;
                DB_Connection.PlayerInfo[player.userID].iLastSeen = UnixTimeStampUTC();
                DB_Connection.PlayerInfo[player.userID].bAlive = player.IsAlive();
                if (reason != "") { DB_Connection.PlayerInfo[player.userID].tReason = reason; }
            }
            else
            {
                InitPlayer(player);
                SaveConnectionData(player, reason);
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveConnectionData(player, reason);
        }
        private Int32 UnixTimeStampUTC()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;
        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
               ? UnixEpoch.AddMilliseconds(unixTimeStamp)
               : UnixEpoch.AddSeconds(unixTimeStamp);
        }
        private DateTime FirstSeenFromID(ulong steamid)
        {
            DateTime date = System.DateTime.Now;
            if (ConnectionDataExistsFromID(steamid)) date = UnixTimeStampToDateTime(DB_Connection.PlayerInfo[steamid].iInitTimestamp);
            return date;
        }
        private int Connections(BasePlayer player)
        {
            int connections = 0;
            if (ConnectionDataExistsFromID(player.userID)) connections = DB_Connection.PlayerInfo[player.userID].iConnections;
            return connections;
        }
        private int ConnectionsFromID(ulong steamid)
        {
            int connections = 0;
            if (ConnectionDataExistsFromID(steamid)) connections = DB_Connection.PlayerInfo[steamid].iConnections;
            return connections;
        }
        private int SecondsPlayed(BasePlayer player)
        {
            int seconds = 0;
            if (ConnectionDataExistsFromID(player.userID)) seconds = DB_Connection.PlayerInfo[player.userID].iSecondsPlayed;
            return seconds;
        }
        private int SecondsPlayedFromID(ulong steamid)
        {
            int seconds = 0;
            if (ConnectionDataExistsFromID(steamid)) seconds = DB_Connection.PlayerInfo[steamid].iSecondsPlayed;
            return seconds;
        }
        private string FirstIP(BasePlayer player)
        {
            if (ConnectionDataExistsFromID(player.userID)) return DB_Connection.PlayerInfo[player.userID].tInitIP;
            return "null";
        }
        private string FirstIPFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) return DB_Connection.PlayerInfo[steamid].tInitIP;
            return "null";
        }
        private string LastIP(BasePlayer player)
        {
            if (ConnectionDataExistsFromID(player.userID)) return DB_Connection.PlayerInfo[player.userID].tLastIP;
            return "null";
        }
        private string LastIPFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) return DB_Connection.PlayerInfo[steamid].tLastIP;
            return "null";
        }
        private string LastName(BasePlayer player)
        {
            if (ConnectionDataExistsFromID(player.userID)) return DB_Connection.PlayerInfo[player.userID].tLastName;
            return "null";
        }
        private string LastNameFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) return DB_Connection.PlayerInfo[steamid].tLastName;
            return "null";
        }
        private string FirstName(BasePlayer player)
        {
            if (ConnectionDataExistsFromID(player.userID)) return DB_Connection.PlayerInfo[player.userID].tFirstName;
            return "null";
        }
        private string FirstNameFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) return DB_Connection.PlayerInfo[steamid].tFirstName;
            return "null";
        }
        private string DisconnectReasonFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) { if(DB_Connection.PlayerInfo[steamid].tReason.Length >= 1) { return DB_Connection.PlayerInfo[steamid].tReason; } }
            return "null";
        }
        private string DisconnectReason(BasePlayer player)
        {
            if (ConnectionDataExistsFromID(player.userID)) { if (DB_Connection.PlayerInfo[player.userID].tReason.Length >= 1) { return DB_Connection.PlayerInfo[player.userID].tReason; } }
            return "null";
        }
        private bool IsPlayerAliveFromID(ulong steamid)
        {
            if (ConnectionDataExistsFromID(steamid)) { return DB_Connection.PlayerInfo[steamid].bAlive; }
            return false;
        }
        private DateTime FirstSeen(BasePlayer player)
        {
            DateTime date = System.DateTime.Now;
            if (ConnectionDataExistsFromID(player.userID)) date = UnixTimeStampToDateTime(DB_Connection.PlayerInfo[player.userID].iInitTimestamp);
            return date;
        }
        private DateTime LastSeen(BasePlayer player)
        {
            DateTime date = System.DateTime.Now;
            if (ConnectionDataExistsFromID(player.userID)) date = UnixTimeStampToDateTime(DB_Connection.PlayerInfo[player.userID].iLastSeen);
            return date;
        }
        private DateTime ConfigInitTimestamp()
        {
            return UnixTimeStampToDateTime(Convert.ToInt32(Config["General", "ConfigInit"]));
        }
        private DateTime LastSeenFromID(ulong steamid)
        {
            DateTime date = System.DateTime.Now;
            if (ConnectionDataExistsFromID(steamid)) date = UnixTimeStampToDateTime(DB_Connection.PlayerInfo[steamid].iLastSeen);
            return date;
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ GENERAL SETTINGS ] ---

            Config["General", "Debug"] = true;
            Config["General", "ConfigInit"] = UnixTimeStampUTC();

            Config["Admin", "AuthLevel"] = 2;

            Config["DB", "UniqueConnections"] = 0;

            Config["Timers", "SecondsInterval"] = 10;

            SaveConfig();
        }
		System.Random rnd = new System.Random();
        protected int GetRandomInt(int min, int max)
        {
            return rnd.Next(min, max);
        }
        void Loaded()
        {
            DB_Connection = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
        }
        private bool ConnectionDataExistsFromID(ulong steamid)
        {
            if (DB_Connection.PlayerInfo.ContainsKey(steamid)) { return true; }
            return false;
        }
        private bool ConnectionDataExists(BasePlayer player)
        {
            if (player == null || !player.isConnected) return false;
            if (DB_Connection.PlayerInfo.ContainsKey(player.userID)) { return true; }
            return false;
        }
        private int UniqueConnections()
        {
            return Convert.ToInt32(Config["DB", "UniqueConnections"]);
        }
        private int ConnectionPlayerCount()
        {
            return DB_Connection.PlayerInfo.Count;
        }
        private void UpdateSeconds()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) { if (ConnectionDataExists(player)) { DB_Connection.PlayerInfo[player.userID].iSecondsPlayed += Convert.ToInt32(Config["Timers", "SecondsInterval"]); SyncAlive(player);  } else { InitPlayer(player); } }
        }
        Timer SecondsCount;
        void OnServerInitialized()
        {
            Puts("Loaded " + ConnectionPlayerCount().ToString() + " profiles. [Unique Connections: " + UniqueConnections().ToString() + "]");
            int seconds = Convert.ToInt32(Config["Timers", "SecondsInterval"]);
            SecondsCount = timer.Repeat(seconds, 0, () => UpdateSeconds());
        }
    }
}