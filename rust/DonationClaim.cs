using System.Text.RegularExpressions;
using Oxide.Ext.MySql;
using System.Text;
using Oxide.Core;
using Oxide.Game.Rust.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace Oxide.Plugins

{
    [Info("Donation Claim", "LeoCurtss", "0.5")]
    [Description("Player can claim rewards from PayPal donations.")]

    class DonationClaim : RustPlugin
    {

        private readonly Ext.MySql.Libraries.MySql _mySql = Interface.GetMod().GetLibrary<Ext.MySql.Libraries.MySql>();
        private Ext.MySql.Connection _mySqlConnection;

        class DCConfig
        {
            public DCConfig()
            {
                ExampleCommands.Add("grant user {0} some.permission");
                ExampleCommands.Add("grant user {0} another.permission");

                Packages.Add("Example Package", ExampleCommands);
                Packages.Add("Example Package 2", ExampleCommands);
            }
            public string MySQLIP = "localhost";
            public int MySQLPort = 3306;
            public string MySQLDatabase = "rustserver";
            public string MySQLusername = "root";
            public string MySQLpassword = "";

            List<string> ExampleCommands = new List<string>();
            public Dictionary<string, List<string>> Packages = new Dictionary<string, List<string>>();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();

            dc_config = new DCConfig();
            Config.WriteObject(dc_config, true);

            SaveConfig();
        }

        void Loaded()
        {
            try
            {
                dc_config = Config.ReadObject<DCConfig>();
            }
            catch
            {
                Puts("Could not read config, creating new default config.");
                LoadDefaultConfig();
            }

            //Lang API dictionary
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DC_NoUnclaimed"] = "There are no unclaimed rewards available for that email address: {0}",
                ["DC_Claimed"] = "You have claimed the {0} donation package.  Thank you for your donation!",
                ["DC_NoPackage"] = "Package {0} could not be found in the config file!  Notify an admin to update the configuration."
            }, this);
        }

        private DCConfig dc_config;

        private string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }

        [ChatCommand("claimreward")]
        void ClaimRewardCommand(BasePlayer player, string command, string[] args)
        {
            string playerEmail = string.Join("", args);

            playerEmail = playerEmail.Replace("@", "@@");

            string packageClaimed = "";

			_mySqlConnection = _mySql.OpenDb(dc_config.MySQLIP, dc_config.MySQLPort, dc_config.MySQLDatabase, dc_config.MySQLusername, dc_config.MySQLpassword, this);
            var sql = Ext.MySql.Sql.Builder.Append("CALL rustserver.claim_donation('" + playerEmail + "');");
            _mySql.Query(sql, _mySqlConnection, list =>
            {

                var sb = new StringBuilder();
                foreach (var entry in list)
                {
                    sb.AppendFormat("{0}", entry["item_name"]);
                    sb.AppendLine();
                }

                packageClaimed = sb.ToString();

                string packageKey = getPackageKey(packageClaimed, dc_config.Packages);

                if (packageClaimed.Length < 3)
                {
                    SendReply(player, string.Format(GetMessage("DC_NoUnclaimed", player.UserIDString), playerEmail.Replace("@@", "@")));
                }
                else
                {

                    List<string> ConsoleCommands;
					if (dc_config.Packages.TryGetValue(packageKey, out ConsoleCommands))
                    {
						RunConsoleCommands(ConsoleCommands, player.UserIDString);

                        SendReply(player, string.Format(GetMessage("DC_Claimed", player.UserIDString), packageClaimed));
                        Puts(player + " has claimed donation package " + packageClaimed);
                    }
                    else
                    {
                        SendReply(player, string.Format(GetMessage("DC_NoPackage", player.UserIDString), packageClaimed));
                        Puts(player + " tried to claim " + packageClaimed + "but the package could not be found in the config!");
                    }
                }

            });
        }


    private string getPackageKey(string packageName, Dictionary<string, List<string>> packages)
        {
            foreach (KeyValuePair<string, List<string>> entry in packages)
            {
                if (packageName.Contains(entry.Key))
                {
                    return entry.Key;
                }
            }
            return "";
        }

        void RunConsoleCommands(List<string> CommandsList, string playerName)
        {
            foreach (string cmmnd in CommandsList)
            {
                ConsoleSystem.Run.Server.Normal(String.Format(cmmnd, playerName));
            }
        }

    }

}