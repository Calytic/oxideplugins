using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Timed Permissions", "LaserHydra", "1.1.5", ResourceId = 1705)]
    [Description("Allows you to grant permissions for a specific time")]
    class TimedPermissions : RustPlugin
    {
        static TimedPermissions Instance = null;
        static List<Player> players = new List<Player>();

        #region Classes

        class Player
        {
            PluginTimers timer = new PluginTimers(Interface.Oxide.RootPluginManager.GetPlugin("TimedPermissions"));
            public List<TimedPermission> permissions = new List<TimedPermission>();
            public List<TimedGroup> groups = new List<TimedGroup>();
            public string name = "unknown";
            public ulong steamID = 0;

            public Player()
            {
                timer.Repeat(60, 0, () => Update());
            }

            internal Player(BasePlayer player)
            {
                steamID = player.userID;
                name = player.displayName;

                timer.Repeat(60, 0, () => Update());
            }

            internal Player(ulong steamID)
            {
                this.steamID = steamID;

                timer.Repeat(1, 0, () => Update());
            }

            internal static Player Get(BasePlayer player) => players.Find((p) => p.steamID == player.userID);

            internal static Player Get(ulong steamID) => players.Find((p) => p.steamID == steamID);

            internal void AddPermission(string permission, DateTime expireDate)
            {
                permissions.Add(new TimedPermission(permission, expireDate));
                GrantPerm(steamID, permission);

                Instance.Puts($"----> {name} ({steamID}) - Permission Granted: {permission} for {expireDate - DateTime.Now}" + Environment.NewLine);

                SaveData(ref players);
            }

            internal void RemovePermission(string permission)
            {
                permissions.Remove(TimedPermission.Get(permission, this));
                RevokePerm(steamID, permission);
                
                Instance.Puts($"----> {name} ({steamID}) - Permission Expired: {permission}" + Environment.NewLine);

                SaveData(ref players);
            }

            internal void AddGroup(string group, DateTime expireDate)
            {
                groups.Add(new TimedGroup(group, expireDate));
                AddToGroup(steamID, group);

                Instance.Puts($"----> {name} ({steamID}) - Added to Group: {group} for {expireDate - DateTime.Now}" + Environment.NewLine);

                SaveData(ref players);
            }

            internal void RemoveGroup(string group)
            {
                groups.Remove(TimedGroup.Get(group, this));
                RemoveFromGroup(steamID, group);
                
                Instance.Puts($"----> {name} ({steamID}) - Group Expired: {group}" + Environment.NewLine);

                SaveData(ref players);
            }

            internal void UpdatePlayer(BasePlayer player) => name = player.displayName;
            
            internal void Update()
            {
                foreach (TimedPermission perm in CopyList(permissions))
                    if (perm.Expired)
                        RemovePermission(perm.permission);

                foreach (TimedGroup group in CopyList(groups))
                    if (group.Expired)
                        RemoveGroup(group.group);
            }

            List<T> CopyList<T>(List<T> list)
            {
                T[] array = new T[list.Count];
                list.CopyTo(array);

                return array.ToList();
            }

            public override bool Equals(object obj)
            {
                if(obj is Player)
                    return ((Player) obj).steamID == steamID;

                return false;
            }

            public override int GetHashCode() => steamID.GetHashCode();
        }

        class TimedPermission
        {
            public string permission = string.Empty;
            public string _expireDate = "00/00/00/00/0000";

            internal DateTime expireDate
            {
                get
                {
                    int[] date = (from val in _expireDate.Split('/') select Convert.ToInt32(val)).ToArray();
                    return new DateTime(date[4], date[3], date[2], date[1], date[0], 0);
                }
                set
                {
                    _expireDate = $"{value.Minute}/{value.Hour}/{value.Day}/{value.Month}/{value.Year}";
                }
            }

            internal bool Expired
            {
                get
                {
                    return DateTime.Compare(DateTime.Now, expireDate) > 0;
                }
            }

            public TimedPermission()
            {
            }

            internal TimedPermission(string permission, DateTime expireDate)
            {
                this.permission = permission;
                this.expireDate = expireDate;
            }

            internal static TimedPermission Get(string permission, Player player) => player.permissions.Find((p) => p.permission == permission);

            public override bool Equals(object obj) => ((TimedPermission) obj).permission == permission;

            public override int GetHashCode() => permission.GetHashCode();
        }

        class TimedGroup
        {
            public string group = string.Empty;
            public string _expireDate = "00/00/00/00/0000";

            internal DateTime expireDate
            {
                get
                {
                    int[] date = (from val in _expireDate.Split('/') select Convert.ToInt32(val)).ToArray();
                    return new DateTime(date[4], date[3], date[2], date[1], date[0], 0);
                }
                set
                {
                    _expireDate = $"{value.Minute}/{value.Hour}/{value.Day}/{value.Month}/{value.Year}";
                }
            }

            internal bool Expired
            {
                get
                {
                    return DateTime.Compare(DateTime.Now, expireDate) > 0;
                }
            }

            public TimedGroup()
            {
            }

            internal TimedGroup(string group, DateTime expireDate)
            {
                this.group = group;
                this.expireDate = expireDate;
            }

            internal static TimedGroup Get(string group, Player player) => player.groups.Find((p) => p.group == group);

            public override bool Equals(object obj) => ((TimedGroup) obj).group == group;

            public override int GetHashCode() => group.GetHashCode();
        }

        #endregion

        #region Plugin General

        ////////////////////////////////////////
        ///     Plugin Related Hooks
        ////////////////////////////////////////

        void Loaded()
        {
#if !RUST
            throw new NotSupportedException("This plugin or the version of this plugin does not support this game!");
#endif

            Instance = this;

            RegisterPerm("use");

            LoadMessages();
            LoadData(ref players);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (Player.Get(player) == null)
                    players.Add(new Player(player));

            SaveData(ref players);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (Player.Get(player) == null)
                players.Add(new Player(player));

            SaveData(ref players);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Invalid Time Format", "Invalid Time Format: Ex: 1d12h30m | d = days, h = hours, m = minutes"}
            }, this);
        }
        
        #endregion

        #region Commands

        [ConsoleCommand("grantperm")]
        void ccmdGrantPerm(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer) arg?.connection?.player ?? null;
            string[] args = arg.HasArgs() ? arg.Args : new string[0];

            cmdGrantPerm(player, arg.cmd.name, args);
        }

        [ChatCommand("grantperm")]
        void cmdGrantPerm(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            if (args.Length != 3)
            {
                SendChatMessage(player, $"Syntax: {(player == null ? string.Empty : "/")}grantperm <player|steamid> <permission> <time Ex: 1d12h30m>");
                return;
            }
            
            ulong steamID = 0;
            BasePlayer target = null;
            string permission = args[1];
            DateTime expireDate;

            if(!TryConvert(args[0], out steamID))
                target = GetPlayer(args[0], player);

            if (steamID == 0 && target == null)
                return;

            if (!TryGetDateTime(args[2], out expireDate))
            {
                SendChatMessage(player, GetMsg("Invalid Time Format", player?.userID ?? null));
                return;
            }

            if (target != null)
            {
                if (Player.Get(target) == null)
                    players.Add(new Player(target));

                Player.Get(target).AddPermission(permission, expireDate);
            }
            else if (steamID != 0)
            {
                if (Player.Get(steamID) == null)
                    players.Add(new Player(steamID));

                Player.Get(steamID).AddPermission(permission, expireDate);
            }
        }

        [ConsoleCommand("addgroup")]
        void ccmdAddGroup(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer) arg?.connection?.player ?? null;
            string[] args = arg.HasArgs() ? arg.Args : new string[0];

            cmdAddGroup(player, arg.cmd.name, args);
        }

        [ChatCommand("addgroup")]
        void cmdAddGroup(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "use"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.userID));
                return;
            }

            if (args.Length != 3)
            {
                SendChatMessage(player, $"Syntax: {(player == null ? string.Empty : "/")}addgroup <player|steamid> <group> <time Ex: 1d12h30m>");
                return;
            }

            ulong steamID = 0;
            BasePlayer target = null;
            string group = args[1];
            DateTime expireDate;

            if (!TryConvert(args[0], out steamID))
                target = GetPlayer(args[0], player);

            if (steamID == 0 && target == null)
                return;

            if (!TryGetDateTime(args[2], out expireDate))
            {
                SendChatMessage(player, GetMsg("Invalid Time Format", player?.userID ?? null));
                return;
            }

            if (target != null)
            {
                if (Player.Get(target) == null)
                    players.Add(new Player(target));

                Player.Get(target).AddGroup(group, expireDate);
            }
            else if (steamID != 0)
            {
                if (Player.Get(steamID) == null)
                    players.Add(new Player(steamID));

                Player.Get(steamID).AddGroup(group, expireDate);
            }
        }

        #endregion

        #region Subject Related

        bool TryGetDateTime(string source, out DateTime date)
        {
            int minutes = 0;
            int hours = 0;
            int days = 0;

            Match m = new Regex(@"(\d+?)m", RegexOptions.IgnoreCase).Match(source);
            Match h = new Regex(@"(\d+?)h", RegexOptions.IgnoreCase).Match(source);
            Match d = new Regex(@"(\d+?)d", RegexOptions.IgnoreCase).Match(source);

            if (m.Success)
                minutes = Convert.ToInt32(m.Groups[1].ToString());

            if (h.Success)
                hours = Convert.ToInt32(h.Groups[1].ToString());

            if (d.Success)
                days = Convert.ToInt32(d.Groups[1].ToString());

            source = source.Replace(minutes.ToString() + "m", string.Empty);
            source = source.Replace(hours.ToString() + "h", string.Empty);
            source = source.Replace(days.ToString() + "d", string.Empty);

            if (!string.IsNullOrEmpty(source) || (!m.Success && !h.Success && !d.Success))
            {
                date = default(DateTime);
                return false;
            }

            date = DateTime.Now + new TimeSpan(days, hours, minutes, 0);
            return true;
        }

        #endregion

        #region General Methods

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer player)
        {
            foreach (BasePlayer current in BasePlayer.activePlayerList)
                if (current.displayName.ToLower() == searchedPlayer.ToLower())
                    return current;

            List<BasePlayer> foundPlayers =
                (from current in BasePlayer.activePlayerList
                 where current.displayName.ToLower().Contains(searchedPlayer.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, "The player can not be found.");
                    break;

                case 1:
                    return foundPlayers[0];

                default:
                    List<string> playerNames = (from current in foundPlayers select current.displayName).ToList();
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

        bool TryConvert<S, C>(S source, out C converted)
        {
            try
            {
                converted = (C) Convert.ChangeType(source, typeof(C));
                return true;
            }
            catch (Exception)
            {
                converted = default(C);
                return false;
            }
        }

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

        void LoadData<T>(ref T data, string filename = "TimedPermissions") => data = Interface.Oxide.DataFileSystem.ReadObject<T>(filename);

        static void SaveData<T>(ref T data, string filename = "TimedPermissions") => Interface.Oxide.DataFileSystem.WriteObject(filename, data);

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

        static void GrantPerm(object uid, string permission) => ConsoleSystem.Run.Server.Normal("oxide.grant user", uid.ToString(), permission);

        static void RevokePerm(object uid, string permission) => ConsoleSystem.Run.Server.Normal("oxide.revoke user", uid.ToString(), permission);

        static void AddToGroup(object uid, string permission) => ConsoleSystem.Run.Server.Normal("oxide.usergroup add", uid.ToString(), permission);

        static void RemoveFromGroup(object uid, string permission) => ConsoleSystem.Run.Server.Normal("oxide.usergroup remove", uid.ToString(), permission);

        ////////////////////////////////////////
        ///     Messaging
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if (player == null)
                Puts(msg == null ? prefix : msg);
            else
                rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
        }

        static void Print(string message) => ConsoleSystem.Run.Server.Normal($"echo {message}");

        #endregion
    }
}
