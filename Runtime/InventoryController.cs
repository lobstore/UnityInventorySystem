using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LOGIYGames.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private List<ItemSlot> slots = new();
        public UnityEvent<ItemStack> OnItemUsed = new();
        public UnityEvent<ItemStack> OnItemEquipped = new();
        public UnityEvent<ItemStack> OnItemSold = new();
        public UnityEvent<ItemStack> OnItemDropped = new();
        public UnityEvent<ItemStack> OnItemDiscarded = new();
        public IReadOnlyList<ItemSlot> Slots => slots;

        public UnityEvent<ItemStack> OnItemAdded = new();
        public UnityEvent<ItemStack> OnItemRemoved = new();

        #region Queries

        public bool IsValidIndex(int index) => index >= 0 && index < slots.Count;

        public ItemSlot GetSlot(int index) => IsValidIndex(index) ? slots[index] : null;

        public int FindSlot(ItemDefinition data)
        {
            if (data == null) return -1;
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].IsEmpty && slots[i].Item.Data == data) return i;
            return -1;
        }

        public int FindEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        public int CountItem(ItemDefinition data)
        {
            if (data == null) return 0;
            int total = 0;
            foreach (var slot in slots)
                if (!slot.IsEmpty && slot.Item.Data == data) total += slot.Item.Amount;
            return total;
        }

        public bool ContainsItem(ItemDefinition data, int amount = 1) => CountItem(data) >= amount;

        public bool IsFull => FindEmptySlot() < 0;

        /// <summary>Поместится ли стак целиком (с учётом частичных стаков того же предмета).</summary>
        public bool CanFit(ItemStack stack)
        {
            if (stack.IsEmpty) return true;

            int space = 0;
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

        /// <summary>Добавляет стак. Возвращает остаток, который не удалось уместить.</summary>
        public int AddItem(ItemStack stack)
        {
            AutoStack();
            if (stack.IsEmpty) return 0;
            int remaining = stack.Amount;

            // 1) Сначала добиваем существующие стаки.
            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot.IsEmpty || !slot.Item.CanStackWith(stack)) continue;
                if (slot.Item.Amount >= slot.Item.MaxStackSize) continue;
                remaining = slot.TryAdd(stack.WithAmount(remaining));
            }

            // 2) Потом кладём в пустые слоты.
            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (!slot.IsEmpty) continue;
                remaining = slot.TryAdd(stack.WithAmount(remaining));
            }

            int added = stack.Amount - remaining;
            if (added > 0) OnItemAdded?.Invoke(stack.WithAmount(added));
            return remaining;
        }

        public int AddItem(ItemDefinition data, int amount) => AddItem(new ItemStack(data, amount));

        /// <summary>Удаляет amount из любого слота (начиная с последних). Всё или ничего.</summary>
        public bool RemoveItem(ItemDefinition data, int amount)
        {
            if (data == null || amount <= 0) return false;
            if (CountItem(data) < amount) return false;

            int remaining = amount;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item.Data != data) continue;

                int take = Mathf.Min(remaining, slot.Item.Amount);
                remaining -= take;

                int left = slot.Item.Amount - take;
                if (left <= 0) slot.Clear();
                else slot.SetItem(new ItemStack(data, left));
            }

            OnItemRemoved?.Invoke(new ItemStack(data, amount - remaining));
            return true;
        }

        /// <summary>Удаляет из конкретного слота. Amount клэмпится по размеру стака.</summary>
        public bool RemoveFromSlot(int index, int amount)
        {
            if (!IsValidIndex(index)) return false;
            var slot = slots[index];
            if (slot.IsEmpty || amount <= 0) return false;

            amount = Mathf.Min(amount, slot.Item.Amount);
            var removed = new ItemStack(slot.Item.Data, amount);

            int left = slot.Item.Amount - amount;
            if (left <= 0) slot.Clear();
            else slot.SetItem(slot.Item.WithAmount(left));

            OnItemRemoved?.Invoke(removed);
            return true;
        }

        /// <summary>Удаляет все стаки предмета. Возвращает удалённое количество.</summary>
        public int RemoveAll(ItemDefinition data)
        {
            if (data == null) return 0;

            int removed = 0;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.Item.Data != data) continue;
                removed += slot.Item.Amount;
                slot.Clear();
            }

            if (removed > 0) OnItemRemoved?.Invoke(new ItemStack(data, removed));
            return removed;
        }

        public void ClearSlot(int index)
        {
            if (IsValidIndex(index) && !slots[index].IsEmpty)
                slots[index].Clear();
        }

        public void Clear()
        {
            foreach (var slot in slots)
                if (!slot.IsEmpty) slot.Clear();
        }

        #endregion

        #region Move / Split / Swap

        /// <summary>Перекладывает / сливает / меняет местами содержимое двух слотов.</summary>
        public bool TryMove(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return false;

            var from = slots[fromIndex];
            var to = slots[toIndex];
            if (from.IsEmpty) return false;

            // 1) Целевой пустой — просто переносим.
            if (to.IsEmpty)
            {
                to.SetItem(from.Item);
                from.Clear();
                return true;
            }

            // 2) Тот же предмет — сливаем, остаток остаётся в from.
            if (from.Item.CanStackWith(to.Item))
            {
                int original = from.Item.Amount;
                int remainder = to.TryAdd(from.Item);

                if (remainder == 0) from.Clear();
                else if (remainder != original) from.SetItem(from.Item.WithAmount(remainder));

                return remainder != original;
            }

            // 3) Разные предметы — свап.
            from.SwapWith(to);
            return true;
        }

        /// <summary>Пытается переложить слот в первый подходящий (долить в стак или в пустой).</summary>
        public bool MoveToFirstAvailable(int fromIndex)
        {
            if (!IsValidIndex(fromIndex)) return false;
            var from = slots[fromIndex];
            if (from.IsEmpty) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i == fromIndex) continue;
                var slot = slots[i];
                if (slot.IsEmpty || !slot.Item.CanStackWith(from.Item)) continue;
                if (slot.Item.Amount >= slot.Item.MaxStackSize) continue;
                return TryMove(fromIndex, i);
            }

            int empty = FindEmptySlot();
            return empty >= 0 && empty != fromIndex && TryMove(fromIndex, empty);
        }

        /// <summary>Делит стак в первый пустой слот.</summary>
        public bool SplitStack(int fromIndex, int amount)
        {
            int empty = FindEmptySlot();
            return empty >= 0 && SplitStack(fromIndex, empty, amount);
        }

        /// <summary>Делит стак в конкретный пустой слот.</summary>
        public bool SplitStack(int fromIndex, int toIndex, int amount)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return false;

            var from = slots[fromIndex];
            var to = slots[toIndex];
            if (from.IsEmpty || !to.IsEmpty) return false;
            if (amount <= 0 || amount >= from.Item.Amount) return false;

            to.SetItem(new ItemStack(from.Item.Data, amount));
            from.SetItem(from.Item.WithAmount(from.Item.Amount - amount));
            return true;
        }

        /// <summary>Прямой свап двух слотов без учёта типов (в т.ч. пустых).</summary>
        public bool SwapSlots(int a, int b)
        {
            if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return false;
            slots[a].SwapWith(slots[b]);
            return true;
        }

        /// <summary>Перекладывает amount из слота fromIndex в другой инвентарь.</summary>
        public bool TransferTo(InventoryController target, int fromIndex, int amount)
        {
            if (target == null || target == this) return false;
            if (!IsValidIndex(fromIndex)) return false;

            var from = slots[fromIndex];
            if (from.IsEmpty || amount <= 0) return false;

            amount = Mathf.Min(amount, from.Item.Amount);
            int remainder = target.AddItem(new ItemStack(from.Item.Data, amount));
            int moved = amount - remainder;
            if (moved <= 0) return false;

            int left = from.Item.Amount - moved;
            if (left <= 0) from.Clear();
            else from.SetItem(from.Item.WithAmount(left));
            return true;
        }

        #endregion

        #region Utilities

        /// <summary>Схлопывает частичные стаки одинаковых предметов.</summary>
        public void AutoStack()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Item.Amount >= slot.Item.MaxStackSize) continue;

                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[j].IsEmpty || !slot.Item.CanStackWith(slots[j].Item)) continue;

                    int original = slots[j].Item.Amount;
                    int remainder = slot.TryAdd(slots[j].Item);

                    if (remainder == 0) slots[j].Clear();
                    else if (remainder != original) slots[j].SetItem(slots[j].Item.WithAmount(remainder));

                    if (slot.Item.Amount >= slot.Item.MaxStackSize) break;
                }
            }
        }

        #endregion
    }
}
