using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 보스 방. 미니 보스와 최종 보스를 같이 관리한다.
    // 최종 보스인지는 이 SO가 아니라 스테이지의 마지막 방인지로 정한다.
    // (마지막 방이 아니면 출구가 생기고, 마지막 방이면 LinearStageGenerator가 클리어 시 탐험 종료를 알린다)
    [CreateAssetMenu(fileName = "Room_Boss", menuName = "Map/Room/Boss Room")]
    public class BossRoomDataSO : RoomDataSO
    {
        [SerializeField] private EnemyBase bossPrefab;
        [SerializeField] private List<EnemySpawnEntry> minions = new();
        [Tooltip("부하 수가 스폰 포인트보다 많아 같은 포인트를 다시 쓸 때, 좌우로 띄워 생성하는 간격")]
        [SerializeField, Min(0f)] private float sameSpawnPointSpacing = 1.5f;

        public override RoomType RoomType => RoomType.Boss;

        public override void OnFirstEnter(RoomController room)
        {
            // 보스는 첫 번째 적 스폰 포인트(없으면 방 중앙)에 생성한다.
            if (bossPrefab != null)
            {
                Transform bossPoint = room.EnemySpawnPoints.Count > 0 ? room.EnemySpawnPoints[0] : null;
                Vector3 position = bossPoint != null ? bossPoint.position : room.transform.position;
                EnemySpawnUtility.SpawnEnemy(room, bossPrefab, position);
            }

            if (minions.Count > 0)
                EnemySpawnUtility.SpawnEnemies(room, room.EnemySpawnPoints, minions, sameSpawnPointSpacing);
        }

        // 보스와 부하를 모두 처치해야 출구가 열린다.
        protected override IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }
    }
}
