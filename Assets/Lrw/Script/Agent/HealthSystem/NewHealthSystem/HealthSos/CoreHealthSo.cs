using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Core Health SO", menuName = "Health/Core Health SO")]
    public class CoreHealthSo : RangeHealthSo
    {
        
        public override IHealth CreateHealth(ModuleOwner owner)
            => new CoreHealth(owner, healthColor, maxHpStatData);
        
    }
}