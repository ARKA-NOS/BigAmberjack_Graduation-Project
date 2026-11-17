using _Scripts.Item;
using _Scripts.Item.ItemTooltip;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 클릭으로 고른 칸의 아이템을 창 아래 상세 정보 패널에 보여준다. 다른 칸을 고를 때까지 고정된 자리에 계속 남는다.
    // 고른 칸의 아이템이 바뀌면(드래그로 맞바꿈, 사용 등) 내용도 따라 바뀌고, 칸이 비면 안내 문구를 보여준다.
    public class InventoryDetailController : MonoBehaviour
    {
        [SerializeField] private ItemTooltipView view;
        [Tooltip("고른 아이템이 없을 때 보여줄 안내 문구. 없으면 비워 둔다.")]
        [SerializeField] private GameObject emptyHint;

        private InventorySlotUI _selected;
        private ItemSO _shownItem;

        private void Awake()
        {
            Show(null);
        }

        public void Select(InventorySlotUI slot)
        {
            _selected = slot;
            Refresh();
        }

        private void Update()
        {
            if (_selected != null) Refresh();
        }

        private void Refresh()
        {
            ItemSO item = _selected != null && _selected.isActiveAndEnabled ? _selected.Item : null;
            if (item == _shownItem) return;

            Show(item);
        }

        private void Show(ItemSO item)
        {
            _shownItem = item;
            view.Show(item);
            if (emptyHint != null) emptyHint.SetActive(item == null);
        }
    }
}
