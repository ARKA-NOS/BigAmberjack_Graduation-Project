using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.UI.InventoryUI
{
    // 칸 하나를 왼쪽 클릭하면 부모의 InventoryDetailController에 그 칸을 고른다고 알린다.
    // 끌어서 놓은 경우는 EventSystem이 클릭으로 치지 않으므로 드래그와 겹치지 않는다.
    [RequireComponent(typeof(InventorySlotUI))]
    public class InventorySlotClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private InventorySlotUI _slot;
        private InventoryDetailController _controller;

        private void Awake()
        {
            _slot = GetComponent<InventorySlotUI>();
            _controller = GetComponentInParent<InventoryDetailController>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_controller != null) _controller.Select(_slot);
        }
    }
}
