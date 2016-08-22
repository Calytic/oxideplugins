using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Oxide.Core;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("MagicCraft", "Norn", "0.2.2", ResourceId = 1347)]
    [Description("An alternative crafting system.")]
    public class MagicCraft : RustPlugin
    {
        int MaxB = 999;
        int MinB = 1;
        int Cooldown = 0;
        [PluginReference]
        Plugin PopupNotifications;
        bool MessageType = false;
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
            public int SkinID;
            public CraftInfo()
            {
            }
        }
        StoredData storedData;
        private void ConfigurationCheck()
        {
            if (Config["RandomizeSkins"] == null) { Config["RandomizeSkins"] = false; }
            try { if (Config.Count() == 0) LoadDefaultConfig(); } catch { Puts("Configuration file seems to be unreadable... Re-generating."); LoadDefaultConfig(); }
            if (Config["RndSkinsFirstGen"] == null)
            {
                Config["RndSkinsFirstGen"] = false;
                SaveConfig();
                Puts("Configuration file out of date... updating.");
            }
        }
        void Loaded()
        {
            if (!permission.PermissionExists("MagicCraft.able")) permission.RegisterPermission("MagicCraft.able", this);
        }
        private void OnServerInitialized()
        {
            storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
            ConfigurationCheck();
            if (Convert.ToBoolean(Config["MessagesEnabled"]))
            {
                if (PopupNotifications && Convert.ToBoolean(Config["UsePopupNotifications"]))
                {
                    MessageType = true;
                }
            }
            int config_protocol = Convert.ToInt32(Config["Protocol"]);
            if (Config["Protocol"] == null)
            {
                Config["Protocol"] = Protocol.network;
            }
            else if (Convert.ToInt32(Config["Protocol"]) != Protocol.network)
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
            Config["UsePopupNotifications"] = false;
            Config["MessagesEnabled"] = true;
            Config["RndSkinsFirstGen"] = true;
            Config["RandomizeSkins"] = false;
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
                        if (Convert.ToBoolean(Config["RndSkinsFirstGen"])) { z.SkinID = definition.Value.skins.GetRandom().id; } else { z.SkinID = 0; }
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
                        int amount = task.amount;
                        if (amount < entry.Value.MinBulkCraft || amount > entry.Value.MaxBulkCraft) { return null; }
                        ItemDefinition item = GetItem(itemname);

                        int final_amount = task.blueprint.amountToCreate * amount; int skinid = 0;
                        if (!Convert.ToBoolean(Config["RandomizeSkins"])) { if (entry.Value.SkinID != 0) { skinid = entry.Value.SkinID; } } else { skinid = item.skins.GetRandom().id; }
                        crafter.inventory.GiveItem(ItemManager.CreateByItemID(item.itemid, final_amount, skinid), null);
                        if (Convert.ToBoolean(Config["MessagesEnabled"]))
                        {
                            string returnstring = null; string skin_string = null;
                            if (skinid != 0) { skin_string = "Skin ID: <color=#66FF66>" + skinid.ToString() + "</color>"; } else { skin_string = "<color=red>No skin</color>"; }
                            if (PopupNotifications && Convert.ToBoolean(Config["UsePopupNotifications"]))
                            {
                                returnstring = "You have crafted <color=#66FF66>" + amount.ToString() + "</color> <color=#66FFFF>" + item.displayName.english.ToString() + "</color>\n\n[Batch Amount: <color=#66FF66>" + final_amount.ToString() + "</color>] [" + skin_string + "]";
                            }
                            else
                            {
                                returnstring = "You have crafted <color=#66FF66>" + amount.ToString() + "</color> <color=#66FFFF>" + item.displayName.english.ToString() + "</color>\n[Batch Amount: <color=#66FF66>" + final_amount.ToString() + "</color>] [" + skin_string + "]";
                            }
                            PrintToChatEx(crafter, returnstring, MessageType);
                        }
                        return false;
                    }
                }
            }
            return null;
        }
        private void PrintToChatEx(BasePlayer player, string result, bool type = false, string tcolour = "#66FF66")
        {
            if (!type) { PrintToChat(player, "<color=\"" + tcolour + "\">[" + this.Title.ToString() + "]</color> " + result); }
            else { PopupNotifications?.Call("CreatePopupNotification", "<color=" + tcolour + ">" + this.Title.ToString() + "</color>\n" + result, player); }
        }
        private ItemDefinition GetItem(string shortname)
        {
            if (string.IsNullOrEmpty(shortname) || mcITEMS == null) return null;
            ItemDefinition item;
            if (mcITEMS.TryGetValue(shortname, out item)) return item;
            return null;
        }
    }
}