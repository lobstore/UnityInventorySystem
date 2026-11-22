using System.Collections.Generic;

namespace LOGIYGames.Inventory
{
    public class ItemSlotPresenter
    {
        public ItemSlotView View { get; private set; }
        public ItemSlot Slot { get; private set; }
        public int Index { get; private set; }
        public InventoryController InventoryPresenter { get; private set; }

        public ItemSlotPresenter(ItemSlotView view, ItemSlot model, int index)
        {
            Slot = model;
            View = view;
            Index = index;
            View.Bind(this);
        }

        public void Bind(InventoryController inventory) => InventoryPresenter = inventory;

        public bool TryMoveTo(ItemSlotPresenter target)
        {
            if (InventoryPresenter == null || target == null) return false;
            return InventoryPresenter.TryMove(Index, target.Index);
        }

        public List<ContextMenuAction> BuildContextActions()
        {
            var list = new List<ContextMenuAction>();
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return list;

            var item = Slot.Item;
            var def = item.Data;

           // if (def is ConsumableItemDefinition)
            //    list.Add(new ContextMenuAction("Использовать", UseItem));

            //if (def is WeaponDefinition || def is ArmorDefinition)
            //    list.Add(new ContextMenuAction("Экипировать", EquipItem));

            if (def.Sellable && def.SellPrice > 0)
                list.Add(new ContextMenuAction($"Продать ({def.SellPrice}×{item.Amount})", SellItem));

            if (item.Amount > 1)
            {
                int half = item.Amount / 2;
                bool canSplit = InventoryPresenter.FindEmptySlot() >= 0;
                list.Add(new ContextMenuAction($"Разделить ({half})", () => SplitItem(half), canSplit));
            }

            if (def.Droppable)
                list.Add(new ContextMenuAction("Выбросить", DropItem));

            list.Add(new ContextMenuAction("Уничтожить", DiscardItem));
            return list;
        }

        private void UseItem()
        {
            if (Slot.IsEmpty) return;
            var data = Slot.Item.Data;
            if (!InventoryPresenter.RemoveFromSlot(Index, 1)) return;
            InventoryPresenter.OnItemUsed.Invoke(new ItemStack(data, 1));
        }

        private void EquipItem()
        {
            if (Slot.IsEmpty) return;
            InventoryPresenter.OnItemEquipped.Invoke(new ItemStack(Slot.Item.Data, 1));
        }

        private void SellItem()
        {
            if (Slot.IsEmpty) return;
            var data = Slot.Item.Data;
            int amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemSold.Invoke(new ItemStack(data, amount));
        }

        private void SplitItem(int amount)
        {
            if (amount <= 0) return;
            InventoryPresenter.SplitStack(Index, amount);
        }

        private void DropItem()
        {
            if (Slot.IsEmpty) return;
            var data = Slot.Item.Data;
            int amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemDropped.Invoke(new ItemStack(data, amount));
        }

        private void DiscardItem()
        {
            if (Slot.IsEmpty) return;
            var data = Slot.Item.Data;
            int amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemDiscarded.Invoke(new ItemStack(data, amount));
        }
    }
}