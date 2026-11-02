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
        [SerializeField, Range(1, Inventory.EquipmentMaxCapacity)] private int startEquipmentCapacity = Inventory.EquipmentMaxCapacity;

        [Header("Debug")]
        [SerializeField] private ItemSO debugItem;

        public event Action<InventorySlotType, int> OnSlotChanged;
        public event Action<int> OnEquipmentCapacityChanged;

        public Inventory Inventory { get; private set; }
        public bool IsLocked { get; set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Inventory = new Inventory(startEquipmentCapacity);
            BindViewModel();
        }

        public bool TryAddItem(ItemSO item)
        {
            if (IsLocked || item == null) return false;

            ItemInstance instance = new ItemInstance(item);

            if (Inventory.Equipment.TryAdd(instance, out int slot))
            {
                instance.ApplyEffects(_owner);
                NotifySlotChanged(InventorySlotType.Equipment, slot);
                return true;
            }

            if (Inventory.Storage.TryAdd(instance, out slot))
            {
                NotifySlotChanged(InventorySlotType.Storage, slot);
                return true;
            }

            return false;
        }

        public bool TryRemoveItem(InventorySlotType type, int slot)
        {
            ItemContainer container = Inventory.Get(type);
            if (IsLocked || !container.IsValidSlot(slot) || container[slot] == null) return false;

            ItemInstance prev = container.Set(slot, null);
            if (type == InventorySlotType.Equipment) prev.RemoveEffects(_owner);
            NotifySlotChanged(type, slot);
            return true;
        }

        public bool TryReplaceItem(InventorySlotType type, int slot, ItemSO item)
        {
            ItemContainer container = Inventory.Get(type);
            if (IsLocked || item == null || !container.IsValidSlot(slot)) return false;

            ItemInstance instance = new ItemInstance(item);
            ItemInstance prev = container.Set(slot, instance);
            if (type == InventorySlotType.Equipment)
            {
                prev?.RemoveEffects(_owner);
                instance.ApplyEffects(_owner);
            }
            NotifySlotChanged(type, slot);
            return true;
        }

        public bool TryMoveItem(InventorySlotType fromType, int fromSlot, InventorySlotType toType, int toSlot)
        {
            ItemContainer from = Inventory.Get(fromType);
            ItemContainer to = Inventory.Get(toType);
            if (IsLocked || !from.IsValidSlot(fromSlot) || !to.IsValidSlot(toSlot) || from[fromSlot] == null) return false;
            if (fromType == toType && fromSlot == toSlot) return false;

            ItemInstance moving = from[fromSlot];
            ItemInstance swapped = to.Set(toSlot, moving);
            from.Set(fromSlot, swapped);

            // 같은 칸 묶음 안에서 옮기면 장착 여부가 그대로이므로 효과도 그대로 둔다.
            if (fromType != toType)
            {
                ItemInstance unequipped = fromType == InventorySlotType.Equipment ? moving : swapped;
                ItemInstance equipped = toType == InventorySlotType.Equipment ? moving : swapped;
                unequipped?.RemoveEffects(_owner);
                equipped?.ApplyEffects(_owner);
            }

            OnSlotChanged?.Invoke(fromType, fromSlot);
            NotifySlotChanged(toType, toSlot);
            return true;
        }

        public bool TryRemoveItem(int index)
            => Inventory.TryResolve(index, out InventorySlotType type, out int slot) && TryRemoveItem(type, slot);

        public bool TryReplaceItem(int index, ItemSO item)
            => Inventory.TryResolve(index, out InventorySlotType type, out int slot) && TryReplaceItem(type, slot, item);

        public bool TryExpandEquipment(int amount = 1)
        {
            if (IsLocked || amount <= 0 || Inventory.Equipment.Expand(amount) == 0) return false;

            OnEquipmentCapacityChanged?.Invoke(Inventory.Equipment.Capacity);
            BindViewModel();
            return true;
        }

        private void NotifySlotChanged(InventorySlotType type, int slot)
        {
            OnSlotChanged?.Invoke(type, slot);
            BindViewModel();
        }

        private void BindViewModel()
        {
            if (viewModel != null)
                viewModel.Bind(Inventory);
        }

        [ContextMenu("Debug/Add Debug Item")]
        private void DebugAddItem()
        {
            if (!TryAddItem(debugItem))
                Debug.LogWarning("아이템을 추가하지 못했습니다. (잠김, 가득 참, 또는 debugItem 미지정)", this);

            DebugLogSlots();
        }

        [ContextMenu("Debug/Expand Equipment")]
        private void DebugExpandEquipment()
        {
            if (!TryExpandEquipment())
                Debug.LogWarning($"장착 칸을 늘리지 못했습니다. (잠김 또는 최대 {Inventory.EquipmentMaxCapacity}칸)", this);

            DebugLogSlots();
        }

        [ContextMenu("Debug/Remove All Items")]
        private void DebugRemoveAll()
        {
            foreach (InventorySlotType type in new[] { InventorySlotType.Equipment, InventorySlotType.Storage })
            {
                for (int i = 0; i < Inventory.Get(type).Capacity; i++)
                    TryRemoveItem(type, i);
            }

            DebugLogSlots();
        }

        [ContextMenu("Debug/Log Slots")]
        private void DebugLogSlots()
        {
            foreach (InventorySlotType type in new[] { InventorySlotType.Equipment, InventorySlotType.Storage })
            {
                ItemContainer container = Inventory.Get(type);
                for (int i = 0; i < container.Capacity; i++)
                    Debug.Log($"[{type} {i}] {(container[i] == null ? "-" : container[i].Data.ItemName)}");
            }
        }
    }
}
