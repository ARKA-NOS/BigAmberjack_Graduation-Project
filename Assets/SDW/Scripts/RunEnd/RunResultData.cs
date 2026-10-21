using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.RunEnd
{
    // 탐험 종료 화면에 표시할 내용. RunResultViewModel에 Bind하면 결과 화면이 열리고, null을 Bind하면 닫힌다.
    public class RunResultData
    {
        public RunEndReason Reason { get; }
        public int Gold { get; }
        public Sprite GoldIcon { get; }
        // 인벤토리 칸 순서대로의 아이콘. 빈 칸은 null.
        public IReadOnlyList<Sprite> ItemIcons { get; }
        // 처치한 적 수. 아직 집계 기능이 없으면 null.
        public int? KillCount { get; }
        // "다음" 버튼을 누르면 호출된다.
        public Action OnNext { get; }

        public RunResultData(RunEndReason reason, int gold, Sprite goldIcon, IReadOnlyList<Sprite> itemIcons, int? killCount, Action onNext)
        {
            Reason = reason;
            Gold = gold;
            GoldIcon = goldIcon;
            ItemIcons = itemIcons;
            KillCount = killCount;
            OnNext = onNext;
        }
    }
}
