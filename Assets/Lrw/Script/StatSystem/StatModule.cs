using System;
using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.StatSystem
{
    public class StatModule : Module, IStatModule
    {
        [SerializeField] private StatGroup baseStats;
        
        private Dictionary<StatData,Stat> _stats = new();
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            if(baseStats == null || baseStats.Stats == null) return;
            
            foreach (StatOverride statOverride in baseStats.Stats)
            {
                if(statOverride == null || statOverride.StatData == null) continue;
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
        
        private Stat AddStat(StatData statData,float baseValue)
        {
            if (statData == null) throw new Exception("StatData is null");
            if (_stats.TryGetValue(statData, out Stat stat)) return stat;
            stat = new Stat(statData,baseValue);
            _stats.Add(statData,stat);
            return stat;
        }

        public Stat GetStat(StatData statData, float baseValue = 0f)
        {
            if (statData == null) throw new Exception("StatData is null");
            return _stats.TryGetValue(statData, out Stat stat) ? stat : AddStat(statData, baseValue);
        }
        
    }
}