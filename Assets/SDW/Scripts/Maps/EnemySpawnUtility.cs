using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Data;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 보스 방(RoomDataSO)과 전투 구역(EncounterZone)이 같이 쓰는 적 생성 로직.
    // 생성한 적은 방의 자식으로 두고 RoomController에 등록해서, 출구 조건과 상호작용 잠금이 이 적들을 보게 한다.
    public static class EnemySpawnUtility
    {
        // 스폰 포인트를 순서대로 돌아가며 엔트리의 적을 생성한다.
        // 적 수가 스폰 포인트보다 많아 같은 포인트를 다시 쓰면, 겹치지 않도록 좌우 랜덤으로 옆에 생성한다.
        // (겹쳐 있으면 한 번의 공격에 여러 마리가 같이 죽는다)
        public static List<EnemyBase> SpawnEnemies(RoomController room, IReadOnlyList<Transform> spawnPoints,
            IReadOnlyList<EnemySpawnEntry> entries, float sameSpawnPointSpacing)
        {
            List<EnemyBase> spawned = new();

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning($"{room.name}에 적 스폰 포인트가 없어 적을 생성하지 않았습니다.");
                return spawned;
            }

            int pointIndex = 0;

            foreach (EnemySpawnEntry entry in entries)
            {
                if (entry == null || entry.EnemyPrefab == null)
                    continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    // 몇 번째로 같은 포인트를 쓰는지 (0이면 처음)
                    int reuseCount = pointIndex / spawnPoints.Count;
                    Transform spawnPoint = spawnPoints[pointIndex++ % spawnPoints.Count];
                    if (spawnPoint == null)
                        continue;

                    Vector3 position = spawnPoint.position + GetReuseOffset(reuseCount, sameSpawnPointSpacing);
                    spawned.Add(SpawnEnemy(room, entry.EnemyPrefab, position));
                }
            }

            return spawned;
        }

        // 방에 스폰 포탈이 지정되어 있으면 포탈 연출이 끝난 뒤에 적이 나온다. 없으면 바로 생성한다.
        public static EnemyBase SpawnEnemy(RoomController room, EnemyBase enemyPrefab, Vector3 position)
        {
            EnemyBase enemy = room.EnemySpawnPortalPrefab != null
                ? EnemySpawnPortal.Spawn(room.EnemySpawnPortalPrefab, enemyPrefab, position, room.transform)
                : Object.Instantiate(enemyPrefab, position, Quaternion.identity, room.transform);
            room.RegisterEnemy(enemy);
            return enemy;
        }

        // 다시 쓸수록 더 멀리, 좌우는 랜덤으로 밀어서 같은 포인트의 적끼리 겹치지 않게 한다.
        private static Vector3 GetReuseOffset(int reuseCount, float spacing)
        {
            if (reuseCount == 0)
                return Vector3.zero;

            float side = Random.value < 0.5f ? -1f : 1f;
            return Vector3.right * (side * spacing * reuseCount);
        }
    }
}
