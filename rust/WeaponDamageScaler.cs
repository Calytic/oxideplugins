using System.Collections.Generic;
using System.Text;
using Oxide.Core.Configuration;
using Oxide.Core;

namespace Oxide.Plugins
{
    //Body part scaling from k1lly0u's plugin, with permission (thanks, k1lly0u)
    //Further code cleanup/improvement with hel pof k1lly0u
    [Info("Weapon Damage Scaler", "Shady", "1.0.5", ResourceId = 1594)]
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
            foreach (ItemDefinition definition in ItemManager.itemList)
            {
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
            foreach (var entry in Bodyparts)
                newData.Add(entry, 1.0f);
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
                Puts("Unable to load data, creating new datafile");
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
            if (entity.LookupShortPrefabName().Contains("explosive.timed"))
            {
                if (!weaponData.Weapons.ContainsKey("explosive.timed")) return;
                if (weaponData.Weapons["explosive.timed"].GlobalModifier != 1.0f)
                {
                    TimedExplosive c4 = entity?.GetComponent<TimedExplosive>() ?? null;
                    if (c4 != null)
                        foreach (var damage in c4.damageTypes)
                            damage.amount *= weaponData.Weapons["explosive.timed"].GlobalModifier;
                }
            }
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            var explosive = entity as TimedExplosive;
            if (!explosive) return;

            var damageMod = 1.0f;
            if (entity.LookupShortPrefabName().Contains("rocket_basic") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.basic")) damageMod = weaponData.AmmoTypes["ammo.rocket.basic"];
            if (entity.LookupShortPrefabName().Contains("rocket_hv") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.hv")) damageMod = weaponData.AmmoTypes["ammo.rocket.hv"];
            if (entity.LookupShortPrefabName().Contains("rocket_fire") && weaponData.AmmoTypes.ContainsKey("ammo.rocket.fire")) damageMod = weaponData.AmmoTypes["ammo.rocket.fire"];
            if (damageMod != 1.0f)
                foreach (var damage in explosive.damageTypes)
                    damage.amount *= damageMod;
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo?.Initiator == null) return;

            if (hitInfo?.Initiator is BasePlayer)
            {
                if ((bool)Config["UseGlobalDamageScaler"])
                {
                    hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageScaler"].ToString()));
                    return;
                }
                if ((bool)Config["PlayersOnly"])
                {
                    if (entity is BasePlayer)
                        if (entity as BasePlayer != null || hitInfo != null)
                            ScaleDealtDamage(hitInfo);
                    return;
                }
                else ScaleDealtDamage(hitInfo);
            }
        }
        private void ScaleDealtDamage(HitInfo hitInfo)
        {
            string bodypart = StringPool.Get(hitInfo.HitBone) ?? string.Empty;
            if (string.IsNullOrEmpty(bodypart)) return;

            float ammoMod = 1.0f;

            BaseProjectile heldWeapon = hitInfo?.Weapon?.GetItem()?.GetHeldEntity() as BaseProjectile ?? null;
            if (heldWeapon != null)
                if (weaponData.AmmoTypes.ContainsKey(heldWeapon.primaryMagazine?.ammoType?.shortname))
                    ammoMod = weaponData.AmmoTypes[heldWeapon.primaryMagazine?.ammoType?.shortname];

            string weapon = hitInfo?.Weapon?.GetItem()?.info?.shortname ?? string.Empty;
            if (string.IsNullOrEmpty(weapon)) return;

            if (InList(weapon, bodypart))
            {
                float globalMod = weaponData.Weapons[weapon].GlobalModifier;
                float individualMod = weaponData.Weapons[weapon].IndividualParts[bodypart];
                float totalMod = (globalMod + individualMod + ammoMod) / 3;
                if (totalMod != 1.0f)
                    hitInfo.damageTypes.ScaleAll(totalMod);
            }
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
            if (arg.connection != null)            
                if (!CanExecute(arg.connection.player as BasePlayer, "setscale")) return;            

            if (arg.Args == null || arg.Args.Length == 0 || arg.Args.Length == 1)
            {
                SendReply(arg, GetMessage("invalidSyntax"));
                return;
            }

            var engName = arg.Args[0].ToLower();
            var shortName = string.Empty;

            foreach (var entry in weaponData.Weapons) // Search for shortname or display name
            {
                if (entry.Value.Name.ToLower().Contains(engName))
                    shortName = entry.Key;
                else if (entry.Key.Contains(engName))
                {
                    shortName = entry.Key;
                    engName = entry.Value.Name.ToLower();
                }
            }            
            if (string.IsNullOrEmpty(shortName))
            {
                SendReply(arg, GetMessage("itemNotFound").Replace("{item}", engName));
                return;
            }
            
            float value = 0;
            float.TryParse(arg.Args[1], out value);

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
            if (args.Length == 0 || args.Length == 1)
            {
                SendReply(player, GetMessage("invalidSyntax"));
                return;
            }

            var engName = args[0].ToLower();
            var shortName = string.Empty;

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
            if (string.IsNullOrEmpty(shortName))
            {
                SendReply(player, GetMessage("itemNotFound").Replace("{item}", engName));
                return;
            }

            float value = 0;
            float.TryParse(args[1], out value);

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