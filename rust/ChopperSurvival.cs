// Requires: EventManager
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ChopperSurvival", "k1lly0u", "0.2.2", ResourceId = 1590)]
    class ChopperSurvival : RustPlugin
    {        
        [PluginReference] EventManager EventManager;
        [PluginReference] Plugin Spawns;

        private bool isCurrent;
        private bool Active;
        
        private float adjHeliHealth;
        private float adjHeliBulletDamage;
        private float adjMainRotorHealth;
        private float adjEngineHealth;
        private float adjTailRotorHealth;
        private float adjHeliAccuracy;

        private int WaveNumber;

        public string CurrentKit;

        private List<CS_Player> CSPlayers = new List<CS_Player>();
        private List<Timer> GameTimers = new List<Timer>();
        private List<CS_Helicopter> CSHelicopters = new List<CS_Helicopter>();
        #region Classes
        class CS_Player : MonoBehaviour
        {
            public BasePlayer player;
            public int deaths;
            public int points;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                enabled = false;
                deaths = 0;
                points = 0;
            }
        }      
        class CS_Helicopter : MonoBehaviour
        {
            public BaseHelicopter Helicopter;
            public PatrolHelicopterAI AI;
            public Vector3 Destination;
            public float MainHealth;
            public float EngineHealth;
            public float TailHealth;
            public float BodyHealth;

            void Awake()
            {
                Helicopter = GetComponent<BaseHelicopter>();                
                AI = Helicopter.GetComponent<PatrolHelicopterAI>();
                Helicopter.maxCratesToSpawn = 0;               
                InvokeRepeating("CheckDistance", 5, 5);
            }
            private void OnDestroy()
            {
                CancelInvoke("CheckDistance"); 
            }
            public int DealDamage(HitInfo info)
            {
                int pointValue = 0;
                bool hitWeakSpot = false;
                
                BaseHelicopter.weakspot[] weakspotArray = Helicopter.weakspots;
                for (int i = 0; i < (int)weakspotArray.Length; i++)
                {
                    BaseHelicopter.weakspot _weakspot = weakspotArray[i];
                    string[] strArrays = _weakspot.bonenames;
                    for (int j = 0; j < strArrays.Length; j++)
                    {
                        string str = strArrays[j];
                        if (info.HitBone == StringPool.Get(str))
                        {
                            switch (str)
                            {
                                case "engine_col":
                                    hitWeakSpot = true;
                                    EngineHealth -= info.damageTypes.Total();
                                    pointValue = HeliHitPoints;
                                    break;
                                case "tail_rotor_col":
                                    hitWeakSpot = true;
                                    TailHealth -= info.damageTypes.Total();
                                    pointValue = RotorHitPoints;
                                    if (TailHealth < 50)
                                        Helicopter.weakspots[i].WeakspotDestroyed();
                                    break;
                                case "main_rotor_col":
                                    hitWeakSpot = true;
                                    MainHealth -= info.damageTypes.Total();
                                    pointValue = RotorHitPoints;
                                    if (MainHealth < 50)
                                        Helicopter.weakspots[i].WeakspotDestroyed();
                                    break;
                            }                            
                        }
                    }
                }                
                if (!hitWeakSpot)
                {
                    pointValue = 1;
                    BodyHealth -= info.damageTypes.Total();
                }
                if (BodyHealth < 1 || EngineHealth < 1 || (TailHealth < 1 && MainHealth < 1))
                    AI.CriticalDamage();
                return pointValue;
            }
            public void SetDestination(Vector3 destination)
            {
                Destination = destination;
                AI.State_Move_Enter(Destination + new Vector3(0.0f, 10f, 0.0f));
            }
            public void SetStats(int health, float main, float engine, float tail, float damage)
            {
                MainHealth = main;
                TailHealth = tail;
                EngineHealth = engine;
                BodyHealth = health;
                Helicopter.bulletDamage = damage;
            }
            private void CheckDistance(BaseEntity entity)
            {
                if (entity == null) return;
                var currentPos = entity.transform.position;
                if (Destination != null)
                {
                    AI.SetTargetDestination(Destination + new Vector3(0.0f, 10f, 0.0f));                    

                    if (Vector3Ex.Distance2D(currentPos, Destination) < 60)
                    {
                        if (useRockets)
                        {
                            int num = UnityEngine.Random.Range(1, 3);
                            if (num == 2)
                                AI.State_Strafe_Think(0);                            
                        }
                        else AI.State_Orbit_Think(40f);
                    }
                    else
                        AI.State_Move_Enter(Destination + new Vector3(0.0f, 10f, 0.0f));
                                    
                }
            }
        }
        class LeaderBoard
        {
            public string Name;
            public int Kills;
        }
        public enum Hitpoints
        {
            Body,
            Main,
            Tail
        }
        #endregion

        #region UI Scoreboard
        private List<CS_Player> SortScores() => CSPlayers.OrderByDescending(pair => pair.points).ToList();           
        
        private string PlayerMsg(int key, CS_Player player) => $"|  <color=#FF8C00>{key}</color>.  <color=#FF8C00>{player.player.displayName}</color> <color=#939393>--</color> <color=#FF8C00>{player.points}</color>  |";
        
        private CuiElementContainer CreateScoreboard(BasePlayer player)
        {
            DestroyUI(player);
            string panelName = "CSScoreBoard";
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
            foreach (var entry in CSPlayers)
            {
                DestroyUI(entry.player);
                AddUI(entry.player);
            }
        }   
        private void AddUI(BasePlayer player) => CuiHelper.AddUi(player, CreateScoreboard(player));
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "CSScoreBoard");
        void ShowHealth()
        {
            foreach (var p in CSPlayers)            
                foreach (var heli in CSHelicopters) 
                    GameTimers.Add(timer.Repeat(0.1f, 0, () =>
                    {
                        if (heli != null)
                            if (Vector3.Distance(p.player.transform.position, heli.Helicopter.transform.position) > 40)
                            p.player.SendConsoleCommand("ddraw.text", 0.1f, Color.green, heli.Helicopter.transform.position + new Vector3(0, 2, 0), $"<size=16>H: { (int)heli.BodyHealth }, MR: {(int)heli.MainHealth}, TR: {(int)heli.TailHealth}, EN: {(int)heli.EngineHealth}</size>");
                    }));
        }        
        #endregion

        #region Oxide hooks
        void OnServerInitialized()
        {
            isCurrent = false;
            Active = false;
            if (EventManager == null)
            {
                Puts(MSG("noEvent"));
                return;
            }
            LoadVariables();
            RegisterMessages();
            RegisterGame();
        }
        void RegisterGame()
        {
            var success = EventManager.RegisterEventGame(EventName);
            if (success == null)
                Puts(MSG("noEvent"));                
        }
        protected override void LoadDefaultConfig()
        {
            Puts(MSG("noConfig"));
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) DestroyUI(player);
            if (isCurrent && Active)                                    
                EventManager.EndEvent(); 
            DestroyEvent();
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (isCurrent && Active)
            {
                try
                {
                    if (hitInfo.Initiator is BasePlayer)
                    {
                        var attacker = (BasePlayer)hitInfo.Initiator;

                        if (entity is BasePlayer)
                        {
                            if (entity.ToPlayer() == null || hitInfo == null) return;
                            if (entity.ToPlayer().userID != hitInfo.Initiator.ToPlayer().userID)                            
                                if (entity.GetComponent<CS_Player>())
                                {
                                    hitInfo.damageTypes.ScaleAll(DamageScale);
                                    MessagePlayer(attacker, "", MSG("fFire"));
                                }                            
                        }
                        if (entity.GetComponent<BaseHelicopter>())
                            if (entity.GetComponent<CS_Helicopter>())
                            {
                                int points = entity.GetComponent<CS_Helicopter>().DealDamage(hitInfo);
                                hitInfo.damageTypes.ScaleAll(0);
                                attacker.GetComponent<CS_Player>().points += points;
                            }
                    }
                }
                catch (NullReferenceException ex)
                {
                }                
            }
        }        
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (isCurrent && Active)
            {
                if (entity is BaseEntity)
                {
                    var entityName = entity.LookupShortPrefabName();

                    if (entityName.Contains("napalm"))
                        if (!useRockets)
                            KillEntity(entity as BaseEntity);

                    if (entityName.Contains("servergibs_patrolhelicopter"))                    
                        KillEntity(entity as BaseEntity);                    
                }
            }
        }    
        void KillEntity(BaseEntity entity)
        {
            if (BaseEntity.saveList.Contains(entity))
                BaseEntity.saveList.Remove(entity);
            entity.KillMessage();
        }         
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (isCurrent && Active)            
                if (entity.GetComponent<BaseHelicopter>())                
                    if (entity.GetComponent<CS_Helicopter>())
                    {
                        DestroyFires(entity.transform.position);
                        CSHelicopters.Remove(entity.GetComponent<CS_Helicopter>());
                        UnityEngine.Object.Destroy(entity.GetComponent<CS_Helicopter>());                        
                        MessageAllPlayers(MSG("heliDest"), "", true);

                        if (CSHelicopters.Count == 0)
                            NextRound();
                    }
        }
        #endregion

        #region Chopper Survival
        private void StartRounds()
        {
            if (isCurrent && Active)
            {
                WaveNumber = 1;
                MessageAllPlayers(string.Format(MSG("firstWave"), 20), "", true);
                SetPlayers();
                GameTimers.Add(timer.Once(20, () => SpawnWave()));
                GameTimers.Add(timer.Repeat(5, 0, () => RefreshUI()));
            }
        }
        private void NextRound()
        {
            if (isCurrent && Active)
            {
                DestroyTimers();
                GameTimers.Add(timer.Repeat(5, 0, () => RefreshUI()));
                WaveNumber++;
                AddPoints();
                if (EventManager.EventMode == EventManager.GameMode.Normal)
                {
                    if (WaveNumber > MaximumWaves)
                    {
                        FindWinner();
                        return;
                    }
                }               
                SetPlayers();
                MessageAllPlayers(string.Format(MSG("nextWave"), SpawnWaveTimer), "", true);
                GameTimers.Add(timer.Once(SpawnWaveTimer, () => SpawnWave()));
            }
        }
        private void SetPlayers()
        {
            foreach (CS_Player hs in CSPlayers)
            {
                EventManager.GivePlayerKit(hs.player, CurrentKit);
                hs.player.health = StartHealth;
            }
        }
        private void SpawnWave()
        {
            if (isCurrent && Active)
            {                
                var num = Math.Ceiling(((float)WaveNumber / (float)MaximumWaves) * (float)MaximumHelicopters);
                if (num < 1) num = 1;
                if (WaveNumber == 1) InitStatModifiers();
                else SetStatModifiers();
                SpawnHelicopter((int)num);
                SetHelicopterStats();
                ShowHealth();
                MessageAllPlayers("", string.Format(MSG("waveInbound"), WaveNumber));
            }
        }
        private void SpawnHelicopter(int num)
        {
            for (int i = 0; i < num; i++)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (entity)
                {
                    BaseHelicopter heli = entity.GetComponent<BaseHelicopter>();
                    entity.Spawn(true);
                    heli.health = 10000;
                    CSHelicopters.Add(heli.gameObject.AddComponent<CS_Helicopter>());
                    MoveToArena(entity);
                }
            }
        }

        private void InitStatModifiers()
        {
            adjHeliBulletDamage = HeliBulletDamage;
            adjHeliHealth = HeliHealth;
            adjMainRotorHealth = MainRotorHealth;
            adjEngineHealth = EngineHealth;
            adjTailRotorHealth = TailRotorHealth;
            adjHeliAccuracy = HeliAccuracy;
            if (showStats) ShowHeliStats();
        }
        private void SetStatModifiers()
        {
            if (isCurrent)
            {
                adjHeliBulletDamage *= HeliModifier;
                adjHeliHealth *= HeliModifier;
                adjMainRotorHealth *= HeliModifier;
                adjEngineHealth *= HeliModifier;
                adjTailRotorHealth = adjTailRotorHealth * HeliModifier;
                adjHeliAccuracy = adjHeliAccuracy - (HeliModifier / 1.5f);
                if (showStats) ShowHeliStats();
            }
        }
        private void ShowHeliStats()
        {
            Puts("---- CS Heli Stats ----");
            Puts("Modifier: " + HeliModifier);
            Puts("Damage: " + adjHeliBulletDamage);
            Puts("Health: " + adjHeliHealth);
            Puts("Main rotor: " + adjMainRotorHealth);
            Puts("Engine: " + adjEngineHealth);
            Puts("Tail rotor: " + adjTailRotorHealth);
            Puts("Accuracy: " + adjHeliAccuracy);
        }       
        private void SetHelicopterStats()
        {
            foreach (var heli in CSHelicopters)            
                heli.SetStats((int)adjHeliHealth, adjMainRotorHealth, adjEngineHealth, adjTailRotorHealth, adjHeliBulletDamage);
            ConVar.PatrolHelicopter.bulletAccuracy = adjHeliAccuracy;
        }
        private void MoveToArena(BaseEntity entity)
        {
            Vector3 spawnPos = FindSpawnPosition(GetDestination());
            entity.transform.position = spawnPos;
            entity.GetComponent<CS_Helicopter>().SetDestination(GetDestination());
        }
        private Vector3 GetDestination() => (Vector3)Spawns.Call("GetRandomSpawn", new object[] { EventSpawnFile });
        private Vector3 FindSpawnPosition(Vector3 arenaPos)
        {
            float x = 0;
            float y = 0;
            float angleRadians = 0;
            Vector2 circleVector;
            angleRadians = UnityEngine.Random.Range(0, 180) * Mathf.PI / 180.0f;
            x = SpawnDistance * Mathf.Cos(angleRadians);
            y = SpawnDistance * Mathf.Sin(angleRadians);
            circleVector = new Vector2(x, y);           
            Vector3 finalPos = FindGround(new Vector3(circleVector.x + arenaPos.x, 0, circleVector.y + arenaPos.z));
            if (finalPos.y < 1) finalPos.y = 5;
            finalPos.y = finalPos.y + 50;

            return finalPos;
        } 
        
        private void DestroyEvent()
        {
            Active = false;            
            DestroyTimers();
            DestroyHelicopters();
            DestroyPlayers();
        }
        private void DestroyHelicopters()
        {
            if (CSHelicopters != null)
            {
                foreach (var heli in CSHelicopters)
                {
                    DestroyFires(heli.Helicopter.transform.position);
                    heli.Helicopter.DieInstantly();
                    UnityEngine.Object.Destroy(heli);
                }
                CSHelicopters.Clear();
            }
        }
        private void DestroyFires(Vector3 pos)
        {
            timer.Once(5, () =>
            {
                //var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();                
                var allobjects = Physics.OverlapSphere(pos, 150);
                foreach (var gobject in allobjects)
                {                   
                    if (gobject.name.ToLower().Contains("fireball"))
                    {                       
                        var fire = gobject.GetComponent<BaseEntity>();
                        if (Vector3.Distance(fire.transform.position, pos) < 200)
                        {
                            KillEntity(fire);
                            UnityEngine.Object.Destroy(gobject);
                        }
                    }
                }
            });
        }
        private void DestroyTimers()
        {
            if (GameTimers != null)
            {
                foreach (var time in GameTimers)
                    time.Destroy();
                GameTimers.Clear();
            }
        }
      
        private void DestroyPlayers()
        {
            if (CSPlayers != null)
            {
                foreach (var player in CSPlayers)
                {
                    DestroyUI(player.player);
                    UnityEngine.Object.Destroy(player);
                }
                CSPlayers.Clear();
            }       
        }

        static Vector3 FindGround(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, LayerMask.GetMask("Terrain", "World", "Construction")))            
                sourcePos.y = hitInfo.point.y;            
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }
        
        #endregion

        #region EventManager hooks
        void OnSelectEventGamePost(string name)
        {
            if (EventName == name)
            {
                isCurrent = true;
                if (EventSpawnFile != null && EventSpawnFile != "")
                    EventManager.SelectSpawnfile(EventSpawnFile);
            }
            else
                isCurrent = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (isCurrent && Active)
            {                
                player.inventory.Strip();
                EventManager.GivePlayerKit(player, CurrentKit);
                player.health = StartHealth;
                timer.Once(3, ()=> AddUI(player));
            }
        }       
        object OnEventOpenPost()
        {
            if (isCurrent)
            {
                MessageAll(MSG("openBroad"), "", true);
            }
            return null;
        }

        object OnEventCancel()
        {
            FindWinner();
            return null;
        }
        object OnEventEndPre()
        {
            if (isCurrent)            
                DestroyEvent();            
            return null;
        }    
        object OnEventEndPost()
        {
            var objPlayers = UnityEngine.Object.FindObjectsOfType<CS_Player>();
            if (objPlayers != null)
                foreach (var gameObj in objPlayers)
                    UnityEngine.Object.Destroy(gameObj);
            var objHelis = UnityEngine.Object.FindObjectsOfType<CS_Helicopter>();
            if (objHelis != null)
                foreach (var gameObj in objHelis)
                    UnityEngine.Object.Destroy(gameObj);
            return null;
        }       
        object OnEventStartPre()
        {
            if (isCurrent)
            {
                Active = true;                
            }          
            return null;
        }        
        object OnSelectKit(string kitname)
        {
            if (isCurrent)
            {
                CurrentKit = kitname;
                return true;
            }
            return null;
        }
        
        object OnEventJoinPost(BasePlayer player)
        {
            if (isCurrent)
            {
                if (player.GetComponent<CS_Player>())
                    UnityEngine.Object.Destroy(player.GetComponent<CS_Player>());
                CSPlayers.Add(player.gameObject.AddComponent<CS_Player>());
                if (Active)
                OnEventPlayerSpawn(player);                              
            }
            return null;
        }       
        object OnEventLeavePost(BasePlayer player)
        {
            if (isCurrent)            
                if (player.GetComponent<CS_Player>())
                {
                    CSPlayers.Remove(player.GetComponent<CS_Player>());
                    UnityEngine.Object.Destroy(player.GetComponent<CS_Player>());
                }            
            if (Active)            
                if (CSPlayers.Count == 0)
                    EventManager.EndEvent();                           
            return null;
        }        
        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (Active)
            {
                DestroyUI(victim);
                victim.GetComponent<CS_Player>().deaths++;
                int LivesLeft = (DeathLimit - victim.GetComponent<CS_Player>().deaths);

                MessageAll(string.Format(MSG("eventDeath"), victim.displayName, victim.GetComponent<CS_Player>().deaths, DeathLimit), "", true);               
                MessagePlayer(victim, string.Format(MSG("livesLeft"), LivesLeft), "", true);

                if (victim.GetComponent<CS_Player>().deaths >= DeathLimit)
                {
                    if (CSPlayers.Count == 1)
                    {
                        Winner(victim);
                        return;
                    }
                    EventManager.LeaveEvent(victim);                    
                }
            }
            return;
        }    
        object OnRequestZoneName()
        {
            if (isCurrent) 
                if (!string.IsNullOrEmpty(EventZoneName))          
                    return EventZoneName;            
            return null;
        }
        object OnSelectSpawnFile(string name)
        {
            if (isCurrent)
            {
                EventSpawnFile = name;
                return true;
            }
            return null;
        }        
        object OnEventStartPost()
        {            
            StartRounds();            
            return null;
        }
        #endregion

        #region Messaging
        private string MSG(string msg) => lang.GetMessage(msg, this);
        private void MessageAll(string msg, string keyword = "", bool title = false)
        {
            string titlestring = "";
            if (title) titlestring = lang.GetMessage("title", this);
            PrintToChat(MainColor + titlestring + keyword + "</color>" + MSGColor + msg + "</color>");
        }
        private void MessageAllPlayers(string msg, string keyword = "", bool title = false)
        {
            string titlestring = "";
            if (title) titlestring = lang.GetMessage("title", this);
            foreach (var csplayer in CSPlayers)
                SendReply(csplayer.player, MainColor + titlestring + keyword + "</color>" + MSGColor + msg + "</color>");
        }
        private void MessagePlayer(BasePlayer player, string msg, string keyword = "", bool title = false)
        {
            string titlestring = "";
            if (title) titlestring = lang.GetMessage("title", this);
            SendReply(player, MainColor + titlestring + keyword + "</color>" + MSGColor + msg + "</color>");
        }
        private void RegisterMessages() => lang.RegisterMessages(new Dictionary<string, string>()
        {
            {"noEvent", "Event plugin doesn't exist" },
            {"noConfig", "Creating a new config file" },
            {"title", "ChopperSurvival : "},
            {"fFire", "Friendly Fire!"},
            {"nextWave", "Next wave in {0} seconds!"},
            {"noPlayers", "The event has no more players, auto-closing."},
            {"openBroad", "Fend off waves of attacking helicopters! Each hit gives you a point, Rotor hits are worth more. The last player standing, or the player with the most points wins!"},
            {"eventWin", "{0} has won the event with {1} points!"},
            {"eventDeath", "{0} has died {1}/{2} times!"},
            {"waveInbound", "Wave {0} inbound!"},
            {"firstWave", "You have {0} seconds to prepare for the first wave!"},
            {"heliDest", "Helicopter Destroyed!"},
            {"livesLeft", "You have {0} lives remaining!"},
            {"notEnough", "Not enough players to start the event"}
        },this);
        #endregion

        #region Config
        private bool Changed;

        static string DefaultKit = "cskit";
        static string EventName = "ChopperSurvival";
        static string EventZoneName = "cszone";
        static string EventSpawnFile = "csspawns";

        static float StartHealth = 100;
        static float DamageScale = 0.0f;

        static float HeliBulletDamage = 4.0f;
        static float HeliHealth = 3800.0f;
        static float MainRotorHealth = 420.0f;
        static float TailRotorHealth = 300.0f;
        static float EngineHealth = 800f;
        static float HeliSpeed = 24.0f;
        static float HeliAccuracy = 8.0f;
        static float HeliModifier = 1.22f;
        static bool useRockets = true;

        static float SpawnDistance = 500f;
        static float SpawnWaveTimer = 10.0f;

        static int RotorHitPoints = 3;
        static int HeliHitPoints = 1; 
               
        static int DeathLimit = 10;

        static int SurvivalPoints = 1;
        static int WinnerPoints = 10;

        static int MaximumWaves = 10; 
        static int MaximumHelicopters = 4;        

        static bool showStats = true;

        static string MainColor = "<color=#FF8C00>";
        static string MSGColor = "<color=#939393>";

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Scoring - Rotor hit points", ref RotorHitPoints);
            CheckCfg("Scoring - Body hit points", ref HeliHitPoints);

            CheckCfg("Scoring - Round survival tokens", ref SurvivalPoints);
            CheckCfg("Scoring - Winner tokens", ref WinnerPoints);

            CheckCfgFloat("Helicopter - Base Stats - Health", ref HeliHealth);
            CheckCfgFloat("Helicopter - Base Stats- Main rotor health", ref MainRotorHealth);
            CheckCfgFloat("Helicopter - Base Stats- Tail rotor health", ref TailRotorHealth);
            CheckCfgFloat("Helicopter - Base Stats- Engine health", ref EngineHealth);
            CheckCfgFloat("Helicopter - Base Stats- Speed", ref HeliSpeed);
            CheckCfgFloat("Helicopter - Base Stats- Accuracy", ref HeliAccuracy);
            CheckCfgFloat("Helicopter - Base Stats- Bullet damage", ref HeliBulletDamage);
            CheckCfg("Helicopter - Use rockets", ref useRockets);
            CheckCfgFloat("Helicopter - Stat modifier", ref HeliModifier);

            CheckCfgFloat("Player - Starting Health", ref StartHealth);
            CheckCfgFloat("Player - FriendlyFire ratio", ref DamageScale);
            CheckCfg("Player - Death limit", ref DeathLimit);

            CheckCfgFloat("Spawning - Wave timer (between rounds)", ref SpawnWaveTimer);
            CheckCfg("Spawning - Maximum waves", ref MaximumWaves);
            CheckCfgFloat("Spawning - Spawn distance (away from arena)", ref SpawnDistance);
            CheckCfg("Spawning - Maximum Helicopters", ref MaximumHelicopters);
                       
            CheckCfg("Debug - Print heli stats on spawn (for balancing)", ref showStats);
            CheckCfg("Kit - Default kit", ref DefaultKit);
            CheckCfg("Zone - Zone name", ref EventZoneName);
            CheckCfg("Spawns - Default spawnfile", ref EventSpawnFile);
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
            if (!isCurrent) return null;
            if (Config[configname] == null) return null;
            return Config[configname];
        }
        #endregion

        #region Scoring
        void AddPoints()
        {
            foreach (CS_Player helisurvivalplayer in CSPlayers)            
                EventManager.AddTokens(helisurvivalplayer.player.UserIDString, (SurvivalPoints * (WaveNumber / 2)));
        }
        void FindWinner()
        {            
            AddPoints();
            int i = 0;
            BasePlayer winner = null;
            foreach (var player in CSPlayers)            
                if (player.GetComponent<CS_Player>().points > i)
                {
                    i = player.GetComponent<CS_Player>().points;
                    winner = player.player;
                }            
            if (winner != null)
                Winner(winner);
        }
        void Winner(BasePlayer player)
        {
            EventManager.AddTokens(player.UserIDString, WinnerPoints);
            MessageAllPlayers(string.Format(MSG("eventWon"), player.displayName, player.GetComponent<CS_Player>().points), "", true);
            var emptobject = new object[] { };
            EventManager.CloseEvent();
            EventManager.EndEvent();
        }
        #endregion
    }
}

