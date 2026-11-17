using _Scripts.Loading;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.LoadingUI
{
    // 로딩창 ViewModel. 로딩 중인지와 지금 단계의 이름·진행도를 노출한다.
    // 언제 얼마나 빨리 나타나고 사라질지는 View(LoadingView)가 정한다.
    [CreateAssetMenu(fileName = "Loading View Model", menuName = "UI/MVVM/Loading View Model")]
    public class LoadingViewModel : ViewModelBase<ILoadingSource>
    {
        private readonly Observable<bool> _isLoading = new();
        private readonly Observable<string> _text = new(string.Empty);
        private readonly Observable<float> _progress = new();

        private ILoadingSource _source;

        public IReadOnlyObservable<bool> IsLoading => _isLoading;

        // 지금 진행 중인 로딩 단계 이름(Scene, Room Bake, Wall Fill 등).
        public IReadOnlyObservable<string> Text => _text;

        // 지금 단계의 진행도(0~1). 단계가 바뀌면 다시 0부터 시작한다.
        public IReadOnlyObservable<float> Progress => _progress;

        protected override void OnConnect(ILoadingSource source)
        {
            _source = source;
            _source.LoadingChanged += HandleLoadingChanged;
            _source.ProgressChanged += HandleProgressChanged;
            HandleLoadingChanged(source.IsLoading);
        }

        protected override void OnDisconnect()
        {
            _source.LoadingChanged -= HandleLoadingChanged;
            _source.ProgressChanged -= HandleProgressChanged;
            _source = null;
        }

        // OnEnable에서도 불리므로 Observable만 건드린다(_source는 null일 수 있다).
        protected override void ResetState()
        {
            _isLoading.Value = false;
            _text.Value = string.Empty;
            _progress.Value = 0f;
        }

        private void HandleLoadingChanged(bool isLoading)
        {
            // 새 로딩이 시작되면 지난 로딩의 단계와 진행도를 지운다.
            if (isLoading)
            {
                _text.Value = string.Empty;
                _progress.Value = 0f;
            }

            _isLoading.Value = isLoading;
        }

        private void HandleProgressChanged(LoadingData data)
        {
            _text.Value = data.Text ?? string.Empty;
            _progress.Value = data.LoadingGauge.Normalized;
        }
    }
}
