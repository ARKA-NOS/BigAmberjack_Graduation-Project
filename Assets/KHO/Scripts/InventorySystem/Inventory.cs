using System.Collections.Generic;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
    // 슬롯 데이터만 다룬다. 효과 적용은 InventoryModule이 맡으므로, 씬을 넘어 유지해야 할 때 이 클래스만 옮기면 된다.
    public class Inventory
    {
        public const int SlotCount = 9;

        private readonly ItemInstance[] _slots = new ItemInstance[SlotCount];

        public IReadOnlyList<ItemInstance> Slots => _slots;
        public bool IsFull => FindEmptySlot() < 0;

        public int FindEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null) return i;
            }

            return -1;
        }

        public bool TryAdd(ItemInstance item, out int slot)
        {
            slot = FindEmptySlot();
            if (slot < 0) return false;

            _slots[slot] = item;
            return true;
        }

        // 해당 슬롯의 아이템을 바꾸고 이전 아이템을 돌려준다. item이 null이면 비운다.
        public ItemInstance Set(int slot, ItemInstance item)
        {
            ItemInstance prev = _slots[slot];
            _slots[slot] = item;
            return prev;
        }
    }
}
