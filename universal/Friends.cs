#region License
/*
 Copyright (c) 2016 dcode [battlelink.io] and contributors

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 documentation files (the "Software"), to deal in the Software without restriction, including without limitation
 the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
 and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all copies or substantial portions
 of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
 TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 DEALINGS IN THE SOFTWARE.

 See: https://github.com/BattleLink/Friends for details
*/
#endregion

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

interface IBattleLinkFriends // BattleLink integration interface for reference
{
    event    Action<string, string>    OnFriendAddedInternal;
    event    Action<string, string>    OnFriendRemovedInternal;

    int      GetMaxFriendsInternal     ();
    string   GetPlayerNameInternal     (string playerId);
    bool     HasFriendInternal         (string playerId, string friendId);
    bool     AreFriendsInternal        (string playerId, string friendId);
    bool     AddFriendInternal         (string playerId, string friendId);
    bool     RemoveFriendInternal      (string playerId, string friendId);
    string[] GetFriendsInternal        (string playerId);
    string[] GetFriendsReverseInternal (string playerId);
}

namespace Oxide.Plugins
{
    [Info("Friends", "dcode", "2.5.0", ResourceId = 2120)]
    [Description("Universal friends plugin.")]
    public class Friends : CovalencePlugin, IBattleLinkFriends
    {
        #region Config

        class ConfigData
        {// Do not edit! These are the defaults. Edit oxide/config/Friends.json instead!

            public int  MaxFriends = 30;
            public bool DisableFriendlyFire = false;

            public bool EnableFriendChat = false;
            public bool LimitFriendChatToMutualFriends = true;
            public bool EnablePrivateChat = false;

            public bool SendOnlineNotification = true;
            public bool SendOfflineNotification = true;
            public bool SendAddedNotification = true;
            public bool SendRemovedNotification = false;
#if RUST
            public RustConfigData Rust = new RustConfigData();
#endif
        }

#if RUST
        class RustConfigData
        {
            public bool ShareCodeLocks = false;
            public bool ShareAutoTurrets = false;
        }
#endif

        ConfigData configData;

        void loadConfig() => configData = Config.ReadObject<ConfigData>();

        protected override void LoadDefaultConfig() => Config.WriteObject(configData = new ConfigData(), true);

        #endregion

        #region Language

        void registerMessages()
        {
            // English [en]
            lang.RegisterMessages(new Dictionary<string, string> {

                // Command replies
                { "PlayerNotFound", "There is no player matching that name." },
                { "NotOnFriendlist", "You don't have a friend matching that name." },
                { "FriendAdded", "[b]{0}[/b] is now one of your friends." },
                { "FriendRemoved", "[b]{0}[/b] is no longer one of your friends." },
                { "AlreadyAFriend", "[b]{0}[/b] is already one of your friends." },
                { "CantAddSelf", "You cannot add yourself to your friends." },
                { "NoFriends", "You haven't added any friends, yet." },
                { "List", "You have [b]{0}[/b] friends ({1} max.):" },
                { "List1", "You have [b]{0}[/b] friend ({1} max.):" },
                { "ListOnline", "[#6cce24](online)[/#]" },
                { "FriendlistFull", "You have already reached the maximum number of friends." },
                { "MultipleMatches", "There are multiple players matching that name. Either try to be more precise or use your friend's unique player id instead." },

                // Chat
                { "FriendChatTag", "[b][#78b1ff](Friends)[/#][/b]" },
                { "FriendChatCount", "{0} friends" },
                { "FriendChatCount1", "{0} friend" },
                { "PrivateChatTag", "[b][#6cce24](Private)[/#][/b]" },
                { "ChatSent", "To [b]{0}[/b]: {1}" },
                { "ChatReceived", "[b]{0}[/b]: {1}" },

                // Notifications
                { "FriendAddedNotification", "[b]{0}[/b] added you as a friend." },
                { "FriendRemovedNotification", "[b]{0}[/b] removed you as a friend." },
                { "FriendOnlineNotification", "[b]{0}[/b] is now online!" },
                { "FriendOfflineNotification", "[b]{0}[/b] is now offline." },

                // Usage text
                { "UsageAdd", "Type [#ffd479]/addfriend <name...>[/#] to add a friend" },
                { "UsageRemove", "Type [#ffd479]/removefriend <name...>[/#] to remove a friend" },
                { "UsageFriendChat", "Type [#ffd479]/fm <message...>[/#] to send a message to all of your friends" },
                { "UsagePrivateChat", "Type [#ffd479]/pm \"<name...>\" <message...>[/#] to send a private message" },
                { "UsageReplyChat", "Type [#ffd479]/rm <message...>[/#] to reply to the last message received" },
                { "HelpText", "Type [#ffd479]/friends[/#] to manage your friends" }

            }, this, "en");

            // Deutsch [de]
            lang.RegisterMessages(new Dictionary<string, string> {

                // Command replies
                { "PlayerNotFound", "Es gibt keinen Spieler unter diesem Namen." },
                { "NotOnFriendlist", "Auf deiner Freundeliste befindet sich kein Spieler mit diesem Namen." },
                { "FriendAdded", "[b]{0}[/b] ist nun einer deiner Freunde." },
                { "FriendRemoved", "[b]{0}[/b] ist nun nicht mehr dein Freund." },
                { "AlreadyAFriend", "[b]{0}[/b] ist bereits dein Freund." },
                { "CantAddSelf", "Du kannst dich nicht selbst als Freund hinzufÃ¼gen." },
                { "NoFriends", "Du hast noch keine Freunde hinzugefÃ¼gt." },
                { "List", "Du hast [b]{0}[/b] Freunde (max. {1}):" },
                { "List1", "Du hast [b]{0}[/b] Freund (max. {1}):" },
                { "ListOnline", "[#6cce24](online)[/#]" },
                { "FriendlistFull", "Du hast bereits die maximale Anzahl an Freunden erreicht." },
                { "MultipleMatches", "Es gibt mehrere Spieler, deren Name zu diesem passt. Versuche entweder prÃ¤ziser zu sein oder verwende die eindeutige Spieler-ID deines Freundes." },

                // Chat
                { "FriendChatTag", "[b][#78b1ff](Freunde)[/#][/b]" },
                { "FriendChatCount", "{0} Freunde" },
                { "FriendChatCount1", "{0} Freund" },
                { "PrivateChatTag", "[b][#6cce24](Privat)[/#][/b]" },
                { "ChatSent", "An [b]{1}[/b]: {2}" },
                { "ChatReceived", "[b]{1}[/b]: {2}" },

                // Notifications
                { "FriendAddedNotification", "[b]{0}[/b] hat dich als Freund hinzugefÃ¼gt." },
                { "FriendRemovedNotification", "[b]{0}[/b] hat dich als Freund entfernt." },
                { "FriendOnlineNotification", "[b]{0}[/b] ist jetzt online!" },
                { "FriendOfflineNotification", "[b]{0}[/b] ist jetzt offline." },

                // Usage text
                { "UsageAdd", "Schreibe [#ffd479]/addfriend <Name...>[/#] um Freunde hinzuzufÃ¼gen" },
                { "UsageRemove", "Schreibe [#ffd479]/removefriend <Name...>[/#] um Freunde zu entfernen" },
                { "UsageFriendChat", "Schreibe [#ffd479]/fm <Nachricht...>[/#] um eine Nachricht an alle Freunde zu senden" },
                { "UsagePrivateChat", "Schreibe [#ffd479]/pm \"<Name...>\" <Nachricht...>[/#] um eine private Nachricht zu senden" },
                { "UsageReplyChat", "Schreibe [#ffd479]/rm <Nachricht...>[/#] um auf die letzte erhaltene Nachricht zu antworten" },
                { "HelpText", "Schreibe [#ffd479]/friends[/#] um deine Freunde zu verwalten" }

            }, this, "de");

        }

        string _(string key, IPlayer recipient) => covalence.FormatText(lang.GetMessage(key, this, recipient.Id));
        string _(string key, string recipientId) => covalence.FormatText(lang.GetMessage(key, this, recipientId));

        #endregion

        #region Persistence

        class PlayerData
        {
            public string Name;
            public HashSet<string> Friends;
        }

        Dictionary<string, PlayerData> friendsData;

        Dictionary<string, HashSet<string>> reverseFriendsData;

        void loadData() => friendsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>(Name);

        Timer saveDataBatchedTimer = null;

        // Collects all save calls within delay and saves once there are no more updates.
        void saveData(float delay = 3f)
        {
            if (saveDataBatchedTimer == null)
                saveDataBatchedTimer = timer.Once(delay, saveDataImmediate);
            else
                saveDataBatchedTimer.Reset(delay);
        }

        void saveDataImmediate()
        {
            if (saveDataBatchedTimer != null)
            {
                saveDataBatchedTimer.DestroyToPool();
                saveDataBatchedTimer = null;
            }
            Interface.Oxide.DataFileSystem.WriteObject(Name, friendsData);
        }

        #endregion

        #region Helpers

        readonly static string[] emptyStringArray = new string[0];

        readonly static IDictionary<Type, Array> emptyTypedArrays = new Dictionary<Type, Array>() { };

        readonly static Type stringType = typeof(string);

        static Array makeTypedArray(Type type)
        {
            if (type == stringType)
                throw new ArgumentException("the string type should never be added", "type");
            Array emptyArray;
            if (emptyTypedArrays.TryGetValue(type, out emptyArray))
                return emptyArray;
            emptyTypedArrays.Add(type, emptyArray = Array.CreateInstance(type, 0));
            return emptyArray;
        }

        static Array makeTypedArray(Type type, ICollection<string> stringCollection)
        {
            var size = stringCollection.Count;
            if (size == 0)
                return makeTypedArray(type);
            var array = Array.CreateInstance(type, size);
            var index = 0;
            foreach (var value in stringCollection)
                array.SetValue(Convert.ChangeType(value, type), index++);
            return array;
        }

        IPlayer findPlayer(string nameOrId, out bool multipleMatches)
        {
            multipleMatches = false;

            // First pass: Check for unique player id
            {
                var player = covalence.Players.FindPlayerById(nameOrId);
                if (player != null)
                    return player;
            }

            // Second pass: Check for exact name
            IPlayer found = null;
            foreach (var player in covalence.Players.All)
            {
                if (player.Name == nameOrId)
                {
                    if (found != null)
                    {
                        multipleMatches = true;
                        return found;
                    }
                    found = player;
                }
            }
            if (found != null)
                return found;

            // Third pass: Check for partial name
            foreach (var player in covalence.Players.All)
            {
                if (player.Name.Contains(nameOrId))
                {
                    if (found != null)
                    {
                        multipleMatches = true;
                        return found;
                    }
                    found = player;
                }
            }
            return found;
        }

        HashSet<string> registeredCommands = new HashSet<string>();

        void registerCommand(string cmd, Action<IPlayer, string, string[]> callback)
        {
            if (registeredCommands.Contains(cmd))
                return;
            covalence.RegisterCommand(cmd, this, (caller, cmd_, args) => {
                callback(caller, cmd_, args);
                return true;
            });
            registeredCommands.Add(cmd);
        }

        void unregisterCommands()
        {
            foreach (var cmd in registeredCommands)
                covalence.UnregisterCommand(cmd, this);
            registeredCommands.Clear();
        }

        #endregion

        #region Hooks

        // Object references to boolean values used to return object from short if statements.
        readonly object @true = true;
        readonly object @false = false;

        void Loaded()
        {
            loadConfig();
            loadData();
            registerMessages();
            reverseFriendsData = new Dictionary<string, HashSet<string>>();
            if (friendsData == null)
                friendsData = new Dictionary<string, PlayerData>();
            else
            {
                foreach (var playerId in friendsData.Keys.ToArray())
                {
                    var playerData = friendsData[playerId];

                    // To be sure, in case the data file has been edited manually:
                    if (playerData == null || playerData.Name == null || playerData.Friends == null)
                    {
                        PrintWarning("Skipping invalid PlayerData record #{0}", playerId);
                        friendsData.Remove(playerId);
                        continue;
                    }

                    // Rebuild reverse friends
                    foreach (var friendId in playerData.Friends)
                    {
                        HashSet<string> reverseFriendData;
                        if (reverseFriendsData.TryGetValue(friendId, out reverseFriendData))
                            reverseFriendData.Add(playerId);
                        else
                            reverseFriendsData.Add(friendId, new HashSet<string>() { playerId });
                    }
                }
            }
            if (configData.EnablePrivateChat) {
                registerCommand("pm", cmdPrivateChat);
                registerCommand("m" , cmdPrivateChat);
            }
            if (configData.EnableFriendChat) {
                registerCommand("fm", cmdFriendChat);
                registerCommand("f" , cmdFriendChat);
            }
            if (configData.EnablePrivateChat || configData.EnableFriendChat) {
                registerCommand("rm", cmdReplyChat);
                registerCommand("r" , cmdReplyChat);
            }
        }

        void Unload()
        {
            unregisterCommands();
            if (saveDataBatchedTimer != null)
                saveDataImmediate();
        }

        void OnUserConnected(IPlayer player)
        {
            // Update the player's remembered name if necessary
            PlayerData data;
            if (friendsData.TryGetValue(player.Id, out data))
            {
                if (player.Name != data.Name)
                {
                    data.Name = player.Name;
                    saveData();
                }
                // Send online notifications if enabled
                if (configData.SendOnlineNotification)
                {
                    foreach (var friendId in data.Friends)
                    {
                        var friend = covalence.Players.FindPlayerById(friendId);
                        if (friend != null && friend.IsConnected)
                            friend.Message(_("FriendOnlineNotification", friend), player.Name);
                    }
                }
            }
            else
            {
                friendsData.Add(player.Id, new PlayerData() { Name = player.Name, Friends = new HashSet<string>() });
                saveData();
            }
        }

        void OnUserDisconnected(IPlayer player)
        {
            // Send offline notifications if enabled
            PlayerData data;
            if (configData.SendOnlineNotification && friendsData.TryGetValue(player.Id, out data))
                foreach (var friendId in data.Friends)
                {
                    var friend = covalence.Players.FindPlayerById(friendId);
                    if (friend != null && friend.IsConnected)
                        friend.Message(_("FriendOfflineNotification", friend), player.Name);
                }
        }

        [Command("friends")]
        void cmdFriends(IPlayer player, string command, string[] args)
        {
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            PlayerData data;
            int count;
            if (friendsData.TryGetValue(player.Id, out data) && (count = data.Friends.Count) > 0)
            {
                List<string> onlineList = new List<string>(configData.MaxFriends);
                List<string> offlineList = new List<string>(configData.MaxFriends);
                player.Reply(_(count == 1 ? "List1" : "List", player), count, configData.MaxFriends);
                foreach (var friendId in data.Friends)
                {
                    // Sort friends by online status and name (must be mutual friends to show online status)
                    var friend = covalence.Players.FindPlayerById(friendId);
                    if (friend != null)
                    {
                        if (friend.IsConnected && HasFriend(friend.Id, player.Id))
                            onlineList.Add(friend.Name);
                        else
                            offlineList.Add(friend.Name);
                    }
                    else
                    {
                        PlayerData friendData;
                        if (friendsData.TryGetValue(friendId, out friendData))
                            offlineList.Add(friendData.Name);
                        else
                            offlineList.Add("#" + friendId);
                    }
                }
                onlineList.Sort((a, b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase));
                var onlineText = _("ListOnline", player);
                foreach (var friendName in onlineList)
                    player.Message(friendName + " " + onlineText);
                onlineList.Clear();
                offlineList.Sort((a, b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase));
                foreach (var friendName in offlineList)
                    player.Message(friendName);
                offlineList.Clear();
            }
            else
                player.Reply(_("NoFriends", player));
            player.Message(_("UsageAdd", player));
            player.Message(_("UsageRemove", player));
            if (configData.EnableFriendChat)
                player.Message(_("UsageFriendChat", player));
            if (configData.EnablePrivateChat)
                player.Message(_("UsagePrivateChat", player));
            if (configData.EnableFriendChat || configData.EnablePrivateChat)
                player.Message(_("UsageReplyChat", player));
        }

        [Command("addfriend")]
        void cmdAddFriend(IPlayer player, string command, string[] args)
        {
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            if (args.Length < 1)
            {
                player.Reply(_("UsageAdd", player));
                return;
            }
            var nameOrId = string.Join(" ", args);
            bool multipleMatches;
            var friend = findPlayer(nameOrId, out multipleMatches);
            if (friend == null)
            {
                player.Reply(_("PlayerNotFound", player));
                return;
            }
            else if (multipleMatches)
            {
                player.Reply(_("MultipleMatches", player));
                return;
            }
            if (friend.Id == player.Id)
            {
                player.Reply(_("CantAddSelf", player));
                return;
            }
            PlayerData data;
            if (configData.MaxFriends < 1 || (friendsData.TryGetValue(player.Id, out data) && data.Friends.Count >= configData.MaxFriends))
                player.Reply(_("FriendlistFull", player));
            else if (AddFriend(player.Id, friend.Id))
                player.Reply(_("FriendAdded", player), friend.Name);
            else
                player.Reply(_("AlreadyAFriend", player), friend.Name);
        }

        [Command("removefriend", "deletefriend")]
        void cmdRemoveFriend(IPlayer player, string command, string[] args)
        {
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            if (args.Length < 1)
            {
                player.Reply(_("UsageRemove", player));
                return;
            }
            var name = string.Join(" ", args);
            bool multipleMatches;
            var friend = findPlayer(name, out multipleMatches);
            if (friend == null)
                player.Reply(_("PlayerNotFound", player));
            else if (multipleMatches)
                player.Reply(_("MultipleMatches", player));
            else if (RemoveFriend(player.Id, friend.Id))
                player.Reply(_("FriendRemoved", player), friend.Name);
            else
                player.Reply(_("NotOnFriendlist", player));
        }

        readonly Dictionary<string, string> replyTo = new Dictionary<string, string>();

        void cmdFriendChat(IPlayer player, string command, string[] args)
        {
            if (!configData.EnableFriendChat)
                return;
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            if (args.Length < 1)
            {
                player.Reply(_("UsageFriendChat", player));
                return;
            }
            var message = string.Join(" ", args).Trim();
            if (message.Length == 0)
            {
                player.Reply(_("UsageFriendChat", player));
                return;
            }
            PlayerData data;
            if (!friendsData.TryGetValue(player.Id, out data) || data.Friends.Count == 0)
            {
                player.Reply(_("NoFriends", player));
                return;
            }
            int recipientCount = 0;
            foreach (var friendId in data.Friends)
            {
                var friend = covalence.Players.FindPlayerById(friendId);
                if (friend != null && friend.IsConnected)
                {
                    PlayerData friendData;
                    if (!configData.LimitFriendChatToMutualFriends || (friendsData.TryGetValue(friend.Id, out friendData) && friendData.Friends.Contains(player.Id)))
                    {
                        friend.Message(_("FriendChatTag", friend) + " " + _("ChatReceived", friend),
                            player.Name,
                            message
                        );
                        replyTo[friend.Id] = player.Id;
                        ++recipientCount;
                    }
                }
            }
            player.Reply(_("FriendChatTag", player) + " " + _("ChatSent", player),
                string.Format(_(recipientCount == 1 ? "FriendChatCount1" : "FriendChatCount", player), recipientCount),
                message
            );
        }

        readonly static Regex leadingDoubleQuotedNameEx = new Regex("^\"(?:\\?.)*?\"", RegexOptions.Compiled);

        void cmdPrivateChat(IPlayer player, string command, string[] args)
        {
            if (!configData.EnablePrivateChat)
                return;
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            var message = string.Join(" ", args).Trim();
            if (message.Length == 0)
            {
                player.Reply(_("UsagePrivateChat", player));
                return;
            }
            string name;
            var match = leadingDoubleQuotedNameEx.Match(message);
            if (match.Success)
            {
                name = message.Substring(match.Index + 1, match.Length - 2).Trim();
                message = message.Substring(match.Index + match.Length).Trim();
            }
            else
            {
                var index = message.IndexOf(' ');
                if (index < 0)
                {
                    player.Reply(_("UsagePrivateChat", player));
                    return;
                }
                name = message.Substring(0, index).Trim();
                message = message.Substring(index + 1).Trim();
            }
            if (name.Length == 0 || message.Length == 0)
            {
                player.Reply(_("UsagePrivateChat", player));
                return;
            }
            bool multipleMatches;
            var recipient = findPlayer(name, out multipleMatches);
            if (recipient == null || !recipient.IsConnected || recipient.Id == player.Id)
            {
                player.Reply(_("PlayerNotFound", player));
                return;
            }
            else if (multipleMatches)
            {
                player.Reply(_("MultipleMatches", player));
                return;
            }
            recipient.Message(_("PrivateChatTag", recipient) + " " + _("ChatReceived", recipient),
                player.Name,
                message
            );
            replyTo[recipient.Id] = player.Id;
            player.Message(_("PrivateChatTag", player) + " " + _("ChatSent", player),
                recipient.Name,
                message
            );
        }

        void cmdReplyChat(IPlayer player, string command, string[] args)
        {
            if (!(configData.EnablePrivateChat || configData.EnableFriendChat))
                return;
            if (player.Id == "server_console")
            {
                player.Reply("This command cannot be used from the server console.");
                return;
            }
            var message = string.Join(" ", args).Trim();
            if (message.Length == 0)
            {
                player.Reply(_("UsageReplyChat", player));
                return;
            }
            string recipientId;
            if (!replyTo.TryGetValue(player.Id, out recipientId))
                return;
            var recipient = covalence.Players.FindPlayerById(recipientId);
            if (recipient == null)
            {
                player.Reply(_("PlayerNotFound", player));
                return;
            }
            recipient.Message(_("PrivateChatTag", recipient) + " " + _("ChatReceived", recipient),
                player.Name,
                message
            );
            replyTo[recipient.Id] = player.Id;
            player.Message(_("PrivateChatTag", player) + " " + _("ChatSent", player),
                recipient.Name,
                message
            );
        }

        #region Game: Rust

#if RUST
        // See: http://oxidemod.org/plugins/helptext.676/
        void SendHelpText(BasePlayer player) => player.ChatMessage(_("HelpText", player.userID.ToString()));

        // Cancels targeting if ShareAutoTurrets is enabled and target is a friend of the turret's owner.
        object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            BasePlayer player;
            return configData.Rust.ShareAutoTurrets
                && (player = (target as BasePlayer)) != null
                && HasFriend(turret.OwnerID, player.userID)
                ? @false
                : null;
        }

        // Cancels the attack if DisableFriendlyFire is enabled and victim is a friend of the attacker.
        object OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            BasePlayer victim;
            return configData.DisableFriendlyFire
                && (victim = (hit.HitEntity as BasePlayer)) != null
                && attacker != victim
                && HasFriend(attacker.userID, victim.userID)
                ? @false
                : null;
        }

        // Allows door usage if ShareCodeLocks is enabled and player is a friend of the door's owner.
        object CanUseDoor(BasePlayer player, BaseLock codeLock)
        {
            ulong ownerId;
            return configData.Rust.ShareCodeLocks
                && (codeLock is CodeLock)
                && (ownerId = codeLock.GetParentEntity().OwnerID) > 0
                && HasFriend(ownerId.ToString(), player.userID.ToString())
                ? @true
                : null;
        }
#endif

        #endregion

        #endregion

        #region BattleLink Integration

        public event Action<string, string> OnFriendAddedInternal;

        public event Action<string, string> OnFriendRemovedInternal;

        public int GetMaxFriendsInternal() => configData.MaxFriends;

        public string GetPlayerNameInternal(string playerId)
        {
            var iplayer = covalence.Players.FindPlayerById(playerId);
            if (iplayer == null)
            {
                PlayerData data;
                if (friendsData.TryGetValue(playerId, out data))
                    return data.Name;
                else
                    return "#" + playerId;
            }
            if (!friendsData.ContainsKey(iplayer.Id))
            {
                friendsData.Add(iplayer.Id, new PlayerData() { Name = iplayer.Name, Friends = new HashSet<string>() });
                saveData();
            }
            return iplayer.Name;
        }

        public bool HasFriendInternal(string playerId, string friendId)
        {
            PlayerData data;
            return friendsData.TryGetValue(playerId, out data) && data.Friends.Contains(friendId);
        }

        public bool AreFriendsInternal(string playerId, string friendId)
        {
            PlayerData playerData, friendData;
            return friendsData.TryGetValue(playerId, out playerData)
                && friendsData.TryGetValue(friendId, out friendData)
                && playerData.Friends.Contains(friendId)
                && friendData.Friends.Contains(playerId);
        }

        public bool AddFriendInternal(string playerId, string friendId)
        {
            var player = covalence.Players.FindPlayerById(playerId);
            if (player == null)
                return false;
            string friendName = null;
            var friendOrNull = covalence.Players.FindPlayerById(friendId);
            if (friendOrNull == null)
            {
                PlayerData friendData;
                if (friendsData.TryGetValue(friendId, out friendData))
                    friendName = friendData.Name;
                else
                    return false;
            }
            else
                friendName = friendOrNull.Name;
            PlayerData data;
            if (friendsData.TryGetValue(player.Id, out data))
            {
                if (data.Friends.Count >= configData.MaxFriends || !data.Friends.Add(friendId))
                    return false;
            }
            else
                data = friendsData[player.Id] = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() { friendId } };
            if (!friendsData.TryGetValue(friendId, out data)) // also add a blank reverse entry remembering the friend's name
                friendsData[friendId] = new PlayerData() { Name = friendName, Friends = new HashSet<string>() };
            saveData();
            HashSet<string> reverseFriendData;
            if (reverseFriendsData.TryGetValue(friendId, out reverseFriendData))
                reverseFriendData.Add(player.Id);
            else
                reverseFriendsData.Add(friendId, new HashSet<string>() { player.Id });
            if (configData.SendAddedNotification && friendOrNull != null && friendOrNull.IsConnected)
            {
                friendOrNull.Message(_("FriendAddedNotification", friendOrNull), player.Name);
                friendOrNull.Message(_("UsageAdd", friendOrNull));
            }
            if (OnFriendAddedInternal != null)
                OnFriendAddedInternal(player.Id, friendId);
            Interface.Oxide.NextTick(() => {
                Interface.Oxide.CallHook("OnFriendAdded", player.Id, friendId);
            });
            return true;
        }

        public bool RemoveFriendInternal(string playerId, string friendId)
        {
            var player = covalence.Players.FindPlayerById(playerId);
            if (player == null)
                return false;
            var friendOrNull = covalence.Players.FindPlayerById(friendId);
            PlayerData data;
            if (friendsData.TryGetValue(playerId, out data) && data.Friends.Remove(friendId))
            {
                saveData();
                HashSet<string> reverseFriendData;
                if (reverseFriendsData.TryGetValue(friendId, out reverseFriendData))
                {
                    reverseFriendData.Remove(playerId);
                    if (reverseFriendData.Count == 0)
                        reverseFriendsData.Remove(friendId);
                }
                if (configData.SendRemovedNotification && friendOrNull != null && friendOrNull.IsConnected)
                    friendOrNull.Message(_("FriendRemovedNotification", friendOrNull), data.Name);
                if (OnFriendRemovedInternal != null)
                    OnFriendRemovedInternal(player.Id, friendId);
                Interface.Oxide.NextTick(() => {
                    Interface.Oxide.CallHook("OnFriendRemoved", player.Id, friendId);
                });
                return true;
            }
            return false;
        }

        public string[] GetFriendsInternal(string playerId)
        {
            PlayerData data;
            return friendsData.TryGetValue(playerId.ToString(), out data) && data.Friends.Count > 0
                ? data.Friends.ToArray()
                : emptyStringArray;
        }

        public string[] GetFriendsReverseInternal(string playerId)
        {
            HashSet<string> reverseFriendData;
            return reverseFriendsData.TryGetValue(playerId.ToString(), out reverseFriendData) && reverseFriendData.Count > 0
                ? reverseFriendData.ToArray()
                : emptyStringArray;
        }

        #endregion

        #region API

        // Returns the maximum number of friends allowed per player.
        int GetMaxFriends() => GetMaxFriendsInternal();

        // Gets player's current or remembered name, by id.
        string GetPlayerName(object playerId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            return GetPlayerNameInternal(playerId.ToString());
        }

        // Tests if player added friend to their friends list, by id.
        bool HasFriend(object playerId, object friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            return HasFriendInternal(playerId.ToString(), friendId.ToString());
        }

        // Tests if player and friend are mutual friends, by id.
        bool AreFriends(object playerId, object friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            return AreFriendsInternal(playerId.ToString(), friendId.ToString());
        }

        // Adds friend to player's friends list, by id.
        bool AddFriend(object playerId, object friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            return AddFriendInternal(playerId.ToString(), friendId.ToString());
        }

        // Removes friend from player's friends list, by id.
        bool RemoveFriend(object playerId, object friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            return RemoveFriendInternal(playerId.ToString(), friendId.ToString());
        }

        // Gets an array of player's friends, by id.
        object GetFriends(object playerId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (playerId is string)
                return GetFriendsInternal(playerId as string);
            PlayerData data;
            return friendsData.TryGetValue(playerId.ToString(), out data) && data.Friends.Count > 0 
                ? makeTypedArray(playerId.GetType(), data.Friends)
                : makeTypedArray(playerId.GetType());
        }

        // Gets an array of players who have added friend to their friends list, by id.
        object GetFriendsReverse(object playerId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (playerId is string)
                return GetFriendsReverseInternal(playerId as string);
            HashSet<string> reverseFriendData;
            return reverseFriendsData.TryGetValue(playerId.ToString(), out reverseFriendData) && reverseFriendData.Count > 0
                ? makeTypedArray(playerId.GetType(), reverseFriendData)
                : makeTypedArray(playerId.GetType());
        }

        #region Cmpatibility layer for http://oxidemod.org/plugins/friends-api.686/

        bool AddFriendS(string playerId, string friendId) => AddFriend(playerId, friendId);
        bool RemoveFriendS(string playerId, string friendId) => HasFriend(playerId, friendId);
        bool HasFriendS(string playerId, string friendId) => HasFriend(playerId, friendId);
        bool AreFriendsS(string playerId, string friendId) => AreFriends(playerId, friendId);
        bool IsFriend(ulong playerId, ulong friendId) => HasFriend(friendId, playerId);
        bool IsFriendS(string playerId, string friendId) => HasFriend(friendId, playerId);
        object GetFriendList(ulong playerId) => GetFriends(playerId);
        object GetFriendListS(string playerId) => GetFriends(playerId);
        object IsFriendOf(ulong friendId) => GetFriendsReverse(friendId);
        object IsFriendOfS(string friendId) => GetFriendsReverse(friendId);

        #endregion

        #endregion
    }
}
