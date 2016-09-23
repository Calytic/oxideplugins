using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;
using Rust;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("RaidNotes", "Calytic", "0.0.1")]
    [Description("Broadcasts raid activity to chat")]
    public class RaidNotes : RustPlugin
    {
        #region Variables

        private int blockLayer = UnityEngine.LayerMask.GetMask(new string[] { "Player (Server)" });
        private Dictionary<string, int> reverseItems = new Dictionary<string, int>();
        private Dictionary<Raid, Timer> timers = new Dictionary<Raid, Timer>();

        private bool checkEntityDamage = true;
        private bool checkEntityDeath = true;

        private float raidDuration = 300f;
        private float raidDistance = 50f;

        private bool announceRaidStart = false;
        private bool announceRaidEnd = false;

        private bool announceGlobal = true;
        private bool announceClan = false;
        private bool printToLog = true;

        private bool useClans = false;

        private string announcePrefixColor = "orange";
        private string announceIcon;
        private string announceNameColor = "lightblue";
        private string announceClanColor = "#00eaff";
        private string announceWeaponColor = "#666666";

        private float announceDelay = 0f;
        private float announceRadius = 0f;

        internal int announceMinParticipants = 0;
        internal int announceMinWeapons = 0;
        internal int announceMinKills = 0;
        internal int announceMinMinutes = 0;

        [PluginReference]
        Plugin Clans;

        List<string> prefabs = new List<string>()
        {
            "door.hinged",
            "door.double.hinged",
            "window.bars",
            "floor.ladder.hatch",
            "floor.frame",
            "wall.frame",
            "shutter"
        };

        #endregion

        #region Classes
        public class AttackVector
        {
            public Vector3 vector;
            public ulong attacker;
            public uint weapon;

            public AttackVector(ulong attacker, uint weapon, Vector3 vector) {
                this.attacker = attacker;
                this.vector = vector;
                this.weapon = weapon;
            }
        }

        public class Raid
        {
            RaidNotes plugin;
            public DateTime start = DateTime.Now;
            public DateTime end;
            public Vector3 firstDamage;
            public Vector3 lastDamage;
            public ulong initiator;
            public ulong victim;
            public List<ulong> blockOwners = new List<ulong>();
            public List<ulong> participants = new List<ulong>();
            public int lastWeapon;
            public Dictionary<int, int> weapons = new Dictionary<int, int>();
            public Dictionary<ulong, Dictionary<ulong, int>> kills = new Dictionary<ulong, Dictionary<ulong, int>>();

            public IPlayer Initiator
            {
                get
                {
                    return plugin.covalence.Players.GetPlayer(initiator.ToString());
                }
            }

            public IPlayer Victim
            {
                get
                {
                    return plugin.covalence.Players.GetPlayer(victim.ToString());
                }
            }

            public Raid(RaidNotes plugin, ulong initiator, ulong victim, Vector3 firstDamage)
            {
                this.plugin = plugin;
                this.initiator = initiator;
                this.victim = victim;
                this.firstDamage = firstDamage;
            }

            public bool IsAnnounced()
            {
                if (participants.Count < plugin.announceMinParticipants) return false;
                if (kills.Count < plugin.announceMinKills) return false;
                if (weapons.Count < plugin.announceMinWeapons) return false;

                TimeSpan ts = end - start;
                if (ts.TotalMinutes < plugin.announceMinMinutes) return false;

                return true;
            }

            internal JObject Vector2JObject(Vector3 vector)
            {
                JObject obj = new JObject();
                obj.Add("x", vector.x);
                obj.Add("y", vector.y);
                obj.Add("z", vector.z);

                return obj;
            }

            internal JObject ToJObject()
            {
                var obj = new JObject();

                obj["start"] = start;
                obj["end"] = end;
                var explosions = new JObject();
                explosions.Add("first", Vector2JObject(firstDamage));
                explosions.Add("last", Vector2JObject(lastDamage));
                obj["explosions"] = explosions;

                obj["initiator"] = initiator;
                obj["victim"] = victim;

                JArray owners = new JArray();
                foreach (ulong owner in blockOwners)
                {
                    owners.Add(owner);
                }

                obj["owners"] = owners;

                JArray participantsData = new JArray();
                foreach (ulong participant in participants)
                {
                    participantsData.Add(participant);
                }

                obj["participants"] = participantsData;


                JObject weaponsData = new JObject();
                foreach (KeyValuePair<int, int> kvp in weapons)
                {
                    weaponsData.Add(kvp.Key.ToString(), kvp.Value);
                }
                obj["weapons"] = weaponsData;

                JObject killData = new JObject();
                foreach (KeyValuePair<ulong, Dictionary<ulong, int>> kvp in kills)
                {
                    JObject killPlayerList = new JObject();
                    foreach (KeyValuePair<ulong, int> kvp2 in kvp.Value)
                    {
                        killPlayerList.Add(kvp2.Key.ToString(), kvp2.Value);
                    }

                    killData.Add(kvp.Key.ToString(), killPlayerList);
                }

                obj["kills"] = killData;

                return obj;
            }

            internal void OnEnded() 
            {
                Interface.CallHook("OnRaidEnded", ToJObject());
            }

            internal void OnStarted()
            {
                Interface.CallHook("OnRaidStarted", ToJObject());
            }
        }

        public class RaidBehavior : MonoBehaviour
        {
            public BasePlayer player;
            public Raid raid;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
            }
        }

        #endregion

        #region Initialization

        protected override void LoadDefaultConfig()
        {
            PrintToConsole("Creating new configuration");
            Config.Clear();

            Config["checkEntityDamage"] = true;
            Config["checkEntityDeath"] = true;

            Config["raidDistance"] = 50f;
            Config["raidDuration"] = 300f;
            Config["announceRaidEnd"] = false;
            Config["announceRaidStart"] = false;

            Config["announceGlobal"] = false;
            Config["announceClan"] = true;
            Config["printToLog"] = true;

            Config["announceIcon"] = 0;
            Config["announcePrefixColor"] = "orange";
            Config["announceNameColor"] = "lightblue";
            Config["announceClanColor"] = "#00eaff";
            Config["announceWeaponColor"] = "#666666";
            Config["announceDelay"] = 0f;
            Config["announceRadius"] = 0f;

            Config["announceMinParticipants"] = 0;
            Config["announceMinWeapons"] = 0;
            Config["announceMinKills"] = 0;
            Config["announceMinMinutes"] = 0;

            Config["useClans"] = false;
        }

        void Loaded()
        {
            CheckConfig();
            LoadMessages();

            raidDistance = GetConfig("raidDistance", 50f);
            raidDuration = GetConfig("raidDuration", 300f);

            checkEntityDamage = GetConfig("checkEntityDamage", true);
            checkEntityDeath = GetConfig("checkEntityDeath", true);

            announceGlobal = GetConfig("announceGlobal", false);
            announceClan = GetConfig("announceClan", true);
            printToLog = GetConfig("printToLog", true);

            announceRaidEnd = GetConfig("announceRaidEnd", false);
            announceRaidStart = GetConfig("announceRaidStart", false);
            announceDelay = GetConfig("announceDelay", 0f);
            announceRadius = GetConfig("announceRadius", 0f);

            announceMinParticipants = GetConfig("announceMinParticipants", 0);
            announceMinWeapons = GetConfig("announceMinWeapons", 0);
            announceMinKills = GetConfig("announceMinKills", 0);
            announceMinMinutes = GetConfig("announceMinMinutes", 0);

            useClans = GetConfig("useClans", false);

            announceIcon = GetConfig("announceIcon", "0");
            announceClanColor = GetConfig("announcePrefixColor", "orange");
            announceNameColor = GetConfig("announceNameColor", "lightblue");
            announceClanColor = GetConfig("announceClanColor", "#00eaff");
            announceWeaponColor = GetConfig("announceWeaponColor", "#666666");

            foreach (ItemDefinition def in ItemManager.itemList)
            {
                var modEntity = def.GetComponent<ItemModEntity>();
                if (modEntity != null)
                {
                    var prefab = modEntity.entityPrefab.Get();
                    var thrownWeapon = prefab.GetComponent<ThrownWeapon>();

                    if (thrownWeapon != null && !reverseItems.ContainsKey(thrownWeapon.prefabToThrow.resourcePath))
                    {
                        reverseItems.Add(thrownWeapon.prefabToThrow.resourcePath, def.itemid);
                    }
                }
            }

            if (useClans || announceClan)
            {
                if (!plugins.Exists("Clans"))
                {
                    useClans = false;
                    announceClan = false;
                    PrintWarning("Clans not found! useClans and announceClan disabled. Cannot use without this plugin. http://oxidemod.org/plugins/rust-io-clans.842/");
                }
            }
        }

        void CheckConfig()
        {
            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", "") != Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            // END NEW CONFIGURATION OPTIONS

            PrintToConsole("Upgrading configuration file");
            SaveConfig();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Announce: Prefix", "Raid"},
                {"Announce: Start", "{initiatorClan} {initiator} ({initiatorClanMates}) is raiding {victimClan} {victim} ({victimClanMates})"},
                {"Announce: End", "{initiatorClan} {initiator} ({initiatorClanMates}) raided {victimClan} {victim} ({victimClanMates}) with {weaponList}"},
            }, this);
        }

        void Unload()
        {
            var objects = GameObject.FindObjectsOfType(typeof(RaidBehavior));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);

        }

        #endregion

        #region Oxide Hooks

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!checkEntityDamage) return;
            if (hitInfo == null || 
                hitInfo.Initiator == null ||
                hitInfo.WeaponPrefab == null ||
                !IsEntityRaidable(entity)) 
                return;

            DamageType majorityDamageType = hitInfo.damageTypes.GetMajorityDamageType();
            
            string prefabName = hitInfo.WeaponPrefab.PrefabName;
            if (reverseItems.ContainsKey(prefabName))
            {
                switch (majorityDamageType)
                {
                    case DamageType.Explosion:
                    case DamageType.Heat:
                        StructureAttack(entity, hitInfo.Initiator, reverseItems[prefabName], hitInfo.HitPositionWorld);
                        break;
                }
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!checkEntityDeath) return;
            if (hitInfo == null) {
                return;
            }

            if (hitInfo.WeaponPrefab != null && hitInfo.Initiator != null)
            {
                BasePlayer initiator = hitInfo.Initiator.ToPlayer();

                if(initiator is BasePlayer) {
                    if (IsEntityRaidable(entity))
                    {
                        DamageType majorityDamageType = hitInfo.damageTypes.GetMajorityDamageType();

                        string prefabName = hitInfo.WeaponPrefab.PrefabName;

                        if (reverseItems.ContainsKey(prefabName))
                        {
                            switch (majorityDamageType)
                            {
                                case DamageType.Explosion:
                                case DamageType.Heat:
                                    StructureAttack(entity, initiator, reverseItems[prefabName], hitInfo.HitPositionWorld);
                                    break;
                            }
                        }
                    }
                    else if (entity is BasePlayer)
                    {
                        BasePlayer player = entity.ToPlayer();
                        RegisterKill(player, initiator);
                    }
                }
            }
        }

        #endregion

        #region Core Methods

        void RegisterKill(BasePlayer player, BasePlayer attacker)
        {
            RaidBehavior behavior = player.GetComponent<RaidBehavior>();
            if (behavior != null && behavior.raid != null)
            {
                Dictionary<ulong, int> kills;
                if (behavior.raid.kills.TryGetValue(attacker.userID, out kills))
                {
                    if (!kills.ContainsKey(player.userID))
                    {
                        kills.Add(player.userID, 1);
                    }
                    else
                    {
                        kills[player.userID]++;
                    }
                }
                else
                {
                    behavior.raid.kills.Add(attacker.userID, new Dictionary<ulong, int>() { { player.userID, 1 } });
                }

                GameObject.Destroy(player.GetComponent<RaidBehavior>());
            }
        }

        void StructureAttack(BaseEntity targetEntity, BaseEntity sourceEntity, int weapon, UnityEngine.Vector3 hitPosition)
        {
            BasePlayer source;

            if (sourceEntity.ToPlayer() is BasePlayer)
            {
                source = sourceEntity.ToPlayer();
            }
            else
            {
                string ownerID = (sourceEntity.OwnerID == 0) ? sourceEntity.OwnerID.ToString() : string.Empty;
                if (!string.IsNullOrEmpty(ownerID))
                {
                    source = BasePlayer.Find(ownerID);
                }
                else
                {
                    return;
                }
            }

            if (source == null)
            {
                return;
            }

            string targetID = (targetEntity.OwnerID > 0) ? targetEntity.OwnerID.ToString() : string.Empty;
            if (!string.IsNullOrEmpty(targetID) && targetID != source.UserIDString)
            {
                ulong targetIDUint = Convert.ToUInt64(targetID);
                IPlayer target = covalence.Players.GetPlayer(targetID);
                Raid raid;
                bool raidFound = TryGetRaid(source, targetIDUint, targetEntity.transform.position, out raid);
                raid.lastWeapon = weapon;
                if (raid.weapons.ContainsKey(weapon))
                {
                    raid.weapons[weapon]++;
                }
                else
                {
                    raid.weapons.Add(weapon, 1);
                }

                if (raid.blockOwners.Count == 0)
                {
                    raid.victim = targetIDUint;
                }

                if (!raid.blockOwners.Contains(targetIDUint))
                {
                    raid.blockOwners.Add(targetIDUint);
                }

                if (!raidFound)
                {
                    if (announceRaidStart)
                    {
                        if (announceDelay > 0)
                        {
                            timer.In(announceDelay, delegate()
                            {
                                AnnounceRaid(raid, GetMsg("Announce: Start"));
                            });
                        }
                        else
                        {
                            AnnounceRaid(raid, GetMsg("Announce: Start"));
                        }
                    }
                }
            }
        }

        bool TryGetRaid(BasePlayer source, ulong victim, UnityEngine.Vector3 position, out Raid raid)
        {
            List<BasePlayer> nearbyTargets = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(position, raidDistance, nearbyTargets, blockLayer);
            nearbyTargets = Sort(position, nearbyTargets);


            List<Raid> existingRaids = new List<Raid>();

            RaidBehavior sourceBehavior = source.GetComponent<RaidBehavior>();

            if (sourceBehavior != null && sourceBehavior.raid != null)
            {
                existingRaids.Add(sourceBehavior.raid);
            }

            if (existingRaids.Count == 0 && nearbyTargets.Count > 0)
            {
                foreach (BasePlayer nearbyTarget in nearbyTargets)
                {
                    RaidBehavior behavior = nearbyTarget.GetComponent<RaidBehavior>();
                    if (behavior != null && behavior.raid != null)
                    {
                        if(!existingRaids.Contains(behavior.raid)) {
                            existingRaids.Add(behavior.raid);
                        }
                    }
                }
            }

            bool found = true;

            if (existingRaids.Count == 0)
            {
                found = false;
                Raid newRaid = StartRaid(source, victim, position);
                existingRaids.Add(newRaid);
            }
            else if (sourceBehavior == null || (sourceBehavior != null && sourceBehavior.raid == null))
            {
                AddToRaid(source, existingRaids[0]);
            }

            if (nearbyTargets.Count > 0)
            {
                foreach (BasePlayer nearbyTarget in nearbyTargets)
                {
                    RaidBehavior behavior = nearbyTarget.GetComponent<RaidBehavior>();
                    if (behavior == null || (behavior != null && behavior.raid == null))
                    {
                        AddToRaid(nearbyTarget, existingRaids[0]);
                    }
                }
            }

            RefreshRaid(existingRaids[0]);

            raid = existingRaids[0];
            return found;
        }

        public Raid StartRaid(BasePlayer source, ulong victim, Vector3 position)
        {
            Raid raid = new Raid(this, source.userID, victim, position);

            RefreshRaid(raid);

            AddToRaid(source, raid);

            raid.OnStarted();

            return raid;
        }

        public void RefreshRaid(Raid raid)
        {
            DestroyTimer(raid);

            timers.Add(raid, timer.In(raidDuration, delegate()
            {
                StopRaid(raid);
                if (announceRaidEnd)
                {
                    if (announceDelay > 0)
                    {
                        timer.In(announceDelay, delegate()
                        {
                            AnnounceRaid(raid, GetMsg("Announce: End"));
                        });
                    }
                    else
                    {
                        AnnounceRaid(raid, GetMsg("Announce: End"));
                    }
                }
            }));
        }

        public void DestroyTimer(Raid raid)
        {
            Timer raidTimer;
            if (timers.TryGetValue(raid, out raidTimer))
            {
                if (!raidTimer.Destroyed)
                {
                    raidTimer.Destroy();
                }

                timers.Remove(raid);
            }
        }

        public void StopRaid(Raid raid)
        {
            foreach (ulong part in raid.participants)
            {
                BasePlayer partPlayer = BasePlayer.FindByID(part);
                if (partPlayer != null && partPlayer.GetComponent<RaidBehavior>() != null)
                {
                    GameObject.Destroy(partPlayer.GetComponent<RaidBehavior>());
                }
            }

            DestroyTimer(raid);

            raid.end = DateTime.Now;
            raid.OnEnded();
            
        }

        void AddToRaid(BasePlayer player, Raid raid)
        {
            RaidBehavior behavior = player.gameObject.AddComponent<RaidBehavior>();
            behavior.raid = raid;
            if (!behavior.raid.participants.Contains(player.userID))
            {
                behavior.raid.participants.Add(player.userID);
            }
        }

        void AnnounceRaid(Raid raid, string format)
        {
            string initiatorClanTag = "";
            string victimClanTag = "";
            string initiatorText = raid.Initiator.Name;
            string victimText = raid.Victim.Name;
            string initiatorClanText = "";
            string victimClanText = "";
            string initiatorClanMatesText = "1";
            string victimClanMatesText = "1";

            if(useClans) {

                initiatorClanTag = Clans.Call<string>("GetClanOf", raid.initiator);
                victimClanTag = Clans.Call<string>("GetClanOf", raid.victim);

                if (initiatorClanTag != null)
                {
                    initiatorClanText = string.Format("<color={0}>{1}</color>", announceClanColor, initiatorClanTag);
                    initiatorClanMatesText = GetClanMembers(initiatorClanTag).Count.ToString();
                }

                if (victimClanTag != null)
                {
                    victimClanText = string.Format("<color={0}>{1}</color>", announceClanColor, victimClanTag);
                    victimClanMatesText = GetClanMembers(victimClanTag).Count.ToString();
                }
            } 

            initiatorText = string.Format("<color={0}>{1}</color>", announceNameColor, initiatorText);
            victimText = string.Format("<color={0}>{1}</color>", announceNameColor, victimText);

            string announcePrefix = string.Format("<color={0}>{1}</color>", announcePrefixColor, GetMsg("Announce: Prefix"));

            string weaponNameText = "";

            ItemDefinition weaponItem = ItemManager.FindItemDefinition(raid.lastWeapon);
            if (weaponItem is ItemDefinition)
            {
                weaponNameText = weaponItem.displayName.english;
                weaponNameText = string.Format("<color={0}>{1}(s)</color>", announceWeaponColor, weaponNameText);
            }

            string weaponsNameText = "";

            List<string> weaponsList = new List<string>();
            foreach (KeyValuePair<int, int> kvp in raid.weapons)
            {
                ItemDefinition weaponsItem = ItemManager.FindItemDefinition(kvp.Key);
                if (weaponsItem is ItemDefinition)
                {
                    weaponsList.Add(kvp.Value + " x " + string.Format("<color={0}>{1}(s)</color>", announceWeaponColor, weaponItem.displayName.english));
                }
            }

            if (weaponsList.Count > 0)
            {
                weaponsNameText = string.Join(", ", weaponsList.ToArray());
            }

            string message = Format(format,
                initiator => initiatorText,
                victim => victimText,
                initiatorClanMates => initiatorClanMatesText,
                victimClanMates => victimClanMatesText,
                initiatorClan => initiatorClanText,
                victimClan => victimClanText,
                weapon => weaponNameText,
                weaponList => weaponsNameText
            );

            if (printToLog)
            {
                PrintToConsole(message);
            }

            if (announceGlobal)
            {
                if (announceRadius > 0)
                {
                    BroadcastLocal(announcePrefix, message, raid.firstDamage);
                }
                else
                {
                    BroadcastGlobal(announcePrefix, message);
                }
            }
            else if(announceClan)
            {
                if (raid.victim > 0)
                {
                    string tag = Clans.Call<string>("GetClanOf", raid.victim);

                    List<string> clan = GetClanMembers(tag);

                    if (clan.Count > 0)
                    {
                        foreach (string memberId in clan)
                        {
                            if (!string.IsNullOrEmpty(memberId))
                            {
                                BroadcastToPlayer(announcePrefix, memberId, message);
                            }
                        }
                    }
                }
            }
        }

        void BroadcastGlobal(string prefix, string message)
        {
            rust.BroadcastChat(prefix, message, announceIcon);
        }

        void BroadcastLocal(string prefix, string message, Vector3 position)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.Distance(position) <= announceRadius)
                {
                    player.ChatMessage(prefix + ": " + message);
                }
            }
        }

        void BroadcastToPlayer(string prefix, string userID, string message)
        {
            BasePlayer player = BasePlayer.Find(userID);

            if (player is BasePlayer)
            {
                player.ChatMessage(prefix + ": " + message);
            }
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (!(hitInfo.HitEntity is BasePlayer)) return;
            if (hitInfo.damageTypes.GetMajorityDamageType() != DamageType.Explosion) return;

            BasePlayer victim = (hitInfo.HitEntity as BasePlayer);

            if (victim != null)
            {
                RaidBehavior victimBehavior = victim.GetComponent<RaidBehavior>();
                RaidBehavior attackerBehavior = attacker.GetComponent<RaidBehavior>();

                if (victimBehavior != null && victimBehavior.raid != null)
                {
                    if (attackerBehavior == null || (attackerBehavior != null && attackerBehavior.raid == null))
                    {
                        AddToRaid(attacker, victimBehavior.raid);
                    }
                }
            }
        }

        public List<string> GetClanMembers(string tag)
        {
            JObject clan = Clans.Call<JObject>("GetClan", tag);

            List<string> members = new List<string>();

            if (clan == null)
            {
                return members;
            }

            foreach (string memberid in clan["members"])
            {
                members.Add(memberid);
            }

            return members;
        }

        public List<string> GetOnlineClanMembers(string tag)
        {
            List<string> allMembers = GetClanMembers(tag);

            List<string> onlineMembers = new List<string>();

            foreach (string mid in allMembers)
            {
                IPlayer p = covalence.Players.GetConnectedPlayer(mid);
                if (p is IPlayer)
                {
                    onlineMembers.Add(mid);
                }
            }

            return onlineMembers;
        }

        public List<Raid> GetRaids()
        {
            List<Raid> raids = new List<Raid>();
            var objects = GameObject.FindObjectsOfType(typeof(RaidBehavior));
            if (objects != null)
                foreach (var gameObj in objects)
                {
                    RaidBehavior raidBehavior = gameObj as RaidBehavior;
                    if (raidBehavior.raid != null)
                    {
                        raids.Add(raidBehavior.raid);
                    }
                }

            return raids;
        }

        public bool IsEntityRaidable(BaseCombatEntity entity)
        {
            if (entity is BuildingBlock)
            {
                return true;
            }

            string prefabName = entity.ShortPrefabName;

            foreach (string p in prefabs)
            {
                if (prefabName.IndexOf(p) != -1)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        string Format(string str, params Expression<Func<string, object>>[] args)
        {
            var parameters = args.ToDictionary
                                (e => string.Format("{{{0}}}", e.Parameters[0].Name)
                                , e => e.Compile()(e.Parameters[0].Name));

            var sb = new StringBuilder(str);
            foreach (var kv in parameters)
            {
                sb.Replace(kv.Key, kv.Value != null ? kv.Value.ToString() : "");
            }

            return sb.ToString();
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                Config[name] = defaultValue;
                Config.Save();
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        private T GetConfig<T>(string name, string name2, T defaultValue)
        {
            if (Config[name, name2] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name, name2], typeof(T));
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }

        public static List<BasePlayer> Sort(Vector3 position, List<BasePlayer> hits)
        {
            return hits.OrderBy(i => i.Distance(position)).ToList();
        }

        #endregion

    }
}