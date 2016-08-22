using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Oxide.Core;
namespace Oxide.Plugins
{
    [Info("NoBowRaid", "Bamabo", "1.0.1")]
    [Description("Gets rid of one of the most broken mechanics in Rust")]

    class NoBowRaid : RustPlugin
    {
        private List<string> whitelist;
        private bool adminBypass;
        private bool ignoreTwig;
        private bool usePermissions;
        private float damageScale;
        void Init()
        {
            adminBypass = (bool)Config["adminBypass"];
            damageScale = Convert.ToSingle(Config["damageScale"]);
            ignoreTwig = (bool)Config["ignoreTwig"];
            usePermissions = (bool)Config["usePermissions"];
            if(usePermissions)
                permission.RegisterPermission("nobowraid.bypass", this);
            whitelist = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("NoBowRaid_Whitelist");
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return;
            if (info == null)
                return;
            BasePlayer attacker = null;
            attacker = info?.Initiator?.ToPlayer();


            if (attacker)
            {

                if (usePermissions)
                    if (permission.UserHasPermission(attacker.UserIDString, "nobowraid.bypass"))
                        return;
                if (attacker.IsAdmin() && adminBypass)
                    return;
                string weapon = info.Weapon.name.ToString();
                if (weapon.Contains("bow_hunting.entity") || weapon.Contains("crossbow.entity"))
                {
                    BuildingBlock block = null;

                    if (entity.GetComponent<BuildingBlock>() != null)
                        block = entity.GetComponent<BuildingBlock>();

                    if (!whitelist.Any(entity.name.Contains) && !block)
                        info.damageTypes.ScaleAll(damageScale);

                    if (block)
                    {
                        if (block.grade.ToString().Equals("Twigs") && ignoreTwig)
                            return;

                        info.damageTypes.ScaleAll(damageScale);
                    }

                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file for NoBowRaid");
            Config.Clear();
            var defaultWhitelist = new List<object>() { "player", "animal" };
            Interface.Oxide.DataFileSystem.WriteObject("NoBowRaid_Whitelist", defaultWhitelist);
            Config["adminBypass"] = false;
            Config["damageScale"] = 0f;
            Config["ignoreTwig"] = true;
            Config["usePermissions"] = false;
            SaveConfig();
        }
    }
}