using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    // 하나의 ParticleSystem으로 그리되, 입자별 2D 표면 접촉 시각/벽 흘러내림을 관리합니다.
    [DisallowMultipleComponent, RequireComponent(typeof(ParticleSystem))]
    public sealed class EnemyBloodParticle : MonoBehaviour
    {
        [Header("Spray (World Units)")]
        [SerializeField] private Vector2Int particleCount = new Vector2Int(14, 20);
        [SerializeField] private Vector2 speedRange = new Vector2(2.8f, 5.2f);
        [SerializeField, Range(0f, 360f)] private float spreadAngle = 105f;
        [SerializeField, Min(0f)] private float upwardSpeed = 1.2f;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.09f, 0.22f);
        [SerializeField, Min(0f)] private float gravityScale = 1.4f;
        [SerializeField, Min(0.1f)] private float maxAirTime = 2f;
        [SerializeField] private Color darkColor = new Color(0.5f, 0.005f, 0.015f, 1f);
        [SerializeField] private Color brightColor = new Color(0.9f, 0.025f, 0.035f, 1f);

        [Header("Surface Traces")]
        [Tooltip("Ground와 Platform만 검사합니다. 캐릭터와 Trigger는 제외합니다.")]
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);
        [Tooltip("첫 표면 접촉부터 페이드 시작까지의 시간. 벽에서 바닥으로 내려와도 다시 세지 않습니다.")]
        [SerializeField] private Vector2 traceDuration = new Vector2(2f, 3f);
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.55f;
        [SerializeField, Min(0f)] private float wallSlideSpeed = 0.22f;

        private const float Skin = 0.005f;
        private enum SurfaceState { Air, Floor, Wall }
        private struct Drop
        {
            public Vector2 Position, Velocity, LocalPosition, LocalNormal;
            public float Size, Angle, Spin, FadeAt;
            public Color Color;
            public Collider2D Surface;
            public SurfaceState State;
            public bool TouchedSurface;
            public uint Seed;
        }

        private ParticleSystem _particles;
        private ParticleSystem.Particle[] _renderParticles;
        private Drop[] _drops;
        private int _count;
        private float _age, _z;
        private ContactFilter2D _filter;
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(8);

        internal float MaximumLifetime => Mathf.Max(0.1f, maxAirTime) +
            // 공중 페이드 도중 늦게 착지하는 경우까지 포함합니다.
            Mathf.Max(0f, traceDuration.x, traceDuration.y) + 2f * Mathf.Max(0.01f, fadeDuration) + 0.1f;

        internal void Emit()
        {
            _particles = GetComponent<ParticleSystem>();
            int minimum = Mathf.Clamp(particleCount.x, 1, 64);
            int maximum = Mathf.Clamp(particleCount.y, minimum, 64);
            _count = Random.Range(minimum, maximum + 1);
            if (_drops == null || _drops.Length < _count)
            {
                _drops = new Drop[maximum];
                _renderParticles = new ParticleSystem.Particle[maximum];
            }
            // 자동 이동/중력 대신 FixedUpdate에서 한 번만 계산합니다.
            var main = _particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = maximum;
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(terrainLayers);
            _age = 0f;
            _z = transform.position.z;
            float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
            float aim = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
            for (int i = 0; i < _count; i++)
            {
                // 부채꼴 구간별로 하나씩 뽑아 한쪽에 전부 뭉치는 경우를 줄입니다.
                float angle = aim + Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f,
                    (i + Random.value) / _count);
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                _drops[i] = new Drop
                {
                    Position = (Vector2)transform.position + Random.insideUnitCircle * 0.025f,
                    Velocity = direction * RandomBetween(speedRange, 0f) + Vector2.up * upwardSpeed,
                    Size = RandomBetween(sizeRange, 0.01f) * scale,
                    Angle = Random.Range(0f, 360f), Spin = Random.Range(-220f, 220f),
                    Color = Color.Lerp(darkColor, brightColor, Random.value),
                    FadeAt = Mathf.Max(0.1f, maxAirTime), Seed = (uint)Random.Range(1, int.MaxValue)
                };
            }
            WriteParticles();
        }

        private static float RandomBetween(Vector2 range, float minimum) =>
            Random.Range(Mathf.Max(minimum, Mathf.Min(range.x, range.y)), Mathf.Max(minimum, Mathf.Max(range.x, range.y)));

        private void FixedUpdate()
        {
            if (_count == 0 || _particles == null) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;
            for (int i = _count - 1; i >= 0; i--)
            {
                Drop drop = _drops[i];
                if (_age >= drop.FadeAt + Mathf.Max(0.01f, fadeDuration))
                {
                    _drops[i] = _drops[--_count];
                    continue;
                }
                if (drop.State != SurfaceState.Air)
                {
                    if (drop.Surface == null || !drop.Surface.enabled || !drop.Surface.gameObject.activeInHierarchy)
                    {
                        drop.State = SurfaceState.Air;
                        drop.Surface = null;
                        drop.Velocity = Vector2.zero;
                    }
                    else
                    {
                        Transform surface = drop.Surface.transform;
                        drop.Position = surface.TransformPoint(drop.LocalPosition);
                        Vector2 normal = surface.TransformDirection(drop.LocalNormal).normalized;
                        drop.Angle = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
                        if (drop.State == SurfaceState.Wall) Slide(ref drop, normal, dt);
                    }
                }
                if (drop.State == SurfaceState.Air)
                {
                    drop.Velocity += Physics2D.gravity * gravityScale * dt;
                    drop.Angle += drop.Spin * dt;
                    Move(ref drop, drop.Velocity * dt);
                }
                _drops[i] = drop;
            }
            WriteParticles();
        }

        private void Move(ref Drop drop, Vector2 delta)
        {
            float distance = delta.magnitude;
            if (distance <= 0.00001f) return;
            if (!FindTerrain(drop.Position, Radius(drop), delta / distance, distance + Skin, out RaycastHit2D hit))
            {
                drop.Position += delta;
                return;
            }
            if (hit.normal.y >= 0.5f || Mathf.Abs(hit.normal.x) >= 0.5f)
                Attach(ref drop, hit);
            else
            {
                // 천장에서는 붙지 않고 표면 안쪽 속도를 제거해 다시 떨어집니다.
                drop.Position = hit.centroid + hit.normal * Skin;
                drop.Velocity -= hit.normal * Mathf.Min(0f, Vector2.Dot(drop.Velocity, hit.normal));
            }
        }

        private void Attach(ref Drop drop, RaycastHit2D hit)
        {
            drop.State = hit.normal.y >= 0.5f ? SurfaceState.Floor : SurfaceState.Wall;
            drop.Surface = hit.collider;
            drop.Position = hit.point + hit.normal * (Radius(drop) + Skin);
            drop.Velocity = Vector2.zero;
            drop.Angle = Mathf.Atan2(-hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;
            drop.LocalPosition = drop.Surface.transform.InverseTransformPoint(drop.Position);
            drop.LocalNormal = drop.Surface.transform.InverseTransformDirection(hit.normal);
            if (!drop.TouchedSurface)
            {
                drop.TouchedSurface = true;
                drop.FadeAt = _age + RandomBetween(traceDuration, 0f);
            }
        }

        private void Slide(ref Drop drop, Vector2 normal, float dt)
        {
            Vector2 tangent = Vector2.down - normal * Vector2.Dot(Vector2.down, normal);
            Vector2 delta = tangent.normalized * wallSlideSpeed * dt;
            Move(ref drop, delta);
            if (drop.State == SurfaceState.Floor) return;
            // 벽 끝을 지나면 공중에서 흘러내리는 대신 중력 낙하로 돌아갑니다.
            if (FindTerrain(drop.Position, Radius(drop), -normal, 0.08f, out RaycastHit2D support) &&
                Mathf.Abs(support.normal.x) >= 0.5f)
            {
                Attach(ref drop, support);
            }
            else
            {
                drop.State = SurfaceState.Air;
                drop.Surface = null;
                drop.Velocity = tangent.normalized * wallSlideSpeed;
            }
        }

        private static float Radius(Drop drop) => drop.Size * 0.2f;

        private bool FindTerrain(Vector2 origin, float radius, Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            nearest = default;
            Physics2D.CircleCast(origin, radius, direction, _filter, _hits, distance);
            float closest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in _hits)
            {
                if (hit.collider == null || hit.distance >= closest ||
                    hit.collider.GetComponentInParent<Agent>() != null || Vector2.Dot(direction, hit.normal) >= -0.0001f) continue;
                if (hit.collider.usedByEffector && hit.collider.TryGetComponent(out PlatformEffector2D platform) &&
                    platform.useOneWay && (direction.y > 0f || hit.normal.y < 0.5f || origin.y < hit.point.y)) continue;
                closest = hit.distance;
                nearest = hit;
            }
            return nearest.collider != null;
        }

        private void WriteParticles()
        {
            if (_count == 0)
            {
                // simulationSpeed가 0이면 시스템 시간도 멈춥니다. 입자가 끝났음을 명시합니다.
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
            for (int i = 0; i < _count; i++)
            {
                Drop drop = _drops[i];
                Color color = drop.Color;
                color.a *= 1f - Mathf.Clamp01((_age - drop.FadeAt) / Mathf.Max(0.01f, fadeDuration));
                Vector3 size = drop.State == SurfaceState.Air ? Vector3.one * drop.Size :
                    new Vector3(drop.Size * 1.6f, drop.Size * 0.4f, drop.Size);
                _renderParticles[i] = new ParticleSystem.Particle
                {
                    position = new Vector3(drop.Position.x, drop.Position.y, _z),
                    startSize3D = size, rotation3D = new Vector3(0f, 0f, drop.Angle),
                    startColor = color, startLifetime = 1f, remainingLifetime = 1f, randomSeed = drop.Seed
                };
            }
            _particles.SetParticles(_renderParticles, _count);
        }

        private void OnDisable()
        {
            _count = 0;
            if (_particles != null) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
