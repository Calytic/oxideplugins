
using System.Linq;
using CodeHatch.Engine.Networking;

namespace Oxide.Plugins
{
    [Info("Name Filter", "Mughisi", 0.2, ResourceId = 1059)]
    public class NameFilter : ReignOfKingsPlugin
    {
        public readonly string AllowedCharacters = "abcdefghijklmnopqrstuvwxyz1234567890[](){}!@#$%^&_-=+. ";

        private void OnPlayerConnected(Player player)
        {
            if (player.Name.All(c => AllowedCharacters.Contains(c.ToString().ToLower()))) return;
            timer.In(1, () =>Server.Kick(player,"Your connection has been denied because your name contains invalid characters", false));
            Puts($"The player {player.Name} ({player.Id}) was denied access because of invalid characters in his/her name.");
        }
    }
}
