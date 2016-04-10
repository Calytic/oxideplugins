ï»¿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("NoEscape", "Calytic", "0.3.0", ResourceId = 1394)]
    [Description("Prevent tp/remove/bgrade while raid/combat is occuring")]
    class NoEscape : RustPlugin
    {
        #region Setup & Configuration

        Dictionary<string, Timer> raidBlocked = new Dictionary<string, Timer>();
        Dictionary<string, Timer> combatBlocked = new Dictionary<string, Timer>();

        List<string> commandsBlocked = new List<string>()
        {
            "remove",
            "tp",
            "bank",
            "trade",
            "recycle",
            "shop",
            "bgrade"
        };

        // COMBAT SETTINGS
        private bool combatBlock;
        private float combatDuration;
        private bool combatOnHitPlayer;
        private bool combatOnTakeDamage;

        // RAID BLOCK SETTINGS
        private bool raidBlock;
        private float raidDuration;
        private float raidDistance;
        private bool blockOnDamage;
        private bool blockOnDestroy;

        // RAID-ONLY SETTINGS
        private bool blockAll; // IGNORES ALL OTHER CHECKS
        private bool ownerBlock;
        private bool friendShare;
        private bool clanShare;
        private bool clanCheck;
        private bool friendCheck;
        private bool raiderBlock;
        private bool raiderFriendShare;
        private bool raiderClanShare;
        private List<string> damageTypes;

        // MISC SETTINGS
        private bool unblockOnDeath;
        private bool unblockOnWakeup;
        private bool unblockOnRespawn;

        private float cacheTimer;

        // MESSAGES
        private bool raidBlockNotify;
        private bool combatBlockNotify;

        private Dictionary<string, List<string>> memberCache = new Dictionary<string, List<string>>();
        private Dictionary<string, string> clanCache = new Dictionary<string, string>();
        private Dictionary<string, List<string>> friendCache = new Dictionary<string, List<string>>();
        private Dictionary<string, DateTime> lastClanCheck = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> lastCheck = new Dictionary<string, DateTime>();

        private Dictionary<string, DateTime> lastFriendCheck = new Dictionary<string, DateTime>();

        private Dictionary<string, DateTime> lastRaidBlock = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> lastCombatBlock = new Dictionary<string, DateTime>();

        private Dictionary<string, DateTime> lastRaidBlockNotification = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> lastCombatBlockNotification = new Dictionary<string, DateTime>();

        [PluginReference]
        Plugin Clans;

        [PluginReference]
        Plugin Friends;

        int blockLayer = UnityEngine.LayerMask.GetMask(new string[] { "Player (Server)" });
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

        private List<string> GetDefaultDamageTypes()
        {
            return new List<string>()
            {
                Rust.DamageType.Bullet.ToString(),
                Rust.DamageType.Blunt.ToString(),
                Rust.DamageType.Stab.ToString(),
                Rust.DamageType.Slash.ToString(),
                Rust.DamageType.Explosion.ToString(),
            };
        }

        protected override void LoadDefaultConfig()
        {
            Config["VERSION"] = Version.ToString();

            Config["raidBlock"] = true;
            Config["raidDuration"] = 300f; // 5 minutes
            Config["raidDistance"] = 100f;

            Config["blockOnDamage"] = true;
            Config["blockOnDestroy"] = false;

            Config["combatBlock"] = false;
            Config["combatDuration"] = 180f; // 3 minutes
            Config["combatOnHitPlayer"] = true;
            Config["combatOnTakeDamage"] = true;

            Config["ownerBlock"] = true;
            Config["clanShare"] = false;
            Config["friendShare"] = false;
            Config["raiderBlock"] = false;
            Config["raiderClanShare"] = false;
            Config["raiderFriendShare"] = false;
            Config["blockAll"] = false;
            Config["friendCheck"] = false;
            Config["clanCheck"] = false;
            Config["unblockOnDeath"] = true;
            Config["unblockOnWakeup"] = false;
            Config["unblockOnRespawn"] = true;
            Config["damageTypes"] = GetDefaultDamageTypes();
            Config["cacheMinutes"] = 1f;

            Config["raidBlockNotify"] = true;
            Config["combatBlockNotify"] = false;

            Config["VERSION"] = Version.ToString();
        }

        void Loaded()
        {
            LoadMessages();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Raid Blocked Message", "You may not do that while raid blocked ({time})"},
                {"Combat Blocked Message", "You may do that while a in combat ({time})"},
                {"Raid Block Complete", "You are no longer raid blocked."},
                {"Combat Block Complete", "You are no longer combat blocked."},
                {"Raid Block Notifier", "You are raid blocked for {time}"},
                {"Combat Block Notifier", "You are combat blocked for {time}"},
                {"Unit Seconds", "second(s)"},
                {"Unit Minutes", "minute(s)"},
            }, this);
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
            Config["raidBlockNotify"] = GetConfig("raidBlockNotify", true);
            Config["combatBlockNotify"] = GetConfig("combatBlockNotify", false);

            Config["blockOnDamage"] = GetConfig("blockOnDamage", true);
            Config["blockOnDestroy"] = GetConfig("blockOnDestroy", false);

            Config["raidBlock"] = GetConfig("raidBlock", true);
            Config["raidDuration"] = GetConfig("duration", 300f); // 5 minutes
            Config["raidDistance"] = GetConfig("distance", 100f);

            Config["combatBlock"] = GetConfig("combatBlock", false);
            Config["combatDuration"] = GetConfig("combatDuration", 180f); // 3 minutes
            Config["combatOnHitPlayer"] = GetConfig("combatOnHitPlayer", true);
            Config["combatOnTakeDamage"] = GetConfig("combatOnTakeDamage", true);

            Config["friendShare"] = GetConfig("friendShare", false);
            Config["raiderFriendShare"] = GetConfig("raiderFriendShare", false);
            Config["friendCheck"] = GetConfig("friendCheck", false);
            Config["unblockOnDeath"] = GetConfig("unblockOnDeath", true);
            Config["unblockOnWakeup"] = GetConfig("unblockOnWakeup", false);
            Config["unblockOnRespawn"] = GetConfig("unblockOnRespawn", true);

            Config["cacheMinutes"] = GetConfig("cacheMinutes", 1f);
            // END NEW CONFIGURATION OPTIONS

            PrintToConsole("Upgrading configuration file");
            SaveConfig();
        }

        void OnServerInitialized()
        {
            foreach (string command in commandsBlocked)
            {
                permission.RegisterPermission("noescape.raid." + command + "block", this);
                permission.RegisterPermission("noescape.combat." + command + "block", this);
            }

            CheckConfig();
            
            raidBlock = GetConfig("raidBlock", true);
            raidDuration = GetConfig("raidDuration", 50f);
            raidDistance = GetConfig("raidDistance", 100f);

            blockOnDamage = GetConfig("blockOnDamage", true);
            blockOnDestroy = GetConfig("blockOnDestroy", false);

            combatBlock = GetConfig("combatBlock", false);
            combatDuration = GetConfig("combatDuration", 180f);
            combatOnHitPlayer = GetConfig("combatOnHitPlayer", true);
            combatOnTakeDamage = GetConfig("combatOnTakeDamage", true);

            friendShare = GetConfig("friendShare", false);
            friendCheck = GetConfig("friendCheck", false);
            clanShare = GetConfig("clanShare", false);
            clanCheck = GetConfig("clanCheck", false);
            blockAll = GetConfig("blockAll", false);
            raiderBlock = GetConfig("raiderBlock", false);
            ownerBlock = GetConfig("ownerBlock", true);
            raiderClanShare = GetConfig("raiderClanShare", false);
            raiderFriendShare = GetConfig("raiderFriendShare", false);
            damageTypes = GetConfig<List<string>>("damageTypes", new List<string>());
            unblockOnDeath = GetConfig("unblockOnDeath", true);
            unblockOnWakeup = GetConfig("unblockOnWakeup", false);
            unblockOnRespawn = GetConfig("unblockOnRespawn", true);
            cacheTimer = GetConfig("cacheMinutes", 1f);

            raidBlockNotify = GetConfig("raidBlockNotify", true);
            combatBlockNotify = GetConfig("combatBlockNotify", false);

            if (clanShare || clanCheck || raiderClanShare )
            {
                if (!plugins.Exists("Clans"))
                {
                    clanShare = false;
                    clanCheck = false;
                    raiderClanShare = false;
                    PrintWarning("Clans not found! All clan options disabled. Cannot use clan options without this plugin. http://oxidemod.org/plugins/rust-io-clans.842/");
                }
            }

            if (friendShare || raiderFriendShare)
            {
                if (!plugins.Exists("Friends"))
                {
                    friendShare = false;
                    raiderFriendShare = false;
                    friendCheck = false;
                    PrintWarning("Friends not found! All friend options disabled. Cannot use friend options without this plugin. http://oxidemod.org/plugins/friends-api.686/");
                }
            }
        }

        #endregion

        #region Oxide Hooks

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (!blockOnDamage) return;
            if (hitInfo == null || hitInfo.WeaponPrefab == null ||  hitInfo.Initiator == null || !IsEntityBlocked(entity))
                return;

            if (damageTypes.Contains(hitInfo.damageTypes.GetMajorityDamageType().ToString())) 
                StructureAttack(entity, hitInfo.Initiator, hitInfo.WeaponPrefab.LookupShortPrefabName(), hitInfo.HitPositionWorld);
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (!combatBlock || !(hitInfo.HitEntity is BasePlayer)) return;
            if (!IsDamageBlocking(hitInfo.damageTypes.GetMajorityDamageType())) return;

            if (combatOnTakeDamage)
                StartCombatBlocking((hitInfo.HitEntity as BasePlayer).UserIDString);

            if (combatOnHitPlayer)
                StartCombatBlocking(attacker.UserIDString);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (blockOnDestroy)
            {
                if (hitInfo == null || hitInfo.WeaponPrefab == null || hitInfo.Initiator == null || !IsEntityBlocked(entity))
                    return;

                if (damageTypes.Contains(hitInfo.damageTypes.GetMajorityDamageType().ToString()))
                    StructureAttack(entity, hitInfo.Initiator, hitInfo.WeaponPrefab.LookupShortPrefabName(), hitInfo.HitPositionWorld);

                return;
            }

            if(entity.ToPlayer() == null) return;
            var player = entity.ToPlayer();
            if ((raidBlock || combatBlock) && unblockOnDeath && IsEscapeBlocked(player))
            {
                timer.In(0.3f, delegate()
                {
                    StopBlocking(player.UserIDString);
                });
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if ((raidBlock || combatBlock) && unblockOnWakeup && IsEscapeBlocked(player))
            {
                timer.In(0.3f, delegate()
                {
                    StopBlocking(player.UserIDString);
                });
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            if ((raidBlock || combatBlock) && unblockOnRespawn && IsEscapeBlocked(player))
            {
                timer.In(0.3f, delegate()
                {
                    StopBlocking(player.UserIDString);
                });
            }
        }

        #endregion

        #region Block Handling

        void StructureAttack(BaseEntity targetEntity, BaseEntity sourceEntity, string weapon, UnityEngine.Vector3 hitPosition )
        {
            string source;

            if (sourceEntity.ToPlayer() is BasePlayer)
                source = sourceEntity.ToPlayer().UserIDString;
            else
            {
                string ownerID = FindOwner(sourceEntity);
                if (!string.IsNullOrEmpty(ownerID))
                    source = ownerID;
                else
                    return;
            }
                
            if (source == null)
                return;

            string targetID = FindOwner(targetEntity);
            if (!string.IsNullOrEmpty(targetID))
            {
                var target = covalence.Players.GetPlayer(targetID);
                List<string> sourceMembers = null;

                if (clanCheck || friendCheck)
                    sourceMembers = getFriends(source);

                if (blockAll)
                {
                    BlockAll(source, targetEntity.transform.position, sourceMembers);

                    return;
                }

                if (ownerBlock)
                    OwnerBlock(source, target.UniqueID, targetEntity.transform.position, sourceMembers);

                if (raiderBlock)
                    RaiderBlock(source, target.UniqueID, targetEntity.transform.position, sourceMembers);
            }
        }

        void BlockAll(string source, UnityEngine.Vector3 position, List<string> sourceMembers = null)
        {
            var nearbyTargets = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(position, raidDistance, nearbyTargets, blockLayer);
            if (nearbyTargets.Count > 0)
                foreach (BasePlayer nearbyTarget in nearbyTargets)
                    if (ShouldBlockEscape(nearbyTarget.UserIDString, source, sourceMembers))
                        StartRaidBlocking(nearbyTarget.UserIDString);
        }

        void OwnerBlock(string source, string target, UnityEngine.Vector3 position, List<string> sourceMembers = null)
        {
            var targetMembers = new List<string>();

            if (clanShare || friendShare)
                targetMembers = getFriends(target);

            var nearbyTargets = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(position, raidDistance, nearbyTargets, blockLayer);
            if (nearbyTargets.Count > 0)
                foreach (BasePlayer nearbyTarget in nearbyTargets)
                    if (ShouldBlockEscape(target, source, sourceMembers) &&
                            (
                                nearbyTarget.UserIDString == target ||
                                (
                                    targetMembers != null &&
                                    targetMembers.Contains(nearbyTarget.UserIDString)
                                )
                            )
                        )
                    {
                        StartRaidBlocking(nearbyTarget.UserIDString);
                    }
        }

        void RaiderBlock(string source, string target, UnityEngine.Vector3 position, List<string> sourceMembers = null)
        {
            var targetMembers = new List<string>();

            if ((raiderClanShare || raiderFriendShare) && sourceMembers == null)
                sourceMembers = getFriends(source);

            var nearbyTargets = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(position, raidDistance, nearbyTargets, blockLayer);
            if (nearbyTargets.Count > 0)
                foreach (BasePlayer nearbyTarget in nearbyTargets)
                    if (ShouldBlockEscape(target, source, sourceMembers) &&
                            (
                                nearbyTarget.UserIDString == source ||
                                (
                                    sourceMembers != null &&
                                    sourceMembers.Contains(nearbyTarget.UserIDString)
                                )
                            )
                        )
                    {
                        StartRaidBlocking(nearbyTarget.UserIDString);
                    }
        }

        #endregion

        #region API

        bool IsEscapeBlocked(BasePlayer target)
        {
            return IsEscapeBlockedS(target.UserIDString);
        }

        bool IsRaidBlocked(BasePlayer target)
        {
            return IsRaidBlockedS(target.UserIDString);
        }

        bool IsCombatBlocked(BasePlayer target)
        {
            return IsCombatBlockedS(target.UserIDString);
        }

        bool IsEscapeBlockedS(string target)
        {
            return raidBlocked.ContainsKey(target) || combatBlocked.ContainsKey(target);
        }

        bool IsRaidBlockedS(string target)
        {
            return raidBlocked.ContainsKey(target);
        }

        bool IsCombatBlockedS(string target)
        {
            return combatBlocked.ContainsKey(target);
        }

        bool ShouldBlockEscape(string target, string source, List<string> sourceMembers = null)
        {
            if (target == source)
            {
                if (ownerBlock && raiderBlock && !clanCheck && !friendCheck)
                {
                    return true;
                }
                return false;
            }

            if (sourceMembers is List<string> && sourceMembers.Count > 0 && sourceMembers.Contains(target))
                return false;

            return true;
        }

        void StartRaidBlocking(string target)
        {
            if (raidBlocked.ContainsKey(target))
            {
                if (!raidBlocked[target].Destroyed)
                    raidBlocked[target].Destroy();
                raidBlocked.Remove(target);
            }

            if (!lastRaidBlock.ContainsKey(target))
                lastRaidBlock.Add(target, DateTime.Now);
            else
                lastRaidBlock[target] = DateTime.Now;

            SendRaidBlockMessage(target);

            raidBlocked.Add(target, timer.Once(raidDuration, delegate()
            {
                raidBlocked.Remove(target);
                if (raidBlockNotify)
                {
                    var targetPlayer = BasePlayer.Find(target);
                    if (targetPlayer is BasePlayer && targetPlayer.IsConnected())
                        SendReply(targetPlayer, GetMsg("Raid Block Complete"));
                }
            }));
        }

        void SendRaidBlockMessage(string target)
        {
            if (!raidBlockNotify)
                return;

            var send = false;
            if (lastRaidBlockNotification.ContainsKey(target))
            {
                TimeSpan diff = DateTime.Now - lastRaidBlockNotification[target];
                if (diff.TotalSeconds >= raidDuration)
                {
                    send = true;
                    lastRaidBlockNotification.Remove(target);
                }
            }
            else
                send = true;

            if (send)
            {
                var targetPlayer = BasePlayer.Find(target);
                if (targetPlayer is BasePlayer)
                {
                    SendReply(targetPlayer, GetMsg("Raid Block Notifier").Replace("{time}", GetCooldownTime(raidDuration)));
                    lastRaidBlockNotification.Add(target, DateTime.Now);
                }
            }
        }

        void StartCombatBlocking(string target)
        {
            if (combatBlocked.ContainsKey(target))
            {
                if (!combatBlocked[target].Destroyed)
                    combatBlocked[target].Destroy();
                combatBlocked.Remove(target);
            }

            if (!lastCombatBlock.ContainsKey(target))
                lastCombatBlock.Add(target, DateTime.Now);
            else
                lastCombatBlock[target] = DateTime.Now;

            SendCombatBlockMessage(target);

            combatBlocked.Add(target, timer.Once(combatDuration, delegate()
            {
                combatBlocked.Remove(target);
                if (combatBlockNotify)
                {
                    BasePlayer targetPlayer = BasePlayer.Find(target);
                    if (targetPlayer is BasePlayer && targetPlayer.IsConnected())
                        SendReply(targetPlayer, GetMsg("Combat Block Complete"));
                }
            }));
        }

        void SendCombatBlockMessage(string target)
        {
            if (!combatBlockNotify)
                return;

            var send = false;
            if (lastCombatBlockNotification.ContainsKey(target))
            {
                TimeSpan diff = DateTime.Now - lastCombatBlockNotification[target];
                if (diff.TotalSeconds >= combatDuration)
                {
                    send = true;
                    lastCombatBlockNotification.Remove(target);
                }
            }
            else
                send = true;

            if (send)
            {
                var targetPlayer = BasePlayer.Find(target);
                if (targetPlayer is BasePlayer)
                {
                    SendReply(targetPlayer, GetMsg("Combat Block Notifier").Replace("{time}", GetCooldownTime(combatDuration)));
                    lastCombatBlockNotification.Add(target, DateTime.Now);
                }
            }
        }

        void StopBlocking(string target)
        {
            if (IsRaidBlockedS(target))
                StopRaidBlocking(target);
            if (IsCombatBlockedS(target))
                StopCombatBlocking(target);
        }

        void StopRaidBlocking(string target)
        {
            if (!raidBlocked.ContainsKey(target))
                return;

            if (!raidBlocked[target].Destroyed)
                raidBlocked[target].Destroy();

            raidBlocked.Remove(target);
        }

        void StopCombatBlocking(string target)
        {
            if (!combatBlocked.ContainsKey(target))
                return;

            if (!combatBlocked[target].Destroyed)
                combatBlocked[target].Destroy();

            combatBlocked.Remove(target);
        }

        #endregion

        #region Friend/Clan Integration

        public List<string> getFriends(string player)
        {
            var players = new List<string>();
            if (player == null)
                return players;

            if (friendShare || raiderFriendShare || friendCheck)
                players.AddRange(getFriendList(player));

            if (clanShare || raiderClanShare || clanCheck)
            {
                var members = getClanMembers(player);
                if(members != null)
                    players.AddRange(members);
            }
            return players;
        }

        public List<string> getFriendList(string player)
        {
            object friends_obj = null;
            if (lastFriendCheck.ContainsKey(player))
            {
                if ((DateTime.Now - lastFriendCheck[player]).TotalMinutes <= cacheTimer)
                    return friendCache[player];
                else
                {
                    friends_obj = Friends?.CallHook("IsFriendOfS", player);
                    lastFriendCheck[player] = DateTime.Now;
                }
            }
            else
            {
                friends_obj = Friends?.CallHook("IsFriendOfS", player);
                if (lastFriendCheck.ContainsKey(player))
                    lastFriendCheck.Remove(player);

                if (friendCache.ContainsKey(player))
                    friendCache.Remove(player);
                
                lastFriendCheck.Add(player, DateTime.Now);
            }

            var players = new List<string>();

            if (friends_obj == null)
                return players;

            string[] friends = friends_obj as string[];
            
            foreach (string fid in friends)
                players.Add(fid);

            if (friendCache.ContainsKey(player))
                friendCache[player] = players;
            else
                friendCache.Add(player, players);

            return players;
        }

        public List<string> getClanMembers(string player)
        {
            string tag = null;
            if (lastClanCheck.ContainsKey(player) && clanCache.ContainsKey(player))
            {
                if ((DateTime.Now - lastClanCheck[player]).TotalMinutes <= cacheTimer)
                    tag = clanCache[player];
                else
                {
                    tag = Clans.Call<string>("GetClanOf", player);
                    clanCache[player] = tag;
                    lastClanCheck[player] = DateTime.Now;
                }
            }
            else
            {
                tag = Clans.Call<string>("GetClanOf", player);
                if (lastClanCheck.ContainsKey(player))
                    lastClanCheck.Remove(player);

                if (clanCache.ContainsKey(player))
                    clanCache.Remove(player);

                clanCache.Add(player, tag);
                lastClanCheck.Add(player, DateTime.Now);
            }

            if (tag == null)
                return null;

            if (memberCache.ContainsKey(tag))
                return memberCache[tag];

            var clan = GetClan(tag);

            if (clan == null)
                return null;

            return CacheClan(clan);
        }

        JObject GetClan(string tag)
        {
            return Clans.Call<JObject>("GetClan", tag);
        }

        List<string> CacheClan(JObject clan)
        {
            string tag = clan["tag"].ToString();
            List<string> players = new List<string>();
            foreach (string memberid in clan["members"])
            {
                if (clanCache.ContainsKey(memberid))
                {
                    clanCache[memberid] = tag;
                }
                else
                {
                    clanCache.Add(memberid, tag);
                }
                players.Add(memberid);
            }

            if (memberCache.ContainsKey(tag))
                memberCache[tag] = players;
            else
                memberCache.Add(tag, players);

            if (lastCheck.ContainsKey(tag))
                lastCheck[tag] = DateTime.Now;
            else
                lastCheck.Add(tag, DateTime.Now);

            return players;
        }

        void OnClanCreate(string tag)
        {
            var clan = GetClan(tag);
            CacheClan(clan);
        }

        void OnClanUpdate(string tag)
        {
            var clan = GetClan(tag);
            CacheClan(clan);
        }

        void OnClanDestroy(string tag)
        {
            if (lastCheck.ContainsKey(tag))
            {
                lastCheck.Remove(tag);
            }

            if (memberCache.ContainsKey(tag))
            {
                memberCache.Remove(tag);
            }
        }

        #endregion

        #region Permission Checking & External API Handling

        bool HasPerm(string userid, string perm)
        {
            return permission.UserHasPermission(userid, "noescape." + perm);
        }

        bool CanRaidCommand(string playerID, string command)
        {
            return raidBlock && HasPerm(playerID, "raid." + command + "block") && IsRaidBlockedS(playerID);
        }

        bool CanCombatCommand(string playerID, string command)
        {
            return combatBlock && HasPerm(playerID, "combat." + command + "block") && IsCombatBlockedS(playerID);
        }

        object CanDo(string command, BasePlayer player)
        {
            if (CanRaidCommand(player.UserIDString, command))
                return GetRaidMessage(player.UserIDString);
            else if (CanCombatCommand(player.UserIDString, command))
                return GetCombatMessage(player.UserIDString);

            return null;
        }

        object CanBank(BasePlayer player)
        {
            return CanDo("bank", player);
        }

        object CanTrade(BasePlayer player)
        {
            return CanDo("trade", player);
        }

        object canRemove(BasePlayer player)
        {
            return CanDo("remove", player);
        }

        object CanShop(BasePlayer player)
        {
            return CanDo("shop", player);
        }

        object CanTeleport(BasePlayer player)
        {
            return CanDo("tp", player);
        }

        object canTeleport(BasePlayer player) // ALIAS FOR MagicTeleportation
        {
            return CanTeleport(player);
        }

        object CanRecycle(BasePlayer player)
        {
            return CanDo("recycle", player);
        }

        object CanAutoGrade(BasePlayer player, int grade, BuildingBlock buildingBlock, Planner planner)
        {
            if (CanRaidCommand(player.UserIDString, "bgrade") || CanCombatCommand(player.UserIDString, "bgrade"))
                return -1;
            return null;
        }

        #endregion

        #region Messages

        string GetCooldownTime(float f)
        {
            if (f > 60)
                return Math.Round(f / 60, 1) + " " + GetMsg("Unit Minutes");

            return f + " " + GetMsg("Unit Seconds");
        }

        public string GetMessage(string player)
        {
            if (IsRaidBlockedS(player))
                return GetRaidMessage(player);
            else if (IsCombatBlockedS(player))
                return GetCombatMessage(player);

            return null;
        }

        public string GetRaidMessage(string player)
        {
            if (raidDuration > 0)
            {
                var ts = DateTime.Now - lastRaidBlock[player];
                var unblocked = Math.Round((raidDuration / 60) - Convert.ToSingle(ts.TotalMinutes), 2);

                if (ts.TotalMinutes <= raidDuration)
                {
                    if (unblocked < 1)
                    {
                        var timelefts = Math.Round(Convert.ToDouble(raidDuration) - ts.TotalSeconds);
                        return GetMsg("Raid Blocked Message").Replace("{time}", timelefts.ToString() + " " + GetMsg("Unit Seconds"));
                    }
                    else
                        return GetMsg("Raid Blocked Message").Replace("{time}", unblocked.ToString() + " " + GetMsg("Unit Minutes"));
                }
            }

            return null;
        }

        public string GetCombatMessage(string player)
        {
            if (combatDuration > 0)
            {
                var ts = DateTime.Now - lastCombatBlock[player];
                var unblocked = Math.Round((combatDuration / 60) - Convert.ToSingle(ts.TotalMinutes), 2);

                if (ts.TotalMinutes <= combatDuration)
                {
                    if (unblocked < 1)
                    {
                        var timelefts = Math.Round(Convert.ToDouble(combatDuration) - ts.TotalSeconds);
                        return GetMsg("Combat Blocked Message").Replace("{time}", timelefts.ToString() + "s");
                    }
                    else
                        return GetMsg("Combat Blocked Message").Replace("{time}", unblocked.ToString() + "m");
                }
            }

            return null;
        }

        #endregion

        #region Utility Methods

        string FindOwner(BaseEntity entity)
        {
            var ownerid = entity.OwnerID;
            if (ownerid == 0)
                return "";

            return ownerid.ToString();
        }

        public bool IsEntityBlocked(BaseCombatEntity entity)
        {
            if (entity is BuildingBlock) return true;

            var prefabName = entity.LookupShortPrefabName();

            foreach (string p in prefabs)
                if (prefabName.IndexOf(p) != -1)
                    return true;

            return false;
        }

        bool IsDamageBlocking(Rust.DamageType dt)
        {
            switch (dt)
            {
                case Rust.DamageType.Bullet:
                case Rust.DamageType.Stab:
                case Rust.DamageType.Explosion:
                case Rust.DamageType.ElectricShock:
                    return true;
            }
            return false;
        }

        T GetConfig<T>(string key, T defaultValue)
        {
            try
            {
                var val = Config[key];
                if (val == null)
                    return defaultValue;
                if (val is List<object>)
                {
                    var t = typeof(T).GetGenericArguments()[0];
                    if (t == typeof(String))
                    {
                        var cval = new List<string>();
                        foreach (var v in val as List<object>)
                            cval.Add((string)v);
                        val = cval;
                    }
                    else if (t == typeof(int))
                    {
                        var cval = new List<int>();
                        foreach (var v in val as List<object>)
                            cval.Add(Convert.ToInt32(v));
                        val = cval;
                    }
                }
                else if (val is Dictionary<string, object>)
                {
                    var t = typeof(T).GetGenericArguments()[1];
                    if (t == typeof(int))
                    {
                        var cval = new Dictionary<string, int>();
                        foreach (var v in val as Dictionary<string, object>)
                            cval.Add(Convert.ToString(v.Key), Convert.ToInt32(v.Value));
                        val = cval;
                    }
                }
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch (Exception ex)
            {
                PrintWarning("Invalid config value: " + key + " (" + ex.Message + ")");
                return defaultValue;
            }
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }

        #endregion
    }
}
