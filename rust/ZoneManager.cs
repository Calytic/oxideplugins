// Reference: RustBuild

using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;

using Rust;

namespace Oxide.Plugins
{
    [Info("ZoneManager", "Reneb / Nogrod", "2.4.4", ResourceId = 739)]
    public class ZoneManager : RustPlugin
    {
        private const string PermZone = "zonemanager.zone";
        private const string PermCanDeploy = "zonemanager.candeploy";
        private const string PermCanBuild = "zonemanager.canbuild";

        [PluginReference]
        Plugin PopupNotifications;

        ////////////////////////////////////////////
        /// Configs
        ////////////////////////////////////////////
        private bool usePopups = false;
        private bool Changed;
        private bool Initialized;
        private float AutolightOnTime;
        private float AutolightOffTime;
        private string prefix;

        private object GetConfig(string menu, string datavalue, object defaultValue)
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

        private static bool GetBoolValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            value = value.Trim().ToLower();
            switch (value)
            {
                case "t":
                case "true":
                case "1":
                case "yes":
                case "y":
                case "on":
                    return true;
                default:
                    return false;
            }
        }
        private static BasePlayer FindPlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }
        private void LoadVariables()
        {
            AutolightOnTime = Convert.ToSingle(GetConfig("AutoLights", "Lights On Time", "18.0"));
            AutolightOffTime = Convert.ToSingle(GetConfig("AutoLights", "Lights Off Time", "8.0"));
            prefix = Convert.ToString(GetConfig("Chat", "Prefix", "<color=#FA58AC>ZoneManager:</color> "));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }


        ////////////////////////////////////////////
        /// FIELDS
        ////////////////////////////////////////////

        private readonly Dictionary<string, ZoneDefinition> ZoneDefinitions = new Dictionary<string, ZoneDefinition>();
        private readonly Dictionary<ulong, string> LastZone = new Dictionary<ulong, string>();
        private readonly Dictionary<BasePlayer, HashSet<Zone>> playerZones = new Dictionary<BasePlayer, HashSet<Zone>>();
        private readonly Dictionary<BaseCombatEntity, HashSet<Zone>> buildingZones = new Dictionary<BaseCombatEntity, HashSet<Zone>>();
        private readonly Dictionary<BaseNPC, HashSet<Zone>> npcZones = new Dictionary<BaseNPC, HashSet<Zone>>();
        private readonly Dictionary<ResourceDispenser, HashSet<Zone>> resourceZones = new Dictionary<ResourceDispenser, HashSet<Zone>>();
        private readonly Dictionary<BaseEntity, HashSet<Zone>> otherZones = new Dictionary<BaseEntity, HashSet<Zone>>();
        private readonly Dictionary<BasePlayer, ZoneFlags> playerTags = new Dictionary<BasePlayer, ZoneFlags>();

        private ZoneFlags disabledFlags = ZoneFlags.None;
        private DynamicConfigFile ZoneManagerData;
        private StoredData storedData;
        private Zone[] zoneObjects = new Zone[0];

        private static readonly FieldInfo decay = typeof(DecayEntity).GetField("decay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo decayTimer = typeof(DecayEntity).GetField("decayTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo decayDelayTime = typeof(DecayEntity).GetField("decayDelayTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo decayDeltaTime = typeof(DecayEntity).GetField("decayDeltaTime", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly FieldInfo npcNextTick = typeof(NPCAI).GetField("nextTick", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
        //private static readonly int triggerLayer = LayerMask.NameToLayer("Trigger");
        private static readonly int playersMask = LayerMask.GetMask("Player (Server)");
        //private static readonly int buildingMask = LayerMask.GetMask("Deployed", "Player (Server)", "Default", "Prevent Building");

        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);

        /////////////////////////////////////////
        // Zone
        // is a Monobehavior
        // used to detect the colliders with players
        // and created everything on it's own (radiations, locations, etc)
        /////////////////////////////////////////

        private static float GetSkyHour()
        {
            return TOD_Sky.Instance.Cycle.Hour;
        }

        public class Zone : MonoBehaviour
        {
            public ZoneDefinition Info;
            public ZoneManager ZoneManagerPlugin;
            public ZoneFlags disabledFlags = ZoneFlags.None;

            public readonly HashSet<ulong> WhiteList = new HashSet<ulong>();
            public readonly HashSet<ulong> KeepInList = new HashSet<ulong>();

            private HashSet<BasePlayer> players = new HashSet<BasePlayer>();
            private HashSet<BaseCombatEntity> buildings = new HashSet<BaseCombatEntity>();

            private bool lightsOn;

            private readonly FieldInfo InstancesField = typeof(MeshColliderBatch).GetField("instances", BindingFlags.Instance | BindingFlags.NonPublic);

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1; //hack to get all trigger layers...otherwise child zones
                gameObject.name = "Zone Manager";

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                var boxCollider = gameObject.GetComponent<BoxCollider>();
                if (Info.Size != Vector3.zero)
                {
                    if (sphereCollider != null) Destroy(sphereCollider);
                    if (boxCollider == null)
                    {
                        boxCollider = gameObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                    }
                    boxCollider.size = Info.Size;
                }
                else
                {
                    if (boxCollider != null) Destroy(boxCollider);
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = Info.Radius;
                }
            }

            public void SetInfo(ZoneDefinition info)
            {
                Info = info;
                if (Info == null) return;
                gameObject.name = $"Zone Manager({Info.Id})";
                transform.position = Info.Location;
                transform.rotation = Quaternion.Euler(Info.Rotation);
                UpdateCollider();
                gameObject.SetActive(Info.Enabled);
                enabled = Info.Enabled;

                if (ZoneManagerPlugin.HasZoneFlag(this, ZoneFlags.AutoLights))
                {
                    var currentTime = GetSkyHour();

                    if (currentTime > ZoneManagerPlugin.AutolightOffTime && currentTime < ZoneManagerPlugin.AutolightOnTime)
                        lightsOn = true;
                    else
                        lightsOn = false;
                    if (IsInvoking("CheckLights")) CancelInvoke("CheckLights");
                    InvokeRepeating("CheckLights", 5f, 30f);
                }

                var radiation = gameObject.GetComponent<TriggerRadiation>();
                if (Info.Radiation > 0)
                {
                    radiation = radiation ?? gameObject.AddComponent<TriggerRadiation>();
                    radiation.RadiationAmount = Info.Radiation;
                    radiation.radiationSize = Info.Radius;
                    radiation.interestLayers = playersMask;
                    radiation.enabled = Info.Enabled;
                }
                else if (radiation != null)
                {
                    radiation.RadiationAmount = 0;
                    radiation.radiationSize = 0;
                    radiation.interestLayers = playersMask;
                    radiation.enabled = false;
                    //Destroy(radiation);
                }
                if (IsInvoking("CheckEntites")) CancelInvoke("CheckEntites");
                InvokeRepeating("CheckEntites", 10f, 10f);
                /*if (HasAnyFlag(info.flags, ZoneFlags.Eject | ZoneFlags.EjectSleepers
                    | ZoneFlags.KillSleepers | ZoneFlags.NoBleed | ZoneFlags.NoBoxLoot | ZoneFlags.NoBuild
                    | ZoneFlags.NoChat | ZoneFlags.NoDeploy | ZoneFlags.NoDrown | ZoneFlags.NoKits
                    | ZoneFlags.NoPlayerLoot | ZoneFlags.NoRemove | ZoneFlags.NoSuicide | ZoneFlags.NoTp
                    | ZoneFlags.NoUpgrade | ZoneFlags.NoWounded | ZoneFlags.PveGod | ZoneFlags.PvpGod
                    | ZoneFlags.SleepGod | ZoneFlags.AutoLights))
                {
                    Interface.Oxide.LogInfo("Mask: Player (Server)");
                }*/
            }

            private void CheckEntites()
            {
                if (ZoneManagerPlugin == null) return;
                var oldPlayers = players;
                players = new HashSet<BasePlayer>();
                int entities;
                if (Info.Size != Vector3.zero)
                    entities = Physics.OverlapBoxNonAlloc(Info.Location, Info.Size, colBuffer, Quaternion.Euler(Info.Rotation), playersMask);
                else
                    entities = Physics.OverlapSphereNonAlloc(Info.Location, Info.Radius, colBuffer, playersMask);
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    colBuffer[i] = null;
                    if (player != null)
                    {
                        if (players.Add(player) && !oldPlayers.Contains(player))
                            ZoneManagerPlugin.OnPlayerEnterZone(this, player);
                    }
                }
                foreach (var player in oldPlayers)
                {
                    if (!players.Contains(player))
                        ZoneManagerPlugin.OnPlayerExitZone(this, player);
                }
            }

            private void OnDestroy()
            {
                CancelInvoke("CheckLights");
                ZoneManagerPlugin.OnZoneDestroy(this);
                ZoneManagerPlugin = null;
                Destroy(gameObject);
            }

            private void CheckLights()
            {
                if (ZoneManagerPlugin == null) return;
                var currentTime = GetSkyHour();
                if (currentTime > ZoneManagerPlugin.AutolightOffTime && currentTime < ZoneManagerPlugin.AutolightOnTime)
                {
                    if (!lightsOn) return;
                    foreach (var building in buildings)
                    {
                        var oven = building as BaseOven;
                        if (oven != null && !oven.IsInvoking("Cook"))
                        {
                            oven.SetFlag(BaseEntity.Flags.On, false);
                            continue;
                        }
                        var door = building as Door;
                        if (door != null && door.PrefabName.Contains("shutter"))
                            door.SetFlag(BaseEntity.Flags.Open, true);
                    }
                    foreach (var player in players)
                    {
                        if (player.userID >= 76560000000000000L || player.inventory?.containerWear?.itemList == null) continue; //only npc
                        var items = player.inventory.containerWear.itemList;
                        foreach (var item in items)
                        {
                            if (!item.info.shortname.Equals("hat.miner") && !item.info.shortname.Equals("hat.candle")) continue;
                            item.SwitchOnOff(false, player);
                            player.inventory.ServerUpdate(0f);
                            break;
                        }
                    }
                    lightsOn = false;
                }
                else
                {
                    if (lightsOn) return;
                    foreach (var building in buildings)
                    {
                        var oven = building as BaseOven;
                        if (oven != null && !oven.IsInvoking("Cook"))
                        {
                            oven.SetFlag(BaseEntity.Flags.On, true);
                            continue;
                        }
                        var door = building as Door;
                        if (door != null && door.PrefabName.Contains("shutter"))
                            door.SetFlag(BaseEntity.Flags.Open, false);
                    }
                    var fuel = ItemManager.FindItemDefinition("lowgradefuel");
                    foreach (var player in players)
                    {
                        if (player.userID >= 76560000000000000L || player.inventory?.containerWear?.itemList == null) continue; // only npc
                        var items = player.inventory.containerWear.itemList;
                        foreach (var item in items)
                        {
                            if (!item.info.shortname.Equals("hat.miner") && !item.info.shortname.Equals("hat.candle")) continue;
                            if (item.contents == null) item.contents = new ItemContainer();
                            var array = item.contents.itemList.ToArray();
                            for (var i = 0; i < array.Length; i++)
                                array[i].Remove(0f);
                            var newItem = ItemManager.Create(fuel, 100);
                            newItem.MoveToContainer(item.contents);
                            item.SwitchOnOff(true, player);
                            player.inventory.ServerUpdate(0f);
                            break;
                        }
                    }
                    lightsOn = true;
                }
            }

            public void OnEntityKill(BaseCombatEntity entity)
            {
                var player = entity as BasePlayer;
                if (player != null)
                    players.Remove(player);
                else if (!(entity is LootContainer) && !(entity is BaseHelicopter) && !(entity is BaseNPC))
                    buildings.Remove(entity);
            }

            private void CheckCollisionEnter(Collider col)
            {
                if (ZoneManagerPlugin.HasZoneFlag(this, ZoneFlags.NoDecay))
                {
                    var decayEntity = col.GetComponentInParent<DecayEntity>();
                    if (decayEntity != null && decay.GetValue(decayEntity) != null)
                    {
                        decayEntity.CancelInvoke("RunDecay");
                        decayTimer.SetValue(decayEntity, 0f);
                    }
                }
                var resourceDispenser = col.GetComponentInParent<ResourceDispenser>();
                if (resourceDispenser != null) //also BaseCorpse
                {
                    ZoneManagerPlugin.OnResourceEnterZone(this, resourceDispenser);
                    return;
                }
                var entity = col.GetComponentInParent<BaseEntity>();
                if (entity == null) return;
                var npc = entity as BaseNPC;
                if (npc != null)
                {
                    ZoneManagerPlugin.OnNpcEnterZone(this, npc);
                    return;
                }
                var combatEntity = entity as BaseCombatEntity;
                if (combatEntity != null && !(entity is LootContainer) && !(entity is BaseHelicopter))
                {
                    buildings.Add(combatEntity);
                    ZoneManagerPlugin.OnBuildingEnterZone(this, combatEntity);
                }
                else
                {
                    ZoneManagerPlugin.OnOtherEnterZone(this, entity);
                }
            }

            private void CheckCollisionLeave(Collider col)
            {
                if (ZoneManagerPlugin.HasZoneFlag(this, ZoneFlags.NoDecay))
                {
                    var decayEntity = col.GetComponentInParent<DecayEntity>();
                    if (decayEntity != null && decay.GetValue(decayEntity) != null && !decayEntity.IsInvoking("RunDecay"))
                        decayEntity.InvokeRepeating("RunDecay", (float) decayDelayTime.GetValue(decayEntity), (float) decayDeltaTime.GetValue(decayEntity));
                }
                var resourceDispenser = col.GetComponentInParent<ResourceDispenser>();
                if (resourceDispenser != null)
                {
                    ZoneManagerPlugin.OnResourceExitZone(this, resourceDispenser);
                    return;
                }
                var entity = col.GetComponentInParent<BaseEntity>();
                if (entity == null) return;
                var npc = entity as BaseNPC;
                if (npc != null)
                {
                    ZoneManagerPlugin.OnNpcExitZone(this, npc);
                    return;
                }
                var combatEntity = entity as BaseCombatEntity;
                if (combatEntity != null && !(entity is LootContainer) && !(entity is BaseHelicopter))
                {
                    buildings.Remove(combatEntity);
                    ZoneManagerPlugin.OnBuildingExitZone(this, combatEntity);
                }
                else
                {
                    ZoneManagerPlugin.OnOtherExitZone(this, entity);
                }
            }

            private void OnTriggerEnter(Collider col)
            {
                //Interface.Oxide.LogInfo("Enter {0}: {1}", Info.ID, col.name);
                var player = col.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    if (!players.Add(player)) return;
                    ZoneManagerPlugin.OnPlayerEnterZone(this, player);
                }
                else if (!col.transform.CompareTag("MeshColliderBatch"))
                    CheckCollisionEnter(col);
                else
                {
                    var colliderBatch = col.GetComponent<MeshColliderBatch>();
                    if (colliderBatch == null) return;
                    var colliders = (ListDictionary<Component, ColliderCombineInstance>) InstancesField.GetValue(colliderBatch);
                    foreach (var instance in colliders.Values)
                        CheckCollisionEnter(instance.collider);
                }
            }

            private void OnTriggerExit(Collider col)
            {
                //Interface.Oxide.LogInfo("Exit {0}: {1}", Info.ID, col.name);
                var player = col.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    if (!players.Remove(player)) return;
                    ZoneManagerPlugin.OnPlayerExitZone(this, player);
                }
                else if(!col.transform.CompareTag("MeshColliderBatch"))
                    CheckCollisionLeave(col);
                else
                {
                    var colliderBatch = col.GetComponent<MeshColliderBatch>();
                    if (colliderBatch == null) return;
                    var colliders = (ListDictionary<Component, ColliderCombineInstance>) InstancesField.GetValue(colliderBatch);
                    foreach (var instance in colliders.Values)
                        CheckCollisionLeave(instance.collider);
                }
            }
        }

        /////////////////////////////////////////
        // ZoneDefinition
        // Stored informations on the zones
        /////////////////////////////////////////
        public class ZoneDefinition
        {

            public string Name;
            public float Radius;
            public float Radiation;
            public Vector3 Location;
            public Vector3 Size;
            public Vector3 Rotation;
            public string Id;
            public string EnterMessage;
            public string LeaveMessage;
            public bool Enabled = true;
            public ZoneFlags Flags;

            public ZoneDefinition()
            {

            }

            public ZoneDefinition(Vector3 position)
            {
                Radius = 20f;
                Location = position;
            }

        }
        [Flags]
        public enum ZoneFlags// : long
        {
            None = 0,
            AutoLights = 1,
            Eject = 1 << 1,
            PvpGod = 1 << 2,
            PveGod = 1 << 3,
            SleepGod = 1 << 4,
            UnDestr = 1 << 5,
            NoBuild = 1 << 6,
            NoTp = 1 << 7,
            NoChat = 1 << 8,
            NoGather = 1 << 9,
            NoPve = 1 << 10,
            NoWounded = 1 << 11,
            NoDecay = 1 << 12,
            NoDeploy = 1 << 13,
            NoKits = 1 << 14,
            NoBoxLoot = 1 << 15,
            NoPlayerLoot = 1 << 16,
            NoCorpse = 1 << 17,
            NoSuicide = 1 << 18,
            NoRemove = 1 << 19,
            NoBleed = 1 << 20,
            KillSleepers = 1 << 21,
            NpcFreeze = 1 << 22,
            NoDrown = 1 << 23,
            NoStability = 1 << 24,
            NoUpgrade = 1 << 25,
            EjectSleepers = 1 << 26,
            NoPickup = 1 << 27,
            NoCollect = 1 << 28,
            NoDrop = 1 << 29
        }

        private bool HasZoneFlag(Zone zone, ZoneFlags flag)
        {
            if ((disabledFlags & flag) == flag) return false;
            return (zone.Info.Flags & ~zone.disabledFlags & flag) == flag;
        }
        private static bool HasAnyFlag(ZoneFlags flags, ZoneFlags flag)
        {
            return (flags & flag) != ZoneFlags.None;
        }
        private static bool HasAnyZoneFlag(Zone zone)
        {
            return (zone.Info.Flags & ~zone.disabledFlags) != ZoneFlags.None;
        }
        private static void AddZoneFlag(ZoneDefinition zone, ZoneFlags flag)
        {
            zone.Flags |= flag;
        }
        private static void RemoveZoneFlag(ZoneDefinition zone, ZoneFlags flag)
        {
            zone.Flags &= ~flag;
        }
        /////////////////////////////////////////
        // Data Management
        /////////////////////////////////////////
        private class StoredData
        {
            public readonly HashSet<ZoneDefinition> ZoneDefinitions = new HashSet<ZoneDefinition>();
        }

        private void SaveData()
        {
            ZoneManagerData.WriteObject(storedData);
        }

        private void LoadData()
        {
            ZoneDefinitions.Clear();
            try
            {
                ZoneManagerData.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = ZoneManagerData.ReadObject<StoredData>();
                Puts("Loaded {0} Zone definitions", storedData.ZoneDefinitions.Count);
            }
            catch
            {
                Puts("Failed to load StoredData");
                storedData = new StoredData();
            }
            ZoneManagerData.Settings.NullValueHandling = NullValueHandling.Include;
            foreach (var zonedef in storedData.ZoneDefinitions)
                ZoneDefinitions[zonedef.Id] = zonedef;
        }

        private void SetupCollectibleEntity()
        {
            var collectibleEntities = Resources.FindObjectsOfTypeAll<CollectibleEntity>();
            //Puts("Found {0} CollectibleEntities.", collectibleEntities.Length);
            for (var i = 0; i < collectibleEntities.Length; i++)
            {
                var collectibleEntity = collectibleEntities[i];
                if (collectibleEntity.GetComponent<Collider>() == null) collectibleEntity.gameObject.AddComponent<BoxCollider>();
            }
        }

        /////////////////////////////////////////
        // OXIDE HOOKS
        /////////////////////////////////////////

        /////////////////////////////////////////
        // Loaded()
        // Called when the plugin is loaded
        /////////////////////////////////////////
        private void Loaded()
        {
            //Puts("ZoneManager loaded: {0}", GetHashCode());
            ZoneManagerData = Interface.Oxide.DataFileSystem.GetFile("ZoneManager");
            ZoneManagerData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };
            permission.RegisterPermission(PermZone, this);
            permission.RegisterPermission(PermCanDeploy, this);
            permission.RegisterPermission(PermCanBuild, this);
            /* for(int i = 0; i < 25; i ++)
             {
                 Debug.Log(UnityEngine.LayerMask.LayerToName(i));
             }*/
            LoadData();
            LoadVariables();
            /*string[] options = new string[32];
            for (int i = 0; i < 32; i++)
            { // get layer names
                options[i] = i + " : " + LayerMask.LayerToName(i);
            }
            Puts("Layers: {0}", string.Join(", ", options));
            var sb = new StringBuilder();
            sb.AppendLine();
            for (int i = 0; i < 32; i++)
            {
                sb.Append(i + ":\t");
                for (int j = 0; j < 32; j++)
                {
                    sb.Append(Physics.GetIgnoreLayerCollision(i, j) ? "  " : "X ");
                }
                sb.AppendLine();
            }
            Puts(sb.ToString());*/
        }
        /////////////////////////////////////////
        // Unload()
        // Called when the plugin is unloaded
        /////////////////////////////////////////
        private void Unload()
        {
            foreach (var zone in zoneObjects)
                UnityEngine.Object.Destroy(zone);
            var collectibleEntities = Resources.FindObjectsOfTypeAll<CollectibleEntity>();
            for (var i = 0; i < collectibleEntities.Length; i++)
            {
                var collider = collectibleEntities[i].GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.Destroy(collider);
            }
        }

        private void OnTerrainInitialized()
        {
            if (Initialized) return;
            SetupCollectibleEntity();
            foreach (var zoneDefinition in ZoneDefinitions.Values)
                NewZone(zoneDefinition);
            Initialized = true;
        }

        private void OnServerInitialized()
        {
            if (Initialized) return;
            timer.In(1, () => {
                SetupCollectibleEntity();
                foreach (var zoneDefinition in ZoneDefinitions.Values)
                    NewZone(zoneDefinition);
            });
            Initialized = true;
        }

        /////////////////////////////////////////
        // OnEntityBuilt(Planner planner, GameObject gameobject)
        // Called when a buildingblock was created
        /////////////////////////////////////////
        private void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null) return;
            if (HasPlayerFlag(player, ZoneFlags.NoBuild) && !hasPermission(player, PermCanBuild))
            {
                gameobject.GetComponentInParent<BaseCombatEntity>().Kill(BaseNetworkable.DestroyMode.Gib);
                SendMessage(player, "You are not allowed to build here");
            }
        }

        private object OnStructureUpgrade(BuildingBlock buildingBlock, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoUpgrade) && !isAdmin(player)) return false;
            return null;
        }

        /////////////////////////////////////////
        // OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        // Called when an item was deployed
        /////////////////////////////////////////
        private void OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        {
            var player = deployer.GetOwnerPlayer();
            if (player == null) return;
            if (HasPlayerFlag(player, ZoneFlags.NoDeploy) && !hasPermission(player, PermCanDeploy))
            {
                deployedEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                SendMessage(player, "You are not allowed to deploy here");
            }
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity ownerEntity, float delta)
        {
            var player = ownerEntity as BasePlayer;
            if (player == null) return;
            if (metabolism.bleeding.value > 0 && HasPlayerFlag(player, ZoneFlags.NoBleed))
                metabolism.bleeding.value = 0f;
            if (metabolism.oxygen.value < 1 && HasPlayerFlag(player, ZoneFlags.NoDrown))
                metabolism.oxygen.value = 1;
        }

        /////////////////////////////////////////
        // OnPlayerChat(ConsoleSystem.Arg arg)
        // Called when a user writes something in the chat, doesn't take in count the commands
        /////////////////////////////////////////
        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return null;
            if (HasPlayerFlag(arg.Player(), ZoneFlags.NoChat))
            {
                SendMessage(arg.Player(), "You are not allowed to chat here");
                return false;
            }
            return null;
        }

        /////////////////////////////////////////
        // OnServerCommand(ConsoleSystem.Arg arg)
        // Called when a user executes a command
        /////////////////////////////////////////
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return null;
            if (arg.cmd?.name == null) return null;
            if (arg.cmd.name == "kill" && HasPlayerFlag(arg.Player(), ZoneFlags.NoSuicide))
            {
                SendMessage(arg.Player(), "You are not allowed to suicide here");
                return false;
            }
            return null;
        }

        /////////////////////////////////////////
        // OnPlayerDisconnected(BasePlayer player)
        // Called when a user disconnects
        /////////////////////////////////////////
        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (HasPlayerFlag(player, ZoneFlags.KillSleepers) && !isAdmin(player)) player.Die();
            else if (HasPlayerFlag(player, ZoneFlags.EjectSleepers))
            {
                HashSet<Zone> zones;
                if (!playerZones.TryGetValue(player, out zones) || zones.Count == 0) return;
                foreach (var zone in zones)
                {
                    if (HasZoneFlag(zone, ZoneFlags.EjectSleepers))
                    {
                        EjectPlayer(zone, player);
                        break;
                    }
                }
            }
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            var disp = hitinfo.HitEntity?.GetComponent<ResourceDispenser>();
            if (disp == null) return;
            HashSet<Zone> resourceZone;
            if (!resourceZones.TryGetValue(disp, out resourceZone)) return;
            foreach (var zone in resourceZone)
            {
                if (HasZoneFlag(zone, ZoneFlags.NoGather))
                    hitinfo.HitEntity = null;
            }
        }

        /////////////////////////////////////////
        // OnEntityAttacked(BaseCombatEntity entity, HitInfo hitinfo)
        // Called when any entity is attacked
        /////////////////////////////////////////
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (entity == null || entity.GetComponent<ResourceDispenser>() != null) return;
            var player = entity as BasePlayer;
            if (player != null)
            {
                var target = hitinfo.Initiator as BasePlayer;
                if (player.IsSleeping() && HasPlayerFlag(player, ZoneFlags.SleepGod))
                {
                    CancelDamage(hitinfo);
                }
                else if (target != null)
                {
                    if (target.userID < 76560000000000000L) return;
                    if (HasPlayerFlag(player, ZoneFlags.PvpGod))
                        CancelDamage(hitinfo);
                    else if (HasPlayerFlag(target, ZoneFlags.PvpGod))
                        CancelDamage(hitinfo);
                }
                else if (HasPlayerFlag(player, ZoneFlags.PveGod))
                    CancelDamage(hitinfo);
                else if (hitinfo.Initiator is FireBall && HasPlayerFlag(player, ZoneFlags.PvpGod))
                    CancelDamage(hitinfo);
                return;
            }
            var npcai = entity as BaseNPC;
            if (npcai != null)
            {
                HashSet<Zone> zones;
                if (!npcZones.TryGetValue(npcai, out zones)) return;
                foreach (var zone in zones)
                {
                    if (HasZoneFlag(zone, ZoneFlags.NoPve))
                    {
                        CancelDamage(hitinfo);
                        break;
                    }
                }
                return;
            }
            if (!(entity is LootContainer) && !(entity is BaseHelicopter))
            {
                HashSet<Zone> zones;
                if (!buildingZones.TryGetValue(entity, out zones)) return;
                foreach (var zone in zones)
                {
                    if (HasZoneFlag(zone, ZoneFlags.UnDestr))
                    {
                        CancelDamage(hitinfo);
                        break;
                    }
                }
            }
            /*else
            {
                HashSet<Zone> zones;
                if (otherZones.TryGetValue(entity, out zones))
                {
                }
            }*/
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            var entity = networkable as BaseEntity;
            if (entity == null) return;
            var resource = entity.GetComponent<ResourceDispenser>();
            if (resource != null)
            {
                HashSet<Zone> zones;
                if (resourceZones.TryGetValue(resource, out zones))
                    OnResourceExitZone(null, resource, true);
                return;
            }
            var player = entity as BasePlayer;
            if (player != null)
            {
                HashSet<Zone> zones;
                if (playerZones.TryGetValue(player, out zones))
                    OnPlayerExitZone(null, player, true);
                return;
            }
            var npc = entity as BaseNPC;
            if (npc != null)
            {
                HashSet<Zone> zones;
                if (npcZones.TryGetValue(npc, out zones))
                    OnNpcExitZone(null, npc, true);
                return;
            }
            var building = entity as BaseCombatEntity;
            if (building != null && !(entity is LootContainer) && !(entity is BaseHelicopter))
            {
                HashSet<Zone> zones;
                if (buildingZones.TryGetValue(building, out zones))
                    OnBuildingExitZone(null, building, true);
            }
            else
            {
                HashSet<Zone> zones;
                if (otherZones.TryGetValue(entity, out zones))
                    OnOtherExitZone(null, entity, true);
            }
        }


        /////////////////////////////////////////
        // OnEntitySpawned(BaseNetworkable entity)
        // Called when any kind of entity is spawned in the world
        /////////////////////////////////////////
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseCorpse)
            {
                timer.Once(2f, () =>
                {
                    HashSet<Zone> zones;
                    if (entity.isDestroyed || !resourceZones.TryGetValue(entity.GetComponent<ResourceDispenser>(), out zones)) return;
                    foreach (var zone in zones)
                    {
                        if (HasZoneFlag(zone, ZoneFlags.NoCorpse))
                        {
                            entity.KillMessage();
                            break;
                        }
                    }
                });
            }
            else if (entity is BuildingBlock && zoneObjects != null)
            {
                var block = (BuildingBlock)entity;
                foreach (var zone in zoneObjects)
                {
                    if (HasZoneFlag(zone, ZoneFlags.NoStability))
                    {
                        if (zone.Info.Size != Vector3.zero)
                        {
                            if (!new Bounds(zone.Info.Location, Quaternion.Euler(zone.Info.Rotation) * zone.Info.Size).Contains(block.transform.position))
                                continue;
                        }
                        else if (Vector3.Distance(block.transform.position, zone.Info.Location) > zone.Info.Radius)
                            continue;
                        block.grounded = true;
                        break;
                    }
                }
            }
            var npc = entity.GetComponent<NPCAI>();
            if (npc != null)
                npcNextTick.SetValue(npc, Time.time + 10f);
        }

        /////////////////////////////////////////
        // OnPlayerLoot(PlayerLoot lootInventory,  BasePlayer targetPlayer)
        // Called when a player tries to loot another player
        /////////////////////////////////////////
        private object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            return OnLootPlayerInternal(looter, target) ? null : (object)false;
        }

        private void OnLootPlayer(BasePlayer looter, BasePlayer target)
        {
            OnLootPlayerInternal(looter, target);
        }

        private bool OnLootPlayerInternal(BasePlayer looter, BasePlayer target)
        {
            if (HasPlayerFlag(looter, ZoneFlags.NoPlayerLoot) || target != null && HasPlayerFlag(target, ZoneFlags.NoPlayerLoot))
            {
                NextTick(looter.EndLooting);
                return false;
            }
            return true;
        }

        private void OnLootEntity(BasePlayer looter, BaseEntity target)
        {
            if (target is BaseCorpse)
                OnLootPlayerInternal(looter, null);
            else if (HasPlayerFlag(looter, ZoneFlags.NoBoxLoot))
            {
                if ((target as StorageContainer)?.transform.position == Vector3.zero) return;
                timer.Once(0.01f, looter.EndLooting);
            }
        }

        private object CanBeWounded(BasePlayer player, HitInfo hitinfo)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoWounded)) return false;
            return null;
        }

        /////////////////////////////////////////
        // Outside Plugin Hooks
        /////////////////////////////////////////

        private object canRedeemKit(BasePlayer player)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoKits)) { return "You may not redeem a kit inside this area"; }
            return null;
        }

        private object CanTeleport(BasePlayer player)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoTp)) { return "You may not teleport in this area"; }
            return null;
        }

        private object canRemove(BasePlayer player)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoRemove)) { return "You may not use the remover tool in this area"; }
            return null;
        }

        private bool CanChat(BasePlayer player)
        {
            if (HasPlayerFlag(player, ZoneFlags.NoChat))
            {
                //SendMessage(player, "You are not allowed to chat here");
                return false;
            }
            return true;
        }

        private void UpdateZoneDefinition(ZoneDefinition zone, string[] args, BasePlayer player = null)
        {
            for (var i = 0; i < args.Length; i = i + 2)
            {
                object editvalue;
                switch (args[i].ToLower())
                {
                    case "name":
                        editvalue = zone.Name = args[i + 1];
                        break;
                    case "id":
                        editvalue = zone.Id = args[i + 1];
                        break;
                    case "radiation":
                        editvalue = zone.Radiation = Convert.ToSingle(args[i + 1]);
                        break;
                    case "radius":
                        editvalue = zone.Radius = Convert.ToSingle(args[i + 1]);
                        break;
                    case "rotation":
                        zone.Rotation = player?.GetNetworkRotation() ?? Vector3.zero;/* + Quaternion.AngleAxis(90, Vector3.up).eulerAngles*/
                        zone.Rotation.x = 0;
                        editvalue = zone.Rotation;
                        break;
                    case "location":
                        if (player != null && args[i + 1].Equals("here", StringComparison.OrdinalIgnoreCase))
                        {
                            editvalue = zone.Location = player.transform.position;
                            break;
                        }
                        var loc = args[i + 1].Trim().Split(' ');
                        if (loc.Length == 3)
                            editvalue = zone.Location = new Vector3(Convert.ToSingle(loc[0]), Convert.ToSingle(loc[1]), Convert.ToSingle(loc[2]));
                        else
                        {
                            if (player != null) SendMessage(player, "Invalid location format, use: \"x y z\" or here");
                            continue;
                        }
                        break;
                    case "size":
                        var size = args[i + 1].Trim().Split(' ');
                        if (size.Length == 3)
                            editvalue = zone.Size = new Vector3(Convert.ToSingle(size[0]), Convert.ToSingle(size[1]), Convert.ToSingle(size[2]));
                        else
                        {
                            if (player != null) SendMessage(player, "Invalid size format, use: \"x y z\"");
                            continue;
                        }
                        break;
                    case "enter_message":
                        editvalue = zone.EnterMessage = args[i + 1];
                        break;
                    case "leave_message":
                        editvalue = zone.LeaveMessage = args[i + 1];
                        break;
                    case "enabled":
                    case "enable":
                        editvalue = zone.Enabled = GetBoolValue(args[i + 1]);
                        break;
                    default:
                        try
                        {
                            var flag = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), args[i], true);
                            var boolValue = GetBoolValue(args[i + 1]);
                            editvalue = boolValue;
                            if (boolValue) AddZoneFlag(zone, flag);
                            else RemoveZoneFlag(zone, flag);
                        }
                        catch
                        {
                            if (player != null) SendMessage(player, $"Unknown zone flag: {args[i]}");
                            continue;
                        }
                        break;
                }
                if (player != null) SendMessage(player, $"{args[i]} set to {editvalue}");
            }
        }

        /////////////////////////////////////////
        // External calls to this plugin
        /////////////////////////////////////////

        /////////////////////////////////////////
        // CreateOrUpdateZone(string ZoneID, object[] args)
        // Create or Update a zone from an external plugin
        // ZoneID should be a name, like Arena (for an arena plugin) (even if it's called an ID :p)
        // args are the same a the /zone command
        // args[0] = "radius" args[1] = "50" args[2] = "Eject" args[3] = "true", etc
        // Third parameter is obviously need if you create a NEW zone (or want to update the position)
        /////////////////////////////////////////
        private bool CreateOrUpdateZone(string zoneId, string[] args, Vector3 position = default(Vector3))
        {
            ZoneDefinition zonedef;
            if (!ZoneDefinitions.TryGetValue(zoneId, out zonedef))
                zonedef = new ZoneDefinition { Id = zoneId, Radius = 20 };
            else
                storedData.ZoneDefinitions.Remove(zonedef);
            UpdateZoneDefinition(zonedef, args);

            if (position != default(Vector3))
                zonedef.Location = position;

            ZoneDefinitions[zoneId] = zonedef;
            storedData.ZoneDefinitions.Add(zonedef);
            SaveData();

            if (zonedef.Location == null) return false;
            RefreshZone(zoneId);
            return true;
        }

        private bool EraseZone(string zoneId)
        {
            ZoneDefinition zone;
            if (!ZoneDefinitions.TryGetValue(zoneId, out zone)) return false;

            storedData.ZoneDefinitions.Remove(zone);
            ZoneDefinitions.Remove(zoneId);
            SaveData();
            RefreshZone(zoneId);
            return true;
        }

        private List<string> ZoneFieldListRaw()
        {
            var list = new List<string> { "name", "ID", "radiation", "radius", "rotation", "size", "Location", "enter_message", "leave_message" };
            list.AddRange(Enum.GetNames(typeof(ZoneFlags)));
            return list;
        }

        private Dictionary<string, string> ZoneFieldList(string zoneId)
        {
            var zone = GetZoneByID(zoneId);
            if (zone == null) return null;
            var fieldlistzone = new Dictionary<string, string>
            {
                { "name", zone.Info.Name },
                { "ID", zone.Info.Id },
                { "radiation", zone.Info.Radiation.ToString() },
                { "radius", zone.Info.Radius.ToString() },
                { "rotation", zone.Info.Rotation.ToString() },
                { "size", zone.Info.Size.ToString() },
                { "Location", zone.Info.Location.ToString() },
                { "enter_message", zone.Info.EnterMessage },
                { "leave_message", zone.Info.LeaveMessage }
            };

            var values = Enum.GetValues(typeof(ZoneFlags));
            foreach (var value in values)
                fieldlistzone[Enum.GetName(typeof(ZoneFlags), value)] = HasZoneFlag(zone, (ZoneFlags)value).ToString();
            return fieldlistzone;
        }

        private List<ulong> GetPlayersInZone(string zoneId)
        {
            var players = new List<ulong>();
            foreach (var pair in playerZones)
                players.AddRange(pair.Value.Where(zone => zone.Info.Id == zoneId).Select(zone => pair.Key.userID));
            return players;
        }

        private bool isPlayerInZone(string zoneId, BasePlayer player)
        {
            HashSet<Zone> zones;
            if (!playerZones.TryGetValue(player, out zones)) return false;
            return zones.Any(zone => zone.Info.Id == zoneId);
        }

        private bool AddPlayerToZoneWhitelist(string zoneId, BasePlayer player)
        {
            var targetZone = GetZoneByID(zoneId);
            if (targetZone == null) return false;
            AddToWhitelist(targetZone, player);
            return true;
        }

        private bool AddPlayerToZoneKeepinlist(string zoneId, BasePlayer player)
        {
            var targetZone = GetZoneByID(zoneId);
            if (targetZone == null) return false;
            AddToKeepinlist(targetZone, player);
            return true;
        }

        private bool RemovePlayerFromZoneWhitelist(string zoneId, BasePlayer player)
        {
            var targetZone = GetZoneByID(zoneId);
            if (targetZone == null) return false;
            RemoveFromWhitelist(targetZone, player);
            return true;
        }

        private bool RemovePlayerFromZoneKeepinlist(string zoneId, BasePlayer player)
        {
            var targetZone = GetZoneByID(zoneId);
            if (targetZone == null) return false;
            RemoveFromKeepinlist(targetZone, player);
            return true;
        }

        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }

        private void ShowZone(BasePlayer player, string zoneId)
        {
            var targetZone = GetZoneByID(zoneId);
            if (targetZone == null) return;
            if (targetZone.Info.Size != Vector3.zero)
            {
                //player.SendConsoleCommand("ddraw.box", 10f, Color.blue, targetZone.Info.Location, targetZone.Info.Size.magnitude);
                var center = targetZone.Info.Location;
                var rotation = Quaternion.Euler(targetZone.Info.Rotation);
                var size = targetZone.Info.Size / 2;
                var point1 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z + size.z), center, rotation);
                var point2 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z + size.z), center, rotation);
                var point3 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z - size.z), center, rotation);
                var point4 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z - size.z), center, rotation);
                var point5 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z + size.z), center, rotation);
                var point6 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z + size.z), center, rotation);
                var point7 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z - size.z), center, rotation);
                var point8 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z - size.z), center, rotation);

                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point2);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point3);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point5);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point2);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point3);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point8);

                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point6);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point7);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point6, point2);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point6);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point7);
                player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point7, point3);
            }
            else
                player.SendConsoleCommand("ddraw.sphere", 10f, Color.blue, targetZone.Info.Location, targetZone.Info.Radius);
        }

        /////////////////////////////////////////
        // Random Commands
        /////////////////////////////////////////
        private object GetZoneRadius(string zoneID) => GetZoneByID(zoneID)?.Info.Radius;
        private object GetZoneSize(string zoneID) => GetZoneByID(zoneID)?.Info.Size;
        private object CheckZoneID(string zoneID) => GetZoneByID(zoneID)?.Info.Id;
        private object GetZoneIDs() => zoneObjects.ToList().ConvertAll(z => z.Info.Id).ToArray();
        private Vector3 GetZoneLocation(string zoneId) => GetZoneByID(zoneId)?.Info.Location ?? Vector3.zero;
        private void AddToWhitelist(Zone zone, BasePlayer player) { zone.WhiteList.Add(player.userID); }
        private void RemoveFromWhitelist(Zone zone, BasePlayer player) { zone.WhiteList.Remove(player.userID); }
        private void AddToKeepinlist(Zone zone, BasePlayer player) { zone.KeepInList.Add(player.userID); }
        private void RemoveFromKeepinlist(Zone zone, BasePlayer player) { zone.KeepInList.Remove(player.userID); }

        private void AddDisabledFlag(string flagString)
        {
            try
            {
                var flag = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), flagString, true);
                disabledFlags |= flag;
            }
            catch
            {
            }
        }

        private void RemoveDisabledFlag(string flagString)
        {
            try
            {
                var flag = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), flagString, true);
                disabledFlags &= ~flag;
            }
            catch
            {
            }
        }

        private void AddZoneDisabledFlag(string zoneId, string flagString)
        {
            try
            {
                var zone = GetZoneByID(zoneId);
                var flag = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), flagString, true);
                zone.disabledFlags |= flag;
                UpdateAllPlayers();
            }
            catch
            {
            }
        }

        private void RemoveZoneDisabledFlag(string zoneId, string flagString)
        {
            try
            {
                var zone = GetZoneByID(zoneId);
                var flag = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), flagString, true);
                zone.disabledFlags &= ~flag;
                UpdateAllPlayers();
            }
            catch
            {
            }
        }

        private Zone GetZoneByID(string zoneId)
        {
            return zoneObjects.FirstOrDefault(gameObj => gameObj.Info.Id == zoneId);
        }

        private void NewZone(ZoneDefinition zonedef)
        {
            if (zonedef == null) return;
            var newZone = new GameObject().AddComponent<Zone>();
            newZone.ZoneManagerPlugin = this;
            newZone.SetInfo(zonedef);
            zoneObjects = Resources.FindObjectsOfTypeAll<Zone>();
        }

        private void RefreshZone(string zoneId)
        {
            var zone = GetZoneByID(zoneId);
            if (zone != null)
                UnityEngine.Object.Destroy(zone);
            ZoneDefinition zoneDef;
            if (ZoneDefinitions.TryGetValue(zoneId, out zoneDef))
                NewZone(zoneDef);
        }

        private void UpdateAllPlayers()
        {
            var players = playerTags.Keys.ToArray();
            for (var i = 0; i < players.Length; i++)
                UpdateFlags(players[i]);
        }

        private void UpdateFlags(BasePlayer player)
        {
            playerTags.Remove(player);
            HashSet<Zone> zones;
            if (!playerZones.TryGetValue(player, out zones) || zones.Count == 0) return;
            var newFlags = ZoneFlags.None;
            foreach (var zone in zones)
                newFlags |= zone.Info.Flags & ~zone.disabledFlags;
            playerTags[player] = newFlags;
        }

        private bool HasPlayerFlag(BasePlayer player, ZoneFlags flag)
        {
            if ((disabledFlags & flag) == flag) return false;
            ZoneFlags tags;
            if (!playerTags.TryGetValue(player, out tags)) return false;
            return (tags & flag) == flag;
        }

        private BasePlayer FindPlayerByRadius(Vector3 position, float rad)
        {
            var cachedColliders = Physics.OverlapSphere(position, rad, playersMask);
            return cachedColliders.Select(collider => collider.GetComponentInParent<BasePlayer>()).FirstOrDefault(player => player != null);
        }

        private void CheckExplosivePosition(TimedExplosive explosive)
        {
            if (explosive == null) return;
            foreach (var zone in zoneObjects)
            {
                if (!HasZoneFlag(zone, ZoneFlags.UnDestr)) continue;
                if (Vector3.Distance(explosive.GetEstimatedWorldPosition(), zone.transform.position) > zone.Info.Radius) continue;
                explosive.KillMessage();
                break;
            }
        }

        private static void CancelDamage(HitInfo hitinfo)
        {
            hitinfo.damageTypes = new DamageTypeList();
            hitinfo.DoHitEffects = false;
            hitinfo.HitMaterial = 0;
        }

        private void OnPlayerEnterZone(Zone zone, BasePlayer player)
        {
            HashSet<Zone> zones;
            if (!playerZones.TryGetValue(player, out zones))
                playerZones[player] = zones = new HashSet<Zone>();
            if (!zones.Add(zone)) return;
            UpdateFlags(player);
            if (!string.IsNullOrEmpty(zone.Info.EnterMessage))
            {
                if (PopupNotifications != null && usePopups)
                    PopupNotifications.Call("CreatePopupNotification", string.Format(zone.Info.EnterMessage, player.displayName), player);
                else
                    SendMessage(player, zone.Info.EnterMessage, player.displayName);
            }
            if (HasZoneFlag(zone, ZoneFlags.Eject)) EjectPlayer(zone, player);
            Interface.Oxide.CallHook("OnEnterZone", zone.Info.Id, player);
            //Puts("OnPlayerEnterZone: {0}", player.GetType());
        }

        private void OnPlayerExitZone(Zone zone, BasePlayer player, bool all = false)
        {
            HashSet<Zone> zones;
            if (!playerZones.TryGetValue(player, out zones)) return;
            if (!all)
            {
                zone.OnEntityKill(player);
                if (!zones.Remove(zone)) return;
                if (zones.Count <= 0) playerZones.Remove(player);
                if (!string.IsNullOrEmpty(zone.Info.LeaveMessage))
                {
                    if (PopupNotifications != null && usePopups)
                        PopupNotifications.Call("CreatePopupNotification", string.Format(zone.Info.LeaveMessage, player.displayName), player);
                    else
                        SendMessage(player, zone.Info.LeaveMessage, player.displayName);
                }
                if (zone.KeepInList.Contains(player.userID)) AttractPlayer(zone, player);
                Interface.Oxide.CallHook("OnExitZone", zone.Info.Id, player);
            }
            else
            {
                foreach (var zone1 in zones)
                {
                    if (!string.IsNullOrEmpty(zone1.Info.LeaveMessage))
                    {
                        if (PopupNotifications != null && usePopups)
                            PopupNotifications.Call("CreatePopupNotification", string.Format(zone1.Info.LeaveMessage, player.displayName), player);
                        else
                            SendMessage(player, zone1.Info.LeaveMessage, player.displayName);
                    }
                    if (zone1.KeepInList.Contains(player.userID)) AttractPlayer(zone1, player);
                    Interface.Oxide.CallHook("OnExitZone", zone1.Info.Id, player);
                }
                playerZones.Remove(player);
            }
            UpdateFlags(player);
            //Puts("OnPlayerExitZone: {0}", player.GetType());
        }

        private void OnResourceEnterZone(Zone zone, ResourceDispenser entity)
        {
            HashSet<Zone> zones;
            if (!resourceZones.TryGetValue(entity, out zones))
                resourceZones[entity] = zones = new HashSet<Zone>();
            if (!zones.Add(zone)) return;
            //Puts("OnResourceEnterZone: {0}", entity.GetType());
        }

        private void OnResourceExitZone(Zone zone, ResourceDispenser resource, bool all = false)
        {
            HashSet<Zone> zones;
            if (!resourceZones.TryGetValue(resource, out zones)) return;
            if (!all)
            {
                if (!zones.Remove(zone)) return;
                if (zones.Count <= 0) resourceZones.Remove(resource);
            }
            else
                resourceZones.Remove(resource);
            //Puts("OnResourceExitZone: {0}", resource.GetType());
        }

        private void OnNpcEnterZone(Zone zone, BaseNPC entity)
        {
            HashSet<Zone> zones;
            if (!npcZones.TryGetValue(entity, out zones))
                npcZones[entity] = zones = new HashSet<Zone>();
            if (!zones.Add(zone)) return;
            if (HasZoneFlag(zone, ZoneFlags.NpcFreeze))
                npcNextTick.SetValue(entity, 999999999999f);
            //Puts("OnNpcEnterZone: {0}", entity.GetType());
        }

        private void OnNpcExitZone(Zone zone, BaseNPC entity, bool all = false)
        {
            HashSet<Zone> zones;
            if (!npcZones.TryGetValue(entity, out zones)) return;
            if (!all)
            {
                if (!zones.Remove(zone)) return;
                if (zones.Count <= 0) npcZones.Remove(entity);
            }
            else
            {
                foreach (var zone1 in zones)
                {
                    if (!HasZoneFlag(zone1, ZoneFlags.NpcFreeze)) continue;
                    npcNextTick.SetValue(entity, Time.time);
                    break;
                }
                npcZones.Remove(entity);
            }
            //Puts("OnNpcExitZone: {0}", entity.GetType());
        }
        private void OnBuildingEnterZone(Zone zone, BaseCombatEntity entity)
        {
            HashSet<Zone> zones;
            if (!buildingZones.TryGetValue(entity, out zones))
                buildingZones[entity] = zones = new HashSet<Zone>();
            if (!zones.Add(zone)) return;
            if (HasZoneFlag(zone, ZoneFlags.NoStability))
            {
                var block = entity as StabilityEntity;
                if (block != null) block.grounded = true;
            }
            if (HasZoneFlag(zone, ZoneFlags.NoPickup))
            {
                var door = entity as Door;
                if (door == null) return;
                door.pickup.enabled = false;
            }
            //Puts("OnBuildingEnterZone: {0}", entity.GetType());
        }

        private void OnBuildingExitZone(Zone zone, BaseCombatEntity entity, bool all = false)
        {
            HashSet<Zone> zones;
            if (!buildingZones.TryGetValue(entity, out zones)) return;
            var stability = false;
            var pickup = false;
            if (!all)
            {
                zone.OnEntityKill(entity);
                if (!zones.Remove(zone)) return;
                stability = HasZoneFlag(zone, ZoneFlags.NoStability);
                pickup = HasZoneFlag(zone, ZoneFlags.NoPickup);
                if (zones.Count <= 0) buildingZones.Remove(entity);
            }
            else
            {
                foreach (var zone1 in zones)
                {
                    zone1.OnEntityKill(entity);
                    stability |= HasZoneFlag(zone1, ZoneFlags.NoStability);
                    pickup |= HasZoneFlag(zone1, ZoneFlags.NoPickup);
                }
                buildingZones.Remove(entity);
            }
            if (stability)
            {
                var block = entity as StabilityEntity;
                if (block == null) return;
                var prefab = GameManager.server.FindPrefab(PrefabAttribute.server.Find<Construction>(block.prefabID).fullName);
                block.grounded = prefab.GetComponent<StabilityEntity>()?.grounded ?? false;
            }
            if (pickup)
            {
                var door = entity as Door;
                if (door == null) return;
                var prefab = GameManager.server.FindPrefab(PrefabAttribute.server.Find<Construction>(door.prefabID).fullName);
                door.pickup.enabled = prefab.GetComponent<Door>()?.pickup.enabled ?? true;
            }
            //Puts("OnBuildingExitZone: {0}", entity.GetType());
        }

        private void OnOtherEnterZone(Zone zone, BaseEntity entity)
        {
            HashSet<Zone> zones;
            if (!otherZones.TryGetValue(entity, out zones))
                otherZones[entity] = zones = new HashSet<Zone>();
            if (!zones.Add(zone)) return;
            var collectible = entity as CollectibleEntity;
            if (collectible != null && HasZoneFlag(zone, ZoneFlags.NoCollect))
            {
                collectible.itemList = null;
            }
            var worldItem = entity as WorldItem;
            if (worldItem != null)
            {
                if (HasZoneFlag(zone, ZoneFlags.NoDrop))
                    timer.Once(2f, () =>
                    {
                        if (worldItem.isDestroyed) return;
                        worldItem.KillMessage();
                    });
                else if (HasZoneFlag(zone, ZoneFlags.NoPickup))
                    worldItem.allowPickup = false;
            }
            //Puts("OnOtherEnterZone: {0}", entity.GetType());
        }

        private void OnOtherExitZone(Zone zone, BaseEntity entity, bool all = false)
        {
            HashSet<Zone> zones;
            if (!otherZones.TryGetValue(entity, out zones)) return;
            var pickup = false;
            var collect = false;
            if (!all)
            {
                if (!zones.Remove(zone)) return;
                pickup = HasZoneFlag(zone, ZoneFlags.NoPickup);
                collect = HasZoneFlag(zone, ZoneFlags.NoCollect);
                if (zones.Count <= 0) otherZones.Remove(entity);
            }
            else
            {
                foreach (var zone1 in zones)
                {
                    pickup |= HasZoneFlag(zone1, ZoneFlags.NoPickup);
                    collect |= HasZoneFlag(zone1, ZoneFlags.NoCollect);
                }
                otherZones.Remove(entity);
            }
            if (collect)
            {
                var collectible = entity as CollectibleEntity;
                if (collectible != null && collectible.itemList == null)
                    collectible.itemList = GameManager.server.FindPrefab(entity).GetComponent<CollectibleEntity>().itemList;
            }
            if (pickup)
            {
                var worldItem = entity as WorldItem;
                if (worldItem != null && !worldItem.allowPickup)
                    worldItem.allowPickup = GameManager.server.FindPrefab(entity).GetComponent<WorldItem>().allowPickup;
            }
            //Puts("OnOtherExitZone: {0}", entity.GetType());
        }

        private void OnZoneDestroy(Zone zone)
        {
            HashSet<Zone> zones;
            foreach (var key in playerZones.Keys.ToArray())
                if (playerZones.TryGetValue(key, out zones) && zones.Contains(zone))
                    OnPlayerExitZone(zone, key);
            foreach (var key in buildingZones.Keys.ToArray())
                if (buildingZones.TryGetValue(key, out zones) && zones.Contains(zone))
                    OnBuildingExitZone(zone, key);
            foreach (var key in npcZones.Keys.ToArray())
                if (npcZones.TryGetValue(key, out zones) && zones.Contains(zone))
                    OnNpcExitZone(zone, key);
            foreach (var key in resourceZones.Keys.ToArray())
                if (resourceZones.TryGetValue(key, out zones) && zones.Contains(zone))
                    OnResourceExitZone(zone, key);
            foreach (var key in otherZones.Keys.ToArray())
            {
                if (!otherZones.TryGetValue(key, out zones))
                {
                    Puts("Zone: {0} Entity: {1} ({2}) {3}", zone.Info.Id, key.GetType(), key.net?.ID, key.isDestroyed);
                    continue;
                }
                if (zones.Contains(zone))
                    OnOtherExitZone(zone, key);
            }
            //UpdateAllPlayers();
        }

        private static void EjectPlayer(Zone zone, BasePlayer player)
        {
            if (isAdmin(player) || zone.WhiteList.Contains(player.userID) || zone.KeepInList.Contains(player.userID)) return;
            float dist;
            if (zone.Info.Size != Vector3.zero)
                dist = zone.Info.Size.x > zone.Info.Size.z ? zone.Info.Size.x : zone.Info.Size.z;
            else
                dist = zone.Info.Radius;
            var newPos = zone.transform.position + (player.transform.position - zone.transform.position).normalized * (dist + 5f);
            newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
            player.MovePosition(newPos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
            player.SendNetworkUpdateImmediate();
        }

        private static void AttractPlayer(Zone zone, BasePlayer player)
        {
            float dist;
            if (zone.Info.Size != Vector3.zero)
                dist = zone.Info.Size.x > zone.Info.Size.z ? zone.Info.Size.x : zone.Info.Size.z;
            else
                dist = zone.Info.Radius;
            var newPos = zone.transform.position + (player.transform.position - zone.transform.position).normalized * (dist - 5f);
            newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
            player.MovePosition(newPos);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
            player.SendNetworkUpdateImmediate();
        }

        private static bool isAdmin(BasePlayer player)
        {
            if (player?.net?.connection == null) return true;
            return player.net.connection.authLevel > 0;
        }

        private bool hasPermission(BasePlayer player, string permname)
        {
            return isAdmin(player) || permission.UserHasPermission(player.UserIDString, permname);
        }
        //////////////////////////////////////////////////////////////////////////////
        /// Chat Commands
        //////////////////////////////////////////////////////////////////////////////
        [ChatCommand("zone_add")]
        private void cmdChatZoneAdd(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            var newzoneinfo = new ZoneDefinition(player.transform.position) { Id = UnityEngine.Random.Range(1, 99999999).ToString() };
            NewZone(newzoneinfo);
            if (ZoneDefinitions.ContainsKey(newzoneinfo.Id)) storedData.ZoneDefinitions.Remove(ZoneDefinitions[newzoneinfo.Id]);
            ZoneDefinitions[newzoneinfo.Id] = newzoneinfo;
            LastZone[player.userID] = newzoneinfo.Id;
            storedData.ZoneDefinitions.Add(newzoneinfo);
            SaveData();
            ShowZone(player, newzoneinfo.Id);
            SendMessage(player, "New Zone created, you may now edit it: " + newzoneinfo.Location);
        }
        [ChatCommand("zone_reset")]
        private void cmdChatZoneReset(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            ZoneDefinitions.Clear();
            storedData.ZoneDefinitions.Clear();
            SaveData();
            Unload();
            SendMessage(player, "All Zones were removed");
        }
        [ChatCommand("zone_remove")]
        private void cmdChatZoneRemove(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            if (args.Length == 0) { SendMessage(player, "/zone_remove XXXXXID"); return; }
            ZoneDefinition zoneDef;
            if (!ZoneDefinitions.TryGetValue(args[0], out zoneDef)) { SendMessage(player, "This zone doesn't exist"); return; }
            storedData.ZoneDefinitions.Remove(zoneDef);
            ZoneDefinitions.Remove(args[0]);
            SaveData();
            RefreshZone(args[0]);
            SendMessage(player, "Zone " + args[0] + " was removed");
        }
        [ChatCommand("zone_stats")]
        private void cmdChatZoneStats(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }

            SendMessage(player, "Players: {0}", playerZones.Count);
            SendMessage(player, "Buildings: {0}", buildingZones.Count);
            SendMessage(player, "Npcs: {0}", npcZones.Count);
            SendMessage(player, "Resources: {0}", resourceZones.Count);
            SendMessage(player, "Others: {0}", otherZones.Count);
        }
        [ChatCommand("zone_edit")]
        private void cmdChatZoneEdit(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            string zoneId;
            if (args.Length == 0)
            {
                HashSet<Zone> zones;
                if (!playerZones.TryGetValue(player, out zones) || zones.Count != 1)
                {
                    SendMessage(player, "/zone_edit XXXXXID");
                    return;
                }
                zoneId = zones.First().Info.Id;
            }
            else
                zoneId = args[0];
            if (!ZoneDefinitions.ContainsKey(zoneId)) { SendMessage(player, "This zone doesn't exist"); return; }
            LastZone[player.userID] = zoneId;
            SendMessage(player, "Editing zone ID: " + zoneId);
            ShowZone(player, zoneId);
        }
        [ChatCommand("zone_player")]
        private void cmdChatZonePlayer(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            var targetPlayer = player;
            if (args != null && args.Length > 0)
            {
                targetPlayer = FindPlayer(args[0]);
                if (targetPlayer == null)
                {
                    SendMessage(player, "Player not found");
                    return;
                }
            }
            ZoneFlags tags;
            playerTags.TryGetValue(targetPlayer, out tags);
            SendMessage(player, $"=== {targetPlayer.displayName} ===");
            SendMessage(player, $"Flags: {tags}");
            SendMessage(player, "========== Zone list ==========");
            HashSet<Zone> zones;
            if (!playerZones.TryGetValue(targetPlayer, out zones) || zones.Count == 0) { SendMessage(player, "empty"); return; }
            foreach (var zone in zones)
                SendMessage(player, $"{zone.Info.Id} => {zone.Info.Name} - {zone.Info.Location}");
            UpdateFlags(targetPlayer);
        }
        [ChatCommand("zone_list")]
        private void cmdChatZoneList(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            SendMessage(player, "========== Zone list ==========");
            if (ZoneDefinitions.Count == 0) { SendMessage(player, "empty"); return; }
            foreach (var pair in ZoneDefinitions)
                SendMessage(player, $"{pair.Key} => {pair.Value.Name} - {pair.Value.Location}");
        }
        [ChatCommand("zone")]
        private void cmdChatZone(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            string zoneId;
            if (!LastZone.TryGetValue(player.userID, out zoneId)) { SendMessage(player, "You must first say: /zone_edit XXXXXID"); return; }

            var zoneDefinition = ZoneDefinitions[zoneId];
            if (args.Length < 1)
            {
                SendMessage(player, "/zone option value/reset");
                SendMessage(player, $"name => {zoneDefinition.Name}");
                SendMessage(player, $"enabled => {zoneDefinition.Enabled}");
                SendMessage(player, $"ID => {zoneDefinition.Id}");
                SendMessage(player, $"radiation => {zoneDefinition.Radiation}");
                SendMessage(player, $"radius => {zoneDefinition.Radius}");
                SendMessage(player, $"Location => {zoneDefinition.Location}");
                SendMessage(player, $"Size => {zoneDefinition.Size}");
                SendMessage(player, $"Rotation => {zoneDefinition.Rotation}");
                SendMessage(player, $"enter_message => {zoneDefinition.EnterMessage}");
                SendMessage(player, $"leave_message => {zoneDefinition.LeaveMessage}");
                SendMessage(player, $"flags => {zoneDefinition.Flags}");

                //var values = Enum.GetValues(typeof(ZoneFlags));
                //foreach (var value in values)
                //    SendMessage(player, $"{Enum.GetName(typeof(ZoneFlags), value)} => {HasZoneFlag(zoneDefinition, (ZoneFlags)value)}");
                ShowZone(player, zoneId);
                return;
            }
            if (args.Length % 2 != 0) { SendMessage(player, "Value missing..."); return; }
            UpdateZoneDefinition(zoneDefinition, args, player);
            RefreshZone(zoneId);
            SaveData();
            ShowZone(player, zoneId);
        }

        [ConsoleCommand("zone")]
        private void ccmdZone(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!hasPermission(player, PermZone)) { SendMessage(player, "You don't have access to this command"); return; }
            var zoneId = arg.GetString(0);
            ZoneDefinition zoneDefinition;
            if (!arg.HasArgs(3) || !ZoneDefinitions.TryGetValue(zoneId, out zoneDefinition)) { SendMessage(player, "Zone Id not found or Too few arguments: zone <zoneid> <arg> <value>"); return; }

            var args = new string[arg.Args.Length - 1];
            Array.Copy(arg.Args, 1, args, 0, args.Length);
            UpdateZoneDefinition(zoneDefinition, args, player);
            RefreshZone(zoneId);
            //SaveData();
            //ShowZone(player, zoneId);
        }

        private void SendMessage(BasePlayer player, string message, params object[] args)
        {
            if (player != null)
            {
                if (args.Length > 0) message = string.Format(message, args);
                SendReply(player, $"{prefix}{message}");
            }
            else
                Puts(message);
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }
    }
}
