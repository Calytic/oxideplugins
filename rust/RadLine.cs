using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("RAD-Line", "SkinN", "3.0.1", ResourceId = 914)]
    [Description("Enables and disables radiation every X minutes")]

    class RadLine : RustPlugin
    {
        #region Plugin Resources

        // Developer Variables
        private readonly bool Dev = false;

        // Configuration Variables
        private string Prefix;
        private string IconProfile;
        private bool EnableIconProfile;
        private bool BroadcastToConsole;
        private bool EnablePluginPrefix;
        private int EnabledInterval;
        private int DisabledInterval;
        private DateTime LastTimer;

        #endregion

        #region Configuration

        protected override void LoadDefaultConfig()
        {
            /* Hook called when the config for the plugin initializes */

            Puts("Creating new configuration file");

            // Clear configuration for a brand new one
            Config.Clear();

            // Load plugin variables
            LoadVariables();
        }

        private void LoadVariables()
        {
            // Clear configuration if on developer mode
            if (Dev)
                Config.Clear();

            // Load configuration variables
            Prefix = GetConfig<string>("General Settings", "Prefix", "[ <cyan>RAD-LINE<end> ]");
            IconProfile = GetConfig<string>("General Settings", "Icon Profile", "76561198248442828");
            BroadcastToConsole = GetConfig<bool>("General Settings", "Broadcast To Console", true);
            EnablePluginPrefix = GetConfig<bool>("General Settings", "Enable Plugin Prefix", true);
            EnableIconProfile = GetConfig<bool>("General Settings", "Enable Icon Profile", false);
            EnabledInterval = GetConfig<int>("General Settings", "Radiation Enabled Interval (In Minutes)", 30);
            DisabledInterval = GetConfig<int>("General Settings", "Radiation Disabled Interval (In Minutes)", 10);
        }

        private T GetConfig<T>(params object[] args)
        {
            /* Gets a value from the configuration file
               Developer mode forces the default values on every setting on each load */

            var stringArgs = new string[args.Length - 1];
            for (var i = 0; i < args.Length - 1; i++)
                stringArgs[i] = args[i].ToString();

            if (Config.Get(stringArgs) == null || Dev)
                Config.Set(args);

            return (T)Convert.ChangeType(Config.Get(stringArgs), typeof(T));
        }

        private void LoadMessages()
        {
            /* Method to register messages to Lang library from Oxide */

            lang.RegisterMessages(new Dictionary<string, string> {
                { "Enabled Radiation", "Radiation levels are now up for <orange>{interval} minutes<end>." },
                { "Disabled Radiation", "Radiation levels are now down for <orange>{interval} minutes<end>." },
                { "Radiation Is Enabled", "Radiation levels are up for <orange>{remaining} minutes<end>."},
                { "Radiation Is Disabled", "Radiation levels are down for <orange>{remaining} minutes<end>." }
            }, this);
        }

        private string GetMsg(string key, object uid = null)
        {
            /* Method to get a plugin message */

            return lang.GetMessage(key, this, uid == null ? null : uid.ToString());
        }

        #endregion

        #region Messages System

        private void Con(string msg)
        {
            /* Broadcasts a message to the server console */

            if (BroadcastToConsole)
                Puts(SimpleColorFormat(msg, true));
        }

        private void Say(string msg, string profile = "0", bool prefix = true)
        {
            /* Broadcasts a message to chat for all players */

            // Log message to console
            Con(msg);

            // Check whether prefix is enabled
            if (!String.IsNullOrEmpty(Prefix) && EnablePluginPrefix && prefix)
                msg = Prefix + " " + msg;

            // Check whether to use a profile
            if (profile == "0" && EnableIconProfile)
                profile = IconProfile;

            rust.BroadcastChat(SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        private void Tell(BasePlayer player, string msg, string profile = "0", bool prefix = true)
        {
            /* Broadcasts a message to chat to a player */

            // Check whether prefix is enabled
            if (!String.IsNullOrEmpty(Prefix) && EnablePluginPrefix && prefix)
                msg = Prefix + " " + msg;

            // Check whether to use a profile
            if (profile == "0" && EnableIconProfile)
                profile = IconProfile;

            rust.SendChatMessage(player, SimpleColorFormat("<silver>" + msg + "<end>"), null, profile);
        }

        public string SimpleColorFormat(string text, bool removeTags = false)
        {
            /*  Simple Color Format ( v3.0 )
                Formats simple color tags to HTML */

            // All patterns
            Regex end = new Regex(@"\<(end?)\>"); // End tags
            Regex hex = new Regex(@"\<(#\w+?)\>"); // Hex codes
            Regex names = new Regex(@"\<(\w+?)\>"); // Names

            if (removeTags)
            {
                // Remove tags
                text = end.Replace(text, "");
                text = names.Replace(text, "");
                text = hex.Replace(text, "");
            }
            else
            {
                // Replace tags
                text = end.Replace(text, "</color>");
                text = names.Replace(text, "<color=$1>");
                text = hex.Replace(text, "<color=$1>");
            }

            return text;
        }

        #endregion

        #region Plugin Hooks / Methods

        void Init()
        {
            // Load plugin variables
            LoadVariables();
            // Load plugin messages
            LoadMessages();

            // Start loop
            Loop(true);
        }
        
        private void Loop(bool force = false)
        {
            // Get current time
            LastTimer = DateTime.Now;

            if (!ConVar.Server.radiation || force)
            {
                // Enable Radiation
                ConVar.Server.radiation = true;

                Say(GetMsg("Enabled Radiation").Replace("{interval}", EnabledInterval.ToString()));

                // Run timer
                timer.Once(EnabledInterval * 60, () => Loop());
            }
            else
            {
                // Disable Radiation
                ConVar.Server.radiation = false;

                Say(GetMsg("Disabled Radiation").Replace("{interval}", DisabledInterval.ToString()));

                // Run timer
                timer.Once(DisabledInterval * 60, () => Loop());
            }
        }

        #endregion

        #region Plugin Commands

        [ChatCommand("rad")]
        void Rad_Command(BasePlayer player, string command, string[] args)
        {
            /* Rad Commands
               Tells the player the radiation current state, and the state time remaining */
            
            // Get the future time
            DateTime Future;
            TimeSpan subtract;
            string msg;

            // Check the radiation state
            if (ConVar.Server.radiation)
            {
                // Add minutes of the current state
                Future = LastTimer.AddMinutes(EnabledInterval);
                // Substract the future time with the last timer
                subtract = Future - DateTime.Now;
                // Get the cuttent state message name
                msg = "Radiation Is Enabled";
            }
            else
            {
                // Add minutes of the current state
                Future = LastTimer.AddMinutes(DisabledInterval);
                // Substract the future time with the last timer
                subtract = Future - DateTime.Now;
                // Get the cuttent state message name
                msg = "Radiation Is Disabled";
            }

            // Add pads to both minutes and seconds and format for the message
            string sec = subtract.Seconds.ToString();
            string min = subtract.Minutes.ToString();
            string rem = min.PadLeft(2, '0') + ":" + sec.PadLeft(2, '0');

            Tell(player, GetMsg(msg).Replace("{remaining}", rem));
        }

        [ChatCommand("radline")]
        void Plugin_Command(BasePlayer player, string command, string[] args)
        {
            /* Plugin Command
               The plugin command is to inform what plugin is and it's version */

            Tell(player, "<orange><size=18>RAD-Line</size><end> <grey>v" + this.Version + "<end>", "76561198248442828", false);
            Tell(player, this.Description, prefix: false);
            Tell(player, "Powered by <orange>Oxide 2<end> and developed by <#9810FF>SkinN<end>", "76561197999302614", false);
        }

        #endregion
    }
}