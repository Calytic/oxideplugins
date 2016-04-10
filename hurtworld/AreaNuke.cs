using System.Collections.Generic;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("AreaNuke", "Noviets", 1.0)]
    [Description("Removes every object within a specified range")]

    class AreaNuke : HurtworldPlugin
    {
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","AreaNuke: You dont have Permission to do this!"},
                {"invalidrange","AreaNuke: The range must be a number."},
				{"invalidargs","AreaNuke: Incorrect usage: /areanuke [range] (example; /areanuke 5)"},
				{"destroyed","AreaNuke: Destroyed {Count} objects"}
			};
			lang.RegisterMessages(messages, this);
        }
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		void Loaded()
        {
            permission.RegisterPermission("areanuke.admin", this);
			LoadDefaultMessages();
		}
		[ChatCommand("areanuke")]
        void cmdareanuke(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"areanuke.admin"))
			{
				if(args.Length == 1)
				{
					int count = 0;
					int range = 0;
					try{range = Convert.ToInt32(args[0]);}
					catch{
						hurt.SendChatMessage(session, Msg("invalidrange",session.SteamId.ToString()));
						return;
					}
					foreach (GameObject objects in Resources.FindObjectsOfTypeAll<GameObject>())
					{
						if(Vector3.Distance(session.WorldPlayerEntity.transform.position, objects.transform.position) <= range)
						{
							if(!objects.name.ToString().Contains("Desert") && !objects.name.ToString().Contains("Cliff") && !objects.name.ToString().Contains("Player") && !objects.name.ToString().Contains("Cam"))
							{
								uLink.NetworkView obj = objects.GetComponent<uLink.NetworkView>();
								try{Singleton<NetworkManager>.Instance.NetDestroy(obj);}catch{}
								count++;
							}
						}
					}
					hurt.SendChatMessage(session, Msg("destroyed",session.SteamId.ToString()).Replace("{Count}",count.ToString()));
				}
				else
					hurt.SendChatMessage(session, Msg("invalidargs",session.SteamId.ToString()));
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
        }
	}
}