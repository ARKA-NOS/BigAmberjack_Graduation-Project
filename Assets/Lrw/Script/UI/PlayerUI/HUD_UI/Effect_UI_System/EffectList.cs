using System.Collections.Generic;
using Lrw.Script.Agent.EffectSystem;
using Lrw.Script.DataStructure;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System
{
    public class EffectList
    {
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
            
            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _effects)
                _sortedEffects.Add(pair);

            _sortedEffects.Sort(PriorityComparer);

            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _sortedEffects)
            {
                AbstractEffectDataSo effectData = pair.Key;
                List<IEffect> list = pair.Value;
                
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
        
        private static Gauge GetEffectGauge(List<IEffect> list)
        {
            ITimedEffect shortest = null;
            float minRemainingTime = float.MaxValue;

            foreach (IEffect effect in list)
            {
                if (effect is not ITimedEffect timedEffect) continue;

                float remainingTime = timedEffect.RemainingTime;
                if (remainingTime >= minRemainingTime) continue;

                minRemainingTime = remainingTime;
                shortest = timedEffect;
            }

            return shortest != null ?
                new Gauge(minRemainingTime, shortest.Duration) 
                : new Gauge(0, 0);
        }
    }
}