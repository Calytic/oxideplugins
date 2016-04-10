// Reference: UnityEngine.UI

using System;
using System.Collections.Generic;
using uLink;

namespace Oxide.Plugins
{
    [Info("NoClone", "Noviets", "1.0.0")]
    [Description("Automatic kicking of players with duplicate names.")]

    class NoClone : HurtworldPlugin
    {
		
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"reason","Your name is already in use."},
				{"msg","<color=#ff0000>{Player} has been Kicked</color>. Duplicate player names are not allowed."}
            };
			
			lang.RegisterMessages(messages, this);
        }
		
		void Loaded()
		{
			permission.RegisterPermission("noclone.bypass", this);
			LoadDefaultMessages();
		}
		
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		
		void OnPlayerConnected(PlayerSession session)
        {
			if (!permission.UserHasPermission(session.SteamId.ToString(), "noclone.bypass"))
			{
				foreach (KeyValuePair<uLink.NetworkPlayer, PlayerSession> pair in (Dictionary<uLink.NetworkPlayer, PlayerSession>)GameManager.Instance.GetSessions())
				{
					string ID = pair.Value.SteamId.ToString();
					string JoinedID = session.SteamId.ToString();
					if(ID != JoinedID)
					{
						if(pair.Value.Name == session.Name && pair.Value != null)
						{
							ConsoleManager.Instance?.ExecuteCommand("kick " + ID + " " +(Msg("reason")));
							hurt.SendChatMessage(session, Msg("msg").Replace("{Player}",pair.Value.Name));
						}
					}
				}
			}
        }
    }
}
