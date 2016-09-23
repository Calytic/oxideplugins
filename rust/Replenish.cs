using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Replenish", "Skrallex", "1.2.2", ResourceId = 1956)]
    [Description("Easily replenish chests")]
    class Replenish : RustPlugin {
    	List<ReplenishableContainer> containers = new List<ReplenishableContainer>();
        List<ReplenishPlayer> playersUsing = new List<ReplenishPlayer>();
        Dictionary<string, string> allowableContainers = new Dictionary<string, string>();

        StoredData data;
        bool RequireAllSlotsEmpty;
        int DefaultTimerLength;
        bool UsePermissionsOnly;

        bool AllowLargeBox;
        bool AllowSmallBox;
        bool AllowFurnace;
        bool AllowLargeFurnace;
        bool AllowRefinery;
        bool AllowCampFire;
        bool AllowSmallStash;
        bool AllowLargeStocking;
        bool AllowSmallStocking;

        const string adminPerm = "replenish.admin";
        const string canEdit = "replenish.edit";
        const string canTest = "replenish.test";
        const string canList = "replenish.list";

        void Loaded() {
        	// Load previously saved replenishing containers.
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ReplenishData");
            if(data.containers != null)
                containers = data.containers;

            // Add permissions.
            permission.RegisterPermission(adminPerm, this);
            permission.RegisterPermission(canEdit, this);
            permission.RegisterPermission(canList, this);
            permission.RegisterPermission(canTest, this);

            // Load config and localisations.
            LoadDefaultMessages();
            LoadConfig();
            LoadAllowableContainers();
        }

        void Unload() {
            // Save replenishing containers.
            if(containers != null)
                data.containers = containers;
            Interface.Oxide.DataFileSystem.WriteObject("ReplenishData", data);
        }

        void OnServerSave() {
            // Save replenishing containers.
            if(containers != null)
                data.containers = containers;
            Interface.Oxide.DataFileSystem.WriteObject("ReplenishData", data);
        }

        protected override void LoadDefaultConfig() {
            Puts("Generating Default Config File");
            Config.Clear();
            Config["RequireAllSlotsEmpty"] = false;
            Config["DefaultTimerLength"] = 30;
            Config["UsePermissionsOnly"] = false;
            Config["AllowLargeBox"] = true;
            Config["AllowSmallBox"] = true;
            Config["AllowFurnace"] = true;
            Config["AllowLargeFurnace"] = true;
            Config["AllowRefinery"] = true;
            Config["AllowCampFire"] = true;
            Config["AllowSmallStash"] = true;
            Config["AllowLargeStocking"] = true;
            Config["AllowSmallStocking"] = true;

            SaveConfig();
        }

        void LoadConfig() {
            RequireAllSlotsEmpty = (bool)Config["RequireAllSlotsEmpty"] == null ? false : (bool)Config["RequireAllSlotsEmpty"];
            DefaultTimerLength = (int)Config["DefaultTimerLength"] == null ? 30 : (int)Config["DefaultTimerLength"];
            UsePermissionsOnly = (bool)Config["UsePermissionsOnly"] == null ? false : (bool)Config["UsePermissionsOnly"];
            AllowLargeBox = (bool)Config["AllowLargeBox"] == null ? false : (bool)Config["AllowLargeBox"];
            AllowSmallBox = (bool)Config["AllowSmallBox"] == null ? false : (bool)Config["AllowSmallBox"];
            AllowFurnace = (bool)Config["AllowFurnace"] == null ? false : (bool)Config["AllowFurnace"];
            AllowLargeFurnace = (bool)Config["AllowLargeFurnace"] == null ? false : (bool)Config["AllowLargeFurnace"];
            AllowRefinery = (bool)Config["AllowRefinery"] == null ? false : (bool)Config["AllowRefinery"];
            AllowCampFire = (bool)Config["AllowCampFire"] == null ? false : (bool)Config["AllowCampFire"];
            AllowSmallStash = (bool)Config["AllowSmallStash"] == null ? false : (bool)Config["AllowSmallStash"];
            AllowLargeStocking = (bool)Config["AllowLargeStocking"] == null ? false : (bool)Config["AllowLargeStocking"];
            AllowSmallStocking = (bool)Config["AllowSmallStocking"] == null ? false : (bool)Config["AllowSmallStocking"];
        }

        void LoadAllowableContainers() {
            if(AllowLargeBox)
                allowableContainers.Add("box.wooden.large", "Large Wood Box");
            if(AllowSmallBox)
                allowableContainers.Add("woodbox_deployed", "Wood Storage Box");
            if (AllowFurnace)
                allowableContainers.Add("furnace", "Furnace");
            if (AllowLargeFurnace)
                allowableContainers.Add("furnace.large", "Large Furnace");
            if (AllowRefinery)
                allowableContainers.Add("refinery_small_deployed", "Small Oil Refinery");
            if (AllowCampFire)
                allowableContainers.Add("campfire.prefab", "Camp Fire");
            if (AllowSmallStash)
                allowableContainers.Add("small_stash_deployed", "Small Stash");
            if (AllowLargeStocking)
                allowableContainers.Add("stocking_small_deployed", "Small Stocking");
            if (AllowLargeStocking)
                allowableContainers.Add("stocking_large_deployed", "Super Stocking");
        }

        void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                {"Prefix", "<color=orange>Replenish</color>"},
                {"NoPermission", "You do not have permission to use this command."},

                {"AddSyntax", "Invalid command syntax. Try <color=cyan>/replenish_add</color> <color=red>{Optional: TimeInSeconds}</color>"},


                {"AlreadyReplenishing", "That container is already set to be replenished. Removing it first."},
                {"NotReplenishing", "That container is not set to replenish."},
                {"BoxAdded", "{0} with uid {1} has been added as a replenishable container with timer {2}s."},
                {"BoxRemoved", "{0} with uid {1} will no longer be replenishing."},
                {"BoxTested", "This {0} with uid {1} is set to replenish every {2}s."},
                {"AddSingle", "Hit the container you want to replenish with a Hammer."},
                {"AddMulti", "Hit the containers you want to replenish with a Hammer. Type /replenish_stop to stop."},
                {"RemoveSingle", "Hit the container you want to remove from replenishing with a Hammer." },
                {"RemoveMulti", "Hit the containers you want to remove from replenishing with a Hammer. Type /replenish_stop to stop."},
                {"TestSingle", "Hit the container you want to test if it is replenishing with a Hammer."},
                {"TestMulti", "Hit the containers you want to test if they are replenishing with a Hammer. Type /replenish_stop to stop."},
                {"NotUsing", "You are not currently using any Replenish commands."},
                {"StoppedUsing", "You have stopped using Replenish commands."},

                {"ListHeading", "The following containers have been set to replenish:{0}"},
                {"ListEmpty", "No containers have been set to replenish. Use /replenish_add to add some." },
                {"ListEntry", "\n\t->{0} ({1}) at x:{2} y:{3} z:{4} ({5}m away)." },
                {"Stop", "You have stopped adding/removing/testing replenishable containers!"},
                {"InvalidTimer", "Invalid timer length. Using default value of {0}s instead"},
                {"HelpText", "Invalid Usage. Valid Commands: \n><color=red>replenish_add</color>: Add a new replenishing container." +
                    "\n><color=red>replenish_addm</color>: Add multiple new replenishing containers." +
                    "\n><color=red>replenish_remove</color>: Remove a replenishing container." +
                    "\n><color=red>replenish_removem</color>: Remove multiple replenishing containers." +
                    "\n><color=red>replenish_test</color>: Test if a container is replenishing." +
                    "\n><color=red>replenish_testm</color>: Test if multiple containers are replenishing." +
                    "\n><color=red>replenish_list</color>: Lists all replenishing containers and their locations." +
                    "\n><color=red>replenish_stop</color>: Stop adding/removing/testing multiple containers." }
            }, this);
        }

        void OnHammerHit(BasePlayer player, HitInfo info) {
        	if(!IsPlayerEditing(player)) {
        		return;
        	}

        	string containerType = "";
        	foreach(KeyValuePair<string, string> entry in allowableContainers) {
        		if(info.HitEntity.ToString().Contains(entry.Key)) {
        			containerType = entry.Value;
        			break;
        		}
        	}

        	if(containerType == "")
        		return;

        	StorageContainer container = info.HitEntity.GetComponent<StorageContainer>();
        	if(container == null)
        		return;
        	ReplenishPlayer rPlayer = GetReplenishPlayer(player);

        	if(rPlayer.adding) {
        		CreateReplenishableContainer(player, rPlayer, container, info, containerType);
        	}

        	if(rPlayer.removing) {
        		RemoveReplenishableContainer(player, container, containerType);
        	}

        	if(rPlayer.testing) {
        		TestReplenishableContainer(player, container, containerType);
        	}

        	if(!rPlayer.multi) {
        		playersUsing.Remove(rPlayer);
        	}
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item) {
        	if(GetReplenishableContainer(container) == null)
        		return;
        	ReplenishableContainer repl = GetReplenishableContainer(container);

        	if(RequireAllSlotsEmpty && container.itemList.Count > 0)
        		return;

        	timer.Once((float)repl.timer, () => {
        		repl.Replenish(container);
        		});
        }

        void CreateReplenishableContainer(BasePlayer player, ReplenishPlayer rPlayer, StorageContainer container, HitInfo info, string type) {
        	if(GetReplenishableContainer(container.inventory) != null) {
        		containers.Remove(GetReplenishableContainer(container.inventory));
        		ReplyPlayer(player, "AlreadyReplenishing");
        	}

        	ReplenishableContainer repl = new ReplenishableContainer(container.inventory.uid, rPlayer.timer);
        	repl.type = type;
        	repl.SaveItems(container.inventory);
        	var worldPos = info.HitEntity.GetEstimatedWorldPosition();
        	repl.pos = new Pos(worldPos.x, worldPos.y, worldPos.z);
        	containers.Add(repl);
        	ReplyFormatted(player, String.Format(Lang("BoxAdded"), repl.type, repl.uid, repl.timer));
        }

        void RemoveReplenishableContainer(BasePlayer player, StorageContainer container, string type) {
        	if(GetReplenishableContainer(container.inventory) == null) {
        		ReplyPlayer(player, "NotReplenishing");
        		return;
        	}
        	ReplenishableContainer repl = GetReplenishableContainer(container.inventory);
        	containers.Remove(repl);
        	ReplyFormatted(player, String.Format(Lang("BoxRemoved"), repl.type, repl.uid));
        }

        void TestReplenishableContainer(BasePlayer player, StorageContainer container, string type) {
        	if(GetReplenishableContainer(container.inventory) == null) {
        		ReplyPlayer(player, "NotReplenishing");
        		return;
        	}
        	ReplenishableContainer repl = GetReplenishableContainer(container.inventory);
        	ReplyFormatted(player, String.Format(Lang("BoxTested"), repl.type, repl.uid, repl.timer));
        }

        [ChatCommand("replenish")]
        void chatCmdReplenish(BasePlayer player, string cmd, string[] args) {
        	ReplyPlayer(player, "HelpText");
        	return;
        }

        [ChatCommand("replenish_add")]
        void chatCmdReplenishAdd(BasePlayer player, string cmd, string[] args) {
        	int timer = DefaultTimerLength;
        	if(!IsAllowed(player, canEdit)) {
        		ReplyPlayer(player, "NoPermission");
        		return;
        	}
        	if(args.Length > 1) {
        		ReplyPlayer(player, "AddSyntax");
        		return;
        	}
        	if(args.Length == 1) {
        		if(!Int32.TryParse(args[0], out timer)) {
        			ReplyPlayer(player, "InvalidTimer");
        			return;
        		}
        	}
        	Add(player, timer, false);
        }

        [ChatCommand("replenish_addm")]
        void chatCmdReplenishAddm(BasePlayer player, string cmd, string[] args) {
        	int timer = DefaultTimerLength;
        	if(!IsAllowed(player, canEdit)) {
        		ReplyPlayer(player, "NoPermission");
        		return;
        	}
        	if(args.Length > 1) {
        		ReplyPlayer(player, "AddSyntax");
        		return;
        	}
        	if(args.Length == 1) {
        		if(!Int32.TryParse(args[0], out timer)) {
        			ReplyPlayer(player, "InvalidTimer");
        			return;
        		}
        	}
        	Add(player, timer, true);
        }

        void Add(BasePlayer player, int timer, bool multi) {
        	ReplenishPlayer rPlayer;
        	if(IsPlayerEditing(player)) {
        		rPlayer = GetReplenishPlayer(player);
        		rPlayer.StopUsing();
        	}
        	else {
        		rPlayer = new ReplenishPlayer(player);
        		playersUsing.Add(rPlayer);
        	}

        	if(multi) {
        		ReplyPlayer(player, "AddMulti");
        		rPlayer.multi = true;
        	}
        	else {
        		ReplyPlayer(player, "AddSingle");
        	}
        	rPlayer.adding = true;
        	rPlayer.timer = timer;
        }

		[ChatCommand("replenish_remove")]
		void chatCmdReplenishRemove(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canEdit)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
            if(args.Length == 1) {
                RemoveByID(player, args[0]);
                return;
            }
			Remove(player, false);
		}

		[ChatCommand("replenish_removem")]
		void chatCmdReplenishRemovem(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canEdit)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			Remove(player, true);
		}

		void Remove(BasePlayer player, bool multi) {
			ReplenishPlayer rPlayer;
			if(IsPlayerEditing(player)) {
				rPlayer = GetReplenishPlayer(player);
				rPlayer.StopUsing();
			}
			else {
				rPlayer = new ReplenishPlayer(player);
				playersUsing.Add(rPlayer);
			}

			if(multi) {
				ReplyPlayer(player, "RemoveMulti");
				rPlayer.multi = true;
			}
			else {
				ReplyPlayer(player, "RemoveSingle");
			}
			rPlayer.removing = true;
		}

        void RemoveByID(BasePlayer player, string id) {
            foreach(ReplenishableContainer container in containers) {
                if(container.uid.ToString() == id) {
                    ReplyFormatted(player, String.Format(Lang("BoxRemoved"), container.type, container.uid));
                    containers.Remove(container);
                    return;
                }
            }
            ReplyPlayer(player, "NotReplenishing");
        }

		[ChatCommand("replenish_test")]
		void chatCmdReplenishTest(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canTest)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			Test(player, false);
		}

		[ChatCommand("replenish_testm")]
		void chatCmdReplenishTestm(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canTest)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			Test(player, true);
		}

		void Test(BasePlayer player, bool multi) {
			ReplenishPlayer rPlayer;
			if(IsPlayerEditing(player)) {
				rPlayer = GetReplenishPlayer(player);
				rPlayer.StopUsing();
			}
			else {
				rPlayer = new ReplenishPlayer(player);
				playersUsing.Add(rPlayer);
			}

			if(multi) {
				ReplyPlayer(player, "TestMulti");
				rPlayer.multi = true;
			}
			else {
				ReplyPlayer(player, "TestSingle");
			}
			rPlayer.testing = true;
		}

		[ChatCommand("replenish_list")]
		void chatCmdReplenishList(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canList)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			List(player);
		}

		void List(BasePlayer player) {
			if(containers.Count < 1) {
				ReplyPlayer(player, "ListEmpty");
				return;
			}
			string reply = "";
			foreach(ReplenishableContainer container in containers) {
				Pos playerPos = new Pos(player.transform.position);
				double distance = Math.Sqrt(((playerPos.x - container.pos.x) * (playerPos.x - container.pos.x) + (playerPos.y - container.pos.y) * (playerPos.y - container.pos.y) + (playerPos.z - container.pos.z) * (playerPos.z - container.pos.z)));
				reply += String.Format(Lang("ListEntry"), container.type, container.uid, Math.Round(container.pos.x, 0), Math.Round(container.pos.y, 0), Math.Round(container.pos.z, 0), Math.Round(distance, 1));
			}
			ReplyFormatted(player, reply);
		}

		[ChatCommand("replenish_stop")]
		void chatCmdReplenishStop(BasePlayer player, string cmd, string[] args) {
			if(!IsAllowed(player, canEdit)) {
				ReplyPlayer(player, "NoPermission");
				return;
			}
			Stop(player);
			ReplyPlayer(player, "StoppedUsing");
		}

		void Stop(BasePlayer player) {
			if(!IsPlayerEditing(player)) {
				ReplyPlayer(player, "NotUsing");
				return;
			}
			ReplenishPlayer rPlayer = GetReplenishPlayer(player);

			playersUsing.Remove(rPlayer);
		}

        bool IsPlayerEditing(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player == player) {
        			return true;
        		}
        	}
        	return false;
        }

        bool IsPlayerAdding(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player == player) {
        			if(rPlayer.adding) {
        				return true;
        			}
        		}
        	}
        	return false;
        }

        bool IsPlayerRemoving(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player == player) {
        			if(rPlayer.removing) {
        				return true;
        			}
        		}
        	}
        	return false;
        }

        bool IsPlayerTesting(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player == player) {
        			if(rPlayer.testing) {
        				return true;
        			}
        		}
        	}
        	return false;
        }

        bool IsPlayerMulti(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player == player) {
        			if(rPlayer.multi) {
        				return true;
        			}
        		}
        	}
        	return false;
        }

        void ReplyPlayer(BasePlayer player, string langKey) {
    		SendReply(player, Lang("Prefix") + ": " + Lang(langKey));
    	}

        void ReplyFormatted(BasePlayer player, string msg) {
    		SendReply(player, Lang("Prefix") + ": " + msg);
    	}

        ReplenishPlayer GetReplenishPlayer(BasePlayer player) {
        	foreach(ReplenishPlayer rPlayer in playersUsing) {
        		if(rPlayer.player = player) {
        			return rPlayer;
        		}
        	}
        	return null;
        }

        ReplenishableContainer GetReplenishableContainer(ItemContainer container) {
        	foreach(ReplenishableContainer repl in containers) {
        		if(repl.uid == container.uid) {
        			return repl;
        		}
        	}
        	return null;
        }

        string Lang(string key) {
    		return lang.GetMessage(key, this, null);
    	}

        bool IsAllowed(BasePlayer player, string perm) {
    		if(player.IsAdmin() && !UsePermissionsOnly) return true;
    		if(permission.UserHasPermission(player.UserIDString, adminPerm)) return true;
    		if(permission.UserHasPermission(player.UserIDString, perm)) return true;
    		return false;
    	}

        public class ReplenishPlayer {
        	public BasePlayer player;
        	public int timer = 30;
        	public bool adding = false;
        	public bool removing = false;
        	public bool testing = false;
        	public bool multi = false;

        	public ReplenishPlayer(BasePlayer player) {
        		this.player = player;
        	}

        	public ReplenishPlayer() {}

        	public void StopUsing() {
        		this.timer = 30;
        		this.adding = false;
        		this.removing = false;
        		this.testing = false;
        		this.multi = false;
        	}
        }

        public class ReplenishableContainer {
        	public uint uid;
        	public string type = "";
        	public int timer;
        	public Pos pos = new Pos(0.0f, 0.0f, 0.0f);
        	public List<ContainerItem> items = new List<ContainerItem>();

        	public ReplenishableContainer(uint uid, int timer) {
        		this.uid = uid;
        		this.timer = timer;
        	}

        	public ReplenishableContainer() {}

        	public void SaveItems(ItemContainer container) {
        		items.Clear();
        		items.AddRange(GetItems(container));
        	}

        	public void Replenish(ItemContainer container) {
        		container.itemList = new List<Item>();
        		foreach (ContainerItem contItem in items) {
                    Item item = ItemManager.CreateByItemID(contItem.itemId, contItem.amount, contItem.skin);
                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                        weapon.primaryMagazine.contents = contItem.ammo;
                    item.MoveToContainer(container);
                    if (contItem.contents == null)
                        continue;
                    foreach (ContainerItem contItem1 in contItem.contents) {
                        Item item1 = ItemManager.CreateByItemID(contItem1.itemId, contItem1.amount);
                        if (item1 == null)
                            continue;
                        item1.condition = contItem1.condition;
                        item1.MoveToContainer(item.contents);
                    }
                }
        	}

        	private IEnumerable<ContainerItem> GetItems(ItemContainer container) {
                return container.itemList.Select(item => new ContainerItem {
                    itemId = item.info.itemid,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    skin = item.skin,
                    condition = item.condition,
                    contents = item.contents?.itemList.Select(item1 => new ContainerItem {
                        itemId = item1.info.itemid,
                        amount = item1.amount,
                        condition = item1.condition
                    }).ToArray()
                });
            }
        }

        public class ContainerItem {
	    	public int itemId, skin, amount, ammo;
	    	public float condition;
	    	public ContainerItem[] contents;
        }

        public class StoredData {
        	public List<ReplenishableContainer> containers = new List<ReplenishableContainer>();
        }

        [System.Serializable]
        public class Pos {
        	public float x, y, z;

        	public Pos(float x, float y, float z) {
        		this.x = x;
        		this.y = y;
        		this.z = z;
        	}

        	public Pos(Vector3 vec3) {
        		this.x = vec3.x;
        		this.y = vec3.y;
        		this.z = vec3.z;
        	}

        	public Pos() {}
        }
    }

}
