using _Scripts.Agent.Enemies.Skills;
using _Scripts.DevLib.ModuleSystem;
using Lean.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    // 공격 준비의 마지막 구간에 입자를 모으고, 모두 모인 다음 발동을 허용합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackPreparationModule : Module
    {
        [Header("Gathering")]
        [SerializeField] private bool particlesEnabled = true;
        [SerializeField] private ParticleSystem particlePrefab;
        [Tooltip("입자가 중심까지 모이는 시간입니다. 기존 준비 시간보다 길면 공격도 그만큼 기다립니다.")]
        [SerializeField, Min(0.05f)] private float gatherTime = 0.25f;
        [SerializeField, Range(1, 128)] private int particleCount = 40;
        [SerializeField] private Color particleColor = new Color(0.65f, 0.08f, 0.1f, 0.95f);
        [Tooltip("입자가 출발하는 중심으로부터의 최소·최대 거리입니다. 월드 유닛 기준입니다.")]
        [SerializeField] private Vector2 spawnRadius = new Vector2(0.8f, 1.25f);
        [Tooltip("입자 하나의 최소·최대 크기입니다. 월드 유닛 기준입니다.")]
        [SerializeField] private Vector2 particleSize = new Vector2(0.07f, 0.15f);

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
        private float _startsAt;
        private float _finishesAt;
        private float _duration;

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

            _preparingSkill = skill;
            _duration = Mathf.Max(0.05f, gatherTime);
            _startsAt = Time.time + Mathf.Max(0f, preparationDuration - _duration);
            _finishesAt = _startsAt + _duration;
            if (Time.time >= _startsAt) SpawnParticles();
        }

        internal bool CanReleaseAttack(EnemyAttackSkill skill) =>
            _preparingSkill != skill || !CanGather || Time.time >= _finishesAt;

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

            if (_particles == null && Time.time >= _startsAt) SpawnParticles();
            if (_particles != null) _particles.transform.position = GetCenter();
        }

        private Vector3 GetCenter()
        {
            Vector3 center = centerOverride != null ? centerOverride.position :
                _bodyCollider != null ? _bodyCollider.bounds.center : _enemy.transform.position;
            return center + (Vector3)centerOffset;
        }

        private void SpawnParticles()
        {
            // 늦게 시작한 프레임만큼 미리 진행해 공격 직전에 모든 입자가 모이도록 합니다.
            _particles = LeanPool.Spawn(particlePrefab, GetCenter(), Quaternion.identity);
            if (_particles == null) return;

            if (_particles.gameObject.scene != gameObject.scene)
                SceneManager.MoveGameObjectToScene(_particles.gameObject, gameObject.scene);
            _particles.transform.localScale = Vector3.one;
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            int count = Mathf.Clamp(particleCount, 1, 128);
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = _duration;
            main.startLifetime = _duration;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = count;
            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;

            // 속도를 0→2배로 늘려 가속하면서도 전체 이동 거리는 초기 속도×수명으로 맞춥니다.
            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 2f));
            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            if (sortingSource != null && renderer != null)
            {
                renderer.sortingLayerID = sortingSource.sortingLayerID;
                renderer.sortingOrder = Mathf.Clamp(sortingSource.sortingOrder + sortingOrderOffset, -32768, 32767);
            }

            float radiusMin = Mathf.Max(0f, Mathf.Min(spawnRadius.x, spawnRadius.y));
            float radiusMax = Mathf.Max(radiusMin, Mathf.Max(spawnRadius.x, spawnRadius.y));
            float sizeMin = Mathf.Max(0.01f, Mathf.Min(particleSize.x, particleSize.y));
            float sizeMax = Mathf.Max(sizeMin, Mathf.Max(particleSize.x, particleSize.y));
            _particles.Play(false);

            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(radiusMin, radiusMax);
                _particles.Emit(new ParticleSystem.EmitParams
                {
                    position = offset,
                    velocity = -offset / _duration,
                    startLifetime = _duration,
                    startSize = Random.Range(sizeMin, sizeMax),
                    startColor = particleColor,
                    applyShapeToPosition = false
                }, 1);
            }

            float elapsed = Mathf.Max(0f, Time.time - _startsAt);
            if (elapsed > 0f)
            {
                _particles.Simulate(elapsed, false, false, false);
                _particles.Play(false);
            }
        }

        private void ClearPreparation()
        {
            _preparingSkill = null;
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
