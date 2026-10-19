using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using Lrw.Script.Agent.Players.InteractSystem;
using UnityEngine;

namespace KHO.Scripts.ItemSystem
{
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

            if (inventory.TryAddItem(item))
                Destroy(gameObject);
        }
    }
}
