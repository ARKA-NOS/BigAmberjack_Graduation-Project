using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Costs
{
    // 인벤토리의 아이템 하나를 소모하는 비용.
    [CreateAssetMenu(fileName = "Cost_Item", menuName = "Interact/Cost/Item Cost", order = 3)]
    public class ItemCostSO : ChoiceCostSO
    {
        [Tooltip("소모할 아이템")]
        [SerializeField] private ItemSO item;

        public override bool CanPay(ModuleOwner owner)
        {
            return item != null
                   && owner.TryGetModule(out IInventoryModule inventory)
                   && FindSlot(inventory) >= 0;
        }

        public override void Pay(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory))
                return;

            int slot = FindSlot(inventory);
            if (slot >= 0 && inventory.TryRemoveItem(slot))
                Debug.Log($"[Cost] 아이템 소모: {item.ItemName}");
        }

        private int FindSlot(IInventoryModule inventory)
        {
            for (int i = 0; i < inventory.Inventory.Slots.Count; i++)
            {
                if (inventory.Inventory.Slots[i]?.Data == item)
                    return i;
            }

            return -1;
        }
    }
}
