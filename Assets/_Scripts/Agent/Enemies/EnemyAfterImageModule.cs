using _Scripts.Agent.Players;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAfterImageModule : Module
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private AfterImageGhost ghostPrefab;
        [SerializeField, Min(0.01f)] private float spawnInterval = 0.035f;
        [SerializeField, Min(0.01f)] private float lifetime = 0.18f;
        [SerializeField] private Color ghostColor = new Color(0.45f, 1f, 0.65f, 0.45f);

        private ObjectPool<AfterImageGhost> _pool;
        private GameObject _poolRoot;
        private EnemyBase _enemy;
        private bool _emitting;
        private bool _disposed;
        private float _nextSpawn;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _poolRoot = new GameObject(owner.name + " AfterImages");
            SceneManager.MoveGameObjectToScene(_poolRoot, owner.gameObject.scene);
            _pool = new ObjectPool<AfterImageGhost>(
                () => Instantiate(ghostPrefab, _poolRoot.transform),
                ghost => { if (ghost != null) ghost.gameObject.SetActive(true); },
                ghost => { if (ghost != null) ghost.gameObject.SetActive(false); },
                ghost => { if (ghost != null) Destroy(ghost.gameObject); }, true, 8, 32);
        }

        public void Play()
        {
            _emitting = isActiveAndEnabled && _pool != null && ghostPrefab != null && sourceRenderer != null;
            _nextSpawn = Time.time;
        }

        public void Stop() => _emitting = false;

        private void Update()
        {
            if (!_emitting || _enemy == null || _enemy.IsDead) return;
            if (Time.time < _nextSpawn) return;

            _nextSpawn = Time.time + Mathf.Max(0.01f, spawnInterval);
            _pool.Get().Show(sourceRenderer, ghostColor, lifetime, ReleaseGhost);
        }

        private void ReleaseGhost(AfterImageGhost ghost)
        {
            if (ghost == null) return;

            if (_disposed) Destroy(ghost.gameObject);
            else _pool.Release(ghost);
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            _disposed = true;
            _pool?.Clear();

            if (_poolRoot != null) Destroy(_poolRoot);
        }
    }
}
