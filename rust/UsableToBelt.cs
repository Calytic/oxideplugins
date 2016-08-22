namespace Oxide.Plugins
{
    [Info("UsableToBelt", "Wulf/lukespragg", "1.2.1", ResourceId = 1141)]
    [Description("Any usable item will be moved to your belt if there is space")]

    class UsableToBelt : RustPlugin
    {
        const string permAllow = "usabletobelt.allow";

        void Init() => permission.RegisterPermission(permAllow, this);

        void HandleItem(Item item, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permAllow)) return;

            var alreadyHasStack = false;
            var belt = player.inventory.containerBelt;
            var main = player.inventory.containerMain;

            foreach (var invItem in main.itemList)
            {
                if (item.info.itemid != invItem.info.itemid) continue;
                if (invItem.info.stackable > 1) alreadyHasStack = true;
            }

            if (alreadyHasStack) return;
            if (item.info.category != ItemCategory.Weapon && item.info.category != ItemCategory.Tool &&
                item.info.category != ItemCategory.Medical && item.info.category != ItemCategory.Food &&
                item.info.category != ItemCategory.Construction) return;

            for (var i = 0; i < PlayerBelt.MaxBeltSlots; i++)
            {
                if (belt.SlotTaken(i)) continue;
                timer.Once(0.1f, () => item.MoveToContainer(belt, i));
                break;
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            var player = container.GetOwnerPlayer();
            if (player != null && !container.HasFlag(ItemContainer.Flag.Belt)) HandleItem(item, player);
        }
    }
}
