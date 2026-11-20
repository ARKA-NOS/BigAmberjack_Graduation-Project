using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.AlarmUI
{
    // 알람 표시 영역에 붙어, AlarmEvents.SendAlarm으로 보낸 알람을 AlarmViewModel에 연결한다.
    // 알람은 특정 오브젝트가 아니라 EventBus로 오므로 UI 쪽에 붙인다.
    public class AlarmConnector : ConnectorBase
    {
        [SerializeField] private AlarmViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"[{name}] AlarmViewModel이 지정되지 않았습니다.", this);
                return;
            }

            viewModel.Connect(new EventBusAlarmSource());
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
