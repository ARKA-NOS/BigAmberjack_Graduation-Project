using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO
{
    [CreateAssetMenu(fileName = "Regen Health SO", menuName = "Health/Regen Health SO")]
    public class RegenHealthSo : AbstractHealthSo
    {
        [field: Header("재생")]
        [field: Tooltip("마지막 피해 후 이 시간(초)이 지나야 재생을 시작한다")]
        [field: SerializeField, Min(0f)] public float RegenDelay { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float RegenPerSecond { get; private set; } = 10f;
        [field: Tooltip("켜면 다른 체력이 막은 피해에도 타이머가 리셋된다. 끄면 이 체력이 깎일 때만 리셋")]
        [field: SerializeField] public bool ResetOnOwnerDamaged { get; private set; } = true;

        public override AbstractHealth CreateHealth(ModuleOwner owner)
            => new RegenHealth(this, owner);
    }
}
