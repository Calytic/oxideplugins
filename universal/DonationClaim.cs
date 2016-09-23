using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Ext.MySql;
using Oxide.Core.Database;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("DonationClaim", "Wulf/lukespragg", "1.0.1", ResourceId = 1923)]
    [Description("Players can claim rewards for automatic PayPal donations")]

    class DonationClaim : CovalencePlugin
    {
        readonly Ext.MySql.Libraries.MySql mySql = new Ext.MySql.Libraries.MySql();
        Connection connection;
        DefaultConfig config;

        #region Configuration

        class DefaultConfig
        {
            readonly List<string> exampleCommands = new List<string>();
            public readonly Dictionary<string, List<string>> Packages = new Dictionary<string, List<string>>();

            public DefaultConfig()
            {
                exampleCommands.Add("grant user {0} some.permission");
                exampleCommands.Add("grant user {0} another.permission");
                Packages.Add("Example", exampleCommands);
                Packages.Add("VIP", exampleCommands);
            }

            public string DatabaseHost = "localhost";
            public int DatabasePort = 3306;
            public string DatabaseName = "oxide";
            public string DatabaseUser = "root";
            public string DatabasePassword = "changeme";
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            Config.Clear();

            config = new DefaultConfig();
            Config.WriteObject(config, true);

            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>            {
                ["Claimed"] = "You claimed the {0} donation package. Thank you for your donation!",
                ["NoPackage"] = "Package {0} could not be found! Please notify an admin",
                ["NoUnclaimed"] = "No unclaimed rewards available for email address: {0}"
            }, this);
        }

        #endregion

        void Init()
        {
            try
            {
                config = Config.ReadObject<DefaultConfig>();
            }
            catch
            {
                PrintWarning("Could not read config, creating new default config");
                LoadDefaultConfig();
            }

            LoadDefaultMessages();
        }

        [Command("claim", "claimdonation", "claimreward")]
        void ChatCommand(IPlayer player, string command, string[] args)
        {
            var playerEmail = string.Join("", args).Replace("@", "@@");
            string packageClaimed;

            connection = mySql.OpenDb(config.DatabaseHost, config.DatabasePort, config.DatabaseName, config.DatabaseUser, config.DatabasePassword, this);
            var sql = Sql.Builder.Append($"call {config.DatabaseName}.claim_donation('" + playerEmail + "');");
            mySql.Query(sql, connection, list =>
            {
                var sb = new StringBuilder();
                foreach (var entry in list)
                {
                    sb.AppendFormat("{0}", entry["item_name"]);
                    sb.AppendLine();
                }

                packageClaimed = sb.ToString();
                var packageKey = GetPackageKey(packageClaimed, config.Packages);

                if (packageClaimed.Length < 3)
                {
                    Reply(player, Lang("NoUnclaimed", player.Id, playerEmail));
                }
                else
                {
                    List<string> consoleCommands;
                    if (config.Packages.TryGetValue(packageKey, out consoleCommands))
                    {
                        RunConsoleCommands(consoleCommands, player.Id);
                        Reply(player, Lang("Claimed", player.Id, packageClaimed));
                        Puts($"{player} has claimed donation package {packageClaimed}");
                    }
                    else
                    {
                        Reply(player, Lang("NoPackage", player.Id, packageClaimed));
                        Puts($"{player} tried to claim {packageClaimed}, but the package could not be found in the config!");
                    }
                }
            });
        }

        static string GetPackageKey(string packageName, Dictionary<string, List<string>> packages)
        {
            foreach (var entry in packages)
                if (packageName.Contains(entry.Key)) return entry.Key;
            return "";
        }

        void RunConsoleCommands(List<string> commandsList, string playerName)
        {
            foreach (var command in commandsList) server.Command(string.Format(command, playerName));
        }

        #region Helpers

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T) Convert.ChangeType(Config[name], typeof (T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        static void Reply(IPlayer player, string message, params object[] args) => player.Reply(string.Format(message, args));

        #endregion
    }
}
