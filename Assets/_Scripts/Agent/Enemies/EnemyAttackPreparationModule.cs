using _Scripts.Agent.Enemies.Skills;
using _Scripts.DevLib.ModuleSystem;
using Lean.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    // 스킬의 공격 시점에 맞춰 준비 구간 안에서 입자를 모으고 정리합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackPreparationModule : Module
    {
        private const float GoldenAngle = 2.399963f;
        private const float TravelRatio = 0.8f;

        private struct GatheringParticle
        {
            public Vector3 StartPosition;
            public float Size;
        }

        [Header("Gathering")]
        [SerializeField] private bool particlesEnabled = true;
        [SerializeField] private ParticleSystem particlePrefab;
        [Tooltip("첫 입자 생성부터 마지막 입자가 중심에서 사라질 때까지의 희망 시간입니다. 공격 준비 시간이 부족하면 이펙트만 압축합니다.")]
        [SerializeField, Min(0.05f)] private float gatherTime = 0.45f;
        [Tooltip("공격 전에 입자를 정리할 여유 시간입니다. 준비 시간의 절반을 넘지 않도록 제한하며 공격은 늦추지 않습니다.")]
        [SerializeField, Min(0f)] private float completionLeadTime = 0.05f;
        [Tooltip("전체 모으기 시간 중 순차 생성에 사용할 비율입니다. 0이면 동시에 생성·도착합니다.")]
        [SerializeField, Range(0f, 0.9f)] private float emissionDurationRatio = 0.6f;
        [SerializeField, Range(1, 128)] private int particleCount = 40;
        [SerializeField] private Color particleColor = new Color(0.85f, 0.12f, 0.16f, 0.95f);
        [Tooltip("입자가 출발하는 중심으로부터의 최소·최대 거리입니다. 월드 유닛 기준입니다.")]
        [SerializeField] private Vector2 spawnRadius = new Vector2(0.8f, 1.25f);
        [Tooltip("입자 하나의 최소·최대 크기입니다. 월드 유닛 기준입니다.")]
        [SerializeField] private Vector2 particleSize = new Vector2(0.11f, 0.44f);

        [Header("Center / Drawing")]
        [Tooltip("비우면 적 루트의 콜라이더 중심을 사용합니다.")]
        [SerializeField] private Transform centerOverride;
        [SerializeField] private Vector2 centerOffset;
        [Tooltip("비우면 적의 첫 SpriteRenderer를 사용합니다.")]
        [SerializeField] private SpriteRenderer sortingSource;
        [SerializeField] private int sortingOrderOffset = 2;

        private EnemyBase _enemy;
        private Collider2D _bodyCollider;
        private EnemyAttackSkill _preparingSkill;
        private ParticleSystem _particles;
        private GatheringParticle[] _gatheringParticles;
        private ParticleSystem.Particle[] _renderParticles;
        private float _startsAt;
        private float _finishesAt;
        private float _duration;
        private float _emissionWindow;
        private float _particleLifetime;
        private int _particleCount;
        private bool _gatheringFinished;

        public bool ParticlesEnabled
        {
            get => particlesEnabled;
            set
            {
                particlesEnabled = value;

                if (!value) ClearPreparation();
            }
        }

        private bool CanGather => isActiveAndEnabled && particlesEnabled && particlePrefab != null &&
            _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead && particleCount > 0;

        public override void Initialize(ModuleOwner owner)
        {
            ClearPreparation();
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _bodyCollider = owner.GetComponent<Collider2D>();

            if (sortingSource == null) sortingSource = owner.GetComponentInChildren<SpriteRenderer>();
        }

        internal void BeginPreparation(EnemyAttackSkill skill, float preparationDuration)
        {
            ClearPreparation();

            if (!CanGather) return;

            float availableTime = Mathf.Max(0f, preparationDuration);
            if (availableTime <= 0f) return;

            float leadTime = Mathf.Min(Mathf.Max(0f, completionLeadTime), availableTime * 0.5f);

            _duration = Mathf.Min(Mathf.Max(0.05f, gatherTime), availableTime - leadTime);
            _preparingSkill = skill;
            _particleCount = Mathf.Clamp(particleCount, 1, 128);
            _emissionWindow = _particleCount > 1 ? _duration * Mathf.Clamp(emissionDurationRatio, 0f, 0.9f) : 0f;
            _particleLifetime = _duration - _emissionWindow;
            _finishesAt = Time.time + availableTime - leadTime;
            _startsAt = _finishesAt - _duration;
            InitializeParticles();

            if (Time.time >= _startsAt) SpawnParticles();
        }

        internal void EndPreparation(EnemyAttackSkill skill)
        {
            if (_preparingSkill == skill) ClearPreparation();
        }

        private void LateUpdate()
        {
            if (_preparingSkill == null) return;

            if (!CanGather || !_preparingSkill.IsPreparing)
            {
                ClearPreparation();

                return;
            }

            FinishGatheringIfDue();

            if (_gatheringFinished) return;

            if (_particles == null && Time.time >= _startsAt) SpawnParticles();

            if (_particles != null)
            {
                _particles.transform.position = GetCenter();
                UpdateParticles();
            }
        }

        private Vector3 GetCenter()
        {
            Vector3 center = centerOverride != null ? centerOverride.position :
                _bodyCollider != null ? _bodyCollider.bounds.center : _enemy.transform.position;

            return center + (Vector3)centerOffset;
        }

        private void SpawnParticles()
        {
            _particles = LeanPool.Spawn(particlePrefab, GetCenter(), Quaternion.identity);

            if (_particles == null) return;

            if (_particles.gameObject.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(_particles.gameObject, gameObject.scene);

            _particles.transform.localScale = Vector3.one;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;

            main.loop = false;
            main.playOnAwake = false;
            main.duration = _duration;
            // 자동 이동이나 수명 소멸 대신, 준비 시간으로 위치와 표시를 직접 계산합니다.
            main.simulationSpeed = 0f;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize3D = false;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = _particleCount;
            var size = _particles.sizeOverLifetime;

            size.enabled = false;
            size.separateAxes = false;
            var color = _particles.colorOverLifetime;

            color.enabled = false;
            var trails = _particles.trails;

            trails.enabled = false;
            var emission = _particles.emission;

            emission.enabled = false;
            var shape = _particles.shape;

            shape.enabled = false;

            var velocity = _particles.velocityOverLifetime;

            velocity.enabled = false;
            var renderer = _particles.GetComponent<ParticleSystemRenderer>();

            if (renderer != null)
            {
                // 속도·카메라 이동에 따른 늘이기와 축별 크기 변형을 사용하지 않습니다.
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.alignment = ParticleSystemRenderSpace.View;
                renderer.lengthScale = 1f;
                renderer.velocityScale = 0f;
                renderer.cameraVelocityScale = 0f;
                renderer.freeformStretching = false;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }

            if (sortingSource != null && renderer != null)
            {
                renderer.sortingLayerID = sortingSource.sortingLayerID;
                renderer.sortingOrder = Mathf.Clamp(sortingSource.sortingOrder + sortingOrderOffset, -32768, 32767);
            }

            _particles.Play(false);
            UpdateParticles();
        }

        private void InitializeParticles()
        {
            if (_gatheringParticles == null || _gatheringParticles.Length < _particleCount)
            {
                _gatheringParticles = new GatheringParticle[_particleCount];
                _renderParticles = new ParticleSystem.Particle[_particleCount];
            }

            float radiusMin = Mathf.Max(0f, Mathf.Min(spawnRadius.x, spawnRadius.y));
            float radiusMax = Mathf.Max(radiusMin, Mathf.Max(spawnRadius.x, spawnRadius.y));
            float sizeMin = Mathf.Max(0.01f, Mathf.Min(particleSize.x, particleSize.y));
            float sizeMax = Mathf.Max(sizeMin, Mathf.Max(particleSize.x, particleSize.y));
            float angleOffset = Random.Range(0f, Mathf.PI * 2f);

            for (int i = 0; i < _particleCount; i++)
            {
                // 연속 입자가 같은 방향에 겹쳐 선처럼 보이지 않도록 각도를 고르게 분산합니다.
                float angle = angleOffset + i * GoldenAngle + Random.Range(-0.1f, 0.1f);

                _gatheringParticles[i] = new GatheringParticle
                {
                    StartPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(radiusMin, radiusMax),
                    Size = Random.Range(sizeMin, sizeMax)
                };
            }
        }

        private void UpdateParticles()
        {
            float interiorRadius = GetInteriorRadius();
            int visibleCount = 0;

            for (int i = 0; i < _particleCount; i++)
            {
                float scheduledAt = _startsAt + (_particleCount > 1
                    ? _emissionWindow * i / (_particleCount - 1)
                    : 0f);
                float age = Time.time - scheduledAt;
                if (age < 0f) break;
                if (age >= _particleLifetime) continue;

                float progress = Mathf.Clamp01(age / _particleLifetime);
                float travel = Mathf.Clamp01(progress / TravelRatio);
                // t²로 가속하고, 이동 종료 시 정확히 로컬 원점(적 내부)에 도착합니다.
                Vector3 position = Vector3.LerpUnclamped(_gatheringParticles[i].StartPosition, Vector3.zero, travel * travel);
                float entering = 1f - Mathf.Clamp01(position.magnitude / interiorRadius);
                float size = Mathf.Lerp(_gatheringParticles[i].Size,
                    Mathf.Min(_gatheringParticles[i].Size, interiorRadius), entering);
                // 마지막 20%는 중심에 도착한 상태에서만 축소·소멸합니다.
                float absorbed = Mathf.Clamp01((progress - TravelRatio) / (1f - TravelRatio));
                Color color = particleColor;

                color.a *= Mathf.Clamp01(progress / 0.08f) * (1f - absorbed);
                _renderParticles[visibleCount++] = new ParticleSystem.Particle
                {
                    position = position,
                    velocity = Vector3.zero,
                    startLifetime = 1f,
                    remainingLifetime = 1f,
                    startSize = size * (1f - absorbed),
                    startColor = color,
                    randomSeed = (uint)(i + 1)
                };
            }

            _particles.SetParticles(_renderParticles, visibleCount);
        }

        private float GetInteriorRadius()
        {
            Vector3 extents = _bodyCollider != null ? _bodyCollider.bounds.extents :
                sortingSource != null ? sortingSource.bounds.extents : Vector3.one * 0.2f;

            return Mathf.Max(0.01f, Mathf.Min(extents.x, extents.y) * 0.5f);
        }

        private void ClearPreparation()
        {
            _preparingSkill = null;
            _gatheringFinished = false;
            ReleaseParticles();
        }

        private void FinishGatheringIfDue()
        {
            if (_gatheringFinished || Time.time < _finishesAt) return;

            ReleaseParticles();
            _gatheringFinished = true;
        }

        private void ReleaseParticles()
        {
            ParticleSystem particles = _particles;

            _particles = null;

            if (particles == null) return;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (LeanPool.Links.TryGetValue(particles.gameObject, out var pool) && pool != null)
                LeanPool.Despawn(particles.gameObject);
            else
                Destroy(particles.gameObject);
        }

        private void OnDisable() => ClearPreparation();

        private void OnDestroy() => ClearPreparation();
    }
}
