using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Oxide.Plugins
{
	[Info("QuarryFactory", "Masteroliw", "1.0.3" , ResourceId = 1376)]
	[Description("Spawn items inside the quarry when it gathers resources")]
	public class QuarryFactory : RustPlugin
	{  

		#region OnQuarryGather

		void OnQuarryGather(MiningQuarry quarry, Item item)
		{
			var sulfur = ItemManager.CreateByName("sulfur.ore", 1);
			var metal = ItemManager.CreateByName("metal.ore", 1);
			var frags = ItemManager.CreateByName("metal.fragments", 1);
			var stones = ItemManager.CreateByName("stones", 1);
			var hqm = ItemManager.CreateByName("hq.metal.ore", 1);

			int randomnumber;
			System.Random generator = new System.Random ();
			
			randomnumber = generator.Next (1, 101);
			
			if (item.info.itemid == sulfur.info.itemid) {
				callSpawn ("SulfurOre", randomnumber, quarry);
				if (!(bool)Config["Options", "SulfurOreGather"]){
					item.amount = 0;
					item.Remove(0f);
					return;

				}
				return;
			}
			if (item.info.itemid == metal.info.itemid) {
				callSpawn ("MetalOre", randomnumber, quarry);
				if (!(bool)Config["Options", "MetalOreGather"]){
					item.amount = 0;
					item.Remove(0f);
					return;
				}
				return;
			}
			if (item.info.itemid == frags.info.itemid) {
				callSpawn ("MetalFrags", randomnumber, quarry);
				if (!(bool)Config["Options", "MetalFragsGather"]){
					item.amount = 0;
					item.Remove(0f);
					return;
				}
				return;
			}
			if (item.info.itemid == stones.info.itemid) {
				callSpawn ("Stones", randomnumber, quarry);
				if (!(bool)Config["Options", "StonesGather"]){
					item.amount = 0;
					item.Remove(0f);
					return;
				}
				return;
			}
			if (item.info.itemid == hqm.info.itemid) {
				callSpawn ("HQMetalOre", randomnumber, quarry);
				if (!(bool)Config["Options", "HighQualityMetalOreGather"]){
					item.amount = 0;
					item.Remove(0f);
					return;
				}
				return;
			}
			return;
			
		}

		#endregion

		#region callSpawn

		public void callSpawn (string name, int randomnumber, MiningQuarry quarry)
		{
			int t = 1;
			string item1 = "Item" + t;
			
			if ((string)Config[item1, "Item"] == null) {
				return;
			}
			
			do {
				
				string itemslot = "Item" + t;
				if ((string)Config[itemslot, "Item"] != null){
					if ((bool)Config [itemslot, "Active"]) {
						if ( name == (string)Config[itemslot, "SpawnsOn"] ) {
							
							if (randomnumber >= (int)Config[itemslot, "RandomNumberMin"] && randomnumber <= (int)Config[itemslot, "RandomNumberMax"]) {
								
								var gather = ItemManager.CreateByName((string)Config[itemslot, "Item"], (int)Config[itemslot, "Amount"]);	
								gather.MoveToContainer(quarry.hopperPrefab.instance.GetComponent<StorageContainer>().inventory);
								
							}
						}
					}
				} else {
					break;
				}
				
				t++;
				
			} while (t != 100);
			
			return;
		}

		#endregion

		#region LoadDefaultConfig
		
		protected override void LoadDefaultConfig()
		{
			PrintWarning("Creating a new configuration file.");
			Config.Clear();
			Config ["Options", "SulfurOreGather"] = false;
			Config ["Options", "MetalOreGather"] = false;
			Config ["Options", "MetalFragsGather"] = false;
			Config ["Options", "StonesGather"] = false;
			Config ["Options", "HighQualityMetalOreGather"] = false;
			SaveConfig();
		}

		#endregion

		#region Help Command

		[ChatCommand("help")]
		private void HelpCommand(BasePlayer player, string command, string[] args)
		{ 
			if (args.Length == 0) {
				PrintToChat (player, "/QF shows information about Quarry Controller.");
			}
		}

		#endregion

		#region QuarryFactory Commands

		[ChatCommand("QF")]
		private void QF(BasePlayer player, string command, string[] args)
		{ 
			int t = 1; 
			int c = 0;


			#region QuarryFactory Information Command

			if (args.Length == 0) {
				PrintToChat (player, "Type /QF and either of SulfurOre, MetalOre, MetalFrags, Stones, HQMetalOre");
				PrintToChat (player, "The command shows what items currently spawn and how common they occour, when that resource gets gathered");
				if (HasAccess(player))
				{
					PrintToChat (player, "The current item list can be found at the oxide docs for rust: (http://docs.oxidemod.org/rust/#item-list)");
					PrintToChat (player, "/QF NewItem (Item ShortName) (Amount) (RandomNumberMin) (RandomNumberMax) (ResourceType)");
					PrintToChat (player, "/QF EditItem (Item slot) (Item ShortName) (Amount) (RandomNumberMin) (RandomNumberMax) (ResourceType)");
					PrintToChat (player, "/QF Enable (Item slot)");
					PrintToChat (player, "/QF Disable (Item slot)");
					return;
				}
				return;
		    }

			#endregion

			#region Show items by resource type Command
			if (args.Length > 0) {
				string arg2 = args [0];
				string arg = GetResourceTypes (arg2);
				if (args [0] == arg) {
					do {
						string item = "Item" + t;

							if (args [0] == (string)Config [item, "SpawnsOn"]) 
							{
								PrintToChat (player ,"Item slot: " + item + " Item: " +
								             Config [item, "Item"] + " Amount: " + 
								             Config [item, "Amount"] + " RandomNumberMin: " + 
								             Config [item, "RandomNumberMin"] + " RandomNumberMax: " + 
								             Config [item, "RandomNumberMax"] + " Active: " + 
								             Config [item, "Active"]);
								c++;
							}

						if ((string)Config [item, "Item"] == null) {
							break;
						}
						t++; 
						
					} while (t != 100);
					
					if (c == 0){
						PrintToChat (player, "There's no items currently set to spawn on this resource");
					}
					return;
				}
		    }
			#endregion

			#region NewItem Command

			if (args [0] == "NewItem" || args [0] == "newitem") {
				if (HasAccess(player))
				{
					string arg2 = args [5];
					string arg = GetResourceTypes (arg2);
					if (args [5] == arg) {
						if (args.Length == 6) {
							do {
								string item = "Item" + t;
								if (Config [item, "Item"] == null) {
									int amount = Int32.Parse(args [2]);
									int min = Int32.Parse(args [3]);
									int max = Int32.Parse(args [4]);
									Config [item, "Item"] = args [1];
									Config [item, "Amount"] = amount;
									Config [item, "RandomNumberMin"] = min;
									Config [item, "RandomNumberMax"] = max;
									Config [item, "SpawnsOn"] = args[5];
									Config [item, "Active"] = true;
									SaveConfig();
									PrintToChat (player, "New item has filled slot " + item);
									break;
								}
								t++;
							} while (t != 100);
							return;
						}
						else {
							PrintToChat (player, "Insufficient/Too many arguments");
							return;
						}
					} else {
						PrintToChat (player, "Invalid ResourceType: " + args[5]);
						return;
					}
				} else {
					PrintToChat (player, "You don't have access to this command");
					return;
				}
			}
			#endregion
			 
			#region EditItem Command

			if (args [0] == "EditItem" || args [0] == "edititem") {
				if (HasAccess(player)){
					string arg2 = args [0];
					string arg = GetResourceTypes (arg2);
					if (args [1] == arg) {
						if (args.Length == 7 && (string)Config [args [1], "Item"] != null) {
							int amount = Int32.Parse(args [3]);
							int min = Int32.Parse(args [4]);
							int max = Int32.Parse(args [5]);
							Config [args [1], "Item"] = args [2];
							Config [args [1], "Amount"] = amount;
							Config [args [1], "RandomNumberMin"] = min;
							Config [args [1], "RandomNumberMax"] = max;
							Config [args [1], "SpawnsOn"] = args [6];
							Config [args [1], "Active"] = true;
							SaveConfig();
							PrintToChat (player, args [1] + " has been edited");
							return;
						} 
						if (args.Length == 7 && (string)Config [args [1], "Item"] == null) {
							int amount = Int32.Parse(args [3]);
							int min = Int32.Parse(args [4]);
							int max = Int32.Parse(args [5]);
							Config [args [1], "Item"] = args [2];
							Config [args [1], "Amount"] = amount;
							Config [args [1], "RandomNumberMin"] = min;
							Config [args [1], "RandomNumberMax"] = max;
							Config [args [1], "SpawnsOn"] = args [6];
							Config [args [1], "Active"] = true;
							SaveConfig();
							PrintToChat (player, args [1] + " did not exist, but has been now created");
							return;
						}
						else {
							PrintToChat (player, "Insufficient/Too many arguments");
							return;
						}
					} else {
						PrintToChat (player, "Invalid ResourceType: " + args[5]);
						return;
					}
				} else {
					PrintToChat (player, "You don't have access to this command");
					return;
				}
			}
			#endregion

			#region Disable and Enable Commands

			if (args [0] == "EnableItem" || args [0] == "enableitem"){
				if (HasAccess(player)){
					if (args.Length == 2) {
						if (Config [args [1], "Active"] != null) {
							Config [args [1], "Active"] = true;
							SaveConfig();
							PrintToChat (player, args [1] + " has been enabled");
							return;
						} else {
							PrintToChat (player, "The item you're trying to enable doesn't exist.");
							return;
						}
					} else {
						PrintToChat (player, "Insufficient/Too many arguments");
						return;
					}
				} else {
					PrintToChat (player, "You don't have access to this command");
					return;
				}
			}
			if (args [0] == "DisableItem" || args [0] == "disableitem"){
				if (HasAccess(player)){
					if (args.Length == 2) {
						if ((string)Config [args [1], "Active"] != null) {
							Config [args [1], "Active"] = false;
							SaveConfig();
							PrintToChat (player, args [1] + " has been disabled");
							return;
						} else {
							PrintToChat (player, "The item you're trying to disable doesn't exist.");
							return;
						}
					} else {
						PrintToChat (player, "Insufficient/Too many arguments");
						return;
					}
				} else {
					PrintToChat (player, "You don't have access to this command");
					return;
				}
			} 
			#endregion

			PrintToChat (player, "Either the command you have just typed does not exist, or you have not typed it correctly");

		} 
		#endregion

		#region GetResourceTypes

		public static string GetResourceTypes(string arg) {
			if ("SulfurOre" == arg) {
				return arg;
			}
			if ("MetalOre" == arg) {
				return arg;
			}
			if ("MetalFrags" == arg) {
				return arg;
			}
			if ("Stones" == arg) {
				return arg;
			}
			if ("HQMetalOre" == arg) {
				return arg;
			}
			return null;
		}

		#endregion

		bool HasAccess(BasePlayer player)
		{
			if (player.net.connection.authLevel > 1) {
				return true;
			}
			return false;
		} 
	} 
} 

