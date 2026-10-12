using System.Collections.Generic;
using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Combat", menuName = "Map/Room/Combat Room")]
    public class CombatRoomDataSO : RoomDataSO
    {
        [SerializeField] private List<EnemySpawnEntry> enemies = new();

        public override RoomType RoomType => RoomType.Combat;

        public override void OnFirstEnter(RoomController room)
        {
            SpawnEnemies(room, enemies);
        }

        // 전투 방은 조건을 따로 지정하지 않으면 적 전멸을 클리어 조건으로 쓴다.
        protected override IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }
    }
}
