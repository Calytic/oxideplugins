using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Reflection;
using Oxide.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("BoobyTraps", "k1lly0u", "0.1.7", ResourceId = 1549)]
    class BoobyTraps : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        private bool Changed;

        StoredData storedData;
        private DynamicConfigFile BoobyTrapData;        
        private Dictionary<uint, bTraps> currentTraps = new Dictionary<uint, bTraps>();
        private Dictionary<uint, bTraps> tempTraps = new Dictionary<uint, bTraps>();
        private List<string> currentRadTraps = new List<string>();
        private Dictionary<string, string> langMsg = new Dictionary<string, string>();

        private FieldInfo serverinput;
        private static LayerMask GROUND_MASKS = LayerMask.GetMask("Terrain", "World", "Construction");

        //////////////////////////////////////////////////////////////////////////////////////
        // Oxide Hooks ///////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        void Loaded()
        {
            permission.RegisterPermission("boobytraps.explosives", this);
            permission.RegisterPermission("boobytraps.deployables", this);
            permission.RegisterPermission("boobytraps.elements", this);
            permission.RegisterPermission("boobytraps.admin", this);
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

            lang.RegisterMessages(messages, this);

            BoobyTrapData = Interface.Oxide.DataFileSystem.GetFile("BoobyTrap");
            BoobyTrapData.Settings.Converters = new JsonConverter[] { new StringEnumConverter(), new UnityVector3Converter(), };

            LoadData();
            LoadVariables();
            
            if (!plugins.Exists("ZoneManager"))
            {
                Puts(lang.GetMessage("noZoneManager", this));
                return;
            }           
                  
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void Unload()
        {
            removeAllZones();
            currentRadTraps.Clear();
            tempTraps.Clear();
        }             
        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;

            if ((entity is SupplyDrop) && (boobyTrapAirdrops))
            {
                var drop = entity as SupplyDrop;
                processDrop(drop);
                return;
            }
            if ((entity is LootContainer) && (boobyTrapLoot))
            {
                var box = entity as LootContainer;
                processLoot(box);
                return;
            }
        }
        void OnLootEntity(BasePlayer inventory, BaseEntity target)
        {
            if (target is StorageContainer)
            {
                checkActivateTrap(target as StorageContainer);           
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is StorageContainer)
            {
                checkActivateTrap(entity as StorageContainer);
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            removeTrap(entity.net.ID);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Configuration /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool boobyTrapAirdrops = true;
        bool boobyTrapLoot = true;
        bool radiationTraps = true;
        bool grenadeTraps = true;
        bool beancanTraps = true;
        bool explosiveTraps = true;
        bool mineTraps = true;
        bool bearTraps = true;
        bool shockTraps = true;
        bool fireTraps = true;
        bool useOwners = true;
        bool buildingPriv = true;

        float trapCountdown = 2f;
        float radiationDestroy = 60f;
        int airdropChance = 5;
        int lootChance = 5;
                       
        int radiationRadius = 10;
        int radiationAmount = 75;

        float fireDamage = 1f;
        float fireRadius = 2f;

        int mineAmount = 10;
        float mineRadius = 2f;

        int beartrapAmount = 10;
        float beartrapRadius = 2f;
        
        int computerAmount = 2;

        int lowgradeAmount = 50;
        int crudeoilAmount = 50;

        int grenadeAmount = 2;
        float grenadeRadius = 5f;
        float grenadeDamage = 75f;

        int beancanAmount = 2;
        float beancanRadius = 4f;
        float beancanDamage = 30f;

        int explosiveAmount = 2;
        float explosiveRadius = 10f;
        float explosiveDamage = 110f;

        float shockRadius = 2f;
        float shockDamage = 95f;

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            CheckCfg("Options - Autotraps - Airdrops", ref boobyTrapAirdrops);
            CheckCfg("Options - Autotraps - Airdrops - Chance", ref airdropChance);
            CheckCfg("Options - Autotraps - Loot Containers", ref boobyTrapLoot);
            CheckCfg("Options - Autotraps - Loot Container - Chance", ref lootChance);
            CheckCfg("Options - Traps - Trap timer", ref trapCountdown);
            CheckCfg("Options - Plugins - Use Owners ", ref useOwners);
            CheckCfg("Options - Tool Cupboard - Use Building Privileges ", ref buildingPriv);

            CheckCfg("Traps - Grenade", ref grenadeTraps);
            CheckCfg("Traps - Grenade - Buy Amount", ref grenadeAmount);
            CheckCfgFloat("Traps - Grenade - Damage", ref grenadeDamage);
            CheckCfgFloat("Traps - Grenade - Radius", ref grenadeRadius);

            CheckCfg("Traps - Beancan", ref beancanTraps);
            CheckCfg("Traps - Beancan - Buy Amount", ref beancanAmount);
            CheckCfgFloat("Traps - Beancan - Damage", ref beancanDamage);
            CheckCfgFloat("Traps - Beancan - Radius", ref beancanRadius);

            CheckCfg("Traps - Explosive", ref explosiveTraps);
            CheckCfg("Traps - Explosive - Buy Amount", ref explosiveAmount);
            CheckCfgFloat("Traps - Explosive - Damage", ref explosiveDamage);
            CheckCfgFloat("Traps - Explosive - Radius", ref explosiveRadius);

            CheckCfg("Traps - Landmine", ref mineTraps);
            CheckCfg("Traps - Landmine - Buy Amount", ref mineAmount);
            CheckCfgFloat("Traps - Landmine - Radius", ref mineRadius);

            CheckCfg("Traps - Beartraps", ref bearTraps);
            CheckCfg("Traps - Beartraps - Buy Amount", ref beartrapAmount);
            CheckCfgFloat("Traps - Beartraps - Radius", ref beartrapRadius);

            CheckCfg("Traps - Radiation", ref radiationTraps);
            CheckCfg("Traps - Radiation - Buy Amount", ref radiationAmount);
            CheckCfg("Traps - Radiation - Radius", ref radiationRadius);
            CheckCfgFloat("Traps - Radiation - Time to keep radiation active", ref radiationDestroy);

            CheckCfg("Traps - Shock", ref shockTraps);
            CheckCfgFloat("Traps - Shock - Damage", ref shockDamage);
            CheckCfgFloat("Traps - Shock - Radius", ref shockRadius);

            CheckCfg("Traps - Fire", ref fireTraps);
            CheckCfg("Traps - Fire - Buy Amount - Oil", ref crudeoilAmount);
            CheckCfg("Traps - Fire - Buy Amount - LowGrade", ref lowgradeAmount);
            CheckCfgFloat("Traps - Fire - Damage", ref fireDamage);
            CheckCfgFloat("Traps - Fire - Radius", ref fireRadius);


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
            {"title", "<color=orange>BoobyTraps</color> : " },
            {"noZoneManager", "ZoneManager is not installed, unable to generate radiation traps" },
            {"alreadyTrapped", "This box is already booby trapped!"},
            {"notYourBox", "This is not your box"},
            {"noBox", "You are not looking at a box"},
            {"needGrenades", "You need {0} Grenade(s) to set this trap"},
            {"needBeancans", "You need {0} Beancan Grenade(s) to set this trap"},
            {"needExplosives", "You need {0} Timed Explosive(s) to set this trap"},
            {"needMines", "You need {0} Landmines to set this trap"},
            {"needBeartraps", "You need {0} Beartraps to set this trap"},
            {"needComputer", "You need {0} Targeting Computer(s) to set this trap"},
            {"needFire", "You need {0} Low Grade Fuel and {1} Crude Oil to set this trap"},
            {"boxTrapped", "This box is now booby trapped with {0}!"},
            {"noPerms", "You do not have permission to use this command"},
            {"badSyntax", "Incorrect Syntax"},
            {"disGren", "Grenade traps are disabled"},            
            {"disBeancan", "Beancan traps are disabled"},
            {"disExplosive", "Explosive traps are disabled"},
            {"disMine", "Landmine traps are disabled"},
            {"disBear", "Beartrap traps are disabled"},
            {"disRads", "Radiation traps are disabled"},
            {"disShock", "Shock traps are disabled"},
            {"disFire", "Fire traps are disabled"},
            {"removedTrap", "You have removed this trap!"},
            {"noTraps", "This box has no traps"},
            {"checkConsole", "Check your console for the list"},
            {"boxClean", "This box is clean!"},
            {"eraseAll", "All traps erased!"},
            {"trappedWith", "This box is booby trapped with {0}!" },
            {"availableTraps", "Available trap names are;" },
            {"explos", "grenade, beancan, explosive" },
            {"deplo", "landmine, beartrap" },
            {"element", "shock, fire" },
            {"rads", "radiation" },
            {"noPrivs", "You must have building privilege to place a trap here" },
            {"activatedTrap", "You activated a booby trap!" }
        };

        //////////////////////////////////////////////////////////////////////////////////////
        // Data Management ///////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
               
        class StoredData
        {
            public readonly HashSet<bTraps> currentTraps = new HashSet<bTraps>();
        }
        void SaveData()
        {
            BoobyTrapData.WriteObject(storedData);
        }
        void LoadData()
        {
            try
            {
                BoobyTrapData.Settings.NullValueHandling = NullValueHandling.Ignore;
                storedData = BoobyTrapData.ReadObject<StoredData>();                
            }
            catch
            {
                Puts("Couldn't load BoobyTrap data, creating new datafile");
                storedData = new StoredData();
            }
            BoobyTrapData.Settings.NullValueHandling = NullValueHandling.Include;

            foreach (var box in storedData.currentTraps)
                currentTraps[box.ID] = box;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Booby Trap data class /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        class bTraps
        {
            public uint ID;
            public string Name;
            public int Trap;
            public Vector3 Position;
            public string TrapOwner;

            public bTraps()
            {

            }
            public bTraps(uint id, string name, int trapnum, Vector3 pos, string trapowner)
            {
                ID = id;
                Name = name;
                Trap = trapnum;
                Position = pos;
                TrapOwner = trapowner;
            }
        }
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        } // borrowed from ZoneManager

        //////////////////////////////////////////////////////////////////////////////////////
        // Booby Trap core functions /////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        private bool randomNumber(int chance)
        {
            int number1 = UnityEngine.Random.Range(1, chance);
            int number2 = UnityEngine.Random.Range(1, chance);
            if (number1 == number2)
                return true;
            return false;
        }
        private void processDrop(SupplyDrop drop)
        {
            bool proceed = randomNumber(airdropChance);
            if (!proceed) return;
            var lootDrop = drop.GetComponent<LootContainer>();
            assignTraps(drop);
        }
        private void processLoot(LootContainer container)
        {
            bool proceed = randomNumber(lootChance);
            if (!proceed) return;
            assignTraps(container);
        }
        private void assignTraps(LootContainer drop)
        {
            uint boxID = drop.net.ID;
            int randomTrap = randomTrapCheck();

            bTraps trapInfo;
            if (!tempTraps.TryGetValue(boxID, out trapInfo))
                trapInfo = new bTraps { ID = boxID };
            trapInfo.Position = drop.transform.position;
            trapInfo.Name = drop.panelName;
            trapInfo.Trap = randomTrap;
            trapInfo.TrapOwner = "boobytraps";

            tempTraps[boxID] = trapInfo;
            var trapName = convertToName(trapInfo.Trap.ToString());
            Puts("Random trap set at " + drop.transform.position + " using trap: " + trapName);
        }
        private int randomTrapCheck()
        {
            int num = UnityEngine.Random.Range(1, 8);
            switch (num)
            {
                case 1:
                    if (grenadeTraps)
                        return 1;
                    break;
                case 2:
                    if (beancanTraps)
                        return 2;
                    break;
                case 3:
                    if (explosiveTraps)
                        return 3;
                    break;
                case 4:
                    if (mineTraps)
                        return 4;
                    break;
                case 5:
                    if (bearTraps)
                        return 5;
                    break;
                case 6:
                    if ((radiationTraps) && (plugins.Exists("ZoneManager")))
                        return 6;
                    break;
                case 7:
                    if (shockTraps)
                        return 7;
                    break;
                case 8:
                    if (fireTraps)
                        return 8;
                    break;                    
            }
            int newNum = randomTrapCheck();
            return newNum;
        }
        private void playerTraps(BasePlayer player, int trap)
        {
            var box = findBox(player);
            if (box != null)
            {
                if (currentTraps.ContainsKey(box.net.ID))
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("alreadyTrapped", this, player.UserIDString));
                    return;
                }
                var name = convertToName(trap.ToString());
                if (canBoobyTrapAdmin(player))
                {
                    addTrap(player, box, trap);
                    return;
                }
                if (useOwners)
                {
                    var owner = box.OwnerID;
                    if (owner == 0 || owner == player.userID)
                    {
                        chargeTraps(player, box, trap);
                        return;
                    }
                    if (owner != player.userID)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("notYourBox", this, player.UserIDString));
                        return;
                    }
                }
                if (buildingPriv)
                {
                    if (!player.CanBuild())
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPrivs", this, player.UserIDString));
                        return;
                    }
                }
                chargeTraps(player, box, trap);
                return;
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noBox", this, player.UserIDString));
        }
        private void chargeTraps(BasePlayer player, StorageContainer box, int trap)
        {
            if (!(box.panelName == "largewoodbox" || box.panelName == "smallwoodbox" || box.panelName == "smallstash" || box.panelName == "generic")) return;

            int invGrenades = player.inventory.GetAmount(-1308622549);
            int invBeancans = player.inventory.GetAmount(384204160);
            int invExplosives = player.inventory.GetAmount(498591726);
            int invLandmines = player.inventory.GetAmount(255101535);
            int invBeartraps = player.inventory.GetAmount(1046072789);
            int invLowgrade = player.inventory.GetAmount(28178745);
            int invCrudeoil = player.inventory.GetAmount(1983936587);
            int invComputer = player.inventory.GetAmount(1490499512);            

            if (canBoobyTrapAdmin(player))
            {                
                addTrap(player, box, trap);
                return;
            }

            switch (trap)
            {
                case 1:
                    if (invGrenades >= grenadeAmount)
                    {
                        player.inventory.Take(null, -1308622549, grenadeAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needGrenades", this, player.UserIDString), grenadeAmount));
                    break;
                case 2:
                    if (invBeancans >= beancanAmount)
                    {
                        player.inventory.Take(null, 384204160, beancanAmount);
                        addTrap(player, box, trap);                        
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needBeancans", this, player.UserIDString), beancanAmount));
                    break;
                case 3:
                    if (invExplosives >= explosiveAmount)
                    {
                        player.inventory.Take(null, 498591726, explosiveAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needExplosives", this, player.UserIDString), explosiveAmount));
                    break;
                case 4:
                    if (invLandmines >= mineAmount)
                    {
                        player.inventory.Take(null, 255101535, mineAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needMines", this, player.UserIDString), mineAmount));
                    break;
                case 5:
                    if (invBeartraps >= beartrapAmount)
                    {
                        player.inventory.Take(null, 1046072789, beartrapAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needBeartraps", this, player.UserIDString), beartrapAmount));
                    break;
                case 6:
                    addTrap(player, box, trap);
                    break;
                case 7:
                    if (invComputer >= computerAmount)
                    {
                        player.inventory.Take(null, 1490499512, computerAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needComputer", this, player.UserIDString), computerAmount));
                    break;
                case 8:
                    if (invLowgrade >= lowgradeAmount && invCrudeoil >= lowgradeAmount)
                    {
                        player.inventory.Take(null, 1983936587, crudeoilAmount);
                        player.inventory.Take(null, 28178745, lowgradeAmount);
                        addTrap(player, box, trap);
                    }
                    else SendReply(player, string.Format(lang.GetMessage("needFire", this, player.UserIDString), lowgradeAmount, crudeoilAmount));
                    break;
                default:
                    return;                 
            }
        }
        private void addTrap(BasePlayer player, StorageContainer box, int trap)
        {
            uint boxID = box.net.ID;
            var trapName = convertToName(trap.ToString());

            bTraps trapInfo;
            if (!currentTraps.TryGetValue(boxID, out trapInfo))
                trapInfo = new bTraps { ID = boxID };
            trapInfo.Position = box.transform.position;
            trapInfo.Name = box.panelName;
            trapInfo.Trap = trap;
            trapInfo.TrapOwner = player.userID.ToString();

            if (!(trapInfo.Name == "generic"))
            {
                currentTraps[boxID] = trapInfo;
                storedData.currentTraps.Add(trapInfo);
                SaveData();
            }
            else tempTraps[boxID] = trapInfo;

            SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("boxTrapped", this, player.UserIDString), trapName));
        }
        private void checkActivateTrap(StorageContainer box)
        {
            uint boxID = box.net.ID;
            Vector3 targetPos = box.transform.position;

            if (currentTraps.ContainsKey(boxID) || tempTraps.ContainsKey(boxID))
            {
                bTraps trapinfo;
                if (currentTraps.TryGetValue(boxID, out trapinfo) || tempTraps.TryGetValue(boxID, out trapinfo))
                {
                    int trap = trapinfo.Trap;
                    if (trap == 0 || trap >= 9) return;
                    switch (trap)
                    {
                        case 1:
                            grenadeTrap(targetPos);
                            break;
                        case 2:
                            beancanTrap(targetPos);
                            break;
                        case 3:
                            explosiveTrap(targetPos);
                            break;
                        case 4:
                            landmineTrap(targetPos);
                            break;
                        case 5:
                            beartrapTrap(targetPos);
                            break;
                        case 6:
                            radiationTrap(targetPos);
                            break;
                        case 7:
                            shockTrap(targetPos);
                            break;
                        case 8:
                            fireTrap(targetPos);
                            break;

                        default:
                            Puts("trap not found");
                            return;
                    }                    
                }
                removeTrap(boxID);                
            }
        }
        object rayBox(Vector3 Pos, Vector3 Aim)
        {
            var hits = Physics.RaycastAll(Pos, Aim);
            float distance = 1000f;
            object target = null;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<StorageContainer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<StorageContainer>();
                    }
                }
                else if (hit.collider.GetComponentInParent<BasePlayer>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BasePlayer>();
                    }
                }
            }
            return target;
        }
        private StorageContainer findBox(BasePlayer player)
        {
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            Vector3 eyesAdjust = new Vector3(0f, 1.5f, 0f);

            var rayResult = rayBox(player.transform.position + eyesAdjust, currentRot);
            if (rayResult is StorageContainer)
            {
                var box = rayResult as StorageContainer;
                return box;
            }
            return null;
        }
        private Vector3 getRandomPosCircle(Vector3 pos, int ang, float radius)
        {            
            Vector3 randPos;
            randPos.x = pos.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            randPos.z = pos.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            randPos.y = pos.y;
            var targetPos = getGroundPosition(randPos);
            return targetPos;
        }
        private string convertToName(string num)
        {
            var trapnum = num.ToString();
            switch (trapnum.ToString())
            {
                case "1":
                    trapnum = "Grenades";
                    break;
                case "2":
                    trapnum = "Beancan Grenades";
                    break;
                case "3":
                    trapnum = "Explosives";
                    break;
                case "4":
                    trapnum = "Landmines";
                    break;
                case "5":
                    trapnum = "Beartraps";
                    break;
                case "6":
                    trapnum = "Radiation";
                    break;
                case "7":
                    trapnum = "Electricity";
                    break;
                case "8":
                    trapnum = "Fire";
                    break;

                default:
                    break;
            }
            return trapnum;
        }
        static Vector3 getGroundPosition(Vector3 sourcePos) // credit Wulf & Nogrod
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, GROUND_MASKS))
            {
                sourcePos.y = hitInfo.point.y;
            }
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }       
       
        private bool removeTrap(uint id)
        {
            if (currentTraps.ContainsKey(id))
            {
                bTraps trapInfo;
                currentTraps.TryGetValue(id, out trapInfo);
                storedData.currentTraps.Remove(trapInfo);
                currentTraps.Remove(id);
                SaveData();
                return true;
            }
            else if (tempTraps.ContainsKey(id))
            {
                tempTraps.Remove(id);
                return true;
            }
            return false;
        }
        private void removeAllZones()
        {
            foreach (var radtrap in currentRadTraps)
            {
                ZoneManager.Call("EraseZone", radtrap);
                Puts("Removed rad trap " + radtrap);
            }
        }       

        //////////////////////////////////////////////////////////////////////////////////////
        // Traps /////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        Rust.DamageType explodeDamage = Rust.DamageType.Explosion;
        Rust.DamageType flameDamage = Rust.DamageType.Heat;
        Rust.DamageType elecDamage = Rust.DamageType.ElectricShock;
        private void grenadeTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/weapons/f1 grenade/effects/bounce.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                Effect.server.Run("assets/prefabs/weapons/f1 grenade/effects/f1grenade_explosion.prefab", pos);
                dealDamage(pos, grenadeDamage, grenadeRadius, explodeDamage);
            });
        }
        private void explosiveTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", pos);
                dealDamage(pos, explosiveDamage, explosiveRadius, explodeDamage);
            });
        }
        private void beancanTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/weapons/beancan grenade/effects/bounce.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                Effect.server.Run("assets/prefabs/weapons/beancan grenade/effects/beancan_grenade_explosion.prefab", pos);
                dealDamage(pos, beancanDamage, beancanRadius, explodeDamage);
            });
        }
        private void radiationTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/bundled/prefabs/fx/smoke/generator_smoke.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                int randomNum = UnityEngine.Random.Range(1, 1000);
                string randID = ("boobytraps_" + randomNum);

                string[] zoneArgs = new string[4]; ;
                zoneArgs[0] = "radius";
                zoneArgs[1] = radiationRadius.ToString();
                zoneArgs[2] = "radiation";
                zoneArgs[3] = radiationAmount.ToString();

                if (pos == null)
                    return;

                ZoneManager?.Call("CreateOrUpdateZone", randID, zoneArgs, pos);
                currentRadTraps.Add(randID);

                timer.Once(radiationDestroy, () =>
                {
                    ZoneManager?.Call("EraseZone", randID);
                    currentRadTraps.Remove(randID);
                    Puts("Radiation trap zone " + randID + " removed.");
                });
            });
        }
        private void landmineTrap(Vector3 pos)
        {
            for (int i = 0; i < mineAmount; i++)
            {
                int ang = i * 32;
                Vector3 targetPos = getRandomPosCircle(pos, ang, mineRadius);
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/deployable/landmine/landmine.prefab", targetPos, new Quaternion(), true);
                entity.Spawn();
            }
        }
        private void beartrapTrap(Vector3 pos)
        {
            for (int i = 0; i < beartrapAmount; i++)
            {
                int ang = i * 32;
                Vector3 targetPos = getRandomPosCircle(pos, ang, beartrapRadius);
                BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/deployable/bear trap/beartrap.prefab", targetPos, new Quaternion(), true);
                entity.Spawn();
            }
        }
        private void shockTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                Effect.server.Run("assets/bundled/prefabs/fx/headshot.prefab", pos);
                dealDamage(pos, shockDamage, shockRadius, elecDamage);
            });            
        }
        private void fireTrap(Vector3 pos)
        {
            timer.Once(0.1f, () => Effect.server.Run("assets/prefabs/weapons/beancan grenade/effects/bounce.prefab", pos));
            timer.Once(trapCountdown, () =>
            {
                Effect.server.Run("assets/bundled/prefabs/fx/fire/fire_v3.prefab", pos);
                timer.Repeat(0.5f, 16, () =>
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/impacts/additive/fire.prefab", pos);
                    dealDamage(pos, fireDamage, fireRadius, flameDamage);
                });
            });
        }

        private void dealDamage(Vector3 deathPos, float damage, float radius, Rust.DamageType type)
        {
            List<BaseCombatEntity> entitiesClose = new List<BaseCombatEntity>();
            List<BaseCombatEntity> entitiesNear = new List<BaseCombatEntity>();
            List<BaseCombatEntity> entitiesFar = new List<BaseCombatEntity>();
            Vis.Entities(deathPos, radius / 3, entitiesClose);
            Vis.Entities(deathPos, radius / 2, entitiesNear);
            Vis.Entities(deathPos, radius, entitiesFar);

            foreach (BaseCombatEntity entity in entitiesClose)
            {
                entity.Hurt(damage, type, null, true);
                notifyPlayer(entity);
            }

            foreach (BaseCombatEntity entity in entitiesNear)
            {
                if (entitiesClose.Contains(entity)) return;
                entity.Hurt(damage / 2, type, null, true);
                notifyPlayer(entity);
            }

            foreach (BaseCombatEntity entity in entitiesFar)
            {
                if (entitiesClose.Contains(entity) || entitiesNear.Contains(entity)) return;
                entity.Hurt(damage / 4, type, null, true);
                notifyPlayer(entity);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Permission/Auth Check /////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////

        bool canBoobyTrapAdmin(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "boobytraps.admin")) return true;
            else if (isAuth(player)) return true;
            return false;
        }
        bool canBoobyTrapDeployable(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "boobytraps.deployables")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }
        bool canBoobyTrapExplosives(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "boobytraps.explosives")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }
        bool canBoobyTrapElements(BasePlayer player)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "boobytraps.elements")) return true;
            else if (isAuth(player)) return true;
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noPerms", this, player.UserIDString));
            return false;
        }
        bool isAuth(BasePlayer player)
        {
            if (player.net.connection.authLevel == 2) return true;            
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////
        // Chat Commands /////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        private void notifyPlayer(BaseEntity entity)
        {
            if (entity is BasePlayer)
            {
                var player = (BasePlayer)entity;
                SendReply(player, lang.GetMessage("activatedTrap", this, player.UserIDString));
            }
        }
        private void incorrectSyntax(BasePlayer player)
        {
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("badSyntax", this, player.UserIDString));
            SendReply(player, lang.GetMessage("availableTraps", this, player.UserIDString));
            if (canBoobyTrapExplosives(player) || (canBoobyTrapAdmin(player)))
            {
                SendReply(player, lang.GetMessage("explos", this, player.UserIDString));
            }
            if (canBoobyTrapDeployable(player) || (canBoobyTrapAdmin(player)))
            {
                SendReply(player, lang.GetMessage("deplo", this, player.UserIDString));
            }
            if (canBoobyTrapElements(player) || (canBoobyTrapAdmin(player)))
            {
                SendReply(player, lang.GetMessage("elements", this, player.UserIDString));
            }
            if (canBoobyTrapAdmin(player))
            SendReply(player, lang.GetMessage("rads", this, player.UserIDString));
        }

        [ChatCommand("settrap")]
        private void chatSetTrap(BasePlayer player, string command, string[] args)
        {
            if (!((canBoobyTrapAdmin(player)) || (canBoobyTrapDeployable(player)) || (canBoobyTrapElements(player)) || (canBoobyTrapExplosives(player)))) return;
            if ((args.Length == 0) || (args.Length >= 2))
            {
                incorrectSyntax(player);
                return;
            }
            switch (args[0].ToLower())
            {
                case "grenade":
                    if (!grenadeTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disGren", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapExplosives(player))
                        playerTraps(player, 1);
                    break;
                case "beancan":
                    if (!beancanTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disBeancan", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapExplosives(player))
                        playerTraps(player, 2);
                    break;
                case "explosive":
                    if (!explosiveTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disExplosive", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapExplosives(player))
                        playerTraps(player, 3);
                    break;
                case "landmine":
                    if (!mineTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disMine", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapDeployable(player))
                        playerTraps(player, 4);
                    break;
                case "beartrap":
                    if (!bearTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disBear", this, player.UserIDString));

                        return;
                    }
                    if (canBoobyTrapDeployable(player))
                        playerTraps(player, 5);
                    break;
                case "radiation":
                    if (!radiationTraps || !(plugins.Exists("ZoneManager")))
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disRads", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapAdmin(player))
                    {
                        if (plugins.Exists("ZoneManager"))
                            playerTraps(player, 6);
                        else SendReply(player, lang.GetMessage("noZoneManager", this, player.UserIDString));
                    }
                    break;
                case "shock":
                    if (!shockTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disShock", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapElements(player))
                        playerTraps(player, 7);
                    break;
                case "fire":
                    if (!fireTraps)
                    {
                        SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("disFire", this, player.UserIDString));
                        return;
                    }
                    if (canBoobyTrapElements(player))
                        playerTraps(player, 8);
                    break;

                default:
                    incorrectSyntax(player);
                return;
            }
        }        

        [ChatCommand("removetrap")]
        private void chatRemoveTrap(BasePlayer player, string command, string[] args)
        {
            if (!canBoobyTrapAdmin(player)) return;
            var box = findBox(player);
            if (box != null)
            {
                if (removeTrap(box.net.ID))
                {
                    SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("removedTrap", this, player.UserIDString));
                    return;
                }

                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noTraps", this, player.UserIDString));
                return;
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noBox", this, player.UserIDString));
        }

        [ChatCommand("listtraps")]
        private void chatListTrap(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;

            Puts("---- Storagebox trap list ----");
            if (currentTraps.Count == 0) Puts("none");
            foreach (var trap in storedData.currentTraps)
            {
                var type = convertToName(trap.Trap.ToString());
                Puts("ID - " + trap.ID + ", Position - " + trap.Position + ", Owner - " + trap.TrapOwner + ", Type - " + type);
            }

            if (boobyTrapAirdrops || boobyTrapLoot)
            {
                Puts("--- Despawnable lootbox trap list ---");
                if (tempTraps.Count == 0) Puts("none");
                foreach (var trap in tempTraps)
                {
                    bTraps info;
                    var key = trap.Key;                    
                    tempTraps.TryGetValue(key, out info);
                    var type = convertToName(info.Trap.ToString());
                    Puts("ID - " + info.ID + ", Position - " + info.Position + ", Owner - " + info.TrapOwner + ", Type - " + type);
                }
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("checkConsole", this, player.UserIDString));
        }

        [ChatCommand("checktrap")]
        private void chatCheckTrap(BasePlayer player, string command, string[] args)
        {
            if (!canBoobyTrapAdmin(player)) return;
            var box = findBox(player);
            if (box != null)
            {
                uint boxID = box.net.ID;
                Vector3 targetPos = box.transform.position;

                if (currentTraps.ContainsKey(boxID))
                {
                    bTraps trapinfo;
                    if (currentTraps.TryGetValue(boxID, out trapinfo))
                    {
                        var trapnum = convertToName(currentTraps[box.net.ID].Trap.ToString());
                        SendReply(player, string.Format(lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("trappedWith", this, player.UserIDString), trapnum));
                        return;
                    }
                }
                SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("boxClean", this, player.UserIDString));
                return;
            }
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("noBox", this, player.UserIDString));
        }

        [ChatCommand("erasealltraps")]
        private void chatEraseAllTrap(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;

            removeAllZones();
            currentRadTraps.Clear();
            tempTraps.Clear();
            storedData.currentTraps.Clear();
            SaveData();
            SendReply(player, lang.GetMessage("title", this, player.UserIDString) + lang.GetMessage("eraseAll", this, player.UserIDString));
        }
    }
}
