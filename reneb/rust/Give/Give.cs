// Reference: Oxide.Ext.Rust

using System;
using System.Collections.Generic;

using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Give", "Reneb", "2.1.2", ResourceId = 666)]
    class Give : RustPlugin
    {
        private bool Changed;

        private int giveBasic;
        private int giveAll;
        private int giveKit;
        private string itemNotFound;
        private string multiplePlayersFound;
        private string noPlayersFound;
        private string noAccess;
        private bool logAdmins;
        private bool Stackable;

        private Dictionary<string,string> displaynameToShortname;

        [PluginReference]
        Plugin Kits;
        
        void Loaded() 
        {
            LoadVariables();
            displaynameToShortname = new Dictionary<string, string>();
        }
        void OnServerInitialized()
        {
            InitializeTable();
        }

        private void InitializeTable () {
            displaynameToShortname.Clear ();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions () ;
            foreach (ItemDefinition itemdef in ItemsDefinition) {
                displaynameToShortname.Add (itemdef.displayName.english.ToString ().ToLower (), itemdef.shortname.ToString ());
            }
        }
        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        private void LoadVariables()
        {
            giveBasic = Convert.ToInt32(GetConfig("authLevel", "give", 1));
            giveAll = Convert.ToInt32(GetConfig("authLevel", "giveall", 2));
            giveKit = Convert.ToInt32(GetConfig("authLevel", "givekit", 1));
            logAdmins = Convert.ToBoolean(GetConfig("Give", "logAdmins", true));
            itemNotFound = Convert.ToString(GetConfig("Messages", "itemNotFound", "This item doesn't exist: "));
            multiplePlayersFound = Convert.ToString(GetConfig("Messages", "multiplePlayersFound", "Multiple Players Found"));
            noPlayersFound = Convert.ToString(GetConfig("Messages", "noPlayersFound", "No Players Found"));
            noAccess = Convert.ToString(GetConfig("Messages", "noAccess", "You are not allowed to use this command"));
            Stackable = Convert.ToBoolean(GetConfig("Give", "overrightStackable", false));
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Give: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        bool hasAccess(BasePlayer player, string ttype)
        {
            if (ttype == "give" && player.net.connection.authLevel >= giveBasic)
                return true;
            if (ttype == "giveall" && player.net.connection.authLevel >= giveAll)
                return true;
            if (ttype == "givekit" && player.net.connection.authLevel >= giveKit)
                return true;
            return false;
        }
        private object FindPlayerByID(ulong steamid) {
            BasePlayer targetplayer = BasePlayer.FindByID(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            targetplayer = BasePlayer.FindSleeping(steamid);
            if (targetplayer != null)
            {
                return targetplayer;
            }
            return null;
        }
        private object FindPlayer(string tofind)
        {
            if (tofind.Length == 17)
            {
                ulong steamid;
                if (ulong.TryParse(tofind.ToString(), out steamid))
                {
                    return FindPlayerByID(steamid);
                }
            }
            List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
            object targetplayer = null;
            foreach (BasePlayer player in onlineplayers.ToArray())
            {

                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return multiplePlayersFound;
                }
            }
            if (targetplayer != null)
                return targetplayer;
            List<BasePlayer> offlineplayers = BasePlayer.sleepingPlayerList as List<BasePlayer>;
            foreach (BasePlayer player in offlineplayers.ToArray())
            {

                if (player.displayName.ToString() == tofind)
                    return player;
                else if (player.displayName.ToString().Contains(tofind))
                {
                    if (targetplayer == null)
                        targetplayer = player;
                    else
                        return multiplePlayersFound;
                }
            }
            if (targetplayer == null)
                return noPlayersFound;
            return targetplayer;
        }
        public object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref, out string description)
        {
            description = itemname;
            itemname = itemname.ToLower();
            if (amount < 1) amount = 1;
            bool isBP = false;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
                return string.Format("{0} {1}",itemNotFound,itemname);
            description = definition.displayName.english.ToString();
            int giveamount = 0;
            int stack = (int)definition.stackable;
            if (stack < 1) stack = 1;
            if (isBP)
            {
                stack = 1;
                description = description + " BP";
            }
            if (Stackable && !isBP)
            {
                player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, amount, isBP), pref);
                SendReply(player, string.Format("You've received {0} x {1}", description, amount.ToString()));
            }
            else
            {
                for (var i = amount; i > 0; i = i - stack)
                {
                    if (i >= stack)
                        giveamount = stack;
                    else
                        giveamount = i;
                    if (giveamount < 1) return true;
                    player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, giveamount, isBP), pref);
                    SendReply(player, string.Format("You've received {0} x {1}", description, giveamount.ToString()));
                }
            }
            return true;
        }
        bool hasKit(string args)
        {
            if (args.Contains(" kit"))
                return true;
            else if (args == "kit")
                return true;
            else if (args.StartsWith("kit "))
                return true;
            return false;
        }
       
        void GiveKit(object source, string[] Args, string ttype)
        {
            if (source is BasePlayer)
            {
                if (((BasePlayer)source).net.connection.authLevel < giveKit)
                {
                    SendTheReply(source, noAccess);
                    return;
                }
            }
            if(Kits == null)
            {
                SendTheReply(source, "You must have the Kits plugin to use this command");
                return;
            }
            if ((ttype == "all" && Args.Length <= 1) || (ttype == "self" && Args.Length <= 1) || (ttype == "player" && Args.Length <= 2))
            {
                SendTheReply(source, "===== Available kits to give =====");
                Kits?.Call ("SendList", source);
                return;
            }
            object target = false;
            if (ttype == "player")
                target = FindPlayer(Args[0]);
            else if (ttype == "self")
                target = source;
            else if (ttype == "all")
                target = true;

            if (target == null) {
                SendTheReply (source, "Couldn't find a player with the steam id " + Args [0].ToString ());
                return;
            }
            if (target is string)
            {
                SendTheReply(source, (string)target);
                return;
            }

            if (Args [Args.Length - 1].ToLower () == "online") {
                var targetPlayer = target as BasePlayer;
                if (!targetPlayer.IsConnected ()) {
                    SendTheReply (source, "Player needs to be online to receive the item!");
                    return;
                }
            }

            object targetkit;
            if (ttype == "player")
                targetkit = Args[2];
            else
                targetkit = Args[1];
            if (ttype == "all")
            {

                int sentkits = 0;
                List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
                foreach (BasePlayer onlineplayer in onlineplayers.ToArray())
                {
                    object trytogivekit = Kits?.Call("GiveKit", onlineplayer, targetkit);
                    if (trytogivekit == null || (trytogivekit is bool && (!(bool)trytogivekit)))
                    {
                        SendTheReply(source, "Couldn't give the kit, does it really exist?");
                        return;
                    }
                    sentkits++;
                }
                SendTheReply(source, string.Format("Kit {0} was given to {1} players",targetkit,sentkits.ToString()));
                if (logAdmins)
                    Puts(string.Format("GIVE: /giveall {0} was used", string.Join(" ", Args)));
            }
            else
            {
                object trytogivekit = Kits?.Call ("GiveKit", target, targetkit);
                if (trytogivekit == null || (trytogivekit is bool && (!(bool)trytogivekit)))
                {
                    SendTheReply(source, "Couldn't give the kit, does it really exist?");
                    return;
                }
                if (logAdmins)
                    Puts(string.Format("GIVE: /give {0} was used", string.Join(" ", Args)));
            }
        }
        void SendTheReply(object source, string message)
        {
            if(source is ConsoleSystem.Arg)
                SendReply((ConsoleSystem.Arg)source, message);
            else
                SendReply((BasePlayer)source, message);
        }
        void GivePlayer(object source, string[] Args)
        {
            if (Args.Length == 1)
            {
                SendTheReply(source, "You need to set an item to give");
                return;
            }
            int amount = 1;
            if (Args.Length > 2)
                int.TryParse(Args[2].ToString(), out amount);

            if (amount == 0)
                amount = 1;

            var target = FindPlayer(Args[0].ToString());
            if (target == null) {
                SendTheReply (source, "Couldn't find a player with the steam id " + Args [0].ToString ());
                return;
            }
            if (target is string)
            {
                SendTheReply(source, target.ToString());
                return;
            }

            if (Args [Args.Length - 1].ToLower() == "online") {
                var targetPlayer = target as BasePlayer;
                if (!targetPlayer.IsConnected()) {
                    SendTheReply (source, "Player needs to be online to receive the item!");
                    return;
                }
            }

            string description = Args[1];
            object error = GiveItem((BasePlayer)target, Args[1], amount, (ItemContainer)((BasePlayer)target).inventory.containerMain, out description);
            if (!(error is bool))
            {
                SendTheReply(source, error.ToString());
                return;
            }
            SendTheReply(source, string.Format("Gave {0} x {1} to {2}", description, amount.ToString(), ((BasePlayer)target).displayName.ToString()));
        }
        void GiveSelf(object source, BasePlayer player, string[] Args)
        {
            int amount = 1;
            if (Args.Length > 1)
                int.TryParse(Args[1].ToString(), out amount);

            string description = Args[0];
            object error = GiveItem(player, Args[0], amount, (ItemContainer)player.inventory.containerMain, out description);
            if (!(error is bool))
            {
                SendTheReply(source, error.ToString());
                return;
            }
            SendTheReply(source, string.Format("Gave {0} x {1} to {2}", description, amount.ToString(), player.displayName.ToString()));
        }
        private void GiveToAll(ConsoleSystem.Arg arg)
        {
            int playersSent = 0;
            int amount = 1;
            if (arg.Args.Length > 1)
            {
                int.TryParse(arg.Args[1].ToString(), out amount);
            }
            List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;
            object error = false;
            string description = arg.Args[0].ToString();
            foreach (BasePlayer player in onlineplayers.ToArray())
            {
                playersSent++;
                error = GiveItem(player, arg.Args[0], amount, (ItemContainer)player.inventory.containerMain, out description);
            }
            if (!(error is bool))
            {
                SendTheReply(arg, error.ToString());
                return;
            }
            SendTheReply(arg, string.Format("Gave {0} x {1} to {2} inventories", description, amount.ToString(), playersSent.ToString()));
        }
        [ChatCommand("give")]
        void cmdChatGivePlayer(BasePlayer player, string command, string[] args)
        {
            if (hasKit(string.Join(" ", args)))
            {
                GiveKit(player, args, "player");
                return;
            }
            if (player.net.connection.authLevel < giveBasic)
            {
                SendReply(player, noAccess);
                return;
            }
            if ((args == null) || (args != null && args.Length == 0))
            {
                SendReply(player, "/give \"Name/SteamID\" \"Item/Kit\" \"Amount\" ");
                return;
            }
            if (logAdmins)
                Puts(string.Format("GIVE: {0} used /give {1}", player.displayName.ToString(), string.Join(" ", args)));
            GivePlayer(player, args);
        }
        [ChatCommand("giveme")]
        void cmdChatGiveMe(BasePlayer player, string command, string[] args)
        {
            if (hasKit(string.Join(" ", args)))
            {
                GiveKit(player, args, "self");
                return;
            }
            if (player.net.connection.authLevel < giveBasic)
            {
                SendReply(player, noAccess);
                return;
            }
            if ((args == null) || (args != null && args.Length == 0))
            {
                SendReply(player, "/giveme \"Item/Kit\" \"Amount\" ");
                return;
            }
            if (logAdmins)
                Puts(string.Format("GIVE: {0} used /giveme {1}", player.displayName.ToString(), string.Join(" ", args)));
            GiveSelf(player, player, args);
        }
        [ConsoleCommand("inv.giveplayer")]
        void cmdConsoleGivePlayer(ConsoleSystem.Arg arg)
        {
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0))
            {
                SendReply(arg, "inv.giveplayer \"Name/SteamID\" \"Item/Kit\" \"Amount\"");
                return;
            }
            if (hasKit(arg.ArgsStr.ToString()))
            {
                GiveKit(arg, (string[])arg.Args, "player");
                return;
            }
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < giveBasic)
                {
                    SendReply(arg, noAccess);
                    return;
                }
                if (logAdmins)
                    Puts(string.Format("GIVE: {0} used inv.giveplayer {1}", ((BasePlayer)arg.connection.player).displayName.ToString(), arg.ArgsStr.ToString()));
            }
            else
                if (logAdmins)
                    Puts(string.Format("GIVE: {0} used inv.giveplayer {1}", "CONSOLE", arg.ArgsStr.ToString()));
            GivePlayer(arg, arg.Args);
        }
        [ConsoleCommand("inv.give")]
        void cmdConsoleGive(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                SendReply(arg, "You can't use this command from the console");
                return;
            }
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0))
            {
                SendReply(arg, "inv.give \"Item/Kit\" \"Amount\"");
                return;
            }
            if (hasKit(arg.ArgsStr.ToString()))
            {
                GiveKit(arg, (string[])arg.Args, "self");
                return;
            }
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < giveBasic)
                {
                    SendReply(arg, noAccess);
                    return;
                }
                if (logAdmins)
                    Puts(string.Format("GIVE: {0} used inv.give {1}", ((BasePlayer)arg.connection.player).displayName.ToString(), arg.ArgsStr.ToString()));
            }
            GiveSelf(arg, (BasePlayer)arg.connection.player, (string[])arg.Args);
        }
        [ConsoleCommand("inv.giveall")]
        void cmdConsoleGiveAll(ConsoleSystem.Arg arg)
        {
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0))
            {
                SendReply(arg, "inv.giveall \"Item/Kit\" \"Amount\"");
                return;
            }
            if (hasKit(arg.ArgsStr.ToString()))
            {
                GiveKit(arg, (string[])arg.Args, "all");
                return;
            }
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < giveAll)
                {
                    SendReply(arg, noAccess);
                    return;
                }
                if (logAdmins)
                    Puts(string.Format("GIVE: {0} used inv.giveall {1}", ((BasePlayer)arg.connection.player).displayName.ToString(), arg.ArgsStr.ToString()));
            }
            else
                if (logAdmins)
                    Puts(string.Format("GIVE: {0} used inv.giveall {1}", "CONSOLE", arg.ArgsStr.ToString()));
            GiveToAll(arg);
        }
    }
}