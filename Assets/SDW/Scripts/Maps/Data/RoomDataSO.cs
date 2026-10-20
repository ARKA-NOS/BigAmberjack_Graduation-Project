using System.Collections.Generic;
using CTJ.Enemies;
using SDW.Scripts.Interact.Chest;
using SDW.Scripts.Interact.NPC;
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
        
        // 등장 확률은 모두 방에서 정한다. 같은 NPC/상자라도 방마다 다른 확률을 줄 수 있다.
        [Header("NPC")]
        [Tooltip("방에 배치할 NPC. 비워두면 배치하지 않는다.")]
        [SerializeField] private Npc npcPrefab;
        [Tooltip("NPC 등장 확률 (0~1)")]
        [SerializeField, Range(0f, 1f)] private float npcSpawnChance = 1f;
        [Tooltip("NPC Spawn Point가 없을 때 방 원점 기준 NPC 배치 오프셋")]
        [SerializeField] private Vector2 npcOffset = new(2f, 0.5f);

        [Header("Chest")]
        [Tooltip("보물상자 등장 확률 (0~1). 성공하면 아래 후보 중 가중치로 하나를 고른다.")]
        [SerializeField, Range(0f, 1f)] private float chestSpawnChance = 1f;
        [Tooltip("보물상자 후보와 가중치")]
        [SerializeField] private List<ChestSpawnEntry> chestCandidates = new();
        [Tooltip("Chest Spawn Point가 없을 때 방 원점 기준 상자 배치 오프셋")]
        [SerializeField] private Vector2 chestOffset = new(-2f, 0.5f);

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
        
        // NPC 등장 확률을 굴려 성공하면 배치한다. 배치했으면 true.
        protected bool TrySpawnNpc(RoomController room)
        {
            if (npcPrefab == null || Random.value >= npcSpawnChance)
                return false;

            Transform spawnPoint = room.Definition != null ? room.Definition.NPCSpawnPoint : null;
            Vector3 basePosition = spawnPoint != null ? spawnPoint.position : room.transform.position + (Vector3)npcOffset;

            Instantiate(npcPrefab, basePosition, Quaternion.identity, room.transform);
            return true;
        }

        // 상자 등장 확률을 굴리고, 성공하면 후보 중 가중치로 하나를 골라 배치한다. 배치했으면 true.
        protected bool TrySpawnChest(RoomController room)
        {
            if (Random.value >= chestSpawnChance)
                return false;

            return SpawnRandomChest(room, GetDefaultChestPosition(room)) != null;
        }

        // 후보 중 가중치로 하나를 골라 지정 위치에 배치한다. 후보가 없으면 null.
        protected Chest SpawnRandomChest(RoomController room, Vector3 position)
        {
            Chest chestPrefab = PickChestByWeight();
            if (chestPrefab == null)
                return null;

            return Instantiate(chestPrefab, position, Quaternion.identity, room.transform);
        }

        // Chest Spawn Point가 있으면 그 위치, 없으면 방 원점 + 오프셋.
        protected Vector3 GetDefaultChestPosition(RoomController room)
        {
            Transform spawnPoint = room.Definition != null ? room.Definition.ChestSpawnPoint : null;
            return spawnPoint != null ? spawnPoint.position : room.transform.position + (Vector3)chestOffset;
        }

        // 프리팹이 비어 있거나 가중치가 0인 후보는 제외하고, 가중치 비율로 하나를 고른다.
        private Chest PickChestByWeight()
        {
            float totalWeight = 0f;
            foreach (ChestSpawnEntry entry in chestCandidates)
            {
                if (entry != null && entry.ChestPrefab != null)
                    totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f)
                return null;

            float roll = Random.value * totalWeight;
            Chest last = null;

            foreach (ChestSpawnEntry entry in chestCandidates)
            {
                if (entry == null || entry.ChestPrefab == null || entry.Weight <= 0f)
                    continue;

                last = entry.ChestPrefab;
                roll -= entry.Weight;
                if (roll < 0f)
                    return last;
            }

            // 부동소수점 오차로 끝까지 남은 경우 마지막 유효 후보를 쓴다.
            return last;
        }
    }
}
