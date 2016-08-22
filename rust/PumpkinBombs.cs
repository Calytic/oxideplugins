using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PumpkinBombs", "k1lly0u", "0.1.1", ResourceId = 2070)]
    class PumpkinBombs : RustPlugin
    {
        #region Fields
        private const string Jack1 = "jackolantern.angry";
        private const string Jack2 = "jackolantern.happy";
        private Dictionary<string, ItemDefinition> ItemDefs;
        private List<ulong> craftedBombs;
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            permission.RegisterPermission("pumpkinbombs.use", this);
            permission.RegisterPermission("pumpkinbombs.free", this);
            lang.RegisterMessages(Messages, this);
            craftedBombs = new List<ulong>();
        }
        void OnServerInitialized()
        {
            LoadVariables();
            ItemDefs = ItemManager.itemList.ToDictionary(i => i.shortname);
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (craftedBombs.Contains(player.userID))
            {
                if (player != null)
                {
                    foreach (var item in configData.CraftingCosts)
                        player.inventory.GiveItem(ItemManager.CreateByItemID(item.itemid, item.amount));
                }
                craftedBombs.Remove(player.userID);
            }
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseOven)
            {
                if (entity.ShortPrefabName == "jackolantern.happy" || entity.ShortPrefabName == "jackolantern.angry")
                {
                    var jack = entity.GetComponent<BaseOven>();
                    if (craftedBombs.Contains(jack.OwnerID))
                    {
                        jack.gameObject.AddComponent<BombLight>();
                        var expEnt = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab", jack.transform.position, new Quaternion(), true);
                        TimedExplosive explosive = expEnt.GetComponent<TimedExplosive>();
                        explosive.timerAmountMax = configData.ExplosiveSettings.DetonationTimer;
                        explosive.timerAmountMin = configData.ExplosiveSettings.DetonationTimer;
                        explosive.explosionRadius = configData.ExplosiveSettings.ExplosionRadius;
                        explosive.damageTypes = new List<Rust.DamageTypeEntry>
                        {
                            new Rust.DamageTypeEntry {amount = configData.ExplosiveSettings.DamageAmount, type = Rust.DamageType.Explosion }
                        };
                        explosive.Spawn();
                        craftedBombs.Remove(jack.OwnerID);
                    }
                }
            }
        }
        #endregion

        #region Helpers
        bool CanUse(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "pumpkinbombs.use") || player.IsAdmin();
        bool IsFree(BasePlayer player) => permission.UserHasPermission(player.UserIDString, "pumpkinbombs.free");
        private bool HasEnoughRes(BasePlayer player, int itemid, int amount) => player.inventory.GetAmount(itemid) >= amount;
        private void TakeResources(BasePlayer player, int itemid, int amount) => player.inventory.Take(null, itemid, amount);
        #endregion

        #region Classes
        class BombLight : MonoBehaviour
        {
            bool isOn;
            public void Awake()
            {
                isOn = false;
                InvokeRepeating("StartLight", 0.5f, 0.5f);
            }

            public void OnDestroy()
            {
                CancelInvoke("StartLight");
                Destroy(gameObject);
            }

            private void StartLight()
            {
                if (isOn)
                {
                    GetComponent<BaseOven>().SetFlag(BaseEntity.Flags.On, false);
                    isOn = false;
                }
                else
                {
                    GetComponent<BaseOven>().SetFlag(BaseEntity.Flags.On, true);
                    isOn = true;
                }
            }            
        }
        #endregion

        #region Chat Commands
        [ChatCommand("pb")]
        void cmdPB(BasePlayer player, string command, string[] args)
        {
            if (!CanUse(player)) return;
            if (craftedBombs.Contains(player.userID))
            {
                if (!HasEnoughRes(player, -1284735799, 1))
                {
                    SendReply(player, $"{configData.Messaging.Main}{msg("lostBomb", player.UserIDString)}</color>");
                    craftedBombs.Remove(player.userID);
                    return;
                }
                SendReply(player, $"{configData.Messaging.Main}{msg("alreadyhave", player.UserIDString)}</color>");
                return;
            }
            if (!IsFree(player))
            {
                bool canCraft = true;
                foreach (var item in configData.CraftingCosts)
                {
                    if (!HasEnoughRes(player, item.itemid, item.amount)) { canCraft = false; break; }
                }
                if (canCraft)
                {
                    foreach (var item in configData.CraftingCosts)
                        TakeResources(player, item.itemid, item.amount);
                }
                else
                {
                    SendReply(player, $"{configData.Messaging.Main}{msg("noRes", player.UserIDString)}</color>");
                    foreach (var item in configData.CraftingCosts)
                        SendReply(player, $"{configData.Messaging.Main}{item.amount}x {ItemDefs[item.shortname].displayName.english}</color>");
                    return;
                }
            }
            craftedBombs.Add(player.userID);
            player.inventory.GiveItem(ItemManager.CreateByItemID(-1284735799, 1));
            SendReply(player, $"{configData.Messaging.Main}{msg("readyMsg", player.UserIDString)}</color>");
        }
        #endregion

        #region Config 
        class CraftCost
        {
            public string shortname;
            public int itemid;
            public int amount;
        }
        class Explosive
        {
            public int DetonationTimer { get; set; }
            public float ExplosionRadius { get; set; }
            public float DamageAmount { get; set; }
        }
        class Messaging
        {
            public string Main { get; set; }
        }
        private ConfigData configData;
        class ConfigData
        {
            public Explosive ExplosiveSettings { get; set; }
            public List<CraftCost> CraftingCosts { get; set; }
            public Messaging Messaging { get; set; }
            
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
                CraftingCosts = new List<CraftCost>
                {
                    new CraftCost
                    {
                        amount = 1,
                        itemid = 498591726,
                        shortname = "explosive.timed"
                    },
                    new CraftCost
                    {
                        amount = 1,
                        itemid = -225085592,
                        shortname = "pumpkin"
                    }
                },
                ExplosiveSettings = new Explosive
                {
                    DetonationTimer = 10,
                    ExplosionRadius = 10,
                    DamageAmount = 550
                },                
                Messaging = new Messaging
                {
                    Main = "<color=orange>"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Messaging
        string msg(string key, string id = null) => lang.GetMessage(key, this, id);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"readyMsg","Your pumpkin bomb is ready. Simply place the Jack'O'Lantern you just received on the floor to activate it" },
            { "noRes","You do not have enough resources to create a pumpkin bomb. You will need the following;"},
            {"alreadyhave", "You already have a pumpkin bomb ready for deployment" },
            {"lostBomb", "It seems you have lost your bomb. Now you must create a new one..." }
        };
        #endregion
    }
}
