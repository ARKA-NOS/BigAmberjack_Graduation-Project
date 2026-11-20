using System;
using _Scripts.Agent.Players.TitleSystem;

namespace _Scripts.TitleCodex
{
    // 런을 넘어 남는 칭호 달성 이력(도감).
    public interface ITitleRecordManager
    {
        // 새 칭호가 기록되었거나 이력이 지워졌을 때.
        event Action OnRecordChanged;

        // 도감에 싣는 칭호 목록. 없으면 null.
        TitleCatalogSo Catalog { get; }

        int EarnedCount { get; }

        bool HasEarned(AbstractTitleSo title);
    }
}
