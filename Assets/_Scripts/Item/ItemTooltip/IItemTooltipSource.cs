using UnityEngine;

namespace _Scripts.Item.ItemTooltip
{
    // 바닥에 떨어진 아이템 툴팁이 읽어 가는 값. 툴팁은 위치·파괴 진행도를 매 프레임 다시 읽는다.
    public interface IItemTooltipSource
    {
        ItemSO Item { get; }

        // 툴팁이 따라다닐 월드 좌표.
        Vector2 TooltipPosition { get; }

        // 파괴 키 홀드 진행도(0~1).
        float DestroyProgress { get; }
    }
}
