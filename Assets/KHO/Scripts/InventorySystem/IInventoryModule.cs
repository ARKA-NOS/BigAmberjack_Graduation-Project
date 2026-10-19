using System;
using KHO.Scripts.ItemSystem;

namespace KHO.Scripts.InventorySystem
{
    public interface IInventoryModule
    {
        event Action<int> OnSlotChanged;

        Inventory Inventory { get; }

        bool IsLocked { get; set; }

        bool TryAddItem(ItemSO item);
        bool TryRemoveItem(int slot);
        bool TryReplaceItem(int slot, ItemSO item);
    }
}
