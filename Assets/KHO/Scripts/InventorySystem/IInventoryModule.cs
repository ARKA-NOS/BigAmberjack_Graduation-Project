using System;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
    public interface IInventoryModule
    {
        // 바뀐 칸. 결과는 Inventory.Get(type)[slot]에서 읽는다.
        event Action<InventorySlotType, int> OnSlotChanged;
        // 장착 칸 수가 늘어났을 때. 인자는 새 장착 칸 수.
        event Action<int> OnEquipmentCapacityChanged;

        Inventory Inventory { get; }

        // 전투 중 변경 차단용. true면 획득·버리기·교체·확장이 모두 실패한다.
        bool IsLocked { get; set; }

        // 장착 칸에 빈자리가 있으면 장착(효과 적용), 없으면 보관 칸에 넣는다. 둘 다 가득 찼거나 잠겨 있으면 false.
        bool TryAddItem(ItemSO item);

        bool TryRemoveItem(InventorySlotType type, int slot);
        bool TryReplaceItem(InventorySlotType type, int slot, ItemSO item);

        // 통합 칸 번호(Inventory.Slots 인덱스) 기준. 기존 호출부 호환용.
        bool TryRemoveItem(int index);
        bool TryReplaceItem(int index, ItemSO item);

        // 장착 칸을 amount만큼 늘린다(최대 Inventory.EquipmentMaxCapacity).
        bool TryExpandEquipment(int amount = 1);
    }
}
