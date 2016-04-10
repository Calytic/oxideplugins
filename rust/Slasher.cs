using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("Slasher", "k1lly0u", "0.1.32", ResourceId = 1662)]
    class Slasher : RustPlugin
    {

        [PluginReference]
        Plugin EventManager;

        [PluginReference]
        Plugin ZoneManager;

        [PluginReference]
        Plugin Spawns;

        private bool useSlasher;
        private bool SlasherStarted;
        private bool autoOpen;
        private bool gameOpen;
        private bool Changed;
        private bool failed;
        private bool timerStarted;

        private List<SlasherPlayer> SlasherPlayers = new List<SlasherPlayer>();
        public List<Timer> SlasherTimers = new List<Timer>();
        private List<ulong> DeadPlayers;        
        private List<BasePlayer> Slashers;
        private List<BasePlayer> Players;
        private Dictionary<ulong, Team> Teams;
        private Dictionary<string, string> displaynameToShortname;

        static string GameName = "Slasher";       

        static int RoundNumber;

        ////////////////////////////////////////////////////////////
        // SlasherPlayer class to store informations ////////////
        ////////////////////////////////////////////////////////////
        class SlasherPlayer : MonoBehaviour
        {
            public BasePlayer player;
            public int kills;
            public bool isSlasher;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                kills = 0;
            }
        }

        #region oxide hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            useSlasher = false;
            SlasherStarted = false;
            failed = false;
            gameOpen = false;
            autoOpen = false;
            timerStarted = false;
            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            displaynameToShortname = new Dictionary<string, string>();
            if (EventManager == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
            LoadVariables();
            RegisterGame();
            if (useAutoStart)
                TimeLoop();
        }
        void RegisterGame()
        {
            var success = EventManager.Call("RegisterEventGame", new object[] { GameName });
            if (success == null)
            {
                Puts("Event plugin doesn't exist");
                return;
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            if (useSlasher && SlasherStarted)
            {
                EventManager.Call("EndEvent", new object[] { });
                var objects = GameObject.FindObjectsOfType(typeof(SlasherPlayer));
                if (objects != null)
                    foreach (var gameObj in objects)
                        GameObject.Destroy(gameObj);
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (useSlasher && SlasherStarted)
            {
                try
                {
                    if (entity is BasePlayer && hitinfo.Initiator is BasePlayer)
                    {
                        var victim = (BasePlayer)entity;
                        var attacker = (BasePlayer)hitinfo.Initiator;
                        if (Teams.ContainsKey(victim.userID) && Teams.ContainsKey(attacker.userID))
                        {
                            if (victim.userID != attacker.userID)
                            {
                                if (Teams[victim.userID] == Teams[attacker.userID])
                                {
                                    hitinfo.damageTypes.ScaleAll(ffDamageMod);
                                    SendReply(attacker, lang.GetMessage("title", this, attacker.UserIDString) + lang.GetMessage("ff", this, attacker.UserIDString));
                                    return;
                                }
                                else if (hitinfo.WeaponPrefab.ToString().ToLower().Contains("torch"))
                                {
                                    hitinfo.damageTypes.ScaleAll(torchDamageMod);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
        #endregion     

        #region eventmanager hooks
        //////////////////////////////////////////////////////////////////////////////////////
        // Beginning Of Event Manager Hooks //////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void OnSelectEventGamePost(string name)
        {
            if (GameName == name)
            {
                useSlasher = true;
                if (PlayerSpawns != null && PlayerSpawns != "")
                    EventManager.Call("SelectSpawnfile", new object[] { PlayerSpawns });
            }
            else
                useSlasher = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (useSlasher && SlasherStarted)
            {
                if (!CheckForTeam(player)) TeamAssign(player);

                player.inventory.Strip();
                player.health = EventStartHealth;
                if (DeadPlayers.Contains(player.userID))
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("waitNext", this, player.UserIDString));
                    return;
                }
                if (Teams[player.userID] == Team.SLASHER)
                {
                    GiveSlasherGear(player);
                }
                else if (Teams[player.userID] == Team.HUNTED)
                {
                    GivePlayerGear(player);
                }                
            }
        }
        object OnSelectSpawnFile(string name)
        {
            if (useSlasher)
            {
                PlayerSpawns = name;
                return true;
            }
            return null;
        }
        void OnSelectEventZone(MonoBehaviour monoplayer, string radius)
        {
            if (useSlasher)
            {
                return;
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == GameName)
            {
                return;
            }
        }
        object CanEventOpen()
        {
            if (useSlasher)
            {
                var time = TOD_Sky.Instance.Cycle.Hour;
                if (time <= openHour && time >= endHour)
                {
                    return (lang.GetMessage("openTime", this) + openHour.ToString() + " & " + endHour.ToString());
                }
            }
            return null;
        }
        object CanEventStart()
        {
            if (useSlasher)
                if (SlasherPlayers.Count <= 1)
                    return "Not enough players to start the game";
            return null;
        }
        object OnEventOpenPost()
        {
            if (useSlasher)
            {               
                Teams = new Dictionary<ulong, Team>();
                DeadPlayers = new List<ulong>();
                Slashers = new List<BasePlayer>();
                Players = new List<BasePlayer>();
                EventManager.Call("BroadcastEvent", new object[] { lang.GetMessage("OpenMsg", this) });
            }
            return null;
        }
        object OnEventClosePost()
        {
            return null;
        }
        object OnEventEndPre()
        {
            if (useSlasher)
            {
                useSlasher = false;
                SlasherStarted = false;
                SlasherPlayers.Clear();
                DeadPlayers.Clear();
                Slashers.Clear();
                Players.Clear();
                Teams.Clear();
            }
            return null;
        }
        object OnEventEndPost()
        {
            
            return null;
        }
        object OnEventStartPre()
        {
            if (useSlasher)
            {
                SlasherStarted = true;
            }
            return null;
        }
        object OnEventStartPost()
        {
            if (useSlasher)
            {
                autoOpen = false;
                timerStarted = false;
                RoundNumber = 0;
                NextRound();
            }
            return null;
        }
        object CanEventJoin(BasePlayer player)
        {
            if (useSlasher)
            {
                
            }
            return null;
        }
        object OnSelectKit(string kitname)
        {
            if (useSlasher)
            {
                Puts("No Kits required for this gamemode!");
                return true;
            }
            return null;
        }
        object OnEventJoinPost(BasePlayer player)
        {
            if (useSlasher)
            {
                if (player.GetComponent<SlasherPlayer>())
                    GameObject.Destroy(player.GetComponent<SlasherPlayer>());
                SlasherPlayers.Add(player.gameObject.AddComponent<SlasherPlayer>());

                if (SlasherStarted)
                {
                    if (!Players.Contains(player)) Players.Add(player);
                    DeadPlayers.Add(player.userID);
                }                               
            }
            return null;
        }
        object OnEventLeavePost(BasePlayer player)
        {
            if (useSlasher)
            {
                if (player.GetComponent<SlasherPlayer>())
                {
                    if (DeadPlayers.Contains(player.userID)) DeadPlayers.Remove(player.userID);
                    if (Teams.ContainsKey(player.userID)) Teams.Remove(player.userID);
                    SlasherPlayers.Remove(player.GetComponent<SlasherPlayer>());
                    GameObject.Destroy(player.GetComponent<SlasherPlayer>());
                    Debug.Log("leavehere");
                    
                }
                if (SlasherPlayers.Count <= 1)
                {
                    var emptyobject = new object[] { };
                    EventManager.Call("BroadcastEvent", (lang.GetMessage("NoPlayers", this)));
                    EventManager.Call("CloseEvent", emptyobject);
                    EventManager.Call("EndEvent", emptyobject);                    
                }
            }
            return null;
        }
        void OnEventPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (useSlasher)
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
            if (useSlasher && SlasherStarted)
            {
                try
                {
                    if (hitinfo.Initiator != null)
                    {
                        BasePlayer attacker = hitinfo.Initiator.ToPlayer();
                        if (attacker != null)
                        {
                            if (attacker != victim)
                            {

                                DeadPlayers.Add(victim.userID);
                                if (victim.GetComponent<SlasherPlayer>().isSlasher)
                                {
                                    AddKill(attacker, victim, KillSlasherTokens);
                                    victim.GetComponent<SlasherPlayer>().isSlasher = false;
                                    if (RoundNumber >= maxRounds)
                                    {
                                        timerMsg(lang.GetMessage("gameEnd", this));
                                        DestroyTimers();
                                        timer.Once(10, () => CheckScores());
                                        return;
                                    }
                                    else
                                        timerMsg(string.Format(lang.GetMessage("killSlasher", this), attacker.displayName.ToString()));
                                    DestroyTimers();
                                    timer.Once(10, () => NextRound());
                                    return;
                                }
                                AddKill(attacker, victim, KillTokens);
                                CheckPlayers();
                            }
                        }
                    }
                }
                catch (Exception ex) { }
            }
            return;
        }
        object EventChooseSpawn(BasePlayer player, Vector3 destination)
        {
            if (useSlasher)
            {                
                if (!CheckForTeam(player))
                {
                    TeamAssign(player);
                    if (SlasherStarted) EventManager.Call("TeleportPlayerToEvent", player);
                    return null;
                }
                string spawnfile = "";
                if (DeadPlayers.Contains(player.userID)) spawnfile = DeadPlayerSpawns;
                else spawnfile = PlayerSpawns;

                var newpos = Spawns.Call("GetRandomSpawn", new object[] { spawnfile });
                return (Vector3)newpos;
            }
            return null;
        }
        object OnRequestZoneName()
        {
            if (useSlasher)
            {
                return EventZoneName;
            }
            return null;
        }
        #endregion

        #region slasher functions
        ////////////////////////////////////////////////////////////
        // Slasher Functions ///////////////////////////////////////
        ////////////////////////////////////////////////////////////

        private void NextRound()
        {
            if (useSlasher && SlasherStarted)
            {
                TOD_Sky.Instance.Cycle.Hour = startHour + 1;
                if (Players.Count == 0)
                {
                    Slashers.Clear();
                    foreach (var plyr in SlasherPlayers)
                    {
                        Players.Add(plyr.player);
                    }
                }               
                DeadPlayers.Clear();
                Teams.Clear();
                RoundNumber++;
                ChooseRandomSlasher();
                EventManager.Call("TeleportAllPlayersToEvent", new object[] { });
                BuildTimer(1);
            }         
        }        
        private void BuildTimer(int type)
        {
            float time;
            bool start = true;
            if (type == 1)
            {
                time = slasherTime;
                SlasherTimers.Add(timer.Repeat(1, slasherTime, () =>
                {
                    if (start)
                    {
                        timerMsg(string.Format("Round {0}", RoundNumber.ToString()));
                        timerMsg(string.Format(lang.GetMessage("weapAvail", this), slasherTime / 60, "Minutes"));
                        start = false;
                    }
                    time--;
                    if (time == 0)
                    {
                        BuildTimer(2);
                        GivePlayerWeapons();
                        return;
                    }
                    if (time == 60)
                    {
                        timerMsg(string.Format(lang.GetMessage("weapAvail", this), 1, "Minute"));
                    }
                    if (time == 30)
                    {
                        timerMsg(string.Format(lang.GetMessage("weapAvail", this), 30, "Seconds"));
                    }
                    if (time == 10)
                    {
                        timerMsg(string.Format(lang.GetMessage("weapAvail", this), 10, "Seconds"));
                    }
                }));
            }
            else if (type == 2)
            {
                time = playTimer;
                SlasherTimers.Add(timer.Repeat(1, playTimer, () =>
                {
                    if (start)
                    {
                        timerMsg(string.Format(lang.GetMessage("skillTime", this), playTimer / 60, "Minutes"));
                        start = false;
                    }
                    time--;
                    if (time == 0)
                    {
                        NextRound();
                        return;
                    }
                    if (time == 60)
                    {
                        timerMsg(string.Format(lang.GetMessage("skillTime", this), 1, "Minute"));
                    }
                    if (time == 30)
                    {
                        timerMsg(string.Format(lang.GetMessage("skillTime", this), 30, "Seconds"));
                    }
                    if (time == 10)
                    {
                        timerMsg(string.Format(lang.GetMessage("skillTime", this), 10, "Seconds"));
                    }
                }));
            }
        }        
        private void timerMsg(string left)
        {
            foreach (var player in SlasherPlayers)
            {
                SendReply(player.player, lang.GetMessage("title", this) + left);
            }
        }
        private void GiveWeapons(BasePlayer player)
        {
            player.inventory.GiveItem(BuildSlasherWeapon(SlasherWeapon), player.inventory.containerBelt);
            GiveItem(player, SlasherAmmo, AmmoAmount, player.inventory.containerMain);
            GiveItem(player, "machete", 1, player.inventory.containerBelt);
        }
        private void GiveSlasherGear(BasePlayer player)
        {
            GiveWeapons(player);
            Give(player, BuildItem("shoes.boots", 1, sBoots));
            Give(player, BuildItem("pants", 1, sPants));
            Give(player, BuildItem("tshirt", 1, sShirt));
            Give(player, BuildItem("mask.bandana", 1, sMask));
        }
        private void GivePlayerGear(BasePlayer player)
        {
            GiveItem(player, "torch", 2, player.inventory.containerBelt);
            Give(player, BuildItem("shoes.boots", 1, pBoots));
            Give(player, BuildItem("pants", 1, pPants));
            Give(player, BuildItem("tshirt", 1, pShirt));
            Give(player, BuildItem("hat.miner", 1, null));
        }
        private void GivePlayerWeapons()
        {            
            foreach (SlasherPlayer slasherplayer in SlasherPlayers)
            {
                if (Teams[slasherplayer.player.userID] == Team.HUNTED)
                    GiveWeapons(slasherplayer.player);
            }            
        }    
        private void CheckPlayers()
        {
            if (DeadPlayers.Count == (SlasherPlayers.Count - 1))
            {
                foreach (var plyr in SlasherPlayers)
                {
                    SendReply(plyr.player, lang.GetMessage("slasherWin", this));
                }
                if (RoundNumber >= maxRounds)
                {
                    timerMsg(lang.GetMessage("gameEnd", this));
                    DestroyTimers();
                    timer.Once(10, () => CheckScores());
                    return;
                }
                DestroyTimers();
                timer.Once(10, ()=> NextRound());
                return;
            }            
        }
        private void TimeLoop()
        {
            timer.Once(30, () => CheckTime());            
        }
        private void CheckTime()
        {
            var time = TOD_Sky.Instance.Cycle.Hour;
            if (!SlasherStarted)
            {                
                if (((time >= openHour && time <= 24) || (time >= 0 && time <= endHour)))
                {
                    if (!autoOpen && !failed)
                    {
                        ForceOpenEvent();
                    }
                    else if (autoOpen)
                    {
                        if (time >= startHour)
                        {
                            if (SlasherPlayers.Count >= 2)
                            {
                                if (!timerStarted)
                                {
                                    timerStarted = true;
                                    timerMsg(lang.GetMessage("startEvent", this));
                                    timer.Once(20, () => timerMsg(lang.GetMessage("10start", this)));
                                    timer.Once(30, () => EventManager.Call("StartEvent", new object[] { }));
                                }
                            }
                        }
                    }
                }
                if (time >= endHour && time <= openHour && gameOpen)
                { EventManager.Call("CloseEvent", new object[] { }); gameOpen = false; }
            }            
            TimeLoop();
        }
        private void ForceOpenEvent()
        {
            if (!SlasherStarted)
            {              

                if (PlayerSpawns == null)
                {
                    failed = true;
                    Puts("Failed to launch Slasher, player spawnfile not found");
                    return;
                }
                object success = Spawns.Call("GetSpawnsCount", new object[] { PlayerSpawns });
                if (success is string)
                {
                    if (!failed)
                    {
                        failed = true;
                        Puts("Failed to launch Slasher, invalid spawn file set");
                        return;
                    }
                    return;
                }
                if (DeadPlayerSpawns == null)
                {
                    failed = true;
                    Puts("Failed to launch Slasher, dead player spawnfile not found");
                    return;
                }
                object successD = Spawns.Call("GetSpawnsCount", new object[] { DeadPlayerSpawns });
                if (successD is string)
                {
                    if (!failed)
                    {
                        failed = true;
                        Puts("Failed to launch Slasher, invalid dead player spawn file set");
                        return;
                    }
                    return;
                }
                EventManager.Call("SelectEvent", new object[] { "Slasher" });
                EventManager.Call("SelectSpawnfile", new object[] { PlayerSpawns });

                var open = EventManager.Call("CanEventOpen", new object[] { });
                if (open is string)
                {
                    Puts("Can not start event because : " + open.ToString());
                    return;
                }

                autoOpen = true;
                gameOpen = true;
                
                EventManager.Call("OpenEvent", new object[] { });
                timer.Once(60, () => OpenReminder());
            }
        }
        private void OpenReminder()
        {
            if (!SlasherStarted && gameOpen)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    SendReply(player, lang.GetMessage("eventOpen", this, player.UserIDString));
                }
                timer.Once(180, () => OpenReminder());
            }

        }
        void DestroyTimers()
        {
            foreach (Timer sTimer in SlasherTimers)
            {
                sTimer.Destroy();
            }
            SlasherTimers.Clear();
        }

        ////////////////////////////////////////////////////////////
        // Give ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            List<ItemDefinition> IDef = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in IDef)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
        }
        public object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref)
        {
            itemname = itemname.ToLower();

            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];

            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
                return string.Format("{0} {1}", "Item not found: ", itemname);
            player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, amount, false), pref);
            return true;
        }
        private void Give(BasePlayer player, Item item)
        {
            player.inventory.GiveItem(item, player.inventory.containerWear);
        }
        private Item BuildItem(string shortname, int amount, object skin)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                if (skin != null)
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, false, (int) skin);
                    return item;
                }
                else
                {
                    Item item = ItemManager.CreateByItemID((int)definition.itemid, amount, false);
                    return item;
                }
            }
            return null;
        }
        private Item BuildSlasherWeapon(string shortname)
        {
            var definition = ItemManager.FindItemDefinition(shortname);
            if (definition != null)
            {
                Item sGun = ItemManager.CreateByItemID((int)definition.itemid, 1, false);

                var weapon = sGun.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    (sGun.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.primaryMagazine.capacity;
                }
                sGun.contents.AddItem(BuildItem("weapon.mod.flashlight", 1, null).info, 1);
                
                return sGun;
            }
            return null;
        }

        ////////////////////////////////////////////////////////////
        // Teams ///////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        enum Team
        {
            SLASHER,
            HUNTED,
            NONE
        }
        private bool CheckForTeam(BasePlayer player)
        {
            if (Teams.ContainsKey(player.userID))
            {
                return true;
            }
            return false;
        }
        private void TeamAssign(BasePlayer player)
        {
            if (useSlasher)
            {
                Team team = Team.HUNTED;
                Teams.Add(player.userID, team);
                player.GetComponent<SlasherPlayer>().isSlasher = false;                
            }        
        }
        private void ChooseRandomSlasher()
        {
            BasePlayer slasher = null;
            int num = UnityEngine.Random.Range(1, Players.Count);
            slasher = Players[num - 1];
            foreach (var p in SlasherPlayers)
            {
                Team team;
                if (p.player == slasher)
                {
                    team = Team.SLASHER;
                    p.player.GetComponent<SlasherPlayer>().isSlasher = true;
                    Slashers.Add(p.player);
                    Players.Remove(p.player);
                    SendReply(p.player, lang.GetMessage("title", this, p.player.UserIDString) + lang.GetMessage("slasherChoose", this, p.player.UserIDString));
                }
                else
                {
                    team = Team.HUNTED;
                    p.player.GetComponent<SlasherPlayer>().isSlasher = false;                    
                }
                Teams.Add(p.player.userID, team);
            }         
        } 
        #endregion      

        #region scoring
        ////////////////////////////////////////////////////////////
        // Scoring /////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        void AddKill(BasePlayer player, BasePlayer victim, int amount)
        {
            if (!player.GetComponent<SlasherPlayer>())
                return;

            player.GetComponent<SlasherPlayer>().kills++;
            EventManager.Call("AddTokens", player.userID.ToString(), amount);
            EventManager.Call("BroadcastEvent", string.Format(lang.GetMessage("KillMsg", this), player.displayName, player.GetComponent<SlasherPlayer>().kills.ToString(), victim.displayName));
        }
        void CheckScores()
        {
            int highScore = 0;
            BasePlayer winner = null;
            TOD_Sky.Instance.Cycle.Hour = endHour;
            foreach (var player in SlasherPlayers)
            {
                int score = player.player.GetComponent<SlasherPlayer>().kills;
                if (score > highScore)
                    highScore = score;
                winner = player.player;
            } 
                     
           if (winner == null) return;
           Winner(winner);
        }
        void Winner(BasePlayer player)
        {
            var winnerobjectmsg = new object[] { string.Format(lang.GetMessage("WinMsg", this), player.displayName) };
            EventManager.Call("AddTokens", player.userID.ToString(), WinTokens);
            var emptyobject = new object[] { };
            for (var i = 1; i < 10; i++)
            {
                EventManager.Call("BroadcastEvent", winnerobjectmsg);
            }
            EventManager.Call("CloseEvent", emptyobject);
            EventManager.Call("EndEvent", emptyobject);
        }
        #endregion

        #region console commands/auth
        ////////////////////////////////////////////////////////////
        // Console Commands ////////////////////////////////////////
        ////////////////////////////////////////////////////////////

        [ConsoleCommand("slasher.spawnfile")]
        void ccmdSpawns(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "slasher.spawnfile \"filename\"");
                return;
            }
            object success = EventManager.Call("SelectSpawnfile", (arg.Args[0]));
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            PlayerSpawns = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Slasher spawnfile is now {0} .", PlayerSpawns));
            failed = false;
        }
        [ConsoleCommand("slasher.deadspawnfile")]
        void ccmdDeadSpawns(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "slasher.deadspawnfile \"filename\"");
                return;
            }
            object success = EventManager.Call("SelectSpawnfile", (arg.Args[0]));
            if (success is string)
            {
                SendReply(arg, (string)success);
                return;
            }
            DeadPlayerSpawns = arg.Args[0];
            SaveConfig();
            SendReply(arg, string.Format("Slasher dead player spawnfile is now {0} .", DeadPlayerSpawns));
            failed = false;
        }
        [ConsoleCommand("slasher.toggle")]
        void ccmdToggle(ConsoleSystem.Arg arg)
        {
            if (!isAuth(arg)) return;
            if (useAutoStart)
            {
                useAutoStart = false;
                Puts("Autostart deactivated");
                return;
            }
            else if (!useAutoStart)
            {
                useAutoStart = true;
                Puts("Autostart activated");
                return;
            }
        }
        bool isAuth(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You dont not have permission to use this command.");
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region config
        //////////////////////////////////////////////////////////////////////////////////////
        // Configurations ////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        static Dictionary<string, object> EventZoneConfig;

    
        public float openHour = 18;
        public float startHour = 19.5f;
        public float endHour = 4f;

        static string EventZoneName = "Slasher";
        static string PlayerSpawns = "slasherspawns";
        static string DeadPlayerSpawns = "deadplayerspawns";
        static string SlasherWeapon = "shotgun.pump";
        static string SlasherAmmo = "ammo.shotgun";

        static float EventStartHealth = 100;
        static float ffDamageMod = 0.4f;
        static float torchDamageMod = 2.2f;
        static int slasherTime = 150;
        static int playTimer = 90;

        static bool useAutoStart = true;

        static int KillTokens = 1;
        static int WinTokens = 10;
        static int KillSlasherTokens = 3;
        static int AmmoAmount = 60;
        static int maxRounds = 2;

        static int sBoots = 10088;
        static int sPants = 10048;
        static int sShirt = 10038;
        static int sMask = 10064;
        static int pBoots = 10044;
        static int pPants = 10078;
        static int pShirt = 10039;


        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Slasher - Spawnfile", ref PlayerSpawns);
            CheckCfg("Slasher - Dead player spawnfile", ref DeadPlayerSpawns);
            CheckCfg("Slasher - Zone name", ref EventZoneName);
            CheckCfg("Slasher - Weapon - Slasher weapon shortname", ref SlasherWeapon);
            CheckCfg("Slasher - Weapon - Ammo type", ref SlasherAmmo);
            CheckCfg("Slasher - AutoStart - Use auto start", ref useAutoStart);
            CheckCfg("Slasher - Rounds to play per night cycle", ref maxRounds);


            CheckCfgFloat("Slasher - AutoStart - Time event will open", ref openHour);
            CheckCfgFloat("Slasher - AutoStart - Time event will start", ref startHour);
            CheckCfgFloat("Slasher - AutoStart - Time event will end", ref endHour);
            CheckCfgFloat("Slasher - Players - Starting health", ref EventStartHealth);
            CheckCfgFloat("Slasher - Players - Friendly fire damage modifier", ref ffDamageMod);
            CheckCfgFloat("Slasher - Players - Torch damage modifier", ref torchDamageMod);
            CheckCfg("Slasher - Round Timers - Slasher timer (seconds)", ref slasherTime);
            CheckCfg("Slasher - Round Timers - Play timer (seconds)", ref playTimer);

            CheckCfg("Tokens - Per Kill", ref KillTokens);
            CheckCfg("Tokens - Per Slasher Kill", ref KillSlasherTokens);
            CheckCfg("Tokens - On Win", ref WinTokens);

            CheckCfg("Skins - Slasher - Boots", ref sBoots);
            CheckCfg("Skins - Slasher - Pants", ref sPants);
            CheckCfg("Skins - Slasher - TShirt", ref sShirt);
            CheckCfg("Skins - Slasher - Bandana", ref sMask);
            CheckCfg("Skins - Player - Boots", ref pBoots);
            CheckCfg("Skins - Player - Pants", ref pPants);
            CheckCfg("Skins - Player - TShirt", ref pShirt);
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
            if (!useSlasher) return null;
            if (Config[configname] == null) return null;
            return Config[configname];
        }
        #endregion

        #region messages
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "<color=#cc0000>Slasher</color> : " },
            {"WinMsg", "{0} won the game with the most kills" },
            {"NoPlayers", "Slasher has no more players, auto-closing." },
            {"KillMsg", "{0} killed {2}. ({1} kills)" },
            {"OpenMsg", "In Slasher, the goal is to hide from the slasher, if you hide long enough you will be given weapons to take down the slasher" },
            {"waitNext", "You must wait for the next round."},
            {"openTime", "Slasher can only be played between the hours of " },
            {"killS", "You killed the slasher!"},
            {"killSlasher", "{0} killed the slasher, next round starts in 10 seconds" },
            {"gameEnd", "Event ends in 10 seconds" },
            {"weapAvail", "Weapons will be available in {0} {1}"},          
            {"skillTime", "{0} {1} left to kill the slasher!" },
            {"slasherWin", "The slasher has won the round! next round starts in 10 seconds"},
            {"slasherChoose", "You are the Slasher!" },
            {"startEvent", "Starting event in 30 seconds" },
            {"10start", "Event starts in 10 seconds"},
            {"ff", "Don't shoot your team mates!"},
            {"eventOpen", "<color=orange>Event:</color> The Event is now open for : <color=#cc0000>Slasher</color> !  Type /event_join to join!" }
        };
        #endregion
    }
}

