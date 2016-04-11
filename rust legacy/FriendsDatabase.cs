using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("FriendsDatabase", "Schwarz", "1.0.0")]
    class FriendsDatabase : RustLegacyPlugin
    {
        private Core.Configuration.DynamicConfigFile Data;
        void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("FriendsDatabase"); }
        void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("FriendsDatabase"); }
        void OnServerSave() { SaveData(); }
        void Unload() { SaveData(); }

        void Loaded()
        {  
            LoadData();
        }

        bool isFriend(string userid, string targetid)
        {
            if (Data[userid] == null) return false;
            return (Data[userid] as Dictionary<string, object>).ContainsKey(targetid);
        }

        public static string notFriends = "You have no friends.";
        public static string friendsList = "Your friends list:";
        public static string couldntFindPlayer = "Couldn't find the target player.";
        public static string removedFriend = "{0} was removed from your friends list.";
        public static string addedFriend = "{0} was added as your friend.";
        public static string notFriendWith = "You are not friend with: {0}";
        public static string alreadyFriend = "You are already friend with {0}";

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Messages: Not Friends", ref notFriends);
            CheckCfg<string>("Messages: Friends List", ref friendsList);
            CheckCfg<string>("Messages: No Player Found", ref couldntFindPlayer);
            CheckCfg<string>("Messages: Removed Friend", ref removedFriend);
            CheckCfg<string>("Messages: Added Friend", ref addedFriend);
            CheckCfg<string>("Messages: Already Friend With", ref alreadyFriend);
            CheckCfg<string>("Messages: Not Friend With", ref notFriendWith);

            SaveConfig();
        }

        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }
        [ChatCommand("unfriend")]
        void cmdChatUnfriend(NetUser netuser, string command, string[] args)
        {
            var playerdata = GetPlayerdata(netuser.playerClient.userID.ToString());
            if (args.Length == 0)
            {
                if (playerdata.Count == 0)
                {
                    SendReply(netuser, notFriends);
                    return;
                }
                SendReply(netuser, friendsList);
                foreach (KeyValuePair<string, object> pair in playerdata)
                {
                    SendReply(netuser, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
                }
                return;
            }
            string targetname = string.Empty;
            string targetid = string.Empty;
            foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if (pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0])
                {
                    targetname = pair.Value.ToString();
                    targetid = pair.Key.ToString();
                }
            }
            if (targetid == string.Empty)
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser == null)
                {
                    SendReply(netuser, couldntFindPlayer);
                    return;
                }
                targetname = targetuser.displayName;
                targetid = targetuser.playerClient.userID.ToString();
            }
            if (playerdata.ContainsKey(targetid))
            {
                SendReply(netuser, string.Format(removedFriend, targetname.ToString()));
                playerdata.Remove(targetid);
                return;
            }
            SendReply(netuser, string.Format(notFriendWith, targetname.ToString()));
        }
        [ChatCommand("addfriend")]
        void cmdChatAddFriend(NetUser netuser, string command, string[] args)
        {
            var playerdata = GetPlayerdata(netuser.playerClient.userID.ToString());
            if (args.Length == 0)
            { 
                if(playerdata.Count == 0)
                {
                    SendReply(netuser, notFriends);
                    return;
                }
                SendReply(netuser, friendsList);
                foreach (KeyValuePair<string, object> pair in playerdata)
                {
                    SendReply(netuser, string.Format("{0} - {1}", pair.Key, pair.Value.ToString()));
                }
                return;
            }
            string targetname = string.Empty;
            string targetid = string.Empty;
            foreach (KeyValuePair<string, object> pair in playerdata)
            {
                if(pair.Value.ToString() == args[0] || pair.Key.ToString() == args[0])
                {
                    targetname = pair.Value.ToString();
                    targetid = pair.Key.ToString();
                }
            }
            if (targetid == string.Empty)
            {
                NetUser targetuser = rust.FindPlayer(args[0]);
                if (targetuser == null)
                {
                    SendReply(netuser, couldntFindPlayer);
                    return;
                }
				if (targetuser == netuser)
				{
					SendReply(netuser, "You can't add yourself as a friend.");
					return;
				}
                targetname = targetuser.displayName;
                targetid = targetuser.playerClient.userID.ToString();
            }
            if(playerdata.ContainsKey(targetid))
            {
                SendReply(netuser, string.Format(alreadyFriend, targetname.ToString()));
                return;
            } 
            playerdata.Add(targetid, targetname);
            SendReply(netuser, string.Format(addedFriend, playerdata[targetid].ToString()));
        }
    }
}