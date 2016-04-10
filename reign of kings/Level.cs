using System;
using System.Linq;
using System.Collections.Generic;
using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Level", "D-Kay", "0.3")]
    public class Level : ReignOfKingsPlugin 
	{
		#region Variables
	
		private bool _allowPvpXp = true; // Turns on/off gold for PVP
		private bool _allowPveXp = true; // Turns on/off gold for PVE
        // PvE settings: 
		private int monsterKillMinXp => GetConfig("monsterKillMinXp", 5); // Minimum amount of xp a player can get for killing a monster.
        private int monsterKillMaxXp => GetConfig("monsterKillMaxXp", 15); // Maximum amount of xp a player can get for killing a monster.
        private int animalKillMinXp => GetConfig("animalKillMinXp", 1); // Minimum amount of xp a player can get for killing an animal.
        private int animalKillMaxXp => GetConfig("animalKillMaxXp", 6); // Maximum amount of xp a player can get for killing an animal.
        // PvP settings:
        private int pvpGetMinXp => GetConfig("pvpGetMinXp", 0); // Minimum amount of xp a player can get for killing a player.
        private int pvpGetMaxXp => GetConfig("pvpGetMaxXp", 15); // Maximum amount of xp a player can get for killing a player.
        private int pvpLoseMinXp => GetConfig("pvpLoseMinXp", 3); // Minimum amount of xp a player can lose for getting killed by a player.
        private int pvpLoseMaxXp => GetConfig("pvpLoseMaxXp", 7); // Maximum amount of xp a player can lose for getting killed by a player.
        private double pvpXpLoss => GetConfig("pvpXpLoss", 20); // Amount of xp you get less for each level difference as percentage.
        // Damage bonus settings:
        private double playerDamageBonusPercentage => GetConfig("playerDamageBonusPercentage", 2); // Damagebonus when hitting a player as percentage for each level gained.
        private double monsterDamageBonusPercentage => GetConfig("monsterDamageBonusPercentage", 5); // Damagebonus when hitting a monster as percentage for each level gained.
        private double siegeDamageBonusPercentage => GetConfig("siegeDamageBonusPercentage", 5); // Damagebonus when using siege weapons as percentage for each level gained.
        private double blockDamageBonusPercentage => GetConfig("blockDamageBonusPercentage", 5); // Damagebonus when hitting a block without siegeweapons as percentage for each level gained.
        // Top level settings:
        private int maxTopPlayersList => GetConfig("maxTopPlayersList", 10); // Number of players in the top list.
        // xp for each level:
        private List<object> XpValues => GetConfig("xpNeededPerLevel", defaultXpValues);
        private List<object> defaultXpValues = new List<object>()
        {
            0,
            50,
            100,
            200,
            400,
            600,
            900,
            1200,
            1500,
            1800, //lvl 10
            2100,
            2400,
            2700,
            3000,
            3300,
            3600,
            3900,
            4200,
            4500,
            4700, //lvl 20
            5300,
            5900,
            6500,
            7100,
            7700,
            8300,
            8900,
            9500,
            10100,
            10700, //30
            11900,
            13100,
            14300,
            15500,
            16700,
            17900,
            19100,
            20300,
            21500,
            22700 //40  // Maximum xp may not be above 2100000000. - 32-bit INTEGER FLOOD WARNING
        };

        void Log(string msg) => Puts($"{Title} : {msg}");

        private Dictionary<string, int> _playerXp = new Dictionary<string, int>();

        private readonly System.Random _random = new System.Random();

        private int MaxPossibleXp;

        #endregion

        #region Save and Load Data Methods

        // SAVE DATA ===============================================================================================
        private void LoadXpData()
		{
            _playerXp = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, int>>("SavedplayerXp");
            _allowPveXp = Interface.GetMod().DataFileSystem.ReadObject<bool>("SavedPvEXpStatus");
            _allowPvpXp = Interface.GetMod().DataFileSystem.ReadObject<bool>("SavedPvPXpStatus");
		}
	
		private void SaveXpData()
		{
            Interface.GetMod().DataFileSystem.WriteObject("SavedplayerXp", _playerXp); // _playerWallet
            Interface.GetMod().DataFileSystem.WriteObject("SavedPvEXpStatus", _allowPveXp);
            Interface.GetMod().DataFileSystem.WriteObject("SavedPvPXpStatus", _allowPvpXp);
		}
		
		private void OnPlayerConnected(Player player)
		{
            CheckPlayerExcists(player);
        }

        void Loaded()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            LoadXpData();

            permission.RegisterPermission("Level.Toggle", this);
            permission.RegisterPermission("Level.ModifyXp", this);

            MaxPossibleXp = Convert.ToInt32(XpValues[(XpValues.Count() - 1)]);
        }

        protected override void LoadDefaultConfig()
        {
            Config["monsterKillMinXp"] = monsterKillMinXp;
            Config["monsterKillMaxXp"] = monsterKillMaxXp;
            Config["animalKillMinXp"] = animalKillMinXp;
            Config["animalKillMaxXp"] = animalKillMaxXp;
            Config["pvpGetMinXp"] = pvpGetMinXp;
            Config["pvpGetMaxXp"] = pvpGetMaxXp;
            Config["pvpLoseMinXp"] = pvpLoseMinXp;
            Config["pvpLoseMaxXp"] = pvpLoseMaxXp;
            Config["pvpXpLoss"] = pvpXpLoss;
            Config["playerDamageBonusPercentage"] = playerDamageBonusPercentage;
            Config["monsterDamageBonusPercentage"] = monsterDamageBonusPercentage;
            Config["siegeDamageBonusPercentage"] = siegeDamageBonusPercentage;
            Config["blockDamageBonusPercentage"] = blockDamageBonusPercentage;
            Config["maxTopPlayersList"] = maxTopPlayersList;
            Config["xpNeededPerLevel"] = XpValues;
            SaveConfig();
        }

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerNotOnline", "That player does not appear to be online right now." },
                { "NoValidNumber", "That is not a valid number." },
                { "XpDataDeleted", "All xp data was deleted." },
                { "PvpXpOn", "PvP xp was turned on." },
                { "PvpXpOff", "PvP xp was turned off." },
                { "PveXpOn", "PvE xp was turned on." },
                { "PveXpOff", "PvE xp was turned off." },
                { "KilledGuildMember", "You won't gain any xp by killing a member of your own guild!" },
                { "GiveXp", "{0} got [00FF00]{1}[FFFF00]xp[FFFFFF]." },
                { "RemoveXp", "{0} lost [00FF00]{1}[FFFF00]xp[FFFFFF]." },
                { "PlayerLevelList", "[00ff00]{0}[ffffff] is level [00ff00]{1}[ffffff]" },
                { "TopPlayersList",   "{0}. {1} [FFFF00](level [00ff00]{2}[FFFF00])[ffffff]." },
                { "CurrentXp", "You currently have [00FF00]{0}[FFFF00]xp[FFFFFF]." },
                { "CurrentLevel", "Your current level is [00FF00]{0}[FFFFFF]." },
                { "NeededXp", "You need [00FF00]{0}[FFFF00]xp[FFFFFF] more to reach the next level." },
                { "HighestLevel", "You have reached the highest level possible." },
                { "GotMaxXp", "You cannot gain any more xp than you now have. Congratulations." },
                { "CollectedXp", "[00FF00]{0}[FFFF00] xp[FFFFFF] collected." },
                { "LostXp", "[00FF00]{0}[FFFF00] xp[FFFFFF] lost." },
                { "LevelUp", "Concratulations! You reached level [00FF00]{0}[FFFFFF]!" },
                { "LevelDown", "Concratulations! You reached level [00FF00]{0}[FFFFFF]!" }
            }, this);
        }

        #endregion

        #region User Commands

        // Check my Xp
        [ChatCommand("xp")]
        private void HowMuchXpAPlayerhas(Player player, string cmd)
        {
            HowMuchXpICurrentlyHave(player, cmd);
        }

        [ChatCommand("givexp")]
        private void GivePlayerXp(Player player, string cmd, string[] input)
        {
            Player target;
            if (!player.HasPermission("Level.ModifyXp")) return;
            if (input.Length < 2)
            {
                target = player;
            }
            else
            {
                target = Server.GetPlayerByName(input[1]);
                if (target == null)
                {
                    PrintToChat(player, GetMessage("PlayerNotOnline", player.Id.ToString()));
                    return;
                }
            }
            int amountToGive;
            if (Int32.TryParse(input[0], out amountToGive))
            {
                GiveXp(target, Convert.ToInt32(input[0]));
                PrintToChat(player, String.Format(GetMessage("GiveXp", player.Id.ToString()), target.Name, amountToGive));
            }
            else
            {
                PrintToChat(player, GetMessage("NoValidNumber", player.Id.ToString()));
            }
        }

        [ChatCommand("removexp")]
        private void RemovePlayerXp(Player player, string cmd, string[] input)
        {
            Player target;
            if (!player.HasPermission("Level.ModifyXp")) return;
            if (input.Length < 2)
            {
                target = player;
            }
            else
            {
                target = Server.GetPlayerByName(input[1]);
                if (target == null)
                {
                    PrintToChat(player, GetMessage("PlayerNotOnline", player.Id.ToString()));
                    return;
                }
            }
            int amountToGive;
            if (Int32.TryParse(input[0], out amountToGive))
            {
                RemoveXp(target, Convert.ToInt32(input[0]));
                PrintToChat(player, String.Format(GetMessage("RemoveXp", player.Id.ToString()), target.Name, amountToGive));
            }
            else
            {
                PrintToChat(player, GetMessage("NoValidNumber", player.Id.ToString()));
            }
        }

        [ChatCommand("clearxp")]
        private void ClearPlayerXp(Player player, string cmd)
        {
            if (!player.HasPermission("Level.ModifyXp")) return;
            _playerXp = new Dictionary<string, int>();
            SaveXpData();
            PrintToChat(player, GetMessage("XpDataDeleted", player.Id.ToString()));
        }

        [ChatCommand("levellist")]
        private void ShowOnlinePlayersLevel(Player player, string cmd)
        {
            CheckPlayerExcists(player);

            List<Player> onlineplayers = Server.ClientPlayers as List<Player>;
            foreach (Player oPlayer in onlineplayers.ToArray())
            {
                if (_playerXp.ContainsKey(oPlayer.ToString())) PrintToChat(player, String.Format(GetMessage("PlayerLevelList", player.Id.ToString()), oPlayer.Name, GetCurrentLevel(oPlayer)));
            }
        }

        [ChatCommand("topplayers")]
        private void ShowTopPlayers(Player player, string cmd)
        {
            CheckPlayerExcists(player);

            Dictionary<string, int> TopPlayers = new Dictionary<string, int>(_playerXp);
            int topList = maxTopPlayersList;
            if (TopPlayers.Keys.Count() < maxTopPlayersList) topList = TopPlayers.Keys.Count();
            for (int i = 1; i <= topList; i++)
            {
                string TopPlayer = "";
                int TopXpAmount = 0;
                foreach (string Name in TopPlayers.Keys)
                {
                    if (TopPlayers[Name] >= TopXpAmount)
                    {
                        TopPlayer = Name;
                        TopXpAmount = TopPlayers[Name];
                    }
                }
                int level = GetCurrentLevel(TopPlayer);
                string TPlayer = TopPlayer.Substring(0, TopPlayer.IndexOf("(") - 1);
                PrintToChat(player, String.Format(GetMessage("TopPlayersList", player.Id.ToString()), i.ToString(), TPlayer, level));
                TopPlayers.Remove(TopPlayer);
            }
        }

        [ChatCommand("xppvp")]
        private void togglePvpXp(Player player, string cmd)
        {
            if (!player.HasPermission("Level.Toggle")) return;
            if (_allowPvpXp) { _allowPvpXp = false; PrintToChat(player, GetMessage("PvpXpOff", player.Id.ToString())); }
            else { _allowPvpXp = true; PrintToChat(player, GetMessage("PvpXpOn", player.Id.ToString())); }
            SaveXpData();
        }

        [ChatCommand("xppve")]
        private void togglePveXp(Player player, string cmd)
        {
            if (!player.HasPermission("Level.Toggle")) return;
            if (_allowPveXp) { _allowPveXp = false; PrintToChat(player, GetMessage("PveXpOff", player.Id.ToString())); }
            else { _allowPveXp = true; PrintToChat(player, GetMessage("PveXpOn", player.Id.ToString())); }
            SaveXpData();
        }

        private void SendHelpText(Player player)
        {
            PrintToChat(player, "[0000FF]Level Commands[FFFFFF]");
            PrintToChat(player, "[00FF00]/xp[FFFFFF] - Shows your current amount of xp, your current level and how much xp you need to reach the next level.");
            PrintToChat(player, "[00FF00]/levellist[FFFFFF] - Shows a list with the levels of all online players.");
            PrintToChat(player, "[00FF00]/topplayers[FFFFFF] - Shows a list of the players with the highest level.");
            if (player.HasPermission("admin"))
            {

                PrintToChat(player, "[00FF00]/givexp (amount) (optional: player)[FFFFFF] - Gives amount of xp (optional: to specified player).");
                PrintToChat(player, "[00FF00]/removexp (amount) (optional: player)[FFFFFF] - Removes amount of xp (optional: from specified player).");
                PrintToChat(player, "[00FF00]/clearxp[FFFFFF] - Removes all xp values from al players.");
                PrintToChat(player, "[00FF00]/xppvp[FFFFFF] - Toggle the allowance of xp gathering by pvp battles.");
                PrintToChat(player, "[00FF00]/xppve[FFFFFF] - Toggle the allowance of xp gathering by pve battles.");
            }
        }

        #endregion

        #region functions

        private int GetCurrentLevel(Player player)
        {
            var XpLNow = _playerXp[player.ToString()];
            int level = 0;

            while ((level < XpValues.Count()) && (XpLNow >= Convert.ToInt32(XpValues[level]))) ++level;

            return level;
        }

        private int GetCurrentLevel(string player)
        {
            var XpLNow = _playerXp[player];
            int level = 0;

            while ((level < XpValues.Count()) && (XpLNow >= Convert.ToInt32(XpValues[level]))) ++level;

            return level;
        }

        private void HowMuchXpICurrentlyHave(Player player, string cmd)
        {
            CheckPlayerExcists(player);

            var XpAmount = _playerXp[player.ToString()];
			PrintToChat(player, String.Format(GetMessage("CurrentXp", player.Id.ToString()), XpAmount));
            int level = GetCurrentLevel(player);
            PrintToChat(player, String.Format(GetMessage("CurrentLevel", player.Id.ToString()), level));

            if (XpValues.Count() != level)
            {
                int NextLevelXp = Convert.ToInt32(XpValues[level]);
                int NeededXp = NextLevelXp - XpAmount;
                PrintToChat(player, String.Format(GetMessage("NeededXp", player.Id.ToString()), NeededXp));
            }
            else
            {
                PrintToChat(player, GetMessage("HighestLevel", player.Id.ToString()));
            }

        }

		
		private void GiveXp(Player player, int amount)
        {
            CheckPlayerExcists(player);

            var XpNow = _playerXp[player.ToString()];
            if (XpNow + amount > MaxPossibleXp)
            {
                PrintToChat(player, GetMessage("GotMaxXp", player.Id.ToString()));
                XpNow = MaxPossibleXp;
            }
            else XpNow = XpNow + amount;
			
            _playerXp[player.ToString()] = XpNow;
			SaveXpData();
        }
		
		private void RemoveXp(Player player, int amount)
        {
            CheckPlayerExcists(player);

            var XpNow = _playerXp[player.ToString()];
            XpNow = XpNow - amount;
            if (XpNow < 0) XpNow = 0;

            _playerXp[player.ToString()] = XpNow;
            SaveXpData();
        }

        private void CheckPlayerExcists(Player returner)
        {
            bool contained = false;
            string player = returner.ToString();

            if (_playerXp.ContainsKey(player)) contained = true;
            if (!contained)
            {
                string playerId = player.Substring(player.IndexOf("7"), 17);

                foreach (var playerNameId in _playerXp.Keys)
                {
                    int xp = 0;
                    if (playerNameId.Contains(playerId))
                    {
                        xp = _playerXp[playerNameId];
                        _playerXp.Add(player, xp);
                        _playerXp.Remove(playerNameId);
                        contained = true;
                        break;
                    }
                }
            }
            if (!contained) _playerXp.Add(player, 0);

            SaveXpData();
        }

        #endregion

        #region Player, Creature and Object Damage

        // Give credits when a player is killed
        private void OnEntityDeath(EntityDeathEvent deathEvent)
		{
		    if (deathEvent.Entity == null) return;
			if (deathEvent.Entity.Owner == null) return;
			if (deathEvent.Entity.Owner.Name == "server") return;

            
            if (deathEvent.KillingDamage.DamageSource.IsPlayer)
            {
                if (deathEvent.KillingDamage == null)
                {
                    Log("deathEvent.KillingDamage was null here!");
                    return;
                }
                if (deathEvent.KillingDamage.DamageSource == null)
                {
                    Log("deathEvent.KillingDamage.DamageSource was null here!");
                    return;
                }
                if (deathEvent.KillingDamage.DamageSource.Owner == null)
                {
                    Log("deathEvent.Damage.DamageSource.Owner was null here!");
                    return;
                }

                var player = deathEvent.KillingDamage.DamageSource.Owner;
                if (player == null)
                {
                    Log("player variable was null here!");
                    return;
                }

                var entity = deathEvent.Entity;
                if (entity == null)
                {
                    Log("entity variable was null here!");
                    return;
                }

                if (!deathEvent.Entity.IsPlayer)
                {
                    if (_allowPveXp)
                    {
                        Health h = entity.TryGet<Health>();
                        if (h.ToString().Contains("Trebuchet")) return;
                        if (h.ToString().Contains("Ballista")) return;

                        bool villager = h.ToString().Contains("Plague Villager");
                        bool bear = h.ToString().Contains("Grizzly Bear");
                        bool wolf = h.ToString().Contains("Wolf");
                        bool werewolf = h.ToString().Contains("Werewolf");

                        bool babyChicken = h.ToString().Contains("Baby Chicken");
                        bool bat = h.ToString().Contains("Bat");
                        bool chicken = h.ToString().Contains("Chicken");
                        bool crab = h.ToString().Contains("Crab");
                        bool crow = h.ToString().Contains("Crow");
                        bool deer = h.ToString().Contains("Deer");
                        bool duck = h.ToString().Contains("Duck");
                        bool moose = h.ToString().Contains("Moose");
                        bool pigeon = h.ToString().Contains("Pigeon");
                        bool rabbit = h.ToString().Contains("Rabbit");
                        bool rooster = h.ToString().Contains("Rooster");
                        bool seagull = h.ToString().Contains("Seagull");
                        bool sheep = h.ToString().Contains("Sheep");
                        bool stag = h.ToString().Contains("Stag");
                        int XpAmount = 0;
                        if (villager || bear || wolf || werewolf)
                        {
                            XpAmount = _random.Next(monsterKillMinXp, (monsterKillMaxXp + 1));
                        }
                        if (babyChicken || bat || chicken || crab || crow || deer || duck || moose || pigeon || rabbit || rooster || seagull || sheep || stag)
                        {
                            XpAmount = _random.Next(animalKillMinXp, (animalKillMaxXp + 1));
                        }
                        if (!(villager || bear || wolf || werewolf || babyChicken || bat || chicken || crab || crow || deer || duck || moose || pigeon || rabbit || rooster || seagull || sheep || stag))
                        {
                            return;
                        }
                        int playerLvl = GetCurrentLevel(player);
                        GiveXp(player, XpAmount);
                        if (_playerXp[player.ToString()] < MaxPossibleXp) PrintToChat(player, String.Format(GetMessage("CollectedXp", player.Id.ToString()), XpAmount));
                        if (GetCurrentLevel(player) > playerLvl) PrintToChat(player, String.Format(GetMessage("LevelUp", player.Id.ToString()), GetCurrentLevel(player)));

                    }
                }
                else
                {
                    if (_allowPvpXp)
                    {
                        var victim = deathEvent.Entity.Owner;
                        if (victim == null)
                        {
                            Log("Victim variable was null here!");
                            return;
                        }

                        if (victim.Id == 0 || player.Id == 0)
                        {
                            Log("Victim or Player had no id!");
                            return;
                        }

                        // Make sure player didn't kill themselves
                        if (victim == player) return;

                        if (victim.GetGuild() == null || player.GetGuild() == null)
                        {
                            Log("The victim or the player guild was null here!");
                            return;
                        }

                        // Make sure the player is not in the same guild
                        if (victim.GetGuild().Name == player.GetGuild().Name)
                        {
                            PrintToChat(player, GetMessage("KilledGuildMember", player.Id.ToString()));
                            return;
                        }

                        // Check victims wallet
                        CheckPlayerExcists(victim);
                        // Check the player has a wallet
                        CheckPlayerExcists(player);
                        // Give the rewards to the player
                        int lvlVictim = GetCurrentLevel(victim);
                        int lvlPlayer = GetCurrentLevel(player);
                        int lvlDiff = lvlPlayer - lvlVictim;
                        int xpGain = _random.Next(pvpGetMinXp, (pvpGetMaxXp + 1));
                        int xpLoss = _random.Next(pvpLoseMinXp, (pvpLoseMaxXp + 1));
                        double xpLvlLoss = (100 - (pvpXpLoss * lvlDiff));
                        if (xpLvlLoss < 0) xpLvlLoss = 0;
                        else if (xpLvlLoss > 100) xpLvlLoss = 100;
                        xpLvlLoss = xpLvlLoss / 100;

                        xpGain = Convert.ToInt32(Convert.ToDouble(xpGain) * xpLvlLoss);
                        xpLoss = Convert.ToInt32(Convert.ToDouble(xpLoss) * xpLvlLoss);

                        GiveXp(player, xpGain);
                        RemoveXp(victim, xpLoss);
                        
                        PrintToChat(player, String.Format(GetMessage("CollectedXp", player.Id.ToString()), xpGain));
                        PrintToChat(victim, String.Format(GetMessage("LostXp", player.Id.ToString()), xpLoss));

                        if (GetCurrentLevel(player) > lvlPlayer) PrintToChat(player, String.Format(GetMessage("LevelUp", player.Id.ToString()), GetCurrentLevel(player)));
                        if (GetCurrentLevel(victim) < lvlVictim) PrintToChat(victim, String.Format(GetMessage("LevelDown", player.Id.ToString()), GetCurrentLevel(victim)));
                    }
                }
            }

            //Save the data
            SaveXpData();
        }

        private void OnCubeTakeDamage(CubeDamageEvent damageEvent)
        {
            string damageSource = damageEvent.Damage.Damager.name.ToString();
            if (damageEvent.Damage.DamageSource.Owner is Player)
            {
                Player player = damageEvent.Damage.DamageSource.Owner;
                double CurrentLevel = Convert.ToDouble(GetCurrentLevel(player));
                double damage = Convert.ToDouble(damageEvent.Damage.Amount);
                double damageBonus = 0;

                if (damageSource.Contains("Trebuchet") || damageSource.Contains("Ballista")) damageBonus = siegeDamageBonusPercentage;
                else damageBonus = blockDamageBonusPercentage;

                damageBonus = (CurrentLevel - 1) * damageBonus;
                damage = damage * (damageBonus / 100 + 1);
                damageEvent.Damage.Amount = Convert.ToInt32(damage);
            }
        }

        private void OnEntityHealthChange(EntityDamageEvent damageEvent) 
		{
            if (damageEvent.Damage.DamageSource.Owner is Player)
            {
                Player player = damageEvent.Damage.DamageSource.Owner;
                double CurrentLevel = Convert.ToDouble(GetCurrentLevel(player));
                double damage = Convert.ToDouble(damageEvent.Damage.Amount);
                double damageBonus = 0;

                if (damageEvent.Entity.Owner is Player) damageBonus = playerDamageBonusPercentage;
                else if (damageEvent.Entity.Owner.DisplayName == "server") damageBonus = monsterDamageBonusPercentage;

                damageBonus = (CurrentLevel - 1) * damageBonus;
                damage = damage * (damageBonus / 100 + 1);
                damageEvent.Damage.Amount = Convert.ToInt32(damage);
            }
		}
        #endregion

        #region Helpers
        
        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        #endregion

    }
}