using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using _Scripts.UI.InventoryUI.StatUI;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.InventoryUI
{
    // 플레이어 프리팹에 붙어 인벤토리 창이 보여줄 플레이어 모듈(인벤토리, 스탯)을 ViewModel에 연결한다.
    // 모듈은 인벤토리 창을 모르고, 이 컴포넌트만 모듈과 ViewModel을 함께 안다. 비워 둔 ViewModel은 건너뛴다.
    public class InventoryWindowConnector : ConnectorBase
    {
        [SerializeField] private InventoryViewModel inventoryViewModel;
        [SerializeField] private StatListViewModel statViewModel;

        protected override void Connect()
        {
            ModuleOwner owner = GetComponentInParent<ModuleOwner>();
            if (owner == null)
            {
                Debug.LogWarning($"[{name}] ModuleOwner를 찾지 못해 인벤토리 창을 연결하지 않습니다.", this);
                return;
            }

            if (inventoryViewModel != null && owner.TryGetModule(out IInventoryModule inventory))
                inventoryViewModel.Connect(inventory);

            if (statViewModel != null && owner.TryGetModule(out IStatModule stats))
                statViewModel.Connect(stats);
        }

        protected override void Disconnect()
        {
            if (inventoryViewModel != null) inventoryViewModel.Disconnect();
            if (statViewModel != null) statViewModel.Disconnect();
        }
    }
}
