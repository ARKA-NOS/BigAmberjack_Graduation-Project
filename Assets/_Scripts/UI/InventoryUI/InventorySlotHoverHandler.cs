using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.UI.InventoryUI
{
    // 칸에 마우스를 올리거나 벗어나면 부모의 InventoryDetailController에 알린다.
    [RequireComponent(typeof(InventorySlotUI))]
    public class InventorySlotHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private InventorySlotUI _slot;
        private InventoryDetailController _controller;

        private void Awake()
        {
            _slot = GetComponent<InventorySlotUI>();
            _controller = GetComponentInParent<InventoryDetailController>(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_controller != null) _controller.BeginPreview(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_controller != null) _controller.EndPreview(_slot);
        }
    }
}
