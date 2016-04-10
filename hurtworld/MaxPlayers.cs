using System;
using Steamworks;

namespace Oxide.Plugins
{
    [Info("MaxPlayers", "Wulf/lukespragg", "0.3.2")]
    [Description("Increases the maxium players limit up to 244, and more")]

    class MaxPlayers : HurtworldPlugin
    {
        void Init() => GameManager.Instance.ServerConfig.MaxPlayers = 244; // DO NOT EDIT THIS

        object OnRunCommand(string command)
        {
            var args = command.Trim().ToLower().Split(' ');

            int maxPlayers;
            if (!args[0].Equals("maxplayers") || !int.TryParse(args[1], out maxPlayers)) return null;

            if (maxPlayers >= 60) PrintWarning("Setting maxplayers above 60 may cause severe performance issues. Use with caution!");

            maxPlayers = Math.Min(244, (maxPlayers == 0 ? 1 : maxPlayers));
            GameManager.Instance.ServerConfig.MaxPlayers = maxPlayers;
            if (SteamworksManagerClient.Instance.InitializedServer) SteamGameServer.SetMaxPlayerCount(maxPlayers);
            return true;
        }
    }
}
