using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Loyalty", "Bamabo", "1.3.1")]
    [Description("Reward your players for play time with new permissions/usergroups")]

    class Loyalty : RustPlugin
    {
        Data data;

        #region Classes
        class Data
        {
            public Dictionary<ulong, Player> players = new Dictionary<ulong, Player>();
            public HashSet<UserGroup> usergroups = new HashSet<UserGroup>();
            public HashSet<LoyaltyReward> rewards = new HashSet<LoyaltyReward>();

            public Data() { }
        }

        public class LoyaltyReward
        {
            public string alias { get; set; }
            public string permission { get; set; }
            public uint requirement { get; set; }

            public LoyaltyReward() { alias = null; permission = null; requirement = 0; }

            public LoyaltyReward(string alias, string permission, uint requirement = 0)
            {
                this.alias = alias;
                this.permission = permission;
                this.requirement = requirement;
            }
        }
        public class Player
        {
            public string name { get; set; }
            public ulong id { get; set; }
            public uint loyalty { get; set; }
            public string group { get; set; }

            public Player() { }

            public Player(ulong id, string name, uint loyalty = 0, string group = "")
            {
                this.id = id;
                this.name = name;
                this.loyalty = loyalty;
                this.group = group;
            }
            public override bool Equals(object obj)
            {
                Player pItem = obj as Player;
                return pItem.GetHashCode() == this.GetHashCode();
            }

            public override int GetHashCode()
            {
                return (int)this.id;
            }

        }
        public class UserGroup
        {
            public string usergroup { get; set; }
            public uint requirement { get; set; }

            public UserGroup() { usergroup = ""; requirement = 0; }
            public UserGroup(string usergroup, uint requirement = 0)
            {
                this.usergroup = usergroup;
                this.requirement = requirement;
            }
        }
        #endregion Classes

        #region Hooks
        void Init()
        {
            RegisterMessages();
            RegisterPermissions();
        }

        void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>("LoyaltyData");
            timer.Repeat(Convert.ToSingle(Config["rate"]), 0, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if ((player.IsAdmin() && (bool)Config["allowAdmin"]) || !player.IsAdmin())
                    {
                        if (!data.players.ContainsKey(player.userID))
                            data.players.Add(player.userID, new Player(player.userID, player.displayName, 1, ""));
                        else
                        {
                            data.players[player.userID].name = player.displayName;
                            data.players[player.userID].loyalty += 1;
                            foreach (var reward in data.rewards)
                            {
                                if (data.players[player.userID].loyalty == reward.requirement)
                                {
                                    if (!reward.permission.StartsWith("\"-"))
                                    {
                                        rust.RunServerCommand("grant user " + rust.QuoteSafe(player.displayName) + " " + reward.permission);
                                        SendMessage(player, "accessGranted", reward.requirement, Config["serverName"].ToString(), reward.alias);
                                        if ((bool)Config["debug"])
                                            Puts("Player: " + player.displayName + " gained access to " + reward.permission + " by reaching " + reward.requirement + " loyalty points.");
                                    }
                                    else
                                    {
                                        rust.RunServerCommand("revoke user " + rust.QuoteSafe(player.displayName) + " " + reward.permission.Replace('-', ' ').Trim());
                                        SendMessage(player, "accessLostSpecific", reward.requirement, reward.alias);
                                        if ((bool)Config["debug"])
                                            Puts("Player: " + player.displayName + " lost access to " + reward.permission + " by reaching " + reward.requirement + " loyalty points.");
                                    }

                                }
                            }
                            foreach (var usergroup in data.usergroups)
                            {
                                if (data.players[player.userID].loyalty == usergroup.requirement)
                                {
                                    if (!usergroup.usergroup.StartsWith("\"-"))
                                    {
                                        rust.RunServerCommand("usergroup add " + rust.QuoteSafe(player.displayName) + " " + usergroup.usergroup);
                                        if (!String.IsNullOrEmpty(data.players[player.userID].group))
                                            rust.RunServerCommand("usergroup remove " + rust.QuoteSafe(player.displayName) + " " + data.players[player.userID].group);

                                        data.players[player.userID].group = usergroup.usergroup;
                                        SendMessage(player, "groupAssigned", usergroup.requirement, Config["serverName"].ToString(), usergroup.usergroup);
                                    }
                                    else
                                    {
                                        rust.RunServerCommand("usergroup remove " + rust.QuoteSafe(player.displayName) + " " + usergroup.usergroup.Replace('-', ' ').Trim());
                                    }

                                }
                            }
                        }
                    }
                }
                Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);
                if ((bool)Config["debug"])
                    Puts("Assigned every online player 1 loyalty point.");
            });
        }

        void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);
        }

        void OnPlayerInit(BasePlayer player)
        {

            if ((player.IsAdmin() && (bool)Config["allowAdmin"]) || !player.IsAdmin())
            {
                if (!data.players.ContainsKey(player.userID))
                {
                    data.players.Add(player.userID, new Player(player.userID, player.displayName, 0));
                    Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);
                }
                if ((bool)Config["debug"])
                    Puts("Player: " + player.displayName + " connected for the first time and got added into data.");
            }
        }

        #endregion Hooks

        #region MainCommand
        [ChatCommand("loyalty")]
        void CmdLoyalty(BasePlayer sender, string command, string[] args)
        {
            if (args.Length == 0)
                if (permission.UserHasPermission(sender.UserIDString, "loyalty.loyalty") || sender.IsAdmin())
                {
                    if (data.players.ContainsKey(sender.userID))
                        SendMessage(sender, "loyaltyCurrent", data.players[sender.userID].loyalty, Config["serverName"]);
                    else
                        SendErrorMessage(sender, "errorNoLoyalty");
                    return;
                }
                else
                    SendErrorMessage(sender, "accessDenied");

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "add":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.add") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length < 4)
                        {
                            SendErrorMessage(sender, "syntaxAdd");
                            return;
                        }
                        string alias = "";
                        for (int i = 3; i < args.Length; i++)
                            alias += args[i] + " ";
                        CmdAdd(sender, args[1], args[2], alias);
                        break;

                    case "remove":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.remove") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 2)
                        {
                            SendErrorMessage(sender, "syntaxRemove");
                            return;
                        }
                        CmdRemove(sender, args[1]);
                        break;
                    case "removeg":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.removegroup") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 2)
                        {
                            SendErrorMessage(sender, "syntaxRemoveGroup");
                            return;
                        }
                        CmdRemoveUserGroup(sender, args[1]);
                        break;
                    case "reset":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.reset") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 2)
                        {
                            SendErrorMessage(sender, "syntaxReset");
                            return;
                        }
                        CmdReset(sender, args[1]);
                        break;

                    case "set":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.set") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 3)
                        {
                            SendErrorMessage(sender, "syntaxSet");
                            return;
                        }
                        CmdSet(sender, args[1], args[2]);
                        break;
                    case "help":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.help") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 1)
                        {
                            SendErrorMessage(sender, "syntaxHelp");
                            return;
                        }
                        SendMessage(sender, "help");
                        break;

                    case "lookup":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.lookup") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 2)
                        {
                            SendErrorMessage(sender, "syntaxLookup");
                            return;
                        }
                        CmdLookup(sender, args[1]);
                        break;

                    case "top":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.top") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 1)
                        {
                            SendErrorMessage(sender, "syntaxTop");
                            return;
                        }
                        CmdTop(sender);
                        break;
                    case "addg":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.addgroup") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 3)
                        {
                            SendErrorMessage(sender, "syntaxAddGroup");
                            return;
                        }
                        CmdAddUserGroup(sender, args[1], args[2]);
                        break;
                    case "rewards":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.rewards") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 1)
                        {
                            SendErrorMessage(sender, "syntaxRewards");
                            return;
                        }
                        CmdRewards(sender);
                        break;
                    case "rewardsg":
                        if (!permission.UserHasPermission(sender.UserIDString, "loyalty.rewardsg") && !sender.IsAdmin())
                        {
                            SendErrorMessage(sender, "accessDenied");
                            return;
                        }
                        if (args.Length != 1)
                        {
                            SendErrorMessage(sender, "syntaxRewardsg");
                            return;
                        }
                        CmdRewardsg(sender);
                        break;
                    default:
                        SendErrorMessage(sender, "errorNoCommand", args[0]);
                        break;
                };
            }
        }
        #endregion MainCommand

        #region Subcommands

        void CmdAdd(BasePlayer sender, string req, string perm, string alias)
        {
            if (!Regex.IsMatch(req, "^\\d+$"))
            {
                SendErrorMessage(sender, "syntaxNotInt", 1);
                return;
            }

            if (RewardExists(rust.QuoteSafe(perm)))
            {
                SendErrorMessage(sender, "rewardExists", perm);
                return;
            }

            if (!permission.PermissionExists(perm.Trim('-')))
            {
                SendErrorMessage(sender, "unregisteredPerm", perm.Replace('-', ' ').Trim());
                return;
            }

            data.rewards.Add(new LoyaltyReward(rust.QuoteSafe(alias), rust.QuoteSafe(perm), Convert.ToUInt32(req, 10)));
            Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);

            SendMessage(sender, "successAdd", Convert.ToUInt32(req, 10), perm, alias);
        }
        void CmdRemove(BasePlayer sender, string permission)
        {
            if (!RewardExists(rust.QuoteSafe(permission)))
            {
                SendErrorMessage(sender, "rewardNoExist", permission);
                return;
            }
            foreach (LoyaltyReward reward in data.rewards)
                if (reward.permission == rust.QuoteSafe(permission))
                {
                    data.rewards.Remove(reward);
                    Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);
                    SendMessage(sender, "rewardRemoved", permission);
                    return;
                }
        }

        void CmdReset(BasePlayer sender, string playerName)
        {
            Player player = data.players.Values.FirstOrDefault(x => x.name.StartsWith(playerName, StringComparison.CurrentCultureIgnoreCase));
            if (player == null)
            {
                SendErrorMessage(sender, "errorPlayerNotFound", playerName);
                return;
            }

            data.players[player.id].loyalty = 0;
            foreach (var reward in data.rewards)
                rust.RunServerCommand("revoke user " + rust.QuoteSafe(player.name) + " " + reward.permission.Replace('-', ' ').Trim());
            foreach (var group in data.usergroups)
                rust.RunServerCommand("usergroup remove " + rust.QuoteSafe(player.name) + " " + group.usergroup.Replace('-', ' ').Trim());
            player.group = "";

            SendMessage(BasePlayer.FindByID(player.id), "loyaltyReset");
            SendMessage(sender, "successReset", player.name);
        }
        void CmdSet(BasePlayer sender, string playerName, string newLoyalty)
        {
            Player player = data.players.Values.FirstOrDefault(x => x.name.StartsWith(playerName, StringComparison.CurrentCultureIgnoreCase));

            if (player == null)
            {
                SendErrorMessage(sender, "errorPlayerNotFound", playerName);
                return;
            }
            if (!Regex.IsMatch(newLoyalty, "^\\d+$"))
            {
                SendErrorMessage(sender, "syntaxNotInt", 2);
                return;
            }

            uint newLoy = Convert.ToUInt32(newLoyalty, 10);

            foreach (var reward in data.rewards)
            {
                if (newLoy >= reward.requirement)
                {
                    if (reward.permission.StartsWith("\"-"))
                        rust.RunServerCommand("revoke user " + rust.QuoteSafe(player.name) + " " + reward.permission.Replace('-', ' ').Trim());
                    else
                        rust.RunServerCommand("grant user " + rust.QuoteSafe(player.name) + " " + reward.permission.Replace('-', ' ').Trim());

                }
                else
                {
                    rust.RunServerCommand("revoke user " + rust.QuoteSafe(player.name) + " " + reward.permission.Replace('-', ' ').Trim());
                }
            }
            SendMessage(BasePlayer.FindByID(player.id), "accessLost", newLoyalty);

            if (!String.IsNullOrEmpty(player.group) && player.group.Equals("") && player.group.Equals(" "))
            {
                rust.RunServerCommand("usergroup remove " + rust.QuoteSafe(player.name) + " " + player.group);
            }
            var newGroup = (from entry in data.usergroups where entry.requirement <= newLoy orderby entry.requirement descending select entry).FirstOrDefault();
            if (newGroup != null)
            {
                if (newGroup.usergroup.Trim().StartsWith("\"-"))
                {
                    rust.RunServerCommand("usergroup remove " + rust.QuoteSafe(player.name) + " " + newGroup.usergroup.Replace('-', ' ').Trim());
                    if (newGroup.usergroup.TrimStart('-') == data.players[player.id].group)
                        data.players[player.id].group = "";
                }
                else
                {
                    rust.RunServerCommand("usergroup add " + rust.QuoteSafe(player.name) + " " + newGroup.usergroup.Replace('-', ' ').Trim());
                    data.players[player.id].group = newGroup.usergroup;
                }
            }


            data.players[player.id].loyalty = newLoy;
            SendMessage(sender, "successSet", player.name, newLoy);

        }
        void CmdLookup(BasePlayer sender, string player)
        {
            Player lookUpPlayer = data.players.Values.FirstOrDefault(x => x.name.StartsWith(player, StringComparison.CurrentCultureIgnoreCase));
            if (lookUpPlayer != null)
                SendMessageFromID(sender, "entryLookup", lookUpPlayer.id, lookUpPlayer.name, data.players[lookUpPlayer.id].loyalty);
            else
            {
                SendErrorMessage(sender, "errorPlayerNotFound", player);
                return;
            }
        }
        void CmdTop(BasePlayer sender)
        {
            var topList = (from entry in data.players orderby entry.Value.loyalty descending select entry).Take(10);
            int counter = 0;
            SendMessage(sender, "topMessage", topList.Count(), data.players.Count());

            foreach (var entry in topList)
                SendMessageFromID(sender, "entryTop", entry.Value.id, ++counter, entry.Value.name, entry.Value.loyalty);
        }

        void CmdRewards(BasePlayer sender)
        {
            var rewards = (from entry in data.rewards orderby entry.requirement ascending where entry.requirement > data.players[sender.userID].loyalty select entry).Take(5);
            if (rewards.Count() > 0)
            {
                SendMessage(sender, "rewardsMessage", rewards.Count());
                foreach (var entry in rewards)
                    SendMessage(sender, "entryRewards", entry.requirement, entry.alias);
            }
            else
                SendMessage(sender, "rewardsNoMoreRewards");
        }
        void CmdRewardsg(BasePlayer sender)
        {
            var rewards = (from entry in data.usergroups orderby entry.requirement ascending where entry.requirement > data.players[sender.userID].loyalty select entry).Take(5);
            if (rewards.Count() > 0)
            {
                SendMessage(sender, "rewardsgMessage", rewards.Count());
                foreach (var entry in rewards)
                    SendMessage(sender, "entryRewards", entry.requirement, entry.usergroup);
            }
            else
                SendMessage(sender, "rewardsNoMoreRewards");
        }
        void CmdAddUserGroup(BasePlayer sender, string requirement, string usergroup)
        {
            if (!Regex.IsMatch(requirement, "^\\d+$"))
            {
                SendErrorMessage(sender, "syntaxNotInt", 1);
                return;
            }

            if (UserGroupExists(rust.QuoteSafe(usergroup)))
            {
                SendErrorMessage(sender, "groupExists", usergroup);
                return;
            }

            if (!permission.GroupExists(usergroup.TrimStart('-')))
            {
                SendErrorMessage(sender, "unregisteredGroup", usergroup.Replace('-', ' ').Trim());
                return;
            }

            data.usergroups.Add(new UserGroup(rust.QuoteSafe(usergroup), Convert.ToUInt32(requirement, 10)));
            Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);

            SendMessage(sender, "successAddGroup", Convert.ToUInt32(requirement, 10), rust.QuoteSafe(usergroup));
        }
        void CmdRemoveUserGroup(BasePlayer sender, string usergroup)
        {
            if (!UserGroupExists(rust.QuoteSafe(usergroup)))
            {
                SendErrorMessage(sender, "groupNoExists", usergroup);
                return;
            }
            foreach (UserGroup usergroupEntry in data.usergroups)
                if (usergroupEntry.usergroup == rust.QuoteSafe(usergroup))
                {
                    data.usergroups.Remove(usergroupEntry);
                    Interface.Oxide.DataFileSystem.WriteObject("LoyaltyData", data);
                    SendMessage(sender, "groupRemoved", usergroup);
                    return;
                }
        }

        #endregion Subcommands

        #region Helpers
        void SendMessage(BasePlayer receiver, string messageID, params object[] args)
        {
            string message;
            if (args.Length > 0)
            {
                object[] arr = new object[args.Length + 1];
                arr[0] = Config["colorHighlight"].ToString();
                for (int i = 1; i < args.Length + 1; i++)
                    arr[i] = args[i - 1];
                message = String.Format(lang.GetMessage(messageID, this), arr);
            }
            else
            {
                message = String.Format(lang.GetMessage(messageID, this), Config["colorHighlight"].ToString());
            }
            rust.SendChatMessage(receiver, "<color=" + Config["colorText"] + ">" + message + "</color>", null, Config["serverIconID"].ToString());
        }
        void SendErrorMessage(BasePlayer receiver, string messageID, params object[] args)
        {
            string message;
            if (args.Length > 0)
            {
                object[] arr = new object[args.Length + 1];
                arr[0] = Config["colorHighlight"].ToString();
                for (int i = 1; i < args.Length + 1; i++)
                    arr[i] = args[i - 1];
                message = String.Format(lang.GetMessage(messageID, this), arr);
            }
            else
            {
                message = String.Format(lang.GetMessage(messageID, this), Config["colorHighlight"].ToString());
            }
            rust.SendChatMessage(receiver, "<color=" + Config["colorError"] + ">" + message + "</color>", null, Config["serverIconID"].ToString());
        }
        void SendMessageFromID(BasePlayer receiver, string messageID, ulong senderID, params object[] args)
        {
            string message;
            if (args.Length > 0)
            {
                object[] arr = new object[args.Length + 1];
                arr[0] = Config["colorHighlight"].ToString();
                for (int i = 1; i < args.Length + 1; i++)
                    arr[i] = args[i - 1];
                message = String.Format(lang.GetMessage(messageID, this), arr);
            }
            else
            {
                message = String.Format(lang.GetMessage(messageID, this), Config["colorHighlight"].ToString());
            }
            rust.SendChatMessage(receiver, "<color=" + Config["colorText"] + ">" + message + "</color>", null, senderID.ToString());
        }
        string FormatMessage(string messageID, params object[] args)
        {
            return String.Format(lang.GetMessage(messageID, this), args);
        }

        bool RewardExists(string permission)
        {
            foreach (LoyaltyReward reward in data.rewards)
                if (reward.permission == permission)
                    return true;

            return false;
        }
        bool UserGroupExists(string usergroup)
        {
            foreach (UserGroup usergEntry in data.usergroups)
                if (usergEntry.usergroup == usergroup)
                    return true;

            return false;
        }

        void RegisterMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["syntaxAdd"] = "Too few or too many arguments. \nUse <color={0}>/loyalty add [int: req] [string: perm.perm] [string: /alias]</color>",
                ["syntaxRemove"] = "Too few or too many arguments. \nUse <color={0}>/loyalty remove [string: perm.perm]</color>",
                ["syntaxRemoveGroup"] = "Too few or too many arguments.\nUse <color={0}>/loyalty removeg [string: loyaltyGroup]</color>",
                ["syntaxSet"] = "Too few or too many arguments. \nUse <color={0}>/loyalty set [string: name] [int: loyaltyPoints]</color>",
                ["syntaxReset"] = "Too few or too many arguments. \nUse <color={0}>/loyalty reset [string: name]</color>",
                ["syntaxHelp"] = "Too few or too many arguments. \nUse <color={0}>/loyalty help</color>",
                ["syntaxLookup"] = "Too few or too many arguments. \nUse <color={0}>/loyalty lookup [string: name]</color>",
                ["syntaxTop"] = "Too few or too many arguments. \nUse <color={0}>/loyalty top</color>",
                ["syntaxRewards"] = "Too few or too many arguments.\nUse <color={0}>/loyalty rewards</color>",
                ["syntaxRewardsg"] = "Too few or too many arguments.\nUse <color={0}>/loyalty rewardsg</color>",
                ["syntaxAddGroup"] = "Too few or too many arguments. \nUse <color={0}>/loyalty addg [int: req] [string: group]</color>",
                ["syntaxNotInt"] = "Invalid syntax. Parameter <color={0}>#{1}</color> needs to be a positive integer.",
                ["rewardExists"] = "A reward for the permission <color={0}>{1}</color> already exists.",
                ["rewardNoExist"] = "No reward for the permission <color={0}>{1}</color> was found.",
                ["rewardRemoved"] = "Loyalty reward <color={0}>{1}</color> was successfully removed.",
                ["accessGranted"] = "Congratulations, by spending <color={0}>{1} minutes</color> on <color={0}>{2}</color> you have gained access to the command <color={0}>{3}</color>. Thank you for playing!",
                ["accessDenied"] = "You do not have access to that command.",
                ["accessLost"] = "Your loyalty was changed to <color={0}>{1}</color> by an admin. You have lost and/or gained access to loyalty rewards accordingly.",
                ["accessLostSpecific"] = "By reaching <color={0}>{1}</color> loyalty you have lost access to <color={0}>{2}</color>",
                ["loyaltyCurrent"] = "You have accumulated a total of <color={0}>{1}</color> loyalty points by playing on <color={0}>{2}</color>",
                ["loyaltyReset"] = "Your loyalty has been reset by an administrator. You have lost access to all commands and/or groups your previously had access to.",
                ["errorNoLoyalty"] = "You have not yet earned any loyalty points. Check again in a minute!",
                ["errorNoCommand"] = "No command <color={0}>{1}</color> was found.",
                ["errorPlayerNotFound"] = "No player by the name <color={0}>{1}</color> was found.",
                ["errorNoPlusMinus"] = "Your usergroup needs to start with <color={0}>'+'</color> or <color={0}>'-'</color>.",
                ["errorFatal"] = "FATAL ERROR. If you see this something has gone terribly wrong.",
                ["stylingMessage"] = "{0}",
                ["stylingSender"] = "<color=lime>{0}</color>",
                ["successSet"] = "Player <color={0}>{1}'s</color> loyalty points were successfully set to <color={0}>{2}</color>.",
                ["successReset"] = "Player <color={0}>{1}'s</color> loyalty points were successfully reset.",
                ["successAdd"] = "Permission reward: <color={0}>[req: {1}, perm: {2}, alias: {3}]</color> successfully added.",
                ["successAddGroup"] = "Usergroup reward: <color={0}>[req: {1}, usergroup: {2}]</color> successfully added.",
                ["topMessage"] = "Top <color={0}>{1}</color> most loyal players out of the total <color={0}>{2}</color>",
                ["entryReward"] = "Req: {1} Perm: {2} Alias: {3}",
                ["entryTop"] = "{1}. <color={0}>{2}</color> - {3}",
                ["entryLookup"] = "<color={0}>{1}</color> has accumulated a total of <color={0}>{2}</color> loyalty points.",
                ["entryRewards"] = "<color={0}>{1} - {2}</color>",
                ["groupExists"] = "A loyalty reward for the usergroup <color={0}>{1}</color> already exists.",
                ["groupNoExists"] = "No group reward called <color={0}>{1}</color> was found.",
                ["groupRemoved"] = "Group reward <color={0}>{1}</color> was successfully removed.",
                ["groupChanged"] = "Your loyalty was set to <color={0}>{1}</color> by an admin. Your current group is: <color={0}>{2}</color>",
                ["groupAssigned"] = "Congratulations, by spending <color={0}>{1} minutes</color> on <color={0}>{2}</color> you have been assigned to the usergroup <color={0}>{3}</color>. Thank you for playing!",
                ["groupRevoked"] = "By reaching <color={0}>{1}</color> loyalty you have been removed from the group <color={0}>{2}</color>.",
                ["rewardsMessage"] = "Showing next <color={0}>{1}</color> loyalty rewards",
                ["rewardsgMessage"] = "Showing next <color={0}>{1}</color> usergroup rewards",
                ["rewardsNoMoreRewards"] = "There are no more rewards available for you to earn. Check again later!",
                ["unregisteredPerm"] = "No permission <color={0}>{1}</color> is registered by oxide. Make sure the plugin you are trying to add permissions for is loaded.",
                ["unregisteredGroup"] = "No usergroup <color={0}>{1}</color> is registered by oxide.",
                ["help"] = "<color={0}>Loyalty by Bamabo</color>\nLoyalty is a plugin that lets server owners reward their players with permissions according to how much time they've spent on the server. 1 Loyalty = 1 minute. \n<color={0}>/loyalty add/remove/set/reset/top/lookup/addg/removeg</color>\n More info and source on <color={0}>github.com/Hazzty/Loyalty</color>",
            }, this);
        }
        void RegisterPermissions()
        {
            permission.RegisterPermission("loyalty.loyalty", this);
            permission.RegisterPermission("loyalty.add", this);
            permission.RegisterPermission("loyalty.remove", this);
            permission.RegisterPermission("loyalty.reset", this);
            permission.RegisterPermission("loyalty.set", this);
            permission.RegisterPermission("loyalty.lookup", this);
            permission.RegisterPermission("loyalty.top", this);
            permission.RegisterPermission("loyalty.help", this);
            permission.RegisterPermission("loyalty.addgroup", this);
            permission.RegisterPermission("loyalty.removegroup", this);
            permission.RegisterPermission("loyalty.rewards", this);
            permission.RegisterPermission("loyalty.rewardsg", this);
        }

        #endregion Helpers

        #region Config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file for Loyalty");
            Config.Clear();
            Config["allowAdmin"] = true;
            Config["colorError"] = "red";
            Config["colorHighlight"] = "yellow";
            Config["colorText"] = "#FFFFFF";
            Config["debug"] = false;
            Config["rate"] = 60.0;
            Config["serverName"] = "Default Server";
            Config["serverIconID"] = "76561198314979344";
            SaveConfig();
        }
        #endregion Config

    }

}