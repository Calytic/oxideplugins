using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TurretConfig", "Calytic", "1.0.1", ResourceId = 1418)]
    [Description("Change turret damage, accuracy, bullet speed, health, targeting, etc")]
    class TurretConfig : RustPlugin
    {
        private readonly string turretPrefab = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private uint turretPrefabId;

        FieldInfo bulletDamageField = typeof(AutoTurret).GetField("bulletDamage", (BindingFlags.Instance | BindingFlags.NonPublic));

        FieldInfo healthField = typeof(BaseCombatEntity).GetField("_health", (BindingFlags.Instance | BindingFlags.NonPublic));
        FieldInfo maxHealthField = typeof(BaseCombatEntity).GetField("_maxHealth", (BindingFlags.Instance | BindingFlags.NonPublic));

        private bool adminOverride;
        private List<object> animals;
        private bool animalOverride;
        private bool sleepOverride;
        private bool useGlobalDamageModifier;
        private float globalDamageModifier;

        private float defaultBulletDamage;
        private float defaultBulletSpeed;
        private string defaultAmmoType;
        private float defaultSightRange;
        private float defaultHealth;
        private float defaultAimCone;

        private Dictionary<string, object> bulletDamages;
        private Dictionary<string, object> bulletSpeeds;
        private Dictionary<string, object> ammoTypes;
        private Dictionary<string, object> sightRanges;
        private Dictionary<string, object> healths;
        private Dictionary<string, object> aimCones;

        private bool infiniteAmmo;

        [PluginReference]
        Plugin Vanish;

        [PluginReference]
        Plugin Skills;

        void Loaded()
        {
            LoadMessages();
            LoadData();

            turretPrefabId = StringPool.Get(turretPrefab);

            permission.RegisterPermission("turretconfig.infiniteammo", this);

            adminOverride = GetConfig("Settings", "adminOverride", true);
            animalOverride = GetConfig("Settings", "animalOverride", false);
            sleepOverride = GetConfig("Settings", "sleepOverride", false);
            animals = GetConfig<List<object>>("Settings", "animals", GetPassiveAnimals());

            useGlobalDamageModifier = GetConfig("Settings", "useGlobalDamageModifier", false);
            globalDamageModifier = GetConfig("Settings", "globalDamageModifier", 1f);
            defaultHealth = GetConfig("Settings", "defaultHealth", 1000f);
            defaultAimCone = GetConfig("Settings", "defaultAimCone", 5f);
            defaultSightRange = GetConfig("Settings", "defaultSightRange", 30f);
            defaultBulletDamage = GetConfig("Settings", "defaultBulletDamage", 10f);
            defaultBulletSpeed = GetConfig("Settings", "defaultBulletSpeed", 10f);
            defaultAmmoType = GetConfig("Settings", "defaultAmmoType", "ammo.rifle");

            bulletDamages = GetConfig("Settings", "bulletDamages", GetDefaultBulletDamages());
            bulletSpeeds = GetConfig("Settings", "bulletSpeeds", GetDefaultBulletSpeeds());
            ammoTypes = GetConfig("Settings", "ammoTypes", GetDefaultAmmoTypes());
            sightRanges = GetConfig("Settings", "sightRanges", GetDefaultSightRanges());
            healths = GetConfig("Settings", "health", GetDefaultHealth());
            aimCones = GetConfig("Settings", "aimCones", GetDefaultAimCones());

            infiniteAmmo = GetConfig("Settings", "infiniteAmmo", false);

            foreach (KeyValuePair<string, object> kvp in bulletDamages)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            foreach (KeyValuePair<string, object> kvp in bulletSpeeds)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            foreach (KeyValuePair<string, object> kvp in ammoTypes)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            foreach (KeyValuePair<string, object> kvp in sightRanges)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            foreach (KeyValuePair<string, object> kvp in healths)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            foreach (KeyValuePair<string, object> kvp in aimCones)
            {
                if (!permission.PermissionExists(kvp.Key))
                {
                    permission.RegisterPermission(kvp.Key, this);
                }
            }

            LoadTurrets();
        }

        [ConsoleCommand("turrets.reload")]
        void ccTurretReload(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, GetMsg("Denied: Permission", arg.connection.userid.ToString()));
                    return;
                }
            }

            LoadTurrets();
        }

        protected void LoadTurrets() {
            AutoTurret[] turrets = GameObject.FindObjectsOfType<AutoTurret>();

            if (turrets.Length > 0)
            {
                int i = 0;
                foreach (AutoTurret turret in turrets.ToList())
                {
                    UpdateTurret(turret);
                    i++;
                }

                PrintWarning("Configured {0} turrets", i);
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new configuration");
            Config.Clear();

            Config["Settings", "defaultBulletDamage"] = 10f;
            Config["Settings", "defaultBulletSpeed"] = 200f;
            Config["Settings", "defaultAmmoType"] = "ammo.rifle";
            Config["Settings", "defaultSightRange"] = 30f;
            Config["Settings", "defaultHealth"] = 1000;
            Config["Settings", "defaultAimCone"] = 5f;

            Config["Settings", "adminOverride"] = true;
            Config["Settings", "sleepOverride"] = false;
            Config["Settings", "animalOverride"] = true;
            Config["Settings", "useGlobalDamageModifier"] = false;
            Config["Settings", "globalDamageModifier"] = 1f;

            Config["Settings", "animals"] = GetPassiveAnimals();
            Config["Settings", "infiniteAmmo"] = false;

            Config["Settings", "bulletDamages"] = GetDefaultBulletDamages();
            Config["Settings", "bulletSpeeds"] = GetDefaultBulletSpeeds();
            Config["Settings", "ammoTypes"] = GetDefaultAmmoTypes();
            Config["Settings", "sightRanges"] = GetDefaultSightRanges();
            Config["Settings", "health"] = GetDefaultHealth();
            Config["Settings", "aimCones"] = GetDefaultAimCones();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Denied: Permission", "You lack permission to do that"},
            }, this);
        }

        private List<object> GetPassiveAnimals()
        {
            return new List<object>
            {
                "stag",
                "boar",
                "chicken",
                "horse",
            };
        }

        private Dictionary<string, object> GetDefaultBulletDamages()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", 10f},
            };
        }

        private Dictionary<string, object> GetDefaultBulletSpeeds()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", 200f},
            };
        }

        private Dictionary<string, object> GetDefaultSightRanges()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", 30f},
            };
        }

        private Dictionary<string, object> GetDefaultAmmoTypes()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", "ammo.rifle"},
            };
        }

        private Dictionary<string, object> GetDefaultHealth()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", 1000f},
            };
        }

        private Dictionary<string, object> GetDefaultAimCones()
        {
            return new Dictionary<string, object>() {
                {"turretconfig.default", 5f},
            };
        }

        void LoadData()
        {
            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig("VERSION", Version.ToString()) != Version.ToString())
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

            PrintWarning("Upgrading Configuration File");
            SaveConfig();
        }

        void OnLootEntity(BasePlayer looter, BaseEntity target)
        {
            if (!infiniteAmmo) return;
            if (!permission.UserHasPermission(target.OwnerID.ToString(), "turretconfig.infiniteammo")) return;

            if (target is AutoTurret)
            {
                timer.Once(0.01f, looter.EndLooting);
            }
        }

        private void OnConsumableUse(Item item, int amount)
        {
            if (!infiniteAmmo) return;
            

            var entity = item.parent?.entityOwner as AutoTurret;
            if (entity != null) {
                if (!permission.UserHasPermission(entity.OwnerID.ToString(), "turretconfig.infiniteammo")) return;
                item.amount++;
            }
        }

        private void CheckAmmo(AutoTurret turret)
        {
            if (!infiniteAmmo) return;
            if (!permission.UserHasPermission(turret.OwnerID.ToString(), "turretconfig.infiniteammo")) return;

            var items = new List<Item>();
            var projectile = turret.ammoType.GetComponent<ItemModProjectile>();
            turret.inventory.FindAmmo(items, projectile.ammoType);

            int total = items.Sum(x => x.amount);

            if (total < 1)
            {
                turret.inventory.AddItem(turret.ammoType, 1);
            }
        }

        private object CanBeTargeted(BaseCombatEntity target, MonoBehaviour turret)
        {
            if(target is BasePlayer) {
                var isInvisible = Vanish?.Call("IsInvisible", target);
                if (isInvisible != null && (bool)isInvisible)
                {
                    return null;
                }

                var isStealthed = Skills?.Call("isStealthed", target);
                if (isStealthed != null && (bool)isStealthed)
                {
                    return null;
                }
            }

            if (!(turret is AutoTurret))
            {
                return null;
            }

            if (animalOverride == true && target.GetComponent<BaseNPC>() != null)
            {
                if(animals.Count > 0) {
                    if(animals.Contains(target.ShortPrefabName.Replace(".prefab","").ToLower())) {
                        return false;
                    } else {
                        return null;
                    }
                } else {
                    return false;
                }
            }

            if (target.ToPlayer() == null)
            {
                return null;
            }

            BasePlayer targetPlayer = target.ToPlayer();

            if (adminOverride && targetPlayer.IsConnected() && targetPlayer.net.connection.authLevel > 0)
            {
                return false;
            } 
            else if(sleepOverride && targetPlayer.IsSleeping()) 
            {
                return false;
            }

            return null;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            
            if (entity.prefabID == turretPrefabId)
            {
                UpdateTurret((AutoTurret)entity, true);
            }
        }

        float GetBulletDamage(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in bulletDamages)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return Convert.ToSingle(kvp.Value);
                    }
                }
            }

            return defaultBulletDamage;
        }

        float GetHealth(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in healths)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return Convert.ToSingle(kvp.Value);
                    }
                }
            }

            return defaultHealth;
        }

        float GetBulletSpeed(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in bulletSpeeds)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return Convert.ToSingle(kvp.Value);
                    }
                }
            }

            return defaultBulletSpeed;
        }

        float GetSightRange(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in sightRanges)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return Convert.ToSingle(kvp.Value);
                    }
                }
            }

            return defaultSightRange;
        }

        float GetAimCone(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in aimCones)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return Convert.ToSingle(kvp.Value);
                    }
                }
            }

            return defaultAimCone;
        }

        string GetAmmoType(string userID)
        {
            if (!string.IsNullOrEmpty(userID) && userID != "0")
            {
                foreach (KeyValuePair<string, object> kvp in ammoTypes)
                {
                    if (permission.UserHasPermission(userID, kvp.Key))
                    {
                        return kvp.Value.ToString();
                    }
                }
            }

            return defaultAmmoType;
        }

        private void UpdateTurret(AutoTurret turret, bool justCreated = false)
        {
            CheckAmmo(turret);

            string userID = turret.OwnerID.ToString();

            float turretHealth = GetHealth(userID);
            string ammoType = GetAmmoType(userID);

            bulletDamageField.SetValue(turret, GetBulletDamage(userID));
            if (justCreated)
            {
                healthField.SetValue(turret, turretHealth);
            }
            maxHealthField.SetValue(turret, turretHealth);

            if (justCreated)
            {
                turret.InitializeHealth(turretHealth, turretHealth);
            }
            else
            {
                turret.InitializeHealth(turret.health, turretHealth);
            }
            
            turret.bulletSpeed = GetBulletSpeed(userID);
            turret.sightRange = GetSightRange(userID);
            turret.startHealth = turretHealth;
            turret.aimCone = GetAimCone(userID);

            var def = ItemManager.FindItemDefinition(ammoType);
            if (def is ItemDefinition)
            {
                turret.ammoType = def;
                ItemModProjectile projectile = def.GetComponent<ItemModProjectile>();
                if (projectile is ItemModProjectile)
                {
                    turret.gun_fire_effect.guid = projectile.projectileObject.guid;
                    turret.bulletEffect.guid = projectile.projectileObject.guid;
                }
            }
            else
            {
                PrintWarning("No ammo of type ({0})", ammoType);
            }

            turret.Reload();

            //turret.enableSaving = false;
            //turret.ServerInit();
            turret.SendNetworkUpdateImmediate(justCreated);
            
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (attacker == null || hitInfo == null || hitInfo.HitEntity == null) return;

            if (hitInfo.HitEntity.prefabID == 3268886773)
            {
                if (useGlobalDamageModifier)
                {
                    hitInfo.damageTypes.ScaleAll(globalDamageModifier);
                    return;
                }
            }
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
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

        string GetMsg(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }
    }
}
