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
    [Info("Factions", "K1lly0u & Absolut", "1.3", ResourceId = 5)]

    class Factions : RustPlugin
    {
        #region Fields

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin Economics;

        [PluginReference]
        Plugin ServerRewards;

        private bool UseFactions;
        private bool ActiveTaxBox;
        private bool TaxBoxFullNotification;

        static FieldInfo buildingPrivlidges;

        TimeData timeData = new TimeData();

        FactionSavedPlayerData playerData;
            private DynamicConfigFile FactionsData;

        FactionLeaderData leaderData;
            private DynamicConfigFile LeaderData;

        TaxBoxes boxesData;
            private DynamicConfigFile BoxesData;

        TaxRate rateData;
            private DynamicConfigFile RateData;

        private List<Faction> ActiveBoxes = new List<Faction>();

        #endregion

        #region Hooks    
        void Loaded()
        {
            FactionsData = Interface.Oxide.DataFileSystem.GetFile("factions_data");
            LeaderData = Interface.Oxide.DataFileSystem.GetFile("factions_leaders");
            BoxesData = Interface.Oxide.DataFileSystem.GetFile("factions_boxes");
            RateData = Interface.Oxide.DataFileSystem.GetFile("factions_taxrates");
            buildingPrivlidges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            lang.RegisterMessages(messages, this);
        }

        void OnServerInitialized()
        {

            LoadData();
            LoadVariables();
            timer.Once(configData.Save_Interval * 60, () => SaveLoop());
            timer.Once(configData.CheckLeader_Interval * 60, () => CheckLeader());
        }
        ///Player FriendlyFire Check        
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            try
            {

                if (UseFactions)
                {

                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        var victim = entity.ToPlayer();
                        var attacker = hitInfo.Initiator.ToPlayer();
                        if (EventManager)
                        {
                            object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                            if (isPlaying is bool)
                                if ((bool)isPlaying)
                                    return;
                        }
                        if (victim != attacker)
                        {
                            if ((FactionMemberCheck(attacker.userID)) && (FactionMemberCheck(victim.userID)))
                                if (!SameFactionCheck(attacker, victim.userID)) return;
                            if (!configData.FFDisabled) return;
                            if (playerData.playerFactions[attacker.userID].faction == Faction.NONE) return;
                            {
                                hitInfo.damageTypes.ScaleAll(configData.FF_DamageScale);
                                SendMSG(attacker, string.Format(lang.GetMessage("FFs", this, attacker.UserIDString), victim.displayName));
                            }
                        }
                    }
                    ///Player Structure Check                    
                    else if (entity is BaseEntity && hitInfo.Initiator is BasePlayer)
                    {
                        var OwnerID = entity.OwnerID;
                        if (OwnerID != 0)
                        {
                            {
                                var attacker = (BasePlayer)hitInfo.Initiator;
                                if (EventManager)
                                {
                                    object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                                    if (isPlaying is bool)
                                        if ((bool)isPlaying)
                                            return;
                                }
                                if (attacker.userID == OwnerID) return;
                                if (AuthorizedTC(attacker))
                                {
                                    return;
                                }
                                if (FactionMemberCheck(attacker.userID))
                                    if (!SameFactionCheck(attacker, OwnerID)) return;
                                if (!configData.BuildingProtectionEnabled) return;
                                if (playerData.playerFactions[attacker.userID].faction == Faction.NONE) return;
                                {
                                    hitInfo.damageTypes.ScaleAll(0);
                                    SendMSG(attacker, string.Format(lang.GetMessage("FFBuildings", this, attacker.UserIDString), playerData.playerFactions[OwnerID].Name));
                                }
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

            try
            {
                var victim = entity.ToPlayer();
                var attacker = hitInfo.Initiator.ToPlayer() as BasePlayer;
                if (UseFactions)
                    if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                    {
                        if (entity as BasePlayer == null || hitInfo == null) return;
                        if (victim.userID != attacker.userID)
                            if (FactionMemberCheck(attacker.userID))
                            {
                                if (!SameFactionCheck(attacker, victim.userID))
                                {
                                    if (EventManager)
                                    {
                                        object isPlaying = EventManager?.Call("isPlaying", new object[] { attacker });
                                        if (isPlaying is bool)
                                            if ((bool)isPlaying)
                                                return;
                                    }
                                    SendDeathNote(attacker, victim, playerData.playerFactions[attacker.userID].faction);
                                    if (configData.UseEconomics)
                                    {
                                        Economics.Call("DepositS", attacker.userID.ToString(), configData.UseEconomicsAmount);
                                        SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.UseEconomicsAmount, "Currency"));
                                    }
                                    if (configData.UseTokens)
                                    {
                                        EventManager.Call("AddTokens", attacker.userID.ToString(), configData.UseTokensAmount);
                                        SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.UseTokensAmount, "Tokens"));
                                    }
                                    if (configData.UseRewards)
                                    {
                                        ServerRewards?.Call("AddPoints", attacker.userID.ToString(), configData.UseRewardsAmount);
                                        SendMSG(attacker, string.Format(lang.GetMessage("Payment", this, attacker.UserIDString), configData.UseRewardsAmount, "Reward Points"));
                                    }
                                }
                            }

                    }
            }
            catch (Exception)
            {
            }
        }
        private void OnPlayerInit(BasePlayer player)
        {
            if (UseFactions)
            {
                if (player.IsSleeping())
                {
                    timer.Once(5, () =>
                    {
                        player.EndSleeping();
                        if (FactionMemberCheck(player.userID))
                        {
                            playerData.playerFactions[player.userID].Name = player.displayName;
                            SendPuts(string.Format(lang.GetMessage("PlayerReturns", this, player.UserIDString), player.displayName));
                            return;
                        }
                        else SetFaction(player, CountPlayers(Faction.A), CountPlayers(Faction.B), CountPlayers(Faction.C), CountPlayers(Faction.NONE));
                        SendPuts(string.Format(lang.GetMessage("PlayerNew", this, player.UserIDString), player.displayName));
                    });
                }
                InitPlayerData(player);
            }
        }

        void DestroyPlayer(BasePlayer player)
        {
            DestroyAll(player);
            SavePlayersData(player);
            timeData.Players.Remove(player.userID);
            if (FactionMemberCheck(player.userID))
                playerData.playerFactions[player.userID].LastConnection = DateTime.Today;
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyPlayer(player);
            SendPuts(string.Format(lang.GetMessage("PlayerLeft", this, player.UserIDString), player.displayName));
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (!UseFactions) return;
            if (FactionMemberCheck(player.userID))
            {
                var faction = GetPlayerFaction(player);
                if (!configData.Use_FactionGear) return;
                GivePlayerGear(player, faction);
            }

            else OnPlayerInit(player);
        }
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer)arg.connection.player;
            if (EventManager)
            {
                object isPlaying = EventManager?.Call("isPlaying", new object[] { player });
                if (isPlaying is bool)
                    if ((bool)isPlaying)
                        return null;
            }
            if (UseFactions)
                if (configData.UsePluginChatControl)
                {

                    string message = arg.GetString(0, "text");
                    string color = "";
                    if (FactionMemberCheck(player.userID))
                    {
                        switch (playerData.playerFactions[player.userID].faction)
                        {
                            case Faction.A:
                                color = configData.FactionA_Chat_Color + "[" + configData.FactionA_Name + "] ";
                                break;
                            case Faction.B:
                                color = configData.FactionB_Chat_Color + "[" + configData.FactionB_Name + "] ";
                                break;
                            case Faction.C:
                                color = configData.FactionC_Chat_Color + "[" + configData.FactionC_Name + "] ";
                                break;
                            case Faction.NONE:
                                color = configData.FactionNone_Chat_Color + "[" + configData.FactionNone_Name + "] ";
                                break;
                        }
                    }
                    if (leaderData.factionLeaders.ContainsValue(player.userID))
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
            return null;
        }

        void OnLootEntity(BasePlayer player, object lootable)
        {
            if (!configData.Use_Taxes) return;
            BaseEntity container = lootable as BaseEntity;
            var faction = playerData.playerFactions[player.userID].faction;
            var coords = container.transform.localPosition;
            if ((player == null) || (container == null) || (!leaderData.factionLeaders.ContainsValue(player.userID))) return;
            {
                if (!ActiveBoxes.Contains(faction))
                {
                    if (GetTaxContainer(faction) == container)
                    {
                        SendMSG(player, lang.GetMessage("TaxBox", this));
                        return;
                    }
                    return;
                }
                if (!boxesData.Boxes.ContainsKey(faction))
                {
                    if (container.LookupShortPrefabName() == "box.wooden.large.prefab")
                    {
                        ActiveBoxes.Remove(faction);
                        boxesData.Boxes.Add(faction, new Coords { x = coords.x, y = coords.y, z = coords.z });
                        SendMSG(player, lang.GetMessage("NewTaxBox", this));
                        SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
                        SaveData();
                        return;
                    }
                    SendMSG(player, lang.GetMessage("TaxBoxError", this));
                    return;
                }
                else
                {
                    ActiveBoxes.Remove(faction);
                    boxesData.Boxes.Remove(faction);
                    boxesData.Boxes.Add(faction, new Coords { x = coords.x, y = coords.y, z = coords.z });
                    SendMSG(player, lang.GetMessage("NewTaxBox", this));
                    SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
                    SaveData();
                    return;
                }
            }

        }


        void OnPlantGather(PlantEntity Plant, Item item, BasePlayer player)
        {
            if (!configData.Use_Taxes) return;

            {
                var faction = playerData.playerFactions[player.userID].faction;
                var factionleaderid = leaderData.factionLeaders[faction];
                BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);

                if (!rateData.rates.ContainsKey(faction)) return;
                var c = boxesData.Boxes[faction];
                var taxrate = rateData.rates[faction];

                if (!boxesData.Boxes.ContainsKey(faction)) return;
                StorageContainer TaxContainer = GetTaxContainer(faction);

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
            if (!configData.Use_Taxes) return;
            {
                var faction = playerData.playerFactions[player.userID].faction;
                var factionleaderid = leaderData.factionLeaders[faction];
                BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);

                if (!rateData.rates.ContainsKey(faction)) return;
                var c = boxesData.Boxes[faction];
                var taxrate = rateData.rates[faction];

                if (!boxesData.Boxes.ContainsKey(faction)) return;
                StorageContainer TaxContainer = GetTaxContainer(faction);

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
            if (!configData.Use_Taxes) return;
            {
                BasePlayer player = entity.ToPlayer();
                var faction = playerData.playerFactions[player.userID].faction;
                var factionleaderid = leaderData.factionLeaders[faction];
                BasePlayer factionleader = BasePlayer.FindByID(factionleaderid);

                if (!rateData.rates.ContainsKey(faction)) return;
                var c = boxesData.Boxes[faction];
                var taxrate = rateData.rates[faction];
                if (!boxesData.Boxes.ContainsKey(faction)) return;
                StorageContainer TaxContainer = GetTaxContainer(faction);
                int Tax = Convert.ToInt32(Math.Round((item.amount * taxrate) / 100));


                if (!TaxContainer.inventory.IsFull())
                {
                    ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);

                    if (ToAdd != null)
                    {
                        TaxContainer.inventory.AddItem(ToAdd, Tax);
                        item.amount = item.amount - Tax;
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

        private void SetBoxFullNotification()
        {
            TaxBoxFullNotification = true;
            timer.Once(5 * 60, () => TaxBoxFullNotification = false);
        }


        #endregion

        #region Functions

        private bool SameFactionCheck(BasePlayer self, ulong target)
        {
            if (playerData.playerFactions[self.userID].faction == playerData.playerFactions[target].faction) return true;
            else return false;
        }

        private bool FactionMemberCheck(ulong player)
        {
            if (playerData.playerFactions.ContainsKey(player)) { return true; }
            else return false;
        }

        private Faction GetPlayerFaction(BasePlayer player)
        {
            if (FactionMemberCheck(player.userID)) return playerData.playerFactions[player.userID].faction;
            else return Faction.NONE;
        }

        public void Broadcast(string message, string userid = "0") => ConsoleSystem.Broadcast("chat.add", userid, message, 1.0);

        private void BroadcastFaction(Faction faction, string message)
        {
            foreach (var entry in playerData.playerFactions)
            {
                BasePlayer player = BasePlayer.FindByID(entry.Key);

                if (entry.Value.faction == faction && entry.Value.faction == Faction.A)
                    SendReply(player, configData.FactionA_Chat_Color + "[" + configData.FactionA_Name + "]" + lang.GetMessage("inFactionChat", this) + "</color> " + message);
                if (entry.Value.faction == faction && entry.Value.faction == Faction.B)
                    SendReply(player, configData.FactionB_Chat_Color + "[" + configData.FactionB_Name + "]" + lang.GetMessage("inFactionChat", this) + "</color> " + message);
                if (entry.Value.faction == faction && entry.Value.faction == Faction.C)
                    SendReply(player, configData.FactionC_Chat_Color + "[" + configData.FactionC_Name + "]" + lang.GetMessage("inFactionChat", this) + "</color> " + message);
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

        private void SendDeathNote(BasePlayer player, BasePlayer victim, Faction faction)
        {
            string colorAttacker = "";
            string colorVictim = "";
            string prefixAttacker = "";
            string prefixVictim = "";
            switch (faction)
            {
                case Faction.A:
                    colorAttacker = configData.FactionA_Chat_Color;
                    prefixAttacker = "[" + configData.FactionA_Name + "] ";
                    break;
                case Faction.B:
                    colorAttacker = configData.FactionB_Chat_Color;
                    prefixAttacker = "[" + configData.FactionB_Name + "] ";
                    break;
                case Faction.C:
                    colorAttacker = configData.FactionC_Chat_Color;
                    prefixAttacker = "[" + configData.FactionC_Name + "] ";
                    break;
                case Faction.NONE:
                    colorAttacker = configData.FactionNone_Chat_Color;
                    prefixAttacker = "[" + configData.FactionNone_Name + "] ";
                    break;
            }
            switch (playerData.playerFactions[victim.userID].faction)
            {
                case Faction.A:
                    colorVictim = configData.FactionA_Chat_Color;
                    prefixVictim = "[" + configData.FactionA_Name + "] ";
                    break;
                case Faction.B:
                    colorVictim = configData.FactionB_Chat_Color;
                    prefixVictim = "[" + configData.FactionB_Name + "] ";
                    break;
                case Faction.C:
                    colorVictim = configData.FactionC_Chat_Color;
                    prefixVictim = "[" + configData.FactionC_Name + "] ";
                    break;
                case Faction.NONE:
                    colorVictim = configData.FactionNone_Chat_Color;
                    prefixVictim = "[" + configData.FactionNone_Name + "] ";
                    break;
            }
            if (configData.BroadcastDeath)
            {
                string formatMsg = colorAttacker + prefixAttacker + player.displayName + "</color>" + lang.GetMessage("DeathMessage", this, player.UserIDString) + colorVictim + prefixVictim + victim.displayName + "</color>";
                Broadcast(formatMsg);
            }
        }
        private string CountPlayers(Faction faction)
        {
            int i = 0;
            foreach (var entry in playerData.playerFactions)
            {
                if (entry.Value.faction == faction)
                    i++;
            }
            return i.ToString();
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

        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMinutes;

        private void SavePlayersData(BasePlayer player)
        {
            var ID = player.userID;
            var time = GrabCurrentTime();
            var p = playerData.playerFactions;

            if (!p.ContainsKey(ID)) return;
            p[ID].FactionMemberTime += (time - timeData.Players[player.userID]);
            timeData.Players[player.userID] = time;
        }

        private void InitPlayerData(BasePlayer player)
        {
            var ID = player.userID;
            if (!timeData.Players.ContainsKey(ID))
                timeData.Players.Add(ID, GrabCurrentTime());
        }

        private StorageContainer GetTaxContainer(Faction faction)
        {
            foreach (var c in boxesData.Boxes)
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

    #endregion

        #region UI Creation
    void SetFaction(BasePlayer player, string Counta, string Countb, string Countc, string Countnone)
        {
            CuiHelper.DestroyUi(player, PanelFactionSelection);
            var element = UI.CreateElementContainer(PanelFactionSelection, "0.9 0.9 0.9 0.0", "0.1 0.1", "0.9 0.9", true);
            UI.CreatePanel(ref element, PanelFactionSelection, "0.7 0.7 0.7 0.1", "0.03 0.05", "0.97 0.95", true);
            UI.CreateLabel(ref element, PanelFactionSelection, "1.0 1.0 1.0 1.0", lang.GetMessage("FactionSelectionTitle", this), 20, "0.2 0.8", "0.8 0.9");
            UI.CreateButton(ref element, PanelFactionSelection, configData.FactionA_GUI_Color, configData.FactionA_Name, 20, "0.10 0.50", "0.30 0.60", "FactionSelect1", "");
            UI.CreateLabel(ref element, PanelFactionSelection, configData.FactionA_GUI_Color, lang.GetMessage("TotalGUI", this) + Counta, 20, "0.07 0.61", "0.33 0.70");
            UI.CreateButton(ref element, PanelFactionSelection, configData.FactionB_GUI_Color, configData.FactionB_Name, 20, "0.40 0.50", "0.60 0.60", "FactionSelect2", "");
            UI.CreateLabel(ref element, PanelFactionSelection, configData.FactionB_GUI_Color, lang.GetMessage("TotalGUI", this) + Countb, 20, "0.37 0.61", "0.63 0.70");
            UI.CreateButton(ref element, PanelFactionSelection, configData.FactionC_GUI_Color, configData.FactionC_Name, 20, "0.70 0.50", "0.90 0.60", "FactionSelect3", "");
            UI.CreateLabel(ref element, PanelFactionSelection, configData.FactionC_GUI_Color, lang.GetMessage("TotalGUI", this) + Countc, 20, "0.67 0.61", "0.93 0.70");
            UI.CreateButton(ref element, PanelFactionSelection, configData.FactionNone_GUI_Color, configData.FactionNone_Name + " - No Faction", 20, "0.40 0.25", "0.60 0.35", "FactionSelectNone", "");
            UI.CreateLabel(ref element, PanelFactionSelection, configData.FactionNone_GUI_Color, lang.GetMessage("TotalGUI", this) + Countnone, 20, "0.37 0.36", "0.63 0.45");
            //UI.CreateButton(ref element, PanelFactionSelection, "0.2 0.2 0.2 0.0", lang.GetMessage("Close", this), 18, "0.81 0.1", "0.93 0.2", "UI_DestroyFS", "");
            CuiHelper.AddUi(player, element);
        }

        void ShowPlayers(BasePlayer player, string APlayers, string BPlayers, string CPlayers, string NonePlayers)
        {
            CuiHelper.DestroyUi(player, PanelShowPlayers);
            var element = UI.CreateElementContainer(PanelShowPlayers, "0.0 0.0 0.0 1.0", "0.25 0.25", "0.75 0.75", true);
            UI.CreatePanel(ref element, PanelShowPlayers, "1.0 1.0 1.0 0.5", "0.0 0.0", "0.24 1.0", true);
            UI.CreatePanel(ref element, PanelShowPlayers, "1.0 1.0 1.0 0.5", "0.25 0.0", "0.49 1.0", true);
            UI.CreatePanel(ref element, PanelShowPlayers, "1.0 1.0 1.0 0.5", "0.50 0.0", "0.74 1.0", true);
            UI.CreatePanel(ref element, PanelShowPlayers, "1.0 1.0 1.0 0.5", "0.75 0.0", "1.0 1.0", true);
            UI.CreatePanel(ref element, PanelShowPlayers, "0.0 0.0 0.0 1.0", "0.0 0.8", "1.0 0.82", true);
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionA_GUI_Color, configData.FactionA_Name, 18, "0.0 0.88", "0.24 1.0");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionB_GUI_Color, configData.FactionB_Name, 18, "0.25 0.88", "0.49 1.0");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionC_GUI_Color, configData.FactionC_Name, 18, "0.50 0.88", "0.74 1.0");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionNone_GUI_Color, configData.FactionNone_Name + "\n-No Faction-", 18, "0.75 0.88", "0.99 1.0");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionA_GUI_Color, lang.GetMessage("MembersGUI", this), 15, "0.0 0.83", "0.24 0.88");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionB_GUI_Color, lang.GetMessage("MembersGUI", this), 15, "0.25 0.83", "0.49 0.88");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionC_GUI_Color, lang.GetMessage("MembersGUI", this), 15, "0.50 0.83", "0.74 0.88");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionNone_GUI_Color, lang.GetMessage("MembersGUI", this), 15, "0.75 0.83", "0.99 0.88");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionA_GUI_Color, APlayers, 14, "0.0 0.0", "0.24 0.79");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionB_GUI_Color, BPlayers, 14, "0.25 0.0", "0.49 0.79");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionC_GUI_Color, CPlayers, 14, "0.50 0.0", "0.74 0.79");
            UI.CreateLabelTM(ref element, PanelShowPlayers, configData.FactionNone_GUI_Color, NonePlayers, 14, "0.75 0.0", "0.99 0.79");
            UI.CreateButton(ref element, PanelShowPlayers, "0.0 0.0 0.0 1.0", lang.GetMessage("Close", this), 15, "0.93 0.05", "0.99 0.1", "UI_DestroySP", "");
            CuiHelper.AddUi(player, element);
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
            CuiHelper.DestroyUi(player, PanelFactionSelection);
        }

        [ConsoleCommand("UI_DestroySP")]
        private void cmdDestroySP(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            DestroySP(player);
        }

        private void DestroySP(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelShowPlayers);
        }

        private void DestroyAll(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PanelFactionSelection);
            CuiHelper.DestroyUi(player, PanelShowPlayers);
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
                var newTeam = Faction.NONE;

                if (foundPlayers[0] != null)
                {
                    AssignPlayerToFaction(foundPlayers[0], newTeam);
                    SendReply(arg, string.Format(lang.GetMessage("UnassignSuccess", this), foundPlayers[0].displayName, configData.FactionNone_Name));
                    SendReply(foundPlayers[0], string.Format(lang.GetMessage("RemovedFromFaction", this), foundPlayers[0].displayName, configData.FactionNone_Name));
                    SetFaction(foundPlayers[0], CountPlayers(Faction.A), CountPlayers(Faction.B), CountPlayers(Faction.C), CountPlayers(Faction.NONE));
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
            if (!FactionMemberCheck(player.userID) || (playerData.playerFactions[player.userID].faction == Faction.NONE))
            {
                SendMSG(player, lang.GetMessage("NoFactionError", this, player.UserIDString));
                return;
            }
            var faction = (GetPlayerFaction(player));
            var message = string.Join(" ", args);
            if (string.IsNullOrEmpty(message))
                return;
            var playerfaction = playerData.playerFactions[player.userID].faction;
            if (leaderData.factionLeaders.ContainsValue(player.userID))
            {
                if (faction == Faction.A) BroadcastFaction(playerfaction,$"{configData.FactionA_Chat_Color}[LEADER] {player.displayName}</color>: " + message);
                if (faction == Faction.B) BroadcastFaction(playerfaction, $"{configData.FactionB_Chat_Color}[LEADER] {player.displayName}</color>: " + message);
                if (faction == Faction.C) BroadcastFaction(playerfaction, $"{configData.FactionC_Chat_Color}[LEADER] {player.displayName}</color>: " + message);
            }
            else
            {
                if (faction == Faction.A) BroadcastFaction(playerfaction, $"{configData.FactionA_Chat_Color}{player.displayName}</color>: " + message);
                if (faction == Faction.B) BroadcastFaction(playerfaction, $"{configData.FactionB_Chat_Color}{player.displayName}</color>: " + message);
                if (faction == Faction.C) BroadcastFaction(playerfaction, $"{configData.FactionC_Chat_Color}{player.displayName}</color>: " + message);
            }
        }

        [ChatCommand("faction")]
        private void cmdfaction(BasePlayer player, string command, string[] args)
        {
            var playerfaction = playerData.playerFactions[player.userID].faction;
            if (args == null || args.Length == 0)
            {
                SendMSG(player, "V " + Version.ToString() + "--  by " + Author, lang.GetMessage("title", this));
                SendMSG(player, lang.GetMessage("FactionJoin1", this, player.UserIDString), lang.GetMessage("FactionJoin", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("FactionList1", this, player.UserIDString), lang.GetMessage("FactionList", this, player.UserIDString));
                SendMSG(player, lang.GetMessage("FactionChat1", this, player.UserIDString), lang.GetMessage("FactionChat", this, player.UserIDString));
                if (leaderData.factionLeaders.ContainsValue(player.userID))
                {
                    SendMSG(player, lang.GetMessage("FactionTaxBox1", this, player.UserIDString), lang.GetMessage("FactionTaxBox", this, player.UserIDString));
                    SendMSG(player, lang.GetMessage("FactionTax1", this, player.UserIDString), lang.GetMessage("FactionTax", this, player.UserIDString));
                }
                if (isAuth(player))
                {
                    SendMSG(player, lang.GetMessage("FactionUnassign1", this, player.UserIDString), lang.GetMessage("FactionUnassign", this, player.UserIDString));
                }
            }
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "join":

                        if (!FactionMemberCheck(player.userID) || (playerData.playerFactions[player.userID].faction == Faction.NONE))
                        {
                            SetFaction(player, CountPlayers(Faction.A), CountPlayers(Faction.B), CountPlayers(Faction.C), CountPlayers(Faction.NONE));
                        }
                        else SendMSG(player, string.Format(lang.GetMessage("FactionChangeError", this, player.UserIDString), player.displayName));
                        return;

                    case "list":
                        {
                            ShowPlayers(player, CreateTeamList(Faction.A), CreateTeamList(Faction.B), CreateTeamList(Faction.C), CreateTeamList(Faction.NONE));
                            return;
                        }

                    case "taxbox":
                        if (!configData.Use_Taxes)
                        {
                            SendMSG(player, lang.GetMessage("TaxesDisabled", this));
                            return;
                        }
                        if (!leaderData.factionLeaders.ContainsValue(player.userID))
                        {
                            SendMSG(player, lang.GetMessage("NotLeader", this));
                            return;
                        }
                        if (!ActiveBoxes.Contains(playerfaction))
                        {
                            ActiveBoxes.Add(playerfaction);
                            SendMSG(player, lang.GetMessage("TaxBoxActivated", this));
                            return;
                        }
                        ActiveBoxes.Remove(playerfaction);
                        SendMSG(player, lang.GetMessage("TaxBoxDeActivated", this));
                        return;

                    case "tax":

                        if (!leaderData.factionLeaders.ContainsValue(player.userID))
                        {
                            SendMSG(player, lang.GetMessage("NotLeader", this));
                            return;
                        }
                        if (!configData.Use_Taxes)
                        {
                            SendMSG(player, lang.GetMessage("TaxesDisabled", this));
                            return;
                        }

                        if (args.Length == 2)
                        {
                            double Num;
                            bool isNum = double.TryParse(MergeParams(args, 1), out Num);
                            if (!isNum)
                            {
                                SendMSG(player, lang.GetMessage("ftaxFormatError", this));
                                return;
                            }
                            else
                            {
                                double NewTaxLevel = Convert.ToDouble(MergeParams(args, 1));

                                if (NewTaxLevel > 25.0)
                                {
                                    SendMSG(player, lang.GetMessage("TaxLimit", this));
                                    NewTaxLevel = 25.0;
                                }
                                if (NewTaxLevel < 1) NewTaxLevel = 0;
                                if (rateData.rates.ContainsKey(playerfaction)) rateData.rates.Remove(playerfaction);
                                rateData.rates.Add(playerfaction, NewTaxLevel);
                                SendMSG(player, string.Format(lang.GetMessage("NewTaxRate", this), NewTaxLevel));
                                SaveData();
                            }
                        }
                        else
                            SendMSG(player, lang.GetMessage("ftaxFormatError", this));
                        return;

                    case "unassign":

                        if (!isAuth(player))
                        {
                            SendMSG(player, lang.GetMessage("NotAuth", this));
                            return;
                        }
                            if (args.Length == 2)
                        {
                            var partialPlayerName = args[1];
                            var foundPlayers = FindPlayer(partialPlayerName);
                            if (foundPlayers.Count == 0)
                            {
                                SendMSG(player, string.Format(lang.GetMessage("NoPlayers", this)));
                                return;
                            }
                            if (foundPlayers.Count > 1)
                            {
                                SendMSG(player, string.Format(lang.GetMessage("multiPlayers", this)));
                                return;
                            }
                            var newTeam = Faction.NONE;
                            var oldfaction = playerData.playerFactions[foundPlayers[0].userID].faction;
                            var factionname = "None";
                            if (oldfaction == Faction.A) factionname = configData.FactionA_Name;
                            if (oldfaction == Faction.B) factionname = configData.FactionB_Name;
                            if (oldfaction == Faction.C) factionname = configData.FactionC_Name;

                            if (foundPlayers[0] != null)
                            {
                                AssignPlayerToFaction(foundPlayers[0], newTeam);
                                SendMSG(player, string.Format(lang.GetMessage("UnassignSuccess", this), foundPlayers[0].displayName, factionname));
                                SendMSG(foundPlayers[0], string.Format(lang.GetMessage("RemovedFromFaction", this), foundPlayers[0].displayName, configData.FactionNone_Name));
                                SetFaction(foundPlayers[0], CountPlayers(Faction.A), CountPlayers(Faction.B), CountPlayers(Faction.C), CountPlayers(Faction.NONE));
                                return;
                            }
                            else SendMSG(player, string.Format(lang.GetMessage("UnassignError", this)));
                        }
                        SendReply(player, string.Format(lang.GetMessage("FactionUnassignFormating", this)));
                        return;
                }

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

        [ConsoleCommand("FactionSelect1")]
        private void cmdFactionSelect1(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToFaction(player, Faction.A);
            PrintToChat($"{configData.FactionA_Chat_Color} {player.displayName} {lang.GetMessage("Joined", this)} {configData.FactionA_Name}!</color>");
            DestroyFS(player);
            if (configData.Use_FactionGear) GivePlayerGear(player, Faction.A);
            if (configData.Use_Groups)
            {
                ConsoleSystem.Run.Server.Normal($"usergroup add {player.userID} {configData.FactionAGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionBGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionCGroupName}");
            }
        }

        [ConsoleCommand("FactionSelect2")]
        private void cmdFactionSelect2(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToFaction(player, Faction.B);
            PrintToChat($"{configData.FactionB_Chat_Color} {player.displayName} {lang.GetMessage("Joined", this)} {configData.FactionB_Name}!</color>");
            DestroyFS(player);
            if (configData.Use_FactionGear) GivePlayerGear(player, Faction.B);
            if (configData.Use_Groups)
            {
                ConsoleSystem.Run.Server.Normal($"usergroup add {player.userID} {configData.FactionBGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionAGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionCGroupName}");
            }
        }

        [ConsoleCommand("FactionSelect3")]
        private void cmdFactionSelect3(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToFaction(player, Faction.C);
            PrintToChat($"{configData.FactionC_Chat_Color} {player.displayName} {lang.GetMessage("Joined", this)} {configData.FactionC_Name}!</color>");
            DestroyFS(player);
            if (configData.Use_FactionGear) GivePlayerGear(player, Faction.C);
            if (configData.Use_Groups)
            {
                ConsoleSystem.Run.Server.Normal($"usergroup add {player.userID} {configData.FactionCGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionAGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionBGroupName}");
            }
        }

        [ConsoleCommand("FactionSelectNone")]
        private void cmdFactionSelectNone(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null)
                return;
            AssignPlayerToFaction(player, Faction.NONE);
            PrintToChat($"{configData.FactionNone_Chat_Color} {player.displayName} {lang.GetMessage("Joined", this)} {configData.FactionNone_Name}!</color>");
            DestroyFS(player);
            if (configData.Use_Groups)
            {
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionAGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionBGroupName}");
                ConsoleSystem.Run.Server.Normal($"usergroup remove {player.userID} {configData.FactionCGroupName}");
            }
        }

        #endregion

        #region Faction Management

        enum Faction
        {
            NONE,
            A,
            B,
            C
        }

        enum Rank
        {
            Follower,
            Apprentice,
            Lieutenant,
            Captain,
            Leader
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

        private void AssignPlayerToFaction(BasePlayer player, Faction faction)
        {
            var ID = player.userID;
            var DT = DateTime.Now;
            var time = GrabCurrentTime();
            var p = playerData.playerFactions;
            var fl = leaderData.factionLeaders;
            var factionname = "None";
            if (faction == Faction.A) factionname = configData.FactionA_Name;
            if (faction == Faction.B) factionname = configData.FactionB_Name;
            if (faction == Faction.C) factionname = configData.FactionC_Name;

            if (FactionMemberCheck(ID))
                if (p[ID].faction == faction)
                    return;

            if (!p.ContainsKey(ID))
                p.Add(ID, new FactionPlayerData { faction = faction, Name = player.displayName, rank = Rank.Apprentice, FactionMemberTime = 0 });
            else
            {
                p[ID].faction = faction;
                p[ID].Name = player.displayName;
                p[ID].FactionMemberTime = 0;
            }


            if (fl.ContainsValue(ID))
            {
                foreach (var entry in fl.Where(kvp => kvp.Value == ID).ToList())
                {
                    fl.Remove(entry.Key);
                }
            }
            if (!fl.ContainsKey(faction) && p[ID].faction != Faction.NONE)
            {
                fl.Add(faction, ID);
                SendPuts(string.Format(lang.GetMessage("NewLeader", this, player.UserIDString), player.displayName, factionname));
            }
            SaveData();
        }

        private void CheckLeader()
        {
            foreach (var p in BasePlayer.activePlayerList)
            {

                var Id = p.userID;
                var data = playerData.playerFactions;
                var pfaction = data[Id].faction;
                var ptime = data[Id].FactionMemberTime;
                if (pfaction == Faction.NONE) return;
                if (leaderData.factionLeaders.ContainsKey(pfaction))
                {
                    ulong CurrentLeaderID = 0;
                    leaderData.factionLeaders.TryGetValue(pfaction, out CurrentLeaderID);
                    var CurrentLeadertime = playerData.playerFactions[CurrentLeaderID].FactionMemberTime;
                    if (CurrentLeadertime >= ptime)
                    {
                            return;
                    }
                    else
                    {
                        leaderData.factionLeaders.Remove(pfaction);
                        leaderData.factionLeaders.Add(pfaction, Id);
                        SendPuts(string.Format(lang.GetMessage("NewLeader", this, p.UserIDString), p.displayName, pfaction));
                        SaveData();
                    }
                }
            }
            timer.Once(configData.CheckLeader_Interval * 60, () => CheckLeader());
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

        string CreateTeamList(Faction team) 
        {
            string message = ""; 
            foreach (var entry in playerData.playerFactions)
            {
                if (entry.Value.faction == team) 
                {
                    message = message + entry.Value.Name + "\n";
                }
            }
            return message;
        }

        #endregion

        #region Giving Items
        private void GivePlayerGear(BasePlayer player, Faction faction)
        {
                var teamGear = new List<Gear>();
                if (faction == Faction.A) teamGear = configData.z_FactionA_Gear;
                else if (faction == Faction.B) teamGear = configData.z_FactionB_Gear;
                else if (faction == Faction.C) teamGear = configData.z_FactionC_Gear;

                if (teamGear != null)
                    foreach (var entry in teamGear)

                        GiveItem(player, BuildItem(entry.shortname, entry.amount, entry.skin), entry.container);
        }

        private Item BuildItem(string shortname, int amount = 1, int skin = 0)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                var item = ItemManager.Create(definition, amount, false, skin);
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
        #endregion

        #region Classes

        //UI
        private string PanelFactionSelection = "SetFaction";
        private string PanelShowPlayers = "ShowPlayers";

        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor,
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

            static public void CreateLabelTM(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.UpperCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = 1.0f, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, string close, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Close = close, Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            } 
        }
        class FactionSavedPlayerData
            {
                public Dictionary<ulong, FactionPlayerData> playerFactions = new Dictionary<ulong, FactionPlayerData>();
            }
        class FactionPlayerData
        {
            public Faction faction;
            public string Name;
            public Rank rank;
            public DateTime LastConnection;
            public double FactionMemberTime = 0;
        }

        class FactionLeaderData
        {
            public Dictionary<Faction, ulong> factionLeaders = new Dictionary<Faction, ulong>(); 
        }

        class TimeData
        {
            public Dictionary<ulong, double> Players = new Dictionary<ulong, double>();
        }

        class TaxBoxes
        {
            public Dictionary<Faction, Coords> Boxes = new Dictionary<Faction, Coords>();
        }

        class Coords
        {
            public float x;
            public float y;
            public float z;
        }

        class TaxRate
        {
            public Dictionary<Faction, double> rates = new Dictionary<Faction, double>();
        }

        class Gear
        {
            public string name;
            public string shortname;
            public int skin;
            public int amount;
            public string container;
        }

        #endregion

        #region Data Management

        private void SaveLoop()
        {
            SaveData();
            timer.Once(configData.Save_Interval * 60, () => SaveLoop());
        }

        void SaveData()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (timeData.Players.ContainsKey(player.userID))
                    SavePlayersData(player);
            }
            FactionsData.WriteObject(playerData);
            LeaderData.WriteObject(leaderData);
            BoxesData.WriteObject(boxesData);
            RateData.WriteObject(rateData);
            Puts("Saved player data");
        }

        void LoadData()
        {
            try
            {
                playerData = FactionsData.ReadObject<FactionSavedPlayerData>();
            }
            catch
            {

                Puts("Couldn't load player data, creating new datafile");
                playerData = new FactionSavedPlayerData();
            }
            try
            {
                leaderData = LeaderData.ReadObject<FactionLeaderData>();
            }
            catch
            {
                Puts("Couldn't load faction leader data, creating new datafile");
                leaderData = new FactionLeaderData();
            }
            try
            {
                boxesData = BoxesData.ReadObject<TaxBoxes>();
            }
            catch
            {
                Puts("Couldn't load faction leader boxes data, creating new datafile");
                boxesData = new TaxBoxes();
            }
            try
            {
                rateData = RateData.ReadObject<TaxRate>();
            }
            catch
            {
                Puts("Couldn't load faction tax rate data, creating new datafile");
                rateData = new TaxRate();
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public string MSG_MainColor { get; set; }
            public string MSG_Color { get; set; }
            public bool BroadcastDeath { get; set; }
            public bool UseEconomics { get; set; }
            public int UseEconomicsAmount { get; set; }
            public bool UseTokens { get; set; }
            public int UseTokensAmount { get; set; }
            public bool UseRewards { get; set; }
            public int UseRewardsAmount { get; set; }

            public string FactionA_Chat_Color { get; set; }
            public string FactionB_Chat_Color { get; set; }
            public string FactionC_Chat_Color { get; set; }
            public string FactionNone_Chat_Color { get; set; }

            public string FactionA_GUI_Color { get; set; }
            public string FactionB_GUI_Color { get; set; }
            public string FactionC_GUI_Color { get; set; }
            public string FactionNone_GUI_Color { get; set; }

            public string FactionA_Name { get; set; }
            public string FactionB_Name { get; set; }
            public string FactionC_Name { get; set; }
            public string FactionNone_Name { get; set; }

            public string FactionAGroupName { get; set; }
            public string FactionBGroupName { get; set; }
            public string FactionCGroupName { get; set; }

            public int CheckLeader_Interval { get; set; }
            public int Save_Interval { get; set; }
            public bool Use_FactionGear { get; set; }
            public bool Use_Ranks { get; set; }
            public bool Use_Taxes { get; set; }
            public bool Use_Groups { get; set; }

            public bool FFDisabled { get; set; }
            public bool BuildingProtectionEnabled { get; set; }

            public float FF_DamageScale { get; set; }
            public bool UsePluginChatControl { get; set; }

            public List<Gear> z_FactionA_Gear { get; set; }
            public List<Gear> z_FactionB_Gear { get; set; }
            public List<Gear> z_FactionC_Gear { get; set; }

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
                UseEconomics = false,
                UseEconomicsAmount = 100,
                UseTokens = false,
                UseTokensAmount = 10,
                UseRewards = false,
                UseRewardsAmount = 10,

                FactionA_Chat_Color = "<color=#CC3232>",
                FactionB_Chat_Color = "<color=#33adff>",
                FactionC_Chat_Color = "<color=#458B00>",
                FactionNone_Chat_Color = "<color=#ffeb33>",

                FactionA_GUI_Color = "0.9 0.1 0.2 1.0",
                FactionB_GUI_Color = "0.2 0.67 1.0 1.0",
                FactionC_GUI_Color = "0.2 0.6 0.2 1.0",
                FactionNone_GUI_Color = "1.0 0.92 .20 1.0",

                FactionA_Name = "Faction A",
                FactionB_Name = "Faction B",
                FactionC_Name = "Faction C",
                FactionNone_Name = "Rebels",

                FactionAGroupName = "FactionA",
                FactionBGroupName = "FactionB",
                FactionCGroupName = "FactionC",

                FFDisabled = true,
                BuildingProtectionEnabled = true,

                FF_DamageScale = 0.0f,
                UsePluginChatControl = true,
                CheckLeader_Interval = 60,
                Save_Interval = 15,
                Use_FactionGear = false,
                Use_Ranks = false,
                Use_Taxes = false,
                Use_Groups = false,

                z_FactionA_Gear = new List<Gear>
                    {
                        new Gear
                        {
                            name = "T-Shirt",
                            shortname = "tshirt",
                            amount = 1,
                            container = "wear",
                            skin = 101
                        }
                    },
                z_FactionB_Gear = new List<Gear>
                    {
                        new Gear
                        {
                            name = "T-Shirt",
                            shortname = "tshirt",
                            amount = 1,
                            container = "wear",
                            skin = 14177
                        }
                    },
                z_FactionC_Gear = new List<Gear>
                    {
                        new Gear
                        {
                            name = "T-Shirt",
                            shortname = "tshirt",
                            amount = 1,
                            container = "wear",
                            skin = 14181
                        }
                    }
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
            {"FactionChangeError", "You are currently assigned to a faction. To switch factions have an admin perform console.command \"faction.unassign {0}\" "},
            {"FFBuildings", "This is a friendly structure owned by {0}! You must be authorized on a nearby Tool Cupboard to damage it!"},
            {"FFs", "{0} is on your faction!"},
            {"Payment", "You have received {0} {1} for that kill!" },
            {"FactionUnassignFormating", "Format: faction.unassign <PARTIAL_PLAYERNAME>"},
            {"PlayerReturns", "{0} has returned!"},
            {"PlayerLeft", "{0} has left!"},
            {"PlayerNew", "{0} has joined the fight!"},
            {"UnassignSuccess", "{0} has been successfully removed from {1}."},
            {"RemovedFromFaction", "You have been removed from your faction. To join a new one type /faction join"},
            {"UnassignError", "There was a error assigning a new faction"},
            {"InvalidAssignment", "Invalid faction assignment."},
            {"multiPlayers", "Multiple players found with that name" },
            {"NoPlayers", "No players found" },
            {"NoFactionError", "You are not a faction member"},
            {"NotAuth", "You do not have permission to use this command."},
            {"DeathMessage", " has killed " },
            {"inFactionChat", "[FACTIONCHAT]" },
            {"TotalGUI", "Faction Player Total:    " },
            {"FactionSelectionTitle", "Which Faction would you like to join?" },
            {"MembersGUI", "Members" },
            {"Close", "Close" },
            {"Joined", "has joined" },
            {"NewLeader", "{0} has become the new leader of {1}!" },
            {"NotLeader", "You are not the leader of a faction" },
            {"TaxBoxActivated", "You have activated Tax Box Selection Mode. Please open a Large Wooden Box to make it a Faction Tax Box. You can deactivate this mode by re-entering /taxbox" },
            {"TaxBoxDeActivated", "You have Deactivated Tax Box Selection Mode." },
            {"NewTaxBox", "You have activated a new Tax Box. Set a tax rate by typing /ftax <amount>." },
            {"TaxBoxError", "A Tax Box must be a Large Wooden Box." },
            {"TaxBox", "This is your Tax Box." },
            {"ftaxFormatError", "Format: /faction tax <rate> . The rate must be a number between 0 and 25." },
            {"NewTaxRate", "The Faction Tax has been changed to {0}."},
            {"TaxLimit", "The Faction Tax may not exceed 25%" },
            {"TaxesDisabled", "Taxes are Disabled." },
            {"TaxBoxFull", "Your Tax Box is Full. Remove items or designate a new one to continue collecting." },
            {"FactionJoin1"," - Displays Faction Selection Screen" },
            {"FactionJoin", "/faction join" },
            {"FactionList1"," - Displays a list of Faction Members" },
            {"FactionList", "/faction list" },
            {"FactionChat1"," - Sends the message to Faction Members ONLY" },
            {"FactionChat", "/fc <message>" },
            {"FactionTaxBox1"," - Enables the Tax Box Selection Mode" },
            {"FactionTaxBox", "/faction taxbox" },
            {"FactionTax1"," - Set the Faction Tax" },
            {"FactionTax", "/faction tax" },
            {"FactionUnassign1"," - UnAssigns the player from their Faction" },
            {"FactionUnassign", "/faction unassign <PartialPlayerName>" }
        };
        #endregion
    }
}
