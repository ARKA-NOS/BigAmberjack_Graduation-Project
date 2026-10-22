using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Lrw.Script.Agent
{
    public class Agent : ModuleOwner, IDamageable
    {
        [SerializeField] protected UnityEvent OnHit;

        protected IHealthModule HealthModule { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            HealthModule = GetModule<IHealthModule>();
            Debug.Assert(HealthModule != null, $"[{name}] HealthModule is not found");
        }

        public virtual void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            ApplyHealthDamage(damageData);
            OnHit?.Invoke();
        }

        /// <summary>
        /// 체력에 피해를 적용하고, 흡혈 비율이 있으면 실제로 깎인 체력만큼 공격자를 회복시킨다.
        /// </summary>
        protected HealthChangeResult ApplyHealthDamage(DamageData damageData)
        {
            HealthChangeResult result = HealthModule.TakeDamage(HealthChangeData.FromDamage(damageData));

            if (damageData.LifeStealRatio > 0f && result.Applied > 0f && damageData.Dealer != null
                && damageData.Dealer.TryGetModule(out IHealthModule dealerHealth))
            {
                float amount = result.Applied * damageData.LifeStealRatio;
                dealerHealth.Heal(HealthChangeData.Heal(amount, HealSourceType.LifeSteal, this));
            }

            return result;
        }
    }
}
