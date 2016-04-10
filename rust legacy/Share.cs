using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("Share", "Reneb", "1.0.1")]
    class Share : RustLegacyPlugin
    {
        private Core.Configuration.DynamicConfigFile Data;
        void LoadData() { Data = Interface.GetMod().DataFileSystem.GetDatafile("ShareDatabase"); }
        void SaveData() { Interface.GetMod().DataFileSystem.SaveDatafile("ShareDatabase"); }
        void OnServerSave() { SaveData(); }
        void Unload() { SaveData(); }

        void Loaded()
        {  
            LoadData();
        }

        bool isSharing(string userid, string targetid)
        {
            if (Data[userid] == null) return false;
            return (Data[userid] as Dictionary<string, object>).ContainsKey(targetid);
        }

        public static string notSharing = "You don't share anything with anyone yet";
        public static string shareList = "Your share list:";
        public static string couldntFindPlayer = "Couldn't find the target player.";
        public static string removedShare = "{0} was removed from your share list";
        public static string addedShare = "{0} was added to your sharelist";
        public static string notSharingWith = "You are not sharing with: {0}";
        public static string alreadySharing = "You are already sharing with {0}";

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
            CheckCfg<string>("Messages: Not Sharing", ref notSharing);
            CheckCfg<string>("Messages: Share List", ref shareList);
            CheckCfg<string>("Messages: No Player Found", ref couldntFindPlayer);
            CheckCfg<string>("Messages: Removed Share", ref removedShare);
            CheckCfg<string>("Messages: Added Share", ref addedShare);
            CheckCfg<string>("Messages: Already Sharing With", ref alreadySharing);
            CheckCfg<string>("Messages: Not Sharing With", ref notSharingWith);

            SaveConfig();
        }

        Dictionary<string, object> GetPlayerdata(string userid)
        {
            if (Data[userid] == null)
                Data[userid] = new Dictionary<string, object>();
            return Data[userid] as Dictionary<string, object>;
        }
        [ChatCommand("unshare")]
        void cmdChatUnshare(NetUser netuser, string command, string[] args)
        {
            var playerdata = GetPlayerdata(netuser.playerClient.userID.ToString());
            if (args.Length == 0)
            {
                if (playerdata.Count == 0)
                {
                    SendReply(netuser, notSharing);
                    return;
                }
                SendReply(netuser, shareList);
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
                SendReply(netuser, string.Format(removedShare, targetname.ToString()));
                playerdata.Remove(targetid);
                return;
            }
            SendReply(netuser, string.Format(notSharingWith, targetname.ToString()));
        }
        [ChatCommand("share")]
        void cmdChatShare(NetUser netuser, string command, string[] args)
        {
            var playerdata = GetPlayerdata(netuser.playerClient.userID.ToString());
            if (args.Length == 0)
            { 
                if(playerdata.Count == 0)
                {
                    SendReply(netuser, notSharing);
                    return;
                }
                SendReply(netuser, shareList);
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
                targetname = targetuser.displayName;
                targetid = targetuser.playerClient.userID.ToString();
            }
            if(playerdata.ContainsKey(targetid))
            {
                SendReply(netuser, string.Format(alreadySharing, targetname.ToString()));
                return;
            } 
            playerdata.Add(targetid, targetname);
            SendReply(netuser, string.Format(addedShare, playerdata[targetid].ToString()));
        }
    }
}