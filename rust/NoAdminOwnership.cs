using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NoAdminOwnership", "k1lly0u", "0.1.3", ResourceId = 2019)]
    class NoAdminOwnership : RustPlugin
    {
        #region Fields
        private List<ulong> itemOwnerDebug = new List<ulong>();
        private bool Initialized = false;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("noadminownership.ignore", this);
            lang.RegisterMessages(messages, this);
        }
        void OnServerInitialized()
        {
            LoadVariables();
            Initialized = true;
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (!Initialized) return;
            var player = container.playerOwner;
            if (player == null) return;
            if (ignoreUser(player))
            {
                RemoveOwner(player.userID, item);
                if (isDebug(player))
                    PrintOwnership(player, item);
            }
        }
        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (!Initialized) return;
            var player = container.playerOwner;            
            if (player == null) return;
            if (ignoreUser(player))
            {
                RemoveOwner(player.userID, item);
                if (isDebug(player))
                    PrintOwnership(player, item);
            }
        }
       
        void RemoveOwner(ulong ID, Item item)
        {
            if (item.owners == null || item.owners.Count == 0) return;            
            else
            {
                foreach (var owner in item.owners)
                {
                    if (owner.userid == ID)
                    {
                        item.owners.Remove(owner);
                        return;
                    }
                }
            }
        }
        public void PrintOwnership(BasePlayer player, Item item)
        {
            SendReply(player, (string.Concat(MSG("owners", player.UserIDString), item.info.shortname)));
            float single = 0f;
            for (int i = 0; i < item.owners.Count; i++)
            {
                Item.OwnerFraction items = item.owners[i];
                SendReply(player, (string.Concat(items.player.displayName ?? items.userid.ToString(), ":", items.fraction)));
                single = single + items.fraction;
            }
            SendReply(player, (string.Concat(MSG("fracts", player.UserIDString), single)));
        }
        #endregion

        #region Functions
        private bool isAllowed(BasePlayer player)
        {
            var auth = player?.net?.connection?.authLevel ?? 0;
            if (auth >= 0) return true;            
            if (permission.UserHasPermission(player.UserIDString, "noadminownership.ignore")) return true;
            return false;
        }
        private bool ignoreUser(BasePlayer player)
        {
            var auth = player?.net?.connection?.authLevel ?? 0;
            if (configData.IgnoreAuth2 && auth == 2) return true;
            if (configData.IgnoreAuth1 && auth == 1) return true;
            if (configData.IgnoreUsingPermission && permission.UserHasPermission(player.UserIDString, "noadminownership.ignore")) return true;
            return false;
        }
        private bool isDebug(BasePlayer player) => itemOwnerDebug.Contains(player.userID);
        #endregion

        #region Chat Command
        [ChatCommand("ownership")]
        private void cmdOwnership(BasePlayer player, string command, string[] args)
        {
            if (isAllowed(player))
            {
                if (!itemOwnerDebug.Contains(player.userID))
                {
                    itemOwnerDebug.Add(player.userID);
                    SendReply(player, MSG("debugActive", player.UserIDString));
                }
                else
                {
                    itemOwnerDebug.Remove(player.userID);
                    SendReply(player, MSG("debugDeactive", player.UserIDString));
                }
            }
        }
        #endregion

        #region Messages
        private string MSG(string key, string ID = null) => lang.GetMessage(key, this, ID);
        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"debugActive", "Ownership debug activated" },
            {"debugDeactive", "Ownership debug de-activated" },
            {"owners", "Owners for :" },
            {"fracts", " Total fraction: " }
        };
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            public bool IgnoreAuth2 { get; set; }
            public bool IgnoreAuth1 { get; set; }
            public bool IgnoreUsingPermission { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                IgnoreAuth1 = true,
                IgnoreAuth2 = true,
                IgnoreUsingPermission = false,
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion       
    }
}
