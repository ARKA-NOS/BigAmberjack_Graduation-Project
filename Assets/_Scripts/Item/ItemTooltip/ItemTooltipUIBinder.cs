using _Scripts.UI.BindSystem;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Item.ItemTooltip
{
    // 상호작용 대상인 바닥 아이템 위에 툴팁과 키 안내를 띄운다.
    // Screen Space - Overlay 캔버스 아래에 두고, 아이템의 월드 좌표를 화면 좌표로 바꿔 따라다닌다.
    public class ItemTooltipUIBinder : AbstractUIBinder<IItemTooltipSource>
    {
        [SerializeField] private ItemTooltipView view;
        [SerializeField] private Vector2 screenOffset = new(0f, 30f);
        [SerializeField] private Image destroyHoldFill;

        protected override void Awake()
        {
            base.Awake();
            view.Hide();
        }

        private void Start()
        {
            OnDataChanged();
        }

        protected override void DataUpdate()
        {
            IItemTooltipSource source = CurrentValue;
            if (source == null || !view.IsShown) return;

            FollowTarget(source.TooltipPosition);
            destroyHoldFill.fillAmount = source.DestroyProgress;
        }

        protected override void OnDataChanged()
        {
            ItemSO item = CurrentValue?.Item;
            view.Show(item);
            if (item == null) return;

            destroyHoldFill.fillAmount = 0f;
            FollowTarget(CurrentValue.TooltipPosition);
        }

        private void FollowTarget(Vector2 worldPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Area, screenPoint, null, out Vector2 local))
                view.MoveTo(local + screenOffset);
        }
    }
}
