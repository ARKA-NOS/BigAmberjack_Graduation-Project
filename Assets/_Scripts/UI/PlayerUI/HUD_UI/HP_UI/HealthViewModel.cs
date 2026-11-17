using System.Collections.Generic;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.HealthSystem.NewHealthSystem.Healths;
using _Scripts.DataStructure;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.HP_UI
{
    // 플레이어 체력 HUD의 ViewModel. 체력 겹마다 색과 값을 Layers에 넣는다.
    // 피해/회복은 이벤트로 바로 반영하고, 시간에 따라 바뀌는 체력(자연 감소, 회복)은 Tick에서 반영한다.
    [CreateAssetMenu(fileName = "Health View Model", menuName = "UI/MVVM/Health View Model")]
    public class HealthViewModel : ViewModelBase<IHealthLayers>, ITickableViewModel
    {
        private readonly ObservableList<HealthLayerData> _layers = new();
        private readonly Observable<float> _missingHealth = new();
        private readonly List<HealthLayerData> _buffer = new();
        private IHealthLayers _model;

        // 체력 겹마다 하나. 우선순위 순서.
        public IReadOnlyObservableList<HealthLayerData> Layers => _layers;

        // 깎인 체력 양. 체력바 오른쪽 빈 칸의 크기로 쓴다.
        public IReadOnlyObservable<float> MissingHealth => _missingHealth;

        protected override void OnConnect(IHealthLayers model)
        {
            _model = model;
            _model.OnHealthChanged += HandleHealthChanged;
            Refresh();
        }

        protected override void OnDisconnect()
        {
            _model.OnHealthChanged -= HandleHealthChanged;
            _model = null;
        }

        protected override void ResetState()
        {
            _layers.Clear();
            _missingHealth.Value = 0f;
        }

        public void Tick() => Refresh();

        private void HandleHealthChanged(float current, float delta, float max) => Refresh();

        private void Refresh()
        {
            if (_model == null) return;

            _buffer.Clear();
            float sumCurrent = 0f;
            float sumRange = 0f;

            IReadOnlyList<IHealth> healths = _model.Healths;
            for (int i = 0; i < healths.Count; i++)
            {
                IHealth health = healths[i];
                IRangeHealth rangeHealth = health as IRangeHealth;
                bool showMax = rangeHealth != null && rangeHealth.ShowMaxHealth;
                Gauge gauge = rangeHealth?.HpGauge ?? new Gauge(health.HP, health.HP);
                AbstractHealthSo healthSo = _model.GetHealthSo(health);
                Color textColor = healthSo != null ? healthSo.HealthTextColor : Color.white;

                HealthLayerData layer = new(health.GetHealthColor(), textColor, gauge.CurrentValue, gauge.MaxValue, showMax);
                _buffer.Add(layer);

                if (layer.IsVisible) sumCurrent += layer.Current;
                sumRange += showMax ? gauge.MaxValue : gauge.CurrentValue;
            }

            SyncLayers(_buffer);
            _missingHealth.Value = sumRange - sumCurrent;
        }

        // 겹 수가 같으면 바뀐 겹만 Replace로 알리고, 달라지면 Reset 한 번으로 바꾼다.
        private void SyncLayers(List<HealthLayerData> next)
        {
            if (next.Count != _layers.Count)
            {
                _layers.SetAll(next);
                return;
            }

            for (int i = 0; i < next.Count; i++)
            {
                if (!_layers[i].Equals(next[i]))
                    _layers[i] = next[i];
            }
        }
    }
}
