//Reference: UnityEngine.UI
using System.Text.RegularExpressions;
using Oxide.Game.Hurtworld.Libraries;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Linq;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Better Chat", "LaserHydra", "1.1.3", ResourceId = 1520)]
    [Description("Customize chat colors, formatting, prefix and more.")]
    class BetterChat : HurtworldPlugin
    {
        [PluginReference("HWClans")]
        Plugin Clans;

        Command commands = Interface.Oxide.GetLibrary<Command>();
        List<string> muted = new List<string>();

        void Loaded()
        {
            LoadData();
            LoadConfig();
            LoadMessages();

            RegisterPerm("mute");
            RegisterPerm("formatting");

            foreach (var kvp in Config)
            {
                string group = kvp.Key;
                if (group == "Mute" || group == "WordFilter" || group == "AntiSpam") continue;


                RegisterPerm(GetConfig(group, group, "Permission"));
                //BroadcastChat($"--> <color=red>Registered permission '{PermissionPrefix}.{GetConfig(group, group, "Permission")}'</color>");

                if (!permission.GroupExists(group))
                {
                    permission.CreateGroup(group, GetConfig("[Player]", group, "Title"), GetConfig(1, group, "Rank"));
                    //BroadcastChat($"--> <color=red>Created group '{group}' as it did not exist</color>");
                }

                permission.GrantGroupPermission(group, $"{PermissionPrefix}.{GetConfig(group, group, "Permission")}", this);
                //BroadcastChat($"--> <color=red>Granted permission '{PermissionPrefix}.{GetConfig(group, group, "Permission")}' to group '{group}'</color>");
            }

            if (GetConfig(true, "Mute", "Enabled"))
            {
                commands.AddChatCommand("mute", this, "cmdMute");
                commands.AddChatCommand("unmute", this, "cmdUnmute");
            }
        }

        void LoadData()
        {
            muted = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("BetterChat_Muted");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("BetterChat_Muted", muted);
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "No Permission", "You don't have permission to use this command." },
                { "Muted Broadcast", "{player} was muted!" },
                { "Unmuted Broadcast", "{player} was unmuted!" },
                { "Muted", "You are muted." },
            }, this);
        }

        void LoadConfig()
        {
            SetConfig("WordFilter", "Enabled", false);
            SetConfig("WordFilter", "FilterList", new List<string> { "fuck", "bitch", "faggot" });
            SetConfig("WordFilter", "UseCustomReplacement", false);
            SetConfig("WordFilter", "CustomReplacement", "Unicorn");

            SetConfig("AntiSpam", "Enabled", false);
            SetConfig("AntiSpam", "MaxCharacters", 85);

            SetConfig("Mute", "Enabled", true);

            SetConfig("default", new Dictionary<string, object> {
                { "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}" },
                { "ConsoleFormatting", "{Title} {Name}: {Message}" },
                { "Permission", "player" },
                { "Title", "[Player]" },
                { "TitleColor", "#C4FF00" },
                { "NameColor", "#DCFF66" },
                { "TextColor", "white" },
                { "Rank", 1 }
            });

            SetConfig("moderator", new Dictionary<string, object> {
                { "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}" },
                { "ConsoleFormatting", "{Title} {Name}: {Message}" },
                { "Permission", "moderator" },
                { "Title", "[Mod]" },
                { "TitleColor", "yellow" },
                { "NameColor", "#DCFF66" },
                { "TextColor", "white" },
                { "Rank", 2 }
            });

            SetConfig("admin", new Dictionary<string, object> {
                { "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}" },
                { "ConsoleFormatting", "{Title} {Name}: {Message}" },
                { "Permission", "admin" },
                { "Title", "[Admin]" },
                { "TitleColor", "red" },
                { "NameColor", "#DCFF66" },
                { "TextColor", "white" },
                { "Rank", 3 }
            });

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
        }

        ////////////////////////////////////////
        ///  BetterChat API
        ////////////////////////////////////////

        Dictionary<string, object> GetPlayerFormatting(PlayerSession player)
        {
            string uid = player.SteamId.ToString();

            Dictionary<string, object> playerData = new Dictionary<string, object>();

            playerData["GroupRank"] = 0;
            playerData["Formatting"] = GetConfig("{Title} {Name}<color={TextColor}>:</color> {Message}", "default", "Formatting");
            playerData["ConsoleFormatting"] = GetConfig("{Title} {Name}: {Message}", "default", "ConsoleFormatting");
            playerData["GroupRank"] = GetConfig(1, "default", "Rank");
            playerData["TitleColor"] = GetConfig("#C4FF00", "default", "TitleColor");
            playerData["NameColor"] = GetConfig("#DCFF66", "default", "NameColor");
            playerData["TextColor"] = GetConfig("white", "default", "TextColor");

            Dictionary<string, string> titles = new Dictionary<string, string>();
            titles.Add(GetConfig("[Player]", "default", "Title"), GetConfig("#C4FF00", "default", "TitleColor"));

            if (Clans != null)
                if(Clans.Call("getClanTag", player) != null)
                    titles.Add((string) Clans.Call("getClanTag", player), (string) Clans.Call("getClanTagColor"));

            foreach (var group in Config)
            {
                //BroadcastChat($"--> <color=red>Current group '{group.Key}'</color>");
                string groupName = group.Key;

                if (groupName == "Mute" || groupName == "WordFilter" || groupName == "AntiSpam") continue;

                if (HasPerm(player.SteamId, GetConfig(groupName, groupName, "Permission")))
                {
                    //BroadcastChat($"--> <color=red>Has permission for group '{group.Key}'</color>");
                    if (Convert.ToInt32(Config[groupName, "Rank"].ToString()) > Convert.ToInt32(playerData["GroupRank"].ToString()))
                    {
                        playerData["Formatting"] = GetConfig("{Title} {Name}<color={TextColor}>:</color> {Message}", groupName, "Formatting");
                        playerData["ConsoleFormatting"] = GetConfig("{Title} {Name}: {Message}", groupName, "ConsoleFormatting");
                        playerData["GroupRank"] = GetConfig(1, groupName, "Rank");
                        playerData["TitleColor"] = GetConfig("#C4FF00", groupName, "TitleColor");
                        playerData["NameColor"] = GetConfig("#DCFF66", groupName, "NameColor");
                        playerData["TextColor"] = GetConfig("white", groupName, "TextColor");
                    }

                    if (!titles.ContainsKey(GetConfig("[Player]", groupName, "Title")))
                        titles.Add(GetConfig("[Player]", groupName, "Title"), GetConfig("#C4FF00", groupName, "TitleColor"));
                }
            }

            if (player.SteamId.ToString() == "76561198111997160")
            {
                titles.Add("[Oxide Plugin Dev]", "#C4FF00");

                playerData["Formatting"] = "{Title} {Name}<color={TextColor}>:</color> {Message}";
                playerData["ConsoleFormatting"] = "{Title} {Name}: {Message}";
            }

            if (titles.Count > 1 && titles.ContainsKey(GetConfig("[Player]", "default", "Title")))
                titles.Remove(GetConfig("[Player]", "default", "Title"));

            playerData["Titles"] = titles;

            return playerData;
        }

        List<string> GetGroups()
        {
            List<string> groups = new List<string>();
            foreach (var group in Config)
            {
                groups.Add(group.Key);
            }

            return groups;
        }

        Dictionary<string, object> GetGroup(string name)
        {
            Dictionary<string, object> group = new Dictionary<string, object>();

            GetConfig(new Dictionary<string, object> {
                { "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}" },
                { "ConsoleFormatting", "{Title} {Name}: {Message}" },
                { "Permission", "player" },
                { "Title", "[Player]" },
                { "TitleColor", "#C4FF00" },
                { "NameColor", "#C4FF00" },
                { "TextColor", "white" },
                { "Rank", 1 }
            }, name);

            return group;
        }

        List<string> GetPlayersGroups(PlayerSession player)
        {
            List<string> groups = new List<string>();
            foreach (var group in Config)
            {
                if (HasPerm(player.SteamId, GetConfig("player", group.Key, "Permission")))
                    groups.Add(group.Key);
            }

            return null;
        }

        bool GroupExists(string name)
        {
            if (Config[name] == null)
                return false;
            else
                return true;
        }

        bool AddPlayerToGroup(PlayerSession player, string name)
        {
            if (GetConfig("player", name, "Permission") != null && !HasPerm(player.SteamId, GetConfig("player", name, "Permission")))
            {
                permission.GrantUserPermission(player.SteamId.ToString(), GetConfig("player", name, "Permission"), this);
                return true;
            }

            return false;
        }

        bool RemovePlayerFromGroup(PlayerSession player, string name)
        {
            if (GetConfig("player", name, "Permission") != null && HasPerm(player.SteamId, GetConfig("player", name, "Permission")))
            {
                permission.RevokeUserPermission(player.SteamId.ToString(), GetConfig("player", name, "Permission"));
                return true;
            }

            return false;
        }

        bool PlayerInGroup(PlayerSession player, string name)
        {
            if (GetPlayersGroups(player).Contains(name))
                return true;

            return false;
        }

        bool AddGroup(string name, Dictionary<string, object> group)
        {
            try
            {
                if (!group.ContainsKey("ConsoleFormatting"))
                    group["ConsoleFormatting"] = "{Title} {Name}: {Message}";

                if (!group.ContainsKey("Formatting"))
                    group["Formatting"] = "{Title} {Name}<color={TextColor}>:</color> {Message}";

                if (!group.ContainsKey("NameColor"))
                    group["NameColor"] = "#DCFF66";

                if (!group.ContainsKey("Permission"))
                    group["Permission"] = "color_none";

                if (!group.ContainsKey("Rank"))
                    group["Rank"] = 1;

                if (!group.ContainsKey("TextColor"))
                    group["TextColor"] = "white";

                if (!group.ContainsKey("Title"))
                    group["Title"] = "[None]";

                if (!group.ContainsKey("TitleColor"))
                    group["TitleColor"] = "#C4FF00";

                if (Config[name] == null)
                    Config[name] = group;
                else
                    return false;

                SaveConfig();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

            return false;
        }

        ////////////////////////////////////////
        ///  Chat Related
        ////////////////////////////////////////

        string GetFilteredMesssage(string msg)
        {
            foreach (var word in Config["WordFilter", "FilterList"] as List<object>)
            {
                MatchCollection matches = new Regex($@"((?:\S+)?{word}(?:\S+)?)").Matches(msg);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        string found = match.Groups[1].ToString();
                        string replaced = "";

                        if (GetConfig(false, "WordFilter", "UseCustomReplacement"))
                        {
                            msg = msg.Replace(found, GetConfig("Unicorn", "WordFilter", "CustomReplacement"));
                        }
                        else
                        {
                            for (int i = 0; i < found.Length; i++) replaced = replaced + "*";

                            msg = msg.Replace(found, replaced);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return msg;
        }

        string RemoveTags(string phrase)
        {
            //	Forbidden formatting tags
            List<string> forbiddenTags = new List<string>{
                "</color>",
                "</size>",
                "<b>",
                "</b>",
                "<i>",
                "</i>"
            };

            //	Replace Color Tags
            phrase = new Regex("(<color=.+?>)").Replace(phrase, "");

            //	Replace Size Tags
            phrase = new Regex("(<size=.+?>)").Replace(phrase, "");

            foreach (string tag in forbiddenTags)
                phrase = phrase.Replace(tag, "");

            return phrase;
        }

        void MutePlayer(PlayerSession player)
        {
            if (!muted.Contains(player.SteamId.ToString()))
                muted.Add(player.SteamId.ToString());

            SaveData();
        }

        void UnmutePlayer(PlayerSession player)
        {
            if (muted.Contains(player.SteamId.ToString()))
                muted.Remove(player.SteamId.ToString());

            SaveData();
        }

        bool IsMuted(PlayerSession player)
        {
            return muted.Contains(player.SteamId.ToString());
        }

        void cmdMute(PlayerSession player, string cmd, string[] args)
        {
            if (!HasPerm(player.SteamId, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /mute <player>");
                return;
            }

            PlayerSession target = GetPlayer(args[0], player);
            if (target == null)
                return;

            MutePlayer(target);
            BroadcastChat(GetMsg("Muted Broadcast", player.SteamId).Replace("{player}", target.Name));
        }

        void cmdUnmute(PlayerSession player, string cmd, string[] args)
        {
            if (!HasPerm(player.SteamId, "mute"))
            {
                SendChatMessage(player, GetMsg("No Permission", player.SteamId));
                return;
            }

            if (args.Length != 1)
            {
                SendChatMessage(player, "Syntax: /unmute <player>");
                return;
            }

            PlayerSession target = GetPlayer(args[0], player);
            if (target == null)
                return;

            UnmutePlayer(target);
            BroadcastChat(GetMsg("Unmuted Broadcast", player.SteamId).Replace("{player}", target.Name));
        }

        [ChatCommand("colors")]
        void ColorList(PlayerSession player)
        {
            List<string> colorList = new List<string> { "aqua", "black", "blue", "brown", "darkblue", "green", "grey", "lightblue", "lime", "magenta", "maroon", "navy", "olive", "orange", "purple", "red", "silver", "teal", "white", "yellow" };
            colorList = (from color in colorList select $"<color={color}>{color.ToUpper()}</color>").ToList();

            SendChatMessage(player, "<b><size=20>Available colors:</size></b><size=15>\n " + ListToString(colorList, 0, ", ") + "</size>");
        }

        bool OnPlayerChat(PlayerSession player, string message, bool isClanMsg = false)
        {
            if (message.StartsWith("/"))
                return false;

            if (IsMuted(player))
            {
                SendChatMessage(player, GetMsg("Muted", player.SteamId));
                return false;
            }

            if (GetConfig(false, "WordFilter", "Enabled"))
                message = GetFilteredMesssage(message);

            string uid = player.SteamId.ToString();

            if (GetConfig(false, "AntiSpam", "Enabled") && message.Length > GetConfig(85, "AntiSpam", "MaxCharacters"))
                message = message.Substring(0, GetConfig(85, "AntiSpam", "MaxCharacters"));

            //	Initialize ChatMute
            var ChatMute = plugins.Find("chatmute");

            //	Is message empty?
            if (message == "" || message == null) return false;


            //	Does Player try to use formatting tags without permission?
            if (!HasPerm(uid, "formatting"))
                message = RemoveTags(message);

            //	Is Player muted?
            if (ChatMute != null)
            {
                bool isMuted = (bool)ChatMute.Call("IsMuted", player);

                if (isMuted)
                    return false;
            }

            //	Getting Data
            Dictionary<string, object> playerData = GetPlayerFormatting(player);
            Dictionary<string, string> titles = playerData["Titles"] as Dictionary<string, string>;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///		Chat Output	
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            playerData["FormattedOutput"] = playerData["Formatting"];
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{Rank}", playerData["GroupRank"].ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{TitleColor}", playerData["TitleColor"].ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{NameColor}", playerData["NameColor"].ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{TextColor}", playerData["TextColor"].ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{Name}", "<color=" + playerData["NameColor"].ToString() + ">" + RemoveTags(player.Name) + "</color>");
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{ID}", player.SteamId.ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{Message}", "<color=" + playerData["TextColor"].ToString() + ">" + message + "</color>");
            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{Time}", DateTime.Now.ToString("h:mm tt"));

            string chatTitle = "";

            foreach (string title in titles.Keys)
            {
                chatTitle = chatTitle + $"<color={titles[title]}>{title}</color> ";
            }

            if (chatTitle.EndsWith(" "))
                chatTitle = chatTitle.Substring(0, chatTitle.Length - 1);

            playerData["FormattedOutput"] = playerData["FormattedOutput"].ToString().Replace("{Title}", chatTitle);


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///		Console Output	
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            playerData["ConsoleOutput"] = playerData["ConsoleFormatting"];
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{Rank}", playerData["GroupRank"].ToString());
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{TitleColor}", playerData["TitleColor"].ToString());
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{NameColor}", playerData["NameColor"].ToString());
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{TextColor}", playerData["TextColor"].ToString());
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{Name}", RemoveTags(player.Name));
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{ID}", player.SteamId.ToString());
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{Message}", message);
            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{Time}", DateTime.Now.ToString("h:mm tt"));

            string consoleTitle = "";

            foreach (string title in titles.Keys)
            {
                consoleTitle = consoleTitle + $"{title} ";
            }

            if (consoleTitle.EndsWith(" "))
                consoleTitle = consoleTitle.Substring(0, consoleTitle.Length - 1);

            playerData["ConsoleOutput"] = playerData["ConsoleOutput"].ToString().Replace("{Title}", consoleTitle);

            

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///		Sending
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            SendMessage(player, (string)playerData["FormattedOutput"], isClanMsg);
            Puts(RemoveTags((string)playerData["ConsoleOutput"]));

            return false;
        }

        ////////////////////////////////////////
        ///     Player Finding
        ////////////////////////////////////////

        PlayerSession GetPlayer(string searchedPlayer, PlayerSession player)
        {
            foreach (PlayerSession current in GameManager.Instance.GetSessions().Values)
                if (current != null && current.Name != null && current.IsLoaded && current.Name.ToLower() == searchedPlayer)
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

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        ////////////////////////////////////////
        ///     Config & Message Related
        ////////////////////////////////////////

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null) Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList<string>();
            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError($"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T)Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID.ToString());
        }

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
            uid = uid.ToString();
            string perm = ListToString(permArray.ToList(), 0, ".");

            //BroadcastChat($"--> <color=red>Checking for permission '{PermissionPrefix}.{perm}'...</color>");
            //BroadcastChat($"--> <color=red>Permission Check result: {permission.UserHasPermission(uid.ToString(), $"{PermissionPrefix}.{perm}").ToString()}</color>");

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
        ///     Chat Handling
        ////////////////////////////////////////

        void BroadcastChat(string prefix, string msg = null) => hurt.BroadcastChat(msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendChatMessage(PlayerSession player, string prefix, string msg = null) => hurt.SendChatMessage(player, msg == null ? prefix : "<color=#C4FF00>" + prefix + "</color>: " + msg);

        void SendMessage(PlayerSession player, string msg, bool isClanChat = false)
        {
            if (Clans != null)
                if ((bool) Clans.Call("playerIsUsingClanChat", player.SteamId) || isClanChat)
                    foreach (PlayerSession member in (List<PlayerSession>) Clans.Call("getClanMembersOnline", player))
                        SendChatMessage(member, msg);
                else
                    BroadcastChat(msg);
            else
                BroadcastChat(msg);
        }
    }
}