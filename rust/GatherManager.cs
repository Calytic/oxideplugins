
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Gathering Manager", "Mughisi", "2.2.3", ResourceId = 675)]
    class GatherManager : RustPlugin
    {

        #region Configuration Data
        // Do not modify these values because this will not change anything, the values listed below are only used to create
        // the initial configuration file. If you wish changes to the configuration file you should edit 'GatherManager.json'
        // which is located in your server's config folder: <drive>:\...\server\<your_server_identity>\oxide\config\

        private bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Gather Manager";
        private const string DefaultChatPrefixColor = "#008000ff";

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; private set; }

        // Plugin options
        private static readonly Dictionary<string, object> DefaultGatherResourceModifiers = new Dictionary<string, object>();
        private static readonly Dictionary<string, object> DefaultGatherDispenserModifiers = new Dictionary<string, object>();
        private static readonly Dictionary<string, object> DefaultQuarryResourceModifiers = new Dictionary<string, object>();
        private static readonly Dictionary<string, object> DefaultPickupResourceModifiers = new Dictionary<string, object>();
        private static readonly Dictionary<string, object> DefaultSurveyResourceModifiers = new Dictionary<string, object>();
        private const float DefaultMiningQuarryResourceTickRate = 5f;

        public Dictionary<string, float> GatherResourceModifiers { get; private set; }
        public Dictionary<string, float> GatherDispenserModifiers { get; private set; }
        public Dictionary<string, float> QuarryResourceModifiers { get; private set; }
        public Dictionary<string, float> PickupResourceModifiers { get; private set; }
        public Dictionary<string, float> SurveyResourceModifiers { get; private set; }
        public float MiningQuarryResourceTickRate { get; private set; }

        // Plugin messages
        private const string DefaultNotAllowed = "You don't have permission to use this command.";
        private const string DefaultInvalidArgumentsGather =
            "Invalid arguments supplied! Use gather.rate <type:dispenser|pickup|quarry|survey> <resource> <multiplier>";
        private const string DefaultInvalidArgumentsDispenser =
            "Invalid arguments supplied! Use dispenser.scale <dispenser:tree|ore|corpse> <multiplier>";
        private const string DefaultInvalidArgumentsSpeed =
            "Invalid arguments supplied! Use quarry.rate <time between gathers in seconds>";
        private const string DefaultInvalidModifier =
            "Invalid modifier supplied! The new modifier always needs to be bigger than 0!";
        private const string DefaultInvalidSpeed = "You can't set the speed lower than 1 second!";
        private const string DefaultModifyResource = "You have set the gather rate for {0} to x{1} from {2}.";
        private const string DefaultModifyResourceRemove = "You have reset the gather rate for {0} from {1}.";
        private const string DefaultModifySpeed = "The Mining Quarry will now provide resources every {0} seconds.";
        private const string DefaultInvalidResource =
            "{0} is not a valid resource. Check gather.resources for a list of available options.";
        private const string DefaultModifyDispenser = "You have set the resource amount for {0} dispensers to x{1}";
        private const string DefaultInvalidDispenser =
            "{0} is not a valid dispenser. Check gather.dispensers for a list of available options.";

        private const string DefaultHelpText = "/gather - Shows you detailed gather information.";
        private const string DefaultHelpTextPlayer = "Resources gained from gathering have been scaled to the following:";
        private const string DefaultHelpTextAdmin = "To change the resources gained by gathering use the command:\r\ngather.rate <type:dispenser|pickup|quarry|survey> <resource> <multiplier>\r\nTo change the amount of resources in a dispenser type use the command:\r\ndispenser.scale <dispenser:tree|ore|corpse> <multiplier>\r\nTo change the time between Mining Quarry gathers:\r\nquarry.tickrate <seconds>";
        private const string DefaultHelpTextPlayerGains = "Resources gained from {0}:";
        private const string DefaultHelpTextPlayerMiningQuarrySpeed = "Time between Mining Quarry gathers: {0} second(s).";
        private const string DefaultHelpTextPlayerDefault = "Default values.";
        private const string DefaultDispensers = "Resource Dispensers";
        private const string DefaultCharges = "Survey Charges";
        private const string DefaultQuarries = "Mining Quarries";
        private const string DefaultPickups = "pickups";

        public string NotAllowed { get; private set; }
        public string InvalidArgumentsGather { get; private set; }
        public string InvalidArgumentsDispenser { get; private set; }
        public string InvalidArgumentsSpeed { get; private set; }
        public string InvalidModifier { get; private set; }
        public string InvalidSpeed { get; private set; }
        public string ModifyResource { get; private set; }
        public string ModifyResourceRemove { get; private set; }
        public string ModifySpeed { get; private set; }
        public string InvalidResource { get; private set; }
        public string ModifyDispenser { get; private set; }
        public string InvalidDispenser { get; private set; }
        public string HelpText { get; private set; }
        public string HelpTextPlayer { get; private set; }
        public string HelpTextAdmin { get; private set; }
        public string HelpTextPlayerGains { get; private set; }
        public string HelpTextPlayerDefault { get; private set; }
        public string HelpTextPlayerMiningQuarrySpeed { get; private set; }
        public string Dispensers { get; private set; }
        public string Charges { get; private set; }
        public string Quarries { get; private set; }
        public string Pickups { get; private set; }

        #endregion

        private readonly List<string> subcommands = new List<string>() { "dispenser", "pickup", "quarry", "survey" };

        private readonly Hash<string, ItemDefinition> validResources = new Hash<string, ItemDefinition>();

        private readonly Hash<string, ResourceDispenser.GatherType> validDispensers = new Hash<string, ResourceDispenser.GatherType>();

        private void Init() => LoadConfigValues();

        private void OnServerInitialized()
        {
            var resourceDefinitions = ItemManager.itemList;
            foreach (var def in resourceDefinitions.Where(def => def.category == ItemCategory.Food || def.category == ItemCategory.Resources))
                validResources.Add(def.displayName.english.ToLower(), def);

            validDispensers.Add("tree", ResourceDispenser.GatherType.Tree);
            validDispensers.Add("ore", ResourceDispenser.GatherType.Ore);
            validDispensers.Add("corpse", ResourceDispenser.GatherType.Flesh);
            validDispensers.Add("flesh", ResourceDispenser.GatherType.Flesh);
        }

        protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");

        [ChatCommand("gather")]
        private void Gather(BasePlayer player, string command, string[] args)
        {
            var help = HelpTextPlayer;
            if (GatherResourceModifiers.Count == 0 && SurveyResourceModifiers.Count == 0 && PickupResourceModifiers.Count == 0 && QuarryResourceModifiers.Count == 0)
                help += HelpTextPlayerDefault;
            else
            {
                if (GatherResourceModifiers.Count > 0)
                {
                    var dispensers = string.Format(HelpTextPlayerGains, Dispensers);
                    dispensers = GatherResourceModifiers.Aggregate(dispensers, (current, entry) => current + ("\r\n    " + entry.Key + ": x" + entry.Value));
                    help += "\r\n" + dispensers;
                }
                if (PickupResourceModifiers.Count > 0)
                {
                    var pickups = string.Format(HelpTextPlayerGains, Pickups);
                    pickups = PickupResourceModifiers.Aggregate(pickups, (current, entry) => current + ("\r\n    " + entry.Key + ": x" + entry.Value));
                    help += "\r\n" + pickups;
                }
                if (QuarryResourceModifiers.Count > 0)
                {
                    var quarries = string.Format(HelpTextPlayerGains, Quarries);
                    quarries = QuarryResourceModifiers.Aggregate(quarries, (current, entry) => current + ("\r\n    " + entry.Key + ": x" + entry.Value));
                    help += "\r\n" + quarries;
                }
                if (SurveyResourceModifiers.Count > 0)
                {
                    var charges = string.Format(HelpTextPlayerGains, Charges);
                    charges = SurveyResourceModifiers.Aggregate(charges, (current, entry) => current + ("\r\n    " + entry.Key + ": x" + entry.Value));
                    help += "\r\n" + charges;
                }
            }

            if (MiningQuarryResourceTickRate != DefaultMiningQuarryResourceTickRate)
                help += "\r\n" + string.Format(HelpTextPlayerMiningQuarrySpeed, MiningQuarryResourceTickRate);

            SendMessage(player, help);
            if (!player.IsAdmin()) return;
            SendMessage(player, HelpTextAdmin);
        }

        private void SendHelpText(BasePlayer player) => SendMessage(player, HelpText);

        [ConsoleCommand("gather.rate")]
        private void GatherRate(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                arg.ReplyWith(NotAllowed);
                return;
            }

            var subcommand = arg.GetString(0).ToLower();
            if (!arg.HasArgs(3) || !subcommands.Contains(subcommand))
            {
                arg.ReplyWith(InvalidArgumentsGather);
                return;
            }

            if (!validResources[arg.GetString(1).ToLower()] && arg.GetString(1) != "*")
            {
                arg.ReplyWith(string.Format(InvalidResource, arg.GetString(1)));
                return;
            }

            var resource = validResources[arg.GetString(1).ToLower()]?.displayName.english ?? "*";
            var modifier = arg.GetFloat(2, -1);
            var remove = false;
            if (modifier < 0)
            {
                if (arg.GetString(2).ToLower() == "remove")
                    remove = true;
                else
                {
                    arg.ReplyWith(InvalidModifier);
                    return;
                }
            }

            switch (subcommand)
            {
                case "dispenser":
                    if (remove)
                    {
                        if (GatherResourceModifiers.ContainsKey(resource))
                            GatherResourceModifiers.Remove(resource);
                        arg.ReplyWith(string.Format(ModifyResourceRemove, resource, Dispensers));
                    }
                    else
                    {
                        if (GatherResourceModifiers.ContainsKey(resource))
                            GatherResourceModifiers[resource] = modifier;
                        else
                            GatherResourceModifiers.Add(resource, modifier);
                        arg.ReplyWith(string.Format(ModifyResource, resource, modifier, Dispensers));
                    }
                    SetConfigValue("Options", "GatherResourceModifiers", GatherResourceModifiers);
                    break;
                case "pickup":
                    if (remove)
                    {
                        if (PickupResourceModifiers.ContainsKey(resource))
                            PickupResourceModifiers.Remove(resource);
                        arg.ReplyWith(string.Format(ModifyResourceRemove, resource, Pickups));
                    }
                    else
                    {
                        if (PickupResourceModifiers.ContainsKey(resource))
                            PickupResourceModifiers[resource] = modifier;
                        else
                            PickupResourceModifiers.Add(resource, modifier);
                        arg.ReplyWith(string.Format(ModifyResource, resource, modifier, Pickups));
                    }
                    SetConfigValue("Options", "PickupResourceModifiers", PickupResourceModifiers);
                    break;
                case "quarry":
                    if (remove)
                    {
                        if (QuarryResourceModifiers.ContainsKey(resource))
                            QuarryResourceModifiers.Remove(resource);
                        arg.ReplyWith(string.Format(ModifyResourceRemove, resource, Quarries));
                    }
                    else
                    {
                        if (QuarryResourceModifiers.ContainsKey(resource))
                            QuarryResourceModifiers[resource] = modifier;
                        else
                            QuarryResourceModifiers.Add(resource, modifier);
                        arg.ReplyWith(string.Format(ModifyResource, resource, modifier, Quarries));
                    }
                    SetConfigValue("Options", "QuarryResourceModifiers", QuarryResourceModifiers);
                    break;
                case "survey":
                    if (remove)
                    {
                        if (SurveyResourceModifiers.ContainsKey(resource))
                            SurveyResourceModifiers.Remove(resource);
                        arg.ReplyWith(string.Format(ModifyResourceRemove, resource, Charges));
                    }
                    else
                    {
                        if (SurveyResourceModifiers.ContainsKey(resource))
                            SurveyResourceModifiers[resource] = modifier;
                        else
                            SurveyResourceModifiers.Add(resource, modifier);
                        arg.ReplyWith(string.Format(ModifyResource, resource, modifier, Charges));
                    }
                    SetConfigValue("Options", "SurveyResourceModifiers", SurveyResourceModifiers);
                    break;
            }
        }

        [ConsoleCommand("gather.resources")]
        private void GatherResources(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                arg.ReplyWith(NotAllowed);
                return;
            }

            arg.ReplyWith(validResources.Aggregate("Available resources:\r\n", (current, resource) => current + (resource.Value.displayName.english + "\r\n")) + "* (For all resources that are not setup separately)");
        }

        [ConsoleCommand("gather.dispensers")]
        private void GatherDispensers(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                arg.ReplyWith(NotAllowed);
                return;
            }

            arg.ReplyWith(validDispensers.Aggregate("Available dispensers:\r\n", (current, dispenser) => current + (dispenser.Value.ToString("G") + "\r\n")));
        }


        [ConsoleCommand("dispenser.scale")]
        private void DispenserRate(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                arg.ReplyWith(NotAllowed);
                return;
            }

            if (!arg.HasArgs(2))
            {
                arg.ReplyWith(InvalidArgumentsDispenser);
                return;
            }

            if (!validDispensers.ContainsKey(arg.GetString(0).ToLower()))
            {
                arg.ReplyWith(string.Format(InvalidDispenser, arg.GetString(0)));
                return;
            }

            var dispenser = validDispensers[arg.GetString(0).ToLower()].ToString("G");
            var modifier = arg.GetFloat(1, -1);
            if (modifier < 0)
            {
                arg.ReplyWith(InvalidModifier);
                return;
            }

            if (GatherDispenserModifiers.ContainsKey(dispenser))
                GatherDispenserModifiers[dispenser] = modifier;
            else
                GatherDispenserModifiers.Add(dispenser, modifier);
            SetConfigValue("Options", "GatherDispenserModifiers", GatherDispenserModifiers);
            arg.ReplyWith(string.Format(ModifyDispenser, dispenser, modifier));
        }

        [ConsoleCommand("quarry.tickrate")]
        private void MiningQuarryTickRate(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin())
            {
                arg.ReplyWith(NotAllowed);
                return;
            }

            if (!arg.HasArgs())
            {
                arg.ReplyWith(InvalidArgumentsSpeed);
                return;
            }

            var modifier = arg.GetFloat(0, -1);
            if (modifier < 1)
            {
                arg.ReplyWith(InvalidSpeed);
                return;
            }

            MiningQuarryResourceTickRate = modifier;
            SetConfigValue("Options", "MiningQuarryResourceTickRate", MiningQuarryResourceTickRate);
            arg.ReplyWith(string.Format(ModifySpeed, modifier));
            var quarries = UnityEngine.Object.FindObjectsOfType<MiningQuarry>();
            foreach (var quarry in quarries.Where(quarry => quarry.IsOn()))
            {
                quarry.CancelInvoke("ProcessResources");
                quarry.InvokeRepeating("ProcessResources", MiningQuarryResourceTickRate, MiningQuarryResourceTickRate);
            }
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;

            var gatherType = dispenser.gatherType.ToString("G");
            var amount = item.amount;

            if (GatherResourceModifiers.ContainsKey(item.info.displayName.english))
                item.amount = (int)(item.amount * GatherResourceModifiers[item.info.displayName.english]);
            else if (GatherResourceModifiers.ContainsKey("*"))
                item.amount = (int)(item.amount * GatherResourceModifiers["*"]);

            if (!GatherDispenserModifiers.ContainsKey(gatherType)) return;

            var dispenserModifier = GatherDispenserModifiers[gatherType];

            dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount += amount - item.amount / dispenserModifier;

            if (dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount < 0)
                item.amount += (int)dispenser.containedItems.Single(x => x.itemid == item.info.itemid).amount;
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (QuarryResourceModifiers.ContainsKey(item.info.displayName.english))
                item.amount = (int)(item.amount * QuarryResourceModifiers[item.info.displayName.english]);
            else if (QuarryResourceModifiers.ContainsKey("*"))
                item.amount = (int)(item.amount * QuarryResourceModifiers["*"]);
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (PickupResourceModifiers.ContainsKey(item.info.displayName.english))
                item.amount = (int)(item.amount * PickupResourceModifiers[item.info.displayName.english]);
            else if (PickupResourceModifiers.ContainsKey("*"))
                item.amount = (int)(item.amount * PickupResourceModifiers["*"]);
        }

        private void OnSurveyGather(SurveyCharge surveyCharge, Item item)
        {
            if (SurveyResourceModifiers.ContainsKey(item.info.displayName.english))
                item.amount = (int)(item.amount * SurveyResourceModifiers[item.info.displayName.english]);
            else if (SurveyResourceModifiers.ContainsKey("*"))
                item.amount = (int)(item.amount * SurveyResourceModifiers["*"]);
        }

        private void OnMiningQuarryEnabled(MiningQuarry quarry)
        {
            if (MiningQuarryResourceTickRate == DefaultMiningQuarryResourceTickRate) return;
            quarry.CancelInvoke("ProcessResources");
            quarry.InvokeRepeating("ProcessResources", MiningQuarryResourceTickRate, MiningQuarryResourceTickRate);
        }
        
        private void LoadConfigValues()
        {
            // Plugin settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);

            // Plugin options
            var gatherResourceModifiers = GetConfigValue("Options", "GatherResourceModifiers", DefaultGatherResourceModifiers);
            var gatherDispenserModifiers = GetConfigValue("Options", "GatherDispenserModifiers", DefaultGatherDispenserModifiers);
            var quarryResourceModifiers = GetConfigValue("Options", "QuarryResourceModifiers", DefaultQuarryResourceModifiers);
            var pickupResourceModifiers = GetConfigValue("Options", "PickupResourceModifiers", DefaultPickupResourceModifiers);
            var surveyResourceModifiers = GetConfigValue("Options", "SurveyResourceModifiers", DefaultSurveyResourceModifiers);
            MiningQuarryResourceTickRate = GetConfigValue("Options", "MiningQuarryResourceTickRate", DefaultMiningQuarryResourceTickRate);

            GatherResourceModifiers = new Dictionary<string, float>();
            foreach (var entry in gatherResourceModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                GatherResourceModifiers.Add(entry.Key, rate);
            }

            GatherDispenserModifiers = new Dictionary<string, float>();
            foreach (var entry in gatherDispenserModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                GatherDispenserModifiers.Add(entry.Key, rate);
            }

            QuarryResourceModifiers = new Dictionary<string, float>();
            foreach (var entry in quarryResourceModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                QuarryResourceModifiers.Add(entry.Key, rate);
            }

            PickupResourceModifiers = new Dictionary<string, float>();
            foreach (var entry in pickupResourceModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                PickupResourceModifiers.Add(entry.Key, rate);
            }

            SurveyResourceModifiers = new Dictionary<string, float>();
            foreach (var entry in surveyResourceModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                SurveyResourceModifiers.Add(entry.Key, rate);
            }

            // Plugin messages
            NotAllowed = GetConfigValue("Messages", "NotAllowed", DefaultNotAllowed);
            InvalidArgumentsGather = GetConfigValue("Messages", "InvalidArgumentsGather", DefaultInvalidArgumentsGather);
            InvalidArgumentsDispenser = GetConfigValue("Messages", "InvalidArgumentsDispenserType", DefaultInvalidArgumentsDispenser);
            InvalidArgumentsSpeed = GetConfigValue("Messages", "InvalidArgumentsMiningQuarrySpeed", DefaultInvalidArgumentsSpeed);
            InvalidModifier = GetConfigValue("Messages", "InvalidModifier", DefaultInvalidModifier);
            InvalidSpeed = GetConfigValue("Messages", "InvalidMiningQuarrySpeed", DefaultInvalidSpeed);
            ModifyResource = GetConfigValue("Messages", "ModifyResource", DefaultModifyResource);
            ModifyResourceRemove = GetConfigValue("Messages", "ModifyResourceRemove", DefaultModifyResourceRemove);
            ModifySpeed = GetConfigValue("Messages", "ModifyMiningQuarrySpeed", DefaultModifySpeed);
            InvalidResource = GetConfigValue("Messages", "InvalidResource", DefaultInvalidResource);
            ModifyDispenser = GetConfigValue("Messages", "ModifyDispenser", DefaultModifyDispenser);
            InvalidDispenser = GetConfigValue("Messages", "InvalidDispenser", DefaultInvalidDispenser);
            HelpText = GetConfigValue("Messages", "HelpText", DefaultHelpText);
            HelpTextAdmin = GetConfigValue("Messages", "HelpTextAdmin", DefaultHelpTextAdmin);
            HelpTextPlayer = GetConfigValue("Messages", "HelpTextPlayer", DefaultHelpTextPlayer);
            HelpTextPlayerGains = GetConfigValue("Messages", "HelpTextPlayerGains", DefaultHelpTextPlayerGains);
            HelpTextPlayerDefault = GetConfigValue("Messages", "HelpTextPlayerDefault", DefaultHelpTextPlayerDefault);
            HelpTextPlayerMiningQuarrySpeed = GetConfigValue("Messages", "HelpTextMiningQuarrySpeed", DefaultHelpTextPlayerMiningQuarrySpeed);
            Dispensers = GetConfigValue("Messages", "Dispensers", DefaultDispensers);
            Quarries = GetConfigValue("Messages", "MiningQuarries", DefaultQuarries);
            Charges = GetConfigValue("Messages", "SurveyCharges", DefaultCharges);
            Pickups = GetConfigValue("Messages", "Pickups", DefaultPickups);

            if (!configChanged) return;
            PrintWarning("Configuration file updated.");
            SaveConfig();
        }

        private T GetConfigValue<T>(string category, string setting, T defaultValue)
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

        private void SetConfigValue<T>(string category, string setting, T newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data != null && data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }

        private void SendMessage(BasePlayer player, string message, params object[] args) => player?.SendConsoleCommand("chat.add", -1, string.Format($"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}", args), 1.0);
    }
}