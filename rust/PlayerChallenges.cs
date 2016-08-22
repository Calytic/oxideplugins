using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PlayerChallenges", "k1lly0u", "2.0.35", ResourceId = 1442)]
    class PlayerChallenges : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin BetterChat;
        [PluginReference] Plugin LustyMap;
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;        

        ChallengeData chData;
        private DynamicConfigFile data;

        private Dictionary<ulong, StatData> statCache = new Dictionary<ulong, StatData>();
        private Dictionary<CTypes, LeaderData> titleCache = new Dictionary<CTypes, LeaderData>();

        private bool UIDisabled = false;
        #endregion

        #region UI Creation
        class PCUI
        {
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
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0f)
            {               
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0f)
            {                
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = fadein },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
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

        #region UI Leaderboard
        static string UIMain = "PCUI_Main";
        static string UIPanel = "PCUI_Panel";
        private void CreateMenu(BasePlayer player)
        {
            CloseMap(player);
            CuiHelper.DestroyUi(player, UIPanel);
            var MenuElement = PCUI.CreateElementContainer(UIMain, UIColors["dark"], "0 0", "1 1", true);
            PCUI.CreatePanel(ref MenuElement, UIMain, UIColors["light"], "0.005 0.93", "0.995 0.99");
            var vNum = Version;
            PCUI.CreateLabel(ref MenuElement, UIMain, "", $"{configData.Messaging.MSG_ColorMain}{MSG("UITitle").Replace("{Version}", vNum.ToString())}</color>", 22, "0.05 0.93", "0.6 0.99", TextAnchor.MiddleLeft);           
            
            CuiHelper.AddUi(player, MenuElement);
            CreateMenuContents(player, 0);
        }
        private void CreateMenuContents(BasePlayer player, int page = 0)
        {
            var MenuElement = PCUI.CreateElementContainer(UIPanel, "0 0 0 0", "0 0", "1 1");           
            switch (page)
            {
                case 0:
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[0], "0.005 0.01", "0.195 0.92", "0.01 0.01", "0.19 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[1], "0.205 0.01", "0.395 0.92", "0.21 0.01", "0.39 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[2], "0.405 0.01", "0.595 0.92", "0.41 0.01", "0.59 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[3], "0.605 0.01", "0.795 0.92", "0.61 0.01", "0.79 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[4], "0.805 0.01", "0.995 0.92", "0.81 0.01", "0.99 0.91");                    
                    break;
                case 1:
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[5], "0.005 0.01", "0.195 0.92", "0.01 0.01", "0.19 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[6], "0.205 0.01", "0.395 0.92", "0.21 0.01", "0.39 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[7], "0.405 0.01", "0.595 0.92", "0.41 0.01", "0.59 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[8], "0.605 0.01", "0.795 0.92", "0.61 0.01", "0.79 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[9], "0.805 0.01", "0.995 0.92", "0.81 0.01", "0.99 0.91");
                    break;
                case 2:
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[10], "0.005 0.01", "0.195 0.92", "0.01 0.01", "0.19 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[11], "0.205 0.01", "0.395 0.92", "0.21 0.01", "0.39 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[12], "0.405 0.01", "0.595 0.92", "0.41 0.01", "0.59 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[13], "0.605 0.01", "0.795 0.92", "0.61 0.01", "0.79 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[14], "0.805 0.01", "0.995 0.92", "0.81 0.01", "0.99 0.91");
                    break;
                case 3:
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[15], "0.005 0.01", "0.195 0.92", "0.01 0.01", "0.19 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[16], "0.205 0.01", "0.395 0.92", "0.21 0.01", "0.39 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[17], "0.405 0.01", "0.595 0.92", "0.41 0.01", "0.59 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[18], "0.605 0.01", "0.795 0.92", "0.61 0.01", "0.79 0.91");
                    AddMenuStats(ref MenuElement, UIPanel, configData.UI_Arrangement[19], "0.805 0.01", "0.995 0.92", "0.81 0.01", "0.99 0.91");
                    break;
                default:
                    break;
            }

            if (page > 0) PCUI.CreateButton(ref MenuElement, UIPanel, UIColors["buttonbg"], "Previous", 16, "0.63 0.94", "0.73 0.98", $"PCUI_ChangePage {page - 1}");
            if (page < 3) PCUI.CreateButton(ref MenuElement, UIPanel, UIColors["buttonbg"], "Next", 16, "0.74 0.94", "0.84 0.98", $"PCUI_ChangePage {page + 1}");
            PCUI.CreateButton(ref MenuElement, UIPanel, UIColors["buttonbg"], "Close", 16, "0.85 0.94", "0.95 0.98", "PCUI_DestroyAll");
            CuiHelper.AddUi(player, MenuElement);
        }
        private void AddMenuStats(ref CuiElementContainer MenuElement, string panel, string type, string posMinA, string posMaxA, string posMinB, string posMaxB)
        {
            var entry = GetTypeFromString(type);
            if (entry != null)
            {
                var ctype = (CTypes)entry;
                if (configData.ActiveChallengeTypes[type])
                {
                    PCUI.CreatePanel(ref MenuElement, UIPanel, UIColors["light"], posMinA, posMaxA);
                    PCUI.CreateLabel(ref MenuElement, UIPanel, "", GetLeaders(ctype), 16, posMinB, posMaxB, TextAnchor.UpperLeft);
                }
            }            
        }

        #region UI Commands       
        [ConsoleCommand("PCUI_ChangePage")]
        private void cmdChangePage(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, UIPanel);
            var page = int.Parse(arg.GetString(0));
            CreateMenuContents(player, page);
        }
        [ConsoleCommand("PCUI_DestroyAll")]
        private void cmdDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;           
            DestroyUI(player);
            OpenMap(player);
        }
        #endregion

        #region UI Functions
        private string GetLeaders(CTypes type)
        {
            var listNames = $" -- {configData.Messaging.MSG_ColorMain}{MSG(type.ToString().ToLower()).ToUpper()}</color>\n\n";

            var userStats = new Dictionary<string, int>();

            foreach (var entry in statCache)
            {
                var name = entry.Value.DisplayName;
                if (userStats.ContainsKey(entry.Value.DisplayName))
                {
                    name += $"({UnityEngine.Random.Range(0, 1000)})";
                }
                userStats.Add(name, entry.Value.Stats[type]);
            }
                

            var leaders = userStats.OrderByDescending(a => a.Value).Take(25);

            int i = 1;

            foreach (var entry in leaders)
            {
                listNames += $"{i}.  - {configData.Messaging.MSG_ColorMain}{entry.Value}</color> -  {entry.Key}\n";
                i++;            
            }
            return listNames;
        }
        private object GetTypeFromString(string name)
        {
            foreach(var type in typeList)
            {
                if (type.ToString() == name)
                    return type;
            }
            return null;
        }
        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.DestroyUi(player, UIPanel);
        }
        #endregion
        #endregion

        #region External Calls
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
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(ulong playerId, ulong friendId)
        {
            if (!Friends) return false;
            bool isFriend = (bool)Friends?.Call("IsFriend", playerId, friendId);
            return isFriend;
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("challenge_data");
            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            CheckValidData();
            RegisterGroups();
            AddAllUsergroups();
            SaveLoop();
            if (configData.UI_Arrangement.Count != 20)
            {
                UIDisabled = true;
                PrintError("The UI has been disabled as there is a error in your config.\n The UI arrangement list does not contain enough entries. Check the overview to ensure you have all challenge types in the list");
            }
            if (configData.Options.UseUpdateTimer)
                CheckUpdateTimer();                    
        }
        void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
            RemoveAllUsergroups();
        }
        
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Rockets.ToString()]) return;            
            AddPoints(player, CTypes.Rockets, 1);
        }
        void OnHealingItemUse(HeldEntity item, BasePlayer target)
        {
            var player = item.GetOwnerPlayer();
            if (player == null) return;
            if (player != target && configData.ActiveChallengeTypes[CTypes.Healed.ToString()])
            {
                AddPoints(player, CTypes.Healed, 1);
            }            
        }
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            var player = task.owner;
            if (player == null) return;

            if (item.info.category == ItemCategory.Attire && configData.ActiveChallengeTypes[CTypes.Clothes.ToString()])
                AddPoints(player, CTypes.Clothes, 1);
            if (item.info.category == ItemCategory.Weapon && configData.ActiveChallengeTypes[CTypes.Weapons.ToString()])
                AddPoints(player, CTypes.Weapons, 1);
        }
        void OnPlantGather(PlantEntity plant, Item item, BasePlayer player)
        {
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Plants.ToString()]) return;
            AddPoints(player, CTypes.Plants, 1);
        }
        void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (item == null) return;
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Plants.ToString()]) return;
            if (plantShortnames.Contains(item?.info?.shortname))
                AddPoints(player, CTypes.Plants, 1);
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();
            if (player == null || dispenser == null) return;

            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree && configData.ActiveChallengeTypes[CTypes.Wood.ToString()])
                AddPoints(player, CTypes.Wood, item.amount);

            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore && configData.ActiveChallengeTypes[CTypes.Rocks.ToString()])
                AddPoints(player, CTypes.Rocks, item.amount);               
        }
        void OnEntityBuilt(Planner plan, GameObject go)
        {
            var player = plan.GetOwnerPlayer();
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Built.ToString()]) return;

            AddPoints(player, CTypes.Built, 1);
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                var attacker = info?.InitiatorPlayer;
                if (attacker == null) return;
                CheckEntry(attacker);
                if (entity.ToPlayer() != null)
                {
                    if (IsFriend(attacker.userID, entity.ToPlayer().userID)) return;
                    if (IsClanmate(attacker.userID, entity.ToPlayer().userID)) return;
                    if (configData.Options.IgnoreSleepers && entity.ToPlayer().IsSleeping()) return;

                    var distance = Vector3.Distance(attacker.transform.position, entity.transform.position);
                    AddDistance(attacker, CTypes.PVPKill, (int)distance);

                    if (info.isHeadshot && configData.ActiveChallengeTypes[CTypes.Headshots.ToString()])
                        AddPoints(attacker, CTypes.Headshots, 1);
                    var weapon = info?.Weapon?.GetItem()?.info?.shortname;
                    if (!string.IsNullOrEmpty(weapon))
                    {
                        if (bladeShortnames.Contains(weapon) && configData.ActiveChallengeTypes[CTypes.Swords.ToString()])
                            AddPoints(attacker, CTypes.Swords, 1);
                        else if (meleeShortnames.Contains(weapon) && configData.ActiveChallengeTypes[CTypes.Melee.ToString()])
                            AddPoints(attacker, CTypes.Melee, 1);
                        else if (weapon == "bow.hunting" && configData.ActiveChallengeTypes[CTypes.Arrows.ToString()])
                            AddPoints(attacker, CTypes.Arrows, 1);
                        else if (weapon == "pistol.revolver" && configData.ActiveChallengeTypes[CTypes.Revolver.ToString()])
                            AddPoints(attacker, CTypes.Revolver, 1);
                        else if (configData.ActiveChallengeTypes[CTypes.Killed.ToString()]) AddPoints(attacker, CTypes.Killed, 1);
                    }
                }
                else if (entity.GetComponent<BaseNPC>() != null)
                {
                    var distance = Vector3.Distance(attacker.transform.position, entity.transform.position);
                    AddDistance(attacker, CTypes.PVEKill, (int)distance);
                    AddPoints(attacker, CTypes.Animals, 1);
                }
            }
            catch { }          
        }
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Explosives.ToString()]) return;
            AddPoints(player, CTypes.Explosives, 1);
        }
        void OnStructureRepair(BaseCombatEntity block, BasePlayer player)
        {
            if (player == null || !configData.ActiveChallengeTypes[CTypes.Repaired.ToString()]) return;
            AddPoints(player, CTypes.Repaired, 1);
        }
        #endregion

        #region Hooks
        [HookMethod("CompletedQuest")]
        public void CompletedQuest(BasePlayer player)
        {
            CheckEntry(player);
            AddPoints(player, CTypes.Quests, 1);            
        }
        #endregion

        #region Functions        
        private void AddPoints(BasePlayer player, CTypes type, int amount)
        {
            if (configData.Options.IgnoreAdmins && player.IsAdmin()) return;
            CheckEntry(player);
            statCache[player.userID].Stats[type] += amount;            
            CheckForUpdate(player, type);
        }
        private void AddDistance(BasePlayer player, CTypes type, int amount)
        {
            if (configData.Options.IgnoreAdmins && player.IsAdmin()) return;
            CheckEntry(player);
            if (statCache[player.userID].Stats[type] < amount)
                statCache[player.userID].Stats[type] = amount;
            CheckForUpdate(player, type);
        }
        private void CheckForUpdate(BasePlayer player, CTypes type)
        {
            if (titleCache[type].UserID == player.userID)
            {
                titleCache[type].Count = statCache[player.userID].Stats[type];
                return;
            }
            if (!configData.Options.UseUpdateTimer)
            {
                if (statCache[player.userID].Stats[type] > titleCache[type].Count)
                {
                    SwitchLeader(player.userID, type);
                }
            }         
        }
        private void SwitchLeader(ulong ID, CTypes type)
        {    
            if (configData.Options.UseBetterChat && BetterChat)
            {
                var name = GetGroupName(type);

                if (UserInGroup(titleCache[type].UserID.ToString(), name))
                    RemoveFromGroup(titleCache[type].UserID.ToString(), name);

                AddToGroup(ID.ToString(), name);
            }

            titleCache[type] = new LeaderData
            {
                Count = statCache[ID].Stats[type],
                DisplayName = statCache[ID].DisplayName,
                UserID = ID
            };

            if (configData.Options.AnnounceNewLeaders)
            {
                string message = MSG("newLeader", ID.ToString())
                    .Replace("{playername}", $"{configData.Messaging.MSG_ColorMain}{statCache[ID].DisplayName}</color>{configData.Messaging.MSG_ColorMsg}")
                    .Replace("{ctype}", $"</color>{configData.Messaging.MSG_ColorMain}{MSG(type.ToString().ToLower(), ID.ToString())}</color>");
                PrintToChat(message);
            }
        }
        private void AddAllUsergroups()
        {
            foreach (var type in titleCache)
            {
                var name = GetGroupName(type.Key);
                if (titleCache[type.Key].UserID == 0U) continue;
                if (!UserInGroup(titleCache[type.Key].UserID.ToString(), name))
                    AddToGroup(titleCache[type.Key].UserID.ToString(), name);
            }
        }
        private void RemoveAllUsergroups()
        {
            foreach (var type in titleCache)
            {
                var name = GetGroupName(type.Key);
                if (titleCache[type.Key].UserID == 0U) continue;
                if (UserInGroup(titleCache[type.Key].UserID.ToString(), name))
                    RemoveFromGroup(titleCache[type.Key].UserID.ToString(), name);
            }
        }
        private void CheckUpdateTimer()
        {
            if ((GrabCurrentTime() - chData.LastUpdate) > configData.Options.UpdateTimer)
            {
                var Updates = new Dictionary<CTypes, ulong>();

                foreach (var type in titleCache)
                    Updates.Add(type.Key, type.Value.UserID);

                foreach (var entry in statCache)
                {
                    foreach (var stat in entry.Value.Stats)
                    {
                        if (stat.Value > titleCache[stat.Key].Count)
                            Updates[stat.Key] = entry.Key;
                    }
                }

                foreach (var entry in Updates)
                {
                    if (titleCache[entry.Key].UserID != Updates[entry.Key])
                    {
                        SwitchLeader(entry.Value, entry.Key);
                    }
                }
            }
            else
            {
                var timeRemaining = ((configData.Options.UpdateTimer - (GrabCurrentTime() - chData.LastUpdate)) * 60) * 60;
                timer.Once((int)timeRemaining + 10, () => CheckUpdateTimer());
            }
        }

        #endregion

        #region Chat Commands
        [ChatCommand("pc")]
        private void cmdPC(BasePlayer player, string command, string[] args)
        {
            if (!UIDisabled)
                CreateMenu(player);
            else SendReply(player, MSG("UIDisabled", player.UserIDString));
        }
        [ChatCommand("pc_wipe")]
        private void cmdPCWipe(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin()) return;
            RemoveAllUsergroups();
            titleCache = new Dictionary<CTypes, LeaderData>();
            statCache = new Dictionary<ulong, StatData>();            
            CheckValidData();
            SendReply(player, MSG("dataWipe", player.UserIDString));
            SaveData();
        }

        #endregion

        #region Helper Methods
        private void CheckEntry(BasePlayer player)
        {
            if (!statCache.ContainsKey(player.userID))
            {
                statCache.Add(player.userID, new StatData
                {
                    DisplayName = player.displayName,
                    Stats = new Dictionary<CTypes, int>()
                });
                foreach (var type in typeList)
                    statCache[player.userID].Stats.Add(type, 0);
            }
        }
        private string GetGroupName(CTypes type)
        {
            switch (type)
            {
                case CTypes.Animals:
                    return configData.Titles.AnimalsKilled;
                case CTypes.Arrows:
                    return configData.Titles.ArrowKills;
                case CTypes.Clothes:
                    return configData.Titles.ClothesCrafted;
                case CTypes.Headshots:
                    return configData.Titles.Headshots;
                case CTypes.Plants:
                    return configData.Titles.PlantsGathered;
                case CTypes.Healed:
                    return configData.Titles.PlayersHealed;
                case CTypes.Killed:
                    return configData.Titles.PlayersKilled;
                case CTypes.Melee:
                    return configData.Titles.MeleeKills;
                case CTypes.Revolver:
                    return configData.Titles.RevolverKills;
                case CTypes.Rockets:
                    return configData.Titles.RocketsFired;
                case CTypes.Rocks:
                    return configData.Titles.RocksGathered;
                case CTypes.Swords:
                    return configData.Titles.BladeKills;
                case CTypes.Built:
                    return configData.Titles.StructuresBuilt;
                case CTypes.Repaired:
                    return configData.Titles.StructuresRepaired;
                case CTypes.Explosives:
                    return configData.Titles.ThrownExplosives;
                case CTypes.Weapons:
                    return configData.Titles.WeaponsCrafted;
                case CTypes.Wood:
                    return configData.Titles.WoodGathered;
                case CTypes.Quests:
                    return configData.Titles.QuestsCompleted;
                case CTypes.PVPKill:
                    return configData.Titles.PVPKillDistance;
                case CTypes.PVEKill:
                    return configData.Titles.PVEKillDistance;
                default:
                    return null;
            }
        }
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).Hours;
        #endregion

        #region BetterChat Intergration
        private void RegisterGroups()
        {
            if (!BetterChat || !configData.Options.UseBetterChat) return;
            if (BetterChat.Version < new VersionNumber(4,0,0))
            {
                PrintError("You are using an old version of BetterChat that is not supported by this plugin");
                return;
            }
            foreach (var type in typeList)
                RegisterGroup(type);
        }
        private void RegisterGroup(CTypes type)
        {
            var name = GetGroupName(type);
            if (!GroupExists(name))
            {
                NewGroup(name);
                SetGroupTitle(name);
                SetGroupColor(name);
            }
        }
        
        private bool GroupExists(string name) => (bool)BetterChat?.Call("API_GroupExists", name);
        private bool NewGroup(string name) => (bool)BetterChat?.Call("API_AddGroup", name);
        private bool UserInGroup(string ID, string name) => (bool)BetterChat?.Call("API_IsUserInGroup", ID, name);
        private bool AddToGroup(string ID, string name) => (bool)BetterChat?.Call("API_AddUserToGroup", ID, name);
        private bool RemoveFromGroup(string ID, string name) => (bool)BetterChat?.Call("API_RemoveUserFromGroup", ID, name);
        private object SetGroupTitle(string name) => BetterChat?.Call("API_SetGroupSetting", name, "title", $"[{name}]");
        private object SetGroupColor(string name) => BetterChat?.Call("API_SetGroupSetting", name, "titlecolor", "orange");
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public Titles Titles { get; set; }
            public Dictionary<string, bool> ActiveChallengeTypes { get; set; }
            public Options Options { get; set; } 
            public Messaging Messaging { get; set; }
            public List<string> UI_Arrangement { get; set; }
        }       
        class Titles
        {
            public string ThrownExplosives;
            public string RocketsFired;
            public string ArrowKills;
            public string PlayersKilled;
            public string Headshots;
            public string AnimalsKilled;
            public string StructuresBuilt;
            public string WoodGathered;
            public string RocksGathered;
            public string PlantsGathered;
            public string ClothesCrafted;
            public string WeaponsCrafted;
            public string PlayersHealed;
            public string RevolverKills;
            public string StructuresRepaired;
            public string MeleeKills;
            public string BladeKills;
            public string QuestsCompleted;
            public string PVPKillDistance;
            public string PVEKillDistance;
        }
       
        class Options
        {
            public bool IgnoreSleepers;
            public bool UseBetterChat;
            public bool IgnoreAdmins;
            public bool AnnounceNewLeaders;
            public bool UseUpdateTimer;
            public int UpdateTimer;
            public int SaveTimer;
        }
        class Messaging
        {
            public string MSG_ColorMain;
            public string MSG_ColorMsg;
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
                ActiveChallengeTypes = new Dictionary<string, bool>
                {
                    {CTypes.Animals.ToString(), true },
                    {CTypes.Arrows.ToString(), true },
                    {CTypes.Built.ToString(), true },
                    {CTypes.Clothes.ToString(), true },
                    {CTypes.Explosives.ToString(), true },
                    {CTypes.Headshots.ToString(), true },
                    {CTypes.Healed.ToString(), true },
                    {CTypes.Killed.ToString(), true },
                    {CTypes.Melee.ToString(), true },
                    {CTypes.Plants.ToString(), true },
                    {CTypes.PVEKill.ToString(), true },
                    {CTypes.PVPKill.ToString(), true },
                    {CTypes.Quests.ToString(), true },
                    {CTypes.Repaired.ToString(), true },
                    {CTypes.Revolver.ToString(), true },
                    {CTypes.Rockets.ToString(), true },
                    {CTypes.Rocks.ToString(), true },
                    {CTypes.Swords.ToString(), true },
                    {CTypes.Weapons.ToString(), true },
                    {CTypes.Wood.ToString(), true }

                },
                Titles = new Titles
                {
                    AnimalsKilled = "Hunter",
                    ArrowKills = "Archer",
                    BladeKills = "Swordsman",
                    ClothesCrafted = "Tailor",
                    Headshots = "Assassin",
                    MeleeKills = "Fighter",
                    PlantsGathered = "Harvester",
                    PlayersHealed = "Medic",
                    PlayersKilled = "Murderer",
                    RevolverKills = "Gunslinger",
                    RocketsFired = "Rocketeer",
                    RocksGathered = "Miner",
                    StructuresBuilt = "Architect",
                    StructuresRepaired = "Handyman",
                    ThrownExplosives = "Bomb-tech",
                    WeaponsCrafted = "Gunsmith",
                    WoodGathered = "Lumberjack",
                    QuestsCompleted = "Adventurer",
                    PVPKillDistance = "Sniper",
                    PVEKillDistance = "Deadshot"
                },
                Options = new Options
                {
                    AnnounceNewLeaders = false,
                    IgnoreAdmins = true,
                    IgnoreSleepers = true,
                    SaveTimer = 600,
                    UseBetterChat = true,
                    UseUpdateTimer = false,
                    UpdateTimer = 168
                },
                Messaging = new Messaging
                {
                    MSG_ColorMain = "<color=orange>",
                    MSG_ColorMsg = "<color=#939393>",
                }  ,
                UI_Arrangement = new List<string>
                {
                    CTypes.Animals.ToString(),
                    CTypes.Arrows.ToString(),
                    CTypes.Built.ToString(),
                    CTypes.Clothes.ToString(),
                    CTypes.Explosives.ToString(),
                    CTypes.Headshots.ToString(),
                    CTypes.Healed.ToString(),
                    CTypes.Killed.ToString(),
                    CTypes.Melee.ToString(),
                    CTypes.Plants.ToString(),
                    CTypes.PVEKill.ToString(),
                    CTypes.PVPKill.ToString(),
                    CTypes.Quests.ToString(),
                    CTypes.Repaired.ToString(),
                    CTypes.Revolver.ToString(),
                    CTypes.Rockets.ToString(),
                    CTypes.Rocks.ToString(),
                    CTypes.Swords.ToString(),
                    CTypes.Weapons.ToString(),
                    CTypes.Wood.ToString()
                }                            
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveLoop() => timer.Once(configData.Options.SaveTimer, () => { SaveData(); SaveLoop(); });
        void SaveData()
        {
            chData.Stats = statCache;
            chData.Titles = titleCache;
            data.WriteObject(chData);
        }
        void LoadData()
        {
            try
            {
                chData = data.ReadObject<ChallengeData>();
                statCache = chData.Stats;
                titleCache = chData.Titles;
            }
            catch
            {
                chData = new ChallengeData();
            }
        }
        void CheckValidData()
        {
            if (titleCache.Count < typeList.Count)
            {
                foreach (var type in typeList)
                {
                    if (!titleCache.ContainsKey(type))
                        titleCache.Add(type, new LeaderData());
                }
            }
            foreach(var player in statCache)
            {
                foreach(var type in typeList)
                {
                    if (!player.Value.Stats.ContainsKey(type))
                        player.Value.Stats.Add(type, 0);
                }
            }
        }
        class ChallengeData
        {
            public Dictionary<ulong, StatData> Stats = new Dictionary<ulong, StatData>();
            public Dictionary<CTypes, LeaderData> Titles = new Dictionary<CTypes, LeaderData>();
            public double LastUpdate = 0;
        }   
        class StatData
        {
            public string DisplayName = null;
            public Dictionary<CTypes, int> Stats = new Dictionary<CTypes, int>();
        }    
        class LeaderData
        {
            public ulong UserID = 0U;
            public string DisplayName = null;
            public int Count = 0;
        }
        enum CTypes
        {
            Animals, Arrows, Clothes, Headshots, Plants, Healed, Killed, Melee, Revolver, Rockets, Rocks, Swords, Built, Repaired, Explosives, Weapons, Wood, Quests, PVPKill, PVEKill
        }

        #endregion

        #region Lists
        List<CTypes> typeList = new List<CTypes> { CTypes.Animals, CTypes.Arrows, CTypes.Clothes, CTypes.Headshots, CTypes.Plants, CTypes.Healed, CTypes.Killed, CTypes.Melee, CTypes.Revolver, CTypes.Rockets, CTypes.Rocks, CTypes.Swords, CTypes.Built, CTypes.Repaired, CTypes.Explosives, CTypes.Weapons, CTypes.Wood, CTypes.Quests, CTypes.PVEKill, CTypes.PVPKill };
        List<string> meleeShortnames = new List<string> { "bone.club", "hammer.salvaged", "hatchet", "icepick.salvaged", "knife.bone", "mace", "machete", "pickaxe", "rock", "stone.pickaxe", "stonehatchet", "torch" };
        List<string> bladeShortnames = new List<string> { "salvaged.sword", "salvaged.cleaver", "longsword", "axe.salvaged" };
        List<string> plantShortnames = new List<string> { "pumpkin", "cloth", "corn", "mushroom", "seed.hemp", "seed.corn", "seed.pumpkin" };
        #endregion

        #region Messaging
        private string MSG(string key, string id = null) => lang.GetMessage(key, this, id);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"newLeader", "{playername} has topped the leader board for most {ctype}" },
            {"animals", "animal kills" },
            {"arrows", "kills with arrows" },
            {"clothes", "clothes crafted" },
            {"headshots", "headshots" },
            {"plants", "plants gathered" },
            {"healed", "players healed" },
            {"killed", "players killed" },
            {"melee", "melee kills" },
            {"revolver", "revolver kills" },
            {"rockets", "rockets fired" },
            {"rocks", "ore gathered" },
            {"swords", "blade kills" },
            {"built", "structures built" },
            {"repaired", "structures repaired" },
            {"explosives", "explosives thrown" },
            {"weapons", "weapons crafted" },
            {"wood", "wood gathered" },
            {"pvekill", "longest PVE kill"},
            {"pvpkill", "longest PVP kill" },
            {"quests", "quests completed" },
            {"UITitle", "Player Challenges   v{Version}" },
            {"UIDisabled", "The UI has been disabled as there is a error in the config. Please contact a admin" },
            {"dataWipe", "You have wiped all player stats and titles" }
        };
        #endregion
    }
}
