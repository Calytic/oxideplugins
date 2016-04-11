using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Player Trade", "emu", "0.1.1", ResourceId = 1242)]
    class PlayerTrade : RustPlugin
    {
        #region Strings //this is now properly formatted, only the chat commands are missing (line 913)
        public const string s_TradeRequestPlayer = "Trade request sent to {0}";
        public const string s_TradeRequestOther = "{0} wants to trade with you! Type /tra to accept";
        public const string s_TradeRequestDeclined = "{0} declined your trade request!";
        public const string s_TradeRequestNotFound = "Cannot find player named {0}";
        public const string s_TradeRequestPlayerBusy = "You already have a trade request pending! Type /trd to decline";
        public const string s_TradeRequestOtherBusy = "{0} is already trading!";
        public const string s_WrongTradeRequest = "Usage: /trr \"partial or full player name\"";
        public const string s_WrongOffer = "Usage: /tro \"partial or full item name\" amount";
        public const string s_WrongOfferBlueprint = "Usage: /trob \"partial or full blueprint name\"";
        public const string s_TradeOfferFailed = "You don't have {0}";
        public const string s_TradeCanceled = "Trade canceled!";
        public const string s_NoItemFound = "No item found with that name!";
        public const string s_Disconnected = "You disconnected! Trade canceled.";
        public const string s_DisconnectedOther = "Your partner disconnected! Trade canceled.";
        public const string s_Death = "You died! Trade canceled.";
        public const string s_DeathOther = "Your partner died! Trade canceled.";
        public const string s_TookDamage = "You took damage! Trade canceled.";
        public const string s_TookDamageOther = "Your partner took damage! Trade canceled.";
        public const string s_TradeMade = "Trade successful!";
        public const string s_TimeOut = "{0} failed to answer your trade request in time.";
        public const string s_TimeOutOther = "You failed to answer the trade request in time.";
        public const string s_TooFar = "Your partner is too far away!";
        public const string s_RangeGet = "Trading range is {0}";
        public const string s_CooldownGet = "Trade cooldown is {0}";
        public const string s_TradePartner = "You are trading with {0}";
        public const string s_TradeSuccessful = "Transaction complete!";
        public const string s_YouGet = "You get";
        public const string s_YouGive = "You give";
        public const string s_PlayerReady = "You are ready";
        public const string s_OtherReady = "Partner is ready";
        #endregion

        #region Config

        private const float defaultTradeDistance = 0f;
        private const float defaultTradeCooldown = 0f;

        private static float tradeDistance = defaultTradeDistance;
        private static float tradeCooldown = defaultTradeCooldown;
        private static float timeOut = 30f;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["TradeRange"] = defaultTradeDistance;
            Config["TradeCooldown"] = defaultTradeCooldown;
            SaveConfig();
        }

        private void LoadConfig()
        {
            tradeDistance = GetConfig<float>("TradeRange", defaultTradeDistance);
            tradeCooldown = GetConfig<float>("TradeCooldown", defaultTradeCooldown);
        }

        T GetConfig<T>(string key, T defaultValue) {
            try {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>) {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String)) {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    } else if (t == typeof(int)) {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                } else if (val is Dictionary<string, object>) {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int)) {
                        var cval = new Dictionary<string,int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            } catch (Exception ex) {
                return defaultValue;
            }
        }

        [ChatCommand("traderange")]
        private void SetRange(BasePlayer player, string command, string[] args)
        {
            if(args.Length == 0)
            {
                PrintToChat(player, string.Format(s_RangeGet, tradeDistance));
                return;
            }

            if(args.Length != 1 || !player.IsAdmin())
                return;

            float range;
            bool isValid = float.TryParse(args[0], out range);

            if(!isValid)
                return;

            Config["TradeRange"] = range;
            SaveConfig();
            LoadConfig();

            PrintToChat(player, "Trade range set to " + range);
        }

        [ChatCommand("tradecooldown")]
        private void SetLimit(BasePlayer player, string command, string[] args)
        {
            if(args.Length == 0)
            {
                PrintToChat(player, string.Format(s_CooldownGet, tradeCooldown));
                return;
            }

            if(args.Length != 1 || !player.IsAdmin())
                return;

            float cooldown;
            bool isValid = float.TryParse(args[0], out cooldown);

            if(!isValid)
                return;

            Config["TradeCooldown"] = cooldown;
            SaveConfig();
            LoadConfig();

            PrintToChat(player, "Trade cooldown set to " + cooldown);
        }

        #endregion


        #region Oxide Hooks

        private void OnPlayerInit(BasePlayer player)
        {
            AddTrader(player);
        }

        private void OnServerInitialized()
        {
            LoadConfig();
            LoadGUITranslation();

            for(int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                AddTrader(BasePlayer.activePlayerList[i]);
            }
        }

        private void AddTrader(BasePlayer player)
        {
            if(player.GetComponent<Trader>() == null)
                player.gameObject.AddComponent<Trader>();
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            Trader trader;
            if(container.playerOwner != null)
            {
                trader = Trader.GetTrader(container.playerOwner);
                if(trader != null)
                {
                    trader.CheckOfferedItems();
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            CancelTrade(player, s_Disconnected, s_DisconnectedOther);
        }


        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            CancelTrade(entity as BasePlayer, s_Death, s_DeathOther);
        }


        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            CancelTrade(entity as BasePlayer, s_TookDamage, s_TookDamageOther);
        }

        #endregion

        private void CancelTrade(BasePlayer player, string message, string partnerMessage)
        {
            if(player == null)
                return;

            Trader trader = player.GetComponent<Trader>();
            TradeSession session;

            if(trader != null)
            {
                session = trader.GetTradeSession();
                if(session != null)
                {
                    PrintToChat(player, message);
                    PrintToChat(trader.GetOther().GetPlayer(), partnerMessage);
                    session.CloseSession();
                }
            }
        }

        #region Commands

        [ChatCommand("tra")]
        private void AcceptTradeRequest(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();

            if(trader == null)
                return;

            TradeSession session = trader.GetTradeSession();

            if(session != null)
                session.AcceptRequest();
        }

        [ChatCommand("tro")]
        private void OfferItem(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();

            if(trader == null)
                return;

            TradeSession session = trader.GetTradeSession();

            if(session == null || !session.IsAccepted())
                return;

            TradeItem tradeItem;
            int amount;
            bool isAmountValid;
            ItemDefinition itemDef;

            if(args.Length == 1 || args.Length == 2)
            {
                itemDef = FindItemByDisplayName(args[0]);

                if(args.Length == 2)
                    isAmountValid = int.TryParse(args[1], out amount);
                else
                {
                    isAmountValid = true;
                    amount = 1;
                }

                if(itemDef != null && isAmountValid)
                {
                    tradeItem = new TradeItem(itemDef, amount);
                    trader.OfferItem(tradeItem);
                    return;
                }
                else if(itemDef == null)
                {
                    PrintToChat(player, s_NoItemFound);
                    return;
                }
            }

            PrintToChat(player, s_WrongOffer);
        }


        [ChatCommand("tror")]
        private void RemoveOffer(BasePlayer player, string command, string[] args)
        {
            Trader trader = Trader.GetTrader(player);

            if(trader == null)
                return;

            TradeSession session = trader.GetTradeSession();

            if(session == null)
                return;

            trader.RemoveLastOffer();
        }

        [ChatCommand("trob")]
        private void OfferBlueprint(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();

            if(trader == null)
                return;

            TradeSession session = trader.GetTradeSession();

            if(session == null || !session.IsAccepted())
                return;

            TradeItem tradeItem;
            ItemDefinition itemDef;

            if(args.Length == 1)
            {
                itemDef = FindItemByDisplayName(args[0]);

                if(itemDef != null)
                {
                    tradeItem = new TradeItem(itemDef, 1, true);
                    trader.OfferItem(tradeItem);
                }
                else
                {
                    PrintToChat(player, s_NoItemFound);
                }

                return;
            }

            PrintToChat(player, s_WrongOfferBlueprint);
        }

        [ChatCommand("trd")]
        private void DeclineTrade(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();
            Trader other;

            if(trader == null)
                return;

            TradeSession session = trader.GetTradeSession();

            if(session != null)
            {
                other = trader.GetOther();
                session.CloseSession();

                if(other != null)
                    PrintToChat(other.GetPlayer(), string.Format(s_TradeRequestDeclined, player.displayName));
            }
        }

        [ChatCommand("trl")]
        private void LockTradeOffer(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();

            if(trader != null && trader.GetTradeSession() != null)
                trader.AcceptTrade();
        }

        [ChatCommand("trr")]
        private void SendTradeRequest(BasePlayer player, string command, string[] args)
        {
            Trader trader = player.GetComponent<Trader>();
            BasePlayer partner = null;

            if(args.Length == 1)
                partner = GetPlayerByName(args[0]);
            else
            {
                PrintToChat(player, s_WrongTradeRequest);
                return;
            }

            if(trader != null)
            {
                if(partner == null || partner == player)
                    PrintToChat(player, string.Format(s_TradeRequestNotFound, args[0]));
                else
                {
                    trader.RequestTrade(partner);
                }
            }
        }

        #endregion

        class Trader : MonoBehaviour
        {
            private BasePlayer player;
            private TradeSession currentTradeSession;

            private float timeOfRequest;
            private float timeOfLastTrade;

            private List<TradeItem> offeredItems = new List<TradeItem>();
            private bool tradeAccepted;

            void Awake()
            {
                player = GetComponent<BasePlayer>();

                if(player == null)
                {
                    Debug.LogError("Trader is not a BasePlayer");
                    GameObject.Destroy(this);
                }
            }

            void Update()
            {
                if(currentTradeSession != null)
                {
                    if(!IsCloseEnoughTo(GetOther()))
                    {
                        player.ChatMessage(s_TooFar);
                        GetOther().GetPlayer().ChatMessage(s_TooFar);
                        currentTradeSession.CloseSession();
                    }

                    if(!currentTradeSession.IsAccepted() && Time.time - timeOfRequest > timeOut)
                    {
                        currentTradeSession.TimeOut();
                    }
                }
            }

            public BasePlayer GetPlayer()
            {
                return player;
            }

            public TradeSession GetTradeSession()
            {
                return currentTradeSession;
            }

            public List<TradeItem> GetOfferedItems()
            {
                return offeredItems;
            }

            public void SetTradeSession(TradeSession session)
            {
                currentTradeSession = session;
            }

            public bool IsCloseEnoughTo(Trader other)
            {
                if(tradeDistance <= 0)
                    return true;

                Vector3 playerPos = player.transform.position;
                Vector3 otherPos = other.transform.position;

                if(Vector3.Distance(playerPos, otherPos) <= tradeDistance)
                    return true;
                else
                    return false;
            }

            public void RequestTrade(Trader partner)
            {
                TradeSession newTrade;
                float lastTradeTimePassed = Time.time - partner.GetTimeOfLastTrade();
                bool abort = false;

                if(lastTradeTimePassed < tradeCooldown)
                {
                    player.ChatMessage(partner.GetPlayer().displayName + " cannot trade for " + (int)(tradeCooldown - lastTradeTimePassed) + " seconds");
                    abort = true;
                }

                lastTradeTimePassed = Time.time - timeOfLastTrade;
                if(lastTradeTimePassed < tradeCooldown)
                {
                    player.ChatMessage("You cannot trade for " + (int)(tradeCooldown - lastTradeTimePassed) + " seconds");
                    abort = true;
                }

                if(abort)
                    return;

                if(this.GetTradeSession() != null)
                {
                    player.ChatMessage(s_TradeRequestPlayerBusy);
                    return;
                }

                if(partner.GetTradeSession() != null)
                {
                    player.ChatMessage(string.Format(s_TradeRequestOtherBusy, partner.GetPlayer().displayName));
                    return;
                }

                partner.GetPlayer().ChatMessage(string.Format(s_TradeRequestOther, player.displayName));
                player.ChatMessage(string.Format(s_TradeRequestPlayer, partner.GetPlayer().displayName));

                timeOfRequest = Time.time;
                partner.SetTimeOfRequest(Time.time);
                newTrade = new TradeSession(this, partner);
                this.SetTradeSession(newTrade);
                partner.SetTradeSession(newTrade);
            }

            public void SetTimeOfRequest(float time)
            {
                timeOfRequest = time;
            }

            public void SetTimeOfLastTrade(float time)
            {
                timeOfLastTrade = time;
            }

            public float GetTimeOfLastTrade()
            {
                return timeOfLastTrade;
            }

            public void RequestTrade(BasePlayer partner)
            {
                Trader partnerTrader = partner.GetComponent<Trader>();

                if(partnerTrader != null)
                    this.RequestTrade(partnerTrader);
            }

            public void ClearTradeSession()
            {
                tradeAccepted = false;
                offeredItems.Clear();
                currentTradeSession = null;
                DestroyTradeGUI();
            }

            public static Trader GetTrader(BasePlayer player)
            {
                if(player == null)
                    return null;

                return player.GetComponent<Trader>();
            }

            public bool GetTradeAccepted()
            {
                return tradeAccepted;
            }

            private void OnItemOffered()
            {
                tradeAccepted = false;
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerAccepted", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherAccepted", null, null, null, null));
            }

            public void AcceptTrade()
            {
                if(tradeAccepted)
                    return;

                Trader other = GetOther();

                tradeAccepted = true;
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(Trader.playerAcceptIndicatorGUI, null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = other.GetPlayer().net.connection }, null, "AddUI", new Facepunch.ObjectList(Trader.otherAcceptIndicatorGUI, null, null, null, null));


                if(tradeAccepted && other.GetTradeAccepted())
                    currentTradeSession.MakeTrade();
            }

            public void OfferItem(TradeItem item)
            {
                if(this.HasItem(item))
                    offeredItems.Add(item);
                else
                {
                    player.ChatMessage(string.Format(s_TradeOfferFailed, item.GetItemDef().displayName.english + " " + item.GetAmount() + "x"));
                    return;
                }

                currentTradeSession.GetPartner().OnItemOffered();
                currentTradeSession.GetInitiator().OnItemOffered();

                UpdateOfferGUI();
            }

            public void RemoveLastOffer()
            {
                if(currentTradeSession == null)
                    return;

                if(offeredItems.Count > 0)
                    offeredItems.Remove(offeredItems[offeredItems.Count-1]);

                currentTradeSession.GetPartner().OnItemOffered();
                currentTradeSession.GetInitiator().OnItemOffered();
                UpdateOfferGUI();
            }

            private bool HasItem(TradeItem item)
            {
                if(item.IsBlueprint())
                {
                    Item[] items = player.inventory.AllItems();

                    for(int i = 0; i < items.Length; i++)
                    {
                        if(items[i].info.itemid == item.GetItemDef().itemid && items[i].IsBlueprint())
                            return true;
                    }
                }
                else
                {
                    int hasAmount;
                    int tradedAmount = 0;
                    hasAmount = player.inventory.GetAmount(item.GetItemDef().itemid);

                    foreach(TradeItem offered in offeredItems)
                    {
                        if(offered.GetItemDef().itemid == item.GetItemDef().itemid)
                            tradedAmount += offered.GetAmount();
                    }

                    if(hasAmount - tradedAmount >= item.GetAmount())
                        return true;
                }
                return false;
            }

            public void CheckOfferedItems()
            {
                if(currentTradeSession == null)
                    return;

                if(currentTradeSession.IsMakingTrade())
                    return;

                List<TradeItem> offersToRemove = new List<TradeItem>();

                foreach(TradeItem item in offeredItems)
                {
                    if(!HasItem(item))
                        offersToRemove.Add(item);
                }

                foreach(TradeItem item in offersToRemove)
                {
                    offeredItems.Remove(item);
                }

                UpdateOfferGUI();
            }

            private void UpdateOfferGUI()
            {
                BasePlayer other = GetOther().GetPlayer();
                string s_OfferedItems = TradeItem.ListToString(offeredItems);

                string PlayerGUIMessage = playerOfferIndicatorGUI.Replace("{s_OfferedItems}", s_OfferedItems);
                string OtherGUIMessage = otherOfferIndicatorGUI.Replace("{s_OfferedItems}", s_OfferedItems);

                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerOfferList", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = other.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherOfferList", null, null, null, null));

                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(PlayerGUIMessage, null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = other.net.connection }, null, "AddUI", new Facepunch.ObjectList(OtherGUIMessage, null, null, null, null));
            }

            public Trader GetOther()
            {
                if(currentTradeSession != null)
                {
                    if(this.IsInitiator())
                        return currentTradeSession.GetPartner();
                    else
                        return currentTradeSession.GetInitiator();
                }

                return null;
            }

            public bool IsInitiator()
            {
                if(currentTradeSession != null)
                {
                    if(currentTradeSession.GetInitiator() == this)
                        return true;
                }

                return false;
            }

            public void CreateTradeGUI()
            {
                string partnerIndicatorGUIMessage = partnerIndicatorGUI.Replace("{s_PartnerIndicator}", string.Format(s_TradePartner, GetOther().GetPlayer().displayName));

                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(baseTradeGUI, null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "AddUI", new Facepunch.ObjectList(partnerIndicatorGUIMessage, null, null, null, null));
            }

            public void DestroyTradeGUI()
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("TradePanel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerPanel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherPanel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("InfoPanel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("InfoLabel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerAccepted", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherAccepted", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerOfferLabel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherOfferLabel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PlayerOfferList", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("OtherOfferList", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PartnerLabelPanel", null, null, null, null));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo() { connection = player.net.connection }, null, "DestroyUI", new Facepunch.ObjectList("PartnerLabel", null, null, null, null));
            }

            #region JSON

            public static string partnerIndicatorGUI = @"[
                            {
                                ""parent"": ""PartnerLabelPanel"",
                                ""name"": ""PartnerLabel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_PartnerIndicator}"",
                                        ""fontSize"":22,
                                        ""fadeIn"": ""0.5"",
                                        ""align"": ""MiddleCenter""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0"",
                                        ""anchormax"": ""1 1""
                                    }
                                ]
                            }
                        ]
                        ";


            public static string playerAcceptIndicatorGUI = @"[
                            {
                                ""parent"": ""PlayerPanel"",
                                ""name"": ""PlayerAccepted"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_PlayerReady}"",
                                        ""fontSize"":24,
                                        ""align"": ""LowerCenter"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.1 1.0 0.1 1.0""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.02"",
                                        ""anchormax"": ""1 0.2""
                                    }
                                ]
                            }
                        ]
                        ";


            public static string otherAcceptIndicatorGUI = @"[
                            {
                                ""parent"": ""OtherPanel"",
                                ""name"": ""OtherAccepted"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_OtherReady}"",
                                        ""fontSize"":24,
                                        ""align"": ""LowerCenter"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.1 1.0 0.1 1.0""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.02"",
                                        ""anchormax"": ""1 0.2""
                                    }
                                ]
                            }
                        ]
                        ";

            public static string playerOfferIndicatorGUI = @"[
                            {
                                ""parent"": ""PlayerPanel"",
                                ""name"": ""PlayerOfferList"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_OfferedItems}"",
                                        ""fontSize"":20,
                                        ""align"": ""UpperCenter"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""1.0 0.4 0.0 1.0""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.1"",
                                        ""anchormax"": ""1 0.9""
                                    }
                                ]
                            }
                        ]
                        ";


            public static string otherOfferIndicatorGUI = @"[
                            {
                                ""parent"": ""OtherPanel"",
                                ""name"": ""OtherOfferList"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_OfferedItems}"",
                                        ""fontSize"":20,
                                        ""align"": ""UpperCenter"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""1.0 0.4 0.0 1.0""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.1"",
                                        ""anchormax"": ""1 0.9""
                                    }
                                ]
                            }
                        ]
                        ";



            public static string baseTradeGUI = @"[
                            {
                                ""parent"": ""HUD/Overlay"",
                                ""name"": ""PartnerLabelPanel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Image"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.5 0.5 0.5 0.5""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0.35 0.92"",
                                        ""anchormax"": ""0.75 0.98"",
                                    }
                                ]
                            },
                            {
                                ""parent"": ""HUD/Overlay"",
                                ""name"": ""InfoPanel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Image"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.5 0.5 0.5 0.5""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0.78 0.3"",
                                        ""anchormax"": ""0.98 0.5""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""InfoPanel"",
                                ""name"": ""InfoLabel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""Chat commands:
Offer items: /tro ''name'' amount
Offer blueprint: /trob ''name''
Remove last offer: /tror
Lock/Accept trade offer: /trl
Cancel trade: /trd"",
                                        ""fontSize"":16,
                                        ""fadeIn"": ""0.5"",
                                        ""align"": ""MiddleCenter""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0"",
                                        ""anchormax"": ""1 1""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""HUD/Overlay"",
                                ""name"": ""TradePanel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Image"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.5 0.5 0.5 0.5""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0.35 0.15"",
                                        ""anchormax"": ""0.75 0.9""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""TradePanel"",
                                ""name"": ""PlayerPanel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Image"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.5 0.5 0.5 0.5""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0.05 0.05"",
                                        ""anchormax"": ""0.475 0.95""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""TradePanel"",
                                ""name"": ""OtherPanel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Image"",
                                        ""fadeIn"": ""0.5"",
                                        ""color"": ""0.5 0.5 0.5 0.5""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0.525 0.05"",
                                        ""anchormax"": ""0.95 0.95""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""PlayerPanel"",
                                ""name"": ""PlayerOfferLabel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_YouGive}"",
                                        ""fontSize"":24,
                                        ""fadeIn"": ""0.5"",
                                        ""align"": ""UpperCenter""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.9"",
                                        ""anchormax"": ""1 1""
                                    }
                                ]
                            },
                            {
                                ""parent"": ""OtherPanel"",
                                ""name"": ""OtherOfferLabel"",
                                ""fadeOut"": ""0.5"",
                                ""components"":
                                [
                                    {
                                        ""type"":""UnityEngine.UI.Text"",
                                        ""text"":""{s_YouGet}"",
                                        ""fontSize"":24,
                                        ""fadeIn"": ""0.5"",
                                        ""align"": ""UpperCenter""
                                    },
                                    {
                                        ""type"":""RectTransform"",
                                        ""anchormin"": ""0 0.9"",
                                        ""anchormax"": ""1 1""
                                    }
                                ]
                            }


                        ]
                        ";
            #endregion
        }


        private void LoadGUITranslation()
        {
            Trader.baseTradeGUI = Trader.baseTradeGUI.Replace("{s_YouGet}", s_YouGet);
            Trader.baseTradeGUI = Trader.baseTradeGUI.Replace("{s_YouGive}", s_YouGive);
            Trader.playerAcceptIndicatorGUI = Trader.playerAcceptIndicatorGUI.Replace("{s_PlayerReady}", s_PlayerReady);
            Trader.otherAcceptIndicatorGUI = Trader.otherAcceptIndicatorGUI.Replace("{s_OtherReady}", s_OtherReady);
        }

        class TradeSession
        {
            private Trader initiator;
            private Trader partner;

            private bool requestAccepted;
            private bool makingTrade;

            public TradeSession(Trader initiator, Trader partner)
            {
                this.initiator = initiator;
                this.partner = partner;
            }

            public void TimeOut()
            {
                initiator.GetPlayer().ChatMessage(string.Format(s_TimeOut, partner.GetPlayer().displayName));
                partner.GetPlayer().ChatMessage(s_TimeOutOther);
                CloseSession();
            }



            public void AcceptRequest()
            {
                if(requestAccepted)
                    return;

                requestAccepted = true;
                initiator.CreateTradeGUI();
                partner.CreateTradeGUI();
            }

            public void CloseSession()
            {
                initiator.ClearTradeSession();
                partner.ClearTradeSession();
            }

            public bool IsMakingTrade()
            {
                return makingTrade;
            }

            public bool IsAccepted()
            {
                return requestAccepted;
            }

            public void MakeTrade()
            {
                makingTrade = true;

                List<TradeItem> initiatorOffered = initiator.GetOfferedItems();
                List<TradeItem> partnerOffered = partner.GetOfferedItems();

                TransferOffered(initiator, partner);
                TransferOffered(partner, initiator);

                if(initiatorOffered.Count > 0 || partnerOffered.Count > 0)
                {
                    initiator.SetTimeOfLastTrade(Time.time);
                    partner.SetTimeOfLastTrade(Time.time);

                    initiator.GetPlayer().ChatMessage(s_TradeSuccessful);
                    partner.GetPlayer().ChatMessage(s_TradeSuccessful);
                }

                CloseSession();
                makingTrade = false;
            }

            public static void TransferOffered(Trader from, Trader to)
            {
                int amountLeft;
                Item itemToGive;
                PlayerInventory fromInventory = from.GetPlayer().inventory;
                PlayerInventory toInventory = to.GetPlayer().inventory;

                List<TradeItem> offered = from.GetOfferedItems();

                Item[] offeredItems;

                foreach(TradeItem item in offered)
                {
                    itemToGive = null;
                    amountLeft = item.GetAmount();
                    offeredItems = fromInventory.AllItems();

                    for(int i = 0; i < offeredItems.Length; i++)
                    {
                        if(amountLeft <= 0)
                            break;

                        if(offeredItems[i].info.itemid == item.GetItemDef().itemid && item.IsBlueprint() == offeredItems[i].IsBlueprint())
                        {
                            itemToGive = offeredItems[i];

                            if(!item.IsBlueprint())
                            {
                                if(offeredItems[i].amount > amountLeft)
                                    itemToGive = itemToGive.SplitItem(amountLeft);

                                amountLeft -= itemToGive.amount;
                            }

                            if(!toInventory.GiveItem(itemToGive))
                            {
                                itemToGive.Drop(to.GetPlayer().transform.position, Vector3.zero);
                            }
                        }
                    }
                }
            }

            public Trader GetInitiator()
            {
                return initiator;
            }

            public Trader GetPartner()
            {
                return partner;
            }
        }

        class TradeItem
        {
            private ItemDefinition itemDef;
            private int amount;
            private bool isBlueprint;
            private bool isMoney;

            public TradeItem(ItemDefinition itemDef, int amount, bool isBlueprint = false, bool isMoney = false)
            {
                this.itemDef = itemDef;
                this.amount = amount;
                this.isBlueprint = isBlueprint;
                this.isMoney = isMoney;
            }

            public ItemDefinition GetItemDef()
            {
                return itemDef;
            }

            public int GetAmount()
            {
                return amount;
            }

            public bool IsBlueprint()
            {
                return isBlueprint;
            }

            public bool IsMoney()
            {
                return isMoney;
            }

            public static string ListToString(List<TradeItem> itemList)
            {
                string str = "";

                foreach(TradeItem item in itemList)
                {
                    if(!item.IsMoney())
                        str = str + item.GetItemDef().displayName.english;
                    else
                        str = str + "Money";

                    if(item.IsBlueprint())
                    {
                        str = str + "(Blueprint)\n";
                    }
                    else
                    {
                        str = str + "("+item.GetAmount()+"x)\n";
                    }
                }

                return str;
            }
        }

        private ItemDefinition FindItemByDisplayName(string name)
        {
            List<ItemDefinition> itemList = ItemManager.GetItemDefinitions();
            ItemDefinition foundItem = null;
            name = name.ToLower();
            string current;
            string last;

            foreach(ItemDefinition item in itemList)
            {
                current = item.displayName.english.ToLower();
                if(current.Contains(name))
                {
                    if(foundItem != null)
                    {
                        last = foundItem.displayName.english.ToLower();
                        if(last.Replace(name, "").Length > current.Replace(name, "").Length)
                        {
                            foundItem = item;
                        }
                    }
                    else
                        foundItem = item;
                }
            }

            return foundItem;
        }

        private BasePlayer GetPlayerByName(string name)
        {
            string currentName;
            string lastName;
            BasePlayer foundPlayer = null;
            name = name.ToLower();

            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                currentName = player.displayName.ToLower();

                if(currentName.Contains(name))
                {
                    if(foundPlayer != null)
                    {
                        lastName = foundPlayer.displayName;
                        if(currentName.Replace(name, "").Length < lastName.Replace(name, "").Length)
                        {
                            foundPlayer = player;
                        }
                    }

                    foundPlayer = player;
                }
            }

            return foundPlayer;
        }
    }
}
