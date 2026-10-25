using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;

namespace SDW.Scripts.Interact.Costs
{
    // HP를 소모하는 비용. 비용으로 죽지 않도록 현재 HP가 소모량보다 많을 때만 치를 수 있다.
    [CreateAssetMenu(fileName = "Cost_Hp", menuName = "Interact/Cost/Hp Cost", order = 2)]
    public class HpCostSO : ChoiceCostSO
    {
        [Tooltip("소모할 HP")]
        [SerializeField, Min(0f)] private float amount = 10f;

        public override bool CanPay(ModuleOwner owner)
        {
            return owner.TryGetModule(out IHealthModule health)
                   && !health.IsDead
                   && health.CurrentHealth > amount;
        }

        public override void Pay(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IHealthModule health))
                return;

            health.CurrentHealth -= amount;
            Debug.Log($"[Cost] HP 소모: -{amount}");
        }
    }
}
