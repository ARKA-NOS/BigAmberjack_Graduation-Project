using System.Collections.Generic;
using UnityEngine;

namespace CTJ.Enemies.Skills
{
    public sealed class EnemyMeleeSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [SerializeField] private Collider2D attackHitbox;
        [SerializeField, Min(0f)] private float damage = 10f;
        [Tooltip("종료 이벤트 누락 복구 시간. 실제 공격 클립보다 길게 둡니다.")]
        [SerializeField, Min(0.1f)] private float attackTimeout = 1.5f;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private bool _hitEventConsumed;
        private float _endDeadline;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            attackAnimator != null && attackAnimator.isActiveAndEnabled && attackAnimator.runtimeAnimatorController != null &&
            attackAnimator.HasState(0, Animator.StringToHash(attackStateName)) && attackHitbox != null &&
            attackHitbox.enabled && attackHitbox.gameObject.activeInHierarchy && attackHitbox.isTrigger;

        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            _hitEventConsumed = false;
            _endDeadline = Time.time + Mathf.Max(0.1f, attackTimeout);
            attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attackAnimator.Play(Animator.StringToHash(attackStateName), 0, 0f);
        }

        public override void OnAttackHit()
        {
            if (!IsUsing || !CanContinue || _hitEventConsumed) return;
            _hitEventConsumed = true; // 빗나가도 소모하여 중복 이벤트의 재타격을 막습니다.
            if (attackHitbox == null || !attackHitbox.enabled || !attackHitbox.gameObject.activeInHierarchy) return;
            Physics2D.SyncTransforms();
            attackHitbox.Overlap(new ContactFilter2D { useTriggers = true }, _overlaps);
            Vector2 direction = CastTarget.position - Enemy.transform.position;
            foreach (Collider2D hit in _overlaps)
                if (EnemyDamageUtility.IsTargetCollider(hit, CastTarget) &&
                    EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage,
                        hit.ClosestPoint(attackHitbox.bounds.center), direction)) break;
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || Time.time < _endDeadline) return;
            EndSkill();
            Debug.LogWarning($"{name}: OnAttackFinished 이벤트가 없어 근접 스킬을 종료했습니다.", this);
        }
    }
}
