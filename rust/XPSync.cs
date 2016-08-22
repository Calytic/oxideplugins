using Oxide.Core.Database;
using Oxide.Core;
using Oxide.Ext.MySql;

namespace Oxide.Plugins
{
    [Info("XP Sync", "PaiN", 0.2, ResourceId = 2072)]
    class XPSync : RustPlugin
    {
        public static readonly Oxide.Ext.MySql.Libraries.MySql _mySql = Interface.Oxide.GetLibrary<Oxide.Ext.MySql.Libraries.MySql>();
        public static Connection _mySqlConnection;

        ConfigFile Cfg = new ConfigFile();

        class ConfigFile
        {
            public string Host = "localhost";
            public int Port = 3306;
            public string Database = "databasename";
            public string User = "root";
            public string Password = "";
        }

        void Loaded()
        {
            Cfg = Config.ReadObject<ConfigFile>();

            _mySqlConnection = _mySql.OpenDb(Cfg.Host, Cfg.Port, Cfg.Database, Cfg.User, Cfg.Password, this);
            if (_mySqlConnection == null)
            {
                Puts("Can't connect to the database!");
                return;
            }

            var sql = Sql.Builder.Append(@"CREATE TABLE IF NOT EXISTS xpsync (
                                 id BIGINT(32),
                                 xp FLOAT
                               );");

            _mySql.ExecuteNonQuery(sql, _mySqlConnection);
        }

        void Unloaded() => _mySql.CloseDb(_mySqlConnection);
        protected override void LoadDefaultConfig() { PrintWarning("Creating a new configuration file . . ."); Config.WriteObject(Cfg, true); }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (_mySqlConnection == null)
            {
                Puts("Can't connect to the database to save player's xp!");
                return;
            }

            var agent = BasePlayer.FindXpAgent(player.userID);

            if (agent.EarnedXp == 0)
                return;

            var sql = Sql.Builder.Append($"SELECT * FROM xpsync WHERE id={player.userID};");

            _mySql.Query(sql, _mySqlConnection, list =>
            {
                if (list.Count == 0)
                {
                    _mySql.Query(Sql.Builder.Append($"INSERT INTO xpsync (`id`, `xp`) VALUES ('{player.userID}', '{agent.EarnedXp}');"), _mySqlConnection, callback => { });
                }
                else
                {
                    if (agent.EarnedXp < System.Convert.ToSingle(list[0]["xp"]))
                        return;

                    _mySql.Query(Sql.Builder.Append($"UPDATE xpsync SET xp={agent.EarnedXp} WHERE  id={player.userID};"), _mySqlConnection, callback => { });
                }
            });
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (_mySqlConnection == null)
            {
                Puts("Can't connect to the database to load player's xp!");
                return;
            }

            _mySql.Query(Sql.Builder.Append($"SELECT * FROM xpsync WHERE id={player.userID};"), _mySqlConnection, list =>
            {
                if (list.Count == 0)
                    return;

                if (BasePlayer.FindXpAgent(player.userID).EarnedXp >= System.Convert.ToSingle(list[0]["xp"]))
                    return;

                player.xp.Reset();
                player.xp.Add(Rust.Xp.Definitions.Cheat, System.Convert.ToSingle(list[0]["xp"]));
            });
        }
    }
}