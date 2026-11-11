using System.Collections.Generic;
using _Scripts.Inventory;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 장착 또는 보관 칸 묶음 하나를 표시한다. 칸 UI는 slotParent의 자식을 쓰고, 칸 수보다 적으면 template를 복제해 늘린다.
    // 칸 하나가 바뀌면 그 칸만 다시 그린다.
    public class InventorySlotListView : ViewBase<InventoryViewModel>
    {
        [SerializeField] private InventorySlotType target;
        [SerializeField] private Transform slotParent;
        [SerializeField] private InventorySlotUI template;

        private readonly List<InventorySlotUI> _slots = new();

        private void Awake()
        {
            slotParent.GetComponentsInChildren(true, _slots);
        }

        protected override void OnBind(InventoryViewModel vm)
        {
            IReadOnlyObservableList<InventorySlotData> slots = vm.GetSlots(target);
            slots.Subscribe(change => HandleSlotsChanged(slots, change)).AddTo(Disposables);
        }

        private void HandleSlotsChanged(IReadOnlyList<InventorySlotData> slots, ListChange<InventorySlotData> change)
        {
            if (change.Kind == ListChangeKind.Replace && change.Index < _slots.Count)
            {
                _slots[change.Index].SetData(change.NewItem);
                return;
            }

            RebuildAll(slots);
        }

        // 칸 수만큼 모두 보여주고, 남는 칸 UI는 끈다.
        private void RebuildAll(IReadOnlyList<InventorySlotData> slots)
        {
            while (_slots.Count < slots.Count && template != null)
                _slots.Add(Instantiate(template, slotParent));

            for (int i = 0; i < _slots.Count; i++)
            {
                bool shown = i < slots.Count;
                _slots[i].gameObject.SetActive(shown);
                if (!shown) continue;

                _slots[i].Setup(target, i);
                _slots[i].SetData(slots[i]);
            }
        }
    }
}
