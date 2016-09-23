using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("StrikeSystem", "LaserHydra", "2.1.3", ResourceId = 1276)]
    [Description("Strike players & time-ban players with a specific amount of strikes")]
    class StrikeSystem : CovalencePlugin
    {
        static List<Player> Players = new List<Player>();

        [PluginReference("EnhancedBanSystem")]
        Plugin EBS;

        #region Classes

        class Player
        {
            public string steamId = "0";
            public string name = "unkown";
            public string lastStrike = "not striked yet";
            public int strikes = 0;
            public int activeStrikes = 0;

            public Player()
            {
            }

            internal Player(IPlayer player)
            {
                this.steamId = player.Id;
                this.name = player.Name;
            }

            internal void Update(IPlayer player) => this.name = player.Name;
            
            internal static Player Get(IPlayer player)
            {
                return Players.Find((p) =>
                {
                    if (p.steamId == player.Id)
                        return true;
                    else
                        return false;
                });
            }

            public override bool Equals(object obj)
            {
                if (obj is Player && ((Player) obj).steamId == this.steamId)
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
            LoadConfig();
            LoadMessages();
            LoadData(ref Players);

            RegisterPerm("admin");

            foreach (IPlayer player in players.Connected)
                if (Player.Get(player) == null)
                {
                    Players.Add(new Player(player));
                    SaveData(ref Players);
                }
                else
                    Player.Get(player).Update(player);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        new void LoadConfig()
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

        [Command("strike")]
        void cmdStrike(IPlayer player, string cmd, string[] args)
        {
            if(args.Length == 0)
            {
                if (HasPerm(player.Id, "admin"))
                    player.Reply("<color=#C4FF00>/strike <player> [reason]</color> strike player" + Environment.NewLine +
                                            "<color=#C4FF00>/strike reset <player> [only Active Strikes: true/false]</color> reset players strikes" + Environment.NewLine +
                                            "<color=#C4FF00>/strike remove <player> [amount] [only Active Strikes: true/false]</color> remove strikes of a player " + Environment.NewLine +
                                            "<color=#C4FF00>/strike info [player]</color> get strike info about a player" + Environment.NewLine +
                                            "<color=#C4FF00>/strike wipe [only Active Strikes: true/false]</color> wipe all strikes" + Environment.NewLine +
                                            "<color=#C4FF00>/strike wipefull</color> wipe all data");
                else
                    player.Reply("<color=#C4FF00>/strike info</color> get your strike info");

                return;
            }

            switch(args[0].ToLower())
            {
                case "reset":
                    if (!HasPerm(player.Id, "admin"))
                        return;

                    if(args.Length != 2 && args.Length != 3)
                    {
                        player.Reply("Syntax: /strike reset <player> [only Active Strikes: true/false]");
                        return;
                    }

                    IPlayer resetTarget = GetPlayer(args[1], player);

                    if (resetTarget == null)
                        return;

                    Player resetPl = Player.Get(resetTarget);

                    if(resetPl == null)
                    {
                        Players.Add(new Player(resetTarget));
                        SaveData(ref Players);

                        resetPl = Player.Get(resetTarget);
                    }

                    bool resetOnlyActive = true;

                    if(args.Length == 3)
                    {
                        try
                        {
                            resetOnlyActive = Convert.ToBoolean(args[2]);
                        }
                        catch(FormatException)
                        {
                            player.Reply(GetMsg("True Or False", player.Id));
                            return;
                        }
                    }

                    resetPl.activeStrikes = 0;

                    if(!resetOnlyActive)
                    {
                        resetPl.strikes = 0;
                        resetPl.lastStrike = "not striked yet";
                    }

                    SaveData(ref Players);

                    player.Reply(GetMsg("Reset", player.Id).Replace("{player}", resetTarget.Name));

                    break;

                case "remove":
                    if (!HasPerm(player.Id, "admin"))
                        return;

                    if (args.Length != 2 && args.Length != 3 && args.Length != 4)
                    {
                        player.Reply("Syntax: /strike remove <player> [amount] [only Active Strikes: true/false]");
                        return;
                    }

                    IPlayer removeTarget = GetPlayer(args[1], player);

                    if (removeTarget == null)
                        return;

                    Player removePl = Player.Get(removeTarget);

                    if (removePl == null)
                    {
                        Players.Add(new Player(removeTarget));
                        SaveData(ref Players);

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
                            player.Reply(GetMsg("Invalid Number", player.Id));
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
                            player.Reply(GetMsg("True Or False", player.Id).Replace("{arg}", "[only Active Strikes: true/false]"));
                            return;
                        }
                    }

                    removePl.activeStrikes -= removeAmount;

                    if (!removeOnlyActive)
                        removePl.strikes -= removeAmount;

                    SaveData(ref Players);

                    player.Reply(GetMsg("Removed", player.Id).Replace("{player}", removeTarget.Name).Replace("{amount}", removeAmount.ToString()));

                    break;

                case "info":
                    if (!HasPerm(player.Id, "admin"))
                    {
                        Player pl = Player.Get(player);

                        if (pl == null)
                        {
                            Players.Add(new Player(player));
                            SaveData(ref Players);

                            pl = Player.Get(player);
                        }

                        player.Reply($"<color=#C4FF00>Last Strike</color>: {pl.lastStrike}" + Environment.NewLine +
                                                $"<color=#C4FF00>Active Strikes</color>: {pl.activeStrikes}" + Environment.NewLine +
                                                $"<color=#C4FF00>Total Strikes</color>: {pl.strikes}");
                    }
                    else
                    {
                        if (args.Length != 1 && args.Length != 2)
                        {
                            player.Reply("Syntax: /strike info [player]");
                            return;
                        }

                        if(args.Length == 1)
                        {
                            Player pl = Player.Get(player);

                            if (pl == null)
                            {
                                Players.Add(new Player(player));
                                SaveData(ref Players);

                                pl = Player.Get(player);
                            }

                            player.Reply($"<color=#C4FF00>Last Strike</color>: {pl.lastStrike}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Active Strikes</color>: {pl.activeStrikes}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Total Strikes</color>: {pl.strikes}");
                        }
                        else
                        {
                            IPlayer infoTarget = GetPlayer(args[1], player);

                            if (infoTarget == null)
                                return;

                            Player infoPl = Player.Get(infoTarget);

                            if (infoPl == null)
                            {
                                Players.Add(new Player(infoTarget));
                                SaveData(ref Players);

                                infoPl = Player.Get(infoTarget);
                            }

                            player.Reply($"<color=#C4FF00>Last Strike</color>: {infoPl.lastStrike}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Active Strikes</color>: {infoPl.activeStrikes}" + Environment.NewLine +
                                                    $"<color=#C4FF00>Total Strikes</color>: {infoPl.strikes}");
                        }
                    }
                    break;

                case "wipe":
                    if (!HasPerm(player.Id, "admin"))
                        return;

                    if (args.Length != 1 && args.Length != 2)
                    {
                        player.Reply("Syntax: /strike wipe [only Active Strikes: true/false]");
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
                            player.Reply(GetMsg("True Or False", player.Id));
                            return;
                        }
                    }

                    foreach (Player pl in Players)
                    {
                        pl.activeStrikes = 0;
                        
                        if(!wipeOnlyActive)
                        {
                            pl.strikes = 0;
                            pl.lastStrike = "not striked yet";
                        }
                    }

                    SaveData(ref Players);

                    player.Reply(GetMsg("Wiped", player.Id));
                    break;

                case "wipefull":
                    if (!HasPerm(player.Id, "admin"))
                        return;

                    Players.Clear();
                    SaveData(ref Players);

                    player.Reply(GetMsg("Fully Wiped", player.Id));
                    break;

                default:
                    if (!HasPerm(player.Id, "admin"))
                        return;

                    IPlayer strikePlayer = GetPlayer(args[0], player);

                    if (strikePlayer == null)
                        return;

                    StrikePlayer(strikePlayer, args.Length == 2 ? args[1] : "none");
                    break;
            }
        }

        #endregion

        #region Subject Related

        void StrikePlayer(IPlayer player, string reason = "none")
        {
            Player pl = Player.Get(player);

            if (pl == null)
            {
                Players.Add(new Player(player));
                SaveData(ref Players);

                pl = Player.Get(player);
            }

            pl.strikes++;
            pl.activeStrikes++;
            pl.lastStrike = DateTime.Now.ToString();
            
            pl.Update(player);

            server.Broadcast(GetMsg("Striked", player.Id).Replace("{player}", player.Name).Replace("{reason}", reason));

            if (ReachedMaxStrikes(player))
                BanPlayer(player, reason);
        }

        bool ReachedMaxStrikes(IPlayer player)
        {
            Player pl = Player.Get(player);
            int maxStrikes = GetConfig(3, "Settings", "Strikes Until Ban");

            if (pl == null)
                return false;
            else
                return (pl.activeStrikes >= maxStrikes);
        }

        
        void BanPlayer(IPlayer player, string reason = "none")
        {
            if (EBS == null)
            {
                PrintError($"Failed to ban player {player.Name} ! EnhancedBanSystem was not found! It is needed for this plugin to work!");
                return;
            }

            int banTime = GetConfig(3, "Settings", "Ban Time in Seconds");

            EBS.Call("Ban", player, player.Id, GetMsg("Can Not Join", player.Id).Replace("{reason}", reason), banTime);

            server.Broadcast(GetMsg("Banned", player.Id).Replace("{player}", player.Name).Replace("{reason}", reason));
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        IPlayer GetPlayer(string searchedPlayer, IPlayer player)
        {
            foreach (IPlayer current in players.Connected)
                if (current.Name.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<IPlayer> foundPlayers =
                (from current in players.Connected
                 where current.Name.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    player.Reply("The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.Name).ToList();
                    string players = ListToString(playerNames, 0, ", ");
                    player.Reply("Multiple matching players found: \n" + players);
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

        #endregion
    }
}
