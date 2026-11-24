using System;
using UnityEngine;
using UnityEngine.Events;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Serializable inventory slot holding zero or one item stack.
    /// Every successful content mutation invokes <see cref="OnItemChanged"/>.
    /// </summary>
    [Serializable]
    public class ItemSlot
    {
        [SerializeField]
        private ItemStack item;

        /// <summary>Raised when the slot's definition or amount changes.</summary>
        public UnityEvent OnItemChanged = new UnityEvent();

        /// <summary>Current stack value. The returned struct is a copy.</summary>
        public ItemStack Item => item;

        /// <summary>True when the slot has no item or a non-positive amount.</summary>
        public bool IsEmpty => item.IsEmpty;

        /// <summary>
        /// Replaces the current stack. Null definitions and non-positive quantities clear the slot.
        /// Amounts greater than the definition's MaxStackSize are clamped to one slot's capacity.
        /// </summary>
        public void SetItem(ItemStack stack)
        {
            var value = stack.IsEmpty
                ? default
                : new ItemStack(stack.Data, Mathf.Min(stack.Amount, stack.MaxStackSize));

            if (item.Data == value.Data && item.Amount == value.Amount)
            {
                return;
            }

            item = value;
            OnItemChanged?.Invoke();
        }

        /// <summary>Clears this slot.</summary>
        public void Clear()
        {
            SetItem(default);
        }

        /// <summary>
        /// Adds as much of a stack as will fit in this slot.
        /// </summary>
        /// <param name="stack">Stack to add.</param>
        /// <returns>Quantity that did not fit. Returns zero for an empty input stack.</returns>
        public int TryAdd(ItemStack stack)
        {
            if (stack.IsEmpty)
            {
                return 0;
            }

            if (IsEmpty)
            {
                var added = Mathf.Min(stack.Amount, stack.MaxStackSize);
                SetItem(new ItemStack(stack.Data, added));
                return stack.Amount - added;
            }

            if (!item.CanStackWith(stack))
            {
                return stack.Amount;
            }

            var space = Mathf.Max(0, item.MaxStackSize - item.Amount);
            if (space == 0)
            {
                return stack.Amount;
            }

            var toAdd = Mathf.Min(space, stack.Amount);
            SetItem(new ItemStack(item.Data, item.Amount + toAdd));
            return stack.Amount - toAdd;
        }

        /// <summary>Exchanges the contents with another slot and notifies both slots.</summary>
        /// <param name="other">Slot to exchange contents with. Null and self references are ignored.</param>
        public void SwapWith(ItemSlot other)
        {
            if (other == null || ReferenceEquals(other, this))
            {
                return;
            }

            var ownStack = item;
            var otherStack = other.item;
            SetItem(otherStack);
            other.SetItem(ownStack);
        }
    }
}
