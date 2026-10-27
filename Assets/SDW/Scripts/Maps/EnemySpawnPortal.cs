using System.Collections;
using CTJ.Enemies;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 적이 등장할 때 쓰는 포탈 연출. 포탈이 열리면서 소용돌이 애니메이션을 재생하고, 애니메이션이 끝나면 적을 꺼낸 뒤 닫힌다.
    // 적은 생성 즉시 방에 등록되지만, 연출이 끝날 때까지 비활성 홀더 아래에 있어서 Awake/AI가 돌지 않는다.
    // (방의 출구 조건과 웨이브는 아직 나오지 않은 적도 살아 있는 적으로 보므로 그 사이에 클리어되지 않는다)
    // 열고 닫을 때 크기를 바꾸는 건 spriteRenderer 오브젝트뿐이다. 루트까지 바꾸면 홀더 아래의 적 크기도 같이 바뀐다.
    public class EnemySpawnPortal : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float frameRate = 12f;
        [Tooltip("적이 나오기 전까지 소용돌이 애니메이션을 몇 번 재생할지")]
        [SerializeField, Min(1)] private int loopsBeforeSpawn = 2;
        [Tooltip("적 위치(피벗) 기준 포탈 중심 위치")]
        [SerializeField] private Vector2 offset;

        [Header("Open / Close")]
        [SerializeField, Min(0f)] private float openDuration = 0.25f;
        [Tooltip("적을 꺼낸 뒤 포탈이 닫히기 시작할 때까지 기다리는 시간")]
        [SerializeField, Min(0f)] private float lingerDuration = 0.15f;
        [SerializeField, Min(0f)] private float closeDuration = 0.25f;

        private Transform _pendingHolder;
        private Vector3 _baseScale;
        private float _animationTime;

        private void Awake()
        {
            _baseScale = spriteRenderer.transform.localScale;

            // 꺼내기 전의 적을 넣어둘 비활성 홀더. 부모가 비활성이면 적의 Awake/Start가 미뤄진다.
            _pendingHolder = new GameObject("PendingEnemy").transform;
            _pendingHolder.SetParent(transform, false);
            _pendingHolder.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
                return;

            _animationTime += Time.deltaTime;
            spriteRenderer.sprite = frames[(int)(_animationTime * frameRate) % frames.Length];
        }

        // position에 포탈을 열고 적을 생성한다. 적은 연출이 끝나면 releaseParent(방)의 자식으로 옮겨져 활성화된다.
        public static EnemyBase Spawn(EnemySpawnPortal portalPrefab, EnemyBase enemyPrefab, Vector3 position,
            Transform releaseParent)
        {
            EnemySpawnPortal portal = Instantiate(portalPrefab, position, Quaternion.identity, releaseParent);
            portal.transform.position += (Vector3)portal.offset;

            EnemyBase enemy = Instantiate(enemyPrefab, position, Quaternion.identity, portal._pendingHolder);
            portal.StartCoroutine(portal.Play(enemy, releaseParent));
            return enemy;
        }

        private IEnumerator Play(EnemyBase enemy, Transform releaseParent)
        {
            float animationLength = frames != null && frames.Length > 0 ? loopsBeforeSpawn * frames.Length / frameRate : 0f;

            // 열림: 애니메이션을 재생하면서 크기를 키운다.
            yield return ScaleTo(0f, 1f, openDuration);

            // 소용돌이 애니메이션이 끝날 때까지 기다린 뒤 적을 꺼낸다.
            while (_animationTime < animationLength)
                yield return null;

            if (enemy != null)
                enemy.transform.SetParent(releaseParent, true);

            yield return new WaitForSeconds(lingerDuration);

            // 닫힘
            yield return ScaleTo(1f, 0f, closeDuration);

            Destroy(gameObject);
        }

        private IEnumerator ScaleTo(float from, float to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                spriteRenderer.transform.localScale = _baseScale * Mathf.Lerp(from, to, t);
                yield return null;
            }

            spriteRenderer.transform.localScale = _baseScale * to;
        }
    }
}
