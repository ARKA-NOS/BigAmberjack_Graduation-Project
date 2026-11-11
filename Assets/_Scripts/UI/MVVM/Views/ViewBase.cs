using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.UI.MVVM.Views
{
    // 모든 View의 베이스. 켜질 때(OnEnable) ViewModel을 구독하고 꺼질 때(OnDisable) 해제한다.
    // 구독하는 즉시 현재 값으로 한 번 그려지므로, 꺼졌다 켜져도 최신 상태가 되고 꺼진 동안은 비용이 없다.
    // OnEnable/OnDisable이 더 필요하면 protected override로 만들고 base를 꼭 호출한다.
    public abstract class ViewBase<TViewModel> : MonoBehaviour where TViewModel : ViewModelBase
    {
        // FormerlySerializedAs: 레거시 BindSystem Binder 스크립트(필드 이름 bindData)를 같은 GUID로 View로 바꿀 때 인스펙터 연결이 유지되게 한다.
        [Tooltip("하나뿐인 UI는 ViewModel 에셋을 연결한다. 런타임 복제본을 쓰는 UI는 비워 두고 Connector가 SetViewModel로 넣는다.")]
        [FormerlySerializedAs("bindData")]
        [SerializeField] private TViewModel viewModel;

        private readonly DisposableBag _disposables = new();
        private bool _isBound;

        protected TViewModel ViewModel => viewModel;

        // OnBind에서 만든 구독을 여기에 넣으면 OnDisable에서 한 번에 해제된다.
        protected DisposableBag Disposables => _disposables;

        protected virtual void OnEnable() => Bind();

        protected virtual void OnDisable() => Unbind();

        // ViewModel을 바꾼다(런타임 복제본 주입). 켜져 있으면 바로 다시 구독한다.
        public void SetViewModel(TViewModel newViewModel)
        {
            if (viewModel == newViewModel) return;

            Unbind();
            viewModel = newViewModel;
            if (isActiveAndEnabled) Bind();
        }

        // vm의 Observable을 구독하고 .AddTo(Disposables)로 등록한다.
        protected abstract void OnBind(TViewModel vm);

        // 구독 외에 정리할 것(트윈 정지 등)이 있으면 쓴다. 구독은 이미 해제된 뒤에 호출된다.
        protected virtual void OnUnbind() { }

        // 런타임 복제본을 기다리는 View는 처음에 ViewModel이 비어 있으므로 경고 없이 건너뛴다.
        private void Bind()
        {
            if (_isBound || viewModel == null) return;

            _isBound = true;
            OnBind(viewModel);
        }

        private void Unbind()
        {
            if (!_isBound) return;

            _isBound = false;
            _disposables.Dispose();
            OnUnbind();
        }
    }
}
