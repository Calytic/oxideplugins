using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{ 
    [Info("PrivilegeDeploy", "k1lly0u", "0.1.23", ResourceId = 1800)]
    class PrivilegeDeploy : RustPlugin
    {
        private readonly int triggerMask = LayerMask.GetMask("Trigger", "Construction");
        private bool Loaded = false;

        private Dictionary<ulong, PendingItem> pendingItems = new Dictionary<ulong, PendingItem>();

        void OnServerInitialized() => LoadVariables();
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (Loaded)
            {
                for (int i = 0; i < configData.deployables.Count; i++)
                    if (entity.ShortPrefabName.Contains(configData.deployables[i]))
                    {
                        var ownerID = entity.GetComponent<BaseEntity>().OwnerID;
                        if (ownerID != 0)
                        {
                            BasePlayer player = BasePlayer.FindByID(ownerID);
                            if (player == null || player.IsAdmin()) return;
                            if (!HasPriv(player))
                            {
                                Item item;
                                if (entity.ShortPrefabName.Contains("landmine"))
                                {
                                    entity.KillMessage();                                    
                                    item = ItemManager.CreateByPartialName("trap.landmine");
                                }
                                else if (entity.ShortPrefabName.Contains("bear"))
                                {
                                    entity.GetComponent<BaseCombatEntity>().DieInstantly();
                                    item = ItemManager.CreateByPartialName("trap.bear");
                                }
                                else
                                {
                                    entity.GetComponent<BaseCombatEntity>().DieInstantly();
                                    item = ItemManager.CreateByPartialName(configData.deployables[i]);
                                    var deployable = item.info.GetComponent<ItemModDeployable>();
                                    if (deployable != null)
                                    {
                                        var oven = deployable.entityPrefab.Get()?.GetComponent<BaseOven>();
                                        if (oven != null)
                                            oven.startupContents = null;
                                    }
                                }

                                if (!pendingItems.ContainsKey(player.userID))
                                    pendingItems.Add(player.userID, new PendingItem());
                                pendingItems[player.userID].item = item;

                                CheckForDuplicate(player);
                            }
                        }
                    }
            }
        }      
        private void CheckForDuplicate(BasePlayer player)
        {
            if (pendingItems[player.userID].timer != null) pendingItems[player.userID].timer.Destroy();
               
            pendingItems[player.userID].timer = timer.Once(0.01f, () => GivePlayerItem(player));

        }
        private void GivePlayerItem(BasePlayer player)
        {
            Item item = pendingItems[player.userID].item;
            player.GiveItem(item);
            SendReply(player, lang.GetMessage("blocked", this, player.UserIDString));
            pendingItems.Remove(player.userID);
        }
        
        private bool HasPriv(BasePlayer player)
        {
            var hit = Physics.OverlapSphere(player.transform.position, 2f, triggerMask);
            foreach (var entity in hit)
            {
                BuildingPrivlidge privs = entity.GetComponentInParent<BuildingPrivlidge>();
                if (privs != null)
                    if (privs.IsAuthed(player)) return true;
            }
            return false;
        }

        #region config

        private ConfigData configData;
        class ConfigData
        {
            public List<string> deployables { get; set; }
        }
        private void LoadVariables()
        {
            Loaded = true;
            RegisterMessages();
            LoadConfigVariables();
            SaveConfig();
        }
        private void RegisterMessages() => lang.RegisterMessages(messages, this);
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                deployables = new List<string>
                    {
                        "barricade.concrete",
                        "barricade.metal",
                        "barricade.sandbags",
                        "barricade.stone",
                        "barricade.wood",
                        "barricade.woodwire",
                        "campfire",
                        "gates.external.high.stone",
                        "gates.external.high.wood",
                        "wall.external.high",
                        "wall.external.high.stone",
                        "landmine",
                        "beartrap"
                    }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        class PendingItem
        {
            public Timer timer;
            public Item item;
        }
        #endregion

        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"blocked", "You can not build this outside of a building privileged area!" }
        };
    }
}

