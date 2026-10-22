using System.Collections.Generic;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.DataStructure;
using Unity.Collections;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.HP_UI
{
    public class HealthBarData
    {
        public readonly List<(Color,Gauge)> Gauges;
        private readonly List<AbstractHealth> _sortHealths;
        private readonly Dictionary<AbstractHealthSo, AbstractHealth> dict;
        
        public HealthBarData(Dictionary<AbstractHealthSo, AbstractHealth> healths)
        {
            Gauges = new();
            _sortHealths = new();
            dict = healths;
        }
        
        public void Update()
        {
            Gauges.Clear();
            _sortHealths.Clear();

            foreach (var pair in dict)
            {
                _sortHealths.Add(pair.Value);
            }
            
            _sortHealths.Sort(HealthComparison);

            
            
            
        }
        
        private static int HealthComparison(AbstractHealth a, AbstractHealth b)
            => a.HealthSo.Priority.CompareTo(b.HealthSo.Priority);
        
    }
}