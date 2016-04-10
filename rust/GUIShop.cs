using System.Collections.Generic;
using System;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GUIShop", "Reneb", "1.3.0", ResourceId = 1319)]
    class GUIShop : RustPlugin
    {
        private const string ShopOverlayName = "ShopOverlay";
        private const string ShopContentName = "ShopContent";
        private const string ShopDescOverlay = "ShopDescOverlay";
        private readonly int[] steps = { 1, 10, 100, 1000 };
        int playersMask;

        //////////////////////////////////////////////////////////////////////////////////////
        // References ////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin Kits;

        void OnServerInitialized()
        {
            InitializeTable();
            if (!Economics) PrintWarning("Economics plugin not found. " + Name + " will not function!");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configs Manager ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T) Config[Key];
            else
            {
                Config[Key] = var;
                configChanged = true;
            }
        }

        private Dictionary<string, object> ShopCategories = DefaultShopCategories();
        private Dictionary<string, object> Shops = DefaultShops();

        string MessageShowNoEconomics = "Couldn't get informations out of Economics. Is it installed?";
        string MessageBought = "You've successfully bought {0}x {1}";
        string MessageSold = "You've successfully sold {0}x {1}";
        string MessageErrorCooldown = "This item has a cooldown of {0} seconds.";
        string MessageErrorCooldownAmount = "This item has a cooldown and amount is limited to 1.";
        string MessageErrorInventoryFull = "Your inventory is full.";
        string MessageErrorInventorySlots = "Your inventory needs {0} free slots.";
        string MessageErrorNoShop = "This shop doesn't seem to exist.";
        string MessageErrorNoActionShop = "You are not allowed to {0} in this shop";
        string MessageErrorNoNPC = "The NPC owning this shop was not found around you";
        string MessageErrorNoActionItem = "You are not allowed to {0} this item here";
        string MessageErrorItemItem = "WARNING: The admin didn't set this item properly! (item)";
        string MessageErrorItemNoValid = "WARNING: It seems like it's not a valid item";
        string MessageErrorRedeemKit = "WARNING: There was an error while giving you this kit";
        string MessageErrorBuyPrice = "WARNING: No buy price was given by the admin, you can't buy this item";
        string MessageErrorSellPrice = "WARNING: No sell price was given by the admin, you can't sell this item";
        string MessageErrorNotEnoughMoney = "You need {0} coins to buy {1} of {2}";
        string MessageErrorNotEnoughSell = "You don't have enough of this item.";
        string MessageErrorItemNoExist = "WARNING: The item you are trying to buy doesn't seem to exist";
        string MessageErrorNPCRange = "You may not use the chat shop. You might need to find the NPC Shops.";

        void Init()
        {
            CheckCfg("Shop - Shop Categories", ref ShopCategories);
            CheckCfg("Shop - Shop List", ref Shops);
            CheckCfg("Message - Error - No Econonomics", ref MessageShowNoEconomics);
            CheckCfg("Message - Bought", ref MessageBought);
            CheckCfg("Message - Sold", ref MessageSold);
            CheckCfg("Message - Error - Cooldown", ref MessageErrorCooldown);
            CheckCfg("Message - Error - Cooldown Amount", ref MessageErrorCooldownAmount);
            CheckCfg("Message - Error - Invetory Full", ref MessageErrorInventoryFull);
            CheckCfg("Message - Error - Invetory Slots", ref MessageErrorInventorySlots);
            CheckCfg("Message - Error - No Shop", ref MessageErrorNoShop);
            CheckCfg("Message - Error - No Action In Shop", ref MessageErrorNoActionShop);
            CheckCfg("Message - Error - No NPC", ref MessageErrorNoNPC);
            CheckCfg("Message - Error - No Action Item", ref MessageErrorNoActionItem);
            CheckCfg("Message - Error - Item Not Set Properly", ref MessageErrorItemItem);
            CheckCfg("Message - Error - Item Not Valid", ref MessageErrorItemNoValid);
            CheckCfg("Message - Error - Redeem Kit", ref MessageErrorRedeemKit);
            CheckCfg("Message - Error - No Buy Price", ref MessageErrorBuyPrice);
            CheckCfg("Message - Error - No Sell Price", ref MessageErrorSellPrice);
            CheckCfg("Message - Error - Not Enough Money", ref MessageErrorNotEnoughMoney);
            CheckCfg("Message - Error - Not Enough Items", ref MessageErrorNotEnoughSell);
            CheckCfg("Message - Error - Item Doesnt Exist", ref MessageErrorItemNoExist);
            CheckCfg("Message - Error - No Chat Shop", ref MessageErrorNPCRange);
            if (configChanged) SaveConfig();
            LoadData();
        }

        void LoadData()
        {
            cooldowns = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, double>>>(nameof(GUIShop));
        }

        void SaveData()
        {
            if (cooldowns == null) return;
            Interface.Oxide.DataFileSystem.WriteObject(nameof(GUIShop), cooldowns);
        }

        void Unload()
        {
            SaveData();
        }
        void OnServerSave()
        {
            SaveData();
        }
        void OnServerShutdown()
        {
            SaveData();
        }

        static int CurrentTime() { return Facepunch.Math.unixTimestamp; }

        //////////////////////////////////////////////////////////////////////////////////////
        // Default Shops for Tutorial purpoise ///////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        static Dictionary<string, object> DefaultShops()
        {
            var shops = new Dictionary<string, object>
            {
                {
                    "chat", new Dictionary<string, object>
                    {
                        {"buy", new List<object> {"Build Kit"}},
                        {"description", "You currently have {0} coins to spend in this builders shop"},
                        {"name", "Build"}
                    }
                },
                {
                    "5498734", new Dictionary<string, object>
                    {
                        {"description", "You currently have {0} coins to spend in this weapons shop"},
                        {"name", "Weaponsmith Shop"},
                        {"buy", new List<object> {"Assault Rifle", "Bolt Action Rifle"}},
                        {"sell", new List<object> {"Assault Rifle", "Bolt Action Rifle"}}
                    }
                },
                {
                    "1234567", new Dictionary<string, object>
                    {
                        {"description", "You currently have {0} coins to spend in this farmers market"},
                        {"name", "Fruit Market"},
                        {"buy", new List<object> {"Apple", "BlueBerries", "Assault Rifle", "Bolt Action Rifle"}},
                        {"sell", new List<object> {"Apple", "BlueBerries", "Assault Rifle", "Bolt Action Rifle"}}
                    }
                }
            };
            return shops;
        }
        static Dictionary<string, object> DefaultShopCategories()
        {
            var dsc = new Dictionary<string, object>
            {
                {
                    "Assault Rifle", new Dictionary<string, object>
                    {
                        {"item", "assault rifle"},
                        {"buy", "10"},
                        {"sell", "8"},
                        {"img", "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405105940"}
                    }
                },
                {
                    "Bolt Action Rifle", new Dictionary<string, object>
                    {
                        {"item", "bolt action rifle"},
                        {"buy", "10"}, {"sell", "8"},
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111457"}
                    }
                },
                {
                    "Build Kit", new Dictionary<string, object>
                    {
                        {"item", "kitbuild"},
                        {"buy", "10"},
                        {"sell", "8"},
                        {"img", "http://oxidemod.org/data/resource_icons/0/715.jpg?1425682952"}
                    }
                },
                {
                    "Apple", new Dictionary<string, object>
                    {
                        {"item", "apple"},
                        {"buy", "1"},
                        {"sell", "1"},
                        {"img", "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103640"}
                    }
                },
                {
                    "BlueBerries", new Dictionary<string, object>
                    {
                        {"item", "blueberries"},
                        {"buy", "1"},
                        {"sell", "1"},
                        {"img", "http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Blueberries_icon.png/revision/latest/scale-to-width-down/100?cb=20150405111338"}
                    }
                }
            };
            return dsc;
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Item Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        readonly Dictionary<string, string> displaynameToShortname = new Dictionary<string, string>();
        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            var ItemsDefinition = ItemManager.itemList;
            foreach (var itemdef in ItemsDefinition)
                displaynameToShortname.Add(itemdef.displayName.english.ToLower(), itemdef.shortname);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            playersMask = LayerMask.GetMask("Player (Server)");
        }

        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (!Shops.ContainsKey(npc.UserIDString)) return;
            ShowShop(player, npc.UserIDString, 0);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // GUI ///////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private static CuiElementContainer CreateShopOverlay(string shopname)
        {
            return new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0.1 0.1 0.1 0.8"},
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                        CursorEnabled = true
                    },
                    new CuiElement().Parent,
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = shopname, FontSize = 30, Align = TextAnchor.MiddleCenter},
                        RectTransform = {AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Item", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.2 0.6", AnchorMax = "0.4 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Buy", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.55 0.6", AnchorMax = "0.7 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiLabel
                    {
                        Text = {Text = "Sell", FontSize = 20, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = "0.75 0.6", AnchorMax = "0.9 0.65"}
                    },
                    ShopOverlayName
                },
                {
                    new CuiButton
                    {
                        Button = {Close = ShopOverlayName, Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.5 0.15", AnchorMax = "0.7 0.2"},
                        Text = {Text = "Close", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName
                }
            };
        }

        private readonly CuiLabel shopDescription = new CuiLabel
        {
            Text = { Text = "{shopdescription}", FontSize = 15, Align = TextAnchor.MiddleCenter },
            RectTransform = { AnchorMin = "0.2 0.7", AnchorMax = "0.8 0.79" }
        };

        private CuiElementContainer CreateShopItemEntry(string price, float ymax, float ymin, string shop, string item, string color, bool sell, bool cooldown)
        {
            var container = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text = {Text = price, FontSize = 15, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = $"{(sell ? 0.75 : 0.55)} {ymin}", AnchorMax = $"{(sell ? 0.78 : 0.6)} {ymax}"}
                    },
                    ShopContentName
                }
            };
            for (var i = 0; i < steps.Length; i++)
            {
                container.Add(new CuiButton
                {
                    Button = {Command = $"shop.{(sell ? "sell" : "buy")} {shop} {item} {steps[i]}", Color = color},
                    RectTransform = {AnchorMin = $"{(sell ? 0.8 : 0.6) + i*0.03} {ymin}", AnchorMax = $"{(sell ? 0.83 : 0.63) + i*0.03} {ymax}"},
                    Text = {Text = steps[i].ToString(), FontSize = 15, Align = TextAnchor.MiddleCenter}
                }, ShopContentName);
                if (cooldown) break;
            }
            return container;
        }

        private static CuiElementContainer CreateShopItemIcon(string name, float ymax, float ymin, string url)
        {
            if (string.IsNullOrEmpty(url))
                return new CuiElementContainer();
            var rawImage = new CuiRawImageComponent();
            if (url.StartsWith("http"))
            {
                rawImage.Url = url;
                rawImage.Sprite = "assets/content/textures/generic/fulltransparent.tga";
            }
            else
                rawImage.Sprite = url;
            var container = new CuiElementContainer
            {
                {
                    new CuiLabel
                    {
                        Text = {Text = name, FontSize = 15, Align = TextAnchor.MiddleLeft},
                        RectTransform = {AnchorMin = $"0.2 {ymin}", AnchorMax = $"0.4 {ymax}"}
                    },
                    ShopContentName
                },
                new CuiElement
                {
                    Parent = ShopContentName,
                    Components =
                    {
                        rawImage,
                        new CuiRectTransformComponent {AnchorMin = $"0.1 {ymin}", AnchorMax = $"0.13 {ymax}"}
                    }
                }
            };
            return container;
        }

        private static CuiElementContainer CreateShopChangePage(string currentshop, int shoppageminus, int shoppageplus)
        {
            return new CuiElementContainer
            {
                {
                    new CuiButton
                    {
                        Button = {Command = $"shop.show {currentshop} {shoppageminus}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.2 0.15", AnchorMax = "0.3 0.2"},
                        Text = {Text = "<<", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName,
                    "ButtonBack"
                },
                {
                    new CuiButton
                    {
                        Button = {Command = $"shop.show {currentshop} {shoppageplus}", Color = "0.5 0.5 0.5 0.2"},
                        RectTransform = {AnchorMin = "0.35 0.15", AnchorMax = "0.45 0.2"},
                        Text = {Text = ">>", FontSize = 20, Align = TextAnchor.MiddleCenter}
                    },
                    ShopOverlayName,
                    "ButtonForward"
                }
            };
        }

        readonly Hash<ulong, int> shopPage = new Hash<ulong, int>();
        private Dictionary<ulong, Dictionary<string, double>> cooldowns;
        private bool configChanged;

        void ShowShop(BasePlayer player, string shopid, int from = 0, bool fullPaint = true, bool refreshMoney = false)
        {
            shopPage[player.userID] = from;
            object shopObj;
            if (!Shops.TryGetValue(shopid, out shopObj))
            {
                SendReply(player, MessageErrorNoShop);
                return;
            }
            if (Economics == null)
            {
                SendReply(player, MessageShowNoEconomics);
                return;
            }
            var playerCoins = (double) Economics.CallHook("GetPlayerMoney", player.userID);

            var shop = (Dictionary<string, object>) shopObj;

            shopDescription.Text.Text = string.Format((string) shop["description"], playerCoins);

            if (refreshMoney)
            {
                CuiHelper.DestroyUi(player, ShopDescOverlay);
                CuiHelper.AddUi(player, new CuiElementContainer { { shopDescription, ShopOverlayName, ShopDescOverlay } });
                return;
            }
            CuiElementContainer container;
            if (fullPaint)
            {
                DestroyUi(player, true);
                container = CreateShopOverlay((string) shop["name"]);
                container.Add(shopDescription, ShopOverlayName, ShopDescOverlay);
            }
            else
            {
                DestroyUi(player);
                container = new CuiElementContainer();
            }
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.2", AnchorMax = "1 0.6" }
            }, ShopOverlayName, ShopContentName);
            if (from < 0)
            {
                CuiHelper.AddUi(player, container);
                return;
            }

            var itemslist = new Dictionary<string, Dictionary<string, bool>>();
            object type;
            if (shop.TryGetValue("sell", out type))
            {
                foreach (string itemname in (List<object>)type)
                {
                    Dictionary<string, bool> itemEntry;
                    if (!itemslist.TryGetValue(itemname, out itemEntry))
                        itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                    itemEntry["sell"] = true;
                }
            }
            if (shop.TryGetValue("buy", out type))
            {
                foreach (string itemname in (List<object>)type)
                {
                    Dictionary<string, bool> itemEntry;
                    if (!itemslist.TryGetValue(itemname, out itemEntry))
                        itemslist[itemname] = itemEntry = new Dictionary<string, bool>();
                    itemEntry["buy"] = true;
                }
            }
            var current = 0;
            foreach (var pair in itemslist)
            {
                object data;
                if (!ShopCategories.TryGetValue(pair.Key, out data)) continue;

                if (current >= from && current < from + 7)
                {
                    var itemdata = (Dictionary<string, object>) data;
                    var pos = 0.85f - 0.125f * (current - from);

                    var cooldown = itemdata.ContainsKey("cooldown") && Convert.ToDouble(itemdata["cooldown"]) > 0;
                    var name = pair.Key;
                    if (cooldown)
                        name += $" ({FormatTime((long)Convert.ToDouble(itemdata["cooldown"]))})";
                    container.AddRange(CreateShopItemIcon(name, pos + 0.125f, pos, (string) itemdata["img"]));
                    var buyed = false;
                    if (cooldown)
                    {
                        Dictionary<string, double> itemCooldowns;
                        double itemCooldown;
                        if (cooldowns.TryGetValue(player.userID, out itemCooldowns)
                            && itemCooldowns.TryGetValue(pair.Key, out itemCooldown)
                            && itemCooldown > CurrentTime())
                        {
                            buyed = true;
                            container.Add(new CuiLabel
                            {
                                Text = {Text = (string)itemdata["buy"], FontSize = 15, Align = TextAnchor.MiddleLeft},
                                RectTransform = {AnchorMin = $"0.55 {pos}", AnchorMax = $"0.6 {pos + 0.125f}"}
                            }, ShopContentName);
                            container.Add(new CuiLabel
                            {
                                Text = {Text = FormatTime((long)(itemCooldown - CurrentTime())), FontSize = 15, Align = TextAnchor.MiddleLeft},
                                RectTransform = {AnchorMin = $"0.6 {pos}", AnchorMax = $"0.7 {pos + 0.125f}"}
                            }, ShopContentName);
                            //current++;
                            //continue;
                        }
                    }
                    if (!buyed && pair.Value.ContainsKey("buy"))
                        container.AddRange(CreateShopItemEntry((string)itemdata["buy"], pos + 0.125f, pos, $"'{shopid}'", $"'{pair.Key}'", "0 0.6 0 0.1", false, cooldown));
                    if (pair.Value.ContainsKey("sell"))
                        container.AddRange(CreateShopItemEntry((string)itemdata["sell"], pos + 0.125f, pos, $"'{shopid}'", $"'{pair.Key}'", "1 0 0 0.1", true, cooldown));
                }
                current++;
            }
            var minfrom = from <= 7 ? 0 : from - 7;
            var maxfrom = from + 7 >= current ? from : from + 7;
            container.AddRange(CreateShopChangePage(shopid, minfrom, maxfrom));
            CuiHelper.AddUi(player, container);
        }

        string FormatTime(long seconds)
        {
            var timespan = TimeSpan.FromSeconds(seconds);
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, Math.Floor(timespan.TotalHours));
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Shop Functions ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object CanDoAction(BasePlayer player, string shop, string item, string ttype)
        {
            var shopdata = (Dictionary<string, object>) Shops[shop];
            if (!shopdata.ContainsKey(ttype))
                return string.Format(MessageErrorNoActionShop, ttype);
            var actiondata = (List<object>) shopdata[ttype];
            if (!actiondata.Contains(item))
                return string.Format(MessageErrorNoActionItem, ttype);
            return true;
        }

        bool CanFindNPC(Vector3 pos, string npcid)
        {
            return Physics.OverlapSphere(pos, 3f, playersMask).Select(col => col.GetComponentInParent<BasePlayer>()).Any(player => player != null && player.UserIDString == npcid);
        }

        object CanShop(BasePlayer player, string shopname)
        {
            if (!Shops.ContainsKey(shopname)) return MessageErrorNoShop;
            if (shopname != "chat" && !CanFindNPC(player.transform.position, shopname))
                return MessageErrorNoNPC;
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Buy Functions /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object TryShopBuy(BasePlayer player, string shop, string item, int amount)
        {
            object success = CanShop(player, shop);
            if (success is string) return success;
            success = CanDoAction(player, shop, item, "buy");
            if (success is string) return success;
            success = CanBuy(player, item, amount);
            if (success is string) return success;
            success = TryGive(player, item, amount);
            if (success is string) return success;
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (itemdata.ContainsKey("cooldown"))
            {
                var cooldown = Convert.ToDouble(itemdata["cooldown"]);
                if (cooldown > 0)
                {
                    Dictionary<string, double> itemCooldowns;
                    if (!cooldowns.TryGetValue(player.userID, out itemCooldowns))
                        cooldowns[player.userID] = itemCooldowns = new Dictionary<string, double>();
                    itemCooldowns[item] = CurrentTime() + cooldown * amount;
                }
            }
            return Economics?.CallHook("Withdraw", player.userID, Convert.ToDouble(itemdata["buy"]) * amount);
        }
        object TryGive(BasePlayer player, string item, int amount)
        {
            var itemdata = (Dictionary<string, object>) ShopCategories[item];

            if(itemdata.ContainsKey("cmd"))
            {
                var cmds = ((List<object>) itemdata["cmd"]).ConvertAll(c => c.ToString());
                for (var i = 0; i < cmds.Count; i++)
                {
                    var c = cmds[i]
                        .Replace("$player.id", player.UserIDString)
                        .Replace("$player.name", player.displayName)
                        .Replace("$player.x", player.transform.position.x.ToString())
                        .Replace("$player.y", player.transform.position.y.ToString())
                        .Replace("$player.z", player.transform.position.z.ToString());
                    if (c.StartsWith("shop.show close", StringComparison.OrdinalIgnoreCase))
                        NextTick(() => ConsoleSystem.Run.Server.Normal(c));
                    else
                        ConsoleSystem.Run.Server.Normal(c);
                }
                Puts("Player: {0} Buyed command: {1}", player.displayName, item);
            }
            if (itemdata.ContainsKey("item"))
            {
                string itemname = itemdata["item"].ToString();
                object iskit = Kits?.CallHook("isKit", itemname);

                if (iskit is bool && (bool)iskit)
                {
                    object successkit = Kits.CallHook("GiveKit", player, itemname);
                    if (successkit is bool && !(bool)successkit) return MessageErrorRedeemKit;
                    return true;
                }
                object success = GiveItem(player, itemname, amount, player.inventory.containerMain);
                if (success is string) return success;
            }
            return true;
        }

        private object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref)
        {
            if (pref.IsFull()) return MessageErrorInventoryFull;
            itemname = itemname.ToLower();

            bool isBP = false;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null) return MessageErrorItemNoExist;
            int stack = definition.stackable;
            if (isBP)
                stack = 1;
            if (stack < 1) stack = 1;
            if (pref.itemList.Count + Math.Ceiling(amount / (float)stack) > pref.capacity)
                return string.Format(MessageErrorInventorySlots, Math.Ceiling(amount / (float)stack));
            for (var i = amount; i > 0; i = i - stack)
            {
                var giveamount = i >= stack ? stack : i;
                if (giveamount < 1) return true;
                var item = ItemManager.CreateByItemID(definition.itemid, giveamount, isBP);
                if (!player.inventory.GiveItem(item, pref))
                    item.Remove(0);
            }
            return true;
        }
        object CanBuy(BasePlayer player, string item, int amount)
        {
            if (Economics == null) return MessageShowNoEconomics;
            var playerCoins = (double)Economics.CallHook("GetPlayerMoney", player.userID);
            if (!ShopCategories.ContainsKey(item)) return MessageErrorItemNoValid;

            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (!itemdata.ContainsKey("buy")) return MessageErrorBuyPrice;
            var buyprice = Convert.ToDouble(itemdata["buy"]);

            if (playerCoins < buyprice * amount)
                return string.Format(MessageErrorNotEnoughMoney, buyprice * amount, amount, item);
            if (itemdata.ContainsKey("cooldown"))
            {
                var cooldown = Convert.ToDouble(itemdata["cooldown"]);
                if (cooldown > 0)
                {
                    if (amount > 1)
                        return MessageErrorCooldownAmount;
                    Dictionary<string, double> itemCooldowns;
                    double itemCooldown;
                    if (cooldowns.TryGetValue(player.userID, out itemCooldowns)
                        && itemCooldowns.TryGetValue(item, out itemCooldown)
                        && itemCooldown > CurrentTime())
                    {
                        return string.Format(MessageErrorCooldown, FormatTime((long) (itemCooldown - CurrentTime())));
                    }
                }
            }
            return true;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Sell Functions ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        object TryShopSell(BasePlayer player, string shop, string item, int amount)
        {
            object success = CanShop(player, shop);
            if (success is string) return success;
            success = CanDoAction(player, shop, item, "sell");
            if (success is string) return success;
            success = CanSell(player, item, amount);
            if (success is string) return success;
            success = TrySell(player, item, amount);
            if (success is string) return success;
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            /*if (itemdata.ContainsKey("cooldown"))
            {
                var cooldown = Convert.ToDouble(itemdata["cooldown"]);
                if (cooldown > 0)
                {
                    Dictionary<string, double> itemCooldowns;
                    if (!cooldowns.TryGetValue(player.userID, out itemCooldowns))
                        cooldowns[player.userID] = itemCooldowns = new Dictionary<string, double>();
                    itemCooldowns[item] = CurrentTime() + cooldown * amount;
                }
            }*/
            Economics?.CallHook("Deposit", player.userID, Convert.ToDouble(itemdata["sell"]) * amount);
            return true;
        }
        object TrySell(BasePlayer player, string item, int amount)
        {
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (!itemdata.ContainsKey("item")) return MessageErrorItemItem;
            string itemname = itemdata["item"].ToString();
            object iskit = Kits?.CallHook("isKit", itemname);

            if (iskit is bool && (bool)iskit) return "You can't sell kits";
            object success = TakeItem(player, itemname, amount);
            if (success is string) return success;
            return true;
        }
        private object TakeItem(BasePlayer player, string itemname, int amount)
        {
            itemname = itemname.ToLower();

            var isBP = false;
            int pamount;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null) return MessageErrorItemNoExist;

            if (isBP)
            {
                var collectItems = new List<Item>();
                var allItems = player.inventory.AllItems();
                pamount = 0;
                foreach (var allItem in allItems)
                {
                    if (allItem.IsBlueprint() && allItem.info.itemid == definition.itemid && !allItem.IsBusy())
                    {
                        collectItems.Add(allItem);
                        pamount++;
                    }
                }
                if (pamount < amount) return MessageErrorNotEnoughSell;
                for (var i = 0; i < amount; i++)
                    collectItems[i].RemoveFromContainer();
            }
            else
            {
                pamount = player.inventory.GetAmount(definition.itemid);
                if (pamount < amount) return MessageErrorNotEnoughSell;
                player.inventory.Take(null, definition.itemid, amount);
            }
            return true;
        }
        object CanSell(BasePlayer player, string item, int amount)
        {
            if (!ShopCategories.ContainsKey(item)) return MessageErrorItemNoValid;
            var itemdata = (Dictionary<string, object>) ShopCategories[item];
            if (!itemdata.ContainsKey("sell")) return MessageErrorSellPrice;
            if (false && itemdata.ContainsKey("cooldown"))
            {
                var cooldown = Convert.ToDouble(itemdata["cooldown"]);
                if (cooldown > 0)
                {
                    if (amount > 1)
                        return MessageErrorCooldownAmount;
                    Dictionary<string, double> itemCooldowns;
                    double itemCooldown;
                    if (cooldowns.TryGetValue(player.userID, out itemCooldowns)
                        && itemCooldowns.TryGetValue(item, out itemCooldown)
                        && itemCooldown > CurrentTime())
                    {
                        return string.Format(MessageErrorCooldown, FormatTime((long)(itemCooldown - CurrentTime())));
                    }
                }
            }
            return true;
        }

        void DestroyUi(BasePlayer player, bool full = false)
        {
            CuiHelper.DestroyUi(player, ShopContentName);
            CuiHelper.DestroyUi(player, "ButtonForward");
            CuiHelper.DestroyUi(player, "ButtonBack");
            if (!full) return;
            CuiHelper.DestroyUi(player, ShopDescOverlay);
            CuiHelper.DestroyUi(player, ShopOverlayName);
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("shop")]
        void cmdShop(BasePlayer player, string command, string[] args)
        {
            if(!Shops.ContainsKey("chat"))
            {
                SendReply(player, MessageErrorNPCRange);
                return;
            }
            ShowShop(player, "chat");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Console Commands //////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("shop.show")]
        void ccmdShopShow(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(2)) return;
            var shopid = arg.GetString(0).Replace("'", "");
            if (shopid.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                var targetPlayer = arg.GetPlayerOrSleeper(1);
                DestroyUi(targetPlayer, true);
                return;
            }
            var player = arg.Player();
            if (player == null) return;
            var shoppage = arg.GetInt(1);
            ShowShop(player, shopid, shoppage, false);
        }

        [ConsoleCommand("shop.buy")]
        void ccmdShopBuy(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3)) return;
            var player = arg.Player();
            if (player == null) return;
            object success = Interface.Oxide.CallHook("canShop", player);
            if(success != null)
            {
                SendReply(player, success as string ?? "You are not allowed to shop at the moment");
                return;
            }

            string shop = arg.Args[0].Replace("'", "");
            string item = arg.Args[1].Replace("'", "");
            int amount = arg.GetInt(2);
            success = TryShopBuy(player, shop, item, amount);
            if(success is string)
            {
                SendReply(player, (string)success);
                return;
            }
            SendReply(player, string.Format(MessageBought, amount, item));
            ShowShop(player, shop, shopPage[player.userID], false, true);
        }
        [ConsoleCommand("shop.sell")]
        void ccmdShopSell(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs(3)) return;
            var player = arg.Player();
            if (player == null) return;
            object success = Interface.Oxide.CallHook("canShop", player);
            if (success != null)
            {
                string message = "You are not allowed to shop at the moment";
                if (success is string)
                    message = (string)success;
                SendReply(player, message);
                return;
            }
            string shop = arg.Args[0].Replace("'", "");
            string item = arg.Args[1].Replace("'", "");
            int amount = Convert.ToInt32(arg.Args[2]);
            success = TryShopSell(player, shop, item, amount);
            if (success is string)
            {
                SendReply(player, (string)success);
                return;
            }
            SendReply(player, string.Format(MessageSold, amount, item));
            ShowShop(player, shop, shopPage[player.userID], false, true);
        }
    }
}
