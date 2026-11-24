using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneChargeSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private EnemyAfterImageModule afterImages;
        [SerializeField, Min(0.1f)] private float distance = 3f;
        [SerializeField, Min(0.1f)] private float speed = 8f;
        [SerializeField, Min(0.01f)] private float easeDuration = 0.08f;
        [SerializeField, Min(0f)] private float damage = 14f;
        [SerializeField, Min(0f)] private float repeatDelay = 0.12f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.4f;

        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private ProductionControlDrone _drone;
        private Vector2 _previousCenter;
        private int _remaining;
        private bool _hitApplied;
        private bool _waiting;
        private bool _enhanced;
        private float _nextDashAt;
        private float _recoverUntil;

        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            _enhanced = _drone.UseLastStandPatterns;
            _remaining = _enhanced ? 2 : 1;
            _waiting = false;
            _drone.StopMovement();
            Enemy.FaceTarget();
            visual.Play("LaserCharge");
        }

        protected override void OnReleaseAttack()
        {
            CoolTimeStartTime = Time.time;
            StartDash();
        }

        private void StartDash()
        {
            Enemy.FaceTarget();
            float direction = Mathf.Sign(CastTarget.position.x - Enemy.transform.position.x);

            if (direction == 0f) direction = Enemy.FacingDirection;

            _drone.TryPlanGroundMove(direction, distance, out Vector2 destination);
            _drone.MoveGround(destination, speed * (_enhanced ? 1.5f : 1f), easeDuration);
            _previousCenter = _drone.ContactBounds.center;
            _hitApplied = false;
            _waiting = false;
            _remaining--;
            visual.Play("Laser");
            afterImages?.Play();
            NotifyAttackPerformed(Enemy.transform.position);
        }

        public override void FixedUpdateAttack()
        {
            if (!IsUsing || !HasReleased || IsRecovering || _waiting) return;

            _drone.TickGroundMovement();
            ApplySweep();
        }

        private void ApplySweep()
        {
            Bounds body = _drone.ContactBounds;
            Vector2 center = body.center;
            Vector2 size = (Vector2)body.size + new Vector2(Mathf.Abs(center.x - _previousCenter.x), 0f);
            Vector2 sweepCenter = (center + _previousCenter) * 0.5f;

            if (!_hitApplied)
            {
                Physics2D.OverlapBox(sweepCenter, size, 0f, new ContactFilter2D { useTriggers = true }, _hits);

                foreach (var hit in _hits)
                {
                    if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                    if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, damage, hit.ClosestPoint(center),
                        Vector2.right * Enemy.FacingDirection)) continue;

                    _hitApplied = true;
                    break;
                }
            }

            _previousCenter = center;
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            if (!IsUsing || !HasReleased) return;

            if (IsRecovering)
            {
                if (Time.time >= _recoverUntil) EndSkill();

                return;
            }

            if (_waiting)
            {
                if (Time.time >= _nextDashAt) StartDash();

                return;
            }

            if (_drone.IsMoving) return;

            ApplySweep();
            afterImages?.Stop();

            if (_remaining > 0)
            {
                _waiting = true;
                _nextDashAt = Time.time + repeatDelay;

                return;
            }

            if (!visual.HasFinished("Laser")) return;

            BeginRecovery();
            visual.Play("Idle");
            _recoverUntil = Time.time + recoveryTime;
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            afterImages?.Stop();
            _drone?.StopMovement();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            afterImages?.Stop();
            _drone?.StopMovement();
        }
    }
}
