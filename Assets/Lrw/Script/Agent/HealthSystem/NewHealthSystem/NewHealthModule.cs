using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Utility.ReadOnlySystem.Attributes;
using UnityEngine;
namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module,IAfterInitModule
    {
        [SerializeField,InspectorReadOnly] private float currentHealth;
        
        [SerializeField] private StatData maxHpStatData;
        
        private Stat _maxHpStat;
        
        private AbstractHealth _baseHealth;
        private List<KeyValuePair<AbstractHealthSo,AbstractHealth>> _healths;
        private Dictionary<AbstractHealthSo, AbstractHealth> _healthDict;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            _healths = new();
        }
        
        public void AfterInit()
        {
            _baseHealth = new RangeHealth(_owner, maxHpStatData);
        }
        private void Update()
        {
            UpdateHealths();
        }
        

        private void UpdateHealths()
        {
            
        }

        public void ApplyDamage(float damage)
        {
            
            
        }

        public void ApplyHeal(float heal)
        {
            
            
        }

        
    }
}