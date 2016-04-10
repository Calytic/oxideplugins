
using System;
using System.Collections.Generic;

using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Infinite Ammo", "Mughisi", "1.2.3", ResourceId = 1083)]
    class InfiniteAmmo : RustPlugin
    {

        #region "Configuration data"

        // Do not modify these values, for modifications to the configuration 
        // file you should modify 'InfiniteAmmo.json' in your server's config folder.
        // <drive>:\...\server\<server identity>\oxide\config\

        private bool configChanged;

        // Plugin settings
        private const string DefaultChatPrefix = "Infinite Ammo";
        private const string DefaultChatPrefixColor = "#008800";

        public string ChatPrefix { get; private set; }
        public string ChatPrefixColor { get; private set; }

        // Plugin messages
        private const string DefaultNotAllowed = "You are not allowed to use this command.";
        private const string DefaultEnabled = "You now have infinite ammo!";
        private const string DefaultDisabled = "You no longer have infinite ammo!";
        private const string DefaultHelpText = "Toggle infinite ammo with /toggleammo.\nInfinite ammo is currently {0} for you.";

        public string NotAllowed { get; private set; }
        public string Enabled { get; private set; }
        public string Disabled { get; private set; }
        public string HelpText { get; private set; }

        #endregion

        #region "StoredData"

        class StoredData
        {
            public HashSet<InfiniteAmmoPlayer> Players = new HashSet<InfiniteAmmoPlayer>();

            public StoredData()
            {
            }
        }

        class InfiniteAmmoPlayer
        {
            public string UserId;

            public InfiniteAmmoPlayer()
            {
            }

            public InfiniteAmmoPlayer(BasePlayer player)
            {
                UserId = player.userID.ToString();
            }
            public ulong GetUserId()
            {
                ulong user_id;
                return !ulong.TryParse(UserId, out user_id) ? 0 : user_id;
            }
        }

        private StoredData storedData;

        private Hash<ulong, InfiniteAmmoPlayer> players = new Hash<ulong, InfiniteAmmoPlayer>();
         
        #endregion

        private void Loaded()
        {
            if (!permission.PermissionExists("infinite.ammo")) permission.RegisterPermission("infinite.ammo", this);
            
            LoadConfigData();
            LoadSavedData();
        }

        protected override void LoadDefaultConfig() => Puts("New configuration file created.");

        private void Unload() => SaveData();

        private void OnServerSave() => SaveData();

        [ChatCommand("ToggleAmmo")]
        private void ToggleAmmo(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "infinite.ammo"))
            {
                SendMessage(player, NotAllowed);
                return;
            }
            
            if (!HasInfiniteAmmo(player))
            {
                var info = new InfiniteAmmoPlayer(player);
                storedData.Players.Add(info);
                players[player.userID] = info;
                SendMessage(player, Enabled);
            }
            else
            {
                storedData.Players.RemoveWhere(info => info.GetUserId() == player.userID);
                players.Remove(player.userID);
                SendMessage(player, Disabled);
            }
        }

        private void SendHelpText(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "infinite.ammo")) return;
            SendMessage(player, HelpText, (HasInfiniteAmmo(player) ? "enabled" : "disabled"));
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (!HasInfiniteAmmo(player)) return;
            projectile.GetItem().condition = projectile.GetItem().info.condition.max;
            if (projectile.primaryMagazine.contents > 0) return;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }   

        private void OnRocketLaunched(BasePlayer player)
        {
            if (!HasInfiniteAmmo(player)) return;
            var weapon = player.GetActiveItem().GetHeldEntity() as BaseProjectile;
            if (weapon == null) return;
            player.GetActiveItem().condition = player.GetActiveItem().info.condition.max;
            if (weapon.primaryMagazine.contents > 0) return;
            weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
            weapon.SendNetworkUpdateImmediate();
        }

        private void OnWeaponThrown(BasePlayer player)
        {
            if (!HasInfiniteAmmo(player)) return;
            var weapon = player.GetActiveItem().GetHeldEntity() as ThrownWeapon;
            if (weapon == null) return;
            weapon.GetItem().amount += 1;
        }

        void SendMessage(BasePlayer player, string message, string args = null) => PrintToChat(player, $"<color={ChatPrefixColor}>{ChatPrefix}</color>: {message}", args);

        private void LoadConfigData()
        {
            // Plugin settings
            ChatPrefix = GetConfigValue("Settings", "ChatPrefix", DefaultChatPrefix);
            ChatPrefixColor = GetConfigValue("Settings", "ChatPrefixColor", DefaultChatPrefixColor);

            // Plugin messages
            NotAllowed = GetConfigValue("Messages", "NotAllowed", DefaultNotAllowed);
            Enabled = GetConfigValue("Messages", "Enabled", DefaultEnabled);
            Disabled = GetConfigValue("Messages", "Disabled", DefaultDisabled);
            HelpText = GetConfigValue("Messages", "Help", DefaultHelpText);

            if (!configChanged) return;
            PrintWarning("The configuration file was updated!");
            SaveConfig();
        }

        private void LoadSavedData()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>("InfiniteAmmo");
            foreach (var player in storedData.Players)
                players[player.GetUserId()] = player;
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

        void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("InfiniteAmmo", storedData);

        private bool HasInfiniteAmmo(BasePlayer player) => players[player.userID] != null;
    }
}