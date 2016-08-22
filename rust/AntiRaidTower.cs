using System;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AntiRaidTower", "Calytic @ RustServers.IO", "0.2.1", ResourceId = 1211)]
    [Description("High jump instant death/No wounded teleport")]
    class AntiRaidTower : RustPlugin
    {
        int FallKill;
        bool BuildingBlockHeight;
        bool DeployableBlockHeight;
        int BuildingMaxHeight;
        int DeployableMaxHeight;
        string BlockHeightMessage;

        void OnServerInitialized()
        {
            LoadData();

            FallKill = GetConfig("FallKill", 200);
            BuildingBlockHeight = GetConfig("BuildingBlockHeight", true);
            DeployableBlockHeight = GetConfig("DeployableBlockHeight", true);
            BlockHeightMessage = GetConfig("BlockHeightMessage", "Too far from ground: {0}");
            BuildingMaxHeight = GetConfig("BuildingMaxHeight", 50);
            DeployableMaxHeight = GetConfig("DeployableMaxHeight", 50);
        }

        protected override void LoadDefaultConfig()
        {
            Config["FallKill"] = 200;
            Config["BuildingBlockHeight"] = true;
            Config["BuildingMaxHeight"] = 50;
            Config["DeployableBlockHeight"] = true;
            Config["DeployableMaxHeight"] = 50;

            Config["BlockHeightMessage"] = "Too far from ground: {0}m";
            Config["VERSION"] = Version.ToString();
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
            if (entity is BasePlayer)
            {
                var player = (BasePlayer)entity;
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
            if (player.IsWounded()) { return "You may not teleport while wounded"; }
            return null;
        }

        object canTeleport(BasePlayer player)
        {
            return CanTeleport(player);
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner.GetOwnerPlayer() == null) return;
            if (!BuildingBlockHeight && !DeployableBlockHeight) return;

            var MaxHeight = 0;
            var entity = gameObject.GetComponent<BaseCombatEntity>();
            if(entity is BuildingBlock) {
                MaxHeight = BuildingMaxHeight;
            }
            else
            {
                MaxHeight = DeployableMaxHeight;
            }

            RaycastHit hitInfo;
            if (entity != null && Physics.Raycast(new Ray(entity.transform.position, Vector3.down), out hitInfo, float.PositiveInfinity, Rust.Layers.Terrain))
            {
                if (hitInfo.distance > MaxHeight)
                {
                    SendReply(planner.GetOwnerPlayer(), BlockHeightMessage, Math.Round(hitInfo.distance,0));
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
    }
}