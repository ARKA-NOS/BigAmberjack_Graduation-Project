using System;

namespace SDW.Scripts.Maps.Conditions
{
    // 방의 문 해제(클리어) 조건의 런타임 인스턴스.
    // SO는 여러 방이 공유하는 에셋이라 상태를 가질 수 없으므로, 방마다 이 인스턴스를 새로 만들어 상태를 담는다.
    public interface IRoomClearCondition
    {
        event Action Cleared;

        // 방에 처음 입장해서 RoomDataSO.OnFirstEnter가 끝난 직후 호출된다.
        void Begin();

        void Dispose();
    }
}
