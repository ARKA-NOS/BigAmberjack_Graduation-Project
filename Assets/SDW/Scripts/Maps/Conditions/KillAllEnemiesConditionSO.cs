using UnityEngine;

namespace SDW.Scripts.Maps.Conditions
{
    [CreateAssetMenu(fileName = "Cond_KillAllEnemies", menuName = "SDW/Map/Condition/Kill All Enemies")]
    public class KillAllEnemiesConditionSO : RoomClearConditionSO
    {
        public override IRoomClearCondition Create(RoomController room)
        {
            return new KillAllEnemiesCondition(room);
        }
    }
}
