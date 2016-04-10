using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("RGive", "LaserHydra", "2.0.2", ResourceId = 929)]
    [Description("Random item giving")]

    class RGive : RustPlugin
    {
        #region Class

        class Category : Dictionary<string, object>
        {
            public Category(int MinimalAmount, int MaximalAmount, bool Enabled)
            {
                this.Add("Minimal Amount", MinimalAmount);
                this.Add("Maximal Amount", MaximalAmount);
                this.Add("Enabled", Enabled);
            }
        }

        #endregion

        #region Plugin General
        ////////////////////////////////////////
        ///     On Plugin Loaded
        ////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("rgive.use", this);

            LoadConfig();
        }

        ////////////////////////////////////////
        ///     Config Handling
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Categories", "Weapon", new Category(1, 2, true));
            SetConfig("Categories", "Construction", new Category(1, 5, true));
            SetConfig("Categories", "Items", new Category(1, 5, true));
            SetConfig("Categories", "Resources", new Category(500, 10000, false));
            SetConfig("Categories", "Attire", new Category(1, 2, true));
            SetConfig("Categories", "Tool", new Category(1, 2, true));
            SetConfig("Categories", "Medical", new Category(1, 5, true));
            SetConfig("Categories", "Food", new Category(5, 10, false));
            SetConfig("Categories", "Ammunition", new Category(5, 64, true));
            SetConfig("Categories", "Traps", new Category(1, 3, true));
            SetConfig("Categories", "Misc", new Category(1, 5, false));

            SetConfig("Settings", "Item Blacklist", new List<object> { "autoturret", "mining.quarry", "mining.pumpjack", "cctv.camera", "targeting.computer" });

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

#endregion

        #region Subject Related

        object GetRandomAmount(string category)
        {
            if (Config["Categories", category] == null)
                return null;

            Dictionary<string, object> settings = Config["Categories", category] as Dictionary<string, object>;

            if (settings == null)
                return null;

            int minAmount = Convert.ToInt32(settings["Minimal Amount"]);
            int maxAmount = Convert.ToInt32(settings["Maximal Amount"]) + 1;

            return UnityEngine.Random.Range(minAmount, maxAmount);
        }

        BasePlayer GetRandomPlayer() => BasePlayer.activePlayerList[UnityEngine.Random.Range(0, BasePlayer.activePlayerList.Count - 1)];

        void GiveRandomItem(BasePlayer player)
        {
            if (player == null)
                return;

            ItemDefinition info = ItemManager.itemList[UnityEngine.Random.Range(0, ItemManager.itemList.Count - 1)];

            List<object> blacklist = GetConfig(new List<object> { "autoturret", "mining.quarry", "mining.pumpjack", "cctv.camera", "targeting.computer" }, "Settings", "Item Blacklist");

            if (!GetConfig(false, "Categories", info.category, "Enabled") || blacklist.Contains(info.shortname))
            {
                GiveRandomItem(player);
                return;
            }

            int amount = (int)GetRandomAmount(info.category.ToString());

            GiveItem(ItemManager.CreateByItemID(info.itemid), amount, player);

            string message = $"You have recieved random items: {amount}x {info.displayName.english}s";

            if (amount == 1 || info.displayName.english.EndsWith("s"))
                message = $"You have recieved a random item: {amount}x {info.displayName.english}";

            SendChatMessage(player, "RGive", message);
        }

        void GiveItem(Item item, int amount, BasePlayer player)
        {
            if (item == null)
                return;

            player.inventory.GiveItem(ItemManager.CreateByItemID(item.info.itemid, amount));
        }

#endregion

        #region Commands

        [ChatCommand("rgive")]
        void cmdRGive(BasePlayer player, string cmd, string[] args)
        {
            if (player != null)
            {
                if (!HasPerm(player.userID, "use"))
                {
                    SendChatMessage(player, "RGive", "You don't have permission to use this command!");
                    return;
                }
            }

            //	Give Random item to all
            if (args.Length == 1 && args[0].ToLower() == "all")
            {
                foreach (BasePlayer current in BasePlayer.activePlayerList)
                    GiveRandomItem(current);

                BroadcastChat("RGive", "Random items have been given to all online players!");

                return;
            }

            //	Show Syntax
            if (args.Length < 2)
            {
                SendChatMessage(player, "<size=20>RGive</size>", "\n" +
                            "<color=#00FF8D>/rgive player <playername></color> give random item to specific player\n" +
                            "<color=#00FF8D>/rgive item <itemname></color> give specific item to random player\n" +
                            "<color=#00FF8D>/rgive all</color> give random item to all players\n");

                return;
            }

            if (args.Length >= 2)
            {
                switch (args[0].ToLower())
                {
                    //	Give random item to specific player
                    case "player":
                        BasePlayer specificTarget = GetPlayer(args[1], player);
                        if (specificTarget == null) return;

                        GiveRandomItem(specificTarget);
                        SendChatMessage(player, "RGive", "Random items given to " + specificTarget.displayName);

                        break;

                    //	Give specific item to random player
                    case "item":
                        BasePlayer randomTarget = GetRandomPlayer();
                        Item item = GetItem(args[1], player);
                        if (item == null)
                            return;

                        int amount = (int)GetRandomAmount(item.info.category.ToString());

                        GiveItem(item, amount, randomTarget);

                        //	Send message to the sender

                        if (amount == 1 || item.info.displayName.english.EndsWith("s"))
                            SendChatMessage(player, "RGive", $"{amount} {item.info.displayName.english} given to {randomTarget.displayName}");
                        else
                            SendChatMessage(player, "RGive", $"{amount} {item.info.displayName.english}s given to {randomTarget.displayName}");

                        //	Send message to the lucky reciever

                        if (amount == 1 || item.info.displayName.english.EndsWith("s"))
                            SendChatMessage(randomTarget, "RGive", $"You have been randomly chosen to recieve {amount} {item.info.displayName.english}");
                        else
                            SendChatMessage(randomTarget, "RGive", "You have been randomly chosen to recieve {amount} {item.info.displayName.english}s");

                        break;

                    //	Wrong args, show Syntax
                    default:
                        SendChatMessage(player, "<size=20>RGive</size>", "\n" +
                            "<color=#00FF8D>/rgive player <playername></color> give random item to specific player\n" +
                            "<color=#00FF8D>/rgive item <itemname></color> give specific item to random player\n" +
                            "<color=#00FF8D>/rgive all</color> give random item to all players\n");
                        break;
                }
            }
        }

        [ConsoleCommand("rgive")]
        void ccmdRGive(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.connection?.player == null ? null : (BasePlayer) arg.connection.player;

            string[] args = arg.HasArgs() ? arg.Args : new string[0];

            cmdRGive(player, arg.cmd.name, args);
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Player & Item Finding
        ////////////////////////////////////////

        Item GetItem(string searchedItem, BasePlayer player)
        {
            if (ItemManager.CreateByName(searchedItem.ToLower()) != null)
                return ItemManager.CreateByName(searchedItem.ToLower());

            List<string> foundItemNames =
                (from info in ItemManager.itemList
                 where info.shortname.ToLower().Contains(searchedItem.ToLower())
                 select info.shortname).ToList();

            switch (foundItemNames.Count)
            {
                case 0:
                    SendChatMessage(player, "The item can not be found.");

                    break;

                case 1:
                    return ItemManager.CreateByName(foundItemNames[0]);

                default:
                    string players = ListToString(foundItemNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching items found: \n" + players);

                    break;
            }

            return null;
        }

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer player)
        {
            foreach (BasePlayer current in BasePlayer.activePlayerList)
                if (current.displayName.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<BasePlayer> foundPlayers =
                (from current in BasePlayer.activePlayerList
                 where current.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.displayName).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Data Related
        ////////////////////////////////////////

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

        void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if (player == null)
                Puts(msg == null ? StripTags(prefix) : StripTags(msg));
            else
                rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
        }

        string StripTags(string original)
        {
            List<string> regexTags = new List<string>
            {
                @"<color=.+?>",
                @"<size=.+?>"
            };

            List<string> tags = new List<string>
            {
                "</color>",
                "</size>",
                "<i>",
                "</i>",
                "<b>",
                "</b>"
            };

            foreach (string tag in tags)
                original = original.Replace(tag, "");

            foreach (string regexTag in regexTags)
                original = new Regex(regexTag).Replace(original, "");

            return original;
        }

        #endregion
    }
}
