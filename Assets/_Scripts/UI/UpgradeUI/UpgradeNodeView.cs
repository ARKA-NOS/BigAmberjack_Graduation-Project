using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using _Scripts.Upgrade.SO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI.UpgradeUI
{
    // 테크트리 노드 버튼 하나. 인스펙터에 지정한 노드의 상태를 ViewModel에서 받아 그리고, 클릭/마우스 올림을 ViewModel에 넘긴다.
    // ViewModel은 UpgradeTreeWindow가 SetViewModel로 넣는다.
    [RequireComponent(typeof(CanvasGroup))]
    public class UpgradeNodeView : ViewBase<UpgradeTreeViewModel>, IPointerEnterHandler
    {
        private const float DimAlpha = 0.5f;
        private static readonly Color LockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField] private UpgradeNodeSO upgradeNode;

        private Button _button;
        private CanvasGroup _canvasGroup;
        private UpgradeNodeItem _item;

        public UpgradeNodeSO Node => upgradeNode;

        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            // 창의 Awake에서 바로 바인드될 수 있으므로 자기 Awake보다 먼저 불려도 되게 여기서 찾는다.
            EnsureComponents();

            _item = vm.GetNode(upgradeNode);
            if (_item == null) return;

            _item.State.Subscribe(ApplyState).AddTo(Disposables);

            if (_button != null)
                _button.BindClick(_item.Purchase).AddTo(Disposables);
        }

        protected override void OnUnbind()
        {
            _item = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _item?.Select();
        }

        private void ApplyState(UpgradeNodeState state)
        {
            _canvasGroup.alpha = state == UpgradeNodeState.Purchased ? 1f : DimAlpha;

            if (_button != null && _button.targetGraphic != null)
                _button.targetGraphic.color = state == UpgradeNodeState.Locked ? LockedColor : Color.white;
        }

        private void EnsureComponents()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            if (_button == null && !TryGetComponent(out _button))
                Debug.LogError($"[{name}] Button is not found", this);
        }
    }
}
