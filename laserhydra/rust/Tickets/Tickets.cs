using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Tickets", "LaserHydra", "2.0.0", ResourceId = 1065)]
    [Description("Gives players the opportunity to send Tickets to admins.")]
    class Tickets : RustPlugin
    {
		class Ticket
		{
			public int ticketID;
			public string steamID;
			public string player;
			public string profile;
			public string position;
			public string message;
			public string reply;
			public float x;
			public float y;
			public float z;
			public string timestamp;
			
			public Ticket(int id, BasePlayer Player, string msg)
			{
				ticketID = id;
				player = Player.displayName;
				steamID = Player.userID.ToString();
				profile = "https://steamcommunity.com/profiles/" + Player.userID.ToString();
				position = $"X: {Player.transform.position.x.ToString()}, Y: {Player.transform.position.y.ToString()}, Z: {Player.transform.position.z.ToString()}";
				x = Player.transform.position.x;
				y = Player.transform.position.y;
				z = Player.transform.position.z;
				message = msg;
				reply = "This Ticket has not been replied to yet.";
				timestamp = System.DateTime.Now.ToString();
			}
			
			public Ticket()
			{
			}
		}
		
		class Data
		{
			public List<Ticket> tickets = new List<Ticket>();
			
			public Data()
			{
			}
		}
		
		Data data;
		
		void Loaded()
		{
			data = LoadData();
			if(!permission.PermissionExists("ticket.admin")) permission.RegisterPermission("ticket.admin", this);
			LoadConfig();
			timer.Once(5 * 60, () => {
					if(CheckUnrepliedTickets())
					{
						foreach(BasePlayer player in BasePlayer.activePlayerList)
						{
							if(IsAdmin(player, false))
							{
								SendChatMessage(player, "Tickets", "There are unreplied tickets! Type /ticket list to see them.");
							}
						}
						
						foreach(BasePlayer player in BasePlayer.activePlayerList)
						{
							CheckRepliedTickets(player);
						}
					}
				}
			);
		}
		
		Data LoadData() 
		{
			return Interface.GetMod().DataFileSystem.ReadObject<Data>("Tickets_Data");
		}
		
		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("Tickets_Data", data);
			//BroadcastChat("Saved data.");
		}	
		
		void LoadConfig()
		{
			SetConfig("Extras", "Enable PushAPI", false);
			SetConfig("Extras", "Enable EmailAPI", false);
		}
		
		void LoadDefaultConfig()
		{
			Puts("Generating new config file...");
		}
		
		void OnPlayerInit(BasePlayer player)
		{
			CheckRepliedTickets(player);
		}
		
		void SendToAPI(Ticket ticket)
		{
			string message = $"A new Ticket has been submitted. Ticket ID {ticket.ticketID}\n" +
								$"Timestamp: {ticket.timestamp}\n" +
								$"Player: {ticket.player}\n" +
								$"SteamID: {ticket.steamID}\n" +
								$"Steam Profile: {ticket.profile}\n" +
								$"Position: {ticket.position}\n" +
								$"Message: {ticket.message}\n";
			
			if ((bool)Config["Extras", "Enable PushAPI"] == true)
			{
				if (!plugins.Exists("PushAPI"))
				{
					Puts("You enabled support for the PushAPI in the config, but PushAPI is not installed! Get it here: http://oxidemod.org/plugins/705/");
				}
				else
				{
					var PushAPI = plugins.Find("PushAPI");
					PushAPI?.CallHook("PushMessage", "Admin Tickets | A new Ticket has been submitted!", message, "high", "gamelan");
				}
			}
				
			if ((bool)Config["Extras", "Enable EmailAPI"] == true)
			{
				if (!plugins.Exists("EmailAPI"))
				{
					Puts("You enabled support for the EmailAPI in the config, but EmailAPI is not installed! Get it here: http://oxidemod.org/plugins/712/");
				}
				else
				{
					var EmailAPI = plugins.Find("EmailAPI");
					EmailAPI?.CallHook("EmailMessage", "Admin Tickets | A new Ticket has been submitted!", message);
				}
			}
		}
				
		void CheckRepliedTickets(BasePlayer player)
		{
			foreach(Ticket ticket in data.tickets)
			{
				if(ticket.steamID == player.userID.ToString() && ticket.reply != "This Ticket has not been replied to yet.")
				{
					SendChatMessage(player, "Tickets", $"A reply to your Ticket has been made. To view it type <color=#00FF8D>/ticket view {ticket.ticketID.ToString()}</color>");
				}
			}
		}
		
		bool CheckUnrepliedTickets()
		{
			foreach(Ticket ticket in data.tickets)
			{
				if(ticket.reply == "This Ticket has not been replied to yet.") return true;
			}
			
			return false;
		}
		
		int GetNewID()
		{
			int id = 0;
			foreach(Ticket ticket in data.tickets)
			{
				if(ticket.ticketID > id) id = ticket.ticketID;
			}
			
			return id + 1;
		}
		
		Ticket GetTicketByID(int id)
		{
			foreach(Ticket ticket in data.tickets)
			{
				if(ticket.ticketID == id) return ticket;
			}
			
			return null;
		}
		
		bool IsAdmin(BasePlayer player, bool reply)
		{
			if(reply && permission.UserHasPermission(player.userID.ToString(), "ticket.admin") == false) SendChatMessage(player, "Tickets", "You do not have permission to use this command.");
			if(permission.UserHasPermission(player.userID.ToString(), "ticket.admin")) return true;
			return false;
		}
		
		void ShowSyntax(BasePlayer player)
		{
			if(IsAdmin(player, false))
			{
				SendChatMessage(player, "Tickets", "\n/ticket reply <ID> <Message>\n" +
					"/ticket add <Message>\n" +
					"/ticket remove <ID>\n" +
					"/ticket view <ID>\n" +
					"/ticket tp <ID>\n" +
					"/ticket list\n" +
					"/ticket clear\n"
				);
			}					
			else 
			{
				SendChatMessage(player, "Tickets", "\n/ticket add <Message>\n" +
					"/ticket remove <ID>\n" +
					"/ticket view <ID>\n" +
					"/ticket list\n"
				);
			}
		}
		
		[ChatCommand("clear")]
		void Clear(BasePlayer player)
		{
			for(int i = 200; i > 0; i--)
			{
				player.ConsoleMessage("<color=white>Â²</color>\n");
			}
		}

		[ChatCommand("ticket")]
		void cmdTicket(BasePlayer player, string cmd, string[] args)
		{
			if(args.Length == 0)
			{
				ShowSyntax(player);
				
				return;
			}
			
			if(args.Length == 1)
			{
				switch(args[0])
				{
					case "list":
						TicketFunction("list", "none", player);
						break;
						
					case "clear":
						if(!IsAdmin(player, true)) return; 
						TicketFunction("clear", "none", player);
						break;
						
					default:
						ShowSyntax(player);
						break;
				}
				
				return;
			}
			
			if(args.Length >= 2 && args[0] == "add")
			{
				TicketFunction("add", ListToString(args.ToList(), 1, " "), player);
				
				return;
			}
			
			if(args.Length == 2)
			{
				switch(args[0])
				{			
					case "remove":
						TicketFunction("remove", args[1], player);
						break;
						
					case "view":
						TicketFunction("view", args[1], player);
						break;
						
					case "tp":
						if(!IsAdmin(player, true)) return;
						TicketFunction("tp", args[1], player);
						break;
						
					default:
						ShowSyntax(player);
						break;
				}
				
				return;
			}
			
			if(args.Length >= 3 && args[0] == "reply")
			{
				if(!IsAdmin(player, true)) return;
				TicketFunction("reply", ListToString(args.ToList(), 1, " "), player);
				
				return;
			}
		}
		
		void TicketFunction(string function, string arg, BasePlayer player)
		{
			switch(function)
			{
				case "reply":
					SendTicketReply(arg);
					SendChatMessage(player, "Tickets", "Your reply has been submitted.");
					break;
					
				case "add":
					int id = GetNewID();
					Puts(player.transform.position.x.ToString());
					data.tickets.Add(new Ticket(id, player, arg));
					SendToAPI(new Ticket(id, player, arg));
					SendChatMessage(player, "Tickets", $"Your Ticket has been submitted. <color=#00FF8D>Ticket ID {id}</color>");
					
					foreach(BasePlayer current in BasePlayer.activePlayerList)
					{
						if(IsAdmin(current, false)) SendChatMessage(current, "Tickets", $"A new Ticket has been submitted. <color=#00FF8D>Ticket ID {id}</color>. To view it type <color=#00FF8D>/ticket view {id}</color>");
					}
					
					break;
						
				case "remove":
					if(IsAdmin(player, false))
					{
						foreach(Ticket ticket in data.tickets)
						{
							if(ticket.ticketID.ToString() == arg) 
							{
								data.tickets.Remove(ticket);
								SendChatMessage(player, "Tickets", $"Removed Ticket {ticket.ticketID.ToString()}");
								break;
							}
						}
					}
					else
					{
						foreach(Ticket ticket in data.tickets)
						{
							if(ticket.ticketID.ToString() == arg && ticket.steamID == player.userID.ToString())
							{
								data.tickets.Remove(ticket);
								SendChatMessage(player, "Tickets", $"Removed Ticket {ticket.ticketID.ToString()}");
								break;
							}
						}
					}
					break;
						
				case "view":
					if(IsAdmin(player, false))
					{
						foreach(Ticket ticket in data.tickets)
						{
							if(ticket.ticketID.ToString() == arg)
							{
								SendChatMessage(player, "Tickets", $"<color=white><color=#00FF8D>------------------ Ticket {ticket.ticketID} ------------------</color>\n" +
									$"<color=#00FF8D>Timestamp:</color> {ticket.timestamp}\n" +
									$"<color=#00FF8D>Player:</color> {ticket.player}\n" +
									$"<color=#00FF8D>SteamID:</color> {ticket.steamID}\n" +
									$"<color=#00FF8D>Steam Profile:</color> {ticket.profile}\n" +
									$"<color=#00FF8D>Position</color>: {ticket.position}\n" +
									$"<color=#00FF8D>Message</color>: {ticket.message}\n" +
									$"<color=#00FF8D>Reply</color>: {ticket.reply}</color>"
								);
								
								player.ConsoleMessage($"<color=white><color=#00FF8D>------------------ Ticket {ticket.ticketID} ------------------</color>\n" +
									$"<color=#00FF8D>Timestamp:</color> {ticket.timestamp}\n" +
									$"<color=#00FF8D>Player:</color> {ticket.player}\n" +
									$"<color=#00FF8D>SteamID:</color> {ticket.steamID}\n" +
									$"<color=#00FF8D>Steam Profile:</color> {ticket.profile}\n" +
									$"<color=#00FF8D>Position</color>: {ticket.position}\n" +
									$"<color=#00FF8D>Message</color>: {ticket.message}\n" +
									$"<color=#00FF8D>Reply</color>: {ticket.reply}</color>"
								);
							}
						}
					}
					else
					{
						foreach(Ticket ticket in data.tickets)
						{
							if(ticket.ticketID.ToString() == arg && ticket.steamID == player.userID.ToString())
							{
								SendChatMessage(player, "Tickets", $"<color=white><color=#00FF8D>------------------ Ticket {ticket.ticketID} ------------------</color>\n" +
									$"<color=#00FF8D>Timestamp:</color> {ticket.timestamp}\n" +
									$"<color=#00FF8D>Message</color>: {ticket.message}\n" +
									$"<color=#00FF8D>Reply</color>: {ticket.reply}</color>"
								);
							}
							
						}
					}
					break;
						
				case "tp":
					foreach(Ticket ticket in data.tickets)
					{
						if(ticket.ticketID.ToString() == arg)
						{
							Teleport(player, new Vector3(ticket.x, ticket.y, ticket.z));
							SendChatMessage(player, "Tickets", $"Teleported to the position where <color=#00FF8D>Ticket {ticket.ticketID.ToString()}</color> has been submitted.");
							break;
						}
					}
					break;
				
				case "list":
					if(IsAdmin(player, false))
					{
						if(data.tickets.Count == 0) SendChatMessage(player, "Tickets", $"There are no active tickets.");
						else SendChatMessage(player, "Tickets", $"Tickets are shown in your player console. Press F1.");
						
						foreach(Ticket ticket in data.tickets)
						{
							player.ConsoleMessage($"\n\n<color=white><color=#00FF8D>------------------------------- Ticket {ticket.ticketID} -------------------------------</color>\n" +
								$"<color=#00FF8D>Timestamp:</color> {ticket.timestamp}\n" +
								$"<color=#00FF8D>Player</color>: {ticket.player}\n" +
								$"<color=#00FF8D>SteamID</color>: {ticket.steamID}\n" +
								$"<color=#00FF8D>Steam Profile</color>: {ticket.profile}\n" +
								$"<color=#00FF8D>Position</color>: {ticket.position}\n" +
								$"<color=#00FF8D>Message</color>: {ticket.message}\n" +
								$"<color=#00FF8D>Reply</color>: {ticket.reply}</color>\n\n"
							);
						}
					}
					else
					{
						int count = 0;
						
						foreach(Ticket ticket in data.tickets)
						{
							if(ticket.steamID != player.userID.ToString()) continue;
							count++;
							
							player.ConsoleMessage($"\n\n<color=white><color=#00FF8D>------------------------------- Ticket {ticket.ticketID} -------------------------------</color>\n" +
								$"<color=#00FF8D>Timestamp:</color> {ticket.timestamp}\n" +
								$"<color=#00FF8D>Message</color>: {ticket.message}\n" +
								$"<color=#00FF8D>Reply</color>: {ticket.reply}</color>\n\n"
							);
						}
						
						if(count == 0) SendChatMessage(player, "Tickets", $"There are no active tickets.");
						else SendChatMessage(player, "Tickets", $"Tickets are shown in your player console. <color=#00FF8D>Press F1.</color>");
					}
					break;
						
				case "clear":
					data.tickets.Clear();
					SendChatMessage(player, "Tickets", "Cleared all tickets.");
					break;
				
				default:
					break;
			}
			
			SaveData();
		}
		
		void SendTicketReply(string arg)
		{
			List<string> args = arg.Split(' ').ToList();
			
			int id = Convert.ToInt32(args[0]);
			string message = ListToString(args.ToList(), 1, " ");
			
			Ticket ticket = GetTicketByID(id);
			BasePlayer player = BasePlayer.Find(ticket.player);
			
			foreach(Ticket current in data.tickets)
			{
				if(current.ticketID == id) current.reply = message;
			}
			
			SaveData();
			
			if(player.IsConnected())
			{
				SendChatMessage(player, "Tickets", $"A reply to your Ticket has been made. To view it type <color=#00FF8D>/ticket view {ticket.ticketID.ToString()}</color>");
			}
		}
		
		void Teleport(BasePlayer player, Vector3 destination)
		{
			player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
			if(!BasePlayer.sleepingPlayerList.Contains(player))	BasePlayer.sleepingPlayerList.Add(player);
			
			player.CancelInvoke("InventoryUpdate");
			player.inventory.crafting.CancelAll(true);
			
			player.MovePosition(destination);
			player.ClientRPCPlayer(null, player, "ForcePositionTo", destination, null, null, null, null);
			player.TransformChanged();
			player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
			player.UpdateNetworkGroup();
			
			player.SendNetworkUpdateImmediate(false);
			player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
			player.SendFullSnapshot();
		}
		
        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

		BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				if(player.displayName.ToLower().Contains(searchedLower)) foundPlayers.Add(player.displayName);
			}
			
			switch(foundPlayers.Count)
			{
				case 0:
					SendChatMessage(executer, prefix, "The Player can not be found.");
					break;
					
				case 1:
					targetPlayer = BasePlayer.Find(foundPlayers[0]);
					break;
				
				default:
					string players = ListToString(foundPlayers, 0, ", ");
					SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
					break;
			}
			
            return targetPlayer;
        }

        //---------------------------->   Converting   <----------------------------//

        string ListToString(List<string> list, int first, string seperator)
		{
			return String.Join(seperator, list.Skip(first).ToArray());
		}

        //------------------------------>   Config   <------------------------------//

        void SetConfig(string Arg1, object Arg2, object Arg3 = null, object Arg4 = null)
		{
			if(Arg4 == null) 
			{
				Config[Arg1, Arg2.ToString()] = Config[Arg1, Arg2.ToString()] ?? Arg3;
			}
			else if(Arg3 == null) 
			{
				Config[Arg1] = Config[Arg1] ?? Arg2;
			}
			else
			{
				Config[Arg1, Arg2.ToString(), Arg3.ToString()] = Config[Arg1, Arg2.ToString(), Arg3.ToString()] ?? Arg4;
			} 
		}

        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

        //---------------------------------------------------------------------------//
        #endregion
    }
}
