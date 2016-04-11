/*
References:
 * System
 * Assembly-CSharp
 * Oxide.Core
 * Oxide.Ext.CSharp
 * OXide.Game.Rust
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("BluePrinter", "mk_sky", "1.0.5", ResourceId = 1343)]
    [Description("Allows producing blueprints your char knows, needs paper or blueprintparts.")]
    class BluePrinter : RustPlugin
    {
        #region vars
        ListDictionary<Rust.Rarity, int> paperNeeded;

        ListDictionary<Rust.Rarity, int> blueprintPartsNeeded;

        ListDictionary<Rust.Rarity, string> blueprintPartsTypeNeeded;

        ListDictionary<Rust.Rarity, int> drawTimes;

        Dictionary<string, int> blueprintPartsID = new Dictionary<string, int>() {
            { "blueprint_fragment", 1351589500 },
            { "blueprint_page", 1625167035 },
            { "blueprint_book", 1624763669 },
            { "blueprint_library", -845335793 }
        };

        ListDictionary<string, string> localization;

        ListDictionary<string, string> itemAlias;

        bool cancelBPWhenDead = true;

        bool paperUsageAllowed = true;

        bool blueprintPartsUsageAllowed = false;

        bool popupsEnabled = false;

        [PluginReference]
        Plugin PopupNotifications;
        #endregion

        void OnServerInitialized()
        {
            ConfigLoader();

            if (!permission.PermissionExists("canuseblueprinter"))
                permission.RegisterPermission("canuseblueprinter", this);
            
            foreach (BasePlayer player in BasePlayer.sleepingPlayerList)
                if (Config["InDrawing"] != null &&
                    Config["InDrawing", player.userID.ToString()] != null &&
                    Config["InDrawing", player.userID.ToString()].ToString() != String.Empty)
                    TimedBluePrint(player.userID.ToString());
                else
                    Config["InDrawing", player.userID.ToString()] = "";

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (Config["InDrawing"] != null &&
                    Config["InDrawing", player.userID.ToString()] != null &&
                    Config["InDrawing", player.userID.ToString()].ToString() != String.Empty)
                    TimedBluePrint(player.userID.ToString());
                else
                    Config["InDrawing", player.userID.ToString()] = "";

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();

            Config["Version"] = this.Version.ToString();

            #region paper
            // just took required blueprint.fragment * 1.5 / 10 for standard, because we are all bad blueprint-drawers and it will already take a lot of wood with this
            // (1)paper = 100 wood (if not changed by other plugin) ; Common = 300 wood ; Uncommon = 4.5k wood ; Rare = 9k wood ; VeryRare = 18k wood

            Config["Paper", "Common"] = 3;

            Config["Paper", "Uncommon"] = 45;

            Config["Paper", "Rare"] = 90;

            Config["Paper", "VeryRare"] = 180;

            Config["Paper", "None"] = 1;
            #endregion
            #region blueprintparts
            Config["BlueprintParts", "Common"] = 20;

            Config["BlueprintParts", "CommonType"] = "blueprint_fragment";

            Config["BlueprintParts", "Uncommon"] = 1;

            Config["BlueprintParts", "UncommonType"] = "blueprint_page";

            Config["BlueprintParts", "Rare"] = 1;

            Config["BlueprintParts", "RareType"] = "blueprint_book";

            Config["BlueprintParts", "VeryRare"] = 1;

            Config["BlueprintParts", "VeryRareType"] = "blueprint_library";

            Config["BlueprintParts", "None"] = 1;

            Config["BlueprintParts", "NoneType"] = "blueprint_fragment";
            #endregion
            #region drawtime
            Config["DrawTime", "Common"] = 10;

            Config["DrawTime", "Uncommon"] = 30;

            Config["DrawTime", "Rare"] = 60;

            Config["DrawTime", "VeryRare"] = 120;

            Config["DrawTime", "None"] = 0;
            #endregion
            #region settings
            Config["Settings", "CancelBPWhenDead"] = true;

            Config["Settings", "PaperUsageAllowed"] = true;

            Config["Settings", "BlueprintPartsUsageAllowed"] = false;

            Config["Settings", "EnablePopups"] = false;
            #endregion
            #region localization
            Config["Localization", "NotEnoughPaper"] = "The required ammount of paper to create this blueprint is {0} and you only have {1}.";

            Config["Localization", "NotEnoughBluePrintParts"] = "The required ammount of blueprintpart-type \"{0}\" to create this blueprint is {1} and you only have {2}.";

            Config["Localization", "BPNotLearned"] = "You don't know the blueprint for this item, learn it yourself first.";

            Config["Localization", "BPIsDrawing"] = "Blueprint is drawing now, please wait {0} seconds.";

            Config["Localization", "BPDelivery"] = "You finished drawing a blueprint for {0}.";

            Config["Localization", "BPRemovedFromQueue"] = "You are dead and can't finish drawing the blueprint for {0}.";

            Config["Localization", "AlreadyDrawing"] = "You are already drawing a blueprint, please wait until this is finished.";

            Config["Localization", "ItemNotFound"] = "An item with the name \"{0}\" was not found.";

            Config["Localization", "NoBP"] = "No blueprint for this item possible.";

            Config["Localization", "Help"] = "Use /blueprinter [ITEM] to create a blueprint from your known blueprints.";

            Config["Localization", "NoPermission"] = "You have no permission to use this command.";
            #endregion
            #region itemalias
            foreach (ItemDefinition itemDef in ItemManager.itemList)
                if (ItemManager.FindBlueprint(itemDef) != null &&
                    !ItemManager.FindBlueprint(itemDef).defaultBlueprint)
                    Config["ZItemAlias", itemDef.shortname] = itemDef.displayName.english; // only putting the Z here so it won't show before the other things in the config

            Config["ZItemAlias", "blueprint_fragment"] = "Blueprint Fragment";

            Config["ZItemAlias", "blueprint_page"] = "Blueprint Page";

            Config["ZItemAlias", "blueprint_book"] = "Blueprint Book";

            Config["ZItemAlias", "blueprint_library"] = "Blueprint Library";
            #endregion
            #region permission
            if (!permission.PermissionExists("canuseblueprinter"))
                permission.RegisterPermission("canuseblueprinter", this);

            if (!permission.GroupHasPermission("player", "canuseblueprinter"))
                permission.GrantGroupPermission("player", "canuseblueprinter", this);

            if (!permission.GroupHasPermission("moderator", "canuseblueprinter"))
                permission.GrantGroupPermission("moderator", "canuseblueprinter", this);

            if (!permission.GroupHasPermission("admin", "canuseblueprinter"))
                permission.GrantGroupPermission("admin", "canuseblueprinter", this);
            #endregion

            SaveConfig();

            PrintWarning("Blueprinter created new config.");
        }

        void ConfigLoader()
        {
            base.LoadConfig();

            #region updater
            if (Config.Exists() &&
                Config["Version"] == null)
            {
                Config.Save(Config.Filename + ".pre103.bak"); //will always call this pre103 as the change for this came with 1.0.3

                LoadDefaultConfig();
            }
            else if (Config.Exists() &&
                     Config["Version"].ToString() != this.Version.ToString())
                ConfigUpdater();
            #endregion
            #region paper
            paperNeeded = new ListDictionary<Rust.Rarity, int>();

            paperNeeded.Add(Rust.Rarity.Common, Convert.ToInt32(Config["Paper", "Common"]));

            paperNeeded.Add(Rust.Rarity.Uncommon, Convert.ToInt32(Config["Paper", "Uncommon"]));

            paperNeeded.Add(Rust.Rarity.Rare, Convert.ToInt32(Config["Paper", "Rare"]));

            paperNeeded.Add(Rust.Rarity.VeryRare, Convert.ToInt32(Config["Paper", "VeryRare"]));

            paperNeeded.Add(Rust.Rarity.None, Convert.ToInt32(Config["Paper", "None"]));
            #endregion
            #region blueprintpartsneeded
            blueprintPartsNeeded = new ListDictionary<Rust.Rarity, int>();

            blueprintPartsNeeded.Add(Rust.Rarity.Common, Convert.ToInt32(Config["BlueprintParts", "Common"]));

            blueprintPartsNeeded.Add(Rust.Rarity.Uncommon, Convert.ToInt32(Config["BlueprintParts", "Uncommon"]));

            blueprintPartsNeeded.Add(Rust.Rarity.Rare, Convert.ToInt32(Config["BlueprintParts", "Rare"]));

            blueprintPartsNeeded.Add(Rust.Rarity.VeryRare, Convert.ToInt32(Config["BlueprintParts", "VeryRare"]));

            blueprintPartsNeeded.Add(Rust.Rarity.None, Convert.ToInt32(Config["BlueprintParts", "None"]));
            #endregion
            #region blueprintpartstype
            blueprintPartsTypeNeeded = new ListDictionary<Rust.Rarity, string>();

            blueprintPartsTypeNeeded.Add(Rust.Rarity.Common, Config["BlueprintParts", "CommonType"].ToString());

            blueprintPartsTypeNeeded.Add(Rust.Rarity.Uncommon, Config["BlueprintParts", "UncommonType"].ToString());

            blueprintPartsTypeNeeded.Add(Rust.Rarity.Rare, Config["BlueprintParts", "RareType"].ToString());

            blueprintPartsTypeNeeded.Add(Rust.Rarity.VeryRare, Config["BlueprintParts", "VeryRareType"].ToString());

            blueprintPartsTypeNeeded.Add(Rust.Rarity.None, Config["BlueprintParts", "NoneType"].ToString());
            #endregion
            #region localization
            localization = new ListDictionary<string, string>();

            localization.Add("NotEnoughPaper", Config["Localization", "NotEnoughPaper"].ToString());

            localization.Add("NotEnoughBluePrintParts", Config["Localization", "NotEnoughBluePrintParts"].ToString());

            localization.Add("BPNotLearned", Config["Localization", "BPNotLearned"].ToString());

            localization.Add("BPIsDrawing", Config["Localization", "BPIsDrawing"].ToString());

            localization.Add("BPDelivery", Config["Localization", "BPDelivery"].ToString());

            localization.Add("AlreadyDrawing", Config["Localization", "AlreadyDrawing"].ToString());

            localization.Add("ItemNotFound", Config["Localization", "ItemNotFound"].ToString());

            localization.Add("NoBP", Config["Localization", "NoBP"].ToString());

            localization.Add("Help", Config["Localization", "Help"].ToString());

            localization.Add("NoPermission", Config["Localization", "NoPermission"].ToString());
            #endregion
            #region itemalias & newitems
            itemAlias = new ListDictionary<string, string>();

            bool newItems = false;

            foreach (ItemDefinition itemDef in ItemManager.itemList)
                if (ItemManager.FindBlueprint(itemDef) != null &&
                    !ItemManager.FindBlueprint(itemDef).defaultBlueprint)
                    if (Config["ZItemAlias", itemDef.shortname] != null)
                        itemAlias.Add(itemDef.shortname, Config["ZItemAlias", itemDef.shortname].ToString());
                    else
                    {
                        Config["ZItemAlias", itemDef.shortname] = itemDef.displayName.english;

                        newItems = true;
                    }

            itemAlias.Add("blueprint_fragment", Config["ZItemAlias", "blueprint_fragment"].ToString().ToLower());

            itemAlias.Add("blueprint_page", Config["ZItemAlias", "blueprint_page"].ToString().ToLower());

            itemAlias.Add("blueprint_book", Config["ZItemAlias", "blueprint_book"].ToString().ToLower());

            itemAlias.Add("blueprint_library", Config["ZItemAlias", "blueprint_library"].ToString().ToLower());

            if (newItems)
            {
                PrintWarning("Config-loader added new items to config.");

                SaveConfig();
            }
            #endregion
            #region drawtimes
            drawTimes = new ListDictionary<Rust.Rarity, int>();

            drawTimes.Add(Rust.Rarity.Common, Convert.ToInt32(Config["DrawTime", "Common"]));

            drawTimes.Add(Rust.Rarity.Uncommon, Convert.ToInt32(Config["DrawTime", "Uncommon"]));

            drawTimes.Add(Rust.Rarity.Rare, Convert.ToInt32(Config["DrawTime", "Rare"]));

            drawTimes.Add(Rust.Rarity.VeryRare, Convert.ToInt32(Config["DrawTime", "VeryRare"]));

            drawTimes.Add(Rust.Rarity.None, Convert.ToInt32(Config["DrawTime", "None"]));
            #endregion
            #region settings
            cancelBPWhenDead = Convert.ToBoolean(Config["Settings", "CancelBPWhenDead"]);

            paperUsageAllowed = Convert.ToBoolean(Config["Settings", "PaperUsageAllowed"]);

            blueprintPartsUsageAllowed = Convert.ToBoolean(Config["Settings", "BlueprintPartsUsageAllowed"]);

            if (!paperUsageAllowed &&
                !blueprintPartsUsageAllowed)
            {
                paperUsageAllowed = true;

                PrintError("Config-Loader reports that neither paperUsage nor blueprintPartsUsage is allowed. PaperUsage will be allowed by default then.");
            }

            if (PopupNotifications == null &&
                Convert.ToBoolean(Config["Settings", "EnablePopups"]))
                PrintError("PopupNotifications-Plugin missing, can't enable pop-ups. Get the plugin first: http://oxidemod.org/plugins/popup-notifications.1252/");
            else if (PopupNotifications != null &&
                     Convert.ToBoolean(Config["Settings", "EnablePopups"]))
                popupsEnabled = true;
            #endregion

            PrintWarning("Blueprinter loaded config.");
        }

        void ConfigUpdater()
        {
            PrintWarning(String.Format("Blueprinter updates config from v{0} to v{1}.", Config["Version"].ToString(), this.Version.ToString()));

            while (Config["Version"].ToString() != this.Version.ToString())
                switch (Config["Version"].ToString())
                {
                    #region 1.0.3 || 1.0.4 => 1.0.5
                    case "1.0.3":
                    case "1.0.4":
                        Config["Settings", "DrawTime"] = null;

                        Config["Settings", "EnablePopups"] = false;

                        Config["DrawTime", "Common"] = 10;

                        Config["DrawTime", "Uncommon"] = 30;

                        Config["DrawTime", "Rare"] = 60;

                        Config["DrawTime", "VeryRare"] = 120;

                        Config["DrawTime", "None"] = 0;

                        Config["Localization", "NoPermission"] = "You have no permission to use this command.";

                        if (!permission.PermissionExists("canuseblueprinter"))
                            permission.RegisterPermission("canuseblueprinter", this);

                        if (!permission.GroupHasPermission("player", "canuseblueprinter"))
                            permission.GrantGroupPermission("player", "canuseblueprinter", this);

                        if (!permission.GroupHasPermission("moderator", "canuseblueprinter"))
                            permission.GrantGroupPermission("moderator", "canuseblueprinter", this);

                        if (!permission.GroupHasPermission("admin", "canuseblueprinter"))
                            permission.GrantGroupPermission("admin", "canuseblueprinter", this);

                        Config["Version"] = "1.0.5";
                        break;
                    #endregion
                }

            SaveConfig();
        }

        [ConsoleCommand("blueconf.recreate")]
        void ConsoleCommandConfigRecreate()
        {
            LoadDefaultConfig();

            ConfigLoader();
        }
		
		[ConsoleCommand("blueconf.load")]
        void ConsoleCommandConfigLoad()
		{
            ConfigLoader();
		}

        [ConsoleCommand("blueconf.set")]
        void ConsoleCommandConfigSet(ConsoleSystem.Arg arg)
        {
            if (IsUInt(arg.GetString(2)))
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetUInt(2);
            else if (arg.GetString(2) == "true" ||
                     arg.GetString(2) == "false")
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetBool(2);
            else
                Config[arg.GetString(0), arg.GetString(1)] = arg.GetString(2);

            SaveConfig();

            ConfigLoader();
        }

        [ChatCommand("bluehelp")]
        void ChatCommandHelp(BasePlayer player)
        {
            if (!popupsEnabled)
                SendReply(player, localization["Help"]);
            else
                PopupNotifications.Call("CreatePopupNotification", localization["Help"].Replace("\"", "'"), player);
        }

        [ChatCommand("blueprinter")]
        void ChatCommandBluePrinter(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "canuseblueprinter"))
            {
                if (!popupsEnabled)
                    SendReply(player, localization["NoPermission"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["NoPermission"].Replace("\"", "'"), player);

                return;
            }
            
            if (args.Length > 1) // in case player forgotten to put "" when writing an itemname with a space in it e.g. Pump Jack => "Pump Jack"
                args = new string[1] { String.Join(" ", args).Trim() };
            else if (args.Length != 1)
            {
                ChatCommandHelp(player);

                return;
            }

            if (Config["InDrawing"] != null &&
                Config["InDrawing", player.userID.ToString()] != null &&
                Config["InDrawing", player.userID.ToString()].ToString() != String.Empty)
            {
                if (!popupsEnabled)
                    SendReply(player, localization["AlreadyDrawing"]);
                else
                    PopupNotifications.Call("CreatePopupNotification", localization["AlreadyDrawing"].Replace("\"", "'"), player);

                return;
            }
            
            foreach (ItemDefinition itemDef in ItemManager.itemList)
                if (itemAlias.Contains(itemDef.shortname) &&
                    itemAlias[itemDef.shortname].ToLower() == args[0].ToLower() ||
                    itemDef.displayName.english.ToLower() == args[0].ToLower())
                {
                    if (ItemManager.FindBlueprint(itemDef) == null ||
                        ItemManager.FindBlueprint(itemDef).defaultBlueprint)
                    {
                        if (!popupsEnabled)
                            SendReply(player, localization["NoBP"]);
                        else
                            PopupNotifications.Call("CreatePopupNotification", localization["NoBP"].Replace("\"", "'"), player);

                        return;
                    }
                    else
                    {
                        Item item = ItemManager.CreateByItemID(itemDef.itemid, 1, true);

                        if (player.blueprints.CanCraft(item.info.itemid, 0))
                        {
                            if (paperUsageAllowed &&
                                player.inventory.FindItemID(106434956) != null &&
                                player.inventory.FindItemID(106434956).amount >= paperNeeded[ItemManager.FindBlueprint(itemDef).rarity] || // 106434956 = paper
                                blueprintPartsUsageAllowed &&
                                player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]) != null &&
                                player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).amount >= blueprintPartsNeeded[ItemManager.FindBlueprint(itemDef).rarity])
                            {
                                if (paperUsageAllowed &&
                                    player.inventory.FindItemID(106434956) != null &&
                                    player.inventory.FindItemID(106434956).amount >= paperNeeded[ItemManager.FindBlueprint(itemDef).rarity])
                                    player.inventory.FindItemID(106434956).amount -= paperNeeded[ItemManager.FindBlueprint(itemDef).rarity];
                                else
                                {
                                    if (player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).amount == blueprintPartsNeeded[ItemManager.FindBlueprint(itemDef).rarity])
                                        player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).RemoveFromContainer();
                                    else
                                    {
                                        player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).amount -= blueprintPartsNeeded[ItemManager.FindBlueprint(itemDef).rarity];

                                        player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain);

                                        player.inventory.SendUpdatedInventory(PlayerInventory.Type.Belt, player.inventory.containerBelt);
                                    }
                                }

                                if (drawTimes[ItemManager.FindBlueprint(itemDef).rarity] != 0)
                                {
                                    if (!popupsEnabled)
                                        SendReply(player, String.Format(localization["BPIsDrawing"], drawTimes[ItemManager.FindBlueprint(itemDef).rarity].ToString()));
                                    else
                                        PopupNotifications.Call("CreatePopupNotification", String.Format(localization["BPIsDrawing"], drawTimes[ItemManager.FindBlueprint(itemDef).rarity].ToString()).Replace("\"", "'"), player);

                                    Config["InDrawing", player.userID.ToString()] = item.info.itemid;

                                    SaveConfig();

                                    Action timed = new Action(() => TimedBluePrint(player.userID.ToString()));

                                    timer.In(drawTimes[ItemManager.FindBlueprint(itemDef).rarity], timed);
                                }
                                else
                                    player.GiveItem(item);
                            }
                            else
                            {
                                if (paperUsageAllowed)
                                {
                                    if (!popupsEnabled)
                                        SendReply(player, String.Format(localization["NotEnoughPaper"], paperNeeded[ItemManager.FindBlueprint(itemDef).rarity].ToString(), player.inventory.FindItemID(106434956) != null ? player.inventory.FindItemID(106434956).amount.ToString() : "0"));
                                    else
                                        PopupNotifications.Call("CreatePopupNotification", String.Format(localization["NotEnoughPaper"], paperNeeded[ItemManager.FindBlueprint(itemDef).rarity].ToString(), player.inventory.FindItemID(106434956) != null ? player.inventory.FindItemID(106434956).amount.ToString() : "0").Replace("\"", "'"), player);
                                }
                                
                                if (blueprintPartsUsageAllowed)
                                {
                                    if (!popupsEnabled)
                                        SendReply(player, String.Format(localization["NotEnoughBluePrintParts"], itemAlias[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]], blueprintPartsNeeded[ItemManager.FindBlueprint(itemDef).rarity].ToString(), player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]) != null ? player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).amount.ToString() : "0"));
                                    else
                                        PopupNotifications.Call("CreatePopupNotification", String.Format(localization["NotEnoughBluePrintParts"], itemAlias[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]], blueprintPartsNeeded[ItemManager.FindBlueprint(itemDef).rarity].ToString(), player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]) != null ? player.inventory.FindItemID(blueprintPartsID[blueprintPartsTypeNeeded[ItemManager.FindBlueprint(itemDef).rarity]]).amount.ToString() : "0").Replace("\"", "'"), player);
                                }
                            }
                        }
                        else
                            if (!popupsEnabled)
                                SendReply(player, localization["BPNotLearned"]);
                            else
                                PopupNotifications.Call("CreatePopupNotification", localization["BPNotLearned"].Replace("\"", "'"), player);

                        return;
                    }
                }

            if (!popupsEnabled)
                SendReply(player, String.Format(localization["ItemNotFound"], args[0]));
            else
                PopupNotifications.Call("CreatePopupNotification", String.Format(localization["ItemNotFound"], args[0]).Replace("\"", "'"), player);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null ||
                hitInfo.Initiator == null ||
                hitInfo.Initiator.ToPlayer() == null)
                return;

            BasePlayer player = hitInfo.Initiator.ToPlayer();

            if (player.userID != null &&
                Config["InDrawing"] != null &&
                Config["InDrawing", player.userID.ToString()] != null &&
                Config["InDrawing", player.userID.ToString()].ToString() != String.Empty &&
                cancelBPWhenDead)
            {
                Config["InDrawing", player.userID.ToString()] = "";

                SaveConfig();
            }
        }

        void TimedBluePrint(string playerID)
        {
            BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(playerID));

            if (Config["InDrawing"] == null ||
                Config["InDrawing", player.userID.ToString()] == null ||
                Config["InDrawing", player.userID.ToString()].ToString() == String.Empty)
                return;

            Item item = ItemManager.CreateByItemID(Convert.ToInt32(Config["InDrawing", playerID]), 1, true);

            if (player == null)
            {
                PrintError(String.Format("Timed blueprint \"{0}\" could not be delivered, player \"{1}\" not found.", itemAlias.Contains(item.info.shortname) && itemAlias[item.info.shortname] != "" ? itemAlias[item.info.shortname] : item.info.displayName.english, playerID));

                Config["FailedDelivery", playerID] = (Config["FailedDelivery", playerID].ToString() != String.Empty ? Config["FailedDelivery", playerID].ToString() + "," : "") + Config["InDrawing", playerID].ToString();

                Config["InDrawing", playerID] = "";

                SaveConfig();

                return;
            }
            else if (player.IsDead() &&
                     !cancelBPWhenDead)
            {
                PrintWarning(String.Format("Player \"{0}\" is dead, delivery for blueprint ({1}) will be postponed by 30 seconds.", playerID, itemAlias.Contains(item.info.shortname) && itemAlias[item.info.shortname] != "" ? itemAlias[item.info.shortname] : item.info.displayName.english));

                Action timed = new Action(() => TimedBluePrint(player.userID.ToString()));

                timer.In(30, timed);

                return;
            }
            else if (!player.IsSleeping())
                if (!popupsEnabled)
                    SendReply(player, String.Format(localization["BPDelivery"], itemAlias.Contains(item.info.shortname) && itemAlias[item.info.shortname] != "" ? itemAlias[item.info.shortname] : item.info.displayName.english));
                else
                    PopupNotifications.Call("CreatePopupNotification", String.Format(localization["BPDelivery"], itemAlias.Contains(item.info.shortname) && itemAlias[item.info.shortname] != "" ? itemAlias[item.info.shortname] : item.info.displayName.english).Replace("\"", "'"), player);

            Config["InDrawing", playerID] = "";

            SaveConfig();

            player.GiveItem(item);
        }

        static bool IsUInt(string s)
        {
            try
            {
                Convert.ToUInt32(s);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
