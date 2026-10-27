using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.DataStructure;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    public struct HealthViewData
    {
        public AbstractHealthSo Data; // 이름, 색, 아이콘, 태그
        public Gauge Gauge;
    }
}
