using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LOGIYGames.Inventory
{
    public class ItemSlotView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerClickHandler
    {
        [SerializeField] ItemView ItemView;

        ItemSlotPresenter presenter;
        ItemSlot ItemSlot;

        public ItemSlotPresenter Presenter => presenter;

        public event Action<ItemSlotView, PointerEventData> BeginDragEvent;
        public event Action<ItemSlotView, PointerEventData> DragEvent;
        public event Action<ItemSlotView, PointerEventData> EndDragEvent;
        public event Action<ItemSlotView, PointerEventData> DropEvent;
        public event Action<ItemSlotView, PointerEventData> RightClickEvent;
        public event Action<ItemSlotView, PointerEventData> LeftClickEvent;

        public void Bind(ItemSlotPresenter presenter)
        {
            this.presenter = presenter;
            ItemSlot = presenter.Slot;
            UpdateView();
            presenter.Slot.OnItemChanged.AddListener(UpdateView);
        }

        private void OnDestroy() => Unbind();

        public void Unbind()
        {
            if (presenter?.Slot != null)
                presenter.Slot.OnItemChanged.RemoveListener(UpdateView);
        }

        private void UpdateView() => ItemView.SetItem(ItemSlot.Item);

        public void OnBeginDrag(PointerEventData e)
        {
            if (ItemSlot == null || ItemSlot.IsEmpty)
            {
                e.pointerDrag = null;
                return;
            }
            BeginDragEvent?.Invoke(this, e);
        }

        public void OnDrag(PointerEventData e) => DragEvent?.Invoke(this, e);
        public void OnEndDrag(PointerEventData e) => EndDragEvent?.Invoke(this, e);
        public void OnDrop(PointerEventData e) => DropEvent?.Invoke(this, e);

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            if (e.button == PointerEventData.InputButton.Left)
                LeftClickEvent?.Invoke(this, e);
            else if (e.button == PointerEventData.InputButton.Right)
                RightClickEvent?.Invoke(this, e);
        }
    }
}