using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class DefaultHealth : AbstractHealth
    {
        private readonly Color healthColor;
        
        public DefaultHealth(int priority, ModuleOwner owner, Color color) : base(priority, owner)
        {
            healthColor = color;
        }
        
        public override Color GetHealthColor() => healthColor;
    }
}