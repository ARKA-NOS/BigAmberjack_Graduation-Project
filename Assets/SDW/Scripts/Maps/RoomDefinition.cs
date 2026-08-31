using UnityEngine;

namespace SDW.Scripts.Maps
{
    // Room Prefab의 루트에 붙여서, 이 프리팹이 어떤 방향을 지원하는지와
    // 각 방향의 포탈 생성 위치를 미리 지정해두는 컴포넌트.
    public class RoomDefinition : MonoBehaviour
    {
        [SerializeField] private RoomDirection supportedDirections;

        [Header("Portal Points")]
        [SerializeField] private Transform upPortalPoint;
        [SerializeField] private Transform downPortalPoint;
        [SerializeField] private Transform leftPortalPoint;
        [SerializeField] private Transform rightPortalPoint;

        [Header("Player Spawn")]
        [Tooltip("시작 방일 때 플레이어가 생성될 위치. 비워두면 방의 위치를 사용한다.")]
        [SerializeField] private Transform spawnPoint;

        public RoomDirection SupportedDirections => supportedDirections;
        public Transform SpawnPoint => spawnPoint;

        // actualDirections가 SupportedDirections의 부분집합인지 확인 (14. 부분집합 규칙)
        public bool Supports(RoomDirection actualDirections)
        {
            return (supportedDirections & actualDirections) == actualDirections;
        }

        public Transform GetPortalPoint(RoomDirection direction)
        {
            int index = direction.Index();
            if (index < 0)
                return null;

            // upPortalPoint/downPortalPoint/leftPortalPoint/rightPortalPoint는
            // RoomDirectionExtensions.All(Up, Down, Left, Right)과 같은 순서를 따른다.
            Transform[] portalPoints = { upPortalPoint, downPortalPoint, leftPortalPoint, rightPortalPoint };
            return portalPoints[index];
        }
    }
}
