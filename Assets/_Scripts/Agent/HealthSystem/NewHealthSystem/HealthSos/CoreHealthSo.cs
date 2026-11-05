using _Scripts.Agent.HealthSystem.NewHealthSystem.Healths;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Core Health SO", menuName = "Health/Core Health SO")]
    public class CoreHealthSo : RangeHealthSo
    {
        
        public override IHealth CreateHealth(ModuleOwner owner)
            => new CoreHealth(Priority, owner, HealthType, healthColor, maxHpStatData);
        
    }
}