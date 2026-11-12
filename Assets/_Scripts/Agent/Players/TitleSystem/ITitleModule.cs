using System;
using System.Collections.Generic;

namespace _Scripts.Agent.Players.TitleSystem
{
    // 칭호 목록을 읽는 쪽(칭호 창 등)이 쓰는 인터페이스. 해금된 칭호만 들어 있다.
    public interface ITitleModule
    {
        // 칭호가 해금·달성되었거나, 퀘스트 진행도나 스테이지(진행 대상 칭호)가 바뀌었을 때.
        event Action OnTitlesChanged;

        IReadOnlyCollection<Title> Titles { get; }
    }
}
