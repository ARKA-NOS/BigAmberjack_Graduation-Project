using System.Collections.Generic;
using _Scripts.Inventory;
using _Scripts.Item;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 인벤토리 창의 ViewModel. 장착 칸과 보관 칸을 최대 칸 수만큼 목록으로 노출하고, 바뀐 칸만 Replace로 알린다.
    // 칸 끌어 놓기(드래그 앤 드롭)는 TryMoveItem으로 받아 인벤토리 모듈에 넘긴다.
    [CreateAssetMenu(fileName = "Inventory View Model", menuName = "UI/MVVM/Inventory View Model")]
    public class InventoryViewModel : ViewModelBase<IInventoryModule>
    {
        private readonly ObservableList<InventorySlotData> _equipment = new(Inventory.Inventory.EquipmentMaxCapacity);
        private readonly ObservableList<InventorySlotData> _storage = new(Inventory.Inventory.StorageCapacity);
        private readonly List<InventorySlotData> _buffer = new();
        private IInventoryModule _model;

        public IReadOnlyObservableList<InventorySlotData> Equipment => _equipment;
        public IReadOnlyObservableList<InventorySlotData> Storage => _storage;

        public IReadOnlyObservableList<InventorySlotData> GetSlots(InventorySlotType type) => GetList(type);

        protected override void OnConnect(IInventoryModule model)
        {
            _model = model;
            _model.OnSlotChanged += HandleSlotChanged;
            _model.OnEquipmentCapacityChanged += HandleEquipmentCapacityChanged;

            RefreshAll(InventorySlotType.Equipment);
            RefreshAll(InventorySlotType.Storage);
        }

        protected override void OnDisconnect()
        {
            _model.OnSlotChanged -= HandleSlotChanged;
            _model.OnEquipmentCapacityChanged -= HandleEquipmentCapacityChanged;
            _model = null;
        }

        // 플레이어가 없을 때도 칸 모양은 보이도록 빈 칸으로 채운다.
        protected override void ResetState()
        {
            FillEmpty(_equipment, Inventory.Inventory.EquipmentMaxCapacity);
            FillEmpty(_storage, Inventory.Inventory.StorageCapacity);
        }

        // 두 칸의 아이템을 맞바꾼다(대상 칸이 비어 있으면 옮긴다). 연결 전이거나 잠겨 있으면 false.
        public bool TryMoveItem(InventorySlotType fromType, int fromSlot, InventorySlotType toType, int toSlot)
            => _model != null && _model.TryMoveItem(fromType, fromSlot, toType, toSlot);

        private void HandleSlotChanged(InventorySlotType type, int slot)
        {
            ObservableList<InventorySlotData> list = GetList(type);
            if (slot < 0 || slot >= list.Count)
            {
                RefreshAll(type);
                return;
            }

            InventorySlotData data = CreateData(_model.Inventory.Get(type), slot);
            if (!list[slot].Equals(data))
                list[slot] = data;
        }

        // 장착 칸이 늘어나면 잠긴 칸이 풀리므로 장착 칸 전체를 다시 채운다.
        private void HandleEquipmentCapacityChanged(int capacity) => RefreshAll(InventorySlotType.Equipment);

        private void RefreshAll(InventorySlotType type)
        {
            ItemContainer container = _model.Inventory.Get(type);

            _buffer.Clear();
            for (int i = 0; i < container.MaxCapacity; i++)
                _buffer.Add(CreateData(container, i));

            GetList(type).SetAll(_buffer);
        }

        private void FillEmpty(ObservableList<InventorySlotData> list, int count)
        {
            _buffer.Clear();
            for (int i = 0; i < count; i++)
                _buffer.Add(InventorySlotData.Empty);

            list.SetAll(_buffer);
        }

        private ObservableList<InventorySlotData> GetList(InventorySlotType type)
            => type == InventorySlotType.Equipment ? _equipment : _storage;

        private static InventorySlotData CreateData(ItemContainer container, int slot)
        {
            bool unlocked = slot < container.Capacity;
            ItemInstance item = unlocked ? container[slot] : null;
            return new InventorySlotData(item?.Data, !unlocked);
        }
    }
}
