using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using _Scripts.Item;
using UnityEngine;

namespace _Scripts.Interactable.Chest.Effects
{
    // 아이템 보상 효과. ItemSO는 ChoiceEffectSO가 아니라서 이 효과로 감싸 상자의 Rewards나 NPC 선택지 Effects에 넣는다.
    // 위치가 주어지면(상자) 인벤토리에 바로 넣지 않고 그 위치에서 아이템 픽업을 튀어나오게 한다. 위치가 없으면(NPC) 바로 지급한다.
    [CreateAssetMenu(fileName = "Effect_Item", menuName = "Chest/Effect/Item Reward", order = 2)]
    public class ItemRewardEffectSO : ChoiceEffectSO, IOriginEffect
    {
        [Tooltip("지급할 아이템")]
        [SerializeField] private ItemSO item;
        [Tooltip("상자에서 꺼낼 때 생성할 아이템 픽업 프리팹")]
        [SerializeField] private ItemPickup pickupPrefab;

        [Tooltip("생성한 픽업의 크기 배율. 스프라이트는 건드리지 않고 오브젝트 스케일만 키운다.")]
        [SerializeField, Min(0.1f)] private float pickupScale = 5f;

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
                Debug.Log($"[Chest] 아이템 획득: {item.ItemName}");
            else
                Debug.LogWarning($"[Chest] 아이템 획득 실패(인벤토리 가득 참): {item.ItemName}");
        }

        public void Apply(ModuleOwner owner, Vector3 origin)
        {
            if (pickupPrefab == null)
            {
                Apply(owner);
                return;
            }

            ItemPickup pickup = Instantiate(pickupPrefab, origin, Quaternion.identity);
            pickup.transform.localScale *= pickupScale;
            pickup.Init(item);
            ItemPopMotion.Play(pickup.gameObject, item.Icon);
            Debug.Log($"[Chest] 아이템 드랍: {item.ItemName}");
        }
    }
}
