using _Scripts.Agent.Players.TitleSystem;
using _Scripts.TitleCodex;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 장착 ViewModel이 보는 Model 묶음.
    // 이번 런의 칭호(칭호 모듈), 장착 상태(장착 모듈), 런을 넘는 달성 이력과 전체 칭호 목록(기록 매니저).
    public readonly struct TitleEquipModel
    {
        public readonly ITitleModule Run;
        public readonly ITitleEquipModule Equip;
        public readonly ITitleRecordManager Record;

        public TitleEquipModel(ITitleModule run, ITitleEquipModule equip, ITitleRecordManager record)
        {
            Run = run;
            Equip = equip;
            Record = record;
        }
    }
}
