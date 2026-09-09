using System.Collections.Generic;
using UnityEngine;

namespace CTJ.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float speed = 10f;
        [SerializeField, Min(0.01f)] private float lifetime = 5f;
        [Tooltip("벽과 바닥 레이어를 지정합니다. 타깃은 이 설정과 관계없이 검사합니다.")]
        [SerializeField] private LayerMask blockingLayers = ~0;

        private readonly List<RaycastHit2D> _castHits = new List<RaycastHit2D>();
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private Rigidbody2D _body;
        private CircleCollider2D _collider;
        private EnemyBase _dealer;
        private Transform _target;
        private Vector2 _direction;
        private float _damage;
        private float _expiresAt;
        private bool _launched;
        private bool _consumed;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _collider.isTrigger = true;
        }

        public void Launch(EnemyBase dealer, Transform target, float damage, Vector2 direction)
        {
            _dealer = dealer;
            _target = target;
            _damage = damage;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            _expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
            _launched = true;
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }

        private void FixedUpdate()
        {
            if (!_launched || _consumed)
                return;

            if (Time.time >= _expiresAt)
            {
                Consume();
                return;
            }

            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            _collider.Overlap(filter, _overlaps);
            // 발사 위치가 벽 안이면 타깃보다 벽을 우선합니다.
            foreach (Collider2D overlap in _overlaps)
            {
                if (IsBlockingCollider(overlap))
                {
                    Consume();
                    return;
                }
            }
            foreach (Collider2D overlap in _overlaps)
            {
                if (EnemyDamageUtility.IsTargetCollider(overlap, _target) && !IsIgnored(overlap))
                {
                    Hit(overlap, overlap.ClosestPoint(_body.position));
                    return;
                }
            }

            float distance = Mathf.Max(0.01f, speed) * Time.fixedDeltaTime;
            _collider.Cast(_direction, filter, _castHits, distance);
            RaycastHit2D nearestHit = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in _castHits)
            {
                if (IsIgnored(hit.collider) ||
                    (!EnemyDamageUtility.IsTargetCollider(hit.collider, _target) && !IsBlockingCollider(hit.collider)))
                    continue;

                if (hit.distance < nearestDistance ||
                    (Mathf.Approximately(hit.distance, nearestDistance) && IsBlockingCollider(hit.collider)))
                {
                    nearestHit = hit;
                    nearestDistance = hit.distance;
                }
            }

            if (nearestHit.collider != null)
            {
                Hit(nearestHit.collider, nearestHit.point);
                return;
            }

            // 이동 구간 전체를 Cast로 검사한 뒤 위치를 갱신해 얇은 벽도 판정합니다.
            _body.position += _direction * distance;
        }

        private bool IsIgnored(Collider2D other)
        {
            return other == null || other == _collider ||
                   other.GetComponentInParent<EnemyProjectile>() != null ||
                   other.GetComponentInParent<EnemyBase>() != null;
        }

        private bool IsBlockingCollider(Collider2D other)
        {
            return !IsIgnored(other) && !EnemyDamageUtility.IsTargetCollider(other, _target) &&
                   !other.isTrigger && (blockingLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        private void Hit(Collider2D other, Vector2 point)
        {
            if (_consumed)
                return;

            // 피해 처리에서 다른 로직이 실행되기 전에 중복 판정을 차단합니다.
            Consume();
            EnemyDamageUtility.TryApply(other, _target, _dealer, _damage, point, _direction);
        }

        private void Consume()
        {
            _consumed = true;
            _collider.enabled = false;
            Destroy(gameObject);
        }
    }
}
