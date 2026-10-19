using System;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
    public interface IInventoryModule
    {
        // 슬롯 번호. 바뀐 결과는 Inventory.Slots[slot]에서 읽는다.
        event Action<int> OnSlotChanged;

        Inventory Inventory { get; }

        // 전투 중 변경 차단용. true면 획득·버리기·교체가 모두 실패한다.
        bool IsLocked { get; set; }

        // 가득 찼거나 잠겨 있으면 false. 가득 찬 경우 호출한 쪽에서 교체 UI를 띄운다.
        bool TryAddItem(ItemSO item);
        bool TryRemoveItem(int slot);
        bool TryReplaceItem(int slot, ItemSO item);
    }
}
