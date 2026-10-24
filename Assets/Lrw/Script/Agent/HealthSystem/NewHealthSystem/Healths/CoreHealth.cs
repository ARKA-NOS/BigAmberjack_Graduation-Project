using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class CoreHealth : RangeHealth
    {
        private const float RedHealthLine = 0.3f;
        private const float OrangeHealthLine = 0.6f;
        
        public CoreHealth(ModuleOwner owner, Color color, StatData maxStatData) : base(owner, color, maxStatData)
        {
            
        }

        public override bool HealthUpdate()
            => UpdateHealthColor();
        
        private bool UpdateHealthColor()
        {
            float normalValue = Health.Normalized;
            
            Color prevColor = CurrentColor;
            
            CurrentColor = normalValue switch
            {
                <= RedHealthLine => Color.red,
                <= OrangeHealthLine => Color.orange,
                _ => Color.green
            };
            
            return CurrentColor != prevColor;
        }
        
    }
}