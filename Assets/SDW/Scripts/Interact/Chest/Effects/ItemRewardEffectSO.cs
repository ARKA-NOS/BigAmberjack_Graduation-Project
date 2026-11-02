using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Chest.Effects
{
    // 아이템을 인벤토리에 지급하는 효과. ItemSO는 ChoiceEffectSO가 아니라서 이 효과로 감싸 상자의 Rewards나 NPC 선택지 Effects에 넣는다.
    [CreateAssetMenu(fileName = "Effect_Item", menuName = "Chest/Effect/Item Reward", order = 2)]
    public class ItemRewardEffectSO : ChoiceEffectSO, IOriginEffect
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
            TryGive(owner);
        }

        // 위치가 주어지면 아이템을 지급하고, 성공했을 때 그 위치에서 아이콘이 튀어나왔다 사라지는 연출을 낸다.
        public void Apply(ModuleOwner owner, Vector3 origin)
        {
            if (TryGive(owner))
                ItemPopupIcon.Spawn(item.Icon, origin);
        }

        private bool TryGive(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory))
                return false;

            if (inventory.TryAddItem(item))
            {
                Debug.Log($"[Chest] 아이템 획득: {item.ItemName}");
                return true;
            }

            Debug.LogWarning($"[Chest] 아이템 획득 실패(인벤토리 가득 참): {item.ItemName}");
            return false;
        }
    }
}
