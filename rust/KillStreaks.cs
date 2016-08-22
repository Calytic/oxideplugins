using System.Collections.Generic;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Killstreaks", "k1lly0u", "0.1.55", ResourceId = 1752)]
    class KillStreaks : RustPlugin
    {
        [PluginReference]
        Plugin Airstrike;
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;
        [PluginReference]
        Plugin EventManager;
        [PluginReference]
        Plugin Economics;
        [PluginReference]
        Plugin ServerRewards;

        private Dictionary<ulong, int> cachedData = new Dictionary<ulong, int>();
        private List<BaseHelicopter> activeHelis = new List<BaseHelicopter>();
        private List<ulong> asGren = new List<ulong>();
        private List<ulong> ssGren = new List<ulong>();
        private List<ulong> arGren = new List<ulong>();
        private List<ulong> heGren = new List<ulong>();
        private List<ulong> mrtdm = new List<ulong>();
        private List<ulong> turret = new List<ulong>();

        private bool isSignal = false;

        private Dictionary<ulong, StreakType> activeGrenades = new Dictionary<ulong, StreakType>();

        private Dictionary<int, StreakType> streakTypes = new Dictionary<int, StreakType>()        
        {
            { 0, StreakType.None },
            { 1, StreakType.Airstrike },
            { 2, StreakType.SquadStrike },
            { 3, StreakType.Artillery },
            { 4, StreakType.Helicopter },
            { 5, StreakType.SupplyDrop },
            { 6, StreakType.AirstrikeGrenade },
            { 7, StreakType.SquadStrikeGrenade },
            { 8, StreakType.ArtilleryGrenade },
            { 9, StreakType.HelicopterGrenade },
            { 10, StreakType.Martyrdom },
            { 11, StreakType.TurretDrop },
            { 12, StreakType.Coins },
            { 13, StreakType.RP }
        };


        DataStorage data;
        private DynamicConfigFile KSData;

        private static Vector2 warningPos = new Vector2(0.25f, 0.13f);
        private static Vector2 warningDim = new Vector2(0.5f, 0.12f);

        private readonly int triggerMask = LayerMask.GetMask("Trigger");
        #region oxide hooks
        void OnServerInitialized()
        {
            RegisterMessages();
            KSData = Interface.Oxide.DataFileSystem.GetFile("killstreak_data");
            LoadData();
            LoadVariables();
            CheckDependencies();           
        }
        private void CheckDependencies()
        {
            if (Friends == null)
            {
                if (useFriendsAPI)
                {
                    PrintWarning($"FriendsAPI could not be found! Disabling friends feature");
                    useFriendsAPI = false;
                }
            }

            if (Clans == null)
            {
                if (useClans)
                {
                    PrintWarning($"Clans could not be found! Disabling clans feature");
                    useClans = false;
                }
            }
            if (Airstrike == null)
            {
                if (useAirstrike)
                {
                    PrintWarning($"Airstrike could not be found! Disabling airstrike feature");
                    useAirstrike = false;
                }
            }
            if (Economics == null)
            {
                PrintWarning("Economics could not be found! Unable to issue monetary rewards");
            }
            if (ServerRewards == null)
            {
                PrintWarning("ServerRewards could not be found! Unable to issue RP rewards");
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();            
        }
        void OnEntityDeath(BaseEntity entity, HitInfo hitinfo)
        {
            try
            {
                if (entity == null || hitinfo?.Initiator == null) return;
                if (entity is BasePlayer)
                {
                    ProcessDeath((BasePlayer)entity, hitinfo);
                    if (hitinfo.Initiator is BasePlayer)
                        ProcessKill((BasePlayer)hitinfo.Initiator, (BasePlayer)entity);
                }
                else if (entity is BaseHelicopter)
                        if (activeHelis.Contains((BaseHelicopter)entity)) activeHelis.Remove((BaseHelicopter)entity);
            }
            catch(Exception ex)
            {
            }      
        }
        void OnPlayerDisconnected(BasePlayer player) => ProcessDeath(player, null, true);
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            var ID = player.userID;
            if (activeGrenades.ContainsKey(ID))
            {
                isSignal = true;
                timer.Once(3, () =>
                {
                    Vector3 pos = entity.transform.position;
                    if (pos == null) Puts("null");
                    if (activeGrenades[ID] == StreakType.AirstrikeGrenade) CallAirstrike(pos);
                    else if (activeGrenades[ID] == StreakType.SquadStrikeGrenade) CallAirstrike(pos, false);
                    else if (activeGrenades[ID] == StreakType.ArtilleryGrenade) { LaunchArtillery(pos); timer.Once(10, () => entity.Kill()); }
                    else if (activeGrenades[ID] == StreakType.HelicopterGrenade)
                    {
                        var count = cachedData[player.userID];
                        Vector3 strikePos = entity.GetEstimatedWorldPosition();
                        CallHeli(strikePos, count, true);
                    }
                    else if (activeGrenades[ID] == StreakType.TurretDrop) { timer.Once(3, () => DropTurret(pos, player)); timer.Once(6, () => entity.Kill()); }
                        activeGrenades.Remove(ID);
                    timer.Once(2.8f, () =>
                    {
                        Effect.server.Run("assets/bundled/prefabs/fx/smoke_signal.prefab", pos);
                        if (entity != null)
                            entity.Kill(BaseNetworkable.DestroyMode.None);
                    });
                });
            }
        }        
        void Unload()
        {  
            SaveData();
            KillHeli();          
        }
        #endregion

        #region functions    
        private bool HasPriv(BasePlayer player)
        {
            var hit = Physics.OverlapSphere(player.transform.position, 2f, triggerMask);
            foreach (var entity in hit)
            {
                BuildingPrivlidge privs = entity.GetComponentInParent<BuildingPrivlidge>();
                if (privs != null)
                    if (privs.IsAuthed(player)) return true;
            }
            return false;
        }
        private void ProcessKill(BasePlayer player, BasePlayer victim)
        {
            if (ignoreBuildPriv)
            {
                if (HasPriv(player))
                    return;
            }
            if (useClans)
                if (victim != null)
                    if (IsClanmate(player.userID, victim.userID))
                        return;
            if (useFriendsAPI)
                if (victim != null)
                    if (IsFriend(player.userID, victim.userID))
                        return;
            if (EventManager)
            {
                object isPlaying = EventManager?.Call("isPlaying", new object[] { player });
                if (isPlaying is bool)
                    if ((bool)isPlaying)
                        return;
            }
            if (!data.killStreakData.ContainsKey(player.userID))
                data.killStreakData.Add(player.userID, new KSDATA() { Name = player.displayName, highestKS = 0 });

            if (!cachedData.ContainsKey(player.userID))
                cachedData.Add(player.userID, 0);

            cachedData[player.userID]++;

            if (cachedData[player.userID] > data.killStreakData[player.userID].highestKS)
                data.killStreakData[player.userID].highestKS = cachedData[player.userID];

            Deal(player);          
        }
        private void ProcessDeath(BasePlayer player, HitInfo info, bool disconnected = false)
        {
            if (cachedData.ContainsKey(player.userID))
            {                
                if (!disconnected)
                    if (mrtdm.Contains(player.userID))
                        ChooseRandomExp(player.transform.position);

                string deathType = lang.GetMessage("suic", this, player.UserIDString);
                if (disconnected)
                    deathType = lang.GetMessage("disconnected", this, player.UserIDString);
                if (info != null) deathType = GetDeathType(info.Initiator);
                                
                ClearPlayerRewards(player);

                if (broadcastEnd)
                    BroadcastToAll(lang.GetMessage("endstreak", this, player.UserIDString) + deathType, player.displayName);                
            }
        }        
        private void ClearPlayerRewards(BasePlayer player)
        {
            ulong ID = player.userID;
            if (asGren.Contains(ID)) asGren.Remove(ID);
            if (ssGren.Contains(ID)) ssGren.Remove(ID);
            if (arGren.Contains(ID)) arGren.Remove(ID);
            if (heGren.Contains(ID)) heGren.Remove(ID);
            if (mrtdm.Contains(ID)) mrtdm.Remove(ID);
            if (turret.Contains(ID)) turret.Remove(ID);
            cachedData.Remove(ID);
        }
        public string GetDeathType(BaseEntity entity)
        {
            string deathtype = "";
            if (entity == null)
                return null;            
            else if (entity.ToPlayer() != null) deathtype = entity.ToPlayer().displayName;
            else if (entity.name.Contains("patrolhelicopter.pr")) deathtype = lang.GetMessage("aheli", this);
            else if (entity.name.Contains("animals/")) deathtype = lang.GetMessage("aanim", this);            
            else if (entity.name.Contains("beartrap.prefab")) deathtype = lang.GetMessage("abt", this);
            else if (entity.name.Contains("landmine.prefab")) deathtype = lang.GetMessage("aldm", this);
            else if (entity.name.Contains("spikes.floor.prefab")) deathtype = lang.GetMessage("flrsp", this);
            else if (entity.name.Contains("autoturret_deployed.prefab")) deathtype = lang.GetMessage("aturr", this);
            else if (entity.name.Contains("deployable/barricades") || entity.name.Contains("wall.external.high")) deathtype = lang.GetMessage("awall", this);
            return deathtype;
        }
        private void BroadcastToAll(string msg, string keyword) => PrintToChat(fontColor1 + keyword + " </color>" + fontColor2 + msg + "</color>");
        private void BroadcastToPlayer(BasePlayer player, string msg, string keyword) => SendReply(player, fontColor1 + keyword + " </color>" + fontColor2 + msg + "</color>");
        private void GUIToPlayer(BasePlayer player, string msg, string keyword) => KSUI.GetPlayer(player).UseUI(fontColor1 + keyword + " </color>" + fontColor2 + msg + "</color>", warningPos, warningDim, 20);
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans || !useClans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private List<BasePlayer> FindNearbyFriends(BasePlayer player)
        {
            List<BaseEntity> nearbyPlayers = new List<BaseEntity>();
            List<BasePlayer> nearbyFriends = new List<BasePlayer>();
            Vis.Entities(player.transform.position, nearbyRadius, nearbyPlayers);            
            foreach(var entry in nearbyPlayers)
                if (entry is BasePlayer)
                    if (entry != null)
                        if (IsClanmate(entry.ToPlayer().userID, player.userID) || IsFriend(entry.ToPlayer().userID, player.userID))
                            nearbyFriends.Add(entry.ToPlayer());
            if (nearbyFriends.Count > 0)
                return nearbyFriends;                       
            return null;
        }
       
        private bool IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends || !useFriendsAPI) return false;
            bool isFriend = (bool)Friends?.Call("IsFriend", playerID, friendID);
            return isFriend;
        }
        private Item GiveSupplySignal()
        {
            var definition = ItemManager.FindItemDefinition("supply.signal");
            if (definition != null)
            {
                Item item = ItemManager.CreateByItemID((int)definition.itemid, 1);
                return item;
            }
            return null;
        }
        #endregion
       
        #region punishments/prizes
        private void Deal(BasePlayer player)
        {
            var count = cachedData[player.userID];
            if (data.killStreaks.ContainsKey(count))
            {
                string langKey = data.killStreaks[count].Message;
                if (broadcastMsg)
                    BroadcastToAll(langKey + string.Format(" {0} kills", count), player.displayName);
                else BroadcastToPlayer(player, player.displayName + langKey, string.Format(" {0} kills", count));
                string message = lang.GetMessage("attract", this);

                if (data.killStreaks[count].StreakType != StreakType.None)
                {
                    
                    var streakType = data.killStreaks[count].StreakType;
                    var pos = player.transform.position;
                    switch (streakType)
                    {
                        case StreakType.Airstrike:
                            if (Airstrike)
                            {
                                CallAirstrike(pos);
                                message = lang.GetMessage("asLaunch", this, player.UserIDString);
                            }
                            break;
                        case StreakType.SquadStrike:
                            if (Airstrike)
                            {
                                CallAirstrike(pos, false);
                                message = lang.GetMessage("ssLaunch", this, player.UserIDString);
                            }
                            break;
                        case StreakType.Artillery:
                                LaunchArtillery(pos);
                            message = lang.GetMessage("arLaunch", this, player.UserIDString);
                            break;
                        case StreakType.Helicopter:
                                CallHeli(player.transform.position, count, false, player);
                            message = lang.GetMessage("asLaunch", this, player.UserIDString);
                            break;
                        case StreakType.SupplyDrop:                            
                                SendSupplyDrop(player, count);
                            message = lang.GetMessage("sdLaunch", this, player.UserIDString);
                            break;
                        case StreakType.Martyrdom:
                            SetMartyrdom(player);
                            message = lang.GetMessage("mrtdmActive", this, player.UserIDString);
                            break;
                        case StreakType.AirstrikeGrenade:
                            GiveRewardGrenade(player, StreakType.AirstrikeGrenade);
                            message = lang.GetMessage("asGrenade", this, player.UserIDString);
                            break;
                        case StreakType.SquadStrikeGrenade:
                            GiveRewardGrenade(player, StreakType.SquadStrikeGrenade);
                            message = lang.GetMessage("ssGrenade", this, player.UserIDString);
                            break;
                        case StreakType.ArtilleryGrenade:
                            GiveRewardGrenade(player, StreakType.ArtilleryGrenade);
                            message = lang.GetMessage("arGrenade", this, player.UserIDString);
                            break;
                        case StreakType.HelicopterGrenade:
                            GiveRewardGrenade(player, StreakType.HelicopterGrenade);
                            message = lang.GetMessage("heGrenade", this, player.UserIDString);
                            break;
                        case StreakType.TurretDrop:
                            GiveRewardGrenade(player, StreakType.TurretDrop);
                            message = lang.GetMessage("tuGrenade", this, player.UserIDString);
                            break;
                        case StreakType.Coins:
                            if (!Economics) return;
                            message = GiveEconomics(player, count);                            
                            break;
                        case StreakType.RP:
                            if (!ServerRewards) return;
                            message = GiveRP(player, count);
                            break;
                    }
                    GUIToPlayer(player, message, lang.GetMessage("warning", this));
                    Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetacquired.prefab", player.transform.position);
                }

            }
        }
        private void GiveRewardGrenade(BasePlayer player, StreakType type)
        {
            List<ulong> list = new List<ulong>();
            if (type == StreakType.ArtilleryGrenade) list = arGren;
            else if (type == StreakType.AirstrikeGrenade) list = asGren;
            else if (type == StreakType.SquadStrikeGrenade) list = ssGren;
            else if (type == StreakType.HelicopterGrenade) list = heGren;
            else if (type == StreakType.TurretDrop) list = turret;
            player.inventory.GiveItem(GiveSupplySignal());
            if (list != null) list.Add(player.userID);
        }

        #region airstrike
        private void CallAirstrike(Vector3 target, bool type = true)
        {
            if (Airstrike)
            {                
                if (type) Airstrike?.Call("callStrike", target, 140 );
                else Airstrike?.Call("massStrike", target, 140 );
            }
            else Puts(lang.GetMessage("noAirstrike", this));
        }
        #endregion

        #region artillery
        private void LaunchArtillery(Vector3 target)
        {            
            timer.Repeat(rocketInterval, rocketAmount, () => RocketSpread(target));
        }
        private void RocketSpread(Vector3 targetPos)
        {
            targetPos = Quaternion.Euler(UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f), UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f), UnityEngine.Random.Range((float)(-rocketSpread * 0.2), rocketSpread * 0.2f)) * targetPos;
            CreateRocket(targetPos);
        }
        private BaseEntity CreateRocket(Vector3 targetPos)
        {
            string fireRocket = "ammo.rocket.fire";
            string rocketType = "ammo.rocket.basic";
            var rocket = rocketType;
            int rand = UnityEngine.Random.Range(1, 7);
                if (rand == 1)
                    rocket = fireRocket;

            var launchPos = targetPos + new Vector3(0, 200, 0);

            ItemDefinition projectileItem = ItemManager.FindItemDefinition(rocket);
            ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();

            BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, launchPos, new Quaternion(), true);

            TimedExplosive rocketExplosion = entity.GetComponent<TimedExplosive>();
            ServerProjectile rocketProjectile = entity.GetComponent<ServerProjectile>();

            rocketExplosion.timerAmountMin = 60;
            rocketExplosion.timerAmountMax = 60;

            Vector3 newDirection = (targetPos - launchPos);

            entity.SendMessage("InitializeVelocity", (newDirection));
            entity.Spawn();

            return null;
        }
        #endregion

        #region helicopters

        private int HeliDistance = 50;
        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");
                
        private void CallHeli(Vector3 pos, int streaknum, bool onSmoke = false, BasePlayer player = null)
        {
            int amount = data.killStreaks[streaknum].Amount;
            int i = 0;
            while (i < amount)
            {
                BaseEntity entity = CreateHeli(pos);
                MoveEntity(entity, pos);
                if (!onSmoke)
                    if (player != null)
                        CheckDistance(entity, player);
                i++;
            }
        }
        private BaseEntity CreateHeli(Vector3 pos)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
            if (!entity) return null;
            ((BaseCombatEntity)entity).startHealth = HeliHealth;
            var weakspots = ((BaseHelicopter)entity).weakspots;
            weakspots[0].maxHealth = MainRotorHealth;
            weakspots[0].health = MainRotorHealth;
            weakspots[1].maxHealth = TailRotorHealth;
            weakspots[1].health = TailRotorHealth;
            entity.GetComponent<BaseHelicopter>().maxCratesToSpawn = 2;
            entity.Spawn();
            activeHelis.Add((BaseHelicopter)entity);
            ConVar.PatrolHelicopter.bulletAccuracy = HeliAccuracy;
            entity.GetComponent<PatrolHelicopterAI>().State_Move_Enter(pos + new Vector3(0.0f, 10f, 0.0f));
            return entity;
        }
        private Vector3 calculateSpawnPos(Vector3 arenaPos)
         {
             Vector3 spawnPos = new Vector3(0, 0, 0);
             float randX = RandomRange(SpawnDistance);
             float randZ = RandomRange(SpawnDistance);
             spawnPos.x = arenaPos.x - randX;
             spawnPos.z = arenaPos.z - randZ;

             var ang = UnityEngine.Random.Range(1, 360);
             Vector3 finalPos = GetGroundPosition(spawnPos);
             finalPos.y = finalPos.y + 30;
             finalPos.x = spawnPos.x + SpawnDistance * Mathf.Sin(ang * Mathf.Deg2Rad);
             finalPos.z = spawnPos.z + SpawnDistance * Mathf.Cos(ang * Mathf.Deg2Rad);

             return finalPos;
         }
        private float RandomRange(float distance, float difference = 50)
         {
             float rand = UnityEngine.Random.Range(distance - difference, distance + difference);
             return rand;
         }
        private void MoveEntity(BaseEntity entity, Vector3 pos)
         {
             Vector3 spawnPos = calculateSpawnPos(pos);
             entity.transform.position = spawnPos;
         }
        private void CheckDistance(BaseEntity entity, BasePlayer player)
        {
            if (entity == null || player == null) return;
            if (cachedData.ContainsKey(player.userID))
            {
                var currentPos = entity.transform.position;
                var targetPos = player.transform.position;
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
                }
                timer.Once(7, () => CheckDistance(entity, player));
            }
        }
        static Vector3 GetGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
         {
             RaycastHit hitInfo;

             if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
             {
                 sourcePos.y = hitInfo.point.y;
             }
             sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
             return sourcePos;
         }
        private void KillHeli()
         {
            int i = 0;
            foreach (var heli in activeHelis) { heli.KillMessage(); i++; }
            if (i > 0) Puts("Destroyed {0} KillStreak Helicopters", i);
         }
        #endregion

        #region martyrdom
        private void SetMartyrdom(BasePlayer player) => mrtdm.Add(player.userID);
        
        private void ChooseRandomExp(Vector3 pos)
        {
            int num = UnityEngine.Random.Range(1, 6);
            if (num == 1 || num == 2 || num == 3) dropGrenade(pos);
            else if (num == 4 || num == 5) dropBeancan(pos);
            else if (num == 6) dropExplosive(pos);
        }
        private void dropGrenade(Vector3 deathPos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/weapons/f1 grenade/effects/bounce.prefab", deathPos));
            timer.Once(4f, () =>
            {
                Effect.server.Run("assets/prefabs/weapons/f1 grenade/effects/f1grenade_explosion.prefab", deathPos);
                dealDamage(deathPos, grenadeDamage, grenadeRadius);
            });
        }

        private void dropBeancan(Vector3 deathPos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/weapons/beancan grenade/effects/bounce.prefab", deathPos));
            timer.Once(4f, () =>
            {
                Effect.server.Run("assets/prefabs/weapons/beancan grenade/effects/beancan_grenade_explosion.prefab", deathPos);
                dealDamage(deathPos, beancanDamage, beancanRadius);
            });
        }

        private void dropExplosive(Vector3 deathPos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", deathPos));
            timer.Once(2f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab.prefab", deathPos));
            timer.Once(4f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab.prefab", deathPos));
            timer.Once(6f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab.prefab", deathPos));
            timer.Once(8f, () =>
            {
                Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", deathPos);
                dealDamage(deathPos, explosiveDamage, explosiveRadius);
            });
        }
        #endregion

        #region supplydrop
        private void SendSupplyDrop(BasePlayer player, int streaknum)
        {
            if (player == null) return;
            int amount = data.killStreaks[streaknum].Amount;
            int i = 0;
            while (i < amount)
            {
                SpawnSignal(player);                
                i++;
            }
        }
        private void SpawnSignal(BasePlayer player)
        {
            var pos = player.transform.position;
            Vector3 setPos = pos + new Vector3(RandomRange(10, 5), 200, RandomRange(10, 5));
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/misc/supply drop/supply_drop.prefab", setPos, new Quaternion(), true);
            if (!entity) return;
            Rigidbody body = entity.GetComponent<Rigidbody>();
            body.drag = 0.5f;              
            entity.Spawn();
            timer.Once(13.5f, () => Effect.server.Run("assets/bundled/prefabs/fx/player/groundfall.prefab", pos) );
        }
        #endregion

        #region turret drop
        private void DropTurret(Vector3 pos, BasePlayer player)
        {
            AutoTurret turret = CreateTurret(pos);
            AssignTurretAuth(player, turret);
            player.SendNetworkUpdateImmediate();
        }       
        private AutoTurret CreateTurret(Vector3 targetPos)
        {
            BaseEntity turret = GameManager.server.CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", targetPos, new Quaternion(), true);
            turret.Spawn(); return (AutoTurret)turret;
        }
        private void AssignTurretAuth(BasePlayer player, AutoTurret turret)
        {
            var nearbyFriends = FindNearbyFriends(player);
            if (nearbyFriends != null)
            {
                foreach (var entry in nearbyFriends)
                    if (entry != null)
                        turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID() { userid = entry.userID, username = entry.displayName });
            }
            turret.inventory.AddItem(ItemManager.FindItemDefinition(turretAmmoTypeName), turretAmmoCount);
            turret.InitiateStartup();
            turret.SendNetworkUpdateImmediate();
        }
        #endregion

        #region payment
        private string GiveEconomics(BasePlayer player, int streaknum)
        {
            int amount = data.killStreaks[streaknum].Amount;
            Economics?.Call("Deposit", player.userID, amount);
            return string.Format(lang.GetMessage("coinsActive", this, player.UserIDString), amount);
        }
        private string GiveRP(BasePlayer player, int streaknum)
        {
            int amount = data.killStreaks[streaknum].Amount;
            ServerRewards?.Call("AddPoints", player.userID, amount);
            return string.Format(lang.GetMessage("rpActive", this, player.UserIDString), amount);
        }

        #endregion

        #region damage
        private void dealDamage(Vector3 deathPos, float damage, float radius)
        {
            List<BaseCombatEntity> entitiesClose = new List<BaseCombatEntity>();
            List<BaseCombatEntity> entitiesNear = new List<BaseCombatEntity>();
            List<BaseCombatEntity> entitiesFar = new List<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(deathPos, radius / 3, entitiesClose);
            Vis.Entities<BaseCombatEntity>(deathPos, radius / 2, entitiesNear);
            Vis.Entities<BaseCombatEntity>(deathPos, radius, entitiesFar);

            foreach (BaseCombatEntity entity in entitiesClose)
            {
                entity.Hurt(damage, Rust.DamageType.Explosion, null, true);
            }

            foreach (BaseCombatEntity entity in entitiesNear)
            {
                if (entitiesClose.Contains(entity)) return;
                entity.Hurt(damage / 2, Rust.DamageType.Explosion, null, true);
            }

            foreach (BaseCombatEntity entity in entitiesFar)
            {
                if (entitiesClose.Contains(entity) || entitiesNear.Contains(entity)) return;
                entity.Hurt(damage / 4, Rust.DamageType.Explosion, null, true);
            }
        }
        #endregion

        #endregion

        #region chat commands
        [ChatCommand("ks")]
        void cmdTarget(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendReply(player, fontColor1 + lang.GetMessage("title", this, player.UserIDString) +"</color>");
                BroadcastToPlayer(player, lang.GetMessage("kstop2", this, player.UserIDString), lang.GetMessage("kstop", this, player.UserIDString));
                BroadcastToPlayer(player, lang.GetMessage("kspb2", this, player.UserIDString), lang.GetMessage("kspb", this, player.UserIDString));
                if (isAuth(player))
                {
                    BroadcastToPlayer(player, lang.GetMessage("ksAdd1", this, player.UserIDString), lang.GetMessage("ksAdd", this, player.UserIDString));
                    BroadcastToPlayer(player, lang.GetMessage("ksRem1", this, player.UserIDString), lang.GetMessage("ksRem", this, player.UserIDString));
                    BroadcastToPlayer(player, lang.GetMessage("ksList1", this, player.UserIDString), lang.GetMessage("ksList", this, player.UserIDString));
                    BroadcastToPlayer(player, lang.GetMessage("ksListNum1", this, player.UserIDString), lang.GetMessage("ksListNum", this, player.UserIDString));

                    BroadcastToPlayer(player, lang.GetMessage("kswipe2", this, player.UserIDString), lang.GetMessage("kswipe", this, player.UserIDString));
                }
                return;
            }
            var ID = player.userID;
            switch (args[0].ToLower())
            {
                case "top":
                    if (args.Length >= 1)
                    {                        
                        int amount = 5;
                        if (args.Length >= 2)
                            if (!int.TryParse(args[1], out amount))
                                amount = 5;
                        Dictionary<string, int> top5 = data.killStreakData.OrderByDescending(pair => pair.Value.highestKS).Take(amount).ToDictionary(pair => pair.Value.Name, pair => pair.Value.highestKS);
                        if (top5.Count > 0)
                        {
                            SendReply(player, fontColor1 + lang.GetMessage("title", this, player.UserIDString) + "</color>" + fontColor2 + lang.GetMessage("bestHits", this, player.UserIDString) + "</color>");
                            foreach (var name in top5)
                            {
                                SendReply(player, string.Format(fontColor2 + lang.GetMessage("topList", this, player.UserIDString) + "</color>", name.Key, name.Value));
                            }
                        }
                    }
                    return;
                case "wipe":
                    if (isAuth(player))
                    {
                        data.killStreakData.Clear();
                        SendReply(player, lang.GetMessage("wipe", this, player.UserIDString));
                    }
                    return;                
                case "pb":
                    if (data.killStreakData.ContainsKey(ID))
                        BroadcastToPlayer(player, data.killStreakData[ID].highestKS.ToString(), lang.GetMessage("pb", this, player.UserIDString));                    
                    return;
                case "list":
                    if (isAuth(player))
                    {
                        if (args.Length >= 2)
                        {
                            int i = -1;
                            int.TryParse(args[1], out i);
                            if (i <= 0) { BroadcastToPlayer(player, lang.GetMessage("invKsNum", this, player.UserIDString), lang.GetMessage("ksListNum", this, player.UserIDString)); return; }
                            if (!data.killStreaks.ContainsKey(i)) { SendReply(player, string.Format(fontColor1 + lang.GetMessage("invKey", this, player.UserIDString) + "</color>", i)); return; }
                            BroadcastToPlayer(player, i.ToString(), lang.GetMessage("kills", this, player.UserIDString));
                            BroadcastToPlayer(player, data.killStreaks[i].StreakType.ToString(), lang.GetMessage("type", this, player.UserIDString));
                            BroadcastToPlayer(player, data.killStreaks[i].Amount.ToString(), lang.GetMessage("amount", this, player.UserIDString));
                            string message = data.killStreaks[i].Message;
                            BroadcastToPlayer(player, message, lang.GetMessage("message", this, player.UserIDString));
                            return;
                        }
                        else
                        {
                            BroadcastToPlayer(player, "", lang.GetMessage("regStreaks" , this, player.UserIDString));
                            foreach (var entry in data.killStreaks) BroadcastToPlayer(player, entry.Key.ToString(), "");                            
                        }
                    }
                    return;
                case "add":
                    if (isAuth(player))
                    {
                        if (args.Length >= 3)
                        {                            
                            int i = -1;
                            int.TryParse(args[1], out i);
                            if (i <= 0) { BroadcastToPlayer(player, "", lang.GetMessage("invKillNum", this, player.UserIDString)); return; }
                            if (data.killStreaks.ContainsKey(i))
                            {
                                BroadcastToPlayer(player, "", string.Format(lang.GetMessage("amountUsed", this, player.UserIDString), i));
                                return;
                            }
                            data.killStreaks.Add(i, new Streaks() { Message = args[2], StreakType = StreakType.None, Amount = 0 });
                            
                            if (args.Length >= 4)
                            {
                                int sNum = -1;
                                int.TryParse(args[3], out sNum);
                                if (!streakTypes.ContainsKey(sNum)) { BroadcastToPlayer(player, "", string.Format(lang.GetMessage("invST", this, player.UserIDString), args[3])); return; }
                                data.killStreaks[i].StreakType = streakTypes[sNum];
                                int o = -1;
                                if (args.Length >= 5) int.TryParse(args[4], out o);
                                if (o != -1)
                                    data.killStreaks[i].Amount = o;
                            }
                            BroadcastToPlayer(player, "", string.Format(lang.GetMessage("addSuccess", this, player.UserIDString), i));
                            return;
                        }
                        BroadcastToPlayer(player, "V " + Version, lang.GetMessage("title", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("ksAddForm1", this, player.UserIDString), lang.GetMessage("ksAddForm", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("kNum1", this, player.UserIDString), lang.GetMessage("kNum", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("kMes1", this, player.UserIDString), lang.GetMessage("kMes", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("kTyp1", this, player.UserIDString), lang.GetMessage("kTyp", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("kAmo1", this, player.UserIDString), lang.GetMessage("kAmo", this, player.UserIDString));
                        BroadcastToPlayer(player, lang.GetMessage("showTypes1", this, player.UserIDString), lang.GetMessage("showTypes", this, player.UserIDString));
                    }
                    return;
                case "show":
                    if (isAuth(player))
                    {
                        BroadcastToPlayer(player, lang.GetMessage("availTypes", this, player.UserIDString), lang.GetMessage("title", this, player.UserIDString));
                        foreach (var entry in streakTypes)
                        {
                            BroadcastToPlayer(player, entry.Value.ToString(), entry.Key.ToString());
                        }
                    }
                    return;
                case "remove":
                    if (isAuth(player))
                    {
                        if (args.Length >= 2)
                        {
                            int i = -1;
                            int.TryParse(args[1], out i);
                            if (i == -1) { BroadcastToPlayer(player, lang.GetMessage("invKSNum", this, player.UserIDString), lang.GetMessage("ksRem", this, player.UserIDString)); return; }
                            if (!data.killStreaks.ContainsKey(i)) { BroadcastToPlayer(player, "", string.Format(lang.GetMessage("invKey", this, player.UserIDString), i)); return; }
                            data.killStreaks.Remove(i);                           
                            
                            BroadcastToPlayer(player, "", string.Format(lang.GetMessage("remKS", this, player.UserIDString), i.ToString()));
                            SaveData();
                        }
                    }
                    return;
                case "strike":
                    
                    if (asGren.Contains(ID))
                    {
                        if (!activeGrenades.ContainsKey(ID))
                            activeGrenades.Add(ID, StreakType.AirstrikeGrenade);
                        else
                            activeGrenades[ID] = StreakType.AirstrikeGrenade;
                        asGren.Remove(ID);
                       BroadcastToPlayer(player, "", lang.GetMessage("asActive", this, player.UserIDString));
                    }
                    return;
                case "squad":                    
                    if (ssGren.Contains(ID))
                    {
                        if (!activeGrenades.ContainsKey(ID))
                            activeGrenades.Add(ID, StreakType.SquadStrikeGrenade);
                        else
                            activeGrenades[ID] = StreakType.SquadStrikeGrenade;
                        ssGren.Remove(ID);
                        BroadcastToPlayer(player, "", lang.GetMessage("ssActive", this, player.UserIDString));
                    }
                    return;
                case "art":
                    if (arGren.Contains(ID))
                    {
                        if (!activeGrenades.ContainsKey(ID))
                            activeGrenades.Add(ID, StreakType.ArtilleryGrenade);
                        else
                            activeGrenades[ID] = StreakType.ArtilleryGrenade;
                        arGren.Remove(ID);
                        BroadcastToPlayer(player, "", lang.GetMessage("arActive", this, player.UserIDString));
                    }
                    return;
                case "heli":
                    if (heGren.Contains(ID))
                    {
                        if (!activeGrenades.ContainsKey(ID))
                            activeGrenades.Add(ID, StreakType.HelicopterGrenade);
                        else
                            activeGrenades[ID] = StreakType.HelicopterGrenade;
                        heGren.Remove(ID);
                        BroadcastToPlayer(player, "", lang.GetMessage("heActive", this, player.UserIDString));
                    }
                    return;
                case "turret":
                    if (turret.Contains(ID))
                    {
                        if(!activeGrenades.ContainsKey(ID))
                            activeGrenades.Add(ID, StreakType.TurretDrop);
                        else
                            activeGrenades[ID] = StreakType.TurretDrop;
                        turret.Remove(ID);
                        BroadcastToPlayer(player, "", lang.GetMessage("tuActive", this, player.UserIDString));
                    }
                    return;                
            }
        }
        
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection != null)
                if (player.net.connection.authLevel < 1)
                    return false;
            return true;
        }
        
        #endregion

        #region gui

        class KSUI : MonoBehaviour
        {
            int i;

            private BasePlayer player;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                i = 0;
            }

            public static KSUI GetPlayer(BasePlayer player)
            {
                KSUI p = player.GetComponent<KSUI>();
                if (p == null) p = player.gameObject.AddComponent<KSUI>();
                return p;
            }
            
            public void UseUI(string msg, Vector2 pos, Vector2 dim, int size = 18)
            {
                i++;
                string uiNum = i.ToString();
                     
                Vector2 posMin = pos;
                Vector2 posMax = posMin + dim;

                var elements = new CuiElementContainer();
                CuiElement textElement = new CuiElement
                {
                    Name = uiNum,
                    Parent = "Overlay",
                    FadeOut = 0.3f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = msg,
                            FontSize = size,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = 0.3f
                        },
                        new CuiOutlineComponent
                        {
                            Distance = "1.0 1.0",
                            Color = "0.0 0.0 0.0 1.0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = posMin.x + " " + posMin.y,
                            AnchorMax = posMax.x + " " + posMax.y
                        }
                    }
                };
                elements.Add(textElement);
                CuiHelper.AddUi(player, elements);
                Interface.GetMod().CallHook("DestroyWarningMsg", new object[] { player, uiNum, 5 });
            }
        }
        private void DestroyNotification(BasePlayer player, string msgNum)
        {
            bool t = CuiHelper.DestroyUi(player, msgNum);
            if (!t) DestroyNotification(player, msgNum);
        }
        private void DestroyWarningMsg(BasePlayer player, string msgNum, int duration)
        {
            timer.Once(duration, () => DestroyNotification(player, msgNum));
        }
        #endregion

        #region config

        bool Changed;

        static bool useFriendsAPI = true;
        static bool useClans = true;
        static bool useAirstrike = true;
        static bool broadcastMsg = true;
        static bool ignoreBuildPriv = false;
        static bool broadcastEnd = true;

        static int saveTimer = 10;

        static float HeliBulletDamage = 3.0f;
        static float HeliHealth = 4000.0f;
        static float MainRotorHealth = 400.0f;
        static float TailRotorHealth = 250.0f;
        static float HeliSpeed = 30.0f;
        static float HeliAccuracy = 6.0f;
        static float SpawnDistance = 500f;

        static string fontColor1 = "<color=orange>";
        static string fontColor2 = "<color=#939393>";

        static float rocketInterval = 0.5f;
        static float rocketSpread = 6.0f;
        static int rocketAmount = 20;

        static float grenadeRadius = 5f;
        static float grenadeDamage = 75f;
        static float beancanRadius = 4f;
        static float beancanDamage = 30f;
        static float explosiveRadius = 10f;
        static float explosiveDamage = 110f;

        static float nearbyRadius = 50f;
        static string turretAmmoTypeName = "ammo.rifle";
        static int turretAmmoCount = 1000;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfgFloat("Helicopter - Bullet damage", ref HeliBulletDamage);
            CheckCfgFloat("Helicopter - Health", ref HeliHealth);
            CheckCfgFloat("Helicopter - Mail rotor health", ref MainRotorHealth);
            CheckCfgFloat("Helicopter - Tail rotor health", ref TailRotorHealth);
            CheckCfgFloat("Helicopter - Speed", ref HeliSpeed);
            CheckCfgFloat("Helicopter - Accuracy", ref HeliAccuracy);
            CheckCfgFloat("Helicopter - Spawn distance (away from player)", ref SpawnDistance);

            CheckCfgFloat("Artillery - Rocket interval", ref rocketInterval);
            CheckCfgFloat("Artillery - Rocket spread", ref rocketSpread);
            CheckCfg("Artillery - Rocket amount", ref rocketAmount);

            CheckCfgFloat("Martyrdom - Explosive radius - Grenade", ref grenadeRadius);
            CheckCfgFloat("Martyrdom - Explosive radius - Beancan", ref beancanRadius);
            CheckCfgFloat("Martyrdom - Explosive radius - Explosive", ref explosiveRadius);
            CheckCfgFloat("Martyrdom - Explosive damage - Grenade", ref grenadeDamage);
            CheckCfgFloat("Martyrdom - Explosive damage - Beancan", ref beancanDamage);
            CheckCfgFloat("Martyrdom - Explosive damage - Explosive", ref explosiveDamage);

            CheckCfgFloat("TurretDrop - Auto-authorize radius", ref nearbyRadius);
            CheckCfg("TurretDrop - Ammunition type shortname", ref turretAmmoTypeName);
            CheckCfg("TurretDrop - Ammunition amount", ref turretAmmoCount);

            CheckCfg("Options - Use FriendsAPI", ref useFriendsAPI);
            CheckCfg("Options - Use Clans", ref useClans);
            CheckCfg("Options - Use Airstrike", ref useAirstrike);
            CheckCfg("Options - Ignore kills in building privilege", ref ignoreBuildPriv);
            
            CheckCfg("Options - Data save timer", ref saveTimer);

            CheckCfg("Messages - Broadcast streak message", ref broadcastMsg);
            CheckCfg("Messages - Broadcast streak end", ref broadcastEnd);
            CheckCfg("Messages - Message color", ref fontColor2);
            CheckCfg("Messages - Main color", ref fontColor1);
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
        #endregion

        #region classes and data storage
        void SaveData()
        {
            foreach (var entry in cachedData)
            {
                var d = data.killStreakData;
                if (!d.ContainsKey(entry.Key))
                    d.Add(entry.Key, new KSDATA());
                if (d[entry.Key].highestKS < entry.Value)
                    d[entry.Key].highestKS = entry.Value;
            }
            KSData.WriteObject(data);
        }
        void SaveLoop()
        {
            SaveData();
            timer.Once(saveTimer * 60, () => SaveData());
        }
        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("killstreak_data");
            }
            catch
            {
                data = new DataStorage();
                data.killStreaks = ksDefault;
            }
            if (data.killStreaks == null || data.killStreaks.Count < 1) data.killStreaks = ksDefault;
            timer.Once(saveTimer, () => SaveLoop());
        }
        void RegisterMessages() => lang.RegisterMessages(messages, this);       
                
        class DataStorage
        {
            public Dictionary<ulong, KSDATA> killStreakData = new Dictionary<ulong, KSDATA>();
            public Dictionary<int, Streaks> killStreaks = new Dictionary<int, Streaks>();
            public DataStorage() { }
        }
        class KSDATA
        {
            public string Name;
            public int highestKS = 0;
        }       
        class Streaks
        {
            public string Message;
            public StreakType StreakType;
            public int Amount = 1;
        }
        enum StreakType
        {
            None,
            Airstrike,
            SquadStrike,
            Artillery,
            Helicopter,
            SupplyDrop,
            AirstrikeGrenade,
            SquadStrikeGrenade,
            ArtilleryGrenade,
            HelicopterGrenade,
            Martyrdom,
            TurretDrop,
            Coins,
            RP
        }
        #endregion

        #region defaultks

        Dictionary<int, Streaks> ksDefault = new Dictionary<int, Streaks>()
        {
            {5, new Streaks() {StreakType = StreakType.SupplyDrop, Message = " is on a killing spree!" } },
            {10, new Streaks() {StreakType = StreakType.ArtilleryGrenade, Message = " is on a kill frenzy!" } },
            {15, new Streaks() {StreakType = StreakType.Martyrdom, Message = " is running riot!" } },
            {20, new Streaks() {StreakType = StreakType.Helicopter, Amount = 1, Message = " is on a rampage!" } },
            {25, new Streaks() {StreakType = StreakType.HelicopterGrenade, Message = " is untouchable!"} },
            {30, new Streaks() {StreakType = StreakType.SquadStrike, Amount = 1, Message = " is invincible!" } },
            {35, new Streaks() {StreakType = StreakType.SupplyDrop, Amount = 3, Message = " is a god!" } }            
        };

        #endregion

        #region message
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"title", "Killstreaks: "},
            {"aheli", "a helicopter" },
            {"aanim", "a animal" },
            {"abt", "a bear trap" },
            {"aldm", "a landmine" },
            {"flrsp", "floor spikes" },
            {"aturr", "a turret" },
            {"awall", "a wall" },
            {"endstreak", "'s killstreak has been ended by " },
            {"suic", "suicide" },
            {"disconnected", "disconnection" },
            {"attract", "Your killstreak has attracted attention!" },
            {"warning", "WARNING! " },
            {"pb", "Highest kill streak: " },
            {"kstop", "/ks top" },
            {"kswipe", "/ks wipe" },
            {"kspb", "/ks pb" },
            {"kswipe2", "- Clears all Killstreak data" },
            {"kstop2", "- Displays top Killstreaks" },
            {"kspb2", "- Shows your personal best Killstreak" },
            {"topList", "{0} : {1} kills" },
            {"bestHits", "Top killstreaks" },
            {"noAirstrike", "Airstrike is not installed, unable to send strike" },
            {"ksAdd", "/ks add" },
            {"amountUsed", "You already have a killstreak set for {0} kills" },
            {"ksRem", "/ks remove ##" },
            {"ksList", "/ks list" },
            {"ksListNum", "/ks list ##" },
            {"ksAdd1", " - Displays the required format to add a kill streak" },
            {"ksRem1", " - Remove a kill streak" },
            {"ksList1", " - List current kills" },
            {"ksListNum1", " - Display kill streak information for <killnumber>" },
            {"invKSNum", " - You must enter a number." },
            {"invKey", "You do not have a killstreak set to {0} kills" },
            {"kills", "Kills: " },
            {"type", "Type: " },
            {"amount", "Amount: " },
            {"message", "Message: " },
            {"regStreaks", "You have kill streaks registered to the following kills:" },
            {"invKillNum", "You must enter a number of kills!" },
            {"invST", "{0} is a invalid Streak type!" },
            {"addSuccess", "You have successfully registered a kill streak for {0} kills" },
            { "ksAddForm", "/ks add <killnumber> <message> <opt:type> <opt:amount>" },
            { "ksAddForm1", " - Kill number and message are required!" },
            { "kNum", "<killnumber>" },
            { "kNum1", " - The amount of kills required to activate the streak" },
            { "kMes", "<message>" },
            { "kMes1", " - The message that will be globally broadcast" },
            { "kTyp", "<opt:type>" },
            { "kTyp1", " - (Optional) Type of streak" },
            { "kAmo", "<opt:amount>" },
            { "kAmo1", " - (Optional) Amount of times the type will be called" },
            { "remKS", "You have removed the kill streak for {0}" },
            { "showTypes", "/ks show" },
            { "showTypes1", " - Show available killstreak types and their ID" },
            { "availTypes", " - Available streak types:" },
            { "asGrenade", "You have been given a Airstrike signal. Activate it with /ks strike" },
            { "ssGrenade", "You have been given a SquadStrike signal. Activate it with /ks squad" },
            { "arGrenade", "You have been given a Artillery signal. Activate it with /ks art" },
            { "heGrenade", "You have been given a Helicopter signal. Activate it with /ks heli" },
            { "tuGrenade", "You have been given a Turret Drop signal. Activate it with /ks turret" },
            { "mrtdmActive", "You have earnt the perk Martyrdom. When you die next you will drop a random explosive!" },
            { "asLaunch", "An Airstrike has been launched at your position!" },
            { "arLaunch", "An Artillery strike has been launched at your position!" },
            { "ssLaunch", "An SquadStrike has been launched at your position!" },
            { "heLaunch", "An helicopter has been sent to your position!" },
            { "sdLaunch", "A supply drop is inbound on your position!" },
            { "asActive", "You have activated your Airstrike, throw the supply signal to call it" },
            { "ssActive", "You have activated your SquadStrike, throw the supply signal to call it" },
            { "arActive", "You have activated your Artillery Strike, throw the supply signal to launch" },
            { "heActive", "You have activated your Helicopter Strike, throw the supply signal to call it" },
            { "tuActive", "You have activated your Turret Drop, throw the supply signal to call it" },
            { "coinsActive", "You have earnt {0} coins" },
            { "rpActive", "You have earnt {0} RP"},
            { "messageCheck", "1234" }
        };
        #endregion


    }
}
