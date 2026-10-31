using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module,IAfterInitModule, INewHealthModule
    {
        [Header("Default Health Setting")]
        [SerializeField] private AbstractHealthSo coreHealth;

        [Header("Other Health Setting")] 
        [SerializeField] private AbstractHealthSo[] otherHealths;

        [Header("Stat Setting")] 
        [SerializeField] private StatData defenseStatData;
        [SerializeField] private StatData damageReductionStatData;
        
        [Header("UI Setting")] 
        [SerializeField] private NewHealthViewModel healthViewModel;
        
        private IHealth _baseHealth;
        private Dictionary<AbstractHealthSo, IHealth> _healthDict;
        private Dictionary<IHealth, AbstractHealthSo> _healthSoDict;
        private List<IHealth> _healths;

        public float CurrentHealth => _healths.Sum(x => x.HP);
        public float MaxHealth =>
            _baseHealth is IRangeHealth rangeHealth 
                ? rangeHealth.HpGauge.MaxValue 
                : _baseHealth.HP;
        
        public event Action OnDead;
        public bool IsDead { get; private set; }
        
        public event HealthChanged OnHealthChanged;

        private IStatModule _statModule;
        private Stat _damageReductionStat;
        private Stat _defenseStat;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _healthDict = new();
            _healthSoDict = new();
            _healths = new();
            
            FDebug.Assert(coreHealth != null, "[NewHealthModule] coreHealth is null");
            
            FDebug.Assert(_owner.TryGetModule(out _statModule),"[NewHealthModule] IStatModule is not found");
            
        }
        
        public void AfterInit()
        {
            StatSetting();
            HealthSetting();
        }

        #region Setting

        private void StatSetting()
        {
            FDebug.Assert(defenseStatData == null || damageReductionStatData == null,$"[NewHealthModule : {gameObject.name}] StatDAta Setting Error");
            
            _defenseStat = _statModule.GetStat(defenseStatData);
            _damageReductionStat = _statModule.GetStat(damageReductionStatData);
        }

        private void HealthSetting()
        {
            _baseHealth = coreHealth.CreateHealth(_owner);
            _healthDict.Add(coreHealth, _baseHealth);
            _healthSoDict.Add(_baseHealth, coreHealth);
                
            foreach (AbstractHealthSo abstractHealthSo in otherHealths)
            {
                IHealth health = abstractHealthSo.CreateHealth(_owner);
                _healthDict.Add(abstractHealthSo,health);
                _healthSoDict.Add(health, abstractHealthSo);
            }

            foreach(IHealth health in _healthDict.Values)
            {
                _healths.Add(health);
            }
            
            _healths.Sort(Compere);
        }

        #endregion

        private void Start()
        {
            InitViewModel();
        }

        private void InitViewModel()
        {
            if(healthViewModel != null)
                healthViewModel.Bind(new HealthData(_healths.Count));
        }
        
        private void Update()
        {
            UpdateHealths();
            UpdateViewModule();
        }

        #region Update

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

            if (healthData == null)
            {
                InitViewModel();
                healthData = healthViewModel.BindData;
            }
            
            healthData.Clear();

            foreach (IHealth health in _healths)
            {
                AbstractHealthSo healthSo = _healthSoDict[health];
                
                healthData.Add(new HealthViewData
                (
                    healthSo,
                    health
                ));
            }
        }

        #endregion

        #region HealthChange

        #region Damage

        public void ApplyDamage(float damage, HealthType targets = HealthType.All)
        {
            if(IsDead) return;
            float prevHealth = CurrentHealth;
            float currentDamage = Mathf.Max(0f, GetApplyDamageValue(damage, _defenseStat.Value, _damageReductionStat.Value));
            for (int i = _healths.Count - 1; i >= 0; i--)
            {
                HealthType type = _healths[i].GetHealthType();
                if (!IsTargetHealth(type,targets)) continue;
                currentDamage = _healths[i].ApplyDamage(currentDamage);
            }

            HealthDeltaEvent(CurrentHealth - prevHealth);
        }

        private static float GetApplyDamageValue(float damage, float defense, float damageReduction)
            => damage * (100f / (100f +  defense)) * (1f - damageReduction);

        #endregion

        #region Heal

        public void ApplyHeal(float heal, HealthType targets = HealthType.Core)
        {
            if(IsDead) return;
            float prevHealth = CurrentHealth;
            float currentHeal = heal;
            for (int i = 0; i < _healths.Count; i++)
            {
                HealthType type = _healths[i].GetHealthType();
                if (!IsTargetHealth(type,targets)) continue;
                currentHeal = _healths[i].ApplyHeal(currentHeal);
            }
            HealthDeltaEvent(CurrentHealth - prevHealth);
        }

        #endregion
        
        private void HealthDeltaEvent(float delta)
        {
            float newCurrentHp = CurrentHealth;
            OnHealthChanged?.Invoke(newCurrentHp, delta, MaxHealth);

            if (newCurrentHp <= 0f)
            {
                OnDead?.Invoke();
                IsDead = true;
            }
        }
        
        private static bool IsTargetHealth(HealthType type, HealthType targets)
            => (type & targets) != HealthType.None;

        #endregion

        #region Healper

        private static int Compere(IHealth a, IHealth b)
            => a.Priority.CompareTo(b.Priority);

        #endregion
    }
}