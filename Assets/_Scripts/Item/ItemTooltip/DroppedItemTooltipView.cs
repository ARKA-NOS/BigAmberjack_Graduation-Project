using _Scripts.UI.MVVM.Bindings;
using _Scripts.UI.MVVM.Disposables;
using _Scripts.UI.MVVM.Views;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Scripts.Item.ItemTooltip
{
    // 상호작용 대상인 바닥 아이템 위에 툴팁과 키 안내를 띄운다.
    // Screen Space - Overlay 캔버스 아래에 두고, 아이템의 월드 좌표를 화면 좌표로 바꿔 따라다닌다.
    public class DroppedItemTooltipView : ViewBase<ItemTooltipViewModel>
    {
        [FormerlySerializedAs("view")]
        [SerializeField] private ItemTooltipView panel;
        [SerializeField] private Vector2 screenOffset = new(0f, 30f);
        [SerializeField] private Image destroyHoldFill;

        private void Awake()
        {
            panel.Hide();
        }

        protected override void OnBind(ItemTooltipViewModel vm)
        {
            vm.Item.Subscribe(ShowItem).AddTo(Disposables);
            vm.DestroyProgress.BindFill(destroyHoldFill).AddTo(Disposables);
        }

        protected override void OnUnbind()
        {
            panel.Hide();
        }

        // 카메라가 움직이므로 월드 좌표가 그대로여도 화면 위치는 매 프레임 다시 계산한다.
        private void Update()
        {
            if (ViewModel == null || !panel.IsShown) return;

            FollowTarget(ViewModel.WorldPosition.Value);
        }

        private void ShowItem(ItemSO item)
        {
            panel.Show(item);
            if (item != null)
                FollowTarget(ViewModel.WorldPosition.Value);
        }

        private void FollowTarget(Vector2 worldPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panel.Area, screenPoint, null, out Vector2 local))
                panel.MoveTo(local + screenOffset);
        }
    }
}
