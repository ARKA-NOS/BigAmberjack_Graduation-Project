using _Scripts.Agent.Players.TitleSystem;
using _Scripts.TitleCodex;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 칭호 도감 ViewModel이 보는 Model 묶음. 이번 런의 칭호(플레이어 칭호 모듈)와 런을 넘는 달성 이력(전역 매니저).
    public readonly struct TitleBookModel
    {
        public readonly ITitleModule Run;
        public readonly ITitleRecordManager Record;

        public TitleBookModel(ITitleModule run, ITitleRecordManager record)
        {
            Run = run;
            Record = record;
        }
    }

    // 도감 목록 범위.
    public enum TitleTab
    {
        Run = 0,     // 이번 런
        Earned = 1,  // 달성한 칭호
        All = 2,     // 전체 칭호
    }

    // 칭호 한 줄의 상태 문구.
    public enum TitleEntryStatus
    {
        RunInProgress, // 이번 런 진행 중
        RunCompleted,  // 이번 런 달성
        Earned,        // 달성 기록 있음
        NotEarned,     // 미달성
    }

    // 도감 달성 이력(달성 수 / 전체 수).
    public readonly struct TitleCompletionData
    {
        public readonly int Earned;
        public readonly int Total;

        public TitleCompletionData(int earned, int total)
        {
            Earned = earned;
            Total = total;
        }

        public float Ratio => Total > 0 ? (float)Earned / Total : 0f;
    }
}
