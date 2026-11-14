using _Scripts._Core._ServiceLocator;
using _Scripts.Settings;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.SettingsUI
{
    // 전역 ISettingsManager와 설정 ViewModel을 잇는다.
    // GameManager는 런이 끝날 때 다시 만들어지는데 이 창(플레이어 UI)은 남을 수 있어서, 창을 열 때 매니저가 바뀌었으면 다시 연결한다.
    public class SettingsConnector : ConnectorBase
    {
        [SerializeField] private SettingsViewModel viewModel;

        private ISettingsManager _connected;

        // 지금 등록된 매니저와 연결돼 있지 않으면 다시 연결한다.
        public void RefreshModel()
        {
            if (!isActiveAndEnabled) return;
            if (ReferenceEquals(_connected, ServiceLocator.Get<ISettingsManager>())) return;

            Reconnect();
        }

        protected override void Connect()
        {
            _connected = ServiceLocator.Get<ISettingsManager>();
            if (_connected != null) viewModel.Connect(_connected);
        }

        protected override void Disconnect()
        {
            _connected = null;
            viewModel.Disconnect();
        }
    }
}
