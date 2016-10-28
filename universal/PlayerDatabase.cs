using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.SQLite.Libraries;
using Oxide.Ext.MySql.Libraries;
using Newtonsoft.Json.Linq;
using Oxide.Core.Database;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PlayerDatabase", "Reneb", "1.5.3")]
    class PlayerDatabase : CovalencePlugin
    {
        List<string> changedPlayersData = new List<string>();

        DataType dataType = DataType.Files;

        enum DataType
        {
            Files,
            SQLite,
            MySql
        }

        ////////////////////////////////////////////////////////////
        // Configs
        ////////////////////////////////////////////////////////////

        static int dataTypeCfg = 1;

        static string sqlitename = "playerdatabase.db";

        static string sql_host = "localhost";
        static int sql_port = 3306;
        static string sql_db = "rust";
        static string sql_user = "root";
        static string sql_pass = "toor";


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
            CheckCfg<int>("Data Type : 0 (Files) or 1 (SQLite) or 2 (MySQL)", ref dataTypeCfg);
            CheckCfg<string>("SQLite - Database Name", ref sqlitename);
            CheckCfg<string>("MySQL - Host", ref sql_host);
            CheckCfg<int>("MySQL - Port", ref sql_port);
            CheckCfg<string>("MySQL - Database Name", ref sql_db);
            CheckCfg<string>("MySQL - Username", ref sql_user);
            CheckCfg<string>("MySQL - Password", ref sql_pass);
            dataType = (DataType)dataTypeCfg;
            SaveConfig();
            SetupDatabase();
        }

        ////////////////////////////////////////////////////////////
        // General Methods
        ////////////////////////////////////////////////////////////

        void FatalError(string msg)
        {
            Interface.Oxide.LogError(msg);
            if (dataType == DataType.MySql) Sql_conn.Con.Close();
            timer.Once(0.01f, () => Interface.Oxide.UnloadPlugin("PlayerDatabase"));
        }

        string GetMsg(string key, object steamid = null) => lang.GetMessage(key, this, steamid == null ? null : steamid.ToString());

        List<string> KnownPlayers() => dataType == DataType.SQLite ? sqliteData.Keys.ToList() : dataType == DataType.MySql ? sqlData.Keys.ToList() : storedData.knownPlayers.ToList();

        bool isKnownPlayer(string userid) => dataType == DataType.SQLite ? sqliteData.ContainsKey(userid) : dataType == DataType.MySql ? sqlData.ContainsKey(userid) : storedData.knownPlayers.Contains(userid);

        List<string> GetAllKnownPlayers() => KnownPlayers();

        object FindPlayer(string arg)
        {
            ulong steamid = 0L;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            if (steamid != 0L && arg.Length == 17)
            {
                if (!isKnownPlayer(arg)) return GetMsg("No players found matching this steamid.", null);
                else return arg;
            }

            Dictionary<string, string> foundPlayers = new Dictionary<string, string>();
            foreach (var userid in KnownPlayers())
            {
                var d = GetPlayerData(userid, "name");
                if (d != null)
                {
                    var name = (string)d;
                    string lowname = name.ToLower();
                    if (lowname.Contains(lowerarg))
                        if (!foundPlayers.ContainsKey(userid))
                            foundPlayers.Add(userid, name.ToString());

                }
            }
            if (foundPlayers.Count > 1)
            {
                string msg = string.Empty;
                foreach (KeyValuePair<string, string> pair in foundPlayers) { msg += string.Format("{0} {1}\n", pair.Key, pair.Value); }
                return msg;
            }
            foreach (string key in foundPlayers.Keys)
            {
                return key;
            }
            return GetMsg("No players found matching this name.", null);
        }

        ////////////////////////////////////////////////////////////
        // Oxide Hooks
        ////////////////////////////////////////////////////////////

        void OnServerSave()
        {
            SavePlayerDatabase();

            if (dataType == DataType.Files) SaveKnownPlayers();
        }

        void Unload()
        {
            OnServerSave();
        }

        void SetupDatabase()
        {
            LoadData();
            LoadPlayers();

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No players found matching this steamid.",  "No players found matching this steamid."},
                { "No players found matching this name.","No players found matching this name." }
            }, this);
        }

        void OnUserConnected(IPlayer player) { OnPlayerJoined(player.Id, player.Name, player.Address); }

        void OnPlayerJoined(string steamid, string name, string ip)
        {
            if (!isKnownPlayer(steamid)) { LoadPlayer(steamid); }
            SetPlayerData(steamid, "name", name);
            SetPlayerData(steamid, "ip", ip);
            SetPlayerData(steamid, "steamid", steamid);
        }

        ////////////////////////////////////////////////////////////
        // Save/Load
        ////////////////////////////////////////////////////////////

        void LoadData()
        {
            switch (dataType)
            {
                case DataType.SQLite:
                    LoadSQLite();
                    break;
                case DataType.MySql:
                    LoadMySQL();
                    break;
                default:
                    LoadFiles();
                    break;
            }
        }

        void LoadPlayers()
        {
            foreach (string userid in KnownPlayers())
            {
                try
                {
                    LoadPlayer(userid);
                }
                catch
                {
                    Interface.Oxide.LogWarning("Couldn't load " + userid);
                }
            }
        }

        void LoadPlayer(string userid)
        {
            if (dataType == DataType.SQLite)
            {
                LoadPlayerSQLite(userid);
            }
            else if (dataType == DataType.MySql)
            {
                LoadPlayerSQL(userid);
            }
            else
            {
                LoadPlayerData(userid);
            }
        }

        void SavePlayerDatabase()
        {
#if RUST
            using (TimeWarning.New("Save PlayerDatabase", 0.1f))
            {
#endif
            foreach (string userid in changedPlayersData)
            {
                try
                {
                    if (dataType == DataType.SQLite)
                    {
                        SavePlayerSQLite(userid);
                    }
                    else if (dataType == DataType.MySql)
                    {
                        SavePlayerSQL(userid);
                    }
                    else
                    {
                        SavePlayerData(userid);
                    }
                }
                catch (Exception e)
                {
                    Interface.Oxide.LogWarning(e.Message);
                }
            }
            changedPlayersData.Clear();
#if RUST
        }
#endif
        }

        ////////////////////////////////////////////////////////////
        // Set / Get PlayerData
        ////////////////////////////////////////////////////////////

        void SetPlayerData(string userid, string key, object data)
        {
            if (!isKnownPlayer(userid)) LoadPlayer(userid);
            if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key))
                {
                    CreateNewColumn(key);
                }
                sqliteData[userid][key] = JsonConvert.SerializeObject(data);
            }
            else if (dataType == DataType.MySql)
            {
                if (!isValidColumn2(key))
                {
                    CreateNewColumn2(key);
                }
                sqlData[userid][key] = JsonConvert.SerializeObject(data);
            }
            else
            {
                var profile = playersData[userid];

                profile[key] = JsonConvert.SerializeObject(data);
                playersData[userid] = profile;
            }

            if (!changedPlayersData.Contains(userid))
                changedPlayersData.Add(userid);
        }

        object GetPlayerDataRaw(string userid, string key)
        {
            if (!isKnownPlayer(userid)) return null;

            if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key)) return null;
                if (sqliteData[userid] == null) return null;
                if (sqliteData[userid][key] == null) return null;
                return (string)sqliteData[userid][key];
            }
            else if (dataType == DataType.MySql)
            {
                if (!isValidColumn2(key)) return null;
                if (sqlData[userid] == null) return null;
                if (sqlData[userid][key] == null) return null;
                return (string)sqlData[userid][key];
            }
            else
            {
                var profile = playersData[userid];
                if (profile[key] == null) return null;
                return (string)profile[key];
            }
        }
        object GetPlayerData(string userid, string key)
        {
            if (!isKnownPlayer(userid)) return null;

            if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key)) return null;
                if (sqliteData[userid] == null) return null;
                if (sqliteData[userid][key] == null) return null;
                return JsonConvert.DeserializeObject((string)sqliteData[userid][key]);
            }
            else if (dataType == DataType.MySql)
            {
                if (!isValidColumn2(key)) return null;
                if (sqlData[userid] == null) return null;
                if (sqlData[userid][key] == null) return null;
                return JsonConvert.DeserializeObject((string)sqlData[userid][key]);
            }
            else
            {
                var profile = playersData[userid];
                if (profile[key] == null) return null;
                return JsonConvert.DeserializeObject((string)profile[key]);
            }
        }


        ////////////////////////////////////////////////////////////
        // Files
        ////////////////////////////////////////////////////////////

        public static DataFileSystem datafile = Interface.GetMod().DataFileSystem;

        string subDirectory = "playerdatabase/";

        Hash<string, DynamicConfigFile> playersData = new Hash<string, DynamicConfigFile>();

        StoredData storedData;

        class StoredData
        {
            public HashSet<string> knownPlayers = new HashSet<string>();

            public StoredData() { }
        }

        void LoadFiles()
        {
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("PlayerDatabase");
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        void LoadPlayerData(string userid)
        {
            if (!storedData.knownPlayers.Contains(userid))
                storedData.knownPlayers.Add(userid);

            string path = subDirectory + userid;
            if (datafile.ExistsDatafile(path)) { }

            DynamicConfigFile profile = Interface.GetMod().DataFileSystem.GetDatafile(path);

            playersData[userid] = profile;
        }

        void SavePlayerData(string userid)
        {
            string path = subDirectory + userid;
            Interface.GetMod().DataFileSystem.SaveDatafile(path);
        }

        void SaveKnownPlayers()
        {
            Interface.GetMod().DataFileSystem.WriteObject("PlayerDatabase", storedData);
        }

        ////////////////////////////////////////////////////////////
        // SQLite
        ////////////////////////////////////////////////////////////

        Ext.SQLite.Libraries.SQLite Sqlite = Interface.GetMod().GetLibrary<Ext.SQLite.Libraries.SQLite>();
        Connection Sqlite_conn;

        List<string> sqliteColumns = new List<string>();

        Dictionary<string, Hash<string, string>> sqliteData = new Dictionary<string, Hash<string, string>>();

        bool isValidColumn(string column) => sqliteColumns.Contains(column);

        void CreateNewColumn(string column)
        {
            Sqlite.Insert(Core.Database.Sql.Builder.Append(string.Format("ALTER TABLE PlayerDatabase ADD COLUMN '{0}' TEXT", column)), Sqlite_conn);
            sqliteColumns.Add(column);
        }

        void LoadSQLite()
        {
            try
            {
                Sqlite_conn = Sqlite.OpenDb(sqlitename, this);
                if (Sqlite_conn == null)
                {
                    FatalError("Couldn't open the SQLite PlayerDatabase. ");
                    return;
                }
                Sqlite.Insert(Core.Database.Sql.Builder.Append("CREATE TABLE IF NOT EXISTS PlayerDatabase ( id INTEGER NOT NULL PRIMARY KEY UNIQUE, userid TEXT );"), Sqlite_conn);
                Sqlite.Query(Core.Database.Sql.Builder.Append("PRAGMA table_info(PlayerDatabase);"), Sqlite_conn, list =>
                {
                    if (list == null)
                    {
                        FatalError("Couldn't get columns. Database might be corrupted.");
                        return;
                    }
                    foreach (var entry in list)
                    {
                        sqliteColumns.Add((string)entry["name"]);
                    }

                });
                Sqlite.Query(Core.Database.Sql.Builder.Append("SELECT userid from PlayerDatabase"), Sqlite_conn, list =>
                {
                    if (list == null) return;
                    foreach (var entry in list)
                    {
                        string steamid = (string)entry["userid"];
                        if (steamid != "0")
                            sqliteData.Add(steamid, new Hash<string, string>());
                    }
                });
            }
            catch (Exception e)
            {
                FatalError(e.Message);
            }
        }

        void LoadPlayerSQLite(string userid)
        {
            if (!sqliteData.ContainsKey(userid)) { sqliteData.Add(userid, new Hash<string, string>()); }
            bool newplayer = true;
            Sqlite.Query(Core.Database.Sql.Builder.Append(string.Format("SELECT * from PlayerDatabase WHERE userid == {0}", userid)), Sqlite_conn, list =>
            {
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        foreach (var p in entry)
                        {
                            sqliteData[userid][p.Key] = (string)p.Value;

                        }
                        newplayer = false;
                    }
                }
                if (newplayer)
                {
                    sqliteData[userid]["userid"] = userid;
                    Sqlite.Insert(Core.Database.Sql.Builder.Append(string.Format("INSERT OR REPLACE INTO PlayerDatabase ( userid ) VALUES ( {0} )", userid)), Sqlite_conn);

                    changedPlayersData.Add(userid);
                }
            });
        }

        void SavePlayerSQLite(string userid)
        {
            var values = sqliteData[userid];
            var i = values.Count;

            string arg = string.Empty;
            var parms = new List<object>();
            foreach (var c in values)
            {
                arg += string.Format("{0}`{1}` = @{2}", arg == string.Empty ? string.Empty : ",", c.Key, parms.Count.ToString());
                parms.Add( c.Value);
            }

            Sqlite.Insert(Core.Database.Sql.Builder.Append(string.Format("UPDATE PlayerDatabase SET {0} WHERE userid = {1}", arg, userid), parms.ToArray()), Sqlite_conn);
        }


        ////////////////////////////////////////////////////////////
        // MySQL
        ////////////////////////////////////////////////////////////

        Ext.MySql.Libraries.MySql Sql = Interface.GetMod().GetLibrary<Ext.MySql.Libraries.MySql>();
        Connection Sql_conn;

        List<string> sqlColumns = new List<string>();

        Dictionary<string, Hash<string, string>> sqlData = new Dictionary<string, Hash<string, string>>();

        bool isValidColumn2(string column) => sqlColumns.Contains(column);

        void CreateNewColumn2(string column)
        {
            Sql.Insert(Core.Database.Sql.Builder.Append(string.Format("ALTER TABLE `playerdatabase` ADD `{0}` LONGTEXT", column)), Sql_conn);
            sqlColumns.Add(column);
        }

        void LoadMySQL()
        {
            try
            {
                Sql_conn = Sql.OpenDb(sql_host, sql_port, sql_db, sql_user, sql_pass, this);
                if (Sql_conn == null || Sql_conn.Con == null)
                {
                    FatalError("Couldn't open the SQLite PlayerDatabase: " + Sql_conn.Con.State.ToString());
                    return;
                }
                Sql.Insert(Core.Database.Sql.Builder.Append("CREATE TABLE IF NOT EXISTS playerdatabase ( `id` int(11) NOT NULL, `userid` VARCHAR(17) NOT NULL );"), Sql_conn);
                Sql.Query(Core.Database.Sql.Builder.Append("desc playerdatabase;"), Sql_conn, list =>
                {
                    if (list == null)
                    {
                        FatalError("Couldn't get columns. Database might be corrupted.");
                        return;
                    }
                    foreach (var entry in list)
                    {
                        sqlColumns.Add((string)entry["Field"]);
                    }

                });
                Sql.Query(Core.Database.Sql.Builder.Append("SELECT userid from playerdatabase"), Sql_conn, list =>
                {
                    if (list == null) return;
                    foreach (var entry in list)
                    {
                        string steamid = (string)entry["userid"];
                        if (steamid != "0")
                            sqlData.Add(steamid, new Hash<string, string>());
                    }
                });
            }
            catch (Exception e)
            {
                FatalError(e.Message);
            }
        }

        void LoadPlayerSQL(string userid)
        {
            if (!sqlData.ContainsKey(userid)) sqlData.Add(userid, new Hash<string, string>());
            bool newplayer = true;
            Sql.Query(Core.Database.Sql.Builder.Append(string.Format("SELECT * from playerdatabase WHERE `userid` = '{0}'", userid)), Sql_conn, list =>
            {
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        foreach (var p in entry)
                        {
                            sqlData[userid][p.Key] = (string)p.Value;
                        }
                        newplayer = false;
                    }
                }
                if (newplayer)
                {
                    sqlData[userid]["userid"] = userid;
                    Sql.Insert(Core.Database.Sql.Builder.Append(string.Format("INSERT IGNORE INTO playerdatabase ( userid ) VALUES ( {0} )", userid)), Sql_conn);

                    changedPlayersData.Add(userid);
                }
            });
        }

        void SavePlayerSQL(string userid)
        {
            var values = sqlData[userid];

            string arg = string.Empty;
            var parms = new List<object>();
            foreach (var c in values)
            {
                arg += string.Format("{0}`{1}` = @{2}", arg == string.Empty ? string.Empty : ",", c.Key, parms.Count.ToString());
                parms.Add(c.Value);
            }

            Sql.Insert(Core.Database.Sql.Builder.Append(string.Format("UPDATE playerdatabase SET {0} WHERE userid = {1}", arg, userid), parms.ToArray()), Sql_conn);
        }
    }
}
