using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Boss", menuName = "SDW/Map/Room/Boss Room")]
    public class BossRoomDataSO : RoomDataSO
    {
        [SerializeField] private EnemyBase bossPrefab;
        [SerializeField] private List<EnemySpawnEntry> minions = new();

        public override RoomType RoomType => RoomType.Boss;

        public override void OnFirstEnter(RoomController room)
        {
            // 보스는 첫 번째 적 스폰 포인트(없으면 방 중앙)에 생성한다.
            if (bossPrefab != null)
            {
                Transform bossPoint = room.EnemySpawnPoints.Count > 0 ? room.EnemySpawnPoints[0] : null;
                Vector3 position = bossPoint != null ? bossPoint.position : room.transform.position;
                SpawnEnemy(room, bossPrefab, position);
            }

            SpawnEnemies(room, minions);
        }

        protected override IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }

        // TODO: 스테이지 클리어 처리(결과 화면, 다음 스테이지 이동 등)가 생기면 여기서 호출한다.
        public override void OnCleared(RoomController room)
        {
            Debug.Log($"{name} : 보스 방 클리어");
        }
    }
}
