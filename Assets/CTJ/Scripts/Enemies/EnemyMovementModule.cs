using System.Collections.Generic;
using Agents;
using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // FSM이 Chase 중에만 호출합니다. 이 모듈 자체에는 Update/FixedUpdate가 없습니다.
    [DisallowMultipleComponent]
    public sealed class EnemyMovementModule : Module
    {
        [Header("Terrain")]
        [SerializeField] private Collider2D bodyCollider;
        [Tooltip("발판/벽 레이어만 지정합니다. 현재 프로젝트의 Ground는 6번입니다.")]
        [SerializeField] private LayerMask terrainLayers = 1 << 6;
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.1f;
        [SerializeField, Min(0.01f)] private float obstacleCheckDistance = 0.3f;
        [SerializeField, Min(0.01f)] private float horizontalTolerance = 0.06f;

        [Header("Jump (World Units)")]
        [SerializeField, Min(0f)] private float jumpHeight = 3f;
        [SerializeField, Min(0f)] private float minimumTargetHeight = 0.45f;
        [SerializeField, Min(0f)] private float jumpCooldown = 0.5f;
        [Tooltip("머리 위 발판의 가까운 가장자리 탐색 거리. 다층 경로 탐색은 하지 않습니다.")]
        [SerializeField, Min(0f)] private float maxLedgeDetour = 3f;

        private const float Skin = 0.04f;
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();
        private Rigidbody2D _body;
        private EnemyBase _enemy;
        private ContactFilter2D _terrainFilter;
        private float _nextJumpTime;
        private bool _approachingLedge;
        private bool _risingPastLedge;
        private float _approachX;
        private float _ledgeTop;
        private Vector2 _plannedTarget;
        private Transform _routeTarget;
        private float _nextPlanTime;
        private Transform _cachedTarget;
        private Collider2D _targetBody;

        public bool IsGrounded => Ready && _body.linearVelocityY <= 0.1f &&
            CastTerrain(bodyCollider.bounds.center, ProbeSize, Vector2.down,
                groundCheckDistance + Skin, out _, 0.5f);
        public bool IsDetouring => _approachingLedge || _risingPastLedge;
        public Vector2 BodyCenter => Ready ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        private Vector2 ProbeSize => Vector2.Max((Vector2)bodyCollider.bounds.size - Vector2.one * Skin,
            Vector2.one * Skin);
        private bool Ready => _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead &&
            isActiveAndEnabled && _body != null && _body.simulated && bodyCollider != null && bodyCollider.enabled;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _body = owner.GetComponent<Rigidbody2D>();
            if (bodyCollider == null)
            {
                foreach (Collider2D candidate in owner.GetComponents<Collider2D>())
                {
                    if (candidate.isTrigger)
                        continue;
                    bodyCollider = candidate;
                    break;
                }
            }

            _terrainFilter = new ContactFilter2D { useTriggers = false };
            _terrainFilter.SetLayerMask(terrainLayers);
            if (_enemy == null || _body == null || bodyCollider == null)
                Debug.LogWarning($"{name}: EnemyMovementModule의 적/물리 몸체/몸체 Collider를 확인하세요.", this);
        }

        public float MoveTowards(Transform target, float speed)
        {
            if (!Ready || target == null || !target.gameObject.activeInHierarchy)
            {
                Stop();
                return 0f;
            }

            Vector2 targetPosition = GetTargetFeet(target);
            if (_routeTarget != target || (targetPosition - _plannedTarget).sqrMagnitude > 1f)
                CancelRoute();

            Bounds bounds = bodyCollider.bounds;
            float heightDifference = targetPosition.y - bounds.min.y;
            float gravity = -Physics2D.gravity.y * _body.gravityScale;
            bool grounded = IsGrounded;

            // 가장자리 밖에서 수직으로 솟은 뒤, 발이 발판 윗면보다 높아져야 안쪽으로 돌아옵니다.
            if (_risingPastLedge)
            {
                if (bounds.min.y >= _ledgeTop + Skin || _body.linearVelocityY <= 0f)
                    _risingPastLedge = false;
                else
                    return SetHorizontal(_approachX, speed);
            }

            if (_approachingLedge)
            {
                float direction = SetHorizontal(_approachX, speed);
                if (Mathf.Abs(_body.position.x - _approachX) <= horizontalTolerance &&
                    TryJump(grounded, gravity))
                {
                    _approachingLedge = false;
                    _risingPastLedge = true;
                }
                return direction;
            }

            float chaseDirection = SetHorizontal(targetPosition.x, speed);
            if (!grounded || speed <= 0f || gravity <= 0f || jumpHeight <= 0f)
                return chaseDirection;

            bool higherTarget = heightDifference > minimumTargetHeight && heightDifference <= jumpHeight;
            // 머리 위에 발판이 있으면 바로 뛰지 않고 도달 가능한 가까운 끝으로 돌아갑니다.
            if (higherTarget && CastTerrain(bounds.center, ProbeSize, Vector2.up, jumpHeight, out _))
            {
                if (Time.time >= _nextPlanTime)
                {
                    _nextPlanTime = Time.time + 0.3f;
                    TryPlanLedge(target, bounds);
                }
                if (_approachingLedge)
                    return SetHorizontal(_approachX, speed);
                return chaseDirection;
            }

            bool obstacle = chaseDirection != 0f && CastTerrain(bounds.center, ProbeSize,
                Vector2.right * chaseDirection, obstacleCheckDistance, out RaycastHit2D obstacleHit) &&
                Mathf.Abs(obstacleHit.normal.x) > 0.5f;
            float airTimeToApex = Mathf.Sqrt(2f * jumpHeight / gravity);
            bool closeEnoughToJump = Mathf.Abs(targetPosition.x - _body.position.x) <=
                                     speed * airTimeToApex + bounds.extents.x;
            if ((higherTarget && closeEnoughToJump) || obstacle)
                TryJump(grounded, gravity);
            return chaseDirection;
        }

        private bool TryJump(bool grounded, float gravity)
        {
            if (!grounded || gravity <= 0f || jumpHeight <= 0f || Time.time < _nextJumpTime ||
                CastTerrain(bodyCollider.bounds.center, ProbeSize, Vector2.up, Skin * 2f, out _))
                return false;

            // v² = 2gh. Rigidbody의 질량이 달라도 지정 높이에 도달하도록 초기 속도를 설정합니다.
            _body.linearVelocityY = Mathf.Sqrt(2f * gravity * jumpHeight);
            _nextJumpTime = Time.time + jumpCooldown;
            return true;
        }

        private bool TryPlanLedge(Transform target, Bounds bounds)
        {
            // Collider.bounds의 maxY는 Tilemap/Composite 전체 높이일 수 있어 실제 윗면을 작은 BoxCast로 찾습니다.
            Vector2 targetFeet = GetTargetFeet(target);
            Vector2 aboveTarget = new Vector2(targetFeet.x, bounds.min.y + jumpHeight + Skin);
            if (!CastTerrain(aboveTarget, Vector2.one * Skin, Vector2.down, jumpHeight,
                    out RaycastHit2D surface, 0.5f) || surface.point.y <= bounds.max.y + Skin)
                return false;

            float firstSide = targetFeet.x < _body.position.x ? -1f : 1f;
            const float step = 0.25f;
            for (float distance = step; distance <= maxLedgeDetour; distance += step)
            {
                for (int side = 0; side < 2; side++)
                {
                    float candidateX = _body.position.x + distance * (side == 0 ? firstSide : -firstSide);
                    Vector2 candidateCenter = new Vector2(candidateX + bounds.center.x - _body.position.x,
                        bounds.center.y);
                    if (CastTerrain(candidateCenter, ProbeSize, Vector2.up, jumpHeight, out _) ||
                        !CastTerrain(candidateCenter, ProbeSize, Vector2.down,
                            groundCheckDistance + Skin, out _, 0.5f) ||
                        CastTerrain(bounds.center, ProbeSize, Vector2.right * Mathf.Sign(candidateX - _body.position.x),
                            Mathf.Abs(candidateX - _body.position.x), out _))
                        continue;

                    _approachX = candidateX;
                    _ledgeTop = surface.point.y;
                    _plannedTarget = targetFeet;
                    _routeTarget = target;
                    _approachingLedge = true;
                    return true;
                }
            }
            return false;
        }

        private float SetHorizontal(float destinationX, float speed)
        {
            float difference = destinationX - _body.position.x;
            float direction = Mathf.Abs(difference) <= horizontalTolerance ? 0f : Mathf.Sign(difference);
            // 목표 X를 한 물리 프레임에 지나쳐 좌우로 떨리는 현상을 막습니다.
            float velocity = direction * Mathf.Min(Mathf.Max(0f, speed), Mathf.Abs(difference) / Time.fixedDeltaTime);
            _body.linearVelocityX = velocity;
            return direction;
        }

        public bool HasClearPath(Vector2 origin, Vector2 destination)
        {
            if (!Ready)
                return false;
            Vector2 offset = destination - origin;
            return offset.sqrMagnitude < 0.0001f ||
                   !CastTerrain(origin, Vector2.one * Skin, offset.normalized, offset.magnitude, out _);
        }

        public float GetTargetHeightDifference(Transform target)
        {
            return Ready && target != null ? GetTargetFeet(target).y - bodyCollider.bounds.min.y : float.PositiveInfinity;
        }

        private Vector2 GetTargetFeet(Transform target)
        {
            if (_cachedTarget != target)
            {
                _cachedTarget = target;
                _targetBody = null;
                foreach (Collider2D candidate in target.GetComponentsInChildren<Collider2D>())
                {
                    if (!candidate.enabled)
                        continue;
                    // 실제 몸체 우선. 몸체가 없는 Probe 타깃은 Trigger도 사용할 수 있습니다.
                    if (_targetBody == null || !candidate.isTrigger)
                        _targetBody = candidate;
                    if (!candidate.isTrigger)
                        break;
                }
            }
            return _targetBody != null && _targetBody.enabled && _targetBody.gameObject.activeInHierarchy
                ? new Vector2(target.position.x, _targetBody.bounds.min.y) : (Vector2)target.position;
        }

        private bool CastTerrain(Vector2 origin, Vector2 size, Vector2 direction, float distance,
            out RaycastHit2D nearest, float minimumNormalY = -1f)
        {
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            Physics2D.BoxCast(origin, size, 0f, direction, _terrainFilter, _hits, distance);
            foreach (RaycastHit2D hit in _hits)
            {
                if (hit.collider == null || hit.rigidbody == _body ||
                    hit.collider.GetComponentInParent<Agent>() != null || hit.normal.y < minimumNormalY)
                    continue;
                if (hit.distance < nearestDistance)
                {
                    nearest = hit;
                    nearestDistance = hit.distance;
                }
            }
            return nearest.collider != null;
        }

        public void Stop()
        {
            if (_body != null)
                _body.linearVelocityX = 0f;
            CancelRoute();
        }

        private void CancelRoute()
        {
            _approachingLedge = false;
            _risingPastLedge = false;
            _routeTarget = null;
        }

        private void OnDisable() => Stop();
    }
}
