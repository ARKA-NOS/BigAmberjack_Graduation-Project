using System.Collections;
using System.Collections.Generic;
using DevLib.BattleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    public sealed class MeleeEnemy : EnemyBase
    {
        [Header("Melee Attack")]
        [SerializeField] private Animator attackAnimator;
        [Tooltip("레이어 이름을 포함한 Animator 상태 경로입니다.")]
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [SerializeField] private Collider2D attackHitbox;
        [SerializeField, Min(0f)] private float damage = 10f;
        [Tooltip("종료 이벤트가 누락됐을 때의 복구 시간. 실제 공격 애니메이션보다 길게 설정합니다.")]
        [SerializeField, Min(0.1f)] private float attackTimeout = 3f;

        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private Transform _attackTarget;
        private Coroutine _timeoutRoutine;
        private bool _isAttacking;
        private bool _hitEventConsumed;
        private bool _configurationWarningShown;

        public override bool IsAttackInProgress => _isAttacking;

        protected override void Attack()
        {
            int stateHash = Animator.StringToHash(attackStateName);
            if (attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || !attackAnimator.HasState(0, stateHash) ||
                attackHitbox == null || !attackHitbox.enabled || !attackHitbox.gameObject.activeInHierarchy ||
                !attackHitbox.isTrigger)
            {
                if (!_configurationWarningShown)
                {
                    Debug.LogWarning($"{name}: 근접 공격 Animator/Attack 상태/활성 Trigger Hitbox를 확인하세요.", this);
                    _configurationWarningShown = true;
                }
                return;
            }

            _attackTarget = Target;
            _hitEventConsumed = false;
            _isAttacking = true;
            // 화면 밖에서도 Animation Event가 멈추지 않게 합니다.
            attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attackAnimator.Play(stateHash, 0, 0f);
            _timeoutRoutine = StartCoroutine(WaitForAttackEnd());
        }

        // Animator가 같은 오브젝트에 있으면 직접, 자식이면 Relay로 호출합니다.
        public void OnAttackHit()
        {
            if (!isActiveAndEnabled || !_isAttacking || _hitEventConsumed)
                return;

            // 빗나가도 이 공격의 판정은 소모됩니다. 중복 이벤트로 다시 타격하지 않습니다.
            _hitEventConsumed = true;
            if (_attackTarget == null || attackHitbox == null || !attackHitbox.enabled ||
                !attackHitbox.gameObject.activeInHierarchy)
                return;

            // 애니메이션이 방금 바꾼 Hitbox 위치를 물리 질의에 반영합니다.
            Physics2D.SyncTransforms();
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            attackHitbox.Overlap(filter, _overlaps);
            Vector2 direction = (Vector2)(_attackTarget.position - transform.position);

            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, _attackTarget))
                    continue;

                if (EnemyDamageUtility.TryApply(hit, _attackTarget, this, damage,
                        hit.ClosestPoint(attackHitbox.bounds.center), direction))
                    break;
            }
        }

        public void OnAttackFinished()
        {
            _isAttacking = false;
            _attackTarget = null;
            if (_timeoutRoutine != null)
            {
                StopCoroutine(_timeoutRoutine);
                _timeoutRoutine = null;
            }
        }

        private IEnumerator WaitForAttackEnd()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, attackTimeout));
            _timeoutRoutine = null;
            _isAttacking = false;
            _attackTarget = null;
            Debug.LogWarning($"{name}: OnAttackFinished 이벤트가 없어 공격 잠금을 해제했습니다.", this);
        }

        private void OnDisable()
        {
            OnAttackFinished();
        }

        public override void ApplyDamage(DamageData damageData, Vector2 hitPoint,
            Vector2 hitDirection, Vector2 hitNormal)
        {
            // 현재 적의 체력/사망 규칙은 정해지지 않아 기존 피격 알림만 전달합니다.
            OnHit?.Invoke();
        }
    }
}
