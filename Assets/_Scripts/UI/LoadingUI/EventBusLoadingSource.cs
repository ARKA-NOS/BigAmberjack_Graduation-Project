using System;
using _Scripts._Core._EventSystem;
using _Scripts.Loading;

namespace _Scripts.UI.LoadingUI
{
    // LoadingEvents로 알린 로딩 상태(EventBus<LoadingStateChangedEvent>)와 진행도(EventBus<SetLoadingGaugeEvent>)를
    // ILoadingSource로 감싼다. LoadingConnector가 만든다.
    public class EventBusLoadingSource : ILoadingSource
    {
        private Action<bool> _loadingChanged;
        private Action<LoadingData> _progressChanged;

        public bool IsLoading => LoadingEvents.IsLoading;

        // 구독자가 생기면 EventBus를 구독하고, 모두 빠지면 해제한다. EventBus는 정적이라 씬이 바뀌어도 남으므로 해제가 중요하다.
        public event Action<bool> LoadingChanged
        {
            add
            {
                if (_loadingChanged == null) EventBus<LoadingStateChangedEvent>.Subscribe(HandleLoadingStateChanged);
                _loadingChanged += value;
            }
            remove
            {
                _loadingChanged -= value;
                if (_loadingChanged == null) EventBus<LoadingStateChangedEvent>.UnSubscribe(HandleLoadingStateChanged);
            }
        }

        public event Action<LoadingData> ProgressChanged
        {
            add
            {
                if (_progressChanged == null) EventBus<SetLoadingGaugeEvent>.Subscribe(HandleSetLoadingGauge);
                _progressChanged += value;
            }
            remove
            {
                _progressChanged -= value;
                if (_progressChanged == null) EventBus<SetLoadingGaugeEvent>.UnSubscribe(HandleSetLoadingGauge);
            }
        }

        private void HandleLoadingStateChanged(LoadingStateChangedEvent evt) => _loadingChanged?.Invoke(evt.Value);

        private void HandleSetLoadingGauge(SetLoadingGaugeEvent evt) => _progressChanged?.Invoke(evt.Value);
    }
}
