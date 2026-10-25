using UnityEngine;
using UnityEngine.Rendering;

namespace SDW.Scripts.Maps
{
    // 방 프리팹 안에 두는 한 장짜리 배경. SpriteRenderer의 스프라이트를 바꾸면 배경이 교체된다.
    // 플레이 중에는 카메라를 따라다니며 화면을 덮고, 카메라가 방 끝으로 갈수록 배경도 그쪽 끝을 보여줘서 약한 패럴랙스가 생긴다.
    // 배경이 움직이는 양은 화면보다 남는 크기(parallaxMargin)만큼이라, 방 어디에 있어도 배경 밖이 보이지 않는다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoomBackground : MonoBehaviour
    {
        // 타일맵(0), 적, 플레이어보다 항상 뒤에 그려지도록 충분히 낮게 둔다.
        private const int DefaultSortingOrder = -100;

        [Tooltip("화면을 덮는 크기보다 얼마나 더 크게 그릴지. 이 남는 만큼 배경이 움직이므로 클수록 패럴랙스가 강해진다.")]
        [SerializeField, Range(0f, 1f)] private float parallaxMargin = 0.15f;

        private SpriteRenderer _spriteRenderer;
        private Bounds _roomBounds;
        private bool _hasRoomBounds;

        private void Reset()
        {
            GetComponent<SpriteRenderer>().sortingOrder = DefaultSortingOrder;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.drawMode = SpriteDrawMode.Simple;

            RoomDefinition room = GetComponentInParent<RoomDefinition>(true);
            _hasRoomBounds = room != null && TilemapBoundsUtility.TryGetWorldBounds(room.transform, out _roomBounds);
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

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!Application.isPlaying || cam != Camera.main || !cam.orthographic || _spriteRenderer.sprite == null)
                return;

            Vector2 viewSize = new Vector2(cam.orthographicSize * 2f * cam.aspect, cam.orthographicSize * 2f);
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

            // 화면을 빈틈없이 덮는 크기에 여유분을 더한다.
            float scale = Mathf.Max(viewSize.x / spriteSize.x, viewSize.y / spriteSize.y) * (1f + parallaxMargin);
            Vector2 slack = spriteSize * scale - viewSize;

            Vector3 camPosition = cam.transform.position;
            Vector2 travel = GetCameraTravel(camPosition, viewSize);

            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position = new Vector3(
                camPosition.x - travel.x * slack.x * 0.5f,
                camPosition.y - travel.y * slack.y * 0.5f,
                transform.position.z);
        }

        // 카메라가 방 안에서 움직일 수 있는 범위 중 어디쯤인지 (-1 왼쪽/아래 끝 ~ 1 오른쪽/위 끝).
        private Vector2 GetCameraTravel(Vector2 camPosition, Vector2 viewSize)
        {
            if (!_hasRoomBounds)
                return Vector2.zero;

            Vector2 range = (Vector2)_roomBounds.extents - viewSize * 0.5f;
            Vector2 offset = camPosition - (Vector2)_roomBounds.center;

            return new Vector2(
                range.x > 0f ? Mathf.Clamp(offset.x / range.x, -1f, 1f) : 0f,
                range.y > 0f ? Mathf.Clamp(offset.y / range.y, -1f, 1f) : 0f);
        }
    }
}
