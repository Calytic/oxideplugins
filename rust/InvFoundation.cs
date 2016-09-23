using System;
using Rust;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InvFoundation", "sami37", "1.1.0", ResourceId = 2096)]
    [Description("Invulnerable foundation")]
    public class InvFoundation : RustPlugin
    {
        #region Building Owners Support
        [PluginReference("BuildingOwners")]
        Plugin BuildingOwners;
        #endregion

        #region Building Owners Support
        [PluginReference("EntityOwner")]
        Plugin EntityOwner;
        #endregion

        private Dictionary<string, object> damageList => GetConfig("DamageList", defaultDamageScale()); 
        private bool UseEntityOwner => GetConfig("UseEntityOwner", false);
        private bool UseBuildOwners => GetConfig("UseBuildingOwner", false);
        private bool UseDamageScaling => GetConfig("UseDamageScaling", false);
        static int colisionentity = LayerMask.GetMask("Construction");
        private readonly int cupboardMask = LayerMask.GetMask("Trigger");
        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

		static Dictionary<string,object> defaultDamageScale()
		{
			var dp = new Dictionary<string, object>();
            dp.Add("Bullet", 0.0);
            dp.Add("Blunt", 0.0);
            dp.Add("Stab", 0.0);
            dp.Add("Slash", 0.0);
            dp.Add("Explosion", 0.0);

			return dp;
		}


        void Loaded()
        {
            Config["UseBuildingOwner"] = UseBuildOwners;
            Config["UseEntityOwner"] = UseEntityOwner;
            Config["UseDamageScaling"] = UseDamageScaling;
            Config["DamageList"] = damageList;
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Generating new config file...");
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            Config["DamageList"] = damageList;
            SaveConfig();
            var messages = new Dictionary<string, string>
            {
				{"NoPerm", "You don't have permission to do this."}
            };
            lang.RegisterMessages(messages, this);
        }
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null)
                return;

            if (entity is BuildingBlock)
            {
                BuildingBlock block = entity as BuildingBlock;
                if (block == null) return;
                if (hitInfo.Initiator == null) return;
                BasePlayer attacker = hitInfo.Initiator.ToPlayer();
                if (attacker == null) return;


                if (block.LookupPrefab().name.Contains("foundation") && !IsOwner(attacker, block) && !CupboardPrivlidge(attacker, block.transform.position))
                {
                    if (!UseDamageScaling)
                    {
                        hitInfo.damageTypes = new DamageTypeList();
                        hitInfo.DoHitEffects = false;
                        hitInfo.HitMaterial = 0;
                        SendReply(attacker, lang.GetMessage("NoPerm", this, attacker.UserIDString));
                        return;
                    }
                    DamageType type = hitInfo.damageTypes.GetMajorityDamageType();
                    object modifier;
                    float mod = 0;
                    if (damageList.TryGetValue(type.ToString(), out modifier))
                    {
                        mod = Convert.ToSingle(modifier);
                        if (mod != 0)
                        {
                            hitInfo.damageTypes.Scale(type, mod);
                        }
                        else
                        {
                            hitInfo.damageTypes = new DamageTypeList();
                            hitInfo.DoHitEffects = false;
                            hitInfo.HitMaterial = 0;
                            SendReply(attacker, lang.GetMessage("NoPerm", this, attacker.UserIDString));
                        }
                    }
                }
            }
        }

        bool IsOwner(BasePlayer player, BaseEntity targetEntity)
        {
            if (targetEntity == null) return false;
            if (targetEntity.OwnerID == player.userID) return true;
            BuildingBlock block = targetEntity.GetComponent<BuildingBlock>();
            if (block == null)
            {
                RaycastHit supportHit;
                if (Physics.Raycast(targetEntity.transform.position + new Vector3(0f, 0.1f, 0f), new Vector3(0f, -1f, 0f), out supportHit, 3f, colisionentity))
                {
                    BaseEntity supportEnt = supportHit.GetEntity();
                    if (supportEnt != null)
                    {
                        block = supportEnt.GetComponent<BuildingBlock>();
                    }
                }
            }
            if (block != null)
            {
				if (UseBuildOwners)
				{
					if (BuildingOwners != null && BuildingOwners.IsLoaded)
					{
                        var returnhook = Interface.GetMod().CallHook("FindBlockData", new object[] {block});
                        if (returnhook is string)
                        {
                            string ownerid = (string) returnhook;
                            if (player.UserIDString == ownerid) return true;
                        }
                    }
                }
				if (UseEntityOwner)
				{
					if (EntityOwner != null && EntityOwner.IsLoaded)
					{
                        var returnhook = Interface.GetMod().CallHook("FindEntityData", new object[] {targetEntity});
                        if (returnhook is string)
                        {
                            string ownerid = (string) returnhook;
                            if (player.UserIDString == ownerid) return true;
                        }
                    }
                }
            }
            return false;
        }
        private bool CupboardPrivlidge(BasePlayer player, Vector3 position)
        {
            var hits = Physics.OverlapSphere(position, 2f, cupboardMask);
            foreach (var collider in hits)
            {
                var buildingPrivlidge = collider.GetComponentInParent<BuildingPrivlidge>();
                if (buildingPrivlidge == null) continue;

                List<string> ids = (from id in buildingPrivlidge.authorizedPlayers select id.userid.ToString()).ToList();
                foreach (string priv in ids)
                {
                    if (priv == player.UserIDString)
                    {
                        return true;
                    }
                }        
            }
            return false;
        }

    }
}