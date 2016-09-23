using System;
using System.Linq;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Autokick", "Exel80", "1.1.1", ResourceId = 2138)]
    [Description("Autokick help you change your server to \"maintenance break\" mode, if you need it!")]
    class Autokick : CovalencePlugin
    {
        #region Helper
        public bool DEBUG = false;
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private void Loaded()
        {
            LoadConfigValues();

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Toggle"] = "Autokick is [#yellow]{0}[/#]",
                ["KickHelp"] = "When Autokick is [#yellow]{0}[/#], use [#yellow]{1}[/#] to kick all online players.",
                ["Kicked"] = "All online players has been kicked! Except players that have [#yellow]{0}[/#] permission or is admin.",
                ["Set"] = "Message is now setted to \"[#yellow]{0}[/#]\"",
                ["Message"] = "AutoKick message is \"[#yellow]{0}[/#]\"",
                ["ToggleHint"] = "Autokick must be [#yellow]{0}[/#], before can execute [#yellow]{1}[/#] command!",
                ["Usage"] = "[#cyan]Usage:[/#] [#silver]{0}[/#] [#grey]{1}[/#]"
            }, this);
        }
        #endregion

        #region Commands
        [Command("ak", "autokick")]
        private void cmdAK(IPlayer player, string command, string[] args)
        {
            if (!hasPermission(player, "autokick.use"))
                return;

            string arg = String.Empty;
            string _name = player.Name;
            string _id = player.Id.ToString();

            // Check that args isn't empty.
            if (args.Length < 1)
            {
                Chat(player, Lang("Usage", _id, $"/{command}", "on/off | kick | set | message"));
                return;
            }
            else
                arg = args[0];

            Debug(player, $"arg: {arg} - Name: {_name} - Id: {_id} - isAdmin: {player.IsAdmin}");

            switch (arg)
            {
                case "on":
                    {
                        // Change Toggle from config file
                        config.Settings["Enabled"] = "true";
                        Config["Settings", "Enabled"] = "true";

                        // Save confs
                        Config.Save();

                        // Print Toggle
                        Chat(player, Lang("Toggle", _id, "ACTIVATED!") + "\n" + Lang("KickHelp", _id, "true", "/ak kick"));
                        Debug(player, $"Changed Toggle to {Config["Settings", "Enabled"]}");
                    }
                    break;
                case "kick":
                    {
                        // Check if Toggle isn't False
                        if (config.Settings["Enabled"] != "true")
                        {
                            Chat(player, Lang("ToggleHint", _id, "true", "/ak kick"));
                            return;
                        }
                        // Kick all players (Except if config allow auth 1 and/or 2 to stay)
                        foreach (IPlayer clients in players.Connected.ToList())
                            Kicker(clients);

                        Chat(player, Lang("Kicked", _id, "autokick.join"));
                    }
                    break;
                case "off":
                    {
                        // Change Toggle from config file
                        config.Settings["Enabled"] = "false";
                        Config["Settings", "Enabled"] = "false";

                        // Save confs
                        Config.Save();

                        // Print Toggle
                        Chat(player, Lang("Toggle", _id, "DE-ACTIVATED!"));
                        Debug(player, $"Changed Toggle to {Config["Settings", "Enabled"]}");
                    }
                    break;
                case "set":
                    {
                        // Try catch for making sure that script wont broke if player forgot set message.
                        try
                        {
                            // Read all args to one string with space.
                            string _arg = string.Join(" ", args).Remove(0, 4);

                            // Change KickMessage from config file
                            config.Settings["KickMessage"] = _arg;
                            Config["Settings", "KickMessage"] = _arg;

                            // Save confs
                            Config.Save();

                            // Print KickMessage
                            Chat(player, Lang("Set", _id, config.Settings["KickMessage"]));
                        }
                        catch (Exception e) { Chat(player, Lang("Usage", _id, $"/{command}", "on/off | kick | set | message")); Puts($"{e.GetBaseException()}"); }
                    }
                    break;
                case "message":
                    {
                        // Print KickMessage
                        Chat(player, Lang("Message", _id, config.Settings["KickMessage"]));
                    }
                    break;
            }

        }
        #endregion

        #region PlayerJoin
        void OnPlayerInit(IPlayer player)
        {
            if (config.Settings["Enabled"].ToLower() == "true")
                timer.Once(8f, SomeoneTryConnect);
        }
        private void SomeoneTryConnect()
        {
            foreach (IPlayer player in players.Connected.ToList())
            {
                string _name = player.Name;
                string _id = player.Id.ToString();
                string message = config.Settings["KickMessage"];

                if (DEBUG) Puts($"[Deubg] Name: {_name}, Id: {_id}, isAdmin: {player.IsAdmin}");

                if (hasPermission(player, "autokick.join"))
                    return;

                if (player.IsConnected)
                    player.Kick(message);
            }
        }
        #endregion

        #region Kicker
        private void Kicker(IPlayer player)
        {
            try
            {
                if (config.Settings["Enabled"].ToLower() == "true")
                {
                    string _name = player.Name;
                    string _id = player.Id.ToString();
                    string message = config.Settings["KickMessage"];

                    if (DEBUG) Puts($"[Deubg] Name: {_name}, Id: {_id}");

                    if (hasPermission(player, "autokick.join"))
                        return;

                    if (player.IsConnected)
                        player.Kick(message);
                }
            }
            catch (Exception) { }
        }
        #endregion

        #region codeCleaner
        private void Chat(IPlayer player, string msg) => player.Reply(covalence.FormatText(config.Settings["Prefix"] + " " + msg));
        private void Debug(IPlayer player, string msg)
        {
            if(DEBUG)
                Puts($"[Debug] {player.Name} - {msg}");
        }
        bool hasPermission(IPlayer player, string permissionName)
        {
            if (player.IsAdmin) return true;
            return permission.UserHasPermission(player.Id.ToString(), permissionName);
        }
        #endregion

        #region Configuration Defaults
        PluginConfig DefaultConfig()
        {
            var defaultConfig = new PluginConfig
            {
                Settings = new Dictionary<string, string>
                {
                    { PluginSettings.Prefix, "[#cyan][AutoKick][/#]" },
                    { PluginSettings.KickMessage, "You have automaticly kicked, Server is on maintenance break!" },
                    { PluginSettings.Enabled, "false" },
                }
            };
            return defaultConfig;
        }
        #endregion

        #region Configuration Setup
        private PluginConfig config;

        class PluginSettings
        {
            public const string Prefix = "Prefix";
            public const string KickMessage = "KickMessage";
            public const string Enabled = "Enabled";
        }

        class PluginConfig
        {
            public Dictionary<string, string> Settings { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(DefaultConfig(), true);
        }

        void LoadConfigValues()
        {
            config = Config.ReadObject<PluginConfig>();
            var defaultConfig = DefaultConfig();
            Merge(config.Settings, defaultConfig.Settings);
        }

        void Merge<T1, T2>(IDictionary<T1, T2> current, IDictionary<T1, T2> defaultDict)
        {
            foreach (var pair in current)
            {
                if (defaultDict.ContainsKey(pair.Key)) continue;
                defaultDict[pair.Key] = pair.Value;
            }
            var oldPairs = current.Keys.Except(defaultDict.Keys).ToList();
            foreach (var oldPair in oldPairs)
            {
                current.Remove(oldPair);
            }
        }
        #endregion
    }
}