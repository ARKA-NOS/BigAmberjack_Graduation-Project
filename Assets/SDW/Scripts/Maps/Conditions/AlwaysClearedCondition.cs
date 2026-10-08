using System;

namespace SDW.Scripts.Maps.Conditions
{
    // 조건이 지정되지 않은 방(시작 방, 보상 방 등)에서 쓰는 기본 조건. 입장 즉시 클리어된다.
    public class AlwaysClearedCondition : IRoomClearCondition
    {
        public event Action Cleared;

        public void Begin()
        {
            Cleared?.Invoke();
        }

        public void Dispose()
        {
        }
    }
}
