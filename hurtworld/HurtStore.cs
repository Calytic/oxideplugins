using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Oxide.Core;

namespace Oxide.Plugins
{
    /*
    [B]Changelog 1.3.2[/B]
    [LIST]
    [*] Added config to round numbers.
    [*] Added config to dilute dynamic pricing.
    [/LIST] 
    */
    [Info("HurtStore", "Pho3niX90", "1.3.2", ResourceId = 1646)]
    class HurtStore : HurtworldPlugin
    {
        #region [FIELDS]
        private int ignore = 0;
        Plugin EconomyBanks;
        string MoneySym;
        int decimalPlaces = 2;
        #endregion

        #region [LISTS]
        private Collection<StockItem> _StoreStock = new Collection<StockItem>();
        private Collection<StockItem> _StoreStockDefaults = new Collection<StockItem>();
        public List<StockItem> StoreStock = new List<StockItem>();
        public class StockItem
        {
            public StockItem(string category, int stockId, double price)
            {
                this.category = category;
                this.stockId = stockId;
                this.price = price;
            }

            public string category { get; set; }
            public int stockId { get; set; }
            public double price { get; set; }
        }
        public List<Transaction> Transactions = new List<Transaction>();
        public class Transaction
        {
            public Transaction(int stockId, int sold, int bought)
            {
                this.stockId = stockId;
                this.sold = sold;
                this.bought = bought;
            }

            public int stockId { get; set; }
            public int sold { get; set; }
            public int bought { get; set; }
        }
        public List<Occurence> Occurences = new List<Occurence>();
        public class Occurence
        {
            public Occurence(int row, int stackRemove)
            { this.row = row; this.stackRemove = stackRemove; }
            public int row { get; set; }
            public int stackRemove { get; set; }
        }
        private Collection<PlayerInventory> allOccurences = new Collection<PlayerInventory>();

        double saleModifier;
        private Collection<StockItem> LoadDefaultStoreStock()
        {
            _StoreStockDefaults = new Collection<StockItem>
            {
            new StockItem(GetMsg("cat_food").ToString(),4,20), // Raw Steak
            new StockItem (GetMsg("cat_food"),25,10D), // Owrong
			
            new StockItem (GetMsg("cat_resources"),28,200D), // Clay
            new StockItem (GetMsg("cat_resources"),19,200D), // Stone
            new StockItem (GetMsg("cat_resources"),20,200D), // Coal
            new StockItem (GetMsg("cat_resources"),18,200D), // Iron Ore
            new StockItem (GetMsg("cat_resources"),131,1000D), // Titranium Ore
            new StockItem (GetMsg("cat_resources"),132,1000D), // Mondinium Ore
            new StockItem (GetMsg("cat_resources"),133,1000D), // Ultranium Ore
			
            new StockItem (GetMsg("cat_weapons"),47,200D), // Wood Bow
            new StockItem (GetMsg("cat_weapons"),49,400D), // Hunting Bow
            new StockItem (GetMsg("cat_weapons"),146,900D), // Bow of Punishment
            new StockItem (GetMsg("cat_weapons"),7,1000D), // Bolt Action Rifle
            new StockItem (GetMsg("cat_weapons"),98,1800D), // Assault Rifle Auto
            new StockItem (GetMsg("cat_weapons"),225,1400D), // Assault Rifle Semi
            new StockItem (GetMsg("cat_weapons"),279,1000D), // Pistol
			
            new StockItem (GetMsg("cat_ammo"),48,05D), // Arrow
            new StockItem (GetMsg("cat_ammo"),52,10D), // Bullet
            new StockItem (GetMsg("cat_ammo"),191,15D), // Shotgun Shell
            new StockItem (GetMsg("cat_ammo"),280,10D), // Pistol Bullet
			
            //new StockItem (GetMsg("cat_roachparts"),123,11000D), // Roach Chasis
            new StockItem (GetMsg("cat_roachparts"),166,700D), // Sand Hopper Wheel
            new StockItem (GetMsg("cat_roachparts"),167,600D), // Billycart Wheel
            new StockItem (GetMsg("cat_roachparts"),171,2000D), // Bakuhatsu Weak Engine
            new StockItem (GetMsg("cat_roachparts"),172,3000D), // Bakuhatsu Standard Engine
            new StockItem (GetMsg("cat_roachparts"),173,4000D), // Bakuhatsu Powerfull Engine
            new StockItem (GetMsg("cat_roachparts"),174,1500D), // Bakuhatsu Gearbox
            new StockItem (GetMsg("cat_roachparts"),175,1500D), // Bakuhatsu Road Gearbox
			
            new StockItem (GetMsg("cat_goatparts"),143,9000D), // Goat Chasis
            new StockItem (GetMsg("cat_goatparts"),184,500D), // Quadbike Nipple Wheel
            new StockItem (GetMsg("cat_goatparts"),297,2000D), // Goat Weak Engine
            new StockItem (GetMsg("cat_goatparts"),298,3000D), // Goat Standard Engine
            new StockItem (GetMsg("cat_goatparts"),296,4000D), // Goat Powerfull Engine
            new StockItem (GetMsg("cat_goatparts"),300,1500D), // Goat Offroad Gearbox
            new StockItem (GetMsg("cat_goatparts"),301,1500D)   // Goat Road Gearbox
            };

            SaveStoreStockDefaults();
            return _StoreStockDefaults;
        }
        #endregion

        #region [DEFAULT CONFIGS]
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"cat_food", "Food"},
                {"cat_resources", "Resources"},
                {"cat_weapons", "Weapons"},
                {"cat_ammo", "Ammo"},
                {"cat_roachparts", "Roach Parts"},
                {"cat_goatparts", "Goat Parts"},
                {"msg_buyInvalid1", "Invalid! Please add an item ID after /buy"},
                {"msg_buyInvalid2", "Incorrect buy quantity. Please give a positive number"},
                {"msg_buyInvalid3", "You need another {moneySymbol}{ammountShort} to buy that, you can afford {canBuy} units"},
                {"msg_buySuccessfull","Purchase successful"},
                {"msg_newBalance", "Your new balance is: {moneySymbol}{walletBalance}"},
                {"header_catlisiting","Categories: {Color:Header}To view categorie use {/Color:Header}{Color:Good}/shop catname{/Color:Good}"},
                {"header_itemlisting","Items for sale: {Color:Header}To buy use {/Color:Header}/buy itemid/itemname qty{Color:Header} to sell {/Color:Header}/sell itemid/itemname qty"},
                {"item_listingN","{itemID}: {itemName} {Color:Good} Buy: {moneySymbol}{itemBuy}{/Color:Good}{canSell}, {Color:Bad}Sell: {moneySymbol}{itemSell}{/Color:Bad}{/canSell}"},
                {"msg_soldMsg", "You sold {sellQty} for {moneySymbol}{totalSalesPrice}"},
                {"msg_sellInvalid2", "Incorrect sell quantity. Please give a positive number"},
                {"msg_AfterSale", "{Color:Bad}Old balance: {moneySymbol}{oldBalance}{/Color:Bad} {Color:Good}New Balance: {moneySymbol}{newBalance}{/Color:Good}"},
                {"msg_notEnoughItems","You do not have enough items to sell"},
                {"msg_notBuying", "We aren't buying that at the moment."},
                {"msg_notSelling", "We aren't selling that at the moment."},
                {"msg_idIncorrect", "You didn't use a id after {command}"},
                {"msg_invDoesntHaveItem", "You do not have that item in your inventory"},
                {"msg_sellDisabled", "Sorry, but you cannot sell here."},
                {"config_Success_dynamic", "Dilution has been set to {amount}"},
                {"config_canSellError", "You have to enter a Boolean value I.E true or false"},
                {"config_sellPercentError", "You have to enter an Integer value I.E 20"},
                {"config_intError", "You have to enter an Integer value I.E 20"},
                {"msg_itemAdded", "Your item has been added, into categorie {Color:Green}{item}{/Color:Green}, with a price of {Color:Green}{price}{/Color:Green}"}
            }, this);
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a configuration file for " + this.Title);
            Config.Clear();
            Config["cansell"] = "true";
            Config["sellpercentage"] = "20";
            Config["dynamicpricing"] = "true";
            Config["rounddeciamls"] = "2";
            Config["dynamicdilution"] = "50";

            SaveConfig();
        }

        void CheckConfig()
        {
            Puts("Will now do failsafe check of config file");
            if (Config["dynamicpricing"] == null || (string)Config["dynamicpricing"] == "")
            {
                PrintWarning("Dynamicpricing not set, now creating default");
                Config["dynamicpricing"] = "true";
            }
            if(Config["dynamicdilution"] == null || (string)Config["dynamicdilution"] == "")
            {
                PrintWarning("Dynamic Dilution not set, now creating default");
                Config["dynamicdilution"] = "50";
            }
            SaveConfig();
        }
        #endregion

        #region [HOOKS]
        void StatsOnEventPublished()
        {

        }
        void Loaded()
        {
            LoadMessages();
            LoadStoreStock();
            LoadTransactions();
            CheckConfig();
            _StoreStockDefaults = LoadDefaultStoreStock();

            Puts("Storestock default " + _StoreStockDefaults.Count);
            Puts("Storestock" + _StoreStock.Count);
            if (_StoreStock.Count < 1)
            {
                SaveStoreStockDefaults();
                LoadStoreStock();
                Puts("Defaults loaded = " + _StoreStock.Count);
            }
            foreach (var item in _StoreStock)
            {
                StoreStock.Add(new StockItem(item.category, item.stockId, item.price));
            }

            SaveStoreStock();
            if (((string)Config["rounddeciamls"] == "" || Config["rounddeciamls"] == null) && !int.TryParse((string)Config["rounddeciamls"], out decimalPlaces))
            {
                Config["rounddeciamls"] = "2"; SaveConfig();
            }

        }

        void Init()
        {
            LoadMessages();

            Puts("StoreStock has loaded with " + StoreStock.Count + " items");

            EconomyBanks = (Plugin)plugins.Find("EconomyBanks");
            if (!EconomyBanks.IsLoaded || EconomyBanks == null)
                throw new PluginLoadFailure("EconomyBanks was not found, please download and install from http://oxidemod.org/plugins/economy-banks.1653/");

            MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");

        }
        void OnPluginLoaded(Plugin name)
        {
            if (name.Title.Equals("EconomyBanks", StringComparison.OrdinalIgnoreCase))
            {
                EconomyBanks = (Plugin)plugins.Find("EconomyBanks");
                MoneySym = (string)EconomyBanks.Call("GetMsg", "moneySymbol");

            }
        }
        #endregion

        #region [CHAT COMMANDS]
        [ChatCommand("checkprice")]
        private void ChatCmd_Test(PlayerSession player, string command, string[] args)
        {
            int itemid = int.Parse(args[0]);
            var trans = from p in StoreStock
                        where p.stockId == itemid
                        select p;

            double price = trans.Select(item => item.price).Distinct().Last();
            hurt.SendChatMessage(player, "Original Price: " + price + "Dynamic price: " + DynPrice(itemid));//+ DynPrice(itemid)
        }
        [ChatCommand("sell")]
        private void ChatCmd_Sell(PlayerSession player, string command, string[] args)
        {
            Occurences.Clear();
            if (!bool.Parse(Config["cansell"].ToString()))
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_sellDisabled", player), "bad"));
                return;
            }

            double tmpPer;
            double.TryParse(Config["sellpercentage"].ToString(), out tmpPer);
            saleModifier = (1d - (tmpPer / 100d));

            var argsL = args.Length;
            if (argsL == 0) return;
            PlayerInventory inventory = player.WorldPlayerEntity.GetComponent<PlayerInventory>();
            var invContents = inventory.Items;
            var sellQty = (argsL == 2 ? Int32.Parse(args[1]) : 1);
            var im = GlobalItemManager.Instance;


            int queryItem;
            if (findItemId(args[0]) != -1)
            {
                queryItem = findItemId(args[0]);
            }
            else
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_idIncorrect", player).Replace("{command}", "/sell"), "bad"));
                return;
            }

            int sellQtyCounter = sellQty;
            var isFound = false;
            for (var i = 0; i < inventory.Capacity && sellQtyCounter > 0; i++)
            {
                var invCnt = invContents[i];
                var itemFound = StoreStock.Find(item => item.stockId == queryItem);
                if (invCnt != null && itemFound != null)
                {
                    isFound = true;
                    var itemId = invCnt.Item.ItemId;
                    var itemName = invCnt.Item.GetNameKey().ToString();
                    var itemQty = invCnt.StackSize;
                    if (argsL >= 1 && itemId == queryItem)
                    {

                        if (sellQtyCounter > 0 && itemQty <= sellQtyCounter)
                        {
                            Occurences.Add(new Occurence(i, itemQty));
                            sellQtyCounter -= itemQty;
                        }
                        else if (sellQtyCounter > 0 && sellQtyCounter > 0 && itemQty >= sellQtyCounter)
                        {
                            Occurences.Add(new Occurence(i, sellQtyCounter));
                            sellQtyCounter -= sellQtyCounter;
                        }

                    }
                }
            }

            if (sellQtyCounter == 0)
            {
                PrintWarning("Perfect");
                foreach (var red in Occurences)
                {
                    var invCnt = invContents[red.row];
                    invCnt.ReduceStackSize(red.stackRemove);
                }

                double totalSalesPrice = Round((DynPrice(StoreStock.Find(item => item.stockId == queryItem).stockId) * (double)saleModifier) * sellQty);
                var oldBalance = Round((double)EconomyBanks.Call("Wallet", player));
                im.GiveItem(player.Player, im.GetItem(22), 0);
                EconomyBanks.Call("AddCash", player, totalSalesPrice);
                UpdateTransactions(queryItem, "sold", sellQty);
                var newBalance = Round((double)EconomyBanks.Call("Wallet", player));
                hurt.SendChatMessage(player, GetMsg("msg_soldMsg", player)
                                            .Replace("{moneySymbol}", MoneySym)
                                            .Replace("{sellQty}", sellQty.ToString())
                                            .Replace("{totalSalesPrice}", totalSalesPrice.ToString()));
                hurt.SendChatMessage(player, GetMsg("msg_AfterSale", player)
                                            .Replace("{moneySymbol}", MoneySym)
                                            .Replace("{oldBalance}", oldBalance.ToString())
                                            .Replace("{newBalance}", newBalance.ToString())
                                            .Replace("{Color:Good}", "<color=#00ff00ff>")
                                            .Replace("{/Color:Good}", "</color>")
                                            .Replace("{Color:Bad}", "<color=#ff0000ff>")
                                            .Replace("{/Color:Bad}", "</color>"));
            }
            else if (!isFound)
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_notBuying", player), "bad"));
            }
            else if (sellQty == sellQtyCounter)
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_invDoesntHaveItem", player), "bad"));
            }
            else if (sellQtyCounter > 0)
            {
                PrintWarning("not enought in stock");
                hurt.SendChatMessage(player, "You did not have the quantity requested to be sold, you only have " + (sellQty - sellQtyCounter));
            }
            else
            {
                PrintError("Sh1t aint right = " + sellQtyCounter + ", please report to developer");
            }


        }
        [ChatCommand("shopclear")]
        private void clearRewards(PlayerSession player)
        {
            if (!player.IsAdmin) return;
            _StoreStock.Clear();
            StoreStock.Clear();
            Puts("Cleared = " + _StoreStock.Count);
            SaveStoreStock();
            hurt.SendChatMessage(player, "StoreStock file cleared. Please add some items with /additem categorie itemid/itemname price");
        }

        [ChatCommand("additem")]
        private void ChatCmd_AddItem(PlayerSession player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length <= 2)
            {
                hurt.SendChatMessage(player, "Syntax error: should be /additem categorie itemid/itemname price");

                return;
            }
            int tmpLen = args.Length - 2;
            int itemid;

            double price;
            if (!double.TryParse(args.Last(), out price))
            {
                hurt.SendChatMessage(player, "Syntax error: should be /additem categorie itemid/itemname price");
            }
            itemid = findItemId(args[args.Length - 2]);
            if (itemid == -1)
            {
                hurt.SendChatMessage(player, "The item wasn't found");
                return;
            }

            Array.Resize<string>(ref args, tmpLen);
            string categorie = string.Join(" ", args);

            _StoreStock.Add(new StockItem(categorie, itemid, price));
            SaveStoreStock();
            hurt.SendChatMessage(player, GetMsg("msg_itemAdded", player)
                .Replace("{item}", GetItemName(itemid))
                .Replace("{price}", price.ToString())
                .Replace("{Color:Good}", "<color=#00ff00ff>")
                .Replace("{/Color:Good}", "</color>")
                .Replace("{Color:Green}", "<color=#00ff00ff>")
                .Replace("{/Color:Green}", "</color>")
                );
            ReLoad();

        }
        [ChatCommand("buy")]
        private void ChatCmd_Buy(PlayerSession player, string command, string[] args)
        {
            int buyQty;
            int itemId;
            var ItemMgr = GlobalItemManager.Instance;
            double walletBalance = Round((double)EconomyBanks.Call("Wallet", player));

            if (args.Length == 0)
            {
                hurt.SendChatMessage(player, GetMsg("msg_buyInvalid1", player.SteamId));
                return;
            }

            if (findItemId(args[0]) != -1)
            {
                itemId = findItemId(args[0]);
            }
            else
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_idIncorrect", player).Replace("{command}", "/buy"), "bad"));
                return;
            }


            if (args.Length > 1)
            {
                Int32.TryParse(args[1], out buyQty);
                if (buyQty < 1)
                {
                    hurt.SendChatMessage(player, Color(GetMsg("msg_buyInvalid2", player.SteamId), "bad"));
                    return;
                }
            }
            else
            {
                buyQty = 1;
            }

            bool isFound = false;
            foreach (var storeStock in StoreStock)
            {
                if (storeStock.stockId == itemId)
                {
                    double totalPrice = DynPrice(storeStock.stockId) * buyQty;
                    if (totalPrice > walletBalance)
                    {
                        var canBuy = Math.Floor(walletBalance / DynPrice(storeStock.stockId));
                        hurt.SendChatMessage(player, GetMsg("msg_buyInvalid3", player.SteamId)
                            .Replace("{moneySymbol}", MoneySym)
                            .Replace("{ammountShort}", (totalPrice - walletBalance).ToString())
                            .Replace("{canBuy}", canBuy.ToString()));
                        isFound = true;
                    }
                    else
                    {
                        EconomyBanks.Call("RemoveCash", player, totalPrice);
                        UpdateTransactions(itemId, "bought", buyQty);
                        ItemMgr.GiveItem(player.Player, ItemMgr.GetItem(itemId), buyQty);
                        hurt.SendChatMessage(player, Color(GetMsg("msg_buySuccessfull", player.SteamId), "good"));
                        hurt.SendChatMessage(player, Color(GetMsg("msg_newBalance", player.SteamId)
                            .Replace("{moneySymbol}", MoneySym)
                            .Replace("{walletBalance}", EconomyBanks.Call("Wallet", player).ToString()), "good"));
                        isFound = true;
                    }
                }
            }
            if (!isFound)
            {
                hurt.SendChatMessage(player, Color(GetMsg("msg_notSelling", player), "bad"));
            }
        }
        [ChatCommand("shop")]
        private void ChatCmd_Shop(PlayerSession player, string command, string[] args)
        {
            double tmpPer;
            double.TryParse(Config["sellpercentage"].ToString(), out tmpPer);
            saleModifier = (1d - ((double)tmpPer / 100d));

            switch (args.Length == 0)
            {
                case true:
                    var DistinctCats = StoreStock.Select(x => x.category).Distinct();
                    //TEST
                    var catsCount = (DistinctCats.ToArray().Length > 0 && DistinctCats.ToArray().Length <= 7) ? 9 - DistinctCats.ToArray().Length : 0;
                    int i = 0;
                    while (i < catsCount)
                    {
                        hurt.SendChatMessage(player, "");
                        i++;
                    }
                    //TEST
                    hurt.SendChatMessage(player, GetMsg("header_catlisiting", player.SteamId)
                                                            .Replace("{Color:Header}", "<color=#00ffffff>")
                                                            .Replace("{/Color:Header}", "</color>")
                                                            .Replace("{Color:Good}", "<color=#00ff00ff>")
                                                            .Replace("{/Color:Good}", "</color>"));
                    foreach (var cats in DistinctCats)
                    {
                        if (cats != null)
                            hurt.SendChatMessage(player, cats);
                    }

                    break;

                case false:
                    PrintWarning("check if settings");
                    if ((args[0] == "config" || args[0] == "setup") && (player.IsAdmin || player.Name.Equals("mariaan", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (args.Length < 2)
                        {
                            hurt.SendChatMessage(player, Config["cansell"].ToString());
                            return;
                        }
                        switch (args[1])
                        {
                            case "cansell":
                                bool cansell;
                                if (!bool.TryParse(args[2], out cansell))
                                {
                                    hurt.SendChatMessage(player, Color(GetMsg("config_canSellError", player), "bad"));
                                    return;
                                }
                                Config["cansell"] = cansell;
                                hurt.SendChatMessage(player, "cansell set to " + args[2]);
                                SaveConfig();
                                break;
                            case "sellpercent":
                                int sellpercent;
                                if (!int.TryParse(args[2], out sellpercent))
                                {
                                    hurt.SendChatMessage(player, Color(GetMsg("config_sellPercentError", player), "bad"));
                                    return;
                                }
                                Config["sellpercentage"] = sellpercent;
                                hurt.SendChatMessage(player, "sellpercent set to " + args[2]);
                                SaveConfig();
                                break;
                            case "pricing":
                                string pricingType = args[2];
                                if (pricingType.Equals("dynamic", StringComparison.OrdinalIgnoreCase))
                                {
                                    Config["dynamicpricing"] = "true";
                                }
                                else if (pricingType.Equals("normal", StringComparison.OrdinalIgnoreCase))
                                {
                                    Config["dynamicpricing"] = "false";
                                }
                                SaveConfig();
                                break;
                            case "dynamic":
                                int dilution;
                                if(!int.TryParse(args[2], out dilution))
                                {
                                    hurt.SendChatMessage(player, GetMsg("config_intError", player));
                                    return;
                                }
                                hurt.SendChatMessage(player, GetMsg("config_Success_dynamic",  player).Replace("{amount}", dilution.ToString()));
                                SaveConfig();
                                break;
                        }
                        return;
                    }

                    var searchString = string.Join(" ", args.ToArray());
                    Puts(searchString);
                    //TEST
                    var tmpCats = from p in StoreStock
                                  where p.category.Equals(searchString, StringComparison.OrdinalIgnoreCase)
                                  select p;
                    StockItem[] cats_si = tmpCats.ToArray();
                    catsCount = (cats_si.Length > 0 && cats_si.Length <= 7) ? 9 - cats_si.Length : 0;
                    //var pages = Math.Ceiling((double)cats_si.Length / 8);
                    i = 0;
                    while (i < catsCount)
                    {
                        hurt.SendChatMessage(player, "\n");
                        i++;
                    }
                    //TEST
                    hurt.SendChatMessage(player, GetMsg("header_itemlisting", player.SteamId)
                                                            .Replace("{Color:Header}", "<color=#00ffffff>")
                                                            .Replace("{/Color:Header}", "</color>")
                                                            .Replace("{Color:Good}", "<color=#00ff00ff>")
                                                            .Replace("{/Color:Good}", "</color>"));
                    foreach (var storeStock in cats_si)
                    {
                        double salesPrice = Round(DynPrice(storeStock.stockId) * (double)saleModifier);
                        var tmpString = GetMsg("item_listingN", player.SteamId)
                            .Replace("{moneySymbol}", MoneySym)
                            .Replace("{itemID}", Color(storeStock.stockId.ToString(), "header"))
                            .Replace("{itemName}", GetItemName(storeStock.stockId))
                            .Replace("{Color:Good}", "<color=#00ff00ff>")
                            .Replace("{/Color:Good}", "</color>")
                            .Replace("{Color:Bad}", "<color=#ff0000ff>")
                            .Replace("{/Color:Bad}", "</color>")
                            .Replace("{itemBuy}", DynPrice(storeStock.stockId).ToString())
                            .Replace("{itemSell}", salesPrice.ToString());

                        var result = Regex.Replace(tmpString, @"\{canSell}(.*?)\{/canSell}",
                            m =>
                            {
                                string codeString = "";
                                if (bool.Parse(Config["cansell"].ToString()))
                                {
                                    codeString = m.Groups[1].Value;
                                }


                                return codeString;
                            });


                        hurt.SendChatMessage(player, result);

                    }
                    break;
            }
        }
        #endregion

        #region [SAVE AND LOADS]
        private void UpdateTransactions(int itemId, string action, int qty)
        {
            // ... define after getting the List/Enumerable/whatever
            var dict = Transactions.ToDictionary(x => x.stockId);
            // ... somewhere in code
            Transaction found;
            if (dict.TryGetValue(itemId, out found))
            {
                if (action.Equals("sold", StringComparison.OrdinalIgnoreCase)) { found.sold += qty; } else if (action.Equals("bought", StringComparison.OrdinalIgnoreCase)) { found.bought += qty; }
            }
            else
            {
                if (action.Equals("sold", StringComparison.OrdinalIgnoreCase)) { Transactions.Add(new Transaction(itemId, qty, 0)); }
                else if (action.Equals("bought", StringComparison.OrdinalIgnoreCase)) { Transactions.Add(new Transaction(itemId, 0, qty)); }
            }
            SaveTransactions();
        }
        private void LoadTransactions()
        {
            Transactions = Interface.GetMod().DataFileSystem.ReadObject<List<Transaction>>("HurtStoreTransactions");
        }
        private void SaveTransactions()
        {
            Interface.GetMod().DataFileSystem.WriteObject("HurtStoreTransactions", Transactions);
        }
        private void LoadStoreStock()
        {
            try
            {
                _StoreStock = Interface.GetMod().DataFileSystem.ReadObject<Collection<StockItem>>("HurtStoreStock");
            }
            catch (Exception e)
            {

                PrintWarning("You are using the old storestock format, we will now try and convert it.");
                var _StoreStock_OLD = Interface.GetMod().DataFileSystem.ReadObject<Collection<string[]>>("HurtStoreStock");
                foreach (var item in _StoreStock_OLD)
                {
                    var cat = item[0];
                    int stockid = int.Parse(item[1]);
                    double price = int.Parse(item[2]);
                    StoreStock.Add(new StockItem(cat, stockid, price));
                }
                PrintWarning("Conversion complete, we converted " + StoreStock.Count + " items");
                SaveStoreStock();

            }
        }
        private void SaveStoreStock()
        {
            Interface.GetMod().DataFileSystem.WriteObject("HurtStoreStock", _StoreStock);
        }

        private void SaveStoreStockDefaults()
        {
            Interface.GetMod().DataFileSystem.WriteObject("HurtStoreStock", _StoreStockDefaults);
        }
        #endregion

        #region [HELPERS]
        void ReLoad()
        {
            _StoreStock.Clear();
            StoreStock.Clear();
            Puts("count before load " + _StoreStock.Count);
            Loaded();
            Puts("count after load " + _StoreStock.Count);
        }
        string GetItemName(int itemId)
        {
            var ItemMgr = GlobalItemManager.Instance;
            return ItemMgr.GetItem(itemId).GetNameKey().ToString().Split('/').Last();
        }
        int findItemId(string searchQuery, PlayerSession player = null)
        {
            int itemidq;
            if (!int.TryParse(searchQuery, out itemidq))
            {

            }
            foreach (var item in GlobalItemManager.Instance.GetItems())
            {
                int itemid = item.Value.ItemId;
                string itemName = item.Value.GetNameKey();
                if (GetItemName(itemid).Equals(searchQuery, StringComparison.OrdinalIgnoreCase) || itemid == itemidq)
                {
                    return itemid;
                }
            }
            return -1;
        }
        double DynPrice(int itemid)
        {
            var trans = from p in Transactions
                        where p.stockId == itemid
                        select p;
            int Supply = trans.Sum(item => item.sold);
            int Demand = trans.Sum(item => item.bought);
            var Sup = from p in StoreStock.ToList()
                      where p.stockId == itemid
                      select p;
            double Price = double.Parse(Sup.Select(item => item.price).Distinct().Last().ToString());
            int dilution; if(!int.TryParse((string)Config["dynamicdilution"], out dilution)) {  dilution = 50; }

            Demand = (Demand == 0) ? 1 : Demand; Demand += dilution;
            Supply = (Supply == 0) ? 1 : Supply; Supply += dilution;

            double FinalPrice = ((double)Demand / Supply) * Price;


            return (bool.Parse((string)Config["dynamicpricing"])) ? Round(FinalPrice) : Round(Price);
        }
        double Round(double amount) { return Math.Round(amount, decimalPlaces); }
        string Color(string text, string color)
        {
            switch (color)
            {
                case "bad":
                    return "<color=#ff0000ff>" + text + "</color>";

                case "good":
                    return "<color=#00ff00ff>" + text + "</color>";

                case "header":
                    return "<color=#00ffffff>" + text + "</color>";

                default:
                    return "<color=#" + color + ">" + text + "</color>";
            }
        }
        string GetMsg(string key, object userID = null)
        {
            return (userID != null) ? lang.GetMessage(key, this, userID.ToString()) : lang.GetMessage(key, this);
        }
        #endregion
    }
}