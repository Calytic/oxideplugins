// Reference: Rust.Workshop
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Oxide.Plugins
{
    [Info("RandomDeployables", "Norn", 0.5, ResourceId = 2187)]
    [Description("Randomize deployable skins")]

    class RandomDeployables : RustPlugin
    {
        void Loaded()
        {
            permission.RegisterPermission("randomdeployables.able", this);
            InitializeTable();
            if (Config["Settings", "AllowDefaultSkin"] == null)
            {
                Puts("Updating configuration...");
                Config["Settings", "AllowDefaultSkin"] = false;
                SaveConfig();
            }
            Puts("[Enabled] Bags: " + Config["Enabled", "SleepingBags"].ToString() + " | Boxes: " + Config["Enabled", "Boxes"].ToString());
        }
        private static Dictionary<string, int> deployedToItem = new Dictionary<string, int>();
        private static List<ulong> SkinList = new List<ulong>();
        private void InitializeTable()
        {
            deployedToItem.Clear();
            SkinList.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                if (itemdef.GetComponent<ItemModDeployable>() != null) deployedToItem.Add(itemdef.GetComponent<ItemModDeployable>().entityPrefab.resourcePath, itemdef.itemid);
                foreach(ItemSkinDirectory.Skin skin in itemdef.skins)
                {
                    var ws = Rust.Workshop.Approved.FindByInventoryId((ulong)skin.id);
                    if (skin.id != 0 && ws != null) { SkinList.Add(ws.WorkshopdId); } else { SkinList.Add((ulong)skin.id); }
                }
            }
            Puts(SkinList.Count.ToString() + " skins verified.");
        }
        protected override void LoadDefaultConfig()
        {
            // -- [ RESET ] ---

            Puts("No configuration file found, generating...");
            Config.Clear();

            // --- [ SETTINGS ] ---
            Config["Settings", "AllDeployables"] = false;
            Config["Settings", "UseAllSkins"] = false;
            Config["Settings", "AllowDefaultSkin"] = false;

            // --- [ CONFIG ] ---

            Config["Enabled", "SleepingBags"] = true;
            Config["Enabled", "Boxes"] = true;

            // --- [ PREFABS ] ---

            Config["PrefabID", "SleepingBag"] = "assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab";
            Config["PrefabID", "LargeBox"] = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";
        }
        private List<int> GetSkins(ItemDefinition def)
        {
            List<int> skins = new List<int> { 0 };
            skins.AddRange(ItemSkinDirectory.ForItem(def).Select(skin => skin.id));
            skins.AddRange(Rust.Workshop.Approved.All.Where(skin => skin.ItemName == def.shortname).Select(skin => (int)skin.WorkshopdId));
            if (!Convert.ToBoolean(Config["Settings", "AllowDefaultSkin"])) { if (skins.Contains(0)) { skins.Remove(0); } }
            return skins;
        }
        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BaseEntity e = gameObject.ToBaseEntity();
            BasePlayer player = planner.GetOwnerPlayer();
            if (permission.UserHasPermission(player.net.connection.userid.ToString(), "randomdeployables.able"))
            {
                if (!(e is BaseEntity) || player == null) { return; }
                if (Convert.ToBoolean(Config["Settings", "AllDeployables"]))
                {
                    if (deployedToItem.ContainsKey(e.PrefabName))
                    {
                        var skin = 0;
                        var def = ItemManager.FindItemDefinition(deployedToItem[e.PrefabName]);
                        if (!Convert.ToBoolean(Config["Settings", "UseAllSkins"])) { skin = GetSkins(def).GetRandom(); }
                        else { skin = (int)SkinList.GetRandom(); }
                        e.skinID = Convert.ToUInt64(skin);
                        e.SendNetworkUpdate();
                    }
                }
                else
                {
                    if (gameObject.name == Config["PrefabID", "SleepingBag"].ToString() && Convert.ToBoolean(Config["Enabled", "SleepingBags"])) // Fire Up
                    {
                        var skin = 0;
                        var def = ItemManager.FindItemDefinition(deployedToItem[Config["PrefabID", "SleepingBag"].ToString()]);
                        if (!Convert.ToBoolean(Config["Settings", "UseAllSkins"])) { skin = GetSkins(def).GetRandom(); }
                        else { skin = (int)SkinList.GetRandom(); }
                        e.skinID = Convert.ToUInt64(skin);
                        e.SendNetworkUpdate();
                    }
                    else if (gameObject.name == Config["PrefabID", "LargeBox"].ToString() && Convert.ToBoolean(Config["Enabled", "Boxes"])) // Fire Up
                    {
                        var skin = 0;
                        var def = ItemManager.FindItemDefinition(deployedToItem[Config["PrefabID", "LargeBox"].ToString()]);
                        if (!Convert.ToBoolean(Config["Settings", "UseAllSkins"])) { skin = GetSkins(def).GetRandom(); }
                        else { skin = (int)SkinList.GetRandom(); }
                        e.skinID = Convert.ToUInt64(skin);
                        e.SendNetworkUpdate();
                    }
                }
            }
        }
    }
}