using System.Collections.Generic;
using System.Reflection;
using System;
using Oxide.Core; 
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
 
namespace Oxide.Plugins
{
    [Info("RPS Battles", "PaiN", "0.3", ResourceId = 1929)] 
    [Description("Rock Paper Scissors game")]
    class RPSBattles : RustPlugin 
    { 
		[PluginReference]
        Plugin Economics;
		
		private static FieldInfo _condition = typeof(Item).GetField("_condition", (BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.GetField));
		
		void Loaded()
		{	
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("RPSBattles");		
			LoadDefaultMessages();
			LoadDefaultConfig();
		}
		
		string sendlang(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }

		void LoadDefaultMessages()
        {
			Dictionary<string, string> messages = new Dictionary<string, string>
			{
				{"CONNECT_DRAW_ITEMS_BACK", "The items from your battle are back in your inventory since it was a draw."},
				{"CONNECT_DRAW_ITEMS_MSG", "NAME: {0} AMMOUNT: {1} CONDITION: {2}"},
				{"CONNECT_WIN_ITEMS_MSG", "Won Items from your battles: \nNAME: {0} AMMOUNT: {1} CONDITION: {2}"},
				{"RPS_LIST", "ID:{0} | Item-Name:{1} | Amount: {2}"},
				{"CMD_CREATE_INCORRECT_SYNTAX", "Incorrect Syntax! || /rps create <RPSchoice> || Example: /rps create r (<r> == rock.)"},
				{"CMD_CREATE_PUT_ITEM", "Please put the item that you want to set a battle for in your first slot of your belt."},
				{"CMD_CREATE_MAX_BATTLES", "You currently have max allowed battles running. Wait for them to finish"},
				{"CMD_CREATE_BATTLE_CREATED", "Battle created."},
				{"CMD_CREATE_RPS_SYNTAX", "Syntax: /rps create <r/p/s>"},
				{"CMD_PLAY_INCORRECT_SYNTAX", "Incorrect Syntax! || /rps play <BattleID> <RPSchoice>"},
				{"CMD_PLAY_DRAW_MSG", "It's a draw!"},
				{"CMD_PLAY_WIN_MSG", "You WON a RPS Battle."},
				{"BROADCAST_WIN","<color=lime>{0}</color> <color=#91FFB5>WON</color> against <color=red>{1}</color> in a RPS Battle"},
				{"BROADCAST_LOSE", "<color=red>{0}</color> <color=yellow>LOST</color> against <color=lime>{1}</color> in a RPS Battle"},
				{"CMD_PLAY_LOSE_MSG", "You have lost a RPS Battle."},
				{"DONT_HAVE_ITEMS", "You dont have that item/item amount"},
				{"RPS_CHOICE_MSG", "RPSchoice must be <r/p/s> || Example: /rps play 1 s  (<s> == scissors)"},
				{"RPS_CREATE_NOT_ENOUGH_MONEY", "You dont have enough money to create a battle! || Battle Cost: {0}"},
				{"RPS_CREATE_PAID", "You paid <color=#91FFB5>{0}</color>$ to create a RPS Battle."},
				{"YOU_NEED_TO_BE_CLOSER", "You need to be closer!"},
				{"CANT_PLAY_OWN_BATTLE", "You can not play your own battle!"}
			};
				
	 
            lang.RegisterMessages(messages, this);
        }
		
		
		protected override void LoadDefaultConfig()
		{ 
			Dictionary<string, object> dict = new Dictionary<string, object>
			{
				{"Assault Rifle", "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200609"},
				{"Bolt Action Rifle", "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200415"},
				{"Camp Fire", "http://vignette4.wikia.nocookie.net/play-rust/images/3/35/Camp_Fire_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060846"},
				{"Wood", "http://vignette4.wikia.nocookie.net/play-rust/images/f/f2/Wood_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061551"}
			}; 
			Config["RPS_CREATE_COST"] = GetConfig("RPS_CREATE_COST", 100);
			Config["Item-Images"] = GetConfig("Item-Images", dict);
			Config["SafeMode-Activated"] = GetConfig("SafeMode-Activated", false);
			Config["SafeBattle-Distance"] = GetConfig("SafeBattle-Distance", 5);
		    SaveConfig();
		}
		
		
			
		void Unloaded()
		{
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			CuiHelper.DestroyUi(player, "BackroundGUI");
		}
		
		class Data
		{
			public List<BattleInfo> Battles = new List<BattleInfo>{};
		}
		Data data;
		
		void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("RPSBattles", data);

		
		public class BattleInfo
		{
			
			
			public int battleid;
			//Creator
			public string ccreatorname;
			public string citemtype;
			public string citemfullname;
			public int citemid;
			public int ciamount;
			public bool cisbp;
			public string cRPSbet;
			public ulong csteamid;
			public string citemshortname;
			public int cicondition;
			//--------------
			//Enemy
			public string eplayername;
			public string eitemtype;
			public string eitemfullname;
			public int eitemid;
			public int eiamount;
			public bool eisbp;
			public string eRPSbet;
			public ulong esteamid;
			public string eitemshortname;
			public int eicondition;
			//--------------
			public bool enabled;
			public string status;
			
			public BattleInfo(string stats, string bet, int id, BasePlayer player, Item item, bool rpsenabled)
			{
				status = stats;
				enabled = rpsenabled;
				battleid = id;
				//Creator
				ccreatorname = player.displayName;
				cisbp = item.IsBlueprint();
				csteamid = player.userID;
				citemid = item.info.itemid;
				citemshortname = item.info.shortname;
				citemfullname = item.info.displayName.english.ToString();
				ciamount = item.amount;
				citemtype = item.info.category.ToString();
				cicondition = Convert.ToInt32(_condition.GetValue(item));
				cRPSbet = bet;
				//--------
				//Enemy
				eplayername = "";
				eisbp = false;
				esteamid = 0;
				eitemid = 0;
				eitemshortname = "";
				eitemfullname = "";
				eiamount = 0;
				eitemtype = "";
				eicondition = 0;
				eRPSbet = "";
				//--------
			}
			
			public BattleInfo()
			{}
		}
		
		
		void UseUI(BasePlayer player, string itemurl, string itemurl2, string rightginfo, string leftginfo)
		{

			CuiElementContainer elements = new CuiElementContainer();
			var backround = elements.Add(new CuiPanel
			{
				Image =
				{
					Color = "0.1 0.1 0.1 0.7"
				},
				RectTransform =
				{
					AnchorMin = "0.8 0.32",
					AnchorMax = "1 0.8"
				},
				CursorEnabled = true
			}, "HUD/Overlay", "BackroundGUI"); 
			elements.Add(new CuiElement
            {
				Parent = "BackroundGUI",
                Components =
				{
					new CuiRawImageComponent
					{
						Url = itemurl,
						Sprite = "assets/content/textures/generic/fulltransparent.tga"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0 0.55",
						AnchorMax = "0.3 0.9"
					}
				}
            });
			elements.Add(new CuiElement
            {
				Parent = "BackroundGUI",
                Components =
				{
					new CuiRawImageComponent
					{
						Url = itemurl2,
						Sprite = "assets/content/textures/generic/fulltransparent.tga"
					},
					new CuiRectTransformComponent
					{
						AnchorMin = "0.7 0.55",
						AnchorMax = "1 0.9"
					}
				}
            });
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = leftginfo, 
                    FontSize = 14,
                    Align = TextAnchor.UpperLeft
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0",
                    AnchorMax = "0.45 0.55"
                }
			}, "BackroundGUI");
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = rightginfo, 
                    FontSize = 14,
                    Align = TextAnchor.UpperRight
                },
                RectTransform =
                {
                    AnchorMin = "0.55 0",
                    AnchorMax = "0.95 0.55"
                }
			}, "BackroundGUI");
			var closeback = new CuiButton
            {
                Button =
                {
                    Close = "BackroundGUI",
                    Color = "255 0 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.9 0.9",
					AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "x",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
			elements.Add(closeback, backround);
			CuiHelper.AddUi(player, elements);
		}
		
		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if(Convert.ToBoolean(Config["SafeMode-Activated"]) == true)
			{ 
				for (int i = 0; i < data.Battles.Count; i++)
				{
					BattleInfo info = data.Battles[i];
					if(data.Battles.Any(x => x.csteamid == player.userID))
					{
						info.enabled = false;
						SaveData();
						return;
						break;
					}
				}
			}
		}

		
		void OnPlayerInit(BasePlayer player)
		{

			for (int i = 0; i < data.Battles.Count; i++)
			{
				BattleInfo info = data.Battles[i];
				if(Convert.ToBoolean(Config["SafeMode-Activated"]) == true)
				{ 
					if(data.Battles.Any(x => x.csteamid == player.userID))
					{
						info.enabled = true;
						SaveData();
						return;
						break;
					}
				}
				if(info.csteamid == player.userID)
				{
					if(info.status == "Draw")
					{
						SendReply(player, sendlang("CONNECT_DRAW_ITEMS_BACK", player.userID.ToString()));
						SendReply(player,  sendlang("CONNECT_DRAW_ITEMS_MSG", player.userID.ToString()), info.citemfullname, info.ciamount, info.cicondition);
						Item item = ItemManager.CreateByItemID(info.citemid, info.ciamount, info.cisbp, 0);
						player.inventory.GiveItem(item);
						data.Battles.Remove(info);
						SaveData();
					}
					if(info.status == "Win")
					{
						Item hisitem = ItemManager.CreateByItemID(info.citemid, info.ciamount, info.cisbp, 0);
						player.inventory.GiveItem(hisitem);
						Item item = ItemManager.CreateByItemID(info.eitemid, info.eiamount, info.eisbp, 0);
						player.inventory.GiveItem(item);
						data.Battles.Remove(info);
						SendReply(player, sendlang("CONNECT_WIN_ITEMS_MSG", player.userID.ToString()), info.eitemfullname, info.eiamount + info.ciamount, info.cicondition);
						SaveData();
					}
					if(info.status == "Lose")
					{
						data.Battles.Remove(info);
						SaveData();
					}
				}
			}
		}
		
		int GetNewId(string battle)
		{
			int id = 0;
			if(battle == "Items")
			{
				foreach(BattleInfo info in data.Battles)
				{
					id = Math.Max(0, info.battleid);
				}
			}
			/*if(battle == "Money")
			foreach(MoneyBattleInfo minfo in data.MoneyBattles)
			{
				id = Math.Max(0, minfo.mbattleid);
			}*/
			return id + 1;
		} 
		
		int GetPlayerBattleCount(BasePlayer player)
		{
			int count = 0;
			foreach(BattleInfo info in data.Battles)
			if(info.csteamid == player.userID)
			count++;
			
			return count;
		}
		 
		[ChatCommand("rps")]
		void cmdRPS(BasePlayer player, string cmd, string[] args)
		{
			switch(args[0])
			{
				case "help":
				SendReply(player, "<color=#91FFB5>Available Commands</color>");
				SendReply(player, "<color=#91FFB5>-</color> /rps list => Shows the current RPS Battles.");
				SendReply(player, "<color=#91FFB5>-</color> /rps list <namepart_of_item> => Shows the RPS Battles that contain the item that you put in the argument.");
				SendReply(player, "<color=#91FFB5>-</color> /rps create <r/p/s> => Create RPS Battles | r = rock, p = paper, s = scissors");
				SendReply(player, "<color=#91FFB5>-</color> /rps play <BattleId> <r/p/s> => Play against already made Battles from players | /rps list");
				break;
				case "list":
				if(args.Length == 1)
				{
					foreach(BattleInfo info in data.Battles)
					{
						if(info.enabled == true)
						SendReply(player, sendlang("RPS_LIST", player.userID.ToString()), info.battleid, info.citemfullname, info.ciamount);
					}
				}
				else if(args.Length == 2)
				{
					foreach(BattleInfo info in data.Battles)
					{
						if(info.citemfullname.ToLower().Contains(args[1].ToLower()))
						SendReply(player, sendlang("RPS_LIST", player.userID.ToString()), info.battleid, info.citemfullname, info.ciamount);
					}
				}
				break;
				case "create": 
				
				if(args.Length != 2)
				{
					SendReply(player, sendlang("CMD_CREATE_INCORRECT_SYNTAX", player.userID.ToString()));
					return;
				}
				if(player.inventory.containerBelt.SlotTaken(0) == false)
				{
					SendReply(player, sendlang("CMD_CREATE_PUT_ITEM", player.userID.ToString()));
					return;
				}
				if(GetPlayerBattleCount(player) >= 3)
				{
					SendReply(player, sendlang("CMD_CREATE_MAX_BATTLES", player.userID.ToString()));
					return;
				}
				if(Convert.ToInt32(Config["RPS_CREATE_COST"]) > 0 && plugins.Find("Economics"))
				{ 
					var playermoney = (double) Economics?.CallHook("GetPlayerMoney", player.userID);
					if(playermoney < Convert.ToInt32(Config["RPS_CREATE_COST"]))
					{
						SendReply(player, sendlang("RPS_CREATE_NOT_ENOUGH_MONEY", player.userID.ToString()), Config["RPS_CREATE_COST"].ToString());
						return;
					}
					Economics?.CallHook("Withdraw", player.userID, Convert.ToInt32(Config["RPS_CREATE_COST"]));
					SendReply(player, sendlang("RPS_CREATE_PAID", player.userID.ToString()), Convert.ToInt32(Config["RPS_CREATE_COST"]).ToString());
				} 
				if(args[1] == "r" || args[1] == "p" || args[1] == "s")
				{ 
					
					Item torps = player.inventory.containerBelt.GetSlot(0);
					string rps = args[1].ToString();
					var idata = new BattleInfo("Awaiting", rps, GetNewId("Items"), player, torps, true);
					data.Battles.Add(idata);
					SendReply(player, sendlang("CMD_CREATE_BATTLE_CREATED", player.userID.ToString()));
					player.inventory.containerBelt.Take(new List<Item>{}, torps.info.itemid, torps.amount);
					SaveData();
				} 
			/*	else if(args[1].ToString().All(char.IsDigit) && plugins.Find("Economics"))
				{
					string rps = args[1].ToString();
					var idata = new MoneyBattleInfo("Awaiting", player, GetNewId("Money"), player, );
					data.Battles.Add(idata);
					SendReply(player, sendlang("CMD_CREATE_BATTLE_CREATED", player.userID.ToString()));
					SaveData();
				} */
				else
				{
					SendReply(player, sendlang("CMD_CREATE_RPS_SYNTAX", player.userID.ToString()));
					return;
				}

				break;
				
				case "play":
				if(args.Length != 3)
				{
					SendReply(player, sendlang("CMD_PLAY_INCORRECT_SYNTAX", player.userID.ToString()));
					return;
				}
				if(args[2] == "r" || args[2] == "p" || args[2] == "s")
				{
					
					for (int i = 0; i < data.Battles.Count; i++)
					{
						BattleInfo info = data.Battles[i];
						if(info.battleid == Convert.ToInt32(args[1]))
						{
							if(Convert.ToBoolean(Config["SafeMode-Activated"]) == true)
							{ 
								BasePlayer cr = BasePlayer.Find(info.csteamid.ToString());
								if(info.enabled == false && cr == null)
								{
									SendReply(player, "Incorrect BattleID");
									return;
								}
								if(Vector3.Distance(player.transform.position, cr.transform.position) <= Convert.ToInt32(Config["SafeBattle-Distance"]))	
								goto Finish;
								else
								{
									SendReply(player, sendlang("YOU_NEED_TO_BE_CLOSER", player.userID.ToString()));
									return;
								}
							}
							Finish:
							if(info.csteamid == player.userID)
							{
								SendReply(player, "You can not play your own battle!");
								return;
							}

							if(player.inventory.GetAmount(info.citemid) >= info.ciamount)
							{
							
								info.status = "In Progress";
								string rpschoice = args[2].ToString();
								if(rpschoice == info.cRPSbet)// Draw
								{
									info.status = "Draw";
									SendReply(player, sendlang("CMD_PLAY_DRAW_MSG", player.userID.ToString()));
									BasePlayer creator = BasePlayer.Find(info.csteamid.ToString());
									if(BasePlayer.activePlayerList.Contains(creator))
									{
										SendReply(creator, sendlang("CMD_PLAY_DRAW_MSG", creator.userID.ToString()));
										Item item = ItemManager.CreateByItemID(info.citemid, info.ciamount, info.cisbp, 0);
										creator.inventory.GiveItem(item);
										data.Battles.Remove(info);
										SaveData();
									}
									else
									info.enabled = false;
									
								}
								if(info.cRPSbet == "r")
								{
									if(rpschoice == "p")//LOSE
									{
										Lose(player, info);
										
									}
									if(rpschoice == "s")//WIN
									{
										Win(player, info, rpschoice);
									}
									
								}
								if(info.cRPSbet == "p")
								{
									if(rpschoice == "s")//LOSE
									{
										Lose(player, info);
										
									}
									if(rpschoice == "r")//WIN
									{
										
										Win(player, info, rpschoice);
									}
									
								}
								if(info.cRPSbet == "s")
								{
									if(rpschoice == "r")//LOSE
									{
										Lose(player, info);
										
									}
									if(rpschoice == "p")//WIN
									{
										Win(player, info, rpschoice);
									}
								}
							}
							else
							{
								SendReply(player, sendlang("DONT_HAVE_ITEMS", player.userID.ToString()));
								return;
							}
						}
					}	
				}
				else
				{
					SendReply(player, sendlang("RPS_CHOICE_MSG", player.userID.ToString()));
					return;
				}
				break;				
			}
		}
		string GetItemImage(string itemname)
		{
			string itemurl = "";
			foreach(var v in Config["Item-Images"] as Dictionary<string, object>)
			{
				if(itemname == v.Key.ToString())
				itemurl = v.Value.ToString();
			}
			return itemurl;
		}
		
		string GetFullChoise(string s)
		{
			string fullchoice = "";
			if(s == "r")
				return "ROCK";
			else if(s == "p")
				return "PAPER";
			else if(s == "s")
				return "SCISSORS";
			return "ERROR";
		}
		
		void Win(BasePlayer player, BattleInfo info, string rpschoice)
		{
			Item eitem = player.inventory.containerMain.FindItemByItemID(info.citemid);
			info.eisbp = eitem.IsBlueprint();
			info.esteamid = player.userID;
			info.eitemid = eitem.info.itemid;
			info.eplayername = player.displayName;
			info.eitemshortname = eitem.info.shortname;
			info.eitemfullname = eitem.info.displayName.english.ToString();
			info.eiamount = eitem.amount;
			info.eitemtype = eitem.info.category.ToString();
			info.eicondition = Convert.ToInt32(_condition.GetValue(eitem));
			DisplayGUI(player, info.eitemfullname, info.eiamount, info.eicondition, info.citemfullname, info.ciamount, Convert.ToInt32(info.cicondition), GetFullChoise(rpschoice).ToString(), GetFullChoise(info.cRPSbet).ToString());
			info.status = "Win";
			SendReply(player, sendlang("CMD_PLAY_LOSE_MSG", player.userID.ToString()));
			player.inventory.containerMain.Take(new List<Item>{}, eitem.info.itemid, eitem.amount);
			BasePlayer creator = BasePlayer.Find(info.csteamid.ToString());
			PrintToChat(sendlang("BROADCAST_WIN", null), creator.displayName, player.displayName);
			if(BasePlayer.activePlayerList.Contains(creator))
			{
				SendReply(creator, sendlang("CMD_PLAY_WIN_MSG", creator.userID.ToString()));
				Item item = ItemManager.CreateByItemID(info.citemid, info.ciamount, info.cisbp, 0);
				Item wonitem = ItemManager.CreateByItemID(info.eitemid, info.eiamount, info.eisbp, 0);
				creator.inventory.GiveItem(item); 
				creator.inventory.GiveItem(wonitem);
				data.Battles.Remove(info);
				SaveData();
			}
			else
			info.enabled = false;
		}
		
		void Lose(BasePlayer player, BattleInfo info)
		{
			info.status = "Lose";
			SendReply(player, sendlang("CMD_PLAY_WIN_MSG", player.userID.ToString()));
			Item wonitem = ItemManager.CreateByItemID(info.citemid, info.ciamount, info.cisbp, 0);
			player.inventory.GiveItem(wonitem);
			BasePlayer creator = BasePlayer.Find(info.csteamid.ToString());
			PrintToChat(sendlang("BROADCAST_LOSE", null), creator.displayName, player.displayName);										
			if(BasePlayer.activePlayerList.Contains(creator))
			{
				SendReply(creator, sendlang("CMD_PLAY_LOSE_MSG", creator.userID.ToString()));
				data.Battles.Remove(info);
				SaveData();
			} 
			else
			info.enabled = false;
		}
		
		
		void DisplayGUI(BasePlayer player, string enifullname, int eniamount, int enicondition, string crifullname, int criamount, int cricondition, string lchoice, string rchoice)
		{
			string leftm = "";
			string rightm = "";
			List<object> left = new List<object>{
				"NAME-LEFT",
				$"{enifullname}",
				"--------------",
				"AMOUNT",
				$"{eniamount}",
				"--------------",
				"CONDITION",
				$"{enicondition}",
				"***************\n",
				$"<size=16><color=yellow>YOU</color>: <color=lime>{lchoice}</color></size>"
			};
			List<object> right = new List<object>{
				"NAME-RIGHT",
				$"{crifullname}",
				"--------------",
				"AMOUNT",
				$"{criamount}",
				"--------------",
				"CONDITION",
				$"{cricondition}",
				"***************\n",
				$"<size=16><color=magenta>{rchoice}</color></size>"
			};
			foreach(var l in left as List<object>)
			leftm = leftm + l.ToString() + "\n";
			foreach(var r in right as List<object>)
			rightm = rightm + r.ToString() + "\n";
			UseUI(player, GetItemImage(enifullname),GetItemImage(crifullname), rightm, leftm);
		}
		
		T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
	}
}
