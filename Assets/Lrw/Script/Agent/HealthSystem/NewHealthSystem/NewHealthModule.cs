using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public class NewHealthModule : Module
    {
        [SerializeField] private AbstractHealthSo[] healthSos;

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
        
        private void InitHealth()
        {
            foreach (AbstractHealthSo healthSo in healthSos)
            {
                AddHealth(healthSo);
            }
        }

        private void SortHealthList()
        {
            if(!_listDirty) return;
            _healths.Sort(HealthComparison);
        }

        private void UpdateHealth()
        {
            foreach (AbstractHealth health in _healths)
            {
                health.Update();
            }
        }
        
        private void AddHealth(AbstractHealthSo healthSo)
        {
            if(_healthDict.ContainsKey(healthSo)) return;
            AbstractHealth health = healthSo.CreateHealth(_owner);
            _healthDict[healthSo] = health;
            _healths.Add(health);
            _listDirty = true;
        }

        
        private static int HealthComparison(AbstractHealth a, AbstractHealth b)
            => a.Priority.CompareTo(b.Priority);
        
    }
}
