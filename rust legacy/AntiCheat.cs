// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("AntiCheat", "Reneb", "2.0.13")]
    class AntiCheat : RustLegacyPlugin
    {
        object OnDeny()
        {
            return false;
        }
        /////////////////////////
        // FIELDS
        /////////////////////////
         
        static Hash<PlayerClient, float> autoLoot = new Hash<PlayerClient, float>();
        
        static Hash<PlayerClient, float> wallhackLogs = new Hash<PlayerClient, float>();

        public static Core.Configuration.DynamicConfigFile ACData;
        private static FieldInfo getblueprints;
        private static FieldInfo getlooters;
        public static Vector3 Vector3Down = new Vector3(0f,-1f,0f);
        public static Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        public static Vector3 UnderPlayerAdjustement = new Vector3(0f, -1.15f, 0f);
        public static float distanceDown = 10f;
        public static int groundsLayer = LayerMask.GetMask(new string[] { LayerMask.LayerToName(10), "Terrain" });
        public static ItemDataBlock wooddata;
         
        public static Vector3 Vector3ABitLeft = new Vector3(-0.03f, 0f, -0.03f);
        public static Vector3 Vector3ABitRight = new Vector3(0.03f, 0f, 0.03f);
        public static Vector3 Vector3NoChange = new Vector3(0f, 0f, 0f);

        /////////////////////////
        // CACHED FIELDS
        /////////////////////////
        public static RaycastHit cachedRaycast;
        public static RaycastHit2 cachedRaycast2;
        public static PlayerClient cachedPlayer;
        public static string cachedModelname;
        public static string cachedObjectname;
        public static float cachedDistance;
        public static Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
        public static Collider cachedCollider;
        public static bool cachedBoolean;
        public static Vector3 cachedvector3;
        public static WeaponImpact cachedWeapon;
        public static BulletWeaponImpact cachedBulletWeapon;
        public static OverKillHandler cachedOverkill;
        public static int cachedInt;
        /////////////////////////
        // Config Management
        /////////////////////////
        public static bool permanent = true;
        public static float timetocheck = 3600f;
        public static bool punishByBan = true;
        public static bool punishByKick = true;
        public static bool broadcastAdmins = true;
        public static bool broadcastPlayers = true;

        public static bool antiSpeedHack = true;
        public static float speedMinDistance = 11f;
        public static float speedMaxDistance = 25f;
        public static float speedDropIgnore = 8f;
        public static float speedDetectionForPunish = 3;
        public static bool speedPunish = true;

        public static bool antiWalkSpeedhack = true;
        public static float walkspeedMinDistance = 6f;
        public static float walkspeedMaxDistance = 15f;
        public static float walkspeedDropIgnore = 8f;
        public static float walkspeedDetectionForPunish = 3;
        public static bool walkspeedPunish = true;
         
        public static bool antiSuperJump = true;
        public static float jumpMinHeight = 5f;
        public static float jumpMaxDistance = 25f;
        public static float jumpDetectionsNeed = 2f;
        public static float jumpDetectionsReset = 300f;
        public static bool jumpPunish;

        public static bool antiBlueprintUnlocker = true;
        public static bool blueprintunlockerPunish = true;

        public static bool antiAutoloot = true;
        public static bool autolootPunish = true;

        public static bool antiSleepingBagHack = true;
        public static bool sleepingbaghackPunish = true;

        public static bool antiOverKill = true;
        public static bool overkillPunish = true;
        public static Dictionary<string,object> overkillDictionary = GetWeaponsMaxDistance();
        public static float overkillResetTimer = 600f;
        public static float overkillDetectionForPunish = 2f;

        public static bool antiMassRadiation = true;

		public static bool antiNoRecoil = true;
		public static float norecoilDistance = 40f;
        public static bool norecoilPunish = true;
		public static int norecoilPunishMinKills = 5;
		public static int norecoilPunishMinRatio = 33; 

        public static bool antiWallhack = true;
        public static bool wallhackPunish = true;

        public static bool antiAutoGather = true;
        public static bool autogatherPunish = true;

        public static bool antiCeilingHack = true;
        public static bool ceilinghackPunish = true;

        public static bool antiFlyhack = true;
        public static float flyhackMaxDropSpeed = 5f;
        public static float flyhackDetectionsForPunish = 3;
        public static bool flyhackPunish = true;
          
        public static string playerHackDetectionBroadcast = "[color #FFD630] {0} [color red]tried to cheat on this server!";
        public static string noAccess = "AntiCheat: You dont have access to this command";
        public static string noPlayerFound = "AntiCheat: No player found with this name or steamid";
        public static string checkingPlayer = "AntiCheat: {0} is now being checked";
        public static string checkingAllPlayers = "AntiCheat: Now checking all players";
        public static string DataReset = "AntiCheat: Data was resetted, all players are now being checked again";
        void LoadDefaultConfig() { }

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
		
        static Dictionary<string,object> GetWeaponsMaxDistance()
        {
            var newdict = new Dictionary<string, object>();
            newdict.Add("9mm Pistol",85f);
            newdict.Add("Hunting Bow",255f);
            newdict.Add("Pipe Shotgun",85f);
            newdict.Add("HandCannon",25f);
            newdict.Add("Revolver",85f);
            newdict.Add("Hatchet",10f);
            newdict.Add("Stone Hatchet", 10f);
            newdict.Add("Rock",10f);
            newdict.Add("M4",145f);
            newdict.Add("MP5A4",85f);
            newdict.Add("P250",125f);
            newdict.Add("Shotgun",35f);
            newdict.Add("Bolt Action Rifle",255f);
            newdict.Add("Pick Axe", 10f);
            return newdict; 
        }

        void Init()
        {
            CheckCfg<bool>("Settings: Permanent Check", ref permanent);
            CheckCfg<bool>("Settings: Broadcast Detections to Admins", ref broadcastAdmins);
            CheckCfg<bool>("Settings: Broadcast Bans to Players", ref broadcastPlayers);
            CheckCfgFloat("Settings: Check Time (seconds)", ref timetocheck);
            CheckCfg<bool>("Settings: Punish by Ban", ref punishByBan);
            CheckCfg<bool>("Settings: Punish by Kick", ref punishByKick);
            CheckCfg<bool>("SpeedHack: activated", ref antiSpeedHack);
            CheckCfgFloat("SpeedHack: Minimum Speed (m/s)", ref speedMinDistance);
            CheckCfgFloat("SpeedHack: Maximum Speed (m/s)", ref speedMaxDistance);
            CheckCfgFloat("SpeedHack: Max Height difference allowed (m/s)", ref speedDropIgnore);
            CheckCfgFloat("SpeedHack: Detections needed in a row before Punishment", ref speedDetectionForPunish);
            CheckCfg<bool>("SpeedHack: Punish", ref speedPunish);
            CheckCfg<bool>("WalkSpeedHack: activated", ref antiWalkSpeedhack);
            CheckCfgFloat("WalkSpeedHack: Minimum Speed (m/s)", ref walkspeedMinDistance);
            CheckCfgFloat("WalkSpeedHack: Maximum Speed (m/s)", ref walkspeedMaxDistance);
            CheckCfgFloat("WalkSpeedHack: Max Height difference allowed (m/s)", ref walkspeedDropIgnore);
            CheckCfgFloat("WalkSpeedHack: Detections needed in a row before Punishment", ref walkspeedDetectionForPunish);
            CheckCfg<bool>("WalkSpeedHack: Punish", ref walkspeedPunish);
            CheckCfg<bool>("SuperJump: activated", ref antiSuperJump);
            CheckCfgFloat("SuperJump: Minimum Height (m/s)", ref jumpMinHeight);
            CheckCfgFloat("SuperJump: Maximum Distance before ignore (m/s)", ref jumpMaxDistance);
            CheckCfgFloat("SuperJump: Detections needed before punishment", ref jumpDetectionsNeed);
            CheckCfgFloat("SuperJump: Time before the superjump detections gets reseted", ref jumpDetectionsReset);
            CheckCfg<bool>("SuperJump: Punish", ref jumpPunish);
            CheckCfg<bool>("FlyHack: activated", ref antiFlyhack);
            CheckCfgFloat("FlyHack: Max Drop Speed before ignoring (m/s)", ref flyhackMaxDropSpeed);
            CheckCfgFloat("FlyHack: Detections needed before punishment", ref flyhackDetectionsForPunish);
            CheckCfg<bool>("FlyHack: Punish", ref flyhackPunish);
            CheckCfg<bool>("BlueprintUnlocker: activated", ref antiBlueprintUnlocker);
            CheckCfg<bool>("BlueprintUnlocker: Punish", ref blueprintunlockerPunish);
            CheckCfg<bool>("Autoloot: activated", ref antiAutoloot);
            CheckCfg<bool>("Autoloot: Punish", ref autolootPunish); 
            CheckCfg<bool>("AntiMassRadiation: activated", ref antiMassRadiation);
            CheckCfg<bool>("OverKill: activated", ref antiOverKill);
            CheckCfg<bool>("OverKill: Punish ", ref overkillPunish);
            CheckCfg<Dictionary<string,object>>("OverKill: Max Distances", ref overkillDictionary);
            CheckCfgFloat("OverKill: Reset Timer ", ref overkillResetTimer);
            CheckCfgFloat("OverKill: Detections before punish", ref overkillDetectionForPunish);
            CheckCfg<bool>("Wallhack: activated", ref antiWallhack);
            CheckCfg<bool>("Wallhack: Punish ", ref wallhackPunish);
            CheckCfg<bool>("CeilingHack: activated", ref antiCeilingHack);
            CheckCfg<bool>("CeilingHack: Punish ", ref ceilinghackPunish);
            CheckCfg<bool>("Sleeping Bag Hack: activated", ref antiSleepingBagHack);
            CheckCfg<bool>("Sleeping Bag Hack: Punish ", ref sleepingbaghackPunish);
            CheckCfg<bool>("NoRecoil: activated", ref antiNoRecoil);
            CheckCfg<bool>("NoRecoil: Punish ", ref norecoilPunish);
            CheckCfgFloat("NoRecoil: Min Distance For Check ", ref norecoilDistance);
            CheckCfg<int>("NoRecoil: Punish Min Kills", ref norecoilPunishMinKills);
            CheckCfg<int>("NoRecoil: Punish Min Ratio in %", ref norecoilPunishMinRatio);

            CheckCfg<bool>("AutoGather: activated", ref antiAutoGather);
            CheckCfg<bool>("AutoGather: Punish ", ref autogatherPunish);

            CheckCfg<string>("Messages: No Access", ref noAccess);
            CheckCfg<string>("Messages: No player found", ref noPlayerFound);
            CheckCfg<string>("Messages: Player being checked", ref checkingPlayer);
            CheckCfg<string>("Messages: All players being checked", ref checkingAllPlayers);
            CheckCfg<string>("Messages: Data Reseted", ref DataReset);
            CheckCfg<string>("Messages: Broadcast Message to Player on Hacker Punishement", ref playerHackDetectionBroadcast);
            SaveConfig();
        } 


        /////////////////////////
        // PlayerHandler
        // Handles the player checks
        /////////////////////////

        public class PlayerHandler : MonoBehaviour
        {
            public float timeleft;
            public float lastTick;
            public float currentTick;
            public float deltaTime;
            public Vector3 lastPosition;
            public PlayerClient playerclient;
            public Character character;
            public Inventory inventory;
            public string userid;
            public float distance3D;
            public float distanceHeight;

            public float currentFloorHeight;
            public bool hasSearchedForFloor = false;

            public float lastSpeed = Time.realtimeSinceStartup;
            public int speednum = 0;


            public float lastWalkSpeed = Time.realtimeSinceStartup;
            public int walkspeednum = 0;
            public bool lastSprint = false;

            public float lastJump = Time.realtimeSinceStartup;
            public int jumpnum = 0;


            public float lastFly = Time.realtimeSinceStartup;
            public int flynum = 0;

			public int noRecoilDetections = 0;
			public int noRecoilKills = 0;

            public float lastWoodCount = 0;

            void Awake()
            {
                lastTick = Time.realtimeSinceStartup;
                enabled = false;
            }
			public void StartCheck()
            {
                this.playerclient = GetComponent<PlayerClient>();
                this.userid = this.playerclient.userID.ToString();
                if (playerclient.controllable == null) return;
                this.character = playerclient.controllable.GetComponent<Character>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
            }
            void FixedUpdate()
            {
                if (Time.realtimeSinceStartup - lastTick >= 1)
                {
                    currentTick = Time.realtimeSinceStartup;
                    deltaTime = currentTick - lastTick;
                    distance3D = Vector3.Distance(playerclient.lastKnownPosition, lastPosition) / deltaTime;
                    distanceHeight = (playerclient.lastKnownPosition.y - lastPosition.y) / deltaTime;
                    checkPlayer(this);
                    lastPosition = playerclient.lastKnownPosition;
                    lastTick = currentTick;
                    if (!permanent)
                    {
                        if (this.timeleft <= 0f) EndDetection(this);
                        this.timeleft--;
                    }
                    this.hasSearchedForFloor = false;
                }
            }
            public Inventory GetInventory()
            {
                if (this.inventory == null) this.inventory = playerclient.rootControllable.idMain.GetComponent<Inventory>();
                return this.inventory;
            }
            public Character GetCharacter()
            {
                if(this.character == null) this.character = playerclient.rootControllable.idMain.GetComponent<Character>();
                return this.character;
            }
            void OnDestroy()
            {
               ACData[this.userid] = this.timeleft.ToString();
            }
        }

        /////////////////////////
        // CeilingHackHandler
        // Handles the ceiling hack checks, it should be much better then the old 1.18 version
        /////////////////////////
        public class CeilingHackHandler : MonoBehaviour
        {
            public Vector3 lastPosition;
            public PlayerClient playerclient;
            public float lastTick;
            public Vector3 cachedceiling;
            public bool checkingNewPos;

            void Awake()
            {
                this.lastTick = Time.realtimeSinceStartup;
                this.checkingNewPos = false;
                this.playerclient = GetComponent<PlayerClient>();
                this.lastPosition = this.playerclient.lastKnownPosition;
                enabled = true;
            }

            void FixedUpdate()
            {
                lastPosition = this.playerclient.lastKnownPosition;
                if (!checkingNewPos)
                {
                    this.lastTick = Time.realtimeSinceStartup;
                    if (lastPosition == default(Vector3)) return;
                    if (!MeshBatchPhysics.Raycast(lastPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) { DestroyCeilingHandler(this); return; }
                    if (cachedhitInstance == null) { DestroyCeilingHandler(this); return; }
                    if (!cachedhitInstance.graphicalModel.ToString().Contains("ceiling")) { DestroyCeilingHandler(this); return; }
                    cachedceiling = cachedRaycast.point;
                    checkingNewPos = true;
                }
                else
                {
                    if (Time.realtimeSinceStartup - this.lastTick < 1f) return;
                    if (MeshBatchPhysics.Raycast(lastPosition, Vector3Up, out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                    {
                        cachedvector3 = cachedceiling - cachedRaycast.point;
                        if (cachedvector3.y > 0.6f)
                        {
                            cachedvector3 = cachedceiling - lastPosition;
                            if (cachedvector3.y > 1.5f && Math.Abs(cachedvector3.x) < 0.1f && Math.Abs(cachedvector3.z) < 0.1f)
                            {
                                Debug.Log(string.Format("{0} {1} - rCeilingHack ({2}) @ from {3} to {4}", playerclient.userID.ToString(), playerclient.userName.ToString(), cachedvector3.y.ToString(), cachedceiling.ToString(), lastPosition.ToString()));
                                AntiCheatBroadcastAdmins(string.Format("{0} {1} - rCeilingHack ({2}) @ from {3} to {4}", playerclient.userID.ToString(), playerclient.userName.ToString(), cachedvector3.y.ToString(), cachedceiling.ToString(), lastPosition.ToString()));
                                if (ceilinghackPunish) Punish(playerclient, string.Format("rCeilingHack ({0})", cachedvector3.y.ToString()));
                            }
                        }
                    }
                    DestroyCeilingHandler(this);
                }
            }
        }
        static void DestroyCeilingHandler(CeilingHackHandler ceilinghandler) { GameObject.Destroy(ceilinghandler); }
		
		public class NoRecoilHandler : MonoBehaviour
		{
			public int Kills = 0;
			public int NoRecoils = 0;
			public Character character;
			public PlayerClient playerClient;
			
			void Awake()
			{
				enabled = false;
				this.playerClient = GetComponent<PlayerClient>();
			}
			public Character GetCharacter()
			{
				if(this.character == null) this.playerClient.controllable.GetComponent<Character>();
				return this.character;
			}
		}

        public class OverKillHandler : MonoBehaviour
        {
            public float lastOverkill = Time.realtimeSinceStartup;
            public float number = 0f;
			
			void Awake()
			{
				enabled = false;
			}
        }
        /////////////////////////
        // Oxide Hooks
        /////////////////////////

        /////////////////////////
        //  Loaded()
        // Called when the plugin is loaded
        /////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("cananticheat", this);
        }
        /////////////////////////
        //  Loaded()
        // Called when the server was initialized (when people can start joining)
        /////////////////////////
        void OnServerInitialized()
        {
            ACData = Interface.GetMod().DataFileSystem.GetDatafile("AntiCheat");
            getblueprints = typeof(PlayerInventory).GetField("_boundBPs", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            getlooters = typeof(Inventory).GetField("_netListeners", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            wooddata = DatablockDictionary.GetByName("Wood");
            PlayerHandler phandler;
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (hasPermission(player.netUser)) continue;
                phandler = player.gameObject.AddComponent<PlayerHandler>();
                phandler.timeleft = GetPlayerData(player);
                phandler.StartCheck();
            }

        }
        /////////////////////////
        // OnServerSave()
        // Called when the server saves
        // Perfect to save data here!
        /////////////////////////
        void OnServerSave()
        {
            SaveData();
        }

        /////////////////////////
        // Unload()
        // Called when the plugin gets unloaded or reloaded
        /////////////////////////
        void Unload()
        {
            SaveData();
            var objects = GameObject.FindObjectsOfType(typeof(PlayerHandler));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }


        /////////////////////////
        // OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        // Called when an item was removed from a none player inventory
        /////////////////////////
        void OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            if (antiAutoloot && inventory.name == "SupplyCrate(Clone)") { CheckSupplyCrateLoot(inventory); return; }
            
        }
        
        /////////////////////////
        // OnItemCraft(CraftingInventory inventory, BlueprintDataBlock bp, int amount, ulong starttime)
        // Called when a player starts crafting an object
        /////////////////////////
        void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock bp, int amount, ulong starttime)
        {
            if (!antiBlueprintUnlocker) return;
            var inv = inventory.GetComponent<PlayerInventory>();
            var blueprints = (List<BlueprintDataBlock>)getblueprints.GetValue(inv);
            if (blueprints.Contains(bp)) return;
            if(blueprintunlockerPunish) Punish(inventory.GetComponent<Controllable>().playerClient, string.Format("rBlueprintUnlocker ({0})", bp.resultItem.name.ToString()));
        }

        /////////////////////////
        // ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        // Called when any damage was made
        /////////////////////////
        object ModifyDamage(TakeDamage takedamage, ref DamageEvent damage)
        {
            /*if (antiMassRadiation && (damage.damageTypes == 0 || damage.damageTypes == DamageTypeFlags.damage_radiation) )
            {
                if (takedamage.GetComponent<Controllable>() == null) return null;
                if (damage.victim.character == null) return null;
                if (float.IsInfinity(damage.amount)) return null;
                if (damage.amount > 12f) { AntiCheatBroadcastAdmins(string.Format("{0} is receiving too much damage from the radiation, ignoring the damage", takedamage.GetComponent<Controllable>().playerClient.userName.ToString())); damage.amount = 0f; return damage; }
            }
            else */
            if (antiWallhack)
            {
                if (damage.status != LifeStatus.WasKilled) return null;
                if (!(damage.extraData is BulletWeaponImpact)) return null;
                cachedBulletWeapon = damage.extraData as BulletWeaponImpact;
                if (!MeshBatchPhysics.Linecast(damage.attacker.character.eyesOrigin, cachedBulletWeapon.worldPoint, out cachedRaycast, out cachedBoolean, out cachedhitInstance)) return null;
                if (cachedhitInstance == null) return null;
                cachedCollider = cachedhitInstance.physicalColliderReferenceOnly;
                if (cachedCollider == null) return null;
                if (!(cachedCollider.gameObject.name.Contains("Wall") || cachedCollider.gameObject.name.Contains("Ceiling"))) return null;
                Debug.Log(string.Format("Wallhack detection on {0} from: {1} to: {2}", damage.attacker.client.userName, damage.attacker.character.eyesOrigin.ToString(), cachedBulletWeapon.worldPoint.ToString()));
                AntiCheatBroadcastAdmins(string.Format("Wallhack detection on {0} from: {1} to: {2}", damage.attacker.client.userName, damage.attacker.character.eyesOrigin.ToString(), cachedBulletWeapon.worldPoint.ToString()));
                damage.status = LifeStatus.IsAlive;
                damage.amount = 0f;
                takedamage.SetGodMode(false);
                takedamage.health = 10f;
                if (takedamage.GetComponent<HumanBodyTakeDamage>() != null) takedamage.GetComponent<HumanBodyTakeDamage>().SetBleedingLevel(0f);
                if (wallhackPunish) 
                { 
                    if (wallhackLogs[damage.attacker.client] == null) wallhackLogs[damage.attacker.client] = Time.realtimeSinceStartup;
                    if ((wallhackLogs[damage.attacker.client] - Time.realtimeSinceStartup) > 3) wallhackLogs[damage.attacker.client] = Time.realtimeSinceStartup;
                    if (wallhackLogs[damage.attacker.client] - Time.realtimeSinceStartup > 0.1) Punish(damage.attacker.client, "rWallhack");
                }
                return damage;
            }
            return null;
        }
        /////////////////////////
        // OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        // Called when a player spawns (after connection or after death)
        /////////////////////////
        void OnPlayerSpawn(PlayerClient player, bool useCamp, RustProto.Avatar avatar)
        {
            if (hasPermission(player.netUser)) return;
            PlayerHandler phandler = player.GetComponent<PlayerHandler>();
            if (phandler == null) { phandler = player.gameObject.AddComponent<PlayerHandler>(); phandler.timeleft = GetPlayerData(player); }
            timer.Once(0.1f, () => phandler.StartCheck());
        }

        /////////////////////////
        // OnPlayerConnected(NetUser netuser)
        // Called when a player connects
        /////////////////////////
        void OnPlayerConnected(NetUser netuser)
        {
            if(antiCeilingHack)
                netuser.playerClient.gameObject.AddComponent<CeilingHackHandler>();
        }

        /////////////////////////
        // OnPlayerConnected(NetUser netuser)
        // Called when a player connects
        /////////////////////////
        void CheckOverKill(TakeDamage takedamage, DamageEvent damage)
        {
            if (!(damage.extraData is WeaponImpact)) return;
            cachedWeapon = damage.extraData as WeaponImpact;
            if (cachedWeapon.dataBlock == null) return;
            if (!overkillDictionary.ContainsKey(cachedWeapon.dataBlock.name)) return;
            if (damage.victim.networkView == null) return;
            if (Vector3.Distance(damage.attacker.networkView.position, damage.victim.networkView.position) < Convert.ToSingle(overkillDictionary[cachedWeapon.dataBlock.name])) return;
            cachedOverkill = damage.attacker.client.GetComponent<OverKillHandler>();
            if (cachedOverkill == null) cachedOverkill = damage.attacker.client.gameObject.AddComponent<OverKillHandler>();
            if (Time.realtimeSinceStartup - cachedOverkill.lastOverkill > overkillResetTimer) cachedOverkill.number = 0f;
            cachedOverkill.lastOverkill = Time.realtimeSinceStartup;
            cachedOverkill.number++;
            AntiCheatBroadcastAdmins(string.Format("{0} did an OverKill with {1} @ {2}m", damage.attacker.client.userName, cachedWeapon.dataBlock.name, Math.Floor(Vector3.Distance(damage.attacker.networkView.position, damage.victim.networkView.position)).ToString()));
            if (overkillPunish && cachedOverkill.number >= overkillDetectionForPunish)
                Punish(damage.attacker.client, string.Format("rOverKill {0} ({1})", cachedWeapon.dataBlock.name, Math.Floor(Vector3.Distance(damage.attacker.networkView.position, damage.victim.networkView.position)).ToString()));
        }
        /*void CheckSilenceKill(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.victim.networkView == null) return;
            
            if (!(damage.extraData is WeaponImpact)) return;
            cachedWeapon = damage.extraData as WeaponImpact;
            if (cachedWeapon.dataBlock == null) return;
            if(Physics2.Raycast2(damage.attacker.client.controllable.GetComponent<Character>().eyesRay, out cachedRaycast2, 250f, 406721553))
            {
                var componenthit = (cachedRaycast2.remoteBodyPart == null) ? ((Component)cachedRaycast2.collider) : ((Component)cachedRaycast2.remoteBodyPart);
                Debug.Log(componenthit.ToString());
                Debug.Log(cachedRaycast2.distance.ToString());
                return;
            }
            Debug.Log("NO HIT");
        }*/
        void CheckNoRecoil(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.victim.networkView == null) return;
            if (damage.damageTypes != DamageTypeFlags.damage_bullet) return;
            if (Vector3.Distance(damage.attacker.networkView.position, damage.victim.networkView.position) < norecoilDistance) return;
            NoRecoilHandler norecoilhandler = damage.attacker.client.GetComponent<NoRecoilHandler>();
            if(norecoilhandler == null) norecoilhandler = damage.attacker.client.gameObject.AddComponent<NoRecoilHandler>();
            norecoilhandler.Kills++;
            Character character = damage.attacker.character;
            norecoilhandler.character = character;
            var eyeangles = (Angle2)character.eyesAngles;
            timer.Once(0.3f, () => CheckNewAngles(norecoilhandler, eyeangles, Time.realtimeSinceStartup));
        }
        void CheckNewAngles(NoRecoilHandler norecoilhandler, Angle2 oldAngles, float lasttimestamp)
        {
        	Character character = norecoilhandler.GetCharacter();
            if(character == null) return;
            if ((lasttimestamp - Time.realtimeSinceStartup) > 0.5f) return;
            if (oldAngles != character.eyesAngles) return;
            norecoilhandler.NoRecoils++;
            AntiCheatBroadcastAdmins(string.Format("{0} is suspected of having done a no recoil kill ({1} detections/{2} kills)", norecoilhandler.playerClient.userName, norecoilhandler.NoRecoils.ToString(), norecoilhandler.Kills.ToString()));
            if (!norecoilPunish) return;
            if (norecoilhandler.Kills < norecoilPunishMinKills) return;
            if (norecoilhandler.NoRecoils / norecoilhandler.Kills < norecoilPunishMinRatio/100) return;
            Punish(norecoilhandler.playerClient, string.Format("rNoRecoil({0}/{1})", norecoilhandler.NoRecoils.ToString(), norecoilhandler.Kills.ToString()));
        }
        
        void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {

            if (antiOverKill)
            {
                CheckOverKill(takedamage, damage);
            }
            if(antiNoRecoil)
            {
            	CheckNoRecoil(takedamage,damage);
            }
        }

        void OnItemDeployed(DeployableObject component, NetUser netuser)
        {
            if (!antiSleepingBagHack) return;
            if (component.gameObject.name == "SleepingBagA(Clone)" || component.gameObject.name == "SingleBed(Clone)")
            {
                var charr = netuser.playerClient.controllable.GetComponent<Character>();
                if (charr == null) return;
                if (!(MeshBatchPhysics.Linecast(charr.eyesOrigin,component.transform.position, out cachedRaycast, out cachedBoolean, out cachedhitInstance))) return;
                if (cachedhitInstance == null && cachedRaycast.collider.gameObject.name != "MetalDoor(Clone)") return;
                if (Vector3.Distance(charr.eyesOrigin, component.transform.position) > 9f) return;
                if ( component.transform.position.y - charr.eyesOrigin.y > 1f) return;
                AntiCheatBroadcastAdmins(string.Format("{0} tried to spawn a {1} @ {2} from {3}", netuser.playerClient.userName, component.gameObject.name.Replace("(Clone)",""), component.transform.position.ToString(), charr.eyesOrigin.ToString()));
                AntiCheatBroadcastAdmins(string.Format("{0} was on the way", (cachedhitInstance == null) ? "Metal Door" : cachedhitInstance.physicalColliderReferenceOnly.gameObject.name.Replace("(Clone)", "")));
                Puts(string.Format("{0} tried to spawn a {1} @ {2} from {3} threw {4}", netuser.playerClient.userName, component.gameObject.name.Replace("(Clone)", ""), component.transform.position.ToString(), charr.eyesOrigin.ToString(), (cachedhitInstance == null) ? "Metal Door" : cachedhitInstance.physicalColliderReferenceOnly.gameObject.name.Replace("(Clone)", "")));
                NetCull.Destroy(component.gameObject);
                if (sleepingbaghackPunish)
                    Punish(netuser.playerClient, string.Format("rSleepHack ({0})", (cachedhitInstance == null) ? "Metal Door" : cachedhitInstance.physicalColliderReferenceOnly.gameObject.name.Replace("(Clone)", "")));

            }
        } 
            /////////////////////////
            // AntiCheat Handler functions
            /////////////////////////

       
        NetUser GetLooter(Inventory inventory)
        {
            foreach (uLink.NetworkPlayer netplayer in (HashSet<uLink.NetworkPlayer>)getlooters.GetValue(inventory))
            {
                return (NetUser)netplayer.GetLocalData();
            }
            return null;
        }
        static void EndDetection(PlayerHandler player)
        { 
            GameObject.Destroy(player);
        }   
        static bool PlayerHandlerHasGround(PlayerHandler player)
        {
            if (!player.hasSearchedForFloor)
            {
                if (Physics.Raycast(player.playerclient.lastKnownPosition + UnderPlayerAdjustement, Vector3Down, out cachedRaycast, distanceDown))
                    player.currentFloorHeight = cachedRaycast.distance;
                else
                    player.currentFloorHeight = 10f;
            }
            player.hasSearchedForFloor = true;
            if (player.currentFloorHeight < 4f) return true;
            return false;
        }
        static bool IsOnSupport(PlayerHandler player)
        {
            foreach( Collider collider in Physics.OverlapSphere(player.playerclient.lastKnownPosition, 5f))
            {
                if (collider.GetComponent<UnityEngine.MeshCollider>())
                    return true;
            }
            return false;
        }
        public static void checkPlayer(PlayerHandler player)
        {
            if (antiSpeedHack)
                checkSpeedhack(player);
			if(antiWalkSpeedhack)
                checkWalkSpeedhack(player);
            if (antiSuperJump)
                checkSuperjumphack(player);
            if (antiFlyhack)
                checkAntiflyhack(player);
            //if (antiAutoGather)
             //   checkAutoGather(player);
        }
        public static void checkAutoGather(PlayerHandler player)
        {
            Inventory inv = player.GetInventory();
            if (inv.activeItem == null) return;
            inv.FindItem(wooddata, out cachedInt);
            Debug.Log(cachedInt.ToString());
            if (Physics.Raycast(player.GetCharacter().eyesRay, out cachedRaycast))
            Debug.Log(cachedRaycast.collider.ToString());
        }
        public static void checkAntiflyhack(PlayerHandler player)
        {
            if (player.distance3D == 0f) { player.flynum = 0; return; }
            if (PlayerHandlerHasGround(player)) { player.flynum = 0; return; }
            if (player.distanceHeight < -flyhackMaxDropSpeed) { player.flynum = 0; return; }
            if (IsOnSupport(player)) { player.flynum = 0; return; }
            if (player.lastFly != player.lastTick) { player.flynum = 0; player.lastFly = player.currentTick; return; }
            player.flynum++;
            player.lastFly = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rFlyhack ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.flynum < flyhackDetectionsForPunish) return;
            if (flyhackPunish) Punish(player.playerclient, string.Format("rFlyhack ({0}m/s)", player.distance3D.ToString()));
        }
        public static void checkSuperjumphack(PlayerHandler player)
        {
			if (player.distanceHeight < jumpMinHeight) { return; }
            if (player.distance3D > jumpMaxDistance) { return; }
            if (PlayerHandlerHasGround(player)) return;
            if (player.currentTick - player.lastJump > jumpDetectionsReset) player.jumpnum = 0;
            player.lastJump = player.currentTick;
            player.jumpnum++;
            AntiCheatBroadcastAdmins(string.Format("{0} - rSuperJump ({1}m/s)", player.playerclient.userName, player.distanceHeight.ToString()));
            if (player.jumpnum < jumpDetectionsNeed) return;
            if(jumpPunish) Punish(player.playerclient, string.Format("rSuperJump ({0}m/s)", player.distanceHeight.ToString()));
        }
        public static void checkWalkSpeedhack(PlayerHandler player)
        {
            if (player.character.stateFlags.sprint) { player.lastSprint = true; player.walkspeednum = 0; return; }
            if (player.distanceHeight < -walkspeedDropIgnore) { player.walkspeednum = 0; return; }
            if (player.distance3D < walkspeedMinDistance) { player.walkspeednum = 0; return; }
            if (!player.character.stateFlags.grounded) { player.lastSprint = true; player.walkspeednum = 0; return; }
            if (player.lastSprint) { player.lastSprint = false; player.walkspeednum = 0; return; }
            if (player.lastWalkSpeed != player.lastTick) { player.walkspeednum = 0; player.lastWalkSpeed = player.currentTick; return; }
            
            player.walkspeednum++;
            player.lastWalkSpeed = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rWalkspeed ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.walkspeednum < walkspeedDetectionForPunish) return;
            if (walkspeedPunish) Punish(player.playerclient, string.Format("rWalkspeed ({0}m/s)", player.distance3D.ToString()));
        }
        public static void checkSpeedhack(PlayerHandler player)
        {
            if (Math.Abs(player.distanceHeight) > speedDropIgnore) { player.speednum = 0; return; }
            if (player.distance3D < speedMinDistance) { player.speednum = 0; return; }
            if (player.lastSpeed != player.lastTick) { player.speednum = 0; player.lastSpeed = player.currentTick; return; }
            player.speednum++;
            player.lastSpeed = player.currentTick;
            AntiCheatBroadcastAdmins(string.Format("{0} - rSpeedhack ({1}m/s)", player.playerclient.userName, player.distance3D.ToString()));
            if (player.speednum < speedDetectionForPunish) return;
            if (speedPunish) Punish(player.playerclient, string.Format("rSpeedhack ({0}m/s)", player.distance3D.ToString()));
        }
        void CheckSupplyCrateLoot(Inventory inventory)
        {
            NetUser looter = GetLooter(inventory);
            if (looter == null) return;
            if (looter.playerClient == null) return;
            if (Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition) > 10f)
            {
                if (autoLoot[looter.playerClient] != null)
                    if (Time.realtimeSinceStartup - autoLoot[looter.playerClient] < 1f)
                        if(autolootPunish)
                            Punish(looter.playerClient, string.Format("rAutoLoot ({0}m)", Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition).ToString()));
                AntiCheatBroadcastAdmins(string.Format("{0} - rAutoLoot ({1}m)", looter.playerClient.userName, Vector3.Distance(inventory.transform.position, looter.playerClient.lastKnownPosition).ToString()));
                autoLoot[looter.playerClient] = Time.realtimeSinceStartup;
            }
        }
       
        static void AntiCheatBan(ulong userid, string name, string reason)
        {
            BanList.Add(userid, name, reason);
            BanList.Save();
        }

        static void Punish(PlayerClient player, string reason)
        {
            if (player.netUser.CanAdmin())
            {
                Debug.Log(string.Format("Ignored punish on {0} because he is an admin.",player.userName));
                if(player.GetComponent<PlayerHandler>() != null) GameObject.Destroy(player.GetComponent<PlayerHandler>());
                return;
            }
            ulong userid = player.userID;
            string username = player.userName;
            if (punishByBan)
            {
                AntiCheatBan(userid, username, reason);
                Interface.CallHook("cmdBan", userid.ToString(), username, reason);
                Debug.Log(string.Format("{0} {1} was auto banned for {2}", userid.ToString(), username, reason));
            }
            AntiCheatBroadcast(string.Format(playerHackDetectionBroadcast, username.ToString()));
            if (punishByKick || punishByBan)
            {
            	if(player != null && player.netUser != null)
                	player.netUser.Kick(NetError.Facepunch_Kick_Violation, true);
                Debug.Log(string.Format("{0} {1} was auto kicked for {2}", userid.ToString(), username.ToString(), reason));
            }
        }
        static void AntiCheatBroadcast(string message) { if (!broadcastPlayers) return; ConsoleNetworker.Broadcast("chat.add AntiCheat \"" + message + "\""); }

        static void AntiCheatBroadcastAdmins(string message)
        {
            if (!broadcastAdmins) return;
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.netUser.CanAdmin())
                    ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add AntiCheat \"" + message + "\"");
            }
        }


        /////////////////////////
        // Data Management
        /////////////////////////
        float GetPlayerData(PlayerClient player)
        {
            if (ACData[player.userID.ToString()] == null) ACData[player.userID.ToString()] = timetocheck.ToString();
            if (hasPermission(player.netUser)) ACData[player.userID.ToString()] = "0.0";
            return Convert.ToSingle(ACData[player.userID.ToString()]);
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("AntiCheat");
        }

        /////////////////////////
        // Random functions
        /////////////////////////
        bool hasPermission(NetUser netuser)
        {
            if (netuser.CanAdmin()) return true;
            return permission.UserHasPermission(netuser.playerClient.userID.ToString(), "cananticheat");
        }

        void CheckPlayer(PlayerClient player, bool forceAdmin)
        {
            PlayerHandler phandler = player.GetComponent<PlayerHandler>();
            if (phandler == null) phandler = player.gameObject.AddComponent<PlayerHandler>();
            if (!forceAdmin && hasPermission(player.netUser)) phandler.timeleft = 0f;
            else phandler.timeleft = timetocheck;
            timer.Once(0.1f, () => phandler.StartCheck());
        }
        PlayerClient FindPlayer(string name)
        {
            foreach (PlayerClient player in PlayerClient.All)
            {
                if (player.userName == name || player.userID.ToString() == name) return player;
            }
            return null;
        }

        /////////////////////////
        // Console Commands
        /////////////////////////
        [ConsoleCommand("ac.check")]
        void cmdConsoleCheck(ConsoleSystem.Arg arg)
        {
            if ((arg.Args == null) || (arg.Args != null && arg.Args.Length == 0)) { SendReply(arg, "ac.check \"Name/SteamID\""); return; }
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            cachedPlayer = FindPlayer(arg.ArgsStr);
            if(cachedPlayer == null) { SendReply(arg, noPlayerFound); return; }
            CheckPlayer(cachedPlayer,true);
            SendReply(arg, checkingPlayer, cachedPlayer.userName);
        }

        [ConsoleCommand("ac.checkall")]
        void cmdConsoleCheckAll(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            foreach (PlayerClient player in PlayerClient.All)
            {
                CheckPlayer(player,false);
            }
            SendReply(arg, checkingAllPlayers);
        }

        [ConsoleCommand("ac.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !hasPermission(arg.argUser)) { SendReply(arg, noAccess); return; }
            ACData.Clear();
            SaveData();
            foreach (PlayerClient player in PlayerClient.All)
            {
                CheckPlayer(player, false);
            }
            SendReply(arg, DataReset);
        }
    }
}
