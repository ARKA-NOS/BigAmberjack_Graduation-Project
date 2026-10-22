using System;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.DataStructure;
using Lrw.Script.UI.BindSystem.ViewModel;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem
{
    public class HealthModule : Module, IHealthModule
    {
        [SerializeField] private GaugeViewModel healthGaugeViewModel;
        [SerializeField] private StatData maxHpStatData;
        
        [SerializeField] private float currentHealth;

        public event HealthChanged OnHealthChanged;
        public event Action OnDead;
        public bool IsDead { get; private set; }
        
        public float CurrentHealth
        {
            get =>  currentHealth;
            set
            {
                float prevValue = currentHealth;
                currentHealth = Mathf.Clamp(value, 0f, MaxHealth);
                if(Mathf.Approximately(currentHealth, prevValue)) return;
                OnHealthChanged?.Invoke(currentHealth, currentHealth - prevValue,MaxHealth);
                
                if (!IsDead && Mathf.Approximately(currentHealth, 0f))
                {
                    IsDead = true;
                    OnDead?.Invoke();
                }
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
            
            _maxHpStat = _statModule.GetStat(maxHpStatData);
            
            FDebug.Assert(_maxHpStat != null,"MaxHpStat is null");

            _maxHpStat.OnValueChanged += MaxHpChanged;
            
            CurrentHealth = MaxHealth;
            
            OnHealthChanged += ViewModelUpdate;
            
            IsDead = false;
        }

        private void Start()
        {
            ViewModelUpdate(CurrentHealth, 0f, MaxHealth);
        }

        private void OnDestroy()
        {
            _maxHpStat.OnValueChanged -= MaxHpChanged;
            OnHealthChanged -= ViewModelUpdate;
        }

        private void MaxHpChanged(float newValue, float delta)
        {
            if(Mathf.Approximately(delta,0f)) return;
            
            CurrentHealth += Mathf.Max(delta,0f);
        }

        private void ViewModelUpdate(float newValue, float delta, float max)
        {
            if (healthGaugeViewModel != null)
            {
                healthGaugeViewModel.Bind(new Gauge(newValue,max));
            }
        }
        
        public override string ToString()
            => $"{CurrentHealth} / {MaxHealth}";
        
#if UNITY_EDITOR
        [ContextMenu("Debug HealthModule")]
        private void Debug() => FDebug.Log(this);
#endif
        
    }
}