using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class EnemyFlightDashSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [SerializeField, Min(0f)] private float damage = 11f;
        [SerializeField, Min(0.01f)] private float dashDistance = 2.4f;
        [SerializeField, Min(0.01f)] private float dashSpeed = 12f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.65f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(0.65f, 0.8f);

        private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        private static readonly int Prepare = Animator.StringToHash("Base Layer.DashPreparation");
        private static readonly int Strike = Animator.StringToHash("Base Layer.Dash");
        private static readonly int Recovery = Animator.StringToHash("Base Layer.Recovery");
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();
        private EnemyFlightModule _flight;
        private Rigidbody2D _body;
        private Vector2 _direction;
        private float _startX;
        private float _recoverUntil;
        private bool _moving;
        private bool _hit;

        public override bool ControlsMovement => IsUsing && _moving;
        public override bool CompletesAfterTargetLost => HasReleased;
        internal override bool InterruptibleByHit => IsPreparing || ControlsMovement;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            _flight = owner.GetModule<EnemyFlightModule>();
            _body = owner.GetComponent<Rigidbody2D>();
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            _flight != null && _flight.isActiveAndEnabled &&
            _flight.LimitTravel(DirectionToTarget(), 0.08f) > 0.04f;

        private Vector2 DirectionToTarget()
        {
            float dx = Enemy.Target.position.x - _body.position.x;
            return Vector2.right * (Mathf.Approximately(dx, 0f) ? Enemy.FacingDirection : Mathf.Sign(dx));
        }

        protected override void OnUseSkill()
        {
            float lastStrike = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = lastStrike;
        }

        protected override void PrepareAttack()
        {
            _flight.Stop();
            Play(Prepare);
        }

        protected override void OnReleaseAttack()
        {
            Enemy.FaceTarget();
            _direction = Vector2.right * Enemy.FacingDirection;
            _startX = _body.position.x;
            _hit = false;
            _moving = true;
            CoolTimeStartTime = Time.time;
            Play(Strike);
        }

        public override void FixedUpdateAttack()
        {
            float remaining = Mathf.Max(0f, dashDistance - (_body.position.x - _startX) * _direction.x);
            float step = _flight.LimitTravel(_direction, Mathf.Min(remaining, dashSpeed * Time.fixedDeltaTime));
            if (step <= 0.001f)
            {
                _flight.Stop();
                _moving = false;
                return;
            }

            if (!_hit && CanContinue) CheckHit(step);
            if (ControlsMovement) _body.linearVelocity = _direction * (step / Time.fixedDeltaTime);
        }

        private void CheckHit(float step)
        {
            Vector2 center = _flight.BodyCenter;
            Physics2D.BoxCast(center, hitboxSize, 0f, _direction,
                new ContactFilter2D { useTriggers = true }, _hits, step);
            foreach (RaycastHit2D hit in _hits)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit.collider, CastTarget)) continue;
                if (Vector2.Dot(_flight.GetTargetCenter(CastTarget) - center, _direction) < 0f) continue;
                Vector2 point = hit.collider.ClosestPoint(center);
                if (!_flight.HasClearPath(center, point) ||
                    !EnemyDamageUtility.TryApply(hit.collider, CastTarget, Enemy, damage, point, _direction))
                    continue;
                _hit = true;
                CoolTimeStartTime = Time.time;
                break;
            }
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased || _moving) return;
            if (!IsRecovering)
            {
                if (attackAnimator != null && attackAnimator.isActiveAndEnabled)
                {
                    AnimatorStateInfo state = attackAnimator.GetCurrentAnimatorStateInfo(0);
                    if (state.fullPathHash == Strike && state.normalizedTime < 1f) return;
                }
                BeginRecovery();
                _recoverUntil = Time.time + recoveryTime;
                Play(Recovery);
            }
            if (Time.time >= _recoverUntil) EndSkill();
        }

        private void Play(int state)
        {
            if (attackAnimator != null && attackAnimator.isActiveAndEnabled &&
                attackAnimator.runtimeAnimatorController != null && attackAnimator.HasState(0, state))
                attackAnimator.Play(state, 0, 0f);
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            ResetDash();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            ResetDash();
        }

        private void ResetDash()
        {
            _moving = false;
            if (_flight != null && Enemy != null && !Enemy.IsDead)
            {
                _flight.Stop();
                Play(Idle);
            }
        }
    }
}
