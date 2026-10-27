using KHO.Scripts.ItemSystem;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.InventoryUI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;

        public void SetItem(ItemInstance item)
        {
            bool hasItem = item != null && item.Data.Icon != null;

            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? item.Data.Icon : null;
        }
    }
}
