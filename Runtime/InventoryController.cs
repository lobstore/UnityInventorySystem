using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LOGIYGames.Inventory
{
    /// <summary>Runtime inventory component with stack-aware add/remove, slot rearrangement, splitting and transfers.</summary>
    /// <remarks>
    /// Runtime identity uses ItemDefinition asset reference equality. Persistence, currency, equipment,
    /// world drops and item use are game-specific and are exposed through events rather than implemented here.
    /// </remarks>
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();

        /// <summary>Raised when game-specific logic uses an item.</summary>
        public UnityEvent<ItemStack> OnItemUsed = new UnityEvent<ItemStack>();
        /// <summary>Raised when game-specific logic equips an item.</summary>
        public UnityEvent<ItemStack> OnItemEquipped = new UnityEvent<ItemStack>();
        /// <summary>Raised when game-specific logic sells an item.</summary>
        public UnityEvent<ItemStack> OnItemSold = new UnityEvent<ItemStack>();
        /// <summary>Raised when game-specific logic drops an item into the world.</summary>
        public UnityEvent<ItemStack> OnItemDropped = new UnityEvent<ItemStack>();
        /// <summary>Raised when game-specific logic permanently discards an item.</summary>
        public UnityEvent<ItemStack> OnItemDiscarded = new UnityEvent<ItemStack>();
        /// <summary>Raised after a stack has been added; Amount is the accepted quantity.</summary>
        public UnityEvent<ItemStack> OnItemAdded = new UnityEvent<ItemStack>();
        /// <summary>Raised after a stack has been removed; Amount is the removed quantity.</summary>
        public UnityEvent<ItemStack> OnItemRemoved = new UnityEvent<ItemStack>();

        /// <summary>Read-only view of the inventory's ordered slots.</summary>
        public IReadOnlyList<ItemSlot> Slots
        {
            get { EnsureSlots(); return slots; }
        }

        private void Awake()
        {
            EnsureSlots();
            EnsureEvents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureSlots();
            EnsureEvents();
        }
#endif

        private void EnsureSlots()
        {
            if (slots == null) slots = new List<ItemSlot>();
            for (var i = 0; i < slots.Count; i++)
                if (slots[i] == null) slots[i] = new ItemSlot();
        }

        private void EnsureEvents()
        {
            OnItemUsed ??= new UnityEvent<ItemStack>();
            OnItemEquipped ??= new UnityEvent<ItemStack>();
            OnItemSold ??= new UnityEvent<ItemStack>();
            OnItemDropped ??= new UnityEvent<ItemStack>();
            OnItemDiscarded ??= new UnityEvent<ItemStack>();
            OnItemAdded ??= new UnityEvent<ItemStack>();
            OnItemRemoved ??= new UnityEvent<ItemStack>();
        }

        #region Queries

        /// <summary>Returns true when index identifies an existing slot.</summary>
        public bool IsValidIndex(int index)
        {
            EnsureSlots();
            return index >= 0 && index < slots.Count;
        }

        /// <summary>Returns a slot at index, or null when index is outside the inventory.</summary>
        public ItemSlot GetSlot(int index) => IsValidIndex(index) ? slots[index] : null;

        /// <summary>Finds the first non-empty slot with the same ItemDefinition asset reference; returns -1 if absent.</summary>
        public int FindSlot(ItemDefinition data)
        {
            EnsureSlots();
            if (data == null) return -1;
            for (var i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && slots[i].Item.Data == data) return i;
            return -1;
        }

        /// <summary>Finds the first empty slot, or -1 when full.</summary>
        public int FindEmptySlot()
        {
            EnsureSlots();
            for (var i = 0; i < slots.Count; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>Counts all units of the specified definition.</summary>
        public int CountItem(ItemDefinition data)
        {
            EnsureSlots();
            if (data == null) return 0;
            var total = 0;
            foreach (var slot in slots)
                if (!slot.IsEmpty && slot.Item.Data == data) total += slot.Item.Amount;
            return total;
        }

        /// <summary>Checks whether at least amount units exist. A valid definition and amount zero returns true; negative amounts and null definitions return false.</summary>
        public bool ContainsItem(ItemDefinition data, int amount = 1)
            => data != null && amount >= 0 && CountItem(data) >= amount;

        /// <summary>True when no empty slot is available.</summary>
        public bool IsFull => FindEmptySlot() < 0;

        /// <summary>Checks whether the full stack fits, including matching partial stacks. Does not mutate inventory.</summary>
        public bool CanFit(ItemStack stack)
        {
            EnsureSlots();
            if (stack.IsEmpty) return true;
            long space = 0;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty) space += stack.MaxStackSize;
                else if (slot.Item.CanStackWith(stack))
                    space += Mathf.Max(0, slot.Item.MaxStackSize - slot.Item.Amount);
                if (space >= stack.Amount) return true;
            }
            return false;
        }

        #endregion

        #region Add / Remove

        /// <summary>Adds as much as possible, filling matching partial stacks then empty slots. Returns the unaccepted remainder.</summary>
        public int AddItem(ItemStack stack)
        {
            EnsureSlots();
            if (stack.IsEmpty) return 0;
            AutoStack();
            var remaining = stack.Amount;

            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot.IsEmpty || !slot.Item.CanStackWith(stack)) continue;
                remaining = slot.TryAdd(stack.WithAmount(remaining));
            }

            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (!slot.IsEmpty) continue;
                remaining = slot.TryAdd(stack.WithAmount(remaining));
            }

            var added = stack.Amount - remaining;
            if (added > 0) OnItemAdded?.Invoke(stack.WithAmount(added));
            return remaining;
        }

        /// <summary>Adds amount units of data and returns the unaccepted remainder.</summary>
        public int AddItem(ItemDefinition data, int amount) => AddItem(new ItemStack(data, amount));

        /// <summary>Removes amount units from matching slots, starting at the end. All-or-nothing.</summary>
        public bool RemoveItem(ItemDefinition data, int amount)
        {
            EnsureSlots();
            if (data == null || amount <= 0 || CountItem(data) < amount) return false;

            var remaining = amount;
            for (var i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item.Data != data) continue;
                var take = Mathf.Min(remaining, slot.Item.Amount);
                remaining -= take;
                var left = slot.Item.Amount - take;
                if (left <= 0) slot.Clear();
                else slot.SetItem(new ItemStack(data, left));
            }

            OnItemRemoved?.Invoke(new ItemStack(data, amount));
            return true;
        }

        /// <summary>Removes up to amount units from one slot. Amount is clamped to the slot quantity.</summary>
        public bool RemoveFromSlot(int index, int amount)
        {
            if (!IsValidIndex(index)) return false;
            var slot = slots[index];
            if (slot.IsEmpty || amount <= 0) return false;
            amount = Mathf.Min(amount, slot.Item.Amount);
            var removed = new ItemStack(slot.Item.Data, amount);
            var left = slot.Item.Amount - amount;
            if (left <= 0) slot.Clear();
            else slot.SetItem(slot.Item.WithAmount(left));
            OnItemRemoved?.Invoke(removed);
            return true;
        }

        /// <summary>Removes every stack of a definition and returns the total units removed.</summary>
        public int RemoveAll(ItemDefinition data)
        {
            EnsureSlots();
            if (data == null) return 0;
            var removed = 0;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.Item.Data != data) continue;
                removed += slot.Item.Amount;
                slot.Clear();
            }
            if (removed > 0) OnItemRemoved?.Invoke(new ItemStack(data, removed));
            return removed;
        }

        /// <summary>Clears one slot and raises OnItemRemoved when it contained an item.</summary>
        public void ClearSlot(int index)
        {
            if (!IsValidIndex(index)) return;
            var slot = slots[index];
            if (slot.IsEmpty) return;
            var removed = slot.Item;
            slot.Clear();
            OnItemRemoved?.Invoke(removed);
        }

        /// <summary>Clears every slot and raises OnItemRemoved once for each previous stack.</summary>
        public void Clear()
        {
            EnsureSlots();
            var removed = new List<ItemStack>();
            foreach (var slot in slots)
            {
                if (slot.IsEmpty) continue;
                removed.Add(slot.Item);
                slot.Clear();
            }
            foreach (var stack in removed) OnItemRemoved?.Invoke(stack);
        }

        #endregion

        #region Move / Split / Swap

        /// <summary>Moves to an empty slot, merges compatible stacks, or swaps incompatible stacks. Unmerged remainder stays in source.</summary>
        public bool TryMove(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return false;
            var from = slots[fromIndex];
            var to = slots[toIndex];
            if (from.IsEmpty) return false;

            if (to.IsEmpty)
            {
                to.SetItem(from.Item);
                from.Clear();
                return true;
            }

            if (from.Item.CanStackWith(to.Item))
            {
                var original = from.Item.Amount;
                var remainder = to.TryAdd(from.Item);
                if (remainder == 0) from.Clear();
                else if (remainder != original) from.SetItem(from.Item.WithAmount(remainder));
                return remainder != original;
            }

            from.SwapWith(to);
            return true;
        }

        /// <summary>Moves to the first compatible partial stack, or otherwise to the first empty slot. A merge may be partial.</summary>
        public bool MoveToFirstAvailable(int fromIndex)
        {
            if (!IsValidIndex(fromIndex)) return false;
            var from = slots[fromIndex];
            if (from.IsEmpty) return false;

            for (var i = 0; i < slots.Count; i++)
            {
                if (i == fromIndex) continue;
                var slot = slots[i];
                if (slot.IsEmpty || !slot.Item.CanStackWith(from.Item)) continue;
                if (slot.Item.Amount >= slot.Item.MaxStackSize) continue;
                return TryMove(fromIndex, i);
            }

            var empty = FindEmptySlot();
            return empty >= 0 && empty != fromIndex && TryMove(fromIndex, empty);
        }

        /// <summary>Splits part of a stack into the first empty slot.</summary>
        public bool SplitStack(int fromIndex, int amount)
        {
            var empty = FindEmptySlot();
            return empty >= 0 && SplitStack(fromIndex, empty, amount);
        }

        /// <summary>Splits amount units into a specific empty slot; the source must retain at least one unit.</summary>
        public bool SplitStack(int fromIndex, int toIndex, int amount)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return false;
            var from = slots[fromIndex];
            var to = slots[toIndex];
            if (from.IsEmpty || !to.IsEmpty || amount <= 0 || amount >= from.Item.Amount) return false;
            to.SetItem(new ItemStack(from.Item.Data, amount));
            from.SetItem(from.Item.WithAmount(from.Item.Amount - amount));
            return true;
        }

        /// <summary>Swaps two slots, including empty slots.</summary>
        public bool SwapSlots(int a, int b)
        {
            if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return false;
            slots[a].SwapWith(slots[b]);
            return true;
        }

        /// <summary>Transfers up to amount units to target. Target accepts as much as possible; only accepted units are removed here.</summary>
        /// <returns>True when at least one unit was transferred.</returns>
        public bool TransferTo(InventoryController target, int fromIndex, int amount)
        {
            if (target == null || target == this || !IsValidIndex(fromIndex) || amount <= 0) return false;
            var from = slots[fromIndex];
            if (from.IsEmpty) return false;

            var requested = Mathf.Min(amount, from.Item.Amount);
            var data = from.Item.Data;
            var remainder = target.AddItem(new ItemStack(data, requested));
            var moved = requested - remainder;
            if (moved <= 0) return false;

            var left = from.Item.Amount - moved;
            if (left <= 0) from.Clear();
            else from.SetItem(from.Item.WithAmount(left));
            OnItemRemoved?.Invoke(new ItemStack(data, moved));
            return true;
        }

        #endregion

        #region Utilities

        /// <summary>Fills compatible partial stacks from later slots. Slot OnItemChanged events are raised for each changed slot.</summary>
        public void AutoStack()
        {
            EnsureSlots();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item.Amount >= slot.Item.MaxStackSize) continue;
                for (var j = i + 1; j < slots.Count; j++)
                {
                    var source = slots[j];
                    if (source.IsEmpty || !slot.Item.CanStackWith(source.Item)) continue;
                    var original = source.Item.Amount;
                    var remainder = slot.TryAdd(source.Item);
                    if (remainder == 0) source.Clear();
                    else if (remainder != original) source.SetItem(source.Item.WithAmount(remainder));
                    if (slot.Item.Amount >= slot.Item.MaxStackSize) break;
                }
            }
        }

        #endregion
    }
}
