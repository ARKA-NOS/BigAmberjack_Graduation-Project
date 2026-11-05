using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KHO.Scripts.UI.InventoryUI
{
    // 칸 하나의 드래그 입력을 부모의 InventoryDragController로 넘긴다.
    // 빈 칸을 끌면 아이템 대신 칸 목록을 스크롤한다.
    [RequireComponent(typeof(InventorySlotUI))]
    public class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventorySlotUI _slot;
        private InventoryDragController _controller;
        private ScrollRect _scroll;
        private bool _draggingItem;

        private void Awake()
        {
            _slot = GetComponent<InventorySlotUI>();
            _controller = GetComponentInParent<InventoryDragController>(true);
            _scroll = GetComponentInParent<ScrollRect>(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _draggingItem = _controller != null && _controller.BeginDrag(_slot, eventData);
            if (!_draggingItem && _scroll != null) _scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_draggingItem) _controller.Drag(eventData);
            else if (_scroll != null) _scroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggingItem) _controller.EndDrag();
            else if (_scroll != null) _scroll.OnEndDrag(eventData);

            _draggingItem = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_controller != null) _controller.Drop(_slot);
        }
    }
}
