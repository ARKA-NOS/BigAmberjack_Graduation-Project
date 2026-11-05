using Unity.Cinemachine;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 방에 들어오면 카메라가 이 방 밖(다른 방이나 빈 공간)을 비추지 않도록 Cinemachine Confiner 2D의 범위를 이 방으로 바꾼다.
    // 범위는 customBounds가 있으면 그 콜라이더, 없으면 방 안의 모든 타일맵을 감싸는 사각형이다.
    // 카메라는 플레이어 프리팹(Lrw)에 있으므로, Confiner 2D가 없으면 런타임에 붙인다.
    [RequireComponent(typeof(RoomController))]
    public class RoomCameraBounds : MonoBehaviour
    {
        [Tooltip("카메라가 비출 수 있는 범위. 비워두면 방의 타일맵 전체를 감싸는 사각형을 쓴다.")]
        [SerializeField] private Collider2D customBounds;

        private RoomController _room;
        private Collider2D _shape;

        private void Awake()
        {
            _room = GetComponent<RoomController>();
            _room.Entered += Confine;

            if (customBounds != null)
            {
                _shape = customBounds;
            }
            else if (TilemapBoundsUtility.TryGetWorldBounds(transform, out Bounds bounds))
            {
                _shape = CreateShape(bounds);
            }
            else
            {
                Debug.LogWarning($"{name} : 타일맵이 없어 카메라 범위를 만들지 않았습니다.");
            }
        }

        private void OnDestroy()
        {
            if (_room != null)
                _room.Entered -= Confine;
        }

        private void Confine()
        {
            if (_shape == null)
                return;

            foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            {
                if (!cam.TryGetComponent(out CinemachineConfiner2D confiner))
                    confiner = cam.gameObject.AddComponent<CinemachineConfiner2D>();

                confiner.BoundingShape2D = _shape;
                confiner.InvalidateBoundingShapeCache();

                // 방을 옮기면 플레이어가 순간이동하므로, 이전 방에서 따라오지 않고 바로 새 위치를 비추게 한다.
                cam.PreviousStateIsValid = false;
            }
        }

        // 범위와 같은 사각형 콜라이더를 만든다.
        // 콜라이더는 Confiner가 모양만 읽으므로 꺼둔다. (켜두면 방 전체가 트리거가 되어 다른 충돌 판정에 걸린다)
        private Collider2D CreateShape(Bounds bounds)
        {
            GameObject shapeObject = new GameObject("CameraBounds");
            shapeObject.transform.SetParent(transform, false);

            PolygonCollider2D polygon = shapeObject.AddComponent<PolygonCollider2D>();
            Vector2 min = shapeObject.transform.InverseTransformPoint(bounds.min);
            Vector2 max = shapeObject.transform.InverseTransformPoint(bounds.max);
            polygon.SetPath(0, new[]
            {
                new Vector2(min.x, min.y),
                new Vector2(max.x, min.y),
                new Vector2(max.x, max.y),
                new Vector2(min.x, max.y)
            });
            polygon.isTrigger = true;
            polygon.enabled = false;

            return polygon;
        }
    }
}
