using System.Collections.Generic;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Coordinates one ItemSlot model with its ItemSlotView and exposes UI actions.
    /// </summary>
    public class ItemSlotPresenter
    {
        /// <summary>View associated with this presenter.</summary>
        public ItemSlotView View { get; private set; }
        /// <summary>Slot model associated with this presenter.</summary>
        public ItemSlot Slot { get; private set; }
        /// <summary>Zero-based index in the inventory slot list.</summary>
        public int Index { get; private set; }
        /// <summary>Inventory that owns this slot.</summary>
        public InventoryController InventoryPresenter { get; private set; }

        /// <summary>Creates a presenter and binds its view to the slot.</summary>
        public ItemSlotPresenter(ItemSlotView view, ItemSlot model, int index)
        {
            View = view;
            Slot = model;
            Index = index;
            View?.Bind(this);
        }

        /// <summary>Assigns the inventory owner for this presenter.</summary>
        public void Bind(InventoryController inventory)
        {
            InventoryPresenter = inventory;
        }

        /// <summary>Moves/merges/swaps this slot with another slot in the same inventory.</summary>
        public bool TryMoveTo(ItemSlotPresenter target)
        {
            if (InventoryPresenter == null || target == null ||
                target.InventoryPresenter != InventoryPresenter)
            {
                return false;
            }

            return InventoryPresenter.TryMove(Index, target.Index);
        }

        /// <summary>Builds the available context-menu actions for the current stack.</summary>
        public List<ContextMenuAction> BuildContextActions()
        {
            var actions = new List<ContextMenuAction>();
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return actions;

            var stack = Slot.Item;
            var definition = stack.Data;

            if (definition.Sellable && definition.SellPrice > 0)
            {
                var totalPrice = (long)definition.SellPrice * stack.Amount;
                actions.Add(new ContextMenuAction($"Продать ({totalPrice})", SellItem));
            }

            if (stack.Amount > 1)
            {
                var half = stack.Amount / 2;
                var canSplit = InventoryPresenter.FindEmptySlot() >= 0;
                actions.Add(new ContextMenuAction($"Разделить ({half})", () => SplitItem(half), canSplit));
            }

            if (definition.Droppable)
                actions.Add(new ContextMenuAction("Выбросить", DropItem));

            actions.Add(new ContextMenuAction("Уничтожить", DiscardItem));
            return actions;
        }

        private void UseItem()
        {
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return;
            var data = Slot.Item.Data;
            if (!InventoryPresenter.RemoveFromSlot(Index, 1)) return;
            InventoryPresenter.OnItemUsed?.Invoke(new ItemStack(data, 1));
        }

        private void EquipItem()
        {
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return;
            InventoryPresenter.OnItemEquipped?.Invoke(new ItemStack(Slot.Item.Data, 1));
        }

        private void SellItem()
        {
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return;
            var data = Slot.Item.Data;
            var amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemSold?.Invoke(new ItemStack(data, amount));
        }

        private void SplitItem(int amount)
        {
            if (amount <= 0 || InventoryPresenter == null) return;
            InventoryPresenter.SplitStack(Index, amount);
        }

        private void DropItem()
        {
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return;
            var data = Slot.Item.Data;
            var amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemDropped?.Invoke(new ItemStack(data, amount));
        }

        private void DiscardItem()
        {
            if (Slot == null || Slot.IsEmpty || InventoryPresenter == null) return;
            var data = Slot.Item.Data;
            var amount = Slot.Item.Amount;
            if (!InventoryPresenter.RemoveFromSlot(Index, amount)) return;
            InventoryPresenter.OnItemDiscarded?.Invoke(new ItemStack(data, amount));
        }
    }
}
