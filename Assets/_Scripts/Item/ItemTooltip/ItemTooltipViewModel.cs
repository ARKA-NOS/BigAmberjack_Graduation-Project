using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.Item.ItemTooltip
{
    // 상호작용 대상인 바닥 아이템의 툴팁 ViewModel. 대상이 없으면 Item이 null이다.
    // 따라다닐 위치와 파괴 키 홀드 진행도는 이벤트가 없으므로 Tick에서 매 프레임 읽는다.
    [CreateAssetMenu(fileName = "Item Tooltip View Model", menuName = "UI/MVVM/Item Tooltip View Model")]
    public class ItemTooltipViewModel : ViewModelBase<IItemTooltipFocusSource>, ITickableViewModel
    {
        private readonly Observable<ItemSO> _item = new();
        private readonly Observable<Vector2> _worldPosition = new();
        private readonly Observable<float> _destroyProgress = new();
        private IItemTooltipFocusSource _source;
        private IItemTooltipSource _target;

        // 툴팁에 보여줄 아이템. null이면 툴팁을 닫는다.
        public IReadOnlyObservable<ItemSO> Item => _item;

        // 툴팁이 따라다닐 월드 좌표.
        public IReadOnlyObservable<Vector2> WorldPosition => _worldPosition;

        // 파괴 키 홀드 진행도(0~1).
        public IReadOnlyObservable<float> DestroyProgress => _destroyProgress;

        protected override void OnConnect(IItemTooltipFocusSource source)
        {
            _source = source;
            _source.FocusChanged += HandleFocusChanged;
        }

        protected override void OnDisconnect()
        {
            _source.FocusChanged -= HandleFocusChanged;
            _source = null;
            _target = null;
        }

        protected override void ResetState()
        {
            _item.Value = null;
            _worldPosition.Value = Vector2.zero;
            _destroyProgress.Value = 0f;
        }

        public void Tick()
        {
            if (_target == null) return;

            // 대상이 파괴됐으면(벗어남 알림 없이) 툴팁을 닫는다.
            if (_target is Object targetObject && targetObject == null)
            {
                SetTarget(null);
                return;
            }

            _worldPosition.Value = _target.TooltipPosition;
            _destroyProgress.Value = _target.DestroyProgress;
        }

        // 대상이 바뀌는 순서(새 대상 Focus → 이전 대상 Unfocus)와 상관없이, 지금 대상이 벗어날 때만 닫는다.
        private void HandleFocusChanged(IItemTooltipSource source, bool focused)
        {
            if (focused)
                SetTarget(source);
            else if (ReferenceEquals(_target, source))
                SetTarget(null);
        }

        // 위치를 먼저 넣어서, 아이템 알림을 받은 View가 툴팁을 바로 제자리에 띄우게 한다.
        private void SetTarget(IItemTooltipSource target)
        {
            _target = target;

            if (target == null)
            {
                _item.Value = null;
                _destroyProgress.Value = 0f;
                return;
            }

            _worldPosition.Value = target.TooltipPosition;
            _destroyProgress.Value = target.DestroyProgress;
            _item.Value = target.Item;
        }
    }
}
