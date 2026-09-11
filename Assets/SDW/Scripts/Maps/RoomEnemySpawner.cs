using System.Collections;
using System.Collections.Generic;
using CTJ.Enemies;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 방 프리팹에 부착해서, 방에 처음 입장했을 때 지정된 지점에 적을 생성한다.
    // DungeonGenerator(시작 방)와 Portal(그 외 방)이 방을 활성화하는 시점에 SpawnIfNeeded()를 호출한다.
    // 적이 하나라도 남아있는 동안은 이 방의 Portal을 잠가서 통로를 막는다.
    [RequireComponent(typeof(RoomDefinition))]
    public class RoomEnemySpawner : MonoBehaviour
    {
        private const float ClearCheckInterval = 0.2f;

        [SerializeField] private EnemyBase enemyPrefab;
        [SerializeField] private List<Transform> spawnPoints = new();

        private readonly List<EnemyBase> _spawnedEnemies = new();
        private bool _hasSpawned;

        public void SpawnIfNeeded()
        {
            if (_hasSpawned)
                return;

            _hasSpawned = true;

            if (enemyPrefab == null)
                return;

            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                    continue;

                EnemyBase enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, transform);
                _spawnedEnemies.Add(enemy);
            }

            if (_spawnedEnemies.Count == 0)
                return;

            SetPortalsLocked(true);
            StartCoroutine(WaitForRoomCleared());
        }

        // 적 사망 이벤트가 따로 없어서, 스폰한 적들이 전부 파괴됐는지 주기적으로 확인한다.
        private IEnumerator WaitForRoomCleared()
        {
            WaitForSeconds interval = new WaitForSeconds(ClearCheckInterval);

            while (_spawnedEnemies.Exists(enemy => enemy != null))
                yield return interval;

            SetPortalsLocked(false);
        }

        private void SetPortalsLocked(bool locked)
        {
            foreach (Portal portal in GetComponentsInChildren<Portal>(true))
                portal.SetLocked(locked);
        }
    }
}
