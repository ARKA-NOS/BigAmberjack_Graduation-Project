using UnityEngine;

namespace _Scripts.Maps.Conditions
{
    [CreateAssetMenu(fileName = "Cond_KillAllEnemies", menuName = "Map/Condition/Kill All Enemies")]
    public class KillAllEnemiesConditionSO : RoomClearConditionSO
    {
        public override IRoomClearCondition Create(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }
    }
}
