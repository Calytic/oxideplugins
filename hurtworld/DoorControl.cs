// Reference: UnityEngine.UI
using Oxide.Core;
using System;
using System.Collections.Generic;
using uLink;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("DoorControl", "Noviets", "1.0.0")]
    [Description("Open and close all doors or within a specific distance")]

    class DoorControl : HurtworldPlugin
    {
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"nopermission","DoorControl: You dont have Permission to do this!"},
                {"opened","DoorControl: Doors have been Opened."},
				{"closed","DoorControl: Doors have been Closed."},
				{"invaliddistance","DoorControl: Distance must be a number if you supply one.     (No distance will control all doors)"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		void Loaded()
        {
			permission.RegisterPermission("doorcontrol.admin", this);
			LoadDefaultMessages();
		}
		
		[ChatCommand("doors")]
        void doorCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"doorcontrol.admin"))
			{
				int i = 0;
				int distance = 0;
				if(args.Length == 2)
				{
					try{distance = Convert.ToInt32(args[1]);}
					catch
					{
						hurt.SendChatMessage(session, Msg("invaliddistance",session.SteamId.ToString()));
						return;
					}
				}
				switch (args[0])
				{
					case "open":
						Open(session, distance);
						break;
					case "close":
						Close(session, distance);
						break;
				}
			}
			else
				hurt.SendChatMessage(session, Msg("nopermission",session.SteamId.ToString()));
		}
		void Open(PlayerSession session, int distance)
		{
			foreach(DoubleDoorServer door in Resources.FindObjectsOfTypeAll<DoubleDoorServer>())
			{
				if (distance > 0)
				{
					if(Vector3.Distance(session.WorldPlayerEntity.transform.position, door.transform.position) <= distance)
					{
						if(!door.IsOpen)
						{
							door.DoorCollider.enabled = false;
							door.RPC("DOP", uLink.RPCMode.OthersBuffered, true);
							door.IsOpen=true;
						}
					}
				}
				else
				{
					if(!door.IsOpen)
					{
						door.DoorCollider.enabled = false;
						door.RPC("DOP", uLink.RPCMode.OthersBuffered, true);
						door.IsOpen=true;
					}
				}
			}
			foreach(GarageDoorServer door in Resources.FindObjectsOfTypeAll<GarageDoorServer>())
			{
				if (distance > 0)
				{
					if(Vector3.Distance(session.WorldPlayerEntity.transform.position, door.transform.position) <= distance)
					{
						if(!door.IsOpen)
						{
							door.DoorCollider.enabled = false;
							door.RPC("DOP", uLink.RPCMode.OthersBuffered, true);
							door.IsOpen=true;
						}
					}
				}
				else
				{
					if(!door.IsOpen)
					{
						door.DoorCollider.enabled = false;
						door.RPC("DOP", uLink.RPCMode.OthersBuffered, true);
						door.IsOpen=true;
					}
				}
			}
			hurt.SendChatMessage(session, Msg("opened",session.SteamId.ToString()));
			
		}
		void Close(PlayerSession session, int distance)
		{
			foreach(DoubleDoorServer door in Resources.FindObjectsOfTypeAll<DoubleDoorServer>())
			{
				if (distance > 0)
				{
					if(Vector3.Distance(session.WorldPlayerEntity.transform.position, door.transform.position) <= distance)
					{
						if(door.IsOpen)
						{
							door.DoorCollider.enabled = true;
							door.RPC("DOP", uLink.RPCMode.OthersBuffered, false);
							door.IsOpen=false;
						}
					}
				}
				else
				{
					if(door.IsOpen)
					{
						door.DoorCollider.enabled = true;
						door.RPC("DOP", uLink.RPCMode.OthersBuffered, false);
						door.IsOpen=false;
					}
				}
			}
			foreach(GarageDoorServer door in Resources.FindObjectsOfTypeAll<GarageDoorServer>())
			{
				if (distance > 0)
				{
					if(Vector3.Distance(session.WorldPlayerEntity.transform.position, door.transform.position) <= distance)
					{
						if(door.IsOpen)
						{
							door.DoorCollider.enabled = true;
							door.RPC("DOP", uLink.RPCMode.OthersBuffered, false);
							door.IsOpen=false;
						}
					}
				}
				else
				{
					if(door.IsOpen)
					{
						door.DoorCollider.enabled = true;
						door.RPC("DOP", uLink.RPCMode.OthersBuffered, false);
						door.IsOpen=false;
					}
				}
			}
			hurt.SendChatMessage(session, Msg("closed",session.SteamId.ToString()));
		}
	}
}