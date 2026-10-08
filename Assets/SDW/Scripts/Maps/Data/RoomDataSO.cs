using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 방 데이터의 부모 SO. 방 종류마다 이 클래스를 상속한 자식 SO를 만들고 OnFirstEnter를 구현한다.
    // RoomController는 이 부모 타입만 알고, 입장 → 클리어 조건 시작 → 클리어 처리 순서로 호출한다.
    public abstract class RoomDataSO : ScriptableObject
    {
        [SerializeField] private RoomDefinition roomPrefab;
        [Tooltip("문 해제 조건. 비워두면 방 종류별 기본 조건(CreateDefaultClearCondition)을 사용한다.")]
        [SerializeField] private RoomClearConditionSO clearCondition;

        public RoomDefinition RoomPrefab => roomPrefab;
        public abstract RoomType RoomType { get; }

        // 방에 처음 입장했을 때 할 일 (적 스폰, 보스 연출, 보상 배치 등).
        public abstract void OnFirstEnter(RoomController room);

        // 클리어 조건을 달성했을 때 할 일 (보상, 스테이지 클리어 등).
        public virtual void OnCleared(RoomController room)
        {
        }

        public IRoomClearCondition CreateClearCondition(RoomController room)
        {
            return clearCondition != null ? clearCondition.Create(room) : CreateDefaultClearCondition(room);
        }

        protected virtual IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new AlwaysClearedCondition();
        }

        // 프리팹의 적 스폰 포인트를 순서대로 돌아가며 엔트리의 적을 생성한다.
        protected static void SpawnEnemies(RoomController room, IReadOnlyList<EnemySpawnEntry> entries)
        {
            IReadOnlyList<Transform> spawnPoints = room.EnemySpawnPoints;
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning($"{room.name}에 적 스폰 포인트가 없어 적을 생성하지 않았습니다.");
                return;
            }

            int pointIndex = 0;

            foreach (EnemySpawnEntry entry in entries)
            {
                if (entry == null || entry.EnemyPrefab == null)
                    continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    Transform spawnPoint = spawnPoints[pointIndex++ % spawnPoints.Count];
                    if (spawnPoint == null)
                        continue;

                    SpawnEnemy(room, entry.EnemyPrefab, spawnPoint.position);
                }
            }
        }

        protected static EnemyBase SpawnEnemy(RoomController room, EnemyBase enemyPrefab, Vector3 position)
        {
            EnemyBase enemy = Instantiate(enemyPrefab, position, Quaternion.identity, room.transform);
            room.RegisterEnemy(enemy);
            return enemy;
        }
    }
}
