using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO
{
    [CreateAssetMenu(fileName = "Damage Reduction Health SO", menuName = "Health/Damage Reduction Health SO")]
    public class DamageReductionHealthSo : AbstractHealthSo
    {
        [field: Header("피해 감소")]
        [field: Tooltip("받는 피해 배율. 0.5 = 반감, 2 = 두 배로 깎임, 0 = 남아 있는 동안 피해를 전부 막음")]
        [field: SerializeField, Min(0f)] public float DamageTakenRate { get; private set; } = 0.5f;

        public override AbstractHealth CreateHealth(ModuleOwner owner)
            => new DamageReductionHealth(this, owner);
    }
}
