// Reference: Facepunch.SQLite

using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Facepunch;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PlayerDatabase", "Reneb", "1.4.1")]
    class PlayerDatabase : CovalencePlugin
    {
        List<string> changedPlayersData = new List<string>();

        DataType dataType = DataType.SQLite;

        enum DataType
        {
            Files,
            SQLite
        }

        ////////////////////////////////////////////////////////////
        // Configs
        ////////////////////////////////////////////////////////////

        static int dataTypeCfg = 1;

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
            CheckCfg<int>("Data Type : 0 (Files) or 1 (SQLite)", ref dataTypeCfg);
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
            timer.Once(0.01f, () => Interface.Oxide.UnloadPlugin("PlayerDatabase"));
        }

        string GetMsg(string key, object steamid = null) => lang.GetMessage(key, this, steamid == null ? null : steamid.ToString());

        List<string> KnownPlayers() => dataType == DataType.SQLite ? sqlData.Keys.ToList() : storedData.knownPlayers.ToList();

        bool isKnownPlayer(string userid) => dataType == DataType.SQLite ? sqlData.ContainsKey(userid) : storedData.knownPlayers.Contains(userid);

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
                case DataType.Files:
                    LoadFiles();
                    break;
                case DataType.SQLite:
                    LoadSQLite();
                    break;
                default:
                    FatalError("Wrong DataType");
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
            if (dataType == DataType.Files)
            {
                LoadPlayerData(userid);
            }
            else if (dataType == DataType.SQLite)
            {
                LoadPlayerSQLite(userid);
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
                    if (dataType == DataType.Files)
                    {
                        SavePlayerData(userid);
                    }
                    else if (dataType == DataType.SQLite)
                    {
                        SavePlayerSQLite(userid);
                    }
                }
                catch(Exception e)
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

            if (dataType == DataType.Files)
            {
                var profile = playersData[userid];

                profile[key] = JsonConvert.SerializeObject(data);
                playersData[userid] = profile;
            }
            else if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key))
                {
                    CreateNewColumn(key);
                }
                sqlData[userid][key] = JsonConvert.SerializeObject(data);
            }

            if (!changedPlayersData.Contains(userid))
                changedPlayersData.Add(userid);
        }

        object GetPlayerDataRaw(string userid, string key)
        {
            if (!isKnownPlayer(userid)) return null;

            if (dataType == DataType.Files)
            {
                var profile = playersData[userid];
                if (profile[key] == null) return null;
                return (string)profile[key];
            }
            else if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key)) return null;
                if (sqlData[userid] == null) return null;
                return (string)sqlData[userid][key];
            }
            return null;
        }
        object GetPlayerData(string userid, string key)
        {
            if (!isKnownPlayer(userid)) return null;

            if (dataType == DataType.Files)
            {
                var profile = playersData[userid];
                if (profile[key] == null) return null;
                return JsonConvert.DeserializeObject((string)profile[key]);
            }
            else if (dataType == DataType.SQLite)
            {
                if (!isValidColumn(key)) return null;
                if (sqlData[userid] == null) return null;
                return JsonConvert.DeserializeObject((string)sqlData[userid][key]);
            }
            return null;
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

        SQLite storedSql;

        List<string> sqliteColumns = new List<string>();

        Dictionary<string, Hash<string, string>> sqlData = new Dictionary<string, Hash<string, string>>();

        bool isValidColumn(string column) => sqliteColumns.Contains(column);

        void CreateNewColumn(string column)
        {
            storedSql.Execute(string.Format("ALTER TABLE PlayerDatabase ADD COLUMN '{0}' TEXT", column));
            sqliteColumns.Add(column);
        }

        void LoadSQLite()
        {
            try
            {
                storedSql = new SQLite();
                if (!storedSql.Open("playerdatabase"))
                {
                    FatalError("Couldn't open the SQLite PlayerDatabase. ");
                    return;
                }
                storedSql.Execute("CREATE TABLE IF NOT EXISTS PlayerDatabase ( id INTEGER NOT NULL PRIMARY KEY UNIQUE, userid TEXT );", new object[0]);
                IntPtr intPtr = storedSql.Query("PRAGMA table_info(PlayerDatabase);");
                if (intPtr == IntPtr.Zero)
                {
                    FatalError("Couldn't get columns. Database might be corrupted.");
                    return;
                }
                while (storedSql.StepRow(intPtr))
                {
                    sqliteColumns.Add(storedSql.GetColumnValue<string>(intPtr, 1, null));
                }
                storedSql.QueryFinalize(intPtr);

                IntPtr intPtr2 = storedSql.Query("SELECT userid from PlayerDatabase");
                if (intPtr2 == IntPtr.Zero)
                {
                    return;
                }
                while (storedSql.StepRow(intPtr2))
                {
                    string steamid = storedSql.GetColumnValue<string>(intPtr2, 0, "0");
                    if (steamid != "0")
                        sqlData.Add(steamid, new Hash<string, string>());
                }
                storedSql.QueryFinalize(intPtr2);
            }
            catch (Exception e) {
                FatalError(e.Message);
            }
        }

        void LoadPlayerSQLite(string userid)
        {
            IntPtr intPtr = storedSql.Query("SELECT * from PlayerDatabase WHERE userid == ?1", new object[]
               {
                    userid
               });


            if (!sqlData.ContainsKey(userid)) sqlData.Add(userid, new Hash<string, string>());
            if (intPtr == IntPtr.Zero)
            {
                return;
            }

            while (storedSql.StepRow(intPtr))
            {
                Interface.Oxide.LogWarning(intPtr.ToString());
                for (int i = 2; i < storedSql.Columns(intPtr); i++)
                {
                    sqlData[userid][sqliteColumns[i]] = storedSql.GetColumnValue<string>(intPtr, i, string.Empty);
                }
            }
            if (sqlData[userid].Count == 0)
            {
                sqlData[userid]["userid"] = userid;
                storedSql.Execute("BEGIN TRANSACTION");
                storedSql.Insert("INSERT OR REPLACE INTO PlayerDatabase ( userid ) VALUES ( ?1 )", new object[] { userid });
                storedSql.Execute("END TRANSACTION");

                changedPlayersData.Add(userid);
            }

            storedSql.QueryFinalize(intPtr);
        }

        void SavePlayerSQLite(string userid)
        {
            storedSql.Execute("BEGIN TRANSACTION");
            var values = sqlData[userid];

            string arg = string.Empty;
            foreach (var c in values)
            {
                arg += string.Format("{0}`{1}` = '{2}'", arg == string.Empty ? string.Empty : ",", c.Key, c.Value);
            }

            storedSql.Insert(string.Format("UPDATE PlayerDatabase SET {0} WHERE userid = {1}", arg, userid));
            storedSql.Execute("END TRANSACTION", new object[0]);
        }


        

        

        

       

        
    }
}
