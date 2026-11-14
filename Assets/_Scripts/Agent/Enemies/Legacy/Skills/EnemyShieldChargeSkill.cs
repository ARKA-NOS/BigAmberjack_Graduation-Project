using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class EnemyShieldChargeSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [SerializeField, Min(0f)] private float damage = 24f;
        [SerializeField, Min(0.01f)] private float chargeDistance = 6f;
        [SerializeField, Min(0.01f)] private float chargeDuration = 0.55f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.4f);
        [SerializeField, Min(0f)] private float hitboxForwardOffset = 0.6f;
        [SerializeField, Min(0f)] private float recoveryTime = 1.2f;
        [SerializeField, Min(0f)] private float parryStunDuration = 1f;

        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(8);
        private EnemyMovementModule _movement;
        private EnemyHitReactionModule _reaction;
        private Rigidbody2D _body;
        private Vector2 _direction;
        private float _startX;
        private float _chargeTimeLeft;
        private float _recoveryUntil;

        public override bool ControlsMovement => IsUsing && HasReleased && !IsRecovering;
        public override bool CompletesAfterTargetLost => HasReleased;
        internal override bool InterruptibleByHit => IsPreparing || ControlsMovement;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            _movement = owner.GetModule<EnemyMovementModule>();
            _reaction = owner.GetModule<EnemyHitReactionModule>();
            _body = owner.GetComponent<Rigidbody2D>();
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            _movement != null && _movement.IsGrounded && _body != null && _body.simulated;

        protected override void PrepareAttack() => PlayAnimation("Base Layer.ChargePreparation");

        public override void UpdateSkill()
        {
            if (IsPreparing && CanContinue) Enemy.FaceTarget();
            base.UpdateSkill();
            if (IsRecovering && Time.time >= _recoveryUntil) EndSkill();
        }

        protected override void OnReleaseAttack()
        {
            Enemy.FaceTarget();
            _direction = Vector2.right * Enemy.FacingDirection;
            _startX = _body.position.x;
            _chargeTimeLeft = Mathf.Max(0.01f, chargeDuration);
            PlayAnimation("Base Layer.Charge");
            Telegraph?.Hide();
        }

        public override void FixedUpdateAttack()
        {
            float distanceLeft = chargeDistance - Mathf.Abs(_body.position.x - _startX);
            if (_chargeTimeLeft <= 0f || distanceLeft <= 0.001f)
            {
                FinishCharge();
                return;
            }

            float stepTime = Mathf.Min(Time.fixedDeltaTime, _chargeTimeLeft);
            float step = Mathf.Min(distanceLeft, chargeDistance / Mathf.Max(0.01f, chargeDuration) * stepTime);
            float stepSpeed = step / Time.fixedDeltaTime;
            if (!_movement.CanMoveOnGround(_direction.x, stepSpeed) || HitPlayerInPath(step))
            {
                if (ControlsMovement) FinishCharge();
                return;
            }

            _body.linearVelocityX = _direction.x * stepSpeed;
            _chargeTimeLeft -= stepTime;
        }

        public bool TryParry()
        {
            if (!InterruptibleByHit || Enemy == null || Enemy.IsDead || _reaction == null ||
                !_reaction.TryStun(parryStunDuration)) return false;
            Enemy.CancelAttack();
            return true;
        }

        private bool HitPlayerInPath(float step)
        {
            if (CastTarget == null || !CastTarget.gameObject.activeInHierarchy) return false;

            Vector2 center = _movement.BodyCenter + _direction * hitboxForwardOffset;
            Physics2D.BoxCast(center, hitboxSize, 0f, _direction, new ContactFilter2D { useTriggers = true }, _hits, step);
            foreach (RaycastHit2D hit in _hits)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit.collider, CastTarget)) continue;
                Vector2 point = hit.collider.ClosestPoint(center);
                if (!_movement.HasClearPath(_movement.BodyCenter, point)) continue;
                EnemyDamageUtility.TryApply(hit.collider, CastTarget, Enemy, damage, point, _direction);
                return true;
            }
            return false;
        }

        private void FinishCharge()
        {
            _movement.Stop();
            BeginRecovery();
            _recoveryUntil = Time.time + Mathf.Max(0f, recoveryTime);
            PlayAnimation("Base Layer.ChargeRecovery");
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            ResetCharge();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            ResetCharge();
        }

        private void ResetCharge()
        {
            if (Enemy == null || Enemy.IsDead) return;
            if (_movement != null) _movement.Stop();
            PlayAnimation("Base Layer.Idle");
        }

        private void PlayAnimation(string state)
        {
            if (attackAnimator != null && attackAnimator.isActiveAndEnabled) attackAnimator.Play(state, 0, 0f);
        }
    }
}
