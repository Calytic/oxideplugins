using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoWeaponDrop", "Fujikura", "0.2.3", ResourceId = 1960)]
	[Description("Prevents dropping of active weapon on wounded or headshot/instant-kill")]
    class NoWeaponDrop : RustPlugin
    {
		[PluginReference]
		Plugin RestoreUponDeath;
		
		private bool Changed = false;
		private bool usePermission;
		private string permissionName;
		private bool disableForROD;
		
		private object GetConfig(string menu, string datavalue, object defaultValue)
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
		
		void LoadVariables()
        {
			usePermission = Convert.ToBoolean(GetConfig("Settings", "Use permissions", false));
			permissionName = Convert.ToString(GetConfig("Settings", "Permission name", "noweapondrop.active"));
			disableForROD =  Convert.ToBoolean(GetConfig("Settings", "Disable death handler when RestoreUponDeath was found", false));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }
		
		protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
		
		void Loaded()
		{
			LoadVariables();
			if (!permission.PermissionExists(permissionName)) permission.RegisterPermission(permissionName, this);
		}
				
		void OnItemRemovedFromContainer(ItemContainer container, Item item)
		{
			if (container.HasFlag(ItemContainer.Flag.Belt))		
			NextTick(() => {
				if(usePermission && !permission.UserHasPermission(container.playerOwner.userID.ToString(), permissionName)) return;
				if (container.playerOwner.IsWounded() && (item.info.category.value__ == 0 ||  item.info.category.value__ == 5))
					item.MoveToContainer(container);
			});
		}
		
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
			if ((entity as BasePlayer) != null && ( info.isHeadshot == true || info.damageTypes.Has(Rust.DamageType.Suicide) )) 
			{
				if(RestoreUponDeath && disableForROD) return null;
				if(usePermission && !permission.UserHasPermission((entity as BasePlayer).userID.ToString(), permissionName)) return null;
				(entity as BasePlayer).svActiveItemID = 0u;
				(entity as BasePlayer).SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
			}
			return null;
        }
	}
}

