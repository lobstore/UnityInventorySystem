using System;
using UnityEngine;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Value type representing a quantity of one <see cref="ItemDefinition"/>.
    /// An empty stack has no definition or a non-positive amount.
    /// Two stacks can merge only when they reference the same definition asset.
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        /// <summary>Definition asset for this stack. Null means the stack is empty.</summary>
        public ItemDefinition Data;

        /// <summary>Number of units in this stack.</summary>
        [Min(0)]
        public int Amount;

        /// <summary>Creates a stack; negative amounts are normalized to zero.</summary>
        /// <param name="data">Item definition.</param>
        /// <param name="amount">Requested quantity.</param>
        public ItemStack(ItemDefinition data, int amount)
        {
            Data = data;
            Amount = Mathf.Max(0, amount);
        }

        /// <summary>True when the definition is missing or the quantity is not positive.</summary>
        public bool IsEmpty => Data == null || Amount <= 0;

        /// <summary>Maximum quantity for this item definition, normalized to at least one.</summary>
        public int MaxStackSize => Data != null ? Mathf.Max(1, Data.MaxStackSize) : 1;

        /// <summary>Whether this stack can merge with another stack of the same definition asset.</summary>
        public bool CanStackWith(ItemStack other)
        {
            return !IsEmpty && !other.IsEmpty && Data == other.Data;
        }

        /// <summary>Returns a value copy of this stack.</summary>
        public ItemStack Clone() => new ItemStack(Data, Amount);

        /// <summary>Returns a new stack with the same definition and a different non-negative amount.</summary>
        public ItemStack WithAmount(int amount) => new ItemStack(Data, amount);

        /// <summary>Returns the canonical empty stack.</summary>
        public static ItemStack Empty => default;

        /// <inheritdoc />
        public override string ToString()
        {
            return IsEmpty ? "Empty" : $"{Data.name} x{Amount}";
        }
    }
}
