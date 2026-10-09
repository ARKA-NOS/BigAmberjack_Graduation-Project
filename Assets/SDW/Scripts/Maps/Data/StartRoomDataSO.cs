using SDW.Scripts.NPC;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Start", menuName = "Map/Room/Start Room")]
    public class StartRoomDataSO : RoomDataSO
    {
        [Tooltip("시작 방에 배치할 NPC. 비워두면 배치하지 않는다.")]
        [SerializeField] private Npc npcPrefab;
        [Tooltip("플레이어 시작 위치(SpawnPoint) 기준 NPC 배치 오프셋")]
        [SerializeField] private Vector2 npcOffset = new(2f, 0.5f);

        public override RoomType RoomType => RoomType.Start;

        public override void OnFirstEnter(RoomController room)
        {
            SpawnNpc(room);
        }

        private void SpawnNpc(RoomController room)
        {
            if (npcPrefab == null)
                return;

            Transform spawnPoint = room.Definition != null ? room.Definition.SpawnPoint : null;
            Vector3 basePosition = spawnPoint != null ? spawnPoint.position : room.transform.position;

            Instantiate(npcPrefab, basePosition + (Vector3)npcOffset, Quaternion.identity, room.transform);
        }
    }
}
