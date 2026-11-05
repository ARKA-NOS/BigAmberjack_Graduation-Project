using UnityEngine;

namespace SDW.Scripts.Maps.Conditions
{
    // 클리어 조건의 부모 SO. 자식 SO는 Create()에서 자신에게 맞는 런타임 조건을 만들어 반환한다.
    // 새 조건이 필요하면 이 클래스를 상속한 SO와 IRoomClearCondition 구현만 추가하면 된다.
    public abstract class RoomClearConditionSO : ScriptableObject
    {
        public abstract IRoomClearCondition Create(RoomController room);
    }
}
