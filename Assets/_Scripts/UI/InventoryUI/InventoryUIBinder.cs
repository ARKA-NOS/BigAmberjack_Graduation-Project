using System.Collections.Generic;
using _Scripts.Inventory;
using _Scripts.UI.BindSystem;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 장착 또는 보관 칸 묶음 하나를 표시한다. 칸 UI는 slotParent의 자식을 쓰고, 최대 칸 수보다 적으면 template를 복제해 늘린다.
    public class InventoryUIBinder : AbstractUIBinder<Inventory.Inventory>
    {
        [SerializeField] private InventorySlotType target;
        [SerializeField] private Transform slotParent;
        [SerializeField] private InventorySlotUI template;

        private readonly List<InventorySlotUI> _slots = new();

        protected override void Awake()
        {
            base.Awake();
            slotParent.GetComponentsInChildren(true, _slots);
        }

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
            Inventory.Inventory inventory = CurrentValue;
            if (inventory == null) return;

            ItemContainer container = inventory.Get(target);

            // 최대 칸 수만큼 모두 보여주고, 아직 열리지 않은 칸(Capacity 이후)은 잠긴 칸으로 표시한다.
            while (_slots.Count < container.MaxCapacity && template != null)
                _slots.Add(Instantiate(template, slotParent));

            for (int i = 0; i < _slots.Count; i++)
            {
                bool shown = i < container.MaxCapacity;
                _slots[i].gameObject.SetActive(shown);
                if (!shown) continue;

                _slots[i].Setup(target, i);
                bool unlocked = i < container.Capacity;
                _slots[i].SetLocked(!unlocked);
                if (unlocked) _slots[i].SetItem(container[i]);
            }
        }
    }
}
