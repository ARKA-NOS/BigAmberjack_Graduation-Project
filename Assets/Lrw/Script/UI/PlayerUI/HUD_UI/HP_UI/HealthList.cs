using System.Collections.Generic;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.DataStructure;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    /// <summary>
    /// 체력 모듈이 가진 모든 체력의 현재/최대값. HealthListViewModel로 UI에 넘긴다.
    /// </summary>
    public class HealthList
    {
        private readonly List<HealthViewData> _views = new();

        /// <summary> 기본 체력 → 추가 체력(Priority 오름차순) </summary>
        public IReadOnlyList<HealthViewData> Views => _views;
        public HealthViewData Base => _views.Count > 0 ? _views[0] : default;
        public Gauge Total { get; private set; }

        public void Refresh(IReadOnlyList<IHealth> healths)
        {
            _views.Clear();
            float current = 0f;
            float max = 0f;

            for (int i = 0; i < healths.Count; i++)
            {
                IHealth health = healths[i];
                _views.Add(new HealthViewData
                {
                    Data = health.Data,
                    Gauge = new Gauge(health.Current, health.Max),
                });

                current += health.Current;
                max += health.Max;
            }

            Total = new Gauge(current, max);
        }

        public bool TryGet(AbstractHealthSo healthSo, out HealthViewData view)
        {
            foreach (HealthViewData data in _views)
            {
                if (data.Data != healthSo) continue;
                view = data;
                return true;
            }

            view = default;
            return false;
        }

        /// <summary> 태그가 하나라도 겹치는 체력들의 합 (예: 보호막 전체) </summary>
        public Gauge GetTotal(HealthTag tags)
        {
            float current = 0f;
            float max = 0f;

            foreach (HealthViewData data in _views)
            {
                if ((data.Data.Tags & tags) == 0) continue;
                current += data.Gauge.CurrentValue;
                max += data.Gauge.MaxValue;
            }

            return new Gauge(current, max);
        }
    }
}
