using System;
using _Scripts.Item;

namespace _Scripts.UI.InventoryUI
{
    // 인벤토리 칸 하나의 표시 데이터. 아직 열리지 않은 칸(장착 칸 확장 전)은 Locked이고 아이템이 없다.
    public readonly struct InventorySlotData : IEquatable<InventorySlotData>
    {
        public static readonly InventorySlotData Empty = new(null, false);

        public ItemSO Item { get; }
        public bool Locked { get; }

        public InventorySlotData(ItemSO item, bool locked)
        {
            Item = item;
            Locked = locked;
        }

        public bool Equals(InventorySlotData other) => Item == other.Item && Locked == other.Locked;

        public override bool Equals(object obj) => obj is InventorySlotData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Item, Locked);
    }
}
