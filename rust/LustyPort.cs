using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("LustyPort", "Kayzor", "1.1.9", ResourceId = 1250)]
    [Description("A simple teleportation plugin!")]
    public class LustyPort : RustPlugin
    {
        // Plugin variables
        string lustyPlugin = null;
        string lustyAuthor = null;
        string lustyVersion = null;
        string lustyDescription = null;
				
        List<LustyPorts> lustyPorts = new List<LustyPorts>();
		List<LustyPlayers> lustyPlayers = new List<LustyPlayers>();
		List<TeleportingPlayers> teleportingPlayers = new List<TeleportingPlayers>();

        int teleportDuration = 10; // Seconds

        void Init()
        {
            object[] getAttributes = this.GetType().GetCustomAttributes(false);

            foreach (Attribute a in getAttributes)
            {
                if (a.ToString() == "Oxide.Plugins.DescriptionAttribute")
                {
                    lustyDescription = (a as DescriptionAttribute).Description;
                }
                else if (a.ToString() == "Oxide.Plugins.InfoAttribute")
                {
                    lustyPlugin = (a as InfoAttribute).Title;
                    lustyAuthor = (a as InfoAttribute).Author;
                    lustyVersion = (a as InfoAttribute).Version.ToString();
                }
            }
			
			lustyPorts = Interface.GetMod().DataFileSystem.ReadObject<List<LustyPorts>>("LustyPortTeleports");
			lustyPlayers = Interface.GetMod().DataFileSystem.ReadObject<List<LustyPlayers>>("LustyPortPlayers");

            timer.Repeat(0.999f, 0, () => timerPort());
        }

        // Chat Commands
        [ChatCommand("tp")]
        private void tpCmd(BasePlayer player, string command, string[] args)
        {
            if (args.Length >= 1)
            {
                tpPort(player, false, args[0]);
            }
            else
            {
                noArg(player, command);
            }
        }

        [ChatCommand("tp_add")]
        private void tpAdd(BasePlayer player, string command, string[] args)
        {
            if (isAdmin(player))
            {
                if (args.Length >= 1)
                {
					bool admin = false;
					if (args.Length >= 2)
					{
						try
						{
							admin = Convert.ToBoolean(args[1]);
						}
						catch {	}
					}
                    addPort(player, args[0], admin);
                }
                else
                {
                    noArg(player, command);
                }
            }
        }

        [ChatCommand("tp_del")]
        private void tpDel(BasePlayer player, string command, string[] args)
        {
            if (isAdmin(player))
            {
                if (args.Length >= 1)
                {
                    delPort(player, args[0]);
                }
                else
                {
                    noArg(player, command);
                }
            }
        }


        [ChatCommand("tp_back")]
        private void tpBack(BasePlayer player, string command, string[] args)
        {
            tpPort(player, true);
        }

        [ChatCommand("tp_list")]
        private void tpList(BasePlayer player, string command, string[] args)
        {
			// List Admin Locations
            if (isAdmin(player))
            {
                List<LustyPorts> listAdminPorts = lustyPorts.FindAll(r => r.admin == true);
				if (listAdminPorts.Count > 0)
				{
					playerMsg(player, "Admin Only Teleport Locations:");
					foreach (LustyPorts lustyPort in listAdminPorts)
					{
						playerMsg(player, "<color=#00ff00ff>" + lustyPort.name + "</color> (" + lustyPort.x + "," + lustyPort.y + "," + lustyPort.z + ")");
					}
				}
			}

            // List Locations
            List<LustyPorts> listPorts = lustyPorts.FindAll(r => r.admin == false);
			if (listPorts.Count > 0)
			{
				playerMsg(player, "Teleport Locations:");
				foreach (LustyPorts lustyPort in listPorts)
				{
					playerMsg(player, "<color=#00ff00ff>" + lustyPort.name + "</color> (" + lustyPort.x + "," + lustyPort.y + "," + lustyPort.z + ")");
				}
			}
			else
			{
				playerMsg(player, "No teleport locations have been setup");
			}
        }

        [ChatCommand("tp_about")]
        private void tpAbout(BasePlayer player, string command, string[] args)
        {
            playerMsg(player, "<color=#00ff00ff>" + lustyPlugin + "</color> v <color=#00ff00ff>" + lustyVersion + "</color> by <color=#00ff00ff>" + lustyAuthor + "</color>");
            playerMsg(player, lustyDescription);
            playerMsg(player, "Type <color=#00ff00ff>/tp_help</color> for a list of all commands");
        }

        [ChatCommand("tp_help")]
        private void tpHelp(BasePlayer player, string command, string[] args)
        {
            playerMsg(player, "<color=#00ff00ff>" + lustyPlugin + "</color> v <color=#00ff00ff>" + lustyVersion + "</color>");
            playerMsg(player, lustyDescription);
            playerMsg(player, "Type <color=#00ff00ff>/tp <location></color> - Teleports you to location");
            playerMsg(player, "Type <color=#00ff00ff>/tp_back</color> - Teleports you back to your original location");
            playerMsg(player, "Type <color=#00ff00ff>/tp_list</color> - Lists all available locations");
            if (isAdmin(player)) {
                playerMsg(player, "Type <color=#00ff00ff>/tp_add <name> (optional)<true></color> - Adds a new location set to your current position (optional)Admin only");
                playerMsg(player, "Type <color=#00ff00ff>/tp_del <name></color> - Deletes a location");
            }
            playerMsg(player, "Type <color=#00ff00ff>/tp_about</color> - Information about the plugin");
            playerMsg(player, "Type <color=#00ff00ff>/tp_help</color> - Displays this menu");
        }
		
		// Teleporting Class
		private class TeleportingPlayers
		{
			public ulong userid { get; set; }
			public DateTime starttime { get; set; }
			public float x { get; set; }
			public float y { get; set; }
			public float z { get; set; }
            public LustyPorts lustyPort { get; set; }
            public bool back { get; set; }
		}
		
		// Player Locations	
		private class LustyPlayers
		{
			public ulong userid	{ get; set; }		
			public LustyPorts lustyPort { get; set; }
        }

        private void savePorts()
        {
            Interface.GetMod().DataFileSystem.WriteObject("LustyPortTeleports", lustyPorts);
        }

        private void savePlayers()
        {
            Interface.GetMod().DataFileSystem.WriteObject("LustyPortPlayers", lustyPlayers);
        }

        // Teleport Locations
        private class LustyPorts
		{
			public string name { get; set; }
			public float x { get; set; }
			public float y { get; set; }
			public float z { get; set; }
			public bool admin { get; set; }
		}
        
		private LustyPorts findPort(string name)
		{
			LustyPorts lustyPort = lustyPorts.Find(r => r.name.ToLower() == name.ToLower());
			return lustyPort;
		}		
		
		private void addPort(BasePlayer player, string name, bool admin = false)
		{
			if (findPort(name) == null)
			{
				LustyPorts lustyPort = new LustyPorts();
				lustyPort.name = name;
				lustyPort.admin = admin;
                lustyPort.x = player.transform.position.x;
                lustyPort.y = player.transform.position.y;
                lustyPort.z = player.transform.position.z;
				lustyPorts.Add(lustyPort);
                savePorts();

                string adminTxt = "";
                if (admin)
                {
                    adminTxt = " (Admin only)";
                }

				playerMsg(player, "Teleport location<color=#00ff00ff> " + name + adminTxt + "</color> added");
			}
			else
			{
				playerMsg(player, "There is already a teleport location named<color=#00ff00ff> " + name + "</color>");
			}		
		}
		
		private void delPort(BasePlayer player, string name)
		{
			LustyPorts lustyPort = findPort(name);
			if (lustyPort != null)
			{
				lustyPorts.Remove(lustyPort);
                savePorts();
				
				playerMsg(player, "Teleport location<color=#00ff00ff> " + name + " </color>removed");
			}
			else
			{
				playerMsg(player, "There is already a teleport location named<color=#00ff00ff> " + name + "</color>");
			}
		}
		
        private void tpPort(BasePlayer player, bool back, string name = null)
        {
            LustyPorts lustyPort = new LustyPorts();
            if (back)
            {
                if (lustyPlayers.Count > 0)
                {
                    lustyPort = lustyPlayers.Find(r => r.userid == player.userID).lustyPort;
                    if (lustyPort == null)
                    {
                        playerMsg(player, "You do not have an orginal location to teleport back too");
                        return;
                    }
                }
                else
                {
                    playerMsg(player, "You do not have an orginal location to teleport back too");
                    return;
                }
            }
            else
            {
                lustyPort = findPort(name);
            }
            if (lustyPort != null)
            {
                if (findTeleportingPlayer(player.userID) == null)
                {
                    if (!lustyPort.admin)
                    {
                        startPort(player, lustyPort, back);
                    }
                    else if (isAdmin(player))
                    {
                        startPort(player, lustyPort, back);
                    }
                    else
                    {
                        playerMsg(player, "You do not have access to teleport to that location");
                    }
                }
                else
                {
                    playerMsg(player, "You already have an active teleport request");
                }
            }
            else
            {
                playerMsg(player, "Teleport location <color=#00ff00ff>" + name + "</color> not found");
            }
        }

        private void startPort(BasePlayer player, LustyPorts lustyPort, bool back)
        {
            TeleportingPlayers tpPlayer = new TeleportingPlayers();
            tpPlayer.starttime = DateTime.UtcNow;
            tpPlayer.userid = player.userID;
            tpPlayer.x = player.transform.position.x;
            tpPlayer.y = player.transform.position.y;
            tpPlayer.z = player.transform.position.z;
            tpPlayer.lustyPort = lustyPort;
            tpPlayer.back = back;
            teleportingPlayers.Add(tpPlayer);
            playerMsg(player, "Teleport initiated, you will be teleported in <color=#00ff00ff>" + teleportDuration + "</color> seconds, remain still!");
        }

        private TeleportingPlayers findTeleportingPlayer(ulong userid)
        {
            TeleportingPlayers teleportingPlayer = teleportingPlayers.Find(r => r.userid == userid);
            if (teleportingPlayer != null)
            {
                return teleportingPlayer;
            }
            return null;
        }

        private BasePlayer findPlayer(ulong userid)
        {
            BasePlayer findPlayer = BasePlayer.activePlayerList.Find(r => r.userID == userid);
            if (findPlayer != null)
            {
                return findPlayer;
            }
            return null;
        }

        // Timer Function
        private void timerPort()
        {
            if (teleportingPlayers.Count > 0)
            {
                for (int i = teleportingPlayers.Count - 1; i >= 0; i--)
                {
                    TeleportingPlayers teleportingPlayer = teleportingPlayers[i];
                    BasePlayer player = findPlayer(teleportingPlayer.userid);

                    if (checkTeleport(player, teleportingPlayer))
                    {
                        if (DateTime.UtcNow > teleportingPlayer.starttime.AddSeconds(teleportDuration))
                        {
                            if (teleportingPlayer.back)
                            {
                                LustyPlayers lustyPlayer = lustyPlayers.Find(r => r.userid == player.userID);
                                lustyPlayers.Remove(lustyPlayer);
                                savePlayers();
                            }
                            else
                            {
                                if (lustyPlayers.Count > 0)
                                {
                                    LustyPlayers lustyPlayerCheck = lustyPlayers.Find(r => r.userid == player.userID);
                                    if (lustyPlayerCheck == null)
                                    {
                                        addPlayer(player);
                                    }
                                }
                                else
                                {
                                    addPlayer(player);
                                }                                
                            }

                            Vector3 destination = new Vector3(teleportingPlayer.lustyPort.x, teleportingPlayer.lustyPort.y, teleportingPlayer.lustyPort.z);
                            TeleportPlayerPosition(player, destination);

                            teleportingPlayers.RemoveAt(i);
                        }
                        else
                        {
                            TimeSpan timeSpan = teleportingPlayer.starttime.AddSeconds(teleportDuration).Subtract(DateTime.UtcNow);
                            int time = Convert.ToInt16(Math.Ceiling(timeSpan.TotalSeconds));
                            if (time > 0 && time < teleportDuration)
                            {
                                playerMsg(player, "Teleporting in " + time.ToString());
                            }
                        }
                    }
                    else
                    {
                        teleportingPlayers.RemoveAt(i);
                        playerMsg(player, "Movement detected, aborting teleport.");
                    }
                }
            }
        }

        private void addPlayer(BasePlayer player)
        {
            LustyPlayers lustyPlayer = new LustyPlayers();
            lustyPlayer.userid = player.userID;

            LustyPorts lustyPort = new LustyPorts();
            lustyPort.admin = false;
            lustyPort.name = player.userID.ToString();
            lustyPort.x = player.transform.position.x;
            lustyPort.y = player.transform.position.y;
            lustyPort.z = player.transform.position.z;

            lustyPlayer.lustyPort = lustyPort;
            lustyPlayers.Add(lustyPlayer);
            savePlayers();
        }

        private bool checkTeleport(BasePlayer player, TeleportingPlayers teleportingPlayer)
        { 
            if (Convert.ToSingle(player.transform.position.x) >= (teleportingPlayer.x - 0.2) && Convert.ToSingle(player.transform.position.x) <= (teleportingPlayer.x + 0.2))
            {
                if (Convert.ToSingle(player.transform.position.y) >= (teleportingPlayer.y - 0.2) && Convert.ToSingle(player.transform.position.y) <= (teleportingPlayer.y + 0.2))
                {
                    if (Convert.ToSingle(player.transform.position.z) >= (teleportingPlayer.z - 0.2) && Convert.ToSingle(player.transform.position.z) <= (teleportingPlayer.z + 0.2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        // Random Functions
        private void noArg(BasePlayer player, string command)
        {
            if (command == "tp")
            {
                playerMsg(player, "Invalid command! Usage: <color=#00ff00ff>/tp <location></color>");
                playerMsg(player, "Type <color=#00ff00ff>/tp_help</color> for a list of all commands");
            }
            if (command == "tp_add")
            {
                playerMsg(player, "Invalid command! Usage: <color=#00ff00ff>/tp_add <name></color> - Adds a new location set to your current location");
                playerMsg(player, "Type <color=#00ff00ff>/tp_help</color> for a list of all commands");
            }
            if (command == "tp_back")
            {
                playerMsg(player, "Invalid command! Usage: <color=#00ff00ff>/tp_del <name></color> - Deletes location from the list");
                playerMsg(player, "Type <color=#00ff00ff>/tp_help<color> for a list of all commands");
            }
        }

        private void playerMsg(BasePlayer player, string msg)
        {
            SendReply(player, String.Format("<color=#008080ff>Lusty Port</color> {0}", msg));
        }

        bool isAdmin(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 1)
            {
                return true;
            }
            return false;
        }

        void TeleportPlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.ClientRPCPlayer(null, player, "StartLoading");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (BasePlayer.sleepingPlayerList.Contains(player) == false) BasePlayer.sleepingPlayerList.Add(player);
            player.transform.position = destination;

            var LastPositionValue = typeof(BasePlayer).GetField("lastPositionValue", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            LastPositionValue.SetValue(player, player.transform.position);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.TransformChanged();

            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.SendFullSnapshot();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, false);
            player.ClientRPCPlayer(null, player, "FinishLoading");
        }        
    }
}