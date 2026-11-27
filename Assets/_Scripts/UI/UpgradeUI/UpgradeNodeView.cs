using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using _Scripts.Upgrade.SO;
using TMPro;
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
        private const float IconPadding = 15f;
        private static readonly Color LockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField] private UpgradeNodeSO upgradeNode;

        [Tooltip("노드 아이콘을 그릴 Image. 비워두면 런타임에 자식으로 만든다.")]
        [SerializeField] private Image iconImage;

        [Tooltip("이름 텍스트. 비워두면 자식의 TMP_Text를 찾아 숨긴다.")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("임시 선택했거나 구매한 노드에 켜는 체크 표시.")]
        [SerializeField] private GameObject checkMark;

        [Tooltip("임시 선택(확정 전)인 노드에 켜는 테두리.")]
        [SerializeField] private GameObject pendingFrame;

        private Button _button;
        private CanvasGroup _canvasGroup;
        private UpgradeNodeItem _item;

        public UpgradeNodeSO Node => upgradeNode;

        protected override void OnBind(UpgradeTreeViewModel vm)
        {
            // 창의 Awake에서 바로 바인드될 수 있으므로 자기 Awake보다 먼저 불려도 되게 여기서 찾는다.
            EnsureComponents();

            ApplyIcon();

            _item = vm.GetNode(upgradeNode);
            if (_item == null) return;

            _item.State.Subscribe(ApplyState).AddTo(Disposables);

            if (_button != null)
                _button.BindClick(_item.Toggle).AddTo(Disposables);
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
            bool selected = state is UpgradeNodeState.Pending or UpgradeNodeState.Purchased;
            _canvasGroup.alpha = selected ? 1f : DimAlpha;

            if (checkMark != null) checkMark.SetActive(selected);
            if (pendingFrame != null) pendingFrame.SetActive(state == UpgradeNodeState.Pending);

            if (_button != null && _button.targetGraphic != null)
                _button.targetGraphic.color = state == UpgradeNodeState.Locked ? LockedColor : Color.white;

            if (iconImage != null)
                iconImage.color = state == UpgradeNodeState.Locked ? LockedColor : Color.white;
        }

        // 이름 대신 노드 아이콘을 보여준다. 아이콘이 없는 노드는 이름을 그대로 둔다.
        private void ApplyIcon()
        {
            if (nameText == null) nameText = GetComponentInChildren<TMP_Text>(true);
            if (iconImage == null)
            {
                iconImage = CreateIconImage();
                // 런타임에 만든 아이콘이 체크 표시를 가리지 않게 맨 뒤로 보낸다.
                iconImage.transform.SetAsFirstSibling();
            }

            Sprite icon = upgradeNode != null ? upgradeNode.NodeIcon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;

            if (nameText != null) nameText.gameObject.SetActive(icon == null);
        }

        private Image CreateIconImage()
        {
            var child = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)child.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(IconPadding, IconPadding);
            rect.offsetMax = new Vector2(-IconPadding, -IconPadding);

            var image = child.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void EnsureComponents()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            if (_button == null && !TryGetComponent(out _button))
                Debug.LogError($"[{name}] Button is not found", this);
        }
    }
}
