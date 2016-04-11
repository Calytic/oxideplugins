using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Permanent Blueprints", "Skipcast", "1.0.1")]
    [Description("Saves blueprints between BP wipes.")]
    public class PermanentBPs : RustPlugin
    {
        const string PermissionName = "permanentbps.blacklistblueprint";

        static class Translations
        {
            public class Translation
            {
                public static Plugin plugin;
                public static Lang lang;

                public readonly string Key;
                public readonly string DefaultValue;

                public Translation(string key, string defaultValue)
                {
                    Key = key;
                    DefaultValue = defaultValue;
                }

                /// <summary>Returns the translated string.</summary>
                public string Get(BasePlayer player)
                {
                    return lang.GetMessage(Key, plugin, player?.UserIDString);
                }
            }

            public static readonly Translation NoItemWithName = new Translation("NoItemWithName", "There is no item with this name.");
            public static readonly Translation ItemAlreadyBlacklisted = new Translation("ItemAlreadyBlacklisted", "This item is already blacklisted.");
            public static readonly Translation ItemNotBlacklisted = new Translation("ItemNotBlacklisted", "This item is not blacklisted.");
            public static readonly Translation AddedToBlacklist = new Translation("AddedToBlacklist", "Added {item} to the blacklist.");
            public static readonly Translation RemovedFromBlacklist = new Translation("RemovedFromBlacklist", "Removed {item} from the blacklist.");
            public static readonly Translation NoPermission = new Translation("NoPermission", "You don't have permission to use this command.");
            public static readonly Translation BlueprintsLearnedNotice = new Translation("BlueprintsLearnedNotice", "Blueprints were recently wiped. Luckily we saved them and restored {numBlueprints} blueprints!");
            public static readonly Translation BlacklistedBlueprintsNotice = new Translation("BlacklistedBlueprintsNotice", "The following blueprints were not restored because they are blacklisted:");
        }

        class PlayerData
        {
            public List<string> Blueprints = new List<string>();
        }

        class StoredData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
        }

        class PluginConfig
        {
            public List<string> BlacklistedBlueprints = new List<string>();
        }

        private StoredData data;
        private List<string> blacklistedBlueprints;
        
        void Init()
        {
            Translations.Translation.lang = lang;
            Translations.Translation.plugin = this;
        }

        protected override void LoadDefaultConfig()
        {
            Debug.LogWarning("Creating default configuration for Permanent Blueprints.");

            Config.Clear();
            Config["BlacklistedBlueprints"] = new List<string>();
            SaveConfig();
        }

        void LoadDefaultMessages()
        {
            Dictionary<string, string> messages = new Dictionary<string, string>();

            foreach (FieldInfo info in typeof (Translations).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (info.FieldType != typeof (Translations.Translation))
                    continue;

                Translations.Translation translation = (Translations.Translation) info.GetValue(null);
                messages.Add(translation.Key, translation.DefaultValue);
            }
            
            lang.RegisterMessages(messages, this);
        }

        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("PermanentBPs") ?? new StoredData();
            blacklistedBlueprints = ((List<object>)Config["BlacklistedBlueprints"]).Cast<string>().ToList();
            Config["BlacklistedBlueprints"] = blacklistedBlueprints;

            LoadDefaultMessages();

            if (blacklistedBlueprints == null)
            {
                Debug.LogWarning("Blacklisted blueprints config value was null.");
                blacklistedBlueprints = new List<string>();
                SaveConfig();
            }
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission(PermissionName, this);
            SaveBPs();
        }

        void OnServerSave()
        {
            SaveBPs();
        }

        private void SaveBPs()
        {
            foreach (var player in BasePlayer.activePlayerList.Concat(BasePlayer.sleepingPlayerList).ToList())
            {
                PlayerData playerData;

                if (!data.PlayerData.ContainsKey(player.userID))
                    data.PlayerData[player.userID] = new PlayerData();

                playerData = data.PlayerData[player.userID];

                var persistantPlayer = ServerMgr.Instance.persistance.GetPlayerInfo(player.userID);

                foreach (var itemId in persistantPlayer.blueprints.complete)
                {
                    var item = ItemManager.FindItemDefinition(itemId);

                    if (!playerData.Blueprints.Contains(item.shortname))
                        playerData.Blueprints.Add(item.shortname);
                }
            }

            Interface.Oxide.DataFileSystem.WriteObject("PermanentBPs", data);
        }

        void OnPlayerInit(BasePlayer player)
        {
            RelearnBlueprints(player);
        }

        [ChatCommand("blacklistbp")]
        void ChatCmd_BlacklistBlueprint(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                var itemDef = GetItemDefinitionFromDisplayName(args);

                if (itemDef == null)
                {
                    player.ChatMessage(Translations.NoItemWithName.Get(player));
                    return;
                }

                if (blacklistedBlueprints.Contains(itemDef.shortname))
                {
                    player.ChatMessage(Translations.ItemAlreadyBlacklisted.Get(player));
                    return;
                }

                blacklistedBlueprints.Add(itemDef.shortname);
                SaveConfig();
                player.ChatMessage(Translations.AddedToBlacklist.Get(player).Replace("{item}", itemDef.displayName.english));
            }
            else
            {
                player.ChatMessage(Translations.NoPermission.Get(player));
            }
        }

        [ChatCommand("whitelistbp")]
        void ChatCmd_WhitelistBlueprint(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                var itemDef = GetItemDefinitionFromDisplayName(args);

                if (itemDef == null)
                {
                    player.ChatMessage(Translations.NoItemWithName.Get(player));
                    return;
                }

                if (!blacklistedBlueprints.Contains(itemDef.shortname))
                {
                    player.ChatMessage(Translations.ItemNotBlacklisted.Get(player));
                    return;
                }

                blacklistedBlueprints.Remove(itemDef.shortname);
                SaveConfig();
                player.ChatMessage(Translations.RemovedFromBlacklist.Get(player).Replace("{item}", itemDef.displayName.english));
            }
            else
            {
                player.ChatMessage(Translations.NoPermission.Get(player));
            }
        }
        
        private static ItemDefinition GetItemDefinitionFromDisplayName(string[] args)
        {
            string itemName = string.Join(" ", args);

            var itemDef = ItemManager.itemList.FirstOrDefault(item => item.displayName.english.ToLower() == itemName.ToLower());
            return itemDef;
        }

        private void RelearnBlueprints(BasePlayer player)
        {
            uint relearnedBlueprintCount = 0;
            List<ItemDefinition> blacklistedBps = new List<ItemDefinition>();

            if (data.PlayerData.ContainsKey(player.userID))
            {
                var playerData = data.PlayerData[player.userID];

                foreach (var shortname in playerData.Blueprints)
                {
                    var itemDef = ItemManager.FindItemDefinition(shortname);

                    if (itemDef == null)
                    {
                        Debug.LogError("No item found with the shortname \"" + shortname + "\".");
                        continue;
                    }

                    if (!player.blueprints.AlreadyKnows(itemDef))
                    {
                        ItemBlueprint blueprint = itemDef.GetComponent<ItemBlueprint>();
                        
                        if (blueprint != null && blueprint.userCraftable && blueprint.rarity != Rarity.None)
                        {
                            if (!blacklistedBlueprints.Contains(itemDef.shortname))
                            {
                                player.blueprints.Learn(itemDef);
                                ++relearnedBlueprintCount;
                            }
                            else
                            {
                                blacklistedBps.Add(itemDef);
                            }
                        }
                    }
                }
            }

            if (relearnedBlueprintCount > 0)
            {
                player.ChatMessage(Translations.BlueprintsLearnedNotice.Get(player).Replace("{numBlueprints}", relearnedBlueprintCount.ToString())
                                    + (blacklistedBps.Count > 0
                                                            ? ("\n\n" + Translations.BlacklistedBlueprintsNotice.Get(player) + "\n- " + String.Join("\n- ", blacklistedBps.Select(itemDef => itemDef.displayName.english).ToArray()))
                                                            : ""));
            }
        }
    }
}
