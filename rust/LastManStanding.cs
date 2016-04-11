using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Reflection;
using Oxide.Core;
using System.Data;
using Rust;

namespace Oxide.Plugins
{
    [Info("LastManStanding", "TheMechanical97", "1.1.3")]
    class LastManStanding : RustPlugin
    {

        #region Plugin References

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        Plugin Spawns;

        #endregion

        #region Config

        //////////////////////////////////////////////////////////////////////////////////////
        // Configurations ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static string DefaultKit = "lmskit";
        static string EventName = "Last Man Standing";
        static string EventZoneName = "lmszone";
        static string EventSpawnFile = "lmsspawns";

        static float StartHealth = 100;

        static int SurvivalPoints = 1;
        static int WinnerPoints = 5;
        static int KillPoints = 2;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            CheckCfg("Scoring - Tokens given to alive players when a players gets eliminated", ref SurvivalPoints);
            CheckCfg("Scoring - Tokens given to player when killing another player", ref KillPoints);
            CheckCfg("Scoring - Winner tokens", ref WinnerPoints);
            CheckCfgFloat("Player - Starting Health", ref StartHealth);
            CheckCfg("Options - Default kit", ref DefaultKit);
            CheckCfg("Options - Zone name", ref EventZoneName);
            CheckCfg("Options - Default spawnfile", ref EventSpawnFile);
            CurrentKit = DefaultKit;
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        object GetEventConfig(string configname)
        {
            if (!useThisEventLMS) return null;
            if (Config[configname] == null) return null;
            return Config[configname];
        }

        private bool useThisEventLMS;
        private bool EventStarted;
        private bool Changed;
        public string CurrentKit;

        private List<LastManStandingPlayers> LMSPlayers = new List<LastManStandingPlayers>();

        #endregion Config

        #region Player Class
        //////////////////////////////////////////////////////////////////////////////////////
        // Player Class //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        class LastManStandingPlayers : MonoBehaviour
        {
            public BasePlayer player;
            public int deaths;
            public int wins;
            public int points;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                deaths = 0;
                points = 0;
            }
        }

        class StoredData
        {
            public Dictionary<ulong, double> totalkills = new Dictionary<ulong, double>();
            public Dictionary<ulong, double> totaldeaths = new Dictionary<ulong, double>();
            public Dictionary<ulong, double> totalwins = new Dictionary<ulong, double>();
        }

        StoredData storedData;

        void showstats(BasePlayer player)
        {
            double _totalkils;
            double _totaldeaths;
            double _totalwins;
            ulong playerID = player.userID;
            if (storedData.totalkills.TryGetValue(playerID, out _totalkils))
            {
                if (storedData.totaldeaths.TryGetValue(playerID, out _totaldeaths))
                {
                    if (storedData.totalwins.TryGetValue(playerID, out _totalwins))
                    {
                        SendReply(player, string.Format(lang.GetMessage("stats0", this)));
                        SendReply(player, string.Format(lang.GetMessage("stats4", this), player.displayName));
                        SendReply(player, string.Format(lang.GetMessage("stats1", this), _totalwins.ToString()));
                        SendReply(player, string.Format(lang.GetMessage("stats2", this), _totalkils.ToString()));
                        SendReply(player, string.Format(lang.GetMessage("stats3", this), _totaldeaths.ToString()));
                        return;
                    }
                    storedData.totalwins[playerID] = 0;
                    Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
                    showstats(player);
                    return;
                }
                storedData.totaldeaths[playerID] = 0;
                Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
                showstats(player);
                return;
            }
            storedData.totalkills[playerID] = 0;
            Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
            showstats(player);
            return;
        }

        List<BasePlayer> onlineplayers = BasePlayer.activePlayerList as List<BasePlayer>;

        #endregion

        #region Messages

        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"noEvent", "Event plugin doesn't exist" },
            {"statsreset", "<color=orange>All Players Stats were Reset!</color>" },
            {"statsresetconsole", "All Players Stats were Reset!" },
            {"noConfig", "Creating a new config file" },
            {"title", "<color=orange>Last Man Standing</color> : "},
            {"titleconsole", "Last Man Standing: "},
            {"noPlayers", "Last Man Standing has no more players, auto-closing."},
            {"openBroad", "Kill other players to survive! Last Player Standing Wins!"},
            {"eventWon", "{0} has won the event!"},
            {"eventDeath", "{0} has died!"},
            {"noperm", "<color=red>You don't have permission to run this command!</color>"},
            {"notEnough", "Not enough players to start the event"},
            {"tokensadded", "You got {0} Tokens for surviving!"},
            {"tokenskill", "You got {0} Tokens for killing a player!"},
            {"tokenswin", "You got {0} Tokens for winning!"},
            {"started", "Event has started! Last player alive wins!"},
            {"playersremaining", "{0} Players remaining!"},
            {"stats0", "<size=25><color=orange>----Last Man Standing Stats----</color></size>"},
            {"stats4", "Stats from: <color=#FFAA00>{0}</color>"},
            {"stats1", "Total Games Won: <color=#FFAA00>{0}</color>"},
            {"stats2", "Total Kills: <color=green>{0}</color>"},
            {"stats3", "Total Deaths: <color=red>{0}</color>"},
            {"suicide", "{0} suicided."}
        };

        private void MessageAllPlayers(string msg)
        {
            foreach (LastManStandingPlayers player in LMSPlayers)
            {
                SendReply(player.player, msg);
            }
        }

        private void MessageAllOnlinePlayers(string msg)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    SendReply(player, msg);
                }
            }
        }

        #endregion

        #region Game
        //////////////////////////////////////////////////////////////////////////////////////
        // Scoring ///////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void SearchWinner()
        {
            if (LMSPlayers.Count == 1)
            {
                foreach (LastManStandingPlayers player in LMSPlayers)
                {
                    Winner(player.player);
                }
            }
        }

        void resetstats(BasePlayer player)
        {
            storedData.totaldeaths.Clear();
            storedData.totalkills.Clear();
            storedData.totalwins.Clear();
            SendReply(player, string.Format(lang.GetMessage("title", this) + lang.GetMessage("statsreset", this)));
        }

        void resetstatsconsole()
        {
            storedData.totaldeaths.Clear();
            storedData.totalkills.Clear();
            storedData.totalwins.Clear();
        }

        void addSurvivalPoints()
        {
            foreach (LastManStandingPlayers player in LMSPlayers)
            {
                EventManager.Call("AddTokens", (player.player).userID.ToString(), SurvivalPoints); //NEED TO BE ADDED :D
                MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("tokensadded", this), SurvivalPoints.ToString()));
            }
        }

        void addKillPoints(BasePlayer player)
        {
            EventManager.Call("AddTokens", player.userID.ToString(), KillPoints);
            SendReply(player, string.Format(lang.GetMessage("title", this) + lang.GetMessage("tokenskill", this), KillPoints.ToString()));
        }

        void addkillstats(BasePlayer player)
        {
            double _totalkils;
            ulong playerID = player.userID;
            if (storedData.totalkills.TryGetValue(playerID, out _totalkils))
            {
                storedData.totalkills[playerID] = _totalkils + 1;
                Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
                return;
            }
            storedData.totalkills[playerID] = 1;
            Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
        }

        void adddeathsstats(BasePlayer player)
        {
            double _totaldeaths;
            ulong playerID = player.userID;
            if (storedData.totaldeaths.TryGetValue(playerID, out _totaldeaths))
            {
                storedData.totaldeaths[playerID] = _totaldeaths + 1;
                Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
                return;
            }
            storedData.totaldeaths[playerID] = 1;
            Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
        }

        void addwinsstats(BasePlayer player)
        {
            double _totalwins;
            ulong playerID = player.userID;
            if (storedData.totalwins.TryGetValue(playerID, out _totalwins))
            {
                storedData.totalwins[playerID] = _totalwins + 1;
                Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
                return;
            }
            storedData.totalwins[playerID] = 1;
            Interface.Oxide.DataFileSystem.WriteObject("LastManStanding", storedData);
        }


        void Winner(BasePlayer player)
        {
            addwinsstats(player);
            EventManager.Call("AddTokens", player.userID.ToString(), WinnerPoints);
            MessageAllOnlinePlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("eventWon", this), player.displayName));
            SendReply(player, string.Format(lang.GetMessage("title", this) + lang.GetMessage("tokenswin", this), WinnerPoints.ToString()));
            var emptobject = new object[] { };
            EventManager.Call("EndEvent", emptobject);
            rust.RunServerCommand("reload LastManStanding");
        }

        #endregion

        #region Oxide Hooks

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void Loaded()
        {
            permission.RegisterPermission("lastmanstanding.admin", this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("LastManStanding");
            useThisEventLMS = false;
            EventStarted = false;
            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            if (EventManager == null)
            {
                Puts(string.Format(lang.GetMessage("noEvent", this)));
                return;
            }
            LoadVariables();
            RegisterGame();
        }
        void RegisterGame()
        {
            var success = EventManager.Call("RegisterEventGame", new object[] { EventName });
            if (success == null)
            {
                Puts(string.Format(lang.GetMessage("noEvent", this)));
                return;
            }
        }
        void LoadDefaultConfig()
        {
            Puts(string.Format(lang.GetMessage("noConfig", this)));
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (useThisEventLMS && EventStarted)
            {
                EventManager.Call("EndEvent", new object[] { });
                var objects = GameObject.FindObjectsOfType(typeof(LastManStandingPlayers));
                if (objects != null)
                    foreach (var gameObj in objects)
                        GameObject.Destroy(gameObj);
            }
        }

        #endregion

        #region EventManager Hooks

        //////////////////////////////////////////////////////////////////////////////////////
        // Event Manager hooks ///////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnSelectEventGamePost(string name)
        {
            if (EventName == name)
            {
                useThisEventLMS = true;
                if (EventSpawnFile != null && EventSpawnFile != "")
                    EventManager.Call("SelectSpawnfile", new object[] { EventSpawnFile });
            }
            else
                useThisEventLMS = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (useThisEventLMS && EventStarted)
            {
                player.inventory.Strip();
                EventManager.Call("GivePlayerKit", new object[] { player, CurrentKit });
                player.health = StartHealth;
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == EventName)
                return;
        }
        object OnEventOpenPost()
        {
            if (useThisEventLMS)
                MessageAllPlayers(lang.GetMessage("title", this) + lang.GetMessage("openBroad", this));
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            if (useThisEventLMS)
            {
                EventStarted = false;
                useThisEventLMS = false;
                LMSPlayers.Clear();
            }
            return null;
        }

        object OnEventEndPost()
        {
            return null;
        }

        object OnEventStartPre()
        {
            var emptyobject = new object[] { };
            if (useThisEventLMS)
            {
                EventStarted = true;
                EventManager.Call("CloseEvent", emptyobject);
                MessageAllPlayers(lang.GetMessage("title", this) + lang.GetMessage("openBroad", this));
            }
            return null;
        }

        object OnSelectKit(string kitname)
        {
            if (useThisEventLMS)
            {
                CurrentKit = kitname;
                return true;
            }
            return null;
        }

        object OnEventJoinPost(BasePlayer player)
        {
            if (useThisEventLMS)
            {
                if (player.GetComponent<LastManStandingPlayers>())
                    GameObject.Destroy(player.GetComponent<LastManStandingPlayers>());
                LMSPlayers.Add(player.gameObject.AddComponent<LastManStandingPlayers>());
            }
            return null;
        }

        object OnEventLeavePost(BasePlayer player)
        {
            if (useThisEventLMS)
            {
                if (player.GetComponent<LastManStandingPlayers>())
                {
                    LMSPlayers.Remove(player.GetComponent<LastManStandingPlayers>());
                    GameObject.Destroy(player.GetComponent<LastManStandingPlayers>());
                }
            }
            if (EventStarted)
            {
                if (LMSPlayers.Count == 0)
                {
                    var emptyobject = new object[] { };
                    MessageAllPlayers(lang.GetMessage("title", this) + lang.GetMessage("noPlayers", this));
                    EventManager.Call("CloseEvent", emptyobject);
                    EventManager.Call("EndEvent", emptyobject);
                }
            }
            return null;
        }

        void OnEventPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (useThisEventLMS)
            {

            }
            return;
        }

        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (useThisEventLMS)
            {
                if (hitinfo.Initiator != null) //WHEN SUICIDE
                {
                    BasePlayer attacker = hitinfo.Initiator.ToPlayer();
                    if (attacker != null) //WHEN DYING BY TRAPS
                    {
                        if (attacker != victim) //KILLING HIMSELF
                        {
                            adddeathsstats(victim); //NORMAL KILL
                            addkillstats(attacker);
                            addKillPoints(attacker);
                            MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("eventDeath", this), victim.displayName));
                            EventManager.Call("LeaveEvent", victim);
                            if (LMSPlayers.Count == 1)
                            {
                                Winner(attacker);
                                return;
                            }
                            //addSurvivalPoints();
                            MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("playersremaining", this), (LMSPlayers.Count).ToString()));
                            return;
                        }
                        adddeathsstats(victim);
                        MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("suicide", this), victim.displayName));
                        EventManager.Call("LeaveEvent", victim);
                        if (LMSPlayers.Count == 1)
                        {
                            SearchWinner();
                            return;
                        }
                        //addSurvivalPoints();
                        MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("playersremaining", this), (LMSPlayers.Count).ToString()));
                        return;
                    }
                    adddeathsstats(victim);
                    MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("eventDeath", this), victim.displayName));
                    EventManager.Call("LeaveEvent", victim);
                    if (LMSPlayers.Count == 1)
                    {
                        Winner(attacker);
                        return;
                    }
                    //addSurvivalPoints();
                    MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("playersremaining", this), (LMSPlayers.Count).ToString()));
                    return;
                } //SUICIDE 
                adddeathsstats(victim);
                MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("suicide", this), victim.displayName));
                EventManager.Call("LeaveEvent", victim);
                if (LMSPlayers.Count == 1)
                {
                    SearchWinner();
                    return;
                }
                //addSurvivalPoints();
                MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("playersremaining", this), (LMSPlayers.Count).ToString()));
                return;
            }
        }

        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            return null;
        }
        object OnRequestZoneName()
        {
            if (useThisEventLMS)
            {
                return EventZoneName;
            }
            return null;
        }
        object OnSelectSpawnFile(string name)
        {
            if (useThisEventLMS)
            {
                EventSpawnFile = name;
                return true;
            }
            return null;
        }
        void OnSelectEventZone(MonoBehaviour monoplayer, string radius)
        {
            if (useThisEventLMS)
            {
                return;
            }
        }
        object CanEventOpen()
        {
            if (useThisEventLMS)
            {

            }
            return null;
        }
        object CanEventStart()
        {
            if (useThisEventLMS)
            {

            }
            return null;
        }
        object OnEventStartPost()
        {
            MessageAllPlayers(string.Format(lang.GetMessage("title", this) + lang.GetMessage("started", this)));
            return null;
        }
        object CanEventJoin()
        {
            return null;
        }

        #endregion

        #region Commands

        [ChatCommand("lms")]
        private void cmdStats(BasePlayer player, string command, string[] args)
        {
            if (args[0] == "stats")
            {
                showstats(player);
            }
            if (args[0] == "reset")
            {
                if (!permission.UserHasPermission(player.UserIDString, "lastmanstanding.admin"))
                {
                    SendReply(player, string.Format(lang.GetMessage("title", this) + lang.GetMessage("noperm", this)));
                    return;
                }
                resetstats(player);
            }
        }

        [ConsoleCommand("lms.reset")]
        void cmdlmsConsole(ConsoleSystem.Arg arg)
        {
            resetstatsconsole();
            arg.ReplyWith(string.Format(lang.GetMessage("titleconsole", this) + lang.GetMessage("statsresetconsole", this)));
        }

        #endregion

    }
}
