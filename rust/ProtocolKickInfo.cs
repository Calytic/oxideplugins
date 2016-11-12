using System;
using System.Collections.Generic;
using Network;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ProtocolKickInfo", "Fujikura", "1.0.2", ResourceId = 2041)]
    class ProtocolKickInfo : RustPlugin
    {
		
		private int serverProtocol;
		
		Dictionary <ulong, int> antiSpam = new Dictionary <ulong, int>();
		Dictionary <ulong, int> quitTimer = new Dictionary <ulong, int>();

		void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			                      {
									{"msgServerWrong", "This server is not updated yet. Come back later"},
									{"msgClientWrong", "Your Rust client needs to be updated. Close your client."},
								  },this);
		}
		
		void Init()
		{
			LoadDefaultMessages();
			serverProtocol = Rust.Protocol.network;
			antiSpam.Clear();
			quitTimer.Clear();
		}
				
		void OnClientAuth(Connection connection)
		{
			if (connection.protocol > serverProtocol)
			{
				if (!antiSpam.ContainsKey(connection.userid))
					antiSpam.Add(connection.userid, -1);
				if (!quitTimer.ContainsKey(connection.userid))
					quitTimer.Add(connection.userid, 0);			
				if (quitTimer[connection.userid] == 2 )
				{
					ConsoleNetwork.SendClientCommand(connection, "global.quit", new object[] {} );
					quitTimer[connection.userid] = 0;
					return;
				}
				if (antiSpam[connection.userid] != DateTime.Now.Minute)
				{
					quitTimer[connection.userid] = 0;
					var player = rust.FindPlayerById(connection.userid);
					if (player != null)
						Puts($"Kicked '{player.displayName}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
					else
						Puts($"Kicked '{connection}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
					antiSpam[connection.userid] = DateTime.Now.Minute;
				}
				else
					quitTimer[connection.userid]++;
				Network.Net.sv.Kick(connection, lang.GetMessage("msgServerWrong", this, connection.userid.ToString()));
				connection.protocol = (uint)serverProtocol;
				NextTick(() => ServerMgr.Instance.connectionQueue.RemoveConnection(connection));
			}
			if (connection.protocol < serverProtocol)
			{
				if (!antiSpam.ContainsKey(connection.userid))
					antiSpam.Add(connection.userid, -1);
				if (!quitTimer.ContainsKey(connection.userid))
					quitTimer.Add(connection.userid, 0);			
				if (quitTimer[connection.userid] == 2 )
				{
					ConsoleNetwork.SendClientCommand(connection, "global.quit", new object[] {} );
					quitTimer[connection.userid] = 0;
					return;
				}
				if (antiSpam[connection.userid] != DateTime.Now.Minute)
				{
					quitTimer[connection.userid] = 0;
					var player = rust.FindPlayerById(connection.userid);
					if (player != null)
						Puts($"Kicked '{player.displayName}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
					else
						Puts($"Kicked '{connection}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
					antiSpam[connection.userid] = DateTime.Now.Minute;
				}
				else
					quitTimer[connection.userid]++;
				Network.Net.sv.Kick(connection, lang.GetMessage("msgClientWrong", this, connection.userid.ToString()));
				connection.protocol = (uint)serverProtocol;
				NextTick(() => ServerMgr.Instance.connectionQueue.RemoveConnection(connection));
			}
		}
	}
}