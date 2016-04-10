// Reference: UnityEngine.UI
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("LootChests", "Noviets", "1.0.7")]
    [Description("Spawns Storage Chests with loot around the map")]

    class LootChests : HurtworldPlugin
    {
		Dictionary<int, int> ItemList = new Dictionary<int, int>();
		Dictionary<uLink.NetworkView, Vector3> ChestList = new Dictionary<uLink.NetworkView, Vector3>();
		void Loaded()
		{
			permission.RegisterPermission("LootChests.admin", this);
			ItemList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<int, int>>("LootChests/ItemList");
			LoadDefaultConfig();
			LoadDefaultMessages();
		}
		void SaveItemList()
		{
			Interface.GetMod().DataFileSystem.WriteObject("LootChests/ItemList", ItemList);
			Interface.GetMod().DataFileSystem.WriteObject("LootChests/ChestList", ChestList);
		}
		void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                {"NoPermission","<color=#ffa500>LootChests</color>: You dont have Permission to do this! (LootChests.admin)"},
                {"ItemAdded","<color=#ffa500>LootChests</color>: Success Adding: <color=green>{Item}</color> Amount: <color=green>{Amount}</color>"},
				{"ItemNotFound","<color=#ffa500>LootChests</color>: That item isn't in the Item List."},
				{"SpawnFail","Spawn stopped. Too many invalid spawn locations. Please check your spawn configs"},
				{"ItemRemoved","<color=#ffa500>LootChests</color>: {Item} has been successfully removed from the list."},
				{"ItemExists","<color=#ffa500>LootChests</color>: That item is already in the Item List."},
				{"Error","<color=#ffa500>LootChests</color>: Incorrect use of the command."},
				{"InvalidItem","<color=#ffa500>LootChests</color>: That item does not exist, please check the ItemID"},
				{"NoItems","There's no items in the item list. Use the chat command: /lootchest add (ItemID) (Amount). Then: /reload LootChests"},
				{"Save","<color=#ffa500>LootChests</color>: Item List has been saved"},
				{"Spawned","<color=#ffa500>LootChests</color>: Loot Chests have Spawned"},
				{"Despawned","<color=#ffa500>LootChests</color>: Loot Chests have Despawned"}
            };
			
			lang.RegisterMessages(messages, this);
        }
		string Msg(string msg, string SteamId = null) => lang.GetMessage(msg, this, SteamId);
		protected override void LoadDefaultConfig()
        {
			var Locs = new List<object>() { "-3000, 450, -3000"};
			if(Config["StartPoints"] == null) Config.Set("StartPoints", Locs);
			if(Config["MininumRange"] == null) Config.Set("MininumRange", 0f);
			if(Config["MaximumRange"] == null) Config.Set("MaximumRange", 3000f);
			if(Config["ChestSpawnCount"] == null) Config.Set("ChestSpawnCount", 20);
			if(Config["SecondsForSpawn"] == null) Config.Set("SecondsForSpawn", 7200);
			if(Config["SecondsTillDestroy"] == null) Config.Set("SecondsTillDestroy", 1800);
			if(Config["ItemsPerChest"] == null) Config.Set("ItemsPerChest", 1);
			if(Config["ShowSpawnMessage"] == null) Config.Set("ShowSpawnMessage", true);
			if(Config["DestroyOnEmpty"] == null) Config.Set("DestroyOnEmpty", true);
			if(Config["ShowDespawnMessage"] == null) Config.Set("ShowDespawnMessage", true);
            SaveConfig();
        }
		[ChatCommand("lootchests")]
        void LootChestCommand(PlayerSession session, string command, string[] args)
        {
			if(permission.UserHasPermission(session.SteamId.ToString(),"LootChests.admin") || session.IsAdmin)
			{
				if(args.Length == 0 || args.Length > 3)
				{
					hurt.SendChatMessage(session, Msg("Error",session.SteamId.ToString()));
					return;
				}
				if(args.Length == 2)
				{
					if(args[0].ToLower() == "remove")
					{
						int ID = 0;
						try{
							ID = Convert.ToInt32(args[1]);
						}catch{
							hurt.SendChatMessage(session, Msg("Error",session.SteamId.ToString()));
							return;
						}
						if(ID != 0 && ItemList.ContainsKey(ID))
						{
							ItemList.Remove(ID);
							SaveItemList();
							var ItemName = GlobalItemManager.Instance.GetItem(ID);
							var CleanName = ItemName.GetNameKey().Replace("Items/","").Replace("AmmoType/","").Replace("Machines/","");
							hurt.SendChatMessage(session, Msg("ItemRemoved",session.SteamId.ToString()).Replace("{Item}",CleanName));
							return;
						}
						else
						{
							hurt.SendChatMessage(session, Msg("ItemNotFound",session.SteamId.ToString()));
							return;
						}
					}
					if(args[0].ToLower() == "save")
					{
						SaveItemList();
						hurt.SendChatMessage(session, Msg("Save",session.SteamId.ToString()));
						return;
					}
				}
				if(args.Length == 3)
				{
					int ID = 0;
					int Amount = 0;
					if(args[0].ToLower() == "add")
					{
						try{
							ID = Convert.ToInt32(args[1]);
						}catch{
							hurt.SendChatMessage(session, Msg("Error",session.SteamId.ToString()));
							return;
						}try{
							Amount = Convert.ToInt32(args[2]);
						}catch{
							hurt.SendChatMessage(session, Msg("Error",session.SteamId.ToString()));
							return;
						}
						if(ID != 0 && Amount != 0)
						{
							if(!ItemList.ContainsKey(ID))
							{
								var ItemName = GlobalItemManager.Instance.GetItem(ID);
								if(ItemName != null)
								{
									var CleanName = ItemName.GetNameKey().Replace("Items/","").Replace("AmmoType/","").Replace("Machines/","");
									ItemList.Add(ID, Amount);
									SaveItemList();
									hurt.SendChatMessage(session, Msg("ItemAdded",session.SteamId.ToString()).Replace("{Item}",CleanName).Replace("{Amount}",args[2]));
									return;
								}
								else
									hurt.SendChatMessage(session, Msg("InvalidItem",session.SteamId.ToString()));
							}
							else
							{
								hurt.SendChatMessage(session, Msg("ItemExists",session.SteamId.ToString()));
								return;
							}
						}
					}
				}
			}
			else
				hurt.SendChatMessage(session, Msg("NoPermission",session.SteamId.ToString()));
		}
		void OnServerInitialized(){timer.Once(1f, () => {ChestSpawns();});}
        void ChestSpawns()
        {
			if(ItemList.Count > 0)
			{
				string chest = "StorageChestDynamicConstructed";
				if((bool)Config["DestroyOnEmpty"])
					chest = "LootCache";
				timer.Repeat(Convert.ToSingle(Config["SecondsForSpawn"]), 0, () =>
				{
					if((bool)Config["ShowSpawnMessage"])
						hurt.BroadcastChat(Msg("Spawned"));
					var LocList = Config.Get<List<string>>("StartPoints");
					float min = Convert.ToSingle(Config["MinimumRange"]);
					float max = Convert.ToSingle(Config["MaximumRange"]);
					int count = Convert.ToInt32(Config["ChestSpawnCount"]);
					foreach(string Loc in LocList)
					{
						int i=0;
						int fail = 0;
						string[] XYZ = Loc.ToString().Split(',');
						Vector3 position = new Vector3(Convert.ToSingle(XYZ[0]),Convert.ToSingle(XYZ[1]),Convert.ToSingle(XYZ[2]));
						while(i < count)
						{
							if(fail > count*3){ Puts(Msg("SpawnFail"));return;}
							Vector3 randposition = new Vector3((position.x + UnityEngine.Random.Range(min, max)), position.y+10, (position.z + UnityEngine.Random.Range(min, max)));
							RaycastHit hitInfo;
							if (Physics.Raycast(randposition, Vector3.down, out hitInfo))
							{
								Quaternion rotation = Quaternion.Euler(0.0f, (float)UnityEngine.Random.Range(0f, 360f), 0.0f);
								rotation = Quaternion.FromToRotation(Vector3.down, hitInfo.normal) * rotation;			
								if(!hitInfo.collider.gameObject.name.Contains("UV") && !hitInfo.collider.gameObject.name.Contains("Cliff") && !hitInfo.collider.gameObject.name.Contains("Zone") && !hitInfo.collider.gameObject.name.Contains("Cube") && !hitInfo.collider.gameObject.name.Contains("Build"))
								{
									GameObject Obj = Singleton<NetworkManager>.Instance.NetInstantiate(chest, hitInfo.point, Quaternion.identity, GameManager.GetSceneTime());
									Inventory inv = Obj.GetComponent<Inventory>() as Inventory;
									uLink.NetworkView nwv = uLink.NetworkView.Get(Obj);
									if((bool)Config["DestroyOnEmpty"])
										inv.DestroyOnEmpty = true;
									GiveItems(inv);
									Destroy(nwv);
									i++;
								}
								else
									fail++;
							}
							else
								fail++;
						}
					}
					if((bool)Config["ShowDespawnMessage"])
					{
						timer.Once(Convert.ToSingle(Config["SecondsTillDestroy"]), () =>
						{
							hurt.BroadcastChat(Msg("Despawned"));
						});
					}
				});
			}
			else
				Puts(Msg("NoItems"));
		}
		void GiveItems(Inventory inv)
		{
			int num = 0;
			int count = Convert.ToInt32(Config["ItemsPerChest"]);
			while(num < count)
			{
				int rand = UnityEngine.Random.Range(0, ItemList.Count);
				var item = Singleton<GlobalItemManager>.Instance.GetItem((int)ItemList.ElementAt(rand).Key);
				Singleton<GlobalItemManager>.Instance.GiveItem(item, ItemList.ElementAt(rand).Value, inv);
				num++;
			}
		}
		void Destroy(uLink.NetworkView nwv)
		{
			timer.Once(Convert.ToSingle(Config["SecondsTillDestroy"]), () =>
			{
				Singleton<NetworkManager>.Instance.NetDestroy(nwv);
			});
		}
	}
}