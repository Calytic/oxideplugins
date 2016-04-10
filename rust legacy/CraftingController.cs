// Reference: Oxide.Ext.RustLegacy
// Reference: Facepunch.ID
// Reference: Facepunch.MeshBatch
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using RustProto;

namespace Oxide.Plugins
{
    [Info("CraftingController", "Reneb", "1.0.0")]
    class CraftingController : RustLegacyPlugin
    {

        public static string blockResearchMessage = "Researching this item has been blocked.";
        public static string blockBlueprintMessage = "This blueprint has been disabled.";
        public static string blockCraftMessage = "Crafting this item has been blocked.";
        public static List<object> blockedCrafting = new List<object>();
        public static List<object> blockedResearch = new List<object>();
        public static List<object> blockedBlueprints = new List<object>();

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void Init()
        {
            CheckCfg<string>("Messages: Block Research", ref blockResearchMessage);
            CheckCfg<string>("Messages: Block Blueprint", ref blockBlueprintMessage);
            CheckCfg<string>("Messages: Block Craft", ref blockCraftMessage);
            CheckCfg<List<object>>("Crafts: Block List", ref blockedCrafting);
            CheckCfg<List<object>>("Researching: Block List", ref blockedResearch);
            CheckCfg<List<object>>("Blueprints: Block List", ref blockedBlueprints);
            SaveConfig();
        }
         

        object OnBlueprintUse(BlueprintDataBlock bpdb, IBlueprintItem item)
        {
            if (!blockedBlueprints.Contains(bpdb.name)) return null;
            if (!item.inventory) return null;
            NetUser netuser = (item.inventory.idMain as Character).netUser;
            if(netuser != null)
                ConsoleNetworker.SendClientCommand(netuser.networkPlayer, "notice.popup 10 q " + Facepunch.Utility.String.QuoteSafe(blockBlueprintMessage));
            return true;
        }

        object OnItemCraft(CraftingInventory inv, BlueprintDataBlock bpdb, int amount, ulong starttime)
        {
            if (!blockedCrafting.Contains(bpdb.resultItem.name)) return null;
            NetUser netuser = (inv.idMain as Character).netUser;
            if (netuser != null)
                ConsoleNetworker.SendClientCommand(netuser.networkPlayer, "notice.popup 10 q " + Facepunch.Utility.String.QuoteSafe(blockCraftMessage));
            return true;
        } 
        object OnResearchItem(InventoryItem resourceitem, IInventoryItem otherItem)
        {
            if (!blockedResearch.Contains(otherItem.datablock.name)) return null;
            NetUser netuser = (resourceitem.inventory.idMain as Character).netUser;
            if (netuser != null)
                ConsoleNetworker.SendClientCommand(netuser.networkPlayer, "notice.popup 10 q " + Facepunch.Utility.String.QuoteSafe(blockResearchMessage));
            return InventoryItem.MergeResult.Failed;
        } 
    }
}
 