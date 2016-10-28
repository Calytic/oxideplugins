using System.Collections.Generic;
using System.Text;
using Oxide.Core.Configuration;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    //Body part scaling from k1lly0u's plugin, with permission (thanks, k1lly0u)
    //Further code cleanup/improvement with help of k1lly0u
    [Info("Weapon Damage Scaler", "Shady", "1.0.7", ResourceId = 1594)]
    [Description("Scale damage per weapon/ammo type, and per body part.")]
    internal class WeaponDamageScaler : RustPlugin
    {        
        bool configWasChanged = false;
        WeaponData weaponData;
        private DynamicConfigFile wData;

        #region Data Management
        class ItemStructure
        {
            public string Name;
            public float GlobalModifier;
            public Dictionary<string, float> IndividualParts;
        }
        class WeaponData
        {            
            public Dictionary<string, ItemStructure> Weapons = new Dictionary<string, ItemStructure>();
            public Dictionary<string, float> AmmoTypes = new Dictionary<string, float>();
        }

        private void InitializeWeaponData()
        {
            weaponData.Weapons.Clear();
            weaponData.AmmoTypes.Clear();
            for(int i = 0; i < ItemManager.itemList.Count; i++)
            {
                var definition = ItemManager.itemList[i];
                if (definition != null)
                {
                    if (definition.category == ItemCategory.Weapon || definition.category == ItemCategory.Tool || definition.category == ItemCategory.Ammunition)
                    {
                        if (!definition.shortname.Contains("mod"))
                            weaponData.Weapons.Add(definition.shortname, new ItemStructure { Name = definition.displayName.english, GlobalModifier = 1.0f, IndividualParts = CreateBodypartList() });
                    }
                    else if (definition.category == ItemCategory.Ammunition)
                        weaponData.AmmoTypes.Add(definition.shortname, 1.0f);
                }
            }      
            SaveData();        
        }
        private Dictionary<string, float> CreateBodypartList()
        {
            Dictionary<string, float> newData = new Dictionary<string, float>();
            for (int i = 0; i < Bodyparts.Length; i++) newData.Add(Bodyparts[i], 1.0f);
            return newData;
        }
        void SaveData() => wData.WriteObject(weaponData);
        void LoadData()
        {
            try
            {
                weaponData = Interface.GetMod().DataFileSystem.ReadObject<WeaponData>("damagescaler_data");
                if (weaponData == null || weaponData.Weapons.Count == 0)
                    InitializeWeaponData();
            }
            catch
            {
                PrintWarning("Unable to load data, creating new datafile!");
                weaponData = new WeaponData();                
            }
        }
        #endregion

        #region Config
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file.");
            Config.Clear();
            Config["UseGlobalDamageScaler"] = false;
            Config["GlobalDamageScaler"] = 1.0;
            Config["PlayersOnly"] = true;
            Config["AllowAuthLevel"] = false;
            Config["AuthLevel"] = 2;
            SaveConfig();
        }
        private void LoadVariables()
        {
            CheckConfigEntry("UseGlobalDamageScaler", false);
            CheckConfigEntry("GlobalDamageScaler", 1.0f);
            CheckConfigEntry("PlayersOnly", true);
            CheckConfigEntry("AllowAuthLevel", false);
            CheckConfigEntry("AuthLevel", 2);
            if (configWasChanged) SaveConfig();
        }
        private void CheckConfigEntry<T>(string key, T value)
        {
            if (Config[key] == null)
            {
                Config[key] = value;
                configWasChanged = true;
            }
        }
        public string[] Bodyparts = new string[]
               {
                    "r_forearm",
                    "l_forearm",
                    "l_upperarm",
                    "r_upperarm",
                    "r_hand",
                    "l_hand",
                    "pelvis",
                    "l_hip",
                    "r_hip",
                    "spine3",
                    "spine4",
                    "spine1",
                    "spine2",
                    "r_knee",
                    "r_foot",
                    "r_toe",
                    "l_knee",
                    "l_foot",
                    "l_toe",
                    "head",
                    "neck",
                    "jaw",
                    "r_eye",
                    "l_eye"
               };
        #endregion

        #region Localization       
        private void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>
            {
                //DO NOT EDIT LANGUAGE FILES HERE! Navigate to oxide\lang\WeaponDamageScaler.en.json
                {"noPerms", "You do not have permission to use this command!"},
                {"invalidSyntax", "Invalid Syntax, usage example: setscale <weaponname> <x.x>"},
                {"itemNotFound",   "Item: \"" + "{item}" + "\" does not exist, syntax example: setscale <weaponname> <x.x>" },
                {"invalidSyntaxBodyPart", "<color=orange>/scalebp weapon <shortname> <bone> <amount></color> - Scale damage done for <shortname> to <bone>"},
                {"bodyPartExample", "<color=orange>-- ex. /scalebp weapon rifle.ak pelvis 1.25</color> - Damage done from a assault rifle to a pelvis is set to 125%"},
                {"scaleList", "<color=orange>/scalebp list</color> - Displays all bones"},
                {"shortnameNotFound", "Could not find a weapon with the shortname: <color=orange>{0}</color>"},
                {"bonePartNotFound", "Could not find a bone called: <color=orange>{0}</color>. Check /scalebp list"},
                {"bodyPartExample2", "<color=orange>/scalebp weapon <shortname> <bone> <amount></color>"},
                {"successfullyChangedValueBP","You have changed <color=orange>{0}'s</color> damage against <color=orange>{1}</color> to <color=orange>{2}</color>x damage" },
                {"alreadySameValue", "This is already the value for the selected item!"},
                {"scaledItem", "Scaled \"" + "{engName}" + "\" (" + "{shortName}" + ") " + "to: " + "{scaledValue}"}
            };
            lang.RegisterMessages(messages, this);
        }
        private string GetMessage(string key, string steamId = null) => lang.GetMessage(key, this, steamId);
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            RegisterPerm("weapondamagescaler.setscale");
            RegisterPerm("weapondamagescaler.setscalebp");

            LoadDefaultMessages();
                        
            wData = Interface.Oxide.DataFileSystem.GetFile("damagescaler_data");

            LoadVariables();
            LoadData();
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity == null) return;
            if (entity.ShortPrefabName.Contains("explosive.timed"))
            {
                if (!weaponData.Weapons.ContainsKey("explosive.timed")) return;
                if (weaponData.Weapons["explosive.timed"].GlobalModifier != 1.0f)
                {
                    TimedExplosive c4 = entity?.GetComponent<TimedExplosive>() ?? null;
                    if (c4 != null)
                        for (int i = 0; i < c4.damageTypes.Count; i++) c4.damageTypes[i].amount += weaponData.Weapons["explosive.timed"].GlobalModifier;
                }
            }
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            var explosive = entity?.GetComponent<TimedExplosive>() ?? null;
            if (!explosive) return;

            var damageMod = 1.0f;
            if (entity.ShortPrefabName.Contains("rocket_basic") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.basic")) damageMod = weaponData.AmmoTypes["ammo.rocket.basic"];
            if (entity.ShortPrefabName.Contains("rocket_hv") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.hv")) damageMod = weaponData.AmmoTypes["ammo.rocket.hv"];
            if (entity.ShortPrefabName.Contains("rocket_fire") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.fire")) damageMod = weaponData.AmmoTypes["ammo.rocket.fire"];
            if (damageMod != 1.0f)
                for (int i = 0; i < explosive.damageTypes.Count; i++) explosive.damageTypes[i].amount += damageMod;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo?.Initiator == null) return;
            var attacker = hitInfo?.Initiator?.GetComponent<BasePlayer>() ?? null;
            var victim = entity?.GetComponent<BasePlayer>() ?? null;
            if (attacker != null)
            {
                if ((bool)Config["UseGlobalDamageScaler"])
                {
                    hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageScaler"].ToString()));
                    return;
                }
                if ((bool)Config["PlayersOnly"] && victim != null) ScaleDealtDamage(hitInfo);
                else ScaleDealtDamage(hitInfo);
            }
        }
        private void ScaleDealtDamage(HitInfo hitInfo)
        {
            string bodypart = StringPool.Get(hitInfo.HitBone) ?? string.Empty;

            var ammoMod = 1.0f;
            var heldWeapon = hitInfo?.Weapon?.GetItem()?.GetHeldEntity()?.GetComponent<BaseProjectile>() ?? null;
            var ammoName = heldWeapon?.primaryMagazine?.ammoType?.shortname ?? string.Empty;
            if (weaponData.Weapons.ContainsKey(ammoName)) ammoMod = weaponData.Weapons[ammoName].GlobalModifier;
            string weapon = hitInfo?.Weapon?.GetItem()?.info?.shortname ?? string.Empty;
            if (string.IsNullOrEmpty(weapon)) return;


            if (InList(weapon, bodypart))
            {
                var globalMod = weaponData.Weapons[weapon].GlobalModifier;
                var individualMod = weaponData.Weapons[weapon].IndividualParts[bodypart];
                var totalMod = (globalMod + individualMod + ammoMod) - 2;
                if (totalMod != 1.0f)
                    hitInfo.damageTypes.ScaleAll(totalMod);
            }
            else if (ammoMod != 1.0f) hitInfo.damageTypes.ScaleAll(ammoMod);
        }
        #endregion

        #region Functions
        private void RegisterPerm(string perm) => permission.RegisterPermission(perm, this);
        private bool CanExecute(BasePlayer player, string perm)
        {
            if ((bool)Config["AllowAuthLevel"] && player.net.connection.authLevel >= (int)Config["AuthLevel"]) return true;

            if (permission.UserHasPermission(player.UserIDString, "weapondamagescaler." + perm)) return true;

            SendReply(player, GetMessage("noPerms", player.userID.ToString()));
            return false;
        }
        private bool InList(string weapon, string bodypart)
        {
            bool changed = false;
            if (!weaponData.Weapons.ContainsKey(weapon))
            {
                weaponData.Weapons.Add(weapon, new ItemStructure { Name = ItemManager.FindItemDefinition(weapon).displayName.english, GlobalModifier = 1.0f, IndividualParts = CreateBodypartList() });
                changed = true;
            }
            if (!weaponData.Weapons[weapon].IndividualParts.ContainsKey(bodypart))
            {
                foreach (var entry in weaponData.Weapons)
                    if (!weaponData.Weapons[entry.Key].IndividualParts.ContainsKey(bodypart))
                        weaponData.Weapons[entry.Key].IndividualParts.Add(bodypart, 1.0f);
                changed = true;
            }
            if (changed) SaveData();
            return true;
        }
        #endregion

        #region Chat and Console Commands
        [ConsoleCommand("weapon.setscale")]
        private void ConsoleSetScale(ConsoleSystem.Arg arg)
        {
            if (arg?.Player() != null) if (!CanExecute(arg.Player(), "setscale")) return;
            var args = arg?.Args ?? null;
            if (args == null || args.Length <= 1)
            {
                SendReply(arg, GetMessage("invalidSyntax"));
                return;
            }
            

            var engName = args[0].ToLower();
            var shortName = string.Empty;

            foreach (var entry in weaponData.Weapons)
            {
                if (entry.Value.Name.ToLower() == engName)
                {
                    shortName = entry.Key;
                    break;
                }
                else if (entry.Key.ToLower() == engName)
                {
                    shortName = entry.Key;
                    break;
                }
            }
            if (string.IsNullOrEmpty(shortName))
            {
                SendReply(arg, GetMessage("itemNotFound").Replace("{item}", engName));
                return;
            }
            
            var value = 0f;
            if (!float.TryParse(args[1], out value))
            {
                SendReply(arg, GetMessage("invalidSyntax"));
                return;
            }

            if (shortName != null && value != 0)
            {
                if (weaponData.Weapons[shortName].GlobalModifier == value)
                {
                    SendReply(arg, GetMessage("alreadySameValue"));
                    return;
                }

                weaponData.Weapons[shortName].GlobalModifier = value;
                SaveData();

                var sb = new StringBuilder(GetMessage("scaledItem"));
                var finalstring =
                    sb
                        .Replace("{engName}", engName)
                        .Replace("{shortName}", shortName)
                        .Replace("{scaledValue}", value.ToString())
                        .ToString();

                SendReply(arg, finalstring);
            }
            else
            {
                SendReply(arg, GetMessage("invalidSyntax"));
            }
        }

        [ChatCommand("setscale")]
        private void CmdSetScale(BasePlayer player, string command, string[] args)
        {
            if (!CanExecute(player, "setscale")) return;
            if (args.Length <= 1)
            {
                SendReply(player, GetMessage("invalidSyntax"));
                return;
            }

            var engName = args[0].ToLower();
            var shortName = string.Empty;

            foreach (var entry in weaponData.Weapons)
            {
                if (entry.Value.Name.ToLower() == engName)
                {
                    shortName = entry.Key;
                    break;
                }
                else if (entry.Key.ToLower() == engName)
                {
                    shortName = entry.Key;
                    break;
                }
            }
            if (string.IsNullOrEmpty(shortName))
            {
                SendReply(player, GetMessage("itemNotFound").Replace("{item}", engName));
                return;
            }

            var value = 0f;
            if (!float.TryParse(args[1], out value))
            {
                SendReply(player, GetMessage("invalidSyntax"));
                return;
            }

            if (shortName != null && value != 0)
            {
                if (weaponData.Weapons[shortName].GlobalModifier == value)
                {
                    SendReply(player, GetMessage("alreadySameValue"));
                    return;
                }

                weaponData.Weapons[shortName].GlobalModifier = value;
                SaveData();


                var sb = new StringBuilder(GetMessage("scaledItem"));

                var finalstring =
                    sb
                        .Replace("{engName}", engName)
                        .Replace("{shortName}", shortName)
                        .Replace("{scaledValue}", value.ToString())
                        .ToString();

                SendReply(player, finalstring);
            }
            else
            {
                SendReply(player, GetMessage("invalidSyntax"));
            }
        }

        [ChatCommand("scalebp")]
        private void cmdScaleBodyPart(BasePlayer player, string command, string[] args)
        {
            if (!CanExecute(player, "setscalebp")) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, lang.GetMessage("invalidSyntaxBodyPart", this, player.UserIDString));
                SendReply(player, lang.GetMessage("bodyPartExample", this, player.UserIDString));
                SendReply(player, lang.GetMessage("scaleList", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "weapon":
                    if (args.Length >= 3)
                    {
                        var shortName = string.Empty;
                        var engName = args[1].ToLower();

                        foreach (var entry in weaponData.Weapons)
                        {
                            if (entry.Value.Name.ToLower().Contains(engName))
                                shortName = entry.Key;
                            else if (entry.Key.Contains(engName))
                            {
                                shortName = entry.Key;
                                engName = entry.Value.Name.ToLower();
                            }
                        }
                        if (!string.IsNullOrEmpty(shortName))
                        {
                            if (weaponData.Weapons.ContainsKey(shortName))
                            {
                                if (weaponData.Weapons[shortName].IndividualParts.ContainsKey(args[2].ToLower()))
                                {
                                    float i = 0;
                                    if (args.Length == 4)
                                        if (!float.TryParse(args[3], out i)) i = 1.0f;
                                    weaponData.Weapons[shortName].IndividualParts[args[2].ToLower()] = i;
                                    SaveData();
                                    SendReply(player, string.Format(lang.GetMessage("successfullyChangedValueBP", this, player.UserIDString), shortName, args[2], i));
                                    return;
                                }
                                SendReply(player, string.Format(lang.GetMessage("bonePartNotFound", this, player.UserIDString), args[2].ToLower()));
                                return;
                            }
                            SendReply(player, string.Format(lang.GetMessage("shortnameNotFound", this, player.UserIDString), args[1].ToLower()));
                            return;
                        }
                    }
                    SendReply(player, lang.GetMessage("bodyPartExample2", this, player.UserIDString));
                    return;

                case "list":
                    for (int i = 0; i < Bodyparts.Length; i += 3)
                        SendReply(player, Bodyparts[i] + ", " + Bodyparts[i + 1] + ", " + Bodyparts[i + 2]);
                    return;
            }

        }
        #endregion
    }
}