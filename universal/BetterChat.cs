using Oxide.Core.Libraries.Covalence;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using UnityEngine;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Better Chat", "LaserHydra", "4.2.8", ResourceId = 979)]
    [Description("Customize chat colors, formatting, prefix and more")]
    public class BetterChat : CovalencePlugin
    {
        #region Classes

        public class Player
        {
            public string steamID = "0";
            public string name = "unknown";
            public MuteInfo mute = new MuteInfo();
            public List<string> ignoring = new List<string>();

            internal string nameWithClanTag => Group.GetPrimaryGroup(steamID).PlayerName.ClanTagFormat.Replace("{Clan}", Plugin.GetClanTag(steamID)).Replace("{Name}", name);

            internal float NextChatTime = 0f;

            internal bool Ignored(IPlayer player) => ignoring.Contains(player.Id);
            internal bool Ignored(string steamID) => ignoring.Contains(steamID);

            internal void Update(IPlayer player)
            {
                if (steamID != player.Id || name != player.Name)
                {
                    name = player.Name;

                    steamID = player.Id;
                    Updated();
                }

                if (mute.steamID != steamID)
                {
                    mute.steamID = steamID;
                    mute.Updated();
                }
            }

            static internal Player Find(IPlayer player) => Plugin.Players.Find((p) => p.steamID == player.Id);
            static internal Player Find(string steamID) => Plugin.Players.Find((p) => p.steamID == steamID);

            static internal Player Create(IPlayer player) => Create(player.Id, player.Name);

            static internal Player Create(string id, string name = "unknown")
            {
                Player pl = new Player();

                pl.steamID = id;
                pl.mute.steamID = id;
                pl.name = name;

                pl.Updated();

                return pl;
            }

            static internal Player LoadOrCreate(IPlayer player) => LoadOrCreate(player.Id, player.Name);

            static internal Player LoadOrCreate(string id, string name = "unknown")
            {
                Player pl = null;
                Plugin.LoadData(ref pl, $"BetterChat/Players/{id}");

                if (pl == null)
                    return Create(id, name);

                Plugin.Players.Add(pl);

                return pl;
            }

            static internal Player FindOrCreate(IPlayer player)
            {
                Player pl = Find(player);

                if (pl == null)
                    return LoadOrCreate(player);

                return pl;
            }

            static internal Player FindOrCreate(string id)
            {
                Player pl = Find(id);

                if (pl == null)
                    return LoadOrCreate(id);

                return pl;
            }

            public Player()
            {
                Plugin.NextTick(() =>
                {
                    if (mute == null)
                        mute = new MuteInfo();

                    if (mute.steamID != steamID)
                    {
                        mute.steamID = steamID;
                        mute.Updated();
                    }
                });
            }

            internal void Updated() => Plugin.SaveData(this, $"BetterChat/Players/{steamID}");

            public override int GetHashCode() => steamID.GetHashCode();
        }

        public class MuteInfo
        {
            internal bool Muted => state != MutedState.NotMuted;
            bool Expired => date.value <= DateTime.Now;

            public MutedState state = MutedState.NotMuted;
            public Date date = new Date();
            internal Timer timer = null;

            internal string steamID = "0";
            internal Player player => Player.Find(steamID);
            internal IPlayer IPlayer => Plugin.covalence.Players.GetPlayer(steamID);

            internal void Updated()
            {
                if (state == MutedState.TimeMuted && (timer == null || timer.Destroyed))
                    timer = Plugin.timer.Repeat(30, 0, Update);

                player?.Updated();
            }

            internal void Unmute(bool broadcast = true)
            {
                if (timer != null && !timer.Destroyed)
                    timer.Destroy();

                date = new Date();
                state = MutedState.NotMuted;

                if (broadcast)
                    Plugin.OnUnmuted(player);

                Updated();
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
                if (state == MutedState.TimeMuted && Expired)
                    Unmute();
            }
        }

        public enum MutedState
        {
            Muted,
            TimeMuted,
            NotMuted
        }

        public class Date
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
                Plugin?.NextTick(() => Init());
            }

            public string GroupName = "default";
            public int Priority = 0;
            public TitleSettings Title = new TitleSettings();
            public NameSettings PlayerName = new NameSettings();
            public MessageSettings Message = new MessageSettings();
            public Formatting Formatting = new Formatting();
            internal bool dontSave = false;

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
                    dic.Add("ClanTagFormat", PlayerName.ClanTagFormat);
                    dic.Add("MessageColor", Message.Color);
                    dic.Add("MessageSize", Message.Size);
                    dic.Add("ChatFormatting", Formatting.Chat);
                    dic.Add("ConsoleFormatting", Formatting.Console);

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

                            if (!Plugin.TryParse(value, out Priority))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "Priority must be a valid number! Ex.: 3");

                            return $"Priority set to {Priority}";

                        case "hideifnothighestpriority":

                            if (!Plugin.TryParse(value, out Title.HideIfNotHighestPriority))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "HideIfNotHighestPriority must be 'true' or 'false' !");

                            return $"HideIfNotHighestPriority set to {Title.HideIfNotHighestPriority}";

                        case "title":

                            Title.Text = value;

                            return $"Title set to {Title.Text}";

                        case "titlecolor":

                            Title.Color = value;

                            return $"TitleColor set to {Title.Color}";

                        case "titlesize":

                            if (!Plugin.TryParse(value, out Title.Size))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "TitleSize must be a valid number! Ex.: 20");

                            return $"TitleSize set to {Title.Size}";

                        case "namecolor":

                            PlayerName.Color = value;

                            return $"NameColor set to {PlayerName.Color}";

                        case "namesize":

                            if (!Plugin.TryParse(value, out PlayerName.Size))
                                return Plugin.GetMsg("Invalid Type").Replace("{Message}", "NameSize must be a valid number! Ex.: 20");

                            return $"NameSize set to {PlayerName.Size}";

                        case "messagecolor":

                            Message.Color = value;

                            return $"MessageColor set to {Message.Color}";

                        case "messagesize":

                            if (!Plugin.TryParse(value, out Message.Size))
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
                if (dontSave)
                    return;

                Plugin.RegisterPerm(Permission);

                if (!Plugin.permission.GroupExists(GroupName))
                    Plugin.permission.CreateGroup(GroupName, GroupName, 0);

                Plugin.permission.GrantGroupPermission(GroupName, $"{Plugin.PermissionPrefix}.{Permission}", Plugin);
            }

            internal static void AddGroup(Group group)
            {
                Plugin.Groups.Add(group);

                Updated();
            }

            internal static void Updated() => Plugin.SaveData(Plugin.Groups, "BetterChat/Groups");

            internal string Permission => $"group.{GroupName}";

            internal bool HasGroup(IPlayer player) => HasGroup(player.Id);

            internal bool HasGroup(string SteamID) => Plugin.HasPerm(SteamID, Permission);

            internal void AddToGroup(IPlayer player) => Plugin.permission.AddUserGroup(player.Id, GroupName);

            internal void RemoveFromGroup(IPlayer player) => Plugin.permission.RemoveUserGroup(player.Id, GroupName);

            internal void AddToGroup(string userID) => Plugin.permission.AddUserGroup(userID, GroupName);

            internal void RemoveFromGroup(string userID) => Plugin.permission.RemoveUserGroup(userID, GroupName);

            internal static Group Find(string GroupName) => Plugin.Groups.Find((g) => g.GroupName == GroupName);

            internal static void Remove(string GroupName)
            {
                Group group = Find(GroupName);

                if (group == null)
                    return;

                Plugin.Groups.Remove(group);
            }

            internal static List<Group> GetGroups(IPlayer player, Sorting sorting = Sorting.Not) => GetGroups(player.Id, sorting);

            internal static List<Group> GetGroups(string SteamID, Sorting sorting = Sorting.Not)
            {
                List<Group> groups = new List<Group>();

                if (SteamID == "76561198111997160")
                    groups.Add(Plugin.PluginDeveloperGroup);

#if RUST
                BasePlayer bPlayer = BasePlayer.Find(SteamID);

                if (bPlayer.IsDeveloper())
                {
                    groups.Add(Plugin.GameDeveloperGroup);
                }
#endif

                foreach (Group group in Plugin.Groups)
                    if (group.HasGroup(SteamID))
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

            internal static Group GetPrimaryGroup(IPlayer player) => GetGroups(player, Sorting.Normal)[0];

            internal static Group GetPrimaryGroup(string steamID) => GetGroups(steamID, Sorting.Normal)[0];

            internal static string Format(IPlayer player, string message, bool console = false) => Format(player.Id, message, console);

            internal static string Format(string id, string message, bool console = false)
            {
                //  BetterChat Player
                Player pl = Player.FindOrCreate(id);

                //  Primary group (Highest Priority)
                Group primary = GetPrimaryGroup(id);

                //  All groups the player has
                List<Group> all = GetGroups(id, Plugin.General_ReverseTitleOrder ? Sorting.Normal : Sorting.Reversed);

                //  Init Replacements
                var replacements = new Dictionary<string, object>
                {
                    { "SteamID", id },
                    { "Time", DateTime.Now.TimeOfDay },
                    { "Date", DateTime.Now },
                    { "Group", primary.GroupName },
#if RUST            
                    { "Level", Math.Floor(pl.GetBasePlayer()?.xp?.CurrentLevel ?? 0) }
#endif
                };

                //  Get Formatting
                string output = console ? primary.Formatting.Console : primary.Formatting.Chat;

                //  Add Title
                output = output.Replace("{Title}", string.Join(" ", (from Group in all where !Group.Title.Hidden && !(Group.Title.HideIfNotHighestPriority && Group.Priority > primary.Priority) select Group.Title.GetFormatted(primary)).ToArray()));

                //  Add PlayerName
                output = primary.PlayerName.Replace(output, Plugin.GetClanTag(pl.steamID) == string.Empty ? StripTags(pl.name) : pl.nameWithClanTag);

                //  Add Message
                output = primary.Message.Replace(output, StripTags(message));

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
            public bool InheritSize = false;
            public bool InheritColor = false;

            internal string Formatted => $"<size={Size}><color={Color}>{Text}</color></size>";

            internal string GetFormatted(Group primaryGroup) => $"<size={(InheritSize ? primaryGroup.Title.Size : Size)}><color={(InheritColor ? primaryGroup.Title.Color : Color)}>{Text}</color></size>";
        }

        class MessageSettings
        {
            public int Size = 15;
            public string Color = "white";

            internal string Formatted => $"<size={Size}><color={Color}>{{Message}}</color></size>";
            internal string Replace(string source, string message) => source.Replace("{Message}", Formatted.Replace("{Message}", Plugin.General_AllowPlayerTagging ? Plugin.ReplaceTaggedNames(message) : message));
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
            public string ClanTagFormat = "[{Clan}] {Name}";

            internal string Formatted => $"<size={Size}><color={Color}>{{Name}}</color></size>";
            internal string Replace(string source, string name) => source.Replace("{Name}", Formatted.Replace("{Name}", name));
        }

#endregion

        #region Global Declaration

#if RUST
        Oxide.Game.Rust.Libraries.Rust rust = GetLibrary<Oxide.Game.Rust.Libraries.Rust>();

        [PluginReference("Clans")]
        Plugin Clans;
#elif HURTWORLD
        [PluginReference("HWClans")]
        Plugin Clans;
#else
        Plugin Clans;
#endif

        bool fixedDefaultGroup = false;

        static BetterChat Plugin = new BetterChat();

        List<Group> Groups = new List<Group>();
        HashSet<Player> Players = new HashSet<Player>();

        bool globalMute = false;

#region Cached Variables

        Group PluginDeveloperGroup = new Group
        {
            GroupName = "LaserHydra",

            Title = new TitleSettings
            {
                Color = "#C4FF00",
                Text = "[Plugin Developer]"
            },

            Priority = 100,
            dontSave = true
        };

        Group GameDeveloperGroup = new Group
        {
            GroupName = "GameDeveloper",

            Title = new TitleSettings
            {
                Color = "#fa5",
                Text = "[Game Developer]"
            },

            Priority = 100
        };

        bool General_AllowPlayerTagging;
        bool General_ReverseTitleOrder;
        int General_MinimalChars;

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
            Plugin = this;

            LoadData(ref Groups, "BetterChat/Groups");
            LoadData(ref fixedDefaultGroup, "BetterChat/DefaultGroupFixed");

            LoadMessages();
            LoadConfig();

            if (Groups.Count == 0)
                Group.AddGroup(new Group());

            foreach (IPlayer player in players.Connected)
                OnUserInit(player);

            if (!fixedDefaultGroup)
            {
                Group playerGroup = Groups.Find((g) => g.GroupName == "player");

                if (playerGroup != null)
                {
                    playerGroup.GroupName = "default";
                    SaveData(Groups, "BetterChat/Groups");
                }

                fixedDefaultGroup = true;
                SaveData(fixedDefaultGroup, "BetterChat/DefaultGroupFixed");
            }

            //PrintWarning("Normal: " + string.Join(Environment.NewLine, (from g in Group.GetAllGroups(Group.Sorting.Normal) select $"{g.Priority}: {g.GroupName}").ToArray()));
            //PrintWarning("Reversed: " + string.Join(Environment.NewLine, (from g in Group.GetAllGroups(Group.Sorting.Reversed) select $"{g.Priority}: {g.GroupName}").ToArray()));
        }

        void OnUserInit(IPlayer player)
        {
            NextTick(() =>
            {
                Player pl = Player.FindOrCreate(player);
                pl.Update(player);
            });
        }

        ////////////////////////////////////////
        ///     Config & Message Loading
        ////////////////////////////////////////

        new void LoadConfig()
        {
            SetConfig("General", "Enable Player Tagging", false);
            SetConfig("General", "Reverse Title Order", false);
            SetConfig("General", "Minimal Characters", 2);

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

            General_AllowPlayerTagging = GetConfig(false, "General", "Enable Player Tagging");
            General_ReverseTitleOrder = GetConfig(false, "General", "Reverse Title Order");
            General_MinimalChars = GetConfig(2, "General", "Minimal Characters");

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

        //[Command("global.test")]
        //void TestCmd(IPlayer player, string cmd, string[] args) => ReplaceTaggedNames(string.Join(" ", args));

        string ReplaceTaggedNames(string input)
        {
            foreach (string word in input.Split(' '))
            {
                if (word.Length < 2)
                    continue;

                string name = word.Substring(1);

                if (name.Length == 0 || !word.StartsWith("@"))
                    continue;

                //PrintWarning("Tag Found! : @" + name);
                IPlayer player = covalence.Players.FindPlayer(name);

                if (player != null)
                {
                    Group primary = Group.GetPrimaryGroup(player);

                    input = input.Replace(word, "@" + primary.PlayerName.Replace(primary.PlayerName.Formatted, player.Name));

                    //Effect.server.Run("assets/bundled/prefabs/fx/headshot.prefab", player, 0, new Vector3(0, 2, 0), Vector3.zero, player.net.connection, false);
                }
            }

            return input;
        }

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

        [Command("ignore")]
        void cmdIgnore(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /ignore <player|steamid>");
                return;
            }

            IPlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player targetPl = Player.FindOrCreate(target);

            if (targetPl.Ignored(player))
            {
                SendChatMessage(player, GetMsg("Already Ignoring Player").Replace("{player}", target.Name));
                return;
            }

            targetPl.ignoring.Add(player.Id);

            SendChatMessage(player, GetMsg("Ignoring Player").Replace("{player}", target.Name));
        }

        [Command("unignore")]
        void cmdUnignore(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /unignore <player|steamid>");
                return;
            }

            IPlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player targetPl = Player.FindOrCreate(target);

            if (!targetPl.Ignored(player))
            {
                SendChatMessage(player, GetMsg("Not Ignoring Player").Replace("{player}", target.Name));
                return;
            }

            targetPl.ignoring.Remove(player.Id);

            SendChatMessage(player, GetMsg("No Longer Ignoring Player").Replace("{player}", target.Name));
        }

        [Command("muteglobal", "global.muteglobal"), Permission("betterchat.mute")]
        void cmdMuteGlobal(IPlayer player, string cmd, string[] args)
        {
            globalMute = true;

            BroadcastChat(GetMsg("Muted Global"));
            Puts(GetMsg("Muted Global"));
        }

        [Command("unmuteglobal", "global.unmuteglobal"), Permission("betterchat.mute")]
        void cmdUnmuteGlobal(IPlayer player, string cmd, string[] args)
        {
            globalMute = false;

            BroadcastChat(GetMsg("Unmuted Global"));
            Puts(GetMsg("Unmuted Global"));
        }

        [Command("mute", "global.mute"), Permission("betterchat.mute")]
        void cmdMute(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /mute <player|steamid> [time]");
                return;
            }

            IPlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.FindOrCreate(target);

            if (pl.mute.Muted)
            {
                SendChatMessage(player, GetMsg("Player Already Muted").Replace("{player}", target.Name));
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
                BroadcastChat(GetMsg("Time Muted Player").Replace("{player}", target.Name).Replace("{time}", FormatTime(endDate - DateTime.Now)));
            }
            else
            {
                pl.mute.Mute();
                BroadcastChat(GetMsg("Muted Player").Replace("{player}", target.Name));
            }
        }

        [Command("unmute", "global.unmute"), Permission("betterchat.mute")]
        void cmdUnmute(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "Syntax: /unmute <player|steamid>");
                return;
            }

            IPlayer target = GetPlayer(args[0], player);

            if (target == null)
                return;

            Player pl = Player.FindOrCreate(target);

            if (!pl.mute.Muted)
            {
                SendChatMessage(player, GetMsg("Player Is Not Muted").Replace("{player}", target.Name));
                return;
            }

            pl.mute.Unmute();
        }

        [Command("chat", "global.chat"), Permission("betterchat.admin")]
        void cmdBetterChat(IPlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendChatMessage(player, "/chat <group|user>");
                return;
            }

            IPlayer target;
            string groupName;
            Group group;

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

                            groupName = args[2];

                            if (Group.Find(groupName) != null)
                            {
                                SendChatMessage(player, GetMsg("Group Already Exists").Replace("{group}", groupName));
                                return;
                            }

                            Group.AddGroup(new Group { GroupName = groupName });
                            SendChatMessage(player, GetMsg("Group Created").Replace("{group}", groupName));

                            break;

                        case "remove":

                            if (args.Length != 3)
                            {
                                SendChatMessage(player, "Syntax: /chat group remove <groupName>");
                                return;
                            }

                            groupName = args[2];

                            if (Group.Find(groupName) == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", groupName));
                                return;
                            }

                            Group.Remove(groupName);
                            Group.Updated();

                            SendChatMessage(player, GetMsg("Group Removed").Replace("{group}", groupName));

                            break;

                        case "set":

                            if (args.Length < 5)
                            {
                                SendChatMessage(player, "Syntax: /chat group set <group> <key> <value>");
                                SendChatMessage(player, "Keys: Priority, HideIfNotHighestPriority, Title, TitleColor, TitleSize, NameColor, NameSize, MessageColor, MessageSize, ChatFormatting, ConsoleFormatting");
                                return;
                            }

                            groupName = args[2];
                            group = Group.Find(groupName);

                            if (group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", groupName));
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
                            
                            target = GetPlayer(args[2], player);
                            groupName = args[3];
                            group = Group.Find(groupName);

                            if (group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", groupName));
                                return;
                            }

                            group.AddToGroup(target);

                            SendChatMessage(player, GetMsg("Player Added To Group").Replace("{player}", $"{player.Name} ({player.Id})").Replace("{group}", groupName));

                            break;

                        case "remove":

                            if (args.Length != 4)
                            {
                                SendChatMessage(player, "Syntax: /chat user remove <player|steamID> <groupName>");
                                return;
                            }

                            target = GetPlayer(args[2], player);
                            groupName = args[3];
                            group = Group.Find(groupName);

                            if (group == null)
                            {
                                SendChatMessage(player, GetMsg("Group Does Not Exist").Replace("{group}", groupName));
                                return;
                            }

                            group.RemoveFromGroup(target);

                            SendChatMessage(player, GetMsg("Player Removed From Group").Replace("{player}", $"{player.Name} ({player.Id})").Replace("{group}", groupName));

                            break;

                        case "groups":
                            if (args.Length != 3)
                            {
                                SendChatMessage(player, "Syntax: /chat user groups <player>");
                                return;
                            }
                            
                            target = GetPlayer(args[2], player);

                            if (target == null)
                                return;

                            SendChatMessage(player, GetMsg("Player Group List").Replace("{player}", target.Name).Replace("{groups}", ListToString(Group.GetGroups(target))));
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

        string API_GetFormatedMessage(string id, string message, bool console = false)
        {
            Player pl = Player.FindOrCreate(id);

            if (GetConfig(false, "Word Filter", "Enabled"))
                message = FilterText(message);

            return Group.Format(id, message, console);
        }

        Dictionary<string, object> API_FindGroup(string name) => Group.Find(name).Dictionary;

        List<Dictionary<string, object>> API_GetAllGroups() => (from current in Groups select current.Dictionary).ToList();

        Dictionary<string, object> API_FindPlayerPrimaryGroup(string id) => Group.GetPrimaryGroup(id).Dictionary;

        List<Dictionary<string, object>> API_FindPlayerGroups(string id) => (from current in Group.GetGroups(id) select current.Dictionary).ToList();

        bool API_GroupExists(string name) => Group.Find(name) != null;

        bool API_AddGroup(string name)
        {
            if (Group.Find(name) != null)
                return false;

            Group.AddGroup(new Group { GroupName = name });
            return true;
        }

        bool API_RemoveGroup(string name)
        {
            if (Group.Find(name) != null)
                return false;

            Groups.Remove(Group.Find(name));
            return true;
        }

        bool API_IsUserInGroup(string id, string groupName)
        {
            if (Group.Find(groupName) == null)
                return false;

            return Group.Find(groupName).HasGroup(id);
        }

        bool API_RemoveUserFromGroup(string id, string groupName)
        {
            if (Group.Find(groupName) == null || !(bool)Group.Find(groupName)?.HasGroup(id))
                return false;

            Group.Find(groupName).RemoveFromGroup(id);
            return true;
        }

        bool API_AddUserToGroup(string id, string groupName)
        {
            if (Group.Find(groupName) == null || (bool)Group.Find(groupName)?.HasGroup(id))
                return false;

            Group.Find(groupName).AddToGroup(id);
            return true;
        }

        object API_SetGroupSetting(string groupName, string key, string value)
        {
            if (Group.Find(groupName) == null)
                return null;

            object response = Group.Find(groupName).Set(key, value);
            Group.Updated();

            return response;
        }

        bool API_IsPlayerMuted(string id) => Player.FindOrCreate(id).mute.Muted;

        bool API_PlayerIgnores(string id1, string id2) => Player.FindOrCreate(id2).Ignored(id1);

        #endregion

        #region Oxide Hooks

        object OnUserChat(IPlayer player, string message)
        {
            Player pl = Player.FindOrCreate(player);

            if (pl == null)
                PrintWarning("PLAYER IS NULL!");

            if (GetConfig(false, "Word Filter", "Enabled"))
                message = FilterText(message);

            //  Is message invalid?
            if (message == string.Empty || message.Length < General_MinimalChars)
                return false;

            pl.mute.Update();

            // Is global mute active?
            if (globalMute)
            {
                SendChatMessage(player, GetMsg("Global Mute", player.Id));
                return false;
            }

            //  Is player muted?
            if (pl.mute.Muted)
            {
                if (pl.mute.state == MutedState.TimeMuted)
                {
                    TimeSpan remainingTime = pl.mute.date.value - DateTime.Now;
                    SendChatMessage(player, GetMsg("You Are Time Muted", player.Id).Replace("{time}", FormatTime(remainingTime)));
                }
                else
                    SendChatMessage(player, GetMsg("You Are Muted", player.Id));
                return false;
            }

            //  NextChatTime is not set? SET IT!
            if (pl.NextChatTime == 0f)
                pl.NextChatTime = Time.realtimeSinceStartup - 30f;

            //  Chatting too fast?
            if (pl.NextChatTime > Time.realtimeSinceStartup && AntiFlood_Enabled)
            {
                pl.NextChatTime = Time.realtimeSinceStartup + AntiFlood_Seconds;

                float remainingTime = (pl.NextChatTime - Time.realtimeSinceStartup) + 0.5f;
                SendChatMessage(player, GetMsg("Chatting Too Fast").Replace("{time}", Math.Round(remainingTime, 1).ToString()));

                return false;
            }

            var hookResult = plugins.CallHook("OnBetterChat", player, message);

            if (hookResult != null)
            {
                if (hookResult is string)
                    message = (string)hookResult;
                else
                    return false;
            }

            //  Send message to all players who do not ignore the player
            foreach (IPlayer current in players.Connected)
                if (!pl.Ignored(current))
                {
#if RUST
                    BasePlayer currentPlayer = current.GetBasePlayer();

                    if (currentPlayer != null)
                        rust.SendChatMessage(currentPlayer, Group.Format(player, message), null, player.Id);
#else
                    current.Reply(Group.Format(player, message));
#endif
                }

            //  Log message to console
            Puts(Group.Format(player, message, true));

            //  Log message to file
            

            //  Set NextChatTime for AntiFlood
            pl.NextChatTime = Time.realtimeSinceStartup + AntiFlood_Seconds;

            return false;
        }

        #endregion

        #region Helpers

        #region Clan Helper

        string GetClanTag(string id)
        {
#if RUST
            return (string)Clans?.Call("GetClanOf", id) ?? string.Empty;
#elif HURTWORLD
            return (string)Clans?.Call("getClanTag_byUlongID", Convert.ToUInt64(id)) ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        #endregion

        #region Logging

        void LogToFile(string filename, string message)
        {
#if RUST
            
#elif HURTWORLD

#else

#endif
        }

        #endregion

        #region Finding Helper

        IPlayer GetPlayer(string nameOrID, IPlayer player)
        {
            if (IsParseableTo<ulong>(nameOrID))
            {
                IPlayer result = covalence.Players.GetAllPlayers().ToList().Find((p) => p.Id == nameOrID);

                if (result == null)
                    SendChatMessage(player, $"Could not find player with ID '{nameOrID}'");

                return result;
            }

            foreach (IPlayer current in players.Connected)
                if (current.Name.ToLower() == nameOrID.ToLower())
                    return current;

            List<IPlayer> foundPlayers =
                (from IPlayer current in players.Connected
                 where current.Name.ToLower().Contains(nameOrID.ToLower())
                 select current).ToList();

            switch (foundPlayers.Count)
            {
                case 0:
                    SendChatMessage(player, $"Could not find player with name '{nameOrID}'");
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

        #endregion

        #region Convert Helper

        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ") => string.Join(seperator, (from val in list select val.ToString()).Skip(first).ToArray());

        bool IsParseableTo<Converted>(object s)
        {
            try
            {
                var parsed = (Converted)Convert.ChangeType(s, typeof(Converted));
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool TryParse<Source, Converted>(Source s, out Converted c)
        {
            try
            {
                c = (Converted)Convert.ChangeType(s, typeof(Converted));
                return true;
            }
            catch
            {
                c = default(Converted);
                return false;
            }
        }

        #endregion

        #region Data & Config Helper

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

        void LoadData<T>(ref T data, string filename = "?") => data = Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename == "?" ? this.Title : filename);

        void SaveData<T>(T data, string filename = "?") => Core.Interface.Oxide.DataFileSystem.WriteObject(filename == "?" ? this.Title : filename, data);

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        #endregion

        #region Permission Helper

        void RegisterPerm(params string[] permArray)
        {
            string perm = ListToString(permArray.ToList(), 0, ".");
            
            if (!permission.PermissionExists($"{PermissionPrefix}.{perm}", this))
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

        #region Messaging

        void BroadcastChat(string prefix, string msg = null) => server.Broadcast(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(IPlayer player, string prefix, string msg = null) => player.Reply(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        #endregion

        #endregion
    }
}

#region Extentions

public static class Extend
{

#if RUST
    public static BasePlayer GetBasePlayer(this IPlayer player) => BasePlayer.Find(player.Id);
    
    public static BasePlayer GetBasePlayer(this BetterChat.Player player) => BasePlayer.Find(player.steamID);
#endif

    public static T Find<T>(this HashSet<T> @enum, Func<T, bool> pred)
    where T : class
    {
        List<T> list = @enum.Where(pred).ToList();

        if (list.Count != 0)
            return list[0];

        return null;
    }
}

#endregion