using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Quests", "k1lly0u", "2.1.72", ResourceId = 1084)]
    public class Quests : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin HumanNPC;
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;
        [PluginReference] Plugin LustyMap;
        [PluginReference] Plugin EventManager;
        [PluginReference] Plugin HuntPlugin;
        [PluginReference] Plugin PlayerChallenges;
        [PluginReference] Plugin BetterChat;

        ConfigData configData;

        QuestData questData;
        PlayerData playerData;
        NPCData vendors;
        ItemNames itemNames;
        private DynamicConfigFile Quest_Data;
        private DynamicConfigFile Player_Data;
        private DynamicConfigFile Quest_Vendors;
        private DynamicConfigFile Item_Names;

        private static FieldInfo serverinput;

        private Dictionary<ulong, PlayerQuestData> PlayerProgress;
        private Dictionary<QuestType, Dictionary<string, QuestEntry>> Quest;

        private Dictionary<string, ItemDefinition> ItemDefs;
        private Dictionary<string, string> DisplayNames = new Dictionary<string, string>();

        private Dictionary<ulong, QuestCreator> ActiveCreations = new Dictionary<ulong, QuestCreator>();
        private Dictionary<ulong, QuestCreator> ActiveEditors = new Dictionary<ulong, QuestCreator>();

        private Dictionary<ulong, bool> AddVendor = new Dictionary<ulong, bool>();

        private Dictionary<QuestType, List<string>> AllObjectives = new Dictionary<QuestType, List<string>>();
        private Dictionary<uint, Dictionary<ulong, int>> HeliAttackers = new Dictionary<uint, Dictionary<ulong, int>>();

        private Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();
        private Dictionary<uint, ulong> Looters = new Dictionary<uint, ulong>();

        private List<ulong> StatsMenu = new List<ulong>();
        private List<ulong> OpenMenuBind = new List<ulong>();
        private static readonly FieldInfo NPCdisplayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

        static string UIMain = "UIMain";
        static string UIPanel = "UIPanel";
        static string UIEntry = "UIEntry";      

        #endregion

        #region Classes
        class PlayerQuestData
        {
            public Dictionary<string, PlayerQuestInfo> Quests = new Dictionary<string, PlayerQuestInfo>();
            public List<QuestInfo> RequiredItems = new List<QuestInfo>();
            public ActiveDelivery CurrentDelivery = new ActiveDelivery();
        }
        class PlayerQuestInfo
        {            
            public QuestStatus Status;
            public QuestType Type;
            public int AmountCollected = 0;
            public bool RewardClaimed = false;
            public double ResetTime = 0;
        }
        class QuestEntry
        {
            public string QuestName;
            public string Description;
            public string Objective;
            public string ObjectiveName;
            public int AmountRequired;
            public int Cooldown;
            public bool ItemDeduction;
            public List<RewardItem> Rewards;
        }          
        class NPCInfo
        {
            public float x;
            public float z;
            public string ID;
            public string Name;
        }
        class DeliveryInfo
        {
            public string Description;
            public NPCInfo Info;
            public RewardItem Reward;
            public float Multiplier;
        }
        class ActiveDelivery
        {
            public string VendorID;
            public string TargetID;
            public float Distance;
        }
        class QuestInfo
        {
            public string ShortName;
            public QuestType Type;
        }      
        class RewardItem
        {
            public bool isRP = false;
            public bool isCoins = false;
            public bool isHuntXP = false;
            public string DisplayName;
            public string ShortName;
            public int ID;
            public float Amount;
            public bool BP;
            public ulong Skin;
        }
        class QuestCreator
        {
            public QuestType type;
            public QuestEntry entry;
            public DeliveryInfo deliveryInfo;
            public RewardItem item;
            public string oldEntry;            
            public int partNum;  
        }
        class ItemNames
        {
            public Dictionary<string, string> DisplayNames = new Dictionary<string, string>();
        }

        enum QuestType
        {            
            Kill,
            Craft,
            Gather,
            Loot,
            Delivery
        }
        enum QuestStatus
        {
            Pending,
            Completed,
            Open
        }        
        #endregion

        #region UI Creation
        class QUI
        {
            public static ConfigData configdata;
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
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
                    new CuiElement().Parent = "Overlay",
                    panelName
                }
            };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateTextOverlay(ref CuiElementContainer container, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                if (configdata.DisableUI_FadeIn)
                    fadein = 0;
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
        }
        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"light", "0.7 0.7 0.7 0.3" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttonopen", "0.2 0.8 0.2 0.9" },
            {"buttoncompleted", "0 0.5 0.1 0.9" },
            {"buttonred", "0.85 0 0.35 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" },
            {"grey8", "0.8 0.8 0.8 1.0" }
        };
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            Quest_Data = Interface.Oxide.DataFileSystem.GetFile("Quests/quests_data");
            Player_Data = Interface.Oxide.DataFileSystem.GetFile("Quests/quests_players");
            Quest_Vendors = Interface.Oxide.DataFileSystem.GetFile("Quests/quests_vendors");
            Item_Names = Interface.Oxide.DataFileSystem.GetFile("Quests/quests_itemnames");
            lang.RegisterMessages(Localization, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            QUI.configdata = configData;
            ItemDefs = ItemManager.itemList.ToDictionary(i => i.shortname);
            FillObjectiveList();
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            timer.Once(900, () => SaveLoop());
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
            SavePlayerData();           
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (configData.Autoset_KeyBind)
            {
                if (!string.IsNullOrEmpty(configData.KeyBind_Key))
                {
                    player.Command("bind " + configData.KeyBind_Key + " QUI_OpenQuestMenu");
                }
            }
        }
        #region Objective Hooks        
        //Kill
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity != null)
                {
                    BasePlayer player = null;

                    if (info?.Initiator is BasePlayer)
                        player = info?.Initiator?.ToPlayer();

                    else if (entity.GetComponent<BaseHelicopter>() != null)
                        player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));

                    if (player != null)
                    {                        
                        if (isPlaying(player)) return;
                        if (hasQuests(player.userID) && isQuestItem(player.userID, entity?.ShortPrefabName, QuestType.Kill))
                            ProcessProgress(player, QuestType.Kill, entity?.ShortPrefabName);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {            
            if (victim.GetComponent<BaseHelicopter>() != null && info?.Initiator?.ToPlayer() != null)
            {
                var heli = victim.GetComponent<BaseHelicopter>();
                var player = info.Initiator.ToPlayer();
                NextTick(() =>
                {
                    if (!HeliAttackers.ContainsKey(heli.net.ID))
                        HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
                    if (!HeliAttackers[heli.net.ID].ContainsKey(player.userID))
                        HeliAttackers[heli.net.ID].Add(player.userID, 0);
                    HeliAttackers[heli.net.ID][player.userID]++;
                });
            }
        }
        // Gather
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player != null)
                if (hasQuests(player.userID) && isQuestItem(player.userID, item.info.shortname, QuestType.Gather))
                    ProcessProgress(player, QuestType.Gather, item.info.shortname, item.amount);
        }
        void OnPlantGather(PlantEntity plant, Item item, BasePlayer player)
        {
            if (player != null)
                if (hasQuests(player.userID) && isQuestItem(player.userID, item.info.shortname, QuestType.Gather))
                    ProcessProgress(player, QuestType.Gather, item.info.shortname, item.amount);
        }
        void OnCollectiblePickup(Item item, BasePlayer player)
        {
           if (player != null)
                if (hasQuests(player.userID) && isQuestItem(player.userID, item.info.shortname, QuestType.Gather))
                    ProcessProgress(player, QuestType.Gather, item.info.shortname, item.amount);
        }
        //Craft
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            var player = task.owner;
            if (player != null)
                if (hasQuests(player.userID) && isQuestItem(player.userID, item.info.shortname, QuestType.Craft))
                    ProcessProgress(player, QuestType.Craft, item.info.shortname, item.amount);
        }
        //Loot
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (Looters.ContainsKey(item.uid))
            {
                if (container.playerOwner != null)
                {
                    if (Looters[item.uid] != container.playerOwner.userID)
                    {
                        if (hasQuests(container.playerOwner.userID) && isQuestItem(container.playerOwner.userID, item.info.shortname, QuestType.Loot))
                        {
                            ProcessProgress(container.playerOwner, QuestType.Loot, item.info.shortname, item.amount);
                            Looters.Remove(item.uid);
                        }
                    }
                }                
            }
            else if (container.playerOwner != null) Looters.Add(item.uid, container.playerOwner.userID);
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            ulong id = 0U;
            if (container.entityOwner != null)
                id = container.entityOwner.OwnerID;
            else if (container.playerOwner != null)
                id = container.playerOwner.userID;

            if (!Looters.ContainsKey(item.uid))
                Looters.Add(item.uid, id);
        }
        // Delivery and Vendors
        void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (player == null || npc == null) return;
            CheckPlayerEntry(player);
            var npcID = npc.UserIDString;
            if (vendors.QuestVendors.ContainsKey(npcID) && configData.UseNPCVendors)
            {                
                CreateMenu(player);
                return;
            }
            if (vendors.DeliveryVendors.ContainsKey(npcID))
            {
                if (hasQuests(player.userID) && PlayerProgress[player.userID].CurrentDelivery.TargetID == npc.UserIDString)                
                    AcceptDelivery(player, npcID, 1);
                
                if (hasQuests(player.userID) && string.IsNullOrEmpty(PlayerProgress[player.userID].CurrentDelivery.TargetID))                
                    AcceptDelivery(player, npcID);                
                else SendMSG(player, LA("delInprog", player.UserIDString), LA("Quests", player.UserIDString));                
            }            
        }
        #endregion    
        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (BetterChat) return null;

            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;

            if (ActiveEditors.ContainsKey(player.userID) || ActiveCreations.ContainsKey(player.userID) || AddVendor.ContainsKey(player.userID))
            {
                QuestChat(player,arg.Args);
                return false;
            }
            return null;
        }
        object OnBetterChat(IPlayer iplayer, string message)
        {
            var player = iplayer.Object as BasePlayer;
            if (player == null) return message;
            if (ActiveEditors.ContainsKey(player.userID) || ActiveCreations.ContainsKey(player.userID) || AddVendor.ContainsKey(player.userID))
            {
                QuestChat(player, message.Split(' '));
                return true;
            }
            return message;
        }
        void QuestChat(BasePlayer player, string[] arg)
        {
            bool isEditing = false;
            bool isCreating = false;
            QuestCreator Creator = new QuestCreator();
            QuestEntry Quest = new QuestEntry();

            if (ActiveEditors.ContainsKey(player.userID))
            {
                isEditing = true;
                Creator = ActiveEditors[player.userID];
                Quest = Creator.entry;
            }
            else if (ActiveCreations.ContainsKey(player.userID))
            {
                isCreating = true;
                Creator = ActiveCreations[player.userID];
                Quest = Creator.entry;
            }
            if (AddVendor.ContainsKey(player.userID) && string.Join(" ", arg).Contains("exit"))
            {
                ExitQuest(player, true);
                return;
            }

            if (!isEditing && !isCreating)
                return;

            var args = string.Join(" ", arg);
            if (args.Contains("exit"))
            {
                ExitQuest(player, isCreating);
                return;
            }

            if (args.Contains("quest item"))
            {
                var item = GetItem(player);
                if (item != null)
                {
                    if (Creator.type != QuestType.Delivery)
                    {
                        Quest.Rewards.Add(item);
                        Creator.partNum++;
                        if (isCreating)
                            CreationHelp(player, 7);
                        else if (isEditing)
                        {
                            SaveRewardsEdit(player);
                            CreationHelp(player, 10);
                        }
                    }
                    else
                    {
                        Creator.deliveryInfo.Reward = item;
                        DeliveryHelp(player, 4);
                    }
                }
                else SendMSG(player, $"{LA("noAItem", player.UserIDString)}'quest item'", LA("QC", player.UserIDString));

                return;
            }

            switch (Creator.partNum)
            {
                case 0:
                    foreach (var type in questData.Quest)
                    {
                        if (type.Value.ContainsKey(args))
                        {
                            SendMSG(player, LA("nameExists", player.UserIDString), LA("QC", player.UserIDString));
                            return;
                        }
                    }
                    Quest.QuestName = args;
                    SendMSG(player, args, "Name:");
                    Creator.partNum++;
                    if (isCreating)
                        CreationHelp(player, 1);
                    else CreationHelp(player, 6);
                    return;
                case 2:
                    {
                        int amount;
                        if (!int.TryParse(arg[0], out amount))
                        {
                            SendMSG(player, LA("objAmount", player.UserIDString), LA("QC", player.UserIDString));
                            return;
                        }
                        Quest.AmountRequired = amount;
                        SendMSG(player, args, LA("OA", player.UserIDString));
                        Creator.partNum++;
                        if (isCreating)
                            CreationHelp(player, 3);
                        else CreationHelp(player, 6);
                    }
                    return;
                case 3:
                    {
                        if (Creator.type == QuestType.Delivery)
                        {
                            Creator.deliveryInfo.Description = args;
                            SendMSG(player, args, LA("Desc", player.UserIDString));
                            DeliveryHelp(player, 6);
                            return;
                        }
                        Quest.Description = args;
                        SendMSG(player, args, LA("Desc", player.UserIDString));
                        Creator.partNum++;
                        if (isCreating)
                            CreationHelp(player, 4);
                        else CreationHelp(player, 6);
                    }
                    return;
                case 5:
                    {
                        if (Creator.type == QuestType.Delivery)
                        {
                            float amount;
                            if (!float.TryParse(arg[0], out amount))
                            {
                                SendMSG(player, LA("noRM", player.UserIDString), LA("QC", player.UserIDString));
                                return;
                            }
                            Creator.deliveryInfo.Multiplier = amount;

                            SendMSG(player, args, LA("RM", player.UserIDString));
                            Creator.partNum++;
                            DeliveryHelp(player, 5);
                        }
                        else
                        {
                            int amount;
                            if (!int.TryParse(arg[0], out amount))
                            {
                                SendMSG(player, LA("noRA", player.UserIDString), LA("QC", player.UserIDString));
                                return;
                            }
                            Creator.item.Amount = amount;
                            Quest.Rewards.Add(Creator.item);
                            Creator.item = new RewardItem();
                            SendMSG(player, args, LA("RA", player.UserIDString));
                            Creator.partNum++;
                            if (isCreating)
                                CreationHelp(player, 7);
                            else if (isEditing)
                            {
                                SaveRewardsEdit(player);
                            }
                        }
                        return;
                    }
                case 6:
                    {
                        int amount;
                        if (!int.TryParse(arg[0], out amount))
                        {
                            SendMSG(player, LA("noCD", player.UserIDString), LA("QC", player.UserIDString));
                            return;
                        }
                        Creator.entry.Cooldown = amount;
                        SendMSG(player, args, LA("CD1", player.UserIDString));
                        CreationHelp(player, 6);
                    }
                    return;
                default:
                    break;
            }            
        }
        #endregion

        #region External Calls
        private bool isPlaying(BasePlayer player)
        {
            if (EventManager)
            {
                var inEvent = EventManager?.Call("isPlaying", player);
                if (inEvent is bool && (bool)inEvent)
                    return true;
            }          
            return false;
        }
        private void CloseMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("DisableMaps", player);                
            }                
        }
        private void OpenMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("EnableMaps", player);
            }
        }
        private void AddMapMarker(float x, float z, string name, string icon = "special")
        {
            if (LustyMap)
            {
                LustyMap.Call("AddMarker", x, z, name, icon);
                LustyMap.Call("addCustom", icon);
                LustyMap.Call("cacheImages");                
            }
        }
        private void RemoveMapMarker(string name)
        {
            if (LustyMap)
                LustyMap.Call("RemoveMarker", name);
        }
        private object CanTeleport(BasePlayer player)
        {
            if (!PlayerProgress.ContainsKey(player.userID)) return null;

            if (!string.IsNullOrEmpty(PlayerProgress[player.userID].CurrentDelivery.TargetID))
            {
                return LA("NoTP", player.UserIDString);
            }
            else
                return null;
        }
        #endregion

        #region Objective Lists
        private void FillObjectiveList()
        {            
            AllObjectives.Add(QuestType.Loot, new List<string>());
            AllObjectives.Add(QuestType.Craft, new List<string>());
            AllObjectives.Add(QuestType.Kill, new List<string>());
            AllObjectives.Add(QuestType.Gather, new List<string>());
            AllObjectives.Add(QuestType.Delivery, new List<string>());
            GetAllCraftables();
            GetAllItems();
            GetAllKillables();
            GetAllResources();

            if (itemNames.DisplayNames == null || itemNames.DisplayNames.Count < 1)
            {
                foreach (var item in ItemDefs)
                {
                    if (!DisplayNames.ContainsKey(item.Key))
                        DisplayNames.Add(item.Key, item.Value.displayName.translated);
                }
                SaveDisplayNames();
            }
            else DisplayNames = itemNames.DisplayNames;
        }
        private void GetAllItems()
        {
            foreach (var item in ItemManager.itemList)
                AllObjectives[QuestType.Loot].Add(item.shortname);
        }
        private void GetAllCraftables()
        {
            foreach (var bp in ItemManager.bpList)
                if (bp.userCraftable)
                    AllObjectives[QuestType.Craft].Add(bp.targetItem.shortname);
        }
        private void GetAllResources()
        {
            AllObjectives[QuestType.Gather].Add("wood");
            AllObjectives[QuestType.Gather].Add("stones");
            AllObjectives[QuestType.Gather].Add("metal.ore");
            AllObjectives[QuestType.Gather].Add("hq.metal.ore");
            AllObjectives[QuestType.Gather].Add("sulfur.ore");
            AllObjectives[QuestType.Gather].Add("cloth");
            AllObjectives[QuestType.Gather].Add("bone.fragments");
            AllObjectives[QuestType.Gather].Add("crude.oil");
            AllObjectives[QuestType.Gather].Add("fat.animal");
            AllObjectives[QuestType.Gather].Add("leather");
            AllObjectives[QuestType.Gather].Add("skull.wolf");
            AllObjectives[QuestType.Gather].Add("skull.human");
            AllObjectives[QuestType.Gather].Add("chicken.raw");
            AllObjectives[QuestType.Gather].Add("mushroom");
            AllObjectives[QuestType.Gather].Add("meat.boar");
            AllObjectives[QuestType.Gather].Add("bearmeat");
            AllObjectives[QuestType.Gather].Add("humanmeat.raw");
            AllObjectives[QuestType.Gather].Add("wolfmeat.raw");
        }
        private void GetAllKillables()
        {
            AllObjectives[QuestType.Kill] = new List<string>
            {
                "bear",
                "boar",
                "chicken",
                "horse",
                "stag",
                "wolf",
                "autoturret_deployed",
                "patrolhelicopter",
                "player"
            };
            DisplayNames.Add("bear", "Bear");
            DisplayNames.Add("boar", "Boar");
            DisplayNames.Add("chicken", "Chicken");
            DisplayNames.Add("horse", "Horse");
            DisplayNames.Add("stag", "Stag");
            DisplayNames.Add("wolf", "Wolf");
            DisplayNames.Add("autoturret_deployed", "Auto-Turret");
            DisplayNames.Add("patrolhelicopter", "Helicopter");
            DisplayNames.Add("player", "Player");
        }        
       
        #endregion

        #region Functions
        private void ProcessProgress(BasePlayer player, QuestType questType, string type, int amount = 0)
        {
            if (string.IsNullOrEmpty(type)) return;
            var data = PlayerProgress[player.userID];
            if (data.RequiredItems.Count > 0)
            {
                foreach (var entry in data.Quests)
                {
                    if (entry.Value.Status == QuestStatus.Completed) continue;
                    var quest = GetQuest(entry.Key);
                    if (quest != null)
                    {
                        if (type == quest.Objective)
                        {
                            if (amount > 0)
                            {
                                var amountRequired = quest.AmountRequired - entry.Value.AmountCollected;
                                if (amount > amountRequired)
                                    amount = amountRequired;
                                entry.Value.AmountCollected += amount;

                                if (quest.ItemDeduction)
                                    TakeQuestItem(player, type, amount);
                            }
                            else entry.Value.AmountCollected++;

                            if (entry.Value.AmountCollected >= quest.AmountRequired)
                                CompleteQuest(player, entry.Key);
                            return;
                        }
                    }
                }
            }           
        }
        private void TakeQuestItem(BasePlayer player, string item, int amount)
        {
            if (ItemDefs.ContainsKey(item))
            {
                var itemDef = ItemDefs[item];
                NextTick(() => player.inventory.Take(null, itemDef.itemid, amount));
            }
            else PrintWarning($"Unable to find definition for: {item}.");           
        }
        private void CompleteQuest(BasePlayer player, string questName)
        {
            var data = PlayerProgress[player.userID].Quests[questName];
            var items = PlayerProgress[player.userID].RequiredItems;
            var quest = GetQuest(questName);
            if (quest != null)
            {
                int cdTime;
                if (quest.Cooldown > 0)
                    cdTime = quest.Cooldown;
                else cdTime = configData.Quest_Cooldown;

                data.Status = QuestStatus.Completed;
                data.ResetTime = GrabCurrentTime() + (cdTime * 60);

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].ShortName == quest.Objective && items[i].Type == data.Type)
                    {
                        items.Remove(items[i]);
                        break;
                    }
                }
                SendMSG(player, "", $"{LA("qComple", player.UserIDString)} {questName}. {LA("claRew", player.UserIDString)}");
                PlayerChallenges?.Call("CompletedQuest", player);
            }           
        }

        private ItemDefinition FindItemDefinition(string shortname)
        {
            ItemDefinition itemDefinition;
            return ItemDefs.TryGetValue(shortname, out itemDefinition) ? itemDefinition : null;
        }       
        private string GetRewardString(List<RewardItem> entry)
        {
            var rewards = "";
            int i = 1;          
            foreach (var item in entry)
            {
                rewards = rewards + $"{(int)item.Amount}x {item.DisplayName}";
                if (i < entry.Count)
                    rewards = rewards + ", ";
                i++;              
            }
            return rewards; 
        }
        private bool GiveReward(BasePlayer player, List<RewardItem> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward.isCoins && Economics)
                {
                    Economics?.Call("Deposit", player.userID, (int)reward.Amount);
                }
                else if (reward.isRP && ServerRewards)
                {
                    ServerRewards?.Call("AddPoints", player.userID, (int)reward.Amount);
                }
                else if (reward.isHuntXP)
                {
                    HuntPlugin?.Call("GiveEXP", player, (int)reward.Amount);
                }                
                else
                {
                    if (string.IsNullOrEmpty(reward.ShortName)) return true;
                    var definition = FindItemDefinition(reward.ShortName);
                    if (definition != null)
                    {
                        var item = ItemManager.Create(definition, (int)reward.Amount, reward.Skin);
                        if (item != null)
                        {
                            player.inventory.GiveItem(item, player.inventory.containerMain);
                        }
                    }
                    else PrintWarning($"Quests: Error building item {reward.ShortName} for {player.displayName}");                    
                }
            }
            return true;
        }
        private void ReturnItems(BasePlayer player, string itemname, int amount)
        {
            if (amount > 0)
            {
                var definition = FindItemDefinition(itemname);
                if (definition != null)
                {
                    var item = ItemManager.Create(definition, amount);
                    if (item != null)
                    {
                        player.inventory.GiveItem(item);
                        PopupMessage(player, $"{LA("qCancel", player.UserIDString)} {item.amount}x {item.info.displayName.translated} {LA("rewRet", player.UserIDString)}");
                    }
                }
            }
        }
        private RewardItem GetItem(BasePlayer player)
        {
            Item item = player.GetActiveItem();
            if (item == null) return null;
            var newItem = new RewardItem
            {
                Amount = item.amount,                
                DisplayName = DisplayNames[item.info.shortname],
                ID = item.info.itemid,
                ShortName = item.info.shortname,
                Skin = item.skin            
            };
            return newItem;
        }

        private bool hasQuests(ulong player)
        {
            if (PlayerProgress.ContainsKey(player))            
                return true;                       
            return false;
        }
        private bool isQuestItem(ulong player, string name, QuestType type)
        {
            var data = PlayerProgress[player].RequiredItems;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].ShortName == name && data[i].Type == type)
                    return true;
            }
            return false;           
        }
        private void CheckPlayerEntry(BasePlayer player)
        {
            if (!PlayerProgress.ContainsKey(player.userID))            
                PlayerProgress.Add(player.userID, new PlayerQuestData());
        }

        private object GetQuestType(string name)
        {
            foreach (var entry in Quest)
                if (entry.Value.ContainsKey(name))
                    return entry.Key;
            return null;
        }        
        private QuestEntry GetQuest(string name)
        {
            var type = GetQuestType(name);
            if (type != null)
            {
                foreach (var entry in questData.Quest[(QuestType)type])
                {
                    if (entry.Key == name)
                        return entry.Value;
                }
            }
            PrintWarning($"Error retrieving quest info for: {name}");
            return null;
        }

        private void SaveQuest(BasePlayer player, bool isCreating)
        {
            QuestCreator Creator;
            QuestEntry Quest;

            if (isCreating)
                Creator = ActiveCreations[player.userID];
            else Creator = ActiveEditors[player.userID];
            Quest = Creator.entry;

            if (isCreating)
            {                
                if (Creator.type == QuestType.Delivery)
                {
                    var npc = BasePlayer.FindByID(ulong.Parse(Creator.deliveryInfo.Info.ID));
                    if (npc != null)
                    {
                        NPCdisplayName.SetValue(npc, Creator.deliveryInfo.Info.Name);
                        npc.SendNetworkUpdateImmediate();
                    }
                    vendors.DeliveryVendors.Add(Creator.deliveryInfo.Info.ID, Creator.deliveryInfo);
                    AddMapMarker(Creator.deliveryInfo.Info.x, Creator.deliveryInfo.Info.z, Creator.deliveryInfo.Info.Name, $"{configData.Icon_Delivery}_{vendors.DeliveryVendors.Count}");
                    AddVendor.Remove(player.userID);
                    SaveVendorData();
                    DestroyUI(player);
                    if (vendors.DeliveryVendors.Count < 2)
                        PopupMessage(player, LA("minDV", player.UserIDString));
                    SendMSG(player, LA("DVSucc", player.UserIDString), LA("QC", player.UserIDString));
                    OpenMap(player);
                    return;
                }
                else questData.Quest[Creator.type].Add(Quest.QuestName, Quest);
                ActiveCreations.Remove(player.userID);
            }
            else
            {
                questData.Quest[Creator.type].Remove(Creator.oldEntry);
                questData.Quest[Creator.type].Add(Quest.QuestName, Quest);
                ActiveEditors.Remove(player.userID);
            }
            DestroyUI(player);
            SaveQuestData();
            SendMSG(player, $"{LA("saveQ", player.UserIDString)} {Quest.QuestName}", LA("QC", player.UserIDString));
        }
        private void SaveRewardsEdit(BasePlayer player)
        {
            QuestCreator Creator = ActiveEditors[player.userID];
            QuestEntry Quest = Creator.entry;
            questData.Quest[Creator.type].Remove(Creator.entry.QuestName);
            questData.Quest[Creator.type].Add(Quest.QuestName, Quest);

            DestroyUI(player);            
            SaveQuestData();
            CreationHelp(player, 10);
            SendMSG(player, $"{LA("saveQ", player.UserIDString)} {Quest.QuestName}", LA("QC", player.UserIDString));            
        }
        private void ExitQuest(BasePlayer player, bool isCreating)
        {
            if (isCreating)
                ActiveCreations.Remove(player.userID);
            else ActiveEditors.Remove(player.userID);

            SendMSG(player, LA("QCCancel", player.UserIDString), LA("QC", player.UserIDString));
            DestroyUI(player);            
        }
        private void RemoveQuest(string questName)
        {
            var Quest = GetQuest(questName);
            if (Quest == null) return;
            var Type = (QuestType)GetQuestType(questName);
            questData.Quest[Type].Remove(questName);
            
            foreach (var player in PlayerProgress)
            {
                if (player.Value.Quests.ContainsKey(questName))                
                    player.Value.Quests.Remove(questName); 
            }
            if (vendors.DeliveryVendors.ContainsKey(Quest.Objective))
                vendors.DeliveryVendors.Remove(Quest.Objective);
            if (vendors.QuestVendors.ContainsKey(Quest.Objective))
                vendors.QuestVendors.Remove(Quest.Objective);

            SaveQuestData();
            SaveVendorData();
        }

        private ulong GetLastAttacker(uint id)
        {
            int hits = 0;
            ulong majorityPlayer = 0U;
            if (HeliAttackers.ContainsKey(id))
            {                
                foreach (var score in HeliAttackers[id])
                {
                    if (score.Value > hits)
                        majorityPlayer = score.Key;
                }                
            }
            return majorityPlayer;
        }
        private string GetTypeDescription(QuestType type)
        {
            switch (type)
            {
                case QuestType.Kill:
                    return LA("KillOBJ");
                case QuestType.Craft:
                    return LA("CraftOBJ");
                case QuestType.Gather:
                    return LA("GatherOBJ");
                case QuestType.Loot:
                    return LA("LootOBJ");
                case QuestType.Delivery:
                    return LA("DelvOBJ");
            }
            return "";
        }
        private QuestType ConvertStringToType(string type)
        {
            switch (type)
            {                
                case "gather":
                case "Gather":
                    return QuestType.Gather;
                case "loot":
                case "Loot":
                    return QuestType.Loot;
                case "craft":
                case "Craft":
                    return QuestType.Craft;
                case "delivery":
                case "Delivery":
                    return QuestType.Delivery;
                default:
                    return QuestType.Kill;
            }
        }

        private string isNPCRegistered(string ID)
        {
            if (vendors.QuestVendors.ContainsKey(ID)) return LA("aQVReg");
            if (vendors.DeliveryVendors.ContainsKey(ID)) return LA("aDVReg");
            return null;
        }
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private BasePlayer FindEntity(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            var rayResult = Ray(player, currentRot);
            if (rayResult is BasePlayer)
            {
                var ent = rayResult as BasePlayer;
                return ent;
            }
            return null;
        }
        private object Ray(BasePlayer player, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(player.transform.position + new Vector3(0f, 1.5f, 0f), Aim);
            float distance = 50f;
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BaseEntity>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BaseEntity>();
                    }
                }
            }
            return target;
        }

        private void SetVendorName()
        {
            foreach(var npc in vendors.DeliveryVendors)
            {
                var player = BasePlayer.FindByID(ulong.Parse(npc.Key));
                if (player != null)
                {
                    NPCdisplayName.SetValue(player, npc.Value.Info.Name);
                }
            }
            foreach(var npc in vendors.QuestVendors)
            {
                var player = BasePlayer.FindByID(ulong.Parse(npc.Key));
                if (player != null)
                {
                    NPCdisplayName.SetValue(player, npc.Value.Name);
                }
            }
        }
        private void RemoveVendor(BasePlayer player, string ID, bool isVendor)
        {
            if (isVendor)
            {
                RemoveMapMarker(vendors.QuestVendors[ID].Name);
                vendors.QuestVendors.Remove(ID);
                                
                int i = 1;
                foreach(var npc in vendors.QuestVendors)
                {
                    RemoveMapMarker(npc.Value.Name);
                    npc.Value.Name = $"QuestVendor_{i}";
                    AddMapMarker(npc.Value.x, npc.Value.z, npc.Value.Name, $"{configData.Icon_Vendor}_{i}");
                    i++;
                }                
            }
            else
            {
                RemoveMapMarker(vendors.DeliveryVendors[ID].Info.Name);
                vendors.DeliveryVendors.Remove(ID);
                
                int i = 1;
                foreach (var npc in vendors.DeliveryVendors)
                {
                    RemoveMapMarker(npc.Value.Info.Name);
                    npc.Value.Info.Name = $"Delivery_{i}";
                    AddMapMarker(npc.Value.Info.x, npc.Value.Info.z, npc.Value.Info.Name, $"{configData.Icon_Delivery}_{i}");
                    i++;
                }
                foreach (var user in PlayerProgress)
                {                    
                    if (user.Value.Quests.ContainsKey(ID))
                        user.Value.Quests.Remove(ID);
                }
            }
            DeleteNPCMenu(player);
            PopupMessage(player, $"You have successfully removed the npc with ID: {ID}");
            SaveVendorData();
        }
        private string GetRandomNPC(string ID)
        {
            var npcIDs = vendors.DeliveryVendors.Keys.ToList();            
            if (npcIDs.Contains(ID))
                npcIDs.Remove(ID);            
            var randNum = UnityEngine.Random.Range(0, npcIDs.Count - 1);
            return npcIDs[randNum];
        }
        private string LA(string key, string userID = null) => lang.GetMessage(key, this, userID);

        #endregion

        #region UI
        private void CreateMenu(BasePlayer player)
        {
            CloseMap(player);

            var MenuElement = QUI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "0.12 1");
            QUI.CreatePanel(ref MenuElement, UIMain, UIColors["light"], "0.05 0.01", "0.95 0.99", true);
            QUI.CreateLabel(ref MenuElement, UIMain, "", $"{configData.MSG_MainColor}Quests</color>", 30, "0.05 0.9", "0.95 1");
            int i = 0;
            CreateMenuButton(ref MenuElement, UIMain, LA("Kill", player.UserIDString), "QUI_ChangeElement kill", i); i++;
            CreateMenuButton(ref MenuElement, UIMain, LA("Gather", player.UserIDString), "QUI_ChangeElement gather", i); i++;
            CreateMenuButton(ref MenuElement, UIMain, LA("Loot", player.UserIDString), "QUI_ChangeElement loot", i); i++;
            CreateMenuButton(ref MenuElement, UIMain, LA("Craft", player.UserIDString), "QUI_ChangeElement craft", i); i++;
            i++;
            if (HumanNPC)
                CreateMenuButton(ref MenuElement, UIMain, LA("Delivery", player.UserIDString), "QUI_ChangeElement delivery", i); i++;
            CreateMenuButton(ref MenuElement, UIMain, LA("Your Quests", player.UserIDString), "QUI_ChangeElement personal", i); i++;

            if (player.IsAdmin())
            {
                QUI.CreateButton(ref MenuElement, UIMain, UIColors["buttonopen"], LA("Create Quest", player.UserIDString), 18, "0.1 0.225", "0.9 0.28", "QUI_ChangeElement creation");
                QUI.CreateButton(ref MenuElement, UIMain, UIColors["buttongrey"], LA("Edit Quest", player.UserIDString), 18, "0.1 0.16", "0.9 0.215", "QUI_ChangeElement editor");
                QUI.CreateButton(ref MenuElement, UIMain, UIColors["buttonred"], LA("Delete Quest", player.UserIDString), 18, "0.1 0.095", "0.9 0.15", "QUI_DeleteQuest");
            }

            QUI.CreateButton(ref MenuElement, UIMain, UIColors["buttonbg"], LA("Close", player.UserIDString), 18, "0.1 0.03", "0.9 0.085", "QUI_DestroyAll");
            CuiHelper.AddUi(player, MenuElement);
        }
        private void CreateEmptyMenu(BasePlayer player)
        {
            CloseMap(player);

            var MenuElement = QUI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "0.12 1");
            QUI.CreatePanel(ref MenuElement, UIMain, UIColors["light"], "0.05 0.01", "0.95 0.99", true);
            QUI.CreateLabel(ref MenuElement, UIMain, "", $"{configData.MSG_MainColor}Quests</color>", 30, "0.05 0.9", "0.95 1");
            CreateMenuButton(ref MenuElement, UIMain, LA("Your Quests", player.UserIDString), "QUI_ChangeElement personal", 4); 

            QUI.CreateButton(ref MenuElement, UIMain, UIColors["buttonbg"], LA("Close", player.UserIDString), 18, "0.1 0.03", "0.9 0.085", "QUI_DestroyAll");
            CuiHelper.AddUi(player, MenuElement);            
        }
        private void CreateMenuButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {
            Vector2 dimensions = new Vector2(0.8f, 0.055f);
            Vector2 origin = new Vector2(0.1f, 0.75f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;

            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 18, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }

        private void ListElement(BasePlayer player, QuestType type, int page = 0)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "", GetTypeDescription(type), 16, "0.1 0.93", "0.9 0.99");
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.015", type.ToString().ToUpper(), 200, "0.01 0.01", "0.99 0.99");
            var quests = Quest[type];
            if (quests.Count > 4)
            {
                var maxpages = (quests.Count - 1) /4 + 1;
                if (page < maxpages - 1)
                    QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], LA("Next", player.UserIDString), 18, "0.84 0.03", "0.97 0.085", $"QUI_ChangeElement listpage {type} {page + 1}");
                if (page > 0)
                    QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], LA("Back", player.UserIDString), 18, "0.03 0.03", "0.16 0.085", $"QUI_ChangeElement listpage {type} {page - 1}");
            }
            int maxentries = (4 * (page + 1));
            if (maxentries > quests.Count)
                maxentries = quests.Count;
            int rewardcount = 4 * page;
            List <string> questNames = new List<string>();
            foreach (var entry in Quest[type])
                questNames.Add(entry.Key);

            if (quests.Count == 0)
                QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("noQ", player.UserIDString)} {type.ToString().ToLower()} {LA("quests", player.UserIDString)} </color>", 24, "0 0.82", "1 0.9");

            CuiHelper.AddUi(player, Main);

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {                
                CreateQuestEntry(player, quests[questNames[n]], i);
                i++;
            }
        } 
        private void CreateQuestEntry(BasePlayer player, QuestEntry entry, int num)
        {            
            Vector2 posMin = CalcQuestPos(num);
            Vector2 dimensions = new Vector2(0.4f, 0.4f);
            Vector2 posMax = posMin + dimensions;
            
            var panelName = UIEntry + num;
            AddUIString(player, panelName);

            var questEntry = QUI.CreateElementContainer(panelName, "0 0 0 0", $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}");
            QUI.CreatePanel(ref questEntry, panelName, UIColors["buttonbg"], $"0 0", $"1 1");

            string buttonCommand = "";
            string buttonText = "";
            string buttonColor = "";
            QuestStatus status = QuestStatus.Open;
            var prog = PlayerProgress[player.userID].Quests;
            if (prog.ContainsKey(entry.QuestName))
            {
                status = prog[entry.QuestName].Status;
                switch (prog[entry.QuestName].Status)
                {
                    case QuestStatus.Pending:
                        
                        buttonColor = UIColors["buttongrey"];
                        buttonText = LA("Pending", player.UserIDString);
                        break;
                    case QuestStatus.Completed:
                        buttonColor = UIColors["buttoncompleted"];
                        buttonText = LA("Completed", player.UserIDString);
                        break;
                }
            }
            else
            {
                buttonColor = UIColors["buttonopen"];
                buttonText = LA("Accept Quest", player.UserIDString);
                buttonCommand = $"QUI_AcceptQuest {entry.QuestName}";
            }
            QUI.CreateButton(ref questEntry, panelName, buttonColor, buttonText, 18, $"0.75 0.83", $"0.97 0.97", buttonCommand);

            string rewards = GetRewardString(entry.Rewards);
            string questInfo = $"{configData.MSG_MainColor}{LA("Status:", player.UserIDString)}</color> {status}";
            questInfo = questInfo + $"\n\n{configData.MSG_MainColor}{LA("Description:", player.UserIDString)} </color>{configData.MSG_Color}{entry.Description}</color>";
            questInfo = questInfo + $"\n\n{configData.MSG_MainColor}{LA("Objective:", player.UserIDString)} </color>{configData.MSG_Color}{entry.ObjectiveName}</color>";
            questInfo = questInfo + $"\n\n{configData.MSG_MainColor}{LA("Amount Required:", player.UserIDString)} </color>{configData.MSG_Color}{entry.AmountRequired}</color>";
            questInfo = questInfo + $"\n\n{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)} </color>{configData.MSG_Color}{rewards}</color>";

            QUI.CreateLabel(ref questEntry, panelName, "", $"{entry.QuestName}", 25, $"0.1 0.8", "0.7 0.95", TextAnchor.MiddleLeft);
            QUI.CreateLabel(ref questEntry, panelName, buttonColor, questInfo, 15, $"0.1 0.1", "0.9 0.78", TextAnchor.MiddleLeft);
            
            CuiHelper.AddUi(player, questEntry);
        }

        private void PlayerStats(BasePlayer player, int page = 0)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "", LA("yqDesc", player.UserIDString), 16, "0.1 0.93", "0.9 0.99");
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.015", LA("STATS", player.UserIDString), 200, "0.01 0.01", "0.99 0.99");

            var stats = PlayerProgress[player.userID];
            if (stats.Quests.Count > 4)
            {
                var maxpages = (stats.Quests.Count - 1) / 4 + 1;
                if (page < maxpages - 1)
                    QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], LA("Next", player.UserIDString), 18, "0.84 0.03", "0.97 0.085", $"QUI_ChangeElement statspage {page + 1}");
                if (page > 0)
                    QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], LA("Back", player.UserIDString), 18, "0.03 0.03", "0.16 0.085", $"QUI_ChangeElement statspage {page - 1}");
            }
            int maxentries = (4 * (page + 1));
            if (maxentries > stats.Quests.Count)
                maxentries = stats.Quests.Count;
            int rewardcount = 4 * page;
            List<string> questNames = new List<string>();
            foreach (var entry in stats.Quests)
                questNames.Add(entry.Key);

            if (stats.Quests.Count == 0)
                QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("noQDSaved", player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");

            CuiHelper.AddUi(player, Main);

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                var Quest = GetQuest(questNames[n]);
                if (Quest == null) continue;
                CreateStatEntry(player, Quest, i);
                i++;
            }
        }
        private void CreateStatEntry(BasePlayer player, QuestEntry entry, int num)
        {
            Vector2 posMin = CalcQuestPos(num);
            Vector2 dimensions = new Vector2(0.4f, 0.4f);
            Vector2 posMax = posMin + dimensions;

            var panelName = UIEntry + num;
            AddUIString(player, panelName);

            var questEntry = QUI.CreateElementContainer(panelName, "0 0 0 0", $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}");
            QUI.CreatePanel(ref questEntry, panelName, UIColors["buttonbg"], $"0 0", $"1 1");
           
            string statusColor = "";
            QuestStatus status = QuestStatus.Open;
            var prog = PlayerProgress[player.userID].Quests;
            if (prog.ContainsKey(entry.QuestName))
            {
                status = prog[entry.QuestName].Status;
                switch (prog[entry.QuestName].Status)
                {
                    case QuestStatus.Pending:
                        statusColor = UIColors["buttongrey"];
                        break;
                    case QuestStatus.Completed:
                        statusColor = UIColors["buttoncompleted"];
                        break;
                }
            }
            
            if (status != QuestStatus.Completed)
                QUI.CreateButton(ref questEntry, panelName, UIColors["buttonred"], LA("Cancel Quest",player.UserIDString), 18, $"0.75 0.83", $"0.97 0.97", $"QUI_CancelQuest {entry.QuestName}");
            if (status == QuestStatus.Completed && !prog[entry.QuestName].RewardClaimed)
                QUI.CreateButton(ref questEntry, panelName, statusColor, LA("Claim Reward", player.UserIDString), 18, $"0.75 0.83", $"0.97 0.97", $"QUI_ClaimReward {entry.QuestName}");
            if (status == QuestStatus.Completed && prog[entry.QuestName].RewardClaimed)
            {
                if (prog[entry.QuestName].ResetTime < GrabCurrentTime())
                    QUI.CreateButton(ref questEntry, panelName, statusColor, LA("Remove", player.UserIDString), 18, $"0.75 0.83", $"0.97 0.97", $"QUI_RemoveCompleted {entry.QuestName}");
                else
                {
                    TimeSpan dateDifference = TimeSpan.FromSeconds(prog[entry.QuestName].ResetTime - GrabCurrentTime());
                    var days = dateDifference.Days;
                    var hours = dateDifference.Hours;
                    hours += (days * 24);
                    var mins = dateDifference.Minutes;
                    string remaining = string.Format("{0:00}h :{1:00}m", hours, mins);
                    QUI.CreateLabel(ref questEntry, panelName, "", $"{LA("Cooldown:", player.UserIDString)} {remaining}", 14, $"0.7 0.83", $"0.99 0.97");
                }

            }
            string stats = $"{configData.MSG_MainColor}{LA("Status:", player.UserIDString)}</color> {status}";
            stats = stats + $"\n{configData.MSG_MainColor}{LA("Quest Type:", player.UserIDString)} </color> {configData.MSG_Color}{prog[entry.QuestName].Type}</color>";  

            string questInfo = $"{configData.MSG_MainColor}{LA("Description:", player.UserIDString)} </color>{configData.MSG_Color}{entry.Description}</color>";
            questInfo = questInfo + $"\n{configData.MSG_MainColor}{LA("Objective:", player.UserIDString)} </color>{configData.MSG_Color}{entry.ObjectiveName}</color>";
            questInfo = questInfo + $"\n{configData.MSG_MainColor}{LA("Amount Required:", player.UserIDString)} </color>{configData.MSG_Color}{entry.AmountRequired}</color>";

            string obtained = $"{configData.MSG_MainColor}{LA("Collected:", player.UserIDString)} </color>{configData.MSG_Color}{prog[entry.QuestName].AmountCollected}</color>";

            var rewards = GetRewardString(entry.Rewards);            

            string reward = $"{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)} </color>{configData.MSG_Color}{rewards}</color>";
            reward = reward + $"\n{configData.MSG_MainColor}{LA("Reward Claimed:", player.UserIDString)} </color>{configData.MSG_Color}{prog[entry.QuestName].RewardClaimed}</color>";

            var percent = System.Convert.ToDouble((float)prog[entry.QuestName].AmountCollected / (float)entry.AmountRequired);
            var yMax = 0.105f + (0.59f * percent);

            QUI.CreateLabel(ref questEntry, panelName, "", $"{entry.QuestName}", 25, $"0.1 0.8", "0.7 0.95", TextAnchor.MiddleLeft);
            QUI.CreateLabel(ref questEntry, panelName, statusColor, stats, 14, $"0.1 0.65", "0.7 0.78", TextAnchor.MiddleLeft);
            QUI.CreateLabel(ref questEntry, panelName, statusColor, questInfo, 14, $"0.1 0.3", "0.7 0.6", TextAnchor.MiddleLeft);
            QUI.CreateLabel(ref questEntry, panelName, statusColor, obtained, 14, $"0.1 0.32", "0.7 0.3", TextAnchor.MiddleLeft);
            QUI.CreatePanel(ref questEntry, panelName, UIColors["buttonbg"], $"0.1 0.26", "0.7 0.32");
            QUI.CreatePanel(ref questEntry, panelName, UIColors["buttonopen"], $"0.105 0.27", $"{yMax} 0.31");
            QUI.CreateLabel(ref questEntry, panelName, statusColor, reward, 14, $"0.1 0.05", "0.7 0.25", TextAnchor.MiddleLeft);
            CuiHelper.AddUi(player, questEntry);
        }
        private void PlayerDelivery(BasePlayer player)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "", GetTypeDescription(QuestType.Delivery), 16, "0.1 0.93", "0.9 0.99");
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.015", LA("DELIVERY", player.UserIDString), 200, "0.01 0.01", "0.99 0.99");

            var npcid = PlayerProgress[player.userID].CurrentDelivery.VendorID;
            var targetid = PlayerProgress[player.userID].CurrentDelivery.TargetID;
            if (string.IsNullOrEmpty(npcid))
                QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("noADM", player.UserIDString)}</color>", 24, "0 0.82", "1 0.9");
            else
            {
                var quest = vendors.DeliveryVendors[npcid];
                var target = vendors.DeliveryVendors[targetid];
                if (quest != null && target != null)
                {
                    var distance = Vector2.Distance(new Vector2(quest.Info.x, quest.Info.z), new Vector2(target.Info.x, target.Info.z));
                    var rewardAmount = distance * quest.Multiplier;
                    if (rewardAmount < 1) rewardAmount = 1;
                    var briefing = $"{configData.MSG_MainColor}{quest.Info.Name}\n\n</color>";
                    briefing = briefing + $"{configData.MSG_Color}{quest.Description}</color>\n\n";
                    briefing = briefing + $"{configData.MSG_MainColor}{LA("Destination:", player.UserIDString)} </color>{configData.MSG_Color}{target.Info.Name}\nX {target.Info.x}, Z {target.Info.z}</color>\n";
                    briefing = briefing + $"{configData.MSG_MainColor}{LA("Distance:", player.UserIDString)} </color>{configData.MSG_Color}{distance}M</color>\n";
                    briefing = briefing + $"{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)} </color>{configData.MSG_Color}{(int)rewardAmount}x {quest.Reward.DisplayName}</color>";
                    QUI.CreateLabel(ref Main, UIPanel, "", briefing, 20, "0.15 0.2", "0.85 1", TextAnchor.MiddleLeft);

                    QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], LA("Cancel", player.UserIDString), 18, "0.2 0.05", "0.35 0.1", $"QUI_CancelDelivery");
                }
            }
            CuiHelper.AddUi(player, Main);
        }

        private void CreationMenu(BasePlayer player)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);

            int i = 0;
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("selCreat", player.UserIDString)}</color>", 20, "0.25 0.8", "0.75 0.9");
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.025", LA("CREATOR", player.UserIDString), 200, "0.01 0.01", "0.99 0.99");
            CreateNewQuestButton(ref Main, UIPanel, LA("Kill", player.UserIDString), "QUI_NewQuest kill", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Gather", player.UserIDString), "QUI_NewQuest gather", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Loot", player.UserIDString), "QUI_NewQuest loot", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Craft", player.UserIDString), "QUI_NewQuest craft", i); i++;
            if (HumanNPC)
                CreateNewQuestButton(ref Main, UIPanel, LA("Delivery", player.UserIDString), "QUI_NewQuest delivery", i); i++;

            CuiHelper.AddUi(player, Main);
        }        
        private void CreationHelp(BasePlayer player, int page = 0)
        {
            DestroyEntries(player);
            QuestCreator quest = null;
            if (ActiveCreations.ContainsKey(player.userID))
                quest = ActiveCreations[player.userID];
            else if (ActiveEditors.ContainsKey(player.userID))
                quest = ActiveEditors[player.userID];
            if (quest == null) return;

            var HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
            QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");

            switch (page)
            {
                case 0:
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelMen", player.UserIDString)}.\n</color> {configData.MSG_Color}{LA("creHelFol", player.UserIDString)}.\n\n{LA("creHelExi", player.UserIDString)} </color>{configData.MSG_MainColor}'exit'\n\n\n\n{LA("creHelName", player.UserIDString)}</color>", 20, "0 0", "1 1");
                break;
                case 1:
                    var MenuMain = QUI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 1", true);
                    QUI.CreatePanel(ref MenuMain, UIMain, UIColors["light"], "0.01 0.01", "0.99 0.99");
                    QUI.CreateLabel(ref MenuMain, UIMain, "", $"{configData.MSG_MainColor}{LA("creHelObj", player.UserIDString)}</color>", 20, "0.25 0.85", "0.75 0.95");
                    CuiHelper.AddUi(player, MenuMain);
                    CreateObjectiveMenu(player);
                    return;
                case 2:
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelRA", player.UserIDString)}</color>", 20, "0.25 0.4", "0.75 0.6");
                    break;
                case 3:
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelQD", player.UserIDString)}</color>", 20, "0.25 0.4", "0.75 0.6");
                    break;
                case 4:
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelRT", player.UserIDString)}</color>", 20, "0.25 0.8", "0.75 1");
                        int i = 0;
                        if (Economics) CreateRewardTypeButton(ref HelpMain, UIPanel, $"{LA("Coins", player.UserIDString)} (Economics)", "QUI_RewardType coins", i); i++;
                        if (ServerRewards) CreateRewardTypeButton(ref HelpMain, UIPanel, $"{LA("RP", player.UserIDString)} (ServerRewards)", "QUI_RewardType rp", i); i++;
                        CreateRewardTypeButton(ref HelpMain, UIPanel, LA("Item", player.UserIDString), "QUI_RewardType item", i); i++;
                        if (HuntPlugin) { CreateRewardTypeButton(ref HelpMain, UIPanel, $"{LA("HuntXP", player.UserIDString)} (HuntRPG)", "QUI_RewardType huntxp", i); i++; }
                    }
                    break;
                case 5:
                    if (quest.item.isCoins || quest.item.isRP || quest.item.isHuntXP)
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelRewA", player.UserIDString)}</color>", 20, "0.25 0.4", "0.75 0.6");
                    else
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.3 0.8", "0.7 0.97");
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelIH", player.UserIDString)} 'quest item'</color>", 20, "0.1 0", "0.9 1");
                    }
                    break;
                case 7:
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelAR", player.UserIDString)}</color>", 20, "0.1 0", "0.9 1");
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Yes", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_AddReward");
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("No", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_RewardFinish");
                    break;
                case 8:
                    if (quest.type != QuestType.Kill)
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelID", player.UserIDString)}</color>", 20, "0.1 0", "0.9 1");
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Yes", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_ItemDeduction 1");
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("No", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_ItemDeduction 0");
                    }
                    else { CreationHelp(player, 9); return; }
                    break;
                case 9:
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.3 0.8", "0.7 0.97");
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelCD", player.UserIDString)}</color>", 20, "0.1 0", "0.9 1");
                    break;
                case 10:
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelNewRew", player.UserIDString)}</color>", 20, "0.25 0.8", "0.75 1");
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("addNewRew", player.UserIDString), 18, "0.7 0.04", "0.95 0.12", $"QUI_AddReward");
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Back", player.UserIDString), 18, "0.05 0.04", "0.3 0.12", $"QUI_EndEditing");

                        int i = 0;
                        foreach (var entry in ActiveEditors[player.userID].entry.Rewards)
                        {
                            CreateDelEditButton(ref HelpMain, 0.1f, UIPanel, $"{entry.Amount}x {entry.DisplayName}", i, "", 0.35f);
                            CreateDelEditButton(ref HelpMain, 0.72f, UIPanel, LA("Remove", player.UserIDString), i, $"QUI_RemoveReward {entry.Amount} {entry.DisplayName}");
                            i++;
                        }
                    }
                    break;
                default:
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelSQ", player.UserIDString)}</color>", 20, "0.1 0.8", "0.9 0.95");
                    string questDetails = $"{configData.MSG_MainColor}{LA("Quest Type:", player.UserIDString)}</color> {configData.MSG_Color}{quest.type}</color>";
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Name:", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.QuestName}</color>";
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Description:", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.Description}</color>";
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Objective:", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.ObjectiveName}</color>";
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Required Amount:", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.AmountRequired}</color>";
                    if (quest.type != QuestType.Kill) questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Item Deduction:", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.ItemDeduction}</color>";
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("CDMin", player.UserIDString)}</color> {configData.MSG_Color}{quest.entry.Cooldown}</color>";

                    var rewards = GetRewardString(quest.entry.Rewards);                    
                    
                    questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)}</color> {configData.MSG_Color}{rewards}</color>";

                    QUI.CreateLabel(ref HelpMain, UIPanel, "", questDetails, 20, "0.1 0.2", "0.9 0.75", TextAnchor.MiddleLeft);
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Save Quest", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_SaveQuest");
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Cancel", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_ExitQuest");
                    break;
            }            
            CuiHelper.AddUi(player, HelpMain);
        }
        private void CreateObjectiveMenu(BasePlayer player, int page = 0)
        {
            DestroyEntries(player);
            var HelpMain = QUI.CreateElementContainer(UIPanel, "0 0 0 0", "0 0", "1 1");
            QuestType type;
            if (ActiveCreations.ContainsKey(player.userID))
                type = ActiveCreations[player.userID].type;
            else type = ActiveEditors[player.userID].type;
            var objCount = AllObjectives[type].Count;
            if (objCount > 100)
            {
                var maxpages = (objCount - 1) / 96 + 1;
                if (page < maxpages - 1)
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Next", player.UserIDString), 18, "0.84 0.05", "0.97 0.1", $"QUI_ChangeElement objpage {page + 1}");
                if (page > 0)
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Back", player.UserIDString), 18, "0.03 0.05", "0.16 0.1", $"QUI_ChangeElement objpage {page - 1}");
            }
            int maxentries = (96 * (page + 1));
            if (maxentries > objCount)
                maxentries = objCount;
            int rewardcount = 96 * page;

            int i = 0;
            for (int n = rewardcount; n < maxentries; n++)
            {
                CreateObjectiveEntry(ref HelpMain, UIPanel, AllObjectives[type][n], i);
                i++;
            }           
            CuiHelper.AddUi(player, HelpMain);
        }
        private void DeliveryHelp(BasePlayer player, int page = 0)
        {
            DestroyEntries(player);
            switch (page)
            {
                case 0:
                    var HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0.0", "1 1", true);
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("delHelMen", player.UserIDString)}\n\n</color> {configData.MSG_Color}{LA("delHelChoo", player.UserIDString)}.\n\n{LA("creHelExi", player.UserIDString)} </color>{configData.MSG_MainColor}'exit'</color>", 20, "0 0", "1 1");
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Quest Vendor", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_AddVendor 1");
                    QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Delivery Vendor", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_AddVendor 2");
                    CuiHelper.AddUi(player, HelpMain);
                    return;
                case 1:
                    var element = QUI.CreateElementContainer(UIMain, UIColors["dark"], "0.25 0.85", "0.75 0.95");
                    QUI.CreatePanel(ref element, UIMain, UIColors["buttonbg"], "0.005 0.04", "0.995 0.96");
                    QUI.CreateLabel(ref element, UIMain, "", $"{configData.MSG_MainColor}{LA("delHelNewNPC", player.UserIDString)} '/questnpc'</color>", 22, "0 0", "1 1");                    
                    CuiHelper.AddUi(player, element);                    
                    return;
                case 2:
                    DestroyUI(player);
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_Color}{LA("delHelMult", player.UserIDString)}</color>\n{configData.MSG_MainColor}{LA("creHelRT", player.UserIDString)}</color>", 18, "0.05 0.82", "0.95 0.98");
                    int i = 0;
                    if (Economics) CreateRewardTypeButton(ref HelpMain, UIPanel, "Coins (Economics)", "QUI_RewardType coins", i); i++;
                    if (ServerRewards) CreateRewardTypeButton(ref HelpMain, UIPanel, "RP (ServerRewards)", "QUI_RewardType rp", i); i++;                    
                    CreateRewardTypeButton(ref HelpMain, UIPanel, LA("Item", player.UserIDString), "QUI_RewardType item", i); i++;                    
                    if (HuntPlugin) { CreateRewardTypeButton(ref HelpMain, UIPanel, "XP (HuntRPG)", "QUI_RewardType huntxp", i); i++; }
                    CuiHelper.AddUi(player, HelpMain);
                    return;
                case 3:
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                        var quest = ActiveCreations[player.userID];
                        if (quest.deliveryInfo.Reward.isCoins || quest.deliveryInfo.Reward.isRP || quest.deliveryInfo.Reward.isHuntXP)
                            DeliveryHelp(player, 4);
                        else
                        {
                            HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.3 0.8", "0.7 0.97");
                            QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                            QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("creHelIH", player.UserIDString)} 'quest item'</color>", 20, "0.1 0", "0.9 1");
                            CuiHelper.AddUi(player, HelpMain);
                        }
                    }
                    return;
                case 4:
                    ActiveCreations[player.userID].partNum = 5;
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("delHelRM", player.UserIDString)}</color> {configData.MSG_Color}\n\n{LA("delHelRM1", player.UserIDString)}</color>{configData.MSG_MainColor} 2000m</color>{configData.MSG_Color} {LA("delHelRM2", player.UserIDString)} </color>{configData.MSG_MainColor}0.25</color>{configData.MSG_Color}, {LA("delHelRM3", player.UserIDString)} </color>{configData.MSG_MainColor}500</color>", 20, "0.05 0.1", "0.95 0.9");
                    CuiHelper.AddUi(player, HelpMain);
                    return;
                case 5:
                    ActiveCreations[player.userID].partNum = 3;
                    HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9");
                    QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("delHelDD", player.UserIDString)}</color>", 20, "0.05 0.1", "0.95 0.9");
                    CuiHelper.AddUi(player, HelpMain);
                    return;
                case 6:
                    {
                        HelpMain = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                        QUI.CreatePanel(ref HelpMain, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                        QUI.CreateLabel(ref HelpMain, UIPanel, "", $"{configData.MSG_MainColor}{LA("delHelNewV", player.UserIDString)}</color>", 20, "0.1 0.8", "0.9 0.95");

                        var quest = ActiveCreations[player.userID];
                        string questDetails = $"{configData.MSG_MainColor}{LA("Quest Type:", player.UserIDString)}</color> {configData.MSG_Color}{quest.type}</color>";
                        questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Name:", player.UserIDString)}</color> {configData.MSG_Color}{quest.deliveryInfo.Info.Name}</color>";
                        questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Description:", player.UserIDString)}</color> {configData.MSG_Color}{quest.deliveryInfo.Description}</color>";
                        questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)}</color> {configData.MSG_Color}{quest.deliveryInfo.Reward.DisplayName}</color>";
                        questDetails = questDetails + $"\n{configData.MSG_MainColor}{LA("Multiplier:", player.UserIDString)}</color> {configData.MSG_Color}{quest.deliveryInfo.Multiplier}</color>";

                        QUI.CreateLabel(ref HelpMain, UIPanel, "", questDetails, 20, "0.1 0.2", "0.9 0.75", TextAnchor.MiddleLeft);
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Save Quest", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_SaveQuest");
                        QUI.CreateButton(ref HelpMain, UIPanel, UIColors["buttonbg"], LA("Cancel", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_ExitQuest");
                        CuiHelper.AddUi(player, HelpMain);
                    }
                    return;
            default:
                    return;
            }            
        }        
        private void AcceptDelivery(BasePlayer player, string npcID, int page = 0)
        {
            var quest = vendors.DeliveryVendors[npcID];

            switch (page)
            {
                case 0:
                    {
                        var target = vendors.DeliveryVendors[GetRandomNPC(npcID)];
                        if (quest != null && target != null)
                        {
                            var distance = Vector2.Distance(new Vector2(quest.Info.x, quest.Info.z), new Vector2(target.Info.x, target.Info.z));
                            var rewardAmount = distance * quest.Multiplier;
                            if (rewardAmount < 1) rewardAmount = 1;
                            var briefing = $"{configData.MSG_MainColor}{quest.Info.Name}\n\n</color>";
                            briefing = briefing + $"{configData.MSG_Color}{quest.Description}</color>\n\n";
                            briefing = briefing + $"{configData.MSG_MainColor}{LA("Destination:", player.UserIDString)} </color>{configData.MSG_Color}{target.Info.Name}\nX {target.Info.x}, Z {target.Info.z}</color>\n";
                            briefing = briefing + $"{configData.MSG_MainColor}{LA("Distance:", player.UserIDString)} </color>{configData.MSG_Color}{distance}M</color>\n";
                            briefing = briefing + $"{configData.MSG_MainColor}{LA("Reward:", player.UserIDString)} </color>{configData.MSG_Color}{(int)rewardAmount}x {quest.Reward.DisplayName}</color>";

                            var VendorUI = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                            QUI.CreatePanel(ref VendorUI, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                            QUI.CreateLabel(ref VendorUI, UIPanel, "", briefing, 20, "0.15 0.2", "0.85 1", TextAnchor.MiddleLeft);

                            QUI.CreateButton(ref VendorUI, UIPanel, UIColors["buttonbg"], LA("Accept", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_AcceptDelivery {npcID} {target.Info.ID} {distance}");
                            QUI.CreateButton(ref VendorUI, UIPanel, UIColors["buttonbg"], LA("Decline", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_DestroyAll");
                            CuiHelper.AddUi(player, VendorUI);
                        }
                    }
                        return;
                    case 1:
                    {
                        var VendorUI = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.4 0.3", "0.95 0.9", true);
                        QUI.CreatePanel(ref VendorUI, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                        QUI.CreateLabel(ref VendorUI, UIPanel, "", $"{configData.MSG_MainColor} {LA("delComplMSG", player.UserIDString)}</color>", 22, "0 0", "1 1");
                        QUI.CreateButton(ref VendorUI, UIPanel, UIColors["buttonbg"], LA("Claim", player.UserIDString), 18, "0.6 0.05", "0.8 0.15", $"QUI_FinishDelivery");
                        QUI.CreateButton(ref VendorUI, UIPanel, UIColors["buttonbg"], LA("Cancel", player.UserIDString), 18, "0.2 0.05", "0.4 0.15", $"QUI_DestroyAll");
                        CuiHelper.AddUi(player, VendorUI);
                    }
                    return;
                default:
                    return;

            }
        }

        private void DeletionEditMenu(BasePlayer player, string page, string command)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.025", page, 200, "0.01 0.01", "0.99 0.99");

            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Kill",player.UserIDString)}</color>", 20, "0 0.87", "0.25 0.92");
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Gather", player.UserIDString)}</color>", 20, "0.25 0.87", "0.5 0.92");
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Loot", player.UserIDString)}</color>", 20, "0.5 0.87", "0.75 0.92");
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Craft", player.UserIDString)}</color>", 20, "0.75 0.87", "1 0.92");
            if (command == "QUI_ConfirmDelete") QUI.CreateButton(ref Main, UIPanel, UIColors["buttonbg"], $"{configData.MSG_MainColor}{LA("Delete NPC", player.UserIDString)}</color>", 18, "0.8 0.94", "0.98 0.98", "QUI_DeleteNPCMenu");

            int killNum = 0;
            int gatherNum = 0;
            int lootNum = 0;
            int craftNum = 0;
            foreach (var entry in questData.Quest[QuestType.Kill])
            {
                CreateDelEditButton(ref Main, 0.035f, UIPanel, entry.Key, killNum, command);
                killNum++;
            }
            foreach (var entry in questData.Quest[QuestType.Gather])
            {
                CreateDelEditButton(ref Main, 0.285f, UIPanel, entry.Key, gatherNum, command);
                gatherNum++;
            }
            foreach (var entry in questData.Quest[QuestType.Loot])
            {
                CreateDelEditButton(ref Main, 0.535f, UIPanel, entry.Key, lootNum, command);
                lootNum++;
            }
            foreach (var entry in questData.Quest[QuestType.Craft])
            {
                CreateDelEditButton(ref Main, 0.785f, UIPanel, entry.Key, craftNum, command);
                craftNum++;
            }           
            CuiHelper.AddUi(player, Main);
        }
        private void DeleteNPCMenu(BasePlayer player)
        {
            DestroyEntries(player);
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.025", LA("REMOVER", player.UserIDString), 200, "0.01 0.01", "0.99 0.99");

            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Delivery Vendors", player.UserIDString)}</color>", 20, "0 0.87", "0.5 0.92");
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("Quest Vendors", player.UserIDString)}</color>", 20, "0.5 0.87", "1 0.92");           
                       
            int VendorNum = 0;
            int DeliveryNum = 0;
            foreach (var entry in vendors.QuestVendors)
            {
                CreateDelVendorButton(ref Main, 0.535f, UIPanel, entry.Value.Name, DeliveryNum, $"QUI_RemoveVendor {entry.Key}");
                VendorNum++;
            }
            foreach (var entry in vendors.DeliveryVendors)
            {
                CreateDelVendorButton(ref Main, 0.035f, UIPanel, entry.Value.Info.Name, DeliveryNum, $"QUI_RemoveVendor {entry.Key}");
                DeliveryNum++;
            }
            CuiHelper.AddUi(player, Main);
        }
        private void ConfirmDeletion(BasePlayer player, string questName)
        {
            var ConfirmDelete = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.2 0.4", "0.8 0.8", true);
            QUI.CreatePanel(ref ConfirmDelete, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            QUI.CreateLabel(ref ConfirmDelete, UIPanel, "", $"{configData.MSG_MainColor}{LA("confDel", player.UserIDString)} {questName}</color>", 20, "0.1 0.6", "0.9 0.9");
            QUI.CreateButton(ref ConfirmDelete, UIPanel, UIColors["buttonbg"], LA("Yes", player.UserIDString), 18, "0.6 0.2", "0.8 0.3", $"QUI_DeleteQuest {questName}");
            QUI.CreateButton(ref ConfirmDelete, UIPanel, UIColors["buttonbg"], LA("No", player.UserIDString), 18, "0.2 0.2", "0.4 0.3", $"QUI_DeleteQuest reject");

            CuiHelper.AddUi(player, ConfirmDelete);
        }
        private void ConfirmCancellation(BasePlayer player, string questName)
        {
            var ConfirmDelete = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.2 0.4", "0.8 0.8", true);
            QUI.CreatePanel(ref ConfirmDelete, UIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            QUI.CreateLabel(ref ConfirmDelete, UIPanel, "", $"{configData.MSG_MainColor}{LA("confCan", player.UserIDString)} {questName}</color>\n{configData.MSG_Color}{LA("confCan2", player.UserIDString)}</color>", 20, "0.1 0.6", "0.9 0.9");
            QUI.CreateButton(ref ConfirmDelete, UIPanel, UIColors["buttonbg"], LA("Yes", player.UserIDString), 18, "0.6 0.2", "0.8 0.3", $"QUI_ConfirmCancel {questName}");
            QUI.CreateButton(ref ConfirmDelete, UIPanel, UIColors["buttonbg"], LA("No", player.UserIDString), 18, "0.2 0.2", "0.4 0.3", $"QUI_ConfirmCancel reject");

            CuiHelper.AddUi(player, ConfirmDelete);
        }
             
        private void QuestEditorMenu(BasePlayer player)
        {
            DestroyEntries(player); 
            var Main = QUI.CreateElementContainer(UIPanel, UIColors["dark"], "0.12 0", "1 1");
            QUI.CreatePanel(ref Main, UIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            QUI.CreateLabel(ref Main, UIPanel, "1 1 1 0.025", LA("EDITOR", player.UserIDString), 200, "0.01 0.01", "0.99 0.99");

            int i = 0;
            QUI.CreateLabel(ref Main, UIPanel, "", $"{configData.MSG_MainColor}{LA("chaEdi", player.UserIDString)}</color>", 20, "0.25 0.8", "0.75 0.9");
            CreateNewQuestButton(ref Main, UIPanel, LA("Name", player.UserIDString), "QUI_EditQuestVar name", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Description", player.UserIDString), "QUI_EditQuestVar description", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Objective", player.UserIDString), "QUI_EditQuestVar objective", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Amount", player.UserIDString), "QUI_EditQuestVar amount", i); i++;
            CreateNewQuestButton(ref Main, UIPanel, LA("Reward", player.UserIDString), "QUI_EditQuestVar reward", i); i++;           

            CuiHelper.AddUi(player, Main);
        }
       
        private void CreateObjectiveEntry(ref CuiElementContainer container, string panelName, string name, int number)
        {
            var pos = CalcEntryPos(number);            
            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], name, 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"QUI_SelectObj {name}");            
        }
        private void CreateNewQuestButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {
            Vector2 dimensions = new Vector2(0.2f, 0.07f);
            Vector2 origin = new Vector2(0.4f, 0.7f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;

            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 18, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }
        private void CreateRewardTypeButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {
            Vector2 dimensions = new Vector2(0.36f, 0.1f);
            Vector2 origin = new Vector2(0.32f, 0.7f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;

            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 18, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }        
        private void CreateDelEditButton(ref CuiElementContainer container, float xPos, string panelName, string buttonname, int number, string command, float width = 0.18f)
        {
            Vector2 dimensions = new Vector2(width, 0.05f);
            Vector2 origin = new Vector2(xPos, 0.8f);
            Vector2 offset = new Vector2(0, (-0.01f - dimensions.y) * number);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", $"{command} {buttonname}");
        }
        private void CreateDelVendorButton(ref CuiElementContainer container, float xPos, string panelName, string buttonname, int number, string command)
        {
            if (number > 15) xPos += 0.25f;
            Vector2 dimensions = new Vector2(0.18f, 0.05f);
            Vector2 origin = new Vector2(xPos, 0.8f);
            Vector2 offset = new Vector2(0, (-0.01f - dimensions.y) * number);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            QUI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }

        private void PopupMessage(BasePlayer player, string msg)
        {
            CuiHelper.DestroyUi(player, "PopupMsg");
            var element = QUI.CreateElementContainer("PopupMsg", UIColors["dark"], "0.25 0.85", "0.75 0.95");
            QUI.CreatePanel(ref element, "PopupMsg", UIColors["buttonbg"], "0.005 0.04", "0.995 0.96");
            QUI.CreateLabel(ref element, "PopupMsg", "", $"{configData.MSG_MainColor}{msg}</color>", 22, "0 0", "1 1");
            CuiHelper.AddUi(player, element);
            timer.Once(3, () => CuiHelper.DestroyUi(player, "PopupMsg"));
        }
               
        private Vector2 CalcQuestPos(int num)
        {
            Vector2 position = new Vector2(0.15f, 0.52f);
            if (num == 1)
                position = new Vector2(0.575f, 0.52f);
            if (num == 2)
                position = new Vector2(0.15f, 0.1f);
            if (num == 3)
                position = new Vector2(0.575f, 0.1f);
            return position;
        }        
        private float[] CalcEntryPos(int number)
        {
            Vector2 position = new Vector2(0.014f, 0.8f);
            Vector2 dimensions = new Vector2(0.12f, 0.055f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number <8)
            {
                offsetX = (0.002f + dimensions.x) * number;
            }
            if (number > 7 && number < 16)
            {
                offsetX = (0.002f + dimensions.x) * (number - 8);
                offsetY = (-0.0055f - dimensions.y) * 1;
            }
            if (number > 15 && number < 24)
            {
                offsetX = (0.002f + dimensions.x) * (number - 16);
                offsetY = (-0.0055f - dimensions.y) * 2;
            }
            if (number > 23 && number < 32)
            {
                offsetX = (0.002f + dimensions.x) * (number - 24);
                offsetY = (-0.0055f - dimensions.y) * 3;
            }
            if (number > 31 && number < 40)
            {
                offsetX = (0.002f + dimensions.x) * (number - 32);
                offsetY = (-0.0055f - dimensions.y) * 4;
            }
            if (number > 39 && number < 48)
            {
                offsetX = (0.002f + dimensions.x) * (number - 40);
                offsetY = (-0.0055f - dimensions.y) * 5;
            }
            if (number > 47 && number < 56)
            {
                offsetX = (0.002f + dimensions.x) * (number - 48);
                offsetY = (-0.0055f - dimensions.y) * 6;
            }
            if (number > 55 && number < 64)
            {
                offsetX = (0.002f + dimensions.x) * (number - 56);
                offsetY = (-0.0055f - dimensions.y) * 7;
            }
            if (number > 63 && number < 72)
            {
                offsetX = (0.002f + dimensions.x) * (number - 64);
                offsetY = (-0.0055f - dimensions.y) * 8;
            }
            if (number > 71 && number < 80)
            {
                offsetX = (0.002f + dimensions.x) * (number - 72);
                offsetY = (-0.0055f - dimensions.y) * 9;
            }
            if (number > 79 && number < 88)
            {
                offsetX = (0.002f + dimensions.x) * (number - 80);
                offsetY = (-0.0055f - dimensions.y) * 10;
            }
            if (number > 87 && number < 96)
            {
                offsetX = (0.002f + dimensions.x) * (number - 88);
                offsetY = (-0.0055f - dimensions.y) * 11;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void AddUIString(BasePlayer player, string name)
        {
            if (!OpenUI.ContainsKey(player.userID))
                OpenUI.Add(player.userID, new List<string>());
            OpenUI[player.userID].Add(name);
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            DestroyEntries(player);             
        }
        private void DestroyEntries(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIPanel);
            if (OpenUI.ContainsKey(player.userID))
            {
                foreach (var entry in OpenUI[player.userID])                
                    CuiHelper.DestroyUi(player, entry);                
                OpenUI.Remove(player.userID);
            }
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("QUI_AcceptQuest")]
        private void cmdAcceptQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var questName = string.Join(" ", arg.Args);
            CheckPlayerEntry(player);
            var data = PlayerProgress[player.userID].Quests;
            if (!data.ContainsKey(questName))
            {
                var type = GetQuestType(questName);
                if (type != null)
                {
                    var quest = Quest[(QuestType)type][questName];
                    data.Add(questName, new PlayerQuestInfo { Status = QuestStatus.Pending, Type = (QuestType)type });
                    PlayerProgress[player.userID].RequiredItems.Add(new QuestInfo { ShortName = quest.Objective, Type = (QuestType)type });
                    DestroyEntries(player);
                    ListElement(player, (QuestType)type);
                    PopupMessage(player, $"{LA("qAccep", player.UserIDString)} {questName}");
                    return;
                }
            }
        }
        [ConsoleCommand("QUI_AcceptDelivery")]
        private void cmdAcceptDelivery(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var vendorID = arg.Args[0];
            var targetID = arg.Args[1];
            var distance = arg.Args[2];
            PlayerProgress[player.userID].CurrentDelivery = new ActiveDelivery { VendorID = vendorID, TargetID = targetID, Distance = float.Parse(distance) };
            PopupMessage(player, LA("dAccep", player.UserIDString));
            DestroyUI(player);
        }
        [ConsoleCommand("QUI_CancelDelivery")]
        private void cmdCancelDelivery(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!string.IsNullOrEmpty(PlayerProgress[player.userID].CurrentDelivery.TargetID))
            {
                PlayerProgress[player.userID].CurrentDelivery = new ActiveDelivery();
                DestroyUI(player);
                PopupMessage(player, LA("canConf", player.UserIDString));
            }
        }
        [ConsoleCommand("QUI_FinishDelivery")]
        private void cmdFinishDelivery(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (PlayerProgress[player.userID].CurrentDelivery != null)
            {
                var npcID = PlayerProgress[player.userID].CurrentDelivery.VendorID;
                var distance = PlayerProgress[player.userID].CurrentDelivery.Distance;
                var quest = vendors.DeliveryVendors[npcID];
                var rewardAmount = distance * quest.Multiplier;
                if (rewardAmount < 1) rewardAmount = 1;

                var reward = quest.Reward;
                reward.Amount = rewardAmount;
                if (GiveReward(player, new List<RewardItem> { reward }))
                {
                    PlayerProgress[player.userID].CurrentDelivery = new ActiveDelivery();
                    var rewards = GetRewardString(new List<RewardItem> { reward });
                    PopupMessage(player, $"{LA("rewRec", player.UserIDString)} {rewards}");
                }
                DestroyUI(player);
            }            
        }
        [ConsoleCommand("QUI_ChangeElement")]
        private void cmdChangeElement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CheckPlayerEntry(player);
            var panelName = arg.GetString(0);
            switch (panelName)
            {
                case "kill":
                    ListElement(player, QuestType.Kill);
                    return;
                case "gather":
                    ListElement(player, QuestType.Gather);
                    return;
                case "loot":
                    ListElement(player, QuestType.Loot);
                    return;
                case "craft":
                    ListElement(player, QuestType.Craft);
                    return;
                case "delivery":
                    PlayerDelivery(player);
                    return;
                case "personal":
                    PlayerStats(player);
                    return;
                case "editor":
                    if (player.IsAdmin())
                        DeletionEditMenu(player, LA("EDITOR", player.UserIDString), "QUI_EditQuest");
                    return;
                case "creation":
                    if (player.IsAdmin())
                    {
                        if (ActiveCreations.ContainsKey(player.userID))
                            ActiveCreations.Remove(player.userID);
                        CreationMenu(player);
                    }
                    return;
                case "objpage":
                    if (player.IsAdmin())
                    {
                        var pageNumber = arg.GetString(1);
                        CreateObjectiveMenu(player, int.Parse(pageNumber));
                    }
                    return;
                case "listpage":
                    {
                        var pageNumber = arg.GetString(2);
                        var type = ConvertStringToType(arg.GetString(1));
                        ListElement(player, type, int.Parse(pageNumber));
                    }
                    return;
                case "statspage":                    
                    {
                        var pageNumber = arg.GetString(1);
                        PlayerStats(player, int.Parse(pageNumber));
                    }
                    return;
            }
        }
        [ConsoleCommand("QUI_DestroyAll")]
        private void cmdDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (StatsMenu.Contains(player.userID))
                StatsMenu.Remove(player.userID);
            if (ActiveCreations.ContainsKey(player.userID))
                ActiveCreations.Remove(player.userID);
            if (ActiveEditors.ContainsKey(player.userID))
                ActiveEditors.Remove(player.userID);
            if (OpenMenuBind.Contains(player.userID))
                OpenMenuBind.Remove(player.userID);
            DestroyUI(player);
            OpenMap(player);
        }       
        [ConsoleCommand("QUI_NewQuest")]
        private void cmdNewQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var questType = arg.GetString(0);
                var Type = ConvertStringToType(questType);
                if (Type == QuestType.Delivery)
                {
                    DeliveryHelp(player);
                    return;
                }

                ActiveCreations.Add(player.userID, new QuestCreator { type = Type, entry = new QuestEntry { Rewards = new List<RewardItem>() }, item = new RewardItem() });
                DestroyUI(player);                
                CreationHelp(player);
            }
        }
        [ConsoleCommand("QUI_AddVendor")]
        private void cmdAddVendor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var vendorType = arg.GetString(0);
                bool isVendor = false;
                if (vendorType == "1")
                    isVendor = true;
                if (!AddVendor.ContainsKey(player.userID))
                    AddVendor.Add(player.userID, isVendor);
                DestroyUI(player);
                DeliveryHelp(player, 1);
            }
        }       
        [ConsoleCommand("QUI_SelectObj")]
        private void cmdSelectObj(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var questItem = string.Join(" ", arg.Args);
                QuestCreator Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];

                Creator.entry.Objective = questItem;
                if (DisplayNames.ContainsKey(questItem))
                    Creator.entry.ObjectiveName = DisplayNames[questItem];
                else
                    Creator.entry.ObjectiveName = questItem;

                Creator.partNum++;
                DestroyUI(player);
                
                CreationHelp(player, 2);
            }
        }
        [ConsoleCommand("QUI_RewardType")]
        private void cmdRewardType(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var rewardType = arg.GetString(0);
                QuestCreator Creator;

                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];                

                bool isRP = false;
                bool isCoins = false;
                bool isHuntXP = false;
                string name = "";

                switch (rewardType)
                {
                    case "rp":
                        isRP = true;
                        name = LA("RP", player.UserIDString);                        
                        break;
                    case "coins":
                        isCoins = true;
                        name = LA("Coins", player.UserIDString);
                        break;
                    case "huntxp":
                        isHuntXP = true;
                        name = LA("HuntXP", player.UserIDString);
                        break;                    
                    default:                        
                        break;
                }
                Creator.partNum = 5;
                if (Creator.type != QuestType.Delivery)
                {
                    Creator.item.isRP = isRP;
                    Creator.item.isCoins = isCoins;
                    Creator.item.isHuntXP = isHuntXP;
                    Creator.item.DisplayName = name;
                    CreationHelp(player, 5);                    
                }
                else
                {
                    Creator.deliveryInfo.Reward.isRP = isRP;
                    Creator.deliveryInfo.Reward.isCoins = isCoins;
                    Creator.deliveryInfo.Reward.isHuntXP = isHuntXP;
                    Creator.deliveryInfo.Reward.DisplayName = name;
                    DeliveryHelp(player, 3);
                }                
            }
        }
        [ConsoleCommand("QUI_ClaimReward")]
        private void cmdClaimReward(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var questName = string.Join(" ", arg.Args);
            var quest = GetQuest(questName);
            if (quest == null) return;
            if (GiveReward(player, quest.Rewards))
            {
                var questStatus = PlayerProgress[player.userID].Quests[questName];
                questStatus.Status = QuestStatus.Completed;
                questStatus.RewardClaimed = true;
                PlayerStats(player);

                var rewards = GetRewardString(quest.Rewards);                
                PopupMessage(player, $"{LA("rewRec", player.UserIDString)} {rewards}");
            }
            else
            {
                PopupMessage(player, LA("rewError", player.UserIDString));
            }
        }
        [ConsoleCommand("QUI_CancelQuest")]
        private void cmdCancelQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var questName = string.Join(" ", arg.Args);
            DestroyUI(player);          
            ConfirmCancellation(player, questName);
        }
        [ConsoleCommand("QUI_ItemDeduction")]
        private void cmdItemDeduction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                QuestCreator Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];
                switch (arg.Args[0])
                {
                    case "0":
                        Creator.entry.ItemDeduction = false;
                        break;                    
                    default:
                        Creator.entry.ItemDeduction = true;
                        break;
                }
                CreationHelp(player, 9);
            }
        }
        [ConsoleCommand("QUI_ConfirmCancel")]
        private void cmdConfirmCancel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var questName = string.Join(" ", arg.Args);
            if (questName.Contains("reject"))
            {
                DestroyUI(player);
                if (StatsMenu.Contains(player.userID))
                    CreateEmptyMenu(player);
                else CreateMenu(player);
                PlayerStats(player);
                return;
            }
            var quest = GetQuest(questName);
            if (quest == null) return;
            var info = PlayerProgress[player.userID];
            var items = info.RequiredItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ShortName == questName && items[i].Type == info.Quests[questName].Type)
                {
                    items.Remove(items[i]);
                    break;
                }
            }
            var type = (QuestType)GetQuestType(questName);
            if (type != QuestType.Delivery && type != QuestType.Kill)
            {
                string questitem = quest.Objective;
                int amount = info.Quests[questName].AmountCollected;
                if (quest.ItemDeduction)
                    ReturnItems(player, questitem, amount);
            }
            PlayerProgress[player.userID].Quests.Remove(questName);

            if (StatsMenu.Contains(player.userID))
                CreateEmptyMenu(player);
            else CreateMenu(player);

            PlayerStats(player);
        }
        [ConsoleCommand("QUI_RemoveCompleted")]
        private void cmdRemoveCompleted(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var questName = string.Join(" ", arg.Args);
            var quest = GetQuest(questName);
            if (quest == null) return;
            var info = PlayerProgress[player.userID];
            var items = info.RequiredItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ShortName == questName && items[i].Type == info.Quests[questName].Type)
                {
                    items.Remove(items[i]);
                    break;
                }
            }            
            PlayerProgress[player.userID].Quests.Remove(questName);           
            PlayerStats(player);
        }
        [ConsoleCommand("QUI_DeleteQuest")]
        private void cmdDeleteQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    DeletionEditMenu(player, LA("REMOVER", player.UserIDString), "QUI_ConfirmDelete");
                    return;
                }
                if (arg.Args.Length == 1 && arg.Args[0] == "reject")
                {
                    DestroyUI(player);
                    CreateMenu(player);
                    DeletionEditMenu(player, LA("REMOVER", player.UserIDString), "QUI_ConfirmDelete");
                    return;
                }
                var questName = string.Join(" ", arg.Args);
                RemoveQuest(questName);
                DestroyUI(player);
                CreateMenu(player);
                DeletionEditMenu(player, LA("REMOVER", player.UserIDString), "QUI_ConfirmDelete");
            }
        }        
        [ConsoleCommand("QUI_DeleteNPCMenu")]
        private void cmdDeleteNPCMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                DeleteNPCMenu(player);
            }
        }
        [ConsoleCommand("QUI_RemoveVendor")]
        private void cmdRemoveVendor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var ID = arg.Args[0];
                foreach(var npc in vendors.QuestVendors)
                {
                    if (npc.Key == ID)
                    {
                        RemoveVendor(player, ID, true);
                        return;
                    }
                }
                foreach (var npc in vendors.DeliveryVendors)
                {
                    if (npc.Key == ID)
                    {
                        RemoveVendor(player, ID, false);
                        return;
                    }
                }
            }
        }        
        [ConsoleCommand("QUI_ConfirmDelete")]
        private void cmdConfirmDelete(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var questName = string.Join(" ", arg.Args);
                DestroyUI(player);
                ConfirmDeletion(player, questName);                        
            }
        }        
        [ConsoleCommand("QUI_EditQuest")]
        private void cmdEditQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                if (ActiveEditors.ContainsKey(player.userID))
                    ActiveEditors.Remove(player.userID);
                ActiveEditors.Add(player.userID, new QuestCreator());

                var questName = string.Join(" ", arg.Args);
                var Quest = GetQuest(questName);
                if (Quest == null) return;
                ActiveEditors[player.userID].entry = Quest;
                ActiveEditors[player.userID].oldEntry = Quest.QuestName;
                ActiveEditors[player.userID].type = (QuestType)GetQuestType(questName);
                ActiveEditors[player.userID].item = new RewardItem();              
                QuestEditorMenu(player);
            }
        }
        [ConsoleCommand("QUI_EditQuestVar")]
        private void cmdEditQuestVar(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                if (ActiveEditors.ContainsKey(player.userID))
                {
                    var Creator = ActiveEditors[player.userID];

                    DestroyUI(player);
                    switch (arg.Args[0].ToLower())
                    {
                        case "name":
                            CreationHelp(player, 0);
                            break;
                        case "description":
                            Creator.partNum = 3;
                            CreationHelp(player, 3);                            
                            break;
                        case "objective":
                            Creator.partNum = 1;
                            CreationHelp(player, 1);
                            break;
                        case "amount":
                            Creator.partNum = 2;
                            CreationHelp(player, 2);
                            break;
                        case "reward":
                            Creator.partNum = 4;
                            CreationHelp(player, 10);
                            break;
                        default:
                            return;
                    }
                }
            }
        }
        [ConsoleCommand("QUI_RemoveReward")]
        private void cmdEditReward(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                QuestCreator Creator = ActiveEditors[player.userID];
                var amount = arg.Args[0];
                var dispName = arg.Args[1];
                foreach(var entry in Creator.entry.Rewards)
                {
                    if (entry.Amount == float.Parse(amount) && entry.DisplayName == dispName)
                    {
                        Creator.entry.Rewards.Remove(entry);
                        break;
                    }
                }
                SaveRewardsEdit(player);
            }
        }
        [ConsoleCommand("QUI_EndEditing")]
        private void cmdEndEditing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                CreateMenu(player);
                DeletionEditMenu(player, LA("EDITOR", player.UserIDString), "QUI_EditQuest");
            }
        }
        [ConsoleCommand("QUI_SaveQuest")]
        private void cmdSaveQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool creating = false;
                if (ActiveCreations.ContainsKey(player.userID))
                    creating = true;
                SaveQuest(player, creating);
            }
        }
        [ConsoleCommand("QUI_ExitQuest")]
        private void cmdExitQuest(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool creating = false;
                if (ActiveCreations.ContainsKey(player.userID))
                    creating = true;
                ExitQuest(player, creating);
            }
        }
        [ConsoleCommand("QUI_AddReward")]
        private void cmdAddReward(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                QuestCreator Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];
                Creator.partNum = 4;
                CreationHelp(player, 4);
            }
        }
        [ConsoleCommand("QUI_RewardFinish")]
        private void cmdFinishReward(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                CreationHelp(player, 8);
            }
        }
        [ConsoleCommand("QUI_OpenQuestMenu")]
        private void cmdOpenQuestMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (!OpenMenuBind.Contains(player.userID))
            {
                cmdOpenMenu(player, "q", new string[0]);
                OpenMenuBind.Add(player.userID);
            }
        }
        #endregion

        #region Chat Commands

        [ChatCommand("q")]
        void cmdOpenMenu(BasePlayer player, string command, string[] args)
        {
            if (AddVendor.ContainsKey(player.userID)) return;
            if ((configData.UseNPCVendors && player.IsAdmin()) || !configData.UseNPCVendors)
            {
                CheckPlayerEntry(player);
                CreateMenu(player);
                return;
            }
            if (configData.UseNPCVendors)
            {
                CheckPlayerEntry(player);
                if (!StatsMenu.Contains(player.userID))
                    StatsMenu.Add(player.userID);
                
                CreateEmptyMenu(player);
                PlayerStats(player);
                PopupMessage(player, LA("noVendor", player.UserIDString));
            }            
        }

        [ChatCommand("questnpc")]
        void cmdQuestNPC(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            var NPC = FindEntity(player);            
            if (NPC != null)
            {
                var isRegistered = isNPCRegistered(NPC.UserIDString);
                if (!string.IsNullOrEmpty(isRegistered))
                {
                    SendMSG(player, isRegistered, LA("Quest NPCs:", player.UserIDString));
                    return;
                }
                if (AddVendor.ContainsKey(player.userID))
                {
                    var pos = new NPCInfo { x = NPC.transform.position.x, z = NPC.transform.position.z, ID = NPC.UserIDString };
                    if (AddVendor[player.userID])
                    {
                        pos.Name = $"QuestVendor_{vendors.QuestVendors.Count + 1}";
                        vendors.QuestVendors.Add(NPC.UserIDString, pos);
                        SendMSG(player, LA("newVSucc", player.UserIDString), LA("Quest NPCs:", player.UserIDString));
                        if (NPC != null)
                        {
                            NPCdisplayName.SetValue(NPC, pos.Name);
                            NPC.UpdateNetworkGroup();
                        }
                        AddMapMarker(pos.x, pos.z, pos.Name, configData.Icon_Vendor);
                        AddVendor.Remove(player.userID);
                        SaveVendorData();
                        DestroyUI(player);
                        OpenMap(player);
                        return;
                    }
                    else
                    {
                        var name = $"Delivery_{ vendors.DeliveryVendors.Count + 1}";

                        if (ActiveCreations.ContainsKey(player.userID))
                            ActiveCreations.Remove(player.userID);
                        pos.Name = name;

                        ActiveCreations.Add(player.userID, new QuestCreator
                        {                            
                            deliveryInfo = new DeliveryInfo
                            {
                                Info = pos,
                                Reward = new RewardItem()
                            },
                            partNum = 4,
                            type = QuestType.Delivery
                        });
                        DeliveryHelp(player, 2);
                    }
                }
            }
            else SendMSG(player, LA("noNPC", player.UserIDString));
        }
        
        #endregion

        #region Data Management
        void SaveQuestData()
        {
            questData.Quest = Quest;
            Quest_Data.WriteObject(questData);
        }
        void SaveVendorData()
        {
            Quest_Vendors.WriteObject(vendors);
        }
        void SavePlayerData()
        {
            playerData.PlayerProgress = PlayerProgress;
            Player_Data.WriteObject(playerData);
        }
        void SaveDisplayNames()
        {
            itemNames.DisplayNames = DisplayNames;
            Item_Names.WriteObject(itemNames);
        }
        private void SaveLoop()
        {
            SavePlayerData();
            timer.Once(900, () => SaveLoop());
        }
        void LoadData()
        {
            try
            {
                questData = Quest_Data.ReadObject<QuestData>();
                Quest = questData.Quest;
            }
            catch
            {
                Puts("Couldn't load quest data, creating new datafile");
                questData = new QuestData();                
            }
            try
            {
                vendors = Quest_Vendors.ReadObject<NPCData>();
            }
            catch
            {
                Puts("Couldn't load quest vendor data, creating new datafile");
                vendors = new NPCData();
            }
            try
            {
                playerData = Player_Data.ReadObject<PlayerData>();
                PlayerProgress = playerData.PlayerProgress;
            }
            catch
            {
                Puts("Couldn't load player data, creating new datafile");
                playerData = new PlayerData();
                PlayerProgress = new Dictionary<ulong, PlayerQuestData>();
            }
            try
            {
                itemNames = Item_Names.ReadObject<ItemNames>();                
            }
            catch
            {
                Puts("Couldn't load item display name data, creating new datafile");
                itemNames = new ItemNames();
            }
        }
        #endregion

        #region Data Storage
        class QuestData
        {
            public Dictionary<QuestType, Dictionary<string, QuestEntry>> Quest = new Dictionary<QuestType, Dictionary<string, QuestEntry>>
            {
                {QuestType.Craft, new Dictionary<string, QuestEntry>() },
                {QuestType.Delivery, new Dictionary<string, QuestEntry>() },
                {QuestType.Gather, new Dictionary<string, QuestEntry>() },
                {QuestType.Kill, new Dictionary<string, QuestEntry>() },
                {QuestType.Loot, new Dictionary<string, QuestEntry>() }
            };
        }
        class PlayerData
        {
            public Dictionary<ulong, PlayerQuestData> PlayerProgress = new Dictionary<ulong, PlayerQuestData>();
        }
        class NPCData
        {
            public Dictionary<string, NPCInfo> QuestVendors = new Dictionary<string, NPCInfo>();
            public Dictionary<string, DeliveryInfo> DeliveryVendors = new Dictionary<string, DeliveryInfo>();
        }
        #endregion

        #region Config

        class ConfigData
        {          
            public bool DisableUI_FadeIn { get; set; } 
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
            public string Icon_Vendor { get; set; }
            public string Icon_Delivery { get; set; }
            public int Quest_Cooldown { get; set; }
            public bool UseNPCVendors { get; set; }
            public bool Autoset_KeyBind { get; set; }
            public string KeyBind_Key { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            ConfigData config = new ConfigData
            {
                Autoset_KeyBind = false,
                DisableUI_FadeIn = false,
                KeyBind_Key = "k",
                MSG_MainColor = "<color=orange>",
                MSG_Color = "<color=#939393>",
                Icon_Delivery = "deliveryicon",
                Icon_Vendor = "vendoricon",
                Quest_Cooldown = 1440,
                UseNPCVendors = false
            };
            SaveConfig(config);
        }
        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region Messaging
        void SendMSG(BasePlayer player, string message, string keyword = "")
        {
            message = $"{configData.MSG_Color}{message}</color>";
            if (!string.IsNullOrEmpty(keyword))
                message = $"{configData.MSG_MainColor}{keyword}</color> {message}";
            SendReply(player, message);
        }
        Dictionary<string, string> Localization = new Dictionary<string, string>
        {
            { "Quests", "Quests:" },
            { "delInprog", "You already have a delivery mission in progress." },
            { "QC", "Quest Creator:" },
            { "noAItem", "Unable to find a active item. Place the item in your hands then type " },
            { "nameExists", "A quest with this name already exists" },
            { "objAmount", "You need to enter a objective amount" },
            { "OA", "Objective Amount:" },
            { "Desc", "Description:" },
            { "noRM", "You need to enter a reward multiplier" },
            { "RM", "Reward Multiplier:" },
            { "noRA", "You need to enter a reward amount" },
            { "RA", "Reward Amount:" },
            { "noCD", "You need to enter a cooldown amount" },
            { "CD1", "Cooldown Timer (minutes):" },
            { "qComple", "You have completed the quest" },
            { "claRew", "You can claim your reward from the quest menu." },
            { "qCancel", "You have cancelled this quest." },
            { "rewRet", "has been returned to you" },
            { "minDV", "Delivery missions require atleast 2 vendors. Add some more vendors to activate delivery missions" },
            { "DVSucc", "You have successfully added a new delivery vendor" },
            { "saveQ", "You have successfully saved the quest:" },
            { "QCCancel", "You have cancelled quest creation" },
            { "KillOBJ", "Kill quests require you to kill 'X' amount of the target objective" },
            { "CraftOBJ", "Crafting quests require you to craft 'X' amount of the objective item" },
            { "GatherOBJ", "Gather quests require you to gather 'X' amount of the objective from resources" },
            { "LootOBJ", "Loot quests require you to collect 'X' amount of the objective item from containers" },
            { "DelvOBJ", "Delivery quests require you to deliver a package from one vendor to another" },
            { "aQVReg", "This NPC is already a registered Quest vendor" },
            { "aDVReg", "This NPC is already a registed Delivery vendor" },
            { "Kill", "Kill" },
            { "Gather", "Gather" },
            { "Craft", "Craft" },
            { "Loot", "Loot" },
            { "Delivery", "Delivery" },
            { "Your Quests", "Your Quests" },
            { "Create Quest", "Create Quest" },
            { "Edit Quest", "Edit Quest" },
            { "Delete Quest", "Delete Quest" },
            { "Close", "Close" },
            { "Next", "Next" },
            { "Back", "Back" },
            { "noQ", "The are currently no" },
            { "quests", "quests" },
            { "Pending", "Pending" },
            { "Completed", "Completed" },
            { "Accept Quest", "Accept Quest" },
            { "Status:", "Status:" },
            { "Description:", "Description:" },
            { "Amount Required:", "Amount Required:" },
            { "Reward:", "Reward:" },
            { "yqDesc", "Check your current progress for each quest" },
            { "STATS", "STATS" },
            { "noQDSaved", "You don't have any quest data saved" },
            { "Cancel Quest", "Cancel Quest" },
            { "Claim Reward", "Claim Reward" },
            { "Remove", "Remove" },
            { "Cooldown", "Cooldown" },
            { "Collected:", "Collected:" },
            { "Reward Claimed:", "Reward Claimed:" },
            { "DELIVERY", "DELIVERY" },
            { "noADM", "You do not have a active delivery mission" },
            { "Destination:", "Destination:" },
            { "Distance:", "Distance:" },
            { "Cancel", "Cancel" },
            { "selCreat", "Select a quest type to begin creation" },
            { "CREATOR", "CREATOR" },
            { "creHelMen", "This is the quest creation help menu" },
            { "creHelFol", "Follow the instructions given by typing in chat" },
            { "creHelExi", "You can exit quest creation at any time by typing" },
            { "creHelName", "To proceed enter the name of your new quest!" },
            { "creHelObj", "Choose a quest objective from the list" },
            { "creHelRA", "Enter a required amount" },
            { "creHelQD", "Enter a quest description" },
            { "creHelRT", "Choose a reward type" },
            { "creHelNewRew", "Select a reward to remove, or add a new one" },
            { "Coins", "Coins" },
            { "RP", "RP" },
            { "HuntXP", "XP" },            
            { "Item", "Item" },
            { "creHelRewA", "Enter a reward amount" },
            { "creHelIH", "Place the item you want to issue as a reward in your hands and type" },
            { "creHelAR", "Would you like to add additional rewards?" },
            { "Yes", "Yes" },
            { "No", "No" },
            { "creHelID", "Would you like to enable item deduction (take items from player when collected)?" },
            { "creHelCD", "Enter a cooldown time (in minutes)" },
            { "creHelSQ", "You have successfully created a new quest. To confirm click 'Save Quest'" },
            { "Save Quest", "Save Quest" },
            { "Name:", "Name:" },
            { "Objective:", "Objective:" },
            { "CDMin", "Cooldown (minutes):" },
            { "Quest Type:", "Quest Type:" },
            { "Required Amount:", "Required Amount:" },
            { "Item Deduction:", "Item Deduction:" },
            { "delHelMen", "Here you can add delivery missions and Quest vendors." },
            { "delHelChoo", "Choose either a Deiver vendor (delivery mission) or a Quest vendor (npc based quest menu)" },
            { "Quest Vendor", "Quest Vendor" },
            { "Delivery Vendor", "Delivery Vendor" },
            { "delHelNewNPC", "Stand infront of the NPC you wish to add and type" },
            { "delHelMult", "Delivery mission rewards are based on distance X a multiplier. Keep this in mind when selecting a reward." },
            { "delHelRM", "Enter a reward multiplier (per unit)." },
            { "delHelRM1", "For example, if a delivery is" },
            { "delHelRM2", "away, and the multiplier is" },
            { "delHelRM3", "the total reward amount would be" },
            { "delHelDD", "Enter a delivery description." },
            { "delHelNewV", "You have successfully added a new delivery vendor. To confirm click 'Save Quest'" },
            { "Accept", "Accept" },
            { "Decline", "Decline" },
            { "Claim", "Claim" },
            { "delComplMSG", "Thanks for making the delivery" },
            { "Delete NPC", "Delete NPC" },
            { "REMOVER", "REMOVER" },
            { "Delivery Vendors", "Delivery Vendors" },
            { "Quest Vendors", "Quest Vendors" },
            { "confDel", "Are you sure you want to delete:" },
            { "confCan", "Are you sure you want to cancel:" },
            { "confCan2", "Any progress you have made will be lost!" },
            { "EDITOR", "EDITOR" },
            { "chaEdi", "Select a value to change" },
            { "Name", "Name" },
            { "Description", "Description" },
            { "Objective", "Objective" },
            { "Amount", "Amount" },
            { "Reward", "Reward" },
            { "qAccep", "You have accepted the quest" },
            { "dAccep", "You have accepted the delivery mission" },
            { "canConf", "You have cancelled the delivery mission" },
            { "rewRec", "You have recieved" },
            { "rewError", "Unable to issue your reward. Please contact an administrator" },
            { "Quest NPCs:", "Quest NPCs:" },
            { "newVSucc", "You have successfully added a new Quest vendor" },
            { "noNPC", "Unable to find a valid NPC" },
            { "addNewRew", "Add Reward" },
            { "NoTP", "You cannot teleport while you are on a delivery mission" },
            { "noVendor", "To accept new Quests you must find a Quest Vendor" }
        };
        #endregion
    }
}
