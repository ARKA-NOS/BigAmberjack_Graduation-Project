using System.Collections.Generic;
using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Combat", menuName = "Map/Room/Combat Room")]
    public class CombatRoomDataSO : RoomDataSO
    {
        [Header("Enemy")]
        [SerializeField] private List<EnemySpawnEntry> enemies = new();

        public override RoomType RoomType => RoomType.Combat;

        public override void OnFirstEnter(RoomController room)
        {
            SpawnEnemies(room, enemies);
        }

        // 클리어 후 NPC와 보물상자 중 하나만 나온다.
        // NPC가 나오면 상자는 건너뛰고, NPC가 없거나 확률에 실패하면 상자 후보 중 하나를 뽑는다.
        public override void OnCleared(RoomController room)
        {
            if (!TrySpawnNpc(room))
                TrySpawnChest(room);
        }

        // 전투 방은 조건을 따로 지정하지 않으면 적 전멸을 클리어 조건으로 쓴다.
        protected override IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }
    }
}
