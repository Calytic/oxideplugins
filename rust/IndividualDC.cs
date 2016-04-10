using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("IndividualDC", "k1lly0u", "0.1.3", ResourceId = 1758)]
    [Description("Damage controller for individual bones and weapons")]
    class IndividualDC : RustPlugin
    {   
        private ConfigData configData;
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
        class ConfigData
        {
            public Dictionary<string, Dictionary<string, float>> Weapons { get; set; }            
        }
        void OnServerInitialized()
        {
            lang.RegisterMessages(messages, this);
            LoadVariables();
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            var config = new ConfigData{ Weapons = SetConfigData() };
            Config.WriteObject(config, true);
        }        
        private Dictionary<string, Dictionary<string, float>> SetConfigData()
        {
            Dictionary<string, Dictionary<string, float>> weapons = new Dictionary<string, Dictionary<string, float>>();
            foreach (ItemDefinition definition in ItemManager.itemList)
            {
                if (definition != null)
                    if (definition.category.ToString() == "Weapon")                        
                        if (!definition.shortname.Contains("mod"))
                        {
                            weapons.Add(definition.shortname, new Dictionary<string, float>());
                            foreach (var entry in Bodyparts)
                                weapons[definition.shortname].Add(entry, 1.0f);
                        }                    
            }
            return weapons;
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            try
            {
                if (entity is BasePlayer && hitInfo.Initiator is BasePlayer)
                {
                    if (entity as BasePlayer == null || hitInfo == null) return;

                    string bodypart = StringPool.Get(hitInfo.HitBone);
                    if (bodypart == null || bodypart == "") return;

                    string weapon = hitInfo.Weapon.GetItem().info.shortname;
                    if (weapon == null || weapon == "") return;

                    if (InList(weapon, bodypart))
                    {
                        float modifier = configData.Weapons[weapon][bodypart];
                        hitInfo.damageTypes.ScaleAll(modifier);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
            }
        } 
        private bool InList(string weapon, string bodypart)
        {
            bool changed = false;
            if (!configData.Weapons.ContainsKey(weapon))
            {
                configData.Weapons.Add(weapon, new Dictionary<string, float>());
                foreach (var entry in Bodyparts)
                    if (!configData.Weapons[weapon].ContainsKey(entry))
                        configData.Weapons[weapon].Add(entry, 1.0f);
                changed = true;
            }
            if (!configData.Weapons[weapon].ContainsKey(bodypart))
            {
                foreach (var entry in configData.Weapons)
                    if (!configData.Weapons[entry.Key].ContainsKey(bodypart))
                        configData.Weapons[entry.Key].Add(bodypart, 1.0f);
                changed = true;
            }
            if (changed) Config.WriteObject(configData, true);
            return true;
        }
        [ChatCommand("scale")]
        private void cmdScale(BasePlayer player, string command, string[] args)
        {
            if (!isAuth(player)) return;
            if (args == null || args.Length == 0)
            {
                SendReply(player, lang.GetMessage("1",this, player.UserIDString));
                SendReply(player, lang.GetMessage("2", this, player.UserIDString));
                SendReply(player, lang.GetMessage("3", this, player.UserIDString));
                return;
            }
            switch (args[0].ToLower())
            {
                case "weapon":
                    if (args.Length >= 3)
                    {
                        if (configData.Weapons.ContainsKey(args[1].ToLower()))
                        {
                            if (configData.Weapons[args[1].ToLower()].ContainsKey(args[2].ToLower()))
                            {
                                float i = 1.0f;
                                if (args.Length == 4)
                                    if (!float.TryParse(args[3], out i)) i = 1.0f;
                                configData.Weapons[args[1].ToLower()][args[2].ToLower()] = i;
                                Config.WriteObject(configData, true);
                                SendReply(player, string.Format(lang.GetMessage("7", this, player.UserIDString), args[1], args[2], i));
                                return;
                            }
                            SendReply(player, string.Format(lang.GetMessage("4", this, player.UserIDString), args[2].ToLower()));
                            return;
                        }
                        SendReply(player, string.Format(lang.GetMessage("5", this, player.UserIDString), args[1].ToLower()));
                        return;
                    }
                    SendReply(player, lang.GetMessage("6", this, player.UserIDString));
                    return;
                case "list":
                    for (int i = 0; i < Bodyparts.Length; i += 3)
                        SendReply(player, Bodyparts[i] + ", " + Bodyparts[i + 1] + ", " + Bodyparts[i + 2]);
                    return;
            }
            
        }
        private bool isAuth(BasePlayer player)
        {
            if (player.net.connection.authLevel >= 1) return true;
            return false;
        }

        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"1", "<color=orange>/scale weapon <shortname> <bone> <amount></color> - Scale damage done for <shortname> to <bone>"},
            {"2", "<color=orange>-- ex. /scale weapon rifle.ak pelvis 1.25</color> - Damage done from a assault rifle to a pelvis is set to 125%"},
            {"3", "<color=orange>/scale list</color> - Displays all bones"},
            {"4", "Could not find a weapon with the shortname: <color=orange>{0}</color>"},
            {"5", "Could not find a bone called: <color=orange>{0}</color>. Check /scale list"},
            {"6", "<color=orange>/scale weapon <shortname> <bone> <amount></color>"},
            {"7","You have changed <color=orange>{0}'s</color> damage against <color=orange>{1}</color> to <color=orange>{2}</color>x damage" }
        };
    }
}
