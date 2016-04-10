using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Oxide.Core;

using Rust;

using RustNative;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BlueprintManager", "Nogrod", "1.2.0", ResourceId = 833)]
    class BlueprintManager : RustPlugin
    {
        private Dictionary<string, string> _itemShortname;
        private int _authLevel = 2;
        private int _authLevelOther = 2;
        private bool _giveOnConnect;
        private bool _configChanged;
        private List<ItemDefinition> _giveBps;
        private Dictionary<ulong, HashSet<string>> playerLearned;

        void OnServerInitialized()
        {
            _itemShortname = ItemManager.itemList.ToDictionary(definition => definition.displayName.english.ToLower(), definition => definition.shortname);
            _authLevel = GetConfig("authLevel", 2);
            _authLevelOther = GetConfig("authLevelOther", 2);
            _giveOnConnect = GetConfig("giveOnConnect", false);
            var bps = GetConfig("bps", new List<object>()).ConvertAll(Convert.ToString);
            var tmp = new HashSet<ItemDefinition>();
            foreach (var bp in bps)
            {
                var name = bp.ToLower();
                if (_itemShortname.ContainsKey(name))
                    name = _itemShortname[name];
                var definition = ItemManager.FindItemDefinition(name);
                if (definition == null)
                {
                    Puts("Item does not exist: {0}", name);
                    continue;
                }
                tmp.Add(definition);
            }
            _giveBps = new List<ItemDefinition>(tmp);

            var file = Interface.Oxide.DataFileSystem.GetFile($"{nameof(BlueprintManager)}_{GetConfig("rememberProtocol", Protocol.network - 1)}");
            if (file.Exists())
                playerLearned = file.ReadObject<Dictionary<ulong, HashSet<string>>>();

            if (!_configChanged) return;
            LoadDefaultConfig();
            SaveConfig();
            _configChanged = false;
        }

        new void LoadDefaultConfig()
        {
            Config.Clear();
            GetConfig("authLevel", 2);
            GetConfig("authLevelOther", 2);
            GetConfig("giveOnConnect", false);
            GetConfig("bps", new List<object>());
            GetConfig("rememberProtocol", Protocol.network - 1);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!_giveOnConnect) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerInit(player));
                return;
            }
            Learn(player.userID, _giveBps.Count > 0 ? _giveBps : ItemManager.itemList);
            SendReply(player, "You learned blueprints");
        }

        bool CheckAccess(BasePlayer player, int authLevel, ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.connection == null || player != null && (player.net?.connection?.authLevel >= authLevel || player.IsAdmin()))
                return true;
            Reply(player, arg, "You are not allowed to use this command");
            return false;
        }

        private T GetConfig<T>(string key, T defaultValue)
        {
            if (Config[key] != null) return (T)Convert.ChangeType(Config[key], typeof(T));
            Config[key] = defaultValue;
            _configChanged = true;
            return defaultValue;
        }

        [ConsoleCommand("bp.print")]
        void cmdConsoleBpPrint(ConsoleSystem.Arg arg)
        {
            Puts(string.Join(",", GetAllPersistentPlayerId().ToList().ConvertAll(id => id.ToString()).ToArray()));
        }

        [ConsoleCommand("bp.add")]
        void cmdConsoleBpAdd(ConsoleSystem.Arg arg)
        {
            BpAdd(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpadd")]
        void cmdChatBpAdd(BasePlayer player, string command, string[] args)
        {
            BpAdd(player, args);
        }

        void BpAdd(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevel, arg)) return;
            if (args == null || args.Length == 0)
            {
                Reply(player, arg, "/bpadd \"Item\" [\"PlayerName\"]");
                return;
            }
            Puts("{0} used /bpadd {1}", player?.displayName, string.Join(" ", args));
            var name = args[0].ToLower();
            if (_itemShortname.ContainsKey(name))
                name = _itemShortname[name];
            var definition = ItemManager.FindItemDefinition(name);
            if (definition == null)
            {
                Reply(player, arg, "Item does not exist: {0}", name);
                return;
            }
            var targetPlayer = player;
            if (args.Length > 1)
            {
                if (!CheckAccess(player, _authLevelOther, arg)) return;
                targetPlayer = FindPlayer(args[1]);
            }
            if (targetPlayer == null)
            {
                Reply(player, arg, "Player not found: {0}", args.Length > 1 ? args[1] : "unknown");
                return;
            }
            targetPlayer.blueprints.Learn(definition);
            SendReply(targetPlayer, "You learned {0}", definition.displayName.translated);
            if (targetPlayer != player)
            {
                Reply(player, arg, "{0} learned {1}", targetPlayer.displayName, definition.displayName.translated);
            }
        }

        [ConsoleCommand("bp.all")]
        void cmdConsoleBpAll(ConsoleSystem.Arg arg)
        {
            BpAll(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpall")]
        void cmdChatBpAll(BasePlayer player, string command, string[] args)
        {
            BpAll(player, args);
        }

        void BpAll(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevel, arg)) return;
            Puts("{0} used /bpall {1}", player?.displayName, string.Join(" ", args));
            var targetPlayer = player;
            if (args != null && args.Length > 0)
            {
                if (!CheckAccess(player, _authLevelOther, arg)) return;
                targetPlayer = FindPlayer(args[0]);
            }
            if (targetPlayer == null)
            {
                Reply(player, arg, "Player not found: {0}", args != null && args.Length > 0 ? args[0] : "unknown");
                return;
            }
            Learn(targetPlayer.userID, ItemManager.itemList);
            SendReply(targetPlayer, "You learned all blueprints");
            if (targetPlayer != player)
                Reply(player, arg, "{0} learned all blueprints", targetPlayer.displayName);
        }

        [ConsoleCommand("bp.reset")]
        void cmdConsoleBpReset(ConsoleSystem.Arg arg)
        {
            BpReset(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpreset")]
        void cmdChatBpReset(BasePlayer player, string command, string[] args)
        {
            BpReset(player, args);
        }

        void BpReset(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevel, arg)) return;
            Puts("{0} used /bpreset {1}", player?.displayName, string.Join(" ", args));
            var targetPlayer = player;
            if (args != null && args.Length > 0)
            {
                if (!CheckAccess(player, _authLevelOther, arg)) return;
                targetPlayer = FindPlayer(args[0]);
            }
            if (targetPlayer == null)
            {
                Reply(player, arg, "Player not found: {0}", args != null && args.Length > 0 ? args[0] : "unknown");
                return;
            }
            var data = ServerMgr.Instance.persistance.GetPlayerInfo(targetPlayer.userID);
            data.blueprints = null;
            PlayerBlueprints.InitializePersistance(data);
            ServerMgr.Instance.persistance.SetPlayerInfo(targetPlayer.userID, data);
            targetPlayer.SendFullSnapshot();
            SendReply(targetPlayer, "You forgot all blueprints");
            if (targetPlayer != player)
                Reply(player, arg, "{0} forgot all blueprints", targetPlayer.displayName);
        }

        [ConsoleCommand("bp.addall")]
        void cmdConsoleBpAddAll(ConsoleSystem.Arg arg)
        {
            BpAddAll(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpaddall")]
        void cmdChatBpAddAll(BasePlayer player, string command, string[] args)
        {
            BpAddAll(player, args);
        }

        void BpAddAll(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpaddall {1}", player?.displayName, string.Join(" ", args));
            List<ItemDefinition> definitions;
            if (args != null && args.Length > 0)
            {
                definitions = new List<ItemDefinition>();
                foreach (var cur in args)
                {
                    foreach (var def in cur.Split(','))
                    {
                        var name = def.ToLower();
                        if (_itemShortname.ContainsKey(name))
                            name = _itemShortname[name];
                        var itemDef = ItemManager.FindItemDefinition(name);
                        if (itemDef == null)
                        {
                            Reply(player, arg, "Item not found: {0}", def);
                            return;
                        }
                        definitions.Add(itemDef);
                    }
                }
            }
            else
                definitions = ItemManager.GetItemDefinitions();
            var allPersistentPlayerId = GetAllPersistentPlayerId();
            foreach (var persistentPlayerId in allPersistentPlayerId)
            {
                Learn(persistentPlayerId, definitions);
                var basePlayer = FindPlayer(persistentPlayerId.ToString());
                if (basePlayer != player)
                    Reply(player, arg, "{0} learned all blueprints", basePlayer == null ? persistentPlayerId.ToString() : basePlayer.displayName);
                if (basePlayer == null) continue;
                SendReply(basePlayer, "You learned all blueprints");
            }
        }

        [ConsoleCommand("bp.remove")]
        void cmdConsoleBpRemove(ConsoleSystem.Arg arg)
        {
            BpRemove(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpremove")]
        void cmdChatBpRemove(BasePlayer player, string command, string[] args)
        {
            BpRemove(player, args);
        }

        void BpRemove(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpremove {1}", player?.displayName, string.Join(" ", args));
            List<int> definitions;
            var defaultBlueprints = new List<int>(ItemManager.defaultBlueprints);
            if (args == null || args.Length <= 0)
            {
                Reply(player, arg, "No player(s) given.");
                return;
            }
            var allPersistentPlayerId = GetAllPersistentPlayerId().ToList();
            var players = new List<ulong>();
            foreach (var nameOrIdOrIp in args[0].Split(','))
            {
                if (string.IsNullOrEmpty(nameOrIdOrIp)) continue;
                var basePlayer = FindPlayer(nameOrIdOrIp);
                if (basePlayer == null)
                {
                    ulong userId;
                    if (!ulong.TryParse(nameOrIdOrIp, out userId) || !allPersistentPlayerId.Contains(userId))
                    {
                        Reply(player, arg, "Player not found: {0}", nameOrIdOrIp);
                        return;
                    }
                    players.Add(userId);
                    continue;
                }
                players.Add(basePlayer.userID);
            }
            if (args.Length > 1)
            {
                definitions = new List<int>();
                foreach (var def in args[1].Split(','))
                {
                    var name = def.ToLower();
                    if (_itemShortname.ContainsKey(name))
                        name = _itemShortname[name];
                    var itemDef = ItemManager.FindItemDefinition(name);
                    if (itemDef == null)
                    {
                        Reply(player, arg, "Item not found: {0}", def);
                        return;
                    }
                    definitions.Add(itemDef.itemid);
                }
            }
            else
            {
                definitions = ItemManager.GetItemDefinitions().ConvertAll(i => i.itemid);
                //just delete non default
                definitions.RemoveAll(d => defaultBlueprints.Contains(d));
            }
            var defaultRemoved = definitions.Where(d => defaultBlueprints.Contains(d)).Select(d => ItemManager.itemDictionary[d].shortname).ToArray();
            if (defaultRemoved.Length > 0)
            {
                Reply(player, arg, "Found default blueprint(s)! Removed until respawn.");
                Reply(player, arg, "Bps: " + string.Join(",", defaultRemoved));
            }
            foreach (var persistentPlayerId in players)
            {
                if (persistentPlayerId == 0 ) continue;
                var data = ServerMgr.Instance.persistance.GetPlayerInfo(persistentPlayerId);
                if (data.blueprints.complete.RemoveAll(a => definitions.Contains(a)) > 0)
                {
                    ServerMgr.Instance.persistance.SetPlayerInfo(persistentPlayerId, data);
                    var targetPlayer = FindPlayer(persistentPlayerId.ToString());
                    if (targetPlayer?.net?.subscriber?.subscribed == null) continue;
                    targetPlayer.SendFullSnapshot();
                }
            }
            Reply(player, arg, "Removed learned blueprints");
        }

        [ConsoleCommand("bp.removeall")]
        void cmdConsoleBpRemoveAll(ConsoleSystem.Arg arg)
        {
            BpRemoveAll(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpremoveall")]
        void cmdChatBpRemoveAll(BasePlayer player, string command, string[] args)
        {
            BpRemoveAll(player, args);
        }

        void BpRemoveAll(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpremoveall {1}", player?.displayName, string.Join(" ", args));
            List<int> definitions;
            var defaultBlueprints = new List<int>(ItemManager.defaultBlueprints);
            if (args != null && args.Length > 0)
            {
                definitions = new List<int>();
                foreach (var cur in args)
                {
                    foreach (var def in cur.Split(','))
                    {
                        var name = def.ToLower();
                        if (_itemShortname.ContainsKey(name))
                            name = _itemShortname[name];
                        var itemDef = ItemManager.FindItemDefinition(name);
                        if (itemDef == null)
                        {
                            Reply(player, arg, "Item not found: {0}", def);
                            return;
                        }
                        definitions.Add(itemDef.itemid);
                    }
                }
            }
            else
            {
                definitions = ItemManager.GetItemDefinitions().ConvertAll(i => i.itemid);
                //just delete non default
                definitions.RemoveAll(d => defaultBlueprints.Contains(d));
            }
            var defaultRemoved = definitions.Where(d => defaultBlueprints.Contains(d)).Select(d => ItemManager.itemDictionary[d].shortname).ToArray();
            if (defaultRemoved.Length > 0)
            {
                Reply(player, arg, "Found default blueprint(s)! Removed until respawn.");
                Reply(player, arg, "Bps: " + string.Join(",", defaultRemoved));
            }
            var allPersistentPlayerId = GetAllPersistentPlayerId();
            foreach (var persistentPlayerId in allPersistentPlayerId)
            {
                if (persistentPlayerId == 0) continue;
                var data = ServerMgr.Instance.persistance.GetPlayerInfo(persistentPlayerId);
                if (data.blueprints.complete.RemoveAll(a => definitions.Contains(a)) > 0)
                {
                    ServerMgr.Instance.persistance.SetPlayerInfo(persistentPlayerId, data);
                    var targetPlayer = FindPlayer(persistentPlayerId.ToString());
                    if (targetPlayer?.net?.subscriber?.subscribed == null) continue;
                    targetPlayer.SendFullSnapshot();
                }
            }
            Reply(player, arg, "Removed learned blueprints");
        }

        [ConsoleCommand("bp.clean")]
        void cmdConsoleBpClean(ConsoleSystem.Arg arg)
        {
            BpClean(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpclean")]
        void cmdChatBpClean(BasePlayer player, string command, string[] args)
        {
            BpClean(player, args);
        }

        void BpClean(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpclean {1}", player?.displayName, string.Join(" ", args));
            var playerIds = BasePlayer.activePlayerList.ConvertAll(p => p.userID);
            playerIds.AddRange(BasePlayer.sleepingPlayerList.ConvertAll(p => p.userID));
            DeletePersistentPlayersExcept(playerIds);
            Reply(player, arg, "Cleaned learned blueprints");
        }

        [ConsoleCommand("bp.save")]
        void cmdConsoleBpSave(ConsoleSystem.Arg arg)
        {
            BpSave(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpsave")]
        void cmdChatBpSave(BasePlayer player, string command, string[] args)
        {
            BpSave(player, args);
        }

        void BpSave(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpsave {1}", player?.displayName, string.Join(" ", args));
            var allPersistentPlayerId = GetAllPersistentPlayerId();
            var playerLearned = new Dictionary<ulong, HashSet<string>>();
            foreach (var persistentPlayerId in allPersistentPlayerId)
            {
                if (persistentPlayerId == 0) continue;
                var learned = GetLearned(persistentPlayerId);
                if (learned.Count <= 0) continue;
                playerLearned.Add(persistentPlayerId, learned);
            }
            Interface.Oxide.DataFileSystem.WriteObject($"{nameof(BlueprintManager)}_{Protocol.network}", playerLearned);
            Reply(player, arg, "Saved learned blueprints");
        }

        [ConsoleCommand("bp.load")]
        void cmdConsoleBpLoad(ConsoleSystem.Arg arg)
        {
            BpLoad(arg.Player(), arg.Args ?? new string[0], arg);
        }

        [ChatCommand("bpload")]
        void cmdChatBpLoad(BasePlayer player, string command, string[] args)
        {
            BpLoad(player, args);
        }

        void BpLoad(BasePlayer player, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (!CheckAccess(player, _authLevelOther, arg)) return;
            Puts("{0} used /bpload {1}", player?.displayName, string.Join(" ", args));
            if (playerLearned == null || playerLearned.Count <= 0)
            {
                Reply(player, arg, "Nothing to remember.");
                return;
            }
            foreach (var learned in playerLearned)
            {
                var definitions = new List<ItemDefinition>();
                foreach (var shortName in learned.Value)
                {
                    var itemDef = ItemManager.FindItemDefinition(shortName);
                    if (itemDef == null)
                    {
                        Reply(player, arg, "Item not found: {0}", shortName);
                        return;
                    }
                    definitions.Add(itemDef);
                }
                Learn(learned.Key, definitions);
                var basePlayer = FindPlayer(learned.Key.ToString());
                if (basePlayer != player)
                    Reply(player, arg, "{0} recovered old blueprints", basePlayer == null ? learned.Key.ToString() : basePlayer.displayName);
                if (basePlayer == null) continue;
                SendReply(basePlayer, "You recovered your old blueprints");
            }
            Reply(player, arg, "Loaded learned blueprints");
        }

        [ChatCommand("remember")]
        void cmdChatRemember(BasePlayer player, string command, string[] args)
        {
            Puts("{0} used /remember {1}", player.displayName, string.Join(" ", args));
            HashSet<string> learned;
            if (playerLearned == null || !playerLearned.TryGetValue(player.userID, out learned) || learned.Count <= 0)
            {
                SendReply(player, "Nothing to remember.");
                return;
            }
            var definitions = new List<ItemDefinition>();
            foreach (var shortName in learned)
            {
                var itemDef = ItemManager.FindItemDefinition(shortName);
                if (itemDef == null)
                {
                    Puts("Item not found: {0} Player: {1}", shortName, player.displayName);
                    return;
                }
                definitions.Add(itemDef);
            }
            Learn(player.userID, definitions);
            SendReply(player, "You recovered your old blueprints");
        }

        void Reply(BasePlayer player, ConsoleSystem.Arg arg, string format, params object[] args)
        {
            if (arg != null) SendReply(arg, format, args);
            else if (player != null) SendReply(player, format, args);
            else Puts(format, args);
        }

        private static void Learn(ulong persistentPlayerId, IEnumerable<ItemDefinition> itemDefs)
        {
            var playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(persistentPlayerId);
            var learned = false;
            var player = FindPlayer(persistentPlayerId.ToString());
            foreach (var itemDef in itemDefs)
            {
                if (playerInfo.blueprints.complete.Contains(itemDef.itemid)) continue;
                learned = true;
                playerInfo.blueprints.complete.Add(itemDef.itemid);
                if (player?.net == null) continue;
                player.SendNetworkUpdate();
                player.ClientRPCPlayer(null, player, "UnlockedBlueprint", itemDef.itemid);
                player.stats.Add("blueprint_studied", 1);
            }
            if (learned)
                ServerMgr.Instance.persistance.SetPlayerInfo(persistentPlayerId, playerInfo);
        }

        private static HashSet<string> GetLearned(ulong persistentPlayerId)
        {
            var playerInfo = ServerMgr.Instance.persistance.GetPlayerInfo(persistentPlayerId);
            var bpss = new HashSet<string>();
            if (playerInfo == null) return bpss;
            var bps = playerInfo.blueprints.complete.ToList();
            bps.RemoveAll(i => ItemManager.defaultBlueprints.Contains(i));
            foreach (var bp in bps)
            {
                var definition = ItemManager.FindItemDefinition(bp);
                if (definition == null) continue;
                bpss.Add(definition.shortname);
            }
            return bpss;
        }

        private void DeletePersistentPlayersExcept(List<ulong> players)
        {
            var dbField = typeof(UserPersistance).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
            var db = (SQLite)dbField?.GetValue(ServerMgr.Instance.persistance);
            if (db == null) return;
            try
            {
                db.Execute($"DELETE FROM datatable WHERE steamid NOT IN ({string.Join(",", players.ConvertAll(p => p.ToString()).ToArray())})");
            }
            catch (Exception e)
            {
                Puts("Execute failed: {0}", e.Message);
            }
        }

        private ulong[] GetAllPersistentPlayerId()
        {
            var dbField = typeof (UserPersistance).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
            var db = (SQLite)dbField?.GetValue(ServerMgr.Instance.persistance);
            var columnValue = new List<ulong>();
            if (db == null) return columnValue.ToArray();
            try
            {
                db.QueryPrepare("SELECT steamid FROM datatable");
                if (db.Columns() <= 0)
                {
                    db.QueryFinalize();
                    return columnValue.ToArray();
                }
                while (db.StepRow())
                    columnValue.Add(db.GetColumnValue<ulong>(0, 0));
                db.QueryFinalize();
            }
            catch (Exception e)
            {
                Puts("Query failed: {0}", e.Message);
                try
                {
                    db.QueryFinalize();
                }
                catch (Exception)
                {
                }
            }
            return columnValue.ToArray();
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
    }
}
