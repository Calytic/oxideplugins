using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Rewards", "Tarek", "1.2.7", ResourceId = 1961)]
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

        private bool IsFriendsLoaded = false;
        private bool IsEconomicsLoaded = false;
        private bool IsServerRewardsLoaded = false;
        private bool IsClansLoaded = false;

        StoredData storedData;

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
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            var rr = new RewardRates
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
                WelcomeMoney = 250
            };
            var m = new Multipliers
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
                distance_400 = 3
            };

            var o = new Options
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
                PrintToConsole = true
            };
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
                ["helicopter"] = "a helicopter"
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
        void Loadcfg()
        {
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
            }
            catch
            {
                Config.Clear(); LoadDefaultConfig(); Loadcfg();
            }
        }
        void Init()
        {
            permission.RegisterPermission("rewards.admin", this);
            LoadDefaultMessages();
            Loadcfg();
            #region Activity Check
            if (options.ActivityReward_Enabled)
            {
                timer.Repeat(60, 0, () =>
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
                });
            }
            #endregion
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
        void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            
            if (victim == null)
                return;
            if (info?.Initiator?.ToPlayer() == null)
                return;
            double totalmultiplier = 1;

            if (options.DistanceMultiplier_Enabled || options.WeaponMultiplier_Enabled)
                totalmultiplier = (options.DistanceMultiplier_Enabled ? multipliers.GetDistanceM(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) * (options.WeaponMultiplier_Enabled ? multipliers.GetWeaponM(info?.Weapon?.GetItem()?.info?.displayName?.english) : 1);
            if (victim.ToPlayer() != null)
            {
                if (info?.Initiator?.ToPlayer().userID == victim.ToPlayer().userID)
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
            else if (victim.name.Contains("patrolhelicopter.prefab") && victim.name.Contains("gibs"))
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
                    SendChatMessage(player, "Rewards", reason == null ? Lang("ActivityReward", player.UserIDString, amount) : Lang("KillReward", player.UserIDString, amount, reason));
                    ConVar.Server.Log("/oxide/logs/RewardsLog.txt", player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
                    if (options.PrintToConsole)
                        Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
                }
                else
                {
                    SendChatMessage(player, "Rewards", Lang("WelcomeReward", player.UserIDString, amount));
                    ConVar.Server.Log("/oxide/logs/RewardsLog.txt", player.displayName + " got " + amount + " as a welcome reward");
                    if (options.PrintToConsole)
                        Puts(player.displayName + " got " + amount + " as a welcome reward");
                }
            }
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
                                SendChatMessage(player, "Rewards", Lang("VictimNoMoney", player.UserIDString, victim.displayName));
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
                        SendChatMessage(player, "Rewards", Lang("KillReward", player.UserIDString, rewardrates.human * multiplier, victim.displayName));
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
                    Config["Rewards", args[0]] = args[1];
                    SaveConfig();
                    try
                    {
                        Loadcfg();
                    }
                    catch
                    {
                        Config.Clear();
                        LoadDefaultConfig();
                        Loadcfg();
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
                    Config["Rewards", args[0]] = args[1];
                    SaveConfig();
                    try
                    {
                        Loadcfg();
                    }
                    catch
                    {
                        Config.Clear();
                        LoadDefaultConfig();
                        Loadcfg();
                    }
                    SendChatMessage(player, "Rewards", Lang("RewardSet", player.UserIDString));
                }
                catch { SendChatMessage(player, "Rewards", Lang("SetRewards", player.UserIDString) + " 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'autoturret', 'ActivityReward', 'ActivityRewardRate_minutes', 'WelcomeMoney'"); }
            }
        }
        [ChatCommand("showrewards")]
        private void showrewardsCommand(BasePlayer player, string command, string[] args)
        {
            if (HasPerm(player, "rewards.admin"))
                SendChatMessage(player, "Rewards", String.Format("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, autoturret = {8} Activity Reward Rate (minutes) = {9}, Activity Reward = {10}, WelcomeMoney = {11}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney));
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

        }
        class Options
        {
            public bool ActivityReward_Enabled { get; set; }
            public bool WelcomeMoney_Enabled { get; set; }
            public bool WeaponMultiplier_Enabled { get; set; }
            public bool DistanceMultiplier_Enabled { get; set; }
            public bool UseEconomicsPlugin { get; set; }
            public bool UseServerRewardsPlugin { get; set; }
            public bool UseFriendsPlugin { get; set; }
            public bool UseClansPlugin { get; set; }
            public bool Economincs_TakeMoneyFromVictim { get; set; }
            public bool ServerRewards_TakeMoneyFromVictim { get; set; }
            public bool PrintToConsole { get; set; }
        }
    }
}