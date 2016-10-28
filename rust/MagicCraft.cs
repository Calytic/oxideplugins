using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("MagicCraft", "Norn", "0.2.4", ResourceId = 1347)]
    [Description("An alternative crafting system.")]
    public class MagicCraft : RustPlugin
    {
        int MaxB = 999;
        int MinB = 1;
        int Cooldown = 0;
        bool MessageType = false;
        int MAX_INV_SLOTS = 30;
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        class StoredData
        {
            public Dictionary<string, CraftInfo> CraftList = new Dictionary<string, CraftInfo>();
            public StoredData()
            {
            }
        }

        class CraftInfo
        {
            public int MaxBulkCraft;
            public int MinBulkCraft;
            public string displayName;
            public string shortName;
            public string description;
            public bool Enabled;
            public int Cooldown;
            public CraftInfo()
            {
            }
        }
        StoredData storedData;
        private void ConfigurationCheck()
        {
            try { if (Config.Count() == 0) LoadDefaultConfig(); } catch { Puts("Configuration file seems to be unreadable... Re-generating."); LoadDefaultConfig(); }
        }
        void Loaded()
        {
            permission.RegisterPermission("MagicCraft.able", this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CraftSuccess"] = "You have crafted <color=#66FF66>{0}</color> <color=#66FFFF>{1}</color>\n[Batch Amount: <color=#66FF66>{2}</color>]",
                ["DifferentSlots"] = "You <color=yellow>only</color> have <color=green>{0}</color> slots left, crafting <color=green>{1}</color> / <color=red>{2}</color> requested.",
                ["InventoryFull"] = "Your <color=yellow>inventory</color> is <color=red>full</color>!"
            }, this);
        }
        private void OnServerInitialized()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
            ConfigurationCheck();
            int config_protocol = Convert.ToInt32(Config["Protocol"]);
            if (Config["Protocol"] == null)
            {
                Config["Protocol"] = Protocol.network;
            }
            else if (Convert.ToInt32(Config["Protocol"]) != Protocol.network && !Convert.ToBoolean(Config["IgnoreProtocolChanges"]))
            {
                Config["Protocol"] = Protocol.network;
                Puts("Updating item list from protocol " + config_protocol.ToString() + " to protocol " + Config["Protocol"].ToString() + ".");
                GenerateItems(true);
                SaveConfig();
            }
            else
            {
                GenerateItems(false);
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("No configuration file found, generating...");
            Config.Clear();
            Config["Protocol"] = Protocol.network;
            Config["IgnoreProtocolChanges"] = false;
            Config["MessagesEnabled"] = true;
            timer.Once(10, () => GenerateItems(true));
            SaveConfig();
        }

        void GenerateItems(bool reset = false)
        {
            if (reset)
            {
                Interface.GetMod().DataFileSystem.WriteObject(this.Title + ".old", storedData);
                storedData.CraftList.Clear();
                Puts("Generating new item list...");
            }
            mcITEMS = ItemManager.itemList.ToDictionary(i => i.shortname);
            int loaded = 0, enabled = 0;
            foreach (var definition in mcITEMS)
            {
                if (definition.Value.shortname.Length >= 1)
                {
                    CraftInfo p = null;
                    if (storedData.CraftList.TryGetValue(definition.Value.shortname, out p))
                    {
                        if (p.Enabled) { enabled++; }
                        loaded++;
                    }
                    else
                    {
                        CraftInfo z = new CraftInfo();
                        z.description = definition.Value.displayDescription.english.ToString();
                        z.displayName = definition.Value.displayName.english.ToString();
                        z.shortName = definition.Value.shortname.ToString();
                        z.MaxBulkCraft = MaxB;
                        z.MinBulkCraft = MinB;
                        z.Cooldown = Cooldown;
                        z.Enabled = false;
                        storedData.CraftList.Add(definition.Value.shortname.ToString(), z);
                        loaded++;
                    }
                }
            }
            int inactive = loaded - enabled;
            Puts("Loaded " + loaded.ToString() + " items. (Enabled: " + enabled.ToString() + " | Inactive: " + inactive.ToString() + ").");
            Interface.GetMod().DataFileSystem.WriteObject(this.Title, storedData);
        }
        public int InventorySlots(BasePlayer player, bool incwear = true, bool incbelt = true)
        {
            List<Item> list = new List<Item>();
            list.AddRange(player.inventory.containerMain.itemList);                     // 24
            if (incbelt) { list.AddRange(player.inventory.containerBelt.itemList); }    // 6
            if (incwear) { list.AddRange(player.inventory.containerWear.itemList); }    // 6
            return list.Count;
        }
        public int FreeInventorySlots(BasePlayer player, bool incwear = true, bool incbelt = true) { return MAX_INV_SLOTS - InventorySlots(player, false, true); }
        private Dictionary<string, ItemDefinition> mcITEMS;
        private object OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (permission.UserHasPermission(crafter.net.connection.userid.ToString(), "MagicCraft.able"))
            {
                var itemname = task.blueprint.targetItem.shortname.ToString();
                foreach (var entry in storedData.CraftList)
                {
                    if (entry.Value.shortName == itemname && entry.Value.Enabled)
                    {
                        if (InventorySlots(crafter, false, true) >= MAX_INV_SLOTS) { task.cancelled = true; RefundIngredients(task.blueprint, task.owner, task.amount); PrintToChatEx(crafter, Lang("InventoryFull", crafter.UserIDString));  return null; }
                        int amount = task.amount;
                        if (amount < entry.Value.MinBulkCraft || amount > entry.Value.MaxBulkCraft) { return null; }
                        ItemDefinition item = GetItem(itemname);
                        int free_slots = FreeInventorySlots(crafter);
                        if (amount > free_slots && item.stackable == 1)
                        {
                            string returnstring = null;
                            returnstring = Lang("DifferentSlots", crafter.UserIDString, free_slots.ToString(), free_slots.ToString(), amount.ToString());
                            PrintToChatEx(crafter, returnstring);
                            int refund_amount = amount - free_slots;
                            RefundIngredients(task.blueprint, task.owner, refund_amount); // Refunding the amount that wasn't crafted.
                            amount = free_slots;
                        }
                       
                        int final_amount = task.blueprint.amountToCreate * amount;
                        if (item.stackable == 1 && final_amount != 1 && task.skinID != 0) // Skin fix attempt
                        {
                            for (int amount_interval = 1; amount_interval <= final_amount; amount_interval++) { crafter.inventory.GiveItem(ItemManager.CreateByItemID(item.itemid, 1, task.skinID), null); }
                        }
                        else { crafter.inventory.GiveItem(ItemManager.CreateByItemID(item.itemid, final_amount, task.skinID), null); }
                        if (Convert.ToBoolean(Config["MessagesEnabled"]))
                        {
                            string returnstring = null;
                            returnstring = Lang("CraftSuccess", crafter.UserIDString, amount.ToString(), item.displayName.english.ToString(), final_amount.ToString());
                            PrintToChatEx(crafter, returnstring);
                        }
                        return false;
                    }
                }
            }
            return null;
        }
        private void RefundIngredients(ItemBlueprint bp, BasePlayer player, int amount = 1)
        {
            using (List<ItemAmount>.Enumerator enumerator = bp.ingredients.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ItemAmount current = enumerator.Current;
                    Item i = ItemManager.CreateByItemID(current.itemid, Convert.ToInt32(current.amount) * amount);
                    if (!i.MoveToContainer(player.inventory.containerMain)) { i.Drop(player.eyes.position, player.eyes.BodyForward() * 2f); }
                }
            }
        }
        private void PrintToChatEx(BasePlayer player, string result, string tcolour = "#66FF66") { PrintToChat(player, "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result); }
        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || mcITEMS == null) return null;
            ItemDefinition item;
            if (mcITEMS.TryGetValue(shortname, out item)) return item;
            return null;
        }
    }
}