using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TurretConfig", "Calytic", "0.1.8", ResourceId = 1418)]
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

        private float bulletDamage;
        private float bulletSpeed;
        private string ammoType;
        private float sightRange;

        private bool useGlobalDamageModifier;
        private float globalDamageModifier;
        private float health;
        private float aimCone;

        private bool infiniteAmmo;

        [PluginReference]
        Plugin Vanish;

        [PluginReference]
        Plugin Skills;

        void Init()
        {
            LoadData();

            turretPrefabId = StringPool.Get(turretPrefab);

            permission.RegisterPermission("turretconfig.infiniteammo", this);

            bulletDamage = GetConfig("bulletDamage", 10f);
            bulletSpeed = GetConfig("bulletSpeed", 10f);
            ammoType = GetConfig("ammoType", "ammo.rifle");
            adminOverride = GetConfig("adminOverride", true);
            animalOverride = GetConfig("animalOverride", false);
            sleepOverride = GetConfig("sleepOverride", false);
            animals = GetConfig<List<object>>("animals", GetPassiveAnimals());

            useGlobalDamageModifier = GetConfig("useGlobalDamageModifier", false);
            globalDamageModifier = GetConfig("globalDamageModifier", 1f);
            health = GetConfig("health", 750f);
            aimCone = GetConfig("aimCone", 5f);
            sightRange = GetConfig("sightRange", 30f);

            infiniteAmmo = GetConfig("infiniteAmmo", false);

            LoadTurrets();
        }

        [ConsoleCommand("turrets.reload")]
        void ccTurretReload(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null)
            {
                if (arg.connection.authLevel < 1)
                {
                    SendReply(arg, "You are not allowed to use this command");
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

            Config["bulletDamage"] = 10f;
            Config["bulletSpeed"] = 200f;
            Config["ammoType"] = "ammo.rifle";
            Config["sightRange"] = 30f;
            Config["adminOverride"] = true;
            Config["sleepOverride"] = false;
            Config["animalOverride"] = true;
            Config["useGlobalDamageModifier"] = false;
            Config["globalDamageModifier"] = 1f;
            Config["health"] = 750f;
            Config["aimCone"] = 5f;
            Config["animals"] = GetPassiveAnimals();
            Config["infiniteAmmo"] = false;
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

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(Config[name], typeof(T));
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

            if(targetPlayer.IsAlive() && !targetPlayer.IsSleeping()) {
                var isInvisible = Vanish?.Call("IsInvisible", target);
                if (isInvisible != null && (bool)isInvisible)
                {
                    return null;
                }

                var isStealthed = Skills?.Call("isStealthed", target);
                if (isStealthed != null && (bool)isStealthed)
                {
                    return false;
                }
            }

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

        private void UpdateTurret(AutoTurret turret, bool justCreated = false)
        {
            CheckAmmo(turret);

            bulletDamageField.SetValue(turret, bulletDamage);
            if (justCreated)
            {
                healthField.SetValue(turret, health);
            }
            maxHealthField.SetValue(turret, health);

            if (justCreated)
            {
                turret.InitializeHealth(health, health);
            }
            else
            {
                turret.InitializeHealth(turret.health, health);
            }
            
            turret.bulletSpeed = bulletSpeed;
            turret.sightRange = sightRange;
            turret.startHealth = health;
            turret.aimCone = aimCone;

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
    }
}
