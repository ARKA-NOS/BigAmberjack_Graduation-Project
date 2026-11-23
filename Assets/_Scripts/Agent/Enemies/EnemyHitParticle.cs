using Lean.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    // 파티클은 적과 따로 재생하고, 수명이 끝나면 자신이 빌려 온 풀로 돌아갑니다.
    [DisallowMultipleComponent]
    public sealed class EnemyHitParticle : MonoBehaviour
    {
        private ParticleSystem _root;
        private EnemyParticleModule _owner;
        private bool _configured;
        private float _startedAt;
        private float _expiresAt;

        internal static EnemyHitParticle Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation,
            Scene scene, float scale, float maximumLifetime, EnemyParticleModule owner = null,
            float speedMultiplier = 1f, float countMultiplier = 1f)
        {
            ParticleSystem particle = LeanPool.Spawn(prefab, position, rotation);
            if (particle == null) return null;

            // 발사나 사망 후에도 재생을 이어가되, 적이나 탄이 있던 씬을 벗어나지 않습니다.
            if (particle.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(particle.gameObject, scene);
            particle.transform.localScale = prefab.transform.localScale * scale;
            if (!particle.TryGetComponent(out EnemyHitParticle lifetime))
                lifetime = particle.gameObject.AddComponent<EnemyHitParticle>();

            lifetime._owner = owner;
            lifetime.Play(particle, maximumLifetime, speedMultiplier, countMultiplier);
            return lifetime;
        }

        internal void Play(ParticleSystem root, float maximumLifetime, float speedMultiplier = 1f, float countMultiplier = 1f)
        {
            _root = root;
            _root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (!_configured)
            {
                foreach (ParticleSystem particle in GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particle.main;
                    main.loop = false;
                    main.playOnAwake = false;
                    // 루트와 자식의 재생이 끝날 때까지 반환 시점은 이 컴포넌트가 관리합니다.
                    main.stopAction = ParticleSystemStopAction.None;
                }

                _configured = true;
            }

            _startedAt = Time.time;
            _expiresAt = _startedAt + Mathf.Max(0.1f, maximumLifetime);
            _root.Play(true);

            if (TryGetComponent(out EnemyBloodParticle blood))
            {
                // 흔적 유지 시간은 방출이 아닌 표면 접촉부터 세므로 일반 효과보다 수명이 깁니다.
                _expiresAt = _startedAt + Mathf.Max(maximumLifetime, blood.MaximumLifetime);
                blood.Emit(speedMultiplier, countMultiplier);
            }
            else if (TryGetComponent(out EnemyMetalFragmentParticle metal))
            {
                _expiresAt = _startedAt + Mathf.Max(maximumLifetime, metal.MaximumLifetime);
                metal.Emit(_root, speedMultiplier, countMultiplier);
            }
        }

        private void Update()
        {
            if (_root != null && (Time.time >= _expiresAt ||
                (Time.time > _startedAt + 0.05f && !_root.IsAlive(true))))
                Release();
        }

        internal void Release()
        {
            if (_root == null) return;

            if (LeanPool.Links.TryGetValue(gameObject, out var pool) && pool != null)
                LeanPool.Despawn(gameObject);
            else
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            if (_root != null) _root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _root = null;
            if (_owner != null) _owner.RemoveActiveEffect(this);
            _owner = null;
        }

        private void OnDestroy()
        {
            // 가산 로드된 전투 씬이 먼저 닫히면 활성 클론만 풀보다 먼저 제거될 수 있습니다.
            if (LeanPool.Links.TryGetValue(gameObject, out var pool))
                LeanPool.Detach(gameObject, pool != null);
        }
    }
}
