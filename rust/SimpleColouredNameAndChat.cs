using UnityEngine;
using Rust;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("SimpleColouredNameAndChat", "Steven", "1.0.0", ResourceId = 8909)]
    class SimpleColouredNameAndChat : RustPlugin
    {
		ulong[] IDS = {76561200445525877};
        string[] NameColours = { "green" };
        string[] TextColours = { "purple" };
		string GetPlayerColour(string name, ulong id)
		{
            for(int i = 0; i < 1; i++)
                if(id == IDS[i]) return string.Format("<color={0}>{1}</color>", NameColours[i], name);
            return name;
		}

        string GetPlayerTextColour(string msg, ulong id)
        {
            for (int i = 0; i < 1; i++)
                if(id == IDS[i]) return string.Format("<color={0}>{1}</color>", TextColours[i], msg);
            return msg;
        }
				
	    [HookMethod("OnPlayerChat")]
        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            string playerChat = arg.GetString(0, "text");
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player != null && playerChat != null)
            {
                PrintToChat(string.Format("{0}: {1}", GetPlayerColour(player.displayName, player.userID), GetPlayerTextColour(playerChat, player.userID)));
            }
            return "handled";
        }
    }
}