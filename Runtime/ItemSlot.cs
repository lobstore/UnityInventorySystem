using System;
using UnityEngine;
using UnityEngine.Events;

namespace LOGIYGames.Inventory
{
    [Serializable]
    public class ItemSlot
    {
        [SerializeField] private ItemStack item;

        // readonly убран: Unity не сериализует readonly-поля,
        // значит нельзя было бы навесить persistent-листенеры в инспекторе.
        public UnityEvent OnItemChanged = new();

        public ItemStack Item => item;
        public bool IsEmpty => item.IsEmpty;

        public void SetItem(ItemStack stack)
        {
            var value = stack.IsEmpty ? default : stack;
            if (item.Data == value.Data && item.Amount == value.Amount) return;
            item = value;
            OnItemChanged?.Invoke();
        }

        public void Clear() => SetItem(default);

        /// <summary>Возвращает остаток, который не поместился в слот.</summary>
        public int TryAdd(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;

            if (IsEmpty)
            {
                int added = Mathf.Min(stack.Amount, stack.MaxStackSize);
                SetItem(new ItemStack(stack.Data, added));
                return stack.Amount - added;
            }

            if (!item.CanStackWith(stack)) return stack.Amount;

            int space = item.MaxStackSize - item.Amount;
            if (space <= 0) return stack.Amount;

            int toAdd = Mathf.Min(space, stack.Amount);
            SetItem(new ItemStack(item.Data, item.Amount + toAdd));
            return stack.Amount - toAdd;
        }

        public void SwapWith(ItemSlot other)
        {
            if (other == null || ReferenceEquals(other, this)) return;
            (item, other.item) = (other.item, item);
            OnItemChanged?.Invoke();
            other.OnItemChanged?.Invoke();
        }
    }
}