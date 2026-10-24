using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Utility.ReadOnlySystem.Attributes;
using UnityEngine;
namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module,IAfterInitModule
    {
        [Header("Default Health Setting")]
        [SerializeField] private AbstractHealthSo coreHealth;
        [SerializeField,InspectorReadOnly] private float currentHealth;

        [Header("Other Setting")] 
        [SerializeField] private AbstractHealthSo[] otherHealths;
        
        private Stat _maxHpStat;
        
        private IHealth _baseHealth;
        private Dictionary<AbstractHealthSo, IHealth> _healthDict;
        private List<KeyValuePair<AbstractHealthSo,IHealth>> _healths;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _healthDict = new();
            _healths = new();
            
            FDebug.Assert(coreHealth != null, "[NewHealthModule] coreHealth is null");
        }
        
        public void AfterInit()
        {
            _baseHealth = coreHealth.CreateHealth(_owner);
            _healthDict.Add(coreHealth, _baseHealth);

            foreach (AbstractHealthSo abstractHealthSo in otherHealths)
            {
                IHealth health = abstractHealthSo.CreateHealth(_owner);
                _healthDict.Add(abstractHealthSo,health);
            }

            foreach (KeyValuePair<AbstractHealthSo, IHealth> abstractHealth in _healthDict)
            {
                _healths.Add(abstractHealth);
            }
            
            _healths.Sort(Compere);
        }

        private static int Compere(KeyValuePair<AbstractHealthSo, IHealth> pair1, KeyValuePair<AbstractHealthSo, IHealth> pair2)
            => pair1.Key.Priority.CompareTo(pair2.Key.Priority);
        
        
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