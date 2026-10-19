using System.Collections.Generic;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
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

        public ItemInstance Set(int slot, ItemInstance item)
        {
            ItemInstance prev = _slots[slot];
            _slots[slot] = item;
            return prev;
        }
    }
}
