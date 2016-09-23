using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("AdminLeaveJoin", "austinv900", "0.1.8", ResourceId = 2032)]
    [Description("Custom Join and Leave Message when admins leave and join")]
    class AdminLeaveJoin : CovalencePlugin
    {
        #region Libraries
        Dictionary<IPlayer, string> ActiveStaffList = new Dictionary<IPlayer, string>();
        #endregion

        #region Oxide Hooks
        void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(Name + "." + AdminPermission, this);
            permission.RegisterPermission(Name + "." + ModeratorPermission, this);
            UpdateList();
        }

        void OnServerSave()
        {
            UpdateList();
        }

        void OnUserConnected(IPlayer player)
        {
            if (IsAdmin(player) || IsModerator(player)) { UpdateList(); JoinLeave(player, true); }
        }

        void OnUserDisconnected(IPlayer player)
        {
            if (IsAdmin(player) || IsModerator(player)) { UpdateList(); JoinLeave(player); }
        }
        #endregion

        #region Configuration

        bool showAdminJoin;
        bool showAdminLeave;
        bool showModJoin;
        bool showModLeave;
        string ModeratorGroup;
        string AdminGroup;
        string AdminPermission;
        string ModeratorPermission;
        string msgPrefix;

        protected override void LoadDefaultConfig()
        {
            SetConfig("General", "Admin", "ShowJoin", true);
            SetConfig("General", "Admin", "ShowLeave", true);
            SetConfig("General", "Admin", "OxideGroup", "admin");
            SetConfig("General", "Admin", "Permission(adminleavejoin.?)", "admin");
            SetConfig("General", "Moderator", "ShowJoin", true);
            SetConfig("General", "Moderator", "ShowLeave", true);
            SetConfig("General", "Moderator", "OxideGroup", "moderator");
            SetConfig("General", "Moderator", "Permission(adminleavejoin.?)", "moderator");
            SetConfig("Settings", "Chat", "Prefix", "AdminJoin");
            SetConfig("Settings", "Chat", "Color", "#005682");
            SaveConfig();

            showAdminJoin = GetConfig(true, "General", "Admin", "ShowJoin");
            showAdminLeave = GetConfig(true, "General", "Admin", "ShowLeave");
            AdminGroup = GetConfig("admin", "General", "Admin", "OxideGroup");
            showModJoin = GetConfig(true, "General", "Moderator", "ShowJoin");
            showModLeave = GetConfig(true, "General", "Moderator", "ShowLeave");
            ModeratorGroup = GetConfig("moderator", "General", "Moderator", "OxideGroup");
            AdminPermission = GetConfig("admin", "General", "Admin", "Permission(adminleavejoin.?)");
            ModeratorPermission = GetConfig("moderator", "General", "Moderator", "Permission(adminleavejoin.?)");

            msgPrefix = GetConfig("AdminJoin", "Settings", "Chat", "Prefix");
        }
        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Online"] = "{0} {1} has come Online",
                ["Offline"] = "{0} {1} has just went Offline",
                ["Admin"] = "Admin",
                ["Moderator"] = "Moderator",
                ["NoActiveStaff"] = "No staff currently online",
                ["ChatFormat"] = "[{0}] : {1}",
                ["AdminListHeader"] = "Active Staff List:",
                ["AdminListValues"] = "{0} - {1}"
            }, this, "en");
        }

        #endregion

        #region ChatCommands
        [Command("staff")]
        void cmdAvailable(IPlayer player, string command, string[] args)
        {
            if (ActiveStaffList.Count == 0) { player.Reply(MessageFormat(Lang("NoActiveStaff", player.Id), player.Id)); return; };
            player.Reply(Lang("AdminListHeader", player.Id));
            foreach(var admin in ActiveStaffList.Keys)
            {
                player.Reply(Lang("AdminListValues", player.Id, admin.Name, ActiveStaffList[admin]));
            }
        }

        #endregion

        #region Plugin Hooks

        void UpdateList()
        {
            ActiveStaffList.Clear();
            foreach(var pl in players.Connected)
            {
                if (IsAdmin(pl) && !ActiveStaffList.ContainsKey(pl)) ActiveStaffList.Add(pl, "Admin");
                if (IsModerator(pl) && !ActiveStaffList.ContainsKey(pl)) ActiveStaffList.Add(pl, "Moderator");
            }
        }
        [Command("tests")]
        void ccmdtest(IPlayer player, string command, string[] args) => JoinLeave(player, true);

        void JoinLeave(IPlayer player, bool Join = false)
        {
            string Rank = "Moderator";
            string join = "Offline";

            if (IsAdmin(player)) Rank = "Admin";
            if (Join) { join = "Online"; if (!ActiveStaffList.ContainsKey(player)) ActiveStaffList.Add(player, Rank); }

            MessageBroadcast(player.Name, Rank, join);

        }

        void MessageBroadcast(string name, string rank, string status)
        {
            if (rank == "Moderator" && status == "Offline" && !showModLeave) return;
            if (rank == "Moderator" && status == "Online" && !showModJoin) return;
            if (rank == "Admin" && status == "Offline" && !showAdminLeave) return;
            if (rank == "Admin" && status == "Online" && !showAdminJoin) return;
            foreach (var pl in players.Connected)
            {
                string message = MessageFormat(Lang(status, pl.Id, name, rank), pl.Id);
                pl.Reply(message);
            }
        }
        #endregion

        #region Helpers
        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from val in list select val.ToString()).Skip(first).ToArray());
        void SetConfig(params object[] args) { List<string> stringArgs = (from arg in args select arg.ToString()).ToList(); stringArgs.RemoveAt(args.Length - 1); if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args); }
        T GetConfig<T>(T defaultVal, params object[] args) { List<string> stringArgs = (from arg in args select arg.ToString()).ToList(); if (Config.Get(stringArgs.ToArray()) == null) { PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin."); return defaultVal; } return (T)System.Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T)); }

        bool IsAdmin(IPlayer player) => permission.UserHasPermission(player.Id, Name + "." + AdminPermission) || permission.UserHasGroup(player.Id, AdminGroup) || player.IsAdmin;

        bool IsModerator(IPlayer player) => permission.UserHasPermission(player.Id, Name + "." + ModeratorPermission) || permission.UserHasGroup(player.Id, ModeratorGroup);

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        string MessageFormat(string msg, string id) => Lang("ChatFormat", id, msgPrefix, msg);
        #endregion
    }
}
