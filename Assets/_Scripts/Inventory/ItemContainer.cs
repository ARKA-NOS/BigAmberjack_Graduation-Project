using System.Collections.Generic;
using _Scripts.Item;
using UnityEngine;

namespace _Scripts.Inventory
{
    // 칸 묶음 하나. 최대 칸 수 안에서 사용 가능한 칸(Capacity)을 늘릴 수 있다.
    public class ItemContainer
    {
        private readonly ItemInstance[] _slots;

        public int Capacity { get; private set; }
        public int MaxCapacity => _slots.Length;
        public bool IsFull => FindEmptySlot() < 0;

        public ItemContainer(int capacity, int maxCapacity)
        {
            _slots = new ItemInstance[maxCapacity];
            Capacity = Mathf.Clamp(capacity, 0, maxCapacity);
        }

        public ItemInstance this[int slot] => _slots[slot];

        public bool IsValidSlot(int slot) => slot >= 0 && slot < Capacity;

        public int FindEmptySlot()
        {
            for (int i = 0; i < Capacity; i++)
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

        // 사용 가능한 칸을 amount만큼 늘린다. 실제로 늘어난 칸 수를 돌려준다.
        public int Expand(int amount)
        {
            int prev = Capacity;
            Capacity = Mathf.Clamp(Capacity + amount, 0, MaxCapacity);
            return Capacity - prev;
        }

        public IEnumerable<ItemInstance> Items()
        {
            for (int i = 0; i < Capacity; i++)
                yield return _slots[i];
        }
    }
}
