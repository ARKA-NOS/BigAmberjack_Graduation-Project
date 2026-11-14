using _Scripts.Agent.Enemies.Skills;
using _Scripts.DevLib.BattleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    public sealed class EnemyShieldbearer : EnemyBase
    {
        private const float SideTolerance = 0.01f;

        [SerializeField, Range(0f, 1f)] private float frontDamageReduction = 0.7f;
        private EnemySkillModule _shieldSkills;

        protected override void Awake()
        {
            base.Awake();
            _shieldSkills = GetModule<EnemySkillModule>();
        }

        public bool TryParryCharge() => _shieldSkills?.ActiveSkill is EnemyShieldChargeSkill charge && charge.TryParry();

        protected override float ModifyIncomingDamage(DamageData damage, Vector2 hitPoint, Vector2 hitDirection,
            out bool suppressHitControl)
        {
            bool guardRaised = !IsHitReacting && _shieldSkills?.ActiveSkill is not EnemyAttackSkill { IsRecovering: true };
            suppressHitControl = guardRaised && SourceSide(damage, hitPoint, hitDirection) * FacingDirection > SideTolerance;
            return suppressHitControl ? damage.DamageAmount * (1f - Mathf.Clamp01(frontDamageReduction)) : damage.DamageAmount;
        }

        private float SourceSide(DamageData damage, Vector2 hitPoint, Vector2 hitDirection)
        {
            float centerX = transform.position.x;
            if (damage.Dealer != null)
            {
                float dealerSide = damage.Dealer.transform.position.x - centerX;
                if (Mathf.Abs(dealerSide) > SideTolerance) return dealerSide;
            }

            float contactSide = hitPoint.x - centerX;
            return Mathf.Abs(contactSide) > SideTolerance ? contactSide : -hitDirection.x;
        }
    }
}
