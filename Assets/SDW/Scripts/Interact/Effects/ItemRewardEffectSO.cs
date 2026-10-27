using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Effects
{
    // 아이템을 인벤토리에 지급하는 효과. ItemSO는 ChoiceEffectSO가 아니라서 이 효과로 감싸 상자의 Rewards나 NPC 선택지 Effects에 넣는다.
    [CreateAssetMenu(fileName = "Effect_Item", menuName = "Interact/Effect/Item Reward", order = 2)]
    public class ItemRewardEffectSO : ChoiceEffectSO
    {
        [Tooltip("지급할 아이템")]
        [SerializeField] private ItemSO item;

        public override bool CanApply(ModuleOwner owner)
        {
            return item != null
                   && owner.TryGetModule(out IInventoryModule inventory)
                   && !inventory.Inventory.IsFull;
        }

        public override void Apply(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory))
                return;

            if (inventory.TryAddItem(item))
                Debug.Log($"[Reward] 아이템 획득: {item.ItemName}");
            else
                Debug.LogWarning($"[Reward] 아이템 획득 실패(인벤토리 가득 참): {item.ItemName}");
        }
    }
}
