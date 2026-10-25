using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;
namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module
    {
        [SerializeField] private StatData maxHpStatData;
        
        [SerializeField] private float currentHealth;
        
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
            
            currentHealth = MaxHealth;
        }


        private void OnDestroy()
        {
            _maxHpStat.OnValueChanged -= MaxHpChanged;
        }

        private void MaxHpChanged(float newValue, float delta)
        {
            if(Mathf.Approximately(delta,0f)) return;
            currentHealth += Mathf.Max(delta,0f);
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
        }
        
    }
}