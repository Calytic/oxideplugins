using System;
using System.Collections.Generic;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Martyrdom", "k1lly0u", "0.1.2", ResourceId = 1523)]
    class Martyrdom : RustPlugin
    {
        private bool Changed;
        private Dictionary<ulong, bool> lsToggle = new Dictionary<ulong, bool>();

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("martyrdom.grenade", this);
            permission.RegisterPermission("martyrdom.beancan", this);
            permission.RegisterPermission("martyrdom.explosive", this);
            lang.RegisterMessages(messages, this);
            LoadVariables();
        }
        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            lsToggle.Clear();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        
        float grenadeRadius = 5f;
        float grenadeDamage = 75f;
        float beancanRadius = 4f;
        float beancanDamage = 30f;
        float explosiveRadius = 10f;
        float explosiveDamage = 110f;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {            
            CheckCfgFloat("Options - Damage - Grenade", ref grenadeDamage);
            CheckCfgFloat("Options - Damage - Beancan Grenade", ref beancanDamage);
            CheckCfgFloat("Options - Damage - Timed Explosive", ref explosiveDamage);
            CheckCfgFloat("Options - Radius - Grenade", ref grenadeRadius);
            CheckCfgFloat("Options - Radius - Beancan Grenade", ref beancanRadius);
            CheckCfgFloat("Options - Radius - Timed Explosive", ref explosiveRadius);
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

        //////////////////////////////////////////////////////////////////////////////////////
        // Messages //////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            {"onOff", "<color=orange>Martyrdom</color> is {0}!" },
            {"badSyntax", "<color=orange>Martyrdom</color> : Incorrect syntax:" },
            {"exSyntax", "\"martyrdom\" \"on\" -or- \"off\"" }
        };

        //////////////////////////////////////////////////////////////////////////////////////
        // Martyrdom /////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        void OnEntityDeath(BaseEntity victim, HitInfo hitInfo)
        {
            if (victim == null) return;
            if (victim is BasePlayer)
            {
                BasePlayer player = (BasePlayer)victim;

                if (lsToggle.ContainsKey(player.userID) && (lsToggle[player.userID] == true))
                {
                    Vector3 deathPos = player.transform.position;
                    List<string> playerItems = new List<string>();

                    foreach (Item item in player.inventory.containerBelt.itemList)
                    {
                        playerItems.Add(item.info.shortname);
                    }

                    if ((playerItems.Contains("grenade.f1")) && (canMartyrdomGrenade(player)))
                    {
                        player.inventory.Take(null, 1308622549, 1);
                        dropGrenade(deathPos);
                    }
                    else if ((playerItems.Contains("grenade.beancan")) && (canMartyrdomBeancan(player)))
                    {
                        player.inventory.Take(null, 384204160, 1);
                        dropBeancan(deathPos);
                    }
                    else if ((playerItems.Contains("explosive.timed")) && (canMartyrdomExplosive(player)))
                    {
                        player.inventory.Take(null, 498591726, 1);
                        dropExplosive(deathPos);
                    }
                    playerItems.Clear();
                }
            }
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

        //////////////////////////////////////////////////////////////////////////////////////
        // Permission/Auth Check /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool canMartyrdomGrenade(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "martyrdom.grenade")) return true;
            else if (player.net.connection.authLevel >= 1) return true;
            return false;
        }
        bool canMartyrdomBeancan(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "martyrdom.beancan")) return true;
            else if (player.net.connection.authLevel >= 1) return true;
            return false;
        }
        bool canMartyrdomExplosive(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "martyrdom.explosive")) return true;
            else if (player.net.connection.authLevel >= 1) return true;
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        [ChatCommand("martyrdom")]
        private void chatToggleStrike(BasePlayer player, string command, string[] args)
        {            
            if (!lsToggle.ContainsKey(player.userID)) lsToggle.Add(player.userID, false);

            if ((!canMartyrdomBeancan(player)) || (!canMartyrdomExplosive(player)) || (!canMartyrdomGrenade(player))) return;

            string reply = "";
            if (lsToggle[player.userID] == true) reply = "ON";
            else reply = "OFF";

            if (args.Length == 0)
            {
                SendReply(player, string.Format(lang.GetMessage("onOff", this, player.UserIDString), reply));
                return;
            }
            else if (args.Length == 1)
            {
                var toggleString = args[0].ToUpper();
                if (toggleString != "ON")
                {
                    reply = "OFF";
                    lsToggle[player.userID] = false;
                    SendReply(player, string.Format(lang.GetMessage("onOff", this, player.UserIDString), reply));
                    return;
                }
                if (toggleString == "ON")
                {
                    lsToggle[player.userID] = true;
                    SendReply(player, string.Format(lang.GetMessage("onOff", this, player.UserIDString), toggleString));
                    return;
                }
            }
            else if (args.Length > 1)
            {
                SendReply(player, lang.GetMessage("badSyntax", this, player.UserIDString));
                SendReply(player, lang.GetMessage("exSyntax", this, player.UserIDString));
                return;
            }

        }
    }
}
