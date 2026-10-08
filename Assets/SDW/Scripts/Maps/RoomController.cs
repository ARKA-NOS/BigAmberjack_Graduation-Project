using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Conditions;
using SDW.Scripts.Maps.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace SDW.Scripts.Maps
{
    // 생성된 방 인스턴스의 런타임 상태를 관리한다.
    // 무엇을 할지는 RoomDataSO(자식 SO)가 정하고, 이 컴포넌트는 입장 → 조건 시작 → 클리어 순서만 책임진다.
    // 클리어 전에는 이 방의 모든 Portal을 잠가서 앞뒤로 이동할 수 없게 한다.
    [RequireComponent(typeof(RoomDefinition))]
    public class RoomController : MonoBehaviour
    {
        [Tooltip("적이 생성될 위치. 어떤 적을 몇 마리 생성할지는 RoomDataSO가 정한다.")]
        [SerializeField, FormerlySerializedAs("spawnPoints")] private List<Transform> enemySpawnPoints = new();

        private readonly List<EnemyBase> _spawnedEnemies = new();
        private IRoomClearCondition _clearCondition;
        private bool _hasEntered;

        public RoomDataSO Data { get; private set; }
        public RoomDefinition Definition { get; private set; }
        public int Index { get; private set; }
        public bool IsCleared { get; private set; }

        public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
        public IReadOnlyList<EnemyBase> SpawnedEnemies => _spawnedEnemies;

        public void Initialize(RoomDataSO data, int index)
        {
            Data = data;
            Index = index;
            Definition = GetComponent<RoomDefinition>();
        }

        // LinearStageGenerator(시작 방)와 Portal(그 외 방)이 방을 활성화한 직후 호출한다.
        // 처음 입장할 때만 동작하므로, 클리어한 방에 다시 들어와도 적이 재생성되지 않는다.
        public void Enter()
        {
            if (_hasEntered)
                return;

            _hasEntered = true;

            if (Data == null)
            {
                Debug.LogWarning($"{name} : RoomDataSO가 없어 입장 처리를 건너뜁니다.");
                return;
            }

            Data.OnFirstEnter(this);

            _clearCondition = Data.CreateClearCondition(this);
            _clearCondition.Cleared += HandleCleared;

            SetPortalsLocked(true);
            _clearCondition.Begin();
        }

        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy != null)
                _spawnedEnemies.Add(enemy);
        }

        private void HandleCleared()
        {
            if (IsCleared)
                return;

            IsCleared = true;

            _clearCondition.Cleared -= HandleCleared;
            _clearCondition.Dispose();
            _clearCondition = null;

            SetPortalsLocked(false);
            Data.OnCleared(this);
        }

        private void SetPortalsLocked(bool locked)
        {
            foreach (Portal portal in GetComponentsInChildren<Portal>(true))
                portal.SetLocked(locked);
        }

        private void OnDestroy()
        {
            _clearCondition?.Dispose();
        }
    }
}
