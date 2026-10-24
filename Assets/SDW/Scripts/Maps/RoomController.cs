using System;
using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Maps.Conditions;
using SDW.Scripts.Maps.Data;
using SDW.Scripts.Maps.Encounter;
using SDW.Scripts.Maps.Spawn;
using UnityEngine;
using UnityEngine.Serialization;

namespace SDW.Scripts.Maps
{
    // 생성된 방 인스턴스의 런타임 상태를 관리한다.
    // 무엇을 할지는 RoomDataSO(자식 SO)가 정하고, 이 컴포넌트는 입장 → 조건 시작 → 클리어 순서만 책임진다.
    // 클리어(출구 조건 달성) 전에는 이 방의 출구 Portal을 잠근다. 넓은 맵은 입장 즉시 클리어되어 출구가 바로 열린다.
    [RequireComponent(typeof(RoomDefinition))]
    public class RoomController : MonoBehaviour
    {
        [Tooltip("보스 같은 방 단위 적이 생성될 위치. 전투 구역의 적 위치는 EncounterZone이 따로 가진다.")]
        [SerializeField, FormerlySerializedAs("spawnPoints")] private List<Transform> enemySpawnPoints = new();

        private readonly List<EnemyBase> _spawnedEnemies = new();
        private EncounterZone[] _encounterZones = Array.Empty<EncounterZone>();
        private IRoomClearCondition _clearCondition;
        private bool _hasEntered;

        public RoomDataSO Data { get; private set; }
        public RoomDefinition Definition { get; private set; }
        public int Index { get; private set; }
        public bool IsCleared { get; private set; }

        // 클리어 조건을 달성한 직후 호출된다. 스테이지 마지막 방이면 생성기가 이 시점에 탐험 종료를 알린다.
        public event Action Cleared;

        public IReadOnlyList<Transform> EnemySpawnPoints => enemySpawnPoints;
        public IReadOnlyList<EnemyBase> SpawnedEnemies => _spawnedEnemies;

        public void Initialize(RoomDataSO data, int index)
        {
            Data = data;
            Index = index;
            Definition = GetComponent<RoomDefinition>();
            _encounterZones = GetComponentsInChildren<EncounterZone>(true);
        }

        // LinearStageGenerator(첫 방)와 Portal(그 외 방)이 방을 활성화한 직후 호출한다.
        // 처음 입장할 때만 동작한다.
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

            SpawnInteractables();
            Data.OnFirstEnter(this);

            _clearCondition = Data.CreateClearCondition(this);
            _clearCondition.Cleared += HandleCleared;

            SetPortalsLocked(true);
            _clearCondition.Begin();
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            Transform spawnPoint = Definition != null ? Definition.PlayerSpawnPoint : null;
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        public void RegisterEnemy(EnemyBase enemy)
        {
            if (enemy != null)
                _spawnedEnemies.Add(enemy);
        }

        // 죽는 연출 중(IsDead)이거나 이미 파괴된 적은 살아 있는 적으로 치지 않는다.
        public static bool IsAlive(EnemyBase enemy)
        {
            return enemy != null && !enemy.IsDead;
        }

        public bool HasAliveEnemy()
        {
            foreach (EnemyBase enemy in _spawnedEnemies)
            {
                if (IsAlive(enemy))
                    return true;
            }

            return false;
        }

        public bool HasAliveEnemyWithin(Vector2 position, float radius)
        {
            float sqrRadius = radius * radius;

            foreach (EnemyBase enemy in _spawnedEnemies)
            {
                if (IsAlive(enemy) && ((Vector2)enemy.transform.position - position).sqrMagnitude <= sqrRadius)
                    return true;
            }

            return false;
        }

        // 상자/NPC 상호작용을 막아야 하는지. InteractionLock이 주기적으로 묻는다.
        // 1) 반경 안에 살아 있는 적이 있거나, 2) 반경 안의 웨이브형 전투가 아직 끝나지 않았으면 잠근다.
        public bool IsInteractionLocked(Vector2 position, float radius)
        {
            if (HasAliveEnemyWithin(position, radius))
                return true;

            foreach (EncounterZone zone in _encounterZones)
            {
                if (zone != null && zone.LocksInteractionAt(position, radius))
                    return true;
            }

            return false;
        }

        // 프리팹에 배치된 상자/NPC 등 상호작용 오브젝트 포인트를 모두 실행한다.
        private void SpawnInteractables()
        {
            foreach (InteractableSpawnPoint point in GetComponentsInChildren<InteractableSpawnPoint>(true))
                point.Spawn(this);
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
            Cleared?.Invoke();
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
