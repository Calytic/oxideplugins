using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Timers;
using Rust;

namespace Oxide.Plugins
{
    [Info("Event Battlefield", "Reneb", "1.0.4")]
    class Battlefield : RustPlugin
    {
        ////////////////////////////////////////////////////////////
        // Setting all fields //////////////////////////////////////
        ////////////////////////////////////////////////////////////
        [PluginReference]
        Plugin EventManager;
        [PluginReference]
        Plugin ZoneManager;

        private bool useThisEvent;
        private bool EventStarted;
        private bool Changed;

        private List<BattlefieldPlayer> BattlefieldPlayers = new List<BattlefieldPlayer>();

        private Hash<BattlefieldPlayer, string> WeaponVote = new Hash<BattlefieldPlayer, string>();
        private Hash<BattlefieldPlayer, string> GroundVote = new Hash<BattlefieldPlayer, string>();
        private string currentGround;
        private string currentWeapon;

        ////////////////////////////////////////////////////////////
        // BattlefieldPlayer class to store informations ////////////
        ////////////////////////////////////////////////////////////
        class BattlefieldPlayer : MonoBehaviour
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
            var success = EventManager.Call("RegisterEventGame", new object[] { EventName });
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Event Battlefield: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (useThisEvent && EventStarted)
            {
                EventManager.Call("EndEvent", new object[] { });
                var objects = GameObject.FindObjectsOfType(typeof(BattlefieldPlayer));
                if (objects != null)
                    foreach (var gameObj in objects)
                        GameObject.Destroy(gameObj);
            }
        }



        //////////////////////////////////////////////////////////////////////////////////////
        // Configurations ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static string EventName = "Battlefield";

        static float EventStartHealth = 100;

        static Dictionary<string, object> EventGrounds = DefaultGrounds();
        static string DefaultGround = "shortrange";
        static string DefaultWeapon = "assault";

        static string EventMessageKill = "{0} killed {2}. ({1} kills)";
        static string EventMessageOpenBroadcast = "In Battlefield, it's a free for all, the goal is to kill as many players as possible!";
        static string EventMessageErrorNotLaunched = "Battlefield isn't currently launched";
        static string EventMessageErrorNotStarted = "You need to wait for the Battlefield to be started to use this command.";
        static string EventMessageErrorVoteNotBF = "You must be in the battlefield to vote";
        static string EventMessageVoteGroundAvaible = "ï¸»â³âä¸ Battlefield Grounds Avaible ä¸ââ³ï¸» Votes required for an item: {0}";
        static string EventMessageVoteGroundVotes = "{0} - {1} votes";
        static string EventMessageErrorVoteNoGround = "This battlefield ground doesn't exist.";
        static string EventMessageErrorVoteAlreadyGround = "The current ground is already {0}.";
        static string EventMessageVoteGroundVoted = "You have voted for the ground: {0}.";
        static string EventMessageVoteGroundShowVotes = "â³âä¸ {0} has {1} votes";
        static string EventMessageVoteGroundNew = "ï¸»â³âä¸ New ground is now: {0}";
        static string EventMessageVoteWeaponNew = "ï¸»â³âä¸ New weapon kit is now: {0}";
        static string EventMessageVoteWeaponShowVotes = "â³âä¸ {0} has {1} votes";
        static string EventMessageVoteWeaponAvaible = "ï¸»â³âä¸ Avaible Weapon Kits For Current Ground ä¸ââ³ï¸»  Votes required for an item: {0}";
        static string EventMessageVoteWeaponVotes = "{0} - {1} votes";
        static string EventMessageErrorVoteNoKit = "This weapon kits doesn't exist in this battleground.";
        static string EventMessageErrorVoteAlreadyWeapon = "The current weapon is already {0}.";
        static string EventMessageVoteWeaponVoted = "ï¸»â³âä¸ You have voted for the weapon: {0}.";

        static int TokensAddKill = 1;
        static int EventVotePercent = 60;


        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg<string>("Battlefield - Event - Name", ref EventName);
            CheckCfgFloat("Battlefield - Start - Health", ref EventStartHealth);

            CheckCfg<string>("Messages - Kill", ref EventMessageKill);
            CheckCfg<string>("Messages - Open Broadcast", ref EventMessageOpenBroadcast);
            CheckCfg<string>("Messages - Error - Not Selected", ref EventMessageErrorNotLaunched);
            CheckCfg<string>("Messages - Error - Not Started", ref EventMessageErrorNotStarted);
            CheckCfg<string>("Messages - Error - Not joined", ref EventMessageErrorVoteNotBF);
            CheckCfg<string>("Messages - Vote - Grounds - Show Avaible Title", ref EventMessageVoteGroundAvaible);
            CheckCfg<string>("Messages - Vote - Grounds - Show Avaible List", ref EventMessageVoteGroundVotes);
            CheckCfg<string>("Messages - Error - Vote - Ground Doesnt Exist", ref EventMessageErrorVoteNoGround);
            CheckCfg<string>("Messages - Error - Vote - Ground Already This One", ref EventMessageErrorVoteAlreadyGround);
            CheckCfg<string>("Messages - Vote - Grounds - Voted", ref EventMessageVoteGroundVoted);
            CheckCfg<string>("Messages - Vote - Grounds - Show Votes", ref EventMessageVoteGroundShowVotes);
            CheckCfg<string>("Messages - Vote - Grounds - New", ref EventMessageVoteGroundNew);
            CheckCfg<string>("Messages - Vote - Weapons - New", ref EventMessageVoteWeaponNew);
            CheckCfg<string>("Messages - Vote - Weapons - Show Votes", ref EventMessageVoteWeaponShowVotes);
            CheckCfg<string>("Messages - Vote - Weapons - Show Avaible Title", ref EventMessageVoteWeaponAvaible);
            CheckCfg<string>("Messages - Vote - Weapons - Show Avaible List", ref EventMessageVoteWeaponVotes);
            CheckCfg<string>("Messages - Vote - Weapons - Voted", ref EventMessageVoteWeaponVoted);
            CheckCfg<string>("Messages - Error - Vote - Weapon Doesnt Exist", ref EventMessageErrorVoteNoKit);
            CheckCfg<string>("Messages - Error - Vote - Weapon Already This One", ref EventMessageErrorVoteAlreadyWeapon);

            CheckCfg<Dictionary<string, object>>("Battlefield - Grounds", ref EventGrounds);

            CheckCfg<string>("Battlefield - Default Ground", ref DefaultGround);
            CheckCfg<string>("Battlefield - Default Weapon Kit", ref DefaultWeapon);
            CheckCfg<int>("Battlefield - Vote - % needed to win", ref EventVotePercent);
            
            CheckCfg<int>("Tokens - Per Kill", ref TokensAddKill);

            currentWeapon = DefaultWeapon;
            currentGround = DefaultGround;

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
        static Dictionary<string,object> DefaultGrounds()
        {
            var grounds = new Dictionary<string, object>();

            var ground1 = new Dictionary<string, object>();
            var ground1kit = new List<string>();
            ground1kit.Add("pistols");
            ground1kit.Add("assault");
            ground1.Add("kits", ground1kit);
            ground1.Add("zone", "BattlefieldGround1");
            ground1.Add("spawnfile", "shortrangespawnfile");
                 
            var ground2 = new Dictionary<string, object>();
            var ground2kit = new List<string>();
            ground2kit.Add("sniper");
            ground2kit.Add("pistols");
            ground2.Add("kits", ground2kit);
            ground2.Add("zone", "BattlefieldGround2");
            ground2.Add("spawnfile", "longrangespawnfile");
             
            grounds.Add("shortrange", ground1);
            grounds.Add("longrange", ground2);

            return grounds;
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
         
        //////////////////////////////////////////////////////////////////////////////////////
        // Beginning Of Event Manager Hooks //////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void OnSelectEventGamePost(string name)
        {
            if (EventName == name)
            {
                useThisEvent = true;
                currentWeapon = DefaultWeapon;
                currentGround = DefaultGround;
                ResetVotes();
                var currentbfdata = EventGrounds[currentGround] as Dictionary<string, object>;
                if (currentbfdata["spawnfile"] != null && currentbfdata["spawnfile"].ToString() != "")
                    EventManager.Call("SelectSpawnfile", new object[] { currentbfdata["spawnfile"].ToString() });
            }
            else
                useThisEvent = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (useThisEvent && EventStarted)
            {
                player.inventory.Strip();
                EventManager.Call("GivePlayerKit", new object[] { player, currentWeapon });
                player.health = EventStartHealth;
            }
        }
        object OnSelectSpawnFile(string name)
        {
            if (useThisEvent)
            {
                return true;
            }
            return null;
        }
        object OnSelectKit(string kitname)
        {
            if (useThisEvent)
            {
                SetWeapon(kitname);
                return true;
            }
            return null;
        }
        object OnEventOpenPost()
        {
            if (useThisEvent)
                EventManager.Call("BroadcastEvent", new object[] { EventMessageOpenBroadcast });
            return null;
        }
        object OnEventEndPost()
        {
            if (useThisEvent)
            {
                EventStarted = false;
                BattlefieldPlayers.Clear();
            }
            return null;
        }
        object OnRequestZoneName()
        {
            if(useThisEvent)
            {
                var eventgrounddata = EventGrounds[currentGround] as Dictionary<string, object>;
                return eventgrounddata["zone"].ToString();
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
        object OnEventJoinPost(BasePlayer player)
        {
            if (useThisEvent)
            {
                if (player.GetComponent<BattlefieldPlayer>())
                    GameObject.Destroy(player.GetComponent<BattlefieldPlayer>());
                BattlefieldPlayers.Add(player.gameObject.AddComponent<BattlefieldPlayer>());
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (useThisEvent)
            {
                if (player.GetComponent<BattlefieldPlayer>())
                {
                    BattlefieldPlayers.Remove(player.GetComponent<BattlefieldPlayer>());
                    GameObject.Destroy(player.GetComponent<BattlefieldPlayer>());
                }
            }
            return null;
        }

        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (useThisEvent)
            {
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
        //////////////////////////////////////////////////////////////////////////////////////
        // End Of Event Manager Hooks ////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void AddKill(BasePlayer player, BasePlayer victim)
        {
            if (!player.GetComponent<BattlefieldPlayer>())
                return;

            player.GetComponent<BattlefieldPlayer>().kills++;
            EventManager.Call("AddTokens", player.userID.ToString(), TokensAddKill);
            EventManager.Call("BroadcastEvent", string.Format(EventMessageKill, player.displayName, player.GetComponent<BattlefieldPlayer>().kills.ToString(), victim.displayName));
        }

        int GetGroundVotes(string groundname)
        {
            int groundvote = 0;
            foreach(KeyValuePair<BattlefieldPlayer, string> pair in GroundVote)
            {
                if (pair.Key == null) continue;
                if (pair.Value == groundname)
                    groundvote++;
            }
            return groundvote;
        }
        int GetWeaponVotes(string weaponname)
        {
            int weaponvote = 0;
            foreach (KeyValuePair<BattlefieldPlayer, string> pair in WeaponVote)
            {
                if (pair.Key == null) continue;
                if (pair.Value == weaponname)
                    weaponvote++;
            }
            return weaponvote;
        }
        int EventPlayersCount()
        {
            int plcount = 0;
            foreach (BattlefieldPlayer bfplayer in BattlefieldPlayers)
            {
                plcount++;
            }
            return plcount;
        }
        int VotePlayersNeeded() { return (int)Math.Ceiling( (decimal)(EventPlayersCount() * EventVotePercent / 100) ); }
        void ResetVotes()
        {
            GroundVote.Clear();
            WeaponVote.Clear();
        }


        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                return false;
            }
            return true;
        }
        
        [ChatCommand("ground")]
        void cmdChatGround(BasePlayer player, string command, string[] args)
        {
            if(!useThisEvent)
            {
                SendReply(player, EventMessageErrorNotLaunched);
                return;
            }
            
            if (!EventStarted)
            {
                SendReply(player, EventMessageErrorNotStarted);
                return;
            }
            BattlefieldPlayer bfplayer = player.GetComponent<BattlefieldPlayer>();
            
            if (bfplayer == null && !hasAccess(player))
            {
                SendReply(player, EventMessageErrorVoteNotBF);
                return;
            }
            if (args.Length == 0)
            {
                SendReply(player, string.Format(EventMessageVoteGroundAvaible, VotePlayersNeeded().ToString()));
                foreach (KeyValuePair<string,object> pair in EventGrounds)
                {
                    SendReply(player, string.Format(EventMessageVoteGroundVotes, pair.Key, GetGroundVotes(pair.Key).ToString()));
                }
                return;
            }
            string voteground = args[0];
            if(!EventGrounds.ContainsKey(voteground))
            {
                SendReply(player, EventMessageErrorVoteNoGround);
                return;
            }
            if (voteground == currentGround)
            {
                SendReply(player, string.Format(EventMessageErrorVoteAlreadyGround, currentGround));
                return;
            }

            if(hasAccess(player))
            {
                SetGround(voteground);
                return;
            }

            SendReply(player, string.Format(EventMessageVoteGroundVoted, voteground));
            if (GroundVote[bfplayer] == voteground)
                return;
            GroundVote[bfplayer] = voteground;
            CheckGroundVotes();
        }
        void CheckGroundVotes()
        {
            int votesneeded = VotePlayersNeeded();
            var votes = new Hash<string, int>();
            foreach(KeyValuePair<BattlefieldPlayer, string> pair in GroundVote)
            {
                votes[pair.Value]++;
                if(votes[pair.Value] >= votesneeded)
                {
                    SetGround(pair.Value);
                    return;
                }
            }
            foreach (KeyValuePair<string, int> pair in votes)
            {
                EventManager.Call("BroadcastEvent", string.Format(EventMessageVoteGroundShowVotes, pair.Key, pair.Value.ToString()));
            }
        }
        void SetGround(string newGround)
        {
            var eventgrounddata = EventGrounds[newGround] as Dictionary<string, object>;
            if (eventgrounddata == null) { EventManager.Call("BroadcastToChat", string.Format("Error while setting new ground: {0}. data doesn't exist. WTF?", newGround)); return; }
            if (eventgrounddata["spawnfile"] == null) { EventManager.Call("BroadcastToChat", string.Format("Error while setting new ground: {0}. spawnfile isn't set", newGround)); return; }

            var newkit = currentWeapon;
            var eventgroundkits = eventgrounddata["kits"] as List<object>;
            if (!eventgroundkits.Contains(newkit))
            {
                newkit = (string)eventgroundkits[0];
            }
            if(newkit == null) { EventManager.Call("BroadcastToChat", string.Format("Error while setting new ground: {0}. no kits were found", newGround)); return; }

            object success = EventManager.Call("SelectSpawnfile", eventgrounddata["spawnfile"].ToString());
            if(success is string)
            {
                EventManager.Call("BroadcastToChat", string.Format("Error while setting new ground: {0}. {1}", newGround, success.ToString()));
                return;
            }

            EventManager.Call("BroadcastEvent", string.Format(EventMessageVoteGroundNew, newGround));
            
            var oldzone = currentGround;
            currentGround = newGround;
            Debug.Log(oldzone);
            Debug.Log(currentGround);
            SetWeapon(newkit);
            foreach (BattlefieldPlayer bfplayer in BattlefieldPlayers)
            {
                bfplayer.player.Die();
                ZoneManager?.Call("RemovePlayerFromZoneKeepinlist", oldzone, bfplayer.player);                
            }
            GroundVote.Clear();
        }
        void SetWeapon(string newWeapon)
        {
            EventManager.Call("BroadcastEvent", string.Format(EventMessageVoteWeaponNew, newWeapon));
            currentWeapon = newWeapon;
            foreach (BattlefieldPlayer bfplayer in BattlefieldPlayers)
            {
                OnEventPlayerSpawn(bfplayer.player);
            }
            WeaponVote.Clear();
        }
        void CheckWeaponVotes()
        {
            int votesneeded = VotePlayersNeeded();
            var votes = new Hash<string, int>();
            foreach (KeyValuePair<BattlefieldPlayer, string> pair in WeaponVote)
            {
                votes[pair.Value]++;
                if (votes[pair.Value] >= votesneeded)
                {
                    SetWeapon(pair.Value);
                    return;
                }
            }
            foreach (KeyValuePair<string, int> pair in votes)
            {
                EventManager.Call("BroadcastEvent", string.Format(EventMessageVoteWeaponShowVotes, pair.Key, pair.Value.ToString()));
            }
        }
        [ChatCommand("weapon")]
        void cmdChatWeapon(BasePlayer player, string command, string[] args)
        {
            if (!useThisEvent)
            {
                SendReply(player, EventMessageErrorNotLaunched);
                return;
            }
            if (!EventStarted)
            {
                SendReply(player, EventMessageErrorNotStarted);
                return;
            }
            BattlefieldPlayer bfplayer = player.GetComponent<BattlefieldPlayer>();
            if (bfplayer == null && !hasAccess(player))
            {
                SendReply(player, EventMessageErrorVoteNotBF);
                return;
            }
            var eventgrounddata = EventGrounds[currentGround] as Dictionary<string, object>;
            var eventgroundkits = eventgrounddata["kits"] as List<object>;
            if (args.Length == 0)
            {
                SendReply(player, string.Format(EventMessageVoteWeaponAvaible, VotePlayersNeeded().ToString()));
                foreach (string kitname in eventgroundkits)
                {
                    SendReply(player, string.Format(EventMessageVoteWeaponVotes, kitname, GetWeaponVotes(kitname).ToString()));
                }
                return;
            }
            string voteweap = args[0];
            
            if (!eventgroundkits.Contains(voteweap))
            {
                SendReply(player, EventMessageErrorVoteNoKit);
                return;
            }

            if(voteweap == currentWeapon)
            {
                SendReply(player, string.Format(EventMessageErrorVoteAlreadyWeapon, currentWeapon));
                return;
            }

            if (hasAccess(player))
            {
                SetWeapon(voteweap);
                return;
            }

            SendReply(player, string.Format(EventMessageVoteWeaponVoted, voteweap));
            if (WeaponVote[bfplayer] == voteweap)
                return;
            WeaponVote[bfplayer] = voteweap;
            CheckWeaponVotes();
        }
    }
}

