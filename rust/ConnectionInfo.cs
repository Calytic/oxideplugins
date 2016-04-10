using System;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("ConnectionInfo", "Norn", 0.1, ResourceId = 1460)]
    [Description("Basic player information.")]
    public class ConnectionInfo : RustPlugin
    {
        [PluginReference]
        Plugin ConnectionDB;

        [ChatCommand("pinfo")]
        private void PlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (ConnectionDB)
            {
                if (Convert.ToBoolean(ConnectionDB.CallHook("ConnectionDataExists", player)))
                {
                    DateTime init_date = Convert.ToDateTime(ConnectionDB.CallHook("FirstSeen", player));
                    int seconds = Convert.ToInt32(ConnectionDB.CallHook("SecondsPlayed", player));
                    TimeSpan ts = TimeSpan.FromSeconds(seconds);
                    PrintToChat(player, "<color=#66ff66>" + player.displayName + "</color> (<color=#ffccff>" + Convert.ToString(ConnectionDB.CallHook("FirstName", player)) + "</color>):\nYou have played <color=yellow>" + Math.Round(ts.TotalMinutes).ToString() + "</color> minutes since <color=yellow>" + init_date.ToShortDateString() + "</color>!\nTotal Seconds: <color=yellow>" + seconds.ToString() + "</color> | Connections: <color=yellow>" + Convert.ToString(ConnectionDB.CallHook("Connections", player)) + "</color>.");
                }
            }
        }
        void Loaded()
        {
            if(!ConnectionDB) { Puts("ConnectionDB [1459] has not been found!"); }
        }
    }
}