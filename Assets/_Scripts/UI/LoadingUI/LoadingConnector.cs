using _Scripts.UI.MVVM.Connectors;
using UnityEngine;

namespace _Scripts.UI.LoadingUI
{
    // LoadingEvents로 알린 로딩을 LoadingViewModel에 연결한다.
    // 로딩은 특정 오브젝트가 아니라 EventBus로 오므로 UI 쪽(GameManager 안의 DefaultCanvas)에 붙인다.
    // ViewModel 에셋 하나에 Connector 하나만 둔다. 씬마다 로딩창을 따로 두면, 그 씬이 내려갈 때 연결이 끊겨 GameManager의 로딩창까지 꺼진다.
    public class LoadingConnector : ConnectorBase
    {
        [SerializeField] private LoadingViewModel viewModel;

        protected override void Connect()
        {
            if (viewModel == null)
            {
                Debug.LogWarning($"[{name}] LoadingViewModel이 지정되지 않았습니다.", this);
                return;
            }

            viewModel.Connect(new EventBusLoadingSource());
        }

        protected override void Disconnect()
        {
            if (viewModel != null)
                viewModel.Disconnect();
        }
    }
}
