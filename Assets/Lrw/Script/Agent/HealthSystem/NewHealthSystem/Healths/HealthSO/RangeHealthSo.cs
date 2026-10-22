using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO
{
    [CreateAssetMenu(fileName = "Range Health SO", menuName = "Health/Range Health SO")]
    public class RangeHealthSo : AbstractHealthSo
    {
        [field:SerializeField] public StatData MaxHealthStatData { get; private set; }

        public override AbstractHealth CreateHealth(ModuleOwner owner)
            => new RangeHealth(this, owner, MaxHealthStatData.DefaultValue);
        
    }
}