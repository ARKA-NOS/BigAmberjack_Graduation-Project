using System.Collections.Generic;
using Lrw.Script.Agent.EffectSystem;
using Lrw.Script.DataStructure;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System
{
    public class EffectList
    {
        // 매 프레임 정렬하므로 비교자를 한 번만 만들어 둔다 (메서드 그룹 전달 시 매번 델리게이트 할당)
        private static readonly IComparer<KeyValuePair<AbstractEffectDataSo, List<IEffect>>> PriorityComparer =
            Comparer<KeyValuePair<AbstractEffectDataSo, List<IEffect>>>.Create(
                (x, y) => x.Key.Priority.CompareTo(y.Key.Priority));

        public readonly List<EffectViewData> ViewDataList;
        private readonly Dictionary<AbstractEffectDataSo, List<IEffect>> _effects;
        private readonly List<KeyValuePair<AbstractEffectDataSo, List<IEffect>>> _sortedEffects;

        public EffectList(Dictionary<AbstractEffectDataSo, List<IEffect>> dict)
        {
            _effects = dict;
            ViewDataList = new();
            _sortedEffects = new();
        }

        public void Update()
        {
            ViewDataList.Clear();
            _sortedEffects.Clear();

            // AddRange는 런타임에 따라 임시 배열을 할당할 수 있어서 직접 추가
            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _effects)
                _sortedEffects.Add(pair);

            _sortedEffects.Sort(PriorityComparer);

            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _sortedEffects)
            {
                AbstractEffectDataSo effectData = pair.Key;
                List<IEffect> list = pair.Value;

                // 모든 스택이 끝난 효과는 표시하지 않는다
                if (list.Count == 0) continue;

                EffectViewData viewData = new EffectViewData
                {
                    EffectIcon = effectData.Sprite,
                    EffectGauge = GetEffectGauge(list),
                    EffectStack = list.Count,
                    EffectName = effectData.EffectName,
                    EffectDescription = effectData.EffectDescription,
                };

                ViewDataList.Add(viewData);
            }
        }

        // 가장 먼저 끝나는 스택 기준 게이지. 시간제 효과가 없으면 빈 게이지
        private static Gauge GetEffectGauge(List<IEffect> list)
        {
            ITimedEffect shortest = null;
            float minRemainingTime = float.MaxValue;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not ITimedEffect timedEffect) continue;

                float remainingTime = timedEffect.RemainingTime;
                if (remainingTime >= minRemainingTime) continue;

                minRemainingTime = remainingTime;
                shortest = timedEffect;
            }

            return shortest != null ? new Gauge(minRemainingTime, shortest.Duration) : new Gauge(0, 0);
        }
    }
}