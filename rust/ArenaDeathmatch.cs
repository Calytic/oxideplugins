// Requires: EventManager
using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Arena Deathmatch", "Reneb", "1.1.6", ResourceId = 741)]
    class ArenaDeathmatch : RustPlugin
    {
        ////////////////////////////////////////////////////////////
        // Setting all fields //////////////////////////////////////
        ////////////////////////////////////////////////////////////
        [PluginReference]
        EventManager EventManager;

        [PluginReference]
        Plugin ZoneManager;

        private bool useThisEvent;
        private bool EventStarted;
        private bool Changed;

        public string CurrentKit;
        private List<DeathmatchPlayer> DeathmatchPlayers = new List<DeathmatchPlayer>();

        ////////////////////////////////////////////////////////////
        // DeathmatchPlayer class to store informations ////////////
        ////////////////////////////////////////////////////////////
        class DeathmatchPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int kills;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                kills = 0;
            }
        }
        class LeaderBoard
        {
            public string Name;
            public int Kills;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            useThisEvent = false;
            EventStarted = false;
        }
        void OnServerInitialized()
        {
            if (EventManager == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
            LoadVariables();
            RegisterGame();
        }
        void RegisterGame()
        {
            var success = EventManager.RegisterEventGame(EventName);
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Event Deathmatch: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);

            if (useThisEvent && EventStarted)            
                EventManager.EndEvent();

            var objects = UnityEngine.Object.FindObjectsOfType<DeathmatchPlayer>();
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Configurations ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static string DefaultKit = "deathmatch";
        static string EventName = "Deathmatch";
        static string EventZoneName = "Deathmatch";
        static string EventSpawnFile = "DeathmatchSpawnfile";
        static int EventWinKills = 10;

        static float EventStartHealth = 100;

        static Dictionary<string, object> EventZoneConfig;

        static string EventMessageWon = "{0} WON THE DEATHMATCH";
        static string EventMessageNoMorePlayers = "Arena has no more players, auto-closing.";
        static string EventMessageKill = "{0} killed {3}. ({1}/{2} kills)";
        static string EventMessageOpenBroadcast = "In Deathmatch, it's a free for all, the goal is to kill as many players as possible!";

        static int TokensAddKill = 1;
        static int TokensAddWon = 5;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("DeathMatch - Kit - Default", ref DefaultKit);
            CheckCfg("DeathMatch - Event - Name", ref EventName);
            CheckCfg("DeathMatch - Event - SpawnFile", ref EventSpawnFile);
            CheckCfg("DeathMatch - Zone - Name", ref EventZoneName);
            CheckCfg("DeathMatch - Win - Kills Needed", ref EventWinKills);
            CheckCfgFloat("DeathMatch - Start - Health", ref EventStartHealth);


            CheckCfg("Messages - Won", ref EventMessageWon);
            CheckCfg("Messages - Empty", ref EventMessageNoMorePlayers);
            CheckCfg("Messages - Kill", ref EventMessageKill);
            CheckCfg("Messages - Open Broadcast", ref EventMessageOpenBroadcast);

            CheckCfg("Tokens - Per Kill", ref TokensAddKill);
            CheckCfg("Tokens - On Win", ref TokensAddWon);

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
            if (!useThisEvent) return null;
            if (Config[configname] == null) return null;
            return Config[configname];
        }
        #region UI Scoreboard
        private List<DeathmatchPlayer> SortScores() => DeathmatchPlayers.OrderByDescending(pair => pair.kills).ToList();

        private string PlayerMsg(int key, DeathmatchPlayer player) => $"|  <color=#FF8C00>{key}</color>.  <color=#FF8C00>{player.player.displayName}</color> <color=#939393>--</color> <color=#FF8C00>{player.kills}</color>  |";

        private CuiElementContainer CreateScoreboard(BasePlayer player)
        {
            DestroyUI(player);

            string panelName = "DMScoreBoard";
            var element = EventManager.UI.CreateElementContainer(panelName, "0.3 0.3 0.3 0.6", "0.1 0.95", "0.9 1", false);

            var scores = SortScores();
            var index = scores.FindIndex(a => a.player == player);

            var scoreMessage = PlayerMsg(index + 1, scores[index]);
            int amount = 3;
            for (int i = 0; i < amount; i++)
            {
                if (scores.Count >= i + 1)
                {
                    if (scores[i].player == player)
                    {
                        amount++;
                        continue;
                    }
                    scoreMessage = scoreMessage + PlayerMsg(i + 1, scores[i]);
                }
            }
            EventManager.UI.CreateLabel(ref element, panelName, "", scoreMessage, 18, "0 0", "1 1");
            return element;
        }
        private void RefreshUI()
        {
            foreach (var entry in DeathmatchPlayers)
            {
                DestroyUI(entry.player);
                AddUI(entry.player);
            }
        }
        private void AddUI(BasePlayer player) => CuiHelper.AddUi(player, CreateScoreboard(player));
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "DMScoreBoard");        
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////
        // Beginning Of Event Manager Hooks //////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void OnSelectEventGamePost(string name)
        {
            if (EventName == name)
            {
                useThisEvent = true;
                if (EventSpawnFile != null && EventSpawnFile != "")
                    EventManager.SelectSpawnfile(EventSpawnFile);
            }
            else
                useThisEvent = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (useThisEvent && EventStarted)
            {                
                player.inventory.Strip();
                EventManager.GivePlayerKit(player, CurrentKit);
                player.health = EventStartHealth;
                AddUI(player);
            }
        }
        object OnSelectSpawnFile(string name)
        {
            if (useThisEvent)
            {
                EventSpawnFile = name;
                return true;
            }
            return null;
        }
        void OnSelectEventZone(MonoBehaviour monoplayer, string radius)
        {
            if (useThisEvent)
            {
                return;
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == EventName)
            {
                return;
            }
        }
        object CanEventOpen()
        {
            if (useThisEvent)
            {

            }
            return null;
        }
        object CanEventStart()
        {
            return null;
        }
        object OnEventOpenPost()
        {
            if (useThisEvent)
                EventManager.BroadcastEvent(EventMessageOpenBroadcast);
            return null;
        }
        object OnEventCancel()
        {
            CheckScores(true);
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            return null;
        }
        object OnEventEndPost()
        {
            if (useThisEvent)
            {
                EventStarted = false;
                DeathmatchPlayers.Clear();
            }
            return null;
        }
        object OnEventStartPre()
        {
            if (useThisEvent)
            {
                EventStarted = true;
            }
            return null;
        }
        object OnEventStartPost()
        {
            return null;
        }
        object CanEventJoin()
        {
            return null;
        }
        object OnSelectKit(string kitname)
        {
            if(useThisEvent)
            {
                CurrentKit = kitname;
                return true;
            }
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (useThisEvent)
            {
                if (player.GetComponent<DeathmatchPlayer>())
                    UnityEngine.Object.Destroy(player.GetComponent<DeathmatchPlayer>());
                DeathmatchPlayers.Add(player.gameObject.AddComponent<DeathmatchPlayer>());
                Puts("here");
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (useThisEvent)
            {
                if (player.GetComponent<DeathmatchPlayer>())
                {
                    DeathmatchPlayers.Remove(player.GetComponent<DeathmatchPlayer>());
                    UnityEngine.Object.Destroy(player.GetComponent<DeathmatchPlayer>());
                    Debug.Log("leavehere");
                    CheckScores();
                }
            }
            return null;
        }
        void OnEventPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (useThisEvent)
            {
                if (!(hitinfo.HitEntity is BasePlayer))
                {
                    hitinfo.damageTypes = new DamageTypeList();
                    hitinfo.DoHitEffects = false;
                }
            }
        }

        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (useThisEvent)
            {
                DestroyUI(victim);
                if (hitinfo.Initiator != null)
                {
                    BasePlayer attacker = hitinfo.Initiator.ToPlayer();
                    if (attacker != null)
                    {
                        if (attacker != victim)
                        {
                            AddKill(attacker, victim);
                        }
                    }
                }
            }
            return;
        }
        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            return null;
        }
        object OnRequestZoneName()
        {
            if (useThisEvent)
            {
                return EventZoneName;
            }
            return null;
        }
        //////////////////////////////////////////////////////////////////////////////////////
        // End Of Event Manager Hooks ////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void AddKill(BasePlayer player, BasePlayer victim)
        {
            if (!player.GetComponent<DeathmatchPlayer>())
                return;

            player.GetComponent<DeathmatchPlayer>().kills++;
            EventManager.AddTokens(player.userID.ToString(), TokensAddKill);
            EventManager.BroadcastEvent(string.Format(EventMessageKill, player.displayName, player.GetComponent<DeathmatchPlayer>().kills.ToString(), EventWinKills.ToString(), victim.displayName));
            CheckScores();
        }
        void CheckScores(bool timelimitreached = false)
        {
            if (DeathmatchPlayers.Count == 0)
            {
                EventManager.BroadcastEvent(EventMessageNoMorePlayers);
                EventManager.CloseEvent();
                EventManager.EndEvent();
                return;
            }
            BasePlayer winner = null;
            int topscore = 0;

            foreach (DeathmatchPlayer deathmatchplayer in DeathmatchPlayers)
            {
                if (deathmatchplayer == null) continue;
                if (EventManager.EventMode == EventManager.GameMode.Normal)
                {
                    if (deathmatchplayer.kills >= EventWinKills || DeathmatchPlayers.Count == 1)
                    {
                        winner = deathmatchplayer.player;
                        break;
                    }
                }
                if (timelimitreached)
                {
                    if (deathmatchplayer.kills > topscore)
                    {
                        winner = deathmatchplayer.player;
                        topscore = deathmatchplayer.kills;
                    }
                }
            }
            
            if (winner == null) return;
            Winner(winner);
        }
        void Winner(BasePlayer player)
        {
            var winnerobjectmsg = new object[] {  };
            EventManager.AddTokens(player.userID.ToString(), TokensAddWon);
            var emptyobject = new object[] { };
            for (var i = 1; i < 10; i++)
            {
                EventManager.BroadcastEvent(string.Format(EventMessageWon, player.displayName));
            }
            EventManager.CloseEvent();
            EventManager.EndEvent();
        }
    }
}

