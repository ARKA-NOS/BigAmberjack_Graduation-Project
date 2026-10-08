using UnityEngine;

namespace SDW.Scripts.Maps
{
    // Room Prefab의 루트에 붙여서, 포탈 생성 위치와 플레이어 스폰 위치를 미리 지정해두는 컴포넌트.
    public class RoomDefinition : MonoBehaviour
    {
        [Header("Portal Points")]
        [SerializeField] private Transform leftPortalPoint;
        [SerializeField] private Transform rightPortalPoint;

        [Header("Player Spawn")]
        [Tooltip("시작 방일 때 플레이어가 생성될 위치. 비워두면 방의 위치를 사용한다.")]
        [SerializeField] private Transform spawnPoint;

        public Transform SpawnPoint => spawnPoint;

        public Transform GetPortalPoint(RoomDirection direction)
        {
            return direction == RoomDirection.Left ? leftPortalPoint : rightPortalPoint;
        }
    }
}
