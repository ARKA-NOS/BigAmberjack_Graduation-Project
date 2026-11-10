using _Scripts._Core._Debug;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.UI.MVVM.ViewModels;
using _Scripts.UI.MVVM.Views;
using _Scripts.UI.Template;
using _Scripts.UI.UI_System;
using _Scripts.Upgrade;
using _Scripts.Upgrade.SO;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 창. 열고 닫기는 UIManager(IWindow)가 하고, 이 창은 열 때 받은 트리를 ViewModel에 연결한다(Connector 역할).
    // 창마다 런타임 ViewModel을 하나 만들어 자식 View(노드, 정보, 선)에 넣는다.
    // 닫을 때 연결을 끊으면 페이드아웃 중에 노드가 잠긴 색으로 바뀌어 보이므로, 연결은 다음에 열 때 새 트리로 바꾸거나 파괴될 때 끊는다.
    public class UpgradeTreeWindow : FadeWindow
    {
        [FormerlySerializedAs("infoPanel")]
        [SerializeField] private UpgradeInfoView infoView;
        [FormerlySerializedAs("lineDrawer")]
        [SerializeField] private UpgradeLineView lineView;

        private UpgradeTreeViewModel _viewModel;
        private UpgradeNodeView[] _nodeViews;

        protected override void Awake()
        {
            base.Awake();

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
            if (_viewModel != null)
                _viewModel.ReleaseRuntime();
        }

        public override void Open(object data)
        {
            base.Open(data);

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
            base.Close();
            ServiceLocator.Get<IPlayerController>().SetInputActive(true);
        }
    }
}
