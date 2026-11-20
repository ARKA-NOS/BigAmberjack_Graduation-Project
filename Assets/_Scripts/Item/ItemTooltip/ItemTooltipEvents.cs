using _Scripts._Core._EventSystem;

namespace _Scripts.Item.ItemTooltip
{
    // 바닥 아이템이 상호작용 대상이 되거나 벗어날 때 알린다. 아이템은 이것만 부르고 툴팁 UI는 모른다.
    public static class ItemTooltipEvents
    {
        public static void Focus(IItemTooltipSource source)
            => EventBus<ItemTooltipFocusEvent>.Raise(new ItemTooltipFocusEvent(source, true));

        public static void Unfocus(IItemTooltipSource source)
            => EventBus<ItemTooltipFocusEvent>.Raise(new ItemTooltipFocusEvent(source, false));
    }

    public class ItemTooltipFocusEvent : IEvent
    {
        public IItemTooltipSource Source { get; }
        // false면 Source가 상호작용 대상에서 벗어난 것이다.
        public bool IsFocused { get; }

        public ItemTooltipFocusEvent(IItemTooltipSource source, bool isFocused)
        {
            Source = source;
            IsFocused = isFocused;
        }
    }
}
