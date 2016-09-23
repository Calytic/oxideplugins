using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Timed Permissions", "LaserHydra", "1.2.6", ResourceId = 1926)]
    [Description("Allows you to grant permissions or groups for a specific time")]
    class TimedPermissions : CovalencePlugin
    {
        static TimedPermissions Instance = null;
        static List<Player> _players = new List<Player>();

        #region Classes

        class Player
        {
            PluginTimers timer = new PluginTimers(Interface.Oxide.RootPluginManager.GetPlugin("TimedPermissions"));
            public List<TimedPermission> permissions = new List<TimedPermission>();
            public List<TimedGroup> groups = new List<TimedGroup>();
            public string name = "unknown";
            public string steamID = "0";

            public Player()
            {
                timer.Repeat(60, 0, () => Update());
            }

            internal Player(IPlayer player)
            {
                steamID = player.Id;
                name = player.Name;

                timer.Repeat(60, 0, () => Update());
            }

            internal Player(string steamID)
            {
                this.steamID = steamID;

                timer.Repeat(1, 0, () => Update());
            }

            internal static Player Get(IPlayer player) => Get(player.Id);

            internal static Player Get(string steamID) => _players.Find((p) => p.steamID == steamID);

            internal static Player GetOrCreate(IPlayer player)
            {
                Player pl = Get(player);

                if (pl == null)
                {
                    pl = new Player(player);

                    _players.Add(pl);
                    SaveData(ref _players);
                }

                return pl;
            }

            internal static Player GetOrCreate(string steamID)
            {
                Player pl = Get(steamID);

                if (pl == null)
                {
                    pl = new Player(steamID);

                    _players.Add(pl);
                    SaveData(ref _players);
                }

                return pl;
            }

            internal void AddPermission(string permission, DateTime expireDate)
            {
                permissions.Add(new TimedPermission(permission, expireDate));
                Instance.permission.GrantUserPermission(steamID, permission, null);

                Instance.Puts($"----> {name} ({steamID}) - Permission Granted: {permission} for {expireDate - DateTime.Now}" + Environment.NewLine);

                SaveData(ref _players);
            }

            internal void RemovePermission(string permission)
            {
                permissions.Remove(TimedPermission.Get(permission, this));
                Instance.permission.RevokeUserPermission(steamID, permission);
                
                Instance.Puts($"----> {name} ({steamID}) - Permission Expired: {permission}" + Environment.NewLine);

                if (groups.Count == 0 && permissions.Count == 0)
                    _players.Remove(this);

                SaveData(ref _players);
            }

            internal void AddGroup(string group, DateTime expireDate)
            {
                groups.Add(new TimedGroup(group, expireDate));
                Instance.permission.AddUserGroup(steamID, group);

                Instance.Puts($"----> {name} ({steamID}) - Added to Group: {group} for {expireDate - DateTime.Now}" + Environment.NewLine);

                SaveData(ref _players);
            }

            internal void RemoveGroup(string group)
            {
                groups.Remove(TimedGroup.Get(group, this));
                Instance.permission.RemoveUserGroup(steamID, group);
                
                Instance.Puts($"----> {name} ({steamID}) - Group Expired: {group}" + Environment.NewLine);

                if (groups.Count == 0 && permissions.Count == 0)
                    _players.Remove(this);

                SaveData(ref _players);
            }

            internal void UpdatePlayer(IPlayer player) => name = player.Name;
            
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
            Instance = this;

            LoadMessages();
            LoadData(ref _players);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Invalid Time Format", "Invalid Time Format: Ex: 1d12h30m | d = days, h = hours, m = minutes"},
                {"Player Has No Info", "There is no info about this player."},
                {"Player Info", $"Info about <color=#C4FF00>{{player}}</color>:{Environment.NewLine}<color=#C4FF00>Groups</color>: {{groups}}{Environment.NewLine}<color=#C4FF00>Permissions</color>: {{permissions}}"}
            }, this);
        }
        
        #endregion

        #region Commands

        [Command("grantperm", "global.grantperm"), Permission("timedpermissions.use")]
        void cmdGrantPerm(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply($"Syntax: {(player.LastCommand == CommandType.Console ? string.Empty : "/")}grantperm <player|steamid> <permission> <time Ex: 1d12h30m>");
                return;
            }
            
            ulong steamID = 0;
            IPlayer target = null;
            string permission = args[1];
            DateTime expireDate;

            if(!TryConvert(args[0], out steamID))
                target = GetPlayer(args[0], player);

            if (steamID == 0 && target == null)
                return;

            if (!TryGetDateTime(args[2], out expireDate))
            {
                player.Reply(GetMsg("Invalid Time Format", player?.Id ?? null));
                return;
            }

            if (target != null)
            {
                if (Player.GetOrCreate(target) == null)
                    _players.Add(new Player(target));

                Player.GetOrCreate(target).AddPermission(permission, expireDate);
            }
            else if (steamID != 0)
            {
                if (Player.GetOrCreate(steamID.ToString()) == null)
                    _players.Add(new Player(steamID.ToString()));

                Player.GetOrCreate(steamID.ToString()).AddPermission(permission, expireDate);
            }
        }

        [Command("addgroup", "global.addgroup"), Permission("timedpermissions.use")]
        void cmdAddGroup(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 3)
            {
                player.Reply($"Syntax: {(player == null ? string.Empty : "/")}addgroup <player|steamid> <group> <time Ex: 1d12h30m>");
                return;
            }

            ulong steamID = 0;
            IPlayer target = null;
            string group = args[1];
            DateTime expireDate;

            if (!TryConvert(args[0], out steamID))
                target = GetPlayer(args[0], player);

            if (steamID == 0 && target == null)
                return;

            if (!TryGetDateTime(args[2], out expireDate))
            {
                player.Reply(GetMsg("Invalid Time Format", player?.Id ?? null));
                return;
            }

            if (target != null)
            {
                if (Player.GetOrCreate(target) == null)
                    _players.Add(new Player(target));

                Player.GetOrCreate(target).AddGroup(group, expireDate);
            }
            else if (steamID != 0)
            {
                if (Player.GetOrCreate(steamID.ToString()) == null)
                    _players.Add(new Player(steamID.ToString()));

                Player.GetOrCreate(steamID.ToString()).AddGroup(group, expireDate);
            }
        }

        [Command("pinfo", "global.pinfo"), Permission("timedpermissions.use")]
        void cmdPlayerInfo(IPlayer player, string cmd, string[] args)
        {
            if (args.Length != 1)
            {
                player.Reply($"Syntax: {(player == null ? string.Empty : "/")}pinfo <player|steamid>");
                return;
            }

            ulong steamID = 0;
            IPlayer target = null;

            if (!TryConvert(args[0], out steamID))
                target = GetPlayer(args[0], player);

            if (steamID == 0 && target == null)
                return;

            if (target != null)
            {
                if (Player.GetOrCreate(target) == null)
                    _players.Add(new Player(target));

                Player pl = Player.Get(target);

                if (pl == null)
                    player.Reply(GetMsg("Player Has No Info"));
                else
                {
                    string msg = GetMsg("Player Info");
                    
                    msg = msg.Replace("{player}", $"{pl.name} ({pl.steamID})");
                    msg = msg.Replace("{groups}", string.Join(", ", (from g in pl.groups select $"{g.@group} until {g.expireDate}").ToArray()));
                    msg = msg.Replace("{permissions}", string.Join(", ", (from p in pl.permissions select $"{p.permission} until {p.expireDate}").ToArray()));

                    player.Reply(msg);
                }
            }
            else if (steamID != 0)
            {
                if (Player.GetOrCreate(steamID.ToString()) == null)
                    _players.Add(new Player(steamID.ToString()));

                Player pl = Player.Get(steamID.ToString());

                if (pl == null)
                    player.Reply(GetMsg("Player Has No Info"));
                else
                {
                    string msg = GetMsg("Player info");

                    msg = msg.Replace("{player}", $"{pl.name} ({pl.steamID})");
                    msg = msg.Replace("{groups}", string.Join(", ", (from g in pl.groups select $"{g.@group} until {g.expireDate}").ToArray()));
                    msg = msg.Replace("{permissions}", string.Join(", ", (from p in pl.permissions select $"{p.permission} until {p.expireDate}").ToArray()));
                    
                    player.Reply(msg);
                }
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

        #endregion
    }
}
