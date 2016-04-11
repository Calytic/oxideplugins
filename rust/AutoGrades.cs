/*
	Created By AlexALX (c) 2015-2016
*/
using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{                 
    [Info("Automatic Build Grades", "AlexALX", "0.0.6", ResourceId = 921)]
    public class AutoGrades : RustPlugin
    {
	
		private Dictionary<string,int> playerGrades;
		private bool LoadDefault = false;
		private bool block = true;
		private string cmdname = "bgrade";

        void Loaded() {
            playerGrades = new Dictionary<string, int>();
			
			// DO NOT EDIT THIS LINES! PLEASE USE oxide/lang/AutoGrades.en.json or own language name!
			lang.RegisterMessages(new Dictionary<string,string>{
				["BGRADE_NOPERM"] = "<color='#DD0000'>You have no access to this command.</color>",
				["BGRADE_NORES"] = "<color='#DD0000'>Not enough resources for construct and upgrade.</color>",
				["BGRADE_NORES2"] = "<color='#DD0000'>Not enough resources for upgrade.</color>",
				["BGRADE_HELP"] = "Automatic Build Grade command usage:",
				["BGRADE_1"] = "<color='#00DD00'>/{0} 1</color> - auto update to wood",
				["BGRADE_2"] = "<color='#00DD00'>/{0} 2</color> - auto update to stone",
				["BGRADE_3"] = "<color='#00DD00'>/{0} 3</color> - auto update to metal",
				["BGRADE_4"] = "<color='#00DD00'>/{0} 4</color> - auto update to armored",
				["BGRADE_0"] = "<color='#00DD00'>/{0} 0</color> - disable auto update",
				["BGRADE_CUR"] = "Current mode: <color='#DD0000'>{0}</color>",
				["BGRADE_SET"] = "<color='#00DD00'>You successfully set auto update to <color='#DD0000'>{0}</color>.</color>",
				["BGRADE_DIS"] = "<color='#00DD00'>You successfully <color='#DD0000'>disabled</color> auto update.</color>",
				["BGRADE_INV"] = "<color='#DD0000'>Invalid building grade.</color>",
				["Disabled"] = "Disabled",
				["Wood"] = "Wood",
				["Stone"] = "Stone",
				["Metal"] = "Metal",
				["TopTier"] = "TopTier",
			}, this);

			lang.RegisterMessages(new Dictionary<string,string>{
				["BGRADE_NOPERM"] = "<color='#DD0000'>ÐÐµÐ´Ð¾ÑÑÐ°ÑÐ¾ÑÐ½Ð¾ Ð¿ÑÐ°Ð² Ð´Ð»Ñ Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°Ð½Ð¸Ñ Ð´Ð°Ð½Ð½Ð¾Ð¹ ÐºÐ¾Ð¼Ð°Ð½Ð´Ñ.</color>",
				["BGRADE_NORES"] = "<color='#DD0000'>ÐÐµÐ´Ð¾ÑÑÐ°ÑÐ¾ÑÐ½Ð¾ ÑÐµÑÑÑÑÐ¾Ð² Ð´Ð»Ñ Ð¿Ð¾ÑÑÑÐ¾Ð¹ÐºÐ¸ Ð¸ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ñ.</color>",
				["BGRADE_NORES2"] = "<color='#DD0000'>ÐÐµÐ´Ð¾ÑÑÐ°ÑÐ¾ÑÐ½Ð¾ ÑÐµÑÑÑÑÐ¾Ð² Ð´Ð»Ñ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ñ.</color>",
				["BGRADE_HELP"] = "ÐÐ²ÑÐ¾Ð¼Ð°ÑÐ¸ÑÐµÑÐºÐ¾Ðµ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ ÐºÐ¾Ð½ÑÑÑÑÐºÑÐ¸Ð¸, Ð¸ÑÐ¿Ð¾Ð»ÑÐ·Ð¾Ð²Ð°Ð½Ð¸Ðµ:",
				["BGRADE_1"] = "<color='#00DD00'>/{0} 1</color> - Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ Ð´Ð¾ Ð´ÐµÑÐµÐ²Ð°",
				["BGRADE_2"] = "<color='#00DD00'>/{0} 2</color> - Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ Ð´Ð¾ ÐºÐ°Ð¼Ð½Ñ",
				["BGRADE_3"] = "<color='#00DD00'>/{0} 3</color> - Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ Ð´Ð¾ Ð¼ÐµÑÐ°Ð»Ð°",
				["BGRADE_4"] = "<color='#00DD00'>/{0} 4</color> - Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ Ð´Ð¾ Ð±ÑÐ¾Ð½Ð¸ÑÐ¾Ð²Ð°Ð½Ð¾Ð³Ð¾",
				["BGRADE_0"] = "<color='#00DD00'>/{0} 0</color> - Ð¾ÑÐºÐ»ÑÑÐ¸ÑÑ Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ",
				["BGRADE_CUR"] = "Ð¢ÐµÐºÑÑÐ¸Ð¹ ÑÐµÐ¶Ð¸Ð¼: <color='#DD0000'>{0}</color>",
				["BGRADE_SET"] = "<color='#00DD00'>ÐÑ ÑÑÐ¿ÐµÑÐ½Ð¾ ÑÑÑÐ°Ð½Ð¾Ð²Ð¸Ð»Ð¸ Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ Ð´Ð¾: <color='#DD0000'>{0}</color>.</color>",
				["BGRADE_DIS"] = "<color='#00DD00'>ÐÑ ÑÑÐ¿ÐµÑÐ½Ð¾ <color='#DD0000'>Ð²ÑÐºÐ»ÑÑÐ¸Ð»Ð¸</color> Ð°Ð²ÑÐ¾ Ð¾Ð±Ð½Ð¾Ð²Ð»ÐµÐ½Ð¸Ðµ.</color>",
				["BGRADE_INV"] = "<color='#DD0000'>ÐÐµÐ²ÐµÑÐ½ÑÐ¹ ÐºÐ»Ð°ÑÑ Ð¿Ð¾ÑÑÑÐ¾Ð¹ÐºÐ¸.</color>",
				["Disabled"] = "ÐÑÐºÐ»ÑÑÐµÐ½Ð¾",
				["Wood"] = "ÐÐµÑÐµÐ²Ð¾",
				["Stone"] = "ÐÐ°Ð¼ÐµÐ½Ñ",
				["Metal"] = "ÐÐµÑÐ°Ð»",
				["TopTier"] = "ÐÑÐ¾Ð½Ð¸ÑÐ¾Ð²Ð°Ð½ÑÐ¹",
			}, this, "ru");
				
        }
		/* Not sure if this needed, maybe someone reconnect or so...
        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player)
        {
			var steamId = player.userID.ToString();
            if (playerGrades.ContainsKey(steamId)) {
				playerGrades.Remove(steamId);
			}
        }*/
		
		void LoadDefaultConfig() {
			LoadDefault = true;
		}
		
		void OnServerInitialized() {
			permission.RegisterPermission("autogrades.all", this);
			permission.RegisterPermission("autogrades.1", this);
			permission.RegisterPermission("autogrades.2", this);
			permission.RegisterPermission("autogrades.3", this);
			permission.RegisterPermission("autogrades.4", this);
			permission.RegisterPermission("autogrades.nores", this);
			
			// config update from 0.0.3
			var gperm = false;
			if (!LoadDefault && (Config["Language"]!=null || Config["Messages"]!=null)) {
				Config.Remove("Language");
				Config["OldMessages"] = Config["Messages"]; // i don't want remove someone translation/customization without any warnings!
				Config.Remove("Messages");
				// not sure if this is correct way of show warning, but it works.
				Interface.Oxide.LogInfo("[{0}] {1}", Title, "Config successfully updated!");
				Interface.Oxide.LogWarning("[{0}] {1}", Title, "Please re-assign player rights! Old rights will not work!");
				gperm = true;
			}
			
			if (Config["OldMessages"]!=null) {
				Interface.Oxide.LogWarning("[{0}] {1}", Title, "Outdated messages detected!");
				Interface.Oxide.LogWarning("[{0}] {1}", Title, "Please remove them from config after merge to oxide/lang file.");
			}
			// end of config update
			
			if (LoadDefault || Config["Block Construct and Refund"]==null || gperm) {
				permission.GrantGroupPermission("admin","autogrades.all",this);
				permission.GrantGroupPermission("admin","autogrades.nores",this);
			}
			ReadFromConfig<bool>("Block Construct and Refund", ref block);
			ReadFromConfig<string>("Command", ref cmdname);
			SaveConfig();
			
			var command = Interface.Oxide.GetLibrary<Command>();
			command.AddChatCommand(cmdname, this, "ChatBuildGrade");	
		}
		
        private void ReadFromConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null) {
				var = (T)Convert.ChangeType(Config[Key], typeof(T));
			}
			Config[Key] = var;
        }
		
		private string GetMessage(string name, string sid = null) {
			return lang.GetMessage(name, this, sid);
		}
		
		private bool HasPerm(BasePlayer ply, string perm) {
			return permission.UserHasPermission(ply.userID.ToString(), "autogrades."+perm);
		}
	
		private int PlayerGrade(string steamId, bool cache = true) {
            if (playerGrades.ContainsKey(steamId)) return playerGrades[steamId];
			if (!cache) return 0;
            playerGrades[steamId] = 0;
            return playerGrades[steamId];
		}
		
		private bool HasAnyPerm(BasePlayer player) {
			return (HasPerm(player,"all") || HasPerm(player,"1") || HasPerm(player,"2") || HasPerm(player,"3") || HasPerm(player,"4"));
		}
		
		private bool CanAffordUpgrade(int grade, BuildingBlock buildingBlock, BasePlayer player)
		{
			bool flag = true;
			List<ItemAmount>.Enumerator enumerator = buildingBlock.blockDefinition.grades[(int)buildingBlock.grade].costToBuild.GetEnumerator(); //this[iGrade].costToBuild.GetEnumerator();
			/*try
			{*/
			// Add cost of build grade 0
			Dictionary<int,float> costs = new Dictionary<int,float>();
			while (enumerator.MoveNext())
			{
				ItemAmount current = enumerator.Current;
				costs[current.itemid] = current.amount;
			}
			
			enumerator = buildingBlock.blockDefinition.grades[grade].costToBuild.GetEnumerator();
			
			// Calc needed costs for upgrade
			while (enumerator.MoveNext())
			{
				ItemAmount current = enumerator.Current;
				var cost = 0f;
				if (costs.ContainsKey(current.itemid)) {
					cost = costs[current.itemid];
					costs.Remove(current.itemid);
				}
				if (player.inventory.GetAmount(current.itemid) >= current.amount+cost)
				{
					continue;
				}
				flag = false;
				return flag;
			}
			
			// check for build grade 0 and needed cost (additional resources)
			if (costs.Count>0) {
				foreach(KeyValuePair<int,float> kvp in costs) {
					if (player.inventory.GetAmount(kvp.Key) >= kvp.Value) {
						continue;
					}
					flag = false;
					return flag;
				}
			}
			//return true;
			/*}
			finally
			{
				((IDisposable)(object)enumerator).Dispose();
			}*/
			return flag;
		}
		
		/* Example of hook usage
		int CanAutoGrade(BasePlayer player, int grade, BuildingBlock buildingBlock, Planner planner) {
			//return -1; // Block upgrade, but create twig part
			//return 0; // Obey plugin settings (block on construct if enabled or not)
			//return 1; // Block upgrade and block build
			return; // allow upgrade
		}*/
		
        void OnEntityBuilt(Planner planner, UnityEngine.GameObject gameObject) {	
			var player = planner.ownerPlayer;
            if (!HasAnyPerm(player)) return;
			BuildingBlock buildingBlock = gameObject.GetComponent<BuildingBlock>();
			if (buildingBlock==null) return;
			var steamId = player.userID.ToString();
			var pgrade = PlayerGrade(steamId,false);
			if (pgrade>0) {
				if (!HasPerm(player,"all") && !HasPerm(player,pgrade.ToString())) return;
				var result = Interface.CallHook("CanAutoGrade", new object[] { player, pgrade, buildingBlock, planner });
				if (result is int) {
					if ((int)result==0 && !block || (int)result<0) return;
					var items = buildingBlock.blockDefinition.grades[(int)buildingBlock.grade].costToBuild;
					foreach (ItemAmount itemAmount in items)
					{
						player.Command(string.Concat(new object[] { "note.inv ", itemAmount.itemid, " ", (int)itemAmount.amount }), new object[0]);
						player.inventory.GiveItem(ItemManager.CreateByItemID(itemAmount.itemid, (int)itemAmount.amount),player.inventory.containerMain);
					}
					gameObject.GetComponent<BaseEntity>().KillMessage();
					return;
				}
				if (!HasPerm(player,"nores")) {
					int amount = 0;
					if (pgrade>(int)buildingBlock.grade&&buildingBlock.blockDefinition.grades[pgrade]) {
						var items = buildingBlock.blockDefinition.grades[(int)buildingBlock.grade].costToBuild;
						if (!CanAffordUpgrade(pgrade,buildingBlock,player)) {
							if (!block) { player.ChatMessage(GetMessage("BGRADE_NORES2",steamId)); return; }
							foreach (ItemAmount itemAmount in items)
							{
								player.Command(string.Concat(new object[] { "note.inv ", itemAmount.itemid, " ", (int)itemAmount.amount }), new object[0]);
								player.inventory.GiveItem(ItemManager.CreateByItemID(itemAmount.itemid, (int)itemAmount.amount),player.inventory.containerMain);
							}
							gameObject.GetComponent<BaseEntity>().KillMessage();
							player.ChatMessage(GetMessage("BGRADE_NORES",steamId)); 
							return;
						} else {
							var grd = (BuildingGrade.Enum) pgrade;
							buildingBlock.SetGrade(grd);
							buildingBlock.UpdateSkin();
							buildingBlock.SetHealthToMax();
							buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
							var items2 = buildingBlock.blockDefinition.grades[pgrade].costToBuild;
							List<Item> items3 = new List<Item>();
							foreach (ItemAmount itemAmount in items2)
							{
								amount = (int)Math.Ceiling(itemAmount.amount);
								player.inventory.Take(items3, itemAmount.itemid, amount);
								player.Command(string.Concat(new object[] { "note.inv ", itemAmount.itemid, " ", amount * -1f }), new object[0]);
							}
						}
					}
				} else {
					var grd = (BuildingGrade.Enum) pgrade;
					buildingBlock.SetGrade(grd);
					buildingBlock.UpdateSkin();
					buildingBlock.SetHealthToMax();
					buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
				}
			}
		}
		
        //[ChatCommand("bgrade")]
        void ChatBuildGrade(BasePlayer player, string command, string[] args) {
			var steamId = player.userID.ToString();
            if (!HasAnyPerm(player)) { 
				SendReply(player, GetMessage("BGRADE_NOPERM",steamId)); return; 
			}
			var chatmsg = new List<string>();
            if (args.Length>0) {
				switch (args[0])
				{
					case "1":
					case "2":
					case "3":
					case "4":
						if (!HasPerm(player,"all") && !HasPerm(player,args[0])) { 
							SendReply(player, GetMessage("BGRADE_NOPERM",steamId)); return; 
						}
						var pgrade = PlayerGrade(steamId);
						playerGrades[steamId] = Convert.ToInt32(args[0]);
						chatmsg.Add(string.Format(GetMessage("BGRADE_SET",steamId),GetMessage(((BuildingGrade.Enum) playerGrades[steamId]).ToString(),steamId)));
					break;
					case "0":
						playerGrades.Remove(steamId);
						chatmsg.Add(GetMessage("BGRADE_DIS",steamId));
					break;
					default:
						chatmsg.Add(GetMessage("BGRADE_INV",steamId));
					break;
				}
			} else {
				var pgrade = PlayerGrade(steamId,false);
				chatmsg.Add(GetMessage("BGRADE_HELP",steamId)+"\n");
				var all = HasPerm(player,"all");
				if (all || HasPerm(player,"1")) chatmsg.Add(string.Format(GetMessage("BGRADE_1",steamId),cmdname));
				if (all || HasPerm(player,"2")) chatmsg.Add(string.Format(GetMessage("BGRADE_2",steamId),cmdname));
				if (all || HasPerm(player,"3")) chatmsg.Add(string.Format(GetMessage("BGRADE_3",steamId),cmdname));
				if (all || HasPerm(player,"4")) chatmsg.Add(string.Format(GetMessage("BGRADE_4",steamId),cmdname));
				chatmsg.Add(string.Format(GetMessage("BGRADE_0",steamId),cmdname));
				var curtxt = ((BuildingGrade.Enum) pgrade).ToString();
				if (pgrade==0) curtxt = "Disabled";
				chatmsg.Add("\n"+string.Format(GetMessage("BGRADE_CUR",steamId), GetMessage(curtxt,steamId)));
			}
			player.ChatMessage(string.Join("\n", chatmsg.ToArray()));
        }
	
	}
	
}