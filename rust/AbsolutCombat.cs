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
using System.Text;

namespace Oxide.Plugins
{
    [Info("AbsolutCombat", "Absolut", "1.0.1", ResourceId = 2103)]

    class AbsolutCombat : RustPlugin
    {
        #region Fields

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin ServerRewards;

        Gear_Weapon_Data gwData;
        private DynamicConfigFile GWData;

        SavedPlayers playerData;
        private DynamicConfigFile PlayerData;

        static GameObject webObject;
        static UnityWeb uWeb;

        string TitleColor = "<color=orange>";
        string MsgColor = "<color=#A9A9A9>";

        private List<ulong> PMState = new List<ulong>();
        private List<ulong> SMState = new List<ulong>();
        private List<ulong> AdminState = new List<ulong>();
        private Dictionary<ulong, Timer> PlayerGearSetTimer = new Dictionary<ulong, Timer>();
        private Dictionary<ulong, Timer> PlayerWeaponSetTimer = new Dictionary<ulong, Timer>();
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        private Dictionary<ulong, PurchaseItem> PendingPurchase = new Dictionary<ulong, PurchaseItem>();
        private Dictionary<ulong, PurchaseItem> SetSelection = new Dictionary<ulong, PurchaseItem>();
        private Dictionary<ulong, Dictionary<string, Dictionary<string, List<string>>>> WeaponSelection = new Dictionary<ulong, Dictionary<string, Dictionary<string, List<string>>>>();
        private List<ACPlayer> ACPlayers = new List<ACPlayer>();

        int currentItemIndex;
        private Dictionary<ulong, CreatorSet> NewSet = new Dictionary<ulong, CreatorSet>();
        private Dictionary<ulong, List<Gear>> UnProcessedGear = new Dictionary<ulong, List<Gear>>();
        private Dictionary<ulong, List<Weapon>> UnProcessedWeapon = new Dictionary<ulong, List<Weapon>>();
        private Dictionary<ulong, List<string>> UnProcessedAttachment = new Dictionary<ulong, List<string>>();

        //corpses///
        private readonly string corpsePrefab = "assets/prefabs/player/player_corpse.prefab";
        private uint corpsePrefabId;
        #endregion

        #region Hooks
        void Loaded()
        {
            GWData = Interface.Oxide.DataFileSystem.GetFile("AbsolutCombat_GWData");
            PlayerData = Interface.Oxide.DataFileSystem.GetFile("AbsolutCombat_PlayerData");
            lang.RegisterMessages(messages, this);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (GetACPlayer(player))
                    DestroyACPlayer(GetACPlayer(player));
            }
            foreach (var timer in timers)
                timer.Value.Destroy();
            timers.Clear();
            SaveData();
            ACPlayers.Clear();
        }

        void OnServerInitialized()
        {
            webObject = new GameObject("WebObject");
            uWeb = webObject.AddComponent<UnityWeb>();
            uWeb.SetDataDir(this);
            uWeb.Add("http://i.imgur.com/zq9zuKw.jpg", "General", "999999999", 0);
            uWeb.Add("http://imgur.com/Im6J2HJ.jpg", "General", "888888888", 0);
            LoadVariables();
            LoadData();
            CheckImages();
            if (configData.UseServerRewards)
            {
                try
                {
                    ServerRewards.Call("isLoaded", null);
                }
                catch (Exception)
                {
                    PrintWarning(GetMSG("NOSR", Name));
                    Interface.Oxide.UnloadPlugin(Name);
                    return;
                }
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                OnPlayerInit(p);
            }
            timers.Add("info", timer.Once(900, () => InfoLoop()));
            timers.Add("save", timer.Once(600, () => SaveLoop()));
            timers.Add("cond", timer.Once(120, () => CondLoop()));
        }

        private void CheckImages()
        {
            foreach (var entry in gwData.GearSets)
                foreach (var set in entry.Value.set)
                {
                    if (set.URL == "" || set.URL == null)
                        if (urls.ContainsKey(set.shortname))
                            if (urls[set.shortname].ContainsKey(set.skin))
                                set.URL = urls[set.shortname][set.skin];
                }
            LoadImages();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (isAuth(player)) player.Command($"bind {configData.AdminMenuKeyBinding} \"UI_ToggleMenus a\"");
            if (player != null)
            {
                player.Command($"bind {configData.PurchaseMenuKeyBinding} \"UI_ToggleMenus p\"");
                player.Command($"bind {configData.SelectionMenuKeyBinding} \"UI_ToggleMenus s\"");
                InitializeACPlayer(player);
                GetSendMSG(player, "ACInfo", configData.PurchaseMenuKeyBinding.ToUpper(), configData.SelectionMenuKeyBinding.ToUpper());
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            DestroyUI(player);
            if (GetACPlayer(player).PlayerGearSets.Count < 1)
            {
                UnityEngine.Object.Destroy(player);
                ACPlayers.Remove(GetACPlayer(player));
                InitializeACPlayer(player);
            }
            player.inventory.Strip();
            GiveSet(player);
            GiveWeapon(player);
            GiveBasicEquipment(player);
            player.health = 100f;
            PlayerHUD(player);
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            try
            {
                var attacker = hitInfo.Initiator.ToPlayer() as BasePlayer;
                var victim = entity.ToPlayer();
                if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                {
                    if (entity as BasePlayer == null || hitInfo == null) return;
                    if (!GetACPlayer(attacker) || !GetACPlayer(victim)) return;
                    if (victim.userID != attacker.userID)
                    {
                        if (EventManager)
                        {
                            object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                            if (isPlaying is bool)
                                if ((bool)isPlaying)
                                    return;
                        }
                        GetACPlayer(attacker).kills += 1;
                        GetACPlayer(victim).deaths += 1;
                        if (configData.UseServerRewards)
                            SRAction(attacker.userID, configData.KillReward, "ADD");
                        else
                            GetACPlayer(attacker).money += configData.KillReward;
                        GetACPlayer(attacker).GearSetKills[GetACPlayer(attacker).currentGearSet] += 1;
                        GetACPlayer(attacker).WeaponSetKills[GetACPlayer(attacker).CurrentWeapons.First().Key] += 1;
                        SendDeathNote(attacker, victim);
                        PlayerHUD(attacker);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void SRAction(ulong ID, int amount, string action)
        {
            if (action == "ADD")
                ServerRewards?.Call("AddPoints", new object[] { ID, amount });
            if (action == "REMOVE")
                ServerRewards?.Call("TakePoints", new object[] { ID, amount });
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return null;
            bool isCreating = false;
            CreatorSet creation;
            if (NewSet.ContainsKey(player.userID))
            {
                isCreating = true;
            }
            if (isCreating)
            {
                creation = NewSet[player.userID];
                var args = string.Join(" ", arg.Args);
                if (args.Contains("quit"))
                {
                    ExitSetCreation(player, isCreating);
                    return true;
                }
                switch (creation.stepNum)
                {
                    case 0:
                        var name = string.Join(" ", arg.Args);
                        creation.setname = name;
                        creation.stepNum = 1;
                        SetCreator(player, 1);
                        return true;
                    case 1:
                        int cost;
                        if (!int.TryParse(arg.Args[0], out cost))
                        {
                            GetSendMSG(player, "INVALIDENTRY", arg.Args[0]);
                            return true;
                        }
                        if (NewSet[player.userID].isWeapon == true)
                            creation.wset.cost = cost;
                        else creation.set.cost = cost;
                        creation.stepNum = 2;
                        SetCreator(player, 2);
                        return true;
                    case 2:
                        int kills;
                        if (!int.TryParse(arg.Args[0], out kills))
                        {
                            GetSendMSG(player, "INVALIDENTRY", arg.Args[0]);
                            return true;
                        }
                        if (NewSet[player.userID].isWeapon == true)
                        {
                            creation.wset.killsrequired = kills;
                            GetSetWeapons(player);
                        }
                        else
                        {
                            creation.set.killsrequired = kills;
                            GetSetGear(player);
                        }
                        return true;
                    case 3:
                        name = string.Join(" ", arg.Args);
                        if (NewSet[player.userID].isWeapon == true)
                            creation.CurrentCreationWeapon.name = name;
                        else creation.CurrentCreationGear.name = name;

                        creation.stepNum = 4;
                        SetCreator(player, 4);
                        return true;
                    case 5:
                        int price;
                        if (!int.TryParse(arg.Args[0], out price))
                        {
                            GetSendMSG(player, "INVALIDENTRY", arg.Args[0]);
                            return true;
                        }
                        if (NewSet[player.userID].isWeapon == true)
                            creation.CurrentCreationWeapon.price = price;
                        else creation.CurrentCreationGear.price = price;
                        creation.stepNum = 6;
                        SetCreator(player, 6);
                        return true;
                    case 6:
                        int kills1;
                        if (!int.TryParse(arg.Args[0], out kills1))
                        {
                            GetSendMSG(player, "INVALIDENTRY", arg.Args[0]);
                            return true;
                        }
                        if (NewSet[player.userID].isWeapon == true)
                            creation.CurrentCreationWeapon.killsrequired = kills1;
                        else creation.CurrentCreationGear.killsrequired = kills1;
                        SetCreator(player, 7);
                        return true;
                }
            }
            return null;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            corpsePrefabId = StringPool.Get(corpsePrefab);
            if (configData.UseEnviroControl)
            {
                if (entity.prefabID == corpsePrefabId)
                {
                    entity.Kill();
                }
                var collectible = entity as CollectibleEntity;
                if (collectible != null)
                {
                    collectible.itemList = null;
                }
                var worldItem = entity as WorldItem;
                if (worldItem != null)
                {
                    worldItem.allowPickup = false;
                }
            }
        }

        private void OnLootEntity(BasePlayer looter, BaseEntity target)
        {
            if (configData.UseEnviroControl && !isAuth(looter))
            {
                if ((target as StorageContainer)?.transform.position == Vector3.zero) return;
                timer.Once(0.01f, looter.EndLooting);
            }
        }

        void OnPlantGather(PlantEntity Plant, Item item, BasePlayer player)
        {
            if (configData.UseEnviroControl && !isAuth(player))
            {
                item.amount = 0;
            }
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (configData.UseEnviroControl && !isAuth(player))
            {
                item.amount = 0;
            }
        }
        void OnDispenserGather(ResourceDispenser Dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (configData.UseEnviroControl && !isAuth(player))
            {
                item.amount = 0;
            }
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (configData.UseEnviroControl && !isAuth(crafter))
            {
                task.cancelled = true;
            }
            return null;
        }
        #endregion

        #region Functions

        private void GetSetGear(BasePlayer player)
        {
            bool isCreating = false;
            CreatorSet creation;
            if (NewSet.ContainsKey(player.userID))
            {
                isCreating = true;
            }
            if (isCreating)
            {
                creation = NewSet[player.userID];
                if (currentItemIndex == -1)
                {
                    if (UnProcessedGear.ContainsKey(player.userID)) UnProcessedGear.Remove(player.userID);
                    UnProcessedGear.Add(player.userID, new List<Gear>());
                    UnProcessedGear[player.userID].AddRange(GetItems(player.inventory.containerWear, "wear"));
                    currentItemIndex = 0;
                }
                if (currentItemIndex >= UnProcessedGear[player.userID].Count())
                {
                    SetCreator(player, 99);
                    return;
                }
                else
                {
                    creation.CurrentCreationGear = UnProcessedGear[player.userID][currentItemIndex];
                    creation.stepNum = 3;
                    SetCreator(player, 3);
                }
            }
        }

        private void GetSetWeapons(BasePlayer player)
        {
            bool isCreating = false;
            CreatorSet creation;
            if (NewSet.ContainsKey(player.userID))
            {
                isCreating = true;
            }
            if (isCreating)
            {
                creation = NewSet[player.userID];
                if (currentItemIndex == -1)
                {
                    if (UnProcessedWeapon.ContainsKey(player.userID)) UnProcessedWeapon.Remove(player.userID);
                    UnProcessedWeapon.Add(player.userID, new List<Weapon>());
                    UnProcessedWeapon[player.userID].AddRange(GetWeapon(player.inventory.containerBelt, "belt"));
                    if (UnProcessedAttachment.ContainsKey(player.userID)) UnProcessedAttachment.Remove(player.userID);
                    UnProcessedAttachment.Add(player.userID, new List<string>());
                    foreach (var entry in UnProcessedWeapon[player.userID])
                        currentItemIndex = 0;
                }
                if (currentItemIndex >= UnProcessedWeapon[player.userID].Count() || currentItemIndex >= 2)
                {
                    SetCreator(player, 99);
                    return;
                }
                else
                {
                    if (UnProcessedAttachment.ContainsKey(player.userID)) UnProcessedAttachment.Remove(player.userID);
                    UnProcessedAttachment.Add(player.userID, new List<string>());
                    creation.CurrentCreationWeapon = UnProcessedWeapon[player.userID][currentItemIndex];
                    creation.stepNum = 3;
                    SetCreator(player, 3);
                }
            }
        }

        private void InitializeACPlayer(BasePlayer player)
        {
            if (!player.GetComponent<ACPlayer>())
            {
                if (playerData.players.ContainsKey(player.userID))
                {
                    if (playerData.priorSave.ContainsKey(player.userID))
                    {
                        if (playerData.players[player.userID].PlayerGearSets.Count < 1 && playerData.priorSave[player.userID].PlayerGearSets.Count > 0)
                        {
                            playerData.players[player.userID] = playerData.priorSave[player.userID];
                        }
                    }
                    if (playerData.players[player.userID].PlayerGearSets.Count < 1 && playerData.priorSave[player.userID].PlayerGearSets.Count < 1)
                    {
                        playerData.players.Remove(player.userID);
                        playerData.priorSave.Remove(player.userID);
                        InitializeACPlayer(player);
                        return;
                    }
                    ACPlayers.Add(player.gameObject.AddComponent<ACPlayer>());
                    ACPlayer ac = GetACPlayer(player);
                    var d = playerData.players[player.userID];
                    ac.deaths = d.deaths;
                    ac.kills = d.kills;
                    ac.money = d.money;
                    ac.PlayerGearSets = d.PlayerGearSets;
                    ac.PlayerWeaponSets = d.PlayerWeaponSets;
                    ac.GearSetKills = d.GearSetKills;
                    ac.WeaponSetKills = d.WeaponSetKills;
                    ac.currentGearSet = d.lastSet;
                    ac.CurrentWeapons = d.lastWeapons;
                }
                else
                {
                    ACPlayers.Add(player.gameObject.AddComponent<ACPlayer>());
                    ACPlayer ac = GetACPlayer(player);
                    ac.currentGearSet = "Starter";
                    ac.kills = 0;
                    ac.deaths = 0;
                    ac.money = 10;
                    ac.PlayerGearSets.Add("Starter", new List<string>());
                    foreach (var entry in gwData.GearSets.Where(kvp => kvp.Key == "Starter"))
                    {
                        foreach (var item in entry.Value.set)
                            ac.PlayerGearSets["Starter"].Add(item.shortname);
                    }
                    ac.PlayerWeaponSets.Add("Starter", new Dictionary<string, List<string>>());
                    ac.CurrentWeapons.Add("Starter", new Dictionary<string, List<string>>());
                    foreach (var entry in gwData.WeaponSets.Where(kvp => kvp.Key == "Starter"))
                    {
                        foreach (var item in entry.Value.set)
                        {
                            ac.PlayerWeaponSets["Starter"].Add(item.shortname, new List<string>());
                            ac.CurrentWeapons["Starter"].Add(item.shortname, new List<string>());
                            foreach (var attachment in item.attachments)
                                if (gwData.Attachments.ContainsKey(attachment))
                                {
                                    ac.PlayerWeaponSets["Starter"][item.shortname].Add(gwData.Attachments[attachment].shortname);
                                    ac.CurrentWeapons["Starter"][item.shortname].Add(gwData.Attachments[attachment].shortname);
                                }

                        }
                    }
                    ac.GearSetKills.Add("Starter", 0);
                    ac.WeaponSetKills.Add("Starter", 0);
                }
            }
            CheckSets(GetACPlayer(player));
            player.inventory.Strip();
            GiveSet(player);
            GiveWeapon(player);
            GiveBasicEquipment(player);
            PlayerHUD(player);
        }

        void CheckSets(ACPlayer player)
        {
            List<string> gearset = new List<string>();
            List<string> weaponset = new List<string>();
            foreach (var entry in player.PlayerGearSets)
                if (!gwData.GearSets.ContainsKey(entry.Key))
                {
                    gearset.Add(entry.Key);
                    if (player.currentGearSet == entry.Key)
                    {
                        if (!gwData.GearSets.ContainsKey("Starter"))
                            player.currentGearSet = null;
                        else player.currentGearSet = "Starter";
                        player.player.inventory.Strip();
                        GiveSet(player.player);
                        GiveWeapon(player.player);
                        GiveBasicEquipment(player.player);
                    }
                }
            foreach (var entry in player.PlayerWeaponSets)
                if (!gwData.WeaponSets.ContainsKey(entry.Key))
                {
                    weaponset.Add(entry.Key);
                    var currentweapon = "";
                    if (GetACPlayer(player.player).CurrentWeapons != null)
                        currentweapon = GetACPlayer(player.player).CurrentWeapons.First().Key;
                    if (currentweapon == entry.Key)
                    {
                        player.CurrentWeapons.Remove(entry.Key);
                        if (!gwData.WeaponSets.ContainsKey("Starter"))
                            player.CurrentWeapons = null;
                        else
                        {
                            player.CurrentWeapons.Add("Starter", new Dictionary<string, List<string>>());
                            foreach (var wp in gwData.WeaponSets.Where(kvp => kvp.Key == "Starter"))
                            {
                                foreach (var item in wp.Value.set)
                                {
                                    player.CurrentWeapons["Starter"].Add(item.shortname, new List<string>());
                                    foreach (var attachment in item.attachments)
                                        if (gwData.Attachments.ContainsKey(attachment))
                                        {
                                            player.CurrentWeapons["Starter"][item.shortname].Add(gwData.Attachments[attachment].shortname);
                                        }
                                }
                            }
                        }
                        player.player.inventory.Strip();
                        GiveSet(player.player);
                        GiveWeapon(player.player);
                        GiveBasicEquipment(player.player);
                    }
                }
            if (gearset != null)
                foreach (var entry in gearset)
                {
                    player.PlayerGearSets.Remove(entry);
                    player.GearSetKills.Remove(entry);
                }
            if (weaponset != null)
                foreach (var entry in weaponset)
                {
                    player.PlayerWeaponSets.Remove(entry);
                    player.WeaponSetKills.Remove(entry);
                }
        }

        private object CheckPoints(ulong ID) => ServerRewards?.Call("CheckPoints", ID);

        void DestroyACPlayer(ACPlayer player)
        {
            if (player.player == null) return;
            {
                SaveACPlayer(player);
                DestroyUI(player.player);
                if (isAuth(player.player)) player.player.Command($"bind {configData.AdminMenuKeyBinding} \"\"");
                player.player.Command($"bind {configData.PurchaseMenuKeyBinding} \"\"");
                player.player.Command($"bind {configData.SelectionMenuKeyBinding} \"\"");
                player.player.Command($"bind tab \"inventory.toggle\"");
                if (ACPlayers.Contains(player))
                {
                    UnityEngine.Object.Destroy(player);
                    ACPlayers.Remove(player);
                }
                if (PMState.Contains(player.player.userID))
                    PMState.Remove(player.player.userID);
                if (SMState.Contains(player.player.userID))
                    SMState.Remove(player.player.userID);
            }
        }

        ACPlayer GetACPlayer(BasePlayer player)
        {
            if (!player.GetComponent<ACPlayer>())
                return null;
            else return player.GetComponent<ACPlayer>();
        }


        private string GetLang(string msg)
        {
            if (messages.ContainsKey(msg))
                return lang.GetMessage(msg, this);
            else return msg;
        }

        private void GetSendMSG(BasePlayer player, string message, string arg1 = "", string arg2 = "", string arg3 = "")
        {
            string msg = string.Format(GetLang(message), arg1, arg2, arg3);
            SendReply(player, TitleColor + lang.GetMessage("title", this, player.UserIDString) + "</color>" + MsgColor + msg + "</color>");
        }

        private string GetMSG(string message, string arg1 = "", string arg2 = "", string arg3 = "")
        {
            string msg = string.Format(lang.GetMessage(message, this), arg1, arg2, arg3);
            return msg;
        }

        static void TPPlayer(BasePlayer player, Vector3 destination)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            player.TransformChanged();
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }
        static void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        public void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseScreen);
            CuiHelper.DestroyUi(player, PanelPurchaseMenu);
            CuiHelper.DestroyUi(player, PanelSelectionMenu);
            CuiHelper.DestroyUi(player, PanelSelectionScreen);
            CuiHelper.DestroyUi(player, PanelOnScreen);
            CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
            CuiHelper.DestroyUi(player, PanelCreation);
            CuiHelper.DestroyUi(player, PanelAdmin);
            CuiHelper.DestroyUi(player, PanelStats);
        }

        public void DestroyPurchaseMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseMenu);
            CuiHelper.DestroyUi(player, PanelPurchaseScreen);
            if (PMState.Contains(player.userID))
                PMState.Remove(player.userID);
        }

        public void DestroySelectionMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelSelectionMenu);
            CuiHelper.DestroyUi(player, PanelSelectionScreen);
            if (SMState.Contains(player.userID))
                SMState.Remove(player.userID);
        }

        public void Broadcast(string message, string userid = "0") => PrintToChat(message);

        private void SendDeathNote(BasePlayer player, BasePlayer victim)
        {
            string colorAttacker = "";
            string colorVictim = "";

            colorAttacker = "<color=#e60000>";
            colorVictim = "<color=#3366ff>";
            if (configData.BroadcastDeath)
            {
                string formatMsg = colorAttacker + player.displayName + "</color>" + GetLang("DeathMessage") + colorVictim + victim.displayName + "</color>";
                Broadcast(formatMsg);
            }
        }


        private void SaveSet(BasePlayer player, bool isCreating)
        {
            CreatorSet creation;
            if (isCreating)
            {
                creation = NewSet[player.userID];
                if (NewSet[player.userID].isWeapon == true)
                {
                    gwData.WeaponSets.Add(creation.setname, creation.wset);
                }
                else
                {
                    gwData.GearSets.Add(creation.setname, creation.set);
                }
                NewSet.Remove(player.userID);
                GetSendMSG(player, "NewSet", creation.setname);
                currentItemIndex = -1;
                CheckImages();
            }
            DestroyCreationPanel(player);
            SaveData();
        }


        private void ExitSetCreation(BasePlayer player, bool isCreating)
        {
            if (isCreating)
            {
                NewSet.Remove(player.userID);
                currentItemIndex = -1;
                DestroyCreationPanel(player);
            }
        }

        public void DestroyCreationPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelCreation);
        }

        public void DestroyAdminPanel(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelAdmin);
            if (AdminState.Contains(player.userID))
                AdminState.Remove(player.userID);
        }

        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
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

        [ConsoleCommand("addmoney")]
        private void cmdaddmoney(ConsoleSystem.Arg arg)
        {
            chatAddMoney(null, arg.Args);
        }

        [ConsoleCommand("takemoney")]
        private void cmdtakemoney(ConsoleSystem.Arg arg)
        {
            chattakemoney(null, arg.Args);
        }

        [ChatCommand("addmoney")]
        private void chatAddMoney(BasePlayer player, string[] args)
        {
            if (player != null)
            {
                if (!isAuth(player))
                {
                    GetSendMSG(player, "NotAuthorized");
                    return;
                }
            }
            if (args.Length == 2)
            {
                int amount;
                if (!int.TryParse(args[1], out amount))
                {
                    if (player == null)
                        Puts(GetMSG("INVALIDENTRY", args[1]));
                    else
                        GetSendMSG(player, "INVALIDENTRY", args[1]);
                    return;
                }
                var partialPlayerName = args[0];
                var foundPlayers = FindPlayer(partialPlayerName);
                if (foundPlayers.Count == 0)
                {
                    if (player == null)
                        Puts(GetMSG("NoPlayers", args[0]));
                    else
                        GetSendMSG(player, "NoPlayers", args[0]);
                    return;
                }
                if (foundPlayers.Count > 1)
                {
                    if (player == null)
                        Puts(GetMSG("MultiplePlayers", args[0]));
                    else
                        GetSendMSG(player, "MultiplePlayers", args[0]);
                    return;
                }
                if (foundPlayers[0] != null)
                {
                    ulong requestor = 0;
                    if (player != null)
                        requestor = player.userID;
                    AddMoney(foundPlayers[0].userID, amount, true, requestor);
                }
            }
            else
            {
                if (player == null)
                    Puts(GetMSG("ArgumentsIncorrect", "/takemoney PLAYERNAME AMOUNT", "/takemoney Absolut 20"));
                else
                    GetSendMSG(player, "ArgumentsIncorrect", "/addmoney PLAYERNAME AMOUNT", "/addmoney Absolut 20");
            }
        }

        [ChatCommand("takemoney")]
        private void chattakemoney(BasePlayer player, string[] args)
        {
            if (player != null)
            {
                if (!isAuth(player))
                {
                    GetSendMSG(player, "NotAuthorized");
                    return;
                }
            }
            if (args.Length == 2)
            {
                int amount;
                if (!int.TryParse(args[1], out amount))
                {
                    if (player == null)
                        Puts(GetMSG("INVALIDENTRY", args[1]));
                    else
                        GetSendMSG(player, "INVALIDENTRY", args[1]);
                    return;
                }
                var partialPlayerName = args[0];
                var foundPlayers = FindPlayer(partialPlayerName);
                if (foundPlayers.Count == 0)
                {
                    if (player == null)
                        Puts(GetMSG("NoPlayers", args[0]));
                    else
                        GetSendMSG(player, "NoPlayers", args[0]);
                    return;
                }
                if (foundPlayers.Count > 1)
                {
                    if (player == null)
                        Puts(GetMSG("MultiplePlayers", args[0]));
                    else
                        GetSendMSG(player, "MultiplePlayers", args[0]);
                    return;
                }
                if (foundPlayers[0] != null)
                {
                    ulong requestor = 0;
                    if (player != null)
                        requestor = player.userID;
                    TakeMoney(foundPlayers[0].userID, amount, true, requestor);
                }
            }
            else
            {
                if (player == null)
                    Puts(GetMSG("ArgumentsIncorrect", "/takemoney PLAYERNAME AMOUNT", "/takemoney Absolut 20"));
                else
                    GetSendMSG(player, "ArgumentsIncorrect", "/takemoney PLAYERNAME AMOUNT", "/takemoney Absolut 20");
            }
        }

        #endregion

        #region UI Creation
        private string PanelPurchaseMenu = "PurchaseMenu";
        private string PanelPurchaseScreen = "PurchaseScreen";
        private string PanelSelectionMenu = "SelectionMenu";
        private string PanelSelectionScreen = "SelectionScreen";
        private string PanelOnScreen = "OnScreen";
        private string PanelPurchaseConfirmation = "PurchaseConfirmation";
        private string PanelCreation = "CreationPanel";
        private string PanelAdmin = "AdminPanel";
        private string PanelStats = "StatsPanel";

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

            static public void LoadURLImage(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Url = url },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            static public void CreateTextOverlay(ref CuiElementContainer container, string panel, string text, string color, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1.0f)
            {
                //if (configdata.DisableUI_FadeIn)
                //    fadein = 0;
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
            {"black", "0 0 0 1.0" },
            {"dark", "0.1 0.1 0.1 0.98" },
            {"header", "1 1 1 0.3" },
            {"light", ".564 .564 .564 1.0" },
            {"grey1", "0.6 0.6 0.6 1.0" },
            {"brown", "0.3 0.16 0.0 1.0" },
            {"yellow", "0.9 0.9 0.0 1.0" },
            {"orange", "1.0 0.65 0.0 1.0" },
            {"blue", "0.2 0.6 1.0 1.0" },
            {"red", "1.0 0.1 0.1 1.0" },
            {"white", "1 1 1 1" },
            {"limegreen", "0.42 1.0 0 1.0" },
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



        private Dictionary<Slot, Vector2> GearSlotPos = new Dictionary<Slot, Vector2>
        {
            { Slot.head, new Vector2(.4f, .65f) },
            { Slot.chest, new Vector2(.27f, .45f) },
            { Slot.chest2, new Vector2(.49f, .45f) },
            { Slot.legs, new Vector2(.27f, .25f) },
            { Slot.legs2, new Vector2(.49f, .25f) },
            { Slot.feet, new Vector2(.4f, .05f) },
            { Slot.hands, new Vector2(.7f, .375f) },
        };

        private Dictionary<Slot, Vector2> WeaponSlotPos = new Dictionary<Slot, Vector2>
        {
            { Slot.main, new Vector2(.21f, .55f) },
            { Slot.secondary, new Vector2(.6f, .55f) },
    };

        #endregion 

        #region UI Panels

        private void SetCreator(BasePlayer player, int step = 0)
        {
            CreatorSet creation = null;
            if (NewSet.ContainsKey(player.userID))
                creation = NewSet[player.userID];
            var key = "";
            if (creation.isWeapon == true)
                key = creation.CurrentCreationWeapon.shortname;
            else key = creation.CurrentCreationGear.shortname;
            var name = "";
            if (creation.isWeapon == true)
                name = creation.CurrentCreationWeapon.name;
            else name = creation.CurrentCreationGear.name;
            Dictionary<Slot, Vector2> list = null;
            if (creation.isWeapon == true)
                list = WeaponSlotPos;
            else list = GearSlotPos;
            var i = 0;
            Vector2 min = new Vector2(0f, 0f);
            Vector2 dimension = new Vector2(.2f, .15f);
            Vector2 offset2 = new Vector2(0.002f, 0.003f);
            var element = UI.CreateElementContainer(PanelCreation, UIColors["dark"], "0.3 0.3", "0.7 0.9");
            UI.CreatePanel(ref element, PanelCreation, UIColors["light"], "0.01 0.02", "0.99 0.98");
            switch (step)
            {
                case 0:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetLang("SetBegin")}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 1:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("SetCost", creation.setname)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 2:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("SetKills", creation.setname)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 3:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("GearName", key)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 4:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    //i = 0;
                    //if (urls.ContainsKey(key))
                    //{
                    //    UI.CreatePanel(ref element, PanelCreation, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                    //    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{MsgColor} {GetMSG("GearSkin", name)}", 20, "0 .9", "1 1", TextAnchor.MiddleCenter);
                    //    foreach (var entry in urls[key])
                    //    {
                    //        string image = entry.Value;
                    //        var pos = CalcButtonPos(i);
                    //        UI.LoadURLImage(ref element, PanelCreation, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                    //        UI.CreateButton(ref element, PanelCreation, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectSkin {entry.Key}");
                    //        i++;
                    //    }
                    //}
                    //else
                    //{
                    creation.stepNum = 5;
                    SetCreator(player, 5);
                    return;
                //    }
                //break;
                case 5:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("GearCost", name, creation.setname)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 6:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("GearKills", name, creation.setname)}", 20, "0.05 0", ".95 1", TextAnchor.MiddleCenter);
                    break;
                case 7:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    if (creation.isWeapon == true)
                    {
                        UI.CreatePanel(ref element, PanelCreation, "0 0 0 0", $".0001 0.0001", $"0.0002 0.0002", true);
                        UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("WeaponAttachments", creation.CurrentCreationWeapon.name)}", 20, "0.05 .9", ".95 1", TextAnchor.MiddleCenter);
                        i = 0;
                        foreach (var attachment in gwData.Attachments)
                        {
                            string image = attachment.Value.URL;
                            var pos = CalcButtonPos(i);
                            if (!UnProcessedAttachment[player.userID].Contains(attachment.Key))
                            {
                                UI.LoadURLImage(ref element, PanelCreation, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                                UI.CreateButton(ref element, PanelCreation, "0 0 0 0", "", 12, $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", $"UI_SelectAttachment {attachment.Key}");
                            }
                            else
                            {
                                UI.CreatePanel(ref element, PanelCreation, UIColors["green"], $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", true);
                                UI.LoadURLImage(ref element, PanelCreation, image, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}");
                                var info = $"{attachment.Value.name}\nADDED";
                                UI.CreateLabel(ref element, PanelCreation, UIColors["white"], info, 14, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}", TextAnchor.MiddleCenter);
                                UI.CreateButton(ref element, PanelCreation, "0 0 0 0", info, 14, $"{pos[0] + 0.005f} {pos[1] + 0.005f}", $"{pos[2] - 0.005f} {pos[3] - 0.005f}", $"UI_SelectAttachment {attachment.Key}", TextAnchor.MiddleCenter);

                            }
                            i++;
                        }
                        UI.CreateButton(ref element, PanelCreation, UIColors["buttonbg"], GetLang("Done"), 18, "0.2 0.05", "0.4 0.15", $"UI_SelectAttachment DONE", TextAnchor.MiddleCenter);
                    }
                    else
                    {
                        creation.stepNum = 8;
                        SetCreator(player, 8);
                        return;
                    }
                    break;
                case 8:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["black"], $"{TextColors["limegreen"]} {GetMSG("GearSlot", name)}", 20, "0.05 .9", ".95 1", TextAnchor.MiddleCenter);
                    foreach (var entry in list)
                    {
                        min = entry.Value;
                        Vector2 max = min + dimension;
                        Vector2 altmin = min + offset2;
                        Vector2 altmax = max - offset2;
                        Vector2 imgmin = min + (offset2 * 2);
                        Vector2 imgmax = max - (offset2 * 2);
                        UI.CreatePanel(ref element, PanelCreation, UIColors["CSorange"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                        UI.CreateButton(ref element, PanelCreation, "0 0 0 0", Enum.GetName(typeof(Slot), entry.Key).ToUpper(), 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_SelectSlot {Enum.GetName(typeof(Slot), entry.Key)}", TextAnchor.MiddleCenter);
                    }
                    break;
                default:
                    CuiHelper.DestroyUi(player, PanelCreation);
                    element = UI.CreateElementContainer(PanelCreation, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
                    UI.CreatePanel(ref element, PanelCreation, UIColors["light"], "0.01 0.02", "0.99 0.98");
                    UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], GetLang("CreationInfo"), 20, "0 .9", "1 1");
                    var items = "";
                    if (NewSet[player.userID].isWeapon == true)
                        foreach (var entry in NewSet[player.userID].wset.set)
                            items += $"Name: {entry.name} //--\\ Cost: {entry.price} //--\\ Kills Required:{entry.killsrequired}\n";
                    else
                        foreach (var entry in NewSet[player.userID].set.set)
                            items += $"Name: {entry.name} //--\\ Cost: {entry.price} //--\\ Kills Required:{entry.killsrequired}\n";
                    string CreationName = GetMSG("CreationName", creation.setname, creation.set.cost.ToString(), creation.set.killsrequired.ToString());
                    string CreationDetails = GetMSG("CreationDetails", items);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], CreationName, 20, "0.1 0.7", "0.9 0.89", TextAnchor.MiddleLeft);
                    UI.CreateLabel(ref element, PanelCreation, UIColors["limegreen"], CreationDetails, 20, "0.1 0.1", "0.9 0.65", TextAnchor.MiddleLeft);
                    UI.CreateButton(ref element, PanelCreation, UIColors["buttonbg"], GetLang("SaveSet"), 18, "0.2 0.05", "0.4 0.15", $"UI_SaveSet", TextAnchor.MiddleCenter);
                    UI.CreateButton(ref element, PanelCreation, UIColors["buttonred"], GetLang("Cancel"), 18, "0.6 0.05", "0.8 0.15", $"UI_ExitSetCreation");

                    break;
            }
            CuiHelper.AddUi(player, element);
        }

        void AdminMenu(BasePlayer player, string panel = "default")
        {
            CuiHelper.DestroyUi(player, PanelAdmin);
            var element = UI.CreateElementContainer(PanelAdmin, UIColors["dark"], "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelAdmin, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PanelAdmin, UIColors["header"], GetLang("AdminOptions"), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);


            UI.CreatePanel(ref element, PanelAdmin, UIColors["CSorange"], "0.25 0.35", "0.45 0.55", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("GearSetManagement"), 14, "0.27 0.365", "0.43 0.535", $"UI_GearSetManagement", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelAdmin, UIColors["CSorange"], "0.55 0.35", "0.75 0.55", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("WeaponSetManagement"), 14, "0.57 0.365", "0.73 0.535", $"UI_WeaponSetManagement", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], "0.4 0.04", "0.6 0.14", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("Close"), 12, "0.403 0.043", "0.597 0.138", "UI_DestroyAdminPanel", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void GearSetManagement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelAdmin);
            var i = 0;
            var element = UI.CreateElementContainer(PanelAdmin, UIColors["dark"], "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelAdmin, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PanelAdmin, UIColors["header"], GetLang("GearSetManagement"), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], $".1 .81", $".9 .96", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("CreateGearSet"), 22, $".105 .815", $".895 .955", $"UI_CreateGearSet", TextAnchor.MiddleCenter);

            foreach (var set in gwData.GearSets)
            {
                Vector2 min = new Vector2(0.025f, 0.70f);
                Vector2 max = new Vector2(0.49f, 0.80f);
                Vector2 offset1 = new Vector2(0f, 0.11f);
                Vector2 offset2 = new Vector2(0.002f, 0.003f);
                Vector2 offset3 = new Vector2(0f, 0.11f);
                offset1 = (offset1 * (i));
                if (i >= 6)
                {
                    min = new Vector2(0.51f, 0.70f);
                    max = new Vector2(0.975f, 0.80f);
                    offset1 = (offset3 * (i - 6));
                }
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = pos1 + offset2;
                Vector2 pos4 = pos2 - offset2;
                UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetMSG("DeleteSet", set.Key), 14, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_DeleteSet {set.Key} gear", TextAnchor.MiddleLeft);
                i++;
            }
            UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], "0.05 0.05", "0.25 0.125", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("Back"), 12, "0.052 0.053", "0.248 0.127", "UI_AdminPanel", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void WeaponSetManagement(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, PanelAdmin);
            var i = 0;
            var element = UI.CreateElementContainer(PanelAdmin, "0 0 0 0", "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelAdmin, UIColors["light"], "0.01 0.02", "0.99 0.98", true);
            UI.CreateLabel(ref element, PanelAdmin, UIColors["header"], GetLang("WeaponSetManagement"), 100, "0.01 0.01", "0.99 0.99", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], $".1 .81", $".9 .96", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("CreateWeaponSet"), 22, $".105 .815", $".895 .955", $"UI_CreateWeaponSet", TextAnchor.MiddleCenter);


            foreach (var set in gwData.WeaponSets)
            {
                Vector2 min = new Vector2(0.025f, 0.70f);
                Vector2 max = new Vector2(0.49f, 0.80f);
                Vector2 offset1 = new Vector2(0f, 0.11f);
                Vector2 offset2 = new Vector2(0.002f, 0.003f);
                Vector2 offset3 = new Vector2(0f, 0.11f);
                offset1 = (offset1 * (i));
                if (i >= 6)
                {
                    min = new Vector2(0.51f, 0.70f);
                    max = new Vector2(0.975f, 0.80f);
                    offset1 = (offset3 * (i - 6));
                }
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = pos1 + offset2;
                Vector2 pos4 = pos2 - offset2;
                UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetMSG("DeleteSet", set.Key), 14, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_DeleteSet {set.Key} weapon", TextAnchor.MiddleLeft);
                i++;
            }
            UI.CreatePanel(ref element, PanelAdmin, UIColors["buttonred"], "0.05 0.10", "0.25 0.2", true);
            UI.CreateButton(ref element, PanelAdmin, UIColors["black"], GetLang("Back"), 12, "0.052 0.103", "0.248 0.197", "UI_AdminPanel", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void PurchaseMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseMenu);
            var element = UI.CreateElementContainer(PanelPurchaseMenu, "0 0 0 0", "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["black"], "0.0 0.90", "1.0 1.0", true);
            string money = GetACPlayer(player).money.ToString();
            if (configData.UseServerRewards)
                if (CheckPoints(player.userID) is int)
                money = CheckPoints(player.userID).ToString();
            UI.CreateLabel(ref element, PanelPurchaseMenu, UIColors["CSorange"], GetMSG("PurchaseMenuPlayer", money, GetACPlayer(player).kills.ToString()), 24, "0.02 0.9", "0.9 0.99", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["black"], "0.0 0.0", "1.0 0.885", true);

            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["CSorange"], "0.25 0.35", "0.45 0.55", true);
            UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], GetLang("GearSets"), 14, "0.27 0.365", "0.43 0.535", $"UI_PurchasingPanel PGMENU none", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["CSorange"], "0.55 0.35", "0.75 0.55", true);
            UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], GetLang("WeaponSets"), 14, "0.57 0.365", "0.73 0.535", $"UI_PurchasingPanel PWMENU none", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["CSorange"], "0.05 0.10", "0.25 0.2", true);
            UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], $"{GetLang("Close")}", 14, "0.052 0.103", "0.248 0.197", "UI_DestroyPurchaseMenu", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void PurchaseMenuGearSets(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseMenu);
            var element = UI.CreateElementContainer(PanelPurchaseMenu, "0 0 0 1", "0.15 0.2", "0.29 0.6", true);
            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["light"], $"0.05 0.03", $".95 .97", true);
            UI.CreateLabel(ref element, PanelPurchaseMenu, UIColors["CSorange"], GetLang("GearSets"), 24, "0.1 0.86", "0.9 0.95", TextAnchor.MiddleCenter);
            var i = 0;
            foreach (var entry in gwData.GearSets)
            {
                Vector2 min = new Vector2(0.1f, 0.8f);
                Vector2 max = new Vector2(0.9f, 0.86f);
                Vector2 offset1 = new Vector2(0f, 0.065f);
                Vector2 offset2 = new Vector2(0.004f, 0.005f);
                offset1 = (offset1 * (i));
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = (pos1 + offset2);
                Vector2 pos4 = (pos2 - offset2);
                if (GetACPlayer(player).PlayerGearSets.ContainsKey(entry.Key))
                    UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["green"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                else UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["red"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], GetLang(entry.Key), 12, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_PurchasingPanel PGSMENU {entry.Key}", TextAnchor.MiddleCenter);
                i++;
            }

            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["CSorange"], "0.1 0.04", "0.9 0.09", true);
            UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], $"{GetLang("Close")}", 12, "0.104 0.045", "0.896 0.085", "UI_DestroyPurchaseMenu", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        void PurchaseSubMenuGearSets(BasePlayer player, string set = "")
        {
            if (set == "")
                set = GetACPlayer(player).currentGearSet;
            if (set == null) return;
            var extra = "";
            if (PendingPurchase.ContainsKey(player.userID))
                PendingPurchase.Remove(player.userID);
            PendingPurchase.Add(player.userID, new PurchaseItem { });
            CuiHelper.DestroyUi(player, PanelPurchaseScreen);
            var money = GetACPlayer(player).money;
            if (configData.UseServerRewards)
                if (CheckPoints(player.userID) is int)
                    money = (int)CheckPoints(player.userID);
            var element = UI.CreateElementContainer(PanelPurchaseScreen, "0 0 0 0", "0.3 0.2", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["black"], "0.0 0.9", "1.0 1.0", true);
            if (!GetACPlayer(player).PlayerGearSets.ContainsKey(set))
                extra = $"Balance: ${money}           Total Kills: {GetACPlayer(player).kills}";
            else
                extra = $"Balance: ${money}           Set Kills: {GetACPlayer(player).GearSetKills[set]}";
            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["CSorange"], $"{string.Format(GetLang("BuySubMenu"), set.ToUpper())}           {extra}", 20, "0.02 0.9", "0.9 0.99", TextAnchor.MiddleCenter);
            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["black"], "0.0 0.0", "1.0 0.85", true);
            string playerimage = gwData.SavedImages["General"][888888888.ToString()][0].ToString();
            UI.LoadImage(ref element, PanelPurchaseScreen, playerimage, $"0.2 0.1", "0.8 0.8");
            foreach (var entry in gwData.GearSets.Where(kvp => kvp.Key == set))
            {
                set = entry.Key;
                PendingPurchase[player.userID].gearpurchase = true;
                PendingPurchase[player.userID].setname = entry.Key;
                PendingPurchase[player.userID].setprice = entry.Value.cost;
                PendingPurchase[player.userID].setkillrequirement = entry.Value.killsrequired;
                foreach (var item in entry.Value.set)
                {
                    string info = "";
                    PendingPurchase[player.userID].gear.Add(item.shortname, item);
                    Vector2 min = new Vector2(0f, 0f);
                    Vector2 dimension = new Vector2(.2f, .15f);
                    Vector2 offset2 = new Vector2(0.002f, 0.003f);

                    if (GearSlotPos.ContainsKey(item.slot))
                    {
                        min = GearSlotPos[item.slot];
                    }
                    Vector2 max = min + dimension;
                    Vector2 altmin = min + offset2;
                    Vector2 altmax = max - offset2;
                    Vector2 imgmin = min + (offset2 * 2);
                    Vector2 imgmax = max - (offset2 * 2);
                    string image = gwData.SavedImages["General"][999999999.ToString()][0].ToString();
                    if (gwData.SavedImages.ContainsKey(set))
                        image = gwData.SavedImages[set][item.name][item.skin].ToString();
                    if (GetACPlayer(player).PlayerGearSets.ContainsKey(set))
                    {
                        var RequiredKills = 0;
                        if (item.slot == Slot.chest)
                            RequiredKills = GetACPlayer(player).kills;
                        else
                            RequiredKills = GetACPlayer(player).GearSetKills[set];
                        if (GetACPlayer(player).PlayerGearSets[set].Contains(item.shortname))
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["green"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\n(OWNED)";
                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["white"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                        }
                        else if (money >= item.price && RequiredKills >= item.killsrequired)
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["red"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                            UI.CreateButton(ref element, PanelPurchaseScreen, "0 0 0 0", info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_PrepPurchase none {item.shortname} none", TextAnchor.MiddleCenter);
                        }
                        else
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                        }
                    }
                    else
                    {
                        UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                        UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                        info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                        UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                    }
                }
            }

            if (!GetACPlayer(player).PlayerGearSets.ContainsKey(set))
            {
                if(money >= gwData.GearSets[set].cost && GetACPlayer(player).kills >= gwData.GearSets[set].killsrequired)
                {
                    UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["CSorange"], "0.05 0.71", "0.35 0.81", true);
                    UI.CreateButton(ref element, PanelPurchaseScreen, UIColors["black"], $"Cost: ${gwData.GearSets[set].cost} --=//=-- Kill Requirement: {gwData.GearSets[set].killsrequired}", 12, "0.052 0.713", "0.348 0.807", $"UI_PurchasingPanel PCONFIRM {set}", TextAnchor.MiddleCenter);

                }
                else
                {
                    UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["red"], "0.05 0.71", "0.35 0.81", true);
                    UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["white"], $"{GetLang(set)} - Cost: ${gwData.GearSets[set].cost} - Kill Requirement: {gwData.GearSets[set].killsrequired}", 12, "0.052 0.713", "0.348 0.807", TextAnchor.MiddleCenter);
                }
            }
            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["CSorange"], "0.05 0.10", "0.25 0.14", true);
            UI.CreateButton(ref element, PanelPurchaseScreen, UIColors["black"], $"{GetLang("Back")}", 12, "0.052 0.103", "0.248 0.137", "UI_PurchasingPanel PMENU none ", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        void PurchaseMenuWeaponSets(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseMenu);
            var element = UI.CreateElementContainer(PanelPurchaseMenu, "0 0 0 1", "0.15 0.2", "0.29 0.6", true);
            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["light"], $"0.05 0.03", $".95 .97", true);
            UI.CreateLabel(ref element, PanelPurchaseMenu, UIColors["CSorange"], GetLang("WeaponSets"), 24, "0.1 0.86", "0.9 0.95", TextAnchor.MiddleCenter);
            var i = 0;
            foreach (var entry in gwData.WeaponSets)
            {
                Vector2 min = new Vector2(0.1f, 0.8f);
                Vector2 max = new Vector2(0.9f, 0.86f);
                Vector2 offset1 = new Vector2(0f, 0.065f);
                Vector2 offset2 = new Vector2(0.004f, 0.005f);
                offset1 = (offset1 * (i));
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = (pos1 + offset2);
                Vector2 pos4 = (pos2 - offset2);
                if (GetACPlayer(player).PlayerWeaponSets.ContainsKey(entry.Key))
                    UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["green"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                else UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["red"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], GetLang(entry.Key), 12, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_PurchasingPanel PWSMENU {entry.Key}", TextAnchor.MiddleCenter);
                i++;
            }

            UI.CreatePanel(ref element, PanelPurchaseMenu, UIColors["CSorange"], "0.1 0.04", "0.9 0.09", true);
            UI.CreateButton(ref element, PanelPurchaseMenu, UIColors["black"], $"{GetLang("Close")}", 12, "0.104 0.045", "0.896 0.085", "UI_DestroyPurchaseMenu", TextAnchor.MiddleCenter);


            CuiHelper.AddUi(player, element);
        }

        void PurchaseSubMenuWeaponSets(BasePlayer player, string set = "")
        {
            if (set == null) set = GetACPlayer(player).PlayerWeaponSets.First().Key;
            if (set == "")
                set = GetACPlayer(player).CurrentWeapons.First().Key;
            var extra = "";
            if (PendingPurchase.ContainsKey(player.userID))
                PendingPurchase.Remove(player.userID);
            PendingPurchase.Add(player.userID, new PurchaseItem { });
            CuiHelper.DestroyUi(player, PanelPurchaseScreen);
            var money = GetACPlayer(player).money;
            if (configData.UseServerRewards)
                if (CheckPoints(player.userID) is int)
                    money = (int)CheckPoints(player.userID);
            var element = UI.CreateElementContainer(PanelPurchaseScreen, "0 0 0 0", "0.3 0.2", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["black"], "0.0 0.9", "1.0 1.0", true);
            if (!GetACPlayer(player).PlayerWeaponSets.ContainsKey(set))
                extra = $"Balance: ${money}           Total Kills: {GetACPlayer(player).kills}";
            else
                extra = $"Balance: ${money}           Weapon Kills: {GetACPlayer(player).WeaponSetKills[set]}";
            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["CSorange"], $"{string.Format(GetLang("BuySubMenu"), set.ToUpper())}           {extra}", 24, "0.02 0.9", "0.9 0.99", TextAnchor.MiddleCenter);
            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["black"], "0.0 0.0", "1.0 0.85", true);
            foreach (var entry in gwData.WeaponSets.Where(kvp => kvp.Key == set))
            {
                set = entry.Key;
                PendingPurchase[player.userID].weaponpurchase = true;
                PendingPurchase[player.userID].setname = entry.Key;
                PendingPurchase[player.userID].setprice = entry.Value.cost;
                PendingPurchase[player.userID].setkillrequirement = entry.Value.killsrequired;
                foreach (var item in entry.Value.set)
                {
                    string info = "";
                    PendingPurchase[player.userID].weapon.Add(item.shortname, item);
                    Vector2 min = new Vector2(0f, 0f);
                    Vector2 dimension = new Vector2(.2f, .15f);
                    Vector2 offset2 = new Vector2(0.002f, 0.003f);

                    if (WeaponSlotPos.ContainsKey(item.slot))
                    {
                        min = WeaponSlotPos[item.slot];
                    }

                    Vector2 max = min + dimension;
                    Vector2 altmin = min + offset2;
                    Vector2 altmax = max - offset2;
                    Vector2 imgmin = min + (offset2 * 2);
                    Vector2 imgmax = max - (offset2 * 2);
                    string image = gwData.SavedImages["General"][999999999.ToString()][0].ToString();
                    if (gwData.SavedImages.ContainsKey(set))
                        image = gwData.SavedImages[set][item.name][item.skin].ToString();
                    if (GetACPlayer(player).PlayerWeaponSets.ContainsKey(set))
                    {
                        var RequiredKills = 0;
                        if (item.slot == Slot.main)
                            RequiredKills = GetACPlayer(player).kills;
                        if (item.slot == Slot.secondary)
                            RequiredKills = GetACPlayer(player).WeaponSetKills[set];
                        if (GetACPlayer(player).PlayerWeaponSets[set].ContainsKey(item.shortname))
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["green"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\n(OWNED)";
                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["white"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                        }
                        else if (money >= item.price && RequiredKills >= item.killsrequired)
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["red"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                            UI.CreateButton(ref element, PanelPurchaseScreen, "0 0 0 0", info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_PrepPurchase none {item.shortname} none", TextAnchor.MiddleCenter);
                        }
                        else
                        {
                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                        }
                    }
                    else
                    {
                        UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                        UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                        info = $"{GetLang(item.name)}\nCost: ${item.price}\nRequired Kills: {item.killsrequired}";
                        UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                    }
                    if (item.attachments.Count > 0)
                    {
                        var m = 0;
                        var s = 0;
                        foreach (var attachment in item.attachments)
                        {
                            if (gwData.Attachments.ContainsKey(attachment))
                            {
                                if (item.slot == Slot.main)
                                {
                                    m++;
                                    if (m == 1) { min = new Vector2(.15f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (m == 2) { min = new Vector2(.255f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (m == 3) { min = new Vector2(.36f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (m == 4) { min = new Vector2(.15f, .299f); dimension = new Vector2(.1f, .1f); }
                                    if (m == 5) { min = new Vector2(.255f, .299f); dimension = new Vector2(.1f, .1f); }
                                    if (m == 6) { min = new Vector2(.36f, .299f); dimension = new Vector2(.1f, .1f); }
                                }
                                else if (item.slot == Slot.secondary)
                                {
                                    s++;
                                    if (s == 1) { min = new Vector2(.55f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (s == 2) { min = new Vector2(.655f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (s == 3) { min = new Vector2(.76f, .4f); dimension = new Vector2(.1f, .1f); }
                                    if (s == 4) { min = new Vector2(.55f, .299f); dimension = new Vector2(.1f, .1f); }
                                    if (s == 5) { min = new Vector2(.655f, .299f); dimension = new Vector2(.1f, .1f); }
                                    if (s == 6) { min = new Vector2(.76f, .299f); dimension = new Vector2(.1f, .1f); }
                                }
                                max = min + dimension;
                                altmin = min + offset2;
                                altmax = max - offset2;
                                imgmin = min + (offset2 * 2);
                                imgmax = max - (offset2 * 2);

                                if (gwData.SavedImages.ContainsKey(attachment))
                                    image = gwData.SavedImages[attachment][attachment][0].ToString();
                                if (GetACPlayer(player).PlayerWeaponSets.ContainsKey(set))
                                {
                                    if (GetACPlayer(player).PlayerWeaponSets[set].ContainsKey(item.shortname))
                                    {
                                        if (GetACPlayer(player).PlayerWeaponSets[set][item.shortname].Contains(attachment))
                                        {
                                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["green"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                            info = $"{GetLang(gwData.Attachments[attachment].name)}\n(OWNED)";
                                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["white"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                        }
                                        else if (money >= gwData.Attachments[attachment].cost && GetACPlayer(player).WeaponSetKills[set] >= gwData.Attachments[attachment].killsrequired)
                                        {
                                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["red"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                            info = $"{GetLang(gwData.Attachments[attachment].name)}\nCost: ${gwData.Attachments[attachment].cost}\nRequired Kills: {gwData.Attachments[attachment].killsrequired}";
                                            UI.CreateButton(ref element, PanelPurchaseScreen, "0 0 0 0", info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_PrepPurchase attachment {item.shortname} {attachment}", TextAnchor.MiddleCenter);
                                        }
                                        else
                                        {
                                            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                            UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                            info = $"{GetLang(gwData.Attachments[attachment].name)}\nCost: ${gwData.Attachments[attachment].cost}\nRequired Kills: {gwData.Attachments[attachment].killsrequired}";
                                            UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                        }
                                    }
                                    else
                                    {
                                        UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                        UI.LoadImage(ref element, PanelPurchaseScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                        info = $"{GetLang(gwData.Attachments[attachment].name)}\nCost: ${gwData.Attachments[attachment].cost}\nRequired Kills: {gwData.Attachments[attachment].killsrequired}";
                                        UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["red"], info, 16, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!GetACPlayer(player).PlayerWeaponSets.ContainsKey(set))
            {
                if (money >= gwData.WeaponSets[set].cost && GetACPlayer(player).kills >= gwData.WeaponSets[set].killsrequired)
                {
                    UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["CSorange"], "0.05 0.71", "0.35 0.81", true);
                    UI.CreateButton(ref element, PanelPurchaseScreen, UIColors["black"], $"Cost: ${gwData.WeaponSets[set].cost} --=//=-- Kill Requirement: {gwData.WeaponSets[set].killsrequired}", 12, "0.052 0.713", "0.348 0.807", $"UI_PurchasingPanel PCONFIRM {set}", TextAnchor.MiddleCenter);

                }
                else
                {
                    UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["red"], "0.05 0.71", "0.35 0.81", true);
                    UI.CreateLabel(ref element, PanelPurchaseScreen, UIColors["white"], $"{GetLang(set)} - Cost: ${gwData.WeaponSets[set].cost} - Kill Requirement: {gwData.WeaponSets[set].killsrequired}", 12, "0.052 0.713", "0.348 0.807", TextAnchor.MiddleCenter);
                }
            }

            UI.CreatePanel(ref element, PanelPurchaseScreen, UIColors["CSorange"], "0.05 0.10", "0.25 0.14", true);
            UI.CreateButton(ref element, PanelPurchaseScreen, UIColors["black"], $"{GetLang("Back")}", 12, "0.052 0.103", "0.248 0.137", "UI_PurchasingPanel PMENU none ", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        void SelectionMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelSelectionMenu);
            CuiHelper.DestroyUi(player, PanelSelectionScreen);
            var element = UI.CreateElementContainer(PanelSelectionMenu, "0 0 0 0", "0.35 0.3", "0.65 0.7", true);
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["black"], "0.0 0.90", "1.0 1.0", true);
            string money = GetACPlayer(player).money.ToString();
            if (configData.UseServerRewards)
                if (CheckPoints(player.userID) is int)
                    money = CheckPoints(player.userID).ToString();
            UI.CreateLabel(ref element, PanelSelectionMenu, UIColors["CSorange"], GetMSG("SelectionMenuPlayer", money, GetACPlayer(player).kills.ToString()), 24, "0.1 0.9", "0.9 0.99", TextAnchor.MiddleLeft);
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["black"], "0.0 0.0", "1.0 0.885", true);

            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["CSorange"], "0.25 0.35", "0.45 0.55", true);
            UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], GetLang("GearSets"), 12, "0.27 0.365", "0.43 0.535", $"UI_GearSelectionMenu", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["CSorange"], "0.55 0.35", "0.75 0.55", true);
            UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], GetLang("WeaponSets"), 12, "0.57 0.365", "0.73 0.535", $"UI_WeaponSelectionMenu", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["CSorange"], "0.05 0.10", "0.25 0.2", true);
            UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], $"{GetLang("Close")}", 14, "0.052 0.103", "0.248 0.197", "UI_DestroySelectionMenu", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void GearSelectionMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelSelectionMenu);
            var element = UI.CreateElementContainer(PanelSelectionMenu, "0 0 0 1", "0.15 0.2", "0.29 0.6", true);
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["light"], $"0.05 0.03", $".95 .97", true);
            UI.CreateLabel(ref element, PanelSelectionMenu, UIColors["CSorange"], GetLang("GearSets"), 24, "0.1 0.86", "0.9 0.95", TextAnchor.MiddleCenter);
            var i = 0;
            foreach (var entry in GetACPlayer(player).PlayerGearSets)
            {
                Vector2 min = new Vector2(0.1f, 0.8f);
                Vector2 max = new Vector2(0.9f, 0.86f);
                Vector2 offset1 = new Vector2(0f, 0.065f);
                Vector2 offset2 = new Vector2(0.004f, 0.005f);
                offset1 = (offset1 * (i));
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = (pos1 + offset2);
                Vector2 pos4 = (pos2 - offset2);
                UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["blue"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], entry.Key, 12, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_GearSelectionScreen {entry.Key}", TextAnchor.MiddleCenter);
                i++;
            }
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["CSorange"], "0.1 0.04", "0.9 0.09", true);
            UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], $"{GetLang("Close")}", 12, "0.104 0.045", "0.896 0.085", "UI_DestroySelectionMenu", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        void GearSelectionScreen(BasePlayer player, string set = "")
        {
            if (set == "")
                set = GetACPlayer(player).currentGearSet;
            if (set == null) return;
            var info = "";
            CuiHelper.DestroyUi(player, PanelSelectionScreen);
            var element = UI.CreateElementContainer(PanelSelectionScreen, "0 0 0 0", "0.3 0.2", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["black"], "0.0 0.9", "1.0 1.0", true);
            UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["CSorange"], GetMSG("currentGearSet", GetACPlayer(player).currentGearSet), 24, "0.1 0.9", "0.9 0.99", TextAnchor.MiddleCenter);
            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["black"], "0.0 0.0", "1.0 0.85", true);
            string playerimage = gwData.SavedImages["General"][888888888.ToString()][0].ToString();
            UI.LoadImage(ref element, PanelSelectionScreen, playerimage, $"0.2 0.1", "0.8 0.8");

            foreach (var entry in gwData.GearSets.Where(kvp => kvp.Key == set))
            {
                if (GetACPlayer(player).PlayerGearSets.ContainsKey(entry.Key))
                    foreach (var item in entry.Value.set)
                        if (GetACPlayer(player).PlayerGearSets[set].Contains(item.shortname))
                        {
                            Vector2 min = new Vector2(0f, 0f);
                            Vector2 dimension = new Vector2(.2f, .15f);
                            Vector2 offset2 = new Vector2(0.002f, 0.003f);

                            if (GearSlotPos.ContainsKey(item.slot))
                            {
                                min = GearSlotPos[item.slot];
                            }

                            Vector2 max = min + dimension;
                            Vector2 altmin = min + offset2;
                            Vector2 altmax = max - offset2;
                            Vector2 imgmin = min + (offset2 * 2);
                            Vector2 imgmax = max - (offset2 * 2);
                            string image = gwData.SavedImages["General"][999999999.ToString()][0].ToString();
                            if (gwData.SavedImages.ContainsKey(set))
                                image = gwData.SavedImages[set][item.name][item.skin].ToString();

                            UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}";
                            UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["white"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);

                        }
            }

            if (GetACPlayer(player).currentGearSet == set)
            {
                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["green"], "0.05 0.7", "0.35 0.8", true);
                UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["white"], GetMSG("currentGearSet", set.ToUpper()), 12, "0.052 0.703", "0.348 0.797", TextAnchor.MiddleCenter);
            }
            else
            {
                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["CSorange"], "0.05 0.7", "0.35 0.8", true);
                UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], $"Select Set: {set.ToUpper()}", 12, "0.052 0.703", "0.348 0.797", $"UI_ProcessSelection set {set}", TextAnchor.MiddleCenter);
            }

            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["CSorange"], "0.05 0.10", "0.25 0.14", true);
            UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], $"{GetLang("Back")}", 12, "0.052 0.103", "0.248 0.137", "UI_SelectionMenu", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        void WeaponSelectionMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelSelectionMenu);
            var element = UI.CreateElementContainer(PanelSelectionMenu, "0 0 0 1", "0.15 0.2", "0.29 0.6", true);
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["light"], $"0.05 0.03", $".95 .97", true);
            UI.CreateLabel(ref element, PanelSelectionMenu, UIColors["CSorange"], GetLang("WeaponSets"), 24, "0.1 0.86", "0.9 0.95", TextAnchor.MiddleCenter);

            var i = 0;
            foreach (var entry in GetACPlayer(player).PlayerWeaponSets)
            {
                Vector2 min = new Vector2(0.1f, 0.8f);
                Vector2 max = new Vector2(0.9f, 0.86f);
                Vector2 offset1 = new Vector2(0f, 0.065f);
                Vector2 offset2 = new Vector2(0.004f, 0.005f);
                offset1 = (offset1 * (i));
                Vector2 pos1 = (min - offset1);
                Vector2 pos2 = (max - offset1);
                Vector2 pos3 = (pos1 + offset2);
                Vector2 pos4 = (pos2 - offset2);
                UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["blue"], $"{pos1.x} {pos1.y}", $"{pos2.x} {pos2.y}", true);
                UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], entry.Key, 12, $"{pos3.x} {pos3.y}", $"{pos4.x} {pos4.y}", $"UI_WeaponSelectionScreen {entry.Key}", TextAnchor.MiddleCenter);
                i++;
            }
            UI.CreatePanel(ref element, PanelSelectionMenu, UIColors["CSorange"], "0.1 0.04", "0.9 0.09", true);
            UI.CreateButton(ref element, PanelSelectionMenu, UIColors["black"], $"{GetLang("Close")}", 12, "0.104 0.045", "0.896 0.085", "UI_DestroySelectionMenu", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
        }

        void WeaponSelectionScreen(BasePlayer player, string set = "")
        {
            if (set == null) return;
            var info = "";
            if (!WeaponSelection.ContainsKey(player.userID))
                WeaponSelection.Add(player.userID, new Dictionary<string, Dictionary<string, List<string>>>());
            CuiHelper.DestroyUi(player, PanelSelectionScreen);
            var currentweapon = "";
            foreach (var entry in GetACPlayer(player).CurrentWeapons)
            {
                currentweapon = entry.Key;
                break;
            }
            var element = UI.CreateElementContainer(PanelSelectionScreen, "0 0 0 0", "0.3 0.2", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["black"], "0.0 0.9", "1.0 1.0", true);
            UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["CSorange"], GetMSG("CurrentWeapons", currentweapon), 24, "0.1 0.9", "0.9 0.99", TextAnchor.MiddleCenter);
            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["black"], "0.0 0.0", "1.0 0.85", true);

            foreach (var entry in gwData.WeaponSets.Where(kvp => kvp.Key == set))
            {
                if (!WeaponSelection[player.userID].ContainsKey(set))
                {
                    WeaponSelection[player.userID].Clear();
                    WeaponSelection[player.userID].Add(entry.Key, new Dictionary<string, List<string>>());
                }
                if (GetACPlayer(player).PlayerWeaponSets.ContainsKey(entry.Key))
                {
                    foreach (var item in entry.Value.set)
                        if (GetACPlayer(player).PlayerWeaponSets[entry.Key].ContainsKey(item.shortname))
                        {
                            if (!WeaponSelection[player.userID][entry.Key].ContainsKey(item.shortname))
                            {
                                WeaponSelection[player.userID][entry.Key].Add(item.shortname, new List<string>());
                            }
                            Vector2 min = new Vector2(0f, 0f);
                            Vector2 dimension = new Vector2(.2f, .15f);
                            Vector2 offset2 = new Vector2(0.002f, 0.003f);

                            if (WeaponSlotPos.ContainsKey(item.slot))
                            {
                                min = WeaponSlotPos[item.slot];
                            }

                            Vector2 max = min + dimension;
                            Vector2 altmin = min + offset2;
                            Vector2 altmax = max - offset2;
                            Vector2 imgmin = min + (offset2 * 2);
                            Vector2 imgmax = max - (offset2 * 2);
                            string image = gwData.SavedImages["General"][999999999.ToString()][0].ToString();
                            if (gwData.SavedImages.ContainsKey(set))
                                image = gwData.SavedImages[set][item.name][item.skin].ToString();

                            UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                            info = $"{GetLang(item.name)}";
                            UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["white"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                            if (item.attachments.Count > 0)
                            {
                                var m = 1;
                                var s = 1;
                                foreach (var attachment in item.attachments)
                                {
                                    if (gwData.Attachments.ContainsKey(attachment))
                                    {
                                        if (item.slot == Slot.main)
                                        {
                                            if (m == 1) { min = new Vector2(.15f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (m == 2) { min = new Vector2(.255f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (m == 3) { min = new Vector2(.36f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (m == 4) { min = new Vector2(.15f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (m == 5) { min = new Vector2(.255f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (m == 6) { min = new Vector2(.36f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (GetACPlayer(player).PlayerWeaponSets[set][item.shortname].Contains(attachment)) m++;
                                        }
                                        else if (item.slot == Slot.secondary)
                                        {
                                            if (s == 1) { min = new Vector2(.55f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (s == 2) { min = new Vector2(.655f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (s == 3) { min = new Vector2(.76f, .4f); dimension = new Vector2(.1f, .1f); }
                                            if (s == 4) { min = new Vector2(.55f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (s == 5) { min = new Vector2(.655f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (s == 6) { min = new Vector2(.76f, .299f); dimension = new Vector2(.1f, .1f); }
                                            if (GetACPlayer(player).PlayerWeaponSets[set][item.shortname].Contains(attachment)) s++;
                                        }
                                        max = min + dimension;
                                        altmin = min + offset2;
                                        altmax = max - offset2;
                                        imgmin = min + (offset2 * 2);
                                        imgmax = max - (offset2 * 2);
                                        image = gwData.SavedImages["General"][999999999.ToString()][0].ToString();
                                        if (gwData.SavedImages.ContainsKey(attachment))
                                            image = gwData.SavedImages[attachment][attachment][0].ToString();
                                        var weapon = ItemManager.Create(ItemManager.FindItemDefinition(item.shortname), 1, 0);
                                        if (GetACPlayer(player).PlayerWeaponSets[set][item.shortname].Contains(attachment))
                                        {
                                            if (!WeaponSelection[player.userID][entry.Key][item.shortname].Contains(attachment))
                                            {
                                                if (WeaponSelection[player.userID][entry.Key][item.shortname].Count < weapon.contents.capacity)
                                                {
                                                    if (WeaponSelection[player.userID][entry.Key][item.shortname].Count == 0)
                                                    {
                                                        UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                                        info = $"{GetLang(gwData.Attachments[attachment].name)}";
                                                        UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["white"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                                        UI.CreateButton(ref element, PanelSelectionScreen, "0 0 0 0", info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_ProcessAttachment add {item.shortname} {attachment} {set}", TextAnchor.MiddleCenter);
                                                    }
                                                    else
                                                        foreach (var a in WeaponSelection[player.userID][entry.Key][item.shortname])
                                                        {
                                                            if (gwData.Attachments[a].location != gwData.Attachments[attachment].location)
                                                            {
                                                                UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                                                info = $"{GetLang(gwData.Attachments[attachment].name)}";
                                                                UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["black"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                                                UI.CreateButton(ref element, PanelSelectionScreen, "0 0 0 0", info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_ProcessAttachment add {item.shortname} {attachment} {set}", TextAnchor.MiddleCenter);
                                                            }
                                                            else if (gwData.Attachments[a].location == gwData.Attachments[attachment].location)
                                                            {
                                                                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                                                UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                                                info = $"{GetLang(gwData.Attachments[attachment].name)}";
                                                                UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["red"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                                            }
                                                        }
                                                }
                                                else
                                                {
                                                    UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["grey"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                                    UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                                    info = $"{GetLang(gwData.Attachments[attachment].name)}";
                                                    UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["red"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                                }
                                            }
                                            else
                                            {
                                                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["green"], $"{min.x} {min.y}", $"{max.x} {max.y}", true);
                                                UI.LoadImage(ref element, PanelSelectionScreen, image, $"{imgmin.x} {imgmin.y}", $"{imgmax.x} {imgmax.y}");
                                                info = $"{GetLang(gwData.Attachments[attachment].name)}\nSelected";
                                                UI.CreateLabel(ref element, PanelSelectionScreen, UIColors["white"], info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", TextAnchor.MiddleCenter);
                                                UI.CreateButton(ref element, PanelSelectionScreen, "0 0 0 0", info, 14, $"{altmin.x} {altmin.y}", $"{altmax.x} {altmax.y}", $"UI_ProcessAttachment remove {item.shortname} {attachment} {set}", TextAnchor.MiddleCenter);
                                            }
                                        }
                                        if (WeaponSelection[player.userID][entry.Key][item.shortname].Count >= weapon.contents.capacity)
                                        {
                                            if (item.slot == Slot.main)
                                            {
                                                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["red"], "0.18 0.16", "0.45 0.2", true);
                                                UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], GetMSG("ClearAttachments", item.shortname), 14, "0.182 0.163", "0.447 0.197", $"UI_ProcessAttachment clear {item.shortname} none {set}", TextAnchor.MiddleCenter);
                                            }
                                            else if (item.slot == Slot.secondary)
                                            {
                                                UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["red"], "0.57 0.16", "0.84 0.2", true);
                                                UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], GetMSG("ClearAttachments", item.shortname), 14, "0.572 0.163", "0.837 0.197", $"UI_ProcessAttachment clear {item.shortname} none {set}", TextAnchor.MiddleCenter);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["CSorange"], "0.05 0.7", "0.35 0.8", true);
            UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], $"Select Set: {set.ToUpper()}", 12, "0.052 0.703", "0.348 0.797", $"UI_ProcessSelection weapon {set}", TextAnchor.MiddleCenter);

            UI.CreatePanel(ref element, PanelSelectionScreen, UIColors["CSorange"], "0.05 0.10", "0.25 0.14", true);
            UI.CreateButton(ref element, PanelSelectionScreen, UIColors["black"], $"{GetLang("Back")}", 12, "0.052 0.103", "0.248 0.137", "UI_SelectionMenu", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, element);
        }

        private void PurchaseConfirmation(BasePlayer player, string item)
        {
            var pending = PendingPurchase[player.userID];
            var itemname = item;
            var currentGearSet = item;
            var itemprice = pending.setprice.ToString();
            if (pending.gearpurchase == true)
            {
                if (pending.set == false)
                {
                    itemname = pending.gear[item].name;
                    itemprice = pending.gear[item].price.ToString();
                    currentGearSet = pending.setname;
                }
            }
            else if (pending.weaponpurchase == true)
            {
                if (pending.set == false)
                {
                    if (pending.attachmentpurchase == true)
                    {
                        itemname = gwData.Attachments[pending.attachmentName].name;
                        itemprice = gwData.Attachments[pending.attachmentName].cost.ToString();
                        currentGearSet = pending.setname;
                    }
                    else
                    {
                        itemname = pending.weapon[item].name;
                        itemprice = pending.weapon[item].price.ToString();
                        currentGearSet = pending.setname;
                    }
                }
            }
            CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
            var element = UI.CreateElementContainer(PanelPurchaseConfirmation, UIColors["dark"], "0.3 0.3", "0.7 0.9", true);
            UI.CreatePanel(ref element, PanelPurchaseConfirmation, UIColors["light"], "0.01 0.02", "0.99 0.98");
            UI.CreateLabel(ref element, PanelPurchaseConfirmation, MsgColor, string.Format(GetLang("PurchaseInfo"), itemname, itemprice), 18, "0.1 0.1", "0.9 0.89", TextAnchor.MiddleLeft);
            UI.CreateButton(ref element, PanelPurchaseConfirmation, UIColors["buttongreen"], "Yes", 18, "0.2 0.05", "0.39 0.15", $"UI_Purchase {item}");
            if (pending.gearpurchase == true)
                UI.CreateButton(ref element, PanelPurchaseConfirmation, UIColors["buttonred"], "No", 18, "0.4 0.05", "0.59 0.15", $"UI_PurchasingPanel PGSMENU {currentGearSet}");
            if (pending.weaponpurchase == true)
                UI.CreateButton(ref element, PanelPurchaseConfirmation, UIColors["buttonred"], "No", 18, "0.4 0.05", "0.59 0.15", $"UI_PurchasingPanel PWSMENU {currentGearSet}");
            CuiHelper.AddUi(player, element);
        }

        void OnScreen(BasePlayer player, string msg)
        {
            CuiHelper.DestroyUi(player, PanelOnScreen);
            var element = UI.CreateElementContainer(PanelOnScreen, "0.0 0.0 0.0 0.0", "0.4 0.4", "0.6 0.6", false);
            UI.CreateLabel(ref element, PanelOnScreen, UIColors["white"], msg, 32, "0.0 0.0", "1.0 1.0", TextAnchor.MiddleCenter);
            CuiHelper.AddUi(player, element);
            timer.Once(3, () => CuiHelper.DestroyUi(player, PanelOnScreen));
        }

		void PlayerHUD(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelStats);
            string money = GetACPlayer(player).money.ToString();
            var element = UI.CreateElementContainer(PanelStats, "0 0 0 0", "0.7 0.9", "0.95 1.0", false);
            UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud1", GetACPlayer(player).WeaponSetKills[GetACPlayer(player).CurrentWeapons.First().Key].ToString()), 12, "0.05 0.66", "0.49 0.99", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud2", GetACPlayer(player).GearSetKills[GetACPlayer(player).currentGearSet].ToString()), 12, "0.05 0.33", "0.49 0.66", TextAnchor.MiddleLeft);
            if (configData.UseServerRewards)
                if (CheckPoints(player.userID) is int)
                {
                    money = CheckPoints(player.userID).ToString();
                    UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud3a", money), 12, "0.05 0.0", "0.49 0.33", TextAnchor.MiddleLeft);
                }
                else
                    UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud3b", money), 12, "0.05 0.0", "0.49 0.33", TextAnchor.MiddleLeft);

            UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud4", GetACPlayer(player).CurrentWeapons.First().Key.ToUpper()), 12, "0.51 0.66", "0.95 0.99", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud5", GetACPlayer(player).currentGearSet.ToUpper()), 12, "0.51 0.33", "0.95 0.66", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref element, PanelStats, "1.0 1.0 1.0 1.0", GetMSG("Hud6", GetACPlayer(player).kills.ToString()),12, "0.51 0.0", "0.95 0.33", TextAnchor.MiddleLeft);
            CuiHelper.AddUi(player, element);
        }
		
        #endregion

        #region UI Calculations

        private float[] CalcButtonPos(int number)
        {
            Vector2 position = new Vector2(0.05f, 0.75f);
            Vector2 dimensions = new Vector2(0.15f, 0.15f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 5)
            {
                offsetX = (0.01f + dimensions.x) * number;
            }
            if (number > 4 && number < 10)
            {
                offsetX = (0.01f + dimensions.x) * (number - 5);
                offsetY = (-0.025f - dimensions.y) * 1;
            }
            if (number > 9 && number < 15)
            {
                offsetX = (0.01f + dimensions.x) * (number - 10);
                offsetY = (-0.025f - dimensions.y) * 2;
            }
            if (number > 14 && number < 20)
            {
                offsetX = (0.01f + dimensions.x) * (number - 15);
                offsetY = (-0.025f - dimensions.y) * 3;
            }
            if (number > 19 && number < 25)
            {
                offsetX = (0.01f + dimensions.x) * (number - 20);
                offsetY = (-0.025f - dimensions.y) * 4;
            }
            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
        }

        #endregion

        #region UI Commands

        [ConsoleCommand("UI_ToggleMenus")]
        private void cmdUI_TogglePurchaseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var panel = arg.Args[0];
            ToggleMenus(player, panel);
        }

        private void ToggleMenus(BasePlayer player, string panel)
        {
            if (SMState.Contains(player.userID) || PMState.Contains(player.userID) || AdminState.Contains(player.userID))
            {
                if (SMState.Contains(player.userID))
                {
                    DestroySelectionMenu(player);
                    if (panel == "s") return;
                }
                else if (PMState.Contains(player.userID))
                {
                    DestroyPurchaseMenu(player);
                    if (panel == "p") return;
                }
                else if (AdminState.Contains(player.userID))
                {
                    DestroyAdminPanel(player);
                    if (panel == "a") return;
                }
            }
            switch (panel)
            {
                case "s":
                    SMState.Add(player.userID);
                    SelectionMenu(player);
                    break;
                case "p":
                    PMState.Add(player.userID);
                    PurchasingPanel(player, "PMENU", "none");
                    break;
                case "a":
                    AdminState.Add(player.userID);
                    AdminMenu(player);
                    break;
            }
        }

        [ConsoleCommand("UI_DestroyPurchaseMenu")]
        private void cmdDestroyPurchaseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyPurchaseMenu(player);
        }

        [ConsoleCommand("UI_SelectionMenu")]
        private void cmdUI_SelectionMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            SelectionMenu(player);
        }

        [ConsoleCommand("UI_AdminPanel")]
        private void cmdUI_AdminPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AdminMenu(player);
        }

        [ConsoleCommand("UI_GearSetManagement")]
        private void cmdUI_GearSetManagement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            GearSetManagement(player);
        }

        [ConsoleCommand("UI_WeaponSetManagement")]
        private void cmdUI_WeaponSetManagement(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            WeaponSetManagement(player);
        }

        [ConsoleCommand("UI_DestroyAdminPanel")]
        private void cmdUI_DestroyAdminPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyAdminPanel(player);
        }

        [ConsoleCommand("UI_DestroySelectionMenu")]
        private void cmdDestroySelectionMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroySelectionMenu(player);
        }

        [ConsoleCommand("UI_GearSelectionMenu")]
        private void cmdGearSelectionMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            GearSelectionMenu(player);
            if (GetACPlayer(player).currentGearSet == null)
                GearSelectionScreen(player);
            else GearSelectionScreen(player, (GetACPlayer(player).currentGearSet));
        }


        [ConsoleCommand("UI_GearSelectionScreen")]
        private void cmdGearSelectionScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var set = MergeParams(arg.Args);
            GearSelectionScreen(player, set);
        }

        [ConsoleCommand("UI_WeaponSelectionMenu")]
        private void cmdWeaponSelectionMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            WeaponSelectionMenu(player);
            if (GetACPlayer(player).CurrentWeapons != null)
                    WeaponSelectionScreen(player, GetACPlayer(player).CurrentWeapons.First().Key);
            else
                WeaponSelectionScreen(player);
        }


        [ConsoleCommand("UI_WeaponSelectionScreen")]
        private void cmdWeaponSelectionScreen(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var set = MergeParams(arg.Args);
            WeaponSelectionScreen(player, set);
        }

        [ConsoleCommand("UI_ProcessAttachment")]
        private void cmdUI_ProcessAttachment(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var request = arg.Args[0];
            var weapon = arg.Args[1];
            var attachment = arg.Args[2];
            var set = MergeParams(arg.Args, 3);
            ProcessAttachment(player, request, set, weapon, attachment);
        }

        void ProcessAttachment(BasePlayer player, string request, string set, string weapon, string attachment)
        {
            switch (request)
            {
                case "clear":
                    WeaponSelection[player.userID][set][weapon].Clear();
                    WeaponSelectionScreen(player, set);
                    break;
                case "add":
                    WeaponSelection[player.userID][set][weapon].Add(attachment);
                    WeaponSelectionScreen(player, set);
                    break;
                case "remove":
                    WeaponSelection[player.userID][set][weapon].Remove(attachment);
                    WeaponSelectionScreen(player, set);
                    break;
            }
        }


        [ConsoleCommand("UI_ProcessSelection")]
        private void cmdUI_ProcessSelection(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var type = arg.Args[0];
            var set = MergeParams(arg.Args, 1);
            ProcessSelection(player, type, set);
        }

        void ProcessSelection(BasePlayer player, string type, string set)
        {
            switch (type)
            {
                case "set":
                    GetACPlayer(player).currentGearSet = set;
                    SelectSet(player, set);
                    break;
                case "weapon":
                    GetACPlayer(player).CurrentWeapons = WeaponSelection[player.userID];
                    SelectWeapons(player);
                    break;
            }
        }

        [ConsoleCommand("UI_PurchasingPanel")]
        private void cmdPurchasePanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var cmd = arg.Args[0];
            var item = MergeParams(arg.Args, 1);
            PurchasingPanel(player, cmd, item);
        }

        string MergeParams(string[] Params, int Start = 0)
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

        void PurchasingPanel(BasePlayer player, string panel, string item)
        {
            CuiHelper.DestroyUi(player, PanelPurchaseScreen);
            CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
            switch (panel)
            {
                case "PMENU":
                    PurchaseMenu(player);
                    break;
                case "PGMENU":
                    PurchaseMenuGearSets(player);
                    if (GetACPlayer(player).currentGearSet != null)
                        PurchaseSubMenuGearSets(player);
                    else
                        PurchaseSubMenuGearSets(player, GetACPlayer(player).currentGearSet);
                    break;
                case "PGSMENU":
                    PurchaseSubMenuGearSets(player, item);
                    break;
                case "PWMENU":
                    PurchaseMenuWeaponSets(player);
                    if (GetACPlayer(player).CurrentWeapons != null)
                    {
                            PurchaseSubMenuWeaponSets(player, GetACPlayer(player).CurrentWeapons.First().Key);
                            break;
                    }
                    else
                        PurchaseSubMenuWeaponSets(player, GetACPlayer(player).PlayerWeaponSets.First().Key);
                    break;
                case "PWSMENU":
                    PurchaseSubMenuWeaponSets(player, item);
                    break;
                case "PCONFIRM":
                    PurchaseConfirmation(player, item);
                    break;
            }
        }

        [ConsoleCommand("UI_PrepPurchase")]
        private void cmdUI_PrepPurchase(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var type = arg.Args[0];
            var set = arg.Args[1];
            var item = arg.Args[2];
            PendingPurchase[player.userID].set = false;
            if (type == "attachment")
            {
                PendingPurchase[player.userID].attachmentpurchase = true;
                PendingPurchase[player.userID].attachmentName = item;
            }
            PurchasingPanel(player, "PCONFIRM", set);
        }

        [ConsoleCommand("UI_Purchase")]
        private void cmdUI_Purchase(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var item = MergeParams(arg.Args);
            Purchase(player, item);
        }

        void Purchase(BasePlayer player, string item)
        {
            DestroyPurchaseMenu(player);
            var money = GetACPlayer(player).money;
            var pending = PendingPurchase[player.userID];
            if (pending.gearpurchase == true)
            {
                if (pending.set == false)
                {
                    GetACPlayer(player).PlayerGearSets[pending.setname].Add(item);
                    if (configData.UseServerRewards)
                        SRAction(player.userID, pending.gear[item].price, "REMOVE");
                    else
                        money -= pending.gear[item].price;
                    CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
                    OnScreen(player, GetMSG("purchaseitem", pending.gear[item].name, pending.setname));
                    timer.Once(3, () => PurchasingPanel(player, "PGSMENU", pending.setname));
                }
                else
                {
                    GetACPlayer(player).PlayerGearSets.Add(item, new List<string>());
                    if (configData.UseServerRewards)
                        SRAction(player.userID, gwData.GearSets[item].cost, "REMOVE");
                    else
                        money -= gwData.GearSets[item].cost;
                    GetACPlayer(player).GearSetKills.Add(item, 0);
                    PlayerHUD(player);
                    CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
                    OnScreen(player, GetMSG("purchaseset", item));
                    timer.Once(3, () => PurchasingPanel(player, "PGSMENU", item));
                }
            }
            else if (pending.weaponpurchase == true)
            {
                if (pending.set == false)
                {
                    if (pending.attachmentpurchase == true)
                    {
                        GetACPlayer(player).PlayerWeaponSets[pending.setname][item].Add(pending.attachmentName);
                        if (configData.UseServerRewards)
                            SRAction(player.userID, gwData.Attachments[pending.attachmentName].cost, "REMOVE");
                        else
                            money -= gwData.Attachments[pending.attachmentName].cost;
                        CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
                        OnScreen(player, GetMSG("purchaseattachment", gwData.Attachments[pending.attachmentName].name, pending.weapon[item].name));
                        timer.Once(3, () => PurchasingPanel(player, "PWSMENU", pending.setname));
                    }
                    else
                    {
                        GetACPlayer(player).PlayerWeaponSets[pending.setname].Add(item, new List<string>());
                        if (configData.UseServerRewards)
                            SRAction(player.userID, pending.weapon[item].price, "REMOVE");
                        else
                            money -= pending.weapon[item].price;
                        CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
                        OnScreen(player, GetMSG("purchaseweapon", pending.weapon[item].name, pending.setname));
                        timer.Once(3, () => PurchasingPanel(player, "PWSMENU", pending.setname));
                    }
                }
                else
                {
                    GetACPlayer(player).PlayerWeaponSets.Add(item, new Dictionary<string, List<string>>());
                    if (configData.UseServerRewards)
                        SRAction(player.userID, gwData.WeaponSets[item].cost, "REMOVE");
                    else
                        money -= gwData.WeaponSets[item].cost;
                    GetACPlayer(player).WeaponSetKills.Add(item, 0);
                    PlayerHUD(player);
                    CuiHelper.DestroyUi(player, PanelPurchaseConfirmation);
                    OnScreen(player, GetMSG("purchaseweaponset", item));
                    timer.Once(3, () => PurchasingPanel(player, "PWSMENU", item));
                }
            }
        }

        [ConsoleCommand("UI_DestroyUI")]
        private void cmdUI_DestroyUI(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyUI(player);
        }

        [ConsoleCommand("UI_SaveSet")]
        private void cmdUI_SaveSet(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            SaveSet(player, true);
        }

        [ConsoleCommand("UI_ExitSetCreation")]
        private void cmdUI_ExitSetCreation(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            ExitSetCreation(player, true);
        }

        [ConsoleCommand("UI_DestroyCreationPanel")]
        private void cmdUI_DestroyCreationPanel(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroyCreationPanel(player);
        }

        [ConsoleCommand("UI_CreateGearSet")]
        private void cmdUI_CreateGearSet(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CreateGearSet(player);
        }

        [ConsoleCommand("UI_CreateWeaponSet")]
        private void cmdUI_CreateWeaponSet(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            CreateWeaponSet(player);
        }

        [ConsoleCommand("UI_DeleteSet")]
        private void cmdUI_DeleteSet(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            var set = arg.Args[0];
            var type = arg.Args[1];
            if (type == "gear")
            {
                if (gwData.GearSets.ContainsKey(set))
                {
                    gwData.GearSets.Remove(set);
                    foreach (ACPlayer ac in ACPlayers)
                        CheckSets(ac);
                    GearSetManagement(player);
                }
            }
            if (type == "weapon")
            {
                if (gwData.WeaponSets.ContainsKey(set))
                {
                    gwData.WeaponSets.Remove(set);
                    foreach (ACPlayer ac in ACPlayers)
                        CheckSets(ac);
                    WeaponSetManagement(player);
                }
            }
        }

        

        [ConsoleCommand("UI_SelectAttachment")]
        private void cmdUI_SelectAttachment(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
           if (arg.Args[0] == "DONE")
            {
                NewSet[player.userID].CurrentCreationWeapon.attachments = UnProcessedAttachment[player.userID];
                NewSet[player.userID].stepNum = 8;
                SetCreator(player, 8);
                return;
            }
            if (UnProcessedAttachment[player.userID].Contains(arg.Args[0]))
            {
                UnProcessedAttachment[player.userID].Remove(arg.Args[0]);
                SetCreator(player, 7);
                return;
            }
            else
            {
                UnProcessedAttachment[player.userID].Add(arg.Args[0]);
                SetCreator(player, 7);
                return;
            }
        }


        [ConsoleCommand("UI_SelectSlot")]
        private void cmdUI_SelectSlot(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            Slot slot = (Slot)Enum.Parse(typeof(Slot), arg.Args[0]);
            if (NewSet[player.userID].isWeapon == true)
            {
                NewSet[player.userID].CurrentCreationWeapon.slot = slot;
                NewSet[player.userID].wset.set.Add(NewSet[player.userID].CurrentCreationWeapon);
                currentItemIndex++;
                GetSetWeapons(player);
            }
            else
            {
                NewSet[player.userID].CurrentCreationGear.slot = slot;
                NewSet[player.userID].set.set.Add(NewSet[player.userID].CurrentCreationGear);
                currentItemIndex++;
                GetSetGear(player);
            }
        }

        [ConsoleCommand("UI_SelectSkin")]
        private void cmdUI_SelectSkin(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            int skin;
            if (!int.TryParse(arg.Args[0], out skin)) return;
            else
            {
                if (NewSet[player.userID].isWeapon == true)
                {
                    NewSet[player.userID].CurrentCreationWeapon.skin = skin;
                    NewSet[player.userID].CurrentCreationWeapon.URL = urls[NewSet[player.userID].CurrentCreationWeapon.shortname][skin];
                    NewSet[player.userID].stepNum = 5;
                    SetCreator(player, 5);
                }
                else
                {
                    NewSet[player.userID].CurrentCreationGear.skin = skin;
                    NewSet[player.userID].CurrentCreationGear.URL = urls[NewSet[player.userID].CurrentCreationGear.shortname][skin];
                    NewSet[player.userID].stepNum = 5;
                    SetCreator(player, 5);
                }
            }
        }

        

        #endregion

        #region Item Management

        private void SelectSet(BasePlayer player, string name)
        {
            if (!PlayerGearSetTimer.ContainsKey(player.userID))
            {
                player.inventory.Strip();
                GiveSet(player);
                GiveWeapon(player);
                GiveBasicEquipment(player);
                PlayerHUD(player);
                DestroySelectionMenu(player);
                TimerPlayerGearSetselection(player);
            }
            else
            {
                GetSendMSG(player, "GearSetCooldown", GetACPlayer(player).currentGearSet);
                PlayerHUD(player);
            }
        }

        private void TimerPlayerGearSetselection(BasePlayer player)
        {
            if (PlayerGearSetTimer.ContainsKey(player.userID))
            {
                PlayerGearSetTimer.Remove(player.userID);
            }
            else PlayerGearSetTimer.Add(player.userID, timer.Once(configData.SetCooldown * 60, () => TimerPlayerGearSetselection(player)));
        }

        private void SelectWeapons(BasePlayer player)
        {
            if (!PlayerWeaponSetTimer.ContainsKey(player.userID))
            {
                player.inventory.Strip();
                GiveSet(player);
                GiveWeapon(player);
                GiveBasicEquipment(player);
                PlayerHUD(player);
                DestroySelectionMenu(player);
                TimerPlayerWeaponselection(player);
            }
            else
            {
                GetSendMSG(player, "WeaponSetCooldown", GetACPlayer(player).CurrentWeapons.First().Key);
                PlayerHUD(player);
            }
        }

        private void TimerPlayerWeaponselection(BasePlayer player)
        {
            if (PlayerWeaponSetTimer.ContainsKey(player.userID))
            {
                PlayerWeaponSetTimer.Remove(player.userID);
            }
            else PlayerWeaponSetTimer.Add(player.userID, timer.Once(configData.SetCooldown * 60, () => TimerPlayerGearSetselection(player)));
        }

        private void GiveSet(BasePlayer player)
        {
            if (GetACPlayer(player).currentGearSet == null) return;
            var set = gwData.GearSets[GetACPlayer(player).currentGearSet];
            foreach (var item in set.set)
            {
                if (GetACPlayer(player).PlayerGearSets[GetACPlayer(player).currentGearSet].Contains(item.shortname))
                    GiveItem(player, BuildSet(item), item.container);
            }
            PlayerHUD(player);
        }
        private void GiveWeapon(BasePlayer player)
        {
            if (GetACPlayer(player).CurrentWeapons == null) return;
            foreach (var set in GetACPlayer(player).CurrentWeapons)
            {
                foreach (var weapon in set.Value)
                {
                    foreach (var entry in gwData.WeaponSets[set.Key].set.Where(kvp => kvp.shortname == weapon.Key))
                        GiveItem(player, BuildWeapon(entry, set.Key, player), entry.container);
                }
            }
            PlayerHUD(player);
        }

        private void GiveBasicEquipment(BasePlayer player)
        {
            foreach (var item in gwData.General)
                GiveItem(player, BuildItem(item.shortname, item.amount, item.skin), "belt");
            PlayerHUD(player);
        }

        private Item BuildWeapon(Weapon weapon, string set, BasePlayer player)
        {
            if (weapon == null) return null;
            var definition = ItemManager.FindItemDefinition(weapon.shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, weapon.amount, weapon.skin);
                if (item != null)
                {
                    var held = item.GetHeldEntity() as BaseProjectile;
                    if (held != null)
                    {
                        held.primaryMagazine.contents = held.primaryMagazine.capacity;
                        if (!string.IsNullOrEmpty(weapon.ammoType))
                        {
                            var ammoType = ItemManager.FindItemDefinition(weapon.ammoType);
                            if (ammoType != null)
                                held.primaryMagazine.ammoType = ammoType;
                            held.primaryMagazine.contents = held.primaryMagazine.capacity;
                        }
                    }
                    if (weapon.ammo != 0)
                        GiveItem(player, BuildAmmo(weapon.ammoType, weapon.ammo), "");
                    if (GetACPlayer(player).CurrentWeapons[set][weapon.shortname] == null) return item;
                    foreach (var attachment in GetACPlayer(player).CurrentWeapons[set][weapon.shortname])
                        if (weapon.attachments.Contains(attachment))
                        {
                            BuildItem(attachment)?.MoveToContainer(item.contents);
                        }
                }
                return item;
            }
            Puts("Error making item: " + weapon.shortname);
            return null;
        }

        private Item BuildAmmo(string shortname, int amount)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, amount, 0);
                if (item != null)
                    return item;
            }
            Puts("Error making item: " + shortname);
            return null;
        }

        private Item BuildSet(Gear gear)
        {
            var definition = ItemManager.FindItemDefinition(gear.shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, gear.amount, gear.skin);
                if (item != null)
                    return item;
            }
            Puts("Error making item: " + gear.shortname);
            return null;
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
            Puts("Error making attachment: " + shortname);
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

        public void CreateGearSet(BasePlayer player)
        {
            if (NewSet.ContainsKey(player.userID))
                NewSet.Remove(player.userID);
            currentItemIndex = -1;
            NewSet.Add(player.userID, new CreatorSet());
            DestroyAdminPanel(player);
            SetCreator(player, 0);
        }

        public void CreateWeaponSet(BasePlayer player)
        {
            if (NewSet.ContainsKey(player.userID))
                NewSet.Remove(player.userID);
            currentItemIndex = -1;
            NewSet.Add(player.userID, new CreatorSet());
            NewSet[player.userID].isWeapon = true;
            DestroyAdminPanel(player);
            SetCreator(player, 0);
        }   

        private IEnumerable<Gear> GetItems(ItemContainer container, string containerName)
        {
            return container.itemList.Select(item => new Gear
            {
                shortname = item.info.shortname,
                container = containerName,
                amount = item.amount,
                skin = item.skin,
            });
        }

        private IEnumerable<Weapon> GetWeapon(ItemContainer container, string containerName)
        {
            return container.itemList.Where(item => item.GetHeldEntity() as BaseProjectile || item.GetHeldEntity() as BaseMelee).Select(item => new Weapon
            {
                shortname = item.info.shortname,
                container = containerName,
                amount = item.amount,
                skin = item.skin,
                ammo = 128,
                ammoType = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? "",
            });
        }

        #endregion

        #region Classes
        [Serializable]
        class ACPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int kills;
            public int deaths;
            public int money;
            //public bool HomeSaved;
            //Vector3 Home;
            public Dictionary<string, List<string>> PlayerGearSets = new Dictionary<string, List<string>>();
            public Dictionary<string, Dictionary<string, List<string>>> PlayerWeaponSets = new Dictionary<string, Dictionary<string, List<string>>>();
            public string currentGearSet;
            public Dictionary<string, Dictionary<string, List<string>>> CurrentWeapons = new Dictionary<string, Dictionary<string, List<string>>>();
            public Dictionary<string, int> GearSetKills = new Dictionary<string, int>();
            public Dictionary<string, int> WeaponSetKills = new Dictionary<string, int>();

            void Awake()
            {
                enabled = false;
                player = GetComponent<BasePlayer>();
            }

            //public void SaveHome()
            //{
            //    if (!HomeSaved)
            //        Home = player.transform.position;
            //    HomeSaved = true;
            //}
            //public void TeleportHome()
            //{
            //    if (!HomeSaved)
            //        return;
            //    TPPlayer(player, Home);
            //    HomeSaved = false;
            //}
        }

        enum Slot
        {
            head,
            chest,
            chest2,
            legs,
            legs2,
            feet,
            hands,
            main,
            secondary
        }

        class Gear_Weapon_Data
        {
            public Dictionary<string, GearSet> GearSets = new Dictionary<string, GearSet>();
            public Dictionary<string, WeaponSet> WeaponSets = new Dictionary<string, WeaponSet>();
            public Dictionary<string, Attachment> Attachments = new Dictionary<string, Attachment>();
            public List<Gear> General = new List<Gear>();
            public Dictionary<string, Dictionary<string, Dictionary<int, uint>>> SavedImages = new Dictionary<string, Dictionary<string, Dictionary<int, uint>>>();
        }

        class SavedPlayers
        {
            public Dictionary<ulong, SavedPlayer> players = new Dictionary<ulong, SavedPlayer>();
            public Dictionary<ulong, SavedPlayer> priorSave = new Dictionary<ulong, SavedPlayer>();
        }

        class SavedPlayer
        {
            public int kills;
            public int deaths;
            public int money;
            public string lastSet;
            public Dictionary<string, Dictionary<string, List<string>>> lastWeapons = new Dictionary<string, Dictionary<string, List<string>>>();
            public Dictionary<string, List<string>> PlayerGearSets = new Dictionary<string, List<string>>();
            public Dictionary<string, Dictionary<string, List<string>>> PlayerWeaponSets = new Dictionary<string, Dictionary<string, List<string>>>();
            public Dictionary<string, int> GearSetKills = new Dictionary<string, int>();
            public Dictionary<string, int> WeaponSetKills = new Dictionary<string, int>();
        }

        class GearSet
        {
            public int cost;
            public int killsrequired;
            public List<Gear> set = new List<Gear>();
        }

        class CreatorSet
        {
            public string setname;
            public GearSet set = new GearSet();
            public Gear CurrentCreationGear = new Gear();
            public WeaponSet wset = new WeaponSet();
            public Weapon CurrentCreationWeapon = new Weapon();
            public bool isWeapon = false;
            public int stepNum;
        }

        class WeaponSet
        {
            public int cost;
            public int killsrequired;
            public List<Weapon> set = new List<Weapon>();
        }


        class Attachment
        {
            public string name;
            public string shortname;
            public int cost;
            public int killsrequired;
            public string location;
            public string URL;
        }

        class Requirements
        {
            public int cost;
            public int killsrequired;

        }

        class PlayerInv
        {
            public int itemid;
            public int skin;
            public string name;
            public string container;
            public int amount;
            public float condition;
            public int ammo;
            public bool approved;
            public List<string> attachments = new List<string>();
        }

        class PurchaseItem
        {
            public Dictionary<string, Gear> gear = new Dictionary<string, Gear>();
            public Dictionary<string, Weapon> weapon = new Dictionary<string, Weapon>();
            public string setname;
            public bool set = true;
            public bool weaponpurchase;
            public bool gearpurchase;
            public bool attachmentpurchase;
            public string attachmentName;
            public int setprice;
            public int setkillrequirement;
        }

        class Gear
        {
            public string name;
            public string shortname;
            public Slot slot;
            public int price;
            public int skin;
            public int killsrequired;
            public int amount;
            public string container;
            public string URL;
        }

        class Weapon
        {
            public string name;
            public string shortname;
            public int skin;
            public Slot slot;
            public string container;
            public int killsrequired;
            public int price;
            public string URL;
            public int amount;
            public int ammo;
            public string ammoType;
            public List<string> attachments = new List<string>();
        }

        #endregion

        #region Unity WWW
        class QueueItem
        {
            public string url;
            public string set;
            public string itemid;
            public int skinid;
            public QueueItem(string ur, string st, string na, int sk)
            {
                url = ur;
                set = st;
                itemid = na;
                skinid = sk;
            }
        }
        class UnityWeb : MonoBehaviour
        {
            AbsolutCombat filehandler;
            const int MaxActiveLoads = 3;
            static readonly List<QueueItem> QueueList = new List<QueueItem>();
            static byte activeLoads;
            private MemoryStream stream = new MemoryStream();

            public void SetDataDir(AbsolutCombat ac) => filehandler = ac;
            public void Add(string url, string set, string itemid, int skinid)
            {
                QueueList.Add(new QueueItem(url, set, itemid, skinid));
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

            IEnumerator WaitForRequest(WWW www, QueueItem info)
            {
                yield return www;

                if (www.error == null)
                {
                    if (!filehandler.gwData.SavedImages.ContainsKey(info.set))
                        filehandler.gwData.SavedImages.Add(info.set, new Dictionary<string, Dictionary<int, uint>>());
                    if (!filehandler.gwData.SavedImages[info.set].ContainsKey(info.itemid.ToString()))
                        filehandler.gwData.SavedImages[info.set].Add(info.itemid.ToString(), new Dictionary<int, uint>());
                    if (!filehandler.gwData.SavedImages[info.set][info.itemid.ToString()].ContainsKey(info.skinid))
                    {
                        ClearStream();
                        stream.Write(www.bytes, 0, www.bytes.Length);
                        uint textureID = FileStorage.server.Store(stream, FileStorage.Type.png, uint.MaxValue);
                        ClearStream();
                        filehandler.gwData.SavedImages[info.set][info.itemid.ToString()].Add(info.skinid, textureID);
                    }
                }
                activeLoads--;
                if (QueueList.Count > 0) Next();
                else filehandler.SaveData();
            }
        }
        [ConsoleCommand("loadimages")]
        private void cmdLoadImages(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null)
            {
                LoadImages();
            }
        }

        private void LoadImages()
        {
            gwData.SavedImages.Clear();
            uWeb.Add("http://i.imgur.com/zq9zuKw.jpg","General", "999999999", 0);
            uWeb.Add("http://imgur.com/Im6J2HJ.jpg", "General", "888888888", 0);
            foreach (var entry in gwData.GearSets)
            {
                foreach (var item in entry.Value.set)
                {
                    if (!string.IsNullOrEmpty(item.URL))
                    {
                        var url = item.URL;
                        uWeb.Add(url, entry.Key, item.name, item.skin);
                    }
                }
            }
            foreach (var entry in gwData.WeaponSets)
            {
                foreach (var item in entry.Value.set)
                {
                    if (!string.IsNullOrEmpty(item.URL))
                    {
                        var url = item.URL;
                        uWeb.Add(url, entry.Key, item.name, item.skin);
                    }
                }
            }
            foreach (var entry in gwData.Attachments)
            {
                if (!string.IsNullOrEmpty(entry.Value.URL))
                {
                    var url = entry.Value.URL;
                    uWeb.Add(url, entry.Key, entry.Value.shortname, 0);
                }
            }
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
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                GetSendMSG(p, "ACInfo", configData.PurchaseMenuKeyBinding.ToUpper(), configData.SelectionMenuKeyBinding.ToUpper());
            }
            timers.Add("info", timer.Once(900, () => InfoLoop()));
        }

        private void CondLoop()
        {
            if (timers.ContainsKey("cond"))
            {
                timers["cond"].Destroy();
                timers.Remove("cond");
            }
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                p.metabolism.calories.value = 500;
                p.metabolism.hydration.value = 250;
            }
            timers.Add("cond", timer.Once(120, () => CondLoop()));
        }


        #endregion

        #region External Hooks

        object AddMoney(ulong TargetID, int amount, bool notify = true, ulong RequestorID = 0)
        {
            try
            {
                BasePlayer target = BasePlayer.FindByID(TargetID);
                if (GetACPlayer(target) != null)
                {
                    GetACPlayer(target).money += amount;
                    if (notify)
                        GetSendMSG(target, "AddMoney", amount.ToString());
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "MoneyAdded", target.displayName, amount.ToString());
                    }
                    return true;
                }
                else if (playerData.players.ContainsKey(TargetID))
                {
                    playerData.players[TargetID].money += amount;
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "MoneyAddedOffline", amount.ToString());
                    }
                    return true;
                }
                else
                {
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "NotACPlayer");
                    }
                    return null;
                }
            }
            catch
            {
                if (RequestorID != 0)
                {
                    BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                    GetSendMSG(requestor, "AddMoneyError");
                }
                Puts(GetLang("AddMoneyError"));
                return null;
            }
        }


        object TakeMoney(ulong TargetID, int amount, bool notify = true, ulong RequestorID = 0)
        {
            try
            {
                BasePlayer target = BasePlayer.FindByID(TargetID);
                if (GetACPlayer(target) != null)
                {
                    GetACPlayer(target).money -= amount;
                    if (notify)
                        GetSendMSG(target, "TakeMoney", amount.ToString());
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "MoneyTaken", target.displayName, amount.ToString());
                    }
                    return true;
                }
                else if (playerData.players.ContainsKey(TargetID))
                {
                    playerData.players[TargetID].money -= amount;
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "MoneyTakenOffline", amount.ToString());
                    }
                    return true;
                }
                else
                {
                    if (RequestorID != 0)
                    {
                        BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                        GetSendMSG(requestor, "NotACPlayer");
                    }
                    return null;
                }
            }
            catch
            {
                if (RequestorID != 0)
                {
                    BasePlayer requestor = BasePlayer.FindByID(RequestorID);
                    GetSendMSG(requestor, "TakeMoneyError");
                }
                Puts(GetLang("TakeMoneyError"));
                return null;
            }
        }

        #endregion

        #region GWData Management
        private Dictionary<string, Attachment> DefaultAttachments = new Dictionary<string, Attachment>
        {
                        { "weapon.mod.muzzleboost", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Muzzle Boost",
                            shortname = "weapon.mod.muzzleboost",
                            location = "front",
                            URL = "http://vignette2.wikia.nocookie.net/play-rust/images/7/7d/Muzzle_Boost_icon.png/revision/latest/scale-to-width-down/50?cb=20160601121705",
                        }
                        },
                        { "weapon.mod.flashlight", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Flash Light",
                            shortname = "weapon.mod.flashlight",
                            location = "middle",
                            URL = "http://vignette3.wikia.nocookie.net/play-rust/images/0/0d/Weapon_Flashlight_icon.png/revision/latest/scale-to-width-down/50?cb=20160211201539",
                        }
                        },
                        { "weapon.mod.silencer", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Silencer",
                            shortname = "weapon.mod.silencer",
                            location = "front",
                            URL = "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Silencer_icon.png/revision/latest/scale-to-width-down/50?cb=20160211200615",
                            }
                        },
                        { "weapon.mod.muzzlebrake", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Muzzle Brake",
                            shortname = "weapon.mod.muzzlebrake",
                            location = "front",
                            URL = "http://vignette2.wikia.nocookie.net/play-rust/images/3/38/Muzzle_Brake_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121719",
                            }
                        },
                        { "weapon.mod.small.scope", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "4x Scope",
                            shortname = "weapon.mod.small.scope",
                            location = "back",
                            URL = "http://vignette4.wikia.nocookie.net/play-rust/images/9/9c/4x_Zoom_Scope_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201610",
                            }
                        },
                        { "weapon.mod.lasersight", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Laser Sight",
                            shortname = "weapon.mod.lasersight",
                            location = "middle",
                            URL = "http://vignette1.wikia.nocookie.net/play-rust/images/8/8e/Weapon_Lasersight_icon.png/revision/latest/scale-to-width-down/50?cb=20160211201545",
                            }
                        },
                        { "weapon.mod.holosight", new Attachment
                        {
                            cost = 0,
                            killsrequired = 0,
                            name = "Holo Sight",
                            shortname = "weapon.mod.holosight",
                            location = "back",
                            URL = "http://vignette4.wikia.nocookie.net/play-rust/images/4/45/Holosight_icon.png/revision/latest/scale-to-width-down/50?cb=20160211200620",
                        }
                        }
        };

        private Dictionary<string, Dictionary<int, string>> urls = new Dictionary<string, Dictionary<int, string>>
        {
            {"tshirt", new Dictionary<int, string>
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
            {"pants", new Dictionary<int, string>
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
            {"shoes.boots", new Dictionary<int, string>
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
             {"tshirt.long", new Dictionary<int, string>
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
             {"mask.bandana", new Dictionary<int, string>
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
             {"mask.balaclava", new Dictionary<int, string>
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
             {"jacket.snow", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/04/Snow_Jacket_-_Red_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200536" },
                {10082, "http://vignette3.wikia.nocookie.net/play-rust/images/7/75/60%27s_Army_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200741"},
                {10113, "http://vignette2.wikia.nocookie.net/play-rust/images/e/ed/Black_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201110" },
                {10083, "http://vignette4.wikia.nocookie.net/play-rust/images/8/89/Salvaged_Shirt%2C_Coat_and_Tie_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200642"},
                {10112, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c9/Woodland_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201105" },
            }
            },
             {"jacket", new Dictionary<int, string>
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
            {"hoodie", new Dictionary<int, string>
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
            {"hat.cap", new Dictionary<int, string>
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
            {"hat.beenie", new Dictionary<int, string>
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
            {"bucket.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a5/Bucket_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200541" },
                {10127, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1c/Medic_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201521"},
                {10126, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Wooden_Bucket_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201438" },
            }
            },
            {"burlap.gloves", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/a/a1/Leather_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200306" },
                {10128, "http://vignette4.wikia.nocookie.net/play-rust/images/b/b5/Boxer%27s_Bandages_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201502"},
            }
            },
            {"burlap.shirt", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/d/d7/Burlap_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200455" },
                {10136, "http://vignette1.wikia.nocookie.net/play-rust/images/7/77/Pirate_Vest_%26_Shirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204350"},
            }
            },
            {"hat.boonie", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/8/88/Boonie_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200347" },
                {10058, "http://vignette4.wikia.nocookie.net/play-rust/images/1/12/Farmer_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195725"},
            }
            },
            {"pistol.revolver", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/58/Revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092929" },
                {10114, "http://vignette1.wikia.nocookie.net/play-rust/images/5/51/Outback_revolver_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092935"},
            }
            },
            {"pistol.semiauto", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6b/Semi-Automatic_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200319" },
                {10087, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Contamination_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200655"},
                {10108, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c3/Halloween_Bat_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201053" },
                {10081, "http://vignette1.wikia.nocookie.net/play-rust/images/7/70/Reaper_Note_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200711"},
                {10073, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Red_Shine_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200630" },
            }
            },
            {"rifle.ak", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/d/d1/Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200609" },
                {10135, "http://vignette2.wikia.nocookie.net/play-rust/images/9/9e/Digital_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225138"},
                {10137, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9f/Military_Camo_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211225144" },
                {10138, "http://vignette1.wikia.nocookie.net/play-rust/images/a/a1/Tempered_AK47_icon.png/revision/latest/scale-to-width-down/100?cb=20160211204335"},
            }
            },
            {"rifle.bolt", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/55/Bolt_Action_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200415" },
                {10117, "http://vignette2.wikia.nocookie.net/play-rust/images/2/22/Dreamcatcher_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234844"},
                {10115, "http://vignette1.wikia.nocookie.net/play-rust/images/9/9e/Ghost_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234902" },
                {10116, "http://vignette1.wikia.nocookie.net/play-rust/images/c/cf/Tundra_Bolt_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160214234858"},
            }
            },
            {"shotgun.pump", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/6/60/Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205718" },
                {10074, "http://vignette4.wikia.nocookie.net/play-rust/images/9/94/Chieftain_Pump_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20151106062100"},
                {10140, "http://vignette4.wikia.nocookie.net/play-rust/images/4/42/The_Swampmaster_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205830" },
            }
            },
            {"shotgun.waterpipe", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/1/1b/Waterpipe_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205730" },
                {10143, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4a/The_Peace_Pipe_icon.png/revision/latest/scale-to-width-down/100?cb=20160310205804"},
            }
            },
            {"rifle.lr300", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/d/d9/LR-300_Assault_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160825132402"},
            }
            },
            {"crossbow", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Crossbow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061004" },
            }
            },
            {"smg.thompson", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4e/Thompson_icon.png/revision/latest/scale-to-width-down/100?cb=20160226092921" },
                {10120, "http://vignette3.wikia.nocookie.net/play-rust/images/8/84/Santa%27s_Little_Helper_icon.png/revision/latest/scale-to-width-down/100?cb=20160225141743"},
            }
            },
            {"wood.armor.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/68/Wood_Armor_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061716" },
            }
            },
            {"wood.armor.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/4f/Wood_Chestplate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060921" },
            }
            },
            {"weapon.mod.small.scope", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/9/9c/4x_Zoom_Scope_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201610" },
            }
            },
            {"weapon.mod.silencer", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9f/Silencer_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200615" },
            }
            },
            {"weapon.mod.muzzlebrake", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/3/38/Muzzle_Brake_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121719" },
            }
            },
            {"weapon.mod.muzzleboost", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/7/7d/Muzzle_Boost_icon.png/revision/latest/scale-to-width-down/100?cb=20160601121705" },
            }
            },
            {"weapon.mod.lasersight", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/8e/Weapon_Lasersight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201545" },
            }
            },
            {"weapon.mod.holosight", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/45/Holosight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200620" },
            }
            },
            {"weapon.mod.flashlight", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/0d/Weapon_Flashlight_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201539" },
            }
            },
            {"spear.wooden", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/f/f2/Wooden_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060930" },
            }
            },
            {"spear.stone", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/0/0a/Stone_Spear_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061726" },
            }
            },
            {"smg.2", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/95/Custom_SMG_icon.png/revision/latest/scale-to-width-down/100?cb=20151108000740" },
            }
            },
            {"shotgun.double", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/3f/Double_Barrel_Shotgun_icon.png/revision/latest/scale-to-width-down/100?cb=20160816061211" },
            }
            },
            {"santahat", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4f/Santa_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151217230743" },
            }
            },
            {"salvaged.sword", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/7/77/Salvaged_Sword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061458" },
            }
            },
            {"salvaged.cleaver", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/7/7e/Salvaged_Cleaver_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054417" },
            }
            },
            {"rocket.launcher", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/0/06/Rocket_Launcher_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061852" },
            }
            },
            {"roadsign.kilt", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/31/Road_Sign_Kilt_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200530" },
            }
            },
            {"roadsign.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/8/84/Road_Sign_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054408" },
            }
            },
            {"riot.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/4e/Riot_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060949" },
            }
            },
            {"rifle.semiauto", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/8/8d/Semi-Automatic_Rifle_icon.png/revision/latest/scale-to-width-down/100?cb=20160128160721" },
            }
            },
            {"pistol.eoka", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/b/b5/Eoka_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061104" },
            }
            },
            {"metal.plate.torso", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/9/9d/Metal_Chest_Plate_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061201" },
            }
            },
            {"metal.facemask", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1f/Metal_Facemask_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061432" },
            }
            },
            {"machete", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/3/34/Machete_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060741" },
            }
            },
            {"mace", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/4/4d/Mace_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061207" },
            }
            },
            {"longsword", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/3/34/Longsword_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061240" },
            }
            },
            {"lmg.m249", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/c/c6/M249_icon.png/revision/latest/scale-to-width-down/100?cb=20151112221315" },
            }
            },
            {"knife.bone", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/Bone_Knife_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061357" },
            }
            },
            {"hazmat.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/6/6a/Hazmat_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060831" },
            }
            },
            {"hazmat.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/2/23/Hazmat_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054359" },
            }
            },
            {"hazmat.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/5/53/Hazmat_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061437" },
            }
            },
            {"hazmat.gloves", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/a/aa/Hazmat_Gloves_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061629" },
            }
            },
            {"hazmat.boots", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/8/8a/Hazmat_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060906" },
            }
            },
            {"hat.miner", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/1/1b/Miners_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060851" },
            }
            },
            {"hat.candle", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/a/ad/Candle_Hat_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061731" },
            }
            },
            {"flamethrower", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/5/55/Flame_Thrower_icon.png/revision/latest/scale-to-width-down/100?cb=20160415084104" },
            }
            },
            {"coffeecan.helmet", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/4/44/Coffee_Can_Helmet_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061305" },
            }
            },
            {"burlap.trousers", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/e/e5/Burlap_Trousers_icon.png/revision/latest/scale-to-width-down/100?cb=20151106054430" },
            }
            },
            {"burlap.shoes", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/10/Burlap_Shoes_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061222" },
            }
            },
            {"burlap.headwrap", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/c/c4/Burlap_Headwrap_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061044" },
            }
            },
            {"bow.hunting", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hunting_Bow_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060745" },
            }
            },
            {"bone.club", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/1/19/Bone_Club_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060940" },
            }
            },
            {"bone.armor.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/4/49/Bone_Armor_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061828" },
            }
            },
            {"bone.armor.jacket", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/f/f7/Bone_Jacket_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061054" },
            }
            },
            {"attire.hide.vest", new Dictionary<int, string>
            {
                {0, "http://vignette3.wikia.nocookie.net/play-rust/images/c/c0/Hide_Vest_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061337" },
            }
            },
            {"attire.hide.skirt", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/9/91/Hide_Skirt_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065030" },
            }
            },
            {"attire.hide.poncho", new Dictionary<int, string>
            {
                {0, "http://vignette4.wikia.nocookie.net/play-rust/images/7/7f/Hide_Poncho_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061141" },
            }
            },
            {"attire.hide.pants", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/e/e4/Hide_Pants_icon.png/revision/latest/scale-to-width-down/100?cb=20151106061352" },
            }
            },
            {"attire.hide.helterneck", new Dictionary<int, string>
            {
                {0, "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hide_Halterneck_icon.png/revision/latest/scale-to-width-down/100?cb=20160513065021" },
            }
            },
            {"attire.hide.boots", new Dictionary<int, string>
            {
                {0, "http://vignette1.wikia.nocookie.net/play-rust/images/5/57/Hide_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20151106060954" },
            }
            }
        };

        private Dictionary<string, GearSet> DefaultGearSets = new Dictionary<string, GearSet>
                {
                    {"Starter", new GearSet{cost = 0, killsrequired = 0, set = new List<Gear>
                    { new Gear
                        {
                        shortname = "tshirt",
                        skin = 10056,
                        slot = Slot.chest,
                        container = "wear",
                        amount = 1,
                        killsrequired = 0,
                        price = 0,
                        name = "Starter Shirt",
                        URL = "",
                        },
                        new Gear
                        {
                       shortname = "pants",
                        skin = 10001,
                        slot = Slot.legs,
                        container = "wear",
                        amount = 1,
                        killsrequired = 0,
                        price = 0,
                        name = "Starter Pants",
                        URL = "",
                        },
                        new Gear
                        {
                        shortname = "shoes.boots",
                        skin = 10044,
                        slot = Slot.feet,
                        container = "wear",
                        amount = 1,
                        killsrequired = 0,
                        price = 0,
                        name = "Starter Shoes",
                        URL = "http://vignette2.wikia.nocookie.net/play-rust/images/5/5b/Scavenged_Sneaker_Boots_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195530",
                        },
                        new Gear
                        {
                       shortname = "hat.cap",
                        skin = 10055,
                        slot = Slot.head,
                        container = "wear",
                        amount = 1,
                        killsrequired = 0,
                        price = 0,
                        name = "Starter Hat",
                        URL = "http://vignette2.wikia.nocookie.net/play-rust/images/4/4e/Friendly_Cap_icon.png/revision/latest/scale-to-width-down/100?cb=20160211195606",
                        },
                        new Gear
                        {
                        shortname = "burlap.gloves",
                        skin = 10128,
                        slot = Slot.hands,
                        container = "wear",
                        amount = 1,
                        killsrequired = 0,
                        price = 0,
                        name = "Starter Gloves",
                        URL = "http://vignette4.wikia.nocookie.net/play-rust/images/b/b5/Boxer%27s_Bandages_icon.png/revision/latest/scale-to-width-down/100?cb=20160211201502"
                        }
                     } } },
                {"First", new GearSet{cost = 0, killsrequired = 10, set = new List<Gear>
                    { new Gear
                        {
                        shortname = "hoodie",
                        skin = 10129,
                        slot = Slot.chest,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Hoodie",
                        },
                        new Gear
                        {
                       shortname = "pants",
                        skin = 10001,
                        slot = Slot.legs,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Pants",
                        },
                        new Gear
                        {
                        shortname = "shoes.boots",
                        skin = 10023,
                        slot = Slot.feet,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Boots",
                        },
                        new Gear
                        {
                       shortname = "coffeecan.helmet",
                        skin = 0,
                        slot = Slot.head,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Helmet",
                        },
                        new Gear
                        {
                        shortname = "wood.armor.pants",
                        skin = 0,
                        slot = Slot.legs2,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Wood Pants",
                        },
                        new Gear
                        {
                        shortname = "wood.armor.jacket",
                        skin = 0,
                        slot = Slot.chest2,
                        container = "wear",
                        amount = 1,
                        killsrequired = 5,
                        price = 5,
                        name = "Wood Jacket",
                        }
                     } } }
        };

        private Dictionary<string, WeaponSet> DefaultWeaponSets = new Dictionary<string, WeaponSet>
                {
                    {"Starter", new WeaponSet{cost = 0, killsrequired = 0, set = new List<Weapon>
                    { new Weapon
                        {
                        name = "Hunting Bow",
                        shortname = "bow.hunting",
                        skin = 0,
                        slot = Slot.main,
                        container = "belt",
                        amount = 1,
                        ammo = 128,
                        ammoType = "arrow.wooden",
                        killsrequired = 0,
                        price = 0,
                        URL = "http://vignette2.wikia.nocookie.net/play-rust/images/2/25/Hunting_Bow_icon.png/revision/latest/scale-to-width-down/50?cb=20151106060745",
                        },
                        new Weapon
                        {
                        name = "Hunting Knife",
                        shortname = "knife.bone",
                        skin = 0,
                        slot = Slot.secondary,
                        container = "belt",
                        amount = 1,
                        ammo = 0,
                        ammoType = "",
                        killsrequired = 0,
                        price = 0,
                        URL = "http://vignette3.wikia.nocookie.net/play-rust/images/c/c7/Bone_Knife_icon.png/revision/latest/scale-to-width-down/50?cb=20151106061357",
                        }
                     } } },
                {"First", new WeaponSet{cost = 0, killsrequired = 0, set = new List<Weapon>
                    { new Weapon
                        {
                        name = "Thompson",
                        shortname = "smg.thompson",
                        skin = 10120,
                        slot = Slot.main,
                        container = "belt",
                        amount = 1,
                        ammo = 128,
                        ammoType = "ammo.pistol",
                        killsrequired = 10,
                        price = 20,
                        URL = "http://vignette3.wikia.nocookie.net/play-rust/images/8/84/Santa%27s_Little_Helper_icon.png/revision/latest/scale-to-width-down/100?cb=20160225141743",
                        attachments = new List<string>
                        {
                            "weapon.mod.muzzleboost",
                            "weapon.mod.muzzlebrake",
                            "weapon.mod.flashlight",
                            "weapon.mod.lasersight",
                            "weapon.mod.holosight",
                            "weapon.mod.silencer"
                        }
                        },
                        new Weapon
                        {
                        name = "Semi-Automatic Pistol",
                        shortname = "pistol.semiauto",
                        skin = 10087,
                        slot = Slot.secondary,
                        container = "belt",
                        amount = 1,
                        ammo = 128,
                        ammoType = "ammo.pistol",
                        killsrequired = 10,
                        price = 20,
                        URL = "http://vignette2.wikia.nocookie.net/play-rust/images/7/7c/Contamination_Pistol_icon.png/revision/latest/scale-to-width-down/100?cb=20160211200655",
                        attachments = new List<string>
                        {
                            "weapon.mod.flashlight",
                            "weapon.mod.lasersight",
                            "weapon.mod.silencer"
                            }
                        }
                     } } }
        };

        private List<Gear> DefaultGeneralItems = new List<Gear>
                {
                        new Gear
                        {
                        shortname = "bandage",
                        amount = 3,
                        skin = 0,
                        },

                        new Gear
                        {
                        shortname = "syringe.medical",
                        amount = 2,
                        skin = 0,
                        }
                    };

        void SaveACPlayer(ACPlayer player)
        {
            if (playerData.players.ContainsKey(player.player.userID))
            {
                if (!playerData.priorSave.ContainsKey(player.player.userID))
                    playerData.priorSave.Add(player.player.userID, playerData.players[player.player.userID]);
            }
            else playerData.players.Add(player.player.userID, new SavedPlayer { });
            var d = playerData.players[player.player.userID];
            d.deaths = player.deaths;
            d.kills = player.kills;
            d.money = player.money;
            d.PlayerGearSets = player.PlayerGearSets;
            d.GearSetKills = player.GearSetKills;
            d.PlayerWeaponSets = player.PlayerWeaponSets;
            d.WeaponSetKills = player.WeaponSetKills;
            d.lastSet = player.currentGearSet;
            d.lastWeapons = player.CurrentWeapons;
            SaveData();
        }

        void SaveData()
        {
            GWData.WriteObject(gwData);
            PlayerData.WriteObject(playerData);
        }

        void LoadData()
        {
            try
            {
                playerData = PlayerData.ReadObject<SavedPlayers>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Combat Saved Player Data, creating a new datafile");
                playerData = new SavedPlayers();
            }
            try
            {
                gwData = GWData.ReadObject<Gear_Weapon_Data>();
            }
            catch
            {
                Puts("Couldn't load the Absolut Combat Gear and Weapons Data, creating a new datafile");
                gwData = new Gear_Weapon_Data();
            }
            if (gwData.GearSets == null || gwData.GearSets.Count == 0)
                LoadDefaultGearData();
            if (gwData.WeaponSets == null || gwData.WeaponSets.Count == 0)
                LoadDefaultWeaponData();
            if (gwData.Attachments == null || gwData.Attachments.Count == 0)
                LoadDefaultAttachmentData();
            if (gwData.General == null || gwData.General.Count == 0)
                LoadDefaultGeneralData();
        }

        void LoadDefaultGearData()
        {
            gwData.GearSets = DefaultGearSets;
        }

        void LoadDefaultWeaponData()
        {
            gwData.WeaponSets = DefaultWeaponSets;
        }

        void LoadDefaultAttachmentData()
        {
            gwData.Attachments = DefaultAttachments;
        }
        void LoadDefaultGeneralData()
        {
            gwData.General = DefaultGeneralItems;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public int KillReward { get; set; }
            public bool BroadcastDeath { get; set; }
            public bool UseEnviroControl { get; set; }
            public int SetCooldown { get; set; }
            public bool PersistentCondition { get; set; }
            public string AdminMenuKeyBinding { get; set; }
            public string PurchaseMenuKeyBinding { get; set; }
            public string SelectionMenuKeyBinding { get; set; }
            public bool UseServerRewards { get; set; }

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
                KillReward = 5,
                BroadcastDeath = true,
                UseEnviroControl = true,
                SetCooldown = 10,
                PersistentCondition = true,
                AdminMenuKeyBinding = "k",
                PurchaseMenuKeyBinding = "p",
                SelectionMenuKeyBinding = "o",
                UseServerRewards = false,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Absolut Combat: " },
            {"ACInfo", "This server is running Absolut Combat. Press '{0}' to access the Equipment Purchase Menu and '{1}' to access the Equipment Selection Menu. You need kills and money to unlock equipment! Goodluck!"},
            {"purchaseitem", "You have successfully purchased: {0} from the {1} set." },
            {"purchaseset", "You have successfully unlocked the {0} Gear Collection" },
            {"purchaseattachment", "You have successfully purchased: {0} for the {1}." },
            {"purchaseweapon", "You have successfully purchased the {0}" },
            {"purchaseweaponset", "You have successfully unlocked the {0} Weapon Collection" },
            {"BuySubMenu", "Set: {0}" },
            {"currentGearSet", "Currently Equipped Set: {0}"},
            {"CurrentWeapons", "Currently Equipped Weapon Set: {0}"},
            {"PurchaseMenuGear", "Purchase Menu - Gear Sets" },
            {"PurchaseMenuWeapon", "Purchase Menu - Weapon Sets" },
            {"GearSets", "Gear Sets" },
            {"WeaponSets", "Weapon Sets" },
            {"PurchaseInfo", "Are you sure you want to purchase: {0} for ${1}?" },
            {"DeathMessage", " has killed " },
            {"SetBegin", "Welcome to set creation! To begin type a name for the new set. You can also type 'quit' to exit.</color>" },
            {"SetCost", "Please type the desired cost for unlocking set: {0}</color>" },
            {"SetKills", "Please type the required amount of kills to unlock this set: {0}</color>" },
            {"GearName", "Please type a name for the item: {0}</color>" },
            {"GearCost", "Please type the desired cost for unlocking {0} from the {1} set</color>" },
            {"GearSlot", "Please select a Slot on the UI for {0}</color>" },
            {"WeaponAttachments", "Please select the desired attachments for {0}"},
            {"GearSkin", "Please select a Skin for {0}</color>" },
            {"GearKills", "Please type the required amount of kills to unlock {0} from the {1} set</color>" },
            {"CreationInfo", "Creation Info" },
            {"AdminOptions", "Admin Options" },
            {"INVALIDENTRY", "The given value: {0} is invalid for this input!" },
            {"INVALIDSLOT", "Invalid Slot Provided!" },
            {"Back", "Back" },
            {"Cancel", "Cancel" },
            {"SaveSet", "Save Set?" },
            {"SelectionMenu", "Equipment Selection Menu" },
            {"PurchaseMenu", "Equipment Purchase Menu" },
            {"CreateWeaponSet", "Create a New\nWeapon Set" },
            {"CreateGearSet", "Create a New\nGear Set" },
            {"DeleteSet", "    Delete Set: {0}" },
            {"WeaponSetManagement", "Weapon Set Management" },
            {"GearSetManagement", "Gear Set Management" },
			{"Hud1", "Current Weapon Kills: {0}"},
            {"Hud2", "Current Gear Kills: {0}"},
            {"Hud3a", "SR Points: {0}"},
            {"Hud3b", "Money: {0}"},
            {"Hud4", "Current Weapon Set: {0}"},
            {"Hud5", "Current Gear Set: {0}"},
            {"Hud6", "Total Kills: {0}"},
            {"GearSetCooldown", "You have changed your Gear Set to: {0} however you are on cooldown and will not get the equipment until respawn" },
            {"WeaponSetCooldown", "You have changed your Weapon Set to: {0} however you are on cooldown and will not get the equipment until respawn" },
            {"AddMoney", "You have been given {0} in Absolut Combat Money!" },
            {"MoneyAdded", "{0} has successfully been given {1} in Absolut Combat Money!" },
            {"MoneyAddedOffline", "{0} has successfully been given but the player is currently offline."},
            {"NotACPlayer", "The provided User ID does not match an Absolut Combat Player " },
            {"AddMoneyError", "There was an error when trying to give money" },
            {"TakeMoney", "{0} in Absolut Combat Money has been taken from you!" },
            {"MoneyTaken", "{1} in Absolut Combat Money has been taken from {0}!" },
            {"MoneyTakenOffline", "{0} has successfully been taken but the player is currently offline."},
            {"TakeMoneyError", "There was an error when trying to take money" },
            {"ClearAttachments", "Clear Attachments: {0}" },
            {"CreationDetails", "This set contains:\n{0}" },
            {"CreationName", "Would you like to save Set Name: {0}?   \nSet Cost: {1}\n      Set Kill Requirement: {2}" },
            {"NewSet", "You have successfully created set: {0}" },
            {"NotAuthorized", "You are not authorized to use this function" },
            {"ArgumentsIncorrect", "You have provided the wrong format. You must specify: {0}. For Example: {1}" },
            {"NoPlayers", "No players found that match {0}" },
            {"MultiplePlayers", "Multiple players found that match {0}" },
            {"NOSR", "ServerRewards is missing. Unloading {0} as it will not work without ServerRewards. Consider setting Use_ServerRewards to false..." },
            {"PurchaseMenuPlayer", "Purchase Menu               Balance: {0}               Kills: {1}" },
            {"SelectionMenuPlayer", "Selection Menu               Balance: {0}               Kills: {1}" }
        };
        #endregion
    }
}
