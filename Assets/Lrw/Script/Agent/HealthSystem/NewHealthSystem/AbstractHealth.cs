using System;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 체력 하나의 런타임 상태.
    /// 피해/회복/갱신의 공통 흐름은 여기서 고정하고, 종류별 동작은 하위 클래스가 protected 훅을 오버라이드해서 만든다.
    /// </summary>
    public abstract class AbstractHealth : IHealth
    {
        private const float Epsilon = 0.0001f;

        protected readonly ModuleOwner Owner;
        private Stat _maxStat;
        private float _elapsed;

        public AbstractHealthSo Data { get; }
        public float Current { get; private set; }
        public float Max => _maxStat?.Value ?? Data.FixedMax;
        public float Normalized => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;
        public bool IsEmpty => Current <= Epsilon;

        /// <summary> 비었거나 지속시간이 끝나서 모듈이 제거해야 하는 상태 </summary>
        public virtual bool IsExpired =>
            (Data.RemoveWhenEmpty && IsEmpty) || (Data.Duration > 0f && _elapsed >= Data.Duration);

        /// <summary> (체력, 변화량). 최대치만 바뀌어도 호출된다 </summary>
        public event Action<AbstractHealth, float> OnValueChanged;

        protected virtual float InitialValue => Max * Data.StartRatio;

        protected AbstractHealth(AbstractHealthSo data, ModuleOwner owner)
        {
            Data = data;
            Owner = owner;
        }

        public void Init()
        {
            if (Data.MaxStat != null)
            {
                FDebug.Assert(Owner.TryGetModule(out IStatModule statModule),
                    $"[{Data.name}] owner is not have IStatModule");

                _maxStat = statModule.GetStat(Data.MaxStat);
                _maxStat.OnValueChanged += HandleMaxChanged;
            }

            Current = InitialValue;
            OnInit();
        }

        public void Dispose()
        {
            if (_maxStat != null)
                _maxStat.OnValueChanged -= HandleMaxChanged;
            OnDispose();
        }

        /// <summary> 같은 SO가 다시 추가됐을 때 </summary>
        public void Reapply()
        {
            _elapsed = 0f;
            OnReapply();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            OnTick(deltaTime);
        }

        /// <summary>
        /// 피해를 받고 잃은 체력을 돌려준다. data.Amount는 다음 체력으로 넘길 남은 원본 피해로 바뀐다.
        /// </summary>
        public float TakeDamage(ref HealthChangeData data)
        {
            HealthChangeData request = data;

            float layerDamage = Mathf.Max(0f, ToLayerDamage(data.Amount, request));
            float taken = Mathf.Min(Current, layerDamage);
            SetValue(Current - taken);
            data.Amount = Mathf.Max(0f, ToRawDamage(layerDamage - taken, request));

            if (taken > 0f) OnDamaged(taken, request);
            return taken;
        }

        /// <summary>
        /// 회복을 받고 찬 체력을 돌려준다. data.Amount는 다음 체력으로 넘길 남은 회복량으로 바뀐다.
        /// </summary>
        public float TakeHeal(ref HealthChangeData data)
        {
            if (!CanHeal(data)) return 0f;

            float healed = Mathf.Clamp(data.Amount, 0f, Mathf.Max(0f, Max - Current));
            if (healed <= 0f) return 0f;

            SetValue(Current + healed);
            data.Amount -= healed;
            OnHealed(healed, data);
            return healed;
        }

        /// <summary> 다른 체력이 막았더라도 주인이 피해를 받으면 모든 체력에 호출된다 </summary>
        public void NotifyOwnerDamaged(in HealthChangeData data)
            => OnOwnerDamaged(data);

        /// <summary> 피해/회복 흐름을 거치지 않고 값을 바로 바꾼다 (0 ~ Max) </summary>
        public void SetValue(float value)
        {
            float prevValue = Current;
            Current = Mathf.Clamp(value, 0f, Max);
            if (Mathf.Approximately(Current, prevValue)) return;

            OnValueChanged?.Invoke(this, Current - prevValue);
        }

        private void HandleMaxChanged(float newValue, float delta)
        {
            float prevValue = Current;
            float target = delta > 0f && Data.HealOnMaxIncrease ? Current + delta : Current;
            Current = Mathf.Clamp(target, 0f, Max);

            OnValueChanged?.Invoke(this, Current - prevValue);
        }

        #region Hooks

        /// <summary> 들어온 원본 피해 중 이 체력이 실제로 잃을 양 (피해 감소 등) </summary>
        protected virtual float ToLayerDamage(float rawDamage, in HealthChangeData data) => rawDamage;

        /// <summary> 이 체력이 다 못 받은 양을 다음 체력에 넘길 원본 피해로 되돌린다 </summary>
        protected virtual float ToRawDamage(float layerDamage, in HealthChangeData data) => layerDamage;

        protected virtual bool CanHeal(in HealthChangeData data) => (Data.HealSources & data.HealSource) != 0;

        protected virtual void OnInit() { }
        protected virtual void OnDispose() { }
        protected virtual void OnReapply() => SetValue(Mathf.Max(Current, InitialValue));
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnDamaged(float taken, in HealthChangeData data) { }
        protected virtual void OnHealed(float healed, in HealthChangeData data) { }
        protected virtual void OnOwnerDamaged(in HealthChangeData data) { }

        #endregion

        public override string ToString()
            => $"{Data.name} : {Current:0.##} / {Max:0.##}";
    }
}
