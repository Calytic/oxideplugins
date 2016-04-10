using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Welcome Message", "SkinN", "2.0.3", ResourceId = 1509)]
    [Description("Message to welcome a player to the server")]

    class WelcomeMessage : HurtworldPlugin
    {
        #region Plugin Resources

        /* The Developer mode is used mostly for debugging or testing features
           of the plugin, also forces the default configuration file
           on each load */

        // Developer Variables
        private const bool Dev = false;

        // Plugin Variables
        private List<string> JoinQueue = new List<string>();

        // Configuration Variables
        private List<object> Messages;

        #endregion

        #region Configuraiton

        protected override void LoadDefaultConfig()
        {
            /* Hook called when the config for the plugin initializes */

            Puts("Creating new configuration file");

            // Clear configuration for a brand new one
            Config.Clear();

            // Load plugin variables
            LoadVariables();
        }

        void LoadVariables()
        {
            /* Method to setup the global variables with all configuration values */

            // Clear configuration if on developer mode
            if (Dev)
                Config.Clear();

            // Advert Messages
            Messages = GetConfig<List<object>>("Messages", new List<object>(new string[] {
                        "<size=17><silver>Welcome <lime>{player}<end><end></size>",
                        "<silver><orange><size=20>â¢</size><end> Type <orange>/help<end> for all available commands.<end>",
                        "<silver><orange><size=20>â¢</size><end> Be respectful to other players.<end>"
                    }
                )
            );

            // Save file
            SaveConfig();
        }

        T GetConfig<T>(params object[] args)
        {
            /* Gets a value from the configuration file
                Method Notes:
                - Developer mode forces the default values on every setting on each load */

            var stringArgs = new string[args.Length - 1];
            for (var i = 0; i < args.Length - 1; i++)
                stringArgs[i] = args[i].ToString();

            if (Config.Get(stringArgs) == null || Dev)
                Config.Set(args);

            return (T)Convert.ChangeType(Config.Get(stringArgs), typeof(T));
        }

        #endregion configuration

        #region Plugin Hooks / Methods

        void Init()
        {
            /* Hook called when plugin initializes */

            // Load plugin variables
            LoadVariables();
        }

        public string SimpleColorFormat(string text, bool removeTags = false)
        {
            /*  Simple Color Format ( v3.0 )
                Formats simple color tags to HTML */

            // All patterns
            Regex end = new Regex(@"\<(end?)\>"); // End tags
            Regex clr = new Regex(@"\<(\w+?)\>"); // Names
            Regex hex = new Regex(@"\<(#\w+?)\>"); // Hex codes

            if (removeTags)
            {   
                // Remove tags
                text = end.Replace(text, "");
                text = clr.Replace(text, "");
                text = hex.Replace(text, "");
            }
            else
            {   
                // Replace tags
                text = end.Replace(text, "</color>");
                text = clr.Replace(text, "<color=$1>");
                text = hex.Replace(text, "<color=$1>");
            }

            return text;
        }

        #endregion

        #region Player Hooks

        void OnPlayerConnected(PlayerSession session)
        {
            /* Hook called when the player connects to the server */

            string uid = session.SteamId.ToString();

            // Add player to queue
            if (!(JoinQueue.Contains(uid)))
                JoinQueue.Add(uid);
        }

        void OnPlayerSpawn(PlayerSession session)
        {
            /* Hook called when the player spawns */

            string uid = session.SteamId.ToString();

            // Is player queued?
            if (JoinQueue.Contains(uid))
            {
                foreach (string item in Messages)
                {
                    // Replace name formatss
                    string line = item.Replace("{player}", session.Name);
                    line = line.Replace("{steamid}", uid);

                    // Send message
                    hurt.SendChatMessage(session, SimpleColorFormat(line));
                }

                // Remove player from queue
                JoinQueue.Remove(uid);
            }
        }

        void OnPlayerDisconnected(PlayerSession session)
        {
            /* Hook called when the player connects to the server */

            string uid = session.SteamId.ToString();

            // Remove player from queue on connection drop
            if (JoinQueue.Contains(uid))
                JoinQueue.Remove(uid);
        }

        #endregion Player Hooks
    }
}