namespace Oxide.Plugins
{
    [Info("Usable To Belt", "Waizujin", 1.1)]
    [Description("Any usable item will be moved to your belt if there is space.")]
    public class UsableToBelt : RustPlugin
    {
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            bool alreadyHasStack = false;
            ItemContainer belt = task.owner.inventory.containerBelt;
            ItemContainer main = task.owner.inventory.containerMain;

            foreach (Item item2 in main.itemList)
            {
                if (item.info.itemid == item2.info.itemid)
                {
                    if (item2.info.stackable > 1)
                    {
                        alreadyHasStack = true;
                    }
                }
            }

            if (alreadyHasStack == false)
            {
                if (item.info.category == ItemCategory.Weapon ||
                    item.info.category == ItemCategory.Tool ||
                    item.info.category == ItemCategory.Medical ||
                    item.info.category == ItemCategory.Food ||
                    item.info.category == ItemCategory.Construction)
                {
                    if (!belt.SlotTaken(0)) {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 0));
                    }
                    else if (!belt.SlotTaken(1))
                    {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 1));
                    }
                    else if (!belt.SlotTaken(2))
                    {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 2));
                    }
                    else if (!belt.SlotTaken(3))
                    {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 3));
                    }
                    else if (!belt.SlotTaken(4))
                    {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 4));
                    }
                    else if (!belt.SlotTaken(5))
                    {
                        timer.Once(0.1f, () => item.MoveToContainer(belt, 5));
                    }
                }
            }
        }
    }
}
