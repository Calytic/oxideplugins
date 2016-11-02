using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Drop Blocker", "Krava", 1.1)]
    [Description("Anti drop items at the craft.")]

    public class DropBlocker : RustPlugin
    {
        private class TempData
        {
            public int Uid { get; set; }
            public int Amount { get; set; }
            public int Stack { get; set; }
        }

        private void Loaded()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "CantCraft", "Item not crafted! Your inventory is full." }
            }, this);
        }

        private void Merge(ItemCraftTask task, ref List<TempData> data)
        {
            var amount = task.amount;
            var item = task.blueprint.targetItem;

            foreach (var p in data.Where(x => x.Uid == item.itemid && x.Stack - x.Amount > 0))
            {
                if (amount == 0) break;

                var toStack = p.Stack - p.Amount;

                if (amount > toStack)
                {
                    p.Amount += toStack;
                    amount -= toStack;
                }
                else
                {
                    p.Amount += amount;
                    amount = 0;
                    break;
                }
            }

            if (amount != 0)
            {
                var count = amount / item.stackable;

                for (var i = 0; i < count; i++)
                    data.Add(new TempData
                    {
                        Uid = item.itemid,
                        Amount = item.stackable,
                        Stack = item.stackable
                    });

                if (amount % item.stackable != 0)
                {
                    data.Add(new TempData
                    {
                        Uid = item.itemid,
                        Amount = amount % item.stackable,
                        Stack = item.stackable
                    });
                }
            }
        }

        private bool CanCraft(BasePlayer player, ItemCraftTask task)
        {
            var data = new List<TempData>();

            foreach (var item in player.inventory.containerMain.itemList)
                data.Add(new TempData
                {
                    Uid = item.info.itemid,
                    Amount = item.amount,
                    Stack = item.MaxStackable()
                });

            foreach (var item in player.inventory.containerBelt.itemList)
                data.Add(new TempData
                {
                    Uid = item.info.itemid,
                    Amount = item.amount,
                    Stack = item.MaxStackable()
                });

            foreach (var t in player.inventory.crafting.queue)
                Merge(t, ref data);

            Merge(task, ref data);

            if (data.Count <= 30)
                return true;

            return false;
        }

        private void OnItemCraft(ItemCraftTask task, BasePlayer player)
        {
            if (task == null || player == null)
                return;

            if (!CanCraft(player, task))
            {
                task.cancelled = true;

                foreach (Item i in task.takenItems)
                    player.inventory.GiveItem(i);

                SendReply(player, GetMessage("CantCraft", player.UserIDString));
            }
        }

        private string GetMessage(string name, string sid = null)
        {
            return lang.GetMessage(name, this, sid);
        }
    }
}