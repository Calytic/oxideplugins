using System;
using System.Collections.Generic;
using Network;

namespace Oxide.Plugins
{
    [Info("ProtocolKickInfo", "Fujikura", "1.0.0")]
    class ProtocolKickInfo : RustPlugin
    {
		
		private int serverProtocol;

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
		}
				
		void OnClientAuth(Connection connection)
		{
			if (connection.protocol > serverProtocol)
			{
				var player = rust.FindPlayerById(connection.userid);
				if (player != null)
					Puts($"Kicked '{player.displayName}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
				else
					Puts($"Kicked '{connection}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
				Network.Net.sv.Kick(connection, lang.GetMessage("msgServerWrong", this, connection.userid.ToString()));
				connection.protocol = (uint)serverProtocol;
				NextTick(() => ServerMgr.Instance.connectionQueue.RemoveConnection(connection));
			}
			if (connection.protocol < serverProtocol)
			{
				var player = rust.FindPlayerById(connection.userid);
				if (player != null)
					Puts($"Kicked '{player.displayName}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
				else
					Puts($"Kicked '{connection}' with client protocol '{connection.protocol}' | server has '{serverProtocol}'");
				Network.Net.sv.Kick(connection, lang.GetMessage("msgClientWrong", this, connection.userid.ToString()));
				connection.protocol = (uint)serverProtocol;
				NextTick(() => ServerMgr.Instance.connectionQueue.RemoveConnection(connection));
			}
		}
	}
}