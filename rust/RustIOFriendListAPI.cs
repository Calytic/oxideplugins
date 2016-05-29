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
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("RustIOFriendListAPI", "Alphawar", "1.4.0", ResourceId = 1734)]
    [Description("Plugin designed work with RustIOs Friend list")]
    class RustIOFriendListAPI : RustPlugin
    {
        void Loaded()
        {
            LoadVariables();
            loadPermissions();
            lang.RegisterMessages(messages, this);
        }
        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            InitializeRustIO();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // RustIO ////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;
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



        //////////////////////////////////////////////////////////////////////////////////////
        // Main Function /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("friend")]
        void friendFunction(BasePlayer _player, string cmd, string[] args)
        {
            //bool result = false;
            //string FriendsNameID = null;
            if (args == null || args.Length == 0)
            {
                ChatMessageHandler(_player, lang.GetMessage("FriendHelp", this, _player.UserIDString));
                return;
            }
            if (!IsInstalled())
            {
                ChatMessageHandler(_player, lang.GetMessage("RustIOMissing", this, _player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                case "+":
                    friendAdd(args, _player);
                    return;

                case "remove":
                case "-":
                    friendRemove(args, _player);
                    return;

                case "check":
                case "?":
                    friendCheck(args, _player);
                    return;

                case "help":
                    ChatMessageHandler(_player, lang.GetMessage("ToADD", this, _player.UserIDString));
                    ChatMessageHandler(_player, lang.GetMessage("ToRemove", this, _player.UserIDString));
                    ChatMessageHandler(_player, lang.GetMessage("ToCheck", this, _player.UserIDString));
                    return;

                case "admin":
                    Puts("im here admin");
                    friendAdmin(args, _player);
                    return;

                case "debug":

                    return;

                default:
                    ChatMessageHandler(_player, lang.GetMessage("IncorrectFormat1", this, _player.UserIDString));
                    ChatMessageHandler(_player, lang.GetMessage("IncorrectFormat2", this, _player.UserIDString));
                    ChatMessageHandler(_player, lang.GetMessage("IncorrectFormat3", this, _player.UserIDString));
                    return;
            }
        }

        void friendAdd (string[] _args, BasePlayer _player)
        {
            if (_args.Length == 1){
                ChatMessageHandler(_player, lang.GetMessage("ToADD", this, _player.UserIDString));
                return;}
            BasePlayer _result = FindPlayer(_args[1], _player);
            if (_result == null) return;
            string _targetID = _result.UserIDString;
            if (_targetID == _player.UserIDString){
                ChatMessageHandler(_player, lang.GetMessage("YouYou", this, _player.UserIDString));
                return;}
            bool result = AddFriend(_player.UserIDString, _targetID);
            if (result == true)ChatMessageHandler(_player, lang.GetMessage("PlayerAdded", this, _player.UserIDString), _result.displayName);
            else ChatMessageHandler(_player, lang.GetMessage("PlayerNotAdded", this, _player.UserIDString), _result.displayName);
        }

        void friendRemove(string[] _args, BasePlayer _player)
        {
            if (_args.Length == 1){
                ChatMessageHandler(_player, lang.GetMessage("ToRemove", this, _player.UserIDString));
                return;}
            BasePlayer _result = FindPlayer(_args[1], _player);
            if (_result == null) return;
            string _targetID = _result.UserIDString;
            if (_targetID == _player.UserIDString){
                ChatMessageHandler(_player, lang.GetMessage("RemoveSelf", this, _player.UserIDString));
                return;}
            bool result = DeleteFriend(_player.UserIDString, _targetID);
            if (result == true) ChatMessageHandler(_player, lang.GetMessage("PlayerRemoved", this, _player.UserIDString), _result.displayName);
            else ChatMessageHandler(_player, lang.GetMessage("PlayerNotRemoved", this, _player.UserIDString), _result.displayName);
        }

        void friendCheck(string[] _args, BasePlayer _player)
        {
            if (_args.Length == 1){
                ChatMessageHandler(_player, lang.GetMessage("ToADD", this, _player.UserIDString));
                return;}
            BasePlayer _result = FindPlayer(_args[1], _player);
            if (_result == null) return;
            string _targetID = _result.UserIDString;
            if (_targetID == _player.UserIDString){
                ChatMessageHandler(_player, lang.GetMessage("YouYou", this, _player.UserIDString));
                return;}
            bool result = HasFriend(_player.UserIDString, _targetID);
            if (result == true){
                ChatMessageHandler(_player, lang.GetMessage("YouFriend", this, _player.UserIDString), _result.displayName);}
            else{
                ChatMessageHandler(_player, lang.GetMessage("YouHFriend", this, _player.UserIDString), _result.displayName);}
            result = HasFriend(_targetID, _player.UserIDString);
            if (result == true){
                ChatMessageHandler(_player, lang.GetMessage("TheyFriend", this, _player.UserIDString), _result.displayName);}
            else{
                ChatMessageHandler(_player, lang.GetMessage("TheyHFriend", this, _player.UserIDString), _result.displayName);}
        }

        void friendAdmin(string[] _args, BasePlayer _player)
        {
            if (!permissionCheck(_player, "admin")) return;
            if ((_args[1] == "check" || _args[1] == "?") && (_args.Length == 4))
            {
                BasePlayer _target1 = FindPlayer(_args[2], _player);
                BasePlayer _target2 = FindPlayer(_args[3], _player);
                if (_target1 == null) return;
                if (_target2 == null) return;
                bool result = HasFriend(_target1.UserIDString, _target2.UserIDString);
                if (result == true){
                    ChatMessageHandler(_player, lang.GetMessage("AdminCheck1-2Y", this, _player.UserIDString), _target1.displayName, _target2.displayName);}
                else{
                    ChatMessageHandler(_player, lang.GetMessage("AdminCheck1-2N", this, _player.UserIDString), _target1.displayName, _target2.displayName);}
                result = HasFriend(_target2.UserIDString, _target1.UserIDString);
                if (result == true){
                    ChatMessageHandler(_player, lang.GetMessage("AdminCheck2-1Y", this, _player.UserIDString), _target2.displayName, _target1.displayName);}
                else{
                    ChatMessageHandler(_player, lang.GetMessage("AdminCheck2-1N", this, _player.UserIDString), _target2.displayName, _target1.displayName);}
            }
        }

        void DebugFunction (string[] _args, BasePlayer _player)
        {

        }

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
                //Puts("returning true"); //debug
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
                //Puts("returning true"); //debug
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
                //Puts("returning true"); //debug
                return true;
            }
            return false;
        }
        public bool HasFriendcmp(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            bool _test2 = HasFriend(_Player2ID, _Player1ID);
            if (_test1 && _test2)
            {
                //Puts("returning true"); //debug
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
                //Puts("returning true"); //debug
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
                //Puts("returning true"); //debug
                return true;
            }
            else if (_test2)
            {
                //Puts("returning true"); //debug
                return true;
            }
            else return false;
        }


        [HookMethod("chkFriend")]
        public bool chkFriend(ulong _Player1, ulong _Player2)
        {
            if ((_Player1 == 0) || (_Player2 == 0)) return false;
            string _Player1ID = Convert.ToString(_Player1);
            string _Player2ID = Convert.ToString(_Player2);
            bool _test1 = HasFriend(_Player1ID, _Player2ID);
            if (_test1)
            {
                //Puts("returning true"); //debug
                return true;
            }
            return false;
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
            if (_test1) { Puts("returning true"); return true; }
            else if (_test2) { Puts("returning true"); return true; }
            else return false;
        }


        //////////////////////////////////////////////////////////////////////////////////////
        // Other Functions ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////


        private BasePlayer FindPlayer(string _target, BasePlayer player)
        {
            var players = new List<BasePlayer>();
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(_target)) players.Add(activePlayer);
                else if (activePlayer.displayName.Contains(_target, CompareOptions.OrdinalIgnoreCase)) players.Add(activePlayer);
            }
            if (!(IsDigitsOnly(_target)) && (NamesIncludeSleepers == false)) Puts("Skipping sleepers(Add Debug message)");
            else
            {
                foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (sleepingPlayer.UserIDString.Equals(_target)) players.Add(sleepingPlayer);
                    else if (sleepingPlayer.displayName.Contains(_target, CompareOptions.OrdinalIgnoreCase)) players.Add(sleepingPlayer);
                }
            }
            if (players.Count <= 0)
            {
                ChatMessageHandler(player, lang.GetMessage("NotFound", this, player.UserIDString));
                return null;
            }
            if (players.Count > 1)
            {
                ChatMessageHandler(player, lang.GetMessage("MultiplePlayers", this, player.UserIDString));
                ChatMessageHandler(player, string.Join("<color=with>,</color> ", players.ConvertAll(p => p.displayName).ToArray()));
                return null;
            }
            return players[0];
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsDigit(c))
                {
                    Puts("Character Detected Returning false");
                    return false;
                }
            }
            Puts("Detected no Characters Returning true");
            return true;
        }

        
        //////////////////////////////////////////////////////////////////////////////////////
        // MessageHandles ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void ChatMessageHandler(BasePlayer player, string message, params object[] args)
        {
            PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: <color={ChatMessageColor}>{message}</color>", args);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Config ////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private bool DebugMode;
        private bool NamesIncludeSleepers;
        private string ChatPrefixColor;
        private string ChatPrefix;
        private string ChatMessageColor;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file!");
            Config.Clear();
            LoadVariables();
        }
        void LoadVariables() //Stores Default Values, calling GetConfig passing: menu, dataValue, defaultValue
        {
            //Booleans
            DebugMode = Convert.ToBoolean(GetConfig("Settings", "DebugMode", false));
            NamesIncludeSleepers = Convert.ToBoolean(GetConfig("Settings", "NamesIncludeSleepers", false));
            //Ints
            //Floats
            //Strings
            ChatPrefix = Convert.ToString(GetConfig("ChatSettings", "ChatPrefix", "FriendList:"));
            ChatPrefixColor = Convert.ToString(GetConfig("ChatSettings", "ChatPrefixColor", "008800"));
            ChatMessageColor = Convert.ToString(GetConfig("ChatSettings", "ChatMessageColor", "yellow"));
        }

        object GetConfig(string menu, string dataValue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }
            object value;
            if (!data.TryGetValue(dataValue, out value))
            {
                value = defaultValue;
                data[dataValue] = value;
            }
            return value;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Permision /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void loadPermissions()
        {
            string[] Permissionarray = { "admin", "debug" };
            foreach (string i in Permissionarray)
            {
                string regPerm = Title.ToLower() + "." + i;
                Puts("Checking if " + regPerm + " is registered.");
                if (!permission.PermissionExists(regPerm))
                {
                    permission.RegisterPermission(regPerm, this);
                    Puts(regPerm + " is registered.");
                }
                else
                {
                    Puts(regPerm + " is already registered.");
                }
            }
        }
        bool permissionCheck(BasePlayer _player, string i)
        {
            string regPerm = Title.ToLower() + "." + i;
            if (permission.UserHasPermission(_player.userID.ToString(), regPerm)) return true;
            return false;
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
            {"TheyFriend", "<color=orange>{0}</color> set u as a friend." },
            {"TheyHFriend", "<color=orange>{0}</color> havent set you as a friend." },
            {"YouHFriend", "You havent set <color=orange>{0}</color> as a friend." },
            {"YouFriend", "You have set <color=orange>{0}</color> as a friend." },
            {"YouYou", "Why did you do you?" },
            {"PlayerNotRemoved", "<color=orange>{0}</color> Not removed, are you enemies already, use check command." },
            {"PlayerRemoved", "<color=orange>{0}</color> has been removed" },
            {"RemoveSelf", "why do you hate yourself, see theropy, from server <3." },
            {"PlayerNotAdded", "<color=orange>{0}</color> Not Added, are you friends already, use check command." },
            {"PlayerAdded", "<color=orange>{0}</color> Added."},
            {"FriendHelp", "Incorrect Command, Use /friend help" },
            {"RustIOMissing", "Rust:IO Does not seem to be installed" },
            {"NotFound", "The specified player couldn't be found." },
            {"MultiplePlayers", "Found multiple players:" },
            {"AdminCheck1-2Y", "<color=orange>{0}</color> has added <color=orange>{1}</color>" },
            {"AdminCheck1-2N", "<color=orange>{0}</color> hasnt added <color=orange>{1}</color>" },
            {"AdminCheck2-1Y", "<color=orange>{0}</color> has added <color=orange>{1}</color>" },
            {"AdminCheck2-1N", "<color=orange>{0}</color> hasnt added <color=orange>{1}</color>" }
        };
    }
}