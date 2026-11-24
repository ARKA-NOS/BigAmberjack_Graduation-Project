using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneChargeSkill : EnemyAttackSkill
    {
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private EnemyAfterImageModule afterImages;
        [Tooltip("플레이어 거리와 명중 여부에 관계없이 이동할 목표 거리입니다. 벽·절벽·아레나 경계에서만 제한합니다.")]
        [SerializeField, Min(0.1f)] private float distance = 6.5f;
        [Tooltip("몸통 뒤쪽에서 제외할 비율입니다. 0.5이면 앞쪽 절반부터 경고와 피해 판정이 시작됩니다.")]
        [SerializeField, Range(0f, 0.95f)] private float damageStartRatio = 0.5f;
        [SerializeField, Min(0f)] private float damage = 18f;
        [SerializeField, Min(0f)] private float lastStandDamage = 22f;
        [Tooltip("돌진 동작의 스프라이트 재생 속도. 돌진 이동 시간과 별도로 조절합니다.")]
        [SerializeField, Min(0.1f)] private float dashAnimationSpeed = 2f;

        [Header("Normal Timeline")]
        [SerializeField, Min(0f)] private float followDuration = 0.65f;
        [SerializeField, Min(0f)] private float chargeDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.8f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.2f;

        [Header("Last Stand Timeline")]
        [SerializeField, Min(0f)] private float lastStandFollowDuration = 0.45f;
        [SerializeField, Min(0f)] private float lastStandChargeDuration = 0.46f;
        [SerializeField, Min(0.01f)] private float lastStandDashDuration = 0.55f;
        [SerializeField, Min(0f)] private float reaimDuration = 0.34f;
        [SerializeField, Min(0f)] private float lastStandRecoveryDuration = 0.2f;

        private enum Step { Following, Charging, FirstDash, Reaiming, SecondDash, Recovering }

        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private ProductionControlDrone _drone;
        private Step _step;
        private bool _enhanced, _hitApplied;
        private float _startedAt, _direction, _currentDamage;
        private float _chargeAt, _firstDashAt, _secondDashAt, _finishAt;
        private float _dashTime, _recoveryTime;
        private Vector2 _previousCenter;
        private Vector2 _dashDestination;

        public override bool ControlsMovement => true;
        protected override bool ShouldShowTelegraph => false;

        protected override void PrepareAttack()
        {
            _drone = (ProductionControlDrone)Enemy;
            _enhanced = _drone.UseLastStandPatterns;
            _currentDamage = _enhanced ? lastStandDamage : damage;
            _startedAt = Time.time;
            _chargeAt = _enhanced ? lastStandFollowDuration : followDuration;
            _firstDashAt = _chargeAt + (_enhanced ? lastStandChargeDuration : chargeDuration);
            _dashTime = Mathf.Max(0.01f, _enhanced ? lastStandDashDuration : dashDuration);
            _recoveryTime = _enhanced ? lastStandRecoveryDuration : recoveryDuration;
            _step = Step.Following;
            _drone.StopMovement();
            LockDirection();

            float gap = Mathf.Abs(CastTarget.position.x - Enemy.transform.position.x);
            float travel = Mathf.Min(Mathf.Max(0f, gap - _drone.ContactBounds.extents.x), Enemy.MoveSpeed * _chargeAt * 0.5f);
            if (_drone.TryPlanGroundMove(_direction, travel, out Vector2 destination))
                _drone.MoveGround(destination, Enemy.MoveSpeed);

            visual.Play("Idle");
        }

        private void LockDirection()
        {
            Enemy.FaceTarget();
            _direction = Enemy.FacingDirection;
        }

        protected override void OnReleaseAttack() => CoolTimeStartTime = Time.time;

        private void StartDash(Step step)
        {
            Telegraph?.Hide();

            _step = step;
            _drone.MoveGroundForDuration(_dashDestination, _dashTime);
            _previousCenter = GetDamageBounds().center;
            _hitApplied = false;
            CoolTimeStartTime = Time.time;
            visual.Play("GroundRush", dashAnimationSpeed);
            afterImages?.Play();
            NotifyAttackPerformed(Enemy.transform.position);
        }

        private void ShowDashWarning()
        {
            _drone.TryPlanGroundMove(_direction, distance, out _dashDestination);

            if (Telegraph == null) return;

            float offset = _dashDestination.x - Enemy.transform.position.x;
            Bounds bounds = GetDamageBounds();

            bounds.center += Vector3.right * (offset * 0.5f);
            bounds.size += Vector3.right * Mathf.Abs(offset);
            Telegraph.SetBounds(bounds);
            Telegraph.Show();
        }

        private Bounds GetDamageBounds()
        {
            Bounds bounds = _drone.ContactBounds;
            float removedWidth = bounds.size.x * Mathf.Clamp(damageStartRatio, 0f, 0.95f);

            bounds.center += Vector3.right * (_direction * removedWidth * 0.5f);
            bounds.size -= Vector3.right * removedWidth;

            return bounds;
        }

        public override void FixedUpdateAttack()
        {
            if (!IsUsing) return;

            if (_step == Step.Following || _step == Step.FirstDash || _step == Step.SecondDash)
                _drone.TickGroundMovement();

            if (_step == Step.FirstDash || _step == Step.SecondDash) ApplySweep();
        }

        private void ApplySweep()
        {
            Bounds body = GetDamageBounds();
            Vector2 center = body.center;
            Vector2 size = (Vector2)body.size + new Vector2(Mathf.Abs(center.x - _previousCenter.x), 0f);

            if (!_hitApplied)
            {
                Physics2D.OverlapBox((center + _previousCenter) * 0.5f, size, 0f,
                    new ContactFilter2D { useTriggers = true }, _hits);

                foreach (var hit in _hits)
                {
                    if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                    if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, _currentDamage, hit.ClosestPoint(center),
                        Vector2.right * _direction)) continue;

                    _hitApplied = true;
                    break;
                }
            }

            _previousCenter = center;
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();
            if (!IsUsing) return;

            float elapsed = Time.time - _startedAt;
            switch (_step)
            {
                case Step.Following:
                    if (elapsed < _chargeAt) return;
                    _drone.StopMovement();
                    _step = Step.Charging;
                    visual.Play("CrouchCharge", _enhanced ? 1.2f : 1f);
                    ShowDashWarning();
                    break;

                case Step.Charging:
                    if (elapsed >= _firstDashAt) StartDash(Step.FirstDash);
                    break;

                case Step.FirstDash:
                    if (_drone.IsMoving) return;
                    FinishDash();
                    if (_enhanced)
                    {
                        _step = Step.Reaiming;
                        _secondDashAt = elapsed + reaimDuration;
                        LockDirection();
                        visual.Play("Idle");
                        ShowDashWarning();
                    }
                    else StartRecovery();
                    break;

                case Step.Reaiming:
                    float previousDirection = _direction;

                    LockDirection();
                    if (_direction != previousDirection) ShowDashWarning();

                    if (elapsed >= _secondDashAt) StartDash(Step.SecondDash);
                    break;

                case Step.SecondDash:
                    if (_drone.IsMoving) return;
                    FinishDash();
                    StartRecovery();
                    break;

                case Step.Recovering:
                    if (elapsed >= _finishAt) EndSkill();
                    break;
            }
        }

        private void FinishDash()
        {
            ApplySweep();
            afterImages?.Stop();
            _drone.StopMovement();
        }

        private void StartRecovery()
        {
            _step = Step.Recovering;
            _finishAt = Time.time - _startedAt + _recoveryTime;
            BeginRecovery();
            visual.Play("Idle");
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            afterImages?.Stop();
            _drone?.StopMovement();
            visual?.Play("Idle");
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            afterImages?.Stop();
            _drone?.StopMovement();
            visual?.ResetPlaybackSpeed();
        }
    }
}
