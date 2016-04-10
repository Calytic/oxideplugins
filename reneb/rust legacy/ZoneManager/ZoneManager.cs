using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustProto;

namespace Oxide.Plugins
{
    [Info("ZoneManager", "Reneb", "1.0.2")]
    class ZoneManager : RustLegacyPlugin
    {
        ////////////////////////////////////////////
        /// FIELDS
        ////////////////////////////////////////////
        static RustServerManagement management;
         
        StoredData storedData; 

        static Hash<string, ZoneDefinition> zonedefinitions = new Hash<string, ZoneDefinition>();
        public Hash<PlayerClient, string> LastZone = new Hash<PlayerClient, string>();
        public static Hash<PlayerClient, List<Zone>> playerZones = new Hash<PlayerClient, List<Zone>>();

        public static int triggerLayer;
        public static int playersMask;

        public FieldInfo[] allZoneFields;
        public FieldInfo cachedField;
        public static FieldInfo fieldInfo;
         
        /////////////////////////////////////////
        /// Cached Fields, used to make the plugin faster
        /////////////////////////////////////////
        public static Vector3 cachedDirection;
        public Collider[] cachedColliders;
        //public DamageTypeList emptyDamageType;
        //public List<DamageTypeEntry> emptyDamageList;
        public PlayerClient cachedPlayer;

        /////////////////////////////////////////
        // ZoneLocation
        // Stored information for the zone location and radius
        /////////////////////////////////////////
        public class ZoneLocation
        {
            public string x;
            public string y;
            public string z;
            public string r;
            Vector3 position;
            float radius;

            public ZoneLocation() { }

            public ZoneLocation(Vector3 position, string radius)
            {
                x = position.x.ToString();
                y = position.y.ToString();
                z = position.z.ToString();

                r = radius.ToString();

                this.position = position;
                this.radius = float.Parse(radius);
            }

            public Vector3 GetPosition()
            {
                if (position == Vector3.zero)
                    position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                return position;
            }
            public float GetRadius()
            {
                if (radius == 0f)
                    radius = float.Parse(r);
                return radius;
            }
            public string String()
            {
                return string.Format("Pos({0},{1},{2}) - Rad({3})", x, y, z, r);
            }
        }
        /////////////////////////////////////////
        // RadiationZone
        // is a MonoBehaviour
        // This is needed for zones that use radiations only
        /////////////////////////////////////////
        public class RadiateZone : MonoBehaviour
        {
            public RadiationZone radiation;
            Zone zone;

            void Awake()
            {
                radiation = gameObject.AddComponent<RadiationZone>();
                zone = GetComponent<Zone>();
                radiation.exposurePerMin = float.Parse(zone.info.radiation);
                radiation.radius = GetComponent<UnityEngine.SphereCollider>().radius;
                Interface.CallHook("anticheatAllowRadiationZone", radiation);
            }
            void OnDestroy()
            {
                GameObject.Destroy(radiation);
            }

        }
        /////////////////////////////////////////
        // Zone
        // is a Monobehavior
        // used to detect the colliders with players
        // and created everything on it's own (radiations, locations, etc)
        /////////////////////////////////////////
        public class Zone : MonoBehaviour
        {
            public ZoneDefinition info;
            public List<PlayerClient> inTrigger = new List<PlayerClient>();
            public List<PlayerClient> whiteList = new List<PlayerClient>();
            public List<PlayerClient> keepInList = new List<PlayerClient>();

            Rigidbody rigidbody;
            RadiateZone radiationzone;
            float radiationamount; 

            void Awake()
            { 
                gameObject.layer = triggerLayer;
                gameObject.name = "Zone Manager";
               // this.rigidbody = gameObject.AddComponent<UnityEngine.Rigidbody>();
               // this.rigidbody.isKinematic = false;
                gameObject.AddComponent<UnityEngine.SphereCollider>();
                collider.isTrigger = true;
                gameObject.SetActive(true);
                enabled = false;
            }
            public void SetInfo(ZoneDefinition info)
            {
                this.info = info;
                GetComponent<UnityEngine.Transform>().position = info.Location.GetPosition();
                GetComponent<UnityEngine.SphereCollider>().radius = info.Location.GetRadius();
                radiationamount = 0f;

              //  this.rigidbody.position = GetComponent<UnityEngine.Transform>().position;
              //  this.rigidbody.constraints = UnityEngine.RigidbodyConstraints.FreezeAll;
                if (float.TryParse(info.radiation, out radiationamount))
                  radiationzone = gameObject.AddComponent<RadiateZone>();
            }

            void OnDestroy()
            {
                GameObject.Destroy(this.rigidbody);
                if(this.radiationzone != null)
                    GameObject.Destroy(this.radiationzone);
            } 
            void OnTriggerEnter(Collider col)
            {
                if (col.GetComponent<Character>())
                {
                    inTrigger.Add(col.GetComponent<Character>().playerClient);
                    OnEnterZone(this, col.GetComponent<Character>().playerClient);
                }
            }
            void OnTriggerExit(Collider col)
            {
                if (col.GetComponent<Character>())
                {
                    inTrigger.Remove(col.GetComponent<Character>().playerClient);
                    OnExitZone(this, col.GetComponent<Character>().playerClient);
                }
            }
        }

        /////////////////////////////////////////
        // ZoneDefinition
        // Stored informations on the zones
        /////////////////////////////////////////
        public class ZoneDefinition
        {

            public string name;
            public string radius;
            public ZoneLocation Location;
            public string ID;
            public string eject;
            public string pvpgod;
            public string pvegod;
            public string sleepgod;
            public string undestr;
            public string nobuild;
            public string notp;
            public string nochat;
            public string nodeploy;
            public string nokits;
            public string nosuicide;
            public string killsleepers;
            public string radiation;
            public string enter_message;
            public string leave_message;

            public ZoneDefinition()
            {

            }

            public ZoneDefinition(Vector3 position)
            {
                this.radius = "20";
                Location = new ZoneLocation(position, this.radius);
            }

        }
        /////////////////////////////////////////
        // Data Management
        /////////////////////////////////////////
        class StoredData
        {
            public HashSet<ZoneDefinition> ZoneDefinitions = new HashSet<ZoneDefinition>();
            public StoredData()
            {
            }
        }
        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("ZoneManager", storedData);
        }
        void LoadData()
        {
            zonedefinitions.Clear();
            try
            {
                storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("ZoneManager");
            }
            catch
            {
                storedData = new StoredData();
            }
            foreach (var zonedef in storedData.ZoneDefinitions)
                zonedefinitions[zonedef.ID] = zonedef;
        }
        /////////////////////////////////////////
        // OXIDE HOOKS
        /////////////////////////////////////////

        /////////////////////////////////////////
        // Loaded()
        // Called when the plugin is loaded
        /////////////////////////////////////////
        void Loaded()
        { 
            permission.RegisterPermission("zone", this);
            permission.RegisterPermission("candeploy", this);
            permission.RegisterPermission("canbuild", this); 
            triggerLayer = UnityEngine.LayerMask.NameToLayer("Character Collision");

            LoadData();
        }  
        /////////////////////////////////////////
        // Unload()
        // Called when the plugin is unloaded
        /////////////////////////////////////////
        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(Zone));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }
        void Unloaded()
        {
            SaveData();
        }
        /////////////////////////////////////////
        // OnServerInitialized()
        // Called when the server is initialized
        /////////////////////////////////////////
        void OnServerInitialized()
        {
            management = RustServerManagement.Get();
            allZoneFields = typeof(ZoneDefinition).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (KeyValuePair<string, ZoneDefinition> pair in zonedefinitions)
            {
                NewZone(pair.Value);
            }
        }

        /////////////////////////////////////////
        // OnEntityBuilt(Planner planner, GameObject gameobject)
        // Called when a buildingblock was created
        /////////////////////////////////////////
        void OnStructurePlaced(StructureComponent component, IStructureComponentItem item)
        {
            if (item.controllable == null) return;
            if (hasTag(item.controllable.playerClient, "nobuild"))
            {
                if (!hasPermission(item.controllable.playerClient, "canbuild"))
                {
                    SendMessage(item.controllable.playerClient, "You are not allowed to build here");
                    timer.Once(0.2f, () => DestroyObject(component.gameObject));
                    
                } 
            }
        } 
        void DestroyObject(GameObject obj)
        {
            if (obj != null)
                NetCull.Destroy(obj);
        }
        /////////////////////////////////////////
        // OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        // Called when an item was deployed
        /////////////////////////////////////////
        void OnItemDeployedByPlayer(DeployableObject deployedEntity, IDeployableItem deployableItem)
        {  
            if (deployableItem.controllable == null) return;
            if (hasTag(deployableItem.controllable.playerClient, "nodeploy"))
            {
                if (!hasPermission(deployableItem.controllable.playerClient, "candeploy"))
                {
                    timer.Once(0.01f, () => DestroyObject(deployedEntity.gameObject));
                    SendMessage(deployableItem.controllable.playerClient, "You are not allowed to deploy here");
                }
            }
            else if(deployedEntity.GetComponent<TimedExplosive>())
            {
                timer.Once(4f, () => CheckPositionExplosive(deployedEntity, deployableItem));
            }
        }
        void CheckPositionExplosive(DeployableObject deployedEntity, IDeployableItem deployableItem)
        {
            var objects = GameObject.FindObjectsOfType(typeof(Zone));
            if (objects != null)
                foreach (Zone zone in objects)
                {
                    if (zone.info.undestr == null) continue;
                    if(Vector3.Distance(deployedEntity.transform.position, zone.info.Location.GetPosition()) < (zone.info.Location.GetRadius() + 5f))
                    {
                        deployableItem.character.GetComponent<Inventory>().AddItemAmount(deployableItem.datablock, 1);
                        NetCull.Destroy(deployedEntity.gameObject);
                    }
                }
        }

        /////////////////////////////////////////
        // OnPlayerChat(ConsoleSystem.Arg arg)
        // Called when a user writes something in the chat, doesn't take in count the commands
        /////////////////////////////////////////
        object OnPlayerChat(NetUser netuser, string args)
        {
            if(hasTag(netuser.playerClient, "nochat"))
            {
                SendMessage(netuser.playerClient, "You are not allowed to chat here");
                return false;
            }
            return null;
        }

        /////////////////////////////////////////
        // OnRunCommand(ConsoleSystem.Arg arg)
        // Called when a user executes a command
        /////////////////////////////////////////
        object OnRunCommand(ConsoleSystem.Arg arg, bool shouldAnswer)
        {
            if (arg == null) return null;
            if (arg.argUser == null) return null;
            if (arg.Function != "suicide") return null;
            if (!hasTag(arg.argUser.playerClient, "nosuicide")) return null;
            SendMessage(arg.argUser.playerClient, "You are not allowed to suicide here");
            return false;
        }

        /////////////////////////////////////////
        // OnPlayerDisconnected(PlayerClient player)
        // Called when a user disconnects
        /////////////////////////////////////////
        void OnPlayerDisconnected(uLink.NetworkPlayer netplayer)
        {
            PlayerClient player = ((NetUser)netplayer.GetLocalData()).playerClient;
            if (hasTag(player, "killsleepers")) TakeDamage.KillSelf(player.controllable.GetComponent<Character>());
        }

        /////////////////////////////////////////
        // OnEntityAttacked(BaseCombatEntity entity, HitInfo hitinfo)
        // Called when any entity is attacked
        /////////////////////////////////////////
        object ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        {
            if (damage.victim.client != null)
            {
                if (damage.attacker.client != null)
                {
                    if (damage.attacker.client == damage.victim.client) return null;
                    if (hasTag(damage.victim.client, "pvpgod"))
                        return CancelDamage(damage);
                }
                else if (damage.attacker.networkView != null && damage.attacker.networkView.GetComponent<HostileWildlifeAI>())
                {
                    if (hasTag(damage.victim.client, "pvegod"))
                        return CancelDamage(damage);
                }
            }
            else if(takedamage.gameObject.name.Contains("MaleSleeper"))
            {
                if(damage.attacker.client != null)
                {
                    if (hasTag(damage.attacker.client, "sleepgod"))
                        return CancelDamage(damage);
                }
            }
            return null;
        }

        /////////////////////////////////////////
        // OnEntityDeath(BaseNetworkable basenet)
        // Called when any entity is spawned
        /////////////////////////////////////////
        void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            if(damage.victim.client != null)
            {
                if(playerZones[damage.victim.client] != null)
                    playerZones[damage.victim.client].Clear(); 
            }
        }


        /////////////////////////////////////////
        // Outside Plugin Hooks
        /////////////////////////////////////////

        /////////////////////////////////////////
        // canRedeemKit(BasePlayer player)
        // Called from the Kits plugin (Reneb) when trying to redeem a kit
        /////////////////////////////////////////
        object canRedeemKit(NetUser player)
        {
            if (hasTag(player.playerClient, "nokits")) { return "You may not redeem a kit inside this area"; }
            return null;
        }

        /////////////////////////////////////////
        // canTeleport(BasePlayer player)
        // Called from Teleportation System (Mughisi) when a player tries to teleport
        /////////////////////////////////////////
        object canTeleport(NetUser player)
        {
            if (hasTag(player.playerClient, "notp")) { return "You may not teleport in this area"; }
            return null;
        }

        /////////////////////////////////////////
        // External calls to this plugin
        /////////////////////////////////////////

        /////////////////////////////////////////
        // CreateOrUpdateZone(string ZoneID, object[] args)
        // Create or Update a zone from an external plugin
        // ZoneID should be a name, like Arena (for an arena plugin) (even if it's called an ID :p)
        // args are the same a the /zone command
        // args[0] = "radius" args[1] = "50" args[2] = "eject" args[3] = "true", etc
        // Third parameter is obviously need if you create a NEW zone (or want to update the position)
        /////////////////////////////////////////
        bool CreateOrUpdateZone(string ZoneID, string[] args, Vector3 position = default(Vector3))
        {
            ZoneDefinition zonedef;
            if (zonedefinitions[ZoneID] == null) zonedef = new ZoneDefinition();
            else zonedef = zonedefinitions[ZoneID];
            zonedef.ID = ZoneID;

            string editvalue;
            for (int i = 0; i < args.Length; i = i + 2)
            {

                cachedField = GetZoneField(args[i]);
                if (cachedField == null) continue;

                switch (args[i + 1])
                {
                    case "true":
                    case "1":
                        editvalue = "true";
                        break;
                    case "null":
                    case "0":
                    case "false":
                    case "reset":
                        editvalue = null;
                        break;
                    default:
                        editvalue = (string)args[i + 1];
                        break;
                }
                cachedField.SetValue(zonedef, editvalue);
                if (args[i].ToLower() == "radius") { if (zonedef.Location != null) zonedef.Location = new ZoneLocation(zonedef.Location.GetPosition(), editvalue); }
            }

            if (position != default(Vector3)) { zonedef.Location = new ZoneLocation((Vector3)position, (zonedef.radius != null) ? zonedef.radius : "20"); }

            if (zonedefinitions[ZoneID] != null) storedData.ZoneDefinitions.Remove(zonedefinitions[ZoneID]);
            zonedefinitions[ZoneID] = zonedef;
            storedData.ZoneDefinitions.Add(zonedefinitions[ZoneID]);
            SaveData();
            if (zonedef.Location == null) return false;
            RefreshZone(ZoneID);
            return true;
        }
        bool EraseZone(string ZoneID)
        {
            if (zonedefinitions[ZoneID] == null) return false;
            storedData.ZoneDefinitions.Remove(zonedefinitions[ZoneID]);
            zonedefinitions[ZoneID] = null;
            
            SaveData();
            RefreshZone(ZoneID);
            return true;
        }
        List<PlayerClient> GetPlayersInZone(string ZoneID)
        {
            List<PlayerClient> baseplayers = new List<PlayerClient>();
            foreach (KeyValuePair<PlayerClient, List<Zone>> pair in playerZones)
            {
                foreach (Zone zone in pair.Value)
                {
                    if (zone.info.ID == ZoneID)
                    {
                        baseplayers.Add(pair.Key);
                    }
                }
            }
            return baseplayers;
        }
        bool isPlayerInZone(string ZoneID, PlayerClient player)
        {
            if (playerZones[player] == null) return false;
            foreach (Zone zone in playerZones[player])
            {
                if (zone.info.ID == ZoneID)
                {
                    return true;
                }
            }
            return false;
        }
        bool AddPlayerToZoneWhitelist(string ZoneID, PlayerClient player)
        {
            Zone targetZone = GetZoneByID(ZoneID);
            if (targetZone == null) return false;
            AddToWhitelist(targetZone, player);
            return true;
        }
        bool AddPlayerToZoneKeepinlist(string ZoneID, PlayerClient player)
        {
            Zone targetZone = GetZoneByID(ZoneID);
            if (targetZone == null) return false;
            AddToKeepinlist(targetZone, player);
            return true;
        }
        bool RemovePlayerFromZoneWhitelist(string ZoneID, PlayerClient player)
        {
            Zone targetZone = GetZoneByID(ZoneID);
            if (targetZone == null) return false;
            RemoveFromWhitelist(targetZone, player);
            return true;
        }
        bool RemovePlayerFromZoneKeepinlist(string ZoneID, PlayerClient player)
        {
            Zone targetZone = GetZoneByID(ZoneID);
            if (targetZone == null) return false;
            RemoveFromKeepinlist(targetZone, player);
            return true; 
        }
        /////////////////////////////////////////
        // Random Commands
        /////////////////////////////////////////
        void AddToWhitelist(Zone zone, PlayerClient player) { if (!zone.whiteList.Contains(player)) zone.whiteList.Add(player); }
        void RemoveFromWhitelist(Zone zone, PlayerClient player) { if (zone.whiteList.Contains(player)) zone.whiteList.Remove(player); }
        void AddToKeepinlist(Zone zone, PlayerClient player) { if (!zone.keepInList.Contains(player)) zone.keepInList.Add(player); }
        void RemoveFromKeepinlist(Zone zone, PlayerClient player) { if (zone.keepInList.Contains(player)) zone.keepInList.Remove(player); }

        Zone GetZoneByID(string ZoneID)
        {
            var objects = GameObject.FindObjectsOfType(typeof(Zone));
            if (objects != null)
                foreach (Zone gameObj in objects)
                {
                    if (gameObj.info.ID == ZoneID) return gameObj;
                }

            return null;
        }

        void NewZone(ZoneDefinition zonedef)
        {
            if (zonedef == null) return;
            var newgameObject = new UnityEngine.GameObject();
            var newZone = newgameObject.AddComponent<Zone>();
            newZone.SetInfo(zonedef);
        }
        void RefreshZone(string zoneID)
        {
            var objects = GameObject.FindObjectsOfType(typeof(Zone));
            if (objects != null)
                foreach (Zone gameObj in objects)
                {
                    if (gameObj.info.ID == zoneID)
                    {
                        foreach (KeyValuePair<PlayerClient, List<Zone>> pair in playerZones)
                        {
                            if (pair.Value.Contains(gameObj)) playerZones[pair.Key].Remove(gameObj);
                        }
                        GameObject.Destroy(gameObj);
                        
                        break;
                    }
                }
            if (zonedefinitions[zoneID] != null)
            {
                NewZone(zonedefinitions[zoneID]);
            }
        }

        int GetRandom(int min, int max) { return UnityEngine.Random.Range(min, max); }

        FieldInfo GetZoneField(string name)
        {
            name = name.ToLower();
            foreach (FieldInfo fieldinfo in allZoneFields) { if (fieldinfo.Name == name) return fieldinfo; }
            return null;
        }
        static bool hasTag(PlayerClient player, string tagname)
        {
            if (playerZones[player] == null) { return false; }
            if (playerZones[player].Count == 0) { return false; }
            fieldInfo = typeof(ZoneDefinition).GetField(tagname, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Zone zone in playerZones[player])
            {
                if (fieldInfo.GetValue(zone.info) != null)
                    return true;
            }
            return false;
        }



        PlayerClient FindPlayerByRadius(Vector3 position, float rad)
        {
            cachedColliders = Physics.OverlapSphere(position, rad);
            foreach (Collider collider in cachedColliders)
            {
                if (collider.GetComponentInParent<PlayerClient>())
                    return collider.GetComponentInParent<PlayerClient>();
            }
            return null;
        }

        object CancelDamage(DamageEvent damage)
        {
            damage.amount = 0f;
            damage.status = LifeStatus.IsAlive;
            return damage;
        }
        static void OnEnterZone(Zone zone, PlayerClient player)
        {
            if (playerZones[player] == null) playerZones[player] = new List<Zone>();
            if (!playerZones[player].Contains(zone)) playerZones[player].Add(zone);
            if (zone.info.enter_message != null) SendMessage(player, zone.info.enter_message);
            if (zone.info.eject != null && !isAdmin(player) && !zone.whiteList.Contains(player) && !zone.keepInList.Contains(player)) EjectPlayer(zone, player);
            Interface.CallHook("OnEnterZone", zone.info.ID, player);
        } 
        static void OnExitZone(Zone zone, PlayerClient player)
        {
            if (playerZones[player].Contains(zone)) playerZones[player].Remove(zone);
            if (zone.info.leave_message != null) SendMessage(player, zone.info.leave_message);
            if (zone.keepInList.Contains(player)) AttractPlayer(zone, player);
            Interface.CallHook("OnExitZone", zone.info.ID, player);
        }
        static void IsCollidingEject(Zone zone, PlayerClient player)
        {
            if (playerZones[player] == null) return;
            EjectPlayer(zone, player);
        }
        static void EjectPlayer(Zone zone, PlayerClient player)
        {
            cachedDirection = player.lastKnownPosition - zone.transform.position;
            player.lastKnownPosition = zone.transform.position + (cachedDirection / cachedDirection.magnitude * (zone.GetComponent<UnityEngine.SphereCollider>().radius + 2f));
            management.TeleportPlayerToWorld(player.netPlayer, player.lastKnownPosition);
            Interface.CallHook("IsCollidingEject",zone, player);
        }
        static void AttractPlayer(Zone zone, PlayerClient player)
        {
            cachedDirection = player.lastKnownPosition - zone.transform.position;
            player.lastKnownPosition = zone.transform.position + (cachedDirection / cachedDirection.magnitude * (zone.GetComponent<UnityEngine.SphereCollider>().radius - 2f));
            management.TeleportPlayerToWorld(player.netPlayer, player.lastKnownPosition);
        }
        static bool isAdmin(PlayerClient player)
        {
            if (player.netUser.CanAdmin())
                return true;
            return false;
        }
        bool hasPermission(PlayerClient player, string permname)
        {
            if (player.netUser.CanAdmin())
                return true;
            return permission.UserHasPermission(player.userID.ToString(), permname);
        }
        //////////////////////////////////////////////////////////////////////////////
        /// Chat Commands
        //////////////////////////////////////////////////////////////////////////////
        [ChatCommand("zone_add")]
        void cmdChatZoneAdd(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            var newzoneinfo = new ZoneDefinition(player.playerClient.lastKnownPosition);
            newzoneinfo.ID = GetRandom(1, 99999999).ToString();
            NewZone(newzoneinfo);
            if (zonedefinitions[newzoneinfo.ID] != null) storedData.ZoneDefinitions.Remove(zonedefinitions[newzoneinfo.ID]);
            zonedefinitions[newzoneinfo.ID] = newzoneinfo;
            LastZone[player.playerClient] = newzoneinfo.ID;
            storedData.ZoneDefinitions.Add(zonedefinitions[newzoneinfo.ID]);
            SaveData();
            SendMessage(player.playerClient, "New Zone created, you may now edit it: " + newzoneinfo.Location.String());
        }
        [ChatCommand("zone_reset")]
        void cmdChatZoneReset(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            zonedefinitions.Clear();
            storedData.ZoneDefinitions.Clear();
            SaveData();
            Unload();
            SendMessage(player.playerClient, "All Zones were removed");
        }
        [ChatCommand("zone_remove")]
        void cmdChatZoneRemove(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            if (args.Length == 0) { SendMessage(player.playerClient, "/zone_remove XXXXXID"); return; }
            if (zonedefinitions[args[0]] == null) { SendMessage(player.playerClient, "This zone doesn't exist"); return; }
            storedData.ZoneDefinitions.Remove(zonedefinitions[args[0]]);
            zonedefinitions[args[0]] = null;
            SaveData();
            RefreshZone(args[0]);
            SendMessage(player.playerClient, "Zone " + args[0] + " was removed");
        }
        [ChatCommand("zone_edit")]
        void cmdChatZoneEdit(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            if (args.Length == 0) { SendMessage(player.playerClient, "/zone_edit XXXXXID"); return; }
            if (zonedefinitions[args[0]] == null) { SendMessage(player.playerClient, "This zone doesn't exist"); return; }
            LastZone[player.playerClient] = args[0];
            SendMessage(player.playerClient, "Editing zone ID: " + args[0]);
        }
        [ChatCommand("zone_list")]
        void cmdChatZoneList(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            SendMessage(player.playerClient, "========== Zone list ==========");
            if (zonedefinitions.Count == 0) { SendMessage(player.playerClient, "empty"); return; }
            foreach (KeyValuePair<string, ZoneDefinition> pair in zonedefinitions)
            {
                SendMessage(player.playerClient, string.Format("{0} => {1} - {2}", pair.Key, pair.Value.name, pair.Value.Location.String()));
            }
        }
        [ChatCommand("zone")]
        void cmdChatZone(NetUser player, string command, string[] args)
        {
            if (!hasPermission(player.playerClient, "zone")) { SendMessage(player.playerClient, "You don't have access to this command"); return; }
            if (LastZone[player.playerClient] == null) { SendMessage(player.playerClient, "You must first say: /zone_edit XXXXXID"); return; }
            object value;

            if (args.Length < 2)
            {
                SendMessage(player.playerClient, "/zone option value/reset");
                foreach (FieldInfo fieldinfo in allZoneFields)
                {
                    value = fieldinfo.GetValue(zonedefinitions[LastZone[player.playerClient]]);
                    switch (fieldinfo.Name)
                    {
                        case "Location":
                            value = ((ZoneLocation)value).String();
                            break;
                        default:
                            if (value == null) value = "false";
                            break;
                    }
                    SendMessage(player.playerClient, string.Format("{0} => {1}", fieldinfo.Name, value.ToString()));
                }
                return;
            }
            string editvalue;
            for (int i = 0; i < args.Length; i = i + 2)
            {

                cachedField = GetZoneField(args[i]);
                if (cachedField == null) continue;

                switch (args[i + 1])
                {
                    case "true":
                    case "1":
                        editvalue = "true";
                        break;
                    case "null":
                    case "0":
                    case "false":
                    case "reset":
                        editvalue = null;
                        break;
                    default:
                        editvalue = args[i + 1];
                        break;
                }
                cachedField.SetValue(zonedefinitions[LastZone[player.playerClient]], editvalue);
                if (args[i].ToLower() == "radius") { zonedefinitions[LastZone[player.playerClient]].Location = new ZoneLocation(zonedefinitions[LastZone[player.playerClient]].Location.GetPosition(), editvalue); }
                SendMessage(player.playerClient, string.Format("{0} set to {1}", cachedField.Name, editvalue));
            }
            RefreshZone(LastZone[player.playerClient]);
            SaveData();
        }
        static void SendMessage(PlayerClient player, string message) { ConsoleNetworker.SendClientCommand(player.netPlayer, "chat.add Oxide " + Facepunch.Utility.String.QuoteSafe(message));  }
    }
}

