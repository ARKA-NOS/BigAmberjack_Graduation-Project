using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using Lrw.Script.Player.InteractSystem;
using UnityEngine;

namespace KHO.Scripts.ItemSystem
{
    // 바닥에 놓인 아이템. 플레이어 InteractModule이 찾을 수 있도록 interactLayer 레이어의 트리거 콜라이더가 필요하다.
    public class ItemPickup : AbstractInteractObject
    {
        [SerializeField] private ItemSO item;
        [SerializeField] private SpriteRenderer iconRenderer;

        private void Start()
        {
            if (iconRenderer != null && item != null)
                iconRenderer.sprite = item.Icon;
        }

        public override void Interact(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IInventoryModule inventory)) return;

            // TODO : 가득 찬 경우 교체 UI 팝업 연결
            if (inventory.TryAddItem(item))
                Destroy(gameObject);
        }
    }
}
