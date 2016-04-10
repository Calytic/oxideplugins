// Reference: Newtonsoft.Json

using System;
using System.Collections.Generic;
using System.Linq;

using Rust;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using UnityEngine;

using JSONObject = JSON.Object;
using JSONArray = JSON.Array;
using JSONValue = JSON.Value;
using JSONValueType = JSON.ValueType;

namespace Oxide.Plugins
{
    [Info("ConstructionConfig", "Nogrod", "1.0.8", ResourceId = 859)]
    class ConstructionConfig : RustPlugin
    {
        private string _configpath = "";
        private bool _loaded;
        private const int VersionConfig = 4;

        void Loaded()
        {
            _configpath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
        }

        private new void LoadDefaultConfig()
        {
        }

        private static JSONObject ToJsonObject(object obj)
        {
            return JSONObject.Parse(ToJsonString(obj));
        }

        private static JSONArray ToJsonArray(object obj)
        {
            return JSONArray.Parse(ToJsonString(obj));
        }

        private static string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new DynamicContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            });
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
            var constructions = new Dictionary<string, object>();
            Config["Constructions"] = constructions;
            var deployables = new Dictionary<string, object>();
            Config["Deployables"] = deployables;
            var protectionProperties = new HashSet<ProtectionProperties>();
            var constructionPrefabs = PrefabAttribute.server.GetAll<Construction>();
            foreach (var construct in constructionPrefabs)
            {
                if (construct.deployable != null)
                {
                    var entity = GameManager.server.FindPrefab(StringPool.Get(construct.deployable.prefabID)).GetComponent<BaseCombatEntity>();
                    var deployable = new Dictionary<string, object>();
                    deployable["health"] = entity.startHealth;
                    if (entity.baseProtection != null)
                    {
                        protectionProperties.Add(entity.baseProtection);
                        deployable["protection"] = entity.baseProtection.name;
                    }
                    deployables[construct.fullName] = deployable;
                    continue;
                }
                var construction = new Dictionary<string, object>();
                var grades = new Dictionary<string, object>();
                construction["costMultiplier"] = construct.costMultiplier;
                construction["healthMultiplier"] = construct.healthMultiplier;
                for (var g = 0; g < construct.grades.Length; g++)
                {
                    var grade = construct.grades[g];
                    if (grade == null) continue;
                    var dict = new Dictionary<string, object>();
                    dict["baseHealth"] = grade.gradeBase.baseHealth;
                    var costToBuild = ToJsonArray(grade.gradeBase.baseCost);
                    foreach (var cost in costToBuild)
                    {
                        cost.Obj["shortname"] = cost.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                        cost.Obj.Remove("itemid");
                        cost.Obj.Remove("itemDef");
                    }
                    dict["baseCost"] = JsonObjectToObject(costToBuild);
                    if (grade.gradeBase.damageProtecton != null)
                        protectionProperties.Add(grade.gradeBase.damageProtecton);
                    grades[((BuildingGrade.Enum)g).ToString()] = dict;
                }
                construction["grades"] = grades;
                constructions[construct.fullName] = construction;
            }
            var protections = new Dictionary<string, object>();
            Config["DamageProtections"] = protections;
            foreach (var protectionProperty in protectionProperties)
            {
                var damageProtection = new Dictionary<string, object>();
                for (var i = 0; i < protectionProperty.amounts.Length; i++)
                {
                    damageProtection[Enum.GetName(typeof(DamageType), i)] = protectionProperty.amounts[i];
                }
                protections[protectionProperty.name] = damageProtection;
            }
            var directionProperties = PrefabAttribute.server.GetAll<DirectionProperties>();
            if (directionProperties != null)
            {
                var directions = new Dictionary<string, object>();
                Config["DirectionProperties"] = directions;
                foreach (var directionProperty in directionProperties)
                {
                    //Puts("DirectionProperty: {0} Radius: {1} Dupe: {2}", directionProperty.fullName, directionProperty.radius, directions.ContainsKey(directionProperty.fullName));
                    var direction = new Dictionary<string, object>();
                    direction["center"] = Vector3ToString(directionProperty.bounds.center);
                    direction["size"] = Vector3ToString(directionProperty.bounds.size);
                    if (directionProperty.extraProtection?.amounts != null)
                    {
                        var damageProtection = new Dictionary<string, object>();
                        for (var i = 0; i < directionProperty.extraProtection.amounts.Length; i++)
                        {
                            damageProtection[Enum.GetName(typeof (DamageType), i)] = directionProperty.extraProtection.amounts[i];
                        }
                        direction["extraProtection"] = damageProtection;
                    }
                    directions[directionProperty.fullName] = direction;
                }
            }
            try
            {
                Config.Save(_configpath);
            }
            catch (Exception e)
            {
                Puts(e.Message);
                return false;
            }
            Puts("Created new config");
            return LoadConfig();
        }

        private new bool LoadConfig()
        {
            try
            {
                if (!Config.Exists())
                    return CreateDefaultConfig();
                Config.Load(_configpath);
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
            if (Config["Version"] != null && (int)Config["Version"] == Protocol.network && Config["VersionConfig"] != null && (int)Config["VersionConfig"] == VersionConfig) return;
            Puts("Incorrect config version({0}/{1}) move to .old", Config["Version"], Config["VersionConfig"]);
            if (Config["Version"] != null) Config.Save($"{_configpath}.old");
            CreateDefaultConfig();
        }

        void OnTerrainInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateConstructions();
            _loaded = true;
        }

        void OnServerInitialized()
        {
            if (!_loaded) OnTerrainInitialized();
        }

        private void UpdateConstructions()
        {
            var constructions = Config["Constructions"] as Dictionary<string, object>;
            if (constructions == null)
            {
                Puts("No constructions in config");
                return;
            }
            var deployables = Config["Deployables"] as Dictionary<string, object>;
            if (deployables == null)
            {
                Puts("No deployables in config");
                return;
            }
            var entites = UnityEngine.Resources.FindObjectsOfTypeAll<BaseCombatEntity>();
            var oldGrades = new HashSet<BuildingGrade>();
            var changedGrades = new HashSet<BuildingGrade>();
            var protectionProperties = new HashSet<ProtectionProperties>();
            var constructionPrefabs = PrefabAttribute.server.GetAll<Construction>();
            var itemsDict = ItemManager.itemList.ToDictionary(i => i.shortname);
            foreach (var common in constructionPrefabs)
            {
                if (common.deployable != null)
                {
                    if (!deployables.ContainsKey(common.fullName))
                    {
                        Puts("Deployable '{0}' doesn't exist in config", common.fullName);
                        continue;
                    }
                    var deployable = (Dictionary<string, object>)deployables[common.fullName];
                    var startHealth = Convert.ToSingle(deployable["health"]);
                    var prefabs = entites.Where(e => e.prefabID == common.deployable.prefabID);
                    foreach (var prefab in prefabs)
                    {
                        if (prefab.isActiveAndEnabled && prefab.IsAlive())
                        {
                            var oldHealth = prefab.health;
                            prefab.InitializeHealth(startHealth * UnityEngine.Mathf.Clamp01(prefab.healthFraction), startHealth);
                            if (prefab.health != oldHealth)
                                prefab.OnHealthChanged(oldHealth, prefab.health);
                        }
                        prefab.startHealth = startHealth;
                    }
                    continue;
                }
                if (!constructions.ContainsKey(common.fullName))
                {
                    Puts("Construction '{0}' doesn't exist in config", common.fullName);
                    continue;
                }
                var construction = ObjectToJsonObject(constructions[common.fullName]);
                common.costMultiplier = construction.Obj.GetFloat("costMultiplier", 0);
                var healthChanged = common.healthMultiplier != construction.Obj.GetFloat("healthMultiplier", 0);
                common.healthMultiplier = construction.Obj.GetFloat("healthMultiplier", 0);
                var grades = construction.Obj.GetObject("grades");
                for (var g = 0; g < common.grades.Length; g++)
                {
                    var gradeType = (BuildingGrade.Enum) g;
                    if (!grades.ContainsKey(gradeType.ToString()))
                    {
                        common.grades[g] = null;
                        continue;
                    }
                    if (common.grades[g] == null)
                    {
                        Puts("Can't create grade: {0} for: {1}", gradeType, common.fullName);
                        continue;
                    }
                    var grade = UnityEngine.Object.Instantiate(common.grades[g].gradeBase);
                    grade.name = grade.name.Replace("(Clone)", "");
                    oldGrades.Add(common.grades[g].gradeBase);
                    common.grades[g].gradeBase = grade;
                    var newGrade = grades.GetObject(gradeType.ToString());
                    if (UpdateConstructionHealth(grade, newGrade.GetFloat("baseHealth", 0), healthChanged))
                        changedGrades.Add(grade);
                    grade.baseCost.Clear();
                    var costToBuild = newGrade.GetArray("baseCost");
                    foreach (var cost in costToBuild)
                    {
                        var shortname = cost.Obj.GetString("shortname", "unnamed");
                        ItemDefinition definition;
                        if (!itemsDict.TryGetValue(shortname, out definition))
                        {
                            Puts("baseCost item: {0} for: {1} not found", shortname, common.fullName);
                            continue;
                        }
                        grade.baseCost.Add(new ItemAmount(definition, cost.Obj.GetFloat("amount", 0)));
                    }
                    if (grade.damageProtecton != null)
                        protectionProperties.Add(grade.damageProtecton);
                }
            }
            foreach (var oldGrade in oldGrades)
                UnityEngine.Object.Destroy(oldGrade);
            var bb = entites.Where(b => b is StabilityEntity && b.isActiveAndEnabled && b.IsAlive());
            foreach (var entity in bb)
            {
                if (entity is BuildingBlock)
                {
                    if (changedGrades.Contains(((BuildingBlock) entity).currentGrade.gradeBase))
                        ((BuildingBlock) entity).SetHealthToMax();
                }
                else
                    entity.health = entity.MaxHealth();
            }
            var protections = Config["DamageProtections"] as Dictionary<string, object>;
            if (protections != null)
            {
                foreach (var protectionProperty in protectionProperties)
                {
                    if (!protections.ContainsKey(protectionProperty.name))
                    {
                        Puts("Protection '{0}' doesn't exist in config", protectionProperty.name);
                        continue;
                    }
                    var damageProtection = protections[protectionProperty.name] as Dictionary<string, object>;
                    if (damageProtection == null) continue;
                    protectionProperty.Clear();
                    foreach (var o in damageProtection)
                        protectionProperty.Add((DamageType) Enum.Parse(typeof (DamageType), o.Key), Convert.ToSingle(o.Value));
                }
            }
            var directionProperties = PrefabAttribute.server.GetAll<DirectionProperties>();
            if (directionProperties != null)
            {
                var directions = Config["DirectionProperties"] as Dictionary<string, object>;
                foreach (var directionProperty in directionProperties)
                {
                    if (!directions.ContainsKey(directionProperty.fullName))
                    {
                        Puts("DirectionProperty '{0}' doesn't exist in config", directionProperty.fullName);
                        continue;
                    }
                    var direction = directions[directionProperty.fullName] as Dictionary<string, object>;
                    directionProperty.bounds.center = Vector3FromString(Convert.ToString(direction["center"]));
                    directionProperty.bounds.size = Vector3FromString(Convert.ToString(direction["size"]));
                    var damageProtection = direction["extraProtection"] as Dictionary<string, object>;
                    if (damageProtection == null) continue;
                    directionProperty.extraProtection.Clear();
                    foreach (var o in damageProtection)
                        directionProperty.extraProtection.Add((DamageType)Enum.Parse(typeof(DamageType), o.Key), Convert.ToSingle(o.Value));
                }
            }
        }

        private string Vector3ToString(Vector3 vector)
        {
            return $"{vector.x} {vector.y} {vector.z}";
        }

        private Vector3 Vector3FromString(string data)
        {
            var values = data.Trim().Split(' ');
            return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
        }

        private bool UpdateConstructionHealth(BuildingGrade grade, float newHealth, bool healthChanged)
        {
            if (!healthChanged && grade.baseHealth == newHealth) return false;
            grade.baseHealth = newHealth;
            return true;
        }

        private JSONValue ObjectToJsonObject(object obj)
        {
            if (obj == null)
            {
                return new JSONValue(JSONValueType.Null);
            }
            if (obj is string)
            {
                return new JSONValue((string)obj);
            }
            if (obj is double)
            {
                return new JSONValue((double)obj);
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

        [ConsoleCommand("construction.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateConstructions();
            Puts("Config reloaded.");
        }

        [ConsoleCommand("construction.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (!CreateDefaultConfig())
                return;
            UpdateConstructions();
        }

        class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive ||
                        property.PropertyType == typeof(List<ItemAmount>) ||
                        property.PropertyType == typeof(ItemDefinition) ||
                        property.PropertyType == typeof(BuildingGrade) ||
                        property.PropertyType == typeof(ConstructionGrade) ||
                        property.PropertyType == typeof(String);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => p.DeclaringType == type && IsAllowed(p)).ToList();
            }
        }
    }
}
