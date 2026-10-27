using System;
using DevLib.ModuleSystem;
using KHO.Scripts.ItemSystem;
using KHO.Scripts.UI.InventoryUI;
using UnityEngine;

namespace KHO.Scripts.InventorySystem
{
    public class InventoryModule : Module, IInventoryModule
    {
        [SerializeField] private InventoryViewModel viewModel;

        [Header("Debug")]
        [SerializeField] private ItemSO debugItem;

        public event Action<int> OnSlotChanged;

        public Inventory Inventory { get; private set; }
        public bool IsLocked { get; set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Inventory = new Inventory();
            BindViewModel();
        }

        public bool TryAddItem(ItemSO item)
        {
            if (IsLocked || item == null) return false;

            ItemInstance instance = new ItemInstance(item);
            if (!Inventory.TryAdd(instance, out int slot)) return false;

            instance.ApplyEffects(_owner);
            NotifySlotChanged(slot);
            return true;
        }

        public bool TryRemoveItem(int slot)
        {
            if (IsLocked || !IsValidSlot(slot) || Inventory.Slots[slot] == null) return false;

            ItemInstance prev = Inventory.Set(slot, null);
            prev.RemoveEffects(_owner);
            NotifySlotChanged(slot);
            return true;
        }

        public bool TryReplaceItem(int slot, ItemSO item)
        {
            if (IsLocked || item == null || !IsValidSlot(slot)) return false;

            ItemInstance instance = new ItemInstance(item);
            ItemInstance prev = Inventory.Set(slot, instance);
            prev?.RemoveEffects(_owner);
            instance.ApplyEffects(_owner);
            NotifySlotChanged(slot);
            return true;
        }

        private void NotifySlotChanged(int slot)
        {
            OnSlotChanged?.Invoke(slot);
            BindViewModel();
        }

        private void BindViewModel()
        {
            if (viewModel != null)
                viewModel.Bind(Inventory);
        }

        private static bool IsValidSlot(int slot) => slot >= 0 && slot < Inventory.SlotCount;

        [ContextMenu("Debug/Add Debug Item")]
        private void DebugAddItem()
        {
            if (!TryAddItem(debugItem))
                Debug.LogWarning("아이템을 추가하지 못했습니다. (잠김, 가득 참, 또는 debugItem 미지정)", this);

            DebugLogSlots();
        }

        [ContextMenu("Debug/Remove All Items")]
        private void DebugRemoveAll()
        {
            for (int i = 0; i < Inventory.SlotCount; i++)
                TryRemoveItem(i);

            DebugLogSlots();
        }

        [ContextMenu("Debug/Log Slots")]
        private void DebugLogSlots()
        {
            for (int i = 0; i < Inventory.SlotCount; i++)
                Debug.Log($"[{i}] {(Inventory.Slots[i] == null ? "-" : Inventory.Slots[i].Data.ItemName)}");
        }
    }
}
