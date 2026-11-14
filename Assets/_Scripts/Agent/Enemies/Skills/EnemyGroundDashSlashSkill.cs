using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class EnemyGroundDashSlashSkill : EnemyAttackSkill
    {
        [SerializeField] private Animator attackAnimator;
        [SerializeField, Min(0.1f)] private float attackRange = 3.8f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 11f;
        [SerializeField, Min(0f)] private float maximumHeightDifference = 0.55f;
        [SerializeField, Min(0.1f)] private float slashReach = 1.35f;
        [SerializeField, Min(0.1f)] private float slashHeight = 0.85f;
        [SerializeField, Min(0f)] private float damage = 16f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.85f;
        [Tooltip("1은 단발, 2는 첫 베기 후 경로가 확보되면 예고부터 한 번 더 반복합니다.")]
        [SerializeField, Range(1, 2)] private int maximumConsecutiveDashes = 1;

        private enum Phase { Preparing, Dashing, Slashing, Recovering }
        private static readonly int Preparation = Animator.StringToHash("Base Layer.DashPreparation");
        private static readonly int Dash = Animator.StringToHash("Base Layer.Dash");
        private static readonly int Slash = Animator.StringToHash("Base Layer.Slash");
        private static readonly int Recovery = Animator.StringToHash("Base Layer.Recovery");
        private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private EnemyMovementModule _movement;
        private Rigidbody2D _body;
        private Phase _phase;
        private float _direction;
        private float _dashEndX;
        private float _dashDeadline;
        private float _warningOriginX;
        private float _recoveryUntil;
        private int _slashes;

        public override bool ControlsMovement => IsUsing && _phase == Phase.Dashing;
        public override bool CompletesAfterTargetLost => HasReleased;
        internal override bool InterruptibleByHit => IsPreparing || ControlsMovement;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            _movement = owner.GetModule<EnemyMovementModule>();
            _body = owner.GetComponent<Rigidbody2D>();
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() &&
            TryPlan(Enemy.Target, out _, out _);

        private bool TryPlan(Transform target, out float direction, out float distance)
        {
            direction = Enemy.FacingDirection;
            distance = 0f;
            if (target == null || _movement == null || !_movement.IsGrounded ||
                Mathf.Abs(_movement.GetTargetHeightDifference(target)) > maximumHeightDifference) return false;

            float offsetX = target.position.x - _body.position.x;
            float targetDistance = Mathf.Abs(offsetX);
            if (targetDistance > attackRange || !_movement.HasClearPath(_movement.BodyCenter, target.position +
                Vector3.up * (_movement.BodyCenter.y - _body.position.y))) return false;

            if (Mathf.Abs(offsetX) > 0.01f) direction = Mathf.Sign(offsetX);
            float approach = Mathf.Min(Mathf.Max(0f, attackRange - slashReach), Mathf.Max(0f, targetDistance - slashReach * 0.65f));
            distance = _movement.GetGroundTravelDistance(direction, approach);
            return targetDistance <= distance + slashReach;
        }

        protected override void OnUseSkill()
        {
            _slashes = 0;
            float lastStrike = CoolTimeStartTime;
            base.OnUseSkill();
            CoolTimeStartTime = lastStrike;
        }

        protected override void PrepareAttack()
        {
            if (!TryPlan(CastTarget, out _direction, out float distance))
            {
                StopSkill();
                return;
            }
            _movement.Stop();
            Enemy.FaceTarget();
            _warningOriginX = _body.position.x;
            _dashEndX = _warningOriginX + _direction * distance;
            _phase = Phase.Preparing;
            Play(Preparation);
        }

        protected override void UpdateTelegraph()
        {
            float length = Mathf.Abs(_dashEndX - _warningOriginX) + slashReach;
            Telegraph.SetBounds(new Bounds(new Vector3(_warningOriginX + _direction * length * 0.5f, _movement.BodyCenter.y),
                new Vector3(length, slashHeight)));
        }

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            _phase = Phase.Dashing;
            _dashDeadline = Time.time + Mathf.Abs(_dashEndX - _body.position.x) / dashSpeed + 2f * Time.fixedDeltaTime;
            Play(Dash);
        }

        public override void FixedUpdateAttack()
        {
            float remaining = (_dashEndX - _body.position.x) * _direction;
            float step = _movement.GetGroundTravelDistance(_direction, Mathf.Min(remaining, dashSpeed * Time.fixedDeltaTime));
            if (step > 0.001f && Time.time < _dashDeadline)
            {
                _body.linearVelocityX = _direction * step / Time.fixedDeltaTime;
                return;
            }

            _movement.Stop();
            _phase = Phase.Slashing;
            _slashes++;
            CoolTimeStartTime = Time.time;
            if (Play(Slash)) attackAnimator.Update(0f);
            StrikeTarget();
        }

        private void StrikeTarget()
        {
            if (!CanContinue) return;
            Vector2 origin = _movement.BodyCenter;
            Vector2 direction = Vector2.right * _direction;
            Vector2 center = origin + direction * (slashReach * 0.5f);
            Physics2D.OverlapBox(center, new Vector2(slashReach, slashHeight), 0f,
                new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                Vector2 point = hit.ClosestPoint(center);
                if (_movement.HasClearPath(origin, point) &&
                    EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage, point, direction)) break;
            }
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing || !HasReleased) return;
            if (_phase == Phase.Slashing)
            {
                if (attackAnimator != null && attackAnimator.isActiveAndEnabled)
                {
                    var state = attackAnimator.GetCurrentAnimatorStateInfo(0);
                    if (state.fullPathHash == Slash && state.normalizedTime < 1f) return;
                }
                if (_slashes < Mathf.Clamp(maximumConsecutiveDashes, 1, 2) && CanContinue &&
                    TryPlan(CastTarget, out _, out _))
                {
                    BeginPreparation();
                    return;
                }
                _phase = Phase.Recovering;
                BeginRecovery();
                _recoveryUntil = Time.time + recoveryTime;
                Play(Recovery);
            }
            if (_phase == Phase.Recovering && Time.time >= _recoveryUntil) EndSkill();
        }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            ResetPose();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            ResetPose();
        }

        private void ResetPose()
        {
            _phase = Phase.Preparing;
            _movement?.Stop();
            if (Enemy != null && !Enemy.IsDead) Play(Idle);
        }

        private bool Play(int state)
        {
            if (attackAnimator == null || !attackAnimator.isActiveAndEnabled ||
                attackAnimator.runtimeAnimatorController == null || !attackAnimator.HasState(0, state)) return false;
            attackAnimator.Play(state, 0, 0f);
            return true;
        }
    }
}
