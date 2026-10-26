using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.DataStructure;
using Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI.New_HP_UI;
using Lrw.Script.Utility.ReadOnlySystem.Attributes;
using UnityEngine;
namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module,IAfterInitModule
    {
        [Header("Default Health Setting")]
        [SerializeField] private AbstractHealthSo coreHealth;
        [SerializeField, InspectorReadOnly] private float currentHealth;

        [Header("Other Setting")] 
        [SerializeField] private AbstractHealthSo[] otherHealths;

        [Header("UI Setting")] 
        [SerializeField] private NewHealthViewModel healthViewModel;
        
        private Stat _maxHpStat;
        
        private IHealth _baseHealth;
        private Dictionary<AbstractHealthSo, IHealth> _healthDict;
        private List<IHealth> _healths;
        
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

            foreach(IHealth health in _healthDict.Values)
            {
                _healths.Add(health);
            }
            
            _healths.Sort(Compere);
        }

        private void Start()
        {
            if(healthViewModel != null)
                healthViewModel.Bind(new HealthData(_healths.Count));
        }

        private static int Compere(IHealth a, IHealth b)
            => a.Priority.CompareTo(b.Priority);
        
        
        private void Update()
        {
            UpdateHealths();
            UpdateViewModule();
        }
        
        private void UpdateHealths()
        {
            foreach (var valuePair in _healths)
            {
                valuePair.HealthUpdate();
            }
        }
        
        private void UpdateViewModule()
        {
            if(healthViewModel == null) return;
            
            HealthData healthData = healthViewModel.BindData;

            healthData.Clear();

            foreach (IHealth health in _healths)
            {
                IRangeHealth rangeHealth = health as IRangeHealth;
                bool hasMaxHealth = rangeHealth != null;
                healthData.Add(new HealthViewData
                (
                    health.GetHealthColor(),
                    hasMaxHealth,
                    hasMaxHealth ? rangeHealth.HpGauge : new Gauge(health.HP,health.HP)
                ));
            }
        }
        
        public void ApplyDamage(float damage, HealthType targets)
        {
            float currentDamage = damage;
            for (int i = _healths.Count - 1; i >= 0; i--)
            {
                HealthType type = _healths[i].GetHealthType();
                if (!IsTargetHealth(type,targets)) continue;
                currentDamage = _healths[i].ApplyDamage(currentDamage);
            }
        }
        
        public void ApplyHeal(float heal, HealthType targets)
        {
            float currentHeal = heal;
            for (int i = 0; i < _healths.Count; i++)
            {
                HealthType type = _healths[i].GetHealthType();
                if (!IsTargetHealth(type,targets)) continue;
                currentHeal = _healths[i].ApplyHeal(currentHeal);
            }
        }
        
        private static bool IsTargetHealth(HealthType type, HealthType targets)
            => (type & targets) != HealthType.None;
        
    }
}