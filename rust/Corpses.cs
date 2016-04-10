using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Corpses", "Steven", "1.0.1", ResourceId = 8913)]
    class Corpses : RustPlugin
    {
        [ChatCommand("deadclean")]
        void DeadCleanCmd(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2)
			{
				int count = 0;
				var c = Resources.FindObjectsOfTypeAll<PlayerCorpse>();
				for (int i = 0; i < c.Length - 1; i++)
				{
					count++;		
					c[i].KillMessage();
				}
				SendReply(player, "Deleted " + count + " Dead Corpses.");
			}
        }
		
		[ChatCommand("deadcount")]
        void DeadCountCmd(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2)
			{
				int count = 0;
				foreach(PlayerCorpse c in Resources.FindObjectsOfTypeAll<PlayerCorpse>()) count++;
				SendReply(player, count-1 + " Dead Corpses Found.");
			}
        }
    }
}
