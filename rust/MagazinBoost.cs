using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Oxide.Game.Rust;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MagazinBoost", "Fujikura", "1.2.1", ResourceId = 1962)]
    [Description("Can change magazines, ammo and conditon for most projectile weapons")]
    public class MagazinBoost : RustPlugin
    {	
		private bool Changed;

		StoredWeapons storedWeapons = new StoredWeapons();
		
		private FieldInfo _itemCondition = typeof(Item).GetField("_condition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
		private FieldInfo _itemMaxCondition = typeof(Item).GetField("_maxCondition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

		#region Config
		
		private string permissionAll;
		private string permissionMaxAmmo;
		private string permissionPreLoad;
		private string permissionMaxCondition;
		private string permissionAmmoType;
		private bool checkPermission;
		private bool removeSkinIfNoRights;		
		
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
			permissionAll = Convert.ToString(GetConfig("Permissions", "permissionAll", "magazinboost.canall"));
			permissionMaxAmmo = Convert.ToString(GetConfig("Permissions", "permissionMaxAmmo", "magazinboost.canmaxammo"));
			permissionPreLoad = Convert.ToString(GetConfig("Permissions", "permissionPreLoad", "magazinboost.canpreload"));
			permissionMaxCondition = Convert.ToString(GetConfig("Permissions", "permissionMaxCondition", "magazinboost.canmaxcondition"));
			permissionAmmoType = Convert.ToString(GetConfig("Permissions", "permissionAmmoType", "magazinboost.canammotype"));
			checkPermission = Convert.ToBoolean(GetConfig("CheckRights", "checkForRightsInBelt", true));
			removeSkinIfNoRights = Convert.ToBoolean(GetConfig("CheckRights", "removeSkinIfNoRights", true));
            
			if (!Changed) return;
            SaveConfig();
            Changed = false;
        }
		
        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }
		
		#endregion Config
		
		
		#region WeaponData

		private void SetupDefaultWeapons()
        {
			var defaultWeapons = new Dictionary<string,ItemDefinition>();
			var weapons = ItemManager.itemList.Where(p => p.category == ItemCategory.Weapon);
			foreach( var weapon in weapons )
			{
				var item = ItemManager.CreateByName(weapon.shortname);
				if (item.GetHeldEntity() is BaseProjectile)
					defaultWeapons.Add(item.info.shortname, item.info);
				item.Remove(0f);
			}
			for(int i=0; i < storedWeapons.weaponStatsList.Count; ++i)
				foreach (WeaponStats stats in storedWeapons.weaponStatsList[i].weaponStatsContent)
					{
						if (stats.servermaxammo > 0 && stats.serverpreload > 0 && stats.serverammotype != null && stats.servermaxcondition > 0 && stats.serveractive)
						{
							defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize = stats.servermaxammo;
							defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents = stats.serverpreload;
							var ammo = ItemManager.FindItemDefinition(stats.serverammotype);
							if (ammo != null)
								defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType = ammo;
							defaultWeapons[stats.name].condition.max = (float)stats.servermaxcondition;

						}
						if (stats.servermaxammo == 0 || stats.serverpreload == 0 || stats.serverammotype == null || stats.servermaxcondition == 0) 
						{
							stats.servermaxammo = defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize;
							stats.serverpreload = defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.contents;
							stats.serverammotype = defaultWeapons[stats.name].GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.ammoType.shortname;
							stats.servermaxcondition = (int)defaultWeapons[stats.name].condition.max;
						}
					}
		}
		
		class StoredWeapons
        {
            public List<WeaponInventory> weaponStatsList = new List<WeaponInventory>();

            public StoredWeapons()
            {
            }
        }
		
		class WeaponInventory
        {
            private FieldInfo _itemMaxCon = typeof(Item).GetField("_maxCondition", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
			
			public List<WeaponStats> weaponStatsContent = new List<WeaponStats>();

            public WeaponInventory() { }

            public WeaponInventory(List<WeaponStats> list)
            {
                weaponStatsContent = list;
            }

            public WeaponInventory(string name, string displayname, int maxammo, int preload, int maxcondition, string ammotype, ulong skinid)
            {
                weaponStatsContent.Add(new WeaponStats(name, displayname, maxammo, preload, maxcondition, ammotype, skinid));
            }

            public int InventorySize()
            {
                return weaponStatsContent.Count;
            }

            public List<WeaponStats> GetweaponStatsContent()
            {
                return weaponStatsContent;
            }
        }
		
		private class WeaponStats
        {
            public string name;
            public string displayname;
			public int maxammo;
			public int preload;
			public int maxcondition;
			public string ammotype;
			public ulong skinid;
			public bool settingactive;
			
			public int servermaxammo;
			public int serverpreload;
			public string serverammotype;
			public int servermaxcondition;
			public bool serveractive;

            public WeaponStats() { }

            public WeaponStats(string name, string displayname, int maxammo, int preload, int maxcondition, string ammotype, ulong skinid)
            {
                this.name = name;
                this.displayname = displayname;
				this.maxammo = maxammo;
                this.preload = preload;
				this.maxcondition = maxcondition;
				this.ammotype = ammotype;
				this.skinid = skinid;
				this.settingactive = true;
            }
        }

		private void LoadWeaponData()
        {
            storedWeapons = Interface.GetMod().DataFileSystem.ReadObject<StoredWeapons>("MagazinBoost");
            if (storedWeapons.weaponStatsList.Count == 0)
            {
                storedWeapons = new StoredWeapons();
                WeaponInventory inv;
				var weapons = ItemManager.itemList.Where(p => p.category == ItemCategory.Weapon).ToList();
				foreach( var weapon in weapons )
				{
					var item = ItemManager.CreateByName(weapon.shortname);
					if (item.GetHeldEntity() is BaseProjectile)
					{
						inv = new WeaponInventory(
							item.info.shortname,
							item.info.displayName.english,
							(item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity,
							(item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents,
							Convert.ToInt32(_itemMaxCondition.GetValue(item)),
							(item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.shortname,
							item.GetHeldEntity().skinID
							);
						storedWeapons.weaponStatsList.Add(inv);
					}
					item.Remove(0f);
				}
				SaveWeaponData();
            }
        }
		
		private void SaveWeaponData()
		{
			Interface.GetMod().DataFileSystem.WriteObject("MagazinBoost", storedWeapons);
		}
		
		#endregion WeaponData
		        
		private WeaponStats craftedWeapon(string name)
		{
			for(int i=0; i < storedWeapons.weaponStatsList.Count; ++i)
				foreach (WeaponStats stats in storedWeapons.weaponStatsList[i].weaponStatsContent.ToList())
					{
						if (stats.name == name)
						return stats;
					}
					return null;
		}
		
		private bool hasAnyRight(BasePlayer player)
		{
			if (permission.UserHasPermission(player.UserIDString, permissionAll)) return true;
			if (permission.UserHasPermission(player.UserIDString, permissionMaxAmmo)) return true;
			if (permission.UserHasPermission(player.UserIDString, permissionPreLoad)) return true;
			if (permission.UserHasPermission(player.UserIDString, permissionMaxCondition)) return true;
			if (permission.UserHasPermission(player.UserIDString, permissionAmmoType)) return true;
			return false;
		}
		
		private bool hasRight(BasePlayer player, string perm)
		{
			bool right = false;
			switch (perm)
			{
				case "all":
						if (permission.UserHasPermission(player.UserIDString, permissionAll)) {right = true;}
						break;
				case "maxammo":
						if (permission.UserHasPermission(player.UserIDString, permissionMaxAmmo)) {right = true;}
						break;
				case "preload":
						if (permission.UserHasPermission(player.UserIDString, permissionPreLoad)) {right = true;}
						break;
				case "maxcondition":
						if (permission.UserHasPermission(player.UserIDString, permissionMaxCondition)) {right = true;}
						break;
				case "ammotype":
						if (permission.UserHasPermission(player.UserIDString, permissionAmmoType)) {right = true;}
						break;
				default:
						break;
				
			}
			return right;
		}

		private void OnServerInitialized()
        {
			NextTick( ()=> {
				LoadVariables();
				LoadWeaponData();
				SetupDefaultWeapons();
				SaveWeaponData();
				if (!permission.PermissionExists(permissionAll)) permission.RegisterPermission(permissionAll, this);
				if (!permission.PermissionExists(permissionMaxAmmo)) permission.RegisterPermission(permissionMaxAmmo, this);
				if (!permission.PermissionExists(permissionPreLoad)) permission.RegisterPermission(permissionPreLoad, this);
				if (!permission.PermissionExists(permissionMaxCondition)) permission.RegisterPermission(permissionMaxCondition, this);
				if (!permission.PermissionExists(permissionAmmoType)) permission.RegisterPermission(permissionAmmoType, this);
			});
		}

		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			if(item.GetHeldEntity() is BaseProjectile)
			{
				if(!hasAnyRight(task.owner)) return;
				var weaponstats = craftedWeapon(item.info.shortname);
				if(weaponstats != null && weaponstats.settingactive == true)			
				{
					if (hasRight(task.owner,"maxammo") || hasRight(task.owner, "all"))
						(item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity = Convert.ToInt32(weaponstats.maxammo);
					if (hasRight(task.owner,"preload") || hasRight(task.owner, "all"))
						(item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = Convert.ToInt32(weaponstats.preload);
					if (hasRight(task.owner,"ammotype") || hasRight(task.owner, "all"))
					{
						var ammo = ItemManager.FindItemDefinition(weaponstats.ammotype);
						if (ammo != null)
							(item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ammo;
					}
					if (hasRight(task.owner,"maxcondition") || hasRight(task.owner, "all"))
					{
						_itemMaxCondition.SetValue(item, Convert.ToSingle(weaponstats.maxcondition));
						_itemCondition.SetValue(item, Convert.ToSingle(weaponstats.maxcondition));				
					}
					if(weaponstats.skinid != 0)
						foreach( var skin in item.info.skins.ToList())
							if (skin.id == (int)weaponstats.skinid)
							{
								item.skin = weaponstats.skinid;
								item.GetHeldEntity().skinID = weaponstats.skinid;
								break;
							}
				}
			}
		}
		
		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if(!checkPermission) return;
			if(item.GetHeldEntity() is BaseProjectile && container.HasFlag(ItemContainer.Flag.Belt))
			{
				var weaponstats = craftedWeapon(item.info.shortname);
				if(weaponstats != null && weaponstats.settingactive)	
				{
					if ((item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity > item.info.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize && !(hasRight(container.playerOwner, "maxammo") || hasRight(container.playerOwner, "all")))
					{
						(item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity = item.info.GetComponent<ItemModEntity>().entityPrefab.Get().GetComponent<BaseProjectile>().primaryMagazine.definition.builtInSize;
						if ((item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents > (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity)
							(item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
					}
					if (item.maxCondition > item.info.condition.max && !(hasRight(container.playerOwner, "maxcondition") || hasRight(container.playerOwner, "all")))
					{
						var newCon = item.condition * (item.info.condition.max / item.maxCondition);
						_itemMaxCondition.SetValue(item, Convert.ToSingle(item.info.condition.max));
						_itemCondition.SetValue(item, Convert.ToSingle(newCon));
					}
					if (removeSkinIfNoRights && !hasAnyRight(container.playerOwner) && item.GetHeldEntity().skinID == weaponstats.skinid && item.GetHeldEntity().skinID != 0)
					{
						item.skin = 0;
						item.GetHeldEntity().skinID = 0;
					}
				}
			}
		}

	}
}
