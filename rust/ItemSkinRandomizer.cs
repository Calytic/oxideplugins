// Reference: Rust.Workshop

using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Item Skin Randomizer", "Mughisi", 1.1, ResourceId = 1328)]
    [Description("Simple plugin that will select a random skin for an item when crafting.")]
    class ItemSkinRandomizer : RustPlugin
    {
        private readonly Dictionary<string, List<int>> skinsCache = new Dictionary<string, List<int>>();
        private readonly List<int> randomizedTasks = new List<int>();

        private void OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            var skins = GetSkins(task.blueprint.targetItem);
            if (skins.Count < 1 || task.skinID != 0) return;
            randomizedTasks.Add(task.taskUID);
            task.skinID = skins.GetRandom();
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!randomizedTasks.Contains(task.taskUID)) return;
            if (task.amount == 0)
            {
                randomizedTasks.Remove(task.taskUID);
                return;
            }
            var skins = GetSkins(task.blueprint.targetItem);
            task.skinID = skins.GetRandom();
        }

        private List<int> GetSkins(ItemDefinition def)
        {
            List<int> skins;
            if (skinsCache.TryGetValue(def.shortname, out skins)) return skins;
            skins = new List<int> { 0 };
            skins.AddRange(ItemSkinDirectory.ForItem(def).Select(skin => skin.id));
            skins.AddRange(Rust.Workshop.Approved.All.Where(skin => skin.ItemName == def.shortname).Select(skin => (int)skin.InventoryId));
            skinsCache.Add(def.shortname, skins);
            return skins;
        }
    }
}
