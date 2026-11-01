using KHO.Scripts.ItemSystem;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.InventoryUI
{
    public class InventorySlotUI : MonoBehaviour
    {
        private const float LockedAlpha = 0.3f;

        [SerializeField] private Image iconImage;

        private CanvasGroup _canvasGroup;

        public void SetItem(ItemInstance item)
        {
            bool hasItem = item != null && item.Data.Icon != null;

            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? item.Data.Icon : null;
        }

        // 아직 열리지 않은 칸(장착 칸 확장 전)은 흐리게 보여준다.
        public void SetLocked(bool locked)
        {
            if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = locked ? LockedAlpha : 1f;
            if (locked) SetItem(null);
        }
    }
}
