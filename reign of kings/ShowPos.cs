using System;
using UnityEngine;
using Oxide.Core;
using CodeHatch.Engine.Networking;

 
namespace Oxide.Plugins
{
    [Info("Show Position", "PaiN", 0.1, ResourceId = 0)] 
    [Description(".")]
    class ShowPos : ReignOfKingsPlugin 
    {
		[ChatCommand("showpos")]
		void cmdShowPos(Player player, string cmd, string[] args)
		{
			SendReply(player, "X: {0}, Y: {1}, Z: {2}", Convert.ToInt32(player.Entity.Position.x).ToString(), Convert.ToInt32(player.Entity.Position.y).ToString(), Convert.ToInt32(player.Entity.Position.z).ToString());
		}
	}
}