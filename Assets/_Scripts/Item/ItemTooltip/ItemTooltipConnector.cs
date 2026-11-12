using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.Item.ItemTooltip
{
    // 바닥 아이템 툴팁 캔버스에 붙어, 상호작용 대상 아이템 알림(ItemTooltipEvents)을 ItemTooltipViewModel에 연결한다.
    // 알림은 특정 오브젝트가 아니라 EventBus로 오므로 UI 쪽에 붙인다.
    public class ItemTooltipConnector : ConnectorBase
    {
        [SerializeField] private ItemTooltipViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"[{name}] ItemTooltipViewModel이 지정되지 않았습니다.", this);
                return;
            }

            viewModel.Connect(new EventBusItemTooltipFocusSource());
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
