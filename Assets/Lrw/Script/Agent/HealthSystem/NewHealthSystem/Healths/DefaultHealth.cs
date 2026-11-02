using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths
{
    public class DefaultHealth : AbstractHealth
    {
        private readonly Color healthColor;
        private readonly HealthType healthType;
        public DefaultHealth(int priority, ModuleOwner owner, HealthType type, Color color) : base(priority, owner)
        {
            healthColor = color;
            healthType = type;
        }
        
        public override Color GetHealthColor() => healthColor;
        public override HealthType GetHealthType() => healthType;
    }
}