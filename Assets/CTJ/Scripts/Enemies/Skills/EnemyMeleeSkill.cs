using System.Collections.Generic;
using UnityEngine;

namespace CTJ.Enemies.Skills
{
    public sealed class EnemyMeleeSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [Tooltip("선택 사항. 준비 시작에 재생하고 마지막 자세를 유지하는 상태. 자동 Attack 전환은 넣지 않습니다.")]
        [SerializeField] private string preparationStateName = "";
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [SerializeField] private string idleStateName = "Base Layer.Idle";
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
            (string.IsNullOrEmpty(preparationStateName) || attackAnimator.HasState(0, Animator.StringToHash(preparationStateName))) &&
            attackHitbox.enabled && attackHitbox.gameObject.activeInHierarchy && attackHitbox.isTrigger;

        protected override void PrepareAttack()
        {
            _hitEventConsumed = false;
            if (!string.IsNullOrEmpty(preparationStateName))
            {
                attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                attackAnimator.Play(Animator.StringToHash(preparationStateName), 0, 0f);
            }
        }

        protected override void UpdateTelegraph()
        {
            if (attackHitbox is BoxCollider2D box) Telegraph.SetBox(box);
            else if (attackHitbox != null) Telegraph.SetBounds(attackHitbox.bounds);
        }

        protected override void OnReleaseAttack()
        {
            if (attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || attackHitbox == null ||
                !attackHitbox.enabled || !attackHitbox.gameObject.activeInHierarchy ||
                !attackAnimator.HasState(0, Animator.StringToHash(attackStateName)))
            {
                StopSkill();
                return;
            }
            _endDeadline = Time.time + Mathf.Max(0.1f, attackTimeout);
            attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attackAnimator.Play(Animator.StringToHash(attackStateName), 0, 0f);
        }

        public override void OnAttackHit()
        {
            if (!IsUsing || !HasReleased || !CanContinue || _hitEventConsumed) return;
            _hitEventConsumed = true; // 빗나가도 소모하여 중복 이벤트의 재타격을 막습니다.
            Telegraph?.Hide();
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
            if (IsUsing && (attackAnimator == null || !attackAnimator.isActiveAndEnabled || attackAnimator.runtimeAnimatorController == null))
            {
                StopSkill();
                return;
            }
            if (!IsUsing || !HasReleased || Time.time < _endDeadline) return;
            EndSkill();
            Debug.LogWarning($"{name}: OnAttackFinished 이벤트가 없어 근접 스킬을 종료했습니다.", this);
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            ReturnToIdle();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            ReturnToIdle();
        }

        private void ReturnToIdle()
        {
            if (Enemy == null || Enemy.IsDead || attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(idleStateName)) return;
            int idleHash = Animator.StringToHash(idleStateName);
            if (attackAnimator.HasState(0, idleHash)) attackAnimator.Play(idleHash, 0, 0f);
        }
    }
}
