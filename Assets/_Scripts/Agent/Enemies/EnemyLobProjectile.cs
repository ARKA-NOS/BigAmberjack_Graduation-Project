using System.Collections.Generic;
using _Scripts.UI.HitFeedback;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    // 투척 후에는 스킬과 별개로 이동하며 지면 접촉 한 번에만 피해를 판정합니다.
    public sealed class EnemyLobProjectile : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField, Min(0.1f)] private float flightDuration = 0.72f;
        [SerializeField, Min(0.05f)] private float arcHeight = 1.05f;
        [SerializeField] private float spinSpeed = 570f;
        [SerializeField] private ParticleSystem impactEffect;
        [SerializeField] private float impactEffectAngle = 90f;
        [SerializeField, Min(0.01f)] private float impactEffectScale = 0.75f;
        [SerializeField] private Sprite[] impactFrames;
        [SerializeField, Min(1f)] private float impactFramesPerSecond = 20f;
        private const float SurfaceTolerance = 0.025f;

        private readonly List<Collider2D> _overlaps = new();
        private readonly RaycastHit2D[] _surfaceHit = new RaycastHit2D[1];
        private ContactFilter2D _terrain;
        private AttackTelegraphUI _warning;
        private EnemyBase _dealer;
        private Transform _target;
        private Vector2 _landingPoint;
        private Vector2 _position;
        private float _damage;
        private float _radius;
        private int _step;
        private int _steps;
        private bool _launched;

        private int FlightSteps => Mathf.Max(1, Mathf.CeilToInt(flightDuration / Time.fixedDeltaTime));

        internal bool HasClearArc(Vector2 origin, Vector2 landingPoint, ContactFilter2D terrain, RaycastHit2D[] hits)
        {
            int steps = FlightSteps;
            Vector2 from = origin;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 to = ArcPoint(origin, landingPoint, i, steps);
                RaycastHit2D hit = CastSegment(from, to, terrain, hits);
                if (hit.collider != null) return IsLanding(hit, landingPoint);
                from = to;
            }
            return IsLanding(ProbeLanding(landingPoint, terrain, hits), landingPoint);
        }

        internal static RaycastHit2D FindSurface(Vector2 origin, Vector2 direction, float distance, ContactFilter2D terrain,
            RaycastHit2D[] hits) =>
            Physics2D.Raycast(origin, direction, terrain, hits, distance) > 0 ? hits[0] : default;

        internal void Launch(EnemyBase dealer, Transform target, Vector2 landingPoint, float damage, float radius,
            LayerMask terrainLayers, AttackTelegraphUI warning)
        {
            _dealer = dealer;
            _target = target;
            _landingPoint = landingPoint;
            _position = transform.position;
            _damage = damage;
            _radius = radius;
            _terrain = new ContactFilter2D { useTriggers = false };
            _terrain.SetLayerMask(terrainLayers);
            _steps = FlightSteps;
            _launched = true;
            _warning = warning;
            if (_warning != null) _warning.transform.SetParent(transform, true);
        }

        private void FixedUpdate()
        {
            if (!_launched) return;
            Vector2 next = ArcPoint(transform.position, _landingPoint, ++_step, _steps);
            RaycastHit2D hit = CastSegment(_position, next, _terrain, _surfaceHit);
            if (hit.collider == null && _step == _steps)
                hit = ProbeLanding(_landingPoint, _terrain, _surfaceHit);
            if (hit.collider != null || _step >= _steps)
            {
                _launched = false;
                _warning?.Hide();
                bool landed = IsLanding(hit, _landingPoint);
                if (landed)
                    ApplyImpact(hit.point);
                if (!landed || !PlayImpactAnimation(hit.point)) Destroy(gameObject);
                return;
            }
            _position = next;
            if (visual != null)
            {
                visual.position = new Vector3(next.x, next.y, transform.position.z);
                visual.Rotate(0f, 0f, spinSpeed * Time.fixedDeltaTime);
            }
        }

        private bool PlayImpactAnimation(Vector2 point)
        {
            if (impactFrames == null || impactFrames.Length == 0 || visual == null ||
                !visual.TryGetComponent(out SpriteRenderer spriteRenderer)) return false;
            visual.SetPositionAndRotation(new Vector3(point.x, point.y, transform.position.z), Quaternion.identity);
            StartCoroutine(AnimateImpact(spriteRenderer));
            return true;
        }

        private System.Collections.IEnumerator AnimateImpact(SpriteRenderer spriteRenderer)
        {
            float startedAt = Time.time;
            int index = 0;
            while (index < impactFrames.Length)
            {
                spriteRenderer.sprite = impactFrames[index];
                yield return null;
                index = Mathf.FloorToInt((Time.time - startedAt) * impactFramesPerSecond);
            }
            Destroy(gameObject);
        }

        private void ApplyImpact(Vector2 center)
        {
            if (impactEffect != null)
            {
                ParticleSystem impact = Instantiate(impactEffect, center, Quaternion.Euler(0f, 0f, impactEffectAngle));
                impact.transform.localScale *= impactEffectScale;
                SceneManager.MoveGameObjectToScene(impact.gameObject, gameObject.scene);
                if (!impact.TryGetComponent(out EnemyHitParticle lifetime))
                    lifetime = impact.gameObject.AddComponent<EnemyHitParticle>();
                lifetime.Play(impact, 5f);
            }
            Physics2D.OverlapCircle(center, _radius, new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, _target)) continue;
                Vector2 closest = hit.ClosestPoint(center);
                Vector2 occlusionOrigin = center + Vector2.up * SurfaceTolerance;
                if (CastSegment(occlusionOrigin, closest + Vector2.up * SurfaceTolerance, _terrain, _surfaceHit).collider != null)
                    continue;
                Vector2 direction = (Vector2)hit.bounds.center - center;
                if (EnemyDamageUtility.TryApply(hit, _target, _dealer, _damage, closest, direction)) break;
            }
        }

        private Vector2 ArcPoint(Vector2 origin, Vector2 end, int step, int steps)
        {
            float t = step / (float)steps;
            return Vector2.Lerp(origin, end, t) + Vector2.up * (4f * arcHeight * t * (1f - t));
        }

        private static RaycastHit2D CastSegment(Vector2 from, Vector2 to, ContactFilter2D terrain, RaycastHit2D[] hits)
        {
            Vector2 offset = to - from;
            return FindSurface(from, offset.normalized, offset.magnitude, terrain, hits);
        }

        private static bool IsLanding(RaycastHit2D hit, Vector2 landingPoint) =>
            hit.collider != null && hit.normal.y > 0.5f &&
            (hit.point - landingPoint).sqrMagnitude <= SurfaceTolerance * SurfaceTolerance;

        private static RaycastHit2D ProbeLanding(Vector2 point, ContactFilter2D terrain, RaycastHit2D[] hits) =>
            FindSurface(point + Vector2.up * SurfaceTolerance, Vector2.down, 2f * SurfaceTolerance, terrain, hits);
    }
}
