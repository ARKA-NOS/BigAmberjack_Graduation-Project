using UnityEngine;

namespace SDW.Scripts.Maps
{
    // Room Prefab의 루트에 붙여서, 플레이어 도착 위치와 출구 위치를 미리 지정해두는 컴포넌트.
    // 상자/NPC 위치는 InteractableSpawnPoint, 적 등장 위치는 EncounterZone이 각자 들고 있다.
    public class RoomDefinition : MonoBehaviour
    {
        [Header("Player Spawn")]
        [Tooltip("이 방에 들어왔을 때 플레이어가 놓일 위치. 비워두면 방의 위치를 사용한다.")]
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Exit")]
        [Tooltip("다음 방으로 가는 출구가 생성될 위치. 스테이지의 마지막 방(최종 보스)은 비워둔다.")]
        [SerializeField] private Transform exitPoint;

        [Header("Gizmos")]
        [SerializeField] private float gizmoSize = 1f;

        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Transform ExitPoint => exitPoint;

        // 좌/우 포탈은 없어졌지만 CTJ EnemyPatrolModule이 순찰 중심 계산에 아직 사용하므로 호환용으로 남겨둔다.
        // 항상 null을 반환하므로 순찰 중심은 방 위치로 떨어진다.
        // TODO: CTJ 순찰 모듈이 스폰 위치 기준으로 바뀌면 RoomDirection과 함께 삭제한다.
        public Transform GetPortalPoint(RoomDirection direction)
        {
            return null;
        }

        private void OnDrawGizmos()
        {
            DrawPoint(playerSpawnPoint, new Color(0.1f, 0.4f, 0.55f));
            DrawPoint(exitPoint, new Color(0.5f, 0.22f, 0.07f));
        }

        private void DrawPoint(Transform point, Color color)
        {
            if (point == null)
                return;

            Gizmos.color = color;
            Gizmos.DrawWireCube(point.position, new Vector3(gizmoSize, gizmoSize * 2f, 0f));
        }
    }
}
