// Reference: Newtonsoft.Json
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Oxide.Core;
using UnityEngine;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Rust;
using Facepunch;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("AntiOfflineRaid", "Calytic", "0.2.6", ResourceId = 1464)]
    [Description("Prevents/reduces offline raiding")]
    public class AntiOfflineRaid : RustPlugin
    {
        #region Variables

        [PluginReference]
        Plugin Clans;

        float tickRate = 5f;

        private Dictionary<ulong, LastOnline> lastOnline = new Dictionary<ulong, LastOnline>();
        private Dictionary<string, object> damageScale = new Dictionary<string, object>();

        internal static int cooldownMinutes;
        private float interimDamage;
        private static int afkMinutes;
        private bool showMessage;
        private bool playSound;
        private string sound;

        private bool clanShare;
        private bool clanFirstOffline;
        private int minMembers;

        Timer lastOnlineTimer;

        List<object> prefabs;

        Dictionary<string, List<string>> memberCache = new Dictionary<string, List<string>>();

        #endregion

        #region Class

        class LastOnline
        {
            public ulong userid;
            public long lastOnlineLong;

            [JsonIgnore]
            public Vector3 lastPosition = default(Vector3);

            [JsonIgnore]
            public float afkMinutes = 0;

            [JsonIgnore]
            public DateTime lastOnline
            {
                get
                {
                    return DateTime.FromBinary(lastOnlineLong);
                }

                set
                {
                    lastOnlineLong = value.ToBinary();
                }
            }

            [JsonConstructor]
            public LastOnline(ulong userid, long lastOnlineLong)
            {
                this.userid = userid;
                this.lastOnlineLong = lastOnlineLong;
            }

            public LastOnline(BasePlayer player, DateTime lastOnline)
            {
                this.userid = player.userID;
                this.lastOnline = lastOnline;
            }

            [JsonIgnore]
            public BasePlayer player
            {
                get
                {
                    return BasePlayer.FindByID(userid);
                }
            }

            public bool IsConnected()
            {
                BasePlayer player = this.player;
                if (player != null && player.IsConnected())
                {
                    return true;
                }

                return false;
            }

            public bool IsOffline()
            {
                return HasMinutes(cooldownMinutes);
            }

            [JsonIgnore]
            public double Days
            {
                get
                {
                    TimeSpan ts = DateTime.Now - lastOnline;
                    return ts.TotalDays;
                }
            }

            public bool HasDays(int days)
            {
                if (Days >= days)
                {
                    return true;
                }

                return false;
            }

            [JsonIgnore]
            public double Minutes
            {
                get
                {
                    TimeSpan ts = DateTime.Now - lastOnline;
                    return ts.TotalMinutes;
                }
            }

            public bool HasMinutes(int minutes)
            {
                if (Minutes >= minutes)
                {
                    return true;
                }

                return false;
            }

            [JsonIgnore]
            public double Hours
            {
                get
                {
                    TimeSpan ts = DateTime.Now - lastOnline;
                    return ts.TotalHours;
                }
            }

            public bool HasHours(int hours)
            {
                DateTime start = DateTime.Now;

                TimeSpan ts = start - lastOnline;

                if (Hours >= hours)
                {
                    return true;
                }

                return false;
            }

            public bool IsAFK()
            {
                if (afkMinutes >= AntiOfflineRaid.afkMinutes)
                {
                    return true;
                }

                return false;
            }

            public bool HasMoved(Vector3 position)
            {
                bool equal = true;

                if (lastPosition.Equals(position)) {
                    
                    equal = false;
                }

                lastPosition = new Vector3(position.x, position.y, position.z);

                return equal;
            }
        }

        #endregion

        #region Initialization & Configuration

        void OnServerInitialized()
        {
            permission.RegisterPermission("antiofflineraid.protect", this);
            permission.RegisterPermission("antiofflineraid.check", this);

            LoadMessages();
            LoadData();

            damageScale = GetConfig<Dictionary<string, object>>("damageScale", GetDefaultReduction());
            prefabs = GetConfig<List<object>>("prefabs", GetDefaultPrefabs());
            afkMinutes = GetConfig<int>("afkMinutes", 5);
            cooldownMinutes = GetConfig<int>("cooldownMinutes", 10);
            interimDamage = GetConfig<float>("interimDamage", 0f);

            clanShare = GetConfig<bool>("clanShare", false);
            clanFirstOffline = GetConfig<bool>("clanFirstOffline", false);
            minMembers = GetConfig<int>("minMembers", 1);
            showMessage = GetConfig<bool>("showMessage", true);
            playSound = GetConfig<bool>("playSound", false);
            sound = GetConfig<string>("sound", "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab");

            if (clanShare)
            {
                if (!plugins.Exists("Clans"))
                {
                    clanShare = false;
                    PrintWarning("Clans not found! clanShare disabled. Cannot use clanShare without this plugin. http://oxidemod.org/plugins/clans.2087/");
                }
            }

            UpdateLastOnlineAll();
            lastOnlineTimer = timer.Repeat(tickRate * 60, 0, delegate()
            {
                UpdateLastOnlineAll();
            });
        }

        protected Dictionary<string, object> GetDefaultReduction()
        {
            return new Dictionary<string, object>()
            {
                {"1", 0.2},
                {"3", 0.35f},
                {"6", 0.5f},
                {"12", 0.8f},
                {"48", 1}
            };
        }

        List<object> GetDefaultPrefabs()
        {
            return new List<object>()
            {
                "door.hinged",
                "door.double.hinged",
                "window.bars",
                "floor.ladder.hatch",
                "floor.frame",
                "wall.frame",
                "shutter",
                "wall.external",
                "gates.external"
            };
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Protection Message", "This building is protected: {amount}%"},
                {"Denied: Permission", "You lack permission to do that"}
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
            PrintToConsole("Creating new configuration");
            Config.Clear();

            Config["damageScale"] = GetDefaultReduction();
            Config["afkMinutes"] = 5;
            Config["cooldownMinutes"] = 10;
            Config["interimDamage"] = 0f;
            Config["minMembers"] = 1;
            Config["clanShare"] = false;
            Config["clanFirstOffline"] = false;
            Config["showMessage"] = true;
            Config["playSound"] = false;
            Config["prefabs"] = GetDefaultPrefabs();
            Config["sound"] = "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab";
            Config["VERSION"] = Version.ToString();
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            Config["clanFirstOffline"] = GetConfig("clanFirstOffline", false);
            // END NEW CONFIGURATION OPTIONS

            PrintWarning("Upgrading configuration file");
            SaveConfig();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnServerShutdown()
        {
            UpdateLastOnlineAll();
            SaveData();
        }

        //void Unload()
        //{
        //    SaveData();
        //}

        void LoadData()
        {
            lastOnline = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, LastOnline>>("antiofflineraid");

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

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<Dictionary<ulong, LastOnline>>("antiofflineraid", lastOnline);
        }

        #endregion

        #region Oxide hooks

        void OnPlayerDisconnected(BasePlayer player)
        {
            UpdateLastOnline(player);
        }

        void OnPlayerInit(BasePlayer player)
        {
            UpdateLastOnline(player);
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input == null) return;
            if (input.current == null) return;
            if (input.previous == null) return;

            LastOnline lastOnlinePlayer = null;
            if (lastOnline.TryGetValue(player.userID, out lastOnlinePlayer) && input.current.buttons != 0 && !input.previous.Equals(input.current))
            {
                lastOnlinePlayer.afkMinutes = 0;
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null) return;

            if (IsBlocked(entity)) OnStructureAttack(entity, hitInfo);
        }

        #endregion

        #region Core Methods

        private void UpdateLastOnlineAll()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!player.IsConnected())
                    continue;

                bool hasMoved = true;
                LastOnline lastOnlinePlayer;
                if (lastOnline.TryGetValue(player.userID, out lastOnlinePlayer))
                {
                    if (!lastOnlinePlayer.HasMoved(player.transform.position))
                    {
                        hasMoved = false;
                        lastOnlinePlayer.afkMinutes += tickRate;
                    }

                    if (lastOnlinePlayer.IsAFK())
                    {
                        continue;
                    }
                } 

                UpdateLastOnline(player, hasMoved);
            }
        }

        private void UpdateLastOnline(BasePlayer player, bool hasMoved = true)
        {
            LastOnline lastOnlinePlayer;
            if (!lastOnline.TryGetValue(player.userID, out lastOnlinePlayer))
            {
                lastOnline.Add(player.userID, new LastOnline(player, DateTime.Now));
            }
            else
            {
                lastOnlinePlayer.lastOnline = DateTime.Now;
                if (hasMoved) lastOnlinePlayer.afkMinutes = 0;
            }
        }

        void OnStructureAttack(BaseEntity entity, HitInfo hitinfo)
        {
            ulong targetID = 0;

            targetID = entity.OwnerID;
            if (targetID != 0 && HasPerm(targetID.ToString(), "antiofflineraid.protect") && lastOnline.ContainsKey(targetID))
            {
                float scale = scaleDamage(targetID);
                if (clanShare)
                {
                    if (IsClanOffline(targetID))
                    {
                        mitigateDamage(hitinfo, scale);
                    }
                }
                else
                {
                    mitigateDamage(hitinfo, scale);
                }
            }
        }

        public void mitigateDamage(HitInfo hitinfo, float scale)
        {
            if (scale > -1 && scale != 1)
            {
                bool isFire = hitinfo.damageTypes.GetMajorityDamageType() == DamageType.Heat;

                if (scale == 0)
                {
                    // completely cancel damage
                    
                    hitinfo.damageTypes = new DamageTypeList();
                    hitinfo.DoHitEffects = false;
                    hitinfo.HitMaterial = 0;
                    if (showMessage && ((isFire && hitinfo.WeaponPrefab != null) || (!isFire)) )
                    {
                        sendMessage(hitinfo);
                    }

                    if (playSound && hitinfo.Initiator is BasePlayer && !isFire)
                    {
                        Effect.server.Run(sound, hitinfo.Initiator.transform.position);
                    }
                }
                else
                {
                    // only scale damage
                    hitinfo.damageTypes.ScaleAll(scale);
                    if (scale < 1)
                    {
                        if (showMessage && ((isFire && hitinfo.WeaponPrefab != null) || (!isFire))) 
                        {
                            sendMessage(hitinfo, 100 - Convert.ToInt32(scale * 100));
                        }

                        if (playSound && hitinfo.Initiator is BasePlayer && !isFire)
                        {
                            Effect.server.Run(sound, hitinfo.Initiator.transform.position);
                        }
                    }
                }
            }
        }

        private void sendMessage(HitInfo hitinfo, int amt = 100)
        {
            if (hitinfo.Initiator is BasePlayer)
            {
                ShowMessage((BasePlayer)hitinfo.Initiator, amt);
            }
        }

        public float scaleDamage(ulong targetID)
        {
            float scale = -1;

            ulong lastOffline = targetID;
            if (clanShare)
            {
                string tag = Clans.Call<string>("GetClanOf", targetID);
                if (!string.IsNullOrEmpty(tag))
                {
                    if (clanFirstOffline)
                    {
                        lastOffline = getClanFirstOffline(tag);
                    }
                    else
                    {
                        lastOffline = getClanLastOffline(tag);
                    }
                }
            }

            LastOnline lastOnlinePlayer;

            if (!lastOnline.TryGetValue(lastOffline, out lastOnlinePlayer))
            {
                return -1;
            }

            if (!lastOnlinePlayer.IsOffline())
            {
                // must be logged out for atleast x minutes before damage is reduced
                return -1;
            }
            else
            {
                if (lastOnlinePlayer.HasMinutes(60))
                {
                    // if you've been offline/afk for more than an hour, use hourly scales
                    var keys = damageScale.Keys.Select(int.Parse).ToList();
                    keys.Sort();

                    foreach (int key in keys)
                    {
                        if (lastOnlinePlayer.HasHours(key))
                        {
                            scale = Convert.ToSingle(damageScale[key.ToString()]);
                        }
                    }
                }
                else
                {
                    // if you have been offline for more than x minutes but less than an hour, use interimDamage
                    scale = interimDamage;
                }
            }

            return scale;
        }

        public bool IsBlocked(BaseCombatEntity entity)
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

        public bool IsOffline(ulong playerID)
        {
            LastOnline lastOnlinePlayer;
            if (lastOnline.TryGetValue(playerID, out lastOnlinePlayer))
            {
                return lastOnlinePlayer.IsOffline();
            }

            BasePlayer player = BasePlayer.FindByID(playerID);
            if (player == null)
            {
                return true;
            }

            if (player.IsConnected())
            {
                return false;
            }

            return true;
        }

        private string SendStatus(Network.Connection connection, string[] args)
        {
            if (args.Length == 1)
            {
                IPlayer target = FindPlayerByPartialName(args[0]);
                ulong userID;
                LastOnline lo;
                if (target is IPlayer && ulong.TryParse(target.Id, out userID) &&  lastOnline.TryGetValue(userID, out lo))
                {
                    StringBuilder sb = new StringBuilder();

                    if (IsOffline(userID))
                    {
                        sb.AppendLine("<color=red><size=15>AntiOfflineRaid Status</size></color>: " + target.Name);
                        if (target.IsConnected)
                        {
                            sb.AppendLine("<color=lightblue>Player Status</color>: <color=orange>AFK</color>: " + lo.lastOnline.ToString());
                        }
                        else
                        {
                            sb.AppendLine("<color=lightblue>Player Status</color>: <color=red>Offline</color>: " + lo.lastOnline.ToString());
                        }
                    }
                    else
                    {
                        sb.AppendLine("<color=lime><size=15>AntiOfflineRaid Status</size></color>: " + target.Name);
                        sb.AppendLine("<color=lightblue>Player Status</color>: <color=lime>Online</color>");
                    }
                    sb.AppendLine("<color=lightblue>AFK</color>: " + lo.afkMinutes + " minutes");
                    if (clanShare)
                    {
                        sb.AppendLine("<color=lightblue>Clan Status</color>: " + (IsClanOffline(userID) ? "<color=red>Offline</color>" : "<color=lime>Online</color>") + " (" + getClanMembersOnline(userID) + ")");
                        string tag = Clans.Call<string>("GetClanOf", userID);
                        if (!string.IsNullOrEmpty(tag))
                        {
                            ulong lastOffline = 0;
                            string msg = "";
                            if (clanFirstOffline)
                            {
                                lastOffline = getClanFirstOffline(tag);
                                msg = "First Offline";
                            }
                            else
                            {
                                lastOffline = getClanLastOffline(tag);
                                msg = "Last Offline";
                            }

                            LastOnline lastOfflinePlayer;

                            if (lastOnline.TryGetValue(lastOffline, out lastOfflinePlayer))
                            {
                                DateTime lastOfflineTime = lastOfflinePlayer.lastOnline;
                                IPlayer p = covalence.Players.FindPlayerById(lastOffline.ToString());
                                sb.AppendLine("<color=lightblue>Clan " + msg + "</color>: " + p.Name + " - " + lastOfflineTime.ToString());
                            }
                        }
                    }

                    float scale = scaleDamage(userID);
                    if (scale != -1)
                    {
                        sb.AppendLine("<color=lightblue>Scale</color>: " + scale);
                    }

                    return sb.ToString();
                }
                else
                {
                    return "No player found.";
                }
            }
            else
            {
                return "Invalid Syntax. ao <PlayerName>";
            }
        }

        #endregion

        #region Clan Integration

        public bool IsClanOffline(ulong targetID)
        {
            int mcount = getClanMembersOnline(targetID);

            if (mcount >= minMembers)
            {
                return false;
            }

            return true;
        }

        public int getClanMembersOnline(ulong targetID)
        {
            var player = covalence.Players.FindPlayerById(targetID.ToString());
            var start = (player.IsConnected == false) ? 0 : 1;
            string tag = Clans.Call<string>("GetClanOf", targetID);
            if (tag == null)
            {
                return start;
            }

            List<string> members = getClanMembers(tag);

            int mcount = start;

            foreach (string memberid in members)
            {
                ulong mid = Convert.ToUInt64(memberid);
                if (mid == targetID) continue;
                if (!IsOffline(mid))
                {
                    mcount++;
                }
            }

            return mcount;
        }

        public List<string> getClanMembers(string tag)
        {
            List<string> memberList;
            if (memberCache.TryGetValue(tag, out memberList))
            {
                return memberList;
            }

            return CacheClan(tag);
        }

        public List<string> CacheClan(string tag)
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

            if (memberCache.ContainsKey(tag))
            {
                memberCache[tag] = members;
            }
            else
            {
                memberCache.Add(tag, members);
            }

            return members;
        }

        public ulong getClanFirstOffline(string tag)
        {
            List<string> clanMembers = getClanMembers(tag);

            Dictionary<string, DateTime> members = new Dictionary<string, DateTime>();

            if (clanMembers.Count == 0)
            {
                return 0;
            }

            foreach (string memberid in clanMembers)
            {
                ulong mid = Convert.ToUInt64(memberid);
                LastOnline lastOnlineMember;
                if (lastOnline.TryGetValue(mid, out lastOnlineMember) && IsOffline(mid))
                {
                    members.Add(memberid, lastOnlineMember.lastOnline);
                }
            }

            foreach (KeyValuePair<string, DateTime> kvp in members.OrderByDescending(p => p.Value))
            {
                return Convert.ToUInt64(kvp.Key);
            }

            return 0;
        }

        public ulong getClanLastOffline(string tag)
        {
            List<string> clanMembers = getClanMembers(tag);

            Dictionary<string, DateTime> members = new Dictionary<string, DateTime>();

            if (clanMembers.Count == 0)
            {
                return 0;
            }

            foreach (string memberid in clanMembers)
            {
                ulong mid = Convert.ToUInt64(memberid);
                LastOnline lastOnlineMember;
                if(lastOnline.TryGetValue(mid, out lastOnlineMember) && IsOffline(mid))  
                {
                    members.Add(memberid, lastOnlineMember.lastOnline);
                }
            }

            foreach (KeyValuePair<string, DateTime> kvp in members.OrderBy(p => p.Value))
            {
                return Convert.ToUInt64(kvp.Key);
            }

            return 0;
        }

        void OnClanCreate(string tag)
        {
            CacheClan(tag);
        }

        void OnClanUpdate(string tag)
        {
            CacheClan(tag);
        }

        void OnClanDestroy(string tag)
        {
            if (memberCache.ContainsKey(tag))
            {
                memberCache.Remove(tag);
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("ao")]
        private void ccStatus(ConsoleSystem.Arg arg)
        {
            if (arg.connection.player is BasePlayer)
            {
                if (!HasPerm(arg.connection.player as BasePlayer, "antiofflineraid.check") && arg.connection.authLevel < 1)
                {
                    SendReply(arg, GetMsg("Denied: Permission", arg.connection.userid));
                    return;
                }
            }
            SendReply(arg, SendStatus(arg.connection, arg.Args));
        }

        [ChatCommand("ao")]
        private void cmdStatus(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, "antiofflineraid.check") && player.net.connection.authLevel < 1)
            {
                SendReply(player, GetMsg("Denied: Permission", player.UserIDString));
                return;
            }

            SendReply(player, SendStatus(player.net.connection, args));
        }

        //[ChatCommand("boffline")]
        //private void cmdboffline(BasePlayer player, string command, string[] args)
        //{
        //    lastOnline[player.userID].lastOnline = lastOnline[player.userID].lastOnline.Subtract(TimeSpan.FromHours(3));
        //}

        //[ChatCommand("bonline")]
        //private void cmdbonline(BasePlayer player, string command, string[] args)
        //{
        //    lastOnline[player.userID].lastOnline = DateTime.Now;
        //    lastOnline[player.userID].afkMinutes = 0;
        //}

        #endregion

        #region HelpText

        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder()
               .Append("AntiOfflineRaid by <color=#ce422b>http://rustservers.io</color>\n");

            if(cooldownMinutes > 0) {
                sb.Append("  ").Append(string.Format("<color=\"#ffd479\">First {0} minutes</color>: 100%", cooldownMinutes)).Append("\n");
                sb.Append("  ").Append(string.Format("<color=\"#ffd479\">Between {0} minutes and 1 hour</color>: {1}%", cooldownMinutes, interimDamage * 100)).Append("\n");
            } else {
                sb.Append("  ").Append(string.Format("<color=\"#ffd479\">First hour</color>: {0}%", interimDamage * 100)).Append("\n");
            }

            var keys = damageScale.Keys.Select(int.Parse).ToList();
            keys.Sort();

            foreach (var key in keys)
            {
                double scale = System.Math.Round(Convert.ToDouble(damageScale[key.ToString()]) * 100, 0);
                double hours = System.Math.Round(Convert.ToDouble(key), 1);
                if (hours >= 24)
                {
                    double days = System.Math.Round(hours / 24, 1);
                    sb.Append("  ").Append(string.Format("<color=\"#ffd479\">After {0} days(s)</color>: {1}%", days, scale)).Append("\n");
                }
                else
                {
                    sb.Append("  ").Append(string.Format("<color=\"#ffd479\">After {0} hour(s)</color>: {1}%", hours, scale)).Append("\n");
                }
            }

            player.ChatMessage(sb.ToString());
        }

        #endregion

        #region Helper Methods

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        protected IPlayer FindPlayerByPartialName(string nameOrIdOrIp)
        {
            if (string.IsNullOrEmpty(nameOrIdOrIp))
                return null;

            IPlayer player = covalence.Players.FindPlayerById(nameOrIdOrIp);

            if (player is IPlayer)
            {
                return player;
            }

            return null;
        }

        bool HasPerm(BasePlayer p, string pe) {
            return permission.UserHasPermission(p.userID.ToString(), pe);
        }

        bool HasPerm(string userid, string pe)
        {
            return permission.UserHasPermission(userid, pe);
        }

        string GetMsg(string key, object userID = null)
        {
            return lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        }

        #endregion

        #region GUI

        private void HideMessage(BasePlayer player)
        {
            if (player.net == null) return;
            if (player.net.connection == null) return;

            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList("AntiOfflineRaidMsg"));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", obj);
        }

        private void ShowMessage(BasePlayer player, int amount = 100)
        {
            HideMessage(player);
            string send = jsonMessage;
            send = send.Replace("{1}", Oxide.Core.Random.Range(1, 99999).ToString());
            send = send.Replace("{protection_message}", GetMsg("Protection Message", player.UserIDString));
            send = send.Replace("{amount}", amount.ToString());
            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList(send));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", obj);

            timer.In(3f, delegate()
            {
                HideMessage(player);
            });
        }

        private string jsonMessage = @"[{""name"":""AntiOfflineRaidMsg"",""parent"":""Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0 0 0 0.78""},{""type"":""RectTransform"",""anchormax"":""0.64 0.88"",""anchormin"":""0.38 0.79""}]},{""name"":""MessageLabel{1}"",""parent"":""AntiOfflineRaidMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""align"":""MiddleCenter"",""fontSize"":""19"",""text"":""{protection_message}""},{""type"":""RectTransform"",""anchormax"":""1 1"",""anchormin"":""0 0""}]}]";

        #endregion
    }
}
