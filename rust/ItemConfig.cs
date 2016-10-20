//Reference: Newtonsoft.Json

using System.Collections.Generic;
using System.Linq;

using Rust;

using UnityEngine;

using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using JSONObject = JSON.Object;
using JSONArray = JSON.Array;
using JSONValue = JSON.Value;
using JSONValueType = JSON.ValueType;

namespace Oxide.Plugins
{
    [Info("ItemConfig", "Nogrod", "1.0.35", ResourceId = 806)]
    class ItemConfig : RustPlugin
    {
        private const int VersionConfig = 9;
        private string _configpath = "";
        private bool _craftingController;
        private bool _stackSizes;
        private Dictionary<string, ItemDefinition> _itemsDict;
        private Dictionary<string, ItemBlueprint> _bpsDict;

        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ContractResolver = new DynamicContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter> {new UnityEnumConverter()}
        };

        void Loaded()
        {
            _configpath = Manager.ConfigPath + $"\\{Name}.json";
        }

        void LoadDefaultConfig()
        {

        }

        private JSONObject ToJsonObject(object obj)
        {
            return JSONObject.Parse(ToJsonString(obj));
        }

        private JSONArray ToJsonArray(object obj)
        {
            return JSONArray.Parse(ToJsonString(obj));
        }

        private string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        private T FromJsonString<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /*private static void StripObject(JSONObject obj)
        {
            if (obj == null) return;
            var keys = obj.Select(entry => entry.Key).ToList();
            foreach (var key in keys)
            {
                if (!key.Equals("shortname") && !key.Equals("itemid"))
                    obj.Remove(key);
            }
        }

        private static void StripArray(JSONArray arr, string key)
        {
            if (arr == null) return;
            foreach (var obj in arr)
            {
                StripObject(obj.Obj[key].Obj);
            }
        }*/

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            Config["Version"] = Protocol.network;
            Config["VersionConfig"] = VersionConfig;
            var gameObjectArray = FileSystem.Load<ObjectList>("Assets/items.asset").objects.Cast<GameObject>().ToArray();
            var itemList = gameObjectArray.Select(x => x.GetComponent<ItemDefinition>()).Where(x => x != null).ToArray();
            var bpList = gameObjectArray.Select(x => x.GetComponent<ItemBlueprint>()).Where(x => x != null).ToArray();
            var items = new JSONArray();
            foreach (var definition in itemList)
            {
                //Puts("Item: {0}", definition.displayName.english);
                var obj = ToJsonObject(definition);
                obj.Remove("itemid");
                obj.Remove("hidden");
                obj.Remove("isWearable");
                obj["Parent"] = definition.Parent?.shortname;
                obj["displayName"] = definition.displayName.english;
                obj["displayDescription"] = definition.displayDescription.english;
                var mods = definition.GetComponentsInChildren<ItemMod>(true);
                var modArray = new JSONArray();
                foreach (var itemMod in mods)
                {
                    if (itemMod.GetType() == typeof (ItemModMenuOption) || itemMod.GetType() == typeof(ItemModConditionHasFlag) || itemMod.GetType() == typeof(ItemModConditionContainerFlag)
                        || itemMod.GetType() == typeof(ItemModSwitchFlag) || itemMod.GetType() == typeof(ItemModCycle) || itemMod.GetType() == typeof(ItemModConditionHasContents)
                        || itemMod.GetType() == typeof(ItemModUseContent) || itemMod.GetType() == typeof(ItemModEntity) || itemMod.GetType() == typeof(ItemModUnwrap)) continue;
                    //Puts("ItemMod: {0}", itemMod.GetType());
                    var mod = ToJsonObject(itemMod);
                    if (itemMod.GetType() == typeof(ItemModBurnable))
                    {
                        mod["byproductItem"] = mod.GetObject("byproductItem")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModCookable))
                    {
                        mod["becomeOnCooked"] = mod.GetObject("becomeOnCooked")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModContainer))
                    {
                        var defaultContents = mod["defaultContents"].Array;
                        foreach (var entry in defaultContents)
                        {
                            entry.Obj["shortname"] = entry.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                            entry.Obj.Remove("itemDef");
                            entry.Obj.Remove("itemid");
                        }
                        mod["onlyAllowedItemType"] = mod.GetObject("onlyAllowedItemType")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModConsume))
                    {
                        mod["effects"] = ToJsonArray(itemMod.GetComponent<ItemModConsumable>().effects);
                    }
                    else if (itemMod.GetType() == typeof(ItemModReveal))
                    {
                        mod["revealedItemOverride"] = mod.GetObject("revealedItemOverride")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModRecycleInto))
                    {
                        mod["recycleIntoItem"] = mod.GetObject("recycleIntoItem")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModUpgrade))
                    {
                        mod["upgradedItem"] = mod.GetObject("upgradedItem")?.GetString("shortname", "unnamed");
                    }
                    else if (itemMod.GetType() == typeof(ItemModSwap))
                    {
                        var becomeItems = mod["becomeItem"].Array;
                        foreach (var entry in becomeItems)
                        {
                            entry.Obj["shortname"] = entry.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                            entry.Obj.Remove("itemDef");
                            entry.Obj.Remove("itemid");
                        }
                    }
                    else if (itemMod.GetType() == typeof(ItemModWearable))
                    {
                        var itemModWearable = itemMod.GetComponent<ItemModWearable>();
                        if (itemModWearable.protectionProperties != null)
                        {
                            var protectionObj = new JSONObject
                            {
                                ["density"] = itemModWearable.protectionProperties.density
                            };
                            var amounts = new JSONObject();
                            for (var i = 0; i < itemModWearable.protectionProperties.amounts.Length; i++)
                                amounts[((DamageType) i).ToString()] = itemModWearable.protectionProperties.amounts[i];
                            protectionObj["amounts"] = amounts;
                            mod["protection"] = protectionObj;
                        }
                        if (itemModWearable.armorProperties != null)
                            mod["armor"] = ToJsonObject(itemModWearable.armorProperties).GetString("area");
                        var targetWearable = mod.GetObject("targetWearable");
                        targetWearable.Remove("showCensorshipCube");
                        targetWearable.Remove("showCensorshipCubeBreasts");
                        targetWearable.Remove("followBone");
                        targetWearable["occupationOver"] = FromJsonString<string>(ToJsonString((OccupationSlotsUnity)itemModWearable.targetWearable.occupationOver));
                        targetWearable["occupationUnder"] = FromJsonString<string>(ToJsonString((OccupationSlotsUnity)itemModWearable.targetWearable.occupationUnder));
                    }
                    if (!mod.Any()) continue;
                    mod["type"] = itemMod.GetType().FullName;
                    modArray.Add(mod);
                }
                var modEntity = definition.GetComponent<ItemModEntity>();
                if (modEntity != null)
                {
                    var prefab = modEntity.entityPrefab?.Get();
                    var timedExplosive = prefab?.GetComponent<ThrownWeapon>()?.prefabToThrow?.Get()?.GetComponent<TimedExplosive>();
                    if (timedExplosive != null)
                    {
                        var mod = ToJsonObject(timedExplosive);
                        mod["type"] = modEntity.GetType().FullName + timedExplosive.GetType().FullName;
                        if (timedExplosive is DudTimedExplosive)
                            mod.Remove("itemToGive");
                        modArray.Add(mod);
                    }
                    var modMelee = prefab?.GetComponent<BaseMelee>();
                    if (modMelee != null)
                    {
                        var mod = ToJsonObject(modMelee);
                        mod["type"] = modEntity.GetType().FullName + typeof(BaseMelee).FullName;
                        mod.Remove("strikeEffect");
                        modArray.Add(mod);
                    }
                    var baseProjectile = prefab?.GetComponent<BaseProjectile>();
                    if (baseProjectile != null)
                    {
                        var mod = new JSONObject
                        {
                            ["ammoType"] = baseProjectile.primaryMagazine.ammoType.shortname,
                            //["ammoTypes"] = ToJsonString(baseProjectile.primaryMagazine.definition.ammoTypes),
                            ["builtInSize"] = baseProjectile.primaryMagazine.definition.builtInSize,
                            //["capacity"] = baseProjectile.primaryMagazine.capacity,
                            ["contents"] = baseProjectile.primaryMagazine.contents,
                            ["type"] = modEntity.GetType().FullName + typeof (BaseProjectile).FullName
                        };
                        modArray.Add(mod);
                    }
                }
                var modProjectile = definition.GetComponent<ItemModProjectile>();
                if (modProjectile != null)
                {
                    var prefab = modProjectile.projectileObject?.Get();
                    var projectile = prefab?.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        var mod = ToJsonObject(projectile);
                        mod.Remove("sourceWeapon");
                        mod.Remove("projectileID");
                        mod.Remove("seed");
                        mod.Remove("velocityScalar");
                        if (modProjectile.mods != null)
                        {
                            var modsArray = new JSONArray();
                            foreach (var projectileMod in modProjectile.mods)
                            {
                                var projMod = ToJsonObject(projectileMod);
                                projMod["type"] = projectileMod.GetType().FullName;
                                modsArray.Add(projMod);
                            }
                            mod["mods"] = modsArray;
                        }
                        var spawn = modProjectile as ItemModProjectileSpawn;
                        if (spawn != null)
                        {
                            mod["createOnImpactChance"] = spawn.createOnImpactChance;
                            mod["spreadAngle"] = spawn.spreadAngle;
                        }
                        mod["type"] = modProjectile.GetType().FullName;
                        modArray.Add(mod);
                    }
                    /*var components = modProjectile.projectileObject.targetObject.GetComponents(typeof (Component));
                    foreach (var component in components)
                    {
                        LocalPuts("Name: " + component.name + " Type: " + component.GetType().Name);
                    }*/
                    var timedExplosive = prefab?.GetComponent<TimedExplosive>();
                    if (timedExplosive != null)
                    {
                        var mod = ToJsonObject(timedExplosive);
                        mod["type"] = modProjectile.GetType().FullName + timedExplosive.GetType().FullName;
                        modArray.Add(mod);
                    }
                    var serverProjectile = prefab?.GetComponent<ServerProjectile>();
                    if (serverProjectile != null)
                    {
                        var mod = ToJsonObject(serverProjectile);
                        mod["type"] = modProjectile.GetType().FullName + serverProjectile.GetType().FullName;
                        modArray.Add(mod);
                    }
                }
                obj["modules"] = modArray;

                items.Add(obj);
            }
            Config["Items"] = JsonObjectToObject(items);
            var bps = ToJsonArray(bpList);
            foreach (var bp in bps)
            {
                bp.Obj["targetItem"] = bp.Obj.GetObject("targetItem").GetString("shortname", "unnamed");
                bp.Obj.Remove("userCraftable");
                bp.Obj.Remove("defaultBlueprint");
                foreach (var ing in bp.Obj.GetArray("ingredients"))
                {
                    ing.Obj["shortname"] = ing.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                    ing.Obj.Remove("itemDef");
                    ing.Obj.Remove("itemid");
                }
            }
            Config["Blueprints"] = JsonObjectToObject(bps);

            try
            {
                Config.Save(_configpath);
            }
            catch (Exception e)
            {
                Puts("Config save failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            Puts("Created new config");
            return LoadConfig();
        }

        private bool LoadConfig()
        {
            try
            {
                Config.Load(_configpath);
            }
            catch (FileNotFoundException)
            {
                return CreateDefaultConfig();
            }
            catch (Exception e)
            {
                Puts("Config load failed: {0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                return false;
            }
            return true;
        }

        private void CheckConfig()
        {
            if (Config["Version"] != null && (int) Config["Version"] == Protocol.network && Config["VersionConfig"] != null && (int)Config["VersionConfig"] == VersionConfig) return;
            Puts("Incorrect config version({0}/{1})", Config["Version"], Config["VersionConfig"]);
            if (Config["Version"] != null) Config.Save(string.Format("{0}.old", _configpath));
            CreateDefaultConfig();
        }

        void OnServerInitialized()
        {
            /*var list = new List<BasePlayer>();
            list.AddRange(BasePlayer.activePlayerList);
            list.AddRange(BasePlayer.sleepingPlayerList);
            foreach (var basePlayer in list)
            {
                Puts("ContainerMain: {0}", basePlayer.inventory.containerMain.maxStackSize);
                Puts("ContainerBelt: {0}", basePlayer.inventory.containerBelt.maxStackSize);
                Puts("ContainerWear: {0}", basePlayer.inventory.containerWear.maxStackSize);
                var items = basePlayer.inventory.AllItems();
                Puts("Items: {0}", items.Length);
                foreach (var item in items)
                {
                    Puts("Name: {0} MaxStack: {1}", item.info.shortname, item.MaxStackable());
                }
            }
            var containers = UnityEngine.Object.FindObjectsOfType<StorageContainer>();
            foreach (var container in containers)
            {
                if (container.maxStackSize > 0 || container.inventory?.maxStackSize > 0)
                {
                    Puts("StorageContainer: {0}", container.maxStackSize);
                    Puts("StorageContainerInv: {0}", container.inventory?.maxStackSize);
                }
            }*/
            if (!LoadConfig())
                return;
            CheckConfig();
            NextTick(UpdateItems);
        }

        private void UpdateItems()
        {
            _itemsDict = ItemManager.itemList.ToDictionary(i => i.shortname);
            _bpsDict = ItemManager.bpList.ToDictionary(i => i.targetItem.shortname);
            //Puts(string.Join(", ", _bpsDict.Keys.ToArray()));
            var items = Config["Items"] as List<object>;
            if (items == null)
            {
                Puts("No items in config");
                return;
            }
            Puts("Found {0} items in config.", items.Count);
            _stackSizes = plugins.Exists("StackSizes") || plugins.Exists("StackSizeController");
            foreach (var item in items)
            {
                var value = ObjectToJsonObject(item);
                if (value.Type != JSONValueType.Object)
                {
                    Puts("Item is not object");
                    continue;
                }
                var definition = GetItem(value.Obj, "shortname");
                if (definition == null) continue;
                UpdateItem(definition, value.Obj);
            }
            NextTick(UpdateBlueprints);
        }

        private void UpdateBlueprints()
        {
            var bps = Config["Blueprints"] as List<object>;
            if (bps == null)
            {
                Puts("No blueprints in config");
                return;
            }
            Puts("Found {0} blueprints in config.", bps.Count);
            _craftingController = plugins.Exists("CraftingController");
            foreach (var blueprint in bps)
            {
                var value = ObjectToJsonObject(blueprint);
                if (value.Type != JSONValueType.Object)
                {
                    Puts("Item is not object");
                    continue;
                }
                var shortname = value.Obj.GetString("targetItem", "unnamed");
                ItemBlueprint bp;
                if (!_bpsDict.TryGetValue(shortname, out bp))
                {
                    Puts("Blueprint does not exist: " + shortname);
                    continue;
                }
                UpdateBlueprint(bp, value.Obj);
            }
            ItemManager.defaultBlueprints = ItemManager.bpList.Where(x => x.defaultBlueprint).Select(x => x.targetItem.itemid).ToArray();
            _itemsDict = null;
            _bpsDict = null;
        }

        private void UpdateBlueprint(ItemBlueprint bp, JSONObject o)
        {
            bp.rarity = GetRarity(o);
            if (!_craftingController) bp.time = o.GetFloat("time", 0);
            bp.amountToCreate = o.GetInt("amountToCreate", 1);
            bp.UnlockPrice = o.GetInt("UnlockPrice", 0);
            bp.UnlockLevel = o.GetInt("UnlockLevel", 10);
            bp.blueprintStackSize = o.GetInt("blueprintStackSize");
            //bp.userCraftable = o.GetBoolean("userCraftable", true);
            bp.isResearchable = o.GetBoolean("isResearchable", true);
            bp.NeedsSteamItem = o.GetBoolean("NeedsSteamItem", false);
            var ingredients = o.GetArray("ingredients");
            bp.ingredients.Clear();
            foreach (var ingredient in ingredients)
            {
                var itemDef = GetItem(ingredient.Obj, "shortname");
                if (itemDef == null) continue;
                bp.ingredients.Add(new ItemAmount(itemDef, ingredient.Obj.GetFloat("amount", 0)));
            }
        }

        private void UpdateItem(ItemDefinition definition, JSONObject item)
        {
            definition.shortname = item.GetString("shortname", "unnamed");
            if (!_stackSizes) definition.stackable = item.GetInt("stackable", 1);
            definition.maxDraggable = item.GetInt("maxDraggable", 0);
            definition.category = (ItemCategory)Enum.Parse(typeof(ItemCategory), item.GetString("category", "Weapon"));
            var condition = item.GetObject("condition");
            definition.condition.enabled = condition.GetBoolean("enabled", false);
            definition.condition.max = condition.GetFloat("max", 0);
            definition.condition.repairable = condition.GetBoolean("repairable", false);
            definition.rarity = GetRarity(item);
            definition.Parent = GetItem(item, "Parent");
            var modules = item.GetArray("modules").Select(m => m.Obj);
            foreach (var mod in modules)
            {
                var typeName = mod.GetString("type", "");
                //Puts("Item: {0} - {1}", definition.shortname, typeName);
                if (typeName.Equals("ItemModConsume"))
                {
                    var itemMod = definition.GetComponent<ItemModConsume>();
                    var itemEffects = itemMod.GetComponent<ItemModConsumable>().effects;
                    var effects = mod.GetArray("effects");
                    itemEffects.Clear();
                    foreach (var effect in effects)
                    {
                        itemEffects.Add(new ItemModConsumable.ConsumableEffect
                        {
                            type = (MetabolismAttribute.Type)Enum.Parse(typeof (MetabolismAttribute.Type), effect.Obj.GetString("type", "")),
                            amount = effect.Obj.GetFloat("amount", 0),
                            time = effect.Obj.GetFloat("time", 0)
                        });
                    }
                }
                else if (typeName.Equals("ItemModContainer"))
                {
                    var itemMod = definition.GetComponent<ItemModContainer>();
                    itemMod.capacity = mod.GetInt("capacity", 6);
                    itemMod.maxStackSize = mod.GetInt("maxStackSize", 0);
                    itemMod.openInDeployed = mod.GetBoolean("openInDeployed", true);
                    itemMod.openInInventory = mod.GetBoolean("openInInventory", true);
                    itemMod.defaultContents.Clear();
                    var defaultContents = mod.GetArray("defaultContents");
                    foreach (var content in defaultContents)
                    {
                        var itemDef = GetItem(content.Obj, "shortname");
                        if (itemDef == null) continue;
                        itemMod.defaultContents.Add(new ItemAmount(itemDef, content.Obj.GetFloat("amount", 0)));
                    }
                    itemMod.onlyAllowedItemType = mod.GetValue("onlyAllowedItemType").Type == JSONValueType.Null ? null : GetItem(mod.GetString("onlyAllowedItemType", "unnamed"));
                }
                else if (typeName.Equals("ItemModBurnable"))
                {
                    var itemMod = definition.GetComponent<ItemModBurnable>() ?? definition.gameObject.AddComponent<ItemModBurnable>();
                    itemMod.fuelAmount = mod.GetFloat("fuelAmount", 10f);
                    itemMod.byproductAmount = mod.GetInt("byproductAmount", 1);
                    itemMod.byproductChance = mod.GetFloat("byproductChance", 0.5f);
                    itemMod.byproductItem = GetItem(mod, "byproductItem");
                }
                else if (typeName.Equals("ItemModCookable"))
                {
                    var itemMod = definition.GetComponent<ItemModCookable>() ?? definition.gameObject.AddComponent<ItemModCookable>();
                    itemMod.cookTime = mod.GetFloat("cookTime", 30f);
                    itemMod.amountOfBecome = mod.GetInt("amountOfBecome", 1);
                    itemMod.lowTemp = mod.GetInt("lowTemp", 0);
                    itemMod.highTemp = mod.GetInt("highTemp", 0);
                    itemMod.setCookingFlag = mod.GetBoolean("setCookingFlag", false);
                    itemMod.becomeOnCooked = GetItem(mod, "becomeOnCooked");
                }
                else if (typeName.Equals("ItemModReveal"))
                {
                    var itemMod = definition.GetComponent<ItemModReveal>();
                    itemMod.revealedItemAmount = mod.GetInt("revealedItemAmount", 1);
                    itemMod.numForReveal = mod.GetInt("numForReveal", 1);
                    itemMod.revealedItemOverride = GetItem(mod, "revealedItemOverride");
                }
                else if (typeName.Equals("ItemModUpgrade"))
                {
                    var itemMod = definition.GetComponent<ItemModUpgrade>();
                    itemMod.numForUpgrade = mod.GetInt("numForUpgrade", 10);
                    itemMod.upgradeSuccessChance = mod.GetFloat("upgradeSuccessChance", 1f);
                    itemMod.numToLoseOnFail = mod.GetInt("numToLoseOnFail", 2);
                    itemMod.numUpgradedItem = mod.GetInt("numUpgradedItem", 1);
                    itemMod.upgradedItem = GetItem(mod, "upgradedItem");
                }
                else if (typeName.Equals("ItemModRecycleInto"))
                {
                    var itemMod = definition.GetComponent<ItemModRecycleInto>();
                    itemMod.numRecycledItemMin = mod.GetInt("numRecycledItemMin", 1);
                    itemMod.numRecycledItemMax = mod.GetInt("numRecycledItemMax", 1);
                    itemMod.recycleIntoItem = GetItem(mod, "recycleIntoItem");
                }
                else if (typeName.Equals("ItemModXPWhenUsed"))
                {
                    var itemMod = definition.GetComponent<ItemModXPWhenUsed>();
                    itemMod.xpPerUnit = mod.GetFloat("xpPerUnit", 0);
                    itemMod.unitSize = mod.GetInt("unitSize", 1);
                }
                else if (typeName.Equals("ItemModSwap"))
                {
                    var itemMod = definition.GetComponent<ItemModSwap>();
                    itemMod.sendPlayerDropNotification = mod.GetBoolean("sendPlayerDropNotification", false);
                    itemMod.sendPlayerPickupNotification = mod.GetBoolean("sendPlayerPickupNotification", false);
                    var items = new List<ItemAmount>();
                    var becomeItems = mod.GetArray("becomeItem");
                    foreach (var content in becomeItems)
                    {
                        var itemDef = GetItem(content.Obj, "shortname");
                        if (itemDef == null) continue;
                        items.Add(new ItemAmount(itemDef, content.Obj.GetFloat("amount", 0)));
                    }
                    itemMod.becomeItem = items.ToArray();
                }
                else if (typeName.Equals("ItemModProjectile") || typeName.Equals("ItemModProjectileSpawn"))
                {
                    var itemMod = definition.GetComponent<ItemModProjectile>();
                    var projectile = itemMod.projectileObject.Get().GetComponent<Projectile>();
                    projectile.drag = mod.GetFloat("drag", 0);
                    projectile.thickness = mod.GetFloat("thickness", 0);
                    projectile.remainInWorld = mod.GetBoolean("remainInWorld", false);
                    projectile.breakProbability = mod.GetFloat("breakProbability", 0);
                    projectile.stickProbability = mod.GetFloat("stickProbability", 1f);
                    projectile.ricochetChance = mod.GetFloat("ricochetChance", 0);
                    projectile.penetrationPower = mod.GetFloat("penetrationPower", 1f);
                    UpdateDamageTypes(mod.GetArray("damageTypes"), projectile.damageTypes);
                    var spawn = itemMod as ItemModProjectileSpawn;
                    if (spawn != null)
                    {
                        spawn.createOnImpactChance = mod.GetFloat("createOnImpactChance", 0);
                        spawn.spreadAngle = mod.GetFloat("spreadAngle", 30);
                    }
                    var projMods = mod.GetArray("mods");
                    var i = 0;
                    foreach (var projMod in projMods)
                    {
                        var curMod = (ItemModProjectileRadialDamage) itemMod.mods[i++];
                        curMod.radius = projMod.Obj.GetFloat("radius", 0);
                        curMod.damage.amount = projMod.Obj.GetObject("damage").GetFloat("amount", 0);
                        curMod.damage.type = (DamageType)Enum.Parse(typeof(DamageType), projMod.Obj.GetObject("damage").GetString("type", ""));
                    }
                }
                else if (typeName.EndsWith("TimedExplosive") || typeName.EndsWith("FlameExplosive")
                    || typeName.EndsWith("SupplySignal") || typeName.EndsWith("SurveyCharge"))
                {
                    TimedExplosive timedExplosive;
                    if (typeName.StartsWith("ItemModProjectile"))
                    {
                        var itemMod = definition.GetComponent<ItemModProjectile>();
                        timedExplosive = itemMod.projectileObject.Get().GetComponent<TimedExplosive>();
                    }
                    else if (typeName.StartsWith("ItemModEntity"))
                    {
                        var itemMod = definition.GetComponent<ItemModEntity>();
                        timedExplosive = itemMod.entityPrefab.Get().GetComponent<ThrownWeapon>().prefabToThrow.Get().GetComponent<TimedExplosive>();
                    }
                    else
                        continue;
                    var flameExplosive = timedExplosive as FlameExplosive;
                    if (flameExplosive != null)
                    {
                        flameExplosive.maxVelocity = mod.GetFloat("maxVelocity", 5);
                        flameExplosive.minVelocity = mod.GetFloat("minVelocity", 2);
                        flameExplosive.numToCreate = mod.GetFloat("numToCreate", 10);
                        flameExplosive.spreadAngle = mod.GetFloat("spreadAngle", 90);
                    }
                    else
                    {
                        var dudTimedExplosive = timedExplosive as DudTimedExplosive;
                        if (dudTimedExplosive != null)
                            dudTimedExplosive.dudChance = mod.GetFloat("dudChance", 0.3f);
                    }
                    timedExplosive.canStick = mod.GetBoolean("canStick", false);
                    timedExplosive.minExplosionRadius = mod.GetFloat("minExplosionRadius", 0);
                    timedExplosive.explosionRadius = mod.GetFloat("explosionRadius", 10);
                    timedExplosive.timerAmountMax = mod.GetFloat("timerAmountMax", 20);
                    timedExplosive.timerAmountMin = mod.GetFloat("timerAmountMin", 10);
                    UpdateDamageTypes(mod.GetArray("damageTypes"), timedExplosive.damageTypes);
                }
                else if (typeName.Equals("ItemModProjectileServerProjectile"))
                {
                    var itemMod = definition.GetComponent<ItemModProjectile>();
                    var projectile = itemMod.projectileObject.Get().GetComponent<ServerProjectile>();
                    projectile.drag = mod.GetFloat("drag", 0);
                    projectile.gravityModifier = mod.GetFloat("gravityModifier", 0);
                    projectile.speed = mod.GetFloat("speed", 0);
                }
                else if (typeName.Equals("ItemModEntityBaseMelee"))
                {
                    var itemMod = definition.GetComponent<ItemModEntity>();
                    var baseMelee = itemMod.entityPrefab.Get().GetComponent<BaseMelee>();
                    baseMelee.attackRadius = mod.GetFloat("attackRadius", 0.3f);
                    baseMelee.isAutomatic = mod.GetBoolean("isAutomatic", true);
                    baseMelee.maxDistance = mod.GetFloat("maxDistance", 1.5f);
                    baseMelee.repeatDelay = mod.GetFloat("repeatDelay", 1.0f);
                    var gathering = mod.GetObject("gathering");
                    UpdateGatherPropertyEntry(baseMelee.gathering.Ore, gathering.GetObject("Ore"));
                    UpdateGatherPropertyEntry(baseMelee.gathering.Flesh, gathering.GetObject("Flesh"));
                    UpdateGatherPropertyEntry(baseMelee.gathering.Tree, gathering.GetObject("Tree"));
                    UpdateDamageTypes(mod.GetArray("damageTypes"), baseMelee.damageTypes);
                }
                else if (typeName.Equals("ItemModEntityBaseProjectile"))
                {
                    var itemMod = definition.GetComponent<ItemModEntity>();
                    var baseProjectile = itemMod.entityPrefab.Get().GetComponent<BaseProjectile>();
                    baseProjectile.primaryMagazine.contents = mod.GetInt("contents", 4);
                    baseProjectile.primaryMagazine.ammoType = GetItem(mod, "ammoType");
                    baseProjectile.primaryMagazine.definition.builtInSize = mod.GetInt("builtInSize", 30);
                    //baseProjectile.primaryMagazine.definition.ammoTypes = FromJsonString<AmmoTypes>(mod.GetString("ammoTypes"));
                }
                else if (typeName.Equals("ItemModWearable"))
                {
                    var itemMod = definition.GetComponent<ItemModWearable>();
                    itemMod.targetWearable.occupationOver = GetOccupationSlot(mod.GetObject("targetWearable").GetValue("occupationOver"));
                    itemMod.targetWearable.occupationUnder = GetOccupationSlot(mod.GetObject("targetWearable").GetValue("occupationUnder"));
                    if (itemMod?.protectionProperties != null)
                    {
                        var protectionObj = mod.GetObject("protection");
                        var entry = itemMod.protectionProperties;
                        entry.density = protectionObj.GetFloat("density", 1f);
                        var amounts = protectionObj.GetObject("amounts");
                        foreach (var amount in amounts)
                            entry.amounts[(int) Enum.Parse(typeof(DamageType), amount.Key)] = (float) amount.Value.Number;
                    }
                    if (itemMod?.armorProperties != null)
                        itemMod.armorProperties.area = (HitArea) Enum.Parse(typeof(HitArea), mod.GetString("armor"), true);
                }
                else if (typeName.Equals("ItemModAlterCondition"))
                {
                    var itemMod = definition.GetComponentsInChildren<ItemModAlterCondition>(true);
                    itemMod[0].conditionChange = mod.GetFloat("conditionChange", 0);
                }
                else if (typeName.Equals("ItemModConditionHasFlag") || typeName.Equals("ItemModCycle")
                    || typeName.Equals("ItemModConditionContainerFlag") || typeName.Equals("ItemModSwitchFlag")
                    || typeName.Equals("ItemModUseContent") || typeName.Equals("ItemModConditionHasContents"))
                {
                    continue;
                }
                else
                {
                    Puts("Unknown type: {0}", typeName);
                }
            }
        }

        private ItemDefinition GetItem(JSONObject obj, string key)
        {
            var value = obj.GetValue(key);
            return value.Type == JSONValueType.String ? GetItem(value.Str) : null;
        }

        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || _itemsDict == null) return null;
            ItemDefinition item;
            if (_itemsDict.TryGetValue(shortname, out item)) return item;
            Puts("Item does not exist: " + shortname);
            return null;
        }

        private Rarity GetRarity(JSONObject item)
        {
            var rarity = "None";
            if (item.ContainsKey("rarity"))
            {
                rarity = item["rarity"].Type == JSONValueType.String ? item.GetString("rarity", "None") : item.GetInt("rarity", 0).ToString();
            }
            return (Rarity)Enum.Parse(typeof(Rarity), rarity);
        }

        private static Wearable.OccupationSlots GetOccupationSlot(JSONValue value)
        {
            if (value.Type == JSONValueType.String && !string.IsNullOrEmpty(value.Str))
            {
                return (Wearable.OccupationSlots)JsonConvert.DeserializeObject<OccupationSlotsUnity>(@"""" + value.Str + @"""", new UnityEnumConverter());
            }
            return 0;
        }

        private static void UpdateDamageTypes(JSONArray newDamageTypes, List<DamageTypeEntry> damageTypes)
        {
            damageTypes.Clear();
            damageTypes.AddRange(newDamageTypes.Select(damageType => new DamageTypeEntry
            {
                amount = damageType.Obj.GetFloat("amount", 0), type = (DamageType) Enum.Parse(typeof (DamageType), damageType.Obj.GetString("type", ""))
            }));
        }

        private void UpdateGatherPropertyEntry(ResourceDispenser.GatherPropertyEntry entry, JSONObject obj)
        {
            entry.conditionLost = obj.GetFloat("conditionLost", 0);
            entry.destroyFraction = obj.GetFloat("destroyFraction", 0);
            entry.gatherDamage = obj.GetFloat("gatherDamage", 0);
        }

        private JSONValue ObjectToJsonObject(object obj)
        {
            if (obj == null)
            {
                return new JSONValue(JSONValueType.Null);
            }
            if (obj is string)
            {
                return new JSONValue((string) obj);
            }
            if (obj is double)
            {
                return new JSONValue((double) obj);
            }
            if (obj is int)
            {
                return new JSONValue((int)obj);
            }
            if (obj is bool)
            {
                return new JSONValue((bool)obj);
            }
            var dict = obj as Dictionary<string, object>;
            if (dict != null)
            {
                var newObj = new JSONObject();
                foreach (var prop in dict)
                {
                    newObj.Add(prop.Key, ObjectToJsonObject(prop.Value));
                }
                return newObj;
            }
            var list = obj as List<object>;
            if (list != null)
            {
                var arr = new JSONArray();
                foreach (var o in list)
                {
                    arr.Add(ObjectToJsonObject(o));
                }
                return arr;
            }
            Puts("Unknown: " + obj.GetType().FullName + " Value: " + obj);
            return new JSONValue(JSONValueType.Null);
        }

        private object JsonObjectToObject(JSONValue obj)
        {
            switch (obj.Type)
            {
                case JSONValueType.String:
                    return obj.Str;
                case JSONValueType.Number:
                    return obj.Number;
                case JSONValueType.Boolean:
                    return obj.Boolean;
                case JSONValueType.Null:
                    return null;
                case JSONValueType.Array:
                    return obj.Array.Select(v => JsonObjectToObject(v.Obj)).ToList();
                case JSONValueType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in obj.Obj)
                    {
                        dict[prop.Key] = JsonObjectToObject(prop.Value);
                    }
                    return dict;
                default:
                    Puts("Missing type: " + obj.Type);
                    break;
            }
            return null;
        }

        [ConsoleCommand("item.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateItems();
            NextTick(() => Puts("Item config reloaded."));
        }

        [ConsoleCommand("item.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (!CreateDefaultConfig())
                return;
            UpdateItems();
        }

        [Flags]
        enum OccupationSlotsUnity
        {
            Everything = -1,
            Nothing = 0,
            HeadTop = 1,
            Face = 2,
            HeadBack = 4,
            TorsoFront = 8,
            TorsoBack = 16,
            LeftShoulder = 32,
            RightShoulder = 64,
            LeftArm = 128,
            RightArm = 256,
            LeftHand = 512,
            RightHand = 1024,
            Groin = 2048,
            Bum = 4096,
            LeftKnee = 8192,
            RightKnee = 16384,
            LeftLeg = 32768,
            RightLeg = 65536,
            LeftFoot = 131072,
            RightFoot = 262144
        }

        class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive || property.PropertyType == typeof(List<ItemAmount>) ||
                             property.PropertyType == typeof(ItemAmount[]) ||
                             property.PropertyType == typeof(List<DamageTypeEntry>) ||
                             property.PropertyType == typeof(DamageTypeEntry) ||
                             property.PropertyType == typeof(DamageType) ||
                             property.PropertyType == typeof(List<ItemModConsumable.ConsumableEffect>) ||
                             property.PropertyType == typeof(ItemModProjectileRadialDamage) ||
                             property.PropertyType == typeof(MetabolismAttribute.Type) ||
                             property.PropertyType == typeof(Rarity) ||
                             property.PropertyType == typeof(ItemCategory) ||
                             property.PropertyType == typeof(HitArea) ||
                             property.PropertyType == typeof(ItemDefinition) ||
                             property.PropertyType == typeof(ItemDefinition.Condition) ||
                             property.PropertyType == typeof(Wearable) ||
                             property.PropertyType == typeof(MinMax) ||
                             property.PropertyType == typeof(Wearable.OccupationSlots) ||
                             property.PropertyType == typeof(ResourceDispenser.GatherProperties) ||
                             property.PropertyType == typeof(ResourceDispenser.GatherPropertyEntry) ||
                             property.PropertyType == typeof(String);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => (p.DeclaringType == type || p.DeclaringType == typeof(TimedExplosive) || p.DeclaringType == typeof(BaseMelee)) && IsAllowed(p)).ToList();
            }
        }

        private class UnityEnumConverter : StringEnumConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null || ((Enum)value).ToString("G")[0] != '-')
                {
                    base.WriteJson(writer, value, serializer);
                    return;
                }
                var objectType = value.GetType();
                var isNullable = (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>));
                var t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
                if (!Enum.IsDefined(t, -1))
                {
                    base.WriteJson(writer, value, serializer);
                    return;
                }
                var everything = Enum.GetName(t, -1);
                var tmp = new JTokenWriter();
                base.WriteJson(tmp, Enum.ToObject(t, ~(int)value), serializer);
                var result = tmp.Token.Value<string>();
                var values = new List<string> { everything };
                if (result.IndexOf(',') != -1)
                {
                    var names = result.Split(',');
                    for (var i = 0; i < names.Length; i++)
                        names[i] = names[i].Trim();
                    values.AddRange(names);
                }
                else
                    values.Add(result);
                writer.WriteValue(string.Join(", ", values.ToArray()));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String || reader.Value.ToString().IndexOf(',') == -1) return base.ReadJson(reader, objectType, existingValue, serializer);
                var enumText = reader.Value.ToString();
                var isNullable = (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>));
                var t = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
                if (!Enum.IsDefined(t, -1))
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                var everything = Enum.GetName(t, -1);
                var inverted = false;
                var names = enumText.Split(',');
                for (var i = 0; i < names.Length; i++)
                {
                    names[i] = names[i].Trim();
                    if (!names[i].Equals(everything, StringComparison.OrdinalIgnoreCase)) continue;
                    names[i] = null;
                    inverted = true;
                }
                names = names.Where(n => n != null).ToArray();

                enumText = string.Join(", ", names);
                reader = new JTokenReader(new JValue(enumText));
                reader.Read();
                var result = base.ReadJson(reader, objectType, existingValue, serializer);
                return inverted ? ~(int)result : result;
            }
        }

    }
}
