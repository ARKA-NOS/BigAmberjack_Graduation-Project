using System.Collections.Generic;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyFlightModule : Module
    {
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);
        [SerializeField, Min(0f)] private float patrolHalfWidth = 3f;

        private const float Clearance = 0.025f;
        private const float ArrivalDistance = 0.06f;
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();
        private Rigidbody2D _body;
        private ContactFilter2D _terrain;
        private Vector2 _spawn;
        private bool _returning;
        private float _patrolDirection;
        private Transform _cachedTarget;
        private Collider2D _targetCollider;

        public Vector2 BodyCenter => bodyCollider != null ? (Vector2)bodyCollider.bounds.center : _body.position;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _body = owner.GetComponent<Rigidbody2D>();
            _spawn = _body.position;
            _terrain = new ContactFilter2D { useTriggers = false };
            _terrain.SetLayerMask(terrainLayers);
        }

        internal void BeginPatrol()
        {
            _returning = Vector2.Distance(_body.position, _spawn) > ArrivalDistance;
            _patrolDirection = Random.value < 0.5f ? -1f : 1f;
        }

        internal float Patrol(float speed)
        {
            if (_returning)
            {
                if (Vector2.Distance(_body.position, _spawn) > ArrivalDistance)
                    return MoveTowards(_spawn, speed);
                _returning = false;
            }

            Vector2 destination = _spawn + Vector2.right * (_patrolDirection * patrolHalfWidth);
            float direction = MoveTowards(destination, speed);
            if (direction == 0f) _patrolDirection = -_patrolDirection;
            return direction;
        }

        internal float Chase(Transform target, float speed) =>
            MoveTowards(_body.position + GetTargetCenter(target) - BodyCenter, speed);

        private float MoveTowards(Vector2 destination, float speed)
        {
            Vector2 delta = destination - _body.position;
            if (delta.sqrMagnitude <= ArrivalDistance * ArrivalDistance)
            {
                Stop();
                return 0f;
            }

            float step = Mathf.Max(0f, speed) * Time.fixedDeltaTime;
            Vector2 move = Vector2.ClampMagnitude(delta, step);
            Vector2 direction = move.normalized;
            float safeDistance = LimitTravel(direction, move.magnitude);
            if (safeDistance + 0.001f < move.magnitude)
            {
                // 막힌 축을 제외한 한 축으로만 움직여 모서리에서 옆으로 미끄러집니다.
                Vector2 horizontal = Vector2.right * Mathf.Sign(delta.x);
                Vector2 vertical = Vector2.up * Mathf.Sign(delta.y);
                float x = LimitTravel(horizontal, Mathf.Min(Mathf.Abs(delta.x), step));
                float y = LimitTravel(vertical, Mathf.Min(Mathf.Abs(delta.y), step));
                move = x >= y ? horizontal * x : vertical * y;
            }
            _body.linearVelocity = move / Time.fixedDeltaTime;
            return Mathf.Abs(move.x) > 0.001f ? Mathf.Sign(move.x) : 0f;
        }

        internal float LimitTravel(Vector2 direction, float distance)
        {
            if (distance <= 0f || bodyCollider == null || !_body.simulated) return 0f;
            Bounds bounds = bodyCollider.bounds;
            Vector2 size = Vector2.Max((Vector2)bounds.size - Vector2.one * Clearance, Vector2.one * Clearance);
            Physics2D.BoxCast(bounds.center, size, 0f, direction, _terrain, _hits, distance + Clearance);
            float allowed = distance;
            foreach (var hit in _hits)
            {
                if (!IsTerrain(hit) || Vector2.Dot(hit.normal, direction) >= -0.001f) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - Clearance));
            }
            return allowed;
        }

        internal bool HasClearPath(Vector2 origin, Vector2 destination)
        {
            Vector2 offset = destination - origin;
            if (offset.sqrMagnitude < 0.0001f) return true;
            Physics2D.Raycast(origin, offset.normalized, _terrain, _hits, offset.magnitude);
            foreach (var hit in _hits)
                if (IsTerrain(hit)) return false;
            return true;
        }

        internal Vector2 GetTargetCenter(Transform target)
        {
            if (_cachedTarget != target)
            {
                _cachedTarget = target;
                _targetCollider = target.GetComponent<Collider2D>();
                if (_targetCollider == null) _targetCollider = target.GetComponentInChildren<Collider2D>();
            }
            return _targetCollider != null && _targetCollider.enabled && _targetCollider.gameObject.activeInHierarchy
                ? (Vector2)_targetCollider.bounds.center : (Vector2)target.position;
        }

        private bool IsTerrain(RaycastHit2D hit) =>
            hit.collider != null && hit.rigidbody != _body && hit.collider.GetComponentInParent<Agent>() == null;

        internal void Stop()
        {
            if (_body != null) _body.linearVelocity = Vector2.zero;
        }
    }
}
