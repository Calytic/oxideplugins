using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Factions", "Absolut", "3.4.0", ResourceId = 1919)]

    class Factions : RustPlugin
    {
        #region Fields

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin ServerRewards;

        [PluginReference]
        Plugin Kits;

        [PluginReference]
        Plugin LustyMap;

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        Plugin ZoneDomes;

        [PluginReference]
        Plugin ActiveCSZone;

        private bool UseFactions;
        private bool TaxBoxFullNotification;
        private int BZPrepTime = 99;
        ushort bZID = 0;
        int GlobalTime = 0;

        static FieldInfo buildingPrivlidges;

        FactionSavedPlayerData playerData;
        private DynamicConfigFile PlayerData;

        PlayerSavedInventories invData;
        private DynamicConfigFile InvData;


        FactionStatistics factionData;
        private DynamicConfigFile FactionData;

        private List<ushort> activeBoxes = new List<ushort>();

        private List<ulong> UnsureWaiting = new List<ulong>();

        private List<ulong> MenuState = new List<ulong>();
        private List<ulong> ButtonState = new List<ulong>();
        private List<ulong> ImmunityList = new List<ulong>();
        private List<ulong> OpenMemberStatus = new List<ulong>();

        private Dictionary<ulong, FactionDesigner> ActiveCreations = new Dictionary<ulong, FactionDesigner>();
        private Dictionary<ulong, FactionDesigner> ActiveEditors = new Dictionary<ulong, FactionDesigner>();
        private Dictionary<ulong, SpawnDesigner> SpawnCreation = new Dictionary<ulong, SpawnDesigner>();
        private Dictionary<ushort, target> FactionInvites = new Dictionary<ushort, target>();
        private Dictionary<ushort, target> FactionKicks = new Dictionary<ushort, target>();
        private Dictionary<ushort, target> LeaderPromotes = new Dictionary<ushort, target>();
        private Dictionary<int, TradeProcessing> TradeAssignments = new Dictionary<int, TradeProcessing>();
        private Dictionary<int, TradeProcessing> TradeRemoval = new Dictionary<int, TradeProcessing>();
        private Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();
        private Dictionary<int, Monuments> MonumentLocations = new Dictionary<int, Monuments>();
        private List<BaseEntity> bzBuildings = new List<BaseEntity>();
        private Dictionary<ulong, PlayerCond> Condition = new Dictionary<ulong, PlayerCond>();
        private Dictionary<ushort, string> BattleZones = new Dictionary<ushort, string>();
        private Dictionary<ulong, BattleZonePlayer> BZPlayers = new Dictionary<ulong, BattleZonePlayer>();
        private Dictionary<ushort, float> BZTimes;
        private Dictionary<ushort, Timer> BZTimers;
        private Dictionary<ulong, Timer> BZKillTimers;
        private List<ulong> SpawnTimers;
        /// Turrets///
        private readonly string turretPrefab = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private uint turretPrefabId;
        Dictionary<ulong, List<AutoTurret>> bzTurrets = new Dictionary<ulong, List<AutoTurret>>();
        FieldInfo bulletDamageField = typeof(AutoTurret).GetField("bulletDamage", (BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo healthField = typeof(BaseCombatEntity).GetField("_health", (BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo maxHealthField = typeof(BaseCombatEntity).GetField("_maxHealth", (BindingFlags.Instance | BindingFlags.NonPublic));
        static string UIMain = "UIMain";
        static string FactionsUIPanel = "FactionsUIPanel";
        static string UIEntry = "UIEntry";
        static string BattleZoneTimer = "BattleZoneTimer";
        static string SpawnTimerUI = "SpawnTimerUI";  

        #endregion

        #region Hooks    
        void Loaded()
        {
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("factions_playerdata");
            InvData = Interface.Oxide.DataFileSystem.GetFile("factions_invdata");
            FactionData = Interface.Oxide.DataFileSystem.GetFile("factions_factiondata");
            buildingPrivlidges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            lang.RegisterMessages(messages, this);
            BZTimers = new Dictionary<ushort, Timer>();
            BZKillTimers = new Dictionary<ulong, Timer>();
            BZTimes = new Dictionary<ushort, float>();
            SpawnTimers = new List<ulong>();
            FindMonuments();
        }

        void Unload()
        {
            activeBoxes.Clear();
            SpawnCreation.Clear();
            FactionKicks.Clear();
            LeaderPromotes.Clear();
            TradeAssignments.Clear();
            TradeRemoval.Clear();
            FactionInvites.Clear();
            SpawnTimers.Clear();
            MonumentLocations.Clear();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                DestroyPlayer(p);
            }
            if (BattleZones.Count > 0)
                EndBZ(bZID, "Unloaded");

            UseFactions = false;
            SaveData();
        }

        void OnServerInitialized()
        {
            LoadVariables();
            LoadData();
            if (configData.Use_TokensReward)
            {
                try
                {
                    EventManager.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"EventManager is missing. Unloading {Name} as it will not work without EventManager.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_ServerRewardsReward)
            {
                try
                {
                    ServerRewards.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"ServerRewards is missing. Unloading {Name} as it will not work without ServerRewards.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            if (configData.Use_EconomicsReward)
            {
                try
                {
                    Economics.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"Economics is missing. Unloading {Name} as it will not work without Economics.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            if (configData.Use_Kits)
            {
                try
                {
                    Kits.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"Kits is missing. Unloading {Name} as it will not work without Kits.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_FactionZones)
            {
                try
                {
                    ZoneManager.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"ZoneManager is missing. Unloading {Name} as it will not work without ZoneManager. Check Option: FactionZones.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_BattleZones)
            {
                try
                {
                    ZoneManager.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning($"ZoneManager is missing. Unloading {Name} as it will not work without ZoneManager. Check Option: BattleZones.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_FactionLeaderByRank)
            {
                if (configData.Use_FactionLeaderByTime == true || configData.Use_FactionLeaderByAdmin == true)
                {
                    PrintWarning($"You have more then (1) Use_FactionLeaderBy setting as true. Unloading {Name} as it will not work properly.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_FactionLeaderByAdmin)
            {
                if (configData.Use_FactionLeaderByRank == true || configData.Use_FactionLeaderByTime == true)
                {
                    PrintWarning($"You have more then (1) Use_FactionLeaderBy setting as true. Unloading {Name} as it will not work properly.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }

            if (configData.Use_FactionLeaderByTime)
            {
                if (configData.Use_FactionLeaderByRank == true || configData.Use_FactionLeaderByAdmin == true)
                {
                    PrintWarning($"You have more then (1) Use_FactionLeaderBy setting as true. Unloading {Name} as it will not work properly.");
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                OnPlayerInit(p);
            }
            GlobalTime = 0;
            InfoLoop();
            timer.Once(configData.Save_Interval * 60, () => SaveLoop());
            timer.Once(1 * 60, () => ChangeGlobalTime());
            timer.Once(30, () => RefreshOpenMemberStatus());
            timer.Once(900 * 60, () => CheckLeaderTime());
            SaveData();
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (gameobject.GetComponent<BaseEntity>() != null)
            {
                if (bZID != 0)
                {
                    var entityowner = gameobject.GetComponent<BaseEntity>().OwnerID;
                    if (BZPlayers.ContainsKey(entityowner))
                        if (BZPlayers[entityowner].died == false && BZPlayers[entityowner].entered == true)
                        {
                            var position = (Vector3)GetZoneLocation(BattleZones[bZID]);
                            var radius = (float)GetZoneRadius(BattleZones[bZID]);
                            var distance = Vector3.Distance(gameobject.transform.position, position);
                            if (distance <= radius)
                            {
                                bzBuildings.Add(gameobject.GetComponent<BaseEntity>());
                            }
                        }
                }
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                if (UseFactions)
                {
                    Bindings(player);
                    RefreshTicker(player);
                    if (player.IsSleeping())
                    {
                        timer.Once(5, () =>
                        {
                            player.EndSleeping();
                            SendMSG(player, lang.GetMessage("FactionsInfo", this));
                            if (FactionMemberCheck(player))
                            {
                                playerData.playerFactions[player.userID].Name = $"{player.displayName}";
                                SendPuts(string.Format(lang.GetMessage("PlayerReturns", this, player.UserIDString), player.displayName));
                                InitPlayerTime(player);
                                AuthorizePlayerOnTurrets(player);
                                if (bZID != 0)
                                    if (!BZPlayers.ContainsKey(player.userID))
                                        BZButton(player, bZID);
                            }
                            if (!FactionMemberCheck(player))
                            {
                                if (!playerData.playerFactions.ContainsKey(player.userID))
                                {
                                    playerData.playerFactions.Add(player.userID, new FactionPlayerData { Name = $"{player.displayName}", trade = Trade.None, level = 1, FactionMemberTime = 0, rank = Rank.Recruit });
                                    Kits?.Call("GiveKit", player, configData.StarterKit);
                                    SendPuts(string.Format(lang.GetMessage("PlayerNew", this, player.UserIDString), player.displayName));
                                }
                                {
                                    SetFaction(player);
                                    return;
                                }
                            }
                        });
                    }
                    else
                    {
                        if (FactionMemberCheck(player))
                        {
                            playerData.playerFactions[player.userID].Name = $"{player.displayName}";
                            InitPlayerTime(player);
                            AuthorizePlayerOnTurrets(player);
                            if (bZID != 0)
                                if (!BZPlayers.ContainsKey(player.userID))
                                    BZButton(player, bZID);
                        }
                        if (!FactionMemberCheck(player))
                        {
                            if (!playerData.playerFactions.ContainsKey(player.userID))
                            {
                                playerData.playerFactions.Add(player.userID, new FactionPlayerData { Name = $"{player.displayName}", trade = Trade.None, level = 1, FactionMemberTime = 0, rank = Rank.Recruit });
                                Kits?.Call("GiveKit", player, configData.StarterKit);
                                SendPuts(string.Format(lang.GetMessage("PlayerNew", this, player.UserIDString), player.displayName));
                            }
                            {
                                SetFaction(player);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (UseFactions)
            {
                if (FactionMemberCheck(player))
                {
                    var faction = GetPlayerFaction(player);
                    RefreshTicker(player);
                    if (OpenMemberStatus.Contains(player.userID))
                        FactionMemberStatus(player);
                    if (!isCSPlayer(player))
                    {
                        if (playerData.playerFactions[player.userID].SavedInventory == true)
                        {
                            RestoreBZPlayer(player);
                        }
                        else if (configData.Use_Kits) GiveFactionKit(player, faction);
                        if (bZID != 0)
                            if (!BZPlayers.ContainsKey(player.userID))
                                BZButton(player, bZID);
                            else if (BZPlayers[player.userID].entered == true)
                            {
                                BZPlayers[player.userID].died = true;
                                BZPlayers[player.userID].oob = false;
                                if (BZKillTimers.ContainsKey(player.userID))
                                {
                                    BZKillTimers[player.userID].Destroy();
                                    BZKillTimers.Remove(player.userID);
                                }
                                CuiHelper.DestroyUi(player, BattleZoneTimer);
                            }
                        SpawnButtons(player, faction);
                        timer.Once(30, () =>
                        {
                            CuiHelper.DestroyUi(player, PanelSpawnButtons);
                        });
                    }
                }
                else OnPlayerInit(player);
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (UseFactions)
                {
                    var attacker = hitInfo.Initiator.ToPlayer();
                    if (!FactionMemberCheck(attacker)) return;
                    if (GetFactionType(attacker) == FactionType.FFA) return;
                    if (configData.Use_Ranks)
                    {
                        int attackerRank = (int)GetPlayerRank(attacker);
                        float dmgMod = (attackerRank * configData.RankBonus + 100) / 100;
                        hitInfo.damageTypes.ScaleAll(dmgMod);
                    }
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        var victim = entity.ToPlayer();
                        if (BZPlayers.ContainsKey(attacker.userID))
                            if (BZPlayers[attacker.userID].died == false && BZPlayers[attacker.userID].entered == true)
                                if (!BZPlayers.ContainsKey(victim.userID))
                                {
                                    SendMSG(attacker, string.Format(lang.GetMessage("VictimNotinBZ", this)));
                                    return;
                                }
                        if (BZPlayers.ContainsKey(victim.userID))
                            if (BZPlayers[victim.userID].died == false && BZPlayers[victim.userID].entered == true)
                                if (!BZPlayers.ContainsKey(attacker.userID))
                                {
                                    SendMSG(attacker, string.Format(lang.GetMessage("AttackerNotinBZ", this)));
                                    return;
                                }
                        if (ImmunityList.Contains(attacker.userID) || ImmunityList.Contains(victim.userID))
                        {
                            hitInfo.damageTypes.ScaleAll(0);
                            try
                            {
                                SendMSG(attacker, string.Format(lang.GetMessage("CurrentlyImmuneAttacker", this)));
                            }
                            catch { }
                            try
                            {
                                SendMSG(victim, string.Format(lang.GetMessage("CurrentlyImmuneVictim", this)));
                            }
                            catch { }
                            return;                
                        }
                        if (!configData.FFDisabled) return;
                        if (GetFactionType(victim) == FactionType.FFA) return;
                        if (EventManager)
                        {
                            object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                            if (isPlaying is bool)
                                if ((bool)isPlaying)
                                    return;
                        }
                        if (ActiveCSZone)
                        {
                            if (isCSPlayer(attacker)) return;
                        }
                        if (victim != attacker)
                        {
                            if ((FactionMemberCheck(attacker)) && (FactionMemberCheck(victim)))
                            {
                                if (configData.Use_RevoltChallenge)
                                {
                                    if ((playerData.playerFactions[attacker.userID].ChallengeStatus) && (playerData.playerFactions[victim.userID].ChallengeStatus)) return;
                                }
                                if (!SameFactionCheck(attacker, victim.userID)) return;
                                {
                                    hitInfo.damageTypes.ScaleAll(configData.FF_DamageScale);
                                    SendMSG(attacker, string.Format(lang.GetMessage("FFs", this, attacker.UserIDString), victim.displayName));
                                }
                            }
                        }
                    }
                    ///Player Structure Check                    
                    else if (entity is BaseEntity && hitInfo.Initiator is BasePlayer)
                    {
                        if (!configData.BuildingProtectionEnabled) return;
                        var OwnerID = entity.OwnerID;
                        if (!FactionMemberCheck(attacker)) return;
                        if (attacker.userID == OwnerID) return;
                        if (!SameFactionCheck(attacker, OwnerID)) return;
                        if (OwnerID != 0)
                        {
                            {
                                if (EventManager)
                                {
                                    object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                                    if (isPlaying is bool)
                                        if ((bool)isPlaying)
                                            return;
                                }

                                if (configData.Use_RevoltChallenge)
                                {
                                    if ((playerData.playerFactions[attacker.userID].ChallengeStatus) && (playerData.playerFactions[OwnerID].ChallengeStatus)) return;
                                }
                                //if (AuthorizedTC(attacker)) return;
                                hitInfo.damageTypes.ScaleAll(0);
                                SendMSG(attacker, string.Format(lang.GetMessage("FFBuildings", this, attacker.UserIDString), playerData.playerFactions[OwnerID].Name));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            if (UseFactions)
            {
                try
                {
                    if (entity is AutoTurret)
                    {
                        var userid = entity.OwnerID;
                        BasePlayer owner = BasePlayer.FindByID(userid);
                        if (InBZ(owner))
                        {
                            if (bzTurrets.ContainsKey(userid))
                                if (bzTurrets[userid].Contains(entity.GetComponent<AutoTurret>()))
                                    bzTurrets[userid].Remove(entity.GetComponent<AutoTurret>());
                            Item item = BuildItem("autoturret", 1);
                            GiveItem(owner, item, "");
                            return;
                        }
                        else if (playerData.playerFactions[userid].factionTurrets.Contains(entity.GetComponent<AutoTurret>()))
                            playerData.playerFactions[userid].factionTurrets.Remove(entity.GetComponent<AutoTurret>());
                        return;
                    }

                    if (entity is StorageContainer)
                    {
                        Vector3 ContPosition = entity.transform.position;
                        foreach (var box in factionData.Boxes)
                        {
                            BasePlayer factionleader = BasePlayer.FindByID(factionData.leader[box.Key]);
                            if (ContPosition.x == box.Value.x && ContPosition.y == box.Value.y && ContPosition.z == box.Value.z)
                            {
                                factionData.Boxes.Remove(box.Key);
                                if (BasePlayer.activePlayerList.Contains(factionleader))
                                    SendMSG(factionleader, lang.GetMessage("TaxBoxDestroyed", this));
                            }

                        }
                        return;
                    }
                    var attacker = hitInfo.Initiator.ToPlayer() as BasePlayer;
                    var victim = entity.ToPlayer();
                    if (InBZ(victim))
                    {
                        if (bzTurrets.ContainsKey(victim.userID))
                        {
                            foreach (var autoturret in bzTurrets[victim.userID])
                                autoturret.DieInstantly();
                            bzTurrets[victim.userID].Clear();
                        }
                        if (BZPlayers.ContainsKey(victim.userID))
                        {
                            BZPlayers[victim.userID].died = true;
                        }
                        if (BZPlayers[victim.userID].owner == true)
                            EndBZ(GetPlayerFaction(victim), "LeaderDeath");
                        else
                            BZAttackerCheck();
                    }
                    if (GetFactionType(attacker) == FactionType.FFA) return;
                    if (GetFactionType(victim) == FactionType.FFA) return;
                    if (!FactionMemberCheck(attacker) || !FactionMemberCheck(victim)) return;
                    var victimfaction = GetPlayerFaction(victim);
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        if (victim.userID != attacker.userID)
                        {
                            var attackerFaction = GetPlayerFaction(attacker);
                            if (!SameFactionCheck(attacker, victim.userID))
                            {
                                if (EventManager)
                                {
                                    object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                                    if (isPlaying is bool)
                                        if ((bool)isPlaying)
                                            return;
                                }
                                if (ActiveCSZone)
                                {
                                    if (isCSPlayer(attacker)) return;
                                }
                                if (configData.Use_FactionKillIncentives)
                                {
                                    factionData.Factions[attackerFaction].Kills += 1;
                                    SaveData();
                                    foreach (BasePlayer p in BasePlayer.activePlayerList)
                                        RefreshTicker(p);
                                    if (factionData.Factions[attackerFaction].Kills >= configData.KillLimit)
                                    {
                                        var fname = factionData.Factions[attackerFaction].Name;
                                        SendPuts(string.Format(lang.GetMessage("KillLimitReached", this), fname));
                                        foreach (var entry in playerData.playerFactions)
                                        {
                                            if (entry.Value.faction == attackerFaction)
                                            {
                                                var reward = "";
                                                if (configData.Use_EconomicsReward)
                                                {
                                                    Economics.Call("DepositS", attacker.userID.ToString(), configData.FactionKillsRewardEconomics);
                                                    reward = $"{configData.FactionKillsRewardEconomics.ToString()} Economics!";
                                                }
                                                if (configData.Use_TokensReward)
                                                {
                                                    EventManager.Call("AddTokens", attacker.userID.ToString(), configData.FactionKillsRewardTokens);
                                                    reward = $"{configData.FactionKillsRewardTokens.ToString()} Tokens!";
                                                }
                                                if (configData.Use_ServerRewardsReward)
                                                {
                                                    ServerRewards?.Call("AddPoints", attacker.userID.ToString(), configData.FactionKillsRewardServerRewards);
                                                    reward = $"{configData.FactionKillsRewardServerRewards.ToString()} Reward Points!";
                                                }
                                                SendPuts(string.Format(lang.GetMessage("KillLimitReward", this), reward));
                                            }
                                        }
                                        SendPuts(string.Format(lang.GetMessage("KillTickerReset", this)));
                                        timer.Once(300, () => ResetTicker());
                                    }
                                }
                                playerData.playerFactions[attacker.userID].Kills += 1;
                                if (configData.Use_Ranks)
                                {
                                    var currentrank = playerData.playerFactions[attacker.userID].rank;
                                    if (currentrank != Rank.SharpShooter)
                                    {
                                        playerData.playerFactions[attacker.userID].Kills += 1;
                                        SaveData();
                                        if (GetPlayerKills(attacker) >= configData.RankRequirement * (int)currentrank)
                                        {

                                            playerData.playerFactions[attacker.userID].rank = currentrank + 1;
                                            playerData.playerFactions[attacker.userID].Kills = 0;
                                            SaveData();
                                            RankAdvancement(attacker);
                                        }
                                    }
                                    CheckLeaderRank(attacker);
                                }
                                SendDeathNote(attacker, victim, GetPlayerFaction(attacker));
                                if (configData.Use_EconomicsReward)
                                {
                                    Economics.Call("DepositS", attacker.userID.ToString(), configData.KillAmountEconomics);
                                    SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.KillAmountEconomics, "Currency"));
                                }
                                if (configData.Use_TokensReward)
                                {
                                    EventManager.Call("AddTokens", attacker.userID.ToString(), configData.KillAmountTokens);
                                    SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.KillAmountTokens, "Tokens"));
                                }
                                if (configData.Use_ServerRewardsReward)
                                {
                                    ServerRewards?.Call("AddPoints", attacker.userID.ToString(), configData.KillAmountServerRewards);
                                    SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.KillAmountServerRewards, "Reward Points"));
                                }
                            }
                            else if (configData.Use_RevoltChallenge)
                            {
                                if (factionData.ActiveChallenges.ContainsKey(victim.userID) && factionData.ActiveChallenges.ContainsKey(attacker.userID))
                                {
                                    if (configData.Use_Ranks)
                                    {
                                        var currentrank = playerData.playerFactions[attacker.userID].rank;
                                        if (currentrank != Rank.SharpShooter)
                                        {
                                            //Puts("Trying Rank");
                                            playerData.playerFactions[attacker.userID].Kills += 1;
                                            SaveData();
                                            if (GetPlayerKills(attacker) >= configData.RankRequirement * (int)currentrank)
                                            {

                                                playerData.playerFactions[attacker.userID].rank = currentrank + 1;
                                                playerData.playerFactions[attacker.userID].Kills = 0;
                                                SaveData();
                                                RankAdvancement(attacker);
                                            }
                                        }
                                    }
                                    if (isleader(attacker))
                                    //Leader wins, Challenger is removed from faction.
                                    {
                                        SendPuts(string.Format(lang.GetMessage("ChallengerLost", this, attacker.UserIDString), victim.displayName));
                                        UnassignPlayerFromFaction(victim.userID);
                                        if (configData.Use_FactionLeaderByRank)
                                        {
                                            playerData.playerFactions[victim.userID].rank = Rank.Recruit;
                                            if (BasePlayer.activePlayerList.Contains(victim)) SendMSG(victim, string.Format(lang.GetMessage("ChallengeLostRank", this)));
                                        }
                                        if (configData.Use_FactionLeaderByTime)
                                        {
                                            playerData.playerFactions[victim.userID].time = GlobalTime;
                                            playerData.playerFactions[victim.userID].FactionMemberTime = 0;
                                            if (BasePlayer.activePlayerList.Contains(victim)) SendMSG(victim, string.Format(lang.GetMessage("ChallengeLostTime", this)));
                                        }
                                    }
                                    else if (isleader(victim))
                                    //Challenger wins, Leader playtime set to 0 or Rank set to Recruit or leader just removed.
                                    {
                                        SendPuts(string.Format(lang.GetMessage("LeaderLost", this, attacker.UserIDString), attacker.displayName));
                                        if (configData.Use_FactionLeaderByRank)
                                        {
                                            playerData.playerFactions[victim.userID].rank = Rank.Recruit;
                                            if (BasePlayer.activePlayerList.Contains(victim)) SendMSG(victim, string.Format(lang.GetMessage("ChallengeLostRank", this)));
                                        }

                                        if (configData.Use_FactionLeaderByTime)
                                        {
                                            playerData.playerFactions[victim.userID].time = GlobalTime;
                                            playerData.playerFactions[victim.userID].FactionMemberTime = 0;
                                            if (BasePlayer.activePlayerList.Contains(victim)) SendMSG(victim, string.Format(lang.GetMessage("ChallengeLostTime", this)));
                                        }

                                        if (configData.Use_FactionLeaderByAdmin)
                                        {
                                            factionData.leader.Remove(victimfaction);
                                            factionData.Factions[victimfaction].tax = 0;
                                            if (factionData.Boxes.ContainsKey(victimfaction)) factionData.Boxes.Remove(victimfaction);
                                            if (BasePlayer.activePlayerList.Contains(victim)) SendMSG(victim, string.Format(lang.GetMessage("ChallengeLostLeader", this)));
                                            BroadcastFaction(victim, string.Format(lang.GetMessage("ChallengeNewLeader", this), attacker.displayName, victim.displayName));
                                            factionData.leader[attackerFaction] = attacker.userID;
                                        }
                                    }
                                    //Remove Challenge Stuff
                                    factionData.ActiveChallenges.Remove(victim.userID);
                                    factionData.ActiveChallenges.Remove(attacker.userID);
                                    FactionDamage(attacker);
                                    FactionDamage(victim);
                                    SaveData();

                                    CheckLeaderTime();
                                    CheckLeaderRank(attacker);
                                }
                            }
                        }
                    }

                }
                catch (Exception)
                {
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyPlayer(player);
            SendPuts(string.Format(lang.GetMessage("PlayerLeft", this, player.UserIDString), player.displayName));
        }

        void DestroyPlayer(BasePlayer player)
        {
            if (player == null) return;
            CuiHelper.DestroyUi(player, SpawnTimerUI);
            player.Command("bind n \"\"");
            player.Command("bind p \"\"");
            DestroyAll(player);
            if (OpenMemberStatus.Contains(player.userID)) OpenMemberStatus.Remove(player.userID);
            if (!FactionMemberCheck(player)) return;
            var faction = GetPlayerFaction(player);
            if (configData.Use_FactionLeaderByTime)
            {
                SaveTimeData(player);
                playerData.playerFactions[player.userID].time = 0;
            }
            if (isleader(player))
            {
                if (activeBoxes.Contains(faction)) activeBoxes.Remove(faction);
            }

        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;
            bool isCreatingFaction = false;
            bool isEditingFaction = false;
            bool isSpawn = false;
            FactionDesigner Creator = new FactionDesigner();
            Faction Faction = new Faction();
            SpawnDesigner Designer = new SpawnDesigner();
            Coords spawn = new Coords();

            if (ActiveEditors.ContainsKey(player.userID))
            {
                isEditingFaction = true;
                Creator = ActiveEditors[player.userID];
                Faction = Creator.Entry;
            }
            else if (ActiveCreations.ContainsKey(player.userID))
            {
                isCreatingFaction = true;
                Creator = ActiveCreations[player.userID];
                Faction = Creator.Entry;
            }
            else if (SpawnCreation.ContainsKey(player.userID))
            {
                isSpawn = true;
                Designer = SpawnCreation[player.userID];
                spawn = Designer.Entry;
            }
            if (isSpawn)
            {
                var args = string.Join(" ", arg.Args);
                if (args.Contains("quit"))
                {
                    ExitSpawnCreation(player, isSpawn);
                    return true;
                }
                if (args.Contains("save spawn"))
                {
                    SaveSpawn(player, isSpawn);
                    return true;
                }
                switch (Designer.partNum)
                {
                    case 0:
                        spawn.Name = string.Join(" ", arg.Args);
                        CreationHelp(player, 10);
                        return true;
                }
            }
            if (isEditingFaction || isCreatingFaction)
            {

                var args = string.Join(" ", arg.Args);
                if (args.Contains("quit"))
                {
                    QuitFactionCreation(player, isCreatingFaction);
                    return true;
                }
                if (args.Contains("save faction"))
                {
                    SaveFaction(player, isCreatingFaction);
                    return true;
                }

                switch (Creator.partNum)
                {
                    case 0:
                        foreach (var faction in factionData.Factions)
                            if (faction.Value.Name.Contains(string.Join(" ", arg.Args)))
                            {
                                SendMSG(player, string.Format(lang.GetMessage("FactionNameExists", this), args));
                                return true;
                            }
                        Faction.Name = string.Join(" ", arg.Args);
                        Creator.partNum++;
                        SendMSG(player, string.Format(lang.GetMessage("CreatorFactionName", this), args));
                        if (isCreatingFaction)
                            CreationHelp(player, 1);
                        else CreationHelp(player, 20);
                        return true;
                    case 1:
                        Faction.LeaderTitle = string.Join(" ", arg.Args);
                        SendMSG(player, string.Format(lang.GetMessage("CreatorLeaderTitle", this), args));
                        Creator.partNum++;
                        if (isCreatingFaction)
                            CreationHelp(player, 2);
                        else CreationHelp(player, 20);
                        return true;
                    case 2:
                        Faction.group = string.Join(" ", arg.Args);
                        SendMSG(player, string.Format(lang.GetMessage("CreatorFactionGroup", this), args));
                        ConsoleSystem.Run.Server.Normal($"group add {Faction.group}");
                        Creator.partNum++;
                        if (isCreatingFaction)
                            CreationHelp(player, 20);
                        return true;
                }
            }
            if (EventManager)
            {
                object isPlaying = EventManager?.Call("isPlaying", new object[] { player });
                if (isPlaying is bool)
                    if ((bool)isPlaying)
                        return null;
            }
            if (ActiveCSZone)
            {
                if (isCSPlayer(player)) return null;
            }
            if (UseFactions)
                if (configData.Use_FactionChatControl)
                {
                    string message = arg.GetString(0, "text");
                    string color = "";
                    if (FactionMemberCheck(player))
                    {
                        color = factionData.Factions[GetPlayerFaction(player)].ChatColor;
                        if (configData.Use_FactionNamesonChat)
                        {
                            color += "[" + factionData.Factions[GetPlayerFaction(player)].Name + "] ";
                        }

                        if (configData.Use_ChatTitles)
                        {
                            if (isleader(player) && isAuth(player))
                            {
                                string formatMsg = color + "[ADMIN][LEADER] " + player.displayName + "</color> : " + message;
                                Broadcast(formatMsg, player.userID.ToString());
                            }
                            else if (isAuth(player))
                            {
                                string formatMsg = color + "[ADMIN] " + player.displayName + "</color> : " + message;
                                Broadcast(formatMsg, player.userID.ToString());
                            }
                            else if (isleader(player))
                            {
                                string formatMsg = color + "[LEADER] " + player.displayName + "</color> : " + message;
                                Broadcast(formatMsg, player.userID.ToString());
                            }
                            else
                            {
                                string formatMsg = color + player.displayName + "</color> : " + message;
                                Broadcast(formatMsg, player.userID.ToString());
                            }
                            return false;
                        }
                        else
                        {
                            string formatMsg = color + player.displayName + "</color> : " + message;
                            Broadcast(formatMsg, player.userID.ToString());
                        }
                        return false;
                    }
                }
            return null;
        }

        void OnLootEntity(BasePlayer player, object lootable)
        {
            if (!configData.Use_Taxes) return;
            BaseEntity container = lootable as BaseEntity;
            if ((player == null) || (container == null) || (!isleader(player))) return;
            {
                var faction = GetPlayerFaction(player);
                var coords = container.transform.localPosition;
                if (!activeBoxes.Contains(faction))
                {
                    if (GetTaxContainer(faction) == container)
                    {
                        SendMSG(player, lang.GetMessage("TaxBox", this));
                        return;
                    }
                    return;
                }
                if (container.OwnerID == player.userID || SameFactionCheck(player, container.OwnerID))
                {
                    if (container.PrefabName == "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab" || container.PrefabName == "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab")
                    {
                        if (!hasTaxBox(faction))
                        {
                            activeBoxes.Remove(faction);
                            factionData.Boxes.Add(faction, new Coords { x = coords.x, y = coords.y, z = coords.z });
                            SendMSG(player, lang.GetMessage("NewTaxBox", this));
                            SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
                            SaveData();
                            return;
                        }
                        else
                        {
                            activeBoxes.Remove(faction);
                            factionData.Boxes.Remove(faction);
                            factionData.Boxes.Add(faction, new Coords { x = coords.x, y = coords.y, z = coords.z });
                            SendMSG(player, lang.GetMessage("NewTaxBox", this));
                            SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
                            SaveData();
                            return;
                        }
                    }
                    SendMSG(player, lang.GetMessage("TaxBoxError", this));
                    return;
                }
                SendMSG(player, lang.GetMessage("TaxBoxOwnerError", this));
                return;
            }
        }

        void OnPlantGather(PlantEntity Plant, Item item, BasePlayer player)
        {
            if (!FactionMemberCheck(player)) return;
            if (configData.Use_Trades)
            {
                if (playerData.playerFactions[player.userID].trade == Trade.Forager)
                {
                    int level = Convert.ToInt32(playerData.playerFactions[player.userID].level);
                    double bonus = Convert.ToInt32(Math.Round(configData.LevelBonus * level));
                    int bonusamount = Convert.ToInt32(Math.Round((bonus * item.amount) / 100));
                    item.amount = item.amount + bonusamount;
                    playerData.playerFactions[player.userID].Gathered += item.amount;
                    SaveData();
                    var currentlevel = playerData.playerFactions[player.userID].level;
                    if (GetPlayerGathered(player) >= (configData.LevelRequirement * currentlevel))
                    {
                        if (currentlevel != configData.MaxLevel)
                        {
                            playerData.playerFactions[player.userID].level += 1;
                            playerData.playerFactions[player.userID].Gathered = 0;
                            SaveData();
                            LevelAdvancement(player);
                        }
                    }
                }
            }
            if (!configData.Use_Taxes) return;
            {
                var faction = GetPlayerFaction(player);
                if (isleader(player)) return;
                if (!factionData.leader.ContainsKey(faction)) return;
                var factionleaderid = factionData.leader[faction];
                BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);
                var taxrate = factionData.Factions[faction].tax;
                StorageContainer TaxContainer = GetTaxContainer(faction);
                if (TaxContainer == null) return;

                int Tax = Convert.ToInt32(Math.Round((item.amount * taxrate) / 100));
                item.amount = item.amount - Tax;

                if (!TaxContainer.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);

                    if (ToAdd != null)
                    {
                        TaxContainer.inventory.AddItem(ToAdd, Tax);
                    }
                }
                else if (BasePlayer.activePlayerList.Contains(factionleader))
                    if (!TaxBoxFullNotification)
                    {
                        SendMSG(player, lang.GetMessage("TaxBoxFull", this));
                        SetBoxFullNotification();
                        return;
                    }
            }
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (!FactionMemberCheck(player)) return;

            if (configData.Use_Trades)
            {
                if (playerData.playerFactions[player.userID].trade == Trade.Forager)
                {
                    int level = Convert.ToInt32(playerData.playerFactions[player.userID].level);
                    double bonus = Convert.ToInt32(Math.Round(configData.LevelBonus * level));
                    int bonusamount = Convert.ToInt32(Math.Round((bonus * item.amount) / 100));
                    item.amount = item.amount + bonusamount;
                    playerData.playerFactions[player.userID].Gathered += item.amount;
                    SaveData();
                    var currentlevel = playerData.playerFactions[player.userID].level;
                    if (GetPlayerGathered(player) >= (configData.LevelRequirement * currentlevel))
                    {
                        if (currentlevel != configData.MaxLevel)
                        {
                            playerData.playerFactions[player.userID].level += 1;
                            playerData.playerFactions[player.userID].Gathered = 0;
                            SaveData();
                            LevelAdvancement(player);
                        }
                    }
                }
            }
            if (!configData.Use_Taxes) return;
            {
                if (isleader(player)) return;
                var faction = GetPlayerFaction(player);
                if (!factionData.leader.ContainsKey(faction)) return;
                var factionleaderid = factionData.leader[faction];
                BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);
                var taxrate = factionData.Factions[faction].tax;
                StorageContainer TaxContainer = GetTaxContainer(faction);
                if (TaxContainer == null) return;

                int Tax = Convert.ToInt32(Math.Round((item.amount * taxrate) / 100));
                item.amount = item.amount - Tax;

                if (!TaxContainer.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);

                    if (ToAdd != null)
                    {
                        TaxContainer.inventory.AddItem(ToAdd, Tax);
                    }
                }
                else if (BasePlayer.activePlayerList.Contains(factionleader))
                    if (!TaxBoxFullNotification)
                    {
                        SendMSG(player, lang.GetMessage("TaxBoxFull", this));
                        SetBoxFullNotification();
                        return;
                    }
            }
        }
        void OnDispenserGather(ResourceDispenser Dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (!FactionMemberCheck(player)) return;

            if (configData.Use_Trades)
            {
                var gatherType = Dispenser.gatherType;
                Trade trade = Trade.None;
                switch (gatherType)
                {
                    case ResourceDispenser.GatherType.Tree:
                        trade = Trade.Lumberjack;
                        break;
                    case ResourceDispenser.GatherType.Ore:
                        trade = Trade.Miner;
                        break;
                    case ResourceDispenser.GatherType.Flesh:
                        trade = Trade.Hunter;
                        break;
                }
                if (playerData.playerFactions[player.userID].trade == trade)
                {
                    double bonus = Convert.ToInt32(Math.Round(configData.LevelBonus * playerData.playerFactions[player.userID].level));
                    int bonusamount = Convert.ToInt32(Math.Round((bonus * item.amount) / 100));
                    //Puts($"Item Amount: {item.amount}");
                    item.amount = item.amount + bonusamount;
                    //Puts($"Item Amount with bonus: {item.amount}");
                    var currentlevel = playerData.playerFactions[player.userID].level;
                    if (currentlevel != configData.MaxLevel)
                    {
                        playerData.playerFactions[player.userID].Gathered += item.amount;
                        SaveData();

                        if (GetPlayerGathered(player) >= (configData.LevelRequirement * currentlevel))
                        {
                            playerData.playerFactions[player.userID].level += 1;
                            playerData.playerFactions[player.userID].Gathered = 0;
                            SaveData();
                            LevelAdvancement(player);
                        }
                    }
                }
            }
            //Puts($"{player.displayName} amount of {gatherType} is {item.amount}");
            //Puts($"Trade: {Enum.GetName(typeof(Trade), playerData.playerFactions[player.userID].trade)}");
            if (configData.Use_Taxes)
                {
                    var faction = GetPlayerFaction(player);
                    if (!factionData.leader.ContainsKey(faction)) return;
                    if (isleader(player)) return;
                    var factionleaderid = factionData.leader[faction];
                    BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);
                    var taxrate = factionData.Factions[faction].tax;
                    StorageContainer TaxContainer = GetTaxContainer(faction);
                    if (TaxContainer == null) return;
                    int Tax = Convert.ToInt32(Math.Round((item.amount * taxrate) / 100));


                    if (!TaxContainer.inventory.IsFull())
                    {
                        ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);

                        if (ToAdd != null)
                        {
                            TaxContainer.inventory.AddItem(ToAdd, Tax);
                            item.amount = item.amount - Tax;
                            //Puts($"tax amount {Tax}");
                            return;

                        }
                    }
                    else if (BasePlayer.activePlayerList.Contains(factionleader))
                        if (!TaxBoxFullNotification)
                        {
                            SendMSG(player, lang.GetMessage("TaxBoxFull", this));
                            SetBoxFullNotification();
                            return;
                        }
                }
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (!configData.Use_Trades) return null;
            if (playerData.playerFactions[crafter.userID].trade == Trade.Crafter)
            {
                var craftingTime = task.blueprint.time;
                int level = Convert.ToInt32(playerData.playerFactions[crafter.userID].level);
                double bonus = Convert.ToInt32(Math.Round(configData.LevelBonus * level));
                int CraftingReduction = Convert.ToInt32(Math.Round(craftingTime * bonus) / 100);
                craftingTime -= CraftingReduction;
                task.blueprint.time = craftingTime;
                //Puts($"{crafter.displayName} crafted {task.blueprint.targetItem} is done in {task.blueprint.time}");
                return null;
            }
            else
            {
                //Puts($"{crafter.displayName} crafted {task.blueprint.targetItem} is done in {task.blueprint.time}");
                return null;
            }
        }

        object OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!configData.Use_Trades) return null;
            if (playerData.playerFactions[task.owner.userID].trade != Trade.Crafter) return null;
            var currentlevel = playerData.playerFactions[task.owner.userID].level;
            if (currentlevel != configData.MaxLevel)
            {
                playerData.playerFactions[task.owner.userID].Crafted += 1;
                SaveData();
                if (GetPlayerCrafted(task.owner) >= ((configData.LevelRequirement * currentlevel) / 100))
                {

                    playerData.playerFactions[task.owner.userID].level += 1;
                    playerData.playerFactions[task.owner.userID].Crafted = 0;
                    SaveData();
                    LevelAdvancement(task.owner);
                }
            }
            return null;

        }

        //AutoTurrets
        void OnEntitySpawned(BaseNetworkable entity)
        {
            turretPrefabId = StringPool.Get(turretPrefab);
            if (UseFactions)
            {
                if (entity == null) return;
                if (entity.prefabID == turretPrefabId)
                {
                    var userid = entity.GetComponent<AutoTurret>().OwnerID;
                    BasePlayer owner = BasePlayer.FindByID(userid);
                    if (!FactionMemberCheck(owner)) return;
                    if (GetFactionType(owner) == FactionType.FFA) return;
                    var faction = GetPlayerFaction(owner);
                    if (BZPlayers.ContainsKey(userid))
                    {
                        if (BZPlayers[userid].died == false && BZPlayers[userid].entered == true && BZPlayers[userid].owner == true)
                        {
                            if (!bzTurrets.ContainsKey(userid))
                                bzTurrets.Add(userid, new List<AutoTurret>());
                            bzTurrets[userid].Add(entity.GetComponent<AutoTurret>());
                            AssignTurretAuth(faction, entity.GetComponent<AutoTurret>());
                            ConfigureTurret(entity.GetComponent<AutoTurret>());
                        }
                    }
                    else if (configData.Use_AutoAuthorization)
                    {
                        if (factionData.leader.ContainsKey(GetPlayerFaction(owner)))
                            if (factionData.leader.ContainsValue(userid))
                        {
                            playerData.playerFactions[userid].factionTurrets.Add(entity.GetComponent<AutoTurret>());
                            AssignTurretAuth(faction, entity.GetComponent<AutoTurret>());
                        }
                    }
                }
            }
        }

        object CanUseDoor(BasePlayer player, BaseLock door)
        {
            if (!FactionMemberCheck(player)) return null;
            if (GetFactionType(player) == FactionType.FFA) return null;
            var parent = door.parentEntity.Get(true);
            var prefab = parent.LookupPrefab();
            if (parent.IsOpen()) return true;
            if (bZID != 0)
            {
                if (BZPlayers.ContainsKey(player.userID))
                    if (BZPlayers[player.userID].died == false && BZPlayers[player.userID].entered == true)
                        if (BZPlayers[player.userID].faction == GetPlayerFaction(player))
                            return true;
            }
            if (configData.Use_AutoAuthorization)
            {
                if (factionData.leader.ContainsKey(GetPlayerFaction(player)))
                    if (parent.OwnerID == factionData.leader[GetPlayerFaction(player)])
                        return true;
            }
            return null;
        }

        void AssignTurretAuth(ushort faction, AutoTurret turret)
        {
            foreach (var p in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
            {
                turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID() { userid = p.Key, username = p.Value.Name });
            }
        }

        void AuthorizePlayerOnTurrets(BasePlayer player)
        {
            var faction = GetPlayerFaction(player);
            if (configData.Use_BattleZones)
                if (BattleZones.ContainsKey(faction))
                    foreach (var entry in bzTurrets[(ulong)GetLeader(faction)])
                    {
                        var turret = entry as AutoTurret;
                        turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID() { userid = player.userID, username = player.displayName });
                    }
            if (configData.Use_AutoAuthorization)
                if (factionData.leader.ContainsKey(faction))
                    if (playerData.playerFactions[(ulong)GetLeader(faction)].factionTurrets != null)
                        foreach (var entry in playerData.playerFactions[(ulong)GetLeader(faction)].factionTurrets)
                        {
                            var turret = entry as AutoTurret;
                            turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID() { userid = player.userID, username = player.displayName });
                        }
        }

        void ConfigureTurret(AutoTurret turret)
        {
            turret.inventory.AddItem(ItemManager.FindItemDefinition(815896488), 256);
            turret.startHealth = 60f;
            healthField.SetValue(turret, 60f);
            maxHealthField.SetValue(turret, 60f);
            bulletDamageField.SetValue(turret, 5f);
            turret.sightRange = 15f;
            turret.InitiateStartup();
        }    

        #endregion

        #region Functions

        private bool SameFactionCheck(BasePlayer self, ulong target)
        {
            if (GetPlayerFaction(self) == playerData.playerFactions[target].faction) return true;
            else return false;
        }

        private bool FactionMemberCheck(BasePlayer player)
        {
            if (!playerData.playerFactions.ContainsKey(player.userID)) return false;
            var factionID = playerData.playerFactions[player.userID].faction;
            if (factionID == default(ushort)) return false;
            foreach (var entry in factionData.Factions)
            {
                if (entry.Key == factionID)
                {
                    return true;
                }
            }
            return false;
        }

        private ushort GetPlayerFaction(BasePlayer player)
        {
            return playerData.playerFactions[player.userID].faction;
        }

        private FactionType GetFactionType(BasePlayer player)
        {
            var faction = GetPlayerFaction(player);
            return factionData.Factions[faction].type;
        }

        private Trade GetPlayerTrade(BasePlayer player)
        {
            if (FactionMemberCheck(player)) return playerData.playerFactions[player.userID].trade;
            return Trade.None;
        }

        private Rank GetPlayerRank(BasePlayer player)
        {
            if (FactionMemberCheck(player)) return playerData.playerFactions[player.userID].rank;
            return Rank.None;
        }

        private int GetPlayerKills(BasePlayer player)
        {
            if (!FactionMemberCheck(player)) return 0;
            else return playerData.playerFactions[player.userID].Kills;
        }

        private int GetFactionKills(ushort faction)
        {
            if (!factionData.Factions.ContainsKey(faction)) return 0;
            return factionData.Factions[faction].Kills;
        }

        private bool CheckForActiveEnemies(ushort faction)
        {
            var p = 0;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (GetFactionType(player) != FactionType.FFA && GetPlayerFaction(player) != faction)
                {
                    p++;
                    if (p >= configData.RequiredBZParticipants) break;
                }           
            }
            if (p >= configData.RequiredBZParticipants) return true;
            else return false;
        }

        private int GetPlayerGathered(BasePlayer player)
        {
            if (FactionMemberCheck(player)) return playerData.playerFactions[player.userID].Gathered;
            return 0;
        }

        private int GetPlayerCrafted(BasePlayer player)
        {
            if (FactionMemberCheck(player)) return playerData.playerFactions[player.userID].Crafted;
            return 0;
        }

        private int GetPlayerLevel(BasePlayer player)
        {
            if (FactionMemberCheck(player)) return playerData.playerFactions[player.userID].level;
            return 0;
        }

        public void Broadcast(string message, string userid = "0") => PrintToChat(message);

        private void BroadcastFaction(BasePlayer source, string message, ushort faction = 0)
        {
            if (faction == 0)
                faction = GetPlayerFaction(source);
            string color = factionData.Factions[faction].ChatColor;
            string name = "";
            if (configData.Use_FactionNamesonChat)
            {
                name = $"[{factionData.Factions[faction].Name}]";
            }
            foreach (var entry in playerData.playerFactions)
            {
                BasePlayer player = BasePlayer.FindByID(entry.Key);

                if (entry.Value.faction == faction)
                    SendReply(player, color + name + lang.GetMessage("inFactionChat", this) + "</color> " + message);
            }
        }

        private void SendMSG(BasePlayer player, string msg, string keyword = "title")
        {
            if (keyword == "title") keyword = lang.GetMessage("title", this, player.UserIDString);
            SendReply(player, configData.MSG_MainColor + keyword + "</color>" + configData.MSG_Color + msg + "</color>");
        }

        private void SendPuts(string msg, string keyword = "title")
        {
            if (keyword == "title") keyword = lang.GetMessage("title", this);
            PrintToChat(configData.MSG_MainColor + keyword + "</color>" + configData.MSG_Color + msg + "</color>");
        }

        private void SendDeathNote(BasePlayer player, BasePlayer victim, ushort faction)
        {
            string colorAttacker = "";
            string colorVictim = "";
            string prefixAttacker = "";
            string prefixVictim = "";

            colorAttacker = factionData.Factions[faction].ChatColor;
            prefixAttacker = "[" + factionData.Factions[faction].Name + "] ";
            var victimfaction = (GetPlayerFaction(victim));
            colorVictim = factionData.Factions[victimfaction].ChatColor;
            prefixVictim = "[" + factionData.Factions[victimfaction].Name + "] ";

            if (configData.BroadcastDeath)
            {
                string formatMsg = colorAttacker + prefixAttacker + player.displayName + "</color>" + lang.GetMessage("DeathMessage", this, player.UserIDString) + colorVictim + prefixVictim + victim.displayName + "</color>";
                Broadcast(formatMsg);
            }
        }
        private string CountPlayers(ushort faction)
        {
            int i = 0;
            foreach (var entry in playerData.playerFactions)
            {
                if (entry.Value.faction == faction)
                    i++;
            }
            return i.ToString();
        }

        private object RaycastAll<T>(Ray ray) where T : BaseEntity
        {
            var hits = Physics.RaycastAll(ray);
            GamePhysics.Sort(hits);
            var distance = 100f;
            object target = false;
            foreach (var hit in hits)
            {
                var ent = hit.GetEntity();
                if (ent is T && hit.distance < distance)
                {
                    target = ent;
                    break;
                }
            }

            return target;
        }

        static bool AuthorizedTC(BasePlayer player)
        {
            List<BuildingPrivlidge> playerpriv = buildingPrivlidges.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0)
            {
                return false;
            }
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                bool foundplayer = false;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                {
                    if (pni.userid == player.userID)
                        foundplayer = true;
                }
                if (!foundplayer)
                {
                    return false;
                }
            }
            return true;
        }

        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalHours;

        private void SavePlayerFactionTime()
        {
            if (!configData.Use_FactionLeaderByTime) return;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    SaveTimeData(player);
                }
            }
            SaveData();
            timer.Once(300, () => SavePlayerFactionTime());
        }

        private void SaveTimeData(BasePlayer player)
        {
            playerData.playerFactions[player.userID].FactionMemberTime += (GlobalTime - playerData.playerFactions[player.userID].time);
            playerData.playerFactions[player.userID].time = GlobalTime;

        }

        //private static long GrabCurrentTimestamp()
        //{
        //    long timestamp = 0;
        //    long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
        //    ticks /= 10000000;
        //    timestamp = ticks;

        //    return timestamp;
        //}

        private void InitPlayerTime(BasePlayer player)
        {
            if (!configData.Use_FactionLeaderByTime) return;
            if (factionData.Factions[GetPlayerFaction(player)].type == FactionType.FFA) return;
            playerData.playerFactions[player.userID].time = GlobalTime;
        }

        private StorageContainer GetTaxContainer(ushort faction)
        {
            foreach (var c in factionData.Boxes)
                if (c.Key == faction)
                {
                    var x = c.Value.x;
                    var y = c.Value.y;
                    var z = c.Value.z;

                    foreach (StorageContainer Cont in StorageContainer.FindObjectsOfType<StorageContainer>())
                    {
                        Vector3 ContPosition = Cont.transform.position;
                        if (ContPosition.x == x && ContPosition.y == y && ContPosition.z == z)
                        {
                            var factionbox = Cont;
                            //Puts($"{factionbox}"); -- Testing
                            return factionbox;
                        }
                    }
                }
            return null;
        }

        string MergeParams(string[] Params, int Start)
        {
            var Merged = new StringBuilder();
            for (int i = Start; i < Params.Length; i++)
            {
                if (i > Start)
                    Merged.Append(" ");
                Merged.Append(Params[i]);
            }

            return Merged.ToString();
        }

        static void MovePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);

            player.CancelInvoke("InventoryUpdate");
            player.inventory.crafting.CancelAll(true);

            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination, null, null, null, null);
            player.TransformChanged();
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();

            player.SendNetworkUpdateImmediate(false);
            player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            player.SendFullSnapshot();
        }

        private void SetBoxFullNotification()
        {
            TaxBoxFullNotification = true;
            timer.Once(5 * 60, () => TaxBoxFullNotification = false);
        }

        object GetTax(ushort faction)
        {
            if (factionData.Factions.ContainsKey(faction))
                return factionData.Factions[faction].tax;
            else return "0";
        }
        object GetLeaderName(ushort faction)
        {
            if (factionData.leader.ContainsKey(faction))
                if (playerData.playerFactions[factionData.leader[faction]].Name == "") return "";
                else return playerData.playerFactions[factionData.leader[faction]].Name;
            else return "";
        }

        object GetLeader(ushort faction)
        {
            if (factionData.leader.ContainsKey(faction)) return factionData.leader[faction];
            else return null;

        }

        private bool isleader(BasePlayer player)
        {
            if (factionData.leader.ContainsValue(player.userID)) return true;
            else return false;
        }

        private bool hasTaxBox(ushort faction)
        {
            if (factionData.Boxes.ContainsKey(faction)) return true;
            else return false;
        }

        void Bindings(BasePlayer player)
        {
            player.Command("bind n \"UI_OpenFactions\"");
            player.Command("bind p \"UI_OpenMemberStatus\"");
        }


        [ChatCommand("buildings")]
        private void cmdtest(BasePlayer player)
        {
            CountFactionBuildings();
            CountTotalPlayerBuildings();
            CountFactionPlayerBuildings();
        }

        private void CountTotalPlayerBuildings()
        {
            foreach (var p in playerData.playerFactions)
            {
                BasePlayer player = BasePlayer.FindByID(p.Key);
                int Count = 0;
                //List<BuildingBlock> PlayerBlocks = new List<BuildingBlock>();
                ////Puts($"Checking {tc.transform.position} owned by {l.Value} ");
                //foreach (BuildingBlock entry in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
                //{
                List<BuildingBlock> AllBlocks = new List<BuildingBlock>();
                //Puts($"Checking {tc.transform.position} owned by {l.Value} ");
                Vis.Entities<BuildingBlock>(new Vector3(0, 0, 0), 999999f, AllBlocks);
                foreach (var entry in AllBlocks)
                {
                    if (entry is BuildingBlock)
                        if (entry.OwnerID == p.Key)
                        {
                            Count++;
                        }
                }
                p.Value.TotalBuildings = Count;
            }
            SaveData();
        }

        private void CountFactionPlayerBuildings()
        {
            foreach (var p in playerData.playerFactions)
            {
                int Count = 0;
                var pfaction = p.Value.faction;
                var leader = factionData.leader[pfaction];
                foreach (BuildingPrivlidge tc in Resources.FindObjectsOfTypeAll<BuildingPrivlidge>())
                    foreach (ProtoBuf.PlayerNameID pni in tc.authorizedPlayers)
                        if (pni.userid == leader)
                        {
                            List<BuildingBlock> PlayerBlocks = new List<BuildingBlock>();
                            //Puts($"Checking {tc.transform.position} owned by {l.Value} "); -- Testing
                            Vis.Entities<BuildingBlock>(tc.transform.position, 100f, PlayerBlocks);
                            foreach (var entry in PlayerBlocks)
                            {
                                if (entry is BuildingBlock)
                                {
                                    if (!playerData.playerFactions.ContainsKey(entry.OwnerID)) continue;
                                    if (entry.OwnerID == p.Key)
                                        Count++;
                                }
                            }
                        }
                playerData.playerFactions[p.Key].FactionBuildings = Count;
            }
            SaveData();
        }

        private void CountFactionBuildings()
        {
            foreach (var leader in factionData.leader)
            {
                var leaderfaction = leader.Key;
                var privileges = new List<Dictionary<string, object>>();
                int Count = 0;
                foreach (BuildingPrivlidge tc in Resources.FindObjectsOfTypeAll<BuildingPrivlidge>())
                    foreach (ProtoBuf.PlayerNameID pni in tc.authorizedPlayers)
                        if (pni.userid == leader.Value)
                        {
                            List<BuildingBlock> FactionBlocks = new List<BuildingBlock>();
                            //Puts($"Checking {tc.transform.position} owned by {l.Value} "); --Testing
                            Vis.Entities<BuildingBlock>(tc.transform.position, 100f, FactionBlocks);
                            foreach (var entry in FactionBlocks)
                            {
                                if (entry is BuildingBlock)
                                {
                                    if (!playerData.playerFactions.ContainsKey(entry.OwnerID)) continue;
                                    if (playerData.playerFactions[entry.OwnerID].faction == leaderfaction)
                                        Count++;
                                }
                            }


                        }
                factionData.Buildings[leaderfaction] = Count;
            }
            SaveData();
        }

        void SpawnTimer(BasePlayer player, int time = 0)
        {
            int interval = 60;
            if (time == 0)
            {
                SpawnTimers.Add(player.userID);
                time = ((configData.SpawnCooldown + 1) * 60);
                timer.Once(1, () => SpawnTimer(player, time));
                return;
            }
            if (BasePlayer.activePlayerList.Contains(player))
            {
                time = time - interval;
                CuiHelper.DestroyUi(player, SpawnTimerUI);
                if (time != 0)
                {
                    var element = UI.CreateElementContainer("SpawnTimerUI", "0.0 0.0 0.0 0.0", "0.88 0.9", "0.98 0.99", false);
                    TimeSpan dateDifference = TimeSpan.FromSeconds(time);
                    string clock = string.Format("{0:D2}", dateDifference.Minutes);
                    UI.CreateLabel(ref element, "SpawnTimerUI", "", $"Spawn Timer\n{clock}", 20, "0 0", "1 1");
                    CuiHelper.AddUi(player, element);
                    timer.Once(interval, () => SpawnTimer(player, time));
                }
            }
            if (time == 0)
            {
                SpawnTimers.Remove(player.userID);
                if (BasePlayer.activePlayerList.Contains(player))
                CuiHelper.DestroyUi(player, SpawnTimerUI);
            }
        }

        private void InitializeBZPlayer(BasePlayer player, ushort BZFaction, string ZoneID)
        {
            SaveInventory(player);
            SaveHealth(player);
            player.health = 100;
            player.metabolism.calories.value = 500;
            player.metabolism.hydration.value = 250;
            player.metabolism.bleeding.value = 0;
            player.metabolism.SendChangesToClient();
            if (ImmunityList.Contains(player.userID)) ImmunityList.Remove(player.userID);
            BZPlayers.Add(player.userID, new BattleZonePlayer { faction = BZFaction, bz = ZoneID, died = false, entered = false, oob = false });
        }

        private IEnumerable<PlayerInv> GetItems(ItemContainer container, string containerName)
        {
            return container.itemList.Select(item => new PlayerInv
            {
                itemid = item.info.itemid,
                container = containerName,
                amount = item.amount,
                ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                skin = item.skin,
                condition = item.condition,
                InvContents = item.contents?.itemList.Select(item1 => new PlayerInv
                {
                    itemid = item1.info.itemid,
                    amount = item1.amount,
                    condition = item1.condition
                }).ToArray()
            });
        }

        public void SaveHealth(BasePlayer player)
        {
            if (Condition.ContainsKey(player.userID))
                Condition.Remove(player.userID);
            Condition.Add(player.userID, new PlayerCond { health = player.health, calories = player.metabolism.calories.value, hydration = player.metabolism.hydration.value });
        }

        public void SaveInventory(BasePlayer player)
        {
            if (playerData.playerFactions[player.userID].SavedInventory)
                invData.PlayerInventory.Remove(player.userID);
            invData.PlayerInventory.Add(player.userID, new List<PlayerInv>());
            invData.PlayerInventory[player.userID].AddRange(GetItems(player.inventory.containerWear, "wear"));
            invData.PlayerInventory[player.userID].AddRange(GetItems(player.inventory.containerMain, "main"));
            invData.PlayerInventory[player.userID].AddRange(GetItems(player.inventory.containerBelt, "belt"));
            playerData.playerFactions[player.userID].SavedInventory = true;
            SaveData();
        }

        public void RestoreBZPlayer(BasePlayer player)
        {
                RestoreInventory(player);
                RestorePlayerHealth(player);
        }

        public void RestoreInventory(BasePlayer player)
        {
            var i = 0;
            player.inventory.Strip();
            foreach (var inv in invData.PlayerInventory[player.userID])
            {
                    i++;
                    var item = ItemManager.CreateByItemID(inv.itemid, inv.amount, inv.skin);
                    item.condition = inv.condition;
                    var weapon = item.GetHeldEntity() as BaseProjectile;
                    if (weapon != null) weapon.primaryMagazine.contents = inv.ammo;
                    player.inventory.GiveItem(item, inv.container == "belt" ? player.inventory.containerBelt : inv.container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
                    if (inv.InvContents == null) continue;
                    foreach (var ckitem in inv.InvContents)
                    {
                        var item1 = ItemManager.CreateByItemID(ckitem.itemid, ckitem.amount);
                        if (item1 == null) continue;
                        item1.condition = ckitem.condition;
                        item1.MoveToContainer(item.contents);
                    }
            }
            playerData.playerFactions[player.userID].SavedInventory = false;
            if (invData.PreviousInventory.ContainsKey(player.userID))
                invData.PreviousInventory.Remove(player.userID);
            invData.PreviousInventory.Add(player.userID, new List<PlayerInv>());
            invData.PreviousInventory[player.userID] = invData.PlayerInventory[player.userID];
            invData.PlayerInventory.Remove(player.userID);
            SaveData();
        }

        private void GiveBZItems(BasePlayer player, string role)
        {
            var gear = new List<Gear>();
                gear = BZItems[role];
            if (gear != null)
                foreach (var entry in gear)

                    GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), "");
        }

        private Item BuildItem(string shortname, int amount = 1, int skin = 0)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, amount, skin);
                if (item != null)
                    return item;
            }
            Puts("Error making item: " + shortname);
            return null;
        }

        public void GiveItem(BasePlayer player, Item item, string container)
        {
            if (item == null) return;
            ItemContainer cont;
            switch (container)
            {
                case "wear":
                    cont = player.inventory.containerWear;
                    break;
                case "belt":
                    cont = player.inventory.containerBelt;
                    break;
                default:
                    cont = player.inventory.containerMain;
                    break;
            }
            player.inventory.GiveItem(item, cont);
        }

        void RestorePlayerHealth(BasePlayer player)
        {
            if (Condition.ContainsKey(player.userID))
            {
                player.health = Condition[player.userID].health;
                player.metabolism.calories.value = Condition[player.userID].calories;
                player.metabolism.hydration.value = Condition[player.userID].hydration;
                player.metabolism.bleeding.value = 0;
                player.metabolism.SendChangesToClient();
            }
        }

        void ChangeGlobalTime()
        {
            if (!configData.Use_FactionLeaderByTime) return;
            GlobalTime++;
            timer.Once(1 * 60, () => ChangeGlobalTime());
        }

        void RefreshOpenMemberStatus()
        {
            foreach (var entry in OpenMemberStatus)
            {
                try { FactionMemberStatus(BasePlayer.FindByID(entry)); }
                catch { OpenMemberStatus.Remove(entry); }
            }
            timer.Once(30, () => RefreshOpenMemberStatus());
        }

        #endregion

        #region Zones
        private object GetZoneLocation(string zoneid) => ZoneManager?.Call("GetZoneLocation", zoneid);
        private object GetZoneRadius(string zoneid) => ZoneManager?.Call("GetZoneRadius", zoneid);

        void DestroyZoneEntities()
        {
            foreach (var entity in bzBuildings)
                entity.KillMessage();
            bzBuildings.Clear();
        }


        void OnEnterZone(string zoneID, BasePlayer player)
        {
            if (configData.Use_BattleZones)
                if (BattleZones.ContainsValue(zoneID))
                {
                    if (BZPlayers.ContainsKey(player.userID))
                        if (BZPlayers[player.userID].bz == zoneID)
                            if (BZPlayers[player.userID].died == false)
                            {
                                if (BZPlayers[player.userID].entered == false)
                                {
                                    if (BZPlayers[player.userID].owner)
                                        GiveBZItems(player, "owner");
                                    else if (!BZPlayers[player.userID].owner && (GetPlayerFaction(player) == BZPlayers[player.userID].faction))
                                        GiveBZItems(player, "defender");
                                    else if (GetPlayerFaction(player) != BZPlayers[player.userID].faction)
                                        GiveBZItems(player, "attacker");
                                    BZPlayers[player.userID].entered = true;
                                }
                                BZPlayers[player.userID].oob = false;
                                if (BZKillTimers.ContainsKey(player.userID))
                                {
                                    BZKillTimers[player.userID].Destroy();
                                    BZKillTimers.Remove(player.userID);
                                }
                                return;
                            }
                    SendMSG(player, lang.GetMessage("NotAllowed", this));
                    Vector3 newPos = CalculateOutsidePos(player, zoneID);
                    MovePlayerPosition(player, newPos);
                    return;
                }
            if (configData.Use_FactionSafeZones)
            {
                if (isAuth(player)) return;
                if (GetPlayerFaction(player).ToString() == zoneID) return;
                else
                {
                    SendMSG(player, lang.GetMessage("NotAllowedFZone", this));
                    Vector3 newPos = CalculateOutsidePos(player, zoneID);
                    MovePlayerPosition(player, newPos);
                }
            }
        }

        void OnExitZone(string zoneID, BasePlayer player)
        {
            if (configData.Use_BattleZones)
                if (bZID != 0)
                {
                    if (BZPlayers.ContainsKey(player.userID))
                    {
                        var faction = BZPlayers[player.userID].faction;
                        BZPlayers[player.userID].oob = true;
                        if (!BZKillTimers.ContainsKey(player.userID))
                        {
                            SendMSG(player, lang.GetMessage("OOBWarning", this));
                            int time = 10;
                            BZKillTimers.Add(player.userID, timer.Repeat(1, time, () =>
                            {
                                if (BZKillTimers.ContainsKey(player.userID))
                                {
                                    if (BZPlayers[player.userID].oob)
                                    {
                                        time--;
                                        SendMSG(player, string.Format(lang.GetMessage("OOBRepeater", this), time));
                                        if (time == 0)
                                        {
                                            BZPlayers[player.userID].died = true;
                                            if (BZPlayers[player.userID].owner == true)
                                                EndBZ(GetPlayerFaction(player), "LeaderDeath");
                                            Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", (player.transform.position));
                                            player.Hurt(200f, Rust.DamageType.Explosion, null, true);
                                            SendPuts(string.Format(lang.GetMessage("OOBDeath", this), player.displayName, factionData.Factions[faction].Name));
                                        }
                                    }
                                    else BZKillTimers.Remove(player.userID);
                                }
                            }));
                        }
                    }
                    else
                    {
                        SendMSG(player, lang.GetMessage("NotAllowed", this));
                        Vector3 newPos = CalculateOutsidePos(player, zoneID);
                        MovePlayerPosition(player, newPos);
                    }
                }
        }

        private void createZone(BasePlayer player, ushort ID, string type)
        {
            if (type == "faction")
            {
                if (!factionData.Factions[ID].FactionZone)
                {
                    var factionname = factionData.Factions[ID].Name;
                    string msg = $"{string.Format(lang.GetMessage("EnterFactionZone", this), factionname)}";
                    Vector3 pos = player.transform.localPosition;
                    List<string> build = new List<string>();
                    build.Add("enter_message");
                    build.Add(msg.ToString());
                    build.Add("radius");
                    build.Add(configData.ZoneRadius.ToString());
                    string[] zoneArgs = build.ToArray();
                    ZoneManager?.Call("CreateOrUpdateZone", ID.ToString(), zoneArgs, pos);
                    factionData.Factions[ID].FactionZone = true;
                    ShadeZone(player, ID.ToString());
                    AddMapMarker(pos.x, pos.z, $"{factionname} {lang.GetMessage("FactionZone", this)}");
                    ZoneManagement(player, "admin");
                    return;
                }
                SendMSG(player, lang.GetMessage("CurrentFZ", this));
                return;
            }
            else if (type == "battle")
            {
                string zone = ID.ToString();
                var faction = GetPlayerFaction(player);
                if (!BattleZones.ContainsKey(faction))
                {
                    var factionname = factionData.Factions[faction].Name;
                    string msg = $"{string.Format(lang.GetMessage("EnterBattleZone", this), factionname)}";
                    Vector3 pos = player.transform.localPosition;
                    if (!BuildingCheck(player, pos, configData.ZoneRadius)) return;
                    InitializeBZPlayer(player, faction, zone);
                    BZPlayers[player.userID].owner = true;
                    List<string> build = new List<string>();
                    build.Add("enter_message");
                    build.Add(msg.ToString());
                    build.Add("radius");
                    build.Add(configData.ZoneRadius.ToString());
                    build.Add("nocorpse ");
                    build.Add("true");
                    build.Add("nogather");
                    build.Add("true");
                    build.Add("noplayerloot");
                    build.Add("true");
                    build.Add("nosuicide");
                    build.Add("true");
                    build.Add("nopickup");
                    build.Add("true");
                    build.Add("nocollect");
                    build.Add("true");
                    build.Add("nodrop");
                    build.Add("true");
                    build.Add("autolights");
                    build.Add("true");
                    string[] zoneArgs = build.ToArray();
                    ZoneManager?.Call("CreateOrUpdateZone", zone, zoneArgs, pos);
                    ShadeZone(player, zone);
                    AddMapMarker(pos.x, pos.z, $"{factionname} {lang.GetMessage("BattleZone", this)}");
                    BattleZones.Add(faction, zone);
                    BZPrepTime = 99;
                    BZPrep(faction, zone);
                    DestroyFactionMenu(player);
                    return;
                }
                else SendMSG(player, lang.GetMessage("CurrentBZ", this));
            }
        }

        private bool BuildingCheck(BasePlayer player, Vector3 pos, float radius)
        {
            foreach (var entry in MonumentLocations)
            {
                var distance = Vector3.Distance(pos, entry.Value.position);
                if (distance < entry.Value.radius + configData.ZoneRadius)
                {
                    SendMSG(player, lang.GetMessage("MonumentFailed", this));
                    return false; 
                }
            }
            List<BuildingBlock> playerbuildings = new List<BuildingBlock>();
            Vis.Entities(pos, radius, playerbuildings);
            if (playerbuildings.Count > 4)
            {
                SendMSG(player, lang.GetMessage("PlayerStructureFailed", this));
                return false;
            }
            else return true;
        }

        private void BZPrep(ushort ID, string zone)
        {
            if (!BattleZones.ContainsKey(ID)) return;
            if (BZPrepTime == 99)
            {
                BZPrepTime = configData.BZPrepTime;
                bZID = ID;
                AnnounceBZ(ID);
                timer.Once(60, () => BZPrep(ID, zone));
                return;
            }
            if (BZPrepTime != 0)
            {
                {
                    BZPrepTime -= 1;
                    AnnounceBZ(ID);
                    timer.Once(60, () => BZPrep(ID, zone));
                    return;
                }
            }
            if (BZPrepTime == 0)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (BZPlayers.ContainsKey(player.userID)) continue;
                    BZButton(player, ID);
                }
                InitializeBZTime(ID, 3600);
                BZTimerCountdown(ID);
                List<string> build = new List<string>();
                string[] zoneArgs = build.ToArray();
                ZoneManager?.Call("CreateOrUpdateZone", zone, zoneArgs);
            }
        }

        private void eraseZone(BasePlayer player, ushort ID, string type)
        {
            if (type == "faction")
            {
                var factionname = factionData.Factions[ID].Name;
                {
                    ZoneManager.Call("EraseZone", ID.ToString());
                    factionData.Factions[ID].FactionZone = false;
                    SendPuts(string.Format(lang.GetMessage("FactionZoneDestroyed", this), factionname));
                    UnShadeZone(player, ID.ToString());
                    RemoveMapMarker($"{factionname} {lang.GetMessage("FactionZone", this)}");
                    ZoneManagement(player, "admin");
                    return;
                }
            }
            else if (type == "battle")
            {
                var zone = BattleZones[ID];
                var factionname = factionData.Factions[ID].Name;
                ZoneManager.Call("EraseZone", zone);
                UnShadeZone(player, zone);
                RemoveMapMarker($"{factionname} {lang.GetMessage("BattleZone", this)}");
                DestroyFactionMenu(player);
            }
        }

        private void ShadeZone(BasePlayer player, string zoneID)
        {
            if (ZoneDomes)
            {
                ZoneDomes.Call("AddNewDome", player, zoneID);
            }
        }

        private void UnShadeZone(BasePlayer player, string zoneID)
        {
            if (ZoneDomes)
            {
                ZoneDomes.Call("RemoveExistingDome", player, zoneID);
            }
        }

        private object VerifyZoneID(string zoneid) => ZoneManager?.Call("CheckZoneID", zoneid);

        private void AddMapMarker(float x, float z, string name, string icon = "special")
        {
            if (LustyMap)
            {
                LustyMap.Call("AddMarker", x, z, name, icon);
            }
        }
        private void RemoveMapMarker(string name)
        {
            if (LustyMap)
                LustyMap.Call("RemoveMarker", name);
        }

        private void InitializeBZTime(ushort ID, int time)
        {
            BZTimers.Add(ID, timer.Once(time, () => EndBZ(ID, "TimeLimit")));
            var num = ID - 1;
            ushort tempID = (ushort)num;
            BZTimers.Add(tempID, timer.Every(time/6, () => BZIntermidiateCheck(tempID)));
            BZTimes.Add(ID, time);
        }

        private void BZIntermidiateCheck(ushort ID)
        {
            BZTimers.Remove(ID);
            var enemydeath = 0;
            var enemyjoined = 0;
            var activeenemies = 0;
            foreach (var player in BZPlayers)
            {
                if (player.Value.faction != playerData.playerFactions[player.Key].faction)
                {
                    if (player.Value.died == true) enemydeath++;
                    if (player.Value.entered == true) enemyjoined++;
                }
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (GetPlayerFaction(p) != bZID) activeenemies++;
            }
            if (enemyjoined == enemydeath && activeenemies <= enemyjoined)
                EndBZ(bZID, "EnemiesDead");
            else BZTimers.Add(ID, timer.Once(BZTimes[bZID] / 2, () => BZIntermidiateCheck(ID)));
        }

        private void BZAttackerCheck()
        {
            var enemydeath = 0;
            var enemyjoined = 0;
            var activeenemies = 0;
            foreach (var player in BZPlayers)
            {
                if (player.Value.faction != playerData.playerFactions[player.Key].faction)
                {
                    if (player.Value.died == true) enemydeath++;
                    if (player.Value.entered == true) enemyjoined++;
                }
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (GetPlayerFaction(p) != bZID) activeenemies++;
            }
            if (enemyjoined == enemydeath && activeenemies <= enemyjoined)
                EndBZ(bZID, "EnemiesDead");
        }

        private bool InBZ(BasePlayer player)
        {
            if (BZPlayers.ContainsKey(player.userID))
            {
                var zone = BZPlayers[player.userID].bz;
                object inZone = ZoneManager?.Call("isPlayerInZone", zone, player);
                if (inZone is bool)
                {
                    return (bool)inZone;
                }
            }
            return false;
        }
 

        private void BZTimerCountdown(ushort ID)
        {
            if (bZID == 0) return;
            var num = ID + 1;
            string zone = num.ToString();
            object success = ZoneManager?.Call("GetPlayersInZone", zone);
            if (success == null) return;
            var playersList = success as List<ulong>;
            BZTimes[ID]--;
            foreach (var p in playersList)
            {
                BasePlayer player = BasePlayer.FindByID(p);
                RefreshBZTimer(player, ID);
            }
            timer.Once(1, () => BZTimerCountdown(ID));
        }

        private void RefreshBZTimer(BasePlayer player, ushort ID)
        {
            CuiHelper.DestroyUi(player, BattleZoneTimer);
            var element = UI.CreateElementContainer("BattleZoneTimer", "0.3 0.3 0.3 0.6", "0.45 0.91", "0.55 0.948", false);
            TimeSpan dateDifference = TimeSpan.FromSeconds(BZTimes[ID]);
            string clock = string.Format("{0:D2}:{1:D2}", dateDifference.Minutes, dateDifference.Seconds);
            UI.CreateLabel(ref element, "BattleZoneTimer", "", clock, 20, "0 0", "1 1");
            if (BattleZones.ContainsKey(ID))
            {
                CuiHelper.AddUi(player, element);
               
            }
        }

        private Vector3 CalculateOutsidePos(BasePlayer player, string zoneID)
        {
            float distance = 0;
            Vector3 zonePos = (Vector3)ZoneManager?.Call("GetZoneLocation", new object[] { zoneID });
            object zoneRadius = ZoneManager?.Call("GetZoneRadius", new object[] { zoneID });
            Vector3 zoneSize = (Vector3)ZoneManager?.Call("GetZoneSize", new object[] { zoneID });
            var playerPos = player.transform.position;
            var cachedDirection = playerPos - zonePos;
            if (zoneSize != Vector3.zero)
                distance = zoneSize.x > zoneSize.z ? zoneSize.x : zoneSize.z;
            else
                distance = (float)zoneRadius;

            var newPos = zonePos + (cachedDirection / cachedDirection.magnitude * (distance + 2f));
            newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
            return newPos;
        }

        void DestroyBZTimers(ushort ID)
        {
            foreach (var entry in BZTimers)
                entry.Value.Destroy();
            BZTimers.Clear();
            foreach (var p in BZKillTimers)
                BZKillTimers[p.Key].Destroy();
            BZKillTimers.Clear();
            BZTimes.Clear();
        }

        void EndBZ(ushort ID, string reason)
        {
            if (GetLeader(ID) != null)
                if (bzTurrets.ContainsKey((ulong)GetLeader(ID)))
                {
                    foreach (var autoturret in bzTurrets[(ulong)GetLeader(ID)])
                        autoturret.DieInstantly();
                    bzTurrets[(ulong)GetLeader(ID)].Clear();
                }
            eraseZone(null, ID, "battle");
            DestroyBZTimers(ID);
            var faction = ID;
            var factionname = factionData.Factions[faction].Name;
            SendPuts(string.Format(lang.GetMessage("BZEnded", this), factionname, lang.GetMessage(reason, this)));
            bZID = 0;
            timer.Once(configData.BattleZonesCooldown * 60, () => BattleZones.Remove(ID));
            int rewardamount = 0;
            var rewardtype = "";
            if (configData.Use_EconomicsReward)
            {
                rewardamount = configData.BattleZoneRewardEconomics;
                rewardtype = "Economics";
            }
            if (configData.Use_TokensReward)
            {
                rewardamount = configData.BattleZoneRewardTokens;
                rewardtype = "Tokens";
            }
            if (configData.Use_ServerRewardsReward)
            {
                rewardamount = configData.BattleZoneRewardServerRewards;
                rewardtype = "Server Reward Points";
            }
            foreach (var player in BZPlayers)
            {
                if (player.Value.died == false && player.Value.entered == true)
                {
                    BasePlayer surviver = BasePlayer.FindByID(player.Key);
                    ImmunityList.Add(player.Key);
                    RestoreBZPlayer(surviver);
                    SendMSG(surviver, string.Format(lang.GetMessage("Immune", this))); 
                }
                if (reason == "LeaderDeath" && player.Value.entered == true)
                {
                    if (playerData.playerFactions[player.Key].faction != ID)
                    {
                        if (configData.Use_EconomicsReward)
                        {
                            Economics.Call("DepositS", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_TokensReward)
                        {
                            EventManager.Call("AddTokens", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_ServerRewardsReward)
                        {
                            ServerRewards?.Call("AddPoints", player.Key.ToString(), rewardamount);
                        }
                        try
                        {
                            BasePlayer participant = BasePlayer.FindByID(player.Key);
                            SendMSG(participant, string.Format(lang.GetMessage("LeaderDeathWinner", this), rewardtype, rewardamount));
                        }
                        catch { }
                    }
                }
                if (reason == "TimeLimit" && player.Value.entered == true)
                {
                    if (playerData.playerFactions[player.Key].faction == ID)
                    {
                        if (configData.Use_EconomicsReward)
                        {
                            Economics.Call("DepositS", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_TokensReward)
                        {
                            EventManager.Call("AddTokens", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_ServerRewardsReward)
                        {
                            ServerRewards?.Call("AddPoints", player.Key.ToString(), rewardamount);
                        }
                        try
                        {
                            BasePlayer participant = BasePlayer.FindByID(player.Key);
                            SendMSG(participant, string.Format(lang.GetMessage("TimeLimitWinner", this), rewardtype, rewardamount));
                        }
                        catch { }
                    }
                }
                if (reason == "EnemiesDead" && player.Value.entered == true)
                {
                    if (playerData.playerFactions[player.Key].faction == ID)
                    {
                        if (configData.Use_EconomicsReward)
                        {
                            Economics.Call("DepositS", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_TokensReward)
                        {
                            EventManager.Call("AddTokens", player.Key.ToString(), rewardamount);
                        }
                        if (configData.Use_ServerRewardsReward)
                        {
                            ServerRewards?.Call("AddPoints", player.Key.ToString(), rewardamount);
                        }
                        try
                        {
                            BasePlayer participant = BasePlayer.FindByID(player.Key);
                            SendMSG(participant, string.Format(lang.GetMessage("EnemiesDeadWinner", this), rewardtype, rewardamount));
                        }
                        catch { }           
                    }
                }
            }
            BZPlayers.Clear();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, BattleZoneTimer);
            DestroyZoneEntities();
            timer.Once(600, () => ClearImmunity());
        }

        void ClearImmunity()
        {
            foreach (var p in ImmunityList)
            {
                if (BasePlayer.activePlayerList.Contains(BasePlayer.FindByID(p)))
                {
                    BasePlayer player = BasePlayer.FindByID(p);
                    SendMSG(player, string.Format(lang.GetMessage("ImmunityGone", this))); 
                }
            }
            ImmunityList.Clear();
        }

        void AnnounceBZ(ushort ID)
        {
            var faction = ID;
            var factionname = factionData.Factions[faction].Name;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (BZPrepTime != 0)
                    SendMSG(player, string.Format(lang.GetMessage("NewBZ", this), factionname, BZPrepTime));
                else
                    SendMSG(player, string.Format(lang.GetMessage("BZStart", this), factionname));
            }

        }
        void AnnounceDuringBZ()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!BZPlayers.ContainsKey(player.userID))
                {
                    SendMSG(player, string.Format(lang.GetMessage("StartedBZ", this)));
                }
                
            }
        }

        private void FindMonuments()
        {
            var i = 0;
            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("autospawn/monument"))
                {
                    var pos = gobject.transform.position;
                    var radius = 60f;
                    MonumentLocations.Add(i, new Monuments { position = pos, radius = radius });
                    i++;
                }
            }
        }

        #endregion

        #region Faction Creation Functions

        private void QuitFactionCreation(BasePlayer player, bool isCreatingFaction)
        {
            if (isCreatingFaction)
            {
                if (ActiveCreations[player.userID].Entry.group != "")
                {
                    ConsoleSystem.Run.Server.Normal($"group remove {ActiveCreations[player.userID].Entry.group}");
                }
                ActiveCreations.Remove(player.userID);
            }
            else ActiveEditors.Remove(player.userID);
            SendMSG(player, lang.GetMessage("QuitFactionCreation", this));
            DestroyUI(player);
        }

        private void RemoveFaction(BasePlayer player, ushort ID)
        {
            var factionname = factionData.Factions[ID].Name;
            DestroyUI(player);
            if (factionData.Factions[ID].FactionZone == true)
                eraseZone(player, ID, "faction");
            factionData.Factions.Remove(ID);
            ReassignPlayers(ID);
            SendPuts($"{factionname} {lang.GetMessage("FactionDeleted", this)}");
            SaveData();
            FactionManager(player);
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                RefreshTicker(p);
        }

        private void ReassignPlayers(ushort ID)
        {
            foreach (var entry in playerData.playerFactions)
                if (entry.Value.faction == ID)
                {
                    playerData.playerFactions[entry.Key].faction = default(ushort);
                    BasePlayer player = BasePlayer.FindByID(entry.Key);
                    SetFaction(player);
                }
        }

        private void SaveFaction(BasePlayer player, bool isCreatingFaction)
        {
            FactionDesigner Creator;
            Faction Faction;

            if (isCreatingFaction)
                Creator = ActiveCreations[player.userID];
            else Creator = ActiveEditors[player.userID];

            Faction = Creator.Entry;

            if (isCreatingFaction)
            {
                factionData.Factions.Add(Creator.ID, Faction);
                ActiveCreations.Remove(player.userID);
            }
            else
            {
                factionData.Factions.Remove(Creator.ID);
                factionData.Factions.Add(Creator.ID, Faction);
                ActiveEditors.Remove(player.userID);
            }
            DestroyUI(player);
            SaveData();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                RefreshTicker(p);
            SendMSG(player,string.Format(lang.GetMessage("NewFactionCreated", this),Faction.Name));
        }
        private void ExitFactionEditor(BasePlayer player, bool isEditingFaction)
        {
            if (isEditingFaction)
            {
                ActiveEditors.Remove(player.userID);
                SendMSG(player, lang.GetMessage("QuitFactionEditor", this));
                DestroyUI(player);
            }
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            DestroyEntries(player);
        }
        private void DestroyEntries(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            if (OpenUI.ContainsKey(player.userID))
            {
                foreach (var entry in OpenUI[player.userID])
                    CuiHelper.DestroyUi(player, entry);
                OpenUI.Remove(player.userID);
            }
        }

        private int GetRandomNumber()
        {
            var random = new System.Random();
            int number = random.Next(int.MinValue, int.MaxValue);
            return number;
        }

        private Faction GetFactionInfo(ushort ID)
        {
            foreach (var entry in factionData.Factions)
            {
                if (entry.Key == ID)
                    return entry.Value;
            }
            return null;
        }

        public static class StringTool
        {
            public static string Truncate(string source, int length)
            {
                if (source.Length > length)
                {
                    source = source.Substring(0, length);
                }
                return source;
            }
        }

        #endregion

        #region Faction Creation Console Commands

        [ConsoleCommand("CUI_SelectColor")]
        private void cmdSelectColor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var Color = arg.Args[0];
                FactionDesigner Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];
                foreach (var entry in Colors.Where(kvp => kvp.Color == Color))
                {
                    Creator.Entry.ChatColor = entry.ChatColor;
                    Creator.Entry.UIColor = entry.UIColor;
                }
                DestroyUI(player);
                if (ActiveEditors.ContainsKey(player.userID))
                {
                    CreationHelp(player, 20);
                    return;
                }
                CreationHelp(player, 3);
            }
        }


        [ConsoleCommand("CUI_SelectFactionType")]
        private void cmdSelectFactionType(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var type = (FactionType)Enum.Parse(typeof(FactionType), arg.Args[0]);
                FactionDesigner Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];
                Creator.Entry.type = type;
                Creator.Entry.Kills = 0;
                Creator.Entry.PlayerCount = 0;
                DestroyUI(player);
                if (ActiveEditors.ContainsKey(player.userID))
                {
                    CreationHelp(player, 20);
                    return;
                }
                if (configData.Use_Kits)
                {
                    CreationHelp(player, 4);
                }
                else if (configData.Use_Groups)
                {
                    CreationHelp(player, 5);
                }
                else
                    CreationHelp(player, 20);
            }
        }


        [ConsoleCommand("CUI_SelectFactionKit")]
        private void cmdSelectFactionKit(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                var kit = arg.Args[0];
                FactionDesigner Creator;
                if (ActiveCreations.ContainsKey(player.userID))
                    Creator = ActiveCreations[player.userID];
                else Creator = ActiveEditors[player.userID];
                Creator.Entry.kit = kit;
                DestroyUI(player);
                if (ActiveEditors.ContainsKey(player.userID))
                {
                    CreationHelp(player, 20);
                    return;
                }
                if (configData.Use_Groups)
                {
                    CreationHelp(player, 5);
                }
                else
                    CreationHelp(player, 20);
            }
        }

        [ConsoleCommand("CUI_NewFaction")]
        private void cmdNewFaction(ConsoleSystem.Arg arg)
        {
            ActiveCreations.Clear();
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFactionMenu(player);
            if (player.IsAdmin())
            {
                var random = new System.Random();
                int number;
                int r = 0;
                int Number = GetRandomNumber();
                ushort ID = (ushort)Number;
                while (r >= 1)
                {
                    number = random.Next(int.MinValue, int.MaxValue);
                    if (ID >= ushort.MinValue & ID <= ushort.MaxValue)
                    {
                        foreach (var entry in factionData.Factions.Where(kvp => kvp.Key == ID)) r++;
                    }
                }
                ActiveCreations.Add(player.userID, new FactionDesigner { ID = ID, Entry = new Faction { } });
                DestroyUI(player);
                CreationHelp(player, 0);
            }
        }

        [ConsoleCommand("CUI_TryDeleteFaction")]
        private void cmdTryDeleteFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                ushort ID = Convert.ToUInt16(arg.Args[0]);
                foreach (var faction in factionData.Factions)
                {
                    if (faction.Key == ID)
                    {
                        DestroyUI(player);
                        ConfirmFactionDeletion(player, ID);
                        return;
                    }
                }
            }
        }

        [ConsoleCommand("UI_CUI_LeaderEditing")]
        private void cmdCUI_LeaderEditing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                ushort faction = Convert.ToUInt16(arg.Args[0]);
                int page = Convert.ToInt16(arg.Args[1]);
                LeaderEditing(player, faction, page);
                return;
            }
        }

        [ConsoleCommand("UI_CUIUnassignLeader")]
        private void cmdCUI_UnassignLeader(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                ushort faction = Convert.ToUInt16(arg.Args[0]);
                var oldleaderID = factionData.leader[faction];
                factionData.leader.Remove(faction);
                try
                {
                    BasePlayer oldleader = BasePlayer.FindByID(oldleaderID);
                    SendMSG(oldleader, string.Format(lang.GetMessage("RemovedAsLeader", this), factionData.Factions[faction].Name));
                    SendMSG(player, string.Format(lang.GetMessage("AdminMSGRemovedLeader", this), oldleader.displayName, factionData.Factions[faction].Name));
                }
                catch
                {
                    SendMSG(player, string.Format(lang.GetMessage("AdminMSGRemovedLeader", this), playerData.playerFactions[oldleaderID].Name, factionData.Factions[faction].Name));
                }
                SaveData();
                FactionMenuBar(player);
                LeaderEditing(player, faction);
            }
        }

        [ConsoleCommand("UI_CUIAssignLeader")]
        private void cmdCUIAssignLeader(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                ushort UID = Convert.ToUInt16(arg.Args[0]);
                var faction = LeaderPromotes[UID].factionID;
                foreach (var entry in LeaderPromotes)
                    if (entry.Key == UID)
                    {
                        var factionname = factionData.Factions[entry.Value.factionID].Name;
                        factionData.leader.Remove(faction);
                        factionData.leader.Add(entry.Value.factionID, entry.Value.playerID);
                        try
                        {
                            BasePlayer newleader = BasePlayer.FindByID(entry.Value.playerID);
                            BroadcastFaction(newleader, $"{ playerData.playerFactions[entry.Value.playerID].Name} {lang.GetMessage("newassignedleader", this)} {factionname}");
                            SendMSG(player, string.Format(lang.GetMessage("AdminMSGAssignedLeader", this), newleader.displayName, factionname));
                        }
                        catch
                        {
                            foreach (BasePlayer p in BasePlayer.activePlayerList)
                                if (GetPlayerFaction(p) == entry.Value.factionID)
                                {
                                    BroadcastFaction(p, $"{ playerData.playerFactions[entry.Value.playerID].Name} {lang.GetMessage("newassignedleader", this)} {factionname}");
                                    break;
                                }
                            SendMSG(player, string.Format(lang.GetMessage("AdminMSGAssignedLeader", this), playerData.playerFactions[entry.Value.playerID].Name, factionname));
                        }
                    }
                foreach (var entry in LeaderPromotes.Where(kvp => kvp.Value.factionID == faction).ToList())
                {
                    LeaderPromotes.Remove(entry.Key);
                }
                SaveData();
                FactionMenuBar(player);
                LeaderEditing(player, faction);
            }
        }

        [ConsoleCommand("CUI_DeleteFaction")]
        private void cmdDeleteFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args[0] == "reject")
            {
                DestroyUI(player);
                SendMSG(player, string.Format(lang.GetMessage("ExitedFactionDeletion", this)));
                return;
            }
            if (player.IsAdmin())
            {
                ushort ID = Convert.ToUInt16(arg.Args[0]);
                RemoveFaction(player, ID);
                return;

            }
        }

        [ConsoleCommand("CUI_EditFaction")]
        private void cmdEditFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                if (ActiveEditors.ContainsKey(player.userID))
                    ActiveEditors.Remove(player.userID);
                ActiveEditors.Add(player.userID, new FactionDesigner());

                ushort ID = Convert.ToUInt16(arg.Args[0]);
                var Faction = GetFactionInfo(ID);
                if (Faction == null) return;
                ActiveEditors[player.userID].ID = ID;
                ActiveEditors[player.userID].Entry = Faction;
                ActiveEditors[player.userID].OldEntry = Faction;
                DestroyFactionMenu(player);
                FactionEditorMenu(player);
            }
        }

        [ConsoleCommand("CUI_EditFactionVar")]
        private void cmdEditFactionVar(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                if (ActiveEditors.ContainsKey(player.userID))
                {
                    DestroyUI(player);
                    switch (arg.Args[0].ToLower())
                    {
                        case "name":
                            CreationHelp(player, 0);
                            break;
                        case "title":
                            CreationHelp(player, 1);
                            break;
                        case "color":
                            CreationHelp(player, 2);
                            break;
                        case "type":
                            CreationHelp(player, 3);
                            break;
                        case "kit":
                            CreationHelp(player, 4);
                            break;
                        case "group":
                            CreationHelp(player, 5);
                            break;
                        default:
                            return;
                    }
                }
            }
        }

        [ConsoleCommand("CUI_SaveFaction")]
        private void cmdSaveFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool creating = false;
                if (ActiveCreations.ContainsKey(player.userID))
                    creating = true;
                SaveFaction(player, creating);
            }
        }
        [ConsoleCommand("CUI_ExitFaction")]
        private void cmdExitFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool creating = false;
                if (ActiveCreations.ContainsKey(player.userID))
                    creating = true;
                QuitFactionCreation(player, creating);
            }
        }

        [ConsoleCommand("CUI_ExitFactionEditor")]
        private void cmdExitFactionEditor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool IsEditing = false;
                if (ActiveEditors.ContainsKey(player.userID))
                    IsEditing = true;
                ExitFactionEditor(player, IsEditing);
            }
        }

        [ConsoleCommand("CUI_TryDeleteSpawn")]
        private void cmdTryDeleteSpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                ushort ID = Convert.ToUInt16(arg.Args[1]);
                string name = "";
                string spawntype = "";
                if (arg.Args.Contains("rally"))
                    foreach (var rally in factionData.RallySpawns)
                    {
                        if (rally.Key == ID)
                        {
                            spawntype = "rally";
                            name = factionData.RallySpawns[ID].Name;
                            DestroyUI(player);
                            ConfirmSpawnDeletion(player, ID, name, spawntype);
                            return;
                        }
                    }
                if (arg.Args.Contains("faction"))
                    foreach (var spawn in factionData.FactionSpawns)
                    {
                        if (spawn.Key == ID)
                        {
                            spawntype = "faction";
                            name = factionData.FactionSpawns[ID].Name;
                            DestroyUI(player);
                            ConfirmSpawnDeletion(player, ID, name, spawntype);
                            return;
                        }
                    }
            }
        }

        [ConsoleCommand("CUI_DeleteSpawn")]
        private void cmdDeleteSpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args[0] == "reject")
            {
                DestroyUI(player);
                SendMSG(player, string.Format(lang.GetMessage("ExitedFactionDeletion", this)));
                return;
            }
            if (player.IsAdmin())
            {
                if (arg.Args[0] == "rally")
                {
                    ushort ID = Convert.ToUInt16(arg.Args[0]);
                    CUIRemoveRallySpawn(player, ID);
                    return;
                }
                else if (arg.Args[0] == "faction")
                {
                    ushort ID = Convert.ToUInt16(arg.Args[0]);
                    CUIRemoveRallySpawn(player, ID);
                    return;
                }

            }
        }

        #endregion

        #region FactionCreationUI

        private void CreationHelp(BasePlayer player, int page = 0, ushort factionID = 0)
        {
            DestroyEntries(player);
            FactionDesigner faction = null;
            SpawnDesigner spawn = null;
            if (ActiveCreations.ContainsKey(player.userID))
                faction = ActiveCreations[player.userID];
            else if (ActiveEditors.ContainsKey(player.userID))
                faction = ActiveEditors[player.userID];
            else if (SpawnCreation.ContainsKey(player.userID))
                spawn = SpawnCreation[player.userID];
            //if (faction == null && spawn == null) return;

            var HelpMain = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.3 0.3", "0.7 0.9");
            UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            switch (page)
            {
                case 0:
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, "", $"{configData.MSG_MainColor}This is the faction creation help menu.\n</color> {configData.MSG_Color}Follow the instructions given by typing in chat.\n\nYou can exit faction creation at any time by typing </color>{configData.MSG_MainColor}'quit'\n\n\n\nTo proceed enter the name of the new Faction!</color>", 20, "0 0", "1 1");
                    break;
                case 1:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, UIColors["red"], $"{configData.MSG_MainColor}Provide a Faction Leader Title</color>", 20, "0 0", "1 1");
                    break;
                case 2:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, "", $"{configData.MSG_MainColor}Select a Faction Color</color>", 20, "0 0", "1 1");
                    int i = 0;
                    foreach (var entry in Colors)
                    {
                        CreateOptionButton(ref HelpMain, FactionsUIPanel, entry.UIColor, entry.Color, $"CUI_SelectColor {entry.Color}", i); i++;
                    }
                    break;
                case 3:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, "", $"{configData.MSG_MainColor}Select a Faction Type</color>", 20, "0 0", "1 1");
                    var values = Enum.GetValues(typeof(FactionType));
                    i = 0;
                    foreach (var entry in values)
                    {
                        CreateOptionButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], $"{ Enum.GetName(typeof(FactionType), entry)}", $"CUI_SelectFactionType {entry}", i); i++;
                    }
                    break;

                case 4:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, "", $"{configData.MSG_MainColor}Select a Faction Kit</color>", 20, "0 0", "1 1");
                    i = 0;
                    CreateKitButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], "No Kit", $"CUI_SelectFactionKit None", i);i++;
                    foreach (string kitname in GetKitNames())
                    {
                        CreateKitButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], kitname, $"CUI_SelectFactionKit {kitname}", i); i++;
                    }
                    break;
                case 5:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, "", $"{configData.MSG_MainColor}Provide a Faction Group</color>", 20, "0 0", "1 1");
                    break;
                case 9:

                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, UIColors["red"], $"{configData.MSG_MainColor}Please Type a Spawn Point Name</color>", 20, "0 0", "1 1");
                    break;
                case 10:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, configData.MSG_Color, $"You have successfully created a New Spawn Point. To confirm click 'Save Spawn'", 20, "0.1 0.1", "0.9 0.89");
                    UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], "Save Spawn", 18, "0.2 0.05", "0.4 0.15", $"CUI_SaveSpawn");
                    UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonred"], "Cancel", 18, "0.6 0.05", "0.8 0.15", $"CUI_ExitSpawn");
                    break;
                case 11:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    foreach (var entry in FactionInvites)
                        if (entry.Value.confirm)
                        {
                            var FactionName = factionData.Factions[entry.Value.factionID].Name;
                            UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                            UI.CreateLabel(ref HelpMain, FactionsUIPanel, configData.MSG_Color, $"You have been invited to join the Faction: {FactionName}. Click 'Accept' or 'Decline'", 20, "0.1 0.1", "0.9 0.89");
                            UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], "Accept", 18, "0.2 0.05", "0.4 0.15", $"CUI_AcceptInvite");
                            UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonred"], "Decline", 18, "0.6 0.05", "0.8 0.15", $"CUI_DeclineInvite");
                            break;
                        }
                    break;
                case 12:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    foreach (var entry in FactionKicks)
                        if (entry.Value.executerID == player.userID && (entry.Value.confirm))
                        {
                            UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                            UI.CreateLabel(ref HelpMain, FactionsUIPanel, configData.MSG_Color, lang.GetMessage("ConfirmKick", this), 20, "0.1 0.1", "0.9 0.89");
                            UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], "Yes", 18, "0.2 0.05", "0.4 0.15", $"UI_ConfirmKickPlayer yes");
                            UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonred"], "No", 18, "0.6 0.05", "0.8 0.15", $"UI_ConfirmKickPlayer no");
                            break;
                        }
                    break;
                default:
                    CuiHelper.DestroyUi(player, FactionsUIPanel);
                    HelpMain = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
                    UI.CreatePanel(ref HelpMain, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, configData.MSG_Color, $"You have successfully created a new faction. To confirm click 'Save Faction'", 20, "0 .9", "1 1");
                    string factionDetails = $"Faction Name: {faction.Entry.Name}\nLeader Title: {faction.Entry.LeaderTitle}\nFaction Type: {faction.Entry.type}";
                    if (configData.Use_Kits)
                    {
                        factionDetails += $"\nFaction Kit: {faction.Entry.FactionKit}";
                    }
                    if (configData.Use_Groups)
                    {
                        factionDetails += $"\nFaction Group: {faction.Entry.group}";
                    }
                    UI.CreateLabel(ref HelpMain, FactionsUIPanel, faction.Entry.UIColor, factionDetails, 20, "0.1 0.1", "0.9 0.89", TextAnchor.MiddleLeft);
                    UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonbg"], "Save Faction", 18, "0.2 0.05", "0.4 0.15", $"CUI_SaveFaction");
                    if (ActiveCreations.ContainsKey(player.userID))
                        UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonred"], "Cancel", 18, "0.6 0.05", "0.8 0.15", $"CUI_ExitFaction");
                    else if (ActiveEditors.ContainsKey(player.userID))
                        UI.CreateButton(ref HelpMain, FactionsUIPanel, UIColors["buttonred"], "Cancel", 18, "0.6 0.05", "0.8 0.15", $"CUI_ExitFactionEditor");
                    break;
            }
            CuiHelper.AddUi(player, HelpMain);
        }

        private void FactionInfo(BasePlayer player, ushort faction, int page)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, FactionsUIPanel, "1 1 1 0.025", "FACTION INFO", 100, "0.01 0.01", "0.99 0.99");
            int i = 0;
            var f = factionData.Factions[faction];
            var typeinfo = "";
            var leadername = "None";
            if (f.type == FactionType.FFA) typeinfo = lang.GetMessage("Normal", this);
            if (f.type == FactionType.Regular)
            {
                if (configData.BuildingProtectionEnabled == true && configData.FFDisabled == true)
                    typeinfo = lang.GetMessage("Normal", this);
                if (configData.BuildingProtectionEnabled == false && configData.FFDisabled == false)
                    typeinfo = lang.GetMessage("NormalNoBuildingProtection", this);
                if (configData.BuildingProtectionEnabled == true && configData.FFDisabled == false)
                    typeinfo = lang.GetMessage("NormalFFEnabled", this);
                if (configData.BuildingProtectionEnabled == false && configData.FFDisabled == true)
                    typeinfo = lang.GetMessage("FFEnabledNoBuildingProtection", this);
            }
            if (factionData.leader.ContainsKey(faction))
            {
                leadername = playerData.playerFactions[factionData.leader[faction]].Name;
            }
            CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Name: {f.ChatColor}{f.Name}</color>", i); i++;
            CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Type: {f.ChatColor}{ typeinfo}</color>", i); i++;
            CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Player Count: {f.ChatColor}{f.PlayerCount}</color>", i); i++;
            if (configData.Use_FactionKillIncentives)
            {
                CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Kill Total: {f.ChatColor}{f.Kills}</color>", i); i++;
            }
            CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Leader: {f.ChatColor}{leadername}</color>", i); i++;
            if (configData.Use_Taxes)
            {
                CreateFactionDetails(ref element, FactionsUIPanel, $"Faction Tax: {f.ChatColor}{f.tax}</color>", i); i++;
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], "Go Back", 18, "0.2 0.05", "0.4 0.15", $"UI_FactionSelection {page}");
            UI.CreateButton(ref element, FactionsUIPanel, f.UIColor, "Join Faction", 18, "0.6 0.05", "0.8 0.15", $"CUI_FactionSelection {faction}");
            CuiHelper.AddUi(player, element);
        }

        private void CreateFactionEditButton(ref CuiElementContainer container, string panelName, string buttonname, string command, int number)
        {
            Vector2 dimensions = new Vector2(0.2f, 0.1f);
            Vector2 origin = new Vector2(0.4f, 0.7f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;

            UI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 18, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", command);
        }

        private void CreateFactionDetails(ref CuiElementContainer container, string panelName, string text, int number)
        {
            Vector2 dimensions = new Vector2(0.8f, 0.1f);
            Vector2 origin = new Vector2(0.1f, 0.7f);
            Vector2 offset = new Vector2(0, (0.01f + dimensions.y) * number);

            Vector2 posMin = origin - offset;
            Vector2 posMax = posMin + dimensions;
            UI.CreateLabel(ref container, panelName, UIColors["buttonbg"], text, 18, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}");
        }

        private void DeletionMenu(BasePlayer player, string page, string command)
        {
            DestroyEntries(player);
            var Main = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.12 0", "1 1");
            UI.CreatePanel(ref Main, FactionsUIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            UI.CreateLabel(ref Main, FactionsUIPanel, "1 1 1 0.025", page, 200, "0.01 0.01", "0.99 0.99");
            int i = 0;
            CreateDelEditButton(ref Main, 0.795f, FactionsUIPanel, "Delete", i, command); i++;
            CuiHelper.AddUi(player, Main);
        }

        private void ConfirmFactionDeletion(BasePlayer player, ushort ID)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var FactionName = factionData.Factions[ID].Name;
            var ConfirmDelete = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.2 0.4", "0.8 0.8", true);
            UI.CreatePanel(ref ConfirmDelete, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref ConfirmDelete, FactionsUIPanel, "", $"{configData.MSG_MainColor}Are you sure you want to delete: {FactionName}</color>", 20, "0.1 0.6", "0.9 0.9");
            UI.CreateButton(ref ConfirmDelete, FactionsUIPanel, UIColors["buttonbg"], "Yes", 18, "0.6 0.2", "0.8 0.3", $"CUI_DeleteFaction {ID}");
            UI.CreateButton(ref ConfirmDelete, FactionsUIPanel, UIColors["buttonbg"], "No", 18, "0.2 0.2", "0.4 0.3", $"CUI_DeleteFaction reject");

            CuiHelper.AddUi(player, ConfirmDelete);
        }

        private void ConfirmSpawnDeletion(BasePlayer player, ushort ID, string name, string spawntype)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var ConfirmDelete = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.2 0.4", "0.8 0.8", true);
            UI.CreatePanel(ref ConfirmDelete, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref ConfirmDelete, FactionsUIPanel, "", $"{configData.MSG_MainColor}Are you sure you want to delete the {spawntype.ToUpper()} spawn: {name}</color>", 20, "0.1 0.6", "0.9 0.9");
            UI.CreateButton(ref ConfirmDelete, FactionsUIPanel, UIColors["buttonbg"], "Yes", 18, "0.6 0.2", "0.8 0.3", $"CUI_DeleteSpawn {ID} {spawntype}");
            UI.CreateButton(ref ConfirmDelete, FactionsUIPanel, UIColors["buttonbg"], "No", 18, "0.2 0.2", "0.4 0.3", $"CUI_DeleteSpawn reject");

            CuiHelper.AddUi(player, ConfirmDelete);
        }

        private void CreateDelEditButton(ref CuiElementContainer container, float xPos, string panelName, string buttonname, int number, string command)
        {
            Vector2 dimensions = new Vector2(0.18f, 0.05f);
            Vector2 origin = new Vector2(xPos, 0.8f);
            Vector2 offset = new Vector2(0, (-0.01f - dimensions.y) * number);

            Vector2 posMin = origin + offset;
            Vector2 posMax = posMin + dimensions;

            UI.CreateButton(ref container, panelName, UIColors["buttonbg"], buttonname, 14, $"{posMin.x} {posMin.y}", $"{posMax.x} {posMax.y}", $"{command} {buttonname}");
        }

        private void FactionEditorMenu(BasePlayer player)
        {
            DestroyEntries(player);
            var Main = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.2 0.4", "0.8 0.8");
            UI.CreatePanel(ref Main, FactionsUIPanel, UIColors["light"], "0.01 0.01", "0.99 0.99", true);
            UI.CreateLabel(ref Main, FactionsUIPanel, "1 1 1 0.025", "EDITOR", 200, "0.01 0.01", "0.99 0.99");

            int i = 0;
            UI.CreateLabel(ref Main, FactionsUIPanel, "", $"{configData.MSG_MainColor}Select a value to change</color>", 20, "0.25 0.8", "0.75 0.9");
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Name", "CUI_EditFactionVar name", i); i++;
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Leader Title", "CUI_EditFactionVar title", i); i++;
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Color", "CUI_EditFactionVar color", i); i++;
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Type", "CUI_EditFactionVar type", i); i++;
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Kit", "CUI_EditFactionVar kit", i); i++;
            CreateFactionEditButton(ref Main, FactionsUIPanel, "Faction Group", "CUI_EditFactionVar group", i); i++;
            CuiHelper.AddUi(player, Main);
        }
        private void NumberPad(BasePlayer player, string msg, string cmd)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.3 0.2", "0.7 0.8", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, FactionsUIPanel, "", $"{configData.MSG_MainColor}{msg}</color>", 12, "0.1 0.7", "0.9 0.9",TextAnchor.UpperCenter);
            var i = 0;
            while (i < 60)
            {
                CreateNumberPadButton(ref element, FactionsUIPanel, .02f, i, cmd); i++;
            }
            CuiHelper.AddUi(player, element);
        }
        private void CreateNumberPadButton(ref CuiElementContainer container, string panelName, float xPos, int number, string command)
        {
            var pos = CalcNumButtonPos(number);
            UI.CreateButton(ref container, panelName, UIColors["buttonbg"], number.ToString(), 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"{command} {number}");
        }

        private float[] CalcNumButtonPos(int number)
        {
            Vector2 position = new Vector2(0.085f, 0.65f);
            Vector2 dimensions = new Vector2(0.0725f, 0.0875f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 10)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 9 && number < 20)
            {
                offsetX = (0.01f + dimensions.x) * (number - 10);
                offsetY = (-0.02f - dimensions.y) * 1;
            }
            if (number > 19 && number < 30)
            {
                offsetX = (0.01f + dimensions.x) * (number - 20);
                offsetY = (-0.02f - dimensions.y) * 2;
            }
            if (number > 29 && number < 40)
            {
                offsetX = (0.01f + dimensions.x) * (number - 30);
                offsetY = (-0.02f - dimensions.y) * 3;
            }
            if (number > 39 && number < 50)
            {
                offsetX = (0.01f + dimensions.x) * (number - 40);
                offsetY = (-0.02f - dimensions.y) * 4;
            }
            if (number > 49 && number < 60)
            {
                offsetX = (0.01f + dimensions.x) * (number - 50);
                offsetY = (-0.02f - dimensions.y) * 5;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #endregion

        #region UI Creation

        //UI
        private string PanelLevelAdvanced = "LevelAdvancement";
        private string PanelRankAdvanced = "RankAdvancement";
        private string PanelKillTicker = "KillTicker";
        private string PanelFactionMenuBar = "FactionMenuBar";
        private string PanelSpawnButtons = "SpawnButtons";
        private string PanelBZButton = "BZButton";
        private string PanelMemberStatus = "MemberStatus";

        public class UI
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
                    new CuiElement().Parent,
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
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

        }

        private void CreateInstructionEntry(ref CuiElementContainer container, string panelName, string color, string command, int num)
        {
            var pos = InstructionPos(num);
            UI.CreateLabel(ref container, panelName, color, command, 18, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperLeft);
        }

        private float[] InstructionPos(int number)
        {
            Vector2 position = new Vector2(0.02f, 0.3f);
            Vector2 dimensions = new Vector2(0.75f, 0.5f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 20)
            {
                offsetY = (-0.0001f - dimensions.y) * number;
            }
            if (number > 19 && number < 40)
            {
                offsetX = (0.35f + dimensions.x) * 1;
                offsetY = (-0.0001f - dimensions.y) * (number - 20);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }



        private void CreateStatusEntry(ref CuiElementContainer container, string panelName, string name, float health, int num)
        {
            var percent = System.Convert.ToDouble((float)health / (float)100f);
            var xMax = 0.6f + (0.4f * percent);
            var ydifference = .0505;
            var ymin = 0.85 - (ydifference * num);
            var ymax = 0.89 - (ydifference * num);
            UI.CreateLabel(ref container, panelName, "1 1 1 1", name, 12, $"0.02 {ymin}", $"0.59 {ymax}", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref container, panelName, UIColors["buttonbg"], $"0.6 {ymin}", $"1.0 {ymax}");
            UI.CreatePanel(ref container, panelName, UIColors["green"], $"0.6 {ymin}", $"{xMax} {ymax}");
        }

        private void CreateTickerEntry(ref CuiElementContainer container, string panelName, ushort faction, int num)
        {
            string name = factionData.Factions[faction].Name.ToUpper();
            string color = factionData.Factions[faction].UIColor;
            int kills = factionData.Factions[faction].Kills;
            var pos = TickerEntryPos(num);
            UI.CreateLabel(ref container, panelName, color, $"{name}: {kills}", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleCenter);
        }

        private float[] TickerEntryPos(int number)
        {
            var c = 0;
            foreach (var faction in factionData.Factions.Where(kvp => kvp.Value.type == FactionType.Regular))
            {
                c++;
            }
            Vector2 position = new Vector2(0.1f, 0.5f);
            Vector2 dimensions = new Vector2(0.2f, 0.2f);
            if (c < 10)
            {
                position = new Vector2(0.2f, 0.5f);
                dimensions = new Vector2(0.2f, 0.2f);
            }
            if (c < 7)
            {
                position = new Vector2(0.3f, 0.5f);
                dimensions = new Vector2(0.2f, 0.2f);
            }
            if (c < 4)
            {
                position = new Vector2(0.4f, 0.5f);
                dimensions = new Vector2(0.2f, 0.2f);
            }
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 3)
            {
                offsetY = (-0.00001f - dimensions.y) * number;
            }
            if (number > 2 && number < 6)
            {
                offsetX = (0.0001f + dimensions.x) * 1;
                offsetY = (-0.00001f - dimensions.y) * (number - 3);
            }
            if (number > 5 && number < 9)
            {
                offsetX = (0.0001f + dimensions.x) * 2;
                offsetY = (-0.00001f - dimensions.y) * (number - 6);
            }
            if (number > 8 && number < 12)
            {
                offsetX = (0.0001f + dimensions.x) * 3;
                offsetY = (-0.00001f - dimensions.y) * (number - 9);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void CreateOptionButton(ref CuiElementContainer container, string panelName, string color, string name, string cmd, int num)
        {
            var pos = CalcButtonPos(num);
            UI.CreateButton(ref container, panelName, color, name, 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private void CreateKitButton(ref CuiElementContainer container, string panelName, string color, string name, string cmd, int num)
        {
            var pos = CalcKitButtonPos(num);
            UI.CreateButton(ref container, panelName, color, name, 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private float[] CalcKitButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.82f);
            Vector2 dimensions = new Vector2(0.125f, 0.125f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.03f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.03f + dimensions.x) * (number - 6);
                offsetY = (-0.06f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.03f + dimensions.x) * (number - 12);
                offsetY = (-0.06f - dimensions.y) * 2;
            }
            if (number > 17 && number < 24)
            {
                offsetX = (0.03f + dimensions.x) * (number - 18);
                offsetY = (-0.06f - dimensions.y) * 3;
            }
            if (number > 23 && number < 36)
            {
                offsetX = (0.03f + dimensions.x) * (number - 24);
                offsetY = (-0.06f - dimensions.y) * 4;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void CreateCMDButton(ref CuiElementContainer container, string panelName, ushort faction, string text, string cmd, int num)
        {
            var color = "";
            if (faction == 0)
            {
                color = UIColors["orange"];
            }
            else
            {
                color = factionData.Factions[faction].UIColor;
                //if (name1.Contains("Remove")) color = UIColors["red"];
                //if (name1.Contains("Add")) color = UIColors["green"];
            }
            var pos = CalcButtonPos(num);
            UI.CreateButton(ref container, panelName, color, text, 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private float[] CalcButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.8f);
            Vector2 dimensions = new Vector2(0.125f, 0.125f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.03f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.03f + dimensions.x) * (number - 6);
                offsetY = (-0.05f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.03f + dimensions.x) * (number - 12);
                offsetY = (-0.05f - dimensions.y) * 2;
            }
            if (number > 17 && number < 24)
            {
                offsetX = (0.03f + dimensions.x) * (number - 18);
                offsetY = (-0.05f - dimensions.y) * 3;
            }
            if (number > 23 && number < 32)
            {
                offsetX = (0.03f + dimensions.x) * (number - 24);
                offsetY = (-0.05f - dimensions.y) * 4;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void CreatePlayerEntry(ref CuiElementContainer container, string panelName, string color, string info, int num)
        {
            var pos = PlayerEntryPos(num);
            UI.CreateLabel(ref container, panelName, color, info, 16, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.UpperCenter);
        }

        private float[] PlayerEntryPos(int number)
        {
            Vector2 position = new Vector2(0.02f, 0.6f);
            Vector2 dimensions = new Vector2(0.22f, 0.20f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 3)
            {
                offsetY = (-0.05f - dimensions.y) * number;
            }
            if (number > 2 && number < 6)
            {
                offsetX = (0.025f + dimensions.x) * 1;
                offsetY = (-0.05f - dimensions.y) * (number - 3);
            }
            if (number > 5 && number < 9)
            {
                offsetX = (0.025f + dimensions.x) * 2;
                offsetY = (-0.05f - dimensions.y) * (number - 6);
            }
            if (number > 8 && number < 12)
            {
                offsetX = (0.025f + dimensions.x) * 3;
                offsetY = (-0.05f - dimensions.y) * (number - 9);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void CreateSBEntry(ref CuiElementContainer container, string panelName, string color, string name, int kills, string faction, int num)
        {
            var pos = SBNamePos(num);
            UI.CreateLabel(ref container, panelName, UIColors["header"], $"Player Name", 20, "0.01 0.87", "0.12 0.909");
            UI.CreateLabel(ref container, panelName, UIColors["header"], $"Faction", 20, "0.18 0.87", "0.27 0.909");
            UI.CreateLabel(ref container, panelName, UIColors["header"], $"Kills", 20, "0.34 0.87", "0.4 0.909");
            if (num > 50)
            {
                UI.CreateLabel(ref container, panelName, UIColors["header"], $"Player Name", 20, "0.5 0.87", ".6 0.909");
                UI.CreateLabel(ref container, panelName, UIColors["header"], $"Faction", 20, "0.68 0.87", "0.77 0.909");
                UI.CreateLabel(ref container, panelName, UIColors["header"], $"Kills", 20, "0.84 0.87", "0.9 0.909");
            }
            UI.CreateLabel(ref container, panelName, color, $"{name}", 11, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref container, panelName, color, $"{faction}", 11, $"{pos[0] + .09} {pos[1]}", $"{pos[2] + .15} {pos[3]}", TextAnchor.MiddleCenter);
            UI.CreateLabel(ref container, panelName, color, $"{kills}", 11, $"{pos[0] + .25} {pos[1]}", $"{pos[2] + .28} {pos[3]}", TextAnchor.MiddleCenter);

        }

        private float[] SBNamePos(int number)
        {
            Vector2 position = new Vector2(0.03f, 0.84f);
            Vector2 dimensions = new Vector2(0.15f, 0.0205f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 40)
            {
                offsetY = (-0.0001f - dimensions.y) * number;
            }
            if (number > 39 && number < 78)
            {
                offsetX = (0.35f + dimensions.x) * 1;
                offsetY = (-0.0001f - dimensions.y) * (number - 40);
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private void CreateFactionSelectionButton(ref CuiElementContainer container, string panelName, ushort faction, int num, int page)
        {
            string name = factionData.Factions[faction].Name;
            string color = factionData.Factions[faction].UIColor;
            string count = $"Player Count: {factionData.Factions[faction].PlayerCount}";
            string cmd = $"UI_CUI_FactionInfo {faction} {page}";
            var pos = CalcButtonPos(num);
            UI.CreateButton(ref container, panelName, color, $"{name}\n{count}", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private void CreateFactionListButton(ref CuiElementContainer container, string panelName, ushort faction, int num)
        {
            string name = factionData.Factions[faction].Name;
            string color = factionData.Factions[faction].UIColor;
            int count = 0;
            foreach (var entry in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
            {
                count++;
            }
            string cmd = $"UI_CUIFactionList {faction} {count} {0}";
            var pos = CalcButtonPos(num);
            UI.CreateButton(ref container, panelName, color, $"{name}", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private void CreateSpawnIDButton(ref CuiElementContainer container, string panelName, string type, string spawnName, ushort faction, ushort SpawnID, int num)
        {
            string cmd = "";
            if (type == "rally") cmd = $"UI_RallyMovePlayerPosition {SpawnID}";
            if (type == "faction") cmd = $"UI_FactionMovePlayerPosition {SpawnID}";
            string color = factionData.Factions[faction].UIColor;
            var pos = CalcButtonPos(num);
            UI.CreateButton(ref container, panelName, color, $"{spawnName}", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);
        }

        private void CreateSpawnButton(ref CuiElementContainer container, string panelName, string type, string spawnName, ushort faction, ushort SpawnID, int num)
        {
            var pos = SpawnButtonPos(num);
            string cmd = "";
            if (type == "rally") cmd = $"UI_RallyMovePlayerPosition {SpawnID}";
            if (type == "faction") cmd = $"UI_FactionMovePlayerPosition {SpawnID}";
            string color = factionData.Factions[faction].UIColor;
            UI.CreateButton(ref container, panelName, color, $"{spawnName}", 10, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", cmd);

        }

        private float[] SpawnButtonPos(int number)
        {
            Vector2 position = new Vector2(0.0f, 0.8f);
            Vector2 dimensions = new Vector2(0.25f, 0.1f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 4)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 3 && number < 8)
            {
                offsetX = (0.01f + dimensions.x) * (number - 4);
                offsetY = (-0.1f - dimensions.y) * 1;
            }
            if (number > 7 && number < 12)
            {
                offsetX = (0.01f + dimensions.x) * (number - 8);
                offsetY = (-0.1f - dimensions.y) * 2;
            }
            if (number > 11 && number < 16)
            {
                offsetX = (0.01f + dimensions.x) * (number - 12);
                offsetY = (-0.1f - dimensions.y) * 3;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        private Dictionary<string, string> UIColors = new Dictionary<string, string>
        {
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "0 0 0 0.6" },
            {"light", ".85 .85 .85 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"green", "0.28 0.82 0.28 1.0" },
            {"grey", "0.85 0.85 0.85 1.0" },
            {"lightblue", "0.6 0.86 1.0 1.0" },
            {"buttonbg", "0.2 0.2 0.2 0.7" },
            {"buttongreen", "0.133 0.965 0.133 0.9" },
            {"buttonred", "0.964 0.133 0.133 0.9" },
            {"buttongrey", "0.8 0.8 0.8 0.9" }
        };

        void SetFaction(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.1 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.03 0.05", "0.97 0.95", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("FactionSelectionTitle", this), 100, "0.01 0.01", "0.99 0.99");
            var count = factionData.Factions.Count();
            int entriesallowed = 30;
            int remainingentries = count - (page * entriesallowed);
            {
                if (remainingentries > entriesallowed)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_FactionSelection {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_FactionSelection {page - 1}");

            }
            int shownentries = page * entriesallowed;
            int i = 0;
            int n = 0;
            if (!configData.Use_FactionsByInvite)
            {
                foreach (var entry in factionData.Factions)
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= shownentries + entriesallowed)
                    {
                        CreateFactionSelectionButton(ref element, FactionsUIPanel, entry.Key, n, page); n++;
                    }
                }
            }
            else if (configData.Use_FactionsByInvite)
            {
                foreach (var entry in factionData.Factions.Where(kvp => kvp.Value.type == FactionType.FFA))
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= shownentries + entriesallowed)
                    {

                        CreateFactionSelectionButton(ref element, FactionsUIPanel, entry.Key, n, page); n++;
                    }
                }
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], lang.GetMessage("NoJoin", this), 12, "0.8 0.05", "0.87 0.09", "UI_DestroyFS");
            CuiHelper.AddUi(player, element);
        }

        void FactionMenuBar(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelFactionMenuBar);
            var element = UI.CreateElementContainer(PanelFactionMenuBar, UIColors["dark"], "0.1 0.1", "0.205 0.5", true);
            UI.CreatePanel(ref element, PanelFactionMenuBar, UIColors["light"], "0.05 0.03", "0.95 0.97", true);
            
            if (configData.Use_FactionKillIncentives)
            {
                var p = 0;
                    foreach (var entry in playerData.playerFactions.Where(kvp => kvp.Value.faction != 0))
                {
                    if (factionData.Factions[entry.Value.faction].type == FactionType.Regular)
                    p++;
                }
                UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["blue"], lang.GetMessage("ScoreBoard", this), 14, "0.1 0.86", "0.9 0.96", $"UI_CUIScoreBoard {p} {0}");
            }
            UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["green"], lang.GetMessage("FactionLists",this), 14, "0.1 0.73", "0.9 0.83", "UI_CUIFactionLists");
            UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["orange"], lang.GetMessage("PlayerCommands", this), 14, "0.1 0.59", "0.9 0.69", "UI_CUIPlayer");
            if (FactionMemberCheck(player))
            {
                if (isleader(player))
                    UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["lightblue"], lang.GetMessage("LeaderCommands", this), 14, "0.1 0.45", "0.9 0.55", "UI_CUILeader");
            }
            if (isAuth(player))
            {
                UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["red"], lang.GetMessage("AdminCommands", this), 14, "0.1 0.31", "0.9 0.41", "UI_CUIAdmin");
                UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["brown"], lang.GetMessage("Options", this), 14, "0.1 0.17", "0.9 0.27", "UI_CUIOptions");
            }

            UI.CreateButton(ref element, PanelFactionMenuBar, UIColors["buttonred"], lang.GetMessage("Close", this), 16, "0.1 0.04", "0.9 0.14", "UI_DestroyFactionMenu");
            CuiHelper.AddUi(player, element);
        }

        void Instructions(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("Information", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            //CreateInstructionEntry(ref element, FactionsUIPanel, UIColors["orange"], string.Format("V " + Version.ToString() + "--  by " + Author, lang.GetMessage("title", this)), i); i++;
            CreateInstructionEntry(ref element, FactionsUIPanel, "1.0 1.0 1.0 1.0", lang.GetMessage("FactionPlayerInfo", this), i); i++;          
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIPlayer");
            CuiHelper.AddUi(player, element);
        }

        void Leader(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("LeaderCommands", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            var faction = GetPlayerFaction(player);
            if (configData.Use_RallySpawns)
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("SpawnManagement", this), "UI_CUISpawnManager leader", i); i++;
            }
            if (configData.Use_Trades)
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("TradeManagement", this), "UI_CUITrades", i); i++;
            }
            if (configData.Use_Taxes)
            {
                CreateCMDButton(ref element, FactionsUIPanel, faction, $"{lang.GetMessage("TaxBoxMode", this)}", "UI_CUISetTaxBox", i); i++;
                CreateCMDButton(ref element, FactionsUIPanel, faction, $"{lang.GetMessage("RemoveTaxBox", this)}", "UI_CUIRemoveTaxBox", i); i++;
                CreateCMDButton(ref element, FactionsUIPanel, faction, $"{lang.GetMessage("SetTax", this)}", "UI_CUIRequestFactionTax", i); i++;
            }
            if (configData.Use_FactionsByInvite)
            {
                CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("InvitePlayers", this), factionData.Factions[faction].Name), $"UI_CUIFactionInvite {0} {0}", i); i++;
                CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("KickPlayers", this), factionData.Factions[faction].Name), $"UI_CUIKickPlayerMenu {0} {0}", i); i++;

            }
            if (configData.Use_BattleZones)
            {
                if (bZID == 0)
                    if (!BattleZones.ContainsKey(faction))
                        if (CheckForActiveEnemies(faction))
                        {
                            var num = faction + 1;
                            ushort ID = (ushort)num;

                            CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("CreateBZ", this), factionData.Factions[faction].Name), $"UI_CreateZone battle {ID}", i); i++;
                        }
            }
            CuiHelper.AddUi(player, element);
        }

        void BZButton(BasePlayer player, ushort ID)
        {
            CuiHelper.DestroyUi(player, PanelBZButton);
            if (bZID == 0) return;
            var element = UI.CreateElementContainer(PanelBZButton, "0.0 0.0 0.0 0.0", "0.89 0.2", "0.99 0.3", false);
            UI.CreateButton(ref element, PanelBZButton, factionData.Factions[ID].UIColor, string.Format(lang.GetMessage("BZButtonText", this), factionData.Factions[ID].Name), 18, "0 0", "1 1", $"UI_BzConfirmation {ID}");
            CuiHelper.AddUi(player, element);
            return;
        }

        private void BZConfirmation(BasePlayer player, ushort BZID)
        {
            CuiHelper.DestroyUi(player, PanelBZButton);
            var BzFactionOwner = factionData.Factions[BZID].Name;
            var BzLeaderTitle = factionData.Factions[BZID].LeaderTitle;
            if (!BZPlayers.ContainsKey(player.userID))
            {
                var invite = UI.CreateElementContainer(PanelBZButton, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
                UI.CreatePanel(ref invite, PanelBZButton, UIColors["light"], "0.01 0.02", "0.99 0.98");
                UI.CreateLabel(ref invite, PanelBZButton, configData.MSG_Color, string.Format(lang.GetMessage("BZJoinDescription", this), BzFactionOwner, BzLeaderTitle), 18, "0.1 0.1", "0.9 0.89", TextAnchor.MiddleLeft);
                UI.CreateButton(ref invite, PanelBZButton, UIColors["buttongreen"], "Yes", 18, "0.2 0.05", "0.39 0.15", $"UI_BZYes {BZID}");
                UI.CreateButton(ref invite, PanelBZButton, UIColors["buttonred"], "No", 18, "0.4 0.05", "0.59 0.15", $"UI_DestroyBZPanel");
                UI.CreateButton(ref invite, PanelBZButton, UIColors["buttonbg"], "Not Yet", 18, "0.6 0.05", "0.8 0.15", $"UI_BzButton {BZID}");
                CuiHelper.AddUi(player, invite);
            }
        }

        private void KickPlayersMenu(BasePlayer player, ushort factionID = 0, int page = 0)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var count = 0;
            int i = 0;
            int n = 0;
            ushort faction = factionID;
            if (faction == 0)
                faction = GetPlayerFaction(player);
            foreach (var p in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
                count++;
            int entriesallowed = 30;
            int remainingentries = count - (page * entriesallowed);
            int shownentries = page * entriesallowed;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, "1 1 1 0.025", lang.GetMessage("UnassignPlayers", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (remainingentries > entriesallowed)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_CUIKickPlayerMenu {factionID} {page + 1}");
            if (page > 0)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_CUIKickPlayerMenu {factionID} {page - 1}");
            foreach (var entry in FactionKicks.Where(kvp => kvp.Value.factionID == faction).ToList())
            {
                FactionKicks.Remove(entry.Key);
            }
            foreach (var p in playerData.playerFactions)
                if (p.Value.faction == faction)
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= (shownentries + entriesallowed))
                    {
                        var num = faction + i;
                        ushort ID = (ushort)num;
                        if (factionID == 0)
                            CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("KickPlayer", this), p.Value.Name, factionData.Factions[faction].Name), $"UI_ConfirmKickPlayer {ID}", n);
                        else
                            CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("RemovePlayer", this), p.Value.Name, factionData.Factions[faction].Name), $"UI_ConfirmKickPlayer {ID}", n);
                        FactionKicks.Add(ID, new target { playerID = p.Key, factionID = faction, executerID = player.userID });
                        n++;
                    }
                }
            if (factionID == 0)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUILeader");
            else
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIPlayerManager");
            CuiHelper.AddUi(player, element);
        }

        void FactionInviteScreen(BasePlayer player, ushort factionID = 0, int page = 0)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var count = 0;
            int i = 0;
            int n = 0;
            ushort faction = factionID;
            if (faction == 0)
                faction = GetPlayerFaction(player);
            foreach (var p in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
                count++;
            int entriesallowed = 30;
            int remainingentries = count - (page * entriesallowed);
            int shownentries = page * entriesallowed;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("FactionInviteScreen", this), 18, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (remainingentries > entriesallowed)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_CUIFactionInvite {factionID} {page + 1}");
            if (page > 0)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_CUIFactionInvite {factionID} {page - 1}");
            foreach (var entry in FactionInvites.Where(kvp => kvp.Value.factionID == faction).ToList())
            {
                FactionInvites.Remove(entry.Key);
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                if (!FactionMemberCheck(p))
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= (shownentries + entriesallowed))
                    {
                        var num = faction + i;
                        ushort ID = (ushort)num;
                        CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("InvitePlayer", this), p.displayName, factionData.Factions[faction].Name), $"UI_TryInvitePlayerToFaction {ID}", n);
                        if (factionID == 0)
                        { FactionInvites.Add(ID, new target { playerID = p.userID, factionID = faction, executerID = player.userID }); n++; }
                        else
                        { FactionInvites.Add(ID, new target { playerID = p.userID, factionID = faction, executerID = player.userID, assign = true }); n++; }
                    }
                }
            if (factionID == 0)
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUILeader");
            else
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIPlayerManager");
            CuiHelper.AddUi(player, element);
        }

        void FactionListsUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("FactionLists", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            foreach (var entry in factionData.Factions)
            {
                CreateFactionListButton(ref element, FactionsUIPanel, entry.Key, i); i++;
            }
            CuiHelper.AddUi(player, element);
        }


        void Player(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("PlayerCommands", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            //CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("Information", this), "UI_CUIInstructions", i); i++;
            if (FactionMemberCheck(player))
            {
                var faction = GetPlayerFaction(player);
                if (factionData.Factions[faction].type == FactionType.Regular && configData.AllowTradesByPlayer)
                {
                    CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("TradeManagement", this), "UI_PlayerTradeMenu", i); i++;
                }
                if (configData.Use_PersistantSpawns)
                {
                    if (configData.Use_RallySpawns)
                        foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Value.FactionID == faction))
                        {
                            CreateSpawnIDButton(ref element, FactionsUIPanel, "rally", spawn.Value.Name, spawn.Value.FactionID, spawn.Key, i); i++;
                        }
                    if (configData.Use_FactionSpawns)
                        foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Value.FactionID == faction))
                        {
                            CreateSpawnIDButton(ref element, FactionsUIPanel, "faction", spawn.Value.Name, spawn.Value.FactionID, spawn.Key, i); i++;
                        }
                }
                i = 27;
                if (configData.Use_RevoltChallenge && factionData.leader.ContainsKey(playerData.playerFactions[player.userID].faction))
                    if (factionData.leader[playerData.playerFactions[player.userID].faction] != player.userID)
                        CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("ChallengeLeader", this), "UI_CUIChallengeLeader", i); i++;
                if (configData.AllowPlayerToLeaveFactions)
                {
                    CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("LeaveFaction", this), "UI_CUILeaveFaction", i); i++;
                }

            }
            if (!FactionMemberCheck(player))
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("FactionSelection", this), $"UI_FactionSelection {0}", i); i++;
            }
            CuiHelper.AddUi(player, element);
        }

        void Admin(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("AdminCommands", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("FactionManagement", this), "UI_CUIFactionManager", i); i++;
            CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("PlayerManagement", this), "UI_CUIPlayerManager", i); i++;
            if (configData.Use_FactionSpawns)
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("SpawnManagement", this), "UI_CUISpawnManager admin", i); i++;
            }
            if (configData.Use_FactionZones)
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("ZoneManagement", this), "UI_CUIZoneManager admin", i); i++;
            }
            i = 11;
            if (configData.Use_BattleZones)
            {
                foreach (var faction in factionData.Factions)
                {
                    if (BattleZones.ContainsKey(faction.Key) && bZID != 0)
                        CreateCMDButton(ref element, FactionsUIPanel, faction.Key, string.Format(lang.GetMessage("DestroyBZ", this), faction.Value.Name), $"UI_CancelBZ {faction.Key} Cancelled", i); i++;
                }
            }
            if (configData.Use_FactionKillIncentives)
            {
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["red"], "RESET KILL TICKER", 16, "0.85 0.03", "0.975 0.155", "UI_CUIResetTicker");
            }

            CuiHelper.AddUi(player, element);
        }

        void SpawnManager(BasePlayer player, string user)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("SpawnManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (user == "admin")
            {
                foreach (var entry in factionData.Factions)
                {
                    CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("CreateFactionSpawn", this), entry.Value.Name), $"UI_CUISetSpawnPoint faction {entry.Key}", i); i++;
                    foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Value.FactionID == entry.Key))
                    {
                        CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("RemoveFactionSpawn", this), entry.Value.Name, spawn.Value.Name), $"UI_CUIRemoveFactionSpawn {spawn.Key}", i); i++;
                    }
                }
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIAdmin");
            }
            else if (user == "leader")
            {
                var faction = GetPlayerFaction(player);
                    CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("CreateRallySpawn", this), factionData.Factions[faction].Name), "UI_CUISetSpawnPoint rally", i); i++;
                    foreach (var entry in factionData.RallySpawns.Where(kvp => kvp.Value.FactionID == faction))
                    {
                        CreateCMDButton(ref element, FactionsUIPanel, faction, string.Format(lang.GetMessage("RemoveRallySpawn", this), entry.Value.Name), $"UI_CUIRemoveRallySpawn {entry.Key}", i); i++;
                    }
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUILeader");
            }
            CuiHelper.AddUi(player, element);
        }

        void ZoneManagement(BasePlayer player, string user)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("ZoneManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (user == "admin")
            {
                object zoneid = "";
                foreach (var faction in factionData.Factions.Where(kvp => kvp.Value.type == FactionType.Regular))
                {
                    if (!factionData.Factions[faction.Key].FactionZone)
                    {
                        CreateCMDButton(ref element, FactionsUIPanel, faction.Key, string.Format(lang.GetMessage("AddFZ", this), faction.Value.Name), $"UI_CreateZone faction {faction.Key}", i); i++;
                    }
                    else if (factionData.Factions[faction.Key].FactionZone)
                    {
                        CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("RemoveFZ", this), faction.Value.Name), $"UI_EraseZone {faction.Key} faction", i); i++;
                    }
                }
                UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIAdmin");
            }
            CuiHelper.AddUi(player, element);
        }
        

        void FactionManager(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("FactionManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("CreateFaction", this)), "CUI_NewFaction", i); i++;
            //CreateCMDButton(ref element, FactionsUIPanel, 0, "Edit", "Factions","", "UI_CUI_FactionEditor", i); i++;
            if (configData.Use_FactionLeaderByAdmin)
            {
                CreateCMDButton(ref element, FactionsUIPanel, 0, lang.GetMessage("LeaderManagement", this), "UI_CUI_FactionLeaders", i); i++;
            }
            i = 6;
            foreach (var entry in factionData.Factions)
            {
                CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("DeleteFaction", this), entry.Value.Name), $"CUI_TryDeleteFaction {entry.Key}", i); i++;
            } 
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIAdmin");
            CuiHelper.AddUi(player, element);
        }

        void PlayerManager(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            int i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("PlayerManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            foreach (var entry in factionData.Factions)
            {
                CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("AssignPlayer", this), entry.Value.Name), $"UI_CUIFactionInvite {entry.Key} {0}", i); i++;
                CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("UnAssignPlayer", this), entry.Value.Name), $"UI_CUIKickPlayerMenu {entry.Key} {0}", i); i++;
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIAdmin");
            CuiHelper.AddUi(player, element);
        }

        void FactionLeaders(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("LeaderManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            foreach (var entry in factionData.Factions.Where(kvp => kvp.Value.type != FactionType.FFA))
            {
                CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("EditLeader", this), entry.Value.Name, entry.Value.LeaderTitle), $"UI_CUI_LeaderEditing {entry.Key} {0}", i); i++;
            }
            CuiHelper.AddUi(player, element);
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIAdmin");
        }

        void LeaderEditing(BasePlayer player, ushort faction, int page = 0)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var count = 0;
            int i = 0;
            int n = 0;
            foreach (var entry in LeaderPromotes.Where(kvp => kvp.Value.factionID == faction).ToList())
            {
                LeaderPromotes.Remove(entry.Key);
            }
            foreach (var p in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
                count++;
            int entriesallowed = 30;
            int remainingentries = count - (page * entriesallowed);
            int shownentries = page * entriesallowed;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("LeaderManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (!factionData.leader.ContainsKey(faction))
            {
                if (remainingentries > entriesallowed)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_CUI_LeaderEditing {faction} {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_CUI_LeaderEditing {faction} {page - 1}");
            }
            foreach (var p in playerData.playerFactions.Where(kvp => kvp.Value.faction == faction))
            {
                if (factionData.leader.ContainsValue(p.Key))
                {
                    CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("RemoveLeader", this), p.Value.Name, factionData.Factions[faction].LeaderTitle), $"UI_CUIUnassignLeader {faction}", i); i++;
                }

                else if (!factionData.leader.ContainsKey(faction))
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= (shownentries + entriesallowed))
                    {
                        var num = faction + i;
                        ushort ID = (ushort)num;
                        CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("PromoteLeader", this), p.Value.Name, factionData.Factions[faction].LeaderTitle), $"UI_CUIAssignLeader {ID}", n);
                        LeaderPromotes.Add(ID, new target { playerID = p.Key, factionID = faction, executerID = player.userID }); n++;
                    }
                }
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUI_FactionLeaders");
            CuiHelper.AddUi(player, element);
        }

        void FactionEditor(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("FactionEditor", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            foreach (var entry in factionData.Factions)
            {
                CreateCMDButton(ref element, FactionsUIPanel, entry.Key, string.Format(lang.GetMessage("EditFaction", this), entry.Value.Name), $"CUI_EditFaction {entry.Key}", i); i++;
            }
            CuiHelper.AddUi(player, element);
        }

        void PlayerTradeMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = -1;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("PlayerTradeSelection", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            var faction = GetPlayerFaction(player);
            if (factionData.Factions[faction].type == FactionType.FFA) return;
            foreach (Trade trade in Enum.GetValues(typeof(Trade)))
            {
                if (trade != Trade.None)
                    CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("AssignTradeSkill", this), Enum.GetName(typeof(Trade), trade).ToUpper()), $"UI_PlayerTradeAssignment {Enum.GetName(typeof(Trade), trade)}", i); i++;
                
            }
            if (playerData.playerFactions[player.userID].trade != Trade.None)
            {
                Trade trade = playerData.playerFactions[player.userID].trade;
                i = 24; 
                CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("UnassignTradeSkill", this), Enum.GetName(typeof(Trade), trade).ToUpper()), $"UI_PlayerTradeUnassignment", i);
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUIPlayer");
            CuiHelper.AddUi(player, element);
        }

        void TradesManager(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = -1;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("TradeManagement", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            var faction = GetPlayerFaction(player);
            foreach (Trade trade in Enum.GetValues(typeof(Trade)))
            {
                if (trade != Trade.None)
                    CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("TradeSkill", this), Enum.GetName(typeof(Trade), trade)), $"UI_TradeAssignment {Enum.GetName(typeof(Trade), trade)}", i); i++;
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", "UI_CUILeader");
            CuiHelper.AddUi(player, element);
        }

        void TradeOverview(BasePlayer player, Trade trade)
        {
            foreach (var entry in TradeRemoval.Where(kvp => kvp.Value.leaderID == player.userID).ToList())
            {
                TradeRemoval.Remove(entry.Key);
            }
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var c = 0;
            string limitmsg = "";
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], Enum.GetName(typeof(Trade), trade).ToUpper(), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            var faction = GetPlayerFaction(player);
            string names = "";
            foreach (var assigned in playerData.playerFactions)
                if (assigned.Value.faction == faction && assigned.Value.trade == trade)
                {
                    var num = faction + i;
                    ushort ID = (ushort)num;
                    CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("UnassignPlayerTradeSkill", this), Enum.GetName(typeof(Trade), trade).ToUpper(), assigned.Value.Name), $"UI_CUIUnassignTradeSkill {ID}", i);
                    TradeRemoval.Add(ID, new TradeProcessing { playerID = assigned.Key, factionID = faction, leaderID = player.userID, trade = trade }); i++;
                    c++;
                    if (c > 0)
                        names += $"\nPlayers: {assigned.Value.Name}\n";
                }
            if (c >= configData.TradeLimit) limitmsg = " at MAX";
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["dark"], $"Current Trade Count:{c}{limitmsg}{names}", 22, "0.1 0.8", "0.9 0.9", TextAnchor.UpperCenter);
            if (c < configData.TradeLimit)
                CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("AssignTradeSkill", this), Enum.GetName(typeof(Trade), trade).ToUpper()), $"UI_CUITradeAssignmentMenu {trade}", 23);
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", $"UI_CUITrades");
            CuiHelper.AddUi(player, element);
        }

        private void TradeAssignmentMenu(BasePlayer player, Trade trade)
        {
            foreach (var entry in TradeAssignments.Where(kvp => kvp.Value.leaderID == player.userID).ToList())
            {
                TradeAssignments.Remove(entry.Key);
            }
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var i = 0;
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("AssignTrades", this), 100, "0.01 0.01", "0.99 0.99");
            var faction = GetPlayerFaction(player);
            foreach (var unassigned in playerData.playerFactions)
                if (unassigned.Value.faction == faction && unassigned.Value.trade != trade)
                {
                    var num = faction + i;
                    ushort ID = (ushort)num;
                    CreateCMDButton(ref element, FactionsUIPanel, 0, string.Format(lang.GetMessage("AssignPlayerTradeSkill", this), Enum.GetName(typeof(Trade), trade).ToUpper(), unassigned.Value.Name), $"UI_CUIAssignTradeSkill {ID}", i);
                    TradeAssignments.Add(ID, new TradeProcessing { playerID = unassigned.Key, factionID = faction, leaderID = player.userID, trade = trade }); i++;
                }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("Back", this)}", 16, "0.03 0.05", "0.09 0.09", $"UI_TradeAssignment {Enum.GetName(typeof(Trade), trade)}");
            CuiHelper.AddUi(player, element);
        }

        void Options(BasePlayer player)
        {
            var i = 0;
            var color = "";
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("Options", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            if (configData.Use_FactionChatControl == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("ChatControlTitle", this), $"UI_CUIChangeOption UI_Use_FactionChatControl ChatControlInfo ChatControlTitle {configData.Use_FactionChatControl}", i); i++;

            if (configData.Use_FactionNamesonChat == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("NameOnChatTitle", this), $"UI_CUIChangeOption UI_Use_FactionNamesonChat NameOnChatInfo NameOnChatTitle {configData.Use_FactionNamesonChat}", i); i++;

            if (configData.Use_ChatTitles == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("ChatTitlesTitle", this), $"UI_CUIChangeOption UI_Use_ChatTitles ChatTitlesInfo ChatTitlesTitle {configData.Use_ChatTitles}", i); i++;

            if (configData.BroadcastDeath == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("BroadcastDeathTitle", this), $"UI_CUIChangeOption UI_BroadcastDeath BroadcastDeathInfo BroadcastDeathTitle {configData.BroadcastDeath}", i); i++;

            if (configData.Use_FactionsInfo == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionsInfoTitle", this), $"UI_CUIChangeOption UI_Use_FactionsInfo FactionsInfoInfo FactionsInfoTitle {configData.Use_FactionsInfo}", i); i++;

            if (configData.Use_FactionKillIncentives == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionKillIncentivesTitle", this), $"UI_CUIChangeOption UI_Use_FactionKillIncentives FactionKillIncentivesInfo FactionKillIncentivesTitle {configData.Use_FactionKillIncentives}", i); i++;



            if (configData.Use_Kits == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("KitsTitle", this), $"UI_CUIChangeOption UI_Use_Kits KitsInfo KitsTitle {configData.Use_Kits}", i); i++;

            if (configData.Use_Groups == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("OxideGroupsTitle", this), $"UI_CUIChangeOption UI_Use_Groups OxideGroupsInfo OxideGroupsTitle {configData.Use_Groups}", i); i++;

            if (configData.Use_Taxes == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("TaxesTitle", this), $"UI_CUIChangeOption UI_Use_Taxes TaxesInfo TaxesTitle {configData.Use_Taxes}", i); i++;

            if (configData.Use_Trades == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("TradesTitle", this), $"UI_CUIChangeOption UI_Use_Trades TradesInfo TradesTitle {configData.Use_Trades}", i); i++;

            if (configData.Use_Ranks == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("RanksTitle", this), $"UI_CUIChangeOption UI_Use_Ranks RanksInfo RanksTitle {configData.Use_Ranks}", i); i++;

            if (configData.AllowTradesByPlayer == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("AllowTradesByPlayerTitle", this), $"UI_CUIChangeOption UI_AllowTradesByPlayer AllowTradesByPlayerInfo AllowTradesByPlayerTitle {configData.AllowTradesByPlayer}", i); i++;



            if (configData.Use_FactionsByInvite == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionsByInviteTitle", this), $"UI_CUIChangeOption UI_Use_FactionsByInvite FactionsByInviteInfo FactionsByInviteTitle {configData.Use_FactionsByInvite}", i); i++;

            if (configData.AllowPlayerToLeaveFactions == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("AllowPlayerToLeaveFactionsTitle", this), $"UI_CUIChangeOption UI_AllowPlayerToLeaveFactions AllowPlayerToLeaveFactionsInfo AllowPlayerToLeaveFactionsTitle {configData.AllowPlayerToLeaveFactions}", i); i++;           

            if (configData.Use_FactionBalancing == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionBalancingTitle", this), $"UI_CUIChangeOption UI_Use_FactionBalancing FactionBalancingInfo FactionBalancingTitle {configData.Use_FactionBalancing}", i); i++;

            if (configData.Use_RallySpawns == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("RallySpawnsTitle", this), $"UI_CUIChangeOption UI_Use_RallySpawns RallySpawnsInfo RallySpawnsTitle {configData.Use_RallySpawns}", i); i++;

            if (configData.Use_FactionSpawns == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionSpawnsTitle", this), $"UI_CUIChangeOption UI_Use_FactionSpawns FactionSpawnsInfo FactionSpawnsTitle {configData.Use_FactionSpawns}", i); i++;

            if (configData.Use_PersistantSpawns == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("PersistantSpawnsTitle", this), $"UI_CUIChangeOption UI_Use_PersistentSpawns PersistantSpawnsInfo PersistantSpawnsTitle {configData.Use_PersistantSpawns}", i); i++;



            if (configData.FFDisabled == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FFDisabledTitle", this), $"UI_CUIChangeOption UI_FFDisabled FFDisabledInfo FFDisabledTitle {configData.FFDisabled}", i); i++;

            if (configData.BuildingProtectionEnabled == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("BuildingProtectionEnabledTitle", this), $"UI_CUIChangeOption UI_BuildingProtectionEnabled BuildingProtectionEnabledInfo BuildingProtectionEnabledTitle {configData.BuildingProtectionEnabled}", i); i++;

            if (configData.Use_RevoltChallenge == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("RevoltChallengeTitle", this), $"UI_CUIChangeOption UI_Use_RevoltChallenge RevoltChallengeInfo RevoltChallengeTitle {configData.Use_RevoltChallenge}", i); i++;

            if (configData.Use_FactionZones == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionZonesTitle", this), $"UI_CUIChangeOption UI_Use_FactionZones FactionZonesInfo FactionZonesTitle {configData.Use_FactionZones}", i); i++;

            if (configData.Use_BattleZones == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("BattleZonesTitle", this), $"UI_CUIChangeOption UI_Use_BattleZones BattleZonesInfo BattleZonesTitle {configData.Use_BattleZones}", i); i++;

            if (configData.Use_AutoAuthorization == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("AutoAuthorizationTitle", this), $"UI_CUIChangeOption UI_Use_AutoAuthorization AutoAuthorizationInfo AutoAuthorizationTitle {configData.Use_AutoAuthorization}", i); i++;



            if (configData.Use_FactionSafeZones == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionSafeZonesTitle", this), $"UI_CUIChangeOption Use_FactionSafeZones FactionSafeZonesInfo FactionSafeZonesTitle {configData.Use_FactionSafeZones}", i); i++;

            if (configData.Use_FactionLeaderByRank == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionLeaderByRankTitle", this), $"UI_CUIChangeOption UI_Use_FactionLeaderByRank FactionLeaderByRankInfo FactionLeaderByRankTitle {configData.Use_FactionLeaderByRank}", i); i++;

            if (configData.Use_FactionLeaderByTime == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionLeaderByTimeTitle", this), $"UI_CUIChangeOption UI_Use_FactionLeaderByTime FactionLeaderByTimeInfo FactionLeaderByTimeTitle {configData.Use_FactionLeaderByTime}", i); i++;

            if (configData.Use_FactionLeaderByAdmin == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("FactionLeaderByAdminTitle", this), $"UI_CUIChangeOption UI_Use_FactionLeaderByAdmin FactionLeaderByAdminInfo FactionLeaderByAdminTitle {configData.Use_FactionLeaderByAdmin}", i); i++;

            if (configData.Use_EconomicsReward == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("EconomicsRewardTitle", this), $"UI_CUIChangeOption UI_UseEconomics EconomicsRewardInfo EconomicsRewardTitle {configData.Use_TokensReward}", i); i++;

            if (configData.Use_ServerRewardsReward == true) color = UIColors["green"];
            else color = UIColors["red"];
            CreateOptionButton(ref element, FactionsUIPanel, color, lang.GetMessage("ServerRewardsRewardTitle", this), $"UI_CUIChangeOption UI_UseRewards ServerRewardsRewardInfo ServerRewardsRewardTitle {configData.Use_ServerRewardsReward}", i); i++;

            CuiHelper.AddUi(player, element);
        }

        [ConsoleCommand("UI_CUIChangeOption")]
        private void cmdCUIChangeOption(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var cmd = arg.Args[0];
            var verbiage = arg.Args[1];
            var optionName = arg.Args[2];
            var status = arg.Args[3];
            ChangeOption(player, cmd, verbiage, optionName, status);
        }

        void ChangeOption(BasePlayer player, string cmd, string verbiage, string optionName, string status)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            string state = "";
            if (status.ToUpper() == "FALSE") state = lang.GetMessage($"FALSE", this);
            if (status.ToUpper() == "TRUE") state = lang.GetMessage($"TRUE", this);
            string title = string.Format(lang.GetMessage($"OptionChangeTitle", this), lang.GetMessage($"{optionName}", this), state);
            string change = "";
            if (status.ToUpper() == "FALSE") change = lang.GetMessage($"TRUE", this);
            else change = lang.GetMessage($"FALSE", this);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.3 0.3", "0.7 0.7", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], title, 18, "0.03 0.85", "0.97 .95", TextAnchor.UpperCenter);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["dark"], lang.GetMessage($"{verbiage}", this), 18, "0.03 0.27", "0.97 0.83", TextAnchor.UpperLeft);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["dark"], string.Format(lang.GetMessage($"OptionChangeMSG", this), change), 18, "0.2 0.18", "0.8 0.26", TextAnchor.MiddleCenter);
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], "Yes", 18, "0.2 0.05", "0.4 0.15", $"{cmd}");
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], "No", 16, "0.6 0.05", "0.8 0.15", "UI_CUIOptions");
            CuiHelper.AddUi(player, element);
        }

        void ScoreBoardUI(BasePlayer player, int count, int page = 0)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            if (configData.Use_FactionKillIncentives)
            {
                var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
                UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
                UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], lang.GetMessage("ScoreBoard", this), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
                int entriesallowed = 78;
                int remainingentries = count - (page * entriesallowed);
                {
                    if (remainingentries > entriesallowed)
                        UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_CUIScoreBoard {count} {page + 1}");
                    if (page > 0)
                        UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_CUIScoreBoard {count} {page - 1}");

                }
                int shownentries = page * entriesallowed;
                int i = 0;
                int n = 0;
                foreach (var entry in playerData.playerFactions.OrderByDescending(e => e.Value.Kills))
                {
                    if (entry.Value.faction != 0)
                    {
                        if (factionData.Factions[entry.Value.faction].type == FactionType.Regular)
                        {
                            i++;
                            if (i < shownentries + 1) continue;
                            else if (i <= (shownentries + entriesallowed))
                            {
                                var kills = entry.Value.Kills;
                                string pname = entry.Value.Name;
                                string fname = factionData.Factions[entry.Value.faction].Name;
                                var color = factionData.Factions[entry.Value.faction].UIColor;
                                CreateSBEntry(ref element, FactionsUIPanel, color, pname, kills, fname, n); n++;
                            }
                        }
                    }
                }
                CuiHelper.AddUi(player, element);
            }
        }

        void ShowFaction(BasePlayer player, ushort faction, int count, int page = 0)
        {
            var color = factionData.Factions[faction].UIColor;
            var name = factionData.Factions[faction].Name;
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            var element = UI.CreateElementContainer(FactionsUIPanel, UIColors["dark"], "0.21 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, FactionsUIPanel, UIColors["header"], name, 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);
            if (factionData.Factions[faction].type == FactionType.Regular)
                UI.CreateLabel(ref element, FactionsUIPanel, color, "Leader: " + GetLeaderName(faction) + "  Tax: " + GetTax(faction) + "%" + "  Kills: " + GetFactionKills(faction), 22, "0.1 0.9", "0.9 0.98", TextAnchor.MiddleCenter);
            UI.CreateLabel(ref element, FactionsUIPanel, color, lang.GetMessage("MembersGUI", this), 18, "0.4 0.84", "0.6 0.89", TextAnchor.MiddleCenter);
            UI.CreatePanel(ref element, FactionsUIPanel, UIColors["dark"], "0.0 0.83", "0.99 0.839", true);
            int entriesallowed = 12;
            int remainingentries = count - (page * entriesallowed);
            {
                if (remainingentries > entriesallowed)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonbg"], $"{lang.GetMessage("Next", this)}", 18, "0.87 0.03", "0.97 0.085", $"UI_CUIFactionList {faction} {count} {page + 1}");
                if (page > 0)
                    UI.CreateButton(ref element, FactionsUIPanel, UIColors["buttonred"], $"{lang.GetMessage("Back", this)}", 18, "0.73 0.03", "0.83 0.085", $"UI_CUIFactionList {faction} {count} {page - 1}");

            }
            int shownentries = page * entriesallowed;
            int i = 0;
            int n = 0;
            string status = "";
            foreach (var entry in playerData.playerFactions.OrderByDescending(e => e.Value.Name))
            {
                if (BasePlayer.activePlayerList.Contains(BasePlayer.FindByID(entry.Key)))
                    status = "Status: <color=#44ff44>Online</color>";
                else status = "Status: <color=#ff4444>Offline</color>";
                if (entry.Value.faction == faction && factionData.Factions[faction].type == FactionType.Regular)
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= shownentries + entriesallowed)
                    {
                        var level = "";
                        var rank = "";
                        var trade = "";
                        var time = "";
                        string pname = entry.Value.Name;
                        string kills = "";

                        if (configData.Use_Ranks)
                            rank = $"{Enum.GetName(typeof(Rank), entry.Value.rank)}-";
                        if (configData.Use_FactionKillIncentives)
                            kills = "Kills:" + entry.Value.Kills.ToString();
                        if (configData.Use_Trades)
                        {
                            level = "Level:" + entry.Value.level.ToString();
                            trade = $"-{Enum.GetName(typeof(Trade), entry.Value.trade)}\n";
                        }
                        if (configData.Use_FactionLeaderByTime)
                            time = " Time:" + playerData.playerFactions[entry.Key].time.ToString();
                        string info = $"{rank}{pname}\n{level}{trade}{kills}{time}\n{status}";
                        CreatePlayerEntry(ref element, FactionsUIPanel, color, info, n); n++;
                    }
                }
                else if (entry.Value.faction == faction && factionData.Factions[faction].type == FactionType.FFA)
                {
                    i++;
                    if (i < shownentries + 1) continue;
                    else if (i <= shownentries + entriesallowed)
                    {
                        string info = $"{entry.Value.Name}\n{status}";
                        CreatePlayerEntry(ref element, FactionsUIPanel, color, info, n); n++;
                    }
                }
            }
            UI.CreateButton(ref element, FactionsUIPanel, UIColors["orange"], $"{lang.GetMessage("FactionLists", this)}", 16, "0.02 0.03", "0.12 0.085", $"UI_CUIFactionLists");
            CuiHelper.AddUi(player, element);
        }

        void FactionMemberStatus(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelMemberStatus);
            if (FactionMemberCheck(player))
            {
                var faction = GetPlayerFaction(player);
                var element = UI.CreateElementContainer(PanelMemberStatus, "0.0 0.0 0.0 0.0", "0.89 0.4", "0.99 0.8", false);
                UI.CreateLabel(ref element, PanelMemberStatus, "1.0 1.0 1.0 1.0", $"{factionData.Factions[faction].Name} - {lang.GetMessage("PLAYERS", this)}", 12, "0.0 0.91", "1.0 0.99");
                int i = 0;
                List<BasePlayer> list = new List<BasePlayer>();
                Vis.Entities(player.transform.position, 40f, list);
                foreach (var p in list)
                {
                    if (p.userID == player.userID) continue;
                    if (FactionMemberCheck(p))
                        if (playerData.playerFactions[p.userID].faction == faction)
                        {
                            var health = p.health;
                            var name = p.displayName;
                            CreateStatusEntry(ref element, PanelMemberStatus, name, health, i); i++;
                        }
                }
                UI.CreateButton(ref element, PanelMemberStatus, UIColors["buttonred"], $"{lang.GetMessage("Close", this)}", 8, "0.79 0.03", "0.99 0.08", $"DestroyMemberStatus");
                CuiHelper.AddUi(player, element);
            }
        }

        void KillTicker(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelKillTicker);
            var element = UI.CreateElementContainer(PanelKillTicker, "0.0 0.0 0.0 0.0", "0.55 0.87", "0.85 1.0", false);
            UI.CreateLabel(ref element, PanelKillTicker, "1.0 1.0 1.0 1.0", $"{lang.GetMessage("FactionKillTotals", this)} - Goal:{configData.KillLimit}" , 12, "0.0 0.7", "1.0 0.99");
            int i = 0;
            foreach (var entry in factionData.Factions.Where(kvp => kvp.Value.type == FactionType.Regular))
            {
                    CreateTickerEntry(ref element, PanelKillTicker, entry.Key, i); i++;
            }
            CuiHelper.AddUi(player, element);
        }

        void RankAdvancement(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelRankAdvanced);
            var element = UI.CreateElementContainer(PanelRankAdvanced, "0.0 0.0 0.0 0.0", "0.85 0.2", ".99 0.35", false);
            UI.CreateLabel(ref element, PanelRankAdvanced, UIColors["header"], $"{configData.MSG_MainColor}<b>Congratulations!!!</b></color>\n\n You have advanced to\nRank\n{Enum.GetName(typeof(Rank), GetPlayerRank(player))}", 16, "0.0 0.0", "1.0 1.0", TextAnchor.UpperCenter);
            CuiHelper.AddUi(player, element);
            timer.Once(10, () => CuiHelper.DestroyUi(player, PanelRankAdvanced));
        }

        void LevelAdvancement(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelLevelAdvanced);
            var element = UI.CreateElementContainer(PanelLevelAdvanced, "0.0 0.0 0.0 0.0", "0.85 0.2", ".99 0.35", false);
            UI.CreateLabel(ref element, PanelLevelAdvanced, UIColors["header"], $"{configData.MSG_MainColor}<b>Congratulations!!!</b></color>\n\n You have advanced to\nLevel\n{GetPlayerLevel(player)}", 16, "0.0 0.0", "1.0 1.0", TextAnchor.UpperCenter);
            CuiHelper.AddUi(player, element);
            timer.Once(10, () => CuiHelper.DestroyUi(player, PanelLevelAdvanced));
        }

        void SpawnButtons(BasePlayer player, ushort faction)
        {
            var c = 0;
            foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Value.FactionID == faction))
            {
                c++;
            }
            foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Value.FactionID == faction))
            {
                c++;
            }
            if (c < 1) return;
            var i = 0;
            var color = factionData.Factions[faction].UIColor;
            var name = factionData.Factions[faction].Name;
            CuiHelper.DestroyUi(player, PanelSpawnButtons);
            var element = UI.CreateElementContainer(PanelSpawnButtons, "0.0 0.0 0.0 0.0", "0.8 0.2", "1.0 .8", false);
            if (configData.Use_RallySpawns || configData.Use_FactionSpawns)              
                UI.CreateButton(ref element, PanelSpawnButtons, "0.0 0.0 0.0 0.0", "Close", 10, "0.8 0.96", "0.9 1.0", "DestroySpawnButtons");
            if (configData.Use_RallySpawns)
            {
                UI.CreateLabel(ref element, PanelSpawnButtons, "1.0 1.0 1.0 1.0", lang.GetMessage("Rally Spawns", this), 12, "0.0 0.90", "1.0 0.95");
                foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Value.FactionID == faction))
                {
                    CreateSpawnButton(ref element, PanelSpawnButtons, "rally", spawn.Value.Name, spawn.Value.FactionID, spawn.Key, i); i++;
                }
            }
            if (configData.Use_FactionSpawns)
            {
                i = 8;
                UI.CreateLabel(ref element, PanelSpawnButtons, "1.0 1.0 1.0 1.0", lang.GetMessage("Faction Spawns", this), 12, "0.0 0.51", "1.0 0.55");
                foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Value.FactionID == faction))
                {
                    CreateSpawnButton(ref element, PanelSpawnButtons, "faction", spawn.Value.Name, spawn.Value.FactionID, spawn.Key, i); i++;
                }
            }
            CuiHelper.AddUi(player, element);
            return;
        }

        [ConsoleCommand("UI_Use_FactionChatControl")]
        private void cmdChatConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionChatControl == true) configData.Use_FactionChatControl = false;
            else configData.Use_FactionChatControl = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_FFDisabled")]
        private void cmdFFConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.FFDisabled == true) configData.FFDisabled = false;
            else configData.FFDisabled = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_BuildingProtectionEnabled")]
        private void cmdBuildingConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.BuildingProtectionEnabled == true) configData.BuildingProtectionEnabled = false;
            else configData.BuildingProtectionEnabled = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_Trades")]
        private void cmdTradesConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_Trades == true) configData.Use_Trades = false;
            else configData.Use_Trades = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_Taxes")]
        private void cmdTaxesConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_Taxes == true) configData.Use_Taxes = false;
            else configData.Use_Taxes = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_Ranks")]
        private void cmdRanks(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_Ranks == true) configData.Use_Ranks = false;
            else configData.Use_Ranks = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_AllowTradesByPlayer")]
        private void cmdAllowTradesByPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.AllowTradesByPlayer == true) configData.AllowTradesByPlayer = false;
            else
            {
                configData.AllowTradesByPlayer = true;
                configData.Use_Trades = true;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }
        

        [ConsoleCommand("UI_Use_Groups")]
        private void cmdGroups(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_Groups == true) configData.Use_Groups = false;
            else configData.Use_Groups = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionsInfo")]
        private void cmdUse_FactionsInfo(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionsInfo == true) configData.Use_FactionsInfo = false;
            else configData.Use_FactionsInfo = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_Kits")]
        private void cmdUse_Kits(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_Kits == true) configData.Use_Kits = false;
            else configData.Use_Kits = true;
            CUIOptions(player);
            SaveConfig(configData);
        }
        

             [ConsoleCommand("UI_AllowPlayerToLeaveFactions")]
        private void cmdAllowPlayerToLeaveFactions(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.AllowPlayerToLeaveFactions == true) configData.AllowPlayerToLeaveFactions = false;
            else configData.AllowPlayerToLeaveFactions = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionBalancing")]
        private void cmdUse_FactionBalancing(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionBalancing == true) configData.Use_FactionBalancing = false;
            else configData.Use_FactionBalancing = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionNamesonChat")]
        private void cmdUse_FactionNamesonChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionNamesonChat == true) configData.Use_FactionNamesonChat = false;
            else
            {
                configData.Use_FactionNamesonChat = true;
                configData.Use_FactionChatControl = true;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_ChatTitles")]
        private void cmdUse_ChatTitles(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_ChatTitles == true) configData.Use_ChatTitles = false;
            else
            {
                configData.Use_ChatTitles = true;
                configData.Use_FactionChatControl = true;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionLeaderByRank")]
        private void cmdUse_FactionLeaderByRank(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionLeaderByRank == true) configData.Use_FactionLeaderByRank = false;
            else
            {
                configData.Use_FactionLeaderByAdmin = false;
                configData.Use_FactionLeaderByRank = true;
                configData.Use_FactionLeaderByTime = false;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionLeaderByTime")]
        private void cmdUse_FactionLeaderByTime(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionLeaderByTime == true) configData.Use_FactionLeaderByTime = false;
            else
            {
                configData.Use_FactionLeaderByAdmin = false;
                configData.Use_FactionLeaderByRank = false;
                configData.Use_FactionLeaderByTime = true;
                foreach (BasePlayer p in BasePlayer.activePlayerList) InitPlayerTime(p);
                ChangeGlobalTime();
                timer.Once(180, () => SavePlayerFactionTime());
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionLeaderByAdmin")]
        private void cmdUse_FactionLeaderByAdmin(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionLeaderByAdmin == true) configData.Use_FactionLeaderByAdmin = false;
            else
            {
                configData.Use_FactionLeaderByAdmin = true;
                configData.Use_FactionLeaderByRank = false;
                configData.Use_FactionLeaderByTime = false;
            }
                CUIOptions(player);
            SaveConfig(configData);
        }


        [ConsoleCommand("UI_Use_RevoltChallenge")]
        private void cmdUse_RevoltChallengeConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_RevoltChallenge == true) configData.Use_RevoltChallenge = false;
            else configData.Use_RevoltChallenge = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionZones")]
        private void cmdUI_Use_FactionZones(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionZones == true) configData.Use_FactionZones = false;
            else configData.Use_FactionZones = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("Use_FactionSafeZones")]
        private void cmdUse_FactionSafeZones(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionSafeZones == true) configData.Use_FactionSafeZones = false;
            else
            {
                configData.Use_FactionSafeZones = true;
                configData.Use_FactionZones = true;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_BattleZones")]
        private void cmdUI_Use_BattleZones(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_BattleZones == true) configData.Use_BattleZones = false;
            else configData.Use_BattleZones = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_AutoAuthorization")]
        private void cmdUI_Use_AutoAuthorization(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_AutoAuthorization == true) configData.Use_AutoAuthorization = false;
            else configData.Use_AutoAuthorization = true;
            CUIOptions(player);
            SaveConfig(configData);
        }
        


        [ConsoleCommand("UI_Use_RallySpawns")]
        private void cmdUse_RallySpawnsConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_RallySpawns == true) configData.Use_RallySpawns = false;
            else configData.Use_RallySpawns = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionSpawns")]
        private void cmdUse_FactionSpawnsConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionSpawns == true) configData.Use_FactionSpawns = false;
            else configData.Use_FactionSpawns = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_PersistentSpawns")]
        private void cmdUI_Use_PersistentSpawns(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_PersistantSpawns == true) configData.Use_PersistantSpawns = false;
            else
            {
                configData.Use_PersistantSpawns = true;
            }
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionsByInvite")]
        private void cmdUI_Use_FactionsByInvite(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionsByInvite == true) configData.Use_FactionsByInvite = false;
            else configData.Use_FactionsByInvite = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_UseEconomics")]
        private void cmdUseEconomicsConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_EconomicsReward == true) configData.Use_EconomicsReward = false;
            else configData.Use_EconomicsReward = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_UseRewards")]
        private void cmdUseRewardsConfig(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_ServerRewardsReward == true) configData.Use_ServerRewardsReward = false;
            else configData.Use_ServerRewardsReward = true;
            DestroyFS(player);
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_BroadcastDeath")]
        private void cmdBroadcastDeath(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.BroadcastDeath == true) configData.BroadcastDeath = false;
            else configData.BroadcastDeath = true;
            CUIOptions(player);
            SaveConfig(configData);
        }

        [ConsoleCommand("UI_Use_FactionKillIncentives")]
        private void cmdUse_FactionKillIncentives(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (configData.Use_FactionKillIncentives == true) configData.Use_FactionKillIncentives = false;
            else
            {
                configData.Use_FactionKillIncentives = true;
                ResetTicker();
            }
            FactionMenuBar(player);
            CUIOptions(player);
            SaveConfig(configData);
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                RefreshTicker(p);
        }

        //[ConsoleCommand("UI_AskLater")]
        //private void cmdAskLater(ConsoleSystem.Arg arg)
        //{
        //    var player = arg.connection.player as BasePlayer;
        //    if (player == null)
        //        return;
        //    AskLater(player);
        //}

        //private void AskLater(BasePlayer player)
        //{
        //    CuiHelper.DestroyUi(player, FactionsUIPanel);
        //    UnsureWaiting.Add(player.userID);
        //    timer.Once(30 * 60, () => CheckForFactionSelect(player));

        //}

        [ConsoleCommand("UI_CUIFactionLists")]
        private void cmdCUIFactionLists(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIFactionLists(player);
        }

        private void CUIFactionLists(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            FactionListsUI(player);
        }

        [ConsoleCommand("UI_CUIFactionList")]
        private void cmdCUIFactionList(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            ushort FactionID = Convert.ToUInt16(arg.Args[0]);
            if (player == null)
                return;
            int count = Convert.ToInt16(arg.Args[1]);
            int page = Convert.ToInt16(arg.Args[2]);
            CUIFactionList(player, FactionID, count, page);
        }

        private void CUIFactionList(BasePlayer player, ushort faction, int count, int page)
        {
            DestroyFactionsUIPanel(player);
            ShowFaction(player, faction, count, page);
        }

        [ConsoleCommand("UI_CUIScoreBoard")]
        private void cmdCUIScoreBoard(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int count = Convert.ToInt16(arg.Args[0]);
            int page = Convert.ToInt16(arg.Args[1]);
            CUIScoreBoard(player, count, page);
        }

        private void CUIScoreBoard(BasePlayer player, int count, int page )
        {
            DestroyFactionsUIPanel(player);
            ScoreBoardUI(player, count, page);
        }

        [ConsoleCommand("UI_CUIInstructions")]
        private void cmdCUIInstructions(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIInstructions(player);
        }

        private void CUIInstructions(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            Instructions(player);
        }

        [ConsoleCommand("UI_CUILeader")]
        private void cmdCUILeader(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUILeader(player);
        }

        private void CUILeader(BasePlayer player)
        {
            var playerfaction = GetPlayerFaction(player);
            DestroyFactionsUIPanel(player);
            Leader(player);
        }

        [ConsoleCommand("UI_CUIAdmin")]
        private void cmdCUIAdmin(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIAdmin(player);
        }

        private void CUIAdmin(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            Admin(player);
        }

        [ConsoleCommand("UI_CUITrades")]
        private void cmdCUITrades(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUITradesdmin(player);
        }

        private void CUITradesdmin(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            TradesManager(player);
        }

        [ConsoleCommand("UI_CUITradeAssignmentMenu")]
        private void cmdCUITradeAssignmentMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            Trade trade;
            trade = (Trade)Enum.Parse(typeof(Trade), arg.Args[0]);
            CUITradeAssignmentMenu(player, trade);
        }

        private void CUITradeAssignmentMenu(BasePlayer player, Trade trade)
        {
            DestroyFactionsUIPanel(player);
            TradeAssignmentMenu(player, trade);
        }

        [ConsoleCommand("UI_CreateZone")]
        private void cmdUI_CreateZone(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            string type = arg.Args[0];
            ushort ID = Convert.ToUInt16(arg.Args[1]);            
                createZone(player, ID, type);
        }

        [ConsoleCommand("UI_EraseZone")]
        private void cmdUI_EraseZone(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort ID = Convert.ToUInt16(arg.Args[0]);
            string type = arg.Args[1];
            eraseZone(player, ID, type);
        }

        [ConsoleCommand("UI_CancelBZ")]
        private void cmdUI_CancelBZ(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort ID = Convert.ToUInt16(arg.Args[0]);
            string reason = arg.Args[1];
            EndBZ(ID, reason);
            Admin(player);
        }


        [ConsoleCommand("UI_CUISpawnManager")]
        private void cmdCUISpawnManager(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args.Contains("admin"))
                CUISpawnManager(player, "admin");
            if (arg.Args.Contains("leader"))
                CUISpawnManager(player, "leader");
        }

        [ConsoleCommand("UI_CUIZoneManager")]
        private void cmdCUIZoneManager(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args.Contains("admin"))
                CUIZoneManager(player, "admin");
            //if (arg.Args.Contains("leader"))
                //CUISpawnManager(player, "leader");
        }

        private void CUISpawnManager(BasePlayer player, string user)
        {
            DestroyFactionsUIPanel(player);
            SpawnManager(player, user);
        }

        private void CUIZoneManager(BasePlayer player, string user)
        {
            DestroyFactionsUIPanel(player);
            ZoneManagement(player, user);
        }

        [ConsoleCommand("UI_CUIFactionManager")]
        private void cmdCUIFactionManager(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIFactionManager(player);
        }

        private void CUIFactionManager(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            FactionManager(player);
        }

        [ConsoleCommand("UI_CUI_FactionLeaders")]
        private void cmdCUI_FactionLeaders(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUI_FactionLeaders(player);
        }

        private void CUI_FactionLeaders(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            FactionLeaders(player);
        }



        [ConsoleCommand("UI_CUI_FactionEditor")]
        private void cmdCUI_FactionEditor(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null) return;
            CUI_FactionEditor(player);
        }

        private void CUI_FactionEditor(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            FactionEditor(player);
        }



        [ConsoleCommand("UI_CUIPlayer")]
        private void cmdCUIPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIPlayer(player);
        }

        private void CUIPlayer(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            Player(player);
        }



        [ConsoleCommand("UI_FactionSelection")]
        private void cmdUI_FactionSelection(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var page = Convert.ToInt16(arg.Args[0]);
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            DestroyFactionMenu(player);
            SetFaction(player, page);

        }

        [ConsoleCommand("UI_CUIFactionInvite")]
        private void cmdCUIFactionInvite(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var faction = Convert.ToUInt16(arg.Args[0]);
            var page = Convert.ToInt16(arg.Args[1]);
            CUIFactionInvite(player, faction, page);
        }

        private void CUIFactionInvite(BasePlayer player, ushort faction, int page)
        {
            DestroyFactionsUIPanel(player);
            FactionInviteScreen(player, faction, page);
        }


        [ConsoleCommand("UI_CUIKickPlayerMenu")]
        private void cmdUI_CUIKickPlayerMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort faction = 0;
            int page = 0;
            if (arg.Args[0] != null)
            {
                faction = Convert.ToUInt16(arg.Args[0]);
                page = Convert.ToInt16(arg.Args[1]);
            }
            CUIKickPlayerMenu(player, faction, page);
        }

        private void CUIKickPlayerMenu(BasePlayer player, ushort faction, int page)
        {
            DestroyFactionsUIPanel(player);
            KickPlayersMenu(player, faction, page);
        }


        [ConsoleCommand("UI_CUIOptions")]
        private void cmdCUIOptions(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIOptions(player);
        }

        private void CUIOptions(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            Options(player);
        }


        [ConsoleCommand("UI_PlayerTradeUnassignment")]
        private void cmdPlayerTradeUnassignment(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            Trade trade = playerData.playerFactions[player.userID].trade;
            playerData.playerFactions[player.userID].trade = Trade.None;
            SendMSG(player, string.Format(lang.GetMessage("PlayerTradeRemoved", this), Enum.GetName(typeof(Trade), trade)));
            SaveData();
            PlayerTradeMenu(player);
        }

        [ConsoleCommand("UI_CUIUnassignTradeSkill")]
        private void cmdCUIUnassignTradeSkill(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort UID = Convert.ToUInt16(arg.Args[0]);
            var trade = TradeRemoval[UID].trade;
            foreach (var entry in TradeRemoval)
                if (entry.Key == UID)
                {
                    playerData.playerFactions[entry.Value.playerID].trade = Trade.None;
                    try
                    {
                        BasePlayer unassigned = BasePlayer.FindByID(entry.Value.playerID);
                        SendMSG(unassigned, string.Format(lang.GetMessage("PlayerTradeRemoved", this), Enum.GetName(typeof(Trade), entry.Value.trade)));
                        SendMSG(player, string.Format(lang.GetMessage("LeaderTradeRemoved", this), Enum.GetName(typeof(Trade), entry.Value.trade), playerData.playerFactions[entry.Value.playerID].Name));
                    }
                    catch
                    {
                        SendMSG(player, string.Format(lang.GetMessage("LeaderTradeRemoved", this), Enum.GetName(typeof(Trade), entry.Value.trade), playerData.playerFactions[entry.Value.playerID].Name));
                    }
                }
            foreach (var entry in TradeRemoval.Where(kvp => kvp.Value.leaderID == player.userID).ToList())
            {
                TradeRemoval.Remove(entry.Key);
            }
            SaveData();
            TradeOverview(player, trade);
        }

        [ConsoleCommand("UI_CUIAssignTradeSkill")]
        private void cmdCUIAssignTradeSkill(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort UID = Convert.ToUInt16(arg.Args[0]);
            var trade = TradeAssignments[UID].trade;
            foreach (var entry in TradeAssignments)
                if (entry.Key == UID)
                {
                    playerData.playerFactions[entry.Value.playerID].trade = entry.Value.trade;
                    try
                    {
                        BasePlayer assigned = BasePlayer.FindByID(entry.Value.playerID);
                        SendMSG(assigned, string.Format(lang.GetMessage("PlayerTradeSuccess", this), Enum.GetName(typeof(Trade), entry.Value.trade)));
                        SendMSG(player, string.Format(lang.GetMessage("LeaderTradeSuccess", this), playerData.playerFactions[entry.Value.playerID].Name, Enum.GetName(typeof(Trade), entry.Value.trade)));
                    }
                    catch
                    {
                        SendMSG(player, string.Format(lang.GetMessage("LeaderTradeSuccess", this), playerData.playerFactions[entry.Value.playerID].Name, Enum.GetName(typeof(Trade), entry.Value.trade)));
                    }
                }
            foreach (var entry in TradeAssignments.Where(kvp => kvp.Value.leaderID == player.userID).ToList())
            {
                TradeAssignments.Remove(entry.Key);
            }
            SaveData();
            TradeOverview(player, trade);
        }

        [ConsoleCommand("UI_RallyMovePlayerPosition")]
        private void cmdCUIRallyMovePlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort SpawnID = Convert.ToUInt16(arg.Args[0]);
            CUIRallyMovePlayer(player, SpawnID);
        }

        private void CUIRallyMovePlayer(BasePlayer player, ushort SpawnID)
        {
            if (SpawnTimers.Contains(player.userID))
            {
                SendMSG(player, string.Format(lang.GetMessage("ActiveSpawnCooldown", this)));
                return;
            }
            var faction = playerData.playerFactions[player.userID].faction;
            foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Key == SpawnID))
            {
                var x = factionData.RallySpawns[spawn.Key].x;
                var y = factionData.RallySpawns[spawn.Key].y;
                var z = factionData.RallySpawns[spawn.Key].z;
                MovePlayerPosition(player, new Vector3(x, y, z));
                CuiHelper.DestroyUi(player, PanelSpawnButtons);
                DestroyFactionMenu(player);
                SpawnTimer(player, 0);
            }
        }
        

        [ConsoleCommand("UI_FactionMovePlayerPosition")]
        private void cmdCUISpawnMovePlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort SpawnID = Convert.ToUInt16(arg.Args[0]);
            CUISpawnMovePlayer(player, SpawnID);
        }

        private void CUISpawnMovePlayer(BasePlayer player, ushort SpawnID)
        {
            if (SpawnTimers.Contains(player.userID))
            {
                SendMSG(player, string.Format(lang.GetMessage("ActiveSpawnCooldown", this)));
                return;
            }
            var faction = playerData.playerFactions[player.userID].faction;
            foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Key == SpawnID))
            {
                var x = factionData.FactionSpawns[spawn.Key].x;
                var y = factionData.FactionSpawns[spawn.Key].y;
                var z = factionData.FactionSpawns[spawn.Key].z;
                MovePlayerPosition(player, new Vector3(x, y, z));
                CuiHelper.DestroyUi(player, PanelSpawnButtons);
                DestroyFactionMenu(player);
                SpawnTimer(player, 0);
            }
        }

        [ConsoleCommand("UI_CUIRemoveRallySpawn")]
        private void cmdCUIRemoveRallySpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort SpawnID = Convert.ToUInt16(arg.Args[0]);
            CUIRemoveRallySpawn(player, SpawnID);
        }

        private void CUIRemoveRallySpawn(BasePlayer player, ushort SpawnID)
        {
            var playerfaction = GetPlayerFaction(player);
            foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Key == SpawnID))
            {
                factionData.RallySpawns.Remove(spawn.Key);
                BroadcastFaction(player, lang.GetMessage("RallySpawnRemoved", this));
                CUISpawnManager(player, "leader");
            }
            SaveData();
        }

        [ConsoleCommand("UI_CUISetSpawnPoint")]
        private void cmdCUISetSpawnPoint(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFactionMenu(player);
            if (arg.Args.Contains("faction"))
            {
                ushort faction = Convert.ToUInt16(arg.Args[1]);
                var i = 0;
                foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Value.FactionID == faction))
                {
                    i++;
                }

                if (i >= configData.SpawnCountLimit)
                {
                    SendMSG(player, string.Format(lang.GetMessage("ToManySpawns", this), configData.SpawnCountLimit));
                    return;
                }
                else
                {
                    int Number = GetRandomNumber();
                    ushort ID = (ushort)Number;
                    var adminCoords = player.transform.localPosition;

                    SpawnCreation.Add(player.userID, new SpawnDesigner { SpawnID = ID, type = "faction", Entry = new Coords { FactionID = faction, x = adminCoords.x, y = adminCoords.y, z = adminCoords.z } });
                    DestroyUI(player);
                    CreationHelp(player, 9);
                }
            }
            if (arg.Args.Contains("rally"))
            {
                var faction = GetPlayerFaction(player);
                var i = 0;
                foreach (var spawn in factionData.RallySpawns.Where(kvp => kvp.Value.FactionID == faction))
                {
                    i++;
                }

                if (i >= configData.SpawnCountLimit)
                {
                    SendMSG(player, string.Format(lang.GetMessage("ToManySpawns", this), configData.SpawnCountLimit));
                    return;
                }

                else
                {
                    int Number = GetRandomNumber();
                    ushort ID = (ushort)Number;
                    var leaderCoords = player.transform.localPosition;

                    SpawnCreation.Add(player.userID, new SpawnDesigner { SpawnID = ID, type = "rally", Entry = new Coords { FactionID = faction, x = leaderCoords.x, y = leaderCoords.y, z = leaderCoords.z } });
                    DestroyUI(player);
                    CreationHelp(player, 9);
                }
            }
        }


        [ConsoleCommand("UI_TryInvitePlayerToFaction")]
        private void cmdUI_TryInvitePlayerToFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort UID = Convert.ToUInt16(arg.Args[0]);
            foreach (var entry in FactionInvites)
                if (entry.Key == UID)
                {
                    entry.Value.confirm = true;
                    BasePlayer invitee = BasePlayer.FindByID(entry.Value.playerID);
                    CreationHelp(invitee, 11);
                    break;
                }
        }

        [ConsoleCommand("UI_PlayerTradeMenu")]
        private void cmdUI_PlayerTradeMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            PlayerTradeMenu(player);
        }


        [ConsoleCommand("UI_PlayerTradeAssignment")]
        private void cmdPlayerTradeAssignment(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            Trade trade;
            var faction = playerData.playerFactions[player.userID].faction;
            try
            {
                trade = (Trade)Enum.Parse(typeof(Trade), arg.Args[0]);
            }
            catch (Exception)
            {
                return;
            }
            var c = 0;
            foreach (var assigned in playerData.playerFactions)
                if (assigned.Value.faction == faction && assigned.Value.trade == trade)
                {
                    c++;
                }
            if (c >= configData.TradeLimit)
            {
                SendMSG(player, string.Format(lang.GetMessage("PlayerTradeFull", this), Enum.GetName(typeof(Trade), trade)));
                PlayerTradeMenu(player);
                return;
            }
            playerData.playerFactions[player.userID].trade = trade;
            SendMSG(player, string.Format(lang.GetMessage("PlayerTradeSuccess", this), Enum.GetName(typeof(Trade), trade)));
            DestroyFactionMenu(player);
        }

        [ConsoleCommand("UI_TradeAssignment")]
        private void cmdUI_TradeAssignment(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            Trade trade;
            try
            {
                trade = (Trade)Enum.Parse(typeof(Trade), arg.Args[0]);
            }
            catch (Exception)
            {
                return;
            }
            TradeOverview(player, trade);
        }



        [ConsoleCommand("UI_CUILeaveFaction")]
        private void cmdCUILeaveFaction(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var faction = GetPlayerFaction(player);
            var factionname = factionData.Factions[faction].Name;
            if (factionData.leader.ContainsValue(player.userID))
                factionData.leader.Remove(faction);
            BroadcastFaction(player, string.Format(lang.GetMessage("LeftTheFaction", this), player.displayName));
            UnassignPlayerFromFaction(player.userID);
            CUIPlayer(player);
        }


        [ConsoleCommand("UI_ConfirmKickPlayer")]
        private void cmdUI_ConfirmKickPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (arg.Args[0].Contains("yes"))
            {
                foreach (var entry in FactionKicks)
                    if (entry.Value.executerID == player.userID && (entry.Value.confirm))
                    {
                        var factionname = factionData.Factions[entry.Value.factionID].Name;
                        try
                        {
                            UnassignPlayerFromFaction(entry.Value.playerID);
                        }
                        catch
                        {
                            UnassignPlayerFromFaction(entry.Value.playerID);
                        }
                        if (factionData.leader.ContainsValue(entry.Value.playerID))
                            factionData.leader.Remove(entry.Value.factionID);
                        FactionKicks.Remove(entry.Key);
                        SendMSG(player, string.Format(lang.GetMessage("KickSuccessful", this), playerData.playerFactions[entry.Value.playerID].Name, factionname));
                        break;
                    }
                DestroyUI(player);
                return;
            }
            if (arg.Args[0].Contains("no"))
            {
                foreach (var entry in FactionKicks)
                    if (entry.Value.executerID == player.userID && (entry.Value.confirm))
                    {
                        FactionKicks.Remove(entry.Key);
                        SendMSG(player, string.Format(lang.GetMessage("KickCanceled", this)));
                        break;
                    }
                DestroyUI(player);
                return;
            }
            else
            {
                ushort UID = Convert.ToUInt16(arg.Args[0]);
                foreach (var entry in FactionKicks)
                    if (entry.Key == UID)
                    {
                        entry.Value.confirm = true;
                        break;
                    }
                CreationHelp(player, 12);

            }
        }

        

        [ConsoleCommand("UI_BzButton")]
        private void cmdUI_BzButton(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort ID = Convert.ToUInt16(arg.Args[0]);
            BZButton(player, ID);
        }

        [ConsoleCommand("UI_BzConfirmation")]
        private void cmdUI_BzConfirmation(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort ID = Convert.ToUInt16(arg.Args[0]);
            BZConfirmation(player, ID);
        }


        [ConsoleCommand("UI_BZYes")]
        private void cmdUI_BZYes(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyBZ(player);
            ushort ID = Convert.ToUInt16(arg.Args[0]);
            var faction = GetPlayerFaction(player);
            var zone = BattleZones[ID];
            InitializeBZPlayer(player, ID, zone);
            Vector3 newPos = CalculateOutsidePos(player, BattleZones[bZID]);
            MovePlayerPosition(player, newPos);
            PrintToChat($"{factionData.Factions[faction].ChatColor} {player.displayName} {lang.GetMessage("BZJoin", this)}{factionData.Factions[ID].Name}!</color>");
            DestroyUI(player);
        }

        [ConsoleCommand("CUI_AcceptInvite")]
        private void cmdCUI_AcceptInvite(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            foreach (var entry in FactionInvites)
                if (entry.Value.playerID == player.userID && entry.Value.confirm == true)
                {
                    var faction = entry.Value.factionID;
                    AssignPlayerToFaction(player, faction);
                    PrintToChat($"{factionData.Factions[faction].ChatColor} {player.displayName} {lang.GetMessage("Joined", this)} {factionData.Factions[faction].Name}!</color>");
                    FactionInvites.Remove(entry.Key);
                    break;
                }
            DestroyUI(player);
        }

       [ConsoleCommand("CUI_DeclineInvite")]
        private void cmdCUI_DeclineInvite(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            foreach (var entry in FactionInvites)
                if (entry.Value.playerID == player.userID && entry.Value.confirm == true)
                {
                    var faction = entry.Value.factionID;
                    if (entry.Value.assign == false)
                    {
                        BasePlayer factionleader = BasePlayer.FindByID(factionData.leader[faction]);
                        SendMSG(factionleader, string.Format(lang.GetMessage("RejectedInviteToLeader", this), player.displayName));
                    }
                    FactionInvites.Remove(entry.Key);
                    SendMSG(player, string.Format(lang.GetMessage("RejectedInvite", this)));
                }
            DestroyUI(player);
        }

        [ConsoleCommand("CUI_SaveSpawn")]
        private void cmdSaveSpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool isSpawn = false;
                if (SpawnCreation.ContainsKey(player.userID))
                    isSpawn = true;
                SaveSpawn(player, isSpawn);
            }
        }
        [ConsoleCommand("CUI_ExitSpawn")]
        private void cmdExitSpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            if (player.IsAdmin())
            {
                bool isSpawn = false;
                if (SpawnCreation.ContainsKey(player.userID))
                    isSpawn = true;
                ExitSpawnCreation(player, isSpawn);
            }
        }

        private void SaveSpawn(BasePlayer player, bool isSpawn)
        {
            SpawnDesigner Designer;
            Coords spawn;
            if (isSpawn)
            {
                Designer = SpawnCreation[player.userID];
                spawn = Designer.Entry;
                ushort faction = spawn.FactionID;
                if (Designer.type == "faction")
                {
                    factionData.FactionSpawns.Add(Designer.SpawnID, spawn);
                    SpawnCreation.Remove(player.userID);
                    BroadcastFaction(null, lang.GetMessage("NewFactionSpawn", this), faction);
                    SendMSG(player, string.Format(lang.GetMessage("NewSpawnAdmin", this)));
                }

                if (Designer.type == "rally")
                {
                    factionData.RallySpawns.Add(Designer.SpawnID, spawn);
                    SpawnCreation.Remove(player.userID);
                    BroadcastFaction(player, lang.GetMessage("NewRallySpawn", this));
                }
            }
            DestroyUI(player);
            SaveData();
        }
        private void ExitSpawnCreation(BasePlayer player, bool isSpawn)
        {
            if (isSpawn)
            {
                SpawnCreation.Remove(player.userID);

                SendMSG(player, "You have cancelled Spawn creation", "Spawn Designer:");
                DestroyUI(player);
            }
        }

        [ConsoleCommand("UI_CUIRemoveFactionSpawn")]
        private void cmdCUIRemoveFactionSpawn(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort SpawnID = Convert.ToUInt16(arg.Args[0]);
            CUIRemoveFactionSpawn(player, SpawnID);
        }

        private void CUIRemoveFactionSpawn(BasePlayer player, ushort SpawnID)
        {
            foreach (var spawn in factionData.FactionSpawns.Where(kvp => kvp.Key == SpawnID))
            {
                factionData.FactionSpawns.Remove(spawn.Key);
                BroadcastFaction(null, string.Format(lang.GetMessage("FactionSpawnRemoved", this), factionData.Factions[spawn.Value.FactionID].Name, spawn.Value.Name), spawn.Value.FactionID);
                SendMSG(player, string.Format(lang.GetMessage("RemovedSpawnAdmin", this)));
                CUISpawnManager(player, "admin");
            }
            SaveData();
        }

        [ConsoleCommand("UI_CUISetTaxBox")]
        private void cmdCUISetTaxBox(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUISetTaxBox(player);
        }

        private void CUISetTaxBox(BasePlayer player)
        {
            var playerfaction = GetPlayerFaction(player);
            if (!activeBoxes.Contains(playerfaction))
            {
                DestroyFactionMenu(player);
                activeBoxes.Add(playerfaction);
                SendMSG(player, lang.GetMessage("TaxBoxActivated", this));
                return;
            }
            DestroyFactionMenu(player);
            activeBoxes.Remove(playerfaction);
            SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
            return;
        }

        [ConsoleCommand("UI_CUIRemoveTaxBox")]
        private void cmdCUIRemoveTaxBox(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIRemoveTaxBox(player);
        }

        private void CUIRemoveTaxBox(BasePlayer player)
        {
            var playerfaction = GetPlayerFaction(player);
            var leaderCoords = player.transform.localPosition;
            if (factionData.Boxes.ContainsKey(playerfaction))
            {
                factionData.Boxes.Remove(playerfaction);
                BroadcastFaction(player, lang.GetMessage("TaxBoxRemoved", this));
            }
            SaveData();
        }

        [ConsoleCommand("UI_CUIChallengeLeader")]
        private void cmdCUIChallengeLeader(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIChallengeLeader(player);
        }

        private void CUIChallengeLeader(BasePlayer player)
        {
            var playerfaction = playerData.playerFactions[player.userID].faction;
            var factionleaderID = factionData.leader[playerfaction];
            BasePlayer factionleader = BasePlayer.FindByID(factionleaderID);
            if ((!factionData.ActiveChallenges.ContainsKey(player.userID)) || (!factionData.ActiveChallenges.ContainsKey(factionleaderID)))
            {
                if (!BasePlayer.activePlayerList.Contains(factionleader))
                {
                    SendMSG(player, lang.GetMessage("ChallengeFactionLeaderOffline", this));
                    return;
                }
                factionData.ActiveChallenges.Add(player.userID, playerfaction);
                factionData.ActiveChallenges.Add(factionleaderID, playerfaction);
                SendMSG(player, lang.GetMessage("ChallengeActivated", this));
                SendMSG(factionleader, string.Format(lang.GetMessage("ChallengeToLeader", this), player.displayName));
                timer.Once(900, () =>
                {
                    FactionDamage(player);
                    FactionDamage(factionleader);
                });
                return;
            }
            if ((!factionData.ActiveChallenges.ContainsKey(player.userID)) && (factionData.ActiveChallenges.ContainsKey(factionleaderID)))
            {
                foreach (var entry in factionData.ActiveChallenges)
                    if (entry.Value == playerfaction && entry.Key != factionleaderID)
                    {
                        var activechallengeID = entry.Key;
                        BasePlayer activechallengeplayer = BasePlayer.FindByID(activechallengeID);
                        SendMSG(player, string.Format(lang.GetMessage("CurrentChallengePlayer", this), activechallengeplayer.displayName));
                        return;
                    }
            }
        }

        [ConsoleCommand("UI_CUIResetTicker")]
        private void cmdCUIResetTicker(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ResetTicker();
        }

        private void ResetTicker()
        {
            foreach (var entry in factionData.Factions)
                entry.Value.Kills = 0;
            SaveData();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                RefreshTicker(p);
            return;
        }

        private void CheckForFactionSelect(BasePlayer player)
        {
            if (UnsureWaiting.Contains(player.userID)) SetFaction(player);
            UnsureWaiting.Remove(player.userID);
        }

        [ConsoleCommand("UI_DestroyBZPanel")]
        private void cmdDestroyBZPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyBZ(player);
        }

        private void DestroyBZ(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelBZButton);
        }

        [ConsoleCommand("UI_DestroyFS")]
        private void cmdDestroyFS(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFS(player);
        }

        private void DestroyFS(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
        }

        [ConsoleCommand("UI_DestroyTicker")]
        private void cmdDestroyTicker(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyTicker(player);
        }

        private void DestroyTicker(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelKillTicker);
        }

        private void RefreshTicker(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelKillTicker);
            if (configData.Use_FactionKillIncentives)
                KillTicker(player);
        }

        [ConsoleCommand("UI_DestroySP")]
        private void cmdDestroySP(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFactionsUIPanel(player);
        }

        private void DestroyFactionsUIPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
        }

        [ConsoleCommand("UI_DestroyFactionMenu")]
        private void cmdDestroyFM(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFactionMenu(player);
            OpenMap(player);
        }

        private void DestroyFactionMenu(BasePlayer player)
        {
            DestroyFactionsUIPanel(player);
            CuiHelper.DestroyUi(player, PanelFactionMenuBar);
        }

        [ConsoleCommand("UI_OpenFactions")]
        private void cmdOpenFactions(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            OpenFactions(player);
        }

        private void OpenFactions(BasePlayer player)
        {
            if (MenuState.Contains(player.userID))
            {
                DestroyFactionMenu(player);
                MenuState.Remove(player.userID);
            }
            else
            {
                MenuState.Add(player.userID);
                CloseMap(player);
                FactionMenuBar(player);
                Player(player);
            }

        }

        [ConsoleCommand("UI_CUIRequestFactionTax")]
        private void cmdCUIRequestFactionTax(ConsoleSystem.Arg arg)
        {
        var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            string msg = lang.GetMessage("TaxRequestMessage", this);
            string cmd = $"UI_CUISetFactionTax";
            NumberPad(player, msg, cmd);
        }

        [ConsoleCommand("UI_CUISetFactionTax")]
        private void cmdCUISetFactionTax(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyFactionsUIPanel(player);
            int tax = Convert.ToInt16(arg.Args[0]);
            var faction = playerData.playerFactions[player.userID].faction;
            factionData.Factions[faction].tax = tax;
            SendMSG(player, string.Format(lang.GetMessage("NewTax", this), tax));
            Leader(player);
        }

        [ConsoleCommand("DestroyAll")]
        private void cmdDestroyAll(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyAll(player);
        }

        [ConsoleCommand("UI_OpenMemberStatus")]
        private void cmdOpenMemberStatus(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            FactionMemberStatus(player);
            OpenMemberStatus.Add(player.userID);
        }

        [ConsoleCommand("DestroyMemberStatus")]
        private void cmdDestroyMemberStatus(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, PanelMemberStatus);
            OpenMemberStatus.Remove(player.userID);
        }


        private void DestroyAll(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            CuiHelper.DestroyUi(player, PanelKillTicker);
            CuiHelper.DestroyUi(player, PanelLevelAdvanced);
            CuiHelper.DestroyUi(player, PanelRankAdvanced);
            CuiHelper.DestroyUi(player, PanelSpawnButtons);
            CuiHelper.DestroyUi(player, FactionsUIPanel);
            CuiHelper.DestroyUi(player, PanelBZButton);
            CuiHelper.DestroyUi(player, PanelMemberStatus); 
            DestroyFactionMenu(player);
        }

        [ConsoleCommand("DestroySpawnButtons")]
        private void cmdDestroySpawnButtons(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroySpawnButtons(player);
        }

        private void DestroySpawnButtons(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelSpawnButtons);
        }

        

        #endregion

        #region Console Commands

        [ConsoleCommand("faction.unassign")]
        private void cmdFactionUnAssign(ConsoleSystem.Arg arg)
        {
            if (!isAuthCon(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, string.Format(lang.GetMessage("FactionUnassignFormating", this)));
                return;
            }
            if (arg.Args.Length > 1)
            {
                SendReply(arg, string.Format(lang.GetMessage("FactionUnassignFormating", this)));
                return;
            }
            if (arg.Args.Length == 1)
            {
                var partialPlayerName = arg.Args[0];
                var foundPlayers = FindPlayer(partialPlayerName);
                if (foundPlayers.Count == 0)
                {
                    SendReply(arg, string.Format(lang.GetMessage("NoPlayers", this)));
                    return;
                }
                if (foundPlayers.Count > 1)
                {
                    SendReply(arg, string.Format(lang.GetMessage("multiPlayers", this)));
                    return;
                }
                if (!FactionMemberCheck(foundPlayers[0]))
                {
                    SendReply(arg, string.Format(lang.GetMessage("TargetNotInFaction", this), foundPlayers[0].displayName, factionData.Factions[GetPlayerFaction(foundPlayers[0])].Name));
                    return;
                }

                if (foundPlayers[0] != null)
                {
                    UnassignPlayerFromFaction(foundPlayers[0].userID);
                    SendReply(arg, string.Format(lang.GetMessage("UnassignSuccess", this), foundPlayers[0].displayName, factionData.Factions[GetPlayerFaction(foundPlayers[0])].Name));
                    SendMSG(foundPlayers[0], string.Format(lang.GetMessage("RemovedFromFaction", this), factionData.Factions[GetPlayerFaction(foundPlayers[0])].Name));
                    timer.Once(5, () =>
                    {
                        SetFaction(foundPlayers[0]);
                    });
                }
                else SendReply(arg, string.Format(lang.GetMessage("UnassignError", this)));
            }
        }

        #endregion

        #region Chat Commands
        ///FactionChat		
        [ChatCommand("fc")]
        private void cmdfactionchat(BasePlayer player, string command, string[] args)
        {
            if (!FactionMemberCheck(player))
            {
                SendMSG(player, lang.GetMessage("NoFactionError", this, player.UserIDString));
                return;
            }
            var faction = (GetPlayerFaction(player));
            var message = string.Join(" ", args);
            if (string.IsNullOrEmpty(message))
                return;
            if (isleader(player))
            {
                BroadcastFaction(player, $"{factionData.Factions[faction].ChatColor}[LEADER]{player.displayName}</color>: " + message);
            }
            else
            {
                BroadcastFaction(player, $"{factionData.Factions[faction].ChatColor}{player.displayName}</color>: " + message);
            }
        }

        [ChatCommand("faction")]
        private void cmdfaction(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                CloseMap(player);
                FactionMenuBar(player);
                Player(player);
                return;
            }
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }

        bool isAuthCon(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (arg.connection.authLevel < 1)
                {
                    SendMSG(player, lang.GetMessage("NotAuth", this));
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region UI Commands

        [ConsoleCommand("UI_CUI_FactionInfo")]
        private void cmdCUI_FactionInfo(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort faction = Convert.ToUInt16(arg.Args[0]);
            var page = Convert.ToInt16(arg.Args[1]);
            FactionInfo(player, faction, page);
        }

        [ConsoleCommand("CUI_FactionSelection")]
        private void cmdFactionSelection(ConsoleSystem.Arg arg)
        {

            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ushort faction = Convert.ToUInt16(arg.Args[0]);
            if (configData.Use_FactionBalancing)
            {
                var max = GetMax();
                var min = GetMin();
                int diff = max - min;
                var selectioncount = factionData.Factions[faction].PlayerCount;
                if (min != selectioncount && diff > configData.AllowedFactionDifference)
                {
                    SendMSG(player, string.Format(lang.GetMessage("FactionToFull", this, player.UserIDString), factionData.Factions[faction].Name));
                    return;
                }
            }
            DestroyFS(player);
            AssignPlayerToFaction(player, faction);
            PrintToChat($"{factionData.Factions[faction].ChatColor} {player.displayName} {lang.GetMessage("Joined", this)} {factionData.Factions[faction].Name}!</color>");

            if (UnsureWaiting.Contains(player.userID)) UnsureWaiting.Remove(player.userID);
        }

        private int GetMax()
        {
            KeyValuePair<ushort, Faction> max = factionData.Factions.First();
            foreach (KeyValuePair<ushort, Faction> count in factionData.Factions)
            {
                if (count.Value.PlayerCount > max.Value.PlayerCount) max = count;
            }
            int num = max.Value.PlayerCount;
            return num;
        }

        private int GetMin()
        {
            KeyValuePair<ushort, Faction> max = factionData.Factions.First();
            foreach (KeyValuePair<ushort, Faction> count in factionData.Factions)
            {
                if (count.Value.PlayerCount < max.Value.PlayerCount) max = count;
            }
            int num = max.Value.PlayerCount;
            return num;
        }

        [ConsoleCommand("UI_CUIPlayerManager")]
        private void cmdUI_CUIPlayerManager(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CUIPlayerManager(player);
        }

        private void CUIPlayerManager(BasePlayer player)
        {
           DestroyFactionsUIPanel(player);
            PlayerManager(player);
        }

        #endregion

        #region AdminOptions



            #endregion

        #region Faction Management

        enum Rank
        {
            None,
            Recruit,
            Apprentice,
            Novice,
            Advanced,
            Expert,
            SharpShooter
        }
        enum Trade
        {
            None,
            Lumberjack,
            Miner,
            Hunter,
            Crafter,
            Forager
        }
        enum FactionType
        {
            Regular,
            FFA
        }
        private List<BasePlayer> FindPlayer(string arg)
        {
            var foundPlayers = new List<BasePlayer>();
            ulong steamid;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            foreach (var p in BasePlayer.activePlayerList)
                if (p != null)
                {
                    if (steamid != 0L)
                        if (p.userID == steamid)
                        {
                            foundPlayers.Add(p);
                            return foundPlayers;
                        }
                    string lowername = p.displayName.ToLower();
                    if (lowername.Contains(lowerarg))
                        foundPlayers.Add(p);
                }
            return foundPlayers;
        }

        private void AssignPlayerToFaction(BasePlayer player, ushort faction)
        {
            var ID = player.userID;
            var p = playerData.playerFactions;
            var fl = factionData.leader;
            var factionname = factionData.Factions[faction].Name;

            if (FactionMemberCheck(player))
                if (p[ID].faction == faction)
                    return;

            if (!p.ContainsKey(ID))
            {
                p.Add(ID, new FactionPlayerData { faction = faction, Name = $"{player.displayName}", trade = Trade.None, level = 1, FactionMemberTime = 0, rank = Rank.Recruit });
                factionData.Factions[faction].PlayerCount += 1;
            }
            else
            {
                p[ID].faction = faction;
                p[ID].Name = $"{player.displayName}";
                p[ID].FactionMemberTime = 0;
                p[ID].trade = Trade.None;
                p[ID].level = 1;
                p[ID].rank = Rank.Recruit;
                factionData.Factions[faction].PlayerCount += 1;
            }
            if (fl.ContainsValue(ID))
            {
                foreach (var entry in fl.Where(kvp => kvp.Value == ID).ToList())
                {
                    fl.Remove(entry.Key);
                }
            }
            if (configData.Use_Groups)
            {
                foreach (var entry in factionData.Factions) { ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {factionData.Factions[entry.Key].group}"); }
                ConsoleSystem.Run.Server.Normal($"usergroup add {player.userID} {factionData.Factions[faction].group}");
            }
            if (configData.Use_Kits) GiveFactionKit(player, faction);
            if (configData.AllowTradesByPlayer)
                PlayerTradeMenu(player);
            AuthorizePlayerOnTurrets(player);
            InitPlayerTime(player);
            CheckLeaderTime();
            SaveData();
        }

        private void UnassignPlayerFromFaction(ulong playerID)
        {
            var ID = playerID;
            var p = playerData.playerFactions;
            var fl = factionData.leader;
            var oldFaction = p[ID].faction;
            var oldfactionname = factionData.Factions[oldFaction].Name;
            p[ID].faction = default(ushort);
            p[ID].FactionMemberTime = 0;
            p[ID].trade = Trade.None;
            p[ID].level = 1;
            p[ID].rank = Rank.Recruit;
            p[ID].ChallengeStatus = false;

            factionData.Factions[oldFaction].PlayerCount -= 1;
            if (factionData.Factions[oldFaction].PlayerCount < 0)
                factionData.Factions[oldFaction].PlayerCount = 0;

            if (fl.ContainsValue(ID))
            {
                foreach (var entry in fl.Where(kvp => kvp.Value == ID).ToList())
                {
                    fl.Remove(entry.Key);
                }
            }
            try
            {
                BasePlayer player = BasePlayer.FindByID(playerID);
                SendMSG(player, string.Format(lang.GetMessage("RemovedFromFaction", this), oldfactionname));
            }
            catch
            {

            }

            CheckLeaderTime();
            SaveData();
        }

        private void CheckLeaderTime()
        {
            if (!configData.Use_FactionLeaderByTime) return;

            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (!FactionMemberCheck(p)) continue;
                var ID = p.userID;
                var pfaction = playerData.playerFactions[ID].faction;
                if (factionData.Factions[pfaction].type == FactionType.FFA) continue;
                var ptime = playerData.playerFactions[ID].FactionMemberTime;
                ulong CurrentLeaderID = 0L;

                if (!factionData.leader.ContainsKey(pfaction))
                {
                    factionData.leader.Add(pfaction, ID); BroadcastFaction(p, $"{factionData.Factions[pfaction].Name} + {lang.GetMessage("NewLeader", this)}");
                    factionData.Factions[pfaction].tax = 0;
                    if (factionData.Boxes.ContainsKey(pfaction)) factionData.Boxes.Remove(pfaction);
                }
                factionData.leader.TryGetValue(pfaction, out CurrentLeaderID);
                if (CurrentLeaderID == ID) continue;
                var CurrentLeadertime = playerData.playerFactions[CurrentLeaderID].FactionMemberTime;
                if (CurrentLeadertime < ptime)
                {
                    BasePlayer leader = BasePlayer.FindByID(CurrentLeaderID);
                    factionData.leader.Remove(pfaction);
                    factionData.leader.Add(pfaction, ID);
                    factionData.Factions[pfaction].tax = 0;
                    if (factionData.Boxes.ContainsKey(pfaction)) factionData.Boxes.Remove(pfaction);
                    if (BasePlayer.activePlayerList.Contains(leader)) SendMSG(leader, string.Format(lang.GetMessage("RemovedAsLeader", this), factionData.Factions[pfaction].Name));
                    BroadcastFaction(p, $"{factionData.Factions[pfaction].Name} + {lang.GetMessage("NewLeader", this)}");
                }
            }
            SaveData();
            timer.Once(1800, () => CheckLeaderTime());
        }

        private void CheckLeaderRank(BasePlayer player)
        {
            if (!configData.Use_FactionLeaderByRank) return;
            if (!FactionMemberCheck(player)) return;
            var pfaction = GetPlayerFaction(player);
            if (factionData.Factions[pfaction].type == FactionType.FFA) return ;
            int leaderrank = 0;
            int playerrank = Convert.ToInt32(playerData.playerFactions[player.userID].rank);
            if (factionData.leader.ContainsKey(pfaction))
            {
                var factionleader = factionData.leader[pfaction];
                if (factionleader == player.userID) return;
                leaderrank = Convert.ToInt32(playerData.playerFactions[factionleader].rank);
            }
            if (leaderrank < playerrank)
            {
                factionData.leader[pfaction] = player.userID;
                factionData.Factions[pfaction].tax = 0;
                if (factionData.Boxes.ContainsKey(pfaction)) factionData.Boxes.Remove(pfaction);
                if (factionData.ActiveChallenges.ContainsValue(pfaction))
                {
                    foreach (var entry in factionData.ActiveChallenges.Where(kvp => kvp.Value == pfaction).ToList())
                    {
                        factionData.ActiveChallenges.Remove(entry.Key);
                        var Challenger = BasePlayer.FindByID(entry.Key);
                        BroadcastFaction(player, lang.GetMessage("LeaderChangedChallengeDone", this));

                    }
                }
                BroadcastFaction(player, $"{player.displayName} is the new Leader!");
                SaveData();
            }
        }

        private void FactionDamage(BasePlayer player)
        {
            var playerfaction = GetPlayerFaction(player);
            var factionleaderID = factionData.leader[playerfaction];
            BasePlayer factionleader = BasePlayer.FindByID(factionleaderID);
            if (!playerData.playerFactions[player.userID].ChallengeStatus)
            {
                playerData.playerFactions[player.userID].ChallengeStatus = true;
                if (factionData.leader.ContainsValue(player.userID))
                {
                    SendMSG(player, lang.GetMessage("FactionDamageEnabledLeader", this));
                    return;
                }
                else SendMSG(player, string.Format(lang.GetMessage("FactionDamageEnabledChallenger", this), factionleader.displayName));
                return;
            }
            if (playerData.playerFactions[player.userID].ChallengeStatus)
            {
                playerData.playerFactions[player.userID].ChallengeStatus = false;
                SendMSG(player, lang.GetMessage("FactionDamageDisabled", this));
            }
        }

        private int Count(ushort faction)
        {
            int count = 0;
            foreach (var player in playerData.playerFactions)
            {
                if (playerData.playerFactions[player.Key].faction == faction) count++;
            }
            return count;
        }

        #endregion

        #region Externally Called Functions

        string GetPlayerFactionEx(ulong playerID)

        {
            foreach (var entry in playerData.playerFactions)
                if (entry.Key == playerID)
                    return entry.Value.faction.ToString();
            return null;
        }

        bool CheckSameFaction(ulong player1ID, ulong player2ID)
        {
            if (ActiveCSZone)
            {
                if (isCSPlayer(BasePlayer.FindByID(player1ID)) || isCSPlayer(BasePlayer.FindByID(player2ID))) return false;
            }
            if (!playerData.playerFactions.ContainsKey(player1ID) || !playerData.playerFactions.ContainsKey(player2ID)) return false;
            var player1faction = playerData.playerFactions[player1ID].faction;
            var player2faction = playerData.playerFactions[player2ID].faction;
            if (player1faction == default(ushort) || player2faction == default(ushort)) return false;
            if (factionData.Factions[player1faction].type == FactionType.FFA) return false;
            if (factionData.Factions[player2faction].type == FactionType.FFA) return false;
            if (player1faction == player2faction)
            {
                foreach (var entry in factionData.Factions)
                {
                    if (entry.Key == player1faction)
                        return true;
                }
                return false;
            }
            else return false;

        }
        bool isCSPlayer(BasePlayer player)
        {
            try
            {
                bool result = (bool)ActiveCSZone?.Call("isCSPlayer", player);
                return result;
            }
            catch
            {
                return false;
            }
        }

        private object GetKits() => Kits?.Call("GetAllKits");

        public string[] GetKitNames()
        {
            var kits = GetKits();
            if (kits != null)
            {
                if (kits is string[])
                {
                    var array = kits as string[];
                    return array;
                }
            }
            return null;
        }

        private void CloseMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("disableMap", player);
            }
        }
        private void OpenMap(BasePlayer player)
        {
            if (LustyMap)
            {
                LustyMap.Call("enableMap", player);
            }
        }

        #endregion

        #region Giving Items
        private void GiveFactionKit(BasePlayer player, ushort faction)
        {
            if (!configData.Use_Kits) return;
            object isKit = Kits?.Call("isKit", new object[] { factionData.Factions[faction].kit });
            if (isKit is bool)
                if ((bool)isKit)
                {
                    Kits?.Call("GiveKit", player, factionData.Factions[faction].kit);
                    return;
                }
            Puts($"{factionData.Factions[faction].kit} {lang.GetMessage("NotAValidKit", this)}");
        }
        #endregion

        #region Classes

        class FactionSavedPlayerData
        {
            public Dictionary<ulong, FactionPlayerData> playerFactions = new Dictionary<ulong, FactionPlayerData>();
        }
        class FactionPlayerData
        {
            public ushort faction;
            public string Name;
            public Rank rank;
            public int level;
            public Trade trade;
            public long LastConnection;
            public int FactionMemberTime;
            public bool ChallengeStatus;
            public int time;
            public int FactionBuildings;
            public int TotalBuildings;
            public int Kills = 0;
            public int Gathered;
            public int Crafted;
            public List<EquipmentKits> AvailableKits { get; set; }
            public string LastKit;
            public ushort LastFaction;
            public bool SavedInventory;
            public List<AutoTurret> factionTurrets {get; set;}
        }

        class PlayerSavedInventories
        {
            public Dictionary<ulong, List<PlayerInv>> PlayerInventory = new Dictionary<ulong, List<PlayerInv>>();
            public Dictionary<ulong, List<PlayerInv>> PreviousInventory = new Dictionary<ulong, List<PlayerInv>>();
        }

        class FactionStatistics
        {
            public Dictionary<ushort, Faction> Factions = new Dictionary<ushort, Faction>();
            public Dictionary<ushort, ulong> leader = new Dictionary<ushort, ulong>();
            public Dictionary<ushort, Coords> Boxes = new Dictionary<ushort, Coords>();
            public Dictionary<ushort, Coords> FactionSpawns = new Dictionary<ushort, Coords>();
            public Dictionary<ushort, Coords> RallySpawns = new Dictionary<ushort, Coords>();
            public Dictionary<ulong, ushort> ActiveChallenges = new Dictionary<ulong, ushort>();
            public Dictionary<ushort, int> Buildings = new Dictionary<ushort, int>();
        }

        class Faction
        {
            public string Name;
            public string FactionKit;
            public string UIColor;
            public string ChatColor;
            public string LeaderTitle;
            public int Kills;
            public int PlayerCount;
            public double tax;
            public string group;
            public string kit;
            public FactionType type;
            public bool FactionZone;
        }

        class target
        {
            public ulong playerID;
            public ulong executerID;
            public ushort factionID;
            public bool confirm;
            public bool assign;

        }

        class EquipmentKits
        {
            public ushort KitID;
            public string KitName;
            public ushort KitLevel;
        }

        class TradeProcessing
        {
            public ushort factionID;
            public ulong playerID;
            public ulong leaderID;
            public Trade trade;
        }

        class Monuments
        {
            public Vector3 position;
            public float radius;
        }

        class Inventory
        {
            List<PlayerInv> InvItems = new List<PlayerInv>();
        }

        class PlayerInv
        {
            public int itemid;
            public int skin;
            public string container;
            public int amount;
            public float condition;
            public int ammo;
            public PlayerInv[] InvContents;
        }

        class PlayerCond
        {
            public float health;
            public float calories;
            public float hydration;
        }
        
        class BattleZonePlayer
        {
            public bool oob;
            public ushort faction;
            public string bz;
            public bool entered;
            public bool died;
            public bool owner;
        }

        class FactionDesigner
        {
            public ushort ID;
            public Faction Entry;
            public Faction OldEntry;
            public int partNum = 0;
        }

        class SpawnDesigner
        {
            public ushort SpawnID;
            public string type;
            public Coords Entry;
            public Coords OldEntry;
            public int partNum = 0;
        }

        class FactionColors
        {
            public string Color;
            public string ChatColor;
            public string UIColor;
        }

        class Coords
        {
            public ushort FactionID;
            public string Name;
            public float x;
            public float y;
            public float z;
        }

        class Gear
        {
            public string shortname;
            public int skin;
            public int amount;
        }

        #endregion

        #region Data Management

        private List<FactionColors> Colors = new List<FactionColors>
                {
                    new FactionColors
                    {
                        Color = "red",
                        ChatColor = "<color=#e60000>",
                        UIColor = "0.91 0.0 0.0 1.0"
                    },

                    new FactionColors
                    {
                        Color = "blue",
                        ChatColor = "<color=#3366ff>",
                        UIColor = "0.2 0.4 1.0 1.0"
                    },

                    new FactionColors
                    {
                        Color = "green",
                        ChatColor = "<color=#29a329>",
                        UIColor = "0.16 0.63 0.16 1.0"
                    },

                    new FactionColors
                    {
                        Color = "yellow",
                        ChatColor = "<color=#ffff00>",
                        UIColor = "1.0 1.0 0.0 1.0"
                    },

                    new FactionColors
                    {
                        Color = "orange",
                        ChatColor = "<color=#ff9933>",
                        UIColor = "1.0 0.51 0.2 1.0"
                    },

                    new FactionColors
                    {
                        Color = "purple",
                        ChatColor = "<color=#7300e6>",
                        UIColor = "0.45 0.0 0.9 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkred",
                        ChatColor = "<color=#A93226>",
                        UIColor = "0.66 0.19 0.15 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkpurple",
                        ChatColor = "<color=#4A235A>",
                        UIColor = "0.29 0.14 0.35 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkblue",
                        ChatColor = "<color=#1F618D>",
                        UIColor = "0.12 0.38 0.55 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkgreen",
                        ChatColor = "<color=#117864>",
                        UIColor = "0.06 0.47 0.39 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkorange",
                        ChatColor = "<color=#D35400>",
                        UIColor = "0.83 0.33 0.0 1.0"
                    },
                    new FactionColors
                    {
                        Color = "darkgrey",
                        ChatColor = "<color=#626567>",
                        UIColor = "0.38 0.39 0.4 1.0"
                    },
                    new FactionColors
                    {
                        Color = "bluegrey",
                        ChatColor = "<color=#34495E>",
                        UIColor = "0.20 0.28 0.37 1.0"
                    },
                    new FactionColors
                    {
                        Color = "hotpink",
                        ChatColor = "<color=#FA0091>",
                        UIColor = "1.0 0.0 0.57 1.0"
                    },
                    new FactionColors
                    {
                        Color = "limegreen",
                        ChatColor = "<color=#3EFF00>",
                        UIColor = "0.24 1.0 0.0 1.0"
                    },
                    new FactionColors
                    {
                        Color = "teal",
                        ChatColor = "<color=#00E0B8>",
                        UIColor = "0.0 0.88 0.72 1.0"
                    }
                };

        private Dictionary<ushort, Faction> defaultFactions = new Dictionary<ushort, Faction>
                {
                    {1254, new Faction
                    {
                    Name = "Faction A",
                        LeaderTitle = "Leader",
                        ChatColor = "<color=#7300e6>",
                        UIColor = "0.45 0.0 0.9 1.0",
                        type = FactionType.Regular
                    }
                    },

                    { 1241, new Faction
                    {
                    Name = "Faction B",
                        LeaderTitle = "Leader",
                        ChatColor = "<color=#ff9933>",
                        UIColor = "1.0 0.51 0.2 1.0",
                        type = FactionType.Regular
                    }
                    },

                    { 1287, new Faction
                    {
                    Name = "Faction C",
                        LeaderTitle = "Leader",
                        ChatColor = "<color=#29a329>",
                        UIColor = "0.16 0.63 0.16 1.0",
                        type = FactionType.Regular
                    }
                    },

                { 2872, new Faction
                    {
                    Name = "Rebels",
                        LeaderTitle = "None",
                        ChatColor = "<color=#85adad>",
                        UIColor = "0.52 0.68 0.68 1.0",
                        type = FactionType.FFA
                    }

            }
        };


        private Dictionary<string, List<Gear>> BZItems = new Dictionary<string, List<Gear>>
                {
                    {"owner", new List<Gear>
                    {
                        new Gear
                        {
                        shortname = "hammer",
                        amount = 1,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "building.planner",
                        amount = 1,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "autoturret",
                        amount = 2,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "door.hinged.toptier",
                        amount = 4,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "lock.code",
                        amount = 4,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "stones",
                        amount = 20000,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "wood",
                        amount = 7500,
                        skin = 0,
                        }
                    }
                    },
                    //  {"defender", new List<Gear>
                    //{
                    //    new Gear
                    //    {
                    //    shortname = "Leader",
                    //    amount = 1,
                    //    skin = 1111,
                    //    }
                    //  }

                    //},
                      {"attacker", new List<Gear>
                    {
                        new Gear
                        {
                        shortname = "explosive.timed",
                        amount = 2,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "rocket.launcher",
                        amount = 1,
                        skin = 0,
                        },
                        new Gear
                        {
                        shortname = "ammo.rocket.basic",
                        amount = 4,
                        skin = 0,
                        }
                      }

                    }
                };



        private void SaveLoop()
        {
            SaveData();
            timer.Once(configData.Save_Interval * 60, () => SaveLoop());
        }

        private void InfoLoop()
        {
            if (!configData.Use_FactionsInfo) return;
            SendPuts(lang.GetMessage("FactionsInfo", this));
            timer.Once(900, () => InfoLoop());
        }

        void SaveData()
        {
            PlayerData.WriteObject(playerData);
            InvData.WriteObject(invData);
            FactionData.WriteObject(factionData);
        }

        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<FactionSavedPlayerData>();
            }
            catch
            {

                Puts("Couldn't load player data, creating new datafile");
                playerData = new FactionSavedPlayerData();
            }
            try
            {
                invData = InvData.ReadObject<PlayerSavedInventories>();
            }
            catch
            {

                Puts("Couldn't load inventory data, creating new datafile");
                invData = new PlayerSavedInventories();
            }
            try
            {
                factionData = FactionData.ReadObject<FactionStatistics>();
            }
            catch
            {
                Puts("Couldn't load FactionBattleZones data, creating new datafile");
                factionData = new FactionStatistics();
            }
            if (factionData.Factions == null || factionData.Factions.Count == 0)
                LoadDefaultData();
        }

        void LoadDefaultData()
        {
            factionData.Factions = defaultFactions;
            SaveData();
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            //--------//General Settings//--------//
            public int Save_Interval { get; set; }
            public bool Use_FactionsInfo { get; set; }
            public float ZoneRadius { get; set; }

            //--------//Chat Settings//--------//
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
            public bool BroadcastDeath { get; set; }
            public bool Use_FactionNamesonChat { get; set; }
            public bool Use_FactionChatControl { get; set; }
            public bool Use_ChatTitles { get; set; }

            //--------//Faction Settings//--------//

            ////////////////////////////
            //Faction Option Settings//
            ///////////////////////////

            public bool Use_Kits { get; set; }
            public bool Use_Taxes { get; set; }
            public bool Use_Groups { get; set; }
            public bool Use_PersistantSpawns { get; set; }
            public int SpawnCountLimit { get; set; }
            public int SpawnCooldown { get; set; }
            public bool Use_RallySpawns { get; set; }
            public bool Use_FactionSpawns { get; set; }
            public bool Use_BattleZones { get; set; }
            public int BattleZonesCooldown { get; set; }
            public int RequiredBZParticipants { get; set; }
            public int BZPrepTime { get; set; }
            public bool Use_FactionSafeZones { get; set; }


            public bool Use_FactionsByInvite { get; set; }
            public bool AllowPlayerToLeaveFactions { get; set; }

            public bool Use_RevoltChallenge { get; set; }
            public bool Use_FactionBalancing { get; set; }
            public int AllowedFactionDifference { get; set; }
            public bool FFDisabled { get; set; }
            public bool BuildingProtectionEnabled { get; set; }
            public float FF_DamageScale { get; set; }

            /////////////////////////////
            //Faction  Specific Options//
            /////////////////////////////

            public string StarterKit { get; set; }

            //////////////////
            //Trades & Ranks//
            //////////////////
            public bool Use_Ranks { get; set; }
            public bool Use_Trades { get; set; }
            public bool AllowTradesByPlayer { get; set; }
            public float RankBonus { get; set; }
            public double LevelBonus { get; set; }
            public int TradeLimit { get; set; }
            public int LevelRequirement { get; set; }
            public int RankRequirement { get; set; }
            public int MaxLevel { get; set; }
            public bool Use_AutoAuthorization { get; set; }


            /////////////////////
            //Faction Buildings//
            /////////////////////


            //--------//Game Modes//--------//

            ///////////////
            //Kill Ticker//
            //////////////
            public bool Use_FactionKillIncentives { get; set; }
            public int KillLimit { get; set; }


            //--------//Leader Modes//--------//

            /////////
            //Time//
            ////////
            public bool Use_FactionLeaderByTime { get; set; }

            /////////
            //Admin//
            ////////
            public bool Use_FactionLeaderByAdmin { get; set; }


            /////////
            //Rank//
            ////////
            public bool Use_FactionLeaderByRank { get; set; }


            ////////////////////
            //FactionZones Mode//
            ////////////////////
            public bool Use_FactionZones { get; set; }

            ///////////////////
            //Reward Settings//
            ///////////////////

            public bool Use_EconomicsReward { get; set; }
            public int KillAmountEconomics { get; set; }
            public int FactionKillsRewardEconomics { get; set; }
            public int BattleZoneRewardEconomics { get; set; }

            public bool Use_TokensReward { get; set; }
            public int KillAmountTokens { get; set; }
            public int FactionKillsRewardTokens { get; set; }
            public int BattleZoneRewardTokens { get; set; }

            public bool Use_ServerRewardsReward { get; set; }
            public int KillAmountServerRewards { get; set; }
            public int FactionKillsRewardServerRewards { get; set; }
            public int BattleZoneRewardServerRewards { get; set; }
        }
        private void LoadVariables()
        {
            UseFactions = true;
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                MSG_MainColor = "<color=orange>",
                MSG_Color = "<color=#A9A9A9>",
                BroadcastDeath = true,
                Use_EconomicsReward = false,
                KillAmountEconomics = 100,
                FactionKillsRewardEconomics = 500,
                BattleZoneRewardEconomics = 500,
                Use_TokensReward = false,
                KillAmountTokens = 10,
                FactionKillsRewardTokens = 50,
                BattleZoneRewardTokens = 50,
                Use_ServerRewardsReward = false,
                KillAmountServerRewards = 10,
                FactionKillsRewardServerRewards = 50,
                BattleZoneRewardServerRewards = 50,
                RankBonus = 25f,
                LevelBonus = 10,
                TradeLimit = 2,
                LevelRequirement = 5000,
                RankRequirement = 1,
                MaxLevel = 20,
                SpawnCooldown = 30,
                SpawnCountLimit = 4,
                Use_PersistantSpawns = false,
                AllowTradesByPlayer = false,
                AllowPlayerToLeaveFactions = false,
                ZoneRadius = 80f,
                Use_FactionZones = false,
                Use_BattleZones = false,
                BattleZonesCooldown = 60,
                Use_AutoAuthorization = false,
                RequiredBZParticipants = 2,
                BZPrepTime = 10,


                StarterKit = "StarterKit",

                FFDisabled = true,
                BuildingProtectionEnabled = true,

                FF_DamageScale = 0.0f,
                Use_FactionChatControl = true,
                Save_Interval = 15,
                Use_Ranks = false,
                Use_Trades = false,
                Use_Taxes = false,
                Use_Groups = false,
                Use_RallySpawns = false,
                Use_FactionSpawns = false,
                Use_FactionsInfo = true,
                Use_RevoltChallenge = false,
                Use_FactionsByInvite = false,
                Use_FactionLeaderByAdmin = false,
                Use_Kits = false,
                AllowedFactionDifference = 5,
                Use_FactionKillIncentives = false,
                Use_FactionLeaderByRank = false,
                Use_FactionLeaderByTime = false,
                Use_FactionBalancing = false,
                KillLimit = 200,
                Use_FactionNamesonChat = true,
                Use_ChatTitles = true,
                Use_FactionSafeZones = false,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Factions: " },
            {"FactionsInfo", "This server is running Factions(type /faction or Press 'N')."},
            {"FFBuildings", "This is a friendly structure owned by {0}! You must be authorized on a nearby Tool Cupboard to damage it!"},
            {"FFs", "{0} is on your faction!"},
            {"Payment", "You have received {0} {1} for that kill!" },
            {"FactionUnassignFormating", "Format: faction.unassign <PARTIAL_PLAYERNAME>"},
            {"PlayerReturns", "{0} has returned!"},
            {"PlayerLeft", "{0} has left!"},
            {"PlayerNew", "{0} has joined the fight!"},
            {"UnassignSuccess", "{0} has been successfully removed from {1}."},
            {"RemovedFromFaction", "You have been removed from {0}."},
            {"UnassignError", "There was an error unassigning the faction"},
            {"multiPlayers", "Multiple players found with that name" },
            {"NoPlayers", "No players found" },
            {"NoFactionError", "You are not a faction member"},
            {"NotAuth", "You do not have permission to use this command."},
            {"DeathMessage", " has killed " },
            {"inFactionChat", "[FACTIONCHAT]" },
            {"FactionSelectionTitle", "Which Faction would you like to join?" },
            {"MembersGUI", "Members" },
            {"Close", "Close" },
            {"Next", "Next" },
            {"Previous", "Previous" },
            {"Information", "Factions Information" },
            {"FactionPlayerInfo", "The Factions Mod enables players to have a different experience in Rust then what is normally expected.\nThis particular server has (3) Factions named Albion, Midgard, and Hibernia. Each Faction has a Leader which can set a Tax on the members.\nThe Leader can also set Spawn Points which can be used by members of the Faction every 15 minutes from within the Player Commands Menu or on respawn.\n\nYou can become the Leader by killing members of the other Faction.\nAs you kill you will increase in Rank. If you have the highest Rank in the Faction, you will become the Leader.\n\nYou can also select one of four professions which provide a bonus to harvesting resources.\nAs you harvest these skills increase in level giving higher and higher bonuses.\n\nEnjoy!" },
            {"ScoreBoard", "ScoreBoard" },
            {"PlayerCommands", "<b><i>Player Commands</i></b>" },
            {"LeaderCommands", "<b><i>Leader Commands</i></b>" },
            {"AdminCommands", "<b><i>Admin Commands</i></b>" },
            {"Options", "Options" },
            {"Joined", "has joined" },
            {"PlayerStructureFailed","You can not build a zone within the radius of a player structure. Move further away and try again!" },
            {"MonumentFailed", "You can not build a zone within the radius of a monument. Move further away and try again!" },
            { "BZJoin", "has joined the BattleZone created by " },
            {"LeftTheFaction", "{0} has left the Faction! If they knew your code locks you should change them!" },
            {"NewLeader", " has a new leader!!" },
            {"TaxBoxActivated", "You have activated Tax Box Selection Mode. Please open a Large Wooden Box or Wooden Box to make it a Faction Tax Box. You can deactivate this mode by 'Pressing the Button Again'" },
            {"TaxBoxDeActivated", "You have Deactivated Tax Box Selection Mode." },
            {"NewTaxBox", "You have activated a new Tax Box. Set a tax to start collecting!." },
            {"TaxBoxRemoved", "The Faction Tax Box has been removed!" },
            {"TaxBoxError", "A Tax Box must be a Large Wooden Box or Wooden Box." },
            {"TaxBoxOwnerError", "The Tax Box must be owned by yourself or a Faction Member." },
            {"TaxBox", "This is your Tax Box." },
            {"TaxBoxFull", "Your Tax Box is Full. Remove items or designate a new one to continue collecting." },
            {"FactionChat1"," - Sends the message to other Faction Members ONLY" },
            {"FactionChat", "/fc <message>" },
            {"NewFactionSpawn", "Attention: A new Faction Spawn has been created for your faction!" },
            {"FactionSpawnRemoved", "Attention: The {0} Spawn: {1} has been removed." },
            {"NewRallySpawn", "Attention: A new Rally Spawn has been created for your faction!" },
            {"RallySpawnRemoved", "Attention: The Rally Spawn has been removed." },
            {"ChallengerLost", "{0} challenged the faction leader and lost. {0} has been removed from the faction." },
            {"LeaderLost", "{0} challenged the faction leader and won!"},
            {"ChallengeActivated", "You have succesfully challenged the faction leader. In 15 minutes you will be able to attack and raid the leader. Losing this challenge will remove you from the faction." },
            {"ChallengeToLeader", "You have been challenged for your leadership by {0}. In 15 minutes you will be able to attack and raid {0}." },
            {"CurrentChallengePlayer", "You can not challenge the leader because {0} has an active challange." },
            {"ChallengeFactionLeaderOffline", "The leader is not currently online. You can not challenge an offline leader."},
            {"FactionDamageEnabledLeader", "The Challenge has begun. Friendly fire between between yourself and the challenger has been enabled." },
            {"FactionDamageEnabledChallenger", "The Challenge has begun. Friendly fire between yourself and your leader {1} has been enabled." },
            {"FactionDamageDisabled", "The Challenge has ended. Friendly fire is once again disabled." },
            {"newassignedleader", "has become the new Faction Leader of" },
            {"TaxBoxDestroyed", "Your Tax Box has been destroyed!" },
            {"FactionToFull", "This Faction can not accept new players at this time because it has to many players compared to the other Factions." },
            {"TargetNotInFaction", "{0} is not in a faction!" },
            {"LeaderTradeRemoved", "Trade: {1} has been removed from {0}"},
            {"PlayerTradeRemoved", "You are no longer a {0}"},
            {"PlayerTradeSuccess", "You are now a {0}"},
            {"LeaderTradeSuccess", "{0} has been added to Trade: {1}" },
            {"ChallengeLostRank", "Your Rank has been set back to Recruit!" },
            {"ChallengeLostTime", "Your FactionTime has been set back to 0" },
            {"ChallengeLostLeader", "You are no longer the leader of the Faction!!" },
            {"ChallengeNewLeader", "{0} has defeated {1} and become the new Leader!" },
            {"FactionKillTotals", "Faction Kill Totals" },
            {"KillLimitReached", "{0} has reached the Kill Limit! CONGRATULATIONS!!!" },
            {"KillTickerReset",  "The Kill Ticker will reset in 5 minutes!" },
            {"KillLimitReward",  "Each Player on the winning Faction received {0}" },
            {"NewSpawnAdmin", "You have successfully created a new Faction Spawn." },
            {"RemovedSpawnAdmin", "You have successfully removed a Faction Spawn." },
            {"LeaderChangedChallengeDone", "The Leader has changed. If you were in an Leader Challenge, it has been terminated." },
            {"RemovedAsLeader", "You have been removed as the Leader of {0}. If you had a Tax Box it has been disabled!" },
            {"NotAValidKit", "is not a valid kit! Please check the Faction Kit Configuration." },
            {"FactionLists", "Faction Lists" },
            { "Immune", "You have recently left a BattleZone and have been granted immunity from attacking or being attacked. This will last 10 minutes. Take this time to get to safety."},
            { "LeaderDeathWinner", "You helped successfully kill the Leader. You have been given {1) {0}"},
            { "TimeLimitWinner", "You helped successfully defend the Leader. You have been given {1) {0}"},
            { "EnemiesDeadWinner", "You helped successfully kill all enemies while protecting the Leader. You have been given {1) {0}"},
            { "ImmunityGone", "You are no longer immune to attacking or being attacked"},
            {"CurrentlyImmuneAttacker", "You or your target are currently immune from combat." },
            {"CurrentlyImmuneVictim", "You or your attacker are currently immune from combat." },
            { "VictimNotinBZ", "You can not attack a player outside of a BattleZone while you are in one."},
            { "AttackerNotinBZ", "You can not attack a player inside a BattleZone if you are not in one."},
            {"CreatorFactionName", "Faction Name Set to {0}"},
            {"CreatorLeaderTitle", "Faction Leader Title Set to {0}"},
            {"CreatorFactionKit", "Faction Kit Set to {0}"},
            {"CreatorFactionGroup", "Faction Group Set to {0}"},
            {"ActiveSpawnCooldown", "Unable to use Spawn as you are on a Cooldown!" },
            {"ToManySpawns", "Unable to add a new spawn! Your spawn count exceeds the limit of {0}" },
            {"SpawnManagement", "Spawn Management" },
            {"ZoneManagement", "Zone Management" },
            {"FactionManagement", "Faction Management" },
            {"PlayerManagement", "Player Management" },
            {"LeaderManagement", "Leader Management" },   
            {"ExitedFactionDeletion", "Faction deletion has been canceled!" },
            {"FactionDeleted", "has been removed! All players in the Faction were removed." },
            {"QuitFactionCreation", "You have successfully quit Faction Creation." },
            {"NewFactionCreated", "You have successfully saved the Faction: {0}" },
            {"QuitFactionEditor", "You have successfully quit Faction Editor." },
            {"FactionEditor", "Faction Editor" },
            {"FactionInviteScreen", "Faction Player Invite Menu\n Players shown below are not currently in a Faction." },
            {"RejectedInviteToLeader", "{0} has rejected your Faction Invite!" },
            {"RejectedInvite", "You have rejected the Faction Invite." },
            {"UnassignPlayers", "Player Unassignment Menu" },
            {"ConfirmKick", "Are you sure you want to kick this player from the faction?" },
            {"KickSuccessful", "You have successfully kicked {0} from {1}!" },
            {"KickCanceled", "The kick has been canceled!" },
            {"TradeManagement", "Trade Manager" },
            {"PlayerTradeSelection", "Please Selection a Trade" },
            {"AdminMSGRemovedLeader", "{0} has been successfully removed as Leader of {1}" },
            {"AdminMSGAssignedLeader", "{0} has been successfully promoted to Leader of {1}" },
            {"Rally Spawns", "Rally Spawns" },
            {"Faction Spawns", "Faction Spawns" },
            {"TaxRequestMessage", "Please select the number you would like to set the Faction Tax at"},
            {"NewTax", "You have set a new tax of {0}%." },
            {"PlayerTradeFull", "Unable to become a {0} becuase the trade is full. " },
            {"AssignTrades", "Assign Trades" },
            {"ServerRewardsRewardInfo", "This setting controls the Kill Incentives 'Victory' Reward and Other Faction Kills. Setting this as 'TRUE' makes it so players received a Server Reward Points Reward for each kill. On Kill Limit reached, it also gives each player on the winning Faction a reward in the form of Server Reward Points. This setting requires the Plugin ServerRewards. The amount of points given per kill 'KillAmountServerRewards' and for Kill limit 'FactionKillsRewardServerRewards' are configured in the Config File." },
            {"ServerRewardsRewardTitle", "Server Rewards Reward Setting" },
            {"OptionChangeMSG", "Do you want to change this setting to: {0}" },
            {"OptionChangeTitle", "{0} is currently set to: {1}" },
            {"TRUE", "<color=#005800>TRUE</color>" },
            {"FALSE", "<color=#FF0000>FALSE</color>" },
            {"TradeSkill", "Trade Skill\n{0}" },
            {"Unassign", "<color=#e60000>Unassign</color>" },
            {"Remove", "<color=#e60000>Remove</color>" },
            {"AssignPlayer", "Assign\nPlayers to\n{0}" },
            {"UnAssignPlayer", "Unassign\nPlayers from\n{0}" },
            {"EditLeader", "Edit\n{0}\n{1}" },
            {"Assign", "Assign" },
            {"Kick", "Kick" },
            {"Invite", "Invite" },
            {"FactionSelection", "Faction Selection" },
            {"Back", "Back" },
            {"PLAYERS", "PLAYERS" },
            {"DeleteFaction", "Delete\n{0}" },
            {"CreateFaction", "Create New Faction" },
            {"EditFaction", "Edit\n{0}" },
            {"AssignTradeSkill", "Assign\n{0}" },
            {"UnassignTradeSkill", "Unassign\n{0}" },
            {"AssignPlayerTradeSkill", "Assign\n{1} as\n{0}" },
            {"UnassignPlayerTradeSkill", "Unassign\n{1} as\n{0}" },
            {"FFA", "A Free for All Faction. No Leader, No Trade Skills, No Protecton." },
            {"Normal", "A Standard Faction with FF Disabled and Building Protection."},
            {"NormalNoBuildingProtection", "A Standard Faction with FF Enabled and Building Protection Disabled."},
            {"NormalFFEnabled", "A Standard Faction with FF Enabled and Building Protection Enabled."},
            {"FFEnabledNoBuildingProtection", "A Standard Faction with FF Disabled and Building Protection Disabled."},
            {"OOBWarning", "You have 10 seconds to return to the arena! "},
            {"OOBRepeater", "{0} seconds remaining..." },
            {"OOBDeath", "{0} left the {1} BattleZone and paid the price." },
            {"SpawnRepeater", "{0} minutes remaining..." },
            {"NotAllowedFZone", "You are not allowed in this Faction Zone!" },
            {"NotAllowed", "You are not allowed to enter this BattleZone. You have either already entered and died or were not online when it began." },
            {"FactionZoneDestroyed", "The Faction Zone for {0} has been destroyed!" },
            {"BZButtonText","Click to Join\nthe {0}\nBattle Zone" },
            {"BZJoinDescription", "Would you like to join the Battle Zone created by {0}?\n\nBefore you decide here is some general information...\n\nJoining the Battle Zone automatically saves your entire current inventory so equip yourself for battle before clicking 'YES'. Once you join you will be teleported outside the Battle Zone. Once you enter the Battle Zone you can not leave until the event ends or you die. If you try to leave the Battle Zone before it ends you will have 10 seconds to return to the zone or you will be killed. Upon death you can not rejoin the Battle Zone, your inventory will be restored as well as health.\n\nThe objective of the Battle Zone is to kill the Leader that created it which will result in a reward. If you are a member of {0} you will want to protect your {1}. Defending your {1} will result in a victory reward.\n\nThe Battle will last 1 hour or until all possible enemies are killed or the Leader is killed.\n\nGOODLUCK!\n\n" },
            {"BattleZone", "BattleZone" },
            {"AddFZ", "Add\n{0}\nFactionZone" },
            {"RemoveFZ", "Remove\n{0}\nFactionZone" },
            {"CurrentFZ", "Your Faction already has a Zone. Delete the current one to create a new one." },
            {"CurrentBZ", "Your Faction already has a BattleZone." },
            {"EnterFactionZone", "You have entered the {0} Faction Zone" },
            {"FactionZone", "Faction Zone" },
            {"EnterBattleZone", "You have entered a BattleZone created by {0}. If you die you may not re-enter. Surviving will result in a Faction Reward." },
            {"NewBZ", "A new BattleZone has been created by {0}. In {1} minutes you will be able to join the Battle. Equip yourself in preparation as this Battle requires you bring your own equipment. However, do not worry your inventory will be restored after the Battle!" },
            {"StartedBZ", "A BattleZone is currently in progress. You can join the battle by clicking the 'Join BattleZone' button. Equip yourself in preparation as this Battle requires you bring your own equipment. However, do not worry your inventory will be restored after the Battle!"},
            {"BZStart", "A new BattleZone created by {0} is now open! You can join the battle by clicking the 'Join BattleZone' button. Equip yourself in preparation as this Battle requires you bring your own equipment. However, do not worry your inventory will be restored after the Battle!"},
            {"BZEnded", "The Battle Zone created by {0} has ended because {1}" },
            {"NoFZ", "This Faction does not have a Zone to remove!" },
            {"ChallengeLeader", "Challenge The Leader" },
            {"LeaveFaction", "Leave Your\nFaction?" },
            {"TaxBoxMode", "Toggle\nTax Box\nMode" },
            {"RemoveTaxBox", "Remove\nTax Box" },
            {"SetTax", "Set\na Faction\nTax" },
            {"Cancelled", "it was cancelled by the Admin!" },
            {"LeaderDeath", "the Leader that created the Zone died!" },
            {"Unloaded", "Factions has been unloaded and therefore the event has ended!" },
            {"TimeLimit", "the Time Limit has been reached!"},
            {"EnemiesDead", "all the opposing Faction members online have been killed!"},
            {"ChatControlTitle", "Factions Chat Control" },
            {"ChatControlInfo", "This setting controls the use of Faction colors and attributes when players send a message. This setting has no impact on the /fc chat command for private internal faction communication." },
            {"NameOnChatTitle", "Name of Faction on Chat Messages" },
            {"NameOnChatInfo", "This setting controls the addition of the Faction Name on Faction Member chat messages. For Example: [FACTIONNAME]PLAYERNAME: MSGâ¦ If this setting is set to âTRUEâ it will automatically set Faction Chat Control to âTRUEâ as it is a requirement. This can also be used with ChatTitles." },
            {"ChatTitlesTitle", "Faction Leader Titles on Chat" },
            {"ChatTitlesInfo", "This setting controls the addition of the Faction Leaders Title on each chat submission by the Leader. For Example: [LEADERTITLE]PLAYERNAME: MSGâ¦ If this setting is set to âTRUEâ it will automatically set Faction Chat Control to âTRUEâ as it is a requirement. This can also be used with Faction Name on Chat." },
            {"BroadcastDeathTitle", "Broadcast Death Messages" },
            {"BroadcastDeathInfo", "This setting controls the announcement of Faction Player deaths and kills from other Factions. If this setting is set to âTRUEâ, on player deaths an announcement will occur in chat with color coded Faction Player names." },
            {"FactionsInfoTitle", "Factions Info Notifier" },
            {"FactionsInfoInfo", "This setting controls the periodic announcement in chat that this server is running Factions and some helpful information. The announcement is hardcoded at every 15 minutes." },
            {"KitsTitle", "Faction Kits and Starter Kit" },
            {"KitsInfo", "This setting controls the ability to assign kits to Factions as well as enables the StarterKit. If this setting is set to âTRUEâ it will enable the prompt to pick a kit when creating a Faction. The available kits must be created prior to starting the Faction Creation process by following the steps required by the Kits Plugin. The kit selected at that time will then be given to members of that Faction on each respawn. Having the setting as âTRUEâ also enables the StarterKit which also must be created using the Kits Plugin. This kit is given automatically to any player that joins the server for the first time. On server wipes this will not reset unless you delete the factions_playerdata.json in the Oxide Data Folder. The StarterKit name can be set in the Config File." },
            {"OxideGroupsTitle", "Oxide Groups" },
            {"OxideGroupsInfo", "This setting controls the ability to assign Faction players to oxide groups automatically when joining a Faction. These groups are assigned to a Faction during Faction Creation. If the group provided does not exist it will automatically be created for you and assigned to the Faction. If the Faction is deleted the group will also be deleted." },
            {"TaxesTitle", "Faction Leader Taxes" },
            {"TaxesInfo", "This setting controls the ability to set a Faction Tax imposed by the Faction Leader. This setting also controls the ability to create a Faction Tax Box which will automatically collect taxed resource upon being harvested by a Faction Member. If this setting is set to âTRUEâ the Faction Leader will have a button under âLeader Commandsâ to assign a âNew Tax Boxâ âRemove Tax Boxâ and âSet Faction Taxâ." },
            {"TradesTitle", "Faction Trade Skills" },
            {"TradesInfo", "This setting controls the ability to have Faction Trade Skills. If this setting is set to âTRUEâ it will enable a âTrade Managerâ under âLeader Commandsâ. This manager allows the Leader to assign and remove Trade Skills from Faction Members. Having a Trade Skill gives the player increased gather rates which can get better with leveling. There is a separate setting that allows Faction Players to choose a Trade Skill on joining a Faction. There are also settings in the Config to alter the amount of resources required to level âLevelRequirementâ, to set the maximum allowed Level âMaxLevelâ, and the bonus in percentage gained for each level âLevelBonusâ." },
            {"RanksTitle", "Faction Rank System" },
            {"RanksInfo", "This setting controls the ability to allow Faction Members to Rank based on killing players in other Factions. With each Rank the player increases the strength of his or her attack. If GameMode âFactionLeaderByRankâ is set to âTRUEâ this setting will also be set to âTRUEâ as it is a requirement. There are also settings in the Config to alter the amount of kills required to Rank âRankRequirementâ and the bonus strength in percentage gained for each Rank âRankBonusâ." },
            {"AllowTradesByPlayerTitle", "Players can Select Trade Skills" },
            {"AllowTradesByPlayerInfo", "This setting controls allowing players to choose their own Trade Skills versus being assigned by the Leader. On joining a Faction the player will be able to choose a Trade Skill. If this is set to âTRUEâ, Use_Trades will automatically be set to âTRUEâ as it is a requirement." },
            {"FactionsByInviteTitle", "Factions can only be joined by Invitation" },
            {"FactionsByInviteInfo", "This setting controls how players are able to join a Faction. If this setting is set to âTRUEâ it disables the default behavior of prompting a player to join a Faction. Instead players must be invited by the Faction Leader to join the Faction. The invite process is found within âLeader Commandsâ by clicking âInvite Playersâ. This will present a window with all players not currently in a Faction. Clicking one of them will process a request to the player asking if they want to join the Faction. If they select âYesâ they will join. This also enables the ability for Faction Leaders to âKick Playersâ within their Faction using the same process. " },
            {"FactionBalancingTitle", "Faction Balancing" },
            {"FactionBalancingInfo", "This setting controls Faction Balancing. If this setting is set to âTRUEâ it will enforce Faction Balancing based on the âAllowedFactionDifferenceâ set within the Config File. If a player tries to join a Faction that has too many players compared to the least populated Faction then they will not be allowed to join. This setting has no impact on âFactionsByInviteâ." },
            {"FactionKillIncentivesTitle", "Factions Kill Incentives â Kill Ticker" },
            {"FactionKillIncentivesInfo", "This setting controls Faction Kill Incentives. If this setting is set to âTRUEâ it enables a Kill Ticker at the top of the screen for all players. It tracks the number of kills each Faction has and enforces a Kill Limit which is set in the Config File. Each time a player kills a member of a different Faction they are given a reward if one has been enabled. If a Faction reaches the Kill Limit the entire Faction is given a reward if one has been enabled. Additional settings related to this setting include: âEconomicsRewardâ âTokensRewardâ and âServerRewardsRewardâ." },
            {"RallySpawnsTitle", "Rally Spawns" },
            {"RallySpawnsInfo", "This setting controls the ability for Faction Leaders to set Rally Spawns. These spawns show up as buttons on the screen on Faction player respawn. These buttons only last 30 seconds preventing abuse of the teleportation system. Upon use spawns are not available to players until they have foregone the Spawn Cooldown which is configurable in the Config File. Admins can also configure the maximum number of Rally Spawns allowed to be created by a Leader by setting the âSpawnCountLimitâ.These spawns can also be active all the time with the same cooldown by setting âPersistant Spawnsâ to âTRUEâ.. see âPersistant Spawnsâ. Rally Spawns are set by the Faction Leader by clicking âLeaderCommandsâ in the Faction Menu. " },
            {"FactionSpawnsTitle", "Faction Spawns" },
            {"FactionSpawnsInfo", "This setting controls the ability for Admins to set Faction Spawns. These spawns show up as buttons on the screen on Faction player respawn. These buttons only last 30 seconds preventing abuse of the teleportation system. Upon use spawns are not available to players until they have foregone the Spawn Cooldown which is configurable in the Config File. Admins can also configure the maximum number of Faction Spawns allowed to be created by setting the âSpawnCountLimitâ.These spawns can also be active all the time with the same cooldown by setting âPersistant Spawnsâ to âTRUEâ.. see âPersistant Spawnsâ. Faction Spawns are set by the Admin by clicking âAdminCommandsâ and selecting âSpawn Managementâ in the Faction Menu. " },
            {"PersistantSpawnsTitle", "Persistent Spawn Buttons" },
            {"PersistantSpawnsInfo", "This setting controls whether spawns are available to players all the time or only after respawn. If this setting is set to âTRUEâ Faction Members will see available spawns listed under the âPlayerCommandsâ Menu." },
            {"FFDisabledTitle", "Friendly Fire Protection" },
            {"FFDisabledInfo", "This setting controls whether Faction Members can be damaged by Friendly Fire. If this setting is set to âTRUEâ players within Factions will not be able to damage eachother. âFF_DamageScaleâ can be changed in the Config File to determine how much damage is done even if Friendly Fire Protection is set to âTRUEâ. The default setting for âFF_DamageScaleâ is â0â. By default Factions configured as Type: FFA will not be subjected to this option as these Factions do not have internal protection. " },
            {"BuildingProtectionEnabledTitle", "Building Protection" },
            {"BuildingProtectionEnabledInfo", "This setting controls whether Faction Members can damage other Faction Members buildings. If this setting is set to âTRUEâ players within Factions will not be able to damage eachothers buildings. By default Factions configured as Type: FFA will not be subjected to this option as these Factions do not have internal protection. " },
            {"AllowPlayerToLeaveFactionsTitle", "Allow a Player to Leave the Faction" },
            {"AllowPlayerToLeaveFactionsInfo", "This setting controls whether Faction Members can leave the Faction. If this setting is set to âTRUEâ Faction Members will have a button within âPlayerCommandsâ called âLeave Your Factionâ. Clicking this button will remove the Player from the Faction and promote the Faction that they have left." },
            {"RevoltChallengeTitle", "Leader Revolt Challenge" },
            {"RevoltChallengeInfo", "This setting controls whether Faction Members can challenge the Faction Leader for leadership. If this setting is set to âTRUEâ and there is a Faction Leader for the Faction then Faction Members will have a button within âPlayerCommandsâ called âChallenge The Leaderâ. Clicking this button will initiate a Challenge. During the Challenge Friendly Fire is Enabled and Building Protection is Disabled. Winning or Losing has repercussions based on other settings.. see Faction Leader Settings." },
            {"FactionZonesTitle", "Faction Zones" },
            {"FactionZonesInfo", "This setting controls whether Admins can create a Faction Zone per Faction. If this setting is  set to 'TRUE' Admins will have a button in 'Zone Management' within Admin Commands that allows them to create a Faction Zone on their current position. The radius of the Zone can be configured in the Config File by setting: ZoneRadius" },    
            {"AutoAuthorizationTitle", "Automatic Faction Authorization" },
            {"AutoAuthorizationInfo", "This setting controls whether members of the faction are automatically authorized on Faction Leader Turrets and Doors. If this setting is set to 'TRUE' when a Leader creates a Turret it will automatically add all Faction Members to the Authorization list. It will also add players as they join the Faction to active turrets. On being removed as Leader this list will NOT reset. This setting also allows Faction Members to open code locked doors without knowing the code." },
            {"BattleZonesTitle", "Faction Battle Zones" },
            {"BattleZonesInfo", "This setting controls whether Faction Leaders can create and participate in Faction Battle Zones. If this setting is set to 'TRUE' each Leader will be able to create a Battle Zone; however only one at a time. These zones require the player to bring their own gear which is saved and restored upon death. Dying will result in ejection from the zone and the inability to rejoin. The objective of the zone is for the Faction Leader to survive. On creation a zone is made around the Leader and he or she is given building materials and turrets. Losing a turret results in the Leader being given a new one. After a 15 minute preparation phase the zone is opened to everyone to join. Enemy Faction members join the zone with C4 and Rockets. If the Faction Leaders Faction can survive all possible enemies (checked every 20 minutes) or last 1 hour then they are given a reward. If any enemy is able to kill the Leader all opposing Faction Members that participated are given a reward. These zones also have a cooldown which is configurable in the Config File. " },
            {"FactionLeaderByRankTitle", "Faction Leader Selection By Rank" },
            {"FactionLeaderByRankInfo", "This setting controls how a Faction Leader is chosen. If this setting is set to âTRUEâ the player on each Faction with the highest Rank will become the Leader. Setting this setting to âTRUEâ will also set âUse_Ranksâ to âTRUEâ as it is a requirement. There can only be one âLeader Byâ setting at a time and therefore any other Faction Leader Selection Setting will be set to âFALSEâ upon this one being set to âTRUEâ." },
            {"FactionLeaderByTimeTitle", "Faction Leader Selection By Time" },
            {"FactionLeaderByTimeInfo", "This setting controls how a Faction Leader is chosen. If this setting is set to âTRUEâ the player on each Faction with the most in Faction Time will become the Leader. There can only be one âLeader Byâ setting at a time and therefore any other Faction Leader Selection Setting will be set to âFALSEâ upon this one being set to âTRUEâ." },
            {"FactionLeaderByAdminTitle", "Faction Leader Selection By Admin" },
            {"FactionLeaderByAdminInfo", "This setting controls how a Faction Leader is chosen. If this setting is set to âTRUEâ Leaders will only be assigned by the Admin. If âLeader Revolt Challengeâ is set to âTRUEâ Faction Members can obtain leadership by defeating the Leader in a Revolt. There can only be one âLeader Byâ setting at a time and therefore any other Faction Leader Selection Setting will be set to âFALSEâ upon this one being set to âTRUEâ." },
            {"EconomicsRewardTitle", "Economics Rewards Reward Setting" },
            {"EconomicsRewardInfo", "This setting controls the Kill Incentives 'Victory' Reward and Other Faction Kills. Setting this as 'TRUE' makes it so players received an Economics Reward for each kill. On Kill Limit reached, it also gives each player on the winning Faction a reward in the form of Economics. This setting requires the Plugin Economics. The amount of economics given per kill âKillAmountEconomicsâ and for Kill limit âFactionKillsRewardEconomicsâ are configured in the Config File." },
            {"FactionSafeZonesTitle", "Faction Safe Zone Setting" },
            {"FactionSafeZonesInfo", "This setting controls whether Faction Zones are restricted to only members of the given Faction. Setting this as 'TRUE' makes it so players in other Factions will be ejected from the given Faction Zone. This setting requires that Faction" },
            {"PromoteLeader", "Promote\n{0}\nas {1}" },
            {"RemoveLeader", "<color=#e60000>Remove</color>\n{0}\nas {1}" },
            {"InvitePlayer", "Invite\n{0}\nto {1}" },
            {"RemovePlayer", "<color=#e60000>Remove</color>\n{0}\nfrom {1}" },
            {"KickPlayer", "<color=#e60000>Kick</color>\n{0}\nfrom {1}" },
            {"InvitePlayers", "Invite\nPlayers to\n{0}" },
            {"KickPlayers", "Kick\nPlayers from\n{0}" },
            {"CreateBZ", "Create\n{0}\nBattle Zone" },
            {"DestroyBZ", "Destroy\n{0}\nBattle Zone" },
            {"CreateFactionSpawn", "Create\n{0}\nFaction Spawn" },
            {"CreateRallySpawn", "Create\nRally Spawn" },
            {"RemoveFactionSpawn", "Remove\n{1}\nSpawn: {0}" },
            {"RemoveRallySpawn", "Remove\nRally Spawn\n{0}" },
            {"NoJoin", "Do Not Join" }
        };
        #endregion
    }
}
