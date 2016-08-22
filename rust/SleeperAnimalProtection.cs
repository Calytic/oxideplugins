using System;
using System.Collections.Generic;
using UnityEngine;

using Facepunch;

namespace Oxide.Plugins
{
    [Info("SleeperAnimalProtection", "Fujikura", "0.2.1")]
	[Description("Protects sleeping players from being killed by animals")]
    class SleeperAnimalProtection : RustPlugin
    {
		private bool Changed = false;
		private bool usePermission;
		private string permissionName;
		private bool checkForFoundation;
        private readonly int buildingLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
		
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
			permissionName = Convert.ToString(GetConfig("Settings", "Permission name", "sleeperanimalprotection.active"));
			checkForFoundation = Convert.ToBoolean(GetConfig("Settings", "Required to sleep ON foundation", false));

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
	
        private List<BuildingBlock> GetFoundation(Vector3 positionCoordinates)
        {
            var position = positionCoordinates;
            var entities = new List<BuildingBlock>();
            var hits = Pool.GetList<BuildingBlock>();
            Vis.Entities(position, 2.5f, hits, buildingLayer);
            for (var i = 0; i < hits.Count; i++)
            {
                var entity = hits[i];
                if (!entity.ShortPrefabName.Contains("foundation") || positionCoordinates.y < entity.WorldSpaceBounds().ToBounds().max.y) continue;
                entities.Add(entity);
            }
            Pool.FreeList(ref hits);
            return entities;
        }	
	
		object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
			if (entity as BasePlayer != null && (entity as BasePlayer).IsSleeping() && info.Initiator is BaseNPC) 
			{
				if(usePermission && !permission.UserHasPermission((entity as BasePlayer).userID.ToString(), permissionName)) return null;
				if(checkForFoundation && GetFoundation(entity.transform.position).Count == 0) return null;
				info.Initiator.Kill();
				return true;
			}
            return null;
        }
    }
}
