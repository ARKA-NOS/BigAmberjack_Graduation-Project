using _Scripts.Item;
using _Scripts.Item.ItemTooltip;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 창 아래 상세 정보 패널에 아이템을 보여준다. 마우스를 올린 칸에 아이템이 있으면 그것을 미리 보여주고,
    // 아니면 클릭으로 고른 칸의 아이템을 보여준다. 보여줄 아이템이 없으면 안내 문구를 보여준다.
    // 칸의 아이템이 바뀌면(드래그로 맞바꿈, 사용 등) 내용도 따라 바뀐다.
    public class InventoryDetailController : MonoBehaviour
    {
        [SerializeField] private ItemTooltipView view;
        [Tooltip("고른 아이템이 없을 때 보여줄 안내 문구. 없으면 비워 둔다.")]
        [SerializeField] private GameObject emptyHint;
        [Tooltip("창의 CanvasGroup. 닫히면(interactable false) 미리보기를 지운다. 없으면 비워 둔다.")]
        [SerializeField] private CanvasGroup windowGroup;
        [Tooltip("끄는 동안에는 미리보기를 하지 않는다. 없으면 비워 둔다.")]
        [SerializeField] private InventoryDragController dragController;

        private InventorySlotUI _selected;
        private InventorySlotUI _hovered;
        private ItemSO _shownItem;

        private void Awake()
        {
            Show(null);
        }

        private void OnDisable()
        {
            _hovered = null;
        }

        public void Select(InventorySlotUI slot)
        {
            _selected = slot;
            Refresh();
        }

        public void BeginPreview(InventorySlotUI slot)
        {
            _hovered = slot;
            Refresh();
        }

        public void EndPreview(InventorySlotUI slot)
        {
            if (_hovered != slot) return;

            _hovered = null;
            Refresh();
        }

        private void Update()
        {
            if (windowGroup != null && !windowGroup.interactable) _hovered = null;

            Refresh();
        }

        private void Refresh()
        {
            bool dragging = dragController != null && dragController.IsDragging;
            ItemSO preview = dragging ? null : GetItem(_hovered);
            ItemSO item = preview != null ? preview : GetItem(_selected);
            if (item == _shownItem) return;

            Show(item);
        }

        private static ItemSO GetItem(InventorySlotUI slot)
            => slot != null && slot.isActiveAndEnabled ? slot.Item : null;

        private void Show(ItemSO item)
        {
            _shownItem = item;
            view.Show(item);
            if (emptyHint != null) emptyHint.SetActive(item == null);
        }
    }
}
