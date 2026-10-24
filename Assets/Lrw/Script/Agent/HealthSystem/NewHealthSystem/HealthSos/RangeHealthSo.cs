using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.HealthSos
{
    [CreateAssetMenu(fileName = "Range Health SO", menuName = "Health/Range Health SO")]
    public class RangeHealthSo : AbstractHealthSo
    {
        [SerializeField] protected StatData maxHpStatData;
        
        public override IHealth CreateHealth(ModuleOwner owner)
            => new RangeHealth(owner,HealthColor,maxHpStatData);
    }
}