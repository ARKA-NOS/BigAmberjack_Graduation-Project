using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(ParticleSystem))]
    public sealed class EnemyMetalFragmentParticle : MonoBehaviour
    {
        [Header("Scatter")]
        [SerializeField] private Vector2Int fragmentCount = new Vector2Int(13, 19);
        [SerializeField] private Vector2 speedRange = new Vector2(3.2f, 6.3f);
        [SerializeField, Range(0f, 360f)] private float spreadAngle = 145f;
        [SerializeField, Min(0f)] private float upwardSpeed = 1.4f;
        [SerializeField] private Vector2 sizeRange = new Vector2(0.08f, 0.20f);
        [SerializeField] private Color darkMetal = new Color(0.26f, 0.28f, 0.31f);
        [SerializeField] private Color lightMetal = new Color(0.58f, 0.60f, 0.63f);
        [Tooltip("작은 부품의 메시를 따로 렌더링할 파티클 시스템입니다.")]
        [SerializeField] private ParticleSystem attachmentParticles;
        [SerializeField, Range(0f, 1f)] private float attachmentChance = 0.25f;
        [SerializeField] private Color[] attachmentColors =
        {
            new Color(0.83f, 0.83f, 0.82f),
            new Color(0.70f, 0.70f, 0.70f)
        };

        [Header("Bounce and Lifetime")]
        [SerializeField] private LayerMask terrainLayers = (1 << 6) | (1 << 10);
        [SerializeField, Min(0f)] private float gravityScale = 1.9f;
        [SerializeField, Range(0f, 1f)] private float bounce = 0.62f;
        [SerializeField, Range(0f, 1f)] private float tangentRetention = 0.84f;
        [SerializeField] private Vector2 lifetimeRange = new Vector2(1.0f, 1.5f);
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;

        private const float Skin = 0.005f;
        private const int MaxContactsPerStep = 3;

        private struct Fragment
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Vector2 Size;
            public Color Tint;
            public float Angle;
            public float Spin;
            public float FadeAt;
            public int MeshIndex;
            public int AttachmentMeshIndex;
            public Vector2 AttachmentOffset;
            public float AttachmentScale;
            public float AttachmentAngle;
            public Color AttachmentTint;
            public float Radius;
        }

        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(8);
        private ContactFilter2D _filter;
        private Fragment[] _fragments;
        private ParticleSystem.Particle[] _renderParticles;
        private ParticleSystem.Particle[] _renderAttachments;
        private ParticleSystem _particles;
        private ParticleSystem _attachmentSystem;
        private int _count;
        private float _age;
        private float _z;

        internal float MaximumLifetime => Mathf.Max(0.1f, lifetimeRange.x, lifetimeRange.y) +
            Mathf.Max(0.01f, fadeDuration) + 0.1f;

        internal void Emit(ParticleSystem particles)
        {
            _particles = particles;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var meshes = new Mesh[renderer.meshCount];
            int meshCount = renderer.GetMeshes(meshes);
            if (meshCount == 0)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
            var vertices = new Vector3[meshCount][];
            for (int i = 0; i < meshCount; i++) vertices[i] = meshes[i].vertices;
            int attachmentMeshCount = attachmentParticles != null && attachmentParticles != particles
                ? attachmentParticles.GetComponent<ParticleSystemRenderer>().meshCount
                : 0;
            _attachmentSystem = attachmentMeshCount > 0 ? attachmentParticles : null;

            int minimum = Mathf.Clamp(fragmentCount.x, 1, 64);
            int maximum = Mathf.Clamp(fragmentCount.y, minimum, 64);
            _count = Random.Range(minimum, maximum + 1);
            if (_fragments == null || _fragments.Length < maximum)
            {
                _fragments = new Fragment[maximum];
                _renderParticles = new ParticleSystem.Particle[maximum * 2];
            }
            if (_attachmentSystem != null)
            {
                if (_renderAttachments == null || _renderAttachments.Length < maximum)
                    _renderAttachments = new ParticleSystem.Particle[maximum];
                ConfigureParticles(_attachmentSystem, maximum);
            }

            ConfigureParticles(particles, _attachmentSystem != null ? maximum : maximum * 2);
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(terrainLayers);
            _age = 0f;
            Vector3 origin = transform.position;
            _z = origin.z;
            float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
            float aim = Mathf.Atan2(transform.right.y, transform.right.x);
            for (int i = 0; i < _count; i++)
            {
                float angle = aim + spreadAngle * Mathf.Deg2Rad * ((i + Random.value) / _count - 0.5f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float size = RandomBetween(sizeRange, 0.01f) * scale;
                var fragment = new Fragment
                {
                    Position = (Vector2)origin + Random.insideUnitCircle * 0.025f,
                    Velocity = direction * RandomBetween(speedRange, 0f) + Vector2.up * upwardSpeed,
                    Size = new Vector2(size * Random.Range(0.92f, 1.08f), size * Random.Range(0.92f, 1.08f)),
                    Tint = Color.Lerp(darkMetal, lightMetal, Random.value),
                    Angle = Random.Range(0f, 360f),
                    Spin = Random.Range(-520f, 520f),
                    FadeAt = RandomBetween(lifetimeRange, 0.1f),
                    MeshIndex = Random.Range(0, meshCount),
                    AttachmentMeshIndex = -1,
                    Radius = size * 0.38f
                };
                if (attachmentChance > 0f && Random.value <= attachmentChance)
                    AddAttachment(ref fragment, vertices[fragment.MeshIndex], meshCount, attachmentMeshCount);
                _fragments[i] = fragment;
            }
            WriteParticles();
        }

        private static void ConfigureParticles(ParticleSystem particles, int maximum)
        {
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = maximum;
        }

        private void AddAttachment(ref Fragment fragment, Vector3[] vertices, int meshCount, int attachmentMeshCount)
        {
            // 가장자리에 다른 메시를 일부 겹쳐 놓고, 본체와 같은 각도로 움직입니다.
            Vector2 side = Random.insideUnitCircle.normalized;
            Vector2 anchor = Vector2.zero;
            float furthest = float.NegativeInfinity;
            foreach (Vector3 vertex in vertices)
            {
                Vector2 point = Vector2.Scale(vertex, fragment.Size);
                float projection = Vector2.Dot(point, side);
                if (projection <= furthest) continue;
                furthest = projection;
                anchor = point;
            }
            fragment.AttachmentOffset = anchor * 0.85f;
            fragment.AttachmentScale = Random.Range(0.42f, 0.62f);
            fragment.AttachmentAngle = Random.Range(-65f, 65f);
            fragment.AttachmentMeshIndex = attachmentMeshCount > 0
                ? Random.Range(0, attachmentMeshCount)
                : (fragment.MeshIndex + Random.Range(0, meshCount - 1) + 1) % meshCount;
            fragment.AttachmentTint = attachmentColors.Length > 0
                ? attachmentColors[Random.Range(0, attachmentColors.Length)]
                : lightMetal;
            fragment.Radius = Mathf.Max(fragment.Radius, fragment.AttachmentOffset.magnitude);
        }

        private void FixedUpdate()
        {
            if (_count == 0 || _particles == null) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;
            float fade = Mathf.Max(0.01f, fadeDuration);
            for (int i = _count - 1; i >= 0; i--)
            {
                Fragment fragment = _fragments[i];
                if (_age >= fragment.FadeAt + fade)
                {
                    _fragments[i] = _fragments[--_count];
                    continue;
                }
                fragment.Velocity += Physics2D.gravity * (gravityScale * dt);
                fragment.Angle += fragment.Spin * dt;
                Move(ref fragment, dt);
                _fragments[i] = fragment;
            }
            WriteParticles();
        }

        private void Move(ref Fragment fragment, float timeLeft)
        {
            for (int i = 0; i < MaxContactsPerStep && timeLeft > 0f; i++)
            {
                float speed = fragment.Velocity.magnitude;
                if (speed <= 0.0001f) return;
                Vector2 direction = fragment.Velocity / speed;
                float distance = speed * timeLeft;
                if (!TryFindContact(fragment.Position, fragment.Radius, direction, distance,
                        out Vector2 center, out Vector2 normal, out float traveled))
                {
                    fragment.Position += direction * distance;
                    return;
                }

                fragment.Position = center + normal * Skin;
                Vector2 normalVelocity = normal * Vector2.Dot(fragment.Velocity, normal);
                Vector2 tangentVelocity = fragment.Velocity - normalVelocity;
                fragment.Velocity = tangentVelocity * tangentRetention - normalVelocity * bounce;
                fragment.Spin *= -0.72f;
                timeLeft -= traveled / speed;
            }
        }

        private bool TryFindContact(Vector2 origin, float radius, Vector2 direction, float distance,
            out Vector2 center, out Vector2 normal, out float traveled)
        {
            center = normal = Vector2.zero;
            traveled = float.PositiveInfinity;
            Physics2D.CircleCast(origin, radius, direction, _filter, _hits, distance);
            foreach (RaycastHit2D hit in _hits)
            {
                Collider2D surface = hit.collider;
                if (surface == null || hit.distance >= traveled || surface.GetComponentInParent<Agent>() != null) continue;
                Vector2 candidateCenter = hit.centroid;
                Vector2 candidateNormal = hit.normal;
                if (hit.distance <= 0.0001f)
                {
                    // 접촉 상태에서 출발한 캐스트는 실제 표면 법선 대신 캐스트 반대 방향을 줍니다.
                    Vector2 closest = surface.ClosestPoint(origin);
                    Vector2 separation = origin - closest;
                    if (separation.sqrMagnitude <= 0.0000001f) continue;
                    candidateNormal = separation.normalized;
                    candidateCenter = closest + candidateNormal * radius;
                }
                if (Vector2.Dot(direction, candidateNormal) >= -0.0001f) continue;
                if (surface.usedByEffector && surface.TryGetComponent(out PlatformEffector2D platform) &&
                    platform.useOneWay && (direction.y > 0f || candidateNormal.y < 0.5f || origin.y < hit.point.y)) continue;
                center = candidateCenter;
                normal = candidateNormal;
                traveled = hit.distance;
            }
            return !float.IsPositiveInfinity(traveled);
        }

        private void WriteParticles()
        {
            if (_count == 0)
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
            int visible = 0;
            int visibleAttachments = 0;
            float fade = Mathf.Max(0.01f, fadeDuration);
            for (int i = 0; i < _count; i++)
            {
                Fragment fragment = _fragments[i];
                float opacity = 1f - Mathf.Clamp01((_age - fragment.FadeAt) / fade);
                _renderParticles[visible++] = RenderParticle(fragment.Position, fragment.Size,
                    fragment.Angle, fragment.Tint, opacity, fragment.MeshIndex, _z);
                if (fragment.AttachmentMeshIndex < 0) continue;
                Vector2 offset = Quaternion.Euler(0f, 0f, fragment.Angle) * fragment.AttachmentOffset;
                var attachment = RenderParticle(fragment.Position + offset,
                    fragment.Size * fragment.AttachmentScale, fragment.Angle + fragment.AttachmentAngle,
                    fragment.AttachmentTint, opacity, fragment.AttachmentMeshIndex, _z - 0.002f);
                if (_attachmentSystem != null) _renderAttachments[visibleAttachments++] = attachment;
                else _renderParticles[visible++] = attachment;
            }
            _particles.SetParticles(_renderParticles, visible);
            if (_attachmentSystem != null) _attachmentSystem.SetParticles(_renderAttachments, visibleAttachments);
        }

        private static ParticleSystem.Particle RenderParticle(Vector2 position, Vector2 size, float angle,
            Color tint, float opacity, int meshIndex, float z)
        {
            tint.a *= opacity;
            var particle = new ParticleSystem.Particle
            {
                position = new Vector3(position.x, position.y, z),
                startSize3D = new Vector3(size.x, size.y, 1f),
                rotation3D = new Vector3(0f, 0f, angle),
                startColor = tint,
                startLifetime = 1f,
                remainingLifetime = 1f
            };
            particle.SetMeshIndex(meshIndex);
            return particle;
        }

        private static float RandomBetween(Vector2 range, float minimum) =>
            Random.Range(Mathf.Max(minimum, Mathf.Min(range.x, range.y)), Mathf.Max(minimum, Mathf.Max(range.x, range.y)));

        private void OnDisable()
        {
            _count = 0;
            if (_particles != null) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
