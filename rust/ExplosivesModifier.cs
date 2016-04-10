using System;
using System.Collections.Generic;
using Rust;

namespace Oxide.Plugins
{

    [Info("Explosives Modifier", "Mughisi", 1.3, ResourceId = 832)]
    class ExplosivesModifier : RustPlugin
    {

        #region Configuration Data

        private bool configChanged;

        // Plugin options
        private const string DefaultChatPrefix = "Bomb Squad";
        private const string DefaultChatPrefixColor = "#008000";
        
        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; private set; }

        // Explosive Charge options
        private const float DefaultExplosiveChargeDamageModifier = 100;
        private const float DefaultExplosiveChargeRadiusModifier = 100;

        public float ExplosiveChargeDamageModifier { get; private set; }
        public float ExplosiveChargeRadiusModifier { get; private set; }

        // F1 Grenade options
        private const float DefaultF1GrenadeDamageModifier = 100;
        private const float DefaultF1GrenadeRadiusModifier = 100;
        private const bool DefaultF1GrenadeSticky = false;

        public float F1GrenadeDamageModifier { get; private set; }
        public float F1GrenadeRadiusModifier { get; private set; }
        public bool F1GrenadeSticky { get; private set; }

        // Beancan Grenade options
        private const float DefaultBeancanGrenadeDamageModifier = 100;
        private const float DefaultBeancanGrenadeRadiusModifier = 100;
        private const bool DefaultBeancanGrenadeSticky = false;

        public float BeancanGrenadeDamageModifier { get; private set; }
        public float BeancanGrenadeRadiusModifier { get; private set; }
        public bool BeancanGrenadeSticky { get; private set; }

        // Rocket options
        private const float DefaultRocketDamageModifier = 100;
        private const float DefaultRocketRadiusModifier = 100;

        public float RocketDamageModifier { get; private set; }
        public float RocketRadiusModifier { get; private set; }

        // Explosive Ammo options
        private const float DefaultExplosiveAmmoDamageModifier = 100;

        public float ExplosiveAmmoDamageModifier { get; private set; }

        // Plugin messages
        private const string DefaultHelpTextPlayersExplosiveCharge =
            "Explosive charges deal {0}% of their normal damage and their radius is set to {1}%";
        private const string DefaultHelpTextPlayersF1Grenade =
            "F1 Grenades deal {0}% of their normal damage and their radius is set to {1}%. Sticky F1 Grenades are {2}";
        private const string DefaultHelpTextPlayersBeancanGrenade =
            "Beancan Grenades deal {0}% of their normal damage and their radius is set to {1}%. Sticky Beancan Grenades are {2}";
        private const string DefaultHelpTextPlayersRocket =
            "Rockets deal {0}% of their normal damage and their radius is set to {1}%.";
        private const string DefaultHelpTextPlayersExplosiveAmmo = "Explosive rounds deals {0}% of their normal damage.";
        private const string DefaultHelpTextAdmins =
            "You can modify the damage of the following types of explosives: \r\n" +
            "  Explosive Charges, F1 Grenades, Beancan Grenades, Rockets and Explosive Ammo. \r\n" +
            "  /explosivedamage <type:timed|f1|beancan|rocket|ammo> <value:percentage> \r\n" +
            "  Example: /explosivedamage timed 50 - For 50% of normal damage. \r\n" +
            "           /explosivedamage ammo 200 - For 200% of normal damage. \r\n \r\n" +
            "You can modify the radius of the following types of explosives: \r\n" +
            "  Explosive Charges, F1 Grenades, Beancan Grenades and Rockets. \r\n" +
            "  /explosiveradius <type:timed|f1|beancan|rocket> <value:percentage> \r\n" +
            "  Example: /explosiveradius timed 50 - For 50% of normal damage. \r\n" +
            "           /explosiveradius rocket 200 - For 200% of normal damage. \r\n \r\n" +
            "You can also toggle sticky grenades for F1 Grenades and Beancan grenades: \r\n" +
            "  /stickygrenades <type:f1|beancan>";
        private const string DefaultModified = "{0} {1} changed to {2}% of the normal {1}.";
        private const string DefaultSticky = "You have {0} sticky {1}s";
        private const string DefaultNotAllowed = "You are not allowed to use this command.";
        private const string DefaultInvalidArguments =
            "Invalid argument(s) supplied! Check /help for more information on the commands.";

        public string HelpTextPlayersExplosiveCharge { get; private set; }
        public string HelpTextPlayersF1Grenade { get; private set; }
        public string HelpTextPlayersBeancanGrenade { get; private set; }
        public string HelpTextPlayersRocket { get; private set; }
        public string HelpTextPlayersExplosiveAmmo { get; private set; }
        public string HelpTextAdmins { get; private set; }
        public string Modified { get; private set; }
        public string Sticky { get; private set; }
        public string NotAllowed { get; private set; }
        public string InvalidArguments { get; private set; }

        #endregion

        private readonly string[] weaponDamageTypes = { "timed", "f1", "beancan", "rocket", "ammo" };

        private readonly string[] weaponRadiusTypes = { "timed", "f1", "beancan", "rocket" };

        protected override void LoadDefaultConfig() => PrintWarning("New configuration file created.");
        
        private void Loaded() => LoadConfiguration();

        private void LoadConfiguration()
        {
            // Plugin options
            ChatPrefix = GetConfigValue("Settings", "Chat Prefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "Chat Prefix Color", DefaultChatPrefixColor);

            // Explosive Charge options
            ExplosiveChargeDamageModifier = GetConfigValue("Options - Explosive Charge", "Damage Modifier",
                DefaultExplosiveChargeDamageModifier);
            ExplosiveChargeRadiusModifier = GetConfigValue("Options - Explosive Charge", "Radius Modifier",
                DefaultExplosiveChargeRadiusModifier);

            // F1 Grenade options
            F1GrenadeDamageModifier = GetConfigValue("Options - F1 Grenade", "Damage Modifier",
                DefaultF1GrenadeDamageModifier);
            F1GrenadeRadiusModifier = GetConfigValue("Options - F1 Grenade", "Radius Modifier",
                DefaultF1GrenadeRadiusModifier);
            F1GrenadeSticky = GetConfigValue("Options - F1 Grenade", "Sticky", DefaultF1GrenadeSticky);

            // Beancan Grenade options
            BeancanGrenadeDamageModifier = GetConfigValue("Options - Beancan Grenade", "Damage Modifier",
                DefaultBeancanGrenadeDamageModifier);
            BeancanGrenadeRadiusModifier = GetConfigValue("Options - Beancan Grenade", "Radius Modifier",
                DefaultBeancanGrenadeRadiusModifier);
            BeancanGrenadeSticky = GetConfigValue("Options - Beancan Grenade", "Sticky", DefaultBeancanGrenadeSticky);

            // Rocket options
            RocketDamageModifier = GetConfigValue("Options - Rocket", "Damage Modifier",
                DefaultRocketDamageModifier);
            RocketRadiusModifier = GetConfigValue("Options - Rocket", "Radius Modifier",
                DefaultRocketRadiusModifier);

            // Explosive Ammo options
            ExplosiveAmmoDamageModifier = GetConfigValue("Options - Explosive Ammo", "Damage Modifier",
                DefaultExplosiveAmmoDamageModifier);

            // Plugin messages
            HelpTextPlayersExplosiveCharge = GetConfigValue("HelpText", "Player - Explosive Charge",
                DefaultHelpTextPlayersExplosiveCharge);
            HelpTextPlayersF1Grenade = GetConfigValue("HelpText", "Player - F1 Grenade",
                     DefaultHelpTextPlayersF1Grenade);
            HelpTextPlayersBeancanGrenade = GetConfigValue("HelpText", "Player - Beancan Grenade",
                     DefaultHelpTextPlayersBeancanGrenade);
            HelpTextPlayersRocket = GetConfigValue("HelpText", "Player - Rocket",
                     DefaultHelpTextPlayersRocket);
            HelpTextPlayersExplosiveAmmo = GetConfigValue("HelpText", "Player - Explosive Ammo",
                     DefaultHelpTextPlayersExplosiveAmmo);
            HelpTextAdmins = GetConfigValue("HelpText", "Admin", DefaultHelpTextAdmins);
            Modified = GetConfigValue("Messages", "Modified", DefaultModified);
            Sticky = GetConfigValue("Messages", "Sticky", DefaultSticky);
            NotAllowed = GetConfigValue("Messages", "Not Allowed", DefaultNotAllowed);
            InvalidArguments = GetConfigValue("Messages", "Invalid Argument(s)", DefaultInvalidArguments);

            if (!configChanged) return;
            Puts("Configuration file updated.");
            SaveConfig();
        }

        [ChatCommand("explosivedamage")]
        void ExplosiveDamage(BasePlayer player, string command, string[] args)
        {
            ChangeExplosive(player, command, args, "Damage Modifier");
        }

        [ChatCommand("explosiveradius")]
        void ExplosiveRadius(BasePlayer player, string command, string[] args)
        {
            ChangeExplosive(player, command, args, "Radius Modifier");
        }

        [ChatCommand("stickygrenades")]
        void StickyGrenades(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player)) return;

            switch (args[0].ToLower())
            {
                case "f1":
                    F1GrenadeSticky = !F1GrenadeSticky;
                    SetConfigValue("Options - F1 Grenade", "Sticky", F1GrenadeSticky);
                    SendChatMessage(player, Sticky, (F1GrenadeSticky ? "enabled" : "disabled"), "F1 Grenade");
                    return;
                case "beancan":
                    BeancanGrenadeSticky = !BeancanGrenadeSticky;
                    SetConfigValue("Options - F1 Grenade", "Sticky", BeancanGrenadeSticky);
                    SendChatMessage(player, Sticky, (BeancanGrenadeSticky ? "enabled" : "disabled"), "Beancan Grenade");
                    return;
            }

            SendChatMessage(player, InvalidArguments);
        }

        void ChangeExplosive(BasePlayer player, string command, string[] args, string type)
        {
            if (!IsAllowed(player)) return;

            if (args.Length != 2)
            {
                SendChatMessage(player, InvalidArguments, command);
                return;
            }

            var invalid = false;
            float newModifier;
            if (!float.TryParse(args[1], out newModifier))
                invalid = true;
            
            if (!IsValidType(command, args[0]) || invalid)
            {
                SendChatMessage(player, InvalidArguments, command);
                return;
            }

            var configCategory = string.Empty;
            if (args[0].ToLower() == "timed")
            {
                configCategory = "Options - Explosive Charge";
                if (type == "Damage Modifier")
                    ExplosiveChargeDamageModifier = newModifier;
                if (type == "Radius Modifier")
                    ExplosiveChargeRadiusModifier = newModifier;
                SendChatMessage(player, Modified, "Explosive Charge", type.Replace(" Modifier", "").ToLower(), newModifier);
            }
            if (args[0].ToLower() == "f1")
            {
                configCategory = "Options - F1 Grenade";
                if (type == "Damage Modifier")
                    F1GrenadeDamageModifier = newModifier;
                if (type == "Radius Modifier")
                    F1GrenadeRadiusModifier = newModifier;
                SendChatMessage(player, Modified, "F1 Grenade", type.Replace(" Modifier", "").ToLower(), newModifier);
            }
            if (args[0].ToLower() == "beancan")
            {
                configCategory = "Options - Beancan Grenade";
                if (type == "Damage Modifier")
                    BeancanGrenadeDamageModifier = newModifier;
                if (type == "Radius Modifier")
                    BeancanGrenadeRadiusModifier = newModifier;
                SendChatMessage(player, Modified, "Beancan Grenade", type.Replace(" Modifier", "").ToLower(), newModifier);
            }
            if (args[0].ToLower() == "rocket")
            {
                configCategory = "Options - Rocket";
                if (type == "Damage Modifier")
                    RocketDamageModifier = newModifier;
                if (type == "Radius Modifier")
                    RocketRadiusModifier = newModifier;
                SendChatMessage(player, Modified, "Rocket", type.Replace(" Modifier", "").ToLower(), newModifier);
            }
            if (args[0].ToLower() == "ammo")
            {
                configCategory = "Options - Explosive Ammo";
                if (type == "Damage Modifier")
                    ExplosiveAmmoDamageModifier = newModifier;
                SendChatMessage(player, Modified, "Explosive Ammo", type.Replace(" Modifier", "").ToLower(), newModifier);
            }

            SetConfigValue(configCategory, type, newModifier);
        }

        #region Hooks

        void OnWeaponThrown(BasePlayer player, BaseEntity entity)
        {
            var explosive = entity as TimedExplosive;
            if (!explosive) return;
            var modifier = 100f;
            var radius = 100f;

            if (entity.name == "items/timed.explosive.deployed") { modifier = ExplosiveChargeDamageModifier; radius = ExplosiveChargeRadiusModifier; }
            if (entity.name == "items/grenade.f1.deployed") { modifier = F1GrenadeDamageModifier; radius = F1GrenadeRadiusModifier; }
            if (entity.name == "items/grenade.beancan.deployed") { modifier = BeancanGrenadeDamageModifier; radius = BeancanGrenadeRadiusModifier; }
            
            foreach (var damage in explosive.damageTypes)
                damage.amount *= modifier / 100;

            explosive.explosionRadius *= radius / 100;
        }
        
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            var explosive = entity as TimedExplosive;
            if (!explosive) return;

            foreach (var damage in explosive.damageTypes)
                damage.amount *= RocketDamageModifier / 100;

            explosive.explosionRadius *= RocketRadiusModifier / 100;
        }
        void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (info.Initiator?.ToPlayer() != null && info.damageTypes.Get(DamageType.Explosion) > 0)
                info.damageTypes.Scale(DamageType.Explosion, ExplosiveAmmoDamageModifier / 100);
        }


        void SendHelpText(BasePlayer player)
        {      
            SendChatMessage(player, HelpTextPlayersExplosiveCharge, ExplosiveAmmoDamageModifier, ExplosiveChargeRadiusModifier);
            SendChatMessage(player, HelpTextPlayersF1Grenade, F1GrenadeDamageModifier, F1GrenadeRadiusModifier, (F1GrenadeSticky ? "enabled" : "disabled"));
            SendChatMessage(player, HelpTextPlayersBeancanGrenade, BeancanGrenadeDamageModifier, BeancanGrenadeRadiusModifier, (BeancanGrenadeSticky ? "enabled" : "disabled"));
            SendChatMessage(player, HelpTextPlayersRocket, RocketDamageModifier, RocketRadiusModifier);
            SendChatMessage(player, HelpTextPlayersExplosiveAmmo, ExplosiveAmmoDamageModifier);

            if (player.net.connection.authLevel == 2)
                SendChatMessage(player, HelpTextAdmins);
        }

        #endregion

        #region Helper Methods
        
        private bool IsValidType(string command, string type)
        {
            switch (command)
            {
                case "explosivedamage":
                    return weaponDamageTypes.Contains(type.ToLower());
                case "explosiveradius":
                    return weaponRadiusTypes.Contains(type.ToLower());
            }
            return false;
        }

        void SendChatMessage(BasePlayer player, string message, params object[] arguments) =>
            PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}", arguments);

        bool IsAllowed(BasePlayer player)
        {
            if (player.net.connection.authLevel == 2) return true;
            SendChatMessage(player, NotAllowed);
            return false;
        }

        T GetConfigValue<T>(string category, string setting, T defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            configChanged = true;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        void SetConfigValue(string category, string setting, object newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;

            if (data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }

            SaveConfig();
        }

        #endregion

    }

}
