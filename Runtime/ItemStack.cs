using System;
using UnityEngine;

namespace LOGIYGames.Inventory
{
    [Serializable]
    public struct ItemStack
    {
        public ItemDefinition Data;
        [Min(0)] public int Amount;

        public ItemStack(ItemDefinition data, int amount)
        {
            Data = data;
            Amount = Mathf.Max(0, amount);
        }

        public bool IsEmpty => Data == null || Amount <= 0;
        public int MaxStackSize => Data != null ? Mathf.Max(1, Data.MaxStackSize) : 1;

        public bool CanStackWith(ItemStack other) =>
            !IsEmpty && !other.IsEmpty && Data == other.Data;

        public ItemStack Clone() => new(Data, Amount);
        public ItemStack WithAmount(int amount) => new(Data, amount);
    }
}