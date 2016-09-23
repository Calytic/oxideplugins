using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Martyrdom", "k1lly0u", "0.2.0", ResourceId = 1523)]
    class Martyrdom : RustPlugin
    {
        #region Fields
        private Dictionary<ulong, EType> Martyrs;
        Dictionary<EType, ExplosiveInfo> Explosives;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            Martyrs = new Dictionary<ulong, EType>();
            Explosives = new Dictionary<EType, ExplosiveInfo>();
            permission.RegisterPermission("martyrdom.grenade", this);
            permission.RegisterPermission("martyrdom.beancan", this);
            permission.RegisterPermission("martyrdom.explosive", this);
            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            SetExplosiveInfo();
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
            {
                var victim = entity.ToPlayer();
                if (Martyrs.ContainsKey(victim.userID))
                {
                    TryDropExplosive(victim);
                }
            }
        }
        #endregion

        #region Functions
        void TryDropExplosive(BasePlayer player)
        {
            var type = Martyrs[player.userID];
            if (HasPerm(player, type))
            {
                if (HasEnoughRes(player, Explosives[type].ItemID, 1))
                {
                    TakeResources(player, Explosives[type].ItemID, 1);
                    CreateExplosive(type, player);
                    Martyrs.Remove(player.userID);
                    return;
                }
            }            
        }
        void CreateExplosive(EType type, BasePlayer player)
        {
            var Details = Explosives[type];
            var expEnt = GameManager.server.CreateEntity(Details.PrefabName, player.transform.position + new Vector3(0, 1.5f, 0), new Quaternion(), true);
            expEnt.OwnerID = player.userID;
            expEnt.creatorEntity = player;
            TimedExplosive explosive = expEnt.GetComponent<TimedExplosive>();
            explosive.timerAmountMax = Details.Fuse;
            explosive.timerAmountMin = Details.Fuse;
            explosive.explosionRadius = Details.Radius;
            explosive.damageTypes = new List<Rust.DamageTypeEntry> { new Rust.DamageTypeEntry {amount = Details.Damage, type = Rust.DamageType.Explosion } };
            explosive.Spawn();
        }
        #endregion

        #region Helpers
        private bool HasEnoughRes(BasePlayer player, int itemid, int amount) => player.inventory.GetAmount(itemid) >= amount;
        private void TakeResources(BasePlayer player, int itemid, int amount) => player.inventory.Take(null, itemid, amount);
        private bool HasPerm(BasePlayer player, EType type)
        {
            switch (type)
            {
                case EType.Grenade:
                    return permission.UserHasPermission(player.UserIDString, "martyrdom.grenade") || player.IsAdmin();
                case EType.Beancan:
                    return permission.UserHasPermission(player.UserIDString, "martyrdom.beancan") || player.IsAdmin();
                case EType.Explosive:
                    return permission.UserHasPermission(player.UserIDString, "martyrdom.explosive") || player.IsAdmin();                
            }
            return false;
        }
        private bool HasAnyPerm(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "martyrdom.grenade") || permission.UserHasPermission(player.UserIDString, "martyrdom.beancan") || permission.UserHasPermission(player.UserIDString, "martyrdom.explosive") || player.IsAdmin();
        #endregion

        #region Explosive Info       
        void SetExplosiveInfo()
        {
            if (configData.Beancan.Activated)
                Explosives.Add(EType.Beancan, new ExplosiveInfo { ItemID = 384204160, PrefabName = "assets/prefabs/weapons/beancan grenade/grenade.beancan.deployed.prefab", Damage = configData.Beancan.Damage, Fuse = configData.Beancan.Fuse, Radius = configData.Beancan.Radius });
            if (configData.Grenade.Activated)
                Explosives.Add(EType.Grenade, new ExplosiveInfo { ItemID = -1308622549, PrefabName = "assets/prefabs/weapons/f1 grenade/grenade.f1.deployed.prefab", Damage = configData.Grenade.Damage, Fuse = configData.Grenade.Fuse, Radius = configData.Grenade.Radius });
            if (configData.Explosive.Activated)
                Explosives.Add(EType.Explosive, new ExplosiveInfo { ItemID = 498591726, PrefabName = "assets/prefabs/tools/c4/explosive.timed.deployed.prefab", Damage = configData.Explosive.Damage, Fuse = configData.Explosive.Fuse, Radius = configData.Explosive.Radius });
        }

        class ExplosiveInfo
        {
            public int ItemID;
            public string PrefabName;
            public float Damage;
            public float Radius;
            public float Fuse;
        }
        enum EType
        {
            Grenade,
            Beancan,
            Explosive
        }
        #endregion

        #region Chat Commands        
        [ChatCommand("m")]
        void cmdM(BasePlayer player, string command, string[] args)
        {
            if (!HasAnyPerm(player)) return;
            if (args == null || args.Length == 0)
            {
                if (HasPerm(player, EType.Beancan) && configData.Beancan.Activated)
                    SendReply(player, "/m beancan");
                if (HasPerm(player, EType.Grenade) && configData.Grenade.Activated)
                    SendReply(player, "/m grenade");
                if (HasPerm(player, EType.Explosive) && configData.Explosive.Activated)
                    SendReply(player, "/m explosive");
                SendReply(player, "/m disable");
                return;
            }
            switch (args[0].ToLower())
            {
                case "beancan":
                    if (HasPerm(player, EType.Beancan))
                    {
                        if (!Martyrs.ContainsKey(player.userID))
                            Martyrs.Add(player.userID, EType.Beancan);
                        else Martyrs[player.userID] = EType.Beancan;
                        SendReply(player, msg("beanAct", player.UserIDString));
                    }
                    return;
                case "grenade":
                    if (HasPerm(player, EType.Grenade))
                    {
                        if (!Martyrs.ContainsKey(player.userID))
                            Martyrs.Add(player.userID, EType.Grenade);
                        else Martyrs[player.userID] = EType.Grenade;
                        SendReply(player, msg("grenAct", player.UserIDString));
                    }
                    return;
                case "explosive":
                    if (HasPerm(player, EType.Explosive))
                    {
                        if (!Martyrs.ContainsKey(player.userID))
                            Martyrs.Add(player.userID, EType.Explosive);
                        else Martyrs[player.userID] = EType.Explosive;
                        SendReply(player, msg("expAct", player.UserIDString));
                    }
                    return;
                case "disable":
                    if (Martyrs.ContainsKey(player.userID))
                    {
                        Martyrs.Remove(player.userID);
                        SendReply(player, msg("marDis", player.UserIDString));
                        return;
                    }
                    else SendReply(player, msg("notAct", player.UserIDString));
                    return;
                default:
                    break;
            }

        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ExpType
        {
            public bool Activated { get; set; }
            public float Damage { get; set; }
            public float Radius { get; set; }
            public float Fuse { get; set; }
        }
        class ConfigData
        {
            public ExpType Grenade { get; set; }
            public ExpType Beancan { get; set; }
            public ExpType Explosive { get; set; }
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
                Beancan = new ExpType
                {
                    Activated = true,
                    Damage = 15f,
                    Fuse = 2f,
                    Radius = 4.5f
                },
                Grenade = new ExpType
                {
                    Activated = true,
                    Damage = 40f,
                    Fuse = 2f,
                    Radius = 4.5f
                },
                Explosive = new ExpType
                {
                    Activated = true,
                    Damage = 500,
                    Fuse = 3,
                    Radius = 10f
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Localization
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"beanAct", "You have activated the beancan Martyr drop" },
            {"grenAct", "You have activated the grenade Martyr drop" },
            {"expAct", "You have activated the explosive Martyr drop" },
            {"marDis", "You have disabled Martyrdom" },
            {"notAct", "You do not have Martyrdom activated" }
        };

        #endregion
    }
}
