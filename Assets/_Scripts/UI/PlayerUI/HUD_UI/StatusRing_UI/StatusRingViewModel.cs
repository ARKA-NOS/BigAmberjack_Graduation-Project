using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.StatusRing_UI
{
    // 원형 HP/MP 게이지 HUD의 ViewModel. 체력과 마나를 각각 현재값/최대값/비율(0~1)로 내보낸다.
    // 피해/회복/소비는 이벤트로 바로 반영하고, 시간에 따라 바뀌는 체력(자연 감소, 회복)은 Tick에서 반영한다.
    [CreateAssetMenu(fileName = "Status Ring View Model", menuName = "UI/MVVM/Status Ring View Model")]
    public class StatusRingViewModel : ViewModelBase<StatusRingSource>, ITickableViewModel
    {
        private readonly Observable<float> _hpCurrent = new();
        private readonly Observable<float> _hpMax = new();
        private readonly Observable<float> _hpRatio = new();

        private readonly Observable<bool> _hasMana = new();
        private readonly Observable<float> _mpCurrent = new();
        private readonly Observable<float> _mpMax = new();
        private readonly Observable<float> _mpRatio = new();

        private StatusRingSource _source;

        public IReadOnlyObservable<float> HpCurrent => _hpCurrent;
        public IReadOnlyObservable<float> HpMax => _hpMax;
        public IReadOnlyObservable<float> HpRatio => _hpRatio;

        // 마나 모듈이 있는지. 없으면 View가 MP 표시를 숨긴다.
        public IReadOnlyObservable<bool> HasMana => _hasMana;
        public IReadOnlyObservable<float> MpCurrent => _mpCurrent;
        public IReadOnlyObservable<float> MpMax => _mpMax;
        public IReadOnlyObservable<float> MpRatio => _mpRatio;

        protected override void OnConnect(StatusRingSource source)
        {
            _source = source;
            _source.Health.OnHealthChanged += HandleHealthChanged;
            if (_source.Mana != null)
                _source.Mana.OnManaChanged += HandleManaChanged;

            Refresh();
        }

        protected override void OnDisconnect()
        {
            _source.Health.OnHealthChanged -= HandleHealthChanged;
            if (_source.Mana != null)
                _source.Mana.OnManaChanged -= HandleManaChanged;

            _source = default;
        }

        // OnEnable에서도 불리므로 Observable만 건드린다(_source는 비어 있을 수 있다).
        protected override void ResetState()
        {
            _hpCurrent.Value = 0f;
            _hpMax.Value = 0f;
            _hpRatio.Value = 0f;

            _hasMana.Value = false;
            _mpCurrent.Value = 0f;
            _mpMax.Value = 0f;
            _mpRatio.Value = 0f;
        }

        public void Tick() => Refresh();

        private void HandleHealthChanged(float current, float delta, float max) => RefreshHealth();

        private void HandleManaChanged(float current, float delta, float max) => RefreshMana();

        private void Refresh()
        {
            if (_source.Health == null) return;

            RefreshHealth();
            RefreshMana();
        }

        // 겹별 체력의 합을 직접 더한다(INewHealthModule.CurrentHealth는 매번 LINQ로 할당한다).
        private void RefreshHealth()
        {
            if (_source.Health == null || _source.Layers == null) return;

            float current = 0f;
            var healths = _source.Layers.Healths;
            for (int i = 0; i < healths.Count; i++)
                current += healths[i].HP;

            float max = _source.Health.MaxHealth;
            _hpCurrent.Value = current;
            _hpMax.Value = max;
            _hpRatio.Value = ToRatio(current, max);
        }

        private void RefreshMana()
        {
            bool hasMana = _source.Mana != null;
            _hasMana.Value = hasMana;
            if (!hasMana) return;

            float current = _source.Mana.CurrentMana;
            float max = _source.Mana.MaxMana;
            _mpCurrent.Value = current;
            _mpMax.Value = max;
            _mpRatio.Value = ToRatio(current, max);
        }

        // 추가 체력 때문에 현재값이 최대치를 넘을 수 있어서 0~1로 자른다.
        private static float ToRatio(float current, float max)
            => max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }
}
