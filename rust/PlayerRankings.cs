using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("PlayerRankings", "Ankawi", "1.4.0")]
    [Description("Gives players ranks based on playtime on a server")]
    class PlayerRankings : RustPlugin
    {
        [PluginReference]
        Plugin ConnectionDB;
        [PluginReference]
        Plugin BetterChat;

        ////////////////////////////////////////
        ///  Plugin Related
        ////////////////////////////////////////

        void Loaded()
        {
            if (!ConnectionDB)
                PrintWarning("You need to have ConnectionDB installed for this plugin to work. Get it here: http://oxidemod.org/plugins/1459/");
            if (!BetterChat)
                PrintWarning("Its recommended to have BetterChat installed, to grant titles for playtime. Get it here: http://oxidemod.org/plugins/979/");

            LoadConfig();

            timer.Repeat(15, 0, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    UpdateTitles(player);
            });

            foreach (var rank in Config)
            {
                if (rank.Key == "Settings")
                    continue;

                if (!permission.PermissionExists(Config[rank.Key, "Permission"].ToString()))
                    permission.RegisterPermission(Config[rank.Key, "Permission"].ToString(), this);
            }
        }

        ////////////////////////////////////////
        ///  Config Related
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Player", "Permission", "betterchat.player");
            SetConfig("Player", "Playtime", "1.0");

            SetConfig("Regular", "Permission", "betterchat.regular");
            SetConfig("Regular", "Playtime", "10.0");

            SetConfig("Pro", "Permission", "betterchat.pro");
            SetConfig("Pro", "Playtime", "25.0");

            SetConfig("Settings", "Ignore Admins", false);

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file...");
        }

        ////////////////////////////////////////
        ///  Config Setup - by LaserHydra
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        ////////////////////////////////////////
        ///  Commands
        ////////////////////////////////////////

        [ChatCommand("ranks")]
        private void RanksCommand(BasePlayer player, string command, string[] args)
        {
            foreach (var rank in Config)
            {
                if (rank.Key == "Settings")
                    continue;

                PrintToChat(player, $"<color=red>Rank</color>: {rank.Key}" + "\n<color=lime>Playtime Required:</color> " + Convert.ToString(Config[rank.Key, "Playtime"]) + " hours");
            }

            PrintToChat(player, $"<color=red>Your Playtime</color>: " + Math.Round(GetPlayTime(player), 2) + " hours");
        }

        ////////////////////////////////////////
        ///  Subject Related
        ////////////////////////////////////////

        void UpdateTitles(BasePlayer player)
        {
            if (player.net.connection.authLevel != 0 && (bool)Config["Settings", "Ignore Admins"]) return;
            if (!ConnectionDB) return;

            double playTime = GetPlayTime(player);

            Dictionary<string, object> newRank = new Dictionary<string, object>{
                {"Permission", ""},
                {"Playtime", 0.0},
                {"Name", "none"}
            };

            foreach (KeyValuePair<string, object> rank in Config)
            {
                if (rank.Key == "Settings")
                    continue;

                double time = Convert.ToDouble(Config[rank.Key, "Playtime"]);

                if (playTime >= time && time > Convert.ToDouble(newRank["Playtime"]))
                {
                    newRank = rank.Value as Dictionary<string, object>;
                    newRank["Name"] = rank.Key;
                }
            }

            if (!permission.UserHasPermission(player.UserIDString, newRank["Permission"] as string) && permission.PermissionExists(newRank["Permission"] as string))
            {
                SendReply(player, $"<color=red>PlayerRankings</color>: You have been ranked up to {newRank["Name"] as string}");
                Puts($"{player.displayName} has been ranked up to {newRank["Name"] as string}");
                Permission("grant", player.UserIDString, newRank["Permission"] as string);
                RevokeLower(player, Convert.ToDouble(newRank["Playtime"]));
            }
        }

        void RevokeLower(BasePlayer player, double time)
        {
            foreach (var rank in Config)
            {
                if (time > Convert.ToDouble(Config[rank.Key, "Playtime"]))
                {
                    if (permission.UserHasPermission(player.UserIDString, Config[rank.Key, "Permission"] as string))
                        Permission("revoke", player.UserIDString, Config[rank.Key, "Permission"] as string);
                }
            }
        }

        double GetPlayTime(BasePlayer player)
        {
            if (ConnectionDB)
            {
                return Convert.ToDouble(ConnectionDB.Call("SecondsPlayed", player)) / 60 / 60;
            }

            return 0;
        }

        void Permission(string action, string uid, string perm)
        {
            ConsoleSystem.Run.Server.Normal(action, "user", uid, perm);
        }
    }
}