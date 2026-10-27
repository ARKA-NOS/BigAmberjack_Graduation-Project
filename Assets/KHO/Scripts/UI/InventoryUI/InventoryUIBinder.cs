using KHO.Scripts.InventorySystem;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace KHO.Scripts.UI.InventoryUI
{
    public class InventoryUIBinder : AbstractUIBinder<Inventory>
    {
        [SerializeField] private InventorySlotUI[] slots = new InventorySlotUI[Inventory.SlotCount];

        private void Start()
        {
            RefreshSlots();
        }

        // 인벤토리는 바뀔 때만 다시 그리면 되므로 매 프레임 할 일은 없다.
        protected override void DataUpdate()
        {
        }

        protected override void OnDataChanged()
        {
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            Inventory inventory = CurrentValue;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                bool inRange = inventory != null && i < inventory.Slots.Count;
                slots[i].SetItem(inRange ? inventory.Slots[i] : null);
            }
        }
    }
}
