using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using System.Linq;
using System;

namespace Oxide.Plugins
{
	[Info("Backpacks", "LaserHydra", "1.2.0", ResourceId = 1408)]
	[Description("Allows players to have Backpacks which provides them extra inventory space.")]
	class Backpacks : RustPlugin
	{
        ////////////////////////////////////////
        ///     UI Builder
        ////////////////////////////////////////

        class UIColor
        {
            double red;
            double green;
            double blue;
            double alpha;

            public UIColor(double red, double green, double blue, double alpha)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                this.alpha = alpha;
            }

            public string GetString()
            {
                return $"{red.ToString()} {green.ToString()} {blue.ToString()} {alpha.ToString()}";
            }
        }

        class UIObject
        {
            List<object> ui = new List<object>();
            List<string> objectList = new List<string>();

            public UIObject()
            {
            }

            string RandomString()
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                List<char> charList = chars.ToList();

                string random = "";

                for (int i = 0; i <= UnityEngine.Random.Range(5, 10); i++)
                    random = random + charList[UnityEngine.Random.Range(0, charList.Count - 1)];

                return random;
            }

            public void Draw(BasePlayer player)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(JsonConvert.SerializeObject(ui).Replace("{NEWLINE}", Environment.NewLine)));
            }

            public void Destroy(BasePlayer player)
            {
                foreach(string uiName in objectList)
                    CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList(uiName));
            }

            public string AddPanel(string name, double left, double top, double width, double height, UIColor color, bool mouse = false, string parent = "HUD/Overlay")
            {
                name = name + RandomString();

                string type = "";
                if (mouse) type = "NeedsCursor";

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Image"},
                                {"color", color.GetString()}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            },
                            new Dictionary<string, string> {
                                {"type", type}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddText(string name, double left, double top, double width, double height, UIColor color, string text, int textsize = 15, string parent = "HUD/Overlay", int alignmode = 0)
            {
                name = name + RandomString(); text = text.Replace("\n", "{NEWLINE}"); string align = "";
                switch (alignmode)
                {
                    case 0: { align = "LowerCenter"; break; };
                    case 1: { align = "LowerLeft"; break; };
                    case 2: { align = "LowerRight"; break; };
                    case 3: { align = "MiddleCenter"; break; };
                    case 4: { align = "MiddleLeft"; break; };
                    case 5: { align = "MiddleRight"; break; };
                    case 6: { align = "UpperCenter"; break; };
                    case 7: { align = "UpperLeft"; break; };
                    case 8: { align = "UpperRight"; break; };
                }
                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Text"},
                                {"text", text},
                                {"fontSize", textsize.ToString()},
                                {"color", color.GetString()},
                                {"align", align}
                            },
                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }

            public string AddButton(string name, double left, double top, double width, double height, UIColor color, string command = "", string parent = "HUD/Overlay", string closeUi = "")
            {
                name = name + RandomString();

                ui.Add(new Dictionary<string, object> {
                    {"name", name},
                    {"parent", parent},
                    {"components",
                        new List<object> {
                            new Dictionary<string, string> {
                                {"type", "UnityEngine.UI.Button"},
                                {"close", closeUi},
                                {"command", command},
                                {"color", color.GetString()},
                                {"imagetype", "Tiled"}
                            },

                            new Dictionary<string, string> {
                                {"type", "RectTransform"},
                                {"anchormin", $"{left.ToString()} {((1 - top) - height).ToString()}"},
                                {"anchormax", $"{(left + width).ToString()} {(1 - top).ToString()}"}
                            }
                        }
                    }
                });

                objectList.Add(name);
                return name;
            }
        }

        UIObject ui = new UIObject();
        public Dictionary<string, BaseEntity> activeBackpacks = new Dictionary<string, BaseEntity>();
		string backpackEntity = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";
		
		////////////////////////////////////////
		///	 Data File Handling
		////////////////////////////////////////

		Dictionary<string, Backpack> backpacks;

		void LoadData()
		{
			backpacks = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, Backpack>>("Backpack_Data");
		}

		void SaveData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("Backpack_Data", backpacks);
		}

		////////////////////////////////////////
		///	 Classes
		////////////////////////////////////////

		class BackpackItem
		{
			public int amount;
			public int skinid;
			public int slot;
			public int itemid;
			public bool blueprint;
		}

		class Backpack
		{
			public List<BackpackItem> items = new List<BackpackItem>();
		}

		////////////////////////////////////////
		///	 On Plugin Loaded
		////////////////////////////////////////

		void Loaded()
		{
			if(!permission.PermissionExists("backpack.use")) permission.RegisterPermission("backpack.use", this);
			
			LoadData();
			LoadConfig();
			
			if((int)Config["Settings", "Backpack Size (1-3)"] == 1)
				backpackEntity = "assets/prefabs/deployable/small stash/small_stash_deployed.prefab";
			else if((int)Config["Settings", "Backpack Size (1-3)"] == 3)
				backpackEntity = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";

           if((bool) Config["Settings", "Use UI"]) ui.AddText("backpackButtonText", 0, 0, 1, 1, new UIColor(1, 1, 1, 1), "<b>Backpack</b>", 20, ui.AddButton("backpackButton", 0.01, 0.95, 0.10, 0.03, new UIColor(0.45, 0.6, 0.2, 0.9), "backpack.open"), 3);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                ui.Draw(player);
        }
		
		////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////
		
		void LoadConfig()
		{
			SetConfig("Settings", "Backpack Size (1-3)", 2);
			SetConfig("Settings", "Drop on Death", true);
            SetConfig("Settings", "Use UI", true);

            SaveConfig();
		}
		
		protected override void LoadDefaultConfig()
		{
			Puts("Generating new configfile...");
			LoadConfig();
		}
		
		////////////////////////////////////////
		///	 Death Handling
		////////////////////////////////////////
		
		void OnEntityDeath(BaseCombatEntity victim, HitInfo hitInfo)
		{
			if(!(bool)Config["Settings", "Drop on Death"]) return;
			if(victim != null && victim.ToPlayer() != null)
			{
				BasePlayer player = victim.ToPlayer();
				if(backpacks.ContainsKey(player.UserIDString) && backpacks[player.UserIDString].items.Count >= 1)
				{
					BaseEntity backpack = GameManager.server.CreateEntity(backpackEntity, new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));

					backpack.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
					backpack.Spawn(true);

					StorageContainer loot = backpack.GetComponent<StorageContainer>();

					foreach (BackpackItem backpackItem in backpacks[player.userID.ToString()].items)
					{
						Item item = ItemManager.CreateByItemID(backpackItem.itemid, backpackItem.amount, backpackItem.blueprint, backpackItem.skinid);
						item.MoveToContainer(loot.inventory, backpackItem.slot);
					}
					
					backpacks[player.UserIDString].items.Clear();
					SaveData();
					
					SendChatMessage(player, "Backpack", "Your Backpack was dropped at the position of your death.");
				}
					
			}
            else if(victim != null && activeBackpacks.ContainsValue(victim))
            {
                foreach(string player in activeBackpacks.Keys)
                {
                    if (activeBackpacks[player] == victim)
                        backpacks[player] = new Backpack();
                }
            }
		}
		
		////////////////////////////////////////
		///	 Commands
		////////////////////////////////////////

        [ConsoleCommand("backpack.open")]
        void ccmdBackpack(ConsoleSystem.Arg arg)
        {
            if(arg != null && arg.connection != null && arg.connection.player != null)
            {
                BasePlayer player = (BasePlayer)arg.connection.player;
                cmdBackpack(player);
                ui.Destroy(player);
            }
        }

		[ChatCommand("backpack")]
		void cmdBackpack(BasePlayer player)
		{
			if(!permission.UserHasPermission(player.UserIDString, "backpack.use"))
			{
				SendChatMessage(player, "Backpack", "You don't' have permission to use this command!");
				return;
			}
			
			OpenBackpack(player);
			SendChatMessage(player, "Backpack", "Backpack opened.");
		}

		////////////////////////////////////////
		///	 Backpack Handling
		////////////////////////////////////////
		
		void OpenBackpack(BasePlayer player)
		{
			if (!backpacks.ContainsKey(player.UserIDString))
				backpacks.Add(player.UserIDString, new Backpack());
			
			BaseEntity backpack = GameManager.server.CreateEntity(backpackEntity, new Vector3(player.transform.position.x, player.transform.position.y - 1, player.transform.position.z));

			backpack.SendMessage("SetDeployedBy", player, UnityEngine.SendMessageOptions.DontRequireReceiver);
			backpack.Spawn(true);

			StorageContainer loot = backpack.GetComponent<StorageContainer>();

			foreach (BackpackItem backpackItem in backpacks[player.userID.ToString()].items)
			{
				Item item = ItemManager.CreateByItemID(backpackItem.itemid, backpackItem.amount, backpackItem.blueprint, backpackItem.skinid);
				item.MoveToContainer(loot.inventory, backpackItem.slot);
			}
			
			backpack.gameObject.AddComponent<LootingHandler>();
			loot.PlayerOpenLoot(player);

			activeBackpacks[player.UserIDString] = backpack;
		}

		void CloseBackpack(BasePlayer player)
		{
			string uid = player.UserIDString;

			if (activeBackpacks.ContainsKey(uid))
			{
				BaseEntity box = activeBackpacks[uid] as BaseEntity;
				StorageContainer loot = box.GetComponent<StorageContainer>();
				Backpack backpack = new Backpack();

				foreach (Item item in loot.inventory.itemList)
				{
					BackpackItem backpackItem = new BackpackItem();
					backpackItem.amount = item.amount;
					backpackItem.blueprint = item.IsBlueprint();
					backpackItem.itemid = item.info.itemid;
					backpackItem.skinid = item.skin;
					backpackItem.slot = item.position;

					backpack.items.Add(backpackItem);
				}

				backpacks[uid] = backpack;
				SaveData();
				
				loot.inventory.itemList.Clear();
				box.Kill();
				activeBackpacks.Remove(uid);
				
				SendChatMessage(player, "Backpack", "Backpack closed.");

                ui.Draw(player);
            }
		}

        ////////////////////////////////////////
        ///     UI Handling
        ////////////////////////////////////////

        void OnPlayerInit(BasePlayer player)
        {
            ui.Draw(player);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            ui.Destroy(player);
        }

        void Unloaded()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                ui.Destroy(player);
        }

        ////////////////////////////////////////
        ///     Config Setup
        ////////////////////////////////////////

        void SetConfig(string Arg1, object Arg2, object Arg3 = null, object Arg4 = null)
        {
            if (Arg4 == null)
            {
                Config[Arg1, Arg2.ToString()] = Config[Arg1, Arg2.ToString()] ?? Arg3;
            }
            else if (Arg3 == null)
            {
                Config[Arg1] = Config[Arg1] ?? Arg2;
            }
            else
            {
                Config[Arg1, Arg2.ToString(), Arg3.ToString()] = Config[Arg1, Arg2.ToString(), Arg3.ToString()] ?? Arg4;
            }
        }
		
		////////////////////////////////////////
		///	 Chat Handling
		////////////////////////////////////////

		void BroadcastChat(string prefix, string msg = null) => PrintToChat(msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

		void SendChatMessage(BasePlayer player, string prefix, string msg = null) => SendReply(player, msg == null ? prefix : "<color=#00FF8D>" + prefix + "</color>: " + msg);

		////////////////////////////////////////
		///	 Looting Handler
		////////////////////////////////////////

		class LootingHandler : MonoBehaviour
		{
			private void PlayerStoppedLooting(BasePlayer player)
			{
				Interface.Oxide.RootPluginManager.GetPlugin("Backpacks").Call("CloseBackpack", player);
			}
		}
	}
}
