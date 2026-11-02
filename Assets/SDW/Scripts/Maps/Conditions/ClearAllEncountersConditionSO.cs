using UnityEngine;

namespace SDW.Scripts.Maps.Conditions
{
    [CreateAssetMenu(fileName = "Cond_ClearAllEncounters", menuName = "Map/Condition/Clear All Encounters")]
    public class ClearAllEncountersConditionSO : RoomClearConditionSO
    {
        public override IRoomClearCondition Create(RoomController room)
        {
            return new ClearAllEncountersCondition(room);
        }
    }
}
