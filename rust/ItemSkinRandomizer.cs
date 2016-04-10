
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Item Skin Randomizer", "Mughisi", "1.0.1")]
    [Description("Simple plugin that will select a random skin for an item when crafting.")]
    class ItemSkinRandomizer : RustPlugin
    {

        private ItemSkinDirectory.Skin defaultSkin;
        private readonly List<int> randomizedTasks = new List<int>();

        private void Loaded()
        {
            defaultSkin = new ItemSkinDirectory.Skin
            {
                name = "Default",
                id = 0
            };
        }

        private void OnItemCraft(ItemCraftTask task, BasePlayer crafter)
        {
            var skins = ItemSkinDirectory.ForItem(task.blueprint.targetItem).ToList();
            if (skins.Count < 1 || task.skinID != 0) return;
            randomizedTasks.Add(task.taskUID);
            skins.Add(defaultSkin);
            task.skinID = skins.GetRandom().id;
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (!randomizedTasks.Contains(task.taskUID)) return;
            if (task.amount == 0)
            {
                randomizedTasks.Remove(task.taskUID);
                return;
            }
            var skins = ItemSkinDirectory.ForItem(task.blueprint.targetItem).ToList();
            skins.Add(defaultSkin);
            task.skinID = skins.GetRandom().id;
        }
    }
}
