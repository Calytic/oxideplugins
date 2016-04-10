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
    [Info("AntiOfflineRaid", "Calytic", "0.1.5", ResourceId = 1464)]
    public class AntiOfflineRaid : RustPlugin
    {
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
        private string protectionMessage;

        private bool clanShare;
        private int minMembers;

        Timer lastOnlineTimer;

        List<object> prefabs;

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
                    return DateTime.FromBinary(this.lastOnlineLong);
                }

                set
                {
                    this.lastOnlineLong = value.ToBinary();
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
                    return BasePlayer.FindByID(this.userid);
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

            public double Days
            {
                get
                {
                    TimeSpan ts = DateTime.Now - this.lastOnline;
                    return ts.TotalDays;
                }
            }

            public bool HasDays(int days)
            {
                if (this.Days >= days)
                {
                    return true;
                }

                return false;
            }

            public double Minutes
            {
                get
                {
                    TimeSpan ts = DateTime.Now - this.lastOnline;
                    return ts.TotalMinutes;
                }
            }

            public bool HasMinutes(int minutes)
            {
                if (this.Minutes >= minutes)
                {
                    return true;
                }

                return false;
            }

            public double Hours
            {
                get
                {
                    TimeSpan ts = DateTime.Now - this.lastOnline;
                    return ts.TotalHours;
                }
            }

            public bool HasHours(int hours)
            {
                DateTime start = DateTime.Now;

                TimeSpan ts = start - this.lastOnline;

                if (this.Hours >= hours)
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

                this.lastPosition = new Vector3(position.x, position.y, position.z);

                return equal;
            }
        }

        void OnServerInitialized()
        {
            permission.RegisterPermission("antiofflineraid.protect", this);
            permission.RegisterPermission("antiofflineraid.check", this);

            this.LoadData();

            damageScale = GetConfig<Dictionary<string, object>>("damageScale", GetDefaultReduction());
            prefabs = GetConfig<List<object>>("prefabs", GetDefaultPrefabs());
            afkMinutes = GetConfig<int>("afkMinutes", 5);
            cooldownMinutes = GetConfig<int>("cooldownMinutes", 10);
            interimDamage = GetConfig<float>("interimDamage", 0f);

            clanShare = GetConfig<bool>("clanShare", false);
            minMembers = GetConfig<int>("minMembers", 1);
            showMessage = GetConfig<bool>("showMessage", true);
            playSound = GetConfig<bool>("playSound", false);
            sound = GetConfig<string>("sound", "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab");
            protectionMessage = GetConfig<string>("protectionMessage", "This building is protected: {amount}%");

            if (clanShare)
            {
                if (!plugins.Exists("Clans"))
                {
                    this.clanShare = false;
                    PrintWarning("Clans not found! clanShare disabled. Cannot use clanShare without this plugin. http://oxidemod.org/plugins/rust-io-clans.842/");
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
            Config["showMessage"] = true;
            Config["playSound"] = false;
            Config["prefabs"] = GetDefaultPrefabs();
            Config["sound"] = "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab";
            Config["protectionMessage"] = "This building is protected: {amount}%";
            Config["VERSION"] = this.Version.ToString();
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = this.Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            Config["playSound"] = false;
            Config["sound"] = "assets/prefabs/weapon mods/silencers/effects/silencer_attach.fx.prefab";
            Config["prefabs"] = GetDefaultPrefabs();
            // END NEW CONFIGURATION OPTIONS

            PrintWarning("Upgrading configuration file");
            SaveConfig();
        }

        void OnServerSave()
        {
            this.SaveData();
        }

        void OnServerShutdown()
        {
            this.SaveData();
        }

        //void Unload()
        //{
        //    this.SaveData();
        //}

        void LoadData()
        {
            this.lastOnline = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, LastOnline>>("antiofflineraid");

            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", "") != this.Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<Dictionary<ulong, LastOnline>>("antiofflineraid", this.lastOnline);
        }

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

            if (lastOnline.ContainsKey(player.userID) && lastOnline[player.userID] != null && input.current.buttons != 0 && !input.previous.Equals(input.current))
            {
                lastOnline[player.userID].afkMinutes = 0;
            }
        }

        private void UpdateLastOnlineAll()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (!player.IsConnected())
                    continue;


                bool hasMoved = true;
                if (lastOnline.ContainsKey(player.userID))
                {
                    if (!lastOnline[player.userID].HasMoved(player.transform.position))
                    {
                        hasMoved = false;
                        lastOnline[player.userID].afkMinutes += tickRate;
                    }

                    if (lastOnline[player.userID].IsAFK())
                    {
                        continue;
                    }
                } 

                this.UpdateLastOnline(player, hasMoved);
            }
        }

        private void UpdateLastOnline(BasePlayer player, bool hasMoved = true)
        {
            if (!lastOnline.ContainsKey(player.userID))
            {
                lastOnline.Add(player.userID, new LastOnline(player, DateTime.Now));
            }
            else
            {
                lastOnline[player.userID].lastOnline = DateTime.Now;
                if (hasMoved)
                {
                    lastOnline[player.userID].afkMinutes = 0;
                }
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null)
            {
                return;
            }

            if (hitInfo.WeaponPrefab != null && hitInfo.Initiator != null && IsBlocked(entity))
            {
                this.OnStructureAttack(entity, hitInfo.Initiator, hitInfo);
            }
        }

        void OnStructureAttack(BaseEntity entity, BaseEntity attackerEntity, HitInfo hitinfo)
        {
            ulong targetID = 0;
            string weapon = hitinfo.WeaponPrefab.LookupShortPrefabName();

            targetID = FindOwner(entity);
            if (targetID != 0 && HasPerm(targetID.ToString(), "antiofflineraid.protect"))
            {
                if (!lastOnline.ContainsKey(targetID))
                {
                    return;
                }

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
                if (scale == 0)
                {
                    // completely cancel damage
                    hitinfo.damageTypes = new DamageTypeList();
                    hitinfo.DoHitEffects = false;
                    hitinfo.HitMaterial = 0;
                    if (this.showMessage)
                    {
                        this.sendMessage(hitinfo);
                    }

                    if (playSound)
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
                        if(this.showMessage) 
                        {
                            this.sendMessage(hitinfo, 100 - Convert.ToInt32(scale * 100));
                        }

                        if (playSound)
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

            if (!lastOnline.ContainsKey(targetID))
            {
                return -1;
            }

            if (!lastOnline[targetID].IsOffline())
            {
                // must be logged out for atleast x minutes before damage is reduced
                return -1;
            }
            else
            {
                if (lastOnline[targetID].HasMinutes(60))
                {
                    // if you've been offline/afk for more than an hour, use hourly scales
                    var keys = damageScale.Keys.Select(int.Parse).ToList();
                    keys.Sort();

                    foreach (int key in keys)
                    {
                        if (lastOnline[targetID].HasHours(key))
                        {
                            scale = Convert.ToSingle(damageScale[key.ToString()]);
                        }
                    }
                }
                else
                {
                    // if you have been offline for more than x minutes but less than an hour, cancel damage completely
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

            string prefabName = entity.LookupShortPrefabName();

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
            if (lastOnline.ContainsKey(playerID))
            {
                return lastOnline[playerID].IsOffline();
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

        public bool IsClanOffline(ulong targetID)
        {
            int mcount = this.getClanMembersOnline(targetID);

            if (mcount >= minMembers)
            {
                return false;
            }

            return true;
        }

        public int getClanMembersOnline(ulong targetID)
        {
            var player = covalence.Players.GetPlayer(targetID.ToString());
            var start = (player.ConnectedPlayer == null) ? 0 : 1;
            string tag = Clans.Call<string>("GetClanOf", targetID);
            if (tag == null)
            {
                return start;
            }

            JObject clan = Clans.Call<JObject>("GetClan", tag);

            if (clan == null)
            {
                return start;
            }

            int mcount = start;

            foreach (string memberid in clan["members"])
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

        ulong FindOwner(BaseEntity entity)
        {
            return entity.OwnerID;
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        [ConsoleCommand("ao")]
        private void ccStatus(ConsoleSystem.Arg arg)
        {
            if (arg.connection.player is BasePlayer)
            {
                if (!HasPerm(arg.connection.player as BasePlayer, "antiofflineraid.check") && arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You lack the permission to do that");
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
                SendReply(player, "You lack the permission to do that");
                return;
            }

            SendReply(player, SendStatus(player.net.connection, args));
        }

        //[ChatCommand("boffline")]
        //private void cmdboffline(BasePlayer player, string command, string[] args)
        //{
        //    this.lastOnline[player.userID].lastOnline = this.lastOnline[player.userID].lastOnline.Subtract(TimeSpan.FromHours(3));
        //}

        private string SendStatus(Network.Connection connection, string[] args)
        {
            if (connection.authLevel < 1)
            {
                return "You lack the permission to do that";
            }

            if (args.Length == 1)
            {
                BasePlayer target = FindPlayerByPartialName(args[0]);
                if (target is BasePlayer && lastOnline.ContainsKey(target.userID))
                {
                    LastOnline lo = lastOnline[target.userID];

                    StringBuilder sb = new StringBuilder();

                    if (IsOffline(target.userID))
                    {
                        sb.AppendLine("<color=red><size=15>AntiOfflineRaid Status</size></color>: " + target.displayName);
                        sb.AppendLine("<color=lightblue>Player Status</color>: <color=red>Offline</color>: " + lo.lastOnline.ToString());
                    }
                    else
                    {
                        sb.AppendLine("<color=lime><size=15>AntiOfflineRaid Status</size></color>: " + target.displayName);
                        sb.AppendLine("<color=lightblue>Player Status</color>: <color=lime>Online</color>");
                    }
                    sb.AppendLine("<color=lightblue>AFK</color>: " + lo.afkMinutes + " minutes");
                    if (clanShare)
                    {
                        sb.AppendLine("<color=lightblue>Clan Status</color>: " + (IsClanOffline(target.userID) ? "<color=red>Offline</color>" : "<color=lime>Online</color>") + " (" + this.getClanMembersOnline(target.userID) + ")");
                    }

                    float scale = scaleDamage(target.userID);
                    if (scale != -1) { 
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

        protected static BasePlayer FindPlayerByPartialName(string nameOrIdOrIp)
        {
            if (string.IsNullOrEmpty(nameOrIdOrIp))
                return null;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.net == null) continue;
                if (activePlayer.net.connection == null) continue;
                if (activePlayer.userID.ToString() == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.userID.ToString() == nameOrIdOrIp)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
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

        private void HideMessage(BasePlayer player)
        {
            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList("AntiOfflineRaidMsg"));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", obj);
        }

        private void ShowMessage(BasePlayer player, int amount = 100)
        {
            this.HideMessage(player);
            string send = this.jsonMessage;
            send = send.Replace("{1}", Oxide.Core.Random.Range(1, 99999).ToString());
            send = send.Replace("{protection_message}", protectionMessage);
            send = send.Replace("{amount}", amount.ToString());
            var obj = new Facepunch.ObjectList?(new Facepunch.ObjectList(send));
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", obj);

            timer.In(3f, delegate()
            {
                this.HideMessage(player);
            });
        }

        private string jsonMessage = @"[{""name"":""AntiOfflineRaidMsg"",""parent"":""HUD/Overlay"",""components"":[{""type"":""UnityEngine.UI.Image"",""color"":""0 0 0 0.78""},{""type"":""RectTransform"",""anchormax"":""0.64 0.88"",""anchormin"":""0.38 0.79""}]},{""name"":""MessageLabel{1}"",""parent"":""AntiOfflineRaidMsg"",""components"":[{""type"":""UnityEngine.UI.Text"",""align"":""MiddleCenter"",""fontSize"":""19"",""text"":""{protection_message}""},{""type"":""RectTransform"",""anchormax"":""1 1"",""anchormin"":""0 0""}]}]";
    }
}
