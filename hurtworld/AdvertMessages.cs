using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Advert Messages", "SkinN", "2.0.4", ResourceId = 1510)]
    [Description("Timed chat messages to work as informational messages")]

    class AdvertMessages : HurtworldPlugin
    {
        #region Plugin Resources

        /* The Developer mode is used mostly for debugging or testing features
           of the plugin, also forces the default configuration file
           on each load */

        // Developer Variables
        private const bool Dev = false;

        // Plugin Variables
        private System.Random AdvertsLoop = new System.Random();
        private int LastAdvert = 0;

        // Configuration Variables
        private bool BroadcastToConsole;
        private int Interval;
        private static List<object> Adverts;

        #endregion Plugin Resources

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

            // Settings Section
            BroadcastToConsole = GetConfig<bool>("Settings", "Broadcast To Console", true);
            Interval = GetConfig<int>("Settings", "Adverts Interval (In Minutes)", 12);

            // Advert Messages
            Adverts = GetConfig<List<object>>("Messages", new List<object>(new string[] {
                        "Welcome to our server, have fun!",
                        "<orange>Need help?<end> Try calling for the <cyan>Admins<end> in the chat.",
                        "Please, be respectful with to the other players.",
                        "Cheating will result in a <red>permanent<end> ban.",
                        "This server is running <orange>Oxide 2<end>."
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

        #endregion Configuration

        #region Plugin Hooks / Methods

        void Init()
        {
            /* Hook called when plugin initializes */

            // Load plugin variables
            LoadVariables();

            // Messages timer
            timer.Repeat(Interval * 60, 0, () => SendAdvert());

            Puts("Starting advert messages timer, set to " + Interval.ToString() + " minute/s");
        }

        void SendAdvert()
        {
            int index = LastAdvert;

            // Is there more than one Advert?
            if (Adverts.Count > 1)
            {
                // Loop untill it gets a different advert
                while (index == LastAdvert)
                    index = AdvertsLoop.Next(Adverts.Count);
            }

            LastAdvert = index;

            hurt.BroadcastChat(SimpleColorFormat("<silver>" + Adverts[index] + "<end>"));
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

        #endregion Plugin Hooks / Methods
    }
}