using _Scripts.Agent.HealthSystem.NewHealthSystem.Healths;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Range Health SO", menuName = "Health/Range Health SO")]
    public class RangeHealthSo : AbstractHealthSo
    {
        [SerializeField] protected StatData maxHpStatData;
        [SerializeField] protected Color healthColor;
        
        public override IHealth CreateHealth(ModuleOwner owner)
            => new RangeHealth(Priority, owner, HealthType, healthColor, maxHpStatData);
    }
}