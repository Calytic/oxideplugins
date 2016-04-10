namespace Oxide.Plugins
{
    [Info("Replace On Broken", "Waizujin", 1.1)]
    [Description("Replaces your active broken item with a not broken item if in inventory.")]
    public class ReplaceOnBroken : RustPlugin
    {
        void OnLoseCondition(Item item, ref float amount)
        {
            if (item.parent.HasFlag(ItemContainer.Flag.IsPlayer))
            {
                if (item.condition <= amount)
                {
                    ItemContainer main = item.parent.playerOwner.inventory.containerMain;
                    ItemContainer belt = item.parent.playerOwner.inventory.containerBelt;

                    foreach (Item item2 in main.itemList)
                    {
                        if (item.info.itemid == item2.info.itemid && item2.condition > 0f)
                        {
                            int brokenItemPosition = item.position;
                            int newItemPosition = item2.position;

                            timer.Once(0.1f, () => item.MoveToContainer(main, newItemPosition));
                        }
                    }
                }
            }
        }
    }
}
