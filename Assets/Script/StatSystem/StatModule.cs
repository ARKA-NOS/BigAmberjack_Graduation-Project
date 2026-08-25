using System;
using System.Collections.Generic;
using Script.ModuleSystem;
using UnityEngine;

namespace Script.StatSystem
{
    public class StatModule : Module
    {
        [SerializeField] private StatOverride[] baseStats;
        
        private Dictionary<StatData,Stat> _stats = new();
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            foreach (StatOverride statOverride in baseStats)
            {
                if(statOverride == null || !statOverride.Check()) continue;
                AddStat(statOverride.StatData, statOverride.BaseValue);
            }
        }
        
        private void LateUpdate()
        {
            foreach (var stat in _stats.Values)
            {
                stat.UpdateValue();
            }
        }

        public Stat AddStat(StatData statData,float baseValue)
        {
            if (_stats.TryGetValue(statData, out Stat stat)) return stat;
            Stat newStat = new Stat(baseValue);
            _stats.Add(statData,newStat);
            return newStat;
        }
        
        public Stat GetStat(StatData statData, float baseValue = 0f)
            => _stats.TryGetValue(statData, out Stat stat) ? stat : AddStat(statData, baseValue);
    }
}