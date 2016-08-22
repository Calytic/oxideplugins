// Requires: EventManager
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Rust;


namespace Oxide.Plugins
{
    [Info("ChopperSurvival", "k1lly0u", "0.2.42", ResourceId = 1590)]
    class ChopperSurvival : RustPlugin
    {        
        [PluginReference] EventManager EventManager;
        [PluginReference] Plugin Spawns;

        private bool isCurrent;
        private bool Active;
		
		private static ChopperSurvival chopperSurvival;
        private static ConfigData config;
        private FieldInfo _spawnTime = typeof(PatrolHelicopterAI).GetField("spawnTime", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

        private float adjHeliHealth;
        private float adjHeliBulletDamage;
        private float adjMainRotorHealth;
        private float adjEngineHealth;
        private float adjTailRotorHealth;
        private float adjHeliAccuracy;

        private int WaveNumber;

        private string EventSpawnFile;
        private string MainColor;
        private string MSGColor;        

        private List<CS_Player> CSPlayers = new List<CS_Player>();
        private List<Timer> GameTimers = new List<Timer>();
        private List<CS_Helicopter> CSHelicopters = new List<CS_Helicopter>();

        public string CurrentKit;
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
            public float MaxMain;
            public float EngineHealth;
            public float MaxEngine;
            public float TailHealth;
            public float MaxTail;
            public float BodyHealth;
            public float MaxBody;
            public int UIHealth;

            void Awake()
            {
                Helicopter = GetComponent<BaseHelicopter>();                
                AI = Helicopter.GetComponent<PatrolHelicopterAI>();
                Helicopter.maxCratesToSpawn = 0;
                enabled = true;         
				chopperSurvival.timer.Once(config.HelicopterSettings.CheckDistanceTimer , () => {
				if (this != null)
					CheckDistance(GetComponent<BaseEntity>());
				});
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
                                    pointValue = config.Scoring.HeliHitPoints;
                                    break;
                                case "tail_rotor_col":
                                    hitWeakSpot = true;
                                    TailHealth -= info.damageTypes.Total();
                                    pointValue = config.Scoring.RotorHitPoints;
                                    if (TailHealth < 50)
                                        Helicopter.weakspots[i].WeakspotDestroyed();
                                    break;
                                case "main_rotor_col":
                                    hitWeakSpot = true;
                                    MainHealth -= info.damageTypes.Total();
                                    pointValue = config.Scoring.RotorHitPoints;
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
                AI.State_Move_Enter(Destination + new Vector3(0.0f, config.HelicopterSettings.DestinationHeightAdjust, 0.0f));
            }
            public void SetStats(int health, float main, float engine, float tail, float damage)
            {
                MainHealth = main;
                MaxMain = main;
                TailHealth = tail;
                MaxTail = tail;
                EngineHealth = engine;
                MaxEngine = engine;
                BodyHealth = health;
                MaxBody = health;
                Helicopter.bulletDamage = damage;
            }
            
			private void CheckDistance(BaseEntity entity)
            {
                if (entity == null) return;
                var currentPos = entity.transform.position;
                if (Destination != null)
                {
                    AI.SetTargetDestination(Destination + new Vector3(0.0f, config.HelicopterSettings.DestinationHeightAdjust, 0.0f));
                    if (Vector3Ex.Distance2D(currentPos, Destination) < 60)
                    {
                        if (config.HelicopterSettings.UseRockets)
                        {
                            int num = UnityEngine.Random.Range(1, 3);
                            if (num == 2)
                                AI.State_Strafe_Think(0);                            
                        }
                        else AI.State_Orbit_Think(40f);
                    }
                    else
                        AI.State_Move_Enter(Destination + new Vector3(0.0f, config.HelicopterSettings.DestinationHeightAdjust, 0.0f));
                                    
                }
				chopperSurvival.timer.Once(config.HelicopterSettings.CheckDistanceTimer , () => CheckDistance(entity));
            }
        }		
        class LeaderBoard
        {
            public string Name;
            public int Kills;
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
        private CuiElementContainer CreateHealthIndicator(CS_Helicopter heli)
        {            
            var panelName = $"csHeli_{heli.UIHealth}";
            var pos = CalcHealthPos(heli.UIHealth);
            var element = EventManager.UI.CreateElementContainer(panelName, "0.1 0.1 0.1 0.7", $"{pos[0]} {pos[1]}", $"{pos[2]} {pos[3]}", false);

            CreateHealthElement(ref element, panelName, "Body Health", heli.MaxBody, heli.BodyHealth, 0.75f);
            CreateHealthElement(ref element, panelName, "Main Rotor", heli.MaxMain, heli.MainHealth, 0.5f);
            CreateHealthElement(ref element, panelName, "Tail Rotor", heli.MaxTail, heli.TailHealth, 0.25f);
            CreateHealthElement(ref element, panelName, "Engine Health", heli.MaxEngine, heli.EngineHealth, 0f);
                       
            return element;
        }
        private void CreateHealthElement(ref CuiElementContainer element, string panelName, string name, float maxHealth, float currentHealth, float minY)
        {
            var percent = System.Convert.ToDouble((float)currentHealth / (float)maxHealth);
            var yMax = 0.98 * percent;
            string color = "0.2 0.6 0.2 0.9";
            if (percent <= 0.5)
                color = "1 0.5 0 0.9";
            if (percent <= 0.15)
                color = "0.698 0.13 0.13 0.9";
            EventManager.UI.CreatePanel(ref element, panelName, color, $"0.01 {minY + 0.005}", $"{yMax} {minY + 0.24}");
            EventManager.UI.CreateLabel(ref element, panelName, "", name, 8, $"0 {minY}", $"1 {minY + 0.25}");
        }
        private void DestroyHealthUI(int number)
        {
            foreach (var entry in CSPlayers)
                CuiHelper.DestroyUi(entry.player, $"csHeli_{number}");
        }
        private void DestroyAllHealthUI(BasePlayer player)
        {
            for (int i = 0; i < config.EventSettings.MaximumHelicopters; i++)
                CuiHelper.DestroyUi(player, $"csHeli_{i}");
        }
        private void RefreshUI()
        {
            foreach (var entry in CSPlayers)
            {                
                AddUI(entry.player);
            }
        }
        private void RefreshHealthUI(CS_Helicopter heli)
        {
            if (!heli) return;
            if (config.EventSettings.ShowHeliHealthUI)
            {
                foreach (var entry in CSPlayers)
                {
                    CuiHelper.DestroyUi(entry.player, $"csHeli_{heli.UIHealth}");
                    CuiHelper.AddUi(entry.player, CreateHealthIndicator(heli));
                }
            }
        }
        private void RefreshPlayerHealthUI(BasePlayer player)
        {
            if (config.EventSettings.ShowHeliHealthUI)
            {
                foreach (var heli in CSHelicopters)
                {
                    CuiHelper.DestroyUi(player, $"csHeli_{heli.UIHealth}");
                    CuiHelper.AddUi(player, CreateHealthIndicator(heli));
                }
            }
        }
        private void AddUI(BasePlayer player) => CuiHelper.AddUi(player, CreateScoreboard(player));
        private void DestroyUI(BasePlayer player) => CuiHelper.DestroyUi(player, "CSScoreBoard");
        private float[] CalcHealthPos(int number)
        {
            Vector2 position = new Vector2(0.1f, 0.86f);
            Vector2 dimensions = new Vector2(0.13f, 0.08f);
            float offsetY = 0;
            float offsetX = 0;
            if (number >= 0 && number < 6)
            {
                offsetX = (0.0033f + dimensions.x) * number;
            }
            if (number > 5 && number < 12)
            {
                offsetX = (0.0033f + dimensions.x) * (number - 6);
                offsetY = (-0.005f - dimensions.y) * 1;
            }
            if (number > 11 && number < 18)
            {
                offsetX = (0.0033f + dimensions.x) * (number - 12);
                offsetY = (-0.005f - dimensions.y) * 2;
            }            

            Vector2 offset = new Vector2(offsetX, offsetY);
            Vector2 posMin = position + offset;
            Vector2 posMax = posMin + dimensions;
            return new float[] { posMin.x, posMin.y, posMax.x, posMax.y };
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
            chopperSurvival = this;
            config = configData;
            EventSpawnFile = config.EventSettings.DefaultSpawnfile;
            MainColor = config.Messaging.MainColor;
            MSGColor = config.Messaging.MSGColor;
            RegisterMessages();
            RegisterGame();
        }    
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) { DestroyUI(player); DestroyAllHealthUI(player); }
            if (isCurrent && Active)                                    
                EventManager.EndEvent(); 
				DestroyEvent();
        }        
		void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
			if (isCurrent && Active && entity != null)
            {
				if (entity.GetComponent<CS_Helicopter>() && hitInfo.Initiator != null && hitInfo.Initiator is BasePlayer && !hitInfo.Initiator.GetComponent<CS_Player>())
				{	
					hitInfo.damageTypes = new DamageTypeList();
					return;
				}
				if (hitInfo.Initiator != null && hitInfo.Initiator is BasePlayer && hitInfo.Initiator.GetComponent<CS_Player>())
				{
					var attacker = hitInfo.Initiator as BasePlayer;
					if (entity is BasePlayer)
					{
						if (entity.ToPlayer() == null || hitInfo == null) return;
						if (entity.ToPlayer().userID != hitInfo.Initiator.ToPlayer().userID)                            
							if (entity.GetComponent<CS_Player>())
							{
								hitInfo.damageTypes.ScaleAll(config.PlayerSettings.FFDamageScale);
								MessagePlayer(attacker, "", MSG("fFire"));
							}
					}
					if (entity.GetComponent<CS_Helicopter>())
					{
						int points = entity.GetComponent<CS_Helicopter>().DealDamage(hitInfo);
                        RefreshHealthUI(entity.GetComponent<CS_Helicopter>());
						hitInfo.damageTypes = new DamageTypeList();
						hitInfo.HitMaterial = 0;
						hitInfo.PointStart = Vector3.zero;
						attacker.GetComponent<CS_Player>().points += points;
					}
				}
			}
        } 		
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (isCurrent && Active)
            {
                if (entity is BaseEntity)
                {
                    var entityName = entity.ShortPrefabName;

					
                    if (entityName.Contains("napalm"))
                        if (!config.HelicopterSettings.UseRockets)
                            KillEntity(entity as BaseEntity);

                    if (entityName.Contains("servergibs_patrolhelicopter"))
                        entity.KillMessage();
                }
            }
        }
        object CanBeTargeted(BaseCombatEntity target, MonoBehaviour turret)
        {
            if (isCurrent && Active && target is BasePlayer && turret is HelicopterTurret)
            {
                if ((turret as HelicopterTurret)._heliAI && (turret as HelicopterTurret)._heliAI.GetComponent<CS_Helicopter>())
                {
                    if ((target as BasePlayer).GetComponent<CS_Player>())
                        return null;
                    else
                        return false;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }        
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            if (isCurrent && Active)            
                if (entity.GetComponent<BaseHelicopter>())                
                    if (entity.GetComponent<CS_Helicopter>())
                    {
                        DestroyHealthUI(entity.GetComponent<CS_Helicopter>().UIHealth);
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
                MessageAllPlayers(string.Format(MSG("firstWave"), config.HelicopterSettings.SpawnBeginTimer), "", true);
                SetPlayers();
                GameTimers.Add(timer.Once(config.HelicopterSettings.SpawnBeginTimer, () => SpawnWave()));
                
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
                    if (WaveNumber > config.EventSettings.MaximumWaves)
                    {
                        FindWinner();
                        return;
                    }
                }               
                SetPlayers();
                MessageAllPlayers(string.Format(MSG("nextWave"), config.HelicopterSettings.SpawnWaveTimer), "", true);
                GameTimers.Add(timer.Once(config.HelicopterSettings.SpawnWaveTimer, () => SpawnWave()));
            }
        }
        private void SetPlayers()
        {
            foreach (CS_Player hs in CSPlayers)
            {
                EventManager.GivePlayerKit(hs.player, CurrentKit);
                hs.player.health = config.PlayerSettings.StartHealth;
            }
        }
        private void SpawnWave()
        {
            if (isCurrent && Active)
            {
                var num = System.Math.Ceiling(((float)WaveNumber / (float)config.EventSettings.MaximumWaves) * (float)config.EventSettings.MaximumHelicopters);
                if (num < 1) num = 1;
                if (WaveNumber == 1) InitStatModifiers();
                else SetStatModifiers();
                SpawnHelicopter((int)num);
                SetHelicopterStats();
                if (config.EventSettings.ShowHeliHealthUI)
                {
                    foreach(var heli in CSHelicopters)
                    {
                        RefreshHealthUI(heli);
                    }
                }
                MessageAllPlayers("", string.Format(MSG("waveInbound"), WaveNumber));
            }
        }
		private void SpawnHelicopter(int num)
        {
            bool lifetime = false;
			if (ConVar.PatrolHelicopter.lifetimeMinutes == 0)
			{
				ConVar.PatrolHelicopter.lifetimeMinutes = 1;
				lifetime = true;
			}
			
			for (int i = 0; i < num; i++)
            {
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                if (entity)
                {
                    BaseHelicopter heli = entity.GetComponent<BaseHelicopter>();
					entity.Invoke("CS_Helicopter", 3600f);
					entity.Spawn();
                    var csHeli = heli.gameObject.AddComponent<CS_Helicopter>();
                    CSHelicopters.Add(csHeli);
                    csHeli.UIHealth = CSHelicopters.Count - 1;
                    
                    heli.GetComponent<CS_Helicopter>().enabled = true;
                    MoveToArena(entity);
					_spawnTime.SetValue(entity.GetComponent<PatrolHelicopterAI>(), UnityEngine.Time.realtimeSinceStartup* 10);					
                }
            }			
			if(lifetime)
				timer.Once(5f, () => ConVar.PatrolHelicopter.lifetimeMinutes = 0);
        }

        private void InitStatModifiers()
        {
            adjHeliBulletDamage = config.HelicopterSettings.HeliBulletDamage;
            adjHeliHealth = config.HelicopterSettings.HeliHealth;
            adjMainRotorHealth = config.HelicopterSettings.MainRotorHealth;
            adjEngineHealth = config.HelicopterSettings.EngineHealth;
            adjTailRotorHealth = config.HelicopterSettings.TailRotorHealth;
            adjHeliAccuracy = config.HelicopterSettings.HeliAccuracy;
            if (config.EventSettings.ShowStatsInConsole) ShowHeliStats();
        }
        private void SetStatModifiers()
        {
            if (isCurrent)
            {
                var HeliModifier = config.HelicopterSettings.HeliModifier;
                adjHeliBulletDamage *= HeliModifier;
                adjHeliHealth *= HeliModifier;
                adjMainRotorHealth *= HeliModifier;
                adjEngineHealth *= HeliModifier;
                adjTailRotorHealth = adjTailRotorHealth * HeliModifier;
                adjHeliAccuracy = adjHeliAccuracy - (HeliModifier / 1.5f);
                if (config.EventSettings.ShowStatsInConsole) ShowHeliStats();
            }
        }
        private void ShowHeliStats()
        {
            Puts("---- CS Heli Stats ----");
            Puts("Modifier: " + config.HelicopterSettings.HeliModifier);
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
            x = config.HelicopterSettings.SpawnDistance * Mathf.Cos(angleRadians);
            y = config.HelicopterSettings.SpawnDistance * Mathf.Sin(angleRadians);
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
					if(heli.Helicopter != null)
						if(heli.Helicopter.transform != null)
							DestroyFires(heli.Helicopter.transform.position);
						if(heli.Helicopter != null)
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
                if(pos == null) return;
				var allobjects = Physics.OverlapSphere(pos, 150);
				foreach (var gobject in allobjects)
                {                   
                    if (gobject.name.ToLower().Contains("oilfireballsmall") || gobject.name.ToLower().Contains("napalm"))
                    {                       
                        var fire = gobject.GetComponent<BaseEntity>();                        
                        KillEntity(fire);
                        UnityEngine.Object.Destroy(gobject);                       
                    }
                }
            });
        }
        void KillEntity(BaseEntity entity)
        {
            if (BaseEntity.saveList.Contains(entity))
                BaseEntity.saveList.Remove(entity);
            entity.KillMessage();
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
                    DestroyAllHealthUI(player.player);
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
        void RegisterGame()
        {
            var success = EventManager.RegisterEventGame(Title);//, config.EventSettings.DefaultSpawnfile, config.EventSettings.DefaultKit, config.EventSettings.DefaultZoneID, true, true, true, 2, 0, false, true, true, false);
            if (success == null)
                Puts(MSG("noEvent"));
        }
        void OnSelectEventGamePost(string name)
        {
            if (Title == name)
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
                //PATCH >>> some more player options
				player.metabolism.hydration.value = config.PlayerSettings.StartHydration;
				player.metabolism.calories.value = config.PlayerSettings.StartCalories;
				player.InitializeHealth(config.PlayerSettings.StartHealth, config.PlayerSettings.StartHealth);
				timer.Once(3, ()=> { AddUI(player); RefreshPlayerHealthUI(player); });
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
            {
					DestroyTimers();
					FindWinner();
					DestroyEvent();
            }           
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
					DestroyUI(player);
                    DestroyAllHealthUI(player);
                }            
            if (isCurrent && Active)            
                if (CSPlayers.Count == 0)
                    EventManager.EndEvent();                           
            return null;
        }        
        void OnEventPlayerDeath(BasePlayer victim, HitInfo hitinfo)
        {
            if (Active)
            {
                DestroyUI(victim);
                DestroyAllHealthUI(victim);
                victim.GetComponent<CS_Player>().deaths++;
                int LivesLeft = (config.PlayerSettings.DeathLimit - victim.GetComponent<CS_Player>().deaths);

                MessageAll(string.Format(MSG("eventDeath"), victim.displayName, victim.GetComponent<CS_Player>().deaths, config.PlayerSettings.DeathLimit), "", true);               
                MessagePlayer(victim, string.Format(MSG("livesLeft"), LivesLeft), "", true);

                if (victim.GetComponent<CS_Player>().deaths >= config.PlayerSettings.DeathLimit)
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
                if (!string.IsNullOrEmpty(config.EventSettings.DefaultZoneID))          
                    return config.EventSettings.DefaultZoneID;            
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
            {"eventWon", "{0} has won the event with {1} points!"},
            {"eventDeath", "{0} has died {1}/{2} times!"},
            {"waveInbound", "Wave {0} inbound!"},
            {"firstWave", "You have {0} seconds to prepare for the first wave!"},
            {"heliDest", "Helicopter Destroyed!"},
            {"livesLeft", "You have {0} lives remaining!"},
            {"notEnough", "Not enough players to start the event"}
        },this);
        #endregion

        #region Config        
        private ConfigData configData;
        class EventSettings
        {
            public string DefaultKit { get; set; }
            public string DefaultSpawnfile { get; set; }
            public string DefaultZoneID { get; set; }
            public int MaximumWaves { get; set; }
            public int MaximumHelicopters { get; set; }
            public bool ShowStatsInConsole { get; set; }
            public bool ShowHeliHealthUI { get; set; }
        }
        class PlayerSettings
        {
            public float StartHealth { get; set; }
            public float StartHydration { get; set; }
            public float StartCalories { get; set; }  
            public int DeathLimit { get; set; }  
            public float FFDamageScale { get; set; }    
        }
        class HeliSettings
        {
            public float HeliBulletDamage { get; set; }
            public float HeliHealth { get; set; }
            public float MainRotorHealth { get; set; }
            public float TailRotorHealth { get; set; }
            public float EngineHealth { get; set; }
            public float HeliSpeed { get; set; }
            public float HeliAccuracy { get; set; }
            public float HeliModifier { get; set; }
            public float SpawnDistance { get; set; }
            public float CheckDistanceTimer { get; set; }
            public float DestinationHeightAdjust { get; set; }
            public float SpawnWaveTimer { get; set; }
            public float SpawnBeginTimer { get; set; }
            
            public bool UseRockets { get; set; }
        }
        class Messaging
        {
            public string MainColor { get; set; }
            public string MSGColor { get; set; }
        }
        class ConfigData
        {
            public EventSettings EventSettings { get; set; }
            public HeliSettings HelicopterSettings { get; set; }
            public PlayerSettings PlayerSettings { get; set; }
            public Messaging Messaging { get; set; }
            public Scoring Scoring { get; set; }
        }
        class Scoring
        {
            public int RotorHitPoints { get; set; }
            public int HeliHitPoints { get; set; }
            public int SurvivalTokens { get; set; }
            public int WinnerTokens { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                EventSettings = new EventSettings
                {                    
                    DefaultKit = "cskit",
                    DefaultSpawnfile = "csspawns",
                    DefaultZoneID = "cszone",
                    MaximumHelicopters = 4,
                    MaximumWaves = 10,
                    ShowHeliHealthUI = true,
                    ShowStatsInConsole = true
                },
                HelicopterSettings = new HeliSettings
                {
                    CheckDistanceTimer = 10f,
                    DestinationHeightAdjust = 10f,
                    EngineHealth = 800f,
                    HeliAccuracy = 8f,
                    HeliBulletDamage = 4f,
                    HeliHealth = 3800f,
                    HeliModifier = 1.22f,
                    HeliSpeed = 24f,
                    MainRotorHealth = 420f,
                    SpawnBeginTimer = 20f,
                    SpawnDistance = 500f,
                    SpawnWaveTimer = 10f,
                    TailRotorHealth = 300f,
                    UseRockets = true
                },
                Messaging = new Messaging
                {                    
                    MainColor = "<color=#FF8C00>",
                    MSGColor = "<color=#939393>"
                },
                PlayerSettings = new PlayerSettings
                {                    
                    DeathLimit = 10,
                    FFDamageScale = 0,
                    StartCalories = 500f,
                    StartHealth = 100f,
                    StartHydration = 250f
                },
                Scoring = new Scoring
                {
                    HeliHitPoints = 1,
                    RotorHitPoints = 3,
                    SurvivalTokens = 1,
                    WinnerTokens = 10
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
       
        #region Scoring
        void AddPoints()
        {
            foreach (CS_Player helisurvivalplayer in CSPlayers)            
                EventManager.AddTokens(helisurvivalplayer.player.UserIDString, config.Scoring.SurvivalTokens);
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
            EventManager.AddTokens(player.UserIDString, config.Scoring.WinnerTokens);
            MessageAllPlayers(string.Format(MSG("eventWon"), player.displayName, player.GetComponent<CS_Player>().points), "", true);
            var emptobject = new object[] { };
            EventManager.CloseEvent();
            EventManager.EndEvent();
        }
        #endregion
    }
}

