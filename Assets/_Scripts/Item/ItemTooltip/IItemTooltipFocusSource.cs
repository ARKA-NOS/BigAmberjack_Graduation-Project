using System;

namespace _Scripts.Item.ItemTooltip
{
    // 바닥 아이템 툴팁 ViewModel이 읽는 Model. 어떤 아이템이 상호작용 대상이 되었는지/벗어났는지 알려 준다.
    public interface IItemTooltipFocusSource
    {
        // (아이템, 대상이 되었는지). false면 그 아이템이 대상에서 벗어난 것이다.
        event Action<IItemTooltipSource, bool> FocusChanged;
    }
}
