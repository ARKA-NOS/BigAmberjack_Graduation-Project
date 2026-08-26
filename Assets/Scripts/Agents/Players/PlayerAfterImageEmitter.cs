using System.Collections;
using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.Pool;

namespace Agents.Players
{
    public class PlayerAfterImageEmitter : Module, IAfterImageEmitter
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private AfterImageGhost afterImagePrefab;

        [SerializeField] private float spawnInterval = 0.035f;
        [SerializeField] private float ghostLifeTime = 0.18f;

        [SerializeField]
        private Color ghostColor = new(0.4f, 0.8f, 1f, 0.5f);

        private ObjectPool<AfterImageGhost> _pool;
        private Coroutine _emitCoroutine;

        private GameObject _poolBundle;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _poolBundle = new GameObject($"{sourceRenderer.transform.parent.name}_AfterImageGhost");

            _pool = new ObjectPool<AfterImageGhost>(
                CreateGhost,
                OnGetGhost,
                OnReleaseGhost,
                OnDestroyGhost,
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 32);
        }

        public void Play(float duration)
        {
            if (_emitCoroutine != null)
                StopCoroutine(_emitCoroutine);

            _emitCoroutine = StartCoroutine(
                EmitRoutine(duration));
        }

        private IEnumerator EmitRoutine(float duration)
        {
            float elapsed = 0f;
            float spawnTimer = spawnInterval;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                spawnTimer += Time.deltaTime;

                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer -= spawnInterval;
                    SpawnGhost();
                }

                yield return null;
            }

            _emitCoroutine = null;
        }

        private void SpawnGhost()
        {
            AfterImageGhost ghost = _pool.Get();

            ghost.Show(
                sourceRenderer,
                ghostColor,
                ghostLifeTime,
                ReleaseGhost);
        }

        private void ReleaseGhost(AfterImageGhost ghost)
        {
            _pool.Release(ghost);
        }

        private AfterImageGhost CreateGhost()
        {
            return Instantiate(afterImagePrefab, _poolBundle.transform);
        }

        private void OnGetGhost(AfterImageGhost ghost)
        {
            ghost.gameObject.SetActive(true);
        }

        private void OnReleaseGhost(AfterImageGhost ghost)
        {
            ghost.gameObject.SetActive(false);
        }

        private void OnDestroyGhost(AfterImageGhost ghost)
        {
            Destroy(ghost.gameObject);
        }
    }
}