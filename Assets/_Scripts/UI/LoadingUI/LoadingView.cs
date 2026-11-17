using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.UI.LoadingUI
{
    // 로딩창. 로딩이 시작되면 빠르게 나타나 화면을 덮고, 모든 로딩이 끝나면 천천히 사라진다.
    // 일시정지(timeScale 0) 중에도 움직이도록 unscaled 시간을 쓴다.
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingView : ViewBase<LoadingViewModel>
    {
        [SerializeField] private RectTransform barFill;

        [Header("Loading Title")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI loadingTitle;
        [SerializeField] private float delay = 1f;

        [Header("Fade Setting")]
        [Tooltip("초당 알파 변화량. SceneLoader는 0.25초 뒤에 씬을 바꾸므로 4 이상으로 둔다.")]
        [SerializeField] private float fadeInSpeed = 4f;
        [FormerlySerializedAs("fadeSpeed")]
        [SerializeField] private float fadeOutSpeed = 1f;

        private const string LoadingTitleFormat = "Loading{0}";
        private const char PointChar = '.';
        private const int MaxPointCount = 3;

        private CanvasGroup _canvasGroup;
        private bool _visible;
        private int _pointCount;
        private float _lastTitleTime;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            SetVisible(false);
        }

        protected override void OnBind(LoadingViewModel vm)
        {
            vm.IsLoading.Subscribe(SetVisible).AddTo(Disposables);
            vm.Progress.Subscribe(SetFill).AddTo(Disposables);

            if (loadingText != null)
                vm.Text.BindText(loadingText).AddTo(Disposables);
        }

        // 구독이 풀리면 숨긴다. 다시 켜지면 구독 즉시 현재 로딩 상태를 받아 다시 그린다.
        protected override void OnUnbind() => SetVisible(false);

        private void Update()
        {
            float target = _visible ? 1f : 0f;
            float speed = _visible ? fadeInSpeed : fadeOutSpeed;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, speed * Time.unscaledDeltaTime);

            if (_canvasGroup.alpha > 0f)
                UpdateLoadingTitle();
        }

        // 보일 때는 뒤의 UI를 누르지 못하게 막는다. 사라지는 중에는 바로 풀어 준다.
        private void SetVisible(bool visible)
        {
            _visible = visible;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private void SetFill(float progress)
        {
            if (barFill == null) return;
            barFill.localScale = new Vector3(progress, 1f, 1f);
        }

        private void UpdateLoadingTitle()
        {
            if (loadingTitle == null) return;
            if (Time.unscaledTime - _lastTitleTime < delay) return;

            _lastTitleTime = Time.unscaledTime;
            if (++_pointCount > MaxPointCount) _pointCount = 0;
            loadingTitle.text = string.Format(LoadingTitleFormat, new string(PointChar, _pointCount));
        }
    }
}
