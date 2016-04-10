using System;
using System.Collections.Generic;
using System.Reflection;
using Rust;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Godmode", "Mughisi", "2.0.2", ResourceId = 673)]
    [Description("Godmode for server admins")]

    class Godmode : RustPlugin
    {
        #region Configuration Data

        // Do not modify these values, for modifications to the configuration
        // file you should modify 'God.json' in your server's config folder.
        // <drive>:\...\server\<server identity>\oxide\config\

        bool configChanged = false;

        // Plugin settings
        string defaultChatPrefix = "God";
        string defaultChatPrefixColor = "008800";
        string defaultPlayerPrefix = "[God]";

        string chatPrefix;
        string chatPrefixColor;
        string playerPrefix;

        // Plugin options
        bool defaultPlayerPrefixEnabled = true;
        bool defaultInformOnAttack = true;
        bool defaultGodDamageEnabled = false;

        bool playerPrefixEnabled;
        bool informOnAttack;
        bool godDamageEnabled;

        // Plugin messages
        string defaultEnabled = "You have enabled godmode.";
        string defaultDisabled = "You have disabled godmode.";
        string defaultEnablePlayer = "You have enabled godmode for {0}.";
        string defaultDisablePlayer = "You have disabled godmode for {0}.";
        string defaultEnabledPlayer = "Your godmode has been enabled by {0}.";
        string defaultDisabledPlayer = "Your godmode has been disabled by {0}.";
        string defaultNotAllowed = "You are not allowed to use this command.";
        string defaultInformAttacker = "{0} is currently in godmode and can't take any damage.";
        string defaultInformVictim = "{0} just tried to deal damage to you.";
        string defaultGodList = "The following players currently have godmode enabled:";
        string defaultPlayerNotFound = "No players were found with that name.";
        string defaultMultiplePlayersFound = "Multiple players were found with that name, select one of these players by using /god list <number>:";
        string defaultInvalidSelection = "Invalid number, use the number in front of the player's name. Use /viewinv list to check the list again";
        string defaultNoListAvailable = "You don't have a player list available, use '/god <name>' instead.";
        string defaultNoGodLoot = "You are not allowed to loot a player that is in Godmode.";

        string enabled;
        string disabled;
        string enablePlayer;
        string disablePlayer;
        string enabledPlayer;
        string disabledPlayer;
        string notAllowed;
        string informAttacker;
        string informVictim;
        string godList;
        string playerNotFound;
        string multiplePlayersFound;
        string invalidSelection;
        string noListAvailable;
        string noGodLoot;

        #endregion

        class StoredData
        {
            public HashSet<PlayerInfo> Gods = new HashSet<PlayerInfo>();

            public StoredData()
            {
            }
        }

        class PlayerInfo
        {
            public string UserId;
            public string Name;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer player)
            {
                UserId = player.userID.ToString();
                Name = player.displayName;
            }

            public ulong GetUserId()
            {
                ulong user_id;
                if (!ulong.TryParse(UserId, out user_id)) return 0;
                return user_id;
            }
        }

        StoredData storedData;

        Hash<ulong, PlayerInfo> gods = new Hash<ulong, PlayerInfo>();

        Dictionary<BasePlayer, long> playerInformHistory = new Dictionary<BasePlayer, long>();

        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        Hash<BasePlayer, List<BasePlayer>> matches = new Hash<BasePlayer, List<BasePlayer>>();

        FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic));

        void Init() => PluginSetup();

        protected override void LoadDefaultConfig() => Config.Clear();

        void Unload() => SaveData();

        void OnServerSave() => SaveData();

        void OnPlayerInit(BasePlayer player)
        {
            if (gods[player.userID] != null)
            {
                ModifyMetabolism(player, true);
                if (playerPrefixEnabled)
                   displayName.SetValue(player, $"{playerPrefix} {player.displayName}");
            }
        }

        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            var attacker = info.Initiator as BasePlayer;

            if (!player) return;

            if (gods[player.userID] != null)
            {
                NullifyDamage(ref info);

                if (informOnAttack)
                    InformPlayers(player, attacker);
            }

            if (!attacker) return;
            if (gods[attacker.userID] != null && !godDamageEnabled)
                NullifyDamage(ref info);
        }

        [ChatCommand("God")]
        void God(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAllowed(player, "godmode.allowed")) return;

            if (args.Length == 0)
            {
                if (gods[player.userID] != null)
                {
                    DisableGodmode(player);
                    SendChatMessage(player, disabled);
                }
                else
                {
                    EnableGodmode(player);
                    SendChatMessage(player, enabled);
                }

                return;
            }

            var name = args[0];
            if (name == "list")
            {
                if (matches[player] == null)
                {
                    SendChatMessage(player, noListAvailable);
                    return;
                }
                if (args.Length == 1)
                {
                    ShowMatchingPlayers(player);
                    return;
                }
                int index;
                if (!int.TryParse(args[1], out index))
                {
                    SendChatMessage(player, invalidSelection);
                    return;
                }

                if (index > matches[player].Count)
                    SendChatMessage(player, invalidSelection);
                else
                    ToggleGodmode(player, matches[player][index - 1]);

                return;
            }

            var playerMatches = FindPlayersByName(name);
            if (playerMatches.Count < 1)
            {
                SendChatMessage(player, playerNotFound);
                return;
            }
            if (playerMatches.Count == 1)
            {
                ToggleGodmode(player, playerMatches[0]);
                return;
            }
            if (playerMatches.Count > 1)
            {
                matches.Add(player, playerMatches);
                ShowMatchingPlayers(player);
                return;
            }
        }

        [ChatCommand("Godlist")]
        void Godlist(BasePlayer player, string cmd, string[] args)
        {
            if (!IsAllowed(player, "godmode.allowed")) return;

            SendChatMessage(player, godList);
            if (gods.Count == 0)
                SendChatMessage(player, "None");
            else
            {
                foreach (var god in gods)
                    SendChatMessage(player, $"{god.Value.Name} [{god.Value.UserId}]");
            }
        }

        void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            if (!target) return;

            if (gods[target.userID] != null)
            {
                NextTick(() =>
                {
                    player.EndLooting();
                    SendChatMessage(player, noGodLoot);
                });
            }
        }

        #region Helpers

        void Log(string msg) => Puts($"{msg}");

        void Warning(string msg) => PrintWarning($"{msg}");

        long GetTimestamp() => Convert.ToInt64((DateTime.UtcNow.Subtract(epoch)).TotalSeconds);

        void DisableGodmode(BasePlayer player)
        {
            storedData.Gods.RemoveWhere(info => info.GetUserId() == player.userID);
            gods.Remove(player.userID);
            ModifyMetabolism(player, false);
            if (playerPrefixEnabled)
                displayName.SetValue(player, player.displayName.Replace(playerPrefix, "").Trim());
        }

        void EnableGodmode(BasePlayer player)
        {
            var info = new PlayerInfo(player);
            storedData.Gods.Add(info);
            gods[player.userID] = info;
            ModifyMetabolism(player, true);
            if (playerPrefixEnabled)
                displayName.SetValue(player, $"{playerPrefix} {player.displayName}");
        }

        void ModifyMetabolism(BasePlayer player, bool isGod)
        {
            if (isGod)
            {
                player.health = 100;
                player.metabolism.bleeding.max = 0;
                player.metabolism.bleeding.value = 0;
                player.metabolism.calories.min = 1000;
                player.metabolism.calories.value = 1000;
                player.metabolism.dirtyness.max = 0;
                player.metabolism.dirtyness.value = 0;
                player.metabolism.heartrate.min = 0.5f;
                player.metabolism.heartrate.max = 0.5f;
                player.metabolism.heartrate.value = 0.5f;
                player.metabolism.hydration.min = 1000;
                player.metabolism.hydration.value = 1000;
                player.metabolism.oxygen.min = 1;
                player.metabolism.oxygen.value = 1;
                player.metabolism.poison.max = 0;
                player.metabolism.poison.value = 0;
                player.metabolism.radiation_level.max = 0;
                player.metabolism.radiation_level.value = 0;
                player.metabolism.radiation_poison.max = 0;
                player.metabolism.radiation_poison.value = 0;
                player.metabolism.temperature.min = 32;
                player.metabolism.temperature.max = 32;
                player.metabolism.temperature.value = 32;
                player.metabolism.wetness.max = 0;
                player.metabolism.wetness.value = 0;
            }
            else
            {
                player.metabolism.bleeding.min = 0;
                player.metabolism.bleeding.max = 1;
                player.metabolism.calories.min = 0;
                player.metabolism.calories.max = 1000;
                player.metabolism.comfort.min = 0;
                player.metabolism.comfort.max = 1;
                player.metabolism.dirtyness.min = 0;
                player.metabolism.dirtyness.max = 100;
                player.metabolism.heartrate.min = 0;
                player.metabolism.heartrate.max = 1;
                player.metabolism.hydration.min = 0;
                player.metabolism.hydration.max = 1000;
                player.metabolism.oxygen.min = 0;
                player.metabolism.oxygen.max = 1;
                player.metabolism.poison.min = 0;
                player.metabolism.poison.max = 100;
                player.metabolism.radiation_level.min = 0;
                player.metabolism.radiation_level.max = 100;
                player.metabolism.radiation_poison.min = 0;
                player.metabolism.radiation_poison.max = 500;
                player.metabolism.temperature.min = -100;
                player.metabolism.temperature.max = 100;
                player.metabolism.wetness.min = 0;
                player.metabolism.wetness.max = 1;
            }
            player.metabolism.SendChangesToClient();
        }

        void InformPlayers(BasePlayer victim, BasePlayer attacker)
        {
            if (!victim || !attacker) return;
            if (victim == attacker) return;

            if (!playerInformHistory.ContainsKey(attacker)) playerInformHistory.Add(attacker, 0);
            if (!playerInformHistory.ContainsKey(victim)) playerInformHistory.Add(victim, 0);

            if (GetTimestamp() - playerInformHistory[victim] > 15)
            {
                SendChatMessage(victim, informVictim, attacker.displayName);
                playerInformHistory[victim] = GetTimestamp();
            }

            if (GetTimestamp() - playerInformHistory[victim] > 15)
            {
                SendChatMessage(attacker, informAttacker, victim.displayName);
                playerInformHistory[victim] = GetTimestamp();
            }
        }

        static void NullifyDamage(ref HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        List<BasePlayer> FindPlayersByName(string name)
        {
            List<BasePlayer> playerMatches = new List<BasePlayer>();

            foreach (var ply in BasePlayer.activePlayerList)
            {
                if (ply.displayName.ToLower().Contains(name.ToLower()))
                    playerMatches.Add(ply);
            }

            foreach (var ply in BasePlayer.sleepingPlayerList)
            {
                if (ply.displayName.ToLower().Contains(name.ToLower()))
                    playerMatches.Add(ply);
            }

            return playerMatches;
        }

        void ShowMatchingPlayers(BasePlayer player)
        {
            int i = 0;
            SendChatMessage(player, multiplePlayersFound);
            foreach (var ply in matches[player])
            {
                i++;
                SendChatMessage(player, $"{i} - {ply.displayName} ({ply.userID})");
            }
        }

        void ToggleGodmode(BasePlayer toggler, BasePlayer target)
        {
            if (gods[target.userID] != null)
            {
                DisableGodmode(target);
                SendChatMessage(target, disabledPlayer, toggler.displayName);
                SendChatMessage(toggler, disablePlayer, target.displayName);
            }
            else
            {
                SendChatMessage(target, enabledPlayer, toggler.displayName);
                SendChatMessage(toggler, enablePlayer, target.displayName);
                EnableGodmode(target);
            }
        }

        void SendChatMessage(BasePlayer player, string message, string args = null) => PrintToChat(player, $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}", args);

        void PluginSetup()
        {
            LoadConfigData();
            LoadSavedData();
            permission.RegisterPermission("godmode.allowed", this);
        }

        void LoadConfigData()
        {
            // Plugin settings
            chatPrefix = GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix);
            chatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor);
            playerPrefix = GetConfigValue("Settings", "PlayerPrefix", defaultPlayerPrefix);

            // Plugin options
            playerPrefixEnabled = GetConfigValue("Options", "PlayerPrefixEnabled", defaultPlayerPrefixEnabled);
            informOnAttack = GetConfigValue("Options", "InformPlayersOnAttack", defaultInformOnAttack);
            godDamageEnabled = GetConfigValue("Options", "GodDamageEnabled", defaultGodDamageEnabled);

            // Plugin messages
            enabled = GetConfigValue("Messages", "Enabled", defaultEnabled);
            disabled = GetConfigValue("Messages", "Disabled", defaultDisabled);
            enablePlayer = GetConfigValue("Messages", "EnablePlayer", defaultEnablePlayer);
            disablePlayer = GetConfigValue("Messages", "DisablePlayer", defaultDisablePlayer);
            enabledPlayer = GetConfigValue("Messages", "EnabledPlayer", defaultEnabledPlayer);
            disabledPlayer = GetConfigValue("Messages", "DisabledPlayer", defaultDisabledPlayer);
            notAllowed = GetConfigValue("Messages", "NotAllowed", defaultNotAllowed);
            informAttacker = GetConfigValue("Messages", "InformAttacker", defaultInformAttacker);
            informVictim = GetConfigValue("Messages", "InformVictim", defaultInformVictim);
            godList = GetConfigValue("Messages", "GodList", defaultGodList);
            playerNotFound = GetConfigValue("Messages", "PlayerNotFound", defaultPlayerNotFound);
            multiplePlayersFound = GetConfigValue("Messages", "MultiplePlayersFound", defaultMultiplePlayersFound);
            invalidSelection = GetConfigValue("Messages", "InvalidPlayerSelected", defaultInvalidSelection);
            noListAvailable = GetConfigValue("Messages", "NoListAvailable", defaultNoListAvailable);
            noGodLoot = GetConfigValue("Messages", "NoGodLoot", defaultNoGodLoot);

            if (configChanged)
            {
                Warning("The configuration file was updated!");
                SaveConfig();
            }
        }

        void LoadSavedData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("Godmode");
            foreach (var god in storedData.Gods)
                gods[god.GetUserId()] = god;
        }

        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("Godmode", storedData);

        bool IsAllowed(BasePlayer player, string perm)
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            SendChatMessage(player, notAllowed);
            return false;
        }

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (!data.TryGetValue(setting, out value))
            {
                value = defaultValue;
                data[setting] = value;
                configChanged = true;
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion
    }
}
