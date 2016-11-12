using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core;
using System.Collections;
using System.IO;

namespace Oxide.Plugins
{
    [Info("AbsolutMarket", "Absolut", "1.3.0", ResourceId = 2118)]

    class AbsolutMarket : RustPlugin
    {

        [PluginReference]
        Plugin ServerRewards;

        static GameObject webObject;
        static UnityImages uImage;
        static UnityBackgrounds uBackground;

        MarketData mData;
        private DynamicConfigFile MData;

        AMImages imgData;
        private DynamicConfigFile IMGData;

        Backgrounds bkData;
        private DynamicConfigFile BKData;

        class Backgrounds
        {
            public Dictionary<string, string> PendingBackgrounds = new Dictionary<string, string>();
        }

        string TitleColor = "<color=orange>";
        string MsgColor = "<color=#A9A9A9>";

        private List<ulong> MenuState = new List<ulong>();
        private List<ulong> SettingBox = new List<ulong>();
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private Dictionary<ulong, List<AMItem>> PlayerBoxContents = new Dictionary<ulong, List<AMItem>>();
        private Dictionary<ulong, AMItem> SalesItemPrep = new Dictionary<ulong, AMItem>();
        private Dictionary<ulong, List<Item>> PlayerInventory = new Dictionary<ulong, List<Item>>();
        private Dictionary<ulong, List<Item>> TransferableItems = new Dictionary<ulong, List<Item>>();
        private Dictionary<ulong, List<Item>> ItemsToTransfer = new Dictionary<ulong, List<Item>>();
        private Dictionary<ulong, bool> PlayerPurchaseApproval = new Dictionary<ulong, bool>();
        private Dictionary<ulong, AMItem> SaleProcessing = new Dictionary<ulong, AMItem>();

        #region Server Hooks

        void Loaded()
        {
            MData = Interface.Oxide.DataFileSystem.GetFile("AbsolutMarket_Data");
            IMGData = Interface.Oxide.DataFileSystem.GetFile("AbsolutMarket_Images");
            BKData = Interface.Oxide.DataFileSystem.GetFile("AbsolutMarket_AddBackgrounds");
            lang.RegisterMessages(messages, this);
        }

        void Unload()
        {
            foreach (var entry in timers)
                entry.Value.Destroy();
            MenuState.Clear();
            timers.Clear();
            PlayerBoxContents.Clear();
            SalesItemPrep.Clear();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                DestroyMarketPanel(p);
                DestroyPurchaseScreen(p);
            }
            SaveData();
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (MenuState.Contains(player.userID))
                MenuState.Remove(player.userID);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                player.Command($"bind {configData.MarketMenuKeyBinding} \"UI_ToggleMarketScreen\"");
                GetSendMSG(player, "AMInfo", configData.MarketMenuKeyBinding);
                if (!mData.names.ContainsKey(player.userID))
                    mData.names.Add(player.userID, player.displayName);
                else
                {
                    var length = player.displayName.Count();
                    if (length > 30)
                    {
                        mData.names[player.userID] = player.displayName.Substring(0, 30);
                    }
                    else mData.names[player.userID] = player.displayName;
                }
                SendMessages(player);
            }
        }

        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            uImage = webObject.AddComponent<UnityImages>();
            uImage.SetDataDir(this);
            uBackground = webObject.AddComponent<UnityBackgrounds>();
            uBackground.SetDataDir(this);
            LoadVariables();
            LoadData();
            timers.Add("info", timer.Once(configData.InfoInterval, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            SaveData();
            if (imgData.SavedImages == null || imgData.SavedImages.Count == 0)
                Getimages();
            else Refreshimages();
            //if (imgData.SavedBackgrounds == null || imgData.SavedBackgrounds.Count == 0)
                //GetBackgrounds();
           // else
            RefreshBackgrounds();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerInit(p);
        }

        #endregion

        #region Player Hooks

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (planner == null) return;
            if (gameobject.GetComponent<BaseEntity>() != null)
            {
                BaseEntity element = gameobject.GetComponent<BaseEntity>();
                var entityowner = element.OwnerID;
                var ID = element.net.ID;
                if (!SettingBox.Contains(entityowner)) return;
                if (element.PrefabName == "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab" || element.PrefabName == "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab")
                {
                    BasePlayer player = BasePlayer.FindByID(entityowner);
                    TradeBoxConfirmation(player, ID);
                }
            }
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity is StorageContainer)
            {
                uint ID = entity.net.ID;
                if (mData.TradeBox.ContainsKey(entity.OwnerID))
                {
                    if (ID == mData.TradeBox[entity.OwnerID])
                    {
                        Dictionary<uint, string> listings = new Dictionary<uint, string>();
                        foreach (var entry in mData.MarketListings.Where(kvp => kvp.Value.seller == entity.OwnerID))
                            listings.Add(entry.Key, entry.Value.shortname);
                        foreach (var entry in listings)
                            RemoveListing(entity.OwnerID, entry.Value, entry.Key, "TradeBoxDestroyed");
                        listings.Clear();
                        mData.TradeBox.Remove(entity.OwnerID);
                        BasePlayer owner = BasePlayer.FindByID(entity.OwnerID);
                        if (BasePlayer.activePlayerList.Contains(owner))
                            GetSendMSG(owner, "TradeBoxDestroyed");
                    }
                    SaveData();
                }
                return;
            }
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;
            bool isPreping = false;
            AMItem item;
            var args = string.Join(" ", arg.Args);
            if (SalesItemPrep.ContainsKey(player.userID))
            {
                isPreping = true;
            }
            if (SettingBox.Contains(player.userID))
            {
                if (args.Contains("quit"))
                {
                    SettingBox.Remove(player.userID);
                    GetSendMSG(player, "ExitedBoxMode");
                    return true;
                }
            }
            if (isPreping)
            {
                item = SalesItemPrep[player.userID];
                if (args.Contains("quit"))
                {
                    CancelListing(player);
                    return true;
                }
                switch (item.stepNum)
                {
                    case 0:
                        var name = string.Join(" ", arg.Args);
                        item.name = name;
                        item.stepNum = 99;
                        SellItems(player, 1);
                        return true;
                }
            }
            return null;
        }



        #endregion

        #region Functions

        private StorageContainer GetTradeBox(ulong Buyer)
        {
            List<StorageContainer> Containers = new List<StorageContainer>();
            if (mData.TradeBox.ContainsKey(Buyer))
            {
                uint ID = mData.TradeBox[Buyer];
                foreach (StorageContainer Cont in StorageContainer.FindObjectsOfType<StorageContainer>())
                {
                    if (Cont.net.ID == ID)
                        return Cont;
                }
            }
            return null;
        }

        private bool GetTradeBoxContents(BasePlayer player)
        {
            if (player == null) return false;
            ulong seller = player.userID;
            if (GetTradeBox(seller) != null)
            {
                StorageContainer box = GetTradeBox(seller);
                if (GetItems(box.inventory).Count() == 0)
                {
                    if (!ServerRewards || !configData.ServerRewards || mData.Blacklist.Contains("SR"))
                    {
                        GetSendMSG(player, "TradeBoxEmpty");
                        return false;
                    }
                    else
                        if (CheckPoints(player.userID) is int)
                        if ((int)CheckPoints(player.userID) < 1)
                        {
                            GetSendMSG(player, "TradeBoxEmptyNoSR");
                            return false;
                        }
                }
                if (PlayerBoxContents.ContainsKey(seller)) PlayerBoxContents.Remove(seller);
                PlayerBoxContents.Add(seller, new List<AMItem>());
                PlayerBoxContents[seller].AddRange(GetItems(box.inventory));
                var bl = 0;
                var c = 0;
                var listed = 0;
                foreach (var entry in PlayerBoxContents[seller])
                {
                    c++;
                    if (mData.Blacklist.Contains(entry.shortname))
                        bl++;
                    if (mData.MarketListings.ContainsKey(entry.ID))
                        listed++;
                    foreach (var cat in imgData.SavedImages)
                        foreach (var item in cat.Value)
                        {
                            if (item.Key == entry.shortname)
                            {
                                entry.cat = cat.Key;
                                break;
                            }
                        }
                }
                if (bl == c)
                {
                    if (!ServerRewards || !configData.ServerRewards || mData.Blacklist.Contains("SR"))
                    {
                        GetSendMSG(player, "AllItemsAreBL");
                        return false;
                    }
                    else
                    if (CheckPoints(player.userID) is int)
                        if ((int)CheckPoints(player.userID) < 1)
                        {
                            GetSendMSG(player, "AllItemsAreBLNoSR");
                            return false;
                        }
                }
                if (c == listed)
                {
                    if (!ServerRewards || !configData.ServerRewards || mData.Blacklist.Contains("SR"))
                    {
                        GetSendMSG(player, "AllItemsAreListed");
                        return false;
                    }
                    else
                    if (CheckPoints(player.userID) is int)
                        if ((int)CheckPoints(player.userID) < 1)
                        {
                            GetSendMSG(player, "AllItemsAreListedNoSR");
                            return false;
                        }
                }
                return true;
            }
            else GetSendMSG(player, "NoTradeBox"); return false;
        }

        private bool BoxCheck(BasePlayer player, uint item)
        {
            if (player == null) return false;
            ulong seller = player.userID;
            if (GetTradeBox(seller) != null)
            {
                StorageContainer box = GetTradeBox(seller);
                if (GetItems(box.inventory).Count() == 0)
                {
                    GetSendMSG(player, "TradeBoxEmpty");
                    return false;
                }
                foreach (var entry in box.inventory.itemList)
                {
                    if (entry.uid == item)
                        return true;
                }
                return false;
            }
            else GetSendMSG(player, "NoTradeBox"); return false;
        }

        private void AddMessages(ulong player, string message, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
        {
            try
            {
                BasePlayer Online = BasePlayer.FindByID(player);
                if (BasePlayer.activePlayerList.Contains(Online))
                    GetSendMSG(Online, message, GetLang(arg1), GetLang(arg2), GetLang(arg3), GetLang(arg4));
            }
            catch
            {
                if (!mData.OutstandingMessages.ContainsKey(player))
                    mData.OutstandingMessages.Add(player, new List<Unsent>());
                mData.OutstandingMessages[player].Add(new Unsent { message = message, arg1 = arg1, arg2 = arg2, arg3 = arg3, arg4 = arg4 });
                SaveData();
            }
        }

        private void SendMessages(BasePlayer player)
        {
            if (mData.OutstandingMessages.ContainsKey(player.userID))
            {
                foreach (var entry in mData.OutstandingMessages[player.userID])
                {
                    GetSendMSG(player, entry.message, GetLang(entry.arg1), GetLang(entry.arg2), GetLang(entry.arg3), GetLang(entry.arg4));
                }
                mData.OutstandingMessages.Remove(player.userID);
            }
        }

        private IEnumerable<AMItem> GetItems(ItemContainer container)
        {
            return container.itemList.Select(item => new AMItem
            {
                amount = item.amount,
                skin = item.skin,
                cat = Category.None,
                pricecat = Category.None,
                shortname = item.info.shortname,
                condition = item.condition,
                ID = item.uid,
            });
        }

        private IEnumerable<Item> GetItemsOnly(ItemContainer container)
        {
            return container.itemList;
        }

        private void XferPurchase(ulong buyer, uint ID, ItemContainer from, ItemContainer to)
        {
            foreach (Item item in from.itemList.Where(kvp => kvp.uid == ID))
                ItemsToTransfer[buyer].Add(item);
            //item.MoveToContainer(to);
        }

        private void XferCost(Item item, BasePlayer player, uint Listing)
        {
            //Puts("Starting");
            ItemContainer from = player.inventory.containerMain;
            if (player.inventory.containerBelt.itemList.Contains(item))
            {
                from = player.inventory.containerBelt;
                //Puts($"{from} belt");
            }
            else if (player.inventory.containerWear.itemList.Contains(item))
            {
                from = player.inventory.containerWear;
                //Puts($"{from} wear");
            }
            else
                //Puts($"{from} main");
            if (mData.MarketListings[Listing].priceAmount > 0)
            {
                //Puts("TRying");
                foreach (Item item1 in from.itemList.Where(kvp => kvp == item))
                {
                    //Puts("Item found)");
                    if (mData.MarketListings[Listing].priceAmount >= item1.amount)
                    {
                        //Puts("1");
                        ItemsToTransfer[player.userID].Add(item1);
                        mData.MarketListings[Listing].priceAmount -= item1.amount;
                        //Puts($"{item1} moved... price amount: {mData.MarketListings[Listing].priceAmount} item amount:{item1.amount}");
                    }
                    else
                    {
                        Item item2 = item1.SplitItem(mData.MarketListings[Listing].priceAmount);
                        ItemsToTransfer[player.userID].Add(item2);
                        mData.MarketListings[Listing].priceAmount = 0;
                        //Puts($"SPLITTING: {item2} moved... price amount: {mData.MarketListings[Listing].priceAmount} item amount:{item2.amount}");
                    }
                    break;
                }
            }
        }

        private Item BuildCostItems(string shortname, int amount)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item1 = ItemManager.Create(definition, amount, 0);
                if (item1 != null)
                    return item1;
            }
            Puts("Error making purchase cost item(s)");
            return null;
        }

        void OnItemRemovedFromContainer(ItemContainer cont, Item item)
        {
            if (cont.entityOwner != null)
                if (mData.TradeBox.ContainsValue(cont.entityOwner.net.ID))
                    if (mData.TradeBox.ContainsKey(cont.entityOwner.OwnerID))
                        if (mData.TradeBox[cont.entityOwner.OwnerID] == cont.entityOwner.net.ID)
                            if (mData.MarketListings.ContainsKey(item.uid))
                            {
                                var name = "";
                                if (configData.UseUniqueNames && item.name != "")
                                    name = mData.MarketListings[item.uid].name;
                                else name = mData.MarketListings[item.uid].shortname;
                                RemoveListing(cont.entityOwner.OwnerID, name, item.uid, "FromBox");
                                mData.MarketListings.Remove(item.uid);
                            }
        }

        private void RemoveListing(ulong seller, string name, uint ID, string reason = "")
        {
            AddMessages(seller, "ItemRemoved", name.ToUpper(), reason);
            mData.MarketListings.Remove(ID);
        }

        private void CancelListing(BasePlayer player)
        {
            DestroyMarketPanel(player);
            if (SalesItemPrep.ContainsKey(player.userID))
                SalesItemPrep.Remove(player.userID);
            if (PlayerBoxContents.ContainsKey(player.userID))
                PlayerBoxContents.Remove(player.userID);
            GetSendMSG(player, "ItemListingCanceled");
        }

        private void SRAction(ulong ID, int amount, string action)
        {
            if (action == "ADD")
                ServerRewards?.Call("AddPoints", new object[] { ID, amount });
            if (action == "REMOVE")
                ServerRewards?.Call("TakePoints", new object[] { ID, amount });
        }

        private object CheckPoints(ulong ID) => ServerRewards?.Call("CheckPoints", ID);

        private void NumberPad(BasePlayer player, string cmd)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            var element = UI.CreateElementContainer(PanelMarket, UIColors["dark"], "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelMarket, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], GetLang("Select Amount"), 20, "0.1 0.85", "0.9 .98", TextAnchor.UpperCenter);
            var n = 1;
            var i = 0;
            while (n < 10)
            {
                CreateNumberPadButton(ref element, PanelMarket, i, n, cmd); i++; n++;
            }
            while (n >= 10 && n < 25)
            {
                CreateNumberPadButton(ref element, PanelMarket, i, n, cmd); i++; n += 5;
            }
            while (n >= 25 && n < 200)
            {
                CreateNumberPadButton(ref element, PanelMarket, i, n, cmd); i++; n += 25;
            }
            while (n >= 200 && n <= 950)
            {
                CreateNumberPadButton(ref element, PanelMarket, i, n, cmd); i++; n += 50;
            }
            while (n >= 1000 && n <= 10000)
            {
                CreateNumberPadButton(ref element, PanelMarket, i, n, cmd); i++; n += 500;
            }
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Quit"), 10, "0.03 0.02", "0.13 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), Category.All)}");
            CuiHelper.AddUi(player, element);
        }

        private void CreateNumberPadButton(ref CuiElementContainer container, string panelName, int i, int number, string command)
        {
            var pos = CalcNumButtonPos(i);
            UI.CreateButton(ref container, panelName, UIColors["buttonbg"], number.ToString(), 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"{command} {number}");
        }

        private string GetLang(string msg)
        {
            if (messages.ContainsKey(msg))
                return lang.GetMessage(msg, this);
            else return msg;
        }

        private void GetSendMSG(BasePlayer player, string message, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3, arg4);
            SendReply(player, TitleColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + MsgColor + msg + "</color>");
        }

        private string GetMSG(string message, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3, arg4);
            return msg;
        }

        public void DestroyMarketPanel(BasePlayer player)
        {
            if (MenuState.Contains(player.userID))
                MenuState.Remove(player.userID);
            CuiHelper.DestroyUi(player, PanelMarket);
        }

        public void DestroyPurchaseScreen(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchase);
        }

        private void ToggleMarketScreen(BasePlayer player)
        {
            if (MenuState.Contains(player.userID))
            {
                MenuState.Remove(player.userID);
                DestroyMarketPanel(player);
                DestroyPurchaseScreen(player);
                return;
            }
            MenuState.Add(player.userID);
            MarketMainScreen(player, 0, Category.All);
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }

        #endregion

        #region UI Creation

        private string PanelMarket = "PanelMarket";
        private string PanelPurchase = "PanelPurchase";

        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                var NewElement = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = color},
                        RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                        CursorEnabled = cursor
                    },
                    new CuiElement().Parent,
                    panel
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer element, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                element.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer element, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            static public void CreateButton(ref CuiElementContainer element, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                element.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            static public void LoadImage(ref CuiElementContainer element, string panel, string png, string aMin, string aMax)
            {
                element.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer element, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                //if (configdata.DisableUI_FadeIn)
                //    fadein = 0;
                element.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"black", "0 0 0 1.0" },
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "1 1 1 0.3" },
            {"light", ".564 .564 .564 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"limegreen", "0.42 1.0 0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"white", "1 1 1 1" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"CSorange", "1.0 0.64 0.10 1.0" }
        };

        private Dictionary<string, string> TextColors = new Dictionary<string, string>
        {
            {"limegreen", "<color=#6fff00>" }
        };

        #endregion

        #region UI Panels

        void MarketMainScreen(BasePlayer player, int page = 0, Category cat = Category.All)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            string purchaseimage = imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString();
            string priceitemimage = imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString();
            string Background = imgData.SavedBackgrounds["NEVERDELETE"].ToString();
            if (!mData.mode.ContainsKey(player.userID))
                mData.mode.Add(player.userID, false);
            var i = 0;
            var c = 0;
            bool seller = false;
            double count = 0;
            if (cat == Category.All)
                count = mData.MarketListings.Count();
            else count = mData.MarketListings.Where(kvp => kvp.Value.cat == cat).Count();
            var element = UI.CreateElementContainer(PanelMarket, "0 0 0 0", "0.2 0.15", "0.8 0.85", true);
            UI.CreatePanel(ref element, PanelMarket, "0 0 0 0", "0 0", "1 1");
            int entriesallowed = 9;
            double remainingentries = count - (page * (entriesallowed - 1));
            double totalpages = (Math.Floor(count / (entriesallowed - 1)));
            if (mData.mode[player.userID] == false)
            {
                if (mData.background.ContainsKey(player.userID))
                    if (imgData.SavedBackgrounds.ContainsKey(mData.background[player.userID]))
                        if (imgData.SavedBackgrounds[mData.background[player.userID]].ToString() != mData.background[player.userID])
                            Background = imgData.SavedBackgrounds[mData.background[player.userID]].ToString();
                UI.LoadImage(ref element, PanelMarket, Background, "0 0", "1 1");
                if (page <= totalpages - 1)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["LAST"][0].ToString(), "0.8 0.02", "0.85 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 18, "0.8 0.02", "0.85 0.075", $"UI_MarketMainScreen {totalpages} {Enum.GetName(typeof(Category), cat)}");
                }
                if (remainingentries > entriesallowed)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["NEXT"][0].ToString(), "0.74 0.02", "0.79 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 18, "0.74 0.02", "0.79 0.075", $"UI_MarketMainScreen {page + 1} {Enum.GetName(typeof(Category), cat)}");
                }
                if (page > 0)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["BACK"][0].ToString(), "0.68 0.02", "0.73 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 18, "0.68 0.02", "0.73 0.075", $"UI_MarketMainScreen {page - 1} {Enum.GetName(typeof(Category), cat)}");
                }
                if (page > 1)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["FIRST"][0].ToString(), "0.62 0.02", "0.67 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 18, "0.62 0.02", "0.67 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), cat)}");
                }

                //UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("Filters"), 22, "0.14 0.08", "0.24 0.14");
                foreach (Category ct in Enum.GetValues(typeof(Category)))
                {
                    var loc = FilterButton(c);
                    if (ct != Category.Extra && ct != Category.None)
                    {
                        if (cat == ct)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["UFILTER"][0].ToString(), $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}");
                            UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], Enum.GetName(typeof(Category), ct), 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", TextAnchor.MiddleCenter);
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), ct)}");
                            c++;
                        }
                        else
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["OFILTER"][0].ToString(), $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}");
                            UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], Enum.GetName(typeof(Category), ct), 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", TextAnchor.MiddleCenter);
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), ct)}");
                            c++;
                        }
                    }
                }
                UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.Building]["box.wooden.large"][0].ToString(), $"0.05 0.9", "0.15 1");
                UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], "", 12, $"0.05 0.9", "0.15 1", TextAnchor.MiddleCenter);
                if (!SettingBox.Contains(player.userID))
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", GetLang("TradeBoxAssignment"), 12, $"0.05 0.9", "0.15 1", $"UI_SetBoxMode");
                else
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", GetLang("TradeBoxAssignment"), 12, $"0.05 0.9", "0.15 1", $"UI_CancelTradeBox");



                UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["SELL"][0].ToString(), $"0.35 0.9", "0.65 1.0");
                UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("ListItem"), 12, $"0.35 0.9", "0.65 1.0", TextAnchor.MiddleCenter);
                UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"0.35 0.9", "0.65 1.0", $"UI_MarketSellScreen {0}");

                if (mData.mode[player.userID] == false)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["OFILTER"][0].ToString(), "0.66 0.9", "0.75 1");
                    UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("ChangeMode"), 12, "0.66 0.9", "0.75 1", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, "0.66 0.9", "0.75 1", $"UI_Mode {1}");
                }
                else
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["UFILTER"][0].ToString(), "0.66 0.9", "0.75 1");
                    UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("ChangeMode"), 12, "0.66 0.9", "0.75 1", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, "0.66 0.9", "0.75 1", $"UI_Mode {0}");
                }

                UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["OFILTER"][0].ToString(), "0.76 0.9", "0.86 1");
                UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("ChangeTheme"), 12, "0.76 0.9", "0.86 1", TextAnchor.MiddleCenter);
                UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, "0.76 0.9", "0.86 1", $"UI_MarketBackgroundMenu {0}");

                if (isAuth(player))
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["UFILTER"][0].ToString(), "0.87 0.9", "0.97 1");
                    UI.CreateLabel(ref element, PanelMarket, UIColors["dark"], GetLang("AdminPanel"), 12, "0.87 0.9", "0.97 1", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, "0.87 0.9", "0.97 1", $"UI_AdminPanel");
                }
                int shownentries = page * entriesallowed;
                int n = 0;
                if (cat == Category.All)
                {
                    foreach (var item in mData.MarketListings)
                    {
                        seller = false;
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            if (item.Value.cat != Category.None && item.Value.cat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.cat].ContainsKey(item.Value.shortname))
                                {
                                    if (imgData.SavedImages[item.Value.cat][item.Value.shortname].ContainsKey(item.Value.skin))
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][item.Value.skin].ToString();
                                    else
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][0].ToString();
                                }
                            }
                            if (item.Value.pricecat != Category.None && item.Value.pricecat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.pricecat].ContainsKey(item.Value.priceItemshortname))
                                {
                                    if (imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname].ContainsKey(0))
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                    else
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                }
                            }
                            if (item.Value.seller == player.userID)
                            {
                                seller = true;
                            }
                            CreateMarketListingButton(ref element, PanelMarket, item.Value, purchaseimage, priceitemimage, seller, n);

                            n++;
                        }
                    }
                }
                else
                    foreach (var item in mData.MarketListings.Where(kvp => kvp.Value.cat == cat))
                    {
                        seller = false;
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            if (item.Value.cat != Category.None && item.Value.cat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.cat].ContainsKey(item.Value.shortname))
                                {
                                    if (imgData.SavedImages[item.Value.cat][item.Value.shortname].ContainsKey(item.Value.skin))
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][item.Value.skin].ToString();
                                    else
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][0].ToString();
                                }
                            }

                            if (item.Value.pricecat != Category.None && item.Value.pricecat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.pricecat].ContainsKey(item.Value.priceItemshortname))
                                {
                                    if (imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname].ContainsKey(0))
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                    else
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                }
                            }
                            if (item.Value.seller == player.userID)
                            {
                                seller = true;
                            }
                            CreateMarketListingButton(ref element, PanelMarket, item.Value, purchaseimage, priceitemimage, seller, n);
                            n++;
                        }
                    }
            }
            else if (mData.mode[player.userID] == true)
            {
                UI.CreatePanel(ref element, PanelMarket, UIColors["dark"], "0. 0", "1 1");
                if (page <= totalpages - 1)
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("Last"), 18, "0.8 0.02", "0.85 0.075", $"UI_MarketMainScreen {totalpages} {Enum.GetName(typeof(Category), cat)}");
                }
                if (remainingentries > entriesallowed)
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("Next"), 18, "0.74 0.02", "0.79 0.075", $"UI_MarketMainScreen {page + 1} {Enum.GetName(typeof(Category), cat)}");
                }
                if (page > 0)
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("Back"), 18, "0.68 0.02", "0.73 0.075", $"UI_MarketMainScreen {page - 1} {Enum.GetName(typeof(Category), cat)}");
                }
                if (page > 1)
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("First"), 18, "0.62 0.02", "0.67 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), cat)}");
                }

                foreach (Category ct in Enum.GetValues(typeof(Category)))
                {
                    var loc = FilterButton(c);
                    if (ct != Category.Extra && ct != Category.None)
                    {
                        if (cat == ct)
                        {
                            UI.CreateButton(ref element, PanelMarket, UIColors["red"], Enum.GetName(typeof(Category), ct), 12, $"{loc[0]} {loc[1] + .02f}", $"{loc[2]} {loc[3] + .02f}", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), ct)}");
                            c++;
                        }
                        else
                        {
                            UI.CreateButton(ref element, PanelMarket, UIColors["header"], Enum.GetName(typeof(Category), ct), 12, $"{loc[0]} {loc[1] + .02f}", $"{loc[2]} {loc[3] + .02f}", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), ct)}");
                            c++;
                        }
                    }
                }
                if (!SettingBox.Contains(player.userID))
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("TradeBoxAssignment"), 12, $"0.05 0.92", "0.15 .98", $"UI_SetBoxMode");
                else
                    UI.CreateButton(ref element, PanelMarket, UIColors["red"], GetLang("TradeBoxAssignment"), 12, $"0.05 0.92", "0.15 .98", $"UI_CancelTradeBox");



                UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("ListItem"), 12, $"0.35 0.92", "0.65 .98", $"UI_MarketSellScreen {0}");

                if (mData.mode[player.userID] == false)
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("ChangeMode"), 12, "0.66 0.92", "0.75 .98", $"UI_Mode {1}");
                }
                else
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("ChangeMode"), 12, "0.66 0.92", "0.75 .98", $"UI_Mode {0}");
                }

                UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("ChangeTheme"), 12, "0.76 0.92", "0.86 .98", $"UI_MarketBackgroundMenu {0}");

                if (isAuth(player))
                {
                    UI.CreateButton(ref element, PanelMarket, UIColors["header"], GetLang("AdminPanel"), 12, "0.87 0.92", "0.97 .98", $"UI_AdminPanel");
                }
                int shownentries = page * entriesallowed;
                int n = 0;
                if (cat == Category.All)
                {
                    foreach (var item in mData.MarketListings)
                    {
                        seller = false;
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            if (item.Value.cat != Category.None && item.Value.cat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.cat].ContainsKey(item.Value.shortname))
                                {
                                    if (imgData.SavedImages[item.Value.cat][item.Value.shortname].ContainsKey(item.Value.skin))
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][item.Value.skin].ToString();
                                    else
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][0].ToString();
                                }
                            }
                            if (item.Value.pricecat != Category.None && item.Value.pricecat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.pricecat].ContainsKey(item.Value.priceItemshortname))
                                {
                                    if (imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname].ContainsKey(0))
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                    else
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                }
                            }
                            if (item.Value.seller == player.userID)
                            {
                                seller = true;
                            }
                            CreateMarketListingButtonSimple(ref element, PanelMarket, item.Value, purchaseimage, priceitemimage, seller, n);

                            n++;
                        }
                    }
                }
                else
                    foreach (var item in mData.MarketListings.Where(kvp => kvp.Value.cat == cat))
                    {
                        seller = false;
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            if (item.Value.cat != Category.None && item.Value.cat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.cat].ContainsKey(item.Value.shortname))
                                {
                                    if (imgData.SavedImages[item.Value.cat][item.Value.shortname].ContainsKey(item.Value.skin))
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][item.Value.skin].ToString();
                                    else
                                        purchaseimage = imgData.SavedImages[item.Value.cat][item.Value.shortname][0].ToString();
                                }
                            }

                            if (item.Value.pricecat != Category.None && item.Value.pricecat != Category.Extra)
                            {
                                if (imgData.SavedImages[item.Value.pricecat].ContainsKey(item.Value.priceItemshortname))
                                {
                                    if (imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname].ContainsKey(0))
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                    else
                                        priceitemimage = imgData.SavedImages[item.Value.pricecat][item.Value.priceItemshortname][0].ToString();
                                }
                            }
                            if (item.Value.seller == player.userID)
                            {
                                seller = true;
                            }
                            CreateMarketListingButtonSimple(ref element, PanelMarket, item.Value, purchaseimage, priceitemimage, seller, n);
                            n++;
                        }
                    }
            }
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Close"), 16, "0.87 0.02", "0.97 0.075", $"UI_DestroyMarketPanel");
            CuiHelper.AddUi(player, element);
        }

        private void CreateMarketListingButton(ref CuiElementContainer container, string panelName, AMItem item, string listingimg, string costimg, bool seller, int num)
        {
            var pos = MarketEntryPos(num);
            var name = item.shortname;
            if (configData.UseUniqueNames && item.name != "")
                name = item.name;
            else if (item.shortname == "SR")
                name = "SR Points";
            UI.CreatePanel(ref container, panelName, UIColors["header"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");

            //SALE ITEM
            UI.LoadImage(ref container, panelName, listingimg, $"{pos[0] + 0.001f} {pos[3] - 0.125f}", $"{pos[0] + 0.1f} {pos[3] - 0.005f}");
            UI.CreateLabel(ref container, panelName, UIColors["dark"], name, 12, $"{pos[0] + .1f} {pos[3] - .04f}", $"{pos[2] - .001f} {pos[3] - .001f}", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Amount", item.amount.ToString()), 12, $"{pos[0] + .1f} {pos[3] - .07f}", $"{pos[2] - .001f} {pos[3] - .041f}", TextAnchor.MiddleLeft);

            if (item.cat != Category.Money)
            {
                Item actual = BuildCostItems(item.shortname, 1);
                if (actual.condition != 0)
                {
                    var percent = System.Convert.ToDouble(item.condition / actual.condition);
                    var xMax = (pos[0] + .1f) + (.175f * percent);
                    var ymin = pos[3] - .11f;
                    var ymax = pos[3] - .08f;
                    UI.CreatePanel(ref container, panelName, UIColors["buttonbg"], $"{pos[0] + .1f} {ymin}", $"{pos[0] + .275f} {ymax}");
                    if (percent * 100 > 75)
                        UI.CreatePanel(ref container, panelName, UIColors["green"], $"{pos[0] + .1f} {ymin}", $"{xMax} {ymax}");
                    else if (percent * 100 > 25 && percent * 100 < 76)
                        UI.CreatePanel(ref container, panelName, UIColors["yellow"], $"{pos[0] + .1f} {ymin}", $"{xMax} {ymax}");
                    else if (percent * 100 > 0 && percent * 100 < 26)
                        UI.CreatePanel(ref container, panelName, UIColors["red"], $"{pos[0] + .1f} {ymin}", $"{xMax} {ymax}");
                    UI.CreateLabel(ref container, panelName, "1 1 1 1", GetMSG("ItemCondition", Math.Round(percent * 100).ToString()), 9, $"{pos[0] + .1f} {ymin}", $"{pos[0] + .275f} {ymax}", TextAnchor.MiddleLeft);
                }
            }

            UI.LoadImage(ref container, PanelMarket, imgData.SavedImages[Category.None]["ARROW"][0].ToString(), $"{pos[0] + .08f} {pos[1] + .07f}", $"{pos[0] + .2f} {pos[1] + .135f}");
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetLang("InExchange"), 14, $"{ pos[0] + .08f} {pos[1] + .07f}", $"{pos[0] + .2f} {pos[1] + .135f}", TextAnchor.UpperCenter);

            //COST ITEM
            if (item.priceItemshortname == "SR")
                name = "SR Points";
            else name = item.priceItemshortname;
            UI.LoadImage(ref container, panelName, costimg, $"{pos[2] - 0.125f} {pos[1] + 0.01f}", $"{pos[2] - 0.005f} {pos[1] + 0.125f}");
            UI.CreateLabel(ref container, panelName, UIColors["dark"], name, 12, $"{pos[0] + 0.005f} {pos[1] + 0.03f}", $"{pos[0] + 0.175f} {pos[1] + 0.06f}", TextAnchor.MiddleRight);
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Amount", item.priceAmount.ToString()), 12, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[0] + 0.175f} {pos[1] + 0.0299f}", TextAnchor.MiddleRight);
            if (mData.names.ContainsKey(item.seller))
                name = mData.names[item.seller];
            else name = "NONE";
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Seller", name), 12, $"{pos[0] + .001f} {pos[3] - .2f}", $"{pos[2] - .1f} {pos[3] - .14f}", TextAnchor.MiddleLeft);

            if (seller == true)
            {
                UI.LoadImage(ref container, PanelMarket, imgData.SavedImages[Category.None]["UFILTER"][0].ToString(), $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}");
                UI.CreateLabel(ref container, panelName, UIColors["dark"], GetLang("removelisting"), 10, $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}", TextAnchor.MiddleCenter);
                UI.CreateButton(ref container, panelName, "0 0 0 0", "", 40, $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}", $"UI_RemoveListing {item.ID}");
            }
            else
            {
                UI.CreateButton(ref container, panelName, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_BuyConfirm {item.ID}");
            }
        }

        private void CreateMarketListingButtonSimple(ref CuiElementContainer container, string panelName, AMItem item, string listingimg, string costimg, bool seller, int num)
        {
            var pos = MarketEntryPos(num);
            var name = item.shortname;
            if (configData.UseUniqueNames && item.name != "")
                name = item.name;
            else if (item.shortname == "SR")
                name = "SR Points";
            UI.CreatePanel(ref container, panelName, UIColors["white"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}");

            //SALE ITEM
            UI.LoadImage(ref container, panelName, listingimg, $"{pos[0] + 0.001f} {pos[3] - 0.125f}", $"{pos[0] + 0.1f} {pos[3] - 0.005f}");
            UI.CreateLabel(ref container, panelName, UIColors["dark"], name, 12, $"{pos[0] + .1f} {pos[3] - .04f}", $"{pos[2] - .001f} {pos[3] - .001f}", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Amount", item.amount.ToString()), 12, $"{pos[0] + .1f} {pos[3] - .07f}", $"{pos[2] - .001f} {pos[3] - .041f}", TextAnchor.MiddleLeft);

            if (item.cat != Category.Money)
            {
                Item actual = BuildCostItems(item.shortname, 1);
                if (actual.condition != 0)
                {
                    var percent = System.Convert.ToDouble(item.condition / actual.condition);
                    //var xMax = (pos[0] + .1f) + (.175f * percent);
                    var ymin = pos[3] - .12f;
                    var ymax = pos[3] - .07f;
                    if (percent * 100 > 75)
                    UI.CreateLabel(ref container, panelName, UIColors["green"], GetMSG("ItemCondition", Math.Round(percent * 100).ToString()), 12, $"{pos[0] + .1f} {ymin}", $"{pos[0] + .275f} {ymax}", TextAnchor.MiddleLeft);
                    else if (percent * 100 > 25 && percent * 100 < 76)
                        UI.CreateLabel(ref container, panelName, UIColors["yellow"], GetMSG("ItemCondition", Math.Round(percent * 100).ToString()), 12, $"{pos[0] + .1f} {ymin}", $"{pos[0] + .275f} {ymax}", TextAnchor.MiddleLeft);
                    else if (percent * 100 > 0 && percent * 100 < 26)
                        UI.CreateLabel(ref container, panelName, UIColors["red"], GetMSG("ItemCondition", Math.Round(percent * 100).ToString()), 12, $"{pos[0] + .1f} {ymin}", $"{pos[0] + .275f} {ymax}", TextAnchor.MiddleLeft);
                }
            }

            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetLang("InExchange"), 14, $"{ pos[0] + .08f} {pos[1] + .07f}", $"{pos[0] + .2f} {pos[1] + .135f}", TextAnchor.UpperCenter);

            //COST ITEM
            if (item.priceItemshortname == "SR")
                name = "SR Points";
            else name = item.priceItemshortname;
            UI.LoadImage(ref container, panelName, costimg, $"{pos[2] - 0.125f} {pos[1] + 0.01f}", $"{pos[2] - 0.005f} {pos[1] + 0.125f}");
            UI.CreateLabel(ref container, panelName, UIColors["dark"], name, 12, $"{pos[0] + 0.005f} {pos[1] + 0.03f}", $"{pos[0] + 0.175f} {pos[1] + 0.06f}", TextAnchor.MiddleRight);
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Amount", item.priceAmount.ToString()), 12, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[0] + 0.175f} {pos[1] + 0.0299f}", TextAnchor.MiddleRight);
            if (mData.names.ContainsKey(item.seller))
                name = mData.names[item.seller];
            else name = "NONE";
            UI.CreateLabel(ref container, panelName, UIColors["dark"], GetMSG("Seller", name), 12, $"{pos[0] + .001f} {pos[3] - .2f}", $"{pos[2] - .1f} {pos[3] - .14f}", TextAnchor.MiddleLeft);

            if (seller == true)
            {
                UI.LoadImage(ref container, PanelMarket, imgData.SavedImages[Category.None]["UFILTER"][0].ToString(), $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}");
                UI.CreateLabel(ref container, panelName, UIColors["dark"], GetLang("removelisting"), 10, $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}", TextAnchor.MiddleCenter);
                UI.CreateButton(ref container, panelName, "0 0 0 0", "", 40, $"{pos[0] + .02f} {pos[3] - .15f}", $"{pos[0] + .08f} {pos[3] - .1f}", $"UI_RemoveListing {item.ID}");
            }
            else
            {
                UI.CreateButton(ref container, panelName, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_BuyConfirm {item.ID}");
            }
        }

        void MarketSellScreen(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            if (GetTradeBox(player.userID) == null)
            {
                GetSendMSG(player, "YourNoTradeBoxBuying");
                MarketMainScreen(player);
                return;
            }
            if (GetTradeBoxContents(player) == false)
            {
                MarketMainScreen(player);
                return;
            }
            float[] pos;
            var i = 0;
            var image = "";
            var element = UI.CreateElementContainer(PanelMarket, "0 0 0 0", "0.275 0.25", "0.725 0.75", true);
            //var count = PlayerBoxContents[player.userID].Count();
            UI.CreateLabel(ref element, PanelMarket, UIColors["black"], $"{TextColors["limegreen"]} {GetLang("SelectItemToSell")}", 20, "0.05 .9", "1 1", TextAnchor.MiddleCenter);
            if (GetTradeBoxContents(player) != false)
            {
                foreach (AMItem item in PlayerBoxContents[player.userID].Where(bl => !mData.Blacklist.Contains(bl.shortname) && !mData.MarketListings.ContainsKey(bl.ID)))
                {
                    pos = CalcButtonPos(i);
                    if (item.cat != Category.None && item.cat != Category.Extra)
                    {
                        if (imgData.SavedImages[item.cat].ContainsKey(item.shortname))
                        {
                            if (imgData.SavedImages[item.cat][item.shortname].ContainsKey(item.skin))
                            {
                                image = imgData.SavedImages[item.cat][item.shortname][item.skin].ToString();
                                UI.LoadImage(ref element, PanelMarket, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            }
                        }
                    }
                    else
                    {
                        image = imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString();
                        UI.LoadImage(ref element, PanelMarket, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                        UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], item.shortname.ToUpper(), 16, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}", TextAnchor.LowerCenter);
                    }
                    if (item.amount > 9999)
                        UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], item.amount.ToString(), 14, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);

                    else if (item.amount > 1)
                        UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], item.amount.ToString(), 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectSalesItem {item.ID}"); i++;
                }
            }
            if (configData.ServerRewards && ServerRewards)
            {
                if (!mData.Blacklist.Contains("SR"))
                {
                    if (CheckPoints(player.userID) is int)
                        if ((int)CheckPoints(player.userID) > 0)
                        {
                            pos = CalcButtonPos(i);
                            UI.CreatePanel(ref element, PanelMarket, "1 1 1 1", $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.Money]["SR"][0].ToString(), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectSR"); i++;
                        }
                }
            }
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Back"), 16, "0.03 0.02", "0.13 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), Category.All)}");
            CuiHelper.AddUi(player, element);
        }

        private void SellItems(BasePlayer player, int step = 0, int page = 0)
        {
            AMItem SalesItem;

            var i = 0;
            string image = "";
            var name = "";
            var element = UI.CreateElementContainer(PanelMarket, "0 0 0 0", "0.3 0.3", "0.7 0.9");
            switch (step)
            {
                case 0:
                    CuiHelper.DestroyUi(player, PanelMarket);
                    SalesItem = SalesItemPrep[player.userID];
                    if (SalesItem == null) return;
                    UI.CreateLabel(ref element, PanelMarket, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("SetName", SalesItem.shortname)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 1:
                    CuiHelper.DestroyUi(player, PanelMarket);
                    SalesItem = SalesItemPrep[player.userID];
                    if (SalesItem == null) return;
                    if (configData.UseUniqueNames && SalesItem.name != "")
                        name = SalesItem.name;
                    else name = SalesItem.shortname;
                    double count = 0;
                    foreach (var item in ItemManager.itemList.Where(a => !mData.Blacklist.Contains(a.shortname)))
                        count++;
                    UI.CreatePanel(ref element, PanelMarket, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    UI.CreateLabel(ref element, PanelMarket, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("SetpriceItemshortname", name)}", 20, "0.05 .95", ".95 1", TextAnchor.MiddleCenter);
                    double entriesallowed = 30;
                    double remainingentries = count - (page * (entriesallowed - 1));
                    double totalpages = (Math.Floor(count / (entriesallowed - 1)));
                    {
                        if (page <= totalpages - 1)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["LAST"][0].ToString(), "0.8 0.02", "0.85 0.075");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.8 0.02", "0.85 0.075", $"UI_SellItems {totalpages}");
                        }
                        if (remainingentries > entriesallowed)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["NEXT"][0].ToString(), "0.74 0.02", "0.79 0.075");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.74 0.02", "0.79 0.075", $"UI_SellItems {page + 1}");
                        }
                        if (page > 0)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["BACK"][0].ToString(), "0.68 0.02", "0.73 0.075");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.68 0.02", "0.73 0.075", $"UI_SellItems {page - 1}");
                        }
                        if (page > 1)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["FIRST"][0].ToString(), "0.62 0.02", "0.67 0.075");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.62 0.02", "0.67 0.075", $"UI_SellItems {0}");
                        }
                    }
                    int n = 0;
                    var pos = CalcButtonPos(n);
                    double shownentries = page * entriesallowed;
                    if (page == 0)
                    {
                        if (configData.ServerRewards == true && ServerRewards)
                        {
                            UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.Money]["SR"][0].ToString(), $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectpriceItemshortname SR");
                            n++;
                            i++;
                        }
                    }
                    foreach (var item in ItemManager.itemList.Where(a => !mData.Blacklist.Contains(a.shortname)))
                    {
                        i++;
                        image = imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString();
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            pos = CalcButtonPos(n);
                            foreach (var category in imgData.SavedImages)
                            {
                                if (category.Value.ContainsKey(item.shortname))
                                    foreach (var entry in category.Value.Where(e => e.Key == item.shortname))
                                    {
                                        image = imgData.SavedImages[category.Key][entry.Key][0].ToString();
                                        break;
                                    }
                                else continue;
                            }
                            UI.LoadImage(ref element, PanelMarket, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            if (image == imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString()) UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], item.shortname, 14, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectpriceItemshortname {item.shortname}");
                            n++;
                        }
                    }
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Quit"), 16, "0.03 0.02", "0.13 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), Category.All)}");
                    break;
                default:
                    CuiHelper.DestroyUi(player, PanelMarket);
                    SalesItem = SalesItemPrep[player.userID];
                    if (SalesItem == null) return;
                    if (configData.UseUniqueNames && SalesItem.name != "")
                        name = SalesItem.name;
                    else name = SalesItem.shortname;
                    UI.CreatePanel(ref element, PanelMarket, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], GetLang("NewItemInfo"), 20, "0.05 .8", ".95 .95");
                    string ItemDetails = GetMSG("ItemDetails", SalesItem.amount.ToString(), name, SalesItem.priceAmount.ToString(), SalesItem.priceItemshortname);
                    UI.CreateLabel(ref element, PanelMarket, UIColors["limegreen"], ItemDetails, 20, "0.1 0.1", "0.9 0.65", TextAnchor.MiddleLeft);
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonbg"], GetLang("ListItem"), 18, "0.2 0.05", "0.4 0.15", $"UI_ListItem", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("CancelListing"), 18, "0.6 0.05", "0.8 0.15", $"UI_CancelListing");
                    break;
            }
            CuiHelper.AddUi(player, element);
        }

        private void PurchaseConfirmation(BasePlayer player, uint index)
        {
            CuiHelper.DestroyUi(player, PanelPurchase);
            AMItem purchaseitem = mData.MarketListings[index];
            string purchaseimage = imgData.SavedImages[Category.None]["MISSINGIMG"][0].ToString();
            var name = "";
            if (configData.UseUniqueNames && purchaseitem.name != "")
                name = purchaseitem.name;
            else name = purchaseitem.shortname;
            var element = UI.CreateElementContainer(PanelPurchase, UIColors["dark"], "0.425 0.35", "0.575 0.65", true);
            UI.CreatePanel(ref element, PanelPurchase, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelPurchase, MsgColor, GetMSG("PurchaseConfirmation", name), 20, "0.05 0.75", "0.95 0.95");
            Vector2 position = new Vector2(0.375f, 0.45f);
            Vector2 dimensions = new Vector2(0.25f, 0.25f);
            Vector2 posMin = position;
            Vector2 posMax = posMin + dimensions;
            if (purchaseitem.cat != Category.None && purchaseitem.cat != Category.Extra)
            {
                if (imgData.SavedImages[purchaseitem.cat].ContainsKey(purchaseitem.shortname))
                {
                    if (imgData.SavedImages[purchaseitem.cat][purchaseitem.shortname].ContainsKey(purchaseitem.skin))
                        purchaseimage = imgData.SavedImages[purchaseitem.cat][purchaseitem.shortname][purchaseitem.skin].ToString();
                    else
                        purchaseimage = imgData.SavedImages[purchaseitem.cat][purchaseitem.shortname][0].ToString();
                }
            }
            else UI.CreateLabel(ref element, PanelPurchase, UIColors["limegreen"], purchaseitem.shortname, 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", TextAnchor.MiddleCenter);
            UI.LoadImage(ref element, PanelPurchase, purchaseimage, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}");
            if (purchaseitem.amount > 1)
                UI.CreateLabel(ref element, PanelPurchase, UIColors["limegreen"], $"x {purchaseitem.amount}", 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", TextAnchor.MiddleCenter);
            if (mData.MarketListings[index].cat != Category.Money)
            {
                Item item = BuildCostItems(mData.MarketListings[index].shortname, mData.MarketListings[index].amount);

                if (item.condition != 0)
                {
                    var percent = System.Convert.ToDouble(purchaseitem.condition / item.condition);
                    var xMax = .1f + (0.8f * percent);
                    var ymin = 0.3;
                    var ymax = 0.4;
                    UI.CreatePanel(ref element, PanelPurchase, UIColors["buttonbg"], $"0.1 {ymin}", $"0.9 {ymax}");
                    UI.CreatePanel(ref element, PanelPurchase, UIColors["green"], $"0.1 {ymin}", $"{xMax} {ymax}");
                    UI.CreateLabel(ref element, PanelPurchase, "1 1 1 1", GetMSG("ItemCondition", Math.Round(percent * 100).ToString()), 20, $"0.1 {ymin}", $"0.9 {ymax}", TextAnchor.MiddleLeft);
                }

                UI.CreateButton(ref element, PanelPurchase, UIColors["buttongreen"], GetLang("Yes"), 14, "0.25 0.05", "0.45 0.2", $"UI_ProcessItem {index}");
            }
            else UI.CreateButton(ref element, PanelPurchase, UIColors["buttongreen"], GetLang("Yes"), 14, "0.25 0.05", "0.45 0.2", $"UI_ProcessMoney {index}");
            UI.CreateButton(ref element, PanelPurchase, UIColors["buttonred"], GetLang("No"), 14, "0.55 0.05", "0.75 0.2", $"UI_DestroyPurchaseScreen");
            CuiHelper.AddUi(player, element);
        }

        private void TradeBoxConfirmation(BasePlayer player, uint ID)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            var element = UI.CreateElementContainer(PanelMarket, UIColors["dark"], "0.425 0.45", "0.575 0.55", true);
            UI.CreatePanel(ref element, PanelMarket, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelMarket, MsgColor, GetLang("TradeBoxCreation"), 14, "0.05 0.56", "0.95 0.9");
            UI.CreateButton(ref element, PanelMarket, UIColors["buttongreen"], GetLang("Yes"), 14, "0.05 0.25", "0.475 0.55", $"UI_SaveTradeBox {ID}");
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("No"), 14, "0.525 0.25", "0.95 0.55", $"UI_CancelTradeBox");
            CuiHelper.AddUi(player, element);
        }

        private void AdminPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            var i = 0;
            var element = UI.CreateElementContainer(PanelMarket, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelMarket, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelMarket, MsgColor, GetLang("AdminPanel"), 75, "0.05 0", "0.95 1");
            var loc = CalcButtonPos(i);
            UI.CreateButton(ref element, PanelMarket, UIColors["CSorange"], GetLang("BlackListingADD"), 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_BlackList {0} add"); i++;
            loc = CalcButtonPos(i);
            UI.CreateButton(ref element, PanelMarket, UIColors["CSorange"], GetLang("BlackListingREMOVE"), 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_BlackList {0} remove"); i++;
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Back"), 16, "0.03 0.02", "0.13 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), Category.All)}");
            CuiHelper.AddUi(player, element);
        }

        private void MarketBackgroundMenu(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            if (!mData.background.ContainsKey(player.userID))
                mData.background.Add(player.userID, "NONE");
            var i = 0;
            var element = UI.CreateElementContainer(PanelMarket, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelMarket, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelMarket, MsgColor, GetLang("SelectTheme"), 20, "0 .9", "1 1");
            var count = imgData.SavedBackgrounds.Count();
            double entriesallowed = 30;
            double remainingentries = count - (page * (entriesallowed - 1));
            double totalpages = (Math.Floor(count / (entriesallowed - 1)));
            {
                if (page <= totalpages - 1)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["LAST"][0].ToString(), "0.8 0.02", "0.85 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.8 0.02", "0.85 0.075", $"UI_MarketBackgroundMenu {totalpages}");
                }
                if (remainingentries > entriesallowed)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["NEXT"][0].ToString(), "0.74 0.02", "0.79 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.74 0.02", "0.79 0.075", $"UI_MarketBackgroundMenu {page + 1}");
                }
                if (page > 0)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["BACK"][0].ToString(), "0.68 0.02", "0.73 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.68 0.02", "0.73 0.075", $"UI_MarketBackgroundMenu {page - 1}");
                }
                if (page > 1)
                {
                    UI.LoadImage(ref element, PanelMarket, imgData.SavedImages[Category.None]["FIRST"][0].ToString(), "0.62 0.02", "0.67 0.075");
                    UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 16, "0.62 0.02", "0.67 0.075", $"UI_MarketBackgroundMenu {0}");
                }
            }

            double shownentries = page * entriesallowed;
            int n = 0;
            foreach (var entry in imgData.SavedBackgrounds)
            {
                i++;
                if (i < shownentries + 1) continue;
                else if (i <= shownentries + entriesallowed)
                {
                    var loc = CalcButtonPos(n);
                    if (mData.background[player.userID] != entry.Key)
                    {
                        UI.LoadImage(ref element, PanelMarket, entry.Value.ToString(), $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}");
                        UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{loc[0]} {loc[1]}", $"{loc[2]} {loc[3]}", $"UI_ChangeBackground {entry.Key}");
                        n++;
                    }
                }
            }
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Back"), 16, "0.03 0.02", "0.13 0.075", $"UI_MarketMainScreen {0} {Enum.GetName(typeof(Category), Category.All)}");
            CuiHelper.AddUi(player, element);
        }

        private void BlackListing(BasePlayer player, int page = 0, string action = "add")
        {
            CuiHelper.DestroyUi(player, PanelMarket);
            var i = 0;
            string image = "";
            double count = 0;
            var element = UI.CreateElementContainer(PanelMarket, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelMarket, UIColors["light"], "0.01 0.02", "0.99 0.98");
            int entriesallowed = 30;
            int shownentries = page * entriesallowed;
            int n = 0;
            if (action == "add")
            {
                foreach (var cat in imgData.SavedImages)
                    foreach (var entry in cat.Value)
                        count++;
                UI.CreateLabel(ref element, PanelMarket, UIColors["black"], $"{TextColors["limegreen"]} {GetLang("SelectItemToBlacklist")}", 75, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                foreach (var category in imgData.SavedImages.Where(kvp => kvp.Key != Category.Extra && kvp.Key != Category.None))
                    foreach (var entry in category.Value.Where(bl => !mData.Blacklist.Contains(bl.Key)).OrderBy(kvp => kvp.Key))
                    {
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            var pos = CalcButtonPos(n);
                            image = imgData.SavedImages[category.Key][entry.Key][0].ToString();
                            UI.LoadImage(ref element, PanelMarket, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_BackListItem add {entry.Key}");
                            n++;
                        }
                    }
            }
            else if (action == "remove")
            {
                count = mData.Blacklist.Count();
                UI.CreateLabel(ref element, PanelMarket, UIColors["black"], $"{TextColors["limegreen"]} {GetLang("SelectItemToUnBlacklist")}", 75, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                foreach (var category in imgData.SavedImages.Where(kvp => kvp.Key != Category.Extra && kvp.Key != Category.None))
                    foreach (var entry in category.Value.Where(bl => mData.Blacklist.Contains(bl.Key)).OrderBy(kvp => kvp.Key))
                    {
                        i++;
                        if (i < shownentries + 1) continue;
                        else if (i <= shownentries + entriesallowed)
                        {
                            var pos = CalcButtonPos(n);
                            image = imgData.SavedImages[category.Key][entry.Key][0].ToString();
                            UI.LoadImage(ref element, PanelMarket, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                            UI.CreateButton(ref element, PanelMarket, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_BackListItem remove {entry.Key}");
                            n++;
                        }
                    }
            }
            double remainingentries = count - (page * (entriesallowed - 1));
            double totalpages = (Math.Floor(count / (entriesallowed - 1)));
            {
                if (page <= totalpages - 1)
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonbg"], GetLang("Last"), 16, "0.87 0.02", "0.97 0.075", $"UI_BlackList {totalpages} {action}");
                if (remainingentries > entriesallowed)
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonbg"], GetLang("Next"), 16, "0.73 0.02", "0.83 0.075", $"UI_BlackList {page + 1} {action}");
                if (page > 0)
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Back"), 16, "0.59 0.02", "0.69 0.075", $"UI_BlackList {page - 1} {action}");
                if (page > 1)
                    UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("First"), 16, "0.45 0.02", "0.55 0.075", $"UI_BlackList {0} {action}");
            }
            UI.CreateButton(ref element, PanelMarket, UIColors["buttonred"], GetLang("Back"), 16, "0.03 0.02", "0.13 0.075", $"UI_AdminPanel");
            CuiHelper.AddUi(player, element);
        }

        #endregion

        #region UI Calculations

        private float[] MarketEntryPos(int number)
        {
            Vector2 position = new Vector2(0.03f, 0.66f);
            Vector2 dimensions = new Vector2(0.3f, 0.25f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 3)
            {
                offsetY = (-0.01f - dimensions.y) * number;
            }
            if (number > 2 && number < 6)
            {
                offsetX = 0.315f;
                offsetY = (-0.01f - dimensions.y) * (number - 3);
            }
            if (number > 5 && number < 9)
            {
                offsetX = 0.315f * 2;
                offsetY = (-0.01f - dimensions.y) * (number - 6);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] FilterButton(int number)
        {
            Vector2 position = new Vector2(0.01f, 0.0f);
            Vector2 dimensions = new Vector2(0.08f, 0.04f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 2)
            {
                offsetY = (0.005f + dimensions.y) * number;
            }
            if (number > 1 && number < 4)
            {
                offsetX = (0.01f + dimensions.x) * 1;
                offsetY = (0.005f + dimensions.y) * (number - 2);
            }
            if (number > 3 && number < 6)
            {
                offsetX = (0.01f + dimensions.x) * 2;
                offsetY = (0.005f + dimensions.y) * (number - 4);
            }
            if (number > 5 && number < 8)
            {
                offsetX = (0.01f + dimensions.x) * 3;
                offsetY = (0.005f + dimensions.y) * (number - 6);
            }
            if (number > 7 && number < 10)
            {
                offsetX = (0.01f + dimensions.x) * 4;
                offsetY = (0.005f + dimensions.y) * (number - 8);
            }
            if (number > 9 && number < 12)
            {
                offsetX = (0.01f + dimensions.x) * 5;
                offsetY = (0.005f + dimensions.y) * (number - 10);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] BackgroundButton(int number)
        {
            Vector2 position = new Vector2(0.3f, 0.97f);
            Vector2 dimensions = new Vector2(0.035f, 0.03f);
            float offsetY = 0;
            float offsetX = 0;
            offsetX = (0.005f + dimensions.x) * number;
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] CalcButtonPos(int number)
        {
            Vector2 position = new Vector2(0.02f, 0.78f);
            Vector2 dimensions = new Vector2(0.15f, 0.15f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.01f + dimensions.x) * (number - 6);
                offsetY = (-0.025f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.01f + dimensions.x) * (number - 12);
                offsetY = (-0.025f - dimensions.y) * 2;
            }
            if (number > 17 && number < 24)
            {
                offsetX = (0.01f + dimensions.x) * (number - 18);
                offsetY = (-0.025f - dimensions.y) * 3;
            }
            if (number > 23 && number < 30)
            {
                offsetX = (0.01f + dimensions.x) * (number - 24);
                offsetY = (-0.025f - dimensions.y) * 4;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private float[] CalcNumButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.75f);
            Vector2 dimensions = new Vector2(0.09f, 0.10f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 9)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 8 && number < 18)
            {
                offsetX = (0.01f + dimensions.x) * (number - 9);
                offsetY = (-0.02f - dimensions.y) * 1;
            }
            if (number > 17 && number < 27)
            {
                offsetX = (0.01f + dimensions.x) * (number - 18);
                offsetY = (-0.02f - dimensions.y) * 2;
            }
            if (number > 26 && number < 36)
            {
                offsetX = (0.01f + dimensions.x) * (number - 27);
                offsetY = (-0.02f - dimensions.y) * 3;
            }
            if (number > 35 && number < 45)
            {
                offsetX = (0.01f + dimensions.x) * (number - 36);
                offsetY = (-0.02f - dimensions.y) * 4;
            }
            if (number > 44 && number < 54)
            {
                offsetX = (0.01f + dimensions.x) * (number - 45);
                offsetY = (-0.02f - dimensions.y) * 5;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #endregion

        #region UI Commands

        [ConsoleCommand("UI_SaveTradeBox")]
        private void cmdUI_SaveTradeBox(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyMarketPanel(player);
            uint ID;
            if (!uint.TryParse(arg.Args[0], out ID))
            {
                GetSendMSG(player, "NoTradeBox");
                return;
            }
            if (mData.TradeBox.ContainsKey(player.userID))
            {
                Dictionary<uint, string> listings = new Dictionary<uint, string>();
                foreach (var entry in mData.MarketListings.Where(kvp => kvp.Value.seller == player.userID))
                    listings.Add(entry.Key, entry.Value.shortname);
                foreach (var entry in listings)
                    RemoveListing(player.userID, entry.Value, entry.Key, "TradeBoxChanged");
                listings.Clear();
                mData.TradeBox.Remove(player.userID);
            }
            mData.TradeBox.Add(player.userID, ID);
            if (SettingBox.Contains(player.userID)) SettingBox.Remove(player.userID);
            SaveData();
        }

        [ConsoleCommand("UI_CancelTradeBox")]
        private void cmdUI_CancelTradeBox(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyMarketPanel(player);
            if (SettingBox.Contains(player.userID))
            {
                SettingBox.Remove(player.userID);
                GetSendMSG(player, "ExitedBoxMode");
            }
        }

        [ConsoleCommand("UI_DestroyMarketPanel")]
        private void cmdUI_DestroyBoxConfirmation(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyMarketPanel(player);
        }

        [ConsoleCommand("UI_DestroyPurchaseScreen")]
        private void cmdUI_DestroyPurchaseScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyPurchaseScreen(player);
        }

        [ConsoleCommand("UI_ToggleMarketScreen")]
        private void cmdUI_ToggleMarketScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ToggleMarketScreen(player);
        }

        [ConsoleCommand("UI_SelectSalesItem")]
        private void cmdUI_SelectSalesItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyMarketPanel(player);
            uint ID;
            if (!uint.TryParse(arg.Args[0], out ID))
                GetSendMSG(player, "INVALIDENTRY", arg.Args[0]);
            if (SalesItemPrep.ContainsKey(player.userID))
                SalesItemPrep.Remove(player.userID);
            SalesItemPrep.Add(player.userID, new AMItem());
            foreach (var entry in PlayerBoxContents[player.userID].Where(k => k.ID == ID))
            {
                SalesItemPrep[player.userID] = entry;
            }
            PlayerBoxContents.Remove(player.userID);
            SalesItemPrep[player.userID].seller = player.userID;
            SalesItemPrep[player.userID].stepNum = 0;
            if (configData.UseUniqueNames)
                SellItems(player);
            else
                SellItems(player, 1);
        }

        [ConsoleCommand("UI_SelectSR")]
        private void cmdUI_SelectSR(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyMarketPanel(player);
            if (SalesItemPrep.ContainsKey(player.userID))
                SalesItemPrep.Remove(player.userID);
            SalesItemPrep.Add(player.userID, new AMItem());
            PlayerBoxContents.Remove(player.userID);
            SalesItemPrep[player.userID].cat = Category.Money;
            SalesItemPrep[player.userID].shortname = "SR";
            SalesItemPrep[player.userID].skin = 0;
            SalesItemPrep[player.userID].ID = GetRandomNumber();
            SalesItemPrep[player.userID].seller = player.userID;
            SalesItemPrep[player.userID].stepNum = 0;
            NumberPad(player, "UI_SRAmount");
        }

        [ConsoleCommand("UI_SRAmount")]
        private void cmdUI_SRAmount(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int amount = Convert.ToInt32(arg.Args[0]);
            var currentSRlisted = 0;
            if (CheckPoints(player.userID) is int)
                if ((int)CheckPoints(player.userID) >= amount)
                {
                    foreach (var entry in mData.MarketListings.Where(kvp => kvp.Value.seller == player.userID && kvp.Value.shortname == "SR"))
                        currentSRlisted += entry.Value.amount;
                    if ((int)CheckPoints(player.userID) - currentSRlisted >= amount)
                    {
                        SalesItemPrep[player.userID].amount = amount;
                        DestroyMarketPanel(player);
                        if (configData.UseUniqueNames)
                        {
                            SellItems(player);
                            return;
                        }
                        else
                        {
                            SellItems(player, 1);
                            return;
                        }
                    }
                }
            GetSendMSG(player, "NotEnoughSRPoints");
        }


        [ConsoleCommand("UI_SelectpriceItemshortname")]
        private void cmdUI_SelectpriceItemshortname(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            string priceItemshortname = arg.Args[0];
            SalesItemPrep[player.userID].priceItemshortname = priceItemshortname;
            foreach (var cat in imgData.SavedImages)
            {
                if (cat.Value.ContainsKey(priceItemshortname))
                {
                    SalesItemPrep[player.userID].pricecat = cat.Key;
                    break;
                }
                else
                {
                    SalesItemPrep[player.userID].pricecat = Category.None;
                    continue;
                }
            }
            SalesItemPrep[player.userID].stepNum = 1;
            NumberPad(player, "UI_SelectPriceAmount");
        }

        [ConsoleCommand("UI_SelectPriceAmount")]
        private void cmdUI_SelectPriceAmount(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int amount = Convert.ToInt32(arg.Args[0]);
            SalesItemPrep[player.userID].priceAmount = amount;
            DestroyMarketPanel(player);
            SellItems(player, 99);
        }


        private uint GetRandomNumber()
        {
            var random = new System.Random();
            uint number = (uint)random.Next(0, int.MaxValue);
            return number;
        }

        [ConsoleCommand("UI_ListItem")]
        private void cmdUI_ListItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (SalesItemPrep[player.userID].cat != Category.Money)
            {
                if (GetTradeBoxContents(player) == false) return;
                if (!mData.MarketListings.ContainsKey(SalesItemPrep[player.userID].ID))
                {
                    if (BoxCheck(player, SalesItemPrep[player.userID].ID))
                    {
                        var solditem = SalesItemPrep[player.userID].shortname;
                        mData.MarketListings.Add(SalesItemPrep[player.userID].ID, SalesItemPrep[player.userID]);
                        if (SalesItemPrep.ContainsKey(player.userID))
                            SalesItemPrep.Remove(player.userID);
                        if (PlayerBoxContents.ContainsKey(player.userID))
                            PlayerBoxContents.Remove(player.userID);
                        GetSendMSG(player, "NewItemListed", solditem);
                        DestroyMarketPanel(player);
                        MarketMainScreen(player);
                        return;
                    }
                    GetSendMSG(player, "ItemNotInBox");
                }
                GetSendMSG(player, "ItemAlreadyListed");
                CancelListing(player);
            }
            else
            {
                var money = "";
                mData.MarketListings.Add(SalesItemPrep[player.userID].ID, SalesItemPrep[player.userID]);
                if (SalesItemPrep[player.userID].shortname == "SR")
                    money = "Server Rewards Points";
                GetSendMSG(player, "NewMoneyListed", money, SalesItemPrep[player.userID].amount.ToString());
                if (SalesItemPrep.ContainsKey(player.userID))
                    SalesItemPrep.Remove(player.userID);
                if (PlayerBoxContents.ContainsKey(player.userID))
                    PlayerBoxContents.Remove(player.userID);
                DestroyMarketPanel(player);
                MarketMainScreen(player);
            }
        }

        [ConsoleCommand("UI_CancelListing")]
        private void cmdUI_CancelListing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CancelListing(player);
        }

        [ConsoleCommand("UI_ChangeBackground")]
        private void cmdUI_ChangeBackground(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            mData.background[player.userID] = arg.Args[0];
            MarketMainScreen(player);
        }

        [ConsoleCommand("UI_MarketMainScreen")]
        private void cmdUI_MainMarketScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            Category cat;
            cat = (Category)Enum.Parse(typeof(Category), arg.Args[1]);
            MarketMainScreen(player, page, cat);
        }

        [ConsoleCommand("UI_MarketBackgroundMenu")]
        private void cmdUI_MarketBackgroundMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            MarketBackgroundMenu(player, page);
        }

        [ConsoleCommand("UI_BlackList")]
        private void cmdUI_BlackList(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            var action = arg.Args[1];
            BlackListing(player, page, action);
        }

        [ConsoleCommand("UI_AdminPanel")]
        private void cmdUI_AdminPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AdminPanel(player);
        }

        [ConsoleCommand("UI_MarketSellScreen")]
        private void cmdUI_MarketSellScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            MarketSellScreen(player, page);
        }

        [ConsoleCommand("UI_Mode")]
        private void cmdUI_Mode(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int action;
            if (!int.TryParse(arg.Args[0], out action)) return;
            if (action == 0)
                mData.mode[player.userID] = false;
            if (action == 1)
                mData.mode[player.userID] = true;
            MarketMainScreen(player);
        }



        [ConsoleCommand("UI_SetBoxMode")]
        private void cmdUI_SetBoxMode(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!SettingBox.Contains(player.userID)) SettingBox.Add(player.userID);
            DestroyMarketPanel(player);
            GetSendMSG(player, "TradeBoxMode");
        }


        [ConsoleCommand("UI_BuyConfirm")]
        private void cmdUI_BuyConfirm(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyPurchaseScreen(player);
            uint ID;
            if (!uint.TryParse(arg.Args[0], out ID)) return;
            AMItem purchaseitem = mData.MarketListings[ID];
            var name = "";
            if (configData.UseUniqueNames && purchaseitem.name != "")
                name = purchaseitem.name;
            else name = purchaseitem.shortname;
            ulong buyer = player.userID;
            ulong seller = mData.MarketListings[ID].seller;
            if (PlayerInventory.ContainsKey(buyer))
                PlayerInventory.Remove(buyer);
            PlayerInventory.Add(buyer, new List<Item>());
            if (GetTradeBox(seller) != null && GetTradeBox(buyer) != null)
            {
                StorageContainer buyerbox = GetTradeBox(buyer);
                StorageContainer sellerbox = GetTradeBox(seller);
                if (!buyerbox.inventory.IsFull() && !sellerbox.inventory.IsFull())
                {
                    if (mData.MarketListings[ID].cat != Category.Money)
                    {
                        var c = 0;
                        foreach (Item item in sellerbox.inventory.itemList.Where(kvp => kvp.uid == ID))
                        {
                            c += item.amount;
                            if (item.condition != purchaseitem.condition)
                            {
                                RemoveListing(seller, name, purchaseitem.ID, "ItemCondChange");
                                MarketMainScreen(player);
                                return;
                            }
                            if (item.amount != purchaseitem.amount)
                            {
                                RemoveListing(seller, name, purchaseitem.ID, "ItemQuantityChange");
                                MarketMainScreen(player);
                                return;
                            }
                            if (c < purchaseitem.amount)
                            {
                                RemoveListing(seller, name, purchaseitem.ID, "ItemGoneChange");
                                MarketMainScreen(player);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (purchaseitem.shortname == "SR")
                            if ((int)CheckPoints(purchaseitem.seller) < purchaseitem.amount)
                            {
                                RemoveListing(seller, name, purchaseitem.ID, "NotEnoughSRPoints");
                                MarketMainScreen(player);
                                return;
                            }
                    }
                    if (mData.MarketListings[ID].pricecat != Category.Money)
                    {
                        var amount = 0;
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerWear));
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerMain));
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerBelt));
                        foreach (var entry in PlayerInventory[buyer].Where(kvp => kvp.info.shortname == purchaseitem.priceItemshortname))
                        {
                            amount += entry.amount;
                            if (amount >= purchaseitem.priceAmount)
                            {
                                PurchaseConfirmation(player, ID);
                                return;
                            }
                        }
                        GetSendMSG(player, "NotEnoughPurchaseItem", purchaseitem.priceItemshortname, purchaseitem.priceAmount.ToString());
                        return;
                    }
                    else
                    {
                        if (purchaseitem.priceItemshortname == "SR")
                            if ((int)CheckPoints(player.userID) >= purchaseitem.priceAmount)
                            {
                                PurchaseConfirmation(player, ID);
                                return;
                            }
                            else
                            {
                                GetSendMSG(player, "NotEnoughPurchaseItem", purchaseitem.priceItemshortname, purchaseitem.priceAmount.ToString());
                                return;
                            }
                    }
                }
                else
                {
                    if (buyerbox.inventory.IsFull())
                        GetSendMSG(player, "YourTradeBoxFullBuying");
                    else if (sellerbox.inventory.IsFull())
                        GetSendMSG(player, "SellerTradeBoxFullBuying");
                }
            }
            else
            {
                if (GetTradeBox(buyer) == null)
                    GetSendMSG(player, "YourNoTradeBoxBuying");
                else if (GetTradeBox(seller) == null)
                    GetSendMSG(player, "SellerNoTradeBoxBuying");
            }
        }


        [ConsoleCommand("UI_ProcessMoney")]
        private void cmdUI_ProcessMoney(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyPurchaseScreen(player);
        }

        [ConsoleCommand("UI_ProcessItem")]
        private void cmdUI_ProcessItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyPurchaseScreen(player);
            uint ID;
            if (!uint.TryParse(arg.Args[0], out ID)) return;
            AMItem purchaseitem = mData.MarketListings[ID];
            ulong buyer = player.userID;
            ulong seller = mData.MarketListings[ID].seller;
            if (PlayerInventory.ContainsKey(buyer))
                PlayerInventory.Remove(buyer);
            if (TransferableItems.ContainsKey(buyer))
                TransferableItems.Remove(buyer);
            if (PlayerPurchaseApproval.ContainsKey(buyer))
                PlayerPurchaseApproval.Remove(buyer);
            TransferableItems.Add(buyer, new List<Item>());
            PlayerInventory.Add(buyer, new List<Item>());
            PlayerPurchaseApproval.Add(buyer, false);
            if (GetTradeBox(seller) != null && GetTradeBox(buyer) != null)
            {
                StorageContainer buyerbox = GetTradeBox(buyer);
                StorageContainer sellerbox = GetTradeBox(seller);
                if (!buyerbox.inventory.IsFull() && !sellerbox.inventory.IsFull())
                {
                    if (purchaseitem.pricecat != Category.Money)
                    {
                        var amount = 0;
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerWear));
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerMain));
                        PlayerInventory[buyer].AddRange(GetItemsOnly(player.inventory.containerBelt));
                        foreach (var entry in PlayerInventory[buyer].Where(kvp => kvp.info.shortname == purchaseitem.priceItemshortname))
                        {
                            amount += entry.amount;
                            TransferableItems[buyer].Add(entry);
                            if (amount >= purchaseitem.priceAmount)
                            {
                                PlayerPurchaseApproval[buyer] = true;
                                break;
                            }
                            else continue;
                        }
                    }
                    else
                    {
                        if (purchaseitem.priceItemshortname == "SR")
                            if ((int)CheckPoints(player.userID) >= purchaseitem.priceAmount)
                            {
                                PlayerPurchaseApproval[buyer] = true;
                            }
                    }
                    if (PlayerPurchaseApproval[buyer] == true)
                    {
                        if (ItemsToTransfer.ContainsKey(buyer))
                            ItemsToTransfer.Remove(buyer);
                        ItemsToTransfer.Add(buyer, new List<Item>());
                        if (purchaseitem.pricecat != Category.Money)
                        {
                            foreach (var entry in TransferableItems[buyer])
                                XferCost(entry, player, ID);
                            foreach (Item item in ItemsToTransfer[buyer])
                                item.MoveToContainer(sellerbox.inventory);
                            ItemsToTransfer[buyer].Clear();
                        }
                        else
                        {
                            SRAction(buyer, purchaseitem.priceAmount, "REMOVE");
                            SRAction(seller, purchaseitem.priceAmount, "ADD");
                        }
                        if (purchaseitem.cat != Category.Money)
                        {
                            XferPurchase(buyer, ID, sellerbox.inventory, buyerbox.inventory);
                        }
                        else
                        {
                            SRAction(seller, purchaseitem.amount, "REMOVE");
                            SRAction(buyer, purchaseitem.amount, "ADD");
                        }
                        GetSendMSG(player, "NewPurchase", purchaseitem.shortname, purchaseitem.amount.ToString());
                        AddMessages(seller, "NewSale", purchaseitem.shortname, purchaseitem.amount.ToString());
                        mData.MarketListings.Remove(ID);
                        if (ItemsToTransfer[buyer].Count > 0)
                            foreach (Item item in ItemsToTransfer[buyer])
                                item.MoveToContainer(buyerbox.inventory);
                        MarketMainScreen(player);
                    }
                    else
                    {
                        GetSendMSG(player, "NotEnoughPurchaseItem", purchaseitem.priceItemshortname, purchaseitem.priceAmount.ToString());
                    }
                }
                else
                {
                    if (buyerbox.inventory.IsFull())
                        GetSendMSG(player, "YourTradeBoxFullBuying");
                    else if (sellerbox.inventory.IsFull())
                        GetSendMSG(player, "SellerTradeBoxFullBuying");
                    MarketMainScreen(player);
                }
            }
            else
            {
                if (GetTradeBox(buyer) == null)
                    GetSendMSG(player, "YourNoTradeBoxBuying");
                else if (GetTradeBox(seller) == null)
                    GetSendMSG(player, "SellerNoTradeBoxBuying");
            }
        }

        [ConsoleCommand("UI_RemoveListing")]
        private void cmdUI_RemoveListing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            uint ID;
            if (!uint.TryParse(arg.Args[0], out ID)) return;
            var name = "";
            if (configData.UseUniqueNames && mData.MarketListings[ID].name != "")
                name = mData.MarketListings[ID].name;
            else name = mData.MarketListings[ID].shortname;
            RemoveListing(player.userID, name, ID, "SellerRemoval");
            MarketMainScreen(player);
        }

        [ConsoleCommand("UI_SellItems")]
        private void cmdUI_SellItems(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int page;
            if (!int.TryParse(arg.Args[0], out page)) return;
            SellItems(player, 1, page);
        }

        [ConsoleCommand("UI_BackListItem")]
        private void cmdUI_BackListItem(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var action = arg.Args[0];
            var item = arg.Args[1];
            if (action == "add")
                mData.Blacklist.Add(item);
            else if (action == "remove")
                mData.Blacklist.Remove(item);
            AdminPanel(player);
        }

        #endregion

        #region Timers

        private void SaveLoop()
        {
            if (timers.ContainsKey("save"))
            {
                timers["save"].Destroy();
                timers.Remove("save");
            }
            SaveData();
            timers.Add("save", timer.Once(600, () => SaveLoop()));
        }

        private void InfoLoop()
        {
            if (timers.ContainsKey("info"))
            {
                timers["info"].Destroy();
                timers.Remove("info");
            }
            if (configData.InfoInterval == 0) return;
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                GetSendMSG(p, "AMInfo", configData.MarketMenuKeyBinding);
            }
            timers.Add("info", timer.Once(configData.InfoInterval * 60, () => InfoLoop()));
        }

        private void SetBoxFullNotification(string ID)
        {
            timers.Add(ID, timer.Once(5 * 60, () => timers.Remove(ID)));
        }

        #endregion

        #region Classes
        class MarketData
        {
            public Dictionary<uint, AMItem> MarketListings = new Dictionary<uint, AMItem>();
            public Dictionary<ulong, uint> TradeBox = new Dictionary<ulong, uint>();
            public Dictionary<ulong, string> background = new Dictionary<ulong, string>();
            public Dictionary<ulong, bool> mode = new Dictionary<ulong, bool>();
            public Dictionary<ulong, List<Unsent>> OutstandingMessages = new Dictionary<ulong, List<Unsent>>();
            public List<string> Blacklist = new List<string>();
            public Dictionary<ulong, string> names = new Dictionary<ulong, string>();
        }

        class AMImages
        {
            public Dictionary<Category, Dictionary<string, Dictionary<ulong, uint>>> SavedImages = new Dictionary<Category, Dictionary<string, Dictionary<ulong, uint>>>();
            public Dictionary<string, uint> SavedBackgrounds = new Dictionary<string, uint>();
        }

        class Unsent
        {
            public string message;
            public string arg1;
            public string arg2;
            public string arg3;
            public string arg4;
        }

        enum Category
        {
            Weapons,
            Armor,
            Attire,
            Ammunition,
            Medical,
            Tools,
            Building,
            Resources,
            Other,
            None,
            All,
            Extra,
            Food,
            Money
        }

        class AMItem
        {
            public string name;
            public string shortname;
            public ulong skin;
            public uint ID;
            public Category cat;
            public bool approved;
            public Category pricecat;
            public string priceItemshortname;
            public int priceItemID;
            public int priceAmount;
            public int amount;
            public int stepNum;
            public ulong seller;
            public float condition;
        }

        #endregion

        #region Unity WWW
        class QueueImage
        {
            public string url;
            public string shortname;
            public ulong skinid;
            public Category cat;
            public QueueImage(string ur, Category ct, string st, ulong sk)
            {
                url = ur;
                shortname = st;
                skinid = sk;
                cat = ct;
            }
        }
        
        class UnityImages : MonoBehaviour
        {
            AbsolutMarket filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueImage> QueueList = new List<QueueImage>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(AbsolutMarket am) => filehandler = am;
            public void Add(string url, Category cat, string shortname, ulong skinid)
            {
                QueueList.Add(new QueueImage(url, cat, shortname, skinid));
                if (activeLoads < MaxActiveLoads) Next();
            }

            void Next()
            {
                activeLoads++;
                var qi = QueueList[0];
                QueueList.RemoveAt(0);
                var www = new WWW(qi.url);
                StartCoroutine(WaitForRequest(www, qi));
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(WWW www, QueueImage info)
            {
                yield return www;

                if (www.error == null)
                {
                    if (!filehandler.imgData.SavedImages.ContainsKey(info.cat))
                        filehandler.imgData.SavedImages.Add(info.cat, new Dictionary<string, Dictionary<ulong, uint>>());
                    if (!filehandler.imgData.SavedImages[info.cat].ContainsKey(info.shortname))
                        filehandler.imgData.SavedImages[info.cat].Add(info.shortname, new Dictionary<ulong, uint>());
                    if (!filehandler.imgData.SavedImages[info.cat][info.shortname].ContainsKey(info.skinid))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.imgData.SavedImages[info.cat][info.shortname].Add(info.skinid, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }

        class QueueBackground
        {
            public string url;
            public string name;
            public QueueBackground(string ur, string nm)
            {
                url = ur;
                name = nm;
            }
        }

        class UnityBackgrounds : MonoBehaviour
        {
            AbsolutMarket filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueBackground> QueueList = new List<QueueBackground>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(AbsolutMarket am) => filehandler = am;
            public void Add(string url, string name)
            {
                QueueList.Add(new QueueBackground(url, name));
                if (activeLoads < MaxActiveLoads) Next();
            }

            void Next()
            {
                activeLoads++;
                var qi = QueueList[0];
                QueueList.RemoveAt(0);
                var www = new WWW(qi.url);
                StartCoroutine(WaitForRequest(www, qi));
            }

            private void ClearStream()
            {
                stream.Position = 0;
                stream.SetLength(0);
            }

            IEnumerator WaitForRequest(WWW www, QueueBackground info)
            {
                yield return www;

                if (www.error == null)
                {
                    if (!filehandler.imgData.SavedBackgrounds.ContainsKey(name))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.imgData.SavedBackgrounds.Add(info.name, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }

        [ConsoleCommand("getimages")]
        private void cmdgetimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Getimages();
            }
        }

        private void Getimages()
        {
            imgData.SavedImages.Clear();
            foreach (var category in urls)
                foreach (var entry in category.Value)
                    foreach (var item in entry.Value)
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            uImage.Add(item.Value, category.Key, entry.Key, item.Key);
                        }
                    }
            Puts(GetLang("ImgReload"));
        }

        [ConsoleCommand("refreshimages")]
        private void cmdrefreshimages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                Refreshimages();
            }
        }

        private void Refreshimages()
        {
            foreach (var category in urls)
            {
                if (!imgData.SavedImages.ContainsKey(category.Key))
                    imgData.SavedImages.Add(category.Key, new Dictionary<string, Dictionary<ulong, uint>>());
                foreach (var entry in category.Value)
                {
                    if (!imgData.SavedImages[category.Key].ContainsKey(entry.Key))
                        imgData.SavedImages[category.Key].Add(entry.Key, new Dictionary<ulong, uint>());
                    foreach (var item in entry.Value)
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            uImage.Add(item.Value, category.Key, entry.Key, item.Key);
                        }
                    }
                }
            }
            Puts(GetLang("ImgRefresh"));
        }

        [ConsoleCommand("getbackgrounds")]
        private void cmdgetbackgrounds(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                GetBackgrounds();
            }
        }

        private void GetBackgrounds()
        {
            imgData.SavedBackgrounds.Clear();
            foreach (var entry in defaultBackgrounds)
                    uBackground.Add(entry.Value, entry.Key);
            timer.Once(10, () =>
            {

                SaveBackgrounds();
                Puts(GetLang("BckReload"));
                RefreshBackgrounds();
            });
        }

        [ConsoleCommand("refreshbackgrounds")]
        private void cmdrefreshbackgrounds(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                RefreshBackgrounds();
            }
        }

        private void RefreshBackgrounds()
        {
            try
            {
                imgData = IMGData.ReadObject<AMImages>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Market Image File, creating a new datafile");
                imgData = new AMImages();
            }
            try
            {
                bkData = BKData.ReadObject<Backgrounds>();
            }
            catch
            {
                Puts("Couldn't Find Pending Background File , creating a new File");
                bkData = new Backgrounds();
                bkData.PendingBackgrounds = defaultBackgrounds;
                BKData.WriteObject(bkData);
            }
            var i = 0;
            foreach (var entry in bkData.PendingBackgrounds)
                if (!imgData.SavedBackgrounds.ContainsKey(entry.Key))
                {
                    uBackground.Add(entry.Value, entry.Key);
                    i++;
                }
            //bkData.PendingBackgrounds.Clear();
            //BKData.WriteObject(bkData);
            timer.Once(10, () =>
            {
                SaveBackgrounds();
                Puts(GetMSG("BckRefresh", i.ToString()));
            });
            //IMGData.WriteObject(imgData);
            
        }

        private void SaveBackgrounds()
        {
            IMGData.WriteObject(imgData);
        }

        #endregion

        #region Absolut Market Data Management

        private Dictionary<Category, Dictionary<string, Dictionary<ulong, string>>> urls = new Dictionary<Category, Dictionary<string, Dictionary<ulong, string>>>
        {
            {Category.Money, new Dictionary<string, Dictionary<ulong, string>>
            {
                {"SR", new Dictionary<ulong, string>
                {
                {0, "http://oxidemod.org/data/resource_icons/1/1751.jpg?1456924271" },
                }
                },
            }
            },

            {Category.None, new Dictionary<string, Dictionary<ulong, string>>
            {
                {"MISSINGIMG", new Dictionary<ulong, string>
                {
                {0, "http://www.hngu.net/Images/College_Logo/28/b894b451_c203_4c08_922c_ebc95077c157.png" },
                }
                },
                {"ARROW", new Dictionary<ulong, string>
                {
                {0, "http://www.freeiconspng.com/uploads/red-arrow-curved-5.png" },
                }
                },
                {"FIRST", new Dictionary<ulong, string>
                {
                {0, "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/simple-black-square-icons-arrows/126517-simple-black-square-icon-arrows-double-arrowhead-left.png" },
                }
                },
                {"BACK", new Dictionary<ulong, string>
                {
                {0, "https://image.freepik.com/free-icon/back-left-arrow-in-square-button_318-76403.png" },
                }
                },
                {"NEXT", new Dictionary<ulong, string>
                {
                {0, "https://image.freepik.com/free-icon/right-arrow-square-button-outline_318-76302.png"},
                }
                },
                {"LAST", new Dictionary<ulong, string>
                {
                {0, "http://cdn.mysitemyway.com/etc-mysitemyway/icons/legacy-previews/icons/matte-white-square-icons-arrows/124577-matte-white-square-icon-arrows-double-arrowhead-right.png" },
                }
                },
                {"OFILTER", new Dictionary<ulong, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/41/button-1157299_960_720.png" },
                }
                },
                {"UFILTER", new Dictionary<ulong, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/42/button-1157301_960_720.png" },
                }
                },
                {"ADMIN", new Dictionary<ulong, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2016/01/23/11/26/button-1157269_960_720.png" },
                }
                },
                {"MISC", new Dictionary<ulong, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2015/07/25/07/55/the-button-859343_960_720.png" },
                }
                },            
                {"SELL", new Dictionary<ulong, string>
                {
                {0, "https://pixabay.com/static/uploads/photo/2015/07/25/08/03/the-button-859350_960_720.png" },
                }
                },
              
                }
            },
            {Category.Attire, new Dictionary<string, Dictionary<ulong, string>>
            {
                { "tshirt", new Dictionary<ulong, string>
                {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/62/T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200547" },
                {10130, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c6/Argyle_Scavenger_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204436"},
                {10033, "http://vignette2.wikia.nocookie.net/play-rust/images/4/44/Baseball_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053725" },
                {10003, "http://vignette1.wikia.nocookie.net/play-rust/images/b/bb/Black_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054123"},
                {14177, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1e/Blue_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053931" },
                {10056, "http://vignette3.wikia.nocookie.net/play-rust/images/9/98/Facepunch_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053603"},
                {14181, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a2/Forest_Camo_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053948" },
                {10024, "http://vignette3.wikia.nocookie.net/play-rust/images/1/19/German_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200140"},
                {10035, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c0/Hacker_Valley_Veteran_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053826" },
                {10046, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4d/Missing_Textures_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200051"},
                {10038, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4c/Murderer_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195600" },
                {101, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f3/Red_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053820" },
                {10025, "http://vignette1.wikia.nocookie.net/play-rust/images/b/bd/Russia_Tshirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195447" },
                {10002, "http://vignette4.wikia.nocookie.net/play-rust/images/5/59/Sandbox_Game_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054255"},
                {10134, "http://vignette1.wikia.nocookie.net/play-rust/images/6/61/Ser_Winter_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234909" },
                {10131, "http://vignette2.wikia.nocookie.net/play-rust/images/7/70/Shadowfrax_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234921"},
                {10041, "http://vignette1.wikia.nocookie.net/play-rust/images/4/43/Skull_%26_Bones_TShirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195506" },
                {10053, "http://vignette4.wikia.nocookie.net/play-rust/images/5/5b/Smile_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195652"},
                {10039, "http://vignette1.wikia.nocookie.net/play-rust/images/6/6d/Target_Practice_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200031" },
                {584379, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1f/Urban_Camo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054016"},
                {10043, "http://vignette2.wikia.nocookie.net/play-rust/images/d/d8/Vyshyvanka_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053755" },
                }
            },
            {"pants", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3f/Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20150821195647" },
                {10001, "http://vignette2.wikia.nocookie.net/play-rust/images/1/1d/Blue_Jeans_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053716"},
                {10049, "http://vignette3.wikia.nocookie.net/play-rust/images/d/de/Blue_Track_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200133" },
                {10019, "http://vignette2.wikia.nocookie.net/play-rust/images/1/17/Forest_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195731"},
                {10078, "http://vignette2.wikia.nocookie.net/play-rust/images/3/30/Old_Prisoner_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200717" },
                {10048, "http://vignette1.wikia.nocookie.net/play-rust/images/4/4d/Punk_Rock_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053631"},
                {10021, "http://vignette2.wikia.nocookie.net/play-rust/images/d/de/Snow_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195500" },
                {10020, "http://vignette4.wikia.nocookie.net/play-rust/images/5/54/Urban_Camo_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195400" },
            }
            },
            {"shoes.boots", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b3/Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200326" },
                {10080, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f1/Army_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200800"},
                {10023, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c5/Black_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195749" },
                {10088, "http://vignette4.wikia.nocookie.net/play-rust/images/a/af/Bloody_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200836"},
                {10034, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a9/Punk_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195937" },
                {10044, "http://vignette2.wikia.nocookie.net/play-rust/images/5/5b/Scavenged_Sneaker_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195530"},
                {10022, "http://vignette1.wikia.nocookie.net/play-rust/images/c/cf/Tan_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195755" },
            }
            },
             {"tshirt.long", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/57/Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200605" },
                {10047, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e1/Aztec_Long_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195354"},
                {10004, "http://vignette3.wikia.nocookie.net/play-rust/images/e/e2/Black_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195617" },
                {10089, "http://vignette2.wikia.nocookie.net/play-rust/images/8/83/Christmas_Jumper_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200701"},
                {10106, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Creepy_Jack_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201059" },
                {10050, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c2/Frankensteins_Sweater_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195302"},
                {10032, "http://vignette1.wikia.nocookie.net/play-rust/images/0/04/Green_Checkered_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195518" },
                {10005, "http://vignette4.wikia.nocookie.net/play-rust/images/5/53/Grey_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195329"},
                {10125, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2c/Lawman_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201508" },
                {10118, "http://vignette4.wikia.nocookie.net/play-rust/images/9/96/Merry_Reindeer_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201444"},
                {10051, "http://vignette1.wikia.nocookie.net/play-rust/images/4/4b/Nightmare_Sweater_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195419" },
                {10006, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f6/Orange_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195658"},
                {10036, "http://vignette4.wikia.nocookie.net/play-rust/images/8/8a/Sign_Painter_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054215" },
                {10042, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1d/Varsity_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195536" },
                {10007, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ad/Yellow_Longsleeve_T-Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195949"},
            }
            },
             {"mask.bandana", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Bandana_Mask_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200526" },
                {10061, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bf/Black_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200233"},
                {10060, "http://vignette2.wikia.nocookie.net/play-rust/images/1/1d/Blue_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200057" },
                {10067, "http://vignette1.wikia.nocookie.net/play-rust/images/1/13/Checkered_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195703"},
                {10104, "http://vignette1.wikia.nocookie.net/play-rust/images/6/64/Creepy_Clown_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201043" },
                {10066, "http://vignette2.wikia.nocookie.net/play-rust/images/e/ee/Desert_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200208"},
                {10063, "http://vignette2.wikia.nocookie.net/play-rust/images/3/3f/Forest_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195310" },
                {10059, "http://vignette4.wikia.nocookie.net/play-rust/images/5/53/Green_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195442"},
                {10065, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9c/Red_Skull_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195511" },
                {10064, "http://vignette4.wikia.nocookie.net/play-rust/images/a/af/Skull_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195334"},
                {10062, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a4/Snow_Camo_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195425" },
                {10079, "http://vignette1.wikia.nocookie.net/play-rust/images/4/49/Wizard_Bandana_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200841"},
            }
            },
             {"mask.balaclava", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/52/Improvised_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200410" },
                {10105, "http://vignette2.wikia.nocookie.net/play-rust/images/0/01/Burlap_Brains_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201020"},
                {10069, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e8/Desert_Camo_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200044"},
                {10071, "http://vignette3.wikia.nocookie.net/play-rust/images/7/70/Double_Yellow_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195931" },
                {10068, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a6/Forest_Camo_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200221"},
                {10057, "http://vignette4.wikia.nocookie.net/play-rust/images/2/2c/Murica_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053748" },
                {10075, "http://vignette2.wikia.nocookie.net/play-rust/images/2/2b/Nightmare_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062050"},
                {10070, "http://vignette2.wikia.nocookie.net/play-rust/images/8/86/Red_Check_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195709" },
                {10054, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f1/Rorschach_Skull_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053839"},
                {10090, "http://vignette2.wikia.nocookie.net/play-rust/images/0/09/Skin_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200829" },
                {10110, "http://vignette1.wikia.nocookie.net/play-rust/images/c/ce/Stitched_Skin_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201026"},
                {10084, "http://vignette2.wikia.nocookie.net/play-rust/images/2/27/The_Rust_Knight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200811" },
                {10139, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e7/Valentine_Balaclava_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204423"},
                {10111, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9c/Zipper_Face_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201014" },
            }
            },
             {"jacket.snow", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/04/Snow_Jacket_-_Red_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200536" },
                {10082, "http://vignette3.wikia.nocookie.net/play-rust/images/7/75/60%27s_Army_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200741"},
                {10113, "http://vignette2.wikia.nocookie.net/play-rust/images/e/ed/Black_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201110" },
                {10083, "http://vignette4.wikia.nocookie.net/play-rust/images/8/89/Salvaged_Shirt%2C_Coat_and_Tie_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200642"},
                {10112, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c9/Woodland_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201105" },
            }
            },
             {"jacket", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/8b/Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200520" },
                {10011, "http://vignette1.wikia.nocookie.net/play-rust/images/6/65/Blue_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200113"},
                {10012, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f0/Desert_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195737" },
                {10009, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4c/Green_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200156"},
                {10015, "http://vignette3.wikia.nocookie.net/play-rust/images/f/fd/Hunting_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195956" },
                {10013, "http://vignette3.wikia.nocookie.net/play-rust/images/d/db/Multicam_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200127"},
                {10072, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b5/Provocateur_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200817" },
                {10010, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Red_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195323" },
                {10008, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fc/Snowcamo_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200105"},
                {10014, "http://vignette2.wikia.nocookie.net/play-rust/images/9/94/Urban_Camo_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200215" },
            }
            },
            {"hoodie", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b5/Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205713" },
                {10142, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/BCHILLZ%21_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160313225348"},
                {14179, "http://vignette3.wikia.nocookie.net/play-rust/images/9/97/Black_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205706" },
                {10052, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Bloody_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205645"},
                {14178, "http://vignette3.wikia.nocookie.net/play-rust/images/5/5a/Blue_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205700" },
                {10133, "http://vignette2.wikia.nocookie.net/play-rust/images/2/27/Cuda87_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205750"},
                {14072, "http://vignette2.wikia.nocookie.net/play-rust/images/2/21/Green_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205654" },
                {10132, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4c/Rhinocrunch_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205744"},
                {10129, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0c/Safety_Crew_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205756" },
                {10086, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c2/Skeleton_Hoodie_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205737"},
            }
            },
            {"hat.cap", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/77/Baseball_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200434" },
                {10029, "http://vignette4.wikia.nocookie.net/play-rust/images/5/56/Blue_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195645"},
                {10027, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Forest_Camo_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200024" },
                {10055, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4e/Friendly_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195606"},
                {10030, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4a/Green_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195925" },
                {10026, "http://vignette3.wikia.nocookie.net/play-rust/images/7/70/Grey_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195714"},
                {10028, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Red_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195742" },
                {10045, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2d/Rescue_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053620" },
            }
            },
            {"hat.beenie", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c1/Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200552" },
                {14180, "http://vignette2.wikia.nocookie.net/play-rust/images/5/58/Black_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106053801"},
                {10018, "http://vignette1.wikia.nocookie.net/play-rust/images/4/43/Blue_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200238" },
                {10017, "http://vignette4.wikia.nocookie.net/play-rust/images/b/b8/Green_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195919"},
                {10040, "http://vignette1.wikia.nocookie.net/play-rust/images/7/71/Rasta_Beenie_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195430" },
                {10016, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4e/Red_Beenie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195554"},
                {10085, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e3/Winter_Deers_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200706" },
            }
            },
            {"burlap.gloves", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a1/Leather_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200306" },
                {10128, "http://vignette4.wikia.nocookie.net/play-rust/images/b/b5/Boxer%27s_Bandages_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201502"},
            }
            },
            {"burlap.shirt", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/d7/Burlap_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200455" },
                {10136, "http://vignette1.wikia.nocookie.net/play-rust/images/7/77/Pirate_Vest_%26_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204350"},
            }
            },
            {"hat.boonie", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/88/Boonie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200347" },
                {10058, "http://vignette4.wikia.nocookie.net/play-rust/images/1/12/Farmer_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195725"},
            }
            },
            {"santahat", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4f/Santa_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151217230743" },
            }
            },
            {"hazmat.pants", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6a/Hazmat_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060831" },
            }
            },
            {"hazmat.jacket", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Hazmat_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054359" },
            }
            },
            {"hazmat.helmet", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Hazmat_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061437" },
            }
            },
            {"hazmat.gloves", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/aa/Hazmat_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061629" },
            }
            },
            {"hazmat.boots", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/8a/Hazmat_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060906" },
            }
            },
            {"hat.miner", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1b/Miners_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060851" },
            }
            },
            {"hat.candle", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/ad/Candle_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061731" },
            }
            },

            {"burlap.trousers", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e5/Burlap_Trousers_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054430" },
            }
            },
            {"burlap.shoes", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/10/Burlap_Shoes_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061222" },
            }
            },
            {"burlap.headwrap", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Burlap_Headwrap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061044" },
            }
            },
            {"shirt.tanktop", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/1e/Tank_Top_icon.png/revision/latest/scale-to-width-down/100?cb=20161102190317" },
            }
            },
            {"shirt.collared", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/8c/Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20161102193325" },
            }
            },
            {"pants.shorts", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/46/Shorts_icon.png/revision/latest/scale-to-width-down/100?cb=20161102194514" },
            }
            },

            }
            },
            {Category.Armor, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"bucket.helmet", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a5/Bucket_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200541" },
                {10127, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1c/Medic_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201521"},
                {10126, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Wooden_Bucket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201438" },
            }
            },
            {"wood.armor.pants", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/68/Wood_Armor_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061716" },
            }
            },
            {"wood.armor.jacket", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4f/Wood_Chestplate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060921" },
            }
            },
            {"roadsign.kilt", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/31/Road_Sign_Kilt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200530" },
            }
            },
            {"roadsign.jacket", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/84/Road_Sign_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054408" },
            }
            },
            {"riot.helmet", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4e/Riot_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060949" },
            }
            },
            {"metal.plate.torso", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9d/Metal_Chest_Plate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061201" },
            }
            },
            {"metal.facemask", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1f/Metal_Facemask_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061432" },
            }
            },

            {"coffeecan.helmet", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/44/Coffee_Can_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061305" },
            }
            },
            {"bone.armor.suit", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/14/Bone_Armor_icon.png/revision/latest/scale-to-width-down/100?cb=20160901064349" },
            }
            },
            {"attire.hide.vest", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c0/Hide_Vest_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061337" },
            }
            },
            {"attire.hide.skirt", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/91/Hide_Skirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065030" },
            }
            },
            {"attire.hide.poncho", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/7/7f/Hide_Poncho_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061141" },
            }
            },
            {"attire.hide.pants", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e4/Hide_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061352" },
            }
            },
            {"attire.hide.helterneck", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hide_Halterneck_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065021" },
            }
            },
            {"attire.hide.boots", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/57/Hide_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060954" },
            }
            },
            {"deer.skull.mask", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/22/Deer_Skull_icon.png/revision/latest/scale-to-width-down/100?cb=20150405141500" },
            }
            },
            }
            },
            {Category.Weapons, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"pistol.revolver", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/58/Revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092929" },
                {10114, "http://vignette1.wikia.nocookie.net/play-rust/images/5/51/Outback_revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092935"},
            }
            },
            {"pistol.semiauto", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6b/Semi-Automatic_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200319" },
                {10087, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Contamination_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200655"},
                {10108, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c3/Halloween_Bat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201053" },
                {10081, "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Reaper_Note_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200711"},
                {10073, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Red_Shine_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200630" },
            }
            },
            {"rifle.ak", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200609" },
                {10135, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9e/Digital_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225138"},
                {10137, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9f/Military_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225144" },
                {10138, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a1/Tempered_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204335"},
            }
            },
            {"rifle.bolt", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200415" },
                {10117, "http://vignette2.wikia.nocookie.net/play-rust/images/2/22/Dreamcatcher_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234844"},
                {10115, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9e/Ghost_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234902" },
                {10116, "http://vignette1.wikia.nocookie.net/play-rust/images/c/cf/Tundra_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234858"},
            }
            },
            {"shotgun.pump", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/60/Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205718" },
                {10074, "http://vignette4.wikia.nocookie.net/play-rust/images/9/94/Chieftain_Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062100"},
                {10140, "http://vignette4.wikia.nocookie.net/play-rust/images/4/42/The_Swampmaster_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205830" },
            }
            },
            {"shotgun.waterpipe", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/1b/Waterpipe_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205730" },
                {10143, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4a/The_Peace_Pipe_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205804"},
            }
            },
            {"rifle.lr300", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/d/d9/LR-300_Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160825132402"},
            }
            },
            {"crossbow", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Crossbow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061004" },
            }
            },
            {"smg.thompson", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4e/Thompson_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092921" },
                {10120, "http://vignette3.wikia.nocookie.net/play-rust/images/8/84/Santa%27s_Little_Helper_icon.png/revision/latest/scale-to-width-down/100?cb=20160225141743"},
            }
            },
            {"weapon.mod.small.scope", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9c/4x_Zoom_Scope_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201610" },
            }
            },
            {"weapon.mod.silencer", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Silencer_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200615" },
            }
            },
            {"weapon.mod.muzzlebrake", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/3/38/Muzzle_Brake_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121719" },
            }
            },
            {"weapon.mod.muzzleboost", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7d/Muzzle_Boost_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121705" },
            }
            },
            {"weapon.mod.lasersight", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/8e/Weapon_Lasersight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201545" },
            }
            },
            {"weapon.mod.holosight", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/45/Holosight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200620" },
            }
            },
            {"weapon.mod.flashlight", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/0d/Weapon_Flashlight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201539" },
            }
            },
            {"spear.wooden", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f2/Wooden_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060930" },
            }
            },
            {"spear.stone", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0a/Stone_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061726" },
            }
            },
            {"smg.2", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Custom_SMG_icon.png/revision/latest/scale-to-width-down/100?cb=20151108000740" },
            }
            },
            {"shotgun.double", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/3f/Double_Barrel_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160816061211" },
            }
            },
            {"salvaged.sword", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/77/Salvaged_Sword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061458" },
            }
            },
            {"salvaged.cleaver", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7e/Salvaged_Cleaver_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054417" },
            }
            },
            {"rocket.launcher", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/06/Rocket_Launcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061852" },
            }
            },
            {"rifle.semiauto", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/8d/Semi-Automatic_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160128160721" },
            }
            },
            {"pistol.eoka", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b5/Eoka_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061104" },
            }
            },
            {"machete", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/34/Machete_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060741" },
            }
            },
            {"mace", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4d/Mace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061207" },
            }
            },
            {"longsword", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/34/Longsword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061240" },
            }
            },
            {"lmg.m249", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c6/M249_icon.png/revision/latest/scale-to-width-down/100?cb=20151112221315" },
            }
            },
            {"knife.bone", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/Bone_Knife_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061357" },
            }
            },
            {"flamethrower", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/55/Flame_Thrower_icon.png/revision/latest/scale-to-width-down/100?cb=20160415084104" },
            }
            },
            {"bow.hunting", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hunting_Bow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060745" },
            }
            },
            {"bone.club", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/19/Bone_Club_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060940" },
            }
            },
            {"grenade.f1", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/52/F1_Grenade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054509" },
            }
            },
            {"grenade.beancan", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/be/Beancan_Grenade_icon.png/revision/latest/scale-to-width-down/50?cb=20151106060959" },
            }
            },
            }
            },


            {Category.Ammunition, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"ammo.handmade.shell", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0d/Handmade_Shell_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061522" },
            }
            },
            {"ammo.pistol", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9b/Pistol_Bullet_icon.png/revision/latest/scale-to-width-down/43?cb=20151106061928" },
            }
            },
             {"ammo.pistol.fire", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/31/Incendiary_Pistol_Bullet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054326" },
            }
            },
            {"ammo.pistol.hv", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e5/HV_Pistol_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061857" },
            }
            },
            {"ammo.rifle", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/49/5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20150405103333" },
            }
            },
            {"ammo.rifle.explosive", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/31/Explosive_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061449" },
            }
            },
            {"ammo.rifle.hv", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/df/HV_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20150612151932" },
            }
            },
            {"ammo.rifle.incendiary", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e1/Incendiary_5.56_Rifle_Ammo_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200254" },
            }
            },
            {"ammo.rocket.basic", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061039" },
            }
            },
            {"ammo.rocket.fire", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f9/Incendiary_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061939" },
            }
            },
            {"ammo.rocket.hv", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f4/High_Velocity_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054436" },
            }
            },
            {"ammo.rocket.smoke", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/80/Smoke_Rocket_icon.png/revision/latest/scale-to-width-down/100?cb=20150531134255" },
            }
            },
            {"ammo.shotgun", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2f/12_Gauge_Buckshot_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061114" },
            }
            },
            {"ammo.shotgun.slug", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/1a/12_Gauge_Slug_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061838" },
            }
            },
            {"arrow.hv", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/e/e5/High_Velocity_Arrow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054350" },
            }
            },
            {"arrow.wooden", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/3d/Wooden_Arrow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061615" },
            }
            },
            }
            },

            {Category.Medical, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"bandage", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f8/Bandage_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061541" },
            }
            },
            {"syringe.medical", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/99/Medical_Syringe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061059" },
            }
            },
            { "largemedkit", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/99/Large_Medkit_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054425" },
            }
            },
            { "antiradpills", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0e/Anti-Radiation_Pills_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060926" },
            }
            },
            }
            },


            {Category.Building, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"bed", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/fe/Bed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061212" },
            }
            },
            {"box.wooden", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/ff/Wood_Storage_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054500" },
            }
            },
            {"box.wooden.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b2/Large_Wood_Box_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200336" },
            }
            },
            {"ceilinglight", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/43/Ceiling_Light_icon.png/revision/latest/scale-to-width-down/100?cb=20160331070008" },
            }
            },
            {"door.double.hinged.metal", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/14/Sheet_Metal_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201657" },
            }
            },
            {"door.double.hinged.toptier", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c1/Armored_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201616" },
            }
            },
            {"door.double.hinged.wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/41/Wood_Double_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201605" },
            }
            },
            {"door.hinged.metal", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Sheet_Metal_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201232" },
            }
            },
            {"door.hinged.toptier", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/bc/Armored_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201148" },
            }
            },
            {"door.hinged.wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7e/Wooden_Door_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201125" },
            }
            },
            {"floor.grill", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/48/Floor_Grill_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201701" },
            }
            },
            {"floor.ladder.hatch", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7c/Ladder_Hatch_icon.png/revision/latest/scale-to-width-down/100?cb=20160203005615" },
            }
            },
            {"gates.external.high.stone", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/85/High_External_Stone_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201341" },
            }
            },
            {"gates.external.high.wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/High_External_Wooden_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200625" },
            }
            },
            {"shelves", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a5/Salvaged_Shelves_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201358" },
            }
            },
            {"shutter.metal.embrasure.a", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/88/Metal_Vertical_embrasure_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201307" },
            }
            },
            {"shutter.metal.embrasure.b", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/5d/Metal_horizontal_embrasure_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201154" },
            }
            },
            {"shutter.wood.a", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/2b/Wood_Shutters_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201159" },
            }
            },
            {"sign.hanging", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/d/df/Two_Sided_Hanging_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200913" },
            }
            },
            {"sign.hanging.banner.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/29/Large_Banner_Hanging_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200937" },
            }
            },
            {"sign.hanging.ornate", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4f/Two_Sided_Ornate_Hanging_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200909" },
            }
            },
            {"sign.pictureframe.landscape", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/87/Landscape_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200943" },
            }
            },
            {"sign.pictureframe.portrait", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/50/Portrait_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200949" },
            }
            },
            {"sign.pictureframe.tall", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/65/Tall_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201003" },
            }
            },
            {"sign.pictureframe.xl", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/bf/XL_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200847" },
            }
            },
            {"sign.pictureframe.xxl", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/95/XXL_Picture_Frame_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200933" },
            }
            },
            {"sign.pole.banner.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/1/16/Large_Banner_on_pole_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200958" },
            }
            },
            {"sign.post.double", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/5e/Double_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200918" },
            }
            },
            {"sign.post.single", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/11/Single_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200928" },
            }
            },
            {"sign.post.town", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/62/One_Sided_Town_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200953" },
            }
            },
            {"sign.post.town.roof", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/fa/Two_Sided_Town_Sign_Post_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200904" },
            }
            },
            {"sign.wooden.huge", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6e/Huge_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054354" },
            }
            },
            {"sign.wooden.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bc/Large_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061909" },
            }
            },
            {"sign.wooden.medium", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c3/Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061634" },
            }
            },
            {"sign.wooden.small", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Small_Wooden_Sign_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061315" },
            }
            },
            {"jackolantern.angry", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/9/96/Jack_O_Lantern_Angry_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062158" },
            }
            },
            {"jackolantern.happy", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/92/Jack_O_Lantern_Happy_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062154" },
            }
            },
            {"ladder.wooden.wall", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c8/Wooden_Ladder_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200358" },
            }
            },
            {"lantern", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/46/Lantern_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060856" },
            }
            },
            {"lock.code", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0c/Code_Lock_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061407" },
            }
            },
            {"mining.quarry", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b8/Mining_Quarry_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054421" },
            }
            },
            {"wall.external.high", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/96/High_External_Wooden_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061300" },
            }
            },
            {"wall.external.high.stone", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b6/High_External_Stone_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060841" },
            }
            },
            {"wall.frame.cell", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f6/Prison_Cell_Wall_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201626" },
            }
            },
            {"wall.frame.cell.gate", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/30/Prison_Cell_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201621" },
            }
            },
            {"wall.frame.fence", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/2a/Chainlink_Fence_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201645" },
            }
            },
            {"wall.frame.fence.gate", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/7a/Chainlink_Fence_Gate_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201556" },
            }
            },
            {"wall.frame.shopfront", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/c/c1/Shop_Front_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201551" },
            }
            },
            {"wall.window.bars.metal", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fe/Metal_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201255" },
            }
            },
            {"wall.window.bars.toptier", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/e/eb/Reinforced_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201133" },
            }
            },
            {"wall.window.bars.wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/27/Wooden_Window_Bars_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201138" },
            }
            },
            {"lock.key", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9e/Lock_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061620" },
            }
            },
            { "barricade.concrete", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b3/Concrete_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061124" },
            }
            },
            {"barricade.metal", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/b/bb/Metal_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061108" },
            }
            },
            { "barricade.sandbags", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a7/Sandbag_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061417" },
            }
            },
            { "barricade.wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e5/Wooden_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061024" },
            }
            },
            { "barricade.woodwire", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7b/Barbed_Wooden_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061508" },
            }
            },
            { "barricade.stone", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/cc/Stone_Barricade_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061226" },
            }
            },
            }
            },

            {Category.Resources, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"charcoal", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ad/Charcoal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061556" },
            }
            },
            {"cloth", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f7/Cloth_icon.png/revision/latest/scale-to-width-down/100?cb=20151106071629" },
            }
            },
            {"crude.oil", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3c/Crude_Oil_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054451" },
            }
            },
            {"fat.animal", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/d/d5/Animal_Fat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060911" },
            }
            },
            {"hq.metal.ore", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/80/High_Quality_Metal_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061625" },
            }
            },
            {"lowgradefuel", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/26/Low_Grade_Fuel_icon.png/revision/latest/scale-to-width-down/100?cb=20151110002210" },
            }
            },
            {"metal.fragments", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/7/74/Metal_Fragments_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061759" },
            }
            },
            {"metal.ore", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0a/Metal_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060814" },
            }
            },
            {"leather", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9a/Leather_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061923" },
            }
            },
            {"metal.refined", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/a/a1/High_Quality_Metal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061156" },
            }
            },
            {"wood", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f2/Wood_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061551" },
            }
            },
            {"seed.corn", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/29/Corn_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054446" },
            }
            },
            {"seed.hemp", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1c/Hemp_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20160708084856" },
            }
            },
            {"seed.pumpkin", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/66/Pumpkin_Seed_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054519" },
            }
            },
            {"stones", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/85/Stones_icon.png/revision/latest/scale-to-width-down/100?cb=20150405123145" },
            }
            },
            {"sulfur", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/32/Sulfur_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061605" },
            }
            },
            {"sulfur.ore", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/22/Sulfur_Ore_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061904" },
            }
            },
            {"gunpowder", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/17/Gun_Powder_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060731" },
            }
            },
            {"researchpaper", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/ac/Research_Paper_icon.png/revision/latest/scale-to-width-down/100?cb=20160819103106" },
            }
            },
            {"explosives", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/47/Explosives_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054330" },
            }
            },
            }
            },





            {Category.Tools, new Dictionary<string, Dictionary<ulong, string>>
            {
            {"botabag", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f5/Bota_Bag_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061015" },
            }
            },
            {"box.repair.bench", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/3/3b/Repair_Bench_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214020" },
            }
            },
            {"bucket.water", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bc/Water_Bucket_icon.png/revision/latest/scale-to-width-down/100?cb=20160413085322" },
            }
            },
            {"explosive.satchel", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/0b/Satchel_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20160813023035" },
            }
            },
            {"explosive.timed", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/6c/Timed_Explosive_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061610" },
            }
            },
            {"flare", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Flare_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061129" },
            }
            },
            {"fun.guitar", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/b/bb/Acoustic_Guitar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060809" },
            }
            },
            {"furnace", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e3/Furnace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054341" },
            }
            },
            {"furnace.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/e/ee/Large_Furnace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054456" },
            }
            },
            {"hatchet", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/06/Hatchet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061743" },
            }
            },
            {"icepick.salvaged", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/e/e1/Salvaged_Icepick_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061453" },
            }
            },
            {"axe.salvaged", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c9/Salvaged_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060750" },
            }
            },
            {"pickaxe", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/86/Pick_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061323" },
            }
            },
            {"research.table", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/21/Research_Table_icon.png/revision/latest/scale-to-width-down/100?cb=20160129014240" },
            }
            },
            {"small.oil.refinery", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/ac/Small_Oil_Refinery_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214041" },
            }
            },
            {"stone.pickaxe", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/77/Stone_Pick_Axe_icon.png/revision/latest/scale-to-width-down/100?cb=20150405134645" },
            }
            },
            {"stonehatchet", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9b/Stone_Hatchet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061843" },
            }
            },
            {"supply.signal", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/24/Supply_Signal_icon.png/revision/latest/scale-to-width-down/100?cb=20151106071621" },
            }
            },
            {"surveycharge", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9a/Survey_Charge_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061517" },
            }
            },
            {"target.reactive", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/60/Reactive_Target_icon.png/revision/latest/scale-to-width-down/100?cb=20160331070018" },
            }
            },
            {"tool.camera", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/0e/Camera_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060755" },
            }
            },
            {"water.barrel", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e2/Water_Barrel_icon.png/revision/latest/scale-to-width-down/100?cb=20160504013134" },
            }
            },
            {"water.catcher.large", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/3/35/Large_Water_Catcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061049" },
            }
            },
            {"water.catcher.small", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/0/04/Small_Water_Catcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061919" },
            }
            },
            {"water.purifier", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6e/Water_Purifier_icon.png/revision/latest/scale-to-width-down/100?cb=20160512082941" },
            }
            },
            {"torch", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/48/Torch_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061512" },
            }
            },
            {"stash.small", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Small_Stash_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062004" },
            }
            },
            {"sleepingbag", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/be/Sleeping_Bag_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200428" },
            }
            },
            {"hammer.salvaged", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f8/Salvaged_Hammer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060804" },
            }
            },
            {"hammer", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/5/57/Hammer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061319" },
            }
            },
            {"blueprintbase", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Blueprint_icon.png/revision/latest/scale-to-width-down/100?cb=20160819063752" },
            }
            },
            {"fishtrap.small", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9d/Survival_Fish_Trap_icon.png/revision/latest/scale-to-width-down/100?cb=20160506135224" },
            }
            },
            {"building.planner", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/ba/Building_Plan_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061847" },
            }
            },
            }
            },

            {Category.Other, new Dictionary<string, Dictionary<ulong, string>>
            {
            { "cctv.camera", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/24/CCTV_Camera_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062215" },
            }
            },
            {"pookie.bear", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/6/61/Pookie_Bear_icon.png/revision/latest/scale-to-width-down/100?cb=20151217230015" },
            }
            },
            {"targeting.computer", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/07/Targeting_Computer_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062210" },
            }
            },
            {"trap.bear", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/b/b0/Snap_Trap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061711" },
            }
            },
            {"trap.landmine", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/83/Land_Mine_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200450" },
            }
            },
            {"autoturret", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f9/Auto_Turret_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062203" },
            }
            },
            {"spikes.floor", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/f/f7/Wooden_Floor_Spikes_icon.png/revision/latest/scale-to-width-down/100?cb=20150517235346" },
            }
            },
            {"note", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/d/d5/Note_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060819" },
            }
            },
            {"paper", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/96/Paper_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054403" },
            }
            },
            {"map", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/c/c8/Paper_Map_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061639" },
            }
            },
            {"campfire", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/35/Camp_Fire_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060846" },
            }
            },
            }
            },

            {Category.Food, new Dictionary<string, Dictionary<ulong, string>>
            {
            { "wolfmeat.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/16/Cooked_Wolf_Meat_icon.png/revision/latest/scale-to-width-down/100?cb=20160131235320" },
            }
            },
            {"waterjug", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/f/f2/Water_Jug_icon.png/revision/latest/scale-to-width-down/100?cb=20160422072821" },
            }
            },
            {"water.salt", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/ce/Salt_Water_icon.png/revision/latest/scale-to-width-down/100?cb=20160708084848" },
            }
            },
            {"water", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/7f/Water_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061403" },
            }
            },
            {"smallwaterbottle", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/fc/Small_Water_Bottle_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061933" },
            }
            },
            {"pumpkin", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4c/Pumpkin_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061814" },
            }
            },
            {"mushroom", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/a8/Mushroom_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060836" },
            }
            },
            {"meat.pork.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Cooked_Pork_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201237" },
            }
            },
            {"humanmeat.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/d/d2/Cooked_Human_Meat_icon.png/revision/latest/scale-to-width-down/100?cb=20150405113229" },
            }
            },
            {"granolabar", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6c/Granola_Bar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060916" },
            }
            },
            {"fish.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/8b/Cooked_Fish_icon.png/revision/latest/scale-to-width-down/100?cb=20160506135233" },
            }
            },
            {"chocholate", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/4/45/Chocolate_Bar_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061914" },
            }
            },
            {"chicken.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/6f/Cooked_Chicken_icon.png/revision/latest/scale-to-width-down/100?cb=20151108000759" },
            }
            },
            {"candycane", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/2/2c/Candy_Cane_icon.png/revision/latest/scale-to-width-down/100?cb=20151217224745" },
            }
            },
            {"can.tuna", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/2/2d/Can_of_Tuna_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061943" },
            }
            },
            {"can.beans", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e5/Can_of_Beans_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060935" },
            }
            },
            {"blueberries", new Dictionary<ulong, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f8/Blueberries_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061231" },
            }
            },
            {"black.raspberries", new Dictionary<ulong, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/6/6f/Black_Raspberries_icon.png/revision/latest/scale-to-width-down/100?cb=20151119214047" },
            }
            },
            {"bearmeat.cooked", new Dictionary<ulong, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/17/Bear_Meat_Cooked_icon.png/revision/latest/scale-to-width-down/100?cb=20160109015147" },
            }
            },
            {"apple", new Dictionary<ulong, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/dc/Apple_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061034" },
            }
            },
            }
            }
        };

        private Dictionary<string, string> defaultBackgrounds = new Dictionary<string, string>
        {
            { "NEVERDELETE", "http://www.intrawallpaper.com/static/images/r4RtXBr.png" },
            { "default2", "http://www.intrawallpaper.com/static/images/background-wallpapers-32_NLplhCS.jpg" },
            { "default3", "http://www.intrawallpaper.com/static/images/Light-Wood-Background-Wallpaper_JHG6qot.jpg" },
            { "default4", "http://www.intrawallpaper.com/static/images/White-Background-BD1.png" },
            { "default5", "http://www.intrawallpaper.com/static/images/Red_Background_05.jpg" },
            { "default6", "http://www.intrawallpaper.com/static/images/White-Background-BD1.png" },
            { "default7", "http://www.intrawallpaper.com/static/images/abstract-hd-wallpapers-1080p_gDn0G81.jpg" },
            { "default8", "http://www.intrawallpaper.com/static/images/Background-HD-High-Quality-C23.jpg" },
            { "default10", "http://www.intrawallpaper.com/static/images/wood_background_hd_picture_3_169844.jpg" },
            { "default11", "http://www.intrawallpaper.com/static/images/518079-background-hd.jpg" },
            { "default12", "http://www.intrawallpaper.com/static/images/special_flashy_stars_background_03_hd_pictures_170805.jpg" },
            { "default13", "http://www.intrawallpaper.com/static/images/maxresdefault_jKFJl8g.jpg" },
            { "default14", "http://www.intrawallpaper.com/static/images/maxresdefault15.jpg" },
        };



        void SaveData()
        {
            MData.WriteObject(mData);
            //IMGData.WriteObject(imgData);
        }

        void LoadData()
        {
            try
            {
                mData = MData.ReadObject<MarketData>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Market Data, creating a new datafile");
                mData = new MarketData();
            }
            try
            {
                imgData = IMGData.ReadObject<AMImages>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Market Image File, creating a new datafile");
                imgData = new AMImages();
            }
            if (!imgData.SavedBackgrounds.ContainsKey("NEVERDELETE"))
                GetBackgrounds(); 
        //try
        //{
        //    bkData = BKData.ReadObject<Backgrounds>();
        //}
        //catch
        //{
        //    Puts("Couldn't Find Pending Background File , creating a new File");
        //    bkData = new Backgrounds();
        //    bkData.PendingBackgrounds = defaultBackgrounds;
        //    BKData.WriteObject(bkData);
        //}
    }

        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        { 
            public string MarketMenuKeyBinding { get; set; }
            public bool UseUniqueNames { get; set; }
            public bool ServerRewards { get; set; }
            public int InfoInterval { get; set; }

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
                MarketMenuKeyBinding = "b",
                UseUniqueNames = false,
                InfoInterval = 15,
                ServerRewards = false,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Absolut Market: " },
            {"AMInfo", "This server is running Absolut Market. Press '{0}' to access the Market Menu and to set a Trade Box. Happy Trading!"},
            {"NoTradeBox", "Error finding target Trade Box!" },
            {"TradeBoxDestroyed", "Your Trade Box has been destroyed!" },
            {"TradeBoxEmpty", "Your Trade Box is empty... place items in it to sell them" },
            {"TradeBoxEmptyNoSR", "Your Trade Box is empty and you have 0 Server Rewards Points...Load Items or Get Points to continue" },
            {"TradeBoxFull", "Your Trade Box is full! Clear room first." },
            {"TradeBoxCreation", "Make this your Trade Box?" },
            {"TradeBoxCanceled", "You have " },
            {"Yes", "Yes?" },
            {"No", "No?" },
            {"SetName", "Please Provide a Name for this Item: {0}</color>" },
            {"SetpriceItemshortname", "Please Select an Item you want in return for {0}</color>"  },
            {"SetPriceAmount", "Please type the amount of {0} required to buy the {1}</color>" },
            {"ItemDetails", "You are listing: {0}: {1}\n          For {2} {3}" },
            {"ItemName", "" },
            {"SelectItemToSell", "Please select an Item from your Trade Box to sell...</color>" },
            {"ListItem", "List Item?" },
            {"CancelListing", "Cancel Listing?" },
            {"ItemListingCanceled", "You have successfully canceled item listing!" },
            {"NewItemListed", "You have successfully listed {0}!" },
            {"NewMoneyListed", "You have successfully listed {1} {0}" },
            {"ItemNotInBox", "It appears the item you are trying to list is no longer in the Trade Box. Listing Canceled..." },
            {"NotEnoughPurchaseItem", "You do not have enough {0}. You need {1}!" },
            {"TradeBoxMode", "You are now in Trade Box Selection Mode. Place a large or small wooden box at anytime to make it your Trade Box. Type quit at anytime to leave this mode." },
            {"ExitedBoxMode", "You have successfully exited Trade Box Selection Mode." },
            {"TradeBoxAssignment", "Set\nTrade Box" },
            {"ItemBeingSold","For Sale" },
            {"Purchasecost", "Cost" },
            {"NewItemInfo", "Listing Item Details" },
            {"removelisting", "Remove?" },
            {"YourTradeBoxFullBuying","Your Trade Box is Full!"},
            {"SellerTradeBoxFullBuying", "Seller's Trade Box is Full!" },
            {"YourNoTradeBoxBuying","You do not have a Trade Box!" },
            {"SellerNoTradeBoxBuying","Seller does not have a Trade Box!" },
            {"NewPurchase", "You have successfully purchased {1} {0}" },
            {"NewSale", "You have successfully sold {1} {0}" },
            {"Next", "Next" },
            {"Back", "Back" },
            {"First", "First" },
            {"Last", "Last" },
            {"Close", "Close"},
            {"Quit", "Quit"},
            {"PurchaseConfirmation", "Would you like to purchase:\n{0}?" },
            {"ItemCondition", "Item Condition: {0}%" },
            {"ConditionWarning", "Some items do not have a condition and will reflect as 0" },
            {"ItemAlreadyListed", "This item already appears to be listed!" },
            {"ItemRemoved", "{0} has been removed from the Absolut Market because {1}" },
            {"FromBox", "it was removed from the Trade Box!" },
            {"ItemCondChange", "the condition of the item has changed." },
            {"ItemQuantityChange", "the quantity of the item has changed." },
            {"TradeBoxChanged", "you have set a new Trade Box." },
            {"ItemGoneChange", "the item is not in the Seller's box." },
            {"SelectItemToBlacklist", "Select an item to Blacklist...</color>" },
            {"SelectItemToUnBlacklist", "Select an item to Remove from Blacklist...</color>" },
            {"AdminPanel", "Admin Menu" },
            {"BlackListingADD", "Add\nBacklist Item" },
            {"BlackListingREMOVE", "Remove\nBacklist Item" },
            {"ChangeTheme", "Change Theme" },
            {"SelectTheme", "Select a Theme" },
            {"Amount", "Amount: {0}" },
            {"Name", "Name: {0}" },
            {"NotEnoughSRPoints", "You do not have enough ServerReward Points!" },
            {"ImgReload", "Images have been wiped and reloaded!" },
            {"ImgRefresh", "Images have been refreshed !" },
            {"BckReload", "Background Images have been wiped and reloaded!" },
            {"BckRefresh", "Background Images have been refreshed and {0} have been added!" },
            {"Seller", "         Seller\n{0}" },
            {"InExchange", "In Exchange\nFor" },
            {"SellerRemoval", "you removed it." },
            {"AllItemsAreBL", "All the items in your box are BlackListed!" },
            {"AllItemsAreBLNoSR", "All the items in your box are BlackListed and you have 0 Server Rewards Points!" },
            {"AllItemsAreListed", "All the items in your box are already listed! Add more and try again." },
            {"AllItemsAreListedNoSR", "All the items in your box are already listed and you have 0 Server Rewards Points!" },
            {"ChangeMode", "Change Mode" }
        };
        #endregion
    }
}
