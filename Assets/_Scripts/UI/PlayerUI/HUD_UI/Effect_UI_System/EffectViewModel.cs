using System;
using System.Collections.Generic;
using _Scripts.Agent.EffectSystem;
using _Scripts.DataStructure;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.Effect_UI_System
{
    // 플레이어에게 걸린 버프/디버프 목록의 ViewModel. 효과 데이터마다 아이콘 하나(겹친 수, 가장 짧은 남은 시간)를 만든다.
    // 효과는 이벤트 없이 생기고 사라지며 남은 시간도 계속 줄어들므로 Tick에서 매 프레임 다시 모은다.
    [CreateAssetMenu(fileName = "Effect View Model", menuName = "UI/MVVM/Effect View Model")]
    public class EffectViewModel : ViewModelBase<IEffectModule>, ITickableViewModel
    {
        private static readonly Comparison<KeyValuePair<AbstractEffectDataSo, List<IEffect>>> ByPriority =
            (x, y) => x.Key.Priority.CompareTo(y.Key.Priority);

        private readonly ObservableList<EffectViewData> _effects = new();
        private readonly List<EffectViewData> _buffer = new();
        private readonly List<KeyValuePair<AbstractEffectDataSo, List<IEffect>>> _sorted = new();
        private IEffectModule _model;

        // 지금 걸려 있는 효과. UI 우선순위 순서. 개수가 같으면 바뀐 칸만 Replace로 알린다.
        public IReadOnlyObservableList<EffectViewData> Effects => _effects;

        protected override void OnConnect(IEffectModule model)
        {
            _model = model;
            Tick();
        }

        protected override void OnDisconnect()
        {
            _model = null;
        }

        protected override void ResetState()
        {
            _effects.Clear();
        }

        public void Tick()
        {
            if (_model?.Effects == null) return;

            _sorted.Clear();
            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _model.Effects)
                _sorted.Add(pair);

            _sorted.Sort(ByPriority);

            _buffer.Clear();
            foreach (KeyValuePair<AbstractEffectDataSo, List<IEffect>> pair in _sorted)
            {
                AbstractEffectDataSo effectData = pair.Key;
                List<IEffect> list = pair.Value;

                if (list.Count == 0) continue;

                _buffer.Add(new EffectViewData
                {
                    EffectIcon = effectData.Sprite,
                    EffectGauge = GetShortestGauge(list),
                    EffectStack = list.Count,
                    EffectName = effectData.EffectName,
                    EffectDescription = effectData.EffectDescription,
                });
            }

            SyncEffects(_buffer);
        }

        // 개수가 같으면 바뀐 칸만 Replace로 알리고, 달라지면 Reset 한 번으로 바꾼다.
        private void SyncEffects(List<EffectViewData> next)
        {
            if (next.Count != _effects.Count)
            {
                _effects.SetAll(next);
                return;
            }

            for (int i = 0; i < next.Count; i++)
            {
                if (!_effects[i].Equals(next[i]))
                    _effects[i] = next[i];
            }
        }

        // 같은 효과가 여러 개 겹쳤으면 가장 먼저 끝나는 것의 남은 시간을 보여준다.
        private static Gauge GetShortestGauge(List<IEffect> list)
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

            return shortest != null
                ? new Gauge(minRemainingTime, shortest.Duration)
                : new Gauge(0, 0);
        }
    }
}
