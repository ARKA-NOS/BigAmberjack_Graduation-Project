using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module,IHealthModule
    {
        [SerializeField] private AbstractHealthSo baseHealthSo;
        [SerializeField] private AbstractHealthSo[] healthSos;
        
        private AbstractHealth baseHealth;
        
        private readonly List<AbstractHealth> _healths = new();
        private readonly Dictionary<AbstractHealthSo, AbstractHealth> _healthDict = new();

        private bool _listDirty = false;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            InitHealth();
        }
        
        private void Update()
        {
            SortHealthList();
            UpdateHealth();
        }

        private void OnDestroy()
        {
            
        }

        private void InitHealth()
        {
            baseHealth = AddHealth(baseHealthSo);
            
            _healths.Add(baseHealth);

            foreach (AbstractHealthSo healthSo in healthSos)
            {
                AbstractHealth health = AddHealth(healthSo);
                _healths.Add(health);
            }
            
            HealthChanged();
        }

        

        private void SortHealthList()
        {
            if(!_listDirty) return;
            _healths.Sort(HealthComparison);
        }

        private void UpdateHealth()
        {
            bool healthChanged = false;
            foreach (AbstractHealth health in _healths)
            {
                healthChanged |= health.UpdateHealth();
            }

            if (healthChanged)
            {
                HealthChanged();
            }
        }
        
        private AbstractHealth AddHealth(AbstractHealthSo healthSo)
        {
            if(_healthDict.TryGetValue(healthSo,out AbstractHealth health)) return health;
            health = healthSo.CreateHealth(_owner);
            _healthDict[healthSo] = health;
            _healths.Add(health);
            health.Init();
            _listDirty = true;
            return health;
        }

        
        private static int HealthComparison(AbstractHealth a, AbstractHealth b)
            => a.HealthSo.Priority.CompareTo(b.HealthSo.Priority);
        
        private void ApplyDamage(HealthChangeData healthChangeData)
        {
            HealthChangeData currentChangeData = healthChangeData;
            for (int i = _healths.Count - 1; i >= 0; i++)
            {
                currentChangeData = _healths[i].ApplyDamage(currentChangeData);

                if (Mathf.Approximately(currentChangeData.Amount, 0f)) break;
            }
            HealthChanged();
        }
        
        private void ApplyHeal(HealthChangeData healthChangeData)
        {
            HealthChangeData currentChangeData = healthChangeData;
            for (int i = 0; i < _healths.Count; i++)
            {
                currentChangeData = _healths[i].ApplyHeal(currentChangeData);

                if (Mathf.Approximately(currentChangeData.Amount, 0f)) break;
            }
            HealthChanged();
        }
        
        private void HealthChanged()
        {
            FDebug.Log("Health Changed");
        }

        public float CurrentHealth { get; set; }
        public float MaxHealth { get; }
        public event HealthModule.HealthChanged OnHealthChanged;
        public event Action OnDead;
        public bool IsDead { get; }
    }
}
