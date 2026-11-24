using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Owns the runtime slot views and connects their pointer events to an InventoryController.
    /// Dispose this factory when the displayed inventory changes or the UI is destroyed.
    /// </summary>
    public class InventoryUIFactory
    {
        private readonly List<ItemSlotPresenter> slotsPresenters = new List<ItemSlotPresenter>();
        private readonly List<ItemSlotView> slotsViews = new List<ItemSlotView>();
        private readonly GameObject inventorySlotPrefab;
        private readonly RectTransform inventoryContainer;
        private readonly Image dragGhost;
        private readonly TMP_Text dragGhostAmount;
        private readonly ItemContextMenu contextMenu;
        private readonly InventoryController inventory;

        private ItemSlotPresenter dragSource;

        /// <summary>Creates a UI factory for an inventory and its required/optional UI references.</summary>
        public InventoryUIFactory(
            InventoryController inventory,
            RectTransform inventoryContainer,
            GameObject inventorySlotPrefab,
            Image dragGhost = null,
            TMP_Text dragGhostAmount = null,
            ItemContextMenu contextMenu = null)
        {
            this.inventory = inventory;
            this.inventorySlotPrefab = inventorySlotPrefab;
            this.inventoryContainer = inventoryContainer;
            this.dragGhost = dragGhost;
            this.dragGhostAmount = dragGhostAmount;
            this.contextMenu = contextMenu;
        }

        /// <summary>Recreates one view for each inventory slot.</summary>
        public void Create()
        {
            Clear();
            if (inventory == null || inventoryContainer == null || inventorySlotPrefab == null)
            {
                Debug.LogWarning("InventoryUIFactory requires an inventory, slot container, and slot prefab.");
                return;
            }

            for (var i = 0; i < inventory.Slots.Count; i++)
            {
                var instance = Object.Instantiate(inventorySlotPrefab, inventoryContainer, false);
                instance.transform.localScale = Vector3.one;

                var view = instance.GetComponent<ItemSlotView>();
                if (view == null)
                {
                    Debug.LogError("Inventory slot prefab is missing ItemSlotView.", instance);
                    Object.Destroy(instance);
                    continue;
                }

                var presenter = new ItemSlotPresenter(view, inventory.Slots[i], i);
                presenter.Bind(inventory);
                HookView(view);
                slotsViews.Add(view);
                slotsPresenters.Add(presenter);
            }
        }

        /// <summary>Unhooks and destroys all views owned by this factory and clears optional UI elements.</summary>
        public void Dispose()
        {
            ClearGhost();
            contextMenu?.Hide();
            Clear();
        }

        private void Clear()
        {
            foreach (var view in slotsViews)
            {
                if (view == null) continue;
                UnhookView(view);
                view.Unbind();
                Object.Destroy(view.gameObject);
            }

            slotsViews.Clear();
            slotsPresenters.Clear();
            dragSource = null;
        }

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

        private void OnBeginDrag(ItemSlotView view, PointerEventData eventData)
        {
            contextMenu?.Hide();
            dragSource = view != null ? view.Presenter : null;
            if (dragSource == null) return;
            ShowGhost(dragSource.Slot.Item);
            MoveGhost(eventData.position);
        }

        private void OnDrag(ItemSlotView view, PointerEventData eventData)
        {
            MoveGhost(eventData.position);
        }

        private void OnEndDrag(ItemSlotView view, PointerEventData eventData)
        {
            ClearGhost();
            dragSource = null;
        }

        private void OnDrop(ItemSlotView view, PointerEventData eventData)
        {
            var target = view != null ? view.Presenter : null;
            if (dragSource == null || target == null || dragSource == target) return;
            dragSource.TryMoveTo(target);
        }

        private void OnRightClick(ItemSlotView view, PointerEventData eventData)
        {
            if (contextMenu == null || view == null || view.Presenter == null) return;
            var actions = view.Presenter.BuildContextActions();
            if (actions.Count > 0) contextMenu.Show(eventData.position, actions);
        }

        private void ShowGhost(ItemStack stack)
        {
            if (dragGhost == null || stack.IsEmpty) return;
            dragGhost.enabled = true;
            dragGhost.sprite = stack.Data.Icon;
            if (dragGhostAmount != null)
                dragGhostAmount.text = stack.Amount > 1 ? stack.Amount.ToString() : string.Empty;
        }

        private void MoveGhost(Vector2 position)
        {
            if (dragGhost == null || !dragGhost.enabled) return;
            dragGhost.rectTransform.position = position;
        }

        private void ClearGhost()
        {
            if (dragGhost != null)
            {
                dragGhost.enabled = false;
                dragGhost.sprite = null;
            }

            if (dragGhostAmount != null) dragGhostAmount.text = string.Empty;
        }
    }
}
