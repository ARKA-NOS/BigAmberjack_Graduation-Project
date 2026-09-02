using DevLib.ModuleSystem;
using Lrw.Script._Core;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem
{
    public class HealthModule : Module, IHealthModule
    {
        [SerializeField] private StatData maxHpStatData;

        [SerializeField] private float currentHealth;

        public delegate void HealthChanged(float newValue, float delta);
        
        public event HealthChanged OnHealthChanged;
        
        public float CurrentHealth
        {
            get => currentHealth;
            set
            {
                float prevValue = currentHealth;
                currentHealth = Mathf.Clamp(value, 0, MaxHealth);
                if(Mathf.Approximately(currentHealth, prevValue)) return;
                OnHealthChanged?.Invoke(currentHealth, currentHealth - prevValue);
            }
        }
        
        public float MaxHealth => _maxHpStat?.Value ?? 0f;
        
        private IStatModule _statModule;
        private Stat _maxHpStat;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _statModule = owner.GetModule<IStatModule>();
            
            FDebug.Assert(_statModule != null,"StatModule is null");
            
            _maxHpStat = _statModule?.GetStat(maxHpStatData, 100f);
            
            FDebug.Assert(_maxHpStat != null,"MaxHpStat is null");

            _maxHpStat!.OnValueChanged += MaxHpChanged;

            CurrentHealth = MaxHealth;
        }

        private void OnDestroy()
        {
            _maxHpStat.OnValueChanged -= MaxHpChanged;
        }

        private void MaxHpChanged(float newValue, float delta)
        {
            if(Mathf.Approximately(delta,0f)) return;
            
            CurrentHealth += Mathf.Max(delta,0f);
        }
        
        public override string ToString()
            => $"[HealthModule] {CurrentHealth} / {MaxHealth}";
        
#if UNITY_EDITOR
        [ContextMenu("Debug HealthModule")]
        private void Debug() 
            => FDebug.Log(this);
#endif
        
    }
}