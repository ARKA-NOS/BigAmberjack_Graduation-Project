using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts._Core._ServiceLocator;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using UnityEngine;

namespace _Scripts.UI.MVVM.ViewModels
{
    /*
    [CreateAssetMenu(fileName = "Xxx View Model", menuName = "UI/MVVM/Xxx View Model")]
    */
    // 모든 ViewModel의 베이스. ScriptableObject 에셋으로 만들고, View와 Connector가 같은 에셋을 참조한다.
    // 값(Observable)은 런타임 상태라 에셋에 저장되지 않는다. 저장이 필요한 값은 ViewModel에 두지 않는다.
    // 같은 UI가 여러 개 필요하면 CreateRuntime으로 복제본을 만들어 쓴다.
    public abstract class ViewModelBase : ScriptableObject
    {
        // 불러온 ViewModel 목록. 도메인 리로드를 끈 상태로 플레이를 다시 시작해도 이전 값이 남지 않게 초기화하는 데 쓴다.
        private static readonly HashSet<ViewModelBase> LoadedViewModels = new();

        private readonly Observable<bool> _connected = new();
        private IUITicker _ticker;

        // Model과 연결돼 있는지. View는 이 값으로 Model이 없을 때 화면을 숨길 수 있다.
        public IReadOnlyObservable<bool> Connected => _connected;
        public bool IsConnected => _connected.Value;

        // CreateRuntime으로 만든 복제본인지.
        public bool IsRuntimeInstance { get; private set; }

        protected virtual void OnEnable()
        {
            LoadedViewModels.Add(this);
            ClearState();
        }

        protected virtual void OnDisable()
        {
            LoadedViewModels.Remove(this);
        }

        // Model 구독을 끊고 값을 기본값으로 되돌린다. 구독 중인 View는 그대로 남아 기본값을 받는다.
        // 화면이 먼저 숨겨지도록 Connected를 먼저 false로 바꾼다.
        public void Disconnect()
        {
            if (!IsConnected) return;

            _connected.Value = false;
            try
            {
                OnDisconnect();
            }
            finally
            {
                UnregisterTick();
                ResetState();
            }
        }

        // Model 이벤트 구독을 해제한다.
        protected abstract void OnDisconnect();

        // Observable들을 기본값으로 되돌린다.
        protected abstract void ResetState();

        // 값이 다 채워진 뒤 화면이 보이도록 Connected는 마지막에 true로 바꾼다.
        private protected void MarkConnected()
        {
            RegisterTick();
            _connected.Value = true;
        }

        #region Runtime Instance

        // 템플릿 에셋을 복제해 인스턴스 전용 ViewModel을 만든다(적 체력바처럼 같은 UI가 여러 개일 때).
        // 다 쓰면 ReleaseRuntime을 호출한다.
        public static T CreateRuntime<T>(T template) where T : ViewModelBase
        {
            if (template == null)
            {
                FDebug.LogError("[ViewModelBase] CreateRuntime template is null");
                return null;
            }

            T instance = Instantiate(template);
            instance.name = $"{template.name} (Runtime)";
            instance.IsRuntimeInstance = true;
            return instance;
        }

        // 템플릿 에셋 없이 기본 설정으로 만든다. 인스펙터 설정이 없는 ViewModel을 창마다 하나씩 쓸 때 쓴다.
        public static T CreateRuntime<T>() where T : ViewModelBase
        {
            T instance = CreateInstance<T>();
            instance.name = $"{typeof(T).Name} (Runtime)";
            instance.IsRuntimeInstance = true;
            return instance;
        }

        // 복제본의 연결을 끊고 파괴한다. 에셋에 호출하면 연결만 끊는다.
        public void ReleaseRuntime()
        {
            Disconnect();

            if (IsRuntimeInstance)
                Destroy(this);
            else
                FDebug.LogWarning($"[{name}] 에셋은 파괴하지 않습니다. 연결만 끊었습니다.", this);
        }

        #endregion

        #region Tick

        private void RegisterTick()
        {
            if (this is not ITickableViewModel tickable) return;

            // 해제할 때 같은 Ticker에서 빼도록 등록한 Ticker를 기억한다.
            _ticker = ServiceLocator.Get<IUITicker>();
            _ticker?.Register(tickable);
        }

        private void UnregisterTick()
        {
            if (_ticker != null && this is ITickableViewModel tickable)
                _ticker.Unregister(tickable);

            _ticker = null;
        }

        #endregion

        // 이전 플레이의 연결 정보와 값을 지운다. Model은 이미 사라졌으므로 OnDisconnect는 호출하지 않는다.
        private void ClearState()
        {
            _ticker = null;
            _connected.SetWithoutNotify(false);
            ResetState();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAllOnPlay()
        {
            foreach (ViewModelBase viewModel in new List<ViewModelBase>(LoadedViewModels))
            {
                if (viewModel != null) viewModel.ClearState();
            }
        }
    }

    // Model 하나와 연결되는 ViewModel. Model이 여러 개 필요하면 ViewModel 옆에 작은 struct를 만들어 TModel로 쓴다.
    public abstract class ViewModelBase<TModel> : ViewModelBase
    {
        // 이미 연결돼 있으면 먼저 끊고 새 Model에 연결한다.
        public void Connect(TModel model)
        {
            if (model == null)
            {
                FDebug.LogWarning($"[{name}] Connect model is null", this);
                return;
            }

            if (IsConnected) Disconnect();

            OnConnect(model);
            MarkConnected();
        }

        // Model 이벤트를 구독하고 현재 값으로 Observable을 채운다.
        protected abstract void OnConnect(TModel model);
    }
}
