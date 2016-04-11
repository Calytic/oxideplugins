using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Join Quit Msg", "Those45Ninjas", "0.4.0", ResourceId = 1409)]
    [Description("Let users know when someone has joined or left the server.")]
    public class JoinQuitMsg : RustPlugin
    {
        #region config Keys
        const string conf_JoinMsg = "Player join message";
        const string conf_QuitMsg = "Player leave message";
        const string conf_AdminCol = "Admin Colour";
        const string conf_PlayerCol = "Player Colour";

        const string conf_OnlyAdmin = "Only Show Admins";
        const string conf_ShowConnect = "Show Connections";
        const string conf_ShowDisConn = "show Disconnections";
        #endregion

        // Keep these config variables in memory so I don't have to keep calling Config[];
        string joinFormat = "<color={1}>{0}</color> has joined the game.";
        string quitFormat = "<color={1}>{0}</color> has left the game (Reason: {2}).";
        string adminColour = "#AAFF55";
        string userColour = "#55AAFF";
        bool onlyShowAdmins = false;
        bool showConnections = true;
        bool showDisconnections = true;

        protected override void LoadDefaultConfig()
        {
            // Create the default configuration file.
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            LoadConfigFile();
        }
        void Loaded()
        {
            LoadConfigFile();
        }
        void LoadConfigFile()
        {
            bool missing = false;
            // Load or set the Join Message Format.
            if (Config[conf_JoinMsg] != null)
                joinFormat = Config[conf_JoinMsg].ToString();
            else
            {
                missing = true;
                Config[conf_JoinMsg] = joinFormat;
            }

            // Load or set the Quit Message Format.
            if (Config[conf_QuitMsg] != null)
                quitFormat = Config[conf_QuitMsg].ToString();
            else
            {
                missing = true;
                Config[conf_QuitMsg] = quitFormat;
            }

            // Load or set the admin colour.
            if (Config[conf_AdminCol] != null)
                adminColour = Config[conf_AdminCol].ToString();
            else
            {
                missing = true;
                Config[conf_AdminCol] = adminColour;
            }

            // Load or set the user colour.
            if (Config[conf_PlayerCol] != null)
                userColour = Config[conf_PlayerCol].ToString();
            else
            {
                missing = true;
                Config[conf_PlayerCol] = userColour;
            }

            // Load or set only show admins.
            if (Config[conf_OnlyAdmin] != null)
                onlyShowAdmins = (bool)Config[conf_OnlyAdmin];
            else
            {
                missing = true;
                Config[conf_OnlyAdmin] = onlyShowAdmins;
            }

            // Load or set show connections.
            if (Config[conf_ShowConnect] != null)
                showConnections = (bool)Config[conf_ShowConnect];
            else
            {
                missing = true;
                Config[conf_ShowConnect] = showConnections;
            }

            // Load or set show disconnections.
            if (Config[conf_ShowDisConn] != null)
                showDisconnections = (bool)Config[conf_ShowDisConn];
            else
            {
                missing = true;
                Config[conf_ShowDisConn] = showDisconnections;
            }

            if (missing)
            {
                Config.Save();
                PrintWarning("Updated the config file.");
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (!showConnections)
                return;

            if(player.IsAdmin())
                rust.BroadcastChat(string.Format(joinFormat, player.displayName, adminColour),null,player.UserIDString);
            else if (!onlyShowAdmins)
                rust.BroadcastChat(string.Format(joinFormat, player.displayName, userColour), null, player.UserIDString);
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!showDisconnections)
                return;

            if(player.IsAdmin())
                rust.BroadcastChat(string.Format(quitFormat, player.displayName, adminColour, reason), null, player.UserIDString);
            else if (!onlyShowAdmins)
                rust.BroadcastChat(string.Format(quitFormat, player.displayName, userColour, reason), null, player.UserIDString);

        }
        /*[ConsoleCommand("msgTest")]
        void Test(ConsoleSystem.Arg arg)
        {
            OnPlayerDisconnected(arg.Player(), "Testing");
            OnPlayerInit(arg.Player());
        }*/
    }
}