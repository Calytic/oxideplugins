using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ChopperSurvival", "k1lly0u", "0.2.0", ResourceId = 1590)]
    class ChopperSurvival : RustPlugin
    {        
        [PluginReference] Plugin EventManager;
        [PluginReference] Plugin ZoneManager;
        [PluginReference] Plugin Spawns;

        private bool isCurrent;
        private bool Active;
        
        private float adjHeliHealth;
        private float adjHeliBulletDamage;
        private float adjMainRotorHealth;
        private float adjTailRotorHealth;
        private float adjHeliAccuracy;

        private int WaveNumber;
        private int HeliDistance = 50;

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
            public BaseHelicopter helicopter;
            public float health;

            void Awake()
            {
                helicopter = GetComponent<BaseHelicopter>();
                health = 10000;
            }
            public void DamageHeli(float amount)
            {
                health = health - amount;
                if (health < 50)
                    helicopter.GetComponent<PatrolHelicopterAI>().State_Death_Enter();
                helicopter.health = health;
                helicopter.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }
        #endregion

        #region UI Scoreboard
        private Dictionary<string, int> FindHighScores() => CSPlayers.OrderByDescending(pair => pair.points).Take(3).ToDictionary(pair => pair.player.displayName, pair => pair.points);
        private CuiElementContainer CreateScoreboard()
        {
            string panelName = "CSScoreBoard";
            var element = new CuiElementContainer()
            {
                {
                    new CuiPanel
                    {
                        Image = {Color = "0.3 0.3 0.3 0.6"},
                        RectTransform = {AnchorMin = "0.1 0.95", AnchorMax = "0.9 1"},
                    },
                    new CuiElement().Parent,
                    panelName
                }
            };
            int i = 1;
            string scoreMsg = "";
            foreach (var entry in FindHighScores())
            {
                scoreMsg = scoreMsg + $"|  {MainColor}{i}</color>.  {MainColor}{entry.Key}</color> {MSGColor}--</color> {MainColor}{entry.Value}</color>  |";
                i++;
            }
            element.Add(new CuiLabel
            {
                Text = { FontSize = 18, Align = TextAnchor.MiddleCenter, Text = scoreMsg },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            },
            panelName);
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
       
        private void AddUI(BasePlayer player) => CuiHelper.AddUi(player, CreateScoreboard());
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "CSScoreBoard");
        void ShowHealth()
        {
            foreach (var p in CSPlayers)            
                foreach (var heli in CSHelicopters) 
                    GameTimers.Add(timer.Repeat(0.1f, 0, () =>
                    {
                        if (heli != null)
                            if (Vector3.Distance(p.player.transform.position, heli.helicopter.transform.position) > 80)
                            p.player.SendConsoleCommand("ddraw.text", 0.1f, Color.green, heli.helicopter.transform.position + new Vector3(0, 2, 0), $"<size=16>H: { (int)heli.health }, MR: {(int)heli.helicopter.weakspots[0].health}, TR: {(int)heli.helicopter.weakspots[1].health}</size>");
                    }));
        }
        #endregion

        #region Oxide hooks
        void OnServerInitialized()
        {
            isCurrent = false;
            Active = false;
            if (!EventManager)
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
            var success = EventManager.Call("RegisterEventGame", new object[] { EventName });
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
            if (Active)
                EventManager.Call("EndEvent", new object[] { });
            DestroyGame();
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
                                if (hitInfo.HitMaterial == 4214819287) attacker.GetComponent<CS_Player>().points += RotorHitPoints;
                                else attacker.GetComponent<CS_Player>().points += HeliHitPoints;
                                entity.GetComponent<CS_Helicopter>().DamageHeli(hitInfo.damageTypes.Total());
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
                var entityName = entity.LookupShortPrefabName();

                if (entityName.Contains("napalm"))
                    if (!useRockets)
                        entity.KillMessage();

                if (entityName.Contains("servergibs_patrolhelicopter"))
                    entity.KillMessage();
            }
        }             
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (isCurrent && Active)            
                if (entity.GetComponent<BaseHelicopter>())                
                    if (entity.GetComponent<CS_Helicopter>())
                    {
                        //entity.GetComponent<BaseHelicopter>().maxCratesToSpawn = 0;
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
                if (WaveNumber > MaximumWaves)
                {
                    FindWinner();
                    return;
                }                
                SetPlayers();
                MessageAllPlayers(string.Format(MSG("nextWave"), SpawnWaveTimer), "", true);
                GameTimers.Add(timer.Once(SpawnWaveTimer, () => SpawnWave()));
            }
        }
        private void SetPlayers()
        {
            foreach (CS_Player helisurvivalplayer in CSPlayers)
            {
                helisurvivalplayer.player.inventory.Strip();
                EventManager.Call("GivePlayerKit", new object[] { helisurvivalplayer.player, CurrentKit });
                helisurvivalplayer.player.health = StartHealth;
            }
        }
        private void SpawnWave()
        {
            if (isCurrent && Active)
            {
                int num = (MaximumHelicopters / MaximumWaves) * WaveNumber;
                if (num < 1) num = 1;              
                if (WaveNumber == 1) InitStatModifiers();
                else SetStatModifiers();
                SpawnHelicopter(num);
                SetHelicopterStats();
                ShowHealth();
                MessageAllPlayers("", string.Format(MSG("waveInbound"), WaveNumber));
            }
        }
        private void SpawnHelicopter(int num)
        {
            int i = 0;
            while (i < num)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (entity)
                {
                    BaseHelicopter heli = entity.GetComponent<BaseHelicopter>();
                    entity.Spawn(true);
                    heli.health = 10000;
                    CSHelicopters.Add(heli.gameObject.AddComponent<CS_Helicopter>());                    
                    MoveToArena(entity, GetDestination());
                    CheckDistance(entity);
                    i++;
                }
            }
        }

        private void InitStatModifiers()
        {
            adjHeliBulletDamage = HeliBulletDamage;
            adjHeliHealth = HeliHealth;
            adjMainRotorHealth = MainRotorHealth;
            adjTailRotorHealth = TailRotorHealth;
            adjHeliAccuracy = HeliAccuracy;
            if (showStats) ShowHeliStats();
        }
        private void SetStatModifiers()
        {
            if (isCurrent)
            {
                adjHeliBulletDamage = adjHeliBulletDamage * HeliModifier;
                adjHeliHealth = adjHeliHealth * HeliModifier;
                adjMainRotorHealth = adjMainRotorHealth * HeliModifier;
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
            Puts("Tail rotor: " + adjTailRotorHealth);
            Puts("Accuracy: " + adjHeliAccuracy);
        }       
        private void SetHelicopterStats()
        {
            foreach (var heli in CSHelicopters)
            {
                var weakspots = heli.helicopter.weakspots;
                weakspots[0].maxHealth = adjMainRotorHealth;
                weakspots[0].health = adjMainRotorHealth;
                weakspots[1].maxHealth = adjTailRotorHealth;
                weakspots[1].health = adjTailRotorHealth;
                heli.helicopter.maxCratesToSpawn = 0;                
                heli.health = adjHeliHealth;

                heli.helicopter.bulletDamage = adjHeliBulletDamage;
            }
            ConVar.PatrolHelicopter.bulletAccuracy = adjHeliAccuracy;
        }

        private Vector3 GetDestination() => (Vector3)Spawns.Call("GetRandomSpawn", new object[] { EventSpawnFile });
        private Vector3 FindSpawnPosition(Vector3 arenaPos)
        {
            Vector3 spawnPos = new Vector3(0,0,0);
            float randX = RandomRange(SpawnDistance);
            float randZ = RandomRange(SpawnDistance);
            spawnPos.x = arenaPos.x - randX;
            spawnPos.z = arenaPos.z - randZ;
            Vector3 finalPos = FindGround(spawnPos);
            finalPos.y = finalPos.y + 30;

            return finalPos;
        }
        private void MoveToArena(BaseEntity entity, Vector3 targetPos)
        {
            Vector3 spawnPos = FindSpawnPosition(targetPos);
            entity.transform.position = spawnPos;
        }

        private float RandomRange(float distance) => UnityEngine.Random.Range(distance - 50, distance + 50);
        
        private void CheckDistance(BaseEntity entity)            
        {
            if (entity == null) return;
            var currentPos = entity.transform.position;
            var targetPos = GetDestination();            
            if (targetPos != null)
            {
                if (Vector3.Distance(currentPos, targetPos) < (currentPos.y + HeliDistance))
                {
                    PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
                    heliAI.State_Orbit_Enter(50);
                    heliAI.maxSpeed = HeliSpeed;                    
                }
                else
                    entity.GetComponent<PatrolHelicopterAI>().State_Move_Enter(targetPos + new Vector3(0.0f, 10f, 0.0f));
                
                GameTimers.Add(timer.Once(7, () => CheckDistance(entity)));
            }
        }
        private void DestroyHelicopters()
        {
            foreach (var heli in CSHelicopters)
            {                
                DestroyFires(heli.helicopter.transform.position);
                heli.helicopter.DieInstantly();
                UnityEngine.Object.Destroy(heli);                
            }
            CSHelicopters.Clear();
        }
        private void DestroyFires(Vector3 pos)
        {
            timer.Once(5, () =>
            {
                var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var gobject in allobjects)
                {
                    if (gobject.name.ToLower().Contains("fireball"))
                    {
                        var fire = gobject.ToBaseEntity();
                        if (Vector3.Distance(fire.transform.position, pos) < 200)
                            fire.KillMessage();
                    }
                }
            });
        }
        private void DestroyTimers()
        {
            foreach (var time in GameTimers)
                time.Destroy();
            GameTimers.Clear();
        }
        private void DestroyPlayers()
        {
            foreach (var entry in CSPlayers)
            {
                DestroyUI(entry.player);
                UnityEngine.Object.Destroy(entry);
            }
            CSPlayers.Clear();
        }
        private void DestroyGame()
        {
            Active = false;
            isCurrent = false;
            DestroyTimers();
            DestroyHelicopters();
            DestroyPlayers();            
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
                    EventManager.Call("SelectSpawnfile", new object[] { EventSpawnFile });
            }
            else
                isCurrent = false;
        }
        void OnEventPlayerSpawn(BasePlayer player)
        {
            if (isCurrent && Active)
            {
                player.inventory.Strip();
                EventManager.Call("GivePlayerKit", new object[] { player, CurrentKit });
                player.health = StartHealth;
                AddUI(player);
            }
        }
        void OnPostZoneCreate(string name)
        {
            if (name == EventName)
                return;
        }        
        object OnEventOpenPost()
        {
            if (isCurrent)
                MessageAll(MSG("openBroad"), "", true);
            return null;
        }        
        object OnEventEndPre()
        {
            if (isCurrent)
                DestroyGame();
            return null;
        }  
        
        object OnEventStartPre()
        {
            if (isCurrent)            
                Active = true;            
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
                {
                    var emptyobject = new object[] { };
                    MessageAll(MSG("noPlayers"), "", true);
                    EventManager.Call("CloseEvent", emptyobject);
                    EventManager.Call("EndEvent", emptyobject);
                }            
            return null;
        }        
        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (isCurrent)
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
                    EventManager.Call("LeaveEvent", victim);                    
                }
            }
            return;
        }    
        object OnRequestZoneName()
        {
            if (isCurrent)            
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

        static float HeliBulletDamage = 3.0f;
        static float HeliHealth = 3200.0f;
        static float MainRotorHealth = 320.0f;
        static float TailRotorHealth = 180.0f;
        static float HeliSpeed = 24.0f;
        static float HeliAccuracy = 8.0f;
        static float HeliModifier = 1.15f;
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
                EventManager.Call("AddTokens", helisurvivalplayer.player.UserIDString, (SurvivalPoints * (WaveNumber / 2)));
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
            EventManager.Call("AddTokens", player.UserIDString, WinnerPoints);
            MessageAllPlayers(string.Format(MSG("eventWon"), player.displayName, player.GetComponent<CS_Player>().points), "", true);
            var emptobject = new object[] { };
            EventManager.Call("CloseEvent", emptobject);
            EventManager.Call("EndEvent", emptobject);
        }
        #endregion
    }
}

