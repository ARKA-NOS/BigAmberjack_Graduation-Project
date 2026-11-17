using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.UI.RunEnd
{
    // 결과창 ViewModel이 읽는 Model. 탐험 종료 이벤트, 결과 값(소지금, 아이템, 처치 수), 다음 단계 진행을 한곳에 모은다.
    // RunResultConnector가 RunResultSource로 만든다.
    public interface IRunResultSource
    {
        // 탐험이 끝났을 때(보스 클리어, 플레이어 사망).
        event Action<RunEndReason> RunEnded;

        int Gold { get; }
        Sprite GoldIcon { get; }

        // 처치한 적 수. 집계하는 매니저가 없으면 null.
        int? KillCount { get; }

        // 인벤토리 칸 순서대로의 아이템 아이콘. 빈 칸은 null.
        IReadOnlyList<Sprite> GetItemIcons();

        // 결과 화면을 보는 동안 플레이어가 움직이지 않게 입력을 끈다.
        void LockPlayerInput();

        // 런 상태를 정리하고 다음 씬으로 넘어간다.
        void GoNext(RunEndReason reason);
    }
}
