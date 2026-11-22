using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    public class InventoryUIFactory
    {
        private readonly List<ItemSlotPresenter> slotsPresenters = new();
        private readonly List<ItemSlotView> slotsViews = new();
        private readonly GameObject inventorySlotPrefab;
        private readonly RectTransform inventoryContainer;
        private readonly Image dragGhost;
        private readonly TMP_Text dragGhostAmount;
        private readonly ItemContextMenu contextMenu;
        private readonly InventoryController Inventory;

        private ItemSlotPresenter dragSource;

        public InventoryUIFactory(
            InventoryController inventory,
            RectTransform inventoryContainer,
            GameObject inventorySlotPrefab,
            Image dragGhost = null,
            TMP_Text dragGhostAmount = null,
            ItemContextMenu contextMenu = null)
        {
            Inventory = inventory;
            this.inventorySlotPrefab = inventorySlotPrefab;
            this.inventoryContainer = inventoryContainer;
            this.dragGhost = dragGhost;
            this.dragGhostAmount = dragGhostAmount;
            this.contextMenu = contextMenu;
        }

        public void Create()
        {
            Clear();
            if (Inventory == null || Inventory.Slots.Count == 0) return;

            for (int i = 0; i < Inventory.Slots.Count; i++)
            {
                var obj = GameObject.Instantiate(inventorySlotPrefab, inventoryContainer, false);
                obj.transform.localScale = Vector3.one;

                var view = obj.GetComponent<ItemSlotView>();
                if (view == null)
                {
                    Debug.LogError("InventorySlotPrefab missing ItemSlotView", obj);
                    continue;
                }

                var presenter = new ItemSlotPresenter(view, Inventory.Slots[i], i);
                presenter.Bind(Inventory);
                HookView(view);

                slotsViews.Add(view);
                slotsPresenters.Add(presenter);
            }
        }

        public void Dispose()
        {
            ClearGhost();
            contextMenu?.Hide();
            Clear();
        }

        private void Clear()
        {
            foreach (var view in slotsViews)
                if (view != null) UnhookView(view);

            slotsViews.Clear();
            slotsPresenters.Clear();
            dragSource = null;

            if (inventoryContainer == null) return;
            for (int i = inventoryContainer.childCount - 1; i >= 0; i--)
                GameObject.Destroy(inventoryContainer.GetChild(i).gameObject);
        }

        // ---------- view events ----------

        private void HookView(ItemSlotView view)
        {
            view.BeginDragEvent += OnBeginDrag;
            view.DragEvent += OnDrag;
            view.EndDragEvent += OnEndDrag;
            view.DropEvent += OnDrop;
            view.RightClickEvent += OnRightClick;
        }

        private void UnhookView(ItemSlotView view)
        {
            view.BeginDragEvent -= OnBeginDrag;
            view.DragEvent -= OnDrag;
            view.EndDragEvent -= OnEndDrag;
            view.DropEvent -= OnDrop;
            view.RightClickEvent -= OnRightClick;
        }

        private void OnBeginDrag(ItemSlotView view, PointerEventData e)
        {
            contextMenu?.Hide();
            dragSource = view.Presenter;
            ShowGhost(view.Presenter.Slot.Item);
            MoveGhost(e.position);
        }

        private void OnDrag(ItemSlotView view, PointerEventData e) => MoveGhost(e.position);

        private void OnEndDrag(ItemSlotView view, PointerEventData e)
        {
            ClearGhost();
            dragSource = null;
        }

        private void OnDrop(ItemSlotView view, PointerEventData e)
        {
            var target = view.Presenter;
            if (dragSource == null || target == null || dragSource == target) return;
            dragSource.TryMoveTo(target);
        }

        private void OnRightClick(ItemSlotView view, PointerEventData e)
        {
            if (contextMenu == null) return;
            var actions = view.Presenter.BuildContextActions();
            if (actions.Count == 0) return;
            contextMenu.Show(e.position, actions);
        }

        // ---------- drag ghost ----------

        private void ShowGhost(ItemStack stack)
        {
            if (dragGhost == null || stack.IsEmpty) return;
            dragGhost.enabled = true;
            dragGhost.sprite = stack.Data.Icon;
            if (dragGhostAmount != null)
                dragGhostAmount.text = stack.Amount > 1 ? stack.Amount.ToString() : string.Empty;
        }

        private void MoveGhost(Vector2 pos)
        {
            if (dragGhost == null || !dragGhost.enabled) return;
            dragGhost.rectTransform.position = pos;
        }

        private void ClearGhost()
        {
            if (dragGhost != null) dragGhost.enabled = false;
            if (dragGhostAmount != null) dragGhostAmount.text = string.Empty;
        }
    }
}