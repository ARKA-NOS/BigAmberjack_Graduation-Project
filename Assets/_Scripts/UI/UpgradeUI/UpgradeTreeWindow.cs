using _Scripts._Core._Debug;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.UI.MVVM.ViewModels;
using _Scripts.UI.MVVM.Views;
using _Scripts.UI.UI_System;
using _Scripts.Upgrade;
using _Scripts.Upgrade.SO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 창. 열고 닫기는 UIManager(IWindow)가 하고, 이 창은 열 때 받은 트리를 ViewModel에 연결한다(Connector 역할).
    // 창마다 런타임 ViewModel을 하나 만들어 자식 View(노드, 정보, 선)에 넣는다.
    // 열고 닫을 때 페이드하고, 움직임은 transition이 맡는다. ViewModel 연결은 다음에 열 때 새 트리로 바꾸거나 파괴될 때 끊는다.
    public class UpgradeTreeWindow : AbstractWindow
    {
        [FormerlySerializedAs("infoPanel")]
        [SerializeField] private UpgradeInfoView infoView;
        [FormerlySerializedAs("lineDrawer")]
        [SerializeField] private UpgradeLineView lineView;
        [Tooltip("열고 닫을 때의 움직임. 비워 두면 페이드만 한다.")]
        [SerializeField] private WindowTransition transition;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private UpgradeTreeViewModel _viewModel;
        private UpgradeNodeView[] _nodeViews;
        // 창 전체를 보이고 숨긴다. 창이 HUD 위에 그려지므로 닫혀 있는 동안에는 클릭도 가로채지 않는다.
        private CanvasGroup _canvasGroup;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetInteractable(false);
            _canvasGroup.alpha = 0f;
            if (transition != null) transition.SnapHidden();

            _viewModel = ViewModelBase.CreateRuntime<UpgradeTreeViewModel>();
            _nodeViews = GetComponentsInChildren<UpgradeNodeView>(true);

            foreach (ViewBase<UpgradeTreeViewModel> view in GetComponentsInChildren<ViewBase<UpgradeTreeViewModel>>(true))
                view.SetViewModel(_viewModel);

            // 창 아래에 있지 않아도 인스펙터로 연결된 View에는 넣는다(이미 넣었으면 SetViewModel이 무시한다).
            if (infoView != null) infoView.SetViewModel(_viewModel);
            if (lineView != null) lineView.SetViewModel(_viewModel);
        }

        protected virtual void OnDestroy()
        {
            if (_canvasGroup != null) _canvasGroup.DOKill();

            if (_viewModel != null)
                _viewModel.ReleaseRuntime();
        }

        public override void Open(object data)
        {
            SetInteractable(true);
            Fade(1f);
            if (transition != null) transition.Show();

            if (data is not UpgradeTreeSO treeSo)
            {
                FDebug.LogError(data == null ? "[UpgradeUI] data is null" : "[UpgradeUI] data type is not UpgradeTreeSO");
                return;
            }

            UpgradeTree tree = ServiceLocator.Get<IUpgradeManager>().GetUpgradeTree(treeSo.TreeName);
            if (tree == null)
            {
                FDebug.LogError($"[UpgradeUI] {treeSo.TreeName} tree is not found");
                return;
            }

            ServiceLocator.Get<IPlayerController>().SetInputActive(false);
            _viewModel.Connect(tree);
            // 이전의 임시 선택은 버린다.
            _viewModel.DiscardPending();

            if (lineView != null)
            {
                Canvas.ForceUpdateCanvases();
                lineView.Draw(_nodeViews);
            }
        }

        // 닫기 버튼(UnityEvent)에서 호출한다.
        public void RequestClose()
            => UIManagerEvents.OpenCloseWindow(null);

        public override void Close()
        {
            // 입력은 바로 끊고, 화면만 뒤따라 사라진다.
            SetInteractable(false);
            Fade(0f);
            if (transition != null) transition.Hide();
            ServiceLocator.Get<IPlayerController>().SetInputActive(true);
        }

        private void SetInteractable(bool value)
        {
            IsOpen = value;
            _canvasGroup.interactable = value;
            _canvasGroup.blocksRaycasts = value;
        }

        private void Fade(float alpha)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(alpha, fadeDuration).SetUpdate(true);
        }
    }
}
