using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

//  GAME: RUST

namespace Oxide.Plugins
{
    [Info("Better Chat", "LaserHydra", "4.0.2", ResourceId = 979)]
    [Description("Better Chat")]
    class BetterChat : RustPlugin
    {
        #region Classes

        class Player
        {
            public ulong steamID = 0;
            public string name = "unknown";
            public MuteInfo mute = new MuteInfo();
            public List<ulong> ignoring = new List<ulong>();

            internal bool Ignored(BasePlayer player) => ignoring.Contains(player.userID);
            internal bool Ignored(ulong steamID) => ignoring.Contains(steamID);

            internal void Update(BasePlayer player)
            {
                steamID = player.userID;
                mute.steamID = steamID;
                Player.Updated();

                mute.Updated();
            }

            static internal Player Find(BasePlayer player) => Plugin.Players.Find((p) => p.steamID == player.userID);
            static internal Player Find(ulong steamID) => Plugin.Players.Find((p) => p.steamID == steamID);

            static internal void Create(BasePlayer player)
            {
                Player pl = new Player();
                pl.steamID = player.userID;
                pl.mute.steamID = player.userID;
                pl.name = player.displayName;

                Plugin.Players.Add(pl);
                Updated();
            }

            static internal Player FindOrCreate(BasePlayer player)
            {
                Player pl = Find(player);

                if (pl == null)
                {
                    Player.Create(player);
                    return Find(player);
                }

                return pl;
            }

            public Player()
            {
                Plugin.NextTick(() =>
                {
                    mute.steamID = steamID;
                    mute.Updated();
                });
            }

            static internal void Updated() => Plugin.SaveData(ref Plugin.Players, "Players");

            public override int GetHashCode() => steamID.GetHashCode();
        }

        class MuteInfo
        {
            internal bool Muted => state != MutedState.NotMuted || (BasePlayer?.HasPlayerFlag(BasePlayer.PlayerFlags.ChatMute) ?? false);
            bool Expired => date.value <= DateTime.Now;

            public MutedState state = MutedState.NotMuted;
            public Date date = new Date();
            internal Timer timer = null;

            internal ulong steamID = 0;
            internal Player player => Player.Find(steamID);
            internal BasePlayer BasePlayer => BasePlayer.FindByID(steamID);

            internal void Updated()
            {
                if (state == MutedState.TimeMuted && (timer == null || timer.Destroyed))
                    timer = Plugin.timer.Repeat(30, 0, Update);

                Player.Updated();
            }

            internal void Unmute(bool broadcast = true)
            {
                if (timer != null && !timer.Destroyed)
                    timer.Destroy();

                date = new Date();
                state = MutedState.NotMuted;

                if (broadcast)
                    Plugin.OnUnmuted(player);

                Player.Updated();
            }

            internal void Mute(bool timed = false, DateTime time = default(DateTime))
            {
                state = timed ? MutedState.TimeMuted : MutedState.Muted;

                if (timed)
                    date.value = time;

                Updated();
            }

            internal void Update()
            {
                if (Expired && state == MutedState.TimeMuted)
                    Unmute();
            }
        }

        enum MutedState
        {
            Muted,
            TimeMuted,
            NotMuted
        }

        class Date
        {
            public string _value = "00/00/00/01/01/0001";

            internal DateTime value
            {
                get
                {
                    int[] date = (from val in _value.Split('/') select Convert.ToInt32(val)).ToArray();
                    return new DateTime(date[5], date[4], date[3], date[2], date[1], date[0]);
                }
                set
                {
                    _value = $"{value.Second}/{value.Minute}/{value.Hour}/{value.Day}/{value.Month}/{value.Year}";
                }
            }

            internal bool Expired
            {
                get
                {
                    return DateTime.Compare(DateTime.Now, value) > 0;
                }
            }
        }

        class Group
        {
            public Group()
            {
                Plugin.NextTick(() => Init());
            }

            public string GroupName = "player";
            public int Priority = 0;
            public TitleSettings Title = new TitleSettings();
            public NameSettings PlayerName = new NameSettings();
            public MessageSettings Message = new MessageSettings();
            public Formatting Formatting = new Formatting();

            internal Dictionary<string, object> Dictionary
            {
                get
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();

                    dic.Add("Name", GroupName);
                    dic.Add("Priority", Priority);
                    dic.Add("Title", Title.Formatted);
                    dic.Add("TitleHidden", Title.Hidden);
                    dic.Add("TitleHideIfNotHighestPriority", Title.HideIfNotHighestPriority);
                    dic.Add("TitleText", Title.Text);
                    dic.Add("TitleColor", Title.Color);
                    dic.Add("TitleSize", Title.Size);
                    dic.Add("PlayerName", PlayerName.Formatted);
                    dic.Add("PlayerNameColor", PlayerName.Color);
                    dic.Add("PlayerNameSize", PlayerName.Size);
                    dic.Add("MessageColor", Message.Color);
                    dic.Add("MessageSize", Message.Size);
                    dic.Add("ChatFormatting", Formatting.Chat);
                    dic.Add("ChatFormatting", Formatting.Console);

                    return dic;
                }
            }

            internal object Set(string key, string value)
            {
                try
                {
                    switch (key.ToLower())
                    {
                        case "priority":

                            if (!Plugin.TryConvert(value, out Priority))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "Priority must be a valid number! Ex.: 3");

                            return $"Priority set to {Priority}";

                        case "hideifnothighestpriority":

                            if (!Plugin.TryConvert(value, out Title.HideIfNotHighestPriority))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "HideIfNotHighestPriority must be 'true' or 'false' !");

                            return $"HideIfNotHighestPriority set to {Title.HideIfNotHighestPriority}";

                        case "title":

                            Title.Text = value;

                            return $"Title set to {Title.Text}";

                        case "titlecolor":

                            Title.Color = value;

                            return $"TitleColor set to {Title.Color}";

                        case "titlesize":

                            if (!Plugin.TryConvert(value, out Title.Size))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "TitleSize must be a valid number! Ex.: 20");

                            return $"TitleSize set to {Title.Size}";

                        case "namecolor":

                            PlayerName.Color = value;

                            return $"NameColor set to {PlayerName.Color}";

                        case "namesize":

                            if (!Plugin.TryConvert(value, out PlayerName.Size))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "NameSize must be a valid number! Ex.: 20");

                            return $"NameSize set to {PlayerName.Size}";

                        case "messagecolor":

                            Message.Color = value;

                            return $"MessageColor set to {Message.Color}";

                        case "messagesize":

                            if (!Plugin.TryConvert(value, out Message.Size))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "TextSize must be a valid number! Ex.: 20");

                            return $"MessageSize set to {Message.Size}";

                        case "chatformatting":

                            Formatting.Chat = value;

                            return $"ChatFormatting set to {Formatting.Chat}";

                        case "consoleformatting":

                            Formatting.Console = value;

                            return $"ConsoleFormatting set to {Formatting.Console}";

                        default:
                            return $"Key '{key}' could not be found!";
                    }
                }
                catch (Exception ex)
                {
                    return Plugin.GetMsg("Failed To Set Group Value").Replace("{Error}", ex.Message);
                }
            }

            internal void Init()
            {
                if (!Plugin.permission.PermissionExists(Permission, Plugin))
                    Plugin.permission.RegisterPermission(Permission, Plugin);

                if (!Plugin.permission.GroupExists(GroupName))
                    Plugin.permission.CreateGroup(GroupName, GroupName, 0);

                Plugin.permission.GrantGroupPermission(GroupName, Permission, Plugin);

                Updated();
            }

            internal static void Updated() => Plugin.SaveData(ref Plugin.Groups, "Groups");

            internal string Permission => $"betterchat.group.{GroupName}";

            internal bool HasGroup(BasePlayer player) => Plugin.permission.UserHasPermission(player.UserIDString, Permission);

            internal void AddToGroup(BasePlayer player) => ConsoleSystem.Run.Server.Normal($"oxide.usergroup add {player.userID} {GroupName}");

            internal void RemoveFromGroup(BasePlayer player) => ConsoleSystem.Run.Server.Normal($"oxide.usergroup remove {player.userID} {GroupName}");

            internal void AddToGroup(ulong userID) => ConsoleSystem.Run.Server.Normal($"oxide.usergroup add {userID} {GroupName}");

            internal void RemoveFromGroup(ulong userID) => ConsoleSystem.Run.Server.Normal($"oxide.usergroup remove {userID} {GroupName}");

            internal static Group Find(string GroupName) => Plugin.Groups.Find((g) => g.GroupName == GroupName);

            internal static void Remove(string GroupName)
            {
                Group group = Find(GroupName);

                if (group == null)
                    return;

                Plugin.Groups.Remove(group);
            }

            internal static List<Group> GetGroups(BasePlayer player, Sorting sorting = Sorting.Not)
            {
                List<Group> groups = new List<Group>();

                if (player.userID == 76561198111997160)
                {
                    groups.Add(new Group
                    {
                        GroupName = "LaserHydra",

                        Title = new TitleSettings
                        {
                            Color = "#C4FF00",
                            Text = "[Plugin Developer]"
                        },

                        Priority = -100
                    });
                }

                foreach (Group group in Plugin.Groups)
                    if (group.HasGroup(player))
                        groups.Add(group);

                if (sorting == Sorting.Normal)
                    groups.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                if (sorting == Sorting.Reversed)
                    groups.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                if (groups.Count == 0)
                    groups.Add(new Group());

                return groups;
            }

            internal static List<Group> GetAllGroups(Sorting sorting = Sorting.Not)
            {
                List<Group> groups = Plugin.Groups;

                if (sorting == Sorting.Normal)
                    groups.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                if (sorting == Sorting.Reversed)
                    groups.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                if (groups.Count == 0)
                    groups.Add(new Group());

                return groups;
            }

            internal enum Sorting
            {
                Not,
                Normal,
                Reversed
            }

            internal static Group GetPrimaryGroup(BasePlayer player) => GetGroups(player, Sorting.Normal)[0];

            internal static string Format(BasePlayer player, string message, bool console = false)
            {
                //  Primary group (Highest Priority)
                Group primary = GetPrimaryGroup(player);

                //  All groups the player has
                List<Group> all = GetGroups(player, Plugin.General_ReverseTitleOrder ? Sorting.Normal : Sorting.Reversed);

                //  Init Replacements
                var replacements = new Dictionary<string, object>
                {
                    { "Name", player.displayName },
                    { "SteamID", player.userID },
                    { "Time", DateTime.Now.TimeOfDay },
                    { "Date", DateTime.Now },
                    { "Group", primary.GroupName }
                };

                //  Get Formatting
                string output = console ? primary.Formatting.Console : primary.Formatting.Chat;

                //  Add Title
                output = output.Replace("{Title}", string.Join(" ", (from Group in all where !Group.Title.Hidden && !(Group.Title.HideIfNotHighestPriority && Group.Priority > primary.Priority) select Group.Title.Formatted).ToArray()));

                //  Add Message
                output = primary.Message.Replace(output, StripTags(message));
                //  Add PlayerName
                output = primary.PlayerName.Replace(output, StripTags(player.displayName));

                //  Replace other tags
                foreach (var kvp in replacements)
                    output = output.Replace($"{{{kvp.Key}}}", StripTags(kvp.Value.ToString()));

                if (console)
                    return StripTags(output);

                return output;
            }

            internal static string StripTags(string source)
            {
                string output = source;

                foreach (string tag in new List<string>
                {
                    "</color>",
                    "</size>",
                    "<i>",
                    "<b>",
                    "</i>",
                    "</b>"
                })
                    output = new Regex(tag, RegexOptions.IgnoreCase).Replace(output, string.Empty);

                foreach (string tag in new List<string>
                {
                    @"<color=.+?>",
                    @"<size=.+?>",
                })
                    output = new Regex(tag, RegexOptions.IgnoreCase).Replace(output, string.Empty);

                return output;
            }

            public override int GetHashCode() => GroupName.GetHashCode();

            public override string ToString() => GroupName;
        }

        class TitleSettings
        {
            public bool Hidden = false;
            public bool HideIfNotHighestPriority = false;
            public int Size = 15;
            public string Color = "#9EC326";
            public string Text = "[Player]";

            internal string Formatted => $"<size={Size}><color={Color}>{Text}</color></size>";
        }

        class MessageSettings
        {
            public int Size = 15;
            public string Color = "white";

            internal string Formatted => $"<size={Size}><color={Color}>{{Message}}</color></size>";
            internal string Replace(string source, string message) => source.Replace("{Message}", Formatted.Replace("{Message}", message));
        }

        class Formatting
        {
            public string Console = "{Title} {Name}: {Message}";
            public string Chat = "{Title} {Name}: {Message}";
        }

        class NameSettings
        {
            public int Size = 15;
            public string Color = "#9EC326";

            internal string Formatted => $"<size={Size}><color={Color}>{{Name}}</color></size>";
            internal string Replace(string source, string name) => source.Replace("{Name}", Formatted.Replace("{Name}", name));
        }

        #endregion

        #region Global Declaration

        static BetterChat Plugin = new BetterChat();

        List<Group> Groups = new List<Group>();
        List<Player> Players = new List<Player>();

        bool globalMute = false;

        #region Cached Variables

        bool General_ReverseTitleOrder;

        bool AntiFlood_Enabled;
        float AntiFlood_Seconds;

        bool WordFilter_Enabled;
        string WordFilter_Replacement;
        bool WordFilter_UseCustomReplacement;
        string WordFilter_CustomReplacement;
        List<object> WordFilter_Phrases;

        #endregion

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

            Plugin = this;

            RegisterPerm("mute");
            RegisterPerm("admin");

            LoadData(ref Groups, "Groups");
            LoadData(ref Players, "Players");

            LoadMessages();
            LoadConfig();

            if (Groups.Count == 0)
            {
                Groups.Add(new Group());
                Group.Updated();
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerInit(player);

            //PrintWarning("Normal: " + string.Join(Environment.NewLine, (from group in Group.GetAllGroups(Sorting.Normal) select "{group.Priority}: {group.GroupName}").ToArray());
            //PrintWarning("Reversed: " + string.Join(Environment.NewLine, (from group in Group.GetAllGroups(Sorting.Reversed) select "{group.Priority}: {group.GroupName}").ToArray());
        }

        void OnPlayerInit(BasePlayer player)
        {
            Player pl = Player.FindOrCreate(player);
            pl.Update(player);
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        void LoadConfig()
        {
            SetConfig("General", "Reverse Title Order", false);

            SetConfig("Anti Flood", "Enabled", true);
            SetConfig("Anti Flood", "Seconds", 1.5f);

            SetConfig("Word Filter", "Enabled", false);
            SetConfig("Word Filter", "Replacement", "*");
            SetConfig("Word Filter", "Custom Replacement", "Unicorn");
            SetConfig("Word Filter", "Use Custom Replacement", false);
            SetConfig("Word Filter", "Phrases", new List<object> {
                "bitch",
                "faggot",
                "fuck"
            });

            SaveConfig();

            //////////////////////////////////////////////////////////////////////////////////

            General_ReverseTitleOrder = GetConfig(false, "General", "Reverse Title Order");

            AntiFlood_Enabled = GetConfig(true, "Anti Flood", "Enabled");
            AntiFlood_Seconds = GetConfig(1.5f, "Anti Flood", "Seconds");

            WordFilter_Enabled = GetConfig(false, "Word Filter", "Enabled");
            WordFilter_Replacement = GetConfig("*", "Word Filter", "Replacement");
            WordFilter_UseCustomReplacement = GetConfig(false, "Word Filter", "Use Custom Replacement");
            WordFilter_CustomReplacement = GetConfig("Unicorn", "Word Filter", "Custom Replacement");
            WordFilter_Phrases = GetConfig(new List<object> {
              "bitch",
              "faggot",
              "fuck"
            }, "Word Filter", "Phrases");
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command."},
                {"Group Created", "Group '{group}' was created."},
                {"Group Removed", "Group '{group}' was removed."},
                {"Group Already Exists", "Group '{group}' already exists!"},
                {"Group Does Not Exist", "Group '{group}' does not exist!"},
                {"Player Added To Group", "Player {player} was added to group '{group}'."},
                {"Player Removed From Group", "Player {player} was removed from group '{group}'."},
                {"Invalid Type", "Failed to convert value: {Message}"},
                {"Failed To Set Group Value", "Failed to set group value: {Error}"},
                {"Muted Player", "{player} was muted!"},
                {"Time Muted Player", "{player} was muted for {time}!"},
                {"Unmuted Player", "{player} was unmuted!"},
                {"Player Is Not Muted", "{player} is not muted."},
                {"Player Already Muted", "{player} is already muted."},
                {"You Are Muted", "You are muted. You may not chat."},
                {"You Are Time Muted", "You are muted for {time}. You may not chat."},
                {"Ignoring Player", "You are now ignoring {player}."},
                {"No Longer Ignoring Player", "You are no longer ignoring {player}."},
                {"Not Ignoring Player", "You are not ignoring {player}."},
                {"Already Ignoring Player", "You are already ignoring {player}."},
                {"Player Group List", "{player}'s groups: {groups}"},
                {"Chatting Too Fast", "You're chatting too fast - try again in {time} seconds"},
                {"Time Muted Global", "All players were muted for {time}!"},
                {"Muted Global", "All players were muted!"},
                {"Unmuted Global", "All players were unmuted!"}
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        #endregion

        #region Formatting Helpers

        string FormatTime(TimeSpan time) => $"{(time.Hours == 0 ? string.Empty : $"{time.Hours} hour(s)")}{(time.Hours != 0 && time.Minutes != 0 ? $", " : string.Empty)}{(time.Minutes == 0 ? string.Empty : $"{time.Minutes} minute(s)")}{(time.Minutes != 0 && time.Seconds != 0 ? $", " : string.Empty)}{(time.Seconds == 0 ? string.Empty : $"{time.Seconds} second(s)")}";

        bool TryGetDateTime(string source, out DateTime date)
        {
            int minutes = 0, hours = 0, days = 0;

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

        #region Commands

        [ChatCommand("ignore")]
        void cmdIgnore(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /ignore <player|steamid>");
                return;
            }

            BasePlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player targetPl = Player.FindOrCreate(target);

            if (targetPl.Ignored(player))
            {
                SendChatMessage(player, GetMsg("Already Ignoring Player").Replace("{player}", target.displayName));
                return;
            }

            targetPl.ignoring.Add(player.userID);

            SendChatMessage(player, GetMsg("Ignoring Player").Replace("{player}", target.displayName));
        }

        [ChatCommand("unignore")]
        void cmdUnignore(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /unignore <player|steamid>");
                return;
            }

            BasePlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player targetPl = Player.FindOrCreate(target);

            if (!targetPl.Ignored(player))
            {
                SendChatMessage(player, GetMsg("Not Ignoring Player").Replace("{player}", target.displayName));
                return;
            }

            targetPl.ignoring.Remove(player.userID);

            SendChatMessage(player, GetMsg("No Longer Ignoring Player").Replace("{player}", target.displayName));
        }

        [ConsoleCommand("muteglobal")]
        void ccmdMuteGlobal(ConsoleSystem.Arg arg) => RunChatCommandFromConsole(arg, cmdMuteGlobal);

        [ChatCommand("muteglobal")]
        void cmdMuteGlobal(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            globalMute = true;

            BroadcastChat(GetMsg("Muted Global"));
        }

        [ConsoleCommand("unmuteglobal")]
        void ccmdUnmuteGlobal(ConsoleSystem.Arg arg) => RunChatCommandFromConsole(arg, cmdUnmuteGlobal);

        [ChatCommand("unmuteglobal")]
        void cmdUnmuteGlobal(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            globalMute = false;

            BroadcastChat(GetMsg("Unmuted Global"));
        }

        [ConsoleCommand("mute")]
        void ccmdMute(ConsoleSystem.Arg arg) => RunChatCommandFromConsole(arg, cmdMute);

        [ChatCommand("mute")]
        void cmdMute(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /mute <player|steamid> [time]");
                return;
            }

            BasePlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.FindOrCreate(target);

            if (pl.mute.Muted)
            {
                SendChatMessage(player, GetMsg("Player Already Muted").Replace("{player}", target.displayName));
                return;
            }

            if (args.Length == 2)
            {
                DateTime endDate;

                if (!TryGetDateTime(args[1], out endDate))
                {
                    SendChatMessage(player, GetMsg("Invalid Time Format"));
                    return;
                }

                endDate += new TimeSpan(0, 0, 1);

                pl.mute.Mute(true, endDate);
                BroadcastChat(GetMsg("Time Muted Player").Replace("{player}", target.displayName).Replace("{time}", FormatTime(endDate - DateTime.Now)));
            }
            else
            {
                pl.mute.Mute();
                BroadcastChat(GetMsg("Muted Player").Replace("{player}", target.displayName));
            }
        }

        [ConsoleCommand("unmute")]
        void ccmdUnmute(ConsoleSystem.Arg arg) => RunChatCommandFromConsole(arg, cmdUnmute);

        [ChatCommand("unmute")]
        void cmdUnmute(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player.userID, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /unmute <player|steamid>");
                return;
            }

            BasePlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.FindOrCreate(target);

            if (!pl.mute.Muted)
            {
                SendChatMessage(player, GetMsg("Player Is Not Muted").Replace("{player}", target.displayName));
                return;
            }

            pl.mute.Unmute();
        }

        [ConsoleCommand("chat")]
        void ccmdBetterChat(ConsoleSystem.Arg arg) => RunChatCommandFromConsole(arg, cmdBetterChat);

        [ChatCommand("chat")]
        void cmdBetterChat(BasePlayer player, string cmd, string[] args)
        {
            if (player != null && !HasPerm(player?.userID, "admin"))
            {
                SendChatMessage(player, GetMsg("No Permission"));
                return;
            }

            if (args.Length == 0)
            {
                SendChatMessage(player, "/chat <group|user>");
                return;
            }

            switch (args[0].ToLower())
            {
                case "group":

                    if (args.Length < 2)
                    {
                        SendChatMessage(player, "/chat group <list|add|remove|set>");
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "list":

                            SendChatMessage(player, "Groups: " + string.Join(", ", (from cur_Group in Groups select cur_Group.GroupName).ToArray()));

                            break;

                        case "add":

                            if (args.Length != 3)
                            {
                                SendChatMessage(player, "Syntax: /chat group add <groupName>");
                                return;
                            }

                            string add_GroupName = args[2];

                            if (Group.Find(add_GroupName) != null)
                            {
                                SendChatMessage(player, GetMsg("Group Already Exists").Replace("{group}", add_GroupName));
                                return;
                            }

                            Groups.Add(new Group { GroupName = add_GroupName });
                            SendChatMessage(player, GetMsg("Group Created").Replace("{group}", add_GroupName));

                            break;

                        case "remove":

                            if (args.Length != 3)
                            {
                                SendChatMessage(player, "Syntax: /chat group remove <groupName>");
                                return;
                            }

                            string remove_GroupName = args[2];

                            if (Group.Find(remove_GroupName) == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", remove_GroupName));
                                return;
                            }

                            Group.Remove(remove_GroupName);
                            Group.Updated();

                            SendChatMessage(player, GetMsg("Group Removed").Replace("{group}", remove_GroupName));

                            break;

                        case "set":

                            if (args.Length < 5)
                            {
                                SendChatMessage(player, "Syntax: /chat group set <group> <key> <value>");
                                SendChatMessage(player, "Keys: Priority, HideIfNotHighestPriority, Title, TitleColor, TitleSize, NameColor, NameSize, MessageColor, MessageSize, ChatFormatting, ConsoleFormatting");
                                return;
                            }

                            string set_GroupName = args[2];
                            Group group = Group.Find(set_GroupName);

                            if (group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", set_GroupName));
                                return;
                            }

                            string key = args[3];
                            string value = ListToString(args.ToList(), 4, " ");

                            object response = group.Set(key, value);

                            if (response == null)
                                return;

                            if (response is string)
                                SendChatMessage(player, (string)response);

                            Group.Updated();

                            break;

                        default:
                            SendChatMessage(player, "/chat group <list|add|remove|set>");
                            break;
                    }

                    break;

                case "user":

                    if (args.Length < 2)
                    {
                        SendChatMessage(player, "/chat user <add|remove|groups>");
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "add":

                            if (args.Length != 4)
                            {
                                SendChatMessage(player, "Syntax: /chat user add <player|steamID> <groupName>");
                                return;
                            }

                            bool add_IsSteamID = false;

                            string add_PlayerNameOrID = args[2];
                            BasePlayer add_Player = null;
                            ulong add_SteamID;

                            add_IsSteamID = TryConvert(add_PlayerNameOrID, out add_SteamID);

                            if (!add_IsSteamID)
                            {
                                add_Player = GetPlayer(add_PlayerNameOrID, player);

                                if (add_Player == null)
                                    return;
                            }

                            string add_GroupName = args[3];
                            Group add_Group = Group.Find(add_GroupName);

                            if (add_Group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", add_GroupName));
                                return;
                            }

                            if (!add_IsSteamID)
                                add_Group.AddToGroup(add_Player);
                            else
                                add_Group.AddToGroup(add_SteamID);

                            SendChatMessage(player, GetMsg("Player Added To Group").Replace("{player}", add_IsSteamID ? add_SteamID.ToString() : add_Player.displayName).Replace("{group}", add_GroupName));

                            break;

                        case "remove":

                            if (args.Length != 4)
                            {
                                SendChatMessage(player, "Syntax: /chat user remove <player|steamID> <groupName>");
                                return;
                            }

                            bool remove_IsSteamID = false;

                            string remove_PlayerNameOrID = args[2];
                            BasePlayer remove_Player = null;
                            ulong remove_SteamID;

                            remove_IsSteamID = TryConvert(remove_PlayerNameOrID, out remove_SteamID);

                            if (!remove_IsSteamID)
                            {
                                remove_Player = GetPlayer(remove_PlayerNameOrID, player);

                                if (remove_Player == null)
                                    return;
                            }

                            string remove_GroupName = args[3];
                            Group remove_Group = Group.Find(remove_GroupName);

                            if (remove_Group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", remove_GroupName));
                                return;
                            }

                            if (!remove_IsSteamID)
                                remove_Group.RemoveFromGroup(remove_Player);
                            else
                                remove_Group.RemoveFromGroup(remove_SteamID);

                            SendChatMessage(player, GetMsg("Player Removed From Group").Replace("{player}", remove_IsSteamID ? remove_SteamID.ToString() : remove_Player.displayName).Replace("{group}", remove_GroupName));

                            break;

                        case "groups":
                            if (args.Length != 3)
                            {
                                SendChatMessage(player, "Syntax: /chat user groups <player>");
                                return;
                            }

                            BasePlayer groups_Player = null;

                            groups_Player = GetPlayer(args[2], player);

                            if (groups_Player == null)
                                return;

                            SendChatMessage(player, GetMsg("Player Group List").Replace("{player}", groups_Player.displayName).Replace("{groups}", ListToString(Group.GetGroups(groups_Player))));
                            break;

                        default:
                            SendChatMessage(player, "/chat user <add|remove|groups>");
                            break;
                    }

                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Plugin Inbuilt Hooks

        void OnUnmuted(Player player)
        {
            if (player == null)
                return;

            BroadcastChat(GetMsg("Unmuted Player").Replace("{player}", player.name));
        }

        #endregion

        #region Word Filter

        string FilterText(string original)
        {
            string filtered = original;

            foreach (string word in original.Split(' '))
                foreach (string bannedword in WordFilter_Phrases)
                    if (TranslateLeet(word).ToLower().Contains(bannedword.ToLower()))
                        filtered = filtered.Replace(word, Replace(word));

            /*foreach (string word in GetConfig(new List<object> { "bitch", "faggot", "fuck" }, "Banned Words"))
                filtered = new Regex(@"((?:[\S]?)+" + word + @"(?:[\S]?)+)", RegexOptions.IgnoreCase).Replace(filtered, (a) => Replace(a));*/

            return filtered;
        }

        string Replace(string original)
        {
            string filtered = string.Empty;

            if (!WordFilter_UseCustomReplacement)
                for (; filtered.Count() < original.Count();)
                    filtered += WordFilter_Replacement;
            else
                filtered = WordFilter_CustomReplacement;

            return filtered;
        }

        string TranslateLeet(string original)
        {
            string translated = original;

            Dictionary<string, string> leetTable = new Dictionary<string, string>
            {
                { "}{", "h" },
                { "|-|", "h" },
                { "]-[", "h" },
                { "/-/", "h" },
                { "|{", "k" },
                { "/\\/\\", "m" },
                { "|\\|", "n" },
                { "/\\/", "n" },
                { "()", "o" },
                { "[]", "o" },
                { "vv", "w" },
                { "\\/\\/", "w" },
                { "><", "x" },
                { "2", "z" },
                { "4", "a" },
                { "@", "a" },
                { "8", "b" },
                { "Ã", "b" },
                { "(", "c" },
                { "<", "c" },
                { "{", "c" },
                { "3", "e" },
                { "â¬", "e" },
                { "6", "g" },
                { "9", "g" },
                { "&", "g" },
                { "#", "h" },
                { "$", "s" },
                { "7", "t" },
                { "|", "l" },
                { "1", "i" },
                { "!", "i" },
                { "0", "o" },
            };

            foreach (var leet in leetTable)
                translated = translated.Replace(leet.Key, leet.Value);

            return translated;
        }

        #endregion

        #region API

        Dictionary<string, object> API_FindGroup(string name) => Group.Find(name).Dictionary;

        List<Dictionary<string, object>> API_GetAllGroups() => (from current in Groups select current.Dictionary).ToList();

        Dictionary<string, object> API_FindPlayerPrimaryGroup(BasePlayer player) => Group.GetPrimaryGroup(player).Dictionary;

        List<Dictionary<string, object>> API_FindPlayerGroups(BasePlayer player) => (from current in Group.GetGroups(player) select current.Dictionary).ToList();

        bool API_GroupExists(string name) => Group.Find(name) != null;

        bool API_AddGroup(string name)
        {
            if (Group.Find(name) != null)
                return false;

            Groups.Add(new Group { GroupName = name });
            return true;
        }

        bool API_RemoveGroup(string name)
        {
            if (Group.Find(name) != null)
                return false;

            Groups.Remove(Group.Find(name));
            return true;
        }

        bool API_IsUserInGroup(BasePlayer player, string groupName)
        {
            if (Group.Find(groupName) == null)
                return false;

            return Group.Find(groupName).HasGroup(player);
        }

        bool API_RemoveUserFromGroup(BasePlayer player, string groupName)
        {
            if (Group.Find(groupName) == null || !(bool)Group.Find(groupName)?.HasGroup(player))
                return false;

            Group.Find(groupName).RemoveFromGroup(player);
            return true;
        }

        bool API_AddUserToGroup(BasePlayer player, string groupName)
        {
            if (Group.Find(groupName) == null || (bool)Group.Find(groupName)?.HasGroup(player))
                return false;

            Group.Find(groupName).AddToGroup(player);
            return true;
        }

        object API_SetGroupSetting(string groupName, string key, string value)
        {
            if (Group.Find(groupName) == null)
                return null;

            return Group.Find(groupName).Set(key, value);
        }

        bool API_IsPlayerMuted(BasePlayer player) => Player.FindOrCreate(player).mute.Muted;

        bool API_PlayerIgnores(BasePlayer player1, BasePlayer player2) => Player.FindOrCreate(player2).Ignored(player1);

        #endregion

        #region Oxide Hooks

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.connection == null || arg.connection.player == null)
                return null;

            BasePlayer player = (BasePlayer)arg.connection.player;
            Player pl = Player.FindOrCreate(player);
            string message = GetConfig(false, "Word Filter", "Enabled") ? FilterText(arg.GetString(0)) : arg.GetString(0);

            //  Is message invalid?
            if (message == string.Empty || message.Length <= 1)
                return false;

            pl.mute.Update();

            // Is global mute active?
            if (globalMute)
            {
                SendChatMessage(player, GetMsg("Global Mute", player.userID));
                return false;
            }

            //  Is player muted?
            if (pl.mute.Muted)
            {
                if (pl.mute.state == MutedState.TimeMuted)
                {
                    TimeSpan remainingTime = pl.mute.date.value - DateTime.Now;
                    SendChatMessage(player, GetMsg("You Are Time Muted", player.userID).Replace("{time}", FormatTime(remainingTime)));
                }
                else
                    SendChatMessage(player, GetMsg("You Are Muted", player.userID));
                return false;
            }

            //  NextChatTime is not set? SET IT!
            if (player.NextChatTime == 0f)
                player.NextChatTime = Time.realtimeSinceStartup - 30f;

            //  Chatting too fast?
            if (player.NextChatTime > Time.realtimeSinceStartup && AntiFlood_Enabled)
            {
                player.NextChatTime += AntiFlood_Seconds;

                float remainingTime = (player.NextChatTime - Time.realtimeSinceStartup) + 0.5f;
                SendChatMessage(player, GetMsg("Chatting Too Fast").Replace("{time}", Math.Round(remainingTime, 1).ToString()));

                return false;
            }

            if ((bool?)plugins.CallHook("OnBetterChat", player, message) ?? true == false)
                return false;

            //  Send message to all players who do not ignore the player
            foreach (BasePlayer current in BasePlayer.activePlayerList)
                if (!pl.Ignored(current))
                    rust.SendChatMessage(current, Group.Format(player, message), null, player.UserIDString);

            //  Log message to console
            Puts(Group.Format(player, message, true));
            //  Set NextChatTime for AntiFlood
            player.NextChatTime = Time.realtimeSinceStartup + AntiFlood_Seconds;

            return false;
        }

        #endregion

        #region General Methods

        void RunChatCommandFromConsole(ConsoleSystem.Arg arg, Action<BasePlayer, string, string[]> chatCommand)
        {
            if (arg == null)
                return;

            chatCommand((BasePlayer)arg.connection?.player ?? null, arg.cmd?.name ?? string.Empty, arg.HasArgs() ? arg.Args : new string[0]);
        }

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer player)
        {
            ulong steamID;
            bool isSteamID = ulong.TryParse(searchedPlayer, out steamID);

            if (isSteamID && BasePlayer.FindByID(steamID) == null)
                SendChatMessage(player, "No player with that steamID could be found.");
            else if (isSteamID)
                return BasePlayer.FindByID(steamID);

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
                    SendChatMessage(player, "No player with that name could be found.");
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

        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from val in list select val.ToString()).Skip(first).ToArray());

        bool TryConvert<Source, Converted>(Source s, out Converted c)
        {
            try
            {
                c = (Converted)Convert.ChangeType(s, typeof(Converted));
                return true;
            }
            catch (Exception)
            {
                c = default(Converted);
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

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? Name : $"{Name}/{filename}");

        void SaveData<T>(ref T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? Name : $"{Name}/{filename}", data);

        string Name => Title.Replace(" ", "");

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

        string PermissionPrefix => Title.Replace(" ", "").ToLower();

        ////////////////////////////////////////
        ///     Chat Related
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => rust.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if (player == null)
                Puts(msg == null ? prefix.Replace("/", "") : prefix + ": " + msg.Replace("/", ""));
            else
                rust.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);
        }

        #endregion
    }
}