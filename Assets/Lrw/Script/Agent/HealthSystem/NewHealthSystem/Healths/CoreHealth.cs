using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class CoreHealth : RangeHealth
    {
        private const float RedHealthLine = 0.3f;
        private const float OrangeHealthLine = 0.6f;
        
        public CoreHealth(int priority, ModuleOwner owner, Color color, StatData maxStatData) : base(priority, owner, color, maxStatData)
        {
            HP = MaxHealthValue;
        }

        public override void HealthUpdate()
            => UpdateHealthColor();
        
        private void UpdateHealthColor()
        {
            float normalValue = HpGauge.Normalized;
            
            CurrentColor = normalValue switch
            {
                <= RedHealthLine => Color.red,
                <= OrangeHealthLine => Color.orange,
                _ => Color.green
            };
        }
        
    }
}