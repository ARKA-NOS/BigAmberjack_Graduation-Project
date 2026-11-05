using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.UI.InventoryUI
{
    // 칸 하나의 마우스 올림/벗어남을 부모의 InventoryTooltipController로 넘긴다. 끌기 시작하면 툴팁을 닫는다.
    [RequireComponent(typeof(InventorySlotUI))]
    public class InventorySlotHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
    {
        private InventorySlotUI _slot;
        private InventoryTooltipController _controller;

        private void Awake()
        {
            _slot = GetComponent<InventorySlotUI>();
            _controller = GetComponentInParent<InventoryTooltipController>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_controller != null && !eventData.dragging) _controller.Enter(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_controller != null) _controller.Exit(_slot);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_controller != null) _controller.Hide();
        }
    }
}
