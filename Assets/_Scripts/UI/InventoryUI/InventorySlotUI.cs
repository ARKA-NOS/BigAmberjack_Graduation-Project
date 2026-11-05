using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.InventoryUI
{
    public class InventorySlotUI : MonoBehaviour
    {
        private const float LockedAlpha = 0.3f;
        private const float DraggingAlpha = 0.4f;

        [SerializeField] private Image iconImage;

        private CanvasGroup _canvasGroup;

        // 이 칸이 가리키는 인벤토리 위치. Binder가 정해 준다.
        public InventorySlotType Type { get; private set; }
        public int Index { get; private set; }
        public ItemInstance Item { get; private set; }
        public Image IconImage => iconImage;

        public void Setup(InventorySlotType type, int index)
        {
            Type = type;
            Index = index;
        }

        public void SetItem(ItemInstance item)
        {
            Item = item;
            bool hasItem = item != null && item.Data.Icon != null;

            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? item.Data.Icon : null;
            SetDragging(false);
        }

        // 아직 열리지 않은 칸(장착 칸 확장 전)은 흐리게 보여준다.
        public void SetLocked(bool locked)
        {
            if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = locked ? LockedAlpha : 1f;
            if (locked) SetItem(null);
        }

        // 끌고 있는 동안 원래 칸의 아이콘을 흐리게 한다.
        public void SetDragging(bool dragging)
        {
            Color color = iconImage.color;
            color.a = dragging ? DraggingAlpha : 1f;
            iconImage.color = color;
        }
    }
}
