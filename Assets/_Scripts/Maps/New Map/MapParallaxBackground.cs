using _Scripts._Core._EventSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Scripts.Maps.New_Map
{
    /*
     * 생성된 맵 전체에 까는 한 장짜리 원경 배경.
     * - 배경은 카메라를 따라다니며 화면을 덮지만, 카메라가 움직인 거리의 parallaxStrength만큼만 화면에서 밀려난다.
     *   그래서 플레이어가 왼쪽으로 많이 가도 배경은 조금만 오른쪽으로 밀려, 멀리 있는 것처럼 보인다.
     * - 맵 생성이 끝나면(MapGenerateEndEvent) 맵 범위를 읽고, 맵 끝까지 가도 배경 밖이 보이지 않도록 스프라이트를 필요한 만큼 키운다.
     * - SpriteRenderer의 스프라이트를 바꾸면 배경이 교체된다.
     *
     * 사용법: MapGenerator 오브젝트 아래에 SpriteRenderer와 함께 붙인다.
     */
    [RequireComponent(typeof(SpriteRenderer))]
    public class MapParallaxBackground : MonoBehaviour
    {
        // 타일맵(0), 적, 플레이어보다 항상 뒤에 그려지도록 충분히 낮게 둔다.
        private const int DefaultSortingOrder = -100;

        [Tooltip("카메라가 1 움직일 때 배경이 화면에서 밀려나는 비율. 0이면 화면에 고정(무한히 먼 배경), 1이면 월드에 고정(타일과 같은 속도). 작을수록 멀어 보인다.")]
        [SerializeField, Range(0f, 1f)] private float parallaxStrength = 0.1f;

        [Tooltip("화면을 딱 맞게 덮는 크기보다 얼마나 더 크게 그릴지. 카메라 흔들림 등으로 가장자리가 보이지 않게 하는 여유분.")]
        [SerializeField, Range(0f, 0.5f)] private float edgeMargin = 0.05f;

        private SpriteRenderer _spriteRenderer;
        private Bounds _mapBounds;
        private bool _hasMapBounds;

        private void Reset()
        {
            GetComponent<SpriteRenderer>().sortingOrder = DefaultSortingOrder;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // 꺼져 있는 동안 생성이 끝나도 범위를 놓치지 않도록 Awake~OnDestroy 동안 구독한다.
            EventBus<MapGenerateEndEvent>.Subscribe(HandleMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus<MapGenerateEndEvent>.UnSubscribe(HandleMapGenerated);
        }

        // 카메라 위치는 Cinemachine이 LateUpdate에서 정하므로, 순서에 상관없이 렌더링 직전에 맞춘다.
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        }

        // 벽 타일맵은 모든 방과 바깥 가장자리를 덮으므로 그 범위를 맵 범위로 쓴다.
        private void HandleMapGenerated(MapGenerateEndEvent evt)
        {
            _hasMapBounds = evt.WallFiller != null
                            && TilemapBoundsUtility.TryGetWorldBounds(evt.WallFiller.transform, out _mapBounds);
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!Application.isPlaying || cam != Camera.main || !cam.orthographic || _spriteRenderer.sprite == null)
                return;

            Vector2 viewSize = new Vector2(cam.orthographicSize * 2f * cam.aspect, cam.orthographicSize * 2f);
            Vector3 camPosition = cam.transform.position;

            // 카메라가 맵 중심에서 떨어진 거리(카메라가 갈 수 있는 범위로 제한)에 비례해 배경을 반대로 민다.
            Vector2 camRange = GetCameraRange(viewSize);
            Vector2 camOffset = _hasMapBounds ? (Vector2)camPosition - (Vector2)_mapBounds.center : Vector2.zero;
            camOffset.x = Mathf.Clamp(camOffset.x, -camRange.x, camRange.x);
            camOffset.y = Mathf.Clamp(camOffset.y, -camRange.y, camRange.y);
            Vector2 shift = camOffset * parallaxStrength;

            // 가장 많이 밀린 상태에서도 화면을 덮도록, 밀리는 최대 거리의 두 배만큼 더 크게 그린다.
            Vector2 needSize = (viewSize + camRange * (2f * parallaxStrength)) * (1f + edgeMargin);
            Bounds spriteBounds = _spriteRenderer.sprite.bounds;
            float scale = Mathf.Max(needSize.x / spriteBounds.size.x, needSize.y / spriteBounds.size.y);

            // 스프라이트 피벗이 가운데가 아니어도 배경 중심이 목표 위치에 오도록 보정한다.
            Vector2 center = (Vector2)camPosition - shift;
            Vector2 pivotOffset = (Vector2)spriteBounds.center * scale;

            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(center.x - pivotOffset.x, center.y - pivotOffset.y, transform.position.z);
        }

        // 화면이 맵 안에 있을 때 카메라 중심이 맵 중심에서 움직일 수 있는 거리.
        private Vector2 GetCameraRange(Vector2 viewSize)
        {
            if (!_hasMapBounds)
                return Vector2.zero;

            Vector2 range = (Vector2)_mapBounds.extents - viewSize * 0.5f;
            return Vector2.Max(range, Vector2.zero);
        }
    }
}
