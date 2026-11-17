using System;
using _Scripts._Core._EventSystem;

namespace _Scripts.Item.ItemTooltip
{
    // ItemTooltipEvents로 보낸 알림(EventBus<ItemTooltipFocusEvent>)을 IItemTooltipFocusSource로 감싼다. ItemTooltipConnector가 만든다.
    public class EventBusItemTooltipFocusSource : IItemTooltipFocusSource
    {
        private Action<IItemTooltipSource, bool> _focusChanged;

        // 구독자가 생기면 EventBus를 구독하고, 모두 빠지면 해제한다. EventBus는 정적이라 씬이 바뀌어도 남으므로 해제가 중요하다.
        public event Action<IItemTooltipSource, bool> FocusChanged
        {
            add
            {
                if (_focusChanged == null) EventBus<ItemTooltipFocusEvent>.Subscribe(HandleFocus);
                _focusChanged += value;
            }
            remove
            {
                _focusChanged -= value;
                if (_focusChanged == null) EventBus<ItemTooltipFocusEvent>.UnSubscribe(HandleFocus);
            }
        }

        private void HandleFocus(ItemTooltipFocusEvent evt) => _focusChanged?.Invoke(evt.Source, evt.IsFocused);
    }
}
