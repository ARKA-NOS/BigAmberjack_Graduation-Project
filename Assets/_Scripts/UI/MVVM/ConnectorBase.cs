using UnityEngine;

namespace _Scripts.UI.MVVM
{
    // Model(게임 로직) 쪽 오브젝트에 붙여 ViewModel과 Model을 이어 주는 UI 쪽 컴포넌트.
    // 모듈은 UI를 모르고, 이 컴포넌트만 Model 인터페이스와 ViewModel을 함께 안다.
    // 처음 Start에서 연결하고(ModuleOwner의 모듈 초기화가 Awake에서 끝난 뒤), 이후에는 켜고 끌 때마다 연결/해제한다(풀링 대응).
    // Start/OnEnable/OnDisable이 더 필요하면 protected override로 만들고 base를 꼭 호출한다.
    public abstract class ConnectorBase : MonoBehaviour
    {
        private bool _started;
        private bool _isConnected;

        protected virtual void Start()
        {
            _started = true;
            TryConnect();
        }

        protected virtual void OnEnable()
        {
            if (_started) TryConnect();
        }

        // 파괴될 때도 OnDisable이 먼저 불리므로 여기서 해제된다.
        protected virtual void OnDisable()
        {
            TryDisconnect();
        }

        // 연결할 Model이 바뀌었을 때(장착 스킬 교체 등) 다시 연결한다.
        protected void Reconnect()
        {
            TryDisconnect();
            if (_started && isActiveAndEnabled) TryConnect();
        }

        // Model을 찾아 viewModel.Connect(model)을 호출한다.
        protected abstract void Connect();

        // Connect에서 연결한 ViewModel의 Disconnect(복제본이면 ReleaseRuntime)를 호출한다.
        protected abstract void Disconnect();

        private void TryConnect()
        {
            if (_isConnected) return;

            // Connect가 중간에 실패해도 Disconnect가 불려 정리되도록 먼저 표시한다.
            _isConnected = true;
            Connect();
        }

        private void TryDisconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            Disconnect();
        }
    }
}
