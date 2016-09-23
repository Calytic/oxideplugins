using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AntiRaidTower", "Calytic @ RustServers.IO", "0.2.2", ResourceId = 1211)]
    [Description("Building/deployable height limit, high jump instant death, no wounded teleport")]
    class AntiRaidTower : RustPlugin
    {
        int FallKill;
        bool BuildingBlockHeight;
        bool DeployableBlockHeight;
        int BuildingMaxHeight;
        int DeployableMaxHeight;

        void OnServerInitialized()
        {
            permission.RegisterPermission("antiraidtower.blockheightbypass", this);
            permission.RegisterPermission("antiraidtower.deployheightbypass", this);
            permission.RegisterPermission("antiraidtower.fallkillbypass", this);
            permission.RegisterPermission("antiraidtower.woundedbypass", this);
            LoadMessages();
            LoadData();

            FallKill = GetConfig("FallKill", 215);
            BuildingBlockHeight = GetConfig("BuildingBlockHeight", true);
            DeployableBlockHeight = GetConfig("DeployableBlockHeight", true);
            BuildingMaxHeight = GetConfig("BuildingMaxHeight", 50);
            DeployableMaxHeight = GetConfig("DeployableMaxHeight", 50);
        }

        protected override void LoadDefaultConfig()
        {
            Config["FallKill"] = 215;
            Config["BuildingBlockHeight"] = true;
            Config["BuildingMaxHeight"] = 50;
            Config["DeployableBlockHeight"] = true;
            Config["DeployableMaxHeight"] = 50;

            Config["VERSION"] = Version.ToString();
        }

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Denied: Height", "Too far from ground: {0}m"},
                {"Denied: Wounded", "You may not teleport while wounded"},
            }, this);
        }

        void LoadData()
        {
            if (Config["VERSION"] == null)
            {
                // FOR COMPATIBILITY WITH INITIAL VERSIONS WITHOUT VERSIONED CONFIG
                ReloadConfig();
            }
            else if (GetConfig<string>("VERSION", Version.ToString()) != Version.ToString())
            {
                // ADDS NEW, IF ANY, CONFIGURATION OPTIONS
                ReloadConfig();
            }
        }

        protected void ReloadConfig()
        {
            Config["VERSION"] = Version.ToString();

            // NEW CONFIGURATION OPTIONS HERE
            Config["BuildingBlockHeight"] = true;
            Config["BuildingMaxHeight"] = 50;
            Config["DeployableBlockHeight"] = true;
            Config["DeployableMaxHeight"] = 50;
            // END NEW CONFIGURATION OPTIONS

            PrintToConsole("Upgrading configuration file");
            SaveConfig();
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (FallKill == 0)
            {
                return;
            }

            if (entity is BasePlayer)
            {
                var player = (BasePlayer)entity;
                if (permission.UserHasPermission(player.UserIDString, "antiraidtower.fallkillbypass"))
                {
                    return;
                }
                if (player.IsConnected() && player.net.connection.authLevel > 0)
                {
                    return;
                }
                var dt = hitinfo.damageTypes.GetMajorityDamageType();
                float amt = hitinfo.damageTypes.Get(dt);
                if (dt == DamageType.Fall)
                {
                    float fallkill = Convert.ToSingle(FallKill);
                    if (amt > fallkill)
                    {
                        player.Die();
                    }
                }
            }
        }

        object CanTeleport(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "antiraidtower.woundedbypass"))
            {
                return null;
            }
            if (player.IsWounded()) { return GetMsg("Denied: Wounded", player.UserIDString); }
            return null;
        }

        object canTeleport(BasePlayer player)
        {
            return CanTeleport(player);
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null) return;
            if (!BuildingBlockHeight && !DeployableBlockHeight) return;

            var MaxHeight = 0;
            var entity = gameObject.GetComponent<BaseCombatEntity>();
            if(entity is BuildingBlock) {
                if (permission.UserHasPermission(player.UserIDString, "antiraidtower.blockheightbypass"))
                {
                    return;
                }
                MaxHeight = BuildingMaxHeight;
            }
            else
            {
                if (permission.UserHasPermission(player.UserIDString, "antiraidtower.deployheightbypass"))
                {
                    return;
                }
                MaxHeight = DeployableMaxHeight;
            }

            RaycastHit hitInfo;
            if (entity != null && Physics.Raycast(new Ray(entity.transform.position, Vector3.down), out hitInfo, float.PositiveInfinity, Rust.Layers.Terrain))
            {
                if (hitInfo.distance > MaxHeight)
                {
                    SendReply(player, GetMsg("Denied: Height", player.UserIDString), Math.Round(hitInfo.distance, 0));
                    entity.Kill(BaseNetworkable.DestroyMode.Gib);
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

        string GetMsg(string key, string userID = null)
        {
            return lang.GetMessage(key, this, userID);
        }
    }
}