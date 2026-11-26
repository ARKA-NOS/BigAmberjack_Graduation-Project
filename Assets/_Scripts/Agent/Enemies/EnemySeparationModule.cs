using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // AI가 정한 이동에 간격 보정만 더합니다. 적끼리 물리 충돌을 켜지 않습니다.
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class EnemySeparationModule : Module
    {
        [Tooltip("몸체 사이에 확보할 여유 거리입니다. 유닛 단위입니다.")]
        [SerializeField, Min(0f)] private float minimumGap = 0.15f;
        [SerializeField, Min(0f)] private float separationSpeed = 1.5f;
        [SerializeField, Min(0.01f)] private float acceleration = 12f;
        [Tooltip("지상 적은 높이 차이가 이 값 이내인 적과만 거리를 벌립니다.")]
        [SerializeField, Min(0f)] private float maximumHeightDifference = 0.6f;

        private static readonly List<EnemySeparationModule> Members = new List<EnemySeparationModule>();

        private EnemyBase _enemy;
        private EnemyMovementModule _movement;
        private EnemyFlightModule _flight;
        private Rigidbody2D _body;
        private Collider2D _collider;
        private Vector2 _correction;

        private bool IsFlying => _flight != null && _flight.isActiveAndEnabled;
        private bool Ready => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled &&
            !_enemy.IsDead && _body != null && _body.simulated && _collider != null && _collider.enabled;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _movement = owner.GetModule<EnemyMovementModule>();
            _flight = owner.GetModule<EnemyFlightModule>();
            _body = owner.GetComponent<Rigidbody2D>();
            _collider = owner.GetComponent<Collider2D>();

            if (isActiveAndEnabled) Register();
        }

        private void OnEnable() => Register();

        private void Register()
        {
            if (!Members.Contains(this)) Members.Add(this);
        }

        private void FixedUpdate()
        {
            // 도약·돌진·공격 준비와 피격 넉백의 이동을 덮어쓰지 않습니다.
            if (!Ready || _enemy.IsAttackInProgress || _enemy.IsHitReacting ||
                (!IsFlying && (_movement == null || !_movement.IsGrounded)))
            {
                _correction = Vector2.zero;

                return;
            }

            Vector2 desired = GetSeparation();
            _correction = Vector2.MoveTowards(_correction, desired, acceleration * Time.fixedDeltaTime);
            if (_correction.sqrMagnitude < 0.000001f) return;

            Vector2 velocity = _body.linearVelocity;
            Vector2 away = _correction.normalized;
            float approaching = Vector2.Dot(velocity, away);

            if (approaching < 0f) velocity -= away * approaching;
            Vector2 nextVelocity = velocity + _correction;

            if (IsFlying)
            {
                float step = nextVelocity.magnitude * Time.fixedDeltaTime;
                float safe = _flight.LimitTravel(nextVelocity.normalized, step);
                _body.linearVelocity = nextVelocity.normalized * (safe / Time.fixedDeltaTime);
            }
            else
            {
                float horizontal = nextVelocity.x;
                _body.linearVelocityX = Mathf.Abs(horizontal) > 0.001f &&
                    _movement.CanMoveOnGround(Mathf.Sign(horizontal), Mathf.Abs(horizontal)) ? horizontal : 0f;
            }
        }

        private Vector2 GetSeparation()
        {
            Bounds bounds = _collider.bounds;
            Vector2 total = Vector2.zero;

            foreach (EnemySeparationModule other in Members)
            {
                if (other == this || other == null || !other.Ready || other.IsFlying != IsFlying ||
                    other.gameObject.scene != gameObject.scene) continue;

                Bounds otherBounds = other._collider.bounds;
                Vector2 difference = bounds.center - otherBounds.center;
                if (!IsFlying && Mathf.Abs(difference.y) > maximumHeightDifference) continue;
                if (!IsFlying) difference.y = 0f;

                float gap = Mathf.Max(minimumGap, other.minimumGap);
                float distance = bounds.extents.x + otherBounds.extents.x + gap;
                if (IsFlying)
                    distance = Mathf.Max(bounds.extents.x, bounds.extents.y) +
                        Mathf.Max(otherBounds.extents.x, otherBounds.extents.y) + gap;
                if (distance <= 0f || difference.sqrMagnitude >= distance * distance) continue;

                float currentDistance = difference.magnitude;
                Vector2 direction = currentDistance > 0.0001f ? difference / currentDistance :
                    Vector2.right * (_enemy.GetInstanceID() < other._enemy.GetInstanceID() ? -1f : 1f);
                total += direction * Mathf.Clamp01((distance - currentDistance) / distance);
            }

            return Vector2.ClampMagnitude(total, 1f) * separationSpeed;
        }

        private void OnDisable()
        {
            Members.Remove(this);
            _correction = Vector2.zero;
        }

        private void OnDestroy() => Members.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Members.Clear();
    }
}
