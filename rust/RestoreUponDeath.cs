using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using System;

namespace Oxide.Plugins
{
    [Info("RestoreUponDeath", "k1lly0u", "0.1.32", ResourceId = 1859)]
    class RestoreUponDeath : RustPlugin
    {
        #region Fields
        RODData rodData;
        private DynamicConfigFile PlayerInvData;

        private Dictionary<ulong, List<SavedItem>> playerInv;
        #endregion

        #region Oxide Hooks 
        void Loaded() => PlayerInvData = Interface.Oxide.DataFileSystem.GetFile("restoreupondeath_data");
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();

            lang.RegisterMessages(new Dictionary<string, string>
                    {
                        {"addSyn", "/rod add <permission> <percentage> - Adds a new permission and percentage" },
                        {"remSyn",  "/rod remove <permission> - Remove a permission"},
                        {"addSuccess", "You have successfully added the permission {0} that has a loss percentage of {1}" },
                        {"invNum", "You must enter a valid percentage number" },
                        {"exists", "That permission already exists" },
                        {"remSuccess", "You have successfully remove the permission {0}" },
                        {"noExist", "The permission {0} does not exist" },
                        {"currentPerms", "Current permissions;" },
                        {"noPerms", "There are currently no permissions set up" },
                        {"listSyn", "/rod list - Lists all permissions and assigned loss percentage" }

                    }, this);

            foreach (var perm in rodData.Permissions)
                permission.RegisterPermission(perm.Key, this);

            playerInv = new Dictionary<ulong, List<SavedItem>>();
            foreach (var entry in rodData.Inventorys)
                playerInv.Add(entry.Key, entry.Value);
            rodData.Inventorys.Clear();

            timer.Once(900, () => SaveLoop());
        }    
        void OnPlayerRespawned(BasePlayer player) => RestoreInventory(player); 
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseCorpse)
            {
                var corpse = entity.GetComponent<LootableCorpse>();
                if (corpse != null)
                {
                    if (corpse.playerSteamID != 0)
                    {
                        if (GetPercentage(corpse.playerSteamID) == 100) return;
                        SaveInventory(corpse.playerSteamID);
                        var loot = corpse.GetComponent<LootableCorpse>().containers;
                        if (loot != null)
                        {
                            ProcessItems(corpse.GetComponent<LootableCorpse>(), 0);
                            ProcessItems(corpse.GetComponent<LootableCorpse>(), 1);
                            ProcessItems(corpse.GetComponent<LootableCorpse>(), 2);
                        }
                    }
                }
            }
        }        
        void Unload() => SaveData();
        
        #endregion

        #region ChatCommands
        [ChatCommand("rod")]
        private void cmdRod(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin())
            {
                if (args == null || args.Length == 0)
                {
                    SendMSG(player, "addSyn");
                    SendMSG(player, "remSyn");
                    SendMSG(player, "listSyn");
                    return;
                }
                if (args.Length >= 1)
                {
                    switch (args[0].ToLower())
                    {
                        case "add":
                            if (args.Length == 3)
                            {
                                string perm = args[1].ToLower();
                                if (!perm.StartsWith(Title.ToLower() + "."))
                                    perm = Title.ToLower() + "." + perm;
                                if (!permission.PermissionExists(perm) && !rodData.Permissions.ContainsKey(perm))                                
                                {
                                    int percentage = 0;
                                    if (int.TryParse(args[2], out percentage))
                                    {
                                        rodData.Permissions.Add(perm, percentage);
                                        permission.RegisterPermission(perm, this);
                                        SaveData();
                                        SendReply(player, string.Format("<color=#FF8C00>" + lang.GetMessage("addSuccess", this, player.UserIDString), perm, percentage) + "</color>");
                                        return;
                                    }
                                    SendMSG(player, "invNum");
                                    return;
                                }
                                SendMSG(player, "exists");
                                return;
                            }
                            SendMSG(player, "addSyn");
                            return;
                        case "remove":
                            if (args.Length >= 2)
                            if (rodData.Permissions.ContainsKey(args[1].ToLower()))
                            {
                                rodData.Permissions.Remove(args[1].ToLower());
                                SaveData();
                                SendReply(player, string.Format("<color=#FF8C00>" + lang.GetMessage("remSuccess", this, player.UserIDString), args[1].ToLower()) + "</color>");
                                return;
                            }
                            SendReply(player, string.Format("<color=#FF8C00>" + lang.GetMessage("noExist", this, player.UserIDString), args[1].ToLower()) + "</color>");
                            return;
                        case "list":
                            if (rodData.Permissions.Count > 0)
                            {
                                SendMSG(player, "currentPerms");
                                foreach (var entry in rodData.Permissions)
                                    SendMSG(player, $"{entry.Key} -- {entry.Value}%");
                                return;
                            }
                            SendMSG(player, "noPerms");
                            return;
                    }                   
                }
            }
        }
        private void SendMSG(BasePlayer player, string key) => SendReply(player, "<color=#FF8C00>" + lang.GetMessage(key, this, player.UserIDString) + "</color>");       
        #endregion

        #region Functions
        private int GetPercentage(ulong playerid)
        {
            int percentage = configData.PercentageOfItemsLost;
            foreach (var entry in rodData.Permissions)
            {
                if (permission.UserHasPermission(playerid.ToString(), entry.Key))
                {
                    percentage = entry.Value;
                    break;
                }
            }
            return percentage;
        }
        private void SaveInventory(ulong playerid)
        {
            List<SavedItem> Items = new List<SavedItem>();

            if (!playerInv.ContainsKey(playerid))
                playerInv.Add(playerid, Items);
            else playerInv[playerid] = Items;
        }
        private void ProcessItems(LootableCorpse corpse, int container)
        {
            string cont = "";
            switch (container)
            {
                case 1:
                    cont = "wear";
                    break;
                case 2:
                    cont = "belt";
                    break;
                default:
                    cont = "main";
                    break;
            }

            ulong ID = corpse.playerSteamID;
            var plyrInv = playerInv[ID];
            var items = corpse.containers[container].itemList;
            var percentage = GetPercentage(ID);
            double amount = (float)(items.Count * percentage) / 100;
            amount = items.Count - amount;
            if (percentage == 0) amount = items.Count;
            amount = Math.Round(Convert.ToDouble(amount), 0, MidpointRounding.AwayFromZero);            
            for (int i = 0; i < amount; i++)
            {
                var num = UnityEngine.Random.Range(0, items.Count);
                var item = items[num];
                var savedItem = ProcessItem(item, cont);
                plyrInv.Add(savedItem);
                items.Remove(item);
            };
        }                
        private SavedItem ProcessItem(Item item, string container)
        {
            SavedItem iItem = new SavedItem();
            iItem.shortname = item.info?.shortname;
            iItem.amount = item.amount;
            iItem.mods = new List<SavedItem>();
            iItem.container = container;
            iItem.skinid = item.skin;
            iItem.itemid = item.info.itemid;
            iItem.weapon = false;
            if (item.hasCondition)
                iItem.condition = item.condition;
            if (item.info.category.ToString() == "Weapon")
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (weapon.primaryMagazine != null)
                    {
                        iItem.ammoamount = weapon.primaryMagazine.contents;
                        iItem.ammotype = weapon.primaryMagazine.ammoType.shortname;
                        iItem.weapon = true;
                        if (item.contents != null)
                            foreach (var mod in item.contents.itemList)
                                if (mod.info.itemid != 0)
                                    iItem.mods.Add(ProcessItem(mod, "none"));
                    }
                }
            }
            return iItem;
        }
        private void RestoreInventory(BasePlayer player)
        {
            List<SavedItem> items = new List<SavedItem>();
            if (playerInv.ContainsKey(player.userID))
            {
                items = playerInv[player.userID];
                GivePlayerInventory(player, items);
                playerInv.Remove(player.userID);
            }
        }
        #endregion

        #region Give
        private void GivePlayerInventory(BasePlayer player, List<SavedItem> items)
        {
            foreach (SavedItem item in items)
            {
                if (item.weapon)
                    GiveItem(player, BuildWeapon(item), item.container);
                else GiveItem(player, BuildItem(item), item.container);
            }
            playerInv.Remove(player.userID);
        }        
        private void GiveItem(BasePlayer player, Item item, string container)
        {
            if (item == null) return;
            ItemContainer cont;
            switch (container)
            {
                case "wear":
                    cont = player.inventory.containerWear;
                    break;
                case "belt":
                    cont = player.inventory.containerBelt;
                    break;
                default:
                    cont = player.inventory.containerMain;
                    break;
            }
            player.inventory.GiveItem(item, cont);
        }
        private Item BuildItem(SavedItem sItem)
        {
            if (sItem.amount < 1) sItem.amount = 1;
            Item item = ItemManager.CreateByItemID(sItem.itemid, sItem.amount, sItem.skinid);
            if (item.hasCondition)
                item.condition = sItem.condition;
            return item;
        }
        private Item BuildWeapon(SavedItem sItem)
        {
            Item item = ItemManager.CreateByItemID(sItem.itemid, 1, sItem.skinid);
            if (item.hasCondition)
                item.condition = sItem.condition;
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.ammoamount;
            }
            if (sItem.mods != null)
                foreach (var mod in sItem.mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }
        #endregion

        #region Classes
        class RODData
        {
            public Dictionary<ulong, List<SavedItem>> Inventorys = new Dictionary<ulong, List<SavedItem>>();
            public Dictionary<string, int> Permissions = new Dictionary<string, int>(); 
        }      
        class SavedItem
        {
            public string shortname;
            public int itemid;
            public string container;
            public float condition;
            public int amount;
            public int ammoamount;
            public string ammotype;
            public int skinid;
            public bool weapon;
            public List<SavedItem> mods;
        }
        #endregion

        #region Data Management
        void SaveData()
        {
            rodData.Inventorys = playerInv;
            PlayerInvData.WriteObject(rodData);
        }
        private void SaveLoop()
        {
            SaveData();
            timer.Once(900, () => SaveLoop());
        }
        void LoadData()
        {
            try
            {
                rodData = PlayerInvData.ReadObject<RODData>();
            }
            catch
            {
                Puts("Couldn't load data, creating new datafile");
                rodData = new RODData();
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {           
            public int PercentageOfItemsLost { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                PercentageOfItemsLost = 25
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
