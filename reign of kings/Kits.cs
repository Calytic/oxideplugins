using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Oxide.Core;

using CodeHatch.Engine.Networking;
using CodeHatch.Inventory.Blueprints.Components;
using CodeHatch.ItemContainer;
using CodeHatch.Permissions;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "Mughisi", 1.1, ResourceId = 1025)]
    public class Kits : ReignOfKingsPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'Kits.json' in your server's config folder.
        // <drive>:\...\save\oxide\config\

        bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Kits";
        private const string DefaultChatPrefixColor = "950415";
        private const bool DefaultLogToConsole = true;
        private const string DefaultAdminPermission = "admin";

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; set; }
        public bool LogToConsole { get; private set; }
        public string AdminPermission { get; private set; }

        // Permissions
        private const bool DefaultUseRoKPermissionGroups = true;
        private const bool DefaultUseOxidePermissions = false;

        public bool UseRoKPermissionGroups { get; private set; }
        public bool UseOxidePermissions { get; private set; }

        // Messages
        private const string DefaultNotAllowed = "You are not allowed to use this command!";
        private const string DefaultInvalidArgs = "Invalid arguments supplied, check /kit help for the available options.";
        private const string DefaultNoKitsAvailable = "There are no kits available right now.";
        private const string DefaultKitNotFound = "A kit with the name '{0}' does not exist.";
        private const string DefaultKitNoRoom = "You can't redeem the kit '{0}' because you don't have enough room in your inventory ({1} slots needed).";
        private const string DefaultKitList = "The following kits are available:";
        private const string DefaultKitRedeemed = "You have redeemed a kit: {0}.";
        private const string DefaultKitNoPermission = "You are not allowed to redeem this kit.";
        private const string DefaultKitOutOfUses = "You have reached your limit for this kit.";
        private const string DefaultKitOnCooldown = "This kit is on cooldown. You can't use this kit for another {0}.";
        private const string DefaultKitUsesReset = "Uses reset after {0}.";
        private const string DefaultKitUsesResetRemaining = "{0} until your uses reset.";
        private const string DefaultAKitReset = "Kits data was reset for {0}";
        private const string DefaultAKitCreated = "You have created the kit '{0}'.";
        private const string DefaultAKitCreatedInvFlag = "{0} items have been added to the kit.";
        private const string DefaultAKitRemoved = "You have removed the kit '{0}'.";
        private const string DefaultAKitAlreadyExists = "A kit with the name '{0}' already exists.";
        private const string DefaultAKitUpdateValue = "You have set the {0} option on kit {1} to {2}.";
        private const string DefaultAKitItemNotFound = "Couldn't find the item {0}.";
        private const string DefaultAKitItemDoesNotExist = "The item {0} does not exist. Check /itemlist for a list of available items.";
        private const string DefaultAKitItemAdded = "Added item {0} ({1}) to the kit {2}.";
        private const string DefaultAKitItemRemoved = "Removed item {0} from the kit {1}.";
        private const string DefaultShowKitList = "You can view all the available kits by using the command [CCCCCC]/kit list";
        private const string DefaultRedeemKit = "You can redeem a kit by using the command [CCCCCC]/kit <name>[FFFFFF], where <name> is the kit you want to redeem.";

        public string NotAllowed { get; private set; }
        public string InvalidArgs { get; private set; }
        public string NoKitsAvailable { get; private set; }
        public string KitNotFound { get; private set; }
        public string KitNoRoom { get; private set; }
        public string KitList { get; private set; }
        public string KitRedeemed { get; private set; }
        public string KitNoPermission { get; private set; }
        public string KitOutOfUses { get; private set; }
        public string KitOnCooldown { get; private set; }
        public string KitUsesReset { get; private set; }
        public string KitUsesResetRemaining { get; private set; }
        public string AKitReset { get; private set; }
        public string AKitCreated { get; private set; }
        public string AKitCreatedInvFlag { get; private set; }
        public string AKitRemoved { get; private set; }
        public string AKitAlreadyExists { get; private set; }
        public string AKitUpdateValue { get; private set; }
        public string AKitItemNotFound { get; private set; }
        public string AKitItemDoesNotExist { get; private set; }
        public string AKitItemAdded { get; private set; }
        public string AKitItemRemoved { get; private set; }
        public string ShowKitList { get; private set; }
        public string RedeemKit { get; private set; }

        // Dictionary
        private const string DefaultTimeHours = "hours";
        private const string DefaultTimeMinutes = "minutes";
        private const string DefaultTimeSeconds = "seconds";
        private const string DefaultTimeHour = "hour";
        private const string DefaultTimeMinute = "minute";
        private const string DefaultTimeSecond = "second";
        private const string DefaultTimeDays = "days";
        private const string DefaultTimeDay = "day";
        private const string DefaultCooldown = "Cooldown:";
        private const string DefaultUses = "Uses:";
        private const string DefaultPermission = "Permission:";
        private const string DefaultReset = "Reset:";
        private const string DefaultAllKits = "all kits";
        private const string DefaultRemainingCooldown = "Remaining cooldown:";
        private const string DefaultRemainingUses = "Remaining uses:";

        public string Hours { get; private set; }
        public string Minutes { get; private set; }
        public string Seconds { get; private set; }
        public string Hour { get; private set; }
        public string Minute { get; private set; }
        public string Second { get; private set; }
        public string Days { get; private set; }
        public string Day { get; private set; }
        public string Cooldown { get; private set; }
        public string Uses { get; private set; }
        public string Permission { get; private set; }
        public string Reset { get; private set; }
        public string AllKits { get; private set; }
        public string Remainingcooldown { get; private set; }
        public string Remaininguses { get; private set; }

        #endregion

        #region StoredData

        class StoredData
        {
            public HashSet<KitData> KitsData = new HashSet<KitData>();
            public HashSet<PlayerData> KitsPlayerData = new HashSet<PlayerData>();

            public StoredData()
            {
            }
        }

        class KitData
        {
            public string Name;
            public string Description;
            public bool Enabled;
            public string Permission;
            public int Cooldown;
            public int Uses;
            public int UsesReset;
            public int Stacks;
            public List<KitItem> Items;

            public KitData()
            {
                Items = new List<KitItem>();
            }
        }

        class KitItem
        {
            public string Name;
            public int Amount;

            public KitItem()
            {
            }

            public KitItem(string name, int amount)
            {
                Name = name;
                Amount = amount;
            }
        }

        class PlayerData
        {
            public string Id;
            public string Name;
            public List<KitUsage> Data;

            public PlayerData()
            {
            }

            public PlayerData(Player player)
            {
                Id = player.Id.ToString();
                Name = player.DisplayName;
                Data = new List<KitUsage>();
            }

            public ulong GetUserId()
            {
                ulong id;
                return !ulong.TryParse(Id, out id) ? 0 : id;
            }
        }

        class KitUsage
        {
            public string Name;
            public int Uses;
            public ulong LastUse;
            public string LastUseDate;

            public KitUsage()
            {
            }

            public KitUsage(string name)
            {
                Name = name;
                Uses = 0;
                LastUse = 0;
                LastUseDate = DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        StoredData storedData;

        Hash<string, KitData> kitsdata = new Hash<string, KitData>();

        Hash<ulong, PlayerData> kitsplayerdata = new Hash<ulong, PlayerData>();

        #endregion

        Permission permissions;

        readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private string whitespace = string.Empty;

        void Loaded()
        {
            LoadConfigData();
            LoadKitsData();

            for (var i = 0; i < ChatPrefix.Length + 4; i++)
                whitespace += " ";
        }

        protected override void LoadDefaultConfig() => Warning("New configuration file created.");

        void OnServerInitialized()
        {
            permissions = Server.Permissions;
            ValidateKits();
        }

        void Unload() => SaveData();

        void OnServerSave() => SaveData();

        void OnServerShutdown() => SaveData();

        [ChatCommand("Kit")]
        void KitCommand(Player player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            if (LogToConsole)
                Log($"{player.DisplayName} ran the command /{command} {args.JoinToString(" ")}");

            var subcmd = args[0].ToLower();
            if (args.Length == 1 && subcmd == "help")
            {
                ShowKitsHelp(player);
                return;
            }

            if (args.Length == 1 && subcmd == "list")
            {
                ShowKits(player);
                return;
            }

            if (subcmd == "add" || subcmd == "detail" || subcmd == "modify" || subcmd == "remove" || subcmd == "reset")
            {
                if (!HasPermission(player, AdminPermission))
                {
                    SendMessage(player, NotAllowed);
                    return;
                }

                if (args.Length == 1)
                {
                    SendMessage(player, InvalidArgs);
                    return;
                }

                var cmdargs = new string[args.Length - 1];
                for (var i = 1; i < args.Length; i++)
                    cmdargs[i - 1] = args[i];

                switch (subcmd)
                {
                    case "add":
                        AddKit(player, cmdargs);
                        break;
                    case "detail":
                        DetailKit(player, cmdargs);
                        break;
                    case "modify":
                        ModifyKit(player, cmdargs);
                        break;
                    case "remove":
                        RemoveKit(player, cmdargs);
                        break;
                    case "reset":
                        ResetKit(player, cmdargs);
                        break;
                }

                return;
            }

            KitData kit;
            if (!FindKit(subcmd, out kit))
            {
                SendMessage(player, KitNotFound, subcmd);
                return;
            }

            if (!HasPermission(player, kit.Permission))
            {
                SendMessage(player, KitNoPermission);
                return;
            }

            GiveKit(player, kit);
        }

        void ShowKits(Player player)
        {
            if (kitsdata.Count == 0)
            {
                SendMessage(player, NoKitsAvailable);
                return;
            }

            var kits = new List<string>();
            foreach (var kit in kitsdata.Values)
            {
                if ((!HasPermission(player, kit.Permission) || kit.Stacks == 0) && !HasPermission(player, AdminPermission)) continue;
                var msg = $"{kit.Name} - {kit.Description}";
                var playerData = kitsplayerdata[player.Id];

                if (kit.Cooldown > 0)
                {
                    msg = $"{msg}\n {whitespace} {Cooldown} {GetTimeSpan(kit.Cooldown)}";
                    if (playerData != null && playerData.Data.Any(v => v.Name == kit.Name))
                    {
                        var usage = playerData.Data.Single(v => v.Name == kit.Name);
                        if (usage != null && (GetTimestamp() - usage.LastUse) < (ulong)kit.Cooldown && (kit.Uses == 0 || kit.Uses > usage.Uses))
                            msg = $"{msg}\n {whitespace} {Remainingcooldown} {GetTimeSpan(kit.Cooldown - (int)(GetTimestamp() - usage.LastUse))}";
                    }
                }
                if (kit.Uses > 0)
                {
                    msg = $"{msg}\n {whitespace} {Uses} {kit.Uses}";
                    if (playerData != null && playerData.Data.Any(v => v.Name == kit.Name))
                    {
                        var usage = playerData.Data.Single(v => v.Name == kit.Name);
                        if (usage != null && kit.Uses >= usage.Uses)
                            msg = $"{msg}\n {whitespace} {Remaininguses} {kit.Uses - usage.Uses}";
                    }
                }
                if (kit.UsesReset > 0 && kit.Uses > 0)
                {
                    var val = kit.UsesReset + " " + (kit.UsesReset == 1 ? Day : Days);
                    var str = string.Format(KitUsesReset, val);
                    msg = $"{msg}\n {whitespace} {str}";

                    if (playerData != null && playerData.Data.Any(v => v.Name == kit.Name))
                    {
                        var usage = playerData.Data.Single(v => v.Name == kit.Name);
                        var lastUse = DateTime.Parse(usage.LastUseDate);
                        var resetDate = lastUse.AddDays(kit.UsesReset);
                        var daysRemaining = (resetDate - DateTime.Now).TotalDays;
                        if (daysRemaining > 0)
                        {
                            val = kit.UsesReset + " " + (kit.UsesReset == 1 ? Day : Days);
                            str = string.Format(KitUsesResetRemaining, val);
                            msg = $"{msg}\n {whitespace} {str}";
                        }

                    }
                }
                if (HasPermission(player, AdminPermission) && kit.Permission != null)
                    msg = $"{msg}\n {whitespace} {Permission} {kit.Permission}";

                kits.Add(msg);
            }

            if (kits.Count == 0)
            {
                SendMessage(player, NoKitsAvailable);
                return;
            }

            foreach (var msg in kits)
                SendMessage(player, msg);
        }

        void ShowKitsHelp(Player player)
        {
            SendMessage(player, ShowKitList);
            SendMessage(player, RedeemKit);
            if (!HasPermission(player, AdminPermission)) return;
            SendMessage(player, "The following Kits admin commands are available:" + "\n" +
            "  [F37735]Adding a kit:" + "\n" +
            "    [793B1A]/kit add <kit> <description> [<flag> <value>]" + "\n" +
            "    [FFFFFF]More information on the flags is available on the plugin overview." + "\n" +
            "  [F37735]Removing a kit:" + "\n" +
            "    [793B1A]/kit remove <kit>" + "\n" +
            "  [F37735]Resetting player data for a specific kit:" + "\n" +
            "    [793B1A]/kit reset <kit> [FFFFFF]- To reset a single kit for all players." + "\n" +
            "    [793B1A]/kit reset * [FFFFFF]- To reset all kits for all players." + "\n" +
            "  [F37735]Modifying a kit:" + "\n" +
            "    [936247]Changing a flag:" + "\n" +
            "      [793B1A]/kit modify <kit> <flag> <value>" + "\n" +
            "    [936247]Adding an item:" + "\n" +
            "      [793B1A]/kit modify <kit> additem <item> [<amount>]" + "\n" +
            "    [936247]Removing an item:" + "\n" +
            "      [793B1A]/kit modify <kit> removeitem <item>[FFFFFF]");

        }

        void AddKit(Player player, string[] args)
        {
            if (args.Length < 2)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            var kitname = args[0];
            var description = args[1];

            KitData kit;
            if (FindKit(kitname, out kit))
            {
                SendMessage(player, AKitAlreadyExists, kitname);
                return;
            }

            kit = new KitData();
            var kitinfo = new KitInfo(args);

            kit.Name = kitname;
            kit.Description = description;
            kit.Permission = kitinfo.GetVariable("permission");
            kit.Cooldown = Convert.ToInt32(kitinfo.GetVariable("cooldown"));
            kit.Uses = Convert.ToInt32(kitinfo.GetVariable("uses"));
            kit.UsesReset = Convert.ToInt32(kitinfo.GetVariable("reset"));
            kit.Stacks = 0;

            SendMessage(player, AKitCreated, kitname);

            if (kitinfo.HasVariable("inventory"))
            {
                var inventory = player.CurrentCharacter.Entity.GetContainerOfType(CollectionTypes.Inventory);
                var itemCount = 0;
                foreach (var item in inventory.Contents.Where(item => item != null))
                {
                    itemCount++;
                    kit.Items.Add(new KitItem(item.Name, item.StackAmount));
                    kit.Stacks++;
                }
                SendMessage(player, AKitCreatedInvFlag, itemCount);
            }

            kitsdata.Add(kitname, kit);
            storedData.KitsData.Add(kit);
        }

        void DetailKit(Player player, string[] args)
        {
            if (args.Length != 1)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            var kitname = args[0];

            KitData kit;
            if (!FindKit(kitname, out kit))
            {
                SendMessage(player, KitNotFound, kitname);
                return;
            }

            string msg = $"{kit.Name} - {kit.Description}";
            if (kit.Cooldown > 0)
                msg = $"{msg}\n {whitespace} {Cooldown} {GetTimeSpan(kit.Cooldown)}";
            if (kit.Uses > 0)
                msg = $"{msg}\n {whitespace} {Uses} {kit.Uses}";
            if (HasPermission(player, AdminPermission) && kit.Permission != null)
                msg = $"{msg}\n {whitespace} {Permission} {kit.Permission}";

            msg = $"{msg}\n {whitespace} Items:";
            whitespace += "  ";

            msg = kit.Items.Aggregate(msg, (current, iteminfo) => $"{current}\n {whitespace} {iteminfo.Amount} x {iteminfo.Name}");

            SendMessage(player, msg);
        }

        void ModifyKit(Player player, string[] args)
        {
            if (args.Length < 3)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            var kitname = args[0];
            var action = args[1];
            var value = args[2];

            KitData kit;
            if (!FindKit(kitname, out kit))
            {
                SendMessage(player, KitNotFound, kitname);
                return;
            }

            if (action == "cooldown" || action == "uses" || action == "reset" || action == "permission" || action == "additem" || action == "removeitem")
            {
                var cmdargs = args.Skip(2).ToArray();

                switch (action)
                {
                    case "cooldown":
                    case "uses":
                    case "reset":
                    case "permission":
                        ModifyKitValue(player, kit, action, value);
                        break;
                    case "additem":
                    case "removeitem":
                        ModifyKitItems(player, kit, action, cmdargs);
                        break;
                }
                return;
            }

            SendMessage(player, InvalidArgs);
        }

        void ModifyKitValue(Player player, KitData kit, string action, string value)
        {
            switch (action)
            {
                case "cooldown":
                    int newCooldown;
                    if (int.TryParse(value, out newCooldown))
                        kit.Cooldown = newCooldown;
                    else
                    {
                        SendMessage(player, InvalidArgs);
                        return;
                    }
                    break;
                case "uses":
                    int newUses;
                    if (int.TryParse(value, out newUses))
                        kit.Uses = newUses;
                    else
                    {
                        SendMessage(player, InvalidArgs);
                        return;
                    }
                    break;
                case "reset":
                    int newReset;
                    if (int.TryParse(value, out newReset))
                        kit.UsesReset = newReset;
                    else
                    {
                        SendMessage(player, InvalidArgs);
                        return;
                    }
                    break;
                case "permission":
                    if (value == "none") value = null;
                    kit.Permission = value;
                    break;
            }

            SendMessage(player, AKitUpdateValue, action, kit.Name, value);
        }

        void ModifyKitItems(Player player, KitData kit, string action, string[] args)
        {
            switch (action)
            {
                case "additem":
                    if (args.Length == 0 || args.Length > 2)
                    {
                        SendMessage(player, InvalidArgs);
                        return;
                    }

                    var itemname = args[0];
                    int amount;
                    if (args.Length == 2)
                    {
                        if (!int.TryParse(args[1], out amount))
                            amount = 1;
                    }
                    else amount = 1;

                    var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(itemname, true, true);
                    if (blueprintForName == null)
                    {
                        SendMessage(player, AKitItemDoesNotExist, itemname);
                        return;
                    }

                    SendMessage(player, AKitItemAdded, blueprintForName.Name, amount, kit.Name);

                    KitItem newItem;
                    if (FindItem(kit, blueprintForName.Name, out newItem))
                    {
                        newItem.Amount += amount;
                        return;
                    }

                    newItem = new KitItem(blueprintForName.Name, amount);
                    kit.Items.Add(newItem);

                    break;
                case "removeitem":
                    if (args.Length != 1)
                    {
                        SendMessage(player, InvalidArgs);
                        return;
                    }

                    KitItem item;
                    if (!FindItem(kit, args[0], out item))
                    {
                        SendMessage(player, AKitItemNotFound, args[0]);
                        return;
                    }

                    kit.Items.Remove(item);
                    SendMessage(player, AKitItemRemoved, item.Name, kit.Name);
                    break;
            }
            UpdateKitStacks(kit);
        }

        void RemoveKit(Player player, string[] args)
        {
            if (args.Length != 1)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            var kitname = args[0];

            KitData kit;
            if (!FindKit(kitname, out kit))
            {
                SendMessage(player, KitNotFound, kitname);
                return;
            }

            SendMessage(player, AKitRemoved, kitname);
            ResetKit(player, args, false);
            kitsdata.Remove(kitname);
            storedData.KitsData.Remove(kit);
        }

        void ResetKit(Player player, string[] args, bool reply = true)
        {
            if (args.Length != 1)
            {
                SendMessage(player, InvalidArgs);
                return;
            }

            var kitname = args[0];

            KitData kit;
            if (!FindKit(kitname, out kit) && kitname != "*")
            {
                SendMessage(player, KitNotFound, kitname);
                return;
            }

            if (kitname == "*")
            {
                foreach (var playerinfo in kitsplayerdata)
                    playerinfo.Value.Data = new List<KitUsage>();

                SendMessage(player, AKitReset, AllKits);
                return;
            }

            foreach (var playerinfo in kitsplayerdata)
            {
                KitUsage usage = null;
                foreach (var used in playerinfo.Value.Data)
                    if (used.Name == kit.Name) usage = used;
                if (usage != null) playerinfo.Value.Data.Remove(usage);
            }

            if (reply)
                SendMessage(player, AKitReset, kitname);
        }

        void GiveKit(Player player, KitData kit)
        {
            if (kit.Items.Count == 0) return;

            var inventory = player.CurrentCharacter.Entity.GetContainerOfType(CollectionTypes.Inventory);

            if (inventory.Contents.FreeSlotCount < kit.Stacks)
            {
                SendMessage(player, KitNoRoom, kit.Name, kit.Stacks);
                return;
            }

            if (!CanRedeemKit(player, kit))
                return;

            foreach (var iteminfo in kit.Items)
            {
                var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(iteminfo.Name, true, true);
                var containerManagement = blueprintForName.TryGet<ContainerManagement>();
                var stackLimit = containerManagement?.StackLimit ?? 0;
                var amount = iteminfo.Amount;
                var amountGiven = 0;
                while (amountGiven < amount)
                {
                    var amountToGive = Mathf.Min(stackLimit, amount - amountGiven);
                    var invGameItemStack = new InvGameItemStack(blueprintForName, amountToGive, null);
                    if (!ItemCollection.AutoMergeAdd(inventory.Contents, invGameItemStack))
                    {
                        var stackAmount = amountToGive - invGameItemStack.StackAmount;
                        if (stackAmount != 0)
                            amountGiven += stackAmount;
                    }
                    else
                        amountGiven += amountToGive;

                    if (inventory.Contents.FreeSlotCount == 0) break;
                }
            }

            SendMessage(player, KitRedeemed, kit.Name);
        }

        #region Helpers

        bool HasPermission(Player player, string perm = null)
        {
            if (perm == null) return true;

            if (UseRoKPermissionGroups)
            {
                var user = permissions.GetUser(player.Name);
                return user != null && user.HasGroup(perm);
            }

            if (UseOxidePermissions)
                return permission.UserHasGroup(player.Id.ToString(), perm);

            return false;
        }

        void SendMessage(Player player, string message, params object[] args) => SendReply(player, $"[{ChatPrefixColor}]{ChatPrefix}[FFFFFF]: {message}", args);

        void Log(string msg) => Puts($"{Title} : {msg}");

        void Warning(string msg) => PrintWarning($"{Title} : {msg}");

        void LoadConfigData()
        {
            // Plugin settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);
            LogToConsole = GetConfigValue("Settings", "LogToConsole", DefaultLogToConsole);
            AdminPermission = GetConfigValue("Settings", "AdminPermission", DefaultAdminPermission);

            // Permissions
            UseRoKPermissionGroups = GetConfigValue("Permissions", "UseRoKPermissions", DefaultUseRoKPermissionGroups);
            UseOxidePermissions = GetConfigValue("Permissions", "UseOxidePermissions", DefaultUseOxidePermissions);

            // Messages
            NotAllowed = GetConfigValue("Messages", "NotAllowed", DefaultNotAllowed);
            InvalidArgs = GetConfigValue("Messages", "InvalidArguments", DefaultInvalidArgs);
            NoKitsAvailable = GetConfigValue("Messages", "NoKitsAvailable", DefaultNoKitsAvailable);
            KitNotFound = GetConfigValue("Messages", "KitNotFound", DefaultKitNotFound);
            KitNoRoom = GetConfigValue("Messages", "KitNoRoom", DefaultKitNoRoom);
            KitList = GetConfigValue("Messages", "KitList", DefaultKitList);
            KitRedeemed = GetConfigValue("Messages", "KitRedeemed", DefaultKitRedeemed);
            KitNoPermission = GetConfigValue("Messages", "KitNoPermission", DefaultKitNoPermission);
            KitOutOfUses = GetConfigValue("Messages", "KitNoUsesLeft", DefaultKitOutOfUses);
            KitOnCooldown = GetConfigValue("Messages", "KitOnCooldown", DefaultKitOnCooldown);
            KitUsesReset = GetConfigValue("Messages", "KitUsesReset", DefaultKitUsesReset);
            KitUsesResetRemaining = GetConfigValue("Messages", "KitUsesResetRemaining", DefaultKitUsesResetRemaining);

            AKitReset = GetConfigValue("AdminMessages", "KitReset", DefaultAKitReset);
            AKitCreated = GetConfigValue("AdminMessages", "KitCreated", DefaultAKitCreated);
            AKitCreatedInvFlag = GetConfigValue("AdminMessages", "KitCreatedInventoryFlag", DefaultAKitCreatedInvFlag);
            AKitRemoved = GetConfigValue("AdminMessages", "KitRemoved", DefaultAKitRemoved);
            AKitAlreadyExists = GetConfigValue("AdminMessages", "KitExists", DefaultAKitAlreadyExists);
            AKitUpdateValue = GetConfigValue("AdminMessages", "KitValueUpdated", DefaultAKitUpdateValue);
            AKitItemAdded = GetConfigValue("AdminMessages", "KitItemAdded", DefaultAKitItemAdded);
            AKitItemRemoved = GetConfigValue("AdminMessages", "KitItemRemoved", DefaultAKitItemRemoved);
            AKitItemNotFound = GetConfigValue("AdminMessages", "KitItemNotFound", DefaultAKitItemNotFound);
            AKitItemDoesNotExist = GetConfigValue("AdminMessages", "KitItemDoesNotExist", DefaultAKitItemDoesNotExist);

            ShowKitList = GetConfigValue("HelpMessages", "ShowKits", DefaultShowKitList);
            RedeemKit = GetConfigValue("HelpMessages", "RedeemKit", DefaultRedeemKit);

            // Dictionary
            Hours = GetConfigValue("Dictionary", "Hours", DefaultTimeHours);
            Hour = GetConfigValue("Dictionary", "Hour", DefaultTimeHour);
            Minutes = GetConfigValue("Dictionary", "Minutes", DefaultTimeMinutes);
            Minute = GetConfigValue("Dictionary", "Minute", DefaultTimeMinute);
            Seconds = GetConfigValue("Dictionary", "Seconds", DefaultTimeSeconds);
            Second = GetConfigValue("Dictionary", "Second", DefaultTimeSecond);
            Days = GetConfigValue("Dictionary", "Days", DefaultTimeDays);
            Day = GetConfigValue("Dictionary", "Day", DefaultTimeDay);
            Cooldown = GetConfigValue("Dictionary", "Cooldown", DefaultCooldown);
            Uses = GetConfigValue("Dictionary", "Uses", DefaultUses);
            Permission = GetConfigValue("Dictionary", "Permission", DefaultPermission);
            Reset = GetConfigValue("Dictionary", "Reset", DefaultReset);
            AllKits = GetConfigValue("Dictionary", "AllKits", DefaultAllKits);
            Remainingcooldown = GetConfigValue("Dictionary", "RemainingCooldown", DefaultRemainingCooldown);
            Remaininguses = GetConfigValue("Dictionary", "RemainingUses", DefaultRemainingUses);

            // Check if only one permission system is enabled and fall back to the RoK one if this isn't the case.
            // In the future the Oxide permission system will be used in case of incorrect values but until a new
            // build hasn't been released this will have to wait.
            if ((UseRoKPermissionGroups && UseOxidePermissions) || (!UseRoKPermissionGroups && !UseOxidePermissions))
            {
                Warning("One permission system needs to be activated, falling back to the RoK Permissions.");
                UseRoKPermissionGroups = true;
                UseOxidePermissions = false;
            }

            if (!configChanged) return;
            Warning("The configuration file was updated!");
            SaveConfig();
        }

        void LoadKitsData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Kits");
            foreach (var player in storedData.KitsPlayerData)
                kitsplayerdata[player.GetUserId()] = player;
            foreach (var kit in storedData.KitsData)
                kitsdata[kit.Name] = kit;
        }
        
        void ValidateKits()
        {
            var invalidItems = new List<InvalidItem>();

            foreach (var kit in kitsdata.Values)
            {
                foreach (var iteminfo in kit.Items)
                {
                    var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(iteminfo.Name, true, true);
                    if (blueprintForName == null)
                    {
                        Warning($"The kit '{kit.Name}' contains an invalid item '{iteminfo.Name}'.");
                        invalidItems.Add(new InvalidItem(kit, iteminfo));
                    }
                    else
                        UpdateKitStacks(kit);
                }

                if (kit.Stacks > 24)
                    Warning($"The kit '{kit.Name}' requires {kit.Stacks} inventory slots which is too much for any inventory to handle, consider removing items from the kit or it can't be redeemed!");
            }

            if (invalidItems.Count <= 0) return;
            foreach (var item in invalidItems)
                item.Kit.Items.Remove(item.Item);
            
            Warning(invalidItems.Count == 1
                ? $"  {invalidItems.Count} invalid item was removed."
                : $"  {invalidItems.Count} invalid items were removed.");
        }

        static void UpdateKitStacks(KitData kit)
        {
            var stacks = 0;
            foreach (var iteminfo in kit.Items)
            {
                var blueprintForName = InvDefinitions.Instance.Blueprints.GetBlueprintForName(iteminfo.Name, true, true);
                if (blueprintForName == null) continue;

                var containerManagement = blueprintForName.TryGet<ContainerManagement>();
                var stackLimit = containerManagement?.StackLimit ?? 0;
                stacks += (int)Math.Ceiling((float)iteminfo.Amount / stackLimit);
            }
            kit.Stacks = stacks;
        }

        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("Kits", storedData);

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        string GetTimeSpan(int val)
        {
            var str = new StringBuilder();
            var t = TimeSpan.FromSeconds(val);

            if (Math.Floor(t.TotalHours) > 0)
                str.Append((int)Math.Floor(t.TotalHours) == 1 ? $" {Math.Floor(t.TotalHours)} {Hour}" : $" {Math.Floor(t.TotalHours)} {Hours}");
            if (t.Minutes > 0)
                str.Append(t.Minutes == 1 ? $" {t.Minutes} {Minute}" : $" {t.Minutes} {Minutes}");
            if (t.Seconds > 0)
                str.Append(t.Seconds == 1 ? $" {t.Seconds} {Second}" : $" {t.Seconds} {Seconds}");

            return str.ToString().Trim();
        }

        ulong GetTimestamp() => Convert.ToUInt64((DateTime.UtcNow.Subtract(epoch)).TotalSeconds);

        bool FindKit(string kitname, out KitData kit)
        {
            if (kitsdata.Any(v => string.Equals(v.Value.Name, kitname, StringComparison.CurrentCultureIgnoreCase)))
            {
                kit = kitsdata.Single(v => string.Equals(v.Value.Name, kitname, StringComparison.CurrentCultureIgnoreCase)).Value;
                return true;
            }

            kit = null;
            return false;
        }

        static bool FindItem(KitData kit, string item, out KitItem itemdata)
        {
            if (kit.Items.Any(v => string.Equals(v.Name, item, StringComparison.CurrentCultureIgnoreCase)))
            {
                itemdata = kit.Items.Single(v => string.Equals(v.Name, item, StringComparison.CurrentCultureIgnoreCase));
                return true;
            }

            itemdata = null;
            return false;
        }

        bool CanRedeemKit(Player player, KitData kit)
        {
            var playerData = kitsplayerdata[player.Id];
            if (playerData == null)
            {
                playerData = new PlayerData(player);
                storedData.KitsPlayerData.Add(playerData);
            }

            KitUsage usage = null;
            foreach (var used in playerData.Data.Where(used => used.Name == kit.Name))
                usage = used;

            if (usage == null) usage = new KitUsage(kit.Name);

            if (kit.UsesReset > 0)
            {
                if ((DateTime.Now - DateTime.Parse(usage.LastUseDate)).TotalDays > kit.UsesReset)
                {
                    usage.Uses = 0;
                    usage.LastUse = 0;
                    usage.LastUseDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
            }

            if (kit.Uses > 0 && kit.Uses <= usage.Uses)
            {
                SendMessage(player, KitOutOfUses);
                return false;
            }

            if ((ulong)kit.Cooldown >= (GetTimestamp() - usage.LastUse))
            {
                SendMessage(player, KitOnCooldown, GetTimeSpan(kit.Cooldown - (int)(GetTimestamp() - usage.LastUse)));
                return false;
            }

            if (kit.Permission != null && !HasPermission(player, kit.Permission))
            {
                SendMessage(player, KitNoPermission);
                return false;
            }

            usage.Uses += 1;
            usage.LastUse = GetTimestamp();

            playerData.Data.Remove(usage);
            playerData.Data.Add(usage);
            kitsplayerdata[player.Id] = playerData;
            return true;
        }

        class KitInfo
        {
            Dictionary<string, string> flags = new Dictionary<string, string>();

            public KitInfo(string[] args)
            {
                var info = string.Empty;
                var key = string.Empty;

                info = args.Aggregate(info, (current, str) => current + ("\"" + str.Trim('/', '\\') + "\""));

                foreach (var str in Split(info))
                {
                    if (str.Length <= 0) continue;
                    var val = str;
                    if (str[0] == '-' || str[0] == '+')
                    {
                        if (key != string.Empty && !flags.ContainsKey(key))
                            flags.Add(key.ToLower(), string.Empty);
                        key = val.Substring(1);
                    }
                    else if (key != string.Empty)
                    {
                        if (!flags.ContainsKey(key))
                            flags.Add(key.ToLower(), val);
                        key = string.Empty;
                    }
                }

                if (key != string.Empty && !flags.ContainsKey(key))
                    flags.Add(key.ToLower(), string.Empty);
            }

            static string[] Split(string input)
            {
                input = input.Replace("\\\"", "&qute;");
                var matchs = new Regex("\"([^\"]+)\"|'([^']+)'|\\S+").Matches(input);
                var strArray = new string[matchs.Count];
                for (var i = 0; i < matchs.Count; i++)
                {
                    strArray[i] = matchs[i].Groups[0].Value.Trim(' ', '"');
                    strArray[i] = strArray[i].Replace("&qute;", "\"");
                }

                return strArray;
            }

            public bool HasVariable(string name) => flags.Any(v => v.Key == name);

            public string GetVariable(string name)
            {
                try
                {
                    return flags.Single(v => v.Key == name).Value;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        struct InvalidItem
        {
            public KitData Kit;
            public KitItem Item;

            public InvalidItem(KitData kit, KitItem item)
            {
                Kit = kit;
                Item = item;
            }
        }

        #endregion

    }
}
