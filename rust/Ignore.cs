using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Ignore", "Nogrod", "2.0.0", ResourceId = 1054)]
    class Ignore : RustPlugin
    {
        private ConfigData configData;
        private Dictionary<ulong, PlayerData> IgnoreData;
        private readonly Dictionary<ulong, HashSet<ulong>> ReverseData = new Dictionary<ulong, HashSet<ulong>>();

        class ConfigData
        {
            public int IgnoreLimit { get; set; }
            public bool ShareCodeLocks { get; set; }
            public bool ShareAutoTurrets { get; set; }
        }

        class PlayerData
        {
            public string Name { get; set; } = string.Empty;
            public HashSet<ulong> Ignores { get; set; } = new HashSet<ulong>();
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                IgnoreLimit = 30,
                ShareCodeLocks = false,
                ShareAutoTurrets = false
            };
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"List", "Ignored {0}:\n{1}"},
                {"NoIngored", "Your ignore list is empty."},
                {"NotOnIgnorelist", "{0} not found on your ignorelist."},
                {"IgnoreRemoved", "{0} was removed from your ignorelist."},
                {"PlayerNotFound", "Player '{0}' not found."},
                {"CantAddSelf", "You cant add yourself."},
                {"AlreadyOnList", "{0} is already ignored."},
                {"IgnoreAdded", "{0} is now ignored."},
                {"IgnorelistFull", "Your ignorelist is full."},
                {"HelpText", "Use /ignore <add|+|remove|-|list> <name/steamID> to add/remove/list ignores"},
                {"Syntax", "Syntax: /ignore <add/+/remove/-> <name/steamID> or /ignore list"}
            }, this);
            configData = Config.ReadObject<ConfigData>();
            try
            {
                IgnoreData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>(nameof(Ignore));
            }
            catch
            {
                IgnoreData = new Dictionary<ulong, PlayerData>();
            }
            foreach (var data in IgnoreData)
                foreach (var friend in data.Value.Ignores)
                    AddIgnoreReverse(data.Key, friend);
        }

        private void SaveIgnores()
        {
            Interface.Oxide.DataFileSystem.WriteObject(nameof(Ignore), IgnoreData);
        }

        private bool AddIgnoreS(string playerS, string ignoreS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(ignoreS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var ignoreId = Convert.ToUInt64(ignoreS);
            return AddIgnore(playerId, ignoreId);
        }

        private bool AddIgnore(ulong playerId, ulong ignoreId)
        {
            var playerData = GetPlayerData(playerId);
            if (playerData.Ignores.Count >= configData.IgnoreLimit || !playerData.Ignores.Add(ignoreId)) return false;
            AddIgnoreReverse(playerId, ignoreId);
            SaveIgnores();
            return true;
        }

        private bool RemoveIgnoreS(string playerS, string ignoreS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(ignoreS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var ignoreId = Convert.ToUInt64(ignoreS);
            return RemoveIgnore(playerId, ignoreId);
        }

        private bool RemoveIgnore(ulong playerId, ulong ignoreId)
        {
            if (!GetPlayerData(playerId).Ignores.Remove(ignoreId)) return false;
            HashSet<ulong> ignoreS;
            if (ReverseData.TryGetValue(ignoreId, out ignoreS))
                ignoreS.Remove(playerId);
            SaveIgnores();
            return true;
        }

        private bool HasIgnoredS(string playerS, string ignoreS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(ignoreS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var ignoreId = Convert.ToUInt64(ignoreS);
            return HasIgnored(playerId, ignoreId);
        }

        private bool HasIgnored(ulong playerId, ulong ignoreId)
        {
            return GetPlayerData(playerId).Ignores.Contains(ignoreId);
        }

        private bool AreIgnoredS(string playerS, string ignoreS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(ignoreS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var ignoreId = Convert.ToUInt64(ignoreS);
            return AreIgnored(playerId, ignoreId);
        }

        private bool AreIgnored(ulong playerId, ulong ignoreId)
        {
            return GetPlayerData(playerId).Ignores.Contains(ignoreId) && GetPlayerData(ignoreId).Ignores.Contains(playerId);
        }

        private bool IsIgnoredS(string playerS, string ignoreS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(ignoreS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var ignoreId = Convert.ToUInt64(ignoreS);
            return IsIgnored(playerId, ignoreId);
        }

        private bool IsIgnored(ulong playerId, ulong ignoreId)
        {
            return GetPlayerData(ignoreId).Ignores.Contains(playerId);
        }

        private string[] GetIgnoreListS(string playerS)
        {
            var playerId = Convert.ToUInt64(playerS);
            return GetIgnoreList(playerId);
        }

        private string[] GetIgnoreList(ulong playerId)
        {
            var playerData = GetPlayerData(playerId);
            var players = new List<string>();
            foreach (var friend in playerData.Ignores)
                players.Add(GetPlayerData(friend).Name);
            return players.ToArray();
        }

        private string[] IsIgnoredByS(string playerS)
        {
            var playerId = Convert.ToUInt64(playerS);
            var ignores = IsIgnoredBy(playerId);
            return ignores.ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        private ulong[] IsIgnoredBy(ulong playerId)
        {
            HashSet<ulong> ignores;
            return ReverseData.TryGetValue(playerId, out ignores) ? ignores.ToArray() : new ulong[0];
        }

        private PlayerData GetPlayerData(ulong playerId)
        {
            var player = FindPlayer(playerId);
            PlayerData playerData;
            if (!IgnoreData.TryGetValue(playerId, out playerData))
                IgnoreData[playerId] = playerData = new PlayerData();
            if (player != null) playerData.Name = player.displayName;
            return playerData;
        }

        [ChatCommand("ignore")]
        private void cmdIgnore(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length <= 0 || args.Length == 1 && !args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                PrintMessage(player, "Syntax");
                return;
            }
            switch (args[0].ToLower())
            {
                case "list":
                    var ignoreList = GetIgnoreList(player.userID);
                    if (ignoreList.Length > 0)
                        PrintMessage(player, "List", $"{ignoreList.Length}/{configData.IgnoreLimit}", string.Join(", ", ignoreList));
                    else
                        PrintMessage(player, "NoIngored");
                    return;
                case "add":
                case "+":
                    var ignorePlayer = FindPlayer(args[1]);
                    if (ignorePlayer == null)
                    {
                        PrintMessage(player, "PlayerNotFound", args[1]);
                        return;
                    }
                    if (player == ignorePlayer)
                    {
                        PrintMessage(player, "CantAddSelf");
                        return;
                    }
                    var playerData = GetPlayerData(player.userID);
                    if (playerData.Ignores.Count >= configData.IgnoreLimit)
                    {
                        PrintMessage(player, "IgnorelistFull");
                        return;
                    }
                    if (playerData.Ignores.Contains(ignorePlayer.userID))
                    {
                        PrintMessage(player, "AlreadyOnList", ignorePlayer.displayName);
                        return;
                    }
                    AddIgnore(player.userID, ignorePlayer.userID);
                    PrintMessage(player, "IgnoreAdded", ignorePlayer.displayName);
                    return;
                case "remove":
                case "-":
                    var ignore = FindIgnore(args[1]);
                    if (ignore <= 0)
                    {
                        PrintMessage(player, "NotOnIgnorelist", args[1]);
                        return;
                    }
                    var removed = RemoveIgnore(player.userID, ignore);
                    PrintMessage(player, removed ? "IgnoreRemoved" : "NotOnIgnorelist", args[1]);
                    return;
            }
        }

        private void SendHelpText(BasePlayer player)
        {
            PrintMessage(player, "HelpText");
        }

        private void AddIgnoreReverse(ulong playerId, ulong ignoreId)
        {
            HashSet<ulong> ignoreS;
            if (!ReverseData.TryGetValue(ignoreId, out ignoreS))
                ReverseData[ignoreId] = ignoreS = new HashSet<ulong>();
            ignoreS.Add(playerId);
        }

        private void PrintMessage(BasePlayer player, string msgId, params object[] args)
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        private ulong FindIgnore(string ignore)
        {
            if (string.IsNullOrEmpty(ignore)) return 0;
            foreach (var playerData in IgnoreData)
            {
                if (playerData.Key.ToString().Equals(ignore) || playerData.Value.Name.Contains(ignore, CompareOptions.OrdinalIgnoreCase))
                    return playerData.Key;
            }
            return 0;
        }

        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }

        private static BasePlayer FindPlayer(ulong id)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == id)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID == id)
                    return sleepingPlayer;
            }
            return null;
        }
    }
}
