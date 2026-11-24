using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LOGIYGames.Inventory
{
    /// <summary>
    /// Unity UI event adapter for an inventory slot. It forwards drag, drop and pointer events
    /// to its owner and refreshes the ItemView when the slot model changes.
    /// </summary>
    public class ItemSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerClickHandler
    {
        [SerializeField] private ItemView itemView;

        private ItemSlotPresenter presenter;
        private ItemSlot itemSlot;

        /// <summary>Presenter currently bound to this view.</summary>
        public ItemSlotPresenter Presenter => presenter;

        public event Action<ItemSlotView, PointerEventData> BeginDragEvent;
        public event Action<ItemSlotView, PointerEventData> DragEvent;
        public event Action<ItemSlotView, PointerEventData> EndDragEvent;
        public event Action<ItemSlotView, PointerEventData> DropEvent;
        public event Action<ItemSlotView, PointerEventData> RightClickEvent;
        public event Action<ItemSlotView, PointerEventData> LeftClickEvent;

        /// <summary>Binds to a presenter and subscribes to its slot content-change event.</summary>
        public void Bind(ItemSlotPresenter value)
        {
            Unbind();
            presenter = value;
            itemSlot = value != null ? value.Slot : null;

            if (itemSlot != null)
                itemSlot.OnItemChanged.AddListener(UpdateView);

            UpdateView();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>Unsubscribes from the current slot and clears local references.</summary>
        public void Unbind()
        {
            if (itemSlot != null)
                itemSlot.OnItemChanged.RemoveListener(UpdateView);

            itemSlot = null;
            presenter = null;
        }

        private void UpdateView()
        {
            if (itemView != null)
                itemView.SetItem(itemSlot != null ? itemSlot.Item : default);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemSlot == null || itemSlot.IsEmpty)
            {
                if (eventData != null) eventData.pointerDrag = null;
                return;
            }

            BeginDragEvent?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData) => DragEvent?.Invoke(this, eventData);
        public void OnEndDrag(PointerEventData eventData) => EndDragEvent?.Invoke(this, eventData);
        public void OnDrop(PointerEventData eventData) => DropEvent?.Invoke(this, eventData);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.dragging) return;
            if (eventData.button == PointerEventData.InputButton.Left)
                LeftClickEvent?.Invoke(this, eventData);
            else if (eventData.button == PointerEventData.InputButton.Right)
                RightClickEvent?.Invoke(this, eventData);
        }
    }
}
