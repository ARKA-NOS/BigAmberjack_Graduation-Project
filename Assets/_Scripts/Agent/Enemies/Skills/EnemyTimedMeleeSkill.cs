using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    // 짧은 시간 동안 판정을 유지하되, 플레이어에게는 한 번만 피해를 주는 근접 스킬입니다.
    public class EnemyTimedMeleeSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [SerializeField] private BoxCollider2D attackHitbox;
        [SerializeField] private string preparationStateName = "Base Layer.AttackPreparation";
        [SerializeField] private string attackStateName = "Base Layer.Attack";
        [SerializeField] private string idleStateName = "Base Layer.Idle";
        [SerializeField, Min(0f)] private float damage = 12f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.8f;
        [Tooltip("준비 시작 때 방향을 정하고, 타격과 후딜이 끝날 때까지 유지합니다.")]
        [SerializeField] private bool lockFacingDuringPreparation;
        [Tooltip("준비가 아닌 타격 시점부터 쿨타임을 계산합니다. 빗나가면 공격이 발동한 시점이 기준입니다.")]
        [SerializeField] private bool cooldownFromStrike;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private float _activeUntil;
        private float _recoveryUntil;
        private Vector2 _direction;
        private bool _hitApplied;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            attackHitbox != null && attackHitbox.enabled && attackHitbox.isTrigger &&
            attackHitbox.gameObject.activeInHierarchy;

        protected override void OnUseSkill()
        {
            float previousCooldownStart = CoolTimeStartTime;
            base.OnUseSkill();
            if (cooldownFromStrike) CoolTimeStartTime = previousCooldownStart;
        }

        protected override void PrepareAttack()
        {
            _hitApplied = false;
            if (lockFacingDuringPreparation)
            {
                Enemy.FaceTarget();
                _direction = Vector2.right * Enemy.FacingDirection;
            }
            PlayAnimation(preparationStateName);
        }

        public override void UpdateSkill()
        {
            if (!lockFacingDuringPreparation && IsPreparing && CanContinue) Enemy.FaceTarget();
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;
            if (Time.time < _activeUntil) TryHit();
            else if (!IsRecovering) BeginRecovery();
            if (Time.time >= _recoveryUntil) EndSkill();
        }

        protected override void OnReleaseAttack()
        {
            if (!lockFacingDuringPreparation)
            {
                Enemy.FaceTarget();
                _direction = Vector2.right * Enemy.FacingDirection;
            }
            if (cooldownFromStrike) CoolTimeStartTime = Time.time;
            _activeUntil = Time.time + Mathf.Max(0.01f, activeDuration);
            _recoveryUntil = _activeUntil + Mathf.Max(0f, recoveryTime);
            PlayAnimation(attackStateName);
            Telegraph?.Hide();
            TryHit();
        }

        private void TryHit()
        {
            if (_hitApplied || attackHitbox == null || !attackHitbox.enabled || CastTarget == null) return;
            if (lockFacingDuringPreparation &&
                Vector2.Dot((Vector2)(CastTarget.position - Enemy.transform.position), _direction) <= 0f) return;
            Physics2D.SyncTransforms();
            attackHitbox.Overlap(new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage,
                        hit.ClosestPoint(attackHitbox.bounds.center), _direction)) continue;
                _hitApplied = true;
                if (cooldownFromStrike) CoolTimeStartTime = Time.time;
                break;
            }
        }

        // 재사용한 그림의 Animation Event가 시간 기반 판정/후딜을 조기 종료하지 않게 합니다.
        public override void OnAttackHit() { }
        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            if (Enemy != null && !Enemy.IsDead) PlayAnimation(idleStateName);
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            if (Enemy != null && !Enemy.IsDead) PlayAnimation(idleStateName);
        }

        private void PlayAnimation(string stateName)
        {
            if (attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (!attackAnimator.HasState(0, hash)) return;
            attackAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            attackAnimator.Play(hash, 0, 0f);
        }
    }
}
