using System;
using System.Collections.Generic;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("RandomDeployables", "Norn", 0.2, ResourceId = 2187)]
    [Description("Randomize deployable skins")]

    class RandomDeployables : RustPlugin
    {
        void Loaded()
        {
            permission.RegisterPermission("randomdeployables.able", this);
            InitializeTable();
            if (Config["Enabled", "SleepingBags"] == null)
            {
                Puts("Updating configuration...");
                Config["Enabled", "SleepingBags"] = true;
                Config["Enabled", "Boxes"] = true;
                SaveConfig();
            }
            Puts("[Enabled] Bags: " + Config["Enabled", "SleepingBags"].ToString() + " | Boxes: " + Config["Enabled", "Boxes"].ToString());
        }
        private static Dictionary<string, int> deployedToItem = new Dictionary<string, int>();
        private void InitializeTable()
        {
            deployedToItem.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                if (itemdef.GetComponent<ItemModDeployable>() != null) deployedToItem.Add(itemdef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath, itemdef.itemid);
            }
        }
        protected override void LoadDefaultConfig()
        {
            // -- [ RESET ] ---

            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ CONFIG ] ---

            Config["Enabled", "SleepingBags"] = true;
            Config["Enabled", "Boxes"] = true;

            // --- [ PREFABS ] ---

            Config["PrefabID", "SleepingBag"] = "assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab";
            Config["PrefabID", "LargeBox"] = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";
        }
        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BaseEntity e = gameObject.ToBaseEntity();
            BasePlayer player = planner.GetOwnerPlayer();
            if (permission.UserHasPermission(player.net.connection.userid.ToString(), "randomdeployables.able"))
            {
                if (!(e is BaseEntity) || player == null)
                {
                    return;
                }
                int skinid = 0;
                if (gameObject.name == Config["PrefabID", "SleepingBag"].ToString() && Convert.ToBoolean(Config["Enabled", "SleepingBags"])) // Fire Up
                {
                    Vector3 position = e.transform.position;
                    Quaternion rot = e.transform.rotation;
                    if (e.GetComponentInParent<SleepingBag>() != null)
                    {
                        // Find skin
                        SleepingBag bag = e.GetComponentInParent<SleepingBag>();
                        if (bag.skinID == 0)
                        {
                            var BaseItem = ItemManager.FindItemDefinition(deployedToItem[bag.gameObject.name]);
                            skinid = BaseItem.skins[UnityEngine.Random.Range(0, (int)BaseItem.skins.Length)].id;
                            bag.skinID = skinid;
                        }
                        else
                        {
                            skinid = bag.skinID;
                        }

                        // Kill old bag and respawn with new skin
                        bag.Kill();
                        BaseEntity new_entity = GameManager.server.CreateEntity(bag.PrefabName, position, rot);
                        SleepingBag updated_bag = new_entity.GetComponentInParent<SleepingBag>();
                        updated_bag.deployerUserID = player.userID;
                        updated_bag.OwnerID = player.userID;
                        updated_bag.niceName = DateTime.Now.ToShortDateString() + " " + DateTime.Now.TimeOfDay.ToShortString();
                        updated_bag.skinID = skinid;
                        updated_bag.Spawn();
                    }
                }
                else if (gameObject.name == Config["PrefabID", "LargeBox"].ToString() && Convert.ToBoolean(Config["Enabled", "Boxes"])) // Fire Up
                {
                    Vector3 position = e.transform.position;
                    Quaternion rot = e.transform.rotation;
                    if (e.GetComponentInParent<Deployable>() != null)
                    {
                        // Find skin
                        StorageContainer box = e.GetComponentInParent<StorageContainer>();
                        if (box.skinID == 0)
                        {
                            var BaseItem = ItemManager.FindItemDefinition(deployedToItem[box.gameObject.name]);
                            skinid = BaseItem.skins[UnityEngine.Random.Range(0, (int)BaseItem.skins.Length)].id;
                            box.skinID = skinid;
                        }
                        else
                        {
                            skinid = box.skinID;
                        }

                        // Kill old box and respawn with new skin
                        box.Kill();
                        BaseEntity new_entity = GameManager.server.CreateEntity(box.PrefabName, position, rot);
                        StorageContainer updated_box = new_entity.GetComponentInParent<StorageContainer>();
                        updated_box.OwnerID = player.userID;
                        updated_box.skinID = skinid;
                        updated_box.Spawn();
                    }
                }
            }
        }
    }
}