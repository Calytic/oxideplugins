using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Rewards", "Tarek", "1.3.5", ResourceId = 1961)]
    [Description("Reward players for killing animals, players, other entities, and activity using Economic and/or ServerRewards")]

    class Rewards : RustPlugin
    {
        [PluginReference]
        Plugin Economics;
        [PluginReference]
        Plugin ServerRewards;
        [PluginReference]
        Plugin Friends;
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin HumanNPC;

        private bool IsFriendsLoaded = false;
        private bool IsEconomicsLoaded = false;
        private bool IsServerRewardsLoaded = false;
        private bool IsClansLoaded = false;
        private bool IsNPCLoaded = false;

        private bool HappyHourActive = false;
        TimeSpan hhstart; TimeSpan hhend; TimeSpan hhnow;

        StoredData storedData;

        RewardRates rr; Multipliers m; Options o; Rewards_Version rv;//Strings str;
        public List<string> Options_itemList = new List<string> { "NPCReward_Enabled", "VIPMultiplier_Enabled", "ActivityReward_Enabled", "WelcomeMoney_Enabled", "WeaponMultiplier_Enabled", "DistanceMultiplier_Enabled", "UseEconomicsPlugin", "UseServerRewardsPlugin", "UseFriendsPlugin", "UseClansPlugin", "Economincs_TakeMoneyFromVictim", "ServerRewards_TakeMoneyFromVictim", "PrintToConsole", "HappyHour_Enabled" };
        public List<string> Multipliers_itemList = new List<string> { "LR300","VIPMultiplier","HuntingBow", "Crossbow", "AssaultRifle", "PumpShotgun", "SemiAutomaticRifle", "Thompson", "CustomSMG", "BoltActionRifle", "TimedExplosiveCharge", "M249", "EokaPistol", "Revolver", "WaterpipeShotgun", "SemiAutomaticPistol", "DoubleBarrelShotgun", "SatchelCharge", "distance_50", "distance_100", "distance_200", "distance_300", "distance_400", "HappyHourMultiplier" };
        public List<string> Rewards_itemList = new List<string> { "human", "bear", "wolf", "chicken", "horse", "boar", "stag", "helicopter", "autoturret", "ActivityRewardRate_minutes", "ActivityReward", "WelcomeMoney", "HappyHour_BeginHour", "HappyHour_DurationInHours", "HappyHour_EndHour", "NPCKill_Reward" };
        //public List<string> Strings_itemList = new List<string> { "CustomPermissionName" };
        //private Strings strings = new Strings();
        private Rewards_Version rewardsversion = new Rewards_Version();
        private RewardRates rewardrates = new RewardRates();
        private Options options = new Options();
        private Multipliers multipliers = new Multipliers();

        private Dictionary<BasePlayer, int> LastReward = new Dictionary<BasePlayer, int>();

        private void OnServerInitialized()
        {            
            if (options.UseEconomicsPlugin && Economics != null)
                IsEconomicsLoaded = true;
            else if (options.UseEconomicsPlugin && Economics == null)
                PrintWarning("Plugin Economics was not found! Can't reward players using Economics.");
            if (options.UseServerRewardsPlugin && ServerRewards != null)
                IsServerRewardsLoaded = true;
            else if (options.UseServerRewardsPlugin && ServerRewards == null)
                PrintWarning("Plugin ServerRewards was not found! Can't reward players using ServerRewards.");
            if (options.UseFriendsPlugin && Friends != null)
                IsFriendsLoaded = true;
            else if (options.UseFriendsPlugin && Friends == null)
                PrintWarning("Plugin Friends was not found! Can't check if victim is friend to killer.");
            if (options.UseClansPlugin && Clans != null)
                IsClansLoaded = true;
            else if (options.UseClansPlugin && Clans == null)
                PrintWarning("Plugin Clans was not found! Can't check if victim is in the same clan of killer.");
            if (options.NPCReward_Enabled && HumanNPC != null)
                IsNPCLoaded = true;
            else if (options.NPCReward_Enabled && HumanNPC == null)
                PrintWarning("Plugin HumanNPC was not found! Can't reward players on NPC kill.");
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            //Config["Strings"] = str;
            Config["Rewards_Version"] = rv;
            Config["Rewards"] = rr;
            Config["Multipliers"] = m;
            Config["Options"] = o;
            SaveConfig();
            LoadConfig();
        }
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["KillReward"] = "You received {0}. Reward for killing {1}",
                ["ActivityReward"] = "You received {0}. Reward for activity",
                ["WelcomeReward"] = "Welcome to server! You received {0} as a welcome reward",
                ["VictimNoMoney"] = "{0} doesn't have enough money.",
                ["SetRewards"] = "Varaibles you can set:",
                ["RewardSet"] = "Reward was set",
                ["stag"] = "a stag",
                ["boar"] = "a boar",
                ["horse"] = "a horse",
                ["bear"] = "a bear",
                ["wolf"] = "a wolf",
                ["chicken"] = "a chicken",
                ["autoturret"] = "an autoturret",
                ["helicopter"] = "a helicopter",
                ["Prefix"] = "Rewards",
                ["HappyHourStart"] = "Happy hour started",
                ["HappyHourEnd"] = "Happy hour ended"
            }, this);
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>("Rewards", storedData);
            Puts("Data saved");
        }
        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Rewards");
        }
        private void SetDefaultConfigValues()
        {
            //str = new Strings
            //{
            //    CustomPermissionName = "null"
            //};
            rv = new Rewards_Version
            {
                Version = this.Version.ToString()
            };
            rr = new RewardRates
            {
                human = 50,
                bear = 35,
                wolf = 30,
                chicken = 15,
                horse = 15,
                boar = 15,
                stag = 10,
                helicopter = 250,
                autoturret = 150,
                ActivityRewardRate_minutes = 30,
                ActivityReward = 25,
                WelcomeMoney = 250,
                HappyHour_BeginHour = 20,
                HappyHour_DurationInHours = 2,
                HappyHour_EndHour = 23,
                NPCKill_Reward = 50
            };
            m = new Multipliers
            {
                AssaultRifle = 1.5,
                BoltActionRifle = 1.5,
                HuntingBow = 1,
                PumpShotgun = 1,
                Thompson = 1.3,
                SemiAutomaticRifle = 1.3,
                Crossbow = 1.3,
                CustomSMG = 1.5,
                M249 = 1.5,
                SemiAutomaticPistol = 1,
                WaterpipeShotgun = 1.4,
                EokaPistol = 1.1,
                Revolver = 1.2,
                TimedExplosiveCharge = 2,
                SatchelCharge = 2,
                DoubleBarrelShotgun = 1.5,
                distance_50 = 1,
                distance_100 = 1.3,
                distance_200 = 1.5,
                distance_300 = 2,
                distance_400 = 3,
                HappyHourMultiplier = 2,
                VIPMultiplier = 2,
                LR300 = 1.5
            };

            o = new Options
            {
                ActivityReward_Enabled = true,
                WelcomeMoney_Enabled = true,
                UseEconomicsPlugin = true,
                UseServerRewardsPlugin = false,
                UseFriendsPlugin = true,
                UseClansPlugin = true,
                Economincs_TakeMoneyFromVictim = true,
                ServerRewards_TakeMoneyFromVictim = false,
                WeaponMultiplier_Enabled = true,
                DistanceMultiplier_Enabled = true,
                PrintToConsole = true,
                HappyHour_Enabled = true,
                VIPMultiplier_Enabled = false,
                NPCReward_Enabled = false
            };
        }
        private void FixConfig()
        {
            try
            {
                
                Dictionary<string, object> temp;
                Dictionary<string, object> temp2;
                Dictionary<string, object> temp3;
                Dictionary<string, object> temp4;
                try { temp = (Dictionary<string, object>)Config["Rewards"]; } catch { Config["Rewards"] = rr; SaveConfig(); temp = (Dictionary<string, object>)Config["Rewards"]; }
                try { temp2 = (Dictionary<string, object>)Config["Options"]; } catch { Config["Options"] = o; SaveConfig(); temp2 = (Dictionary<string, object>)Config["Options"]; }
                try { temp3 = (Dictionary<string, object>)Config["Multipliers"]; } catch { Config["Multipliers"] = m; SaveConfig(); temp3 = (Dictionary<string, object>)Config["Multipliers"]; }
                //try { temp4 = (Dictionary<string, object>)Config["Strings"]; } catch { Config["Strings"] = str; SaveConfig(); temp4 = (Dictionary<string, object>)Config["Strings"]; Puts(temp4["CustomPermissionName"].ToString()); }
                foreach (var s in Rewards_itemList)
                {
                    if (!temp.ContainsKey(s))
                    {
                        Config["Rewards", s] = rr.GetItemByString(s);
                        SaveConfig();
                    }
                }
                foreach (var s in Options_itemList)
                {
                    if (!temp2.ContainsKey(s))
                    {
                        Config["Options", s] = o.GetItemByString(s);
                        SaveConfig();
                    }
                }
                foreach (var s in Multipliers_itemList)
                {
                    if (!temp3.ContainsKey(s))
                    {
                        Config["Multipliers", s] = m.GetItemByString(s);
                        SaveConfig();
                    }
                }                
                Config["Rewards_Version", "Version"] = this.Version.ToString();
                SaveConfig();
            }
            catch (Exception ex)
            { Puts(ex.Message); Puts("Couldn't fix. Creating new config file"); Config.Clear(); LoadDefaultConfig(); Loadcfg(); }
        }
        void Loadcfg()
        {
            SetDefaultConfigValues();
            try
            {
                Dictionary<string, object> temp = (Dictionary<string, object>)Config["Rewards_Version"];
                if (this.Version.ToString() != temp["Version"].ToString())
                {
                    Puts("Outdated config file. Fixing");
                    FixConfig();
                }              
            }
            catch (Exception e)
            {
                Puts("Outdated config file. Fixing");
                FixConfig();                            
            }
            try
            {
                Dictionary<string, object> temp = (Dictionary<string, object>)Config["Rewards"];
                rewardrates.ActivityReward = Convert.ToDouble(temp["ActivityReward"]);
                rewardrates.ActivityRewardRate_minutes = Convert.ToDouble(temp["ActivityRewardRate_minutes"]);
                rewardrates.autoturret = Convert.ToDouble(temp["autoturret"]);
                rewardrates.bear = Convert.ToDouble(temp["bear"]);
                rewardrates.boar = Convert.ToDouble(temp["boar"]);
                rewardrates.chicken = Convert.ToDouble(temp["chicken"]);
                rewardrates.helicopter = Convert.ToDouble(temp["helicopter"]);
                rewardrates.horse = Convert.ToDouble(temp["horse"]);
                rewardrates.human = Convert.ToDouble(temp["human"]);
                rewardrates.stag = Convert.ToDouble(temp["stag"]);
                rewardrates.WelcomeMoney = Convert.ToDouble(temp["WelcomeMoney"]);
                rewardrates.wolf = Convert.ToDouble(temp["wolf"]);
                rewardrates.HappyHour_BeginHour = Convert.ToDouble(temp["HappyHour_BeginHour"]);
                rewardrates.HappyHour_DurationInHours = Convert.ToDouble(temp["HappyHour_DurationInHours"]);
                rewardrates.HappyHour_EndHour = Convert.ToDouble(temp["HappyHour_EndHour"]);
                rewardrates.NPCKill_Reward = Convert.ToDouble(temp["NPCKill_Reward"]);

                Dictionary<string, object> temp2 = (Dictionary<string, object>)Config["Options"];
                options.ActivityReward_Enabled = (bool)temp2["ActivityReward_Enabled"];
                options.DistanceMultiplier_Enabled = (bool)temp2["DistanceMultiplier_Enabled"];
                options.Economincs_TakeMoneyFromVictim = (bool)temp2["Economincs_TakeMoneyFromVictim"];
                options.ServerRewards_TakeMoneyFromVictim = (bool)temp2["ServerRewards_TakeMoneyFromVictim"];
                options.UseClansPlugin = (bool)temp2["UseClansPlugin"];
                options.UseEconomicsPlugin = (bool)temp2["UseEconomicsPlugin"];
                options.UseFriendsPlugin = (bool)temp2["UseFriendsPlugin"];
                options.UseServerRewardsPlugin = (bool)temp2["UseServerRewardsPlugin"];
                options.WeaponMultiplier_Enabled = (bool)temp2["WeaponMultiplier_Enabled"];
                options.WelcomeMoney_Enabled = (bool)temp2["WelcomeMoney_Enabled"];
                options.PrintToConsole = (bool)temp2["PrintToConsole"];
                options.VIPMultiplier_Enabled = (bool)temp2["VIPMultiplier_Enabled"];
                options.NPCReward_Enabled = (bool)temp2["NPCReward_Enabled"];
                options.HappyHour_Enabled = (bool)temp2["HappyHour_Enabled"];

                Dictionary<string, object> temp3 = (Dictionary<string, object>)Config["Multipliers"];
                multipliers.AssaultRifle = Convert.ToDouble(temp3["AssaultRifle"]);
                multipliers.BoltActionRifle = Convert.ToDouble(temp3["BoltActionRifle"]);
                multipliers.HuntingBow = Convert.ToDouble(temp3["HuntingBow"]);
                multipliers.PumpShotgun = Convert.ToDouble(temp3["PumpShotgun"]);
                multipliers.Thompson = Convert.ToDouble(temp3["Thompson"]);
                multipliers.SemiAutomaticRifle = Convert.ToDouble(temp3["SemiAutomaticRifle"]);
                multipliers.Crossbow = Convert.ToDouble(temp3["Crossbow"]);
                multipliers.CustomSMG = Convert.ToDouble(temp3["CustomSMG"]);
                multipliers.M249 = Convert.ToDouble(temp3["M249"]);
                multipliers.TimedExplosiveCharge = Convert.ToDouble(temp3["TimedExplosiveCharge"]);
                multipliers.EokaPistol = Convert.ToDouble(temp3["EokaPistol"]);
                multipliers.Revolver = Convert.ToDouble(temp3["Revolver"]);
                multipliers.SemiAutomaticPistol = Convert.ToDouble(temp3["SemiAutomaticPistol"]);
                multipliers.WaterpipeShotgun = Convert.ToDouble(temp3["WaterpipeShotgun"]);
                multipliers.DoubleBarrelShotgun = Convert.ToDouble(temp3["DoubleBarrelShotgun"]);
                multipliers.SatchelCharge = Convert.ToDouble(temp3["SatchelCharge"]);
                multipliers.distance_50 = Convert.ToDouble(temp3["distance_50"]);
                multipliers.distance_100 = Convert.ToDouble(temp3["distance_100"]);
                multipliers.distance_200 = Convert.ToDouble(temp3["distance_200"]);
                multipliers.distance_300 = Convert.ToDouble(temp3["distance_300"]);
                multipliers.distance_400 = Convert.ToDouble(temp3["distance_400"]);
                multipliers.VIPMultiplier = Convert.ToDouble(temp3["VIPMultiplier"]);
                multipliers.HappyHourMultiplier = Convert.ToDouble(temp3["HappyHourMultiplier"]);
                multipliers.LR300 = Convert.ToDouble(temp3["LR300"]);

                //Dictionary<string, object> temp4 = (Dictionary<string, object>)Config["Strings"];
                //str.CustomPermissionName = temp4["CustomPermissionName"].ToString();
            }
            catch
            {
                FixConfig(); Loadcfg();
            }
        }
        void Init()
        {
            permission.RegisterPermission("rewards.admin", this);
            permission.RegisterPermission("rewards.vip", this);
            LoadDefaultMessages();
            Loadcfg();
            if (options.HappyHour_Enabled)
            {
                hhstart = new TimeSpan(Convert.ToInt32(rewardrates.HappyHour_BeginHour), 0, 0);
                hhend = new TimeSpan(Convert.ToInt32(rewardrates.HappyHour_EndHour), 0, 0);
                
            }
            #region Activity Check
            if (options.ActivityReward_Enabled || options.HappyHour_Enabled)
            {
                timer.Repeat(60, 0, () =>
                {
                    if (options.ActivityReward_Enabled)
                    {
                        foreach (var p in BasePlayer.activePlayerList)
                        {
                            if (Convert.ToDouble(p.secondsConnected) / 60 > rewardrates.ActivityRewardRate_minutes)
                            {
                                if (LastReward.ContainsKey(p))
                                {
                                    if (Convert.ToDouble(p.secondsConnected - LastReward[p]) / 60 > rewardrates.ActivityRewardRate_minutes)
                                    {
                                        RewardPlayer(p, rewardrates.ActivityReward);
                                        LastReward[p] = p.secondsConnected;
                                    }
                                }
                                else
                                {
                                    RewardPlayer(p, rewardrates.ActivityReward);
                                    LastReward.Add(p, p.secondsConnected);
                                }
                            }
                        }
                    }
                    if (options.HappyHour_Enabled)
                    {
                        if (!HappyHourActive)
                        {
                            if (GameTime() >= rewardrates.HappyHour_BeginHour)
                            {
                                HappyHourActive = true;
                                Puts("Happy hour started. Ending at " + rewardrates.HappyHour_EndHour);
                                BroadcastMessage(Lang("Prefix"), Lang("HappyHourStart"));
                            }
                        }
                        else
                        {
                            if (GameTime() > rewardrates.HappyHour_EndHour)
                            {
                                HappyHourActive = false;
                                Puts("Happy hour ended");
                                BroadcastMessage(Lang("Prefix"), Lang("HappyHourEnd"));
                            }
                        }
                    }

                });
            }
            #endregion
        }
        bool checktime(float gtime, double cfgtime)
        {

            return false;
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (options.WelcomeMoney_Enabled)
            {
                if (!storedData.Players.Contains(player.UserIDString))
                {
                    RewardPlayer(player, rewardrates.WelcomeMoney, 1, null, true);
                    storedData.Players.Add(player.UserIDString);
                    SaveData();
                }
            }
        }
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        bool HasPerm(BasePlayer p, string pe) => permission.UserHasPermission(p.userID.ToString(), pe);
        void SendChatMessage(BasePlayer player, string prefix, string msg = null, object uid = null) => rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg, null, uid?.ToString() ?? "0");
        void BroadcastMessage(string prefix, string msg = null, object uid = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg, null);
        void OnKillNPC(BasePlayer victim, HitInfo info)
        {
            
            if (options.NPCReward_Enabled)
            {
                if (info?.Initiator?.ToPlayer() == null)
                    return;
                double totalmultiplier = 1;

                if (options.DistanceMultiplier_Enabled || options.WeaponMultiplier_Enabled)
                    totalmultiplier = (options.DistanceMultiplier_Enabled ? multipliers.GetDistanceM(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) * (options.WeaponMultiplier_Enabled ? multipliers.GetWeaponM(info?.Weapon?.GetItem()?.info?.displayName?.english) : 1) * (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(info?.Initiator?.ToPlayer(), "rewards.vip")) ? multipliers.VIPMultiplier : 1) * ((HasPerm(info?.Initiator?.ToPlayer(), "rewards.vip")) ? multipliers.VIPMultiplier : 1);

                RewardPlayer(info?.Initiator?.ToPlayer(), rewardrates.NPCKill_Reward, totalmultiplier, victim.displayName);
            }
        }
        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {          
            if (victim == null)
                return;
            if (info?.Initiator?.ToPlayer() == null)
                return;
            double totalmultiplier = 1;
            
            if (options.DistanceMultiplier_Enabled || options.WeaponMultiplier_Enabled)
                totalmultiplier = (options.DistanceMultiplier_Enabled ? multipliers.GetDistanceM(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) * (options.WeaponMultiplier_Enabled ? multipliers.GetWeaponM(info?.Weapon?.GetItem()?.info?.displayName?.english) : 1) * (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(info?.Initiator?.ToPlayer(), "rewards.vip")) ? multipliers.VIPMultiplier : 1) * ((HasPerm(info?.Initiator?.ToPlayer(), "rewards.vip")) ? multipliers.VIPMultiplier : 1);
            
            if (victim.ToPlayer() != null)
            {
                if (victim.ToPlayer().userID <= 2147483647)
                    return;
                else if (info?.Initiator?.ToPlayer().userID == victim.ToPlayer().userID)
                    return;
                else { RewardForPlayerKill(info?.Initiator?.ToPlayer(), victim.ToPlayer(), totalmultiplier); return; }
            }
            else if (victim.name.Contains("autospawn/animals"))
            {
                try
                {
                    var AnimalName = victim.name.Split(new[] { "autospawn/animals/" }, StringSplitOptions.None)[1].Split('.')[0];
                    double rewardmoney = 0;
                    if (AnimalName == "stag")
                        rewardmoney = rewardrates.stag;
                    else if (AnimalName == "boar")
                        rewardmoney = rewardrates.boar;
                    else if (AnimalName == "horse")
                        rewardmoney = rewardrates.horse;
                    else if (AnimalName == "bear")
                        rewardmoney = rewardrates.bear;
                    else if (AnimalName == "wolf")
                        rewardmoney = rewardrates.wolf;
                    else if (AnimalName == "chicken")
                        rewardmoney = rewardrates.chicken;
                    else
                        return;
                    RewardPlayer(info?.Initiator?.ToPlayer(), rewardmoney, totalmultiplier, Lang(AnimalName, info?.Initiator?.ToPlayer().UserIDString));
                }
                catch { }
            }
            else if (victim.name.Contains("helicopter/patrolhelicopter.prefab"))
            {
                RewardPlayer(info?.Initiator?.ToPlayer(), rewardrates.helicopter, totalmultiplier, Lang("helicopter", info?.Initiator?.ToPlayer().UserIDString));
            }
            else if (victim.name == "assets/prefabs/npc/autoturret/autoturret_deployed.prefab")
            {
                RewardPlayer(info?.Initiator?.ToPlayer(), rewardrates.autoturret, totalmultiplier, Lang("autoturret", info?.Initiator?.ToPlayer().UserIDString));
            }
        }
        private void RewardPlayer(BasePlayer player, double amount, double multiplier = 1, string reason = null, bool isWelcomeReward = false)
        {
            
            if (amount > 0)
            {
                amount = amount * multiplier;
                if (options.UseEconomicsPlugin)
                    Economics?.Call("Deposit", player.userID, amount);
                if (options.UseServerRewardsPlugin)
                    ServerRewards?.Call("AddPoints", new object[] { player.userID, amount });
                if (!isWelcomeReward)
                {
                    SendChatMessage(player, Lang("Prefix"), reason == null ? Lang("ActivityReward", player.UserIDString, amount) : Lang("KillReward", player.UserIDString, amount, reason));
                    ConVar.Server.Log("/oxide/logs/RewardsLog.txt", player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
                    if (options.PrintToConsole)
                        Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
                }
                else
                {
                    SendChatMessage(player, Lang("Prefix"), Lang("WelcomeReward", player.UserIDString, amount));
                    ConVar.Server.Log("/oxide/logs/RewardsLog.txt", player.displayName + " got " + amount + " as a welcome reward");
                    if (options.PrintToConsole)
                        Puts(player.displayName + " got " + amount + " as a welcome reward");
                }
            }
        }
        private static float GameTime()
        {           
            return TOD_Sky.Instance.Cycle.Hour;
        }
        private void RewardForPlayerKill(BasePlayer player, BasePlayer victim, double multiplier = 1)
        {
            if (rewardrates.human > 0)
            {
                bool success = true;
                bool isFriend = false;
                if (IsFriendsLoaded)
                    isFriend = (bool)Friends?.CallHook("HasFriend", player.userID, victim.userID);               
                if (!isFriend && IsClansLoaded)
                {
                    string pclan = (string)Clans?.CallHook("GetClanOf", player); string vclan = (string)Clans?.CallHook("GetClanOf", victim);
                    if (pclan == vclan)
                        isFriend = true;
                }
                if (!isFriend)
                {
                    if (IsEconomicsLoaded) //Eco
                    {
                        if (options.Economincs_TakeMoneyFromVictim)
                        {
                            if (!(bool)Economics?.Call("Transfer", victim.userID, player.userID, rewardrates.human * multiplier))
                            {
                                SendChatMessage(player, Lang("Prefix"), Lang("VictimNoMoney", player.UserIDString, victim.displayName));
                                success = false;
                            }
                        }
                        else
                            Economics?.Call("Deposit", player.userID, rewardrates.human * multiplier);
                    }
                    if (IsServerRewardsLoaded) //ServerRewards
                    {
                        if (options.ServerRewards_TakeMoneyFromVictim)
                            ServerRewards?.Call("TakePoints", new object[] { victim.userID, rewardrates.human * multiplier });
                        ServerRewards?.Call("AddPoints", new object[] { player.userID, rewardrates.human * multiplier });
                        success = true;
                    }
                    if (success) //Send message if transaction was successful
                    {
                        SendChatMessage(player, Lang("Prefix"), Lang("KillReward", player.UserIDString, rewardrates.human * multiplier, victim.displayName));
                        ConVar.Server.Log("/oxide/logs/RewardsLog.txt", player.displayName + " got " + rewardrates.human * multiplier + " for killing " + victim.displayName);
                        if (options.PrintToConsole)
                            Puts(player.displayName + " got " + rewardrates.human * multiplier + " for killing " + victim.displayName);
                    }
                }
            }
        }
        [ConsoleCommand("setreward")]
        private void setreward(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin)
            {
                try
                {
                    var args = arg.Args;
                    Config["Rewards", args[0]] = Convert.ToDouble(args[1]);
                    SaveConfig();
                    try
                    {
                        Loadcfg();
                    }
                    catch
                    {
                        FixConfig();
                    }
                    arg.ReplyWith("Reward set");
                }
                catch { arg.ReplyWith("Varaibles you can set: 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'autoturret', 'ActivityReward' 'ActivityRewardRate_minutes', 'WelcomeMoney'"); }
            }
        }
        [ConsoleCommand("showrewards")]
        private void showrewards(ConsoleSystem.Arg arg)
        {
            if (arg.isAdmin)
                arg.ReplyWith(String.Format("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, autoturret = {8} Activity Reward Rate (minutes) = {9}, Activity Reward = {10}, WelcomeMoney = {11}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney));
        }
        [ChatCommand("setreward")]
        private void setrewardCommand(BasePlayer player, string command, string[] args)
        {
            if (HasPerm(player, "rewards.admin"))
            {
                try
                {
                    Config["Rewards", args[0]] = Convert.ToDouble(args[1]);
                    SaveConfig();
                    try
                    {
                        Loadcfg();
                    }
                    catch
                    {
                        FixConfig();
                    }
                    SendChatMessage(player, Lang("Prefix"), Lang("RewardSet", player.UserIDString));
                }
                catch { SendChatMessage(player, Lang("Prefix"), Lang("SetRewards", player.UserIDString) + " 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'autoturret', 'ActivityReward', 'ActivityRewardRate_minutes', 'WelcomeMoney'"); }
            }
        }
        [ChatCommand("showrewards")]
        private void showrewardsCommand(BasePlayer player, string command, string[] args)
        {
            if (HasPerm(player, "rewards.admin"))
                SendChatMessage(player, Lang("Prefix"), String.Format("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, autoturret = {8} Activity Reward Rate (minutes) = {9}, Activity Reward = {10}, WelcomeMoney = {11}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney));
        }
        class StoredData
        {
            public HashSet<string> Players = new HashSet<string>();
            public StoredData()
            {
            }
        }
        class RewardRates
        {
            public double human { get; set; }
            public double bear { get; set; }
            public double wolf { get; set; }
            public double chicken { get; set; }
            public double horse { get; set; }
            public double boar { get; set; }
            public double stag { get; set; }
            public double helicopter { get; set; }
            public double autoturret { get; set; }
            public double ActivityRewardRate_minutes { get; set; }
            public double ActivityReward { get; set; }
            public double WelcomeMoney { get; set; }
            public double HappyHour_BeginHour { get; set; }
            public double HappyHour_DurationInHours { get; set; }
            public double HappyHour_EndHour { get; set; }
            public double NPCKill_Reward { get; set; }
            public double GetItemByString(string itemName)
            {
                if (itemName == "human")
                    return this.human;
                else if (itemName == "bear")
                    return this.bear;
                else if (itemName == "wolf")
                    return this.wolf;
                else if (itemName == "chicken")
                    return this.chicken;
                else if (itemName == "horse")
                    return this.horse;
                else if (itemName == "boar")
                    return this.boar;
                else if (itemName == "stag")
                    return this.stag;
                else if (itemName == "helicopter")
                    return this.helicopter;
                else if (itemName == "autoturret")
                    return this.autoturret;
                else if (itemName == "ActivityRewardRate_minutes")
                    return this.ActivityRewardRate_minutes;
                else if (itemName == "ActivityReward")
                    return this.ActivityReward;
                else if (itemName == "WelcomeMoney")
                    return this.WelcomeMoney;
                else if (itemName == "HappyHour_BeginHour")
                    return this.HappyHour_BeginHour;
                else if (itemName == "HappyHour_DurationInHours")
                    return this.HappyHour_DurationInHours;
                else if (itemName == "HappyHour_EndHour")
                    return this.HappyHour_EndHour;
                else if (itemName == "NPCKill_Reward")
                    return this.NPCKill_Reward;
                else
                    return 0;
            }
        }
        class Multipliers
        {
            public double HuntingBow { get; set; }
            public double Crossbow { get; set; }
            public double AssaultRifle { get; set; }
            public double PumpShotgun { get; set; }
            public double SemiAutomaticRifle { get; set; }
            public double Thompson { get; set; }
            public double CustomSMG { get; set; }
            public double BoltActionRifle { get; set; }
            public double TimedExplosiveCharge { get; set; }
            public double M249 { get; set; }
            public double EokaPistol { get; set; }
            public double Revolver { get; set; }
            public double WaterpipeShotgun { get; set; }
            public double SemiAutomaticPistol { get; set; }
            public double DoubleBarrelShotgun { get; set; }
            public double SatchelCharge { get; set; }
            public double distance_50 { get; set; }
            public double distance_100 { get; set; }
            public double distance_200 { get; set; }
            public double distance_300 { get; set; }
            public double distance_400 { get; set; }
            public double HappyHourMultiplier { get; set; }
            public double VIPMultiplier { get; set; }
            public double CustomPermissionMultiplier { get; set; }
            public double LR300 { get; set; }
            public double GetWeaponM(string wn)
            {
                if (wn == "Assault Rifle")
                    return this.AssaultRifle;
                else if (wn == "Hunting Bow")
                    return this.HuntingBow;
                else if (wn == "Bolt Action Rifle")
                    return this.BoltActionRifle;
                else if (wn == "Crossbow")
                    return this.Crossbow;
                else if (wn == "Thompson")
                    return this.Thompson;
                else if (wn == "Eoka Pistol")
                    return this.EokaPistol;
                else if (wn == "Revolver")
                    return this.Revolver;
                else if (wn == "Custom SMG")
                    return this.CustomSMG;
                else if (wn == "Semi-Automatic Rifle")
                    return this.SemiAutomaticRifle;
                else if (wn == "Semi-Automatic Pistol")
                    return this.SemiAutomaticPistol;
                else if (wn == "Pump Shotgun")
                    return this.PumpShotgun;
                else if (wn == "Waterpipe Shotgun")
                    return this.WaterpipeShotgun;
                else if (wn == "M249")
                    return this.M249;
                else if (wn == "Explosivetimed")
                    return this.TimedExplosiveCharge;
                else if (wn == "Explosivesatchel")
                    return this.SatchelCharge;
                else if (wn == "Double Barrel Shotgun")
                    return this.DoubleBarrelShotgun;
                else if (wn == "LR-300 Assault Rifle")
                    return this.LR300;
                else
                    return 1;
            }
            public double GetDistanceM(float distance)
            {
               
                if (distance >= 400)
                    return this.distance_400;
                else if (distance >= 300)
                    return this.distance_300;
                else if (distance >= 200)
                    return this.distance_200;
                else if (distance >= 100)
                    return this.distance_100;
                else if (distance >= 50)
                    return this.distance_50;
                else
                    return 1;
            }

            public double GetItemByString(string itemName)
            {
                if (itemName == "HuntingBow")
                    return this.HuntingBow;
                else if (itemName == "Crossbow")
                    return this.Crossbow;
                else if (itemName == "AssaultRifle")
                    return this.AssaultRifle;
                else if (itemName == "PumpShotgun")
                    return this.PumpShotgun;
                else if (itemName == "SemiAutomaticRifle")
                    return this.SemiAutomaticRifle;
                else if (itemName == "Thompson")
                    return this.Thompson;
                else if (itemName == "CustomSMG")
                    return this.CustomSMG;
                else if (itemName == "BoltActionRifle")
                    return this.BoltActionRifle;
                else if (itemName == "TimedExplosiveCharge")
                    return this.TimedExplosiveCharge;
                else if (itemName == "M249")
                    return this.M249;
                else if (itemName == "EokaPistol")
                    return this.EokaPistol;
                else if (itemName == "Revolver")
                    return this.Revolver;
                else if (itemName == "WaterpipeShotgun")
                    return this.WaterpipeShotgun;
                else if (itemName == "SemiAutomaticPistol")
                    return this.SemiAutomaticPistol;
                else if (itemName == "DoubleBarrelShotgun")
                    return this.DoubleBarrelShotgun;
                else if (itemName == "SatchelCharge")
                    return this.SatchelCharge;
                else if (itemName == "distance_50")
                    return this.distance_50;
                else if (itemName == "distance_100")
                    return this.distance_100;
                else if (itemName == "distance_200")
                    return this.distance_200;
                else if (itemName == "distance_300")
                    return this.distance_300;
                else if (itemName == "distance_400")
                    return this.distance_400;
                else if (itemName == "HappyHourMultiplier")
                    return this.HappyHourMultiplier;
                else if (itemName == "VIPMultiplier")
                    return this.VIPMultiplier;
                else if (itemName == "CustomPermissionMultiplier")
                    return this.CustomPermissionMultiplier;
                else if (itemName == "LR300")
                    return this.LR300;
                else
                    return 0;
            }
            
        }
        class Options
        {
            public bool ActivityReward_Enabled { get; set; }
            public bool WelcomeMoney_Enabled { get; set; }
            public bool WeaponMultiplier_Enabled { get; set; }
            public bool DistanceMultiplier_Enabled { get; set; }
            public bool HappyHour_Enabled { get; set; }
            public bool VIPMultiplier_Enabled { get; set; }
            public bool UseEconomicsPlugin { get; set; }
            public bool UseServerRewardsPlugin { get; set; }
            public bool UseFriendsPlugin { get; set; }
            public bool UseClansPlugin { get; set; }
            public bool Economincs_TakeMoneyFromVictim { get; set; }
            public bool ServerRewards_TakeMoneyFromVictim { get; set; }
            public bool PrintToConsole { get; set; }           
            public bool CustomPermissionMultiplier_Enabled { get; set; }  
            public bool NPCReward_Enabled { get; set; }         
            public bool GetItemByString(string itemName)
            {
                if (itemName == "ActivityReward_Enabled")
                    return this.ActivityReward_Enabled;
                else if (itemName == "WelcomeMoney_Enabled")
                    return this.WelcomeMoney_Enabled;
                else if (itemName == "WeaponMultiplier_Enabled")
                    return this.WeaponMultiplier_Enabled;
                else if (itemName == "DistanceMultiplier_Enabled")
                    return this.DistanceMultiplier_Enabled;
                else if (itemName == "UseEconomicsPlugin")
                    return this.UseEconomicsPlugin;
                else if (itemName == "UseServerRewardsPlugin")
                    return this.UseServerRewardsPlugin;
                else if (itemName == "UseFriendsPlugin")
                    return this.UseFriendsPlugin;
                else if (itemName == "UseClansPlugin")
                    return this.UseClansPlugin;
                else if (itemName == "Economincs_TakeMoneyFromVictim")
                    return this.Economincs_TakeMoneyFromVictim;
                else if (itemName == "ServerRewards_TakeMoneyFromVictim")
                    return this.ServerRewards_TakeMoneyFromVictim;
                else if (itemName == "PrintToConsole")
                    return this.PrintToConsole;
                else if (itemName == "HappyHour_Enabled")
                    return this.HappyHour_Enabled;
                else if (itemName == "VIPMultiplier_Enabled")
                    return this.VIPMultiplier_Enabled;
                else if (itemName == "NPCReward_Enabled")
                    return this.NPCReward_Enabled;
                else
                    return false;
            }          
        }
        class Rewards_Version
        {
            public string Version { get; set; }
        }
        //class Strings
        //{
        //    public string CustomPermissionName { get; set; }
        //    public string GetItemByString(string itemName)
        //    {
        //        if (itemName == "CustomPermissionName")
        //            return this.CustomPermissionName;
        //        else
        //            return null;
        //    }
        //}
    }
}