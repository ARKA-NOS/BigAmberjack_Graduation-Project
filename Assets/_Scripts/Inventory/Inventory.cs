using System.Collections;
using System.Collections.Generic;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
    // 장착 칸(효과 적용, 최대 9칸까지 늘어남)과 보관 칸(효과 없음, 20칸)을 가진다.
    public class Inventory
    {
        public const int EquipmentMaxCapacity = 9;
        public const int StorageCapacity = 20;

        // 통합 칸 번호 기준의 최대 칸 수(장착 최대 + 보관).
        public const int SlotCount = EquipmentMaxCapacity + StorageCapacity;

        public ItemContainer Equipment { get; }
        public ItemContainer Storage { get; }

        // 장착 칸(현재 Capacity만큼) 뒤에 보관 칸이 이어지는 통합 목록. 통합 칸 번호는 이 목록의 인덱스다.
        public IReadOnlyList<ItemInstance> Slots { get; }

        public bool IsFull => Equipment.IsFull && Storage.IsFull;

        public Inventory(int equipmentCapacity)
        {
            Equipment = new ItemContainer(equipmentCapacity, EquipmentMaxCapacity);
            Storage = new ItemContainer(StorageCapacity, StorageCapacity);
            Slots = new CombinedSlots(this);
        }

        public ItemContainer Get(InventorySlotType type)
            => type == InventorySlotType.Equipment ? Equipment : Storage;

        // 통합 칸 번호를 (장착/보관, 칸 번호)로 바꾼다.
        public bool TryResolve(int index, out InventorySlotType type, out int slot)
        {
            if (index >= 0 && index < Equipment.Capacity)
            {
                type = InventorySlotType.Equipment;
                slot = index;
                return true;
            }

            type = InventorySlotType.Storage;
            slot = index - Equipment.Capacity;
            return Storage.IsValidSlot(slot);
        }

        private class CombinedSlots : IReadOnlyList<ItemInstance>
        {
            private readonly Inventory _inventory;

            public CombinedSlots(Inventory inventory) => _inventory = inventory;

            public int Count => _inventory.Equipment.Capacity + _inventory.Storage.Capacity;

            public ItemInstance this[int index]
            {
                get
                {
                    if (!_inventory.TryResolve(index, out InventorySlotType type, out int slot))
                        throw new System.ArgumentOutOfRangeException(nameof(index));
                    return _inventory.Get(type)[slot];
                }
            }

            public IEnumerator<ItemInstance> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
