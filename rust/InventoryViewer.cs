using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Inventory Viewer", "Mughisi", "1.0.1")]
    class InventoryViewer : RustPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'InventoryViewer.json' in your server's config folder.
        // <drive>:\...\server\<server identity>\oxide\config\

        bool configChanged;
        bool configCreated;

        // Plugin settings
        string defaultChatPrefix = "Inspector";
        string defaultChatPrefixColor = "#008000ff";

        string chatPrefix;
        string chatPrefixColor;

        // Plugin options
        bool defaultAllowAdmin = true;
        bool defaultAllowModerator = false;

        bool allowAdmin;
        bool allowModerator;

        // Plugin messages
        string defaultNotAllowed = "You are not allowed to use this command.";
        string defaultNoPlayersFound = "Couldn't find any players matching that name.";
        string defaultMultiplePlayersFound = "Multiple players found with that name. Select one of these players by using '/viewinv list <number>':";
        string defaultInvalidSelection = "Invalid number, use the number in front of the player's name. Use /viewinv list to check the list again";
        string defaultInvalidArguments = "Invalid arguments! Use '/viewinv <name>' or '/viewinv list <number>'";
        string defaultNoListAvailable = "You don't have a player list available, use '/viewin <name>' instead.";
        string defaultTargetDied = "The player you were looting died.";

        string notAllowed;
        string noPlayersFound;
        string multiplePlayersFound;
        string invalidSelection;
        string invalidArguments;
        string noListAvailable;
        string targetDied;

        #endregion

        class OnlinePlayer
        {
            public BasePlayer Player;
            public BasePlayer Target;
            public LootableCorpse View;
            public List<BasePlayer> Matches;

            public OnlinePlayer(BasePlayer player)
            {
            }
        }

        [OnlinePlayers]
        Hash<BasePlayer, OnlinePlayer> onlinePlayers = new Hash<BasePlayer, OnlinePlayer>();

        void Loaded() => LoadConfigValues();

        protected override void LoadDefaultConfig()
        {
            configCreated = true;
            Log("New configuration file created.");
        }

        void Unloaded()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (onlinePlayers[player].View != null)
                    CloseInventoryView(player, onlinePlayers[player].View);
            }
        }

        void OnTick()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (onlinePlayers[player].View != null)
                {
                    if (!player.inventory.loot.IsLooting())
                    {
                        CloseInventoryView(player, onlinePlayers[player].View);
                        return;
                    }

                    if (player.inventory.loot.containers[0].playerOwner.IsDead())
                    {
                        SendChatMessage(player, targetDied);
                        CloseInventoryView(player, onlinePlayers[player].View);
                        return;
                    }

                    player.inventory.loot.SendImmediate();
                    onlinePlayers[player].View.ClientRPCPlayer(null, player, "RPC_ClientLootCorpse", new object[0]);
                    player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (onlinePlayers[player].View != null)
                {
                    if (player != entity as BasePlayer && onlinePlayers[player].View != entity as LootableCorpse) return;
                    info.damageTypes = new DamageTypeList();
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                }
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            onlinePlayers[player].View = null;
            onlinePlayers[player].Target = null;
            onlinePlayers[player].Matches = null;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (onlinePlayers[player].View != null)
                CloseInventoryView(player, onlinePlayers[player].View);
        }

        void LoadConfigValues()
        {
            // Plugin settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));

            // Plugin options
            allowAdmin = Convert.ToBoolean(GetConfigValue("Options", "AllowAdmins", defaultAllowAdmin));
            allowModerator = Convert.ToBoolean(GetConfigValue("Options", "AllowModerators", defaultAllowModerator));

            // Plugin messages
            notAllowed = Convert.ToString(GetConfigValue("Messages", "NotAllowed", defaultNotAllowed));
            noPlayersFound = Convert.ToString(GetConfigValue("Messages", "NoPlayersFound", defaultNoPlayersFound));
            multiplePlayersFound = Convert.ToString(GetConfigValue("Messages", "MultiplePlayersFound", defaultMultiplePlayersFound));
            invalidSelection = Convert.ToString(GetConfigValue("Messages", "InvalidSelection", defaultInvalidSelection));
            invalidArguments = Convert.ToString(GetConfigValue("Messages", "InvalidArguments", defaultInvalidArguments));
            noListAvailable = Convert.ToString(GetConfigValue("Messages", "NoListAvailable", defaultNoListAvailable));
            targetDied = Convert.ToString(GetConfigValue("Messages", "TargetDied", defaultTargetDied));

            if (configChanged && !configCreated)
            {
                Log("Configuration file updated.");
                SaveConfig();
            }
        }

        [ChatCommand("viewinv")]
        void ViewInventory(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player)) return;

            if (args.Length < 1)
            {
                SendChatMessage(player, invalidArguments);
                return;
            }

            var name = args[0];
            var ply = onlinePlayers[player];
            if (name == "list")
            {
                if (ply.Matches == null)
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

                if (index > ply.Matches.Count)
                    SendChatMessage(player, invalidSelection);
                else
                    InspectInventory(player, ply.Matches[index - 1]);

                return;
            }

            var matches = FindPlayersByName(name);
            if (matches.Count < 1)
            {
                SendChatMessage(player, noPlayersFound);
                return;
            }
            if (matches.Count > 1)
            {
                ply.Matches = matches;
                ShowMatchingPlayers(player);
                return;
            }

            InspectInventory(player, matches[0]);
        }

        void InspectInventory(BasePlayer player, BasePlayer target)
        {
            var ply = onlinePlayers[player];
            if (ply.View == null)
            {
                OpenInventoryView(player, target);
                return;
            }

            CloseInventoryView(player, ply.View);
            timer.In(1f, () => OpenInventoryView(player, target));
        }

        void OpenInventoryView(BasePlayer player, BasePlayer target)
        {
            var pos = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
            var corpse = GameManager.server.CreateEntity("player/player_corpse") as BaseCorpse;
            corpse.parentEnt = null;
            corpse.transform.position = pos;
            corpse.CancelInvoke("RemoveCorpse");

            if (!corpse) return;

            var view = corpse as LootableCorpse;
            ItemContainer[] source = new ItemContainer[] { target.inventory.containerMain, target.inventory.containerWear, target.inventory.containerBelt };
            view.containers = new ItemContainer[source.Length];
            for (int i = 0; i < source.Length; i++)
                view.containers[i] = source[i];
            view.playerName = $"Inventory viewer: {target.displayName}";
            view.playerSteamID = target.userID;
            view.enableSaving = false;
            view.Spawn(true);
            player.inventory.loot.StartLootingEntity(view, false);
            for (int i = 0; i < source.Length; i++)
                view.containers[i] = source[i];
            view.SetFlag(BaseEntity.Flags.Open, true);
            foreach (var container in view.containers)
                player.inventory.loot.containers.Add(container);
            player.inventory.loot.SendImmediate();
            view.ClientRPCPlayer(null, player, "RPC_ClientLootCorpse", new object[0]);
            player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

            onlinePlayers[player].View = view;
            onlinePlayers[player].Target = target;
        }

        void CloseInventoryView(BasePlayer player, LootableCorpse view)
        {
            if (onlinePlayers[player].View == null) return;

            for (int i = 0; i < view.containers.Length; i++)
                view.containers[i] = new ItemContainer();

            if (player.inventory.loot.IsLooting())
                player.SendConsoleCommand("inventory.endloot", null);

            onlinePlayers[player].View = null;
            onlinePlayers[player].Target = null;

            view.KillMessage();
        }

        List<BasePlayer> FindPlayersByName(string name)
        {
            List<BasePlayer> matches = new List<BasePlayer>();

            foreach (var ply in BasePlayer.activePlayerList)
            {
                if (ply.displayName.ToLower().Contains(name.ToLower()))
                    matches.Add(ply);
            }

            foreach (var ply in BasePlayer.sleepingPlayerList)
            {
                if (ply.displayName.ToLower().Contains(name.ToLower()))
                    matches.Add(ply);
            }

            return matches;
        }

        void ShowMatchingPlayers(BasePlayer player)
        {
            int i = 0;
            SendChatMessage(player, multiplePlayersFound);
            foreach (var ply in onlinePlayers[player].Matches)
            {
                i++;
                SendChatMessage(player, $"{i} - {ply.displayName} ({ply.userID})");
            }
        }

        bool IsAllowed(BasePlayer player)
        {
            var authLevel = player.net.connection.authLevel;
            if (authLevel == 1 && allowModerator) return true;
            if (authLevel == 2 && allowAdmin) return true;
            SendChatMessage(player, notAllowed);
            return false;
        }

        #region Helper methods

        void Log(string message) =>
            Puts("{0} : {1}", Title, message);

        void SendChatMessage(BasePlayer player, string message, string arguments = null) =>
            PrintToChat(player, $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}");

        object GetConfigValue(string category, string setting, object defaultValue)
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
            return value;
        }

        #endregion

    }
}