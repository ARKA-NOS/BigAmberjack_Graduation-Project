using System;
using System.Collections.Generic;
using System.Text;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem.ViewModel;
using Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 기본 체력 1개 위에 추가 체력(보호막 등)을 겹쳐 관리한다.
    /// 피해 : 추가 체력(Priority 높은 순) → 기본 체력
    /// 회복 : 기본 체력 → 추가 체력(Priority 낮은 순)
    /// IHealthModule의 CurrentHealth / MaxHealth / OnHealthChanged는 기본 체력 기준이다.
    /// </summary>
    public class NewHealthModule : Module, IAfterInitModule, IHealthModule, IHealthLayerModule
    {
        private const float Epsilon = 0.0001f;

        [SerializeField] private AbstractHealthSo baseHealthSo;
        [SerializeField] private AbstractHealthSo[] healthSos;

        [Header("UI")]
        [SerializeField] private GaugeViewModel baseGaugeViewModel;
        [SerializeField] private HealthListViewModel healthListViewModel;

        private AbstractHealth _baseHealth;
        private readonly List<AbstractHealth> _layers = new(); // 피해 순서(Priority 내림차순), 기본 체력 제외
        private readonly List<IHealth> _healths = new();       // 기본 체력 → 추가 체력(Priority 오름차순)
        private readonly Dictionary<AbstractHealthSo, AbstractHealth> _healthDict = new();

        // 피해/회복/갱신 도중에 들어온 추가/제거는 처리가 끝난 뒤 반영한다
        private readonly List<AbstractHealth> _pendingAdd = new();
        private readonly List<AbstractHealth> _pendingRemove = new();
        private int _processDepth;

        private readonly HealthList _healthList = new();
        private bool _viewDirty;

        public event HealthChanged OnHealthChanged;
        public event Action OnDead;
        public event Action<IHealth, float> OnAnyHealthChanged;
        public event Action<IHealth> OnHealthAdded;
        public event Action<IHealth> OnHealthRemoved;

        public bool IsDead { get; private set; }

        /// <summary> 기본 체력을 피해/회복 흐름 없이 바로 설정한다 </summary>
        public float CurrentHealth
        {
            get => _baseHealth?.Current ?? 0f;
            set => _baseHealth?.SetValue(value);
        }

        public float MaxHealth => _baseHealth?.Max ?? 0f;

        public IHealth BaseHealth => _baseHealth;
        public IReadOnlyList<IHealth> Healths => _healths;

        public float TotalHealth
        {
            get
            {
                float total = 0f;
                foreach (IHealth health in _healths)
                    total += health.Current;
                return total;
            }
        }

        // 스탯 모듈 초기화 순서와 상관없도록 AfterInit에서 체력을 만든다
        public void AfterInit()
        {
            FDebug.Assert(baseHealthSo != null, $"[{name}] baseHealthSo is null");

            IsDead = false;
            _baseHealth = CreateHealth(baseHealthSo);
            RebuildHealths();

            if (healthSos == null) return;
            foreach (AbstractHealthSo healthSo in healthSos)
                AddHealth(healthSo);
        }

        private void Start()
            => RefreshView();

        private void Update()
        {
            if (IsDead || _baseHealth == null) return;

            float deltaTime = Time.deltaTime;

            _processDepth++;
            _baseHealth.Tick(deltaTime);
            foreach (AbstractHealth layer in _layers)
                layer.Tick(deltaTime);
            _processDepth--;

            FlushPending();
        }

        private void LateUpdate()
        {
            if (_viewDirty) RefreshView();
        }

        private void OnDestroy()
        {
            _baseHealth?.Dispose();
            foreach (AbstractHealth layer in _layers)
                layer.Dispose();
            foreach (AbstractHealth health in _pendingAdd)
                health.Dispose();
        }

        #region Damage / Heal

        public HealthChangeResult TakeDamage(HealthChangeData data)
        {
            if (IsDead || _baseHealth == null || !data.IsValid) return default;

            HealthChangeData remaining = data;
            float applied = 0f;

            _processDepth++;

            foreach (AbstractHealth layer in _layers)
            {
                if (remaining.Amount <= Epsilon) break;
                if (layer.IsEmpty || IsIgnored(layer, data)) continue;

                applied += layer.TakeDamage(ref remaining);
            }

            if (remaining.Amount > Epsilon && !IsIgnored(_baseHealth, data))
                applied += _baseHealth.TakeDamage(ref remaining);

            _baseHealth.NotifyOwnerDamaged(data);
            foreach (AbstractHealth layer in _layers)
                layer.NotifyOwnerDamaged(data);

            _processDepth--;
            FlushPending();

            return new HealthChangeResult(applied, remaining.Amount);
        }

        public HealthChangeResult Heal(HealthChangeData data)
        {
            if (IsDead || _baseHealth == null || !data.IsValid) return default;

            HealthChangeData remaining = data;

            _processDepth++;

            float applied = _baseHealth.TakeHeal(ref remaining);
            for (int i = _layers.Count - 1; i >= 0 && remaining.Amount > Epsilon; i--)
                applied += _layers[i].TakeHeal(ref remaining);

            _processDepth--;
            FlushPending();

            return new HealthChangeResult(applied, remaining.Amount);
        }

        private static bool IsIgnored(AbstractHealth health, in HealthChangeData data)
            => (health.Data.Tags & data.IgnoreTags) != 0;

        #endregion

        #region Add / Remove

        public IHealth AddHealth(AbstractHealthSo healthSo)
        {
            if (healthSo == null) return null;

            if (_healthDict.TryGetValue(healthSo, out AbstractHealth exist))
            {
                if (exist != _baseHealth) exist.Reapply();
                return exist;
            }

            AbstractHealth health = CreateHealth(healthSo);
            _pendingAdd.Add(health);
            FlushPending();
            return health;
        }

        public bool RemoveHealth(AbstractHealthSo healthSo)
        {
            if (healthSo == null || !_healthDict.TryGetValue(healthSo, out AbstractHealth health)) return false;

            if (health == _baseHealth)
            {
                FDebug.LogWarning($"[{name}] 기본 체력은 제거할 수 없습니다.");
                return false;
            }

            _healthDict.Remove(healthSo);

            // 아직 리스트에 들어가지 않은 체력은 바로 정리한다
            if (_pendingAdd.Remove(health))
            {
                health.OnValueChanged -= HandleValueChanged;
                health.Dispose();
                return true;
            }

            _pendingRemove.Add(health);
            FlushPending();
            return true;
        }

        public bool TryGetHealth(AbstractHealthSo healthSo, out IHealth health)
        {
            health = null;
            if (healthSo == null || !_healthDict.TryGetValue(healthSo, out AbstractHealth found)) return false;

            health = found;
            return true;
        }

        private AbstractHealth CreateHealth(AbstractHealthSo healthSo)
        {
            AbstractHealth health = healthSo.CreateHealth(_owner);
            health.Init();
            health.OnValueChanged += HandleValueChanged;
            _healthDict[healthSo] = health;
            return health;
        }

        private void FlushPending()
        {
            if (_processDepth > 0) return;

            List<AbstractHealth> removed = null;
            List<AbstractHealth> added = null;

            foreach (AbstractHealth health in _pendingRemove)
            {
                if (_layers.Remove(health))
                    (removed ??= new()).Add(health);
            }
            _pendingRemove.Clear();

            // 비었거나 지속시간이 끝난 체력 정리
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                AbstractHealth layer = _layers[i];
                if (!layer.IsExpired) continue;

                _layers.RemoveAt(i);
                if (_healthDict.TryGetValue(layer.Data, out AbstractHealth registered) && registered == layer)
                    _healthDict.Remove(layer.Data);
                (removed ??= new()).Add(layer);
            }

            if (_pendingAdd.Count > 0)
            {
                added = new List<AbstractHealth>(_pendingAdd);
                _pendingAdd.Clear();
                foreach (AbstractHealth health in added)
                    InsertLayer(health);
            }

            if (removed == null && added == null) return;

            RebuildHealths();
            _viewDirty = true;

            if (removed != null)
            {
                foreach (AbstractHealth health in removed)
                {
                    health.OnValueChanged -= HandleValueChanged;
                    health.Dispose();
                    OnHealthRemoved?.Invoke(health);
                }
            }

            if (added != null)
            {
                foreach (AbstractHealth health in added)
                    OnHealthAdded?.Invoke(health);
            }
        }

        // 같은 Priority끼리는 먼저 들어온 쪽이 먼저 피해를 받는다
        private void InsertLayer(AbstractHealth health)
        {
            int index = _layers.FindIndex(layer => layer.Data.Priority < health.Data.Priority);
            _layers.Insert(index < 0 ? _layers.Count : index, health);
        }

        private void RebuildHealths()
        {
            _healths.Clear();
            if (_baseHealth != null) _healths.Add(_baseHealth);
            for (int i = _layers.Count - 1; i >= 0; i--)
                _healths.Add(_layers[i]);
        }

        #endregion

        private void HandleValueChanged(AbstractHealth health, float delta)
        {
            _viewDirty = true;
            OnAnyHealthChanged?.Invoke(health, delta);

            if (health != _baseHealth) return;

            OnHealthChanged?.Invoke(health.Current, delta, health.Max);

            if (!IsDead && health.IsEmpty)
            {
                IsDead = true;
                OnDead?.Invoke();
            }
        }

        private void RefreshView()
        {
            _viewDirty = false;
            if (_baseHealth == null) return;

            if (baseGaugeViewModel != null)
                baseGaugeViewModel.Bind(new Gauge(_baseHealth.Current, _baseHealth.Max));

            if (healthListViewModel != null)
            {
                _healthList.Refresh(_healths);
                healthListViewModel.Bind(_healthList);
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new($"[{name}] IsDead : {IsDead}, Total : {TotalHealth:0.##}");
            foreach (IHealth health in _healths)
                builder.Append($"\n{health}");
            return builder.ToString();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Take 10 Damage")]
        private void DebugTakeDamage() => DebugLogResult("Damage", TakeDamage(HealthChangeData.Damage(10f)));

        [ContextMenu("Debug/Heal 10")]
        private void DebugHeal() => DebugLogResult("Heal", Heal(HealthChangeData.Heal(10f)));

        [ContextMenu("Debug/LifeSteal Heal 10")]
        private void DebugLifeStealHeal()
            => DebugLogResult("LifeSteal", Heal(HealthChangeData.Heal(10f, HealSourceType.LifeSteal)));

        [ContextMenu("Debug/Log Healths")]
        private void DebugLogHealths() => FDebug.Log(this);

        private void DebugLogResult(string label, HealthChangeResult result)
            => FDebug.Log($"[{label}] {result}\n{this}");
#endif
    }
}
