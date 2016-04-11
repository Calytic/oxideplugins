using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System;                      //DateTime
using System.Data;
using System.Linq;
using System.Collections.Generic;  //Required for Whilelist
using UnityEngine;
using Rust;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("RustIO: Friends List", "Alphawar", "1.1.1", ResourceId = 1734)]
    [Description("Plugin designed work with RustIOs Friend list")]
    class RustIOFriendListAPI : RustPlugin
    {

        private bool DebugMode = false;
        private string ChatPrefixColor = "008800";
        private string ChatPrefix = "Server";

        #region Rust:IO Bindings

        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;

        void Loaded()
        {
            lang.RegisterMessages(messages, this);
        }
        private void InitializeRustIO()
        {
            lib = Interface.GetMod().GetLibrary<Library>("RustIO");
            if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (hasFriend = lib.GetFunction("HasFriend")) == null || (addFriend = lib.GetFunction("AddFriend")) == null || (deleteFriend = lib.GetFunction("DeleteFriend")) == null)
            {
                lib = null;
                Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!");
            }
        }

        private bool IsInstalled()
        {
            if (lib == null) return false;
            return (bool)isInstalled.Invoke(lib, new object[] { });
        }

        private bool HasFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)hasFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool AddFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool DeleteFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)deleteFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        #endregion

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            InitializeRustIO();
        }

        #region Rust:Chat Functions
        
        void BroadcastToChat(string msg)
        {
            PrintToChat($"<color={ChatPrefixColor}>{ChatPrefix}</color>: {msg}");
        }
        void ChatMessageHandler(BasePlayer player, string message)
        {
            PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}");
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////
        // Compatability Hooks ///////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        public bool areFriends(string _Player1, string _Player2)
        {
            if (string.IsNullOrEmpty(_Player1) || string.IsNullOrEmpty(_Player2)) return false;
            Puts("The areFriends code is being called"); //debug
            bool test = HasFriend(_Player1, _Player2);
            bool test1 = HasFriend(_Player2, _Player1);
            if (test && test1)
            {
                Puts("returning true"); //debug
                return true;
            }
            return false;
        }
        public bool AreFriends(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            bool _test2 = HasFriend(_Player2ID, _Player1ID);
            if (_test1 && _test2)
            {
                Puts("returning true"); //debug
                return true;
            }
            return false;
        }
        private bool AreFriendsS(string _Player1, string _Player2)
        {
            if (string.IsNullOrEmpty(_Player1) || string.IsNullOrEmpty(_Player2)) return false;
            bool _test1 = HasFriend(_Player1, _Player2);
            bool _test2 = HasFriend(_Player2, _Player1);
            if (_test1 && _test2)
            {
                Puts("returning true"); //debug
                return true;
            }
            return false;
        }
        public bool HasFriendCompatability(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            bool _test2 = HasFriend(_Player2ID, _Player1ID);
            if (_test1 && _test2)
            {
                Puts("returning true"); //debug
                return true;
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // New Hooks /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [HookMethod("ANDFriends")]
        private bool ANDFriends(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            bool _test2 = HasFriend(_Player2ID, _Player1ID);
            if (_test1 && _test2)
            {
                Puts("returning true"); //debug
                return true;
            }
            return false;
        }
        [HookMethod("ORFriends")]
        bool ORFriends(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            bool _test2 = HasFriend(_Player2ID, _Player1ID);
            if (_test1)
            {
                Puts("returning true"); //debug
                return true;
            }
            else if (_test2)
            {
                Puts("returning true"); //debug
                return true;
            }
            else return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // String Hooks //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [HookMethod("ANDFriendsS")]
        private bool ANDFriendsS(string _Player1, string _Player2)
        {
            if (string.IsNullOrEmpty(_Player1) || string.IsNullOrEmpty(_Player2)) return false;
            bool _test1 = HasFriend(_Player1, _Player2);
            bool _test2 = HasFriend(_Player2, _Player1);
            if (_test1) { Puts("returning true"); return true; }
            else if (_test2) { Puts("returning true"); return true; }
            else return false;
        }
        [HookMethod("ORFriendsS")]
        private bool ORFriendsS(string _Player1, string _Player2)
        {
            if (string.IsNullOrEmpty(_Player1) || string.IsNullOrEmpty(_Player2)) return false;
            bool _test1 = HasFriend(_Player1, _Player2);
            bool _test2 = HasFriend(_Player2, _Player1);
            if (_test1){Puts("returning true");return true;}
            else if (_test2){Puts("returning true");return true;}
            else return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Friend Control Hooks //////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("friend")]
        void friendFunction(BasePlayer player, string cmd, string[] args)
        {
            bool result = false;
            string FriendsNameID = null;
            if (args == null || args.Length == 0)
            {
                ChatMessageHandler(player, lang.GetMessage("FriendHelp", this, player.UserIDString));
                return;
            }
            if (!IsInstalled())
            {
                ChatMessageHandler(player, lang.GetMessage("RustIOMissing", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                case "+":
                    if (args.Length == 1)
                    {
                        ChatMessageHandler(player, lang.GetMessage("ToADD", this, player.UserIDString));
                        return;
                    }
                    FriendsNameID = getFullName(args[1]).UserIDString;
                    if (FriendsNameID == player.UserIDString)
                    {
                        ChatMessageHandler(player, lang.GetMessage("YouYou", this, player.UserIDString));
                        return;
                    }
                    result = AddFriend(player.UserIDString, FriendsNameID);
                    if (result == true)
                    {
                        ChatMessageHandler(player, lang.GetMessage("PlayerAdded", this, player.UserIDString));
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("PlayerNotAdded", this, player.UserIDString));
                    }
                    return;

                case "remove":
                case "-":
                    if (args.Length == 1)
                    {
                        ChatMessageHandler(player, lang.GetMessage("ToRemove", this, player.UserIDString));
                        return;
                    }
                    FriendsNameID = getFullName(args[1]).UserIDString;
                    if (FriendsNameID == player.UserIDString)
                    {
                        ChatMessageHandler(player, lang.GetMessage("RemoveSelf", this, player.UserIDString));
                        return;
                    }
                    result = DeleteFriend(player.UserIDString, FriendsNameID);
                    if (result == true)
                    {
                        ChatMessageHandler(player, lang.GetMessage("PlayerRemoved", this, player.UserIDString));
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("PlayerNotRemoved", this, player.UserIDString));
                    }
                    return;

                case "check":
                case "?":
                    if (args.Length == 1)
                    {
                        ChatMessageHandler(player, lang.GetMessage("ToCheck", this, player.UserIDString));
                        return;
                    }
                    FriendsNameID = getFullName(args[1]).UserIDString;
                    if (FriendsNameID == player.UserIDString)
                    {
                        ChatMessageHandler(player, lang.GetMessage("YouYou", this, player.UserIDString));
                        return;
                    }
                    result = HasFriend(player.UserIDString, FriendsNameID);
                    if (result == true)
                    {
                        ChatMessageHandler(player, lang.GetMessage("YouFriend", this, player.UserIDString));
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("YouHFriend", this, player.UserIDString));
                    }
                    result = HasFriend(FriendsNameID, player.UserIDString);
                    if (result == true)
                    {
                        ChatMessageHandler(player, lang.GetMessage("TheyFriend", this, player.UserIDString));
                    }
                    else
                    {
                        ChatMessageHandler(player, lang.GetMessage("TheyHFriend", this, player.UserIDString));
                    }
                    return;

                case "help":
                    ChatMessageHandler(player, lang.GetMessage("ToADD", this, player.UserIDString));
                    ChatMessageHandler(player, lang.GetMessage("ToRemove", this, player.UserIDString));
                    ChatMessageHandler(player, lang.GetMessage("ToCheck", this, player.UserIDString));
                    return;

                default:
                    ChatMessageHandler(player, lang.GetMessage("IncorrectFormat1", this, player.UserIDString));
                    ChatMessageHandler(player, lang.GetMessage("IncorrectFormat2", this, player.UserIDString));
                    ChatMessageHandler(player, lang.GetMessage("IncorrectFormat3", this, player.UserIDString));
                    return;
            }
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Part Name Handler//////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private BasePlayer getFullName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var allPlayers = BasePlayer.activePlayerList.ToArray();
            // Try to find an exact match first
            foreach (var p in allPlayers)
            {
                if (p.displayName == name)
                {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            if (player != null)
                return player;
            // Otherwise try to find a partial match
            foreach (var p in allPlayers)
            {
                if (p.displayName.ToLower().IndexOf(name) >= 0)
                {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            return player;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"IncorrectFormat1", "Incorrect Format, please use one of the following:" },
            {"IncorrectFormat2", "/friend <add|+> Name or /friend <remove|-> Name" },
            {"IncorrectFormat3", "/friend <check|?> Name  - To check if you are friends" },
            {"ToADD", "  To ADD a friend use:/friend <add|+> Name" },
            {"ToRemove", "  To REMOVE a friend use:/friend <remove|-> Name" },
            {"ToCheck", "  To CHECK if you are friends use: /friend <check|?> Name" },
            {"TheyFriend", "You have set them as a friend." },
            {"TheyHFriend", "They havent set you as a friend." },
            {"YouHFriend", "You havent set them as a friend." },
            {"YouFriend", "You have set them as a friend." },
            {"YouYou", "Why did you do you?" },
            {"PlayerNotRemoved", "Player Not removed, are you enemies already, use check command." },
            {"PlayerRemoved", "Player has been removed" },
            {"RemoveSelf", "why do you hate yourself, see theropy, from server <3." },
            {"PlayerNotAdded", "Player Not Added, are you friends already, use check command." },
            {"PlayerAdded", "Player Added."},
            {"FriendHelp", "Incorrect Command, Use /friend help" },
            {"RustIOMissing", "Rust:IO Does not seem to be installed" }
        };
    }
}