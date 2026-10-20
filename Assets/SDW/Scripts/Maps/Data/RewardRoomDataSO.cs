using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 보상방. 입장하면 Reward Chest Spawn Point마다 상자를 하나씩 배치하고, 모두 열 수 있다.
    // 확정 보상이라 상자 등장 확률(chestSpawnChance)은 쓰지 않고, 후보와 가중치만 사용한다.
    // 클리어 조건은 기본값(AlwaysClearedCondition)이라 입장 즉시 포탈이 열린다.
    [CreateAssetMenu(fileName = "Room_Reward", menuName = "Map/Room/Reward Room")]
    public class RewardRoomDataSO : RoomDataSO
    {
        public override RoomType RoomType => RoomType.Reward;

        public override void OnFirstEnter(RoomController room)
        {
            IReadOnlyList<Transform> spawnPoints = room.Definition != null
                ? room.Definition.RewardChestSpawnPoints
                : null;

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning($"{room.name}에 Reward Chest Spawn Point가 없어 기본 위치에 상자 하나만 생성합니다.");
                SpawnRandomChest(room, GetDefaultChestPosition(room));
                return;
            }

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                    SpawnRandomChest(room, spawnPoint.position);
            }
        }
    }
}
