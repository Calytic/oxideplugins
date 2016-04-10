using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("StrikeSystem", "LaserHydra", "2.0.0", ResourceId = 0)]
    [Description("Strike players & time-ban players with a specific amount of strikes")]
    class StrikeSystem : HurtworldPlugin
    {
        static List<Player> players = new List<Player>();

        [PluginReference("EnhancedBanSystem")]
        Plugin EBS;

        #region Classes

        class Player
        {
            public ulong steamId = 0;
            public string name = "unkown";
            public string lastStrike = "not striked yet";
            public int strikes = 0;
            public int activeStrikes = 0;

            public Player()
            {
            }

            internal Player(PlayerSession player)
            {
                this.steamId = Convert.ToUInt64(player.SteamId.ToString());
                this.name = player.Name;
            }

            internal void Update(PlayerSession player) => this.name = player.Name;

            internal static Player Get(PlayerSession player)
            {
                return players.Find((p) =>
                {
                    if (p.steamId == Convert.ToUInt64(player.SteamId.ToString()))
                        return true;
                    else
                        return false;
                });
            }

            public override bool Equals(object obj)
            {
                if (obj is Player && ((Player)obj).steamId == this.steamId)
                    return true;

                return false;
            }

            public override int GetHashCode() => Convert.ToInt32(steamId);
        }

        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !HURTWORLD
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            LoadConfig();
            LoadMessages();
            LoadData(ref players);

            RegisterPerm("admin");

            foreach (PlayerSession player in GameManager.Instance.GetSessions().Values)
                if (Player.Get(player) == null)
                {
                    players.Add(new Player(player));
                    SaveData(ref players);
                }
                else
                    Player.Get(player).Update(player);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("Settings", "Strikes Until Ban", 3);
            SetConfig("Settings", "Permanent Ban", false);
            SetConfig("Settings", "Ban Time in Seconds", 86400);

            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Striked", "{player} has been striked. Reason: {reason}"},
                {"Banned", "{player} was banned due too many strikes. Reason: {reason}"},
                {"Can Not Join", "You were banned due too many strikes. Reason: {reason}"},
                {"True Or False", "{arg} must be 'true' or 'false'!"},
                {"Invalid Number", "{arg} must be a valid number!"},
                {"Reset", "{player}'s strikes were reset."},
                {"Removed", "Removed {amount} strikes from {player}"},
                {"Wiped", "Strikes were been wiped!"},
                {"Fully Wiped", "All data was wiped!"}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Commands

        [ChatCommand("strike")]
        void cmdStrike(PlayerSession player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                if (HasPerm(player.SteamId, "admin"))
                    SendChatMessage(player, "<color=#C4FF00>/strike <player> [reason]</color> strike player" + Environment.NewLine +
                                            "<color=#C4FF00>/strike reset <player> [only Active Strikes: true/false]</color> reset players strikes" + Environment.NewLine +
                                            "<color=#C4FF00>/strike remove <player> [amount] [only Active Strikes: true/false]</color> remove strikes of a player " + Environment.NewLine +
                                            "<color=#C4FF00>/strike info [player]</color> get strike info about a player" + Environment.NewLine +
                                            "<color=#C4FF00>/strike wipe [only Active Strikes: true/false]</color> wipe all strikes" + Environment.NewLine +
                                            "<color=#C4FF00>/strike wipefull</color> wipe all data");
                else
                    SendChatMessage(player, "<color=#C4FF00>/strike info</color> get your strike info");

                return;
            }

            switch (args[0].ToLower())
            {
                case "reset":
                    if (!HasPerm(player.SteamId, "admin"))
                        return;

                    if (args.Length != 2 && args.Length != 3)
                    {
                        SendChatMessage(player, "Syntax: /strike reset <player> [only Active Strikes: true/false]");
                        return;
                    }

                    PlayerSession resetTarget = GetPlayer(args[1], player);

                    if (resetTarget == null)
                        return;

                    Player resetPl = Player.Get(resetTarget);

                    if (resetPl == null)
                    {
                        players.Add(new Player(resetTarget));
                        SaveData(ref players);

                        resetPl = Player.Get(resetTarget);
                    }

                    bool resetOnlyActive = true;

                    if (args.Length == 3)
                    {
                        try
                        {
                            resetOnlyActive = Convert.ToBoolean(args[2]);
                        }
                        catch (FormatException)
                        {
                            SendChatMessage(player, GetMsg("True Or False", player.SteamId));
                            return;
                        }
                    }

                    resetPl.activeStrikes = 0;

                    if (!resetOnlyActive)
                    {
                        resetPl.strikes = 0;
                        resetPl.lastStrike = "not striked yet";
                    }

                    SaveData(ref players);

                    SendChatMessage(player, GetMsg("Reset", player.SteamId).Replace("{player}", resetTarget.Name));

                    break;

                case "remove":
                    if (!HasPerm(player.SteamId, "admin"))
                        return;

                    if (args.Length != 2 && args.Length != 3 && args.Length != 4)
                    {
                        SendChatMessage(player, "Syntax: /strike remove <player> [amount] [only Active Strikes: true/false]");
                        return;
                    }

                    PlayerSession removeTarget = GetPlayer(args[1], player);

                    if (removeTarget == null)
                        return;

                    Player removePl = Player.Get(removeTarget);

                    if (removePl == null)
                    {
                        players.Add(new Player(removeTarget));
                        SaveData(ref players);

                        removePl = Player.Get(removeTarget);
                    }

                    int removeAmount = 1;

                    if (args.Length >= 3)
                    {
                        try
                        {
                            removeAmount = Convert.ToInt32(args[2]);
                        }
                        catch (FormatException)
                        {
                            SendChatMessage(player, GetMsg("Invalid Number", player.SteamId));
                            return;
                        }
                    }

                    bool removeOnlyActive = true;

                    if (args.Length == 4)
                    {
                        try
                        {
                            removeOnlyActive = Convert.ToBoolean(args[3]);
                        }
                        catch (FormatException)
                        {
                            SendChatMessage(player, GetMsg("True Or False", player.SteamId).Replace("{arg}", "[only Active Strikes: true/false]"));
                            return;
                        }
                    }

                    removePl.activeStrikes -= removeAmount;

                    if (!removeOnlyActive)
                        removePl.strikes -= removeAmount;

                    SaveData(ref players);

                    SendChatMessage(player, GetMsg("Removed", player.SteamId).Replace("{player}", removeTarget.Name).Replace("{amount}", removeAmount.ToString()));

                    break;

                case "info":
                    if (!HasPerm(player.SteamId, "admin"))
                    {
                        Player pl = Player.Get(player);

                        if (pl == null)
                        {
                            players.Add(new Player(player));
                            SaveData(ref players);

                            pl = Player.Get(player);
                        }

                        SendChatMessage(player, $"<color=#C4FF00>Last Strike</color>: {pl.lastStrike}" + Environment.NewLine +
                                                $"<color=#C4FF00>Active Strikes</color>: {pl.activeStrikes}" + Environment.NewLine +
                                                $"<color=#C4FF00>Total Strikes</color>: {pl.strikes}");
                    }
                    else
                    {
                        if (args.Length != 1 && args.Length != 2)
                        {
                            SendChatMessage(player, "Syntax: /strike info [player]");
                            return;
                        }

                        if (args.Length == 1)
                        {
                            Player pl = Player.Get(player);

                            if (pl == null)
                            {
                                players.Add(new Player(player));
                                SaveData(ref players);

                                pl = Player.Get(player);
                            }

                            SendChatMessage(player, $"<color=#C4FF00>Last Strike</color>: {pl.lastStrike}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Active Strikes</color>: {pl.activeStrikes}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Total Strikes</color>: {pl.strikes}");
                        }
                        else
                        {
                            PlayerSession infoTarget = GetPlayer(args[1], player);

                            if (infoTarget == null)
                                return;

                            Player infoPl = Player.Get(infoTarget);

                            if (infoPl == null)
                            {
                                players.Add(new Player(infoTarget));
                                SaveData(ref players);

                                infoPl = Player.Get(infoTarget);
                            }

                            SendChatMessage(player, $"<color=#C4FF00>Last Strike</color>: {infoPl.lastStrike}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Active Strikes</color>: {infoPl.activeStrikes}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Total Strikes</color>: {infoPl.strikes}");
                        }
                    }
                    break;

                case "wipe":
                    if (!HasPerm(player.SteamId, "admin"))
                        return;

                    if (args.Length != 1 && args.Length != 2)
                    {
                        SendChatMessage(player, "Syntax: /strike wipe [only Active Strikes: true/false]");
                        return;
                    }

                    bool wipeOnlyActive = true;

                    if (args.Length == 2)
                    {
                        try
                        {
                            wipeOnlyActive = Convert.ToBoolean(args[1]);
                        }
                        catch (FormatException)
                        {
                            SendChatMessage(player, GetMsg("True Or False", player.SteamId));
                            return;
                        }
                    }

                    foreach (Player pl in players)
                    {
                        pl.activeStrikes = 0;

                        if (!wipeOnlyActive)
                        {
                            pl.strikes = 0;
                            pl.lastStrike = "not striked yet";
                        }
                    }

                    SaveData(ref players);

                    SendChatMessage(player, GetMsg("Wiped", player.SteamId));
                    break;

                case "wipefull":
                    if (!HasPerm(player.SteamId, "admin"))
                        return;

                    players.Clear();
                    SaveData(ref players);

                    SendChatMessage(player, GetMsg("Fully Wiped", player.SteamId));
                    break;

                default:
                    if (!HasPerm(player.SteamId, "admin"))
                        return;

                    PlayerSession strikePlayer = GetPlayer(args[0], player);

                    if (strikePlayer == null)
                        return;

                    StrikePlayer(strikePlayer, args.Length == 2 ? args[1] : "none");
                    break;
            }
        }

        #endregion

        #region Subject Related

        void StrikePlayer(PlayerSession player, string reason = "none")
        {
            Player pl = Player.Get(player);

            if (pl == null)
            {
                players.Add(new Player(player));
                SaveData(ref players);

                pl = Player.Get(player);
            }

            pl.strikes++;
            pl.activeStrikes++;
            pl.lastStrike = DateTime.Now.ToString();

            pl.Update(player);

            BroadcastChat(GetMsg("Striked", player.SteamId).Replace("{player}", player.Name).Replace("{reason}", reason));

            if (ReachedMaxStrikes(player))
                BanPlayer(player, reason);
        }

        bool ReachedMaxStrikes(PlayerSession player)
        {
            Player pl = Player.Get(player);
            int maxStrikes = GetConfig(3, "Settings", "Strikes Until Ban");

            if (pl == null)
                return false;
            else
                return (pl.activeStrikes >= maxStrikes);
        }


        void BanPlayer(PlayerSession player, string reason = "none")
        {
            if (EBS == null)
            {
                PrintError($"Failed to ban player {player.Name} ! EnhancedBanSystem was not found! It is needed for this plugin to work!");
                return;
            }

            int banTime = GetConfig(3, "Settings", "Ban Time in Seconds");

            EBS.Call("BanID", player, player.SteamId, GetMsg("Can Not Join", player.SteamId).Replace("{reason}", reason), banTime);

            BroadcastChat(GetMsg("Banned", player.SteamId).Replace("{player}", player.Name).Replace("{reason}", reason));
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        PlayerSession GetPlayer(string searchedPlayer, PlayerSession player)
        {
            foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                if (current != null && current.Name != null && current.IsLoaded && current.Name.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<PlayerSession> foundPlayers =
                (from current in GameManager.Instance.GetSessions().Values
                 where current != null && current.Name != null && current.IsLoaded && current.Name.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.Name).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    SendChatMessage(player, "Multiple matching players found: \n" + players);
                    break;
            }

            return null;
        }

        ////////////////////////////////////////
        ///     Converting
        ////////////////////////////////////////

        string ListToString<T>(List<T> list, int first, string seperator) => string.Join(seperator, (from item in list select item.ToString()).Skip(first).ToArray());

        ////////////////////////////////////////
        ///     Config Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        ////////////////////////////////////////
        ///     Data Related
        ////////////////////////////////////////

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

        void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

        ////////////////////////////////////////
        ///     Message Related
        ////////////////////////////////////////

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        ////////////////////////////////////////
        ///     Permission Related
        ////////////////////////////////////////

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            permission.RegisterPermission($"{PermissionPrefix}.{perm}", this);
        }

        bool HasPerm(object uid, params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");

            return permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}");
        }

        string PermissionPrefix
        {
            get
            {
                return this.Title.Replace(" ", "").ToLower();
            }
        }

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion
    }
}
