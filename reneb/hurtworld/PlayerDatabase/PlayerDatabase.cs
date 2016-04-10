using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Steamworks;
using uLink;

namespace Oxide.Plugins
{
    [Info("PlayerDatabase", "Reneb", "1.0.1")]
    class PlayerDatabase : HurtworldPlugin
    {
        public static DataFileSystem datafile;

        string subDirectory = "playerdatabase/";
        Hash<string, Dictionary<string, Dictionary<string, object>>> playersData = new Hash<string, Dictionary<string, Dictionary<string, object>>>();
        List<string> changedPlayersData = new List<string>();

        ////////////////////////////////////////////////////////////
        // Known Players
        ////////////////////////////////////////////////////////////

        StoredData storedData;

        class StoredData
        {
            public HashSet<string> knownPlayers = new HashSet<string>();

            public StoredData() { }
        }

        void OnServerSave() { SaveData(); SavePlayerDatabase(); }

        void SaveData() { Interface.GetMod().DataFileSystem.WriteObject("PlayerDatabase", storedData); }

        void LoadData()
        {
            try { storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("PlayerDatabase"); }
            catch { storedData = new StoredData(); }
        }

        bool isKnownPlayer(PlayerSession player) { return isKnownPlayer(player.SteamId.ToString()); }
        bool isKnownPlayer(PlayerIdentity player) { return isKnownPlayer(player.SteamId.ToString()); }
        bool isKnownPlayer(string userid) { return storedData.knownPlayers.Contains(userid); }

        void Loaded()
        {
            datafile = Interface.GetMod().DataFileSystem;
            LoadData();
            foreach (string userid in storedData.knownPlayers)
            {
                LoadPlayer(userid);
            }
        }

        void SavePlayerDatabase()
        {
            foreach (string userid in changedPlayersData)
            {
                string path = subDirectory + userid;
                datafile.WriteObject<Dictionary<string, Dictionary<string, object>>>(path, playersData[userid]);
            }
            changedPlayersData.Clear();
        }
        void Unload()
        {
            SavePlayerDatabase();
            SaveData();
        }

        void OnPlayerConnected(PlayerSession player)
        {
           // var net = GameManager.GetPlayerEntity(netPlayer) as UnityEngine.GameObject;
           // PlayerIdentity player = net.GetComponent<PlayerIdentity>();
            if (player == null) return;
            if (!isKnownPlayer(player.SteamId.ToString())) { LoadPlayer(player.SteamId.ToString()); }
            if (!playersData[player.SteamId.ToString()].ContainsKey("default"))
                playersData[player.SteamId.ToString()].Add("default", new Dictionary<string, object>());
            if (playersData[player.SteamId.ToString()]["default"].ContainsKey("name"))
                playersData[player.SteamId.ToString()]["default"]["name"] = player.Name;
            else
                playersData[player.SteamId.m_SteamID.ToString()]["default"].Add("name", player.Name);
        }

        HashSet<string> GetAllKnownPlayers()
        {
            return storedData.knownPlayers;
        }

        object FindPlayer(string arg)
        {
            ulong steamid = 0L;
            ulong.TryParse(arg, out steamid);
            string lowerarg = arg.ToLower();

            if (steamid != 0L && arg.Length == 17)
            {
                if (playersData[arg] == null) return "No players found with this steamid";
                else return arg;
            }

            string foundSteamID = string.Empty;
            string foundUserNames = string.Empty;
            string returnString = string.Empty;
            bool multiplePlayers = false;
            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, object>>> pair in playersData)
            {
                if (pair.Value.ContainsKey("default"))
                    if (pair.Value["default"].ContainsKey("name"))
                    {
                        string lowname = pair.Value["default"]["name"].ToString().ToLower();
                        if (lowname.Contains(lowerarg))
                            if (foundSteamID == string.Empty)
                            {
                                foundSteamID = pair.Key;
                                foundUserNames = pair.Value["default"]["name"].ToString();
                            }
                            else
                            {
                                returnString = "Multiple players found: ";
                                foundSteamID += " " + pair.Key;
                                foundUserNames += " " + pair.Value["default"]["name"].ToString();
                                multiplePlayers = true;
                            }
                    }
            }
            if (foundSteamID == string.Empty)
                return "Couldn't find any player with this name";
            if(multiplePlayers)
                return returnString + foundUserNames;
            return returnString + foundSteamID;
        }
        void LoadPlayer(PlayerSession player) { LoadPlayer(player.SteamId.ToString()); }
        void LoadPlayer(PlayerIdentity player) { LoadPlayer(player.SteamId.ToString()); }
        void LoadPlayer(string userid)
        {
            if (!storedData.knownPlayers.Contains(userid))
                storedData.knownPlayers.Add(userid);
            string path = subDirectory + userid;

            if (datafile.ExistsDatafile(path)) { }

            object profile;
            try 
            {
                profile = datafile.ReadObject<Dictionary<string, Dictionary<string, object>>>(path);
            }
            catch (Exception exception) {
                profile = new Dictionary<string, object>();
                datafile.WriteObject<Dictionary<string, Dictionary<string, object>>>(path, new Dictionary<string, Dictionary<string, object>>());
            }

            playersData[userid] = profile as Dictionary<string, Dictionary<string, object>>;
        }
          
        void SetPlayerData(string userid, string key, Dictionary<string, object> data)
        { 
            if (!playersData.ContainsKey(userid)) LoadPlayer(userid);
            Dictionary<string, Dictionary<string, object>> profile = playersData[userid];
              
            if (!profile.ContainsKey(key)) profile.Add(key, data);
            else profile[key] = data;
            playersData[userid] = profile;

            if (!changedPlayersData.Contains(userid))
                changedPlayersData.Add(userid);
        } 
        object GetPlayerData(string userid, string key)
        {
            if (!playersData.ContainsKey(userid)) return null;
            Dictionary<string, Dictionary<string,object>> profile =  playersData[userid];
            if (!profile.ContainsKey(key))
            {
                return null;
            }
            else
            {
                return profile[key];
            }
        }
    }
}
